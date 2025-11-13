namespace MyBooks.Common.BaseClasses;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string User = "User";
    public const string Support = "Support";
    public const string GlobalReviewer = "GlobalReviewer";
    public const string TenantService = "TenantService";
    public const string CatalogService = "CatalogService";
    public const string EmailService = "EmailService";
    public const string FileService = "FileService";
    public const string AuthService = "AuthService";

    // helpers for common combos
    public const string Admins = SuperAdmin + "," + Owner + "," + Admin + "," + Support;
    public const string Editors = Admins + "," + Editor + "," + Support;
    public const string OwnerPlus = Owner + "," + SuperAdmin + "," + Support;
    public const string AllBooksAccess = GlobalReviewer + "," + SuperAdmin ;

    public static readonly string[] AdminsArray = { SuperAdmin, Owner, Admin, Support };
    public static readonly string[] EditorsArray = { SuperAdmin, Owner, Admin, Editor, Support };
    public static readonly string[] AssignableRoles = { Admin, Editor, User };
    public static readonly string[] AllRoles = { SuperAdmin, Owner, Admin, Editor, User, Support, GlobalReviewer };
    public static readonly string[] OwnersArray = { Owner, SuperAdmin, Support };
    public static readonly string[] CustomerRolesArray = { Owner, Admin, Editor, User };
}