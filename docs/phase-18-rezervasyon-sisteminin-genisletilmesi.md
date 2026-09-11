# KiraTakip — Faz 18 Rezervasyon Sisteminin Genişletilmesi

> **Doküman türü:** Faz bazlı uygulama planı ve teknik karar kaydı  
> **Durum:** Tamamlandı — kullanıcı kapanış onayı ve production migrationları  
> **Son güncelleme:** 2026-08-17  
> **Kapsam:** İç kullanıcı rezervasyon yönetimi, kiracı portalı talep akışı,
> yerel uygunluk/çakışma kontrolü, onay/ret, katılımcılar, tahakkuk ve otomatik tamamlama  
> **Aktif ilerleme:** [`PROGRESS.md`](PROGRESS.md)

---

## 1. Dokümanın amacı

Bu doküman, KiraTakip rezervasyon sistemini yalnız KiraTakip verileri ve kurallarıyla
genişletmek için uygulanacak teknik planın ana kaynağıdır.

Hedeflenen sonuçlar:

- Kiracı kullanıcısı yetkili olduğu rezervasyon birimleri için talep oluşturabilir.
- İç kullanıcı kendi kapsamındaki talepleri inceleyebilir, onaylayabilir veya reddedebilir.
- Yetkili iç kullanıcı isterse talebi oluşturup aynı işlemde onaylayabilir.
- Yerel rezervasyonlar için liste ve takvim uygunluğu gösterilir.
- Aynı birim ve zaman aralığında iki onaylı rezervasyon oluşması engellenir.
- Başlık, katılımcı listesi, paylaşılan not ve iç not ayrı tutulur.
- İptal, ret ve onay bilgileri açıklama alanını bozmadan audit edilebilir biçimde saklanır.
- Tahakkuk yalnız uygun durumdaki rezervasyondan ve bir kez oluşturulur.
- Süresi biten rezervasyonlar bitişten 15 dakika sonra kalıcı olarak tamamlanır.

---

## 2. Kullanım kuralları

Her iç fazda şu sıra zorunludur:

1. `PROGRESS.md` içindeki aktif iç faz okunur.
2. Bu dokümanda yalnız ilgili iç faz ve onun doğrudan referans verdiği bölümler okunur.
3. Mevcut hedef dosyalar baştan sona okunur ve bütün sembol referansları `rg` ile aranır.
4. Dosya, sınıf, DTO, servis ve repository imzalarını içeren Implementation Plan sunulur.
5. Kullanıcı onayı alınmadan kod, migration veya yapılandırma değiştirilmez.
6. Yalnız onaylanan iç faz uygulanır; sonraki fazdan kod eklenmez.
7. SQL davranışı testlerinde yalnız `KiraTakipDb_Test` SQL Server veritabanı kullanılır.
8. Hedefli testler ve gerekli build başarılı görülmeden görev tamamlandı işaretlenmez.
9. Her iç faz sonunda `PROGRESS.md` ile bu dokümanın checklist'i birlikte güncellenir.
10. Kullanıcıya yapılanlar, dosyalar, doğrulama, kalan sorunlar ve sonraki faz sade dille raporlanır.

Durum işaretleri:

- `[ ]` Başlanmadı
- `[-]` Aktif
- `[x]` Tamamlandı ve doğrulandı
- `[!]` Kullanıcı kararı veya dış koşul nedeniyle bloke

---

## 3. Kapsam değişikliği kararı

Microsoft Outlook/Microsoft 365 entegrasyonu 2026-08-12 tarihinde kullanıcı kararıyla
Faz 18 kapsamından tamamen çıkarıldı.

Gerekçe:

- Kullanılabilir ücretsiz Microsoft 365 test ortamı bulunmuyor.
- Entegrasyon için ek lisans ve operasyon maliyeti oluşuyor.
- Graph, sertifika, Room Mailbox ve Exchange yönetimi temel rezervasyon ihtiyacına göre
  orantısız teknik ve operasyonel yük getiriyor.
- Henüz Outlook'a özel uygulama kodu veya migration yazılmadığı için kapsamı şimdi
  sadeleştirmek veri kaybı veya geri alma maliyeti oluşturmuyor.

Bu kararın sonucu olarak aşağıdakiler uygulanmayacaktır:

- Microsoft Graph SDK ve kimlik doğrulama altyapısı
- Microsoft 365 tenant, organizatör mailbox ve Room Mailbox POC'u
- Outlook event oluşturma, güncelleme veya silme
- Outlook free/busy sorgusu
- `OutlookSyncStatus`, `ReservationOutlookSync` ve Outlook alanları
- Outlook için integration outbox, retry, dead-letter, webhook ve reconcile
- `Internal.Reservation.ManageOutlookSync` izni
- Outlook hata/bekleme durumları ve Outlook operasyon ekranları

İleride takvim entegrasyonu istenirse Faz 18'e geri eklenmez; ayrı bir ana faz ve ayrı
gereksinim olarak planlanır. Lisans gerektirmeyen `.ics` dosyası üretimi de bu fazın
kapsamında değildir.

---

## 4. Doğrulanmış mevcut durum

2026-08-11 ve 2026-08-12 tarihlerinde mevcut kod yüzeyi doğrulandı.

### 4.1 Mevcut domain

- `Reservation` alanları: birim, kiracı, başlangıç/bitiş, süre/fiyat snapshot'ları,
  durum ve tek `Description` alanı.
