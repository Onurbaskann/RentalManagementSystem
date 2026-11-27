# Permission Catalog

> ⚠️ **GÜNCEL DEĞİL.** Bu dosya Ara Refactor — Permission Hiyerarşisi ile aşıldı.
> Kanonik kaynak: `docs/refactor-permission-hiyerarsisi.md` ve `KiraTakip/Authorization/PermissionCatalog.cs`.
> Eski format (`Tasinmaz.View`, `Birim.ManageRate` vb.) artık kullanılmıyor.

---

## Alan (Alan Hiyerarşisi, F1)

| Permission | Açıklama |
|------------|----------|
| `Internal.Space` | Alan modülü; GET `Space`, `Space/{id}`, `Space/Delete/{id}` (silme önizlemesi) |
| `Internal.Space.Create` | `POST Space/Create` — kök veya alt alan ekle |
| `Internal.Space.Edit` | `POST Space/Edit` — alan düzenle |
| `Internal.Space.Deactivate` | `POST Space/Deactivate` — alanı alt ağacıyla pasifleştir |
| `Internal.Space.Activate` | `POST Space/Activate` — alanı etkinleştir |
| `Internal.Space.Delete` | `POST Space/Delete` — alanı sil (onay) |
| `Internal.Space.EditRate` | `POST Space/EditRate` — alan tarifesi ekle/güncelle/sil |
| `Internal.Space.EditAddress` | `POST Space/EditAddress` — kök alan adresi |
| `Internal.Space.EditAttributes` | `POST Space/EditAttributes` — alan özellik değerleri |

## Alan Türü (Parametre)

| Permission | Açıklama |
|------------|----------|
| `Internal.SpaceType` | Alan türleri listesi/formu (`/Admin/SpaceType`) |
| `Internal.SpaceType.Create` | `POST Admin/SpaceType/Create` |
| `Internal.SpaceType.Edit` | `POST Admin/SpaceType/Edit` |
| `Internal.SpaceType.ToggleStatus` | `POST Admin/SpaceType/ToggleStatus` — aktif/pasif |

## Alan Özelliği (Parametre)

| Permission | Açıklama |
|------------|----------|
| `Internal.SpaceAttribute` | Alan özellik kataloğu (`/Admin/SpaceAttribute`) |
| `Internal.SpaceAttribute.Create` | `POST Admin/SpaceAttribute/Create` |
| `Internal.SpaceAttribute.Edit` | `POST Admin/SpaceAttribute/Edit` |
| `Internal.SpaceAttribute.ToggleStatus` | `POST Admin/SpaceAttribute/ToggleStatus` — aktif/pasif |

## Kiracı

| Permission | Açıklama |
|------------|----------|
| `Kiraci.View` | Kiracı listele/detay |
| `Kiraci.Create` | Kiracı ekle |
| `Kiraci.Edit` | Kiracı düzenle |

## Sözleşme

