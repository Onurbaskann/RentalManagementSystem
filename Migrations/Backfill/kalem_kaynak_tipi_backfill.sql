-- Backfill: KalemKaynakTipi düzeltmesi
-- Manuel tahakkuklara ait kalemler (şu anda yanlış olarak SozlesmeTarifesi=1 yazılı)
-- → ManuelGiris=5 olarak güncellenir.
-- Rezervasyon tahakkuklarına ait kalemler
-- → RezervasyonKurali=6 olarak güncellenir.
--
-- KiraTahakkuklar.KaynakTipi: Sozlesme=1, Manuel=2, Rezervasyon=3
-- TahakkukKalemleri.KaynakTipi: TanimsizTarife=0, SozlesmeTarifesi=1, ..., ManuelGiris=5, RezervasyonKurali=6
--
-- Etkilenen tablolar: TahakkukKalemleri (sadece UPDATE, şema değişikliği yok)
-- Çalıştırmadan önce: BACKUP alın veya tx içinde çalıştırın.

BEGIN TRANSACTION;

-- Manuel tahakkuklara ait kalemler → ManuelGiris (5)
UPDATE TahakkukKalemleri
SET KaynakTipi = 5
WHERE TahakkukId IN (
    SELECT Id FROM KiraTahakkuklar WHERE KaynakTipi = 2  -- Manuel=2
)
AND KaynakTipi = 1;  -- Sadece yanlış SozlesmeTarifesi=1 olanları düzelt

-- Rezervasyon tahakkuklarına ait kalemler → RezervasyonKurali (6)
UPDATE TahakkukKalemleri
SET KaynakTipi = 6
WHERE TahakkukId IN (
    SELECT Id FROM KiraTahakkuklar WHERE KaynakTipi = 3  -- Rezervasyon=3
)
AND KaynakTipi = 1;  -- Sadece yanlış SozlesmeTarifesi=1 olanları düzelt

-- Doğrulama
SELECT
    t.KaynakTipi AS TahakkukKaynakTipi,
    k.KaynakTipi AS KalemKaynakTipi,
    COUNT(*) AS Adet
FROM TahakkukKalemleri k
JOIN KiraTahakkuklar t ON k.TahakkukId = t.Id
GROUP BY t.KaynakTipi, k.KaynakTipi
ORDER BY t.KaynakTipi, k.KaynakTipi;

COMMIT;
