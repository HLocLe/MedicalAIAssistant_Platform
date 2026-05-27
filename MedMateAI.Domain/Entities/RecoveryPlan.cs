namespace MedMateAI.Domain.Entities;

public sealed class RecoveryPlan : BaseEntity
{
    public Guid TreatmentJourneyId { get; set; }

    public Guid? TestSessionId { get; set; }

    public Guid? SymptomAnalysisSessionId { get; set; }

    public string? PlanName { get; set; }

    public int DurationDays { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public TreatmentJourney TreatmentJourney { get; set; } = null!;

    public LabTestSession? TestSession { get; set; }

    public SymptomAnalysisSession? SymptomAnalysisSession { get; set; }

    public ICollection<TreatmentLog> TreatmentLogs { get; set; } = new List<TreatmentLog>();

    public ICollection<AISystemConfig> AISystemConfigs { get; set; } = new List<AISystemConfig>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
}
