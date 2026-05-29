using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Persistence;
using MedMateAI.Domain.Repository;
using MedMateAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace MedMateAI.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IMedicalFacilityRepository? _medicalFacilities;
    private IDoctorRepository? _doctors;
    private IFeedbackReviewRepository? _feedbackReviews;
    private ISymptomAnalysisSessionRepository? _symptomAnalysisSessions;
    private ISessionSymptomRepository? _sessionSymptoms;
    private IDepartmentRecommendationRepository? _departmentRecommendations;
    private IMedicalDepartmentRepository? _medicalDepartments;
    private IFacilityDepartmentRepository? _facilityDepartments;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IMedicalFacilityRepository MedicalFacilities =>
        _medicalFacilities ??= new MedicalFacilityRepository(_context);

    public IDoctorRepository Doctors =>
        _doctors ??= new DoctorRepository(_context);

    public IFeedbackReviewRepository FeedbackReviews =>
        _feedbackReviews ??= new FeedbackReviewRepository(_context);

    public ISymptomAnalysisSessionRepository SymptomAnalysisSessions =>
        _symptomAnalysisSessions ??= new SymptomAnalysisSessionRepository(_context);

    public ISessionSymptomRepository SessionSymptoms =>
        _sessionSymptoms ??= new SessionSymptomRepository(_context);

    public IDepartmentRecommendationRepository DepartmentRecommendations =>
        _departmentRecommendations ??= new DepartmentRecommendationRepository(_context);

    public IMedicalDepartmentRepository MedicalDepartments =>
        _medicalDepartments ??= new MedicalDepartmentRepository(_context);

    public IFacilityDepartmentRepository FacilityDepartments =>
        _facilityDepartments ??= new FacilityDepartmentRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
