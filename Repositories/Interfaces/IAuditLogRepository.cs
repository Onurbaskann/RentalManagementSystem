using KiraTakip.Models.Entities;
using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

/// <summary>
/// AuditLog long anahtarlı ortak okuma altyapısını kullanır.
/// Append-only yapısı nedeniyle yazma operasyonlarından yalnızca AddAsync sunulur.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog, long>
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<PagedResult<AuditLog>> QueryAsync(
        string? eventType,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        TableQuery query,
        CancellationToken ct = default);

    Task<List<string>> GetDistinctEventTypesAsync(CancellationToken ct = default);
    Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default);
}
