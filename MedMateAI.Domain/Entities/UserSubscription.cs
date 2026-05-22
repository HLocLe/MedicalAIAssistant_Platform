using MedMateAI.Domain.Enums;

namespace MedMateAI.Domain.Entities;

public sealed class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SubscriptionStatus Status { get; set; }

    public bool AutoRenew { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;

    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
