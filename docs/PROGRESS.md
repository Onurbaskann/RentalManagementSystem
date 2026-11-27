# PROGRESS — Aktif Görev Takibi

> Bu dosya yalnız güncel çalışma bağlamını tutar. Tamamlanan çalışmaların ayrıntıları
> ilgili phase/refactor dosyalarında bulunur. Tarihsel bir karar gerektiğinde
> [`PROGRESS-HISTORY.md`](PROGRESS-HISTORY.md) yalnız ilgili bölüm için okunur; dosya artık güncellenmez.

> **Yeni çekirdek iş (2026-11-24) — Alan hiyerarşisi:** Taşınmaz/birim ve kontenjan yapısı serbest derinlikli alan ağacına (parent-child) taşınacak. Kararlar K1–K70 (Vault analizi rev. 8). **F0 tamam: uygulama spec'i yazıldı** (2026-11-26) — [`refactor-alan-hiyerarsisi.md`](refactor-alan-hiyerarsisi.md). Dal `alan-hiyerarsisi`, klasör `KiraTakip-alan`, DB yalnız `DBTeknoPark_Test`. Codex spec denetimi B01–B10 spec'e işlendi (2026-11-26). **F1 tamam (2026-11-26):** DB adı kilidi (`BranchDatabaseGuard`, factory + Web açılışı, S-14); 8 alan tablosu + migration `AddSpaceTree`; `SpaceTreePolicy`/`SpaceChangePolicy`; `SpaceService`/`SpaceTypeService`/`SpaceAttributeService`/`SpaceRateService`, kök kilidi; `/Space` ağaç ekranı, Alan Türleri/Özellikleri ekranları; `Space*` izinleri; alan seed'i; Development'ta eksik sistem ayarlarını varsayılanla yazan seed adımı (migration'larda 8 ayar yoktu). Build temiz, tam paket 1291/1291, birleşik denetim ONAY; ekranlar tarayıcıda denenmedi; commit yok. **F2a başladı (2026-11-26):** parça kartları [`refactor-alan-f2a-parcalar.md`](refactor-alan-f2a-parcalar.md) (P0 envanter, D1–D20 kararları, P1a–P7d). **P1 tamam:** P1a K55 kiracı kategorisi NOT NULL (migration `MakeTenantCategoryRequired`), P1b `SpaceAccessScope`/`SpaceScopeService`/`ReservationSpacePolicy`, P1c `SpaceRateResolver` + `SpaceTestSeed`; build temiz, 1306/1306, üç denetim ONAY; commit yok. **P2, P3a, P4, P3b, P5 tamam + Codex P2–P5 denetimi (B01–B05, Ş01) ve Codex denetimi 2 (N01, N02; N03 → P7a) düzeltildi (2026-11-27, opus denetimleri ONAY; commit yok). **P6 tamam (2026-11-27):** rapor/istatistik/hatırlatma/doküman/banka servisleri ve ana sayfa alana geçti; S2 doluluk kök alan başına; iki faz + son denetim ONAY; commit yok. **P7a tamam (2026-11-27):** 108 eski dosya silindi, izinler (N03 + BorcHatirlatma ScopeAware), seed alana taşındı, migration `ReplacePropertyUnitWithSpace`, DB drop+update+seed hatasız; src build temiz (tests P7b'de); üç faz + son denetim ONAY; commit yok. **P7e tamam (2026-11-27):** 16 yeni endpoint izni + ekran izin kontrolleri + eski metinler (yardım hariç); iki faz + son denetim ONAY; seed hatasız. Sıradaki: P7b. Kart dosyasında "… sonrası" notları sonraki kartların işini taşır.**

> **Sıradaki oturumun işi (2026-11-17):** Paylaşımlı ve açık birim [uygulama planı](refactor-paylasimli-acik-birim-uygulama-plani.md) onaylandı; A1–A6 yazıldığı gibi geçerli. **F1 tamamlandı** (domain politikaları + ondalık ayar altyapısı; commit `d787d45`). **F2 tamamlandı** (şema + `AddSharedAndCapacityUnits`; commit `cae168a`; migration production'a henüz uygulanmadı, F3 ile birlikte uygulanmalı). **F3 tamamlandı** (birim anlık bilgisi, taslak çakışması, görev ayrılığı kaldırıldı; commit `7f7fb5e`). **F4 tamamlandı** (birim türü alt kırılımı ve özellik kilidi, kontenjan girişi/kuralları/tarihçe, alan ve tür kilidi; tek birimli taşınmaz da kontenjan bazlı olabiliyor; commit `81b527d`). **F5 tamamlandı** (tahakkuk/istatistik kiralanan m²'yi kullanıyor; commit `246fffe`). **Unique index kaldırıldı:**
- migration `RemoveLeaseOpenApplicationUniqueIndex`; test DB'ye uygulandı, production'a uygulanmadı
- normal birimde tek açık başvuru kuralı serviste duruyor
- commit `a96a469`

