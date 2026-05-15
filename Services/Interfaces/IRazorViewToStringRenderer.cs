namespace KiraTakip.Services.Interfaces;

public interface IRazorViewToStringRenderer
{
    Task<string> RenderAsync<TModel>(string viewName, TModel model);
}
