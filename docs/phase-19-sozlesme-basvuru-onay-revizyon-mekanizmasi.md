# KiraTakip — Faz 19: Sözleşme Başvuru, Onay ve Revizyon Mekanizması

> **Doküman türü:** Faz bazlı uygulama planı, teknik spesifikasyon ve karar kaydı  
> **Durum:** Tamamlandı — 2026-08-08 kullanıcı kabulü, `IsSuperAdmin` istisnası dahil 172/172 regresyon ve test DB sağlık kontrolleri başarılı  
> **Son güncelleme:** 2026-08-08  
> **Kapsam:** İç kullanıcı sözleşme başvurusu, taslak düzenleme, onay, revizyon, açıklamalı silme ve onay sonrası tahakkuk üretimi  
> **İlgili mevcut alan:** `Lease`, `LeaseRateOverride`, `LeaseActivityLog`, `Charge`, sözleşme belgeleri ve `PermissionCatalog.Lease`

---

## 1. Dokümanın amacı

Bu doküman, sözleşme oluşturma akışının doğrudan aktif sözleşme ve tahakkuk oluşturan mevcut davranıştan, kontrollü bir başvuru ve onay mekanizmasına dönüştürülmesi için tek teknik referans kaynağıdır.

Hedeflenen iş akışı:

1. İç kullanıcı mevcut sözleşme formunu doldurur.
2. Form gönderildiğinde aktif sözleşme veya tahakkuk oluşturulmaz.
3. Girilen bilgiler `Draft` durumunda bir sözleşme başvurusu olarak saklanır.
4. Yetkili iç kullanıcı başvuruyu inceler.
5. Yetkili kullanıcı başvuruyu:
   - onaylayabilir,
   - açıklama girerek revizyona gönderebilir,
   - açıklama girerek silebilir.
6. Revizyon istenen başvuru sahibi mevcut sözleşme formu üzerinden bilgileri değiştirir ve yeniden onaya gönderir.
7. Bir sözleşme için birden fazla revizyon turu ve birden fazla açıklama saklanabilir.
8. Başvuru onaylandığında sözleşme aktif hale gelir ve mevcut tahakkuk üretim süreci aynen çalıştırılır.
9. Onay öncesindeki kayıtlar kiracı portalında sözleşme olarak gösterilmez.

Bu doküman kodun yerine geçmez. Her faz uygulanmadan önce ilgili mevcut kod yeniden okunmalı, dosya ve metot adlarının hâlâ geçerli olduğu doğrulanmalı ve yalnızca o fazın kapsamı uygulanmalıdır.

---

## 2. Dokümanın kullanım kuralları

Her faz aşağıdaki sırayla yürütülür:

1. Fazın ön koşulları kontrol edilir.
2. Fazın etkilediği mevcut dosyalar baştan sona okunur.
3. Bu dokümandaki değişmez kararlarla güncel kod arasında çelişki olup olmadığı kontrol edilir.
4. Faz için uygulanacak dosya ve imza değişiklikleri kullanıcıya özetlenir.
5. Kullanıcı onayı alınır.
6. Yalnızca onaylanan faz uygulanır.
7. İlgili testler çalıştırılır.
8. Fazın tamamlanma ölçütleri doğrulanır.
9. Bu dokümandaki checklist güncellenir.
10. Tamamlanma ölçütleri sağlanmadan sonraki faza geçilmez.

### 2.1 Durum işaretleri

- `[ ]` Başlanmadı
- `[-]` Devam ediyor
- `[x]` Tamamlandı ve doğrulandı
- `[!]` Bloke; kullanıcı kararı veya harici hazırlık bekliyor

### 2.2 Varsayım yasağı

Uygulama sırasında aşağıdaki konular geliştirici tarafından yeniden yorumlanmamalıdır:

- Taslak kaydın ayrı bir başvuru tablosunda tutulup tutulmayacağı.
- Tahakkukun taslak aşamasında üretilip üretilmeyeceği.
- Kiracı kullanıcısının taslakları görüp görmeyeceği.
- Revizyon ve silme açıklamasının zorunlu olup olmadığı.
- Silme işleminin fiziksel mi soft-delete mi olacağı.
- Başvuruyu oluşturan kullanıcının kendi başvurusunu onaylayıp onaylayamayacağı.
- Bir birim için aynı anda birden fazla açık başvuru bulunup bulunamayacağı.
- Revizyon geçmişinin `LeaseActivityLog` içinde mi ayrı tabloda mı tutulacağı.

Bu konular §5'te değişmez kararlara bağlanmıştır. Karar değişecekse önce bu doküman güncellenmelidir.

### 2.3 Terimler

| Terim | Anlam |
|---|---|
| Başvuru | `Draft` veya `RevisionRequested` durumundaki, henüz operasyonel sözleşme sayılmayan `Lease` kaydı |
| Taslak | İlk gönderimi yapılmış ve yetkili onayı bekleyen başvuru |
| Revizyon | Yetkili kullanıcının açıklamayla değişiklik istediği başvuru durumu |
| Aktifleştirme | Onayın ardından `Lease.Status = Active` yapılması ve tahakkukların üretilmesi |
| İnceleme geçmişi | Başvuru oluşturma, güncelleme, revizyon, yeniden gönderim, onay ve silme olaylarının eklemeli geçmişi |
| Başvuru sahibi | `Lease.CreatedBy` ile belirlenen, başvuruyu ilk oluşturan iç kullanıcı |
| Değerlendirici | Onay, revizyon veya başvuru silme iznine sahip iç kullanıcı |

---

## 3. Kaynak gereksinim

### 3.1 Zorunlu iş davranışı

- Mevcut sözleşme oluşturma ekranı korunmalıdır.
- Form gönderildiğinde `LeaseStatus.Active` atanmamalıdır.
- Form gönderildiğinde hiçbir sözleşme tahakkuku oluşturulmamalıdır.
- Başvuru `Draft` durumunda kaydedilmelidir.
- Yetkili iç kullanıcı başvuruyu onaylayabilmeli, revizyona gönderebilmeli veya silebilmelidir.
- Revizyon açıklaması zorunlu olmalıdır.
- Silme açıklaması zorunlu olmalıdır.
- Bir sözleşme için birden fazla revizyon ve açıklama tutulabilmelidir.
- Taslak ve revizyon ekranı mevcut oluşturma formunu yeniden kullanmalıdır.
- Ekranın üstünde durum ve son inceleme mesajı gösterilmelidir.
- Onay sonucunda mevcut sözleşme oluşturma ve tahakkuk üretme iş kuralları çalışmalıdır.

### 3.2 Başarı tanımı

Akış yalnızca aşağıdaki koşullar birlikte sağlandığında başarılı sayılır:

- Taslak oluşturma sonunda veritabanında tahakkuk yoktur.
- Taslak, aktif sözleşme sorgularına girmez.
- Taslak, kiracı portalında görünmez.
- Yetkisiz kullanıcı karar veremez.
- Revizyon ve silme açıklamasız yapılamaz.
- Bütün revizyon turları sıralı olarak görüntülenebilir.
- Onay işlemi tek transaction içinde aktifleşme ve tahakkuk üretimini tamamlar.
- Onay sırasında hata oluşursa sözleşme aktifleşmiş halde bırakılmaz.
- Aynı başvuru iki kez onaylanamaz ve mükerrer tahakkuk oluşturulamaz.

---

## 4. Mevcut sistemin doğrulanmış durumu

Bu bölüm 2026-08-07 tarihinde mevcut kod üzerinden doğrulanmıştır. İlgili faz başlarken yeniden kontrol edilmelidir.

### 4.1 Mevcut `Lease` modeli

`KiraTakip/Models/Entities/Lease.cs` içinde sözleşme:

- `UnitId`
- `TenantId`
- `Status`
- KDV ve vade alanları
- başlangıç/bitiş tarihleri
- fesih alanları
- açıklama
- `LeaseActivityLog`
- `LeaseRateOverride`

ilişkilerini taşımaktadır.

`Status` varsayılanı şu anda `LeaseStatus.Active` değeridir.

### 4.2 Mevcut sözleşme durumları

`KiraTakip/Models/Enums.cs` içindeki mevcut değerler:

```text
Active     = 1
Ended      = 2
Terminated = 3
```

Mevcut sayısal değerler migration uyumluluğu için değiştirilmeyecektir.

### 4.3 Mevcut oluşturma akışı

`LeaseService.CreateAsync` mevcut durumda:

1. Birimi ve yetki kapsamını doğrular.
2. Birimin kiralanabilirliğini doğrular.
3. Kiracıyı doğrular.
4. Birimde devam eden aktif sözleşme kontrolü yapar.
5. Tarife override'larını doğrular.
6. `Lease` kaydını `Active` durumunda ekler.
7. `LeaseRateOverride` kayıtlarını yazar.
8. Güncel tarife kalemlerini hesaplar.
9. `LeaseActivityType.Creation` kaydı ekler.
10. `ChargeGenerationService.GenerateForLeaseAsync` çağrısıyla tahakkuk üretir.

Bu zincirin 6–10. adımları yeni akışta onay sınırına taşınacaktır.

### 4.4 Mevcut belge akışı

`LeaseController.Create` içinde zorunlu belge kontrolü form gönderiminden önce yapılmaktadır. Sözleşme kaydı servis tarafından oluşturulduktan sonra dosyalar controller içindeki `UploadDocumentsAsync` çağrısıyla yüklenmektedir.

Sonuçları:

- Belge sahipliği için önce bir `Lease.Id` gereklidir.
- Taslağın mevcut `Lease` tablosunda tutulması belge ilişkisini değiştirmeden korur.
- Dosya yükleme hatası nedeniyle belgesiz taslak kalma ihtimali vardır.
- Bu nedenle onay anında zorunlu belgeler veritabanından tekrar doğrulanmalıdır.

### 4.5 Mevcut tarife ilişkisi

`LeaseRateOverride` kayıtları doğrudan `LeaseId` üzerinden sözleşmeye bağlıdır. Taslağın ayrı başvuru tablosunda tutulması halinde onay sırasında tarife verilerinin başka tabloya kopyalanması gerekirdi.

Yeni tasarım bu kopyalamayı yapmayacak; taslak ve aktif sözleşme aynı `Lease.Id` üzerinden yaşamaya devam edecektir.

### 4.6 Mevcut işlem geçmişi

`LeaseActivityLog` şu işlemleri temsil etmektedir:

- Creation
- Extension
- Termination
- TufeIncrease
- KdvUpdate
- ChargeRegeneration

Bu tablo aktif sözleşmenin operasyonel geçmişidir. Başvuru inceleme mesajlarının aynı tabloya eklenmesi iki farklı yaşam döngüsünü karıştıracağından kullanılmayacaktır.

### 4.7 Mevcut iç kullanıcı ekranları

- `Lease/Index` aktif, dolmak üzere, sona eren ve feshedilen sözleşme filtrelerine sahiptir.
- `Lease/Details` yalnızca mevcut üç durumu bilir.
- Bilinmeyen bir durum detay ekranında varsayılan olarak aktifmiş gibi etiketlenebilir.
- `Lease/Create` bütün sözleşme alanlarını, tarife kalemlerini ve belgeleri barındırır.

Taslak ve revizyon kayıtları aktif detay ekranına gönderilmemelidir.

### 4.8 Mevcut kiracı portalı

`TenantLeaseController` ve `LeaseRepository.GetByTenantIdAsync` kiracıya ait sözleşmeleri durumdan bağımsız getirebilir. `ApplicationDbContext` global filtresi yalnızca `TenantId` ve `IsDeleted` kontrolü yapmaktadır.

Yeni durumlar eklendiğinde ek filtre konulmazsa taslak ve revizyon kayıtları kiracı portalına sızabilir. Kiracıya özel repository metotlarında açık durum filtresi zorunludur.

### 4.9 Mevcut yetkiler

`PermissionCatalog.Lease` altında şu izinler bulunmaktadır:

```text
Internal.Lease
Internal.Lease.Create
Internal.Lease.Edit
Internal.Lease.Extend
Internal.Lease.Terminate
Internal.Lease.OverrideRate
```

Onay, revizyon ve taslak silme için ayrı izin bulunmamaktadır.

### 4.10 Mevcut transaction altyapısı

`ITransactionalService` uygulayan servisler `TransactionInterceptor` tarafından DB transaction'ı içinde çalıştırılmaktadır. İç içe transactional servis çağrıları mevcut transaction'a katılmaktadır.

Bu altyapı, onay sırasında:

- durum güncellemesi,
- inceleme geçmişi,
- sözleşme işlem geçmişi,
- tahakkuklar,
- tahakkuk kalemleri

işlemlerini aynı transaction içinde tutmak için kullanılacaktır.

