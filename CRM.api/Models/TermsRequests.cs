namespace CRM.api.Models;

public class UpdateTermsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool ForceReacceptance { get; set; } = false;
}

public class RevokeTermsRequest
{
    public int CompanyId { get; set; }
}
