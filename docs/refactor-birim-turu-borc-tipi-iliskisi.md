# Refactor — BirimTuru × BorcTipi İlişkisi

> Amaç: `BirimTuru.BorcTipiId` FK kurarak rezervasyon tahakkuk akışını deterministik hale getirmek. Cascade pasifleştirme + ters yön engel.

## Kapsam Kararları

| Madde | Karar |
|---|---|
| `BirimTuru.BorcTipiId` (nullable FK) | ✅ Eklenir |
| Otomatik BorcTipi oluşturma (Create POST) | ❌ YOK — admin manuel seçer |
| Kod/Ad otomatik senkron (Edit POST) | ❌ YOK — Kod immutable, Ad bağımsız |
| `REZ_` prefix enforce | ❌ YOK — sadece convention |
| Cascade pasif (BirimTuru pasif → BorcTipi pasif) | ✅ + aktif tahakkuk/rezervasyon kontrolü |
| Ters engel (aktif BirimTuru'a bağlı BorcTipi pasif yapılamaz) | ✅ |
| `TransferToTahakkukAsync` FK kullanımı + fallback | ✅ |

**Tek doğruluk kaynağı:** `BirimTuru.BorcTipiId`. `BorcTipi.Davranis = RezervasyonOzel` sadece "tip filtresi" rolünde kalır.

---

## 1. Data Model

[KiraTakip/Models/BirimTuru.cs](KiraTakip/Models/BirimTuru.cs):
```csharp
public int? BorcTipiId { get; set; }
public BorcTipi? BorcTipi { get; set; }
```

[KiraTakip/Data/ApplicationDbContext.cs](KiraTakip/Data/ApplicationDbContext.cs) — `OnModelCreating`:
```csharp
modelBuilder.Entity<BirimTuru>()
    .HasOne(b => b.BorcTipi)
    .WithMany()
    .HasForeignKey(b => b.BorcTipiId)
    .OnDelete(DeleteBehavior.Restrict);
```

Migration: `AddBirimTuruBorcTipiFK` (sadece kolon + FK; veri backfill yok — seed halleder).

---

## 2. AdminBirimTuruController

[KiraTakip/Controllers/AdminBirimTuruController.cs](KiraTakip/Controllers/AdminBirimTuruController.cs)

**Create / Edit POST** kuralları:
- `RezervasyonYapilabilirMi == true` ise `BorcTipiId` **zorunlu** + seçilen kayıt `Davranis == RezervasyonOzel && Aktif` olmalı.
- `KiralanabilirMi == true` ise `BorcTipiId` zorla `null` set edilir (UI'den gelse bile).

**DurumDegistir POST** — pasif yaparken kontroller (sıra önemli):
1. Bu birim türüne bağlı `Birim`'ler aracılığıyla `KiraTahakkuk` (Durum ∈ {Bekleniyor, KismenOdendi}) varsa → engelle.
2. Aktif `ToplantiSalonuRezervasyon` (Durum = Planlandi) varsa → engelle.
3. Geçerse: `entity.Aktif = false`. **Bağlı `BorcTipi`** başka aktif BirimTuru tarafından kullanılmıyorsa cascade pasif yap.
4. Pasif → Aktif yönünde cascade yok (BorcTipi'yi admin elle aktifleştirir).

**Dropdown veri seti** (Create/Edit GET):
```csharp
ViewBag.BorcTipiAdaylari = await _ctx.BorcTipleri
    .Where(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif)
    .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
    .ToListAsync();
```

ViewModel/binding doğrudan `BirimTuru` üzerinden yürütülebilir (yeni VM şart değil).

---

## 3. AdminBorcTipiController

[KiraTakip/Controllers/AdminBorcTipiController.cs](KiraTakip/Controllers/AdminBorcTipiController.cs)

**DurumDegistir POST** — pasife çekme yolunda ek kontrol:
```csharp
if (entity.Aktif) {
    var bagliAktifBirimTuru = await _ctx.BirimTurleri
        .AnyAsync(b => b.BorcTipiId == id && b.Aktif);
    if (bagliAktifBirimTuru) {
        TempData["Error"] = "Bu borç tipi aktif bir birim türüne bağlı. Önce ilgili birim türünü pasif yapın.";
        return RedirectToAction(nameof(Index));
    }
}
```
`Sistem` flag kontrolü zaten var, korunur.

---

## 4. RezervasyonService

[KiraTakip/Services/RezervasyonService.cs:218-221](KiraTakip/Services/RezervasyonService.cs#L218-L221) yerine:

```csharp
var birimTuru = rezervasyon.Birim.BirimTuru;
BorcTipi? borcTipi = null;

if (birimTuru?.BorcTipiId is int btId)
    borcTipi = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Id == btId && b.Aktif);

// Fallback (geriye uyum — BirimTuru.BorcTipiId set edilmemiş eski kayıtlar)
borcTipi ??= await _ctx.BorcTipleri
    .FirstOrDefaultAsync(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif);

if (borcTipi == null)
    return (false, "Rezervasyon borç tipi bulunamadı. Lütfen yöneticinize başvurun.", null);
```

`GetByIdAsync` zaten `.Include(r => r.Birim).ThenInclude(b => b.Tasinmaz)` yapıyor; BirimTuru include'unu eklemek gerekir:
```csharp
.Include(r => r.Birim).ThenInclude(b => b.BirimTuru)
```
(GetByIdAsync ve GetAllAsync için.)

---

## 5. SeedDataService

[KiraTakip/Services/SeedDataService.cs](KiraTakip/Services/SeedDataService.cs)

**Sıra zaten doğru:** `SeedBorcTipleriAsync` (satır 21) → `SeedBirimTurleriAsync` (satır 87). Program.cs içindeki çağrı sırasını doğrula.

### 5.1 `SeedBirimTurleriAsync` güncellemesi

Mevcut `toAdd` listesinde rezervasyon türlerinin `BorcTipiId`'sini lookup'tan doldur:

```csharp
public async Task SeedBirimTurleriAsync()
{
    var existingCodes = await _ctx.BirimTurleri.Select(t => t.Kod).ToListAsync();

    // BorcTipi lookup'ları (Kod ile — Id sabit kabul edilmez)
    var toplantiBorcTipiId = await _ctx.BorcTipleri
        .Where(b => b.Kod == "TOPLANTI")
        .Select(b => (int?)b.Id)
        .FirstOrDefaultAsync();

    var toAdd = new List<BirimTuru>();
    if (!existingCodes.Contains("OFIS"))     toAdd.Add(new BirimTuru { Ad = "Ofis",            Kod = "OFIS",     Aktif = true, KiralanabilirMi = true,  RezervasyonYapilabilirMi = false, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow });
    if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BirimTuru { Ad = "Toplantı Salonu", Kod = "TOPLANTI", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 10, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = toplantiBorcTipiId });
    if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BirimTuru { Ad = "Etkinlik Alanı",  Kod = "ETKINLIK", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 11, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = toplantiBorcTipiId });
    if (!existingCodes.Contains("DIGER"))    toAdd.Add(new BirimTuru { Ad = "Diğer",           Kod = "DIGER",    Aktif = true, KiralanabilirMi = true,  RezervasyonYapilabilirMi = false, Sira = 99, OlusturmaTarihi = DateTime.UtcNow });

    if (toAdd.Any())
    {
        _ctx.BirimTurleri.AddRange(toAdd);
        await _ctx.SaveChangesAsync();
    }

    // Idempotent backfill: mevcut TOPLANTI/ETKINLIK kayıtlarında BorcTipiId null ise doldur
    if (toplantiBorcTipiId.HasValue)
    {
        await _ctx.BirimTurleri
            .Where(t => (t.Kod == "TOPLANTI" || t.Kod == "ETKINLIK") && t.BorcTipiId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.BorcTipiId, toplantiBorcTipiId));
    }
}
```

**Dikkat:**
- TOPLANTI ve ETKINLIK aynı borç tipini (`TOPLANTI`) paylaşır — bu kabuldür, sorun değil; cascade pasif kuralı "başka aktif kullanan var mı" kontrolü içerdiği için tek noktadan pasifleştirme bozulmaz.
- Kiralanabilir türlere (`OFIS`, `DIGER`) `BorcTipiId` set **edilmez** (null kalır).
- `existingCodes.Contains` kontrolü idempotent; ek `ExecuteUpdateAsync` bloğu yeniden çalıştırmada yeni eklenenlerin/önceden null kalanların FK'sını doldurur.

---

## 6. Migration ve Çalıştırma Sırası

1. `dotnet ef migrations add AddBirimTuruBorcTipiFK`
2. `dotnet ef database update` (kolon eklenir, tüm satırlar `BorcTipiId = NULL`)
3. Uygulama açılışında `SeedBorcTipleriAsync` → `SeedBirimTurleriAsync` çalışır; backfill bloğu eski kayıtların FK'sını doldurur.
4. `RezervasyonService` değişikliği fallback içerdiği için 2. ve 3. adım arasında ara duraklamada da çalışır.

---

## 7. Doğrulama Listesi

- [ ] Migration `BorcTipiId` kolonu + FK ile oluşturuldu.
- [ ] Mevcut DB'de TOPLANTI/ETKINLIK BirimTuru kayıtlarının `BorcTipiId` doldu (backfill).
- [ ] Rezervasyon türü için BorcTipi seçilmeden `BirimTuru` kaydedilemiyor.
- [ ] Kiralanabilir türde `BorcTipiId` null kalıyor.
- [ ] Aktif tahakkuk/rezervasyonu olan BirimTuru pasif yapılamıyor.
- [ ] BirimTuru pasif yapılınca bağlı BorcTipi başka aktif kullanım yoksa pasif oluyor.
- [ ] Aktif BirimTuru'na bağlı BorcTipi doğrudan pasif yapılamıyor (uyarı).
- [ ] `TransferToTahakkukAsync` doğru BorcTipi'yi seçiyor; fallback eski veride çalışıyor.
- [ ] `GetByIdAsync` / `GetAllAsync` BirimTuru include ediyor.

---

## 8. Dokunulan Dosyalar

| Dosya | Değişiklik |
|---|---|
| [Models/BirimTuru.cs](KiraTakip/Models/BirimTuru.cs) | `BorcTipiId` + nav property |
| [Data/ApplicationDbContext.cs](KiraTakip/Data/ApplicationDbContext.cs) | `OnModelCreating` FK |
| `Migrations/<ts>_AddBirimTuruBorcTipiFK.cs` | Yeni migration |
| [Controllers/AdminBirimTuruController.cs](KiraTakip/Controllers/AdminBirimTuruController.cs) | Create/Edit doğrulama + DurumDegistir cascade |
| [Controllers/AdminBorcTipiController.cs](KiraTakip/Controllers/AdminBorcTipiController.cs) | DurumDegistir ters engel |
| [Views/AdminBirimTuru/Create.cshtml](KiraTakip/Views/AdminBirimTuru/Create.cshtml) + [Edit.cshtml](KiraTakip/Views/AdminBirimTuru/Edit.cshtml) | BorcTipi dropdown (Alpine ile rezervasyon seçildiğinde göster) |
| [Services/RezervasyonService.cs](KiraTakip/Services/RezervasyonService.cs) | TransferToTahakkukAsync + Get*Async BirimTuru include |
| [Services/SeedDataService.cs](KiraTakip/Services/SeedDataService.cs) | `SeedBirimTurleriAsync` lookup + idempotent backfill |

---

## 9. Kapsam Dışı

- `BorcTipi.Kod` veya `Ad` değişikliği senkronizasyonu (geçmiş veri/raporlama riski).
- Otomatik BorcTipi üretimi (gizli yan etki).
- `REZ_` prefix enforce.
- View tarafında BirimTuru ↔ BorcTipi linki göstermek (opsiyonel — sonraki iyileştirme).
