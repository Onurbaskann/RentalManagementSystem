# Tasarım Dili Brief'i — Figma Make → KiraTakip

> **Durum: ONAYLANDI (2026-09-21)** — "Tahakkuklar" yoğun liste ekranı üzerinde doğrulandı.
> **Kod tarafı uygulandı (2026-09-21, Faz 1-4; commit'lenmedi):** `wwwroot/css/tokens.css` (yeni, token'ların tek kaynağı) +
> `app.css`, `Views/Shared/_BrandHead.cshtml` (yeni), iki layout, iki sidebar, iki dashboard, giriş/hata sayfaları.
> Onaydan **sapmalar** (kullanıcı geri bildirimiyle): Inter'e 700 ağırlığı eklendi (uygulamada `font-bold` yaygın);
> hero gradyanı teal'e %135'te ulaşır (sağ uçta beyaz metin okunsun); sidebar footer'ı koyu yarı saydam zemin
> (teal ucunda okunurluk); zemin deseni 64px karo + %2,5 opaklık (44px/%4 göz yordu); "iyi durum" anlamındaki
> `--color-primary` kullanımları `--color-success`'e alındı. Faz 5 (aynı gün) de tamam: e-posta
> şablonları navy'ye alındı (header gradyanı, düz navy'ye düşer); markup'taki tüm `uppercase`/`tracking-*` kaldırıldı (büyük harf yalnız `th` ve
> sidebar grup etiketi, CSS'ten); indigo vurgular navy'ye çevrildi (tarife alanları: kira tarifesi mor-mavi / rezervasyon tarifesi fuchsia,
> ve Ana Sayfa renk kodlu kutuları bilerek hariç); legacy alias'lar ve `site.css` silindi. Kalan: yalnız görsel kontrol ve commit.
>
> **Dashboard (2026-09-21):** Figma "Create Design Language" dashboard'u (Ana Sayfa + Kiracı Paneli) bizim sisteme uyarlandı: `.stat-rail` (tek çerçeve, ince ayraç KPI şeridi),
> `.section-label`, `.link-more` (metinde `→` yok, CSS chevron), `.action-tile`, hero uyarı çipi, Figma tarzı donut legend'i; renk kodlu ikon kutuları kalktı.
> **Üst çubuk (topbar: arama/zil/tarih) yapılmayacak** (kullanıcı kararı). **Daralan sidebar yapıldı** (kullanıcı sonradan istedi): masaüstünde 240↔72px, tercih `localStorage`,
> ikon tooltip'leri sağda; alttaki kullanıcı menüsü yeniden tasarlandı (kimlik başlığı, ikonlu öğeler, kırmızı "Çıkış Yap"; daralmışken sağa açılır).
>
> **Not (2026-09-21):** mevcut ~128 inline style ve 77 sabit hex'in tamamı sınıfa taşındı; e-posta hariç `style=` yalnız 8 hesaplanan `width:@yüzde%` kaldı.
>
> **Merkezi stil kuralı (kalıcı):** önce Tailwind sınıfı, yoksa özel sınıf; inline `style=` ve sabit hex yok. Renkler/fontlar tek kaynak
> `tokens.css` (`--rgb-*`, `--font-*`); Tailwind config (`_BrandHead.cshtml`) bunları okur (`bg-primary/20` gibi alfa dahil). JS'te `brandColor('navy')`,
> toast `.toast-*` sınıfları. İstisna: e-posta şablonları (inline hex zorunlu). Aşağıdaki Bölüm D `:root` örneği onay anındaki hex halidir;
> güncel yapı `tokens.css`'tedir (aynı değerler, RGB kanalı olarak).
> Onaylı tam kaynak: [`design-reference/tahakkuklar-approved.css`](design-reference/tahakkuklar-approved.css)
> (Figma Make çıktısı `Uygulama Tasarımını Uygula (6)` — `src/index.css`).

## Bölüm A — Eski durum (değişecek olan)

