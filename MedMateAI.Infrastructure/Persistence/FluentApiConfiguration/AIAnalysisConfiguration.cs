using MedMateAI.Domain.Entities;
using MedMateAI.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class AIAnalysisConfiguration : IEntityTypeConfiguration<AIAnalysis>
{
    public void Configure(EntityTypeBuilder<AIAnalysis> builder)
    {
        builder.ToTable("AIAnalysis");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("AIAnalysisId").ValueGeneratedOnAdd();

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecoveryPlan)
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.RecoveryPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SymptomAnalysisSession)
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.SymptomAnalysisSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.TestSession)
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.TestSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ConsultationSession)
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.ConsultationSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
