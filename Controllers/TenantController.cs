using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize]
public class TenantController : Controller
{
    private readonly ITenantService _kiraciService;
    private readonly ILeaseService _sozlesmeService;
    private readonly IStatisticsService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;
    private readonly IRoleService _rolService;
    private readonly IInvitationService _davetiyeService;
    private readonly IPermissionScopeProvider _provider;
    private readonly IDocumentService _belgeService;

    public TenantController(
        ITenantService tenantService,
        ILeaseService leaseService,
        IStatisticsService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx,
        IRoleService roleService,
        IInvitationService invitationService,
        IPermissionScopeProvider provider,
        IDocumentService documentService)
    {
        _kiraciService = tenantService;
        _sozlesmeService = leaseService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
        _rolService = roleService;
        _davetiyeService = invitationService;
        _provider = provider;
        _belgeService = documentService;
    }

    [Authorize(Policy = PermissionCatalog.Tenant.Module)]
    public async Task<IActionResult> Index()
    {
        var propertyIds = _provider.GlobalAccess ? null : _provider.AccessiblePropertyIds;
        var kiraciler = await _kiraciService.GetAllAsync(propertyIds);
        var sozlesmeler = await _sozlesmeService.GetAllAsync(propertyIds: propertyIds);
        ViewBag.AktifSozlesme = sozlesmeler
            .Where(s => s.Aktif)
            .GroupBy(s => s.KiraciId)
            .ToDictionary(g => g.Key, g => g.Count());
        return View(kiraciler);
    }

