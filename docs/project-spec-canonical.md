# Kira Takip Sistemi — .NET 9 MVC + 21st.dev Magic UI

> **GÜNCELLİK NOTU:** Bu dosya projenin ilk vizyon ve UI spec dokümanıdır. Güncel mimari kararlar için **MASTER-PLAN.md** ve **PROGRESS.md** dosyaları esas alınmalıdır; **PROGRESS-HISTORY.md** tarihsel arşivdir.
> - Proje **.NET 9** sürümündedir.
> - Veritabanı olarak **SQL Server + EF Core** kullanılmaktadır (DummyDataService kaldırıldı).
> - "KatBazli" terminolojisi **"BirimBazli"** olarak genelleştirilmiştir.
> - Enum olarak planlanan Tipler ve Kategoriler artık **Dinamik Parametre Tabloları** (Faz 8) üzerinden yönetilmektedir.

> **Bu doküman başlangıç vizyon ve UI spec dosyasıdır; doğrudan Claude Code talimatı DEĞİLDİR.** Tamamını okuma — yalnızca ilgili domain alanında kısmi oku (`offset`/`limit` ile). Güncel kararlar için `CLAUDE.md`, `PROGRESS.md`, aktif çalışmanın plan dosyası ve `MASTER-PLAN.md` esas alınmalıdır.

---

## 1. Proje Özeti

**Amaç:** Bir kullanıcının sahip olduğu taşınmazları (bina, arazi, tarla, kantin vb.) sisteme tanımlayıp, bu taşınmazları kiracılara kiralayabildiği, kira ücretlerini (yıllık/aylık) takip edebildiği, boştaki/kiralı/süresi dolmak üzere olan taşınmazları istatistik olarak görebildiği bir kira takip uygulaması.

**Kritik Davranış:** Bina tipindeki taşınmazlar **iki farklı şekilde** kiralanabilir:
1. **Tek parça** olarak (örn: kantin binası bütün olarak)
2. **Kat bazlı** olarak (örn: teknokent binasının her katı ayrı kiracıya)

Bu ayrımı domain modelinde net olarak kurmak zorundasın — `Birim` ara katmanı ile çözüldü, aşağıda detaylı.

