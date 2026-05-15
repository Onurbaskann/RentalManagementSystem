using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KiraTakip.Services;

public class RazorViewToStringRenderer : IRazorViewToStringRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RazorViewToStringRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> RenderAsync<TModel>(string viewName, TModel model)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? new DefaultHttpContext { RequestServices = _serviceProvider };

        var actionContext = new ActionContext(
            httpContext,
            httpContext.GetRouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        await using var sw = new StringWriter();

        var viewResult = FindView(actionContext, viewName);
        if (!viewResult.Success)
            throw new InvalidOperationException($"Razor view '{viewName}' bulunamadı. Aranan yerler: {string.Join(", ", viewResult.SearchedLocations ?? [])}");

        var viewDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

    private ViewEngineResult FindView(ActionContext actionContext, string viewName)
    {
        var result = _viewEngine.GetView(null, viewName, false);
        if (result.Success) return result;

        result = _viewEngine.FindView(actionContext, viewName, false);
        if (result.Success) return result;

        // Try with full path prefix for email templates
        result = _viewEngine.GetView(null, $"/Views/Shared/EmailTemplates/{viewName}.cshtml", false);
        return result;
    }
}
