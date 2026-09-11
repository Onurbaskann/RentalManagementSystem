# Faz 10 — Taşınmaz Detayı UX İyileştirme

> **GÜNCELLİK NOTU:** Bu spec yazıldığında `BorcTipiDavranisi.ManuelTetiklemeli(3)` kullanılıyordu.
> Faz 9-1 refactor'u ile bu değer ayrıştırıldı: `KullaniciManuel = 3`, `RezervasyonOzel = 4`.
> Spec içindeki `ManuelTetiklemeli` ifadelerini güncel kodda `KullaniciManuel || RezervasyonOzel` olarak okuyun.
> Detay: `phase-9-1-borc-tipi-davranisi-refactor.md`

> Scope: Birimler sekmesi kat-accordion + birim tipi (kira/rezervasyon) ayrımı + OzelFiyat ekranının tipe göre dallanması.
> Önceki spec'lere referans: `bina-ofis-birim-spec.md`, `phase-8-parametre-rezervasyon-ve-manuel-borc.md`, `phase-9-fiyatlandirma-mimarisi.md`.

## Karar Özeti (kullanıcı onaylı)

| # | Karar |
|---|---|
| 1 | Hibrit birim **yasak**. `KiralanabilirMi` XOR `RezervasyonYapilabilirMi` (tam biri true). |
| 2 | Accordion default: **tüm katlar kapalı**. "Boş" filtresi seçildiğinde boş birimi olan katlar otomatik açılır. |
| 3 | localStorage state **kullanılmayacak**. Her sayfa yüklemesinde temiz başlar. |
| 4 | Rezervasyon alanı herhangi bir katta olabilir (zemin=0, bodrum=negatif, üst=pozitif). Kat etiketi: `0 → "Zemin Kat"`, `<0 → "X. Bodrum"`, `>0 → "N. Kat"`. |
| 5 | OzelFiyat'ta `BorcTipiDavranisi.ManuelTetiklemeli` borç tipleri **gizlenecek** (kira birim ekranında listelenmez). |

---

## 1. BirimTuru — Hibrit Engeli

**Dosya:** `Controllers/AdminBirimTuruController.cs` (Create + Edit POST)

Validasyon kuralı (ikisinde de uygulanacak):

```csharp
if (model.KiralanabilirMi == model.RezervasyonYapilabilirMi)
{
    ModelState.AddModelError(string.Empty,
        "Tam olarak bir kullanım türü seçilmelidir: Kiralanabilir VEYA Rezervasyon yapılabilir.");
    return View(model);
}
```

**View güncellemesi** (`Views/AdminBirimTuru/Create.cshtml` + `Edit.cshtml`): iki checkbox'ı **radio group**'a çevir (`Kiralanabilir` / `Rezervasyon yapılabilir`). Bind: Alpine'da `x-data="{ tur: @(Model.RezervasyonYapilabilirMi ? "'rez'" : "'kira'") }"` + iki hidden input form post'ta `KiralanabilirMi = tur==='kira'`, `RezervasyonYapilabilirMi = tur==='rez'`. (Veya sunucuda checkbox kalsın, sadece JS ile karşılıklı toggle yapılsın — daha basit.)

**Migration gereksinimi yok** — model değişmiyor, sadece validasyon.

**Data temizleme:** Migration sonrası bir kerelik script — mevcut `BirimTuru` kayıtlarından ikisi de true/false olanlar var mı kontrol edilip elle düzeltilecek. Seed data zaten temiz (her tipte tek flag true).

---

## 2. Birimler Sekmesi — Kat Accordion

**Dosya:** `Views/Tasinmaz/Detay.cshtml` (satır ~110-330 Birimler tab).

### Yapı

```
[Filtre Bar]  Durum: [Tümü ▼]  [Tümünü Aç] [Tümünü Kapat]   Arama: [_______]
─────────────────────────────────────────────────────────────────────
▶ 3. Kat (5 birim · 2 boş)
▶ 2. Kat (8 birim)
▶ 1. Kat (6 birim · 1 boş)
▶ Zemin Kat (4 birim · 3 rezervasyon)
▶ 1. Bodrum (2 birim · depo)
```

- Her kat satırı tıklanabilir; chevron `▶/▼` ile aç/kapat.
- Açıkken altında o katın birim tablosu render edilir (mevcut tablo HTML aynen).
- Alpine.js: `x-data="{ open: false }"` + `x-show="open"` + `x-transition`. localStorage yok.

