# Faz 20 / İç Faz 5 — Authenticated Kiracı Ödeme Deneyimi ve Token Kaldırma Implementation Plan

**Durum:** Kullanıcı tarafından kabul edildi (2026-09-01); `dotnet build` ve `dotnet test` tam yeşil (333/333, 0 skip). Manuel kontrolde sorun bulunmadı.
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)
**Ön koşul:** İç Faz 4 tamamlandı ve kullanıcı tarafından kabul edildi (2026-08-31).
**Kapsam:** Anonim, tokenlı `/Payment/Portal` altyapısının tamamen kaldırılması; borç hatırlatma
e-postalarının kiracının zaten var olan authenticated portalına (`TenantChargeController`)
deep-link vermesi; POS sekmesindeki sahte kart formu alanlarının kaldırılması; IDOR/tenant
izolasyon regresyon testleri.
**Kapsam dışı:** Provider soyutu ve gerçek sanal POS entegrasyonu (İç Faz 6/7/8) — POS sekmesi
hâlâ placeholder, yalnız içeriği "kart alanı" barındırmayacak şekilde sadeleştirildi.

---

## 1. Kesinleşen kararlar

- **Hatırlatma linki hedefi:** `{scheme}://{host}/Tenant/Charges/Details/{charge.Id.ToHashId()}`.
  Mevcut `TenantChargeController.Details(int id)` route'u zaten global
  `HashidsModelBinderProvider` ile otomatik çözülüyor; controller'da değişiklik gerekmedi.
- **Base URL üretimi:** Yeni bir ayar sınıfı eklenmedi — `InvitationService`/
  `PasswordResetService`'teki mevcut desen (`IHttpContextAccessor` → `{Scheme}://{Host}`,
  yoksa `http://localhost:5031` fallback) `ChargeReminderService`'e de uygulandı.
- **Alıcı listesi (kullanıcı kararı, 2026-08-31):** Hatırlatma e-postaları hâlâ **`Tenant.Email`**
  (kiracı firma adresi) adresine gönderiliyor — bireysel portal kullanıcılarının şahsi
  e-postalarına dağıtılmıyor. Kullanıcının gerekçesi: kiracı bilgilerinin bireysel şahsi
  maillere gitmesi veri güvenliği açısından uygun değil. Bu, ana plandaki "hatırlatma
  alıcılarının aktif/yetkili tenant kullanıcılarıyla uyumlu hale getirilmesi" maddesini
  **bilinçli olarak** "mevcut tek-alıcı (tenant.Email) davranışı korunur" şeklinde kapatıyor.
- Link artık süre sınırlı değil — kalıcı bir authenticated route'tur; e-postadaki "X saat
  geçerlidir" ifadesi kaldırıldı, yerine "giriş yapmanız gerekir" notu eklendi.
- POS (Kart ile Öde) sekmesindeki sahte kart numarası/CVV/son kullanma `<input>` alanları
  kaldırıldı; yerine "kart bilgileriniz hiçbir zaman KiraTakip üzerinden geçmez" notu kondu.
  Sekme/rozet ve devre dışı gönder butonu korunuyor (İç Faz 6/7 gerçek entegrasyonu bekliyor).

---

## 2. Kaldırılanlar

**Controller/View:** `PaymentPortalController.cs`, `Views/PaymentPortal/*` (Index/Invalid/
NoDebt), `Views/Shared/_PortalLayout.cshtml`.

**Domain/Data:** `PaymentLinkRecord` entity + `PaymentLinkStatus` enum,
`ApplicationDbContext`'teki `DbSet`/config bloğu, `IPaymentLinkRecordRepository`/
`PaymentLinkRecordRepository`, `IChargeRepository.GetPaymentPortalChargesAsync` (+ repository
implementasyonu).

**Servis:** `IPaymentLinkService`/`PaymentLinkService`, `IPaymentPortalService`/
`PaymentPortalService`. (`ISecureTokenService` dokunulmadı — `InvitationService`/
`PasswordResetService` de kullanıyor.)

**DTO/ViewModel/Validator:** `PaymentPortalDtos.cs`, `PaymentLinkDtos.cs`,
`PaymentPortalRequestViewModel.cs`, `TenantPaymentPortalViewModel.cs`,
`PaymentPortalRequestViewModelValidator.cs`.

**Config/Ayar:** `PaymentLinkSettings.cs`, `appsettings.json`'daki `"PaymentLink"` bloğu,
`SystemSettingDefinitions.Payment.LinkValidityHours` (sabit + tanım kaydı + parse satırı),
`OperationalPolicySettings.PaymentLinkValidityHours`.

