namespace MedMateAI.Domain.Entities;

public sealed class LabTestResultDetail : BaseEntity
{
    public Guid TestSessionId { get; set; }

    public Guid IndicatorId { get; set; }

    public double? UserValue { get; set; }

    public string? Status { get; set; }

    public LabTestSession TestSession { get; set; } = null!;

    public LabIndicatorMaster Indicator { get; set; } = null!;
}