`wwwroot/css/app.css:1-10` token'ları: `--color-primary #1a6b5c` · `--color-primary-light #f0faf7` ·
`--color-border #e8ecef` · `--color-text #1a2332` · `--color-muted #6b7c93` · `--color-bg #f7f8fa` ·
`--color-card #fff` · `--sidebar-width 15rem`.
Fontlar: Plus Jakarta Sans + Fraunces (`.page-title`, `.card-value`, `.sidebar-title`) + JetBrains Mono.
Kart: radius 12px, `box-shadow 0 1px 3px`. Zemin düz `#f7f8fa`. Badge'ler ve buton renkleri hardcoded hex.

**Eski → yeni eşleme (uygulama sırasında):**

| Eski | Yeni |
|---|---|
| `--color-primary` `#1a6b5c` | `--color-navy` `#0d1a5e` (marka); teal artık vurgu |
| `--color-primary-light` `#f0faf7` | `--color-teal-light` `#e0f8fb` |
| `--color-border` `#e8ecef` | `--color-border` `#dde1ec` (+ `--color-border-strong`) |
| `--color-text` / `--color-muted` | `--color-text-primary` / `--color-text-muted` (+ `--color-text-secondary`) |
| `--color-bg` `#f7f8fa` | `--color-surface` `#f4f5f9`; `body` zemini `--canvas-bg` (desenli) |
| Fraunces / Plus Jakarta / JetBrains Mono | Bricolage Grotesque / Inter / **mono yok** |
| kart radius 12px, shadow | kart 8px, gölgesiz (yalnız 1px border); kontrol 5px |

## Bölüm A2 — Onaylanan marka yönü

- **Birincil marka rengi navy**, teal ikincil/vurgu. Marka gradyanı (navy → navy-mid → teal) sidebar'da **ve** `.btn-primary`'de kullanılır (butondaki kullanım bilinçli istisna; başka yerde gradyan yok).
- **Keskin radius, hiyerarşik:** kart/tablo 8px · buton/input 5px · badge pill. Tek düze değer yok.
- **Font ikilisi:** Bricolage Grotesque (başlık) + Inter (gövde **ve** sayılar). Mono font **kullanılmaz**: DM Mono ve IBM Plex Mono'nun sıfırı işaretli (glif analiziyle doğrulandı: Plex Mono varsayılan `0` = 3 kontur/nokta; Inter `0` = 2 kontur/düz, çizgili sıfır yalnız `zero` özelliğiyle). Tutarlar `font-variant-numeric: tabular-nums` + sağa hizalı; `zero` özelliği **açılmaz**.
- **6 badge varyantı değişmedi** (semantik renkler): kirali `#ecfdf5/#065f46` · bos `#f1f5f9/#475569` · surek `#fffbeb/#92400e` · gecmis `#fef2f2/#991b1b` · bireysel `#f0f9ff/#0369a1` · kurumsal `#f5f3ff/#6d28d9`.
- **Klişe kuralları:** `→` soneki yok (chevron ikon) · orta-nokta ile birleştirilmiş meta metin yok · ALL-CAPS yalnız tablo başlığı ve nav grup etiketi · gradyan yalnız sidebar + `.btn-primary`.

## Bölüm B — Figma Make Prompt (yeniden kullanım şablonu)

Yeni bir ekran tasarlatırken: aşağıdaki bloğu **.md dosyası olarak ekle**, mesaj kutusuna kısa bir talimat yaz
("Ekli dosya tam spesifikasyondur, harfiyen uygula"). Notlar:
- Figma Make **her zaman React/Vite/TSX üretir**; "single static HTML" isteği üç denemede de dikkate alınmadı, istenmez. Taşınabilirliği sağlayan şey **mevcut class isimlerinin birebir kullanılması**.
- Küçük düzeltmeleri aynı thread'de tek mesajla gönder (Bölüm C), brief'i tekrarlama.
- `<SCREEN SPEC>` yerine ekranı gerçek Türkçe etiket ve gerçekçi verilerle tarif et.

