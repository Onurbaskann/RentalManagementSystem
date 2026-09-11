# Faz 8 — Parametre Yönetimi, Toplantı Salonu Rezervasyonu ve Manuel Borç

> **GÜNCELLİK NOTU:** Bu fazda tanımlanan "Taşınmaz Kategori Çarpanı" (Bölüm 9), Faz 9 ile birlikte yerini tüm borç tiplerini kapsayan **TasinmazKiraciKategoriFiyat** matrisine bırakmıştır. Güncel fiyatlandırma için **phase-9-fiyatlandirma-mimarisi.md** okunmalıdır.

> **Hedef:** Sistemde kullanılan sabit/parametrik değerleri yönetilebilir hale getirmek; birim türleri, kiracı kategorileri, sektörler, taşınmaz kategori çarpanları, toplantı salonu rezervasyonu ve manuel borç ekleme altyapısını kurmak.
>
> Bu faz, Faz 7’de tamamlanan çok kalemli tahakkuk altyapısının üzerine inşa edilir.
>
> Sistem üzerinden ödeme yapılmayacaktır. Bu faz ödeme alma değil; rezervasyon, ücret hesaplama ve borç/tahakkuk oluşturma süreçlerini kapsar.

---

## 1. Kapsam

Bu faz kapsamında aşağıdaki özellikler geliştirilecektir:

- Parametre / tanım ekranları
- Birim türleri
- Kiracı üst kategorileri
- Kiracı sektör bilgisi
- Taşınmaz bazlı kategori çarpanları
- Ofis kira hesaplamasında kategori çarpanı kullanımı
- Toplantı salonu rezervasyon takibi
- Toplantı salonu ücretsiz kullanım süresi
- Ücretsiz süre aşımı sonrası ücretlendirme
- Manuel borç ekleme
- Manuel borcun tahakkuk kalemleriyle entegrasyonu
- Permission entegrasyonu
- Dashboard / raporlama etkileri

---

## 2. Kapsam Dışı

Bu faz kapsamında aşağıdakiler yapılmayacaktır:

- Online ödeme alma
- Kiracı portalı
- Banka API entegrasyonu
- Yeni ödeme altyapısı tasarımı
- Mevcut ödeme modülünün baştan yazılması
- Multi-tenant yapı
- E-fatura / e-arşiv entegrasyonu
- Harici rezervasyon sistemi entegrasyonu

---

## 3. Önkoşullar

Bu faza başlamadan önce aşağıdaki fazlar tamamlanmış kabul edilir:

- Faz 1 — Permission Modeli
- Faz 2 — SQL Server Tam Geçiş
- Faz 3 — Domain servislerinin EF Core’a taşınması
- Faz 4 — Permission implementasyonu
- Faz 5 — Ödeme takip modülü
- Faz 6 — Tablo & UX iyileştirmeleri (UI altyapısı için)
- Faz 7 — Çok kalemli aylık tahakkuk

Bu faz, mevcut SQL Server + EF Core + servis katmanı mimarisini koruyacaktır.

---

## 4. Genel Mimari Kararlar

- Yeni sabitler kod içi enum olarak gömülmeyecektir.
- Yönetilebilir parametreler SQL Server tablolarında tutulacaktır.
- Parametre ekranları Admin tarafından yönetilecektir.
- Permission kontrolleri mevcut claims/policy altyapısına uyacaktır.
- Controller’lar mümkün olduğunca servis arayüzleri üzerinden çalışacaktır.
- Mevcut UI dili Tailwind / Magic UI premium SaaS estetiğini koruyacaktır.
- Bootstrap veya generic MVC görünümü kullanılmayacaktır.

---

## 5. Parametre Yönetimi

Sistemde kullanıcıya gösterilen ve yönetilebilir olması gereken sabitler için merkezi parametre yönetimi ekranları oluşturulacaktır.

### 5.1 Parametre Ana Ekranı

Route önerisi:

```text
/Admin/Parametreler
```

Bu ekran altında sekmeli yapı kullanılabilir:

- Taşınmaz Tipleri
- Birim Türleri
- Kiracı Kategorileri
- Sektörler
- Rezervasyon Ücret Kuralları

Alternatif olarak her parametre için ayrı admin controller oluşturulabilir.

Tercih edilen basit yapı:

```text
/Admin/TasinmazTipi
/Admin/BirimTuru
/Admin/KiraciKategori
/Admin/Sektor
/Admin/RezervasyonUcretKural
```

---

## 6. Birim Türleri

Birim türleri, taşınmaz içindeki kiralanabilir veya rezervasyon yapılabilir alt alanları temsil eder. Otomat, Bankamatik ve Depo gibi bağımsız taşınmaz türleri artık `TasinmazTipi` tablosunda yönetilir (bkz. Bölüm 6a).

Örnek kiralanabilir birim türleri:

- Ofis
- Restoran
- Dükkan
- Stand
- Kiosk
- Diğer

Örnek rezervasyon alanı türleri:

- Toplantı Salonu
- Etkinlik Alanı
- Konferans Salonu
- Eğitim Salonu
- Çok Amaçlı Salon

### 6.1 Yeni Entity: `BirimTuru`

```csharp
public class BirimTuru
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Kod { get; set; } = string.Empty;

    public bool KiralanabilirMi { get; set; } = true;

    public bool RezervasyonYapilabilirMi { get; set; } = false;

    public bool Aktif { get; set; } = true;

    public int Sira { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}
```

### 6.2 Birim Modeli Güncellemesi

`Birim` modeline aşağıdaki alan eklenecektir:

```csharp
public int? BirimTuruId { get; set; }

public BirimTuru? BirimTuru { get; set; }
```

### 6.3 Seed Birim Türleri

Seed olarak eklenecek kayıtlar:

**Kiralanabilir birim türleri** (`KiralanabilirMi = true`, `RezervasyonYapilabilirMi = false`):

