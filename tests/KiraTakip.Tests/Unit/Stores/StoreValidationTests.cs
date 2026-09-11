using KiraTakip.Models.Constants;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;

namespace KiraTakip.Tests;

public class StoreValidationTests
{
    [Fact]
    public void StoreValidator_ShouldRejectBlankAndLongFields()
    {
        var validator = new StoreFormViewModelValidator();
        var result = validator.Validate(new StoreFormViewModel
        {
            Name = " ",
            Description = new string('x', 501)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreFormViewModel.Name));
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreFormViewModel.Description));
    }

    [Fact]
    public void AccountValidator_ShouldRejectUnsupportedProviderAndCurrency()
    {
        var validator = new StoreAccountFormViewModelValidator();
        var result = validator.Validate(new StoreAccountFormViewModel
        {
            ProviderCode = "Unknown",
            Currency = "USD",
            MerchantId = "merchant",
            MerchantUser = "user",
            MerchantPassword = "secret"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreAccountFormViewModel.ProviderCode));
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreAccountFormViewModel.Currency));
    }

    [Fact]
    public void AccountValidator_ShouldRejectMissingAndLongMerchantFieldsWithoutEchoingPassword()
    {
        var password = new string('S', 1001);
        var validator = new StoreAccountFormViewModelValidator();
        var result = validator.Validate(new StoreAccountFormViewModel
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = " ",
            MerchantUser = new string('u', 201),
            MerchantPassword = password
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreAccountFormViewModel.MerchantId));
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreAccountFormViewModel.MerchantUser));
        Assert.Contains(result.Errors, error => error.Field == nameof(StoreAccountFormViewModel.MerchantPassword));
        Assert.All(result.Errors, error => Assert.DoesNotContain(password, error.Message, StringComparison.Ordinal));
    }
}
