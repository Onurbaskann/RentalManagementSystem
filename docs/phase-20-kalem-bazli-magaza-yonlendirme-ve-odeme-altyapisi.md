# Faz 20 — Kalem Bazlı Mağaza Yönlendirme ve Ödeme Altyapısı

**Durum:** Uygulamada; İç Faz 1-5 kullanıcı tarafından kabul edildi (İç Faz 5: 2026-09-01).
İç Faz 6 implementasyonu tamamlandı, `dotnet build`/`dotnet test` tam yeşil (352/352, 0 skip),
kullanıcı onayı bekleniyor.
**Aktif iç faz:** İç Faz 6 — Provider soyutu ve sanal POS işlem omurgası (kullanıcı onayı bekleniyor)  
**Ön koşullar:** Faz 18 ve Faz 19 tamamlandı.  
**Ana hedef:** Her tahakkuk kaleminin güncel kurallarla doğru mağaza hesabına yönlendirilmesi,
mevcut ödeme kanallarının kalem bazında izlenmesi ve bu çekirdeğin üzerine en son aşamalarda
soyutlanmış sanal POS/Paratika entegrasyonunun kurulması.

---

## 1. Kapsam özeti

Bu faz iki ana dalgadan oluşur:

1. **Önce ödeme yönlendirme çekirdeği:** Mağaza, mağaza hesabı, borç tipi + kapsam
   yönlendirmesi, deterministik hesap çözümleme, tahakkuk kalemi bazlı kısmi ödeme ve
   mevcut manuel/dekont/banka eşleştirme akışları.
2. **Sonra sanal POS:** Authenticated kiracı deneyimi, tek sağlayıcı soyutu,
   Paratika PayByLink, callback doğrulaması ve mutabakat görevi.

Sanal POS, mağaza çözümleme ve kalem bazlı ödeme kayıt modeli doğrulanmadan sisteme
bağlanmayacaktır.

---

## 2. Kesinleşen iş kararları

- “Mağaza”, ödemenin yönlendirileceği alıcı/merchant hesabının işletmesel kimliğidir.
- Mağaza adı ve mağaza hesap bilgileri kiracı kullanıcıya gösterilmez.
- Mağazanın ödeme sağlayıcısı hesap bilgileri ayrı tabloda tutulur.
- Bir mağazanın aynı anda yalnız bir aktif hesabı bulunur; eski hesaplar pasif tarihçe
  olarak korunur ve fiziksel silinmez.
- Yönlendirme `BorcTipi + kapsam` üzerinden tanımlanır.
- Kapsam seviyeleri genel varsayılan, taşınmaz ve birimdir.
- Çözümleme önceliği `birim → taşınmaz → genel varsayılan` şeklindedir.
- Her aktif borç tipi için genel varsayılan mağaza tanımı zorunludur.
- Uygun yönlendirme veya mağazaya ait aktif hesap bulunamazsa ödeme başlatılmaz/kaydedilmez.
- Yönlendirme değişikliği mevcut tahakkuk kayıtlarını güncellemez.
- Henüz başlatılmamış tüm ödemeler güncel yönlendirmeyi kullanır.
- Başlatılmış sanal POS denemesi veya yapılmış manuel ödeme, başladığı anda çözülen mağaza
  hesabıyla tarihsel olarak korunur.
- Kiracı tahakkukun kalemlerini ayrı görür ve tek seferde yalnız bir tahakkuk kalemi için
  ödeme başlatır.
- Birden fazla tahakkuk kaleminin tek ödeme denemesinde seçilmesi ve mağazalar arası bölünmüş
  tek işlem desteklenmez.
- Kısmi ödeme kalem seviyesinde devam eder.
- Mağaza yönlendirmesi sanal POS, havale/EFT bildirimi, manuel ödeme, dekont ve banka
  hareketi eşleştirme dahil bütün ödeme kanallarında geçerlidir.
- Ödeme portalı anonim token yerine kiracı portalı authentication'ı ile çalışır.
- Login olmadan ödeme adresine gelen kullanıcı güvenli `returnUrl` ile login sonrasında aynı
  tahakkuk/kalem ekranına döner.
- Ödeme sağlayıcısındaki gerçek işlem, bağlantı/e-posta üretilirken değil authenticated
  kullanıcının “Öde” eyleminde oluşturulur.
- KiraTakip kart numarası, CVV veya son kullanma tarihi saklamaz; sağlayıcının barındırdığı
  ödeme sayfasını kullanır.
- Sağlayıcıya iptal veya iade talebi gönderme Faz 20 kapsamında değildir.
- Sağlayıcıdan gözlenen başarısız/iptal durumları kayıt altına alınabilir.
- Ödeme sağlayıcısı tek `IOnlinePaymentProvider` soyutu arkasında tutulur.
- İlk somut sağlayıcı `ParatikaOnlinePaymentProvider` olur; yeni sağlayıcı eklenirken ortak
  ödeme orkestrasyonu değiştirilmez.
- KiraTakip ödeme başlatırken doğrudan Paratika'ya gider; ayrı Paratika servisi yalnız
  mutabakat/işlem sorgulama referansı olarak incelenmiştir.

---

## 3. Mevcut durum ve kapatılacak boşluklar

- `Tahakkuk` birden fazla `TahakkukKalemi` içerir; borç tipi kalem üzerindedir.
- `TahakkukOdemeleri` şu an yalnız `TahakkukId` taşır; ödemenin hangi kaleme ve mağaza
  hesabına ait olduğu bilinmez.
