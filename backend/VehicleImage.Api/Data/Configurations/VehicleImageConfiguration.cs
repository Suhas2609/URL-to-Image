using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImageEntity>
{
    public void Configure(EntityTypeBuilder<VehicleImageEntity> builder)
    {
        builder.ToTable("VehicleImages", t =>
        {
            t.HasCheckConstraint("CK_VehicleImages_FileSize", "[FileSize] > 0");
            t.HasCheckConstraint("CK_VehicleImages_Width", "[Width] > 0");
            t.HasCheckConstraint("CK_VehicleImages_Height", "[Height] > 0");
        });

        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id)
            .ValueGeneratedNever();

        builder.HasIndex(image => image.VehicleId)
            .IsUnique();

        builder.HasIndex(image => image.StorageKey)
            .IsUnique();

        builder.Property(image => image.SourceUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(image => image.FinalSourceUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(image => image.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(image => image.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(image => image.Format)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(image => image.RetrievalStrategy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(image => image.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(image => image.FailureReason)
            .IsRequired(false);

        builder.Property(image => image.Sha256)
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(image => image.FileSize)
            .IsRequired();

        builder.Property(image => image.Width)
            .IsRequired();

        builder.Property(image => image.Height)
            .IsRequired();

        builder.Property(image => image.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(image => image.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
