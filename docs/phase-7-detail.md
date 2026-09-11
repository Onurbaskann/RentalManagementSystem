# Faz 7 — Çok Kalemli Aylık Tahakkuk

## Karar Özeti

1. **Pro-rata:** 30/30 standardı — tam aylar 1.0; kısmi aylar `gün / 30` (ay-bağımsız payda). Revizyon: 2026-05-14, önceki kural "gün / ay-gün-sayısı" idi.
2. **Regenerate:** Manuel "Yeniden Üret" butonu. Sözleşme başlangıcından sonraki ödenmemiş tahakkuklar için kullanıcıya sorulur.
3. **Backfill:** Yok — temiz seed (test verisi). Mevcut Tahakkuk/Odeme/BankaHareketi/Eslesme verileri silinir.
4. **Numaralandırma:** Faz 7 (Faz 5 ve 6 ayrı kalır).

## Veri Modeli

```
BorcTipi
  Id, Ad, Kod (KIRA/ORTAK/PORTAL), Aktif, Sira

Tarife
  Id, Yil, Aciklama, Aktif, OlusturmaTarihi
  └─ Kalemler: TarifeKalemi[]
       BorcTipiId, HesaplamaYontemi (M2/Sabit), BirimDeger, KdvOrani

BirimRate (opsiyonel override)
  BirimId, BorcTipiId, HesaplamaYontemi, BirimDeger, KdvOrani

SozlesmeRate (opsiyonel override)
  SozlesmeId, BorcTipiId, HesaplamaYontemi, BirimDeger, KdvOrani

KiraTahakkuk (mevcut)
  + Kalemler: TahakkukKalemi[]
  ToplamTutar/KdvTutari = Sum(Kalem.*) — DB'de tutulur

TahakkukKalemi (yeni)
  TahakkukId, BorcTipiId,
  Aciklama, HesaplamaYontemi, BirimDeger, Carpan,    -- snapshot
  Tutar, KdvOrani, KdvTutari, ToplamTutar,           -- snapshot
  KaynakTipi (Sozlesme/Birim/Tarife)                 -- debug için
```

## Resolve Sırası

Her ay × her aktif BorcTipi için:

1. SozlesmeRate var mı? → kullan
2. BirimRate var mı? → kullan
3. **TasinmazKiraciKategoriFiyat (Faz 9)** var mı? → kullan
4. Aktif Tarife (sözleşme başlangıç yılı) → TarifeKalemi → kullan
5. Yoksa: kalem oluşturulmaz (Faz 9 sonrası `Davranis=AylikSabit` ise 0₺ kalem üretilir)

Resolve sonucu kalemin içine snapshot olarak yazılır.

## Pro-rata Kuralı (30/30 Standardı)

> **Revizyon notu (2026-05-14):** Önceki kural "gün / ay-gün-sayısı" idi; Şubat tamamı gibi senaryolarda payda 28/29 olduğundan kısmi aylar arası eşit muamele bozuluyordu. Yeni kural: **payda her zaman 30**, tam ay erken-return ile 1.0 sabitlenir.

- Sözleşme bu ayın 1'inden son gününe kadar bütününü kapsıyorsa (`etkinBaslangic == donemIlkGunu && etkinBitis == ayBitis`) → katsayı `1.0` (ayın 28/29/30/31 olması fark etmez)
- Aksi halde → `katsayi = gunSayisi / 30m`; `gunSayisi = (etkinBitis - etkinBaslangic).Days + 1`
- Sigorta: `Math.Min(1.0m, katsayi)` — sözleşmenin ay-içi başlayıp ayın sonunu aşan teorik kenar durumları için (31 günlük ay tam kullanımı kısa-devre ile zaten yakalanır)
- `ayBitis` hesaplamasında `DateTime.DaysInMonth` kullanılmaya devam eder — bu yalnızca "ayın son tarih nesnesini" bulmak içindir, payda değildir
- Pro-rata KDV de orantılı kesilir
- `IlkAyTekSeferlik` davranışındaki kalemler (depozito vb.) pro-rata uygulanmadan tam tutarla üretilir — mevcut davranış korunur
- Yalnızca yeni üretilecek tahakkuklar etkilenir; geçmiş tahakkuklar dokunulmaz. Kullanıcı "Yeniden Üret" akışıyla manuel olarak yeni mantığa göre üretebilir.

## Üretim/Yeniden Üretim Davranışı

- **Sözleşme `Olustur`:** baştan-sona tüm aylar üretilir
- **Sözleşme `Uzat`:** yeni dönem için ek aylar üretilir (idempotent)
- **Sözleşme `Fesih`:** ödenmemiş gelecek tahakkuklar `İptalEdildi` durumuna alınır
- **Manuel "Yeniden Üret":** kullanıcı tetikler. Etki: belirtilen başlangıç tarihinden sonraki **ödenmemiş** tahakkuklar silinip resolve sırasıyla yeniden üretilir. Ödeme alınmış tahakkuklar dokunulmaz.

## Permission (Yeni)

