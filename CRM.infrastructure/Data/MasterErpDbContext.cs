using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CRM.domain.Entities;

namespace CRM.infrastructure.Data;

public class MasterErpDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Device> Devices { get; set; }

    public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();

    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();

    public MasterErpDbContext(
        DbContextOptions<MasterErpDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.CompanyId);

            entity.Property(x => x.CompanyCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.CompanyCode)
                .IsUnique();
        });

        builder.Entity<CompanyDatabase>(entity =>
        {
            entity.HasKey(x => x.CompanyDatabaseId);

            entity.Property(x => x.ServerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.DatabaseName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.DeviceId);

            entity.Property(x => x.DeviceCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.DeviceName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.CompanyId, x.DeviceCode })
                .IsUnique();
        });

        builder.Entity<CompanySubscription>(entity =>
        {
            entity.HasKey(x => x.SubscriptionId);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.PlanName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.MonthlyFee)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.AvailedModules)
                .HasMaxLength(500)
                .IsRequired();
        });
    }
}