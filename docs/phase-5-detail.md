# Faz 5 — Ödeme Takip Modülü: Detay Görevler

## Kararlaştırılan Tasarım Kararları

| Konu | Karar |
|------|-------|
| Tahakkuk oluşturma | Manuel tetik (UI butonu, background job yok) |
| Dekont depolama | Sunucu diski, `wwwroot` dışı (`Storage/Dekontlar/`), GUID isim, controller üzerinden servis |
| Dosya boyutu sınırı | 5 MB |
| Banka formatı | Akbank CSV (ileride genişletilebilir `IBankaHareketiParser` soyutlamasıyla) |
| Kısmi ödeme | Bir tahakkuğa birden fazla ödeme bağlanabilir |
| Gecikme kuralı | VadeTarihi geçti ve tam ödeme yok → Gecikti |
| DB dosya saklama | HAYIR — disk + controller-served yeterli |

---

## Domain Modeli (Kesinleşmiş)

### `KiraTahakkuk`
```
Id                  int PK
KiraSozlesmesiId    int FK → KiraSozlesmesi
DonemBaslangic      DateTime
DonemBitis          DateTime
VadeTarihi          DateTime  (varsayılan: DonemBaslangic)
BeklenenTutar       decimal(18,2)
KdvTutari           decimal(18,2)?
ToplamTutar         decimal(18,2)
OdenenTutar         decimal(18,2)  (denormalize — güncellenir)
Durum               TahakkukDurumu
OlusturmaTarihi     DateTime
```

### `KiraOdeme`
```
Id                  int PK
KiraTahakkukId      int FK → KiraTahakkuk
KiraSozlesmesiId    int FK → KiraSozlesmesi  (denormalize)
OdemeTarihi         DateTime
Tutar               decimal(18,2)
OdemeKanali         OdemeKanali
Aciklama            string?
Durum               OdemeDurumu
GirenUserId         string FK → AspNetUsers
GirisTarihi         DateTime
OnaylayanUserId     string? FK → AspNetUsers
OnayTarihi          DateTime?
RedNedeni           string?
```

### `Dekont`
```
Id                  int PK
KiraOdemeId         int FK → KiraOdeme
OrijinalDosyaAdi    string
DiskDosyaAdi        string  (GUID tabanlı)
DosyaYolu           string  (Storage/Dekontlar/ altında relatif)
DosyaTipi           string  (mime type)
DosyaBoyutu         long
YukleyenUserId      string FK → AspNetUsers
YuklemeTarihi       DateTime
```

### `BankaHareketi`
```
Id                  int PK
ImportBatchId       Guid
HareketTarihi       DateTime
Tutar               decimal(18,2)
Aciklama            string
KarsiHesap          string?
KarsiUnvan          string?
Bakiye              decimal(18,2)?
BankaKodu           string  ("AKBANK")
EslesmeDurumu       BankaEslesmeDurumu
ImportTarihi        DateTime
ImportEdenUserId    string FK → AspNetUsers
```

### `OdemeBankaEslesme`
```
Id                  int PK
KiraOdemeId         int FK → KiraOdeme
BankaHareketiId     int FK → BankaHareketi
EslesmeTipi         EslesmeTipi
EslestirenUserId    string? FK → AspNetUsers
EslesmeTarihi       DateTime
```

### Enum'lar
```csharp
TahakkukDurumu   : Bekleniyor, KismenOdendi, TamOdendi, Gecikti
OdemeDurumu      : OnayBekliyor, Onaylandi, Reddedildi
OdemeKanali      : Havale, EFT, Nakit, Diger
BankaEslesmeDurumu : Eslestirilmedi, Eslesti, ManuelEslesti
EslesmeTipi      : Otomatik, Manuel
```

---

## Gecikme Durumu Güncelleme Kuralı

Background job yok → durum güncellemesi iki noktada tetiklenir:
1. Tahakkuk listesi açıldığında (servis katmanında toplu kontrol)
2. Ödeme onaylandığında / reddedildiğinde

```
Kural:
  TamOdendi → değişmez
  today >= VadeTarihi:
    OdenenTutar == 0     → Gecikti
    OdenenTutar < Toplam → Gecikti (kısmi + gecikmiş)
    OdenenTutar >= Toplam → TamOdendi
```

---

## Dekont Depolama Yapısı

```
[ProjectRoot]/
  Storage/
    Dekontlar/
      {sozlesmeId}/
        {GUID}.pdf
        {GUID}.jpg
```

- `Storage/` klasörü `wwwroot` dışında
- `appsettings.json`'a `"DekontStoragePath": "Storage/Dekontlar"` eklenir
- Erişim: `GET /Odeme/Dekont/{dekontId}` → yetki kontrolü → `PhysicalFile()` döner
- Goruntuleyici için ek kontrol: ödemenin taşınmazı yetkili listede olmalı

---

## Banka Soyutlaması

```
Services/Banka/
  IBankaHareketiParser.cs      → banka-spesifik kolon mapping arayüzü
  AkbankCsvParser.cs           → ilk implementasyon
  BankaHareketiImportService.cs → CSV okuma + parser seçimi
```

`IBankaHareketiParser`:
```csharp
string BankaKodu { get; }
IEnumerable<BankaHareketi> Parse(Stream csv, Guid batchId, string userId);
```

Yeni banka eklemek için sadece `IBankaHareketiParser` implemente edilip DI'ya kayıt edilir.

---

## Eşleştirme Akışı (Faz 5.6 UI Eki)

### Karar
**Manuel eşleştirme + akıllı öneri.** Sessiz otomatik eşleştirme yok — para işinde yanlış bağlama tehlikeli. `EslesmeTipi.Otomatik` enum değeri ileride kullanılmak üzere saklı; Faz 5'te yalnızca `Manuel` üretilir.

