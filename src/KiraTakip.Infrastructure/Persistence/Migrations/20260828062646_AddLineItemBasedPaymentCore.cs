using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddLineItemBasedPaymentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Yeni kolonlar — FK kolonları geçici olarak nullable, backfill sonrası NOT NULL'a alınır.
            migrationBuilder.AddColumn<decimal>(
                name: "OdenenTutar",
                table: "TahakkukKalemleri",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TahakkukKalemiId",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: true);

            // 2) Backfill B1 — tek kalemli tahakkukların ödemelerini o tek kaleme bağla.
            //    Çok kalemli tahakkukların ödemeleri için tahmine dayalı dağıtım YAPILMAZ;
            //    bunlar preflight script (docs/migration-scripts/phase-20-inner-phase-3-preflight.sql)
            //    ile raporlanır ve elle çözülür.
            migrationBuilder.Sql(@"
UPDATE o SET o.TahakkukKalemiId = k.Id
FROM TahakkukOdemeleri o
JOIN TahakkukKalemleri k ON k.TahakkukId = o.TahakkukId AND k.IsDeleted = 0
WHERE o.TahakkukKalemiId IS NULL
  AND (SELECT COUNT(*) FROM TahakkukKalemleri k2
       WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0) = 1;");

            // 3) Backfill B2 — mağaza hesabı, resolver önceliğinin (Birim -> Taşınmaz -> Genel)
            //    SQL karşılığıyla çözülür (bkz. PaymentStoreRoutingRepository.GetResolutionCandidateAsync).
            migrationBuilder.Sql(@"
UPDATE o SET o.MagazaHesapBilgisiId = r.HesapId
FROM TahakkukOdemeleri o
CROSS APPLY (
    SELECT TOP 1 h.Id AS HesapId
    FROM TahakkukKalemleri k
    JOIN Tahakkuklar t ON t.Id = k.TahakkukId
    JOIN Birimler b ON b.Id = t.BirimId
    JOIN OdemeMagazaYonlendirmeleri y
      ON y.BorcTipiId = k.TahakkukTipiId AND y.Aktif = 1 AND y.IsDeleted = 0
     AND (y.BirimId = b.Id
          OR (y.BirimId IS NULL AND y.TasinmazId = b.TasinmazId)
          OR (y.BirimId IS NULL AND y.TasinmazId IS NULL))
    JOIN Magazalar m ON m.Id = y.MagazaId AND m.Aktif = 1 AND m.IsDeleted = 0
    JOIN MagazaHesapBilgileri h ON h.MagazaId = m.Id AND h.Aktif = 1 AND h.IsDeleted = 0
    WHERE k.Id = o.TahakkukKalemiId
    ORDER BY CASE WHEN y.BirimId = b.Id THEN 0
                  WHEN y.TasinmazId = b.TasinmazId THEN 1 ELSE 2 END
) r
WHERE o.MagazaHesapBilgisiId IS NULL AND o.TahakkukKalemiId IS NOT NULL;");

            // 4) Backfill B3 — kalem OdenenTutar, o kaleme ait onaylı (Durum = 2) ödemelerin toplamı.
            migrationBuilder.Sql(@"
UPDATE k SET k.OdenenTutar = ISNULL(x.Toplam, 0)
FROM TahakkukKalemleri k
OUTER APPLY (SELECT SUM(o.Tutar) AS Toplam FROM TahakkukOdemeleri o
             WHERE o.TahakkukKalemiId = k.Id AND o.Durum = 2 AND o.IsDeleted = 0) x;");

            // 5) Guard — backfill eksik kalırsa migration burada durur, sessizce ilerlemez.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM TahakkukOdemeleri
           WHERE TahakkukKalemiId IS NULL OR MagazaHesapBilgisiId IS NULL)
    THROW 51000, N'Faz20/IcFaz3: Kalem veya magaza hesabi atanamayan odeme kayitlari var. Once docs/migration-scripts/phase-20-inner-phase-3-preflight.sql calistirilip kayitlar elle cozulmelidir.', 1;
IF EXISTS (SELECT 1 FROM TahakkukKalemleri WHERE OdenenTutar > ToplamTutar)
    THROW 51000, N'Faz20/IcFaz3: OdenenTutar > ToplamTutar olan tahakkuk kalemleri var.', 1;");

            // 6) Backfill tamamlandı — kolonları NOT NULL'a al.
            migrationBuilder.AlterColumn<int>(
                name: "MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TahakkukKalemiId",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri",
                column: "MagazaHesapBilgisiId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_TahakkukKalemiId_Durum",
                table: "TahakkukOdemeleri",
                columns: new[] { "TahakkukKalemiId", "Durum" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TahakkukKalemleri_OdenenLimit",
                table: "TahakkukKalemleri",
                sql: "[OdenenTutar] >= 0 AND [OdenenTutar] <= [ToplamTutar]");

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeleri_MagazaHesapBilgileri_MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri",
                column: "MagazaHesapBilgisiId",
                principalTable: "MagazaHesapBilgileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeleri_TahakkukKalemleri_TahakkukKalemiId",
                table: "TahakkukOdemeleri",
                column: "TahakkukKalemiId",
                principalTable: "TahakkukKalemleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeleri_MagazaHesapBilgileri_MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeleri_TahakkukKalemleri_TahakkukKalemiId",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropIndex(
                name: "IX_TahakkukOdemeleri_MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropIndex(
                name: "IX_TahakkukOdemeleri_TahakkukKalemiId_Durum",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TahakkukKalemleri_OdenenLimit",
                table: "TahakkukKalemleri");

            migrationBuilder.DropColumn(
                name: "MagazaHesapBilgisiId",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropColumn(
                name: "TahakkukKalemiId",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropColumn(
                name: "OdenenTutar",
                table: "TahakkukKalemleri");
        }
    }
}