- Mevcut durumlar: `Planned`, `Completed`, `Cancelled`, `TransferredToCharge`.
- `UnitTypeUsage.Reservable`, rezervasyon yapılabilir birim türünü belirliyor.
- `Charge.ReservationId`, rezervasyon ile tahakkuk arasında tekil ilişki kuruyor.
- `Reservation` ve ilgili entity'lerde soft-delete/audit altyapısı mevcut.

### 4.2 Mevcut iç kullanıcı akışı

- İç kullanıcı kapsamındaki birim ve kiracıyı seçerek rezervasyon oluşturuyor.
- Oluşturma işlemi doğrudan `Planned` durumuna geçiyor; onay/ret akışı yok.
- Yerel çakışma kontrolü iptal edilmemiş bütün rezervasyonları blok kabul ediyor.
- İptal nedeni `Description` alanının üzerine yazılıyor.
- Tahakkuka aktarım `TransferredToCharge` durumuyla ayrıca işaretleniyor.

### 4.3 Mevcut kiracı akışı

- Tenant controller yalnız kendi rezervasyonlarını listeliyor.
- Tenant create, detail ve cancel endpoint/view'ları bulunmuyor.
- Tenant izolasyonu query filter, mevcut kullanıcı bağlamı ve permission scope ile uygulanıyor.

### 4.4 Mevcut durum/tamamlanma davranışı

- Bitiş zamanı geçen `Planned` rezervasyon bazı liste sorgularında yalnız DTO üzerinde
  `Completed` gösteriliyor.
- Bu davranış veritabanındaki durumu değiştirmiyor ve sayfalar arasında farklı sonuç
  üretme riski taşıyor.
- Yeni modelde sorgu sırasında durum türetilmeyecek; zamanlanmış işlem kalıcı güncelleyecek.

### 4.5 Mevcut altyapı

- Controller doğrudan `ApplicationDbContext` kullanmıyor.
- `ReservationService`, entity repository'leri ve `IUnitOfWork` kullanıyor.
- Servisler `ITransactionalService` ile transaction proxy üzerinden çalışıyor.
- Validator altyapısı projeye ait `IValidator<T>` yapısıdır; FluentValidation kullanılmıyor.
- SQL Server entegrasyon testleri yalnız `KiraTakipDb_Test` adını kabul eden guard içeriyor.

---

## 5. Değişmez mimari ve iş kuralları

- KiraTakip rezervasyon, uygunluk, onay, fiyat, tahakkuk ve audit için tek iş kaynağıdır.
- Controller doğrudan DB işlemi yapmaz.
- Servis doğrudan `ApplicationDbContext` sorgusu yapmaz.
- Repository yalnız gerçek entity karşılığıdır.
- Birden fazla entity kullanan sorgu ana entity'nin custom repository metodunda tutulur.
- ViewModel servis veya repository parametresi olarak kullanılmaz.
- Servis metodu tek primitive alsa bile Request/Command DTO kullanır.
- Gerçek DB tablo/kolon adları değiştirilmez; yeni DB adları Türkçe ve açık mapping'li olur.
- C# kimlikleri İngilizce, kullanıcı metinleri Türkçe kalır.
- Form iş kuralı hatasında aynı view ve kullanıcı girdileri korunur.
- Başarılı redirect işlemleri merkezi “İşlem başarılı” alanını kullanır.
- Permission yalnız buton gizlemeye bırakılmaz; endpoint policy ve servis scope birlikte uygulanır.
- Tenant ID, user ID, scope veya onaylayan kullanıcı bilgisi güvenilir form girdisi sayılmaz.
- `Description`, iptal veya ret bilgisiyle ezilmez.
- `InternalNotes` tenant görünümüne veya tenant DTO'suna taşınmaz.
- Pending talep birimi bloke etmez; yalnız `Confirmed` rezervasyon çakışmayı bloke eder.
- Çakışma create sırasında bilgilendirme, onay sırasında kesin kontrol olarak uygulanır.
- Finansal durum reservation lifecycle enum'una yazılmaz; `Charge.ReservationId` ve
  `Charge.Status` üzerinden izlenir.
- Ücretsiz rezervasyon için tahakkuk oluşturulmaz.
- Onaylı ödemesi bulunan tahakkuka bağlı rezervasyon mevcut koruma olmadan iptal edilmez.
- Fiziksel silme yapılmaz.

---

## 6. Hedef rezervasyon yaşam döngüsü

### 6.1 Hedef `ReservationStatus`

İlk sürümde yalnız şu durumlar bulunur:

```text
PendingApproval
Confirmed
Rejected
Cancelled
Completed
```

| Durum | Anlam |
|---|---|
| `PendingApproval` | Talep oluşturuldu; iç kullanıcı kararı bekleniyor. |
| `Confirmed` | Yetkili onayladı; zaman aralığı yerel sistemde bloke edildi. |
| `Rejected` | Yetkili reddetti; slot bloke edilmez. |
| `Cancelled` | Talep veya onaylı rezervasyon iptal edildi; slot bloke edilmez. |
| `Completed` | Onaylı rezervasyonun bitiş zamanı ve 15 dakikalık bekleme süresi geçti. |

`NoShow`, `PendingCalendarConfirmation`, `RequiresReview`, `TransferredToCharge` ve bütün
Outlook sync durumları yeni enum'a taşınmaz.

### 6.2 Geçerli durum geçişleri

```text
PendingApproval
  ├─ Approve ─────> Confirmed
  ├─ Reject ──────> Rejected
  └─ Cancel ──────> Cancelled

Confirmed
  ├─ Allowed edit -> Confirmed
  ├─ Cancel ──────> Cancelled
  └─ End + 15 dk ─> Completed
```

