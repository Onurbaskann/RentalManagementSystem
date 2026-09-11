# İç Kullanıcı Ana Sayfa Redesign — Implementation Spec

> **AMAÇ:** `/Home/Index` sayfasını kiracı paneliyle aynı görsel dile ("Paket Sadık") çevirmek; ek olarak 5 yeni operasyonel/finansal metrik göstermek.
> Bu dosya **kontrat**tır. Spec dışına çıkma.

---

## 🚫 NEGATİF LİSTE — KESİNLİKLE YAPMA

### Kütüphane EKLEME
- ❌ Lottie / lottie-web
- ❌ AOS (Animate On Scroll)
- ❌ SweetAlert2
- ❌ GridStack
- ❌ DaisyUI / başka UI framework
- ❌ Bootstrap
- ❌ Material Design
- ❌ Chart.js / D3

### Yeni Tailwind RENGİ EKLEME
- ❌ tailwind.config'e yeni color ekleme
- İzin verilen palet: `slate`, `red`, `amber`, `emerald`, `blue`, `indigo`, `white` + `primary` (zaten `#1a6b5c`)
- Hero için: `slate-700 → slate-900` gradient

### Mevcut sınıfları DEĞİŞTİRME
- ❌ `wwwroot/css/app.css` içindeki `.card`, `.btn-*`, `.badge-*`, `.form-*`, `.sidebar*`, `.page-*`, `.stats-grid`, `.two-col`, `.day-badge*`, `.progress-*`, `.section-*` sınıflarına dokunma
- ❌ `_Layout.cshtml` ya da `_Sidebar.cshtml` yapısını bozma
- ❌ Mevcut tailwind.config palette'ini değiştirme

