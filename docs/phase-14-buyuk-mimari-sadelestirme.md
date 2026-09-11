# Büyük Mimari Sadeleştirme — Karar Dökümanı

> **Durum:** Tamamlandı ✅ (karar + uygulama). Uygulama detayları için `refaktor-asama-a-uygulama.md`, `refaktor-asama-b-uygulama.md`, `refaktor-asama-c-uygulama.md` dosyalarına ve `PROGRESS.md` Faz 14 bölümüne bak.
> **Karar Tarihi:** 2026-05-18
> **Not:** Bu dosya karar dökümanıdır; uygulanmamış taslak DEĞİLDİR. Güncel şema ve kod durumu için `PROGRESS.md` ve ilgili refactor dosyaları esas alınmalıdır.

---

## 1. Bağlam

KiraTakip projesinde 13 faz + 7 ara refactor sonrası şema 27 ana tabloya ulaştı. Aşağıdaki sorunlar tespit edildi:

- **Adlandırma tutarsızlıkları** (`SozlesmeRate.SozlesmeId` vs diğer yerlerde `KiraSozlesmesiId`)
- **Mükerrer alanlar** (`KiraSozlesmesi.Depozito` zaten tahakkuk kalemi olarak yazılıyor)
- **Semantik çakışmalar** (`RezervasyonUcretKural` + `RezervasyonGenelTarife` aynı ücret bilgisini tutuyor)
- **Aşırı normalizasyon** (`TasinmazTipiKiralamaSekli` ara tablosu 2 enum değeri için kurulmuş)
- **Kapak tabloları** (`Tarife` sadece `Yil` + `Aciklama` tutuyor)
- **Kanal-spesifik ödeme akışı** (`KiraOdeme` sadece manuel kayıtlar için)
- **Veritabanı okunabilirlik** (enum kolonları DBA için anlamsız)
- **Ölü UI** (`SozlesmeIslemGecmisi` sadece INSERT ediliyor, hiç gösterilmiyor)

Bu döküman, kullanıcıyla mutabakat sağlanan **uygulanacak** ve **reddedilen** kararları kayda alır. Uygulama bu dökümanda DEĞİL, ayrı aşama uygulama planlarında yapılacak.

---

## 2. Uygulanacak Kararlar

