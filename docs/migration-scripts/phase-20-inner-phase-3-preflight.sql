-- Faz 20 / İç Faz 3 — AddLineItemBasedPaymentCore migration'ı öncesi salt-okunur kontrol.
--
-- Bu script HİÇBİR satırı DEĞİŞTİRMEZ; yalnız SELECT çalıştırır.
--
-- R1 veya R2 dolu dönerse: ilgili ödemelerin hangi tahakkuk kalemine ait olduğu operasyon
-- ekibi tarafından elle belirlenip
--   UPDATE TahakkukOdemeleri SET TahakkukKalemiId = <kalemId> WHERE Id = <odemeId>;
-- ile yazılmalıdır. Tahmine dayalı otomatik dağıtım YAPILMAZ (Faz 20 ana plan kararı).
-- Migration'ın kendi backfill'i (B1) yalnız TEK kalemli tahakkukların ödemelerini otomatik
-- bağlar; R1'de listelenen çok kalemli tahakkuk ödemeleri migration'ın THROW guard'ını
-- tetikler ve migration uygulanamaz.
--
-- R3 dolu dönerse: backfill sonrasında bir kalemin OdenenTutar'ı ToplamTutar'ını aşacak
-- demektir (CK_TahakkukKalemleri_OdenenLimit ihlali) — migration guard'ı bunu da durdurur;
-- kök neden datadaki tutarsız/eksik ödeme kaydıdır ve elle incelenmelidir.

-- R1: Birden fazla kalemi olan tahakkukların ödemeleri (migration B1 tarafından otomatik
-- bağlanamaz).
SELECT
    o.Id AS OdemeId,
    o.TahakkukId,
    o.Tutar,
    o.Durum,
    (SELECT COUNT(*) FROM TahakkukKalemleri k2
     WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0) AS KalemSayisi
FROM TahakkukOdemeleri o
WHERE o.IsDeleted = 0
  AND (SELECT COUNT(*) FROM TahakkukKalemleri k2
       WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0) > 1;

-- R2: Hiç (silinmemiş) kalemi olmayan tahakkukların ödemeleri.
SELECT
    o.Id AS OdemeId,
    o.TahakkukId,
    o.Tutar,
    o.Durum
FROM TahakkukOdemeleri o
WHERE o.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM TahakkukKalemleri k2
      WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0);

-- R3: Backfill sonrasında SUM(onaylı ödeme) > ToplamTutar olacak kalemler (yalnız tek
-- kalemli tahakkukların ödemeleri hesaba katılır; R1'de listelenenler B1 tarafından hiç
-- bağlanmayacağı için bu hesaba dahil değildir).
SELECT
    k.Id AS KalemId,
    k.TahakkukId,
    k.ToplamTutar,
    onayli.OnayliToplam
FROM TahakkukKalemleri k
CROSS APPLY (
    SELECT SUM(o.Tutar) AS OnayliToplam
    FROM TahakkukOdemeleri o
    WHERE o.IsDeleted = 0
      AND o.Durum = 2 -- Approved
      AND o.TahakkukId = k.TahakkukId
      AND (SELECT COUNT(*) FROM TahakkukKalemleri k2
           WHERE k2.TahakkukId = k.TahakkukId AND k2.IsDeleted = 0) = 1
) onayli
WHERE k.IsDeleted = 0
  AND onayli.OnayliToplam > k.ToplamTutar;