### Backend
- ❌ `HomeController.cs` mantığına dokunma — sadece okunacak (view'ı doldurmak için)
- ❌ `DashboardViewModel.cs` yapısına dokunma — alanları olduğu gibi kullan
- ❌ Yeni service çağrısı / DbContext sorgusu ekleme
- ❌ Yeni route / yeni controller action ekleme

### Genel
- ❌ Plan dışı yeni komponent yaratma
- ❌ Yorumlu skeleton'da yazılmayan bölüm ekleme
- ❌ Inline `<style>` blokları yazma (Tailwind utility kullan)
- ❌ Mevcut "Genel Bakış" başlığını koruma — Hero banner onun yerini alıyor

---

## ✅ İZİN VERİLENLER

### Kütüphaneler (zaten `_Layout.cshtml`'de YÜKLÜ)
- **Tailwind CSS** — utility class'ları
- **Alpine.js 3** — interaktivite (`x-data`, `@@click`, `x-show`)
- **Lucide** — SVG ikonları (`<i data-lucide="ICON"></i>`)
- **ApexCharts** — grafikler (`new ApexCharts(el, opts).render()`)
- **CountUp.js 2** — sayı sayım animasyonu

### Doldurulacak Dosya
- `KiraTakip/Views/Home/Index.cshtml` — sadece bu

---

## §1. Hedef Sayfa

- URL: `/` (Home/Index)
- Layout: `_Layout` (zaten ayarlı)
- Model: `KiraTakip.Models.ViewModels.DashboardViewModel` (zaten genişletildi)

---

## §2. Renk Paleti (İç Kullanıcı Dashboard)

| Rol | Tailwind sınıfı | Hex |
|---|---|---|
| Hero (üst banner) | `bg-gradient-to-br from-slate-700 to-slate-900` | `#334155 → #0f172a` |
| Primary (kiracı portalla aynı) | `bg-[#1a6b5c]` `text-[#1a6b5c]` | `#1a6b5c` |
| Primary soft | `bg-[#f0faf7]` `text-emerald-700` | açık teal |
| Slate | `bg-slate-500` `text-slate-500` | `#64748b` |
| Danger | `bg-red-500` `text-red-500` `bg-red-50` | `#ef4444` |
| Warning | `bg-amber-500` `text-amber-700` `bg-amber-50` | `#f59e0b` |
| Success | `bg-emerald-500` `text-emerald-700` `bg-emerald-50` | `#10b981` |
| Info / Banka | `bg-indigo-500` `text-indigo-700` `bg-indigo-50` | `#6366f1` |
| Blue | `bg-blue-500` `text-blue-700` `bg-blue-50` | `#3b82f6` |
| Background | `bg-white` `bg-slate-50` |
| Border | `border-slate-200` |
| Text | `text-slate-800` (başlık) `text-slate-600` (gövde) `text-slate-500` (muted) |

**ApexCharts JS paleti:**
```js
const PALETTE = {
    primary:   '#1a6b5c',
    secondary: '#64748b',
    danger:    '#ef4444',
    warning:   '#f59e0b',
    success:   '#10b981',
    info:      '#6366f1',
    blue:      '#3b82f6'
};
```

---

## §3. Sayfa Bölümleri

> Sıra: 3.1 Hero → 3.2 Üst KPI → 3.3 Bugün Uyarı Bandı → 3.4 Ödeme KPI → 3.5 Grafik → 3.6 Liste Paneli → 3.7 Rezervasyon İnce Satır → 3.8 Hızlı Eylemler

### §3.1 Hero Welcome Banner

```
┌────────────────────────────────────────────────────────────┐
│ Hoş geldiniz, {KullaniciAd}              ╱  ╳  rozet      │
│ {TarihEtiket}                          ┃ AB  │  {Rol}     │
│ → Operasyonel mesaj (gecikmiş/onay bekleyen/güncel)        │
└────────────────────────────────────────────────────────────┘
```

**Kurallar:**
- Wrap: `relative overflow-hidden rounded-3xl p-8 mb-6 bg-gradient-to-br from-slate-700 to-slate-900 text-white`
- Sol:
  - h1: `text-2xl font-bold` → `"Hoş geldiniz, {Model.KullaniciAd}"`
  - `text-sm text-white/80 mt-1` → `Model.TarihEtiket`
  - Motivasyon (`text-xs text-white/70 mt-2`):
    - `Model.GecikmisTahakkukAdet > 0` → `"⚠ {GecikmisTahakkukAdet} gecikmiş tahakkuk var ({GecikmisTutarToplam:N2} ₺)"`
    - else `Model.OnayBekleyenOdemeAdet > 0` → `"{OnayBekleyenOdemeAdet} ödeme onay bekliyor"`
    - else `Model.BuAyYenilenecek > 0` → `"Bu ay {BuAyYenilenecek} sözleşme yenilenmeli"`
    - else → `"Tüm akışlar güncel."`
- Sağ (`absolute right-6 top-1/2 -translate-y-1/2 flex items-center gap-3`):
  - Avatar (`w-14 h-14 rounded-full bg-white/15 text-white text-xl font-bold flex items-center justify-center`) — `Model.KullaniciAd` baş harfleri (ilk 2 kelime, .ToUpper(tr-TR))
  - Rol bloğu (`text-right`):
    - Üst (`text-xs text-white/60`): `"Yetki"`
    - Alt (rozet, `inline-block text-[10px] font-bold bg-white/20 text-white px-2 py-0.5 rounded`): `Model.KullaniciRol`
- Dekor (opsiyonel): `<svg class="absolute -right-10 -top-10 w-48 h-48 opacity-10">` daire/blob

---

### §3.2 Üst KPI Kartları (4 kart)

**Grid:** `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4 mb-6`

| # | Başlık | İkon (lucide) | Renk teması | Sayı | Decimals | Rozet | Özel |
|---|---|---|---|---|---|---|---|
| A | Toplam Taşınmaz | `building-2` | primary teal — `from-[#1a6b5c] to-[#155549]`, rozet `bg-[#f0faf7] text-[#1a6b5c] border-[#1a6b5c]/20` | `@Model.ToplamTasinmaz` | 0 | "ADET" | Alt etikette `TipiDagilim` özeti (`text-[11px] text-slate-500`) |
| B | Aktif Sözleşme | `file-text` | blue — `from-blue-500 to-blue-600` | `@Model.AktifSozlesme` | 0 | `"{BuAyYenilenecek} yenilenecek"` (varsa) | — |
| C | Aylık Kira Geliri | `trending-up` | success emerald — `from-emerald-500 to-emerald-600` | `@Model.AylikToplamGelir` (CountUp `data-suffix=" ₺"` `data-decimals="2"`) | 2 | Δ% rozeti — pozitifse yeşil (`bg-emerald-50 text-emerald-700`), negatifse kırmızı, sıfırsa slate. İçerik: `"↑ %{Δ}"`/`"↓ %{|Δ|}"`/`"="` | — |
| D | Doluluk Oranı | `pie-chart` | slate — `from-slate-500 to-slate-600` | `kiraliPct` (`data-suffix="%"` `data-decimals="0"`) | 0 | `"{KiraliBirim}/{ToplamBirim}"` | Sayı altında **küçük 3-renk segment çubuk** (kiralı/dolmak üzere/boş — mevcut sayfadaki gibi) |

**Kart şablonu (örnek A):**
```html
<div class="bg-white rounded-2xl border border-slate-200 p-5 hover:-translate-y-0.5 hover:shadow-md transition-all">
    <div class="flex items-start justify-between mb-4">
        <div class="w-11 h-11 rounded-xl bg-gradient-to-br from-[#1a6b5c] to-[#155549]
                    flex items-center justify-center text-white shadow-sm">
            <i data-lucide="building-2" class="w-5 h-5"></i>
        </div>
        <span class="text-[10px] font-bold uppercase tracking-wider
                     text-[#1a6b5c] bg-[#f0faf7] border border-[#1a6b5c]/20 px-2 py-0.5 rounded-md">ADET</span>
    </div>
    <div class="text-3xl font-bold text-slate-800" data-countup="@Model.ToplamTasinmaz" data-decimals="0">0</div>
    <div class="text-[12px] text-slate-500 mt-1">Toplam Taşınmaz</div>
    <div class="text-[11px] text-slate-400 mt-1 truncate">
        @string.Join(" · ", Model.TipiDagilim.Select(kv => $"{kv.Value} {kv.Key}"))
    </div>
</div>
```

**C kartı Δ% rozeti — Razor:**
```razor
@{
    string deltaRozetCls = Model.AylikGelirDelta > 0 ? "bg-emerald-50 text-emerald-700 border-emerald-100"
        : Model.AylikGelirDelta < 0 ? "bg-red-50 text-red-700 border-red-100"
        : "bg-slate-50 text-slate-600 border-slate-100";
    string deltaText = Model.AylikGelirDelta > 0 ? $"↑ %{Model.AylikGelirDelta:F1}"
        : Model.AylikGelirDelta < 0 ? $"↓ %{Math.Abs(Model.AylikGelirDelta):F1}"
        : "=";
}
<span class="text-[10px] font-bold uppercase tracking-wider @deltaRozetCls border px-2 py-0.5 rounded-md">@deltaText</span>
```

**D kartı segment çubuk (sayı altında):**
```html
<div class="h-1.5 mt-2 rounded-full overflow-hidden flex bg-slate-100">
    <div class="bg-emerald-500" style="width:@kiraliPct.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)%"></div>
    <div class="bg-amber-500"   style="width:@surekPct.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)%"></div>
    <div class="bg-slate-300"   style="width:@bosPct.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)%"></div>
</div>
```
> `kiraliPct`, `surekPct`, `bosPct` hesabı: mevcut view'da kullanılan formül aynen taşınır (toplamBirim'e böl).

