using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedAndCapacityUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnlikBirimTuruAdi",
                table: "Sozlesmeler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnlikBirimYuzolcumu",
                table: "Sozlesmeler",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AnlikKiralamaBicimi",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "UnitBased=1, CapacityBased=2");

            migrationBuilder.AddColumn<int>(
                name: "AnlikKullanimTuru",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Rentable=1, Reservable=2, NonRentable=3");

            migrationBuilder.AddColumn<int>(
                name: "SozlesmeGrubuId",
                table: "Sozlesmeler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KiralamaBicimi",
                table: "BirimTurleri",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "UnitBased=1, CapacityBased=2");

            migrationBuilder.AddColumn<int>(
                name: "Kontenjan",
                table: "Birimler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BirimKontenjanGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    Kontenjan = table.Column<int>(type: "int", nullable: false),
                    GecerlilikBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimKontenjanGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BirimKontenjanGecmisleri_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeAlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SozlesmeId = table.Column<int>(type: "int", nullable: false),
                    KiralananAlan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FizikselAlan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeAlanlari", x => x.Id);
                    table.CheckConstraint("CK_SozlesmeAlanlari_Alanlar", "[KiralananAlan] > 0 AND [FizikselAlan] > 0 AND [FizikselAlan] <= [KiralananAlan]");
                    table.ForeignKey(
                        name: "FK_SozlesmeAlanlari_Sozlesmeler_SozlesmeId",
                        column: x => x.SozlesmeId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeGruplari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    OncekiGrupId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeGruplari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozlesmeGruplari_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SozlesmeGruplari_SozlesmeGruplari_OncekiGrupId",
                        column: x => x.OncekiGrupId,
                        principalTable: "SozlesmeGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_SozlesmeGrubuId",
                table: "Sozlesmeler",
                column: "SozlesmeGrubuId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Birimler_Kontenjan",
                table: "Birimler",
                sql: "[Kontenjan] IS NULL OR [Kontenjan] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BirimKontenjanGecmisleri_BirimId_GecerlilikBaslangici",
                table: "BirimKontenjanGecmisleri",
                columns: new[] { "BirimId", "GecerlilikBaslangici" });

            migrationBuilder.CreateIndex(
                name: "UX_SozlesmeAlanlari_SozlesmeId_Silinmemis",
                table: "SozlesmeAlanlari",
                column: "SozlesmeId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeGruplari_BirimId",
                table: "SozlesmeGruplari",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeGruplari_OncekiGrupId",
                table: "SozlesmeGruplari",
                column: "OncekiGrupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sozlesmeler_SozlesmeGruplari_SozlesmeGrubuId",
                table: "Sozlesmeler",
                column: "SozlesmeGrubuId",
                principalTable: "SozlesmeGruplari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE s SET AnlikBirimTuruAdi = ISNULL(bt.Ad, ''),
                  AnlikKullanimTuru = ISNULL(bt.KullanimTuru, 1),
                  AnlikKiralamaBicimi = 1,
                  AnlikBirimYuzolcumu = b.Yuzolcumu
                FROM Sozlesmeler s
                JOIN Birimler b ON b.Id = s.BirimId
                LEFT JOIN BirimTurleri bt ON bt.Id = b.BirimTuruId
                """);

            migrationBuilder.InsertData(
                table: "SistemAyarlari",
                columns: new[]
                {
                    "Id", "CreatedAt", "CreatedBy", "Aktif", "IsDeleted",
                    "Anahtar", "UpdatedAt", "UpdatedBy", "Deger"
                },
                values: new object[]
                {
                    17, new DateTime(2026, 11, 17, 0, 0, 0, DateTimeKind.Utc),
                    "System", true, false, "Lease.MinimumUsageArea", null, null, "5"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SistemAyarlari",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DropForeignKey(
                name: "FK_Sozlesmeler_SozlesmeGruplari_SozlesmeGrubuId",
                table: "Sozlesmeler");

            migrationBuilder.DropTable(
                name: "BirimKontenjanGecmisleri");

            migrationBuilder.DropTable(
                name: "SozlesmeAlanlari");

            migrationBuilder.DropTable(
                name: "SozlesmeGruplari");

            migrationBuilder.DropIndex(
                name: "IX_Sozlesmeler_SozlesmeGrubuId",
                table: "Sozlesmeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Birimler_Kontenjan",
                table: "Birimler");

            migrationBuilder.DropColumn(
                name: "AnlikBirimTuruAdi",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikBirimYuzolcumu",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikKiralamaBicimi",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikKullanimTuru",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "SozlesmeGrubuId",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "KiralamaBicimi",
                table: "BirimTurleri");

            migrationBuilder.DropColumn(
                name: "Kontenjan",
                table: "Birimler");
        }
    }
}
