using KiraTakip.Infrastructure.Exceptions;

namespace KiraTakip.Services.Interfaces;

public interface IStoreBusinessRules : IBusinessRules
{
    Task<Store> GetStoreAsync(int id);
    Task EnsureCodeAvailableAsync(string code, int? excludeId = null);
    void EnsureProviderAndCurrencySupported(string providerCode, string currency);
    Task<StoreAccount> GetActiveAccountAsync(int storeId, int accountId);
}
