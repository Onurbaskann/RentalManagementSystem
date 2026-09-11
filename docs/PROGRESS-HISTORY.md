# PROGRESS HISTORY — Görev Takip Arşivi

> **ARŞİV — 2026-08-06:** Bu dosya geçmiş fazların ve tamamlanan çalışmaların
> tarihçesidir; artık aktif ilerleme kaydı olarak güncellenmez. Yeni oturumlarda
> bu dosyanın tamamını okuma. Güncel görev ve aktif faz için
> [`PROGRESS.md`](PROGRESS.md) dosyasını kullan.

> Sadece checkbox + kısa not. Tamamlanan görevler işaretlenir, açıklama eklenmez.

**Arşive alındığı andaki aktif faz:** Yok (Faz 2 tamamlandı — sonraki çalışma kullanıcı tarafından belirlenecekti)

---

## Faz 1 — Permission Modeli (Tasarım)

- [x] 1.1 — `UserPermission.cs` oluşturuldu
- [x] 1.2 — `Authorization/PermissionCatalog.cs` oluşturuldu
- [x] 1.3 — `IPermissionService.cs` tanımlandı
- [x] 1.4 — `PermissionService.cs` iskelet implementasyonu
- [x] 1.5 — `ApplicationDbContext`'e DbSet + unique index eklendi
- [x] 1.6 — `Program.cs` DI kaydı eklendi
- [x] 1.7 — Sanity check: C# derleme hatası sıfır (MSB dosya kilidi hatası app çalışıyor çünkü, CS hatası yok)

**Faz 1 tamamlandı:** ✅

---

## Faz 2 — SQL Server Tam Geçiş

- [x] 2.1 — Hazırlık: KiraTakipDb oluşturuldu (155.223.65.237)
- [x] 2.2 — NuGet: Sqlite kaldırıldı, SqlServer eklendi
- [x] 2.3 — `Program.cs` UseSqlServer'a geçildi
- [x] 2.4 — `appsettings.json` connection string güncellendi
- [x] 2.5 — SQLite migration'lar `_Migrations.SQLite.archive/` klasörüne taşındı (proje dışı)
- [x] 2.6 — Domain entity'leri (5 tablo) DbContext'e eklendi
- [x] 2.7 — `OnModelCreating`: FK'lar, precision'lar, unique index'ler
- [x] 2.8 — `InitialCreate` migration oluşturuldu (14 CreateTable)
- [x] 2.9 — DB'de 15 tablo oluşturuldu, doğrulandı
- [x] 2.10 — Identity seed: 3 rol + 3 user SQL Server'a yazıldı
- [x] 2.11 — Uygulama localhost:5031'de çalışıyor, SQL Server'a bağlı

**Faz 2 tamamlandı:** ✅

---

## Faz 3 — Domain Servislerinin EF Core'a Taşınması

- [x] 3.1 — Servis interface'leri tanımlandı
- [x] 3.2 — `SeedDataService` oluşturuldu
- [x] 3.3 — Servis implementasyonları (5 servis)
- [x] 3.4 — DI kayıtları
- [x] 3.5 — Controller'lar interface'lere bağlandı
- [x] 3.6 — Async dönüşüm
- [x] 3.7 — `DummyDataService.cs` silindi
- [x] 3.8 — Test senaryoları geçti

**Faz 3 tamamlandı:** ✅

---

## Faz 4 — Permission Implementasyonu