| Permission | Açıklama |
|------------|----------|
| `Sozlesme.View` | Sözleşme listele/detay |
| `Sozlesme.Create` | Sözleşme oluştur |
| `Sozlesme.Edit` | Sözleşme düzenle (TÜFE/KDV güncelleme dahil) |
| `Sozlesme.Extend` | Sözleşme süresi uzat |
| `Sozlesme.Terminate` | Sözleşme feshet |
| `Sozlesme.OverrideRate` | Sözleşme kalem fiyatına elle müdahale (SozlesmeRate) yetkisini yönet |
| `Internal.Lease.UpdateDraft` | `POST Lease/UpdateDraft` — başvuru taslağı düzenle (eski: `Lease.Create`); seed'de `Lease.Create` sahibi rollere verilir |
| `Internal.Lease.ResubmitRevision` | `POST Lease/ResubmitRevision` — revizyon istenen başvuruyu yeniden gönder (eski: `Lease.Create`); `Lease.Create` sahiplerine |
| `Internal.Lease.UploadDraftDocument` | `POST Lease/UploadDraftDocument` — başvuru belgesi yükle (eski: `Lease.Create`); `Lease.Create` sahiplerine |
| `Internal.Lease.UpdateDueDate` | `POST Lease/UpdateDueDate` — vade günü düzenle (eski: `Lease.Edit`); `Lease.Edit` sahiplerine |
| `Internal.ReservationRateRule.ToggleStatus` | `POST Admin/ReservationRateOverride/ToggleStatus` — kural aktif/pasif (eski: `ReservationRateRule.Edit`); `ReservationRateRule.Edit` sahiplerine |
| `Internal.ChargeType.ToggleStatus` | `POST Admin/ChargeType/ToggleStatus/{id} — aktif/pasif` (eski: `ChargeType.Edit`); `ChargeType.Edit` sahiplerine |
| `Internal.ChargeType.ChangeSortOrder` | `POST Admin/ChargeType/ChangeSortOrder/{id} — sıra değiştir` (eski: `ChargeType.Edit`); `ChargeType.Edit` sahiplerine |
| `Internal.DocumentType.ToggleStatus` | `POST Admin/DocumentType/ToggleStatus/{id} — aktif/pasif` (eski: `DocumentType.Edit`); `DocumentType.Edit` sahiplerine |
| `Internal.Sector.ToggleStatus` | `POST Admin/Sector/ToggleStatus/{id} — aktif/pasif` (eski: `Sector.Edit`); `Sector.Edit` sahiplerine |
| `Internal.TenantCategory.ToggleStatus` | `POST Admin/TenantCategory/ToggleStatus/{id} — aktif/pasif` (eski: `TenantCategory.Edit`); `TenantCategory.Edit` sahiplerine |
| `Internal.Store.ToggleStatus` | `POST Admin/Store/ToggleStatus/{id} — aktif/pasif` (eski: `Store.Edit`); `Store.Edit` sahiplerine |
| `Internal.Store.DeactivateAccount` | `POST Admin/Store/DeactivateAccount/{id}/{accountId} — hesap pasifleştir` (eski: `Store.Account`); `Store.Account` sahiplerine |
| `Internal.Store.ReplaceAccount` | `POST Admin/Store/ReplaceAccount/{id} — hesap değiştir` (eski: `Store.Account`); `Store.Account` sahiplerine |
| `Internal.RateSchedule.ToggleStatus` | `POST Admin/Rate/ToggleStatus/{year} — aktif/pasif` (eski: `RateSchedule.Edit`); `RateSchedule.Edit` sahiplerine |
| `Internal.RateSchedule.UpdateLine` | `POST Admin/Rate/Year/{year}/UpdateLine — tarife satırı güncelle` (eski: `RateSchedule.Edit`); `RateSchedule.Edit` sahiplerine |
| `Internal.Payment.UnmatchBankTransaction` | `POST BankTransaction/Unmatch — banka eşleşmesini kaldır; ScopeAware` (eski: `Payment.MatchBankTransaction`); `Payment.MatchBankTransaction` sahiplerine |
| `Internal.Payment.RemovePaymentMatch` | `POST BankTransaction/RemovePaymentMatch — ödeme eşleşmesini kaldır; ScopeAware` (eski: `Payment.MatchBankTransaction`); `Payment.MatchBankTransaction` sahiplerine |
| `Tenant.System.User.CancelInvite` | `POST Tenant/Users/Invite/Cancel/{id} — daveti iptal et` (eski: `Tenant.System.User.Invite`); `Tenant.System.User.Invite` sahiplerine |
| `Tenant.System.User.ResendInvite` | `POST Tenant/Users/Invite/Resend/{id} — daveti yeniden gönder` (eski: `Tenant.System.User.Invite`); `Tenant.System.User.Invite` sahiplerine |
| `Tenant.Payment.Report` | `POST Tenant/Charges/ReportPayment — ödeme bildir` (eski: `Tenant.Payment (modül)`); `Tenant.Payment (modül)` sahiplerine |
| `Tenant.Payment.StartOnline` | `POST Tenant/Charges/StartOnlinePayment — online ödeme başlat` (eski: `Tenant.Payment (modül)`); `Tenant.Payment (modül)` sahiplerine |
| `Internal.Notification.BorcHatirlatma` | `POST ChargeReminder/SendDebtorEmails`; ScopeAware (alan kapsamı uygulanır) |