**Teknoloji Yığını:**
- Backend: **.NET 9 MVC**
- UI Üretimi: **21st.dev Magic MCP** (`@21st-dev/magic`)
- Stil: **Tailwind CSS** (CDN ile)
- Veritabanı: **SQL Server** — EF Core servis katmanı
- Etkileşim: **Alpine.js** (CDN, küçük state'ler için) + vanilla JS

---

## 2. Magic MCP'nin Nasıl Kullanılacağı — KRİTİK

`@21st-dev/magic` MCP sunucusu sana bağlı. Bu sunucu, **doğal dilden modern React UI bileşenleri üretir**. Ancak senin projen .NET MVC, yani çıktı Razor (`.cshtml`) olmalı.

### 2.1 Ana İş Akışı

Her sayfa için şu adımları izle:

1. **Magic'ten ilham al, bileşen iste:** `/ui` komutuyla (veya MCP araçlarını çağırarak) sayfanın ihtiyacı olan bileşeni doğal dilde tarif et. Örnek prompt'lar Bölüm 7'de.
2. **Üretilen React/TSX kodunu al:** Magic genelde Tailwind + shadcn-style React bileşeni döner.
3. **Razor'a port et:** JSX'i HTML'e çevir, prop'ları Razor `@Model` ile değiştir, state gereken yerlere Alpine.js direktifi yerleştir.
4. **CSS sınıflarını ve animasyonları AYNEN koru.** Magic'in ürettiği bileşenlerin asıl değeri Tailwind class kombinasyonları, gradient, blur, shadow ve mikro-etkileşim detaylarındadır. Bunları sadeleştirme.

### 2.2 Magic'in Diğer Yetenekleri

Magic MCP'nin bileşen üretmek dışında iki güçlü özelliği daha var, **kullan**:
- **`logo_search`** veya benzeri: SVGL üzerinden brand logo arama. Header'daki marka logosu için kullan.
- **`component_inspiration`** veya benzeri: Mevcut 21st.dev kütüphanesinden bileşen örneklerine erişim. Sıfırdan üretmeden önce her zaman ilham/örnek için sor.

### 2.3 Magic ile Nasıl Konuşulur — Prompt Disiplini

Generic prompt'lar generic çıktı üretir. **Bağlam ver:**

❌ Kötü: `/ui dashboard cards`
✅ İyi: `/ui Generate 4 KPI stat cards for a real estate rental management dashboard. Each card should have: an icon (using lucide-react), a label, a large metric value, a delta indicator (green/red), and a subtle sparkline or sub-text. Style: refined, minimal, premium SaaS feel like Linear or Vercel dashboards. Use a warm neutral palette (stone/zinc) with a single accent color (emerald). Include subtle hover states.`

❌ Kötü: `/ui sidebar`
✅ İyi: `/ui Build a vertical sidebar navigation for a property management SaaS in Turkish. Width 260px, fixed left. Top: brand mark + product name "Kira Takip". Middle: nav sections — Genel (Anasayfa), Yönetim (Taşınmazlar, Kiracılar, Sözleşmeler), Raporlar. Each item has a lucide-react icon, label, and an active state with a subtle left accent bar. Bottom: user profile card with avatar, name, role, and a kebab menu. Style: minimalist, off-white background, soft borders, no harsh shadows.`

**Her sayfa için aşağıda Bölüm 7'de hazır prompt'lar var. Kullan, gerekirse Türkçe/iş bağlamına göre uyarla.**

---

## 3. Domain Modeli

`Models/` klasörü altında oluştur.

### 3.1 Enum'lar (`Models/Enums.cs`)

```csharp
public enum TasinmazTipi { Bina = 1, Arazi = 2, Tarla = 3, Depo = 4, Diger = 5 }
public enum KiralamaSekli { TekParca = 1, KatBazli = 2 }
public enum KiraDurumu { Bos = 1, Kirali = 2, SuresiDolmakUzere = 3 }
public enum KiraPeriyodu { Aylik = 1, Yillik = 2 }
public enum BirimTipi { Komple = 1, Kat = 2 }
```

### 3.2 Tasinmaz

```csharp
public class Tasinmaz
{
    public int Id { get; set; }
    public string Ad { get; set; }
    public TasinmazTipi Tipi { get; set; }
    public KiralamaSekli KiralamaSekli { get; set; }

    public string Il { get; set; }
    public string Ilce { get; set; }
    public string Mahalle { get; set; }
    public string AcikAdres { get; set; }

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    public int? KatSayisi { get; set; }       // Sadece Bina + KatBazli için
    public string? Aciklama { get; set; }
    public DateTime KayitTarihi { get; set; }

    public List<Birim> Birimler { get; set; } = new();
}
```

### 3.3 Birim — KRİTİK

Kira sözleşmeleri **her zaman** bir `Birim`'e bağlanır. Bu polimorfik davranışın anahtarıdır.

- `KiralamaSekli == TekParca` → 1 adet `Birim` (`BirimTipi = Komple`)
- `KiralamaSekli == KatBazli` → `KatSayisi` adet `Birim` (`BirimTipi = Kat`, `KatNo = 1..n`)

```csharp
public class Birim
{
    public int Id { get; set; }
    public int TasinmazId { get; set; }
    public Tasinmaz Tasinmaz { get; set; }

    public BirimTipi BirimTipi { get; set; }
    public int? KatNo { get; set; }
    public string Ad { get; set; }                   // "Komple" / "1. Kat"
    public decimal Yuzolcumu { get; set; }

    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = new();
}
```

### 3.4 Kiracı

```csharp
public class Kiraci
{
    public int Id { get; set; }
    public string AdSoyad { get; set; }
    public bool Kurumsal { get; set; }
    public string? VergiNo { get; set; }
    public string? TcKimlikNo { get; set; }
    public string Telefon { get; set; }
    public string Email { get; set; }
    public string? Adres { get; set; }
    public DateTime KayitTarihi { get; set; }
}
```

### 3.5 KiraSozlesmesi

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

    public decimal KiraBedeli { get; set; }
    public KiraPeriyodu Periyot { get; set; }

    public decimal? Depozito { get; set; }
    public string? Notlar { get; set; }
}
```

---

## 4. Servis Katmanı

### 4.1 `Services/DummyDataService.cs` — Singleton

Tüm veriyi in-memory listelerde tutar. `Program.cs`'te `AddSingleton<DummyDataService>()` olarak kayıtlı. Aşağıdaki metotları içerir:

- `IEnumerable<Tasinmaz> GetAllTasinmaz()`
- `Tasinmaz? GetTasinmaz(int id)`
- `int AddTasinmaz(Tasinmaz t)` — eklerken otomatik olarak `Birim`leri de oluşturur
- `IEnumerable<Kiraci> GetAllKiraci()` / `GetKiraci(int id)` / `AddKiraci(...)`
- `IEnumerable<KiraSozlesmesi> GetAllSozlesmeler()` / `GetSozlesme(int id)` / `AddSozlesme(...)`
- `IEnumerable<Birim> GetBirimler()` / `GetBirim(int id)` / `GetBosBirimler()`

Constructor'da seed veriyi yükle (Bölüm 5).

### 4.2 `Services/IstatistikService.cs`

Dashboard ve durum hesaplamaları:

```csharp
public KiraDurumu GetBirimDurumu(Birim birim)
{
    var aktif = birim.Sozlesmeler
        .Where(s => s.BaslangicTarihi <= DateTime.Now && s.BitisTarihi >= DateTime.Now)
        .OrderByDescending(s => s.BitisTarihi)
        .FirstOrDefault();

    if (aktif == null) return KiraDurumu.Bos;
    return (aktif.BitisTarihi - DateTime.Now).Days <= 30
        ? KiraDurumu.SuresiDolmakUzere
        : KiraDurumu.Kirali;
}

public decimal AylikBedel(KiraSozlesmesi s) =>
    s.Periyot == KiraPeriyodu.Yillik ? s.KiraBedeli / 12 : s.KiraBedeli;

public decimal YillikBedel(KiraSozlesmesi s) =>
    s.Periyot == KiraPeriyodu.Aylik ? s.KiraBedeli * 12 : s.KiraBedeli;

