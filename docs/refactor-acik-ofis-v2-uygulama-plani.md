# Açık Ofis v2 — Aşama 1 Uygulama Planı

**Durum:** Onaylandı (2026-11-18). G1–G8 tamamlandı (Aşama 1 bitti).
**Kaynaklar:** `Downloads/teknopark_acik_ofis_v2_karar_ozeti_ve_claude_devir_dokumani.md` (esas belge), `Downloads/teknopark_acik_ofis_v2_kod_incelemesi.md` (kod incelemesi; satır kanıtları orada).
**Kapsam:** Yalnız `CapacityBased` (açık ofis) birimler. Aşama 2 (optimizasyon önerisi) ve Aşama 3 (geçmiş doluluk raporu) kapsam dışı.

## 0. Kesinleşmiş kararlar

- Kontenjan = sözleşme sayısı. `R ≥ Min`, `Min ≤ F ≤ R` (normdan bağımsız), `C(t) ≤ K`, `ΣF(t) ≤ TA`, `K ≤ floor(TA/Min)` sert kurallar.
- Mod (`Automatic` / `Manual`) yönetici seçer ve saklanır. Otomatik F = `min(R, floor_0.01(TA/K))`. Otomatik F elle değiştirilirse mod `Manual` olur.
- "Boş kontenjan başına alan" (eski Kontrol 3) sert engel değil, **uyarı**: norm hedefi ve minimum hedefi.
- K değişikliği yalnız bugünden geçerli. `UnitCapacityHistory` korunur.
- F tarihçesi ayrı tabloda tutulur. Mevcut sözleşmeler `Manual` başlar.
- Onayda otomatik F güncel değerlerle yeniden hesaplanır ve yazılır.
- Kontenjan azaltımında önizleme + sert kayıt engeli + aynı ekranda manuel F düzeltme. `C>K` ve `ΣF>TA` ayrı hatalar.
- Min artışı (2026-11-18 kararı): aktif sözleşme veya ofis Kmax ihlali varsa ayar kaydedilmez. Uyumsuz taslaklar yalnız uyarı listesinde gösterilir; onayları zaten güncel Min ile reddedilir.
- Kapasiteyi etkileyen tüm yazmalar aynı birim için `sp_getapplock` ile seri.

## 0.1 Onayda teyit edilecek varsayımlar

1. **Kontenjan değişikliği ayrı ekrana taşınır.** Mevcut birimin kontenjanı Property düzenleme formunda salt okunur olur; "Kontenjanı değiştir" ekranı birim satırından açılır. Yeni birim oluştururken kontenjan formda kalır. Gerekçe: önizleme + F düzeltme + ayrı izin aynı formda birleşemez.
2. **Kapasite kontrolünün başlangıç günü `max(sözleşme başı, bugün)`.** Geçmiş günler güncel K ile yeniden ölçülmez.
3. **Kontenjan kaydında taslak ve revizyondaki otomatik F'ler de güncellenir** (tarihçe yazılmaz; tarihçe yalnız Aktif sözleşmede). Onaydaki yeniden hesap ikinci güvence.
4. **LeaseArea'sı olmayan açık kontenjan sözleşmesi** her sorguda F=0 ile sayılır (kontenjan tüketir). Ayrıca G2'de dummy veride böyle kayıt olup olmadığı raporlanır.
5. **Kilit kaynakları:** birim için `KiraTakip.Capacity.Unit.{id}` (Exclusive), Min ayarı için `KiraTakip.Capacity.Global` (birim işlemleri Shared, Min kaydı Exclusive). Rezervasyon kilidinden ayrı.
6. **Uyarılar** sözleşme formunda (kayıt sonrası bilgi mesajı) ve kontenjan ekranında gösterilir, yalnız **bugün** için hesaplanır.
7. **Paylaşımlı grup sözleşmeleri** (UnitBased) mod kolonunu `Manual` alır; otomatik hesap onlara uygulanmaz.

## 1. Yeni izinler

