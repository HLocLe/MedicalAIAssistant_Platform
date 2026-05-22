using MedMateAI.Domain.Entities;
using MedMateAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();

    public DbSet<SymptomAnalysisSession> SymptomAnalysisSessions => Set<SymptomAnalysisSession>();

    public DbSet<SessionSymptom> SessionSymptoms => Set<SessionSymptom>();

    public DbSet<MedicalDepartment> MedicalDepartments => Set<MedicalDepartment>();

    public DbSet<DepartmentRecommendation> DepartmentRecommendations => Set<DepartmentRecommendation>();

    public DbSet<ConsultationSession> ConsultationSessions => Set<ConsultationSession>();

    public DbSet<ConsultationQuestion> ConsultationQuestions => Set<ConsultationQuestion>();

    public DbSet<MedicalFacility> MedicalFacilities => Set<MedicalFacility>();

    public DbSet<FacilityDepartment> FacilityDepartments => Set<FacilityDepartment>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<MedicalVisit> MedicalVisits => Set<MedicalVisit>();

    public DbSet<FeedbackReview> FeedbackReviews => Set<FeedbackReview>();

    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    public DbSet<MedicalRecordFile> MedicalRecordFiles => Set<MedicalRecordFile>();

    public DbSet<LabResult> LabResults => Set<LabResult>();

    public DbSet<LabResultDetail> LabResultDetails => Set<LabResultDetail>();

    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();

    public DbSet<AISystemConfig> AISystemConfigs => Set<AISystemConfig>();

    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    public DbSet<Medicine> Medicines => Set<Medicine>();

    public DbSet<MedicationScan> MedicationScans => Set<MedicationScan>();

    public DbSet<MedicationScanResult> MedicationScanResults => Set<MedicationScanResult>();

    public DbSet<UserMedication> UserMedications => Set<UserMedication>();

    public DbSet<DrugAnalysis> DrugAnalyses => Set<DrugAnalysis>();

    public DbSet<DrugAnalysisResult> DrugAnalysisResults => Set<DrugAnalysisResult>();

    public DbSet<TreatmentJourney> TreatmentJourneys => Set<TreatmentJourney>();

    public DbSet<RecoveryPlan> RecoveryPlans => Set<RecoveryPlan>();

    public DbSet<TreatmentLog> TreatmentLogs => Set<TreatmentLog>();

    public DbSet<FollowUpReminder> FollowUpReminders => Set<FollowUpReminder>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<Payment> Payments => Set<Payment>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
