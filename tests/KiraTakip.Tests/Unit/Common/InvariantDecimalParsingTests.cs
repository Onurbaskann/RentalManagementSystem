using KiraTakip.Web.ModelBinding;

namespace KiraTakip.Tests;

/// <summary>
/// Müşteri şikayeti: "38,72" gibi virgüllü bir m² değeri girildiğinde, eski kültür-tahminli
/// ayrıştırma bunu binlik ayraçlı "3872" olarak yanlış okuyordu (InvariantCulture'da virgül
/// binlik ayracıdır ve NumberStyles.Any bunu hatasız kabul ediyordu). Bu testler, düzeltilmiş
/// "son ayraç = ondalık nokta" kuralını doğrular.
/// </summary>
public class InvariantDecimalParsingTests
{
    [Theory]
    [InlineData("38,72", 38.72)]
    [InlineData("38.72", 38.72)]
    [InlineData("12.500,75", 12500.75)]
    [InlineData("12,500.75", 12500.75)]
    [InlineData("3872", 3872)]
    [InlineData("-38,72", -38.72)]
    [InlineData(",72", 0.72)]
    public void TryParseFlexibleDecimal_ShouldTreatLastSeparatorAsDecimalPoint(
        string input, decimal expected)
    {
        var success = InvariantDecimalModelBinder.TryParseFlexibleDecimal(input, out var value);

        Assert.True(success);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12,3a")]
    public void TryParseFlexibleDecimal_ShouldFail_ForInvalidInput(string input)
    {
        var success = InvariantDecimalModelBinder.TryParseFlexibleDecimal(input, out _);

        Assert.False(success);
    }
}
