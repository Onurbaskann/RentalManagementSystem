using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

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
                (routing.Property != null && routing.Property.Name.Contains(search)) ||
                (routing.Unit != null && routing.Unit.Name.Contains(search)));
        }

        if (Enum.TryParse<PaymentRoutingScope>(tableQuery.Source, out var scope))
        {
            query = scope switch
            {
                PaymentRoutingScope.General => query.Where(r => r.PropertyId == null && r.UnitId == null),
                PaymentRoutingScope.Property => query.Where(r => r.PropertyId != null && r.UnitId == null),
                PaymentRoutingScope.Unit => query.Where(r => r.PropertyId == null && r.UnitId != null),
                _ => query
            };
        }

        query = string.Equals(tableQuery.Status, "gecmis", StringComparison.OrdinalIgnoreCase)
            ? query.Where(r => !r.IsActive)
            : query.Where(r => r.IsActive);

        var items = query
            .OrderByDescending(routing => routing.IsActive)
            .ThenBy(routing => routing.ChargeType.SortOrder)
            .ThenBy(routing => routing.PropertyId == null && routing.UnitId == null ? 0 : routing.PropertyId != null ? 1 : 2)
            .ThenBy(routing => routing.Property != null ? routing.Property.Name : routing.Unit != null ? routing.Unit.Property.Name : string.Empty)
            .ThenBy(routing => routing.Unit != null ? routing.Unit.Name : string.Empty)
            .ThenBy(routing => routing.Id)
            .Select(routing => new PaymentStoreRoutingListItemDto
            {
                Id = routing.Id,
                ChargeTypeId = routing.ChargeTypeId,
                ChargeTypeName = routing.ChargeType.Name,
                ChargeTypeCode = routing.ChargeType.Code,
                Scope = routing.PropertyId == null && routing.UnitId == null
                    ? PaymentRoutingScope.General
                    : routing.PropertyId != null
                        ? PaymentRoutingScope.Property
                        : PaymentRoutingScope.Unit,
                ScopeName = routing.PropertyId == null && routing.UnitId == null
                    ? "Tüm taşınmazlar"
                    : routing.Property != null
                        ? routing.Property.Name
                        : routing.Unit!.Property.Name + " / " + routing.Unit.Name,
                PropertyId = routing.PropertyId,
                UnitId = routing.UnitId,
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
                routing.PropertyId == null &&
                routing.UnitId == null &&
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
        int? propertyId,
        int? unitId,
        bool tracking = true)
    {
        IQueryable<PaymentStoreRouting> query = _dbSet;
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(routing =>
            routing.ChargeTypeId == chargeTypeId &&
            routing.PropertyId == propertyId &&
            routing.UnitId == unitId &&
            routing.IsActive);
    }

    public Task<PaymentStoreRouting?> GetTrackedByIdAsync(int id)
        => _dbSet.FirstOrDefaultAsync(routing => routing.Id == id);

    public Task<int?> GetDefaultStoreIdAsync(int chargeTypeId)
        => _dbSet.AsNoTracking()
            .Where(routing =>
                routing.ChargeTypeId == chargeTypeId &&
                routing.PropertyId == null &&
                routing.UnitId == null &&
                routing.IsActive)
            .Select(routing => (int?)routing.StoreId)
            .FirstOrDefaultAsync();

    public Task<bool> HasUsableDefaultAsync(int chargeTypeId)
        => _dbSet.AsNoTracking().AnyAsync(routing =>
            routing.ChargeTypeId == chargeTypeId &&
            routing.PropertyId == null &&
            routing.UnitId == null &&
            routing.IsActive &&
            routing.Store.IsActive &&
            routing.Store.Accounts.Count(account => account.IsActive) == 1);

    public async Task<PaymentRoutingResolutionCandidateDto?> GetResolutionCandidateAsync(
        int chargeTypeId,
        int unitId,
        CancellationToken cancellationToken = default)
    {
        var unit = await _ctx.Units.AsNoTracking()
            .Where(item => item.Id == unitId)
            .Select(item => new { item.Id, item.PropertyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (unit == null) return null;

        var routing = await _dbSet.AsNoTracking()
            .Where(item => item.IsActive && item.ChargeTypeId == chargeTypeId &&
                (item.UnitId == unitId ||
                 (item.UnitId == null && item.PropertyId == unit.PropertyId) ||
                 (item.UnitId == null && item.PropertyId == null)))
            .OrderBy(item => item.UnitId == unitId ? 0 : item.PropertyId == unit.PropertyId ? 1 : 2)
            .Select(item => new PaymentRoutingResolutionCandidateDto
            {
                UnitId = unit.Id,
                PropertyId = unit.PropertyId,
                RoutingId = item.Id,
                MatchedScope = item.UnitId != null
                    ? PaymentRoutingScope.Unit
                    : item.PropertyId != null
                        ? PaymentRoutingScope.Property
                        : PaymentRoutingScope.General,
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

        return routing ?? new PaymentRoutingResolutionCandidateDto
        {
            UnitId = unit.Id,
            PropertyId = unit.PropertyId
        };
    }

    public Task<bool> HasActiveRoutingForStoreAsync(int storeId)
        => _dbSet.AsNoTracking().AnyAsync(routing => routing.StoreId == storeId && routing.IsActive);
}
