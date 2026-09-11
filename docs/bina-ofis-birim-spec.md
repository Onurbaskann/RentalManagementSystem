# Bina ve Birim Bazlı Kiralama Spec

> **GÜNCELLİK NOTU:** Bu dosya tarihsel gelişim sürecini içerir. Güncel mimari kararlar için **MASTER-PLAN.md**, **PROGRESS.md** ve ilgili Faz 8+ dosyaları esas alınmalıdır; **PROGRESS-HISTORY.md** tarihsel arşivdir.
> - "OfisBazli" terminolojisi Faz 8 sonrası **"BirimBazli"** olarak genelleştirilmiştir.
> - DummyDataService yerine **EF Core** servisleri kullanılmaktadır.
>
> Ana proje spec dosyasını şişirmemek için bina/birim domain modeline ait detaylar bu dosyada tutulur.
>
> Bu doküman `project-spec-canonical.md` dosyasını tamamlayıcı niteliktedir.

---

## 1. Amaç

Mevcut sistemde bina türündeki taşınmazlar `KatBazli` olarak tanımlandığında, sistem kat sayısı kadar kiralanabilir `Birim` üretmektedir.

Bu yaklaşım değiştirilecektir.

Yeni hedef:

```text
Bina
 ├── Ofis 101
 ├── Ofis 102
 ├── Ofis 201
 ├── Ofis 202
 └── Ofis 301
```

Kiralama hedefi **kat değil, ofis** olacaktır.

Kat bilgisi gerekiyorsa ayrı bir `Kat` modeli oluşturulmayacak; ofis birimi üzerinde sadece bilgi amaçlı `KatNo` alanı tutulacaktır.

---

## 2. Kapsam

Bu geliştirme kapsamında aşağıdaki değişiklikler yapılacaktır:

- `KatBazli` kiralama yaklaşımı kaldırılacak veya `OfisBazli` olarak yeniden yorumlanacaktır.
- Bina içinde kiralanabilir birimler **ofis** olacaktır.
- Katlar ayrı entity/model olmayacaktır.
- `BirimTipi.Kat` yerine `BirimTipi.Ofis` kullanılacaktır.
- `Birim` modeline `OfisNo`, `KatNo` ve `Aciklama` alanları eklenecektir.
- `KatSayisi` sadece bilgi amaçlı kalacaktır.
- `KatSayisi` artık otomatik kiralanabilir birim üretmeyecektir.
- Bina + OfisBazli seçildiğinde kullanıcı ofisleri manuel/dinamik olarak tanımlayacaktır.
- Sözleşmeler katlara değil, ofis birimlerine bağlanacaktır.
- Taşınmaz detayında binaya bağlı ofisler listelenecektir.
- Sözleşme ekleme ekranında boş ofisler seçilebilecektir.
- Dashboard doluluk ve gelir hesapları ofis/komple birimler üzerinden çalışacaktır.
- Seed veri ofis bazlı olacak şekilde güncellenecektir.

---

## 3. Kapsam Dışı

Bu geliştirme kapsamında aşağıdakiler yapılmayacaktır:

- Ayrı `Kat` modeli oluşturulmayacaktır.
- Kat bazlı kiralama yapılmayacaktır.
- Katlara doğrudan sözleşme bağlanmayacaktır.
- Domain verileri EF Core’a taşınmayacaktır.
- `DummyDataService` in-memory mimarisi bozulmayacaktır.
- Mevcut Authentication & Authorization yapısı değiştirilmemelidir.
- Mevcut kiracı/finans/TÜFE/KDV kuralları değiştirilmemelidir.

---

## 4. Temel Domain Kararı

Yeni domain yapısı:

```text
Tasinmaz
 └── Birimler
      ├── Komple
      └── Ofis
```

Bina içinde ofisler doğrudan `Tasinmaz` nesnesine bağlı `Birim` kayıtlarıdır.

Kat sadece ofisin üzerinde bilgi amaçlı bir alandır:

```text
Ofis 201
- OfisNo: 201
- KatNo: 2
```

Kat ayrı bir model değildir.

---

## 5. Kavramlar

| Kavram | Açıklama |
|---|---|
| `Tasinmaz` | Bina, arazi, tarla, depo gibi ana varlık |
| `Birim` | Kiralanabilir varlık |
| `BirimTipi.Komple` | Taşınmazın tamamı |
| `BirimTipi.Ofis` | Bina içindeki kiralanabilir ofis |
| `OfisNo` | Ofisin numarası/kodu |
| `KatNo` | Ofisin bulunduğu kat bilgisi, ayrı entity değildir |
| `KiraSozlesmesi` | Her zaman bir `BirimId` üzerinden bağlanır |

---

## 6. Enum Güncellemeleri

`Models/Enums.cs` güncellenecektir.

### 6.1 KiralamaSekli

Eski yaklaşım:

