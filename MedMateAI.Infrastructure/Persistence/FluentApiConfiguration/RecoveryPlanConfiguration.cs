using MedMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class RecoveryPlanConfiguration : IEntityTypeConfiguration<RecoveryPlan>
{
    public void Configure(EntityTypeBuilder<RecoveryPlan> builder)
    {
        builder.ToTable("RecoveryPlan");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("RecoveryPlanId").ValueGeneratedOnAdd();

        builder.HasOne(x => x.TestSession)
            .WithMany(x => x.RecoveryPlans)
            .HasForeignKey(x => x.TestSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SymptomAnalysisSession)
            .WithMany(x => x.RecoveryPlans)
            .HasForeignKey(x => x.SymptomAnalysisSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.TreatmentLogs)
            .WithOne(x => x.RecoveryPlan)
            .HasForeignKey(x => x.RecoveryPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
