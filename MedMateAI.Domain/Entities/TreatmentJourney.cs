namespace MedMateAI.Domain.Entities;

public sealed class TreatmentJourney : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? FacilityId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? DoctorId { get; set; }

    

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovalStatus { get; set; }

    public string? ApprovalNote { get; set; }

    public string? Title { get; set; }

    public string? DiagnosisSummary { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public MedicalFacility? Facility { get; set; }

    public MedicalDepartment? Department { get; set; }

    public Doctor? Doctor { get; set; }

   

    public ICollection<RecoveryPlan> RecoveryPlans { get; set; } = new List<RecoveryPlan>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();

    public ICollection<UserMedication> UserMedications { get; set; } = new List<UserMedication>();
}
