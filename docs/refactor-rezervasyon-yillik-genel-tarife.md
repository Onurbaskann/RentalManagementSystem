# Refactor — Rezervasyon Yıllık Genel Tarifeleri

> Amaç: Tek global `RezervasyonUcretKural` yerine, `Tarife.Yil` parent'ı altında her aktif **rezervasyon yapılabilir** BirimTuru için yıllık genel rezervasyon tarifesi tutmak. Birime özel kural override olarak korunur.

## Kapsam Kararları

| Madde | Karar |
|---|---|
| Yeni entity `RezervasyonGenelTarife` | ✅ Eklenir |
| `RezervasyonUcretKural`'a `TarifeId`/`BirimTuruId` eklenmesi | ❌ YOK — discriminator antipattern |
| `RezervasyonUcretKural` (BirimId zorunlu) | ✅ Aynen korunur (sadece birime özel override) |
| Admin/Tarife Detay'a ikinci tablo | ✅ — aynı sayfa, ayrı bölüm |
| Ayrı admin ekranı | ❌ YOK |
| `Tasinmaz/Ekle` parent kart | ✅ — ayrı VM + ayrı partial |
| `HesaplaAsync` precedence değişikliği | ✅ Birim → Birim.BirimTuru cari yıl → hata |
| Eski global `RezervasyonUcretKural` (BirimId=null) | ✅ Backfill ile dönüştür, sonra sil |
| YilEkle kopyalama | ✅ Rezervasyon kayıtları da kopyalanır |
| Migration | ✅ `AddRezervasyonGenelTarife` |

**Tek doğruluk kaynağı:** `RezervasyonGenelTarife (TarifeId × BirimTuruId)` = yıllık genel; `RezervasyonUcretKural (BirimId)` = birime özel override.

---

## 1. Data Model

### Yeni: `KiraTakip/Models/RezervasyonGenelTarife.cs`

```csharp
namespace KiraTakip.Models;

public class RezervasyonGenelTarife
{
    public int Id { get; set; }
    public int TarifeId { get; set; }
    public int BirimTuruId { get; set; }

    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public Tarife Tarife { get; set; } = null!;
    public BirimTuru BirimTuru { get; set; } = null!;
}
```

### [Data/ApplicationDbContext.cs](KiraTakip/Data/ApplicationDbContext.cs)

```csharp
public DbSet<RezervasyonGenelTarife> RezervasyonGenelTarifeleri => Set<RezervasyonGenelTarife>();
```

`OnModelCreating`:
```csharp
modelBuilder.Entity<RezervasyonGenelTarife>(e =>
{
    e.HasIndex(r => new { r.TarifeId, r.BirimTuruId }).IsUnique();
    e.Property(r => r.PeriyotUcreti).HasPrecision(18, 2);
    e.Property(r => r.KdvOrani).HasPrecision(5, 2);
    e.HasOne(r => r.Tarife).WithMany().HasForeignKey(r => r.TarifeId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(r => r.BirimTuru).WithMany().HasForeignKey(r => r.BirimTuruId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

Migration: `AddRezervasyonGenelTarife`

---

## 2. RezervasyonService — Precedence

[KiraTakip/Services/RezervasyonService.cs:60-72](KiraTakip/Services/RezervasyonService.cs#L60-L72) `HesaplaAsync` güncellemesi:

```csharp
// 1) Birime özel kural
var kural = await _ctx.RezervasyonUcretKurallari
    .Where(k => k.Aktif && k.BirimId == birimId)
    .FirstOrDefaultAsync();

