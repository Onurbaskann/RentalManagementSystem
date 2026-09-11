# Faz 17 — Yetki Kapsamı (Permission-Bazlı Taşınmaz Kapsamı)

> **Plan dokümanı.** Karar tarihi: 2026-06-19. Implementasyon ayrı oturumda yapılacak.
> Bu faz, **Faz 16 Karar 12**'yi (`UserTasinmazYetki` aynen korunur) ve **16F**'deki
> `IIcKapsamFiltresi`'yi **genelleştirerek geçersiz kılar** (bkz. §3.9).
> Esin kaynağı: Unisis/ABP "Kullanıcı-İzin-Organizasyon" iki katmanlı yetki modeli;
> KiraTakip domain'ine "Yetki Kapsamı" olarak uyarlanmıştır.

---

## 1. Bağlam ve Hedef

Bugün yetki kontrolü tek katmanlı: kullanıcının bir aksiyona erişip erişemeyeceği
permission policy'siyle belirlenir ([Program.cs:54-61](../KiraTakip/Program.cs#L54)),
izinler login'de claim'e gömülür ([PermissionClaimsTransformer.cs](../KiraTakip/Authorization/PermissionClaimsTransformer.cs)).
Satır-bazlı kısıt yalnızca `Goruntuleyici` rolüne özel, tek eksenli bir mekanizma olarak var:
`UserTasinmazYetki` (user→tasinmaz) + `IcKapsamFiltresi` ([IcKapsamFiltresi.cs](../KiraTakip/Services/IcKapsamFiltresi.cs)).

Bu faz **ikinci bir bağımsız katman** ekler: **Yetki Kapsamı** — "erişebildiği işlemi
hangi taşınmazlar üzerinde yapabilir?" Böylece herhangi bir Internal kullanıcı, rolden
bağımsız olarak belirli taşınmazlara kapsamlanabilir.

**Hedef (bu faz):** Internal kullanıcılar için **permission-aware Taşınmaz kapsamı.**
**Kapsam dışı (bu faz):** Kiracı tarafı dokunulmaz; `Birim` kapsamı; full per-permission
atama; Redis. (Hepsi ileriye açık — §6.)

---

## 2. Temel Karar Özeti

| # | Konu | Karar |
|---|---|---|
| 1 | Model | İki bağımsız katman: **Aksiyon yetkisi** (mevcut, değişmez) + **Yetki Kapsamı** (yeni) |
| 2 | İlk hedef | Internal kullanıcılar için **Taşınmaz** kapsamı |
| 3 | Kiracı tarafı | **Değişmez** — `KiraciId` + global query filter devam (Faz 16E) |
| 4 | Kapsam tipi | `Tasinmaz` ile başlar; `KapsamTipi` enum'una `Birim` için yer ayrılır |
| 5 | Atama granülerliği | **(B) Hibrit** — satır = (user, tasinmaz) tek kapsam seti; tüm *kapsamlı* permission'lara uygulanır. Full (user, permission, tasinmaz) seçilmedi (§6) |
| 6 | Kapsamlı vs global | Permission'lar ikiye ayrılır; filtre yalnızca **kapsamlı** olanlara uygulanır (§3.2) |
| 7 | Cache | `IMemoryCache`, **`IYetkiKapsamiCache` soyutlaması** arkasında; Redis sonra |
| 8 | Enforcement | Aksiyon yetkisi controller'da kalır; kapsam = scoped `IYetkiKapsamiProvider` + servis/repo guard |
| 9 | Global erişim | Liste şişirmemek için **flag** ile (Unisis `IsGlobalAccess` mantığı); Admin/Yonetici global, Goruntuleyici kapsamlı (başlangıç) |
| 10 | Göç | `UserTasinmazYetki` + `IcKapsamFiltresi` yeni modele taşınır; Faz 16 Karar 12 geçersiz (§3.9) |

---

## 3. Mimari Karar Detayları

### 3.1 İki Katman

- **Katman 1 — Aksiyon yetkisi (DEĞİŞMEZ).** `[Authorize(Policy = PermissionCatalog.X)]`,
  login claim'i, `AdminBypassHandler`. Mevcut sistem aynen korunur.
- **Katman 2 — Yetki Kapsamı (YENİ).** Request pipeline'ında ayrı noktada: cache → provider
  → (okuma filtresi + yazma guard). Katman 1'den bağımsız çalışır.

### 3.2 Kapsamlı (scope-aware) vs Global Permission

Her permission taşınmaza bağlı değildir. `PermissionCatalog`'a bir işaret (`ScopeAware` listesi)
eklenir:

- **Kapsamlı:** `Internal.Tasinmaz.*`, `Internal.Birim.*`, `Internal.Kiraci.*`,
  `Internal.Sozlesme.*`, `Internal.Odeme.*`, `Internal.ManuelBorc.*`,
  `Internal.Rezervasyon.*`, `Internal.Tahakkuk.*` — bir taşınmaza çözülebilen veriler.
