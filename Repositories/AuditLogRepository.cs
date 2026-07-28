using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _ctx;

    public AuditLogRepository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
        => await _ctx.AuditLogs.AddAsync(log, ct);

    public async Task<(List<AuditLog> Rows, int TotalCount)> QueryAsync(
        string? eventType,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        int page,
        int pageSize,
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

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (rows, totalCount);
    }

    public async Task<List<string>> GetDistinctEventTypesAsync(CancellationToken ct = default)
        => await _ctx.AuditLogs.Select(a => a.EventType).Distinct().OrderBy(e => e).ToListAsync(ct);

    public async Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken ct = default)
        => await _ctx.AuditLogs.Where(a => a.EntityType != null).Select(a => a.EntityType!).Distinct().OrderBy(e => e).ToListAsync(ct);
}
