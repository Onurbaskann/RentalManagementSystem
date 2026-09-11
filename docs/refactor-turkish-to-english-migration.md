# Ara Refactor — Turkish to English Codebase Migration

> **Yapay Zeka (Agent) İçin Kritik Not (Low-Context Optimization):**
> Bu belge, düşük kapasiteli ve kısıtlı bağlam (context) penceresine sahip AI modelleri için özel olarak hazırlanmıştır. 
> 
> **🛑 Kesin Kurallar:**
> 1. **Sadece istenen Faza odaklan:** Kullanıcı "Faz 2.1'i yap" dediğinde SADECE o adımda belirtilen klasörleri/dosyaları oku ve değiştir. Başka hiçbir şeye dokunma.
> 2. **Veritabanı Türkçe Kalacak:** SQL schema, tablolar ve kolonlar asla değişmeyecek. Sınıfları İngilizceye çevirirken mutlak suretle `[Table("TurkceIsim")]` ve `[Column("TurkceIsim")]` kullan. (Örn: `using System.ComponentModel.DataAnnotations.Schema;`)
> 3. **İş Mantığı (Business Logic) Korunacak:** Refactoring sırasında hiçbir algoritma, if/else veya LINQ filtresi davranışsal olarak değiştirilemez.
> 4. **Derle ve Doğrula:** Her fazın sonunda `dotnet build` çalıştırarak kırık referansları bul.

---

## 📖 Çeviri Sözlüğü (Single Source of Truth)
Bu sözlük dışına çıkma, sinonim (eş anlamlı) uydurma.

| C# Kod (İngilizce Hedef) | Veritabanı / Mevcut (Türkçe) |
| --- | --- |
| `Property` | `Tasinmaz` |
| `Unit` | `Birim` |
| `Tenant` | `Kiraci` |
| `Lease` | `Sozlesme` |
| `Charge` | `Tahakkuk` |
| `ChargeLineItem` | `TahakkukKalemi` |
| `PaymentAllocation` | `TahakkukOdeme` |
| `Payment` | `Odeme` |
| `BankTransaction` | `BankaHareketi` |
| `PaymentMatch` | `OdemeBankaEslesme` |
| `PaymentLink` | `OdemeLinkKayit` |
| `RateSchedule` | `GenelTarife` |
| `PropertyRateOverride` | `TasinmazTarife` |
| `UnitRateOverride` | `BirimTarife` |
| `LeaseRateOverride` | `SozlesmeTarife` |
| `ReservationRateOverride`| `RezervasyonTarife` |
| `ChargeType` | `BorcTipi` |
| `Document` | `Belge` |
| `DocumentType` | `BelgeTuru` |
| `DocumentContent` | `BelgeIcerik` |
| `Invitation` | `Davetiye` |
| `Reservation` | `Rezervasyon` |
| `TenantCategory` | `KiraciKategori` |
| `Role` | `Rol` |
| `PasswordResetRequest` | `SifreSifirlamaTalebi` |
| `AccessScope` | `YetkiKapsami` |
| `UserAccessScope` | `KullaniciYetkiKapsami` |
| `LookupValue` | `EnumDegeri` |
| `ManualCharge` | `ManuelBorc` |
| `LeaseActivityLog` | `SozlesmeIslemGecmisi` |
| `DashboardService` | `IstatistikService` |
| `User` | `Kullanici` |
| `ChargeTypeBehavior` | `BorcTipiDavranisi` |
| `ChargeStatus` | `TahakkukDurumu` |
| `ChargeSourceType` | `TahakkukKaynakTipi` |
| `LeaseStatus` | `SozlesmeDurumu` |
| `ReservationStatus` | `RezervasyonDurumu` |
| `PaymentStatus` | `OdemeDurumu` |
| `ScopeType` | `KapsamTipi` |
| `CalculationMethod` | `HesaplamaYontemi` |
| `RoleService` | `RolService` |

**Not:** `RoleNames` sınıfı zaten İngilizce isim taşıdığı için değişmez. Identity'nin `IdentityRole` sınıfıyla karışmaması için namespace ile ayrıştırılmış durumda.

---

## 🏗️ Execution Plan (Yürütme Aşamaları)
*Aşağıdaki adımlar yukarıdan aşağıya (Bottom-Up) uygulanmalıdır.*

### Faz 1: Enums & Constants
- **Dosyalar:** `Models/Enums.cs`, `Models/KapsamTipi.cs`, `Authorization/PermissionCatalog.cs`
- **İşlem:** Enum adlarını çevir. Değer adlarını çevir ama **tam sayı (int) karşılıklarını aynı bırak**. (Örn: `Aktif = 1` -> `Active = 1`). `PermissionCatalog` içindeki hem C# property/const adları **hem de string literal değerleri** İngilizceye çevrilir.

#### Faz 1 — Permission String Değerleri de İngilizceye Çevrilir

`PermissionCatalog` içindeki string literal değerleri şu prefix eşleşmesiyle güncellenir:

