# Aşama C — Uygulama Planı

> **Üst karar dökümanı:** [refaktor-buyuk-mimari-sadelestirme.md](refaktor-buyuk-mimari-sadelestirme.md)
> **Kapsam:** Büyük cerrahi — 3 lookup tablosunun (`TasinmazTipi` + `KiraciKategori` + `Sektor`) tek `Kategori` tablosunda birleşimi.
> **Aşama süresi:** Tek oturum hedefi (büyük etki, dikkat gerekir).
> **Durum:** ⏳ Bekliyor.
>
> **Önemli:** Bu döküman yeni bir oturumda **tek başına** uygulanabilecek şekilde yazıldı. Önce mevcut kod tabanını taze gözle araştır, sonra adımları sırayla uygula. Karar #3 dışındaki kapsam genişletmesinden kaçın.

---

## 1. Hedef

Üç lookup tablosunu (`TasinmazTipi`, `KiraciKategori`, `Sektor`) ortak alanları nedeniyle tek bir `Kategori` tablosunda birleştirmek; `KategoriTipi` discriminator enum'u ile tip ayrımı yapmak. `BorcTipi` ve `BirimTuru` **birleşime dahil değildir, aynen kalır.**

**Neden:** 3 tablonun da aynı alanları (`Ad`, `Kod`, `Aktif`, `Sira`, `OlusturmaTarihi`) tutması yapısal duplikasyon. Sadece `TasinmazTipi`'nin `TekParcaDestekli` + `BirimBazliDestekli` bayrakları farklı.

---

## 2. Veri Modeli

### Yeni entity (`Models/Kategori.cs`)

```csharp
public class Kategori
{
    public int Id { get; set; }
    public KategoriTipi Tipi { get; set; }       // discriminator
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    // Yalnızca KategoriTipi.Tasinmaz için anlamlı:
    public bool TekParcaDestekli { get; set; }
    public bool BirimBazliDestekli { get; set; }
}

public enum KategoriTipi
{
    Tasinmaz = 1,
    Kiraci = 2,
    Sektor = 3
}
```

### EF Core konfigürasyonu (`ApplicationDbContext.OnModelCreating`)

- `HasIndex(k => new { k.Tipi, k.Kod }).IsUnique()` — tip içinde Kod benzersiz
- `Property(k => k.Ad).HasMaxLength(150)`
- `Property(k => k.Kod).HasMaxLength(50)`
- `Property(k => k.Tipi).HasComment("Tasinmaz=1, Kiraci=2, Sektor=3")`

---

## 3. FK İsim Stratejisi

Yeni session'da uygulayan kişiye karar: **FK kolon adlarını koru, sadece target tabloyu değiştir.** Bu daha az dosyaya dokunmaktır.

| Tablo | Mevcut FK | Yeni FK Target | Filtre |
|---|---|---|---|
| `Tasinmaz` | `TasinmazTipiId` | `Kategori.Id` | `Tipi=Tasinmaz` |
| `Kiraci` | `KiraciKategoriId` | `Kategori.Id` | `Tipi=Kiraci` |
| `Kiraci` | `SektorId` | `Kategori.Id` | `Tipi=Sektor` |
| `BirimRate` | `KiraciKategoriId` | `Kategori.Id` | `Tipi=Kiraci` |
| `Tarife` (TarifeKalemi) | `KiraciKategoriId` | `Kategori.Id` | `Tipi=Kiraci` |
| `TasinmazKiraciKategoriFiyat` | `KiraciKategoriId` | `Kategori.Id` | `Tipi=Kiraci` |

**Navigation property'ler:** `Tasinmaz.TasinmazTipi` → `Tasinmaz.Kategori`, `Kiraci.KiraciKategori` → `Kiraci.Kategori`, `Kiraci.Sektor` → `Kiraci.SektorKategori` (yeni isim — `Kategori` tipi olduğu için aynı tip altında iki nav).

---

## 4. UI Stratejisi

**Karar:** 3 ayrı admin ekranı (`AdminTasinmazTipi`, `AdminKiraciKategori`, `AdminSektor`) **korunur**. Her controller kendi `KategoriTipi` filtresiyle Kategori tablosuna yazar/okur. Kullanıcı deneyimi değişmez; sadece backend tek tabloya yazar.

Bu, scope'u dar tutar — yeni `AdminKategori` ekranı, menü değişikliği vb. bu refaktöre dahil değildir.

---

## 5. Uygulama Adımları

