# Süper Admin ve Pure Claims Mimarisi Refaktörü — Implementation Spec

**Durum:** 🟡 Tasarım onaylandı, uygulamaya hazır (2026-06-29)

> Bu dosya **kontrat**tır. Spec dışına çıkma. Karar değişikliği için önce buraya yansıt.

---

## Amaç

Sistemin iş mantığına, UI gösterimlerine ve veri izolasyonu kurallarına ismen (hardcoded string olarak) gömülmüş olan `Sistem Yöneticisi` ve `Operasyon Müdürü` rolü bağımlılıklarını tamamen ortadan kaldırmak. Süper Adminlik yetkisini role değil kullanıcıya (flag olarak) atamak ve menü gösterimlerini `.Module` gibi gereksiz yetkiler yerine akıllı (prefix tabanlı) kontrollere devretmek.

**Üç temel hedef:**
1. **Güvenlik İzolasyonu:** `IsSuperAdmin` bir rol değil kullanıcı bayrağı olacak ve arayüzden kimseye atanamayacak (sadece SQL ile).
2. **Magic String Temizliği:** İş mantığından (Örn: Veri izolasyonu) `RoleNames.OperasyonMuduru` gibi isim kontrolleri kaldırılıp mantık genelleştirilecek.
3. **Akıllı Menü (Prefix):** `.Module` veya sahte `.View` yetkileri silinecek, menü gösterimi sadece ana modül kök iznine (`Internal.Tasinmaz`) veya alt aksiyonların varlığına bağlanacak.

---

## 🚫 NEGATİF LİSTE — KESİNLİKLE YAPMA

### Yapısal
- ❌ Süper Admin için herhangi bir arayüz (UI) bileşeni, checkbox veya toggle yapma.
- ❌ `Roller` tablosunda "Sistem Yöneticisi" veya eşdeğeri bypass rolü bırakma.
- ❌ UI veya Controller katmanında `User.IsInRole("Operasyon Müdürü")` tarzı isim tabanlı yetki kontrolü yapma.
- ❌ Modül listeleme erişimi için tekrar `.Module` (Örn: `Internal.Tasinmaz.Module`) gibi 3. seviye yetkiler icat etme.

### UI
- ❌ Admin panelinde kullanıcı oluştururken veya düzenlerken `IsSuperAdmin` özelliğini gösterme.
- ❌ Arayüzden ikinci bir Süper Admin yaratılmasına imkan tanıma (bu sadece veritabanı müdahalesi ile yapılmalı).

---

## §1. Süper Admin Flag'i ve Rol Temizliği

### §1.1 `ApplicationUser.cs` Güncellemesi ve Migration
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Models\Entities\ApplicationUser.cs`
- `ApplicationUser` sınıfına aşağıdaki property eklenmelidir:
```csharp
public bool IsSuperAdmin { get; set; } = false;
```
- Bu değişiklikten sonra Package Manager Console (veya CLI) üzerinden `AddIsSuperAdminToUsers` adında bir EF Core Migration oluşturulmalı ve veritabanına uygulanmalıdır (`dotnet ef migrations add AddIsSuperAdminToUsers`).

### §1.2 Seed Temizliği ve Super Admin Ataması
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Services\IdentitySeedService.cs`
- Seed işlemi sırasında oluşturulan `RoleNames.SistemYoneticisi` rolüne ait tüm oluşturma ve `RolPermissions` tohumlama (seeding) kodları **tamamen silinmelidir**. 
- Varsayılan `admin@kiratakip.local` kullanıcısı yaratılırken `IsSuperAdmin = true` olarak atanmalıdır.

### §1.3 Claim Üretimi (Claims Transformation)
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Authorization\PermissionClaimsTransformer.cs`
- Kullanıcı sisteme girdiğinde `IsSuperAdmin` veritabanından kontrol edilmeli ve eğer `true` ise Claims içerisine özel bir bayrak eklenmelidir.
- Mevcut yapıdaki `if (roles.Contains(RoleNames.SistemYoneticisi))` bloğu silinmelidir. 
- **Bunun yerine eklenecek mantık (Pseudo-code):**
```csharp
var user = await _userManager.GetUserAsync(principal);
if (user != null && user.IsSuperAdmin)
{
    ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("IsSuperAdmin", "true"));
    // SUPER ADMIN'E VERITABANINDAN VEYA KODDAN TUM YETKILERI EKLEMEYIN! 
    // BypassHandler bunu halledecektir. Sadece bayragi (claim) koyup gecin.
}
```

### §1.4 Bypass Handler Güncellemesi
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Authorization\AdminBypassHandler.cs`
- **Eski Kod:** `if (context.User.IsInRole(RoleNames.SistemYoneticisi))`
- **Yeni Kod:** `if (context.User.HasClaim(c => c.Type == "IsSuperAdmin" && c.Value == "true"))`
- Bu kontrol geçilirse `context.Succeed(requirement);` çağrılır ve yetki engeli aşılır.

