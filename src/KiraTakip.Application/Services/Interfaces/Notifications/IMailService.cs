namespace KiraTakip.Services.Interfaces.Notifications;

public interface IMailService
{
    Task SendAsync(string toAddress, string toName, string subject, string htmlBody, CancellationToken ct = default);
}
