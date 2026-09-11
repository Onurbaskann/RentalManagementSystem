# Faz 20 / İç Faz 1 — Mağaza ve Mağaza Hesabı Yönetimi Implementation Plan

**Durum:** Tamamlandı, doğrulandı ve kullanıcı tarafından kabul edildi.  
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)  
**Kapsam:** Yalnız mağaza ve mağazaya ait sürümlü ödeme hesabı yönetimi.  
**Kapsam dışı:** Borç tipi/kapsam yönlendirmesi, tahakkuk kalemi ödeme değişiklikleri,
kiracı ekranları, sanal POS çağrısı ve Paratika HTTP entegrasyonu.

---

## 1. Değişmeyecek uygulama kararları

- Kod kimlikleri İngilizce, tablo/kolon adları mevcut proje yaklaşımına uygun Türkçe olur.
- `Store` işletmesel alıcı kimliğidir; `StoreAccount` sağlayıcı hesabının tarihsel sürümüdür.
- Mağaza kodu kullanıcıdan alınmaz; mağaza adından mevcut `CodeSlugger.ToCode` ile üretilir.
- Silinmemiş mağazalarda kod benzersizdir. Pasif mağazanın kodu başka mağazada yeniden kullanılamaz.
- Bir mağazada aynı anda en fazla bir aktif ve silinmemiş hesap bulunur.
- Mevcut hesap düzenlenmez. Hesap/secret değişikliği eski hesabı kapatır ve yeni hesap sürümü oluşturur.
- İlk hesap ve yeni sürüm başlangıç zamanı sunucunun `TimeProvider` UTC zamanıdır; kullanıcı tarih girmez.
- Hesap sürümü kapatıldığında `IsActive=false` ve `ValidUntil=now` birlikte yazılır.
- Bir mağaza hesap tanımlanmadan oluşturulabilir. Yönlendirme çözümleyicisi İç Faz 2'de aktif
  hesap bulunmayan mağazayı ödeme için geçersiz sayacaktır.
- Mağaza pasif yapılınca hesap tarihçesi değiştirilmez. Sonraki resolver hem mağazanın hem hesabın
  aktif olmasını zorunlu tutacaktır.
- Fiziksel silme endpoint'i veya servis metodu eklenmez. İleride eklenecek yönlendirme/ödeme FK'leri
  `DeleteBehavior.Restrict` kullanacaktır.
- İlk UI seçenekleri `Paratika` ve `TRY` ile sınırlıdır; değerler magic string değil merkezi
  sabitlerden gelir.

---

## 2. Veri modeli ve SQL sözleşmesi

### `Store` → `Magazalar`

Dosya: `KiraTakip/Models/Entities/Store.cs`

| Kod alanı | SQL kolonu | Tip/sınır | Kural |
|---|---|---|---|
| `Id` | `Id` | `int` | PK |
| `Code` | `Kod` | `nvarchar(100)` | Zorunlu |
| `Name` | `Ad` | `nvarchar(200)` | Zorunlu |
| `Description` | `Aciklama` | `nvarchar(500)`, nullable | İsteğe bağlı |
| Base alanları | mevcut adlar | mevcut tipler | Audit, aktiflik, soft-delete |

- Navigation: `ICollection<StoreAccount> Accounts`.
- Filtreli unique index:
  `UX_Magazalar_Kod_Silinmemis`, kolon `Kod`, filtre `[IsDeleted] = 0`.

### `StoreAccount` → `MagazaHesapBilgileri`

Dosya: `KiraTakip/Models/Entities/StoreAccount.cs`

| Kod alanı | SQL kolonu | Tip/sınır | Kural |
|---|---|---|---|
| `Id` | `Id` | `int` | PK |
| `StoreId` | `MagazaId` | `int` | Zorunlu FK |
| `ProviderCode` | `SaglayiciKodu` | `nvarchar(50)` | Zorunlu |
| `Currency` | `ParaBirimi` | `char(3)` | Zorunlu |
| `MerchantId` | `MerchantId` | `nvarchar(200)` | Zorunlu |
| `MerchantUser` | `MerchantUser` | `nvarchar(200)` | Zorunlu |
| `ProtectedMerchantPassword` | `SifreliMerchantPassword` | `nvarchar(max)` | Zorunlu, yalnız ciphertext |
| `ValidFrom` | `GecerlilikBaslangici` | `datetime2` | Zorunlu UTC |
| `ValidUntil` | `GecerlilikBitisi` | `datetime2`, nullable | Açık sürümde null |
| Base alanları | mevcut adlar | mevcut tipler | Audit, aktiflik, soft-delete |

