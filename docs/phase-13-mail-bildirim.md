# Faz 13 — Mail Bildirim Altyapısı ve Ödeme Portalı İskeleti

> **Aktif Faz.** Bu dosya `MASTER-PLAN.md`'nin token tasarrufu kuralı gereği aktif faz boyunca yüklenir.

---

## Hedef

`/Sozlesme` ekranındaki **"Borçlulara Mail" butonu**, vadesi yaklaşan tahakkukları olan kiracılara, ödeme yapabilecekleri **harici bir portal sayfası**na yönlendiren hatırlatma maili gönderecek. Mail/SMTP altyapısı sıfırdan kurulur; ödeme portalı iskelet olarak çıkar (sanal POS sonra eklenir).

## Kapsam

**Dahil:**
- SMTP gönderim altyapısı (`MailKit`)
- HMAC ile imzalı kalıcı ödeme linki üreten servis
- Razor şablon ile HTML mail üretimi
- Manuel "Borçlulara Mail" butonu (form POST + onay modal)
- `OdemePortal` iskelet controller + view (login yok, token doğrular, "yakında sanal POS" mesajı)

**Dışında (sonraki fazlar):**
- Otomatik scheduler (`IHostedService` / Hangfire)
- Mail gönderim audit tablosu
- Sanal POS entegrasyonu (iyzico/Param/PayTR vb.)
- Çoklu tahakkuğu olan kiracıya gruplu mail
- Mail şablonu çoklu dil

---

## Kararlar (Onaylandı)

| Konu | Karar | Gerekçe |
|---|---|---|
| Tetik | Manuel buton | Scheduling altyapısı + LastReminderSentAt tracking ileride |
| Koşul | `VadeTarihi <= today + N` AND `Durum NOT IN (TamOdendi, IptalEdildi)` | Asıl talep "vade yaklaşıyor" |
| N parametresi | `appsettings.json → PaymentLink:ReminderDaysBefore = 5` | Konfigürable |
| Link | HMAC-SHA256 imzalı token, self-contained expires | DB tablosu gerekmez, ölçeklenebilir |
| Login | Portal sayfası `[AllowAnonymous]` | Kiracı sisteme alınmıyor |
| Permission | `PermissionCatalog.Tahakkuk.View` | En yakın eşleşme; ileride `Bildirim.Gonder` |

---

## Konfigürasyon

`appsettings.json` (eklenir):

```json
"Smtp": {
  "Host": "",
  "Port": 587,
  "User": "",
  "Pass": "",
  "From": "",
  "FromName": "KiraTakip",
  "UseStartTls": true,
  "TimeoutSeconds": 30
},
"PaymentLink": {
  "BaseUrl": "https://localhost:5031",
  "Secret": "",
  "TokenTtlHours": 168,
  "ReminderDaysBefore": 5
}
```

**Kullanıcı dolduracak:** `Smtp.{Host, Port, User, Pass, From}`, `PaymentLink.{BaseUrl, Secret}`.

---

## Mimari Bileşenler

### Servisler
- **`IMailService` / `SmtpMailService`** — MailKit ile SMTP gönderimi
- **`IPaymentLinkService` / `PaymentLinkService`** — HMAC token üret/doğrula
- **`IRazorViewToStringRenderer`** — Razor şablonunu string'e render etme helper

### Model
- **`SmtpSettings`** (`Models/Settings/`)
- **`PaymentLinkSettings`** (`Models/Settings/`)
- **`BorcHatirlatmaMailModel`** (`Models/ViewModels/`)
- **`OdemePortalViewModel`** (`Models/ViewModels/`)

