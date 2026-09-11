# Tahakkuk Mimari Netleştirme Refactor — Implementation Spec

**Durum:** 🟡 Tasarım onaylandı, uygulamaya hazır (2026-06-30)

> Bu dosya **kontrat**tır. Spec dışına çıkma. Karar değişikliği için önce buraya yansıt.

---

## Amaç

Tahakkuk modelini netleştirmek:

1. **Kiracı + Birim çift merkezli yapı** — `Tahakkuk` mali sahiplik (Kiracı) ve operasyonel sahiplik (Birim) için ayrı zorunlu FK'lere sahip olur. Yetki/raporlama sorguları tek satıra iner.
2. **Tutarlı kaynak FK yönü** — Sözleşme tahakkuğunda `Tahakkuk.KiraSozlesmesiId` (mevcut) pattern'ine simetrik olarak `Tahakkuk.RezervasyonId?` eklenir; `Rezervasyon.TahakkukId` kaldırılır.
3. **Manuel borcun sözleşmeye opsiyonel bağı** — Manuel borç ayrı taahhüt olarak kalır; sözleşmeyle ilişkisi pür audit/raporlama bilgisidir, presentational değil.
4. **Birim bazlı yetki kapısı** — Mevcut `KullaniciYetkiKapsami` polymorphic tablosunun `KapsamTipi.Birim` enum değeri aktif edilir; iç ve kiracı kullanıcılar için tek mekanizma kalır.

---

## 🚫 NEGATİF LİSTE — KESİNLİKLE YAPMA

### Yapısal
- ❌ Yeni `KiraciUserBirimYetki` / `UserBirimYetki` tablosu — mevcut `KullaniciYetkiKapsami` yeterli (polymorphic, hem iç hem kiracı kullanıcı için)
- ❌ Rezervasyon için ayrı bir tahakkuk tablosu / polymorphic ayrım (tek `Tahakkuk` tablosu + `KaynakTipi` discriminator korunur)
- ❌ Manuel borç için ayrı entity (Tahakkuk satırı yeterli, `KaynakTipi == Manuel`)
- ❌ Rezervasyon → Sözleşme bağı (`Rezervasyon.SozlesmeId` vb.) — domain ayrımı net, KiraciId üzerinden yeterli
- ❌ "Sözleşme avantajı olarak rezervasyon hakkı" feature için tablo/alan eklemek (YAGNI, ileride ayrı feature)
- ❌ Sözleşme fesih sırasında bağlı manuel borçları otomatik iptal etmek
- ❌ Sözleşme yeniden üretim sırasında manuel borçlara dokunmak (`KaynakTipi == Sozlesme` filtresi korunur)

### Semantik
- ❌ Manuel borca KiraSozlesmesiId atandığında `KaynakTipi`'yi `Sozlesme` yapmak — KaynakTipi üretim yöntemini söyler, bağı değil; bağımsız iki bilgi
- ❌ `Tahakkuk.BirimId` ile `Tahakkuk.KiraSozlesmesi.BirimId` arasında genel bir DB constraint koymak — invariant tipine göre değişir (manuel borçta esnek)

