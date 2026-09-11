# Faz 20 / İç Faz 2 — Ödeme Mağaza Yönlendirmeleri ve Resolver Implementation Plan

**Durum:** Uygulandı ve 272/272 regresyon testi geçti; kullanıcı kabulü bekliyor.  
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)  
**Ön koşul:** İç Faz 1 tamamlandı ve kullanıcı tarafından kabul edildi.  
**Kapsam:** Borç tipi + genel/taşınmaz/birim kapsamlı mağaza yönlendirmesi, yönetim ekranı,
genel varsayılan zorunluluğu ve deterministik mağaza hesabı resolver'ı.  
**Kapsam dışı:** Tahakkuk kalemi ödeme kolonları, manuel/banka ödemeleri, tenant ödeme UI,
sanal POS ve Paratika HTTP çağrıları.

**Uygulama sonucu (2026-08-27):** Veri modeli, migration, yönetim ekranı, permission'lar,
genel/taşınmaz/birim upsert akışı, tarihsel override kapatma, deterministik resolver ve ChargeType
aktivasyon kapısı tamamlandı. `20260827085852_AddPaymentStoreRoutings` migration'ı test veritabanına
uygulandı; production veritabanına uygulanmadı. Hedeflenen 14 test ve 272 testlik tam paket geçti.

**Kabul öncesi UX düzenlemeleri (2026-08-27):** Kullanıcı geri bildirimiyle ekran metinleri
gözden geçirildi:

- Sabit "yeni borç tipi pasif oluşturuldu" bilgi kutusu kaldırıldı; yerine gerçek duruma göre
  (`HasUsableDefault` / `IsChargeTypeActive`) değişen 3 adımlı kurulum rehberi paneli eklendi.
  Rehber, genel yönlendirme kaydedilince otomatik olarak "son adım: aktifleştir" paneline döner.
- "Override'ı Kapat" aksiyonu Türkçe "Yönlendirmeyi Kaldır" olarak değiştirildi; onay ve başarı
  mesajları fallback zincirini (Birim → Taşınmaz → Genel) ve kaydın silinmediğini açıkça anlatır.
  Ekranda "override" kelimesi kalmadı.
- Kapsam seçiminin altına dinamik bir açıklama metni eklendi (Genel/Taşınmaz/Birim ne anlama
  gelir).
- Borç tipinin aktif/pasif durumunu listede veya dropdown'da ayrıca **göstermeme** kararı
  bilinçli olarak alındı (kullanıcı: kafa karıştırıcı). `ChargeTypeRoutingOptionDto.IsActive`
  yalnız rehber panelinin hangi durumda gösterileceğini belirlemek için kullanılır, UI'da etiket
  olarak yazılmaz.
- Veri modeli/migration/resolver/iş kuralı değişmedi; yalnız `PaymentStoreRoutingService`,
  `AdminPaymentStoreRoutingController` ve `Index.cshtml` güncellendi.
- Yeni test: `GetManagementDataAsync_ShouldReturnChargeTypeOptionsWithActivityFlag`. Tam paket
  273/273 geçti.

**İkinci tur düzeltme (2026-08-27):** Kullanıcı geri bildirimiyle iki ek düzeltme yapıldı:

- **Mantık hatası düzeltildi:** `GetMissingDefaultsAsync` yalnız aktif borç tiplerini tarıyordu.
  Yeni borç tipleri her zaman pasif oluşturulup genel tanım olmadan aktifleşemediği için bu
  filtre, mevcut eski kayıtlar tamamlanınca kalıcı olarak boş kalacaktı — bundan sonra oluşan
  hiçbir borç tipi sarı kutuya düşmeyecekti. Filtre kaldırıldı; artık aktif/pasif ayrımı
  yapılmadan genel tanımı eksik tüm borç tipleri listelenir (ek sorgu/join eklenmedi, yalnız
  `Where` koşulundan `chargeType.IsActive &&` kaldırıldı). Sarı kutu başlığı "aktif borç tipinin"
  yerine "borç tipinin" olarak güncellendi.
- **Mavi/yeşil rehber paneli sadeleştirildi:** Numaralı adım listesi kaldırıldı; her iki durum
  için de (genel tanım eksik / genel tanım var ama pasif) tek, duruma göre doğru bilgi veren bir
  cümle kullanılıyor. Görsel tasarım ilk versiyondaki sade tek-mesaj yapısına yaklaştırıldı; fark
  olarak metin artık statik değil, gerçek duruma göre değişiyor.
