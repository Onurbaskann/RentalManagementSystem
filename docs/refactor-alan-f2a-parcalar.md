# F2a parça kartları — Taşınmaz/Birim → Alan kesmesi

> Kaynak: [`refactor-alan-hiyerarsisi.md`](refactor-alan-hiyerarsisi.md) §4.3, §6, §7, §11 F2a + P0 envanteri (2026-11-26, 4 okuma ajanı, grep ile doğrulandı).
> Her yazıcı **yalnız kendi kartını** alır; kart dışı dosyaya dokunmaz. Satır numaraları envanter anındakidir; yazıcı önce `Grep` ile yeniden bulur, kaymışsa yeniden bulduğu satırı kullanır.
> Yollar: `App`=`src/KiraTakip.Application`, `Dom`=`src/KiraTakip.Domain`, `Inf`=`src/KiraTakip.Infrastructure`, `Web`=`src/KiraTakip.Web`, `T`=`tests/KiraTakip.Tests`.

## 0. Ortak kurallar (her kart)

- DB yalnız `DBTeknoPark_Test`; `dotnet ef` komutları yalnız P1a ve P7a'da, ana oturum çalıştırır.
- **Kabul ölçüsü (P2–P6):** `dotnet build KiraTakip.slnx 2>&1 | grep -E "error CS"` çıktısı kartın dosya listesine süzülünce **0 satır**. Diğer dosyalardaki hatalar beklenir (sonraki kartlar kapatır).
- Kartta yazmayan iş kuralı icat edilmez. Belirsizlik → dur, soruyu raporla.
- Magic string yok (`PermissionCatalog`, `RoleNames`, `AppClaimTypes`). Yeni write endpoint = yeni izin (CLAUDE.md).
- Testler P7b–P7d'de çevrilir (test projesi src derlenmeden derlenmez). P1 kartları kendi yeni testlerini yazar.
- C# adları İngilizce (`SpaceId`, `Space`), kolon adları Türkçe `[Column("AlanId")]` (F1 deseni: `Dom/Models/Entities/Space.cs`).

## 1. Kararlar (envanter sorularının cevabı)

| # | Soru | Karar | Dayanak |
|---|---|---|---|
| D1 | `RentalModeSnapshot`, `UnitAreaSnapshot`, `UnitType*Snapshot` | Dördü de kalkar (= `AnlikKiralamaBicimi`, `AnlikBirimYuzolcumu`, `AnlikBirimTuruAdi`, `AnlikKullanimTuru`) | spec §4.3 |
| D2 | Çakışma kontrolü (`HasOverlappingActiveLeaseAsync`) | F2a: yalnız **aynı alan** (bugünkü birim davranışı). Ata/torun ve K52 F2b'de `SpaceOccupancyPolicy` ile | spec §11 F2b |
| D3 | `LeaseOccupancyPolicy` | F2a'da `DetermineStatus` alana uyarlanıp kalır (aktif sözleşmesi olan alan = kiralı); `DetermineCapacityStatus` silinir. F2b'de `SpaceOccupancyPolicy` yerini alır | spec §5.2, §7.1 "F2a / F2b" |
| D4 | `LeaseGroup` | `UnitId` → `SpaceId`; grup şartı "birim UnitBased" → **alan `IsShared` + `Usage = Rentable`** | spec §4.3, K27 |
| D5 | `LineItemSourceType.UnitRateOverride` / `PropertyRateOverride` | Tek değer `SpaceRate`'e iner (DB sıfırlanıyor, eski int değer korunmaz) | spec §6.4 |
| D6 | Tarife yıla düşmesi (`BatchRateResolver:108-116`, `RateScheduleRepository:27-32`) | F2a'da **kalır** | spec §11 F3 |
| D7 | `ReservationUnitPolicy` | `ReservationSpacePolicy` olur: alan aktif + `Usage = Reservable` | spec §5.6 satır 274 |
| D8 | Rezervasyon kuralı çözümü | Alanın kendi aktif kuralı, yoksa **en yakın atanın** aktif kuralı; ücret + KDV + `BorcTipiId` birlikte gelir. Kural yoksa rezervasyon yapılamaz (genel/tür-yıl kuralı kalkar) | spec §4.3, §9 satır 486, K28, K69 |
| D9 | `AdminRate` matrisindeki rezervasyon satırı (`RateMatrixReservationRow`, `RateScheduleService:174,230,273`) | Kaldırılır; rezervasyon kuralı yalnız alan ekranında | D8 (tür×yıl kuralı yok) |
| D10 | `PaymentRoutingScope` enum | Silinir; `AlanId == null` = genel, dolu = alan | spec §4.3 |
| D11 | `ReservationCompletionService` | F2a'da değişmez (Unit kullanmıyor); kilit protokolü F2b | envanter |
| D12 | `ApplicationUser.TumTasinmazlaraErisim` | C# `HasAccessToAllSpaces`, kolon `TumAlanlaraErisim`; `Invitation` aynı | spec §4.3 |
| D13 | Kapsam modeli | `UserScopeDto { GlobalAccess, SpaceRootPaths }`, `IsSpaceInScope(path)` = bir kapsam yolu önekse. Repository filtresi `Space.Path.StartsWith(kapsamYol)` OR. Tek `SpaceAccessScope` girdi tipi (S-9). F2a'da "sözleşmenin alanı kapsamdaysa görünür" (tek alanlı → K32 farkı yok); `NOT EXISTS` F4 | spec §6.8 |
| D14 | Alan yolu değişince / alan silinince kapsam | Alan silme kapsam satırlarını siler (§6.2); alan taşınırsa (Path değişir) **tüm** kapsam cache'i temizlenir | spec §6.2, basit ve güvenli |
| D15 | Kiracı kullanıcısının atanabilir alanları | Bugünkü davranış: kiracının sözleşmeli + rezervasyonlu alanları; "tüm alanlara erişim" bayrağı = `HasAccessToAllSpaces` | bugünkü kod korunur |
| D16 | Gösterim "Taşınmaz · Birim" | Alanın ad zinciri (`AnlikYol` sözleşmede, canlı `Space` yolu başka yerde) | spec §4.3 `AnlikYol` |
| D17 | `MinimumUsageArea` sistem ayarı | Kontenjanla birlikte **silinir** (yalnız kontenjan kodu kullanıyor) | K45 — **Onur onayı istenir (S1)** |
| D18 | Ana sayfa doluluk/kapasite kartları | **Onur onayı istenir (S2)** | — |
| D19 | `PropertyReservationDto`, `UnitReservationRateOverrideDto`, `tasinmaz-form.js` | Silinir | envanter (referanssız) |
| D20 | Silinen özelliklerin testleri | Silinir; `LeaseUnitSnapshotTests` → `LeaseArea` anlık alan testine, `UnitReservationRule*` testleri → alan rezervasyon kuralı testine çevrilir | spec §11 F2a kabul |

