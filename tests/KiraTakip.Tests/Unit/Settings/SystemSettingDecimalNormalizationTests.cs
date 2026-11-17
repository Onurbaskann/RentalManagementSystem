using KiraTakip.Models.Settings;

namespace KiraTakip.Tests;

public class SystemSettingDecimalNormalizationTests
{
    private static readonly SystemSettingDefinition DecimalDefinition = new(
        999, "Test.Decimal", "Test", "Test", "Test", "1",
        SystemSettingInputKind.Decimal, true, 0m, 100m);

    [Theory]
    [InlineData("8,5", "8.5")]
    [InlineData("8.5", "8.5")]
    [InlineData(" 5 ", "5")]
    public void TryNormalizeValue_ShouldNormalizeDecimal(string input, string expected)
    {
        var ok = SystemSettingDefinitions.TryNormalizeValue(DecimalDefinition, input, out var normalized, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.000,5")]
    public void TryNormalizeValue_ShouldRejectInvalidDecimal(string input)
    {
        var ok = SystemSettingDefinitions.TryNormalizeValue(DecimalDefinition, input, out _, out var error);

        Assert.False(ok);
        Assert.Equal("Ondalık bir sayı girilmelidir.", error);
    }

    [Fact]
    public void TryNormalizeValue_ShouldRejectBelowMinimum()
    {
        var ok = SystemSettingDefinitions.TryNormalizeValue(DecimalDefinition, "-1", out _, out var error);

        Assert.False(ok);
        Assert.Equal("Değer en az 0 olmalıdır.", error);
    }

    [Fact]
    public void TryNormalizeValue_ShouldRejectAboveMaximum()
    {
        var ok = SystemSettingDefinitions.TryNormalizeValue(DecimalDefinition, "101", out _, out var error);

        Assert.False(ok);
        Assert.Equal("Değer en fazla 100 olmalıdır.", error);
    }

    [Fact]
    public void TryNormalizeValue_IntegerDefinition_ShouldStillRejectDecimalInput()
    {
        var integerDefinition = DecimalDefinition with { InputKind = SystemSettingInputKind.Integer };

        var ok = SystemSettingDefinitions.TryNormalizeValue(integerDefinition, "8.5", out _, out _);

        Assert.False(ok);
    }
}