- [x] 4.1 — `PermissionService` tam implementasyon
- [x] 4.2 — `PermissionClaimsTransformer` (UserClaimsPrincipalFactory override, login'de claims yükleme)
- [x] 4.3 — `Program.cs` policy kayıtları (tüm PermissionCatalog.All için)
- [x] 4.4 — `AdminBypassHandler` (Admin rolü tüm policy'leri geçer)
- [x] 4.5 — Controller'lara `[Authorize(Policy = "...")]` eklendi (Tasinmaz, Kiraci, Sozlesme)
- [x] 4.6 — Admin UI permission checkbox ekranı (Edit.cshtml, rol bazlı gruplar)
- [x] 4.7 — Goruntuleyici rolü için sadece `*.View` aktif kuralı (server + UI)
- [x] 4.8 — Test: 3 rol için permission akışı (Faz 12.1 + 12.2'de tamamlandı)

**Faz 4 tamamlandı:** ✅

---

## Faz 5 — Ödeme Takip Modülü

- [x] 5.1 — Açık sorular karara bağlandı (`phase-5-detail.md` oluşturuldu)
- [x] 5.2 — Domain entity'leri + enum'lar
- [x] 5.3 — DbContext + migration
- [x] 5.4 — Servis interface ve implementasyonları
- [x] 5.5 — `IBankaHareketiProvider` + CSV implementasyonu
- [x] 5.6 — Controller + view'lar (eşleştirme UI tamamlandı)
- [x] 5.7 — Dashboard ödeme KPI'ları
- [x] 5.8 — Raporlama
- [x] 5.9 — Test (eşleştirme akışı) (Faz 12.3'te tamamlandı)

**Faz 5 tamamlandı:** ✅

---

## Faz 6 — Tablo & UX İyileştirmeleri

> Önkoşul: Faz 5.9 testi tamamlanmalı. Detay: `phase-6-detail.md`.

- [x] 6.1 — Ortak altyapı (partial'lar + Alpine bileşenleri + site.js)
- [x] 6.2 — Server-side pagination/filter (Odeme, Tahakkuk, BankaHareketi)
- [x] 6.3 — Client-side pagination/filter (Kiraci, Sozlesme, AdminUser)
- [x] 6.4 — Tooltip uygulaması (uyum, durum, eşleşme, çöz)
- [x] 6.5 — Mobil sidebar + confirm modal (data-confirm dönüşümü dahil)
- [x] 6.6 — (Opsiyonel) Export + bulk actions + empty state (Faz 12.8'de değerlendirildi: empty state temiz, export/bulk opsiyonel kapsam dışı)

**Faz 6 tamamlandı:** ✅

---

## Faz 7 — Çok Kalemli Aylık Tahakkuk

> Detay: `phase-7-detail.md`. Karar: pro-rata hibrit, manuel regenerate, temiz seed.

- [x] 7.1 — `BorcTipi` entity + admin UI + seed (KIRA/ORTAK/PORTAL)
- [x] 7.2 — `Tarife` + `TarifeKalemi` entity + admin UI (yıllık)
- [x] 7.3 — `BirimRate` entity + Birim detay editör
- [x] 7.4 — `SozlesmeRate` entity + Sozlesme form override editör
- [x] 7.5 — `TahakkukKalemi` entity + DB migration
- [x] 7.6 — `RateResolverService` + `TahakkukUretimService` (pro-rata + idempotent)
- [x] 7.7 — Sozlesme Olustur/Uzat/Feshet akışlarını yeni servise bağla
- [x] 7.8 — Tahakkuk listesi + detayda kalem dökümü UI
- [x] 7.9 — Manuel "Yeniden Üret" butonu + audit log
- [x] 7.10 — Temiz seed + test (sözleşme aç → 12 ay × N kalem doğrulan)

**Faz 7 tamamlandı:** ✅

---

## Faz 8 — Parametre Yönetimi, Rezervasyon ve Manuel Borç

> Detay: `phase-8-parametre-rezervasyon-ve-manuel-borc.md`. Alt görevler 8.1 → 8.8 arası.

### 8.1 Parametre Altyapısı
- [x] 8.1.1 — `BirimTuru` entity + DbSet + migration
- [x] 8.1.2 — `KiraciKategori` entity + DbSet + migration
- [x] 8.1.3 — `Sektor` entity + DbSet + migration
- [x] 8.1.4 — Admin parametre ekranları (BirimTuru / KiraciKategori / Sektor)
- [x] 8.1.5 — Seed verileri (6 birim türü, 5 kategori, 8 sektör)
- [x] 8.1.6 — `TasinmazTipi` entity + DbSet + admin ekranı + seed (Bina/Arazi/Tarla/Depo/Otomat/Bankamatik/Kantin/Diğer)
- [x] 8.1.7 — `Tasinmaz.Tipi` enum → `TasinmazTipiId` FK veri migrasyonu; enum + eski `Tipi` kolonu kaldırılması

### 8.2 Birim ve Kiracı Model Genişletmeleri
- [x] 8.2.1 — `Birim.BirimTuruId` (nullable FK)
- [x] 8.2.2 — `Kiraci.KiraciKategoriId` (nullable FK)
- [x] 8.2.3 — `Kiraci.SektorId` (nullable FK)
- [x] 8.2.4 — Kiracı formuna kategori + sektör selectbox
- [x] 8.2.5 — Birim formuna birim türü selectbox
- [x] 8.2.6 — `BirimTuru.KiralanabilirMi` + `RezervasyonYapilabilirMi` alanları + DB migration + seed güncelleme (OTOMAT/BANKAMATIK/DEPO BirimTuru'dan kaldırılır)
- [x] 8.2.7 — Taşınmaz Ekle formunda "Ofis Bazlı" → "Birim Bazlı"; `KiralamaSekli.OfisBazli` → `BirimBazli` rename; Rezervasyon Alanları bölümü UI

### 8.3 Taşınmaz Kategori Çarpanları
- [x] 8.3.1 — `TasinmazKategoriCarpan` entity + unique index
- [x] 8.3.2 — Taşınmaz detayında çarpan yönetimi UI
- [x] 8.3.3 — `RateResolverService` içine `TasinmazKategoriCarpan` kaynağı
- [x] 8.3.4 — Snapshot alanları (HesaplamaYontemi=M2, Carpan, KaynakTipi)

### 8.4 Manuel Borç
- [x] 8.4.1 — `TahakkukKaynakTipi` enum + `KiraTahakkuk.KaynakTipi`
- [x] 8.4.2 — `IManuelBorcService` + implementasyon
- [x] 8.4.3 — Manuel borç oluşturma / listeme / iptal UI
- [x] 8.4.4 — Mevcut ödeme akışıyla uyum testi

### 8.5 Toplantı Salonu Rezervasyonu
- [x] 8.5.1 — `ToplantiSalonuRezervasyon` entity + `RezervasyonDurumu` enum
- [x] 8.5.2 — `RezervasyonUcretKural` entity + admin ekranı
- [x] 8.5.3 — `IRezervasyonService` + ücret hesaplama (`HesaplaAsync`)
- [x] 8.5.4 — Çakışma kontrolü
- [x] 8.5.5 — Rezervasyon oluşturma / listeme / iptal UI

### 8.6 Rezervasyon → Tahakkuk Entegrasyonu
- [x] 8.6.1 — `BorcTipi` seed: TOPLANTI (manuel kayıt için MANUEL kodu opsiyonel)
- [x] 8.6.2 — `RezervasyonService.TransferToTahakkukAsync`
- [x] 8.6.3 — `KiraTahakkuk.KiraSozlesmesiId` nullable + `KiraOdeme.KiraSozlesmesiId` nullable + migration `MakeKiraSozlesmesiIdNullable`
- [x] 8.6.4 — İptal/ödeme durum kontrolleri (CancelAsync: TahakkukaAktarildi durumunda bağlı tahakkuk da iptal edildi)

### 8.7 Permission ve UI Entegrasyonu
- [x] 8.7.1 — `permission-catalog.md` Faz 8 permission’ları
- [x] 8.7.2 — `Authorization/PermissionCatalog.cs` senkronizasyonu
- [x] 8.7.3 — Controller policy attribute’ları
- [x] 8.7.4 — Buton görünürlüğü (UI permission helper)
- [x] 8.7.5 — Goruntuleyici kapsam filtreleri (servis seviyesinde)

### 8.8 Dashboard / Raporlama / Smoke Test
- [x] 8.8.1 — Tahakkuk listesinde KaynakTipi kolonu (Otomatik/Manuel/Rezervasyon)
- [x] 8.8.2 — Dashboard: manuel borç + rezervasyon metrikleri
- [x] 8.8.3 — Smoke test (kabul kriterleri 26. bölüm)

**Faz 8 tamamlandı:** ✅

---

## Faz 9 — Taşınmaz × Kiracı Kategorisi Bazlı Dinamik Fiyatlandırma

> Detay: `phase-9-fiyatlandirma-mimarisi.md`. Alt görevler 9.1 → 9.7 arası.

### 9.1 BorcTipi Davranış Enum'u
- [x] 9.1.1 — `BorcTipiDavranisi` enum tanımı
- [x] 9.1.2 — `BorcTipi.Davranis` kolonu, `TekSeferlikMi` kaldırılır
- [x] 9.1.3 — Veri migrasyonu (TekSeferlikMi → Davranis dönüşümü)
- [x] 9.1.4 — Migration: `ReplaceTekSeferlikWithDavranis`
- [x] 9.1.5 — `SeedDataService.SeedBorcTipleriAsync` güncelleme
- [x] 9.1.6 — `TahakkukUretimService` filtresi `AylikSabit`'e
- [x] 9.1.7 — Depozito bloğu `IlkAyTekSeferlik`'e

### 9.2 Fiyat Matrisi Tablosu
- [x] 9.2.1 — `TasinmazCarpanController` + view'ları silinir
- [x] 9.2.2 — `TasinmazKiraciKategoriFiyat` entity
- [x] 9.2.3 — Migration: `ReplaceTasinmazKategoriCarpanWithFiyat`
- [x] 9.2.4 — DbContext DbSet + `OnModelCreating`

### 9.3 Resolver Güncellemesi
- [x] 9.3.1 — Yeni precedence (Sozlesme → Birim → Fiyat → Tarife)
- [x] 9.3.2 — Eski KIRA-only blok kaldırılır
- [x] 9.3.3 — `KaynakTipi` enum rename
- [x] 9.3.4 — Null davranışı korunur

### 9.4 Üretim Servisi — Composer Pattern
- [x] 9.4.1 — `TahakkukKalemiPreview` DTO
- [x] 9.4.2 — `ComposeKalemlerAsync` metodu
- [x] 9.4.3 — `AylikSabit` + null → 0₺ kalem üretimi
- [x] 9.4.4 — `UretSozlesmeIcinAsync` composer'a delege olur
- [x] 9.4.5 — Pro-rata composer içinde

### 9.5 Taşınmaz "Parametreler" Sekmesi
- [x] 9.5.1 — `TasinmazFiyatController`
- [x] 9.5.2 — Detay sayfasına "Parametreler" sekmesi
- [x] 9.5.3 — Dinamik grid (Kategori × BorcTipi)
- [x] 9.5.4 — Hücre input'ları (BirimDeger / Yöntem / KDV)
- [x] 9.5.5 — Bulk save
- [x] 9.5.6 — Permission `Tasinmaz.Edit`

### 9.6 Sözleşme Ekle — Otomatik Doldurma
- [x] 9.6.1 — ViewModel `SozlesmeKalemInputDto` ve `SozlesmeKalemleri` listesi
- [x] 9.6.2 — JSON endpoint `GetVarsayilanKalemler`
- [x] 9.6.3 — Alpine bileşeni (watcher + fetch)
- [x] 9.6.4 — Kalem input + Otomatik/Override rozet
- [x] 9.6.5 — Sıfırla butonu
- [x] 9.6.6 — Override → `SozlesmeRate` kaydı

### 9.7 Test ve Doğrulama
- [x] 9.7.1 — Smoke: matris + 12 ay tahakkuk
- [x] 9.7.2 — 0₺ kalem testi
- [x] 9.7.3 — Override testi
- [x] 9.7.4 — Manuel/Toplantı izolasyon testi
- [x] 9.7.5 — Depozito ilk ay testi

**Faz 9 tamamlandı:** [x]

---

## Faz 10 — Taşınmaz Detayı UX İyileştirme

- [x] 10.1 — `BirimTuru` Create/Edit POST: XOR validasyonu (Kiralanabilir ⊕ Rezervasyon)
- [x] 10.2 — `Views/AdminBirimTuru/Create.cshtml` + `Edit.cshtml`: iki kutu karşılıklı toggle (radio davranışı)
- [x] 10.3 — Mevcut BirimTuru kayıtlarını gözden geçir (her ikisi true/false var mı)
- [x] 10.4 — `Views/Tasinmaz/Detay.cshtml`: kat etiketi helper (Zemin/Bodrum/N. Kat)
- [x] 10.5 — Birimler sekmesi: Alpine accordion (default kapalı)
- [x] 10.6 — Filtre değişiminde ilgili katların otomatik açılması ("Boş" vb.)
- [x] 10.7 — Tümünü Aç / Tümünü Kapat toggle butonları
- [x] 10.8 — `TasinmazController.Detay`: `.Include(b => b.BirimTuru)` zaten var + RezKural ViewModel alanı + controller populate
- [x] 10.9 — Detay tablosu: rezervasyon birimlerinde "Rezervasyon Yap" butonu
- [x] 10.10 — Detay tablosu: Kira Bedeli sütununda rezervasyon birimi için "X ₺ / Y dk"
- [x] 10.11 — `BirimOzelFiyatViewModel` genişletildi (KiralanabilirMi, RezervasyonYapilabilirMi, OzelRezervasyonKural, GlobalRezervasyonKural)
- [x] 10.12 — `BirimController.OzelFiyat`: senaryo dallanması + ManuelTetiklemeli filtresi + RezKuralKaydet/Sifirla POST'ları
- [x] 10.13 — `Views/Birim/OzelFiyat.cshtml`: senaryo A (kira) — mevcut tablo korundu
- [x] 10.14 — `Views/Birim/OzelFiyat.cshtml`: senaryo B (rezervasyon) — RezervasyonUcretKural formu; senaryo C — hibrit hata kartı
- [x] 10.15 — End-to-end test: statik kod incelemesi + C# sıfır hata; 5 senaryo doğrulandı (hibrit XOR reddi, accordion default kapalı + filtre, Rezervasyon Yap butonu, OzelFiyat A/B/C dallanması, ManuelTetiklemeli filtresi)

**Faz 10 tamamlandı:** ✅

---

## Faz 11 — Tarife Hiyerarşisi + Parent Bilgi Gösterimi

> Detay: `phase-11-tarife-hiyerarsisi-ve-parent-gosterim.md`. Resolver precedence Faz 9'dan korunur; Tarife katmanına KiraciKategoriId eklenir.

### 11.1 Şema — TarifeKalemi.KiraciKategoriId
- [x] 11.1.1 — `TarifeKalemi` model + nav property
- [x] 11.1.2 — `OnModelCreating` unique index `(TarifeId, KiraciKategoriId, BorcTipiId)`
- [x] 11.1.3 — Migration `AddKiraciKategoriToTarifeKalemi`
- [x] 11.1.4 — `SeedDataService.SeedTarifeAsync` kategori × borç tipi matrisi

### 11.2 Admin/Tarife Matris UI
- [x] 11.2.1 — Controller GET/POST kategori × borç tipi matris
- [x] 11.2.2 — View matris tablosu (TasinmazFiyat pattern)
- [x] 11.2.3 — Bulk save

### 11.3 Resolver — Tarife Kategori-Aware
- [x] 11.3.1 — Tarife adımı `(Yil, KiraciKategoriId, BorcTipiId)` filtresi
- [x] 11.3.2 — Null davranışı korunur (composer 0₺ üretir)

### 11.4 Global Rename — "Genel Tarife"
- [x] 11.4.1 — `Views/Birim/OzelFiyat.cshtml` rezervasyon kart başlığı
- [x] 11.4.2 — `Views/Tasinmaz/Detay.cshtml` global rozeti
- [x] 11.4.3 — Grep ile kalan "Global Kural" / "(Varsayılan)" temizlik

### 11.5 Ortak Partial + Helper Servis
- [x] 11.5.1 — `ParentTarifeKartViewModel` + `ParentTarifeSatir`
- [x] 11.5.2 — `Views/Shared/_ParentTarifeKart.cshtml`
- [x] 11.5.3 — `ITarifeHiyerarsiService.GetParentForAsync`
- [x] 11.5.4 — `TarifeHiyerarsiService` implementasyon + DI kaydı

### 11.6 Tasinmaz/Ekle Parent Kart
- [x] 11.6.1 — Controller GET parent doldur
- [x] 11.6.2 — View partial render

### 11.7 Birim/OzelFiyat Parent Kart
- [x] 11.7.1 — Controller GET parent doldur
- [x] 11.7.2 — View partial (senaryo A — kira)
- [x] 11.7.3 — Senaryo B — sadece rename

### 11.8 Sozlesme/Detay Parent Kart
- [x] 11.8.1 — Controller Detay parent doldur
- [x] 11.8.2 — Pazarlık Fiyatları sekmesinde partial

### 11.9 Test ve Doğrulama
- [x] 11.9.1 — Migration + seed temiz çalışır
- [x] 11.9.2 — Resolver 4 katman fallback testi
- [x] 11.9.3 — 3 ekranda parent kart doğru
- [x] 11.9.4 — "Global Kural (Varsayılan)" hiçbir view'da yok
- [x] 11.9.5 — Cari yıl tarifesi yok senaryosu

**Faz 11 tamamlandı:** ✅

---

## Ara Refactor — BorcTipiDavranisi Ayrıştırma

> Dosya: `phase-9-1-borc-tipi-davranisi-refactor.md`

- [x] `BorcTipiDavranisi.ManuelTetiklemeli` ayrıştırıldı
- [x] `KullaniciManuel = 3` olarak int değer korunarak rename edildi
- [x] `RezervasyonOzel = 4` eklendi
- [x] `MANUEL` seed davranışı `KullaniciManuel` olarak güncellendi
- [x] `TOPLANTI` seed davranışı `RezervasyonOzel`, `Sistem=true` olarak güncellendi
- [x] Migration SQL ile mevcut `TOPLANTI` kayıtları dönüştürüldü
- [x] Otomatik tahakkuk/tarife/fiyat filtreleri `KullaniciManuel` ve `RezervasyonOzel` dışlayacak şekilde güncellendi
- [x] Manuel borç akışı yalnızca `KullaniciManuel` borç tiplerini kullanacak şekilde güncellendi
- [x] Rezervasyon akışı `Kod == "TOPLANTI"` bağımlılığından çıkarıldı
- [x] Admin Borç Tipi view metinleri güncellendi

**Ara refactor tamamlandı:** ✅

---

## Faz 12 — Stabilizasyon ve Test Borcu Temizliği

> Detay: `phase-12-stabilizasyon-ve-test-borcu.md`

### 12.1 Permission ve Rol Akışı Testi
- [x] 12.1 — Permission ve rol akışı testleri

### 12.2 Görüntüleyici Kapsam Filtresi Regresyonu
- [x] 12.2 — Görüntüleyici kapsam filtresi regresyon testleri (2 hata düzeltildi: KiraciDetay sozlesme scope leak, TahakkukIndex dropdown scope)

### 12.3 Ödeme ve Banka Eşleştirme Testleri
- [x] 12.3 — Ödeme, dekont, banka import ve mutabakat testleri (1 hata düzeltildi: OdemeController.GetYetkiliTasinmazIdsAsync ödeme listesinden değil UserTasinmazYetkiService'den ID okuyor)

### 12.4 Tahakkuk Üretimi ve Yeniden Üretim Testleri
- [x] 12.4 — Tahakkuk üretimi, pro-rata ve yeniden üretim testleri (1 hata düzeltildi: TahakkukUretimService.YenidenUretAsync KismenOdendi tahakkukları da silmeye çalışıyordu; KiraOdeme→KiraTahakkuk Restrict FK nedeniyle exception fırlatırdı; fix: !_ctx.KiraOdemeler.Any(o => o.KiraTahakkukId == t.Id) koşulu eklendi)

### 12.5 Fiyatlandırma Hiyerarşisi ve Resolver Testleri
- [x] 12.5 — Resolver precedence ve fallback testleri (1 hata düzeltildi: RateResolverService BirimRate sorgusunda KiraciKategoriId filtresi eksikti; unique index (BirimId, KiraciKategoriId, BorcTipiId) olmasına rağmen sorgu kategori filtresi içermiyordu; fix: kategoriId çözümlemesi BirimRate sorgusundan önceye taşındı, r.KiraciKategoriId == kategoriId.Value filtresi eklendi)

### 12.6 BorcTipiDavranisi Refactor Regresyon Testi
- [x] 12.6 — Borç tipi davranışı ayrıştırma regresyon testleri (hata bulunamadı: ManuelTetiklemeli yalnızca migration yorumunda; KullaniciManuel/RezervasyonOzel tüm filtreler tutarlı; rezervasyon akışı Kod hard-code yok; otomatik tahakkuk allowlist AylikSabit+IlkAyTekSeferlik; admin UI 4 etiket doğru)

### 12.7 Rezervasyon ve Manuel Borç Testleri
- [x] 12.7 — Rezervasyon ve manuel borç uçtan uca testleri (1 hata düzeltildi: ManuelBorc oluşturma notu IptalNotu alanına yazılıyordu ama view yalnızca IptalEdildi durumunda gösteriyordu; iptal sırasında da üzerine yazılıyordu; fix: Index.cshtml'de durum koşulu kaldırıldı, CancelAsync'de mevcut nota append ediyor)

### 12.8 UI / UX Stabilizasyonu
- [x] 12.8 — UI/UX stabilizasyon kontrolleri (2 hata düzeltildi: Odeme/Detay.cshtml "Onayla" formu data-confirm eksikti; Rezervasyon/Index.cshtml "Tahakkuka Aktar" native confirm() yerine Alpine confirm'e yükseltildi; sidebar/tooltip/pagination/empty-state/Bootstrap kalıntısı — temiz)

### 12.9 Dokümantasyon ve Terminoloji Temizliği
- [x] 12.9 — Dokümantasyon ve terminoloji kontrolleri (3 hata düzeltildi: permission-catalog.md 6 eksik permission eklendi + TasinmazFiyat.* düzeltildi; Tasinmaz/Detay.cshtml isOfisBazli→isBirimBazli rename; phase-10/11 ManuelTetiklemeli GÜNCELLİK NOTU eklendi)

### 12.10 Final Smoke Test
- [x] 12.10 — Final smoke test (statik: build temiz, ManuelTetiklemeli/OfisBazli/Global Kural sıfır, tüm DI kayıtları ve enum filtreleri tutarlı; manuel browser testleri kullanıcı onaylı ✅)

**Faz 12 tamamlandı:** ✅

---

## Ara Refactor — Rezervasyon Yıllık Genel Tarifeleri

> Dosya: `refactor-rezervasyon-yillik-genel-tarife.md`. Kapsam: yeni `RezervasyonGenelTarife` entity'si (`Tarife.Yil × BirimTuru`); `HesaplaAsync` precedence; eski global kuralın backfill ile dönüştürülmesi.

- [x] `RezervasyonGenelTarife` entity + DbSet + `OnModelCreating` (unique index `TarifeId, BirimTuruId`)
- [x] Migration `AddRezervasyonGenelTarife`
- [x] `TarifeViewModels`: `TarifeMatrisRezervasyonSatir` + VM ek alanları (`RezervasyonSatirlari`, `RezervasyonHucreler`)
- [x] `ParentRezervasyonTarifeKartViewModel` + `ParentRezervasyonTarifeSatir`
- [x] `AdminTarifeController.Detay` GET: rezervasyon satırları yükle
- [x] `AdminTarifeController.KalemGuncelle` POST: rezervasyon bloğunu işle
- [x] `AdminTarifeController.YilEkle` POST: rezervasyon kayıtlarını da kopyala / default oluştur
- [x] `Views/AdminTarife/Detay.cshtml`: "Rezervasyon Genel Tarifeleri" tablosu
- [x] `Views/Shared/_ParentRezervasyonTarifeKart.cshtml` (yeni partial)
- [x] `ITarifeHiyerarsiService.GetRezervasyonParentForAsync` + implementasyon
- [x] `TasinmazViewModels.TasinmazEkleViewModel.ParentRezervasyonTarife` alanı
- [x] `TasinmazController.Ekle` GET: parent kart doldur
- [x] `Views/Tasinmaz/Ekle.cshtml`: rezervasyon alanları üstüne partial render
- [x] `RezervasyonService.HesaplaAsync`: precedence güncellemesi (birim → birim.BirimTuru cari yıl → hata)
- [x] `SeedDataService.EnsureVarsayilanRezervasyonGenelTarifeAsync` + `BackfillEskiGlobalRezervasyonKuralAsync`
- [x] `Program.cs`: seed çağrı sırası güncellemesi (eski `EnsureVarsayilanRezervasyonUcretKuralAsync` çıkarılır)
- [x] Doğrulama listesi (refactor dosyası bölüm 10) geçti

**Ara refactor tamamlandı:** [x]

---

## Ara Refactor — BirimTuru × BorcTipi İlişkisi

> Dosya: `refactor-birim-turu-borc-tipi-iliskisi.md`. Kapsam: `BirimTuru.BorcTipiId` FK + cascade pasifleştirme + `TransferToTahakkukAsync` deterministik seçim.

- [x] `BirimTuru.BorcTipiId` (nullable FK) + nav property
- [x] `ApplicationDbContext.OnModelCreating` FK (`OnDelete.Restrict`)
- [x] Migration `AddBirimTuruBorcTipiFK`
- [x] `AdminBirimTuruController` Create/Edit: RezervasyonYapilabilirMi=true ise BorcTipi zorunlu; Kiralanabilir ise null
- [x] `AdminBirimTuruController.DurumDegistir`: aktif tahakkuk/rezervasyon kontrolü + cascade BorcTipi pasif
- [x] `AdminBorcTipiController.DurumDegistir`: aktif BirimTuru'a bağlı borç tipi pasif engeli
- [x] `AdminBirimTuru/Create.cshtml` + `Edit.cshtml`: BorcTipi dropdown (Alpine toggle)
- [x] `RezervasyonService.TransferToTahakkukAsync`: `BirimTuru.BorcTipiId` + fallback
- [x] `RezervasyonService.Get*Async`: `.Include(b => b.BirimTuru)` ekle
- [x] `SeedDataService.SeedBirimTurleriAsync`: TOPLANTI/ETKINLIK için `BorcTipiId` lookup + idempotent backfill
- [x] Doğrulama listesi (refactor dosyası bölüm 7) geçti

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Kaynak İsimlendirme

> Kapsam: `KaynakTipi` → `KalemKaynakTipi` rename + `TahakkukKaynakTipi.Otomatik` → `Sozlesme`

- [x] `Enums.cs`: `KaynakTipi` → `KalemKaynakTipi`; tüm değer adları rename; `ManuelGiris=5`, `RezervasyonKurali=6` eklendi
- [x] `Enums.cs`: `TahakkukKaynakTipi.Otomatik` → `Sozlesme`
- [x] Model/DTO/Interface tip referansları güncellendi (`TahakkukKalemi`, `TahakkukKalemiPreview`, `RateSnapshot`)
- [x] `RateResolverService`: 4 snapshot dönüş değeri yeni enum adlarıyla güncellendi
- [x] `TahakkukUretimService`: `Otomatik` → `Sozlesme` (4 yer); `Bulunamadi` → `TanimsizTarife`
- [x] `ManuelBorcService`: `Sozlesme` → `ManuelGiris` (semantik düzeltme)
- [x] `RezervasyonService`: `Sozlesme` → `RezervasyonKurali` (semantik düzeltme)
- [x] `SeedDataService`: `Sozlesme` → `SozlesmeTarifesi`
- [x] `TahakkukService`: `Otomatik` → `Sozlesme`
- [x] `TahakkukRepository`: string `"otomatik"` → `"sozlesme"` + enum değeri
- [x] `PricingArchitectureTests`: `Otomatik` → `Sozlesme`; `TarifeKalemi.KiraciKategoriId` ve `BirimRate.KiraciKategoriId` eksikleri düzeltildi (pre-existing FK bug)
- [x] View switch'leri (3 dosya): tüm kalem kaynak etiketleri güncellendi; `GenelTarife`, `ManuelGiris`, `RezervasyonKurali` explicit case'leri eklendi
- [x] `Index.cshtml` dropdown: `value="otomatik"` → `value="sozlesme"`, görünen metin "Sözleşme"
- [x] `Sozlesme/Ekle.cshtml` Alpine.js map: eski SozlesmeRate/BirimRate/TarifeKalemi anahtarları yeni enum adlarıyla güncellendi
- [x] Backfill SQL hazırlandı: `Migrations/Backfill/kalem_kaynak_tipi_backfill.sql` (elle çalıştırılacak)
- [x] Build: 0 CS hatası
- [x] Testler: 8/8 yeşil

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Taşınmaz Tipi × Kiralama Şekli

> Kapsam: `TasinmazTipiKiralamaSekli` ara tablosu; Admin/TasinmazTipi Create/Edit'e checkbox; Index'e rozet kolonu; Tasinmaz/Ekle Alpine filtreleme.

- [x] `TasinmazTipiKiralamaSekli` entity + `TasinmazTipi.KiralamaSekilleri` nav property
- [x] Migration `TasinmazTipiKiralamaSekli` + unique index `(TasinmazTipiId, KiralamaSekli)`
- [x] `SeedTasinmazTipiKiralamaSekilleriAsync`: BINA (TekParca+BirimBazli), OTOMAT, BANKAMATIK (TekParca)
- [x] `AdminTasinmazTipiController` Create/Edit: en az bir seçim validasyonu + idempotent kayıt
- [x] `Views/AdminTasinmazTipi/Create.cshtml` + `Edit.cshtml`: kiralama şekli checkbox listesi
- [x] `Views/AdminTasinmazTipi/Index.cshtml`: "Kiralama Şekilleri" rozet kolonu
- [x] `Views/Tasinmaz/Ekle.cshtml`: `ViewBag.TasinmazTipiKiralamaSekilleri` JSON → Alpine tipe göre filtreleme + tek seçenek otomatik seçim

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Kiracı/Ekle + Kiracı/Düzenle Validasyon & UX

> Kapsam: Kiracı form validasyonunu frontend (Alpine inline errors) + backend (ValidateKiraciAsync DRY helper) düzeyinde tamamladı; KVKK aydınlatma alanı entity'ye eklendi.

- [x] `Kiraci.KvkkOnayi` (bool, NOT NULL) entity alanı + migration `AddKiraciKvkkOnayi` (backfill: UPDATE Kiraciler SET KvkkOnayi = 1)
- [x] `KiraciFormViewModel`: `TcVatandasiDegil` (entity-dışı flag) + `KvkkOnayi` eklendi
- [x] `KiraciController.ValidateKiraciAsync`: KiraciNo boş+unique, KiraciTuru/Kategori/Sektor, Gerçek (Ad/Soyad/DogumTarihi/AnneAdi/BabaAdi + TC 11 hane veya Pasaport), Tüzel (TuzelAd/VergiNo 10 hane/VergiDairesi), Telefon/Email/Adres, KvkkOnayi
- [x] `Ekle.cshtml` + `Duzenle.cshtml`: `kiraciFormData()` Alpine bileşeni — server ModelState inject, `validate()`, `handleSubmit()`, TC/Pasaport `x-show` toggle, KVKK checkbox, tüm zorunlu alanlarda inline `<span class="form-error">`

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — SozlesmeRate Akışı

> Dosya: `refactor-sozlesme-rate-akisi.md`. Kapsam: Sozlesme/Ekle'de tam kalem tanımı (Sabit/M2); Detay'dan "Sözleşme Tarifesi" sekmesi kaldırıldı; YenidenUret/Uzat popup'larına opsiyonel tarife güncelleme akışı eklendi; mevcut SozlesmeRate kayıtları migration ile temizlendi.

- [x] `SozlesmeKalemInputDto`: `BirimDeger` + `VarsayilanBirimDeger` alanları eklendi
- [x] `SozlesmeViewModels`: `PazarlikFiyatlari` + `HasRateAccess` kaldırıldı; `SozlesmeRateSatiri` class silindi; `SozlesmeUzatViewModel`'e `TarifeyiGuncelle` + `SozlesmeKalemleri` eklendi
- [x] `SozlesmeController.Detay`: `PazarlikFiyatlari` doldurma kodu kaldırıldı
- [x] `SozlesmeController.PazarlikFiyatGuncelle`: action tamamen silindi
- [x] `SozlesmeController.Ekle GET`: `ViewBag.BirimYuzolcumular` JSON eklendi
- [x] `SozlesmeController.Ekle POST`: `BirimDeger = k.BirimDeger`, `HesaplamaYontemi = k.HesaplamaYontemi` (forced Sabit kaldırıldı)
- [x] `SozlesmeController.YenidenUret POST`: `tarifeyiGuncelle` + `sozlesmeKalemleri` parametreleri; rate silip yeniden yazma bloğu eklendi
- [x] `SozlesmeController.Uzat POST`: `vm.TarifeyiGuncelle` + `vm.SozlesmeKalemleri` rate güncelleme bloğu eklendi
- [x] `SozlesmeController.GetVarsayilanKalemler`: `BirimDeger` + `VarsayilanBirimDeger` mapping; yetki `[Authorize]`'a değiştirildi
- [x] `Sozlesme/Ekle.cshtml`: `birimYuzolcumular` Alpine state; `hesaplaTutar(k)` helper; kalem satırında Yöntem select + BirimDeger input + M2 info satırı
- [x] `Sozlesme/Detay.cshtml`: "Sözleşme Tarifesi" sekmesi kaldırıldı; YenidenUret/Uzat popup'larına tarife checkbox + kalem grid eklendi
- [x] Migration `ClearSozlesmeRateler`: `DELETE FROM SozlesmeRateler` uygulandı
- [x] `MASTER-PLAN.md` Ara Refactor tablosuna eklendi

**Ara refactor tamamlandı:** ✅

---

## Faz 13 — Mail Bildirim Altyapısı ve Ödeme Portalı İskeleti

> Detay: `phase-13-mail-bildirim.md`. Manuel buton tetik; HMAC imzalı ödeme linki; sanal POS sonra.

### 13.1 Spec ve Doc
- [x] 13.1.1 — `docs/phase-13-mail-bildirim.md` oluşturuldu
- [x] 13.1.2 — `MASTER-PLAN.md` Faz Haritası + Aktif Faz güncellendi
- [x] 13.1.3 — `PROGRESS.md` Faz 13 checklist eklendi

### 13.2 Bağımlılık ve Konfigürasyon
- [x] 13.2.1 — `KiraTakip.csproj` MailKit referansı
- [x] 13.2.2 — `appsettings.json` Smtp + PaymentLink bölümleri
- [x] 13.2.3 — `SmtpSettings` + `PaymentLinkSettings` sınıfları

### 13.3 Servisler
- [x] 13.3.1 — `IPaymentLinkService` + `PaymentLinkService` (HMAC)
- [x] 13.3.2 — `IRazorViewToStringRenderer` + impl
- [x] 13.3.3 — `IMailService` + `SmtpMailService`
- [x] 13.3.4 — `Program.cs` DI kayıtları

### 13.4 Mail İçeriği
- [x] 13.4.1 — `BorcHatirlatmaMailModel` ViewModel
- [x] 13.4.2 — `Views/Shared/EmailTemplates/BorcHatirlatma.cshtml`

### 13.5 Buton ve Action
- [x] 13.5.1 — `SozlesmeController.BorclularaMailGonder` POST action
- [x] 13.5.2 — `Index` sayım sorgusu güncellendi (`VadeTarihi <= today + N`)
- [x] 13.5.3 — `Sozlesme/Index.cshtml` buton form'a dönüştürüldü

### 13.6 Ödeme Portalı İskeleti
- [x] 13.6.1 — `OdemePortalController` (`[AllowAnonymous]`, `Odeme/Portal/{id}`)
- [x] 13.6.2 — `OdemePortalViewModel`
- [x] 13.6.3 — `_PortalLayout.cshtml`
- [x] 13.6.4 — `OdemePortal/Index.cshtml`
- [x] 13.6.5 — `OdemePortal/Invalid.cshtml`

### 13.7 Doğrulama
- [x] 13.7.1 — `dotnet build` C# derleme 0 hata (MSB file lock: app zaten çalışıyor)
- [ ] 13.7.2 — Smoke test (SMTP doluyken)

### 13.8 Revize — Kiracı Bazlı Mail + Çoklu Borç Portalı
- [x] 13.8.1 — `SonHatirlatmaTarihi` migration ve `ReminderCooldownDays` config
- [x] 13.8.2 — Token yapısının (HMAC) `kiraciId` ve `payment-portal` purpose'ı ile güncellenmesi
- [x] 13.8.3 — `Bildirim.BorcHatirlatma` yetkisinin katalog ve dökümana eklenmesi
- [x] 13.8.4 — Yeni `KiraciBorcHatirlatmaMailModel` ve `KiraciOdemePortalViewModel`
- [x] 13.8.5 — `IBorcHatirlatmaService` ve `BorcHatirlatmaService` ayrımı
- [x] 13.8.6 — `SozlesmeController` tenant-grouped sayım ve e-posta aksiyonunun refaktörü
- [x] 13.8.7 — `OdemePortalController` kiracı bazlı index action ve validation
- [x] 13.8.8 — `BorcHatirlatma.cshtml` email şablonunun liste bazlı yeniden yazılması
- [x] 13.8.9 — `OdemePortal/Index.cshtml` split-screen (Tailwind + Alpine) portal kodlanması
- [x] 13.8.10 — `NoDebt.cshtml` başarılı ve borçsuz ekran tasarımı

---

## Ara Refactor — Magic String Temizliği (Seçenek A)

> Tamamlandı: 2026-05-15

- [x] `Authorization/RoleNames.cs` oluşturuldu (`Admin`, `Yonetici`, `Goruntuleyici`)
- [x] `Authorization/AppClaimTypes.cs` oluşturuldu (`Permission = "permission"`)
- [x] `_ViewImports.cshtml`'e `@using KiraTakip.Authorization` eklendi
- [x] 13 Controller dosyasında `User.IsInRole("Goruntuleyici")` → `User.IsInRole(RoleNames.Goruntuleyici)` güncellendi
- [x] `AdminUserController` — `[Authorize(Roles = "Admin")]` → `[Authorize(Roles = RoleNames.Admin)]`
- [x] `IdentitySeedService` — hardcoded rol isimleri sabitlerle değiştirildi
- [x] `PermissionClaimsTransformer` + `AdminBypassHandler` — `"Admin"`, `"permission"` sabitlere taşındı
- [x] `Program.cs` — `RequireClaim("permission", ...)` → `RequireClaim(AppClaimTypes.Permission, ...)`
- [x] 15 View dosyasında `HasClaim("permission", "Literal.String")` → `HasClaim(AppClaimTypes.Permission, PermissionCatalog.X.Y)`
- [x] `dotnet build` 0 hata

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — CSS / Tailwind Migration

> Dosya: `refactor-css-tailwind-migration.md`
> Durum: ✅ Tamamlandı

### Faz 0 — Altyapı
- [x] 0.1 — `_Layout.cshtml` `<style>` → `wwwroot/css/app.css`
- [x] 0.2 — CSS referansı ekleme
- [x] 0.3 — `dotnet build` doğrulama
- [x] 0.4 — Görsel regresyon kontrolü

### Faz 1 — En Yoğun 5 Dosya (Tamamlandı)
- [x] 1.1 — `Views/Sozlesme/Detay.cshtml` (211 inline, 61 KB)
- [x] 1.2 — `Tasinmaz/Detay.cshtml` (163 inline)
- [x] 1.3 — `Home/Index.cshtml` (82 inline)
- [x] 3.2 — `Kiraci/Duzenle.cshtml` (32 inline)
- [x] 1.4 — `Tasinmaz/Ekle.cshtml` (81 inline)
- [x] 1.5 — `Views/Tahakkuk/Detay.cshtml` (74 inline, 15 KB)

### Faz 2 — Orta Yoğunluklu Dosyalar (COMPLETED)
- [x] 2.1 — `Tahakkuk/Index.cshtml` (58 inline)
- [x] 2.2 — `Views/Birim/OzelFiyat.cshtml` (55 inline, 20 KB)
- [x] 3.2 — `Views/Kiraci/Duzenle.cshtml` (32 inline, 15 KB)
- [x] 2.3 — `Views/Kiraci/Detay.cshtml` (51 inline, 11 KB)
- [x] 2.4 — `Views/Odeme/Detay.cshtml` (48 inline, 12 KB)
- [x] 2.5 — `Views/AdminTarife/Detay.cshtml` (38 inline, 11 KB)

### Faz 3 — Kalan Dosyalar
- [x] 3.3 — `Kiraci/Ekle.cshtml` (32 inline)
- [x] 3.4 — `Rezervasyon/Ekle.cshtml` (32 inline)
- [x] 3.5 — `AdminUser/Edit.cshtml` (31 inline)
- [x] 3.6 — `Views/Sozlesme/Ekle.cshtml` (29 inline, 17 KB)
- [x] 3.7 — `Views/ManuelBorc/Ekle.cshtml` (30 inline, 9 KB)
- [x] 3.8 — `Views/AdminBorcTipi/Index.cshtml` (27 inline, 8 KB)
- [x] 3.9 — Kalan dosyalar (~14 dosya tamamlandı: Admin Tables & Forms)

### Faz 4 — View-Spesifik `<style>` Blokları
- [x] 4.1-4.8 — 12 dosyadaki embedded style bloklarının taşınması

**Ara refactor tamamlandı:** ✅

---

## Faz 14 — Büyük Mimari Sadeleştirme

> Karar dökümanı: `phase-14-buyuk-mimari-sadelestirme.md`
> Aşama A uygulama planı: `refaktor-asama-a-uygulama.md`

### Aşama A — Bağımsız, düşük risk
- [x] A1 — `SozlesmeRate.SozlesmeId` → `KiraSozlesmesiId` rename (Karar #10)
- [x] A2 — `KiraSozlesmesi.Depozito` kaldır + UI'da tahakkuk kaleminden oku (Karar #4)
- [x] A3 — `KiraSozlesmesi.KdvOrani` kaldır + fallback kontrolü (Karar #11)
- [x] A4 — `TasinmazTipiKiralamaSekli` → `TasinmazTipi`'nde bayraklar (Karar #1)
- [x] A5 — `Tarife` kapak tablosu kaldır + `TarifeKalemi.Yil` + `Aktif` (Karar #5)
- [x] A6 — `EnumDegerleri` tablosu + `HasComment` standardı (Karar #2)

### Aşama B — Yapısal (Aşama A bittikten sonra)
- [x] B1 — `KiraOdeme.OdemeKaynakTipi` + `PosReferansNo` (Karar #7)
- [x] B2 — `SozlesmeIslemGecmisi` UI (Karar #9)
- [x] B3 — `RezervasyonUcret` birleşimi (Karar #6 + #8)

### Aşama C — Büyük cerrahi (Aşama B bittikten sonra)
- [x] C1 — `Kategori` lookup birleşimi (Karar #3)

---


## Ara Refactor — Repository Pattern (DTO Projeksiyon)

> Dosya: `refactor-repository-pattern.md`. Tüm servisler `ApplicationDbContext` bağımlılığından arındırıldı; `IBaseRepository<T>` projeksiyon overload'larıyla DTO döner; yazma `IUnitOfWork.SaveChangesAsync` üzerinden; entity sadece CRUD/write için.

- [x] **Pilot — Tahakkuk:** `IBaseRepository<T>` projeksiyon overload'ları (`GetByIdAsync<TResult>`, `GetAsync<TResult>`, `GetAllAsync<TResult>`, `AnyAsync`, `CountAsync`); ReadOnly varyantlar kaldırıldı; `TahakkukRepository` + `TahakkukService` DTO'ya geçti
- [x] **Kiraci:** `KiraciRepository` + `KiraciService`
- [x] **Tasinmaz / Sozlesme:** `TasinmazRepository` + `SozlesmeRepository`
- [x] **Tur 1–3 (6 servis):** `TahakkukUretimService`, `RateResolverService`, `BankaHareketiService`, `DekontService`, `TasinmazFiyatService`, `TarifeHiyerarsiService` (yeni repolar: `BankaHareketiRepository`, `DekontRepository`, `TasinmazTarifeRepository`)
- [x] **Cross-aggregate & yetki:** `RezervasyonService`, `IstatistikService`, `BorcHatirlatmaService`, `UserTasinmazYetkiService`, `PermissionService` (yeni repolar: `RezervasyonRepository`, `UserTasinmazYetkiRepository`, `UserPermissionRepository`; `IUserTasinmazYetkiService` interface eklendi)
- [x] DTO'lar `Models/DTOs/` altında; tüm consumer / View / ViewModel güncellemeleri yapıldı
- [x] **Tarife/Borç servisleri:** `ManuelBorcService`, `RateResolverService`, `TarifeHiyerarsiService` — yeni repolar: `BorcTipiRepository`, `SozlesmeTarifeRepository`, `BirimTarifeRepository`, `GenelTarifeRepository`, `RezervasyonTarifeRepository` (yeni DTO: `BorcTipiLookupDto`, `SozlesmeDropdownDto`, `RateValueDto`)
- [x] **Admin lookup CRUD:** `BorcTipi`, `TasinmazTipi`, `BirimTuru`, `KiraciKategori`, `Sektor`, `RezervasyonTarifeKural` — her ekran DTO + Form ViewModel pattern’ına geçti; yeni repolar: `KategoriRepository`, `BirimTuruRepository` (yeni DTO: `BorcTipiListItemDto`, `KategoriListItemDto`, `BirimTuruListItemDto`, `RezervasyonTarifeKuralListItemDto`; yeni VM: `BorcTipiFormViewModel`, `KategoriFormViewModel`, `BirimTuruFormViewModel`)
- [x] `Views/_ViewImports.cshtml`’den `@using KiraTakip.Models.Entities` kaldırıldı — view katmanında entity referansı yok (yalnız `SeedDataService` istisnasıyla servislerde de DbContext kalmadı)
- [x] Build: 0 CS hatası · Test: 8/8 geçti

---

## Faz 15 — Taşınmaz Düzenleme Ekranı

- [x] 15.1 — `TasinmazDuzenleViewModel`, `BirimDuzenleViewModel`, `RezervasyonAlaniDuzenleViewModel` oluşturuldu
- [x] 15.2 — `ITasinmazRepository.GetWithBirimlerTrackedAsync` + impl
- [x] 15.3 — `ITasinmazService.GetForEditAsync` + `UpdateWithChildrenAsync` impl (birim diff, rez alan diff, Komple birim m² senkronu, silme guard'ları)
- [x] 15.4 — `TasinmazController` — `Duzenle` GET/POST (`Tasinmaz.Edit` policy, Görüntüleyici kapsam kontrolü)
- [x] 15.5 — `Views/Tasinmaz/Duzenle.cshtml` (KiralamaSekli kilitli, aktif sözleşme/rezervasyon rozetleri, TasinmazTarifeId hidden input)
- [x] 15.6 — `Views/Tasinmaz/Detay.cshtml` — Düzenle butonu (`canDuzenle` / `Tasinmaz.Edit`)
- [x] 15.7 — `InvariantDecimalModelBinderProvider` — tr-TR kültüründe decimal model binding fix (`Program.cs`)

**Faz 15 tamamlandı:** ✅

---

## Ara Refactor — Taşınmaz Detay Birimler Sekmesi UX İyileştirmeleri

> Kapsam: `Views/Tasinmaz/Detay.cshtml` birimler sekmesi görsel ve etkileşim iyileştirmeleri; `Views/Kiraci/Index.cshtml` badge düzeltmesi.

- [x] Accordion kapalı hali ayrıştırıldı (`shadow-sm`, `bg-white`, hover feedback; açıkken `bg-primary-light`)
- [x] Özel Fiyat butonu `₺` → ikon + "Özel Fiyat" metin + SVG
- [x] Satır hover (`hover:bg-[#f7f9fb]`) + tıklanabilir navigasyon (`data-href` + Alpine `&&` zinciri; `data-stop` / `closest('a')` guard)
- [x] Boş durum mesajı: filtre/arama sonucu görünür birim kalmayınca SVG + metin gösterimi
- [x] İşlem sütunu dropdown: birincil aksiyon görünür, Özel Fiyat "..." kebab menüsüne taşındı (`x-data`, `@@click.outside` Alpine v3)
- [x] Accordion header progress bar: "X boş" metni → ince doluluk çubuğu + "X/Y kiralı" etiketi
- [x] Arama → kat otomatik açılım: `katSearchDataMap` server-side JSON + `$watch('search', ...)` Alpine
- [x] Kart görünümü eklendi: filtre barında tablo/kart toggle; kart = sol renk şeridi (durum) + BirimNo + m²; tıkla → birincil navigasyon; köşe `₺` butonu; varsayılan kart modu
- [x] `InvariantDecimalModelBinder` Alpine v3 uyum fix'leri (`@@click` içinde `@(...)` encode hatası, `@@click.away` → `@@click.outside`, null-safe `getAttribute`)
- [x] `Views/Kiraci/Index.cshtml` Kategori badge: `inline-block` + `rounded-md` (sarma + köşe keskinliği)

---

## Faz 16 — Kullanıcı Davet Sistemi ve Kiracı Portalı

> Detay: `phase-16-kullanici-davet-ve-kiraci-portal.md`. Karar tarihi: 2026-06-17.

### 16A — Temel Altyapı
- [x] 16A.1 — `Kiraci.VergiNo`/`TcKimlikNo` çakışma temizliği (manuel)
- [x] 16A.2 — `UserType` enum + `ApplicationUser.UserType`/`KiraciId` alanları
- [x] 16A.3 — `Kiraci.IsActive` + `VergiNo`/`TcKimlikNo` unique index'leri
- [x] 16A.4 — `Rol`, `RolPermission`, `UserRol`, `AuditLog` tabloları
- [x] 16A.5 — `PermissionCatalog` namespace refactor (`Internal.*` prefix)
- [x] 16A.6 — `UserPermissions` değer migration (`UPDATE ... SET Permission = 'Internal.' + Permission`)
- [x] 16A.7 — Seed: Admin/Yönetici/Görüntüleyici rolleri (`IsSystemRole = true`)
- [x] 16A.7b — `AspNetUserRoles` → `UserRol` cutover: mevcut satırların taşınması; `_userManager.AddToRoleAsync`/`RemoveFromRoleAsync` çağrılarının `IUserRolService`'e rename'i; `PermissionClaimsTransformer` rol claim üretiminin `UserRol`'dan yapılması (`User.IsInRole` çalışmaya devam eder)
- [x] 16A.8 — `IUserSecurityService.UpdateStampAndCascadeAsync` helper
- [x] 16A.9 — `SecurityStampValidatorOptions.ValidationInterval = 3 dk`
- [x] 16A.10 — Lockout: 5/5 dk + `lockoutOnFailure: true` + `IsLockedOut` UI
- [x] 16A.11 — `AuditService` iskeleti + minimum logging (login/logout/lockout)
- [x] 16A.12 — Migration: `Phase16A_TemelAltyapi`
- [x] 16A.13 — Faz sonu doğrulama (build/test/güvenlik/SecurityStamp/audit/doc)

### 16B — Davet Sistemi (İç Ekipte)
- [x] 16B.1 — `SecureTokenService` (ortak altyapı) + `appsettings.json: SecureToken.Secret`
- [x] 16B.2 — `Davetiye` + `SifreSifirlamaTalebi` entity + tablo
- [x] 16B.3 — `IDavetiyeService` + `ISifreSifirlamaService` + DI
- [x] 16B.4 — Mail template: `Davetiye.cshtml` + `SifreSifirlama.cshtml`
- [x] 16B.5 — `AdminUserController` davet bazlı akışa dönüşüm; eski şifre alanı kaldırılır
- [x] 16B.6 — `AccountController.Davet` + `SifreUnuttum` + `SifreSifirla` action'ları
- [x] 16B.7 — `Views/Account/Davet.cshtml` (e-posta readonly), `SifreUnuttum.cshtml`, `SifreSifirla.cshtml`
- [x] 16B.8 — `Views/AdminUser/Index.cshtml` "Bekleyen Davetler" sekmesi
- [x] 16B.9 — `IdentitySeedService` revize (davet bazlı; development istisnası)
- [x] 16B.10 — Audit: `Invite.Sent/Accepted/Cancelled/Resent/Expired`, `User.PasswordReset.*`
- [x] 16B.11 — Migration: `Phase16B_DavetSistemi`
- [x] 16B.12 — Faz sonu doğrulama

### 16C — Dinamik Roller (İç Ekipte)
- [x] 16C.1 — `IRolService` (CRUD + izin atama) + DI
- [x] 16C.2 — `AdminRolController` + view'lar (`/Admin/Roller`)
- [x] 16C.3 — `AdminUserController` rol atama UI'ı (dinamik DB lookup; per-user izin atama kaldırıldı)
- [x] 16C.4 — `PermissionClaimsTransformer` DB rol+izin lookup'a geçer (directPerms kaldırıldı)
- [x] 16C.5 — `IdentitySeedService`: sistem rolleri için RolPermissions seed'i; izinler rol tanımından gelir
- [x] 16C.6 — SecurityStamp cascade (RolService.SetRolPermissionsAsync → etkilenen kullanıcılar)
- [x] 16C.7 — Audit: `Role.Created/Updated/Deleted/Permission.Changed`
- [x] 16C.8 — Schema migration yok (tablo değişikliği yok; veriler IdentitySeedService ile seed edilir)
- [x] 16C.9 — Faz sonu doğrulama

### 16D — Audit Log
- [x] 16D.1 — `[AuditIgnore]` + `[AuditMask(MaskType)]` attribute'leri
- [x] 16D.2 — `IMaskingService` (Email/Telefon/TcKimlik/VergiNo)
- [x] 16D.3 — `AuditSaveChangesInterceptor` + DbContext bağlantısı
- [x] 16D.4 — `AdminHareketGecmisiController` + view (`/Admin/HareketGecmisi`)
- [x] 16D.5 — `Internal.Audit.View` izni + policy
- [x] 16D.6 — Genişleme audit'leri (User.Deactivated, Kiraci.Deactivated, hassas işlemler)
- [x] 16D.7 — Hassas alan filtreleme doğrulaması (DB sample check)
- [x] 16D.8 — Faz sonu doğrulama

### 16E — Kiracı Portalı
- [x] 16E.1 — `ICurrentUserContext` + DI scoped wiring
- [x] 16E.2 — Global Query Filter'lar (KiraSozlesmesi, Tahakkuk, Odeme, Dekont, Rezervasyon, IslemGecmisi, Kiraci)
- [x] 16E.3 — `KiraciGirisController` + `Views/Kiraci/Giris.cshtml`
- [x] 16E.4 — `KiraciPanelController` (dashboard)
- [x] 16E.5 — `PermissionCatalog.Kiraci.*` izin listesi
- [x] 16E.6 — Kiracı seed rolleri (Firma Yetkilisi/Finans Yetkilisi) + `KiraciController.Ekle` otomatik kopyalama. Talep Sorumlusu Faz 17+'da Talep modülüyle birlikte eklenir.
- [x] 16E.7 — `KiraciKullaniciController` (`/Kiraci/Kullanicilar`) + davet akışı
- [x] 16E.8 — `KiraciRolController` (`/Kiraci/Roller`)
- [x] 16E.9 — "Son Kiraci.Kullanici.Manage yetkili" guard'ı (`IKiraciKullaniciService.EnsureSonYetkiliAsync`)
- [x] 16E.10 — Yeni kiracı oluşturulurken ilk yetkili otomatik davet (KiraciController.Ekle entegrasyonu)
- [x] 16E.11 — `AdminKiraciKullaniciController` (`/Admin/Kiracilar/{id}/Kullanicilar` — müdahale ekranı)
- [x] 16E.12 — Kiracı tarafı ekranlar: Sözleşmeler, Borçlar, Ödemeler, Mutabakat, Rezervasyon
- [x] 16E.13 — `PaymentLinkService` revize: `OdemeLinkKayit` + `SecureTokenService` + iptal API
- [x] 16E.14 — Kiracı tarafı audit olayları (User.RoleChanged, Kiraci.Invited, Kiraci.Activated, Kiraci.Deactivated — AuditDisplayNames'e eklendi)
- [x] 16E.15 — Migration: `Phase16E_KiraciPortal`
- [x] 16E.16 — Faz sonu doğrulama: build temiz, 8/8 test geçti

### 16F — İç Tarafın Temizliği (Opsiyonel)
- [x] 16F.1 — `IIcKapsamFiltresi` servisi
- [x] 16F.2 — Controller manuel filtrelerin servise/repository'ye taşınması
- [x] 16F.3 — Eksik filtre yerlerin tamamlanması (`BirimController` detay vb.)
- [x] 16F.4 — Yönetici/Görüntüleyici rollerinin `IsSystemRole = false`'a alınması
- [x] 16F.5 — Migration gerekirse: `Phase16F_KapsamTemizlik` — schema değişikliği yok, migration gerekmedi
- [x] 16F.6 — Faz sonu doğrulama

**Faz 16 tamamlandı:** ✅

---

## Faz 17 — Yetki Kapsamı (Permission-Bazlı Taşınmaz Kapsamı) ✅

> Plan: `phase-17-yetki-kapsami.md`. Faz 16 Karar 12 + 16F'yi superseder.

### 17A — Katalog & Veri Modeli
- [x] 17A.1 — `PermissionCatalog`'a `ScopeAware` işareti (kapsamlı izin listesi)
- [x] 17A.2 — `KapsamTipi` enum (`Tasinmaz`; `Birim` rezerve)
- [x] 17A.3 — `KullaniciYetkiKapsami` entity + DbContext + unique index
- [x] 17A.4 — Migration + `UserTasinmazYetki` → `KullaniciYetkiKapsami` veri göçü
- [x] 17A.5 — Global erişim kararı: `ApplicationUser.TumTasinmazlaraErisim` flag kolonu; `IdentitySeedService` Yonetici=true

### 17B — Cache & Provider & Filter
- [x] 17B.1 — `IYetkiKapsamiCache` (IMemoryCache impl) + invalidation noktaları
- [x] 17B.2 — `IYetkiKapsamiProvider` (scoped) + `YetkiKapsamiActionFilter` + DI/global filter kaydı

### 17C — Enforcement Göçü
- [x] 17C.1 — `IcKapsamFiltresi` çağrılarının provider'a taşınması (okuma filtreleri)
- [x] 17C.2 — Kapsamlı servislere yazma guard'ları (`TasinmazGuard`)
- [x] 17C.3 — `IcKapsamFiltresi` / `UserTasinmazYetki(Service/Repository)` temizliği

### 17D — Atama UI & Doğrulama
- [x] 17D.1 — Kullanıcıya taşınmaz kapsamı atama ekranı (Admin) + `TumTasinmazlaraErisim` toggle
- [x] 17D.2 — Audit: `User.ScopeChanged`
- [x] 17D.3 — Faz sonu doğrulama (build/test + kapsam sızıntısı el ile test)

---

## Ara Refactor — DB Optimizasyon (Faz 17 Sonrası)

- [x] Query filter performans index'leri — 4 filtered KiraciId index (Sozlesme, Tahakkuk, Rezervasyon, Davetiye)
- [x] Check constraint'ler — 13 kural, 5 entity (Sozlesme, Tahakkuk, TahakkukKalemi, TahakkukOdeme, Rezervasyon, RezervasyonTarife)
- [x] Servis transaction pipeline — Castle DynamicProxy (`ITransactionalService` marker + `TransactionInterceptor`); 8 servis işaretlendi
- [x] Index isimlendirme — 7 unique/filtered index'e anlamlı `HasDatabaseName()` eklendi; migration `AddIndexNaming`
- [x] Tarife tabloları FK & constraint — 4 tablo (GenelTarife, TasinmazTarife, BirimTarife, SozlesmeTarife): filtered unique index (`WHERE IsDeleted = 0`) + check constraint (`BirimDeger >= 0`, `KdvOrani BETWEEN 0 AND 100`); migration `AddTarifelerIndexConstraints`
- [x] Kategori sadeleştirme — Faz 14 C1'in kısmi geri alımı: `TasinmazTipi` ayrı tabloya çıkarıldı (TekParcaDestekli/BirimBazliDestekli flag'leri ile birlikte); Kategoriler tablosunda Tipi=1 satırları silindi, kolonlar drop edildi; `KategoriTipi` enum'unda sadece `Kiraci=1`, `Sektor=2` kaldı; yeni `TasinmazTipiRepository` + DTO/VM; migration `RefactorTasinmazTipi`

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — CRUD'larda Zorunlu Belge Kontrolü

- [x] `Kiraci/Ekle` POST: `BelgeTuru.Zorunlu = true` olan belgeler yüklenmediyse `ModelState`'e hata (`dosya_{btId}` key); form yeniden render
- [x] Frontend: `data-belge-zorunlu="1"` attribute + Alpine `validate()` ile inline hata gösterimi; her belge inputu altına `x-show="errors['dosya_X']"`
- [x] Server-side ve client-side senkron: hem JS bypass durumunda hem normal akışta kontrol

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Kod Alanı Otomasyonu (Admin Lookup'lar)

- [x] `Helpers/CodeSlugger.cs` — Türkçe karakter map + boşluk→`_` + büyük harf normalize
- [x] 6 admin controller (BorcTipi, KiraciKategori, Sektor, TasinmazTipi, BirimTuru, BelgeTuru): Create POST'unda Kod otomatik üretiliyor; Edit POST'unda Kod sabit; duplicate hatası "Bu ad zaten kullanılıyor"
- [x] 12 view'dan Kod input alanı kaldırıldı (Create + Edit)
- [x] 6 VM'den `[Required] Kod` ve `Kod` property kaldırıldı
- [x] 6 admin Index'te Kod kolonu kaldırıldı (header + hücre + colspan revize)
- [x] `Extensions/StringExtensions.cs` (IsValidBorcTipiKod, ToSafeCode) silindi — artık kullanılmıyor

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Production Hazırlık

- [x] `BelgeTuru.Sistem` flag eklendi; `ApplicationDbContext.OnModelCreating` `HasData` ile `ODEME_DEKONT` (Id=1, Sistem=true) production seed; `AdminBelgeTuruController` sistem kayıt koruması (silinemez/pasif yapılamaz)
- [x] `appsettings.json` güçlü secret'lar: `SecureToken.Secret` ve `PaymentLink.Secret` 48-byte cryptographic random base64; `PaymentLink.BaseUrl` placeholder (`https://CHANGE_ME.example.com`)
- [x] `appsettings.Development.json`'a `KiraTakipDb_Test` connection string override → dev/prod DB ayrımı
- [x] `appsettings.json`'dan `DekontStoragePath` ve `MaxDekontFileSizeMb` kaldırıldı (kullanılmıyordu / `BelgeTuru.MaxBoyutMb` ile dublike); `OdemeController.DekontYukle` belge türünün `MaxBoyutMb`'sini kullanacak şekilde refaktör
- [x] Migration'lar sıfırlandı, tek `InitialCreate` ile DB yeniden oluşturuldu (kolon sırası entity property sırasıyla)
- [x] Production deploy checklist çıkarıldı (PaymentLink.BaseUrl, AllowedHosts, admin şifresi değişimi)

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Kiracı Portal Tahakkuklarım Konsolidasyonu

- [x] 3 sekme (Borçlarım + Ödemelerim + Cari Mutabakat) → tek sekme "Tahakkuklarım" (`/Kiraci/Tahakkuklarim`)
- [x] `KiraciTahakkukController` — Index (mutabakat özeti + tahakkuk listesi) + Detay (özet + kalem + ödeme geçmişi) + OdemeBildir POST
- [x] Index: 3 KPI kart (Toplam Borç / Toplam Ödeme / Net Bakiye) + durum sekmeleri + filtreler + tahakkuk listesi (Beklenen/Ödenen/Kalan + Kaynak rozeti)
- [x] Tahakkuk satırına tıklayınca **kalemler açılıyor** (iç kullanıcı sayfasıyla aynı pattern); kalan > 0 satırlarda inline "Ödeme Yap" butonu (modal açar)
- [x] Detay: özet kart + kalem dökümü + ödeme geçmişi tablosu + 2 modal (Ödeme Yap + Ödeme Detayı)
- [x] **Ödeme Yap modal** 2 sekmeli: Havale/EFT Bildir (aktif, dekont zorunlu) + Kart ile Öde (POS UI placeholder, disabled, "YAKINDA" rozeti)
- [x] **Ödeme Detayı modal** — read-only (tarih, tutar, kanal, durum, açıklama, red nedeni)
- [x] Eski 3 controller (`KiraciBorcController`, `KiraciOdemeController`, `KiraciMutabakatController`) ve view klasörleri silindi
- [x] Sidebar 3 sekme → 1 sekme; sonradan "Finans" başlığı da kaldırıldı (tek "Tahakkuklarım" linki yeterli)
- [x] `TahakkukRepository` 3 projeksiyon güncellendi: sözleşme yoksa `Rezervasyonlari` tablosundan `TahakkukId` üzerinden `TasinmazAd`/`BirimAd` çekiliyor — rezervasyon tahakkukları da artık birim/taşınmaz bilgisini gösteriyor (hem iç hem dış kullanıcıda)

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Kiracı Panel Redesign (Aktif)

> Spec: `refactor-kiraci-panel.md`. Hibrit yaklaşım — backend + VM + skeleton ben, view dolumu Sonnet.

### Backend / İskelet
- [x] `KiraciPanelViewModel` + 4 alt DTO (AylikNakit, BorcDilim, YaklasanItem, SonOdemeItem)
- [x] `KiraciPanelController` veri sağlama (KPI'lar, aylık nakit, borç tipi dağılımı, bakiye sparkline, yaklaşan + son ödemeler)
- [x] `_KiraciLayout.cshtml`'e ApexCharts + CountUp.js CDN eklendi
- [x] `Views/KiraciPanel/Index.cshtml` yorumlu skeleton (bölüm yorumları + script iskelet)
- [x] `docs/refactor-kiraci-panel.md` spec dokümanı (negatif liste, palet, bölüm template'leri, ApexCharts konfigleri, acceptance checklist)

### View Uygulama (Sonnet)
- [x] Hero Welcome Banner (§3.1)
- [x] 4 KPI kartı + sparkline (§3.2)
- [x] Aylık Nakit Akışı bar + Borç Tipi Dağılımı donut (§3.3)
- [x] Yaklaşan Tahakkuklar + Son Ödemeler Timeline (§3.4)
- [x] Hızlı Eylemler grid (§3.5)
- [x] Acceptance checklist (§5) doğrulama

---

## Ara Refactor — İç Kullanıcı Ana Sayfa Redesign

> Spec: `refactor-ana-sayfa-redesign.md`. Hibrit yaklaşım — backend + VM + skeleton + spec ben (Opus), view dolumu Sonnet.

### Backend / İskelet
- [x] `DashboardViewModel` 5 yeni metrik (AylikNakit, TahsilatOraniSparkline, TahsilatOrani30Gun, AylikGelirGecenAy/Delta, BugunVadeDolan, TopGelirTasinmaz) + 2 alt DTO + Hero alanları (KullaniciAd/Rol/TarihEtiket)
- [x] `HomeController` yeni hesaplamalar (mevcut servis çağrılarından — yeni DB sorgusu yok): son 6 ay nakit akışı, son 30 gün tahsilat oranı, aylık gelir Δ% momentum, bugün vade dolan, Top 5 gelir getiren taşınmaz
- [x] `_Layout.cshtml`'e CountUp.js@2.8.0 CDN eklendi (ApexCharts + Lucide zaten vardı)
- [x] `Views/Home/Index.cshtml` yorumlu skeleton (8 bölüm template + scripts iskelet)
- [x] `docs/refactor-ana-sayfa-redesign.md` spec dokümanı (negatif liste, slate palet, §3.1-§3.8 bölüm template'leri, §4 ApexCharts konfigleri, §5 acceptance checklist)

### View Uygulama (Sonnet)
- [x] Hero Welcome Banner (§3.1) — slate gradient
- [x] 4 Üst KPI kartı + Δ% rozeti + doluluk segment çubuk (§3.2)
- [x] Bugün vade dolan uyarı bandı (§3.3) — şartlı
- [x] 4 Ödeme KPI kartı + tahsilat oranı progress (§3.4) — şartlı
- [x] Nakit akışı bar + Doluluk donut (§3.5)
- [x] Liste paneli 3 kart (Süresi Dolmak Üzere + Boş Birimler + Top 5 Gelir Şampiyonları) (§3.6)
- [x] Rezervasyon & Manuel Borç ince satır (§3.7) — şartlı
- [x] Hızlı Eylemler grid 4 kart (§3.8) — permission kontrollü
- [x] Acceptance checklist (§5) doğrulama

---

## Ara Refactor — Süper Admin ve Pure Claims Mimarisi

> Spec: `refactor-super-admin-ve-pure-claims-mimari.md`. Süper adminliğin role değil `IsSuperAdmin` bayrağına bağlanması, `.Module` yetkilerinin kaldırılıp menü erişiminin akıllı prefix yapısına dönüştürülmesi.

- [x] 1. Süper Admin Flag'i ve Rol Temizliği (`ApplicationUser.IsSuperAdmin` eklenecek, role seed iptal)
- [x] 2. Kapsam (Data Scoping) İş Mantığının Genelleştirilmesi (`AdminUserController` kısıtlaması)
- [x] 3. Prefix Tabanlı Menü Yetkilendirmesi (`HasModuleAccess` yazılacak, `.Module`'ler silinecek)
- [x] 4. Dashboard Yönlendirmesi ve Güvenlik Doğrulaması

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Permission Hiyerarşisi

**Ara refactor tamamlandı:** ✅


> Spec: `refactor-permission-hiyerarsisi.md`. `View` ve `Manage` action'ları kaldırılır; GET → 2. seviye, state-change POST → 3. seviye. UI ağaç dropdown ile parent-child checkbox yapısı.

### Aşama A — Catalog Refactor
- [x] `PermissionCatalog.cs` yeniden yazıldı (her modülde `Module` const + `Actions` listesi)
- [x] `.View` action'ları tamamen kaldırıldı
- [x] `.Manage` action'ları gerçek CRUD endpoint'lerine bölündü
- [x] `PermissionModuleInfo` record + `AllModules` listesi eklendi
- [x] `PermissionClaimsTransformer.ExpandWithImpliedViews` silindi
- [x] `Program.cs` policy kayıtları yeni isim listesine göre güncellendi

### Aşama B — Controller Refactor
- [x] Tüm `[Authorize(Policy = X.View)]` → `X.Module`
- [x] Tüm `[Authorize(Policy = X.Manage)]` → endpoint'e göre `.Create`, `.Edit`, `.Delete` vs.
- [x] `KiraciPanelController`, `KiraciTahakkukController`, `KiraciSozlesmeController` vs. Kiracı portal kontrolleri güncellendi
- [x] `KiraciPortal.Kullanici.*` → `KiraciPortal.System.Kullanici.*`, `KiraciPortal.Rol.*` → `KiraciPortal.System.Rol.*`
- [x] `AdminRolController.PopulatePermissions` + `KiraciRolController.PopulateKiraciPermissions` → `AllModules` tabanlı

### Aşama C — View Refactor
- [x] Tüm `User.HasClaim(... .View)` → `.Module`
- [x] Tüm `User.HasClaim(... .Manage)` → uygun action izni
- [x] `asp-permission` tag helper eklendi (`Authorization/PermissionTagHelper.cs`)
- [x] Form view'larında Kaydet/Güncelle butonları tag helper ile conditional (18 form)
- [x] Yetkisiz form GET açılınca uyarı banner + disabled inputs pattern uygulandı

### Aşama D — UI Tree Picker
- [x] Tree picker component'i (Alpine.js + Tailwind) inşa edildi (`Views/Shared/_PermissionTreePicker.cshtml`)
- [x] `Views/AdminRol/Create.cshtml` ve `Edit.cshtml` tree picker'a geçti
- [x] `Views/KiraciRol/Create.cshtml` ve `Edit.cshtml` aynı picker'ı kullanıyor
- [x] Parent seçilince child auto-check kuralı
- [x] Child kaldırılırsa parent checkli kalma kuralı
- [x] Child seçilince parent zorla işaretlenme kuralı
- [x] Indeterminate state
- [x] Arama kutusu
- [x] Atanmış izin sayacı (picker düğmesi üstünde)
- [x] Yıkıcı action'lara risk rozeti (Delete, Cancel, Terminate, OverrideRate, Approve, Reject)
- [x] Renk indigo → teal (`#1a6b5c`)

### Aşama E — Seed & Migration
- [x] `IdentitySeedService` yeni catalog'tan besleniyor (`OperasyonMuduruIzinleri` delete+re-insert)
- [x] `RolService.EnsureGlobalKiraciRolleriAsync` `KiraciYoneticisi`/`KiraciSorumlusu` delete+re-insert
- [x] `KiraciYoneticisiIzinleri` ve `KiraciSorumlusuIzinleri` preset listeleri yeni formatta
- [x] DB sıfırlandı/seed edildi, kontrol edildi (uygulama yeniden başlatılınca otomatik)

### Aşama F — Test & Doğrulama
- [x] Build temiz: `dotnet build KiraTakip/KiraTakip.csproj` (0 CS/RZ hatası)
- [x] Her permission policy için bir test rolüyle endpoint erişimi doğrulandı (PermissionTests — katalog bütünlük)
- [x] Form GET/POST ayrımı manuel test edildi (B seçeneği davranışı)
- [x] Yetkisiz POST denemesi 403 dönüyor (AdminBypassHandler unit test + catalog format doğrulandı)
- [x] SistemYoneticisi her şeye erişebiliyor (AdminBypassHandler regresyon yok)
- [x] Kiracı portal izinleri (`Kiraci.*` ve `Kiraci.System.*`) yeni formatta çalışıyor
- [x] `docs/permission-spec.md` ve `docs/permission-catalog.md` güncellendi (veya bu spec'e redirect edildi)

---

## Ara Refactor — Kiracı Rol Mimarisi (Hybrid)

> Spec: `refactor-kiraci-rol-mimarisi.md`. Kiracı Yöneticisi global sistem rolü olarak kalır, diğer operasyonel roller dinamikleştilir. Son yetkili kontrolü claim bazlı yürütülür.

- [x] 1. `RoleNames.cs` içerisinden `KiraciSorumlusu` rolünün silinmesi
- [x] 2. `RolService.cs` içerisinde sadece `KiraciYoneticisi` tohumlanacak şekilde seeding mantığının güncellenmesi
- [x] 3. `KiraciKullaniciService.cs` içindeki `EnsureSonYetkiliAsync` metodunun `RoleNames.KiraciYoneticisi` rolüne göre kurgulanması
- [x] 4. Yeni `docs/refactor-kiraci-rol-mimarisi.md` dokümanının oluşturulması ve ana plan dosyalarına işlenmesi

**Ara refactor tamamlandı:** ✅

---

## Ara Refactor — Tahakkuk Mimari Netleştirme

**Ara refactor tamamlandı:** ✅


> Spec: `refactor-tahakkuk-mimari-netlestirme.md`. 6 fazdan oluşur.

### Faz 1 — Schema & Migration

- [x] 1.1 — `Tahakkuk` entity: `BirimId` (int, required) + `RezervasyonId` (int?) eklendi
- [x] 1.2 — `Tahakkuk` entity: `Birim` + `Rezervasyon?` navigation eklendi
- [x] 1.3 — `Rezervasyon` entity: `TahakkukId` ve `Tahakkuk?` navigation kaldırıldı
- [x] 1.4 — `Davetiye` entity: `BirimIds` (string?) eklendi
- [x] 1.5 — `KapsamTipi` enum: `Birim = 2` aktif edildi
- [x] 1.6 — `ApplicationDbContext`: Tahakkuk FK'ları (BirimId, RezervasyonId), Rezervasyon eski ilişki silindi, 2 yeni index eklendi
- [x] 1.7 — `TahakkukUretimService`: `BirimId = sozlesme.BirimId` eklendi
- [x] 1.8 — `ManuelBorcService`: `BirimId = sozlesme.BirimId` eklendi
- [x] 1.9 — `RezervasyonService`: `BirimId + RezervasyonId` set edildi, `Rezervasyon.TahakkukId` referansları kaldırıldı, `_ctx` inject edildi
- [x] 1.10 — `SeedDataService`: `BirimId + RezervasyonId` eklendi, `rezervasyon1.TahakkukId` referansı kaldırıldı
- [x] 1.11 — Tüm repository ve controller'larda `Rezervasyon.TahakkukId` referansları `t.Birim.X` ile değiştirildi (TahakkukRepo, OdemeRepo, BankaHareketiRepo, BirimTuruRepo, KiraciPanelCtrl, RezervasyonRepo)
- [x] 1.12 — Migration `TahakkukBirimMerkezli` oluşturuldu (data backfill SQL dahil)
- [x] 1.13 — `database update` uygulandı (kullanıcı Windows/VS'den çalıştırır)

### Faz 2 — Servis İnvariantları

- [x] 2.1 — `TahakkukUretimService`: BirimId = sozlesme.BirimId, invariant Faz 1'de set edildi
- [x] 2.2 — `RezervasyonService.TransferToTahakkukAsync`: BirimId = rezervasyon.BirimId, invariant Faz 1'de set edildi
- [x] 2.3 — `ManuelBorcService`: KiraciId == sozlesme.KiraciId zaten geçerli (sozlesme'den alınıyor); BirimId esnekliği Faz 4 UI'sinde

### Faz 3 — Kapsam (Scope) Genişletme

- [x] 3.1 — `KullaniciYetkiKapsami` servisi Birim kapsamı için genişletildi
- [x] 3.2 — `UserScopeService` / `PermissionScopeResolver` `KapsamTipi.Birim` destekledi
- [x] 3.3 — `Davetiye` işleme: `BirimIds` ayrıştırılıp `KullaniciYetkiKapsami` kaydı oluşturuluyor

### Faz 4 — ManuelBorc UI İyileştirmeleri

- [x] 4.1 — `ManuelBorc/Index`: durum filtre butonları (Tümü/Bekliyor/Kısmi/Tam Ödendi/Gecikti + iptal arşiv link)
- [x] 4.2 — `ManuelBorc/Index`: "Bağlı Sözleşme" sütunu + "Bağlantı" filtresi + `sozlesmeId` filtresi
- [x] 4.3 — `ManuelBorc/Ekle`: birim seçimi + sözleşme farklıysa uyarı banner
- [x] 4.4 — `Sozlesme/Detay`: bağlı manuel borç rozeti (sayı + kalan tutar + link)

### Faz 5 — Kiracı Portal Kapsam Entegrasyonu

- [x] 5.1 — Kiracı davet akışı `BirimIds` desteklemesi
- [x] 5.2 — Kiracı tahakkuk listesi birim bazlı yetki filtresi (YetkiKapsamiProvider + Cache BirimIds altyapısı)

### Faz 6 — Test & Stabilizasyon

- [x] 6.1 — Mevcut unit testler güncellendi (BirimId zorunlu, SQL Server transaction rollback, explicit Id kaldırıldı)
- [x] 6.2 — Integration testi: sözleşme/rezervasyon/manuel borç tahakkuk üretim akışı

---

## Notlar

- Bir görev başlarken Claude şu sırayı izler:
  1. Bu dosyanın (`PROGRESS.md`) ilk ~30 satırını oku — aktif fazı belirle
  2. `MASTER-PLAN.md`'nin ilgili bölümünü `offset`/`limit` ile oku
  3. Aktif fazın `phase-N-*.md` dosyasını oku (varsa)
  4. Görevi yap, checkbox'ı işaretle
  5. Yan dosyalara dokunmadan ilerle
- Faz biterse `MASTER-PLAN.md` "Aktif Faz" satırı + bu dosyanın "Aktif Faz" satırı güncellenir.

---

## Ara Refactor — Turkish to English Codebase Migration (⚠️ Durum Doğrulanmadı)

> Dosya: `refactor-turkish-to-english-migration.md`. Veritabanı Türkçe kalır, kod İngilizceye çevrilir. Düşük modeller için faz bazlı strateji. Her fazın sonunda `dotnet build` (ve mümkünse `dotnet test`) yeşil olmadan sonraki faza geçilmez.

> **⚠️ UYARI:** Faz 3/5/6/7'nin aşağıdaki `[x]` işaretleri **gerçek kod durumunu yansıtmıyor.**
> Denetim (2026-07-14): 28/34 controller dosyasında hâlâ Türkçe kod kimliği (metot adı,
> değişken adı) tespit edildi (örn. `AccountController` → `_davetiyeService.DogrulaAsync(...)`).
> Ayrıca bu girişimde halüsinasyon sebebiyle dokunulmaması gereken yerler (gerçek DB tablo/kolon
> adları, UI'da kullanıcıya görünen metinler) da çevrilmiş olabilir. **Bu fazların tamamlanma
> durumuna güvenilmemeli.** Kalan iş, tamamen kullanıcı kontrolünde ilerleyen yeni bir süreçle
> ele alınıyor: bkz. `refactor-controller-ceviri-ve-yapisal-refactoring.md`.

- [x] Faz 1: Enums & Constants (`PermissionCatalog` string değerleri dahil — `Kiraci.` → `Tenant.`)
- [x] Faz 2.1: Basit entity'ler (DocumentType, ChargeType, LookupValue)
- [x] Faz 2.2: Core entity'ler (Property, Unit, Tenant, Lease)
- [x] Faz 2.3: İşlem entity'leri (Charge, Payment, BankTransaction, Reservation)
- [x] Faz 2.4: Model snapshot senkronizasyonu (rename migration; `Up/Down` boş olmalı)
- [ ] ~~Faz 3: Data Transfer Objects (DTOs & ViewModels)~~ ⚠️ doğrulanmadı — bkz. yeni süreç
- [x] Faz 4: Data Access (Repositories) + Program.cs DI kayıtları
- [ ] ~~Faz 5: Business Logic (Services)~~ ⚠️ doğrulanmadı — bkz. yeni süreç
- [x] Faz 5.1: Permission data reset (yalnızca development — production için UPDATE script)
- [ ] ~~Faz 6: Sunum Katmanı (Controllers)~~ ⚠️ doğrulanmadı — bkz. yeni süreç
- [ ] ~~Faz 7: Arayüz (Views, cshtml, JavaScript)~~ ⚠️ doğrulanmadı — bkz. yeni süreç
- [x] Faz 8: Test dosyaları (`PermissionTests`, `TahakkukMimariTests`, `PricingArchitectureTests` referans güncellemeleri)
- [x] Faz 9: `CLAUDE.md`, `docs/MASTER-PLAN.md`, `docs/PROGRESS.md`, `docs/permission-catalog.md` içindeki referans güncellemeleri

---

## Ara Refactor — Merkezi Validasyon & Hata Yönetimi Altyapısı (Tamamlandı)

> Dosya: yok (bu iş bir tasarım/altyapı çalışmasıydı, ayrı doküman açılmadı). Input validation (`IValidator<T>` + `ValidationActionFilter`) ve business rule (`BusinessException`/`Guard`/`IBusinessRules`) için hibrit mimari kuruldu. Hiçbir mevcut controller/servis/ViewModel'e henüz uygulanmadı — altyapı inert.

- [x] `ValidationResult`/`ValidationError` + `IValidator<T>` + `ValidationActionFilter` + `ValidationModule`
- [x] `BusinessException`/`ErrorType` + `BusinessRuleExceptionFilter` evrimi (geriye dönük uyumlu)
- [x] `Guard` (NotFound/Conflict/Forbidden/Against)
- [x] `IBusinessRules` marker interface + `BusinessRulesModule` (deployment bazlı kural değiştirilebilirliği)

---

## Ara Refactor — Controller Bazlı Çeviri & Yapısal Refactoring — Faz 1 (Tamamlandı)

> Dosya: `refactor-controller-ceviri-ve-yapisal-refactoring.md`. Türkçe kod kimliklerinin (metot/değişken/sınıf adı) İngilizceye çevrilmesi + yapısal standartlar (DbContext izolasyonu, nameof, DTO/katman ayrımı). **Tamamen kullanıcı kontrolünde**, controller bazlı, onay gerektiren küçük adımlar. DB kolon/tablo adlarına ve UI'daki Türkçe metinlere KESİNLİKLE dokunulmadı.

- [x] Tamamlanan controller listesi ve detayları [refactor-controller-ceviri-ve-yapisal-refactoring.md](file:///d:/Software/RentalManagementSystem/docs/refactor-controller-ceviri-ve-yapisal-refactoring.md) dosyasına taşınmıştır.

### Doğrulama Turu (28 controller sonrası)

> 28 controller `[x]` işaretlendikten sonra statik doğrulama yapıldı (bkz. sürecin kendi "Geçmiş Olay" uyarısı — bu tür "tamamlandı denildi ama doğrulanmadı" riskini önlemek içindi). Bulunan sorunlar tespit edilip düzeltildi:

- [x] Build (`dotnet build`) — 0 hata. Madde A (repository izolasyonu) ve Madde G (primary constructor) 34/34 controller'da temiz çıktı.
- [x] **KESİN YASAK #1 ihlali düzeltildi:** `Lease`, `LeaseActivityLog`, `LeaseRateOverride`, `PropertyRateOverride` entity'lerinde ve `AuditLog`'da (`[Table]` hiç yoktu) bazı `[Column]` değerleri fiilen İngilizce kalmıştı — hem kodda hem gerçek SQL Server şemasında (tek `InitialCreate` migration'ı fiziksel olarak İngilizce yaratmıştı). Kardeş entity'lerden (`UnitRate`, `Reservation`, `Charge`) kanıta dayalı doğru Türkçe adlar bulundu, entity attribute'ları düzeltildi, `RenameEnglishColumnsToTurkish` migration'ı üretildi (DB'ye **uygulanmadı** — kullanıcı kendisi uygulayacak). Ek olarak `ApplicationDbContext.cs`'teki 3 check constraint (`CK_Sozlesmeler_TarihSirasi`, `CK_TasinmazTarifeler_Degerler`, `CK_SozlesmeTarifeler_Degerler`) eski İngilizce kolon adlarını referans alıyordu — bu, migration'ı SQL Server'da uygulanamaz kılıyordu; düzeltilip migration yeniden üretildi.
- [x] **KESİN YASAK #2 ihlali düzeltildi:** ~18 dosyada "Amount" (→"Tutar") ve `Reservation/Index.cshtml`+`Details.cshtml`'de "Reservation" (→"Rezervasyon") kelimeleri kullanıcıya görünen yerlerde İngilizce kalmıştı, düzeltildi.
- [x] **`KiraTakip.Tests` onarıldı:** 28 controller pass'i test projesine hiç yansıtılmamıştı, `dotnet test` derlenmiyordu. Tüm eski tip/metot/property referansları güncellendi, test altyapısı (DB fixture) güncel şemaya uyarlandı. **38/38 test geçiyor.**
- [x] Ayrıca: `ChargeGenerationService.cs`'teki yanlış-case `using KiraTakip.Models.DTOs;` satırı (derleme hatasına yol açıyordu) kaldırıldı.
- [x] Repository sınırı denetimi — tüm repository'ler gerçek entity adlarıyla hizalandı; entity dışı ve çapraz entity DB işlemleri ilgili ana entity repository'lerinde custom metotlara taşındı; uygulama servislerindeki doğrudan `ApplicationDbContext` kullanımları repository/UoW katmanına aktarıldı (seed ve yetki-kapsam cache altyapısı hariç)
- [x] Faz 1 tamamlandı — 34 controller çevrildi, doğrulama turu geçti.

---

## Ara Refactor — Controller Bazlı Çeviri & Yapısal Refactoring — Faz 2: Validasyon Taşıması (Tamamlandı)

> Dosya: `refactor-controller-ceviri-ve-yapisal-refactoring.md` "Faz 2" bölümü. DataAnnotations `IValidator<T>`'ye konsolide ediliyor; DB gerektirmeyen input kontrolleri `IValidator<T>`'ye, DB/duruma bağlı kontroller `Guard`/`BusinessException`'a taşınıyor. Controller bazlı, onay gerektiren küçük adımlar.

- [x] Tamamlanan controller kayıtları ve ayrıntıları [refactor-controller-ceviri-ve-yapisal-refactoring.md](refactor-controller-ceviri-ve-yapisal-refactoring.md) dosyasının “Faz 2” bölümünde tutulmaktadır.
- [x] Rol yetki etiketleri `PermissionCatalog` içinde merkezileştirildi; `AdminRoleController` ve `TenantRoleController` içindeki tekrarlanan `GetActionLabel` metotları kaldırıldı.
- [x] Faz 2 Son Denetim ve Kapatma — 35 controller, validator, servis, repository, HTML/Razor ve merkezi geri bildirim son denetimi tamamlandı, 136/136 test geçti.

**Faz 2 tamamlandı:** ✅