## 2. Sıra

```
P1a ‖ P1b → P1c → P2 → (P3a ‖ P4) → (P3b ‖ P5) → P6 → P7a → P7b → (P7c ‖ P7d) → son denetim
```

Her kart: 1 yazıcı (sonnet) + 1 denetçi (opus). Paralel kartların dosyaları kesişmez.

---

## P1a — K55 kiracı kategorisi NOT NULL (yeşil kalır)

- **Amaç:** `Tenant.TenantCategoryId` zorunlu, FK Restrict; tüm üretim ve test kodu kategorili kiracı oluşturur.
- **Dosyalar:** `Dom/Models/Entities/Tenant.cs:9-10,50`; `Inf/Persistence/Configurations/Properties/TenantConfiguration.cs:25-27`; `App/Models/DTOs/Tenant/TenantQueryDtos.cs:34,56`; `App/Models/DTOs/TenantDetailsDto.cs:6`; `App/Services/Tenants/TenantService.cs:71-85,129-132,213`; `Web/Models/ViewModels/TenantViewModels.cs:29`; `Web/Controllers/TenantController.cs:192,271,291`; `Web/Views/Tenant/{Create,Edit}.cshtml` (kategori select required); `Inf/.../Tenants/TenantRepository.cs:151,197` (null varsayımı); testlerde kategorisiz `new Tenant` kurulumları (~35 dosya; yalnız kiracı kurulum satırına kategori eklenir, başka satıra dokunulmaz).
- **Yapılmaz:** Tarife/kategori çözümü, başka entity.
- **Migration:** `MakeTenantCategoryRequired` (ana oturum ekler, DB'ye uygular).
- **Kabul:** build temiz; tam test paketi yeşil.

## P1b — Yol tabanlı kapsam altyapısı + `ReservationSpacePolicy` (yeşil kalır, yalnız ekleme)

- **Amaç:** D13'teki kapsam tipleri ve yardımcıları **eski yapının yanına** eklenir; henüz kimse kullanmaz.
- **Yeni:** `App/Models/DTOs/SpaceAccessScope.cs` (S-9); `App/Services/Spaces/SpaceScopeService.cs` + arayüz (`Services/Interfaces/Spaces/`): kullanıcının kapsam alan id'lerinden `SpaceRootPaths` üretir, `IsSpaceInScope(path)`; `ISpaceRepository`'ye "id listesinin yollarını getir" sorgusu; DI `App/DependencyInjection/ApplicationModule.cs`. `Dom/Reservations/ReservationSpacePolicy.cs` (D7; `ReservationUnitPolicy` henüz silinmez).
- **Testler:** `T/Unit/Spaces/SpaceScopeTests.cs` (önek eşleşmesi: `/1/2/` kapsamı `/1/2/5/`'i kapsar, `/1/20/`'yi kapsamaz), `T/Unit/Reservations/ReservationSpacePolicyTests.cs`.
- **Yapılmaz:** Mevcut `UserScopeDto`, `PermissionScope*`, repository filtreleri (P5).
- **Kabul:** build temiz; yeni testler + `T/Unit` yeşil.

## P1c — Ağaç tarife çözücüsü + test tabanı (yeşil kalır, yalnız ekleme)

- **Amaç:** `SpaceRateResolver`: (alan, kiracı kategorisi, borç tipi, dönem) → alanın kendi `AlanTarifeleri` satırı, yoksa en yakın atanınki, yoksa genel tarife (bugünkü yıla düşme dahil, D6). Her ay çözülür (dondurma F3). Ve `SpaceTestBase` / `SpaceTestSeed` test yardımcısı (Space + SpaceType + tarife kurar).
- **Yeni:** `App/Services/Pricing/SpaceRateResolver.cs` + arayüz; DI. `T/Support/Spaces/SpaceTestSeed.cs`.
- **Önce oku:** spec §6.4 (satır 336-350), F1 `SpaceRateService`, `App/Services/Pricing/RateResolverService.cs` ve `BatchRateResolver.cs` (çözüm sırası ve dönüş tipi aynen korunur; yalnız kaynak alan zinciri olur).
- **Testler:** `T/Integration/Pricing/SpaceRateResolverTests.cs`: kendi satırı > ata > genel; kategori eşleşmesi; ata zincirinde ilk bulunan kazanır.
- **Yapılmaz:** `RateResolverService`/`BatchRateResolver`'a dokunmak (P3b).
- **Kabul:** build temiz; yeni testler yeşil.

## P2 — Entity ve konfigürasyon kesmesi (çözüm kırmızıya döner)

- **Amaç:** §4.3 entity değişiklikleri (LeaseRate hariç, F3). Eski entity'ler **henüz silinmez** (P7a); yalnız bağlar kopar.
- **Dosyalar ve değişiklik:**
  - `Dom/.../Lease.cs:11,16-25,67` — `UnitId`, `Unit`, 4 snapshot kalkar (D1); `Areas` (1-N) eklenir, `LeaseArea` 1:1 navigation kalkar.
  - `Dom/.../LeaseArea.cs:16,19` — `AllocatedArea`, `AllocationMode` kalkar; `SpaceId`, `Space`, `SnapshotCode/Name/TypeName/Usage/Area/Path` (`AnlikKod/Ad/TurAdi/Kullanim/Yuzolcumu/Yol`) eklenir.
  - `Inf/.../Leases/LeaseAreaConfiguration.cs:11-18` — 1:1 → 1-N; unique `(SozlesmeId, AlanId)` silinmemişler; `CK_..._Alanlar` kalkar; `KiralananAlan > 0` check.
  - `Inf/.../Leases/LeaseConfiguration.cs:11-14,17,22,23,32` — Unit FK, `IX_Lease_UnitId`, snapshot kolonları kalkar.
  - `Dom/.../LeaseGroup.cs:9,14` + `LeaseGroupConfiguration.cs:10` — `SpaceId` (Restrict).
  - `Dom/.../Charge.cs:16,58` + `ChargeConfiguration.cs:27-29,46` — `SpaceId int?` (`AlanId`), index. Dönem filtresi (`:39-42`) **değişmez** (F3).
  - `Dom/.../Reservation.cs:14,106` + `ReservationConfiguration.cs:31-34,55` — `SpaceId` (Restrict), `ChargeTypeId` (`BorcTipiId`, NOT NULL, Restrict); index `(AlanId, BaslangicTarihi, BitisTarihi, Durum)`.
  - `Dom/.../ReservationRateOverride.cs:9-15,32-33` + `Inf/.../Pricing/ReservationRateOverrideConfiguration.cs:15-29` — `int SpaceId` (Cascade), `UnitTypeId`/`Year` kalkar, `ChargeTypeId` (Restrict, zorunlu); eski unique ve check kalkar; unique `AlanId` silinmemişler.
  - `Dom/.../PaymentStoreRouting.cs:12-22` + `PaymentStoreRoutingConfiguration.cs:16-48` — tek `int? SpaceId`; 2 kısmi unique (spec §4.3); check kalkar. `Dom/Models/Enums/PaymentRoutingScope.cs` silinir (D10); `Dom/Payments/PaymentRoutingPolicy.cs:7-22` tek `spaceId`'ye uyarlanır.
  - `Dom/Models/Enums/ScopeType.cs` — tek `Space = 1`. `Dom/.../UserPermissionScope.cs:12,15` + `UserPermissionScopeConfiguration.cs:13` — `ScopeId` → Space FK Restrict.
  - `Dom/.../Invitation.cs:42-49`, `ApplicationUser.cs:15` — D12.
  - `Dom/Models/Enums/RateHierarchyLayer.cs:5-6`, `LineItemSourceType` (D5); `Enums.cs`'te `LeaseAreaMode`, `LeaseAreaChangeReason` silinir (`UnitStructure`, `UnitRentalMode`, `UnitTypeUsage` P7a'da, Unit'le birlikte).
  - `Inf/Persistence/ApplicationDbContext.cs` — `LeaseAreaHistories`, `UnitCapacityHistories` DbSet'leri; `LeaseAreaHistory.cs`, `UnitCapacityHistory.cs` ve konfigürasyonları **silinir** (yalnız bunlar).
- **Yapılmaz:** `LeaseRateOverride` (F3); servis/repository/controller (sonraki kartlar); migration.
- **Kabul:** `src/KiraTakip.Domain` 0 hata; `Inf/Persistence/Configurations/**` ve `ApplicationDbContext.cs` 0 hata.

## P3a — Sözleşme (P4 ile paralel)

- **Amaç:** Sözleşme oluşturma/taslak/onay/uzatma/grup akışı tek alanla çalışır; kontenjan kodu çağrılmaz.
- **Dosyalar:** `App/Services/Leases/LeaseService.cs` (`:38` repo; `:79-252` CreateDraft/UpdateDraft; `:342,400,435,468` kapsam; `:476` m²; `:557`; `:597-616` ApplyApplicationChanges; `:716-718`), `LeaseApplicationRules.cs` (`:26`, `:65,473-482`, `:81-82`, `:121-125` tür kuralı kalkar, `:135-176`, `:187-251` kontenjan metotları **silinir**, `:318-357`), `ILeaseApplicationRules.cs`, `ILeaseService.cs` (`UpdateAllocatedAreaInput` silinir), `LeaseGroupService.cs` (D4; `:31-625` envanter satırları), `App/Repositories/Interfaces/Leases/{ILeaseRepository,ILeaseGroupRepository}.cs`, `Inf/.../Leases/LeaseRepository.cs` (tüm `Unit/Property` Include/Select; `:744`, `:760`, `:801`, `:809` **silinir**; `:830`, `:856` N satıra; kapsam filtreleri `SpaceAccessScope` ile — P1b), `LeaseGroupRepository.cs`, `Inf/.../Leases/CapacityOccupantQuery.cs` (**silinir**), Lease DTO'ları (`LeaseQueryDtos`, `LeaseReviewDtos`, `LeaseGroupDtos`, `LeaseDetailDto`, `LeaseListItemDto`, `LeaseDropdownDto`; `LeaseCapacityWarningDto` **silinir**), `Web/Controllers/LeaseController.cs` + `LeaseController.Group.cs` (`Create(int? unitId)` → `spaceId`; `UpdateAllocatedArea` + `PermissionCatalog.Lease.UpdateAllocatedArea` kullanımı **silinir**; `capacityPlanService` çağrısı kalkar), Lease ViewModel/Validator'ları, `Web/Views/Lease/*` (envanter satırları), `_LeaseUnitPicker*` → `_LeaseSpaceSelect.cshtml` (**düz liste**: kiralanabilir, aktif alanlar, `Path` ile gösterim; ağaç seçici F2b).
- **Kurallar:** tek alan zorunlu (form ve servis); `LeaseArea` satırına anlık alanlar onayda yazılır (bugün snapshot'ın yazıldığı yer); `RentedArea` boşsa alan m²'si; D2 çakışma; paylaşımlı alan yalnız grup akışında (K27).
- **Yapılmaz:** Tahakkuk/tarife kodu (P3b), `StatisticsService` (P6), ön tahsis, K52, çok alan (F2b).
- **Kabul:** süzülmüş build 0 hata.

## P4 — Rezervasyon, rezervasyon kuralı, borç tipi koruması (P3a ile paralel)

- **Dosyalar:** `App/Services/Reservations/ReservationService.cs` (envanterdeki tüm satırlar; `:851` `Charge.SpaceId = reservation.SpaceId`; `:822-835` borç tipi `reservation.ChargeTypeId`'den), `ReservationBusinessRules.cs:147-177`, `Dom/Reservations/ReservationUnitPolicy.cs` (**silinir**, P1b'deki `ReservationSpacePolicy` kullanılır), `IReservationRepository.cs`, `ReservationRepository.cs`, `IReservationRateOverrideRepository.cs`, `ReservationRateOverrideRepository.cs` (`GetGeneralAsync` **silinir**; `GetActiveForSpaceAsync` D8), `ChargeTypeService.cs` (`:15` `IUnitTypeRepository` kalkar; `UpdateAsync` ve `ToggleStatusAsync`: borç tipi aktif bir rezervasyon kuralında kullanılıyorsa pasifleştirme/`ReservationSpecific` dışına çıkarma reddedilir; **düzeltme:** spec §6.6(a) geçerli: davranış değişikliği tip herhangi bir kuralda/rezervasyonda/tahakkuk kaleminde kullanılmışsa reddedilir, `IsChargeTypeUsedAsync`), `IChargeTypeRepository.cs:18` + `ChargeTypeRepository.cs:112-120` (`ResolveReservationTypeAsync` **silinir**), `App/Services/Pricing/RateHierarchyService.cs:15,68,75` (yalnız rezervasyon ata kartı), `RateScheduleService.cs:174,230,273` + `Web/Controllers/AdminRateController.cs` + `RateMatrix*` VM + `Views/AdminRate/Detail.cshtml` (D9), Reservation DTO'ları, `ReservationController`, `TenantReservationController`, `AdminReservationRateRuleController` (`BorcTipiId` seçimi; `Yil` kalkar), Reservation VM/Validator/View'ları, `Shared/_ParentReservationRateOverrideCard.cshtml`.
- **Kurallar:** D7, D8; ücret hesaplanan her an (oluşturma, talep, güncelleme) `BorcTipiId` kuraldan kopyalanır (K69); kural kaydında borç tipi aktif + `ReservationSpecific`; çakışma yalnız aynı alan, yalnız Confirmed (bugünkü).
- **Yapılmaz:** Kapsam filtrelerinin içi (P5 — burada yalnız `SpaceAccessScope` tipine geçilir), ödeme mağazası, kilit protokolü (F2b).
- **Kabul:** süzülmüş build 0 hata.

## P3b — Tahakkuk ve tarife (P5 ile paralel)

> **Dosya sahipliği (P3b‖P5, 2026-11-27):** `ChargeRepository` (dahil `ApplyScope`), `ChargeController`, `ManualChargeController` kapsam dönüşümü **P3b**'nin. `LeaseController` **P5**'in; `GetParentRateInput` satırını P3b sonrası ana oturum düzeltir.

- **Dosyalar:** `App/Services/Charges/ChargeGenerationService.cs` (`:25,55,100` → `SpaceId` = sözleşmenin tek `LeaseArea.SpaceId` (S-15); `:199-325`), `ManualChargeService.cs` + `ManualChargeController.cs` + `ManualChargeDtos` + `ManualChargeListItemDto` + `Dom/Charges/ManualChargePolicy.cs:34-46` (birim seçimi → alan), `ChargeService.cs`, `ChargeController.cs`, `IChargeRepository.cs`, `Inf/.../Charges/ChargeRepository.cs` (envanter satırları; `:1087`, `:1095` değişmez — F3), `ChargeLineItemRepository.cs:50-115`, Charge DTO'ları (`ChargeInputs`, `ChargeDetailDto`, `ChargeListItemDto`), `App/Services/Pricing/RateResolverService.cs` ve `BatchRateResolver.cs` (`IUnitRateRepository`/`IPropertyRateOverrideRepository` → P1c `SpaceRateResolver`; yıla düşme kalır D6), `IBatchRateResolver.cs`, `RateHierarchyService.cs` (sözleşme tarafı; P4'ün dokunduğu rezervasyon metotları hariç), `RateHierarchyInputs.cs`, `LeaseRepository.GetPropertyAndCategoryAsync/…Async` **yerine** yeni `GetSpaceAndCategoryAsync` (P3a'nın dosyası: P3a bu metodu imza olarak bırakır, P3b gövdeyi yazmaz — bkz. not), `Web/Views/Charge/Details.cshtml:119` linki.
- **Not:** `LeaseRepository` P3a'ya aittir. P3a, tarife çözümünün kullandığı `GetPropertyAndCategoryAsync` çiftini `GetSpaceAndCategoryAsync(leaseId) → (SpaceId, SpacePath, TenantCategoryId)` olarak çevirir; P3b yalnız çağırır.
- **P4 sonrası:** `RateHierarchyInputs.GetParentReservationRateInput` artık kullanılmıyor, silinir.
- **P3a sonrası:** `ChargeGenerationService` (`LeaseAccessScopeInput` → `SpaceAccessScope`, `ComposeLeaseLineItemsInput.UnitId` → `SpaceId`); `BatchRateResolver`/`RateResolverService` `GetSpaceAndCategor(y|ies)Async` çağırır; `ManualChargeService` + `CreateManualChargeViewModel` yeni `LeaseDropdownDto` alanlarına uyar.
- **Ek:** `LeaseController`'daki `GetParentRateInput(...)` çağrısı (`RateHierarchyInputs`'a bağlı) P3a'da bırakılır; P3b girdiyi alana çevirince bu satırı da düzeltir.
- **Yapılmaz:** `LeaseRateOverride` (F3), dönem filtresi/yeniden üretim (F3), ödeme.
- **P1c notu:** `SpaceRateResolver` silinmiş alan için doğrudan genel tarifeye düşer; silinmiş/pasif **ata** alanın tarifesi uygulanır (yalnız tarife satırının durumuna bakılır). P3b bu davranışı teyit eder; değişmesi gerekiyorsa durup sorar. Alan tarifesi kaynağı geçici `UnitRateOverride` → P2'de D5 ile `SpaceRate`.
- **Kabul:** süzülmüş build 0 hata.

## P5 — Yetki kapsamı, kullanıcı/davet, kiracı paneli, ödeme mağazası (P3b ile paralel)

- **Kapsam çekirdeği:** `App/Models/DTOs/UserScopeDto.cs:5-7` (D13), `Inf/Identity/PermissionScopeCacheService.cs:17-61`, `App/Services/Identity/PermissionScopeProvider.cs:11-20` + `IPermissionScopeProvider.cs:8-10` (`AccessiblePropertyIds/UnitIds`, `IsInScope(p,u)` → `SpaceRootPaths`, `IsSpaceInScope(path)`), `Web/Authorization/YetkiKapsamiActionFilter.cs`, `IUserPermissionScopeRepository.cs:8` + `UserPermissionScopeRepository.cs:14-41` (`ReplaceAsync(userId, spaceIds)`), F1 `SpaceService` silme/taşıma → D14.
- **Kapsam filtresi uygulanan repository'ler (yalnız filtre ifadesi):** `PaymentAllocationRepository.cs:227-228,249-250,371-372`, `TenantRepository.cs:28-29,55-56,74-75,112-132,254-255`, `ChargeRepository.cs:1108-1126 ApplyScope` (P3b dosyası — **P3b dokunmaz, yalnız bu metot P5'indir**). Lease/Reservation repository filtreleri P3a/P4'te `SpaceAccessScope` ile yazılır.
- **Kapsam girdisi oluşturan controller'lar:** `BankTransaction:166-167`, `Charge:26-28,71-73`, `ChargeReminder:30-31`, `ManualCharge:143-144`, `Payment:149-150`, `Report:26,29`, `Tenant:299-300` (Lease/Reservation controller'ları P3a/P4'te).
- **Kullanıcı/davet:** `Invitation`/`SendInvitationInput.cs:9-11`, `InvitationService.cs:49-54,145,159-181`, `AdminUserService.cs:28-269` (doğrulama `SpaceService` ile), `AdminUserDtos.cs`, `ApplicationUserRepository.cs:219,240`, `AdminUserController.cs`, `AdminUser{Edit,Invite}ViewModel.cs`, `Views/AdminUser/{Edit,Invite}.cshtml` (Taşınmaz/Birim seçicileri → **tek alan çoklu seçim listesi**, `Path` ile), `Inf/Identity/IdentitySeedService.cs` (`TumTasinmazlaraErisim`).
- **Kiracı kullanıcı + panel:** `TenantUserService.cs:195-455` (D15), `TenantUserDtos.cs`, `AdminTenantUserController.cs`, `TenantUserController.cs`, `TenantUserViewModels.cs`, `TenantUserEditViewModelValidator.cs`, `Views/AdminTenantUser/Invite.cshtml`, `Views/TenantUser/Invite.cshtml`, `Shared/_TenantUserEditFields.cshtml`, `TenantPanelService.cs:43-112` (D16), `TenantPanelDtos.cs`, `TenantChargeDtos.cs`, `TenantLeaseController`, `TenantChargeController`, `TenantPanelController`, `TenantChargeViewModels.cs`, `TenantPanelViewModel.cs`.
- **Ödeme mağazası:** `PaymentService.cs:32-294`, `OnlinePaymentService.cs:42,74`, `PaymentStoreResolver.cs` + `IPaymentStoreResolver.cs` (alan → ataları (Path) → genel; ilk eşleşen), `IPaymentStoreRoutingRepository.cs`, `PaymentStoreRoutingRepository.cs`, `PaymentStoreRoutingBusinessRules.cs`, `PaymentStoreRoutingService.cs`, `PaymentStoreRoutingDtos.cs`, `AdminPaymentStoreRoutingController.cs`, `PaymentStoreRoutingViewModels.cs`, `PaymentStoreRoutingFormViewModelValidator.cs`, `Views/AdminPaymentStoreRouting/Index.cshtml` (D10; alan seçici düz liste).
- **Köprü (P2 sonrası, ana oturum):** `IPermissionScopeProvider.SpaceScope` (`SpaceAccessScope`) eklendi; şimdilik `new(GlobalAccess, [])` (global olmayan kullanıcı hiçbir şey görmez). P3a/P4 bunu kullanır. P5 bunu `UserScopeDto.SpaceRootPaths`'ten gerçek yollarla doldurur ve `AccessiblePropertyIds/UnitIds/IsInScope(int)`'i siler.
- **P4 sonrası:** `ReservationAccessScopeInput` silindi; `TenantUserDtos.cs:84,106`, `TenantUserService.cs:441`, `AdminTenantUserController`, `TenantUserController` `SpaceAccessScope`'a geçer.
- **P3a sonrası:** `TenantController`, `TenantLeaseController` `LeaseAccessScopeInput` → `SpaceAccessScope`; `TenantUserService` → `GetActiveLeaseSpacesByTenantIdAsync`; `TenantPanelService` → yeni `CountActiveByTenantAsync`; `Views/Tenant/Details.cshtml` (`LeaseListItemDto.PropertyName` yok); `LeaseController.LegacyScopeIds()` kaldırılır (GetTenantsInput, DocumentAccessScopeInput, ChargeReminderScopeInput yeni kapsamla).
- **Yapılmaz:** K32 `NOT EXISTS`, K47, K34, K35 (F4).
- **Kabul:** süzülmüş build 0 hata.

