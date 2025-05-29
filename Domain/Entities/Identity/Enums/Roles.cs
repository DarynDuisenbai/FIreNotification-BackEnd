namespace Domain.Entities.Identity.Enums
{
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Manager = "manager";
        public const string User = "user";

        public static readonly string[] AllRoles = { Admin, Manager, User };

        public static bool IsValidRole(string role)
        {
            return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}