- `BorcTipi.Manage`
- `Tarife.View`, `Tarife.Manage`
- `Birim.ManageRate`
- `Sozlesme.OverrideRate`
- `Tahakkuk.Regenerate`

## Görev Listesi

### 7.1 — BorcTipi entity + admin UI + seed
- Entity, DbSet, migration
- `/Admin/BorcTipi` CRUD (basit liste + ekle/düzenle/aktif-pasif/sıra)
- Seed: KIRA, ORTAK, PORTAL (Sira 1/2/3)
- Permission: `BorcTipi.Manage`

### 7.2 — Tarife + TarifeKalemi entity + admin UI
- 2 entity, migration, FK
- `/Admin/Tarife` — yıl listesi → seçilen yılın kalemleri grid'inde düzenleme
- "Yıl ekle" akışı: yeni yıl + opsiyonel "önceki yıldan kopyala" butonu
- Seed: cari yıl (DateTime.Now.Year) + tüm aktif BorcTipi'lere placeholder rate
- Permission: `Tarife.View`, `Tarife.Manage`

### 7.3 — BirimRate entity + Birim Detay editör
- Entity, migration, FK (Birim → BirimRate, BorcTipi → BirimRate)
- `/Tasinmaz/Detay/{id}` veya `/Birim/Edit` içinde "Özel Fiyatlar" bölümü
- Boş bırakılırsa tarife kullanılır (override yok)
- Permission: `Birim.ManageRate`

### 7.4 — SozlesmeRate entity + Sozlesme form override editör
- Entity, migration, FK
- `/Sozlesme/Olustur` ve `/Sozlesme/Detay` üzerinde "Pazarlık Fiyatları" bölümü
- Boş bırakılırsa BirimRate veya Tarife kullanılır
- Permission: `Sozlesme.OverrideRate`

### 7.5 — TahakkukKalemi entity + DB migration
- Entity, FK Tahakkuk ← Kalem
- Tahakkuk.ToplamTutar/KdvTutari computed (Sum)
- `KaynakTipi` enum: Sozlesme/Birim/Tarife (snapshot kullanılan kaynak)

### 7.6 — RateResolverService + TahakkukUretimService
- `IRateResolver.Resolve(sozlesme, borcTipi, donem)` → snapshot dict
- `ITahakkukUretim.UretSozlesmeIcin(sozlesme)` — başlangıç-bitiş arası tüm ayları üretir
- `ITahakkukUretim.YenidenUret(sozlesme, baslangicTarihi)` — ödenmemiş gelecek aylar
- Pro-rata hesaplama burada
- Idempotent: aynı dönem için 2. çağrı kayıt eklemez

### 7.7 — Mevcut Sözleşme akışlarını yeni servise bağlama
- `Sozlesme/Olustur` POST → `UretSozlesmeIcin` çağrısı
- `Sozlesme/Uzat` POST → yeni dönem için `UretSozlesmeIcin`
- `Sozlesme/Feshet` POST → ödenmemiş gelecek tahakkukları iptal et
- TÜFE artış flag'i kaldırılabilir (artık SozlesmeRate güncelleme + YenidenUret yapılır)

### 7.8 — Tahakkuk listesi + detay UI
- `/Tahakkuk/Index`'te kolonlar: Dönem | Kiracı | Toplam | Ödenen | Kalan | Durum
- Satıra tıklama → expand: kalem dökümü (Kira X + Ortak Y + Portal Z)
- `/Tahakkuk/Detay/{id}` — kalem listesi + KaynakTipi etiketi + ödeme geçmişi

### 7.9 — Manuel "Yeniden Üret" butonu
- Sozlesme detayında buton → modal: başlangıç tarihi seç + onay
- POST → `YenidenUret`
- TempData success mesajı + audit log (`SozlesmeIslemGecmisi`)
- Permission: `Tahakkuk.Regenerate`

### 7.10 — Temiz seed + test
- Mevcut `Tahakkuk/Odeme/BankaHareketi/BankaEslesme/TahakkukKalemi` tablolarını boşalt
- Yeni seed: BorcTipi (3 adet) + Tarife (cari yıl, placeholder rate'lerle)
- Test: yeni sözleşme aç → ay sayısı doğrulan, kalem dökümü doğrulan, override hiyerarşisi doğrulan, fesih davranışı doğrulan, pro-rata doğrulan

## Notlar

- Mevcut `KiraSozlesmesi.KiraBedeli` snapshot olarak bırakılır (raporlamada hızlı erişim için Sözleşme oluşturulurken Sum(ilk ay kalemleri) yazılır).
- Mevcut `KiraSozlesmesi.KdvUygulanacakMi/KdvOrani` artık fallback değil — kalem bazlı KDV önceliklidir. Bu alanlar UI'dan kaldırılabilir veya bilgi amaçlı kalabilir (Faz 7.2 sonrası karar).
- Banka eşleştirme akışı (Faz 5) **dokunulmaz** — Tahakkuk.ToplamTutar üstünden çalışmaya devam eder.