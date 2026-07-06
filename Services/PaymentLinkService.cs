using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace KiraTakip.Services;

public class PaymentLinkService : IPaymentLinkService, ITransactionalService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecureTokenService _tokenService;
    private readonly PaymentLinkSettings _settings;
    private const string Purpose = "payment-portal";

    public PaymentLinkService(
        ApplicationDbContext db,
        ISecureTokenService tokenService,
        IOptions<PaymentLinkSettings> options)
    {
        _db = db;
        _tokenService = tokenService;
        _settings = options.Value;
    }

    public async Task<string> BuildLinkAsync(int tenantId, CancellationToken ct = default)
    {
        var ttl = TimeSpan.FromHours(_settings.TokenTtlHours);
        var kayit = new OdemeLinkKayit
        {
            TenantId = tenantId,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };
        _db.OdemeLinkKayitlari.Add(kayit);
        await _db.SaveChangesAsync(ct);

        var result = _tokenService.Generate(kayit.Id.ToString(), Purpose, ttl);
        kayit.TokenHash = result.TokenHash;
        await _db.SaveChangesAsync(ct);

        return $"{_settings.BaseUrl.TrimEnd('/')}/Payment/Portal?t={Uri.EscapeDataString(result.RawToken)}";
    }

    public async Task<(bool Success, int KiraciId, string? Reason)> TryValidateAsync(string token, CancellationToken ct = default)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return (false, 0, "Geçersiz token formatı.");

        string entityId;
        try
        {
            var padded = parts[0].Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            entityId = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
        catch
        {
            return (false, 0, "Geçersiz token formatı.");
        }

        if (!int.TryParse(entityId, out var kayitId))
            return (false, 0, "Geçersiz token formatı.");

        var kayit = await _db.OdemeLinkKayitlari
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == kayitId, ct);

        if (kayit == null)
            return (false, 0, "Ödeme linki bulunamadı.");

        if (kayit.Durum == PaymentLinkStatus.Cancelled)
            return (false, 0, "Bu ödeme linki iptal edilmiştir.");

        if (!_tokenService.TryValidate(token, entityId, Purpose, out var reason))
        {
            if (kayit.Durum == PaymentLinkStatus.Active && kayit.ExpiresAt < DateTime.UtcNow)
            {
                kayit.Durum = PaymentLinkStatus.Expired;
                await _db.SaveChangesAsync(ct);
            }
            return (false, 0, reason ?? "Geçersiz ödeme linki.");
        }

        return (true, kayit.TenantId, null);
    }

    public async Task IptalEtAsync(int kayitId, string iptalEdenUserId, CancellationToken ct = default)
    {
        var kayit = await _db.OdemeLinkKayitlari
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == kayitId, ct)
            ?? throw new InvalidOperationException("Kayıt bulunamadı.");

        if (kayit.Durum != PaymentLinkStatus.Active)
            throw new InvalidOperationException("Yalnızca aktif linkler iptal edilebilir.");

        kayit.Durum = PaymentLinkStatus.Cancelled;
        kayit.IptalEdenUserId = iptalEdenUserId;
        kayit.IptalTarihi = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