## P6 — Rapor, ana sayfa, yan servisler

- **Dosyalar:** `Web/Controllers/HomeController.cs` (`:12-321`, D18), `DashboardViewModel.cs`, `Views/Home/Index.cshtml`, `ReportController.cs:24-38`, `MonthlyCollectionReportDtos.cs:8-9`, `App/Services/Reporting/StatisticsService.cs` + `IStatisticsService.cs` (D3), `ChargeReminderService.cs:41-109` + `ChargeReminderDtos.cs`, `DocumentService.cs:219-223` + `DocumentDtos.cs:37`, `BankTransactionService.cs:75-152`, `Web/Auditing/AuditDisplayNames.cs` (eski adlar silinir, `Space*` ve `UserAlanYetki` eklenir).
- **P4 sonrası:** `HomeController` rezervasyon çağrısı yeni imzayla: `GetReservationsInput(SpaceAccessScope Scope)` (`permissionScopeProvider.SpaceScope`).
- **P5 sonrası (Onur kabul etti, 2026-11-27):** P5 şu P6 sözleşmelerini **zaten değiştirdi**, P6 yalnız tüketicileri uyarlar: `GetMonthlyCollectionReportInput(SpaceAccessScope? Scope)`, `DocumentOwnerContextDto(TenantId, SpacePaths)`, `PaymentMatchingContextDto` (`PropertyId/UnitId` yok), `Match/Unmatch/GetCandidates*Input(SpaceAccessScope? Scope)`. P6 yapacak: `DocumentAccessScopeInput(AllowedOwnerTypes, SpaceAccessScope? Scope = null, int? TenantId = null)`, `ChargeReminderScopeInput` → `SpaceAccessScope`; `BankTransactionService`, `DocumentService`, `ChargeReminderService`, `StatisticsService` (`GetLeaseSummaryInput`), `HomeController` (`GetPaymentsInput(ChargeId, Scope)`, `GetLeasesInput`).
- **P3b sonrası:** `GetPendingChargeRemindersInput.Scope` (`SpaceAccessScope`) — `ChargeRepository:986` bekliyor.
- **P3a sonrası:** `StatisticsService` (`Lease.Unit` yok → `Lease.Areas`); `DocumentService` `GetDocumentOwnerContextAsync` artık `LeaseOwnerContextDto` döner; `HomeController` `GetLeasesInput` yeni imza; `Views/Home/Index.cshtml` `/Lease/Create?unitId=` → `spaceId`.
- **Yapılmaz:** Gelir payı (K33/K49, F4).
- **Kabul:** süzülmüş build 0 hata.

