using Microsoft.EntityFrameworkCore;
using CRM.domain.Entities;

namespace CRM.infrastructure.Data;

public class TenantErpDbContext : DbContext
{
    public TenantErpDbContext(
        DbContextOptions<TenantErpDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<RetentionAction> RetentionActions => Set<RetentionAction>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Quotation> Quotations => Set<Quotation>();

    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<ProjectFeedback> ProjectFeedbacks => Set<ProjectFeedback>();
    public DbSet<ProjectIssue> ProjectIssues => Set<ProjectIssue>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<Branch> Branches => Set<Branch>();



    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.ProductId);

            entity.Property(x => x.ProductCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.HasIndex(x => x.ProductCode)
                .IsUnique();
        });

        builder.Entity<Customer>(entity =>
        {

            entity.HasKey(x => x.CustomerId);

            entity.Property(x => x.FirstName)
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.Property(x => x.CustomerType)
                .HasMaxLength(50);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(x => new
            {
                x.CompanyId,
                x.Email
            });

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Lead>(entity => 
        {
            entity.Property(x => x.ConvertedByUserId).HasMaxLength(450);

            entity.HasKey(x => x.LeadId);

            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.LeadSource)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ServiceInterest)
                .HasMaxLength(200);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(x => new
            {
                x.CompanyId,
                x.Email
            });

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(x => x.SupplierId);

            entity.Property(x => x.SupplierCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.SupplierName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.ContactPerson)
                .HasMaxLength(200);

            entity.Property(x => x.ContactNumber)
                .HasMaxLength(30);

            entity.Property(x => x.EmailAddress)
                .HasMaxLength(200);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.HasIndex(x => new
            {
                x.CompanyId,
                x.SupplierCode
            })
            .IsUnique();

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Inventory>(entity =>
        {
            entity.HasKey(x => x.InventoryId);

            entity.Property(x => x.QuantityOnHand)
                .HasPrecision(18, 2);

            entity.Property(x => x.ReorderLevel)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.ProjectId);

            entity.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ProjectName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProjectType).HasMaxLength(100);
            entity.Property(x => x.Location).HasMaxLength(500);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);

            // ---- Workflow fields ----
            entity.Property(x => x.DesignerId).HasMaxLength(450);        // Identity user id length
            entity.Property(x => x.DesignerName).HasMaxLength(200);
            entity.Property(x => x.DesignerAssignedBy).HasMaxLength(450);
            entity.Property(x => x.DesignStage).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DesignNotes).HasMaxLength(4000);

            entity.HasIndex(x => new { x.CompanyId, x.ProjectCode }).IsUnique();

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Quotation>(entity =>
        {
            entity.HasKey(x => x.QuotationId);

            entity.Property(x => x.QuotationNumber).HasMaxLength(50).IsRequired();

            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.Discount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);

            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);

            // ---- Workflow / Payment fields ----
            entity.Property(x => x.AmountPaid).HasPrecision(18, 2);
            entity.Property(x => x.DepositRequired).HasPrecision(18, 2);
            entity.Property(x => x.PaymentMethod).HasMaxLength(50);
            entity.Property(x => x.PaymentReference).HasMaxLength(100);
            entity.Property(x => x.PaymentStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IssuedByUserId).HasMaxLength(450);

            entity.Property(x => x.ApprovalStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ApprovedByUserId).HasMaxLength(450);
            entity.Property(x => x.ApprovedByName).HasMaxLength(200);
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);

            entity.HasIndex(x => new { x.CompanyId, x.QuotationNumber }).IsUnique();

            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });
        builder.Entity<QuotationItem>(entity =>
        {
            entity.HasKey(x => x.QuotationItemId);

            entity.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 2);

            entity.Property(x => x.Unit)
                .HasMaxLength(50);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Quotation)
                .WithMany()
                .HasForeignKey(x => x.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.QuotationId,
                x.SortOrder
            });
        });

        builder.Entity<Activity>(entity =>
        {
            entity.HasKey(x => x.ActivityId);

            entity.Property(x => x.ActivityType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Lead)
                .WithMany()
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });

        // ---- ProjectFeedback ----
        builder.Entity<ProjectFeedback>(entity =>
        {
            entity.HasKey(x => x.ProjectFeedbackId);

            entity.Property(x => x.Comments).HasMaxLength(2000);
            entity.Property(x => x.DesignLikes).HasMaxLength(1000);
            entity.Property(x => x.DesignImprovements).HasMaxLength(1000);
            entity.Property(x => x.SubmittedByUserId).HasMaxLength(450);

            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });

        // ---- ProjectIssue ----
        builder.Entity<ProjectIssue>(entity =>
        {
            entity.HasKey(x => x.ProjectIssueId);

            entity.Property(x => x.IssueType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();

            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.ResolutionNotes).HasMaxLength(4000);
            entity.Property(x => x.RequestedAction).HasMaxLength(1000);
            entity.Property(x => x.PaymentReference).HasMaxLength(100);

            entity.Property(x => x.DisputedAmount).HasPrecision(18, 2);

            entity.Property(x => x.ResolvedByUserId).HasMaxLength(450);
            entity.Property(x => x.ReportedByUserId).HasMaxLength(450);

            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });

        builder.Entity<RetentionAction>(entity =>
        {
            entity.HasKey(x => x.RetentionActionId);

            entity.Property(x => x.OfferType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OfferDescription).HasMaxLength(500);
            entity.Property(x => x.Segment).HasMaxLength(50);
            entity.Property(x => x.Basis).HasMaxLength(1000);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.ScriptUsed).HasMaxLength(4000);
            entity.Property(x => x.Source).HasMaxLength(50);
            entity.Property(x => x.ActionTaken).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.Property(x => x.CreatedByName).HasMaxLength(200);

            entity.Property(x => x.OfferValue).HasPrecision(18, 2);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Promotion>(entity =>
        {
            entity.HasKey(x => x.PromotionId);

            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.OfferType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OfferValue).HasPrecision(18, 2);
            entity.Property(x => x.TargetSegment).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.Property(x => x.CreatedByName).HasMaxLength(200);

            entity.Ignore(x => x.Company);
        });

        builder.Entity<Branch>(entity =>
        {
            entity.HasKey(x => x.BranchId);

            entity.Property(x => x.BranchCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.BranchName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.ContactNumber).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(100);
            entity.Property(x => x.ManagerUserId).HasMaxLength(450);
            entity.Property(x => x.ManagerName).HasMaxLength(200);
            entity.Property(x => x.ManagerEmail).HasMaxLength(200);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeStringProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeStringProperties();
        return base.SaveChanges();
    }

    private void NormalizeStringProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(string) && property.CurrentValue == null && !property.Metadata.IsNullable)
                {
                    property.CurrentValue = string.Empty;
                }
            }
        }
    }
}