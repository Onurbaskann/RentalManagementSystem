# Faz 3 — Domain Servislerinin EF Core'a Taşınması

> **Hedef:** `DummyDataService` (in-memory) → EF Core servisleri. Mevcut UI ve davranış bozulmadan veri SQL Server'dan gelir.
> **Strateji:** Önce interface'leri tanımla, sonra implementasyonları yaz, en son Controller'ları interface'lere bağla.

**Referanslar:** `phase-2-sqlserver-migration.md` (DbContext yapısı)

---

## Görevler

### 3.1 — Servis interface'lerini tanımla
`Services/Interfaces/` klasörü oluştur:

- [ ] `ITasinmazService.cs`
  - `GetAllAsync(string? userId = null)` — userId verilirse Goruntuleyici filtresi uygulanır
  - `GetByIdAsync(int id)`
  - `CreateAsync(Tasinmaz t)`
  - `UpdateAsync(Tasinmaz t)`
  - `GetBosBirimlerAsync()`

- [ ] `IBirimService.cs`
  - `GetByTasinmazIdAsync(int tasinmazId)`
  - `GetByIdAsync(int id)`
  - `CreateAsync(Birim b)`
  - `UpdateAsync(Birim b)`

- [ ] `IKiraciService.cs`
  - `GetAllAsync(string? userId = null)`
  - `GetByIdAsync(int id)`
  - `CreateAsync(Kiraci k)`
  - `UpdateAsync(Kiraci k)`
  - `GenerateKiraciNoAsync()`

- [ ] `ISozlesmeService.cs`
  - `GetAllAsync(SozlesmeFiltresi filtre, string? userId = null)`
  - `GetByIdAsync(int id)`
  - `CreateAsync(KiraSozlesmesi s)`
  - `UzatAsync(int id, DateTime yeniBitis, decimal? yeniBedel, string? aciklama)`
  - `FeshetAsync(int id, DateTime fesihTarihi, string neden)`
  - `UpdateKdvTufeAsync(int id, decimal? tufeOrani, bool? kdvUygulanacak, decimal? kdvOrani)`

