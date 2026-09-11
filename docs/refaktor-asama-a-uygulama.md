# Aşama A — Uygulama Planı

> **Üst karar dökümanı:** [refaktor-buyuk-mimari-sadelestirme.md](refaktor-buyuk-mimari-sadelestirme.md)
> **Kapsam:** Bağımsız, düşük risk, hızlı kazanç refaktörleri.
> **Aşama süresi:** Tek oturum hedefi.

---

## Görevler (Sırayla)

### A1 — Karar #10: `SozlesmeRate.SozlesmeId` → `KiraSozlesmesiId`

**Amaç:** Diğer tüm FK'larla tutarlı adlandırma.

**Etkilenen dosyalar:**
- `Models/SozlesmeRate.cs` — property + navigation property adı
- `Data/ApplicationDbContext.cs` — Fluent API FK konfigürasyonu
- Tüm referans yerleri (Service, Controller, View) — grep ile tespit edilecek

**Migration:** `RenameSozlesmeRate_SozlesmeId_To_KiraSozlesmesiId`

**Doğrulama:**
- `dotnet build` → 0 hata
- Sözleşme detay sayfasında pazarlık fiyatları görünüyor
- Yeni sözleşme oluşturmada SozlesmeRate insert/update çalışıyor

---

### A2 — Karar #4: `KiraSozlesmesi.Depozito` Kaldırılması

**Amaç:** Mükerrer kolonu kaldır; gerçek değer ilk tahakkuğun DEPOZITO kaleminden okunsun.

**Adımlar:**
1. UI helper: `SozlesmeController` (veya `SozlesmeService`) içine `GetDepozitoTutarAsync(sozlesmeId)` metodu ekle.
   - İlk tahakkuğu bul: `KiraTahakkuk WHERE KiraSozlesmesiId=X ORDER BY DonemBaslangic LIMIT 1`
   - O tahakkuğun `TahakkukKalemi WHERE BorcTipi.Kod='DEPOZITO'` kaleminden `ToplamTutar` döndür
2. Detail view'ında bu helper'dan oku (`@Model.DepozitoTutari` gibi ViewModel'e yansıt)
3. `KiraSozlesmesi.Depozito` property'sini kaldır
4. `SozlesmeController.Create/Edit`'te `Depozito = depozito` atamasını kaldır
5. ViewModel'lerden `Depozito` kolonunu kaldır (veya UI input olarak yine var, ama entity'ye yazılmaz — kaldırılması daha temiz)

**Migration:** `DropKiraSozlesmesiDepozito`

**Doğrulama:**
- Sözleşme detay sayfasında "Depozito: X TL" görünüyor (ilk tahakkuk kaleminden)
- Yeni sözleşme oluşturma + ilk tahakkuk üretimi çalışıyor
- Mevcut sözleşmelerin depozitosu doğru gösteriliyor

---

### A3 — Karar #11: `KiraSozlesmesi.KdvOrani` Kaldırılması

**Amaç:** Her rate kendi KDV oranını taşıyor; sözleşme bazında genel oran gereksiz. `KdvUygulanacakMi` master switch korunur.