| Kod | Ad | Sıra |
|---|---|---:|
| OFIS | Ofis | 1 |
| RESTORAN | Restoran | 2 |
| DUKKAN | Dükkan | 3 |
| STAND | Stand | 4 |
| KIOSK | Kiosk | 5 |
| DIGER | Diğer | 99 |

**Rezervasyon alanı türleri** (`KiralanabilirMi = false`, `RezervasyonYapilabilirMi = true`):

| Kod | Ad | Sıra |
|---|---|---:|
| TOPLANTI | Toplantı Salonu | 10 |
| ETKINLIK | Etkinlik Alanı | 11 |
| KONFERANS | Konferans Salonu | 12 |
| EGITIM | Eğitim Salonu | 13 |
| COKAMACLI | Çok Amaçlı Salon | 14 |

> **Veri migrasyonu:** Mevcut seed'de yer alan OTOMAT, BANKAMATIK, DEPO kayıtları `TasinmazTipi` tablosuna taşınmalı ve `BirimTuru` tablosundan silinmelidir (bkz. Bölüm 6a.5).

### 6.4 Kurallar

- Birim türü pasif yapılabilir.
- Pasif birim türü yeni birim oluştururken seçilemez.
- Var olan birimlerde geçmiş veri bozulmaz.
- Birim bazlı taşınmazlarda varsayılan birim türü `KiralanabilirMi = true` olan bir tür olmalıdır.
- Rezervasyon modülü sadece `RezervasyonYapilabilirMi = true` olan birim türlerine sahip birimleri listeler.
- Sözleşme oluşturma ekranında sadece `KiralanabilirMi = true` olan birim türlerine sahip birimler listelenir.
- Kira doluluk oranı hesabına sadece `KiralanabilirMi = true` birimler dahil edilir; rezervasyon alanları dahil edilmez.

---

## 6a. Taşınmaz Tipleri Parametresi

`Tasinmaz.Tipi` enum alanı kaldırılarak `TasinmazTipi` parametre tablosuna geçilmektedir. Bu değişiklik admin tarafından dinamik olarak yönetilebilen taşınmaz tipleri sunar.

### 6a.1 Yeni Entity: `TasinmazTipi`

```csharp
public class TasinmazTipi
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Kod { get; set; } = string.Empty;

    public bool Aktif { get; set; } = true;

    public int Sira { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}
```

### 6a.2 Taşınmaz Modeli Güncellemesi

Mevcut `Tipi` enum alanı kaldırılır; yerine `TasinmazTipiId` FK eklenir:

```csharp
// Kaldırılacak:
// public TasinmazTipi Tipi { get; set; }  (enum)

// Eklenecek:
public int? TasinmazTipiId { get; set; }
public TasinmazTipi? TasinmazTipi { get; set; }
```

> **İsim çakışması:** Mevcut `TasinmazTipi` enum adı silinecek; entity sınıfı aynı adı alacaktır.

### 6a.3 Seed Taşınmaz Tipleri

| Kod | Ad | Sıra |
|---|---|---:|
| BINA | Bina | 1 |
| ARAZI | Arazi | 2 |
| TARLA | Tarla | 3 |
| DEPO | Depo | 4 |
| OTOMAT | Otomat | 5 |
| BANKAMATIK | Bankamatik | 6 |
| KANTIN | Kantin | 7 |
| DIGER | Diğer | 99 |

### 6a.4 Admin Ekranı

Route: `/Admin/TasinmazTipi`

Basit tablo yönetimi: ekle / düzenle / aktif-pasif. Diğer parametre ekranlarıyla aynı yapı.

### 6a.5 Veri Migrasyonu

Mevcut `Tasinmaz.Tipi` enum değerleri yeni `TasinmazTipiId` FK'ya dönüştürülmelidir. EF Core migration içinde `MigrationBuilder.Sql()` ile aşağıdaki eşleştirme yapılır:

```sql
-- Önce seed kayıtlarının ID'leri oluşturulduktan sonra çalıştırılır
UPDATE Tasinmazlar SET TasinmazTipiId = (
    SELECT Id FROM TasinmazTipleri WHERE Kod = CASE Tipi
        WHEN 1 THEN 'BINA'
        WHEN 2 THEN 'ARAZI'
        WHEN 3 THEN 'TARLA'
        WHEN 4 THEN 'DEPO'
        WHEN 5 THEN 'DIGER'
    END
)
```

Eski `Tipi` kolonu migration sonunda `DropColumn` ile kaldırılır.

### 6a.6 Kurallar

- `TasinmazTipiId` nullable FK; migration sırasında geçici olarak boş kalabilir.
- Admin formunda sadece aktif tipler selectbox'ta gösterilir.
- Toplantı salonu / rezervasyon alanı kavramları birim türüyle yönetilir, taşınmaz tipiyle değil.

---

## 6b. KiralamaSekli Terminoloji ve Rezervasyon Alanları

### 6b.1 OfisBazli → BirimBazli

Mevcut `KiralamaSekli` enum'undaki `OfisBazli` değeri, "Birim Bazlı" olarak yeniden adlandırılacaktır.

> **Geçiş notu:** Kod içinde `OfisBazli` enum değeri geçici olarak korunabilir. UI dili ve tüm yeni dokümanlarda "Birim Bazlı" kullanılır. OfisBazli → BirimBazli kod rename'i 8.2.7 kapsamında yapılır; kırıcı değişiklik riski düşüktür.

Taşınmaz Ekle / Düzenle formunda:

- Eski: "Ofis Bazlı"
- Yeni: "Birim Bazlı"

### 6b.2 Taşınmaz Ekle Formuna Rezervasyon Alanları Bölümü

Taşınmaz Ekle formuna "Rezervasyon Alanları" başlıklı yeni bir bölüm eklenir. Bu bölüm kiralanabilir birim bölümünden bağımsızdır; kiralama şeklinden bağımsız olarak görünür.

