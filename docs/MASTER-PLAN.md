# MASTER PLAN — KiraTakip Geliştirme Yol Haritası

> **Aktif faz için önce `PROGRESS.md` dosyasını oku.** `PROGRESS-HISTORY.md`
> artık tamamlanmış çalışmaların arşividir ve yalnız tarihsel araştırmada ilgili
> bölümü kısmi olarak okunur.

---

## Token Tasarrufu Kuralları (Claude için)

1. **Her oturumda sadece şunları oku:**
   - `MASTER-PLAN.md` (bu dosya)
   - `PROGRESS.md` (aktif görev için)
   - **Sadece aktif çalışmanın** detaylı plan dosyası
2. **Diğer faz ve plan dosyalarını okuma.** Aktif çalışma değişene kadar gerek yok.
3. **`permission-catalog.md`** sadece izin/yetki ile ilgili görevde okunur.
4. **`auth-spec.md`, `bina-ofis-birim-spec.md`, `kiraci-sozlesme-finans-spec.md`, `project-spec-canonical.md`** — sadece o domain alanında değişiklik yapılırken kısmi olarak okunur. Tamamını yükleme.
5. **Kod dosyalarını araştırırken Glob/Grep** kullan, klasör tarama yapma.
6. **Tamamlanan aktif fazı** `PROGRESS.md` dosyasında işaretle; ayrıntıyı ilgili plan dosyasında tut.

---

## Faz Haritası

| Faz | Başlık | Durum | Dosya |
| --- | --- | --- | --- |
| 0 | Mimari Karar & Plan | ✅ Tamamlandı | (bu dosya) |
| 1 | Permission Modeli (Tasarım) | ✅ Tamamlandı | `phase-1-permission-model.md` |
| 2 | SQL Server Tam Geçiş | ✅ Tamamlandı | `phase-2-sqlserver-migration.md` |
| 3 | Domain Servislerinin EF Core'a Taşınması | ✅ Tamamlandı | `phase-3-domain-ef-migration.md` |
| 4 | Permission Implementasyonu (Claims, Policy) | ✅ Tamamlandı | `phase-1-permission-model.md` (Bölüm 4) |
| 5 | Ödeme Takip Modülü | ✅ Tamamlandı | `phase-5-detail.md` |
| 6 | Tablo & UX İyileştirmeleri | ✅ Tamamlandı | `phase-6-detail.md` |
| 7 | Çok Kalemli Aylık Tahakkuk | ✅ Tamamlandı | `phase-7-detail.md` |
| 8 | Parametre, Rezervasyon ve Manuel Borç | ✅ Tamamlandı | `phase-8-parametre-rezervasyon-ve-manuel-borc.md` |
| 9 | Taşınmaz × Kiracı Kategorisi Fiyatlandırma | ✅ Tamamlandı | `phase-9-fiyatlandirma-mimarisi.md` |
| 10 | Taşınmaz Detayı UX İyileştirme | ✅ Tamamlandı | `phase-10-tasinmaz-detay-ux-iyilestirme.md` |
| 11 | Tarife Hiyerarşisi + Parent Bilgi Gösterimi | ✅ Tamamlandı | `phase-11-tarife-hiyerarsisi-ve-parent-gosterim.md` |
| 12 | Stabilizasyon ve Test Borcu Temizliği | ✅ Tamamlandı | `phase-12-stabilizasyon-ve-test-borcu.md` |
| 13 | Mail Bildirim Altyapısı ve Ödeme Portalı İskeleti | ✅ Tamamlandı | `phase-13-mail-bildirim.md` |
| 14 | Büyük Mimari Sadeleştirme | ✅ Tamamlandı | `phase-14-buyuk-mimari-sadelestirme.md` (karar); `refaktor-asama-a/b/c-uygulama.md` (uygulama) |
| 15 | Taşınmaz Düzenleme Ekranı | ✅ Tamamlandı | (ayrı phase dosyası yok — PROGRESS-HISTORY.md Faz 15 bölümüne bkz.) |
| 16 | Kullanıcı Davet Sistemi ve Kiracı Portalı | ✅ Tamamlandı | `phase-16-kullanici-davet-ve-kiraci-portal.md` |
| 17 | Yetki Kapsamı (Permission-Bazlı Taşınmaz Kapsamı) | ✅ Tamamlandı | `phase-17-yetki-kapsami.md` |
| 18 | Rezervasyon Sisteminin Genişletilmesi | ✅ Tamamlandı — kullanıcı kapanış onayı ve production migrationları | `phase-18-rezervasyon-sisteminin-genisletilmesi.md` |
| 19 | Sözleşme Başvuru, Onay ve Revizyon Mekanizması | ✅ Tamamlandı — kullanıcı kabulü, 172/172 regresyon | `phase-19-sozlesme-basvuru-onay-revizyon-mekanizmasi.md` |
| 20 | Kalem Bazlı Mağaza Yönlendirme ve Ödeme Altyapısı | 🟡 Uygulamada — İç Faz 1 kabul edildi, İç Faz 2 kullanıcı kabulünde | `phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md` |