### UI
- ❌ Sözleşme detayında manuel borçları liste olarak göstermek (sadece §4.3'teki opsiyonel rozet izin verilen tek istisna)
- ❌ Tahakkuk Sağlığı metriğine manuel borç dahil etme
- ❌ Manuel borç → sözleşme bağlama UI'ında "kiracının kendi sözleşmeleri" listesi dışında gezinme
- ❌ "Bağlı Sözleşme" sütunu için ayrı bir sayfa açmak — ManuelBorc/Index üzerinde filtre yeterli

### Naming
- ❌ `TahakkukKaynakTipi` enum değerlerini değiştirme (Sozlesme/Manuel/Rezervasyon kalır)
- ❌ `KullaniciYetkiKapsami` tablo / enum adlarını değiştirme

---

## §1. Karar Tablosu (17 Madde)

| # | Karar | Tarafı |
|---|-------|--------|
| 1 | Tahakkuk **Kiracı + Birim çift merkezli**, `BirimId` zorunlu | Schema |
| 2 | `Tahakkuk.RezervasyonId?` eklenir, `Rezervasyon.TahakkukId` kaldırılır | Schema |
| 3 | Manuel borç sözleşmeye bağlanabilir (`KiraSozlesmesiId` opsiyonel), birim zorunlu | Schema + Service |
| 4 | Sözleşme detayında manuel borçlar **hiç görünmez** (Tahakkuk Sağlığı ve Ödemeler tab sadece `KaynakTipi == Sozlesme`) | UI |
| 5 | Rezervasyon → Sözleşme bağı **yok**; KiraciId üzerinden yeterli | Domain |
| 6 | `KapsamTipi.Birim` enum değeri açılır — iç + kiracı aynı tablo (`KullaniciYetkiKapsami`) | Domain |
| 7 | **Permissive default** — yetki kapsamı tanımlı değilse tüm kiracı birimleri görünür | Service |
| 8 | Yetki kaynağı: iç kullanıcı (`Admin/Kiracilar/.../Davet`) + kiracı yöneticisi (`KiraciKullanici/Davet`); `Davetiye.BirimIds` (CSV) kolonu eklenir | Schema + UI |
| 9 | Manuel borç iptal: mevcut soft-cancel korunur (`Durum = IptalEdildi`, `IptalNotu`) | Service |
| 10 | İptal edilmiş bağlı manuel borç sözleşme detayında görünmez | UI |
| 11 | Sözleşme feshi bağlı manuel borçlara dokunmaz | Service |
| 12 | ManuelBorc/Index hepsini gösterir; "Bağlı Sözleşme" sütunu + "Bağlantı" filtresi + Durum filtre butonları + iptal arşiv | UI |
| 13 | Sözleşme detayında "Bu sözleşmeyle ilgili X manuel borç → ManuelBorc/Index" opsiyonel rozet | UI |
| 14 | Sözleşme tahakkuğu `BirimId` invariant **zorunlu** (kod set eder) | Service |
| 15 | Rezervasyon tahakkuğu `BirimId` invariant **zorunlu** (kod set eder) | Service |
| 16 | Manuel borç `BirimId` **esnek**; `KiraSozlesmesiId` set ise UI uyarı verir ama farklı birim seçimine izin verir | Service + UI |
| 17 | **Sıkı kural:** `Tahakkuk.KiraciId == Tahakkuk.KiraSozlesmesi.KiraciId` (hard validation, manuel borç sözleşmeye bağlandığında) | Service |

---

## §2. Schema Değişiklikleri

### §2.1 `Tahakkuk` Entity

```csharp
public class Tahakkuk : BaseEntity
{
    public int KiraciId { get; set; }
    public int BirimId { get; set; }                  // YENİ — zorunlu
    public int? KiraSozlesmesiId { get; set; }
    public int? RezervasyonId { get; set; }           // YENİ — opsiyonel
    // ... mevcut alanlar
    public Kiraci Kiraci { get; set; } = null!;
    public Birim Birim { get; set; } = null!;          // YENİ
    public Sozlesme? KiraSozlesmesi { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }     // YENİ
    // ... mevcut navigation
}
```

**FK kuralları:**
- `Tahakkuk.BirimId → Birimler.Id` — `OnDelete(Restrict)` (birim silinemez eğer tahakkuğu varsa)
- `Tahakkuk.RezervasyonId → Rezervasyonlar.Id` — `OnDelete(Restrict)`
- Mevcut `Tahakkuk.KiraSozlesmesiId` ilişkisi korunur

### §2.2 `Rezervasyon` Entity

```csharp
public class Rezervasyon : BaseEntity
{
    public int BirimId { get; set; }
    public int KiraciId { get; set; }
    // public int? TahakkukId { get; set; }  ← KALDIRILDI
    // public Tahakkuk? Tahakkuk { get; set; }  ← KALDIRILDI
    // ... mevcut alanlar
}
```

> Inverse navigation: `Tahakkuk.Rezervasyon` üzerinden ulaşılır. "Bu rezervasyon fatura edildi mi?" → `Rezervasyon.Durum == TahakkukaAktarildi` enum değeri korunur (zaten var).

### §2.3 `Davetiye` Entity

```csharp
public class Davetiye : BaseEntity
{
    // ... mevcut alanlar
    public bool TumTasinmazlaraErisim { get; set; } = true;  // Default DEĞİŞTİ → permissive
    public string? TasinmazIds { get; set; }
    public string? BirimIds { get; set; }            // YENİ — CSV, TasinmazIds ile aynı pattern
}
```

### §2.4 `KapsamTipi` Enum

```csharp
public enum KapsamTipi
{
    Tasinmaz = 1,
    Birim = 2   // ← yorum kaldırıldı, aktif
}
```

### §2.5 EF Core Index'leri

```
Tahakkuklar.BirimId            — index (yetki filtre performansı için kritik)
Tahakkuklar.RezervasyonId      — unique filtered (RezervasyonId IS NOT NULL) — 1 rezervasyon = 1 tahakkuk
```

---

## §3. Domain İnvariantları

### §3.1 Sözleşme Tahakkuğu (`KaynakTipi == Sozlesme`)
- `KiraSozlesmesiId NOT NULL`
- `RezervasyonId IS NULL`
- `BirimId == KiraSozlesmesi.BirimId` — **kod set eder, başka değer alamaz**
- `KiraciId == KiraSozlesmesi.KiraciId`

### §3.2 Rezervasyon Tahakkuğu (`KaynakTipi == Rezervasyon`)
- `RezervasyonId NOT NULL`
- `KiraSozlesmesiId IS NULL`
- `BirimId == Rezervasyon.BirimId` — **kod set eder, başka değer alamaz**
- `KiraciId == Rezervasyon.KiraciId`

### §3.3 Manuel Borç (`KaynakTipi == Manuel`)
- `RezervasyonId IS NULL`
- `BirimId NOT NULL` — kullanıcı seçer, zorunlu
- `KiraSozlesmesiId IS NULL veya NOT NULL` — opsiyonel
- Eğer `KiraSozlesmesiId NOT NULL`:
  - **Hard:** `KiraciId == KiraSozlesmesi.KiraciId` (validation, hata mesajı)
  - **Soft:** `BirimId == KiraSozlesmesi.BirimId` — farklıysa form banner uyarısı verir ama kullanıcı onaylayarak devam edebilir (§4.2)

### §3.4 Yeniden Üretim Güvenliği
- `TahakkukUretimService` mevcut `KaynakTipi == Sozlesme` filtresi korunur
- Manuel borç ve rezervasyon tahakkukları regenerate'ten **etkilenmez**

### §3.5 Fesih Bağımsızlığı
- `SozlesmeService.FeshetAsync` mevcut davranış korunur
- Bağlı manuel borçlar (`KiraSozlesmesiId == feshedilen.Id`) **dokunulmaz**
- Bağlı rezervasyon tahakkukları **dokunulmaz**

### §3.6 Tahakkuk Sağlığı Metriği (B kararı, B1 reddedildi)
- Sözleşme detay sayfası "Tahakkuk Sağlığı" hesabı **sadece** `KaynakTipi == Sozlesme` filtresiyle çalışır (mevcut `SozlesmeController` line 147/163/174 korunur)
- "Ek borç bilgi satırı" eklenmez

---

## §4. UI Davranışları

### §4.1 ManuelBorc/Index

**Filtre yapısı (Tahakkuk/Index pattern'i):**

```
[Tümü] [Bekliyor] [Kısmi] [Tam Ödendi] [Gecikti]                          ← durum butonları
                                                            [İptal Edildi sekmesi: gizli, "Görüntüle →" linki]

──────────────────────────────────────────────────
[Ara: kiracı, taşınmaz…]
[Taşınmaz ▼] [Birim ▼] [Kaynak: —] [Yıl ▼] [Bağlantı ▼] [Filtrele]      ← form satırı
                                              ↑ YENİ
──────────────────────────────────────────────────
```

**Bağlantı dropdown değerleri:**
- `—` (Hepsi)
- `sozlesmeli`
- `sozlesmesiz`

**Default davranış:**
- `durum=tum` veya `durum` boş → `Durum != IptalEdildi` olan kayıtlar
- `durum=iptal` → sadece iptal edilenler (arşiv görünümü, üst tarafta `@iptalEdildiSayisi iptal edilmiş kayıt varsayılan görünümde gizleniyor [Görüntüle →]` uyarısı)

**Yeni sütun:** "Bağlı Sözleşme"
- Sözleşmesiz: `—`
- Sözleşmeli: `#1234` (link → `/Sozlesme/Detay/1234`, `data-stop` ile satır click'i engellenir)

### §4.2 ManuelBorc/Ekle (Birim Uyarı UX'i)

```
[Kiracı seç ▼] → seçilince aktif sözleşmeleri yüklenir
[Sözleşme (opsiyonel) ▼]
   ↳ seçilince Birim alanı sözleşmenin birim'iyle ön-doldurulur (JS)
[Birim ▼] (zorunlu)
   ↳ kullanıcı değiştirirse, sözleşme seçilmişse banner:
     ⚠ Seçilen birim, sözleşmenin birim'inden farklı (sözleşmenin: B1).
        Bu doğru ise devam edebilirsiniz.
[Vade] [Tutar kalemleri]...
[Kaydet]
```

Banner Alpine.js ile reactive; sözleşme veya birim değişince değerlendirilir.

### §4.3 Sözleşme Detayı — Opsiyonel Rozet

Mevcut Tahakkuk Sağlığı kartının yanına veya altına küçük bir özet rozet:

```
🔗 Bu sözleşmeyle ilgili 2 manuel borç (1.500 ₺ kalan)  →
```

- Tıklayınca `/ManuelBorc?sozlesmeId={X}&durum=bekliyor` linkine gider
- İptal edilmiş manuel borçlar sayıma katılmaz
- Eğer bağlı manuel borç yoksa rozet gösterilmez (boş card kirletmesin)

### §4.4 Yetki Davet Formları

**Hem `Admin/Kiracilar/{id}/Kullanicilar/Davet` hem `KiraciKullanici/Davet`:**

```
☐ Tüm taşınmazlara erişim (varsayılan: ✓)
─────────────────
Taşınmaz seçimi (multi):
   ☐ Y. Plaza
   ☐ Z. Plaza
Birim seçimi (multi):                ← YENİ
   ☐ Ofis 5 / Y. Plaza
   ☐ Ofis 7 / Y. Plaza
```

Permissive default: `TumTasinmazlaraErisim = true` checked.

Davet kabul akışında (`DavetiyeService.KabulAsync`):
- `TasinmazIds` CSV → `KullaniciYetkiKapsami` kayıtları (`KapsamTipi = Tasinmaz`)
- `BirimIds` CSV → `KullaniciYetkiKapsami` kayıtları (`KapsamTipi = Birim`)

---

## §5. Fazlar (Uygulama Sırası)

### Faz 1 — Schema & Migration

1. `Tahakkuk` entity'sine `BirimId` (int, zorunlu) ve `RezervasyonId` (int?, opsiyonel) eklenir
2. Navigation property'ler eklenir (`Birim`, `Rezervasyon`)
3. `Rezervasyon.TahakkukId` ve navigation kaldırılır
4. `Davetiye.BirimIds` (string?) eklenir
5. `KapsamTipi.Birim = 2` enum değeri aktif edilir
6. `ApplicationDbContext.OnModelCreating`:
   - FK kuralları (`OnDelete(Restrict)`)
   - Index: `Tahakkuk.BirimId`, `Tahakkuk.RezervasyonId` (unique filtered)
7. EF Core migration: `TahakkukBirimMerkezli`
8. **Veri backfill (geliştirme aşaması — seed yeniden):**
   - Sözleşme tahakkukları: `BirimId = KiraSozlesmesi.BirimId`
   - Rezervasyon tahakkukları: `BirimId = Rezervasyon.BirimId`, `RezervasyonId = Rezervasyon.Id`
   - Manuel borçlar (mevcut): BirimId backfill yapılamaz (eski veride yok) → seed sıfırlanır
9. Migration uygulanır, DB doğrulanır

> **Not:** Production veri yok, dummy/seed aşamasındayız (CLAUDE.md uyarısı).

### Faz 2 — Domain Service Katmanı

1. `TahakkukUretimService`:
   - Sözleşme tahakkuğu üretirken `BirimId = s.BirimId` set
   - Mevcut `KaynakTipi == Sozlesme` filtresi korunur (regenerate güvenliği)
2. `RezervasyonService`:
   - Rezervasyon → Tahakkuk dönüşümünde:
     - `tahakkuk.BirimId = r.BirimId`
     - `tahakkuk.RezervasyonId = r.Id`
     - `tahakkuk.KaynakTipi = Rezervasyon`
   - `Rezervasyon.TahakkukId` referansları kaldırılır (inverse: `_ctx.Tahakkuklari.FirstOrDefault(t => t.RezervasyonId == r.Id)`)
3. `ManuelBorcService.CreateAsync`:
   - `BirimId` parametresi zorunlu hale getirilir (ViewModel + service signature)
   - `SozlesmeId` opsiyonel; set ise:
     - **Hard:** `KiraciId == sozlesme.KiraciId` (uyumsuzlukta hata döndür)
     - Birim farkı validation **YOK** (UI banner ile yönetilir, service izin verir)
4. `ManuelBorcService.CancelAsync`: değişiklik yok (mevcut soft-cancel)
5. `SozlesmeService.FeshetAsync`: değişiklik yok (bağlı tahakkuklara dokunmuyor zaten)
6. `TahakkukRepository`:
   - Yetki filtresi sadeleştirilir:
     ```csharp
     if (yetkiliBirimIds != null)
         q = q.Where(t => yetkiliBirimIds.Contains(t.BirimId));
     else if (yetkiliTasinmazIds != null)
         q = q.Where(t => yetkiliTasinmazIds.Contains(t.Birim.TasinmazId));
     ```
   - Rezervasyon için ayrı subquery (`_ctx.Rezervasyonlari.Any(...)`) **kaldırılır**
   - `GetManuelBorcListAsync` parametreleri genişletilir: `durum`, `baglanti`, `sozlesmeId?`
7. `YetkiKapsamiProvider`:
   - `ErisilebilirBirimIds` property eklenir
   - `KapsamTipi.Birim` kayıtlarını okur
   - `GlobalErisim` mantığı korunur (boşsa tüm)

### Faz 3 — UI: ManuelBorc Tarafı

1. **ManuelBorc/Ekle:**
   - Form yeniden düzenlenir: Kiracı → Sözleşme → Birim sırası
   - Alpine.js: sözleşme seçilince `birimId` ön doldur
   - Alpine.js: birim sözleşmenin birim'inden farklıysa banner reactive
2. **ManuelBorc/Index:**
   - Üst kısımda durum filtre butonları (Tahakkuk/Index pattern'i)
   - Form çubuğunda "Bağlantı" dropdown'u + Filtrele butonu
   - Default'ta iptal gizli, "Görüntüle →" linki ile arşiv açılır
   - Tabloya "Bağlı Sözleşme" sütunu (`<th>` ve `<td>` link ile)
3. **Controller:** `Index([FromQuery] string? durum, string? baglanti, int? sozlesmeId)`
4. **Repository:** `GetManuelBorcListAsync` filter parametreleri uygulanır + iptal sayısı döndürülür

### Faz 4 — UI: Sözleşme Detayı (Opsiyonel Rozet)

1. `Sozlesme/Detay` ve `KiraciSozlesme/Detay`'a Tahakkuk Sağlığı kartının altına küçük rozet:
   ```razor
   @if (Model.BagliManuelBorcSayisi > 0)
   {
       <a href="/ManuelBorc?sozlesmeId=@s.Id&durum=bekliyor"
          class="...">
           🔗 Bu sözleşmeyle ilgili @Model.BagliManuelBorcSayisi manuel borç
              (@Model.BagliManuelBorcKalan.ToString("N2") ₺ kalan) →
       </a>
   }
   ```
2. `SozlesmeDetayViewModel` veya kontroller'a `BagliManuelBorcSayisi` ve `BagliManuelBorcKalan` eklenir:
   ```csharp
   var bagli = await _ctx.Tahakkuklari
       .Where(t => t.KiraSozlesmesiId == id
                && t.KaynakTipi == TahakkukKaynakTipi.Manuel
                && t.Durum != TahakkukDurumu.IptalEdildi)
       .ToListAsync();
   vm.BagliManuelBorcSayisi = bagli.Count;
   vm.BagliManuelBorcKalan  = bagli.Sum(t => t.ToplamTutar - t.OdenenTutar);
   ```

### Faz 5 — Yetki Kapsamı (Birim UI'sı)

1. `KullaniciKapsami` admin ekranlarına Birim seçimi eklenir (mevcut Taşınmaz seçimi yanına)
2. `Davetiye.BirimIds` davet formlarına eklenir:
   - `Admin/Kiracilar/{id}/Kullanicilar/Davet`
   - `KiraciKullanici/Davet`
3. `DavetiyeService.KabulAsync` BirimIds → `KullaniciYetkiKapsami` (`KapsamTipi = Birim`)
4. `YetkiKapsamiProvider` Birim kapsamını okur ve filtrede uygular
5. Permissive default kontrolü:
   - İç kullanıcı: kapsam tanımlı değilse + GlobalErisim → tüm taşınmazlar/birimler
   - Kiracı kullanıcı: kapsam tanımlı değilse → kendi kiracısının tüm birimleri
6. UI ipuçları:
   - "Tüm taşınmazlara erişim" checkbox işaretli iken Taşınmaz/Birim seçim alanları disable

### Faz 6 — Test ve Doğrulama

Aşağıdaki senaryolar manuel test edilir (otomatik test borcu için ayrı görev):

#### Yetki Senaryoları
- [ ] Kiracı X, sadece A taşınmazı yetkilisi B taşınmazındaki tahakkukları görmez
- [ ] Kiracı X, sadece Birim 7 yetkilisi Birim 5 ve 12'nin tahakkuklarını görmez
- [ ] Manuel borç da yetki filtresine dahil (eskiden eksikti, şimdi dahil olmalı)
- [ ] Rezervasyon tahakkuğu yetki filtresine BirimId üzerinden dahil

#### Manuel Borç Senaryoları
- [ ] Sözleşmesiz manuel borç eklenebilir (BirimId zorunlu)
- [ ] Sözleşmeli manuel borç eklenirken aynı kiracı zorunlu (uyumsuzlukta hata)
- [ ] Sözleşmeli manuel borç eklenirken birim farklı → uyarı banner, onaylayınca devam
- [ ] İptal edilen manuel borç ManuelBorc/Index "iptal" sekmesinde görünür
- [ ] İptal edilen bağlı manuel borç sözleşme detayında görünmez (rozet sayısına da girmez)

#### Yeniden Üretim ve Fesih
- [ ] Sözleşme regenerate sonrası bağlı manuel borçlar etkilenmemiş
- [ ] Sözleşme fesih sonrası bağlı manuel borçlar aktif kalıyor

#### Sözleşme Detayı
- [ ] Tahakkuk Sağlığı sadece KaynakTipi==Sozlesme kayıtları üzerinden hesaplanıyor
- [ ] Ödemeler tab'ında manuel borç gözükmüyor
- [ ] Opsiyonel rozet sadece bağlı manuel borç varsa görünüyor
- [ ] Rozet sayısı doğru (iptal edilenler hariç)

#### ManuelBorc/Index
- [ ] Durum filtre butonları çalışıyor
- [ ] "Bağlantı" dropdown filtresi çalışıyor (Sözleşmeli/Sözleşmesiz/Hepsi)
- [ ] Default'ta iptal edilenler görünmüyor, "Görüntüle →" linki çalışıyor
- [ ] "Bağlı Sözleşme" sütunu doğru link veriyor

#### Davet ve Kapsam
- [ ] Davet formunda Birim seçilebiliyor (iç + kiracı yöneticisi)
- [ ] Davet kabul sonrası KullaniciYetkiKapsami kayıtları doğru (Tasinmaz + Birim)
- [ ] Permissive default: kapsam yok = tüm

---

## §6. Migration Notu

**Geliştirme aşaması:** Production verisi yok, dummy/seed ile çalışıyoruz (CLAUDE.md). Bu yüzden:

- Manuel borç backfill yapılamayacak (eski veride BirimId yok)
- Migration sonrası `SeedDataService` yeniden çalıştırılır, temiz veriyle başlanır
- EF Core migration script'i yine doğru olmalı (production gelişiminde gerekli olacak)

**Production sırası (bu refactor kapsamı dışında, not olarak):**

Production verisi olduğunda BirimId NOT NULL eklemek için 2 fazlı migration gerekir:
1. Faz 1: BirimId nullable eklenir, backfill script çalışır
2. Faz 2: Backfill tamamlanınca NOT NULL constraint eklenir
3. Manuel borçlar için backfill: ya KullanıcıYetkili manuel girer ya da "varsayılan birim" politikası

Bu detay şu an scope dışı.

---

## §7. Bu Refactor Kapsamı Dışındaki İşler

İleride değerlendirilebilir ama bu refactor'a girmiyor:

- **Sözleşme avantajı olarak rezervasyon hakkı** (ücretsiz X saat) — ayrı feature, ayrı tablo (`SozlesmeRezervasyonHakki`)
- **Manuel borca `IlgiliSozlesmeAciklamasi` (text)** — `KiraSozlesmesiId` FK kalır; text alternatifi tartışılmadı, FK yeterli
- **Rezervasyon → Tahakkuk silme politikası** (soft-delete cascade)
- **Otomatik test borcu** — bu refactor sonrası eklenebilir (xUnit ile yetki filtre testleri özellikle değerli)
- **"Tüm taşınmazları kapsayan ortak borç" senaryosu** (açılış bakiyesi vb.) — şu an Birim zorunlu, ileride "varsayılan/genel birim" politikası gerekirse ayrı tartışma
- **Birim bazlı yetki için Permission tipinde 2. seviye ek izinler** — şu an `KapsamTipi.Birim` veri kapsamı; permission spec'le çelişmiyor (kapsam ≠ permission)

---

## §8. Açık Tartışmadan Geçen Kararlar (Audit)

Bu kararlar tartışma sırasında değişti, son hali tablo §1'de:

| İlk Karar | Son Karar | Sebep |
|-----------|-----------|-------|
| Kiracı merkezli kalır | **Kiracı + Birim çift merkezli** | Yetki/operasyonel sorgular Birim üzerinden; manuel borç yetki filtresinde kayıp olmasın |
| Tahakkuk Sağlığı B1 (sözleşme metrik + ek borç satırı) | **B (saf):** sadece sözleşme kaynaklı, ek satır yok | Semantik ayrım: manuel borç sözleşmenin "kalemi" değil, "ilgili" taahhüt — presentational karışıklık yaratıyordu |
| Yeni `KiraciUserBirimYetki` tablosu | `KullaniciYetkiKapsami` (mevcut polymorphic) | `KapsamTipi.Birim` zaten "ileride" yorumuyla hazır; yeni tablo gereksiz |

---

## §9. Gelecek Notları

- **Faz N+1:** Manuel borç audit log iyileştirmesi — kim hangi sözleşmeye bağladı, hangi birim seçti, uyarıyı onayladı mı (analitik)
- Birim bazlı raporlama dashboardları — "Şu birimin yıllık geliri" doğrudan sorgulanabilir hale gelecek
- "Bağlı manuel borç" özet metrikleri üst düzeyde Kiracı/Detay'da da görüntülenebilir (mevcut tasarımda Kiracı/Detay'ı kontrol etmedik, fırsat olunca eklenir)

---

**Onay:** Tartışma tamamlandı (2026-06-30). Uygulama bekliyor.