**F6 tamamlandı** (kontenjan bazlı birimde sözleşme: kapasite kontrolü, kiralanan/fiziksel m², `LeaseArea` yazımı, formda boş kontenjan/m² ve son kontenjan önerisi); commit `dcc5f1f`.

**F7a tamamlandı** (commit 55af9ea). **F7b tamamlandı** (paylaşımlı grup: uzatma, fesih, yeniden yapılandırma; eski grup ödeme/depozito ön koşulu; 3 yeni izin; grup detayında modallar ve yeniden yapılandırma formu); commit f199f7d. **F8 tamamlandı** (açık birim doluluğu: Kısmen Dolu durumu, kesirli adet ve m² oranı, kontenjan göstergesi; ana sayfa, taşınmaz listesi ve detayında rozet; kiracı panelinde kiralanan m² gösterimi); commit 273d6ee. **F9 son doğrulama:** build + tam paket 1132/1132, birleşik denetim ONAY. Denetim bulgusuyla uzatmaya "birim sözleşmeden sonra değişmişse uzatma yok" kontrolü eklendi; commit edilmedi. Production'a uygulanacak migration'lar: `AddLeaseExpirationActivityType`, `AddSharedAndCapacityUnits`, `RemoveLeaseOpenApplicationUniqueIndex`.

**Sıradaki:** Açık ofis v2 Aşama 1. Kod incelemesi tamamlandı (2026-11-18; rapor `Downloads/teknopark_acik_ofis_v2_kod_incelemesi.md`; Min artışı kararı: taslak ayarı engellemez). Uygulama planı [`refactor-acik-ofis-v2-uygulama-plani.md`](refactor-acik-ofis-v2-uygulama-plani.md) onaylandı; **Aşama 1 (G1–G8) tamamlandı** (2026-11-18; tam paket 1188/1188, `dotnet build KiraTakip.slnx` temiz; commit yok; `KiraTakipDb` prod'a migration `AddLeaseAreaModeAndHistory` uygulanmadı; ekranlar tarayıcıda denenmedi). Yarım kalan F9 açık not kararları: [`refactor-acik-ofis-v2-devir.md`](refactor-acik-ofis-v2-devir.md). Her faz sonunda kullanıcıdan "devam" beklenir. Kaynak: [`refactor-paylasimli-acik-birim.md`](refactor-paylasimli-acik-birim.md). Faz 20 Paratika blokajda olduğu için bu iş öne alındı.

**Aktif çalışma:** Kalem bazlı mağaza yönlendirme ve ödeme altyapısı (blokajda)  
**Aktif faz:** Faz 20 / İç Faz 7 — Paratika Hosted Payment Page entegrasyonu (implementasyon tamamlandı, manuel duman testi ve kullanıcı onayı bekleniyor)  
**Durum:** İç Faz 7 implementasyonu tamamlandı (backend + kiracı arayüzü tetikleyicisi); `dotnet build`/`dotnet test` tam yeşil (776/776, 0 skip). Proje bu arada Clean Architecture katmanlarına ayrıldı (`src/KiraTakip.{Domain,Application,Infrastructure,Web}`).
**BLOKAJ (2026-09-14):** DURMA KAPISI C (manuel duman testi) Paratika test merchant kimlik bilgisi (MERCHANT/MERCHANTUSER/MERCHANTPASSWORD) eksikliğinden bekliyor. Bu bilgi genel API dokümanında yayınlanmaz — Paratika/Payten'den hesaba özel bir test/sandbox üye iş yeri kaydı talep edilmesi gerekiyor. Test kartları dokümanda var ama kimlik bilgisi olmadan hiçbir istek (SESSIONTOKEN dahil) atılamıyor. Kullanıcı kararı: kimlik bilgisi gelene kadar burada durulacak, İç Faz 8'e şimdilik geçilmeyecek.  
**Detaylı plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)  
**Tamamlanan İç Faz 1 planı:** [`phase-20-inner-phase-1-implementation-plan.md`](phase-20-inner-phase-1-implementation-plan.md)  
**Tamamlanan İç Faz 2 planı:** [`phase-20-inner-phase-2-implementation-plan.md`](phase-20-inner-phase-2-implementation-plan.md)  
**Tamamlanan İç Faz 3 planı:** [`phase-20-inner-phase-3-implementation-plan.md`](phase-20-inner-phase-3-implementation-plan.md)  
**Tamamlanan İç Faz 4 planı:** [`phase-20-inner-phase-4-implementation-plan.md`](phase-20-inner-phase-4-implementation-plan.md)  
**Tamamlanan İç Faz 5 planı:** [`phase-20-inner-phase-5-implementation-plan.md`](phase-20-inner-phase-5-implementation-plan.md)  
**İç Faz 6 planı:** [`phase-20-inner-phase-6-implementation-plan.md`](phase-20-inner-phase-6-implementation-plan.md) (implementasyon tamamlandı; kullanıcı yazılı "onaylıyorum" vermeden "Faz 7'ye geçelim" diyerek ilerledi)  
**İç Faz 7 planı (onay bekliyor):** [`phase-20-inner-phase-7-implementation-plan.md`](phase-20-inner-phase-7-implementation-plan.md)  
**Sonraki adım:** Kullanıcı Paratika/Payten'den test merchant kimlik bilgisi (MERCHANT/MERCHANTUSER/MERCHANTPASSWORD) aldığında bu sohbete dönülüp gerçek Paratika test ortamına karşı manuel duman testi yapılacak (ApiBaseUrl/HostedPaymentPageBaseUrl/RETURNURL alan adı hiçbiri gerçek sunucuya karşı doğrulanmadı); ardından kullanıcı İç Faz 7 sonucunu onaylayacak, onay sonrası İç Faz 8 (callback, mutabakat, operasyonel dayanıklılık) planlanacak

