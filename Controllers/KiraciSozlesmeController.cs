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

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Authorize(Policy = PermissionCatalog.TenantPortal.Lease.Module)]
[Route("Kiraci/Sozlesmeler")]
public class KiraciSozlesmeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly ISozlesmeService _sozlesmeService;
    private readonly IIstatistikService _istatistik;
    private readonly ITahakkukService _tahakkukService;
    private readonly IBelgeService _belgeService;

    public KiraciSozlesmeController(
        ApplicationDbContext db,
        ICurrentUserContext currentUser,
        ISozlesmeService sozlesmeService,
        IIstatistikService istatistik,
        ITahakkukService tahakkukService,
        IBelgeService belgeService)
    {
        _db = db;
        _currentUser = currentUser;
        _sozlesmeService = sozlesmeService;
        _istatistik = istatistik;
        _tahakkukService = tahakkukService;
        _belgeService = belgeService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var sozlesmeler = await _sozlesmeService.GetByKiraciIdAsync(kiraciId);
        return View(sozlesmeler);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        var kiraciId = _currentUser.KiraciId!.Value;
        if (s.KiraciId != kiraciId) return Forbid();

        var dummySozlesme = new Sozlesme
        {
            Id = s.Id,
            KiraciId = s.KiraciId,
            BirimId = s.BirimId,
            BaslangicTarihi = s.BaslangicTarihi,
            BitisTarihi = s.BitisTarihi,
            Durum = s.Durum,
            FesihTarihi = s.FesihTarihi,
            Birim = new Birim
            {
                Id = s.BirimId,
                Yuzolcumu = s.BirimYuzolcumu,
                UnitKind = s.UnitKind,
                TasinmazId = s.TasinmazId
            }
        };

        var vm = new SozlesmeDetayViewModel
        {
            Sozlesme = s,
            KalanGun = _istatistik.KalanGun(dummySozlesme),
            AylikBedel = await _istatistik.AylikBedelAsync(dummySozlesme),
            YillikBedel = await _istatistik.YillikBedelAsync(dummySozlesme),
            Aktif = _istatistik.Aktif(dummySozlesme),
            SureYuzdesi = _istatistik.SureYuzdesi(dummySozlesme),
            Durum = _istatistik.GetBirimDurumu(dummySozlesme.Birim),
            HasOdemeAccess = true,
            KdvOraniEtkin = s.SozlesmeTarifeler
                .FirstOrDefault(r => r.BorcTipiDavranis == ChargeTypeBehavior.MonthlyFixed)?.KdvOrani ?? 20m
        };

        await _tahakkukService.GecikmeleriGuncelleAsync();
        vm.Tahakkuklar = await _tahakkukService.GetListAsync(sozlesmeId: id);

        var bugun = DateTime.Today;
        var guncelTahakkuk = await _db.Tahakkuklar
            .Include(t => t.Kalemler).ThenInclude(k => k.BorcTipi)
            .Where(t => t.KiraSozlesmesiId == id && t.Durum != ChargeStatus.Cancelled && t.DonemBaslangic <= bugun)
            .OrderByDescending(t => t.DonemBaslangic)
            .FirstOrDefaultAsync();
        vm.GuncelKalemler = guncelTahakkuk?.Kalemler
            .Where(k => k.BorcTipi.Davranis == ChargeTypeBehavior.MonthlyFixed)
            .OrderBy(k => k.BorcTipi.Sira).ToList() ?? new();
        vm.GuncelKalemDonemi = guncelTahakkuk?.DonemBaslangic;

        var depozitoTutarlari = await _sozlesmeService.GetDepozitoTutarlariAsync(new[] { id });
        vm.DepozitoTutari = depozitoTutarlari.TryGetValue(id, out var dep) ? dep : null;

        vm.BelgeTurleri = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Sozlesme);
        vm.Belgeler     = await _belgeService.GetListAsync(BelgeOwnerTipi.Sozlesme, id);

        return View(vm);
    }
}