---

## §2. Kapsam (Data Scoping) İş Mantığının Genelleştirilmesi

### §2.1 Veri İzolasyonu (AdminUserController.cs)
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Controllers\AdminUserController.cs`
- `Create` ve `Edit` (POST) metotları içerisinde taşınmaz kısıtlamasını "Operasyon Müdürü"ne özel kılan tüm hardcoded string kontrolleri silinmelidir.
- **Eski Mantık:** `var scopeIds = (yeniRol.Ad == RoleNames.OperasyonMuduru && !model.TumTasinmazlaraErisim) ...`
- **Yeni Mantık:** Hedeflenen kullanıcı Süper Admin değilse, kısıtlanabilmesine izin verilmelidir:
```csharp
var scopeIds = (!model.TumTasinmazlaraErisim) ? model.SelectedTasinmazIds : null;
// Süper admin kontrolü ayrıca user bazlı yapılabilir: if (!targetUser.IsSuperAdmin && !model.TumTasinmazlaraErisim)
```
- Arayüzde (UI) de `SelectedTasinmazIds` alanını gizleyen/gösteren JavaScript varsa, rol ismine bakmaksızın her iç (internal) kullanıcı için açılmalıdır.

---

## §3. Prefix Tabanlı Menü Yetkilendirmesi (Claims)

### §3.1 Extension Metodu (Yeni Sınıf)
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Extensions\ClaimsPrincipalExtensions.cs` (Eğer klasör yoksa oluşturun)
- Menüleri ".Module" eki aramaksızın dinamik göstermek için extension yazılmalıdır:
```csharp
public static class ClaimsPrincipalExtensions
{
    public static bool HasModuleAccess(this ClaimsPrincipal user, string modulePrefix)
    {
        // 1. Süper admin her modüle erişebilir
        if (user.HasClaim(c => c.Type == "IsSuperAdmin" && c.Value == "true"))
            return true;
            
        // 2. Kullanıcının claim'leri arasında bu prefix ile başlayan bir Permission var mı?
        // Örn: "Internal.Tasinmaz" prefix'i verildiğinde, "Internal.Tasinmaz.Create" varsa true döner.
        return user.Claims.Any(c => 
            c.Type == KiraTakip.Authorization.AppClaimTypes.Permission && 
            c.Value.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase));
    }
}
```

### §3.2 Gereksiz Yetkilerin Silinmesi
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Authorization\PermissionCatalog.cs`
- Her modül altındaki `public const string Module = "Internal.X.Module";` sabitleri silinmelidir.
- `OperasyonMuduruIzinleri` gibi seed listelerinde yer alan tüm `.Module` içeren atamalar temizlenmelidir.

### §3.3 Arayüz (View) Kontrolleri
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Views\Shared\_Sidebar.cshtml` (ve ilgili menü dosyaları)
- Dosyanın başına `@using KiraTakip.Extensions` eklenmelidir.
- **Eski Kod Örneği:** 
  `@if (User.IsInRole(RoleNames.SistemYoneticisi) || User.IsInRole(RoleNames.OperasyonMuduru))`
- **Yeni Kod Örneği:** 
  `@if (User.HasModuleAccess("Internal.Tasinmaz"))`
- Sistemdeki tüm (Kiracı modülleri dahil) hardcoded `IsInRole` kontrolleri bu formata çekilmelidir.

### §3.4 Dashboard Yönlendirmeleri
**Dosya:** `d:\Software\RentalManagementSystem\KiraTakip\Controllers\HomeController.cs`
- `Index` metodundaki yönlendirme `User.IsInRole(RoleNames.OperasyonMuduru)` şeklindeki rol kontrolünden çıkarılmalıdır.
- **Yeni Mantık:**
```csharp
if (User.HasModuleAccess("Internal")) 
{
    return RedirectToAction("Index", "Dashboard"); // İç ekip dashboard
}
else if (User.HasModuleAccess("Kiraci"))
{
    return RedirectToAction("Index", "KiraciPanel"); // Kiracı portalı
}
```

---

## §4. Doğrulama (Verification) Adımları

1. **Güvenlik Doğrulaması:** `ApplicationUser.IsSuperAdmin` alanı hiçbir UI formuna (Create/Edit) dahil edilmemiştir.
2. **Rol Silinmesi:** Veritabanında (veya UI'da) "Sistem Yöneticisi" adında bir rol bulunmadığı kontrol edilir.
3. **Bypass Doğrulaması:** SQL üzerinden `IsSuperAdmin=true` yapılan bir hesabın tüm modüllere istisnasız erişebildiği kontrol edilir.
4. **Dinamik Kapsam Doğrulaması:** Yeni oluşturulan sıradan bir "Muhasebe Uzmanı" personeline sadece "AVM-1" taşınmazını görebilmesi için (Operasyon Müdürü olmasına gerek kalmadan) başarıyla kapsam kısıtlaması yapılabilmesi test edilir.