if (kural != null)
{
    // mevcut hesap mantığı — kural.UcretsizSureDakika vs. kullanılır
}
else
{
    // 2) Birim.BirimTuru cari yıl genel tarifesi
    var birim = await _ctx.Birimler
        .Include(b => b.BirimTuru)
        .FirstOrDefaultAsync(b => b.Id == birimId);

    if (birim?.BirimTuruId is not int btId)
    {
        sonuc.HataMessaji = "Birim türü tanımlanmamış.";
        return sonuc;
    }

    int cariYil = baslangic.Year;
    var genel = await _ctx.RezervasyonGenelTarifeleri
        .Include(g => g.Tarife)
        .Where(g => g.BirimTuruId == btId && g.Tarife.Aktif && g.Tarife.Yil == cariYil)
        .FirstOrDefaultAsync();

    if (genel == null)
    {
        sonuc.HataMessaji = $"{cariYil} yılı için '{birim.BirimTuru?.Ad}' türünde rezervasyon tarifesi tanımlı değil.";
        return sonuc;
    }

    // genel.UcretsizSureDakika / UcretlendirmePeriyoduDakika / PeriyotUcreti / KdvOrani kullanılır
}
```

**Önemli:** `kural` (birime özel) ve `genel` (BirimTuru bazlı) için aynı formül blokunu kullan; lokal değişkenlere atayarak (`int ucretsiz, int periyot, decimal ucret, decimal kdv`) tek hesap akışı kalsın.

`baslangic.Year` → cari yıl seçimi rezervasyonun başladığı yıla göre.

---

## 3. AdminTarifeController

[KiraTakip/Controllers/AdminTarifeController.cs](KiraTakip/Controllers/AdminTarifeController.cs)

### Detay GET (satır 31)

Mevcut yüklemelere ek:
```csharp
var rezervasyonBirimTurleri = await _ctx.BirimTurleri
    .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
    .OrderBy(t => t.Sira)
    .ToListAsync();

var mevcutRezervasyonlar = await _ctx.RezervasyonGenelTarifeleri
    .Where(r => r.TarifeId == tarife.Id)
    .ToListAsync();

vm.RezervasyonSatirlari = rezervasyonBirimTurleri.Select(bt =>
{
    var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.BirimTuruId == bt.Id);
    return new TarifeMatrisRezervasyonSatir
    {
        RezervasyonGenelTarifeId    = mevcut?.Id ?? 0,
        BirimTuruId                 = bt.Id,
        BirimTuruAd                 = bt.Ad,
        UcretsizSureDakika          = mevcut?.UcretsizSureDakika ?? 0,
        UcretlendirmePeriyoduDakika = mevcut?.UcretlendirmePeriyoduDakika ?? 60,
        PeriyotUcreti               = mevcut?.PeriyotUcreti ?? 0,
        KdvOrani                    = mevcut?.KdvOrani ?? 20
    };
}).ToList();
```

### KalemGuncelle POST (satır 88)

Mevcut `vm.Hucreler` döngüsünden sonra:
```csharp
foreach (var rez in vm.RezervasyonHucreler)
{
    var mevcut = await _ctx.RezervasyonGenelTarifeleri
        .FirstOrDefaultAsync(r => r.TarifeId == tarife.Id && r.BirimTuruId == rez.BirimTuruId);

    if (mevcut == null)
    {
        _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
        {
            TarifeId                    = tarife.Id,
            BirimTuruId                 = rez.BirimTuruId,
            UcretsizSureDakika          = rez.UcretsizSureDakika,
            UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
            PeriyotUcreti               = rez.PeriyotUcreti,
            KdvOrani                    = rez.KdvOrani,
            OlusturmaTarihi             = DateTime.Now
        });
    }
    else
    {
        mevcut.UcretsizSureDakika          = rez.UcretsizSureDakika;
        mevcut.UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika;
        mevcut.PeriyotUcreti               = rez.PeriyotUcreti;
        mevcut.KdvOrani                    = rez.KdvOrani;
    }
}
```

### YilEkle POST (satır 167-187)

Mevcut `kaynak.Kalemler` kopyalamasının yanına:
```csharp
var kaynakRezervasyonlar = await _ctx.RezervasyonGenelTarifeleri
    .Where(r => r.TarifeId == kaynak.Id).ToListAsync();
// ... yeniTarife.Id SaveChangesAsync sonrası bilinir; iki SaveChanges veya
// transient liste tutup sonra ekle. (Kaynak yoksa BirimTuru taraması ile default değerler oluştur.)
```

Boş yıl (kaynak yok) senaryosunda: cari aktif `BirimTuru (RezYap=true)` listesi için varsayılan 0₺ / 60dk / 0% / 0dk satırları üret.

---

## 4. ViewModels

[KiraTakip/Models/ViewModels/TarifeViewModels.cs](KiraTakip/Models/ViewModels/TarifeViewModels.cs)

```csharp
public class TarifeMatrisRezervasyonSatir
{
    public int RezervasyonGenelTarifeId { get; set; }
    public int BirimTuruId { get; set; }
    public string BirimTuruAd { get; set; } = "";
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
}
```

- `TarifeMatrisViewModel.RezervasyonSatirlari : List<TarifeMatrisRezervasyonSatir>`
- `TarifeMatrisPostViewModel.RezervasyonHucreler : List<TarifeMatrisRezervasyonSatir>`

### `Models/ViewModels/ParentRezervasyonTarifeKartViewModel.cs`

```csharp
public class ParentRezervasyonTarifeKartViewModel
{
    public string KaynakAdi { get; set; } = "";
    public string? Aciklama { get; set; }
    public List<ParentRezervasyonTarifeSatir> Satirlar { get; set; } = [];
}

