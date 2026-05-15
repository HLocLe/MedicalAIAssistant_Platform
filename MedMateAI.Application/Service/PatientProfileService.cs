using AutoMapper;
using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.PatientProfiles.Requests;
using MedMateAI.Application.DTOs.PatientProfiles.Responses;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Persistence;
using MedMateAI.Domain.Repository;

namespace MedMateAI.Application.Service;

public sealed class PatientProfileService : IPatientProfileService
{
    private readonly IUserService _userService;
    private readonly IGenericRepository<PatientProfile> _patientProfiles;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _uow;

    public PatientProfileService(
        IUserService userService,
        IGenericRepository<PatientProfile> patientProfiles,
        IMapper mapper,
        IUnitOfWork uow)
    {
        _userService = userService;
        _patientProfiles = patientProfiles;
        _mapper = mapper;
        _uow = uow;
    }

   public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteMyProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var current = await _userService.GetCurrentUserAsync(cancellationToken);
        if (current is null)
        {
            return (false, new[] { "Unauthorized." });
        }

        var entity = await _patientProfiles.FirstOrDefaultAsync(
            p => p.UserId == current.Id && !p.IsDeleted,
            asNoTracking: false,
            cancellationToken);

        if (entity is null)
        {
            return (false, new[] { "Patient profile not found." });
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        _patientProfiles.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return (true, Array.Empty<string>());
    }

    public async Task<PagedResponse<PatientProfileResponse>> ListPatientProfilesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _patientProfiles.GetPagedAsync(
            pageNumber,
            pageSize,
            p => !p.IsDeleted,
            q => q.OrderByDescending(p => p.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResponse<PatientProfileResponse>
        {
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
            Items = paged.Items.Select(e => _mapper.Map<PatientProfileResponse>(e)).ToList(),
        };
    }

    public async Task<(bool NotFound, PatientProfileResponse? Data)> GetPatientProfileByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _patientProfiles.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
        {
            return (true, null);
        }

        return (false, _mapper.Map<PatientProfileResponse>(entity));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors, PatientProfileResponse? Data)> CreatePatientProfileAsync(
        CreatePatientProfileRequest request,
        CancellationToken cancellationToken = default)
    {
       
        if (request.UserId==Guid.Empty)
        {
            return (false, new[] { "userid is required." }, null);
        }

        var duplicate = await _patientProfiles.FirstOrDefaultAsync(
            p => p.UserId == request.UserId && !p.IsDeleted,
            asNoTracking: true,
            cancellationToken);
        
        if (duplicate is not null)
        {
            return (false, new[] { "A patient profile already exists for this user." }, null);
        }

        var entity = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            BloodType = string.IsNullOrWhiteSpace(request.BloodType) ? null : request.BloodType.Trim(),
            Height = request.Height,
            Weight = request.Weight,
            AllergyNote = string.IsNullOrWhiteSpace(request.AllergyNote) ? null : request.AllergyNote.Trim(),
            ChronicDiseaseNote = string.IsNullOrWhiteSpace(request.ChronicDiseaseNote)
                ? null
                : request.ChronicDiseaseNote.Trim(),
        };
        _patientProfiles.Add(entity);
        
        await _uow.SaveChangesAsync(cancellationToken);

        var mark = await _userService.MarkPatientProfileCompletedAsync(entity.UserId, cancellationToken);
        var dto=_mapper.Map<PatientProfileResponse>(entity);
        var user = await _userService.GetUserByIdAsync(entity.UserId, cancellationToken);
        dto.IsProfileCompleted = user?.IsProfileCompleted ?? true;
       
        if (!mark.Succeeded)
        {
            return (false, mark.Errors, dto);
        }


        return (true, Array.Empty<string>(), dto);
    }

    public async Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors, PatientProfileResponse? Data)> UpdatePatientProfileAsync(
        Guid id,
        UpdatePatientProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return (false, false, new[] { "Invalid patient profile id." }, null);
        }

        var entity = await _patientProfiles.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
        {
            return (false, true, new[] { "Patient profile not found." }, null);
        }

        if (request.BloodType is not null)
        {
            entity.BloodType = string.IsNullOrWhiteSpace(request.BloodType) ? null : request.BloodType.Trim();
        }

        if (request.Height.HasValue)
        {
            entity.Height = request.Height;
        }

        if (request.Weight.HasValue)
        {
            entity.Weight = request.Weight;
        }

        if (request.AllergyNote is not null)
        {
            entity.AllergyNote = string.IsNullOrWhiteSpace(request.AllergyNote) ? null : request.AllergyNote.Trim();
        }

        if (request.ChronicDiseaseNote is not null)
        {
            entity.ChronicDiseaseNote = string.IsNullOrWhiteSpace(request.ChronicDiseaseNote)
                ? null
                : request.ChronicDiseaseNote.Trim();
        }

        entity.UpdatedAt = DateTime.UtcNow;
        _patientProfiles.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

       

        return (true, false, Array.Empty<string>(), _mapper.Map<PatientProfileResponse>(entity));
    }

    public async Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors)> SoftDeletePatientProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return (false, false, new[] { "Invalid patient profile id." });
        }

        var entity = await _patientProfiles.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
        {
            return (false, true, new[] { "Patient profile not found." });
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _patientProfiles.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return (true, false, Array.Empty<string>());
    }
}
