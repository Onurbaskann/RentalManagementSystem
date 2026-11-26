# Refactor — Alan hiyerarşisi (uygulama spec'i)

> **Durum:** Spec (F0 kapanışı). Kod değişikliği yok. Uygulama Onur onayıyla F1'den başlar.
> **Dal / klasör:** `alan-hiyerarsisi` dalı, `D:\Software\RentalManagementSystem\KiraTakip-alan` (git worktree).
> **Veritabanı:** yalnız `DBTeknoPark_Test` (bkz. `CLAUDE.md` başı). Başka DB'ye migration uygulanmaz, uygulama ve testler başka DB'ye bağlanmaz.
> **Kaynak (kararların tam metni):** Vault `D:\Vault\projects\UniTeknoPark\Alan-Hiyerarsisi-Analizi.md` rev. 8 (K1–K70) ve sayfası `Alan-Hiyerarsisi-Analizi.html`. Codex çapraz kontrolünün ham kaydı: `Alan-Hiyerarsisi-Codex-Tartismasi.md`. Bu spec kararları değiştirmez; çelişki görülürse analiz geçerlidir ve Onur'a sorulur.
> **Tarih:** 2026-11-26. İlk taslak (2026-11-24, A1–A9) bu dosyayla değiştirildi.

## İçindekiler