- Kısmi ödeme ve kalan tutar hesabı tahakkuk seviyesindedir.
- Kiracı ve iç kullanıcı manuel ödeme/dekont akışları tahakkuk seviyesinde çalışır.
- `BankaHareketleri` alıcı mağaza hesabını bilmez.
- Banka eşleştirmesi mağaza hesabı uyumunu kontrol edemez.
- Anonim `PaymentPortalController`, token, `PaymentLinkRecord`, token süresi ve secret tabanlı
  eski ödeme bağlantısı altyapısı mevcuttur.
- Mevcut sanal POS ekranı yalnız UI placeholder'dır; gerçek sağlayıcı entegrasyonu yoktur.
- Tahakkuk hatırlatma e-postaları tokenlı anonim ödeme portalına bağlantı üretir.

---

## 4. Hedef veri modeli

### 4.1 Yeni tablolar

#### `Magazalar` (`Store`)

- `Id`
- `Kod` — aktif/silinmemiş kayıtlarda benzersiz
- `Ad`
- `Aciklama?`
- Standart `Aktif`, soft-delete ve audit alanları

#### `MagazaHesapBilgileri` (`StoreAccount`)

- `Id`
- `MagazaId`
- `SaglayiciKodu` — ilk değer `Paratika`
- `ParaBirimi` — ilk kapsam `TRY`
- `MerchantId`
- `MerchantUser`
- Korunmuş merchant secret/credential alanı
- `GecerlilikBaslangici`
- `GecerlilikBitisi?`
- Standart `Aktif`, soft-delete ve audit alanları

Kurallar:

- Bir mağaza için yalnız bir aktif hesap bulunur.
- Merchant secret hiçbir liste/detail DTO'sunda geri döndürülmez.
- Secret değişikliği eski kaydı değiştirmek yerine eski hesabı pasife alıp yeni hesap
  sürümü oluşturur.
- Ödeme kayıtlarının referans verdiği hesap fiziksel olarak silinmez.
- Paratika servis URL'leri mağaza hesabında tekrarlanmaz; global güçlü tipli teknik ayarda
  tutulur.

#### `OdemeMagazaYonlendirmeleri` (`PaymentStoreRouting`)

- `Id`
- `ChargeTypeId`
- `PropertyId?`
- `UnitId?`
- `MagazaId`
- Standart `Aktif`, soft-delete ve audit alanları

Kapsam invariantları:

```text
PropertyId = null, UnitId = null  → genel varsayılan
PropertyId dolu, UnitId = null    → taşınmaz tanımı
PropertyId = null, UnitId dolu    → birim tanımı
PropertyId dolu, UnitId dolu      → geçersiz
```

Filtreli unique indexler:

- Aktif genel kayıtta `ChargeTypeId`
- Aktif taşınmaz kaydında `ChargeTypeId + PropertyId`
- Aktif birim kaydında `ChargeTypeId + UnitId`

#### `SanalPosIslemleri` (`OnlinePaymentTransaction`)

- `Id`
- `ChargeLineItemId`
- `StoreAccountId`
- `InitiatedByUserId`
- `PaymentAllocationId?`
- `ProviderCode`
- `MerchantPaymentId` — global benzersiz/idempotency anahtarı
- `ProviderTransactionId?` — Paratika `pgTranId`
- `Amount`
- `Currency`
- Normalize `Status`
- Sağlayıcının ham `ResponseCode`, `TransactionStatus`, `ErrorCode` ve güvenli mesaj alanları
- `SessionExpiresAt?`
- `CallbackReceivedAt?`
- `LastInquiryAt?`
- `InquiryCount`
- `CompletedAt?`
- Standart audit alanları ve optimistic concurrency alanı

#### `SanalPosIslemOlaylari` (`OnlinePaymentEvent`)

- `Id`
- `OnlinePaymentTransactionId`
- Olay tipi: session isteği/sonucu, callback, sorgulama, başarı, hata
- Sağlayıcı kodları ve maskelenmiş/güvenli özet
- Sağlayıcı zamanı ve yerel kayıt zamanı

Bu tablo append-only teknik/audit geçmişidir. Kart verisi, merchant secret veya tekrar
kullanılabilir session credential saklanmaz.

### 4.2 Mevcut tablo değişiklikleri

#### `TahakkukKalemleri`

- `OdenenTutar` eklenir.
- `0 <= OdenenTutar <= ToplamTutar` DB constraint'i eklenir.
- Kalan tutar `ToplamTutar - OdenenTutar` olarak hesaplanır.

#### `TahakkukOdemeleri`

- `TahakkukKalemiId` zorunlu FK olur.
- `MagazaHesapBilgisiId` zorunlu FK olur.
- `TahakkukId` mevcut sorgu/aggregate sınırı için korunur.
- Servis, kalemin verilen tahakkuka ait olduğunu doğrular.
- Doğrulanmış sanal POS ödemesi `VirtualPos + Approved` olarak ortak ödeme kaydına dönüşür.

#### `BankaHareketleri`

- `MagazaHesapBilgisiId` eklenir.
- İçe aktarma işlemi dosyanın hangi alıcı hesabına ait olduğunu zorunlu olarak bilir.
- Banka referans benzersizliği gerekirse `MagazaHesapBilgisiId + BankaReferansNo`
  kapsamında uygulanır.

#### Kaldırılacak eski token altyapısı

- `OdemeLinkKayitlari` / `PaymentLinkRecord`
- `IPaymentLinkService` ve implementasyonu
- Token DTO/ViewModel/validator/repository bileşenleri
- Anonim `PaymentPortalController` ve ona özel view'lar
- `PaymentLink` secret/base URL konfigürasyonu
- Ödeme bağlantısı geçerlilik süresi sistem ayarı

Silme, authenticated kiracı ödeme akışı doğrulandıktan sonra aynı migration/release dalgasında
yapılır.

---

## 5. Temel iş kuralları

### 5.1 Mağaza çözümleme

