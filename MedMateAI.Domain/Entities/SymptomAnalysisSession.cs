namespace MedMateAI.Domain.Entities;

public sealed class SymptomAnalysisSession : BaseEntity
{
    public Guid? UserId { get; set; }

    public string? InputText { get; set; }

    public string? SeverityLevel { get; set; }

    public string? Status { get; set; }

    public bool DisclaimerShown { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ICollection<SessionSymptom> SessionSymptoms { get; set; } = new List<SessionSymptom>();

    public ICollection<DepartmentRecommendation> DepartmentRecommendations { get; set; } = new List<DepartmentRecommendation>();

    public ICollection<ConsultationSession> ConsultationSessions { get; set; } = new List<ConsultationSession>();

    public ICollection<AISystemConfig> AISystemConfigs { get; set; } = new List<AISystemConfig>();
}
