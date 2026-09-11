# Aşama B — Uygulama Planı (Retroaktif)

> **Üst karar dökümanı:** [refaktor-buyuk-mimari-sadelestirme.md](refaktor-buyuk-mimari-sadelestirme.md)
> **Kapsam:** Yapısal, orta etki — ödeme kanal-agnostikliği, işlem geçmişi UI, rezervasyon ücret birleşimi.
> **Aşama süresi:** İki oturuma yayıldı (B1+B2: ilk oturum, B3: ikinci oturum).
> **Durum:** ✅ Tamamlandı (2026-05-19).
>
> **NOT:** Bu döküman aşama tamamlandıktan sonra Aşama A standardına uygun şekilde retroaktif olarak yazıldı. Aşama C için aynı sıra korunacak: plan → uygulama.

---

## Görevler

### B1 — Karar #7: `KiraOdeme` Kanal-Agnostik (`OdemeKaynakTipi` + `PosReferansNo`)

**Amaç:** Ödemenin hangi kanaldan geldiği bilgisini ayrı bir enum'a taşımak; sanal POS dönüşünde otomatik onay akışını desteklemek.

**Etkilenen dosyalar:**
- `Models/KiraOdeme.cs` — `OdemeKaynakTipi` (enum) + `PosReferansNo` (string?, max 100) eklendi
- `Models/Enums.cs` — `OdemeKaynakTipi { Manuel=1, BankaEslesme=2, SanalPos=3 }`
- `Data/ApplicationDbContext.cs` — kolon `HasComment("Manuel=1, BankaEslesme=2, SanalPos=3")`
- Ödeme oluşturma akışlarında varsayılan `Manuel` atandı

**Migration:** `20260519103023_B1_KiraOdemeKaynakTipi`
- `KiraOdemeler.OdemeKaynakTipi` (int, NOT NULL, default 1, comment ile)
- `KiraOdemeler.PosReferansNo` (nvarchar(100), nullable)

**Doğrulama:**
- ✅ `dotnet build` 0 hata
- ✅ Migration `database update` başarılı
- ✅ Mevcut ödemeler `Manuel` olarak işaretlendi (default değerle)

---

### B2 — Karar #9: `SozlesmeIslemGecmisi` UI Eklenmesi

**Amaç:** Daha önce yazılmış olan `SozlesmeIslemGecmisi` tablosunun UI'da görünür hale gelmesi; sözleşme detay sayfasına "İşlem Geçmişi" sekmesi.

**Etkilenen dosyalar:**
- `Views/Sozlesme/Detay.cshtml` — "İşlem Geçmişi" sekmesi (tab) eklendi
- `Controllers/SozlesmeController.cs` — `Detay` GET'te `SozlesmeIslemGecmisi` listesi yüklendi (`.OrderByDescending(i => i.OlusturmaTarihi)`)
- `Models/ViewModels/SozlesmeViewModels.cs` — `SozlesmeDetayViewModel.IslemGecmisi` koleksiyonu

**Migration:** Yok (tablo zaten vardı, sadece UI bağlandı).

**Doğrulama:**
- ✅ Sözleşme detay sayfasında "İşlem Geçmişi" sekmesi tüm kayıtları gösteriyor
- ✅ Oluşturma, uzatma, fesih, yeniden üretim işlemleri kronolojik listede

---

### B3 — Karar #6 + #8: `RezervasyonUcret` Birleşimi

**Amaç:** `RezervasyonUcretKural` (birime özel) + `RezervasyonGenelTarife` (yıl × birim türü) → tek `RezervasyonUcret` tablosu. "Global fallback yok" kuralı: `BirimId == null` olan eski global kayıtlar kaldırıldı.

**Yeni entity (`Models/RezervasyonUcret.cs`):**
```csharp
public class RezervasyonUcret
{
    public int Id { get; set; }

    // Birime özel kural: BirimId dolu, BirimTuruId + Yil null
    public int? BirimId { get; set; }
    public Birim? Birim { get; set; }

    // Yıllık genel tarife: BirimTuruId + Yil dolu, BirimId null
    public int? BirimTuruId { get; set; }
    public BirimTuru? BirimTuru { get; set; }
    public int? Yil { get; set; }

    public bool Aktif { get; set; } = true;
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
```