### 6.3 Geçersiz geçişler

- `Rejected`, `Cancelled` veya `Completed` kayıt yeniden onaylanamaz.
- `PendingApproval`, `Rejected` veya `Cancelled` kayıt tahakkuka aktarılamaz.
- `Completed` kayıt tarih/salon değiştirilerek yeniden aktif hale getirilemez.
- Tenant onay bekleyen talebi düzenleyemez; iptal edip yeni talep oluşturur.
- Tenant onaylı rezervasyonu düzenleyemez.
- İç kullanıcı yalnız `Edit` izni, kapsam ve zaman sınırı uygunsa onaylı kaydı düzenler.

### 6.4 Eski durumların deterministik dönüşümü

| Eski durum | Koşul | Yeni durum | Finansal gerçeklik |
|---|---|---|---|
| `Planned` | Bitiş gelecekte | `Confirmed` | İlişkili charge üzerinden |
| `Planned` | Bitiş geçmişte | `Completed` | İlişkili charge üzerinden |
| `Completed` | Tümü | `Completed` | İlişkili charge üzerinden |
| `Cancelled` | Tümü | `Cancelled` | Mevcut charge durumu korunur |
| `TransferredToCharge` | Bitiş gelecekte | `Confirmed` | İlişkili charge zorunlu |
| `TransferredToCharge` | Bitiş geçmişte | `Completed` | İlişkili charge zorunlu |

Migration, audit sonucu temiz değilse varsayımla veri düzeltmez.

---

## 7. Hedef veri modeli

### 7.1 `Reservation` genişletmesi

Önerilen alanlar:

| C# alanı | Amaç |
|---|---|
| `Title` | Toplantı/rezervasyon başlığı. |
| `Notes` | Tenant ve yetkili iç kullanıcıya gösterilebilen toplantı notu. |
| `InternalNotes` | Yalnız yetkili iç kullanıcıya açık not. |
| `RequestedByUserId` | Talebi oluşturan kullanıcı kimliği. |
| `RequestedByDisplayNameSnapshot` | Oluşturan kişinin o andaki görünen adı. |
| `RequestedByEmailSnapshot` | Oluşturan kişinin o andaki e-posta adresi. |
| `ApprovedByUserId` | Onaylayan iç kullanıcı. |
| `ApprovedAt` | Onay zamanı. |
| `RejectedByUserId` | Reddeden iç kullanıcı. |
| `RejectedAt` | Ret zamanı. |
| `RejectionReason` | Açıklamadan ayrı ret nedeni. |
| `CancelledByUserId` | İptal eden kullanıcı. |
| `CancelledAt` | İptal zamanı. |
| `CancellationReason` | Açıklamadan ayrı iptal nedeni. |
| `RowVersion` | SQL Server eşzamanlı karar/güncelleme koruması. |

Kurallar:

- `Description` mevcut kayıt uyumluluğu için korunur; yeni UI'da genel açıklama olarak
  kullanılıp kullanılmayacağı Faz 1 Implementation Plan'da netleştirilir.
- Yeni taleplerde `Title`, talep sahibi snapshot'ı ve yaşam döngüsü alanları doğrulanır.
- Eski kayıtlara veri uydurulmaz; elde edilemeyen snapshot alanları migration'da nullable kalır.
- User FK silme davranışı geçmiş audit bilgisini kaybetmeyecek şekilde `Restrict/NoAction` olur.

### 7.2 `ReservationAttendee`

Katılımcı listesi yerel iş bilgisi olarak korunur.

```text
Id
ReservationId
DisplayName
EmailAddress
NormalizedEmailAddress
IsReservationOwner
CreatedAt / CreatedBy / UpdatedAt / UpdatedBy / IsDeleted / IsActive
```

Kurallar:

- Katılımcıya e-posta veya takvim daveti gönderilmez.
- E-posta zorunlu ve normalize edilir.
- Aynı rezervasyonda aynı normalize e-posta yalnız bir kez bulunur.
- Rezervasyonu oluşturan kişi katılımcı listesinde owner olarak tutulur.
- Tenant yalnız kendi rezervasyonunun katılımcılarını görebilir.
- Katılımcı türü required/optional ayrımı Outlook kaldırıldığı için eklenmez.

### 7.3 Eklenmeyecek modeller

- `ReservationOutlookSync`
- `IntegrationOutboxMessage`
- `OutlookSyncStatus`
- `Unit.OutlookIntegrationEnabled`
- `Unit.OutlookRoomMailboxAddress`
- `Unit.OutlookTimeZone`
- Graph/options/sertifika entity veya repository'leri

---

## 8. Yerel uygunluk ve çakışma modeli

### 8.1 Uygunluk sonucu

```text
Available
BusyConfirmed
PendingRequestExists
UnavailableByPolicy
```

- `Confirmed` ve henüz bitmemiş rezervasyon slotu bloke eder.
- `PendingApproval` slotu bloke etmez; kullanıcıya “Bu saat için bekleyen talep var”
  bilgisi gösterilebilir.
- `Rejected`, `Cancelled` ve `Completed` slotu bloke etmez.
- Tenant başka talebin başlık, not, katılımcı veya sahibi bilgisini göremez.

### 8.2 Kesin onay kontrolü

Create sırasında görülen boşluk kesin onay garantisi değildir. Onay işleminde:

