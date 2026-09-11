-- Faz 20 / İç Faz 4 — AddBankTransactionStoreAccount migration'ı öncesi salt-okunur kontrol.
--
-- Bu script HİÇBİR satırı DEĞİŞTİRMEZ; yalnız SELECT çalıştırır.
--
-- R1 dolu dönerse: bu banka hareketleri hiçbir ödemeyle eşleşmemiş, dolayısıyla migration'ın
-- kendi backfill'i (eşleşmiş ödemenin mağaza hesabından) bunlara mağaza hesabı çözemez.
-- Migration'ın guard'ı bu durumda THROW ile durur. Çözüm iki yoldan biri:
--   (a) İlgili hareketin hangi mağaza hesabına ait olduğu elle belirlenip
--       UPDATE BankaHareketleri SET MagazaHesapBilgisiId = <hesapId> WHERE Id = <hareketId>;
--       ile yazılır, veya
--   (b) Kayıt gerçek bir işlem değilse (dummy/test verisi) aşağıdaki yorum satırındaki
--       temizlik bloğu açılarak silinir.
-- Tahmine dayalı otomatik dağıtım YAPILMAZ.

-- R1: Hiçbir ödemeyle eşleşmemiş (çözülemeyen) banka hareketleri.
SELECT
    b.Id AS BankaHareketId,
    b.IslemTarihi,
    b.IslemTutari,
    b.EslesmeDurumu,
    b.Aciklama
FROM BankaHareketleri b
WHERE b.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM OdemeBankaEslesmeleri e
      WHERE e.BankaHareketId = b.Id AND e.IsDeleted = 0);

-- R2: Eşleşme üzerinden mağaza hesabı çözülebilecek hareket sayısı (beklenen: R1'in tersi).
SELECT COUNT(*) AS CozulebilirHareketSayisi
FROM BankaHareketleri b
JOIN OdemeBankaEslesmeleri e ON e.BankaHareketId = b.Id AND e.IsDeleted = 0
JOIN TahakkukOdemeleri o     ON o.Id = e.TahakkukOdemesiId AND o.IsDeleted = 0
WHERE b.IsDeleted = 0;

-- R3: Operatörün R1'deki kayıtlara elle atama yapabilmesi için aktif mağaza hesabı envanteri.
SELECT
    h.Id AS MagazaHesapBilgisiId,
    m.Ad AS MagazaAdi,
    h.SaglayiciKodu,
    h.ParaBirimi
FROM MagazaHesapBilgileri h
JOIN Magazalar m ON m.Id = h.MagazaId
WHERE h.Aktif = 1 AND h.IsDeleted = 0 AND m.Aktif = 1 AND m.IsDeleted = 0
ORDER BY m.Ad;

-- Yalnız dummy/test verisi için — bilinçli olarak açılır, otomatik çalışmaz:
-- DELETE FROM BankaHareketleri
-- WHERE Id NOT IN (SELECT BankaHareketId FROM OdemeBankaEslesmeleri);
