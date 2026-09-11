using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;

namespace KiraTakip.Tests;

public class PaymentValidationTests
{
    [Fact]
    public void CreatePayment_ValidInput_IsAccepted()
    {
        var result = new CreatePaymentViewModelValidator().Validate(new CreatePaymentViewModel
        {
            ChargeId = 1,
            ChargeLineItemId = 5,
            Amount = 100m,
            PaymentDate = new DateTime(2026, 7, 1),
            PaymentChannel = PaymentChannel.Eft
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreatePayment_MissingLineItem_IsRejected()
    {
        var result = new CreatePaymentViewModelValidator().Validate(new CreatePaymentViewModel
        {
            ChargeId = 1,
            ChargeLineItemId = null,
            Amount = 100m,
            PaymentDate = new DateTime(2026, 7, 1),
            PaymentChannel = PaymentChannel.Eft
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.ChargeLineItemId));
    }

    [Fact]
    public void CreatePayment_ZeroLineItem_IsRejected()
    {
        var result = new CreatePaymentViewModelValidator().Validate(new CreatePaymentViewModel
        {
            ChargeId = 1,
            ChargeLineItemId = 0,
            Amount = 100m,
            PaymentDate = new DateTime(2026, 7, 1),
            PaymentChannel = PaymentChannel.Eft
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.ChargeLineItemId));
    }

    [Fact]
    public void CreatePayment_MissingChargeAndAmountAndLineItem_ReturnsAllErrors()
    {
        var result = new CreatePaymentViewModelValidator().Validate(new CreatePaymentViewModel
        {
            ChargeId = 0,
            ChargeLineItemId = null,
            Amount = 0,
            PaymentDate = default,
            PaymentChannel = (PaymentChannel)999
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.ChargeId));
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.ChargeLineItemId));
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.Amount));
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.PaymentDate));
        Assert.Contains(result.Errors, error => error.Field == nameof(CreatePaymentViewModel.PaymentChannel));
    }
}
