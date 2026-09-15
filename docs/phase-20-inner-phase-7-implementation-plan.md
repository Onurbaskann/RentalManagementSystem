# Faz 20 / İç Faz 7 — Paratika Hosted Payment Page Entegrasyonu Implementation Plan

**Durum:** Implementasyon tamamlandı (backend + kiracı arayüzü tetikleyicisi); `dotnet build`/
`dotnet test` tam yeşil (776/776, 0 skip). Gerçek Paratika sandbox'ına karşı manuel duman
testi ve kullanıcı onayı bekleniyor.
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)
**Ön koşul:** İç Faz 6 implementasyonu tamamlandı; kullanıcı "Faz 7'ye geçelim" diyerek bir
sonraki faza yönlendi (2026-09-11, ayrı yazılı onay cümlesi alınmadı). Bu arada proje Clean
Architecture katmanlarına ayrıldı (`src/KiraTakip.{Domain,Application,Infrastructure,Web}`);
İç Faz 6 kodu sorunsuz taşındı, 757/757 test yeşildi.
**Kapsam:** Gerçek Paratika API'sine bağlanan uçtan uca bir ödeme akışı: oturum başlatma
(`SESSIONTOKEN`) → hosted ödeme sayfasına yönlendirme → kullanıcı geri döner → authenticated
`QUERYTRANSACTION` ile gerçek sonuç öğrenilir → tek bir Approved ödeme oluşturulur. Hem backend
(provider, completion servisi) hem kiracı arayüzündeki tetikleyici ("Kart ile Öde" butonu) dahil.
**Kapsam dışı:** `NOTIFICATIONURL` webhook endpoint'i, arka plan mutabakat worker'ı, çoklu
instance idempotency kilidi, callback-query yarışı testleri — bunlar İç Faz 8'de.

---

## 1. Kesinleşen kararlar

- **Doküman güvenilirliği:** Kullanıcı bu oturumda Paratika API v2 dokümanının SESSIONTOKEN ve
  QUERYTRANSACTION bölümlerinin ham metnini doğrudan yapıştırdı (WebFetch özetlemesi değil) —
  tam istek/yanıt alan listeleri ve örnekleri güvenilir şekilde bilinir hale geldi.
- **İmza doğrulama bilinçli olarak implement edilmedi.** Bulunan `sdSha512`/`SD_SHA512` imza
  şeması dokümanın **Direct POST/MOTO** (server-to-server kart geçişi) bölümüne ait — bizim
  kullandığımız **Hosted Payment Page (SESSIONTOKEN)** akışına uygulanabilirliği doğrulanamadı.
  **Mimari karşılık:** RETURNURL/NOTIFICATIONURL'den gelen hiçbir veriye (tutar, durum, imza)
  güvenilmez; geri dönüş yalnızca "bu `merchantPaymentId`'yi kendi kimlik bilgilerimizle
  sorgula" tetikleyicisi olarak kullanılır — asıl karar her zaman authenticated
  `QUERYTRANSACTION` çağrısının sonucuna dayanır. Bu, ana planın İç Faz 8 için zaten hedeflediği
  "callback ve QueryTransaction aynı completion servisini kullanır" ilkesiyle tam uyumlu.
- **RETURNURL endpoint'i authenticated kalır, `[AllowAnonymous]` eklenmedi** —
  `TenantChargeController.OnlinePaymentReturn`, `[Authorize(Policy = "TenantUser")]` +
  `[RequireKiraciId]` altında; Paratika'nın hosted sayfasına gidiş top-level browser redirect'i
  olduğu için tarayıcı çerezleri korunuyor, kiracı oturumu kopmuyor.
