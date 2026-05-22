using KiraTakip.Authorization;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers
{
    [Authorize]
    [Route("Tasinmaz/{tasinmazId}/Parametreler")]
    public class TasinmazFiyatController : Controller
    {
        private readonly ITasinmazFiyatService _fiyatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasinmazFiyatController(ITasinmazFiyatService fiyatService,
                                      UserManager<ApplicationUser> userManager)
        {
            _fiyatService = fiyatService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCatalog.Tasinmaz.View)]
        public async Task<IActionResult> Index(int tasinmazId, int page = 1, int pageSize = 10)
        {
            var vm = await _fiyatService.GetMatrisiAsync(tasinmazId, page, pageSize);
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(vm);
        }
    }
}