```
ROLE
You are extending an already-decided visual language for KiraTakip, an internal
back-office tool Turkish property managers use to track leases, tenants, and rent
collection. Audience: finance/ops staff who live in dense tables. Light theme only.

LOCKED SYSTEM (approved, apply exactly)
- Brand navy #0d1a5e (mid #0a4a8a, dark #070f1f), accent teal #00b8cf (tint #e0f8fb, bright #00cfe0)
- Neutrals: surface #f4f5f9, card #ffffff, border #dde1ec, border-strong #b8bfd4
- Text: primary #0d1a5e, secondary #3d4f78, muted #7a89ab
- Semantic: success #0d9f6e, warning #e8960a, danger #d93535, info #4f52c8
- Fonts: "Bricolage Grotesque" (headings), "Inter" (UI, body AND all numbers with
  font-variant-numeric: tabular-nums; never enable slashed zero). No monospace font.
- Radius: cards/table wrapper 8px, buttons/inputs/dropdowns 5px, badges pill.
- Status badges, exact colors (pill): kirali #ecfdf5/#065f46, bos #f1f5f9/#475569,
  surek #fffbeb/#92400e, gecmis #fef2f2/#991b1b, bireysel #f0f9ff/#0369a1, kurumsal #f5f3ff/#6d28d9
- Sidebar 240px fixed: linear-gradient(160deg, navy 0%, navy-mid 55%, teal 100%)
- .btn-primary: linear-gradient(100deg, navy-mid 0%, teal 100%), hover filter: brightness(1.08)
- Canvas: body background #f4f5f9 + 64x64 SVG diagonal hatch, navy stroke, opacity 0.025 (0.04 göz yorduğu için düşürüldü)
- Cards are opaque white with 1px border, no shadow; the pattern shows only between them.

AVOID
- No "→" appended to link/button text (use a chevron icon).
- No middle-dot joined facts ("A · B"); separate spatially.
- No ALL-CAPS labels except table column headers and sidebar group labels.
- No gradients anywhere except sidebar and .btn-primary.

EXISTING CSS CLASS VOCABULARY (reuse exact names; output must merge into a hand-written app.css)
Layout: .sidebar .main-content .page-header .page-title .page-subtitle
Cards: .card .card-sm .card-title .card-value .card-sub
Table: .table-wrapper, plain table/th/td, .num .num-positive .num-negative .cell-sub .cell-center
Toolbar: .table-toolbar .toolbar-search .toolbar-input .toolbar-select .filter-chips .filter-chip
Pagination: .pagination-wrap .pagination-info .pagination .page-btn .page-btn-active .page-btn-disabled .page-ellipsis
Buttons: .btn .btn-primary .btn-secondary .btn-danger .btn-sm .btn-amber .btn-ghost
Forms: .form-group .form-label .form-control .form-hint .form-error
Badges: .badge .badge-kirali .badge-bos .badge-surek .badge-gecmis .badge-bireysel .badge-kurumsal
If something has no class, invent the smallest terse-noun class (e.g. .empty-state).

SCREEN TO MOCK UP
<SCREEN SPEC>

DELIVERABLE (compact)
1. Rendered mockup
2. CSS in two parts: :root tokens + canvas rule; updated rules for the class vocabulary above
3. One example row/block of markup using those class names
4. Max 3 risk bullets; one line confirming the AVOID list was checked
```

## Bölüm C — İterasyon komutları

| İstek | Komut |
|---|---|
| Desen çok belirgin / soluk | "Reduce / slightly increase the pattern opacity, keep everything else." |
| Çizgi aralığı | "Change the hatch tile size to NNpx (same stroke/opacity), keep everything else." |
| Gradyan çok koyu başlıyor | Başlangıç durağını kaldır, orta tondan başlat; yalnızca ilgili kuralı ver, "everything else as-is". |
| Sadece kodu ver | "Skip the render, give me the updated :root CSS block and SVG." |

## Bölüm D — Onaylanan sonuç (2026-09-21)

