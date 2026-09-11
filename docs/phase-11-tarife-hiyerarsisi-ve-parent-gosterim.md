# Faz 11 — Tarife Hiyerarşisi (Kategori Bazlı Genel Tarife + Parent Bilgi Gösterimi)

> **GÜNCELLİK NOTU:** Bu spec yazıldığında `BorcTipiDavranisi.ManuelTetiklemeli` kullanılıyordu.
> Faz 9-1 refactor'u ile bu değer ayrıştırıldı: `KullaniciManuel = 3`, `RezervasyonOzel = 4`.
> Spec içindeki `ManuelTetiklemeli` ifadelerini güncel kodda `KullaniciManuel || RezervasyonOzel` olarak okuyun.
> Detay: `phase-9-1-borc-tipi-davranisi-refactor.md`

> Önkoşul: Faz 9 (Fiyatlandırma Mimarisi) ve Faz 10 (Detay UX) tamamlandı.
> Bu spec onlara dayanır; resolver precedence değişmez, sadece **Tarife** katmanına `KiraciKategoriId` eklenir ve 3 ekrana **parent tarife bilgi kartı** konur.

---

## 0. Karar Özeti (kullanıcı onaylı)

| # | Karar |
|---|---|
| 1 | Hiyerarşi: **Sözleşme → Birim → Taşınmaz → Genel** (child en güçlü). Resolver precedence Faz 9'dan AYNEN korunur. |
| 2 | Child'da o `(BorcTipi, KiraciKategori)` çifti yoksa parent'a düşülür. Hiçbir katmanda yoksa resolver `null` → composer 0₺ kalem üretir (Faz 9.4.3). "Default kategori"ye atlama YOK. |
| 3 | `TarifeKalemi` artık `(TarifeId, KiraciKategoriId, BorcTipiId)` unique. Kategori zorunlu (NOT NULL). |
| 4 | Mevcut Tarife verisi dummy — migration veri taşıma yapmaz, seed yeniden çalıştırılır. |
| 5 | Rename: **"Global Kural (Varsayılan)" → "Genel Tarife"** (kira + rezervasyon, tüm view metinleri). |
| 6 | Parent bilgi kartı UI baz alacağı stil: mevcut `Birim/.../OzelFiyat` rezervasyon "Global Kural (Varsayılan)" kartı. |
| 7 | Genel tarife yıl bazlı; bilgi kartı **bulunulan yılın** (`DateTime.Now.Year`) tarifesini çeker. O yıl tarifesi yoksa bilgi mesajı: "Bu yıl için genel tarife tanımlanmamış." |

---

## 1. Şema Değişikliği — `TarifeKalemi.KiraciKategoriId`

**Dosya:** `Models/Tarife.cs`

- 1.1 `TarifeKalemi`'ne `int KiraciKategoriId` + nav `KiraciKategori KiraciKategori` eklenir.
- 1.2 `ApplicationDbContext.OnModelCreating`:
  - Eski unique index `(TarifeId, BorcTipiId)` kaldırılır.
  - Yeni unique index: `(TarifeId, KiraciKategoriId, BorcTipiId)`.
  - FK: `KiraciKategori` → `KiraciKategoriler`, `OnDelete: Restrict`.
- 1.3 Migration: `AddKiraciKategoriToTarifeKalemi` (drop old index → add column NOT NULL with default 0 → drop default → add new index → add FK). Dummy data, veri taşıma yok.
- 1.4 `SeedDataService.SeedTarifeAsync`: her aktif `KiraciKategori × BorcTipi (Davranis ≠ ManuelTetiklemeli)` için kalem üretir. Mevcut Tarife/TarifeKalemi kayıtları silinip yeniden seed edilir.

---

## 2. Admin/Tarife Ekranı

**Dosyalar:** `Controllers/AdminTarifeController.cs`, `Views/AdminTarife/Yil.cshtml` (veya mevcut adı).