    [Authorize(Policy = PermissionCatalog.Tenant.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var propertyIds = _provider.GlobalAccess ? null : _provider.AccessiblePropertyIds;
        if (!_provider.GlobalAccess)
        {
            var kiraciler = await _kiraciService.GetAllAsync(propertyIds);
            if (!kiraciler.Any(k => k.Id == id)) return Forbid();
        }

        var k = await _kiraciService.GetDetayAsync(id);
        if (k == null) return NotFound();

        List<SozlesmeListItemDto> sozlesmeler;
        if (!_provider.GlobalAccess)
        {
            var all = await _sozlesmeService.GetAllAsync(propertyIds: propertyIds);
            sozlesmeler = all.Where(s => s.KiraciId == id).ToList();
        }
        else
        {
            sozlesmeler = await _sozlesmeService.GetByTenantIdAsync(id);
        }

        var depozitoTutarlari = await _sozlesmeService.GetDepozitoTutarlariAsync(sozlesmeler.Select(s => s.Id));
        var belgeler = await _belgeService.GetListAsync(Models.Entities.BelgeOwnerTipi.Tenant, id);
        var belgeTurleri = await _belgeService.GetTurlerAsync(Models.Entities.BelgeOwnerTipi.Tenant);

        var vm = new KiraciDetayViewModel
        {
            Tenant = k,
            Leases = sozlesmeler,
            DepozitoTutarlari = depozitoTutarlari,
            Belgeler = belgeler,
            DocumentTypes = belgeTurleri
        };
        return View(vm);
    }

    private async Task PopulateKiraciViewBagAsync()
    {
        ViewBag.Kategoriler = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tenant && k.IsActive).OrderBy(k => k.Sira).ToListAsync();
        ViewBag.Sektorler = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor && k.IsActive).OrderBy(k => k.Sira).ToListAsync();
        ViewBag.DocumentTypes = await _belgeService.GetTurlerAsync(Models.Entities.BelgeOwnerTipi.Tenant);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tenant.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateKiraciViewBagAsync();
        var vm = new KiraciFormViewModel
        {
            KiraciNo = await _kiraciService.GenerateKiraciNoAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tenant.Create)]
    public async Task<IActionResult> Ekle(KiraciFormViewModel vm)
    {
        await ValidateKiraciAsync(vm);

        var belgeTurleri = await _belgeService.GetTurlerAsync(Models.Entities.BelgeOwnerTipi.Tenant);
        foreach (var bt in belgeTurleri.Where(bt => bt.Required))
        {
            var f = Request.Form.Files.GetFile($"dosya_{bt.Id}");
            if (f == null || f.Length == 0)
                ModelState.AddModelError($"dosya_{bt.Id}", $"'{bt.Name}' belgesi zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateKiraciViewBagAsync();
            return View(vm);
        }

        var k = BuildKiraciFromVm(vm);
        await _kiraciService.CreateAsync(k);

        // Yüklenen belgeleri kaydet
        foreach (var bt in belgeTurleri)
        {
            var file = Request.Form.Files.GetFile($"dosya_{bt.Id}");
            if (file == null || file.Length == 0) continue;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            await _belgeService.UploadAsync(
                Models.Entities.BelgeOwnerTipi.Tenant, k.Id, bt.Id,
                file.FileName, file.ContentType, ms.ToArray());
        }

        var currentUserId = _userManager.GetUserId(User)!;

        // İlk firma yetkilisine otomatik davet gönder (e-posta girilmişse)
        if (!string.IsNullOrWhiteSpace(vm.IlkYetkiliEmail))
        {
            try
            {
                var firmaRol = await _ctx.Roller
                    .FirstOrDefaultAsync(r => r.KiraciId == null && r.Ad == RoleNames.KiraciYoneticisi);
                if (firmaRol != null)
                    await _davetiyeService.GonderAsync(vm.IlkYetkiliEmail, vm.IlkYetkiliAdSoyad, firmaRol.Id, currentUserId, k.Id);
                TempData["Success"] = $"'{k.DisplayName}' eklendi ve {vm.IlkYetkiliEmail} adresine davet gönderildi.";
            }
            catch
            {
                TempData["Success"] = $"'{k.DisplayName}' başarıyla eklendi.";
                TempData["Error"] = "İlk yetkili daveti gönderilemedi; Kiracı > Kullanıcılar ekranından tekrar deneyebilirsiniz.";
            }
        }
        else
        {
            TempData["Success"] = $"'{k.DisplayName}' başarıyla eklendi.";
        }

        return RedirectToAction("Detay", new { id = k.Id });
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Tenant.Edit)]
    public async Task<IActionResult> Duzenle(int id)
    {
        var k = await _kiraciService.GetDetayAsync(id);
        if (k == null) return NotFound();

        await PopulateKiraciViewBagAsync();
        var vm = new KiraciFormViewModel
        {
            Id = k.Id,
            KiraciNo = k.KiraciNo,
            Ad = k.Ad,
            TicaretSicilNo = k.TicaretSicilNo,
            VergiNo = k.VergiNo,
            VergiDairesi = k.VergiDairesi,
            MersisNo = k.MersisNo,
            Telefon = k.Telefon,
            Email = k.Email,
            Adres = k.Adres,
            KiraciKategoriId = k.KiraciKategoriId,
            SektorId = k.SektorId
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Tenant.Edit)]
    public async Task<IActionResult> Duzenle(int id, KiraciFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        await ValidateKiraciAsync(vm, excludeId: id);

        if (!ModelState.IsValid)
        {
            await PopulateKiraciViewBagAsync();
            return View(vm);
        }

        var k = BuildKiraciFromVm(vm);
        k.Id = id;
        await _kiraciService.UpdateAsync(k);
        TempData["Success"] = "Kiracı bilgileri güncellendi.";
        return RedirectToAction("Detay", new { id });
    }

    private async Task ValidateKiraciAsync(KiraciFormViewModel vm, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(vm.KiraciNo))
            ModelState.AddModelError("KiraciNo", "Kiracı No zorunludur.");
        else if (await _kiraciService.KiraciNoExistsAsync(vm.KiraciNo, excludeId))
            ModelState.AddModelError("KiraciNo", "Bu Kiracı No zaten kullanımda.");

        if (!vm.KiraciKategoriId.HasValue || vm.KiraciKategoriId <= 0)
            ModelState.AddModelError("KiraciKategoriId", "Kiracı kategorisi seçilmelidir.");

        if (!vm.SektorId.HasValue || vm.SektorId <= 0)
            ModelState.AddModelError("SektorId", "Sektör seçilmelidir.");

        if (string.IsNullOrWhiteSpace(vm.Ad))
            ModelState.AddModelError("Ad", "Firma / Kurum Adı zorunludur.");

        if (string.IsNullOrWhiteSpace(vm.VergiNo))
            ModelState.AddModelError("VergiNo", "Vergi No zorunludur.");
        else if (vm.VergiNo.Length != 10 || !vm.VergiNo.All(char.IsDigit))
            ModelState.AddModelError("VergiNo", "Vergi No 10 haneli rakamdan oluşmalıdır.");

        if (string.IsNullOrWhiteSpace(vm.VergiDairesi))
            ModelState.AddModelError("VergiDairesi", "Vergi Dairesi zorunludur.");

        if (string.IsNullOrWhiteSpace(vm.Telefon))
            ModelState.AddModelError("Telefon", "Telefon zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Email))
            ModelState.AddModelError("Email", "E-posta zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Adres))
            ModelState.AddModelError("Adres", "Adres zorunludur.");
    }

    private static Tenant BuildKiraciFromVm(KiraciFormViewModel vm)
    {
        return new Tenant
        {
            TenantNo = vm.KiraciNo,
            Name = vm.Ad,
            TradeRegistryNo = vm.TicaretSicilNo,
            TaxNo = vm.VergiNo,
            TaxOffice = vm.VergiDairesi,
            MersisNo = vm.MersisNo,
            Phone = vm.Telefon,
            Email = vm.Email,
            Address = vm.Adres,
            TenantCategoryId = vm.KiraciKategoriId,
            SectorId = vm.SektorId
        };
    }
}
