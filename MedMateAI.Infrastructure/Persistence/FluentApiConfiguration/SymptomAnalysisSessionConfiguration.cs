using MedMateAI.Domain.Entities;
using MedMateAI.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class SymptomAnalysisSessionConfiguration : IEntityTypeConfiguration<SymptomAnalysisSession>
{
    public void Configure(EntityTypeBuilder<SymptomAnalysisSession> builder)
    {
        builder.ToTable("SymptomAnalysisSession");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SymptomAnalysisSessionId").ValueGeneratedOnAdd();

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.SymptomAnalysisSessions)
            .HasForeignKey(x => x.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.SessionSymptoms)
            .WithOne(x => x.SymptomAnalysisSession)
            .HasForeignKey(x => x.SymptomAnalysisSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DepartmentRecommendations)
            .WithOne(x => x.SymptomAnalysisSession)
            .HasForeignKey(x => x.SymptomAnalysisSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ConsultationSessions)
            .WithOne(x => x.SymptomAnalysisSession)
            .HasForeignKey(x => x.SymptomAnalysisSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