- FK: `StoreAccount.StoreId → Store.Id`, `DeleteBehavior.Restrict`.
- Filtreli unique index:
  `UX_MagazaHesapBilgileri_Magaza_Aktif`, kolon `MagazaId`, filtre
  `[Aktif] = 1 AND [IsDeleted] = 0`.
- Normal index: `IX_MagazaHesapBilgileri_MagazaId_GecerlilikBaslangici` üzerinde
  `MagazaId + GecerlilikBaslangici`.
- Check constraint: `GecerlilikBitisi IS NULL OR GecerlilikBitisi >= GecerlilikBaslangici`.
- Hesap entity'sinde plaintext parola/secret alanı bulunmaz.

### Merkezi sabitler

Dosya: `KiraTakip/Models/Constants/PaymentProviderCodes.cs`

- `PaymentProviderCodes.Paratika = "Paratika"`
- `CurrencyCodes.Try = "TRY"`
- UI seçenek listeleri aynı dosyada salt-okunur koleksiyonlar olarak tutulur.

### DbContext ve migration

- `ApplicationDbContext` içine `DbSet<Store> Stores` ve `DbSet<StoreAccount> StoreAccounts` eklenir.
- Yukarıdaki uzunluk, ilişki, index ve constraint'ler `OnModelCreating` içinde açıkça tanımlanır.
- Migration adı: `AddStoresAndStoreAccounts`.
- Migration yalnız `Magazalar` ve `MagazaHesapBilgileri` tablolarını, FK/index/constraint'leri
  ekler; mevcut ödeme veya tahakkuk tablolarına dokunmaz.
- Model snapshot EF tarafından güncellenir; migration dosyası elle kurgulanmaz.

---

## 3. Secret koruma ve anahtar yönetimi

### Kontrat

Yeni dosyalar:

- `KiraTakip/Services/Interfaces/IStoreAccountCredentialProtector.cs`
- `KiraTakip/Services/StoreAccountCredentialProtector.cs`
- `KiraTakip/Models/Settings/DataProtectionSettings.cs`

`IStoreAccountCredentialProtector` yalnız iki metot taşır:

```csharp
string Protect(string plaintext);
string Unprotect(string protectedValue);
```

- Implementasyon ASP.NET Core `IDataProtectionProvider` kullanır.
- Purpose sabiti sürümlüdür:
  `KiraTakip.Payments.StoreAccountCredentials.v1`.
- `Unprotect` yalnız ileride provider adapter'ına hesap hazırlayan servis tarafından kullanılmak
  üzere kontratta bulunur; İç Faz 1 controller/DTO/view kodu bu metodu çağırmaz.
- Plaintext yalnız command DTO → service → `Protect` çağrısı boyunca bellekte bulunur;
  loglanmaz, entity'ye veya response modeline atanmaz.

### Key ring

- `InfrastructureModule` içinde `AddDataProtection().SetApplicationName("KiraTakip")` eklenir.
- `DataProtection:KeyRingPath` verilmişse key ring bu dizinde kalıcılaştırılır; verilmemişse
  framework varsayılan deposu kullanılır.
- `appsettings.Example.json` içine örnek `DataProtection.KeyRingPath` eklenir.
- Production kabul notu: Dizin uygulama hesabına özel ACL ile korunmalı ve deploymentlar arasında
  kalıcı olmalıdır. Key ring kaybedilirse kayıtlı merchant secret'ları çözülemez.
- Anahtarlar veya plaintext secret hiçbir log mesajında yer almaz.

---

## 4. DTO, ViewModel ve validator sözleşmeleri

### Command/response DTO'ları

Dosya: `KiraTakip/Models/DTOs/Store/StoreDtos.cs`

- `CreateStoreInput(Name, Description, IsActive)`
- `UpdateStoreInput(Name, Description, IsActive)`
- `CreateStoreAccountVersionInput(StoreId, ProviderCode, Currency, MerchantId, MerchantUser, MerchantPassword)`
- `StoreListItemDto`: `Id`, `Code`, `Name`, `Description`, `IsActive`, `HasActiveAccount`,
  `ActiveProviderCode`, `ActiveCurrency`
