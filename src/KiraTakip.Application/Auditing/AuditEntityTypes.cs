using KiraTakip.Models.Entities;

namespace KiraTakip.Auditing;

/// <summary>
/// Manuel audit olaylarında kullanılan merkezi varlık adları.
/// </summary>
public static class AuditEntityTypes
{
    public const string ApplicationUser = nameof(ApplicationUser);
    public const string Invitation = nameof(Invitation);
    public const string PasswordResetRequest = nameof(PasswordResetRequest);
    public const string Role = nameof(Role);

    private static readonly HashSet<string> Defined =
    [
        ApplicationUser,
        Invitation,
        PasswordResetRequest,
        Role
    ];

    public static bool IsDefined(string entityType) => Defined.Contains(entityType);
}