public DashboardStats GetDashboardStats() { /* ... */ }
```

`DashboardStats` ViewModel:
- `int ToplamTasinmaz`
- `Dictionary<TasinmazTipi, int> TasinmazTipDagilimi`
- `int ToplamBirim`, `int KiraliBirim`, `int BosBirim`, `int SuresiDolmakUzereBirim`
- `int AktifSozlesmeSayisi`
- `decimal AylikGelir`, `decimal YillikProjeksiyon`
- `List<KiraSozlesmesi> SuresiDolmakUzereSozlesmeler` (top 5)
- `List<Birim> BosBirimlerListe` (top 5)
- `List<(string Ay, decimal Gelir)> Son12AyGelir` — chart için (dummy)

---

## 5. Seed Verisi

`DummyDataService` constructor'ında **bilinçli çeşitlilikte** veri yükle:

**Taşınmazlar:**

| Ad | Tipi | KiralamaSekli | Kat | Konum | Açık m² | Kapalı m² |
|---|---|---|---|---|---|---|
| Teknokent A Blok | Bina | KatBazli | 5 | İzmir/Bornova | 800 | 4500 |
| Çamlık Kantini | Bina | TekParca | - | İzmir/Karşıyaka | 100 | 180 |
| Bornova Tarlası | Tarla | TekParca | - | İzmir/Bornova | 12000 | 0 |
| Atatürk Cd. Dükkan | Bina | TekParca | - | İzmir/Alsancak | 0 | 95 |
| Sanayi Sitesi B Blok | Bina | KatBazli | 3 | İzmir/Kemalpaşa | 400 | 1800 |
| Menemen Arazi | Arazi | TekParca | - | İzmir/Menemen | 8500 | 0 |
| Buca Deposu | Depo | TekParca | - | İzmir/Buca | 200 | 1200 |

**Kiracılar (en az 7):**
- Bireysel: Ahmet Yılmaz, Ayşe Demir, Mehmet Kaya
- Kurumsal: Yıldız Yazılım A.Ş., Anadolu Lojistik Ltd., Ege Tarım Koop., Mavi Cafe & Restoran

**Sözleşmeler — durum çeşitliliği şart:**
- Teknokent A Blok 5 katı: 1.kat dolu (8 ay kalmış), 2.kat dolu (45 gün kalmış), 3.kat süresi dolmak üzere (12 gün kalmış), 4.kat boş, 5.kat boş
- Sanayi B Blok 3 katı: 1.kat dolu, 2.kat süresi dolmak üzere (22 gün), 3.kat boş
- Çamlık Kantini: Aktif (Mavi Cafe, 60+ gün kalmış)
- Atatürk Cd. Dükkan: Süresi dolmuş (geçmiş sözleşme)
- Bornova Tarlası: Aktif (Ege Tarım, yıllık periyot)
- Buca Deposu: Aktif (Anadolu Lojistik)
- Menemen Arazi: Boş

Bu veri seti dashboard'da **anlamlı istatistikler** üretir.

---

## 6. Sayfa Yapısı

| Controller | Action | Route | Açıklama |
|---|---|---|---|
| `HomeController` | `Index` | `/` | Dashboard |
| `TasinmazController` | `Index` | `/Tasinmaz` | Taşınmaz listesi (kart grid) |
| `TasinmazController` | `Detay` | `/Tasinmaz/Detay/{id}` | Detay + birimler + sözleşmeler |
| `TasinmazController` | `Ekle` | `/Tasinmaz/Ekle` | Yeni taşınmaz formu |
| `KiraciController` | `Index` | `/Kiraci` | Kiracı tablosu |
| `KiraciController` | `Detay` | `/Kiraci/Detay/{id}` | Kiracı profili |
| `KiraciController` | `Ekle` | `/Kiraci/Ekle` | Yeni kiracı formu |
| `SozlesmeController` | `Index` | `/Sozlesme` | Sözleşme listesi (filtreli) |
| `SozlesmeController` | `Detay` | `/Sozlesme/Detay/{id}` | **Kira Takip Kartı (vitrin sayfası)** |
| `SozlesmeController` | `Ekle` | `/Sozlesme/Ekle?birimId=...` | Yeni sözleşme |

---

## 7. Magic MCP Prompt'ları — Sayfa Sayfa

> Aşağıdaki prompt'ları Magic MCP'ye `/ui` komutuyla gönder. Dönen React/TSX kodunu Razor'a port et. **Stil (Tailwind class) kombinasyonlarını koru.**

### 7.1 Layout — Sidebar + Topbar

```
/ui Build a complete dashboard shell layout for a Turkish property rental management SaaS called "Kira Takip".
- Fixed left sidebar (260px), full height. Off-white background (stone-50), 1px right border (stone-200).
- Sidebar header: small geometric brand mark (a stylized building icon in emerald-600), product name "Kira Takip" in a serif display font, subtitle "Yönetim Paneli" in muted text.
- Sidebar nav grouped in sections with small uppercase labels:
  - "GENEL" → Anasayfa (LayoutDashboard icon)
  - "YÖNETİM" → Taşınmazlar (Building2), Kiracılar (Users), Sözleşmeler (FileText)
  - "İŞLEMLER" → Hızlı Ekle (Plus, with dropdown chevron)
