-- ============================================================================
-- rename-indexes-to-turkish.sql
--
-- rename-english-columns-to-turkish.sql'in kapsamadığı 6 index'in adını,
-- artık Türkçe olan kolon adlarıyla tutarlı hale getirir. Fonksiyonel bir
-- etkisi yoktur (eski adıyla kalsa da index doğru çalışır) — yalnızca
-- kozmetik/isimlendirme tutarlılığı içindir.
--
-- Bu script, kolon rename'lerinin (rename-english-columns-to-turkish.sql)
-- ZATEN UYGULANMIŞ olduğunu varsayar — index'i, üzerinde bulunduğu kolonun
-- YENİ (Türkçe) adına göre bulur. Index adını hardcode etmez; ilgili
-- tablo+kolona bakıp gerçek mevcut adı sorgular, sonra dinamik SQL ile
-- yeniden adlandırır. Zaten doğru adla ise dokunmadan atlar.
--
-- ÖNEMLİ: Çalıştırmadan önce DB backup alın. Tek transaction, hata olursa
-- tamamı geri alınır.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @tableName sysname, @columnName sysname, @targetIndexName sysname, @currentIndexName sysname, @qualifiedIndexName nvarchar(517);

    -- Her satır: (tablo, aranacak öncü kolon, hedef index adı)
    DECLARE @work TABLE (TableName sysname, ColumnName sysname, TargetIndexName sysname);
    INSERT INTO @work (TableName, ColumnName, TargetIndexName) VALUES
        ('TasinmazTarifeler',       'BorcTipiId', 'IX_TasinmazTarifeler_BorcTipiId'),
        ('SozlesmeTarifeler',       'BorcTipiId', 'IX_SozlesmeTarifeler_BorcTipiId'),
        ('SozlesmeIslemGecmisleri', 'SozlesmeId', 'IX_SozlesmeIslemGecmisleri_SozlesmeId'),
        ('HareketGecmisleri',       'UserId',     'IX_HareketGecmisleri_UserId_CreatedAt'),
        ('HareketGecmisleri',       'OlayTipi',   'IX_HareketGecmisleri_OlayTipi_CreatedAt'),
        ('HareketGecmisleri',       'VarlikTipi', 'IX_HareketGecmisleri_VarlikTipi_VarlikId');

    DECLARE work_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ColumnName, TargetIndexName FROM @work;

    OPEN work_cursor;
    FETCH NEXT FROM work_cursor INTO @tableName, @columnName, @targetIndexName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @currentIndexName = NULL;

        SELECT @currentIndexName = i.name
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('dbo.' + @tableName)
          AND c.name = @columnName
          AND i.is_primary_key = 0
          AND i.type > 0; -- heap (0) hariç

        IF @currentIndexName IS NOT NULL AND @currentIndexName <> @targetIndexName
        BEGIN
            SET @qualifiedIndexName = N'dbo.' + @tableName + N'.' + @currentIndexName;
            EXEC sp_rename
                @objname = @qualifiedIndexName,
                @newname = @targetIndexName,
                @objtype = N'INDEX';
        END

        FETCH NEXT FROM work_cursor INTO @tableName, @columnName, @targetIndexName;
    END;

    CLOSE work_cursor;
    DEALLOCATE work_cursor;

    COMMIT TRANSACTION;
    PRINT 'Başarılı: index adları güncellendi (veya zaten doğruydu).';

END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'work_cursor') >= -1
    BEGIN
        CLOSE work_cursor;
        DEALLOCATE work_cursor;
    END
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'HATA — hiçbir değişiklik uygulanmadı (rollback yapıldı):';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
