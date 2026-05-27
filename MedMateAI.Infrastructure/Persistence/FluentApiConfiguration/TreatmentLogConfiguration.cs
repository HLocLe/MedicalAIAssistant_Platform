using MedMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class TreatmentLogConfiguration : IEntityTypeConfiguration<TreatmentLog>
{
    public void Configure(EntityTypeBuilder<TreatmentLog> builder)
    {
        builder.ToTable("TreatmentLog");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("TreatmentLogId").ValueGeneratedOnAdd();

        builder.Property(x => x.AiFeedbackNote).HasColumnName("AI_FeedbackNote");
    }
}