### Filtre etkisi

`x-data="{ filter: 'tum', openFloors: {} }"` üst seviyede.
- Filter değişince `openFloors[katNo] = true` atanır eğer kat o filtreyle eşleşen birim içeriyorsa.
- "Boş" seçilirse → sadece `AktifSozlesmesiYok==true` olan birimler gösterilir; o birimleri içeren katlar açılır, diğerleri kapalı kalır (içlerinde eşleşen yoksa).
- "Tümü" filtresi tüm katları açıp kapatma durumunu kullanıcıya bırakır (toggle butonlarıyla).

### Kat etiketi helper (Razor inline)

```csharp
@functions {
    string KatEtiketi(int katNo) => katNo switch
    {
        0   => "Zemin Kat",
        < 0 => $"{Math.Abs(katNo)}. Bodrum",
        _   => $"{katNo}. Kat"
    };
}
```

Sıralama: katlar büyükten küçüğe (üst kat üstte, bodrum altta). Mevcut groupBy + OrderByDescending yeterli.

### Kat header'ı meta sayaçlar

`X birim · Y boş · Z rezervasyon` formatı. Hesaplama Razor'da `katBirimleri.Count`, `.Count(b => b.AktifSozlesmesiYok)`, `.Count(b => b.Birim.BirimTuru.RezervasyonYapilabilirMi)`.

---

## 3. İşlem Sütunu — Tipe Göre Buton

**Dosya:** `Views/Tasinmaz/Detay.cshtml` (mevcut "Kiraya Ver" butonu satır ~223-236).

### Kural Matrisi

| BirimTuru | Aktif Sözleşme | Buton |
|---|---|---|
| Kiralanabilir | yok | **Kiraya Ver** → `/Sozlesme/Ekle?birimId=…` |
| Kiralanabilir | var | **Sözleşme** → `/Sozlesme/Detay/{id}` (mevcut davranış korunur) |
| Rezervasyon | — | **Rezervasyon Yap** → `/Rezervasyon/Ekle?birimId=…` |

OzelFiyat butonu (₺) her iki tipte de görünür ama içerik dallanır (bkz. §4).

### Controller değişikliği

`TasinmazController.Detay` action'da `Birim.BirimTuru` zaten Include ediliyorsa kontrol et; değilse `.Include(b => b.BirimTuru)` ekle. (Detay view'ı `BirimTuru.RezervasyonYapilabilirMi` field'ına erişebilmeli.)

### Kira Bedeli sütunu

Rezervasyon birimleri için "Kira Bedeli" değeri anlamsız. Bu sütunda rezervasyon birimi için göster: `"{PeriyotUcreti} ₺ / {UcretlendirmePeriyoduDakika} dk"`. Veri kaynağı: `RezervasyonUcretKural` (birim-spesifik varsa onu, yoksa global). Hesaplama view'da değil controller'da yapılıp ViewBag/ViewModel ile geçirilirse temiz olur.

---

## 4. OzelFiyat Ekranı — Üç Senaryo

**Dosyalar:** `Controllers/BirimController.cs` (`OzelFiyat` GET/POST), `Views/Birim/OzelFiyat.cshtml`.

Controller `BirimTuru` include eder; aşağıdaki dallanma view'da yapılır.

### Senaryo A — Salt Kiralanabilir (`KiralanabilirMi=true`)

- Mevcut tablo korunur (Borç Tipi × özel fiyat satırları + Alpine aktif/pasif toggle).
- **Filtre:** Sadece `Davranis ∈ {AylikSabit, IlkAyTekSeferlik}` borç tipleri listelenir. `ManuelTetiklemeli` (örn. TOPLANTI) gizlenir.
- Controller sorgusu güncellenir:
  ```csharp
  var aktifBorcTipleri = await _ctx.BorcTipleri
      .Where(b => b.Aktif
                && b.Davranis != BorcTipiDavranisi.ManuelTetiklemeli)
      .OrderBy(b => b.Sira)
      .ToListAsync();
  ```

### Senaryo B — Salt Rezervasyon (`RezervasyonYapilabilirMi=true`)

