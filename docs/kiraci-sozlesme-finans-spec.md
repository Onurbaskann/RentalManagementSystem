# Kiracı, Sözleşme ve Finansal Hesaplama Spec

> Bu doküman; kiracı gerçek/tüzel ayrımı, kiracı no, sözleşme süre uzatımı, sözleşme feshi, TÜFE artış hesaplama ve KDV hesaplama kapsamını tanımlar.
>
> Ana proje spec dosyasını şişirmemek için detaylar bu dosyada tutulur.
>
> Bu doküman `project-spec.md` dosyasını tamamlayıcı niteliktedir.

---

## 1. Kapsam

Bu geliştirme kapsamında aşağıdaki özellikler eklenecektir:

- Kiracı türü: **Gerçek** / **Tüzel**
- Kiracı no alanı
- Kiracı türüne göre form alanlarının gösterilmesi/gizlenmesi
- Kiracı türüne göre ilgisiz alanların server-side olarak temizlenmesi
- Sözleşme süre uzatımı
- Sözleşme fesih işlemi
- TÜFE artış hesaplama
- KDV hesaplama
- TÜFE + KDV birlikte hesaplama
- Auth rol kurallarına uygun işlem yetkileri

---

## 2. Genel Teknik Kurallar

- Proje `.NET 8 MVC` yapısını koruyacaktır.
- Domain verileri veritabanına taşınmayacaktır.
- `DummyDataService` in-memory yapı olarak kalacaktır.
- Mevcut Authentication & Authorization yapısı bozulmayacaktır.
- Identity sadece kullanıcı/rol yönetimi için kullanılmaya devam edecektir.
- Yeni özellikler mevcut premium Tailwind/Magic UI tasarım diliyle uyumlu olmalıdır.
- Bootstrap görünümü veya scaffold Identity UI kullanılmamalıdır.
- Gereksiz büyük mimari dönüşüm yapılmamalıdır.

---

## 3. Kiracı Türü

Mevcut `bool Kurumsal` yapısı yerine daha açık ve sürdürülebilir bir enum kullanılacaktır.

### 3.1 Yeni Enum

`Models/Enums.cs` içine eklenecektir:

```csharp
public enum KiraciTuru
{
    Gercek = 1,
    Tuzel = 2
}
```

### 3.2 Eski Alan Dönüşümü

Eski modelde varsa:

```csharp
public bool Kurumsal { get; set; }
public string AdSoyad { get; set; }
```

Bu yapı yeni modele göre güncellenecektir.

Yeni yaklaşım:

- `Kurumsal == false` karşılığı: `KiraciTuru.Gercek`
- `Kurumsal == true` karşılığı: `KiraciTuru.Tuzel`
- `AdSoyad` yerine:
  - Gerçek kişi için: `Ad + Soyad`
  - Tüzel kişi için: `Ad`

---

## 4. Kiracı Modeli

`Models/Kiraci.cs` aşağıdaki yapıya göre güncellenecektir.

```csharp
public class Kiraci
{
    public int Id { get; set; }

    public string KiraciNo { get; set; } = string.Empty;

    public KiraciTuru KiraciTuru { get; set; }

    // Ortak alan
    // Gerçek kişi için ad
    // Tüzel kişi için firma/kurum adı
    public string Ad { get; set; } = string.Empty;

    // Gerçek kişi alanları
    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }

    // Tüzel kişi alanları
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }

    // İletişim
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }

    public DateTime KayitTarihi { get; set; }

    public string GosterimAdi =>
        KiraciTuru == KiraciTuru.Gercek
            ? $"{Ad} {Soyad}".Trim()
            : Ad;
}
```

---

## 5. Kiracı No

Her kiracının benzersiz bir `KiraciNo` alanı olacaktır.

### 5.1 Kurallar

- Zorunludur.
- Benzersiz olmalıdır.
- Manuel girilebilir.
- Boş bırakılırsa sistem otomatik üretebilir.
- Otomatik üretim `DummyDataService` içinde yapılabilir.

### 5.2 Önerilen Format

```text
KRC-000001
KRC-000002
KRC-000003
```

### 5.3 Benzersizlik Kontrolü

`DummyDataService` içinde aşağıdaki mantık bulunmalıdır:

- Yeni kiracı eklenirken aynı `KiraciNo` var mı kontrol edilir.
- Varsa validation hatası döndürülür veya controller `ModelState.AddModelError` kullanır.
- Güncelleme sırasında aynı `KiraciNo` başka bir kiracıya aitse hata verilir.

---

## 6. Kiracı Form Davranışı

Kiracı ekleme ve düzenleme formlarında `KiraciTuru` seçimi bulunacaktır.

