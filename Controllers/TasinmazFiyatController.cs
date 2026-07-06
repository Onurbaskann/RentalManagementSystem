using KiraTakip.Authorization;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers
{
    [Authorize]
    [Route("Property/{propertyId}/Parametreler")]
    public class TasinmazFiyatController : Controller
    {
        private readonly IPropertyPricingService _fiyatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasinmazFiyatController(IPropertyPricingService fiyatService,
                                      UserManager<ApplicationUser> userManager)
        {
            _fiyatService = fiyatService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCatalog.Property.Module)]
        public async Task<IActionResult> Index(int propertyId, int page = 1, int pageSize = 10)
        {
            var vm = await _fiyatService.GetMatrisiAsync(propertyId, page, pageSize);
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(vm);
        }
    }
}