---

### §3.3 Bugün Vade Dolan — Uyarı Bandı (şartlı)

`Model.HasOdemeAccess && Model.BugunVadeDolanAdet > 0` ise göster, yoksa render etme.

```html
<div class="mb-6 flex items-center justify-between p-4 rounded-2xl border border-amber-200 bg-amber-50">
    <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-amber-500 flex items-center justify-center text-white shadow-sm">
            <i data-lucide="alarm-clock" class="w-5 h-5"></i>
        </div>
        <div>
            <div class="text-sm font-bold text-amber-900">Bugün vade dolan @Model.BugunVadeDolanAdet tahakkuk</div>
            <div class="text-xs text-amber-700 mt-0.5">Toplam kalan: <strong>@Model.BugunVadeDolanTutar.ToString("N2") ₺</strong> — Bugün takip et</div>
        </div>
    </div>
    <a href="/Tahakkuk?vade=bugun" class="text-xs font-semibold text-amber-800 hover:underline shrink-0">İncele →</a>
</div>
```

---

### §3.4 Ödeme KPI Kartları (4 kart) — Şartlı

`Model.HasOdemeAccess` true ise göster. Üstte küçük başlık.

```html
<div class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3">Ödeme Durumu</div>
<div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4 mb-6">
    <!-- 4 kart -->
</div>
```

