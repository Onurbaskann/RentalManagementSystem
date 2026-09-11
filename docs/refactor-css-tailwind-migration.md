# CSS Refaktör — Inline Stillerden Tailwind Utility Migration

**Durum:** 🟡 Aktif (2026-05-15)

## Amaç

Proje genelindeki **~1.690** inline `style="..."` kullanımını **Tailwind CSS utility class'larına** dönüştürmek; `_Layout.cshtml` içindeki ~195 satırlık embedded `<style>` bloğunu harici `wwwroot/css/app.css` dosyasına çıkarmak. Bu sayede:

- CSS bakım maliyeti düşer (tek bir yerde değişiklik, tüm view'larda etki).
- Tailwind CDN zaten yüklü ancak hiç kullanılmıyor; bu yatırım geri kazanılır.
- Kod okunabilirliği artar (`style="font-size:12px;color:var(--color-muted);"` → `class="text-xs text-muted"`).

---

## Mevcut Durum

### Tailwind CDN (Zaten Mevcut)
- `_Layout.cshtml` satır 8: `<script src="https://cdn.tailwindcss.com"></script>`
- Custom config: primary renk `#1a6b5c` extend edilmiş
- Ancak view'larda Tailwind utility class'ları neredeyse hiç kullanılmamış

### CSS Değişkenleri (`:root`)
```css
--color-primary: #1a6b5c;
--color-primary-light: #f0faf7;
--sidebar-width: 240px;
--color-border: #e8ecef;
--color-text: #1a2332;
--color-muted: #6b7c93;
--color-bg: #f7f8fa;
--color-card: #ffffff;
```

### Dağılım
- **60 dosyada** toplam ~1.690 inline style
- **12 dosyada** view-spesifik `<style>` blokları
- **1 dosya** (`_Layout.cshtml`) ~195 satır embedded CSS (tasarım sistemi)

---

## Kapsam

### Dahil
- Tüm `.cshtml` dosyalarındaki inline `style="..."` kullanımları
- `_Layout.cshtml` içindeki `<style>` bloğunun harici dosyaya çıkarılması
- View-spesifik `<style>` bloklarının konsolidasyonu
- Tailwind utility class'larına geçiş (öncelik)
- Tailwind'de karşılığı olmayan stiller için `app.css` içine minimal semantik class'lar

### Kapsam Dışı
- `Views/Shared/EmailTemplates/BorcHatirlatma.cshtml` — E-posta şablonu inline CSS kullanmak zorundadır
- `_Layout.cshtml` içindeki mevcut bileşen class'ları (`.card`, `.badge`, `.btn` vb.) — bunlar `app.css`'e taşınacak ama yeniden adlandırılmayacak
- JavaScript veya Razor mantık değişiklikleri

---

## Tailwind Eşleştirme Tablosu

Aşağıdaki en yaygın inline stiller ve Tailwind karşılıkları:

| Tekrar | Inline Stil | Tailwind Class |
|:-:|---|---|
| 37 | `font-weight:600;` | `font-semibold` |
| 29 | `color:red` | `text-red-600` |
| 26 | `display:flex;align-items:center;justify-content:space-between;` | `flex items-center justify-between` |
| 25 | `font-size:12px;` | `text-xs` |
| 23 | `margin:0;` | `m-0` |
| 22 | `text-align:center;` | `text-center` |
| 19 | `font-size:13px;` | `text-[13px]` |
| 19 | `color:#dc2626;` | `text-red-600` |
| 18 | `margin-bottom:16px;` | `mb-4` |
| 15 | `margin-top:16px;` | `mt-4` |
| 10 | `font-size:14px;` | `text-sm` |
| 10 | `font-weight:500;` | `font-medium` |

### CSS Değişkeni Kullananlar → `app.css` Semantik Class

| Tekrar | Inline Stil | Custom Class |
|:-:|---|---|
| 24 | `font-size:12px;color:var(--color-muted);` | `.text-muted-sm` |
| 19 | `font-size:12px;color:var(--color-muted);margin-top:4px;` | `.hint-text` |
| 16 | `color:var(--color-muted);` | `.text-muted` |
| 11 | `font-size:11.5px;color:var(--color-muted);margin-bottom:2px;` | `.detail-label` |
| 14 | `background:#ecfdf5;border:...;color:#065f46;...` | `.alert-success` |
| 11 | `font-size:15px;font-weight:700;...border-bottom:...` | `.section-heading` |

---

## Uygulama Sırası

### Faz 0 — Altyapı Hazırlığı
- [x] 0.1 — `_Layout.cshtml` `<style>` bloğunu → `wwwroot/css/app.css` olarak çıkar
- [x] 0.2 — `<link rel="stylesheet" href="~/css/app.css">` referansı ekle (Tailwind CDN'den sonra)
- [x] 0.3 — `dotnet build` doğrulama
- [x] 0.4 — Tarayıcıda görsel regresyon kontrolü

### Faz 1 — En Yoğun 5 Dosya (Tamamlandı)
- [x] 1.1 — `Views/Sozlesme/Detay.cshtml` (211 inline, 61 KB)
- [x] 1.2 — `Views/Tasinmaz/Detay.cshtml` (163 inline, 48 KB)
- [x] 1.3 — `Views/Home/Index.cshtml` (82 inline, 19 KB)
- [x] 1.4 — `Views/Tasinmaz/Ekle.cshtml` (81 inline, 35 KB)
- [x] 1.5 — `Views/Tahakkuk/Detay.cshtml` (74 inline, 15 KB)

### Faz 2 — Orta Yoğunluklu Dosyalar (Tamamlandı)
- [x] 2.1 — `Views/Tahakkuk/Index.cshtml` (58 inline, 18 KB)
- [x] 2.2 — `Views/Birim/OzelFiyat.cshtml` (55 inline, 20 KB)
- [x] 2.3 — `Views/Kiraci/Detay.cshtml` (51 inline, 11 KB)
- [x] 2.4 — `Views/Odeme/Detay.cshtml` (48 inline, 12 KB)
- [x] 2.5 — `Views/AdminTarife/Detay.cshtml` (38 inline, 11 KB)

### Faz 3 — Kalan Dosyalar
- [x] 3.1 — `Views/Rapor/Index.cshtml` (37 inline, 9 KB)
- [x] 3.2 — `Views/Kiraci/Duzenle.cshtml` (32 inline, 15 KB)
- [x] 3.3 — `Views/Kiraci/Ekle.cshtml` (32 inline, 15 KB)
- [x] 3.4 — `Views/Rezervasyon/Ekle.cshtml` (32 inline, 11 KB)
- [x] 3.5 — `Views/AdminUser/Edit.cshtml` (31 inline, 8 KB)
- [x] 3.6 — `Views/Sozlesme/Ekle.cshtml` (29 inline, 17 KB)
- [x] 3.7 — `Views/ManuelBorc/Ekle.cshtml` (30 inline, 9 KB)
- [x] 3.9 — Kalan dosyalar (Tamamlandı: ManuelBorc, BankaHareketi, Rapor, Tahakkuk ve Admin formları)

### Faz 4 — View-Spesifik `<style>` Blokları
- [x] 4.1 — Tahakkuk/Index.cshtml (Filtre barı ve matris tablo)
- [x] 4.2 — TasinmazFiyat/Index.cshtml (Fiyat matrisi)
- [x] 4.3 — Kiraci/Ekle.cshtml & Duzenle.cshtml (Form kartları ve validasyon)
- [x] 4.4 — AdminUser/Create.cshtml & Edit.cshtml (İzin yönetimi ve formlar)
- [x] 4.5 — Odeme/Detay.cshtml (Dekontlar ve onay akışı)
- [x] 4.6 — Sozlesme/Detay.cshtml (Genel görünüm ve modal iyileştirmeleri)
- [x] 4.7 — Shared Bileşenler (_Sidebar, _ConfirmDialog, _ParentTarifeKart vb.)

## Phase 5: Stabilizasyon ve Final Kontrol [COMPLETED]
- [x] 5.1 — Birim/OzelFiyat.cshtml (Özel fiyat matrisi refactor)
- [x] 5.2 — Global x-cloak ve marker yönetimi standardizasyonu
- [x] 5.3 — site.css ve app.css uyumluluk kontrolü

---

## Doğrulama

- [ ] Grep ile kalan inline style sayısı ölçülerek ilerleme raporlanmalı
- [ ] Tarayıcıda görsel kontrol (sidebar, dashboard, sözleşme detay, tasinmaz detay)
- [ ] Admin, Yönetici ve Görüntüleyici rolleriyle giriş yaparak menü/buton görünürlük testi

---

## Kurallar

1. **Dosya dosya ilerle** — her seferinde sadece bir dosya üzerinde çalış.
2. **Tailwind class her zaman öncelikli** — eğer bir stil Tailwind'de varsa, onu kullan.
3. **Tailwind'de yoksa** → `app.css`'e semantik class ekle.
4. **Tailwind arbitrary value** syntax'ını kullan: `text-[11.5px]`, `w-[140px]`, `gap-[14px]`.
5. **Mevcut class'lara dokunma** — `.card`, `.badge`, `.btn` gibi class'lar korunur; Tailwind class'ları yanına eklenir.
6. **BorcHatirlatma.cshtml KAPSAM DIŞI.**
