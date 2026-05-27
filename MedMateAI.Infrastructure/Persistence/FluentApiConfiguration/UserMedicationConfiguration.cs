using MedMateAI.Domain.Entities;
using MedMateAI.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMateAI.Infrastructure.Persistence.FluentAPiConfiguration;

public sealed class UserMedicationConfiguration : IEntityTypeConfiguration<UserMedication>
{
    public void Configure(EntityTypeBuilder<UserMedication> builder)
    {
        builder.ToTable("UserMedication");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("UserMedicationId").ValueGeneratedOnAdd();

        builder.Property(x => x.MedicineName).HasMaxLength(256).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.UserMedications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
