# Kiracı Panel Redesign — Implementation Spec

> **AMAÇ:** `/Kiraci/Panel` sayfasını "Paket Sadık" tasarımıyla yeniden yap.
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

### Yeni Tailwind RENGİ EKLEME
- ❌ tailwind.config'e yeni color ekleme
- Sadece şu renkler kullanılabilir: `slate`, `red`, `amber`, `emerald`, `blue`, `white` + kiracı `primary` (zaten tanımlı, `#1a6b5c`)

### Mevcut sınıfları DEĞİŞTİRME
- ❌ `wwwroot/css/app.css` içindeki `.card`, `.btn-*`, `.badge-*`, `.form-*`, `.sidebar*`, `.page-*` sınıflarına dokunma
- ❌ `_KiraciLayout.cshtml` veya `_KiraciSidebar.cshtml` yapısını bozma
- ❌ Mevcut tailwind.config palette'ini değiştirme

### Backend
- ❌ `KiraciPanelController.cs` mantığına dokunma — sadece okunacak (View'ı doldurmak için)
- ❌ `KiraciPanelViewModel.cs` yapısına dokunma — alanları olduğu gibi kullan
- ❌ Yeni DbContext sorgusu ekleme — model üzerinden gelen verilerle yetin
- ❌ Yeni route / yeni controller action ekleme

### Genel
- ❌ Plan dışı yeni komponent yaratma
- ❌ Yorumlu skeleton'da yazılmayan bölüm ekleme
- ❌ Inline `<style>` blokları yazma (Tailwind utility kullan)

---

## ✅ İZİN VERİLENLER

### Kütüphaneler (zaten `_KiraciLayout.cshtml`'de YÜKLÜ)
- **Tailwind CSS** — utility class'ları
- **Alpine.js 3** — interaktivite (`x-data`, `@@click`, `x-show`)
- **Lucide** — SVG ikonları (`<i data-lucide="ICON"></i>` ya da inline SVG)
- **ApexCharts** — grafikler (`new ApexCharts(el, opts).render()`)
- **CountUp.js 2** — sayı sayım animasyonu (`new countUp.CountUp(el, target, opts).start()`)

### Doldurulacak Dosya
- `KiraTakip/Views/KiraciPanel/Index.cshtml` — sadece bu

---

## §1. Hedef Sayfa

- URL: `/Kiraci/Panel`
- Layout: `_KiraciLayout` (zaten ayarlı)
- Model: `KiraTakip.Models.ViewModels.KiraciPanelViewModel` (zaten ayarlı)

---

## §2. Renk Paleti (Kiracı Portal)

Sadece bu paletten kullan:

| Rol | Tailwind sınıfı | Hex |
|---|---|---|
| Primary (kiracı) | `bg-[#1a6b5c]` `text-[#1a6b5c]` | `#1a6b5c` (teal) |
| Primary dark | `bg-[#155549]` | `#155549` |
| Primary soft | `bg-[#f0faf7]` `text-emerald-700` | açık teal |
| Secondary | `bg-slate-500` `text-slate-500` | `#64748b` |
| Danger | `bg-red-500` `text-red-500` `bg-red-50` `border-red-200` | `#ef4444` |
| Warning | `bg-amber-500` `text-amber-700` `bg-amber-50` `border-amber-200` | `#f59e0b` |
| Success | `bg-emerald-500` `text-emerald-700` `bg-emerald-50` `border-emerald-200` | `#10b981` |
| Info | `bg-blue-500` `text-blue-700` `bg-blue-50` | `#3b82f6` |
| Background | `bg-white` `bg-slate-50` | beyaz / açık gri |
| Border | `border-slate-200` | açık gri |
| Text | `text-slate-800` (başlık) `text-slate-600` (gövde) `text-slate-500` (muted) | |

**ApexCharts JS paleti (sabitler):**
```js
const PALETTE = {
    primary:   '#1a6b5c',
    secondary: '#64748b',
    danger:    '#ef4444',
    warning:   '#f59e0b',
    success:   '#10b981',
    info:      '#3b82f6'
};
```

---

## §3. Sayfa Bölümleri

### §3.1 Hero Welcome Banner

```
┌─────────────────────────────────────────────────────────────┐
│ Hoş geldiniz, {KullaniciAd}                  ╱  ╳  rozet  │
│ {TarihEtiket}                              ┃ AB  │  Rol   │
│ → motivasyon mesajı                        ╲    ╲         │
└─────────────────────────────────────────────────────────────┘
```

**Kurallar:**
- Wrap: `relative overflow-hidden rounded-3xl p-8 mb-6 bg-gradient-to-br from-[#1a6b5c] to-[#155549] text-white`
- Sol içerik:
  - h1: `text-2xl font-bold` — `"Hoş geldiniz, {Model.KullaniciAd}"`
  - Alt satır: `text-sm text-white/80 mt-1` — `Model.TarihEtiket`
  - Motivasyon (`text-xs text-white/70 mt-2`):
    - `Model.GecikmisAdet > 0` → `"⚠ {GecikmisAdet} gecikmiş borcunuz var ({GecikmisTutar:N2} ₺)"`
    - else `Model.YaklasanOdemeAdet > 0` → `"7 gün içinde {YaklasanOdemeAdet} vade yaklaşıyor"`
    - else → `"Bugün tüm ödemeleriniz güncel."`
- Sağ içerik (`absolute right-6 top-1/2 -translate-y-1/2 flex items-center gap-3`):
  - Avatar (`w-14 h-14 rounded-full bg-white/15 text-white text-xl font-bold flex items-center justify-center`)
    - İçerik: `Model.KiraciAd`'tan baş harfler (ilk 2 kelime → ilk harfleri, .ToUpper())
  - Sağında küçük metin block: `text-right`
    - Üst: `text-xs text-white/60` → `Model.KiraciAd`
    - Alt: rozet — `inline-block text-[10px] font-bold bg-white/20 text-white px-2 py-0.5 rounded` → `Model.KullaniciRol`
- Dekor (opsiyonel, hafif): `<svg class="absolute -right-10 -top-10 w-48 h-48 opacity-10" ...>` — daire/blob

---

### §3.2 KPI Kartları (4 kart)

**Grid wrap:** `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4 mb-6`

**Tek kart template:**
```html
<div class="bg-white rounded-2xl border border-slate-200 p-5
            hover:-translate-y-0.5 hover:shadow-md transition-all">
    <div class="flex items-start justify-between mb-4">
        <!-- İKON KUTUSU -->
        <div class="w-11 h-11 rounded-xl bg-gradient-to-br from-{X}-500 to-{X}-600
                    flex items-center justify-center text-white shadow-sm">
            <i data-lucide="ICON" class="w-5 h-5"></i>
        </div>
        <!-- ROZET -->
        <span class="text-[10px] font-bold uppercase tracking-wider
                     text-{X}-700 bg-{X}-50 border border-{X}-100 px-2 py-0.5 rounded-md">
            ROZET
        </span>
    </div>
    <div class="text-3xl font-bold text-slate-800" data-countup="42" data-decimals="0">0</div>
    <div class="text-[12px] text-slate-500 mt-1">Etiket</div>
</div>
```

**4 KART:**

| # | Başlık | İkon (lucide) | Renk (X) | Sayı | Decimals | Rozet |
|---|---|---|---|---|---|---|
| A | Aktif Sözleşme | `file-text` | yok — kullan **primary** (`bg-[#1a6b5c]` to `bg-[#155549]`) ve rozet `bg-[#f0faf7] text-[#1a6b5c] border-[#1a6b5c]/20` | `@Model.AktifSozlesmeAdedi` | 0 | "ADET" |
| B | Toplam Açık Borç | `wallet` | slate | `@Model.ToplamAcikBorc` | 2 | "TOPLAM ₺" (suffix " ₺" ile) — **altında sparkline div** (`<div id="sparkline-borc" class="h-10 mt-3 -mx-2"></div>`) |
| C | Yaklaşan (7 gün) | `clock` | amber | `@Model.YaklasanOdemeAdet` | 0 | `{Model.YaklasanOdemeTutar:N2} ₺` |
| D | Gecikmiş | `alert-triangle` | red | `@Model.GecikmisAdet` | 0 | `{Model.GecikmisTutar:N2} ₺` |

**D kartı özel:** `Model.GecikmisAdet > 0` ise:
- Kart wrap: `border-red-200 bg-red-50/40` (border-slate-200 yerine)
- İkon kutusunun yanına `<span class="absolute top-3 left-12 w-2 h-2 rounded-full bg-red-500 animate-pulse"></span>` (üst container'a `relative` ekle)

**CountUp suffix/prefix:**
- B kartı: `data-suffix=" ₺"` `data-decimals="2"`
- A/C/D: decimals=0, suffix yok

---

### §3.3 Grafik Paneli

**Grid wrap:** `grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6`

#### §3.3.A Sol Kart — Aylık Nakit Akışı (col-span-2)

```html
<div class="lg:col-span-2 bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div>
            <div class="text-sm font-bold text-slate-800">Son 6 Ay Nakit Akışı</div>
            <div class="text-xs text-slate-500 mt-0.5">Aylık beklenen ve onaylı ödeme tutarları</div>
        </div>
        <div class="flex items-center gap-3 text-[11px]">
            <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-sm bg-slate-400"></span>Beklenen</span>
            <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-sm bg-emerald-500"></span>Ödenen</span>
        </div>
    </div>
    <div id="chart-nakit"></div>
</div>
```

#### §3.3.B Sağ Kart — Borç Tipi Dağılımı

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="text-sm font-bold text-slate-800 mb-1">Borç Tipi Dağılımı</div>
    <div class="text-xs text-slate-500 mb-4">Açık tahakkukların kalem dağılımı</div>
    @if (Model.BorcTipiDagilimi.Any())
    {
        <div id="chart-donut"></div>
    }
    else
    {
        <div class="text-center py-10 text-slate-400 text-sm">Açık borç yok</div>
    }
</div>
```

---

### §3.4 Yaklaşan Tahakkuklar + Son Ödemeler

**Grid wrap:** `grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6`

#### §3.4.A Sol — Yaklaşan Tahakkuklar

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div class="text-sm font-bold text-slate-800">Yaklaşan Tahakkuklar</div>
        <a href="/Kiraci/Tahakkuklarim"
           class="text-xs font-semibold text-[#1a6b5c] hover:underline">Tümünü Gör →</a>
    </div>
    @if (!Model.YaklasanTahakkuklar.Any())
    {
        <div class="text-center py-8 text-slate-400 text-sm">Yaklaşan tahakkuk yok</div>
    }
    else
    {
        <div class="space-y-2">
            @foreach (var t in Model.YaklasanTahakkuklar)
            {
                <!-- SATIR — border-l-4 border-{t.BorderRenk}-500 -->
                <!-- hover:translate-x-0.5 transition -->
            }
        </div>
    }
</div>
```

**Satır template:**
- Wrap: `flex items-center justify-between p-3 rounded-lg bg-slate-50/50 border-l-4 border-{renk}-500 hover:translate-x-0.5 transition-all`
  - `{renk}` = `Model.YaklasanTahakkuklar[i].BorderRenk` (string: "red" / "amber" / "emerald")
  - **Önemli:** Razor C# tarafında ternary ile sınıf adı belirle. Tailwind dinamik string'i derleyemediği için **safelist**'te olması ya da **conditional tam sınıf** yazılması gerek:
    ```razor
    string borderCls = t.BorderRenk switch {
        "red" => "border-red-500",
        "amber" => "border-amber-500",
        _ => "border-emerald-500"
    };
    ```
- Sol içerik (`flex-1 min-w-0`):
  - `text-sm font-semibold text-slate-800 truncate` → `t.Donem`
  - `text-xs text-slate-500 truncate mt-0.5` → `t.BirimAd`
- Orta — vade rozeti:
  - Konum: `mx-3 shrink-0`
  - Sınıf (renge göre conditional):
    - red:    `bg-red-100 text-red-700`
    - amber:  `bg-amber-100 text-amber-700`
    - emerald: `bg-emerald-100 text-emerald-700`
  - Şablon: `px-2 py-1 rounded text-[11px] font-bold`
  - İçerik:
    - `t.GunFarki < 0` → `"{Math.Abs(GunFarki)} gün geçti"`
    - `t.GunFarki == 0` → `"Bugün"`
    - `t.GunFarki <= 7` → `"{GunFarki} gün kaldı"`
    - else → `t.VadeTarihi:dd.MM.yyyy`
- Sağ:
  - `text-right shrink-0`
  - `text-sm font-bold text-slate-800` → `{t.Kalan:N2} ₺`
  - Alt: `<a>` link `/Kiraci/Tahakkuklarim/Detay/{t.TahakkukId}` → `text-[11px] font-semibold text-[#1a6b5c] hover:underline mt-0.5 block` → "Ödeme Yap →"

#### §3.4.B Sağ — Son Ödemeler (Timeline)

```html
<div class="bg-white rounded-2xl border border-slate-200 p-5">
    <div class="flex items-center justify-between mb-4">
        <div class="text-sm font-bold text-slate-800">Son Ödemeler</div>
        <a href="/Kiraci/Tahakkuklarim"
           class="text-xs font-semibold text-[#1a6b5c] hover:underline">Tümünü Gör →</a>
    </div>
    @if (!Model.SonOdemeler.Any())
    {
        <div class="text-center py-8 text-slate-400 text-sm">Henüz ödeme kaydı yok</div>
    }
    else
    {
        <div class="relative pl-6">
            <!-- dikey çizgi -->
            <div class="absolute left-2 top-1 bottom-1 w-px bg-slate-200"></div>
            @foreach (var o in Model.SonOdemeler)
            {
                <!-- TIMELINE SATIRI -->
            }
        </div>
    }
</div>
```

**Timeline satırı template:**
- Wrap: `relative flex items-center justify-between py-2.5`
- Dot (mutlak sol): `absolute -left-[18px] top-1/2 -translate-y-1/2 w-2.5 h-2.5 rounded-full ring-4 ring-white bg-{renk}-500`
  - `{renk}` = `o.DurumDotRenk` → conditional aynı yöntem:
    ```razor
    string dotCls = o.DurumDotRenk switch {
        "emerald" => "bg-emerald-500",
        "red" => "bg-red-500",
        _ => "bg-amber-500"
    };
    ```
- Sol:
  - `text-sm font-semibold text-slate-800` → `{o.OdemeTarihi:dd.MM.yyyy}`
  - `text-xs text-slate-500 mt-0.5` → `o.KanalAd`
- Sağ:
  - `text-right`
  - `text-sm font-bold text-slate-800` → `{o.Tutar:N2} ₺`
  - Alt: `text-[10px] font-bold uppercase tracking-wider` + renk:
    - emerald → `text-emerald-700`
    - red → `text-red-700`
    - amber → `text-amber-700`
    İçerik: `o.DurumAd`

---

### §3.5 Hızlı Eylemler

**Grid wrap:** `grid grid-cols-2 md:grid-cols-4 gap-3 mb-2`

**4 kart:**

| # | Başlık | Açıklama | Hedef | İkon (lucide) | Gradient bg | İkon kutusu gradient |
|---|---|---|---|---|---|---|
| 1 | Tahakkuklarım | Borç ve ödeme akışı | `/Kiraci/Tahakkuklarim` | `receipt` | `from-emerald-50 to-emerald-100/60` | `from-emerald-500 to-emerald-600` |
| 2 | Sözleşmelerim | Aktif sözleşme listesi | `/Kiraci/Sozlesme` | `file-text` | `from-blue-50 to-blue-100/60` | `from-blue-500 to-blue-600` |
| 3 | Rezervasyonlarım | Salon rezervasyonu | `/Kiraci/Rezervasyonum` | `calendar` | `from-amber-50 to-amber-100/60` | `from-amber-500 to-amber-600` |
| 4 | Firma Profili | Firma bilgileri | `/Kiraci/Profil` | `building-2` | `from-slate-50 to-slate-100/60` | `from-slate-500 to-slate-600` |

**Kart template:**
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
    colors: ['#94a3b8', '#10b981'],   // slate-400, emerald-500
    plotOptions: { bar: { columnWidth: '55%', borderRadius: 4 } },
    dataLabels: { enabled: false },
    legend: { show: false },
    grid: { borderColor: '#e2e8f0', strokeDashArray: 4, yaxis: { lines: { show: true } }, xaxis: { lines: { show: false } } },
    xaxis: { categories: aylar, labels: { style: { fontSize: '11px', colors: '#64748b' } }, axisBorder: { show: false }, axisTicks: { show: false } },
    yaxis: { labels: { style: { fontSize: '11px', colors: '#94a3b8' }, formatter: v => v.toLocaleString('tr-TR') + ' ₺' } },
    tooltip: { y: { formatter: v => v.toLocaleString('tr-TR', {minimumFractionDigits: 2, maximumFractionDigits: 2}) + ' ₺' } }
}).render();
```

### §4.B Borç Tipi Dağılımı (Donut — `#chart-donut`)