Rezervasyon alanı giriş alanları:

| Alan | Tip | Zorunlu | Not |
|---|---|---|---|
| Alan adı | Text | Evet | Örn: Toplantı Salonu A |
| Birim türü | Select | Hayır | `RezervasyonYapilabilirMi = true` türler |
| Kapasite | Number | Hayır | Kişi sayısı |
| Kat no | Number | Hayır | |
| Açıklama | Text | Hayır | |

### 6b.3 Ayrım Kuralı

- **Kiralanabilir birimler:** sözleşmeye bağlanır, tahakkuk üretimine dahil edilir, doluluk hesabına girer.
- **Rezervasyon alanları:** sözleşmeye bağlanmaz, rezervasyon kaydı ile kullanılır, doluluk hesabına dahil edilmez.

Bu ayrım `BirimTuru.KiralanabilirMi` ve `BirimTuru.RezervasyonYapilabilirMi` bayrakları üzerinden yapılır.

---

## 7. Kiracı Üst Kategorileri

Kiracılara üst kategori eklenecektir.

Örnek kategoriler:

- Akademisyen
- Akademisyen Olmayan
- Firma
- Kamu Kurumu
- Diğer

Bu kategoriler taşınmaz bazlı çarpan tanımlarında kullanılacaktır.

### 7.1 Yeni Entity: `KiraciKategori`

```csharp
public class KiraciKategori
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Kod { get; set; } = string.Empty;

    public bool Aktif { get; set; } = true;

    public int Sira { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}
```

### 7.2 Kiracı Modeli Güncellemesi

`Kiraci` modeline aşağıdaki alanlar eklenecektir:

```csharp
public int? KiraciKategoriId { get; set; }

public KiraciKategori? KiraciKategori { get; set; }
```

### 7.3 Kurallar

- Kiracı kategorisi selectbox ile seçilir.
- Pasif kategoriler yeni kiracı oluştururken seçilemez.
- Var olan kiracılarda kategori boş olabilir.
- Kira çarpanı hesaplamasında kategori yoksa sistem fallback tarifeye döner.

---

## 8. Kiracı Sektör Bilgisi

Kiracı tanımına sektör bilgisi eklenecektir.

Örnek sektörler:

- Yazılım
- Lojistik
- Gıda
- Tarım
- Finans
- Eğitim
- Kamu
- Diğer

### 8.1 Yeni Entity: `Sektor`

```csharp
public class Sektor
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Kod { get; set; } = string.Empty;

    public bool Aktif { get; set; } = true;

    public int Sira { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}
```

### 8.2 Kiracı Modeli Güncellemesi

`Kiraci` modeline aşağıdaki alanlar eklenecektir:

```csharp
public int? SektorId { get; set; }

public Sektor? Sektor { get; set; }
```

### 8.3 Kiracı Form Güncellemesi

Kiracı ekleme/düzenleme formlarına aşağıdaki selectbox’lar eklenecektir:

- Kiracı Kategorisi
- Sektör

Kurallar:

- Sektör opsiyonel olabilir.
- Kiracı kategorisi çarpan hesaplamaları için önemlidir.
- Gerçek/Tüzel kiracı form davranışı bozulmamalıdır.

---

## 9. Taşınmaz Bazlı Kategori Çarpanları

Her taşınmaz altında kiracı kategori bazında kira çarpanı tanımlanabilecektir.

Örnek:

| Taşınmaz | Kiracı Kategorisi | Çarpan |
|---|---|---:|
| Teknokent A Blok | Akademisyen | 120 |
| Teknokent A Blok | Akademisyen Olmayan | 180 |
| Sanayi Sitesi B Blok | Firma | 250 |

### 9.1 Yeni Entity: `TasinmazKategoriCarpan`

```csharp
public class TasinmazKategoriCarpan
{
    public int Id { get; set; }

    public int TasinmazId { get; set; }

    public Tasinmaz Tasinmaz { get; set; }

    public int KiraciKategoriId { get; set; }

    public KiraciKategori KiraciKategori { get; set; }

    public decimal Carpan { get; set; }

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; }

    public string? Aciklama { get; set; }
}
```

### 9.2 Unique Kural

Aynı taşınmaz için aynı kiracı kategorisinden sadece bir aktif çarpan olmalıdır.

Önerilen unique index:

```text
TasinmazId + KiraciKategoriId
```

### 9.3 Formül

Ofis kira hesaplamasında kullanılacak temel formül:

```text
Kira Tutarı = Kiralanan Ofis m² × Çarpan
```

Örnek:

```text
Ofis m²: 45
Kategori: Akademisyen
Çarpan: 120

Kira: 45 × 120 = 5.400 ₺
```

### 9.4 Kullanım Kapsamı

Bu çarpan öncelikli olarak `KiralanabilirMi = true` olan birimlerde kira hesaplaması için kullanılacaktır.

Rezervasyon alanlarında (`RezervasyonYapilabilirMi = true`) çarpan kullanımı kapsam dışıdır.

---

## 10. Rate Resolver Entegrasyonu

Faz 7’de bulunan rate resolve mantığı genişletilecektir.

Mevcut resolve sırası korunur; taşınmaz kategori çarpanı uygun noktaya eklenir.

Önerilen yeni sıra:

```text
1. SozlesmeRate
2. BirimRate
3. TasinmazKategoriCarpan
4. TarifeKalemi
```

### 10.1 Kural

`TasinmazKategoriCarpan` sadece şu durumda kullanılır:

- Borç tipi `KIRA`
- Sözleşmenin kiracısında `KiraciKategoriId` vardır
- Sözleşmenin birimi bir taşınmaza bağlıdır
- O taşınmaz + kiracı kategorisi için aktif çarpan vardır
- Birim m² değeri sıfırdan büyüktür

