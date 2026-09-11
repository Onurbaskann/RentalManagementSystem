# Faz 16 — Kullanıcı Davet Sistemi ve Kiracı Portalı

> **Aktif Faz.** Bu dosya `MASTER-PLAN.md`'nin token tasarrufu kuralı gereği aktif faz boyunca yüklenir.
> Karar tarihi: 2026-06-17.

---

## 1. Bağlam ve Hedef

Bugünkü sistemin tek bir kullanıcı dünyası var: iç ekip (Admin, Yönetici, Görüntüleyici). Kiracı firmalar pasif veri kaydı olarak tutulur — kendi kullanıcılarıyla sisteme giremez. Yeni kullanıcı eklenirken Admin şifreyi elle giriyor. Yetki değişiklikleri 8 saatlik cookie süresince yansımıyor. Audit log yok. Rol/permission listesi kodda sabit.

Bu faz iki ana sorunu çözer:

- **A. Kiracı portalı:** Kiracı firmalar kendi kullanıcılarıyla sisteme girer, yalnızca kendi verilerini (sözleşme, borç, ödeme, mutabakat, rezervasyon) görür ve yönetir.
- **B. Davet bazlı kullanıcı oluşturma:** Hem iç tarafta hem kiracı tarafında davet linki üzerinden hesap oluşturma akışı.

Yan iyileştirmeler:
- Mevcut **SecurityStamp açığı** kapanır (yetki değişiklikleri en geç 3 dakikada yansır).
- **Lockout politikası** eklenir (5 başarısız → 5 dakika kilit).
- **Audit log altyapısı** kurulur (güvenlik olayları izlenir).
- **Roller veritabanına taşınır** (kiracı kendi rollerini tanımlayabilir).
- **PaymentLinkService** tek kullanımlık + iptal edilebilir hale getirilir.

---

## 2. Temel Karar Özeti

| # | Konu | Karar |
|---|---|---|
| 1 | Kullanıcı tablosu | Tek `AspNetUsers`; `UserType` (Internal/Kiraci) + `KiraciId` (nullable) |
| 2 | Firma kavramı | Yeni entity yok; mevcut `Kiraci = firma`; `VergiNo` ve `TcKimlikNo` unique olur |
| 3 | İzin namespace | Sabit listeler kodda: `Internal.*` ve `Kiraci.*` |
| 4 | Rol modeli | DB'de dinamik; `Rol(Scope, KiraciId, IsSystemRole)`; her kiracı kendi rollerini yönetir |
| 5 | Davet sistemi | `Davetiye` tablosu + HMAC token (DB'de hash) + tek kullanımlık + e-posta lock |
| 6 | Şifre sıfırlama | `SifreSifirlamaTalebi` tablosu + ortak `SecureTokenService` |
| 7 | Veri kapsamı | Global Query Filter (kiracı kullanıcısı için otomatik) |
| 8 | Audit log | `AuditLog` tablosu; otomatik (interceptor) + manuel (anlamlı olaylar); hassas alanlar maskelenir |
| 9 | SecurityStamp | Yetki/rol/aktiflik değişikliğinde güncellenir; validator interval = 3 dakika |
| 10 | Lockout | 5 başarısız deneme → 5 dakika kilit |
| 11 | Mevcut roller | Admin sabit (kod); Yönetici/Görüntüleyici seed (DB); `IsSystemRole` koruması |
| 12 | UserTasinmazYetki | Aynen korunur, Görüntüleyici'ye özel iş kuralı — **⚠️ Faz 17 (Yetki Kapsamı) ile superseded:** `KullaniciYetkiKapsami`'ye göç eder, kapsam tek role bağlı olmaktan çıkar. Bkz. `phase-17-yetki-kapsami.md` §3.9 |
| 13 | Kiracı pasifleştirme | `Kiraci.IsActive`; pasifse bağlı kullanıcılar login olamaz; SecurityStamp cascade |
| 14 | Giriş ekranları | İki UI (`/Account/Login`, `/Kiraci/Giris`); tek `SignInManager`, tek cookie scheme |
| 15 | PaymentLinkService | `SecureToken` altyapısına çekilir; tek kullanımlık + iptal edilebilirlik eklenir |
| 16 | Şifre politikası | Mevcut korunur (min 6 + büyük + küçük + rakam + özel); `RequireLowercase` explicit yazılır |

---

## 3. Mimari Karar Detayları

### 3.1 Kullanıcı Modeli

**Tek `AspNetUsers` tablosu, iki ayrı kullanıcı dünyası.**

`ApplicationUser`'a iki yeni alan eklenir:

```csharp
public enum UserType { Internal = 1, Kiraci = 2 }

public class ApplicationUser : IdentityUser, IAuditable
{
    public UserType UserType { get; set; }     // DB'de int, kodda enum
    public int? KiraciId { get; set; }         // Internal'de null, Kiraci'de dolu
    // ... mevcut alanlar (AdSoyad, IsActive, IAuditable)
}
```

**Invariant:** `UserType = Internal` ise `KiraciId = null`; `UserType = Kiraci` ise `KiraciId` dolu. Service ve seed katmanlarında doğrulanır.

### 3.2 Firma Kavramı

Yeni entity yok. **Mevcut `Kiraci` = portal sahibi firma.** `ApplicationUser.KiraciId` doğrudan `Kiraci` tablosuna bağlanır.

**Eklenecek alanlar (`Kiraci`):**

```csharp
public bool IsActive { get; set; } = true;  // YENI: kiracı pasifse bağlı kullanıcılar login olamaz
```

**Unique constraint eklemesi (`ApplicationDbContext.OnModelCreating`):**

```csharp
entity.HasIndex(k => k.VergiNo)
      .IsUnique()
      .HasFilter("[VergiNo] IS NOT NULL AND [VergiNo] <> ''");

entity.HasIndex(k => k.TcKimlikNo)
      .IsUnique()
      .HasFilter("[TcKimlikNo] IS NOT NULL AND [TcKimlikNo] <> ''");
```

**Migration backfill notu:** Mevcut `Kiraci` kayıtlarında aynı `VergiNo` veya `TcKimlikNo` çakışmaları olabilir. Migration'dan **önce** çakışmalar tespit edilir, manuel temizlenir; bu adım Faz 16A'nın ilk işidir.

### 3.3 İzin (Permission) Namespace Yapısı

İzinler kodda sabit string. İki ayrı namespace:

```
PermissionCatalog
├── Internal/
│   ├── Internal.Tasinmaz.View, Create, Edit
│   ├── Internal.Birim.View, Create, Edit, ManageRate
│   ├── Internal.Kiraci.View, Create, Edit
│   ├── Internal.Sozlesme.View, Create, Edit, Extend, Terminate, OverrideRate
│   ├── Internal.Odeme.View, Create, Approve, Reject, UploadDekont,
│   │                       ImportBankStatement, MatchBankTransaction
│   ├── Internal.Kullanici.View, Create, Edit, AssignPermission
│   ├── Internal.Rol.View, Create, Edit, Delete                        [YENI]
│   ├── Internal.Davetiye.View, Create, Cancel, Resend                 [YENI]
│   ├── Internal.Audit.View                                            [YENI]
│   └── ... (mevcut admin/parametre/tarife izinleri Internal.* altına taşınır)
│
└── Kiraci/
    ├── Kiraci.Sozlesme.View                                           [YENI]
    ├── Kiraci.Borc.View                                                [YENI]
    ├── Kiraci.Odeme.View                                               [YENI]
    ├── Kiraci.Cari.View                                                [YENI]
    ├── Kiraci.Mutabakat.Manage                                         [YENI]
    ├── Kiraci.Rezervasyon.View, Create, Cancel                         [YENI]
    ├── Kiraci.Kullanici.View, Invite, Edit, Deactivate, Manage         [YENI]
    └── Kiraci.Rol.View, Create, Edit, Delete                           [YENI]

# Kiraci.Kullanici.Manage = "üst hak". Bu izine sahip olan
# kullanıcı, kiracı kullanıcılarını yönetmeye yetkili kabul edilir
# ve "son yetkili koruması" (bkz. §3.4) bu izin üzerinden çalışır.
# View ≠ Manage: bir kullanıcı yalnızca listeyi görebilir (View) ama
# yönetememeli (Manage) — ikisi farklı semantik, ayrı izin.

# NOT: Kiraci.Talep.* ve Kiraci.Duyuru.* bu fazda TANIMLANMAZ.
# İlgili modüller (Talep Yönetimi, Duyurular) Faz 17+'a ertelendi.
# Bkz. "Açık Riskler" tablosu.
```

**Mevcut izin adlarının `Internal.` prefix'iyle taşınması:** Faz 16A'da tek seferde yapılır. Tüm `[Authorize(Policy = ...)]` kullanımları rename edilir, `PermissionClaimsTransformer` ve `AdminBypassHandler` güncellenir.

**Migration etkisi:** `UserPermissions` tablosundaki mevcut satırlarda `Permission` kolonu değer güncellemesi gerekir (`UPDATE UserPermissions SET Permission = 'Internal.' + Permission`).

### 3.4 Rol Modeli (Dinamik)

İzinler sabit ama roller DB'de.

**Yeni tablolar:**

```
Rol
├── Id (int, PK)
├── Ad (string, max 100)
├── Aciklama (string?, max 500)
├── Scope (int — enum: Internal=1, Kiraci=2)
├── KiraciId (int?, nullable — Scope=Kiraci ise zorunlu, Internal ise null)
├── IsSystemRole (bool — silinemez/scope/KiraciId değiştirilemez)
├── IsActive (bool)
├── CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (IAuditable)
└── UNIQUE INDEX (Scope, KiraciId, Ad)   — aynı kapsamda aynı isimde iki rol olmaz

RolPermission
├── Id (int, PK)
├── RolId (FK → Rol)
├── Permission (string — Internal.* veya Kiraci.* namespace)
└── UNIQUE INDEX (RolId, Permission)

UserRol
├── Id (int, PK)
├── UserId (string, FK → AspNetUsers)
├── RolId (int, FK → Rol)
├── AtanmaTarihi, AtayanUserId
└── UNIQUE INDEX (UserId, RolId)
```

**Identity'nin `AspNetUserRoles` tablosu kullanılmaz.** Çünkü yeni `Rol` tablosu Identity şemasından bağımsız (kendi sütunlarına ihtiyacımız var: `Scope`, `KiraciId`, `IsSystemRole`).

#### Geçiş — `PermissionClaimsTransformer` ne yapar?

Faz 16A cutover'ı sonrası `AspNetUserRoles` tablosu **boş** kalır; tüm rol bağlamaları `UserRol` tablosunda durur. Ancak mevcut kodda **rol-bazlı `User.IsInRole(...)` kontrolleri** ciddi sayıda yerde aktif (örn. `TasinmazController.cs:43, 51, 105, 127`, `KiraciController`, `SozlesmeController`, `OdemeController`, `TahakkukController`, `BirimController`, view'larda sidebar görünürlüğü, `AdminBypassHandler`). Bu kontroller `ClaimTypes.Role` claim'ine bakar.

