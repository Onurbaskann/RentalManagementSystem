# Faz 20 / İç Faz 4 — Mevcut Ödeme Kanallarının Uyarlanması Implementation Plan

**Durum:** Kullanıcı tarafından kabul edildi (2026-08-31); `dotnet build` ve `dotnet test` tam yeşil (333/333, 0 skip).
**Üst plan:** [`phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md`](phase-20-kalem-bazli-magaza-yonlendirme-ve-odeme-altyapisi.md)
**Ön koşul:** İç Faz 3 tamamlandı ve kullanıcı tarafından kabul edildi (2026-08-28).
**Kapsam:** İç kullanıcı manuel ödeme ekranında ve kiracı dekont bildirme modalında kalem seçimi
(radio grubu), banka hareketi → mağaza hesabı zorunluluğu, eşleştirme adaylarının mağaza hesabına
göre filtrelenmesi, iç kullanıcı ekranlarında (yalnız iç kullanıcı) kalem/mağaza bilgisinin
görünür kılınması, eski opsiyonel `ChargeLineItemId` sözleşmelerinin sıkılaştırılması.
**Kapsam dışı:** Authenticated kiracı ödeme deep-link deneyimi (İç Faz 5), provider soyutu / sanal
POS (İç Faz 6/7/8), production migration, iptal/iade.

---

## 1. Veri modeli ve migration

`BankTransaction` (`BankaHareketleri`) tablosuna zorunlu `StoreAccountId`
(`MagazaHesapBilgisiId`, FK → `StoreAccount`, Restrict) eklendi. Migration
`AddBankTransactionStoreAccount`, İç Faz 3'teki desenle aynı sırayı izler: nullable ekle →
mevcut eşleşmiş ödemenin mağaza hesabından backfill → `MagazaHesapBilgisiId IS NULL` guard
(`THROW 51000`) → NOT NULL → index (`IX_BankaHareketleri_MagazaHesapBilgisiId`) → FK
(`FK_BankaHareketleri_MagazaHesapBilgileri_MagazaHesapBilgisiId`, Restrict).

Test DB'de backfill ile çözülemeyen 2 dummy `BankaHareketleri` satırı vardı (`OdemeBankaEslesmeleri`
İç Faz 3'te tamamen temizlenmişti); kullanıcının önceki genel onayı ("hepsi dummy, silinebilir")
kapsamında migration öncesi sqlcmd ile temizlendi, sonra migration uygulandı. Preflight script:
[`migration-scripts/phase-20-inner-phase-4-preflight.sql`](migration-scripts/phase-20-inner-phase-4-preflight.sql).

---

## 2. Kalem seçimi — iç kullanıcı ve kiracı

`CreatePaymentInput`/`ReportTenantPaymentInput`'taki `ChargeLineItemId: int?` parametresinin
**varsayılan değeri kaldırıldı** — tip aynı kalır (tek-kalemli tahakkukta otomatik seçim İç Faz
3 kararı olarak korunur), yalnız "sessizce atlanabilirlik" ortadan kalktı.

- `Views/Payment/Create.cshtml`: ödenebilir kalemler (`AvailableAmount > 0`) için radio kart
  grubu; Alpine (`odemeKalemSecimi()`) seçilen kaleme göre tutarı ve `:max` sınırını günceller.
  Ödenebilir kalem yoksa uyarı gösterilip Kaydet butonu devre dışı kalır.
  `PaymentController.Create` (GET) artık çok kalemli tahakkukta `Amount = 0` döner (eski
  `charge.TotalAmount - charge.PaidAmount` düşümü kaldırıldı — kalem seçilmeden tutar dolmaz).
- `Views/TenantCharge/Details.cshtml`: aynı görsel desenle kiracı dekont modalına kalem radio
  grubu eklendi (`tenantKalemSecimi()`); modal başlığı "Seçili kalem kalanı" gösterir. Mağaza/hesap
  bilgisi bu ekranda **hiçbir zaman** gösterilmez.
- `CreatePaymentViewModelValidator` ve `TenantChargePaymentFormViewModelValidator`'a
  `ChargeLineItemId is not > 0` kuralı eklendi.

---

## 3. Banka hareketi mağaza zorunluluğu ve eşleştirme filtresi

- İçe aktarma formu (`Views/BankTransaction/Import.cshtml`) artık zorunlu "Hedef Mağaza"
  dropdown'u içerir (`StoreRoutingOptionDto`, İç Faz 2'den değiştirilmeden yeniden kullanıldı).
  `BankTransactionService.ImportAsync` seçilen mağazanın aktif hesabını çözer
  (`BANK_IMPORT_STORE_ACCOUNT_NOT_FOUND`) ve CSV'deki her hareketi o hesaba bağlar; boş CSV
  `BANK_IMPORT_NO_TRANSACTIONS` ile reddedilir.
- `PaymentMatchingBasisDto`/`PaymentMatchingContextDto` üçüncü alan olarak `StoreAccountId`
  taşır; `PaymentAllocationRepository.GetCandidatesAsync` ve
  `BankTransactionRepository.GetTransactionCandidatesAsync` aday listelerini
  `StoreAccountId` eşitliğiyle filtreler.
- `BankTransactionService.MatchAsync`, hareketle ödemenin mağaza hesabı farklıysa
  `BANK_MATCH_STORE_ACCOUNT_MISMATCH` ile reddeder.

---

## 4. İç kullanıcı ekranlarında görünürlük (kiracıda asla)