Aşağıdakiler `app.css`'e taşınacak değerlerdir; bileşen kurallarının tamamı
[`design-reference/tahakkuklar-approved.css`](design-reference/tahakkuklar-approved.css) içinde.

```css
@import 'https://fonts.googleapis.com/css2?family=Bricolage+Grotesque:opsz,wght@12..96,400;12..96,600;12..96,700&family=Inter:wght@400;500;600&display=swap';

:root {
  /* Brand */
  --color-navy:        #0d1a5e;
  --color-navy-dark:   #070f1f;
  --color-navy-mid:    #0a4a8a;
  --color-teal:        #00b8cf;
  --color-teal-light:  #e0f8fb;
  --color-teal-bright: #00cfe0;
  /* Neutrals */
  --color-surface:      #f4f5f9;
  --color-card:         #ffffff;
  --color-border:       #dde1ec;
  --color-border-strong:#b8bfd4;
  /* Text */
  --color-text-primary:   #0d1a5e;
  --color-text-secondary: #3d4f78;
  --color-text-muted:     #7a89ab;
  /* Semantic */
  --color-success: #0d9f6e;
  --color-warning: #e8960a;
  --color-danger:  #d93535;
  --color-info:    #4f52c8;
  /* Radius hierarchy */
  --radius-card:    8px;
  --radius-control: 5px;
  --radius-badge:   9999px;
  /* Typography */
  --font-display: 'Bricolage Grotesque', sans-serif;
  --font-ui:      'Inter', sans-serif;
  --sidebar-width: 240px;
  /* Canvas pattern — diagonal hatch, 64px tile */
  --canvas-bg: #f4f5f9
    url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='64' height='64'%3E%3Cline x1='0' y1='64' x2='64' y2='0' stroke='%230d1a5e' stroke-opacity='0.025' stroke-width='1'/%3E%3C/svg%3E");
}

body { font-family: var(--font-ui); font-size: 14px; color: var(--color-text-primary); background: var(--canvas-bg); min-height: 100vh; }

.sidebar { background: linear-gradient(160deg, var(--color-navy) 0%, var(--color-navy-mid) 55%, var(--color-teal) 100%); }

.btn-primary { background: linear-gradient(100deg, var(--color-navy-mid) 0%, var(--color-teal) 100%); color: #fff; border-color: transparent; }
.btn-primary:hover { filter: brightness(1.08); }

.num { font-family: var(--font-ui); font-size: 13px; font-variant-numeric: tabular-nums; text-align: right; }
```

**Port notları (uygulama planına girdi):**
- Yeni class'lar (eski vocabulary'de yok): `.toolbar-select`, `.btn-ghost`, `.filter-chip-remove`, `.filter-chip-active`, `.empty-state(-title)`, `.sidebar-*` iç sınıfları. Karar noktası: satır aksiyonları için `.btn-ghost` mı, mevcut `.btn.btn-sm.btn-secondary` mı.
- Razor layout'larındaki (`_Layout`, `_KiraciLayout`) inline `tailwind.config` `primary` rampası hâlâ teal (`#1a6b5c`); navy'ye çevrilmeli. Views'ta `#1a6b5c` ~38 yerde hardcoded (hero, KPI ikon tile'ları, linkler).
- Login/hata sayfaları (`Layout = null`) `app.css` yüklemiyor, farklı palet ve Inter kullanıyor — tasarım dilinden bağımsız kalır, ayrıca ele alınmalı.
- Tutar sütunu `th`'sine `min-width` ekle (9+ haneli sayılar). Desen 150%+ zoom'da ince kenarlıklarla çakışabilir; canlıda kontrol et. `badge-surek` ile `badge-bos` bazı monitörlerde benzeşebilir (mevcut renkler, yeni risk değil).
- Dashboard (Home/Index, TenantPanel) hero'su ve KPI kartları bu turda tasarlanmadı; yeni palete uyarlanması gerekir ("gradyan yalnız sidebar + buton" kuralı hero için ayrıca karara bağlanmalı).
