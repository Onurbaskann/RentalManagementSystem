namespace KiraTakip.Services.Interfaces.Documents;

public interface IRazorViewToStringRenderer
{
    Task<string> RenderAsync<TModel>(string viewName, TModel model);
}
