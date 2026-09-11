# Faz 2 — SQL Server Tam Geçiş

> **Hedef:** SQLite'tan SQL Server'a geç. Identity + tüm domain entity'leri + permission tabloları tek dalga migration ile oluştur.
> **Kritik:** Mevcut migration'lar SQLite'a göre yazıldı. SQL Server'da tip uyumsuzlukları olabilir → temiz başlangıç.

**Referanslar:** `permission-spec.md`

---

## Görevler

### 2.1 — Hazırlık
- [ ] SQL Server bağlantısının çalıştığını doğrula (LocalDB veya SQL Server Express)
- [ ] Test DB adı belirle: `KiraTakipDb` (geliştirme), `KiraTakipDb_Test` (test)
- [ ] Mevcut `kiratakip.db` SQLite dosyasını yedekle: `KiraTakip/kiratakip.db.backup`

### 2.2 — NuGet paketlerini güncelle
- [ ] `Microsoft.EntityFrameworkCore.Sqlite` paketini KALDIR
- [ ] `Microsoft.EntityFrameworkCore.SqlServer` paketini EKLE
- [ ] `Microsoft.EntityFrameworkCore.Design` paketini koru (CLI için)
- [ ] `dotnet restore` çalıştır

### 2.3 — `Program.cs` provider değişimi
- [ ] `UseSqlite(...)` → `UseSqlServer(...)` olarak değiştir
- [ ] Connection string mantığını koru

### 2.4 — `appsettings.json` connection string güncelle
- [ ] `DefaultConnection` → SQL Server formatı:
  ```
  "Server=(localdb)\\MSSQLLocalDB;Database=KiraTakipDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  ```
- [ ] `appsettings.Development.json` aynı formatla güncelle
- [ ] Yorumla eski SQLite connection string'i sakla

### 2.5 — Mevcut migration'ları arşivle
- [ ] `KiraTakip/Migrations/` klasörünü `KiraTakip/_Migrations.SQLite.archive/` olarak yeniden adlandır
- [ ] Yeni boş `Migrations/` klasörü oluşturma — `dotnet ef` otomatik yapar

### 2.6 — Domain entity'lerini DbContext'e ekle
`Data/ApplicationDbContext.cs` içine:
- [ ] `DbSet<Tasinmaz> Tasinmazlar`
- [ ] `DbSet<Birim> Birimler`
- [ ] `DbSet<Kiraci> Kiraciler`
- [ ] `DbSet<KiraSozlesmesi> Sozlesmeler`
- [ ] `DbSet<SozlesmeIslemGecmisi> SozlesmeIslemGecmisleri`
- [ ] `DbSet<UserPermission> UserPermissions` (Faz 1'den)
- [ ] `DbSet<UserTasinmazYetki> UserTasinmazYetkileri` (mevcut)

### 2.7 — `OnModelCreating` ilişki tanımları
- [ ] `Tasinmaz` ↔ `Birim` (1-N)
- [ ] `Birim` ↔ `KiraSozlesmesi` (1-N)
- [ ] `Kiraci` ↔ `KiraSozlesmesi` (1-N)
- [ ] `KiraSozlesmesi` ↔ `SozlesmeIslemGecmisi` (1-N, cascade delete)
- [ ] `UserPermission`: `(UserId, Permission)` unique index
- [ ] `UserTasinmazYetki.TasinmazId` → `Tasinmazlar.Id` FK ekle (artık tablo var)
- [ ] `decimal` alanlar için precision: `HasPrecision(18, 2)` (KiraBedeli, Yuzolcumu vb.)
- [ ] `Kiraci.KiraciNo` unique index

### 2.8 — Yeni baseline migration
- [ ] `dotnet ef migrations add InitialCreate` çalıştır
- [ ] Üretilen migration dosyasını incele:
  - [ ] Tüm tablolar var mı?
  - [ ] FK'lar doğru mu?
  - [ ] `decimal` precision'ları doğru mu?
  - [ ] `nvarchar` uzunlukları makul mu?
- [ ] Sorun varsa migration'ı sil, model'i düzelt, tekrar çalıştır

### 2.9 — DB oluştur
- [ ] `dotnet ef database update` çalıştır
- [ ] SSMS/Azure Data Studio ile bağlan, tablolar oluştu mu kontrol et
- [ ] `AspNetUsers`, `Tasinmazlar`, `UserPermissions`, `UserTasinmazYetkileri` görünmeli

### 2.10 — Identity seed test
- [ ] Uygulamayı çalıştır
- [ ] `IdentitySeedService` 3 rol + 3 default user oluşturuyor mu?
- [ ] Login dene: `admin@kiratakip.local` / `Admin123!`

### 2.11 — Sanity check
- [ ] Mevcut sayfalar açılıyor mu? (Domain verisi henüz yok ama hata vermemeli)
- [ ] DB'ye sorgu atılıyor mu? (Hata yoksa boş liste döner)

---

## Risk Listesi

| Risk | Etki | Önlem |
|------|------|-------|
| `decimal` precision hatası | KiraBedeli yanlış kayıt | Tüm `decimal` alanlara explicit precision ver |
| `string` uzunlukları default 4000 | Index hatası | `[StringLength]` veya `HasMaxLength` ekle |
| `KiraciNo` çakışması | Duplicate | Unique index zorunlu |
| Identity tablo isim çakışması | Migration hatası | `IdentityDbContext` zaten halleder, dokunma |
| `DummyDataService` hâlâ Singleton | Lifetime hatası | Faz 3'te kaldırılacak; Faz 2'de dokunma |

---

## Çıktılar

- Güncellenmiş: `ApplicationDbContext.cs`, `Program.cs`, `appsettings.json`, `KiraTakip.csproj`
- Yeni: `Migrations/InitialCreate.cs`, `Migrations/ApplicationDbContextModelSnapshot.cs`
- Arşivlenmiş: `_Migrations.SQLite.archive/`
- DB: SQL Server'da `KiraTakipDb` oluşmuş, Identity verisi seeded

---

## Dikkat

1. **DummyDataService'e dokunma.** Hâlâ Singleton olarak çalışmaya devam edecek; Controller'lar ondan veri okumaya devam edecek. Faz 3'te kaldırılacak.
2. **Hiçbir domain verisi DB'ye gitmiyor henüz.** Faz 3'te seed edilecek.
3. **UserTasinmazYetki sorguları çalışmaya devam etmeli.** Tablo zaten vardı, sadece FK eklendi.

---

## Tamamlandığında

- [ ] `PROGRESS.md` Faz 2 bölümünü işaretle
- [ ] `MASTER-PLAN.md` "Aktif Faz" → **Faz 3**
- [ ] `phase-3-domain-ef-migration.md` oku
