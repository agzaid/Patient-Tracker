using Microsoft.EntityFrameworkCore;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<LabTest> LabTests { get; set; }
    public DbSet<RadiologyScan> RadiologyScans { get; set; }
    public DbSet<Diagnosis> Diagnoses { get; set; }
    public DbSet<Surgery> Surgeries { get; set; }
    public DbSet<SharedLink> SharedLinks { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
        });

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne(u => u.Profile)
                  .HasForeignKey<Profile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Medication configuration
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Medications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        // LabTest configuration
        modelBuilder.Entity<LabTest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.LabTests)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.TestName).IsRequired().HasMaxLength(255);
        });

        // RadiologyScan configuration
        modelBuilder.Entity<RadiologyScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RadiologyScans)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.ScanType).IsRequired().HasMaxLength(255);
        });

        // Diagnosis configuration
        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Diagnoses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.DiagnosisName).IsRequired().HasMaxLength(255);
        });

        // Surgery configuration
        modelBuilder.Entity<Surgery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Surgeries)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.SurgeryName).IsRequired().HasMaxLength(255);
        });

        // SharedLink configuration
        modelBuilder.Entity<SharedLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SharedLinks)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // This will be used for initial seed data if needed
        // For now, we'll keep it empty as the app will be populated by users
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                ((BaseEntity)entry.Entity).CreatedAt = DateTime.UtcNow;
            }
            ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }
}

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
