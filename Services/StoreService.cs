using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class StoreService(
    IStoreRepository storeRepository,
    IStoreAccountRepository storeAccountRepository,
    IStoreBusinessRules businessRules,
    IStoreAccountCredentialProtector credentialProtector,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IStoreService, ITransactionalService
{
    public Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery query)
        => storeRepository.GetPagedListAsync(query);

    public Task<StoreDetailDto?> GetDetailAsync(int id)
        => storeRepository.GetDetailAsync(id);

    public async Task<int> CreateAsync(CreateStoreInput input)
    {
        var name = input.Name.Trim();
        var code = CodeSlugger.ToCode(name);
        await businessRules.EnsureCodeAvailableAsync(code);

        var store = new Store
        {
            Name = name,
            Code = code,
            Description = NormalizeOptional(input.Description),
            IsActive = input.IsActive
        };

        await storeRepository.AddAsync(store);
        await SaveWithUniqueConflictAsync("Bu mağaza adı zaten kullanılıyor.", "STORE_CODE_EXISTS");
        return store.Id;
    }

    public async Task UpdateAsync(int id, UpdateStoreInput input)
    {
        var store = await businessRules.GetStoreAsync(id);
        var name = input.Name.Trim();
        var code = CodeSlugger.ToCode(name);
        await businessRules.EnsureCodeAvailableAsync(code, id);

        store.Name = name;
        store.Code = code;
        store.Description = NormalizeOptional(input.Description);
        store.IsActive = input.IsActive;

        await SaveWithUniqueConflictAsync("Bu mağaza adı zaten kullanılıyor.", "STORE_CODE_EXISTS");
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var store = await businessRules.GetStoreAsync(id);
        store.IsActive = !store.IsActive;
        await unitOfWork.SaveChangesAsync();
        return store.IsActive;
    }

    public async Task ReplaceAccountAsync(CreateStoreAccountVersionInput input)
    {
        await businessRules.GetStoreAsync(input.StoreId);
        businessRules.EnsureProviderAndCurrencySupported(input.ProviderCode, input.Currency);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var activeAccount = await storeAccountRepository.GetActiveByStoreIdAsync(input.StoreId);
        if (activeAccount != null)
        {
            activeAccount.IsActive = false;
            activeAccount.ValidUntil = now;
            await unitOfWork.SaveChangesAsync();
        }

        var providerCode = PaymentProviderCodes.Supported.Single(code =>
            code.Equals(input.ProviderCode, StringComparison.OrdinalIgnoreCase));
        var currency = CurrencyCodes.Supported.Single(code =>
            code.Equals(input.Currency, StringComparison.OrdinalIgnoreCase));

        var account = new StoreAccount
        {
            StoreId = input.StoreId,
            ProviderCode = providerCode,
            Currency = currency,
            MerchantId = input.MerchantId.Trim(),
            MerchantUser = input.MerchantUser.Trim(),
            ProtectedMerchantPassword = credentialProtector.Protect(input.MerchantPassword),
            ValidFrom = now,
            IsActive = true
        };

        await storeAccountRepository.AddAsync(account);
        await SaveWithUniqueConflictAsync(
            "Bu mağazaya aynı anda yalnız bir aktif hesap tanımlanabilir.",
            "STORE_ACTIVE_ACCOUNT_EXISTS");
    }

    public async Task DeactivateAccountAsync(int storeId, int accountId)
    {
        var account = await businessRules.GetActiveAccountAsync(storeId, accountId);
        account.IsActive = false;
        account.ValidUntil = timeProvider.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync();
    }

    private async Task SaveWithUniqueConflictAsync(string message, string code)
    {
        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException(message, ErrorType.Conflict, code);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