- **`ApiBaseUrl`/`HostedPaymentPageBaseUrl` doğrulanmamış varsayımlar** — `ApiBaseUrl` dokümanın
  kendi sayfa adresinden (`/paratika/api/v2/doc` → `/paratika/api/v2`) türetildi;
  `HostedPaymentPageBaseUrl` formatı (`/payment/[SESSION_TOKEN]`) bir önceki WebFetch özetinden
  geliyor, kullanıcı tarafından ham metinle doğrulanmadı. Yanlışsa yalnız config/404 sorunu —
  güvenlik riski yok. DURMA KAPISI C'de gerçek sandbox'a karşı ilk kez doğrulanacak.
- **CUSTOMER/CUSTOMERNAME/CUSTOMEREMAIL/CUSTOMERPHONE eşlemesi** — `Tenant.TenantNo`/`Name`/
  `Email`/`Phone` (hepsi zorunlu/non-nullable alan, doğrudan eşleşiyor).
- **`SESSIONEXPIRY` saat formatına yuvarlanır** (`ParatikaOptions.SessionExpiryMinutes` →
  `"{saat}h"`, en az `"1h"`) — dokümanda yalnız saat birimli örnek (`168h`) doğrulandı.
- **`ReturnUrl`/`SessionType` provider'ın kendi sorumluluğunda** — `CreatePaymentSessionRequest`
  DTO'sunda YOK; provider kendi `ParatikaOptions.ReturnUrl`'ini okur, `SESSIONTYPE` sabit
  `"PAYMENTSESSION"` olarak hardcode edilir (ortak orkestrasyon servisi Paratika config'ine
  bağımlı olmasın diye — provider-nötr DTO ilkesi korunur).
- **Yeni "doğrudan Approved oluşturma" kod yolu** — bugüne kadar tüm ödemeler `PendingApproval`
  ile başlıyordu. Yeni `PaymentAllocation`, sanal POS onaylı sonucunda doğrudan
  `Status = Approved`, `PaymentSourceType = VirtualPos`, `PosReferenceNo = pgTranId` ile
  yazılır; `ApprovedByUserId` null kalır (insan onaycı yok, sistem onayı).
- **Yeni `PaymentChannel.Card = 5` enum değeri eklendi** — mevcut enumda kart/online kanalı
  yoktu (`BankTransfer/Eft/Cash/Other`); küçük, kolon tipini değiştirmeyen bir migration
  (`AddCardPaymentChannel`, yalnız SQL comment güncellemesi) eklendi.
- **"Kart ile Öde" butonu aktifleştirildi** (İç Faz 5'te bırakılan `disabled`/"YAKINDA"
  placeholder kaldırıldı) — bu, ilk yazılan plan metninde açıkça yer almıyordu; implementasyon
  sırasında fark edilip kullanıcıya sorularak (AskUserQuestion) onaylandı ve aynı oturumda
  eklendi. POS sekmesinde şu an **yalnız kalemin tam kullanılabilir tutarı** gönderiliyor —
  havale/EFT sekmesindeki gibi ayrı, düzenlenebilir bir tutar alanı yok (kapsam dışı bırakıldı).

---

## 2. Yeni/Değişen kod

**Yeni:**
- `Infrastructure/Payments/ParatikaOnlinePaymentProvider.cs` — `CreateSessionAsync` (form-
  urlencoded POST, `ACTION=SESSIONTOKEN`), `QueryAsync` (`ACTION=QUERYTRANSACTION`), `Validate
  CallbackAsync` (no-op, yalnız `merchantPaymentId` okur), `BuildHostedPaymentPageUrl`.
- `tests/.../Unit/Payments/ParatikaOnlinePaymentProviderTests.cs` — `StubHttpMessageHandler`
  ile gerçek HTTP olmadan form alanları ve JSON ayrıştırma; kullanıcının yapıştırdığı gerçek
  örnek istek/yanıtlar sabit test verisi.
- Migration `AddCardPaymentChannel` (yalnız kolon yorumu güncellemesi).

**Değişecek:**
- `Models/DTOs/OnlinePayment/OnlinePaymentProviderDtos.cs` — `CreatePaymentSessionRequest`'e
  `CustomerCode/Name/Email/Phone` eklendi.