```js
const donutData   = [@Html.Raw(string.Join(",", Model.BorcTipiDagilimi.Select(x => x.Tutar.ToString(System.Globalization.CultureInfo.InvariantCulture))))];
const donutLabels = [@Html.Raw(string.Join(",", Model.BorcTipiDagilimi.Select(x => $"'{x.Ad.Replace("'", "\\'")}'")))];

if (donutData.length > 0) {
    new ApexCharts(document.querySelector('#chart-donut'), {
        chart: { type: 'donut', height: 240, animations: { enabled: true, speed: 600 } },
        series: donutData,
        labels: donutLabels,
        colors: ['#1a6b5c', '#3b82f6', '#f59e0b', '#64748b', '#10b981', '#ef4444'],
        plotOptions: { pie: { donut: { size: '68%', labels: { show: true, total: { show: true, label: 'Toplam', formatter: w => w.globals.seriesTotals.reduce((a,b)=>a+b,0).toLocaleString('tr-TR', {minimumFractionDigits: 2}) + ' ₺' } } } } },
        dataLabels: { enabled: false },
        legend: { position: 'bottom', fontSize: '11px', markers: { width: 8, height: 8 } },
        stroke: { width: 2, colors: ['#fff'] },
        tooltip: { y: { formatter: v => v.toLocaleString('tr-TR', {minimumFractionDigits: 2}) + ' ₺' } }
    }).render();
}
```

