namespace CRM.domain.DTOs;

public class ConvertLeadRequest
{
    public string? CustomerType { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectType { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}