- `StoreDetailDto`: mağaza alanları + `IReadOnlyList<StoreAccountHistoryItemDto>`
- `StoreAccountHistoryItemDto`: `Id`, `ProviderCode`, `Currency`, `MerchantId`, `MerchantUser`,
  `ValidFrom`, `ValidUntil`, `IsActive`

Hiçbir response DTO'su `MerchantPassword`, `ProtectedMerchantPassword`, `Secret`, `Credential`
veya çözülebilir provider session alanı taşımaz.

### ViewModel'ler

Dosya: `KiraTakip/Models/ViewModels/StoreViewModels.cs`

- `StoreFormViewModel`: `Id`, `Name`, `Description`, `IsActive`
- `StoreEditViewModel`: `StoreFormViewModel Store`, hesap tarihçesi ve boş
  `StoreAccountFormViewModel NewAccount`
- `StoreAccountFormViewModel`: `StoreId`, `ProviderCode`, `Currency`, `MerchantId`,
  `MerchantUser`, `MerchantPassword`

Edit GET hiçbir zaman parola alanını doldurmaz. Hata ile view yeniden gösterildiğinde controller
parola alanını temizler; kullanıcı yeniden girmek zorundadır.

### Validator'lar

Yeni dosyalar:

- `KiraTakip/Validators/StoreFormViewModelValidator.cs`
- `KiraTakip/Validators/StoreAccountFormViewModelValidator.cs`

Kurallar:

- Mağaza adı trim sonrası zorunlu, en fazla 200; açıklama en fazla 500.
- Provider yalnız `PaymentProviderCodes.Supported` içinde olabilir.
- Para birimi yalnız `CurrencyCodes.Supported` içinde olabilir.
- Merchant ID ve Merchant User trim sonrası zorunlu, en fazla 200.
- Merchant Password boş olamaz ve en fazla 1000 karakter olabilir.
- Validasyon plaintext değeri hata mesajına eklemez.

---

## 5. Repository, business-rules ve servis

### Repository

Yeni dosyalar:

- `KiraTakip/Repositories/Interfaces/IStoreRepository.cs`
- `KiraTakip/Repositories/StoreRepository.cs`
- `KiraTakip/Repositories/Interfaces/IStoreAccountRepository.cs`
- `KiraTakip/Repositories/StoreAccountRepository.cs`

`IStoreRepository`:

- `GetPagedListAsync(TableQuery query)`
- `GetDetailAsync(int id)`
- `CodeExistsAsync(string code, int? excludeId = null)`

`IStoreAccountRepository`:

- `GetActiveByStoreIdAsync(int storeId, bool tracking = true)`
- `GetHistoryByStoreIdAsync(int storeId)`

Repository list/detail projeksiyonları şifreli secret kolonunu seçmez.

### Business rules

Yeni dosyalar:

- `KiraTakip/Services/Interfaces/IStoreBusinessRules.cs`
- `KiraTakip/Services/StoreBusinessRules.cs`

Interface `IBusinessRules` türetir ve otomatik DI taramasına girer. Kurallar:

- Mağaza adı/kod benzersizliğini servis için doğrular.
- Mağaza ve hesap varlık/yumuşak silinme kontrollerini tek yerde yapar.
- Provider ve currency sabit listelerini server-side tekrar doğrular.
- Yeni hesap eklenirken mağazanın silinmemiş olmasını zorunlu tutar; mağazanın pasif olması hesap
  sürümü oluşturmaya engel değildir ancak ödeme çözümünde kullanılamaz.
- Aktif hesap kapatma isteğinde hesabın o mağazaya ait ve aktif olduğunu doğrular.

### Servis

Yeni dosyalar:

- `KiraTakip/Services/Interfaces/IStoreService.cs`
- `KiraTakip/Services/StoreService.cs`

`IStoreService` metotları:

```csharp
Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery query);
Task<StoreDetailDto?> GetDetailAsync(int id);
Task<int> CreateAsync(CreateStoreInput input);
Task UpdateAsync(int id, UpdateStoreInput input);
Task<bool> ToggleStatusAsync(int id);
Task ReplaceAccountAsync(CreateStoreAccountVersionInput input);
Task DeactivateAccountAsync(int storeId, int accountId);
```