Seçenekler:

- Gerçek
- Tüzel

Bu alan zorunludur.

---

## 7. Gerçek Kiracı Seçildiğinde

Kiracı türü **Gerçek** seçildiğinde aşağıdaki alanlar gösterilecek ve doldurulabilir olacaktır:

| Alan | Durum |
|---|---|
| Kiracı No | Gösterilir |
| Kiracı Türü | Gösterilir |
| Ad | Gösterilir |
| Soyad | Gösterilir |
| T.C. Kimlik No | Gösterilir |
| Pasaport No | Gösterilir |
| Ünvanı | Gösterilir |
| Anne Adı | Gösterilir |
| Baba Adı | Gösterilir |
| Doğum Tarihi | Gösterilir |
| Doğum Yeri | Gösterilir |
| Telefon | Gösterilir |
| Email | Gösterilir |
| Adres | Gösterilir |

Aşağıdaki tüzel kişi alanları **gösterilmeyecek** veya **disabled** olacaktır:

| Alan | Durum |
|---|---|
| Ticaret Sicil No | Gizlenir veya disabled |
| Vergi No | Gizlenir veya disabled |
| Vergi Dairesi | Gizlenir veya disabled |
| Mersis No | Gizlenir veya disabled |

### 7.1 Gerçek Kiracı Server-Side Temizleme

Gerçek kiracı kaydedilirken aşağıdaki alanlar server-side olarak `null` yapılmalıdır:

```csharp
TicaretSicilNo = null;
VergiNo = null;
VergiDairesi = null;
MersisNo = null;
```

Bu kural önemlidir. Kullanıcı arayüzünde alanlar gizlense bile malicious POST ile veri gönderilebilir. Bu yüzden controller veya service katmanında temizleme yapılmalıdır.

---

## 8. Tüzel Kiracı Seçildiğinde

Kiracı türü **Tüzel** seçildiğinde aşağıdaki alanlar gösterilecek ve doldurulabilir olacaktır:

| Alan | Durum |
|---|---|
| Kiracı No | Gösterilir |
| Kiracı Türü | Gösterilir |
| Ad / Firma Adı | Gösterilir |
| Ticaret Sicil No | Gösterilir |
| Vergi No | Gösterilir |
| Vergi Dairesi | Gösterilir |
| Mersis No | Gösterilir |
| Telefon | Gösterilir |
| Email | Gösterilir |
| Adres | Gösterilir |

Aşağıdaki gerçek kişi alanları **gösterilmeyecek** veya **disabled** olacaktır:

| Alan | Durum |
|---|---|
| Soyad | Gizlenir veya disabled |
| T.C. Kimlik No | Gizlenir veya disabled |
| Pasaport No | Gizlenir veya disabled |
| Ünvanı | Gizlenir veya disabled |
| Anne Adı | Gizlenir veya disabled |
| Baba Adı | Gizlenir veya disabled |
| Doğum Tarihi | Gizlenir veya disabled |
| Doğum Yeri | Gizlenir veya disabled |

### 8.1 Tüzel Kiracı Server-Side Temizleme

Tüzel kiracı kaydedilirken aşağıdaki alanlar server-side olarak `null` yapılmalıdır:

```csharp
Soyad = null;
TcKimlikNo = null;
PasaportNo = null;
Unvan = null;
AnneAdi = null;
BabaAdi = null;
DogumTarihi = null;
DogumYeri = null;
```

Bu kural önemlidir. Kullanıcı arayüzünde alanlar gizlense bile malicious POST ile veri gönderilebilir. Bu yüzden controller veya service katmanında temizleme yapılmalıdır.

---

## 9. Kiracı Form Validasyon Kuralları

### 9.1 Ortak Zorunlu Alanlar

Her kiracı türü için zorunlu alanlar:

- Kiracı No
- Kiracı Türü
- Ad
- Telefon
- Email

### 9.2 Gerçek Kiracı Zorunlu Alanları

Gerçek kiracı için ek zorunlu alan:

- Soyad

### 9.3 Tüzel Kiracı Zorunlu Alanları

Tüzel kiracı için `Ad` alanı firma/kurum adı olarak kullanılır.

Tüzel kiracı için aşağıdaki alanlar bu aşamada doldurulabilir alanlardır, ancak zorunluluk seviyesi iş kuralına göre artırılabilir:

- Ticaret Sicil No
- Vergi No
- Vergi Dairesi
- Mersis No

Bu aşamada minimum validasyon:

- `Ad` boş olamaz.
- `KiraciTuru` boş olamaz.
- Gerçek/Tüzel tipine ait olmayan alanlar kaydedilmez.

---

