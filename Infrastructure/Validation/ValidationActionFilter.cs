using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KiraTakip.Infrastructure.Validation;

/// <summary>
/// Action çalışmadan önce, her action argümanı için DI'da kayıtlı bir IValidator&lt;T&gt;
/// olup olmadığına bakar; varsa çalıştırır ve hatalarını ModelState'e basar.
///
/// Short-circuit YAPMAZ: action normal akışında devam eder. Mevcut
/// `if (!ModelState.IsValid) return View(model);` deseni değişmeden çalışmaya devam eder,
/// input korunarak inline hata gösterimi sağlanır.
///
/// Kayıtlı validator bulunmayan argümanlar için hiçbir şey yapmaz (inert) — bu filtre
/// varlığıyla mevcut davranışı bozmaz; yalnızca ValidationModule üzerinden bir IValidator&lt;T&gt;
/// kaydedildiğinde etkinleşir.
/// </summary>
public class ValidationActionFilter : IActionFilter
{
    private static readonly ConcurrentDictionary<Type, ValidatorLookup> Cache = new();

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var lookup = Cache.GetOrAdd(argument.GetType(), BuildLookup);
            var validator = context.HttpContext.RequestServices.GetService(lookup.ValidatorType);
            if (validator is null) continue;

            var result = (ValidationResult)lookup.ValidateMethod.Invoke(validator, [argument])!;
            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static ValidatorLookup BuildLookup(Type argumentType)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
        var validateMethod = validatorType.GetMethod(nameof(IValidator<object>.Validate))!;
        return new ValidatorLookup(validatorType, validateMethod);
    }

    private readonly record struct ValidatorLookup(Type ValidatorType, MethodInfo ValidateMethod);
}
