# Faz 20 / İç Faz 3 — Tahakkuk Kalemi Bazlı Ödeme Çekirdeği Implementation Plan

**Durum:** Uygulandı; 306/306 regresyon testi geçti; kullanıcı kabulü bekliyor.
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)
**Ön koşul:** İç Faz 1 ve İç Faz 2 tamamlandı ve kullanıcı tarafından kabul edildi (2026-08-27).
**Kapsam:** `ChargeLineItem.PaidAmount`, `PaymentAllocation.ChargeLineItemId`/`StoreAccountId`,
kalem bazlı kalan/kullanılabilir tutar hesabı, `IPaymentStoreResolver`'ın ilk gerçek tüketicisi,
eşzamanlılık kilidi, migration + backfill.
**Kapsam dışı:** Manuel ödeme/banka eşleştirme UI değişiklikleri (İç Faz 4), authenticated kiracı
ödeme deneyimi (İç Faz 5), provider soyutu ve sanal POS (İç Faz 6/7/8), production migration.

---

## 1. Değişmeyecek iş kararları

- Mağaza, tahakkuk kaleminin borç tipi ve kapsamı üzerinden `IPaymentStoreResolver` ile belirlenir.
- Ödeme oluşturulduğu anda çözülen `StoreAccountId` o kayda **sabitlenir** (snapshot); sonradan
  yönlendirme değişse bile geçmiş ödeme kaydı değişmez.
- `PaymentAllocation.ChargeId` **korunur** — kaldırılmaz (kiracı izolasyon query filter'ı
  `o.Charge.TenantId` üzerinden çalışıyor).
- Kalem verilen tahakkuka ait mi kontrolü **yalnız servis seviyesinde** yapılır
  (`PaymentBusinessRules.EnsureLineItemBelongsToCharge`); DB'de bileşik FK kurulmadı — bu
  projede `HasAlternateKey`/bileşik FK deseni hiç kullanılmıyor, tek kolonlu FK + servis
  kontrolü tercih edildi.
- Kiracı aynı anda yalnız bir tahakkuk kalemini öder; birden fazla kalem tek işlemde
  seçilemez/bölünemez.
- Kısmi ödeme kalem seviyesinde devam eder; onaylı **ve** onay bekleyen tutarlar kullanılabilir
  bakiyeden düşülür (hem admin hem kiracı akışında).

**Bilinen, kalıcı olmayan kısıt:** Çok kalemli bir tahakkukta (örn. kira + aidat) hangi kalemin
ödeneceği bu iç fazda UI'dan seçilemiyor; `ChargeLineItemId` verilmezse ve birden fazla ödenebilir
kalem varsa `PAYMENT_LINE_ITEM_SELECTION_REQUIRED` hatası döner. Kalem seçim ekranı İç Faz 4
kapsamındadır.

---

## 2. Veri modeli

### `ChargeLineItem` (`TahakkukKalemleri`)

Eklendi: `PaidAmount` (`OdenenTutar`, `decimal(18,2)`, NOT NULL, default 0), navigation
`Allocations`. Yeni check: `CK_TahakkukKalemleri_OdenenLimit`
(`[OdenenTutar] >= 0 AND [OdenenTutar] <= [ToplamTutar]`). Yeni index:
`IX_TahakkukKalemleri_TahakkukId`.

### `PaymentAllocation` (`TahakkukOdemeleri`)

Eklendi: `ChargeLineItemId` (`TahakkukKalemiId`, zorunlu FK → `ChargeLineItem`, Restrict),
`StoreAccountId` (`MagazaHesapBilgisiId`, zorunlu FK → `StoreAccount`, Restrict). Yeni index'ler:
`IX_TahakkukOdemeleri_TahakkukKalemiId_Durum` (filtreli, `IsDeleted=0`),
`IX_TahakkukOdemeleri_MagazaHesapBilgisiId`.

### `ChargeLineItem` için query filter (eksiklik giderme)

`ChargeLineItem`'da daha önce hiç kiracı query filter'ı yoktu; bu iç fazda eklendi:
`k => !k.IsDeleted && (!_currentUser.IsKiraciUser || k.Charge.TenantId == _currentUser.TenantId)`.

---

## 3. Migration — `AddLineItemBasedPaymentCore`

Tek migration, elle 6 adıma sıralanmış `Up()`:

1. Yeni kolonlar (`OdenenTutar` NOT NULL default 0; `TahakkukKalemiId`/`MagazaHesapBilgisiId`
   geçici olarak nullable).