### 10.2 Hesaplama

```text
Tutar = Birim.Yuzolcumu × TasinmazKategoriCarpan.Carpan
```

KDV varsa mevcut kalem bazlı KDV hesaplama mantığı korunur.

### 10.3 Snapshot

Tahakkuk kalemi oluşturulurken aşağıdaki değerler snapshot olarak yazılmalıdır:

- HesaplamaYontemi: M2
- BirimDeger: Birim m²
- Carpan: Çarpan değeri
- KaynakTipi: TasinmazKategoriCarpan
- Tutar
- KDV oranı
- KDV tutarı
- Toplam tutar

---

## 11. Manuel Borç Ekleme

Otomatik tahakkuklara ek olarak manuel borç eklenebilmelidir.

Örnek manuel borçlar:

- Ek hizmet bedeli
- Hasar bedeli
- Anahtar / kart bedeli
- Geçici kullanım bedeli
- Toplantı salonu süre aşım bedeli
- Diğer

### 11.1 Temel Karar

Manuel borç, mevcut tahakkuk sisteminden ayrı kopuk bir kayıt olmamalıdır.

Manuel borç, mümkünse `KiraTahakkuk` ve `TahakkukKalemi` yapısıyla entegre olmalıdır.

### 11.2 Önerilen Yaklaşım

Manuel borç için yeni bir tahakkuk oluşturulabilir veya mevcut döneme ek kalem eklenebilir.

Tercih edilen ilk sürüm:

```text
Manuel borç girildiğinde ayrı bir KiraTahakkuk oluşturulur.
Bu tahakkuk tek veya çok kalemli olabilir.
Tahakkuk tipi "Manuel" olarak işaretlenir.
```

### 11.3 KiraTahakkuk Modeli Güncellemesi

`KiraTahakkuk` modeline tahakkuk kaynağı eklenmelidir.

```csharp
public TahakkukKaynakTipi KaynakTipi { get; set; }
```

Yeni enum:

```csharp
public enum TahakkukKaynakTipi
{
    Otomatik = 1,
    Manuel = 2,
    Rezervasyon = 3
}
```

### 11.4 Manuel Borç Form Alanları

Manuel borç ekleme formunda şu alanlar bulunmalıdır:

- Sözleşme
- Kiracı
- Birim / Taşınmaz bilgisi
- Borç tipi
- Açıklama
- Tutar
- KDV uygulanacak mı?
- KDV oranı
- Vade tarihi
- Not

### 11.5 Kurallar

- Manuel borç sadece yetkili kullanıcı tarafından eklenebilir.
- Tutar sıfırdan büyük olmalıdır.
- Vade tarihi zorunludur.
- Manuel borç ödeme modülünde normal tahakkuk gibi takip edilir.
- Manuel borç iptal edilebilir ama fiziksel silme yapılmamalıdır.
- Ödeme alınmış manuel borç iptal edilememelidir.

---

## 12. Rezervasyon Alanları Takibi

Rezervasyon alanları, `BirimTuru.RezervasyonYapilabilirMi = true` olan birimlerdir (Toplantı Salonu, Etkinlik Alanı, Konferans Salonu vb.).

Bu birimler için rezervasyon takibi yapılacaktır.

### 12.1 Yeni Entity: `ToplantiSalonuRezervasyon`

```csharp
public class ToplantiSalonuRezervasyon
{
    public int Id { get; set; }

    public int BirimId { get; set; }

    public Birim Birim { get; set; }

    public int KiraciId { get; set; }

    public Kiraci Kiraci { get; set; }

    public int? KiraSozlesmesiId { get; set; }

    public KiraSozlesmesi? KiraSozlesmesi { get; set; }

    public DateTime BaslangicTarihi { get; set; }

    public DateTime BitisTarihi { get; set; }

    public int ToplamSureDakika { get; set; }

    public int UcretsizSureDakika { get; set; }

    public int UcretliSureDakika { get; set; }

    public decimal BirimUcret { get; set; }

    public decimal UcretTutar { get; set; }

    public decimal? KdvOrani { get; set; }

    public decimal? KdvTutari { get; set; }

    public decimal ToplamTutar { get; set; }

    public RezervasyonDurumu Durum { get; set; }

    public int? KiraTahakkukId { get; set; }

    public KiraTahakkuk? KiraTahakkuk { get; set; }

    public string? Aciklama { get; set; }

    public string OlusturanUserId { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }
}
```

### 12.2 Enum: `RezervasyonDurumu`

```csharp
public enum RezervasyonDurumu
{
    Planlandi = 1,
    Tamamlandi = 2,
    IptalEdildi = 3,
    TahakkukaAktarildi = 4
}
```

---

## 13. Rezervasyon Ücret Kuralları

Toplantı salonu ücretlendirme kuralları yönetilebilir olmalıdır.

### 13.1 Yeni Entity: `RezervasyonUcretKural`

```csharp
public class RezervasyonUcretKural
{
    public int Id { get; set; }

    public int? BirimId { get; set; }

    public Birim? Birim { get; set; }

    public int UcretsizSureDakika { get; set; }

    public int UcretlendirmePeriyoduDakika { get; set; }

    public decimal PeriyotUcreti { get; set; }

    public decimal KdvOrani { get; set; } = 20;

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; }

    public string? Aciklama { get; set; }
}
```

### 13.2 Kural Seviyesi

İlk sürümde iki kullanım desteklenebilir:

1. Genel toplantı salonu kuralı
2. Belirli bir toplantı salonuna özel kural

Basit çözüm:

- `BirimId == null` ise genel kural
- `BirimId != null` ise salon özel kuralı

Özel kural varsa özel kural kullanılır.
Yoksa genel kural kullanılır.

### 13.3 Örnek

```text
Ücretsiz süre: 120 dakika
Ücretlendirme periyodu: 60 dakika
Periyot ücreti: 500 ₺
KDV oranı: %20
```