```csharp
public enum KiralamaSekli
{
    TekParca = 1,
    KatBazli = 2
}
```

Yeni yaklaşım:

```csharp
public enum KiralamaSekli
{
    TekParca = 1,
    OfisBazli = 2
}
```

> Not: Projede `KatBazli` referansları varsa bunlar `OfisBazli` olarak güncellenmelidir.

### 6.2 BirimTipi

Eski yaklaşım:

```csharp
public enum BirimTipi
{
    Komple = 1,
    Kat = 2
}
```

Yeni yaklaşım:

```csharp
public enum BirimTipi
{
    Komple = 1,
    Ofis = 2
}
```

> Not: Projede `BirimTipi.Kat` referansları varsa bunlar `BirimTipi.Ofis` olarak güncellenmelidir.

---

## 7. Tasinmaz Modeli

`Models/Tasinmaz.cs` aşağıdaki yapıya göre güncellenecektir.

```csharp
public class Tasinmaz
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public TasinmazTipi Tipi { get; set; }

    public KiralamaSekli KiralamaSekli { get; set; }

    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    // Sadece bina bilgisi için tutulur.
    // Kiralanabilir birim üretimi için kullanılmaz.
    public int? KatSayisi { get; set; }

    public string? Aciklama { get; set; }

    public DateTime KayitTarihi { get; set; }

    public List<Birim> Birimler { get; set; } = new();
}
```

---

## 8. Birim Modeli

`Models/Birim.cs` aşağıdaki yapıya göre güncellenecektir.

```csharp
public class Birim
{
    public int Id { get; set; }

    public int TasinmazId { get; set; }

    public Tasinmaz Tasinmaz { get; set; }

    public BirimTipi BirimTipi { get; set; }

    // Komple birimlerde null olabilir.
    // Ofis birimlerinde ofisin bulunduğu kat bilgisidir.
    // Ayrı Kat modeli yoktur.
    public int? KatNo { get; set; }

    // Örn: "Komple", "Ofis 101", "Ofis 202"
    public string Ad { get; set; } = string.Empty;

    // Örn: "101", "202", "A-12"
    // Sadece ofis birimleri için kullanılır.
    public string? OfisNo { get; set; }

    public decimal Yuzolcumu { get; set; }

    public string? Aciklama { get; set; }

    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = new();
}
```

---

## 9. Kira Sözleşmesi Kuralı

`KiraSozlesmesi` modeli doğrudan değişmek zorunda değildir.

Temel kural korunur:

```text
Kira sözleşmeleri her zaman BirimId üzerinden bağlanır.
```

Yeni yorum:

| Taşınmaz Tipi | Kiralama Şekli | Sözleşmenin Bağlanacağı Birim |
|---|---|---|
| Bina | TekParca | Komple birim |
| Bina | OfisBazli | Ofis birimi |
| Arazi | TekParca | Komple birim |
| Tarla | TekParca | Komple birim |
| Depo | TekParca | Komple birim |

Sözleşme hiçbir zaman doğrudan binaya veya kata bağlanmaz.

---

## 10. Kiralama Şekli Kuralları

### 10.1 Bina + TekParca

Bina tek parça kiralanacaksa sistem otomatik olarak tek bir `Komple` birim oluşturur.

```csharp
new Birim
{
    BirimTipi = BirimTipi.Komple,
    Ad = "Komple",
    OfisNo = null,
    KatNo = null,
    Yuzolcumu = tasinmaz.KapaliYuzolcumu
}
```

### 10.2 Bina + OfisBazli

Bina ofis bazlı kiralanacaksa kullanıcı ofisleri tanımlar.

Örnek ofisler:

| Ofis No | Kat No | Ad | m² |
|---|---:|---|---:|
| 101 | 1 | Ofis 101 | 45 |
| 102 | 1 | Ofis 102 | 60 |
| 201 | 2 | Ofis 201 | 75 |
| 202 | 2 | Ofis 202 | 80 |

Her ofis bir `Birim` kaydıdır.

```csharp
new Birim
{
    BirimTipi = BirimTipi.Ofis,
    OfisNo = "101",
    KatNo = 1,
    Ad = "Ofis 101",
    Yuzolcumu = 45
}
```

### 10.3 Bina Dışı Taşınmazlar

Arazi, tarla, depo ve diğer tiplerde taşınmaz otomatik olarak tek parça kiralanır.

```csharp
new Birim
{
    BirimTipi = BirimTipi.Komple,
    Ad = "Komple",
    OfisNo = null,
    KatNo = null
}
```

---

## 11. KatSayisi Kuralı

`KatSayisi` alanı artık otomatik birim üretmek için kullanılmayacaktır.

Eski davranış:

```text
KatSayisi = 5
→ 5 adet Kat birimi oluştur
```

Yeni davranış:

```text
KatSayisi = 5
→ sadece bina bilgisi olarak sakla
→ ofisleri kullanıcı ayrıca tanımlar
```