**Paralel çalışma (2026-09-21):** Tasarım dili (navy marka rengi, desenli zemin, keskin radius, Bricolage+Inter) Figma Make'te "Tahakkuklar" ekranıyla onaylandı — [`design-system-brief.md`](design-system-brief.md) Bölüm D, tam kaynak [`design-reference/tahakkuklar-approved.css`](design-reference/tahakkuklar-approved.css). Faz 1-4 uygulandı (token'lar `tokens.css`, `app.css`, layout/sidebar, dashboard'lar, giriş/hata sayfaları); Razor derlemesi geçti, görsel kontrol kullanıcıda, commit'lenmedi. Faz 5 de tamam (e-posta şablonları, `uppercase` taraması, indigo→navy [tarife alanları hariç], alias ve `site.css` temizliği). Merkezileştirme (2026-09-21): renkler tek kaynak `tokens.css` (`--rgb-*`; Tailwind config `_BrandHead` bunları okur), toast tek tanım + `.toast-*` sınıfları, `<style>` blokları/inline font/SVG hex temizlendi, ölü `_Layout.cshtml.css` silindi. Eski ~128 inline style ve 77 arbitrary hex de taşındı (e-posta hariç `style=` yalnız 8 dinamik genişlik; `:style` yok; arbitrary hex 0; `font-serif` → `font-display`; Razor renk değişkenleri sınıf adı üretiyor). Dashboard'lar (Ana Sayfa + Kiracı Paneli) Figma tasarımına göre yeniden yazıldı (`.stat-rail`, `.section-label`, `.link-more`, `.action-tile`; topbar yapılmıyor). Sidebar daraltma (240↔72px, `sidebar-init.js`, `html.sidebar-collapsed`) ve yeni kullanıcı menüsü eklendi. Liste/detay tabloları satır tıklamasına geçti (`tr[data-href]` + `.cell-link` ile klavye erişimi; Kiracı Detay Sözleşmeler, Tahakkuk Detay Ödeme Geçmişi, Rezervasyonlar [Başlık kolonu, `İptal Edildi` filtresi + gizli-kayıt notu]); Ödeme Detay geri bağlantısı ilgili tahakkuka gider; sidebar'a Tahakkuklar altında Ödemeler alt öğesi eklendi. Commit edildi (`bcd9fd9`). Kalan: görsel kontrol (özellikle iki dashboard, SelectMatch, AdminReservationRateRule, Lease/Details, Property/Details, Payment/Create, tarih seçici).

