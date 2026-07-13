using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStatisticsService _istatistikService;
    private readonly ApplicationDbContext _ctx;
    private readonly IUnitRepository _unitRepository;
    private readonly IReservationRateOverrideRepository _rezervasyonTarifeRepository;

    public PropertyService(
        IPropertyRepository repo,
        IUnitOfWork uow,
        IStatisticsService statisticsService,
        ApplicationDbContext ctx,
        IUnitRepository unitRepository,
        IReservationRateOverrideRepository rezervasyonTarifeRepository)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = statisticsService;
        _ctx = ctx;
        _unitRepository = unitRepository;
        _rezervasyonTarifeRepository = rezervasyonTarifeRepository;
    }

    public async Task<List<TasinmazListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tasinmazIds?.ToList());
    }

    public async Task<PropertyDetailDto?> GetByIdAsync(int id)
    {
        var dto = await _repo.GetDetayAsync(id);
        if (dto == null) return null;

        // Birimlerin aktif sözleşmelerinin aylık bedellerini hesapla
        foreach (var b in dto.Units)
        {
            if (b.ActiveLeaseId.HasValue)
            {
                var lease = new Lease
                {
                    Id = b.ActiveLeaseId.Value,
                    TenantId = b.ActiveLeaseTenantId ?? 0,
                    UnitId = b.Id,
                    Unit = new Unit { Id = b.Id, Area = b.Area }
                };
                b.MonthlyRent = await _istatistikService.AylikBedelAsync(lease);
            }
        }

        // Sözleşme geçmişindeki sözleşmelerin aylık bedellerini hesapla
        foreach (var s in dto.LeaseHistory)
        {
            var birimYuzolcumu = dto.Units.FirstOrDefault(b => b.Id == s.UnitId)?.Area ?? 0m;
            var lease = new Lease
            {
                Id = s.Id,
                TenantId = s.TenantId,
                UnitId = s.UnitId,
                Unit = new Unit { Id = s.UnitId, Area = birimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(lease);
        }

        return dto;
    }

    public async Task<Property> CreateAsync(Property t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null, int? singleUnitTypeId = null)
    {
        if (t.UnitStructure == UnitStructure.SingleUnit)
        {
            if (!singleUnitTypeId.HasValue)
                throw new InvalidOperationException("Tek birim yapısı için birim türü zorunludur.");

            t.Units.Add(new Unit
            {
                Name = t.Name,
                Area = t.ClosedArea > 0 ? t.ClosedArea : t.OpenArea,
                UnitTypeId = singleUnitTypeId.Value
            });
        }
        else
        {
            foreach (var b in birimler ?? [])
            {
                t.Units.Add(new Unit
                {
                    UnitNo = b.UnitNo,
                    FloorNo = b.FloorNo,
                    Name = string.IsNullOrWhiteSpace(b.Name) ? $"Birim {b.UnitNo}" : b.Name,
                    Area = b.Area,
                    Description = b.Description,
                    UnitTypeId = b.UnitTypeId!.Value
                });
            }

            foreach (var r in rezervasyonAlanlari ?? [])
            {
                var unit = new Unit
                {
                    UnitNo = r.UnitNo,
                    Name = string.IsNullOrWhiteSpace(r.Name) ? "Rezervasyon Alanı" : r.Name,
                    Area = r.Area,
                    Description = r.Description,
                    UnitTypeId = r.UnitTypeId!.Value
                };
                t.Units.Add(unit);
                await _repo.AddReservationRateOverrideAsync(new ReservationRateOverride
                {
                    Unit = unit,
                    FreeDurationMinutes = r.FreeDurationMinutes,
                    BillingPeriodMinutes = 60,
                    PeriodRate = r.SaatlikUcret,
                    KdvRate = r.KdvRate,
                    Description = $"{r.Name} için otomatik oluşturuldu"
                });
            }
        }

        await _repo.AddAsync(t);
        await _uow.SaveChangesAsync();
        return t;
    }
    public async Task UpdateAsync(Property t)
    {
        await _repo.UpdateAsync(t);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> CanChangeUnitStructureAsync(int propertyId)
    {
        var unitIds = await _ctx.Units.IgnoreQueryFilters()
            .Where(u => u.PropertyId == propertyId)
            .Select(u => u.Id)
            .ToListAsync();

        if (unitIds.Count == 0) return true;

        return !await _ctx.Leases.IgnoreQueryFilters().AnyAsync(x => unitIds.Contains(x.UnitId))
            && !await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(x => unitIds.Contains(x.UnitId))
            && !await _ctx.Charges.IgnoreQueryFilters().AnyAsync(x => unitIds.Contains(x.UnitId));
    }

    public async Task<TasinmazDuzenleViewModel?> GetForEditAsync(int id)
    {
        var t = await _repo.GetWithBirimlerTrackedAsync(id);
        if (t == null) return null;

        var now = DateTime.Now;
        var unitIds = t.Units.Select(b => b.Id).ToList();
        var reservationRates = await _ctx.RezervasyonTarifeler
            .Where(rt => rt.UnitId != null && unitIds.Contains(rt.UnitId.Value) && rt.IsActive)
            .ToDictionaryAsync(rt => rt.UnitId!.Value);
        var activeReservationUnitIds = await _ctx.Reservations
            .Where(r => unitIds.Contains(r.UnitId) && r.Status == ReservationStatus.Planned && r.EndDate >= now)
            .Select(r => r.UnitId)
            .Distinct()
            .ToListAsync();

        var units = new List<BirimDuzenleViewModel>();
        var reservationAreas = new List<RezervasyonAlaniDuzenleViewModel>();

        if (t.UnitStructure == UnitStructure.MultipleUnits)
        {
            foreach (var unit in t.Units)
            {
                if (unit.UnitType.Usage == UnitTypeUsage.Reservable)
                {
                    reservationRates.TryGetValue(unit.Id, out var rate);
                    reservationAreas.Add(new RezervasyonAlaniDuzenleViewModel
                    {
                        Id = unit.Id,
                        UnitNo = unit.UnitNo ?? string.Empty,
                        Name = unit.Name,
                        Area = unit.Area,
                        UnitTypeId = unit.UnitTypeId,
                        Description = unit.Description,
                        FreeDurationMinutes = rate?.FreeDurationMinutes ?? 0,
                        SaatlikUcret = rate?.PeriodRate ?? 0,
                        KdvRate = rate?.KdvRate ?? 20,
                        AktifRezervasyonuVar = activeReservationUnitIds.Contains(unit.Id)
                    });
                }
                else
                {
                    units.Add(new BirimDuzenleViewModel
                    {
                        Id = unit.Id,
                        UnitNo = unit.UnitNo ?? string.Empty,
                        FloorNo = unit.FloorNo,
                        Name = unit.Name,
                        Area = unit.Area,
                        Description = unit.Description,
                        UnitTypeId = unit.UnitTypeId,
                        AktifSozlesmesiVar = unit.Leases.Any()
                    });
                }
            }
        }

        var singleUnit = t.UnitStructure == UnitStructure.SingleUnit ? t.Units.SingleOrDefault() : null;
        return new TasinmazDuzenleViewModel
        {
            Id = t.Id,
            Ad = t.Name,
            TasinmazTipiId = t.PropertyTypeId,
            UnitStructure = t.UnitStructure,
            BirimYapisiDegistirilebilir = await CanChangeUnitStructureAsync(t.Id),
            KompleUnitTypeId = singleUnit?.UnitTypeId,
            Il = t.City,
            Ilce = t.District,
            Mahalle = t.Neighborhood,
            AcikAdres = t.Address,
            AcikYuzolcumu = t.OpenArea,
            KapaliYuzolcumu = t.ClosedArea,
            KatSayisi = t.FloorCount,
            Aciklama = t.Description,
            Units = units,
            RezervasyonAlanlari = reservationAreas
        };
    }
    public async Task UpdateWithChildrenAsync(TasinmazDuzenleViewModel vm)
    {
        var property = await _repo.GetWithBirimlerTrackedAsync(vm.Id);
        if (property == null) return;

        var structureChanged = property.UnitStructure != vm.UnitStructure;
        if (structureChanged && !await CanChangeUnitStructureAsync(property.Id))
            throw new InvalidOperationException("Sözleşme, rezervasyon veya tahakkuk geçmişi bulunan taşınmazın birim yapısı değiştirilemez.");

        property.Name = vm.Ad;
        property.PropertyTypeId = vm.TasinmazTipiId;
        property.City = vm.Il;
        property.District = vm.Ilce;
        property.Neighborhood = vm.Mahalle;
        property.Address = vm.AcikAdres;
        property.OpenArea = vm.AcikYuzolcumu;
        property.ClosedArea = vm.KapaliYuzolcumu;
        property.FloorCount = vm.KatSayisi;
        property.Description = vm.Aciklama;

        if (structureChanged)
        {
            var oldUnitIds = property.Units.Select(u => u.Id).ToList();
            var oldUnitRates = await _ctx.UnitRates.IgnoreQueryFilters()
                .Where(r => oldUnitIds.Contains(r.UnitId)).ToListAsync();
            var oldReservationRates = await _ctx.RezervasyonTarifeler.IgnoreQueryFilters()
                .Where(r => r.UnitId.HasValue && oldUnitIds.Contains(r.UnitId.Value)).ToListAsync();
            _ctx.UnitRates.RemoveRange(oldUnitRates);
            _ctx.RezervasyonTarifeler.RemoveRange(oldReservationRates);
            _ctx.Units.RemoveRange(property.Units);
            property.Units.Clear();
            property.UnitStructure = vm.UnitStructure;
        }

        if (property.UnitStructure == UnitStructure.SingleUnit)
        {
            if (!vm.KompleUnitTypeId.HasValue)
                throw new InvalidOperationException("Tek birim yapısı için birim türü zorunludur.");

            var unit = property.Units.SingleOrDefault();
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }

            unit.Name = property.Name;
            unit.UnitNo = null;
            unit.FloorNo = null;
            unit.Area = vm.KapaliYuzolcumu > 0 ? vm.KapaliYuzolcumu : vm.AcikYuzolcumu;
            unit.Description = vm.Aciklama;
            unit.UnitTypeId = vm.KompleUnitTypeId.Value;
        }
        else
        {
            await UpdateMultipleUnitsAsync(property, vm);
        }

        await _uow.SaveChangesAsync();
    }

    private async Task UpdateMultipleUnitsAsync(Property property, TasinmazDuzenleViewModel vm)
    {
        var unitIds = property.Units.Select(u => u.Id).ToList();
        var reservationRates = await _ctx.RezervasyonTarifeler
            .Where(r => r.UnitId.HasValue && unitIds.Contains(r.UnitId.Value))
            .ToDictionaryAsync(r => r.UnitId!.Value);
        var normalUnits = property.Units.Where(u => u.UnitType.Usage != UnitTypeUsage.Reservable).ToList();
        var reservationUnits = property.Units.Where(u => u.UnitType.Usage == UnitTypeUsage.Reservable).ToList();

        var incomingNormalIds = vm.Units.Where(u => u.Id.HasValue).Select(u => u.Id!.Value).ToHashSet();
        foreach (var existing in normalUnits.Where(u => !incomingNormalIds.Contains(u.Id)))
        {
            if (await HasHistoricalDependencyAsync(existing.Id))
                throw new InvalidOperationException($"'{existing.Name}' biriminin işlem geçmişi bulunduğu için silinemez.");
            var rates = await _ctx.UnitRates.Where(r => r.UnitId == existing.Id).ToListAsync();
            _ctx.UnitRates.RemoveRange(rates);
            _ctx.Units.Remove(existing);
        }

        foreach (var input in vm.Units)
        {
            var name = string.IsNullOrWhiteSpace(input.Name) ? $"Birim {input.UnitNo}" : input.Name;
            var unit = input.Id.HasValue
                ? property.Units.FirstOrDefault(u => u.Id == input.Id.Value)
                : null;
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }
            unit.UnitNo = input.UnitNo;
            unit.FloorNo = input.FloorNo;
            unit.Name = name;
            unit.Area = input.Area;
            unit.Description = input.Description;
            unit.UnitTypeId = input.UnitTypeId!.Value;
        }

        var incomingReservationIds = vm.RezervasyonAlanlari.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();
        foreach (var existing in reservationUnits.Where(u => !incomingReservationIds.Contains(u.Id)))
        {
            if (await HasHistoricalDependencyAsync(existing.Id))
                throw new InvalidOperationException($"'{existing.Name}' rezervasyon biriminin işlem geçmişi bulunduğu için silinemez.");
            if (reservationRates.TryGetValue(existing.Id, out var rate))
                _ctx.RezervasyonTarifeler.Remove(rate);
            _ctx.Units.Remove(existing);
        }

        foreach (var input in vm.RezervasyonAlanlari)
        {
            var unit = input.Id.HasValue
                ? property.Units.FirstOrDefault(u => u.Id == input.Id.Value)
                : null;
            if (unit == null)
            {
                unit = new Unit();
                property.Units.Add(unit);
            }
            unit.UnitNo = input.UnitNo;
            unit.FloorNo = null;
            unit.Name = input.Name ?? "Rezervasyon Alanı";
            unit.Area = input.Area;
            unit.Description = input.Description;
            unit.UnitTypeId = input.UnitTypeId!.Value;

            if (!reservationRates.TryGetValue(unit.Id, out var rate))
            {
                rate = new ReservationRateOverride { Unit = unit, BillingPeriodMinutes = 60 };
                await _ctx.RezervasyonTarifeler.AddAsync(rate);
            }
            rate.FreeDurationMinutes = input.FreeDurationMinutes;
            rate.PeriodRate = input.SaatlikUcret;
            rate.KdvRate = input.KdvRate;
            rate.Description = $"{input.Name} için otomatik oluşturuldu";
        }
    }

    private async Task<bool> HasHistoricalDependencyAsync(int unitId)
    {
        return await _ctx.Leases.IgnoreQueryFilters().AnyAsync(x => x.UnitId == unitId)
            || await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(x => x.UnitId == unitId)
            || await _ctx.Charges.IgnoreQueryFilters().AnyAsync(x => x.UnitId == unitId);
    }
    public async Task<List<UnitLookupDto>> GetBosBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetBosBirimlerAsync(tasinmazIds?.ToList());
    }
}