public class ParentRezervasyonTarifeSatir
{
    public string BirimTuruAd { get; set; } = "";
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
}
```

---

## 5. Views

### [Views/AdminTarife/Detay.cshtml](KiraTakip/Views/AdminTarife/Detay.cshtml)

Kira matrisi formundan sonra **aynı form içinde** ikinci tablo:
- Başlık: "Rezervasyon Genel Tarifeleri"
- Sütunlar: Alan Türü, Ücretsiz Süre (dk), Periyot (dk), Periyot Ücreti (₺), KDV (%)
- Her satır: hidden `BirimTuruId` + 4 input.
- Form name'leri: `RezervasyonHucreler[i].BirimTuruId` vb. (model binder otomatik bağlar)

Boş liste durumu: "Aktif rezervasyon yapılabilir birim türü yok" mesajı.

### Yeni: `Views/Shared/_ParentRezervasyonTarifeKart.cshtml`

Mevcut `_ParentTarifeKart.cshtml` pattern'i — başlık + tablo + boş durum mesajı. Model: `ParentRezervasyonTarifeKartViewModel`.

### [Views/Tasinmaz/Ekle.cshtml](KiraTakip/Views/Tasinmaz/Ekle.cshtml)

"Rezervasyon Alanları" bölümünün **üstüne** partial render:
```cshtml
@if (Model.ParentRezervasyonTarife != null)
{
    <partial name="_ParentRezervasyonTarifeKart" model="Model.ParentRezervasyonTarife" />
}
```

---

## 6. TarifeHiyerarsiService

[KiraTakip/Services/Interfaces/ITarifeHiyerarsiService.cs](KiraTakip/Services/Interfaces/ITarifeHiyerarsiService.cs) ve [TarifeHiyerarsiService.cs](KiraTakip/Services/TarifeHiyerarsiService.cs)

**Yeni method** (mevcut `GetParentForAsync`'i değiştirme):

```csharp
Task<ParentRezervasyonTarifeKartViewModel?> GetRezervasyonParentForAsync(int? yil = null);
```

Implementasyon:
```csharp
int hedefYil = yil ?? DateTime.Now.Year;
var tarife = await _ctx.Tarifeler
    .FirstOrDefaultAsync(t => t.Aktif && t.Yil == hedefYil);

if (tarife == null)
    return new ParentRezervasyonTarifeKartViewModel
    {
        KaynakAdi = $"Rezervasyon Tarifesi - {hedefYil}",
        Satirlar  = []
    };

var satirlar = await _ctx.RezervasyonGenelTarifeleri
    .Include(r => r.BirimTuru)
    .Where(r => r.TarifeId == tarife.Id && r.BirimTuru.Aktif)
    .OrderBy(r => r.BirimTuru.Sira)
    .Select(r => new ParentRezervasyonTarifeSatir
    {
        BirimTuruAd                 = r.BirimTuru.Ad,
        UcretsizSureDakika          = r.UcretsizSureDakika,
        UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
        PeriyotUcreti               = r.PeriyotUcreti,
        KdvOrani                    = r.KdvOrani
    })
    .ToListAsync();

