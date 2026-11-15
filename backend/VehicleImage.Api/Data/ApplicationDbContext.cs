
using Microsoft.EntityFrameworkCore;
using VehicleImage.Api.Entities;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleImageEntity> VehicleImages =>
        Set<VehicleImageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly
        );
    }
}