**Önkoşul (Araştırma):**
- `KiraSozlesmesi.KdvOrani` kodda nerede okunuyor? Eğer:
  - Sadece set ediliyor, okunmuyor → doğrudan kaldır
  - Fallback olarak okunuyor (rate'ler KDV doldurmamışken) → rate tablolarında `KdvOrani` nullable olamaz, zorunlu yap

**Adımlar:**
1. Grep: `KiraSozlesmesi.KdvOrani`, `sozlesme.KdvOrani`, `.KdvOrani` okuma yerleri
2. Fallback bulunursa: ilgili kodu rate'in kendi KdvOrani'ni okuyacak şekilde değiştir
3. `KiraSozlesmesi.KdvOrani` property'sini kaldır
4. `SozlesmeController.Create/Edit` ve View'lardan KdvOrani input'unu kaldır
5. ViewModel temizliği

**Migration:** `DropKiraSozlesmesiKdvOrani`

**Doğrulama:**
- Tahakkuk üretimi rate'lerin KDV oranını kullanıyor
- Mevcut sözleşmelerle tahakkuk üretimi aynı sonucu veriyor (regresyon)

---

### A4 — Karar #1: `TasinmazTipiKiralamaSekli` → Bayraklar

**Amaç:** N:N ara tablosunu kaldır; bayrak kolonlarla değiştir.

**Adımlar:**
1. `Models/TasinmazTipi.cs` — ekle:
   ```csharp
   public bool TekParcaDestekli { get; set; }
   public bool BirimBazliDestekli { get; set; }
   ```
2. Mevcut `TasinmazTipiKiralamaSekli` kayıtlarını bayraklara çeviren data migration (Up'ta SQL):
   ```sql
   UPDATE TasinmazTipi SET TekParcaDestekli = 1 WHERE Id IN (SELECT TasinmazTipiId FROM TasinmazTipiKiralamaSekli WHERE KiralamaSekli = 1)
   UPDATE TasinmazTipi SET BirimBazliDestekli = 1 WHERE Id IN (SELECT TasinmazTipiId FROM TasinmazTipiKiralamaSekli WHERE KiralamaSekli = 2)
   ```
3. `TasinmazTipi.KiralamaSekilleri` navigation property kaldır
4. `Models/TasinmazTipiKiralamaSekli.cs` sil
5. `ApplicationDbContext` — `DbSet<TasinmazTipiKiralamaSekli>` sil, Fluent API kaldır
6. `TasinmazController.cs:160-198` — `_ctx.TasinmazTipiKiralamaSekilleri` sorgularını bayrak okumayla değiştir
7. `AdminTasinmazTipi*` Controller + View'lar — bayrak checkbox UI'sı

**Migration:** `ReplaceTasinmazTipiKiralamaSekliWithFlags`

**Doğrulama:**
- AdminTasinmazTipi UI'da bayraklar düzgün toggle ediliyor
- Tasinmaz Create akışında izinli KiralamaSekli seçenekleri filtreleniyor

---

### A5 — Karar #5: `Tarife` Kaldır + `TarifeKalemi.Yil` + `Aktif`

**Amaç:** Kapak tablosunu kaldır; yıl bilgisini doğrudan kaleme yaz.

**Adımlar:**
1. `Models/Tarife.cs` (`TarifeKalemi` aynı dosyada) — değiştir:
   - `TarifeKalemi.TarifeId` → kaldır
   - `TarifeKalemi.Yil` (int) ekle
   - `TarifeKalemi.Aktif` (bool, default true) ekle
2. `RezervasyonGenelTarife` (Karar #6/8 ile ilişkili) — `TarifeId` → `Yil` (int), `Aktif` (bool) eklenecek
3. Data migration: `TarifeKalemi.Yil = Tarife.Yil`, `TarifeKalemi.Aktif = Tarife.Aktif` (eski Tarife verisinden taşı)
4. `Tarife` DbSet'i ve tablosu drop
5. `Models/Tarife.cs` dosyasından `Tarife` sınıfını sil, sadece `TarifeKalemi` kalsın (veya yeniden adlandır)
6. `Services/RateResolverService` veya `TarifeHiyerarsiService` — `Tarife` referanslarını kaldır, `WHERE Yil=cari yıl AND Aktif=true` filtresi
7. `AdminTarifeController` — Tarife kapak yönetimi yerine "yıl + kalemler" yönetimi
8. View'lar — `Views/AdminTarife/` reorganizasyon
9. Yıl pasifleştirme akışı: `_ctx.TarifeKalemleri.Where(t => t.Yil == yil).ExecuteUpdateAsync(s => s.SetProperty(t => t.Aktif, false))`

**Migration:** `DropTarifeTableAddYilAktifToTarifeKalemi`

**Doğrulama:**
- AdminTarife UI'da yıllar listeleniyor, kalemler düzenlenebiliyor
- Yıl pasifleştirme tüm kalemleri pasif yapıyor
- Tahakkuk üretimi cari yıl tarifesini kullanıyor

**NOT:** `RezervasyonGenelTarife`'deki `TarifeId` da burada eş zamanlı dönüştürülmeli (yoksa FK kırılır). Karar #6/#8 Aşama B'de tam birleşim olarak yapılacak, ama `TarifeId` → `Yil` dönüşümü burada yapılabilir veya RezervasyonGenelTarife geçici olarak silinebilir (Aşama B'de yeni `RezervasyonUcret` tablosuyla geri gelecek).

**Karar:** Bu refaktörde `RezervasyonGenelTarife` tablosunu `Yil`+`Aktif`'e dönüştür (Tarife.Aciklama → RezervasyonGenelTarife.Aciklama). Aşama B'de tam birleşim yapılacak.

---

### A6 — Karar #2: `EnumDegerleri` + `HasComment` Standardı

**Amaç:** Veritabanı incelemesinde enum okunabilirliği.

**Adımlar:**
1. `Models/EnumDegerleri.cs` yeni entity:
   ```csharp
   public class EnumDegerleri {
       public int Id { get; set; }
       public string EnumAd { get; set; }      // "SozlesmeDurumu"
       public int Deger { get; set; }          // 1
       public string Kod { get; set; }         // "AKTIF"
       public string Aciklama { get; set; }    // "Aktif sözleşme"
   }
   ```
   - Unique index: `(EnumAd, Deger)`
2. `Services/EnumSeedService.cs` yeni servis:
   - Reflection ile `KiraTakip.Models` ve `KiraTakip.Enums` namespace'lerindeki tüm `enum` tiplerini tara
   - Her enum için `EnumDegerleri` tablosuna kayıt ekle/güncelle (eksikleri ekle, fazlaları sil)
3. `Program.cs` — start-up'ta `IdentitySeedService` yanına `EnumSeedService.SeedAsync()` çağrısı
4. `ApplicationDbContext.OnModelCreating` — kritik enum kolonlarına `HasComment` ekle:
   - `KiraSozlesmesi.Durum`
   - `KiraTahakkuk.Durum`
   - `KiraTahakkuk.KaynakTipi`
   - `KiraOdeme.Durum`
   - `KiraOdeme.OdemeKanali`
   - `KiraOdeme.OdemeKaynakTipi` (Aşama B'de eklenecek)
   - `BankaHareketi.EslesmeDurumu`
   - `OdemeBankaEslesme.EslesmeTipi`
   - `ToplantiSalonuRezervasyon.Durum`
   - `BorcTipi.Davranis`
   - `Kiraci.KiraciTuru`
   - `TahakkukKalemi.KaynakTipi`, `HesaplamaYontemi`
   - `SozlesmeIslemGecmisi.IslemTipi`

**Migration:** `AddEnumDegerleriTableAndEnumComments`

**Doğrulama:**
- Start-up sonrası `EnumDegerleri` tablosunda tüm enum'lar listelenir
- SSMS'de bir tablo açıldığında, enum kolonlarının Description bölümü dolu gelir

---

## Uygulama Sırası

1. **A1** → en bağımsız, en küçük
2. **A2** → bağımsız
3. **A3** → bağımsız (önce grep ile fallback kontrolü)
4. **A4** → bağımsız (TasinmazTipi)
5. **A5** → en geniş (Tarife + RezervasyonGenelTarife)
6. **A6** → en son (Enum standartı, diğerleri uygulandıktan sonra `HasComment` listesi netleşir)

Her görev sonrası:
- `dotnet build` → 0 hata
- Migration üret + DB'ye uygula
- İlgili akışı tarayıcıda manuel test et

---

## Aşama A Sonrası

PROGRESS.md güncellenir, Aşama B uygulama planı yazılır.
