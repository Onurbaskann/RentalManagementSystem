# Permission Mimari Tasarımı

> ⚠️ **GÜNCEL DEĞİL.** Bu dosya Ara Refactor — Permission Hiyerarşisi ile aşıldı.
> Güncel mimari: `docs/refactor-permission-hiyerarsisi.md`. Eski `UserPermission` tablosu,
> `View` implication ve Rol-bazlı bypass bu refactor'da kaldırıldı.

---

## 1. Yetki Katmanları

```
┌─────────────────────────────────────┐
│ Katman 1: ROL                       │
│   Admin / Yonetici / Goruntuleyici  │
├─────────────────────────────────────┤
│ Katman 2: PERMISSION                │
│   Tasinmaz.Create, Sozlesme.Extend  │
├─────────────────────────────────────┤
│ Katman 3: KAPSAM (UserTasinmazYetki)│
│   Kullanıcı hangi taşınmazları gör. │
└─────────────────────────────────────┘
```

**Yetki kontrolü mantığı:**
1. Kullanıcı authenticated mi?
2. Rolü Admin mi? → Erişim ver, atla.
3. Permission var mı? (`UserPermission` tablosu)
4. (View ise) Goruntuleyici ise: kapsamı kontrol et (`UserTasinmazYetki`)

---

## 2. Veritabanı Modeli

### `UserPermission` (Yeni)

```
Id              int PK
UserId          string FK → AspNetUsers.Id
Permission      string (örn: "Sozlesme.Terminate")
GrantedBy       string FK → AspNetUsers.Id (nullable)
GrantedAt       DateTime
```

**Index:** `(UserId, Permission)` unique
**Cascade:** Kullanıcı silinirse permission'ları da silinir

### `UserTasinmazYetki` (Mevcut, korunacak)

```
Id              int PK
UserId          string FK → AspNetUsers.Id
TasinmazId      int FK → Tasinmazlar.Id (Faz 2'de FK eklenecek)
AtanmaTarihi    DateTime
AtayanUserId    string FK → AspNetUsers.Id (nullable)
```

---

## 3. ASP.NET Core Entegrasyonu

### Stratej: Claims tabanlı Policy

**Login sırasında:**
- Kullanıcının `UserPermission` kayıtları okunur
- Her permission için `Claim(type: "permission", value: "Tasinmaz.Create")` eklenir
- Admin rolü varsa: tüm permission'lar otomatik claim olarak yüklenir

**Policy tanımları (`Program.cs`):**
```
services.AddAuthorization(options =>
{
    foreach (var perm in PermissionCatalog.All)
        options.AddPolicy(perm, p => p.RequireClaim("permission", perm));
});
```

**Controller kullanımı:**
```
[Authorize(Policy = "Sozlesme.Terminate")]
public IActionResult Feshet(...) { ... }
```

**Servis kullanımı (programatik):**
```
await _authService.AuthorizeAsync(user, "Odeme.Approve");
```

---

## 4. Sınıf Hiyerarşisi (Plan)

```
Models/
  UserPermission.cs                 (yeni entity)

Services/
  IPermissionService.cs             (yeni interface)
  PermissionService.cs              (UserPermission CRUD)

Authorization/
  PermissionCatalog.cs              (statik liste, permission-catalog.md ile senkron)
  PermissionClaimsTransformer.cs    (login'de claims'e permission yükler)
  AdminBypassHandler.cs             (Admin rolü tüm policy'leri geçer)
```

---

## 5. Admin UI (Faz 4'te)

`/Admin/Kullanicilar/Edit` ekranında:
- Kullanıcının rolü seçilir
- **Goruntuleyici** seçilirse: sadece `*.View` checkbox'ları aktif, diğerleri disabled
- **Yonetici** seçilirse: tüm permission checkbox'ları aktif
- **Admin** seçilirse: tüm permission'lar otomatik (UI'da gösterilir ama düzenlenemez)
- Submit'te `UserPermission` tablosu güncellenir (delta hesaplanır)

---

## 6. Goruntuleyici + Permission Birleşimi

Goruntuleyici rolündeki kullanıcı için:
- Permission listesi: sadece atanmış olanlar (`UserPermission`)
- Görünür kayıtlar: sadece `UserTasinmazYetki`'de eşleşen taşınmazlar
- **İki kontrol birleşir:**
  ```
  TasinmazService.ListAsync(userId):
    if Admin → tümü
    if has "Tasinmaz.View" permission:
      if Goruntuleyici → sadece UserTasinmazYetki ile filtreli
      else → tümü
    else → boş liste
  ```

---

## 7. Sözleşme/Kiracı için Kapsam Türetme

`UserTasinmazYetki` sadece taşınmaz bazlıdır. Ama Goruntuleyici sözleşme veya kiracı görmek isteyince:

```
SozlesmeService.ListAsync(userId):
    yetkiliTasinmazIds = UserTasinmazYetki(userId)
    yetkiliBirimIds = Birim where TasinmazId in yetkiliTasinmazIds
    return Sozlesmeler where BirimId in yetkiliBirimIds

KiraciService.ListAsync(userId):
    yetkiliTasinmazIds = UserTasinmazYetki(userId)
    aktiveSozlesmeler = ... (yukarıdaki gibi türet)
    return Kiraciler where Id in aktiveSozlesmeler.KiraciId
```

Bu mantık `UserTasinmazYetkiService` içinde helper metotlarla tutulur.

---

## 8. Audit Notu

`UserPermission` tablosunda `GrantedBy` ve `GrantedAt` alanları audit izi sağlar. İleride:
- `UserPermissionHistory` tablosu eklenebilir (silme/değişiklik kayıtları)
- Şu an sadece son durum tutulur, geçmiş tutulmaz.

---

## 9. Permission Catalog Senkronizasyonu

`Authorization/PermissionCatalog.cs` ile `docs/permission-catalog.md` aynı listeyi içerir.
**Kural:** Yeni permission eklerken her iki yerde birden güncellenir. Tek doğruluk kaynağı yok; manuel senkronizasyon. (İleride code-gen yapılabilir.)