---

## Ara Refactor'lar

| Refactor | Durum | Dosya | Not |
| --- | --- | --- | --- |
| BorcTipiDavranisi Ayrıştırma | ✅ Tamamlandı | `phase-9-1-borc-tipi-davranisi-refactor.md` | `ManuelTetiklemeli` ayrıştırıldı: `KullaniciManuel` ve `RezervasyonOzel` |
| Kaynak İsimlendirme Refactor | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md Ara Refactor bölümü) | `KaynakTipi`→`KalemKaynakTipi`; `Otomatik`→`Sozlesme`; `ManuelGiris=5`, `RezervasyonKurali=6` |
| BirimTuru × BorcTipi İlişkisi | ✅ Tamamlandı | `refactor-birim-turu-borc-tipi-iliskisi.md` | `BirimTuru.BorcTipiId` FK + cascade pasif; `TransferToTahakkukAsync` deterministik |
| Rezervasyon Yıllık Genel Tarifeleri | ✅ Tamamlandı | `refactor-rezervasyon-yillik-genel-tarife.md` | `RezervasyonGenelTarife` entity'si; `Tarife.Yil × BirimTuru` matrisi; `HesaplaAsync` precedence |
| Taşınmaz Tipi × Kiralama Şekli | ✅ Tamamlandı | `refactor-tasinmaz-tipi-kiralama-sekli.md` | `TasinmazTipi`'ne çoklu `KiralamaSekli` ara tablo; Ekle ekranında tipe göre filtreleme |
| SozlesmeRate Akışı Refactor | ✅ Tamamlandı | `refactor-sozlesme-rate-akisi.md` | Ekle tam tanım (Sabit/M2); Detay sekmesi kaldır; YenidenUret/Uzat popup'ta tarife; mevcut kayıtlar temizlendi |
| Magic String Temizliği | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md Ara Refactor bölümü) | `RoleNames`, `AppClaimTypes` sabitleri; 30+ dosyada hardcoded string → constant |
| CSS / Tailwind Migration | ✅ Tamamlandı | `refactor-css-tailwind-migration.md` | ~1.690 inline stil → Tailwind utility class; `<style>` bloğu → `app.css` |
| Repository Pattern — DTO Projeksiyon | ✅ Tamamlandı | `refactor-repository-pattern.md` | Tüm servisler `ApplicationDbContext`'ten ayrıldı (yalnız `SeedDataService` istisna); admin lookup CRUD ekranları DTO/ViewModel'e geçti; `_ViewImports.cshtml`'den entity `using` kaldırıldı |
| DB Optimizasyon (Faz 17 Sonrası) | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | Query filter perf index'leri (4×KiraciId), 13 check constraint, Castle DynamicProxy transaction pipeline (`ITransactionalService`), index isimlendirme; tarife tablolarına filtered unique index + check constraint |
| Kategori Sadeleştirme | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | Faz 14 C1'in kısmi geri alımı: `TasinmazTipi` ayrı tabloya çıkarıldı; `KategoriTipi` enum → `Kiraci=1, Sektor=2` |
| CRUD'larda Zorunlu Belge Kontrolü | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | `Kiraci/Ekle` POST: `BelgeTuru.Zorunlu` belgeler yüklenmeden geçilemez; client+server senkron validasyon |
| Kod Alanı Otomasyonu | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | `CodeSlugger` helper; 6 admin lookup'ta Kod = `ToCode(Ad)`; view/VM/Index'lerden Kod alanı tamamen kaldırıldı |
| Production Hazırlık | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | `BelgeTuru.Sistem` flag + `HasData` (ODEME_DEKONT); güçlü secret'lar; dev/prod DB ayrımı (`KiraTakipDb_Test`); migration sıfırlama (tek `InitialCreate`); kullanılmayan config alanları silindi |
| Kiracı Portal Tahakkuklarım Konsolidasyonu | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | 3 sekme (Borçlar+Ödemeler+Mutabakat) → tek "Tahakkuklarım"; iç kullanıcı `Tahakkuk/Index` pattern'i kiracıya uyarlandı; Ödeme Yap modal (2 sekme: Havale + POS placeholder); Ödeme Detayı modal; `TahakkukRepository` rezervasyon tahakkukları için birim/taşınmaz projeksiyonu eklendi |
| Kiracı Panel Redesign | ✅ Tamamlandı | `refactor-kiraci-panel.md` | "Paket Sadık" tasarımı (hero banner + KPI kartları + sparkline + ApexCharts bar/donut + timeline ödemeler + hızlı eylemler); ApexCharts + CountUp.js eklendi; hibrit yaklaşım — backend+skeleton Opus, view dolumu Sonnet |
| İç Kullanıcı Ana Sayfa Redesign | ✅ Tamamlandı | `refactor-ana-sayfa-redesign.md` | Slate hero gradient + 4 üst KPI (Δ% momentum + doluluk segment) + bugün vade dolan alert + 4 ödeme KPI (tahsilat oranı %) + nakit akışı bar + doluluk donut + 3 liste paneli (Top 5 Gelir Şampiyonları yeni) + permission'lı hızlı eylemler; sonrasında hero rengi teal'e çekildi, kart linkleri eklendi (Gecikmiş Tahakkuk / Aktif Sözleşme) |
| Permission Hiyerarşisi | ✅ Tamamlandı | `refactor-permission-hiyerarsisi.md` | `View` ve `Manage` action'ları kaldırıldı; GET endpoint'leri → 2. seviye modül izni, state-change POST'lar → 3. seviye action izni; UI ağaç dropdown ile parent-child checkbox; `asp-permission` tag helper; `AdminBypassHandler` `IsSuperAdmin` claim'i ile bypass; `PermissionTests` (9 test) ile katalog bütünlük ve handler davranışı doğrulandı |
| Tahakkuk Mimari Netleştirme | ✅ Tamamlandı | `refactor-tahakkuk-mimari-netlestirme.md` | Tahakkuk Kiracı + Birim çift merkezli (`BirimId` zorunlu); `Tahakkuk.RezervasyonId?` eklendi, `Rezervasyon.TahakkukId` kaldırıldı; manuel borç sözleşmeye opsiyonel bağlanabilir; `KapsamTipi.Birim` enum açıldı; `Davetiye.BirimIds` eklendi; `TahakkukMimariTests` (5 test) ile sözleşme/rezervasyon/manuel borç üretim akışı ve birim kapsam filtresi doğrulandı |
| Süper Admin ve Pure Claims Mimarisi | ✅ Tamamlandı | `refactor-super-admin-ve-pure-claims-mimari.md` | IsSuperAdmin flag, BypassHandler revizyonu, Module yetkisi temizliği, Prefix tabanlı menü erişimi, Operasyon Muduru veri izolasyon bağımlılığı temizliği |
| Kiracı Rol Mimarisi (Hybrid) | ✅ Tamamlandı | `refactor-kiraci-rol-mimarisi.md` | Kiracı Yöneticisi global sistem rolü olarak kalır, diğer tüm roller dinamiktir. |
| Merkezi Sistem Ayarları Altyapısı | ✅ Tamamlandı | (bkz. `PROGRESS.md`) | `SistemAyarlari` anahtar/değer tablosu; kod tabanlı tür ve validasyon tanımları; rezervasyon ve operasyonel politikalar için güçlü tipli provider; `Internal.Parameter` yetkili yönetim ekranı |
| Turkish to English Codebase Migration | ⚠️ Tarihsel işaretleri güvenilmez | `refactor-turkish-to-english-migration.md` | İlk toplu girişimin Faz 3/5/6/7 işaretleri güvenilir kabul edilmez. Yerine yürütülen Controller Bazlı Çeviri & Yapısal Refactoring Faz 1 ve validasyon taşıması Faz 2 doğrulanarak tamamlandı. |
| Merkezi Validasyon & Hata Yönetimi Altyapısı | ✅ Tamamlandı | (bkz. PROGRESS-HISTORY.md) | Input validation (`IValidator<T>` + `ValidationActionFilter`) ve business rule (`BusinessException`/`Guard`/`IBusinessRules`) için hibrit mimari; henüz hiçbir mevcut controller/servis/ViewModel'e uygulanmadı (inert altyapı). |
| Controller Bazlı Çeviri & Yapısal Refactoring | ✅ Tamamlandı | `refactor-controller-ceviri-ve-yapisal-refactoring.md` | Faz 1 kod kimliği/yapısal refactoring ve Faz 2 validasyon taşıması tamamlandı. DB kolon/tablo adlarına ve UI Türkçe metinlerine dokunulmadı. |