**Paralel çalışma (2026-11-16):** Vault'taki yardım dokümanı `/Help` Yardım Merkezi olarak uygulamaya taşındı: `HelpController` + `Help/HelpSections.cs` (15 bölüm kataloğu, kiracıya sunucu tarafında yalnız kiracı/ortak bölümler), `Views/Help/Index.cshtml` + `Views/Help/Sections/*` (içerik gerçek ekranlarla doğrulanıp düzeltildi), `help.css`/`help.js`, `wwwroot/images/help/`. Kullanıcı menüsünde "Şifre Değiştir" altına "Yardım"; her ekranda `_HelpButton` ile controller'a göre ilgili bölüme giden `?` butonu (`app.css` `.help-fab`: main-content üst dolgusunda sağ üst köşe, `.main-content{position:relative}`). Build temiz, `HelpControllerTests` 3/3. Commit edildi (`505c03a`). Kalan: tarayıcıda görsel kontrol (özellikle ? butonu konumu, tarife rozetleri).

**Paralel çalışma (2026-11-16) — Birim/sözleşme düzeltmeleri:** Yapısal düzenleme öncesi analizde çıkan üç sorun düzeltildi. (1) Bitişi geçmiş Aktif sözleşmeler `LeaseExpirationBackgroundService` ile saatlik olarak `Ended` yapılıyor (`LeaseActivityType.Expiration=7`, ayar `LeaseExpiration`); `Ended` sözleşme bugünden sonraki bir tarihe uzatılabiliyor ve yeniden `Active` oluyor; fesih/vade/tahakkuk yalnız `Active`'te. (2) Birim çakışma kontrolü "şu an aktif" yerine tarih aralığına göre (`HasOverlappingActiveLeaseAsync`; oluşturma, taslak güncelleme, yeniden gönderme, onay, uzatma). (3) `UnitType.Code` immutable; ad benzersizliği ayrı kontrol, kod çakışmasında `-2` eki (silinmiş kayıtlar dahil). Migration: `AddLeaseExpirationActivityType` (yalnız kolon açıklaması; test DB'ye uygulandı, production'a uygulanmalı). `BankTransactionMatchingTests` paylaşılan DB kalıntısına karşı izole edildi. `dotnet test` 972/972. Bilinçli bırakılanlar: sözleşme formundaki birim listesi dolu birimleri hâlâ göstermiyor (gelecek tarihli sözleşme yalnız arka uçta mümkün); `Ended` sözleşme uzatmasında arada başka bir `Ended` sözleşmenin dönemi çakışma kontrolüne girmiyor. Commit edildi (`a09221f`). Yapısal düzenleme isteği, aşağıdaki paylaşımlı/açık birim çalışmasına dönüştü.

**Paralel çalışma (2026-11-16) — Paylaşımlı ve açık birim (gereksinim analizi):** Bir birimin m² bazlı olarak birden fazla kiracıya kiralanması isteniyor. İki model var: paylaşımlıda grup sözleşme anında kurulur ve bütün olarak hareket eder; açık birimde ise kontenjan ve fiziksel m² ile tanımlanan bir kapasite zaman içinde bağımsız sözleşmelerle doldurulur. Gereksinim ve veri modeli kararları [`refactor-paylasimli-acik-birim.md`](refactor-paylasimli-acik-birim.md) dosyasında. Analiz 2026-11-17'de tamamlandı: doluluk, rezervasyon ilişkisi, kiracı paneli ve yetkiler de dokümanda. Kod değişikliği yok. `CLAUDE.md` dosyasına "her write endpoint'in kendi yetkisi" kuralı eklendi. 16 açık nokta 2026-11-17'de kullanıcıyla kararlaştırıldı ve dokümana işlendi. Bunlara ek olarak şunlar kararlaştırıldı: birim türüne alt kırılım (Birim bazlı / Kontenjan bazlı); sözleşmesi olan türün özellik kilidi; sözleşmede birim anlık bilgisi; tüm birimlerde taslağın çakışmaya girmesi. **Sonraki adım:** Uygulama planı hazırlanacak (orchestration akışıyla onaya sunulacak).

---

## Devam ederken okunacaklar

1. Bu dosya.
2. Aktif çalışma oluşturulduktan sonra yalnız ilgili phase dosyası.
3. Aktif çalışmanın doğrudan etkilediği kod ve test dosyaları.
4. Genel faz bağımlılığı gerekiyorsa `MASTER-PLAN.md` dosyasının yalnız ilgili bölümü.
5. Tarihsel karar gerekiyorsa `PROGRESS-HISTORY.md` dosyasının yalnız ilgili bölümü.