### 4.11 Mevcut eşzamanlılık boşluğu

`Lease` üzerinde uygulama seviyesinde bir row version/concurrency token bulunmamaktadır. Aynı başvuruya eşzamanlı karar verilmesini engelleyecek koruma yeni eklenmelidir.

---

## 5. Değişmez mimari kararlar

Bu bölümdeki kararlar kullanıcı tarafından ayrıca değiştirilmedikçe bütün fazlarda geçerlidir.

### 5.1 Ayrı sözleşme başvurusu tablosu oluşturulmaz

- Taslak başvuru mevcut `Lease` entity'sinde tutulur.
- `Draft` ve `RevisionRequested` kayıtları fiziksel olarak `Sozlesmeler` tablosundadır.
- Bu kayıtlar onaylanana kadar iş anlamında aktif sözleşme sayılmaz.
- Onay sırasında yeni bir `Lease` satırı kopyalanmaz veya oluşturulmaz.
- Aynı satır `Active` durumuna geçirilir.

Gerekçe:

- Belgeler zaten `DocumentOwnerType.Lease + OwnerId` ile `Lease.Id` değerine bağlıdır.
- Tarife override'ları zaten `LeaseId` ile bağlıdır.
- Ayrı başvuru tablosu belge, tarife ve audit verilerinin onay sırasında taşınmasını gerektirir.
- Tek kimlik kullanımı bağlantı kopması ve kopyalama hatası riskini azaltır.

### 5.2 Başvuru geçmişi ayrı tabloda tutulur

- `LeaseActivityLog` yalnızca aktif sözleşmenin operasyonel geçmişi olarak kalır.
- Başvuru oluşturma, güncelleme, revizyon, yeniden gönderim, onay, mesaj ve silme olayları yeni `LeaseReviewHistory` tablosunda tutulur.
- Her olay ayrı satırdır.
- Geçmiş satırları iş akışı üzerinden güncellenmez veya silinmez.

### 5.3 Onay operasyonel sınırdır

`Lease.Status = Active` yapılmadan önce:

- sözleşme oluşturulmuş sayılmaz,
- kiracıya gösterilmez,
- birimi kiralanmış göstermez,
- sözleşme tahakkuku üretilemez,
- uzatma/fesih/vade güncelleme/yeniden üretim yapılamaz.

### 5.4 Taslak aşamasında tahakkuk üretilemez

- Taslak oluşturma metodu `GenerateForLeaseAsync` çağırmaz.
- Taslak güncelleme metodu tahakkuk üretmez.
- Revizyon isteme ve yeniden gönderme tahakkuk üretmez.
- `ChargeGenerationService.GenerateForLeaseAsync` aktif olmayan sözleşmeyi guard ile reddeder.
- Onay dışında sözleşmeyi aktif hale getiren ikinci bir kod yolu oluşturulmaz.

### 5.5 Kiracı portalı taslakları göstermez

Kiracı portalı yalnızca şu durumları görebilir:

```text
Active
Ended
Terminated
```

`Draft` ve `RevisionRequested` hem listeden hem detay endpoint'inden gizlenir. Başka kiracıya ait kayıttan ayrıştırılamayacak şekilde `NotFound` davranışı tercih edilir.

### 5.6 Silme fiziksel değildir

- Yalnızca `Draft` veya `RevisionRequested` başvurular silinebilir.
- Silme açıklaması zorunludur.
- Önce `LeaseReviewActionType.Deleted` geçmiş satırı eklenir.
- Başvuru `IsDeleted = true` yapılarak normal sorgulardan çıkarılır.
- Bağlı taslak tarifeleri ve belgeler kontrollü biçimde soft-delete edilir.
- İnceleme geçmişi ve genel audit kaydı korunur.
- `Active`, `Ended` veya `Terminated` sözleşmeler bu endpoint ile silinemez.
- Aktif sözleşme yaşam döngüsü mevcut fesih işlemiyle yönetilir.

### 5.7 Kendi başvurusunu onaylama yasaktır

- `Lease.CreatedBy == currentUserId` ise aynı kullanıcı başvuruyu onaylayamaz.
- Aynı kullanıcı gerekli izne sahip olsa bile dört göz kuralı uygulanır.
- Kendi başvurusuna revizyon isteme ve değerlendirme amaçlı silme de varsayılan olarak engellenir.
- Bu kural değişecekse §25 karar kaydı güncellenmelidir.

### 5.8 Bir birim için tek açık başvuru vardır

Bir birimde aynı anda en fazla bir adet silinmemiş açık başvuru bulunabilir:

```text
Status IN (Draft, RevisionRequested)
AND IsDeleted = false
```

- Bu kural DB filtered unique index ile korunur.
- Taslak birimi `OccupancyStatus.Leased` yapmaz.
- Ancak başka bir yeni sözleşme başvurusunda seçilemez.
- Taslak düzenleme ekranı kendi mevcut birimini seçili olarak gösterebilir.
- Onay sırasında aktif sözleşme çakışması ayrıca yeniden kontrol edilir.

### 5.9 Yetki kapsamı bütün kararlarda yeniden kontrol edilir

- Başvuruyu listede görebilmek karar vermeye tek başına yeterli değildir.
- Approve, RequestRevision ve DeleteDraft servisleri birim/taşınmaz kapsamını tekrar doğrular.
- Controller'dan gelen kapsam bilgisine göre repository sorgusu daraltılır ve servis guard'ı çalışır.
- İstemciden aktör kullanıcı kimliği veya yetki bilgisi kabul edilmez.

### 5.10 Controller iş kuralı taşımaz

- Controller doğrudan `ApplicationDbContext` kullanmaz.
- Controller durum geçişi yapmaz.
- Controller tahakkuk üretmez.
- Controller yalnızca model binding, HTTP yetkisi, dosya alma, servis çağrısı ve yönlendirme yapar.
- Durum geçişleri ve iş invariantları servis katmanında uygulanır.

### 5.11 İnceleme geçmişi audit'in yerine geçmez

- `LeaseReviewHistory` kullanıcıya gösterilecek iş geçmişidir.
- `AuditSaveChangesInterceptor` teknik veri değişikliği audit'ini üretmeye devam eder.
- Birinin bulunması diğerinin kaldırılmasına gerekçe değildir.

### 5.12 Mevcut tahakkuk üretim algoritması kopyalanmaz

- Onay metodu mevcut `ChargeGenerationService.GenerateForLeaseAsync` metodunu kullanır.
- Ayrı bir “onay tahakkuk üreticisi” yazılmaz.
- Tarife çözümleme ve dönem oluşturma kuralları tek kaynaktan çalışır.
- Onay refactor'ı mevcut tahakkuk hesaplama sonuçlarını değiştirmez.

---

## 6. Hedef durum modeli

### 6.1 `LeaseStatus` genişletmesi

Hedef enum:

```csharp
public enum LeaseStatus
{
    Active = 1,
    Ended = 2,
    Terminated = 3,
    Draft = 4,
    RevisionRequested = 5
}
```

Kurallar:

- Mevcut `1`, `2`, `3` değerleri değişmez.
- Eski sözleşmelerin durumu migration ile dönüştürülmez.
- Yeni başvuru servisi `Draft` değerini açıkça atar.
- Entity üzerindeki mevcut `Active` varsayılanı, eski seed/test davranışlarını yanlışlıkla değiştirmemek için Faz 1'de test etkisi görülmeden değiştirilmez.
- Üretim kodunda yeni sözleşme kaydı yalnızca `CreateDraftAsync` üzerinden oluşturulur.

### 6.2 Kullanıcıya gösterilecek durum etiketleri

| Enum | İç kullanıcı etiketi | Kiracı portalı | Operasyonel anlam |
|---|---|---|---|
| `Draft` | Taslak — Onay Bekliyor | Gizli | Başvuru incelemeye hazır |
| `RevisionRequested` | Revizyon İstendi | Gizli | Başvuru sahibi değişiklik yapmalı |
| `Active` | Aktif | Görünür | Sözleşme aktif, tahakkuk üretilebilir |
| `Ended` | Sona Erdi | Görünür | Mevcut sona erme davranışı |
| `Terminated` | Feshedildi | Görünür | Mevcut fesih davranışı |

### 6.3 İnceleme aksiyonları

```csharp
public enum LeaseReviewActionType
{
    DraftCreated = 1,
    DraftUpdated = 2,
    RevisionRequested = 3,
    Resubmitted = 4,
    Approved = 5,
    Deleted = 6
}
```

Serbest `Comment` aksiyonu ilk sürüm kapsamına dahil değildir ve enum'a eklenmez.
İleride bu ihtiyaç oluşursa ayrı bir gereksinim olarak eklenir; kullanılmayan enum değeri
önceden rezerve edilmez.

### 6.4 Durum geçiş tablosu

| Başlangıç | Aksiyon | Hedef | Aktör | Açıklama | Sonuç |
|---|---|---|---|---|---|
| Kayıt yok | Başvuru oluştur | `Draft` | `Lease.Create` sahibi | Sözleşme açıklaması opsiyonel | Taslak, tarife ve belgeler kaydedilir; tahakkuk yok |
| `Draft` | Taslak güncelle | `Draft` | Başvuru sahibi | İnceleme açıklaması gerekmez | Alanlar güncellenir, `DraftUpdated` eklenir |
| `Draft` | Revizyon iste | `RevisionRequested` | Yetkili değerlendirici | Zorunlu | Mesaj geçmişe eklenir; tahakkuk yok |
| `RevisionRequested` | Revizyonu kaydet ve gönder | `Draft` | Başvuru sahibi | Kullanıcı notu opsiyonel | `Resubmitted` eklenir; tekrar onay bekler |
| `Draft` | Onayla | `Active` | Yetkili değerlendirici | Opsiyonel | Sözleşme aktifleştirilir ve tahakkuklar üretilir |
| `Draft` | Sil | Soft-delete | Yetkili değerlendirici | Zorunlu | Başvuru normal sorgulardan çıkar |
| `RevisionRequested` | Sil | Soft-delete | Yetkili değerlendirici | Zorunlu | Başvuru normal sorgulardan çıkar |

### 6.5 Geçersiz durum geçişleri

Aşağıdaki işlemler servis seviyesinde reddedilir:

- `RevisionRequested` kaydı doğrudan onaylamak.
- `Active`, `Ended` veya `Terminated` kayda revizyon istemek.
- `Active`, `Ended` veya `Terminated` kaydı başvuru silme endpoint'iyle silmek.
- `Draft` olmayan kaydı taslak güncelleme endpoint'iyle değiştirmek; `RevisionRequested` yalnızca yeniden gönderim akışıyla değiştirilebilir.
- `Active` olmayan kaydı uzatmak.
- `Active` olmayan kaydı feshetmek.
- `Active` olmayan kaydın vade kuralını aktif sözleşme endpoint'iyle güncellemek.
- `Active` olmayan kayıt için tahakkuk üretmek veya yeniden üretmek.
- Soft-delete edilmiş kayıt için herhangi bir karar vermek.
- Row version değeri eskimiş başvuruyu güncellemek veya karara bağlamak.
- Normal başvuru sahibinin kendi başvurusunu değerlendirmesi; yalnız `IsSuperAdmin=true` claim'i bu kuralın operasyon istisnasıdır.

### 6.6 Revizyon döngüsü

Bir sözleşme başvurusu aşağıdaki döngüyü birden fazla kez yaşayabilir:

```text
Draft
  → RevisionRequested (1. mesaj)
  → Draft             (1. yeniden gönderim)
  → RevisionRequested (2. mesaj)
  → Draft             (2. yeniden gönderim)
  → Active
```

Her ok için ayrı `LeaseReviewHistory` satırı bulunur. Önceki revizyon açıklaması yeni açıklamayla ezilmez.

---

## 7. Hedef veri modeli

### 7.1 `Lease` değişiklikleri