## P7a — Silme, DI, izin, menü, seed, migration (tek yazıcı; sonunda src yeşil)

- **Silinir:** envanterdeki 115 dosya (8 entity, 8 config, 3 policy, 9 servis, 8 servis arayüzü, 6 repo arayüzü, 7 repo, 19 DTO, 6 controller, 18 VM, 7 validator, 15 view, `tasinmaz-form.js`); `Enums.cs`'te `UnitStructure`, `UnitRentalMode`, `UnitTypeUsage` (`SpaceUsage` zaten var); `SystemSetting`'te `MinimumUsageArea` (D17); ApplicationDbContext DbSet'leri.
- **Değişir:** `PermissionCatalog.cs` (`Property`, `Unit`, `PropertyType`, `UnitType`, `PropertyMultiplier`, `Lease.UpdateAllocatedArea` + modül/rol kayıtları), `docs/permission-catalog.md`, `_Sidebar.cshtml` (Taşınmaz, Birim Türleri, Taşınmaz Tipleri, hızlı ekle), `_ViewImports.cshtml:21,22,34`, `Web/Help/HelpSections.cs:46,53-54` (controller adları), `ApplicationModule.cs:56-64,99-100`, `RepositoryModule.cs:41-67`.
- **Seed** (`Inf/Persistence/Seeding/SeedDataService.cs`): `SeedTasinmazTipleriAsync`, `SeedBirimTurleriAsync`, `SeedTasinmazFiyatlarAsync`, 5 taşınmaz + birim bloğu silinir; sözleşmeler (`MakeSozlesme`) F1 `SpaceSeedData` alanlarına `LeaseArea(SpaceId)` ile bağlanır; tahakkuk `SpaceId` (S-15); rezervasyon + rezervasyon kuralı alana ve `BorcTipiId`'ye; mağaza yönlendirmeleri alana; kullanıcı kapsamı `ScopeType.Space`; `ClearDomainDataAsync` FK sırası; rol izin listeleri. `SeedingExtensions.cs:58,89-103` (guard `Spaces`, çağrılar). Toplantı/Etkinlik → borç tipi bağı rezervasyon kuralına taşınır.
- **P4 sonrası:** Unit tarafında kırık kalanlar silinecek dosyalarla gider: `IUnitRepository.GetReservationContextAsync`/`UnitRepository` (`ReservationUnitContextDto`), `UnitPricingService`, `PropertyService`, `UnitTypeService`, `UnitController` rezervasyon kuralı çağrıları, `ReservationArea*ViewModel`. Seed: her kök alana varsayılan rezervasyon kuralı (S-7); kural yoksa rezervasyon yapılamaz.
- **P3a sonrası:** Kontenjan kalıntıları silinir: `CapacityPlanService`/`ICapacityPlanService`/`CapacityPlanDtos`, `IAllocationSuggestionStrategy` + Default/Proportional, `UnitCapacityController` + VM'leri, `ApplicationModule.cs:59-62` kayıtları, `IUnitRepository` (`LeaseUnitContextDto`). `CapacityOccupantQueryTests` P7b'de silinir.
- **Codex denetimi 2 (N03):** Endpoint'e özel izin eksik olan write endpoint'ler (CLAUDE.md kuralı): `Lease.UpdateDraft`, `Lease.ResubmitRevision`, `Lease.UploadDraftDocument` (bugün `Lease.Create`), `Lease.UpdateDueDate` (bugün `Lease.Edit`), `ReservationRateRule.ToggleStatus` (bugün `ReservationRateRule.Edit`). Her biri için sabit + modül/rol kaydı + `[Authorize]` + gerekiyorsa `ScopeAware` + `docs/permission-catalog.md`; seed rol izinleri bugünkü sahiplere aynısını verir (davranış değişmez).
- **P6 sonrası (2026-11-27):** (1) `PaymentMatchRepository.GetWithDetailsAsync` hâlâ `ThenInclude(charge => charge.Unit)` içeriyor → kaldır/`Space`. (2) Borç hatırlatma e-postası yalnız yaprak alan adı gösteriyor (`ChargeReminderService:~106`, `charge.Space?.Name`); D16 ad zinciri için `ChargeRepository.GetPendingReminderChargesAsync` sözleşmeli tahakkukta `Lease.Areas` (`SnapshotPath`) Include etmeli (`PaymentAllocationRepository:52-54` deseni). (3) `ChargeReminderController` ScopeAware'e eklenene kadar `permissionScopeProvider.SpaceScope` başlatılmıyor (fail-open) — ScopeAware maddesi bunu kapatır, doğrula.
- **P5 denetimi (eski risk):** `Notification.BorcHatirlatma` `PermissionCatalog.ScopeAware` listesine eklenir. Eklenmezse `ChargeReminderController`'da provider başlatılmaz, `SpaceScope` varsayılanı global kalır ve kapsamlı kullanıcı kapsam dışı borçlulara hatırlatma gönderebilir. Bu risk eski kodda da vardı. **F2b notu:** `PermissionScopeCacheService.InvalidateAll` tetikleyicisi eklenirken reset token `Set` öncesinde yerel değişkene alınır (yarış durumu / dispose).
- **P3b sonrası:** `RepositoryModule.cs:51,54` (`IPropertyRateOverrideRepository`, `IUnitRateRepository`) kayıtları; `PropertyController` (silinir). Testlerde (P7d) hata kodları: `MANUAL_CHARGE_LEASE_UNIT_MISMATCH` → `..._SPACE_MISMATCH`, `MANUAL_CHARGE_UNIT_FORBIDDEN` → `MANUAL_CHARGE_SPACE_FORBIDDEN`, yeni `MANUAL_CHARGE_SPACE_NOT_FOUND` (`ChargeArchitectureTests:277,302`).
- **Ana oturum:** `dbcontext info` → `DBTeknoPark_Test`; migration `ReplacePropertyUnitWithSpace`; `database drop --force` + `update`; uygulamayı bir kez çalıştırıp seed.
- **Kabul:** `dotnet build KiraTakip.slnx` src projeleri 0 hata; migration üretildi; seed hatasız.

