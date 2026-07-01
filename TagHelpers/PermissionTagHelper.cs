using KiraTakip.Authorization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Security.Claims;

namespace KiraTakip.TagHelpers;

/// <summary>
/// Renders the element only when the current user has the specified permission claim.
/// Usage: &lt;button asp-permission="@PermissionCatalog.Sozlesme.Create"&gt;Kaydet&lt;/button&gt;
/// </summary>
[HtmlTargetElement("*", Attributes = "asp-permission")]
public class PermissionTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    [HtmlAttributeName("asp-permission")]
    public string? Permission { get; set; }

    public PermissionTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Permission))
            return;

        var user = _httpContextAccessor.HttpContext?.User;
        var isSuperAdmin = user?.HasClaim("IsSuperAdmin", "true") ?? false;
        var hasPermission = isSuperAdmin || user?.HasClaim(AppClaimTypes.Permission, Permission) == true;

        if (!hasPermission)
        {
            output.SuppressOutput();
        }
        else
        {
            output.Attributes.RemoveAll("asp-permission");
        }
    }
}
