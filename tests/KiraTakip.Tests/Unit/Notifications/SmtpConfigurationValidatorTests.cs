using KiraTakip.Infrastructure.Notifications;
using KiraTakip.Models.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace KiraTakip.Tests;

public class SmtpConfigurationValidatorTests
{
    [Fact]
    public void IsConfigured_WhenHostAndFromAreSet_ReturnsTrue()
    {
        var options = Options.Create(new SmtpSettings
        {
            Host = "smtp.example.com",
            From = "noreply@example.com"
        });
        var validator = new SmtpConfigurationValidator(options);

        Assert.True(validator.IsConfigured);
    }

    [Theory]
    [InlineData(null, "noreply@example.com")]
    [InlineData("", "noreply@example.com")]
    [InlineData("   ", "noreply@example.com")]
    [InlineData("smtp.example.com", null)]
    [InlineData("smtp.example.com", "")]
    [InlineData("smtp.example.com", "   ")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void IsConfigured_WhenHostOrFromIsMissingOrWhitespace_ReturnsFalse(string? host, string? from)
    {
        var options = Options.Create(new SmtpSettings
        {
            Host = host!,
            From = from!
        });
        var validator = new SmtpConfigurationValidator(options);

        Assert.False(validator.IsConfigured);
    }
}