- Yeni test genişletildi:
  `GetManagementDataAsync_ShouldReturnChargeTypeOptionsAndSurfaceInactiveMissingDefaults` — pasif
  bir borç tipinin `MissingDefaults` listesinde de göründüğünü doğrular. Tam paket 273/273 geçti
  (test sayısı artmadı, mevcut test genişletildi).

**Üçüncü tur düzeltme — geçmiş yönlendirmelerin varsayılan görünümden gizlenmesi (2026-08-27):**
Kullanıcı, `Charge/Index.cshtml` ekranındaki "N iptal edilmiş kayıt varsayılan görünümde
gizleniyor. Görüntüle →" deseninin ödeme yönlendirmeleri listesine de uygulanmasını istedi:

- `TableQuery.Status` alanı (bu ekranda daha önce kullanılmıyordu) yeniden kullanıldı:
  `status` boş/`tum` ise liste yalnız aktif yönlendirmeleri gösterir; `status=gecmis` yalnız
  pasifleştirilmiş (geçmiş) kayıtları gösterir. `GetPagedListAsync` içine tek bir `IsActive`
  filtresi eklendi.
- Yeni `IPaymentStoreRoutingRepository.GetHistoryCountAsync()` — toplam pasif kayıt sayısını
  döndürür (Charge ekranındaki `CancelledCount` ile aynı yaklaşım: arama/kapsam filtrelerinden
  bağımsız global sayım).
- `PaymentStoreRoutingIndexDataDto.HistoryCount` eklendi.
- `Index.cshtml`: varsayılan görünümde `HistoryCount > 0` ise
  "N geçmiş kayıt varsayılan görünümde gizleniyor. Görüntüle →" notu; `status=gecmis`
  görünümünde "Yalnızca geçmiş yönlendirmeler gösteriliyor. ← Güncel yönlendirmelere dön" notu
  gösterilir. Arama formuna `status` gizli alanı eklendi (arama/kapsam değiştirilirken geçmiş
  görünüm korunur); sayfalama zaten `TableQuery.ToQueryDict()` üzerinden `status`'u koruyordu.
- Yeni test: `GetManagementDataAsync_ShouldHideHistoryByDefaultAndExposeItViaStatusFilter`. Tam
  paket 274/274 geçti.

---

## 1. Değişmeyecek iş kararları

- Yönlendirme anahtarı `ChargeTypeId + kapsam` olur.
- Kapsam `General`, `Property` veya `Unit` değerlerinden tam olarak biridir.
- Öncelik sırası `Unit → Property → General` olur.
- Birim tanımında yalnız `UnitId`, taşınmaz tanımında yalnız `PropertyId` saklanır.
- Genel tanımda hem `PropertyId` hem `UnitId` null olur.
- Birim kaydında birimin taşınmazı ayrıca snapshot olarak yazılmaz; resolver güncel
  `Unit.PropertyId` ilişkisini kullanır.
- Her borç tipi için tek aktif genel tanım bulunabilir.
- Yeni borç tipi mağaza seçimi yapılmadan oluşturulur ve her zaman pasif başlar.
- Borç tipi, “Ödeme Yönlendirmeleri” ekranında kullanılabilir genel mağaza tanımlanmadan
  pasiften aktife alınamaz.
- Mevcut borç tiplerine migration sırasında mağaza tahmini atanmaz. Genel tanımı eksik
  borç tipleri aktif/pasif ayrımı yapılmadan yönetim ekranında gösterilir; aktif olanlarda ödeme
  resolver'ı tarafından bloklanır, pasif olanlarda aktivasyon engellenir.
- Genel varsayılan tanım silinmez/pasifleştirilmez; yalnız başka mağazaya güncellenebilir.
- Taşınmaz ve birim override'ları pasifleştirilebilir; sonrasında resolver bir alt kapsama düşer.
- Yönlendirme oluşturulurken mağazanın ve tek aktif hesabının kullanılabilir olması zorunludur.
- Yönlendirme sonradan mağaza/hesap pasifleştirilerek geçersiz hale gelirse resolver sessizce bir
  alt kapsama düşmez; en yüksek öncelikli tanım için sabit hata üretir.
