namespace KiraTakip.Services.Interfaces;

public interface IMailService
{
    Task SendAsync(string toAddress, string toName, string subject, string htmlBody, CancellationToken ct = default);
}
