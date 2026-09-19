namespace CRM.domain.Entities;

public class Customer : CompanyEntity
{
    public int CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public string CustomerType { get; set; } = "Regular";
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Conversion tracking ----
    public int? ConvertedFromLeadId { get; set; }
}