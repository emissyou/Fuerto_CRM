namespace CRM.domain.Entities;

public class Customer : CompanyEntity
{
    public int CustomerId { get; set; }
    public int? BranchId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? CustomerType { get; set; } = "Regular";
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Conversion tracking ----
    public int? ConvertedFromLeadId { get; set; }
}