- `Models/DTOs/OnlinePayment/OnlinePaymentInputs.cs` — `InitiateOnlinePaymentResult.RedirectUrl`
  eklendi; yeni `CompleteOnlinePaymentInput`/`CompleteOnlinePaymentResult` eklendi.
- `Services/Interfaces/Payments/IOnlinePaymentService.cs` — `CompleteAsync` eklendi.
- `Services/Interfaces/Payments/IOnlinePaymentProvider.cs` — `BuildHostedPaymentPageUrl`
  eklendi.
- `Services/Interfaces/Payments/IOnlinePaymentBusinessRules.cs` +
  `Services/Payments/OnlinePaymentBusinessRules.cs` — `NormalizeProviderStatus` eklendi (ana
  plan §5.4 eşlemesi: `AP`→Approved, `FA`/`CA`→Failed, `VD`→Cancelled, `IP`→Pending,
  `MR`/diğer/boş→Unknown).
- `Services/Payments/OnlinePaymentService.cs` — `InitiateAsync` artık `ITenantRepository`'den
  müşteri bilgisi çekip provider'a geçiriyor ve dönüşe `RedirectUrl` ekliyor; yeni
  `CompleteAsync` metodu eklendi (bkz. §3).
- `Repositories/Interfaces/Payments/IOnlinePaymentTransactionRepository.cs` + implementasyon —
  `GetByMerchantPaymentIdAsync` eklendi (tracked, `ChargeLineItem`+`Charge` include).
- `Infrastructure/DependencyInjection/InfrastructureModule.cs` —
  `AddHttpClient<IOnlinePaymentProvider, ParatikaOnlinePaymentProvider>(...)` (projede ilk
  `IHttpClientFactory` kullanımı — grep ile teyit edildi, mevcut bir konvansiyon yoktu).