Rezervasyon:

```text
Toplam süre: 180 dakika
Ücretsiz süre: 120 dakika
Ücretli süre: 60 dakika
Ücret: 500 ₺
KDV: 100 ₺
Toplam: 600 ₺
```

---

## 14. Rezervasyon Ücret Hesaplama

### 14.1 Formül

```text
Toplam Süre = Bitis - Baslangic

Ücretli Süre = Max(0, Toplam Süre - Ücretsiz Süre)

Ücretli Periyot Sayısı = Ceiling(Ücretli Süre / Ücretlendirme Periyodu)

Ücret Tutarı = Ücretli Periyot Sayısı × Periyot Ücreti

KDV Tutarı = Ücret Tutarı × KDV Oranı / 100

Toplam Tutar = Ücret Tutarı + KDV Tutarı
```

### 14.2 Kurallar

- Başlangıç tarihi bitiş tarihinden küçük olmalıdır.
- Aynı toplantı salonunda çakışan rezervasyon oluşturulmamalıdır.
- İptal edilen rezervasyonlar çakışma kontrolüne dahil edilmez.
- Ücretsiz süre aşılıyorsa ücret hesaplanır.
- Ücret sıfırsa tahakkuk oluşturulmayabilir.
- Ücret çıkıyorsa manuel/rezervasyon kaynaklı tahakkuk oluşturulabilir.

---

## 15. Rezervasyon Tahakkuk Entegrasyonu

Toplantı salonu rezervasyonu ücret oluşturuyorsa tahakkuka aktarılabilmelidir.

### 15.1 Karar

Rezervasyon ücreti, `KiraTahakkuk` üzerinden takip edilecektir.

`KiraTahakkuk.KaynakTipi = Rezervasyon` olur.

Tahakkuk içinde bir veya daha fazla `TahakkukKalemi` bulunabilir.

Önerilen borç tipi kodu:

```text
TOPLANTI
```

(Bölüm 23.4 ile aynı kod kullanılır.) Eğer `BorcTipi` tablosunda yoksa seed ile eklenmelidir.

### 15.2 Akış

```text
1. Rezervasyon oluşturulur.
2. Süre ve ücret hesaplanır.
3. Ücret sıfırsa sadece rezervasyon kaydı tutulur.
4. Ücret > 0 ise kullanıcı "Tahakkuka Aktar" işlemi yapabilir.
5. Sistem KiraTahakkuk + TahakkukKalemi oluşturur.
6. Rezervasyon KiraTahakkukId ile bağlanır.
7. Rezervasyon durumu TahakkukaAktarildi olur.
```

### 15.3 Kurallar

- Bir rezervasyon yalnızca bir kez tahakkuka aktarılabilir.
- Tahakkuka aktarılmış rezervasyon iptal edilirse ilgili tahakkuk ödeme durumuna göre kontrol edilmelidir.
- Ödeme alınmış tahakkuka bağlı rezervasyon doğrudan silinemez.
- İptal için ayrı bir işlem ve açıklama istenmelidir.

---

## 16. Dashboard ve Raporlama Etkileri

Dashboard’a aşağıdaki metrikler eklenebilir veya ödeme/tahakkuk raporlarında gösterilebilir:

- Bu ay manuel borç toplamı
- Bu ay rezervasyon gelirleri
- Ücretli toplantı salonu rezervasyon sayısı
- Ücretsiz toplantı salonu rezervasyon sayısı
- Tahakkuka aktarılmamış ücretli rezervasyonlar
- En çok kullanılan toplantı salonları

İlk sürümde zorunlu olanlar:

- Tahakkuk listesinde manuel ve rezervasyon kaynakları görünsün.
- Ödeme takip ekranlarında bu tahakkuklar normal tahakkuk gibi takip edilsin.
- Rezervasyon listesinde ücretli/ücretsiz ayrımı görünsün.

---

## 17. Permission Güncellemeleri

`permission-catalog.md` ve `Authorization/PermissionCatalog.cs` senkron güncellenmelidir.

### 17.1 Yeni Permission Önerileri

```text
Parametre.View
Parametre.Manage

TasinmazTipi.View
TasinmazTipi.Manage

BirimTuru.View
BirimTuru.Manage

KiraciKategori.View
KiraciKategori.Manage

Sektor.View
Sektor.Manage

TasinmazCarpan.View
TasinmazCarpan.Manage

Rezervasyon.View
Rezervasyon.Create
Rezervasyon.Edit
Rezervasyon.Cancel
Rezervasyon.TransferToTahakkuk

ManuelBorc.View
ManuelBorc.Create
ManuelBorc.Cancel
```

### 17.2 Rol Davranışı

| Permission | Admin | Yonetici | Goruntuleyici |
|---|---:|---:|---:|
| Parametre.* | Evet | Hayır | Hayır |
| TasinmazTipi.* | Evet | Hayır | Hayır |
| BirimTuru.* | Evet | Hayır | Hayır |
| KiraciKategori.* | Evet | Hayır | Hayır |
| Sektor.* | Evet | Hayır | Hayır |
| TasinmazCarpan.* | Evet | Atanabilir | Hayır |
| Rezervasyon.View | Evet | Atanabilir | Atanmış taşınmaz kapsamında |
| Rezervasyon.Create/Edit/Cancel | Evet | Atanabilir | Hayır |
| Rezervasyon.TransferToTahakkuk | Evet | Atanabilir | Hayır |
| ManuelBorc.View | Evet | Atanabilir | Atanmış taşınmaz kapsamında |
| ManuelBorc.Create/Cancel | Evet | Atanabilir | Hayır |

### 17.3 Kapsam Kuralı

Goruntuleyici kullanıcı:

