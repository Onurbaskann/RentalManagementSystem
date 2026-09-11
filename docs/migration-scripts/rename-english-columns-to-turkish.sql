-- ============================================================================
-- rename-english-columns-to-turkish.sql
--
-- Amaç: 28 controller'lık çeviri/refactor pass'i sırasında bazı entity
-- attribute'ları yanlışlıkla İngilizce kalmış, ve InitialCreate migration'ı
-- bu kolonları/tabloyu FİZİKSEL OLARAK İngilizce isimlerle yaratmıştı.
-- Bu script, gerçek DB şemasını entity kodundaki (düzeltilmiş) [Column]/[Table]
-- attribute değerleriyle eşitler.
--
-- NOT: Bağımlı FK/CHECK/PK constraint adları, DB'nin nasıl yaratıldığına göre
-- EF'in varsayılan isimlendirme kuralından FARKLI olabilir (bir önceki
-- denemede "FK_SozlesmeIslemGecmisleri_Sozlesmeler_LeaseId bir kısıtlama
-- değil" hatası bunu doğruladı). Bu yüzden bu script constraint adlarını
-- HARDCODE ETMEZ — sys.foreign_keys / sys.check_constraints / sys.key_constraints
-- üzerinden ilgili tablo+kolona bakarak gerçek adı bulur, sonra dinamik SQL ile
-- düşürür. Geri eklerken EF'in üreteceği standart adları kullanır (ileride
-- InitialCreate migration'ı yeniden üretildiğinde bununla eşleşsin diye).
--
-- Bu script EF Core migration mekanizmasını KULLANMAZ — manuel çalıştırılır.
-- Çalıştırdıktan sonra: InitialCreate migration'ı sıfırdan yeniden üretilecek
-- ve __EFMigrationsHistory tablosu elle güncellenecek (ayrı adım, bu script'in
-- kapsamı dışında).
--
-- ÖNEMLİ: Çalıştırmadan önce DB backup alın. Script tek bir transaction
-- içinde çalışır; herhangi bir adım hata verirse tamamı geri alınır.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @constraintName sysname;

    -- ── 1) Bağımlı FK constraint'lerini bul ve düşür ────────────────────────────

    SELECT @constraintName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.SozlesmeIslemGecmisleri') AND c.name = 'LeaseId';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.SozlesmeIslemGecmisleri DROP CONSTRAINT [' + @constraintName + ']');

    SELECT @constraintName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.SozlesmeTarifeler') AND c.name = 'ChargeTypeId';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.SozlesmeTarifeler DROP CONSTRAINT [' + @constraintName + ']');

    SELECT @constraintName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.SozlesmeTarifeler') AND c.name = 'LeaseId';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.SozlesmeTarifeler DROP CONSTRAINT [' + @constraintName + ']');

    SELECT @constraintName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.TasinmazTarifeler') AND c.name = 'ChargeTypeId';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.TasinmazTarifeler DROP CONSTRAINT [' + @constraintName + ']');

    -- ── 2) CHECK constraint'lerini bul ve düşür (tanım metninde eski kolon adı geçen) ──

    SELECT @constraintName = cc.name
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID('dbo.TasinmazTarifeler') AND cc.definition LIKE '%UnitValue%';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.TasinmazTarifeler DROP CONSTRAINT [' + @constraintName + ']');

    SELECT @constraintName = cc.name
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID('dbo.SozlesmeTarifeler') AND cc.definition LIKE '%UnitValue%';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.SozlesmeTarifeler DROP CONSTRAINT [' + @constraintName + ']');

    SELECT @constraintName = cc.name
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID('dbo.Sozlesmeler') AND cc.definition LIKE '%StartDate%';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.Sozlesmeler DROP CONSTRAINT [' + @constraintName + ']');

    -- ── 3) AuditLogs PK'sini bul ve düşür ────────────────────────────────────────

    SELECT @constraintName = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID('dbo.AuditLogs') AND kc.type = 'PK';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.AuditLogs DROP CONSTRAINT [' + @constraintName + ']');

    -- ── 4) Tablo adı ──────────────────────────────────────────────────────────

    EXEC sp_rename 'dbo.AuditLogs', 'HareketGecmisleri';

    -- ── 5) Kolon adları ───────────────────────────────────────────────────────

    EXEC sp_rename 'dbo.TasinmazTarifeler.UnitValue',    'BirimDeger', 'COLUMN';
    EXEC sp_rename 'dbo.TasinmazTarifeler.KdvRate',       'KdvOrani',   'COLUMN';
    EXEC sp_rename 'dbo.TasinmazTarifeler.ChargeTypeId',  'BorcTipiId', 'COLUMN';

    EXEC sp_rename 'dbo.SozlesmeTarifeler.UnitValue',   'BirimDeger', 'COLUMN';
    EXEC sp_rename 'dbo.SozlesmeTarifeler.LeaseId',     'SozlesmeId', 'COLUMN';
    EXEC sp_rename 'dbo.SozlesmeTarifeler.KdvRate',     'KdvOrani',   'COLUMN';
    EXEC sp_rename 'dbo.SozlesmeTarifeler.ChargeTypeId','BorcTipiId', 'COLUMN';

    EXEC sp_rename 'dbo.Sozlesmeler.StartDate', 'BaslangicTarihi', 'COLUMN';
    EXEC sp_rename 'dbo.Sozlesmeler.EndDate',   'BitisTarihi',     'COLUMN';

    EXEC sp_rename 'dbo.SozlesmeIslemGecmisleri.TransactionDate', 'IslemTarihi', 'COLUMN';
    EXEC sp_rename 'dbo.SozlesmeIslemGecmisleri.LeaseId',         'SozlesmeId',  'COLUMN';
    EXEC sp_rename 'dbo.SozlesmeIslemGecmisleri.KdvRate',         'KdvOrani',    'COLUMN';

    EXEC sp_rename 'dbo.HareketGecmisleri.IpAddress',  'IpAdresi',  'COLUMN';
    EXEC sp_rename 'dbo.HareketGecmisleri.EventType',  'OlayTipi',  'COLUMN';
    EXEC sp_rename 'dbo.HareketGecmisleri.EntityType', 'VarlikTipi','COLUMN';
    EXEC sp_rename 'dbo.HareketGecmisleri.EntityId',   'VarlikId',  'COLUMN';
    EXEC sp_rename 'dbo.HareketGecmisleri.Details',    'Detaylar',  'COLUMN';

    -- ── 6) İlgili index adlarını (varsa) EF konvansiyonuna göre güncelle ────────
    -- (Index bulunamazsa hata vermeden atlanır — bazı DB'lerde index olmayabilir.)

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TasinmazTarifeler') AND name = 'IX_TasinmazTarifeler_ChargeTypeId')
        EXEC sp_rename 'dbo.IX_TasinmazTarifeler_ChargeTypeId', 'IX_TasinmazTarifeler_BorcTipiId', 'INDEX';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.SozlesmeTarifeler') AND name = 'IX_SozlesmeTarifeler_ChargeTypeId')
        EXEC sp_rename 'dbo.IX_SozlesmeTarifeler_ChargeTypeId', 'IX_SozlesmeTarifeler_BorcTipiId', 'INDEX';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.SozlesmeIslemGecmisleri') AND name = 'IX_SozlesmeIslemGecmisleri_LeaseId')
        EXEC sp_rename 'dbo.IX_SozlesmeIslemGecmisleri_LeaseId', 'IX_SozlesmeIslemGecmisleri_SozlesmeId', 'INDEX';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.HareketGecmisleri') AND name = 'IX_AuditLogs_UserId_CreatedAt')
        EXEC sp_rename 'dbo.IX_AuditLogs_UserId_CreatedAt', 'IX_HareketGecmisleri_UserId_CreatedAt', 'INDEX';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.HareketGecmisleri') AND name = 'IX_AuditLogs_EventType_CreatedAt')
        EXEC sp_rename 'dbo.IX_AuditLogs_EventType_CreatedAt', 'IX_HareketGecmisleri_OlayTipi_CreatedAt', 'INDEX';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.HareketGecmisleri') AND name = 'IX_AuditLogs_EntityType_EntityId')
        EXEC sp_rename 'dbo.IX_AuditLogs_EntityType_EntityId', 'IX_HareketGecmisleri_VarlikTipi_VarlikId', 'INDEX';

    -- ── 7) Constraint'leri doğru adlarla (EF konvansiyonu) geri ekle ────────────

    ALTER TABLE dbo.HareketGecmisleri ADD CONSTRAINT PK_HareketGecmisleri PRIMARY KEY (Id);

    ALTER TABLE dbo.SozlesmeIslemGecmisleri
        ADD CONSTRAINT FK_SozlesmeIslemGecmisleri_Sozlesmeler_SozlesmeId
        FOREIGN KEY (SozlesmeId) REFERENCES dbo.Sozlesmeler (Id) ON DELETE CASCADE;

    ALTER TABLE dbo.SozlesmeTarifeler
        ADD CONSTRAINT FK_SozlesmeTarifeler_BorcTipleri_BorcTipiId
        FOREIGN KEY (BorcTipiId) REFERENCES dbo.BorcTipleri (Id) ON DELETE NO ACTION;

    ALTER TABLE dbo.SozlesmeTarifeler
        ADD CONSTRAINT FK_SozlesmeTarifeler_Sozlesmeler_SozlesmeId
        FOREIGN KEY (SozlesmeId) REFERENCES dbo.Sozlesmeler (Id) ON DELETE CASCADE;

    ALTER TABLE dbo.TasinmazTarifeler
        ADD CONSTRAINT FK_TasinmazTarifeler_BorcTipleri_BorcTipiId
        FOREIGN KEY (BorcTipiId) REFERENCES dbo.BorcTipleri (Id) ON DELETE NO ACTION;

    ALTER TABLE dbo.TasinmazTarifeler
        ADD CONSTRAINT CK_TasinmazTarifeler_Degerler
        CHECK ([BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100);

    ALTER TABLE dbo.SozlesmeTarifeler
        ADD CONSTRAINT CK_SozlesmeTarifeler_Degerler
        CHECK ([BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100);

    ALTER TABLE dbo.Sozlesmeler
        ADD CONSTRAINT CK_Sozlesmeler_TarihSirasi
        CHECK ([BitisTarihi] > [BaslangicTarihi]);

    COMMIT TRANSACTION;
    PRINT 'Başarılı: tüm rename ve constraint işlemleri uygulandı.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'HATA — hiçbir değişiklik uygulanmadı (rollback yapıldı):';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