`KatSayisi`:

- Opsiyonel olabilir.
- Sadece bina için gösterilir.
- Bina + OfisBazli olduğunda kullanıcıya bilgi amaçlı sorulabilir.
- Ofis oluşturma sayısını belirlemez.
- Otomatik birim üretmez.

---

## 12. Taşınmaz Ekle Formu

`Views/Tasinmaz/Ekle.cshtml` güncellenecektir.

### 12.1 Bina Tipi Seçildiğinde

Bina seçilirse kiralama şekli gösterilir:

- Tek Parça
- Ofis Bazlı

Eski “Kat Bazlı” ifadesi kullanılmamalıdır.

### 12.2 Tek Parça Seçilirse

Ofis tanımlama alanı gizlenir.

Kaydetme sırasında otomatik tek `Komple` birim oluşturulur.

### 12.3 Ofis Bazlı Seçilirse

Ofis tanımlama alanı gösterilir.

Kullanıcı bir veya daha fazla ofis satırı ekleyebilmelidir.

Ofis satır alanları:

| Alan | Zorunlu | Açıklama |
|---|---:|---|
| Ofis No | Evet | 101, 102, A-12 gibi |
| Kat No | Hayır | Sadece bilgi amaçlı |
| Ad | Hayır | Boşsa `Ofis {OfisNo}` üretilir |
| Yüzölçümü | Evet | m² |
| Açıklama | Hayır | Opsiyonel not |

### 12.4 Dinamik Ofis Satırları

Kullanıcı formda:

- Ofis ekleyebilmeli
- Ofis satırı silebilmeli
- Her ofisin m² bilgisini girebilmeli
- Aynı bina içinde aynı Ofis No tekrar girilememeli

Tercih edilen davranış:

- Alpine.js ile dinamik satır yönetimi
- Server-side validation ile tekrar kontrol
- İlgisiz alanları gizleme
- Premium Tailwind/Magic UI tasarım dili

---

## 13. Taşınmaz Form ViewModel

Yeni veya mevcut form view model aşağıdaki alanları desteklemelidir.

```csharp
public class TasinmazFormViewModel
{
    public int? Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public TasinmazTipi Tipi { get; set; }

    public KiralamaSekli KiralamaSekli { get; set; }

    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    public int? KatSayisi { get; set; }

    public string? Aciklama { get; set; }

    public List<OfisBirimInputViewModel> Ofisler { get; set; } = new();
}
```

Ofis input modeli:

```csharp
public class OfisBirimInputViewModel
{
    public string OfisNo { get; set; } = string.Empty;

    public int? KatNo { get; set; }

    public string? Ad { get; set; }

    public decimal Yuzolcumu { get; set; }

    public string? Aciklama { get; set; }
}
```

---

## 14. Taşınmaz Ekle Server-Side Validasyon

Taşınmaz kaydedilirken aşağıdaki kurallar uygulanmalıdır.

### 14.1 Ortak Kurallar

- `Ad` zorunludur.
- `Tipi` zorunludur.
- `Il`, `Ilce`, `Mahalle` ve `AcikAdres` validasyonu mevcut sistem kurallarına göre yapılır.
- Yüzölçümü değerleri negatif olamaz.

### 14.2 Bina + TekParca

- Ofis listesi dikkate alınmaz.
- Tek `Komple` birim oluşturulur.
- `KatSayisi` bilgi amaçlı kaydedilebilir.

### 14.3 Bina + OfisBazli

- En az 1 ofis girilmelidir.
- Her ofiste `OfisNo` zorunludur.
- Her ofiste `Yuzolcumu > 0` olmalıdır.
- Aynı bina içinde aynı `OfisNo` tekrar edemez.
- `Ad` boşsa otomatik olarak `Ofis {OfisNo}` oluşturulur.
- Her ofis için `BirimTipi.Ofis` kullanılır.
- `KatNo` opsiyoneldir.

### 14.4 Bina Dışı Taşınmazlar

- `KiralamaSekli` otomatik `TekParca` olmalıdır.
- Ofis listesi dikkate alınmaz.
- Tek `Komple` birim oluşturulur.

---

## 15. DummyDataService Güncellemeleri

`Services/DummyDataService.cs` güncellenecektir.

### 15.1 AddTasinmaz

`AddTasinmaz` metodu yeni ofis mantığına göre çalışmalıdır.

Önerilen davranış:

```csharp
public int AddTasinmaz(Tasinmaz t, List<Birim>? ofisler = null)
{
    // Id üret
    // Tasinmaz kaydet
    // Kiralama şekline göre birimleri oluştur
}
```

Daha temiz yaklaşım için view model controller’da domain modele dönüştürülebilir.

### 15.2 Birim Oluşturma Kuralları

```text
Bina + TekParca
→ 1 Komple birim

Bina + OfisBazli
→ Kullanıcının girdiği ofisler kadar Ofis birimi

Arazi/Tarla/Depo/Diger
→ 1 Komple birim
```