### Önkoşul
1. `git status` temiz olmalı (önceki commit'ler yansımış olmalı)
2. `dotnet build` → 0 hata
3. Aşama B tamamlandığı doğrulanmalı (`PROGRESS.md` B3 ✅)

### Adım 1 — Yeni entity + enum
1. `Models/Kategori.cs` oluştur (yukarıdaki sınıf + enum)
2. `Data/ApplicationDbContext.cs`:
   - `DbSet<Kategori> Kategoriler` ekle
   - `OnModelCreating`'e Kategori konfigürasyonu (unique index, MaxLength, HasComment)

### Adım 2 — Migration (yeni tablo + veri taşıma + FK switch + drop)

**Migration adı:** `C1_KategoriBirlestirme`

Migration'ı **iki dosya halinde değil tek migration** üret, ama içinde sıralı raw SQL adımları kullan:

1. `CreateTable("Kategoriler")` — yeni şema
2. Veri kopyala (raw SQL):
   ```sql
   -- TasinmazTipi → Kategori
   INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
   SELECT 1, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli FROM TasinmazTipleri;

   -- KiraciKategori → Kategori
   INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
   SELECT 2, Ad, Kod, Aktif, Sira, OlusturmaTarihi, 0, 0 FROM KiraciKategorileri;

   -- Sektor → Kategori
   INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
   SELECT 3, Ad, Kod, Aktif, Sira, OlusturmaTarihi, 0, 0 FROM Sektorler;
   ```
3. **Id eşleme:** Eski tablonun `Id` değeri ile yeni `Kategori.Id` farklı olacak (auto-increment). FK'ları güncellemek için lookup gerekir:
   ```sql
   -- Örnek: Tasinmaz.TasinmazTipiId güncelle
   UPDATE t SET t.TasinmazTipiId = k.Id
   FROM Tasinmazlar t
   INNER JOIN TasinmazTipleri eski ON eski.Id = t.TasinmazTipiId
   INNER JOIN Kategoriler k ON k.Tipi = 1 AND k.Kod = eski.Kod;
   -- Aynı kalıp Kiraci.KiraciKategoriId, Kiraci.SektorId, BirimRate.KiraciKategoriId,
   -- TarifeKalemleri.KiraciKategoriId, TasinmazKiraciKategoriFiyatlari.KiraciKategoriId için
   ```
   **NOT:** Bu çalışsın diye her 3 tabloda `Kod` benzersiz olmalı (zaten öyle). Onay için seed verisini önce kontrol et.

4. Eski FK constraint'lerini drop, yenilerini ekle (Kategoriler tablosuna)
5. `DropTable("TasinmazTipleri")`, `DropTable("KiraciKategorileri")`, `DropTable("Sektorler")`

**Migration üretim taktiği:** EF Core `dotnet ef migrations add C1_KategoriBirlestirme` ile başla, ardından üretilen migration dosyasını yukarıdaki SQL adımlarıyla manuel zenginleştir (EF Core kendi başına veri taşıma + FK switch üretemez).

### Adım 3 — Eski entity'leri kaldır
1. `Models/TasinmazTipi.cs` → namespace + yorum, sınıf silinir (Aşama B'deki `RezervasyonUcretKural` pattern'i)
2. `Models/KiraciKategori.cs` → aynı
3. `Models/Sektor.cs` → aynı
4. `Data/ApplicationDbContext.cs` — 3 DbSet silinir + ilgili `OnModelCreating` blokları silinir

### Adım 4 — Navigation property güncellemeleri
- `Models/Tasinmaz.cs` — `public TasinmazTipi? TasinmazTipi` → `public Kategori? Kategori`
- `Models/Kiraci.cs` — `KiraciKategori`, `Sektor` → `Kategori`, `SektorKategori`
- `Models/BirimRate.cs` — `KiraciKategori` → `Kategori`
- `Models/Tarife.cs` (TarifeKalemi) — `KiraciKategori` → `Kategori`
- `Models/TasinmazKiraciKategoriFiyat.cs` — `KiraciKategori` → `Kategori`

### Adım 5 — Controller / Servis grep + güncelleme

Aşağıdakileri grep ile bul ve `Kategori` (Tipi filtreli) ile değiştir:
- `_ctx.TasinmazTipleri` → `_ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tasinmaz)`
- `_ctx.KiraciKategorileri` → `_ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Kiraci)`
- `_ctx.Sektorler` → `_ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor)`

**Etkilenen controller'lar (önceden tespit edildi):**
- `AdminTasinmazTipiController.cs` — tüm CRUD, ekleme/güncellemede `Tipi = KategoriTipi.Tasinmaz` set et
- `AdminKiraciKategoriController.cs` — aynı (`Tipi = KategoriTipi.Kiraci`)
- `AdminSektorController.cs` — aynı (`Tipi = KategoriTipi.Sektor`)
- `TasinmazController.cs`, `BirimController.cs`, `SozlesmeController.cs`, `KiraciController.cs`, `AdminTarifeController.cs`, `HomeController.cs`

**Include zincirleri:**
- `.Include(t => t.TasinmazTipi)` → `.Include(t => t.Kategori)`
- `.Include(k => k.KiraciKategori)` → `.Include(k => k.Kategori)`
- `.Include(k => k.Sektor)` → `.Include(k => k.SektorKategori)`

### Adım 6 — ViewModel güncellemeleri

Önceden tespit edilen dosyalar:
- `Models/ViewModels/BirimRateViewModels.cs` (2 yer)
- `Models/ViewModels/KiraciViewModels.cs` (2 yer)
- `Models/ViewModels/TarifeViewModels.cs` (2 yer)
- `Models/ViewModels/TasinmazViewModels.cs` (1 yer)
- `Models/ViewModels/TasinmazFiyatMatrisiViewModel.cs` (2 yer)

ViewModel `int` property adları (`TasinmazTipiId`, `KiraciKategoriId`, `SektorId`) **DEĞİŞMEZ** (UI formları aynı kalır); sadece dropdown doldurma kaynağı `Kategori` filtreli olur.

### Adım 7 — View güncellemeleri
- `Views/AdminTasinmazTipi/*.cshtml` — `@model` tipi `Kategori`'ye değişir (veya `TasinmazTipiViewModel` projeksiyon)
- `Views/AdminKiraciKategori/*.cshtml` — aynı
- `Views/AdminSektor/*.cshtml` — aynı
- Diğer view'larda `@item.TasinmazTipi.Ad`, `@kiraci.KiraciKategori?.Ad`, `@kiraci.Sektor?.Ad` referansları `Kategori`/`SektorKategori`'ye dönüşür

### Adım 8 — Seed servisi
`Services/SeedDataService.cs` içindeki:
- `SeedTasinmazTipleriAsync`
- `SeedKiraciKategorileriAsync`
- `SeedSektorlerAsync`

Hepsi `Kategoriler` DbSet'ine yazacak şekilde güncellenir; `Tipi` set edilir.

### Adım 9 — Build + Migration + Smoke Test
1. `dotnet build` → 0 hata (warning kabul edilebilir)
2. `dotnet ef database update` → başarılı
3. Manuel smoke test:
   - Admin Taşınmaz Tipleri sayfasında liste açılıyor, yeni ekleme + düzenleme çalışıyor
   - Admin Kiracı Kategorileri aynı
   - Admin Sektörler aynı
   - Taşınmaz Ekle dropdown'larda taşınmaz tipi listesi geliyor
   - Kiracı Ekle dropdown'larda kategori + sektör listesi geliyor
   - Sözleşme ekleme + tahakkuk üretimi çalışıyor (RateResolver Kategori üzerinden)
   - Var olan Taşınmaz/Kiracı detay sayfaları doğru bilgi gösteriyor

### Adım 10 — PROGRESS.md güncelle
```
### Aşama C — Büyük cerrahi (Aşama B bittikten sonra)
- [x] C1 — `Kategori` lookup birleşimi (Karar #3)
```

`MASTER-PLAN.md` Aktif Faz satırını "Faz 14 tamamlandı" olarak güncelle.

---

## 6. Riskler ve Dikkat Noktaları

1. **Veri kaybı riski:** Migration veri taşımayı raw SQL ile yapıyor. Mutlaka migration dosyasını uygulanmadan önce gözden geçir.
2. **FK constraint cycle:** Eski tablolar drop edilmeden FK'ları yeni tabloya yönlendirilmeli, yoksa drop edilemez. EF Core üretilen migration'da bu sırayı manuel düzelt.
3. **Dummy veri:** Üretim verisi yok, ama yine de yedek al (`BACKUP DATABASE`).
4. **`Kod` benzersizliği:** Veri taşımada `Kod` ile eşleştirme yapılıyor — her 3 eski tabloda Kod benzersizdir, doğrulamak için önce sorgu çalıştır:
   ```sql
   SELECT Kod, COUNT(*) FROM TasinmazTipleri GROUP BY Kod HAVING COUNT(*) > 1;
   -- Aynısı KiraciKategorileri ve Sektorler için
   ```
5. **`TasinmazKategoriCarpan` muhtemel kalıntı:** Faz 9'da kaldırıldı ama grep ile kontrol et — varsa migration'a dahil etme, sadece silinen entity'lere odaklan.
6. **`TasinmazTipiKiralamaSekli`:** Aşama A4'te kaldırıldı; bu refaktöre dahil değil.

---

## 7. Doğrulama Kriterleri

- [ ] `dotnet build` → 0 hata
- [ ] `dotnet ef database update` → başarılı
- [ ] DB'de `Kategoriler` tablosu dolu (3 tip × eski kayıt sayısı toplamı)
- [ ] DB'de `TasinmazTipleri`, `KiraciKategorileri`, `Sektorler` tabloları yok
- [ ] Tüm FK'lar `Kategoriler.Id`'ye işaret ediyor (SSMS ile doğrula)
- [ ] 9 controller smoke test geçti (yukarıdaki Adım 9 listesi)
- [ ] Tahakkuk üretimi mevcut sözleşmelerle aynı sonucu veriyor (regresyon)

---

## 8. Aşama C Sonrası

- `MASTER-PLAN.md` → "Faz 14: Tamamlandı ✅"
- `PROGRESS.md` → Aşama C ✅, Aktif Faz bir sonraki fazaya geçer
- Karar dökümanı (`refaktor-buyuk-mimari-sadelestirme.md`) ek not: "11 karar uygulandı."