## 10. Kiracı Form UI

Kiracı ekleme ve düzenleme formlarında Alpine.js veya vanilla JS ile dinamik davranış sağlanacaktır.

Tercih edilen yaklaşım:

- Kiracı türü seçiminde segmented control veya select kullanılabilir.
- Gerçek/Tüzel değiştiğinde ilgili alan grubu animasyonlu şekilde görünür.
- İlgisiz alanlar tamamen gizlenir.
- Disabled bırakılacaksa görsel olarak pasif olduğu net anlaşılır.
- Ancak tercih edilen davranış: **gizlemek**.

### 10.1 Örnek Alpine State

```html
<div x-data="{ kiraciTuru: 'Gercek' }">
    <!-- Gerçek kişi alanları -->
    <div x-show="kiraciTuru === 'Gercek'">
        ...
    </div>

    <!-- Tüzel kişi alanları -->
    <div x-show="kiraciTuru === 'Tuzel'">
        ...
    </div>
</div>
```

---

## 11. Kiracı Liste ve Detay Sayfaları

Kiracı liste ve detay sayfaları yeni alanlara göre güncellenecektir.

### 11.1 Liste Sayfası

Kiracı listesinde gösterilecek önerilen kolonlar:

| Kolon | Açıklama |
|---|---|
| Kiracı No | `KRC-000001` |
| Ad / Firma | Gerçek için Ad Soyad, tüzel için Ad |
| Tür | Gerçek / Tüzel badge |
| Telefon | Telefon |
| Email | Email |
| Aktif Sözleşme | Sayı |
| Kayıt Tarihi | Kısa tarih |
| İşlemler | Detay / Düzenle |

### 11.2 Detay Sayfası

Kiracı detayında:

- Kiracı No
- Kiracı Türü
- Ad / Firma
- Gerçek kişi alanları veya tüzel kişi alanları
- İletişim bilgileri
- Adres
- Bu kiracıya ait sözleşmeler

gösterilecektir.

Kiracı türüne ait olmayan alanlar detay sayfasında da gösterilmemelidir.

---

## 12. Sözleşme Durumu

Sözleşmeler artık fesih ve süre uzatma işlemlerini destekleyecektir.

### 12.1 Yeni Enum

`Models/Enums.cs` içine eklenecektir:

```csharp
public enum SozlesmeDurumu
{
    Aktif = 1,
    SonaErdi = 2,
    Feshedildi = 3
}
```

> Not: “Süresi dolmak üzere” kalıcı bir durum değildir. Tarihe göre hesaplanan bir görünüm durumudur.

---

## 13. Sözleşme İşlem Geçmişi

Sözleşme üzerinde yapılan önemli işlemler için işlem geçmişi tutulacaktır.

### 13.1 Yeni Enum

```csharp
public enum SozlesmeIslemTipi
{
    Olusturma = 1,
    SureUzatma = 2,
    Fesih = 3,
    TufeArtis = 4,
    KdvGuncelleme = 5
}
```

### 13.2 Yeni Model

`Models/SozlesmeIslemGecmisi.cs` oluşturulacaktır:

```csharp
public class SozlesmeIslemGecmisi
{
    public int Id { get; set; }

    public int KiraSozlesmesiId { get; set; }

    public SozlesmeIslemTipi IslemTipi { get; set; }

    public DateTime IslemTarihi { get; set; }

    public string Aciklama { get; set; } = string.Empty;

    public DateTime? EskiBitisTarihi { get; set; }
    public DateTime? YeniBitisTarihi { get; set; }

    public decimal? EskiKiraBedeli { get; set; }
    public decimal? YeniKiraBedeli { get; set; }

    public decimal? TufeOrani { get; set; }

    public bool? KdvUygulandiMi { get; set; }
    public decimal? KdvOrani { get; set; }
    public decimal? KdvTutari { get; set; }
    public decimal? KdvDahilTutar { get; set; }
}
```

---

## 14. Kira Sözleşmesi Modeli Genişletmesi

`Models/KiraSozlesmesi.cs` aşağıdaki alanlarla genişletilecektir.

```csharp
public class KiraSozlesmesi
{
    public int Id { get; set; }

    public int BirimId { get; set; }
    public Birim Birim { get; set; }

    public int KiraciId { get; set; }
    public Kiraci Kiraci { get; set; }

    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }

    // KDV hariç ana kira bedeli
    public decimal KiraBedeli { get; set; }

    public KiraPeriyodu Periyot { get; set; }

    public decimal? Depozito { get; set; }

    public string? Notlar { get; set; }

    public SozlesmeDurumu Durum { get; set; } = SozlesmeDurumu.Aktif;

    // Fesih bilgileri
    public DateTime? FesihTarihi { get; set; }
    public string? FesihNedeni { get; set; }

    // KDV bilgileri
    public bool KdvUygulanacakMi { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    // İşlem geçmişi
    public List<SozlesmeIslemGecmisi> IslemGecmisi { get; set; } = new();
}
```

