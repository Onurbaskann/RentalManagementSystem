# Faz 1 — Permission Modeli (Tasarım Aşaması)

> **Hedef:** Permission entity'lerini ve interface'leri tanımla.
> **Yapma:** DB migration, login claim yükleme, policy aktivasyonu — bunlar Faz 4'te.
> **Süre:** Küçük. Sadece dosya oluşturma + DI kayıt iskelet.

**Referanslar:** `permission-spec.md`, `permission-catalog.md`

---

## Görevler

### 1.1 — `Models/UserPermission.cs` oluştur
- [ ] Alanlar: `Id`, `UserId`, `Permission`, `GrantedBy`, `GrantedAt`
- [ ] `ApplicationUser` navigation property'si yok (sadece string FK; cascade için DbContext'te yapılandırılır)
- [ ] Validation attribute'ları: `[Required]`, `[StringLength(100)]` permission alanına

### 1.2 — `Authorization/PermissionCatalog.cs` oluştur
- [ ] Statik class
- [ ] Her permission için `public const string` (örn: `public const string TasinmazView = "Tasinmaz.View";`)
- [ ] `public static IReadOnlyList<string> All` listesi
- [ ] Modül bazlı gruplandırma için iç class'lar (örn: `PermissionCatalog.Tasinmaz.View`)
- [ ] **Senkron:** `permission-catalog.md` ile birebir aynı

### 1.3 — `Services/IPermissionService.cs` interface'i tanımla
- [ ] `Task<IList<string>> GetUserPermissionsAsync(string userId)`
- [ ] `Task<bool> HasPermissionAsync(string userId, string permission)`
- [ ] `Task SetUserPermissionsAsync(string userId, IEnumerable<string> permissions, string grantedByUserId)`
- [ ] `Task<IDictionary<string, IList<string>>> GetPermissionsByRoleAsync()` (UI için varsayılan değerler)

### 1.4 — `Services/PermissionService.cs` iskelet implementasyonu
- [ ] `IPermissionService` implementasyonu
- [ ] Constructor: `ApplicationDbContext`, `UserManager<ApplicationUser>`
- [ ] **Şimdilik:** Metod gövdeleri `throw new NotImplementedException()` veya boş — DB henüz hazır değil
- [ ] **Faz 4'te tamamlanacak**

### 1.5 — `ApplicationDbContext`'e DbSet ekle
- [ ] `public DbSet<UserPermission> UserPermissions { get; set; }`
- [ ] `OnModelCreating`'de unique index: `(UserId, Permission)`
- [ ] **Migration oluşturma — Faz 2'de yapılacak**

### 1.6 — `Program.cs` DI kaydı
- [ ] `services.AddScoped<IPermissionService, PermissionService>();`
- [ ] **Policy kayıtlarını şimdi yapma** — Faz 4'te

### 1.7 — Sanity check
- [ ] `dotnet build` başarılı mı? Hatasız derlenmeli.
- [ ] `dotnet run` başlıyor mu? (Migration henüz çalıştırılmamalı çünkü tablo yok ama DbSet eklendi — Faz 2'ye kadar bu tabloya hiçbir sorgu atılmıyor olmalı)

---

## Çıktılar

- Yeni dosyalar: `UserPermission.cs`, `PermissionCatalog.cs`, `IPermissionService.cs`, `PermissionService.cs`
- Güncellenmiş: `ApplicationDbContext.cs`, `Program.cs`
- Migration: **YOK** (Faz 2'de oluşturulacak)

---

## Dikkat

1. **Hiçbir Controller'a `[Authorize(Policy = ...)]` ekleme.** Policy'ler henüz tanımlı değil, runtime hatası verir.
2. **`PermissionService` çağrısı yapılmasın.** DB tablosu yok.
3. **Mevcut yetki davranışı bozulmasın.** Rol bazlı `[Authorize(Roles = ...)]` aynen çalışmaya devam etmeli.

---

## Tamamlandığında

- [ ] `PROGRESS.md` Faz 1 bölümünü işaretle
- [ ] `MASTER-PLAN.md` "Aktif Faz" satırını **Faz 2** olarak güncelle
- [ ] Faz 2'ye geç: `phase-2-sqlserver-migration.md` oku