- 2.1 Yıl detayı: artık tek tablo değil, **Kategori × BorcTipi matrisi** (TasinmazFiyat'taki UI ile birebir aynı pattern — referans: `Views/Tasinmaz/Detay.cshtml` "Kira Tarifeleri" Bölüm 1).
- 2.2 Satır = `KiraciKategori` (Aktif olanlar, `Sira` sıralı). Sütun = `BorcTipi` (`Davranis ≠ ManuelTetiklemeli`, `Sira` sıralı).
- 2.3 Hücre input'ları: `BirimDeger`, `HesaplamaYontemi` (Sabit/M2), `KdvOrani` — TasinmazFiyat ile aynı pattern.
- 2.4 Bulk save: tek POST'ta tüm değişen hücreler kaydedilir. Boş `BirimDeger=0` kabul edilir (kasıtlı tanımlı 0₺).
- 2.5 Permission: mevcut `Tarife.Manage` (varsa) veya `Admin` rolü — mevcut policy korunur.

---

## 3. Resolver — Kategori-Aware Tarife Fallback

**Dosya:** `Services/RateResolverService.cs`

- 3.1 `ResolveAsync(birimId, kiraciId, borcTipiId, sozlesmeId?)` precedence değişmez:
  ```
  1. SozlesmeRate            (sozlesmeId varsa)
  2. BirimRate               ((BirimId, KiraciKategoriId, BorcTipiId))
  3. TasinmazKiraciKategoriFiyat
  4. Tarife (cari yıl) → TarifeKalemi
  ```
- 3.2 Tarife adımı artık `(Yil = sozlesme.BaslangicTarihi.Year veya DateTime.Now.Year, KiraciKategoriId, BorcTipiId)` filtresi uygular.
- 3.3 Hiçbir katmanda eşleşme yoksa `null` döner (Faz 9 davranışı korunur). Composer null'u 0₺ kaleme çevirir.

---

## 4. Global Rename — "Genel Tarife"

Tek seferlik metin rename. Property adlarına dokunma (geriye uyumluluk maliyeti yok ama gereksiz risk).

**Dosyalar (view metinleri):**
- 4.1 `Views/Birim/OzelFiyat.cshtml` — rezervasyon kart başlığı `"Global Kural (Varsayılan)"` → `"Genel Tarife"`.
- 4.2 `Views/Tasinmaz/Detay.cshtml` — Bölüm 3 (Rezervasyon Ücret Kuralları) içinde "Global" rozeti → "Genel Tarife".
- 4.3 Grep ile bul: `"Global Kural"`, `"(Varsayılan)"` geçen tüm view dosyaları. Bulunan her yerde rename.

---

## 5. Parent Tarife Bilgi Kartı — Ortak Partial

**Yeni dosya:** `Views/Shared/_ParentTarifeKart.cshtml`

- 5.1 Input ViewModel: `ParentTarifeKartViewModel { string KaynakAdi (örn. "Genel Tarife - 2026"), string? Aciklama, List<ParentTarifeSatir> Satirlar }`. `ParentTarifeSatir { string KategoriAd, string BorcTipiAd, HesaplamaYontemi, decimal BirimDeger, decimal KdvOrani }`.
- 5.2 Render: rezervasyon "Global Kural" kart stilinde, üstte rozet `[Genel Tarife]` veya `[Taşınmaz Tarifesi]`, altında readonly tablo (Kategori / Borç Tipi / Yöntem / Fiyat / KDV).
- 5.3 Boş durum: "Parent katmanda tanımlı fiyat yok. Bu kalemler 0₺ olarak hesaplanacak."
- 5.4 Helper servis: `ITarifeHiyerarsiService.GetParentForAsync(level, tasinmazId?, birimId?, kategoriId?, yil)` — aşağıdaki 3 ekran bunu çağırır.
  - `level=Tasinmaz` → genel tarifeyi döner.
  - `level=Birim` → taşınmaz tarifesi varsa onu, yoksa geneli döner.
  - `level=Sozlesme` → birim varsa onu, yoksa taşınmaz, yoksa geneli döner.

---

## 6. Ekran 1 — `Tasinmaz/Ekle` ("Fiyat Parametreleri" Kartı)

**Dosyalar:** `Controllers/TasinmazController.cs` `Ekle GET`, `Views/Tasinmaz/Ekle.cshtml`.

- 6.1 Controller `Ekle()` action'ında `ITarifeHiyerarsiService.GetParentForAsync(Tasinmaz, yil: DateTime.Now.Year)` çağrılır; sonuç `vm.ParentTarife`'ye konur (yeni nullable property).
- 6.2 View'da mevcut "Fiyat Parametreleri (Varsayılan)" kartının ÜSTÜNE `_ParentTarifeKart` partial render edilir.
- 6.3 Açıklama satırı: "Aşağıdaki tarifede tanımlama yapmazsanız genel tarife uygulanır."

---

## 7. Ekran 2 — `Birim/.../OzelFiyat` (En Yakın Parent)

**Dosyalar:** `Controllers/BirimController.cs` `OzelFiyat GET`, `Views/Birim/OzelFiyat.cshtml`.

- 7.1 Controller'da `GetParentForAsync(Birim, tasinmazId: birim.TasinmazId, yil: DateTime.Now.Year)` çağrılır. `BirimOzelFiyatViewModel.ParentTarife` set edilir.
- 7.2 View'da:
  - Senaryo A (kira): mevcut matris tablosunun ÜSTÜNE parent kart.
  - Senaryo B (rezervasyon): mevcut "Genel Tarife" (eski adıyla Global Kural) kartı zaten parent görevini görüyor — yeni partial eklenmez, sadece §4 rename yapılır.
- 7.3 Kart rozet metni: parent taşınmazdan geliyorsa "Taşınmaz Tarifesi"; genelden geliyorsa "Genel Tarife - {Yıl}".

---

## 8. Ekran 3 — `Sozlesme/Detay` ("Pazarlık Fiyatları" Sekmesi)

**Dosyalar:** `Controllers/SozlesmeController.cs` `Detay`, `Views/Sozlesme/Detay.cshtml`.

- 8.1 Controller'da `GetParentForAsync(Sozlesme, tasinmazId, birimId, kategoriId: sozlesme.Kiraci.KiraciKategoriId, yil)` çağrılır.
- 8.2 "Pazarlık Fiyatları" sekmesinde override input tablosunun ÜSTÜNE `_ParentTarifeKart` partial.
- 8.3 Rozet: kaynağa göre "Birim Tarifesi" / "Taşınmaz Tarifesi" / "Genel Tarife - {Yıl}".

---

## 9. Test ve Doğrulama

- 9.1 Migration sonrası: `dotnet ef database update` hatasız geçer; seed tüm `Kategori × BorcTipi` matrisini doldurur.
- 9.2 Resolver birim testi: 4 katmanda da fiyat tanımlı → SozlesmeRate döner. Sırasıyla Sözleşme→Birim→Taşınmaz→Genel her birini boşalt; her seferinde bir sonraki katman seçilmeli. Hiçbiri yoksa `null`.
- 9.3 3 ekranda parent kart görseli doğru — rozet ve içerik beklenen kaynaktan.
- 9.4 "Global Kural (Varsayılan)" metni hiçbir view'da kalmadı (grep ile doğrula).
- 9.5 Cari yıl tarifesi olmayan senaryoda kart "Bu yıl için genel tarife tanımlanmamış." mesajı gösterir.

---

## 10. Dosya Değişiklik Özeti

| Dosya | Tip | Not |
|---|---|---|
| `Models/Tarife.cs` | Edit | `TarifeKalemi.KiraciKategoriId` |
| `Data/ApplicationDbContext.cs` | Edit | Unique index değişimi |
| `Migrations/*_AddKiraciKategoriToTarifeKalemi.cs` | New | EF migration |
| `Services/SeedDataService.cs` | Edit | `SeedTarifeAsync` kategori matrisi |
| `Services/RateResolverService.cs` | Edit | Tarife adımı kategori filtresi |
| `Services/Interfaces/ITarifeHiyerarsiService.cs` | New | `GetParentForAsync` |
| `Services/TarifeHiyerarsiService.cs` | New | implementasyon |
| `Program.cs` | Edit | DI kaydı |
| `Controllers/AdminTarifeController.cs` | Edit | Matris UI POST/GET |
| `Views/AdminTarife/Yil.cshtml` | Edit | Matris tablosu |
| `Models/ViewModels/TasinmazViewModels.cs` | Edit | `ParentTarife` property |
| `Models/ViewModels/BirimOzelFiyatViewModel.cs` | Edit | `ParentTarife` property |
| `Models/ViewModels/SozlesmeViewModels.cs` | Edit | `ParentTarife` property |
| `Models/ViewModels/ParentTarifeKartViewModel.cs` | New | Partial input |
| `Views/Shared/_ParentTarifeKart.cshtml` | New | Ortak partial |
| `Controllers/TasinmazController.cs` | Edit | `Ekle GET` parent doldur |
| `Controllers/BirimController.cs` | Edit | `OzelFiyat GET` parent doldur |
| `Controllers/SozlesmeController.cs` | Edit | `Detay` parent doldur |
| `Views/Tasinmaz/Ekle.cshtml` | Edit | Partial render |
| `Views/Birim/OzelFiyat.cshtml` | Edit | Partial + rename |
| `Views/Tasinmaz/Detay.cshtml` | Edit | "Global" rozeti rename |
| `Views/Sozlesme/Detay.cshtml` | Edit | Partial render |

**Permission:** Mevcut policy'ler yeterli.
**Breaking:** TarifeKalemi şeması değişiyor; mevcut Tarife verisi seed ile yeniden oluşturulacak.

---

## 11. Uygulama Sırası

1. §1 + §3 (model + resolver) → derleme + migration + seed.
2. §2 (Admin/Tarife matris UI) → genel tarife tanımlanabilir hale gelir.
3. §4 (global rename) → küçük, izole.
4. §5 (ortak partial + helper servis).
5. §6, §7, §8 sırasıyla — her ekranı tek tek test et.
6. §9 doğrulama.