---

## 15. Aktif Sözleşme Kuralı

Bir sözleşme aktif sayılabilmesi için:

- `Durum == SozlesmeDurumu.Aktif`
- `BaslangicTarihi <= DateTime.Now`
- `BitisTarihi >= DateTime.Now`

olmalıdır.

Feshedilmiş sözleşmeler aktif kabul edilmez.

```csharp
public bool SozlesmeAktifMi(KiraSozlesmesi s)
{
    return s.Durum == SozlesmeDurumu.Aktif
        && s.BaslangicTarihi <= DateTime.Now
        && s.BitisTarihi >= DateTime.Now;
}
```

---

## 16. Birim Durumu Hesaplama

`IstatistikService.GetBirimDurumu` metodu güncellenmelidir.

Feshedilmiş sözleşmeler dikkate alınmamalıdır.

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

---

## 17. Kira Süre Uzatımı

Admin ve Yonetici rolündeki kullanıcılar sözleşme süresini uzatabilir.

### 17.1 Yetki

| Rol | Süre Uzatabilir |
|---|---:|
| Admin | Evet |
| Yonetici | Evet |
| Goruntuleyici | Hayır |

### 17.2 Form Alanları

Sözleşme süre uzatma işleminde şu alanlar alınacaktır:

- Yeni bitiş tarihi
- Yeni kira bedeli
- TÜFE uygulanacak mı?
- TÜFE oranı
- KDV uygulanacak mı?
- KDV oranı
- Açıklama / not

### 17.3 Kurallar

- Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.
- Yeni kira bedeli sıfırdan büyük olmalıdır.
- TÜFE oranı negatif olamaz.
- KDV oranı negatif olamaz.
- KDV oranı varsayılan olarak `%20` olmalıdır.
- Süre uzatma sonrası sözleşme `Aktif` kalır.
- İşlem geçmişine `SureUzatma` kaydı eklenir.

### 17.4 Süre Uzatma Davranışı

Basit demo yaklaşımı:

- Mevcut sözleşmenin `BitisTarihi` güncellenir.
- Mevcut sözleşmenin `KiraBedeli` güncellenir.
- Varsa KDV bilgileri güncellenir.
- İşlem geçmişine eski/yeni değerler yazılır.

Bu aşamada ayrı bir yenileme sözleşmesi oluşturulmayacaktır.

---

## 18. Kira Fesih İşlemi

Admin ve Yonetici rolündeki kullanıcılar sözleşmeyi feshedebilir.

### 18.1 Yetki

| Rol | Fesih Yapabilir |
|---|---:|
| Admin | Evet |
| Yonetici | Evet |
| Goruntuleyici | Hayır |

### 18.2 Form Alanları

Fesih işleminde şu alanlar alınacaktır:

- Fesih tarihi
- Fesih nedeni
- Açıklama / not

### 18.3 Kurallar

- Feshedilmiş sözleşme tekrar aktif hale getirilmez.
- Fesih sonrası sözleşme durumu `Feshedildi` olur.
- Fesih tarihi sözleşmeye yazılır.
- Fesih nedeni sözleşmeye yazılır.
- İşlem geçmişine `Fesih` kaydı eklenir.
- Feshedilen sözleşme aktif gelir hesaplarına dahil edilmez.
- Feshedilen sözleşmenin birimi tekrar kiralanabilir hale gelir.

### 18.4 Birimin Tekrar Kiralanabilir Olması

`GetBosBirimler()` metodu güncellenmelidir.

Bir birimde yalnızca aktif ve feshedilmemiş sözleşme varsa dolu kabul edilir.

Feshedilmiş veya sona ermiş sözleşmeler birimin kiralanmasını engellememelidir.

---

## 19. TÜFE Artış Hesaplama

TÜFE artışı manuel oran girilerek hesaplanacaktır.

Harici TÜFE API entegrasyonu bu aşamada yapılmayacaktır.

### 19.1 Form Alanları

- Mevcut kira bedeli
- TÜFE oranı
- Artış tutarı
- Yeni kira bedeli

### 19.2 Hesaplama Formülü

```text
Artış Tutarı = Mevcut Kira Bedeli * TÜFE Oranı / 100

Yeni Kira Bedeli = Mevcut Kira Bedeli + Artış Tutarı
```

### 19.3 Örnek

