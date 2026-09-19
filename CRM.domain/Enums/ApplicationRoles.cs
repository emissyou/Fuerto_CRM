namespace CRM.domain.Enums;

public static class ApplicationRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Designer = "Designer";

    public static readonly string[] All =
    {
        SuperAdmin, Admin, Staff, Designer
    };
}