
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImage.Api.Entities;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        // Table configuration
        builder.ToTable("Vehicles");

        // Primary key
        builder.HasKey(v => v.Id);

        // Required properties
        builder.Property(v => v.Make)
            .IsRequired();

        builder.Property(v => v.Model)
            .IsRequired();

        builder.Property(v => v.Variant)
            .IsRequired();

        builder.Property(v => v.Year)
            .IsRequired();

        // Timestamps
        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired();

        // One-to-one relationship with VehicleImage
        builder.HasOne(v => v.VehicleImage)
            .WithOne(image => image.Vehicle)
            .HasForeignKey<VehicleImageEntity>(
                image => image.VehicleId
            );
    }
}
