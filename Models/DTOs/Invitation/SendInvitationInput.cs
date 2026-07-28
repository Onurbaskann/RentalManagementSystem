namespace KiraTakip.Models.Dtos.Invitation;

public record SendInvitationInput(
    string Email,
    string? FullName,
    int RoleId,
    string InvitedByUserId,
    int? TenantId = null,
    bool HasAccessToAllProperties = false,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);