2. Backfill B1 — tek kalemli tahakkukların ödemelerini o tek kaleme bağlar.
3. Backfill B2 — mağaza hesabını resolver önceliğinin (Birim → Taşınmaz → Genel) SQL karşılığıyla
   çözer.
4. Backfill B3 — kalem `OdenenTutar`'ını o kaleme ait onaylı (Durum=2) ödemelerin toplamından
   hesaplar.
5. Guard — `TahakkukKalemiId`/`MagazaHesapBilgisiId` hâlâ NULL olan kayıt varsa veya
   `OdenenTutar > ToplamTutar` olan kalem varsa `THROW 51000` ile migration'ı durdurur.
6. Kolonları NOT NULL'a alır, index/check constraint/FK'leri ekler.

Çok kalemli tahakkukların ödemeleri **tahmin edilerek dağıtılmaz**; preflight script
(`docs/migration-scripts/phase-20-inner-phase-3-preflight.sql`, salt-okunur, R1/R2/R3) bunları
raporlar; elle çözülmeden migration guard'ı geçemez.

**Test veritabanı deneyimi:** Migration ilk denemede guard'a takıldı — `KiraTakipDb_Test`
içinde Faz 20 öncesinden kalma 56 dummy ödeme/324 kalem/109 tahakkuk kaydı, mağaza yönlendirmesi
hiç tanımlanmadan oluşturulmuştu ve B2 bunlara mağaza hesabı çözemedi. Proje kuralı gereği
(gerçek üretim verisi yok) kullanıcı onayıyla bu dummy kayıtlar temizlendi
(`OdemeBankaEslesmeleri` → `TahakkukOdemeleri` → `TahakkukKalemleri` → `Tahakkuklar` sırasıyla),
migration tekrar denenip başarıyla uygulandı. Uygulama bir sonraki çalıştırmasında
`SeedDataService` ile örnek veri yeniden üretilecek (bkz. Bölüm 6).

---

## 4. Servis/repository katmanı

- `IChargeLineItemRepository` (mevcut dosya genişletildi, yeni repository açılmadı):
  `GetPaymentBalanceAsync`, `GetPaymentBalancesByChargeAsync`, `GetForPaymentUpdateAsync`,
  `GetChargePaidAmountTotalAsync`, `AcquirePaymentLockAsync`.
- `IPaymentAllocationRepository`: `GetChargeLineItemIdAsync` eklendi; `GetForDecisionAsync`
  artık `ChargeLineItem`'ı da include ediyor.
- Yeni `ChargeLineItemPaymentBalanceDto` (`Models/DTOs/Payment/PaymentLineItemDtos.cs`) —
  `ApprovedAmount`/`PendingAmount` her zaman canlı `TahakkukOdemeleri` toplamından hesaplanır;
  `ChargeLineItem.PaidAmount` denormalize alanı bakiye kararlarında hiç kullanılmaz.
- `PaymentService.CreateAsync`/`ReportTenantPaymentAsync`/`ApproveAsync`/`RejectAsync` kalem
  bazlı akışa taşındı; `IPaymentStoreResolver.ResolveAsync` artık `CreateAsync`/
  `ReportTenantPaymentAsync` içinde çağrılıyor — resolver'ın **ilk gerçek tüketicisi**.
- `ChargeService.UpdatePaidAmountAsync(ChargeId, ChargeLineItemId)` — önce kalemin
  `PaidAmount`'ını, sonra tahakkukun `PaidAmount`/`Status`'unu günceller.
- Yeni `IPaymentBusinessRules`/`PaymentBusinessRules` (`IBusinessRules` işaretçisi üzerinden
  `BusinessRulesModule` tarafından otomatik DI kaydı): kalem-tahakkuk eşleşmesi, ödenebilirlik,
  admin/kiracı tutar sınırı, onay sınırı, çok-kalem-var otomatik seçim kuralı.

---

## 5. Sabit hata kodları

| Kod | Anlamı |
|---|---|
| `PAYMENT_LINE_ITEM_NOT_FOUND` | Kalem bulunamadı |
| `PAYMENT_LINE_ITEM_CHARGE_MISMATCH` | Seçilen kalem verilen tahakkuka ait değil |
| `PAYMENT_LINE_ITEM_FULLY_PAID` | Kalemin kalan borcu yok |
| `PAYMENT_LINE_ITEM_NO_AVAILABLE_AMOUNT` | Onay bekleyen ödemeler kalanı kapatıyor |
| `PAYMENT_LINE_ITEM_SELECTION_REQUIRED` | Birden fazla ödenebilir kalem var, seçim gerekli |
| `PAYMENT_AMOUNT_NOT_POSITIVE` | Tutar 0'dan büyük değil |
| `PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE` | Tutar kalemin kullanılabilir tutarını aşıyor |
| `PAYMENT_APPROVAL_EXCEEDS_LINE_ITEM_REMAINING` | Onay, kalemin kalan borcunu aşıyor |
| `CHARGE_LINE_ITEM_NOT_FOUND` | `ChargeService.UpdatePaidAmountAsync` içinde kalem bulunamadı |

