using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PropertyService(
    IPropertyRepository propertyRepository,
    IPropertyTypeRepository propertyTypeRepository,
    IUnitRepository unitRepository,
    IUnitTypeRepository unitTypeRepository,
    IReservationRepository reservationRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IPropertyPricingService propertyPricingService,
    IUnitOfWork unitOfWork,
    IStatisticsService statisticsService) : IPropertyService, ITransactionalService
{
    public Task<List<PropertyListItemDto>> GetAllAsync(GetPropertiesInput input)
        => propertyRepository.GetListAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public async Task<PropertyDetailDto?> GetDetailsAsync(GetPropertyDetailsInput input)
    {
        var details = await propertyRepository.GetDetailsAsync(input.PropertyId);
        if (details == null) return null;

        foreach (var unit in details.Units)
        {
            if (unit.ActiveLeaseId.HasValue)
            {
                var lease = new Lease
                {
                    Id = unit.ActiveLeaseId.Value,
                    TenantId = unit.ActiveLeaseTenantId ?? 0,
                    UnitId = unit.Id,
                    Unit = new Unit { Id = unit.Id, Area = unit.Area }
                };
                unit.MonthlyRent = await statisticsService.GetMonthlyAmountAsync(lease);
            }
        }

        foreach (var leaseHistory in details.LeaseHistory)
        {
            var unitArea = details.Units.FirstOrDefault(unit => unit.Id == leaseHistory.UnitId)?.Area ?? 0m;
            var lease = new Lease
            {
                Id = leaseHistory.Id,
                TenantId = leaseHistory.TenantId,
                UnitId = leaseHistory.UnitId,
                Unit = new Unit { Id = leaseHistory.UnitId, Area = unitArea }
            };
            leaseHistory.MonthlyAmount = await statisticsService.GetMonthlyAmountAsync(lease);
        }

        return details;
    }

    public async Task<PropertyFormOptionsDto> GetFormOptionsAsync()
        => new(await propertyTypeRepository.GetActiveOptionsAsync(), 
               await unitTypeRepository.GetActiveOptionsAsync());

    public async Task<CreatedPropertyDto> CreateAsync(CreatePropertyInput input)
    {
        await ValidateDatabaseRulesAsync(input);

        var property = new Property
        {
            Name = input.Name,
            PropertyTypeId = input.PropertyTypeId,
            UnitStructure = input.UnitStructure,
            City = input.City,
            District = input.District,
            Neighborhood = input.Neighborhood,
            Address = input.Address,
            OpenArea = input.OpenArea,
            ClosedArea = input.ClosedArea,
            FloorCount = input.FloorCount,
            Description = input.Description
        };

        if (property.UnitStructure == UnitStructure.SingleUnit)
        {
            Guard.Against(!input.SingleUnitTypeId.HasValue, "Tek birim yapısı için birim türü zorunludur.");

            property.Units.Add(new Unit
            {
                Name = property.Name,
                Area = property.ClosedArea > 0 ? property.ClosedArea : property.OpenArea,
                UnitTypeId = input.SingleUnitTypeId.Value
            });
        }
        else
        {
            foreach (var unitInput in input.Units)
            {
                property.Units.Add(new Unit
                {
                    UnitNo = unitInput.UnitNo,
                    FloorNo = unitInput.FloorNo,
                    Name = string.IsNullOrWhiteSpace(unitInput.Name)
                        ? $"Birim {unitInput.UnitNo}"
                        : unitInput.Name,
                    Area = unitInput.Area,
                    Description = unitInput.Description,
                    UnitTypeId = unitInput.UnitTypeId!.Value
                });
            }

            foreach (var reservationInput in input.ReservationAreas)
            {
                var unit = new Unit
                {
                    UnitNo = reservationInput.UnitNo,
                    Name = string.IsNullOrWhiteSpace(reservationInput.Name)
                        ? "Rezervasyon Alanı"
                        : reservationInput.Name,
                    Area = reservationInput.Area,
                    Description = reservationInput.Description,
                    UnitTypeId = reservationInput.UnitTypeId!.Value
                };
                property.Units.Add(unit);
                await reservationRateOverrideRepository.AddAsync(new ReservationRateOverride
                {
                    Unit = unit,
                    FreeDurationMinutes = reservationInput.FreeDurationMinutes,
                    BillingPeriodMinutes = 60,
                    PeriodRate = reservationInput.HourlyRate,
                    KdvRate = reservationInput.VatRate,
                    Description = $"{reservationInput.Name} için otomatik oluşturuldu"
                });
            }
        }

        await propertyRepository.AddAsync(property);
        await unitOfWork.SaveChangesAsync();

        input.PricingMatrix.PropertyId = property.Id;
        await propertyPricingService.SaveMatrixAsync(input.PricingMatrix);

        return new CreatedPropertyDto(property.Id, property.Name);
    }

    public async Task<PropertyEditDto?> GetForEditAsync(GetPropertyForEditInput input)
    {
        var property = await propertyRepository.GetWithUnitsTrackedAsync(input.PropertyId);
        if (property == null) return null;

        var now = DateTime.Now;
        var unitIds = property.Units.Select(unit => unit.Id).ToList();
        var reservationRates = await reservationRateOverrideRepository.GetByUnitIdsAsync(
            unitIds,
            activeOnly: true);
        var activeReservationUnitIds = await reservationRepository.GetActiveUnitIdsAsync(unitIds, now);

        var result = new PropertyEditDto
        {
            PropertyId = property.Id,
            Name = property.Name,
            PropertyTypeId = property.PropertyTypeId,
            UnitStructure = property.UnitStructure,
            CanChangeUnitStructure = await propertyRepository.CanChangeUnitStructureAsync(property.Id),
            SingleUnitTypeId = property.UnitStructure == UnitStructure.SingleUnit
                ? property.Units.SingleOrDefault()?.UnitTypeId
                : null,
            City = property.City,
            District = property.District,
            Neighborhood = property.Neighborhood,
            Address = property.Address,
            OpenArea = property.OpenArea,
            ClosedArea = property.ClosedArea,
            FloorCount = property.FloorCount,
            Description = property.Description
        };

        if (property.UnitStructure != UnitStructure.MultipleUnits) return result;

        foreach (var unit in property.Units)
        {
            if (unit.UnitType.Usage == UnitTypeUsage.Reservable)
            {
                reservationRates.TryGetValue(unit.Id, out var rate);
                result.ReservationAreas.Add(new ReservationAreaInputDto
                {
                    Id = unit.Id,
                    UnitNo = unit.UnitNo ?? string.Empty,
                    Name = unit.Name,
                    Area = unit.Area,
                    UnitTypeId = unit.UnitTypeId,
                    Description = unit.Description,
                    FreeDurationMinutes = rate?.FreeDurationMinutes ?? 0,
                    HourlyRate = rate?.PeriodRate ?? 0,
                    VatRate = rate?.KdvRate ?? 20
                });
                if (activeReservationUnitIds.Contains(unit.Id))
                    result.ActiveReservationUnitIds.Add(unit.Id);
            }
            else
            {
                result.Units.Add(new PropertyUnitInputDto
                {
                    Id = unit.Id,
                    UnitNo = unit.UnitNo ?? string.Empty,
                    FloorNo = unit.FloorNo,
                    Name = unit.Name,
                    Area = unit.Area,
                    Description = unit.Description,
                    UnitTypeId = unit.UnitTypeId
                });
                if (unit.Leases.Any())
                    result.ActiveLeaseUnitIds.Add(unit.Id);
            }
        }

        return result;
    }

    public async Task UpdateWithChildrenAsync(UpdatePropertyInput input)
    {
        var property = Guard.NotFound(
            await propertyRepository.GetWithUnitsTrackedAsync(input.PropertyId),
            "Taşınmaz bulunamadı.",
            "Property.NotFound");
        Guard.Forbidden(
            input.AccessiblePropertyIds != null && !input.AccessiblePropertyIds.Contains(property.Id),
            "Bu taşınmaz üzerinde işlem yapma yetkiniz bulunmuyor.",
            "Property.OutOfScope");

        var structureChanged = property.UnitStructure != input.UnitStructure;
        Guard.InvalidField(
            structureChanged && !await propertyRepository.CanChangeUnitStructureAsync(property.Id),
            nameof(input.UnitStructure),
            "Sözleşme, rezervasyon veya tahakkuk geçmişi bulunan taşınmazın birim yapısı değiştirilemez.",
            "Property.UnitStructureHasHistory");

        await ValidateDatabaseRulesAsync(input);
        if (!structureChanged && property.UnitStructure == UnitStructure.MultipleUnits)
            await ValidateSubmittedUnitsAsync(property, input);

        property.Name = input.Name;
        property.PropertyTypeId = input.PropertyTypeId;
        property.City = input.City;
        property.District = input.District;
        property.Neighborhood = input.Neighborhood;
        property.Address = input.Address;
        property.OpenArea = input.OpenArea;
        property.ClosedArea = input.ClosedArea;
        property.FloorCount = input.FloorCount;
        property.Description = input.Description;

        if (structureChanged)
        {
            await unitRepository.RemoveStructureDataAsync(property.Units);
            property.Units.Clear();
            property.UnitStructure = input.UnitStructure;
        }

        if (property.UnitStructure == UnitStructure.SingleUnit)
        {
            Guard.Against(!input.SingleUnitTypeId.HasValue, "Tek birim yapısı için birim türü zorunludur.");

            var unit = property.Units.SingleOrDefault();
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }

            unit.Name = property.Name;
            unit.UnitNo = null;
            unit.FloorNo = null;
            unit.Area = input.ClosedArea > 0 ? input.ClosedArea : input.OpenArea;
            unit.Description = input.Description;
            unit.UnitTypeId = input.SingleUnitTypeId.Value;
        }
        else
        {
            await UpdateMultipleUnitsAsync(property, input);
        }

        await unitOfWork.SaveChangesAsync();
        input.PricingMatrix.PropertyId = property.Id;
        await propertyPricingService.SaveMatrixAsync(input.PricingMatrix);
    }

    private async Task UpdateMultipleUnitsAsync(Property property, UpdatePropertyInput input)
    {
        var unitIds = property.Units.Select(unit => unit.Id).ToList();
        var reservationRates = await reservationRateOverrideRepository.GetByUnitIdsAsync(
            unitIds,
            activeOnly: false);
        var normalUnits = property.Units
            .Where(unit => unit.UnitType.Usage != UnitTypeUsage.Reservable)
            .ToList();
        var reservationUnits = property.Units
            .Where(unit => unit.UnitType.Usage == UnitTypeUsage.Reservable)
            .ToList();

        var incomingNormalIds = input.Units
            .Where(unit => unit.Id.HasValue)
            .Select(unit => unit.Id!.Value)
            .ToHashSet();
        foreach (var existing in normalUnits.Where(unit => !incomingNormalIds.Contains(unit.Id)))
        {
            await unitRepository.RemoveWithRatesAsync(existing);
        }

        foreach (var unitInput in input.Units)
        {
            var name = string.IsNullOrWhiteSpace(unitInput.Name)
                ? $"Birim {unitInput.UnitNo}"
                : unitInput.Name;
            var unit = unitInput.Id.HasValue
                ? property.Units.FirstOrDefault(item => item.Id == unitInput.Id.Value)
                : null;
            Guard.Forbidden(
                unitInput.Id.HasValue && unit == null,
                "Gönderilen birim bu taşınmaza ait değil.",
                "Property.ForeignUnit");
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }
            unit.UnitNo = unitInput.UnitNo;
            unit.FloorNo = unitInput.FloorNo;
            unit.Name = name;
            unit.Area = unitInput.Area;
            unit.Description = unitInput.Description;
            unit.UnitTypeId = unitInput.UnitTypeId!.Value;
        }

        var incomingReservationIds = input.ReservationAreas
            .Where(area => area.Id.HasValue)
            .Select(area => area.Id!.Value)
            .ToHashSet();
        foreach (var existing in reservationUnits.Where(unit => !incomingReservationIds.Contains(unit.Id)))
        {
            if (reservationRates.TryGetValue(existing.Id, out var rate))
                reservationRateOverrideRepository.Remove(rate);
            unitRepository.Remove(existing);
        }

        foreach (var reservationInput in input.ReservationAreas)
        {
            var unit = reservationInput.Id.HasValue
                ? property.Units.FirstOrDefault(item => item.Id == reservationInput.Id.Value)
                : null;
            Guard.Forbidden(
                reservationInput.Id.HasValue && unit == null,
                "Gönderilen rezervasyon birimi bu taşınmaza ait değil.",
                "Property.ForeignReservationUnit");
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }

            unit.UnitNo = reservationInput.UnitNo;
            unit.FloorNo = null;
            unit.Name = reservationInput.Name ?? "Rezervasyon Alanı";
            unit.Area = reservationInput.Area;
            unit.Description = reservationInput.Description;
            unit.UnitTypeId = reservationInput.UnitTypeId!.Value;

            if (!reservationRates.TryGetValue(unit.Id, out var rate))
            {
                rate = new ReservationRateOverride { Unit = unit, BillingPeriodMinutes = 60 };
                await reservationRateOverrideRepository.AddAsync(rate);
            }

            rate.FreeDurationMinutes = reservationInput.FreeDurationMinutes;
            rate.PeriodRate = reservationInput.HourlyRate;
            rate.KdvRate = reservationInput.VatRate;
            rate.Description = $"{reservationInput.Name} için otomatik oluşturuldu";
        }
    }

    private async Task ValidateDatabaseRulesAsync(CreatePropertyInput input)
    {
        Guard.InvalidField(
            input.PropertyTypeId is null or <= 0,
            nameof(input.PropertyTypeId),
            "Taşınmaz tipi zorunludur.");

        var support = await propertyTypeRepository.GetStructureSupportAsync(input.PropertyTypeId!.Value);
        Guard.InvalidField(
            support == null,
            nameof(input.PropertyTypeId),
            "Seçilen taşınmaz tipi aktif değil veya bulunamadı.",
            "Property.PropertyTypeUnavailable");
        Guard.InvalidField(
            input.UnitStructure == UnitStructure.SingleUnit
                ? !support!.SupportsSingleUnit
                : !support!.SupportsMultipleUnits,
            nameof(input.UnitStructure),
            "Seçilen taşınmaz tipi bu birim yapısına izin vermiyor.",
            "Property.UnsupportedUnitStructure");

        var normalIds = input.Units.Where(unit => unit.UnitTypeId.HasValue)
            .Select(unit => unit.UnitTypeId!.Value).Distinct().ToList();
        var reservationIds = input.ReservationAreas.Where(area => area.UnitTypeId.HasValue)
            .Select(area => area.UnitTypeId!.Value).Distinct().ToList();
        var requestedIds = input.UnitStructure == UnitStructure.SingleUnit
            ? input.SingleUnitTypeId.HasValue ? new List<int> { input.SingleUnitTypeId.Value } : []
            : normalIds.Concat(reservationIds).Distinct().ToList();
        var usages = await unitTypeRepository.GetActiveUsagesAsync(requestedIds);
        var usageMap = usages.ToDictionary(item => item.UnitTypeId, item => item.Usage);

        if (input.UnitStructure == UnitStructure.SingleUnit)
        {
            Guard.InvalidField(
                !input.SingleUnitTypeId.HasValue || !usageMap.ContainsKey(input.SingleUnitTypeId.Value),
                nameof(input.SingleUnitTypeId),
                "Seçilen birim türü aktif değil veya bulunamadı.",
                "Property.UnitTypeUnavailable");
            return;
        }

        Guard.InvalidField(
            normalIds.Any(id => !usageMap.TryGetValue(id, out var usage) || usage == UnitTypeUsage.Reservable),
            nameof(input.Units),
            "Birimler için aktif ve rezervasyon dışı bir birim türü seçilmelidir.",
            "Property.InvalidNormalUnitType");
        Guard.InvalidField(
            reservationIds.Any(id => !usageMap.TryGetValue(id, out var usage) || usage != UnitTypeUsage.Reservable),
            nameof(input.ReservationAreas),
            "Rezervasyon alanları için aktif bir rezervasyon birim türü seçilmelidir.",
            "Property.InvalidReservationUnitType");
    }

    private async Task ValidateSubmittedUnitsAsync(Property property, UpdatePropertyInput input)
    {
        var existing = property.Units.ToDictionary(unit => unit.Id);
        foreach (var unit in input.Units.Where(unit => unit.Id.HasValue))
        {
            Guard.Forbidden(
                !existing.TryGetValue(unit.Id!.Value, out var entity)
                    || entity.UnitType.Usage == UnitTypeUsage.Reservable,
                "Gönderilen birim kaydı bu taşınmaza veya bölüme ait değil.",
                "Property.InvalidUnitOwnership");
        }

        foreach (var area in input.ReservationAreas.Where(area => area.Id.HasValue))
        {
            Guard.Forbidden(
                !existing.TryGetValue(area.Id!.Value, out var entity)
                    || entity.UnitType.Usage != UnitTypeUsage.Reservable,
                "Gönderilen rezervasyon kaydı bu taşınmaza veya bölüme ait değil.",
                "Property.InvalidReservationOwnership");
        }

        var incomingNormalIds = input.Units.Where(unit => unit.Id.HasValue)
            .Select(unit => unit.Id!.Value).ToHashSet();
        var incomingReservationIds = input.ReservationAreas.Where(area => area.Id.HasValue)
            .Select(area => area.Id!.Value).ToHashSet();

        foreach (var unit in property.Units)
        {
            var isReservationUnit = unit.UnitType.Usage == UnitTypeUsage.Reservable;
            var isRemoved = isReservationUnit
                ? !incomingReservationIds.Contains(unit.Id)
                : !incomingNormalIds.Contains(unit.Id);
            if (!isRemoved) continue;

            Guard.InvalidField(
                await unitRepository.HasHistoricalDependencyAsync(unit.Id),
                isReservationUnit ? nameof(input.ReservationAreas) : nameof(input.Units),
                $"'{unit.Name}' biriminin işlem geçmişi bulunduğu için silinemez.",
                isReservationUnit
                    ? "Property.ReservationUnitHasHistory"
                    : "Property.UnitHasHistory");
        }
    }

    public Task<List<UnitLookupDto>> GetAvailableUnitsAsync(GetAvailableUnitsInput input)
        => unitRepository.GetAvailableAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
}
