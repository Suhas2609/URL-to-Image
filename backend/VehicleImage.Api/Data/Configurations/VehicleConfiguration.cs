using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImage.Api.Entities;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles", t =>
        {
            t.HasCheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= 2100");
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Make)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Variant)
            .IsRequired(false)
            .HasMaxLength(150);

        builder.Property(v => v.Year)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(v => v.Image)
            .WithOne(image => image.Vehicle)
            .HasForeignKey<VehicleImageEntity>(image => image.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
