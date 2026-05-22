using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using MedMateAI.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscription");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("UserSubscriptionId").ValueGeneratedOnAdd();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(SubscriptionStatus.Pending)
            .IsRequired();

        builder.HasIndex(x => x.Status);

        builder.HasOne<ApplicationUser>()            
            .WithMany(x => x.UserSubscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.PaymentTransactions)
            .WithOne(x => x.UserSubscription)
            .HasForeignKey(x => x.UserSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.UserSubscription)
            .HasForeignKey(x => x.UserSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
