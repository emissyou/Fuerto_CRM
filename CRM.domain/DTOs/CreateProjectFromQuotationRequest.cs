namespace CRM.domain.DTOs;

public class CreateProjectFromQuotationRequest
{
    public int CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? TargetEndDate { get; set; }
}