1. Permission ve scope doğrulanır.
2. RowVersion doğrulanır.
3. Birim aktif ve rezervasyona uygun doğrulanır.
4. Yerel zaman aralığı çakışması tekrar kontrol edilir.
5. Aynı transaction içinde durum `Confirmed` yapılır ve audit alanları yazılır.

Aynı anda yapılan iki onaydan yalnız birinin başarılı olması SQL Server entegrasyon
testiyle kanıtlanmadan faz tamamlanmaz. Kesin kilitleme stratejisi Faz 1 Implementation
Plan'da mevcut transaction altyapısına göre kullanıcı onayına sunulur.

---

## 9. Servis ve repository sınırları

### 9.1 Uygulama servisleri

| Sorumluluk | Önerilen servis |
|---|---|
| Liste, detail, talep ve lifecycle operasyonları | `IReservationService` veya SRP gerekirse ayrılan servisler |
| Yerel uygunluk ve takvim sorgusu | `IReservationAvailabilityService` |
| Bitişi geçen kayıtları tamamlama | `IReservationCompletionService` |
| Zamanlanmış çalıştırma | `ReservationCompletionBackgroundService` |

Servis bölme kararı dosya ve metot büyüklüğü incelenerek Implementation Plan'da sunulur;
isimler önceden var kabul edilmez.

### 9.2 Repository'ler

| Repository | Sorumluluk |
|---|---|
| `IReservationRepository` | Reservation sorgu/projeksiyonları, conflict, lifecycle ve completion adayları |
| `IReservationAttendeeRepository` | Gerçek `ReservationAttendee` entity işlemleri |
| `IUnitRepository` | Reservable unit ve yerel uygunluk için birim bilgileri |
| `IChargeRepository` | Reservation–charge ilişkisi ve finansal korumalar |

Entity karşılığı olmayan `ReservationPortalRepository`, `CalendarRepository` veya benzeri
repository oluşturulmaz.

### 9.3 Command/query sözleşmeleri

Beklenen command/query grupları:

```text
CreateReservationRequestInput
ApproveReservationInput
RejectReservationInput
CancelReservationInput
UpdateConfirmedReservationInput
GetReservationAvailabilityInput
GetPendingReservationsPageInput
CompleteExpiredReservationsInput
```

Kesin ad ve alanlar her iç fazın Implementation Plan'ında mevcut DTO standardıyla
karşılaştırılarak onaylanır.

---

## 10. Yetki ve kapsam modeli

### 10.1 İç kullanıcı izinleri

Mevcut izinler korunur:

```text
Internal.Reservation
Internal.Reservation.Create
Internal.Reservation.Edit
Internal.Reservation.Cancel
Internal.Reservation.TransferToCharge
```

Yeni izinler:

```text
Internal.Reservation.Approve
Internal.Reservation.Reject
```

`Internal.Reservation.ManageOutlookSync` eklenmez.

### 10.2 Tenant izinleri

```text
Tenant.Reservation
Tenant.Reservation.Create
Tenant.Reservation.Cancel
```

Tenant edit izni ilk sürümde eklenmez.

### 10.3 Scope

- Tenant yalnız mevcut permission scope kapsamında açıkça erişebildiği reservable
  birimleri görür.
- İç kullanıcı property veya doğrudan unit scope birleşimiyle çalışır.
- Global access mevcut proje davranışına uygun korunur.
- Liste, detail, create, approve, reject, update, cancel ve transfer operasyonlarının
  tamamı doğrudan URL/POST denemelerine karşı scope uygular.

---

## 11. Politika, validasyon ve UX kararları

- Pending talep slotu bloke etmez.
- Tenant pending talebi düzenlemez; iptal edip yeniden oluşturur.
- İç kullanıcı talebi varsayılan olarak onaya gönderir.
- `Approve` yetkili iç kullanıcı için “oluştur ve onayla” seçeneği bulunur ve audit edilir.
- İptal ve onaylı rezervasyon güncelleme sınırı yapılandırılabilir; varsayılan 120 dakikadır.
- Rezervasyon, bitişten 15 dakika sonra zamanlanmış işlemle kalıcı `Completed` yapılır.
- İlk sürümde check-in ve `NoShow` bulunmaz.
- Form validasyonu yalnız proje `IValidator<T>` yapısıyla uygulanır.
- Business validation controller'da kopyalanmaz; servis Guard kuralları kullanılır.
- Validasyon hatasında aynı view, doldurulmuş option listeleri ve kullanıcının girdileri korunur.
- Başarı redirect sonrası merkezi alanda gösterilir.
- NotFound/Forbidden gibi forma bağlı olmayan istisnalar mevcut merkezi hata davranışını kullanabilir.

---

## 12. İç Faz 0 — Kapsam revizyonu ve mevcut sistem doğrulaması

### Amaç

Outlook kapsamını temizlemek, yerel iş kararlarını kesinleştirmek ve Faz 1 planını
uygulanabilir hale getirmek.

### Görevler

- [x] Mevcut reservation entity, enum, servis, repository, controller, DTO, ViewModel,
  validator, view, migration ve test yüzeyi doğrulandı.
- [x] Tenant ve iç kullanıcı mevcut akışları doğrulandı.
- [x] Pending talebin slotu bloke etmeyeceği kararlaştırıldı.
- [x] Tenant edit yerine iptal + yeni talep davranışı onaylandı.
- [x] İç kullanıcı create ve create+approve davranışı onaylandı.
- [x] Varsayılan iptal/güncelleme sınırı 120 dakika olarak kararlaştırıldı.
- [x] Bitişten 15 dakika sonra kalıcı `Completed` davranışı kararlaştırıldı.
- [x] Outlook/Microsoft 365 entegrasyonu kapsamdan çıkarıldı.
- [x] Outlook'a özgü model, servis, permission ve fazlar plandan çıkarıldı.
- [-] Faz 1'in kesin dosya/imza/migration/test planı kullanıcı onayına sunulacak.