### 15.3 GetBosBirimler

`GetBosBirimler()` ofis/komple birim üzerinden çalışmalıdır.

- Kat diye bir birim olmayacaktır.
- Feshedilmiş sözleşmeler aktif sayılmayacaktır.
- Süresi geçmiş sözleşmeler aktif sayılmayacaktır.
- Aktif sözleşmesi olmayan ofisler boş kabul edilir.

### 15.4 GetBirimler

`GetBirimler()` artık şu tipleri döndürür:

- `BirimTipi.Komple`
- `BirimTipi.Ofis`

`BirimTipi.Kat` kullanılmamalıdır.

---

## 16. IstatistikService Güncellemeleri

`Services/IstatistikService.cs` güncellenecektir.

### 16.1 Birim Durumu

Birim durumu ofis/komple birim üzerinden hesaplanmalıdır.

```csharp
public KiraDurumu GetBirimDurumu(Birim birim)
{
    var aktif = birim.Sozlesmeler
        .Where(s =>
            s.Durum == SozlesmeDurumu.Aktif &&
            s.BaslangicTarihi <= DateTime.Now &&
            s.BitisTarihi >= DateTime.Now)
        .OrderByDescending(s => s.BitisTarihi)
        .FirstOrDefault();

    if (aktif == null)
        return KiraDurumu.Bos;

    return (aktif.BitisTarihi - DateTime.Now).Days <= 30
        ? KiraDurumu.SuresiDolmakUzere
        : KiraDurumu.Kirali;
}
```

Eğer projede `SozlesmeDurumu` henüz yoksa mevcut aktif sözleşme kuralı korunabilir. Ancak `kiraci-sozlesme-finans-spec.md` uygulanmışsa `SozlesmeDurumu` dikkate alınmalıdır.

### 16.2 Dashboard

Dashboard istatistikleri artık ofis/komple birimler üzerinden hesaplanır.

| Metrik | Yeni Yorum |
|---|---|
| Toplam Birim | Ofis + Komple birimler |
| Kiralı Birim | Aktif sözleşmesi olan ofis/komple birimler |
| Boş Birim | Aktif sözleşmesi olmayan ofis/komple birimler |
| Süresi Dolmak Üzere | Aktif sözleşmesi 30 gün içinde bitecek ofis/komple birimler |
| Aylık Gelir | Aktif sözleşmelerin aylık kira bedeli |
| Yıllık Projeksiyon | Aktif sözleşmelerin yıllık karşılığı |

Kat sayısı dashboard birim sayısına dahil edilmez.

---

## 17. Taşınmaz Detay Sayfası

`Views/Tasinmaz/Detay.cshtml` güncellenecektir.

### 17.1 Birimler Bölümü

Bina + OfisBazli taşınmazlarda ofisler listelenmelidir.

Gösterilecek bilgiler:

| Alan | Açıklama |
|---|---|
| Ofis No | 101, 201, A-12 |
| Ad | Ofis 101 |
| Kat No | Varsa gösterilir |
| m² | Ofis yüzölçümü |
| Durum | Boş / Kiralı / Süresi Dolmak Üzere |
| Kiracı | Aktif sözleşme varsa |
| Kalan Gün | Aktif sözleşme varsa |
| İşlem | Kirala / Detay |

### 17.2 Görsel Gruplama

Ayrı `Kat` modeli olmadığı halde UI’da görsel olarak kat bazlı gruplama yapılabilir.

Örnek:

```text
1. Kat
 ├── Ofis 101
 └── Ofis 102

2. Kat
 ├── Ofis 201
 └── Ofis 202

Kat belirtilmemiş
 └── Ofis A-12
```

Bu sadece view-level gruplamadır.

Domain modelde ayrı `Kat` entity oluşturulmaz.

### 17.3 Tek Parça Taşınmazlar

Bina + TekParca veya bina dışı taşınmazlarda tek `Komple` birim gösterilir.

---

## 18. Sözleşme Ekle Sayfası

`Views/Sozlesme/Ekle.cshtml` güncellenecektir.

### 18.1 Birim Seçimi

Sözleşme oluştururken yalnızca boş kiralanabilir birimler seçilebilmelidir.

Seçenek formatı:

```text
Teknokent A Blok — Ofis 101 • 1. Kat • 45 m²
Teknokent A Blok — Ofis 202 • 2. Kat • 80 m²
Çamlık Kantini — Komple • 180 m²
```

### 18.2 Filtreleme

Birim seçimi şu bilgilere göre aranabilir olmalıdır:

- Taşınmaz adı
- Ofis No
- Ofis adı
- Kat No
- İl / İlçe
- m²

### 18.3 Kurallar

