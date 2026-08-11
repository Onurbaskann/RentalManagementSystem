using KiraTakip.Data;
using KiraTakip.Models.Dtos.AuditLog;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace KiraTakip.Services;

public class AuditService(
    IHttpContextAccessor httpContextAccessor,
    IAuditLogRepository auditLogRepository,
    IApplicationUserRepository applicationUserRepository,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager) : IAuditService
{
    public async Task LogAsync(string eventType, string? entityType = null, string? entityId = null, string? details = null)
    {
        var httpContext = httpContextAccessor.HttpContext;

        await auditLogRepository.AddAsync(new AuditLog
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier),
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers.UserAgent.ToString() is { Length: > 0 } ua
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
            var user = await userManager.FindByEmailAsync(input.UserEmail);
            if (user != null)
            {
                userId = user.Id;
            }
            else
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
            IpAddress = r.IpAddress,
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
