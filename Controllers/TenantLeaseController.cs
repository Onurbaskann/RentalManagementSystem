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
    private readonly ILeaseService _leaseService;
    private readonly IStatisticsService _istatistik;
    private readonly IChargeService _chargeService;
    private readonly IDocumentService _documentService;

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
        _leaseService = leaseService;
        _istatistik = istatistik;
        _chargeService = tahakkukService;
        _documentService = documentService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = _currentUser.TenantId!.Value;
        var sozlesmeler = await _leaseService.GetByTenantIdAsync(tenantId);
        return View(sozlesmeler);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _leaseService.GetByIdAsync(id);
        if (s == null) return NotFound();

        var tenantId = _currentUser.TenantId!.Value;
        if (s.TenantId != tenantId) return Forbid();

        var lease = new Lease
        {
            Id = s.Id,
            TenantId = s.TenantId,
            UnitId = s.UnitId,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status,
            TerminationDate = s.TerminationDate,
            Unit = new Unit
            {
                Id = s.UnitId,
                Area = s.UnitArea,
                PropertyId = s.PropertyId
            }
        };

        var vm = new SozlesmeDetayViewModel
        {
            Lease = s,
            KalanGun = _istatistik.KalanGun(lease),
            AylikBedel = await _istatistik.AylikBedelAsync(lease),
            YillikBedel = await _istatistik.YillikBedelAsync(lease),
            Aktif = _istatistik.Aktif(lease),
            SureYuzdesi = _istatistik.SureYuzdesi(lease),
            Durum = _istatistik.GetBirimDurumu(lease.Unit),
            HasOdemeAccess = true,
            KdvOraniEtkin = s.LeaseRateOverrides
                .FirstOrDefault(r => r.ChargeTypeBehavior == ChargeTypeBehavior.MonthlyFixed)?.KdvRate ?? 20m
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

        var depozitoTutarlari = await _leaseService.GetDepozitoTutarlariAsync(new[] { id });
        vm.DepozitoTutari = depozitoTutarlari.TryGetValue(id, out var dep) ? dep : null;

        vm.DocumentTypes = await _documentService.GetTurlerAsync(DocumentOwnerType.Lease);
        vm.Belgeler     = await _documentService.GetListAsync(DocumentOwnerType.Lease, id);

        return View(vm);
    }
}