- Dolu ofisler listelenmemelidir.
- Feshedilmiş sözleşmeye sahip ofisler yeniden kiralanabilir olmalıdır.
- Süresi geçmiş sözleşmeye sahip ofisler yeniden kiralanabilir olmalıdır.
- Sözleşme her zaman `BirimId` ile oluşturulmalıdır.

---

## 19. Sözleşme Detay Sayfası

`Views/Sozlesme/Detay.cshtml` güncellenecektir.

Sözleşme detayında kiralanan birim ofis ise şu bilgiler gösterilmelidir:

- Taşınmaz adı
- Ofis No
- Ofis adı
- Kat No
- Ofis m²
- Adres
- Birim durumu

Örnek gösterim:

```text
Teknokent A Blok — Ofis 201
2. Kat • 75 m² • Bornova / İzmir
```

Komple birimlerde:

```text
Çamlık Kantini — Komple
180 m² • Karşıyaka / İzmir
```

---

## 20. Kiracı Sayfaları ile Etkileşim

Kiracı detayında sözleşmeler listelenirken ofis bilgisi gösterilmelidir.

Örnek:

```text
Yıldız Yazılım A.Ş.
Teknokent A Blok — Ofis 201
2. Kat • 75 m²
```

Kiracı modeli veya gerçek/tüzel kiracı kuralları bu spec kapsamında değiştirilmez.

---

## 21. Finans/TÜFE/KDV ile Etkileşim

Bu spec, TÜFE/KDV hesaplama kurallarını değiştirmez.

Ancak gelir ve tahsilat hesapları ofis/komple birimler üzerinden bağlı sözleşmelere göre çalışmalıdır.

KDV/TÜFE hesapları sözleşme üzerinde kalır.

Ofis modeli sadece sözleşmenin hangi kiralanabilir birime bağlı olduğunu belirler.

---

## 22. Yetkilendirme

Mevcut Authentication & Authorization kuralları korunacaktır.

Genel kural:

| İşlem | Admin | Yonetici | Goruntuleyici |
|---|---:|---:|---:|
| Taşınmaz liste/detay | Evet | Evet | Evet |
| Taşınmaz ekleme | Evet | Evet | Hayır |
| Taşınmaz düzenleme | Evet | Evet | Hayır |
| Ofis tanımlama | Evet | Evet | Hayır |
| Sözleşme ekleme | Evet | Evet | Hayır |
| Sözleşme detay | Evet | Evet | Evet |

Write action’larda mevcut rol kontrolleri korunmalıdır:

```csharp
[Authorize(Roles = "Admin,Yonetici")]
```

Goruntuleyici işlem butonlarını görmemelidir.

---

## 23. UI Tasarım Kuralları

Bu geliştirme mevcut premium SaaS tasarım diliyle uyumlu olmalıdır.

Uyulacak kurallar:

- Tailwind CSS kullanılacaktır.
- Magic UI’dan gelen estetik korunacaktır.
- Bootstrap/generic MVC görünümü kullanılmayacaktır.
- Form alanları sıkışık olmamalıdır.
- Ofis ekleme satırları temiz ve anlaşılır olmalıdır.
- Hover/focus state’leri korunmalıdır.
- Para, tarih ve sayı formatları mevcut helper’larla uyumlu olmalıdır.
- Boş ofis listesi için empty state gösterilmelidir.
- Taşınmaz detayında ofis kartları/table yapısı okunabilir olmalıdır.

---

## 24. Form UI Önerisi

Bina + OfisBazli seçildiğinde formda aşağıdaki gibi bir bölüm gösterilebilir:

```text
Ofisler

[ Ofis No ] [ Kat No ] [ Ad ] [ m² ] [ Açıklama ] [ Sil ]

+ Ofis Ekle
```

Örnek satırlar:

| Ofis No | Kat No | Ad | m² | Açıklama |
|---|---:|---|---:|---|
| 101 | 1 | Ofis 101 | 45 | Girişe yakın |
| 102 | 1 | Ofis 102 | 60 | Cepheli |
| 201 | 2 | Ofis 201 | 75 | Toplantı odalı |

Alan davranışları:

- `OfisNo` girilince `Ad` boşsa otomatik `Ofis {OfisNo}` önerilebilir.
- `KatNo` boş bırakılabilir.
- `Yuzolcumu` sıfırdan büyük olmalıdır.
- Kullanıcı en az bir ofis eklemeden OfisBazli bina kaydedememelidir.

---

## 25. Alpine.js Örnek State

Taşınmaz ekleme formunda aşağıdaki mantık kullanılabilir.