Yeni alan:

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = [];
```

EF yapılandırması:

- SQL Server `rowversion`/concurrency token olarak tanımlanır.
- Formlarda Base64 veya uygun model binding formatında hidden alan olarak taşınır.
- İstemci değeri yalnızca concurrency karşılaştırması için kullanılır.
- Kullanıcıdan status, createdBy veya karar veren kullanıcı değeri alınmaz.

Yeni navigation:

```csharp
public List<LeaseReviewHistory> ReviewHistory { get; set; } = [];
```

### 7.2 Yeni entity: `LeaseReviewHistory`

Önerilen fiziksel tablo adı:

```text
SozlesmeIncelemeGecmisleri
```

Önerilen alanlar:

| Alan | DB alanı | Tür | Zorunlu | Açıklama |
|---|---|---|---|---|
| `Id` | `Id` | `int` | Evet | Primary key |
| `LeaseId` | `SozlesmeId` | `int` | Evet | İlgili başvuru/sözleşme |
| `ActionType` | `IslemTipi` | `int` | Evet | `LeaseReviewActionType` |
| `FromStatus` | `OncekiDurum` | `int?` | Hayır | Olay öncesi durum |
| `ToStatus` | `YeniDurum` | `int?` | Hayır | Olay sonrası durum |
| `Explanation` | `Aciklama` | `nvarchar(1000)` | Koşullu | Revizyon ve silmede zorunlu |
| `ActorUserId` | `IslemYapanKullaniciId` | `nvarchar(450)` | Evet | Identity kullanıcı FK |
| `ActionDate` | `IslemTarihi` | `datetime2` | Evet | Olay zamanı |
| Audit alanları | Mevcut convention | Çeşitli | Evet/koşullu | `BaseEntity` alanları |

Entity mevcut repository ve audit convention'larıyla uyum için `BaseEntity` türevi olabilir. Ancak:

- geçmiş satırı business servisleriyle güncellenmez,
- geçmiş satırı business servisleriyle silinmez,
- `IsDeleted` ve `IsActive` alanları tarihçeyi temizlemek için kullanılmaz.

### 7.3 İlişkiler

`LeaseReviewHistory → Lease`:

- Çoktan bire ilişki.
- FK zorunlu.
- `OnDelete(DeleteBehavior.Restrict)` veya `NoAction`.
- Lease soft-delete edildiğinde geçmiş fiziksel olarak kalır.

`LeaseReviewHistory → ApplicationUser`:

- `ActorUserId` FK.
- `OnDelete(DeleteBehavior.Restrict)`.
- Kullanıcı soft-delete edilse bile geçmiş ilişkisi korunur.

### 7.4 Index'ler

Zorunlu index'ler:

```text
IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi
IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId
```

Açık başvuru unique filtered index'i:

```text
UX_Sozlesmeler_BirimId_AcikBasvuru
```

Önerilen filtre:

```sql
[IsDeleted] = 0 AND [Durum] IN (4, 5)
```

Index:

```text
UnitId — unique
```

Migration üretilirken SQL Server filtered index ifadesinin gerçekten desteklenen biçimde çıktığı migration SQL'i üzerinden doğrulanmalıdır.

### 7.5 Onay bilgisi nerede tutulur

İlk sürümde `Lease` üzerine ayrıca `ApprovedByUserId` ve `ApprovalDate` eklenmeyecektir.

Kaynak:

- Onaylayan kullanıcı: son `LeaseReviewActionType.Approved` satırı.
- Onay tarihi: aynı satırın `ActionDate` alanı.

Performans ölçümünde ihtiyaç oluşursa denormalize alan eklenmesi ayrı karar olarak ele alınır. Aynı bilginin iki yerde tutarsız kalması ilk sürümde önlenir.

### 7.6 Açıklama kuralları

- Revizyon açıklaması trim sonrası 1–1000 karakter olmalıdır.
- Silme açıklaması trim sonrası 1–1000 karakter olmalıdır.
- Onay açıklaması opsiyonelse trim sonrası en fazla 1000 karakterdir.
- Boşluklardan oluşan açıklama geçersizdir.
- HTML kabul edilmez; UI çıktısı Razor encoding ile gösterilir.
- Açıklama sonradan değiştirilmez.

### 7.7 Tarih ve saat kararı

- Yeni inceleme olayları servis içinde `DateTime.UtcNow` ile yazılır.
- UI, mevcut uygulama saat dilimi ve kültür kurallarıyla gösterir.
- İstemciden `ActionDate` alınmaz.
- Migration veya seed dışında geçmiş tarihli olay yazılmaz.

---

## 8. Domain invariantları

### 8.1 Taslak oluşturma invariantları

Taslak oluşturulmadan önce:

- `UnitId` geçerli olmalıdır.
- Birim yetki kapsamında olmalıdır.
- Birim aktif ve kiralanabilir türde olmalıdır.
- `TenantId` geçerli olmalıdır.
- Kiracı yetki kapsamında olmalıdır.
- `EndDate > StartDate` olmalıdır.
- `DueDay` 1–31 arasında olmalıdır.
- `DueDateRuleType` desteklenen değerlerden biri olmalıdır.
- Tarife kalemleri mevcut validasyonlardan geçmelidir.
- Birimde mevcut iş kuralına göre devam eden aktif sözleşme bulunmamalıdır.
- Birimde başka `Draft` veya `RevisionRequested` başvuru bulunmamalıdır.
- Zorunlu belge alanları form seviyesinde kontrol edilmelidir.

Başarılı oluşturma sonunda:

- `Lease.Status == Draft`.
- `LeaseRateOverride` kayıtları taslağa bağlıdır.
- `LeaseReviewHistory` içinde bir `DraftCreated` satırı vardır.
- `LeaseActivityLog` içinde `Creation` satırı yoktur.
- `Charge` satırı yoktur.

### 8.2 Taslak güncelleme invariantları

- Kayıt `Draft` olmalıdır.
- Kayıt soft-delete edilmemiş olmalıdır.
- Kullanıcı başvuru sahibi olmalıdır.
- Kullanıcı `Lease.Create` iznine sahip olmalıdır.
- Kullanıcı yeni seçilen birim/taşınmaz kapsamında olmalıdır.
- Row version güncel olmalıdır.
- Bütün oluşturma validasyonları tekrar çalışmalıdır.
- Birim değişiyorsa açık başvuru unique kuralı tekrar uygulanmalıdır.
- Başarılı güncellemede status `Draft` kalır.
- `DraftUpdated` geçmiş satırı eklenir.
- Tahakkuk üretilmez.

### 8.3 Revizyon isteme invariantları

- Kayıt `Draft` olmalıdır.
- Kullanıcı `Lease.RequestRevision` iznine sahip olmalıdır.
- Kullanıcı başvuru sahibi olmamalıdır.
- Kayıt kullanıcının yetki kapsamında olmalıdır.
- Açıklama zorunlu ve en fazla 1000 karakter olmalıdır.
- Row version güncel olmalıdır.
- Durum `RevisionRequested` yapılır.
- `RevisionRequested` geçmiş satırı eklenir.
- Sözleşme alanları değerlendirme işlemi tarafından değiştirilmez.
- Tahakkuk üretilmez.

### 8.4 Revizyonu yeniden gönderme invariantları

- Kayıt `RevisionRequested` olmalıdır.
- Kullanıcı başvuru sahibi olmalıdır.
- Kullanıcı `Lease.Create` iznine sahip olmalıdır.
- Row version güncel olmalıdır.
- Bütün oluşturma ve tarife validasyonları tekrar çalışmalıdır.
- Zorunlu belgeler form ve DB seviyesinde doğrulanmalıdır.
- Durum `Draft` yapılır.
- `Resubmitted` geçmiş satırı eklenir.
- Eski revizyon mesajları korunur.
- Tahakkuk üretilmez.

### 8.5 Onay invariantları

Onaydan hemen önce, form gönderiminde daha önce yapılmış olmasına bakılmaksızın aşağıdakiler tekrar doğrulanır:

- Kayıt `Draft` olmalıdır.
- Kayıt soft-delete edilmemiş olmalıdır.
- Kullanıcı `Lease.Approve` iznine sahip olmalıdır.
- Kullanıcı başvuru sahibi olmamalıdır.
- Birim ve taşınmaz karar veren kullanıcının kapsamında olmalıdır.
- Row version güncel olmalıdır.
- Birim hâlâ mevcut ve kiralanabilir olmalıdır.
- Kiracı hâlâ mevcut olmalıdır.
- Başlangıç/bitiş ve vade alanları geçerli olmalıdır.
- Birimde aktif sözleşme çakışması bulunmamalıdır.
- Tarife override'ları geçerli olmalıdır.
- Zorunlu belgelerin tamamı kalıcı olarak yüklenmiş olmalıdır.
- Bu başvuruya ait sözleşme kaynaklı tahakkuk bulunmamalıdır.
- `LeaseActivityType.Creation` kaydı daha önce oluşmamış olmalıdır.

Başarılı onay transaction'ı:

1. `Lease.Status = Active` yapar.
2. `Approved` inceleme geçmişi ekler.
3. Güncel tarife kalemlerinden KDV ve aylık tutar özetini hesaplar.
4. `LeaseActivityType.Creation` kaydı ekler.
5. Değişiklikleri kaydeder.
6. Mevcut `GenerateForLeaseAsync` metodunu çağırır.
7. Bütün tahakkukları ve kalemleri aynı transaction içinde kaydeder.

Herhangi bir adım başarısızsa:

- transaction rollback olur,
- kayıt `Draft` olarak kalır,
- `Approved` geçmiş satırı kalmaz,
- kısmi tahakkuk kalmaz.

### 8.6 Başvuru silme invariantları

- Kayıt yalnızca `Draft` veya `RevisionRequested` olabilir.
- Kullanıcı `Lease.DeleteDraft` iznine sahip olmalıdır.
- Kullanıcı başvuru sahibi olmamalıdır.
- Kullanıcı kapsam kontrolünden geçmelidir.
- Açıklama zorunlu ve en fazla 1000 karakter olmalıdır.
- Row version güncel olmalıdır.
- Kayıtta tahakkuk olmamalıdır.
- `Deleted` geçmiş satırı soft-delete işleminden önce eklenmelidir.
- Lease, taslak tarifeleri ve belgeler soft-delete edilir.
- İnceleme geçmişi silinmez.

### 8.7 Aktif sözleşme işlemleri

Aşağıdaki mevcut servis metotları açıkça `LeaseStatus.Active` guard'ı taşımalıdır:

- Extend
- Terminate
- UpdateDueDate
- Regenerate
- GenerateForLease

Mevcut `Terminated` kontrolleri tek başına yeterli değildir; yeni `Draft` ve `RevisionRequested` durumları da aktif işlem endpoint'lerine karşı korunmalıdır.

### 8.8 Belge invariantları

- Taslak oluşturma ekranı mevcut zorunlu belge kuralını korur.
- Dosya yükleme hatası taslağı aktif sözleşmeye dönüştürmez.
- Onay metodu, request içindeki dosyalara değil kalıcı `Document` kayıtlarına bakar.
- Revizyon sırasında mevcut belge listesi gösterilir.
- Belge değiştiriliyorsa mevcut `DocumentService` invalidation/replacement davranışı kullanılmalıdır.
- Silinen taslağın belgeleri normal belge sorgularında görünmemelidir.
- Belge içeriği fiziksel olarak silinecekse bunun güvenlik ve geri kazanım etkisi ayrıca onaylanmadan yapılmaz; ilk sürüm soft-delete kullanır.

---

## 9. Hedef servis ve repository sınırları

### 9.1 `ILeaseService` hedef metotları

Önerilen komutlar:

```csharp
Task<LeaseDraftResultDto> CreateDraftAsync(CreateLeaseDraftInput input);
Task<LeaseDraftEditDto> GetDraftForEditAsync(GetLeaseDraftInput input);
Task UpdateDraftAsync(UpdateLeaseDraftInput input);
Task RequestRevisionAsync(RequestLeaseRevisionInput input);
Task ResubmitRevisionAsync(ResubmitLeaseRevisionInput input);
Task ApproveAsync(ApproveLeaseInput input);
Task DeleteDraftAsync(DeleteLeaseDraftInput input);
Task<IReadOnlyList<LeaseReviewHistoryDto>> GetReviewHistoryAsync(GetLeaseReviewHistoryInput input);
```

Notlar:

- Mevcut `CreateAsync` semantik belirsizlik bırakmamak için `CreateDraftAsync` olarak değiştirilir.
- Controller action adı `Create` kalabilir; servis adı iş davranışını açıkça söyler.
- Onay içinde çalışan aktifleşme bölümü private bir metoda ayrılabilir ancak başka public endpoint'ten çağrılmaz.
- Servis input'ları entity değil primitive/DTO taşır.

### 9.2 Önerilen input alanları

`CreateLeaseDraftInput`:

- Mevcut `CreateLeaseInput` alanları
- Current user id controller'dan güvenilir bağlamla eklenebilir; aktör yine server-side doğrulanır
- Access scope

`UpdateLeaseDraftInput` ve `ResubmitLeaseRevisionInput`:

- `LeaseId`
- Düzenlenebilir sözleşme alanları
- Tarife override girdileri
- `ExpectedRowVersion`
- Access scope

Karar input'ları:

- `LeaseId`
- `Explanation` — aksiyona göre zorunlu/opsiyonel
- `ActorUserId`
- `ExpectedRowVersion`
- Access scope

### 9.3 `ILeaseRepository` hedef sorumlulukları

Mevcut repository aşağıdaki sorguları açıkça desteklemelidir:

- Kapsam uygulanmış başvuru listesi.
- Taslak düzenleme projeksiyonu.
- Karar için tracking açık Lease + Unit sorgusu.
- Birimde başka açık başvuru kontrolü.
- Taslak için tahakkuk/creation geçmişi varlık kontrolü.
- Kiracı portalı için yalnızca görünür durumları döndüren sorgular.
- Soft-delete edilmiş başvuru audit sorgusu gerekiyorsa `IgnoreQueryFilters` kullanan, yalnızca admin/audit amacına özel metot.

Genel bir `GetByIdAsync` çağrısı karar vermek için yeterli kabul edilmez. Karar sorgusu gereken ilişkileri ve kapsam filtresini açıkça taşır.

### 9.4 Yeni repository: `ILeaseReviewHistoryRepository`

Önerilen metotlar:

```csharp
Task AddAsync(LeaseReviewHistory history);
Task<List<LeaseReviewHistoryDto>> GetByLeaseIdAsync(int leaseId);
Task<LeaseReviewHistoryDto?> GetLatestRevisionAsync(int leaseId);
```

Repository:

- geçmiş satırını update etmez,
- geçmiş satırını delete etmez,
- kullanıcı görünen adını `ApplicationUser` navigation üzerinden projekte eder,
- sıralamayı `ActionDate`, eşitlikte `Id` üzerinden deterministik yapar.

### 9.5 Transaction sınırı

Transactional olması gereken public servis metotları:

- CreateDraftAsync
- UpdateDraftAsync
- RequestRevisionAsync
- ResubmitRevisionAsync
- ApproveAsync
- DeleteDraftAsync

Dosya içeriği yükleme controller sonrası ayrı servis çağrısında kalırsa taslak oluşturma ve dosya yükleme tam atomik değildir. Bu kabul edilebilir çünkü:

- kayıt yalnızca taslaktır,
- onay zorunlu belgeyi DB'den yeniden kontrol eder,
- eksik belgeyle aktifleşme mümkün değildir.

Dosya yüklemeyi aynı DB transaction'a alma girişimi büyük binary işlem süresini artırmamalıdır. Mevcut `DocumentService` sınırı korunur.

---

## 10. Controller ve route tasarımı

### 10.1 Hedef action'lar

| HTTP | Route/action | Yetki | Davranış |
|---|---|---|---|
| GET | `Lease/Create` | `Lease.Create` | Boş başvuru formu |
| POST | `Lease/Create` | `Lease.Create` | `Draft` oluşturur, taslak ekranına yönlendirir |
| GET | `Lease/Draft/{id}` | Modül + sahip/değerlendirici | Ortak form ve inceleme geçmişi |
| POST | `Lease/UpdateDraft/{id}` | `Lease.Create` + sahiplik | `Draft` bilgilerini günceller |
| POST | `Lease/ResubmitRevision/{id}` | `Lease.Create` + sahiplik | Revizyonu günceller, `Draft` durumuna döndürür |
| POST | `Lease/Approve/{id}` | `Lease.Approve` | Aktifleştirir ve tahakkuk üretir |
| POST | `Lease/RequestRevision/{id}` | `Lease.RequestRevision` | Açıklamayla revizyona gönderir |
| POST | `Lease/DeleteDraft/{id}` | `Lease.DeleteDraft` | Açıklamayla soft-delete eder |

Route adları uygulama aşamasında mevcut naming convention ile uyumlu tutulmalıdır. Hashids model binder kullanılan route parametreleri düz integer gibi elle parse edilmemelidir.

### 10.2 HTTP güvenlik kuralları

- Bütün mutation action'ları `[HttpPost]` olmalıdır.
- Bütün mutation action'ları anti-forgery doğrulaması taşımalıdır.
- GET action'ı durum değiştirmez.
- Geçersiz model business servise gönderilmez.
- Yetki attribute'u controller'da; kapsam, sahiplik ve durum kontrolü serviste tekrar uygulanır.
- Redirect hedefi status'a göre taslak ekranı veya aktif detay ekranıdır.

### 10.3 `Details` davranışı

`LeaseController.Details` bir `Draft` veya `RevisionRequested` id alırsa aktif sözleşme detayını render etmemelidir.

Önerilen davranış:

- Kullanıcı başvuruyu görmeye yetkiliyse `Draft` action'ına yönlendir.
- Yetkili değilse `Forbid` veya mevcut güvenlik convention'ına göre `NotFound`.
- Soft-delete kayıt `NotFound`.

### 10.4 Hata geri bildirimi

- Form alanına bağlı business validation, ilgili alanın `ModelState` hatası olarak gösterilir.
- Karar modalı validation hataları kullanıcıya merkezi feedback ile gösterilir.
- Concurrency hatası: “Bu başvuru başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyip tekrar deneyin.”
- Onay sırasında aktif birim çakışması: taslak korunur ve açık hata gösterilir.
- Tahakkuk üretim hatası başarı mesajına dönüştürülmez.

---

## 11. Yetki modeli

### 11.1 Yeni izinler

`PermissionCatalog.Lease` altına:

```text
Internal.Lease.Approve
Internal.Lease.RequestRevision
Internal.Lease.DeleteDraft
```

eklenir.

Önerilen kullanıcı etiketleri:

| Permission | Etiket |
|---|---|
| `Approve` | Onayla |
| `RequestRevision` | Revizyon İste |
| `DeleteDraft` | Başvuru Sil |

### 11.2 Mevcut izinlerin anlamı

| Permission | Yeni akıştaki anlamı |
|---|---|
| `Lease.Module` | Sözleşme ve kapsamındaki başvuru listesini görme |
| `Lease.Create` | Yeni başvuru oluşturma; kendi taslak/revizyon kaydını düzenleme ve gönderme |
| `Lease.Edit` | Aktif sözleşmenin mevcut düzenleme işlemleri; başvuru düzenleme hakkı vermez |
| `Lease.Extend` | Yalnız aktif sözleşme uzatma |
| `Lease.Terminate` | Yalnız aktif sözleşme fesih |
| `Lease.OverrideRate` | Mevcut tarife override kuralı; taslak formunda da mevcut davranışı korur |

### 11.3 Yetki ve sahiplik matrisi

| İşlem | Başvuru sahibi | Başka iç kullanıcı | Değerlendirici |
|---|---:|---:|---:|
| Başvuru görüntüleme | Evet, kapsam içindeyse | Yalnız modül/kapsam politikasına göre | Evet, kapsam içindeyse |
| Taslak düzenleme | `Lease.Create` ile evet | Hayır | Hayır; kendi başvurusu değilse read-only |
| Revizyonu yeniden gönderme | `Lease.Create` ile evet | Hayır | Hayır |
| Onaylama | Hayır | Hayır | `Lease.Approve` ile evet |
| Revizyon isteme | Hayır | Hayır | `Lease.RequestRevision` ile evet |
| Başvuru silme | Hayır | Hayır | `Lease.DeleteDraft` ile evet |

`IsSuperAdmin=true` claim'li kullanıcı, gerekli karar permission'larını authorization bypass
üzerinden taşıdığı için kendi başvurusunda onay, revizyon isteme ve başvuru silme işlemlerini
yapabilir. İstisna rol adına veya form verisine değil güvenilir claim'e bağlıdır; gerçek aktör
review history kaydında korunur.

### 11.4 Scope aware listeleri

Yeni izinler aşağıdaki katalog listelerine kontrollü eklenmelidir:

- `PermissionCatalog.ScopeAware`
- `OperasyonMuduruIzinleri` — iş kararı gereği varsayılan olarak eklenir
- `PermissionCatalog.All`
- Sistem yöneticisi claim üretim listeleri

Kiracı rol preset'lerine hiçbir yeni internal izin eklenmez.

### 11.5 Rol ekranı risk işaretleri

Rol oluşturma/düzenleme ekranlarında riskli permission segment listesi yeni aksiyonları doğru vurgulamalıdır:

- `Approve`
- `RequestRevision`
- `DeleteDraft`

`DeleteDraft` mevcut `Delete` risk eşleşmesine otomatik giriyorsa ayrıca string eklenmeden önce UI davranışı doğrulanmalıdır.

---

## 12. UI/UX davranışları

### 12.1 Ortak form mimarisi

`Lease/Create.cshtml` içindeki form alanları ortak bir partial/view component yapısına çıkarılmalıdır.

Önerilen yapı:

```text
Views/Lease/Create.cshtml
Views/Lease/Draft.cshtml
Views/Lease/_LeaseForm.cshtml
Views/Lease/_LeaseReviewTimeline.cshtml
Views/Lease/_LeaseReviewActions.cshtml
```

`_LeaseForm`:

- birim,
- kiracı,
- tarih,
- vade,
- tarife kalemleri,
- belgeler,
- sözleşme açıklaması

alanlarını tek kaynaktan render eder.

### 12.2 Yeni başvuru ekranı

Başlık:

```text
Yeni Sözleşme Başvurusu
```

Ana buton:

```text
Onaya Gönder
```

Yardım metni:

```text
Başvuru onaylanana kadar sözleşme aktifleşmez ve tahakkuk oluşturulmaz.
```

Başarılı POST sonrası kullanıcı aktif sözleşme detayına değil taslak ekranına yönlendirilir.

### 12.3 Taslak banner'ı

Renk: amber/sarı.

Metin:

```text
Bu başvuru onay bekliyor. Onaylanana kadar sözleşme aktifleşmez ve tahakkuk oluşturulmaz.
```

Gösterilecek ek bilgiler:

- Başvuru sahibi
- Oluşturulma tarihi
- Son güncelleme tarihi
- Durum rozeti

Başvuru sahibi formu düzenleyebilir. Değerlendirici aynı alanları read-only görür.

### 12.4 Revizyon banner'ı

Renk: kırmızı/amber.

Metin yapısı:

```text
Bu başvuru için revizyon istendi.
[Son revizyon açıklaması]
İsteyen: [Ad Soyad] — [Tarih/Saat]
```

Başvuru sahibi için ana buton:

```text
Revizyonu Tamamla ve Onaya Gönder
```

Değerlendirici için form read-only kalır. Başvuru tekrar `Draft` durumuna dönmeden onay butonu gösterilmez.

### 12.5 İnceleme geçmişi

Taslak ekranında en yeniden eskiye veya zaman çizelgesi mantığıyla eskiden yeniye tek ve tutarlı sıralama kullanılmalıdır. Önerilen: eskiden yeniye zaman çizelgesi.

Her satır:

- aksiyon etiketi,
- açıklama,
- aktör görünen adı,
- tarih/saat,
- önceki ve yeni durum

bilgilerini gösterebilir.

Açıklaması olmayan `DraftUpdated` gibi teknik olayların UI kalabalığı oluşturması halinde timeline filtresi uygulanabilir; kayıt DB'de korunur.

### 12.6 Değerlendirme aksiyonları

`Draft` durumunda yetkili değerlendirici:

- Onayla
- Revizyon İste
- Başvuruyu Sil

butonlarını görür.

`RevisionRequested` durumunda:

- Onayla görünmez.
- Revizyon İste görünmez.
- Başvuruyu Sil, yetkisi varsa görünür.

`Active` durumunda bu üç butonun hiçbiri görünmez.

### 12.7 Revizyon modalı

Alanlar:

- Açıklama textarea
- Karakter sayacı: `0 / 1000`
- Vazgeç
- Revizyon İste

Kurallar:

- HTML `required` yalnız yardımcıdır; server validation zorunludur.
- Boşluk-only giriş kabul edilmez.
- Modal açıldığında açıklama alanı focus alır.
- Submit sırasında buton çift tıklamaya karşı disable edilir.

### 12.8 Silme modalı

Silme yüksek riskli aksiyon olarak sunulur.

Metin:

```text
Bu başvuru normal listelerden kaldırılacaktır. İşlem açıklaması ve inceleme geçmişi korunacaktır.
```

Alanlar:

- Silme nedeni textarea — zorunlu
- Vazgeç
- Başvuruyu Sil

Genel amaçlı yalnız “Emin misiniz?” confirm dialog'u açıklama alamadığı için tek başına yeterli değildir.

### 12.9 Sözleşme listesi

Filtreler:

```text
Tümü
Onay Bekleyen
Revizyon İstenen
Aktif
Dolmak Üzere
Sona Erdi
Feshedildi
```

Liste satırı:

- `Draft` → amber “Onay Bekliyor” rozeti
- `RevisionRequested` → kırmızı/amber “Revizyon İstendi” rozeti
- Aktif ve tarihsel durumlar mevcut davranışı korur

Taslak/revizyon satırı `Lease/Draft/{id}` ekranına; diğer durumlar `Lease/Details/{id}` ekranına gider.

### 12.10 Kiracı portalı

- Taslak ve revizyon liste sayısına dahil edilmez.
- Taslak id doğrudan URL ile çağrılırsa `NotFound` döner.
- Taslağa bağlı belgeler kiracı belge sorgusundan erişilemez.
- Taslak için tahakkuk zaten bulunmamalıdır.
- Aktifleştirmeden sonra aynı `Lease.Id` normal sözleşme olarak görünür.

### 12.11 Belge görünümü

Taslak/revizyon ekranı:

- mevcut yüklenmiş belgeleri listeler,
- zorunlu belge eksikse üst banner veya alan seviyesinde gösterir,
- revizyon sırasında belge değiştirmeye izin verir,
- değerlendiriciye dosya görüntüleme erişimi verir ancak form alanlarını değiştirmez.

---

## 13. Sorgu ve ekran izolasyonu

### 13.1 `LeaseRepository.GetListAsync`

Yeni filtre eşlemeleri açık olmalıdır:

```text
onaybekliyor → Status == Draft
revizyon     → Status == RevisionRequested
aktif        → mevcut aktif + tarih kuralı
surek        → mevcut dolmak üzere kuralı
gecmis       → Status == Ended
feshedildi   → Status == Terminated
tum          → soft-delete olmayan bütün internal kayıtlar
```

`Tümü` içinde taslakların görünmesi bilinçli internal davranıştır.

### 13.2 Kiracıya özel sorgular

Internal ve tenant kullanımı aynı belirsiz repository metodunu paylaşmamalıdır.

Öneri:

- Internal: mevcut `GetByTenantIdAsync` gerekirse bütün durumları döndürebilir.
- Tenant portal: yeni ve açık isimli `GetTenantPortalListAsync` yalnız görünür durumları döndürür.
- Tenant detail: status filtresi SQL sorgusuna dahil edilir.

Global query filter'a `Status` şartı eklenmez; internal kullanıcıların taslakları görmesi gerekir.

### 13.3 Aktif sözleşme dropdown'ları

Şu sorgular yalnız `Active` döndürmeye devam eder:

- Manuel borç sözleşme seçimi
- Aktif sözleşme birimleri
- Ödeme/tahakkuk bağlantılı dropdown'lar
- Kiracı aktif sözleşme özetleri

`Draft` ve `RevisionRequested` bu sorgulara eklenmez.

### 13.4 Birim uygunluk sorgusu

Yeni başvuru formunda birim aşağıdaki durumlarda seçilemez:

- Mevcut aktif sözleşme kontrolüne takılıyorsa.
- Başka açık `Draft` veya `RevisionRequested` başvuruya sahipse.

Taslak düzenleme formu mevcut seçili birimi seçenek listesine eklemelidir; genel uygunluk sorgusunun onu dışlaması mevcut form değerini kaybettirmemelidir.

### 13.5 Birim doluluk durumu

`OccupancyStatus` yalnız aktif ve tarih aralığında devam eden sözleşmelerden hesaplanır.

- Taslak, birimi `Leased` yapmaz.
- Revizyon, birimi `Leased` yapmaz.
- Birim listesinde istenirse ayrı “Başvuru bekliyor” bilgi rozeti gösterilebilir; bu ilk sürüm kabul kriteri değildir.

### 13.6 İstatistik ve sözleşme detay hesapları

- Draft ekranı `LeaseDetailsViewModel` ve aktif sözleşme istatistiklerini kullanmaz.
- Taslak için tahakkuk sağlığı hesaplanmaz.
- Taslak için uzatma/fesih/regenerate varsayılanları hazırlanmaz.
- Taslak aylık tutarı gerekiyorsa formdaki tarife önizlemesinden gösterilir.

### 13.7 Document owner context

Kiracı tarafından belge erişimi öncesinde ilgili Lease'in tenant portalına görünür durumda olduğu doğrulanmalıdır. Yalnız `TenantId` eşleşmesi taslak belge erişimi için yeterli değildir.

---

## 14. Eşzamanlılık ve idempotency

### 14.1 Row version

Taslak görüntülendiğinde `RowVersion` view model'e taşınır. Her mutation:

- istemcinin beklediği row version değerini entity original value olarak ayarlar,
- `DbUpdateConcurrencyException` yakalanır,
- kullanıcıya sayfayı yenilemesi söylenir,
- işlem otomatik olarak başka duruma uygulanmaz.

### 14.2 Çift onay

Aynı başvuru iki sekmede onaylanırsa:

- ilk transaction `Draft → Active` geçişini tamamlar,
- ikinci işlem row version veya status guard'ında başarısız olur,
- ikinci tahakkuk seti oluşmaz.

Mevcut tahakkuk unique index'i ek savunma sağlar ancak business idempotency'nin tek dayanağı değildir.

### 14.3 Revizyon–onay yarışı

Aynı taslağa aynı anda revizyon ve onay verilirse yalnız bir işlem başarılı olabilir. Diğer işlem concurrency hatası alır. Sonradan gelen işlem yeni duruma otomatik uyarlanmaz.

### 14.4 Taslak güncelleme–onay yarışı

Başvuru sahibi taslağı düzenlerken değerlendirici eski sayfadan onay verirse row version mekanizması eski veriyle onayı engeller. Değerlendirici güncel veriyi yeniden incelemelidir.

### 14.5 Açık başvuru unique index'i

Uygulama seviyesindeki “başka açık başvuru var mı?” kontrolü kullanıcı dostu hata üretir. DB unique filtered index eşzamanlı iki create isteğinde son güvenlik katmanıdır.

### 14.6 Onay transaction'ı

Onay transaction'ı içinde harici servis çağrısı yoktur. Bütün işlem yerel DB işlemleridir. Transaction'ın ortasında HTTP response veya dosya stream işlemi yapılmaz.

---

## 15. Validasyon ve hata sözleşmesi

### 15.1 ViewModel'ler

Önerilen yeni modeller:

```text
LeaseDraftViewModel
RequestLeaseRevisionViewModel
DeleteLeaseDraftViewModel
ApproveLeaseViewModel
```

`LeaseDraftViewModel`, mevcut `CreateLeaseViewModel` alanlarını genişletebilir veya ortak alan modeli içerebilir. Entity doğrudan view'e gönderilmez.

### 15.2 Validator'lar

Zorunlu validator'lar:

- Create/update draft alan validasyonu
- Request revision açıklaması validasyonu
- Delete draft açıklaması validasyonu
- Approve row version/id validasyonu
- Resubmit revision alan validasyonu

Business durumu ve DB varlığı view model validator'a taşınmaz; servis guard'ı olarak kalır.

### 15.3 Önerilen business hata kodları

| Kod | Durum |
|---|---|
| `Lease.DraftNotFound` | Başvuru bulunamadı veya erişilemiyor |
| `Lease.InvalidDraftStatus` | İşlem mevcut durumda yapılamaz |
| `Lease.SelfApprovalForbidden` | Başvuru sahibi kendi kaydını onaylıyor |
| `Lease.SelfReviewForbidden` | Başvuru sahibi değerlendirme aksiyonu deniyor |
| `Lease.RevisionReasonRequired` | Revizyon açıklaması boş |
| `Lease.DeleteReasonRequired` | Silme açıklaması boş |
| `Lease.OpenApplicationExists` | Birimde başka açık başvuru var |
| `Lease.ActiveUnitConflict` | Onay anında aktif sözleşme çakışması var |
| `Lease.RequiredDocumentMissing` | Zorunlu belge kalıcı olarak yok |
| `Lease.DraftHasCharges` | Taslakta beklenmeyen tahakkuk bulundu |
| `Lease.AlreadyActivated` | Başvuru daha önce onaylanmış |
| `Lease.ConcurrencyConflict` | Row version eskimiş |
| `Lease.InactiveChargeGeneration` | Aktif olmayan sözleşmede tahakkuk üretimi istendi |

Kod adları uygulama başlamadan mevcut hata naming convention'ı ile karşılaştırılır. Aynı anlamda mevcut kod varsa yenisi üretilmez.

### 15.4 Kullanıcı mesajları

Mesajlar teknik exception içeriğini göstermemelidir. Örnekler:

- “Bu başvuru başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyip tekrar deneyin.”
- “Seçilen birim için başka bir açık sözleşme başvurusu bulunmaktadır.”
- “Başvuru onaylanamadı; seçilen birimde devam eden aktif bir sözleşme bulunmaktadır.”
- “Başvurunun onaylanabilmesi için zorunlu belgeleri tamamlayın.”
- “Yalnızca onay bekleyen başvurular onaylanabilir.”

---

## 16. Güvenlik ve audit

### 16.1 Server-side aktör bilgisi

- `ActorUserId`, oturumdaki kullanıcıdan alınır.
- Form hidden alanından aktör kabul edilmez.
- `CreatedBy` değerine istemciden müdahale edilemez.
- Başvuru sahipliği DB'deki audit alanından belirlenir.

### 16.2 Kapsam kontrolü

Her karar için:

- global access varsa normal sorgu,
- yoksa `PropertyIds OR UnitIds` kapsamı,
- hem liste hem karar sorgusunda aynı kapsam semantiği

uygulanır.

### 16.3 Açıklama güvenliği

- Açıklama düz metindir.
- Razor output encoding devre dışı bırakılmaz.
- `Html.Raw` ile gösterilmez.
- Maksimum uzunluk hem validator hem DB kolonunda uygulanır.
- Log'a açıklamanın tamamını gereksiz yere kopyalamak yerine entity audit'i kullanılır.

### 16.4 Silinen başvuru audit'i

Soft-delete edilen başvuru normal listede görünmez. Audit ekranı veya ilerideki özel arşiv sorgusu:

- Lease kaydını `IgnoreQueryFilters` ile,
- silme olayını inceleme geçmişinden,
- teknik alan değişikliklerini audit log'dan

okuyabilir.

İlk sürümde kullanıcıya açık “silinen başvurular arşivi” zorunlu değildir.

---

## 17. Migration ve veri geçişi

### 17.1 Migration içeriği

Migration en az aşağıdakileri içermelidir:

1. `Sozlesmeler.RowVersion` eklenmesi.
2. `SozlesmeIncelemeGecmisleri` tablosu.
3. Lease FK ve ApplicationUser FK kuralları.
4. Açıklama kolonunun maksimum uzunluğu.
5. Enum comment güncellemeleri.
6. İnceleme geçmişi index'leri.
7. Birim başına açık başvuru filtered unique index'i.
8. Model snapshot güncellemesi.

### 17.2 Mevcut veriler

- Mevcut `Active`, `Ended`, `Terminated` değerleri aynı kalır.
- Mevcut Lease kayıtları için geçmiş satırı backfill edilmez.
- Eski `LeaseActivityType.Creation` kayıtları onay geçmişine taşınmaz.
- Mevcut belgeler ve tarifeler değiştirilmez.
- Production gerçek veri olmadığı bilgisi güncel repo talimatlarıyla tekrar doğrulanmalıdır; migration yine de veri kaybına yol açmamalıdır.

### 17.3 Seed verisi

- Mevcut aktif sözleşme seed'leri açıkça `Status = Active` kullanmalıdır.
- En az bir `Draft` ve bir `RevisionRequested` örneği yalnız UI/demo ihtiyacı varsa eklenir.
- Demo geçmişi eklenirse aktör kullanıcı FK'leri mevcut seed kullanıcılarına bağlanır.
- Seed tahakkuk üretimi taslakları kesinlikle atlamalıdır.

### 17.4 Migration doğrulaması

- Migration script'i okunur.
- Filtered index SQL'i doğrulanır.
- Yeni FK'lerin hard delete davranışı doğrulanır.
- Mevcut sözleşme ve tahakkuk satırlarının sayısı migration öncesi/sonrası karşılaştırılır.
- Uygulama başlatılmadan `dotnet build` ve ilgili test projesi doğrulanır.

### 17.5 Rollback ilkesi

Migration henüz veri kabul etmeden geri alınabilir. Yeni akışla taslak veri oluşmuşsa rollback:

- yeni status değerlerini eski enum'un okuyamayacağı,
- inceleme geçmişinin kaybolacağı,
- taslak belgelerin sahip kaydıyla birlikte yanlış yorumlanabileceği

için doğrudan uygulanmaz. Önce taslak veriler için kontrollü export/temizleme planı hazırlanır.

---

## 18. Faz 0 — Kararların ve kapsamın sabitlenmesi

### 18.1 Amaç

Uygulama başlamadan iş akışının değişmez kararlarını doğrulamak ve mevcut kodun bu dokümandaki envanterle uyumlu olduğunu teyit etmek.

### 18.2 Görevler

- [x] Bu doküman kullanıcı tarafından kapsam olarak onaylandı.
- [x] Başvuru sahibinin kendi başvurusunu onaylayamayacağı doğrulandı.
- [x] Birim başına tek açık başvuru kararı doğrulandı.
- [x] Taslakların kiracı portalında görünmeyeceği doğrulandı.
- [x] Silmenin soft-delete olduğu doğrulandı.
- [x] `LeaseReviewHistory` tablosunun bütün inceleme olaylarını taşıyacağı doğrulandı.
- [x] Onay açıklamasının opsiyonel olduğu kesinleştirildi.
- [x] Serbest `Comment` aksiyonunun ilk sürüme dahil edilmeyeceği kesinleştirildi.
- [x] Revizyon isteyen kullanıcının aynı zamanda silme iznine sahip olmak zorunda olmadığı doğrulandı.
- [x] Mevcut kod dosyaları ve metot imzaları yeniden tarandı.
- [x] Aktif başka çalışmalarla migration/dosya çakışması kontrol edildi.

### 18.3 Faz 0 doğrulama kaydı

- Mevcut üretim kodunda veya migration'larda Faz 19'a ait kısmi değişiklik bulunmadı.
- Çalışma kopyasındaki `.git` metadata kullanılabilir olmadığı için çakışma kontrolü
  `git status` ile yapılamadı; hedef dosyalar, migration listesi ve Faz 19 sembolleri
  dosya sistemi üzerinden tarandı.
- Onay açıklaması opsiyonel, revizyon ve silme açıklaması zorunlu ve trim sonrası en
  fazla 1000 karakter olarak sabitlendi.
- Serbest `Comment` aksiyonu ilk sürümden çıkarıldı.
- `Approve`, `RequestRevision` ve `DeleteDraft` izinleri bağımsız permission'lardır.
- Faz 1 migration adı `AddLeaseApprovalWorkflow` olarak sabitlendi.
- Faz 1'de `LeaseReviewHistory`, `LeaseReviewActionType`, `LeaseStatus.Draft`,
  `LeaseStatus.RevisionRequested`, `Lease.RowVersion` ve `Lease.ReviewHistory` uygulanır.
- Zorunlu index adları `IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi`,
  `IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId` ve
  `UX_Sozlesmeler_BirimId_AcikBasvuru` olarak sabitlendi.

### 18.4 Faz tamamlanma ölçütleri

- Bütün karar maddeleri `[x]` durumundadır.
- Belirsiz iş kuralı kalmamıştır.
- Kod değişikliği yapılmamıştır.
- Faz 1 için kesin entity, enum ve migration listesi çıkarılmıştır.

### 18.5 Kapsam dışı

- Entity oluşturma
- Migration üretme
- UI değişikliği
- Permission ekleme

---

## 19. Faz 1 — Domain modeli ve migration

### 19.1 Amaç

Yeni durumları, inceleme geçmişini ve concurrency korumasını uygulamanın geri kalanından bağımsız ve doğrulanabilir biçimde oluşturmak.

### 19.2 Ön koşullar

- Faz 0 tamamlandı.
- Migration çakışması yok.
- Mevcut DB provider SQL Server olarak doğrulandı.

### 19.3 Görevler

- [x] `LeaseStatus.Draft = 4` eklendi.
- [x] `LeaseStatus.RevisionRequested = 5` eklendi.
- [x] `LeaseReviewActionType` onaylı değerlerle eklendi.
- [x] `Lease.RowVersion` concurrency token eklendi.
- [x] `Lease.ReviewHistory` navigation eklendi.
- [x] `LeaseReviewHistory` entity oluşturuldu.
- [x] `ApplicationDbContext` DbSet eklendi.
- [x] Enum comment convention uygulandı.
- [x] Açıklama maksimum uzunluğu yapılandırıldı.
- [x] Lease ve ApplicationUser FK'leri yapılandırıldı.
- [x] İnceleme geçmişi index'leri eklendi.
- [x] Açık başvuru filtered unique index'i eklendi.
- [x] Migration üretildi ve dosyası elle incelendi.
- [x] Snapshot doğrulandı.
- [x] Lease test entity oluşturma ve rowversion davranışı gerçek SQL Server üzerinde kontrol edildi.
- [x] Build başarılı oldu.

### 19.4 Testler

- [x] Yeni enum sayısal değerleri sabit.
- [x] Eski enum sayısal değerleri değişmedi.
- [x] Aynı Lease için birden fazla geçmiş satırı eklenebiliyor.
- [x] Geçmiş aktör FK'si çalışıyor.
- [x] Aynı birimde iki açık başvuru DB tarafından reddediliyor.
- [x] Aynı birimde açık başvuru ve başka birimde açık başvuru eklenebiliyor.
- [x] Soft-delete edilmiş açık başvuru yeni başvuruyu engellemiyor.
- [x] RowVersion update sonrasında değişiyor.

### 19.5 Tamamlanma ölçütleri

- [x] Migration, Faz 1 nesneleri bulunmayan `InitialCreate` baseline şemasına uygulanabiliyor.
- [x] Migration mevcut `KiraTakipDb_Test` veritabanına uygulanabiliyor.
- [x] Migration geri alma ve yeniden uygulama senaryosu `KiraTakipDb_Test` üzerinde doğrulandı.
- [x] Entity model testleri başarılı.
- [x] Henüz controller/UI davranışı değiştirilmedi.

### 19.6 Doğrulama sonucu — 2026-08-07

Kullanıcı onayıyla `AddLeaseApprovalWorkflow` mevcut `KiraTakipDb_Test` veritabanına
uygulandı. Veritabanı daha sonra `InitialCreate` seviyesine başarıyla geri alındı ve yeni
migration yeniden uygulandı. Her iki uygulamadan sonra model ve SQL Server entegrasyonlarını
kapsayan hedefli `LeaseApproval` testleri 5/5 başarılıdır. Kullanıcı onayıyla çalıştırılan
tam regresyon paketi de 141/141 başarılıdır. Faz 1 tamamlanma kapısı geçilmiş ve Faz 2
uygulamaya hazırdır.

---

## 20. Faz 2 — Repository ve sorgu izolasyonu

### 20.1 Amaç

Taslakların internal kullanıcılar tarafından görülebilmesini, kiracı portalı ve aktif sözleşme sorgularından ise kesin olarak ayrılmasını sağlamak.

### 20.2 Görevler

- [x] `ILeaseReviewHistoryRepository` oluşturuldu.
- [x] `LeaseReviewHistoryRepository` oluşturuldu.
- [x] Repository DI kaydı eklendi.
- [x] `ILeaseRepository` başvuru sorguları eklendi.
- [x] Karar için tracking + scope sorgusu eklendi.
- [x] Birimde açık başvuru kontrolü eklendi.
- [x] Taslakta tahakkuk/creation geçmişi kontrol sorguları eklendi.
- [x] Internal liste `Draft` ve `RevisionRequested` filtrelerini destekliyor.
- [x] Tenant portal liste sorgusu explicit durum filtresi taşıyor.
- [x] Tenant portal detay sorgusu explicit durum filtresi taşıyor.
- [x] Aktif dropdown sorgularının yalnız `Active` döndürdüğü doğrulandı.
- [x] Birim uygunluk sorgusu açık başvuruları dışlıyor.
- [x] Taslak edit sorgusu seçili birimi kaybetmiyor.
- [x] Document owner context tenant görünürlük kuralıyla uyumlu hale getirildi.

### 20.3 Repository testleri

- [x] Internal `tum` filtresi taslağı gösteriyor.
- [x] `onaybekliyor` yalnız Draft döndürüyor.
- [x] `revizyon` yalnız RevisionRequested döndürüyor.
- [x] Tenant liste taslağı göstermiyor.
- [x] Tenant detail taslak için null/NotFound sonucuna temel olacak dönüş yapıyor.
- [x] Başka tenant taslağı görülemiyor.
- [x] Property scope dışındaki başvuru gelmiyor.
- [x] Unit scope dışındaki başvuru gelmiyor.
- [x] Global access bütün internal başvuruları görebiliyor.
- [x] İnceleme geçmişi deterministik sırada geliyor.
- [x] Soft-delete Lease normal sorguda görünmüyor; audit sorgusunda bulunabiliyor.

### 20.4 Tamamlanma ölçütleri

- [x] Tenant sızıntısı testlerle engellenmiştir.
- [x] Aktif sözleşme dropdown'larında taslak yoktur.
- [x] Controller henüz yeni mutation yapmasa bile yeni sorgular bağımsız test edilebilir.

---

## 21. Faz 3 — Servis katmanı ve durum geçişleri

### 21.1 Amaç

Bütün başvuru yaşam döngüsünü controller'dan bağımsız, transaction'lı ve test edilebilir servis metotlarıyla uygulamak.

### 21.2 Görevler

- [x] Mevcut oluşturma akışı `CreateDraftAsync` olarak ayrıldı.
- [x] Taslak oluşturma `Status = Draft` atıyor.
- [x] Taslak oluşturma tahakkuk üretmiyor.
- [x] Taslak oluşturma `LeaseActivityType.Creation` eklemiyor.
- [x] `DraftCreated` geçmiş satırı ekleniyor.
- [x] `UpdateDraftAsync` uygulandı.
- [x] `RequestRevisionAsync` uygulandı.
- [x] `ResubmitRevisionAsync` uygulandı.
- [x] `ApproveAsync` uygulandı.
- [x] `DeleteDraftAsync` uygulandı.
- [x] Açıklama normalize/trim işlemi merkezi yapıldı.
- [x] Normal kullanıcı self-review guard'ı ve `IsSuperAdmin` claim istisnası eklendi.
- [x] Scope guard bütün kararlara eklendi.
- [x] Status guard bütün kararlara eklendi.
- [x] RowVersion kontrolü eklendi.
- [x] Onay öncesi zorunlu belge kontrolü eklendi.
- [x] Onay öncesi aktif birim çakışması tekrar kontrol edildi.
- [x] Onay öncesi tarife validasyonu tekrar çalışıyor.
- [x] `ChargeGenerationService` aktif olmayan Lease'i reddediyor.
- [x] Extend/Terminate/UpdateDueDate/Regenerate aktif durum guard'ı taşıyor.
- [x] Soft-delete bağlı taslak veri davranışı uygulandı.
- [x] Transaction interceptor ile bütün mutation servisleri sarılıyor.

### 21.3 Servis testleri

- [x] CreateDraft yalnız Draft oluşturuyor.
- [x] CreateDraft rate override'ları kaydediyor.
- [x] CreateDraft tahakkuk oluşturmuyor.
- [x] CreateDraft creation activity oluşturmuyor.
- [x] Draft update status'u değiştirmiyor.
- [x] Başka kullanıcı taslağı güncelleyemiyor.
- [x] Revision reason boşsa işlem reddediliyor.
- [x] RevisionRequested kaydı onaylanamıyor.
- [x] Resubmit status'u Draft yapıyor ve eski mesajı koruyor.
- [x] Delete reason boşsa işlem reddediliyor.
- [x] Active kayıt draft delete ile silinemiyor.
- [x] Normal kullanıcı self approval reddediliyor; `IsSuperAdmin` istisnası onay/revizyon/silme için test ediliyor.
- [x] Scope dışı approval/revision/delete reddediliyor.
- [x] Eksik zorunlu belge approval'ı engelliyor.
- [x] Aktif birim çakışması approval'ı engelliyor.
- [x] Approval Active yapıyor.
- [x] Approval creation activity ekliyor.
- [x] Approval mevcut algoritmayla tahakkuk üretiyor.
- [x] Approval hatasında status, history ve charges rollback oluyor.
- [x] İkinci approval reddediliyor.
- [x] Draft için GenerateForLease reddediliyor.
- [x] Draft için Extend/Terminate/Regenerate reddediliyor.

### 21.4 Tamamlanma ölçütleri

- [x] Bütün geçerli ve geçersiz durum geçişleri otomatik testlere sahiptir.
- [x] Servisler controller veya Razor olmadan test edilebilir.
- [x] Onay transaction rollback testi geçer.
- [x] Mevcut aktif sözleşme tahakkuk testleri bozulmaz.

**Doğrulama:** `LeaseApprovalServiceTests` 8/8 ve tam regresyon paketi 156/156 başarılıdır.

---

## 22. Faz 4 — Permission, controller, ViewModel ve validasyon

### 22.1 Amaç

Servis durum geçişlerini güvenli HTTP endpoint'lerine bağlamak ve permission katalog bütünlüğünü sağlamak.

### 22.2 Görevler

- [x] `Lease.Approve` permission eklendi.
- [x] `Lease.RequestRevision` permission eklendi.
- [x] `Lease.DeleteDraft` permission eklendi.
- [x] Action definition etiketleri eklendi.
- [x] ScopeAware listesi güncellendi.
- [x] Internal preset ve All listeleri güncellendi.
- [x] Permission architecture testleri güncellendi.
- [x] Yeni ViewModel'ler eklendi.
- [x] Yeni validator'lar eklendi ve DI discovery doğrulandı.
- [x] Create POST `CreateDraftAsync` çağırıyor.
- [x] Draft GET action eklendi.
- [x] UpdateDraft POST eklendi.
- [x] ResubmitRevision POST eklendi.
- [x] Approve POST eklendi.
- [x] RequestRevision POST eklendi.
- [x] DeleteDraft POST eklendi.
- [x] Anti-forgery bütün mutation action'larında mevcut.
- [x] Details, taslak durumunu aktif detay olarak göstermiyor.
- [x] Kiracı controller taslak detayını açmıyor.
- [x] Başarı ve hata redirect'leri status'a uygun.

### 22.3 Controller güvenlik testleri

- [x] Permission attribute'ları doğru sabitleri kullanıyor.
- [x] GET ile mutation yapılamıyor.
- [x] Anti-forgery eksik değil.
- [x] ModelState invalid olduğunda servis çağrılmıyor.
- [x] Hashid route davranışı korunuyor.
- [x] Tenant policy internal draft endpoint'ine erişemiyor.
- [x] Internal kullanıcı tenant portal filtresini aşan endpoint üretmiyor.

### 22.4 Tamamlanma ölçütleri

- [x] Endpoint'ler servisleri doğru input'larla çağırır.
- [x] Controller doğrudan context veya repository kullanmaz.
- [x] Permission katalog testleri başarılıdır.
- [x] UI olmadan HTTP seviyesinde temel akış çalışır.

**Doğrulama:** Faz 4'e özgü permission/controller/validator testleri 19/19 ve tam regresyon paketi 166/166 başarılıdır.

---

## 23. Faz 5 — Taslak/revizyon UI ve liste deneyimi

### 23.1 Amaç

Mevcut sözleşme oluşturma deneyimini koruyarak başvuru sahibi ve değerlendirici için açık, yanlış aksiyonu önleyen bir ekran sağlamak.

### 23.2 Görevler

- [x] Create form ortak partial'a çıkarıldı.
- [x] Draft ekranı ortak formu kullanıyor.
- [x] Create başlığı “Yeni Sözleşme Başvurusu” oldu.
- [x] Ana buton “Onaya Gönder” oldu.
- [x] Tahakkuk oluşmayacağına dair yardım metni eklendi.
- [x] Draft banner eklendi.
- [x] RevisionRequested banner eklendi.
- [x] Son revizyon mesajı üstte gösteriliyor.
- [x] İnceleme timeline partial'ı eklendi.
- [x] Başvuru sahibi alanları düzenleyebiliyor.
- [x] Değerlendirici alanları read-only görüyor.
- [x] RowVersion hidden alanı doğru taşınıyor.
- [x] Revizyon modalı eklendi.
- [x] Silme modalı eklendi.
- [x] Karakter limitleri ve validation mesajları eklendi.
- [x] Butonlar status ve permission'a göre gösteriliyor.
- [x] Çift submit UI seviyesinde azaltıldı.
- [x] Mevcut belgeler draft ekranında gösteriliyor.
- [x] Zorunlu belge eksikliği görünür hale getirildi.
- [x] Liste filtreleri eklendi.
- [x] Taslak/revizyon rozetleri eklendi.
- [x] Liste satırları doğru ekrana yönleniyor.
- [x] Tenant ekranlarında taslak etiketi veya fallback görünmüyor.

### 23.3 UI smoke senaryoları

- [x] Yeni başvuru formu mevcut tarife hesaplamasını koruyor.
- [x] Form gönderimi draft ekranına gidiyor.
- [x] Draft banner doğru metni gösteriyor.
- [x] Revision banner açıklama, kullanıcı ve tarihi gösteriyor.
- [x] Başvuru sahibi reviewer butonlarını görmüyor.
- [x] Reviewer form alanlarını değiştiremiyor.
- [x] Reviewer doğru aksiyon butonlarını görüyor.
- [x] Revizyon modalı boş açıklamayla gönderilemiyor.
- [x] Silme modalı boş açıklamayla gönderilemiyor.
- [x] Permission olmayan kullanıcı butonları görmüyor.
- [x] Direkt POST denemesi server-side reddediliyor.
- [x] Mobile ve desktop yerleşim responsive kaynak kontrolünden geçti.
- [x] Eski aktif sözleşme detay ekranı görsel kaynak ve regresyon kontrolünden geçti.

### 23.4 Tamamlanma ölçütleri

- [x] Başvuru sahibi ve değerlendirici davranışları otomatik testler ve 2026-08-08 kullanıcı kabulüyle doğrulanmıştır.
- [x] Ortak form create ve edit modunda aynı alanları üretir.
- [x] Status'a uymayan hiçbir buton görünmez.
- [x] UI mesajları tahakkukun onaydan önce oluşmadığını açıkça belirtir.

**Doğrulama:** Faz 5'e özgü UI/controller/permission kontrolleri 23/23 ve tam regresyon paketi 170/170 başarılıdır. Gerçek hesaplı tarayıcı UAT'si Faz 6'ya bırakılmıştır.

---

## 24. Faz 6 — Test, UAT, dokümantasyon ve kontrollü yayın

### 24.1 Amaç

Yeni akışın mevcut sözleşme, belge, tarife, kiracı portalı ve tahakkuk davranışlarını bozmadığını kanıtlamak ve kontrollü yayına hazırlamak.

### 24.2 Otomatik test grupları

#### Domain/validator testleri

- [x] Açıklama required/trim/max length.
- [x] Status transition matrisi.
- [x] Self-review kuralları.
- [x] RowVersion conflict davranışı.

#### Repository testleri

- [x] Internal filtreler.
- [x] Tenant izolasyonu.
- [x] Scope izolasyonu.
- [x] Open application unique kuralı.
- [x] Review history sıralaması.

#### Servis entegrasyon testleri

- [x] Draft no-charge invariantı.
- [x] Approval charge generation.
- [x] Approval rollback.
- [x] Multi-revision history.
- [x] Soft-delete ve audit korunması.
- [x] Required document approval guard.
- [x] Active-only operation guards.

#### Permission/architecture testleri

- [x] Permission bütün listelerde doğru.
- [x] Tenant preset'lerine internal permission sızmıyor.
- [x] Controller transaction/business rule taşımıyor.
- [x] Servis ve repository DI kayıtları eksiksiz.

#### Regresyon testleri

- [x] Mevcut aktif sözleşme oluşturma sonucundaki tahakkuk hesapları onay sonrası aynı.
- [x] Extend davranışı.
- [x] Terminate davranışı.
- [x] Regenerate davranışı.
- [x] Manual charge aktif sözleşme dropdown'ı.
- [x] Tenant lease list/detail.
- [x] Document upload/view.
- [x] Permission scope testleri.

### 24.3 UAT senaryoları

#### Senaryo A — Doğrudan onay

1. Kullanıcı A başvuru oluşturur.
2. Başvuru Draft görünür.
3. Tahakkuk listesinde kayıt yoktur.
4. Normal kullanıcı A onay butonu görmez; A `IsSuperAdmin` ise istisna gereği karar butonlarını görebilir.
5. Kullanıcı B başvuruyu inceler ve onaylar.
6. Başvuru Active olur.
7. Mevcut dönem tahakkukları eksiksiz oluşur.
8. Kiracı portalında sözleşme görünür.

#### Senaryo B — Tek revizyon

1. Kullanıcı A başvuru oluşturur.
2. Kullanıcı B açıklamayla revizyon ister.
3. Başvuru RevisionRequested olur.
4. Kullanıcı A mesajı görür.
5. Kullanıcı A alanı değiştirip yeniden gönderir.
6. Başvuru Draft olur.
7. Kullanıcı B güncel veriyi onaylar.
8. İki yönlü geçmiş timeline'da görünür.

#### Senaryo C — Çoklu revizyon

1. Başvuru iki defa revizyona gönderilir.
2. Her açıklama ayrı satır olarak saklanır.
3. İkinci açıklama birincisini ezmez.
4. Son onay geçmişe eklenir.

#### Senaryo D — Açıklamalı silme

1. Kullanıcı B Draft başvuruyu silmek ister.
2. Boş açıklama reddedilir.
3. Geçerli açıklamayla silme başarılı olur.
4. Başvuru normal listeden çıkar.
5. Audit ve inceleme geçmişinde neden bulunur.
6. Birim yeni başvuruya açılır.

#### Senaryo E — Yarış koşulu

1. İki değerlendirici aynı Draft sayfasını açar.
2. Birinci kullanıcı onaylar.
3. İkinci kullanıcı revizyon ister veya tekrar onaylar.
4. İkinci işlem concurrency hatası alır.
5. Tek tahakkuk seti vardır.

#### Senaryo F — Onay anında birim çakışması

1. Başvuru Draft durumundadır.
2. Birimde başka yolla aktif sözleşme oluşur.
3. Değerlendirici onaylar.
4. Onay reddedilir.
5. Başvuru Draft kalır.
6. Tahakkuk oluşmaz.

#### Senaryo G — Eksik belge

1. Dosya yükleme hatasıyla zorunlu belge kalıcılaşmaz.
2. Başvuru Draft kalır.
3. Onay zorunlu belge hatası verir.
4. Belge tamamlandıktan sonra onay başarılı olur.

#### Senaryo H — Tenant güvenliği

1. Kiracıya ait Draft bulunur.
2. Kiracı listesinde görünmez.
3. Doğrudan Draft id ile tenant detail çağrısı NotFound döner.
4. Belge endpoint'i taslak belgeyi döndürmez.

### 24.4 Kontrollü yayın

Adım adım yayın, permission geçişi, sağlık sorguları ve geri dönüş prosedürü için
[`phase-19-release-runbook.md`](phase-19-release-runbook.md) kullanılmalıdır.

- [x] Migration yedeği/geri dönüş planı hazır.
- [x] Permission seed/claim yenileme etkisi kontrol edildi; mevcut rollere otomatik backfill olmadığı runbook'a kaydedildi.
- [x] Mevcut kullanıcı rol/claim karar davranışı kullanıcı kabulüyle doğrulandı.
- [x] Test başvurusu ve karar akışları kullanıcı tarafından sorunsuz kabul edildi.
- [x] Onay öncesi DB'de charge olmadığı otomatik test ve sağlık sorgusuyla doğrulandı.
- [x] Onay sonrası charge sayısı ve tutarları otomatik entegrasyon testleriyle doğrulandı.
- [x] Tenant görünürlüğü otomatik testler ve kullanıcı kabulüyle doğrulandı.
- [x] Audit ve timeline otomatik testler ve kullanıcı kabulüyle doğrulandı.
- [x] Testlerde concurrency/FK sorunu gözlenmedi; DB invariant sorguları temizdir.

### 24.5 Faz tamamlanma ölçütleri

- [x] Bütün otomatik testler başarılı.
- [x] Kullanıcı 2026-08-08 tarihinde testlerde sorunla karşılaşmadığını bildirerek UAT kabulü verdi.
- [x] Otomatik kabul ve test DB kapsamında açık kritik/yüksek risk yok.
- [x] Permission ve migration yayın adımları dokümante edildi.
- [x] Bu dokümandaki tamamlanan checklist maddeleri gerçek sonuca göre güncellendi.

**Faz 6 kapanış kaydı (2026-08-08):** `IsSuperAdmin` self-review istisnası dahil tam regresyon 172/172 ve ayrı çıktı build'i başarılıdır. Test DB'de iki migration uygulanmıştır; açık başvuru tekrarı, taslak tahakkuku, taslak Creation activity ve mükerrer sözleşme-dönem tahakkuku kontrollerinin tamamı `0` dönmüştür. Kullanıcı testlerde sorunla karşılaşmadığını bildirerek UAT kabulü vermiş ve Faz 19'un kapatılmasını onaylamıştır.

---

## 25. Karar kayıtları

| # | Tarih | Karar | Durum | Gerekçe |
|---|---|---|---|---|
| 1 | 2026-08-07 | Taslak ayrı başvuru tablosu yerine `Lease` satırında tutulur | Kabul edilen plan kararı | Belge ve tarife sahipliğini taşımamak |
| 2 | 2026-08-07 | `Draft` ve `RevisionRequested` yeni Lease durumlarıdır | Kabul edilen plan kararı | Başvuru yaşam döngüsünü açık temsil etmek |
| 3 | 2026-08-07 | Tahakkuk yalnız onaydan sonra üretilir | Kabul edilen gereksinim | Finansal kayıtların onaysız oluşmasını engellemek |
| 4 | 2026-08-07 | İnceleme mesajları ayrı, çoklu geçmiş tablosunda tutulur | Kabul edilen gereksinim | Birden fazla revizyon turunu korumak |
| 5 | 2026-08-07 | Revizyon ve silme açıklaması zorunludur | Kabul edilen gereksinim | Karar gerekçesini audit edebilmek |
| 6 | 2026-08-07 | Silme soft-delete olarak uygulanır | Faz 0'da kesinleştirildi | Geçmiş ve audit kaybını önlemek |
| 7 | 2026-08-07 | Kiracı portalı taslak/revizyon kayıtlarını göstermez | Faz 0'da kesinleştirildi | Onaysız başvuruyu sözleşme gibi göstermemek |
| 8 | 2026-08-07 | Normal başvuru sahibi kendi başvurusunu değerlendiremez | 2026-08-08 tarihli Karar 14 ile süper admin istisnası eklendi | Dört göz kontrolü |
| 9 | 2026-08-07 | Birim başına tek açık başvuru vardır | Faz 0'da kesinleştirildi | Rakip taslaklar ve onay çakışmasını azaltmak |
| 10 | 2026-08-07 | Onay aynı Lease satırını Active yapar; yeni satır kopyalamaz | Kabul edilen plan kararı | Tek kimlik ve bağlı veri bütünlüğü |
| 11 | 2026-08-07 | Onay açıklaması opsiyoneldir | Faz 0'da kesinleştirildi | Onayı gereksiz veri girişine bağlamadan açıklama bırakabilmek |
| 12 | 2026-08-07 | Serbest `Comment` aksiyonu ilk sürümde yoktur | Faz 0'da kesinleştirildi | Kullanılmayan aksiyon ve UI kapsamı oluşturmamak |
| 13 | 2026-08-07 | `Approve`, `RequestRevision` ve `DeleteDraft` bağımsız izinlerdir | Faz 0'da kesinleştirildi | En az yetki ve görev ayrılığı sağlamak |
| 14 | 2026-08-08 | `IsSuperAdmin=true` claim'li kullanıcı kendi başvurusunu onaylayabilir, revizyona gönderebilir veya silebilir | Kullanıcı tarafından onaylandı ve uygulandı | Acil sistem yönetimi operasyonlarında kontrollü istisna sağlamak |
| 15 | 2026-08-08 | Faz 19 kullanıcı kabulüyle tamamlandı; aktif çalışma Faz 18 / İç Faz 0'a devredildi | Tamamlandı | Testlerde sorun gözlenmemesi ve 172/172 regresyon başarısı |

Karar 6–13 kullanıcı tarafından değiştirilirse ilgili domain, UI, test ve migration bölümleri birlikte güncellenmelidir.

---

## 26. Risk kayıtları

| Risk | Etki | Önlem |
|---|---|---|
| Taslak tenant portalına sızar | Onaysız bilgi kiracıya görünür | Tenant repository'lerinde explicit status filtresi ve entegrasyon testi |
| İki kullanıcı aynı taslağı karara bağlar | Çift durum geçişi/mükerrer charge | RowVersion + status guard + transaction |
| Onay ortasında charge üretimi hata verir | Active ama eksik finansal kayıt | Tek transaction ve rollback testi |
| Dosya yükleme başarısız olur | Eksik belgeli taslak | Onay anında DB'den zorunlu belge kontrolü |
| Mevcut Details bilinmeyen status'u aktif gösterir | Yanlış UI ve aksiyonlar | Draft route ayrımı ve explicit switch |
| Aktif-only servis draft üzerinde çalışır | Yanlış fesih/uzatma/charge | Servis guard'ları ve negatif testler |
| Review history silinen Lease ile erişilemez | Audit kaybı | Restrict FK, history'yi silmeme, audit özel sorgusu |
| Filtered unique index provider sözdizimi hatası | Migration/yayın hatası | Migration SQL'ini elle inceleme ve gerçek SQL Server testi |
| Permission preset eksik güncellenir | Yetkili kullanıcı işlem yapamaz | Permission architecture testleri ve rol UAT |
| Ortak form refactor'ı tarife JS'ini bozar | Yanlış kira tutarı | Create/edit smoke ve mevcut pricing testleri |
| BaseEntity default status yanlış yorumlanır | Yanlışlıkla Active kayıt | Üretim create yolunu tek servisle sınırlandırma ve architecture test |

---

## 27. Kapsam dışı özellikler

Bu çalışma aşağıdakileri içermez:

- Kiracı kullanıcısının sözleşme başvurusu oluşturması.
- E-posta, SMS veya push bildirimleri.
- Çok kademeli onay zinciri.
- Departman bazlı paralel onay.
- Onay delegasyonu veya vekâlet.
- Dijital imza/e-imza entegrasyonu.
- Sözleşme PDF'i üretme veya imzalama.
- Revizyonlar arasında alan bazlı diff ekranı.
- Silinen başvurular için kullanıcıya açık arşiv ekranı.
- Aktif sözleşmenin yeniden onaya gönderilmesi.
- Aktif sözleşme değişiklikleri için ayrı onay mekanizması.
- Tahakkuk hesaplama algoritmasının değiştirilmesi.
- Sona eren sözleşme status otomasyonunun düzeltilmesi.
- Genel bildirim altyapısı.

Bu maddeler ihtiyaç halinde ayrı gereksinim ve plan olarak ele alınır.

---

## 28. Olası dosya etkileri

Bu liste uygulama sırasında yeniden doğrulanmalıdır.

### 28.1 Mevcut dosyalar

```text
KiraTakip/Models/Enums.cs
KiraTakip/Models/Entities/Lease.cs
KiraTakip/Data/ApplicationDbContext.cs
KiraTakip/Authorization/PermissionCatalog.cs
KiraTakip/Controllers/LeaseController.cs
KiraTakip/Controllers/TenantLeaseController.cs
KiraTakip/Services/LeaseService.cs
KiraTakip/Services/ChargeGenerationService.cs
KiraTakip/Services/Interfaces/ILeaseService.cs
KiraTakip/Repositories/LeaseRepository.cs
KiraTakip/Repositories/UnitRepository.cs
KiraTakip/Repositories/Interfaces/ILeaseRepository.cs
KiraTakip/Models/DTOs/Lease/LeaseQueryDtos.cs
KiraTakip/Models/DTOs/LeaseDetailDto.cs
KiraTakip/Models/DTOs/LeaseListItemDto.cs
KiraTakip/Models/ViewModels/CreateLeaseViewModel.cs
KiraTakip/Validators/CreateLeaseViewModelValidator.cs
KiraTakip/Views/Lease/Create.cshtml
KiraTakip/Views/Lease/Index.cshtml
KiraTakip/Views/Lease/Details.cshtml
KiraTakip/Views/TenantLease/Index.cshtml
KiraTakip/Views/TenantLease/Details.cshtml
KiraTakip/Views/AdminRole/Create.cshtml
KiraTakip/Views/AdminRole/Edit.cshtml
KiraTakip/Services/SeedDataService.cs
KiraTakip/Migrations/*
KiraTakip.Tests/PermissionTests.cs
KiraTakip.Tests/ChargeArchitectureTests.cs
KiraTakip.Tests/TenantLeaseArchitectureTests.cs
```

### 28.2 Beklenen yeni dosyalar

```text
KiraTakip/Models/Entities/LeaseReviewHistory.cs
KiraTakip/Models/DTOs/Lease/LeaseReviewDtos.cs
KiraTakip/Models/ViewModels/LeaseDraftViewModel.cs
KiraTakip/Models/ViewModels/RequestLeaseRevisionViewModel.cs
KiraTakip/Models/ViewModels/DeleteLeaseDraftViewModel.cs
KiraTakip/Validators/RequestLeaseRevisionViewModelValidator.cs
KiraTakip/Validators/DeleteLeaseDraftViewModelValidator.cs
KiraTakip/Repositories/LeaseReviewHistoryRepository.cs
KiraTakip/Repositories/Interfaces/ILeaseReviewHistoryRepository.cs
KiraTakip/Views/Lease/Draft.cshtml
KiraTakip/Views/Lease/_LeaseForm.cshtml
KiraTakip/Views/Lease/_LeaseReviewTimeline.cshtml
KiraTakip/Views/Lease/_LeaseReviewActions.cshtml
KiraTakip.Tests/LeaseApprovalArchitectureTests.cs
```

Dosyalar gereksiz yere çoğaltılmamalıdır. Mevcut ortak ViewModel/validator yapısı uygun ise aynı sorumluluk sınırında genişletilebilir.

---

## 29. Fazlar arası bağımlılık haritası

```text
Faz 0 — Kararlar
   │
   ▼
Faz 1 — Domain + Migration
   │
   ▼
Faz 2 — Repository + Sorgu İzolasyonu
   │
   ▼
Faz 3 — Servis + Durum Geçişleri
   │
   ▼
Faz 4 — Permission + Controller + Validation
   │
   ▼
Faz 5 — UI/UX
   │
   ▼
Faz 6 — Test + UAT + Yayın
```

Kurallar:

- Faz 1 tamamlanmadan repository yeni entity'ye bağlanmaz.
- Faz 2 tamamlanmadan servis kapsam ve tenant izolasyonu varsaymaz.
- Faz 3 tamamlanmadan controller durum geçişi yapmaz.
- Faz 4 tamamlanmadan UI karar butonları açılmaz.
- Faz 5 tamamlanmadan UAT başlatılmaz.
- Her faz kendi testleri geçmeden sonraki faza ilerlemez.

---

## 30. Üretim kabul kriterleri

Çalışma aşağıdaki maddelerin tamamı doğrulanmadan bitmiş sayılmaz:

- [x] Yeni form gönderiminde `Lease.Status == Draft`.
- [x] Yeni form gönderiminde hiçbir sözleşme tahakkuku oluşmuyor.
- [x] Yeni form gönderiminde `LeaseActivityType.Creation` oluşmuyor.
- [x] `DraftCreated` inceleme geçmişi oluşuyor.
- [x] Taslak sahibi kaydı düzenleyebiliyor.
- [x] Başka kullanıcı taslağı düzenleyemiyor.
- [x] Yetkili kullanıcı taslağı onaylayabiliyor.
- [x] Normal başvuru sahibi kendi kaydını onaylayamıyor; `IsSuperAdmin` istisnası doğrulandı.
- [x] Onay işlemi sözleşmeyi Active yapıyor.
- [x] Onay işlemi mevcut algoritmayla tahakkukları oluşturuyor.
- [x] Onay işleminde creation activity oluşuyor.
- [x] Onay hatasında bütün DB değişiklikleri geri alınıyor.
- [x] Revizyon açıklaması olmadan işlem yapılamıyor.
- [x] Silme açıklaması olmadan işlem yapılamıyor.
- [x] Birden fazla revizyon açıklaması ayrı kayıtlar olarak korunuyor.
- [x] Revizyon sonrası yeniden gönderim Draft durumuna dönüyor.
- [x] RevisionRequested doğrudan onaylanamıyor.
- [x] Silinen başvuru normal listede görünmüyor.
- [x] Silme nedeni audit/geçmişte korunuyor.
- [x] Bir birimde iki açık başvuru oluşturulamıyor.
- [x] Taslak birimi kiralanmış göstermiyor.
- [x] Taslak kiracı portalında görünmüyor.
- [x] Taslak belgesi kiracı portalından indirilemiyor.
- [x] Aktif sözleşme dropdown'ları taslakları içermiyor.
- [x] Draft üzerinde extend/terminate/regenerate çalışmıyor.
- [x] Aynı taslak iki kez onaylanamıyor.
- [x] Mükerrer tahakkuk oluşmuyor.
- [x] Permission kapsamı dışındaki kararlar reddediliyor.
- [x] Mevcut aktif sözleşme, tahakkuk ve tenant testleri başarılı.
- [x] Migration SQL Server üzerinde doğrulandı.
- [x] UI desktop ve mobile smoke testleri kullanıcı kabulüyle tamamlandı.

---

## 31. Genel ilerleme özeti

- [x] Faz 0 — Kararların ve kapsamın sabitlenmesi
- [x] Faz 1 — Domain modeli ve migration
- [x] Faz 2 — Repository ve sorgu izolasyonu
- [x] Faz 3 — Servis katmanı ve durum geçişleri
- [x] Faz 4 — Permission, controller, ViewModel ve validasyon
- [x] Faz 5 — Taslak/revizyon UI ve liste deneyimi
- [x] Faz 6 — Test, UAT, dokümantasyon ve kontrollü yayın

Bu özet yalnız faz seviyesini gösterir. Alt görevlerin doğruluk kaynağı ilgili faz bölümündeki checklist'tir.