`PaymentDetailDto`'ya yalnız `StoreAccountId`/`StoreName`/`StoreProviderCode`/`StoreCurrency`
eklendi (Merchant kimlik bilgisi/şifre **eklenmedi**). Bu DTO yalnız `PaymentController.Details`
(iç kullanıcı) tarafından tüketiliyor — `ChargeDetailDto`/`PaymentAllocationDto`/
`ChargeLineItemDto` (kiracı ile paylaşılan tipler) **değiştirilmedi**.

- `Views/Payment/Details.cshtml`: "Tahakkuk Kalemi" ve "Hedef Mağaza" hücreleri eklendi.
- `Views/Charge/Details.cshtml`: ödeme geçmişi tablosuna "Kalem" kolonu eklendi (mağaza
  eklenmedi — bu görünüm `PaymentAllocationDto` üzerinden çalışıyor).
- `Views/BankTransaction/Index.cshtml`, `SelectMatch.cshtml`, `SelectForPayment.cshtml`:
  mağaza kolonu/hücresi ve "yalnız aynı mağaza hesabına ait kayıtlar listelenir" açıklaması
  eklendi.

---

## 5. Testler

Yeni dosyalar: `PaymentValidationTests.cs` (4 test, DB'siz), `BankTransactionMatchingTests.cs`
(11 test, mağaza filtresi + eşleştirme + içe aktarma), `BankTransactionSchemaTests.cs` (4 test,
NOT NULL/index/FK Restrict), `PaymentUiArchitectureTests.cs` (6 test, view/DTO metin denetimi,
DB'siz). `TenantChargeValidationTests.cs`'e 2 yeni test eklendi.
`PaymentArchitectureTests.cs`'teki 12 `CreatePaymentInput`/`ReportTenantPaymentInput` çağrısına
(varsayılan kaldırıldığı için) `ChargeLineItemId` argümanı eklendi — senaryo/assert değişmedi.

Sonuç: `dotnet test` → 333/333 geçti, 0 skip.

---

## 6. Durma kapıları

- **DURMA KAPISI A** (migration öncesi dummy veri temizliği): tamamlandı.
- **DURMA KAPISI B** (`dotnet build` + `dotnet test` tam yeşil): tamamlandı.
- **DURMA KAPISI C** (manuel duman testi): tamamlandı — kullanıcı gerçek ortamda hem iç kullanıcı
  hem kiracı akışlarını denedi; süreçte bulunan 3 hata (bkz. §7) düzeltildi.
- **DURMA KAPISI D** (kullanıcı onayı): İç Faz 4 kullanıcı tarafından kabul edildi (2026-08-31).

**Bilinen, kalıcı kısıt (üst plandan taşındı):** Mağaza hesabı versiyonlandığında eski hesapla
snapshot'lanmış ödemeler, yeni hesaba import edilen hareketlerle otomatik eşleşmez; operatör
`Unmatch` + elle çözer.

---

## 7. Manuel test sürecinde bulunan ek düzeltmeler

Kullanıcının Durma Kapısı C sırasında yaptığı gerçek ortam testinde, orijinal planda yer almayan
üç ek sorun bulunup düzeltildi:

1. **Gerçek regresyon — `Views/TenantCharge/Index.cshtml`:** Bu liste sayfasının kendi, satırlar
   arasında paylaşılan ayrı bir "Ödeme Yap" modali vardı (İç Faz 4 planında yalnız
   `TenantCharge/Details.cshtml`'in modalı güncellenmişti). Bu modal hiç `ChargeLineItemId`
   göndermiyordu; yeni zorunluluk kuralı yüzünden bu ekrandan **hiçbir ödeme bildirilemiyordu**.
   Aynı kalem seçim deseni (JSON ile taşınan `modalCtx.lineItems` + `<template x-for>`) bu
   paylaşılan modale de eklendi.
2. **Görsel hizalama — `Charge/Details.cshtml` ve `TenantCharge/Details.cshtml`:** Özet karttaki
   "Kalem Bazında" accordion'u (bkz. sonraki not) başlangıçta tüm kartın genişliğine yayılıyordu.
   Sağ taraftaki üç istatistik kartını (Beklenen/Ödenen/Kalan) saran blok `flex flex-col
   items-end` yapılıp accordion bu blok içine, kartların hemen altına taşındı.
3. **UX — kalem seçiminin sekme üstüne taşınması:** Kiracı ödeme modalinde (hem Details hem
   Index) "Ödeme Yapılacak Kalem" radio grubu yalnızca "Havale/EFT Bildir" sekmesinin içindeydi;
   "Kart ile Öde" sekmesine geçildiğinde görünmüyordu ama tutar hâlâ ona bağlıydı. Radio grubu
   Başlık ile Sekmeler arasına taşınarak her iki sekmeden de bağımsız/ortak hale getirildi;
   Havale formunun içinde yalnızca gizli bir `ChargeLineItemId` input'u kaldı.

Ayrıca kullanıcı isteği üzerine, özet kartlara (`Charge/Details.cshtml`, `TenantCharge/Details.cshtml`,
`Payment/Create.cshtml`) kalem bazlı Toplam/Ödenen/Kalan dökümünü gösteren, projedeki
`_ParentRateCard.cshtml` (Tarife kartı) ile aynı native `<details>/<summary>` + `group-open:`
deseniyle bir accordion eklendi — İç Faz 4'ün orijinal planında yoktu, kullanıcının "Kullanılabilir"
teriminin kafa karıştırıcı bulunması üzerine talep edildi. Aynı görüşmede, ödeme radio kartlarındaki
"Kullanılabilir" etiketi "Kalan" olarak değiştirildi; yalnızca onay bekleyen ödeme varsa altına
açıklayıcı bir not (⏳) eklendi.

Bu ek düzeltmelerin tümü `dotnet build` + `dotnet test` (333/333) ile doğrulandı.
