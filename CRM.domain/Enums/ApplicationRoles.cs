namespace CRM.domain.Enums;

public static class ApplicationRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Designer = "Designer";

    public static readonly string[] All =
    {
        SuperAdmin, Admin, Manager, Staff, Designer
    };

    /// <summary>
    /// Roles that can access the User Management module.
    /// </summary>
    public static readonly string[] CanManageUsers =
    {
        SuperAdmin, Admin, Manager
    };

    /// <summary>
    /// Roles that a Manager can create/edit/delete.
    /// </summary>
    public static readonly string[] ManagerCanTouch =
    {
        Staff, Designer
    };
}