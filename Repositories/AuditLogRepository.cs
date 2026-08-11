using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class AuditLogRepository(ApplicationDbContext ctx)
    : Repository<AuditLog, long>(ctx, auditLog => auditLog.Id), IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
        => await _ctx.AuditLogs.AddAsync(log, ct);

    public async Task<PagedResult<AuditLog>> QueryAsync(
        string? eventType,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        TableQuery tableQuery,
        CancellationToken ct = default)
    {
        var query = _ctx.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(a => a.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value.ToUniversalTime());

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt < endDate.Value.AddDays(1).ToUniversalTime());

        if (userId != null)
            query = query.Where(a => a.UserId == userId);

        return await GetPagedResultAsync(
            query,
            query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id),
            tableQuery,
            ct);
    }

    public async Task<List<string>> GetDistinctEventTypesAsync(CancellationToken ct = default)
        => await _ctx.AuditLogs.Select(a => a.EventType).Distinct().OrderBy(e => e).ToListAsync(ct);

    public async Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default)
        => await _ctx.AuditLogs.Where(a => a.EntityType != null).Select(a => a.EntityType!).Distinct().OrderBy(e => e).ToListAsync(ct);
}
