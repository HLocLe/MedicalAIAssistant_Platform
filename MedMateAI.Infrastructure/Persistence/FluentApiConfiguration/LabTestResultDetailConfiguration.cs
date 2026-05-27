using MedMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class LabTestResultDetailConfiguration : IEntityTypeConfiguration<LabTestResultDetail>
{
    public void Configure(EntityTypeBuilder<LabTestResultDetail> builder)
    {
        builder.ToTable("LabTestResultDetail");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ResultDetailId").ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(50);
    }
}
