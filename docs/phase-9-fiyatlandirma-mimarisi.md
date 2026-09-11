# FAZ 9 — Taşınmaz × Kiracı Kategorisi Bazlı Dinamik Fiyatlandırma

> **GÜNCELLİK NOTU:** Faz 9’da başlangıçta `BorcTipiDavranisi` üç değerli tasarlanmıştı:
> `AylikSabit`, `IlkAyTekSeferlik`, `ManuelTetiklemeli`.
> Daha sonra `phase-9-1-borc-tipi-davranisi-refactor.md` ile `ManuelTetiklemeli` ayrıştırıldı:
> - `KullaniciManuel = 3`
> - `RezervasyonOzel = 4`
>
> Güncel kodda `ManuelTetiklemeli` kullanılmamalıdır.

> Faz 8 ile gelen TasinmazKategoriCarpan tablosunun yerini, tüm borç tipleri için
> kategori bazlı fiyat matrisi alır. Sözleşme formu otomatik doldurma kazanır.

## Hedef

- BorcTipi davranışı tek enum ile netleşsin (Aylık / İlk Ay / Manuel).
- Tüm sabit borç tipleri için Taşınmaz × Kiracı Kategorisi matrisi tanımlanabilsin.
- Sözleşme oluşturulurken kalemler birim+kiracı seçildiğinde otomatik dolsun.

## Kabul Kriterleri

1. `BorcTipi.Davranis = AylikSabit` olan kalem fiyat bulunamasa bile 0₺ olarak tahakkuk üzerinde görünür.
2. DEPOZITO sadece ilk ay tahakkukunda yer alır (mevcut davranış korunur).
3. Yeni `TasinmazKiraciKategoriFiyat` matrisi resolver'da BirimRate ile Tarife arasında öncelik sırasına girer.
4. Sözleşme/Ekle ekranında Birim + Kiracı seçildiğinde tüm sabit kalemler ve hesaplanan tutarlar input'lara otomatik dolar.
5. Kullanıcı dolan tutarı silip override girebilir; kayıtta `SozlesmeRate` olarak yazılır.

## Mimari Karar Notları

- **Davranış enum'u tek kaynaklı**: `TekSeferlikMi` boolean'ı ile `SabitMi` boolean'ı birlikte tutulamaz; çelişkili durumlar yaratır. Tek enum ile üç durum net ayrılır.
- **0₺ kalem mantığı üretim servisinde**: Resolver'ın sorumluluğu "configured rate'i bul"; null döndürmesi "konfigürasyon eksik" bilgisini taşır. "Sabit kalem her zaman görünsün" kararı tahakkuk-üretim domain kuralıdır ve composer pattern ile hem otomatik üretim hem de Sözleşme/Ekle preview API'si tarafından paylaşılır.
- **`BirimDeger` adlandırması**: `BirimRate`, `SozlesmeRate`, `TarifeKalemi` ile tutarlı kalır; eski `Carpan` ismi terk edilir.

---

## 9.1 BorcTipi Davranış Enum'u

- [ ] 9.1.1 — `BorcTipiDavranisi { AylikSabit, IlkAyTekSeferlik, ManuelTetiklemeli }` enum tanımı (tarihsel tasarım, bkz: güncellik notu)
- [ ] 9.1.2 — `BorcTipi` modeline `Davranis` kolonu eklenir; `TekSeferlikMi` kaldırılır
- [ ] 9.1.3 — Veri migrasyonu: `TekSeferlikMi=true` → `IlkAyTekSeferlik`; MANUEL/TOPLANTI → `ManuelTetiklemeli`; diğerleri → `AylikSabit`
- [ ] 9.1.4 — Migration: `ReplaceTekSeferlikWithDavranis` (kolon ekle → veri taşı → eski kolon drop)
- [ ] 9.1.5 — `SeedDataService.SeedBorcTipleriAsync` güncellenir (`TekSeferlikMi` → `Davranis`)
- [ ] 9.1.6 — `TahakkukUretimService` filtresi: `Davranis == AylikSabit` (mevcut `!TekSeferlikMi` yerine)
- [ ] 9.1.7 — Depozito ekleme bloğu: `Davranis == IlkAyTekSeferlik` kontrolüne çevrilir

## 9.2 Fiyat Matrisi Tablosu

- [ ] 9.2.1 — `TasinmazCarpanController` + view'ları silinir; eski `TasinmazKategoriCarpan` referansları temizlenir
- [ ] 9.2.2 — `TasinmazKiraciKategoriFiyat` entity oluşturulur:
  - `TasinmazId`, `KiraciKategoriId`, `BorcTipiId`, `BirimDeger` (decimal), `HesaplamaYontemi` (Sabit/M2), `KdvOrani`, `Aktif`, `OlusturmaTarihi`, `Aciklama`
  - Unique index: `(TasinmazId, KiraciKategoriId, BorcTipiId)`