| # | Başlık | İkon | Renk | Sayı (CountUp) | Rozet / Alt |
|---|---|---|---|---|---|
| A | Tahsilat Oranı (30 gün) | `gauge` | success emerald | `@Model.TahsilatOrani30Gun` (`data-suffix="%"` `data-decimals="1"`) | Alt: `"Beklenenin {TahsilatOrani30Gun:F0}%'i tahsil edildi"`; sayı altında küçük yatay progress (emerald-500 fill, slate-100 bg) |
| B | Gecikmiş Tahakkuk | `alert-triangle` | red — `from-red-500 to-red-600` | `@Model.GecikmisTahakkukAdet` (0) | Rozet: `{GecikmisTutarToplam:N2} ₺` (red); kart wrap `border-red-200 bg-red-50/40` ve ikon yanında `animate-pulse` dot (sayım > 0 ise) |
| C | Onay Bekleyen Ödeme | `clock` | amber — `from-amber-500 to-amber-600` | `@Model.OnayBekleyenOdemeAdet` (0) | Rozet yok; alt etikette `"İncele →"` link (`/Odeme?durum=onaybekliyor`); sayım > 0 ise kart `border-amber-200 bg-amber-50/40` |
| D | Eşleşmemiş Banka Hareketi | `link` | indigo — `from-indigo-500 to-indigo-600` | `@Model.EslesmemisHareketAdet` (0) | Rozet yok; alt etikette `"Eşleştir →"` link (`/BankaHareketi?durum=eslestirilmedi`); sayım > 0 ise kart `border-indigo-200 bg-indigo-50/40` |

**A kartı — özel sayı altı progress:**
```html
<div class="h-1.5 mt-2 rounded-full overflow-hidden bg-slate-100">
    <div class="h-full bg-emerald-500"
         style="width:@Math.Min((double)Model.TahsilatOrani30Gun, 100d).ToString("F0", System.Globalization.CultureInfo.InvariantCulture)%"></div>
</div>
```

**B kartı özel (gecikmiş):** `Model.GecikmisTahakkukAdet > 0` ise:
- Kart wrap `border-red-200 bg-red-50/40` (border-slate-200 yerine)
- İkon container'ına `relative` ekle; sağ üst köşeye `<span class="absolute -top-0.5 -right-0.5 w-2.5 h-2.5 rounded-full bg-red-500 animate-pulse"></span>`

---

### §3.5 Grafik Paneli

**Grid:** `grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6`

#### §3.5.A Sol — Son 6 Ay Nakit Akışı (col-span-2)

```html
<div class="lg:col-span-2 bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div>
            <div class="text-sm font-bold text-slate-800">Son 6 Ay Nakit Akışı</div>
            <div class="text-xs text-slate-500 mt-0.5">Aylık beklenen tahsilat ve gerçekleşen ödenen tutar</div>
        </div>
        <div class="flex items-center gap-3 text-[11px] text-slate-600">
            <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-sm bg-slate-400 inline-block"></span>Beklenen</span>
            <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-sm bg-emerald-500 inline-block"></span>Ödenen</span>
        </div>
    </div>
    <div id="chart-nakit"></div>
</div>
```

