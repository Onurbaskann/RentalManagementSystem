# Paylaşımlı ve Açık Birim — Uygulama Planı

> Kaynak (tek doğruluk kaynağı): [`refactor-paylasimli-acik-birim.md`](refactor-paylasimli-acik-birim.md). Bu plan oradaki kararları uygular, yeni kural eklemez.
> Durum: **Onaylandı (2026-11-17); A1–A6 yazıldığı gibi geçerli.** F1–F8 ve unique index migration'ı tamamlandı; F9'da 1–5 tamamlandı, açık notların değerlendirmesi (madde 6) sürüyor.
> Kodda doğrulama: 2026-11-17, 3 Explore ajanı + ana oturum Grep/Read.

## 0. Bu planla kararlaştırılanlar (2026-11-17, kullanıcı cevabı)
1. **Bakiye ön koşulu sözleşme bazlıdır.** Yeniden yapılanmada yeni grup için eski gruptaki **her sözleşmenin** iptal edilmemiş tüm tahakkukları `Paid` olmalıdır. Kiracının başka sözleşmelerine bakılmaz. Kodda kiracı veya sözleşme düzeyinde bakiye kavramı yoktur; bakiye yalnız tahakkuk kalemi bazında tutulur (`GetPaymentBalanceAsync`).
2. **Min. kullanım m² ondalıklıdır, varsayılan 5.** Ayar altyapısına ondalık giriş türü eklenir (`SystemSettingInputKind`, `TryNormalizeValue`, Min/Max tipi).
3. **Kontenjan Property/Create'te de girilir.** Create formundaki birim satırına K alanı eklenir; kontenjan bazlı türde zorunludur. Yeni endpoint yoktur; mevcut `Internal.Property.Create` izni geçerlidir.
4. **Kontenjan bazlıya geçişte K taşınmaz ekranında girilir.** Tür kaydedilir, o türdeki birimlerin K'sı boş kalır. K girilene kadar bu birimler sözleşme formunda listelenmez ve sözleşme açılamaz. Property/Edit kaydında K zorunludur.

## 0.1 Onayda teyit edilecek varsayımlar
Aşağıdakiler dokümanda açık yazmıyor. Plan bu şekilde kuruldu; onay verirken düzeltilebilir.
- **A1 — Rol varsayılanları:** Yeni grup izinleri, tekil karşılığı rolde varsa role verilir:
  - `Create` varsa → `CreateGroup`, `UpdateGroup`, `ResubmitGroup`
  - `Approve` → `ApproveGroup`
  - `Extend` → `ExtendGroup`
  - `Terminate` → `TerminateGroup`
  - `DeleteDraft` → `DeleteGroupDraft`
  - `RestructureGroup` → yalnız `Terminate` **ve** `Create` izni olan role.
  - Süper rol tümünü `InternalAll` üzerinden alır.
- **A2 — Grup uzatma formu:** `ExtendGroup` mevcut `Extend` formunun alanlarını alır: yeni bitiş, açıklama, TÜFE, KDV ve tarife güncelleme. Tek fark yeni bitiş tarihinin ortak olmasıdır. Tarife satırları sözleşme başına girilir, çünkü her sözleşmenin tarifesi kendine aittir.
- **A3 — Kontenjan temizlenince tarihçe:** Tür Birim bazlıya döndüğünde temizlenen K için `BirimKontenjanGecmisleri` tablosuna satır yazılmaz. Gerekçe: Özellik kilidi gereği o türde hiç sözleşme yoktur, geçmiş raporu etkilenmez.
- **A4 — "Dolu kontenjan":** K azaltma ve form listesindeki "boş kontenjan" bugünden itibaren en yoğun gün üzerinden hesaplanır. Bu, dokümandaki kapasite kuralının ("en yoğun gün") bugün ve sonrasına uygulanmasıdır.
- **A5 — Açık birimde "süresi dolmak üzere" eşiği:** Yeni doluluk sorguları mevcut repository deseniyle aynı eşiği kullanır. Bkz. Uyuşmazlık U5.
- **A6 — Yeni GET ekranları:** `Lease/CreateGroup` formu, grup detay ve yeniden yapılanma formu `CLAUDE.md` kuralı gereği modül izniyle (`Internal.Lease.Module`) korunur. Butonlar ilgili POST izni varsa görünür.

