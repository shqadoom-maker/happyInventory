namespace Bariza.Core.Enums;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Management = "Management";
    public const string Auditor = "Auditor";
    public const string Seller = "Seller";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Management, Auditor, Seller };

    public static string? Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return All.FirstOrDefault(
            candidate => string.Equals(candidate, role.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