#### §3.5.B Sağ — Doluluk Donut

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="text-sm font-bold text-slate-800 mb-1">Doluluk Dağılımı</div>
    <div class="text-xs text-slate-500 mb-4">Birim bazında (Kiralı / Dolmak Üzere / Boş)</div>
    @if (Model.ToplamBirim > 0)
    {
        <div id="chart-doluluk"></div>
    }
    else
    {
        <div class="text-center py-10 text-slate-400 text-sm">Henüz birim yok</div>
    }
</div>
```

---

### §3.6 Liste Paneli (3 kart)

**Grid:** `grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6`

#### §3.6.A — Süresi Dolmak Üzere

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div class="text-sm font-bold text-slate-800">Süresi Dolmak Üzere</div>
        <a href="/Sozlesme?filtre=surek" class="text-xs font-semibold text-[#1a6b5c] hover:underline">Tümü →</a>
    </div>
    @if (!Model.SuresiDolmakUzere.Any())
    {
        <div class="text-center py-8 text-slate-400 text-sm">Yakında dolacak sözleşme yok</div>
    }
    else
    {
        <div class="space-y-2">
            @foreach (var s in Model.SuresiDolmakUzere)
            {
                <!-- SATIR — border-l-4 (renk: KalanGun <=10 red, <=30 amber, else emerald) -->
            }
        </div>
    }
</div>
```

**Satır şablonu:**
- Wrap: `flex items-center justify-between p-3 rounded-lg bg-slate-50/50 border-l-4 @borderCls hover:translate-x-0.5 transition-all`
- Razor:
  ```razor
  var borderCls = s.KalanGun <= 10 ? "border-red-500" : s.KalanGun <= 30 ? "border-amber-500" : "border-emerald-500";
  var rozetCls  = s.KalanGun <= 10 ? "bg-red-100 text-red-700" : s.KalanGun <= 30 ? "bg-amber-100 text-amber-700" : "bg-emerald-100 text-emerald-700";
  ```
- Sol (`flex-1 min-w-0`):
  - `text-sm font-semibold text-slate-800 truncate` → `s.KiraciAdi`
  - `text-xs text-slate-500 truncate mt-0.5` → `"{s.TasinmazAdi} · {s.BirimAdi}"`
- Sağ:
  - Rozet (`px-2 py-1 rounded text-[11px] font-bold @rozetCls`) → `"{s.KalanGun} gün"`
  - Alt: `<a href="/Sozlesme/Detay/@s.SozlesmeId" class="text-[11px] font-semibold text-[#1a6b5c] hover:underline mt-0.5 block">Detay →</a>`

#### §3.6.B — Boş Birimler

Yapı 3.6.A ile aynı; başlık "Boş Birimler", Tümü link `"/Sozlesme/Ekle"` (Kiraya Ver).

- Satır:
  - Wrap: `flex items-center justify-between p-3 rounded-lg bg-slate-50/50 border-l-4 border-slate-300 hover:translate-x-0.5 transition-all`
  - Sol: `text-sm font-semibold text-slate-800 truncate` → `b.TasinmazAdi`; alt `text-xs text-slate-500 truncate mt-0.5` → `"{b.BirimAdi} · {b.Ilce}"`
  - Sağ: rozet `bg-slate-100 text-slate-700 px-2 py-1 rounded text-[11px] font-bold` → `"{b.Yuzolcumu:F0} m²"`; alt link `<a href="/Sozlesme/Ekle?birimId=@b.BirimId">Kiraya Ver →</a>`
- Empty state: "Tüm birimler kiralı"

