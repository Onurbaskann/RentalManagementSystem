using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KiraTakip.Infrastructure;

/// <summary>
/// Servislerin iş kuralı ihlallerinde fırlattığı InvalidOperationException mesajlarını
/// kullanıcıya hata modalında gösterir: mesaj TempData["Error"]'a yazılır ve kullanıcı
/// geldiği sayfaya geri yönlendirilir (_Layout'taki modal mesajı otomatik açar).
/// Diğer exception türleri yakalanmaz; onlar ortama göre developer page / hata sayfasına düşer.
/// </summary>
public class BusinessRuleExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public BusinessRuleExceptionFilter(ITempDataDictionaryFactory tempDataFactory)
    {
        _tempDataFactory = tempDataFactory;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidOperationException ex) return;

        var request = context.HttpContext.Request;

        // JSON/AJAX istekleri redirect yerine mesajı 400 gövdesinde alır.
        if (!request.Headers.Accept.ToString().Contains("text/html"))
        {
            context.Result = new BadRequestObjectResult(ex.Message);
            context.ExceptionHandled = true;
            return;
        }

        var tempData = _tempDataFactory.GetTempData(context.HttpContext);
        tempData["Error"] = ex.Message;

        var referer = request.Headers.Referer.ToString();
        context.Result = string.IsNullOrEmpty(referer)
            ? new RedirectToActionResult("Index", "Home", null)
            : new RedirectResult(referer);
        context.ExceptionHandled = true;
    }
}