### Tamamlanma kapısı

- [x] Açık Microsoft 365 bağımlılığı kalmadı.
- [x] Yerel rezervasyon iş kararları dokümana işlendi.
- [x] Mevcut kod yüzeyi doğrulandı.
- [x] Faz 1 Implementation Plan kullanıcı tarafından onaylandı.
- [x] `PROGRESS.md` aktif fazı İç Faz 1'e geçirildi.

### Bu fazda yapılmayacaklar

- Uygulama kodu veya migration yazmak.
- Mevcut rezervasyon verisini değiştirmek.
- Outlook/Graph için paket veya config eklemek.

---

## 13. İç Faz 1 — Domain modeli, migration ve durum geçişleri

### Amaç

Yeni yerel yaşam döngüsünü, katılımcı modelini, audit alanlarını ve güvenli veri
dönüşümünü kurmak.

### Görev grupları

#### 1A — Enum ve lifecycle

- [x] Yeni `ReservationStatus` değerleri ve sayısal karşılıkları kesinleştirildi.
- [x] Geçerli/geçersiz durum geçişleri merkezi sözleşmeye bağlandı.
- [x] `TransferredToCharge` bağımlılıkları finansal `Charge.ReservationId` ilişkisine taşındı.
- [x] DTO üzerinde sessiz `Planned → Completed` dönüşümü kaldırıldı.

#### 1B — Entity modeli

- [x] Reservation başlık, not, internal note, talep sahibi snapshot ve karar audit alanları eklendi.
- [x] Cancellation/ret alanları `Description` alanından ayrıldı.
- [x] SQL Server `RowVersion` concurrency token eklendi.
- [x] `ReservationAttendee` entity ve reservation navigation'ı eklendi.
- [x] Outlook/sync/outbox alanı eklenmediği doğrulandı.

#### 1C — EF mapping ve repository

- [x] Yeni alan/tablo/FK/index/constraint'ler Türkçe DB adlarıyla map edildi.
- [x] Attendee normalize e-posta unique index'i eklendi.
- [x] Reservation tarih/conflict sorgu index'i doğrulandı.
- [x] Entity karşılığı olan attendee repository eklendi ve DI kaydı yapıldı.
- [x] Reservation repository lifecycle/concurrency ihtiyaçları için güncellendi.

#### 1D — Veri audit ve migration

- [x] `TransferredToCharge` olup charge bulunmayan kayıtlar sayıldı; sorun bulunmadı.
- [x] Aynı reservation'a bağlı birden fazla charge kontrol edildi; sorun bulunmadı.
- [x] Geçersiz tarih, aktif çakışma ve pasif birime bağlı gelecek kayıtlar raporlandı; sorun bulunmadı.
- [x] Audit güvenlik kontrolleri migration içine durdurucu kurallar olarak eklendi.
- [x] Eski statüler deterministik tabloya göre dönüştürüldü.
- [x] Migration mevcut SQL Server `KiraTakipDb_Test` üzerinde up/down/up doğrulandı.

#### 1E — Test ve uyarlama

- [x] Servis, repository, HomeController, SeedDataService, DTO ve view enum referansları uyarlandı.
- [x] Lifecycle, RowVersion model ve katılımcı unique index testleri eklendi.
- [x] Migration kayıt/FK/charge ilişkisi testleri geçti.
- [x] Hedefli test, tam test projesi ve build başarılı.

### Tamamlanma kapısı

- [x] Yeni lifecycle ve migration veri kaybı olmadan çalışıyor.
- [x] Attendee ve concurrency modeli SQL Server test DB'de doğrulandı.
- [x] Outlook'a özel alan veya bağımlılık eklenmedi.
- [x] İç Faz 2 Implementation Plan kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- Controller/view ile yeni talep/onay akışını açmak.
- Permission veya politika endpoint'lerini eklemek.
- Background completion worker çalıştırmak.

---

## 14. İç Faz 2 — Yetkiler, kapsam ve rezervasyon politikaları

### Amaç

Onay/ret izinlerini, tenant–unit kapsamını ve merkezi politika/Guard sözleşmesini kurmak.

### Görevler

- [x] `Approve`, `Reject` ve zaman sınırı istisnası permission'ları, etiketleri ve preset dağılımları eklendi.
- [x] Outlook permission'ı bulunmadığı doğrulandı.
- [x] Tenant ve iç kullanıcı reservable unit erişim/scope sözleşmeleri kesinleştirildi.
- [x] Varsayılan 120 dakika iptal/güncelleme sınırı options/policy modeline taşındı.
- [x] 15 dakikalık completion grace ayarı merkezi hale getirildi.
- [x] Başlık, not, attendee, süre, geçmiş tarih ve reservable unit kuralları tanımlandı.
- [x] Lifecycle/cancel/scope için Guard hata kodları ve Türkçe mesajlar tanımlandı; approve/reject endpoint bağları İç Faz 5'te kurulacak.
- [x] Permission katalog ve servis scope testlerinde doğrulandı.
- [x] Tenant sahiplik Guard'ı ve mevcut query filter başka tenant erişimini engelledi.

### Tamamlanma kapısı

