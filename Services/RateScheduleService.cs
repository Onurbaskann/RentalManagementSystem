using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;

namespace KiraTakip.Services;

public class RateScheduleService(
    IRateScheduleRepository rateScheduleRepository,
    ICategoryRepository categoryRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitTypeRepository unitTypeRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IUnitOfWork uow
) : IRateScheduleService
{
    public async Task<List<RateYearSummaryDto>> GetYearSummariesAsync()
    {
        var all = await rateScheduleRepository.GetAllAsync();
        return all.GroupBy(k => k.Year)
            .Select(g => new RateYearSummaryDto(g.Key, g.Any(k => k.IsActive), g.Count()))
            .OrderByDescending(o => o.Year)
            .ToList();
    }

    public Task<PagedResult<RateYearSummaryDto>> GetYearSummariesPagedAsync(TableQuery query)
        => rateScheduleRepository.GetYearSummariesPagedAsync(query);

    public async Task<List<int>> GetExistingYearsAsync()
    {
        var all = await rateScheduleRepository.GetAllAsync();
        return all.Select(k => k.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
    }

    public async Task<RateMatrixDto?> GetMatrixAsync(int year)
    {
        var kalemler = await rateScheduleRepository.GetAllAsync(k => k.Year == year);
        if (kalemler.Count == 0) return null;

        var kategoriler = await categoryRepository.GetAllAsync(k => k.Type == CategoryType.Tenant && k.IsActive);
        kategoriler = kategoriler.OrderBy(k => k.Order).ToList();

        var borcTipleri = await chargeTypeRepository.GetAllAsync(b =>
            b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific);
        borcTipleri = borcTipleri.OrderBy(b => b.SortOrder).ToList();

        var columns = borcTipleri.Select(bt => new RateMatrixColumnDto(bt.Id, bt.Name, bt.Code)).ToList();

        var rows = kategoriler.Select(kat => new RateMatrixRowDto(
            kat.Id,
            kat.Name,
            borcTipleri.Select(bt =>
            {
                var mevcut = kalemler.FirstOrDefault(k =>
                    k.TenantCategoryId == kat.Id && k.ChargeTypeId == bt.Id);
                return new RateMatrixCellDto(
                    mevcut?.Id ?? 0,
                    kat.Id,
                    bt.Id,
                    mevcut?.CalculationMethod ?? CalculationMethod.Fixed,
                    mevcut?.UnitValue ?? 0,
                    mevcut?.KdvRate ?? 0
                );
            }).ToList()
        )).ToList();

        var rezervasyonBirimTurleri = await unitTypeRepository.GetAllAsync(t =>
            t.IsActive && t.Usage == UnitTypeUsage.Reservable);
        rezervasyonBirimTurleri = rezervasyonBirimTurleri.OrderBy(t => t.SortOrder).ToList();

        var mevcutRezervasyonlar = await reservationRateOverrideRepository.GetAllAsync(r => r.UnitId == null && r.Year == year);

        var reservationRows = rezervasyonBirimTurleri.Select(bt =>
        {
            var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.UnitTypeId == bt.Id);
            return new RateMatrixReservationRowDto(
                mevcut?.Id ?? 0,
                bt.Id,
                bt.Name,
                mevcut?.FreeDurationMinutes ?? 0,
                mevcut?.BillingPeriodMinutes ?? 60,
                mevcut?.PeriodRate ?? 0,
                mevcut?.KdvRate ?? 20
            );
        }).ToList();

        return new RateMatrixDto(year, kalemler.Any(k => k.IsActive), columns, rows, reservationRows);
    }

    public async Task SaveMatrixAsync(int year, SaveRateMatrixInput input)
    {
        var mevcutKalemler = await rateScheduleRepository.GetAllAsync(k => k.Year == year);
        Guard.NotFound(mevcutKalemler.FirstOrDefault(), "Tarife bulunamadı.");

        var gecerliKategoriIdleri = (await categoryRepository.GetAllAsync(
                k => k.Type == CategoryType.Tenant && k.IsActive))
            .Select(k => k.Id)
            .ToHashSet();
        var gecerliBorcTipiIdleri = (await chargeTypeRepository.GetAllAsync(b =>
                b.IsActive
                && b.Behavior != ChargeTypeBehavior.UserManual
                && b.Behavior != ChargeTypeBehavior.ReservationSpecific))
            .Select(b => b.Id)
            .ToHashSet();
        Guard.Against(
            input.Cells.Any(cell =>
                !gecerliKategoriIdleri.Contains(cell.TenantCategoryId)
                || !gecerliBorcTipiIdleri.Contains(cell.ChargeTypeId)),
            "Geçersiz tarife kalemi gönderildi.");

        var gecerliRezervasyonBirimTuruIdleri = (await unitTypeRepository.GetAllAsync(
                t => t.IsActive && t.Usage == UnitTypeUsage.Reservable))
            .Select(t => t.Id)
            .ToHashSet();
        Guard.Against(
            input.ReservationCells.Any(cell =>
                !gecerliRezervasyonBirimTuruIdleri.Contains(cell.UnitTypeId)),
            "Geçersiz rezervasyon tarife kalemi gönderildi.");

        foreach (var cell in input.Cells)
        {
            var mevcut = mevcutKalemler.FirstOrDefault(k =>
                k.TenantCategoryId == cell.TenantCategoryId && k.ChargeTypeId == cell.ChargeTypeId);
            if (mevcut == null)
            {
                await rateScheduleRepository.AddAsync(new RateSchedule
                {
                    Year = year,
                    TenantCategoryId = cell.TenantCategoryId,
                    ChargeTypeId = cell.ChargeTypeId,
                    CalculationMethod = cell.CalculationMethod,
                    UnitValue = cell.UnitValue,
                    KdvRate = cell.KdvRate
                });
            }
            else
            {
                mevcut.CalculationMethod = cell.CalculationMethod;
                mevcut.UnitValue = cell.UnitValue;
                mevcut.KdvRate = cell.KdvRate;
            }
        }

        var mevcutRezervasyonlar = await reservationRateOverrideRepository.GetAllAsync(r => r.UnitId == null && r.Year == year);

        foreach (var rez in input.ReservationCells)
        {
            var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.UnitTypeId == rez.UnitTypeId);
            if (mevcut == null)
            {
                await reservationRateOverrideRepository.AddAsync(new ReservationRateOverride
                {
                    Year = year,
                    UnitTypeId = rez.UnitTypeId,
                    FreeDurationMinutes = rez.FreeDurationMinutes,
                    BillingPeriodMinutes = rez.BillingPeriodMinutes,
                    PeriodRate = rez.PeriodRate,
                    KdvRate = rez.KdvRate
                });
            }
            else
            {
                mevcut.FreeDurationMinutes = rez.FreeDurationMinutes;
                mevcut.BillingPeriodMinutes = rez.BillingPeriodMinutes;
                mevcut.PeriodRate = rez.PeriodRate;
                mevcut.KdvRate = rez.KdvRate;
            }
        }

        await uow.SaveChangesAsync();
    }

    public async Task CreateYearAsync(CreateRateYearInput input)
    {
        var yearExists = await rateScheduleRepository.AnyAsync(k => k.Year == input.Year)
            || await reservationRateOverrideRepository.AnyAsync(r =>
                r.UnitId == null && r.Year == input.Year);
        Guard.InvalidField(
            yearExists,
            nameof(input.Year),
            "Bu yıl için zaten tarife mevcut.");

        if (input.CopyFromYear.HasValue)
        {
            var kaynakKalemler = await rateScheduleRepository.GetAllAsync(k => k.Year == input.CopyFromYear.Value);
            Guard.InvalidField(
                kaynakKalemler.Count == 0,
                nameof(input.CopyFromYear),
                "Kopyalanacak tarife yılı bulunamadı.");

            foreach (var kalem in kaynakKalemler)
            {
                await rateScheduleRepository.AddAsync(new RateSchedule
                {
                    Year = input.Year,
                    TenantCategoryId = kalem.TenantCategoryId,
                    ChargeTypeId = kalem.ChargeTypeId,
                    CalculationMethod = kalem.CalculationMethod,
                    UnitValue = kalem.UnitValue,
                    KdvRate = kalem.KdvRate
                });
            }

            var kaynakRezervasyonlar = await reservationRateOverrideRepository.GetAllAsync(r => r.UnitId == null && r.Year == input.CopyFromYear.Value);
            foreach (var rez in kaynakRezervasyonlar)
            {
                await reservationRateOverrideRepository.AddAsync(new ReservationRateOverride
                {
                    Year = input.Year,
                    UnitTypeId = rez.UnitTypeId,
                    FreeDurationMinutes = rez.FreeDurationMinutes,
                    BillingPeriodMinutes = rez.BillingPeriodMinutes,
                    PeriodRate = rez.PeriodRate,
                    KdvRate = rez.KdvRate
                });
            }
        }
        else
        {
            var kategoriler = await categoryRepository.GetAllAsync(k => k.Type == CategoryType.Tenant && k.IsActive);
            kategoriler = kategoriler.OrderBy(k => k.Order).ToList();

            var aktifBorcTipleri = await chargeTypeRepository.GetAllAsync(b =>
                b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific);
            aktifBorcTipleri = aktifBorcTipleri.OrderBy(b => b.SortOrder).ToList();

            Guard.Conflict(
                kategoriler.Count == 0 || aktifBorcTipleri.Count == 0,
                "Aktif kiracı kategorisi veya borç tipi bulunmuyor.");

            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    await rateScheduleRepository.AddAsync(new RateSchedule
                    {
                        Year = input.Year,
                        TenantCategoryId = kat.Id,
                        ChargeTypeId = bt.Id,
                        CalculationMethod = CalculationMethod.Fixed,
                        UnitValue = 0,
                        KdvRate = 0
                    });
                }
            }

            var rezBirimTurleri = await unitTypeRepository.GetAllAsync(t => t.IsActive && t.Usage == UnitTypeUsage.Reservable);
            foreach (var bt in rezBirimTurleri)
            {
                await reservationRateOverrideRepository.AddAsync(new ReservationRateOverride
                {
                    Year = input.Year,
                    UnitTypeId = bt.Id,
                    FreeDurationMinutes = 0,
                    BillingPeriodMinutes = 60,
                    PeriodRate = 0,
                    KdvRate = 0
                });
            }
        }

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int year)
    {
        var kalemler = await rateScheduleRepository.GetAllAsync(k => k.Year == year);
        Guard.NotFound(kalemler.FirstOrDefault(), "Tarife bulunamadı.");

        var yeniDeger = !kalemler.First().IsActive;

        foreach (var k in kalemler)
        {
            k.IsActive = yeniDeger;
        }

        var rezler = await reservationRateOverrideRepository.GetAllAsync(r => r.UnitId == null && r.Year == year);
        foreach (var r in rezler)
        {
            r.IsActive = yeniDeger;
        }

        await uow.SaveChangesAsync();
        return yeniDeger;
    }
}