- `StoreService`, `ITransactionalService` uygular.
- Create/update işleminde ad trimlenir ve kod `CodeSlugger` ile yeniden üretilir.
- `ReplaceAccountAsync` tek transaction içinde şu sırayı uygular:
  1. Mağaza ve input doğrulanır.
  2. Tek `now = TimeProvider.GetUtcNow().UtcDateTime` alınır.
  3. Mevcut aktif hesap varsa `IsActive=false`, `ValidUntil=now` yapılır ve kaydedilir.
  4. Plaintext parola protector ile şifrelenir.
  5. Yeni hesap `ValidFrom=now`, `ValidUntil=null`, `IsActive=true` oluşturulur ve kaydedilir.
- İki eşzamanlı aktif hesap yazma yarışı DB unique index ile reddedilir ve kullanıcıya secret
  içermeyen sabit bir conflict mesajı döner.
- `DeactivateAccountAsync` aktif sürümü kapatır; tarihsel kaydı soft-delete/hard-delete yapmaz.
- Controller hiçbir entity'yi doğrudan değiştirmez.

DI kayıtları:

- `RepositoryModule`: `IStoreRepository`, `IStoreAccountRepository`.
- `ServiceModule`: `IStoreService`, `IStoreAccountCredentialProtector`.
- `IStoreBusinessRules` mevcut assembly taramasıyla kaydolur; manuel ikinci kayıt eklenmez.

---

## 6. Yetki, controller ve yönetim ekranları

### Permission

`PermissionCatalog.Store`:

- Module: `Internal.Store`
- Actions: `Internal.Store.Create`, `Internal.Store.Edit`, `Internal.Store.Account`
- Görünen adlar: `Ekle`, `Düzenle`, `Hesap Yönet`
- `AllModules`, `OperasyonMuduruIzinleri` ve `All` listelerine eklenir.
- Store permission'ları row-level taşınmaz kapsamına bağlı olmadığından `ScopeAware` listesine eklenmez.
- Tenant permission listelerine eklenmez.

### Controller

Dosya: `KiraTakip/Controllers/AdminStoreController.cs`

Base route: `/Admin/Store`, tüm controller `[Authorize]`.

| Method/route | Policy | Davranış |
|---|---|---|
| `GET /Admin/Store` | `Store.Module` | Sayfalı liste |
| `GET /Admin/Store/Create` | `Store.Module` | Boş form |
| `POST /Admin/Store/Create` | `Store.Create` | Mağaza oluştur, edit'e dön |
| `GET /Admin/Store/Edit/{id}` | `Store.Module` | Mağaza + hesap tarihçesi |
| `POST /Admin/Store/Edit/{id}` | `Store.Edit` | Mağaza bilgilerini güncelle |
| `POST /Admin/Store/ToggleStatus/{id}` | `Store.Edit` | Aktif/pasif değiştir |
| `POST /Admin/Store/ReplaceAccount/{id}` | `Store.Account` | İlk/yeni hesap sürümü oluştur |
| `POST /Admin/Store/DeactivateAccount/{id}/{accountId}` | `Store.Account` | Aktif hesabı kapat |

- Bütün POST action'lar antiforgery doğrular.
- Route `id`, form `StoreId` ile uyuşmazsa `BadRequest`.
- BusinessValidationException alan hatası uygun forma eklenir.
- Hesap formu hatasında mağaza detail/tarihçe yeniden servisten yüklenir; parola temizlenir.
- Hashids mevcut model binder/tag-helper akışıyla korunur.

### Views

Yeni dosyalar:

- `KiraTakip/Views/AdminStore/Index.cshtml`
- `KiraTakip/Views/AdminStore/Create.cshtml`
- `KiraTakip/Views/AdminStore/Edit.cshtml`

Ekran davranışı:

- Index ad/kod araması, aktiflik ve aktif hesap var/yok bilgisini gösterir.
- Create yalnız mağaza bilgilerini toplar.
- Edit üstte mağaza formunu, altta aktif hesap özetini, hesap değiştirme formunu ve salt-okunur
  tarihçeyi gösterir.
