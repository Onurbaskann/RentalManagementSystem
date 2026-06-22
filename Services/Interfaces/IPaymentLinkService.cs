namespace KiraTakip.Services.Interfaces;

public interface IPaymentLinkService
{
    Task<string> BuildLinkAsync(int kiraciId, CancellationToken ct = default);
    Task<(bool Success, int KiraciId, string? Reason)> TryValidateAsync(string token, CancellationToken ct = default);
    Task IptalEtAsync(int kayitId, string iptalEdenUserId, CancellationToken ct = default);
}
