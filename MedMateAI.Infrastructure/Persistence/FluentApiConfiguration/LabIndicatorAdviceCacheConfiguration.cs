using MedMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class LabIndicatorAdviceCacheConfiguration : IEntityTypeConfiguration<LabIndicatorAdviceCache>
{
    public void Configure(EntityTypeBuilder<LabIndicatorAdviceCache> builder)
    {
        builder.ToTable("LabIndicatorAdviceCache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("CacheId").ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.UrgencyLevel).HasMaxLength(50);

        builder.HasIndex(x => new { x.IndicatorId, x.Status }).IsUnique();
    }
}
