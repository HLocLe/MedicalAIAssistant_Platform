using System.Globalization;
using System.Text.Json;
using MedMateAI.Application.DTOs.Request;
using MedMateAI.Application.DTOs.Response;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace MedMateAI.Infrastructure.Services;

public sealed class MedicalDepartmentService : IMedicalDepartmentService
{
    private const int DefaultPageSize = 10;
    private const string ListCacheVersionKey = "medical-departments:list-version";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = CacheTtl,
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public MedicalDepartmentService(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IDistributedCache cache)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<PagedMedicalDepartmentsResponse> ListMedicalDepartmentsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? DefaultPageSize : pageSize;
        var listVersion = await GetListCacheVersionAsync(cancellationToken);
        var cacheKey = $"medical-departments:page:{page}:size:{size}:v:{listVersion}";

        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResponse = JsonSerializer.Deserialize<PagedMedicalDepartmentsResponse>(cached);
            if (cachedResponse is not null)
            {
                return cachedResponse;
            }
        }

        var query = _dbContext.MedicalDepartments
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DepartmentName);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)size);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new MedicalDepartmentResponse
            {
                Id = x.Id,
                DepartmentName = x.DepartmentName,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var response = new PagedMedicalDepartmentsResponse
        {
            PageNumber = page,
            PageSize = size,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items,
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), CacheOptions, cancellationToken);

        return response;
    }

    public async Task<MedicalDepartmentResponse?> GetMedicalDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"medical-departments:{id}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResponse = JsonSerializer.Deserialize<MedicalDepartmentResponse>(cached);
            if (cachedResponse is not null)
            {
                return cachedResponse;
            }
        }

        var item = await _dbContext.MedicalDepartments
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == id)
            .Select(x => new MedicalDepartmentResponse
            {
                Id = x.Id,
                DepartmentName = x.DepartmentName,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(item), CacheOptions, cancellationToken);
        return item;
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors, MedicalDepartmentResponse? Data)> CreateMedicalDepartmentAsync(
        CreateMedicalDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return (false, new[] { "Department name is required." }, null);
        }

        var entity = new MedicalDepartment
        {
            Id = Guid.NewGuid(),
            DepartmentName = request.DepartmentName.Trim(),
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.MedicalDepartments.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateListCacheAsync(cancellationToken);

        return (true, Array.Empty<string>(), MapToResponse(entity));
    }

    public async Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors, MedicalDepartmentResponse? Data)> UpdateMedicalDepartmentAsync(
        Guid id,
        UpdateMedicalDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return (false, false, new[] { "Invalid medical department id." }, null);
        }

        if (request.DepartmentName is not null && string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return (false, false, new[] { "Department name cannot be empty when provided." }, null);
        }

        var entity = await _dbContext.MedicalDepartments
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (entity is null)
        {
            return (false, true, new[] { "Medical department not found." }, null);
        }

        if (request.DepartmentName is not null)
        {
            entity.DepartmentName = request.DepartmentName.Trim();
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var idCacheKey = $"medical-departments:{id}";
        await _cache.RemoveAsync(idCacheKey, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);

        return (true, false, Array.Empty<string>(), MapToResponse(entity));
    }

    public async Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors)> SoftDeleteMedicalDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return (false, false, new[] { "Invalid medical department id." });
        }

        var entity = await _dbContext.MedicalDepartments
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (entity is null)
        {
            return (false, true, new[] { "Medical department not found." });
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var idCacheKey = $"medical-departments:{id}";
        await _cache.RemoveAsync(idCacheKey, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);

        return (true, false, Array.Empty<string>());
    }

    private static MedicalDepartmentResponse MapToResponse(MedicalDepartment entity)
    {
        return new MedicalDepartmentResponse
        {
            Id = entity.Id,
            DepartmentName = entity.DepartmentName,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    private async Task<int> GetListCacheVersionAsync(CancellationToken cancellationToken)
    {
        var rawValue = await _cache.GetStringAsync(ListCacheVersionKey, cancellationToken);
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) && version > 0)
        {
            return version;
        }

        const int initialVersion = 1;
        await _cache.SetStringAsync(
            ListCacheVersionKey,
            initialVersion.ToString(CultureInfo.InvariantCulture),
            CacheOptions,
            cancellationToken);

        return initialVersion;
    }

    private async Task InvalidateListCacheAsync(CancellationToken cancellationToken)
    {
        // IDistributedCache does not support wildcard removal, so list cache entries are versioned.
        var currentVersion = await GetListCacheVersionAsync(cancellationToken);
        var nextVersion = currentVersion + 1;

        await _cache.SetStringAsync(
            ListCacheVersionKey,
            nextVersion.ToString(CultureInfo.InvariantCulture),
            CacheOptions,
            cancellationToken);
    }
}
