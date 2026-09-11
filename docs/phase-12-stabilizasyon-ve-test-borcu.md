# Faz 12 — Stabilizasyon ve Test Borcu Temizliği

## Amaç
- Yeni ürün özelliği eklemek değil.
- Faz 4–11 arasında biriken test borçlarını kapatmak.
- Permission, ödeme, tahakkuk, fiyatlandırma, rezervasyon, manuel borç, UI ve dokümantasyon tutarlılığını doğrulamak.
- Üretime yakınlaşmadan önce kritik regresyon risklerini azaltmak.
- Eksik smoke testleri ve acceptance testleri tamamlamak.

## Karar Özeti

| Konu | Karar |
|---|---|
| Faz tipi | Stabilizasyon / test / regresyon |
| Yeni özellik | Hayır |
| Migration | Beklenmez; sadece hata bulunursa ayrıca değerlendirilir |
| UI geliştirme | Sadece eksik empty state, küçük UX düzeltmesi veya test sırasında çıkan hata varsa |
| Öncelik | Permission + ödeme/tahakkuk + fiyatlandırma resolver + görüntüleyici kapsam filtreleri |
| Faz çıktısı | Test edilmiş, dokümantasyonu güncel, kritik akışları doğrulanmış sistem |

## 1. Kapsam

- Permission akışı doğrulama
- Admin / Yonetici / Goruntuleyici rollerinin uçtan uca testi
- UserTasinmazYetki kapsam filtresi regresyon testi
- Ödeme, tahakkuk, dekont ve banka eşleştirme testleri
- Manuel borç ve rezervasyon tahakkuk entegrasyonu testleri
- Çok kalemli tahakkuk, pro-rata ve yeniden üretim testleri
- Fiyatlandırma hiyerarşisi ve parent fallback testleri
- BorcTipiDavranisi ayrıştırmasının regresyon testi
- Tablo/UX empty state ve opsiyonel export değerlendirmesi
- Dokümantasyon tutarlılığı ve eski terim kalıntılarını temizleme

## 2. Kapsam Dışı

- Yeni büyük modül geliştirme
- Yeni ödeme sağlayıcı veya banka API entegrasyonu
- Kiracı portalı
- Online ödeme
- E-fatura / e-arşiv
- Büyük UI redesign
- Multi-tenant mimari
- Yeni permission sistemi tasarımı

## 12.1 — Permission ve Rol Akışı Testi

- [ ] Admin tüm permission’ları bypass edebiliyor mu?
- [ ] Yonetici kendisine atanmış permission’larla işlem yapabiliyor mu?
- [ ] Goruntuleyici sadece view permission’larıyla ve sadece atanmış taşınmaz kapsamında veri görebiliyor mu?
- [ ] Permission olmayan kullanıcı ilgili route’a gidince AccessDenied/Forbid davranışı doğru mu?
- [ ] Admin kullanıcı düzenleme ekranında permission checkbox’ları rol bazlı doğru davranıyor mu?
- [ ] `permission-catalog.md` ile `Authorization/PermissionCatalog.cs` senkron mu?
- [ ] Controller policy attribute’ları eksik permission içeriyor mu?
- [ ] UI’da yetkisiz butonlar gizli mi?
- [ ] Direkt URL erişimi server-side engelleniyor mu?

## 12.2 — Görüntüleyici Kapsam Filtresi Regresyonu

- [ ] Goruntuleyici sadece atanmış taşınmazları listeliyor mu?
- [ ] Atanmamış taşınmaz detayına direkt URL ile erişemiyor mu?
- [ ] Kiracı listesi sadece atanmış taşınmazlardaki sözleşmelerden türeyen kiracıları gösteriyor mu?
- [ ] Sözleşme listesi sadece atanmış taşınmazların birimlerine ait sözleşmeleri gösteriyor mu?
- [ ] Tahakkuk ve ödeme listeleri kapsam filtresine uyuyor mu?
- [ ] Rezervasyon ve manuel borç listeleri kapsam filtresine uyuyor mu?
- [ ] Dashboard KPI’ları sadece yetkili kapsamdan hesaplanıyor mu?

## 12.3 — Ödeme ve Banka Eşleştirme Testleri