## 1. Kod–doküman uyuşmazlıkları (bildirim)
Plan bunlara göre kuruldu. Hiçbiri kararları değiştirmiyor.
- **U1:** Satır numaraları kaymış.
  - `PropertyService.cs:347`, edit metodu değil, `UpdateMultipleUnitsAsync` içindeki `unit.Area = unitInput.Area` satırı. Edit metodu `UpdateWithChildrenAsync` ([PropertyService.cs:237](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L237)).
  - [PropertyService.cs:429-438](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L429-L438) ayrı bir "geçiş yasağı" değil. Çoklu birimde normal birim listesine rezervasyon türü seçilmesini reddeden doğrulamadır.
  - Tek birimli taşınmazda tür değişikliğinde kullanım türü kontrolü **yok** ([PropertyService.cs:419-427](../src/KiraTakip.Application/Services/Properties/PropertyService.cs#L419-L427)).
- **U2:** `LeaseController` izinleri dokümandan farklı. `Extend` için `Lease.Extend` izni kullanılıyor (dokümanda `Lease.Edit` ile gruplanmış). `Regenerate` için `Charge.Regenerate` kullanılıyor.
- **U3:** `!isOwner` koşulları `Details` action'ında değil, `Draft` GET action'ında ([LeaseController.cs:230-233](../src/KiraTakip.Web/Controllers/LeaseController.cs#L230-L233)).
- **U4:** `Property/Details` ekranı `_DurumBadge` kullanmıyor; yerel bir `StatusBadge` fonksiyonu var. `_DurumBadge` yalnız `Lease/Details` ekranında kullanılıyor.
- **U5:** "Süresi dolmak üzere" eşiği iki farklı yerden geliyor.
  - Repository'lerde 30 gün sabit kodlanmış: `PropertyRepository` (`GetListAsync`, `GetPagedListAsync`, `GetDetailsAsync:289`) ve `UnitRepository:43,129`.
  - `Lease.ExpiringSoonStatusDays` ayarını yalnız `StatisticsService` kullanıyor.
  - Bu çalışmada düzeltilmez (dokümanda yok).
- **U6:** `SystemSettingDefinitions` içindeki SeedId 9-16 ayarlarının (`Lease.ExpiringSoonStatusDays` dahil) migration'da seed'i bulunamadı. Yalnız `AddSystemSettings` 1-7'yi ekliyor. `SystemSettingTests.Migration_ShouldSeedAllDefinedSystemSettings` ise tüm tanımları DB'de bekliyor. Yeni ayar bu belirsizliğe dayanmaz; kendi migration'ında `InsertData` ile seed'lenir.
- **U7:** `CLAUDE.md` içindeki DbContext yolu güncel değil.
  - Yazan: `KiraTakip/Data/ApplicationDbContext.cs`
  - Gerçek: `src/KiraTakip.Infrastructure/Persistence/ApplicationDbContext.cs` (namespace `KiraTakip.Data`)
- **U8:** `Internal.Lease.DeleteDraft` operasyon rolünün varsayılan listesinde yok (`SeedDataService.cs` ~3445).
- **U9:** `Lease/Details` (iç) rozeti de `Model.UnitStatus` kullanıyor ([Lease/Details.cshtml:19](../src/KiraTakip.Web/Views/Lease/Details.cshtml#L19)). Doküman yalnız kiracı ekranının değişmesini istediği için iç ekran değişmez.
- **U10:** `UnitRepository.GetAvailableAsync` hem sözleşme formunu hem ana sayfadaki "boş birimler" listesini (`HomeController:63`) besliyor. Doküman yalnız sözleşme formu kuralını değiştiriyor. Bu yüzden kontenjan bazlı listeleme ayrı bir parametreyle yalnız sözleşme formunda açılır; ana sayfa listesi değişmez.

## 2. Mevcut davranışı değiştiren maddeler
| # | Değişiklik | Bugün | Sonra | Faz |
|---|---|---|---|---|
| D1 | Görev ayrılığı kaldırılır | `EnsureNotSelfReview` revizyon/silme/onayda ([LeaseService.cs:223/245/274](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L223)), `Draft` ekranında `!isOwner \|\| IsSuperAdmin` | Metot ve üç çağrı silinir, ekran koşulu yalnız izne bakar. Grup için de aynı kural geçerli. Düzenleme (`requireOwner: true`) **değişmez**. | F3, F7a |
| D2 | Tek açık başvuru kuralı uyarlanır | `HasOpenApplicationForUnitAsync` her birimde tek Taslak/Revizyon | Normal birimde aynen kalır. Paylaşımlıda grup tek başvurudur (aynı grubun taslakları birbirini engellemez). Kontenjan bazlıda kural atlanır, yerine kapasite kontrolü gelir. | F6, F7a |
| D3 | Çakışma kontrolünde taslak sayılır | `HasOverlappingActiveLeaseAsync` yalnız `Active` | `Active` + `Draft` + `RevisionRequested`. Paylaşımlıda aynı grubun sözleşmeleri hariç tutulur. Hata mesajı "açık sözleşme" olur, kod (`Lease.ActiveUnitConflict`) korunur. | F3 (grup hariç tutma F7a) |
| D4 | Birim türü özellik kilidi | `UnitTypeService.UpdateAsync` kullanım türünü serbestçe değiştiriyor ([UnitTypeService.cs:91](../src/KiraTakip.Application/Services/Properties/UnitTypeService.cs#L91)) | Türün herhangi bir biriminde silinmemiş tek bir sözleşme varsa (durumu ne olursa olsun) `Usage` ve `RentalMode` değiştirilemez | F4 |
| D5 | Birim alanı kilidi | Alan serbestçe düzenleniyor | Açık sözleşmesi (Active / gelecek tarihli / Draft / RevisionRequested) olan birimin alanı değiştirilemez. Tek birimli taşınmazda kapalı/açık alan da kilitlenir. Tek birimin türü de açık sözleşme varsa değiştirilemez. | F4 |

## 3. Fazlar
Her faz kendi başına derlenir ve testleri yeşil biter. Fazlar sırayla ilerler; bir faz denetimden geçmeden sonraki başlamaz.

**Ajan düzeni (tüm fazlar):**
- Okuma: Explore/sonnet, en fazla 4.
- Yazma: general-purpose/sonnet, en fazla 2; aynı dosyaya iki yazar verilmez.
- Denetim: her yazma için 1 `orch-reviewer`/opus.
- Testleri ana oturum çalıştırır: önce ilgili test sınıfı, faz sonunda tam paket.

### F1 — Domain politikaları ve ondalık ayar altyapısı (DB'siz) — ✅ Tamamlandı (2026-11-17)
- **Sonuç:** `UnitCapacityPolicy`, `LeaseGroupPolicy` eklendi. `LeaseRatePolicy.CalculateMultiplier` yeni overload'ı ve `LeaseOccupancyPolicy.DetermineCapacityStatus` eklendi. `SystemSettingInputKind.Decimal` geldi ve Min/Max `decimal?` oldu; tip değişikliği DTO'ya, ViewModel'e ve `Edit.cshtml`'e yansıdı. İlgili testler 71/71, tam paket 1020/1020 geçti, orch-reviewer ONAY verdi.
- **Sonraki fazlar için not:** `UnitCapacityPolicy.GetViolations`, Σ fiziksel > P iken boş kontenjan da varsa `AreaExceeded` ile birlikte `NormBelowMinimum` döner (k negatif olur). Bu hataların kullanıcıya nasıl gösterileceği F4 ve F6'da ele alınır.
- **Amaç:** Bütün hesap kurallarını saf ve test edilebilir hale getirmek. Entity değişmez, migration yok.
- **Dosyalar:**
  - `Domain/Models/Enums/Enums.cs`
    - `UnitRentalMode { UnitBased = 1, CapacityBased = 2 }` eklenir.
    - `OccupancyStatus` enum'una `PartiallyLeased = 4` eklenir. Değer hesaplanır, saklanmaz.
  - Yeni `Domain/Leases/UnitCapacityPolicy.cs`. İçerdiği hesaplar:
    - s = P/K
    - fiziksel m² aralığı: kiralanan > s ise [min, kiralanan], değilse = kiralanan
    - tarih aralığındaki en yoğun gün
    - kapasite ihlalleri: dolu ≤ K, Σ fiziksel ≤ P, boş kontenjan varsa k ≥ min
    - son kontenjan önerisi
    - izin verilen en yüksek K artışı
    - K azaltma alt sınırı
  - Yeni `Domain/Leases/LeaseGroupPolicy.cs`. Kurallar:
    - en az 2 sözleşme
    - kiracılar benzersiz
    - Σ m² = P
    - ortak başlangıç/bitiş
    - yeni grup başlangıcı > fesih tarihi; öneri fesih + 1 gün
  - `Domain/Leases/LeaseRatePolicy.cs`: Yeni çarpan aşırı yüklemesi `CalculateMultiplier(method, unitArea, rentedArea?, isLeaseRateSource)`.
    - M2 yöntemi → `rentedArea ?? unitArea`
    - Fixed yöntemi → sözleşme tarifesinden geliyorsa 1; değilse `rentedArea / unitArea`, `rentedArea` yoksa 1
    - Mevcut imza korunur.
  - `Domain/Leases/LeaseOccupancyPolicy.cs`: Açık birim durumu (Boş / Kısmen Dolu / Dolu).
  - `Application/Models/Settings/SystemSettingInputKind.cs` + `Application/Settings/SystemSettingDefinitions.cs`
    - `Decimal` giriş türü eklenir.
    - `TryNormalizeValue` ondalık normalizasyonu yapar (`InvariantCulture`).
    - Min/Max `decimal?` olur.
    - Yeni ayar tanımı **bu fazda eklenmez**. Eklenirse `Migration_ShouldSeedAllDefinedSystemSettings` kırılır.
  - Ayar ekranının ondalık input'u: `Views/AdminSystemSetting/*`.
- **Endpoint/izin:** yok.
- **Testler:** Unit/Leases altında yeni `UnitCapacityPolicyTests`, `LeaseGroupPolicyTests`, `LeaseOccupancyPolicyTests` (genişletme), `LeaseRatePolicyTests` (genişletme). Unit/Settings altında ondalık normalizasyon testi.
- **Ajan:** 1 yazma + 1 denetçi.

### F2 — Şema ve EF migration (tek yazma ajanı, başka yazma yok) — ✅ Tamamlandı (2026-11-17)
- **Sonuç:** Şema, entity ve EF konfigürasyonları ile `AddSharedAndCapacityUnits` migration’ı eklendi; mevcut sözleşmelerin dört birim anlık bilgisi silinmiş kayıtlar dahil dolduruldu. `Lease.MinimumUsageArea` (SeedId 17, varsayılan 5, ondalık) ve operasyon politikası alanı eklendi. Kullanıcının açıkça onayladığı kapsam eklemesiyle `AuditDisplayNames.EntityTypes` içine `LeaseGroup` → "Sözleşme grubu", `LeaseArea` → "Sözleşme alanı", `UnitCapacityHistory` → "Birim kontenjan geçmişi" eşlemeleri eklendi. Build: 0 hata, önceden var olan 5 ilgisiz uyarı; ayar/backfill testleri 10/10, audit testleri 4/4, tam paket 1022/1022 (0 skip, 36 sn); bağımsız denetçi APPROVE verdi. Test DB migration’ı uygulandı; production uygulaması bekliyor. F2 commit edilmedi.
- **Sonraki faz için not:** Yeni sözleşmelerde oluşturma ve taslak düzenlemesi sırasında anlık bilgi yazımı F3 kapsamındadır; bu fazda servis/controller/UI davranışı değiştirilmedi.
- **Entity'ler.** Sınıf adları İngilizce, tablo/kolon adları Türkçe ([Table]/[Column]); precision, check ve index ayarları `Persistence/Configurations/<Alan>/` altında:

| Entity / alan | Tablo / kolon | Not |
|---|---|---|
| `Lease.LeaseGroupId` | `Sozlesmeler.SozlesmeGrubuId` (int?, FK) | |
| `Lease.UnitTypeNameSnapshot` | `Sozlesmeler.AnlikBirimTuruAdi` | `BirimTurleri.Ad` uzunluğunda, not null |
| `Lease.UnitTypeUsageSnapshot` | `Sozlesmeler.AnlikKullanimTuru` (int) | |
| `Lease.RentalModeSnapshot` | `Sozlesmeler.AnlikKiralamaBicimi` (int) | |
| `Lease.UnitAreaSnapshot` | `Sozlesmeler.AnlikBirimYuzolcumu` (decimal) | `Birimler.Yuzolcumu` ile aynı precision |
| Yeni `LeaseGroup` : BaseEntity | `SozlesmeGruplari`: `BirimId`, `OncekiGrupId` (self-FK, null) | tarife tutmaz |
| Yeni `LeaseArea` : BaseEntity | `SozlesmeAlanlari`: `SozlesmeId` (unique, filtreli `IsDeleted=0`), `KiralananAlan`, `FizikselAlan` | CK: ikisi de > 0, `FizikselAlan <= KiralananAlan` |
| `UnitType.RentalMode` | `BirimTurleri.KiralamaBicimi` (int, varsayılan 1) | |
| `Unit.Capacity` | `Birimler.Kontenjan` (int?) | CK: null veya > 0 |
| Yeni `UnitCapacityHistory` : BaseEntity | `BirimKontenjanGecmisleri`: `BirimId`, `Kontenjan`, `GecerlilikBaslangici` | index (BirimId, GecerlilikBaslangici) |

- **Ayar:** `SystemSettingDefinitions` içine `Lease.MinimumUsageArea` eklenir.
  - Görünen ad "Min. kullanım m²", grup "Sözleşme".
  - Varsayılan "5", giriş türü `Decimal`, Min 0.
  - SeedId: mevcut en büyük + 1.
  - `OperationalPolicySettings` / `CreateOperationalPolicy` alanı eklenir.
- **Migration:** `AddSharedAndCapacityUnits`, tek migration.
  - Yeni kolon ve tabloları ekler.
  - `migrationBuilder.Sql` ile mevcut sözleşmelerin birim anlık bilgisini doldurur:
    ```sql
    UPDATE s SET AnlikBirimTuruAdi = ISNULL(bt.Ad, ''),
      AnlikKullanimTuru = ISNULL(bt.KullanimTuru, 1),
      AnlikKiralamaBicimi = 1,
      AnlikBirimYuzolcumu = b.Yuzolcumu
    FROM Sozlesmeler s
    JOIN Birimler b ON b.Id = s.BirimId
    LEFT JOIN BirimTurleri bt ON bt.Id = b.BirimTuruId
    ```
    Silinmiş sözleşmeler dahildir.
  - Ayarı `InsertData` ile seed'ler (`AddSystemSettings` deseni).
  - `Down` her şeyi geri alır.
- **Ana oturum adımları:**
  1. `dotnet build KiraTakip.slnx`
  2. **DB güncelle:**
     ```powershell
     dotnet ef database update --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
     ```
  3. Tam test paketi: `dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj`
- **Testler:**
  - `SystemSettingTests` mevcut haliyle yeşil olmalı (yeni ayar seed'li).
  - Yeni `Integration/Migrations/LeaseUnitSnapshotBackfillTests`: Silinmemiş hiçbir sözleşmenin `AnlikBirimTuruAdi` alanı boş değil, `AnlikBirimYuzolcumu` birimin alanına eşit.
- **Ajan:** 1 yazma + 1 denetçi.

### F3 — Mevcut tekil akış: anlık bilgi, taslağın çakışmaya girmesi, görev ayrılığı (D1, D3) — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - **Anlık bilgi:** Oluşturmada ve taslak düzenlemesinde/yeniden gönderimde güncel birimden yazılıyor (`LeaseUnitContextDto` 3 alanla genişledi). Onay sonrası yollar yazmıyor. Seed, sözleşmeleri eklemeden önce tek döngüyle dolduruyor; `MakeSozlesme` değişmedi.
  - **Çakışma (D3):** `HasOverlappingActiveLeaseAsync` artık Active + Draft + RevisionRequested sayıyor; `excludeLeaseGroupId` eklendi ama henüz çağrılmıyor. Mesaj "açık sözleşme" oldu, kod `Lease.ActiveUnitConflict` aynı.
  - **Görev ayrılığı (D1):** `EnsureNotSelfReview` silindi; `Lease/Draft` butonları yalnız izin ve duruma bakıyor. Düzenlemede sahiplik korunuyor.
  - **Ekranlar:** `Lease/Details` ve `Lease/Draft` anlık bilgiyi `FormatHelpers.UnitSnapshotText` ile gösteriyor ("70,00 m² · Ad (Kullanım · Biçim)").
  - **Testler:** Yeni `LeaseUnitSnapshotTests`, `LeaseDraftOverlapTests` ve ortak `LeaseServiceTestHarness`. `LeaseApprovalServiceTests` güncellendi. Tam paket 1031/1031 geçti.
  - **Denetim:** Yazma A ve B sırayla çalıştı, çünkü ikisi de `LeaseRepository.cs`'e dokunuyordu. İki denetçi ve son birleşik denetim ONAY verdi.
- **Açık nokta (kullanıcı kararı):** Anlık alanlar yalnız ekranın kullandığı iki projeksiyona eklendi: `ProjectDetails` ve `GetDraftForEditAsync`. `LeaseListItemDto` projeksiyonlarına eklenmedi, çünkü bunları gösteren bir liste ekranı yok. Gerekirse sonraki fazda eklenir.
- **Sonraki fazlar için not:** `ProjectDetails` kiracı tarafıyla ortak, bu yüzden kiracı `LeaseDetailDto`'su da anlık alanları taşıyor; kiracı ekranı henüz göstermiyor (F8). F2'den F3'e kadar oluşturulmuş sözleşme varsa anlık alanları "" ve 0 kalır. Veri dummy olduğu için sorun yok, ama production'a F2 ve F3 birlikte çıkmalı.
- **Servis ve repository:**
  - `LeaseService.CreateDraftAsync` ve `ApplyApplicationChangesAsync` ([:681](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L681)) birim anlık bilgisini güncel birimden yazar. Onaydan sonra hiçbir yol bu alanları yazmaz.
  - `LeaseRepository.HasOverlappingActiveLeaseAsync`: Draft ve RevisionRequested da sayılır. İmzaya `excludeLeaseGroupId` parametresi bu fazda eklenir, kullanımı F7a'da başlar.
  - `UnitRepository.GetLeaseContextAsync` anlık bilgi için tür adı, kullanım türü ve alt kırılımı da döner.
  - `EnsureNotSelfReview` ve üç çağrısı silinir (D1).
  - `SeedDataService` sözleşme oluşturduğu yerlerde (~1785, ~1972) anlık bilgiyi doldurur.
- **Controller:** `LeaseController.Draft` ([:230-233](../src/KiraTakip.Web/Controllers/LeaseController.cs#L230-L233)) `!isOwner || IsSuperAdmin` koşullarını kaldırır.
- **Ekranlar:**
  - `Lease/Details` ve `Lease/Draft` ekranlarında birim anlık bilgisi gösterilir. Örnek: "70 m² · Ortak Ofis (Kiralanabilir · Kontenjan bazlı)".
  - Sözleşme DTO'larına anlık alanlar eklenir: `LeaseRepository` projeksiyonları (`UnitArea` alanının olduğu yerler) ve `LeaseDetailDto`.
- **Endpoint/izin:** yok.
- **Testler:**
  - Etkilenen mevcut test: `LeaseApprovalServiceTests.Draft_ShouldRejectSelfReviewScopeAndActiveOnlyOperations` güncellenir; self-review beklentisi kalkar, kapsam ve aktif kontrolleri kalır.
  - Yeni `Integration/Leases/LeaseUnitSnapshotTests`:
    - oluşturmada yazılır
    - taslakta birim değişince yeniden yazılır
    - onaydan sonra birim türü adı/alanı değişse de sözleşme değişmez
  - Yeni `Integration/Leases/LeaseDraftOverlapTests`: B-12'de Active 2026 sözleşmesi + 2027 Draft varken Active sözleşme 2027-06'ya uzatılmak istenirse `Lease.ActiveUnitConflict` alınır.
  - `LeaseApprovalServiceTests`'e yeni durumlar: Oluşturan kişi `Lease.Approve`, `RequestRevision` ve `DeleteDraft` izinleriyle kendi başvurusunu onaylar, revizyon ister ve siler.
  - `Architecture/Leases/LeaseReview*ArchitectureTests` gerekirse güncellenir.
- **Ajan:** 2 yazma + 2 denetçi.
  - Yazma A: servis, repository, seed, testler.
  - Yazma B: controller, view, DTO projeksiyonları.

### F4 — Birim türü alt kırılımı, kontenjan, kilitler (D4, D5) — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - **Birim türü:** `RentalMode` yalnız Kiralanabilir türde kabul ediliyor; diğer kullanımlarda sessizce Birim bazlı kaydediliyor. Özellik kilidi `UnitTypeRepository.HasAnyLeaseAsync` ile çalışıyor (kod `UnitType.FeaturesLocked`; silinmemiş her sözleşme, silinmiş birimdekiler dahil). Birim bazlıya dönüşte K'lar `ClearUnitCapacitiesAsync` ile temizleniyor, tarihçe yazılmıyor.
  - **Taşınmaz:** Kontenjan bazlı türde K zorunlu (`Property.CapacityRequired`), tek birimli taşınmaz dahil. K değişince veya alan değişince iki kontrol çalışıyor:
    - Dolu kontenjanın altına inilemiyor (`Property.CapacityBelowOccupied`).
    - k ≥ min bozulursa kayıt reddediliyor ve mesajda en yüksek K gösteriliyor (`Property.CapacityAboveMaximum`). Örnek: P=70, min=5 → 14.
  - **Kilitler:** Açık sözleşmeli birimde alan değişmiyor (`Property.UnitAreaLocked`), tür de değişmiyor (`Property.UnitTypeLocked`). Tek birimde kapalı/açık alan da kilitli.
  - **Tarihçe:** K null→değer veya değer→farklı değer olduğunda `UnitCapacityHistory` satırı yazılıyor (`EffectiveFrom` = bugün). Create'te K ile oluşan birim için de yazılıyor.
  - **Testler:** `UnitTypeServiceTests` genişledi; yeni `PropertyUnitLockTests` ve `UnitCapacityChangeTests` eklendi. Tam paket 1062/1062 geçti.
  - **Denetim:** Yazma A ve B sırayla çalıştı, çünkü ikisi de `UnitTypeRepository`'ye dokunuyordu. İki denetçi ve son birleşik denetim ONAY verdi.
- **Açık noktalar (kullanıcı kararı):**
  - **Tek açık başvuru index'i:** DB'de `UX_Sozlesmeler_BirimId_AcikBasvuru` filtreli unique index'i var (`IsDeleted=0 AND Durum IN (4,5)`). Birim başına tek Taslak/Revizyon kaydına izin veriyor. Bu index, F6'daki "aynı kontenjan bazlı birimde birden çok taslak" ve F7a'daki "aynı gruptaki taslaklar" kurallarını veritabanı düzeyinde engelliyor. Planda yer almıyor. **Karar (2026-11-17):** F6'dan önce ayrı bir migration adımında (tek yazma ajanı) index kaldırılır. Normal birimdeki tek açık başvuru kuralını servis (`HasOpenApplicationForUnitAsync`) korumaya devam eder.
    - **Uygulandı (2026-11-17):**
      - Migration `20261117143035_RemoveLeaseOpenApplicationUniqueIndex` eklendi; Down index'i aynı ad ve filtreyle geri kurar.
      - Mimari test artık index'in olmadığını ve aynı birime ikinci taslağın DB'ye yazılabildiğini doğruluyor.
      - `LeaseApprovalServiceTests`'e servisin ikinci taslağı `Lease.OpenApplicationConflict` ile reddettiğini doğrulayan test eklendi.
      - Tam paket 1067/1067 geçti; denetim ONAY verdi.
      - Kabul edilen risk: Normal birimde iki eşzamanlı istek servis kontrolünü birlikte geçerse aynı birimde iki açık başvuru oluşabilir.
  - **Tek birimli taşınmaz (kapandı, 2026-11-17 kullanıcı kararı):** "Tek birimli taşınmaz kontenjan bazlı olamaz" kuralı kaldırıldı. Tek birim bölümüne K girişi (`SingleUnitCapacity`) eklendi; çoklu birimdeki K kuralları ve tarihçe aynen uygulanıyor. Kontenjan girilmemiş birimin K'sı DB'de boş kalıyor, Edit ekranında 0 görünüyor; kayıt için K ≥ 1 gerekiyor.
  - **Sınır durumu (düşük önem):** P < min olan sözleşmesiz birimde mesaj "en yüksek K: 1" diyor, oysa 1 de reddediliyor.
- **Birim türü:**
  - `UnitTypeService.CreateAsync/UpdateAsync`:
    - `RentalMode` yalnız `Usage == Rentable` iken kabul edilir.
    - Usage Rentable dışına çevrilirse `RentalMode = UnitBased` olur ve türdeki birimlerin `Capacity` değeri null yapılır (A3).
    - Özellik kilidi için yeni `UnitTypeRepository.HasAnyLeaseAsync(unitTypeId)`.
    - Kontenjan bazlıya geçişte birimlerin K'sı boş kalır (karar 4).
  - `AdminUnitTypeController`, `UnitTypeFormViewModel` ve validator güncellenir.
  - `Views/AdminUnitType/{Create,Edit}`: Alt kırılım seçimi yalnız Kiralanabilir seçiliyken görünür (Alpine). Kilitliyse alanlar salt okunur, açıklaması yazılır.
- **Taşınmaz:**
  - `PropertyService.CreateAsync` / `UpdateWithChildrenAsync` / `ValidateDatabaseRulesAsync`:
    - `PropertyUnitInputDto.Capacity` alanı eklenir; kontenjan bazlı türde zorunludur, diğer türlerde null zorlanır.
    - ~~Tek birimli taşınmazda kontenjan bazlı tür seçilemez.~~ (kaldırıldı; tek birimde K `SingleUnitCapacity` ile girilir)
    - K değişikliği kuralları:
      - dolu kontenjanın altına inemez (A4)
      - artış k ≥ min'i bozuyorsa reddedilir ve izin verilen en yüksek K mesajda gösterilir
      - her değişiklikte `BirimKontenjanGecmisleri` tablosuna satır eklenir (`GecerlilikBaslangici` = bugün)
    - Alan kilidi: Çoklu birimde `Area`, tek birimde `ClosedArea`/`OpenArea`. Açık sözleşme varsa değiştirilemez.
    - Tür değişikliği: Açık sözleşmesi olan birimin türü değiştirilemez; yalnız bitmiş sözleşme engel değildir.
  - Yeni `UnitRepository.GetOpenLeaseUnitIdsAsync(unitIds)`.
  - `PropertyEditDto` yalnız `ActiveLeaseUnitIds` taşıyor; yanına `OpenLeaseUnitIds` eklenir.
- **Ekranlar:**
  - `Views/Property/{Create,Edit}.cshtml` satır içi Alpine birim satırlarına K input'u eklenir; tür kontenjan bazlıysa görünür.
  - Açık sözleşmeli birimde alan input'u kilitli olur; tek birimde kapalı/açık alan da kilitli.
- **Endpoint/izin:** Yeni endpoint yok. Mevcut `Property/Create` (`Internal.Property.Create`), `Property/Edit` (`Internal.Property.Edit`), `AdminUnitType/Create|Edit` (`Internal.UnitType.Create|Edit`) kullanılır.
- **Testler:**
  - `Integration/Properties/UnitTypeServiceTests` genişletilir:
    - bitmiş sözleşmesi olan tür Rezerve edilebilir yapılamaz
    - sözleşmesiz tür Kontenjan bazlıya geçer, birimlerin K'sı boş kalır
    - Rentable dışına çevrilince K temizlenir
  - Yeni `Integration/Properties/PropertyUnitLockTests`:
    - P=60 birimde A 30 + B 30 taslak grup varken alan 65 yapılamaz. F4'te grup henüz yok; taslak sözleşme satırı test verisiyle kurulur.
    - Tek birimli taşınmazda kapalı/açık alan kilidi.
    - Yalnız bitmiş sözleşmesi olan birimin türü değişebilir; açık sözleşmesi olanınki değişemez.
    - ~~Tek birimli taşınmaz kontenjan bazlı türe bağlanamaz.~~ (yerine: tek birimde K ile kabul edilir, K kuralları geçerli)
  - Yeni `Integration/Properties/UnitCapacityChangeTests`:
    - K artışı/azaltışı, en yüksek izin verilen artış mesajı.
    - Tarihçe satırı, `GecerlilikBaslangici` = bugün.
    - Create'te K zorunluluğu.
    - Kontenjan bazlıya geçen türün birimine Edit'te K zorunluluğu.
- **Ajan:** 2 yazma + 2 denetçi.
  - Yazma A: birim türü (servis, repository, controller, view, testler).
  - Yazma B: taşınmaz (servis, DTO, `UnitRepository`, view, testler).

### F5 — m² çarpanı ve sabit kalem oranı — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - Tahakkuk (`ChargeGenerationService.ComposeLineItemsAsync`), istatistik (tekil ve toplu yol) ve uzatma/yeniden üretimdeki tarife doğrulaması sözleşmenin `RentedArea` değerini kullanıyor. Okuma `LeaseRepository.GetRentedAreaAsync` ve `GetRentedAreasAsync` ile yapılıyor.
  - Sözleşme tarifesi kaynağı `SourceType == LeaseRateOverride` ile ayrılıyor.
  - Sözleşme oluşturmada henüz kiralanan m² girişi yok, bu yüzden oluşturma `Unit.Area` kullanmaya devam ediyor.
  - `_LeaseFormScripts.cshtml` içine `rentedArea` (varsayılan null) eklendi; giriş alanı F6 ve F7a'da gelecek.
  - Yeni `LeaseRatePolicy.CalculateBaseAmount` overload'u kuruşa yuvarlıyor (ToEven, tahakkukla aynı). Bu yüzden istatistik ve uzatma ekranındaki tutarlar da artık kuruşa yuvarlanıyor; fark kalem başına en fazla 0,005 TL.
  - Testler: yeni `LeaseAreaChargeGenerationTests` eklendi. Tam paket 1067/1067 geçti; denetim ONAY verdi.
- **Sonraki fazlar için not:**
  - `PropertyService.cs:70` civarı: istatistikte birim alanı bulunamazsa 0'a düşüyor. LeaseArea'lı ve Fixed bir kalemde `CalculateMultiplier` bu durumda istisna atar. LeaseArea yazan akış (F6/F7a) eklenince ele alınmalı.
  - `Multiplier` kolonu (18,4) oranı 0,3333 olarak saklıyor. Tutar ayrı hesaplandığı için doğru (2.000,00), ama ekranda "birim değer × çarpan" 1.999,80 gibi görünebilir.
  - Yeniden üretme yolu ve silinmiş LeaseArea senaryosu ayrıca test edilmedi.
- **Tahakkuk:** `ChargeGenerationService:258`
  - Sözleşmenin `LeaseArea` satırını ve `RateSnapshot.SourceType == LineItemSourceType.LeaseRateOverride` bilgisini F1'deki yeni `CalculateMultiplier` ile kullanır.
  - Birim alanı (P) olarak hesap anındaki `Unit.Area` kullanılır. Alan kilidi sayesinde açık sözleşme boyunca sabittir.
- **İstatistik:** `StatisticsService` :50, :80, :143 aynı çarpanı kullanır (`CalculateBaseAmount` genişletilir).
- **Sözleşme tarifesi doğrulaması:** `LeaseService.EnsureRateOverridesAsync` / `CanUseCalculationMethod` birim alanı yerine `RentedArea ?? Unit.Area` alır. Çağrı yerleri: [:377](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L377), [:504](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L504), [:647](../src/KiraTakip.Application/Services/Leases/LeaseService.cs#L647).
- **Önizleme:** `_LeaseFormScripts.cshtml` içindeki `unitArea()` önizlemesi, kiralanan m² girilmişse onu kullanır.
- **Endpoint/izin:** yok.
- **Testler:** Yeni `Integration/Charges/LeaseAreaChargeGenerationTests`:
  - P=60, A 20 + B 40, birim tarifesinde aidat sabit 6.000 TL → A 2.000, B 4.000.
  - A'ya sözleşme tarifesiyle aidat sabit 1.500 TL → A 1.500, B 4.000.
  - M2 kira: kiralanan m² × birim fiyat.
  - `LeaseArea` olmayan normal sözleşmede tutar değişmez (regresyon).
  - `StatisticsService.GetMonthlyAmountAsync` aynı sonuçları verir.
- **Ajan:** 1 yazma + 1 denetçi.

### F6 — Açık birim (kontenjan bazlı) sözleşme akışı (D2'nin kontenjan kısmı) — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - `LeaseService` Create/UpdateDraft/ResubmitRevision/Approve/Extend kontenjan bazlı birimde tek açık başvuru kuralını atlıyor, `UnitCapacityPolicy.GetViolations` ile kapasite kontrolü yapıyor ve `LeaseArea` yazıyor. Taslakta birim kontenjan bazlıdan normale dönerse `LeaseArea` siliniyor. Hata kodları: `Lease.UnitCapacityMissing`, `Lease.CapacityExceeded`, `Lease.CapacityAreaExceeded`, `Lease.CapacityFreeAreaBelowMinimum` (alan UnitId), `Lease.RentedAreaBelowMinimum`, `Lease.AllocatedAreaOutOfRange`.
  - Uzatmada kapasite yalnız eklenen aralıkta (`EndDate+1..NewEndDate`) kontrol ediliyor; önceki aralık zaten sayılmış durumda.
  - `LeaseService` yapıcısına `IOperationalPolicyProvider` eklendi.
  - `UnitRepository.GetAvailableAsync(..., includeCapacityBased)`: yalnız sözleşme formu true geçiyor. Boş kontenjan, boş m² ve son kontenjan önerisi bugünden en geç bitişe kadarki en yoğun güne göre.
  - Form: kontenjan bazlı birimde "Kiralanan m²", norm aşılınca uyarı ve "Fiziksel m²", son kontenjan önerisi, etikette "N kişi · X m² boş".
  - Taslak kaydetme/yeniden gönderme hataları eskisi gibi modalla gösteriliyor (alan altı gösterim yalnız Create'te).
  - Testler: yeni `CapacityLeaseServiceTests` (11 test). Tam paket 1078/1078; iki yazma denetimi ve son denetim ONAY.
- **Açık notlar (düşük önem):**
  - Onayda doğrulamanın hesapladığı fiziksel m² geri yazılmıyor; taslaktan sonra P/K değişirse DB'de eski fiziksel m² kalabilir.
  - Taslak düzenlenirken birim listesinde taslağın kendisi de dolu sayılıyor; etikette boş kontenjan 1 eksik görünür, öneri gereğinden küçük olabilir.
  - Onayda kapasite yeniden kontrolü ve taslakta birim değişimi ayrıca test edilmedi. Form tarayıcıda denenmedi.
  - `LeaseController` `!ModelState.IsValid` dalında `Draft()` görünüm adı vermeden dönüyor (F6 öncesinden kalma).
- **Servis:** `LeaseService` oluşturma, taslak güncelleme, yeniden gönderme, onay ve uzatmada birimin `RentalMode` değerine göre dallanır. Kontenjan bazlıda:
  - `HasOpenApplicationForUnitAsync` atlanır.
  - Tarih çakışması yerine kapasite kontrolü yapılır: aralıktaki en yoğun gün; Active + gelecek tarihli + Draft + RevisionRequested sayılır.
  - Kiralanan m² ≥ min.
  - Fiziksel m² aralığı uygulanır.
  - `LeaseArea` satırı yazılır.
  - Uzatmada kapasite yeni aralık için kontrol edilir.
  - K boş (karar 4) ise sözleşme açılamaz.
- **Repository:** Yeni `LeaseRepository.GetOpenLeaseAreasForUnitAsync(unitId, start, end, excludeLeaseId)`. `UnitRepository.GetAvailableAsync` yeni parametreyle kontenjan bazlı birimleri yalnız sözleşme formunda listeler (U10). Liste kuralları:
  - boş kontenjan > 0
  - K dolu
  - etiket "4 kişi · 28 m² boş"
- **Controller ve ekran:**
  - `CreateLeaseViewModel` / `LeaseDraftViewModel` ve input'lara `RentedArea`, `AllocatedArea` eklenir.
  - `Views/Lease/_LeaseForm.cshtml` + `_LeaseFormScripts.cshtml`: Kontenjan bazlı birim seçilince m² alanları açılır. Davranışlar:
    - kiralanan > s ise uyarı ve fiziksel m² girişi
    - son kontenjanda kalan fiziksel m² önerisi
    - birim listesinde boş kontenjan ve boş m² gösterimi
- **Endpoint/izin:** Yeni endpoint yok; `Lease/Create` (`Internal.Lease.Create`) forma alan eklenir.
- **Testler:** Yeni `Integration/Leases/CapacityLeaseServiceTests`:
  - K=10, 8 aktif + 2 taslak → yeni taslak reddedilir.
  - O-1'de aynı anda 3 taslak açılabilir.
  - P=70, K=10, 10 m² için fiziksel 5–10 kabul edilir, 4 reddedilir.
  - Son kontenjan önerisi: 9 dolu, Σ fiziksel 60 → 10/10.
  - k < min → kayıt reddedilir.
  - Uzatmada kapasite ihlali.
  - Kiralanan < min reddedilir.
- **Ajan:** 2 yazma + 2 denetçi.
  - Yazma A: servis, repository, testler.
  - Yazma B: viewmodel, controller, form.
  - B, A'nın imzaları kesinleştikten sonra başlar (sıralı).

### F7a — Paylaşımlı grup: oluşturma, taslak, onay, revizyon, silme — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - Yeni `LeaseGroupService` / `LeaseGroupRepository`. Onay doğrulamaları ve aktivasyon, tekil ve grup akışı aynı kodu kullansın diye `LeaseService`'ten `LeaseApplicationRules`'a çıkarıldı (kullanıcı onayı; tekil davranış değişmedi).
  - Ortak alanlar: tarihler, vade kuralı, açıklama, bedelsiz. Sözleşme başına: kiracı, m², sözleşme tarifesi.
  - UpdateGroup ve ResubmitGroup yalnız grubu oluşturana açık (tekildeki sahiplik kuralı). DeleteGroupDraft ve ApproveGroup izne bağlı.
  - Ek kural: her pay > 0 m² (`LeaseGroup.InvalidArea`; kullanıcı kararı, spec'e eklendi).
  - Seed: operasyon rolü CreateGroup, UpdateGroup, ResubmitGroup, ApproveGroup aldı; rolde DeleteDraft olmadığı için DeleteGroupDraft verilmedi.
  - Web: `LeaseController.Group.cs` (partial), `Group`, `GroupForm` ekranları; tarife satırları ortak `_LeaseLineItemFields` partial'ında. `Draft` action'ı artık görünüm adını açıkça veriyor (F6'da not edilen 500 riski kapandı).
  - Testler: `LeaseGroupServiceTests` (12), `LeaseGroupControllerArchitectureTests`, `PermissionTests` +1. Tam paket 1105/1105; iki yazma denetimi ve son denetim ONAY.
- **Açık notlar (plan sonunda bakılacak):**
  - `Lease/UpdateDueDate` gruptaki tek sözleşmede açık; doküman kapatılanlar arasında saymıyor. Grup ortak vade kuralı taşıdığı için kapatılması gerekip gerekmediği karara bağlanmalı.
  - Formlar tarayıcıda denenmedi.
- **Sonradan kapatılanlar (2026-11-17):** grup onayı "hepsi ya da hiçbiri" testi (`Lease.RequiredDocumentsMissing`) ile tekil ResubmitRevision/Extend/Terminate kapatma testleri eklendi; `Lease/Group/{id}/Edit` bekleyen üyesi olmayan grupta açılmıyor, detaya "Grup bu durumda düzenlenemez." uyarısıyla dönüyor; hata sonrası üye başlığı `LeaseNo` gizli alanıyla korunuyor. Tam paket 1108/1108, denetim ONAY.
- **Servis:** Yeni `Application/Services/Leases/LeaseGroupService` (+ arayüz) ve `ILeaseGroupRepository`.
  - **CreateGroup:**
    - Yalnız Birim bazlı ve Kiralanabilir birim.
    - ≥2 farklı kiracı, Σ m² = P, ortak tarihler.
    - Birimde başka açık başvuru yok.
    - Grup dışı sözleşmelerle tarih çakışması yok.
    - Her sözleşmeye anlık bilgi ve `LeaseArea` yazılır (kiralanan = fiziksel = pay).
  - **UpdateGroup:** pay/m², tarihler ve kiracılar. Kayıtta Σ m² = P.
  - **ResubmitGroup:** revizyondaki sözleşmeleri Taslağa döndürür.
  - **DeleteGroupDraft.**
  - **ApproveGroup:**
    - Gruptaki tüm sözleşmeler Draft olmalı; biri RevisionRequested ise reddedilir.
    - Her sözleşme için mevcut onay doğrulamaları çalışır (veri, zorunlu belge, kira tarifesi). Ardından hepsi tek `SaveChanges` ile `Active` olur ve her biri için tahakkuk üretilir.
    - `PreviousGroupId` varsa ön koşul kontrol edilir (F7b'de doldurulur; bu fazda null).
  - **Tekil işlemlerin kapatılması:** `LeaseId` bir gruba aitse tekil `UpdateDraft`, `ResubmitRevision`, `DeleteDraft`, `Approve`, `Extend`, `Terminate` reddedilir. `RequestRevision` ve `UploadDraftDocument` açık kalır.
  - `HasOpenApplicationForUnitAsync` ve `HasOverlappingActiveLeaseAsync` aynı grubu hariç tutar (D2, D3).
  - Görev ayrılığı yoktur (D1).
- **İzinler:** `PermissionCatalog.cs` Lease sınıfına sabit ve etiket eklenir (`ActionDefinitions`). Ayrıca `InternalAll` listesine, tekil karşılıkları oradaysa `ScopeAware` listesine eklenir. Rol varsayılanları `SeedDataService.cs` ~3430-3475 (A1). Yetki ağacı `AllModules` + `ActionDefinitions` üzerinden kendiliğinden gelir.

| Endpoint (POST) | İzin | Etiket |
|---|---|---|
| `Lease/CreateGroup` | `Internal.Lease.CreateGroup` | Paylaşımlı Grup Oluştur |
| `Lease/UpdateGroup` | `Internal.Lease.UpdateGroup` | Grup Taslağı Düzenle |
| `Lease/ResubmitGroup` | `Internal.Lease.ResubmitGroup` | Grubu Yeniden Gönder |
| `Lease/DeleteGroupDraft` | `Internal.Lease.DeleteGroupDraft` | Grup Taslağı Sil |
| `Lease/ApproveGroup` | `Internal.Lease.ApproveGroup` | Grup Onayla |

- **Ekranlar** (GET'ler `Internal.Lease.Module` ile, A6):
  - `Lease/CreateGroup` formu: birim, tarihler, kiracı + m² satırları, sözleşme başına opsiyonel sözleşme tarifesi.
  - `Lease/Group/{id}` detay: sözleşmeler, durumları, grup işlem butonları.
  - Grup düzenleme formu.
  - Gruptaki sözleşmenin `Lease/Draft` ekranında gruba bağlantı verilir ve tekil butonlar gizlenir (revizyon iste ve belge yükleme kalır).
  - Kiracı tarafı değişmez (taslak görünür).
- **Testler:**
  - Yeni `Integration/Leases/LeaseGroupServiceTests`:
    - oluşturma: Σ≠P reddi, tek kiracı reddi, aynı kiracı iki pay reddi, kontenjan bazlı birim reddi
    - gruptaki taslaklar birbirini engellemez, aynı birimde ikinci grup ya da tekil taslak engellenir
    - bir sözleşme RevisionRequested iken grup onaylanamaz, tek onayla tümü Active olur
    - oluşturan kişi `CreateGroup` + `ApproveGroup` izinleriyle kendi grubunu onaylar ve siler
    - gruptaki sözleşmeye tekil Approve/UpdateDraft/DeleteDraft reddedilir
    - grup içi revizyonda m² yalnız UpdateGroup ile değişir
  - `Unit/Authorization/PermissionTests`: Yeni izinler `InternalAll`'da, etiketler doğru, path'ler benzersiz.
  - Yeni `Architecture/Leases/LeaseGroupControllerArchitectureTests`: Her yeni POST kendi izniyle korunur.
- **Ajan:** 2 yazma (sıralı) + 2 denetçi.
  - Yazma A: servis, repository, izin kataloğu, seed, testler.
  - Yazma B: controller, view'lar, mimari test.

### F7b — Paylaşımlı grup: uzatma, fesih, yeniden yapılanma — ✅ Tamamlandı (2026-11-17)
- **Sonuç:**
  - `LeaseGroupService`: `ExtendGroupAsync`, `TerminateGroupAsync`, `RestructureGroupAsync`; `ApproveGroup` ön koşulu (`LeaseGroup.PreviousGroupUnsettled`, `LeaseGroup.PreviousDepositNotRefunded`). Dummy `IDepositRefundStatusService` her zaman true.
  - Tekil uzatma doğrulamaları `LeaseApplicationRules.EnsureExtensionAllowedAsync`'e taşındı; sıra ve kodlar aynı.
  - Kullanıcı kararları: yeniden yapılandırma formunda yeni grubun bitiş, vade kuralı, açıklama ve bedelsiz alanları formda ve eski gruptan dolu; fesih nedeni yeniden yapılandırmada zorunlu; grup fesihte tekil fesihteki gibi (web formunda zorunlu, serviste değil). `GetDefaultLineItems` izin kontrolüne `ExtendGroup` ve `RestructureGroup` eklendi.
  - Seed: operasyon rolü `ExtendGroup`, `TerminateGroup`, `RestructureGroup` aldı.
  - Web: grup detayında uzatma ve fesih modalları, `Lease/Group/{id}/Restructure` formu, önceki gruba bağlantı. Form `/Lease/RestructureGroup` adresine gider (denetimde bulunan 405 düzeltildi).
  - Testler: `LeaseGroupLifecycleTests` (11), mimari ve izin testleri. Tam paket 1124/1124; iki yazma denetimi ve son denetim ONAY.
- **Açık notlar (plan sonunda bakılacak):**
  - Formlar tarayıcıda denenmedi (uzatma/fesih modalları, başlangıç önerisi, yeniden yapılandırma gönderimi).
  - Grup uzatmada tarife satırları grup başlangıç tarihiyle getiriliyor; tekil uzatmadaki tarife getirme mantığından farklı olabilir.
  - Eski grubun bitişi geçmişteyse yeniden yapılandırma formu ilk gönderimde bitiş hatası verir; kullanıcı düzeltebilir.
  - Grup uzatma formunda tekil formdaki canlı tutar özeti yok.
- **Servis:**
  - **ExtendGroup:** Tüm sözleşmelere ortak yeni bitiş; mevcut uzatma kuralları sözleşme başına çalışır (A2). Çakışma kontrolü grubu hariç tutar.
  - **TerminateGroup:** Ortak fesih tarihi; tüm sözleşmeler `Terminated` olur ve gelecek tahakkuklar iptal edilir (`CancelFutureChargesAsync`).
  - **RestructureGroup:** Tek işlemde eski grubun tüm sözleşmeleri girilen fesih tarihiyle feshedilir. Yeni grup `PreviousGroupId` ile Draft açılır.
    - Başlangıç önerisi fesih + 1 gün; fesih tarihi veya öncesi seçilemez.
    - Yeni sözleşmelere tam depozito kesilir (mevcut tahakkuk üretimi).
  - **ApproveGroup ön koşulu:** `PreviousGroupId` varsa eski gruptaki her sözleşme için iki şart aranır:
    - iptal edilmemiş tüm tahakkuklar `Paid` (karar 1)
    - yeni `IDepositRefundStatusService.IsRefundedAsync(leaseId)` true. Dummy uygulama her zaman true döner ve DI'a kaydedilir.
- **İzinler** (aynı katalog/seed deseni):

| Endpoint (POST) | İzin | Etiket |
|---|---|---|
| `Lease/ExtendGroup` | `Internal.Lease.ExtendGroup` | Grubu Uzat |
| `Lease/TerminateGroup` | `Internal.Lease.TerminateGroup` | Grubu Feshet |
| `Lease/RestructureGroup` | `Internal.Lease.RestructureGroup` | Grubu Yeniden Yapılandır |

- **Ekranlar:**
  - Grup detayında uzatma ve fesih formları.
  - Yeniden yapılanma ekranı tek formdur: fesih tarihi, yeni başlangıç (öneri dolu), yeni kiracı + m² satırları.
  - Kiracı tarafında eski sözleşme normal fesih olarak, yeni sözleşme ayrıca listelenir (mevcut liste davranışı, ek iş yok).
- **Testler:** Yeni `Integration/Leases/LeaseGroupLifecycleTests`:
  - Uzatma tüm gruba uygulanır.
  - Fesih tüm gruba uygulanır.
  - Yeniden yapılanma (A 30 + B 30 → A 35 + B 25):
    - eski sözleşmeler `Terminated`, yeni grup Draft ve `PreviousGroupId` = eski
    - başlangıç ≤ fesih reddedilir
    - eski sözleşmede ödenmemiş tahakkuk varken yeni grup onaylanamaz; ödenince onaylanır
    - kiracının başka sözleşmesindeki borç engel değildir (karar 1)
    - tek kiracılı yeni grup reddedilir
  - Mimari test yeni üç POST'u kapsar.
- **Ajan:** 2 yazma (sıralı) + 2 denetçi.

### F8 — Doluluk, ana sayfa, kiracı paneli — ✅ Tamamlandı (2026-11-18)
- **Doluluk:** `LeaseOccupancyPolicy` ve `PropertyRepository` (`GetListAsync`, `GetPagedListAsync`, `GetDetailsAsync`) ile `UnitRepository` (`GetByPropertyIdAsync`, `GetDetayAsync`) güncellenir.
  - **Açık birim:**
    - Boş / Kısmen Dolu / Dolu.
    - Adet oranına dolu/K kesri, m² oranına Σ fiziksel m² girer (≤ P).
    - "Dolmak üzere" sayacına girmez; ayrı işaret olarak "N sözleşme X gün içinde bitiyor" gösterilir.
  - **Paylaşımlı birim:** Bugünkü hesap aynen (adet 1, m² P).
  - Yalnız bugün aktif sözleşmeler sayılır.
- **DTO ve ekranlar:**
  - DTO'lar: `PropertyListItemDto`'ya `PartiallyLeasedUnitCount`, kesirli adet, Σ fiziksel m², toplam/dolu kontenjan eklenir. `PropertyDetailDto.Units` ve `UnitListItemDto`/`UnitDetailDto` "6/10 kişi · 42/70 m²" özetini taşır.
  - `HomeController` + `DashboardViewModel`:
    - Kesirli adet oranı ve m² oranı.
    - Yeni kontenjan göstergesi (toplam dolu / toplam kontenjan).
    - `PropertyOccupancies` için Dolu · Kısmen dolu · Dolmak üzere · Boş.
  - `_DurumBadge.cshtml` ve `Property/Details.cshtml` (yerel `StatusBadge`, U4): "Kısmen Dolu" rozeti ve özet.
- **Kiracı paneli:** `TenantLease/Details.cshtml` (:22, :125, :426) ve DTO.
  - Rozet sözleşmenin kendi bitiş tarihinden hesaplanır.
  - Gösterilen m²: `LeaseArea.RentedArea` varsa o, yoksa anlık birim alanı.
  - Paylaşımlıda "60 m²'lik birimin 20 m²'si, paylaşımlı" (60 anlık bilgiden okunur).
  - Açık birimde yalnız "8 m²"; toplam alan, ortak alan, kontenjan ve diğer kiracılar yok, fiziksel m² gösterilmez.
- **Endpoint/izin:** yok.
- **Testler:**
  - `Unit/Leases/LeaseOccupancyPolicyTests` (açık birim durumları).
  - Yeni `Integration/Properties/PropertyOccupancyTests`:
    - 3 dolu normal + 1 açık (K=10, 6 dolu) → Dolu 3 · Kısmen 1 · Dolmak üzere 0 · Boş 0
    - O-1 10/10 ve 2 sözleşmesi 30 gün içinde bitiyor → Dolu sayacında
    - adet oranında 0,6, m² oranı ≤ %100
  - `Architecture/Tenants/TenantLeaseArchitectureTests`: Kiracı DTO'sunda fiziksel m² ve diğer kiracı bilgisi yok.
- **Ajan:** 2 yazma + 2 denetçi.
  - Yazma A: repository, policy, Home, Property ekranları.
  - Yazma B: kiracı paneli.
- **Sonuç:**
  - Liste sayaçları `PropertyRepository.ApplyOccupancyAsync` içinde bellekte hesaplanıyor (sayfa başına 1 ek sorgu).
  - Yeni alanlar:
    - `PropertyListItemDto`: `PartiallyLeasedUnitCount`, `OccupiedUnitEquivalent`, `LeasedPhysicalArea`, `TotalCapacity`, `OccupiedCapacity`.
    - `UnitDetailDto`/`UnitListItemDto`: `OccupiedCount`, `Capacity`, `OccupiedPhysicalArea`, `ExpiringLeaseCount`.
    - `LeaseDetailDto`: `RentedArea`, `LeaseGroupId`.
  - "Kısmen Dolu" rozeti `badge-bireysel` ile gösteriliyor.
  - Kullanıcı kararıyla (2026-11-18) taşınmaz listesine (`Property/Index.cshtml`) de "N Kısmen Dolu" rozeti eklendi.
  - Tam paket 1129/1129.
  - Denetim: A ve B onay; son denetim tek turda Property/Index bulgusuyla RED, düzeltme sonrası ONAY.
- **Açık notlar (plan sonunda bakılacak):**
  - Mülk ve birim repository'lerinde "dolmak üzere" eşiği sabit 30 gün. `StatisticsService`'in kullandığı `LeaseExpiringSoonStatusDays` ayarına bağlı değil (eski durum).
  - Birim kartındaki dolu m² (`OccupiedPhysicalArea`) birim alanıyla sınırlanmıyor. Tahsis toplamı alanı aşarsa "80/70 m²" görünebilir; oranlar sınırlı.
  - `LeasedPhysicalArea` dolduruluyor ama hiçbir ekranda kullanılmıyor.
  - Birimler sayfasındaki `data-durum` filtresi `ActiveLeaseId`'ye bağlı; kısmen dolu birim "kiralı" filtresine düşüyor.
  - Tarayıcıda denenmedi.

### F9 — Son doğrulama — 1–5 tamamlandı (2026-11-18); 6 kullanıcıyla değerlendiriliyor
- **Sonuç:**
  - Build 0 hata. Tam paket 1132/1132.
  - Birleşik denetim (a09221f..273d6ee) tek bulguyla RED verdi: alan/tür kilidi Ended sözleşmeye bakmıyor, uzatma Ended'i kabul ediyor. Bu yüzden birim değiştirilip sözleşme uzatılabiliyordu. Örnek: 60 m² birimde 30+30 grubu sona erer, alan 80 yapılır, grup uzatılır; 6.000 TL aidatın yalnız 4.500 TL'si kesilir.
  - Kullanıcı kararı (2026-11-18), kilit kuralı değişmedi. Uzatmada (`LeaseApplicationRules.EnsureExtensionAllowedAsync`) iki kontrol eklendi:
    - Birimin kiralama biçimi `RentalModeSnapshot`'tan farklıysa ret: `Lease.UnitRentalModeChanged` (tekil + grup).
    - Grup sözleşmesinde `Unit.Area` ≠ `UnitAreaSnapshot` ise ret: `Lease.UnitAreaChanged`.
  - 3 test eklendi. Düzeltme denetimi ONAY; birleşik denetim ikinci turda ONAY.
  - Production'a uygulanacak migration'lar: `20261116102739_AddLeaseExpirationActivityType`, `20261117123255_AddSharedAndCapacityUnits`, `20261117143035_RemoveLeaseOpenApplicationUniqueIndex`.
  - Manuel tarayıcı kontrolü kullanıcıda.
1. Ana oturum tam paketi çalıştırır: `dotnet build KiraTakip.slnx` + `dotnet test`.
2. Birleşik değişiklik için yeni bir `orch-reviewer` (opus) çalışır; RED gelirse en fazla 2 tur.
3. `PROGRESS.md` güncellenir ve kaynak dokümanda "Sonraki adım" kapatılır.
4. Production notu: `AddSharedAndCapacityUnits` (ve bekleyen `AddLeaseExpirationActivityType`) production'a uygulanmalı.
5. Manuel tarayıcı kontrolü kullanıcıdadır.
6. Fazlardan kalan açık notlar kullanıcıyla birlikte tekrar değerlendirilir (2026-11-17 kullanıcı kararı): F5 "Sonraki fazlar için not", F6, F7a, F7b ve F8 "Açık notlar" maddeleri.

## 4. Doğrulama maddesi → test eşlemesi
| Dokümandaki madde | Test | Faz |
|---|---|---|
| Kapasite kuralları (en yoğun gün, k, son kontenjan, K artışı/azaltışı) | `UnitCapacityPolicyTests`, `CapacityLeaseServiceTests`, `UnitCapacityChangeTests` | F1, F6, F4 |
| Paylaşımlı grup aktivasyonu ve yeniden yapılanma ön koşulu | `LeaseGroupPolicyTests`, `LeaseGroupServiceTests`, `LeaseGroupLifecycleTests` | F1, F7a, F7b |
| m² çarpanı ve oran hesapları | `LeaseRatePolicyTests`, `LeaseAreaChargeGenerationTests` | F1, F5 |
| Tüm birimlerde taslağın çakışmaya girmesi | `LeaseDraftOverlapTests`, `CapacityLeaseServiceTests` (taslak yer tutar), `LeaseGroupServiceTests` (grup hariç tutma) | F3, F6, F7a |
| Birim türü özellik kilidi ve tek birim türü değişikliği | `UnitTypeServiceTests`, `PropertyUnitLockTests` | F4 |
| Birim anlık bilgisinin korunması | `LeaseUnitSnapshotBackfillTests`, `LeaseUnitSnapshotTests` | F2, F3 |
| Tek açık başvuru kuralının üç birim türünde davranışı | Normal: mevcut testler + `LeaseApprovalServiceTests`; kontenjan: `CapacityLeaseServiceTests`; paylaşımlı: `LeaseGroupServiceTests` | F3, F6, F7a |
| Görev ayrılığının kaldırılması (tekil ve grup) | `LeaseApprovalServiceTests`, `LeaseGroupServiceTests` | F3, F7a |
| Birim alanı kilidi (taslak dahil, tek birimde kapalı/açık alan) | `PropertyUnitLockTests` | F4 |

Testler DB'yi kendisi migrate etmez. "DB güncelle" adımı F2 sonunda ana oturumda koşulur. Sonraki fazlarda şema değişmez; değişirse aynı adım tekrarlanır.

## 5. Riskler
- **R1 — Çakışma kontrolünün genişlemesi** (D3): Onay sırasında `ValidateApplicationDataAsync` kendi sözleşmesini hariç tutuyor ama grup kardeşlerini tutmuyor; F7a'da grup hariç tutma eklenmezse grup hiç onaylanamaz. Mevcut testlerde taslak verisi bırakan senaryolar `Lease.ActiveUnitConflict` alabilir; F3'te tam paket koşulur.
- **R2 — Paylaşılan test DB'si:** Testler `KiraTakipDb_Test` üzerinde kalıntı bırakabiliyor (önceden `BankTransactionMatchingTests` izole edildi). Yeni testler kendi birim ve taşınmazlarını oluşturur, sabit Id kullanmaz.
- **R3 — Tahakkuk tutarı regresyonu:** Çarpan değişikliği tüm sözleşmeleri etkiler. `LeaseArea` olmayan sözleşmede formül bugünküyle birebir aynı kalmalı; F5'te regresyon testi var.
- **R4 — Ayar seed belirsizliği (U6):** Yeni ayar kendi `InsertData`'sıyla gelir. Production'da SeedId çakışması olmaması için mevcut en büyük SeedId'nin üstü kullanılır.
- **R5 — F7 büyüklüğü:** En büyük faz; iki alt faza bölündü. Grup onayında tahakkuk üretimi sözleşme başına ayrı çağrıdır; biri hata verirse kısmi durum oluşmaması için önce tüm doğrulamalar, sonra durum değişikliği ve tek `SaveChanges`, en son tahakkuk üretimi yapılır (mevcut tekil akışla aynı sıra).
- **R6 — Ondalık ayar:** Kültür farkı (`8,5` / `8.5`) normalize edilmezse kapasite kuralları yanlış çalışır; F1 testinde iki giriş de denenir.
- **R7 — Var olan izin ihlali:** `UpdateDraft` / `ResubmitRevision` / `UploadDraftDocument` `Lease.Create` iznini paylaşıyor. Bu kapsam dışı ve değişmiyor.

## 6. Kapsam dışı (plana alınmadı)
Kaynak dokümandaki "Kapsam dışı" bölümü aynen geçerlidir:
- depozito iade özelliği (yalnız dummy servis)
- kontenjan tarihçesini okuyan rapor
- hot-desk
- normal birimlerde dolu birimlerin formda görünmemesi
- `Ended` uzatmada ara dönem çakışması
- `Lease.Create` izin paylaşımı

Ek olarak:
- U5 (30 gün sabiti)
- U8 (operasyon rolünde `DeleteDraft`)
- U9 (iç sözleşme rozeti)