---

**Aktif Faz:** Faz 20 — Kalem Bazlı Mağaza Yönlendirme ve Ödeme Altyapısı. İç Faz 1, İç Faz 2 ve İç Faz 3 kabul edildi; İç Faz 4 planlanıyor.

## Genel Mimari Kararlar (Özet)

- **Hedef DB:** SQL Server (SQLite sadece geliştirme için kullanılmıştı)
- **Auth:** ASP.NET Core Identity + Claims tabanlı permission
- **Permission tablosu:** Ayrı `UserPermission` tablosu (audit için)
- **Yetki katmanları:**
  1. Rol (Admin / Yonetici / Goruntuleyici)
  2. Permission (Tasinmaz.Create vb.)
  3. Kapsam (UserTasinmazYetki — row-level)
- **Servis katmanı:** Interface tabanlı, EF Core
- **Ödeme:** Faz 20 kapsamında önce borç tipi + birim/taşınmaz/default kapsamından mağaza hesabı çözümleme ve kalem bazlı kısmi ödeme kurulacak; mevcut ödeme kanalları bu çekirdeğe taşındıktan sonra authenticated kiracı portalına soyutlanmış Paratika PayByLink ve mutabakat entegrasyonu eklenecek.
- **Sözleşme onay akışı:** Yeni sözleşme kaydı önce taslak başvuru olarak tutulacak; tahakkuk yalnız yetkili onayından sonra mevcut üretim servisiyle oluşturulacak; revizyon ve silme gerekçeleri ayrı çoklu geçmiş tablosunda korunacak.
- **Rezervasyon:** Faz 18 kapsamında KiraTakip iş kaynağıdır; tenant talebi, iç kullanıcı onayı/ret işlemi, yerel uygunluk, katılımcılar, tahakkuk ve otomatik tamamlama geliştirilecektir. Harici takvim entegrasyonu bu fazın kapsamında değildir.
- **Banka entegrasyonu:** Şimdilik CSV/Excel import. İleride API için `IBankaHareketiProvider` arayüzü kurulacak.

