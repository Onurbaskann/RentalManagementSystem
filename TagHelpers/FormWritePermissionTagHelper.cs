using KiraTakip.Authorization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KiraTakip.TagHelpers;

/// <summary>
/// When the current user lacks the specified write permission, wraps the form body
/// in a disabled fieldset and prepends a read-only warning banner.
/// Usage: &lt;form asp-form-write-permission="@PermissionCatalog.Sozlesme.Create"&gt;
/// SistemYoneticisi (IsSuperAdmin claim) bypasses the check.
/// </summary>
[HtmlTargetElement("form", Attributes = "asp-form-write-permission")]
public class FormWritePermissionTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    [HtmlAttributeName("asp-form-write-permission")]
    public string? Permission { get; set; }

    public FormWritePermissionTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("asp-form-write-permission");

        if (string.IsNullOrWhiteSpace(Permission)) return;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return;

        var isSuperAdmin = user.HasClaim("IsSuperAdmin", "true");
        var hasPermission = isSuperAdmin || user.HasClaim(AppClaimTypes.Permission, Permission);

        if (hasPermission) return;

        const string banner =
            "<div class=\"bg-amber-50 border border-amber-200 text-amber-800 p-3 rounded-lg mb-4 text-[13.5px] flex items-center gap-2\">" +
            "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"w-4 h-4 shrink-0\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\">" +
            "<path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" " +
            "d=\"M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z\"/>" +
            "</svg>" +
            "Bu kaydı görüntüleyebilirsiniz ancak değiştiremezsiniz." +
            "</div>";

        output.PreContent.AppendHtml("<fieldset disabled style=\"border:none;padding:0;margin:0;\">" + banner);
        output.PostContent.AppendHtml("</fieldset>");
    }
}