Kaldırılan eski kodlar: `PAYMENT_AMOUNT_EXCEEDS_REMAINING`, `PAYMENT_APPROVAL_EXCEEDS_REMAINING`
(tahakkuk bazlı hesaptan kalem bazlıya geçişle birlikte). `PAYMENT_ROUTING_*` kodları resolver'dan
doğrudan kabarcıklanır, sarmalanmaz. `TENANT_PAYMENT_AMOUNT_EXCEEDS_AVAILABLE` ve diğer mevcut
`PAYMENT_CHARGE_*`/`PAYMENT_NOT_FOUND`/`PAYMENT_NOT_PENDING` kodları korunmuştur.

---

## 6. Eşzamanlılık

`ReservationRepository.AcquireUnitDecisionLockAsync`'in birebir kopyası:
`ChargeLineItemRepository.AcquirePaymentLockAsync` — SQL Server `sp_getapplock`, kaynak adı
`KiraTakip.Payment.ChargeLineItem.{id}`, `LockMode=Exclusive, LockOwner=Transaction,
LockTimeout=10000`. `PaymentService.CreateAsync`/`ApproveAsync`/`RejectAsync` kilidi alıp
bakiyeyi kilit ALTINDA taze okur; ikinci istekçi ya bekler ya da güncel toplamla reddedilir.
`RowVersion` eklenmedi — kilit + kilit-altı yeniden okuma yeterli.

---

## 7. Geriye uyumlu köprü (İç Faz 4'e kadar)

`CreatePaymentViewModel`/`TenantChargePaymentFormViewModel`'e opsiyonel `ChargeLineItemId`
eklendi; mevcut View'lar bu alanı post etmediği için `null` gelir ve
`PaymentService.ResolveTargetLineItemIdAsync` devreye girer: tek ödenebilir kalem varsa otomatik
seçilir, birden fazla varsa `PAYMENT_LINE_ITEM_SELECTION_REQUIRED` döner.
`PaymentController.Create` (GET) tek kalemli tahakkuklarda formu doğru `ChargeLineItemId`/`Amount`
ile önceden doldurur. **View dosyaları bu iç fazda değiştirilmedi.**

`SeedDataService.SeedOdemeMagazalariAsync()` eklendi (idempotent): 1 seed mağaza/hesap + her
borç tipi için 1 genel yönlendirme oluşturur; borç tipi seed'inden hemen sonra çağrılır.
Mevcut ödeme seed metotları (`SeedGecmisYilOdemeleriAsync`, `SeedKismiOdemelerAsync`) artık
tahakkukun tüm kalemlerine `ToplamTutar` oranında dağıtılan, her kalem için ayrı
`PaymentAllocation` üretecek şekilde değiştirildi.

---

## 8. Test sonucu

Tam paket: **306/306** (İç Faz 2 sonu 274 idi; +5 mevcut testte yeni senaryo,
+27 tamamen yeni test).