---

## Sıralama Mantığı

```
Faz 1: Permission entity TASARIMI (kod yazma yok, sadece model class'ları)
   ↓
Faz 2: SQL Server'a TEK DALGA migration (Identity + domain + permission tabloları birlikte)
   ↓
Faz 3: Domain servislerini EF Core'a taşı, DummyDataService → SeedDataService
   ↓
Faz 4: Permission'ı tam implement et (Claims, Policy, Middleware, Admin UI)
   ↓
Faz 5: Ödeme takip modülü
```

**Kritik kural:** Permission entity tasarımı (Faz 1) yapılır ama implementasyon Faz 4'te gelir. Böylece migration tek dalgada olur.

**Güncel yürütme sırası:** Yeni çalışma bekleniyor.

---

## Proje Sabitleri

- **Proje:** `KiraTakip/` (.NET 9.0 MVC)
- **Roller:** `Admin`, `Yonetici`, `Goruntuleyici`
- **DbContext:** `Data/ApplicationDbContext.cs` — `IdentityDbContext<ApplicationUser>`
- **Mevcut DB tablo:** `UserTasinmazYetkileri` + Identity tabloları
- **Veri kaynağı:** EF Core servisleri + `SeedDataService` (DummyDataService Faz 3'te kaldırıldı)
- **Connection string:** `appsettings.json` → `DefaultConnection` (SQL Server, Faz 2'de geçildi)

---

## Referanslar

- Permission listesi: `permission-catalog.md`
- Permission mimari: `permission-spec.md`
- Aktif görev durumu: `PROGRESS.md`
- Tamamlanmış çalışma tarihçesi: `PROGRESS-HISTORY.md`
- BorcTipiDavranisi Refactor: `phase-9-1-borc-tipi-davranisi-refactor.md`
- Faz 12 Stabilizasyon: `phase-12-stabilizasyon-ve-test-borcu.md`
- Faz 19 Sözleşme Başvuru, Onay ve Revizyon Planı: `phase-19-sozlesme-basvuru-onay-revizyon-mekanizmasi.md`
- Faz 18 Rezervasyon Sisteminin Genişletilmesi: `phase-18-rezervasyon-sisteminin-genisletilmesi.md`
- Faz 18 Yayın ve Kullanıcı Kabul Kontrol Listesi: `phase-18-yayin-ve-kullanici-kabul-kontrol-listesi.md`
- Faz 20 Kalem Bazlı Mağaza Yönlendirme ve Ödeme Altyapısı: `phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`
- Var olan domain spec'leri (sadece gerektiğinde):
  - `auth-spec.md` (572 satır)
  - `bina-ofis-birim-spec.md` (1170 satır)
  - `kiraci-sozlesme-finans-spec.md` (1248 satır)
  - `project-spec-canonical.md` (791 satır)


