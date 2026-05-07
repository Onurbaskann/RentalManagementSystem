using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddRezervasyonModeli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RezervasyonUcretKurallari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretlendirmePeriyoduDakika = table.Column<int>(type: "int", nullable: false),
                    PeriyotUcreti = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RezervasyonUcretKurallari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RezervasyonUcretKurallari_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ToplantiSalonuRezervasyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToplamSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretliSureDakika = table.Column<int>(type: "int", nullable: false),
                    BirimUcret = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UcretTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    KiraTahakkukId = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturanUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToplantiSalonuRezervasyonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToplantiSalonuRezervasyonlari_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToplantiSalonuRezervasyonlari_KiraTahakkuklar_KiraTahakkukId",
                        column: x => x.KiraTahakkukId,
                        principalTable: "KiraTahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ToplantiSalonuRezervasyonlari_Kiraciler_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiraciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToplantiSalonuRezervasyonlari_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonUcretKurallari_BirimId",
                table: "RezervasyonUcretKurallari",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_ToplantiSalonuRezervasyonlari_BirimId_BaslangicTarihi",
                table: "ToplantiSalonuRezervasyonlari",
                columns: new[] { "BirimId", "BaslangicTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_ToplantiSalonuRezervasyonlari_KiraciId",
                table: "ToplantiSalonuRezervasyonlari",
                column: "KiraciId");

            migrationBuilder.CreateIndex(
                name: "IX_ToplantiSalonuRezervasyonlari_KiraSozlesmesiId",
                table: "ToplantiSalonuRezervasyonlari",
                column: "KiraSozlesmesiId");

            migrationBuilder.CreateIndex(
                name: "IX_ToplantiSalonuRezervasyonlari_KiraTahakkukId",
                table: "ToplantiSalonuRezervasyonlari",
                column: "KiraTahakkukId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RezervasyonUcretKurallari");

            migrationBuilder.DropTable(
                name: "ToplantiSalonuRezervasyonlari");
        }
    }
}
