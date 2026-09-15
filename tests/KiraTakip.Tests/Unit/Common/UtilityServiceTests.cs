using KiraTakip.Helpers;
using KiraTakip.Web.Helpers;
using KiraTakip.Infrastructure;
using KiraTakip.Domain.Auditing;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Security;
using Microsoft.Extensions.Options;

namespace KiraTakip.Tests;

public class UtilityServiceTests
{
    [Fact]
    public void FormatHelpers_ShouldConvertUtcValuesToTurkeyTime()
    {
        var utc = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var unspecifiedUtcFromSqlServer = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);

        Assert.Equal("15.01.2026 13:30", utc.TurkiyeTarihSaat());
        Assert.Equal("15.01.2026 13:30", unspecifiedUtcFromSqlServer.TurkiyeTarihSaat());
        Assert.Equal("—", ((DateTime?)null).TurkiyeTarihSaat());
    }

    [Fact]
    public void M2_ShouldFormatWithTwoDecimalPlacesByDefault()
    {
        Assert.Equal("38,72 m²", 38.72m.M2());
        Assert.Equal("38,72 m²", 38.72d.M2());
        Assert.Equal("—", ((decimal?)null).M2());
    }

    // ── CodeSlugger Tests ────────────────────────────────────────────────────

    [Theory]
    [InlineData("Ofis Kira Bedeli", "OFIS_KIRA_BEDELI")]
    [InlineData("  ÇĞIÖŞÜ çğıöşü  ", "CGIOSU_CGIOSU")]
    [InlineData("test---name", "TEST_NAME")]
    [InlineData("!!!Hello World!!!", "HELLO_WORLD")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void CodeSlugger_ToCode_ShouldGenerateSafeUppercaseSlugs(string? input, string expected)
    {
        var result = CodeSlugger.ToCode(input);
        Assert.Equal(expected, result);
    }

    // ── MaskingService Tests ──────────────────────────────────────────────────

    [Fact]
    public void MaskingService_ShouldMaskEmailCorrectly()
    {
        var service = new MaskingService();

        var result = service.Mask("onur.baskan@example.com", MaskType.Email);

        // "onur.baskan" -> "o**********"
        // "example.com" -> "e******.com"
        Assert.StartsWith("o*", result);
        Assert.Contains("@e*", result);
        Assert.EndsWith(".com", result);
    }

    [Theory]
    [InlineData("05551234567", "0555****567")]
    [InlineData("0555-123-4567", "0555****567")]
    [InlineData("123", "***")]
    public void MaskingService_ShouldMaskPhoneNumberCorrectly(string phone, string expected)
    {
        var service = new MaskingService();
        var result = service.Mask(phone, MaskType.Telefon);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12345678901", "123*****901")]
    [InlineData("12", "12")]
    public void MaskingService_ShouldMaskTcKimlikCorrectly(string tc, string expected)
    {
        var service = new MaskingService();
        var result = service.Mask(tc, MaskType.TcKimlik);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1234567890", "123***7890")]
    [InlineData("12", "12")]
    public void MaskingService_ShouldMaskVergiNoCorrectly(string vno, string expected)
    {
        var service = new MaskingService();
        var result = service.Mask(vno, MaskType.VergiNo);
        Assert.Equal(expected, result);
    }

    // ── SecureTokenService Tests ──────────────────────────────────────────────

    [Fact]
    public void SecureTokenService_ShouldGenerateAndValidateTokenSuccessfully()
    {
        var options = Options.Create(new SecureTokenSettings
        {
            Secret = "SuperSecretCryptographicallySecureKeyOfLengthAtLeast32Bytes"
        });
        var service = new SecureTokenService(options);

        var entityId = "lease-1234";
        var purpose = "SifreSifirlama";
        var ttl = TimeSpan.FromMinutes(10);

        var tokenResult = service.Generate(entityId, purpose, ttl);

        Assert.NotNull(tokenResult);
        Assert.NotEmpty(tokenResult.RawToken);
        Assert.NotEmpty(tokenResult.TokenHash);

        var isValid = service.TryValidate(tokenResult.RawToken, entityId, purpose, out var reason);
        Assert.True(isValid, reason);
    }

    [Fact]
    public void SecureTokenService_ShouldFailValidationWithInvalidParameters()
    {
        var options = Options.Create(new SecureTokenSettings
        {
            Secret = "SuperSecretCryptographicallySecureKeyOfLengthAtLeast32Bytes"
        });
        var service = new SecureTokenService(options);

        var entityId = "lease-1234";
        var purpose = "SifreSifirlama";
        var ttl = TimeSpan.FromMinutes(10);

        var tokenResult = service.Generate(entityId, purpose, ttl);

        // Test 1: Wrong EntityId
        var isValid1 = service.TryValidate(tokenResult.RawToken, "wrong-id", purpose, out var reason1);
        Assert.False(isValid1);
        Assert.Equal("Bağlantı kimliği uyuşmuyor.", reason1);

        // Test 2: Wrong Purpose
        var isValid2 = service.TryValidate(tokenResult.RawToken, entityId, "WrongPurpose", out var reason2);
        Assert.False(isValid2);
        Assert.Equal("İmza geçersiz.", reason2);

        // Test 3: Expired Token
        var tokenResultExpired = service.Generate(entityId, purpose, TimeSpan.FromSeconds(-5));
        var isValid3 = service.TryValidate(tokenResultExpired.RawToken, entityId, purpose, out var reason3);
        Assert.False(isValid3);
        Assert.Equal("Bağlantının süresi dolmuş.", reason3);
    }
}
