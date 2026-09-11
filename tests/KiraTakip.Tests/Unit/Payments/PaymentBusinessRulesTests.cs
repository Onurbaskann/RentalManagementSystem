using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Payment;
using KiraTakip.Services.Payments;

namespace KiraTakip.Tests;

public class PaymentBusinessRulesTests
{
    private static ChargeLineItemPaymentBalanceDto CreateBalance(
        int chargeLineItemId = 1,
        int chargeId = 10,
        decimal totalAmount = 1000m,
        decimal approvedAmount = 0m,
        decimal pendingAmount = 0m)
        => new(
            chargeLineItemId,
            chargeId,
            ChargeTypeId: 5,
            UnitId: 7,
            TenantId: 3,
            ChargeTypeName: "Kira Bedeli",
            Description: "Test kalemi",
            totalAmount,
            approvedAmount,
            pendingAmount);

    [Fact]
    public void EnsureAdminAmountWithinAvailable_ShouldRejectNonPositiveAmount()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance();

        var exception = Assert.Throws<BusinessValidationException>(() =>
            rules.EnsureAdminAmountWithinAvailable(balance, 0m));

        Assert.Equal("PAYMENT_AMOUNT_NOT_POSITIVE", exception.Code);
    }

    [Fact]
    public void EnsureAdminAmountWithinAvailable_ShouldRejectAmountAbovePendingAdjustedAvailable()
    {
        var rules = new PaymentBusinessRules();
        // Total 1000, onaylı 0, bekleyen 700 → kullanılabilir 300.
        var balance = CreateBalance(pendingAmount: 700m);

        var exception = Assert.Throws<BusinessValidationException>(() =>
            rules.EnsureAdminAmountWithinAvailable(balance, 400m));

        Assert.Equal("PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE", exception.Code);
    }

    [Fact]
    public void EnsureAdminAmountWithinAvailable_ShouldAcceptAmountWithinAvailable()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(pendingAmount: 700m);

        var exception = Record.Exception(() => rules.EnsureAdminAmountWithinAvailable(balance, 300m));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureTenantAmountWithinAvailable_ShouldKeepLegacyErrorCode()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(pendingAmount: 700m);

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureTenantAmountWithinAvailable(balance, 300.01m));

        Assert.Equal("TENANT_PAYMENT_AMOUNT_EXCEEDS_AVAILABLE", exception.Code);
    }

    [Fact]
    public void EnsureApprovalWithinRemaining_ShouldIgnorePendingAmounts()
    {
        var rules = new PaymentBusinessRules();
        // Onaylı 600, bekleyen 300 (görmezden gelinmeli) — 600+300 (bu onay) = 900 <= 1000 geçmeli.
        var balance = CreateBalance(approvedAmount: 600m, pendingAmount: 300m);

        var exception = Record.Exception(() => rules.EnsureApprovalWithinRemaining(balance, 300m));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureApprovalWithinRemaining_ShouldRejectWhenApprovedTotalExceedsLineItemTotal()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(approvedAmount: 700m);

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureApprovalWithinRemaining(balance, 400m));

        Assert.Equal("PAYMENT_APPROVAL_EXCEEDS_LINE_ITEM_REMAINING", exception.Code);
    }

    [Fact]
    public void EnsureLineItemBelongsToCharge_ShouldRejectForeignLineItem()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(chargeId: 10);

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureLineItemBelongsToCharge(balance, chargeId: 99));

        Assert.Equal("PAYMENT_LINE_ITEM_CHARGE_MISMATCH", exception.Code);
    }

    [Fact]
    public void EnsureLineItemPayable_ShouldRejectFullyPaidLineItem()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(approvedAmount: 1000m);

        var exception = Assert.Throws<BusinessException>(() => rules.EnsureLineItemPayable(balance));

        Assert.Equal("PAYMENT_LINE_ITEM_FULLY_PAID", exception.Code);
    }

    [Fact]
    public void EnsureLineItemPayable_ShouldRejectWhenPendingCoversRemaining()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance(approvedAmount: 200m, pendingAmount: 800m);

        var exception = Assert.Throws<BusinessException>(() => rules.EnsureLineItemPayable(balance));

        Assert.Equal("PAYMENT_LINE_ITEM_NO_AVAILABLE_AMOUNT", exception.Code);
    }

    [Fact]
    public void ResolveAutoSelectedLineItem_ShouldReturnSingleItemWhenOnlyOnePayable()
    {
        var rules = new PaymentBusinessRules();
        var balance = CreateBalance();

        var resolved = rules.ResolveAutoSelectedLineItem([balance]);

        Assert.Equal(balance.ChargeLineItemId, resolved.ChargeLineItemId);
    }

    [Fact]
    public void ResolveAutoSelectedLineItem_ShouldThrowWhenMultiplePayableItemsExist()
    {
        var rules = new PaymentBusinessRules();
        var first = CreateBalance(chargeLineItemId: 1);
        var second = CreateBalance(chargeLineItemId: 2);

        var exception = Assert.Throws<BusinessException>(() =>
            rules.ResolveAutoSelectedLineItem([first, second]));

        Assert.Equal("PAYMENT_LINE_ITEM_SELECTION_REQUIRED", exception.Code);
    }

    [Fact]
    public void ResolveAutoSelectedLineItem_ShouldThrowWhenNoPayableItemExists()
    {
        var rules = new PaymentBusinessRules();

        var exception = Assert.Throws<BusinessException>(() =>
            rules.ResolveAutoSelectedLineItem([]));

        Assert.Equal("PAYMENT_LINE_ITEM_FULLY_PAID", exception.Code);
    }
}