```text
Girdi: ChargeTypeId + UnitId
1. ChargeTypeId + UnitId aktif tanımını ara
2. Yoksa Unit.PropertyId ile taşınmaz tanımını ara
3. Yoksa ChargeTypeId genel varsayılanını ara
4. Bulunan mağazanın tek aktif hesabını getir
5. Herhangi bir adım eksik/çakışmalıysa ödemeyi blokla
```

- Resolver tek repository sorgu/use-case sınırında deterministik çalışır.
- Controller ve view mağaza seçimi yapmaz.
- Tenant DTO'ları mağaza, merchant veya hesap bilgisi taşımaz.
- Bir yönlendirme değişikliği tahakkukları veya henüz başlamamış işlemleri topluca güncellemez.

### 5.2 Kalem bazlı kısmi ödeme

- Ödeme tutarı sıfırdan büyük ve kalemin kullanılabilir kalan tutarından küçük/eşit olur.
- Onaylı ödemeler kalemin `OdenenTutar` değerini artırır.
- Reddedilen/başarısız ödemeler ödenen tutara dahil edilmez.
- Onay bekleyen manuel bildirimler ve aktif sanal POS denemeleri kullanılabilir tutar
  hesabında dikkate alınır.
- Aynı kalem için eşzamanlı ödeme/approval işlemleri transaction ve concurrency kontrolüyle
  fazla tahsilatı engeller.
- Tahakkuk `OdenenTutar`, kalem ödemelerinin toplamıdır; tahakkuk durumu bu aggregate üzerinden
  güncellenir.

### 5.3 Başlatılmış işlem ve sonradan değişen yönlendirme

- Henüz ödeme denemesi yoksa güncel mağaza çözülür.
- Sanal POS işlemi oluşturulduğunda kullanılan `StoreAccountId` sabitlenir.
- Yönlendirme sonradan değişse de açık işlemin sağlayıcı hesabı değişmez.
- Eski işlem başarısız/süresi dolmuşsa sonraki yeni deneme güncel mağaza hesabını kullanır.
- Aynı kalemde çözülmemiş aktif POS denemesi varken ikinci işlem oluşturulmaz; önce durum
  sorgulanır veya oturumun terminal/expired olması beklenir.

### 5.4 Provider başarı normalizasyonu