- `Domain/Models/Enums/Enums.cs` — `PaymentChannel.Card = 5`.
- `Web/Controllers/TenantChargeController.cs` — `StartOnlinePayment` (yeni, `[Authorize]` +
  `[ValidateAntiForgeryToken]`, `InitiateAsync` çağırıp Paratika'ya `Redirect()` eder) ve
  `OnlinePaymentReturn` (yeni, RETURNURL dönüşünü karşılar, `CompleteAsync` çağırır — anti-
  forgery YOK, çünkü Paratika'nın kendi POST-redirect'i, bizim formumuz değil).
- `Web/Views/TenantCharge/{Details,Index}.cshtml` — "Kart ile Öde" sekmesindeki "YAKINDA" rozeti
  ve devre dışı buton kaldırıldı; gerçek bir form (`StartOnlinePayment`'e POST) eklendi.
- `appsettings.json` — `Paratika.ApiBaseUrl`/`HostedPaymentPageBaseUrl` test ortamı değerleriyle
  dolduruldu; `ReturnUrl` hâlâ placeholder (gerçek public base URL yayında girilecek).

**Dokunulmadı:** `PaymentService.cs`, migration şeması (`OnlinePaymentTransaction`/`Event`
tabloları İç Faz 6'da zaten tüm gerekli alanları içeriyordu).

---

## 3. `CompleteAsync` akışı

1. `GetByMerchantPaymentIdAsync` — bulunamazsa `ONLINE_PAYMENT_TRANSACTION_NOT_FOUND`.
2. Tenant sahiplik kontrolü — `transaction.ChargeLineItem.Charge.TenantId != input.TenantId` ise
   `ONLINE_PAYMENT_TRANSACTION_FORBIDDEN`.
3. `AcquirePaymentLockAsync(transaction.ChargeLineItemId)` (İç Faz 6'dan reuse).
4. Provider + `StoreAccount` çözümlenir, `provider.QueryAsync(...)` çağrılır.
5. `NormalizeProviderStatus` ile yeni durum belirlenir; `LastInquiryAt`/`InquiryCount` her
   zaman güncellenir, bir `InquiryPerformed` event'i her zaman yazılır.
6. `IsValidStatusTransition(eski, yeni)` **false** ise (örn. zaten terminal durumda, sayfa
   yenilendi) sessizce mevcut durum döner — hata fırlatılmaz (idempotent no-op).
7. **Approved** ise: `PaymentAllocation` (`Approved`, `VirtualPos`, `PosReferenceNo = pgTranId`)
   oluşturulur, `transaction.PaymentAllocationId` doldurulur, `Succeeded` event'i yazılır,
   `IChargeService.UpdatePaidAmountAsync` çağrılır (değiştirilmeden reuse).
8. **Failed/Cancelled** ise: `Failed` event'i yazılır, hiçbir ödeme oluşturulmaz.

---

## 4. Testler

- `OnlinePaymentServiceTests`e eklenen: `CompleteAsync_ShouldCreateApprovedPaymentAllocation
  _WhenQueryReturnsApproved`, `..._ShouldNotCreatePayment_WhenQueryReturnsFailed`,
  `..._ShouldBeIdempotent_WhenCalledAgainAfterApproval`, `..._ShouldThrowForbidden_When
  TenantDoesNotOwnTransaction`.
- `OnlinePaymentBusinessRulesTests`e eklenen: `NormalizeProviderStatus` için `AP`/`FA`/`CA`/
  `VD`/`IP`/`MR`/bilinmeyen/`null` kombinasyonları (8 teori verisi).
- Yeni `ParatikaOnlinePaymentProviderTests` (6 test): form alanlarının doğru kurulduğu,
  `SESSIONEXPIRY` yuvarlaması, `QUERYTRANSACTION` yanıt ayrıştırma (boş liste dahil),
  `BuildHostedPaymentPageUrl`, `ValidateCallbackAsync`'in no-op davranışı.

Sonuç: `dotnet test` → 776/776 geçti (757 önceki + 19 yeni).

**Kapsam dışı test:** Gerçek Paratika sandbox'ına giden bir entegrasyon testi yazılmadı (secret
gerektirir, CI'da çalıştırılamaz) — yalnız DURMA KAPISI C'deki manuel duman testinde denenecek.

---

## 5. Durma kapıları

- **DURMA KAPISI A** (migration): tamamlandı — `AddCardPaymentChannel`, yalnız kolon yorumu,
  test DB'de uygulandı.
- **DURMA KAPISI B** (`dotnet build` + `dotnet test` tam yeşil): tamamlandı (776/776).
- **DURMA KAPISI C** (manuel duman testi): **BLOKE** (2026-09-14) — test merchant kimlik
  bilgisi (`MERCHANT`/`MERCHANTUSER`/`MERCHANTPASSWORD`) yok. Bu bilgi genel API dokümanında
  yayınlanmıyor; Paratika/Payten'den hesaba özel bir test/sandbox üye iş yeri kaydı talep
  edilmesi gerekiyor (test kartları dokümanda mevcut, ama kimlik bilgisi olmadan `SESSIONTOKEN`
  dahil hiçbir istek atılamıyor). Kimlik bilgisi geldiğinde bir `StoreAccount` seed edilip
  uçtan uca ödeme denenecek — bu adım şunları ilk kez gerçek sunucuya karşı doğrulayacak:
  `ApiBaseUrl` yolu, `HostedPaymentPageBaseUrl` formatı, RETURNURL POST body'sindeki gerçek
  alan adı (`merchantPaymentId` varsayımı — büyük/küçük harf iki varyant da deneniyor),
  `SESSIONEXPIRY` formatının kabul edilip edilmediği. **Kullanıcı kararı:** kimlik bilgisi
  gelene kadar İç Faz 8'e geçilmeyecek, bu noktada durulacak.
- **DURMA KAPISI D** (kullanıcı onayı): bekleniyor (DURMA KAPISI C'ye bağımlı).