#### §3.6.C — Top 5 Gelir Getiren Taşınmaz (YENİ)

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div>
            <div class="text-sm font-bold text-slate-800">Gelir Şampiyonları</div>
            <div class="text-[11px] text-slate-500 mt-0.5">Son 12 ay tahsilat</div>
        </div>
        <a href="/Tasinmaz" class="text-xs font-semibold text-[#1a6b5c] hover:underline">Tümü →</a>
    </div>
    @if (!Model.TopGelirTasinmaz.Any())
    {
        <div class="text-center py-8 text-slate-400 text-sm">Henüz tahsilat verisi yok</div>
    }
    else
    {
        var maxGelir = Model.TopGelirTasinmaz.Max(x => x.ToplamTahsilat);
        <div class="space-y-2.5">
            @{ int rank = 0; }
            @foreach (var g in Model.TopGelirTasinmaz)
            {
                rank++;
                var pct = maxGelir > 0 ? (double)(g.ToplamTahsilat / maxGelir) * 100 : 0;
                <!-- SATIR — sıra rozetı + ilerleme çubuğu -->
            }
        </div>
    }
</div>
```

**Satır şablonu:**
```html
<a href="/Tasinmaz/Detay/@g.TasinmazId" class="block p-2 rounded-lg hover:bg-slate-50 transition-colors no-underline">
    <div class="flex items-center justify-between mb-1.5">
        <div class="flex items-center gap-2 min-w-0">
            <span class="w-5 h-5 rounded-md bg-[#f0faf7] text-[#1a6b5c] text-[10px] font-bold flex items-center justify-center shrink-0">@rank</span>
            <span class="text-[13px] font-semibold text-slate-800 truncate">@g.TasinmazAd</span>
        </div>
        <span class="text-[12px] font-bold text-slate-800 shrink-0">@g.ToplamTahsilat.ToString("N0") ₺</span>
    </div>
    <div class="h-1 rounded-full overflow-hidden bg-slate-100">
        <div class="h-full bg-gradient-to-r from-[#1a6b5c] to-emerald-500"
             style="width:@pct.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)%"></div>
    </div>
    <div class="text-[10px] text-slate-500 mt-1">@g.BirimSayisi birim</div>
</a>
```

---

### §3.7 Rezervasyon & Manuel Borç İnce Satır (şartlı)

Tetik: `Model.HasOdemeAccess && (Model.BuAyManuelBorcToplami > 0 || Model.BuAyRezervasyonGeliri > 0 || Model.TahakkukaAktarilmamisRezervasyonAdet > 0)`

```html
<div class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3">Rezervasyon & Manuel Borç</div>
<div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
    <!-- 3 kart (mevcut layout aynen, sadece KPI kartı şablonuyla uyumlu hale getir) -->
</div>
```

3 kart aynı yapıda — ikon kutusu daha küçük (`w-9 h-9` yeterli), kompakt:
- Bu Ay Manuel Borç (ikon `pencil-line`, slate)
- Bu Ay Rezervasyon Geliri (ikon `calendar-check`, blue)
- Aktarılmamış Ücretli Rezervasyon (ikon `arrow-right-circle`, amber — sayım > 0 ise `border-amber-200 bg-amber-50/40`)

Her kartta `text-3xl font-bold` yerine `text-2xl font-bold` (zaten ikincil).

---

### §3.8 Hızlı Eylemler (4 kart)

**Grid:** `grid grid-cols-2 md:grid-cols-4 gap-3 mb-2`

| # | Başlık | Açıklama | Hedef | İkon | Gradient bg | İkon kutusu gradient |
|---|---|---|---|---|---|---|
| 1 | Yeni Sözleşme | Kiraya verme akışı | `/Sozlesme/Ekle` | `file-plus` | `from-emerald-50 to-emerald-100/60` | `from-emerald-500 to-emerald-600` |
| 2 | Yeni Rezervasyon | Salon/birim rezervasyonu | `/Rezervasyon/Ekle` | `calendar-plus` | `from-blue-50 to-blue-100/60` | `from-blue-500 to-blue-600` |
| 3 | Manuel Borç | Tek seferlik borç gir | `/ManuelBorc/Ekle` | `pencil-line` | `from-amber-50 to-amber-100/60` | `from-amber-500 to-amber-600` |
| 4 | Banka İmport | CSV/Excel yükle | `/BankaHareketi` | `upload-cloud` | `from-indigo-50 to-indigo-100/60` | `from-indigo-500 to-indigo-600` |

**Şablon:**
```html
<a href="HEDEF"
   class="block p-5 rounded-2xl bg-gradient-to-br {gradient-bg} border border-white/60
          hover:scale-[1.02] hover:shadow-md transition-all no-underline">
    <div class="w-12 h-12 rounded-xl bg-gradient-to-br {ikon-gradient}
                flex items-center justify-center text-white shadow-sm">
        <i data-lucide="ICON" class="w-6 h-6"></i>
    </div>
    <div class="text-sm font-bold text-slate-800 mt-3">Başlık</div>
    <div class="text-xs text-slate-600 mt-0.5">Açıklama</div>
