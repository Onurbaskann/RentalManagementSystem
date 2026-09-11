# Permission Hiyerarşisi Refaktörü — Implementation Spec

**Durum:** 🟡 Tasarım onaylandı, uygulamaya hazır (2026-06-29)

> Bu dosya **kontrat**tır. Spec dışına çıkma. Karar değişikliği için önce buraya yansıt.

---

## Amaç

Mevcut "flat string + tek yönlü View implication" izin modelini, **endpoint odaklı + UI'da ağaç yapılı + backend'de düz claim** modeline çevirmek.

**Üç temel hedef:**

1. **Self-documenting permission:** Her claim, korunan endpoint'i string olarak ifade eder. `Internal.Sozlesme.Feshet` görünce hangi action olduğu net.
2. **Tek izin = tek endpoint:** Sürpriz hiyerarşi yok. Backend claim'de ne yazıyorsa onu kontrol eder, otomatik genişletme yapmaz.
3. **UI'da gruplanmış keşfedilebilirlik:** Tree dropdown ile parent-child ilişkisi görsel olarak görünür, toplu işlem yapılabilir, ama veri tabanında saklanan claim'ler net kalır.

---

## 🚫 NEGATİF LİSTE — KESİNLİKLE YAPMA

### Yapısal
- ❌ Wildcard claim formatı tanıtma (`Internal.Sozlesme.*` gibi)
- ❌ Claim-time genişletme (parent claim verirsen child'ları otomatik eklemek)
- ❌ Check-time genişletme (claim kontrolünde parent path'lere bakmak)
- ❌ "View" permission'ını farklı isim altında tutmak (`Read`, `Index`, `List` vs.)
- ❌ Modül seviyesinde `Manage` izni
- ❌ Action hiyerarşisi (Edit'in Create'i içermesi vs.)

### Naming
- ❌ Modül adlarını Türkçeleştirme (ileride yapılacak büyük refactor'a saklanıyor)
- ❌ Action adlarını Türkçeleştirme (Create/Edit/Delete/Approve/Reject vs. İngilizce kalır)
- ❌ Prefix'leri değiştirme (Internal/System/Kiraci/Kiraci.System aynen korunur)

### UI
- ❌ Düz checkbox listesi (mevcut Create.cshtml stili)
- ❌ Parent ve child'ları bağımsız muamele etme — UI'da parent seçilince child'lar otomatik işaretlenir
- ❌ Tag helper olmadan view'larda kopya `@if (User.HasClaim(...))` bloğu

---

## §1. Permission Format Kuralları

### §1.1 Seviye Yapısı

| Seviye | İçerik | Kullanım |
|---|---|---|
| 1 | `Scope` | `Internal`, `System`, `Kiraci`, `Kiraci.System` (2 seviyeli prefix) |
| 2 | `Module` | Controller adı (Sozlesme, Tahakkuk, Kullanici vs.) |
| 3 | `Action` | Sadece state değiştiren POST endpoint'leri için |

### §1.2 GET endpoint'leri — 2. seviye

**Kural:** Modülün **tüm** GET endpoint'leri tek bir 2. seviye izinle korunur.

| Endpoint | İzin |
|---|---|
| `GET /Sozlesme` (Index) | `Internal.Sozlesme` |
| `GET /Sozlesme/Detay/{id}` | `Internal.Sozlesme` |
| `GET /Sozlesme/Ekle` (form ekranı) | `Internal.Sozlesme` |
| `GET /Sozlesme/Duzenle/{id}` (form ekranı) | `Internal.Sozlesme` |

> **Önemli:** `Ekle` ve `Duzenle` form ekranları da modül seviyesinde açılır. POST'a yetki yoksa form açılır ama Kaydet butonu render edilmez (§3.2).

### §1.3 State Değiştiren POST endpoint'leri — 3. seviye

**Kural:** Backend'de Create / Update / Delete veya state değişikliği yapan her POST endpoint için ayrı 3. seviye izin.

| Endpoint | İzin |
|---|---|
| `POST /Sozlesme/Ekle` | `Internal.Sozlesme.Create` |
| `POST /Sozlesme/Duzenle/{id}` | `Internal.Sozlesme.Edit` |
| `POST /Sozlesme/Sil/{id}` | `Internal.Sozlesme.Delete` |
| `POST /Sozlesme/Feshet/{id}` | `Internal.Sozlesme.Terminate` |
| `POST /Sozlesme/Uzat/{id}` | `Internal.Sozlesme.Extend` |
| `POST /Odeme/Onayla/{id}` | `Internal.Odeme.Approve` |
| `POST /Odeme/Reddet/{id}` | `Internal.Odeme.Reject` |
| `POST /Tahakkuk/YenidenUret/{id}` | `Internal.Tahakkuk.Regenerate` |

### §1.4 Scope Prefix'leri

Aynen korunur:

| Prefix | Kullanım |
|---|---|
| `Internal.*` | İç ekip operasyonel modüller |
| `System.*` | İç ekip sistem yönetimi (Kullanici, Rol, Davetiye, Audit) |
| `Kiraci.*` | Kiracı portal operasyonel modüller |
| `Kiraci.System.*` | Kiracı firma içi yönetim (Kullanici, Rol, Davetiye) — **4 seviyeli** |

`Kiraci.System.Kullanici` (GET) ve `Kiraci.System.Kullanici.Invite` (POST) gibi 4-seviyeli izinler ağaç yapısında doğal olarak temsil edilir.

---

## §2. Kaldırılacaklar

### §2.1 `.View` Action'ları
Tüm `.View` sonlu izinler silinir. Karşılığı **modül seviyesi izin** olur.

| Eski | Yeni |
|---|---|
| `Internal.Sozlesme.View` | `Internal.Sozlesme` |
| `System.Kullanici.View` | `System.Kullanici` |
| `Kiraci.Sozlesme.View` | `Kiraci.Sozlesme` |
| `Kiraci.System.Kullanici.View` | `Kiraci.System.Kullanici` |

### §2.2 `.Manage` Action'ları
Tüm `.Manage` izinleri silinir; gerçek endpoint'lere bölünür.

**Etkilenen modüller:**
- `Internal.BorcTipi.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.BelgeTuru.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.Tarife.Manage` → `.Create`, `.Edit`, `.Delete` (gerçek endpoint setine göre)
- `Internal.Parametre.Manage` → `.Edit` (gerçekte sadece güncelleme var mı, controller bazlı kontrol et)
- `Internal.TasinmazTipi.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.BirimTuru.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.KiraciKategori.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.Sektor.Manage` → `.Create`, `.Edit`, `.Delete`
- `Internal.TasinmazCarpan.Manage` → controller bazlı
- `Internal.RezervasyonTarifeKural.Manage` → controller bazlı
- `Internal.Birim.ManageRate` → `Internal.Birim.OverrideRate` veya benzeri (özel isim, controller'a bakılacak)
- `Kiraci.Mutabakat.Manage` → controller'a göre netleştirilecek
- `Kiraci.System.Kullanici.Manage` → controller'a göre netleştirilecek

> **Uygulama notu:** Her `.Manage` için controller'ı açıp endpoint'leri tek tek listele, ona göre 3. seviye action izni belirle.

### §2.3 `ExpandWithImpliedViews` kuralı
`PermissionClaimsTransformer.ExpandWithImpliedViews` metodu tamamen silinir. Backend, claim'lere hiçbir şey eklemez.

---

## §3. UI Tasarımı

### §3.1 Tree Dropdown Picker

`Roller/Ekle` ve `Roller/Duzenle` ekranlarındaki düz checkbox listesi yerine **ağaç yapılı dropdown** kullanılır.

**Görsel yapı:**

```
İzinler:  [ Seç... 3 modül, 12 izin atanmış  ▾ ]

(dropdown açık)
┌────────────────────────────────────────────────┐
│ 🔍 Ara: [____________]                         │
├────────────────────────────────────────────────┤
│ Internal                                       │
│ ├─ ☑ Sozlesme                                  │
│ │   ├─ ☑ Create                                │
│ │   ├─ ☑ Edit                                  │
│ │   ├─ ☐ Delete         ← child unchecked,    │
│ │   ├─ ☑ Extend            parent CHECKED kalır│
│ │   ├─ ☑ Terminate                             │
│ │   └─ ☐ OverrideRate                          │
│ ├─ ☐ Tahakkuk                                  │
│ │   └─ ☐ Regenerate                            │
│ └─ ☑ BorcTipi (sadece görüntüleme)             │
│     ├─ ☐ Create                                │
│     ├─ ☐ Edit                                  │
│     └─ ☐ Delete                                │
├────────────────────────────────────────────────┤
│ System                                         │
│ ├─ ☐ Kullanici                                 │
│ │   ├─ ☐ Create                                │
│ │   └─ ☐ Edit                                  │
│ └─ ☐ Rol                                       │
│     ├─ ...                                     │
└────────────────────────────────────────────────┘
```

### §3.2 Tıklama Davranışı

| Aksiyon | Sonuç |
|---|---|
| Parent checkbox işaretlenir | Tüm child'lar otomatik işaretlenir |
| Parent checkbox kaldırılır | Tüm child'lar otomatik kaldırılır |
| Child işaretlenir | **Parent otomatik işaretlenir** ve kullanıcı bu parent'ı kaldıramaz (disabled değil, ama uncheck child'lar kalır parent kalkmaz) |
| Child kaldırılır (parent checkli iken) | **Yalnızca o child kaldırılır**, parent checkli kalır → kullanıcı sadece GET izni alır |
| Parent işaretsiz iken child işaretlenir | Parent otomatik işaretlenir (yukarıdaki kural) |

**Özet ilke:** Child seçili olabilmek için parent zorunlu olarak seçili olmalıdır. Bu UI tarafında zorlanır, backend'de değil — backend, gelen listeyi olduğu gibi kaydeder; ancak UI iki kuralı garanti eder:
1. Bir child seçiliyse parent kesinlikle seçilidir (form submit anında validate edilir).
2. Parent seçili ama child seçili değilse → kullanıcı bilinçli olarak "sadece görüntüleme" istemiştir.

### §3.3 Görsel Detaylar

- **Indeterminate state:** Parent'ın bazı child'ları seçili, bazıları değilse parent checkbox'ı yarım işaretli görünür (HTML `indeterminate`).
- **Arama:** Dropdown üstünde input — yazılan metne göre node'lar filtrelenir, eşleşen node'ların atası açık tutulur.
- **Sayaç:** Picker düğmesinde "X modül, Y izin atanmış" gibi özet.
- **Renk:** Mevcut indigo accent yerine `#1a6b5c` primary teal kullanılır (proje tutarlılığı).
- **Risk rozeti:** `Delete`, `Cancel`, `Terminate`, `OverrideRate`, `Approve`, `Reject` gibi yıkıcı/yetki devreden action'ların yanına küçük kırmızı veya kehribar nokta.

### §3.4 Permission Tag Helper

Tüm view'lardaki manuel `@if (User.HasClaim(AppClaimTypes.Permission, X)) { ... }` bloklarını kısaltmak için tag helper:

```html
<!-- Eski -->
@if (User.HasClaim(AppClaimTypes.Permission, PermissionCatalog.Sozlesme.Create))
{
    <button class="btn btn-primary">Kaydet</button>
}

<!-- Yeni -->
<button asp-permission="@PermissionCatalog.Sozlesme.Create" class="btn btn-primary">Kaydet</button>
```

`asp-permission` attribute'u olan element, yetki yoksa hiç render edilmez. Form GET ile gelen "Ekle/Duzenle" ekranlarında kaydet butonu bu yöntemle conditional olur.

---

## §4. Form GET / POST Davranışı (Soru 4 Kararı — B Seçeneği)

### §4.1 Kural

> Form GET endpoint'i (`/Sozlesme/Ekle` GET, `/Sozlesme/Duzenle/{id}` GET) modül seviyesi `Internal.Sozlesme` ile korunur. Form gönderme endpoint'i (`POST`) ise 3. seviye `.Create` veya `.Edit` ile korunur.

### §4.2 Razor Davranışı

Form ekranını açan kullanıcının yazma izni yoksa:
- Üstte uyarı banner'ı: *"Bu kaydı görüntüleyebilirsiniz, değiştiremezsiniz."*
- Tüm input/select/textarea elementleri `disabled` (veya `readonly`)
- "Kaydet" / "Güncelle" butonu hiç render edilmez (`asp-permission` tag helper ile)
- "İptal" / "Geri" butonu kalır
- Validation summary alanı kalır ama boş olur

### §4.3 Backend Davranışı

- `GET` endpoint: `[Authorize(Policy = PermissionCatalog.Sozlesme.Module)]` → modül seviyesi
- `POST` endpoint: `[Authorize(Policy = PermissionCatalog.Sozlesme.Create)]` → 3. seviye

POST endpoint'i yetkisiz çağrılırsa 403 döner; URL manipülasyonu işe yaramaz.

### §4.4 Risk Değerlendirmesi

- **Bilgi sızıntısı:** Yok. Form input alanları zaten listede görülebilir kayıtlardan tahmin edilebilir.
- **Yanlışlıkla doldurma:** Düşük. Kaydet butonu olmadığı ve input'lar disabled olduğu için kullanıcı dolduramaz.
- **Backend güvenliği:** Tam. POST kuralı 3. seviye ile koruma altında.

---

## §5. PermissionCatalog Yeniden Düzenleme

### §5.1 Modül Sınıfı Şablonu

```csharp
public static class Sozlesme
{
    /// <summary>Modül seviyesi izin — tüm GET endpoint'leri için.</summary>
    public const string Module = "Internal.Sozlesme";

    public const string Create    = "Internal.Sozlesme.Create";
    public const string Edit      = "Internal.Sozlesme.Edit";
    public const string Delete    = "Internal.Sozlesme.Delete";
    public const string Extend    = "Internal.Sozlesme.Extend";
    public const string Terminate = "Internal.Sozlesme.Terminate";

    /// <summary>Modülün tüm 3. seviye action'ları.</summary>
    public static readonly IReadOnlyList<string> Actions =
        [Create, Edit, Delete, Extend, Terminate];
}
```

- `Module` const'u GET endpoint'lerinin korunmasında kullanılır.
- `Actions` listesi UI'da child'ları üretmek için kullanılır.
- `.View` ve `.Manage` tamamen kaldırılır.

### §5.2 PermissionCatalog.All Yeniden Yapılandırılır

Eski format: tek düz string listesi.
Yeni format: modül-bazlı yapısal — UI bunu okuyarak ağaç inşa eder.

```csharp
public static readonly IReadOnlyList<PermissionModuleInfo> AllModules =
[
    new("Internal.Sozlesme", "Sözleşme", Sozlesme.Actions),
    new("Internal.Tahakkuk", "Tahakkuk", Tahakkuk.Actions),
    // ...
    new("System.Kullanici", "Kullanıcı", Kullanici.Actions),
    new("Kiraci.Sozlesme", "Kiracı — Sözleşme", KiraciPortal.Sozlesme.Actions),
    new("Kiraci.System.Kullanici", "Kiracı Yönetim — Kullanıcı", KiraciPortal.System.Kullanici.Actions),
];

public record PermissionModuleInfo(string Path, string DisplayName, IReadOnlyList<string> Actions);
```

UI bu liste üzerinden ağacı inşa eder. `Path` içindeki noktalar derinliği belirler.

### §5.3 KiraciYoneticisi/KiraciSorumlusu Preset'leri

Mevcut listeler güncel formatla yeniden yazılır:

```csharp
public static readonly IReadOnlyList<string> KiraciYoneticisiIzinleri =
[
    KiraciPortal.Sozlesme.Module,         // eski: .View
    KiraciPortal.Borc.Module,             // eski: .View
    KiraciPortal.Odeme.Module,            // eski: .View
    KiraciPortal.Cari.Module,             // eski: .View
    KiraciPortal.Mutabakat.Module,        // eski: .Manage → patlatma kararı netleşince
    KiraciPortal.Rezervasyon.Module,
    KiraciPortal.Rezervasyon.Create,
    KiraciPortal.Rezervasyon.Cancel,
    KiraciPortal.System.Kullanici.Module,
    KiraciPortal.System.Kullanici.Invite,
    // ...
];
```

---

## §6. AdminBypassHandler ve Identity Seed

### §6.1 AdminBypassHandler
`SistemYoneticisi` rolü için tüm policy kontrollerini geçen bypass aynen korunur. Hiç değişiklik gerekmez — bu, claim mekanizmasından bağımsız çalışıyor.

### §6.2 PermissionClaimsTransformer
`ExpandWithImpliedViews` silinir. Kalan kod:

```csharp
if (roles.Contains(RoleNames.SistemYoneticisi))
{
    // Tüm AllModules.Module + AllModules.Actions claim olarak eklenir
    foreach (var m in PermissionCatalog.AllModules)
    {
        identity.AddClaim(new Claim(AppClaimTypes.Permission, m.Path));
        foreach (var a in m.Actions)
            identity.AddClaim(new Claim(AppClaimTypes.Permission, a));
    }
}
else
{
    var rolePerms = await _userRolService.GetUserPermissionsFromRolesAsync(user.Id);
    foreach (var p in rolePerms.Distinct())
        identity.AddClaim(new Claim(AppClaimTypes.Permission, p));
}
```

Genişletme yok — claim'de ne varsa o.

---

## §7. Migration Planı

### §7.1 Mevcut Veri
Dummy data, üretim verisi yok. `RolPermissions` tablosundaki mevcut string'ler programatik olarak dönüştürülecek.

### §7.2 Adımlar

**Aşama A — Catalog Refactor (Kod)**
1. `PermissionCatalog.cs` yeniden yazılır: her modül sınıfında `Module` const'u, `.View` ve `.Manage` silinir, `.Manage` olanlar gerçek action'lara bölünür.
2. `PermissionModuleInfo` record'u ve `AllModules` listesi tanımlanır.
3. `PermissionClaimsTransformer.ExpandWithImpliedViews` silinir.
4. `Program.cs` içindeki policy kayıtları yeni isim listesine göre güncellenir.

**Aşama B — Controller Refactor**
1. Tüm `[Authorize(Policy = PermissionCatalog.X.View)]` → `[Authorize(Policy = PermissionCatalog.X.Module)]`
2. Tüm `[Authorize(Policy = PermissionCatalog.X.Manage)]` → endpoint'e göre `.Create`, `.Edit`, `.Delete` veya yeni özel action izni
3. `[Authorize(Policy = PermissionCatalog.X.Create/Edit/Delete vs.)]` zaten doğru, dokunulmaz

**Aşama C — View Refactor**
1. Tüm `User.HasClaim(AppClaimTypes.Permission, X.View)` → `X.Module`
2. Tüm `User.HasClaim(AppClaimTypes.Permission, X.Manage)` → uygun action izni
3. `asp-permission` tag helper eklenir
4. Form view'larında kaydet/güncelle butonları conditional render edilir; uyarı banner'ı ve disabled input pattern'i uygulanır

**Aşama D — UI Tree Picker**
1. `Roller/Create.cshtml` ve `Edit.cshtml` yeniden yazılır
2. Tree picker component'i (Alpine.js + Tailwind, mevcut kütüphanelerle) inşa edilir
3. `KiraciRol/Create.cshtml` ve `Edit.cshtml` aynı picker'ı kullanır
4. Arama, indeterminate state, sayaç, risk rozeti detayları uygulanır

**Aşama E — Migration & Seed**
1. `IdentitySeedService` ve `SeedDataService` içindeki rol-permission eşleştirmeleri yeni formata güncellenir
2. Migration sıfırlandığı için ek bir EF migration gerekmez; ancak `DbInitializer` veya seed scripti güncel `PermissionCatalog.All*` listelerinden besleneceği için kendiliğinden yeniden yapılandırılır
3. `RolPermission` tablosu seed sırasında temizlenip yeniden doldurulur (dummy ortamda risksiz)

**Aşama F — Test**
1. Her permission policy için bir test rolü oluşturup endpoint erişimi doğrulanır
2. Form GET/POST ayrımı manuel test edilir (B seçeneği davranışı)
3. UI tree picker tüm scope'larda (Internal/System/Kiraci/Kiraci.System) render edilir

### §7.3 Geri Alma Stratejisi
Tüm değişiklikler dummy ortamda yapıldığı için geri alma = git revert. Veritabanı zaten seed'lenir.

---

## §8. Kapsam Dışı (Bu refactor'da YAPMAYACAĞIMIZ)

- ❌ Modül adlarının Türkçeleştirilmesi (ileride büyük refactor)
- ❌ Action adlarının Türkçeleştirilmesi
- ❌ Controller method isimlerinin İngilizceleştirilmesi
- ❌ Yetki kapsamı (UserTasinmazYetki / row-level) sisteminde değişiklik
- ❌ Yeni controller/endpoint ekleme
- ❌ Audit log şemasında değişiklik

---

## §9. Acceptance Checklist

- [ ] `PermissionCatalog.cs` yeniden yazıldı, `.View` ve `.Manage` tamamen kaldırıldı, `AllModules` yapısı eklendi
- [ ] `PermissionClaimsTransformer.ExpandWithImpliedViews` silindi
- [ ] Tüm controller'lardaki `[Authorize(Policy = ...)]` attribute'ları güncellendi
- [ ] Tüm view'lardaki `User.HasClaim(...)` çağrıları güncellendi
- [ ] `asp-permission` tag helper'ı eklendi ve view'larda kullanıldı
- [ ] Form GET / POST davranışı B seçeneğine göre uygulandı (uyarı banner + disabled inputs + butonsuz form)
- [ ] `Roller/Create.cshtml` ve `Edit.cshtml` tree picker ile yeniden yazıldı
- [ ] `KiraciRol/Create.cshtml` ve `Edit.cshtml` aynı tree picker'ı kullanıyor
- [ ] Tree picker: arama, indeterminate state, parent-child auto-check kuralları çalışıyor
- [ ] Kiracı portal izinlerinde aynı kural uygulandı (`Kiraci.*` ve `Kiraci.System.*`)
- [ ] `KiraciYoneticisi` ve `KiraciSorumlusu` preset listeleri yeni formatla güncellendi
- [ ] `IdentitySeedService` / `SeedDataService` yeni catalog'tan besleniyor
- [ ] Yetkisiz POST denemesi 403 dönüyor
- [ ] Build temiz: `dotnet build KiraTakip/KiraTakip.csproj`
- [ ] `SistemYoneticisi` AdminBypassHandler ile her şeye erişebiliyor (regresyon yok)

---

## §10. Referanslar

- Mevcut catalog: `Authorization/PermissionCatalog.cs`
- Claim transformer: `Authorization/PermissionClaimsTransformer.cs`
- Admin bypass: `Authorization/AdminBypassHandler.cs`
- Rol service: `Services/RolService.cs`
- Mevcut Create UI: `Views/AdminRol/Create.cshtml`
- Mevcut Edit UI: `Views/AdminRol/Edit.cshtml`
- Kiracı rol UI: `Views/KiraciRol/Create.cshtml`, `Edit.cshtml`
- Önceki permission spec: `docs/permission-spec.md`, `docs/permission-catalog.md`

> Bu refactor tamamlandıktan sonra `docs/permission-spec.md` ve `docs/permission-catalog.md` güncellenmeli (veya bu dosyaya redirect edilmeli).