- Hesap formunda parola tipi `password`, autocomplete `new-password`; mevcut değer/asıl
  ciphertext hiçbir HTML attribute'unda yer almaz.
- “Yeni hesap sürümü oluştur” işlemi mevcut aktif hesabı kapatacağını açıkça belirtir.
- Tenant layout/sidebar/view dosyalarına mağaza bağlantısı veya veri eklenmez.
- `_Sidebar.cshtml` Parametreler erişim hesabına `Internal.Store` eklenir ve “Mağazalar” linki
  `/Admin/Store` yoluna bağlanır.

---

## 7. Test planı

### `KiraTakip.Tests/StoreManagementTests.cs`

SQL Server fixture ve her testte rollback transaction kullanılır:

- Mağaza oluşturma adı normalize eder ve benzersiz kod üretir.
- Aynı silinmemiş kod ikinci kez oluşturulamaz.
- DB filtreli index aynı mağazada iki aktif hesabı reddeder.
- İlk hesap oluşturma plaintext değeri DB'ye yazmaz; protector ile geri çözülen değer input ile aynıdır.
- Hesap değiştirme eski hesabı aynı UTC anda kapatır ve yalnız yeni hesabı aktif bırakır.
- Hesap kapatma tarihsel kaydı silmez.
- Liste/detail DTO projeksiyonlarında korumalı secret bulunmaz.
- Pasif mağaza tarihçesini korur.
- Geçerlilik check constraint'i ters tarih aralığını reddeder.

### `KiraTakip.Tests/StoreArchitectureTests.cs`

- Permission module/action'ları `AllModules`, `All`, operasyon yöneticisi listelerinde bulunur;
  tenant ve `ScopeAware` listelerinde bulunmaz.
- `AdminStoreController` action policy ve antiforgery attribute'ları kontrol edilir.
- Response DTO'larında secret/credential isimli property bulunmadığı reflection ile doğrulanır.
- `StoreAccount` entity'sinde plaintext secret property bulunmadığı doğrulanır.
- Store/account repository ve servis DI kayıtları çözülebilir.
- Tenant view/model kaynaklarında `Store`, `Merchant` veya `Magaza` alanı eklenmediği kontrol edilir.

### `KiraTakip.Tests/StoreValidationTests.cs`

- Boş/uzun mağaza alanları.
- Desteklenmeyen provider/currency.
- Boş/uzun merchant alanları ve parola.
- Validasyon mesajının parola değerini içermemesi.

Doğrulama komutları:

```powershell
dotnet ef migrations add AddStoresAndStoreAccounts --project KiraTakip --startup-project KiraTakip
dotnet build KiraTakip.csproj
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj --filter "Store"
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj
```

Test fixture migrationı `KiraTakipDb_Test` üzerinde uygular. Production database update bu iç fazda
çalıştırılmaz.

---

## 8. Dosya değişiklik listesi

Yeni dosyalar:

- `Models/Entities/Store.cs`
- `Models/Entities/StoreAccount.cs`
- `Models/Constants/PaymentProviderCodes.cs`
- `Models/DTOs/Store/StoreDtos.cs`
- `Models/ViewModels/StoreViewModels.cs`
- `Repositories/Interfaces/IStoreRepository.cs`
- `Repositories/StoreRepository.cs`
- `Repositories/Interfaces/IStoreAccountRepository.cs`
- `Repositories/StoreAccountRepository.cs`
- `Services/Interfaces/IStoreService.cs`
- `Services/StoreService.cs`
- `Services/Interfaces/IStoreBusinessRules.cs`
- `Services/StoreBusinessRules.cs`
- `Services/Interfaces/IStoreAccountCredentialProtector.cs`
- `Services/StoreAccountCredentialProtector.cs`
- `Models/Settings/DataProtectionSettings.cs`
- `Validators/StoreFormViewModelValidator.cs`
- `Validators/StoreAccountFormViewModelValidator.cs`
- `Controllers/AdminStoreController.cs`
- `Views/AdminStore/Index.cshtml`
- `Views/AdminStore/Create.cshtml`
- `Views/AdminStore/Edit.cshtml`
- `Migrations/<timestamp>_AddStoresAndStoreAccounts.cs`
- `Migrations/<timestamp>_AddStoresAndStoreAccounts.Designer.cs`
- `KiraTakip.Tests/StoreManagementTests.cs`
- `KiraTakip.Tests/StoreArchitectureTests.cs`
- `KiraTakip.Tests/StoreValidationTests.cs`