- `PaymentArchitectureTests.cs` — kalem bazlı akışa taşındı, 5 yeni test eklendi (admin pending
  düşümü, çok kalem seçim zorunluluğu, yabancı kalem reddi, eksik yönlendirme engeli, mağaza
  snapshot'ı).
- Yeni `PaymentBusinessRulesTests.cs` (11 test, DB'siz).
- Yeni `ChargeLineItemPaymentSchemaTests.cs` (6 test — NOT NULL/check constraint/FK Restrict/
  kiracı izolasyonu).
- Yeni `ChargeLineItemPaymentBalanceTests.cs` (4 test — bakiye ayrımı, sıralama, ödenebilir
  filtre, kalem/tahakkuk toplam invariant'ı).
- Yeni `PaymentConcurrentLineItemTests.cs` (2 test — eşzamanlı onay ve eşzamanlı oluşturma
  yarışları; 3 tekrar çalıştırmada kararlı).
- Yeni `ChargeLineItemPaymentMigrationTests.cs` (3 test — preflight R1/R2/R3 SQL mantığı).
- Derleme uyumu için `ReservationArchitectureTests.cs`, `TenantPanelArchitectureTests.cs`,
  `ReportArchitectureTests.cs` içindeki seed'lere yeni zorunlu alanlar eklendi; yeni paylaşılan
  `PaymentLineItemTestHelper.cs` yardımcı sınıfı bu tekrarı azaltır.

Doğrulama komutları:
```powershell
dotnet build KiraTakip.csproj
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj --no-restore
```

---

## 9. Dosya değişiklik listesi

**Yeni:** `Models/DTOs/Payment/PaymentLineItemDtos.cs`,
`Services/Interfaces/IPaymentBusinessRules.cs`, `Services/PaymentBusinessRules.cs`,
`Migrations/20260828062646_AddLineItemBasedPaymentCore.cs(+.Designer.cs)`,
`docs/migration-scripts/phase-20-inner-phase-3-preflight.sql`,
`KiraTakip.Tests/PaymentLineItemTestHelper.cs`, `KiraTakip.Tests/PaymentBusinessRulesTests.cs`,
`KiraTakip.Tests/ChargeLineItemPaymentSchemaTests.cs`,
`KiraTakip.Tests/ChargeLineItemPaymentBalanceTests.cs`,
`KiraTakip.Tests/PaymentConcurrentLineItemTests.cs`,
`KiraTakip.Tests/ChargeLineItemPaymentMigrationTests.cs`.

**Değişen (özet):** `Models/Entities/ChargeLineItem.cs`, `Models/Entities/PaymentAllocation.cs`,
`Data/ApplicationDbContext.cs` (+snapshot), `Repositories/ChargeLineItemRepository.cs`
(+interface), `Repositories/PaymentAllocationRepository.cs` (+interface),
`Repositories/ChargeRepository.cs`, `Services/PaymentService.cs` (+interface),
`Services/ChargeService.cs`, `Services/SeedDataService.cs`,
`Models/DTOs/Payment/PaymentQueryDtos.cs`, `Models/DTOs/Charge/ChargeInputs.cs`,
`Models/Dtos/ChargeLineItemDto.cs`, `Models/Dtos/PaymentAllocationDto.cs`,
`Models/Dtos/PaymentDetailDto.cs`, `Models/Dtos/PaymentListItemDto.cs`,
`Models/ViewModels/CreatePaymentViewModel.cs`, `Models/ViewModels/TenantChargeViewModels.cs`,
`Controllers/PaymentController.cs`, `Controllers/TenantChargeController.cs`,
`Infrastructure/DependencyInjection/SeedingExtensions.cs`,
`KiraTakip.Tests/PaymentArchitectureTests.cs`, `KiraTakip.Tests/ReservationArchitectureTests.cs`,
`KiraTakip.Tests/TenantPanelArchitectureTests.cs`, `KiraTakip.Tests/ReportArchitectureTests.cs`.

**Dokunulmayan:** `Views/Payment/Create.cshtml`, `Views/Charge/Details.cshtml`,
`Views/TenantCharge/Details.cshtml`, `Services/BankTransactionService.cs`,
`Models/Entities/PaymentMatch.cs`, `Models/Entities/BankTransaction.cs`,
`Services/PaymentLinkService.cs`, `Services/PaymentPortalService.cs`,
`Services/PaymentStoreResolver.cs`, `Repositories/PaymentStoreRoutingRepository.cs`.

---

## 10. Tamamlanma kapıları — sonuç

- [x] Migration test DB'de preflight+backfill+guard sırasıyla uygulandı (guard bir kez gerçekten
  tetiklendi ve doğru çalıştığı doğrulandı).
- [x] `ChargeLineItemPaymentSchemaTests` (constraint/FK/query-filter) yeşil.
- [x] Her yeni `PaymentAllocation`'da `ChargeLineItemId`/`StoreAccountId` NOT NULL dolduruluyor.
- [x] `PaymentConcurrentLineItemTests` iki testi yeşil (3 tekrar çalıştırmada kararlı).
- [x] `ChargeAndLineItemPaidAmounts_ShouldStayConsistentAfterApproveAndReject` yeşil.
- [x] Tam test paketi 306/306 geçti.
- [ ] Kullanıcı İç Faz 3 sonucunu, çok-kalem geçici kısıtını görerek onaylar.

**Production'a yapılmadı:** Migration yalnız `KiraTakipDb_Test`'e uygulandı; production
veritabanına (`KiraTakipDb`) dokunulmadı.
