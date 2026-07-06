using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IInvitationService
{
    Task<Davetiye> GonderAsync(string email, string? adSoyad, int rolId, string davetEdenUserId, int? tenantId = null, bool tumTasinmazlaraErisim = false, List<int>? tasinmazIds = null, List<int>? birimIds = null, CancellationToken ct = default);
    Task<(bool Success, string? Error, Davetiye? Davetiye)> DogrulaAsync(string rawToken, CancellationToken ct = default);
    Task<ApplicationUser> KabulEtAsync(Davetiye davetiye, string adSoyad, string password, CancellationToken ct = default);
    Task IptalEtAsync(int davetiyeId, CancellationToken ct = default);
    Task YenidenGonderAsync(int davetiyeId, string davetEdenUserId, CancellationToken ct = default);
    Task<List<Davetiye>> GetBekleyenlerAsync(CancellationToken ct = default);
    Task SuresiDolanlariIsaretle(CancellationToken ct = default);
}
