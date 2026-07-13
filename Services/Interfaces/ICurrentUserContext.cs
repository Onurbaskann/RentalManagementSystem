using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }
    UserType? UserType { get; }
    int? TenantId { get; }
    bool IsKiraciUser { get; }
}
