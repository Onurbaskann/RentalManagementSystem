using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class StoreBusinessRules(
    IStoreRepository storeRepository,
    IStoreAccountRepository storeAccountRepository) : IStoreBusinessRules
{
    public async Task<Store> GetStoreAsync(int id)
        => Guard.NotFound(
            await storeRepository.GetByIdAsync(id),
            "Mağaza bulunamadı.",
            "STORE_NOT_FOUND");

    public async Task EnsureCodeAvailableAsync(string code, int? excludeId = null)
        => Guard.InvalidField(
            await storeRepository.CodeExistsAsync(code, excludeId),
            "Name",
            "Bu mağaza adı zaten kullanılıyor. Farklı bir ad girin.",
            "STORE_CODE_EXISTS");

    public void EnsureProviderAndCurrencySupported(string providerCode, string currency)
    {
        Guard.InvalidField(
            !PaymentProviderCodes.Supported.Contains(providerCode, StringComparer.OrdinalIgnoreCase),
            "ProviderCode",
            "Desteklenmeyen ödeme sağlayıcısı.",
            "STORE_ACCOUNT_PROVIDER_UNSUPPORTED");
        Guard.InvalidField(
            !CurrencyCodes.Supported.Contains(currency, StringComparer.OrdinalIgnoreCase),
            "Currency",
            "Desteklenmeyen para birimi.",
            "STORE_ACCOUNT_CURRENCY_UNSUPPORTED");
    }

    public async Task<StoreAccount> GetActiveAccountAsync(int storeId, int accountId)
    {
        await GetStoreAsync(storeId);
        var account = await storeAccountRepository.GetByIdAsync(accountId);
        account = Guard.NotFound(
            account != null && account.StoreId == storeId ? account : null,
            "Mağaza hesabı bulunamadı.",
            "STORE_ACCOUNT_NOT_FOUND");
        Guard.Conflict(
            !account.IsActive,
            "Mağaza hesabı zaten pasif.",
            "STORE_ACCOUNT_ALREADY_INACTIVE");
        return account;
    }
}
