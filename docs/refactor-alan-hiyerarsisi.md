# Refactor — Fiziksel alanların ağaç (parent-child) yapısına taşınması

> Durum: **F0 (spec) — açık noktalar kullanıcıyla kapatılacak.** Kod değişikliği yok.
> Başlangıç: 2026-11-24. İlgili önceki çalışmalar: [`refactor-paylasimli-acik-birim.md`](refactor-paylasimli-acik-birim.md), [`refactor-acik-ofis-v2-uygulama-plani.md`](refactor-acik-ofis-v2-uygulama-plani.md).

## 1. Neden

Bugünkü model iki seviyeli: `Property (Tasinmazlar) → Unit (Birimler)`. Kiralama biçimi `UnitType.RentalMode` ile Birim bazlı / Kontenjan bazlı ayrılıyor; kontenjan tarafı `Unit.Capacity`, `UnitCapacityHistory`, `LeaseArea` (kiralanan/fiziksel m²), paylaşımlı taraf `LeaseGroup` ile çözüldü. Müşteriler kampüs, bina, kat, ofis, masa gibi kırılımlar istiyor. İki seviye bunu taşımıyor; kontenjan ve paylaşım mekanizmaları da bu kısıtı aşmak için eklenmiş yamalar. Uzun vadeli esneklik için fiziksel alan tanımı tek bir ağaç yapısına taşınır.

## 2. Kullanıcı kararları (2026-11-24)

| # | Karar |
|---|---|
| K1 | Derinlik serbest. Her teknopark kendi seviyelerini alan türleriyle tanımlar. |
| K2 | Her seviye kiralanabilir. Ne yapılabileceğini (kiralanabilir / rezerve edilebilir) alan türü belirler. |
| K3 | Alanlar özellik (attribute) tanımlarıyla genişletilebilir. |
| K4 | Kontenjan mantığı kalkar. Açık ofis üst düğümdür; masalar/kontenjanlar çocuk düğümlerdir. |
| K5 | Bir sözleşme birden çok alanı kapsayabilir. |

## 3. Veri modeli

### 3.1 `Alanlar` (Space)
Tek self-referencing tablo.

| Kolon | Açıklama |
|---|---|
| `UstAlanId` (ParentId) | null = kök. Kökler bugünkü taşınmazlardır. |
| `AlanTuruId` | `AlanTurleri` FK |
| `Kod`, `Ad`, `Sira`, `Aciklama` | |
| `Yol` | Materialized path, ör. `/1/5/12/`. Index'li. Ata/torun sorguları `LIKE 'yol%'`. |
| `Derinlik` | Kök = 0 |
| `Yuzolcumu` | m² |
| `KatNo` | opsiyonel |

`hierarchyid` yerine materialized path seçildi: yeni paket bağımlılığı yok, EF sorguları sade. Taşıma (move) işleminde alt ağacın `Yol` değerleri tek `UPDATE` ile güncellenir.

### 3.2 `AlanTurleri` (SpaceType) — `UnitType` ve `PropertyType`'ın yerine
`Ad, Kod (immutable), Kiralanabilir, RezerveEdilebilir, KokOlabilir, BorcTipiId? (rezervasyon için), Sira`.
Davranış bayrakları sorgu performansı için kolon olarak kalır; özellik tablosuna konmaz.

### 3.3 `AlanTuruKurallari`
`UstTurId → AltTurId`. Hangi türün hangi türün altına gelebileceğini belirler. Ör. Bina → Kat, Kat → Ofis / Açık Ofis, Açık Ofis → Masa.

### 3.4 Özellikler (K3)
- `AlanOzellikTanimlari`: `AlanTuruId, Kod, Ad, VeriTipi (Text/Number/Decimal/Bool/Date/List), Zorunlu, Sira, SecenekListesi?`
- `AlanOzellikDegerleri`: `AlanId, TanimId, Deger (nvarchar; tipine göre doğrulanır)`
- Özellikler gösterim ve filtre içindir. İş kuralları (m², kiralanabilirlik) çekirdek kolonlarda kalır.

### 3.5 Sözleşme–alan (K5)
`SozlesmeAlanlari` yeniden tanımlanır: `SozlesmeId, AlanId, KiralananAlan (varsayılan: alanın m²'si)`. `Lease.UnitId` kalkar. Tahakkuk m²'si satırların toplamıdır.

## 4. Doluluk kuralı (yeni çekirdek)

Bir alan, tarih aralığı çakışan açık bir sözleşmede (Active / Draft / RevisionRequested) **kendisi, bir atası veya bir torunu** yer alıyorsa kiralanamaz.
- Bütün kat kiralıysa altındaki ofisler kiralanamaz.
- Bir ofis kiralıysa kat bütün olarak kiralanamaz.
- Üst düğüm durumu: **Boş** / **Kısmen Dolu** (bazı torunlar dolu) / **Dolu** (kendisi veya atası kiralı, ya da tüm kiralanabilir torunları dolu).

Bugünkü `HasOverlappingActiveLeaseAsync` bu kurala genişletilir.

## 5. Ağaçla genelleşen hiyerarşiler (en yakın ata kazanır)

| Bugün | Yeni |
|---|---|
| `PropertyRateOverride` + `UnitRate` | `AlanTarifeleri (AlanId, KiraciKategoriId, BorcTipiId, ...)`; çözümleme: Sözleşme > alan > atalar > kök > `RateSchedule` |
| `PaymentStoreRouting` (Property? / Unit?) | `AlanId?` |
| `ReservationRateOverride.UnitId` | `AlanId` |
| `UserPermissionScope` (Property / Unit) | `AlanId`, alt düğümlere miras |
| `Invitation.PropertyIds / UnitIds` | `AlanIds` |
| `Charge.UnitId`, `Reservation.UnitId` | `AlanId` |

