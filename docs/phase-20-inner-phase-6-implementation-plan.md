# Faz 20 / İç Faz 6 — Provider Soyutu ve Sanal POS İşlem Omurgası Implementation Plan

**Durum:** Implementasyon tamamlandı; `dotnet build`/`dotnet test` tam yeşil (352/352, 0 skip).
Kullanıcı onayı bekleniyor.
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)
**Ön koşul:** İç Faz 5 tamamlandı ve kullanıcı tarafından kabul edildi (2026-09-01).
**Kapsam:** Gerçek Paratika çağrısı yapmadan sanal POS işleminin ortak omurgası — tek bir
`IOnlinePaymentProvider` soyutu, provider-nötr işlem kaydı (`OnlinePaymentTransaction` +
append-only `OnlinePaymentEvent`), bunu yöneten orchestration servisi (`OnlinePaymentService.
InitiateAsync` — yalnız "initiate" ucu).
**Kapsam dışı:** Gerçek Paratika entegrasyonu (`ParatikaOnlinePaymentProvider`, HTTP çağrıları,
imza doğrulama) İç Faz 7'de; callback ayrıştırma ve idempotent tamamlama (`PaymentAllocation`
dönüşümü) İç Faz 8'de. Bu faz saf backend — controller/view değişikliği yok, "Kart ile Öde"
sekmesi İç Faz 5'te bırakıldığı gibi placeholder kalır.

---

## 1. Kesinleşen kararlar

- **Provider seçimi:** `IBankaHareketiParser`/`AkbankCsvParser` deseni birebir kopyalandı —
  yeni somut provider eklendiğinde `AddSingleton<IOnlinePaymentProvider, ...>()` ile kaydedilecek;
  İç Faz 6'da hiçbir somut provider kaydı yok (yalnız test projesindeki `FakeOnlinePaymentProvider`).
- **Kilitleme/bakiye:** Yeni kod yazılmadı — `ChargeLineItemRepository.AcquirePaymentLockAsync`
  ve `GetPaymentBalanceAsync` doğrudan reuse edildi.
- **İş kuralı deseni:** `IOnlinePaymentBusinessRules : IBusinessRules` — `BusinessRulesModule`'ün
  reflection taramasıyla DI'a otomatik kaydedildi, elle `services.AddScoped(...)` yazılmadı.
- **`ParatikaOptions`:** Kullanıcı incelemesi sonucu düz POCO olarak eklendi (`SmtpSettings`/
  `SecureTokenSettings` ile aynı şekil) — `DataAnnotations`/`ValidateOnStart()` **kullanılmadı**;
  bu desen projede hiç kullanılmıyor (grep ile doğrulandı). Doğrulama İç Faz 7'de gerçek HTTP
  çağrısı eklendiğinde kullanım anında `Guard.Against(...)` ile yapılacak.
- **Ayarların yeri:** `SistemAyarlari` tablosuna taşınmadı — o tablo admin panelinden serbestçe
  düzenlenebilen Integer/Text iş politikası değerleri için (kullanıcı kararı); Paratika'nın
  ortam bağımlı, URL formatlı, entegrasyonu kırma riski taşıyan ayarları appsettings.json'da kaldı.
- **Kalem seçimi zorunlu:** `InitiateOnlinePaymentInput.ChargeLineItemId` non-nullable —
  auto-select yok. Kiracı ekranlarında (İç Faz 4/5) kalem radio seçimi ödeme yöntemi
  sekmelerinden bağımsız ve zaten zorunlu; `PaymentService.CreateAsync`'teki auto-select
  (`ResolveAutoSelectedLineItem`) burada gerekmiyor.
- **Sahiplik/kapsam kontrolü:** `ReportTenantPaymentAsync` ile aynı desen —
  `chargeService.GetTenantDetailsAsync(GetTenantChargeDetailsInput(...))` (TenantId + scope
  filtreli, bulunamazsa/başka kiracıya aitse 404).

---

## 2. Yeni veri modeli

