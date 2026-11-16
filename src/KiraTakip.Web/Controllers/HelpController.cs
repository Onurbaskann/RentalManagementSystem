using KiraTakip.Authorization;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Help;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Web.Controllers;

[Authorize]
public class HelpController : Controller
{
    public IActionResult Index()
    {
        var isTenant = User.FindFirst(AppClaimTypes.UserType)?.Value == ((int)UserType.Tenant).ToString();
        return View(new HelpIndexViewModel { IsTenant = isTenant, Sections = HelpSections.For(isTenant) });
    }
}
