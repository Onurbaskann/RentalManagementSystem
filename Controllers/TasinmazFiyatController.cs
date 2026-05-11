using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.ViewModels;
using KiraTakip.Authorization;
using Microsoft.AspNetCore.Identity;
using KiraTakip.Data;
using KiraTakip.Models;

namespace KiraTakip.Controllers
{
    [Authorize]
    [Route("Tasinmaz/{tasinmazId}/Parametreler")]
    public class TasinmazFiyatController : Controller
    {
        private readonly ITasinmazFiyatService _fiyatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _ctx;

        public TasinmazFiyatController(ITasinmazFiyatService fiyatService,
                                      UserManager<ApplicationUser> userManager,
                                      ApplicationDbContext ctx)
        {
            _fiyatService = fiyatService;
            _userManager = userManager;
            _ctx = ctx;
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