</a>
```

> **Permission kontrolü:** `User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Sozlesme.Create)` — yetkisi yoksa "Yeni Sözleşme" gizle. Aynı şekilde diğer 3 link için `Rezervasyon.Create`, `Tahakkuk.Create`, `BankaHareketi.View` kontrolleri eklenebilir (catalog tam path'ler için `PermissionCatalog`'a bak). Görünmeyenleri grid'den çıkar.

---

## §4. ApexCharts Konfigürasyonları

### §4.A Aylık Nakit Akışı (Bar — `#chart-nakit`)

```js
const beklenenSeri = [@Html.Raw(string.Join(",", Model.AylikNakit.Select(x => x.Beklenen.ToString(System.Globalization.CultureInfo.InvariantCulture))))];
const odenenSeri   = [@Html.Raw(string.Join(",", Model.AylikNakit.Select(x => x.Odenen.ToString(System.Globalization.CultureInfo.InvariantCulture))))];
const aylar        = [@Html.Raw(string.Join(",", Model.AylikNakit.Select(x => $"'{x.AyEtiket}'")))];

new ApexCharts(document.querySelector('#chart-nakit'), {
    chart: { type: 'bar', height: 260, toolbar: { show: false }, animations: { enabled: true, easing: 'easeinout', speed: 600 } },
    series: [
        { name: 'Beklenen', data: beklenenSeri },
        { name: 'Ödenen',   data: odenenSeri }
    ],
    colors: ['#94a3b8', '#10b981'],
    plotOptions: { bar: { columnWidth: '55%', borderRadius: 4 } },
    dataLabels: { enabled: false },
    legend: { show: false },
    grid: { borderColor: '#e2e8f0', strokeDashArray: 4, yaxis: { lines: { show: true } }, xaxis: { lines: { show: false } } },
    xaxis: { categories: aylar, labels: { style: { fontSize: '11px', colors: '#64748b' } }, axisBorder: { show: false }, axisTicks: { show: false } },
    yaxis: { labels: { style: { fontSize: '11px', colors: '#94a3b8' }, formatter: v => v.toLocaleString('tr-TR') + ' ₺' } },
    tooltip: { y: { formatter: v => v.toLocaleString('tr-TR', {minimumFractionDigits: 2, maximumFractionDigits: 2}) + ' ₺' } }
}).render();
```

### §4.B Doluluk Donut (`#chart-doluluk`)

```js
const dolulukData   = [@Model.KiraliBirim, @Model.SuresiDolmakUzereBirim, @Model.BosBirim];
const dolulukLabels = ['Kiralı', 'Dolmak Üzere', 'Boş'];

if (dolulukData.reduce((a, b) => a + b, 0) > 0) {
    new ApexCharts(document.querySelector('#chart-doluluk'), {
        chart: { type: 'donut', height: 240, animations: { enabled: true, speed: 600 } },
        series: dolulukData,
        labels: dolulukLabels,
        colors: ['#10b981', '#f59e0b', '#cbd5e1'],
        plotOptions: { pie: { donut: { size: '68%', labels: { show: true, total: { show: true, label: 'Birim', formatter: w => w.globals.seriesTotals.reduce((a,b)=>a+b,0) } } } } },
        dataLabels: { enabled: false },
        legend: { position: 'bottom', fontSize: '11px', markers: { width: 8, height: 8 } },
        stroke: { width: 2, colors: ['#fff'] },
        tooltip: { y: { formatter: v => v + ' birim' } }
    }).render();
}
```

