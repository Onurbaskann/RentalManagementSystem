using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddTasinmazKategoriCarpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TasinmazKategoriCarpanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    Carpan = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazKategoriCarpanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasinmazKategoriCarpanlari_KiraciKategorileri_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "KiraciKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasinmazKategoriCarpanlari_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazKategoriCarpanlari_KiraciKategoriId",
                table: "TasinmazKategoriCarpanlari",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazKategoriCarpanlari_TasinmazId_KiraciKategoriId",
                table: "TasinmazKategoriCarpanlari",
                columns: new[] { "TasinmazId", "KiraciKategoriId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TasinmazKategoriCarpanlari");
        }
    }
}
