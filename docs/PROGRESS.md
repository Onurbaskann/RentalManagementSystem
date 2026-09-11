# PROGRESS — Aktif Görev Takibi

> Bu dosya yalnız güncel çalışma bağlamını tutar. Tamamlanan çalışmaların ayrıntıları
> ilgili phase/refactor dosyalarında bulunur. Tarihsel bir karar gerektiğinde
> [`PROGRESS-HISTORY.md`](PROGRESS-HISTORY.md) yalnız ilgili bölüm için okunur; dosya artık güncellenmez.

**Aktif çalışma:** Kalem bazlı mağaza yönlendirme ve ödeme altyapısı  
**Aktif faz:** Faz 20 / İç Faz 6 — Provider soyutu ve sanal POS işlem omurgası (implementasyon tamamlandı, kullanıcı onayı bekleniyor)  
**Durum:** İç Faz 6 implementasyonu tamamlandı; `dotnet build`/`dotnet test` tam yeşil (352/352, 0 skip)  
**Detaylı plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)  
**Tamamlanan İç Faz 1 planı:** [`phase-20-inner-phase-1-implementation-plan.md`](phase-20-inner-phase-1-implementation-plan.md)  
**Tamamlanan İç Faz 2 planı:** [`phase-20-inner-phase-2-implementation-plan.md`](phase-20-inner-phase-2-implementation-plan.md)  
**Tamamlanan İç Faz 3 planı:** [`phase-20-inner-phase-3-implementation-plan.md`](phase-20-inner-phase-3-implementation-plan.md)  
**Tamamlanan İç Faz 4 planı:** [`phase-20-inner-phase-4-implementation-plan.md`](phase-20-inner-phase-4-implementation-plan.md)  
**Tamamlanan İç Faz 5 planı:** [`phase-20-inner-phase-5-implementation-plan.md`](phase-20-inner-phase-5-implementation-plan.md)  
**İç Faz 6 planı (onay bekliyor):** [`phase-20-inner-phase-6-implementation-plan.md`](phase-20-inner-phase-6-implementation-plan.md)  
**Sonraki adım:** Kullanıcı İç Faz 6 sonucunu kontrol edip onaylayacak; onay sonrası İç Faz 7 (Paratika PayByLink entegrasyonu) planlanacak

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
- [ ] Faz 20 kalem bazlı mağaza yönlendirme ve ödeme altyapısında İç Faz 1-5 kabul edildi; İç Faz 6 implementasyonu tamamlandı, kullanıcı onayı sırada.

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
