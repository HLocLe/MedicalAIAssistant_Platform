using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MedMateAI.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public PatientProfile? PatientProfile { get; set; }

    public ICollection<SymptomAnalysisSession> SymptomAnalysisSessions { get; set; } = new List<SymptomAnalysisSession>();

    public ICollection<ConsultationSession> ConsultationSessions { get; set; } = new List<ConsultationSession>();

    public ICollection<MedicalVisit> MedicalVisits { get; set; } = new List<MedicalVisit>();

    public ICollection<FeedbackReview> FeedbackReviews { get; set; } = new List<FeedbackReview>();

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();

    public ICollection<MedicationScan> MedicationScans { get; set; } = new List<MedicationScan>();

    public ICollection<UserMedication> UserMedications { get; set; } = new List<UserMedication>();

    public ICollection<DrugAnalysis> DrugAnalyses { get; set; } = new List<DrugAnalysis>();

    public ICollection<TreatmentJourney> TreatmentJourneys { get; set; } = new List<TreatmentJourney>();

    public ICollection<FollowUpReminder> FollowUpReminders { get; set; } = new List<FollowUpReminder>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public string? DisplayName { get; set; }

    public string? Address { get; set; }
    
    public UserStatus Status { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool IsFirstLogin { get; set; }

    public bool IsProfileCompleted { get; set; }
}
