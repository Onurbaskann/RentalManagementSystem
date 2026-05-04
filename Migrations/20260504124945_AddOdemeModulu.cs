using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddOdemeModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankaHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HareketTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KarsiHesap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KarsiUnvan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BankaKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EslesmeDurumu = table.Column<int>(type: "int", nullable: false),
                    ImportTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportEdenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaHareketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankaHareketleri_AspNetUsers_ImportEdenUserId",
                        column: x => x.ImportEdenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiraTahakkuklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: false),
                    DonemBaslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonemBitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeklenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiraTahakkuklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiraTahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiraOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraTahakkukId = table.Column<int>(type: "int", nullable: false),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeKanali = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    GirenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnaylayanUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedNedeni = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiraOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_AspNetUsers_GirenUserId",
                        column: x => x.GirenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_AspNetUsers_OnaylayanUserId",
                        column: x => x.OnaylayanUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_KiraTahakkuklar_KiraTahakkukId",
                        column: x => x.KiraTahakkukId,
                        principalTable: "KiraTahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Dekontlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraOdemeId = table.Column<int>(type: "int", nullable: false),
                    OrijinalDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiskDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DosyaTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    YukleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    YuklemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dekontlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dekontlar_AspNetUsers_YukleyenUserId",
                        column: x => x.YukleyenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dekontlar_KiraOdemeler_KiraOdemeId",
                        column: x => x.KiraOdemeId,
                        principalTable: "KiraOdemeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdemeBankaEslesmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraOdemeId = table.Column<int>(type: "int", nullable: false),
                    BankaHareketiId = table.Column<int>(type: "int", nullable: false),
                    EslesmeTipi = table.Column<int>(type: "int", nullable: false),
                    EslestirenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EslesmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeBankaEslesmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_AspNetUsers_EslestirenUserId",
                        column: x => x.EslestirenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_BankaHareketleri_BankaHareketiId",
                        column: x => x.BankaHareketiId,
                        principalTable: "BankaHareketleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_KiraOdemeler_KiraOdemeId",
                        column: x => x.KiraOdemeId,
                        principalTable: "KiraOdemeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportBatchId",
                table: "BankaHareketleri",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportEdenUserId",
                table: "BankaHareketleri",
                column: "ImportEdenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dekontlar_KiraOdemeId",
                table: "Dekontlar",
                column: "KiraOdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dekontlar_YukleyenUserId",
                table: "Dekontlar",
                column: "YukleyenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_GirenUserId",
                table: "KiraOdemeler",
                column: "GirenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_KiraSozlesmesiId",
                table: "KiraOdemeler",
                column: "KiraSozlesmesiId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_KiraTahakkukId",
                table: "KiraOdemeler",
                column: "KiraTahakkukId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_OnaylayanUserId",
                table: "KiraOdemeler",
                column: "OnaylayanUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri",
                column: "BankaHareketiId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_EslestirenUserId",
                table: "OdemeBankaEslesmeleri",
                column: "EslestirenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_KiraOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "KiraOdemeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dekontlar");

            migrationBuilder.DropTable(
                name: "OdemeBankaEslesmeleri");

            migrationBuilder.DropTable(
                name: "BankaHareketleri");

            migrationBuilder.DropTable(
                name: "KiraOdemeler");

            migrationBuilder.DropTable(
                name: "KiraTahakkuklar");
        }
    }
}
