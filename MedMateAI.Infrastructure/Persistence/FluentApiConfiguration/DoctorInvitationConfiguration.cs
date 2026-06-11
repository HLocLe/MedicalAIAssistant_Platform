using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class DoctorInvitationConfiguration : IEntityTypeConfiguration<DoctorInvitation>
{
    public void Configure(EntityTypeBuilder<DoctorInvitation> builder)
    {
        builder.ToTable("DoctorInvitation");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("DoctorInvitationId").ValueGeneratedOnAdd();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(DoctorInvitationStatus.Pending)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.DoctorId);

        builder.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