- [ ] Tahakkuk listesi açıldığında gecikme durumu doğru güncelleniyor mu?
- [ ] Ödeme girişi `OnayBekliyor` durumuyla oluşuyor mu?
- [ ] Ödeme onaylanınca tahakkuk `OdenenTutar` ve `Durum` doğru güncelleniyor mu?
- [ ] Kısmi ödeme sonrası `KismenOdendi` davranışı doğru mu?
- [ ] Tam ödeme sonrası `TamOdendi` davranışı doğru mu?
- [ ] Red işlemi `Reddedildi` ve red nedeni ile kaydediliyor mu?
- [ ] Dekont dosyası doğru klasöre kaydediliyor mu?
- [ ] Dekont görüntüleme yetki kontrolünden geçiyor mu?
- [ ] Akbank CSV import çalışıyor mu?
- [ ] Banka hareketi aday eşleştirme heuristik sıralaması doğru mu?
- [ ] Manuel eşleştirme kayıt oluşturuyor mu?
- [ ] Eşleşme çözme ödeme ve banka hareketi durumlarını doğru geri alıyor mu?
- [ ] Goruntuleyici eşleştirme yapamıyor, sadece yetkili kapsamda görüntülüyor mu?

## 12.4 — Tahakkuk Üretimi ve Yeniden Üretim Testleri

- [ ] Yeni sözleşme oluşturulunca sözleşme süresi boyunca aylık tahakkuklar oluşuyor mu?
- [ ] Her tahakkukta beklenen borç tipleri kalem olarak oluşuyor mu?
- [ ] `AylikSabit` borç tiplerinde rate bulunamazsa 0₺ kalem oluşuyor mu?
- [ ] `IlkAyTekSeferlik` borç tipleri sadece ilk ayda oluşuyor mu?
- [ ] Pro-rata ilk ay için doğru hesaplanıyor mu?
- [ ] Pro-rata son ay için doğru hesaplanıyor mu?
- [ ] KDV kalem bazlı doğru hesaplanıyor mu?
- [ ] Sözleşme uzatma yeni dönem tahakkuklarını idempotent üretiyor mu?
- [ ] Sözleşme fesih sonrası ödenmemiş gelecek tahakkuklar iptal ediliyor mu?
- [ ] Yeniden üretim ödenmiş tahakkuklara dokunmuyor mu?
- [ ] Yeniden üretim ödenmemiş tahakkukları güncel rate hiyerarşisine göre yeniden oluşturuyor mu?

## 12.5 — Fiyatlandırma Hiyerarşisi ve Resolver Testleri

Test edilecek precedence:

1. SozlesmeRate
2. BirimRate
3. TasinmazKiraciKategoriFiyat
4. Tarife / Genel Tarife
5. Yoksa null → composer 0₺ kalem

- [ ] Sözleşme override varsa en üst öncelikte kullanılıyor mu?
- [ ] Birim özel fiyatı sözleşme override yoksa kullanılıyor mu?
- [ ] Taşınmaz × Kiracı Kategorisi fiyatı doğru kategori için kullanılıyor mu?
- [ ] Genel Tarife kategori bazlı fallback olarak çalışıyor mu?
- [ ] Kiracı kategorisi olmayan durumda davranış kontrollü mü?
- [ ] Parent fallback default kategoriye atlamıyor mu?
- [ ] Rate bulunamadığında resolver null dönüyor mu?
- [ ] Composer null sonucu 0₺ kaleme çeviriyor mu?
- [ ] Snapshot alanları doğru yazılıyor mu?
- [ ] KaynakTipi doğru set ediliyor mu?

## 12.6 — BorcTipiDavranisi Refactor Regresyon Testi

- [ ] Kodda `ManuelTetiklemeli` kullanımı kalmadı mı?
- [ ] `KullaniciManuel = 3` mevcut manuel borç kayıtlarıyla uyumlu mu?
- [ ] `RezervasyonOzel = 4` TOPLANTI borç tipinde kullanılıyor mu?
- [ ] Manuel borç ekranında yalnızca `KullaniciManuel` borç tipleri görünüyor mu?
- [ ] Rezervasyon tahakkuka aktarımında `Kod == "TOPLANTI"` hard-code kullanılmıyor mu?
- [ ] Otomatik tahakkuk üretimi `KullaniciManuel` ve `RezervasyonOzel` borç tiplerini dışlıyor mu?
- [ ] Tarife ve fiyat matrislerinde manuel/rezervasyon özel borç tipleri listelenmiyor mu?
- [ ] Admin Borç Tipi ekranında davranış etiketleri doğru mu?

## 12.7 — Rezervasyon ve Manuel Borç Testleri

- [ ] Rezervasyon sadece rezervasyon yapılabilir birimler için oluşturuluyor mu?
- [ ] Çakışan rezervasyon engelleniyor mu?
- [ ] Ücretsiz süre doğru uygulanıyor mu?
- [ ] Ücretli süre periyot bazlı doğru yuvarlanıyor mu?
- [ ] Rezervasyon tahakkuka aktarılınca `KaynakTipi=Rezervasyon` oluyor mu?
- [ ] Tahakkuka aktarılmış rezervasyon iptalinde bağlı tahakkuk doğru yönetiliyor mu?
- [ ] Manuel borç oluşturulunca ayrı tahakkuk oluşuyor mu?
- [ ] Manuel borç iptal edilebiliyor mu?
- [ ] Ödeme alınmış manuel borç iptal edilemiyor mu?
- [ ] Manuel ve rezervasyon kaynaklı tahakkuklar ödeme ekranlarında normal tahakkuk gibi takip ediliyor mu?

