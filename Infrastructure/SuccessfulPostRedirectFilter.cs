using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KiraTakip.Infrastructure;

public sealed class SuccessfulPostRedirectFilter(
    ITempDataDictionaryFactory tempDataFactory) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
            return;

        var tempData = tempDataFactory.GetTempData(context.HttpContext);
        if (context.ActionDescriptor.EndpointMetadata
            .OfType<SuppressAutomaticSuccessFeedbackAttribute>()
            .Any())
        {
            tempData.Remove(FeedbackTempDataKeys.OperationSucceeded);
            tempData.Remove(FeedbackTempDataKeys.SuccessMessage);
            return;
        }

        if (!context.ModelState.IsValid
            || !IsRedirect(context.Result))
            return;

        if (tempData.ContainsKey("Error")) return;

        tempData[FeedbackTempDataKeys.OperationSucceeded] = true;
    }
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static bool IsRedirect(IActionResult result)
        => result is RedirectResult
            or LocalRedirectResult
            or RedirectToActionResult
            or RedirectToRouteResult;
}