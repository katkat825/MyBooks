namespace MyBooks.Common.BaseClasses;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string User = "User";

    // helpers for common combos
    public const string Admins = SuperAdmin + "," + Owner + "," + Admin;
    public const string Editors = Admins + "," + Editor;
    public const string OwnerPlus = Owner + "," + SuperAdmin;

    public static readonly string[] AdminsArray = { SuperAdmin, Owner, Admin };
    public static readonly string[] EditorsArray = { SuperAdmin, Owner, Admin, Editor };
    public static readonly string[] AssignableRoles = { Admin, Editor, User };
    public static readonly string[] AllRoles = { SuperAdmin, Owner, Admin, Editor, User };
    public static readonly string[] OwnersArray = { Owner, SuperAdmin };
}