```text
Mevcut Kira Bedeli: 10.000 ₺
TÜFE Oranı: %25

Artış Tutarı: 2.500 ₺
Yeni Kira Bedeli: 12.500 ₺
```

### 19.4 Helper Metodu

`IstatistikService` veya ayrı bir helper/service içinde aşağıdaki mantık bulunabilir:

```csharp
public decimal TufeArtisliBedel(decimal mevcutBedel, decimal tufeOrani)
{
    if (tufeOrani < 0)
        throw new ArgumentException("TÜFE oranı negatif olamaz.");

    return mevcutBedel + (mevcutBedel * tufeOrani / 100);
}
```

---

## 20. KDV Hesaplama

KDV kira hesaplamalarında desteklenecektir.

### 20.1 Ana Kural

`KiraBedeli` alanı **KDV hariç ana kira bedeli** olarak kabul edilir.

KDV ayrıca hesaplanır.

### 20.2 Varsayılan KDV Oranı

```text
%20
```

### 20.3 Form Alanları

- KDV uygulanacak mı?
- KDV oranı
- KDV hariç kira bedeli
- KDV tutarı
- KDV dahil toplam kira bedeli

### 20.4 Hesaplama Formülü

```text
KDV Tutarı = KDV Hariç Bedel * KDV Oranı / 100

KDV Dahil Toplam = KDV Hariç Bedel + KDV Tutarı
```

### 20.5 Örnek

```text
KDV Hariç Kira: 12.500 ₺
KDV Oranı: %20

KDV Tutarı: 2.500 ₺
KDV Dahil Toplam: 15.000 ₺
```

### 20.6 Helper Metotları

```csharp
public decimal KdvTutari(decimal kdvHaricBedel, decimal kdvOrani)
{
    if (kdvOrani < 0)
        throw new ArgumentException("KDV oranı negatif olamaz.");

    return kdvHaricBedel * kdvOrani / 100;
}

public decimal KdvDahilTutar(decimal kdvHaricBedel, decimal kdvOrani)
{
    return kdvHaricBedel + KdvTutari(kdvHaricBedel, kdvOrani);
}
```

---

## 21. TÜFE + KDV Birlikte Hesaplama

TÜFE ve KDV birlikte uygulanırsa hesaplama sırası aşağıdaki gibi olmalıdır:

```text
1. Önce TÜFE artışı hesaplanır.
2. TÜFE sonrası yeni kira bedeli bulunur.
3. KDV, TÜFE sonrası yeni kira bedeli üzerinden hesaplanır.
```

### 21.1 Örnek

```text
Mevcut Kira Bedeli: 10.000 ₺
TÜFE Oranı: %25
KDV Oranı: %20

TÜFE Artışı: 2.500 ₺
TÜFE Sonrası Kira: 12.500 ₺
KDV Tutarı: 2.500 ₺
KDV Dahil Toplam: 15.000 ₺
```

### 21.2 Helper Model

```csharp
public class KiraHesaplamaSonucu
{
    public decimal MevcutKiraBedeli { get; set; }
    public decimal? TufeOrani { get; set; }
    public decimal TufeArtisTutari { get; set; }
    public decimal TufeSonrasiKiraBedeli { get; set; }

    public bool KdvUygulandiMi { get; set; }
    public decimal? KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal KdvDahilToplam { get; set; }
}
```

### 21.3 Helper Metodu

```csharp
public KiraHesaplamaSonucu HesaplaKiraArtisi(
    decimal mevcutKiraBedeli,
    decimal? tufeOrani,
    bool kdvUygulanacakMi,
    decimal? kdvOrani)
{
    var sonuc = new KiraHesaplamaSonucu
    {
        MevcutKiraBedeli = mevcutKiraBedeli,
        TufeOrani = tufeOrani,
        KdvUygulandiMi = kdvUygulanacakMi,
        KdvOrani = kdvUygulanacakMi ? (kdvOrani ?? 20) : null
    };

    var tufeArtisTutari = tufeOrani.HasValue
        ? mevcutKiraBedeli * tufeOrani.Value / 100
        : 0;

    var tufeSonrasiBedel = mevcutKiraBedeli + tufeArtisTutari;

    sonuc.TufeArtisTutari = tufeArtisTutari;
    sonuc.TufeSonrasiKiraBedeli = tufeSonrasiBedel;

    if (kdvUygulanacakMi)
    {
        var oran = kdvOrani ?? 20;
        sonuc.KdvTutari = tufeSonrasiBedel * oran / 100;
        sonuc.KdvDahilToplam = tufeSonrasiBedel + sonuc.KdvTutari;
    }
    else
    {
        sonuc.KdvTutari = 0;
        sonuc.KdvDahilToplam = tufeSonrasiBedel;
    }

    return sonuc;
}
```

