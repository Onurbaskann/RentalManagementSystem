namespace KiraTakip.Services.Interfaces.Notifications;

public interface ISmtpConfigurationValidator
{
    bool IsConfigured { get; }
}