Aktif faz dışındaki büyük phase/spec dosyaları bağlam amacıyla baştan sona okunmaz.

---

## Son doğrulanmış proje durumu

- [x] Controller bazlı Türkçe→İngilizce kod kimliği ve yapısal refactoring tamamlandı.
- [x] Controller validasyon taşıması tamamlandı.
- [x] Permission etiketleri `PermissionCatalog` içinde merkezileştirildi ve rol izin ağacı genel amaçlı `TreePicker` bileşenine dönüştürüldü.
- [x] Faz 19 sözleşme başvuru, onay ve revizyon mekanizması tamamlandı.
- [x] Faz 18 rezervasyon sisteminin genişletilmesi tamamlandı; kullanıcı kapanış onayı 2026-08-17 tarihinde verildi ve production migrationları uygulandı.
- [x] Merkezi `SistemAyarlari` altyapısı ile rezervasyon ve operasyon ayarları tamamlandı.
- [ ] Faz 20 kalem bazlı mağaza yönlendirme ve ödeme altyapısında İç Faz 1-5 kabul edildi, İç Faz 6 implementasyonu tamamlanıp bir sonraki faza geçildi; İç Faz 7 implementasyonu tamamlandı, manuel duman testi ve kullanıcı onayı sırada.

---

## Tamamlanan çalışmaların kayıtları

### Faz 18 — Rezervasyon Sisteminin Genişletilmesi

- Durum: Tamamlandı ve doğrulandı.
- Kapsam: Yerel rezervasyon yaşam döngüsü, uygunluk, tenant talebi, onay/ret, güncelleme/iptal, tahakkuk bağlantısı, otomatik tamamlama ve kontrollü yayın.
- Detaylı plan: [`phase-18-rezervasyon-sisteminin-genisletilmesi.md`](phase-18-rezervasyon-sisteminin-genisletilmesi.md)
- Yayın/UAT: [`phase-18-yayin-ve-kullanici-kabul-kontrol-listesi.md`](phase-18-yayin-ve-kullanici-kabul-kontrol-listesi.md)
- Kısa test rehberi: [`phase-18-kisa-test-rehberi.md`](phase-18-kisa-test-rehberi.md)
- Devredilen operasyon notu: Windows Server üzerindeki rezervasyon tamamlama worker'ı gerçek yayın sonrasında ayrıca izlenmelidir.

### Faz 19 — Sözleşme Başvuru, Onay ve Revizyon Mekanizması

- Durum: Tamamlandı ve doğrulandı.
- Detaylı plan: [`phase-19-sozlesme-basvuru-onay-revizyon-mekanizmasi.md`](phase-19-sozlesme-basvuru-onay-revizyon-mekanizmasi.md)
- Yayın ve geri dönüş: [`phase-19-release-runbook.md`](phase-19-release-runbook.md)
- Kalan iyileştirme: Kalıcı teknik log sink'i ayrı bir yayın iyileştirmesi olarak değerlendirilebilir.

### Merkezi Sistem Ayarları Altyapısı

- Durum: Tamamlandı ve doğrulandı.
- `SistemAyarlari` anahtar/değer tablosu, güçlü tipli politika sağlayıcısı ve yönetim ekranı eklendi.
- Rezervasyon ile operasyonel süre/tolerans ayarları merkezi altyapıya taşındı.
- `AddSystemSettings` ve `AddOperationalSystemSettings` migrationları test veritabanında doğrulandı; yeni sürümden önce production'a uygulanmalıdır.

### Diğer tamamlanan refactoring çalışmaları

- Detaylı kayıt: [`refactor-controller-ceviri-ve-yapisal-refactoring.md`](refactor-controller-ceviri-ve-yapisal-refactoring.md)

---

## Güncelleme kuralları

- Bu dosyada yalnız aktif çalışma, üst seviye durum, sıradaki adım ve gerçek blokajlar tutulur.
- Atomik checklist, iş kuralları, teknik kontratlar ve test ayrıntıları ilgili phase dosyasında kalır.
- Faz tamamlandığında burada kısa bir sonuç ve ilgili doküman bağlantısı bırakılır.
- Yeni faz başladığında üst bölümdeki aktif çalışma, aktif faz, durum, detaylı plan ve sonraki adım birlikte güncellenir.
- Uzun uygulama günlüğü, dosya diff'i ve ham test çıktısı bu dosyaya eklenmez.
