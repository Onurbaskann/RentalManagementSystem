using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

/// <summary>
/// Faz 20 İç Faz 3 sonrası `PaymentAllocation.ChargeLineItemId`/`StoreAccountId` zorunlu FK
/// alanlarını dolduramayan eski test seed'lerinin ihtiyaç duyduğu asgari mağaza/hesap ve
/// tahakkuk kalemi kaydını oluşturur. Testin kendisi mağaza yönlendirmesini incelemiyorsa
/// bu yardımcı yeterlidir.
/// </summary>
internal static class PaymentLineItemTestHelper
{
    public static async Task<int> CreateStoreAccountAsync(ApplicationDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var store = new Store
        {
            Name = $"Test Mağaza {suffix}",
            Code = $"TSTORE-{suffix}",
            IsActive = true
        };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MERCHANT-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        return store.Accounts.Single().Id;
    }

    public static async Task<ChargeLineItem> AddLineItemAsync(
        ApplicationDbContext context,
        Charge charge,
        decimal? totalAmount = null)
    {
        var chargeType = await context.ChargeTypes.FirstAsync();
        var lineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Test kalemi",
            Amount = totalAmount ?? charge.TotalAmount,
            TotalAmount = totalAmount ?? charge.TotalAmount
        };
        context.ChargeLineItems.Add(lineItem);
        await context.SaveChangesAsync();
        return lineItem;
    }
}