### §4.C Sparkline (Borç Bakiyesi — `#sparkline-borc`)

```js
const sparkData = [@Html.Raw(string.Join(",", Model.BorcBakiyesiSparkline.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture))))];

new ApexCharts(document.querySelector('#sparkline-borc'), {
    chart: { type: 'area', height: 40, sparkline: { enabled: true }, animations: { enabled: true } },
    series: [{ name: 'Bakiye', data: sparkData }],
    stroke: { curve: 'smooth', width: 2 },
    colors: ['#1a6b5c'],
    fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0 } },
    tooltip: { fixed: { enabled: false }, x: { show: false }, y: { formatter: v => v.toLocaleString('tr-TR', {minimumFractionDigits: 2}) + ' ₺', title: { formatter: () => '' } }, marker: { show: false } }
}).render();
```

### §4.D CountUp init

> Skeleton'da YAZILI. Olduğu gibi bırak.

---

## §5. Acceptance Checklist (Bitince kontrol et)

- [ ] Sayfa açılınca **build hatası yok**, **JS console hatası yok**
- [ ] Hero banner: gradient `#1a6b5c → #155549`, sağda avatar + rol rozeti, motivasyon mesajı doğru durum (gecikmiş/yaklaşan/güncel) bazlı değişiyor
- [ ] 4 KPI kartı: her birinin **kendi renk teması** doğru, ikon kutusu **gradient**, sayı **CountUp animasyonu** ile artıyor
- [ ] Toplam Açık Borç kartının altında **sparkline** görünüyor (eğri çizgi + soft fill)
- [ ] Gecikmiş Adet > 0 ise kart **kırmızı border + pulse dot**
- [ ] Aylık Nakit Akışı bar chart: 6 ay, 2 seri (slate-400 + emerald-500), grid sade (yatay çizgi), dataLabels yok, toolbar yok
- [ ] Borç Tipi Donut: total ortada görünür, palet tutarlı, boş ise "Açık borç yok" mesajı
- [ ] Yaklaşan Tahakkuklar 5 satır, **sol border 4px** vade durumuna göre renkli (red/amber/emerald), her satırda "Ödeme Yap" link'i Detay'a gidiyor
- [ ] Son Ödemeler 5 satır, **timeline** görünümü (sol çizgi + renkli dot), durum rengi tutarlı
- [ ] Hızlı Eylemler 4 kart, gradient bg, hover scale, doğru route'lara link
- [ ] Sayfa **mobile responsive**: KPI grid 1 sütuna, grafik paneli 1 sütuna düşüyor
- [ ] Lucide ikonları render oluyor (sayfa sonunda `lucide.createIcons()` zaten var)
- [ ] Yeni kütüphane EKLENMEMİŞ (sadece ApexCharts + CountUp kullanıldı, ikisi de zaten yüklü)
- [ ] Yeni Tailwind rengi EKLENMEMİŞ
- [ ] Mevcut `.card`, `.btn-*`, `.badge-*` sınıflarına dokunulmamış

---

## §6. Tailwind Safelist Uyarısı

Tailwind CDN runtime'da kullanılıyor (`https://cdn.tailwindcss.com`). Bu mod tüm class'ları otomatik tanır, **safelist** kurmana gerek yok. Yine de dinamik string interpolation yapma — Razor'da **conditional ile tam class adı** yaz (skeleton'daki `switch` örnekleri gibi).

---

## §7. Test Senaryosu

1. **Yeni veri yokken** sayfa aç → tüm kartlar "0" gösteriyor, grafik "Açık borç yok" mesajı, listeler empty state
2. **Gecikmiş borç eklenmiş kullanıcıyla** aç → hero'da uyarı, KPI'da kırmızı kart + pulse, yaklaşan listede kırmızı border
3. **Mobile (DevTools, 375px)** → tek sütun, taşma yok, grafik küçülüyor
4. **Sayfa refresh** → CountUp tekrar 0'dan başlıyor

---

## §8. Bitince

- Yukarıdaki acceptance checklist'i kontrol et
- Build temiz mi: `dotnet build KiraTakip/KiraTakip.csproj`
- Tarayıcıda `/Kiraci/Panel`'i aç ve görsel testler yap
- Sapma yoksa: "Tamamlandı, kontrole hazır" diye bildir