## P7b — Test altyapısı + silinecek testler + izin testleri

- **Silinir (19):** `Integration/Properties/{PropertyCapacityLockTests,PropertyDetailsBulkFieldsTests,PropertyOccupancyTests,PropertyUnitLockTests,UnitCapacityChangeTests,UnitTypeServiceTests}`, `Integration/Leases/{CapacityConcurrencyTests,CapacityLeaseServiceTests,CapacityOccupantQueryTests,LeaseAllocatedAreaUpdateTests}`, `Integration/Migrations/LeaseUnitSnapshotBackfillTests`, `Unit/Leases/{UnitCapacityPolicyTests,ProportionalReductionStrategyTests}`, `Unit/Properties/PropertyStructurePolicyTests`, `Architecture/Properties/{PropertyArchitectureTests,PropertyFormUiArchitectureTests}`, `Integration/Settings/MinimumUsageAreaChangeTests` (D17). Çevrilir: `Integration/Leases/LeaseUnitSnapshotTests` → `LeaseAreaSnapshotTests`, `Unit/Leases/LeaseOccupancyPolicyTests` (D3, kapasite kısmı silinir), `Architecture/Pricing/UnitPricingArchitectureTests` → rezervasyon kuralı kısmı `SpaceReservationRuleTests`'e (D20).
- **Altyapı:** `Integration/Leases/LeaseServiceTestHarness.cs` (`Seed`, `SeedAsync`, `AddUnitAsync` → `AddSpaceAsync`, DI) → P1c `SpaceTestSeed`; yerel `CreateUnit` yardımcıları (`ReservationArchitectureTests:892`, `PaymentStoreResolverTests:150`, `ReservationAvailabilityTests:246`) → `SpaceTestSeed`.
- **İzin testleri:** `Unit/Authorization/*` (5 dosya), `Unit/Identity/{UserPermissionCacheTests,UserPermissionCacheTransactionTests}`, `Integration/Authorization/PermissionCachePhase4IntegrationTests`, `Architecture/Tenants/RoleAssignmentSecurityTests`, `Architecture/DependencyInjectionArchitectureTests`.
- **Kabul:** bu dosyalar 0 derleme hatası; derlenebilen bu testler yeşil.