**DI:** `ServiceModule.cs`, `RepositoryModule.cs`, `InfrastructureModule.cs`'deki ilgili
kayıt satırları.

**Test:** `PaymentPortalArchitectureTests.cs` (tamamen silindi); `SystemSettingTests.cs`'teki
2 `PaymentLinkValidityHours` assertion'ı kaldırıldı.

**Ek bulgu (plandan sonra tespit edildi):** `SeedDataService.ClearDomainDataAsync()` içinde
`_ctx.OdemeLinkKayitlari.RemoveRange(...)` satırı vardı — kaldırıldı (build hatasıyla
yakalandı, plan dosyasının envanterinde yoktu).

**Migration:** `20260901060941_RemovePaymentLinkInfrastructure` — `OdemeLinkKayitlari`
tablosunu (FK/index dahil) düşürür; `SistemAyarlari` tablosundan `Payment.LinkValidityHours`
anahtarlı satırı `DELETE` ile temizler. Test DB'de uygulandı.

---

## 3. Yeni/Değişen

- **`Services/ChargeReminderService.cs`:** `IPaymentLinkService`/`IOptions<PaymentLinkSettings>`
  bağımlılıkları çıkarıldı; `IHttpContextAccessor` eklendi. `SendDebtRemindersAsync` artık her
  `charge` için ayrı bir `ChargeDetailsUrl` üretiyor (tek tenant-seviyeli link yerine).
- **`Models/ViewModels/TenantDebtReminderEmailViewModel.cs`:** `PaymentLink`/
  `PaymentLinkValidityText` kaldırıldı; `DebtReminderLineViewModel.ChargeDetailsUrl` eklendi.
- **`Views/Shared/EmailTemplates/DebtReminder.cshtml`:** Tek genel link yerine her borç
  satırına kendi "Detayları Gör ve Öde →" linki eklendi; "giriş yapmanız gerekir" notu kondu.
- **`Views/TenantCharge/Details.cshtml`, `Views/TenantCharge/Index.cshtml`:** POS sekmesi sahte
  kart formu → bilgi notu.

---

## 4. Testler

Yeni: `TenantChargeAuthorizationTests.cs` (4 test — farklı kiracı erişimi reddedilir, kapsam
dışı birim erişimi reddedilir, aynı kiracı+kapsam içinde ve global erişimde başarılı),
`ChargeReminderServiceTests.cs` (3 test — üretilen linkin `/Tenant/Charges/Details/` deseninde
olduğu, e-postanın hâlâ `tenant.Email`'e gittiği, `HttpContext` yokken localhost'a düştüğü).

**Bilinçli olarak yazılmayan test:** Login→returnUrl round-trip'ini uçtan uca doğrulayan bir
`WebApplicationFactory` tabanlı HTTP entegrasyon testi. Bu proje hiçbir yerde böyle bir test
altyapısı kullanmıyor (grep ile doğrulandı) ve `AccountController.Login`'deki
`Url.IsLocalUrl` kontrolü bu fazda değiştirilmeyen, zaten var olan kod. Yeni bir ağır test
altyapısı kurmak bu fazın kapsamına göre orantısız görüldü.

Sonuç: `dotnet test` → 333/333 geçti (326 mevcut − 7 silinen `PaymentPortalArchitectureTests`
+ 4 yeni `TenantChargeAuthorizationTests` + 3 yeni `ChargeReminderServiceTests` = 333/333).

---

## 5. Durma kapıları

- **DURMA KAPISI A** (migration): tamamlandı, test DB'de uygulandı.
- **DURMA KAPISI B** (`dotnet build` + `dotnet test` tam yeşil): tamamlandı.
- **DURMA KAPISI C** (manuel duman testi): tamamlandı — kullanıcı hatırlatma e-postasını
  tetikleyip linkin doğru formatta geldiğini, login olmadan tıklanınca login'e düşüp giriş
  sonrası aynı sayfaya döndüğünü ve POS sekmesinde sahte kart formu kalmadığını doğruladı;
  sorun bulunmadı.
- **DURMA KAPISI D** (kullanıcı onayı): İç Faz 5 kullanıcı tarafından kabul edildi (2026-09-01).

**Bilinen, kalıcı olmayan not:** `PaymentReminderDaysBefore`/`PaymentReminderCooldownDays`
sistem ayarları (Payment.LinkValidityHours'tan bağımsız, ayrı borç hatırlatma zamanlama
ayarları) korunuyor — bu fazda dokunulmadı.
