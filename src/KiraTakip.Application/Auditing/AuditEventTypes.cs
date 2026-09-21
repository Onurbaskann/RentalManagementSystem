namespace KiraTakip.Auditing;

/// <summary>
/// İş ve güvenlik olaylarında kullanılan merkezi audit olay adları.
/// </summary>
public static class AuditEventTypes
{
    public const string EntityAdded = "Entity.Added";
    public const string EntityModified = "Entity.Modified";
    public const string EntityDeleted = "Entity.Deleted";

    public const string UserActivated = "User.Activated";
    public const string UserDeactivated = "User.Deactivated";
    public const string UserRoleChanged = "User.RoleChanged";
    public const string UserScopeChanged = "User.ScopeChanged";
    public const string UserLoginFailed = "User.LoginFailed";
    public const string UserLoginSuccess = "User.LoginSuccess";
    public const string UserLockedOut = "User.LockedOut";
    public const string UserLogout = "User.Logout";
    public const string UserPasswordChanged = "User.PasswordChanged";
    public const string UserPasswordResetRequested = "User.PasswordReset.Requested";
    public const string UserPasswordResetCompleted = "User.PasswordReset.Completed";

    public const string RoleCreated = "Role.Created";
    public const string RoleUpdated = "Role.Updated";
    public const string RoleDeleted = "Role.Deleted";
    public const string RolePermissionChanged = "Role.Permission.Changed";

    public const string InviteSent = "Invite.Sent";
    public const string InviteAccepted = "Invite.Accepted";
    public const string InviteCancelled = "Invite.Cancelled";
    public const string InviteResent = "Invite.Resent";

    private static readonly HashSet<string> Defined =
    [
        EntityAdded,
        EntityModified,
        EntityDeleted,
        UserActivated,
        UserDeactivated,
        UserRoleChanged,
        UserScopeChanged,
        UserLoginFailed,
        UserLoginSuccess,
        UserLockedOut,
        UserLogout,
        UserPasswordChanged,
        UserPasswordResetRequested,
        UserPasswordResetCompleted,
        RoleCreated,
        RoleUpdated,
        RoleDeleted,
        RolePermissionChanged,
        InviteSent,
        InviteAccepted,
        InviteCancelled,
        InviteResent
    ];

    public static bool IsDefined(string eventType) => Defined.Contains(eventType);
}
