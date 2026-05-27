namespace MedMateAI.Domain.Entities;

public sealed class LabTestSession : BaseEntity
{
    public Guid UserId { get; set; }

    public string? RawOcrText { get; set; }

    public ICollection<LabTestResultDetail> LabTestResultDetails { get; set; } = new List<LabTestResultDetail>();

    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();

    public ICollection<AISystemConfig> AISystemConfigs { get; set; } = new List<AISystemConfig>();

    public ICollection<RecoveryPlan> RecoveryPlans { get; set; } = new List<RecoveryPlan>();
}