| İzin | Endpoint |
|---|---|
| `Internal.Unit.UpdateCapacity` | `POST UnitCapacity/Apply` |
| `Internal.Lease.UpdateAllocatedArea` | `POST Lease/UpdateAllocatedArea` |

Önizleme (`POST UnitCapacity/Preview`) state değiştirmez; `Unit.Module` ile korunur. Min ayarı mevcut `Parameter.Edit` ile kalır. `docs/permission-catalog.md` ve rol seed'i güncellenir.

## 2. Fazlar

Her fazda 1 yazma ajanı (`general-purpose` / sonnet) + 1 denetçi (`orch-reviewer` / opus). Ajansız uygulanabilecek kadar küçük faz yok. Her faz sonunda kısa durum yazılır, kullanıcıdan "devam" beklenir. Commit kullanıcı isteyince.

### G1 — Domain politikası (DB'siz) — ✅ Tamamlandı (2026-11-18)
> Uygulanan: yeni API eski metotların **yanına** eklendi (eski metotlar `[Obsolete]`'suz duruyor; G4/G5'te çağıranlar geçince silinecek). Eski kontrolün yerine `GetHardViolations`, uyarılar için `GetSnapshot` (`CapacitySnapshot.NormTargetShort/MinimumTargetShort`), `GetCapacityLimit` = Kmax. `CapacityOccupant` 6 parametreli + EF expression tree uyumu için ayrı 3 parametreli constructor. `UnitCapacityPolicyTests` 44/44.

- `Dom/Leases/UnitCapacityPolicy.cs`:
  - Ekle: `CalculateEffectiveNorm`, `CalculateAutomaticArea`, `GetMaximumCapacity(TA, Min)`, `ValidateArea(R, F, Min)`, `GetWarnings`.
  - `GetViolations` yalnız `CapacityExceeded`, `AreaExceeded` döndürür.
  - Kaldır: `NormBelowMinimum`, `GetAllocatedAreaRange`, `IsAllocatedAreaAllowed`, `SuggestLastSlotArea`, eski `GetMaximumCapacity`. Çağıranlar G4/G5'te uyarlanır; G1'de derleme kırılmasın diye eski metotlar `[Obsolete]` ile G5 sonuna kadar kalabilir.
- `CapacityOccupant`: `LeaseId`, `RentedArea`, `Mode` alanları.
- Yeni enum `LeaseAreaMode { Automatic = 1, Manual = 2 }`.
- Test: `Tst/Unit/Leases/UnitCapacityPolicyTests.cs` yeniden yazılır. T04, T05, T06, T07, T16 ve T02/T14 uyarıları, 70/3 = 23,33.

### G2 — Şema ve migration (tek yazma, başka yazma yok) — ✅ Tamamlandı (2026-11-18)
> Uygulanan: migration `20261118105247_AddLeaseAreaModeAndHistory`; yalnız `KiraTakipDb_Test`'e uygulandı (`KiraTakipDb`'ye uygulanmadı). Tarihçe tablosu `SozlesmeAlanGecmisleri`; kim/ne zaman için `CreatedAt/CreatedBy` kullanılır, ek kolon yok. Neden enum'u `LeaseAreaChangeReason` (+ `Migration = 4`, mevcut aktif açık ofis sözleşmeleri için başlangıç satırı). Test DB'de açık ofis sözleşmesi yok → başlangıç satırı 0; INSERT gerçek veriyle denenmedi. G3 notu: `LeaseAreaHistory.Mode/Reason` için varsayılan değer yok, yazan kod her zaman set etmeli.

- `LeaseArea.AllocationMode` (NOT NULL, default `Manual`).
- Yeni `LeaseAreaHistory`: `Id, LeaseAreaId (FK Restrict), AllocatedArea decimal(18,2), Mode, EffectiveFrom (date), ChangedAt, ChangedByUserId, Reason (enum: Activation, CapacityChange, ManualEdit)`. Indeks `(LeaseAreaId, EffectiveFrom)`. Türkçe tablo/kolon adları mevcut konvansiyonla.
- Migration `AddLeaseAreaModeAndHistory`, `dotnet ef database update` (dev + `KiraTakipDb_Test`).
- Dummy veride LeaseArea'sız açık `CapacityBased` sözleşme sorgusu çalıştırılıp sonuç raporlanır (veri değiştirilmez).