- [x] Permission, preset, policy, Guard ve scope testleri başarılı.
- [x] Mevcut tenant endpoint izolasyonu ve servis sahiplik kuralı doğrulandı; yeni POST senaryoları endpoint'lerle birlikte test edilecek.
- [x] İç Faz 3 Implementation Plan kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- Yeni create/approve UI açmak.
- Takvim görünümü yazmak.
- Tahakkuk veya completion davranışını değiştirmek.

---

## 15. İç Faz 3 — Yerel uygunluk ve takvim görünümü

### Amaç

Yalnız KiraTakip rezervasyonlarından güvenli ve kapsamlı uygunluk sonucu üretmek.

### Görevler

- [x] Yerel availability command/result DTO'ları tanımlandı.
- [x] Reservation repository interval-overlap ve pending bilgi sorguları eklendi.
- [x] `Confirmed` kayıtların bloke, pending kayıtların bilgi olduğu kural uygulandı.
- [x] İç kullanıcı takvim DTO'su gerekli operasyon bilgisini taşıdı.
- [x] Tenant takvim DTO'su başka rezervasyonun özel verisini taşımadı.
- [x] Haftalık görünüm endpoint/view'ları eklendi.
- [x] Türkiye saati ve sınır çakışmaları test edildi.
- [x] Scope dışı birimlerin takvimde görünmediği doğrulandı.

### Tamamlanma kapısı

- [x] Yerel uygunluk sonucu doğru ve gizlilik korumalı.
- [x] Pending/confirmed ayrımı ve sınır çakışmaları test edildi.
- [x] İç Faz 4 Implementation Plan kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- Talep oluşturma veya onay POST akışını eklemek.
- Dış takvim verisi kullanmak.

---

## 16. İç Faz 4 — Talep oluşturma ve tenant portalı

### Amaç

Tenant ve iç kullanıcının güvenli biçimde `PendingApproval` talebi oluşturmasını sağlamak.

### Görevler

- [x] İç kullanıcı ve tenant create ViewModel'leri ayrıldı.
- [x] ViewModel'ler için proje `IValidator<T>` sınıfları eklendi.
- [x] ViewModel → command mapper'ları oluşturuldu.
- [x] Tenant kimliği `ICurrentUserContext` üzerinden alındı; formdan güvenilir sayılmadı.
- [x] Başlık, notlar, katılımcılar, birim, tarih ve fiyat snapshot'ı kaydedildi.
- [x] Talep sahibi attendee owner olarak normalize edildi.
- [x] Create sırasında policy, scope, unit ve doğrudan onay yerel uygunluğu tekrar doğrulandı.
- [x] Yeni talep `PendingApproval` oluşturuldu.
- [x] İç kullanıcı için create ve yetkiliyse create+approve seçenekleri ayrıldı.
- [x] Tenant list/detail/create/cancel endpoint ve view'ları eklendi.
- [x] Validasyon hatasında girdiler ve form seçenekleri korundu.
- [x] Başarı redirect sonrası merkezi alanda gösterildi.

### Tamamlanma kapısı

- [x] Tenant yalnız kendi adına ve kendi kapsamındaki unit için talep oluşturabiliyor.
- [x] İç kullanıcı kapsam ve permission kurallarıyla talep oluşturabiliyor.
- [x] Create+approve ayrı permission ve audit ile çalışıyor.
- [x] Katılımcı normalize/unique, fiyat snapshot ve input preservation doğrulamaları başarılı.
- [x] İç Faz 5 Implementation Plan kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- Pending talebi bloke edici kabul etmek.
- Tenant edit endpoint'i eklemek.
- Tahakkuk oluşturmak.

---

## 17. İç Faz 5 — Onay, ret, güncelleme ve iptal

### Amaç

İç kullanıcı kararlarını eşzamanlılık ve kesin çakışma kontrolüyle uygulamak.

### Görevler

- [x] Pending queue ve detail DTO/view'ları eklendi.
- [x] Approve command permission, scope, RowVersion ve kesin conflict kontrolü uyguladı.
- [x] Aynı unit için transaction-owned application lock stratejisi SQL Server'da uygulandı.
- [x] Başarılı onay `Confirmed` ve audit alanlarını aynı transaction'da yazdı.
- [x] Reject command gerekçe ve audit alanlarını yazdı; slot bloke etmedi.
- [x] Pending cancel tenant ve iç kullanıcı için scope/zaman kurallarıyla çalıştı.
- [x] Confirmed update yalnız iç kullanıcı `Edit` izni ve 120 dakika sınırıyla çalıştı.
- [x] Unit/time update sırasında kesin conflict tekrar kontrol edildi.
- [x] Confirmed cancel mevcut charge/payment korumalarını sürdürdü.
- [x] `Description` iptal veya ret sırasında ezilmedi.
- [x] Paralel çakışan approve, stale karar ve terminal durum SQL Server testleri eklendi.

### Tamamlanma kapısı

- [x] Aynı talepte iki karar `RowVersion` nedeniyle birlikte başarılı olamıyor.
- [x] Çakışan iki talebin aynı slot için ikisi birden onaylanamıyor.
- [x] Permission, scope, cutoff, audit ve input preservation testleri başarılı.
- [x] İç Faz 6 Implementation Plan kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- Tahakkuk kurallarını lifecycle status'a kopyalamak.
- Background completion worker eklemek.

---

## 18. İç Faz 6 — Tahakkuk ve otomatik tamamlama

### Amaç

