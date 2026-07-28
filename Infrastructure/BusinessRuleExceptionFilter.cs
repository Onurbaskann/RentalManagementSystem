using KiraTakip.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KiraTakip.Infrastructure;

/// <summary>
/// Servislerin iş kuralı ihlallerinde fırlattığı hataları kullanıcıya gösterir.
/// İki exception türü yakalanır:
/// - BusinessException: yeni standart. ErrorType'a göre AJAX isteklerinde uygun HTTP
///   status kodu (404/409/403/400) döner; HTML isteklerinde mesaj TempData["Error"]'a
///   yazılıp kullanıcı geldiği sayfaya geri yönlendirilir (_Layout'taki modal açılır).
/// - InvalidOperationException: geriye dönük uyumluluk. Henüz BusinessException'a taşınmamış
///   servis çağrıları için korunur; her zaman 400 / redirect olarak ele alınır.
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
        var (isHandled, message, statusCode) = context.Exception switch
        {
            BusinessException ex => (true, ex.Message, ToStatusCode(ex.ErrorType)),
            InvalidOperationException ex => (true, ex.Message, StatusCodes.Status400BadRequest),
            _ => (false, null, 0)
        };

        if (!isHandled) return;

        var request = context.HttpContext.Request;

        // JSON/AJAX istekleri redirect yerine mesajı ilgili status kodunun gövdesinde alır.
        if (!request.Headers.Accept.ToString().Contains("text/html"))
        {
            context.Result = new ObjectResult(message) { StatusCode = statusCode };
            context.ExceptionHandled = true;
            return;
        }

        var tempData = _tempDataFactory.GetTempData(context.HttpContext);
        tempData["Error"] = message;

        var referer = request.Headers.Referer.ToString();
        context.Result = string.IsNullOrEmpty(referer)
            ? new RedirectToActionResult("Index", "Home", null)
            : new RedirectResult(referer);
        context.ExceptionHandled = true;
    }

    private static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest,
    };
}