---

## 22. Dashboard ve Gelir Hesapları

Dashboard gelir hesapları güncellenmelidir.

### 22.1 KDV Hariç Gelir

Ana kira geliri `KiraBedeli` üzerinden hesaplanır.

Bu değer KDV hariç kira geliridir.

### 22.2 KDV Dahil Tahsilat

KDV uygulanıyorsa ayrıca KDV dahil tahsilat toplamı hesaplanabilir.

Dashboard’da tercihen şu iki değer gösterilir:

- Aylık kira geliri
- KDV dahil aylık tahsilat

### 22.3 Feshedilen Sözleşmeler

Feshedilmiş sözleşmeler:

- Aktif sözleşme sayısına dahil edilmez.
- Aylık gelir toplamına dahil edilmez.
- Yıllık projeksiyona dahil edilmez.
- Boş birimler hesaplamasında birimin yeniden kiralanmasına engel olmaz.

---

## 23. Sözleşme Detay UI Güncellemesi

`Views/Sozlesme/Detay.cshtml` güncellenecektir.

Sözleşme detay sayfasında aşağıdaki işlemler bulunmalıdır:

- Sözleşmeyi Uzat
- Sözleşmeyi Feshet
- TÜFE / KDV Hesapla

### 23.1 Butonlar

| Buton | Yetki |
|---|---|
| Sözleşmeyi Uzat | Admin, Yonetici |
| Sözleşmeyi Feshet | Admin, Yonetici |
| TÜFE / KDV Hesapla | Admin, Yonetici |

Goruntuleyici rolündeki kullanıcılar bu butonları görmemelidir.

### 23.2 Modal Tercihi

Tercih edilen UI:

- Süre uzatma: Modal
- Fesih: Onay modalı + fesih nedeni alanı
- TÜFE/KDV hesaplama: Süre uzatma modalı içinde hesaplama alanı veya ayrı modal

### 23.3 Sözleşme Detayında Gösterilecek Yeni Alanlar

- Sözleşme durumu
- KDV uygulanıyor mu?
- KDV oranı
- KDV hariç kira bedeli
- KDV tutarı
- KDV dahil toplam
- Fesih tarihi
- Fesih nedeni
- İşlem geçmişi

---

## 24. Controller Route Yapısı

### 24.1 Kiracı

Mevcut route’lar korunur.

Ek olarak düzenleme sayfası yoksa eklenebilir:

| Controller | Action | Route | Açıklama |
|---|---|---|---|
| `KiraciController` | `Ekle` | `/Kiraci/Ekle` | Yeni kiracı |
| `KiraciController` | `Duzenle` | `/Kiraci/Duzenle/{id}` | Kiracı güncelleme |
| `KiraciController` | `Detay` | `/Kiraci/Detay/{id}` | Kiracı detay |

### 24.2 Sözleşme

Sözleşme controller’a aşağıdaki action’lar eklenmelidir:

| Controller | Action | Route | Açıklama |
|---|---|---|---|
| `SozlesmeController` | `Uzat` | `/Sozlesme/Uzat/{id}` | Sözleşme süre uzatma |
| `SozlesmeController` | `Feshet` | `/Sozlesme/Feshet/{id}` | Sözleşme fesih |
| `SozlesmeController` | `HesaplaTufeKdv` | `/Sozlesme/HesaplaTufeKdv` | TÜFE/KDV hesaplama |

`HesaplaTufeKdv` action’ı JSON dönebilir.

Örnek:

```csharp
[HttpPost]
[Authorize(Roles = "Admin,Yonetici")]
public IActionResult HesaplaTufeKdv(decimal mevcutBedel, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani)
{
    var sonuc = _istatistikService.HesaplaKiraArtisi(mevcutBedel, tufeOrani, kdvUygulanacakMi, kdvOrani);
    return Json(sonuc);
}
```

---

## 25. Yetkilendirme Kuralları

Mevcut Authentication & Authorization yapısına uyulacaktır.

| İşlem | Admin | Yonetici | Goruntuleyici |
|---|---:|---:|---:|
| Kiracı listeleme | Evet | Evet | Evet |
| Kiracı detay | Evet | Evet | Evet |
| Kiracı ekleme | Evet | Evet | Hayır |
| Kiracı düzenleme | Evet | Evet | Hayır |
| Sözleşme detay | Evet | Evet | Evet |
| Sözleşme süre uzatma | Evet | Evet | Hayır |
| Sözleşme fesih | Evet | Evet | Hayır |
| TÜFE/KDV hesaplama | Evet | Evet | Hayır |