Rezervasyon lifecycle'ı ile finansal ilişkiyi ayırmak ve süresi geçen kayıtları kalıcı
olarak tamamlamak.

### Görevler

- [x] `TransferredToCharge` güncel kod/view/test bağımlılıklarında bulunmadı; yalnız tarihsel migration metni kaldı.
- [x] Tahakkuk varlığı `Charge.ReservationId` üzerinden projekte edildi.
- [x] Yalnız `Confirmed` ve ücretli reservation tahakkuka aktarılabildi.
- [x] Aynı reservation için ikinci charge engellendi.
- [x] Ücretsiz reservation için charge üretilmedi.
- [x] Tahakkuka aktarım reservation lifecycle status'unu değiştirmedi.
- [x] İptal/payment korumaları yeni lifecycle ile doğrulandı.
- [x] Completion aday sorgusu `Confirmed` ve `EndDate + 15 dakika <= now` kuralını kullandı.
- [x] Completion servisi command DTO ve repository üzerinden kalıcı `Completed` geçişi yaptı.
- [x] Background service her kayıt için ayrı scope/transaction açarak servisi yapılandırılabilir aralıkta çağırdı.
- [x] Sayfa sorgularında türetilmiş `Completed` davranışı bulunmadığı doğrulandı.
- [x] Worker kayıt ve batch hatalarında uygulamayı durdurmadan yapılandırılmış log üretti.

### Tamamlanma kapısı

- [x] Finansal durum reservation status'tan bağımsız.
- [x] Duplicate/free/payment/cancel korumaları başarılı.
- [x] Bitişten 15 dakika sonra kalıcı completion çalışıyor ve idempotent.
- [x] İç Faz 7 Implementation Plan ve release kontrolü kullanıcı tarafından onaylandı.

### Bu fazda yapılmayacaklar

- `NoShow` veya check-in eklemek.
- Dış takvim veya bildirim entegrasyonu eklemek.

---

## 19. İç Faz 7 — Test, UAT ve kontrollü yayın

### Amaç

Yerel rezervasyon akışını otomatik testler ve kullanıcı kabul senaryolarıyla doğrulamak.

Uygulanabilir yayın, Windows Server, geri dönüş ve manuel UAT adımları:
[`phase-18-yayin-ve-kullanici-kabul-kontrol-listesi.md`](phase-18-yayin-ve-kullanici-kabul-kontrol-listesi.md)

### Otomatik doğrulamalar

- [x] Ana proje build başarılı.
- [x] Tam test projesi başarılı.
- [x] Migration temiz geçici SQL Server test DB'de ileri–geri uygulanarak başarılı.
- [x] Lifecycle ve geçersiz geçiş testleri başarılı.
- [x] Permission/scope ve tenant izolasyon testleri başarılı.
- [x] Yerel availability, pending ve sınır conflict testleri başarılı.
- [x] Paralel approve/reject/update ve RowVersion testleri başarılı.
- [x] Attendee normalize/unique ve privacy testleri başarılı.
- [x] Tahakkuk/ödeme/iptal regresyon testleri başarılı.
- [x] Completion worker idempotency ve graceful hata testleri başarılı.
- [x] Kiracı kullanıcı kapsamı düzenleme ekranında global, sözleşmeli ve rezervasyon birimi seçenekleri güvenli biçimde yönetildi.
- [x] Aynı kapsam yönetimi `System.User.Edit` yetkisiyle iç yönetim paneline bağlandı ve ortak form alanlarında birleştirildi.
- [x] Controller/form input preservation ve merkezi başarı davranışı başarılı.

### Zorunlu UAT senaryoları

1. Tenant kapsamındaki salona talep oluşturur; yetkili onaylar.
2. Tenant kapsam dışı salonu göremez ve doğrudan URL ile erişemez.
3. Pending talep slotu bloke etmez; aynı saat için ikinci talep oluşturulabilir.
4. Aynı slot için iki pending talepten yalnız biri onaylanabilir.
5. Yetkili talebi reddeder; ret nedeni tenant'a güvenli biçimde görünür.
6. Tenant pending talebi iptal edip yeni talep oluşturur.
7. İç kullanıcı create+approve işlemini gerekli permission ile yapar.
8. Onaylı rezervasyon izin verilen sürede güncellenir; çakışan saat reddedilir.
9. 120 dakika sınırı içindeki güncelleme/iptal politika kararına göre engellenir.
10. Ücretli confirmed reservation tek charge üretir; ikinci transfer reddedilir.
11. Ücretsiz reservation için tahakkuk oluşturulmaz.
12. Onaylı ödemesi bulunan tahakkuka bağlı iptal korunur.
13. Bitişten 15 dakika sonra reservation kalıcı `Completed` olur.
14. Tenant başka reservation'ın internal note veya katılımcılarını göremez.
15. Validasyon hatasında form girdileri korunur; başarı merkezi alanda görünür.

### Kapanış

- [x] Bilinen açık kritik/yüksek risk kalmadı.
- [x] Kullanıcı UAT ve Faz 18 kapanış sonucunu 2026-08-17 tarihinde onayladı.
- [x] Production migrationları uygulandı; migration geçmişi ve kayıt korunumu doğrulandı.
- [x] `PROGRESS.md` ve `MASTER-PLAN.md` gerçek durumla güncellendi.

Windows Server completion worker'ının gerçek yayın ortamındaki gözlemi, Faz 18'i yeniden
açmadan yürütülecek operasyonel yayın kontrolü olarak bırakıldı.

---

## 20. Faz bağımlılık haritası