- Active nav item: subtle emerald-50 background, emerald-700 text, 2px emerald-600 left accent bar.
- Hover state: stone-100 background.
- Bottom of sidebar: user card with circular avatar (initials "DK"), name "Demo Kullanıcı", role "Yönetici", and a small kebab menu icon.
- Top bar (right of sidebar): 64px height, white, 1px bottom border. Left: page title (dynamic). Center: search input with kbd hint "⌘K". Right: notification bell with red dot, divider, theme toggle.
- Main content area: stone-50 background, generous padding (p-8).
- Use Inter for body, but headings use a serif like "Fraunces" or "Instrument Serif" via Google Fonts.
- Polished, premium SaaS feel — think Linear, Cal.com, Vercel. No harsh shadows, refined borders.
```

### 7.2 Dashboard

```
/ui Build a property management dashboard hero section in Turkish.
Top: 5-column grid of KPI stat cards. Each card has:
- Small icon in a colored rounded square (different accent per card: emerald, blue, amber, rose, violet)
- Uppercase tracking-wide label ("TOPLAM TAŞINMAZ", "AKTİF SÖZLEŞME", etc.)
- Large metric (text-3xl, semibold, tabular-nums)
- Sub-text with mini breakdown ("3 bina · 2 arazi · 1 tarla")
- Optional inline mini sparkline at bottom right
Cards: white bg, 1px stone-200 border, rounded-xl, p-5, hover lifts shadow subtly.

Below: 2-column grid (60/40 split).
- LEFT (60%): "Süresi Dolmak Üzere" card. Header has title + "Tümünü gör" link. List of 5 contracts, each row: tenant avatar (initials), name + property/unit name, days remaining badge (red if <15 days, amber if 15-30), contract end date in muted text. Subtle row dividers.
- RIGHT (40%): "Boş Birimler" card. Same header pattern. Each row: property type icon, property name + unit, sqm meta, "Kirala" ghost button on right.

Below those: a wide chart card titled "Aylık Kira Geliri (Son 12 Ay)" with an area chart in emerald gradient (using recharts). Y-axis formatted as "₺ 1.2M" style. Subtle grid lines.

Style: refined, premium, generous whitespace. Tailwind only. Lucide icons.
```

### 7.3 Taşınmaz Listesi

```
/ui Build a property cards grid view for a real estate management app.
Top toolbar: page title "Taşınmazlar", subtitle "7 kayıt", on right a search input, filter dropdown (Tip: Tümü/Bina/Arazi/Tarla/Depo), filter dropdown (Durum), and a primary "+ Yeni Taşınmaz" button.

Below: responsive grid (1 / 2 / 3 columns). Each property card:
- Top: a colored gradient header strip (different per type — Bina: emerald, Arazi: amber, Tarla: green, Depo: slate) with the type icon centered, large and translucent.
- Body: property name (text-lg semibold), location with map-pin icon (city/district), 3-dot meta row (sqm, kat sayısı or unit count, status pills).
- Status section: count of "X kiralı / Y boş" with mini progress bar.
- Footer: "Detayları Görüntüle →" link.

Card has rounded-2xl, subtle border, hover lifts. The whole card is clickable.
```

### 7.4 Taşınmaz Detay

```
/ui Build a property detail page for a rental management app.
Top: breadcrumb (Anasayfa / Taşınmazlar / [Property Name]), then a hero header: large property name (serif), location with icon, type badge, "Düzenle" and "Sözleşme Ekle" buttons on the right.

Below header: 4 quick stat tiles in a row — Toplam m² (open/closed split), Birim Sayısı, Aktif Sözleşme, Aylık Gelir.

Then a tabbed interface (Genel / Birimler / Sözleşmeler / Notlar):

GENEL tab: 2-column layout.
- Left: details list (label/value pairs) — Tip, Kiralama Şekli, İl, İlçe, Mahalle, Açık Adres, Açık m², Kapalı m², Kayıt Tarihi.
- Right: a small map placeholder (gray rectangle with map-pin) and quick contact actions.

BİRİMLER tab: a clean table OR cards (use cards for this kind of UI). Each unit (e.g., "1. Kat", "2. Kat", "Komple") shows: name, sqm, current status badge (Boş/Kiralı/Süresi Dolmak Üzere with appropriate colors), if rented: tenant name + days remaining, action button ("Kirala" if empty / "Detay" if rented).

SÖZLEŞMELER tab: timeline-style list of all contracts (active + past) for this property. Each item: tenant avatar/name, unit, period (start–end), monthly rent, status badge.

Style: editorial/premium, generous spacing, refined typography.
```

### 7.5 Taşınmaz Ekle Formu

```
/ui Build a "Create Property" form for a rental management app, in Turkish.
Layout: 2-column form on a card with rounded-2xl border.
Sections (with section headers):
1. "Temel Bilgiler" — Ad (text), Tip (segmented control: Bina/Arazi/Tarla/Depo/Diğer with icons), Kayıt Tarihi (date).
2. "Kiralama Yapısı" — only visible if Tip=Bina. Radio cards: "Tek Parça Kirala" (single building) vs "Kat Bazlı Kirala" (per-floor). Each radio card has icon + title + description. If "Kat Bazlı", show "Kat Sayısı" number input.
3. "Adres" — İl, İlçe, Mahalle (3-column grid), Açık Adres (textarea full width).
4. "Yüzölçümü" — Açık Alan (m²), Kapalı Alan (m²) side by side with unit suffix.
5. "Açıklama" — textarea optional.

Bottom: sticky action bar with "İptal" ghost button left, "Kaydet" primary emerald button right. Show a small note: "Kat bazlı kiralama seçilirse, kat sayısı kadar birim otomatik oluşturulacaktır."

