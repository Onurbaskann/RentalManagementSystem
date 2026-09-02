using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBankTransactionStoreAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Kolon geçici olarak nullable — backfill sonrası NOT NULL'a alınır.
            migrationBuilder.AddColumn<int>(
                name: "MagazaHesapBilgisiId",
                table: "BankaHareketleri",
                type: "int",
                nullable: true);

            // 2) Backfill — eşleşmiş ödemenin mağaza hesabından çözülür. Tahmine dayalı
            //    dağıtım YAPILMAZ; eşleşmesi olmayan hareketler için preflight script
            //    (docs/migration-scripts/phase-20-inner-phase-4-preflight.sql) elle çözüm
            //    ister.
            migrationBuilder.Sql(@"
UPDATE b SET b.MagazaHesapBilgisiId = o.MagazaHesapBilgisiId
FROM BankaHareketleri b
JOIN OdemeBankaEslesmeleri e ON e.BankaHareketId = b.Id AND e.IsDeleted = 0
JOIN TahakkukOdemeleri o     ON o.Id = e.TahakkukOdemesiId AND o.IsDeleted = 0
WHERE b.MagazaHesapBilgisiId IS NULL;");

            // 3) Guard — backfill eksik kalırsa migration burada durur, sessizce ilerlemez.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM BankaHareketleri WHERE MagazaHesapBilgisiId IS NULL)
    THROW 51000, N'Faz20/IcFaz4: Magaza hesabi atanamayan banka hareketi kayitlari var. Once docs/migration-scripts/phase-20-inner-phase-4-preflight.sql calistirilip kayitlar elle cozulmeli veya silinmelidir.', 1;");

            // 4) Backfill tamamlandı — kolonu NOT NULL'a al.
            migrationBuilder.AlterColumn<int>(
                name: "MagazaHesapBilgisiId",
                table: "BankaHareketleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_MagazaHesapBilgisiId",
                table: "BankaHareketleri",
                column: "MagazaHesapBilgisiId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHareketleri_MagazaHesapBilgileri_MagazaHesapBilgisiId",
                table: "BankaHareketleri",
                column: "MagazaHesapBilgisiId",
                principalTable: "MagazaHesapBilgileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankaHareketleri_MagazaHesapBilgileri_MagazaHesapBilgisiId",
                table: "BankaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaHareketleri_MagazaHesapBilgisiId",
                table: "BankaHareketleri");

            migrationBuilder.DropColumn(
                name: "MagazaHesapBilgisiId",
                table: "BankaHareketleri");
        }
    }
}
