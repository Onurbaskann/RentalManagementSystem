using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddTasinmazTipiParametre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Önce yeni tablo oluştur
            migrationBuilder.CreateTable(
                name: "TasinmazTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTipleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTipleri_Kod",
                table: "TasinmazTipleri",
                column: "Kod",
                unique: true);

            // 2. Seed verisini ekle (SeedDataService guard'ı çakışmayı önler)
            migrationBuilder.Sql(@"
                INSERT INTO TasinmazTipleri (Ad, Kod, Aktif, Sira, OlusturmaTarihi) VALUES
                (N'Bina',       'BINA',       1, 1,  GETUTCDATE()),
                (N'Arazi',      'ARAZI',      1, 2,  GETUTCDATE()),
                (N'Tarla',      'TARLA',      1, 3,  GETUTCDATE()),
                (N'Depo',       'DEPO',       1, 4,  GETUTCDATE()),
                (N'Otomat',     'OTOMAT',     1, 5,  GETUTCDATE()),
                (N'Bankamatik', 'BANKAMATIK', 1, 6,  GETUTCDATE()),
                (N'Kantin',     'KANTIN',     1, 7,  GETUTCDATE()),
                (N'Diğer',      'DIGER',      1, 99, GETUTCDATE())
            ");

            // 3. Yeni FK kolonu ekle (nullable)
            migrationBuilder.AddColumn<int>(
                name: "TasinmazTipiId",
                table: "Tasinmazlar",
                type: "int",
                nullable: true);

            // 4. Mevcut Tipi enum değerlerini yeni FK'ya taşı
            migrationBuilder.Sql(@"
                UPDATE t SET t.TasinmazTipiId = (
                    SELECT tt.Id FROM TasinmazTipleri tt WHERE tt.Kod = CASE t.Tipi
                        WHEN 1 THEN 'BINA'
                        WHEN 2 THEN 'ARAZI'
                        WHEN 3 THEN 'TARLA'
                        WHEN 4 THEN 'DEPO'
                        ELSE 'DIGER'
                    END
                )
                FROM Tasinmazlar t
            ");

            // 5. FK constraint ekle
            migrationBuilder.CreateIndex(
                name: "IX_Tasinmazlar_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasinmazlar_TasinmazTipleri_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId",
                principalTable: "TasinmazTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 6. Eski Tipi kolonu kaldır (veri taşındıktan sonra)
            migrationBuilder.DropColumn(
                name: "Tipi",
                table: "Tasinmazlar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasinmazlar_TasinmazTipleri_TasinmazTipiId",
                table: "Tasinmazlar");

            migrationBuilder.DropTable(
                name: "TasinmazTipleri");

            migrationBuilder.DropIndex(
                name: "IX_Tasinmazlar_TasinmazTipiId",
                table: "Tasinmazlar");

            migrationBuilder.DropColumn(
                name: "TasinmazTipiId",
                table: "Tasinmazlar");

            migrationBuilder.AddColumn<int>(
                name: "Tipi",
                table: "Tasinmazlar",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
