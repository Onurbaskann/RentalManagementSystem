using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KiraTakip.Services;

public class BorcHatirlatmaService : IBorcHatirlatmaService
{
    private readonly ITahakkukRepository _tahakkukRepo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly ILogger<BorcHatirlatmaService> _logger;
    private readonly PaymentLinkSettings _paymentSettings;
    private readonly SmtpSettings _smtpSettings;

    public BorcHatirlatmaService(
        ITahakkukRepository tahakkukRepo,
        IUnitOfWork uow,
        IPaymentLinkService paymentLinkService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        ILogger<BorcHatirlatmaService> logger,
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

        // Group by Kiraci
        var groups = borclar.GroupBy(t => t.KiraSozlesmesi!.KiraciId).ToList();
        sonuc.ToplamBorclu = groups.Count;

        foreach (var group in groups)
        {
            var kiraci = group.First().KiraSozlesmesi!.Kiraci;
            if (kiraci == null || string.IsNullOrWhiteSpace(kiraci.Email))
            {
                _logger.LogWarning("Kiraci {KiraciId} için geçerli e-posta adresi bulunamadı. Atlanıyor.", group.Key);
                sonuc.BasarisizGonderim++;
                continue;
            }

            // 3. Cooldown check
            var debtsOutsideCooldown = group.Where(t => t.SonHatirlatmaTarihi == null || t.SonHatirlatmaTarihi.Value.Date <= cooldownThreshold).ToList();

            if (!debtsOutsideCooldown.Any())
            {
                _logger.LogInformation("Kiraci {KiraciId} için son hatırlatmalar bekleme süresi içerisinde. Atlanıyor.", group.Key);
                sonuc.CooldownAtlanan++;
                continue;
            }

            // 4. Prepare email model with ALL outstanding debts for the tenant
            var mailModel = new KiraciBorcHatirlatmaMailModel
            {
                Ad = kiraci.Ad,
                Soyad = "",
                Email = kiraci.Email,
                OdemeLink = await _paymentLinkService.BuildLinkAsync(kiraci.Id, ct),
                Borclar = group.OrderBy(t => t.VadeTarihi).Select(t => new BorcSatiri
                {
                    TasinmazAdi = t.KiraSozlesmesi!.Birim!.Tasinmaz!.Ad,
                    BirimAdi = t.KiraSozlesmesi!.Birim!.Ad,
                    DonemBaslangic = t.DonemBaslangic,
                    VadeTarihi = t.VadeTarihi,
                    ToplamTutar = t.ToplamTutar,
                    OdenenTutar = t.Odemeler.Where(o => o.Durum == OdemeDurumu.Onaylandi).Sum(o => o.Tutar)
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
                _logger.LogError(ex, "Kiraci {KiraciId} için e-posta şablonu render edilemedi.", kiraci.Id);
                sonuc.BasarisizGonderim++;
                continue;
            }

            // 6. Send Mail
            try
            {
                await _mailService.SendAsync(
                    kiraci.Email,
                    mailModel.GosterimAdi,
                    "KiraTakip - Ödeme Hatırlatması",
                    htmlBody,
                    ct
                );

                // 7. Mark as sent ONLY for debts outside cooldown (so we reset their cooldown)
                foreach (var debt in debtsOutsideCooldown)
                {
                    debt.SonHatirlatmaTarihi = DateTime.Today;
                }

                sonuc.BasariliGonderim++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kiraci {KiraciId} e-posta gönderimi başarısız.", kiraci.Id);
                sonuc.BasarisizGonderim++;
            }
        }

        // 8. Save changes to DB (updates SonHatirlatmaTarihi)
        if (sonuc.BasariliGonderim > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        return sonuc;
    }
}
