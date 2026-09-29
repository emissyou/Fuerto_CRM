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
    /// Roles that a Super Admin can create/edit/delete for a company.
    /// Super Admin ONLY manages Admin accounts of the company.
    /// </summary>
    public static readonly string[] SuperAdminCanTouch =
    {
        Admin
    };

    /// <summary>
    /// Roles that an Admin in the company can create/edit/delete.
    /// Company Admin manages user accounts: Manager and Staff.
    /// </summary>
    public static readonly string[] AdminCanTouch =
    {
        Manager, Staff
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