## 12.8 — UI / UX Stabilizasyonu

- [ ] Mobil sidebar çalışıyor mu?
- [ ] Confirm modal tüm kritik POST işlemlerinde kullanılıyor mu?
- [ ] Tooltip’ler hatasız çalışıyor mu?
- [ ] Server-side pagination URL state koruyor mu?
- [ ] Client-side pagination/filter küçük tablolarda çalışıyor mu?
- [ ] Empty state eksik olan kritik listeler tamamlandı mı?
- [ ] Export/bulk action opsiyonellerinden hangilerinin kalacağı netleşti mi?
- [ ] Tailwind/Magic UI tasarım dili bozulmuş sayfa var mı?
- [ ] Bootstrap/jenerik MVC görünümü kalıntısı var mı?

## 12.9 — Dokümantasyon ve Terminoloji Temizliği

- [ ] `MASTER-PLAN.md` Faz 12 aktif olarak güncellendi mi?
- [ ] `PROGRESS.md` Faz 12 checklist’i eklendi mi?
- [ ] `phase-9-1-borc-tipi-davranisi-refactor.md` referansları doğru mu?
- [ ] “Global Kural” metin kalıntıları temizlendi mi?
- [ ] “Ofis Bazlı” yerine “Birim Bazlı” kullanımı tutarlı mı?
- [ ] `ManuelTetiklemeli` dokümantasyonlarda yalnızca tarihsel not olarak geçiyor mu?
- [ ] `permission-catalog.md` güncel mi?
- [ ] Tarihsel spec’lerde gerekli güncellik notları var mı?

## 12.10 — Final Smoke Test

- [ ] `dotnet build` hatasız
- [ ] Uygulama açılıyor
- [ ] Login/logout çalışıyor
- [ ] Admin temel CRUD ve yönetim ekranlarına erişiyor
- [ ] Yonetici yetkili işlemleri yapabiliyor
- [ ] Goruntuleyici sadece görüntüleme ve kapsamlı erişim yapabiliyor
- [ ] Yeni taşınmaz + birim + kiracı + sözleşme akışı çalışıyor
- [ ] Tahakkuk üretimi çalışıyor
- [ ] Ödeme girişi/onay/red çalışıyor
- [ ] Banka hareketi import/eşleştirme/çözme çalışıyor
- [ ] Rezervasyon oluşturma/tahakkuka aktarma çalışıyor
- [ ] Manuel borç oluşturma/iptal çalışıyor
- [ ] Dashboard KPI’ları hata vermeden hesaplanıyor
- [ ] Kritik listelerde 404/500 yok
- [ ] Browser console kritik hata içermiyor

## Risk Listesi

| Risk | Etki | Önlem |
|---|---|---|
| Permission testleri eksik kalır | Yetkisiz erişim/veri sızıntısı | Rol + permission + kapsam matrisiyle test |
| Görüntüleyici kapsam filtresi bazı servislerde unutulur | Veri güvenliği açığı | Servis seviyesinde UserTasinmazYetki kontrolü |
| Ödeme/tahakkuk durumları tutarsız kalır | Finansal raporlar yanlış olur | Onay/red/gecikme/kısmi ödeme senaryolarını test et |
| Resolver fallback yanlış çalışır | Yanlış kira tahakkuku | Precedence testlerini ayrı ayrı çalıştır |
| Manuel/rezervasyon borç tipi ayrımı bozulur | Çift kayıt veya yanlış listeleme | BorcTipiDavranisi regresyon testi |
| Tarihsel dokümanlar LLM’i yanıltır | Eski mimari geri gelir | Güncellik notları ve MASTER-PLAN önceliği |
| Opsiyonel Faz 6 işleri kapsamı büyütür | Stabilizasyon fazı uzar | Sadece empty state/export gibi düşük riskli işler seçilir |

## Çıktılar

- Güncellenmiş test/stabilizasyon checklist’i
- Doğrulanmış permission ve kapsam davranışı
- Doğrulanmış ödeme/tahakkuk/mutabakat akışı
- Doğrulanmış fiyatlandırma resolver hiyerarşisi
- Temizlenmiş dokümantasyon referansları
- Güncellenmiş `MASTER-PLAN.md`
- Güncellenmiş `PROGRESS.md`
