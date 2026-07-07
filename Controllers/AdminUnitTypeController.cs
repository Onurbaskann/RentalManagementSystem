using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/UnitType")]
public class AdminUnitTypeController : Controller
{
    private readonly IUnitTypeRepository _repo;
    private readonly IChargeTypeRepository _borcTipiRepo;
    private readonly IUnitOfWork _uow;

    public AdminUnitTypeController(
        IUnitTypeRepository repo,
        IChargeTypeRepository borcTipiRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _borcTipiRepo = borcTipiRepo;
        _uow = uow;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetListAsync();
        return View(list);
    }

    [HttpGet("Ekle")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = (await _repo.GetMaxSiraAsync()) + 1;
        var vm = new UnitTypeFormViewModel
        {
            SortOrder = nextSira,
            CanBeRented = true,
            ChargeTypeCandidates = await _borcTipiRepo.GetRezervasyonAdaylariAsync()
        };
        return View(vm);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Create)]
    public async Task<IActionResult> Create(UnitTypeFormViewModel model)
    {
        if (model.CanBeReserved && (!model.ChargeTypeId.HasValue || model.ChargeTypeId <= 0))
            ModelState.AddModelError(nameof(model.ChargeTypeId), "Reservation unit türü için borç tipi seçilmelidir.");

        if (model.CanBeRented == model.CanBeReserved)
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Reservation yapılabilir.");

        if (!ModelState.IsValid)
        {
            model.ChargeTypeCandidates = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var kod = CodeSlugger.ToCode(model.Name);
        if (await _repo.KodExistsAsync(kod))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ad zaten kullanılıyor. Farklı bir ad girin.");
            model.ChargeTypeCandidates = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var entity = new UnitType
        {
            Name = model.Name,
            Code = kod,
            SortOrder = model.SortOrder,
            CanBeRented = model.CanBeRented,
            CanBeReserved = model.CanBeReserved,
            ChargeTypeId = model.CanBeRented ? null : model.ChargeTypeId,
            IsActive = model.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' unit türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = ToFormVm(entity);
        vm.ChargeTypeCandidates = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
        return View(vm);
    }

    [HttpPost("Duzenle/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Edit)]
    public async Task<IActionResult> Edit(int id, UnitTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (model.CanBeReserved && (!model.ChargeTypeId.HasValue || model.ChargeTypeId <= 0))
            ModelState.AddModelError(nameof(model.ChargeTypeId), "Reservation unit türü için borç tipi seçilmelidir.");

        if (model.CanBeRented == model.CanBeReserved)
            ModelState.AddModelError(string.Empty,
                "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Reservation yapılabilir.");

        if (!ModelState.IsValid)
        {
            model.ChargeTypeCandidates = await _borcTipiRepo.GetRezervasyonAdaylariAsync();
            return View(model);
        }

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        entity.Name = model.Name;
        entity.SortOrder = model.SortOrder;
        entity.CanBeRented = model.CanBeRented;
        entity.CanBeReserved = model.CanBeReserved;
        entity.ChargeTypeId = model.CanBeRented ? null : model.ChargeTypeId;
        entity.IsActive = model.IsActive;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Edit)]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        if (entity.IsActive) // Pasife çekme
        {
            if (await _repo.HasAktifTahakkukForUnitTypeAsync(id))
            {
                TempData["Error"] = "Bu unit türüne bağlı birimlerde aktif charge bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (await _repo.HasPlanlanmisRezervasyonForUnitTypeAsync(id))
            {
                TempData["Error"] = "Bu unit türüne bağlı birimlerde planlanmış reservation bulunduğu için pasif yapılamaz.";
                return RedirectToAction(nameof(Index));
            }

            // Cascade BorcTipi pasif (başka aktif UnitType kullanmıyorsa)
            if (entity.ChargeTypeId.HasValue)
            {
                var baskaKullananVar = await _repo.AnyAktifByBorcTipiIdAsync(entity.ChargeTypeId.Value, id);
                if (!baskaKullananVar)
                {
                    var borcTipi = await _borcTipiRepo.GetByIdAsync(entity.ChargeTypeId.Value);
                    if (borcTipi != null) borcTipi.IsActive = false;
                }
            }
        }

        entity.IsActive = !entity.IsActive;
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"'{entity.Name}' {(entity.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    private static UnitTypeFormViewModel ToFormVm(UnitType e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        SortOrder = e.SortOrder,
        CanBeRented = e.CanBeRented,
        CanBeReserved = e.CanBeReserved,
        ChargeTypeId = e.ChargeTypeId,
        IsActive = e.IsActive
    };
}
