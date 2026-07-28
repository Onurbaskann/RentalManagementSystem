using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IInvitationService
{
    Task<Invitation> SendAsync(SendInvitationInput input, CancellationToken ct = default);
    Task<(bool Success, string? Error, Invitation? Invitation)> ValidateAsync(string token, CancellationToken ct = default);
    Task<ApplicationUser> AcceptAsync(Invitation invitation, AcceptInput input, CancellationToken ct = default);
    Task CancelAsync(int invitationId, CancellationToken ct = default);
    Task ResendAsync(int invitationId, string invitedByUserId, CancellationToken ct = default);
    Task<List<Invitation>> GetPendingAsync(CancellationToken ct = default);
    Task MarkExpiredAsync(CancellationToken ct = default);
}