Style: refined, with field labels above inputs, helper text below, focus rings in emerald.
```

### 7.6 Kiracı Listesi

```
/ui Build a tenants table page in Turkish.
Top: title "Kiracılar", subtitle with count, search input, "Tümü/Bireysel/Kurumsal" segmented filter, "+ Yeni Kiracı" button.

Table columns: Avatar+Ad, Tip badge (Bireysel/Kurumsal), İletişim (phone+email stacked), Aktif Sözleşme Sayısı (count + small pill), Toplam Aylık Gelir (formatted ₺), Kayıt Tarihi, action menu (•••).

Row hover: subtle emerald-50 tint. Row click → tenant detail.
Empty state: illustration + "Henüz kiracı yok" + CTA button.
Style: premium SaaS table, like Linear or Notion. No striping, just clean dividers.
```

### 7.7 Kiracı Detay

```
/ui Build a tenant profile page for a rental management app.
Top: large profile header card with avatar (initials for individuals, building icon for corporate), name, type badge, contact details (phone, email) with copy buttons.

Below header: stat row — Aktif Sözleşme Sayısı, Toplam Kiraladığı Birim, Aylık Toplam Ödeme, Kiracı Olduğu Süre.

Main content: 2 sections.
1. "Kira Sözleşmeleri" — card list. Each card: property+unit name, period, monthly rent, status badge. Click → contract detail.
2. "Kişisel/Kurumsal Bilgiler" — info grid: TC/Vergi No, Adres, Kayıt Tarihi.

Style: editorial, generous whitespace.
```

### 7.8 Kiracı Ekle

```
/ui Build a "Add Tenant" form. At top, a large segmented toggle: "Bireysel" vs "Kurumsal". The form fields below change based on selection (animate transition).
- Bireysel: Ad Soyad, TC Kimlik No (11 digits), Telefon, Email, Adres.
- Kurumsal: Firma Adı, Vergi No, Yetkili Kişi (optional), Telefon, Email, Adres.
Same refined style as the property form. Sticky save bar.
```

### 7.9 Sözleşme Listesi

```
/ui Build a rental contracts list page.
Top: title "Sözleşmeler" + filter chips (Tümü, Aktif, Süresi Dolmak Üzere, Geçmiş) showing counts in pills. Search input. Date range picker. "+ Yeni Sözleşme" button.

List as cards (NOT a dense table — these contracts deserve more visual weight). Each card row layout:
- Left: tenant avatar + name, "→" arrow, property+unit name.
- Middle: period range with calendar icon, days remaining (or "Sona erdi"), status badge.
- Right: monthly amount in large tabular nums + "/ay" suffix. Yıllık total in small muted text.
- Action: "Detay" button.

Süresi dolmak üzere olan sözleşmeler için kartın sol kenarında 3px amber accent bar.
```

### 7.10 Sözleşme Detay — KİRA TAKİP KARTI (VİTRİN)

> **Bu sayfa uygulamanın yıldızı. Ekstra özen göster.**

```
/ui Build a "rental contract detail" page in Turkish — this is the showcase page of the app, treat it like a premium ticket/receipt UI.

HERO SECTION: A large card with subtle paper-texture or noise overlay.
- Top strip: contract ID (e.g., "SÖZ-2024-0042") in mono font, status pill (Aktif/Süresi Dolmak Üzere/Sona Ermiş).
- Two-column header inside the hero:
  - LEFT: "Kiracı" label, then large name (serif font, text-2xl), type badge, contact line.
  - RIGHT (right-aligned): "Taşınmaz" label, then property name + unit (e.g., "Teknokent A Blok — 3. Kat"), location small.
- Visual divider: a dashed horizontal line with a small "ticket notch" effect (two semi-circle cutouts at the edges).

METRICS ROW: 3 cards in a row, equal width.
1. "Kira Bedeli" card — two stacked numbers: large monthly amount "₺ 12.500 /ay", smaller yearly "₺ 150.000 /yıl". Periyot badge.
2. "Sözleşme Süresi" card — Başlangıç → Bitiş dates (with arrow icon between), below a horizontal progress bar showing % elapsed (emerald fill, stone track), with "8 ay 12 gün geçti" caption.
3. "Kalan Süre" card — large day count (text-4xl tabular-nums), "gün" suffix, contextual subtitle: "Sözleşme aktif ve düzenli ilerliyor" (or warning text if <30 days).

TABS BELOW: Detaylar / Birim Bilgisi / Kiracı Bilgisi / Geçmiş Sözleşmeler.

DETAYLAR tab: Definition list — Depozito, Periyot, Notlar, Oluşturulma tarihi, Son güncelleme. Two-column.

BİRİM BİLGİSİ tab: A mini property card — name, type, sqm breakdown, address, link to property detail.

KİRACI BİLGİSİ tab: A mini tenant card — name, contact, link to tenant detail.

GEÇMİŞ SÖZLEŞMELER tab: timeline of past contracts on this same unit.

Top right floating action bar: "Sözleşmeyi Yenile" primary, "PDF İndir" ghost, "Sonlandır" danger ghost.

