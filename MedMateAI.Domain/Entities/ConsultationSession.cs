namespace MedMateAI.Domain.Entities;

public sealed class ConsultationSession : BaseEntity
{
    public Guid SymptomAnalysisSessionId { get; set; }

    public Guid UserId { get; set; }

    public Guid FacilityId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid? DoctorId { get; set; }

    public string? VisitReason { get; set; }

    public string? CurrentSymptoms { get; set; }

    public string? Status { get; set; }

    public SymptomAnalysisSession SymptomAnalysisSession { get; set; } = null!;

    public MedicalFacility Facility { get; set; } = null!;

    public MedicalDepartment Department { get; set; } = null!;

    public Doctor? Doctor { get; set; }

    public ICollection<ConsultationQuestion> ConsultationQuestions { get; set; } = new List<ConsultationQuestion>();

    public ICollection<AISystemConfig> AISystemConfigs { get; set; } = new List<AISystemConfig>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
}
