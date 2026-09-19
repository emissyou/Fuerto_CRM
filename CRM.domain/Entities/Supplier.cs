namespace CRM.domain.Entities;

public class Supplier : CompanyEntity
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}