- **P1c notu:** `SpaceTestSeed` her çağrıda `SaveChanges` yapar; **yalnız transaction/rollback içinde** kullanılır. Rollback'siz testte alan, alan türü, tarife ve `TestTenantCategory` temizliği elle yapılır (P7b–P7d hepsi için geçerli).

## P7c — Integration testleri (P7d ile paralel)

- **Dosyalar:** `Integration/{Banking/BankTransactionMatchingTests, Charges/*(4), Leases/*(6, harness hariç), Migrations/{ChargeLineItemPaymentMigrationTests,ChargeLineItemPaymentSchemaTests}, Payments/*(4), Persistence/ServerSidePaginationTests, Pricing/BatchRateResolverTests, Reservations/*(3), Tenants/TenantChargeAuthorizationTests, Settings/SystemSettingTests}`.
- **Kural:** test niyeti korunur (aynı senaryo, birim yerine alan); assert gevşetilmez; silinen özelliğe ait assert varsa silinir ve raporlanır.
- **Codex düzeltmesi sonrası:** `ServerSidePaginationTests` `GetRateRulesPagedAsync(query, scope)` (scope zorunlu). `BatchRateResolverTests` eşdeğerlik testine "LeaseId dolu + farklı SpaceId" girdisi eklenir (iki çözücü de sözleşmenin alanını esas alır). Kapsam testleri: kiracı tahakkuk özeti, rezervasyon kuralı yönetimi ve alan yönetimi kapsam dışı alanı reddeder.
- **Kabul:** bu dosyalar yeşil.
- **Codex denetimi 2 sonrası:** `SpaceServiceTests` `SpaceDeletePreviewDto`/`SpaceLinkedDefinitions` yeni alanları (rezervasyon kuralı, ödeme yönlendirmesi); test: kuralı ve alana özel yönlendirmesi olan yaprak alan silinince ikisi de soft delete + pasif, genel yönlendirme dokunulmaz (N02). Kontrol testi: `ReservationRateRule.Edit` izinli, `Create` izinsiz kullanıcı `Edit/0` POST ile kural oluşturamaz (N01, NotFound).

