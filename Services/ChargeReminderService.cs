using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KiraTakip.Services;

public class ChargeReminderService : IChargeReminderService
{
    private readonly IChargeRepository _tahakkukRepo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly ILogger<ChargeReminderService> _logger;
    private readonly PaymentLinkSettings _paymentSettings;
    private readonly SmtpSettings _smtpSettings;

    public ChargeReminderService(
        IChargeRepository tahakkukRepo,
        IUnitOfWork uow,
        IPaymentLinkService paymentLinkService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        ILogger<ChargeReminderService> logger,
        IOptions<PaymentLinkSettings> paymentOptions,
        IOptions<SmtpSettings> smtpOptions)
    {
        _tahakkukRepo = tahakkukRepo;
        _uow = uow;
        _paymentLinkService = paymentLinkService;
        _mailService = mailService;
        _renderer = renderer;
        _logger = logger;
        _paymentSettings = paymentOptions.Value;
        _smtpSettings = smtpOptions.Value;
    }

    public async Task<BorcHatirlatmaSonucDto> GonderAsync(CancellationToken ct = default)
    {
        var sonuc = new BorcHatirlatmaSonucDto();

        // 1. Config Guards
        if (string.IsNullOrWhiteSpace(_paymentSettings.Secret) || _paymentSettings.Secret.Length < 32)
            throw new InvalidOperationException("PaymentLink:Secret yapılandırılmamış veya çok kısa (min 32 karakter).");
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host) || string.IsNullOrWhiteSpace(_smtpSettings.From))
            throw new InvalidOperationException("SMTP sunucu ayarları (Host veya From) yapılandırılmamış.");

        var today = DateTime.Today;
        var limitVade = today.AddDays(_paymentSettings.ReminderDaysBefore);
        var cooldownThreshold = today.AddDays(-_paymentSettings.ReminderCooldownDays);

        // 2. Fetch Outstanding Debts
        var borclar = await _tahakkukRepo.GetBekleyenBorclarAsync(limitVade, ct);

        // Group by Tenant
        var groups = borclar.GroupBy(t => t.TenantId).ToList();
        sonuc.ToplamBorclu = groups.Count;

        foreach (var group in groups)
        {
            var tenant = group.First().Tenant;
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Email))
            {
                _logger.LogWarning("Tenant {KiraciId} için geçerli e-posta adresi bulunamadı. Atlanıyor.", group.Key);
                sonuc.BasarisizGonderim++;
                continue;
            }

            // 3. Cooldown check
            var debtsOutsideCooldown = group.Where(t => t.LastReminderDate == null || t.LastReminderDate.Value.Date <= cooldownThreshold).ToList();

            if (!debtsOutsideCooldown.Any())
            {
                _logger.LogInformation("Tenant {KiraciId} için son hatırlatmalar bekleme süresi içerisinde. Atlanıyor.", group.Key);
                sonuc.CooldownAtlanan++;
                continue;
            }

            // 4. Prepare email model with ALL outstanding debts for the tenant
            var mailModel = new KiraciBorcHatirlatmaMailModel
            {
                Ad = tenant.Name,
                Soyad = "",
                Email = tenant.Email,
                OdemeLink = await _paymentLinkService.BuildLinkAsync(tenant.Id, ct),
                Borclar = group.OrderBy(t => t.DueDate).Select(t => new BorcSatiri
                {
                    PropertyName = t.Lease?.Unit?.Property?.Name ?? "-",
                    BirimAdi = t.Lease?.Unit?.Name ?? "-",
                    PeriodStart = t.PeriodStart,
                    DueDate = t.DueDate,
                    ToplamTutar = t.TotalAmount,
                    PaidAmount = t.Allocations.Where(o => o.Status == PaymentStatus.Approved).Sum(o => o.Amount)
                }).ToList()
            };

            // 5. Render HTML
            string htmlBody;
            try
            {
                htmlBody = await _renderer.RenderAsync("BorcHatirlatma", mailModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant {KiraciId} için e-posta şablonu render edilemedi.", tenant.Id);
                sonuc.BasarisizGonderim++;
                continue;
            }

            // 6. Send Mail
            try
            {
                await _mailService.SendAsync(
                    tenant.Email,
                    mailModel.GosterimAdi,
                    "KiraTakip - Ödeme Hatırlatması",
                    htmlBody,
                    ct
                );

                // 7. Mark as sent ONLY for debts outside cooldown (so we reset their cooldown)
                foreach (var debt in debtsOutsideCooldown)
                {
                    debt.LastReminderDate = DateTime.Today;
                }

                sonuc.BasariliGonderim++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant {KiraciId} e-posta gönderimi başarısız.", tenant.Id);
                sonuc.BasarisizGonderim++;
            }
        }

        // 8. Save changes to DB (updates LastReminderDate)
        if (sonuc.BasariliGonderim > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        return sonuc;
    }
}
