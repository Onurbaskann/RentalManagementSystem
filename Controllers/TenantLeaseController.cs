using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Extensions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Authorize(Policy = PermissionCatalog.TenantPortal.Lease.Module)]
[Route("Tenant/Leases")]
public class TenantLeaseController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILeaseService _sozlesmeService;
    private readonly IStatisticsService _istatistik;
    private readonly IChargeService _chargeService;
    private readonly IDocumentService _belgeService;

    public TenantLeaseController(
        ApplicationDbContext db,
        ICurrentUserContext currentUser,
        ILeaseService leaseService,
        IStatisticsService istatistik,
        IChargeService tahakkukService,
        IDocumentService documentService)
    {
        _db = db;
        _currentUser = currentUser;
        _sozlesmeService = leaseService;
        _istatistik = istatistik;
        _chargeService = tahakkukService;
        _belgeService = documentService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = _currentUser.KiraciId!.Value;
        var sozlesmeler = await _sozlesmeService.GetByTenantIdAsync(tenantId);
        return View(sozlesmeler);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        var tenantId = _currentUser.KiraciId!.Value;
        if (s.KiraciId != tenantId) return Forbid();

        var dummySozlesme = new Lease
        {
            Id = s.Id,
            TenantId = s.KiraciId,
            UnitId = s.BirimId,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Durum,
            TerminationDate = s.FesihTarihi,
            Unit = new Unit
            {
                Id = s.BirimId,
                Area = s.BirimYuzolcumu,
                UnitKind = s.UnitKind,
                PropertyId = s.TasinmazId
            }
        };

        var vm = new SozlesmeDetayViewModel
        {
            Lease = s,
            KalanGun = _istatistik.KalanGun(dummySozlesme),
            AylikBedel = await _istatistik.AylikBedelAsync(dummySozlesme),
            YillikBedel = await _istatistik.YillikBedelAsync(dummySozlesme),
            Aktif = _istatistik.Aktif(dummySozlesme),
            SureYuzdesi = _istatistik.SureYuzdesi(dummySozlesme),
            Durum = _istatistik.GetBirimDurumu(dummySozlesme.Unit),
            HasOdemeAccess = true,
            KdvOraniEtkin = s.SozlesmeTarifeler
                .FirstOrDefault(r => r.BorcTipiDavranis == ChargeTypeBehavior.MonthlyFixed)?.KdvRate ?? 20m
        };

        await _chargeService.UpdateDelaysAsync();
        vm.Charges = await _chargeService.GetListAsync(leaseId: id);

        var bugun = DateTime.Today;
        var guncelTahakkuk = await _db.Charges
            .Include(t => t.LineItems).ThenInclude(k => k.ChargeType)
            .Where(t => t.LeaseId == id && t.Status != ChargeStatus.Cancelled && t.PeriodStart <= bugun)
            .OrderByDescending(t => t.PeriodStart)
            .FirstOrDefaultAsync();
        vm.GuncelKalemler = guncelTahakkuk?.LineItems
            .Where(k => k.ChargeType.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .OrderBy(k => k.ChargeType.SortOrder).ToList() ?? new();
        vm.GuncelKalemDonemi = guncelTahakkuk?.PeriodStart;

        var depozitoTutarlari = await _sozlesmeService.GetDepozitoTutarlariAsync(new[] { id });
        vm.DepozitoTutari = depozitoTutarlari.TryGetValue(id, out var dep) ? dep : null;

        vm.DocumentTypes = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Lease);
        vm.Belgeler     = await _belgeService.GetListAsync(BelgeOwnerTipi.Lease, id);

        return View(vm);
    }
}