1. Okuma kuralları ve işaretler
2. Kararların özeti (K1–K70)
3. Adlandırma ve sözlük
4. Şema
5. Domain kuralları (policy'ler)
6. Servis akışları ve kilit protokolü
7. Mevcut kod → yeni karşılık
8. İzinler
9. Ekranlar
10. Migration, veritabanı ve seed
11. Fazlar (F1–F6)
12. Test planı
13. Spec'in teknik tercihleri, riskler, doğrulanamayanlar

---

## 1. Okuma kuralları ve işaretler

- `mevcut:` bugün kodda olan ad/dosya. `yeni:` bu refactor'da eklenecek ad. Yeni C# adları **öneridir**; uygulamada değişirse spec güncellenir.
- Kod referansları `path:line` biçimindedir; satırlar dalın başlangıç commit'i `f023cba` için geçerlidir. `src/` = `src/KiraTakip.*`.
- `⚠ doğrulanmadı` işaretli satırlar okuma ajanlarınca teyit edilemedi; uygulamadan önce koda bakılır.
- Tablo/kolon adları Türkçe (`[Table]`/`[Column]`), C# adları İngilizce (proje deseni).
- Her kural bir K numarasına bağlıdır. Spec'in kendi teknik tercihleri **S-n** ile işaretlenir (liste §13.1).

## 2. Kararların özeti (K1–K70)

Tam metin analizde (§0). Uygulayıcının sık bakacağı kısa hali:

| K | Kısa kural |
|---|---|
| K1–K6 | Derinlik serbest; her seviye kiralanabilir; özellikler; açık ofis → masa alt alanları; sözleşme çok alanlı; tür yalnız etiket, hiyerarşi kuralı yok |
| K7 | Tür sonradan değişir; geçmiş anlık kopya + tarihçe + dondurulmuş tutarlarla korunur |
| K8, K10, K20, K21, K24 | Ön tahsis: sözleşme gibi engeller; isteğe bağlı; süreyi kullanıcı girer (üst sınır yok); onay/ücret yok, yalnız iç kullanıcı; kiracıya başka alan için taslak açılınca kaldırılır |
| K9, K51 | Tarife onay günü dondurulur; yalnız uzatma ve yeniden üretimde yeniden bulunur |
| K11 | Kullanım: Kiralanabilir / Rezerve edilebilir / Kiralanamaz |
| K12, K26, K27, K46 | Paylaşımlı alan grup sözleşmeyle; pay toplamı = alan m²; grup sözleşmesi yalnız o alanı içerir; fiyat her üyede ayrı dondurulur |
| K13, K29 | Adres `AlanAdresleri` (kökte zorunlu); tek `Yuzolcumu`; kat sayısı kalkar |
| K14 | Yetki kapsamı alt alanlara geçer, üste geçmez |
| K15 | İlk sürümde taşıma yok |
| K16, K60 | Geçmişi olan alan silinmez, pasifleşir; "geçmiş" = sözleşme/ön tahsis/rezervasyon/fatura; alt alanı olan silinmez; bağlı tanımlar listelenip birlikte silinir |
| K17 | Üst alan m²'si elle; alt alanların toplamı üst alanın m²'sini aşarsa uyarı (Onur 2026-11-26, Codex B10) |
| K18 | Kiralanamaz → Kiralanabilir, açık kayıt varken engellenir |
| K19 | Ekran: kök listesi → ağaç; sözleşmede ağaç seçici |
| K22 | Kullanımdaki tür silinmez, pasifleşir |
| K23, K48 | Üst alan bütün kiralanınca altındaki Rezerve edilebilir/Kiralanamaz alanlar sözleşmeye girmez; salon herkese açık kalır |
| K25 | Özellikler ortak katalogdan; tür yalnız öneri |
| K28, K69 | Rezervasyon kuralı yalnız alana bağlı, alta miras; borç tipi kuralda; rezervasyona ücretle birlikte kopyalanır |
| K30, K39, K40 | `SozlesmeTarifeler` alan × borç tipi; kalem = borç tipi × KDV oranı; kırılım `TahakkukKalemDetaylari`; yuvarlama detayda |
| K31 | Sözleşme farklı köklerden alan içerebilir |
| K32, K33, K47, K49, K56 | Görünürlük: sözleşmedeki **tüm** alanlara yetkili olan görür (kiracı kullanıcı dahil); rapor alan payı; tahsilat KDV dahil orana göre bölünür |
| K34, K35 | Aynı borç tipi farklı mağazaya düşüyorsa onay ve yönlendirme kaydı engellenir |
| K36, K37 | Tarife anahtarı alan × kategori × borç tipi; genel tarifede yalnız onay yılı |
| K38, K54, K64 | Uzatmada yeni satır; geçerlilik ayın 1'i; aynı ay satırı güncellenir; ileri tarihli satır korunur |
| K41 | Onaylı sözleşmeye alan eklenmez/çıkarılmaz |
| K42, K43 | Kök kilidi (`sp_getapplock`); ata/torun `Yol` ile, derinlik ≤ 20 |
| K44, K45 | `SozlesmeAlanlari` dönüşür; kontenjan kodu silinir; çok alanlı taslakta ön tahsis dönüş/kaldırma |
| K50, K61, K62 | Ayrı dal; faz başına ek migration; migration yalnız şema; dev/test verisi silinip dummy seed |
| K52 | Aynı sözleşmede alan + atası/torunu olamaz |
| K53, K66 | Yeniden üretimde ödemesi olan ay korunur; kesin reddedilmiş kart denemeli ay izli iptal + yeni tahakkuk |
| K55 | Tarifesi bulunamayan kalem listelenir; KIRA yoksa onay engellenir; kiracı kategorisi zorunlu; yıla düşme yok |
| K57 | Elle borç tek alan; sözleşmeliyse detay satırında |
| K58 | Paylaşımlı alana ön tahsis yok |
| K59 | Feshedilen fesih tarihine, Sona erdi bitişe kadar tutar |
| K63 | Fesihte kısmi ödenmiş gelecek ayın iptali hatası F3'te düzelir |
| K65 | Yetki dışı ön tahsisi kaldıracak taslak engellenir |
| K67 | Açık sözleşmesi olan alanın m²'si değişmez |
| K68 | İç içe Rezerve edilebilir alan yok; saat çakışması yalnız aynı alanda, onaylılar arasında, `[başlangıç, bitiş)` |
| K70 | "Kesilmeyecek" = `KaynakTipi` 4 satırı |

**Dış teyit bekleyen** (uygulamayı durdurmaz; F3/F4 kapanmadan teyit): K39, K40, K49 (muhasebe), K48 (operasyon).

## 3. Adlandırma ve sözlük

| Kavram | Tablo | C# |
|---|---|---|
| Alan | yeni `Alanlar` | yeni `Space` |
| Alan türü (etiket) | yeni `AlanTurleri` | yeni `SpaceType` |
| Alan adresi | yeni `AlanAdresleri` | yeni `SpaceAddress` |
| Alan tarihçesi | yeni `AlanTarihcesi` | yeni `SpaceHistory` |
| Özellik tanımı / değeri / tür önerisi | yeni `AlanOzellikTanimlari`, `AlanOzellikDegerleri`, `AlanTuruVarsayilanOzellikleri` | yeni `SpaceAttributeDefinition`, `SpaceAttributeValue`, `SpaceTypeDefaultAttribute` |
| Alan tarifesi | yeni `AlanTarifeleri` | yeni `SpaceRate` |
| Ön tahsis | yeni `OnTahsisler` | yeni `PreAllocation` |
| Sözleşme alanı | mevcut `SozlesmeAlanlari` (dönüşür) | mevcut `LeaseArea` (ad korunur) |
| Sözleşme tarifesi | mevcut `SozlesmeTarifeler` (genişler) | mevcut `LeaseRateOverride` → yeni `LeaseRate` (S-1) |
| Fatura kalem detayı | yeni `TahakkukKalemDetaylari` | yeni `ChargeLineItemDetail` |
| Kullanım | kolon `Kullanim` | yeni enum `SpaceUsage { Rentable = 1, Reservable = 2, NotRentable = 3 }` (mevcut `UnitTypeUsage` ile aynı değerler, `src/KiraTakip.Domain/Models/Enums/Enums.cs:59`) |

**Silinen mevcut sınıflar:** `Property`, `Unit`, `PropertyType`, `UnitType`, `UnitRate`, `PropertyRateOverride`, `UnitCapacityHistory`, `LeaseAreaHistory`. **Silinen enum'lar:** `UnitStructure` (`Enums.cs:45`), `UnitRentalMode` (`:201`), `LeaseAreaMode` (`:207`), `LeaseAreaChangeReason` (`:213`); `UnitTypeUsage` (`:59`) → `SpaceUsage`. `ScopeType` (`Enums/ScopeType.cs:3`, `Property = 1, Unit = 2`) → tek değer `Space = 1` (S-2).

## 4. Şema

Tüm yeni entity'ler `BaseEntity`'den türer (`src/KiraTakip.Domain/Models/Entities/BaseEntity.cs`: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt?`, `UpdatedBy?`, `IsDeleted`, `IsActive` → `[Column("Aktif")]`). Soft delete global filtreyle gelir (`ApplicationDbContext.cs:160-169`). `SaveChanges` override yalnız Added/Modified işler; `RemoveRange` **gerçek silmedir** (yeniden üretimde önemli, §6.5). "Silinmemişler" filtresi = `[IsDeleted] = 0`.

Entity dosyaları `src/KiraTakip.Domain/Models/Entities/`, konfigürasyonlar `src/KiraTakip.Infrastructure/Persistence/Configurations/<Klasör>/` altına (mevcut desen).

### 4.1 `Alanlar` — `Space` (yeni)

| C# | Kolon | Tip | Null | Not |
|---|---|---|---|---|
| `ParentId` | `UstAlanId` | int | ✔ | FK `Alanlar`, Restrict. Boş = kök. Check `CK_Alanlar_UstAlan`: `[UstAlanId] IS NULL OR [UstAlanId] <> [Id]` |
| `SpaceTypeId` | `AlanTuruId` | int | | FK `AlanTurleri`, Restrict |
| `Code` | `Kod` | nvarchar(50) | | Unique `(UstAlanId, Kod)` silinmemişler + kökler için ayrı filtreli unique `UX_Alanlar_Kok_Kod` (`[UstAlanId] IS NULL AND [IsDeleted] = 0`; NULL'lar filtreli unique'te tekil sayılmadığı için) |
| `Name` | `Ad` | nvarchar(200) | | |
| `SortOrder` | `Sira` | int | | |
| `Description` | `Aciklama` | nvarchar(1000) | ✔ | |
| `Path` | `Yol` | varchar(450) | | `/1/5/12/`; collation `Latin1_General_BIN2`; index `IX_Alanlar_Yol`. 20 seviye × (10 hane + `/`) ≈ 221 karakter; 450 index sınırının altında (K43) |
| `Depth` | `Derinlik` | int | | Kök 0; check `[Derinlik] BETWEEN 0 AND 19` (azami 20 seviye, K43) |
| `Area` | `Yuzolcumu` | decimal(18,2) | | m² (K29); check `> 0`. Açık sözleşmesi olan alanda değişmez (K67) |
| `Usage` | `Kullanim` | int (`SpaceUsage`) | | |
| `IsPreAllocatable` | `OnTahsiseAcik` | bit | | |
| `IsShared` | `Paylasimli` | bit | | |
| `IsActive` | `Aktif` | bit | | BaseEntity; pasifleştirme alt ağaca yayılır (K16) |

Check `CK_Alanlar_Bayraklar` (analiz §2.2): `(([OnTahsiseAcik] = 0 AND [Paylasimli] = 0) OR [Kullanim] = 1) AND NOT ([OnTahsiseAcik] = 1 AND [Paylasimli] = 1)` (K11, K58).

`Yol` ve `Derinlik` ekleme sonrası **aynı transaction ve kök kilidi altında** yazılır: satır geçici `Yol` ile eklenir, `SaveChanges` ile Id alınır, sonra `Yol = ustYol + Id + "/"` güncellenir (K43). Ara durum dışarıdan görünmez.

Servis kuralları (DB'de değil): kullanım geçişleri (§5.4), K68 iç içe Rezerve edilebilir yasağı, pasif üstün altında aktif alan olmaması, kökte adres zorunluluğu.

### 4.2 Diğer yeni tablolar

**`AlanTurleri` — `SpaceType`**: `Kod` (nvarchar(50), unique), `Ad`, `Sira`, `VarsayilanKullanim` (`DefaultUsage`, `SpaceUsage`), `VarsayilanOnTahsis` (`DefaultIsPreAllocatable`, bit), `Aktif`. Kullanımdaki tür silinmez, pasifleşir (K22).

**`AlanAdresleri` — `SpaceAddress`**: `AlanId` (unique, FK Cascade), `Il`, `Ilce`, `Mahalle`, `AcikAdres` (bugün `Tasinmazlar`'da, `Property.cs`).

**`AlanTarihcesi` — `SpaceHistory`**: `AlanId` (FK Restrict), `Alan` (`Field`, enum `SpaceHistoryField { Created = 1, Type, Name, Parent, Area, Usage, PreAllocatable, Shared, Active }`), `EskiDeger`, `YeniDeger` (nvarchar(500); tür ve üst alan için **ad**), `Tarih`, `KullaniciId`. Silme kararında "geçmiş" sayılmaz (K60).

**`AlanOzellikTanimlari` — `SpaceAttributeDefinition`**: `Kod` (unique), `Ad`, `VeriTipi` (`AttributeDataType { Text = 1, Number = 2, Boolean = 3, List = 4 }`), `Secenekler` (nvarchar(max), yalnız List), `Aktif`.
**`AlanOzellikDegerleri` — `SpaceAttributeValue`**: `AlanId` (FK Cascade), `OzellikTanimId` (FK Restrict), `Deger` (nvarchar(1000)); unique `(AlanId, OzellikTanimId)`.
**`AlanTuruVarsayilanOzellikleri` — `SpaceTypeDefaultAttribute`**: `AlanTuruId` (FK Cascade), `OzellikTanimId` (FK Restrict); unique çift. Yalnız formda öneri (K25).

**`AlanTarifeleri` — `SpaceRate`** (`BirimTarifeler` + `TasinmazTarifeler` birleşir, K36): `AlanId` (FK Cascade), `KiraciKategoriId`, `BorcTipiId` (FK Restrict), `HesaplamaYontemi` (`CalculationMethod { Fixed = 1, M2 = 2 }`), `BirimDeger` decimal(18,4) (bugünkü `BirimTarifeler` hassasiyeti), `KdvOrani`. Unique `(AlanId, KiraciKategoriId, BorcTipiId)` silinmemişler. Değer check'i bugünkü `CK_BirimTarifeler_Degerler` ile aynı mantık (`Configurations/Pricing/UnitRateConfiguration.cs`). Yıl yok (K37). Dondurulmuş satırın kaynağı olduğu için silme soft delete'tir (S-3).

**`OnTahsisler` — `PreAllocation`**:

| C# | Kolon | Tip | Null | Not |
|---|---|---|---|---|
| `SpaceId` | `AlanId` | int | | FK Restrict |
| `TenantId` | `KiraciId` | int | | FK Restrict |
| `StartDate`, `EndDate` | `BaslangicTarihi`, `BitisTarihi` | date | | Düşünülen sözleşme dönemi; check sıra |
| `HoldUntil` | `TahsisBitis` | datetime2 | | Son an, iş saat dilimi Europe/Istanbul (§6.7) |
| `Status` | `Durum` | int | | `PreAllocationStatus { Active = 1, Converted = 2, Removed = 3, Expired = 4, Cancelled = 5 }` |
| `LeaseId` | `SozlesmeId` | int | ✔ | FK Restrict; check `[Durum] <> 2 OR [SozlesmeId] IS NOT NULL` |
| `StatusReason` | `DurumNedeni` | nvarchar(500) | ✔ | Ör. "Başka alan için taslak: SZ-…" |
| `StatusChangedAt`, `StatusChangedBy` | `DurumTarihi`, `DurumDegistiren` | | ✔ | |

Index `(AlanId, Durum, BaslangicTarihi, BitisTarihi)` ve `(KiraciId, Durum)` silinmemişler. **Aktif ön tahsis** = `Durum = 1 AND islemAni < TahsisBitis` (analiz §13 C10); eşitlikte bitmiştir. Arka plan servisi yalnız durumu `Expired` yapar.

**`TahakkukKalemDetaylari` — `ChargeLineItemDetail`** (K30):

| C# | Kolon | Tip | Null | Not |
|---|---|---|---|---|
| `ChargeLineItemId` | `TahakkukKalemId` | int | | FK **Cascade** |
| `LeaseAreaId` | `SozlesmeAlanId` | int | | FK Restrict |
| `LeaseRateId` | `SozlesmeTarifeId` | int | ✔ | FK Restrict; sözleşmeli elle borçta boş (K57) |
| `CalculationMethod`, `UnitValue`, `Multiplier`, `KdvRate` | `HesaplamaYontemi`, `BirimDeger`, `Carpan`, `KdvOrani` | | | O ay gerçekten uygulanan değerler |
| `Amount`, `KdvAmount` | `Tutar`, `KdvTutari` | decimal(18,2) | | Detay başına kuruşa yuvarlanır (K40) |

Unique `(TahakkukKalemId, SozlesmeAlanId)`. "Σ detay = kalem" servis kuralı + test (§12).

### 4.3 Değişen tablolar

| Tablo (C#) | Değişiklik |
|---|---|
| `SozlesmeAlanlari` (`LeaseArea`; config `Configurations/Leases/LeaseAreaConfiguration.cs`) | **Kalkar:** 1:1 ilişki (`Lease.LeaseArea`, `:14`), unique `UX_SozlesmeAlanlari_SozlesmeId_Silinmemis` (`:16-17`), `FizikselAlan` (`AllocatedArea`), `HesaplamaModu` (`AllocationMode`), `CK_SozlesmeAlanlari_Alanlar` (`:18`). **Eklenir:** `AlanId` (FK Restrict), unique `(SozlesmeId, AlanId)` silinmemişler, `AnlikKod`, `AnlikAd`, `AnlikTurAdi`, `AnlikKullanim`, `AnlikYuzolcumu`, `AnlikYol` (ad zinciri, "A Blok / Kat 1 / Ofis 102"). `KiralananAlan` (`RentedArea`) kalır, check `> 0`. Navigation `Lease.Areas` (1-N) (K44) |
| `Sozlesmeler` (`Lease`) | **Kalkar:** `BirimId` (`UnitId`), `IX_Lease_UnitId` (`LeaseConfiguration.cs:32`), `AnlikBirimTuruAdi`, `AnlikKullanimTuru`, `AnlikKiralamaBicimi`, `AnlikBirimYuzolcumu` (`Lease.cs:16-25`) |
| `SozlesmeTarifeler` (`LeaseRateOverride` → `LeaseRate`) | **Kalkar:** unique `(SozlesmeId, BorcTipiId)`. **Eklenir:** `SozlesmeAlanId` (FK Restrict; aynı sözleşmenin alanı olmalı — servis kuralı), `GecerlilikBaslangici` (date; check `DAY([GecerlilikBaslangici]) = 1`, K54), `KiraciKategoriId` (FK Restrict, zorunlu), `KaynakTipi` (`LeaseRateSource { LeaseSpecific = 1, SpaceRate = 2, GeneralRate = 3, NotCharged = 4 }`), `KaynakAlanId`, `KaynakAlanTarifeId`, `KaynakGenelTarifeId` (FK Restrict), `DondurmaTarihi`. `HesaplamaYontemi`, `BirimDeger`, `KdvOrani` **nullable**. Unique `(SozlesmeAlanId, BorcTipiId, GecerlilikBaslangici)` silinmemişler. Check `CK_SozlesmeTarifeler_Kaynak`: 1 → üç kaynak FK'sı boş, değerler dolu; 2 → `KaynakAlanId` + `KaynakAlanTarifeId` dolu, `KaynakGenelTarifeId` boş, değerler dolu; 3 → `KaynakGenelTarifeId` dolu, alan kaynakları boş, değerler dolu; 4 → değerler ve üç kaynak FK'sı boş (K70). `CK_SozlesmeTarifeler_Degerler` nullable'a göre yeniden yazılır |
| `Tahakkuklar` (`Charge`; config `Configurations/Charges/ChargeConfiguration.cs`) | `BirimId` (zorunlu, index `:46`) → `AlanId` (`SpaceId`) **nullable**; yalnız rezervasyon faturası ve sözleşmesiz elle borçta dolu (K32, K57). **Eklenir (K66, S-4):** `YenidenUretimleIptal` (`IsReplacedByRegeneration`, bit, default 0) ve yeni tahakkukta `DegistirilenTahakkukId` (`ReplacesChargeId`, int?, FK self Restrict, filtreli unique `IS NOT NULL`). Dönem tekilliği `(SozlesmeId, DonemBaslangici)` filtresine (`:39-42`, bugün `SozlesmeId NOT NULL AND KaynakTipi=1 AND IsDeleted=0`) `AND [YenidenUretimleIptal] = 0` eklenir |
| `TahakkukKalemleri` (`ChargeLineItem`) | Unique `(TahakkukId, TahakkukTipiId, KdvOrani)` silinmemişler (bugün yalnız `TahakkukId` index'i, `ChargeLineItemConfiguration.cs:31`). `HesaplamaYontemi`, `BirimDegeri`, `Carpan` **nullable** (K39); 3 check (`:35-37`) güncellenir |
| `RezervasyonTarifeler` (`ReservationRateOverride`) | `BirimId` (nullable, SetNull) → `AlanId` **zorunlu**, FK Cascade, unique silinmemişler. **Kalkar:** `BirimTuruId`, `Yil`, `UX_RezervasyonTarifeler_BirimTuruYil_GenelKural`, `CK_RezervasyonTarife_BirimOrYilTuru`. **Eklenir:** `BorcTipiId` (FK Restrict, zorunlu; kayıtta aktif + `ReservationSpecific`, K69) |
| `Rezervasyonlar` (`Reservation`) | `BirimId` → `AlanId` (Restrict); index `(AlanId, BaslangicTarihi, BitisTarihi, Durum)` (`ReservationConfiguration.cs:55`). **Eklenir:** `BorcTipiId` (FK Restrict). Ücretin hesaplandığı her an (oluşturma, talep, güncelleme) kuraldan kopyalanır (K69). Ücret talep anında da hesaplanıyor (`ReservationService.cs:427`, `CreateRequestAsync` içinde) → `BorcTipiId` **NOT NULL** |
| `SozlesmeGruplari` (`LeaseGroup`) | `BirimId` → `AlanId` (Restrict) |
| `OdemeMagazaYonlendirmeleri` (`PaymentStoreRouting`) | `TasinmazId`, `BirimId` → tek `AlanId` (nullable; boş = genel). 3 kısmi unique (`PaymentStoreRoutingConfiguration.cs:29-39`) → 2: `(BorcTipiId)` `AlanId IS NULL` ve `(BorcTipiId, AlanId)` `AlanId IS NOT NULL` (bugünkü `Aktif = 1, IsDeleted = 0` filtreleriyle). `CK_OdemeMagazaYonlendirmeleri_Kapsam` (`:48`) kalkar |
| `KullaniciYetkiKapsamlari` (`UserPermissionScope`) | `KapsamTipi` tek değer (`Space = 1`); `KapsamId` = `AlanId`. FK eklenir (Restrict; alan silinirken servis kapsam satırlarını siler, §6.2) (S-2). Unique `(UserId, KapsamTipi, KapsamId)` (`:13`) kalır |
| `Davetiyeler` (`Invitation`) | `TasinmazIds`, `BirimIds` → `AlanIds`; `TumTasinmazlaraErisim` → `TumAlanlaraErisim` |
| `AspNetUsers` (`ApplicationUser.cs:15`) | `TumTasinmazlaraErisim` → `TumAlanlaraErisim` (C# `HasAccessToAllSpaces`) |
| `Kiracilar` (`Tenant`) | `KiraciKategoriId` **NOT NULL**; FK SetNull → Restrict (`TenantConfiguration.cs:27`) (K55) |
| `GenelTarifeler` (`RateSchedule`) | Şema değişmez; yıla düşme kalkar (§6.4) |

### 4.4 Silinen tablolar

`Tasinmazlar`, `Birimler`, `TasinmazTipleri`, `BirimTurleri`, `BirimTarifeler`, `TasinmazTarifeler`, `BirimKontenjanGecmisleri`, `SozlesmeAlanGecmisleri` ve bunlara bağlı FK/index/check'ler.

### 4.5 Örnek (Firma X, sözleşme 7, Mart)

| Tablo | Satır |
|---|---|
| `SozlesmeAlanlari` | 31: Ofis 102 · 80 m² — 32: Masa 2 · 10 m² |
| `SozlesmeTarifeler` | 51: alan 31 · KIRA · m² · 400 · KaynakTipi 2 (Kat 1) · 2027-01-01 — 52: alan 32 · KIRA · m² · 250 · KaynakTipi 2 (Açık Ofis) · 2027-01-01 |
| `TahakkukKalemleri` | 700: KIRA · KDV %20 · 34.500 (ödeme buna) |
| `TahakkukKalemDetaylari` | 700/31/51: 400 × 80 = 32.000 — 700/32/52: 250 × 10 = 2.500 |

## 5. Domain kuralları (policy'ler)

Mevcut desen: `src/KiraTakip.Domain/<Konu>/<Ad>Policy.cs`, `static` sınıf, saf fonksiyon, ihlal listesi döner (örn. `Leases/LeaseGroupPolicy.cs:15` `GetViolations`). Yeni policy'ler DB'ye dokunmaz; gereken veriyi servis okuyup parametre verir. Her policy'nin birim testi zorunlu (§12).

### 5.1 `SpaceTreePolicy` (yeni, `Domain/Spaces/`)

- `string BuildPath(string? parentPath, int id)` → `"/1/5/12/"`.
- `IReadOnlyList<int> GetAncestorIds(string path)` (kendisi hariç, kökten aşağı sıralı).
- `bool IsAncestorOrSelf(string ancestorPath, string path)` = `path.StartsWith(ancestorPath, Ordinal)`.
- `int MaxDepth = 19` (0 tabanlı, 20 seviye, K43); `bool CanAddChild(int parentDepth)`.
- `IReadOnlyList<(int A, int B)> FindNestedPairs(IEnumerable<(int Id, string Path)> spaces)` → K52: aynı sözleşmede ata/torun çiftleri.
- `int GetRootId(string path)`.

### 5.2 `SpaceOccupancyPolicy` (yeni, `Domain/Spaces/`; mevcut `Leases/LeaseOccupancyPolicy.cs` `DetermineStatus`/`DetermineCapacityStatus` yerine)

Girdi kayıtları servis tarafından **tarih çakışan** olarak getirilir:

```text
OccupancyRecord(Kind: Lease|PreAllocation, Id, SpaceId, SpacePath, TenantId, LeaseGroupId?, From, To)
```

- **Sözleşme tutma aralığı** (`LeaseHoldRange(status, start, end, terminationDate)`): Active / Draft / RevisionRequested / **Ended** → `[start, end]`; **Terminated** → `[start, terminationDate]`; silinmiş → yok. Gün dahil (K59, analiz §3). Bugünkü sorgu (`LeaseRepository.cs:528`, statüler `:540-542`) Ended/Terminated saymıyor → hata düzelir (analiz §13 C2).
- `IReadOnlyList<OccupancyBlocker> GetBlockers(target(SpaceId, Path), records, exclusions)`: engelleyen = kaydın alanı hedefin **kendisi, atası ya da torunu** (`Yol` öneki iki yönde).
  - `exclusions`: bu işlemde **dönüşecek** (aynı alandaki kendi ön tahsisi) ya da kullanıcının onayıyla **kaldırılacak** kendi ön tahsisleri (K45, analiz §3 istisna). Aynı kiracının sözleşmeleri asla dışlanmaz.
  - Paylaşımlı grupta aynı grubun sözleşmeleri birbirini engellemez (`LeaseGroupId` eşit).
- `SpaceOccupancyState DetermineState(space, descendantsRentable, records)` → `Empty | PreAllocated | PartiallyOccupied | Occupied` (analiz §3.2). Kiralanamaz alanlar "tüm kiralanabilir torunlar dolu" hesabına girmez.
- Rezervasyon doluluğa girmez (K48, K68).

### 5.3 `PreAllocationPolicy` (yeni, `Domain/Spaces/` ya da `Domain/PreAllocations/`)

- `bool IsActive(PreAllocationStatus status, DateTime holdUntil, DateTime now)` = `status == Active && now < holdUntil`.
- `GetCreateViolations(space, now, holdUntil, start, end)`: alan Kiralanabilir + `OnTahsiseAcik` + aktif + paylaşımlı değil (K58); `holdUntil > now`; `start <= end`. Süre üst sınırı yok (K20).
- `CanExtend`: yalnız aktifken (analiz §4.1/9). `CanCancel`: yalnız aktifken.
- `PlanOnDraft(draftSpaceIds, tenantActivePreAllocations)` → `(ToConvert, ToRemove)`: taslaktaki alanın **kendisindeki** ön tahsis dönüşür; diğer her aktif ön tahsis (ata/torun dahil) kaldırılır (K24, K45).

### 5.4 `SpaceChangePolicy` (yeni, `Domain/Spaces/`)

Girdi: mevcut alan, istenen değişiklik, "açık kayıt" bayrakları (**açık sözleşme** = Taslak, Revizyon İstendi, Aktif ve fesih tarihi henüz gelmemiş Feshedildi; S-16) (kendisinde / atasında / alt ağacında: açık sözleşme, aktif ön tahsis, bitmemiş rezervasyon). **Bitmemiş rezervasyon** = durum Confirmed ya da PendingApproval ve `BitisTarihi > now` (analiz §6.1). Çıktı ihlal listesi:

| Değişiklik | Açık kayıt yok | Açık kayıt var |
|---|---|---|
| Tür etiketi, ad | ✔ | ✔ |
| m² | ✔ | ❌ yalnız alanın **kendisinde** açık sözleşme varken (K67) |
| Kiralanabilir → başka | ✔ | ❌ |
| Kiralanamaz → Kiralanabilir | ✔ | ❌ (K18) |
| Rezerve edilebilir → başka | ✔ | ❌ bitmemiş rezervasyon varken |
| → Rezerve edilebilir | ✔ (atası/torunu Rezerve edilebilir değilse, K68) | ❌ |
| Ön tahsis → kapalı | ✔ | ✔ mevcut ön tahsis sürer, yenisi açılamaz |
| Paylaşımlı aç/kapa | ✔ | ❌ (S-5: açık grup/sözleşme varken değişmez) |
| Pasifleştirme | ✔ (alt ağaç da) | ❌ |
| Etkinleştirme | üstü aktifse ✔; yalnız seçilen alan, alt ağaç pasif kalır (Onur 2026-11-26, Codex B09) | — |
| Silme | geçmiş yok + alt alan yok ✔ | ❌ (K60) |
| Üst alan değiştirme | ❌ (K15) | ❌ |

Ek uyarı (engel değil): alt alanların toplamı üst alanın m²'sini aşarsa (K17). Örnek: kat 100 m², ofisler toplamı 120 m² → uyarı; kat 120 m², ofisler 100 m² → uyarı yok.

### 5.5 Tarife ve fatura policy'leri

- **`LeaseRateSelectionPolicy`** (yeni, `Domain/Leases/`): `SelectForMonth(rows, leaseAreaId, chargeTypeId, monthStart)` → en büyük `GecerlilikBaslangici <= monthStart` satırı; `NotCharged` ise "kalem yok"; hiç satır yoksa "kapsam dışı" (analiz §5.3 kapsam). Seçilen satır `FirstMonthOneTime` tipindeyse ve ay sözleşmenin başlangıç ayı değilse "kalem yok" (Codex B04).
- `DateTime EffectiveMonthForExtension(DateTime extensionStart)` → ay 1'iyse o ay, değilse sonraki ayın 1'i (K54).
- `RegenerationWindows(selectedMonth, existingRowsByKey)` → her alan × borç tipi için yeni satırın geçerli olacağı aralık; sonraki aydan geçerli satır varsa onun başlangıcına kadar (K64).
- **`ChargeCompositionPolicy`** (yeni, `Domain/Charges/`): detayları `(ChargeTypeId, KdvRate)` ile gruplayıp kalem üretir; kalem `Tutar`/`KdvTutari` = detay toplamları (K40); detayların `HesaplamaYontemi` ve `BirimDeger`'i hepsi aynıysa kalemde bu ikisi dolu ve kalem `Carpan` = detay çarpanlarının **toplamı** (2 × 80 m² × 400 → 400 × 160 = 64.000), değilse üçü de boş (K39). Kalem tutarı hiçbir durumda başlıktan yeniden hesaplanmaz (K40) (Codex B07). Detay hesabı bugünkü `ChargeAmountPolicy.Calculate` (`Domain/Charges/ChargeAmountPolicy.cs`) + `VatPolicy.CalculateAmount` (`Domain/Financial/VatPolicy.cs:8`, `Math.Round(…, 2, ToEven)`) ile.
- **`RevenueSharePolicy`** (yeni, `Domain/Reporting/` ya da `Domain/Charges/`): tahsilatı detayların KDV dahil tutarı oranında böler, yuvarlama farkı son alana (K49, K56). Rapor anında; saklanmaz.

### 5.6 Değişen / silinen mevcut policy'ler

| Mevcut | Karar |
|---|---|
| `Domain/Leases/UnitCapacityPolicy.cs` | Silinir (K45) |
| `Domain/Leases/LeaseOccupancyPolicy.cs` | `SpaceOccupancyPolicy` ile değiştirilir |
| `Domain/Properties/PropertyStructurePolicy.cs` (`ResolveChargeTypeId`, `IsValidReservationUnitUsage` vb.) | Silinir; kullanım kuralları `SpaceChangePolicy`'ye, borç tipi rezervasyon kuralına (K69) |
| `Domain/Reservations/ReservationUnitPolicy.cs` (`IsReservable`) | `ReservationSpacePolicy` olur: alan aktif + `Kullanim = Reservable` |
| `Domain/Leases/LeaseGroupPolicy.cs` | Kalır; `AreaSumMismatch` alan m²'siyle (K26) |
| `Domain/Leases/LeaseRatePolicy.cs` | Kalır (hesap yardımcıları) |
| `Domain/Payments/PaymentRoutingPolicy.cs` (`HasValidScopeSelection`, `CanDeactivateOverride(propertyId, unitId)`) | Tek `spaceId?` parametresine iner |

## 6. Servis akışları ve kilit protokolü

### 6.1 Kilit protokolü (K42, analiz §5.2, §13 C6–C8)

Tüm kilitler `sp_getapplock`, `@LockOwner = 'Transaction'`, transaction sonuna kadar; bugünkü yardımcı `UnitRepository.cs:528` `AcquireAppLockAsync(resource, mode)` deseni (timeout 10000 ms, aktif transaction yoksa `InvalidOperationException`) yeni `SpaceLockRepository`'ye taşınır. **Sıra hep aynı, yükseltme yok:**

| Sıra | Kaynak adı (yeni) | Mod | Kim alır |
|---|---|---|---|
| 1 | `KiraTakip.PreAllocation.Tenant.{kiraciId}` (kiracı Id artan) | Exclusive | Ön tahsis durumunu değiştiren her yol: taslak oluşturma/güncelleme, ön tahsis oluştur/uzat/iptal, süre dolumu |
| 2 | `KiraTakip.Space.Root.{kokId}` (kök Id artan) | Exclusive | Sözleşme taslağı/onayı/uzatması, ön tahsis oluştur/uzat, süre dolumu, alan ekleme/düzenleme/pasifleştirme/etkinleştirme/silme, kullanım ve bayrak değişikliği, grup işlemleri |
| 2 | aynı | **Shared** | Rezervasyon yazıları: oluşturma, talep, onay, güncelleme, iptal, tamamlama (bugünkü `Capacity.Global` Shared deseni, `UnitRepository.cs:505`) |
| 3 | `KiraTakip.Reservation.Space.{alanId}` (Id artan) | Exclusive | Rezervasyon yazıları; yer değiştiren güncellemede eski + yeni alan. Bugünkü `KiraTakip.Reservation.Unit.{unitId}` (`ReservationRepository.cs:261`) buna dönüşür |
| 3a | `KiraTakip.PaymentRouting` | Exclusive / **Shared** | Exclusive: ödeme yönlendirmesi kaydı ve pasifleştirme (başka kilit almaz). Shared: sözleşme onayı (K34 kontrolünden önce, kök kilitlerinden sonra) (Codex B03) |
| 4 | `KiraTakip.Charge.Lease.{sozlesmeId}` (sözleşme Id artan) | Exclusive | Bir sözleşmenin `SozlesmeTarifeler` ya da tahakkuk satırlarına yazan **her** yol, ilk yazımdan önce alır: onay (dondurma + üretim), uzatma (yeni tarife + üretim), yeniden üretim, aylık üretim, fesih iptali. Grup işlemlerinde tüm üyelerin kilidi Id artan (Codex B02) |
| 5 | `KiraTakip.Payment.ChargeLineItem.{kalemId}` (Id artan) | Exclusive | Mevcut (`ChargeLineItemRepository.cs:141`); yeniden üretim/fesih iptali faturanın tüm kalemleri için alır; ödeme yolları yalnız bunu alır (`OnlinePaymentService.cs:57`, `:175`) |

Ek: bugünkü `KiraTakip.Reservation.Completion.{rezervasyonId}` (`ReservationRepository.cs:289-294`, `ReservationCompletionService.cs:38`) aynen kalır; tamamlamada kök Shared ve alan kilidinden **sonra** alınır.

Kalkan kilitler: `KiraTakip.Capacity.Global`, `KiraTakip.Capacity.Unit.{id}` (`UnitRepository.cs:497-528`).

Kilitlerden sonra durum **yeniden okunur** (alan ve ataları aktif mi, kullanım uygun mu, kullanıcıya gösterilen liste aynı mı, yetki var mı) ve **işlem anı bir kez alınır** (§6.7). Ön tahsis durum güncellemesi koşulludur (`WHERE Durum = 1`); etkilenen satır sayısı beklenenden azsa transaction geri alınır.

Kök kümesi: işlemin dokunduğu alanların ve etkilenen ön tahsislerin alanlarının kökleri (`SpaceTreePolicy.GetRootId`). Ön tahsis etkileyen işlemlerde küme, kiracı kilidi alındıktan **sonra** ön tahsisler okunarak hesaplanır; sonra kök kilitleri alınır (§6.3 adım 3).

### 6.2 Alan yönetimi (`SpaceService`, yeni; `PropertyService`, `UnitService`, `PropertyTypeService`, `UnitTypeService`, `CapacityPlanService` yerine)

- **Ekle:** kök kilidi (yeni kökte kilit gerekmez, S-6) → üst alan aktif mi, derinlik, K68 (kullanım Rezerve edilebilirse ata/torun kontrolü) → ekle → `Yol`/`Derinlik` yaz → `AlanTarihcesi(Created)` → kökse adres zorunlu (K13). Tür varsayılanları yalnız formu doldurur (K6).
- **Düzenle:** `SpaceChangePolicy` (§5.4) → değişen her alan için `AlanTarihcesi` satırı.
- **Pasifleştir:** alt ağaçla birlikte. **Etkinleştir:** yalnız seçilen alan, üstü aktifse; alt alanlar tek tek açılır (Codex B09, Onur 2026-11-26).
- **Sil:** K60 — geçmiş (sözleşme alanı, ön tahsis, rezervasyon, tahakkuk `AlanId`) yok ve alt alan yok; bağlı tanımlar (alan tarifesi, rezervasyon kuralı, ödeme yönlendirmesi, yetki kapsamı, özellik değerleri, adres) önizlemede listelenir, onayla birlikte silinir. Alan silme **soft delete**'tir (`BaseEntity.IsDeleted`); FK Cascade'leri soft delete'te çalışmadığı için bağlı tanımları servis aynı transaction'da kendisi siler (soft delete'li tablolarda soft, diğerlerinde gerçek silme). Şemadaki Cascade'ler yalnız gerçek silme için güvencedir.
- **Alan tarifesi, adres, özellikler:** ayrı endpoint'ler (§8).
- **Tür / özellik kataloğu:** `SpaceTypeService`, `SpaceAttributeService` (CRUD + aktif/pasif).

### 6.3 Sözleşme (`LeaseService`, `LeaseApplicationRules`, `LeaseGroupService`)

**Taslak oluşturma / güncelleme** (bugün `LeaseService.cs:101` `CreateAsync` → `:124` `CreateDraftAsync`; `:195` `UpdateDraftAsync`; `:568` `ApplyApplicationChangesAsync`). Sıra (analiz §5.2, §13 C3):

1. Girdi: kiracı, tarihler, `IReadOnlyList<(SpaceId, RentedArea?)>`, kullanıcının gördüğü ön tahsis listesi + "kaldırmayı onaylıyorum".
2. Doğrula (kilitsiz, erken hata): alanlar Kiralanabilir, K52 (`FindNestedPairs`), paylaşımlı alan yalnız grup akışında (K27), kiracı kategorisi var (K55).
3. Transaction → kiracı kilidi → kiracının aktif ön tahsislerini oku → kök kümesini hesapla (taslaktaki alanların + kaldırılacak/dönüşecek ön tahsislerin alanlarının kökleri) → kök kilitleri (Id artan).
4. Ön tahsisleri kök kilitleri altında yeniden oku; gösterilen listeyle karşılaştır; farklıysa dur, liste yeniden gösterilir (analiz §4.1/7). Yeniden okumada yeni bir kök çıkarsa (başka kökte yeni ön tahsis) işlem durur ve liste yeniden gösterilir; kilit sonradan eklenmez (yükseltme yok).
5. `PlanOnDraft` → dönüşecek/kaldırılacak. Kaldırılacak biri kullanıcının kapsamı dışındaysa **engelle** (K65; ad/tarih gösterilmez). Kaldırma onayı yoksa yönlendirici mesajla dur.
6. Her alan için `SpaceOccupancyPolicy.GetBlockers` (dönüşecek + kaldırılacak dışlanır).
7. Yaz: `Sozlesmeler` + `SozlesmeAlanlari` (anlık kopya; `KiralananAlan` varsayılan alan m²'si) + ön tahsis durumları (koşullu UPDATE) — tek transaction.

**Onay** (`LeaseService.cs:315` `ApproveAsync` → `LeaseApplicationRules.cs:306` `ActivateAsync` → `ChargeGenerationService.GenerateForLeaseAsync`): kök kilitleri → `PaymentRouting` Shared (F4'ten itibaren) → sözleşme tahakkuk kilidi (§6.1 sıra 4) → doluluğu yeniden kontrol → **tarife dondurma** (§6.4) → K55 eksik tarife (KIRA yoksa ve kirasız değilse engel) → **K34 mağaza kontrolü** (her borç tipi için alanların çözülen mağazası tek olmalı) → `Active` → tahakkuk üretimi.

**Ret:** Taslağın reddi bugün `DeleteDraftAsync` (`:286`) / `RequestRevisionAsync` (`:265`) ile ⚠ (ayrı `Reject` metodu bulunamadı). Silinen taslakta dönüşmüş ön tahsis geri gelmez; ret ekranı bunu söyler (K45).

**Uzatma** (`:335` `ExtendAsync`, kural `LeaseApplicationRules.cs:102` `EnsureExtensionAllowedAsync`): kök kilitleri → sözleşme tahakkuk kilidi → uzatılan dönem için doluluk → her alan × borç tipi güncel tarife ağaçtan bulunur, eski fiyatın yanında gösterilir; kullanıcı satır bazında özel fiyat girebilir (TÜFE yardımcısı, önceki özel fiyat dolu gelir) → yeni satırlar `EffectiveMonthForExtension` ayından (K38, K54). Bugünkü "birim türü/kiralama biçimi değişmediyse uzatma" kuralları (`LeaseApplicationRules`) kalkar; yerine "alanlar aktif ve kullanım hâlâ Kiralanabilir".

**Fesih** (`:393` `TerminateAsync`): fesih tarihi `[BaslangicTarihi, BitisTarihi]` içinde (bugün sınır yok, analiz §13 C2) → `Terminated` → `CancelFutureChargesAsync` (§6.5). Grup feshi (`LeaseGroupService.cs:333`) aynı sınır.

**Grup** (`LeaseGroupService.cs`): `ValidateGroupAsync` (`:462`) `UnitBased` şartı (`Lease.GroupUnitNotUnitBased`) kalkar; yerine alan `Paylasimli`. Her üye sözleşmesinde tek `SozlesmeAlanlari` satırı = grubun alanı, `KiralananAlan` = pay (K27). Pay toplamı = alan m² (K26, `LeaseGroupPolicy`). Doluluk grup Id'siyle dışlanır. Create/Update/Resubmit/Approve/Extend/Terminate/Restructure metotları (`:41`, `:87`, `:169`, `:229`, `:257`, `:333`, `:349`) aynı kilit protokolünü kullanır. Grup taslağında K24/K45 **her üye kiracı için** uygulanır: kiracı kilitleri tüm üyeler için Id artan alınır, her üyenin aktif ön tahsisleri okunur, grubun alanındaki kendi ön tahsisi dönüşür, diğerleri (onayla) kaldırılır; K65 her üye için geçerlidir.

**Kalkanlar:** kontenjan doğrulaması (`LeaseApplicationRules.cs:187` `ValidateCapacityLeaseAsync`, `:243` `EnsureCapacityAvailable`), `UpdateAllocatedAreaAsync` (`CapacityPlanService.cs:106`), `LeaseRepository.GetOpenLeaseAreasForUnitAsync` (`:744`), `GetOpenLeasesForCapacityPlanAsync` (`:760`), `CapacityOccupantQuery`.

### 6.4 Tarife çözümü ve dondurma (K9, K30, K36–K38, K51, K54, K55, K64, K70)

Yeni `SpaceRateResolver` (Application/Services/Pricing), bugünkü zincirin (`RateResolverService.cs:19`: LeaseRateOverride `:21-26` > UnitRate `:46-50` > PropertyRateOverride `:53-58` > RateSchedule `:60-65`) yerine:

1. Sözleşmeye özel fiyat (formdan) → `KaynakTipi = 1`.
2. Alanın kendisi, sonra ataları (yakından uzağa, `Yol`'dan) `AlanTarifeleri`'nde (alan × kiracı kategorisi × borç tipi) → `2` + `KaynakAlanId`, `KaynakAlanTarifeId`.
3. `GenelTarifeler` **yalnız dondurma yılı** (onay yılı / uzatma yılı / yeniden üretim yılı) → `3`. Bugünkü yıla düşme (`RateScheduleRepository.cs:27-31`, `BatchRateResolver.cs:111-114`) kalkar (K55).
4. Bulunamazsa: ekranda listelenir; kullanıcı özel fiyat girer ya da "kesilmeyecek" (`4`). KIRA ise ve sözleşme kirasız değilse engel.

**Kapsam:** dondurma anında her alan × uygulanabilir borç tipi için bir satır yazılmadan işlem bitmez. **Dondurulacak** borç tipleri = aktif `MonthlyFixed` + `FirstMonthOneTime` (`ChargeTypeRepository.cs:100`), kirasız sözleşmede KIRA hariç (`LeaseBillingPolicy.ShouldIncludeChargeType`). **O ay faturaya girecek** tipler ayrı kuraldır: `FirstMonthOneTime` yalnız sözleşmenin başlangıç ayında (`ChargeGenerationService.cs:237-250` bugünkü filtre aylık üretimde ve yeniden üretimde korunur; pro-rata uygulanmaz, `ChargeProrationPolicy.cs:23`). Kullanılan tipin davranışı değişemediği için (§6.6) davranış snapshot'ı gerekmez (Codex B04). Tahakkuk yalnız sözleşmenin kendi satırlarından üretilir.

**Ne zaman:** ilk onay (sözleşme başlangıç ayından), uzatma (K54 ayından), yeniden üretim (seçilen aydan, K64 pencereleriyle). Aynı ay için satır varsa güncellenir (K54). Eski satırlar silinmez.

`RateHierarchyService` (`:19` `GetParentForAsync`, `:68` `GetReservationParentAsync`; ekrandaki "üstten gelen değer" gösterimi) ağaç yukarı çıkacak şekilde yeniden yazılır; `DateTime.Now.Year` (`:21`, `:71`) yerine `TimeProvider`.

### 6.5 Tahakkuk (`ChargeGenerationService`, `ChargeRepository`)

- **Aylık üretim** (`GenerateForLeaseAsync`, `:28`): sözleşme tahakkuk kilidi → dönemler (`ChargePeriodPolicy.GetMonthlyPeriodStarts`) → dönem var mı (`:43-46`; `YenidenUretimleIptal = 1` olanlar yok sayılır, diğer iptaller dönemi dolu tutar) → her `SozlesmeAlanlari` × borç tipi için `LeaseRateSelectionPolicy.SelectForMonth` → detay hesabı (`KiralananAlan` m² çarpanı; pro-rata `:50` bugünkü gibi) → `ChargeCompositionPolicy` → `Tahakkuklar` (`AlanId` boş) + `TahakkukKalemleri` + `TahakkukKalemDetaylari`. Kirasız sözleşmede KIRA yok (K57; bugün `:86`).
- **Yeniden üretim** (`RegenerateAsync`, `:123`; bugün `GetSilineceklerAsync` `ChargeRepository.cs:1087` → `DeleteRangeAsync` `:1095` gerçek silme): yeni akış
  1. Önizleme: seçilen aydan itibaren aylar; **korunan aylar** = ödemesi olan (PaymentAllocations) ya da kartı Pending/Unknown olan (`OnlinePaymentTransactionStatus`) (K53, K66); **izli iptal edilecek** = kartı yalnız Failed/Cancelled olan; **silinecek** = hiç ödeme/kart kaydı olmayan. Eski/yeni fiyat yan yana; K64 pencereleri. Kullanıcı satır bazında sözleşmeye özel fiyat girebilir; önceki özel fiyat dolu gelir (K54).
  2. Kayıt: sözleşme tahakkuk kilidi → faturaların tüm kalem kilitleri (Id artan) → korunan küme yeniden hesaplanır; önizlemedekinden farklıysa dur.
  3. Yeni `SozlesmeTarifeler` satırları (K51, K54, K64).
  4. Adaylar yalnız güncel faturalardır (`YenidenUretimleIptal = 0`); izli iptal edilmiş eskiler aday olmaz. `DegistirilenTahakkukId` dolu bir fatura (önceki yenilemenin ürünü) ödeme/kart kaydı olmasa da **gerçek silinmez**, izli iptal edilir; böylece A→B→C zinciri korunur (Codex B05). Diğer silinecekler gerçekten silinir (kart kaydı yok, FK sorunu yok); izli iptaller: eski tahakkuk `Status = Cancelled`, `YenidenUretimleIptal = 1`, `SaveChanges`; sonra yeni tahakkuk `DegistirilenTahakkukId = eski.Id` ile eklenir (dönem unique filtresi sırası bu yüzden önemli, S-4). Kart kaydı eski kaleme bağlı kalır (`OnlinePaymentTransactionConfiguration.cs:21-24` Restrict).
  5. Bugün kart denemesi olan ayda yeniden üretim FK hatasıyla duruyor (analiz §13 C8) → düzelir.
- **Fesih iptali** (`CancelFutureChargesAsync`, `:175`; bugün `Status != Paid` olanları iptal ediyor, ödeme dağıtımına bakmıyor): ödemesi olan (kısmen ödenmiş dahil) ay iptal edilmez, listelenir (K63). Aynı kilitler.
- **Elle borç** (`ManualChargeService.cs:59` `CreateAsync`, birim seçimi `:54`): kullanıcı tek alan seçer; sözleşmeye bağlıysa alan sözleşmenin alanlarından biri olmalı → `Tahakkuklar.AlanId` boş + tek detay satırı (`SozlesmeTarifeId` boş); sözleşmesizse `Tahakkuklar.AlanId` dolu, detay yok (K57).
- `DateTime.Today`/`Now` kullanan yerler (`ChargeGenerationService.cs:157`, `LeaseApplicationRules.cs:129,145,230`, `LeaseExpirationService`) `TimeProvider`'a geçer (§6.7).

### 6.6 Rezervasyon (`ReservationService`, `ReservationRepository`)

- **Kural çözümü** (K28, K69): alanın kendisi, sonra ataları `RezervasyonTarifeler`'de; ilk bulunan satır ücret + KDV + borç tipini birlikte verir. Tür + yıl genel kuralı (bugün `ReservationService.cs:266-273` `GetGeneralAsync(unit.UnitTypeId, year)`) kalkar. Kural yoksa rezervasyon yapılamaz (S-7: bugünkü varsayılan kural `EnsureVarsayilanReservationRateOverrideAsync` seed'de kök alana kural olarak yazılır).
- **Kopya:** `CreateAsync` (`:312`), `CreateRequestAsync` (`:377`), `UpdateAsync` (`:593`; güncelleme her zaman yeniden hesaplar) ücreti hesapladığı an `BorcTipiId`'yi de yazar. `TransferToChargeAsync` (`:797`) `Rezervasyonlar.BorcTipiId`'yi kullanır; `ChargeTypeRepository.ResolveReservationTypeAsync` (`:112`) ve "ilk aktif tipe düşme" yedeği kalkar (`:823`).
- **Borç tipi koruması (Codex B08):** (a) **Davranış değişikliği** (`ChargeTypeService.UpdateAsync` `:50`), tip herhangi bir rezervasyon kuralında (aktif/pasif), rezervasyonda (`Rezervasyonlar.BorcTipiId`) ya da tahakkuk kaleminde kullanılmışsa engellenir. (b) **Pasifleştirme** (`UpdateAsync` ve `ToggleStatusAsync` `:84`) yalnız tip aktif bir rezervasyon kuralında geçiyorsa engellenir; geçmiş rezervasyonlar pasif tipi kullanmaya devam eder. Bugünkü `unitTypeRepository.AnyAktifByBorcTipiIdAsync` kontrolü `reservationRateRepository.AnyActiveByChargeTypeAsync` + yeni `IsChargeTypeUsedAsync`'e döner.
- **Çakışma** (`ReservationRepository.cs:375` `IsConflictAsync`): yalnız aynı alan, yalnız `Confirmed`, `Start < end && End > start` (bugünkü gibi, K68). Kilit §6.1 (kök Shared + alan Exclusive). Kilitlerden sonra alan aktif + `Reservable` yeniden kontrol.
- **Tamamlama** (`ReservationCompletionService.cs:31`, hosted `ReservationCompletionBackgroundService.cs`): aynı kilitler.
- `UnitTypeService.ValidateChargeTypeAsync` (`:171`) mantığı rezervasyon kuralı kaydına taşınır (aktif + `ReservationSpecific`).

### 6.7 Ön tahsis (`PreAllocationService`, yeni) ve zaman

- **Oluştur:** kiracı kilidi → kök kilidi → `PreAllocationPolicy.GetCreateViolations` → doluluk (`SpaceOccupancyPolicy`, kiracının kendi kayıtları dahil normal kural) → INSERT.
- **Uzat:** yalnız aktifken; yeni `TahsisBitis > now`.
- **İptal:** yalnız aktifken → `Cancelled`.
- **Süre dolumu:** yeni `PreAllocationExpirationBackgroundService` (desen `LeaseExpirationBackgroundService.cs`, `PeriodicTimer`): `Durum = 1 AND TahsisBitis <= now` adayları → kiracı + kök kilidi → koşullu UPDATE `Expired`. Gecikme hakkı uzatmaz (aktiflik `now < TahsisBitis` ile okunur).
- **Zaman:** iş saat dilimi `Europe/Istanbul` ayardan (S-8: `SystemSettings`'e anahtar, varsayılan Europe/Istanbul) → `IBusinessClock.Now` (`TimeProvider` + saat dilimi). `TimeProvider` kaydı mevcut (`InfrastructureModule.cs:80`). İşlem anı kilitlerden sonra bir kez alınır.

### 6.8 Yetki kapsamı (K14, K32, K47)

- Bugün: `UserScopeDto { GlobalAccess, PropertyIds, UnitIds }`; cache `PermissionScopeCacheService.cs:11` (`YetkiKapsami_{userId}`), provider `Application/Services/Identity/PermissionScopeProvider.cs` (`IsInScope(propertyId)`), doldurma `Web/Authorization/YetkiKapsamiActionFilter.cs:9-48`. `PropertyIds`/`UnitIds` ~65 dosyada (en çok `ChargeRepository` 59, `LeaseRepository` 44, `ReservationRepository` 27, `UnitRepository` 27, `PropertyRepository` 26, `PaymentAllocationRepository` 21, `ManualChargeService` 21, `TenantRepository` 20). Ayrıca servis başına kapsam DTO'ları: `ReservationAccessScopeInput`, `PaymentAccessScopeInput`, `TenantAccessScopeInput`, `DocumentAccessScopeInput`.
- Yeni: `UserScopeDto { GlobalAccess, SpaceRootPaths }` (kapsam alanlarının `Yol`'ları) + yardımcı `IsSpaceInScope(path)` = herhangi bir kapsam yolunun öneki. Repository filtreleri: `alan.Yol LIKE kapsamYol + '%'` (OR). Tek `SpaceAccessScope` girdi tipi dört DTO'nun yerine (S-9).
- **Sözleşme/fatura/ödeme görünürlüğü (K32, K47):** sözleşmenin **tüm** `SozlesmeAlanlari`'nın alanları kapsamda olmalı: `NOT EXISTS (alan kapsamda değil)`. Sözleşmesiz tahakkuk `Tahakkuklar.AlanId` ile. Kiracı kullanıcısı aynı kural (`TenantPanelService.cs:43-71`). Kiracı kullanıcı düzenleme ekranında kısmi kapsam uyarısı (`TenantUserService.cs:357-368`, `AdminTenantUserController.cs:121-122`).
- **Rapor (K33, K49):** sözleşmeli faturalar kalem detayından (kullanıcının kapsamındaki alanların satırları), rezervasyon ve sözleşmesiz elle borç faturaları `Tahakkuklar.AlanId` ile tek alana yazılır; tahsilat sözleşmelide `RevenueSharePolicy` ile bölünür (§5.5), tek alanlı faturada tamamı o alana.
- Yetki kapsamı ekranı (`AdminUserService` `:132,:167`, `InvitationService` `:145`) ağaç seçiciyle alan seçer.

### 6.9 Ödeme mağazası (K34, K35)

- `PaymentStoreResolver.ResolveAsync(chargeTypeId, unitId)` (`Application/Services/Payments/PaymentStoreResolver.cs:12`; adaylar `PaymentStoreRoutingRepository.cs` ~`:151-180` Unit > Property > General) → `ResolveAsync(chargeTypeId, spaceId)`: alanın kendisi, sonra ataları, sonra genel.
- Sözleşmeli tahakkukta mağaza: kalemin detaylarındaki alanların çözdüğü mağaza (K34 sayesinde tek). `PaymentService.cs:94`, `:153`, `OnlinePaymentService.cs:74` bugün `balance.UnitId` kullanıyor → `balance.SpaceIdForRouting` (sözleşmeliyse ilk detay alanı). Teklik onayda K34, sonrasında K35 ile korunur; K35 borcu kalan kapanmış sözleşmeleri de kapsadığı için ödeme anında da geçerlidir (Codex B06).
- **K34:** onayda her borç tipi için sözleşme alanlarının çözülen mağazaları tek olmalı; değilse onay engellenir ("ayrı sözleşme yapın").
- **K35:** yönlendirme kaydında (`PaymentStoreRoutingService.UpsertAsync` `:48`, `DeactivateOverrideAsync` `:74`): kayıt sonrası durumu simüle et; açık bir sözleşmenin **ya da ödenmemiş borcu kalan bir sözleşmenin** (Sona erdi/Feshedildi dahil; Onur 2026-11-26, Codex B06) alanlarını aynı borç tipi için farklı mağazaya düşürecekse reddet ve etkilenen sözleşmeleri listele. S-16 tanımı değişmez; borçlu sözleşme K35 için ayrıca eklenir.
- `BankTransactionService.cs` (`:75-78`, `:124-127`, `:151-152`) kapsam kontrolleri §6.8'e göre.

## 7. Mevcut kod → yeni karşılık

`UnitId`/`PropertyId` yaklaşık 160 dosyada geçiyor (analiz §12). Aşağıdaki liste ana dosyalardır; derleme hataları kalan küçük dosyaları (DTO, validator, view model) gösterir. "Faz" sütunu §11'e bağlar.

### 7.1 Silinir

| Dosya | Faz |
|---|---|
| Entity: `Property.cs`, `Unit.cs`, `PropertyType.cs`, `UnitType.cs`, `UnitRate.cs`, `PropertyRateOverride.cs`, `UnitCapacityHistory.cs`, `LeaseAreaHistory.cs` + konfigürasyonları | F2a |
| `Application/Services/Properties/`: `PropertyService.cs`, `UnitService.cs`, `PropertyTypeService.cs`, `UnitTypeService.cs`, `CapacityPlanService.cs`, `DefaultAllocationSuggestionStrategy.cs`, `ProportionalReductionStrategy.cs` | F2a |
| `Application/Services/Pricing/`: `PropertyPricingService.cs`, `UnitPricingService.cs` (`SpaceRateService`'e) | F2a |
| `Domain/Leases/UnitCapacityPolicy.cs`, `Domain/Properties/PropertyStructurePolicy.cs`, `Domain/Leases/LeaseOccupancyPolicy.cs` | F2a / F2b |
| Repository: `UnitRepository.cs`, `PropertyRepository.cs` (+ tip/tarife repository'leri) ⚠ tam liste derlemede | F2a |
| Controller: `PropertyController.cs`, `UnitController.cs`, `UnitCapacityController.cs`, `AdminUnitTypeController.cs`, `AdminPropertyTypeController.cs`, `PropertyPricingController.cs` | F2a |
| View: `Views/Property/*`, `Views/Unit/Rates.cshtml`, `Views/UnitCapacity/Index.cshtml`, `Views/PropertyPricing/Index.cshtml`, `Views/AdminUnitType/*`, `Views/AdminPropertyType/*`, `Views/Lease/_LeaseUnitPicker*.cshtml` | F2a (seçici F2b'de ağaç seçiciyle) |
| ViewModel (`Web/Models/ViewModels`): `CreatePropertyViewModel`, `EditPropertyViewModel`, `PropertyDetailsViewModel`, `PropertyUnitInputViewModel`, `PropertyUnitEditViewModel`, `PropertyViewModelMapper`, `PropertyPricing*`, `PropertyTypeFormViewModel`, `UnitTypeFormViewModel`, `UnitCapacityViewModels`, `UnitCustomRateViewModel`, `UnitPricing*`, `UnitRateCategory*` | F2a |
| `LeaseController.cs:426-429` `UpdateAllocatedArea` + izin `Lease.UpdateAllocatedArea` | F2a |
| `ChargeTypeRepository.ResolveReservationTypeAsync` (`:112`) yedeği | F2a |

### 7.2 Yeni

| Dosya (öneri) | Faz |
|---|---|
| Entity + config: `Space`, `SpaceType`, `SpaceAddress`, `SpaceHistory`, `SpaceAttributeDefinition`, `SpaceAttributeValue`, `SpaceTypeDefaultAttribute`, `SpaceRate` | F1 |
| `PreAllocation` | F2b |
| `ChargeLineItemDetail` | F3 |
| Domain: `Spaces/SpaceTreePolicy`, `Spaces/SpaceChangePolicy` | F1 |
| Domain: `Spaces/SpaceOccupancyPolicy`, `Spaces/PreAllocationPolicy` | F2b |
| Domain: `Leases/LeaseRateSelectionPolicy`, `Charges/ChargeCompositionPolicy` | F3 |
| Domain: `Reporting/RevenueSharePolicy`, `Reservations/ReservationSpacePolicy` | F4 / F2a |
| Application: `SpaceService`, `SpaceTypeService`, `SpaceAttributeService`, `SpaceRateService`, `SpaceRateResolver`, `SpaceScopeService` (kapsam yolları) | F1 / F2a |
| Application: `PreAllocationService`, `PreAllocationExpirationService` | F2b |
| Infrastructure: `SpaceRepository` (ata/torun `Yol` sorguları), `SpaceLockRepository` (§6.1), `PreAllocationRepository` | F1 / F2b |
| Web: `SpaceController` (kök listesi, ağaç, CRUD), `AdminSpaceTypeController`, `AdminSpaceAttributeController`, `PreAllocationController`, `Views/Shared/_SpaceTreePicker.cshtml` | F1 / F2b / F5 |
| Hosted: `PreAllocationExpirationBackgroundService` | F2b |

### 7.3 Dönüşür

| Dosya | Ne değişir | Faz |
|---|---|---|
| `Lease.cs`, `LeaseArea.cs`, `LeaseGroup.cs`, `LeaseRateOverride.cs` (→ `LeaseRate`) | §4.3 | F2a (LeaseArea N), F3 (LeaseRate) |
| `LeaseService.cs`, `LeaseApplicationRules.cs`, `LeaseGroupService.cs`, `LeaseRepository.cs` (`:528` çakışma, `:546` `GetOpenLeaseUnitIdsAsync`, `:728-760` alan/kontenjan sorguları) | §6.3 | F2a, F2b |
| `LeaseController.cs` (`Create(int? unitId, …)` `:147` → `spaceIds`), `LeaseController.Group.cs` | Alan seçimi | F2a, F2b |
| `LeaseExpirationService.cs` | `TimeProvider` | F2b |
| `RateResolverService.cs`, `BatchRateResolver.cs`, `RateHierarchyService.cs`, `RateScheduleRepository.cs:27` | Ağaç zinciri (F2a); dondurulmuş satırdan okuma ve yıla düşmenin kalkması (F3) | F2a, F3 |
| `ChargeGenerationService.cs`, `ChargeRepository.cs` (`:1087`, `:1095`), `ManualChargeService.cs`, `Charge.cs`, `ChargeLineItem.cs` | §6.5 | F2a (AlanId), F3 |
| `ReservationService.cs`, `ReservationRepository.cs`, `ReservationCompletionService.cs`, `ReservationRateOverride.cs`, `Reservation.cs`, `AdminReservationRateRuleController.cs` | §6.6 | F2a |
| `ChargeTypeService.cs` (`:50`, `:84`) | Rezervasyon kuralı koruması | F2a |
| `PaymentService.cs`, `OnlinePaymentService.cs`, `PaymentStoreResolver.cs`, `PaymentStoreRoutingRepository.cs`, `PaymentStoreRoutingService.cs`, `PaymentStoreRoutingBusinessRules.cs`, `AdminPaymentStoreRoutingController.cs`, `PaymentStoreRouting.cs` | §6.9 (çözüm F2a; K34/K35 F4) | F2a, F4 |
| `UserScopeDto`, `PermissionScopeCacheService.cs`, `PermissionScopeProvider.cs`, `YetkiKapsamiActionFilter.cs`, `UserPermissionScope.cs`, `ScopeType.cs`, kapsam filtreli repository'ler (§6.8) | §6.8 | F2a (yol tabanlı), F4 (K32 "tüm alanlar") |
| `AdminUserService.cs`, `InvitationService.cs`, `TenantUserService.cs`, `AdminTenantUserController.cs`, `Invitation.cs`, `ApplicationUser.cs` | Alan kapsamı | F2a, F4 |
| `TenantPanelService.cs`, kiracı controller'ları (`TenantLeaseController`, `TenantChargeController`, `TenantReservationController`, `TenantPanelController`) | Alan + K47 | F2a, F4 |
| `ReportService.cs`, `ReportController.cs`, `StatisticsService.cs`, `HomeController.cs` (`:55-57`, `:99`, `:175-190`, `:316`, `:348`) | Alan; gelir payı (K33, K49) | F2a, F4 |
| `ChargeReminderService.cs` (`:41-42`, `:67-68`), `DocumentService.cs` (`:219-223`), `BankTransactionService.cs`, `AuditDisplayNames` / `AdminAuditLog` ⚠ | Alan adı/kapsamı | F2a |
| `SeedDataService.cs` (§10.3), `SeedingExtensions.cs:31` (`Properties.AnyAsync` → `Spaces.AnyAsync`) | Yeni seed | F1–F3 |
| `PermissionCatalog.cs`, `docs/permission-catalog.md`, rol seed'i (`SeedIcRollerVeKullanicilarAsync` `SeedDataService.cs:3412-3518`) | §8 | Her faz kendi izinleri |
| `Views/Shared/_Sidebar.cshtml` (`:66-70` Taşınmazlar, `~:159-161` Birim Türleri, `:199-203` Taşınmaz Tipleri, `:230-245` hızlı ekle) | Alanlar, Alan Türleri, Özellikler, Ön Tahsisler | F1, F2b |
| Yardım: `Web/Help/HelpSections.cs` (`:46` `yonetim-tasinmaz`, `:53-54`), `Views/Help/Sections/_TarifeHiyerarsisi.cshtml` | Yeni anlatım | F6 |

## 8. İzinler

Kural (`CLAUDE.md`): state değiştiren her POST endpoint'in kendi 3. seviye izni vardır, adı endpoint'in adıdır; GET'ler modül izniyle korunur. Sabitler `src/KiraTakip.Application/Authorization/PermissionCatalog.cs`'e, açıklamalar `docs/permission-catalog.md`'ye eklenir; kapsama duyarlı olanlar `ScopeAware` kümesine (`PermissionCatalog.cs:384` `IsScopeAware`). Analiz §11'de "kesin adlar spec'te" denmişti; endpoint adına göre kesinleşen adlar:

| Modül (yeni) | Endpoint → izin |
|---|---|
| `Internal.Space` | `Module` (GET); POST `Create`, `Edit`, `Deactivate`, `Activate`, `Delete`, `EditRate`, `EditAddress`, `EditAttributes` |
| `Internal.SpaceType` | `Module`; POST `Create`, `Edit`, `ToggleStatus` (analizdeki `Deactivate`, endpoint adına göre) |
| `Internal.SpaceAttribute` | `Module`; POST `Create`, `Edit`, `ToggleStatus` |
| `Internal.PreAllocation` | `Module`; POST `Create`, `Extend`, `Cancel` |
| `Internal.ReservationRateRule` (mevcut, `:209`) | `Module`, `Create`, `Edit` kalır; ekran alana bağlanır. Bugün `ToggleStatus` (`AdminReservationRateRuleController.cs:133-135`) `Edit` ile korunuyor → yeni `ToggleStatus` izni. Bugün `Unit` controller'daki `SaveReservationRule`/`ClearReservationRule` (`UnitController.cs:54-57`, `:96-99`, `Unit.OverrideRate`) → alan ekranında `Create`/`Edit` ve yeni `Delete` |
| `Internal.PaymentRouting` (mevcut, `:100`) | `Save` (`AdminPaymentStoreRoutingController.cs:31-33`, bugün `Create`) ve `Deactivate` (`:58-60`, bugün `Edit`) kalır; kapsam seçimi alana iner. İzin adlarını endpoint adına çekmek (S-10) F4'te |
| `Internal.Lease` (mevcut, `:47`) | Eylemler kalır; `UpdateAllocatedArea` (`:66`) kalkar |
| `Internal.Charge` (mevcut, `:165`) | `Regenerate` kalır; önizleme GET, kayıt aynı POST |

**Kalkan:** `Internal.Property` (`:20`: Module, Create, Edit), `Internal.Unit` (`:28`: Module, Create, Edit, OverrideRate, UpdateCapacity), `Internal.PropertyType` (`:116`), `Internal.UnitType` (`:124`), `Internal.PropertyMultiplier` (`:202`), `Internal.Lease.UpdateAllocatedArea` (`:66`).

Rol seed'i (`SeedDataService.cs:3412-3518`, `finansPermissions` `:3421`, `operasyonPermissions` `:3455`) yeni izinlerle güncellenir. Mimari testler (`tests/KiraTakip.Tests/Architecture/Properties/PropertyArchitectureTests.cs:21` `HttpPostAttribute` taraması ve diğer controller mimari testleri) yeni controller'lar için yazılır.

## 9. Ekranlar

Her faz kendi özelliğini **test edilebilir kılan en az ekranı** getirir; F5 tam UX'tir. Ön yüz kuralları: önce Tailwind sınıfı, yoksa custom sınıf; inline style ve sabit hex yok; renk/font `wwwroot/css/tokens.css` (proje kuralı).

| Ekran | İçerik | Faz |
|---|---|---|
| Kök alan listesi (`/Space`) | Kökler, özet doluluk | F1 |
| Alan ağacı (`/Space/{rootId}`) | İç içe ağaç; düğümde tür etiketi, kullanım rengi, m²; ekle/düzenle/pasifleştir/sil; tarihçe; adres; özellikler; alan tarifesi | F1 (temel), F5 (rozetler) |
| Alan türleri, özellik kataloğu | Liste + form | F1 |
| Sözleşme formu | Alan seçimi: F2a'da tek alan listesi, F2b'de ağaç seçici (`_SpaceTreePicker`): dolu/ön tahsisli düğümler kilitli, seçili alanın atası/torunu seçilemez (K52); ön tahsis hatırlatma ve onay kutusu | F2a, F2b |
| Ön tahsis | Alan ağacından oluştur; liste; uzat/iptal | F2b |
| Rezervasyon kuralı | Alan ekranında "rezervasyon kuralı" sekmesi (ücret, KDV, borç tipi); en yakın atadan gelen değer gösterilir | F2a |
| Onay / uzatma / yeniden üretim önizlemesi | Alan × borç tipi: eski/yeni fiyat, kaynak, eksik tarife listesi, "kesilmeyecek"; yeniden üretimde korunan/izli iptal/silinecek aylar | F3 |
| Fatura | Kalem + "N alan" kırılımı | F3 |
| Ödeme yönlendirme | Alan seçici; K35 reddinde etkilenen sözleşmeler | F4 |
| Gelir raporu | Alan payı | F4 |
| Doluluk rozetleri, dashboard, yardım | analiz §3.2 durumları | F5 |

## 10. Migration, veritabanı ve seed

### 10.1 Veritabanı kuralı

- Tek DB: `DBTeknoPark_Test`. Bu klasördeki `appsettings*.json` kopyaları yalnız bunu gösterir; `tests/KiraTakip.Tests/Infrastructure/DatabaseFixture.cs` ve `src/KiraTakip.Web/Hosting/SeedingExtensions.cs` başka adda hata verir.
- Connection string elle değiştirilmez; `--connection` parametresi kullanılmaz.
- **Eksik kilit (F1'in ilk adımı, S-14):** bugün kilit yalnız `DatabaseFixture.cs:28` ve `SeedingExtensions.cs:43`'te. `dotnet ef` komutları design-time factory'den (`src/KiraTakip.Infrastructure/Persistence/ApplicationDbContextFactory.cs:34-41`; `appsettings*.json` + ortam değişkenleri) bağlanır ve DB adına bakmaz; uygulama açılışında `IdentitySeedService.SeedAsync()` (`SeedingExtensions.cs:23`) kilitten önce yazar. F1 ilk iş olarak aynı DB adı kilidini (a) `ApplicationDbContextFactory`'ye ve (b) Web açılışında, `SeedAsync`'ten önce ekler. Böylece `drop`/`update`/uygulama başka DB'de çalışamaz.
- Her `database drop`/`update`'ten önce `dotnet ef dbcontext info` ile hedef DB adı `DBTeknoPark_Test` mi bakılır. 2026-11-26'da makinede `ConnectionStrings__DefaultConnection` ortam değişkeni yoktu (Process/User/Machine).
- Dal `main`'e birleşirken tüm DB adı kilitleri (fixture, seed, factory, Web açılışı) ve `CLAUDE.md` başındaki not geri alınır (F6).

### 10.2 Faz başına adımlar (K61, K62)

```powershell
# 1) Migration ekle (yalnız şema)
dotnet ef migrations add <Ad> --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
# 2) DB'yi sıfırla (dummy veri; K62) ve tüm migration'ları uygula
dotnet ef database drop --force --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
dotnet ef database update --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
# 3) Seed: uygulamayı Development'ta bir kez çalıştır (boş DB'yi görünce seed eder)
dotnet run --project src/KiraTakip.Web/KiraTakip.Web.csproj
# 4) Testler
dotnet test tests\KiraTakip.Tests\KiraTakip.Tests.csproj
```

- `database drop` her fazda DB'yi sıfırlar (S-11): migration'lar veri çevirmez, eski satırlar yeni NOT NULL/FK kolonlarına takılmaz. Sıfırlama yalnız `DBTeknoPark_Test`'e yapılır: ayar dosyaları bunu gösterir, F1'deki factory kilidi (S-14) başka adı reddeder, komut öncesi `dbcontext info` kontrolü yapılır. Onur onayı: 2026-11-26.
- **İlk adım (F1 başı):** DB yoktur; `database update` mevcut 20 migration'la (`Persistence/Migrations/`, son `20261118105247_AddLeaseAreaModeAndHistory`) oluşturur. SQL kullanıcısının DB oluşturma yetkisi yoksa Onur DB'yi açar.
- Migration adları: F1 `AddSpaceTree`; F2a `ReplacePropertyUnitWithSpace`; F2b `AddPreAllocations`; F3 `FreezeLeaseRatesAndChargeDetails`; F4 gerekirse `AlignPaymentRoutingPermissions` (şema değişmezse yok).
- Testler DB'yi kendisi migrate etmez (`DatabaseFixture.cs:40-73`); bekleyen migration ya da migration'a dökülmemiş model değişikliği varsa net hata verir.
- **Canlı:** bu dalda canlıya ait hiçbir adım yok. Canlı taşıma betiği F6'dan sonra ayrı iş (K62).

### 10.3 Seed (`SeedDataService.cs`, 3576 satır)

Bugün: `SeedTasinmazTipleriAsync` (`:478-492`, 3 tip), `SeedBirimTurleriAsync` (`:494-534`, 5 tür; TOPLANTI/ETKINLIK'e borç tipi), `SeedDomainDataAsync` (`:613-~1807`, 5 taşınmaz, ~27 birim, 12 sözleşme), `SeedTasinmazFiyatlarAsync` (`:1882-1975`), `SeedTahakkuklarAsync` (`:1976-1985`) → `SeedTahakkuklarVeOdemelerAsync` (`:1987-2409`), `ClearDomainDataAsync` (`:3224-~3313`), `EnsureVarsayilanReservationRateOverrideAsync`. Çağrı sırası `SeedingExtensions.cs:60-75`.

Yeni seed (her faz kendi kısmını ekler; F1'de eski seed yanında çalışır, F2a'da eskisi silinir):

- **Alan türleri:** Kampüs, Bina, Kat, Ofis, Açık Ofis, Masa, Toplantı Salonu, Etkinlik Alanı, Depo, Otopark Alanı, Koridor/Teknik.
- **Ağaç:** bugünkü 5 taşınmazın karşılığı; en az bir 4+ seviyeli dal (Kampüs → Blok → Kat → Ofis), kat olmadan binaya bağlı Kuluçka Merkezi, Açık Ofis + Masa çocukları, paylaşımlı ofis, Rezerve edilebilir salon (iç içe değil, K68), Kiralanamaz teknik oda. Kökte adres.
- **Tarifeler:** kök ve ara seviyelerde `AlanTarifeleri` (kategori × borç tipi), genel tarife yalnız seed yılı; kök alanda rezervasyon kuralı (`BorcTipiId` ile).
- **Sözleşmeler:** tek alanlı, çok alanlı (farklı köklerden biri, K31), paylaşımlı grup (pay toplamı = m²), Sona erdi, Feshedildi, taslak; F2b'de aktif/süresi dolmuş/dönüşmüş ön tahsisler.
- **Tahakkuk ve ödeme:** F3'ten sonra dondurulmuş satırlardan; kısmen ödenmiş ay, kart denemesi Failed/Pending ay örnekleri (K53, K66 testleri için).
- **Kiracılar:** hepsinde kategori (K55).
- `ClearDomainDataAsync` yeni tablolarla FK sırasına göre güncellenir.

## 11. Fazlar

Analiz §8'deki F1–F6 omurgası korunur, ama içerik kayar (S-12, S-13): F2, `Unit`/`Property` kaldırılmadan çok alanlı sözleşmeye geçilemediği için **F2a (kesme)** ve **F2b (çok alanlı sözleşme, doluluk, ön tahsis)** diye ikiye bölünür (S-12). Her faz sonunda: build temiz, ilgili testler yeşil, denetçi ONAY, Onur "devam". Commit'leri Onur atar.

### F1 — Alan ağacı çekirdeği (eski yapı yanında)

- **İçerik:** §4.1, §4.2'deki ağaç tabloları (`OnTahsisler` ve `TahakkukKalemDetaylari` hariç), `SpaceTreePolicy`, `SpaceChangePolicy`, `SpaceService`/`SpaceTypeService`/`SpaceAttributeService`/`SpaceRateService`, `SpaceRepository` (ata/torun sorguları), `SpaceLockRepository` (kök kilidi), alan ekranları (§9 F1), izinler `Space`, `SpaceType`, `SpaceAttribute`, seed'in alan kısmı.
- **Dokunulmaz:** `Property`/`Unit` ve bağlıları; mevcut ekranlar çalışmaya devam eder.
- **Migration:** `AddSpaceTree`. DB ilk kez oluşturulur (§10.2).
- **Kabul:** ağaç CRUD; `Yol`/`Derinlik` doğru; 21. seviye reddedilir; K68, K15, K60, K67 (sözleşme henüz yok → m² serbest), K17 uyarısı; tarihçe satırları; policy birim testleri; kök kilidi altında eşzamanlı alt alan ekleme testi.

### F2a — Kesme: Taşınmaz/Birim → Alan

- **İçerik:** §7.1'deki silmeler; `Lease`/`LeaseArea`/`LeaseGroup`/`Charge`/`Reservation`/`ReservationRateOverride`/`PaymentStoreRouting`/`UserPermissionScope`/`Invitation`/`ApplicationUser` alan bağına geçer (§4.3); sözleşme bu fazda **tek alanlı** form ile çalışır (`SozlesmeAlanlari` zaten N satır destekler); kontenjan kodu ve tabloları silinir (K45); tarife zinciri ağaçta ama **hâlâ her ay çözülür** (bugünkü davranış; dondurma F3); rezervasyon kuralı alana bağlı + `BorcTipiId` (K28, K69) ve `ChargeTypeService` koruması; mağaza çözümü alan + ataları; yetki kapsamı `Yol` tabanlı (§6.8, ilk adım: bugünkü "biri kapsamda" davranışı korunur); kiracı kategorisi NOT NULL (K55).
- **Geçici kural (F2a–F3 arası, S-15):** sözleşmeler tek alanlıdır; sözleşmeli tahakkukta `Tahakkuklar.AlanId` sözleşmenin tek `SozlesmeAlanlari` satırının alanıyla **doldurulur** (bugünkü `UnitId` davranışı). Mağaza çözümü ve kapsam filtreleri bu alanı kullanır. F3'te kalem detayı gelince sözleşmeli tahakkukta `AlanId` boş bırakılır (§4.3) ve alan detaydan okunur; DB her fazda sıfırlandığı için veri çevirmesi gerekmez.
- **Migration:** `ReplacePropertyUnitWithSpace`.
- **Kabul:** derleme hatası yok; mevcut sözleşme/fatura/rezervasyon/ödeme akışları tek alanla eskisi gibi çalışır; silinen özelliklerin testleri silinmiş, kalanlar alan modeline çevrilmiş ve yeşil.

### F2b — Çok alanlı sözleşme, doluluk, ön tahsis, kilitler

- **İçerik:** `SpaceOccupancyPolicy`, `PreAllocationPolicy`, `OnTahsisler`, `PreAllocationService`, süre dolumu servisi, kilit protokolü (§6.1) sözleşme/grup/ön tahsis/rezervasyon/alan yollarında, taslak sırası (§6.3), K52, K59 + fesih tarihi sınırı (analiz §13 C2 hatası düzelir), K65, ağaç seçici, ön tahsis ekranı, `TimeProvider`/iş saat dilimi (§6.7), izinler `PreAllocation`.
- **Ara durum (Codex B01):** çok alanlı **taslak** açılır, kaydedilir, revizyona gider; çok alanlı sözleşmenin **onayı kapalıdır** (servis hatası: "çok alanlı onay F3'te"). Tek alanlı akış S-15 ile sürer.
- **Migration:** `AddPreAllocations`.
- **Kabul:** analiz §3.1 Kat 1 senaryosu; §4.1 kuralları 1–9; ata/torun çakışmaları; Sona erdi ve feshedilmiş sözleşmenin tuttuğu aralıklar; eşzamanlılık testleri (aynı anda Kat 1 + Ofis 101; alt alan ekleme ↔ üstü pasifleştirme; rezervasyon ↔ pasifleştirme; taslak ↔ başka kökteki ön tahsisin süre dolumu).

### F3 — Tarife dondurma ve tahakkuk

- **İçerik:** `SozlesmeTarifeler` genişler (`LeaseRate`), `SpaceRateResolver` dondurma (§6.4), K55 eksik tarife + K70, uzatma/yeniden üretim önizlemeleri (K38, K51, K53, K54, K64, K66), `TahakkukKalemleri` anahtarı ve `TahakkukKalemDetaylari` (K30, K39, K40), `LeaseRateSelectionPolicy`, `ChargeCompositionPolicy`, izli iptal (S-4), fesih iptali düzeltmesi (K63), elle borç (K57), yıla düşmenin kalkması.
- **Ara durum (Codex B01):** çok alanlı onay bu fazda açılır; **K34 onay kontrolü F4'ten buraya alınır** (mağaza tek değilse onay engellenir). Görünürlük F4'e kadar geçici olarak "sözleşme alanlarından biri kapsamda" kuralıyla çalışır (yalnız dev DB; canlı yok), F4'te "tüm alanlar" olur.
- **Migration:** `FreezeLeaseRatesAndChargeDetails`.
- **Kabul:** analiz §5.3 örnekleri (Firma C 400/500; çok alanlı 34.500; K64 Haziran–Temmuz 450 / Ağustos 500); kart denemesi Failed olan ayda yeniden üretim FK hatası olmadan izli iptal + yeni tahakkuk; Pending olan ay korunur; kısmen ödenmiş ay ne yeniden üretimde ne fesihte silinir/iptal edilir; Σ detay = kalem.

### F4 — Görünürlük, ödeme mağazası, rapor

- **İçerik:** K32/K47 "tüm alanlar" görünürlüğü (iç ve kiracı kullanıcı), kiracı kullanıcı kısmi kapsam uyarısı, K35 yönlendirme kaydı kontrolü (borçlu kapanmış sözleşmeler dahil, B06) ve `KiraTakip.PaymentRouting` kilidi (§6.1, B03), K33/K49/K56 gelir raporu alan payı, `PaymentRouting` izin adları (S-10).
- **Kabul:** analiz §5.5 Firma X / Ayşe örneği (görmez; raporda 17.297,30); K35 reddi ve listelenen sözleşmeler.

### F5 — Ekranlar

- **İçerik:** ağaç seçicinin son hali, doluluk rozetleri (analiz §3.2), dashboard ve listelerde doluluk, alan detay sekmeleri, fatura kırılım ekranı son hali, menü düzeni.
- **Kabul:** Onur'un tarayıcıda gezinti onayı.

### F6 — Temizlik ve doğrulama

- **İçerik:** ölü kod ve kullanılmayan DTO/izin taraması, yardım sayfaları, `docs/permission-catalog.md`, `PROGRESS.md`, tam test paketi, birleşik denetim. Birleştirme hazırlığı: `DatabaseFixture.cs`/`SeedingExtensions.cs` DB kilidinin, `ApplicationDbContextFactory` ve Web açılışındaki DB adı kilidinin (S-14) ve `CLAUDE.md` notunun geri alınması (birleştirme commit'inde, Onur onayıyla).
- **Sonra (ayrı iş):** canlı taşıma betiği (K62).

## 12. Test planı

- **Birim (Domain):** her yeni policy (§5) için örnek tablolu testler: `SpaceTreePolicy` (yol, ata listesi, derinlik sınırı, K52 çiftleri), `SpaceOccupancyPolicy` (analiz §3.1 Kat 1 senaryosu, durum aralıkları, dışlamalar, grup), `PreAllocationPolicy` (aktiflik sınırı `now == TahsisBitis`, `PlanOnDraft`), `SpaceChangePolicy` (§5.4 tablosunun her satırı), `LeaseRateSelectionPolicy` (K54, K64, K70), `ChargeCompositionPolicy` (K39, K40), `RevenueSharePolicy` (K49 yuvarlama farkı son alana).
- **Entegrasyon (`DBTeknoPark_Test`):** servis akışları (§6), eşzamanlılık (§11 F2b kabulü), yeniden üretim + kart kayıtları (F3 kabulü), kapsam filtreleri (F4).
- **Mimari:** yeni controller'larda her `[HttpPost]` kendi iznine sahip (bugünkü `PropertyArchitectureTests.cs:21` deseni).
- **Silinecek testler:** `Integration/Properties/` altındaki 6 dosya (`PropertyCapacityLockTests`, `PropertyUnitLockTests`, `UnitCapacityChangeTests`, `UnitTypeServiceTests`, `PropertyOccupancy*`, `PropertyDetailsBulkFields*`) ⚠ alan karşılığı olanlar dönüştürülür; `Integration/Leases/{CapacityConcurrencyTests, CapacityLeaseServiceTests, CapacityOccupantQueryTests, LeaseAllocatedAreaUpdateTests}`; `Unit/Leases/UnitCapacityPolicyTests`.
- **Dönüşecek testler:** `Integration/Leases/{LeaseGroupLifecycleTests, LeaseGroupServiceTests}` ve diğer `Integration/Leases` (12 dosya), `Unit/Leases/LeaseGroupPolicyTests`, `Integration/Charges/LeaseAreaChargeGenerationTests`, `Architecture/Properties/*`, Payments/Reservations/Authorization testleri (`UnitId`/`PropertyId` geçen ~76 dosya, kaba sayım).
- Önce ilgili test, faz sonunda tam paket (kullanıcı kuralı).

## 13. Spec'in teknik tercihleri, riskler, doğrulanamayanlar

### 13.1 Teknik tercihler (S-n)

Analizde açıkça karara bağlanmamış uygulama ayrıntıları. Onur aksini söylemezse böyle uygulanır.

| # | Tercih | Neden |
|---|---|---|
| S-1 | `LeaseRateOverride` sınıfı `LeaseRate` olur (tablo adı aynı) | Artık her dondurulmuş fiyatı tutuyor |
| S-2 | `ScopeType` tek değer `Space`; `KullaniciYetkiKapsamlari.KapsamId` → `Alanlar` FK (Restrict) | Polimorfizm kalmadı; alan silinirken kapsam satırlarını servis siler (K60 bağlı tanımlar) |
| S-3 | `AlanTarifeleri` silme soft delete; `SozlesmeTarifeler` kaynak FK'ları Restrict | Dondurulmuş satır kaynağını kaybetmesin |
| S-4 | İzli iptal: eski tahakkukta `YenidenUretimleIptal`, yenisinde `DegistirilenTahakkukId`; önce eski güncellenir, sonra yeni eklenir; dönem unique filtresi bayrağı dışlar | Analiz "kolon adı spec'te" diyor; dönem tekilliği bozulmasın |
| S-5 | Açık sözleşme/grup varken `Paylasimli` bayrağı değişmez | Analiz §6.1 bu satırı ayrıca yazmıyor; K26/K67 ile tutarlı en dar seçenek |
| S-6 | Yeni kök eklemede kilit alınmaz | Henüz başka işlem o köke dokunamaz |
| S-7 | Bugünkü varsayılan rezervasyon kuralı seed'de kök alana kural olarak yazılır | Kural yoksa rezervasyon yapılamaz (K28) |
| S-8 | İş saat dilimi `SystemSettings` anahtarı, varsayılan Europe/Istanbul | Analiz "ayardan" diyor; mevcut ayar altyapısı |
| S-9 | Dört kapsam DTO'su yerine tek `SpaceAccessScope` | Kapsam mantığı tek yerde (K14, K32) |
| S-10 | `PaymentRouting` izinlerinin endpoint adına (`Save`, `Deactivate`) çekilmesi F4'te | Proje kuralı; analiz "eylemler kalır" dediği için ayrı fazda |
| S-11 | Her fazda `dotnet ef database drop` + `update` + seed (yalnız `DBTeknoPark_Test`) — **Onur onayladı (2026-11-26)** | K62: migration veri çevirmez; eski dummy satırlar yeni kısıtlara takılmasın. K61'deki "sıfırlama yok" migration geçmişi içindir (mevcut 20 migration korunur), DB içeriği için değil |
| S-12 | F2'nin F2a/F2b diye bölünmesi | Çok alanlı sözleşme, `Unit` kalkmadan yazılamaz; faz başına derleme yeşil kalsın |
| S-13 | Analizde F4'te olan rezervasyon kuralı (K28, K69, `ChargeTypeService` koruması), mağaza çözümünün alana inmesi ve yol tabanlı kapsam F2a'ya alınır; F4'te K32/K47, K34/K35, K33/K49 kalır | `UnitType`/`Unit`/`Property` F2a'da silindiği için bunlara bağlı kod aynı fazda alana geçmek zorunda |
| S-14 | DB adı kilidi design-time factory'ye ve Web açılışına (seed'den önce) eklenir; F1'in ilk adımı | Bugünkü kilit `dotnet ef` ve `IdentitySeedService` yolunu korumuyor (§10.1) |
| S-15 | F2a–F3 arasında sözleşmeli tahakkukta `AlanId` tek sözleşme alanıyla dolu; F2b'de çok alanlı sözleşme yalnız taslak, onay F3'te (Codex B01) | Kalem detayı F3'te gelir; ara fazda mağaza ve kapsam alanı bir yerden okumalı |
| S-16 | **Açık sözleşme** = Taslak, Revizyon İstendi, Aktif ve fesih tarihi henüz gelmemiş Feshedildi — **Onur onayladı (2026-11-26)** | K18, K67, K35, pasifleştirme, S-5'te kullanılan tanım; doluluk aralığı (K59) ayrı kavramdır |

### 13.2 Riskler

- **F2a büyük:** ~160 dosya. Derleme hataları rehber; faz içinde alt adımlar (entity → repository → servis → controller/view → test) sırayla, her adımda derleme.
- **Yetki kapsamı geçişi:** 40+ yerde dağınık filtre; F2a'da davranış korunur, F4'te K32 uygulanır. Kaçan bir filtre veri sızdırır → F4'te kapsam testleri her repository için.
- **Yeniden üretim:** gerçek silme + kart FK'sı; S-4 sırası ve kilitler test edilmeden F3 kapanmaz.
- **Zaman:** `DateTime.Now`/`Today` kalıntıları (§6.5, §6.7) iş saat dilimi hatası üretir; F2b/F3'te tarama.
- **Dış teyit:** K39, K40, K49 (muhasebe), K48 (operasyon) değişirse F3/F4 etkilenir.

### 13.3 Doğrulanamayanlar (uygulamadan önce koda bak)

- Sözleşme taslağı reddinin ayrı bir metodu olup olmadığı (`LeaseService.cs` içinde `Reject` bulunamadı; `RequestRevisionAsync` `:265`, `DeleteDraftAsync` `:286`).
- `CapacityOccupantQuery` statü filtresi (silineceği için önemsiz).
- `IdentitySeedService` içeriği; `AuditDisplayNames`/`AdminAuditLog`'daki Taşınmaz/Birim adları.
- Kalan küçük `PropertyIds`/`UnitIds` dosyaları (~35; DTO/validator) — derleme gösterir.

