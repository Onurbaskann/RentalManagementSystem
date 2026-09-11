# Taşınmaz Tipi × Kiralama Şekli Refactor

## Amaç

Parametreler ekranında yönetilen `TasinmazTipi` kayıtları için hangi kiralama şekillerinin kullanılabileceği tanımlanacaktır.

Örnek:

| Taşınmaz Tipi | İzinli Kiralama Şekilleri |
|---|---|
| Bina | Tek Parça, Birim Bazlı |
| Arazi | Tek Parça |
| Tarla | Tek Parça |
| Depo | Tek Parça, Birim Bazlı |
| Kantin | Tek Parça |
| Diğer | Tek Parça, Birim Bazlı |

Bu sayede taşınmaz ekleme/düzenleme ekranında seçilen taşınmaz tipine göre uygun kiralama şekilleri gösterilecektir.

---

## Kapsam

- `TasinmazTipi` parametre ekranında kiralama şekli seçimi yapılabilir hale gelir.
- Bir taşınmaz tipi birden fazla kiralama şekline izin verebilir.
- Taşınmaz ekleme ekranında kiralama şekli seçenekleri seçilen taşınmaz tipine göre filtrelenir.
- Mevcut taşınmaz tipleri için varsayılan izinli kiralama şekilleri seed/backfill ile korunur.

---

## Kapsam Dışı

- Yeni kiralama şekli ekleme
- Rezervasyon alanı mantığını değiştirme
- Birim türü modelini değiştirme
- Tahakkuk/fiyatlandırma yapısını değiştirme
- Permission mimarisini değiştirme

---

## Model Kararı

`TasinmazTipiKiralamaSekli` ara tablosu oluşturulur.

Alanlar:

- `Id`
- `TasinmazTipiId`
- `TasinmazTipi`
- `KiralamaSekli`

C# model:

```csharp
public class TasinmazTipiKiralamaSekli
{
    public int Id { get; set; }
    public int TasinmazTipiId { get; set; }
    public TasinmazTipi TasinmazTipi { get; set; } = null!;
    public KiralamaSekli KiralamaSekli { get; set; }
}
```

Unique index: `TasinmazTipiId + KiralamaSekli`

Cascade: `TasinmazTipi` silindiğinde ilişkili satırlar silinir.

`TasinmazTipi` entity'sine `ICollection<TasinmazTipiKiralamaSekli> KiralamaSekilleri` navigation property eklenir.

---

## Admin Parametre Ekranı

`Admin/TasinmazTipi` Create/Edit ekranlarına kiralama şekli checkbox listesi eklenir.

Kurallar:

- En az bir kiralama şekli seçilmelidir (server-side validation).
- Pasif taşınmaz tipleri taşınmaz oluşturma ekranında listelenmez.
- Edit POST: mevcut satırlar silinir, seçili olanlar eklenir (idempotent).

Index ekranında "Kiralama Şekilleri" özet kolonu eklenir (rozetler ile).

---

## Taşınmaz Ekle Etkisi

`Tasinmaz/Ekle` ekranında taşınmaz tipi seçimine göre kiralama şekli radio butonları filtrelenir.

- Hiç tip seçilmediyse: tüm seçenekler pasif (disabled).
- Tek seçenek varsa: otomatik seçilir.
- Birden fazla varsa: izinli olanlar enable.
- Server-side: Submit edilen `KiralamaSekli` izinli set içinde değilse hata.

Client-side: Alpine.js. `ViewBag.TasinmazTipiKiralamaSekilleri` (Dictionary<int, int[]>) ile mapping view'a aktarılır.

---

## Migration / Backfill

Yeni tablo: `TasinmazTipiKiralamaSekilleri`

`SeedDataService.SeedTasinmazTipiKiralamaSekilleriAsync` idempotent — sadece eksik kayıtları ekler.

Mevcut seed verisindeki tipler için varsayılanlar:

| Kod | Kiralama Şekilleri |
|---|---|
| BINA | TekParca, BirimBazli |
| OTOMAT | TekParca |
| BANKAMATIK | TekParca |

Spec'te bahsedilen ARAZI/TARLA/DEPO/KANTIN/DIGER tipleri seed'de henüz yok — kullanıcı eklediğinde admin ekranından kiralama şekli seçecektir. Bu refactor onların seed'ini eklemiyor.

---

## Doğrulama

- [x] Parametreler > Taşınmaz Tipleri ekranında kiralama şekilleri seçilebiliyor.
- [x] En az bir kiralama şekli seçilmeden kayıt engelleniyor.
- [x] Taşınmaz ekleme ekranında seçilen tipe göre kiralama şekilleri filtreleniyor.
- [x] Tek seçenek varsa otomatik seçiliyor.
- [x] Mevcut taşınmaz kayıtları bozulmuyor.
- [x] `dotnet build` başarılı.

---

## Dokümantasyon

Uygulama tamamlandıktan sonra:

- `MASTER-PLAN.md` Ara Refactor'lar tablosuna eklendi.
- `PROGRESS.md`'ye kısa tamamlanma kaydı eklenir.