```text
Faz 0  Kapsam ve mevcut sistem
  │
  ▼
Faz 1  Domain + migration
  │
  ▼
Faz 2  Permission + scope + policy
  │
  ▼
Faz 3  Yerel uygunluk + takvim
  │
  ▼
Faz 4  Talep oluşturma + tenant portalı
  │
  ▼
Faz 5  Onay + ret + update + cancel
  │
  ▼
Faz 6  Tahakkuk + otomatik tamamlama
  │
  ▼
Faz 7  Test + UAT + yayın
```

Bir fazın tamamlanma kapısı geçilmeden sonraki fazın kodu yazılmaz.

---

## 21. Beklenen dosya etkileri

Bu liste kesin Implementation Plan değildir; her iç fazda doğrulanıp daraltılır.

### Mevcut dosya grupları

- `KiraTakip/Models/Entities/Reservation.cs`
- `KiraTakip/Models/Entities/Unit.cs`
- `KiraTakip/Models/Enums.cs`
- `KiraTakip/Data/ApplicationDbContext.cs`
- `KiraTakip/Authorization/PermissionCatalog.cs`
- `KiraTakip/Controllers/ReservationController.cs`
- `KiraTakip/Controllers/TenantReservationController.cs`
- `KiraTakip/Services/Interfaces/IReservationService.cs`
- `KiraTakip/Services/ReservationService.cs`
- `KiraTakip/Repositories/Interfaces/IReservationRepository.cs`
- `KiraTakip/Repositories/ReservationRepository.cs`
- `KiraTakip/Repositories/Interfaces/IUnitRepository.cs`
- `KiraTakip/Repositories/UnitRepository.cs`
- Reservation DTO, ViewModel, mapper, validator ve view dosyaları
- `KiraTakip/Controllers/HomeController.cs`
- `KiraTakip/Services/SeedDataService.cs`
- `KiraTakip/Infrastructure/DependencyInjection/*Module.cs`
- `KiraTakip/Program.cs`
- EF migration ve model snapshot
- Reservation, tenant reservation ve charge testleri

### Beklenen yeni dosya grupları

- `ReservationAttendee` entity ve repository
- Yeni lifecycle/availability/approval command-query DTO'ları
- Approval/reject/update ViewModel ve `IValidator<T>` sınıfları
- Tenant create/detail/cancel view'ları
- Yerel availability/takvim view'ları
- Completion service ve background service
- EF Core migration
- İlgili unit, repository, service, controller ve SQL Server integration testleri

### Eklenmeyecek dosya grupları

- Graph calendar client
- Microsoft calendar options
- Outlook sync entity/repository/service
- Integration outbox/processor/worker
- Webhook veya reconcile controller/service/view

---

## 22. Karar kayıtları

| No | Konu | Karar | Tarih | Durum |
|---:|---|---|---|---|
| 1 | Ana iş kaynağı | Rezervasyon ve uygunluk için yalnız KiraTakip kullanılacak. | 2026-08-12 | Onaylandı |
| 2 | Outlook entegrasyonu | Lisans ve operasyon maliyeti nedeniyle Faz 18'den tamamen çıkarıldı. | 2026-08-12 | Onaylandı |
| 3 | Pending slot | Pending talep slotu bloke etmeyecek; onayda kesin conflict kontrolü yapılacak. | 2026-08-11 | Onaylandı |
| 4 | Tenant salon kapsamı | Tenant yalnız permission scope kapsamında erişebildiği reservable birimleri görecek. | 2026-08-11 | Onaylandı |
| 5 | Tenant edit | Pending talep düzenlenmeyecek; iptal edilip yeni talep oluşturulacak. | 2026-08-11 | Onaylandı |
| 6 | İç kullanıcı create | Varsayılan pending; `Approve` yetkisi varsa audit edilen create+approve kullanılabilecek. | 2026-08-11 | Onaylandı |
| 7 | İptal/güncelleme sınırı | Yapılandırılabilir; varsayılan 120 dakika. | 2026-08-11 | Onaylandı |
| 8 | Otomatik tamamlanma | Bitişten 15 dakika sonra zamanlanmış işlem kalıcı `Completed` yapacak. | 2026-08-11 | Onaylandı |
| 9 | Check-in/NoShow | İlk sürüm kapsamına alınmayacak. | 2026-08-11 | Onaylandı |
| 10 | Not görünürlüğü | `Notes` tenant'a gösterilebilir; `InternalNotes` yalnız iç kullanıcıya açık. | 2026-08-11 | Onaylandı |
| 11 | Katılımcılar | Yerel listede tutulacak; e-posta veya takvim daveti gönderilmeyecek. | 2026-08-12 | Plan kararı |
| 12 | Finansal durum | Reservation status'tan ayrılacak; `Charge.ReservationId` ana kaynak olacak. | 2026-08-12 | Mimari karar |

---

## 23. Genel ilerleme

- [x] İç Faz 0 — Kapsam revizyonu ve mevcut sistem doğrulaması
- [x] İç Faz 1 — Domain modeli, migration ve durum geçişleri
- [x] İç Faz 2 — Yetkiler, kapsam ve rezervasyon politikaları
- [x] İç Faz 3 — Yerel uygunluk ve takvim görünümü
- [x] İç Faz 4 — Talep oluşturma ve tenant portalı
- [x] İç Faz 5 — Onay, ret, güncelleme ve iptal
- [x] İç Faz 6 — Tahakkuk ve otomatik tamamlama
- [x] İç Faz 7 — Test, UAT ve kontrollü yayın
