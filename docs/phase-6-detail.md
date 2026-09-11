# Faz 6 — Tablo & UX İyileştirmeleri

> Pagination + filter + tooltip + UX. Sıfır ek bağımlılık (Alpine + mevcut stack).

---

## Karar Özeti

| Konu | Karar |
|---|---|
| Pagination | **Hibrit B+C**: Odeme/Tahakkuk/BankaHareketi → server-side (URL state); Kiraci/Sozlesme/AdminUser → Alpine client-side |
| Filter | Toolbar partial: metin (debounced 250ms), tarih aralığı, tutar min/max, çoklu durum, aktif filtre çipleri, URL state |
| Tooltip | **Alpine bileşeni** (sıfır bağımlılık). `x-tip="..."` direktifi |
| Confirm | Native `confirm()` → Alpine modal partial |
| Mobil | Sidebar hamburger + overlay |

---

## 6.1 Ortak Altyapı

- `Views/Shared/_Pagination.cshtml` — server-side; `?page=N&size=25`; prev/next + sayfa numaraları + toplam
- `Views/Shared/_TableToolbar.cshtml` — search input + slot (`@RenderSection`) + aktif filtre çipleri
- `Views/Shared/_ConfirmDialog.cshtml` — Alpine modal (`x-data="confirmDialog"`)
- `wwwroot/js/site.js` içine:
  - `Alpine.data('tip', ...)` — hover/focus tooltip (positioned div, ESC kapat)
  - `Alpine.data('clientTable', ...)` — client-side pagination + filter state
  - `Alpine.data('confirmDialog', ...)` — async confirm
- `_Layout.cshtml` → `site.js` script ref ekle

**Sözleşmeler:**
- Sayfa boyutu sabit: **25** (toolbar'da değiştirilebilir: 10/25/50/100)
- URL param adları: `page`, `size`, `q`, `from`, `to`, `min`, `max`, `durum`
- Filtre değişiminde `page=1`'e döner

---

## 6.2 Server-side Pagination + Filter

**Hedef tablolar:** Odeme, Tahakkuk, BankaHareketi

**Servis değişiklikleri (her biri için):**
```csharp
Task<PagedResult<T>> GetPagedAsync(TableQuery query);
```
- `PagedResult<T>` (Models/Common): `Items`, `Total`, `Page`, `Size`
- `TableQuery` (Models/Common): `Page`, `Size`, `Q`, `From`, `To`, `Min`, `Max`, `Durum`, `UserId`

**Controller:**
- `Index([FromQuery] TableQuery q)` → `PagedResult<T>` view'a geçer
- ViewBag yerine ViewModel kullan: `OdemeListViewModel { PagedResult, Filters }`

**Filtre alanları:**
| Tablo | Metin | Tarih | Tutar | Durum |
|---|---|---|---|---|
| Odeme | kiracı/açıklama | OdemeTarihi | Tutar | OdemeDurumu |
| Tahakkuk | kiracı | DonemBaslangic | KiraTutari | TahakkukDurumu |
| BankaHareketi | açıklama/karşı taraf | HareketTarihi | Tutar | EslesmeDurumu |

Goruntuleyici rolü filtresi mevcut UserTasinmazYetki üzerinden devam eder (servis seviyesinde).

---

## 6.3 Client-side Pagination + Filter

**Hedef tablolar:** Kiraci, Sozlesme, AdminUser

**Yapı:**
```html
<div x-data="clientTable({ pageSize: 25 })" x-init="init($refs.tbody)">
  @await Html.PartialAsync("_TableToolbar")
  <table>
    <tbody x-ref="tbody">@foreach(...) { <tr data-search="@..." data-tutar="@...">...</tr> }</tbody>
  </table>
  @await Html.PartialAsync("_Pagination", new { mode = "client" })
</div>
```

- Filtre kriterleri `data-*` attribute'ları üzerinden okunur
- Sıralama opsiyonel (gerekirse `data-sort-tarih` vb.)
- 1000+ satırda dejenerasyon: `Tahakkuk` server-side'da, küçük tablolar client-side'da → kabul edilebilir

---

## 6.4 Tooltip Uygulaması

**Bileşen:** `Alpine.data('tip', () => ({ show:false, content:'', show(e){...}, hide(){...} }))`

**Kullanım:**
```html
<span x-tip="'Tutar tam + tarih farkı ≤15 gün'">Yüksek</span>
```
Veya direktif: `x-data="tip" x-on:mouseenter="show($event,'...')" x-on:mouseleave="hide"`. En basit: bir `[data-tip]` selector'ını dinleyen global helper, `wwwroot/js/site.js` içinde init.

**Eklenecek yerler (öncelik sırası):**
1. Uyum badge'leri (HareketSec, EslestirSec) — Yüksek/Orta/Zayıf/Düşük tanımları
2. OdemeDurumu badge'leri — Onay Bekliyor anlamı
3. "Çöz" butonları — "Banka eşleşmesini kaldır"
4. Domain terimleri (Tahakkuk, Birim, Tahakkuk-Ödeme ilişkisi) — sayfa başlığında bilgi ikonu
5. Maskeli alanlar (TC/VKN/IBAN) — tam değer (Admin/Yonetici only)

---

## 6.5 Mobil Sidebar + Confirm Modal

**Sidebar:**
- `_Layout.cshtml`: `<button class="sidebar-toggle" x-show="$store.ui.mobile">☰</button>`
- `Alpine.store('ui', { sidebarOpen: false, mobile: false })`
- `@media (max-width: 900px)`: sidebar `transform: translateX(-100%)`, açıkken `translateX(0)` + overlay
- Body scroll-lock açıkken

**Confirm Modal:**
- `_ConfirmDialog.cshtml` global mount
- Kullanım: `<form data-confirm="Eşleşme kaldırılsın mı?">` — JS form submit'i intercept eder, modal açar, onay gelirse submit
- `onclick="return confirm(...)"` örüntülerinin tümü `data-confirm` ile değiştirilir

---

## 6.6 (Opsiyonel) Export + Bulk Actions + Empty State

- **CSV Export:** Toolbar'a "İndir" butonu; mevcut filtreyle servis `GetForExportAsync` döner; controller `FileResult` (CSV, UTF-8 BOM)
- **Bulk Actions:** Tabloya checkbox kolonu; toolbar'da seçim sayısı + toplu eylem butonları (Odeme: toplu onay)
- **Empty State:** `_EmptyState.cshtml` partial — ikon + başlık + açıklama + CTA butonu

Bu görev set'i Faz 6 sonrasında ayrı küçük PR'larla geçilebilir; 6.6 zorunlu değil.

---

## Görev Listesi

- [ ] 6.1 — Ortak altyapı (partial'lar + Alpine bileşenleri + site.js)
- [ ] 6.2 — Server-side pagination/filter (Odeme, Tahakkuk, BankaHareketi)
- [ ] 6.3 — Client-side pagination/filter (Kiraci, Sozlesme, AdminUser)
- [ ] 6.4 — Tooltip uygulaması (5 öncelik alanı)
- [ ] 6.5 — Mobil sidebar + confirm modal
- [ ] 6.6 — (Opsiyonel) Export + bulk actions + empty state

---

## Önkoşul

**Faz 5.9 testi (eşleştirme akışı) önce tamamlanmalı.** Faz 6 ondan sonra başlar.
