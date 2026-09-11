# CLAUDE.md — KiraTakip AI Asistan Kuralları

> Bu dosya her session başında **otomatik** yüklenir. Token tasarrufu için
> kısa tutulmuştur. Detaylar `docs/` altındadır.

## Proje Özeti

KiraTakip — taşınmaz/birim kira ve rezervasyon takip uygulaması.
**Stack:** .NET 9.0 MVC · EF Core (SQL Server) · ASP.NET Identity + Claims
permission · Tailwind CSS · Alpine.js · xUnit (test projesi .NET 10.0).
**Aktif faz:** Her session başında `docs/PROGRESS.md` dosyasını oku — `Aktif faz:` satırı güncel fazı belirler.

## Doküman Haritası (önce buraya bak, sonra dosya aç)

| Dosya | Boyut | Ne zaman okunur |
|---|---|---|
| `docs/PROGRESS.md` | küçük | Her oturumda oku; aktif çalışma ve fazı buradan belirle |
| `docs/PROGRESS-HISTORY.md` | arşiv | Yalnız tarihsel bir karar gerektiğinde ilgili bölümü kısmi oku; tamamını yükleme |
| `docs/MASTER-PLAN.md` | büyük | Sadece aktif faz satırı + ilgili bölüm (`offset`/`limit` ile) |
| `docs/phase-N-*.md` | büyük | **Sadece** aktif faza ait olanı yükle |
| `docs/refactor-*.md` | büyük | Sadece üzerinde çalışılan refactor için |
| `docs/permission-catalog.md` | orta | Sadece yetki/permission görevlerinde |
| `docs/auth-spec.md`, `bina-ofis-birim-spec.md`, `kiraci-sozlesme-finans-spec.md`, `project-spec-canonical.md` | çok büyük (570–1248 satır) | Sadece o domain'de değişiklik varsa **kısmi** oku |

## Token / Context Kuralları

- `PROGRESS.md` kısa aktif kayıttır; oturum başında okunabilir.
- Arşiv `PROGRESS-HISTORY.md` dosyasını **limit'siz okuma**; yalnız tarihsel araştırmada ilgili bölümünü aç.
- `MASTER-PLAN.md` ve `phase-*.md` dosyalarını **limit'siz `Read` ile çekme**.
  Önce başlıkları tara (ilk ~120 satır), sonra ilgili bölümü `offset`+`limit` ile oku.
- **Aktif faz dışındaki** `phase-N-*.md` ve `refactor-*.md` dosyalarını "context için" açma.
- Aynı dosyayı bir session'da iki kez okuma.
- Kod araması için **`Glob` ve `Grep`** kullan; klasör tarama / `ls -R` yapma.
- `bin/`, `obj/`, `_Migrations.SQLite.archive/` klasörlerini görmezden gel
  (arşiv ve build çıktıları).
- Cevaplar kısa; dosya referansı `[path:line](path#Lline)` formatında.
- Tamamlanan aktif fazı `PROGRESS.md` dosyasında işaretle; ayrıntıyı ilgili plan dosyasında tut.

## Güncellik Önceliği

Dokümanlar çakıştığında bu sıra geçerlidir (üstteki daha güncel):

1. `CLAUDE.md` (bu dosya) — anlık kurallar ve mimari sabitler
2. `docs/PROGRESS.md` — aktif çalışma ve faz
3. Aktif çalışmanın detaylı plan/spec dosyası — güncel uygulama kararları
4. `docs/MASTER-PLAN.md` — genel faz haritası ve mimari kararlar özeti
5. `docs/PROGRESS-HISTORY.md` — tamamlanmış çalışmaların tarihsel arşivi
6. `docs/phase-N-*.md` / `docs/refactor-*.md` — diğer uygulama detayları
7. `docs/project-spec-canonical.md`, `auth-spec.md` vb. — başlangıç vizyonu / tarihsel kararlar

`docs/project-spec-canonical.md` ve diğer büyük spec dosyaları **tarihsel** içerik taşıyabilir; güncel mimari kararlar için yukarıdaki kaynaklara bak.

## Veri Durumu

Proje şu an **geliştirme/dummy veri** aşamasındadır. Sistemde gerçek üretim verisi yoktur. Tablo, kolon, migration ve model sadeleştirme kararlarında geriye dönük üretim verisi koruma kaygısı öncelikli değildir; ancak migration etkileri ve dokümantasyon değişiklikleri net yazılmalıdır.

## Komutlar (doğrulanmış)

Çözüm dosyası: `KiraTakip.slnx`

```powershell
# Build (çözüm)
dotnet build KiraTakip.slnx

# Çalıştır
dotnet run --project src/KiraTakip.Web/KiraTakip.Web.csproj

# Testler
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj

# EF migration ekle
dotnet ef migrations add <Name> --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj

# DB güncelle
dotnet ef database update --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj

# Migration geri al (son migration'a kadar)
dotnet ef database update <PreviousMigrationName> --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
```

**Not:** Uygulama lokalde çalışırken `dotnet build` MSB dosya kilidi hatası verir;
CS derleme hatası yoksa bu normaldir, çıktıyı buna göre yorumla.

## Dil ve Stil

- Kullanıcıyla **Türkçe** konuş.
- Açıklama yerine eylem; gerekçe sadece sorulduğunda.
- Kod yorumlarını gereksiz ekleme (`CLAUDE` ana kuralı geçerli).
- Cevaplar kısa, başlık ve madde spam'i yapma.

## Yapma Listesi

- "Sadece okumak için" `dotnet build` / `dotnet run` koşma.
- `dotnet ef` komutlarını `--project` / `--startup-project` bayrakları olmadan deneme.
- `docs/` altındaki büyük spec dosyalarını tamamen `Read` ile çekme.
- Aktif faz dışındaki phase dosyalarını "context için" yükleme.
- Magic string yazma — `Authorization/RoleNames.cs`, `AppClaimTypes.cs`,
  `PermissionCatalog` sabitlerini kullan.
- `_Migrations.SQLite.archive/` içine dokunma (arşiv).

## Mimari Sabitler (hızlı referans)

- **DbContext:** `KiraTakip/Data/ApplicationDbContext.cs` (IdentityDbContext)
- **Roller:** SistemYonetici · KiraciYoneticisi
- **Yetki katmanları:** Rol → Permission (`Property.Create` vb.) → Kapsam (`KullaniciYetkiKapsami` / `AccessScope` row-level)
- **Connection string:** `appsettings.json` → `DefaultConnection` (SQL Server)
- **Seed:** `Infrastructure/Seeding/SeedDataService.cs` (DummyDataService Phase 3'te kaldırıldı)