Write action’larda aşağıdaki attribute kullanılmalıdır:

```csharp
[Authorize(Roles = "Admin,Yonetici")]
```

Goruntuleyici işlem butonlarını UI’da görmemelidir.

---

## 26. ViewModel Önerileri

### 26.1 Kiracı Form ViewModel

```csharp
public class KiraciFormViewModel
{
    public int? Id { get; set; }

    public string KiraciNo { get; set; } = string.Empty;

    public KiraciTuru KiraciTuru { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }

    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }

    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
}
```

### 26.2 Sözleşme Uzatma ViewModel

```csharp
public class SozlesmeUzatViewModel
{
    public int SozlesmeId { get; set; }

    public DateTime YeniBitisTarihi { get; set; }

    public decimal YeniKiraBedeli { get; set; }

    public bool TufeUygulanacakMi { get; set; }

    public decimal? TufeOrani { get; set; }

    public bool KdvUygulanacakMi { get; set; }

    public decimal? KdvOrani { get; set; }

    public string? Aciklama { get; set; }
}
```

### 26.3 Sözleşme Fesih ViewModel

```csharp
public class SozlesmeFesihViewModel
{
    public int SozlesmeId { get; set; }

    public DateTime FesihTarihi { get; set; }

    public string FesihNedeni { get; set; } = string.Empty;

    public string? Aciklama { get; set; }
}
```

---

## 27. DummyDataService Güncellemeleri

`DummyDataService` aşağıdaki metotlarla genişletilecektir.

### 27.1 Kiracı

```csharp
IEnumerable<Kiraci> GetAllKiraci();

Kiraci? GetKiraci(int id);

int AddKiraci(Kiraci kiraci);

void UpdateKiraci(Kiraci kiraci);

bool KiraciNoExists(string kiraciNo, int? excludeId = null);

string GenerateKiraciNo();
```

### 27.2 Sözleşme

```csharp
KiraSozlesmesi? GetSozlesme(int id);

void UzatSozlesme(
    int sozlesmeId,
    DateTime yeniBitisTarihi,
    decimal yeniKiraBedeli,
    bool kdvUygulanacakMi,
    decimal kdvOrani,
    decimal? tufeOrani,
    string? aciklama);

void FeshetSozlesme(
    int sozlesmeId,
    DateTime fesihTarihi,
    string fesihNedeni,
    string? aciklama);

IEnumerable<Birim> GetBosBirimler();
```

---

## 28. Seed Veri Güncellemesi

Seed kiracılar gerçek/tüzel ayrımına göre güncellenmelidir.

### 28.1 Gerçek Kiracı Örnekleri

- Ahmet Yılmaz
- Ayşe Demir
- Mehmet Kaya

Örnek alanlar:

- Kiracı No
- Ad
- Soyad
- T.C. Kimlik No
- Telefon
- Email
- Adres

### 28.2 Tüzel Kiracı Örnekleri

- Yıldız Yazılım A.Ş.
- Anadolu Lojistik Ltd.
- Ege Tarım Koop.
- Mavi Cafe & Restoran

Örnek alanlar:

- Kiracı No
- Ad / Firma Adı
- Ticaret Sicil No
- Vergi No
- Vergi Dairesi
- Mersis No
- Telefon
- Email
- Adres

### 28.3 Sözleşme Seed Güncellemesi

Bazı sözleşmelerde KDV uygulanacak şekilde seed veri hazırlanmalıdır.

Örnek:

- Kurumsal kiracılara ait sözleşmelerde `KdvUygulanacakMi = true`
- KDV oranı `%20`
- Bireysel kiracılarda KDV uygulanmayabilir

---

## 29. Formatlama Kuralları

Para formatı Türkçe gösterilmelidir:

```text
1.250.000 ₺
```

Yüzde formatı:

```text
%20
%25,50
```

Tarih formatı:

```text
12 Mar 2025
```

Helper metotlar `Helpers/FormatHelpers.cs` içinde tutulabilir.

Örnek:

```csharp
public static string Tl(this decimal v)
{
    return v.ToString("N0", new CultureInfo("tr-TR")) + " ₺";
}

public static string Yuzde(this decimal v)
{
    return "%" + v.ToString("N2", new CultureInfo("tr-TR"));
}
```

---

## 30. Güncellenecek Dosyalar

Muhtemel dosyalar:

```text
Models/Enums.cs
Models/Kiraci.cs
Models/KiraSozlesmesi.cs
Models/SozlesmeIslemGecmisi.cs

Models/ViewModels/KiraciFormViewModel.cs
Models/ViewModels/SozlesmeUzatViewModel.cs
Models/ViewModels/SozlesmeFesihViewModel.cs
Models/ViewModels/KiraHesaplamaSonucu.cs

Services/DummyDataService.cs
Services/IstatistikService.cs

Helpers/FormatHelpers.cs

Controllers/KiraciController.cs
Controllers/SozlesmeController.cs

Views/Kiraci/Index.cshtml
Views/Kiraci/Detay.cshtml
Views/Kiraci/Ekle.cshtml
Views/Kiraci/Duzenle.cshtml

Views/Sozlesme/Detay.cshtml
Views/Sozlesme/Ekle.cshtml

wwwroot/js/kiraci-form.js
wwwroot/js/sozlesme-form.js
```

---

## 31. Uygulama Sırası

Claude Code veya benzeri AI aracı aşağıdaki sırayla ilerlemelidir:

1. `Models/Enums.cs` içine `KiraciTuru`, `SozlesmeDurumu`, `SozlesmeIslemTipi` enumlarını ekle.
2. `Kiraci` modelini gerçek/tüzel ayrımına göre güncelle.
3. `KiraSozlesmesi` modelini durum, fesih ve KDV alanlarıyla genişlet.
4. `SozlesmeIslemGecmisi` modelini oluştur.
5. Gerekli ViewModel dosyalarını oluştur.
6. `DummyDataService` içinde kiracı no üretimi ve benzersizlik kontrolünü ekle.
7. `DummyDataService` içinde süre uzatma ve fesih metotlarını ekle.
8. `GetBosBirimler` ve aktif sözleşme hesaplarını fesih durumunu dikkate alacak şekilde güncelle.
9. `IstatistikService` içinde TÜFE/KDV hesaplama metotlarını ekle.
10. Kiracı ekleme/düzenleme formlarını gerçek/tüzel alan mantığına göre güncelle.
11. Server-side temizleme ve validasyon kurallarını ekle.
12. Kiracı liste/detay sayfalarını yeni alanlara göre güncelle.
13. Sözleşme detay sayfasına süre uzatma, fesih ve TÜFE/KDV hesaplama UI’larını ekle.
14. Yetki kontrollerini Admin/Yonetici/Goruntuleyici rollerine göre uygula.
15. Seed verisini gerçek/tüzel ve KDV örnekleriyle güncelle.
16. Smoke test yap.

---

## 32. Kabul Kriterleri

- [ ] Kiracı ekleme formunda Kiracı No alanı vardır.
- [ ] Kiracı No zorunlu ve benzersizdir.
- [ ] Kiracı türü Gerçek/Tüzel olarak seçilebilir.
- [ ] Gerçek seçildiğinde tüzel kişi alanları gösterilmez veya disabled olur.
- [ ] Tüzel seçildiğinde gerçek kişi alanları gösterilmez veya disabled olur.
- [ ] Gerçek kiracı kaydedilirken tüzel kişi alanları server-side olarak temizlenir.
- [ ] Tüzel kiracı kaydedilirken gerçek kişi alanları server-side olarak temizlenir.
- [ ] Kiracı listesi Kiracı No, tür ve ad/firma bilgisini doğru gösterir.
- [ ] Kiracı detay sayfası yalnızca ilgili kiracı türüne ait alanları gösterir.
- [ ] Sözleşme detayında “Sözleşmeyi Uzat” işlemi yapılabilir.
- [ ] Sözleşme detayında “Sözleşmeyi Feshet” işlemi yapılabilir.
- [ ] Feshedilen sözleşme aktif sözleşme sayısına dahil edilmez.
- [ ] Feshedilen sözleşme kira gelir hesaplarına dahil edilmez.
- [ ] Feshedilen sözleşmenin birimi yeniden kiralanabilir hale gelir.
- [ ] TÜFE oranı girilerek yeni kira bedeli hesaplanabilir.
- [ ] KDV oranı girilerek KDV tutarı ve KDV dahil toplam hesaplanabilir.
- [ ] TÜFE ve KDV birlikte uygulanırsa önce TÜFE, sonra KDV hesaplanır.
- [ ] Kurumsal/tüzel kiracı sözleşmelerinde KDV örnekleri seed veride bulunur.
- [ ] Admin ve Yonetici süre uzatma, fesih ve hesaplama işlemlerini yapabilir.
- [ ] Goruntuleyici bu işlem butonlarını göremez.
- [ ] Mevcut Authentication & Authorization yapısı bozulmaz.
- [ ] Mevcut DummyDataService in-memory mimarisi bozulmaz.
- [ ] Mevcut Tailwind/Magic UI premium tasarım dili korunur.
- [ ] Bootstrap veya generic MVC görünümü kullanılmaz.

---