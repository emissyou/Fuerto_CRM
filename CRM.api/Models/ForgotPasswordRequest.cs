namespace CRM.api.Models;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordWithCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}