| # | Karar | Detay |
|---|---|---|
| 1 | `TasinmazTipiKiralamaSekli` ara tablosu kaldırılacak | `TasinmazTipi`'ne `TekParcaDestekli` + `BirimBazliDestekli` (bool) bayrakları eklenir. N:N ara tablo silinir. |
| 2 | Enum okunabilirlik standardı | (a) `EnumDegerleri` tablosu — start-up'ta reflection ile C# enum'ları auto-seed edilir. (b) Kritik enum kolonlarında EF Core `HasComment` — SQL Server extended property olarak yazılır. |
| 3 | Lookup tabloları birleşimi | `TasinmazTipi` + `KiraciKategori` + `Sektor` → ortak `Kategori` tablosunda `KategoriTipi` discriminator ile. `BorcTipi` ve `BirimTuru` (kendine özgü davranış alanları var) ayrı kalır. |
| 4 | `KiraSozlesmesi.Depozito` kolonu kaldırılacak | UI ilk tahakkuğun DEPOZITO kaleminden okur (snapshot). Sözleşme oluşturma akışı zaten ilk tahakkuğu otomatik üretiyor (`SozlesmeController.cs:222` → `_tahakkukUretim.UretSozlesmeIcinAsync`). |
| 5 | `Tarife` kapak tablosu kaldırılacak | `TarifeKalemi`'ne `Yil` (int) + `Aktif` (bool) kolonları eklenir. Yıl pasifleştirme `ExecuteUpdateAsync` ile batch update yapılır. |
| 6 | Rezervasyon tarife standardı | `RezervasyonUcret` yeni tablosunda da `Yil` (int) + `Aktif` (bool) kolonları (TarifeKalemi ile aynı standart). |
| 7 | `KiraOdeme` kanal-agnostik yapılacak | `OdemeKaynakTipi` enum eklenir (Manuel=1, BankaEslesme=2, SanalPos=3) + `PosReferansNo` (string?) eklenir. Sanal POS akışında otomatik `Durum=Onaylandi`. `OdemeKanali` (fiziksel yol) ile karıştırılmamalı. |
| 8 | Rezervasyon ücret tabloları birleşimi | `RezervasyonUcretKural` + `RezervasyonGenelTarife` → tek `RezervasyonUcret` tablosu. Alanlar: `BirimId?`, `BirimTuruId?`, `Yil?`, ücret alanları, `Aktif`. Check constraint: (a) `BirimId` dolu **veya** (b) `BirimTuruId`+`Yil` dolu. **Global fallback (hepsi null) YOK** — `Yil`+`BirimTuru` domain'i kaplıyor. |
| 9 | `SozlesmeIslemGecmisi` UI eklenecek | Tablo silinmiyor. Sözleşme detay sayfasına "İşlem Geçmişi" sekmesi eklenir; 6 işlem tipi (Olusturma/SureUzatma/Fesih/TufeArtis/KdvGuncelleme/TahakkukYenidenUretim) renkli badge ile gösterilir, eski→yeni değer diff'i listelenir. |
| 10 | `SozlesmeRate.SozlesmeId` → `KiraSozlesmesiId` rename | Diğer tüm FK'larla tutarlı adlandırma. Property + Fluent API + tüm referans yerleri rename. |
| 11 | `KiraSozlesmesi.KdvOrani` kolonu kaldırılacak | Her rate kendi KDV oranını taşıyor (`SozlesmeRate`, `BirimRate`, `TasinmazKiraciKategoriFiyat`, `TarifeKalemi`); snapshot `TahakkukKalemi.KdvOrani`'nda saklı. Eğer kodda fallback olarak kullanılan yer varsa rate tablolarda `KdvOrani` zorunlu (non-nullable) yapılır. `KdvUygulanacakMi` (bool) korunur — master switch. |

---

## 3. Reddedilen Kararlar

