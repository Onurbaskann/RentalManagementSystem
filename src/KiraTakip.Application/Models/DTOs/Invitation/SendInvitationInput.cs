namespace KiraTakip.Models.Dtos.Invitation;

public record SendInvitationInput(
    string Email,
    string? FullName,
    int RoleId,
    string InvitedByUserId,
    int? TenantId = null,
    bool HasAccessToAllSpaces = false,
    IReadOnlyList<int>? SpaceIds = null);
