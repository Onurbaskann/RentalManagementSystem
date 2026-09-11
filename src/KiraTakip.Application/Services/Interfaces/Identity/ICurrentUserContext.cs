using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Identity;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? DisplayName => null;
    string? EmailAddress => null;
    UserType? UserType { get; }
    int? TenantId { get; }
    bool IsKiraciUser { get; }
    bool IsSuperAdmin => false;
}
