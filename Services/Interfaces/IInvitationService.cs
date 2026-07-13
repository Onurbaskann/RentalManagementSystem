using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IInvitationService
{
    Task<Invitation> GonderAsync(string email, string? adSoyad, int rolId, string davetEdenUserId, int? tenantId = null, bool tumTasinmazlaraErisim = false, List<int>? tasinmazIds = null, List<int>? birimIds = null, CancellationToken ct = default);
    Task<(bool Success, string? Error, Invitation? Invitation)> DogrulaAsync(string rawToken, CancellationToken ct = default);
    Task<ApplicationUser> KabulEtAsync(Invitation invitation, string adSoyad, string password, CancellationToken ct = default);
    Task IptalEtAsync(int invitationId, CancellationToken ct = default);
    Task YenidenGonderAsync(int invitationId, string davetEdenUserId, CancellationToken ct = default);
    Task<List<Invitation>> GetBekleyenlerAsync(CancellationToken ct = default);
    Task SuresiDolanlariIsaretle(CancellationToken ct = default);
}
