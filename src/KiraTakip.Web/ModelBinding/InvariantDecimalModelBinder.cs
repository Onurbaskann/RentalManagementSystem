using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace KiraTakip.Web.ModelBinding;

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

        var normalized = raw.Trim().Replace(" ", "");
        if (TryParseFlexibleDecimal(normalized, out var value))
        {
            ctx.Result = ModelBindingResult.Success(value);
        }
        else
        {
            ctx.ModelState.TryAddModelError(ctx.ModelName, $"'{raw}' geçerli bir sayı değil.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Virgül/nokta hangisinin ondalık ayracı olduğunu kültüre göre TAHMİN ETMEK yerine
    /// (bu, "38,72" gibi değerlerin binlik ayraçlı 3872 olarak yanlış ayrıştırılmasına yol
    /// açıyordu — bkz. m² alanı hatası) metindeki SON virgül/nokta karakterini her zaman
    /// ondalık ayracı sayar; ondan önceki virgül/nokta karakterleri binlik ayracı kabul edilip
    /// temizlenir. "38,72", "38.72", "12.500,75" ve "12,500.75" hepsi doğru ayrıştırılır.
    /// </summary>
    public static bool TryParseFlexibleDecimal(string input, out decimal value)
    {
        var lastSeparatorIndex = input.LastIndexOfAny(['.', ',']);

        var normalized = input;
        if (lastSeparatorIndex >= 0)
        {
            var integerPart = input[..lastSeparatorIndex].Replace(",", "").Replace(".", "");
            var fractionalPart = input[(lastSeparatorIndex + 1)..];
            normalized = fractionalPart.Length > 0 ? $"{integerPart}.{fractionalPart}" : integerPart;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
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