Style: editorial, with a subtle emerald accent. The hero card should feel like a beautifully designed receipt or ticket. Use real visual hierarchy with serif display headings.
```

### 7.11 Sözleşme Ekle

```
/ui Build a "Create Contract" form, multi-step style with a stepper at top:
1. Birim Seç → searchable combobox listing only available units (with property name + unit + sqm preview), optionally pre-selected from URL.
2. Kiracı Seç → searchable combobox with all tenants, OR "+ Yeni Kiracı" inline option.
3. Sözleşme Bilgileri → date range picker (Başlangıç + Bitiş), Periyot segmented (Aylık/Yıllık), Kira Bedeli with ₺ prefix, Depozito (optional), Notlar textarea.
4. Özet → review card showing all entered info before saving.

Right sidebar throughout: a sticky summary card showing selected unit + tenant + computed monthly/yearly totals as user fills in.

Style: refined, calm, with smooth step transitions.
```

---

## 8. Tasarım Sistemi — Renkler ve Tipografi

> Magic farklı varyantlar üretebilir. Tutarlılık için aşağıdaki tokenları **`_Layout.cshtml` içinde CSS değişkenleri olarak tanımla** ve Magic'e prompt yazarken bu paleti referans göster.

### 8.1 Renk Paleti (Tailwind referansları ile)

```css
:root {
  /* Surface */
  --bg-app: #fafaf9;          /* stone-50 */
  --bg-surface: #ffffff;
  --bg-muted: #f5f5f4;        /* stone-100 */

  /* Text */
  --text-primary: #1c1917;    /* stone-900 */
  --text-secondary: #57534e;  /* stone-600 */
  --text-muted: #a8a29e;      /* stone-400 */

  /* Borders */
  --border-default: #e7e5e4;  /* stone-200 */
  --border-strong: #d6d3d1;   /* stone-300 */

  /* Brand: derin emerald (yapraklı, premium) */
  --brand-50:  #ecfdf5;
  --brand-100: #d1fae5;
  --brand-500: #10b981;
  --brand-600: #059669;
  --brand-700: #047857;

  /* Accent: amber (uyarı, sıcak vurgu) */
  --accent-50:  #fffbeb;
  --accent-500: #f59e0b;
  --accent-700: #b45309;

  /* Status */
  --status-bos: var(--text-secondary);
  --status-kirali: var(--brand-600);
  --status-uyari: var(--accent-500);
  --status-sona: #b91c1c;     /* red-700 */
}
```

### 8.2 Tipografi

`<head>` içinde Google Fonts:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,400;9..144,500;9..144,600&family=Inter:wght@400;500;600&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
```

- **Body & UI:** Inter
- **Display headings (h1, h2, hero metrikleri):** Fraunces (italic varyantları zarif)
- **Mono (kontrat ID, sayılar):** JetBrains Mono

`tailwind.config` tarzı yerine CDN kullanırken `<script>` ile özelleştir:

```html
<script>
  tailwind.config = {
    theme: {
      extend: {
        fontFamily: {
          sans: ['Inter', 'sans-serif'],
          serif: ['Fraunces', 'serif'],
          mono: ['JetBrains Mono', 'monospace']
        }
      }
    }
  }
</script>
```

### 8.3 Mikro-Detaylar (Önemli)

Bu detaylar generic AI görünümünden ayıran şeyler — ihmal etme:

- **Tabular numerals:** Tüm para ve gün sayılarında `font-variant-numeric: tabular-nums` (Tailwind: `tabular-nums`).
- **Para formatı:** Türkçe locale: `1.250.000 ₺` (binlik nokta, ondalık virgül, sembol sonda). Razor helper:
  ```csharp
  public static string Tl(this decimal v) => v.ToString("N0", new CultureInfo("tr-TR")) + " ₺";
  ```
- **Tarih formatı:** "12 Mar 2025" gibi kısa Türkçe (CultureInfo "tr-TR").
- **Hover transitions:** `transition-all duration-200`. Aniden değişen state olmasın.
- **Focus ring:** Brand color, 2px, offset 2px. Erişilebilirlik için `focus-visible`.
- **Boş durum (empty state):** Her liste sayfasının boş hali için Magic'e ayrıca prompt yaz — illustration + cta.
- **Scrollbar:** Webkit'ı özelleştir (`scrollbar-width: thin`).
- **Selection:** `::selection { background: var(--brand-100); color: var(--brand-700); }`.

---

## 9. Form Davranışları (Alpine.js ile)

CDN ekle: `<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>`

### 9.1 Taşınmaz Ekle

```html
<div x-data="{ tip: 'Bina', kiralamaSekli: 'TekParca' }">
  <!-- Tip seçim -->
  <div x-show="tip === 'Bina'">
    <!-- Kiralama şekli radio -->
    <div x-show="kiralamaSekli === 'KatBazli'">
      <!-- Kat sayısı input -->
    </div>
  </div>
</div>
```

### 9.2 Kiracı Ekle

`x-data="{ kurumsal: false }"` — switch state'ine göre alanlar değişir, animasyonlu transition.

### 9.3 Sözleşme Ekle (Multi-step)

`x-data="{ step: 1, birimId: null, kiraciId: null, ... }"` — step state'i, ilerleme barı, "Devam Et" / "Geri" butonları.

---

## 10. Razor View Yapısı — Magic Çıktısını Port Etme

Magic, React (TSX) bileşeni döner. Şu pattern'i izle:

**Magic çıktısı (örnek):**
```tsx
export function StatCard({ label, value, delta }) {
  return (
    <div className="bg-white rounded-xl border border-stone-200 p-5 hover:shadow-md transition-shadow">
      <p className="text-xs font-medium uppercase tracking-wider text-stone-500">{label}</p>
      <p className="text-3xl font-semibold mt-2 tabular-nums">{value}</p>
      {delta && <span className="text-emerald-600 text-sm">{delta}</span>}
    </div>
  );
}
```

