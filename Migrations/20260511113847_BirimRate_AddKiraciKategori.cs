using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class BirimRate_AddKiraciKategori : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut veriler dummy — temizle
            migrationBuilder.Sql("DELETE FROM BirimRateler");

            // Eski unique index kaldır (yoksa atla)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BirimRateler_BirimId_BorcTipiId' AND object_id = OBJECT_ID('BirimRateler'))
                    DROP INDEX [IX_BirimRateler_BirimId_BorcTipiId] ON [BirimRateler];");

            // KiraciKategoriId kolonu ekle (yoksa)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BirimRateler') AND name = 'KiraciKategoriId')
                    ALTER TABLE [BirimRateler] ADD [KiraciKategoriId] int NOT NULL DEFAULT 0;");

            // FK ekle (yoksa)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BirimRateler_KiraciKategorileri_KiraciKategoriId')
                    ALTER TABLE [BirimRateler] ADD CONSTRAINT [FK_BirimRateler_KiraciKategorileri_KiraciKategoriId]
                        FOREIGN KEY ([KiraciKategoriId]) REFERENCES [KiraciKategorileri] ([Id]) ON DELETE NO ACTION;");

            // Yeni unique index (yoksa)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BirimRateler_BirimId_KiraciKategoriId_BorcTipiId' AND object_id = OBJECT_ID('BirimRateler'))
                    CREATE UNIQUE INDEX [IX_BirimRateler_BirimId_KiraciKategoriId_BorcTipiId] ON [BirimRateler] ([BirimId], [KiraciKategoriId], [BorcTipiId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BirimRateler_KiraciKategorileri_KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.DropIndex(
                name: "IX_BirimRateler_BirimId_KiraciKategoriId_BorcTipiId",
                table: "BirimRateler");

            migrationBuilder.DropColumn(
                name: "KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.CreateIndex(
                name: "IX_BirimRateler_BirimId_BorcTipiId",
                table: "BirimRateler",
                columns: new[] { "BirimId", "BorcTipiId" },
                unique: true);
        }
    }
}