Paratika resmi API dokümanından (`https://vpos.paratika.com.tr/paratika/api/v2/doc`,
2026-08-31'de incelendi) doğrulanan kural:

- `responseCode 00` (`Onaylı`) → istek seviyesinde başarılı; `QUERYTRANSACTION` ile
  transaction status'u de kontrol edilir.
- `QUERYTRANSACTION` transaction status değerleri: `AP` (Approved) → başarılı, `FA`
  (Failed/Declined) → başarısız, `VD` (Voided) → iptal/void gözlemi, `CA` (kart sahibi
  onay sırasında iptal etti) → başarısız/iptal, `IP` (In Progress/Created) → henüz
  sonuçlanmadı, `MR` (Needs manual review) → `Unknown/PendingReconciliation`.
- `responseCode 98` (genel hata) veya `99` (reddedildi) → başarısız; `errorCode`/`errorMsg`
  teknik günlüğe yazılır, kullanıcıya güvenli/genel mesaj gösterilir.
- Diğer/tanınmayan durumlar → `Unknown/PendingReconciliation`; başarılı ödeme oluşturulmaz.

Üst seviye `QUERYTRANSACTION responseCode=00`, tek başına ödeme başarısı sayılmaz — asıl
belirleyici transaction status'tur (yukarıdaki eşleme).

**Doğrulanmamış, kodlama öncesi tekrar teyit edilmeli:** `sdSha512`/`SD_SHA512` imza alan
sırası, ayraç karakteri ve hangisinin (güncel/legacy) kullanılacağı — bkz. §11.

---

## 6. Sağlayıcı soyutlaması

Tek provider soyutu:

```csharp
public interface IOnlinePaymentProvider
{
    string ProviderCode { get; }

    Task<CreatePaymentSessionResult> CreateSessionAsync(
        CreatePaymentSessionRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);

    Task<PaymentInquiryResult> QueryAsync(
        PaymentInquiryRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);

    Task<PaymentCallbackResult> ValidateCallbackAsync(
        PaymentCallbackRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);
}
```

- `ParatikaOnlinePaymentProvider` Paratika alan adları, form-urlencoded istek, kod eşleme,
  PayByLink URL'si ve callback imza doğrulamasını kapsar.
- Ortak ödeme servisi authentication, scope, kalem/tutar, mağaza çözümleme, transaction
  kaydı, idempotency ve tahakkuk güncellemesini yönetir.
- Provider sınıfı EF entity/repository veya tenant ViewModel bilmez.
- Provider seçimi `StoreAccount.ProviderCode` ile resolver/factory üzerinden yapılır.
- Yeni provider yalnız yeni `IOnlinePaymentProvider` implementasyonu ve DI/provider kaydıyla
  eklenir; ortak domain akışı değiştirilmez.
- `CancelAsync` veya `RefundAsync` bu fazda soyuta eklenmez.

Paratika global teknik ayarları güçlü tipli `ParatikaOptions` altında tutulur (alan adları
Paratika dokümanındaki tabloyla uyumlu tutulacak şekilde İç Faz 6'da netleştirilir):

- `ApiBaseUrl` — ortama göre `test.paratika.com.tr` / `entegrasyon.paratika.com.tr` /
  `vpos.paratika.com.tr`; tek bir API uç noktası altında `SESSIONTOKEN`/`QUERYTRANSACTION`
  action'ları form alanıyla ayrışıyor (ayrı URL değil).
- `HostedPaymentPageBaseUrl` — `/payment/[SESSION_TOKEN]` şeklinde birleştirilir.
- `ReturnUrl` — `RETURNURL` alanına gönderilir; ödeme sonrası kullanıcı tarayıcısına
  POST-redirect ile dönülecek adres.
- `NotificationUrl` — opsiyonel, sunucudan sunucuya webhook bildirimi için `NOTIFICATIONURL`
  alanına gönderilir (5 saniyelik yanıt zaman aşımı var — endpoint hızlı 2xx dönmeli, asıl
  işleme arka planda yapılmalı).
- `SessionExpiryMinutes` — `SESSIONEXPIRY` alanına gönderilir; Paratika varsayılanı 7 gün ama
  bizim ürün kararımız çok daha kısa bir süre olacak (İç Faz 7'de netleşir).
- `HttpTimeout`

Mağazaya özgü merchant kimlikleri (`MERCHANT`, `MERCHANTUSER`, `MERCHANTPASSWORD` — Paratika
dokümanındaki 3 ayrı alan) bu options sınıfında tutulmaz, `StoreAccount`'tan okunur;
İç Faz 6/7 implementasyon planında `StoreAccount`'un mevcut `MerchantId`/`MerchantUser`/
korunmuş secret alanlarının bu 3 Paratika alanına birebir mi eşleneceği, yoksa `MerchantId`
alanının `MERCHANT`+`CUSTOMER` gibi iki ayrı Paratika alanını mı karşılayacağı netleştirilir.

---

## 7. İç faz sırası

```text
İç Faz 0  Kapsam ve mevcut yapı doğrulaması
    ↓
İç Faz 1  Mağaza ve mağaza hesabı yönetimi
    ↓
İç Faz 2  Borç tipi/kapsam yönlendirmesi ve resolver
    ↓
İç Faz 3  Tahakkuk kalemi bazlı ödeme çekirdeği
    ↓
İç Faz 4  Manuel ödeme, dekont ve banka eşleştirme uyarlaması
    ↓
İç Faz 5  Authenticated kiracı ödeme deneyimi ve token kaldırma
    ↓
İç Faz 6  Provider soyutu ve sanal POS işlem omurgası
    ↓
İç Faz 7  Paratika PayByLink entegrasyonu
    ↓
İç Faz 8  Callback, mutabakat ve operasyonel dayanıklılık
    ↓
İç Faz 9  Regresyon, migration, UAT ve kontrollü yayın
```

Her iç faz için kod değişikliğinden önce ilgili dosya/simge araştırması yapılır, kısa
Implementation Plan kullanıcıya sunulur ve onay alınır.

---

## 8. İç faz checklist'leri

### İç Faz 0 — Kapsam ve mevcut yapı doğrulaması

- [x] Tahakkuk ve tahakkuk kalemi ilişkisi incelendi.
- [x] Mevcut ödeme, kısmi ödeme, dekont ve banka eşleştirme yapısı incelendi.
- [x] Anonim tokenlı ödeme portalı ve hatırlatma e-postası bağımlılıkları incelendi.
- [x] Referans Paratika PayByLink isteği, callback ve mutabakat akışı incelendi.
- [x] Ara servisin ödeme başlatmadığı, yalnız mutabakat yaptığı netleştirildi.
- [x] Mağaza kapsam seviyeleri ve precedence kararlaştırıldı.
- [x] Tek kalem/tek ödeme denemesi ve kısmi ödeme kararı kesinleştirildi.
- [x] İptal/iade başlatmanın kapsam dışı olduğu kesinleştirildi.
- [x] Tek provider soyutu kararlaştırıldı.
- [x] Faz 20 planı ve doküman bağlantıları oluşturuldu.

Tamamlanma kapısı:

- [x] Sanal POS öncesi tamamlanması gereken çekirdek finansal işler ayrıştırıldı.
- [x] Sağlayıcı dokümanı bekleyen ayrıntılar çekirdek tablolardan izole edildi.

### İç Faz 1 — Mağaza ve mağaza hesabı yönetimi

- [x] `Store` ve `StoreAccount` entity/EF konfigürasyonları oluşturulur.
- [x] Tek aktif hesap invariantı filtreli unique index ve servis kuralıyla korunur.
- [x] Merchant secret koruma/anahtar yönetimi belirlenir ve plaintext log/DTO engellenir.
- [x] Repository, command DTO, validator, business-rules ve servis katmanları eklenir.
- [x] Mağaza liste/ekle/düzenle/pasife alma yönetim ekranları eklenir.
- [x] Hesap sürümleme ve secret değiştirme akışı eklenir.
- [x] Store/account permission'ları merkezi kataloğa eklenir.
- [x] Aktif yönlendirme/ödeme tarafından kullanılan mağaza veya hesap fiziksel silinmez.
- [x] Migration ve SQL Server bütünlük testleri yazılır.

Tamamlanma kapısı:

- [x] İç kullanıcı güvenli biçimde mağaza ve tek aktif hesap yönetebilir.
- [x] Secret hiçbir response, log veya liste projeksiyonunda açığa çıkmaz.
- [x] Kullanıcı İç Faz 1 sonucunu onaylar.

### İç Faz 2 — Yönlendirme tanımları ve resolver

- [x] `PaymentStoreRouting` entity, constraint ve filtreli unique indexleri eklenir.
- [x] Genel, taşınmaz ve birim kapsamlı CRUD akışları geliştirilir.
- [x] Scope uyumu ve birimin taşınmaz ilişkisi servis seviyesinde doğrulanır.
- [x] Unit → property → default resolver repository sorgusu ve servisi eklenir.
- [x] İnaktif mağaza/hesap, eksik default ve çakışmalı tanım sabit hata kodlarıyla bloklanır.
- [x] Her aktif borç tipi için genel tanım zorunluluğu aktivasyon/ödeme kapısına bağlanır.
- [x] Genel tanımı eksik borç tipleri, aktif/pasif ayrımı yapılmadan yönetim ekranında görünür olur.
- [x] Yönlendirme ekranı tenant DTO/view akışlarından tamamen ayrılır.
- [x] Precedence, pasif kayıt, unique yarış ve kapsam testleri yazılır.

Tamamlanma kapısı:

- [x] Her kalem için tek ve deterministik aktif mağaza hesabı çözülebilir.
- [x] Eksik tanımlı kalemde resolver ödeme başlatılmasını sabit hata koduyla engeller.
- [x] Kullanıcı İç Faz 2 sonucunu onaylar (2026-08-27).

### İç Faz 3 — Tahakkuk kalemi bazlı ödeme çekirdeği

- [x] `ChargeLineItem.PaidAmount` ve tutar constraint'i eklenir.
- [x] `PaymentAllocation.ChargeLineItemId` ve `StoreAccountId` ilişkileri eklenir.
- [x] Kalem kalan/kullanılabilir tutar hesabı tek servis/repository kontratında toplanır.
- [x] Tahakkuk toplam ödeme ve durum güncellemesi kalem aggregate'ından üretilir.
- [x] Pending manuel ödeme kullanılabilir tutardan düşülür (aktif POS rezervasyonu İç Faz 6+
  kapsamındadır, bu iç fazda henüz yok).
- [x] Eşzamanlı onay/ödeme için transaction, lock/concurrency ve idempotency uygulanır
  (`sp_getapplock`, `ReservationRepository` deseninin birebir kopyası).
- [x] Liste/detail DTO'ları kalem açıklaması, toplamı, ödeneni ve kalanı taşıyacak şekilde güncellenir.
- [x] Mevcut ödeme verisi için preflight/backfill stratejisi uygulanır; çok kalemli eski
  ödemelerde tahmine dayalı dağıtım yapılmaz.
- [x] Kalem/tahakkuk toplam invariant testleri yazılır.

Tamamlanma kapısı:

- [x] Kısmi ödeme tek tahakkuk kalemine güvenilir biçimde bağlanır.
- [x] Kalem ve tahakkuk toplamları ayrışmaz; fazla ödeme yarışı engellenir.
- [x] Kullanıcı İç Faz 3 sonucunu onaylar (2026-08-28) — çok kalemli tahakkukta
  `PAYMENT_LINE_ITEM_SELECTION_REQUIRED` hatasının gerçek ortamda (RunSeed ile yeniden
  üretilen veriyle) beklendiği gibi göründüğü doğrulandı.

**Çözüldü (İç Faz 4):** Çok kalemli bir tahakkukta hangi kalemin ödeneceği artık hem iç kullanıcı
manuel ödeme ekranında hem kiracı dekont modalında radio grubuyla seçilebiliyor; ayrıntı için
[`phase-20-inner-phase-4-implementation-plan.md`](phase-20-inner-phase-4-implementation-plan.md).

### İç Faz 4 — Mevcut ödeme kanallarının uyarlanması

- [x] İç kullanıcı manuel ödeme oluştururken tahakkuk kalemi seçer.
- [x] Kiracı havale/EFT/dekont bildirirken tahakkuk kalemi seçer.
- [x] Her ödeme oluşturma anında güncel mağaza hesabı resolver ile belirlenir ve snapshot FK
  ödeme üzerinde korunur (İç Faz 3'te kuruldu, İç Faz 4'te UI'dan gerçek kalem seçimiyle beslenir).
- [x] Pending approval, approve ve reject kuralları kalem kullanılabilir tutarına uyarlanır
  (İç Faz 3'te tamamlandı, bu iç fazda değişmedi).
- [x] Banka hareketi importunda hedef mağaza hesabı zorunlu hale getirilir.
- [x] Otomatik/manüel banka eşleştirme adayları aynı mağaza hesabıyla sınırlandırılır.
- [x] Ödeme/dekont/detail ekranlarında iç kullanıcıya mağaza bilgisi gerektiği ölçüde gösterilir;
  tenant ekranlarında gösterilmez.
- [x] Eski tahakkuk-seviyeli request/ViewModel/validator sözleşmeleri kaldırılır
  (`ChargeLineItemId` varsayılan değeri kaldırıldı, iki validator'a zorunluluk kuralı eklendi).
- [x] Manuel, dekont ve banka eşleştirme regresyon testleri yazılır.

Tamamlanma kapısı:

- [x] Sanal POS olmadan bütün mevcut ödeme kanalları kalem + mağaza hesabı ekseninde çalışır.
- [x] Tenant verisi ve mağaza gizliliği korunur (mimari testlerle doğrulandı).
- [x] Kullanıcı İç Faz 4 sonucunu onayladı (2026-08-31).

### İç Faz 5 — Authenticated kiracı ödeme deneyimi

- [x] Kiracı tahakkuk detayında her kalemin toplam/ödenen/kalan tutarı ayrı gösterilir
  (İç Faz 4'ün UX iyileştirme turunda tamamlandı — "Kalem Bazında" accordion).
- [x] Tek kalem ve kısmi tutar seçimi permission, tenant sahipliği ve birim scope ile korunur
  (mevcut `ChargeRepository.GetTenantDetailsAsync` filtresi; `TenantChargeAuthorizationTests.cs`
  ile regresyon testi eklendi).
- [x] Ödeme deep-link'i unauthenticated kullanıcıyı login'e, başarılı login sonrası aynı ekrana
  güvenli local `returnUrl` ile döndürür (ASP.NET Core cookie-auth + `AccountController.Login`'in
  mevcut `Url.IsLocalUrl` kontrolü — değiştirilmedi, artık gerçekten authenticated route'a
  yönlendirildiği için devreye giriyor).
- [x] Normal login davranışı portal ana sayfasında kalır (değişmedi).
- [x] Hatırlatma e-postaları token yerine authenticated tenant route'ları üretir
  (`/Tenant/Charges/Details/{hashid}`).
- [x] **Karar değişikliği (kullanıcı, 2026-08-31):** Hatırlatma alıcıları bireysel tenant
  kullanıcılarına değil, **hâlâ `Tenant.Email`'e** gönderiliyor — veri güvenliği gerekçesiyle
  kullanıcı bilinçli olarak bu şekilde kalmasını istedi. Ayrıntı için
  [`phase-20-inner-phase-5-implementation-plan.md`](phase-20-inner-phase-5-implementation-plan.md).
- [x] `PaymentLinkRecord`, token servisi bağımlılığı, anonim portal ve validity ayarı kaldırılır.
- [x] POS placeholder kart alanları kaldırılır; ham kart verisi KiraTakip view/modeline alınmaz.
- [x] IDOR ve tenant izolasyonu testleri yazıldı; safe-return-url için bilinçli olarak yeni bir
  `WebApplicationFactory` entegrasyon testi **yazılmadı** (projede hiç kullanılmayan bir test
  altyapısı gerektirirdi, değişmeyen framework davranışı için orantısız görüldü).

Tamamlanma kapısı:

- [x] Ödeme sayfası yalnız authenticated ve yetkili tenant kullanıcısına açıktır.
- [x] Eski anonim ödeme tokenı kullanılmaz.
- [x] Kullanıcı İç Faz 5 sonucunu onayladı (2026-09-01) — kontrolde sorun bulunmadı.

### İç Faz 6 — Provider soyutu ve sanal POS işlem omurgası

- [x] `IOnlinePaymentProvider` ve provider-neutral request/result DTO'ları eklenir.
- [x] Provider resolver/factory `ProviderCode` üzerinden somut sınıf seçer (provider-by-code
      DI deseni — `IBankaHareketiParser`/`AkbankCsvParser` ile birebir aynı).
- [x] `OnlinePaymentTransaction` ve append-only olay geçmişi entity/repository/servisleri eklenir.
- [x] Normalize durum modeli ve izin verilen durum geçişleri tanımlanır
      (`OnlinePaymentBusinessRules.EnsureValidStatusTransition` — saf fonksiyon, İç Faz 7/8
      için hazır, henüz hiçbir yerden çağrılmıyor).
- [x] MerchantPaymentId benzersizliği (filtreli unique index, şema testiyle doğrulandı) ve
      `PaymentAllocationId` nullable FK iskeleti korunur — birebir başarı bağlantısı İç Faz 8'de
      doldurulacak.
- [x] Ortak orchestration servisi auth/scope, kalem, tutar, resolver ve transaction kaydını yönetir
      (`OnlinePaymentService.InitiateAsync` — yalnız "initiate" ucu, bkz. bu fazın kapsam kararı).
- [x] Fake provider ile provider-bağımsız akış test edilir (`OnlinePaymentServiceTests.cs`,
      test projesine özel `FakeOnlinePaymentProvider`).
- [x] `ParatikaOptions` güçlü tipli teknik config eklenir — **başlangıç doğrulaması bilinçli
      olarak eklenmedi**: kullanıcı incelemesinde, projede `DataAnnotations`+`ValidateOnStart()`
      deseninin hiç kullanılmadığı (grep ile doğrulandı) tespit edildi; mevcut ayar sınıfları
      (`SmtpSettings`, `SecureTokenSettings`) düz POCO ve doğrulama kullanım anında `Guard` ile
      yapılıyor. `ParatikaOptions` bu konvansiyona uydurulmuştur; doğrulama İç Faz 7'de gerçek
      HTTP çağrısı eklendiğinde `Guard.Against(...)` ile yapılacaktır.
- [ ] Secret redaction, güvenli log ve HTTP client timeout politikaları hazırlanır — **İç Faz
      7'ye ertelendi**: bu fazda henüz gerçek bir HTTP çağrısı/`IHttpClientFactory` kullanımı
      yok, dolayısıyla redaksiyon/timeout politikasının uygulanacağı bir çağrı noktası da yok.

Tamamlanma kapısı:

- [x] Sağlayıcı dış çağrısı olmadan ortak sanal POS yaşam döngüsü test edilebilir.
- [x] Yeni provider eklemek ortak ödeme servisini değiştirmeyi gerektirmez.
- [ ] Kullanıcı İç Faz 6 sonucunu onaylar.

### İç Faz 7 — Paratika PayByLink entegrasyonu

**Ön not (2026-08-31):** Resmi API dokümanı elde edildi (bkz. §11), ana akış ve uç noktalar
netleşti. Ancak imza doğrulama algoritmasının (`sdSha512`/`SD_SHA512`) tam alan sırası bir
web-fetch özetinden geliyor — implementasyona başlamadan önce doküman sayfasının ilgili
bölümü harfi harfine tekrar okunup teyit edilmeli.

- [ ] `ParatikaOnlinePaymentProvider` session, query ve callback metotlarını uygular.
- [ ] `SESSIONTOKEN` isteği doğru account ve tek kalem tutarıyla oluşturulur; hosted payment
  page (`/payment/[SESSION_TOKEN]`) adresine yönlendirilir.
- [ ] MerchantPaymentId işlem oluşturulmadan önce kalıcı ve unique üretilir.
- [ ] Session token başarılıysa hosted PayByLink sayfasına güvenli yönlendirme yapılır.
- [ ] Kısmi tutar Paratika isteğinden hemen önce transaction içinde yeniden doğrulanır.
- [ ] Sağlayıcı callback alanları (`RETURNURL` POST-redirect ve/veya `NOTIFICATIONURL`
  webhook) provider sınıfında parse ve normalize edilir.
- [ ] İmza doğrulama algoritması, dokümandan harfi harfine teyit edildikten sonra uygulanır;
  doğrulanmayan callback ödeme oluşturamaz.
- [ ] Referans başarı kodları adapter testleriyle doğrulanır.
- [ ] Başarılı sonuç ortak completion servisiyle tek Approved ödeme oluşturur.
- [ ] Taksit, komisyon, maskeli kart ve provider işlem bilgileri yalnız sağlayıcının döndürdüğü
  ölçüde güvenli biçimde kaydedilir/gösterilir.
- [ ] İptal ve iade başlatma endpoint/metotları eklenmez.

Tamamlanma kapısı:

- [ ] Authenticated kiracı tek kaleme tam/kısmi Paratika ödemesi başlatabilir.
- [ ] Mağaza bilgisi tenant'a sızmadan doğru merchant hesabı kullanılır.
- [ ] Kullanıcı İç Faz 7 sonucunu onaylar.

### İç Faz 8 — Callback, mutabakat ve operasyonel dayanıklılık

- [ ] Callback ve QueryTransaction aynı idempotent completion servisini kullanır.
- [ ] Yalnız `SALE` ve doğrulanmış başarı kombinasyonları ödeme oluşturur.
- [ ] Unknown/belirsiz işlemler başarılı sayılmaz ve mutabakat kuyruğuna girer.
- [ ] BackgroundService/PeriodicTimer ile batch, cancellation ve kayıt bazlı hata izolasyonu uygulanır.
- [ ] İlk sorgu, tekrar aralığı, maksimum deneme ve session expiry politikaları güçlü tipli ayarlara bağlanır.
- [ ] Transaction bazlı SQL Server application lock veya eşdeğer koruma ile çoklu instance
  idempotency sağlanır.
- [ ] Callback kaybı, tekrarlı callback, callback-query yarışı ve geç başarılı işlem test edilir.
- [ ] Operasyonel log, sağlık göstergesi ve manuel yeniden sorgulama yolu eklenir.
- [ ] Sağlayıcı hatası tenant'a güvenli Türkçe mesaj; iç kullanıcıya takip koduyla gösterilir.

Tamamlanma kapısı:

- [ ] Callback gelmese bile başarılı ödeme mutabakatla güvenli biçimde kaydedilir.
- [ ] Aynı provider işlemi ikinci tahsilat oluşturmaz.
- [ ] Kullanıcı İç Faz 8 sonucunu onaylar.

### İç Faz 9 — Regresyon, migration, UAT ve kontrollü yayın

- [ ] Temiz SQL Server test DB üzerinde migration uygulanır.
- [ ] Migration preflight, backfill ve rollback/roll-forward adımları doğrulanır.
- [ ] Mağaza/account/routing unique ve constraint testleri geçer.
- [ ] Kalem bazlı manuel, tenant dekont ve banka eşleştirme regresyonları geçer.
- [ ] Tenant mağaza gizliliği ve authenticated deep-link UAT senaryoları geçer.
- [ ] Paratika test hesabıyla başarılı, başarısız, terk edilmiş ve mutabakat senaryoları doğrulanır.
- [ ] Secret/config production checklist'i tamamlanır.
- [ ] Önce routing ve mevcut kanallar, sonra sanal POS feature flag ile kontrollü açılır.
- [ ] Monitoring ve geri dönüş adımları ayrı release runbook'ta yazılır.
- [ ] Kullanıcı kabulü ve Faz 20 kapanış onayı alınır.

---

## 9. Migration ve veri geçişi ilkeleri

- Yeni tablolar önce eklenir; mağaza ve default yönlendirmeler yapılandırılmadan ödeme feature'ı açılmaz.
- Mevcut `TahakkukOdemeleri` için önce nullable FK'ler eklenir, preflight ve güvenilir backfill
  yapılır, sonra zorunlu hale getirilir.
- Tek kalemli eski tahakkuk ödemeleri deterministik eşlenebilir.
- Çok kalemli eski tahakkuk ödemeleri iş bilgisi olmadan kalemlere dağıtılmaz.
- Projede gerçek production verisi bulunmadığı mevcut proje kuralıdır; yine de migration
  tahmin yürütmez ve uyuşmayan kayıtları raporlar.
- Test/dummy ödeme verisi gerekiyorsa açık seed temizleme/yeniden üretme adımı kullanılır.
- Ödeme tarafından referanslanan mağaza, hesap, yönlendirme ve işlem kayıtları hard-delete edilmez.

---

## 10. Test matrisi

- Resolver precedence: birim, taşınmaz, default ve eksik tanım.
- İnaktif mağaza/hesap ve aynı mağazada iki aktif hesap yarışı.
- Aynı kapsam için paralel duplicate yönlendirme.
- Kalem tam ödeme, ardışık kısmi ödeme ve kalan sınırı.
- Pending manuel tutar + yeni ödeme denemesi.
- Aynı kalemde paralel approve/POS başarı yarışı.
- Tahakkuk kalemi toplamı ile tahakkuk aggregate tutarlılığı.
- Tenant sahipliği, birim scope, permission ve URL ID değiştirme.
- Tenant DTO/view içinde mağaza/merchant/secret bulunmaması.
- Banka hareketi ile ödeme mağaza hesabı uyuşmazlığı.
- Provider session hatası, invalid callback signature ve bilinmeyen durum.
- Tekrarlı callback, callback-query yarışı ve QueryTransaction ile geç başarı.
- Worker iki instance ve tekrar çalıştırma idempotency'si.
- Tokenlı anonim portal route'larının kapalı olması.

---

## 11. Provider dokümanı — durum

Resmi Paratika API v2 dokümanı ulaştı (`https://vpos.paratika.com.tr/paratika/api/v2/doc`,
2026-08-31). Mağazaya özel bilgi içermiyor, genel API referansı. Aşağıdaki maddeler bu
dokümanla kapandı/kapanmadı:

- ~~Production/test endpoint~~ **Kapandı.** Üç ortam: `test.paratika.com.tr`,
  `entegrasyon.paratika.com.tr`, `vpos.paratika.com.tr` (aynı `/paratika/api/v2` yolu, tek
  taban URL farklı). Merchant credential değerleri (mağazaya özel) hâlâ ayrı sağlanacak —
  doküman bunları içermiyor, zaten beklenmiyordu.
- ~~Resmi session expiry sınırı~~ **Kapandı.** `SESSIONEXPIRY` parametresiyle ayarlanır,
  Paratika varsayılanı 7 gün. Bizim ürün kararımız (muhtemelen çok daha kısa) ayrı bir
  İç Faz 7 kararı olacak — Paratika kaynaklı bir üst sınır dokümanda görünmüyor.
- ~~Callback'in browser POST dışında server-to-server bildirimi olup olmadığı~~ **Kapandı.**
  İkisi de var: Hosted Payment Page tamamlandığında `RETURNURL`'e kullanıcı tarayıcısı
  üzerinden POST-redirect, ayrıca ayrı yapılandırılabilen `NOTIFICATIONURL` ile sunucudan
  sunucuya webhook (5 saniyelik yanıt zaman aşımı notu var). İkisi de güvenilmez/at-least-once
  kabul edilip `QUERYTRANSACTION` ile doğrulanacak — mevcut mutabakat tasarımı zaten bunu
  öngörüyordu, değişmedi.
- ~~Taksit/komisyon seçeneklerinin response davranışı~~ **Kapandı (kapsam dışı olarak).**
  `INSTALLMENTS`/`COMMISSIONAMOUNT`/`TOTALSELLERCOMMISSIONAMOUNT` alanları var ama Faz 20
  bunları kullanmıyor (§12 kararı korunuyor) — bu alanlar isteklerimizde gönderilmeyecek.
- **`SD_SHA512` alan sırası, secret ve doğrulama algoritması — hâlâ açık.** Dokümanı çeken
  araç iki farklı imza şeması özetledi (güncel `sdSha512` ve legacy `SD_SHA512`, farklı alan
  birleştirme sırasıyla) ama bu bir web-fetch özeti — byte-hassas bir güvenlik kontrolü için
  yeterince güvenilir değil. **İç Faz 7 kodlamasına başlamadan önce doküman sayfasının ilgili
  bölümü harfi harfine (tablo/örnek istek-yanıt dahil) tekrar okunup alan sırası, ayraç
  karakteri ve kullanılacak şema (güncel/legacy) doğrulanacak.**
- **QueryTransaction rate-limit — hâlâ açık.** Dokümanda belirtilmemiş. Worker'ın sorgulama
  aralığı Paratika kaynaklı bir kısıttan değil, bizim seçeceğimiz güvenli bir varsayılandan
  (İç Faz 8'de belirlenir) gelecek.

**Yeni netleşmesi gereken nokta (dokümandan çıktı, önceden bilinmiyordu):** `StoreAccount`
şu an `MerchantId`/`MerchantUser`/korunmuş secret olarak 3 alan tutuyor; Paratika'nın istek
şeması `MERCHANT` (mağaza/merchant kodu), `MERCHANTUSER`, `MERCHANTPASSWORD` **ve ayrıca**
her istekte zorunlu bağımsız bir `CUSTOMER` alanı bekliyor. Bu 4. alanın `StoreAccount`'a mı
ekleneceği yoksa başka bir kaynaktan mı (örn. sabit/tenant bazlı) türetileceği İç Faz 6/7
implementasyon planında netleştirilir — mağaza resolver/veri modeli kararları burada yeniden
açılmıyor, yalnız `StoreAccount` şemasına olası tek-alan eklentisi olarak not düşülüyor.

Referans koddan bilinen başlangıç kontratı `Merchant`, `MerchantUser`, `MerchantPassword`,
`PAYBYLINKPAYMENT`, `QUERYTRANSACTION`, `SALE`, `00/0000`, `AP`, `FA`, `VO`, `pgTranId`,
`merchantPaymentId`, `installmentCount`, komisyon ve maskeli kart alanlarını kapsar.

---

## 12. Faz 20 kapsamında yapılmayacaklar

- Tek işlemde birden fazla tahakkuk kalemi ödeme.
- Mağazalar arasında tek provider işlemini bölme/split payment.
- Anonim veya paylaşılabilir tokenla ödeme.
- Kart numarası/CVV saklama veya KiraTakip içinde kart formu işleme.
- Provider'a iptal, void veya iade isteği gönderme.
- Chargeback yönetim modülü.
- Otomatik/tekrarlayan ödeme ve kart saklama.
- Paratika dışındaki ikinci sağlayıcının somut entegrasyonu.
- Banka API entegrasyonu; mevcut dosya import yöntemi korunur.
- Mağaza veya merchant bilgisini tenant kullanıcıya gösterme.

---

## 13. Güncelleme kuralları

- Atomik checklist ve teknik kararlar bu dosyada tutulur; `PROGRESS.md` yalnız aktif iç fazı
  ve sıradaki adımı özetler.
- İç faz Implementation Plan onayı olmadan ilgili kod değişikliğine başlanmaz.
- Her iç faz sonunda test sonucu, migration etkisi, kalan risk ve sonraki faz açıkça yazılır.
- Provider dokümanı geldiğinde yalnız Bölüm 4.1, 5.4, 6, İç Faz 7/8 ve açık teknik ayrıntılar
  güncellenir; mağaza resolver ve kalem bazlı ödeme kararları yeniden açılmaz.