- Borç tipi sonradan pasif olsa bile önceden oluşmuş borçların ödenebilmesi için mevcut
  yönlendirmeler korunur ve resolver borç tipinin aktif olmasını şart koşmaz.
- Resolver tenant DTO/ViewModel/view'ına mağaza, merchant veya hesap alanı eklemez.
- Yönlendirme değişikliği mevcut tahakkuk ve tahakkuk kalemlerini güncellemez.
- Bu iç faz ödeme başlatmaz; yalnız ileriki ödeme kanallarının çağıracağı çözümleme kontratını kurar.

---

## 2. Hedef veri modeli

### `PaymentStoreRouting` → `OdemeMagazaYonlendirmeleri`

Dosya: `KiraTakip/Models/Entities/PaymentStoreRouting.cs`

| Kod alanı | SQL kolonu | Tip | Kural |
|---|---|---|---|
| `Id` | `Id` | `int` | PK |
| `ChargeTypeId` | `BorcTipiId` | `int` | Zorunlu FK |
| `PropertyId` | `TasinmazId` | `int?` | Yalnız taşınmaz kapsamında dolu |
| `UnitId` | `BirimId` | `int?` | Yalnız birim kapsamında dolu |
| `StoreId` | `MagazaId` | `int` | Zorunlu FK |
| Base alanları | mevcut adlar | mevcut tipler | Audit, aktiflik, soft-delete |

Navigation'lar `ChargeType`, nullable `Property`, nullable `Unit` ve `Store` olur. Tüm FK'ler
`DeleteBehavior.Restrict` kullanır; yönlendirme geçmişi olan kayıtlar fiziksel silinemez.

### Scope constraint

Constraint: `CK_OdemeMagazaYonlendirmeleri_Kapsam`

```sql
([TasinmazId] IS NULL AND [BirimId] IS NULL)
OR ([TasinmazId] IS NOT NULL AND [BirimId] IS NULL)
OR ([TasinmazId] IS NULL AND [BirimId] IS NOT NULL)
```

### Filtreli unique indexler

- `UX_OdemeMagazaYonlendirmeleri_Genel_Aktif`
  - `BorcTipiId`
  - `[TasinmazId] IS NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0`
- `UX_OdemeMagazaYonlendirmeleri_Tasinmaz_Aktif`
  - `BorcTipiId + TasinmazId`
  - `[TasinmazId] IS NOT NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0`
- `UX_OdemeMagazaYonlendirmeleri_Birim_Aktif`
  - `BorcTipiId + BirimId`
  - `[TasinmazId] IS NULL AND [BirimId] IS NOT NULL AND [Aktif] = 1 AND [IsDeleted] = 0`

Ek indexler `MagazaId`, `TasinmazId` ve `BirimId` üzerinde oluşturulur.

### Scope enum

Dosya: `KiraTakip/Models/PaymentRoutingScope.cs`

```csharp
public enum PaymentRoutingScope
{
    General = 0,
    Property = 1,
    Unit = 2
}
```

Enum DB'de saklanmaz; nullable FK kombinasyonundan türetilir.

### DbContext ve migration

- `ApplicationDbContext` içine `DbSet<PaymentStoreRouting> PaymentStoreRoutings` eklenir.
- İlişki, constraint ve indexler `OnModelCreating` içinde açıkça tanımlanır.
- Migration adı `AddPaymentStoreRoutings` olur.
- Migration yalnız yeni tablo/index/constraint/FK'leri ekler.
- Mevcut borç tipleri için routing seed veya tahmini backfill yapmaz.

---

## 3. DTO ve ViewModel sözleşmeleri

Dosya: `KiraTakip/Models/DTOs/PaymentStoreRouting/PaymentStoreRoutingDtos.cs`

- `UpsertPaymentStoreRoutingInput`
  - `ChargeTypeId`, `Scope`, `PropertyId?`, `UnitId?`, `StoreId`
- `PaymentStoreRoutingListItemDto`
  - routing kimlikleri, borç tipi, scope, kapsam adı, mağaza adı/kodu
  - `IsStoreActive`, `HasActiveStoreAccount`, `ProviderCode`, `Currency`, `IsActive`
- `PaymentStoreRoutingLookupDto`
  - `Id`, `Name`, isteğe bağlı üst kapsam görünen adı