- **Global (kapsamsız):** `Internal.Kullanici.*`, `Internal.Rol.*`, `Internal.Davetiye.*`,
  `Internal.Audit.*`, `Internal.Parametre.*`, `*Tipi/*Turu/Sektor.*`, `Internal.Tarife.*`,
  `Internal.BorcTipi.*`, `Internal.Bildirim.*` — taşınmazdan bağımsız.

Provider yalnızca **kapsamlı** permission'larda taşınmaz filtresi uygular; global permission'larda
"kısıt yok" döner.

> **Birim → Tasinmaz çözümü:** Kapsamlı entity'lerin çoğu taşınmaza dolaylı bağlıdır
> (Sozlesme → Birim → Tasinmaz). Okuma filtreleri ve yazma guard'ları, ilgili entity'nin
> `TasinmazId`'sini bu zincirden çözer.

### 3.3 Veri Modeli ve Göç

**Yeni tablo — `KullaniciYetkiKapsami`:**

```
KullaniciYetkiKapsami
├── Id (int, PK)
├── UserId (string, FK → AspNetUsers)
├── KapsamTipi (int — enum: Tasinmaz=1, [Birim=2 ileride])
├── KapsamId (int — şimdilik TasinmazId)
├── AtayanUserId (string?, FK → AspNetUsers)
├── AtanmaTarihi (datetime)
├── CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (IAuditable)
└── UNIQUE INDEX (UserId, KapsamTipi, KapsamId)
```

(B) hibrit kararı gereği satırlar permission içermez — kapsam seti kullanıcının **tüm
kapsamlı permission'larına** uygulanır.

**Global erişim flag'i:** `ApplicationUser.TumTasinmazlaraErisim` (bool, default false) **veya**
başlangıçta rolden türetme (Admin/Yonetici → global, Goruntuleyici → kapsamlı). Flag set ise
kapsam filtresi hiç uygulanmaz (cache'te taşınmaz listesi tutulmaz → şişme önlenir).

**`UserTasinmazYetki` göçü:** Dummy veri olduğu için ([CLAUDE.md "Veri Durumu"]) geriye dönük
kaygı yok. `UserTasinmazYetki` satırları `KullaniciYetkiKapsami`'ye (KapsamTipi=Tasinmaz) taşınır;
eski tablo emekliye ayrılır. `UserTasinmazYetkiService`/`Repository` ve `IcKapsamFiltresi`
yeni provider'a evrilir.

### 3.4 Cache Katmanı

```csharp
public interface IYetkiKapsamiCache
{
    Task<KullaniciKapsamDto> GetAsync(string userId);   // yoksa DB'den yükle + yaz
    void Invalidate(string userId);
    void InvalidateMany(IEnumerable<string> userIds);    // rol/kapsam fan-out
}

public class KullaniciKapsamDto
{
    public bool GlobalErisim { get; set; }
    public List<int> TasinmazIds { get; set; } = new();  // GlobalErisim=true ise boş/yok sayılır
}
```

- İmplementasyon: `IMemoryCache`. Key: `YetkiKapsami_{userId}`, ayarlanabilir TTL.
- **Redis sonra:** interface değişmeden `IDistributedCache` tabanlı implementasyon eklenir.

**Invalidation noktaları (fan-out dahil):**
- Kullanıcının kapsamı değişti → `Invalidate(userId)`.
- Kullanıcının rolü/aktiflik durumu değişti → `Invalidate(userId)`.
- `TumTasinmazlaraErisim` flag'i değişti → `Invalidate(userId)`.
- Bir taşınmaz silindi/pasifleşti → etkilenen kullanıcılar `InvalidateMany(...)`.

> **SecurityStamp ile ilişki:** Yetki kapsamı claim'e gömülü olmadığı için (cache + per-request
> provider) anlık etki eder; aktif oturumu düşürmeye gerek yok. SecurityStamp yalnızca aksiyon
> yetkisi/rol değişiminde devrede kalır (Faz 16).

### 3.5 Provider + Action Filter

```csharp
public interface IYetkiKapsamiProvider   // scoped
{
    bool GlobalErisim { get; }
    IReadOnlyList<int> ErisilebilirTasinmazIds { get; }
    bool KapsamdaMi(int tasinmazId);                 // global ise her zaman true
    void TasinmazGuard(int tasinmazId);              // kapsamda değilse 403/exception
}
```

- **`YetkiKapsamiActionFilter` (`IAsyncActionFilter`, global kayıt):** request başında,
  action'ın gerektirdiği permission **kapsamlıysa** cache'den kullanıcının kapsamını yükler ve
  scoped provider'ı doldurur. Kapsamsız/global aksiyonlarda provider "kısıt yok" durumunda kalır.
- `IYetkiKapsamiProvider`, `IIcKapsamFiltresi`'nin yerini alır.

### 3.6 Enforcement

