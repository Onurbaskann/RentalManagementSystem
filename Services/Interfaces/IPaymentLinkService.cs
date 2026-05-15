namespace KiraTakip.Services.Interfaces;

public interface IPaymentLinkService
{
    string BuildLink(int tahakkukId);
    bool TryValidate(int tahakkukId, string token, out string? reason);
}
