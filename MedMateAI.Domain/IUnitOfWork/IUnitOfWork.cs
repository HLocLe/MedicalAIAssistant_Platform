using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Repository;

namespace MedMateAI.Domain.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    IMedicalFacilityRepository MedicalFacilities { get; }
    IDoctorRepository Doctors { get; }
    IFeedbackReviewRepository FeedbackReviews { get; }

    ISymptomAnalysisSessionRepository SymptomAnalysisSessions { get; }

    ISessionSymptomRepository SessionSymptoms { get; }

    IDepartmentRecommendationRepository DepartmentRecommendations { get; }

    IMedicalDepartmentRepository MedicalDepartments { get; }

    IFacilityDepartmentRepository FacilityDepartments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
