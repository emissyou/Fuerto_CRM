namespace CRM.domain.DTOs;

public class UpdateProgressRequest
{
    public int ProgressPercentage { get; set; }
    public string? DesignNotes { get; set; }
    public string? Stage { get; set; }
}   