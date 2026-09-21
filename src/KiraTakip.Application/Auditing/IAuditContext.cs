using KiraTakip.Models.Enums;

namespace KiraTakip.Auditing;

/// <summary>
/// Audit kaydı üreten tüm mekanizmaların kullandığı ortak aktör ve istek bağlamı.
/// HTTP dışı işlemlerde alanlar doğal olarak boş olabilir.
/// </summary>
public interface IAuditContext
{
    string? UserId { get; }
    UserType? UserType { get; }
    int? TenantId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