Değişecek dosyalar:

- `Data/ApplicationDbContext.cs`
- `Migrations/ApplicationDbContextModelSnapshot.cs`
- `Authorization/PermissionCatalog.cs`
- `Infrastructure/DependencyInjection/InfrastructureModule.cs`
- `Infrastructure/DependencyInjection/RepositoryModule.cs`
- `Infrastructure/DependencyInjection/ServiceModule.cs`
- `Views/Shared/_Sidebar.cshtml`
- `appsettings.Example.json`
- `docs/phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`
- `docs/PROGRESS.md`

Dokunulmayacak alanlar:

- Tahakkuk, tahakkuk kalemi ve mevcut ödeme entity/servisleri.
- Tenant controller, DTO, ViewModel, view ve layout'ları.
- `PaymentPortal`, `PaymentLinkRecord` ve token altyapısı.
- Banka hareketi ve eşleştirme altyapısı.
- Paratika HTTP/callback/query kodu ve `ParatikaOptions`.
- Production veritabanı.

---

## 9. Tamamlanma ve durma kapıları

İç Faz 1 tamamlanmış sayılması için:

- Migration temiz SQL Server test veritabanına uygulanır.
- Tam test paketi geçer veya ilgisiz mevcut hata açıkça raporlanır.
- Mağaza CRUD yerine create/update/aktiflik yönetimi ve hesap sürümleme ekranı çalışır.
- Aynı mağazada ikinci aktif hesap hem servis hem DB seviyesinde engellenir.
- Plaintext/parola hiçbir entity response, DTO, view source'u, log veya DB kolonunda bulunmaz.
- Tenant tarafında mağaza bilgisi görünmez.
- Phase ana checklist'i ve `PROGRESS.md` gerçek sonuçla güncellenir.
- Kullanıcı sonucu onaylamadan İç Faz 2'ye geçilmez.

Uygulama sırasında aşağıdakilerden biri ortaya çıkarsa varsayım yapılmadan durulur:

- Production key-ring dizini veya anahtar şifreleme yöntemi için mevcut deployment kuralıyla çelişki.
- Merchant credential modelinin Paratika dışındaki zorunlu alanlara ihtiyaç duyduğunun kesinleşmesi.
- Mağazanın hesap olmadan oluşturulamayacağına dair yeni iş kararı.
- İç Faz 1 kapsamı dışında tahakkuk/ödeme tablosu değişikliği zorunluluğu.

---

## 10. Uygulama sonucu

- `Store` ve `StoreAccount` katmanları, yönetim ekranları, permission ve DI kayıtları eklendi.
- Merchant parola ASP.NET Core Data Protection ile korunuyor; key-ring yolu yapılandırılabilir.
- Şifreli credential alanı `[AuditIgnore]` ile audit ayrıntılarından çıkarıldı.
- Hesap değişikliği eski sürümü kapatıp yeni sürüm oluşturuyor; hard-delete akışı bulunmuyor.
- Migration: `20260827071753_AddStoresAndStoreAccounts`.
- Migration `KiraTakipDb_Test` SQL Server veritabanına test fixture tarafından başarıyla uygulandı.
- Mağaza odaklı testler: `14/14` başarılı.
- İlk uygulama sonrası tam test paketi: `257/257` başarılı.
- Kullanıcı kabul düzeltmesinde hesap tarihçesi Türkiye saatine bağlandı ve aktif hesap özeti
  yapılandırılmış bilgi kartı olarak yenilendi.
- Kabul düzeltmeleri sonrası tam test paketi: `258/258` başarılı.
- Ana proje build'i başarılı; yeni değişikliklerden kaynaklanan derleme hatası yok.
- Mevcut `MailKit 4.9.0` NU1902 uyarısı ve önceden var olan nullable/Razor uyarıları bu iç
  fazın kapsamı dışında bırakıldı.
- Production veritabanına migration uygulanmadı.
- Kullanıcı kabulü: mağaza yönetimi, hesap tarihçesi, tenant gizliliği ve kabul düzeltmeleri
  doğrulandı; İç Faz 2'ye geçiş onayı verildi.
