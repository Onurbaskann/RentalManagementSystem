using KiraTakip.Auditing;
using KiraTakip.Data;
using KiraTakip.Models.Dtos.AuditLog;
using KiraTakip.Repositories.Interfaces.Auditing;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Services.Auditing;

public class AuditService(
    IAuditContext auditContext,
    IAuditLogRepository auditLogRepository,
    IApplicationUserRepository applicationUserRepository,
    IUnitOfWork unitOfWork,
    IApplicationUserManager userManager) : IAuditService
{
    public async Task LogAsync(string eventType, string? entityType = null, string? entityId = null, string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (!AuditEventTypes.IsDefined(eventType))
            throw new ArgumentException($"Tanımsız audit olay tipi: {eventType}", nameof(eventType));
        if (entityType is not null && !AuditEntityTypes.IsDefined(entityType))
            throw new ArgumentException($"Tanımsız audit varlık tipi: {entityType}", nameof(entityType));
        if (details is not null)
            AuditDetails.EnsureStructured(details);

        await auditLogRepository.AddAsync(new AuditLog
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = auditContext.UserId,
            UserType = auditContext.UserType,
            KiraciId = auditContext.TenantId,
            IpAddress = auditContext.IpAddress,
            UserAgent = auditContext.UserAgent is { Length: > 0 } ua
                ? ua[..Math.Min(ua.Length, 500)]
                : null,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<QueryResult> QueryAsync(QueryInput input, CancellationToken ct = default)
    {
        string? userId = null;
        string? userNotFoundMessage = null;
        var noResults = false;

        if (!string.IsNullOrWhiteSpace(input.UserEmail))
        {
            var normalizedEmail = userManager.NormalizeEmail(input.UserEmail.Trim());
            userId = await applicationUserRepository.FindIdByNormalizedEmailForAuditAsync(normalizedEmail, ct);
            if (userId is null)
            {
                noResults = true;
                userNotFoundMessage = $"\"{input.UserEmail}\" adresine sahip bir kullanıcı bulunamadı.";
            }
        }

        var availableEventTypes = await auditLogRepository.GetDistinctEventTypesAsync(ct);
        var availableEntityTypes = await auditLogRepository.GetDistinctEntityTypesAsync(ct);

        if (noResults)
        {
            return new QueryResult
            {
                Records = new PagedResult<RowResult>
                {
                    Items = [],
                    Total = 0,
                    Page = Math.Max(1, input.Query.Page),
                    Size = input.Query.SafeSize
                },
                AvailableEventTypes = availableEventTypes,
                AvailableEntityTypes = availableEntityTypes,
                UserNotFoundMessage = userNotFoundMessage
            };
        }

        var page = await auditLogRepository.QueryAsync(
            input.EventType,
            input.EntityType,
            input.Query.From,
            input.Query.To,
            userId,
            input.Query,
            ct);

        var userIds = page.Items.Where(r => r.UserId != null).Select(r => r.UserId!).Distinct().ToList();
        var userMap = await applicationUserRepository.GetDisplayNamesAsync(userIds, ct);

        var resultRows = page.Items.Select(r => new RowResult
        {
            Id = r.Id,
            EventType = r.EventType,
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            UserFullName = r.UserId != null && userMap.TryGetValue(r.UserId, out var u)
                ? (u ?? r.UserId)
                : r.UserId,
            UserType = r.UserType,
            TenantId = r.KiraciId,
            IpAddress = r.IpAddress,
            UserAgent = r.UserAgent,
            Details = r.Details,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new QueryResult
        {
            Records = new PagedResult<RowResult>
            {
                Items = resultRows,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size
            },
            AvailableEventTypes = availableEventTypes,
            AvailableEntityTypes = availableEntityTypes,
            UserNotFoundMessage = userNotFoundMessage
        };
    }
}