## P7d — Architecture + Unit testleri (P7c ile paralel)

- **Dosyalar:** `Architecture/{Charges/ChargeArchitectureTests, Leases/LeaseApprovalArchitectureTests, Payments/*(2), Pricing/PricingArchitectureTests, Reports/ReportArchitectureTests, Reservations/ReservationArchitectureTests, Tenants/*(4)}`, `Unit/{Charges/ManualChargePolicyTests, Payments/*(2), Tenants/*(2)}`.
- **Kural:** P7c ile aynı. Yalnız EF `entity.Property(...)` için eşleşen dosyalar değişmeden bırakılır.
- **Kabul:** bu dosyalar yeşil; ardından ana oturum **tam paket**.

## Son denetim

Birleşik değişiklik + tam test sonucu → 1 opus denetçi. Sonra `PROGRESS.md`, spec §11 F2a işareti, Vault iş notu.

## Onur kararları (2026-11-26)

- **S1 (D17):** `MinimumUsageArea` ayarı kontenjanla birlikte silinir.
- **S2 (D18):** Kapasite kartları silinir; doluluk kartı kök alan başına "kiralanabilir alan sayısı / aktif sözleşmeli alan sayısı" gösterir (F2b'de `SpaceOccupancyPolicy` ile iyileşir).
- Kartlar onaylandı.
