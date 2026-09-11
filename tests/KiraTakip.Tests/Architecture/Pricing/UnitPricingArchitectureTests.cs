using KiraTakip.Data;
using KiraTakip.Web.Controllers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reservations;
using KiraTakip.Web.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class UnitPricingArchitectureTests : IDisposable
{
    private const int PricingYear = 2026;
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public UnitPricingArchitectureTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public void UnitRateActions_ShouldBindUnitIdentifierOnlyFromRoute()
    {
        var actionNames = new[]
        {
            nameof(UnitController.Rates),
            nameof(UnitController.SaveReservationRule),
            nameof(UnitController.ClearReservationRule)
        };
        var actions = typeof(UnitController)
            .GetMethods()
            .Where(method => actionNames.Contains(method.Name));

        foreach (var action in actions)
        {
            var unitIdParameter = Assert.Single(
                action.GetParameters(),
                parameter => parameter.Name == "unitId");
            var fromRoute = Assert.Single(
                unitIdParameter.GetCustomAttributes(
                    typeof(Microsoft.AspNetCore.Mvc.FromRouteAttribute),
                    inherit: false)
                    .Cast<Microsoft.AspNetCore.Mvc.FromRouteAttribute>());

            Assert.Equal("id", fromRoute.Name);
        }
    }
    [Fact]
    public void Validator_ShouldRejectInvalidAndDuplicateActiveCells()
    {
        var viewModel = new UnitPricingFormViewModel
        {
            Rows =
            [
                new UnitRateCategoryRow
                {
                    TenantCategoryId = 1,
                    Cells =
                    [
                        new UnitRateCell
                        {
                            TenantCategoryId = 1,
                            ChargeTypeId = 2,
                            IsCustomRateActive = true,
                            CalculationMethod = (CalculationMethod)int.MaxValue,
                            UnitValue = -1,
                            KdvRate = 101
                        },
                        new UnitRateCell
                        {
                            TenantCategoryId = 1,
                            ChargeTypeId = 2
                        }
                    ]
                }
            ]
        };

        var result = new UnitPricingFormViewModelValidator().Validate(viewModel);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("yinelenen"));
        Assert.Contains(result.Errors, error => error.Field?.EndsWith(nameof(UnitRateCell.UnitValue)) == true);
        Assert.Contains(result.Errors, error => error.Field?.EndsWith(nameof(UnitRateCell.KdvRate)) == true);
        Assert.Contains(result.Errors, error => error.Field?.EndsWith(nameof(UnitRateCell.CalculationMethod)) == true);
    }

    [Fact]
    public async Task Pricing_ShouldAcceptDirectUnitScopeAndRejectOutsideScope()
    {
        var seed = await SeedAsync();
        var service = CreatePricingService();

        var matrix = await service.GetPricingMatrixAsync(new GetUnitPricingInput(
            seed.RentableUnitId,
            PricingYear,
            new UnitPricingAccessScopeInput([], [seed.RentableUnitId])));

        Assert.Equal(seed.RentableUnitId, matrix.UnitId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetPricingMatrixAsync(new GetUnitPricingInput(
                seed.RentableUnitId,
                PricingYear,
                new UnitPricingAccessScopeInput([], []))));
        Assert.Equal(ErrorType.Forbidden, exception.ErrorType);
        Assert.Equal("UNIT_PRICING_OUT_OF_SCOPE", exception.Code);
    }

    [Fact]
    public async Task Pricing_ShouldGuardMissingAndNonRentableUnits()
    {
        var seed = await SeedAsync();
        var service = CreatePricingService();

        var missingException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetPricingMatrixAsync(new GetUnitPricingInput(
                int.MaxValue,
                PricingYear,
                new UnitPricingAccessScopeInput())));
        Assert.Equal("UNIT_PRICING_UNIT_NOT_FOUND", missingException.Code);

        var nonRentableException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SavePricingMatrixAsync(new SaveUnitPricingInput(
                seed.FirstReservableUnitId,
                [],
                new UnitPricingAccessScopeInput())));
        Assert.Equal("UNIT_PRICING_NOT_RENTABLE", nonRentableException.Code);
    }

    [Fact]
    public async Task SavePricing_ShouldDeactivateAndReactivateSameRate()
    {
        var seed = await SeedAsync();
        var service = CreatePricingService();
        var accessScope = new UnitPricingAccessScopeInput();
        var matrix = await service.GetPricingMatrixAsync(new GetUnitPricingInput(
            seed.RentableUnitId,
            PricingYear,
            accessScope));
        var targetCell = matrix.Rows
            .SelectMany(row => row.Cells)
            .Single(cell => cell.TenantCategoryId == seed.CategoryId
                && cell.ChargeTypeId == seed.ChargeTypeId);
        targetCell.IsCustomRateActive = false;

        await service.SavePricingMatrixAsync(
            new UnitPricingFormViewModel { Rows = matrix.ToViewModel().Rows }
                .ToSaveInput(seed.RentableUnitId, accessScope));

        var deactivatedRate = await _context.UnitRates.IgnoreQueryFilters()
            .SingleAsync(rate => rate.Id == seed.UnitRateId);
        Assert.True(deactivatedRate.IsDeleted);
        Assert.False(deactivatedRate.IsActive);

        matrix = await service.GetPricingMatrixAsync(new GetUnitPricingInput(
            seed.RentableUnitId,
            PricingYear,
            accessScope));
        targetCell = matrix.Rows
            .SelectMany(row => row.Cells)
            .Single(cell => cell.TenantCategoryId == seed.CategoryId
                && cell.ChargeTypeId == seed.ChargeTypeId);
        targetCell.IsCustomRateActive = true;
        targetCell.CalculationMethod = CalculationMethod.Fixed;
        targetCell.UnitValue = 250;
        targetCell.VatRate = 20;

        await service.SavePricingMatrixAsync(
            new UnitPricingFormViewModel { Rows = matrix.ToViewModel().Rows }
                .ToSaveInput(seed.RentableUnitId, accessScope));

        var reactivatedRate = await _context.UnitRates.IgnoreQueryFilters()
            .SingleAsync(rate => rate.Id == seed.UnitRateId);
        Assert.False(reactivatedRate.IsDeleted);
        Assert.True(reactivatedRate.IsActive);
        Assert.Equal(250, reactivatedRate.UnitValue);
    }

    [Fact]
    public async Task SavePricing_ShouldRejectTamperedMatrixBeforeMutation()
    {
        var seed = await SeedAsync();
        var service = CreatePricingService();
        var accessScope = new UnitPricingAccessScopeInput();
        var matrix = await service.GetPricingMatrixAsync(new GetUnitPricingInput(
            seed.RentableUnitId,
            PricingYear,
            accessScope));
        var input = new UnitPricingFormViewModel { Rows = matrix.ToViewModel().Rows }
            .ToSaveInput(seed.RentableUnitId, accessScope);
        var firstRow = input.Rows[0];
        var tamperedRows = input.Rows
            .Append(new UnitPricingRowInput(firstRow.TenantCategoryId, firstRow.Cells))
            .ToList();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SavePricingMatrixAsync(input with { Rows = tamperedRows }));

        Assert.Equal("UNIT_PRICING_INVALID_CELL", exception.Code);
        var storedRate = await _context.UnitRates.AsNoTracking()
            .SingleAsync(rate => rate.Id == seed.UnitRateId);
        Assert.Equal(100, storedRate.UnitValue);
    }

    [Fact]
    public async Task UnitReservationRule_ShouldRejectForeignRuleAndOutsideScope()
    {
        var seed = await SeedAsync();
        var service = CreateReservationService();

        var foreignException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveUnitReservationRateRuleAsync(new SaveUnitReservationRateRuleInput(
                seed.SecondReservationRuleId,
                seed.FirstReservableUnitId,
                0,
                60,
                100,
                20,
                null,
                true,
                new ReservationAccessScopeInput())));
        Assert.Equal("UNIT_RESERVATION_RATE_FOREIGN_RULE", foreignException.Code);

        var scopeException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ClearUnitReservationRateRuleAsync(new ClearUnitReservationRateRuleInput(
                seed.SecondReservableUnitId,
                new ReservationAccessScopeInput([], [seed.FirstReservableUnitId]))));
        Assert.Equal("UNIT_RESERVATION_RATE_OUT_OF_SCOPE", scopeException.Code);
    }

    [Fact]
    public async Task ClearUnitReservationRule_ShouldGuardMissingRule()
    {
        var seed = await SeedAsync();
        var service = CreateReservationService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ClearUnitReservationRateRuleAsync(new ClearUnitReservationRateRuleInput(
                seed.FirstReservableUnitId,
                new ReservationAccessScopeInput())));

        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
        Assert.Equal("UNIT_RESERVATION_RATE_RULE_NOT_FOUND", exception.Code);
    }

    private UnitPricingService CreatePricingService()
    {
        return new(new UnitRateRepository(_context),
                   null!,
                   null!,
                   new UnitOfWork(_context));
    }

    private ReservationService CreateReservationService()
    {
        return new(null!,
                   new ReservationRateOverrideRepository(_context),
                   null!,
                   null!,
                   new UnitRepository(_context),
                   null!,
                   ReservationBusinessRulesTestFactory.Create(),
                   new UnitOfWork(_context),
                   new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
    }

    private async Task<UnitPricingSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = new Category
        {
            Type = CategoryType.Tenant,
            Name = $"Birim Fiyat Kategorisi {suffix}",
            Code = $"UCP_{suffix}"
        };
        var chargeType = new ChargeType
        {
            Name = $"Birim Fiyat Borç Tipi {suffix}",
            Code = $"UCT_{suffix}",
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        var property = new Property { Name = $"Birim Fiyat Taşınmazı {suffix}" };
        var rentableType = new UnitType
        {
            Name = $"Kiralanabilir Tür {suffix}",
            Code = $"URT_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var reservableType = new UnitType
        {
            Name = $"Rezervasyon Türü {suffix}",
            Code = $"URS_{suffix}",
            Usage = UnitTypeUsage.Reservable
        };
        _context.AddRange(category, chargeType, property, rentableType, reservableType);
        await _context.SaveChangesAsync();

        var rentableUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = rentableType.Id,
            Name = $"Kiralanabilir Birim {suffix}",
            Area = 10
        };
        var firstReservableUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = reservableType.Id,
            Name = $"Birinci Rezervasyon Birimi {suffix}",
            Area = 10
        };
        var secondReservableUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = reservableType.Id,
            Name = $"İkinci Rezervasyon Birimi {suffix}",
            Area = 10
        };
        _context.AddRange(rentableUnit, firstReservableUnit, secondReservableUnit);
        await _context.SaveChangesAsync();

        var unitRate = new UnitRate
        {
            UnitId = rentableUnit.Id,
            TenantCategoryId = category.Id,
            ChargeTypeId = chargeType.Id,
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = 100,
            KdvRate = 20
        };
        var secondReservationRule = new ReservationRateOverride
        {
            UnitId = secondReservableUnit.Id,
            FreeDurationMinutes = 0,
            BillingPeriodMinutes = 60,
            PeriodRate = 100,
            KdvRate = 20
        };
        _context.AddRange(unitRate, secondReservationRule);
        await _context.SaveChangesAsync();

        return new UnitPricingSeed(
            category.Id,
            chargeType.Id,
            rentableUnit.Id,
            unitRate.Id,
            firstReservableUnit.Id,
            secondReservableUnit.Id,
            secondReservationRule.Id);
    }

    private sealed record UnitPricingSeed(
        int CategoryId,
        int ChargeTypeId,
        int RentableUnitId,
        int UnitRateId,
        int FirstReservableUnitId,
        int SecondReservableUnitId,
        int SecondReservationRuleId);
}