- [ ] 9.2.3 — Migration: `ReplaceTasinmazKategoriCarpanWithFiyat` (drop + create; test verisi olduğundan veri taşıma yok)
- [ ] 9.2.4 — DbContext DbSet + `OnModelCreating` index/precision

## 9.3 Resolver Güncellemesi

- [ ] 9.3.1 — `RateResolverService.ResolveAsync` precedence: Sozlesme → Birim → **TasinmazKiraciKategoriFiyat (tüm borç tipleri için)** → Tarife
- [ ] 9.3.2 — Eski "sadece KIRA için TasinmazKategoriCarpan" bloğu kaldırılır
- [ ] 9.3.3 — `KaynakTipi.TasinmazKategoriCarpan` enum değeri `TasinmazKiraciKategoriFiyat` olarak rename
- [ ] 9.3.4 — Resolver hâlâ null döndürebilir (rate yok); davranış kararı resolver'a sızmaz

## 9.4 Üretim Servisi — Composer Pattern

- [ ] 9.4.1 — `TahakkukKalemiPreview` DTO (`BorcTipiId`, `Ad`, `HesaplamaYontemi`, `BirimDeger`, `Carpan`, `Tutar`, `KdvOrani`, `KdvTutari`, `ToplamTutar`, `KaynakTipi`, `RateBulundu`)
- [ ] 9.4.2 — `ITahakkukUretimService.ComposeKalemlerAsync(birimId, kiraciId, donem, sozlesmeId?)` metodu eklenir
- [ ] 9.4.3 — Composer içinde: `Davranis == AylikSabit` ve resolver null döndüyse → `BirimDeger=0`, `Tutar=0` kalem üret (`RateBulundu=false` işaretlenir, log için)
- [ ] 9.4.4 — `UretSozlesmeIcinAsync` mevcut inline foreach'i `ComposeKalemlerAsync` çağrısına dönüştürülür
- [ ] 9.4.5 — Pro-rata uygulaması composer içinde tutulur (`donem` parametresine göre)

## 9.5 Taşınmaz "Parametreler" Sekmesi (Admin UI)

- [ ] 9.5.1 — `TasinmazFiyatController` (Index/Edit/Save) — taşınmaz başına fiyat matrisi
- [ ] 9.5.2 — Taşınmaz Detay sayfasına "Parametreler" sekmesi eklenir
- [ ] 9.5.3 — Dinamik grid: Satırlar = KiraciKategori, Sütunlar = BorcTipi (`Davranis ≠ ManuelTetiklemeli` olanlar)
- [ ] 9.5.4 — Her hücrede: `BirimDeger` input + `HesaplamaYontemi` select (Sabit/M2) + `KdvOrani` input
- [ ] 9.5.5 — Bulk save: tek POST'ta tüm değişen hücreler kaydedilir
- [ ] 9.5.6 — Permission: `Tasinmaz.Edit` policy'sine bağlı

## 9.6 Sözleşme Ekle — Otomatik Doldurma

- [ ] 9.6.1 — `SozlesmeEkleViewModel`'e `List<KalemOverrideDto> KalemOverrideleri` eklenir
- [ ] 9.6.2 — `SozlesmeController.GetVarsayilanKalemler(int birimId, int kiraciId, DateTime baslangic)` JSON endpoint'i — `ComposeKalemlerAsync` delegasyonu
- [ ] 9.6.3 — `Ekle.cshtml`'de Alpine bileşeni: birim/kiracı/baslangic değişimini watch eder, fetch atar, input'ları doldurur
- [ ] 9.6.4 — Her kalem için: ad (readonly) + Tutar input + "Otomatik" / "Override" rozet
- [ ] 9.6.5 — Kullanıcı tutarı değiştirdiğinde rozet "Override"a döner; varsayılana dönmek için "Sıfırla" butonu
- [ ] 9.6.6 — Sözleşme kaydedilirken override edilen kalemler için `SozlesmeRate` kayıtları üretilir (Faz 7.4 altyapısı)

## 9.7 Test ve Doğrulama

- [ ] 9.7.1 — Smoke: 2 kategori × 3 borç tipi matris kurulu taşınmazda, sözleşme açılıp 12 ay tahakkuk üretilir; her ay tüm sabit kalemler görünür
- [ ] 9.7.2 — 0₺ kalem testi: `BirimDeger=0` olarak tanımlı bir kalem tahakkukta 0₺ olarak görünür
- [ ] 9.7.3 — Override testi: sözleşmede özel tutar girilmiş kalem o sözleşme için kategori fiyatını ezer
- [ ] 9.7.4 — Manuel borç + Toplantı tahakkukları aylık otomatik üretime karışmaz
- [ ] 9.7.5 — Depozito ilk ay davranışı korunur

---

## Bağımlılık Notları

- Faz 7 (BorcTipi / Tarife / SozlesmeRate altyapısı) — gerekli, mevcut.
- Faz 8 (KiraciKategori, TasinmazKategoriCarpan) — kategori entity'si kalır, çarpan tablosu drop edilir.
- Permission değişikliği yok; mevcut `Tasinmaz.*` policy'leri yeterli.
