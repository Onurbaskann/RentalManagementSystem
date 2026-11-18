# Refactor — Paylaşımlı ve Açık Birim: Gereksinim Analizi

## Context
Bir birim birden fazla kiracıya m² bazlı kiralanabilmeli. Mevcut sistem "1 birim = aynı anda 1 aktif sözleşme = 1 kiracı" varsayımıyla kurulu:
- Çakışma kontrolü bu varsayıma göre çalışıyor.
- m² fiyat çarpanı `unit.Area` ([ChargeGenerationService.cs:258](../src/KiraTakip.Application/Services/Charges/ChargeGenerationService.cs#L258), [LeaseRatePolicy.cs](../src/KiraTakip.Domain/Leases/LeaseRatePolicy.cs)).
- Doluluk dolu/boş ikili.
- İstatistikler `lease.Unit.Area` kullanıyor ([StatisticsService.cs:50](../src/KiraTakip.Application/Services/Reporting/StatisticsService.cs#L50)).

Gereksinim 2026-11-16/17 oturumunda kullanıcıyla soru-cevap ile netleştirildi. Bu dosya kararların özetidir; henüz kod değişikliği yoktur.

> **Okuma kuralı:** Bu dosyadaki kararlar kullanıcı tarafından onaylanmıştır. Tüm açık noktalar 2026-11-17'de kararlaştırıldı. Yeni bir belirsizlik çıkarsa cevabı tahmin edilmez, kullanıcıya sorulur.
>
> **Terim:** "Açık birim", türünün alt kırılımı **Kontenjan bazlı** olan birimdir. "Normal birim", alt kırılımı **Birim bazlı** olan birimdir.

## Mevcut sistem gerçekleri (kodda doğrulandı, 2026-11-17)
- Sözleşme tek birim ve tek kiracıya bağlı (`Lease.UnitId`, `Lease.TenantId`).
- Entity sınıfları İngilizce, tablo/kolon adları Türkçe (`[Table]`/`[Column]`). Örnek: `LeaseRateOverride` → `SozlesmeTarifeler`.
- **Çakışma kontrolü:** `LeaseRepository.HasOverlappingActiveLeaseAsync(unitId, start, end, excludeLeaseId)`, çağrı yerleri [LeaseService.cs:371](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L371) ve [LeaseService.cs:643](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L643).
- **Sözleşme tarifesi:** `LeaseRateOverride` (LeaseId, ChargeTypeId, CalculationMethod, UnitValue, KdvRate). Sözleşme bazında satırlar.
- **m² çarpanı:** `LeaseRatePolicy.CalculateMultiplier(method, unitArea)` → M2 ise `unitArea`, Fixed ise 1.
- **Depozito:** Ayrı bir borç tipi (seed'de M2 yöntemli). **Depozito iadesi mekanizması kodda yok** ("Refund"/"İade" araması sonuç vermedi).
- **Kullanım türü:** `UnitTypeUsage` (Rentable / Reservable / NonRentable), birim türünde tutuluyor.
- **Onay:** İç kullanıcı onaylar; sözleşmeyi oluşturan onaylayamaz, süper admin hariç (`!isOwner || IsSuperAdmin`, [LeaseController.cs:231-232](../src/KiraTakip.Web/Controllers/LeaseController.cs#L231-L232)).
- **Sözleşme write endpoint'leri** (`LeaseController`): `Create`, `UpdateDraft`, `ResubmitRevision`, `UploadDraftDocument` (üçü de `Lease.Create` izniyle), `Approve`, `RequestRevision`, `DeleteDraft`, `Extend`, `UpdateDueDate` (`Lease.Edit`), `Terminate`, `Regenerate`.
- **Birim düzenleme:** Ayrı bir `Unit/Edit` endpoint'i yok. Birimlerin alanı, türü ve katı taşınmaz ekranından düzenlenir: `Property/Edit`, izin `Internal.Property.Edit` ([PropertyController.cs:112](../src/KiraTakip.Web/Controllers/PropertyController.cs#L112), [PropertyService.cs:347](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L347)). `Internal.Unit.Edit` katalogda var ama hiçbir controller kullanmıyor. `UnitController` yalnız tarife ve rezervasyon kuralı işlemleri içerir.
- **Tek birimli taşınmazın alanı:** Birim alanı taşınmazın kapalı ve açık alanından hesaplanır (`PropertyStructurePolicy.CalculateSingleUnitArea`, [PropertyService.cs:291](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L291)).
- **Birim türü ekranı:** `AdminUnitTypeController` (`Create`, `Edit`, `ToggleStatus`); izinler `Internal.UnitType.Create` / `Internal.UnitType.Edit`.
- **Genel parametreler:** `SistemAyarlari` altyapısı, ekran `AdminSystemSettingController`, izin `Internal.Parameter.Edit`.
- **Tarife kaynağı biliniyor:** `RateResolverService` sonucu (`RateSnapshot`) kaynağı taşır: `LineItemSourceType` (`LeaseRateOverride`, `UnitRateOverride`, `PropertyRateOverride`, `RateSchedule` …). "Tarife sözleşme tarifesinden mi geldi" ayrımı için yeni mekanizma gerekmez.
- **Sözleşme tarifesi doğrulaması** birim alanını kullanır: `EnsureRateOverridesAsync(..., unit.Area, ...)` ([LeaseService.cs:377](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L377), [:504](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L504), [:647](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L647)).
- **Doluluk durumu:** `OccupancyStatus` (Vacant / Leased / ExpiringSoon), `LeaseOccupancyPolicy`. "Süresi dolmak üzere" gün sayısı `Lease.ExpiringSoonStatusDays` sistem ayarından gelir.
- **Otomatik bitiş:** `LeaseExpirationBackgroundService` bitişi geçmiş Aktif sözleşmeleri `Ended` yapar.
- **Çakışmada sayılan durumlar:** `HasOverlappingActiveLeaseAsync` bugün yalnız `Active` sözleşmelere bakıyor; Taslak ve Revizyon İstendi sayılmıyor.
- **Birim başına tek açık başvuru (mevcut kural):** `LeaseRepository.HasOpenApplicationForUnitAsync` ([LeaseRepository.cs:627-632](../src/KiraTakip.Infrastructure/Persistence/Repositories/Leases/LeaseRepository.cs#L627-L632)) bir birimde aynı anda yalnız bir Taslak veya Revizyon İstendi sözleşmeye izin verir; **tarihe bakmaz**. Oluşturmada ([LeaseService.cs:104](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L104)) ve taslak güncellemede ([LeaseService.cs:676](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L676)) çağrılır. Normal birimde ikinci bir taslak bugün de açılamaz. Bu kural paylaşımlı grubu (aynı birimde birden fazla taslak) ve açık birimi (birden fazla bağımsız taslak) olduğu haliyle engeller; uyarlanmalıdır.
- **Görev ayrılığı:** `LeaseService.EnsureNotSelfReview` ([LeaseService.cs:754-758](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L754-L758)) oluşturanın kendi başvurusunu değerlendirmesini engeller (süper admin hariç).
- **Birim türü değişikliği:**
  - `UnitTypeService.UpdateAsync` kullanım türü değişikliğinde türdeki birimlerin sözleşmelerine bakmıyor ([UnitTypeService.cs:91](../src/KiraTakip.Application/Services/Properties/UnitTypeService.cs#L91)).
  - Taşınmaz ekranında tek bir birimin kiralama türünden rezervasyon türüne geçmesi yasak ([PropertyService.cs:429-438](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L429-L438)). Tür ekranından bütün türü çevirmek bu yasağı atlatıyor.
  - Sözleşme ekranı birim türünü ve kullanım türünü göstermiyor; birim bilgileri her açılışta güncel kayıttan okunuyor ([Lease/Details.cshtml:297-305](../src/KiraTakip.Web/Views/Lease/Details.cshtml#L297-L305)). Bu yüzden türü değişen birimin eskiden kiralanabilir olduğu sözleşmeden anlaşılmıyor; iz yalnız hareket geçmişinde (`AuditSaveChangesInterceptor`) kalıyor.
  - Rezervasyon tarafı sözleşmelere, tahakkuk üretimi kullanım türüne bakmıyor. Aktif sözleşmeli birimin türü rezervasyona çevrilirse birim kira tahakkuku üretmeye devam ederken rezerve edilebilir olur ve doluluktan düşer.

## Terimler
- **P:** Birimin fiziksel m²'si (`Unit.Area`). Açık sözleşmesi varken değiştirilemez (bkz. Veri modeli / alan kilidi).
- **K:** Kontenjan (kişi). Yalnız açık birimde var.
- **s:** P / K (norm).
- **Kiralanan m²:** Sözleşmeye yazılır; bedeli ve oranı belirler.
- **Fiziksel m²:** Sözleşmenin kapasiteden düşülen alanı.
- **k:** (P − Σ fiziksel m²) / boş kontenjan.

## Paylaşımlı birim
- Normal bir birimdir. Paylaşım kararı sözleşme anında verilir ve bir **grup** kurulur. Her kiracıya ayrı sözleşme açılır.
- **Grup bütün olarak hareket eder:**
  - Sözleşmeler aynı gün başlar, aynı gün biter.
  - Σ m² her zaman P'ye eşittir.
- **Tarife:** Grup tarifesi diye bir kavram yok. Her sözleşme, tekil sözleşmede olduğu gibi isteğe bağlı kendi sözleşme tarifesine (`LeaseRateOverride`) sahiptir ve mevcut zincir (sözleşme → birim → taşınmaz → genel) aynen çalışır. Tek fark, birim alanı yerine sözleşmeye girilen m²'nin kullanılmasıdır. Bir kiracıya sözleşme tarifesi tanımlanırsa onun tutarları bu tarifeden, tanımlanmayan kiracınınkiler üst katmandan hesaplanır. Kopyalama ya da ortak satır yoktur.
- **Aktivasyon:** Mevcut iç onay akışı kullanılır. Kiracı onay sürecinde yer almaz. Onaycı, `Internal.Lease.ApproveGroup` iznine sahip kullanıcıdır; görev ayrılığı yoktur, grubu oluşturan kişi bu izne de sahipse kendi grubunu onaylayabilir (bkz. Yetkiler).
  - Onaycı, gruptaki her sözleşme için ayrı ayrı revizyon isteyebilir.
  - Grup tek bir onay işlemiyle birlikte aktifleşir. Gruptaki sözleşmelerden biri Revizyon İstendi durumundaysa grup onaylanamaz.
  - **Grup taslağı işlemleri:** Grup taslağı yeni grup endpoint'leriyle yönetilir: `Lease/UpdateGroup` (paylar/m², tarihler, kiracılar), `Lease/ResubmitGroup`, `Lease/DeleteGroupDraft`. Belge yüklemesi sözleşme bazında mevcut `Lease/UploadDraftDocument` ile yapılır. Gruptaki bir sözleşmede tekil `UpdateDraft`, `ResubmitRevision` ve `DeleteDraft` kapalıdır.
  - **Revizyonda m² düzeltmesi:** m² yalnız grup düzeyinde, grup düzenleme ekranında (`Lease/UpdateGroup`) düzeltilir. Oluşturan kişi tüm payları birlikte günceller (örn. A 30 + B 30 → A 35 + B 25); kayıtta Σ m² = P kontrol edilir. Tekil sözleşme revizyonunda m² alanı kilitlidir.
- **Grup büyüklüğü:** Grupta en az 2 sözleşme olur; tek kiracı için normal sözleşme kullanılır. Bir kiracı grupta yalnız bir pay (tek sözleşme) alabilir. Her pay 0 m²'den büyük olmalıdır (örn. P=60'ta A 60 + B 0 reddedilir; 2026-11-17 kullanıcı kararı).
  - Yeniden yapılanma da en az 2 kiracılı yeni grup ister. Tek kiracı kalacaksa önce `TerminateGroup`, sonra normal `Lease/Create` kullanılır. Örnek: A 30 + B 30, B çıkıyor ve A 60 m²'nin tamamını alacak → grup feshedilir, A için normal sözleşme açılır.
- **Tekil işlemler kapalı:** Gruptaki bir sözleşmeye tekil `Lease/Approve`, `UpdateDraft`, `ResubmitRevision`, `DeleteDraft`, `Extend` ve `Terminate` ile işlem yapılamaz; grup işlemleri kullanılır. Açık kalan tekil işlemler: `RequestRevision`, `UploadDraftDocument`.
- **Uzatma:** Tüm gruba birlikte uygulanır.
- **Yeniden yapılanma** (fesih, çıkış, yeni kiracı, m² değişikliği):
  - Tek ekranda yapılır. Eski grubun tüm sözleşmeleri feshedilir, yeni grup taslak olarak açılır.
  - **Zamanlama:** Fesih işlem anında, yöneticinin girdiği fesih tarihiyle yapılır. Yeni grup taslak kalır; eski kiracıların mutabakatı sürerken birim boş kalabilir. "Boş alan yok" kuralı yalnız aktif bir grubun içinde Σ m² = P anlamına gelir.
  - **Yeni grubun başlangıcı:** Form fesih tarihinin ertesi gününü önerir; yönetici daha ileri bir tarih seçebilir (arada birim boş kalır). Fesih tarihi veya öncesi seçilemez.
  - Yeni grup, eski gruptaki her kiracının bakiyesi kapanıp depozitosu iade edilmeden aktifleşemez.
  - **Depozito iadesi (geçici):** İade özelliği bu çalışmadan sonra ayrı bir iş olarak eklenecek. Şimdilik ön koşul bir "depozito iade durumu" servisi arayüzü üzerinden kontrol edilir; bu çalışmadaki dummy uygulama her zaman "iade edildi" döner. Pratikte şu an yalnız bakiye kontrolü işler. İade özelliği gelince yalnız bu servisin uygulaması değişir.
  - Yeni sözleşmelere tam depozito kesilir.
- Son grup bitince birim normal birime döner.
- Oranlama borç tipine değil hesaplama yöntemine bağlıdır (bkz. Çarpan formülleri):
  - **M2 yöntemli kalem** (kira, seed'deki depozito): kiralanan m² × birim fiyat.
  - **Fixed yöntemli kalem** üst katmandan (birim / taşınmaz / genel) geliyorsa m² / P oranında yansır. Boş alan yoktur, bu yüzden oranların toplamı her zaman %100'dür.
  - Sözleşme tarifesindeki Fixed tutar ise o kiracının kendi tutarıdır, oranlanmaz. Örnek: P=60, A 20 + B 40; birim tarifesinde aidat sabit 6.000 TL → A 2.000, B 4.000. A'ya sözleşme tarifesiyle aidat sabit 1.500 TL girilirse A 1.500, B 4.000 öder.

## Açık birim
- Birim, alt kırılımı **Kontenjan bazlı** olan bir birim türüyle tanımlanır; P ve K birim tanımında girilir. Alt kırılım birim türüne bağlıdır (bkz. Birim türü ve alt kırılım).
- 1 sözleşme = 1 kontenjan. Sözleşmeler birbirinden bağımsızdır. m² değişikliği = fesih + yeni sözleşme.
- **Kiralanan m² > s:** Yönetici uyarılır, fiziksel m²'yi kendisi belirler: min. kullanım m² ile kiralanan m² arasında serbest bir değer (s'nin altına da inebilir). Örnek: P=70, K=10 → s=7, min. 5; 10 m² kiralayan için fiziksel m² 5–10 arası (örn. 6 veya 8,5).
- **Fiziksel m² alt sınırı:** Fiziksel m² hiçbir durumda min. kullanım m²'sinin altına inemez.
- **Kiralanan m² ≤ s:** Fiziksel m² = kiralanan.
- **Kiralanan m²:** En az min. kullanım m² olmalı. Bu, genel bir parametredir.
- **Kapasite kuralları:** Her yeni sözleşmede, uzatmada ve kontenjan artışında kontrol edilir; sözleşmenin tarih aralığındaki en yoğun güne bakılır. Aktif, gelecek tarihli, Taslak ve Revizyon İstendi durumundaki sözleşmeler birlikte sayılır (taslak yer tutar). Örnek: K=10, 8 aktif + 2 taslak → yeni taslak açılamaz. "Taslak yer tutar" kuralı normal ve paylaşımlı birimlerin çakışma kontrolünde de geçerlidir (bkz. Ortak kurallar).
  - Dolu kontenjan ≤ K olmalı.
  - Σ fiziksel m² ≤ P olmalı.
  - Boş kontenjan varsa k ≥ min. kullanım m² olmalı. Bu kural ihlal ediliyorsa kayıt yapılmaz; yönetici ya kontenjanı azaltır ya da fiziksel m²'yi düşürür.
- **Son kontenjan:** Kalan fiziksel m²'nin tamamı uyarıyla hem kiralanan hem fiziksel m² olarak önerilir; yönetici değiştirebilir. Örnek: P=70, K=10, 9 dolu, Σ fiziksel 60 → form kiralanan 10, fiziksel 10 önerir.
- **Kontenjan değişikliği:**
  - K artırılabilir; ancak k minimumun altına düşecekse artış engellenir ve izin verilen en yüksek artış gösterilir.
  - K, dolu kontenjan sayısının altına indirilemez.
  - Mevcut sözleşmelere dokunulmaz.
  - **Geçerlilik:** K değişikliği kaydedildiği gün geçerli olur (`EffectiveFrom` = bugün; ileri tarih girilmez). Kapasite kontrolleri her zaman güncel K'yı kullanır. Örnek: K 10'dan 12'ye çıkarıldıysa ertesi gün açılan 2027 sözleşmesi K=12'ye göre kontrol edilir.
  - **Tarihçe tutulur:** Her K değişikliği geçerlilik tarihiyle saklanır. İleride eklenecek geçmiş dönem raporları o tarihteki K'yı kullanabilsin diye tutulur. Örnek: Ocak K=10, 6 dolu; Mart'ta K=12 yapıldı → böyle bir rapor Ocak için 6/10 = %60 gösterir.
  - **Bu çalışmada yalnız kaydedilir.** Tarihçeyi okuyan rapor ya da ekran bu çalışmaya girmez; bugün geçmiş dönem doluluk raporu yoktur.
- **Bedel:** M2 yöntemli kalemler kiralanan m² üzerinden hesaplanır. Üst katmandan gelen Fixed kalemler kiralanan m² / P oranında yansır; sözleşme tarifesindeki Fixed tutar oranlanmaz (paylaşımlıdaki kuralla aynı). Tarife sözleşme bazındadır.
- **Gösterim:** Boş kontenjan ve boş m² birlikte gösterilir.
- **Sözleşme formu birim listesi:** Kontenjan bazlı birim yalnız boş kontenjanı varsa listelenir, yanında boş kontenjan ve boş m² bilgisiyle. Örnek: O-1 (K=10, 6 dolu, 28 m² boş) "4 kişi · 28 m² boş" olarak görünür. O-2 (K=8, 8 dolu) görünmez.

## Ortak kurallar
- **Taslak yer tutar (tüm birimler):** Normal birim dahil her birimde Taslak ve Revizyon İstendi sözleşmeler çakışma/kapasite hesabına girer. Tarih çakışması kontrolü (`HasOverlappingActiveLeaseAsync`) bugün yalnız Active'e bakıyor; Taslak ve Revizyon İstendi'yi de sayacak şekilde genişler. Normal birimde ikinci taslağı zaten "tek açık başvuru" kuralı engellediği için pratik etki en çok kontenjan bazlı birimin kapasite hesabında ve paylaşımlı grup/tekil ayrımında görülür.
- **Açık sözleşme:** Bu dokümanda aktif, gelecek tarihli, Taslak veya Revizyon İstendi durumundaki sözleşme demektir.
- **Birim başına tek açık başvuru (uyarlama):**
  - Normal birimde mevcut kural aynen kalır: aynı anda tek Taslak/Revizyon İstendi, tarihe bakılmaz. Örnek: B-12'de 2027 için X'in taslağı varken 2028 için Y'ye taslak açılamaz.
  - Paylaşımlıda grup tek başvuru sayılır: gruptaki taslaklar birbirini engellemez; aynı birimde ikinci bir grup ya da tekil taslak engellenir.
  - Kontenjan bazlı birimde bu kural uygulanmaz; yerine kapasite kontrolü geçer. Örnek: O-1'de (K=10) aynı anda 3 taslak açılabilir.

## Veri modeli
- `Lease`: `LeaseGroupId` (nullable FK) ve birim anlık bilgisi kolonları eklenir.
  - **Birim anlık bilgisi:** Birim türü adı, kullanım türü, alt kırılım (Birim bazlı / Kontenjan bazlı) ve birim alanı (P) sözleşmeye kopyalanır ve sözleşme ekranında gösterilir. Kontenjan kopyalanmaz.
  - **Ne zaman yazılır:** Oluşturmada ve her taslak düzenlemesinde güncel birimden yeniden yazılır (taslakta birim değişebilir, [LeaseService.cs:681](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L681)). Sözleşme onaylanıp aktifleşince dondurulur, bir daha değişmez.
  - **Mevcut sözleşmeler:** Migration'da bugünkü birim/tür değerleriyle doldurulur (veri dummy).
  - Sonradan tür adı, türün özellikleri ya da birimin türü değişse bile sözleşme ilk halini gösterir. Örnek: "70 m² · Ortak Ofis (Kiralanabilir · Kontenjan bazlı) · kiralanan 8 m²".
- `LeaseGroup`: Id, UnitId, PreviousGroupId (nullable, self-FK). Paylaşımlı grup içindir; tarife tutmaz. Yeniden yapılanmada yeni grup eski grubu `PreviousGroupId` ile bilir (örn. #15.PreviousGroupId = 12); aktivasyon ön koşulu eski grubun kiracılarına bu bağla bakar.
- `LeaseArea` (ortak, 1:1 opsiyonel): LeaseId, RentedArea, AllocatedArea.
  - Satır yalnız paylaşımlı ve açık sözleşmelerde açılır; normal sözleşmede satır olmaz.
  - Paylaşımlıda iki değer eşittir.
- `UnitType`: `RentalMode` (Birim bazlı / Kontenjan bazlı). Kullanım türü (`Usage`) enum'una yeni değer eklenmez; ayrı alan tutulur. Gerekçe: kodda 12 dosyada `Usage == Rentable` kontrolü var.
- `Unit`: `Capacity` (güncel kontenjan). Yalnız birim türü Kontenjan bazlı ise dolu ve zorunludur.
- `UnitCapacityHistory`: UnitId, Capacity, EffectiveFrom. Her K değişikliğinde satır eklenir; geçmiş dönem hesapları buradan okunur.
- **Birim alanı (`Unit.Area`) kilidi:** Açık sözleşmesi (aktif, gelecek tarihli, Taslak, Revizyon İstendi) olan hiçbir birimin alanı düzenlenemez (normal birimler dahil). Örnek: P=60 birimde A 30 + B 30 taslak grup varken alan 65 yapılamaz. Tek birimli taşınmazda alan taşınmazın kapalı/açık alanından hesaplandığı için kilit bu iki alanı da kapsar. Kilit `Property/Edit` akışında uygulanır.
- **Tek birimli taşınmaz:** Birimi paylaşımlı veya kontenjan bazlı olabilir. Kontenjan bazlı türde K, taşınmaz formunun tek birim bölümünde girilir; P taşınmazın kapalı + açık alanıdır. (2026-11-17 kullanıcı kararıyla "kontenjan bazlı olamaz" kuralı kaldırıldı.)
- **Kontenjan girilmemiş birim:** Tür kontenjan bazlıya çevrilince birimlerin K'sı veritabanında boş kalır, taşınmaz Edit ekranında 0 görünür; kayıt için K ≥ 1 girilmelidir.
- Genel parametre: min. kullanım m².
- **Çarpan formülleri:**
  - M2 yöntemi: `LeaseArea.RentedArea` varsa o, yoksa `Unit.Area`.
  - Fixed yöntemi: Tarife sözleşme tarifesinden geliyorsa (`RateSnapshot` kaynağı `LineItemSourceType.LeaseRateOverride`) 1. Üst katmandan geliyorsa `LeaseArea` varsa `RentedArea / Unit.Area`, yoksa 1.
  - Tarife öncelik zinciri değişmez: sözleşme → birim → taşınmaz → genel. Her sözleşmenin sözleşme tarifesi kendine aittir.

## Etkilenecek alanlar (uygulama aşamasında)
- Sözleşme çakışma kontrolü, birim türünün alt kırılımına göre kapasite kontrolüne dönüşecek; tüm birimlerde Taslak ve Revizyon İstendi de sayılacak (`HasOverlappingActiveLeaseAsync`).
- Sözleşme formu birim listesi: boş kontenjanı olan kontenjan bazlı birimler.
- Sözleşme ekranları (iç ve kiracı): birim anlık bilgisinin gösterimi.
- `UnitTypeService` ve birim türü ekranı: alt kırılım alanı, özellik kilidi; birim türü değişikliğinde açık sözleşme kontrolü.
- `LeaseRatePolicy` ve `ChargeGenerationService`: m² çarpanı ve sabit kalem oranı.
- `LeaseService.EnsureRateOverridesAsync`: sözleşme tarifesi doğrulamasında birim alanı yerine kiralanan m².
- `LeaseRepository.HasOpenApplicationForUnitAsync`: birim türüne göre uyarlama (normal aynen, paylaşımlıda grup tek başvuru, kontenjan bazlıda uygulanmaz).
- `LeaseService.EnsureNotSelfReview` ve `LeaseController` `CanApprove/CanRequestRevision/CanDelete`: görev ayrılığının kaldırılması.
- `PropertyService` / `Property/Edit`: kontenjan alanı, birim alanı kilidi (tek birimli taşınmazda kapalı/açık alan dahil), birim türü değişikliği kontrolü.
- `StatisticsService` ve doluluk sorguları: kısmi doluluk (m² ve kontenjan).
- Sözleşme oluşturma, uzatma ve fesih akışları; paylaşımlı grup ekranı (yeniden yapılanma dahil).
- Genel parametreler (`AdminSystemSetting`): min. kullanım m².
- EF migration: yeni tablolar ve kolonlar.

## Doluluk
Doluluk değerleri yalnız bugün aktif olan sözleşmelerden hesaplanır.

**Paylaşımlı birim**
- Bugünkü hesapla aynı şekilde çalışır. Birim ya tamamen doludur ya tamamen boş, çünkü Σ m² = P ve sözleşme tarihleri ortaktır.
- Adet oranına 1, m² oranına P olarak girer.

**Açık birim — durum**
- Üç durum vardır:
  - **Boş:** Hiç sözleşme yok.
  - **Kısmen Dolu:** Yeni durum. En az bir sözleşme var ama kontenjan dolmamış.
  - **Dolu:** Tüm kontenjanlar dolu. Kalan fiziksel m² olsa da durum değişmez.
- Rozetin yanında "6/10 kişi · 42/70 m²" gibi bir özet gösterilir.
- **Süresi dolmak üzere:** Durumu değiştirmez. Ayrı bir işaret olarak gösterilir (örneğin "2 sözleşme 30 gün içinde bitiyor").

**Açık birim — oranlar**
- **Adet oranı:** Birim kesirli sayılır (dolu kontenjan / K). Örneğin 6/10 dolu birim 0,6 sayılır.
- **m² oranı:** Kiralı alan olarak Σ fiziksel m² kullanılır; oran %100'ü geçmez.

**Ana sayfa**
- Adet ve m² oranlarının yanına açık birimler için ayrı bir kontenjan göstergesi eklenir: toplam dolu kontenjan / toplam kontenjan.
- **Taşınmaz kırılımı** (`PropertyOccupancies`): Ayrı bir "Kısmen dolu" sayacı eklenir: Dolu · Kısmen dolu · Dolmak üzere · Boş. Örnek: 3 dolu normal birim + 1 açık birim (K=10, 6 dolu) → Dolu 3 · Kısmen dolu 1 · Dolmak üzere 0 · Boş 0.
  - Açık birim "Dolmak üzere" sayacına girmez; yalnız Dolu / Kısmen dolu / Boş sayacına girer. Örnek: O-1 10/10 dolu ve 2 sözleşmesi 30 gün içinde bitiyor → Dolu sayacında sayılır. "Dolmak üzere" yalnız normal ve paylaşımlı birimler içindir.

**Etkilenen kod**
- `OccupancyStatus` enum'u ve `LeaseOccupancyPolicy`.
- `HomeController` oran hesapları ve `DashboardViewModel`.
- `PropertyRepository` ve `UnitRepository` durum ve sayım sorguları.
- `_DurumBadge`, `Property/Details` ve ilgili DTO'lar.

## Rezervasyon ilişkisi
- Paylaşımlı ve açık yapı yalnız kullanım türü **Kiralanabilir** olan birimlerde kullanılır.
- Rezerve edilebilir ve kiralanamaz birimler bu çalışmadan etkilenmez. Mevcut ayrım korunur ([ReservationUnitPolicy.cs](../src/KiraTakip.Domain/Reservations/ReservationUnitPolicy.cs)).
- Açık birimin boş kontenjanları rezervasyona açılmaz; hot-desk yok. Açık birim yalnız sözleşmeyle kiralanır.

## Birim türü ve alt kırılım
- Sistem kaydı birim türleri (ör. sabit "Ofis" / "Açık Ofis") yoktur. Tüm türler kullanıcı tanımlıdır.
- **Alt kırılım** (`UnitType.RentalMode`): Birim bazlı / Kontenjan bazlı. Birim türü ekranında yalnız kullanım türü Kiralanabilir seçiliyken görünür. Türü Kontenjan bazlı olan tüm birimler açık birimdir. Kontenjan miktarı birim tanımında girilir.
- **Kullanım türü Kiralanabilir dışına çevrilirse** alt kırılım kendiliğinden Birim bazlı olur ve birimlerin kontenjanları temizlenir. Bu değişiklik yalnız aşağıdaki kilit izin veriyorsa yapılabilir.
- **Özellik kilidi:** Türün herhangi bir biriminde, aktif ya da bitmiş fark etmeksizin tek bir sözleşme bile varsa kullanım türü ve alt kırılım değiştirilemez. Farklı özellik gerekiyorsa yeni tür oluşturulur. Örnek: "Atölye" türündeki Birim 12'nin 2025'te bitmiş bir sözleşmesi var → "Atölye" Rezerve edilebilir yapılamaz.
- **Alt kırılım değişikliği** (kilit yoksa): Kontenjan bazlıya geçilirse o türdeki her birime kontenjan girilmesi gerekir. Birim bazlıya geçilirse kontenjan değerleri temizlenir.
- **Tek birimin türü değiştirilirse:** Birimde açık sözleşme yoksa izin verilir. Bitmiş sözleşmeler engel değildir, çünkü birim anlık bilgisini kendileri taşır (bkz. Veri modeli / `Lease`). Örnek: Birim 12'nin yalnız bitmiş sözleşmesi var → "Ortak Ofis" türüne taşınabilir; eski sözleşme "Atölye (Kiralanabilir · Birim bazlı)" göstermeye devam eder.

## Kiracı paneli
- **Gösterilen m²:** Kiracı birimin alanını değil, kendi kiraladığı m²'yi görür.
  - Bugün detay ekranı `UnitArea` gösteriyor ([TenantLease/Details.cshtml:125](../src/KiraTakip.Web/Views/TenantLease/Details.cshtml#L125), [TenantLease/Details.cshtml:426](../src/KiraTakip.Web/Views/TenantLease/Details.cshtml#L426)); bu değişecek.
  - Açık birimde fiziksel m² kiracıya gösterilmez.
  - **Açık birim kiracısı** yalnız kiraladığı m²'yi görür (örn. "8 m²"); birimin toplam alanı, ortak alan olduğu bilgisi, kontenjan ve diğer kiracılar gösterilmez.
- **Paylaşım bilgisi (yalnız paylaşımlı birim):** Yalnız paylaşım bilgisi gösterilir, örneğin "60 m²'lik birimin 20 m²'si, paylaşımlı". Diğer kiracıların adları ve payları gizli kalır. Birim alanı (60 m²) sözleşmedeki birim anlık bilgisinden okunur.
- **Rozet:** "Süresi Dolmak Üzere" rozeti birimin durumundan değil, sözleşmenin kendi bitiş tarihinden belirlenir. Bugün `Model.UnitStatus` kullanılıyor ([TenantLease/Details.cshtml:22](../src/KiraTakip.Web/Views/TenantLease/Details.cshtml#L22)).
- **Grup onayı beklenirken:** Kiracı fark görmez. Sözleşme, grup aktifleşene kadar taslak olarak kalır.
- **Yeniden yapılanma:** Eski sözleşme kiracıya normal bir fesih gibi görünür. Yeni sözleşme ayrıca listelenir.

## Yetkiler
Proje kuralı (`CLAUDE.md`, `refactor-permission-hiyerarsisi.md` §1.3): State değiştiren her POST endpoint, endpoint'in adıyla ayrı bir 3. seviye izinle korunur. Mevcut izinler birleştirilerek yeni bir endpoint korunmaz.

| İşlem | Endpoint | Yetki |
|---|---|---|
| Paylaşımlı grup oluşturma | Yeni `Lease/CreateGroup` | `Internal.Lease.CreateGroup` |
| Grup onayı | Yeni `Lease/ApproveGroup` | `Internal.Lease.ApproveGroup` |
| Grup uzatma | Yeni `Lease/ExtendGroup` | `Internal.Lease.ExtendGroup` |
| Grup fesih | Yeni `Lease/TerminateGroup` | `Internal.Lease.TerminateGroup` |
| Grup taslağını düzenleme | Yeni `Lease/UpdateGroup` | `Internal.Lease.UpdateGroup` |
| Revizyon sonrası grubu yeniden gönderme | Yeni `Lease/ResubmitGroup` | `Internal.Lease.ResubmitGroup` |
| Grup taslağını silme | Yeni `Lease/DeleteGroupDraft` | `Internal.Lease.DeleteGroupDraft` |
| Gruptaki sözleşmeye belge yükleme | Mevcut `Lease/UploadDraftDocument` | Mevcut izni (`Internal.Lease.Create`) |
| Yeniden yapılanma | Yeni `Lease/RestructureGroup` | `Internal.Lease.RestructureGroup` |
| Gruptaki tek sözleşmeye revizyon | Mevcut `Lease/RequestRevision` | `Internal.Lease.RequestRevision` |
| Açık birimde sözleşme ve fiziksel m² | Mevcut `Lease/Create` (forma alan eklenir) | `Internal.Lease.Create` |
| Kontenjan değişikliği | Mevcut `Property/Edit` (birim satırına alan eklenir) | `Internal.Property.Edit` |
| Birim türü alt kırılımı | Mevcut `AdminUnitType/Create` ve `AdminUnitType/Edit` | `Internal.UnitType.Create` / `Internal.UnitType.Edit` |
| Min. kullanım m² parametresi | Mevcut `AdminSystemSetting` ekranı | `Internal.Parameter.Edit` |

- **Görev ayrılığı kaldırılır (grup ve tekil):** Oluşturma ve onay iki ayrı izindir; ikisine de sahip olan kullanıcı kendi oluşturduğu sözleşmeyi/grubu onaylayabilir ve revizyon isteyebilir.
  - Grup: `CreateGroup` + `ApproveGroup` izinleri.
  - Tekil: `Lease.Create` + `Lease.Approve` / `Lease.RequestRevision` / `Lease.DeleteDraft`. Bugünkü `EnsureNotSelfReview` kontrolü üç işlemde de kaldırılır: revizyon ([LeaseService.cs:223](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L223)), silme ([:245](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L245)), onay ([:274](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L274)). Ekrandaki `!isOwner` koşulları da kaldırılır ([LeaseController.cs:231-233](../src/KiraTakip.Web/Controllers/LeaseController.cs#L231-L233)). Bu, mevcut davranış değişikliğidir.
  - Grup taslağını silme (`DeleteGroupDraft`) için de aynı kural geçerlidir: izni olan kendi grubunu silebilir.
- Yeni izinler `PermissionCatalog.cs`, rol varsayılanları ve UI yetki ağacına eklenir.
- Kapsam (row-level) kuralları değişmez; gruptaki tüm sözleşmeler aynı birimdedir.

## Kapsam dışı (bu çalışmada yapılmaz)
- Depozito iade özelliği (yalnız dummy servis arayüzü; bkz. Yeniden yapılanma).
- Kontenjan tarihçesini okuyan geçmiş dönem raporu ya da ekranı.
- Kontenjan bazlı birimin boş kontenjanlarının rezervasyona açılması (hot-desk).
- Normal birimlerde sözleşme formunun dolu birimleri göstermemesi (`PROGRESS.md`'de bilinçli bırakılan; yalnız kontenjan bazlı birimler için liste kuralı değişir).
- `Ended` sözleşme uzatmasında arada başka bir `Ended` sözleşmenin döneminin çakışma kontrolüne girmemesi (`PROGRESS.md`'de bilinçli bırakılan).
- Mevcut `UpdateDraft` / `ResubmitRevision` / `UploadDraftDocument` endpoint'lerinin `Lease.Create` iznini paylaşması (proje kuralına aykırı ama mevcut durum; bu çalışmada düzeltilmez).

## Açık noktalar
Tüm açık noktalar 2026-11-17'de kullanıcıyla kararlaştırıldı ve ilgili bölümlere işlendi (ikinci tutarlılık turu dahil).

## Sonraki adım
Kapandı (2026-11-18). [Uygulama planı](refactor-paylasimli-acik-birim-uygulama-plani.md) F1–F9 ile uygulandı. Kalan açık notlar planın F9 bölümünde.

## Doğrulama
- Bu aşamada kod yok. Doğrulama, dokümanın kullanıcı tarafından gözden geçirilmesidir.
- Uygulama aşamasında xUnit testleri şunları kapsamalı:
  - kapasite kuralları (en yoğun gün, k, son kontenjan, K artışı/azaltışı);
  - paylaşımlı grup aktivasyonu ve yeniden yapılanma ön koşulu;
  - m² çarpanı ve oran hesapları;
  - tüm birimlerde taslağın çakışmaya girmesi;
  - birim türü özellik kilidi ve tek birim türü değişikliği;
  - sözleşmede birim anlık bilgisinin korunması (taslakta güncellenir, onayda donar);
  - birim başına tek açık başvuru kuralının üç birim türündeki davranışı;
  - görev ayrılığının kaldırılması (tekil onay/revizyon/silme ve grup);
  - birim alanı kilidinin taslak dahil açık sözleşmelerde ve tek birimli taşınmazın kapalı/açık alanında çalışması.
