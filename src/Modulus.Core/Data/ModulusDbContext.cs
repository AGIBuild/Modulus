using Microsoft.EntityFrameworkCore;
using Modulus.Core.Data.Entities;

namespace Modulus.Core.Data;

/// <summary>
/// EF Core DbContext for Modulus application data.
/// </summary>
public class ModulusDbContext : DbContext
{
    public ModulusDbContext(DbContextOptions<ModulusDbContext> options) : base(options)
    {
    }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<InstalledModule> InstalledModules => Set<InstalledModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AppSetting configuration
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Value).IsRequired();
        });

        // InstalledModule configuration
        modelBuilder.Entity<InstalledModule>(entity =>
        {
            entity.ToTable("InstalledModules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.PackagePath).IsRequired();
        });
    }
}

