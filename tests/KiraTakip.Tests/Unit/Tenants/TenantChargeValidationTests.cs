using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Http;

namespace KiraTakip.Tests;

public class TenantChargeValidationTests
{
    [Fact]
    public void Query_InvalidValues_ReturnsAllRelevantErrors()
    {
        var result = new TenantChargeQueryViewModelValidator().Validate(new TenantChargeQueryViewModel
        {
            Page = 0,
            Size = 201,
            UnitId = 0,
            Year = 1999,
            Status = "bilinmeyen",
            Source = "bilinmeyen"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.Page));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.Size));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.UnitId));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.Year));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.Status));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargeQueryViewModel.Source));
    }

    [Fact]
    public void Payment_ValidInput_IsAccepted()
    {
        var receipt = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "Receipt", "dekont.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        var input = new TenantChargePaymentFormViewModel
        {
            ChargeId = 1,
            ChargeLineItemId = 5,
            Amount = 100m,
            PaymentDate = new DateTime(2026, 7, 1),
            PaymentChannel = PaymentChannel.Eft,
            Receipt = receipt
        };

        var result = new TenantChargePaymentFormViewModelValidator().Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Payment_MissingReceiptAndInvalidScalars_ReturnsErrors()
    {
        var result = new TenantChargePaymentFormViewModelValidator().Validate(
            new TenantChargePaymentFormViewModel
            {
                ChargeId = 0,
                Amount = 0,
                PaymentDate = default,
                PaymentChannel = (PaymentChannel)999
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.ChargeId));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.ChargeLineItemId));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.Amount));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.PaymentDate));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.PaymentChannel));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.Receipt));
    }

    [Fact]
    public void Payment_MissingLineItem_IsRejected()
    {
        var receipt = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "Receipt", "dekont.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        var result = new TenantChargePaymentFormViewModelValidator().Validate(
            new TenantChargePaymentFormViewModel
            {
                ChargeId = 1,
                ChargeLineItemId = null,
                Amount = 100m,
                PaymentDate = new DateTime(2026, 7, 1),
                PaymentChannel = PaymentChannel.Eft,
                Receipt = receipt
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.ChargeLineItemId));
    }

    [Fact]
    public void Payment_ZeroLineItem_IsRejected()
    {
        var receipt = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "Receipt", "dekont.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        var result = new TenantChargePaymentFormViewModelValidator().Validate(
            new TenantChargePaymentFormViewModel
            {
                ChargeId = 1,
                ChargeLineItemId = 0,
                Amount = 100m,
                PaymentDate = new DateTime(2026, 7, 1),
                PaymentChannel = PaymentChannel.Eft,
                Receipt = receipt
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantChargePaymentFormViewModel.ChargeLineItemId));
    }
}