- `StoreRoutingOptionDto`
  - `Id`, `Name`, `ProviderCode`, `Currency`
- `MissingDefaultRoutingDto`
  - `ChargeTypeId`, `ChargeTypeName`, `ChargeTypeCode`, `IsChargeTypeActive`
- `PaymentRoutingResolutionCandidateDto`
  - repository'nin resolver'a verdiği iç aday/durum bilgileri
- `ResolvedPaymentStoreAccountDto`
  - `RoutingId`, `MatchedScope`, `ChargeTypeId`, `UnitId`, `PropertyId`
  - `StoreId`, `StoreAccountId`, `ProviderCode`, `Currency`

Resolver sonucu `StoreName`, `MerchantId`, `MerchantUser`, protected secret veya tenant'a
gösterilebilecek mağaza metni taşımaz. Provider kimlikleri İç Faz 6/7'de `StoreAccountId`
üzerinden yalnız server-side hazırlanır.

Dosya: `KiraTakip/Models/ViewModels/PaymentStoreRoutingViewModels.cs`

- `PaymentStoreRoutingFormViewModel`: upsert alanları ve dropdown seçenekleri.
- `PaymentStoreRoutingIndexViewModel`: `TableQuery`, paged routing listesi, eksik defaults ve form.

ChargeType form/DTO sözleşmelerine mağaza alanı eklenmez. Mağaza seçimi yalnız ödeme
yönlendirmesi ViewModel ve ekranında bulunur.

---

## 4. Validator sözleşmeleri

Yeni `PaymentStoreRoutingFormViewModelValidator`:

- `ChargeTypeId > 0`, `StoreId > 0` zorunlu.
- `General`: iki scope FK null.
- `Property`: yalnız `PropertyId > 0`.
- `Unit`: yalnız `UnitId > 0`.
- Enum dışı scope reddedilir.
- Hata alanları form select isimleriyle birebir eşleşir.

`ChargeTypeFormViewModelValidator` mağaza veya routing doğrulaması yapmaz; mevcut ad
validasyonu korunur. Aktivasyon için genel default kontrolü servis/business-rule katmanındadır.

Mağaza/hesap kullanılabilirliği, FK varlığı ve duplicate kontrolleri business-rules katmanındadır.

---

## 5. Repository sözleşmesi

Yeni dosyalar:

- `Repositories/Interfaces/IPaymentStoreRoutingRepository.cs`
- `Repositories/PaymentStoreRoutingRepository.cs`

Metotlar:

```csharp
Task<PagedResult<PaymentStoreRoutingListItemDto>> GetPagedListAsync(TableQuery query);
Task<List<MissingDefaultRoutingDto>> GetMissingDefaultsAsync();
Task<PaymentStoreRouting?> FindActiveAsync(
    int chargeTypeId, int? propertyId, int? unitId, bool tracking = true);
Task<PaymentStoreRouting?> GetTrackedByIdAsync(int id);
Task<int?> GetDefaultStoreIdAsync(int chargeTypeId);
Task<bool> HasUsableDefaultAsync(int chargeTypeId);
Task<PaymentRoutingResolutionCandidateDto?> GetResolutionCandidateAsync(
    int chargeTypeId, int unitId, CancellationToken cancellationToken = default);
Task<bool> HasActiveRoutingForStoreAsync(int storeId);
```

`GetResolutionCandidateAsync` tek repository/use-case sınırında birimi ve `PropertyId` değerini
bulur, Unit/Property/General adaylarını önceliklendirir ve yalnız en yüksek adayı mağaza/hesap
durumlarıyla döndürür. Geçersiz mağaza/hesap nedeniyle alt scope'a düşmez.

Mevcut repository değişiklikleri:

- `IStoreRepository.GetRoutingOptionsAsync()` yalnız aktif mağaza + tek aktif hesap seçeneklerini
  secret/merchant taşımadan döndürür.
- `IUnitRepository.HasHistoricalDependencyAsync` routing geçmişini de kontrol eder.
- `IPropertyRepository.CanChangeUnitStructureAsync` property/unit routing geçmişini de kontrol eder.

Bu son iki kontrol FK `Restrict` hatasının ham DB hatası olarak kullanıcıya çıkmasını önler.

---

## 6. Business-rules ve servisler

Yeni business-rules dosyaları:

- `Services/Interfaces/IPaymentStoreRoutingBusinessRules.cs`
- `Services/PaymentStoreRoutingBusinessRules.cs`

Kurallar:

- Borç tipi, taşınmaz, birim ve mağaza varlığını doğrular.
- Scope/FK kombinasyonunu tekrar doğrular.
- Seçilen mağazanın aktif ve tek aktif hesaplı olmasını zorunlu tutar.
- Aynı scope kaydını duplicate eklemek yerine upsert için bulur.
- Genel varsayılan routing'in pasifleştirilmesini reddeder.

Yeni yönetim servisi:

- `Services/Interfaces/IPaymentStoreRoutingService.cs`
- `Services/PaymentStoreRoutingService.cs`

```csharp
Task<PaymentStoreRoutingIndexDataDto> GetManagementDataAsync(TableQuery query);
Task UpsertAsync(UpsertPaymentStoreRoutingInput input);
Task DeactivateOverrideAsync(int id);
Task<int?> GetDefaultStoreIdAsync(int chargeTypeId);
Task<bool> HasUsableDefaultAsync(int chargeTypeId);
```

- Servis `ITransactionalService` uygular.
- Aynı aktif scope varsa `StoreId` güncellenir; yoksa yeni aktif tarihsel kayıt eklenir.
- Pasif override yeniden tanımlanırsa eski kayıt canlandırılmaz, yeni kayıt eklenir.
- SQL 2601/2627 yarışı `PAYMENT_ROUTING_DUPLICATE` conflict hatasına çevrilir.
- Deactivate yalnız Property/Unit override'ını kapatır; hard-delete yapmaz.

Yeni runtime resolver:

- `Services/Interfaces/IPaymentStoreResolver.cs`
- `Services/PaymentStoreResolver.cs`

```csharp
Task<ResolvedPaymentStoreAccountDto> ResolveAsync(
    int chargeTypeId,
    int unitId,
    CancellationToken cancellationToken = default);
```

Sabit hata kodları:

- `PAYMENT_ROUTING_UNIT_NOT_FOUND`
- `PAYMENT_ROUTING_NOT_FOUND`
- `PAYMENT_ROUTING_STORE_INACTIVE`
- `PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND`
- `PAYMENT_ROUTING_ACTIVE_ACCOUNT_CONFLICT`

Resolver controller, tenant context, ödeme tutarı veya tahakkuk entity'si bilmez.

---

## 7. Genel default zorunluluğunun ChargeType aktivasyonuna bağlanması

Değişecek alanlar:

- `ChargeTypeService` ve interface'i
- ChargeType `CreateInput`
- `AdminChargeTypeController`
- `Views/AdminChargeType/Create.cshtml`

Kurallar:

- ChargeType Create/Edit formuna mağaza dropdown'ı veya routing alanı eklenmez.
- Create ekranındaki `Aktif` seçimi kaldırılır; yeni borç tipi servis seviyesinde zorunlu olarak
  `IsActive=false` oluşturulur.
- `CreateInput` aktiflik alanı taşımaz ve `CreateAsync` oluşturulan borç tipi kimliğini döndürür.
- Create sonrasında kullanıcı yeni borç tipi önceden seçili olacak şekilde
  `/Admin/PaymentStoreRouting?chargeTypeId={id}` ekranına yönlendirilir.
- Genel mapping yalnız ödeme yönlendirmeleri ekranında tanımlanır.
- `UpdateAsync` veya `ToggleStatusAsync` pasiften aktife geçmeye çalıştığında kullanılabilir
  general default yoksa `CHARGE_TYPE_DEFAULT_STORE_REQUIRED` conflict hatası üretilir.
- Aktiften pasife geçiş routing'i silmez.
- ChargeType ile routing aynı form/transaction içinde oluşturulmaz; iki domain yönetimi ayrıdır.
- Sistem borç tipinin mevcut davranış kilidi korunur.
- Mevcut aktif/defaultsuz borç tipleri uyarı listesinde görünür; migration pasife almaz.

---

## 8. Permission, controller ve UI

`PermissionCatalog.PaymentRouting`:

- Module: `Internal.PaymentRouting`
- Actions: `Internal.PaymentRouting.Create`, `Internal.PaymentRouting.Edit`
- “Parametreler” grubuna, `OperasyonMuduruIzinleri` ve `All` listelerine eklenir.
- Global tanım olduğu için `ScopeAware` ve tenant listelerine eklenmez.

