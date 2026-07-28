using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

/// <summary>
/// AuditLog, BaseEntity'den türemediği (long Id, soft-delete/audit alanları yok) için
/// IBaseRepository&lt;T&gt;'ye uymuyor; standalone bir repository olarak tanımlanır.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(List<AuditLog> Rows, int TotalCount)> QueryAsync(
        string? eventType,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<List<string>> GetDistinctEventTypesAsync(CancellationToken ct = default);
    Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default);
}
