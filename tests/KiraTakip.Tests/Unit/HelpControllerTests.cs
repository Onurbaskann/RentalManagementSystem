using System.Security.Claims;
using KiraTakip.Authorization;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.Help;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Tests;

public class HelpControllerTests
{
    private static HelpIndexViewModel Run(UserType type)
    {
        var identity = new ClaimsIdentity([new Claim(AppClaimTypes.UserType, ((int)type).ToString())], "test");
        var controller = new HelpController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
        };
        return Assert.IsType<HelpIndexViewModel>(Assert.IsType<ViewResult>(controller.Index()).Model);
    }

    [Fact]
    public void Tenant_SeesNoAdminSections()
    {
        var model = Run(UserType.Tenant);
        Assert.True(model.IsTenant);
        Assert.DoesNotContain(model.Sections, s => s.Audience == HelpAudience.Admin);
    }

    [Fact]
    public void Internal_SeesAllSections()
    {
        var model = Run(UserType.Internal);
        Assert.False(model.IsTenant);
        Assert.Equal(15, model.Sections.Count);
    }

    [Fact]
    public void ForController_Maps()
    {
        Assert.Equal("kiraci-sozlesme-borc", HelpSections.ForController("TenantCharge"));
        Assert.Equal("kiraci-sozlesme-borc", HelpSections.ForController("tenantcharge"));
        Assert.Null(HelpSections.ForController("Help"));
        Assert.Null(HelpSections.ForController(null));
        Assert.Null(HelpSections.ForController("Bilinmeyen"));
        Assert.Equal("yonetim-finans", HelpSections.ForController("Charge"));
        Assert.Equal("yonetim-parametre", HelpSections.ForController("adminrate"));
    }
}