- [ ] `IIstatistikService.cs` (mevcut sınıfın interface'i)
  - Mevcut public metodları olduğu gibi yansıt
  - **Implementasyon EF Core sorgularıyla yeniden yazılacak**

### 3.2 — `SeedDataService` oluştur
- [ ] `Services/SeedDataService.cs` — Scoped
- [ ] `DummyDataService`'in constructor'ındaki seed verisini buraya taşı
- [ ] `Task SeedDomainDataAsync()` metodu:
  - DB boşsa 8 taşınmaz, birimler, 7 kiracı, 10 sözleşme oluştur
  - DB doluysa atla (idempotent)
- [ ] `Program.cs`'de uygulama başlangıcında çağır (sadece Development ortamında)

### 3.3 — Servis implementasyonları (EF Core)
- [ ] `Services/TasinmazService.cs`
  - `ITasinmazService` implementasyonu
  - `Include(t => t.Birimler)` ile eager load
  - `userId` verilirse `UserTasinmazYetki`'den filtreleme yap

- [ ] `Services/BirimService.cs`
  - Standart EF Core CRUD
  - `Include(b => b.Sozlesmeler)`

- [ ] `Services/KiraciService.cs`
  - Standart EF Core CRUD
  - `userId` filtresi: aktif sözleşmesi atanmış taşınmazlardan olanlar

- [ ] `Services/SozlesmeService.cs`
  - `Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)`
  - `Include(s => s.Kiraci)`
  - `Include(s => s.IslemGecmisi)`
  - `Uzat`, `Feshet` metotları `SozlesmeIslemGecmisi` kayıtlarını da oluşturur

- [ ] `Services/IstatistikService.cs` (mevcut sınıf güncelle)
  - `DummyDataService` bağımlılığını kaldır
  - Yerine `ApplicationDbContext` veya domain servislerini enjekte et
  - Hesaplama metotlarını koru ama veri kaynağını değiştir

### 3.4 — DI kayıtları
`Program.cs`'de:
- [ ] `services.AddScoped<ITasinmazService, TasinmazService>();`
- [ ] `services.AddScoped<IBirimService, BirimService>();`
- [ ] `services.AddScoped<IKiraciService, KiraciService>();`
- [ ] `services.AddScoped<ISozlesmeService, SozlesmeService>();`
- [ ] `services.AddScoped<IIstatistikService, IstatistikService>();`
- [ ] `services.AddScoped<SeedDataService>();`
- [ ] **`DummyDataService` kaydını KALDIR**

### 3.5 — Controller'ları güncelle
Her controller'ı interface'e bağla:
- [ ] `HomeController` → `IIstatistikService`, `ITasinmazService`, `UserTasinmazYetkiService`
- [ ] `TasinmazController` → `ITasinmazService`, `IBirimService`, `IIstatistikService`
- [ ] `KiraciController` → `IKiraciService`, `ISozlesmeService`
- [ ] `SozlesmeController` → `ISozlesmeService`, `IIstatistikService`, `IBirimService`, `IKiraciService`
- [ ] `AdminUserController` → `ITasinmazService` (DummyDataService yerine)
- [ ] **Tüm `DummyDataService` referanslarını çıkar**
- [ ] **Async/await kullan** — interface'ler tamamen async

### 3.6 — Async dönüşüm
- [ ] Controller action'ları `async Task<IActionResult>` olarak değiştir
- [ ] `View()` çağrısı öncesi `await` kullan
- [ ] Razor view'lar değişmiyor (model tipi aynı)

### 3.7 — `DummyDataService.cs` dosyasını sil
- [ ] **Sadece tüm controller'lar geçirildikten ve build başarılı olduktan sonra**
- [ ] Dosyayı sil (Git history'de zaten kalır)

### 3.8 — Test senaryoları
- [ ] Login: 3 rol için ayrı ayrı çalışıyor mu?
- [ ] Dashboard: KPI'lar doğru hesaplanıyor mu?
- [ ] Taşınmaz listesi: Admin tümünü, Goruntuleyici sadece atanmışları görüyor mu?
- [ ] Yeni taşınmaz ekle: DB'ye yazılıyor mu?
- [ ] Yeni sözleşme oluştur: `IslemGecmisi` kaydı oluşuyor mu?
- [ ] Sözleşme uzat/feshet: durum + tarih + geçmiş doğru mu?
- [ ] Kullanıcı yönetimi: Admin yeni Yonetici ekleyebiliyor mu?
- [ ] UserTasinmazYetki ataması çalışıyor mu?

---

## Risk Listesi

| Risk | Etki | Önlem |
|------|------|-------|
| `IstatistikService` DummyDataService'e bağımlı | Faz başında derleme hatası | Bu servisi son güncelle, önce interface tanımla |
| Controller'larda Singleton → Scoped geçişi | Lifetime mismatch | Önce interface kayıt, sonra constructor değiştir |
| Eager loading eksik | NullReferenceException view'da | `Include` zincirlerini view'larla eşleştir |
| Async olmayan EF çağrıları | Performance + warning | Tüm sorgular `ToListAsync`, `FirstOrDefaultAsync` |
| `KiraciNo` çakışması (paralel kayıt) | Unique constraint | Transaction içinde generate + retry |
| Seed her başlatmada çalışıyor | Duplicate veri | `if (await _ctx.Tasinmazlar.AnyAsync()) return;` |

---

## Çıktılar

- Yeni: 5 interface, 5 servis implementasyonu, `SeedDataService`
- Silinmiş: `DummyDataService.cs`
- Güncellenmiş: 6 Controller, `Program.cs`, `IstatistikService.cs`
- DB: Seed verisiyle dolu

---

## Tamamlandığında

- [ ] `PROGRESS.md` Faz 3 bölümünü işaretle
- [ ] `MASTER-PLAN.md` "Aktif Faz" → **Faz 4 (Permission Implementasyonu)**
- [ ] Faz 4: `phase-1-permission-model.md` Bölüm "Faz 4 İlerlemesi" başlığını incele (veya yeni `phase-4-permission-impl.md` oluştur)

---

## Sonraki Faz İpuçları (Faz 4)

Faz 4 kapsamı (henüz başlatma):
- `PermissionService` implementasyonunu tamamla
- Login'de claims yükleyen `PermissionClaimsTransformer` ekle
- `Program.cs`'de policy'leri kaydet
- Controller'lara `[Authorize(Policy = "...")]` ekle
- Admin UI'ında permission checkbox ekranı yap
