# SozlesmeRate Akışı Refactor

**Durum:** ✅ Tamamlandı (2026-05-15)

## Amaç

Sözleşme bazlı tarife (SozlesmeRate) düzenlemesini sadece **iki kontrollü akışta** açmak: yeni sözleşme oluşturma ve mevcut sözleşmede tahakkuk üretimi (YenidenUret / Uzat). "Sözleşme Tarifesi" sekmesi kaldırılarak yüzey azaltılır; Sozlesme/Ekle ekranı diğer kalem tanımlama ekranlarıyla aynı tam tanım (Sabit/M2) desteğine kavuşur.

---

## Kapsam

- `Sozlesme/Ekle` kalem grid'i: `HesaplamaYontemi` (Sabit/M2) + `BirimDeger` + `KdvOrani` tam tanım. Parent zincirden (BirimRate→TasinmazKiraciKategoriFiyat→Tarife) hesaplanmış değer **bilgi olarak** gösterilir; override edilirse satır SozlesmeRate olarak yazılır.
- `Sozlesme/Detay` ekranından "Sözleşme Tarifesi" sekmesi tamamen kaldırılır.
- `Sozlesme/YenidenUret` popup'ı: opsiyonel "Tarifeyi de güncelle" checkbox'ı; işaretlenirse aktif borç tipleri için kalem grid'i açılır.
- `Sozlesme/Uzat` popup'ı: aynı opsiyonel tarife güncelleme akışı.
- `PazarlikFiyatGuncelle` action'ı ve ilgili view bloğu kaldırılır.
- Her kalem satırında "↺ Parent değere dön" aksiyonu (override'ı silip resolver chain'e bırakır).
- Refactor uygulanırken mevcut tüm `SozlesmeRate` kayıtları temizlenir (dummy data; korumaya gerek yok).

---

## Kapsam Dışı

- Entity / migration değişikliği yok (`SozlesmeRate` şeması korunur).
- Resolver precedence chain değişmiyor.
- Ödenmiş tahakkuklara dokunulmuyor; mevcut Faz 12.4 davranışı korunur.
- Permission modeli değişmiyor; `Sozlesme.OverrideRate` aynı kapı.
- M2 değeri sözleşme bazında override edilmez (Birim.M2 readonly).

---

## Akış 1 — Sozlesme/Ekle (kalem grid + parent info + M2 desteği)

- Kalem grid: aktif `BorcTipi`'leri (Davranış = `AylikSabit` veya `IlkAyTekSeferlik`) listele.
- Her satır kolonları: `BorcTipi adı | ParentBilgi (yöntem, değer, KDV, kaynak rozet) | Yöntem dropdown | BirimDeger | KdvOrani | KullaniciDegistirdiMi (flag) | ↺`
- Parent bilgi seçilen `BirimId` değiştiğinde AJAX ile yeniden hesaplanır (Tasinmaz/Ekle pattern'i).
- `HesaplamaYontemi.M2` seçildiğinde `M2Adet` = `Birim.M2` readonly olarak gösterilir; Birim'de M2 yoksa M2 seçeneği disabled + tooltip.
- Submit: `KullaniciDegistirdiMi = true` olan satırlar için `SozlesmeRate` yazılır (`HesaplamaYontemi` artık zorlama yok).
- `↺ Parent değere dön`: satırın override'ını sıfırlar, grid'deki yöntem/değer parent değerle senkronlanır.

---

## Akış 2 — Sozlesme/Detay (Sözleşme Tarifesi sekmesi kaldır)

- Detay.cshtml içindeki `PazarlikFiyat*` tab başlığı ve panel içeriği kaldırılır.
- `SozlesmeController.PazarlikFiyatGuncelle` action'ı ve route'u silinir.
- `SozlesmeController.Detay` action'ında `PazarlikFiyatlari` viewmodel doldurma kodu silinir.
- Mevcut `SozlesmeRate` kayıtları **silinir** — refactor PR'ı içinde tek seferlik temizleme (`DELETE FROM SozlesmeRateler` veya `SeedDataService` reset). Yarı-ölü state bırakmamak için.

---

## Akış 3 — YenidenUret popup (opsiyonel SozlesmeRate güncellemesi)

- Popup açıldığında: `BaslangicTarihi` (mevcut) + `TarifeyiGuncelle` checkbox (varsayılan kapalı).
- Checkbox açılırsa: Ekle'deki kalem grid'i aynı şekilde render edilir (parent bilgi + override kolonları + ↺).
- Submit:
  1. `TarifeyiGuncelle = true` ise: mevcut `SozlesmeRate` kayıtlarını sözleşme için silip, override edilmiş satırları yeniden yazar (transaction içinde).
  2. `YenidenUretAsync(id, baslangicTarihi)` çağrılır (mevcut Faz 12.4 davranışı: ödemesiz tahakkuklar silinir, yenisi üretilir).
- `IslemGecmisi` kaydı: tarife güncellemesi yapıldı bilgisi log'a eklenir.

---

## Akış 4 — Uzat popup (opsiyonel SozlesmeRate güncellemesi)

- `UzatAsync` imzası genişletilir veya wrapper controller action'ı eklenir: yeni tarife satırları parametre olarak alınır (opsiyonel).
- Akış 3 ile aynı pattern: `TarifeyiGuncelle` checkbox + kalem grid.
- Submit:
  1. Uzatma uygulanır (mevcut davranış).
  2. `TarifeyiGuncelle = true` ise: SozlesmeRate kayıtları yeniden yazılır.
  3. Sonrası: uzatma tarihinden sonraki yeni tahakkuklar yeni rate'le üretilir (resolver doğal olarak yeni SozlesmeRate'i okur).

---

## Permission Notu

- Tarife güncelleme bölümü (her iki popup'ta) `Sozlesme.OverrideRate` permission'ı kontrol edilir; permission yoksa checkbox disabled + tooltip.
- `YenidenUret` ve `Uzat` aksiyonları kendi mevcut permission'larıyla çalışır (`Tahakkuk.Regenerate`, `Sozlesme.Uzat` vb.).

---

## Doğrulama

- [ ] Sozlesme/Ekle'de kalem satırı için `HesaplamaYontemi=M2` seçilebiliyor; Birim.M2 readonly gösteriliyor.
- [ ] Birim'de M2 yokken M2 seçeneği disabled + tooltip görünüyor.
- [ ] Override edilmemiş satırlar parent değerle hesaplanıyor; override edilenler SozlesmeRate olarak yazılıyor.
- [ ] `↺` butonu override'ı sıfırlıyor, parent değer görünüyor.
- [x] Sozlesme/Detay ekranında "Sözleşme Tarifesi" sekmesi YOK.
- [x] `PazarlikFiyatGuncelle` route'u 404 / action silinmiş.
- [ ] YenidenUret popup'ında checkbox kapalı iken sadece tahakkuk yeniden üretiliyor (eski davranış).
- [ ] YenidenUret popup'ında checkbox açıkken yeni rate yazılıp tahakkuklar yeni rate'le üretiliyor.
- [ ] Uzat popup'ında aynı checkbox akışı çalışıyor.
- [x] Refactor uygulandıktan sonra `SozlesmeRateler` tablosu boş (eski kayıtlar temizlendi).
- [ ] `Sozlesme.OverrideRate` izni olmayan kullanıcı için checkbox disabled.
- [x] `dotnet build` başarılı.

---

## Dokümantasyon

Uygulama tamamlandıktan sonra:

- `MASTER-PLAN.md` Ara Refactor'lar tablosuna eklenir.
- `PROGRESS.md`'ye kısa tamamlanma kaydı eklenir.