- `SanalPosIslemleri` (`OnlinePaymentTransaction`, `BaseEntity`): `ChargeLineItemId`,
  `StoreAccountId`, `InitiatedByUserId`, `PaymentAllocationId?` (İç Faz 8'de doldurulacak),
  `ProviderCode`, `MerchantPaymentId` (filtreli unique index), `ProviderTransactionId?`,
  `Amount`, `Currency`, `Status` (`OnlinePaymentTransactionStatus`), ham
  `ResponseCode?`/`TransactionStatus?`/`ErrorCode?`/`SafeMessage?`, `SessionExpiresAt?`,
  `CallbackReceivedAt?`, `LastInquiryAt?`, `InquiryCount`, `CompletedAt?`, `[Timestamp] RowVersion`.
- `SanalPosIslemOlaylari` (`OnlinePaymentEvent`, `AuditLog` gibi `BaseEntity`'den türemez —
  append-only): `OnlinePaymentTransactionId`, `EventType`, sağlayıcı kodları, `SafeSummary`,
  `ProviderTimestamp?`, `CreatedAt`.
- Migration `20260902084119_AddOnlinePaymentTransactionBackbone` — yalnız `CreateTable`/
  `CreateIndex` (yeni tablolar, backfill gerekmedi), FK'ler Restrict, test DB'de uygulandı.

---

## 3. Yeni/Değişen kod

**Yeni:** `Models/Entities/OnlinePaymentTransaction.cs`, `Models/Entities/OnlinePaymentEvent.cs`,
`Models/DTOs/OnlinePayment/OnlinePaymentProviderDtos.cs`,
`Models/DTOs/OnlinePayment/OnlinePaymentInputs.cs`, `Models/Settings/ParatikaOptions.cs`,
`Services/Interfaces/IOnlinePaymentProvider.cs`, `Services/Interfaces/IOnlinePaymentService.cs`,
`Services/OnlinePaymentService.cs`, `Services/Interfaces/IOnlinePaymentBusinessRules.cs`,
`Services/OnlinePaymentBusinessRules.cs`,
`Repositories/Interfaces/IOnlinePaymentTransactionRepository.cs`,
`Repositories/OnlinePaymentTransactionRepository.cs`,
`Repositories/Interfaces/IOnlinePaymentEventRepository.cs`,
`Repositories/OnlinePaymentEventRepository.cs`, migration `AddOnlinePaymentTransactionBackbone`,
`KiraTakip.Tests/OnlinePaymentServiceTests.cs` (fake provider dahil).

**Değişecek:** `Models/Enums.cs` (`OnlinePaymentTransactionStatus`, `OnlinePaymentEventType`),
`Data/ApplicationDbContext.cs` (2 `DbSet` + config), `Infrastructure/DependencyInjection/
ServiceModule.cs` (`IOnlinePaymentService` kaydı), `Infrastructure/DependencyInjection/
RepositoryModule.cs` (2 repository kaydı), `Infrastructure/DependencyInjection/
InfrastructureModule.cs` (`ParatikaOptions` config), `appsettings.json` (`"Paratika"` bloğu,
placeholder `CHANGE_ME` değerleriyle).

**Dokunulmadı:** `PaymentService.cs`, `PaymentStoreResolver.cs`, `ChargeLineItemRepository.cs`,
hiçbir controller/view.

---

## 4. `OnlinePaymentService.InitiateAsync` akışı

1. `chargeService.GetTenantDetailsAsync` — sahiplik/kapsam + tahakkuk durum kontrolü.
2. `AcquirePaymentLockAsync(chargeLineItemId)`.
3. `GetPaymentBalanceAsync` → kalem/tahakkuk eşleşmesi + `EnsureAmountWithinAvailable`.
4. `HasActiveAttemptAsync` — aynı kalemde sonuçlanmamış (`Pending`) başka deneme varsa
   `ONLINE_PAYMENT_ACTIVE_ATTEMPT_EXISTS`.
5. `IPaymentStoreResolver.ResolveAsync` → mağaza hesabı + `ProviderCode`.
6. `IEnumerable<IOnlinePaymentProvider>`'dan seç; yoksa `ONLINE_PAYMENT_PROVIDER_NOT_FOUND`.
7. `StoreAccount` kaydından merchant kimlik bilgileri + `IStoreAccountCredentialProtector.
   Unprotect` ile şifre çözme.
8. Benzersiz `MerchantPaymentId` (`Guid.NewGuid().ToString("N")`) ile `provider.
   CreateSessionAsync(...)` çağrısı.
9. Sonuca göre `OnlinePaymentTransaction` (`Pending` veya sağlayıcı reddettiyse `Failed`) +
   `SessionRequested`/`SessionResult` iki `OnlinePaymentEvent` satırı.
10. `PaymentAllocationId` null kalır (İç Faz 8).

Hata durumunda (provider/mağaza/limit) hiçbir satır yazılmaz — `PaymentService.CreateAsync`'teki
"hata → kayıt yok" deseniyle tutarlı.

---

## 5. Testler

`KiraTakip.Tests/OnlinePaymentServiceTests.cs`:

- `OnlinePaymentServiceTests` (transaction/rollback deseni, `BankTransactionSchemaTests` gibi):
  başarılı başlatma → 1 transaction + 2 event; tutar kullanılabilir bakiyeyi aşarsa hiçbir satır
  yazılmaz; aynı kalemde aktif deneme varken ikinci istek reddedilir; bilinmeyen `ProviderCode`
  için `ONLINE_PAYMENT_PROVIDER_NOT_FOUND`; `MerchantPaymentId` DB'de unique index ile korunuyor
  (`DbUpdateException`).
- `OnlinePaymentServiceConcurrencyTests` (`PaymentConcurrentLineItemTests`'teki ayrı bağlantı/
  commit deseni — aynı kalemde eşzamanlı iki başlatma denemesinden yalnız biri başarılı olur).
- `OnlinePaymentBusinessRulesTests` (DB'siz, `PaymentBusinessRulesTests` deseni):
  `IsValidStatusTransition`/`EnsureValidStatusTransition` izinli/izinsiz geçiş kombinasyonları,
  `EnsureAmountWithinAvailable` doğrulaması.

**Kapsam dışı test:** Gerçek Paratika'ya HTTP isteği atan hiçbir test yok — henüz böyle bir
sınıf/çağrı yok, provider soyutu yalnız fake ile doğrulanıyor.

Sonuç: `dotnet test` → 352/352 geçti (333 mevcut + 19 yeni: 5 `OnlinePaymentServiceTests` +
1 `OnlinePaymentServiceConcurrencyTests` + 13 `OnlinePaymentBusinessRulesTests`).

---

## 6. Durma kapıları

- **DURMA KAPISI A** (migration): tamamlandı, test DB'de uygulandı (yalnız `CreateTable`/
  `CreateIndex`, backfill gerekmedi).
- **DURMA KAPISI B** (`dotnet build` + `dotnet test` tam yeşil): tamamlandı (352/352).
- **DURMA KAPISI C** (manuel duman testi): gerekmez — bu faz saf backend, UI değişikliği yok.
- **DURMA KAPISI D** (kullanıcı onayı): bekleniyor.
