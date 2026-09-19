namespace CRM.domain.DTOs;

public class AssignDesignerRequest
{
    public string DesignerId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}