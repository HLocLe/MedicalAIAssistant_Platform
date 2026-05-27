namespace MedMateAI.Domain.Entities;

public sealed class FollowUpReminder : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid TreatmentLogId { get; set; }

    public string? ReminderType { get; set; }

    public DateTime? ReminderTime { get; set; }

    public string? Message { get; set; }

    public string? Status { get; set; }

    public TreatmentLog TreatmentLog { get; set; } = null!;

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
