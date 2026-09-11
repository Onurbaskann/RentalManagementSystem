using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces.Notifications;
using Microsoft.Extensions.Options;

namespace KiraTakip.Infrastructure.Notifications;

public class SmtpConfigurationValidator(IOptions<SmtpSettings> smtpOptions) : ISmtpConfigurationValidator
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(smtpOptions.Value.Host) &&
        !string.IsNullOrWhiteSpace(smtpOptions.Value.From);
}