Yeni `AdminPaymentStoreRoutingController`, base route `/Admin/PaymentStoreRouting`:

| Route | Policy | Davranış |
|---|---|---|
| `GET /Admin/PaymentStoreRouting` | Module | Liste, eksik defaults ve form |
| `POST /Admin/PaymentStoreRouting/Save` | Create | Yeni scope veya upsert |
| `POST /Admin/PaymentStoreRouting/Deactivate/{id}` | Edit | Property/Unit override kapatma |

Tüm POST action'lar antiforgery doğrular ve business validation aynı ekranda gösterilir.

Yeni `Views/AdminPaymentStoreRouting/Index.cshtml`:

- Aktif borç tiplerinde eksik general default sayısını ve kayıtları gösterir.
- Borç tipi, scope ve mağaza seçimi içerir.
- Yeni oluşturulan borç tipi query-string ile gelirse formda seçili ve “önce genel
  yönlendirmeyi tanımlayın” açıklamasıyla açılır.
- Property scope'ta property; Unit scope'ta property adıyla gruplanmış unit seçimi görünür.
- Unit seçildiğinde ayrıca `PropertyId` post edilmez.
- Liste scope, mağaza, aktif hesap/provider durumu ve fallback etkisini gösterir.
- General kayıt için pasifleştirme aksiyonu render edilmez.
- Override kapatma metni bir alt scope'a fallback yapılacağını açıklar.

Sidebar Parametreler grubuna “Ödeme Yönlendirmeleri” linki eklenir.

---

## 9. Resolver algoritması

```text
Girdi: ChargeTypeId + UnitId
1. Unit yoksa PAYMENT_ROUTING_UNIT_NOT_FOUND.
2. ChargeTypeId + UnitId aktif routing ara.
3. Yoksa ChargeTypeId + Unit.PropertyId aktif routing ara.
4. Yoksa ChargeTypeId general routing ara.
5. Yoksa PAYMENT_ROUTING_NOT_FOUND.
6. Seçilen en yüksek routing'in Store'u pasifse
   PAYMENT_ROUTING_STORE_INACTIVE; fallback yapma.
7. Aktif hesap sayısı 0 ise PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND.
8. Aktif hesap sayısı >1 ise PAYMENT_ROUTING_ACTIVE_ACCOUNT_CONFLICT.
9. Tek hesap varsa RoutingId, scope, StoreId ve StoreAccountId döndür.
```

---

## 10. Test planı

### `PaymentStoreRoutingManagementTests.cs`

- Scope constraint iki FK dolu kaydı reddeder.
- Üç filtreli unique index duplicate aktif kayıtları reddeder.
- General/Property/Unit upsert doğru kolonları yazar.
- General pasifleştirilemez; override tarihsel kayıt korunarak kapatılır.
- İnaktif mağaza veya aktif hesabı olmayan mağaza tanımda reddedilir.
- Existing active/defaultsuz borç tipi uyarıda görünür; upsert sonrası çıkar.
- Unit/property yapı değişikliği routing geçmişi varken bloklanır.

### `PaymentStoreResolverTests.cs`

Kullanıcı örnekleri doğrudan test edilir:

- Taşınmaz A / Ofis 101 / Portal → Unit override Mağaza A.
- Taşınmaz A / Ofis 102 / Portal → Unit override Mağaza B.
- Taşınmaz B / Portal → Property override Mağaza C.
- Başka taşınmaz / Portal → General Mağaza A.
- Taşınmaz B / Ofis 101 / Portal → Unit override Mağaza B; Property C'ye düşmez.

Ek testler:

- Unit yoksa Property, o da yoksa General çözülür.
- Eksik unit/routing sabit hata verir.
- En yüksek mapping'in mağazası pasif veya hesabı yoksa fallback yapılmaz.
- Routing değişince yeni resolve yeni mağazayı döndürür; tahakkuk tabloları değişmez.
- Pasif borç tipi mevcut routing ile çözülür.

### Mimari ve ChargeType testleri