### View
- **`Views/Shared/EmailTemplates/BorcHatirlatma.cshtml`** — HTML mail şablonu
- **`Views/Shared/_PortalLayout.cshtml`** — yalın layout (sistem layout'undan ayrı)
- **`Views/OdemePortal/Index.cshtml`** — tahakkuk özeti + "Sanal POS yakında"
- **`Views/OdemePortal/Invalid.cshtml`** — token hatası sayfası

### Controller
- **`OdemePortalController`** — `[AllowAnonymous]`, `Index(int id, string t)`
- **`SozlesmeController.BorclularaMailGonder`** — POST action

---

## Token Mekanizması

`PaymentLinkService` HMAC-SHA256 ile self-contained token üretir:

```
plaintext: "{tahakkukId}|{expiresUnixSeconds}"
hmac = HMACSHA256(plaintext, PaymentLink:Secret)
token = "{expiresUnixSeconds}.{Base64Url(hmac)}"
link  = "{BaseUrl}/Odeme/Portal/{tahakkukId}?t={token}"
```

Doğrulama: expires kontrolü → HMAC eşitliği (constant-time compare).

---

## Borçlu Sorgusu

`SozlesmeController.BorclularaMailGonder`:

```csharp
var bugun = DateTime.Today;
var esik = bugun.AddDays(_paymentLinkOptions.Value.ReminderDaysBefore);

var borclular = await _ctx.KiraTahakkuklar
    .Include(t => t.KiraSozlesmesi).ThenInclude(s => s!.Kiraci)
    .Include(t => t.KiraSozlesmesi).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
    .Include(t => t.Odemeler)
    .Where(t => t.VadeTarihi <= esik
        && t.Durum != TahakkukDurumu.TamOdendi
        && t.Durum != TahakkukDurumu.IptalEdildi
        && t.KiraSozlesmesi != null
        && t.KiraSozlesmesi.KiraciId != 0)
    .ToListAsync();
```

`Index` sayım sorgusu da aynı filtreye güncellenir (tutarlılık).

---

## Mail Gönderim Akışı

Her tahakkuk için:
1. `KalanTutar = ToplamTutar - Odemeler.Where(Onaylandi).Sum(Tutar)`
2. `link = _paymentLink.BuildLink(t.Id)`
3. `model = new BorcHatirlatmaMailModel { ... OdemeLink = link }`
4. `html = await _razorRenderer.RenderAsync("EmailTemplates/BorcHatirlatma", model)`
5. `try { await _mail.SendAsync(kiraci.Email, ..., subject, html); gonderildi++; }`
6. `catch { logger.LogError(...); hata++; }`
7. Email boşsa atlanır; `atlandi++`

Sonuç `TempData["Success"]` ile özet mesaj.

---

## Checklist

### 13.1 Spec ve Doc
- [ ] 13.1.1 — `docs/phase-13-mail-bildirim.md` oluşturuldu
- [ ] 13.1.2 — `MASTER-PLAN.md` Faz Haritası + Aktif Faz güncellendi
- [ ] 13.1.3 — `PROGRESS.md` Faz 13 checklist eklendi

### 13.2 Bağımlılık ve Konfigürasyon
- [ ] 13.2.1 — `KiraTakip.csproj` MailKit referansı
- [ ] 13.2.2 — `appsettings.json` Smtp + PaymentLink bölümleri
- [ ] 13.2.3 — `SmtpSettings` + `PaymentLinkSettings` sınıfları

### 13.3 Servisler
- [ ] 13.3.1 — `IPaymentLinkService` + `PaymentLinkService` (HMAC)
- [ ] 13.3.2 — `IRazorViewToStringRenderer` + impl
- [ ] 13.3.3 — `IMailService` + `SmtpMailService`
- [ ] 13.3.4 — `Program.cs` DI kayıtları

### 13.4 Mail İçeriği
- [ ] 13.4.1 — `BorcHatirlatmaMailModel` ViewModel
- [ ] 13.4.2 — `Views/Shared/EmailTemplates/BorcHatirlatma.cshtml` (inline CSS, mail-safe)

### 13.5 Buton ve Action
- [ ] 13.5.1 — `SozlesmeController.BorclularaMailGonder` POST action
- [ ] 13.5.2 — `Index` sayım sorgusu `VadeTarihi <= today + N` filtresine güncellendi
- [ ] 13.5.3 — `Sozlesme/Index.cshtml` buton form'a dönüştürüldü, `data-confirm` eklendi

### 13.6 Ödeme Portalı İskeleti
- [ ] 13.6.1 — `OdemePortalController` ([AllowAnonymous]) `Index(int id, string t)`
- [ ] 13.6.2 — `OdemePortalViewModel`
- [ ] 13.6.3 — `Views/Shared/_PortalLayout.cshtml`
- [ ] 13.6.4 — `Views/OdemePortal/Index.cshtml` (özet + "Sanal POS yakında")
- [ ] 13.6.5 — `Views/OdemePortal/Invalid.cshtml` (hata sayfası)

### 13.7 Doğrulama
- [ ] 13.7.1 — `dotnet build` 0 hata
- [ ] 13.7.2 — Manuel smoke test: SMTP doluyken buton → mail kutusunda doğru HTML
- [ ] 13.7.3 — Linke tıkla → token doğrulanır → özet sayfa
- [ ] 13.7.4 — Token süresi dolmuş URL → Invalid sayfası
- [ ] 13.7.5 — Email boş kiracı varsa → atlanır, exception yok
- [ ] 13.7.6 — SMTP erişilemezse → her tahakkuk için log + özet mesaj

---

## Acceptance Kriterleri

1. Mail HTML görüntülenir, "Ödeme Yap" CTA butonu doğru linke yönlendirir
2. Token süresi içinde valide olur, süre dışı validate olmaz
3. Aynı sözleşme ekranındaki `borcluSayisi` ile gönderilen mail sayısı tutarlı
4. Email boş / SMTP hatası tek tek kiracılarda dururulabilir, toplu akışı çökertmez
5. `appsettings.Smtp` boş bırakılırsa "SMTP yapılandırılmamış" kullanıcı mesajı

---

## Notlar

- **Mailpit/MailHog** local SMTP test için kullanılabilir (geliştirme sırasında)
- HMAC `Secret` minimum 32 karakter; geliştirme için biz üretip yazabiliriz, canlıda kullanıcı kendi anahtarını koyar
- `OdemePortalController` ileride sanal POS entegrasyonu için genişler (POST `/Odeme/Portal/{id}/Ode`)
- `KiraTahakkuk` modelinde `LastReminderSentAt` alanı **yok** ve eklenmiyor (manuel buton akışı için gerekmez); otomatik scheduler fazına bırakıldı

---

## 13.8 Revize — Kiracı Bazlı Mail + Çoklu Borç Portalı (2026-05-18)

> İlk versiyon (13.1–13.7) tahakkuk bazlı çalışıyordu: bir kiracının 3 borcu varsa 3 ayrı mail; butona tekrar basıldığında mükerrer; portal sadece tek borç gösteriyor. Bu revize üç sorunu çözer: gruplama, cooldown, kiracı-bazlı portal.

### 13.8.1 Onaylanan Kararlar

| Konu | Karar | Gerekçe |
|---|---|---|
| Mail birimi | Kiracı bazlı (1 kiracı = 1 mail) | Mükerrer mail önlenir; kiracı tüm borçlarını tek yerde görür |
| Cooldown | `KiraTahakkuk.SonHatirlatmaTarihi` + `PaymentLink:ReminderCooldownDays = 7` | Aynı borç için 7 gün içinde tekrar mail atılmaz |
| Cooldown semantiği | **Tetik + tam tablo**: kiracının cooldown dışı ≥1 borcu varsa mail gönderilir; mail'de TÜM bekleyen borçlar listelenir; `SonHatirlatmaTarihi` sadece cooldown dışı borçlarda update edilir | Kiracıya eksik bilgi verme; ama mükerrer hatırlatma yok |
| Token payload | `{kiraciId}\|{expires}` (eski `{tahakkukId}` yerine) | Tek link = portal'da tüm borçlar |
| Permission | `Bildirim.BorcHatirlatma` | `Tahakkuk.View`'dan ayrı; `Bildirim.*` grup ileride genişler |
| Portal URL | `/Odeme/Portal/{kiraciId}?t={hmac}` | RESTful + token kiracıyla eşleşir |
| Boş portal | `NoDebt.cshtml` | Token geçerli + aktif borç yok → bilgi sayfası |
| Tutar gösterim | Yalnız kalan tutar; kısmi ödenmişse `(Toplam X TL, Y TL ödendi)` alt notu | Ana bilgi öne çıkar |
| Subject | `"Kira borç hatırlatma — {N} bekleyen ödeme"` | Sayı ile bilgi yoğun |
| Eski tahakkuk-bazlı link uyumluluğu | Yok (henüz gerçek mail gönderilmedi) | YAGNI |
| POS, kart kaydı, scheduler, audit | Kapsam dışı | Sonraki fazlar |

### 13.8.2 Etkilenen Dosyalar

**Yeni:**
- `Migrations/{tarih}_AddSonHatirlatmaTarihiToKiraTahakkuk.cs`
- `Models/ViewModels/KiraciBorcHatirlatmaMailModel.cs`
- `Models/ViewModels/KiraciOdemePortalViewModel.cs`
- `Views/OdemePortal/NoDebt.cshtml`
- `Services/Interfaces/IBorcHatirlatmaService.cs` + `Services/BorcHatirlatmaService.cs`

**Değişecek:**
- `Models/KiraTahakkuk.cs` — `DateTime? SonHatirlatmaTarihi` alanı
- `Authorization/PermissionCatalog.cs` — `Bildirim.BorcHatirlatma` static class
- `Models/Settings/PaymentLinkSettings.cs` — `int ReminderCooldownDays = 7`
- `appsettings.json` — `PaymentLink:ReminderCooldownDays: 7`
- `Services/PaymentLinkService.cs` + `IPaymentLinkService.cs` — payload `tahakkukId`→`kiraciId`; `BuildLink(int kiraciId)`, `TryValidate(int kiraciId, string token, out string? reason)`
- `Controllers/SozlesmeController.cs` — `BorclularaMailGonder` `IBorcHatirlatmaService`'a delege; Index sayım sorgusu güncelle
- `Controllers/OdemePortalController.cs` — route `{id}`→`{kiraciId}`; yeni view model
- `Views/Sozlesme/Index.cshtml` — buton permission gate (`Bildirim.BorcHatirlatma`)
- `Views/Shared/EmailTemplates/BorcHatirlatma.cshtml` — kiracı başlığı + borç listesi + tek CTA
- `Views/OdemePortal/Index.cshtml` — tam yeniden yazılır (split-screen, Alpine radio-card)
- `Views/OdemePortal/Invalid.cshtml` — küçük metin güncelleme
- `Program.cs` — DI kayıtları (yeni servis + policy)
- `docs/PROGRESS.md` — 13.8 alt-bölüm checklist
- `docs/permission-catalog.md` — `Bildirim.BorcHatirlatma` eklenir

**Silinecek:**
- `Models/ViewModels/BorcHatirlatmaMailModel.cs` (kiracı bazlı versiyon devralır)
- `Models/ViewModels/OdemePortalViewModel.cs` (kiracı bazlı versiyon devralır)

### 13.8.3 Mimari Notlar

#### Borçlu Sorgusu — Cooldown

```csharp
var bugun = DateTime.Today;
var vadeEsigi = bugun.AddDays(_paymentLinkOptions.Value.ReminderDaysBefore);
var cooldownEsigi = bugun.AddDays(-_paymentLinkOptions.Value.ReminderCooldownDays);

var bekleyenTahakkuklar = await _ctx.KiraTahakkuklar
    .Include(t => t.KiraSozlesmesi).ThenInclude(s => s!.Kiraci)
    .Include(t => t.KiraSozlesmesi).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
    .Include(t => t.Odemeler)
    .Where(t => t.Durum != TahakkukDurumu.TamOdendi
             && t.Durum != TahakkukDurumu.IptalEdildi
             && t.KiraSozlesmesi != null
             && t.KiraSozlesmesi.KiraciId != 0
             && t.VadeTarihi <= vadeEsigi)
    .ToListAsync();

foreach (var grup in bekleyenTahakkuklar.GroupBy(t => t.KiraSozlesmesi!.KiraciId))
{
    var cooldownDisi = grup.Where(t =>
        t.SonHatirlatmaTarihi == null || t.SonHatirlatmaTarihi <= cooldownEsigi).ToList();

    if (cooldownDisi.Count == 0) { atlandiCooldown++; continue; }

    // mail içeriği: grup'taki TÜM borçlar listelenir
    // başarılı gönderim sonrası: SADECE cooldownDisi[i].SonHatirlatmaTarihi = bugun
}
```

#### Token (`PaymentLinkService`)

```
plaintext: "{kiraciId}|{expiresUnixSeconds}"
hmac      = HMACSHA256(plaintext, PaymentLink:Secret)
token     = "{expiresUnixSeconds}.{Base64Url(hmac)}"
link      = "{BaseUrl}/Odeme/Portal/{kiraciId}?t={token}"
```

Doğrulama: expires < now → "Token süresi dolmuş" → `CryptographicOperations.FixedTimeEquals` ile HMAC karşılaştır.

#### Portal Controller

```csharp
[AllowAnonymous]
[Route("Odeme/Portal/{kiraciId:int}")]
public async Task<IActionResult> Index(int kiraciId, string t)
{
    if (!_paymentLink.TryValidate(kiraciId, t, out var reason)) return View("Invalid", reason);

    var kiraci = await _ctx.Kiracilar.FindAsync(kiraciId);
    if (kiraci == null) return View("Invalid", "Kiracı bulunamadı");

    var borclar = await _ctx.KiraTahakkuklar
        .Include(t => t.KiraSozlesmesi).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
        .Include(t => t.Odemeler)
        .Where(t => t.KiraSozlesmesi!.KiraciId == kiraciId
                 && t.Durum != TahakkukDurumu.TamOdendi
                 && t.Durum != TahakkukDurumu.IptalEdildi)
        .OrderBy(t => t.VadeTarihi)
        .ToListAsync();

    if (borclar.Count == 0) return View("NoDebt", new { kiraci.Ad, kiraci.Soyad });
    return View(new KiraciOdemePortalViewModel { /* ... */ });
}
```

#### Portal UI (Split-Screen)

`Views/OdemePortal/Index.cshtml` — Tailwind `lg:grid-cols-2`, mobilde alt alta:

**Sol panel** (UI-only form): kart no, AA/YY, CVV, kart sahibi. Submit butonu `disabled` + sarmalayıcı `<div title="Online ödeme yakında aktif olacak. Ödemeniz için lütfen yönetiminizle iletişime geçin." class="cursor-not-allowed">`. Tutar göstergesi Alpine `x-text` ile seçili borca bağlı.

**Sağ panel** (Alpine radio-card):

```html
<div x-data="{ selected: @firstId, selectedAmount: '@firstAmount' }">
  @foreach (var b in borclar) {
    <label :class="selected === @b.Id ? 'border-blue-500 bg-blue-50' : 'border-gray-200'">
      <input type="radio" x-model.number="selected" value="@b.Id" class="hidden"
             @@change="selectedAmount = '@formattedAmounts[b.Id]'">
      <!-- birim, dönem, vade, kalan tutar -->
      @if (kismiOdenmis) {
        <span class="text-xs text-gray-500">(Toplam @b.ToplamTutar TL, @odenen TL ödendi)</span>
      }
      @if (b.VadeTarihi < DateTime.Today) {
        <span class="text-xs text-red-600">Vadesi geçmiş</span>
      }
    </label>
  }
</div>
```

Default seçim: en eski vadeli (`OrderBy(VadeTarihi)` → ilk).

#### Servis Ayrımı

`IBorcHatirlatmaService.GonderAsync()` controller'dan bağımsız:
- Sorgu, gruplama, mail render, send loop, cooldown update burada
- Controller sadece servisi çağırır → DTO alır → TempData → redirect
- Sonuç DTO: `BorcHatirlatmaSonucDto { int KiraciSayisi, int TahakkukSayisi, int AtlandiCooldown, int AtlandiEmailBos, int HataSayisi }`
- İleride `IHostedService` doğrudan bu servisi çağırabilir

#### Permission

```csharp
public static class Bildirim
{
    public const string BorcHatirlatma = "Bildirim.BorcHatirlatma";
    public static IEnumerable<string> All() => new[] { BorcHatirlatma };
}
```

- `PermissionCatalog.All` collection'a `Bildirim.All()` katılır → `Program.cs` policy otomatik kayıt
- `SozlesmeController.BorclularaMailGonder`: `[Authorize(Policy = PermissionCatalog.Bildirim.BorcHatirlatma)]`
- `Views/Sozlesme/Index.cshtml` buton: `@if (User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Bildirim.BorcHatirlatma)) { ... }`
- Mevcut oturumlu kullanıcı: logout/login gerekir (`PermissionClaimsTransformer` cache)

#### Config Guard

`BorcHatirlatmaService.GonderAsync` başında:
- `PaymentLink:Secret` boş veya < 32 char → `throw new InvalidOperationException("PaymentLink:Secret yapılandırılmamış (min 32 karakter)")`
- `Smtp:Host` veya `Smtp:From` boş → `throw new InvalidOperationException("SMTP yapılandırılmamış")`
- Controller yakalar → `TempData["Error"]` ile gösterir

### 13.8.4 Görev Adımları

1. `KiraTahakkuk.SonHatirlatmaTarihi` + migration `AddSonHatirlatmaTarihiToKiraTahakkuk`
2. `PaymentLinkSettings.ReminderCooldownDays` + `appsettings.json` `PaymentLink:ReminderCooldownDays: 7`
3. `PaymentLinkService` payload `kiraciId`'ye geçer (imza + impl)
4. `PermissionCatalog.Bildirim.BorcHatirlatma` + `docs/permission-catalog.md`
5. `KiraciBorcHatirlatmaMailModel` + `KiraciOdemePortalViewModel`
6. `IBorcHatirlatmaService` + `BorcHatirlatmaService` (sorgu + gruplama + send + cooldown update + config guard)
7. `Views/Shared/EmailTemplates/BorcHatirlatma.cshtml` yeniden yaz (kiracı + borç listesi + tek CTA)
8. `SozlesmeController.BorclularaMailGonder` servise delege; `Index` sayım sorgusu güncelle
9. `Views/Sozlesme/Index.cshtml` buton permission gate
10. `OdemePortalController` kiracı bazlı route + sorgu
11. `Views/OdemePortal/Index.cshtml` split-screen UI baştan yaz
12. `Views/OdemePortal/NoDebt.cshtml` yeni
13. `Views/OdemePortal/Invalid.cshtml` küçük metin güncelleme
14. Eski `BorcHatirlatmaMailModel` + `OdemePortalViewModel` sil
15. `Program.cs` DI kayıtları
16. `dotnet build` — 0 hata
17. `docs/PROGRESS.md` — 13.8 alt-bölüm checklist

### 13.8.5 Acceptance Kriterleri

1. `dotnet build` → 0 C# / Razor hatası
2. Migration uygulanır; mevcut tahakkuklar `SonHatirlatmaTarihi = NULL`
3. 3 farklı kiracıya ait 6 tahakkuk seed → buton → **3 mail** gider (kiracı sayısı)
4. Mail HTML: kiracı adı + tüm borç satırları (kalan tutar + kısmi alt not) + tek "Ödeme Yap" CTA
5. Aynı butona hemen tekrar bas → **0 mail**; "Tüm kiracılar cooldown'da" özet
6. Cooldown süresi geçince tekrar bas → tekrar mail
7. Mail linki → `/Odeme/Portal/{kiraciId}?t={hmac}` → split-screen portal
8. Sağ panelde tüm bekleyen borçlar; varsayılan en eski vadeli seçili
9. Borca tıkla → sol paneldeki tutar Alpine ile güncellenir; buton disabled kalır
10. Disabled butona hover → tooltip mesajı
11. Token süresi dolmuş URL → `Invalid` sayfası
12. Token geçerli + 0 borç → `NoDebt` sayfası
13. Goruntuleyici rolü → buton görünmez; doğrudan POST → 403
14. SMTP boş → "SMTP yapılandırılmamış" hata; mail atılmaz
15. `Secret` < 32 char → "Secret yapılandırılmamış" hatası
16. Mobilde portal: paneller alt alta düzgün; kartlar tam genişlik
17. `permission-catalog.md` + `PROGRESS.md` güncel
18. (Manuel) Smoke test: gerçek SMTP ile 1 kiracı mail; portal + tooltip kontrolü

### 13.8.6 Kapsam Dışı

POS / iyzico / Param entegrasyonu, kart bilgisi backend POST/kayıt, `BildirimLog` audit, Hangfire/`IHostedService` scheduler, SMS/push, mail throughput optimizasyonu, çoklu dil.

### 13.8.7 Riskler

- **Permission claim cache**: Mevcut oturumlu kullanıcılar logout/login yapmadan yeni permission'ı görmez
- **Disabled button hover**: `<button disabled>` bazı tarayıcılarda hover event almaz; sarmalayıcı `<div title=...>` ile çözüldü
- **Çoklu sözleşme**: Kiracının tüm sözleşmelerinin borçları portalda görünür — kabul edildi
- **Eski link kırılması**: Henüz gerçek mail gönderilmediği için sorun yok
