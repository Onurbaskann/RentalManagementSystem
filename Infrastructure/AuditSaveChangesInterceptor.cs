using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace KiraTakip.Infrastructure;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IMaskingService _maskingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

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
        ["Email"]       = MaskType.Email,
        ["UserName"]    = MaskType.Email,
        ["PhoneNumber"] = MaskType.Telefon,
    };

    public AuditSaveChangesInterceptor(IMaskingService maskingService, IHttpContextAccessor httpContextAccessor)
    {
        _maskingService = maskingService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is ApplicationDbContext ctx)
            AddAuditEntries(ctx);
        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private void AddAuditEntries(ApplicationDbContext ctx)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var now = DateTime.UtcNow;

        // Tracks IAuditable entities only; AuditLog itself does not implement IAuditable → no recursion
        var entries = ctx.ChangeTracker.Entries<Models.Entities.Interfaces.IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var typeName = entry.Entity.GetType().Name;
            var action = entry.State.ToString(); // Added / Modified / Deleted
            string? entityId = entry.State == EntityState.Added
                ? null
                : entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

            var changes = BuildChanges(entry, action);
            if (changes.Count == 0 && action == "Modified") continue;

            var details = JsonSerializer.Serialize(new { action, changes });

            ctx.AuditLogs.Add(new AuditLog
            {
                EventType = $"Entity.{action}",
                EntityType = typeName,
                EntityId = entityId,
                UserId = userId,
                IpAddress = ip,
                Details = details,
                CreatedAt = now
            });
        }
    }

    private List<object> BuildChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string action)
    {
        var result = new List<object>();
        var type = entry.Entity.GetType();

        foreach (var prop in entry.Properties)
        {
            var propName = prop.Metadata.Name;
            if (AlwaysIgnore.Contains(propName)) continue;

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
}
