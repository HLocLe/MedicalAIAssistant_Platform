namespace MedMateAI.Domain.Entities;

public sealed class AISystemConfig : BaseEntity
{
    public Guid? SymptomAnalysisSessionId { get; set; }

    public Guid? RecoveryPlanId { get; set; }

    public Guid? TestSessionId { get; set; }

    public Guid? ConsultationSessionId { get; set; }

    public string TaskType { get; set; } = string.Empty;

    public string? SystemPrompt { get; set; }

    public string? Model { get; set; }

    public decimal? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public bool IsActive { get; set; }

    public SymptomAnalysisSession? SymptomAnalysisSession { get; set; }

    public RecoveryPlan? RecoveryPlan { get; set; }

    public LabTestSession? TestSession { get; set; }

    public ConsultationSession? ConsultationSession { get; set; }
}