```html
<div x-data="{
    tip: 'Bina',
    kiralamaSekli: 'TekParca',
    ofisler: [
        { ofisNo: '', katNo: '', ad: '', yuzolcumu: '', aciklama: '' }
    ],
    addOfis() {
        this.ofisler.push({ ofisNo: '', katNo: '', ad: '', yuzolcumu: '', aciklama: '' });
    },
    removeOfis(index) {
        this.ofisler.splice(index, 1);
    }
}">
    <!-- Kiralama şekli -->
    <div x-show="tip === 'Bina'">
        <!-- TekParca / OfisBazli seçim -->
    </div>

    <!-- Ofis listesi -->
    <div x-show="tip === 'Bina' && kiralamaSekli === 'OfisBazli'">
        <!-- Dinamik ofis satırları -->
    </div>
</div>
```

---

## 26. Seed Veri Güncellemesi

Seed veri ofis bazlı olacak şekilde güncellenecektir.

### 26.1 Teknokent A Blok

Eski yapı:

```text
Teknokent A Blok
- 1. Kat
- 2. Kat
- 3. Kat
- 4. Kat
- 5. Kat
```

Yeni yapı:

```text
Teknokent A Blok
- Ofis 101
- Ofis 102
- Ofis 201
- Ofis 202
- Ofis 301
- Ofis 302
- Ofis 401
- Ofis 501
```

Örnek ofisler:

| Ofis No | Kat No | m² | Durum |
|---|---:|---:|---|
| 101 | 1 | 55 | Kiralı |
| 102 | 1 | 65 | Kiralı |
| 201 | 2 | 80 | Süresi Dolmak Üzere |
| 202 | 2 | 75 | Boş |
| 301 | 3 | 90 | Boş |
| 302 | 3 | 70 | Kiralı |
| 401 | 4 | 110 | Boş |
| 501 | 5 | 120 | Boş |

### 26.2 Sanayi Sitesi B Blok

Örnek ofis/birimler:

| Ofis No | Kat No | m² | Durum |
|---|---:|---:|---|
| 101 | 1 | 180 | Kiralı |
| 201 | 2 | 220 | Süresi Dolmak Üzere |
| 301 | 3 | 240 | Boş |

### 26.3 Tek Parça Taşınmazlar

Aşağıdaki taşınmazlar tek `Komple` birim olarak kalabilir:

- Çamlık Kantini
- Bornova Tarlası
- Atatürk Cd. Dükkan
- Menemen Arazi
- Buca Deposu

---

## 27. Seed Sözleşme Güncellemesi

Sözleşmeler artık katlara değil ofislere bağlanmalıdır.

Örnek:

```text
Teknokent A Blok — Ofis 101
Kiracı: Yıldız Yazılım A.Ş.
Durum: Aktif
Bitişe: 8 ay

Teknokent A Blok — Ofis 102
Kiracı: Ahmet Yılmaz
Durum: Aktif
Bitişe: 45 gün

Teknokent A Blok — Ofis 201
Kiracı: Ayşe Demir
Durum: Süresi Dolmak Üzere
Bitişe: 12 gün

Teknokent A Blok — Ofis 202
Durum: Boş

Teknokent A Blok — Ofis 301
Durum: Boş
```

Sözleşme gösterimlerinde `1. Kat` gibi kiralanan birim adı kullanılmamalıdır.

Doğru gösterim:

```text
Teknokent A Blok — Ofis 101
```

Yanlış gösterim:

```text
Teknokent A Blok — 1. Kat
```

---

## 28. Eski Referansların Temizlenmesi

Projede aşağıdaki eski ifadeler aranıp güncellenmelidir.

### 28.1 Kod Referansları

| Eski | Yeni |
|---|---|
| `KiralamaSekli.KatBazli` | `KiralamaSekli.OfisBazli` |
| `BirimTipi.Kat` | `BirimTipi.Ofis` |
| `KatNo` kiralanabilir birim kimliği gibi kullanılıyorsa | Sadece bilgi alanı olarak kullanılmalı |
| `KatSayisi` kadar birim oluşturma | Kaldırılmalı |

### 28.2 UI Metinleri

| Eski Metin | Yeni Metin |
|---|---|
| Kat Bazlı Kirala | Ofis Bazlı Kirala |
| Her kat ayrı kiralanabilir | Bina içindeki ofisler ayrı kiralanabilir |
| Kat Sayısı kadar birim oluşturulur | Ofisler manuel tanımlanır |
| 1. Kat / 2. Kat kiralanıyor | Ofis 101 / Ofis 201 kiralanıyor |

### 28.3 Doküman Referansları

`project-spec-canonical.md` içinde kat bazlı kiralama anlatan bölümler kısa notla güncellenmelidir.

Eklenecek referans:

```md
## Bina ve Ofis Bazlı Kiralama

Bina türündeki taşınmazlarda kiralanabilir birimler kat değil, ofislerdir.

Bina + OfisBazli yapıda:
- Bina ana taşınmazdır.
- Ofisler binaya bağlı kiralanabilir birimlerdir.
- Kat bilgisi ayrı model değildir; ofis üzerinde `KatNo` alanı olarak tutulur.
- Kira sözleşmeleri ofislere bağlanır.

Detaylar için:

`docs/bina-ofis-birim-spec.md`
```

