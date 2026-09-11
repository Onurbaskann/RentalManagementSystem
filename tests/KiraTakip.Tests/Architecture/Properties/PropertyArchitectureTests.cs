using KiraTakip.Web.Controllers;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace KiraTakip.Tests;

public class PropertyArchitectureTests
{
    [Theory]
    [InlineData(nameof(PropertyController.Create))]
    [InlineData(nameof(PropertyController.Edit))]
    public void PropertyWriteEndpoint_ShouldAllowLargePricingAndUnitForms(string actionName)
    {
        var action = typeof(PropertyController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method =>
                method.Name == actionName &&
                method.GetCustomAttribute<HttpPostAttribute>() != null);

        var formLimits = action.GetCustomAttribute<RequestFormLimitsAttribute>();

        Assert.NotNull(formLimits);
        Assert.Equal(10_000, formLimits.ValueCountLimit);
    }

    [Fact]
    public void CreateValidator_ShouldNotMutateSingleUnitCollections()
    {
        var viewModel = ValidCreate();
        viewModel.UnitStructure = UnitStructure.SingleUnit;
        viewModel.SingleUnitTypeId = 1;
        viewModel.Units.Add(new PropertyUnitInputViewModel { UnitNo = "101" });

        _ = new CreatePropertyViewModelValidator().Validate(viewModel);

        Assert.Single(viewModel.Units);
    }

    [Fact]
    public void CreateValidator_ShouldRejectDuplicateNumbersAcrossUnitSections()
    {
        var viewModel = ValidCreate();
        viewModel.UnitStructure = UnitStructure.MultipleUnits;
        viewModel.Units.Add(new PropertyUnitInputViewModel
        {
            UnitNo = "A-1",
            FloorNo = 1,
            Name = "Ofis",
            Area = 10,
            UnitTypeId = 1
        });
        viewModel.ReservationAreas.Add(new ReservationAreaInputViewModel
        {
            UnitNo = "a-1",
            Name = "Salon",
            Area = 20,
            UnitTypeId = 2
        });

        var result = new CreatePropertyViewModelValidator().Validate(viewModel);

        Assert.Contains(result.Errors, error => error.Message.Contains("tekrar kullanılamaz"));
    }

    [Fact]
    public void EditValidator_ShouldRejectVatAndNegativePricingValues()
    {
        var viewModel = new EditPropertyViewModel
        {
            Id = 1,
            Name = "Taşınmaz",
            PropertyTypeId = 1,
            UnitStructure = UnitStructure.SingleUnit,
            SingleUnitTypeId = 1,
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Merkez",
            Address = "Adres",
            PricingMatrix = new PropertyPricingMatrixViewModel
            {
                Rows =
                [
                    new TenantCategoryPricingRowViewModel
                    {
                        TenantCategoryId = 1,
                        Cells =
                        [
                            new PropertyPricingCellViewModel
                            {
                                TenantCategoryId = 1,
                                ChargeTypeId = 1,
                                UnitValue = -1,
                                VatRate = 101
                            }
                        ]
                    }
                ]
            }
        };

        var result = new EditPropertyViewModelValidator().Validate(viewModel);

        Assert.Contains(result.Errors, error => error.Field?.EndsWith("UnitValue") == true);
        Assert.Contains(result.Errors, error => error.Field?.EndsWith("VatRate") == true);
    }

    [Theory]
    [InlineData(0, 1, 10, "PropertyId")]
    [InlineData(1, 0, 10, "Page")]
    [InlineData(1, 1, 0, "PageSize")]
    [InlineData(1, 1, 101, "PageSize")]
    public void PricingQueryValidator_ShouldRejectInvalidRouteAndPagination(
        int propertyId,
        int page,
        int pageSize,
        string expectedField)
    {
        var result = new PropertyPricingQueryViewModelValidator().Validate(
            new PropertyPricingQueryViewModel
            {
                PropertyId = propertyId,
                Page = page,
                PageSize = pageSize
            });

        Assert.Contains(result.Errors, error => error.Field == expectedField);
    }

    private static CreatePropertyViewModel ValidCreate()
        => new()
        {
            Name = "Taşınmaz",
            PropertyTypeId = 1,
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Merkez",
            Address = "Adres"
        };
}
