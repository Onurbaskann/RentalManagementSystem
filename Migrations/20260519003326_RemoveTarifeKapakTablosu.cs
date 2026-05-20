using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTarifeKapakTablosu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) FK kısıtlamalarını kaldır — veri update için gerekli
            migrationBuilder.DropForeignKey(
                name: "FK_RezervasyonGenelTarifeleri_Tarifeler_TarifeId",
                table: "RezervasyonGenelTarifeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_TarifeKalemleri_Tarifeler_TarifeId",
                table: "TarifeKalemleri");

            // 2) TarifeId (FK değeri) → Tarifeler.Yil değeriyle güncelle
            migrationBuilder.Sql(@"
                UPDATE tk SET tk.TarifeId = t.Yil
                FROM TarifeKalemleri tk
                INNER JOIN Tarifeler t ON t.Id = tk.TarifeId;

                UPDATE rgt SET rgt.TarifeId = t.Yil
                FROM RezervasyonGenelTarifeleri rgt
                INNER JOIN Tarifeler t ON t.Id = rgt.TarifeId;
            ");

            migrationBuilder.DropTable(
                name: "Tarifeler");

            migrationBuilder.RenameColumn(
                name: "TarifeId",
                table: "TarifeKalemleri",
                newName: "Yil");

            migrationBuilder.RenameIndex(
                name: "IX_TarifeKalemleri_TarifeId_KiraciKategoriId_BorcTipiId",
                table: "TarifeKalemleri",
                newName: "IX_TarifeKalemleri_Yil_KiraciKategoriId_BorcTipiId");

            migrationBuilder.RenameColumn(
                name: "TarifeId",
                table: "RezervasyonGenelTarifeleri",
                newName: "Yil");

            migrationBuilder.RenameIndex(
                name: "IX_RezervasyonGenelTarifeleri_TarifeId_BirimTuruId",
                table: "RezervasyonGenelTarifeleri",
                newName: "IX_RezervasyonGenelTarifeleri_Yil_BirimTuruId");

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "TarifeKalemleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "RezervasyonGenelTarifeleri",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "TarifeKalemleri");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "RezervasyonGenelTarifeleri");

            migrationBuilder.RenameColumn(
                name: "Yil",
                table: "TarifeKalemleri",
                newName: "TarifeId");

            migrationBuilder.RenameIndex(
                name: "IX_TarifeKalemleri_Yil_KiraciKategoriId_BorcTipiId",
                table: "TarifeKalemleri",
                newName: "IX_TarifeKalemleri_TarifeId_KiraciKategoriId_BorcTipiId");

            migrationBuilder.RenameColumn(
                name: "Yil",
                table: "RezervasyonGenelTarifeleri",
                newName: "TarifeId");

            migrationBuilder.RenameIndex(
                name: "IX_RezervasyonGenelTarifeleri_Yil_BirimTuruId",
                table: "RezervasyonGenelTarifeleri",
                newName: "IX_RezervasyonGenelTarifeleri_TarifeId_BirimTuruId");

            migrationBuilder.CreateTable(
                name: "Tarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarifeler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tarifeler_Yil",
                table: "Tarifeler",
                column: "Yil",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RezervasyonGenelTarifeleri_Tarifeler_TarifeId",
                table: "RezervasyonGenelTarifeleri",
                column: "TarifeId",
                principalTable: "Tarifeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TarifeKalemleri_Tarifeler_TarifeId",
                table: "TarifeKalemleri",
                column: "TarifeId",
                principalTable: "Tarifeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
