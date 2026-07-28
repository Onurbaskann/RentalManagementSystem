using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace KiraTakip.Services;

public class PaymentLinkService(
    IPaymentLinkRecordRepository paymentLinkRepository,
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    ISecureTokenService tokenService,
    IOptions<PaymentLinkSettings> options) : IPaymentLinkService, ITransactionalService
{
    private readonly PaymentLinkSettings settings = options.Value;
    private const string Purpose = "payment-portal";

    public async Task<string> CreateAsync(
        CreatePaymentLinkInput input,
        CancellationToken cancellationToken = default)
    {
        Guard.Against(
            settings.TokenTtlHours <= 0,
            "PaymentLink:TokenTtlHours sıfırdan büyük olmalıdır.",
            "PAYMENT_LINK_INVALID_TTL");

        var hasValidBaseUrl = Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri)
            && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps);
        Guard.Against(
            !hasValidBaseUrl,
            "PaymentLink:BaseUrl geçerli bir HTTP/HTTPS adresi olmalıdır.",
            "PAYMENT_LINK_INVALID_BASE_URL");

        Guard.NotFound(
            await tenantRepository.GetByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "PAYMENT_LINK_TENANT_NOT_FOUND");

        var ttl = TimeSpan.FromHours(settings.TokenTtlHours);
        var record = new PaymentLinkRecord
        {
            TenantId = input.TenantId,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };
        await paymentLinkRepository.AddAsync(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = tokenService.Generate(record.Id.ToString(), Purpose, ttl);
        record.TokenHash = result.TokenHash;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"{settings.BaseUrl.TrimEnd('/')}/Payment/Portal?t={Uri.EscapeDataString(result.RawToken)}";
    }

    public async Task<PaymentLinkValidationResultDto> ValidateAsync(
        ValidatePaymentLinkInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Token))
            return new PaymentLinkValidationResultDto(false, 0, "Geçersiz bağlantı biçimi.");

        var parts = input.Token.Split('.');
        if (parts.Length != 3)
            return new PaymentLinkValidationResultDto(false, 0, "Geçersiz bağlantı biçimi.");

        string entityId;
        try
        {
            var padded = parts[0].Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            entityId = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
        catch
        {
            return new PaymentLinkValidationResultDto(false, 0, "Geçersiz bağlantı biçimi.");
        }

        if (!int.TryParse(entityId, out var recordId))
            return new PaymentLinkValidationResultDto(false, 0, "Geçersiz bağlantı biçimi.");

        var record = await paymentLinkRepository.GetByIdIgnoringFiltersAsync(
            recordId,
            cancellationToken);

        if (record == null)
            return new PaymentLinkValidationResultDto(false, 0, "Ödeme bağlantısı bulunamadı.");

        if (!TokenHashMatches(record.TokenHash, tokenService.ComputeHash(input.Token)))
            return new PaymentLinkValidationResultDto(false, 0, "İmza geçersiz.");

        if (record.Status == PaymentLinkStatus.Cancelled)
            return new PaymentLinkValidationResultDto(false, 0, "Bu ödeme bağlantısı iptal edilmiştir.");

        if (!tokenService.TryValidate(input.Token, entityId, Purpose, out var reason))
        {
            if (record.Status == PaymentLinkStatus.Active && record.ExpiresAt < DateTime.UtcNow)
            {
                record.Status = PaymentLinkStatus.Expired;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return new PaymentLinkValidationResultDto(
                false,
                0,
                reason ?? "Geçersiz ödeme bağlantısı.");
        }

        return new PaymentLinkValidationResultDto(true, record.TenantId, null);
    }

    public async Task CancelAsync(
        CancelPaymentLinkInput input,
        CancellationToken cancellationToken = default)
    {
        var record = Guard.NotFound(
            await paymentLinkRepository.GetByIdIgnoringFiltersAsync(
                input.RecordId,
                cancellationToken),
            "Kayıt bulunamadı.",
            "PAYMENT_LINK_NOT_FOUND");

        Guard.Conflict(
            record.Status != PaymentLinkStatus.Active,
            "Yalnızca aktif bağlantılar iptal edilebilir.",
            "PAYMENT_LINK_NOT_ACTIVE");

        record.Status = PaymentLinkStatus.Cancelled;
        record.CancelledByUserId = input.CancelledByUserId;
        record.CancelledAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool TokenHashMatches(string storedHash, string suppliedHash)
    {
        var storedBytes = Encoding.UTF8.GetBytes(storedHash);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedHash);
        return CryptographicOperations.FixedTimeEquals(storedBytes, suppliedBytes);
    }
}
