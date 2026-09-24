namespace CRM.domain.Enums;

public static class ApplicationRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";   // Staff now also does design work

    public static readonly string[] All =
    {
        SuperAdmin, Admin, Manager, Staff
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
        Staff
    };

    /// <summary>
    /// Roles that can be assigned to projects as designers.
    /// (Staff does design work; no separate Designer role.)
    /// </summary>
    public static readonly string[] CanBeDesigner =
    {
        Staff, Manager, Admin, SuperAdmin
    };
}