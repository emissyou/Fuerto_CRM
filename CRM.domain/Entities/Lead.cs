namespace CRM.domain.Entities;

public class Lead : CompanyEntity
{
    public int LeadId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string LeadSource { get; set; } = "Other";
    public string Status { get; set; } = "New";   // New, Contacted, Qualified, Converted, Lost
    public string ServiceInterest { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Conversion tracking ----
    public int? ConvertedToCustomerId { get; set; }
    public int? ConvertedToProjectId { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string ConvertedByUserId { get; set; } = string.Empty;
}