using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Reporting;
using KiraTakip.Domain.Properties;
using KiraTakip.Domain.Leases;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Domain.Reservations;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Services.Properties;

public class PropertyService(
    IPropertyRepository propertyRepository,
    IPropertyTypeRepository propertyTypeRepository,
    IUnitRepository unitRepository,
    IUnitTypeRepository unitTypeRepository,
    IReservationRepository reservationRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IPropertyPricingService propertyPricingService,
    IUnitOfWork unitOfWork,
    IStatisticsService statisticsService,
    IOperationalPolicyProvider operationalPolicyProvider,
    TimeProvider timeProvider) : IPropertyService, ITransactionalService
{
    public Task<List<PropertyListItemDto>> GetAllAsync(GetPropertiesInput input)
        => propertyRepository.GetListAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<PagedResult<PropertyListItemDto>> GetPagedAsync(GetPropertiesPageInput input)
        => propertyRepository.GetPagedListAsync(
            input.Query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public async Task<PropertyDetailDto?> GetDetailsAsync(GetPropertyDetailsInput input)
    {
        var details = await propertyRepository.GetDetailsAsync(input.PropertyId);
        if (details == null) return null;

        // Units + LeaseHistory'deki tüm sözleşmeler tek seferde toplu (bulk) hesaplanır (N+1 sorgu
        // önlenir, bkz. IStatisticsService.GetMonthlyAmountsAsync). LeaseId'ye göre dedupe edilir —
        // bir birimin aktif sözleşmesi aynı zamanda LeaseHistory'de de göründüğü için aynı hesap
        // tekrar yapılmaz.
        var leasesById = new Dictionary<int, Lease>();

        foreach (var unit in details.Units)
        {
            if (!unit.ActiveLeaseId.HasValue) continue;
            leasesById[unit.ActiveLeaseId.Value] = new Lease
            {
                Id = unit.ActiveLeaseId.Value,
                TenantId = unit.ActiveLeaseTenantId ?? 0,
                UnitId = unit.Id,
                IsRentFree = unit.ActiveLeaseIsRentFree,
                Unit = new Unit { Id = unit.Id, Area = unit.Area }
            };
        }

        foreach (var leaseHistory in details.LeaseHistory)
        {
            if (leasesById.ContainsKey(leaseHistory.Id)) continue;
            var unitArea = details.Units.FirstOrDefault(unit => unit.Id == leaseHistory.UnitId)?.Area ?? 0m;
            leasesById[leaseHistory.Id] = new Lease
            {
                Id = leaseHistory.Id,
                TenantId = leaseHistory.TenantId,
                UnitId = leaseHistory.UnitId,
                IsRentFree = leaseHistory.IsRentFree,
                Unit = new Unit { Id = leaseHistory.UnitId, Area = unitArea }
            };
        }

        var monthlyAmounts = await statisticsService.GetMonthlyAmountsAsync(leasesById.Values.ToList());

        foreach (var unit in details.Units)
        {
            if (unit.ActiveLeaseId.HasValue && monthlyAmounts.TryGetValue(unit.ActiveLeaseId.Value, out var amount))
                unit.MonthlyRent = amount;
        }

        foreach (var leaseHistory in details.LeaseHistory)
        {
            if (monthlyAmounts.TryGetValue(leaseHistory.Id, out var amount))
                leaseHistory.MonthlyAmount = amount;
        }

        return details;
    }

    public async Task<PropertyFormOptionsDto> GetFormOptionsAsync()
        => new(await propertyTypeRepository.GetActiveOptionsAsync(),
               await unitTypeRepository.GetActiveOptionsAsync());

    public async Task<CreatedPropertyDto> CreateAsync(CreatePropertyInput input)
    {
        var rentalModes = await ValidateDatabaseRulesAsync(input);
        if (input.UnitStructure == UnitStructure.MultipleUnits)
        {
            for (var i = 0; i < input.Units.Count; i++)
            {
                var capacity = EffectiveCapacity(input.Units[i], rentalModes, null, UnitCapacityField(i));
                if (capacity.HasValue)
                    ValidateCapacityLimit(UnitCapacityField(i), capacity.Value, input.Units[i].Area);
            }
        }
        else
        {
            var singleCapacity = EffectiveSingleCapacity(input, rentalModes, null);
            if (singleCapacity.HasValue)
                ValidateCapacityLimit(
                    nameof(input.SingleUnitCapacity),
                    singleCapacity.Value,
                    PropertyStructurePolicy.CalculateSingleUnitArea(input.ClosedArea, input.OpenArea));
        }

        var newCapacityUnits = new List<(Unit Unit, int Capacity)>();
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

            var singleCapacity = EffectiveSingleCapacity(input, rentalModes, null);
            var singleUnit = new Unit
            {
                Name = property.Name,
                Area = PropertyStructurePolicy.CalculateSingleUnitArea(property.ClosedArea, property.OpenArea),
                Capacity = singleCapacity,
                UnitTypeId = input.SingleUnitTypeId.Value
            };
            property.Units.Add(singleUnit);
            if (singleCapacity.HasValue)
                newCapacityUnits.Add((singleUnit, singleCapacity.Value));
        }
        else
        {
            foreach (var unitInput in input.Units)
            {
                var capacity = EffectiveCapacity(unitInput, rentalModes, null, nameof(PropertyUnitInputDto.Capacity));
                var newUnit = new Unit
                {
                    UnitNo = unitInput.UnitNo,
                    FloorNo = unitInput.FloorNo,
                    Name = string.IsNullOrWhiteSpace(unitInput.Name)
                        ? $"Birim {unitInput.UnitNo}"
                        : unitInput.Name,
                    Area = unitInput.Area,
                    Capacity = capacity,
                    Description = unitInput.Description,
                    UnitTypeId = unitInput.UnitTypeId!.Value
                };
                property.Units.Add(newUnit);
                if (capacity.HasValue)
                    newCapacityUnits.Add((newUnit, capacity.Value));
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
            }
        }

        await propertyRepository.AddAsync(property);
        var today = Today();
        foreach (var (capacityUnit, capacity) in newCapacityUnits)
            unitRepository.AddCapacityHistory(capacityUnit, capacity, today);
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
        var activeReservationUnitIds = await reservationRepository.GetActiveUnitIdsAsync(unitIds, now);
        var openLeaseUnitIds = await unitRepository.GetOpenLeaseUnitIdsAsync(unitIds);

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
            SingleUnitCapacity = property.UnitStructure == UnitStructure.SingleUnit
                ? property.Units.SingleOrDefault()?.Capacity
                : null,
            City = property.City,
            District = property.District,
            Neighborhood = property.Neighborhood,
            Address = property.Address,
            OpenArea = property.OpenArea,
            ClosedArea = property.ClosedArea,
            FloorCount = property.FloorCount,
            Description = property.Description,
            OpenLeaseUnitIds = openLeaseUnitIds
        };

        if (property.UnitStructure != UnitStructure.MultipleUnits) return result;

        foreach (var unit in property.Units)
        {
            if (unit.UnitType.Usage == UnitTypeUsage.Reservable)
            {
                result.ReservationAreas.Add(new ReservationAreaInputDto
                {
                    Id = unit.Id,
                    UnitNo = unit.UnitNo ?? string.Empty,
                    Name = unit.Name,
                    Area = unit.Area,
                    UnitTypeId = unit.UnitTypeId,
                    Description = unit.Description
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
                    UnitTypeId = unit.UnitTypeId,
                    Capacity = unit.Capacity
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

        var rentalModes = await ValidateDatabaseRulesAsync(
            input,
            structureChanged ? null : property.Units.ToDictionary(unit => unit.Id));
        if (!structureChanged && property.UnitStructure == UnitStructure.MultipleUnits)
            await ValidateSubmittedUnitsAsync(property, input);
        await ValidateUnitChangesAsync(property, input, rentalModes, structureChanged);

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
            var isNew = unit == null;
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }

            var singleCapacity = EffectiveSingleCapacity(input, rentalModes, unit);
            var singleCapacityChanged = singleCapacity.HasValue && (isNew || unit.Capacity != singleCapacity);
            unit.Name = property.Name;
            unit.UnitNo = null;
            unit.FloorNo = null;
            unit.Area = PropertyStructurePolicy.CalculateSingleUnitArea(input.ClosedArea, input.OpenArea);
            unit.Description = input.Description;
            unit.UnitTypeId = input.SingleUnitTypeId.Value;
            unit.Capacity = singleCapacity;
            if (singleCapacityChanged)
                unitRepository.AddCapacityHistory(unit, singleCapacity!.Value, Today());
        }
        else
        {
            await UpdateMultipleUnitsAsync(property, input, rentalModes);
        }

        await unitOfWork.SaveChangesAsync();
        input.PricingMatrix.PropertyId = property.Id;
        await propertyPricingService.SaveMatrixAsync(input.PricingMatrix);
    }

    private async Task UpdateMultipleUnitsAsync(
        Property property,
        UpdatePropertyInput input,
        IReadOnlyDictionary<int, UnitRentalMode> rentalModes)
    {
        var today = Today();
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
            var isNew = unit == null;
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }
            var capacity = EffectiveCapacity(unitInput, rentalModes, unit, UnitCapacityField(input.Units.IndexOf(unitInput)));
            var capacityChanged = capacity.HasValue && (isNew || unit.Capacity != capacity);
            unit.UnitNo = unitInput.UnitNo;
            unit.FloorNo = unitInput.FloorNo;
            unit.Name = name;
            unit.Area = unitInput.Area;
            unit.Capacity = capacity;
            unit.Description = unitInput.Description;
            unit.UnitTypeId = unitInput.UnitTypeId!.Value;
            if (capacityChanged)
                unitRepository.AddCapacityHistory(unit, capacity!.Value, today);
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
        }
    }

    private async Task<IReadOnlyDictionary<int, UnitRentalMode>> ValidateDatabaseRulesAsync(
        CreatePropertyInput input,
        IReadOnlyDictionary<int, Unit>? existingUnits = null)
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
            !PropertyStructurePolicy.SupportsUnitStructure(
                input.UnitStructure,
                support!.SupportsSingleUnit,
                support!.SupportsMultipleUnits),
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
        var rentalModes = usages.ToDictionary(item => item.UnitTypeId, item => item.RentalMode);

        if (input.UnitStructure == UnitStructure.SingleUnit)
        {
            Guard.InvalidField(
                !input.SingleUnitTypeId.HasValue || !usageMap.ContainsKey(input.SingleUnitTypeId.Value),
                nameof(input.SingleUnitTypeId),
                "Seçilen birim türü aktif değil veya bulunamadı.",
                "Property.UnitTypeUnavailable");
            Guard.InvalidField(
                rentalModes[input.SingleUnitTypeId!.Value] == UnitRentalMode.CapacityBased
                    && input.SingleUnitCapacity is null or <= 0
                    && existingUnits?.Values.SingleOrDefault()?.Capacity is null,
                nameof(input.SingleUnitCapacity),
                "Kontenjan bazlı birim türünde kontenjan (K) girilmeli ve 0'dan büyük olmalıdır.",
                "Property.CapacityRequired");
            return rentalModes;
        }

        Guard.InvalidField(
            normalIds.Any(id => !usageMap.TryGetValue(id, out var usage) || !PropertyStructurePolicy.IsValidNormalUnitUsage(usage)),
            nameof(input.Units),
            "Birimler için aktif ve rezervasyon dışı bir birim türü seçilmelidir.",
            "Property.InvalidNormalUnitType");
        Guard.InvalidField(
            reservationIds.Any(id => !usageMap.TryGetValue(id, out var usage) || !PropertyStructurePolicy.IsValidReservationUnitUsage(usage)),
            nameof(input.ReservationAreas),
            "Rezervasyon alanları için aktif bir rezervasyon birim türü seçilmelidir.",
            "Property.InvalidReservationUnitType");

        for (var i = 0; i < input.Units.Count; i++)
        {
            var unit = input.Units[i];
            Guard.InvalidField(
                rentalModes[unit.UnitTypeId!.Value] == UnitRentalMode.CapacityBased && unit.Capacity is null or <= 0
                    && !HasExistingCapacity(existingUnits, unit.Id),
                $"Units[{i}].{nameof(unit.Capacity)}",
                "Kontenjan bazlı birim türünde kontenjan (K) girilmeli ve 0'dan büyük olmalıdır.",
                "Property.CapacityRequired");
        }

        return rentalModes;
    }

    private static bool HasExistingCapacity(IReadOnlyDictionary<int, Unit>? existingUnits, int? unitId)
        => unitId.HasValue
            && existingUnits != null
            && existingUnits.TryGetValue(unitId.Value, out var existing)
            && existing.Capacity.HasValue;

    private static int? EffectiveCapacity(
        PropertyUnitInputDto unit,
        IReadOnlyDictionary<int, UnitRentalMode> rentalModes,
        Unit? existing,
        string field)
        => unit.UnitTypeId.HasValue
            && rentalModes.TryGetValue(unit.UnitTypeId.Value, out var mode)
            && mode == UnitRentalMode.CapacityBased
                ? ResolveCapacity(unit.Capacity, existing, field)
                : null;

    private static int? EffectiveSingleCapacity(
        CreatePropertyInput input,
        IReadOnlyDictionary<int, UnitRentalMode> rentalModes,
        Unit? existing)
        => input.SingleUnitTypeId.HasValue
            && rentalModes.TryGetValue(input.SingleUnitTypeId.Value, out var mode)
            && mode == UnitRentalMode.CapacityBased
                ? ResolveCapacity(input.SingleUnitCapacity, existing, nameof(input.SingleUnitCapacity))
                : null;

    private static int? ResolveCapacity(int? submitted, Unit? existing, string field)
    {
        if (existing?.Capacity is not { } current) return submitted;

        Guard.InvalidField(
            submitted.HasValue && submitted.Value != current,
            field,
            "Kontenjan, Kontenjanı değiştir ekranından güncellenir.",
            "Property.CapacityChangeNotAllowed");
        return current;
    }

    private static string UnitCapacityField(int index)
        => $"Units[{index}].{nameof(PropertyUnitInputDto.Capacity)}";

    private DateTime Today() => timeProvider.GetLocalNow().DateTime.Date;

    private async Task ValidateUnitChangesAsync(
        Property property,
        UpdatePropertyInput input,
        IReadOnlyDictionary<int, UnitRentalMode> rentalModes,
        bool structureChanged)
    {
        var existing = structureChanged
            ? new Dictionary<int, Unit>()
            : property.Units.ToDictionary(unit => unit.Id);
        var openLeaseUnitIds = await unitRepository.GetOpenLeaseUnitIdsAsync(existing.Keys.ToList());

        if (input.UnitStructure == UnitStructure.SingleUnit)
        {
            var single = existing.Values.SingleOrDefault();
            if (single != null && openLeaseUnitIds.Contains(single.Id))
            {
                Guard.InvalidField(
                    property.ClosedArea != input.ClosedArea,
                    nameof(input.ClosedArea),
                    "Açık sözleşmesi bulunan birimin alanı değiştirilemez.",
                    "Property.UnitAreaLocked");
                Guard.InvalidField(
                    property.OpenArea != input.OpenArea,
                    nameof(input.OpenArea),
                    "Açık sözleşmesi bulunan birimin alanı değiştirilemez.",
                    "Property.UnitAreaLocked");
                Guard.InvalidField(
                    single.UnitTypeId != input.SingleUnitTypeId,
                    nameof(input.SingleUnitTypeId),
                    "Açık sözleşmesi bulunan birimin türü değiştirilemez.",
                    "Property.UnitTypeLocked");
            }

            var singleCapacity = EffectiveSingleCapacity(input, rentalModes, single);
            if (!singleCapacity.HasValue) return;
            var singleArea = PropertyStructurePolicy.CalculateSingleUnitArea(input.ClosedArea, input.OpenArea);
            if (single?.Capacity == null)
                ValidateCapacityLimit(nameof(input.SingleUnitCapacity), singleCapacity.Value, singleArea);
            else if (single.Area != singleArea)
                await ValidateAreaChangeAsync(
                    nameof(input.SingleUnitCapacity),
                    nameof(input.ClosedArea),
                    singleCapacity.Value,
                    singleArea,
                    single.Id);
            return;
        }

        for (var i = 0; i < input.Units.Count; i++)
        {
            var unitInput = input.Units[i];
            Unit? entity = null;
            if (unitInput.Id.HasValue)
                existing.TryGetValue(unitInput.Id.Value, out entity);

            if (entity != null && openLeaseUnitIds.Contains(entity.Id))
            {
                Guard.InvalidField(
                    entity.Area != unitInput.Area,
                    $"Units[{i}].{nameof(unitInput.Area)}",
                    "Açık sözleşmesi bulunan birimin alanı değiştirilemez.",
                    "Property.UnitAreaLocked");
                Guard.InvalidField(
                    entity.UnitTypeId != unitInput.UnitTypeId,
                    $"Units[{i}].{nameof(unitInput.UnitTypeId)}",
                    "Açık sözleşmesi bulunan birimin türü değiştirilemez.",
                    "Property.UnitTypeLocked");
            }

            var capacity = EffectiveCapacity(unitInput, rentalModes, entity, UnitCapacityField(i));
            if (!capacity.HasValue) continue;
            if (entity?.Capacity == null)
                ValidateCapacityLimit(UnitCapacityField(i), capacity.Value, unitInput.Area);
            else if (entity.Area != unitInput.Area)
                await ValidateAreaChangeAsync(
                    UnitCapacityField(i),
                    $"Units[{i}].{nameof(unitInput.Area)}",
                    capacity.Value,
                    unitInput.Area,
                    entity.Id);
        }
    }

    private void ValidateCapacityLimit(string field, int capacity, decimal unitArea)
    {
        var maximum = UnitCapacityPolicy.GetCapacityLimit(
            unitArea, operationalPolicyProvider.Current.LeaseMinimumUsageArea);
        Guard.InvalidField(
            capacity > maximum,
            field,
            $"Kontenjan çok yüksek. İzin verilen en yüksek kontenjan: {maximum}.",
            "Property.CapacityAboveMaximum");
    }

    private async Task ValidateAreaChangeAsync(
        string capacityField,
        string areaField,
        int capacity,
        decimal newArea,
        int unitId)
    {
        ValidateCapacityLimit(capacityField, capacity, newArea);

        var today = Today();
        var occupants = await unitRepository.GetCapacityOccupantsAsync(unitId, today);
        var rangeEnd = occupants.Count == 0 ? today : occupants.Max(occupant => occupant.EndDate).Date;
        if (rangeEnd < today) rangeEnd = today;

        Guard.InvalidField(
            UnitCapacityPolicy.GetHardViolations(capacity, newArea, occupants, today, rangeEnd)
                .Contains(CapacityHardViolation.AreaExceeded),
            areaField,
            "Birim alanı, açık sözleşmelerin fiziksel alanları toplamının altına düşürülemez.",
            "Property.CapacityAreaExceeded");
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
            input.UnitIds?.ToList(),
            input.IncludedUnitId,
            input.IncludeCapacityBased);
}