return new ParentRezervasyonTarifeKartViewModel
{
    KaynakAdi = $"Rezervasyon Tarifesi - {hedefYil}",
    Aciklama  = tarife.Aciklama,
    Satirlar  = satirlar
};
```

---

## 7. TasinmazController + ViewModel

[KiraTakip/Models/ViewModels/TasinmazViewModels.cs](KiraTakip/Models/ViewModels/TasinmazViewModels.cs):

```csharp
public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
```

[KiraTakip/Controllers/TasinmazController.cs](KiraTakip/Controllers/TasinmazController.cs) — `Ekle` GET:
```csharp
vm.ParentRezervasyonTarife = await _tarifeHiyerarsi.GetRezervasyonParentForAsync();
```

DI'da `ITarifeHiyerarsiService` zaten enjekte ediliyorsa ek bir şey yok; yoksa constructor'a ekle.

---

## 8. SeedDataService

[KiraTakip/Services/SeedDataService.cs](KiraTakip/Services/SeedDataService.cs)

### 8.1 `EnsureVarsayilanRezervasyonUcretKuralAsync` (satır 49-64)

**No-op'a çevir** veya tamamen kaldır. Çağrı yerini `Program.cs`'te kontrol et — `EnsureVarsayilanRezervasyonGenelTarifeAsync` ile değiştir.

### 8.2 Yeni: `EnsureVarsayilanRezervasyonGenelTarifeAsync`

```csharp
public async Task EnsureVarsayilanRezervasyonGenelTarifeAsync()
{
    var cariYil = DateTime.Now.Year;
    var tarife = await _ctx.Tarifeler
        .FirstOrDefaultAsync(t => t.Yil == cariYil);
    if (tarife == null) return; // SeedTarifelerAsync henüz çalışmamış

    var rezBirimTurleri = await _ctx.BirimTurleri
        .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
        .ToListAsync();
    if (!rezBirimTurleri.Any()) return;

    var mevcut = await _ctx.RezervasyonGenelTarifeleri
        .Where(r => r.TarifeId == tarife.Id)
        .Select(r => r.BirimTuruId)
        .ToListAsync();

    foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
    {
        _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
        {
            TarifeId                    = tarife.Id,
            BirimTuruId                 = bt.Id,
            UcretsizSureDakika          = 120,
            UcretlendirmePeriyoduDakika = 60,
            PeriyotUcreti               = 500m,
            KdvOrani                    = 20m,
            Aciklama                    = $"{cariYil} varsayılan — {bt.Ad}",
            OlusturmaTarihi             = DateTime.UtcNow
        });
    }
    await _ctx.SaveChangesAsync();
}
```

### 8.3 Backfill — Eski global `RezervasyonUcretKural`

Tek seferlik, idempotent:
```csharp
public async Task BackfillEskiGlobalRezervasyonKuralAsync()
{
    var eskiGlobal = await _ctx.RezervasyonUcretKurallari
        .FirstOrDefaultAsync(k => k.BirimId == null);
    if (eskiGlobal == null) return;

    var cariYil = DateTime.Now.Year;
    var tarife = await _ctx.Tarifeler.FirstOrDefaultAsync(t => t.Yil == cariYil);
    if (tarife == null) return;

    var rezBirimTurleri = await _ctx.BirimTurleri
        .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
        .ToListAsync();

    var mevcut = await _ctx.RezervasyonGenelTarifeleri
        .Where(r => r.TarifeId == tarife.Id)
        .Select(r => r.BirimTuruId)
        .ToListAsync();

    foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
    {
        _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
        {
            TarifeId                    = tarife.Id,
            BirimTuruId                 = bt.Id,
            UcretsizSureDakika          = eskiGlobal.UcretsizSureDakika,
            UcretlendirmePeriyoduDakika = eskiGlobal.UcretlendirmePeriyoduDakika,
            PeriyotUcreti               = eskiGlobal.PeriyotUcreti,
            KdvOrani                    = eskiGlobal.KdvOrani,
            Aciklama                    = $"Backfill: eski global kural — {bt.Ad}",
            OlusturmaTarihi             = DateTime.UtcNow
        });
    }
    await _ctx.SaveChangesAsync();

    // Eski global kuralı sil
    _ctx.RezervasyonUcretKurallari.Remove(eskiGlobal);
    await _ctx.SaveChangesAsync();
}
```

### 8.4 Program.cs çağrı sırası

Mevcut sıraya ek:
```
SeedBorcTipleriAsync
→ SeedBirimTurleriAsync
→ SeedKiraciKategorileriAsync
→ SeedSektorlerAsync
→ SeedTarifelerAsync                        // Tarife.Yil oluşur
→ BackfillEskiGlobalRezervasyonKuralAsync   // tek seferlik dönüştür+sil
→ EnsureVarsayilanRezervasyonGenelTarifeAsync   // eksik BirimTuru satırlarını ekle
```

`EnsureVarsayilanRezervasyonUcretKuralAsync` çağrısı **kaldırılır** (artık no-op).

**Dikkat:**
- Backfill önce çalışmalı — yoksa default değerler insert edilir, eski global'in değerleri kaybolur.
- Backfill idempotent: eski global yok → no-op. İkinci kez çalışırsa `eskiGlobal == null` → no-op.
- Birime özel `RezervasyonUcretKural` (BirimId != null) kayıtlarına **dokunulmaz** — override olarak aynen kalır.

---

## 9. Migration ve Çalıştırma Sırası

1. `dotnet ef migrations add AddRezervasyonGenelTarife`
2. `dotnet ef database update`
3. Uygulama açılışı: Seed sırası (yukarıdaki bölüm 8.4) çalışır; backfill eski global'i dönüştürür, eksik default satırları ekler.
4. UI testleri (bölüm 10).

---

## 10. Doğrulama Listesi

- [ ] Migration `RezervasyonGenelTarifeleri` tablosu + unique index oluşturuldu.
- [ ] Eski global `RezervasyonUcretKural` (BirimId=null) silindi; cari yıl genel tarifelerine dönüştürüldü.
- [ ] Birime özel `RezervasyonUcretKural` (BirimId!=null) kayıtlarına dokunulmadı.
- [ ] Admin/Tarife/Yil/{yil} sayfasında "Rezervasyon Genel Tarifeleri" tablosu görünüyor.
- [ ] Değer değiştir + kaydet + yenile → değerler korunuyor.
- [ ] Yeni yıl açıldığında (YilEkle kopyalama) rezervasyon satırları da kopyalandı.
- [ ] Tasinmaz/Ekle ekranında rezervasyon alanları üstünde parent kart görünüyor.
- [ ] Karttaki satırlar Admin/Tarife cari yıl değerleriyle eşleşiyor.
- [ ] Rezervasyon hesabı: birime özel kural varsa o kullanılıyor (override).
- [ ] Rezervasyon hesabı: birime özel yoksa Birim.BirimTuru cari yıl genel tarifesi kullanılıyor.
- [ ] Cari yıl tarifesi yoksa anlamlı hata mesajı dönüyor.

---

## 11. Dokunulan Dosyalar

| Dosya | Değişiklik |
|---|---|
| `Models/RezervasyonGenelTarife.cs` | **Yeni entity** |
| [Data/ApplicationDbContext.cs](KiraTakip/Data/ApplicationDbContext.cs) | DbSet + OnModelCreating |
| `Migrations/<ts>_AddRezervasyonGenelTarife.cs` | Yeni migration |
| [Models/ViewModels/TarifeViewModels.cs](KiraTakip/Models/ViewModels/TarifeViewModels.cs) | `TarifeMatrisRezervasyonSatir` + VM ek alanları |
| `Models/ViewModels/ParentRezervasyonTarifeKartViewModel.cs` | **Yeni VM** |
| [Models/ViewModels/TasinmazViewModels.cs](KiraTakip/Models/ViewModels/TasinmazViewModels.cs) | `ParentRezervasyonTarife` alanı |
| [Controllers/AdminTarifeController.cs](KiraTakip/Controllers/AdminTarifeController.cs) | Detay/KalemGuncelle/YilEkle |
| [Controllers/TasinmazController.cs](KiraTakip/Controllers/TasinmazController.cs) | Ekle GET parent doldur |
| [Services/Interfaces/ITarifeHiyerarsiService.cs](KiraTakip/Services/Interfaces/ITarifeHiyerarsiService.cs) + [TarifeHiyerarsiService.cs](KiraTakip/Services/TarifeHiyerarsiService.cs) | `GetRezervasyonParentForAsync` |
| [Services/RezervasyonService.cs](KiraTakip/Services/RezervasyonService.cs) | HesaplaAsync precedence |
| [Views/AdminTarife/Detay.cshtml](KiraTakip/Views/AdminTarife/Detay.cshtml) | Rezervasyon tablosu |
| `Views/Shared/_ParentRezervasyonTarifeKart.cshtml` | **Yeni partial** |
| [Views/Tasinmaz/Ekle.cshtml](KiraTakip/Views/Tasinmaz/Ekle.cshtml) | Partial render |
| [Services/SeedDataService.cs](KiraTakip/Services/SeedDataService.cs) | EnsureVarsayilan* + Backfill |
| `Program.cs` | Seed çağrı sırası |

---

## 12. Kapsam Dışı

- `RezervasyonUcretKural` modeline `TarifeId`/`BirimTuruId` ekleme (discriminator antipattern reddedildi).
- Ayrı admin ekranı (Tarife yönetimi içinde tutuldu).
- Mevcut `_ParentTarifeKart.cshtml`'i değiştirme (kira tarafı saf kalır).
- BirimTuru bazlı özel override (sadece Birim bazlı override desteklenir; ihtiyaç doğarsa sonraki refactor).
- Geçmiş rezervasyon kayıtlarının yeniden fiyatlandırılması — geçmiş kayıtlar olduğu gibi kalır.
