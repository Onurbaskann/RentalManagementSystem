using KiraTakip.Data;
using KiraTakip.Auditing;
using KiraTakip.Services.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using KiraTakip.Domain.Auditing;

namespace KiraTakip.Infrastructure.Auditing;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IMaskingService _maskingService;
    private readonly IAuditContext _auditContext;
    private readonly List<PendingAuditEntry> _pendingAuditEntries = [];
    private bool _isFinalizingAuditIds;

    // IdentityUser properties that can't have attributes — handled here
    private static readonly HashSet<string> AlwaysIgnore =
    [
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "NormalizedUserName", "NormalizedEmail", "LockoutEnd",
        "AccessFailedCount", "LockoutEnabled", "TwoFactorEnabled",
        "PhoneNumberConfirmed", "EmailConfirmed"
    ];

    private static readonly Dictionary<string, MaskType> InheritedMasks = new()
    {
        ["Email"] = MaskType.Email,
        ["UserName"] = MaskType.Email,
        ["PhoneNumber"] = MaskType.Telefon,
    };

    public AuditSaveChangesInterceptor(IMaskingService maskingService, IAuditContext auditContext)
    {
        _maskingService = maskingService;
        _auditContext = auditContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (!_isFinalizingAuditIds && eventData.Context is ApplicationDbContext ctx)
            AddAuditEntries(ctx);

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (!_isFinalizingAuditIds && eventData.Context is ApplicationDbContext ctx)
            AddAuditEntries(ctx);
        return await base.SavingChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (!_isFinalizingAuditIds && eventData.Context is ApplicationDbContext ctx)
            FinalizeAuditEntityIds(ctx);

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (!_isFinalizingAuditIds && eventData.Context is ApplicationDbContext ctx)
            await FinalizeAuditEntityIdsAsync(ctx, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendingAuditEntries.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pendingAuditEntries.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void AddAuditEntries(ApplicationDbContext ctx)
    {
        _pendingAuditEntries.Clear();
        var now = DateTime.UtcNow;

        // Tracks IAuditable entities only; AuditLog itself does not implement IAuditable → no recursion
        var entries = ctx.ChangeTracker.Entries<Models.Entities.Interfaces.IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Metadata.ClrType
                .GetCustomAttributes(typeof(AuditExcludeAttribute), inherit: true).Length == 0)
            .ToList();

        foreach (var entry in entries)
        {
            var typeName = entry.Metadata.ClrType.Name;
            var action = entry.State.ToString(); // Added / Modified / Deleted
            var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            string? entityId = primaryKey?.CurrentValue?.ToString();

            if (entry.State == EntityState.Added && primaryKey?.IsTemporary == true)
                entityId = null;

            var changes = BuildChanges(entry, action);
            if (changes.Count == 0 && action == "Modified") continue;

            var details = JsonSerializer.Serialize(new { action, changes });

            var auditLog = new AuditLog
            {
                EventType = action switch
                {
                    "Added" => AuditEventTypes.EntityAdded,
                    "Modified" => AuditEventTypes.EntityModified,
                    "Deleted" => AuditEventTypes.EntityDeleted,
                    _ => throw new InvalidOperationException($"Desteklenmeyen audit işlemi: {action}")
                },
                EntityType = typeName,
                EntityId = entityId,
                UserId = _auditContext.UserId,
                UserType = _auditContext.UserType,
                KiraciId = _auditContext.TenantId,
                IpAddress = _auditContext.IpAddress,
                UserAgent = _auditContext.UserAgent is { Length: > 0 } userAgent
                    ? userAgent[..Math.Min(userAgent.Length, 500)]
                    : null,
                Details = details,
                CreatedAt = now
            };

            ctx.AuditLogs.Add(auditLog);

            if (entry.State == EntityState.Added && primaryKey?.IsTemporary == true)
                _pendingAuditEntries.Add(new PendingAuditEntry(primaryKey, auditLog));
        }
    }

    private void FinalizeAuditEntityIds(ApplicationDbContext ctx)
    {
        if (_pendingAuditEntries.Count == 0)
            return;

        try
        {
            _isFinalizingAuditIds = true;
            ApplyGeneratedEntityIds();
            ctx.SaveChanges();
        }
        finally
        {
            _pendingAuditEntries.Clear();
            _isFinalizingAuditIds = false;
        }
    }

    private async Task FinalizeAuditEntityIdsAsync(ApplicationDbContext ctx, CancellationToken cancellationToken)
    {
        if (_pendingAuditEntries.Count == 0)
            return;

        try
        {
            _isFinalizingAuditIds = true;
            ApplyGeneratedEntityIds();
            await ctx.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _pendingAuditEntries.Clear();
            _isFinalizingAuditIds = false;
        }
    }

    private void ApplyGeneratedEntityIds()
    {
        foreach (var pending in _pendingAuditEntries)
            pending.AuditLog.EntityId = pending.PrimaryKey.CurrentValue?.ToString();
    }

    private List<object> BuildChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string action)
    {
        var result = new List<object>();
        var type = entry.Metadata.ClrType;

        foreach (var prop in entry.Properties)
        {
            var propName = prop.Metadata.Name;
            if (AlwaysIgnore.Contains(propName)) continue;
            if (action == "Added" && prop.Metadata.IsPrimaryKey()) continue;

            var clrProp = type.GetProperty(propName);
            if (clrProp?.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).Length > 0) continue;

            if (action == "Modified" && !prop.IsModified) continue;

            var maskAttr = clrProp?.GetCustomAttributes(typeof(AuditMaskAttribute), true)
                                    .Cast<AuditMaskAttribute>()
                                    .FirstOrDefault();

            MaskType? maskType = maskAttr?.MaskType
                ?? (InheritedMasks.TryGetValue(propName, out var m) ? m : null);

            string? Serialize(object? val)
            {
                if (val == null) return null;
                var str = val.ToString();
                return maskType.HasValue ? _maskingService.Mask(str, maskType.Value) : str;
            }

            if (action == "Deleted")
                result.Add(new { prop = propName, old = Serialize(prop.OriginalValue) });
            else if (action == "Modified")
                result.Add(new { prop = propName, old = Serialize(prop.OriginalValue), @new = Serialize(prop.CurrentValue) });
            else // Added
                result.Add(new { prop = propName, @new = Serialize(prop.CurrentValue) });
        }

        return result;
    }

    private sealed record PendingAuditEntry(PropertyEntry PrimaryKey, AuditLog AuditLog);
}