- Sadece kendisine atanmış taşınmazlara bağlı rezervasyonları görebilir.
- Sadece kendisine atanmış taşınmazlara bağlı manuel borç/tahakkukları görebilir.
- Rezervasyon oluşturamaz.
- Manuel borç oluşturamaz.
- Tahakkuka aktarma yapamaz.

---

## 18. Route Önerileri

### 18.1 Parametreler

| Controller | Action | Route |
|---|---|---|
| `TasinmazTipiController` | `Index` | `/Admin/TasinmazTipi` |
| `BirimTuruController` | `Index` | `/Admin/BirimTuru` |
| `KiraciKategoriController` | `Index` | `/Admin/KiraciKategori` |
| `SektorController` | `Index` | `/Admin/Sektor` |
| `RezervasyonUcretKuralController` | `Index` | `/Admin/RezervasyonUcretKural` |

### 18.2 Taşınmaz Çarpanları

| Controller | Action | Route |
|---|---|---|
| `TasinmazCarpanController` | `Index` | `/Tasinmaz/{id}/Carpanlar` |
| `TasinmazCarpanController` | `Create` | `/Tasinmaz/{id}/Carpanlar/Ekle` |
| `TasinmazCarpanController` | `Edit` | `/Tasinmaz/Carpanlar/Duzenle/{id}` |

### 18.3 Rezervasyon

| Controller | Action | Route |
|---|---|---|
| `RezervasyonController` | `Index` | `/Rezervasyon` |
| `RezervasyonController` | `Create` | `/Rezervasyon/Ekle` |
| `RezervasyonController` | `Edit` | `/Rezervasyon/Duzenle/{id}` |
| `RezervasyonController` | `Cancel` | `/Rezervasyon/Iptal/{id}` |
| `RezervasyonController` | `TransferToTahakkuk` | `/Rezervasyon/TahakkukaAktar/{id}` |

### 18.4 Manuel Borç

| Controller | Action | Route |
|---|---|---|
| `ManuelBorcController` | `Index` | `/ManuelBorc` |
| `ManuelBorcController` | `Create` | `/ManuelBorc/Ekle` |
| `ManuelBorcController` | `Cancel` | `/ManuelBorc/Iptal/{id}` |

---

## 19. Servis Önerileri

Aşağıdaki servisler oluşturulacaktır:

```text
IBirimTuruService
IKiraciKategoriService
ISektorService
ITasinmazKategoriCarpanService
IRezervasyonUcretKuralService
IRezervasyonService
IManuelBorcService
```

### 19.1 RezervasyonService

Temel metotlar:

```csharp
Task<IList<ToplantiSalonuRezervasyon>> GetAllAsync(string? userId = null);

Task<ToplantiSalonuRezervasyon?> GetByIdAsync(int id, string? userId = null);

Task<RezervasyonHesapSonucu> HesaplaAsync(
    int birimId,
    DateTime baslangic,
    DateTime bitis);

Task<int> CreateAsync(RezervasyonCreateViewModel model, string userId);

Task CancelAsync(int id, string userId, string neden);

Task<int?> TransferToTahakkukAsync(int id, string userId);
```

### 19.2 ManuelBorcService

Temel metotlar:

```csharp
Task<IList<KiraTahakkuk>> GetAllAsync(string? userId = null);

Task<int> CreateAsync(ManuelBorcCreateViewModel model, string userId);

Task CancelAsync(int tahakkukId, string userId, string neden);
```

---

## 20. UI Gereksinimleri

### 20.1 Parametre Ekranları

- Basit ve hızlı yönetilebilir tablo yapısı
- Ekle / düzenle / aktif-pasif işlemleri
- Sıra alanı
- Arama ve filtre
- Empty state
- Confirm modal

### 20.2 Taşınmaz Çarpanları

Taşınmaz detay sayfasına yeni sekme veya kart eklenebilir:

```text
Çarpanlar
```

Gösterilecek bilgiler:

- Kiracı kategorisi
- Çarpan
- Aktif/Pasif
- Açıklama
- İşlem

Ekleme formu:

- Kiracı kategorisi selectbox
- Çarpan input
- Açıklama
- Aktif mi?

### 20.3 Rezervasyon Ekranı

Rezervasyon listesinde gösterilecek kolonlar:

- Toplantı salonu
- Taşınmaz
- Kiracı
- Başlangıç
- Bitiş
- Toplam süre
- Ücretsiz süre
- Ücretli süre
- Toplam tutar
- Durum
- İşlem

Rezervasyon oluşturma formu:

- Toplantı salonu seçimi
- Kiracı seçimi
- Sözleşme seçimi opsiyonel
- Başlangıç tarihi/saati
- Bitiş tarihi/saati
- Hesaplama önizlemesi
- Açıklama

### 20.4 Manuel Borç Ekranı

Manuel borç listesinde:

- Kiracı
- Sözleşme
- Taşınmaz / Birim
- Borç tipi
- Tutar
- KDV
- Toplam
- Vade
- Durum
- İşlem

Manuel borç oluşturma formunda:

- Sözleşme seçimi
- Borç tipi seçimi
- Açıklama
- Tutar
- KDV
- Vade tarihi
- Not

---

## 21. DbContext Güncellemeleri

`ApplicationDbContext` içine aşağıdaki DbSet’ler eklenecektir:

```csharp
public DbSet<TasinmazTipi> TasinmazTipleri { get; set; }

public DbSet<BirimTuru> BirimTurleri { get; set; }

public DbSet<KiraciKategori> KiraciKategorileri { get; set; }

public DbSet<Sektor> Sektorler { get; set; }

public DbSet<TasinmazKategoriCarpan> TasinmazKategoriCarpanlari { get; set; }

public DbSet<RezervasyonUcretKural> RezervasyonUcretKurallari { get; set; }

public DbSet<ToplantiSalonuRezervasyon> ToplantiSalonuRezervasyonlari { get; set; }
```

Gerekirse manuel borç için ayrı entity oluşturulmaz; `KiraTahakkuk.KaynakTipi = Manuel` yeterlidir.

