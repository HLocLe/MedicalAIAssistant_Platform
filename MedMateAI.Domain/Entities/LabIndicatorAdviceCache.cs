namespace MedMateAI.Domain.Entities;

public sealed class LabIndicatorAdviceCache : BaseEntity
{
    public Guid IndicatorId { get; set; }

    public string? Status { get; set; }

    public string? PossibleCauses { get; set; }

    public string? LifestyleAdvice { get; set; }

    public string? NutritionalAdvice { get; set; }

    public string? UrgencyLevel { get; set; }

    public string? WarningSigns { get; set; }

    public string? FollowUpSuggestion { get; set; }

    public string? DoctorQuestions { get; set; }

    public LabIndicatorMaster Indicator { get; set; } = null!;
}
