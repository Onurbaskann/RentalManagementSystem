using KiraTakip.Web.Authorization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KiraTakip.Web.TagHelpers;

/// <summary>
/// Renders the element only when the current user has the specified permission.
/// Usage: &lt;button asp-permission="@PermissionCatalog.Lease.Create"&gt;Kaydet&lt;/button&gt;
/// </summary>
[HtmlTargetElement("*", Attributes = "asp-permission")]
public class PermissionTagHelper : TagHelper
{
    private readonly ICurrentUserPermissionService _permissionService;

    [HtmlAttributeName("asp-permission")]
    public string? Permission { get; set; }

    public PermissionTagHelper(ICurrentUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Permission))
            return;

        var hasPermission = await _permissionService.HasPermissionAsync(Permission);

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