| # | Reddedilen | Sebep |
|---|---|---|
| R1 | Tüm enum'ları tek `Parametreler` tablosuna taşımak | God Table / Entity-Attribute-Value anti-pattern; EF Core type safety kaybı, sorgu performansı düşüşü, her sorguda `WHERE ParametreTipi='X'` zorunluluğu. Yerine: Karar #2 hibrit standardı. |
| R2 | 5 fiyatlandırma tablosunu (SozlesmeRate + BirimRate + TasinmazKiraciKategoriFiyat + Tarife + TarifeKalemi) tek tabloda birleştirmek | Polimorfik nullable FK karmaşıklığı; her biri farklı scope/iş kuralı temsil ediyor; check constraint orkestrasyonu mevcut yapıdan daha karmaşık olur. Sadece kapak `Tarife` kaldırılır (Karar #5). |
| R3 | Migration history collapse (32 → InitialCreate) | Refaktör bitince yeniden değerlendirilecek. |
| R4 | KVKK kapsamındaki `Kiraci` kişisel veri alanlarını temizlemek (AnneAdi, BabaAdi, DogumTarihi, DogumYeri, PasaportNo, MersisNo, Unvan) | İş kuralı: tüm alanlar korunacak. Şu an kullanılmasa bile gelecekte ihtiyaç olabilir; kullanıcı içeride zorunluluk kuralları yönetiyor. |
| R5 | KDV otoritesi tek yere indirgemek | Snapshot mantığı (`TahakkukKalemi.KdvOrani`) doğrudur — KDV oranı sözleşme süresince değişebilir, tahakkukta saklanmalı. Rate tablolarında her borç tipinin kendi KDV oranı olabilir. SADECE `KiraSozlesmesi.KdvOrani` kaldırılır (Karar #11); `KdvUygulanacakMi` master switch korunur. |
| R6 | `KiraTahakkuk.OdenenTutar` denormalizasyonunu kaldırmak | Performans için kalsın; raporlamada her tahakkuk için ödeme toplamı hesaplamak yerine snapshot tutuluyor. Mevcut servis bu alanı senkron tutuyor. |
| R7 | "Hepsi null = global" rezervasyon kuralı | `Yil`+`BirimTuru` zaten domain'i tam kaplıyor; her birim mutlaka bir `BirimTuru`'ya sahip, her yıl için BirimTuru tarifesi tanımlanmalı. Global kavramı gereksiz, kaldırıldı (Karar #8'in parçası). |

---

## 4. Uygulama Sırası

### Aşama A — Bağımsız, düşük risk, hızlı kazanç

1. **Karar #10:** `SozlesmeRate.SozlesmeId` → `KiraSozlesmesiId` rename
2. **Karar #4:** `KiraSozlesmesi.Depozito` kaldırma + UI'da DEPOZITO kalem okuma helper'ı
3. **Karar #11:** `KiraSozlesmesi.KdvOrani` kaldırma (fallback kullanım kontrolüyle)
4. **Karar #1:** `TasinmazTipiKiralamaSekli` → bayraklar
5. **Karar #5:** `Tarife` kaldırma + `TarifeKalemi.Yil` + `Aktif`
6. **Karar #2:** `EnumDegerleri` + `HasComment` standardı

### Aşama B — Yapısal, orta etki

7. **Karar #7:** `KiraOdeme` `OdemeKaynakTipi` + `PosReferansNo`
8. **Karar #9:** `SozlesmeIslemGecmisi` UI
9. **Karar #8 + #6:** `RezervasyonUcret` birleşimi (Yil+Aktif ile birlikte)

### Aşama C — Büyük cerrahi

10. **Karar #3:** `Kategori` lookup birleşimi (`TasinmazTipi`+`KiraciKategori`+`Sektor`)

Her aşama:
- Ayrı commit
- Ayrı migration
- Manuel doğrulama
- Mevcut dummy veriyle regresyon testi

---

## 5. Doğrulama Kriterleri

Her karar uygulandıktan sonra:

- `dotnet build` → 0 hata
- `dotnet ef migrations add <ad>` → temiz migration üretimi
- `dotnet ef database update` → DB'ye başarılı uygulama
- Manuel UI testi: ilgili akış (sözleşme oluşturma, tahakkuk üretimi, rezervasyon, ödeme, vb.) çalışıyor
- Mevcut dummy verilerle regresyon kontrolü

---

## 6. İlişkili Belgeler

- [`MASTER-PLAN.md`](MASTER-PLAN.md) — bu refaktöre atıf eklenecek
- [`PROGRESS.md`](PROGRESS.md) — yeni aşama olarak takip edilecek
- [`refactor-tasinmaz-tipi-kiralama-sekli.md`](refactor-tasinmaz-tipi-kiralama-sekli.md) — Karar #1 ile ilişkili eski refactor
- [`refactor-rezervasyon-yillik-genel-tarife.md`](refactor-rezervasyon-yillik-genel-tarife.md) — Karar #8 ile ilişkili eski refactor
- [`refactor-sozlesme-rate-akisi.md`](refactor-sozlesme-rate-akisi.md) — Karar #10 ile ilişkili eski refactor
- [`phase-9-fiyatlandirma-mimarisi.md`](phase-9-fiyatlandirma-mimarisi.md) — Fiyatlandırma resolver mimarisi referansı
- Uygulama planları sırası geldiğinde ayrı dosyalarda yazılacak (öneri: `refaktor-asama-a-uygulama.md`, `refaktor-asama-b-uygulama.md`, `refaktor-asama-c-uygulama.md`)

---

## 7. Notlar

- Bu döküman **karar kayıtları**dır. Uygulama detayları, migration adımları, kod örnekleri **bu dökümanda yer almaz** — onlar her aşamanın uygulama planında yazılacak.
- Reddedilen kararlar (R1–R7) gelecekte tekrar tartışmaya açılırsa, sebepleri burada kayıtlı; tekrar aynı tartışmanın açılmaması için referans.
- Aşamalar bağımsız — birinden vazgeçilse diğerleri uygulanabilir, sıra esnek.
