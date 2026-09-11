# Faz 19 — Kontrollü Yayın ve Geri Dönüş Runbook'u

> **Tarih:** 2026-08-08  
> **Kapsam:** Sözleşme başvuru, onay, revizyon ve onay sonrası tahakkuk akışı  
> **Durum:** Test DB doğrulandı; gerçek kullanıcı UAT'si ve üretim yayını bekliyor

## 1. Yayın öncesi zorunlu kontroller

- Veritabanının geri yüklenebilir tam yedeğini alın ve geri yükleme denemesinin sorumlusunu kaydedin.
- Uygulama dosyalarının önceki çalışan sürümünü geri dönüş paketi olarak saklayın.
- `20260807103848_AddLeaseApprovalWorkflow` migration'ının hedef DB'de uygulanıp uygulanmadığını kontrol edin.
- Açık başvuru, taslak tahakkuku ve mükerrer tahakkuk sağlık sorgularını çalıştırın.
- Operasyon rolüne aşağıdaki izinlerin atanacağını iş sahibiyle doğrulayın:
  - `Internal.Lease.Approve`
  - `Internal.Lease.RequestRevision`
  - `Internal.Lease.DeleteDraft`
- Başvuru sahibi ve değerlendirici için birbirinden farklı iki UAT kullanıcısı hazırlayın.

## 2. Permission ve claim geçişi

Yeni izinler `PermissionCatalog` ve operasyon preset'inde tanımlıdır; fakat mevcut rol
kayıtlarına otomatik permission backfill yapılmaz.

1. Yönetim panelinde ilgili iç kullanıcı rolünü açın.
2. Onayla, Revizyon İste ve Başvuru Sil izinlerini iş kararına göre atayın.
3. Rol üyelerinin mevcut oturumlarını kapatıp yeniden giriş yapmasını sağlayın.
4. Yeniden girişten sonra normal başvuru sahibinin kendi başvurusunda karar butonu görmediğini,
   değerlendiricinin ise yalnız kendisine atanmış kararları gördüğünü doğrulayın.
   `IsSuperAdmin=true` kullanıcısı bu self-review kuralının kontrollü istisnasıdır.
5. Tenant rollerine hiçbir `Internal.Lease.*` karar izni atamayın.

## 3. Migration uygulama

Hedef bağlantı ayarı deployment ortamından sağlanmalıdır; bağlantı bilgisi komuta veya
dokümana yazılmamalıdır.

```powershell
dotnet ef database update 20260807103848_AddLeaseApprovalWorkflow `
  --project KiraTakip.csproj `
  --startup-project KiraTakip.csproj
```

Migration sonrası `__EFMigrationsHistory` tablosunda hem `InitialCreate` hem de
`AddLeaseApprovalWorkflow` kayıtları bulunmalıdır.

## 4. Yayın sonrası smoke/UAT sırası

1. Kullanıcı A yeni başvuru oluşturur.
2. Başvuru Draft görünür; Charge ve Creation activity oluşmadığı DB'den doğrulanır.
3. Normal kullanıcı A kendi başvurusunda Onayla/Revizyon İste/Sil kararlarını göremez;
   ayrıca bir `IsSuperAdmin` hesabıyla istisna butonları ve servis işlemi doğrulanır.
4. Kullanıcı B aynı başvuruyu salt-okunur görür.
5. Zorunlu belge eksikken onay reddedilir.
6. Kullanıcı A belgeyi tamamlar.
7. Kullanıcı B açıklamayla revizyon ister.
8. Kullanıcı A revizyonu tamamlayıp yeniden gönderir.
9. Kullanıcı B başvuruyu onaylar.
10. Lease Active olur; tek Creation activity ve tek dönemsel tahakkuk seti oluşur.
11. Tenant portalında yalnız onaylanan sözleşme görünür.
12. Timeline'da DraftCreated, RevisionRequested, Resubmitted ve Approved ayrı satırlardır.

Silme smoke'u ayrı bir taslak üzerinde, zorunlu açıklamayla yapılmalıdır. Silinen taslak
normal listeden kalkmalı; review history korunmalı ve birim yeni başvuruya açılmalıdır.

## 5. DB sağlık sorguları

Aşağıdaki sorguların tamamı `0` dönmelidir:

```sql
-- Birim başına birden fazla açık başvuru
SELECT COUNT(*)
FROM (
    SELECT BirimId
    FROM Sozlesmeler
    WHERE IsDeleted = 0 AND Durum IN (4, 5)
    GROUP BY BirimId
    HAVING COUNT(*) > 1
) AS Duplicates;

-- Taslak/revizyon durumunda tahakkuk
SELECT COUNT(*)
FROM Tahakkuklar AS T
JOIN Sozlesmeler AS S ON S.Id = T.SozlesmeId
WHERE S.IsDeleted = 0 AND S.Durum IN (4, 5) AND T.IsDeleted = 0;

-- Taslak/revizyon durumunda Creation activity
SELECT COUNT(*)
FROM SozlesmeIslemGecmisleri AS H
JOIN Sozlesmeler AS S ON S.Id = H.SozlesmeId
WHERE S.IsDeleted = 0 AND S.Durum IN (4, 5)
  AND H.IsDeleted = 0 AND H.IslemTipi = 1;

-- Aynı sözleşme ve dönem için mükerrer tahakkuk
SELECT COUNT(*)
FROM (
    SELECT SozlesmeId, DonemBaslangici
    FROM Tahakkuklar
    WHERE IsDeleted = 0 AND SozlesmeId IS NOT NULL AND KaynakTipi = 1
    GROUP BY SozlesmeId, DonemBaslangici
    HAVING COUNT(*) > 1
) AS Duplicates;
```

## 6. Geri dönüş planı

Migration uygulandıktan ve yeni akışta veri üretildikten sonra migration'ı doğrudan
`InitialCreate` seviyesine indirmek güvenli geri dönüş yöntemi değildir; review history,
RowVersion ve yeni durum anlamları kaybedilebilir.

Kritik hata durumunda:

1. Yeni başvuru işlemlerini durdurun ve uygulamayı bakım moduna alın.
2. Hata zamanını, etkilenen LeaseId'leri ve oluşmuş Charge kayıtlarını kaydedin.
3. Uygulama + DB birlikte geri alınacaksa yayın öncesi doğrulanmış DB yedeğini geri yükleyin.
4. Yalnız uygulama geri alınacaksa eski sürümün Draft/Revision durumlarını güvenli biçimde
   ele alabildiği kanıtlanmadan trafiği açmayın.
5. Veri düzeltmesini manuel `DELETE`/status güncellemesiyle yapmayın; olay bazlı düzeltme
   planı ve ayrı onay kullanın.

## 7. Gözlemlenebilirlik notu

Uygulamada kalıcı bir teknik hata log sink'i yapılandırılmamıştır. Yayın sırasında platform
stdout/Event Log kayıtları ayrıca toplanmalı; FK, concurrency, duplicate charge ve transaction
rollback hataları özellikle izlenmelidir. Kalıcı merkezi loglama ayrı bir iyileştirme olarak
planlanmalıdır.