> Kaldırıldı (alan hiyerarşisi F2a/P7a): `Internal.Property.*`, `Internal.Unit.*` (Create/Edit/OverrideRate/UpdateCapacity), `Internal.PropertyType.*`, `Internal.UnitType.*`, `Internal.PropertyMultiplier.*`, `Internal.Lease.UpdateAllocatedArea`.
>
> Kaldırıldı (P7e, hiçbir endpoint'i korumuyordu): `Internal.Store.Account` (yerine `Store.DeactivateAccount`/`ReplaceAccount`), `Internal.RateSchedule.Edit` (yerine `RateSchedule.ToggleStatus`/`UpdateLine`), `System.User.Create` (kullanıcı davetle oluşur: `Invitation.Create`), `System.User.AssignPermission` (kullanılmıyordu).

## Ödeme (Faz 5'te aktifleşir, şimdiden tanımlı)

| Permission | Açıklama |
|------------|----------|
| `Odeme.View` | Tahakkuk/ödeme listele |
| `Odeme.Create` | Ödeme kaydı gir |
| `Odeme.UploadDekont` | Dekont dosyası yükle |
| `Odeme.Approve` | Ödeme onayla |
| `Odeme.Reject` | Ödeme reddet |
| `Odeme.ImportBankStatement` | Banka hareketi içe aktar (CSV/Excel) |
| `Odeme.MatchBankTransaction` | Banka hareketi-ödeme eşleştir |

## Kullanıcı Yönetimi

| Permission | Açıklama |
|------------|----------|
| `Kullanici.View` | Kullanıcı listele |
| `Kullanici.Create` | Kullanıcı ekle |
| `Kullanici.Edit` | Kullanıcı düzenle (rol/şifre dahil) |
| `Kullanici.AssignPermission` | Kullanıcıya permission ata |

## Parametre Yönetimi (Faz 8)

| Permission | Açıklama |
|------------|----------|
| `Parametre.View` | Parametre ekranlarını görüntüle (genel) |
| `Parametre.Manage` | Parametre kayıtlarını yönet (genel) |
| `KiraciKategori.View` | Kiracı kategorilerini görüntüle |
| `KiraciKategori.Manage` | Kiracı kategorilerini yönet |
| `Sektor.View` | Sektörleri görüntüle |
| `Sektor.Manage` | Sektörleri yönet |
| `TasinmazCarpan.View` | (Eski/Tarihsel) Taşınmaz kategori çarpanlarını görüntüle — PermissionCatalog.cs'de `TasinmazCarpanPerm` olarak tutuldu |
| `TasinmazCarpan.Manage` | (Eski/Tarihsel) Taşınmaz kategori çarpanlarını yönet — PermissionCatalog.cs'de `TasinmazCarpanPerm` olarak tutuldu |

## Tarife ve Fiyatlandırma (Faz 7/9/11)

| Permission | Açıklama |
|------------|----------|
| `Tarife.View` | Genel tarife matrisini görüntüle |
| `Tarife.Manage` | Genel tarife matrisini yönet |

## Tahakkuk (Faz 7)

| Permission | Açıklama |
|------------|----------|
| `Tahakkuk.Regenerate` | Sözleşme tahakkuklarını yeniden üret |

## Borç Tipi (Faz 7)

| Permission | Açıklama |
|------------|----------|
| `BorcTipi.Manage` | Borç tiplerini yönet (Admin) |

## Rezervasyon Ücret Kuralı (Faz 8)

| Permission | Açıklama |
|------------|----------|
| `RezervasyonUcretKural.Manage` | Toplantı salonu rezervasyon ücret kurallarını yönet |

## Rezervasyon (Faz 8)

| Permission | Açıklama |
|------------|----------|
| `Rezervasyon.View` | Rezervasyon listele/detay |
| `Rezervasyon.Create` | Rezervasyon oluştur |
| `Rezervasyon.Edit` | Rezervasyon düzenle |
| `Rezervasyon.Cancel` | Rezervasyon iptal et |
| `Rezervasyon.TransferToTahakkuk` | Rezervasyonu tahakkuka aktar |

## Manuel Borç (Faz 8)

| Permission | Açıklama |
|------------|----------|
| `ManuelBorc.View` | Manuel borç listele/detay |
| `ManuelBorc.Create` | Manuel borç ekle |
| `ManuelBorc.Cancel` | Manuel borç iptal et |

## Bildirim (Faz 13)

| Permission | Açıklama |
|------------|----------|
| `Bildirim.BorcHatirlatma` | Borçlulara/kiracılara borç hatırlatma maili gönder |

---

## Rol-Permission Varsayılan Eşleşmesi

| Permission | Admin | Yonetici | Goruntuleyici |
|------------|:-----:|:--------:|:-------------:|
| `*.View` (tümü) | ✅ | ✅ | ✅ (sadece atanmış taşınmazlar) |
| `Kiraci.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Sozlesme.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Sozlesme.Extend/Terminate` | ✅ | ⚙️ | ❌ |
| `Odeme.*` | ✅ | ⚙️ | ❌ |
| `Kullanici.*` | ✅ | ❌ | ❌ |
| `Parametre.*` / `KiraciKategori.*` / `Sektor.*` | ✅ | ❌ | ❌ |
| `Tarife.*` / `BorcTipi.Manage` / `RezervasyonUcretKural.Manage` | ✅ | ❌ | ❌ |
| `TasinmazCarpan.*` | ✅ | ⚙️ | ❌ |
| `Sozlesme.OverrideRate` / `Tahakkuk.Regenerate` | ✅ | ⚙️ | ❌ |
| `Rezervasyon.View` / `ManuelBorc.View` | ✅ | ⚙️ | ✅ (kapsam) |
| `Rezervasyon.Create/Edit/Cancel/TransferToTahakkuk` | ✅ | ⚙️ | ❌ |
| `ManuelBorc.Create/Cancel` | ✅ | ⚙️ | ❌ |
| `Bildirim.BorcHatirlatma` | ✅ | ⚙️ | ❌ |

**Lejant:**
- ✅ = Otomatik sahip (kod düzeyinde)
- ⚙️ = Admin tarafından atanabilir (UserPermission tablosunda kayıt)
- ❌ = Verilemez

---

## Kurallar

1. **Admin** → tüm permission'lara otomatik sahiptir, kod düzeyinde bypass uygulanır.
2. **Goruntuleyici** → sadece `*.View` permission'ları alabilir; Create/Edit/Delete asla atanmaz.
3. **Yonetici** → varsayılan olarak hiçbir özel permission yok; Admin atar.
4. **Permission isimleri kanoniktir.** Yeni permission eklerken bu dosyayı güncelle.
5. **Kontrol noktası:** Servis katmanı (controller değil). Controller `[Authorize(Policy = "...")]` ile entry kontrolü yapar.
6. **Senkronizasyon:** Bu listedeki her değişiklik `Authorization/PermissionCatalog.cs` dosyasındaki sabitlere de işlenmelidir.