| Eski | Yeni |
|---|---|
| `Internal.` | `Internal.` (aynı, zaten İngilizce) |
| `System.` | `System.` (aynı) |
| `Kiraci.` | `Tenant.` |
| `Kiraci.System.` | `Tenant.System.` |

Örnek:
```csharp
// Eski
public static class Tasinmaz
{
    public const string Module = "Internal.Tasinmaz";
    public const string Create = "Internal.Tasinmaz.Create";
}
public static class KiraciPortal
{
    public static class Sozlesme
    {
        public const string Module = "Kiraci.Sozlesme";
    }
}

// Yeni
public static class Property
{
    public const string Module = "Internal.Property";
    public const string Create = "Internal.Property.Create";
}
public static class TenantPortal
{
    public static class Lease
    {
        public const string Module = "Tenant.Lease";
    }
}
```

`AllModules` listesindeki `Path` değerleri ve `ScopeAware` listesi de buna uygun güncellenir.

**DB tarafında etkisi vardır; çözümü Faz 5.1'de anlatılıyor. Faz 1 sadece kod değişikliği yapar, DB'ye şu aşamada dokunmaz.**

### Faz 2: Domain Modelleri (Entities) & EF Eşlemesi
- **Hedef Klasör:** `Models/Entities/`
- **İşlem:** 
  1. Dosya adlarını ve `class` adlarını İngilizce yap.
  2. Her entity'nin başına `[Table("TurkceTabloAdi")]` ekle.
  3. Her property'nin başına `[Column("TurkceKolonAdi")]` ekle. Navigasyon property'lerini İngilizce yap.
  4. **DbSet Güncellemesi:** `ApplicationDbContext` içindeki `DbSet<...>` nesnelerini güncelle.
- **Alt Aşamalar:**
  - Faz 2.1: Basit nesneler (DocumentType, ChargeType, LookupValue)
  - Faz 2.2: Core nesneler (Property, Unit, Tenant, Lease)
  - Faz 2.3: İşlem nesneleri (Charge, Payment, BankTransaction, Reservation)