---

## 22. Migration Notları

Migration’da dikkat edilecekler:

- Decimal alanlara `HasPrecision(18,2)` verilmeli.
- Kod alanlarında unique index olabilir.
- `TasinmazKategoriCarpan` için `TasinmazId + KiraciKategoriId` unique index önerilir.
- Rezervasyon çakışma kontrolü uygulama servisinde yapılır.
- `Birim.BirimTuruId`, `Kiraci.KiraciKategoriId`, `Kiraci.SektorId` nullable olabilir.
- Eski veriler için seed/default değerler kontrollü atanmalıdır.

---

## 23. Seed Veriler

Seed eklenecek kayıtlar:

### 23.1 Birim Türleri

Kiralanabilir (`KiralanabilirMi = true`):
- Ofis
- Restoran
- Dükkan
- Stand
- Kiosk
- Diğer

Rezervasyon alanı (`RezervasyonYapilabilirMi = true`):
- Toplantı Salonu
- Etkinlik Alanı
- Konferans Salonu
- Eğitim Salonu
- Çok Amaçlı Salon

> Mevcut OTOMAT, BANKAMATIK, DEPO kayıtları `TasinmazTipi` tablosuna taşınmıştır.

### 23.2 Kiracı Kategorileri

- Akademisyen
- Akademisyen Olmayan
- Firma
- Kamu Kurumu
- Diğer

### 23.3 Sektörler

- Yazılım
- Lojistik
- Gıda
- Tarım
- Finans
- Eğitim
- Kamu
- Diğer

### 23.5 Taşınmaz Tipleri

- Bina
- Arazi
- Tarla
- Depo
- Otomat
- Bankamatik
- Kantin
- Diğer

### 23.4 Borç Tipi

`BorcTipi` tablosuna gerekiyorsa şu kayıt eklenir:

| Kod | Ad |
|---|---|
| MANUEL | Manuel Borç |
| TOPLANTI | Toplantı Salonu Kullanım Bedeli |

---

## 24. Uygulama Sırası

Claude Code veya benzeri AI aracı aşağıdaki sırayla ilerlemelidir:

**8.1 — Parametre Altyapısı (kısmen tamamlandı)**
1. `BirimTuru`, `KiraciKategori`, `Sektor` entity’leri + admin ekranları + seed. ✅ (8.1.1–8.1.5)
2. `TasinmazTipi` entity’si + admin ekranı + seed. (8.1.6)
3. `Tasinmaz.Tipi` enum → `TasinmazTipiId` FK veri migrasyonu + enum silme. (8.1.7)

**8.2 — Model Genişletmeleri (kısmen tamamlandı)**
4. `BirimTuruId` → `Birim`, `KiraciKategoriId` + `SektorId` → `Kiraci` FK’ları. ✅ (8.2.1–8.2.5)
5. `BirimTuru.KiralanabilirMi` + `RezervasyonYapilabilirMi` alanları + DB migration. (8.2.6)
6. Taşınmaz Ekle formunda "Ofis Bazlı" → "Birim Bazlı"; Rezervasyon Alanları bölümü eklenmesi. (8.2.7)

**8.3 — Taşınmaz Kategori Çarpanları**
7. `TasinmazKategoriCarpan` entity + servis + admin UI.
8. Taşınmaz detayına çarpan yönetimi ekranı.
9. Rate resolver içine `TasinmazKategoriCarpan` kaynağı.

**8.4 — Manuel Borç**
10. `TahakkukKaynakTipi` enum + `KiraTahakkuk.KaynakTipi`.
11. Manuel borç oluşturma/listeme/iptal akışları.

**8.5 — Rezervasyon Alanları**
12. `ToplantiSalonuRezervasyon` entity + `RezervasyonDurumu` enum.
13. `RezervasyonUcretKural` entity + admin ekranı.
14. `IRezervasyonService` + ücret hesaplama.
15. Çakışma kontrolü + rezervasyon oluşturma/listeme/iptal UI.
   - Birim listesi: sadece `RezervasyonYapilabilirMi = true` olanlar.

**8.6 — Rezervasyon → Tahakkuk Entegrasyonu**
16. `BorcTipi` seed: TOPLANTI.
17. `RezervasyonService.TransferToTahakkukAsync`.
18. İptal/ödeme durum kontrolleri.

**8.7 — Permission ve UI Entegrasyonu**
19. `permission-catalog.md` + `Authorization/PermissionCatalog.cs` senkronizasyonu.
20. Controller policy attribute’ları.
21. Görüntüleyici kapsam filtreleri (servis seviyesinde).

**8.8 — Dashboard, Raporlama ve Smoke Test**
22. Tahakkuk listesinde `KaynakTipi` kolonu (Otomatik/Manuel/Rezervasyon).
23. Dashboard: manuel borç + rezervasyon metrikleri.
24. Smoke test (kabul kriterleri 26. bölüm).

---

## 25. Test Senaryoları

### 25.1 Parametre Yönetimi

- Admin birim türü ekleyebilmeli.
- Admin kiracı kategorisi ekleyebilmeli.
- Admin sektör ekleyebilmeli.
- Pasif parametreler yeni kayıt formlarında görünmemeli.

### 25.2 Kiracı Formu

- Kiracı eklerken kategori seçilebilmeli.
- Kiracı eklerken sektör seçilebilmeli.
- Gerçek/tüzel kiracı form davranışı bozulmamalı.

### 25.3 Birim Türü

- Birim/ofis üzerinde birim türü seçilebilmeli.
- Toplantı salonu türündeki birimler rezervasyon ekranında listelenmeli.
- Ofis türündeki birimler rezervasyon ekranında listelenmemeli.

### 25.4 Taşınmaz Çarpanı

