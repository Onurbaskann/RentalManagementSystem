using KiraTakip.Data;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class StoreAccountRepository(ApplicationDbContext context)
    : RepositoryBase<StoreAccount>(context), IStoreAccountRepository
{
    public Task<StoreAccount?> GetActiveByStoreIdAsync(int storeId, bool tracking = true)
    {
        IQueryable<StoreAccount> query = _dbSet;
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(account => account.StoreId == storeId && account.IsActive);
    }

    public Task<List<StoreAccountHistoryItemDto>> GetHistoryByStoreIdAsync(int storeId)
        => _dbSet.AsNoTracking()
            .Where(account => account.StoreId == storeId)
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
            .ToListAsync();
}
