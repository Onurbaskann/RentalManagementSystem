using KiraTakip.Data;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class StoreRepository(ApplicationDbContext context) : RepositoryBase<Store>(context), IStoreRepository
{
    public Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(store => store.Name.Contains(search) || store.Code.Contains(search));
        }

        var items = query
            .OrderBy(store => store.Name)
            .ThenBy(store => store.Id)
            .Select(store => new StoreListItemDto
            {
                Id = store.Id,
                Code = store.Code,
                Name = store.Name,
                Description = store.Description,
                IsActive = store.IsActive,
                HasActiveAccount = store.Accounts.Any(account => account.IsActive),
                ActiveProviderCode = store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.ProviderCode)
                    .FirstOrDefault(),
                ActiveCurrency = store.Accounts
                    .Where(account => account.IsActive)
                    .Select(account => account.Currency)
                    .FirstOrDefault()
            });

        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<StoreDetailDto?> GetDetailAsync(int id)
        => _dbSet.AsNoTracking()
            .Where(store => store.Id == id)
            .Select(store => new StoreDetailDto
            {
                Id = store.Id,
                Code = store.Code,
                Name = store.Name,
                Description = store.Description,
                IsActive = store.IsActive,
                Accounts = store.Accounts
                    .OrderByDescending(account => account.ValidFrom)
                    .ThenByDescending(account => account.Id)
                    .Select(account => new StoreAccountHistoryItemDto
                    {
                        Id = account.Id,
                        ProviderCode = account.ProviderCode,
                        Currency = account.Currency,
                        MerchantId = account.MerchantId,
                        MerchantUser = account.MerchantUser,
                        ValidFrom = account.ValidFrom,
                        ValidUntil = account.ValidUntil,
                        IsActive = account.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        => _dbSet.AsNoTracking()
            .AnyAsync(store => store.Code == code && (excludeId == null || store.Id != excludeId));

    public Task<List<StoreRoutingOptionDto>> GetRoutingOptionsAsync()
        => _dbSet.AsNoTracking()
            .Where(store => store.IsActive && store.Accounts.Count(account => account.IsActive) == 1)
            .OrderBy(store => store.Name)
            .Select(store => new StoreRoutingOptionDto(
                store.Id,
                store.Name,
                store.Accounts.Where(account => account.IsActive).Select(account => account.ProviderCode).First(),
                store.Accounts.Where(account => account.IsActive).Select(account => account.Currency).First()))
            .ToListAsync();
}