- Taşınmaz altında kategori bazlı çarpan tanımlanabilmeli.
- Aynı taşınmaz + kategori için ikinci aktif çarpan girilememeli.
- Sözleşme/tahakkuk üretiminde uygun kategorideki çarpan kullanılmalı.
- Formül `m² × çarpan` ile doğru hesaplanmalı.

### 25.5 Manuel Borç

- Yetkili kullanıcı manuel borç ekleyebilmeli.
- Manuel borç tahakkuk listesinde görünmeli.
- Manuel borç ödeme takibine dahil edilmeli.
- Ödeme alınmamış manuel borç iptal edilebilmeli.
- Ödeme alınmış manuel borç doğrudan iptal edilememeli.

### 25.6 Rezervasyon

- Toplantı salonu için rezervasyon oluşturulabilmeli.
- Çakışan rezervasyon engellenmeli.
- Ücretsiz süre aşılmadığında tutar sıfır olmalı.
- Ücretsiz süre aşıldığında ücret hesaplanmalı.
- Ücretli rezervasyon tahakkuka aktarılabilmeli.
- Aynı rezervasyon ikinci kez tahakkuka aktarılamamalı.
- İptal edilen rezervasyon çakışma kontrolünde dikkate alınmamalı.

### 25.7 Yetki

- Admin tüm işlemleri yapabilmeli.
- Yonetici sadece atanmış permission’lar kadar işlem yapabilmeli.
- Goruntuleyici sadece yetkili taşınmaz kapsamındaki kayıtları görebilmeli.
- Goruntuleyici manuel borç veya rezervasyon oluşturamamalı.
- Permission olmayan kullanıcı butonları görmemeli ve URL ile erişememeli.

---

## 26. Kabul Kriterleri

- [ ] Parametre yönetimi ekranları oluşturuldu.
- [ ] Taşınmaz tipleri `TasinmazTipi` tablosundan yönetilebilir hale geldi.
- [ ] `Tasinmaz.Tipi` enum kaldırıldı, veri migrasyonu tamamlandı.
- [ ] Birim türleri `KiralanabilirMi` / `RezervasyonYapilabilirMi` ayrımıyla yönetilebilir hale geldi.
- [ ] Kiracı kategorileri yönetilebilir hale geldi.
- [ ] Sektörler yönetilebilir hale geldi.
- [ ] Kiracı formunda kategori ve sektör selectbox’ları var.
- [ ] Birim modelinde birim türü var.
- [ ] Taşınmaz Ekle formunda "Ofis Bazlı" → "Birim Bazlı" terminoloji güncellendi.
- [ ] Rezervasyon alanları bölümü Taşınmaz Ekle formuna eklendi.
- [ ] Rezervasyon modülü sadece `RezervasyonYapilabilirMi = true` birimleri listeler.
- [ ] Taşınmaz altında kategori bazlı çarpan tanımlanabiliyor.
- [ ] Çarpan hesaplaması `m² × çarpan` formülüne göre çalışıyor.
- [ ] Rate resolver `TasinmazKategoriCarpan` kaynağını destekliyor.
- [ ] Manuel borç eklenebiliyor.
- [ ] Manuel borç tahakkuk ve ödeme takibiyle entegre.
- [ ] Rezervasyon oluşturulabiliyor.
- [ ] Rezervasyonlarda ücretsiz süre uygulanıyor.
- [ ] Ücretsiz süre aşılırsa ücret hesaplanıyor.
- [ ] Ücretli rezervasyon tahakkuka aktarılabiliyor.
- [ ] Permission catalog yeni permission’larla güncellendi.
- [ ] `Authorization/PermissionCatalog.cs` markdown katalogla senkron.
- [ ] Goruntuleyici kapsam filtreleri korunuyor.
- [ ] Mevcut ödeme, tahakkuk, sözleşme ve kiracı akışları bozulmadı.
- [ ] SQL Server migration başarıyla çalışıyor.
- [ ] Mevcut Tailwind/Magic UI tasarım dili korunuyor.

---

## 27. Riskler ve Önlemler

| Risk | Etki | Önlem |
|---|---|---|
| Parametreler enum gibi dağılır | Bakım zorlaşır | Merkezi parametre tabloları kullan |
| Çarpan mevcut rate resolver ile çakışır | Yanlış tahakkuk | Resolve sırasını net uygula |
| Manuel borç ödeme sisteminden kopuk olur | Raporlama bozulur | KiraTahakkuk + TahakkukKalemi kullan |
| Rezervasyon ücretleri tahakkuka aktarılmaz | Gelir takibi eksik kalır | Rezervasyon → tahakkuk entegrasyonu kur |
| Görüntüleyici yetkisi sızar | Veri güvenliği riski | Servis seviyesinde UserTasinmazYetki filtresi uygula |
| Eski verilerde kategori/tür null kalır | NullReference hatası | Nullable alan + fallback gösterim kullan |
| Permission catalog senkron bozulur | Policy hatası | Markdown + C# catalog birlikte güncellenir |
| `TasinmazTipi` enum → parametre tablo migrasyonunda eşleşme eksik kalır | Taşınmaz tipi veri kaybı | Migration SQL'i test ortamında çalıştır; enum silinmeden önce doğrula |
| `OfisBazli` enum değeri UI'da "Birim Bazlı" gösterilmezse terminoloji uyuşmazlığı oluşur | Kullanıcı karışıklığı | 8.2.7 kapsamında tüm form metinleri kontrol edilmeli |

---

## 28. Faz 8 Tamamlandığında

Tamamlandığında:

- `PROGRESS.md` içinde Faz 8 checklist’i işaretlenir.
- `MASTER-PLAN.md` aktif faz bilgisi güncellenir.
- Yeni permission’lar test edilir.
- Rezervasyon ve manuel borç akışları smoke testten geçirilir.
- Tahakkuk/ödeme ekranlarında manuel ve rezervasyon kaynakları doğrulanır.