- Permission iç sistem listelerinde; tenant/ScopeAware dışında kalır.
- Controller policy ve antiforgery attribute'ları doğrulanır.
- Resolver/routing DI kayıtları doğrulanır.
- Tenant contract/view'larına mağaza alanı eklenmez.
- Resolver response DTO'su merchant/secret taşımaz.
- Yeni borç tipi zorunlu pasif oluşturulur ve routing ekranına yönlendirilir.
- ChargeType form/DTO'larında mağaza alanı bulunmaz.
- General yönlendirme yalnız routing ekranından oluşturulur/güncellenir.
- Update veya toggle ile pasiften aktife defaultsuz geçilmez; pasife alma default'u korur.

Doğrulama:

```powershell
dotnet ef migrations add AddPaymentStoreRoutings --project KiraTakip --startup-project KiraTakip
dotnet build KiraTakip.csproj
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj --filter "PaymentStoreRouting|PaymentStoreResolver|ChargeTypeDefaultRouting"
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj
```

Uygulama açıksa süreç kapatılmaz; workspace içinde izole output path kullanılır. Production DB
update bu iç fazda çalıştırılmaz.

---

## 11. Dosya değişiklik listesi

Yeni dosyalar:

- `Models/Entities/PaymentStoreRouting.cs`
- `Models/PaymentRoutingScope.cs`
- `Models/DTOs/PaymentStoreRouting/PaymentStoreRoutingDtos.cs`
- `Models/ViewModels/PaymentStoreRoutingViewModels.cs`
- `Repositories/Interfaces/IPaymentStoreRoutingRepository.cs`
- `Repositories/PaymentStoreRoutingRepository.cs`
- `Services/Interfaces/IPaymentStoreRoutingBusinessRules.cs`
- `Services/PaymentStoreRoutingBusinessRules.cs`
- `Services/Interfaces/IPaymentStoreRoutingService.cs`
- `Services/PaymentStoreRoutingService.cs`
- `Services/Interfaces/IPaymentStoreResolver.cs`
- `Services/PaymentStoreResolver.cs`
- `Validators/PaymentStoreRoutingFormViewModelValidator.cs`
- `Controllers/AdminPaymentStoreRoutingController.cs`
- `Views/AdminPaymentStoreRouting/Index.cshtml`
- `Migrations/<timestamp>_AddPaymentStoreRoutings.cs`
- `Migrations/<timestamp>_AddPaymentStoreRoutings.Designer.cs`
- Dört routing/resolver/ChargeType test dosyası

Değişecek dosyalar:

- `Data/ApplicationDbContext.cs` ve model snapshot
- Store, Unit ve Property repository interface/implementasyonları
- Repository/Service DI modülleri
- `Authorization/PermissionCatalog.cs` ve `_Sidebar.cshtml`
- Bölüm 7'deki sınırlı ChargeType dosyaları
- Faz 20 ve PROGRESS dokümanları

Dokunulmayacak alanlar:

- `Charge`, `ChargeLineItem`, `PaymentAllocation` entity ve ödeme servisleri.
- Banka, manuel ödeme, dekont ve tenant akışları.
- Payment portal/token ve provider/Paratika kodları.
- StoreAccount credential formatı.
- Production veritabanı.

---

## 12. Tamamlanma ve durma kapıları

Tamamlanma koşulları:

- Migration SQL Server test DB'ye uygulanır.
- General/Property/Unit constraint ve unique testleri geçer.
- Kullanıcının beş precedence örneği doğru hesabı çözer.
- Geçersiz en yüksek mapping sessiz fallback yapmaz.
- Yeni borç tipi pasif oluşturulur ve defaultsuz aktive edilemez.
- Existing aktif/defaultsuz borç tipleri görünür ve resolver'da bloklanır.
- Tenant tarafına mağaza/merchant verisi sızmaz.
- Tam test paketi geçer; phase ve PROGRESS güncellenir.
- Kullanıcı onayı olmadan İç Faz 3'e geçilmez.

Şu durumlarda varsayım yapılmadan durulur:

- Yeni borç tipinin pasif başlamaması veya mağaza seçiminin tekrar ChargeType formuna alınması kararı.
- Pasif/hesapsız spesifik mapping'de fallback istenmesi.
- Birim mapping'inde `PropertyId` snapshot gereksinimi.
- İç Faz 2'de ödeme/tahakkuk tablosu değişikliği zorunluluğu.
- Routing permission'ının row scope ile sınırlandırılması gereksinimi.
