
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleImageConfiguration
    : IEntityTypeConfiguration<VehicleImageEntity>
{
    public void Configure(EntityTypeBuilder<VehicleImageEntity> builder)
    {
        // Table configuration
        builder.ToTable("VehicleImages");

        // Primary key
        builder.HasKey(image => image.Id);

        // Required properties
        builder.Property(image => image.SourceUrl)
            .IsRequired();

        builder.Property(image => image.StorageKey)
            .IsRequired();

        builder.Property(image => image.ContentType)
            .IsRequired();

        builder.Property(image => image.Format)
            .IsRequired();

        builder.Property(image => image.RetrievalStrategy)
            .IsRequired();

        builder.Property(image => image.Status)
            .IsRequired();

        // Optional properties
        builder.Property(image => image.FinalSourceUrl)
            .IsRequired(false);

        builder.Property(image => image.FailureReason)
            .IsRequired(false);

        // Image metadata
        builder.Property(image => image.FileSize)
            .IsRequired();

        builder.Property(image => image.Width)
            .IsRequired();

        builder.Property(image => image.Height)
            .IsRequired();

        // Timestamps
        builder.Property(image => image.CreatedAt)
            .IsRequired();

        builder.Property(image => image.UpdatedAt)
            .IsRequired();
    }
}
