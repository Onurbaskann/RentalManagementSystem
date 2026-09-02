using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PaymentBusinessRules : IPaymentBusinessRules
{
    public void EnsureLineItemBelongsToCharge(ChargeLineItemPaymentBalanceDto balance, int chargeId)
    {
        Guard.Conflict(
            balance.ChargeId != chargeId,
            "Seçilen kalem bu tahakkuka ait değil.",
            "PAYMENT_LINE_ITEM_CHARGE_MISMATCH");
    }

    public void EnsureLineItemPayable(ChargeLineItemPaymentBalanceDto balance)
    {
        Guard.Conflict(
            balance.RemainingAmount <= 0,
            "Bu tahakkuk kaleminin kalan borcu bulunmuyor.",
            "PAYMENT_LINE_ITEM_FULLY_PAID");
        Guard.Conflict(
            balance.AvailableAmount <= 0,
            "Bu kalem için onay bekleyen ödemeler kalan borcun tamamını karşılıyor.",
            "PAYMENT_LINE_ITEM_NO_AVAILABLE_AMOUNT");
    }

    public void EnsureAdminAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount)
    {
        Guard.InvalidField(
            amount <= 0,
            "Amount",
            "Tutar 0'dan büyük olmalıdır.",
            "PAYMENT_AMOUNT_NOT_POSITIVE");
        Guard.InvalidField(
            amount > balance.AvailableAmount,
            "Amount",
            $"Tutar, kalemin kullanılabilir kalan tutarından ({balance.AvailableAmount:N2} ₺) küçük veya eşit olmalıdır.",
            "PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE");
    }

    public void EnsureTenantAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount)
    {
        Guard.Against(
            amount <= 0 || amount > balance.AvailableAmount,
            $"Tutar 0'dan büyük ve kalan borçtan ({Math.Max(0, balance.AvailableAmount):N2} ₺) küçük/eşit olmalıdır.",
            "TENANT_PAYMENT_AMOUNT_EXCEEDS_AVAILABLE");
    }

    public void EnsureApprovalWithinRemaining(ChargeLineItemPaymentBalanceDto balance, decimal amount)
    {
        Guard.Conflict(
            balance.ApprovedAmount + amount > balance.TotalAmount,
            "Ödeme tutarı tahakkuk kaleminin kalan borcunu aşıyor.",
            "PAYMENT_APPROVAL_EXCEEDS_LINE_ITEM_REMAINING");
    }

    public ChargeLineItemPaymentBalanceDto ResolveAutoSelectedLineItem(
        IReadOnlyList<ChargeLineItemPaymentBalanceDto> payableLineItems)
    {
        Guard.Conflict(
            payableLineItems.Count == 0,
            "Bu tahakkuk kaleminin kalan borcu bulunmuyor.",
            "PAYMENT_LINE_ITEM_FULLY_PAID");
        Guard.Conflict(
            payableLineItems.Count > 1,
            "Bu tahakkukta birden fazla ödenebilir kalem var; ödeme yapılacak kalem seçilmelidir.",
            "PAYMENT_LINE_ITEM_SELECTION_REQUIRED");

        return payableLineItems[0];
    }
}