## 6. Kalkanlar

`UnitStructure`, `PropertyType.SupportsSingleUnit/SupportsMultipleUnits`, `UnitRentalMode`, `Unit.Capacity`, `UnitCapacityHistory`, `LeaseAreaMode`, `LeaseAreaHistory`, kontenjan önerisi ve kontenjan değiştirme ekranı, `PropertyStructurePolicy`'nin tek birim senkronu. `LeaseGroup` için bkz. açık nokta A1.

## 7. Açık noktalar (F0'da kapatılacak)

| # | Soru | Öneri |
|---|---|---|
| A1 | Paylaşımlı grup (`LeaseGroup`) kalacak mı? | **Karar (2026-11-25): Kalır.** `SozlesmeGruplari.BirimId` → `AlanId`. Grup tek bir alan üzerinde kurulur; `LeaseGroupPolicy` kuralları (en az 2 sözleşme, farklı kiracı, m² toplamı = alanın m²'si, ortak tarih) aynen geçerli. Doluluk kuralında (§4) grup tek kiracı gibi sayılır: gruptaki sözleşmeler birbirine çakışma sayılmaz, grup dışı sözleşmeler için alan dolu kabul edilir. |
| A2 | Adres / il / ilçe nerede durur? | Yeniden konuşulacak (bkz. §7.1). |
| A3 | Yetki kapsamı mirası | **Karar (2026-11-25):** Bir alana verilen kapsam tüm torunlarına geçer; atasına ve kardeşlerine geçmez. |
| A4 | Alan taşıma (move) | **Karar (2026-11-25): İlk sürümde yok.** Yanlış yere eklenen alan silinip/pasifleştirilip yeniden açılır. `Internal.Space.Move` izni ve taşıma ekranı sonraki sürüme kalır. |
| A5 | Alan silme | Geçmişi (sözleşme, tahakkuk, rezervasyon) olan alan silinmez, pasifleşir. Pasif üstün altında aktif alan olamaz. |
| A6 | Kiralanabilir üst düğümün m²'si | Elle girilir; torunların toplamından büyük olabilir (koridor vb.). Uyarı verilir, engellenmez. |
| A7 | Rezerve edilebilir alanın kiralanması | Bir tür hem kiralanabilir hem rezerve edilebilir olabilir mi? Öneri: hayır, ikisi ayrı. |
| A8 | Production'a uygulanmamış migration'lar (`AddSharedAndCapacityUnits`, `AddLeaseAreaModeAndHistory` vb.) ve commit edilmemiş açık ofis v2 Aşama 1 | Önce commit edilip yayın sırası kararlaştırılır; yeni yapı bunların üstüne gelir. |
| A9 | Ekran akışı | Taşınmaz listesi → alan ağacı ekranı (genişlet/daralt, düğüm ekle, düzenle). Sözleşmede `TreePicker` ile çoklu alan seçimi. |

## 8. Fazlar (her faz sonunda "devam" beklenir)

- **F0 – Spec:** bu doküman; açık noktalar kapanır.
- **F1 – Domain + şema:** `Space`, `SpaceType`, tür kuralları, özellik tabloları; `SpaceTreePolicy` (döngü engeli, tür kuralı, yol hesaplama), `SpaceOccupancyPolicy` (ata/torun çakışması). Migration ve dummy veri dönüşümü: Tasinmaz → kök, Birim → çocuk, kontenjanlı birim → N masa düğümü.
- **F2 – Sözleşme:** çoklu alan, ağaç çakışma kuralı, oluşturma/onay/uzatma/fesih; kontenjan ve `LeaseArea` mantığı silinir.
- **F3 – Tahakkuk + tarife:** alan tarifeleri, ata zinciriyle çözümleme, m² toplamı, `Charge.AlanId`.
- **F4 – Rezervasyon, ödeme yönlendirme, yetki kapsamı, davet.**
- **F5 – Ekranlar:** alan ağacı yönetimi, alan türü + kural + özellik tanımı ekranları, sözleşmede çoklu alan seçici, doluluk rozetleri, dashboard ve kiracı paneli.
- **F6 – Temizlik + doğrulama:** eski tablo/kolonlar düşer, tam test paketi, Yardım içeriği.

Yeni write endpoint'lerin her biri ayrı izin alır (ör. `Internal.Space.Create/Update/Delete`, `Internal.SpaceType.*`, `Internal.SpaceAttribute.*`).

## 9. Yeniden kullanılacaklar

- `TreePicker` bileşeni → alan seçici.
- `HasOverlappingActiveLeaseAsync` → ağaç kuralıyla genişletilir.
- `RateHierarchyLayer` tarife çözümlemesi → ata zinciri döngüsü.
- F8 doluluk rozetleri (Kısmen Dolu) → üst düğüm doluluğu.

## 10. Doğrulama

- Policy birim testleri: döngü engeli, tür kuralı, ata/torun çakışması, en yakın ata tarife çözümü.
- Integration: dönüşüm sonrası sözleşme oluşturma, tahakkuk üretimi, rezervasyon senaryoları yeşil.
- Tarayıcı: 4 seviyeli örnek ağaç, kat kiralanınca ofislerin kilitlenmesi, iki ofisli tek sözleşme ve tahakkuk tutarı.

## 11. Etki

`UnitId` / `PropertyId` `src` altında 160 dosyada 1382 yerde geçiyor (2026-11-24). Büyük iş; fazlar arası ara durumda derleme yeşil tutulur.