### §4.C CountUp init

```js
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-countup]').forEach(function (el) {
        var target = parseFloat(el.getAttribute('data-countup')) || 0;
        var decimals = parseInt(el.getAttribute('data-decimals') || '0', 10);
        var prefix = el.getAttribute('data-prefix') || '';
        var suffix = el.getAttribute('data-suffix') || '';
        var c = new countUp.CountUp(el, target, {
            duration: 1.2,
            separator: '.',
            decimal: ',',
            decimalPlaces: decimals,
            prefix: prefix,
            suffix: suffix
        });
        if (!c.error) c.start();
    });
});
```

---

## §5. Acceptance Checklist

- [ ] Build temiz: `dotnet build KiraTakip/KiraTakip.csproj` (CS/RZ hatası yok; PDB kilidi normal)
- [ ] Sayfa açılınca **JS console hatası yok**
- [ ] Hero: slate gradient, sağda avatar + rol rozeti, motivasyon mesajı şartlara göre değişiyor (gecikmiş > onay bekleyen > yenilenecek > güncel)
- [ ] Üst 4 KPI: her birinin **kendi renk teması** doğru, ikon kutusu **gradient**, sayı **CountUp animasyonu**, C kartında **Δ% rozeti** (yeşil/kırmızı/slate), D kartında **segment çubuk**
- [ ] Bugün vade dolan alert bandı: `BugunVadeDolanAdet > 0` ise görünür, değilse hiç render etmez
- [ ] Ödeme KPI: 4 kart, A'da progress çubuk; B kart `GecikmisTahakkukAdet > 0` ise kırmızı border + pulse dot; C/D linkler çalışıyor
- [ ] Nakit akışı bar chart: 6 ay, 2 seri (slate-400 + emerald-500), grid sade, dataLabels yok, toolbar yok
- [ ] Doluluk donut: total ortada (Birim adedi), 3 dilim doğru palet
- [ ] 3 liste kartı: Süresi Dolmak Üzere (border-l-4 renk), Boş Birimler, Top 5 Gelir Şampiyonları (rank rozeti + ilerleme çubuğu)
- [ ] Rezervasyon ince satır: şartlı görünüyor (en az 1 metrik > 0)
- [ ] Hızlı eylemler 4 kart, gradient bg, hover scale, route'lar doğru
- [ ] Sayfa **mobile responsive**: KPI grid 1 sütuna, grafik paneli 1 sütuna düşüyor
- [ ] Lucide ikonları render oluyor (sayfa sonunda `lucide.createIcons()` zaten var)
- [ ] Yeni kütüphane EKLENMEMİŞ (sadece ApexCharts + CountUp, ikisi de _Layout'da yüklü)
- [ ] Yeni Tailwind rengi EKLENMEMİŞ
- [ ] Mevcut `.card`, `.btn-*`, `.badge-*`, `.stats-grid`, `.two-col` sınıflarına dokunulmamış (yeni kartlar Tailwind utility ile)

---

## §6. Tailwind Safelist Uyarısı

Tailwind CDN runtime kullanılıyor — tüm class'lar otomatik tanınır. **Dinamik string interpolation** kullanma; Razor `?:` ternary ile **tam class adı** yaz (skeleton'daki örnekler gibi).

---

## §7. Test Senaryosu

1. **Veri sıfır** → tüm kartlar 0, bar chart 6 ay boş, donut "Henüz birim yok", listeler empty state
2. **Gecikmiş + Bugün vade dolan + Onay bekleyen olan kullanıcı** → Hero kırmızı uyarı, alert bandı görünür, B/C/D kartlarda renkli border
3. **Mobile (375px)** → tek sütun, taşma yok, grafik küçülüyor
4. **Refresh** → CountUp tekrar 0'dan başlıyor

---

## §8. Bitince

- Acceptance checklist'i kontrol et
- Build temiz mi: `dotnet build KiraTakip/KiraTakip.csproj`
- Tarayıcıda `/` aç, görsel test yap
- Sapma yoksa: "Tamamlandı, kontrole hazır" diye bildir
