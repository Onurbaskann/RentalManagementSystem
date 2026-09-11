# Faz: Kiracı Rol Mimarisi Refactoring (Hybrid Yaklaşım)

Bu refactoring, kiracı tarafındaki (Tenant Portal) rol mimarisini modernize etmeyi amaçlamaktadır. Amaç, ilk "Owner" (Kiracı Yöneticisi) rolünü global ve değiştirilemez bir sistem rolü olarak tutarken, geri kalan tüm rolleri kiracının kendi yönetimine bırakmaktır.

## Neden Bu Modele Geçildi?
Önceki yapıda `Kiracı Yöneticisi` ve `Kiracı Sorumlusu` gibi sabit roller vardı. Ancak kiracıların "Kendi rollerini oluşturma" (Dinamik Roller) özelliği sisteme eklendiği için, birden fazla global rolün olması karmaşa yaratıyordu.

Sistemin kurulumu (Bootstrapping) ve kiracının yeni özelliklere otomatik kavuşabilmesi için melez (Hybrid) bir yapı seçilmiştir:
1. **Kiracı Yöneticisi:** Tüm kiracılar için ortak olan, silinemeyen ve düzenlenemeyen, sistem tarafından yönetilen tek Global Rol (`KiraciId = null`, `IsSystemRole = true`).
2. **Dinamik Roller:** Kiracının kendisi tarafından yaratılan (`KiraciId != null`), `PermissionCatalog.KiraciPortal` içinden istenilen yetkilerin eklenebildiği roller. (Örn: Muhasebe, Saha).

## Yapılan Değişiklikler

### 1. `RoleNames.cs` Temizliği
- `RoleNames.KiraciSorumlusu` tamamen silindi. 
- Enum yapısında sadece `SistemYoneticisi` ve `KiraciYoneticisi` bırakıldı.

### 2. `RolService.cs` (Seeding)
- `EnsureGlobalKiraciRolleriAsync` metodu sadece `KiraciYoneticisi` rolünü kontrol edip, ona `PermissionCatalog.KiraciAll` listesindeki tüm yetkileri atayacak şekilde güncellendi.
- "Kiracı Sorumlusu" seeding mantığı kaldırıldı.

### 3. Son Yetkili Kontrolü (`EnsureSonYetkiliAsync`)
- Bir kullanıcının hesabı pasifleştirilirken veya rolü değiştirilirken çalışan güvenlik kalkanı güncellendi.
- Eski rol tabanlı (Role ID ile dışlama yapan) yapı yerine, "Kiracı içinde aktif durumda ve `KiraciYoneticisi` rolüne sahip en az 1 kişi kalıyor mu?" şeklinde Claim/Role bazlı bir kontrol mekanizması kuruldu.

## Gelecek Notları
- Kiracı portalına yeni bir modül eklendiğinde, `PermissionCatalog.KiraciAll` listesine eklendiği an `EnsureGlobalKiraciRolleriAsync` çalıştığında tüm kiracı yöneticilerine otomatik olarak aktarılmış olacaktır. (Hybrid mimarinin en büyük faydası).