### G3 — Repository altyapısı — ✅ Tamamlandı (2026-11-18)
> Uygulanan: `Inf/.../Leases/CapacityOccupantQuery.cs` (ortak sorgu; `IgnoreQueryFilters` + `!IsDeleted`, çünkü Lease filtresi kiracı kullanıcısına yalnız kendi sözleşmelerini gösterir). `GetCapacityOccupantsAsync` artık LeaseArea'sız açık sözleşmeyi de sayar. Kilitler `IUnitRepository.AcquireCapacityLocksAsync(unitId)` (global Shared → birim Exclusive) ve `AcquireGlobalCapacityLockExclusiveAsync()`. Tarihçe `ILeaseRepository.AddLeaseAreaHistory` / `GetLeaseAreaHistoryAsync`. `UpsertLeaseAreaAsync(..., mode)` → `Task<LeaseArea>`; mevcut çağıranlar `Manual` geçiyor (G4'te gerçek mod). LeaseArea hard-delete edildiği için projeksiyonda `IsDeleted` koşulu yok. İlgili testler 158/158. Kilidin gerçekten bloklaması G8'de test edilecek.

- Ortak occupant sorgusu: `GetOpenLeaseAreasForUnitAsync` ve `UnitRepository.GetCapacityOccupantsAsync` aynı statü, aynı `.Date` karşılaştırması, LeaseArea'sız kayıt F=0 ile. Tek private sorgu kurucu.
- `IUnitRepository.AcquireCapacityLockAsync(unitId)` ve `AcquireGlobalCapacityLockAsync(exclusive)` (`ReservationRepository.AcquireUnitDecisionLockAsync` deseni).
- `ILeaseAreaHistoryRepository` (veya `LeaseRepository` içinde) `AddHistory`, `GetHistoryAsync(leaseId)`.
- `UpsertLeaseAreaAsync` mod parametresi alır.
- Test: occupant sorgusu tutarlılığı (LeaseArea'sız kayıt iki yolda da sayılır).

### G4 — Sözleşme akışı — ✅ Tamamlandı (2026-11-18)
> Uygulanan (G4a backend + G4b ekran, sıralı iki yazma): mod `null` → `Automatic`. Kilit `LeaseApplicationRules` içinde occupant okumadan önce alınıyor (Create/UpdateDraft/Resubmit/Activate/Extend hepsi buradan geçer). `Lease.CapacityFreeAreaBelowMinimum` kalktı. Onayda `UpsertLeaseAreaAsync` + `LeaseAreaHistory(Activation, EffectiveFrom=StartDate)`. Uyarı: `ILeaseService.GetCapacityWarningAsync(leaseId)` (kilit/kapsam yok; controller yalnız başarılı kayıt/onay sonrası çağırır) → yeni `FeedbackTempDataKeys.WarningMessage`, `_Layout`'ta amber kutu (`_KiraciLayout` değişmedi). Formda Otomatik F istemcide `min(R, normArea)` gösterilir, gönderilmez. Test harness'e `MinimumUsageArea` eklendi. Testler 393/393 (G4a) ve 441/441 (G4b filtre). Arayüz tarayıcıda denenmedi; tarihçe testi `EffectiveFrom`'u doğrulamıyor.
- DTO/VM: `AllocationMode` alanı (`LeaseQueryDtos`, `LeaseReviewDtos`, `CreateLeaseViewModel`, `LeaseReviewViewModels`, validator'lar).
- `LeaseApplicationRules.ValidateCapacityLeaseAsync`: otomatikse F hesaplanır (kullanıcı girdisi yok sayılır), manuelse `ValidateArea`. Aralık başı `max(start, bugün)`. Uyarılar dönüş değerine eklenir.
- `ActivateAsync`: hesaplanan F + mod yazılır, `LeaseAreaHistory (Activation)` eklenir.
- `EnsureExtensionAllowedAsync`: yeni doğrulama fonksiyonları; F değişmez.
- Kilit: Create, UpdateDraft, Resubmit, Activate, Extend (yalnız `CapacityBased`); birim kilidinden önce global Shared.
- UI: `_LeaseForm.cshtml` + `_LeaseFormScripts.cshtml` mod seçimi; otomatikte F salt okunur önerilen değer, manuelde [Min, R]. Uyarılar TempData bilgi mesajı.
- Test: `CapacityLeaseServiceTests` güncellenir ("norm altında zorunlu atama" ve "minimum kullanım ihlali" senaryoları yeni kurala göre), T05, T06, T10, T11, inceleme §4 madde 5 (Kontrol 3 sözleşmeyi engellemez), `LeaseApprovalServiceTests` onayda F yazımı.

### G5 — Kontenjan değiştirme ekranı — ✅ Tamamlandı (2026-11-18)
> Uygulanan (G5a backend + G5b ekran): `ICapacityPlanService` (`PreviewAsync`/`ApplyAsync`), `IAllocationSuggestionStrategy` → `DefaultAllocationSuggestionStrategy`. Ek hata kodu `Capacity.Invalid` (K≤0). Hash mevcut K, yeni K, TA, Min, satırlar ve override'ları içerir (kültür bağımsız). Apply hataları tek `BusinessValidationException`'da birleşik mesaj. Önizlemede olmayan/LeaseArea'sız satıra gelen override sessizce yok sayılır. Route: `GET /UnitCapacity/{id}`, `POST /UnitCapacity/{id}/Preview` (JSON, `Internal.Unit`), `POST /UnitCapacity/{id}/Apply` (`Internal.Unit.UpdateCapacity`). Property formu: mevcut kontenjanlı birimde K salt okunur + link, gönderilmez; mevcut birimde TA değişirse Kmax + `Property.CapacityAreaExceeded` (açık sözleşmeli birimde alan zaten `UnitAreaLocked`). Seed "Tesis ve Operasyon Sorumlusu" rolüne yeni izin eklendi. Eski domain metotları silindi; `GetPeakUsage`, `CalculateNormArea`, `SuggestLastSlotArea` `UnitRepository` kullandığı için kaldı. Testler 654/654 ve 655/655 (filtreli). Ekran tarayıcıda denenmedi.
- Yeni `CapacityPlanService` (`ITransactionalService`):
  - `PreviewAsync(unitId, newK, overrides)`: sözleşme listesi (kiracı, tarih, statü, R, mod, mevcut F, önerilen F), sert hatalar (`Capacity.CountExceeded` C>K, `Capacity.AreaOverflow` ΣF>TA + m², `Capacity.AboveMaximum` K>floor(TA/Min), `Capacity.AreaOutOfRange` F aralığı), uyarılar.
  - `ApplyAsync(unitId, newK, overrides, expectedHash)`: kilit → yeniden oku → hesapla → hata varsa `BusinessValidationException`; önizleme özeti değiştiyse `Capacity.PreviewOutdated`. Yazım: `Unit.Capacity`, `UnitCapacityHistory`, değişen `LeaseArea` (override edilen otomatikler `Manual` olur), Aktif olanlar için `LeaseAreaHistory (CapacityChange)`.
  - Önerilen F üretimi `IAllocationSuggestionStrategy` arkasında (Aşama 2 için genişleme noktası); tek uygulama "otomatikleri yeniden hesapla, manuelleri koru".
- `UnitCapacityController` (Preview POST, Apply POST) + view; Alpine.js ile satır içi F düzenleme, Kaydet sert hata varken kapalı.
- `PropertyService`: mevcut birimde kontenjan değişikliği kabul edilmez (`Property.CapacityChangeNotAllowed` → ekrana yönlendir); `ValidateCapacityAsync` ve eski Kontrol 3 kaldırılır. `Property/Edit.cshtml` alanı salt okunur + link.
- İzin `Internal.Unit.UpdateCapacity` + catalog + seed.
- Test: `UnitCapacityChangeTests` yeniden yazılır. T01, T02, T03, T08, T09, T17, T18; önizleme bayatlaması.

### G6 — Manuel F düzenleme (sözleşme detayı) — ✅ Tamamlandı (2026-11-18)
> Uygulanan: `ICapacityPlanService.UpdateAllocatedAreaAsync` (kapsam → kilit → yeniden okuma; açık statü Active/Draft/RevisionRequested, diğerleri `Lease.AllocatedAreaNotEditable`; LeaseArea yoksa `Lease.AllocatedAreaMissing`; F 2 haneye yuvarlanır, aynı değer no-op; tarihçe yalnız Aktifte, `EffectiveFrom = max(StartDate, bugün)`). Okuma ayrı: `GetAllocatedAreaInfoAsync` → `LeaseAllocatedAreaInfoDto`; kiracıyla ortak `LeaseDetailDto`'ya fiziksel alan eklenmedi (mimari test kuralı). `POST Lease/UpdateAllocatedArea` + `Internal.Lease.UpdateAllocatedArea` (seed: Lease.Edit'e sahip "Tesis ve Operasyon Sorumlusu"). Details'te "Fiziksel Alan" kartı + modal; Details taslakları Draft sayfasına yönlendirdiği için ekrandan pratikte yalnız Aktif sözleşme düzenlenir (servis taslakta da çalışır). Testler `LeaseAllocatedAreaUpdateTests` 13/13; filtreli 298/298. Ekran tarayıcıda denenmedi.
- `CapacityPlanService.UpdateAllocatedAreaAsync(leaseId, F)`: kilit, `ValidateArea`, kapasite kontrolü, mod `Manual`, Aktifse tarihçe (`ManualEdit`).
- `POST Lease/UpdateAllocatedArea` + izin `Internal.Lease.UpdateAllocatedArea`.
- `Lease/Details.cshtml`: F, mod, F tarihçesi tablosu, yetkiliyse düzenleme modalı. Kiracı paneli değişmez (F gösterilmez).
- Test: T03, T13, T18 (detaydan), Min altı ret, ΣF>TA ret.

### G7 — Min ayarı etki analizi — ✅ Tamamlandı (2026-11-18)
> Uygulanan: `SystemSettingService` `ITransactionalService` YAPILMADI (proxy, policy provider `RefreshAsync`'i de transaction içine alır; provider ayrı bağlantıyla okuduğu için kilitli satırda bloklanırdı). Yerine `IUnitOfWork.ExecuteInTransactionAsync` içinde global Exclusive kilit + kontrol + kayıt; cache yenileme commit sonrası. Yeni okuma `IUnitRepository.GetCapacityUnitsWithOpenLeasesAsync(bugün)` (açık statüler + `EndDate ≥ bugün`; LeaseArea yoksa R=F=0; K null birim Kmax dışı). Ret kodu `SystemSettingCodes.MinimumUsageAreaConflict`, mesaj satır satır liste; Edit ekranında inline (merkezi modal kapalı). Uyarılar `SystemSettingUpdateResult.Warnings` → Index'te amber kutu (" | " ile birleşik). Bilinen dar pencereler: eski Min kilitten önce okunur; commit ile cache yenileme arası. Testler `MinimumUsageAreaChangeTests` 6/6, filtreli 126/126. Test edilmeyen durumlar: RevisionRequested uyarısı, Ended, silinmiş kayıt, K null, LeaseArea yok, eşit değer. Ekran tarayıcıda denenmedi.
- `SystemSettingService` → `ITransactionalService`. `Lease.MinimumUsageArea` artıyorsa: global kilit Exclusive → tüm açık `CapacityBased` Aktif sözleşmelerde `R ≥ Min`, `F ≥ Min`; her `CapacityBased` birimde `K ≤ floor(TA/Min)`. İhlal varsa `Setting.MinimumUsageAreaConflict` + birim/sözleşme listesi. Taslak/revizyon ihlalleri kayıt sonrası uyarı listesi.
- Admin ayar ekranında liste gösterimi.
- Test: T12 (aktif → ret; yalnız taslak → kayıt + uyarı), Kmax ihlali → ret, Min azaltma → kontrolsüz kayıt.

### G8 — Eşzamanlılık ve kapanış — ✅ Tamamlandı (2026-11-18)
> Uygulanan: `tests/.../Integration/Leases/CapacityConcurrencyTests.cs` (6 test; gerçek commit eden ayrı context'ler, `ProxyGenerator` + `TransactionInterceptor` ile üretimdeki transaction; verisini `finally`'de siler). T15 paralel + elle tutulan kilitle deterministik bloklama; kontenjan düşürme–sözleşme açma yarışı (Apply tarafında `CountExceeded` veya `PreviewOutdated` kabul); gerçek DI ile `ILeaseService`/`ICapacityPlanService` Castle proxy doğrulaması. **Bulunan hata (TOCTOU) ve düzeltme:** `LeaseApplicationRules` birimin K/TA'sını kilitten önce okuyordu; kilitte beklerken K düşerse sözleşme eski K ile geçiyordu (K=2 birimde 3 sözleşme). Artık `ValidateCapacityLeaseAsync` ve `EnsureExtensionAllowedAsync` önce kilidi alıp `GetLeaseContextAsync` ile birimi yeniden okuyor. Tam paket 1188/1188, build temiz. Açık: Min politika cache'i kilit sonrası yeniden okunmuyor (commit–refresh arası dar pencere, G7 notu). Son denetim (ONAY) notları: `PropertyService` TA değişikliği ve yeni birimde Kmax kontrolü kapasite kilidi almıyor (nadir yönetici yarışı; sonraki iş); `ValidatedLeaseApplication` kilit öncesi birim DTO'sunu döndürüyor (snapshot alanları teorik olarak bayat).
- T15: iki ayrı scope/DbContext, paralel `Task`, son kontenjana iki taslak → biri `CapacityExceeded`. Kontenjan düşürme ile sözleşme oluşturma yarışı. Servisin proxy'li (interceptor'lu) kurulduğu doğrulanır; değilse test kendi transaction'ını açar.
- Tüm test paketi, `dotnet build KiraTakip.slnx`.
- `docs/PROGRESS.md`, `docs/permission-catalog.md`, bu plan güncellenir. Son denetim (birleşik değişiklik) `orch-reviewer` ile.

## 3. Senaryo → faz eşlemesi

| Senaryo | Faz |
|---|---|
| T01, T02, T03, T08, T09, T17 | G5 |
| T04, T07, T14, T16 | G1 (+ G5 ekran) |
| T05, T06, T10, T11 | G4 |
| T12 | G7 |
| T13, T18 | G5 + G6 |
| T15 | G8 |

## 4. Riskler

- G1'deki kaldırmalar G4–G5'e kadar derlemeyi kırar → `[Obsolete]` geçişi; G5 sonunda temizlik.
- Kontenjanın Property formundan çıkması mevcut `UnitCapacityChangeTests` ve Property testlerini etkiler.
- Kilitler testlerde transaction yoksa `InvalidOperationException` fırlatır (rezervasyon deseni gibi); servis testleri proxy ile kurulmalı.
- Paratika (Faz 20) değişiklikleriyle dosya çakışması beklenmiyor.

## 5. Kapsam dışı

- Manuel F optimizasyon önerisi (Aşama 2).
- Geçmiş tarihli doluluk raporu (Aşama 3).
- İleri tarihli kontenjan değişikliği, TA(t)/Min(t) sürümleme.
- Doluluk ekranlarının (bugün Aktif sayan) davranışı.
- F9 açık notları 1–3 ve 7–14.
