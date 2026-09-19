namespace CRM.domain.Entities;

public class Product : CompanyEntity
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
}