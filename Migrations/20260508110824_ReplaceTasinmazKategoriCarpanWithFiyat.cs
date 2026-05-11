using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTasinmazKategoriCarpanWithFiyat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TasinmazKategoriCarpanlari");

            migrationBuilder.CreateTable(
                name: "TasinmazKiraciKategoriFiyatlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazKiraciKategoriFiyatlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasinmazKiraciKategoriFiyatlari_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasinmazKiraciKategoriFiyatlari_KiraciKategorileri_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "KiraciKategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasinmazKiraciKategoriFiyatlari_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazKiraciKategoriFiyatlari_BorcTipiId",
                table: "TasinmazKiraciKategoriFiyatlari",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazKiraciKategoriFiyatlari_KiraciKategoriId",
                table: "TasinmazKiraciKategoriFiyatlari",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazKiraciKategoriFiyatlari_TasinmazId_KiraciKategoriId_BorcTipiId",
                table: "TasinmazKiraciKategoriFiyatlari",
                columns: new[] { "TasinmazId", "KiraciKategoriId", "BorcTipiId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TasinmazKiraciKategoriFiyatlari");

            migrationBuilder.CreateTable(
                name: "TasinmazKategoriCarpanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Carpan = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
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
    }
}
