namespace KiraTakip.Services.Interfaces;

public interface IPaymentLinkService
{
    string BuildLink(int kiraciId);
    bool TryValidate(string token, out int kiraciId, out string? reason);
}