**Eğer transformer sadece permission claim üretirse:**
- `base.GenerateClaimsAsync()` rol claim'i için `UserManager.GetRolesAsync()` çağırır → boş `AspNetUserRoles`'ten boş döner → `ClaimTypes.Role` claim'i **hiç eklenmez**.
- `User.IsInRole(RoleNames.Goruntuleyici)` → `false` döner.
- Görüntüleyici scope filtresi devreye girmez → **veri sızıntısı**.
- Build temiz, test geçer (test data'da Görüntüleyici yoksa), production'da sessiz patlar.

**Bunun için `PermissionClaimsTransformer` Faz 16A'dan itibaren iki tip claim üretir:**

1. **`ClaimTypes.Role` claim'leri** — `UserRol` tablosundan çekilen rol adlarıyla. `Admin`, `Yonetici`, `Goruntuleyici`, `Firma Yetkilisi` vb. Her rol için ayrı bir `Claim(ClaimTypes.Role, rolAdi)`.
2. **`permission` claim'leri** (`AppClaimTypes.Permission`) — kullanıcının `UserRol → Rol → RolPermission` zincirinden derlenen izinler **+ geçiş döneminde** doğrudan `UserPermission` kayıtları.

**Sonuç:** Tüm mevcut `User.IsInRole(RoleNames.X)` ve `[Authorize(Roles = RoleNames.X)]` kullanımları **değiştirilmeden çalışmaya devam eder**. Sadece rol claim'inin kaynağı değişti (`AspNetUserRoles` → `UserRol`); semantik aynı.

**`base.GenerateClaimsAsync()` korunur** çünkü Identity'nin diğer claim'lerini (`NameIdentifier`, `Name`, `Email`, `SecurityStamp` vb.) üretir; sadece rol claim'i kısmı boş döner ve transformer kendi rol claim'lerini ekler.

**İki kavramı ayır — rol claim kaynağı ile permission claim kaynağı:**

> **Rol claim kaynağında cutover net, hibrit YOK.** Faz 16A'da `AspNetUserRoles` satırları `UserRol`'a tek seferde taşınır, sonra `UserManager.AddToRoleAsync()` / `RemoveFromRoleAsync()` çağrıları **kaldırılır**, yerine `IUserRolService.AddRoleAsync(userId, rolId)` / `RemoveRoleAsync(...)` gelir. Rol claim'i için **çift kaynak okuma denenmez** — `AspNetUserRoles` Faz 16A sonrası boş kalır, `UserRol` tek otorite.
>
> **Permission claim kaynağında ise Faz 16A–16C arası geçici HİBRİT VAR.** `PermissionClaimsTransformer` permission claim'lerini iki kaynaktan derler ve birleştirir (deduplicate edilir):
> 1. Kullanıcının `UserRol → Rol → RolPermission` zincirinden gelen izinler (yeni yapı)
> 2. Doğrudan `UserPermission` tablosundaki kayıtlar (eski yapı, geçiş döneminde korunur)
>
> Bu hibrit dönem **bilinçli ve zorunlu** — mevcut kullanıcıların `UserPermission` satırlarındaki izinleri, role-bazlı yapıya **tam taşınana** kadar (Faz 16C) claim'lere yansımalı; aksi halde mevcut Yönetici/Görüntüleyici kullanıcıları Faz 16A sonrası **izinsiz kalır**. Faz 16C sonunda `UserPermission` tablosu temizlenir, transformer sadece rol-bazlı zinciri okur, hibrit dönem kapanır.

**Seed davranışı:**

| Rol | Scope | KiraciId | IsSystemRole | Notlar |
|---|---|---|---|---|
| Admin | Internal | null | true | Sabit, silinemez, kod'da bypass mantığı var |
| Yönetici | Internal | null | true (Faz 16F sonuna kadar) | Faz 6 sonrası Admin manuel `false` yapabilir |
| Görüntüleyici | Internal | null | true (Faz 16F sonuna kadar) | Faz 6 sonrası Admin manuel `false` yapabilir |
| Firma Yetkilisi | Kiraci | (her kiracı için ayrı kayıt) | true | Silinemez; izin seti kiracı tarafından değiştirilebilir |
| Finans Yetkilisi | Kiraci | (her kiracı için ayrı kayıt) | false | Kiracı serbestçe silebilir/değiştirebilir |

> **Faz 17+ ile gelecek:** "Talep Sorumlusu" rolü Talep modülü ile birlikte seed edilecek. Bu fazda izinleri (`Kiraci.Talep.*`) tanımlanmadığı için seed edilmesi yetim kayıt yaratırdı. Modüler bağımsızlık: rol ve izinleri aynı fazda gelir.

**Kiracı oluşturma sırasında otomatik seed:** Her yeni `Kiraci` kaydı eklendiğinde sistem bu iki şablon rolü o kiracıya kopyalar (`KiraciId` doldurulur, `RolPermission` satırları varsayılan izinlerle yazılır).

**Son yetkili koruması — Bütünsel Kural**

Her aktif kiracıda **`Kiraci.Kullanici.Manage` iznine sahip en az bir aktif kullanıcı** bulunmalıdır. Bu kural rol-temelli değil **izin-temelli** çalışır (bu izine sahip son aktif kullanıcı tespit edilir). Aşağıdaki **tüm** senaryolarda guard tetiklenir; ihlal eden işlem reddedilir:

1. **Kullanıcı pasifleştirme** — Bu kullanıcı pasifleştirildiğinde kiracıda `Kiraci.Kullanici.Manage` izini olan başka aktif kullanıcı kalmıyorsa engellenir.
2. **Kullanıcı rolünü düşürme** — Yeni rol bu izini içermiyorsa ve mevcut rolü içeriyorsa, aynı kontrol.
3. **Kullanıcıdan rol kaldırma (`UserRol` silme)** — Aynı kontrol.
4. **Rolün izin listesinden `Kiraci.Kullanici.Manage`'i çıkarma** — O role atanmış kullanıcıların hepsinin izini eş zamanlı düşer. Eğer kiracıda başka kaynaktan bu izin alan aktif kullanıcı kalmıyorsa engellenir.
5. **Rolü silme** — O role atanmış kullanıcılar role bağlantısını kaybeder. Eğer onlardan herhangi birinin son `Kiraci.Kullanici.Manage` kaynağı bu role bağlıysa engellenir.
6. **Kullanıcı kendi rolünü/iznini değiştiremez** — Self-modification için ayrı kontrol (iç tarafta `AdminUserController.cs:186-208` pattern'inin kiracı tarafı eşdeğeri).

İmplementasyon merkezi: `KiraciKullaniciService.ValidateSonYetkiliAsync(kiraciId, plannedChange)`. Her POST endpoint'i (Edit, Deactivate, Role Edit, Role Delete) bu metodu çağırır. Hata Türkçe ve eyleme-özel mesaj döner: "Sistemde en az bir aktif Firma Yetkilisi bulunmalıdır. Bu işlem onaylanamadı."

### 3.5 Davet Sistemi

**Yeni tablo (`Davetiye`):**

```
Davetiye
├── Id (int, PK)
├── Email (string, max 256)               — davet hedef e-postası, KİLİT
├── AdSoyad (string?, max 200)            — davet eden bilgi olarak girer (opsiyonel)
├── UserType (int — Internal/Kiraci)
├── KiraciId (int?, nullable — Kiraci tarafında dolu)
├── RolId (int, FK → Rol)                 — davet edilen role
├── TokenHash (string, max 128)           — SHA-256 hex, asla düz metin
├── ExpiresAt (datetime)                  — default: now + 7 gün
├── Durum (int — enum: Beklemede=1, KabulEdildi=2, SuresiDolmus=3, IptalEdildi=4)
├── DavetEdenUserId (string, FK → AspNetUsers)
├── KabulTarihi (datetime?)
├── OlusanUserId (string?, FK → AspNetUsers — davet kabulü sonrası oluşan kullanıcı)
├── CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
└── INDEX (Email, Durum), INDEX (KiraciId)
```

**Token formatı (link'te):** `BASE64URL(davetiyeId).BASE64URL(expiresUnix).BASE64URL(HMACSHA256(secret, davetiyeId + "|" + expiresUnix + "|invite"))`

**DB'de saklanan:** `TokenHash = SHA256(rawToken)`. Doğrulama: gelen token hash'lenir, DB'deki hash ile `FixedTimeEquals` ile karşılaştırılır.

**Tek kullanımlık:** Davet kabul edildiğinde `Durum = KabulEdildi` olur. Aynı token bir daha çalışmaz.

**E-posta lock kuralı:**
- Hesap oluşturma formunda e-posta alanı readonly, `Davetiye.Email`'den önceden doldurulur.
- POST tarafında gelen e-posta dikkate alınmaz; server-side olarak `Davetiye.Email` kullanılır.
- (Defansif kodlama: form'da gelen e-posta da varsa `Davetiye.Email` ile karşılaştırılır, eşleşmezse hata.)

**Süresi dolma:** `ExpiresAt < now` ise validation reddeder ve `Durum = SuresiDolmus` olarak işaretlenir.

**Yeniden gönderim:** Mevcut davetiyenin yeni token'ı üretilir (eski `TokenHash` silinir/üzerine yazılır), `ExpiresAt` uzatılır, audit log'a "yeniden gönderildi" kaydı düşer.

**İptal:** Manuel `Durum = IptalEdildi` set edilir, audit log'a kayıt.

**Davet gönderimi tetikleyenler:**
1. İç Admin → yeni iç kullanıcı için (`/Admin/Kullanicilar/Davet`)
2. Yeni kiracı oluşturulduğunda → otomatik, ilk yetkili için (`KiraciController.Ekle` POST sonrası)
3. İç Admin → kiracının ilk yetkilisi davet kabul etmediyse yeniden gönderim
4. Kiracı yetkilisi → kendi ekibi için (`/Kiraci/Kullanicilar/Davet`)

### 3.6 Şifre Sıfırlama Sistemi

Davet sistemi ile **aynı altyapıyı paylaşır** (ortak `SecureTokenService`), ama ayrı entity:

```
SifreSifirlamaTalebi
├── Id (int, PK)
├── UserId (string, FK → AspNetUsers)
├── TokenHash (string, max 128)
├── ExpiresAt (datetime)            — default: now + 1 saat (daveden daha kısa)
├── Durum (int — Beklemede=1, Kullanildi=2, SuresiDolmus=3, IptalEdildi=4)
├── KullanmaTarihi (datetime?)
├── TalepEdenIp (string?, max 64)   — log/güvenlik için
├── CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
└── INDEX (UserId, Durum)
```

**Akış:** `/Account/SifreUnuttum` → e-posta gir → `SifreSifirlamaTalebi` oluştur → e-posta gönder → linke tıkla → yeni şifre belirleme formu → `Durum = Kullanildi` + şifre değişimi (aşağıdaki teknik akış) + `UpdateSecurityStampAsync`.

**Teknik akış — `SecureTokenService` ile Identity arasındaki bağ:**

`UserManager.ResetPasswordAsync(user, code, newPassword)` Identity'nin **kendi token'ını** bekler (HMAC değil, `GeneratePasswordResetTokenAsync` ile üretilen DataProtection token'ı). Bizim HMAC token'ımız doğrudan oraya geçirilemez. Bu yüzden iki katmanlı akış kullanılır:

1. **Dış katman (kullanıcıya görünür):** `SecureTokenService.TryValidate(rawToken, "password-reset")` — bizim HMAC token'ımız doğrulanır, `SifreSifirlamaTalebi` DB hash'i ile eşleştirilir, süre ve durum kontrolü yapılır.
2. **İç katman (sadece doğrulama başarılıysa, atomik blok içinde):**
   ```csharp
   var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
   var result = await _userManager.ResetPasswordAsync(user, identityToken, newPassword);
   ```
   Identity'nin password normalize + validate + hash zincirini bozmamak için bu yol seçildi (`RemovePasswordAsync` + `AddPasswordAsync` kombinasyonu yerine).
3. **Sonra:** `SifreSifirlamaTalebi.Durum = Kullanildi`, `UpdateSecurityStampAsync(user)`, audit log.

**Aynı pattern davet kabulünde de geçerli:** Kullanıcı davet linkiyle hesap oluştururken `UserManager.CreateAsync(user, password)` çağrılır — bu zaten Identity'nin kendi şifre hash zincirini kullanır, ekstra token akışı gerekmez. Davet token'ı sadece "bu kişinin davet ile geldiğini" doğrulamak için kullanılır.

**Kötüye kullanım koruması:** Aynı e-posta için son 15 dakika içinde 3'ten fazla aktif talep varsa rate-limit (yeni talep reddedilir veya yapılır ama mail gönderilmez).

### 3.7 Ortak `SecureTokenService`

Hem `Davetiye` hem `SifreSifirlamaTalebi` hem (revize) `PaymentLinkService` aynı altyapıyı kullanır.

```csharp
public interface ISecureTokenService
{
    // Token üret: raw token döner (link'e konacak), DB'ye yazılacak hash dışarı bilgi olarak verilir
    SecureTokenResult Generate(string entityId, string purpose, TimeSpan ttl);
    
    // Doğrula: raw token + entityId + purpose alır, imza ve süre kontrolü yapar
    bool TryValidate(string rawToken, string entityId, string purpose, out string? reason);
    
    // Hash hesapla: DB'de saklama için
    string ComputeHash(string rawToken);
}

public record SecureTokenResult(string RawToken, string TokenHash, DateTime ExpiresAt);
```

**Purpose değerleri:** `"invite"`, `"password-reset"`, `"payment-portal"`. Aynı imza şeması farklı amaçlar için ayrılır (cross-use prevention).

**Settings:** `SecureToken:Secret` (`appsettings.json`, min 32 karakter, env'den de override edilebilir).

### 3.8 Veri Kapsamı (Global Query Filter)

Kiracı kullanıcısı için **mimari koruma katmanı**. Bu, ana savunma hattıdır — controller'da elle yazılan `KiraciId` filtresine bağımlılığı azaltır. Ancak "imkansız" değildir; bypass yolları vardır (bkz. aşağıdaki güvenlik notu).

**ICurrentUserContext servisi:**

```csharp
public interface ICurrentUserContext
{
    string? UserId { get; }
    UserType? UserType { get; }
    int? KiraciId { get; }
    bool IsKiraciUser { get; }
}
```

DI ile scope'lu olarak `HttpContextAccessor` üzerinden doldurulur (login claims'den okur).

**Etkilenen entity'ler (`OnModelCreating` içinde):**

```csharp
// Kiraci kullanıcısının erişebileceği veriler — KiraciId üzerinden filter
builder.Entity<KiraSozlesmesi>().HasQueryFilter(
    s => !_currentUser.IsKiraciUser || s.KiraciId == _currentUser.KiraciId);

builder.Entity<KiraTahakkuk>().HasQueryFilter(
    t => !_currentUser.IsKiraciUser || t.KiraSozlesmesi.KiraciId == _currentUser.KiraciId);

builder.Entity<KiraOdeme>().HasQueryFilter(
    o => !_currentUser.IsKiraciUser || o.KiraTahakkuk.KiraSozlesmesi.KiraciId == _currentUser.KiraciId);

builder.Entity<Dekont>().HasQueryFilter(
    d => !_currentUser.IsKiraciUser || d.KiraOdeme.KiraTahakkuk.KiraSozlesmesi.KiraciId == _currentUser.KiraciId);

builder.Entity<Rezervasyon>().HasQueryFilter(
    r => !_currentUser.IsKiraciUser || r.KiraciId == _currentUser.KiraciId);

builder.Entity<SozlesmeIslemGecmisi>().HasQueryFilter(
    h => !_currentUser.IsKiraciUser || h.KiraSozlesmesi.KiraciId == _currentUser.KiraciId);

// Kiraci entity'sinin kendisi: kiracı sadece kendi kaydını görür
builder.Entity<Kiraci>().HasQueryFilter(
    k => !_currentUser.IsKiraciUser || k.Id == _currentUser.KiraciId);
```

**İç kullanıcılarda filtre devre dışı** (`IsKiraciUser = false` olduğu için `!false || X` her zaman `true`).

#### Güvenlik Notu — Global Query Filter'ın Bypass Olduğu Yerler

Global Query Filter **sadece LINQ sorgularını** kapsar. Aşağıdaki yollarda filtre devreye girmez ve veri sızıntısı oluşabilir; her birinin nerede kullanıldığı **bilinçli olarak izlenmeli**:

1. **`IgnoreQueryFilters()`** — Açıkça filtreyi atlatır. Sadece iç kullanıcılar için açılan ekranlarda (örn. `/Admin/Kiracilar/{id}/Kullanicilar`) ve `[Authorize(Roles = RoleNames.Admin)]` korumalı endpoint'lerde kullanılır. Her çağrı code review'dan geçer.
2. **Raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`, `Database.ExecuteSqlRawAsync`)** — EF Core query pipeline'ı çalışmadığı için filtre uygulanmaz. Kiracı kullanıcısının dolaylı tetikleyebileceği endpoint'lerde **raw SQL yasaklanır**. Sadece iç tarafta (rapor, banka import vb.) kullanılır.
3. **Dapper / micro-ORM kullanımı** — Aynı sebep. Bu proje sadece EF Core kullanır, ileride Dapper eklenirse aynı disiplin uygulanır.
4. **Background job'lar (`IHostedService`, scheduled task)** — `HttpContext` olmadığı için `ICurrentUserContext` boş döner; filtre **tümüyle devre dışı kalır** (`IsKiraciUser = false`). Background job kodu **kasıtlı olarak tüm veriye erişir** — bu doğru davranış, ama burada üretilen veri **kiracı kullanıcısına servis edilirse** sızıntı olur. Job çıktısı bir tabloya yazılıyorsa, o tabloda `KiraciId` taşımalı ve onun da query filter'ı olmalı.
5. **Özel rapor / aggregate sorguları** — Eğer kiracı tarafına rapor eklenirse (`/Kiraci/Raporlar/...`), sorgu **mutlaka** `KiraciId` filtresi içermeli. Global filter kapsamına alınamayan view/store procedure varsa, manuel `WHERE KiraciId = @kiraciId` zorunlu.
6. **`AsNoTracking().IgnoreQueryFilters()` zinciri** — Çift kombinasyon code review uyarısıdır. Mantıklı bir sebep yoksa kullanılmaz.

**Uygulama:** `IgnoreQueryFilters()` çağrılarını **grep ile periyodik olarak taramak** Faz 16E sonrası DoD listesine eklenir (`grep -rn "IgnoreQueryFilters" KiraTakip/`).

### 3.9 Audit Log

**Yeni tablo:**

```
AuditLog
├── Id (long, PK)
├── EventType (string, max 100) — örn. "User.LoginSuccess", "Invite.Sent", "Role.Permission.Changed"
├── EntityType (string?, max 100) — "ApplicationUser", "Kiraci", "KiraSozlesmesi" ...
├── EntityId (string?, max 100) — int veya guid; string olarak tutulur
├── UserId (string?, FK → AspNetUsers)
├── UserType (int?, enum)
├── KiraciId (int?, FK → Kiraci)
├── IpAddress (string?, max 64)
├── UserAgent (string?, max 500)
├── Details (string?, JSON) — eski/yeni değer, ek bilgiler
├── CreatedAt (datetime, indexed)
└── INDEX (EventType, CreatedAt), INDEX (UserId, CreatedAt), INDEX (EntityType, EntityId)
```

**İki katman:**

**Otomatik katman (Faz 16D'de eklenir) — EF Core `SaveChangesInterceptor`:**
- Her entity create/update/delete için `ChangeTracker`'dan eski/yeni değer okunur.
- Hassas alanlar **filtrelenir** (bkz. 3.10).
- Domain entity'leri için `EventType = "{Entity}.Created/Updated/Deleted"` standardı.

**Recursion önleme (kritik):**
- `AuditLog` entity'sinin kendisi **interceptor tarafından işlenmez**. Aksi halde her audit yazımı yeni bir audit kaydı tetikler → sonsuz döngü / `StackOverflowException`.
- Aynı şekilde **domain history tabloları** (`SozlesmeIslemGecmisi`) interceptor'dan atlanır — kendi audit'leri var, ikinci kez izlenmesi gürültü.
- İmplementasyon: interceptor başında `entry.Entity is AuditLog or SozlesmeIslemGecmisi` ise `continue`. İstisnalar tek bir merkezi listede tutulur (`AuditExclusions.IsExcluded(Type)`).
- Identity sistemi otomatik olarak yaratılan `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` gibi yardımcı tablolar da exclusions listesine alınır (gürültü; manuel olaylar zaten yakalıyor).

**Manuel katman (Faz 16B'den itibaren):**
- `IAuditService.LogAsync(eventType, entityType?, entityId?, details?)` ile çağrılır.
- Anlamlı olaylar: `User.LoginSuccess`, `User.LoginFailed`, `User.Logout`, `User.LockedOut`, `User.PasswordChanged`, `User.PasswordReset`, `Invite.Sent`, `Invite.Resent`, `Invite.Accepted`, `Invite.Cancelled`, `Invite.Expired`, `Role.Created`, `Role.Deleted`, `Role.Permission.Changed`, `User.RoleChanged`, `User.Deactivated`, `Kiraci.Deactivated`.

**UI:** Admin paneline `/Admin/HareketGecmisi` ekranı (Faz 16D). Filtre: kullanıcı, tarih aralığı, EventType, EntityType. **Kiracı kullanıcısına AÇILMAZ.**

**`SozlesmeIslemGecmisi` ile ilişkisi:** İki tablo bağımsız. `SozlesmeIslemGecmisi` domain bilgisidir (sözleşme yaşam döngüsü olaylarını UI'da gösterir, raporlanır). `AuditLog` teknik konudur. Karıştırılmaz.

**Saklama süresi:** Şimdilik belirsiz. Belge "ileride karar verilecek" notuyla bırakılır. Faz 16D'de tablo kurulurken `CreatedAt`'a index zaten var; arşivleme/silme job'u ileride eklenir.

### 3.10 Audit'te Hassas Veri Maskeleme

**Asla loglanmaz (`[AuditIgnore]` attribute):**
- `ApplicationUser.PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`
- `ApplicationUser.PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount` (gürültü; lockout olayı manuel log'a düşer zaten)
- `Davetiye.TokenHash`
- `SifreSifirlamaTalebi.TokenHash`
- Mevcut `PaymentLink` token kayıtları (3.11'den sonra)

**Maskelenerek loglanır (`[AuditMask]` attribute + maske türü):**
- `Kiraci.TcKimlikNo` → `MaskType.TcKimlik` (`1**********`)
- `Kiraci.VergiNo` → `MaskType.VergiNo` (`12*******`)
- `Kiraci.Email`, `ApplicationUser.Email`, `Davetiye.Email` → `MaskType.Email` (`a***@firma.com`)
- `Kiraci.Telefon`, `ApplicationUser.PhoneNumber` → `MaskType.Telefon` (`+90 *** *** ** 67`)

**Implementasyon:** EF Core interceptor entity property'lerini reflection ile okur, `[AuditIgnore]` varsa atlar, `[AuditMask]` varsa maske uygular. Maske türleri tek bir `MaskingService` üzerinden uygulanır.

### 3.11 SecurityStamp ve Lockout

**SecurityStamp güncellemeleri:**

Şu olaylarda `UserManager.UpdateSecurityStampAsync(user)` çağrılır:
- Kullanıcının rolü değişti (`UserRol` ekleme/silme)
- Kullanıcının izinleri değişti (`UserPermission` ekleme/silme — eğer doğrudan kullanıcıya izin atanırsa)
- Bir rolün izin listesi değişti → o role bağlı **tüm kullanıcıların** SecurityStamp'i güncellenir
- Kullanıcı pasifleştirildi/aktifleştirildi
- Kullanıcı şifresini değiştirdi (Identity zaten bunu yapar, kontrol edilir)
- Davet kabul edildi (yeni kullanıcı için ilk stamp)
- Kiracı pasifleştirildi → o kiracıya bağlı tüm kullanıcıların stamp'i güncellenir
- Kiracı yeniden aktifleştirildi → bağlı kullanıcıların stamp'i güncellenir

**Validator interval:**

```csharp
// Program.cs
builder.Services.Configure<SecurityStampValidatorOptions>(o =>
{
    o.ValidationInterval = TimeSpan.FromMinutes(3);
});
```

**Davranış ifadesi:** "Yetki/rol/aktiflik değişiklikleri **en geç 3 dakika** içinde kullanıcının açık oturumuna yansır." (Anlık değil.)

**Lockout politikası:**

```csharp
// Program.cs
options.Lockout.AllowedForNewUsers = true;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
```

`AccountController` ve `KiraciGirisController` her ikisinde de `PasswordSignInAsync(..., lockoutOnFailure: true)`. Login akışında `result.IsLockedOut` branch'i eklenir:

> "Hesabınız geçici olarak kilitlendi. Lütfen X dakika sonra tekrar deneyin."

### 3.12 Kiracı Pasifleştirme Davranışı

`Kiraci.IsActive = false` yapıldığında:

1. Kiracı kullanıcılarının `IsActive`'i değişmez.
2. Login akışında çift kontrol: `user.IsActive && (user.Kiraci == null || user.Kiraci.IsActive)`.
3. O anda açık oturumu olan kiracı kullanıcılarının **tümünün SecurityStamp'i güncellenir** → en geç 3 dakikada oturumları düşer.
4. Audit log'a `Kiraci.Deactivated` kaydı + etkilenen kullanıcı sayısı.

Kiracı tekrar aktifleştirilince kullanıcılar otomatik tekrar girebilir. Tek tek `IsActive` toparlamaya gerek yok.

### 3.13 Giriş Ekranları ve Yönlendirme

**Tek `SignInManager`, tek cookie scheme.** İki UI sayfası:

| URL | Hedef | Sonraki Sayfa |
|---|---|---|
| `/Account/Login` | İç ekip giriş ekranı | `/Home` (`UserType = Internal` ise) |
| `/Kiraci/Giris` | Kiracı kullanıcı giriş ekranı | `/Kiraci/Panel` (`UserType = Kiraci` ise) |

**Post-login yönlendirme `UserType`'a göre yapılır.** Bir kullanıcı yanlış sayfadan girerse system doğru panele yönlendirir (engellemek yerine yönlendirir — esnek). Çıkış akışı her iki taraf için de `/Account/Logout`.

### 3.14 PaymentLinkService İyileştirmesi

Mevcut durumda `PaymentLinkService` HMAC imzalı + süreli ama **stateless** (DB'de iz yok). Yani:
- ❌ Tek kullanımlık değil — token süresi içinde sınırsız tıklanabilir.
- ❌ İptal edilemez — Admin "bu linki geri al" diyemez.
- ✅ Düz metin saklama riski **yok** (zaten saklanmıyor).

**Faz 16E sırasında:**

`PaymentLinkService`, ortak `SecureTokenService` altyapısına geçer + yeni tablo:

```
OdemeLinkKayit
├── Id (int, PK)
├── KiraciId (int, FK)
├── TokenHash (string, max 128)
├── ExpiresAt (datetime)
├── Durum (int — Aktif=1, IptalEdildi=2, SuresiDolmus=3)
├── OlusturmaTarihi (datetime)
├── IptalEdenUserId (string?, FK)
├── IptalTarihi (datetime?)
└── INDEX (KiraciId, Durum)
```

**Tek kullanımlık opsiyonu:** Ödeme linki için "tek kullanımlık" zorunlu değil; aynı kiracının birden çok borcu olabilir, link birden çok kez tıklanabilir. Ama **iptal edilebilir** ve **durum izlenebilir** olur.

Mevcut HMAC mantığı `SecureTokenService` içine taşınır, `BuildLink` ve `TryValidate` aynı interface'i kullanır + ek olarak DB durum kontrolü yapar.

**Eski link uyumluluğu — KARAR: gerekmiyor.**

Proje henüz canlıda değil ve üretim mail'i gönderilmemiş. Faz 16E'de `PaymentLinkService` refactor edilirken eski format token'lar (`{expiresUnix}.{kiraciId}.{hmacBase64Url}`) için geriye dönük destek **eklenmez**. Geliştirme/test ortamında eski mail'lerde geçen linkler Faz 16E sonrası çalışmaz — bilinçli kabul. Eğer ileride canlıya geçmeden önce stage'de gerçek mail testi yapıldıysa ve aktif linkler varsa, refactor'dan önce kullanıcıya hatırlatma yapılır.

Sonuç: Yeni format zorunlu, eski format desteklenmez, `TryValidate` eski formatlı token gelirse "Geçersiz token formatı" döner.

---

## 4. Veri Şeması — Toplu Değişiklik Listesi

### Yeni Tablolar

| Tablo | Faz | Amaç |
|---|---|---|
| `Rol` | 16A | Dinamik rol kayıtları |
| `RolPermission` | 16A | Rol → izin bağlamaları |
| `UserRol` | 16A | Kullanıcı → rol bağlaması |
| `AuditLog` | 16A (tablo) / 16B+ (yazım) | Tüm güvenlik ve domain olayları |
| `Davetiye` | 16B | Davet kayıt + token hash |
| `SifreSifirlamaTalebi` | 16B | Şifre sıfırlama kayıt + token hash |
| `OdemeLinkKayit` | 16E | PaymentLinkService durum takibi |

### Mevcut Tablolarda Değişiklik

| Tablo | Değişiklik | Faz |
|---|---|---|
| `AspNetUsers` | `UserType` (int, NOT NULL, default 1) | 16A |
| `AspNetUsers` | `KiraciId` (int?, FK → Kiraci, nullable) | 16A |
| `Kiraci` | `IsActive` (bool, NOT NULL, default true) | 16A |
| `Kiraci` | `VergiNo` unique index (filter: `IS NOT NULL AND <> ''`) | 16A |
| `Kiraci` | `TcKimlikNo` unique index (filter: `IS NOT NULL AND <> ''`) | 16A |
| `UserPermissions` | `Permission` kolonu değer güncellemesi (prefix `Internal.`) | 16A |

### Silinen Tablolar / Kolonlar

`AspNetUserRoles` tablosu Identity'nin standart tablosu — **silinmez** (Identity şeması korunur) ama **Faz 16A'da net cutover** yapılır: mevcut satırlar `UserRol`'a taşınır, sonra tablo yazılmaz (boş kalır). Identity'nin `UserManager.AddToRoleAsync()` / `RemoveFromRoleAsync()` çağrıları **Faz 16A'da** kaldırılır, yerine `IUserRolService.AddRoleAsync(...)` / `RemoveRoleAsync(...)` gelir. Rol claim'leri artık `PermissionClaimsTransformer` tarafından `UserRol` tablosundan derlenir (bkz. §3.4 "Geçiş — `PermissionClaimsTransformer` ne yapar?").

---

## 5. Alt Faz Haritası

### Faz 16A — Temel Altyapı (Dış davranışı korur, iç güvenlik temelini kurar)

**Hedef:** Veritabanı şeması ve sabit altyapı bileşenleri kurulur. Mevcut SecurityStamp açığı kapanır. Dış davranış büyük ölçüde aynı kalır ama bazı içsel etkileri vardır (aşağıdaki "dikkat noktaları"na bakın).

**Dikkat noktaları:**
- `PermissionCatalog` `Internal.*` rename'i tüm `[Authorize(Policy = ...)]` çağrılarını etkiler — tek dosyada yanlış prefix → 403 hatası.
- `UserPermissions` tablosundaki değer migrasyonu **canlı veriye dokunan bir UPDATE**'tir. Backfill SQL gözle gözle okunmalı, geri alma planı (`DOWN()`) yazılı olmalı.
- Mevcut `UserPermission` kayıtları **geçici olarak** `UserRol`'a paralel tutulur (Faz 16C'de tek kaynağa indirilir). Bu süreçte `PermissionClaimsTransformer` her iki kaynağı da okur — claim çoğalmasına dikkat.
- `SecurityStampValidator` interval'ı kısaltıldığı an mevcut aktif oturumlar 3 dakikada bir doğrulama turundan geçer; bu küçük bir performans yükü ekler ama farkedilmez.
- Lockout devreye girdiği an, mevcut yanlış şifre girme alışkanlığı olan kullanıcılar (örn. test) ilk 5 yanlış denemeden sonra geçici kilitlenir. Test/dev kullanıcılarının bilgilendirilmesi gerekir.

**Adımlar:**

1. **Çakışma temizliği (kod öncesi):** `Kiraci` tablosunda aynı `VergiNo` veya `TcKimlikNo` olan kayıtların manuel temizliği. (DB sorgusu, sonuç user'a sunulur.)
2. `UserType` enum tanımı (`Models/Entities/Enums.cs`).
3. `ApplicationUser`'a `UserType` + `KiraciId` ekleme.
4. `Kiraci`'ya `IsActive` ekleme.
5. `VergiNo` / `TcKimlikNo` unique index'leri.
6. `Rol`, `RolPermission`, `UserRol` tabloları (DbContext + entity'ler).
7. `AuditLog` tablosu (DbContext + entity).
8. `PermissionCatalog`'da tüm mevcut izinleri `Internal.` prefix'iyle taşıma. Tüm `[Authorize(Policy = ...)]` çağrılarının güncellenmesi. `PermissionClaimsTransformer` ve `AdminBypassHandler` güncellenmesi.
9. `UserPermissions` tablosunda mevcut satırların değer güncellenmesi (`Permission = 'Internal.' + Permission`) — migration `Up()` içinde SQL.
10. `Rol`, `RolPermission` seed: Admin/Yönetici/Görüntüleyici kayıtları + mevcut `UserPermission` kayıtlarının role-bazlı olarak yeniden organize edilmesi (geçici çift veri — her kullanıcı hem `UserPermission`'larını korur hem yeni `UserRol`'a bağlanır; Faz 16C'de `UserPermission` temizliği yapılır).
11. **`AspNetUserRoles` → `UserRol` cutover:** Mevcut `AspNetUserRoles` satırları `UserRol` tablosuna taşınır (migration `Up()` içinde SQL). `IdentitySeedService.EnsureUser` ile `IUserRolService` çağrılır (Identity'nin `AddToRoleAsync`'i değil). Tüm `_userManager.AddToRoleAsync(...)` ve `RemoveFromRoleAsync(...)` çağrıları `AdminUserController` dahil rename edilir. `PermissionClaimsTransformer` rol claim'lerini de `UserRol`'dan üretmeye başlar (§3.4 detay).
12. SecurityStamp güncellemesi — etkilenen olaylar için service helper'ı (`IUserSecurityService.UpdateStampAndCascadeAsync`).
13. `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.FromMinutes(3)`.
14. Lockout konfigürasyonu (5 deneme / 5 dakika).
15. `AccountController.PasswordSignInAsync` → `lockoutOnFailure: true`; `IsLockedOut` branch'i + UI mesajı.
16. `AuditService` iskelet (interface + ekleme metodu — interceptor henüz yok).
17. **Minimum audit logging:** Login başarılı/başarısız, logout, lockout, SecurityStamp güncellemeleri loglanır.
18. Migration: `Phase16A_TemelAltyapi`.
19. **Sanity check:** `dotnet build` 0 CS hatası; mevcut testler yeşil; el ile login/logout senaryosu test edilir.

**Faz 16A bitiminde:** Dışarıdan görünüm büyük ölçüde aynı (login/logout/yönetim akışları korunur). İçeride: SecurityStamp açığı kapanmış, lockout devrede, audit altyapısı kayıt yazmaya hazır, permission namespace `Internal.*` standardına geçmiş, **rol tabloları aktif kullanımda** (`AspNetUserRoles` → `UserRol` cutover tamamlandı; rol claim'leri `UserRol`'dan üretiliyor; `User.IsInRole(...)` ve `[Authorize(Roles = ...)]` kontrolleri bozulmadan çalışıyor). **Dinamik rol yönetimi UI'ı** (Admin'in yeni rol tanımlayabilmesi) Faz 16C'de gelir.

### Faz 16B — Davet Sistemi (İç Ekipte)

**Hedef:** Admin'in yeni iç kullanıcı eklerken şifre girmesi sona erer; davet bazlı akış devreye girer. Şifre sıfırlama akışı da bu fazla birlikte eklenir.

**Adımlar:**

1. `SecureTokenService` (ortak altyapı). `appsettings.json` → `SecureToken.Secret`.
2. `Davetiye` entity + tablo + migration.
3. `SifreSifirlamaTalebi` entity + tablo + migration.
4. `IDavetiyeService` + `IsifreSifirlamaService` + DI kayıtları.
5. Mail template: `Views/Shared/EmailTemplates/Davetiye.cshtml` + `SifreSifirlama.cshtml`.
6. `Models/ViewModels/DavetiyeMailModel.cs` + `SifreSifirlamaMailModel.cs`.
7. `AdminUserController` refactor:
   - `Create` GET → davet formuna dönüşür (e-posta + rol seç).
   - `Create` POST → `Davetiye` oluşturur + mail gönderir + audit log.
   - `DavetiyeListesi`, `DavetiyeIptal`, `DavetiyeYenidenGonder` action'ları.
   - Mevcut "şifre girerek user oluşturma" akışı kaldırılır (geriye dönüş yok).
8. `AccountController` genişlemesi:
   - `Davet` GET (token validate) → ad/soyad/şifre formu.
   - `Davet` POST → user yarat + `UpdateSecurityStampAsync` + auto-login + audit.
   - `SifreUnuttum` GET/POST.
   - `SifreSifirla` GET/POST (token validate + reset).
9. `IdentitySeedService` revize: Seed kullanıcılar artık şifreyle değil, "şifre belirlenmiş hâlde" gelmez — `EnsureUser` mantığı korunur (development seed'i için pratik istisna; production seed'i sadece Admin yaratır + Admin'e bir kez kullanım için davet linkini console'a basar).
10. **Audit:** `Invite.Sent`, `Invite.Accepted`, `Invite.Cancelled`, `Invite.Resent`, `Invite.Expired`, `User.PasswordReset.Requested`, `User.PasswordReset.Completed`.
11. UI: `Views/Account/Davet.cshtml`, `SifreUnuttum.cshtml`, `SifreSifirla.cshtml`. Davet kabul formunda e-posta readonly.
12. `Views/AdminUser/Index.cshtml` → "Bekleyen Davetler" sekmesi.
13. Migration: `Phase16B_DavetSistemi`.
14. Manuel test: davet gönder → mail al → linke tıkla → hesap oluştur → login. Şifre sıfırlama akışı için aynı.

### Faz 16C — Dinamik Roller (İç Ekipte)

**Hedef:** Admin yeni iç rol tanımlayabilir; mevcut Yönetici/Görüntüleyici DB üzerinden yönetilir. `UserPermission` doğrudan kullanıcıya izin atama kaldırılır (rol üzerinden geçer).

**Adımlar:**

1. `IRolService` (CRUD + izin atama) + DI.
2. `AdminRolController` (`/Admin/Roller`). Liste, oluştur, düzenle (izin checkbox grid), sil (IsSystemRole kontrolü).
3. `AdminUserController` rol atama UI'ı: kullanıcıya artık tek tek izin değil, **rol** atanır.
4. `PermissionClaimsTransformer` revize: kullanıcının rolleri DB'den çekilir, her rolün izinleri claim olarak yazılır. (Doğrudan `UserPermission` atamaları geçişlik dönem boyunca desteklenir, ama yeni atama yapılamaz.)
5. **Geçiş migrasyonu:** Mevcut `UserPermission` kayıtları analiz edilir, "Yonetici" ve "Goruntuleyici" rolüne göre standart rol kalıpları oluşturulur. Eğer bir kullanıcının izinleri bu kalıba uymuyorsa Admin'e manuel atama uyarısı gösterilir.
6. **Audit:** `Role.Created`, `Role.Updated`, `Role.Deleted`, `Role.Permission.Changed`, `User.RoleChanged`.
7. SecurityStamp cascade: rolün izin listesi değişince o role bağlı tüm kullanıcılar.
8. UI: `Views/AdminRol/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`.
9. Migration: `Phase16C_DinamikRoller`.

### Faz 16D — Audit Log

**Hedef:** EF Core interceptor ile otomatik audit; hassas alan maskeleme; Admin paneline "Hareket Geçmişi" ekranı.

**Adımlar:**

1. `[AuditIgnore]` ve `[AuditMask(MaskType)]` attribute'leri.
2. `IMaskingService` + maske türleri (Email, Telefon, TcKimlik, VergiNo).
3. `AuditSaveChangesInterceptor` — `ChangeTracker.Entries()` ile her değişikliği yakalar, hassas alanları filtreler, `AuditLog` tablosuna yazar.
4. `Program.cs`'te DbContext'e `AddInterceptors(...)` ile bağlanır.
5. `AdminHareketGecmisiController` (`/Admin/HareketGecmisi`). Filtre: kullanıcı, tarih aralığı, EventType, EntityType, KiraciId.
6. UI: `Views/AdminHareketGecmisi/Index.cshtml` (server-side pagination, filter).
7. `Internal.Audit.View` izni eklenir, Admin policy.
8. **Genişleme audit'leri:** CRUD'a ek olarak `User.Deactivated`, `User.Activated`, `Kiraci.Deactivated`, `Kiraci.Activated`, hassas işlemler (dekont yükleme, banka import, manuel borç iptal).
9. Migration: `Phase16D_AuditInterceptor` (sadece kod değişikliği, ama belge için kayıt).

### Faz 16E — Kiracı Portalı (Asıl İş)

**Hedef:** Kiracı kullanıcılarının sisteme giriş yapması ve kendi firmasının verilerini görüp yönetmesi.

**Adımlar:**

1. `ICurrentUserContext` servisi + DI scoped.
2. `ApplicationDbContext` Global Query Filter'ları (3.8'deki entity listesi).
3. `KiraciGirisController` (`/Kiraci/Giris`).
4. `KiraciPanelController` (`/Kiraci/Panel`) — kiracı dashboard.
5. Kiracı tarafı permission listeleri: `PermissionCatalog.Kiraci.*` eklemeleri.
6. Kiracı seed rolleri (Firma Yetkilisi, Finans Yetkilisi) — `KiraciController.Ekle` POST sonrası otomatik kopyalama. Talep Sorumlusu Faz 17+'da Talep modülüyle birlikte eklenir.
7. Kiracı yetkilisi davet etme akışı: `/Kiraci/Kullanicilar`, `/Kiraci/Kullanicilar/Davet`. Davet altyapısı zaten Faz 16B'de hazır.
8. Kiracı kendi rol tanımlama: `/Kiraci/Roller`. (`AdminRolController` mantığı + KiraciId filtresi.)
9. "Son `Kiraci.Kullanici.Manage` yetkili" koruması — 6 senaryo için merkezi guard (`KiraciKullaniciService.ValidateSonYetkiliAsync`): pasifleştirme, rol düşürme, rol kaldırma, rolden izin çıkarma, rol silme, self-modification. Bkz. 3.4 "Son yetkili koruması — Bütünsel Kural".
10. Yeni kiracı kaydı oluşturulurken ilk yetkili davet edilir: `KiraciController.Ekle` POST'a entegrasyon.
11. İç Admin için **müdahale ekranı**: `/Admin/Kiracilar/{id}/Kullanicilar`. Admin bir kiracının kullanıcılarına bakabilir, şifresini sıfırlayabilir, pasifleştirebilir.
12. Kiracı tarafı ekranlar (asgari):
    - `/Kiraci/Sozlesmeler` (kendi firmasının sözleşmeleri)
    - `/Kiraci/Borclar` (kendi firmasının borçları/tahakkukları)
    - `/Kiraci/Odemeler` (geçmiş ödemeler)
    - `/Kiraci/Mutabakat` (cari hesap)
    - `/Kiraci/Rezervasyon` (yeni rezervasyon + listeleme)
13. `Account/Logout` her iki taraf için ortak çalışır.
14. `PaymentLinkService` revize: `OdemeLinkKayit` tablosu + `SecureTokenService` entegrasyonu + iptal API'si.
15. **Audit (kiracı tarafı olayları):** `Kiraci.User.Invited`, `Kiraci.User.Accepted`, `Kiraci.User.Deactivated`, `Kiraci.Role.Created`, `Kiraci.Mutabakat.Confirmed` vb. (UI sadece iç ekibe açık, ama kayıt tutulur.)
16. Migration: `Phase16E_KiraciPortal`.

### Faz 16F — İç Tarafın Temizliği (Opsiyonel)

**Hedef:** Controller'larda dağılmış manuel `User.IsInRole(Goruntuleyici)` + `_yetkiService.GetYetkiliTasinmazIdsAsync()` filtrelerini tek noktaya çekmek.

**Adımlar:**

1. `IIcKapsamFiltresi` servisi — Görüntüleyici scope'unu repository seviyesinde uygular.
2. Etkilenen controller'lar (mevcut taraması): `TasinmazController`, `KiraciController`, `SozlesmeController`, `OdemeController`, `BirimController`, `TahakkukController`, `RezervasyonController`, `ManuelBorcController`.
3. Eksik filtre olduğu tespit edilen yerler (örn. `BirimController` detay ekranı) tamamlanır.
4. Eski rollerin `IsSystemRole`'ü `false`'a çevrilir (Yönetici / Görüntüleyici) — bundan sonra Admin onları yeniden adlandırabilir/silebilir.
5. Migration gerekirse: `Phase16F_KapsamTemizlik` (sadece veri güncellemesi).

---

## 6. Faz Sonu Doğrulama Disiplini (DoD)

Her faz tamamlandığında aşağıdaki kontroller yapılır. **Tüm proje yeniden analiz edilmez** — sadece o fazda değişen dosyalar, doğrudan ilişkili servisler/controller'lar/view'lar/modeller, migration dosyaları ve testler incelenir.

### 6.1 Build Kontrolü
- `dotnet build KiraTakip.csproj` çalıştırılır.
- CS derleme hatası **sıfır olmalı**.
- MSB dosya kilit hataları (uygulama lokalde çalışırken normaldir) yok sayılır.

### 6.2 Test Kontrolü
- `dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj` çalıştırılır.
- Değişen iş kuralları için yeni unit/integration testler eklenir.
- Hata varsa **sadece hata mesajı + stack trace + dosya:satır** context'e alınır. Tüm çıktı yüklenmez.
- Baseline: Faz 16A başında mevcut testlerin yeşil/kırmızı durumu tespit edilir (referans noktası).

### 6.3 Migration Kontrolü
- Yeni migration dosyası `Up()` metodu **gözle okunur**.
- `DropTable`, `DropColumn`, `DropForeignKey` çağrıları varsa **bilinçli mi** doğrulanır.
- Backfill SQL'leri (örn. `UPDATE UserPermissions SET Permission = ...`) açıkça yazılır, otomatik scaffold'a güvenilmez.
- Geliştirme/dummy veri aşamasında olunsa bile etki değerlendirilir.

### 6.4 Güvenlik ve Yetki Kontrolü
- Yetkisiz kullanıcı ilgili ekrana/işleme **erişemez** (manuel test).
- Kiracı kullanıcısı başka kiracıya ait veriye **erişemez** (global query filter aktif).
- Pasif kullanıcı login olamaz.
- Pasif kiracının kullanıcıları login olamaz.
- SecurityStamp davranışı: iki tarayıcıdan giriş, birinde rol değiştir, diğerinde 3 dakika içinde oturum düşer (manuel test).

### 6.5 UI ve Akış Kontrolü
- İlgili ekranlar manuel browser testiyle kontrol edilir.
- Menü görünürlüğünün permission yapısıyla uyumlu olduğu doğrulanır.
- Davet, giriş, yönlendirme, şifre belirleme/sıfırlama akışları gerçek kullanıcı senaryoları üzerinden test edilir.

### 6.6 Audit Kontrolü
- Davet gönderme, davet kabulü, login/logout, başarısız giriş, rol değişikliği, yetki değişikliği, kullanıcı pasifleştirme, şifre değiştirme/sıfırlama olaylarının `AuditLog` tablosuna yazıldığı **doğrudan DB sorgusuyla** kontrol edilir:
  ```sql
  SELECT TOP 50 * FROM AuditLog ORDER BY CreatedAt DESC
  ```
- Audit log içinde `PasswordHash`, `SecurityStamp`, `TokenHash` ve hassas kişisel verilerin **açık şekilde tutulmadığı** doğrulanır (sample satırlarda gözle bakılır).

### 6.7 Dokümantasyon Kontrolü
- `PROGRESS.md`'de ilgili alt faz checkbox'ları işaretlenir, **açıklama eklenmez**.
- Uygulama sırasında alınan yeni kararlar, değişen varsayımlar veya bilinçli sapmalar **bu dosyaya** (faz dokümanı) kısa ve net şekilde eklenir.
- `MASTER-PLAN.md`'de faz durumu güncellenir.

---

## 7. Açık Riskler ve İleride Karar Verilecekler

| Konu | Durum | Not |
|---|---|---|
| Audit log saklama süresi (örn. 1 yıl, 5 yıl, sınırsız) | Belirsiz | KVKK + şirket politikası kararı. Arşivleme/silme job'u Faz 16D sonrası eklenir. |
| Şifre politikasının güçlendirilmesi (min 10+) | Şimdilik 6 | Konfigürable yapılır, ileride config'den değiştirilir. |
| IP bazlı lockout / rate limiting | Yok | Şimdilik kullanıcı bazlı lockout yeter. Saldırı senaryosunda gündeme gelir. |
| Background job (Hangfire/Quartz) | Yok | Davet/sıfırlama mail'leri senkron gönderilir. Hacim artarsa eklenir. |
| 2FA | Yok | Internal araç için kabul edilebilir. Admin için ileride düşünülebilir. |
| Kiracı yetkilisinin firma profili düzenleme yetkisi | Karar dışı | Faz 16E sırasında UX'le birlikte karara bağlanır. |
| Kiracı portalı talep yönetimi (Talep Modülü) | Faz 17+'a ertelendi | İzinler ve UI bu fazda yok. Modül tamamen ayrı bir faz olarak gelir (Talep entity'leri, atama, durum geçişleri, bildirim akışı vb.). |
| Kiracı portalı duyuru sistemi | Faz 17+'a ertelendi | İzinler ve UI bu fazda yok. Admin tarafı duyuru yönetimi + kiracı tarafı duyuru görüntüleme sonraki fazda. |

---

## 8. Etkilenecek Mevcut Dosyalar (Hızlı Referans)

### Authorization
- `Authorization/RoleNames.cs` — `Yonetici` ve `Goruntuleyici` sabitleri korunur (geçiş döneminde kod hâlâ kullanır)
- `Authorization/PermissionCatalog.cs` — tüm üyeler `Internal.` prefix'iyle yeniden organize edilir; `Kiraci.*` bloğu eklenir
- `Authorization/PermissionClaimsTransformer.cs` — DB'den rol+izin okumaya geçer
- `Authorization/AdminBypassHandler.cs` — `Internal.*` namespace'ine göre güncellenir

### Data
- `Data/ApplicationDbContext.cs` — yeni `DbSet`'ler (`Roller`, `RolPermissionlar`, `UserRoller`, `Davetiyeler`, `SifreSifirlamaTalepleri`, `AuditLog`, `OdemeLinkKayitlari`); `OnModelCreating` query filter'lar; unique index'ler
- `Data/UnitOfWork.cs` — yeni entity'ler için pattern korunur

### Models/Entities
- `Models/Entities/ApplicationUser.cs` — `UserType`, `KiraciId` eklenir
- `Models/Entities/Kiraci.cs` — `IsActive` eklenir
- `Models/Entities/Enums.cs` — `UserType`, `RolScope`, `DavetiyeDurumu`, `SifreSifirlamaDurumu`, `MaskType` enum'ları eklenir
- Yeni: `Rol.cs`, `RolPermission.cs`, `UserRol.cs`, `Davetiye.cs`, `SifreSifirlamaTalebi.cs`, `AuditLog.cs`, `OdemeLinkKayit.cs`

### Services
- `Services/IdentitySeedService.cs` — davet bazlı seed'e geçer (development istisnası dışında)
- `Services/UserTasinmazYetkiService.cs` — değişmez (iç taraf Görüntüleyici scope)
- `Services/PermissionService.cs` — DB rol/izin lookup
- `Services/PaymentLinkService.cs` — `SecureTokenService` üzerine refactor + `OdemeLinkKayit` entegrasyonu
- Yeni: `Services/SecureTokenService.cs`, `DavetiyeService.cs`, `SifreSifirlamaService.cs`, `RolService.cs`, `AuditService.cs`, `MaskingService.cs`, `UserSecurityService.cs`, `CurrentUserContext.cs`, `KiraciKullaniciService.cs`

### Controllers
- `Controllers/AccountController.cs` — `Davet`, `SifreUnuttum`, `SifreSifirla` action'ları
- `Controllers/AdminUserController.cs` — davet bazlı akışa dönüşür; rol atama UI'ı
- Yeni: `Controllers/KiraciGirisController.cs`, `KiraciPanelController.cs`, `KiraciKullaniciController.cs`, `KiraciRolController.cs`, `AdminRolController.cs`, `AdminHareketGecmisiController.cs`, `AdminKiraciKullaniciController.cs` (Admin'in kiracıya müdahalesi için)
- Kiracı tarafı modül controller'ları: `KiraciSozlesmeController.cs`, `KiraciBorcController.cs`, `KiraciOdemeController.cs`, `KiraciMutabakatController.cs`, `KiraciRezervasyonController.cs`

### Views
- `Views/Account/` — `Davet.cshtml`, `SifreUnuttum.cshtml`, `SifreSifirla.cshtml`
- `Views/AdminUser/` — davet akışı UI
- `Views/AdminRol/`, `Views/AdminHareketGecmisi/` (yeni)
- `Views/Kiraci/` (portal kullanıcı arayüzleri — yeni)
- `Views/Shared/EmailTemplates/Davetiye.cshtml`, `SifreSifirlama.cshtml`

### Program.cs
- `Identity` konfigürasyonu: lockout, validation interval
- DI: yeni servisler
- Global query filter context wiring
- `AuditSaveChangesInterceptor` bağlantısı

### appsettings.json
- `SecureToken:Secret`
- `Identity:Lockout:*` (mevcut konfigürasyon `Program.cs` içinde sabit; istenirse buraya taşınabilir)
- (Mevcut `PaymentLink:Secret` korunur, ortak kullanıma alınır)

---

## 9. Notlar

- **Veri kaybı tehdidi**: Faz 16A'nın `VergiNo` / `TcKimlikNo` unique index migrasyonu çakışan kayıt olduğunda **başarısız olur**. Bu bilinçli — temizlik manuel yapılmalı.
- **Geriye dönüş**: Faz 16A migration'ı geri alınabilir (`DropColumn` ile `UserType`/`KiraciId`/`IsActive` geri çıkar). Sonraki fazlarda geriye dönüş giderek zorlaşır.
- **Test stratejisi**: Faz 16A başlamadan önce mevcut test paketinin **baseline durumu** PROGRESS.md'ye not düşülür ("XX/YY test yeşil" gibi). Sonraki fazlarda regresyon bu noktaya göre değerlendirilir.
