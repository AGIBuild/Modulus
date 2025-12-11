using Microsoft.EntityFrameworkCore;
using Modulus.Infrastructure.Data.Models;
using Modulus.UI.Abstractions;

namespace Modulus.Infrastructure.Data;

public class ModulusDbContext : DbContext
{
    public DbSet<ModuleEntity> Modules { get; set; } = null!;
    public DbSet<MenuEntity> Menus { get; set; } = null!;
    public DbSet<AppSettingEntity> AppSettings { get; set; } = null!;
    public DbSet<PendingCleanupEntity> PendingCleanups { get; set; } = null!;

    public ModulusDbContext(DbContextOptions<ModulusDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ModuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.ManifestHash);
            entity.Property(e => e.ValidatedAt);
            entity.Property(e => e.MenuLocation).HasDefaultValue(MenuLocation.Main);
        });

        modelBuilder.Entity<MenuEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired();
            
            entity.HasOne(d => d.Module)
                .WithMany(p => p.Menus)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSettingEntity>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Value).IsRequired();
        });

        modelBuilder.Entity<PendingCleanupEntity>(entity =>
        {
            entity.ToTable("PendingCleanups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DirectoryPath).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.ModuleId).HasMaxLength(64);
            entity.HasIndex(e => e.DirectoryPath).IsUnique();
            entity.HasIndex(e => e.ModuleId);
        });
    }
}