#### Faz 2.4: Model Snapshot Senkronizasyonu
- **Hedef Dosya:** `Migrations/ApplicationDbContextModelSnapshot.cs`
- **Sorun:** Faz 2.1-2.3'te entity CLR type adları değişti (Örn: `KiraTakip.Models.Entities.Tasinmaz` → `KiraTakip.Models.Entities.Property`). EF Core model snapshot dosyasında CLR type tam adı tutulur; değişince mevcut migration referansları ile tutarsızlık oluşur.
- **İşlem Adımları:**
  1. `dotnet build KiraTakip.csproj` — derleme temiz olmalı, sıfır CS hatası.
  2. `dotnet ef migrations add RenameEntitiesToEnglish --project KiraTakip --startup-project KiraTakip` komutunu çalıştır.
  3. Üretilen migration dosyasının `Up()` ve `Down()` metotlarını aç. **Bu metotlar boş olmalıdır.** ([Table] ve [Column] attribute'ları DB adlarını sabit tuttuğu için yalnızca snapshot güncellenir.)
  4. Eğer migration içinde `DropTable`, `CreateTable`, `RenameColumn` gibi ifadeler görürsen: bir entity veya property üzerinde `[Table("...")]` ya da `[Column("...")]` attribute'u eksik demektir. Migration'ı **sil** (`dotnet ef migrations remove --project KiraTakip --startup-project KiraTakip`), Faz 2'ye dön, eksik attribute'ları tamamla, sonra bu fazı tekrar dene.
  5. Migration temizse: `dotnet ef database update --project KiraTakip --startup-project KiraTakip` çalıştır. DB'de fiziksel değişiklik olmayacaktır (sadece `__EFMigrationsHistory` tablosuna kayıt düşer).
- **Doğrulama:** Uygulamayı `dotnet run --project KiraTakip` ile başlat, ana sayfayı aç, bir kayıt listele. Runtime hata yoksa faz tamamdır.

### Faz 3: Data Transfer Objects (DTOs & ViewModels)
- **Hedef Klasörler:** `Models/DTOs/`, `Models/ViewModels/`
- **İşlem:** Dosya adlarını, sınıf adlarını ve içerdiği property adlarını İngilizceye çevir. Entity referanslarını Faz 2'deki yeni İngilizce adlarıyla değiştir.

### Faz 4: Data Access (Repositories)
- **Hedef Klasörler:** `Repositories/Interfaces/`, `Repositories/`
- **İşlem:** Interface adlarını (Örn: `ITasinmazRepository` -> `IPropertyRepository`) ve Concrete sınıflarını çevir. `GetByTasinmazId` gibi metot adlarını `GetByPropertyId` yap.

### Faz 5: Business Logic (Services)
- **Hedef Klasörler:** `Services/Interfaces/`, `Services/`
- **İşlem:** Servis adlarını, interface adlarını, metot adlarını ve değişkenleri tamamen İngilizceye geçir. `IdentitySeedService`, `RolService`, `KiraciKullaniciService` içindeki hard-coded permission preset listeleri (SistemYoneticisi, OperasyonMuduru, KiraciYoneticisi, KiraciSorumlusu izin setleri) **yeni string değerleriyle** güncellenir. Bu değerler Faz 1'de belirlenen prefix eşleşmesine uygun olmalıdır.

#### Faz 5.1: Permission Data Reset (Yalnızca Development)

Faz 1'de `PermissionCatalog` string değerleri İngilizceye çevrildiği için (`Kiraci.` → `Tenant.`, `Internal.Tasinmaz` → `Internal.Property` vb.) DB'deki eski format kayıtları geçersiz kaldı. Bu adım yeni format'a geçiş içindir.

- **Hedef:** DB'deki eski format permission kayıtlarını temizle ki uygulama başlangıcında seed servisi yeni değerleri yazsın.
- **İşlem (SQL Server üzerinde çalıştır):**
  ```sql
  DELETE FROM UserPermission;
  DELETE FROM RolePermission;
  DELETE FROM AspNetUserClaims WHERE ClaimType = 'Permission';
  DELETE FROM AspNetRoleClaims WHERE ClaimType = 'Permission';
  ```
- **Adım Sonrası:** Uygulamayı `dotnet run --project KiraTakip` ile başlat. Seed servislerinin "delete + re-insert" mantığı otomatik devreye girer ve yeni format string'leri yazar. Süper admin ve preset rol izinleri güncel formatta gelir.
- **Doğrulama:** Bir role login olup korunmuş bir sayfaya (Örn: `/Property`, `/Tenant` sayfaları) erişilerek yeni claim'lerin çalıştığı test edilir. Yetkisiz kullanıcının 403 aldığı doğrulanır.

---

**⚠️ Production Ortamı Uyarısı (Faz 5.1 için Kritik):**

Yukarıdaki `DELETE` stratejisi **YALNIZCA geliştirme/dummy veri ortamı içindir.** Bu refactor production'a aktarılırken:

- Gerçek kullanıcı, rol ve yetki atamaları mevcut olacağından `DELETE` **kesinlikle kullanılmaz**; tüm yetki atamaları kaybolur ve kullanıcılar sisteme giremez hale gelir.
- Bunun yerine bir **UPDATE migration script'i** hazırlanır. Eski string'ler yeni string'lere yerinde çevrilir:
  ```sql
  -- Prefix migration
  UPDATE UserPermission
     SET PermissionPath = REPLACE(PermissionPath, 'Kiraci.', 'Tenant.')
   WHERE PermissionPath LIKE 'Kiraci.%';

  UPDATE AspNetUserClaims
     SET ClaimValue = REPLACE(ClaimValue, 'Kiraci.', 'Tenant.')
   WHERE ClaimType = 'Permission' AND ClaimValue LIKE 'Kiraci.%';

  -- Entity bazlı migration (her entity için ayrı UPDATE)
  UPDATE UserPermission
     SET PermissionPath = REPLACE(PermissionPath, 'Internal.Tasinmaz', 'Internal.Property')
   WHERE PermissionPath LIKE 'Internal.Tasinmaz%';
  -- ... diğer entity'ler için tekrarla
  ```
- Script çalıştırılmadan **önce** DB backup zorunludur.
- Script çalıştırıldıktan **sonra** kullanıcı login olabilmeli, korunmuş sayfalara erişim regresyon testi yapılmalıdır.
- Production deploy sırası: (1) DB backup → (2) uygulama offline → (3) UPDATE script → (4) yeni build deploy → (5) smoke test → (6) uygulama online.
- Production migration script'i `docs/migration-scripts/permission-english-migration.sql` dosyasında versiyonlu olarak saklanır (bu dosya refactor kapsamında hazırlanır).

---

### Faz 6: Sunum Katmanı (Controllers)
- **Hedef Klasör:** `Controllers/`
- **İşlem:** Controller sınıf adlarını (`PropertyController`), Action metot adlarını ve Dependency Injection (constructor) değişken adlarını güncelle. Route adları (`[Route(...)]`) bozulmamalı, arayüz bağlantıları kopmamalıdır (bu opsiyoneldir, eğer kırılırsa View'lar güncellenirken toplanacaktır).

### Faz 7: Arayüz (Views & JavaScript)
- **Hedef Klasörler:** `Views/`, `wwwroot/js/`
- **İşlem:** 
  1. Klasör adlarını Controller isimleriyle senkronize et (Örn: `Views/Tasinmaz/` -> `Views/Property/`).
  2. `.cshtml` içindeki `@model` tanımlarını yeni isimlerle güncelle.
  3. Tag helper'ları (`asp-controller="Property"`) güncelle.
  4. Alpine.js ve diğer javascript hooklarındaki JSON key'lerini yeni DTO/ViewModel property adlarıyla eşitle.