---

## 29. Güncellenecek Dosyalar

Muhtemel dosyalar:

```text
Models/Enums.cs
Models/Tasinmaz.cs
Models/Birim.cs

Models/ViewModels/TasinmazFormViewModel.cs
Models/ViewModels/OfisBirimInputViewModel.cs
Models/ViewModels/TasinmazDetayViewModel.cs
Models/ViewModels/SozlesmeDetayViewModel.cs

Services/DummyDataService.cs
Services/IstatistikService.cs

Controllers/TasinmazController.cs
Controllers/SozlesmeController.cs

Views/Tasinmaz/Ekle.cshtml
Views/Tasinmaz/Detay.cshtml
Views/Tasinmaz/Index.cshtml

Views/Sozlesme/Ekle.cshtml
Views/Sozlesme/Detay.cshtml
Views/Sozlesme/Index.cshtml

Views/Kiraci/Detay.cshtml

wwwroot/js/tasinmaz-form.js
wwwroot/js/sozlesme-form.js

docs/project-spec-canonical.md
```

---

## 30. Uygulama Sırası

Claude Code veya benzeri AI aracı aşağıdaki sırayla ilerlemelidir:

1. `Models/Enums.cs` içinde `KiralamaSekli.KatBazli` yerine `OfisBazli` yaklaşımını uygula.
2. `Models/Enums.cs` içinde `BirimTipi.Kat` yerine `Ofis` yaklaşımını uygula.
3. `Birim` modeline `OfisNo`, nullable `KatNo` ve `Aciklama` alanlarını ekle.
4. `KatSayisi` alanının otomatik birim üretmediğinden emin ol.
5. Taşınmaz ekleme view modeline ofis listesi ekle.
6. Taşınmaz ekleme formunda Bina + OfisBazli için dinamik ofis satırları oluştur.
7. Server-side validasyon ekle:
   - En az 1 ofis
   - Benzersiz OfisNo
   - Yuzolcumu > 0
8. `DummyDataService.AddTasinmaz` metodunu ofis bazlı mantığa göre güncelle.
9. `GetBosBirimler`, `GetBirimler` ve aktif sözleşme hesaplarını ofis/komple birimlere göre güncelle.
10. `IstatistikService` içinde dashboard hesaplarının kat yerine ofis/komple birimleri saydığından emin ol.
11. Taşınmaz detay sayfasında ofisleri listele.
12. Sözleşme ekleme sayfasında boş ofisleri seçilebilir hale getir.
13. Sözleşme detayında ofis bilgisini göster.
14. Kiracı detayında sözleşme kartlarında ofis bilgisini göster.
15. Seed veriyi ofis bazlı hale getir.
16. Eski “kat bazlı kiralama” UI metinlerini temizle.
17. `project-spec-canonical.md` içine kısa referans bölümünü ekle.
18. Smoke test yap.

---

## 31. Kabul Kriterleri

- [ ] Bina içinde katlar ayrı entity/model olarak oluşturulmaz.
- [ ] Kiralanabilir birimler kat değil, ofistir.
- [ ] `BirimTipi.Kat` kullanılmaz.
- [ ] `BirimTipi.Ofis` kullanılır.
- [ ] `KiralamaSekli.KatBazli` yerine `OfisBazli` yaklaşımı uygulanır.
- [ ] `KatSayisi` otomatik birim üretmez.
- [ ] Bina + TekParca seçildiğinde tek `Komple` birim oluşturulur.
- [ ] Bina + OfisBazli seçildiğinde kullanıcı ofisleri tanımlayabilir.
- [ ] Ofis alanları: OfisNo, KatNo, Ad, Yuzolcumu, Aciklama.
- [ ] OfisNo aynı bina içinde benzersizdir.
- [ ] OfisBazli bina en az 1 ofis olmadan kaydedilemez.
- [ ] Arazi/Tarla/Depo/Diger için tek `Komple` birim oluşturulur.
- [ ] Sözleşmeler ofislere veya komple birimlere bağlanır.
- [ ] Sözleşmeler katlara bağlanmaz.
- [ ] Taşınmaz detay sayfasında ofisler listelenir.
- [ ] Taşınmaz detay sayfasında ofisler isteğe bağlı olarak KatNo’ya göre görsel gruplanabilir.
- [ ] Sözleşme ekleme sayfasında yalnızca boş ofis/komple birimler listelenir.
- [ ] Feshedilmiş sözleşmeye sahip ofis yeniden kiralanabilir.
- [ ] Süresi geçmiş sözleşmeye sahip ofis yeniden kiralanabilir.
- [ ] Dashboard toplam birim sayısı ofis + komple birimler üzerinden hesaplanır.
- [ ] Dashboard gelir hesapları ofis + komple birimlere bağlı aktif sözleşmeler üzerinden hesaplanır.
- [ ] Seed veride Teknokent A Blok ve Sanayi Sitesi B Blok ofis bazlıdır.
- [ ] UI’da “Kat Bazlı Kirala” yerine “Ofis Bazlı Kirala” ifadesi kullanılır.
- [ ] UI’da “kat kiralama” algısı oluşturacak metinler temizlenir.
- [ ] Mevcut Authentication & Authorization yapısı bozulmaz.
- [ ] Mevcut kiracı/finans/TÜFE/KDV yapısı bozulmaz.
- [ ] Mevcut DummyDataService in-memory mimarisi korunur.
- [ ] Mevcut Tailwind/Magic UI premium tasarım dili korunur.
- [ ] Bootstrap veya generic MVC görünümü kullanılmaz.