- **Okuma (liste filtresi):** servis/repository, `GlobalErisim` değilse sorguyu
  `ErisilebilirTasinmazIds` ile daraltır. (Bugün `_kapsamFiltresi` enjekte eden controller'ların
  —`SozlesmeController` vb.— çağrıları provider'a taşınır.)
- **Yazma (defansif guard):** kapsamlı servis/repo işlemlerinde `provider.TasinmazGuard(tasinmazId)`.
  Dropdown filtreli gelse de manipüle edilmiş request'e karşı arka kapı güvencesi.

### 3.7 Rol Davranışı (başlangıç)

| Rol | Kapsam |
|---|---|
| Admin | `GlobalErisim = true` (bypass) |
| Yonetici | Global (tüm taşınmaz) |
| Goruntuleyici | Kapsamlı (atanmış taşınmazlar) — bugünkü davranışın permission-aware karşılığı |

### 3.8 Kiracı Tarafı

Hiç dokunulmaz. `RequireKiraciIdAttribute` + `KiraciId` claim + global query filter (Faz 16E)
devam eder. Yeni provider/filter yalnızca Internal alanında çalışır.

### 3.9 Faz 16 Karar 12 / 16F Supersession

Faz 16 Karar 12 "`UserTasinmazYetki` aynen korunur, Görüntüleyici'ye özel iş kuralı" demiş ve
16F'de `IIcKapsamFiltresi` eklenmişti. Bu faz her ikisini **genelleştirerek değiştirir**:
kapsam artık tek role bağlı değil, herhangi bir Internal kullanıcıya atanabilir; tek eksenli
user→tasinmaz yerine `KapsamTipi` ile genişleyebilen (`Birim` ileride) model gelir.
`phase-16` dokümanına bu noktada "Faz 17 ile superseded" işareti düşülür.

---

## 4. Veri Şeması — Değişiklik Listesi

### Yeni Tablolar
| Tablo | Amaç |
|---|---|
| `KullaniciYetkiKapsami` | (user, tasinmaz) kapsam seti; ileride `Birim` |

### Mevcut Tablolarda Değişiklik
| Tablo | Değişiklik |
|---|---|
| `AspNetUsers` | `TumTasinmazlaraErisim` (bool, default false) — *opsiyonel; rolden türetme tercih edilirse eklenmez* |

### Emekliye Ayrılan
| Tablo | Not |
|---|---|
| `UserTasinmazYetki` | Satırları `KullaniciYetkiKapsami`'ye göç; tablo kaldırılır (dummy veri) |

---

## 5. Alt Faz Haritası

### 17A — Katalog & Veri Modeli
1. `PermissionCatalog`'a `ScopeAware` işareti (kapsamlı izin listesi).
2. `KapsamTipi` enum (`Tasinmaz`, [`Birim` rezerve]).
3. `KullaniciYetkiKapsami` entity + DbContext + unique index.
4. Migration + `UserTasinmazYetki` → `KullaniciYetkiKapsami` veri göçü.
5. Global erişim kararı (flag kolonu vs rolden türetme) sabitlenir.

### 17B — Cache & Provider & Filter
6. `IYetkiKapsamiCache` (IMemoryCache impl) + invalidation noktaları.
7. `IYetkiKapsamiProvider` (scoped) + `YetkiKapsamiActionFilter` + DI/global filter kaydı.

### 17C — Enforcement Göçü
8. `IcKapsamFiltresi` çağrılarının provider'a taşınması (okuma filtreleri).
9. Kapsamlı servislere yazma guard'larının (`TasinmazGuard`) eklenmesi.
10. `IcKapsamFiltresi` / `UserTasinmazYetki(Service/Repository)` temizliği.

### 17D — Atama UI & Doğrulama
11. Kullanıcıya taşınmaz kapsamı atama ekranı (Admin) + `TumTasinmazlaraErisim` toggle.
12. Audit olayları (`User.ScopeChanged`).
13. Faz sonu doğrulama: build temiz, testler yeşil, kapsam sızıntısı el ile test
    (Goruntuleyici atanmamış taşınmazı görmemeli; yazma guard manipüle request'i reddetmeli).

---

## 6. Açık Riskler / İleri İş

- **(A) Full per-permission kapsam:** (user, permission, tasinmaz) granülerliği seçilmedi (UI maliyeti).
  Model (B)'den (A)'ya, `KullaniciYetkiKapsami`'ye `Permission` kolonu + provider'da permission
  parametresi eklenerek genişletilebilir.
- **`Birim` kapsam tipi:** `KapsamTipi` enum + provider çözümü hazır; ikinci tip eklenince
  Tasinmaz→Birim daraltma mantığı netleştirilir.
- **Redis:** `IYetkiKapsamiCache` arkasında `IDistributedCache` implementasyonu.
- **Tree görünüm:** Unisis'teki `TreeOrganizationIds` (UI ağacı için üst düğümler) bu fazda yok;
  Bina/Birim ağacı eklenirse provider'a eklenir — yetki vermez, yalnızca UI içindir.
- **Kiracı tarafına kapsam:** Şimdilik gerekmiyor (global query filter yeterli). Gerekirse aynı
  altyapı `Scope=Kiraci` için genişletilir.