**Razor Partial View'a port (`Views/Shared/_StatCard.cshtml`):**
```cshtml
@model StatCardModel
<div class="bg-white rounded-xl border border-stone-200 p-5 hover:shadow-md transition-shadow">
  <p class="text-xs font-medium uppercase tracking-wider text-stone-500">@Model.Label</p>
  <p class="text-3xl font-semibold mt-2 tabular-nums">@Model.Value</p>
  @if (!string.IsNullOrEmpty(Model.Delta))
  {
    <span class="text-emerald-600 text-sm">@Model.Delta</span>
  }
</div>
```

**Kuralllar:**
- `className` → `class`
- `{variable}` → `@Model.Variable` veya `@variable`
- Conditional rendering `{x && <...>}` → `@if (x) { <...> }`
- Map `{items.map(x => ...)}` → `@foreach (var x in Model.Items) { ... }`
- React state → Alpine `x-data` veya server-side
- Lucide icons: SVG olarak inline kopyala (https://lucide.dev) veya `<i data-lucide="..."></i>` + Lucide CDN.

**Lucide CDN:**
```html
<script src="https://unpkg.com/lucide@latest"></script>
<script>document.addEventListener('DOMContentLoaded', () => lucide.createIcons());</script>
```

---

## 11. Proje Yapısı

```
KiraTakip/
├── Controllers/
│   ├── HomeController.cs
│   ├── TasinmazController.cs
│   ├── KiraciController.cs
│   └── SozlesmeController.cs
├── Models/
│   ├── Enums.cs
│   ├── Tasinmaz.cs / Birim.cs / Kiraci.cs / KiraSozlesmesi.cs
│   └── ViewModels/
│       ├── DashboardViewModel.cs
│       ├── TasinmazDetayViewModel.cs
│       ├── SozlesmeDetayViewModel.cs
│       ├── KiraciDetayViewModel.cs
│       └── StatCardViewModel.cs
├── Services/
│   ├── DummyDataService.cs
│   └── IstatistikService.cs
├── Helpers/
│   └── FormatHelpers.cs        // Tl, Tarih, vb. extension methods
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml      // Tailwind + Fonts + Alpine + Lucide
│   │   ├── _Sidebar.cshtml
│   │   ├── _Topbar.cshtml
│   │   ├── _StatCard.cshtml
│   │   ├── _StatusBadge.cshtml
│   │   ├── _EmptyState.cshtml
│   │   └── _ValidationSummary.cshtml
│   ├── Home/Index.cshtml
│   ├── Tasinmaz/{Index,Detay,Ekle}.cshtml
│   ├── Kiraci/{Index,Detay,Ekle}.cshtml
│   └── Sozlesme/{Index,Detay,Ekle}.cshtml
├── wwwroot/
│   ├── css/site.css            // Custom utilities (selection, scrollbar)
│   └── js/
│       ├── tasinmaz-form.js
│       ├── sozlesme-form.js
│       └── chart.js            // Dashboard chart init
├── Program.cs
└── appsettings.json
```

---

## 12. Dashboard Chart — Recharts Yerine

`recharts` React kütüphanesi. Vanilla için **ApexCharts** veya **Chart.js** kullan (CDN). Önerim: **ApexCharts** — Magic'in ürettiği grafik estetiğiyle daha uyumlu.

```html
<script src="https://cdn.jsdelivr.net/npm/apexcharts"></script>
<div id="gelirChart"></div>
<script>
  new ApexCharts(document.querySelector("#gelirChart"), {
    chart: { type: 'area', height: 280, toolbar: { show: false } },
    colors: ['#059669'],
    fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05 } },
    stroke: { curve: 'smooth', width: 2 },
    dataLabels: { enabled: false },
    grid: { borderColor: '#e7e5e4', strokeDashArray: 4 },
    series: [{ name: 'Gelir', data: @Html.Raw(Json.Serialize(Model.Son12AyGelir.Select(x => x.Gelir))) }],
    xaxis: { categories: @Html.Raw(Json.Serialize(Model.Son12AyGelir.Select(x => x.Ay))), labels: { style: { colors: '#78716c', fontSize: '11px' } } },
    yaxis: { labels: { formatter: v => '₺ ' + (v/1000).toFixed(0) + 'B', style: { colors: '#78716c' } } },
    tooltip: { theme: 'light', y: { formatter: v => v.toLocaleString('tr-TR') + ' ₺' } }
  }).render();
</script>
```

---

## 13. Yapma Listesi DIŞINDAKİLER

- ❌ Veritabanı (EF Core, SQLite vs.)
- ❌ Dosya yükleme
- ❌ Email/SMS bildirim
- ❌ Ödeme takibi
- ❌ Multi-tenant
- ❌ Mobil özel sayfa (responsive yeterli)

Bunları README'nin "Roadmap" bölümünde sırala.

---

## 14. Yapma Sırası — Önerilen

1. `dotnet new mvc -n KiraTakip` ile projeyi oluştur. .NET 8 doğrula.
2. **`Models/`** — tüm sınıfları yaz.
3. **`Services/DummyDataService.cs`** — seed veriyle. Bu adımda durup veriyi gözden geçir.
4. **`Services/IstatistikService.cs`** + **`Helpers/FormatHelpers.cs`**.
5. **`Program.cs`** — DI kayıtları (`AddSingleton<DummyDataService>()`, `AddScoped<IstatistikService>()`).
6. **`_Layout.cshtml`** — Tailwind CDN + Google Fonts + Alpine + Lucide + custom CSS variables. Boş bir shell.
7. **Magic MCP'ye bağlan, Bölüm 7.1'i çalıştır** → sidebar+topbar üret → Razor'a port et → `_Sidebar.cshtml` ve `_Topbar.cshtml` partial'larına böl.
8. **Dashboard (Bölüm 7.2)** → port et → ApexCharts entegre et.
9. **Taşınmaz CRUD** (Bölüm 7.3, 7.4, 7.5) → form davranışları (Alpine).
10. **Kiracı CRUD** (Bölüm 7.6, 7.7, 7.8).
11. **Sözleşme listesi + ekleme** (Bölüm 7.9, 7.11).
12. **🌟 Sözleşme Detay — Kira Takip Kartı (Bölüm 7.10)** — bu sayfaya en az 2 saat ayır, mükemmelleştir.
13. **Empty states, loading states, error states** — Magic'e ayrı prompt'larla aldır.
14. **Smoke test:** Her sayfayı aç, link'leri tıkla, formları submit et. Console temiz olmalı, 404/500 olmasın.
15. **README.md** — kurulum, screenshot'lar, kullanım, roadmap.

---

## 15. Kabul Kriterleri (Done Definition)

- [ ] `dotnet run` ile sorunsuz açılıyor.
- [ ] Sidebar tüm sayfalarda sabit, aktif item highlight ediliyor, hover transitions akıcı.
- [ ] Dashboard'daki 5 KPI kartı, 2 liste ve area chart düzgün render oluyor.
- [ ] Teknokent A Blok detay → Birimler tab'ında 5 kat 5 farklı durumla görünüyor (boş, kiralı, süresi dolmak üzere karışık).
- [ ] Çamlık Kantini detay → tek "Komple" birim, kiralı durumda.
- [ ] Yeni taşınmaz formu: Tip=Bina seçilince KiralamaSekli ve KatSayisi alanları beliriyor (Alpine ile akıcı transition). Diğer tipler seçilince gizleniyor.
- [ ] Yeni sözleşme oluşturulduğunda dashboard'daki sayılar güncel.
- [ ] **Sözleşme detay sayfası vitrin gibi.** Hero kart, ticket-style notch, 3 metrik kartı, progress bar, tab'lar — hepsi premium hissi veriyor.
- [ ] Tüm para değerleri `1.250.000 ₺` formatında, `tabular-nums` ile hizalı.
- [ ] Tüm tarihler "12 Mar 2025" formatında.
- [ ] Hiçbir sayfa Bootstrap/jenerik MVC görünümünde değil. Magic ile üretilen estetik korunmuş.
- [ ] Empty state'ler düzgün (boş kiracı listesi, boş sözleşme listesi vb.).
- [ ] Form validation hataları zarif gösteriliyor (input altında küçük kırmızı yazı, focus ring kırmızıya dönüyor).
- [ ] Mobil/tablet'te sidebar drawer'a dönüyor (responsive).
- [ ] Tipografi tutarlı: başlıklar Fraunces, body Inter, sayılar JetBrains Mono.
- [ ] Hover'lar, focus ring'leri, transition'lar 200ms civarında, ani geçiş yok.

---

## 16. Magic MCP Kullanım Disiplini — Son Hatırlatma

- **Her sayfada Magic'i kullan.** Kendi başına Tailwind class'ı icat etme; Magic'in dönen kompozisyonlarını koru.
- **Birden fazla varyant iste:** Magic genelde alternatif sunabilir; iyisini seç.
- **Lucide ikonlarını kullan** — Magic'in default'u zaten bu, koru.
- **Tutarsız bileşen varsa** (örn. iki sayfada farklı stil "card") — Magic'e "match the style of [previous page]" diyerek tutarlılık talep et.
- **`logo_search` aracını dene:** Sidebar brand mark için 21st.dev logo arama.
- **Şüpheli durumda durup tasarım kararı sor:** Bir sayfada Magic önerisi domain ihtiyacıyla çelişiyorsa, kullanıcıya not düş ve devam et.

---

## 17. Authentication & Authorization

Authentication & Authorization artık kapsam dahilindedir. Detaylar: `docs/auth-spec.md`

---

## Bina ve Ofis Bazlı Kiralama

Bina türündeki taşınmazlarda kiralanabilir birimler kat değil, ofislerdir.

Bina + OfisBazli yapıda:
- Bina ana taşınmazdır.
- Ofisler binaya bağlı kiralanabilir birimlerdir.
- Kat bilgisi ayrı model değildir; ofis üzerinde `KatNo` alanı olarak tutulur.
- Kira sözleşmeleri ofislere bağlanır.

Detaylar için:

`docs/bina-ofis-birim-spec.md`

**Son not:** Bu proje bir teknoloji demosu olmaktan öte, **gerçek bir mülk yöneticisinin günlük olarak açıp keyifle kullanmak isteyeceği** bir araç gibi hissetmeli. Az ama mükemmel sayfa, çok ama yarım sayfadan iyidir. Acele etme, Magic'in gücünü tam kullan.