### İki Giriş Noktası (Tek Akış)

**B — Banka Hareketleri sayfası (birincil)**
- CSV import sonrası doğal akış
- `Views/BankaHareketi/Index.cshtml`: eşleşmemiş satırlarda "Eşleştir" butonu
- Modal/sayfa: aday `KiraOdeme` listesi (heuristik sıralı)
- Kullanıcı seçer → `POST /BankaHareketi/Eslestir(odemeId, bankaHareketiId)`

**A — Ödeme detayı (tamamlayıcı)**
- "Önce ödeme girildi, sonra ekstre geldi" senaryosu
- `Views/Odeme/Detay.cshtml` "Banka Eşleşmeleri" kartının üstünde "Banka hareketi eşleştir" butonu
- Modal: aynı heuristikle eşleşmemiş `BankaHareketi` listesi
- Aynı POST endpoint'ine düşer

### Aday Sıralama Heuristik'i

Modalda gösterilen aday liste şu sırayla:

```
1. Tutar tam eşleşme + |HareketTarihi - OdemeTarihi| ≤ 15 gün
2. Tutar tam eşleşme + tarih farkı > 15 gün
3. Tutar farkı ≤ %2 + 15 gün penceresi  (kuruş yuvarlama)
4. Aday yoksa: tüm eşleşmemiş kayıtlar, tutar farkına göre artan
```

- Filtre kapsamı (B): `OdemeDurumu IN (OnayBekliyor, Onaylandi)` ve henüz `OdemeBankaEslesme` kaydı olmayan ödemeler
- Filtre kapsamı (A): `EslesmeDurumu = Eslestirilmedi` olan banka hareketleri
- Goruntuleyici rolünde aday liste yetkili taşınmaz ödemeleri ile sınırlı

### Eşleşme Çözme
- "Banka Eşleşmeleri" kartında her eşleşme yanına "Çöz" butonu
- `POST /BankaHareketi/EslesmeCoz(eslesmeId)` — mevcut endpoint, UI eksik

### Yetki
- Eşleştirme + çözme: `Odeme.MatchBankTransaction` permission
- Goruntuleyici rolü: aday liste filtreli, eşleştirme butonu gizli

---

## Detay Görevler

### 5.1 — Domain entity'leri + enum'lar
- [ ] `Models/KiraTahakkuk.cs`
- [ ] `Models/KiraOdeme.cs`
- [ ] `Models/Dekont.cs`
- [ ] `Models/BankaHareketi.cs`
- [ ] `Models/OdemeBankaEslesme.cs`
- [ ] Enum'lar `Models/Enums/OdemeEnums.cs` içine

### 5.2 — DbContext + migration
- [ ] `ApplicationDbContext`'e 5 DbSet ekle
- [ ] `OnModelCreating`: FK'lar, precision'lar, index'ler
- [ ] Migration oluştur ve uygula

### 5.3 — Servis interface'leri
- [ ] `ITahakkukService`
- [ ] `IOdemeService`
- [ ] `IDekontService`
- [ ] `IBankaHareketiService`
- [ ] `IBankaHareketiParser` (banka soyutlaması)

### 5.4 — Servis implementasyonları
- [ ] `TahakkukService` (manuel oluşturma + gecikme güncelleme)
- [ ] `OdemeService` (CRUD, onay/red, tahakkuk OdenenTutar güncelleme)
- [ ] `DekontService` (dosya kaydet/sil/sun)
- [ ] `BankaHareketiService` (import, eşleştirme)
- [ ] `AkbankCsvParser`

### 5.5 — DI kayıtları + appsettings
- [ ] `Program.cs`'e servis kayıtları
- [ ] `appsettings.json`'a `DekontStoragePath` + `MaxDekontFileSizeMb`
- [ ] `Storage/Dekontlar/` klasörü oluştur

### 5.6 — Controller + view'lar
- [x] `TahakkukController`: Index, Olustur (manuel tetik)
- [x] `OdemeController`: Index, Ekle, Onayla, Reddet, Dekont görüntüle
- [x] `BankaHareketiController`: Index, Import, Eslestir, EslesmeCoz (endpoint'ler)
- [x] İlgili view'lar (temel)
- [ ] **Eşleştirme UI (eksik)**:
    - [ ] `Views/BankaHareketi/Index.cshtml`: eşleşmemiş satıra "Eşleştir" butonu + aday ödeme modal'ı
    - [ ] `Views/Odeme/Detay.cshtml`: "Banka hareketi eşleştir" butonu + aday hareket modal'ı
    - [ ] `Views/Odeme/Detay.cshtml`: mevcut eşleşmelere "Çöz" butonu (`POST EslesmeCoz`)
    - [ ] Aday sıralama heuristik'i servis katmanında (`IBankaHareketiService.GetEslesmeAdaylariAsync`)

### 5.7 — Sözleşme detay entegrasyonu
- [ ] `Sozlesme/Detay` sayfasına tahakkuk + ödeme özeti widget'ı

### 5.8 — Dashboard KPI'ları
- [ ] Bu ay beklenen tahsilat
- [ ] Geciken ödemeler (tutar + adet)
- [ ] Onay bekleyen ödemeler
- [ ] Eşleşmemiş banka hareketleri

### 5.9 — Test
- [x] Tahakkuk oluşturma + gecikme geçişi
- [x] Kısmi ödeme akışı
- [x] Onay/red akışı
- [x] CSV import (parser + DB kayıt)
- [ ] **Eşleştirme akışı UI** (5.6 eklenince yeniden test) — B akışı, A akışı, Çöz, aday heuristiği, Goruntuleyici filtre
- [x] Goruntuleyici kısıt kontrolü