- Tablo gizlenir.
- Yerine **tek bir kart**: `RezervasyonUcretKural` (BirimId=bu birim) için form.
- Alanlar: `UcretsizSureDakika`, `UcretlendirmePeriyoduDakika`, `PeriyotUcreti`, `KdvOrani`, `Aktif`, `Aciklama`.
- Form yoksa: "Bu birim için özel rezervasyon kuralı yok — global kural uygulanır." mesajı + "Özel Kural Tanımla" butonu.
- Form varsa: değerler doldurulmuş + "Sıfırla (Global Kuralı Kullan)" butonu (kayıt silinir).
- Service: `RezervasyonService.SaveUcretKuralAsync` zaten mevcut, reuse.

### Senaryo C — Hibrit

**Olmayacak** (§1 ile engellendi). View defansif olarak hata kartı gösterir: "Bu birim türü hatalı tanımlı — yöneticiye bildirin."

### Controller iskeleti

```csharp
public async Task<IActionResult> OzelFiyat(int id)
{
    var birim = await _ctx.Birimler
        .Include(b => b.BirimTuru)
        .Include(b => b.Tasinmaz)
        .FirstOrDefaultAsync(b => b.Id == id);
    if (birim == null) return NotFound();

    var vm = new BirimOzelFiyatViewModel { Birim = birim };

    if (birim.BirimTuru.KiralanabilirMi)
    {
        vm.AktifBorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.ManuelTetiklemeli)
            .OrderBy(b => b.Sira).ToListAsync();
        vm.OzelFiyatlar = await _ctx.BirimBorcTipiFiyatlari
            .Where(f => f.BirimId == id).ToListAsync();
    }
    else if (birim.BirimTuru.RezervasyonYapilabilirMi)
    {
        vm.OzelRezervasyonKural = await _ctx.RezervasyonUcretKurallari
            .FirstOrDefaultAsync(r => r.BirimId == id);
        vm.GlobalRezervasyonKural = await _ctx.RezervasyonUcretKurallari
            .FirstOrDefaultAsync(r => r.BirimId == null && r.Aktif);
    }

    return View(vm);
}
```

---

## 5. Dosya Değişiklik Listesi

| Dosya | Tip | Değişiklik |
|---|---|---|
| `Controllers/AdminBirimTuruController.cs` | Edit | XOR validasyonu (Create+Edit POST) |
| `Views/AdminBirimTuru/Create.cshtml` | Edit | Radio/JS karşılıklı toggle |
| `Views/AdminBirimTuru/Edit.cshtml` | Edit | Aynı toggle |
| `Controllers/TasinmazController.cs` | Edit | `.Include(b => b.BirimTuru)`; rezervasyon ücret hesaplaması ViewBag |
| `Views/Tasinmaz/Detay.cshtml` | Edit | Kat accordion (Alpine) + tipe göre buton + kat etiketi helper |
| `Controllers/BirimController.cs` | Edit | OzelFiyat dallanma + ManuelTetiklemeli filtre |
| `Models/ViewModels/BirimOzelFiyatViewModel.cs` | New | Senaryo A/B alanlarını tutar |
| `Views/Birim/OzelFiyat.cshtml` | Edit | İki senaryo dallanması (`@if KiralanabilirMi` / `else if RezervasyonYapilabilirMi`) |

**Migration:** Yok.
**Permission:** Mevcut policy'ler yeterli (`BirimTuruPerm.Manage`, `Birim.ManageRate`, `Rezervasyon.ManageRate` — varsa).
**Test edilecekler:** (1) Hibrit kayıt denemesi reddediliyor mu, (2) accordion default kapalı + filtre açma, (3) rezervasyon birimde "Kiraya Ver" yerine "Rezervasyon Yap", (4) OzelFiyat üç senaryo doğru dallanıyor, (5) ManuelTetiklemeli borç tipleri kira birim OzelFiyat'ta görünmüyor.

---

## 6. Uygulama Sırası (Önerilen)

1. **BirimTuru XOR validasyonu** (en küçük, izole değişiklik — diğer adımlar bunun sağlam çalıştığını varsayar).
2. **Mevcut data temizleme** (manuel SQL veya seed kontrolü).
3. **Detay sayfası: kat etiketi + accordion** (Birimler sekmesi).
4. **Detay sayfası: tipe göre buton + rezervasyon ücreti sütunu**.
5. **OzelFiyat senaryo A — ManuelTetiklemeli filtre** (minimal değişiklik, mevcut akış korunur).
6. **OzelFiyat senaryo B — rezervasyon kuralı formu** (yeni view dalı).
7. **End-to-end test + PROGRESS.md güncelleme**.