---

## 32. Smoke Test Senaryoları

Aşağıdaki sayfalar manuel olarak test edilmelidir:

### 32.1 Taşınmaz Ekle

- Bina seç.
- Ofis Bazlı seç.
- 3 adet ofis ekle.
- Kaydet.
- Detay sayfasında ofislerin listelendiğini doğrula.

### 32.2 Taşınmaz Detay

- Teknokent A Blok detayına git.
- Ofis 101, Ofis 102, Ofis 201 gibi ofislerin göründüğünü doğrula.
- “1. Kat” gibi kiralanabilir kat birimlerinin olmadığını doğrula.

### 32.3 Sözleşme Ekle

- Sözleşme ekle sayfasına git.
- Boş ofisleri gör.
- Dolu ofislerin listelenmediğini doğrula.
- Boş bir ofise sözleşme oluştur.
- Dashboard rakamlarının güncellendiğini doğrula.

### 32.4 Sözleşme Detay

- Ofise bağlı sözleşme detayına git.
- Taşınmaz + Ofis No + Kat No + m² bilgilerinin doğru göründüğünü doğrula.

### 32.5 Dashboard

- Toplam birim sayısının katları değil ofisleri saydığını doğrula.
- Boş birimler listesinde boş ofislerin göründüğünü doğrula.

---

## 33. Claude Code İçin Kısa Görev Prompt’u

Aşağıdaki prompt, bu spec’i uygulatmak için kullanılabilir:

```text
docs/bina-ofis-birim-spec.md dosyasını oku ve uygula.

Token verimliliği için:
- project-spec-canonical.md dosyasını baştan sona okuma.
- Gerekirse sadece Domain Modeli, Servis Katmanı, Taşınmaz Ekle, Taşınmaz Detay, Sözleşme Ekle ve Tasarım Sistemi bölümlerine bak.
- auth-spec.md dosyasına dokunma.
- kiraci-sozlesme-finans-spec.md dosyasına dokunma; yalnızca SozlesmeDurumu veya fesih mantığı gerekiyorsa mevcut yapıyı koruyarak kullan.
- DummyDataService in-memory yapısını koru.
- Domain verilerini EF Core’a taşıma.

Görev:
Bina içindeki katlar kiralanabilir birim olmayacak. Katlar ayrı entity/model olmayacak. Kiralanabilir birimler doğrudan binaya bağlı ofisler olacak. Kat bilgisi gerekiyorsa Birim üzerinde nullable KatNo alanı olarak tutulacak.

Beklentiler:
- KiralamaSekli içindeki KatBazli yaklaşımını OfisBazli olarak değiştir veya tüm kodda OfisBazli mantığına taşı.
- BirimTipi içindeki Kat yaklaşımını Ofis olarak değiştir.
- Birim modeline OfisNo, nullable KatNo ve Aciklama alanlarını ekle.
- KatSayisi artık otomatik kat birimi üretmesin.
- Bina + OfisBazli seçildiğinde kullanıcı ofisleri tanımlayabilsin.
- Ofis alanları: OfisNo, KatNo, Ad, Yuzolcumu, Aciklama.
- Bina + TekParca seçildiğinde tek Komple birim oluşturulsun.
- Arazi/Tarla/Depo/Diger için tek Komple birim oluşturulsun.
- Taşınmaz detayında ofisler listelensin.
- Sözleşme ekleme ekranında boş ofisler seçilebilsin.
- Dashboard doluluk ve gelir hesapları ofis/komple birim üzerinden çalışsın.
- Seed veride Teknokent A Blok ve Sanayi Sitesi B Blok için ofis birimleri oluştur.
- “Kat Bazlı Kirala” gibi eski UI metinlerini “Ofis Bazlı Kirala” olarak güncelle.
- Mevcut Tailwind/Magic UI premium tasarım dili korunmalı.
- Bootstrap/generic görünüm kullanılmamalı.

İş sonunda kısa özet ver:
- Hangi modeller değişti?
- Hangi servis metotları değişti?
- Hangi controller action’ları değişti?
- Hangi view’lar güncellendi?
- Seed veri nasıl değişti?
- Smoke test için hangi sayfalar kontrol edilmeli?
```
