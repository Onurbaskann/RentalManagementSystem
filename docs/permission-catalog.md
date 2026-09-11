# Permission Catalog

> ⚠️ **GÜNCEL DEĞİL.** Bu dosya Ara Refactor — Permission Hiyerarşisi ile aşıldı.
> Kanonik kaynak: `docs/refactor-permission-hiyerarsisi.md` ve `KiraTakip/Authorization/PermissionCatalog.cs`.
> Eski format (`Tasinmaz.View`, `Birim.ManageRate` vb.) artık kullanılmıyor.

---

## Taşınmaz

| Permission | Açıklama |
|------------|----------|
| `Tasinmaz.View` | Taşınmaz listele/detay görüntüle |
| `Tasinmaz.Create` | Yeni taşınmaz ekle |
| `Tasinmaz.Edit` | Taşınmaz düzenle |

## Birim (Taşınmaza bağlı)

| Permission | Açıklama |
|------------|----------|
| `Birim.View` | Birim görüntüle |
| `Birim.Create` | Birim ekle |
| `Birim.Edit` | Birim düzenle |
| `Birim.ManageRate` | Birim özel fiyat (BirimRate) yönet |

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
| `TasinmazTipi.View` | Taşınmaz tiplerini görüntüle |
| `TasinmazTipi.Manage` | Taşınmaz tiplerini yönet |
| `BirimTuru.View` | Birim türlerini görüntüle |
| `BirimTuru.Manage` | Birim türlerini yönet |
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
| `Tasinmaz.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Birim.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Kiraci.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Sozlesme.Create/Edit` | ✅ | ⚙️ | ❌ |
| `Sozlesme.Extend/Terminate` | ✅ | ⚙️ | ❌ |
| `Odeme.*` | ✅ | ⚙️ | ❌ |
| `Kullanici.*` | ✅ | ❌ | ❌ |
| `Parametre.*` / `TasinmazTipi.*` / `BirimTuru.*` / `KiraciKategori.*` / `Sektor.*` | ✅ | ❌ | ❌ |
| `Tarife.*` / `BorcTipi.Manage` / `RezervasyonUcretKural.Manage` | ✅ | ❌ | ❌ |
| `TasinmazCarpan.*` | ✅ | ⚙️ | ❌ |
| `Birim.ManageRate` / `Sozlesme.OverrideRate` / `Tahakkuk.Regenerate` | ✅ | ⚙️ | ❌ |
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
