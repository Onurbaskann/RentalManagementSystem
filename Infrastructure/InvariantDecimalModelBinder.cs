using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace KiraTakip.Infrastructure;

public class InvariantDecimalModelBinder : IModelBinder
{
    private readonly bool _isNullable;

    public InvariantDecimalModelBinder(bool isNullable) => _isNullable = isNullable;

    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var result = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;

        ctx.ModelState.SetModelValue(ctx.ModelName, result);
        var raw = result.FirstValue;

        if (string.IsNullOrWhiteSpace(raw))
        {
            ctx.Result = _isNullable
                ? ModelBindingResult.Success(null)
                : ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        // Önce InvariantCulture dene (browser type=number invariant gönderir),
        // başarısız olursa tr-TR dene (kullanıcı virgüllü girmiş olabilir).
        var normalized = raw.Trim().Replace(" ", "");
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ||
            decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out value))
        {
            ctx.Result = ModelBindingResult.Success(value);
        }
        else
        {
            ctx.ModelState.TryAddModelError(ctx.ModelName, $"'{raw}' geçerli bir sayı değil.");
        }

        return Task.CompletedTask;
    }
}

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var t = context.Metadata.ModelType;
        if (t == typeof(decimal)) return new InvariantDecimalModelBinder(false);
        if (t == typeof(decimal?)) return new InvariantDecimalModelBinder(true);
        return null;
    }
}