**EF Core konfigürasyon (`Data/ApplicationDbContext.cs`):**
- `HasCheckConstraint("CK_RezervasyonUcret_BirimOrYilTuru", "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)")`
- `HasIndex(r => new { r.BirimTuruId, r.Yil }).IsUnique().HasFilter("[BirimId] IS NULL")` — filtreli unique index
- `Birim` FK `OnDelete.SetNull`, `BirimTuru` FK `OnDelete.Restrict`

**Etkilenen dosyalar (18):**
- **Yeni:** `Models/RezervasyonUcret.cs`
- **Boşaltıldı (sadece namespace + yorum):** `Models/RezervasyonUcretKural.cs`, `Models/RezervasyonGenelTarife.cs`
- **DbContext:** `Data/ApplicationDbContext.cs` — eski 2 DbSet kaldırıldı, yeni `RezervasyonUcretler` eklendi
- **Servisler:** `Services/RezervasyonService.cs`, `Services/Interfaces/IRezervasyonService.cs`, `Services/TarifeHiyerarsiService.cs`, `Services/SeedDataService.cs` (3 yer), `Services/TasinmazService.cs`
- **Controller'lar:** `Controllers/BirimController.cs`, `Controllers/TasinmazController.cs`, `Controllers/AdminTarifeController.cs` (5 yer)
- **ViewModel'ler:** `Models/ViewModels/TasinmazViewModels.cs`, `Models/ViewModels/BirimRateViewModels.cs`, `Models/ViewModels/TarifeViewModels.cs`
- **View'lar:** `Views/AdminRezervasyonUcretKural/Index.cshtml`, `Views/AdminTarife/Detay.cshtml`, `Views/Tasinmaz/Detay.cshtml`

**Global fallback temizliği:**
- `BirimController.OzelFiyat`: `vm.GlobalRezervasyonKural` atama bloğu silindi
- `TasinmazController.Detay`: `globalKural` query'si + `?? globalKural` fallback'i silindi; `globalRezKural` ViewModel atama satırı silindi
- `Views/Tasinmaz/Detay.cshtml`: `aktifKural = ozelKural ?? GlobalRezervasyonKural` → `aktifKural = ozelKural`; "Global" rozeti → "Tanımsız"

**Migration:** `20260519104651_B3_RezervasyonUcretBirlestirme`
- `DropTable("RezervasyonGenelTarifeleri")`
- `DropTable("RezervasyonUcretKurallari")`
- `CreateTable("RezervasyonUcretler")` — yukarıdaki check constraint + filtreli unique index ile

**Doğrulama:**
- ✅ `dotnet build` 0 hata, 38 warning (B3 öncesiyle aynı)
- ✅ Migration `database update` başarılı
- ✅ `RezervasyonService.HesaplaAsync` iki katmanlı cascade ile çalışıyor (birim → birim türü + cari yıl)
- ✅ Admin Tarife Detay sayfasında rezervasyon genel tarifeleri tablosu (yıl bazlı)
- ✅ Admin Rezervasyon Ücret Kuralları sayfasında sadece birime özel kayıtlar listeleniyor

---

## Aşama B Çıktıları

| # | Migration | Tablo Değişikliği | UI Değişikliği |
|---|---|---|---|
| B1 | `B1_KiraOdemeKaynakTipi` | `KiraOdemeler` +2 kolon | — (kanal seçimi backend) |
| B2 | — | — | Sözleşme Detay → İşlem Geçmişi sekmesi |
| B3 | `B3_RezervasyonUcretBirlestirme` | 2 tablo drop + 1 tablo create | Admin Tarife + Birim + Taşınmaz görünümleri |

---

## Aşama B Sonrası

- ✅ PROGRESS.md → B1, B2, B3 işaretlendi
- ✅ Aşama C uygulama planı: `refaktor-asama-c-uygulama.md`
- ➡️ Aşama C yeni oturumda uygulanacak
