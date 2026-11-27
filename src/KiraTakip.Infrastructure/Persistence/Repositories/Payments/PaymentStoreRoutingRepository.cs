using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Payments;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Payments;

public class PaymentStoreRoutingRepository(ApplicationDbContext context)
    : RepositoryBase<PaymentStoreRouting>(context), IPaymentStoreRoutingRepository
{
    public Task<PagedResult<PaymentStoreRoutingListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(routing =>
                routing.ChargeType.Name.Contains(search) ||
                routing.ChargeType.Code.Contains(search) ||
                routing.Store.Name.Contains(search) ||
                routing.Store.Code.Contains(search) ||
                (routing.Space != null && routing.Space.Name.Contains(search)));
        }

        if (string.Equals(tableQuery.Source, "General", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.SpaceId == null);
        else if (string.Equals(tableQuery.Source, "Space", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.SpaceId != null);

        query = string.Equals(tableQuery.Status, "gecmis", StringComparison.OrdinalIgnoreCase)
            ? query.Where(r => !r.IsActive)
            : query.Where(r => r.IsActive);

        var items = query
            .OrderByDescending(routing => routing.IsActive)
            .ThenBy(routing => routing.ChargeType.SortOrder)
            .ThenBy(routing => routing.SpaceId == null ? 0 : 1)
            .ThenBy(routing => routing.Space != null ? routing.Space.Path : string.Empty)
            .ThenBy(routing => routing.Id)
            .Select(routing => new PaymentStoreRoutingListItemDto
            {
                Id = routing.Id,
                ChargeTypeId = routing.ChargeTypeId,
                ChargeTypeName = routing.ChargeType.Name,
                ChargeTypeCode = routing.ChargeType.Code,
                ScopeName = routing.Space != null ? routing.Space.Name : "Tüm alanlar",
                SpaceId = routing.SpaceId,
                StoreId = routing.StoreId,
                StoreName = routing.Store.Name,
                StoreCode = routing.Store.Code,
                IsStoreActive = routing.Store.IsActive,
                HasActiveStoreAccount = routing.Store.Accounts.Count(account => account.IsActive) == 1,
                ProviderCode = routing.Store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.ProviderCode)
                    .FirstOrDefault(),
                Currency = routing.Store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.Currency)
                    .FirstOrDefault(),
                IsActive = routing.IsActive
            });

        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<int> GetHistoryCountAsync()
        => _dbSet.AsNoTracking().CountAsync(routing => !routing.IsActive);

    public Task<List<MissingDefaultRoutingDto>> GetMissingDefaultsAsync()
        => _ctx.ChargeTypes.AsNoTracking()
            .Where(chargeType => !_dbSet.Any(routing =>
                routing.ChargeTypeId == chargeType.Id &&
                routing.SpaceId == null &&
                routing.IsActive &&
                routing.Store.IsActive &&
                routing.Store.Accounts.Count(account => account.IsActive) == 1))
            .OrderBy(chargeType => chargeType.SortOrder)
            .ThenBy(chargeType => chargeType.Name)
            .Select(chargeType => new MissingDefaultRoutingDto(
                chargeType.Id,
                chargeType.Name,
                chargeType.Code,
                chargeType.IsActive))
            .ToListAsync();

    public Task<PaymentStoreRouting?> FindActiveAsync(
        int chargeTypeId,
        int? spaceId,
        bool tracking = true)
    {
        IQueryable<PaymentStoreRouting> query = _dbSet;
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(routing =>
            routing.ChargeTypeId == chargeTypeId &&
            routing.SpaceId == spaceId &&
            routing.IsActive);
    }

    public Task<PaymentStoreRouting?> GetTrackedByIdAsync(int id)
        => _dbSet.FirstOrDefaultAsync(routing => routing.Id == id);

    public Task<int?> GetDefaultStoreIdAsync(int chargeTypeId)
        => _dbSet.AsNoTracking()
            .Where(routing =>
                routing.ChargeTypeId == chargeTypeId &&
                routing.SpaceId == null &&
                routing.IsActive)
            .Select(routing => (int?)routing.StoreId)
            .FirstOrDefaultAsync();

    public Task<bool> HasUsableDefaultAsync(int chargeTypeId)
        => _dbSet.AsNoTracking().AnyAsync(routing =>
            routing.ChargeTypeId == chargeTypeId &&
            routing.SpaceId == null &&
            routing.IsActive &&
            routing.Store.IsActive &&
            routing.Store.Accounts.Count(account => account.IsActive) == 1);

    public async Task<PaymentRoutingResolutionCandidateDto?> GetResolutionCandidateAsync(
        int chargeTypeId,
        int? spaceId,
        CancellationToken cancellationToken = default)
    {
        var candidateSpaceIds = new List<int>();
        if (spaceId.HasValue)
        {
            var path = await _ctx.Spaces.AsNoTracking()
                .Where(item => item.Id == spaceId.Value)
                .Select(item => item.Path)
                .FirstOrDefaultAsync(cancellationToken);
            if (path == null) return null;

            candidateSpaceIds.AddRange(SpaceTreePolicy.GetAncestorIds(path));
            candidateSpaceIds.Add(spaceId.Value);
        }

        var routing = await _dbSet.AsNoTracking()
            .Where(item => item.IsActive && item.ChargeTypeId == chargeTypeId &&
                (item.SpaceId == null || candidateSpaceIds.Contains(item.SpaceId.Value)))
            .OrderByDescending(item => item.SpaceId == null ? -1 : item.Space!.Depth)
            .Select(item => new PaymentRoutingResolutionCandidateDto
            {
                SpaceId = spaceId,
                RoutingId = item.Id,
                MatchedSpaceId = item.SpaceId,
                StoreId = item.StoreId,
                IsStoreActive = item.Store.IsActive,
                ActiveAccountCount = item.Store.Accounts.Count(account => account.IsActive),
                StoreAccountId = item.Store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => (int?)account.Id)
                    .FirstOrDefault(),
                ProviderCode = item.Store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.ProviderCode)
                    .FirstOrDefault(),
                Currency = item.Store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.Currency)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return routing ?? new PaymentRoutingResolutionCandidateDto { SpaceId = spaceId };
    }

    public Task<bool> HasActiveRoutingForStoreAsync(int storeId)
        => _dbSet.AsNoTracking().AnyAsync(routing => routing.StoreId == storeId && routing.IsActive);
}
