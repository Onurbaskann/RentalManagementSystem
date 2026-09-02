using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlinePaymentTransactionBackbone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SanalPosIslemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TahakkukKalemiId = table.Column<int>(type: "int", nullable: false),
                    MagazaHesapBilgisiId = table.Column<int>(type: "int", nullable: false),
                    BaslatanKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OdemeId = table.Column<int>(type: "int", nullable: true),
                    SaglayiciKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UyeIsyeriOdemeNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SaglayiciIslemNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ParaBirimi = table.Column<string>(type: "char(3)", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Pending=1, Approved=2, Failed=3, Cancelled=4, Unknown=5"),
                    YanitKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IslemDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HataKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GuvenliMesaj = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OturumSonTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeriBildirimAlinmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonSorgulamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SorgulamaSayisi = table.Column<int>(type: "int", nullable: false),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanalPosIslemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanalPosIslemleri_AspNetUsers_BaslatanKullaniciId",
                        column: x => x.BaslatanKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SanalPosIslemleri_MagazaHesapBilgileri_MagazaHesapBilgisiId",
                        column: x => x.MagazaHesapBilgisiId,
                        principalTable: "MagazaHesapBilgileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SanalPosIslemleri_TahakkukKalemleri_TahakkukKalemiId",
                        column: x => x.TahakkukKalemiId,
                        principalTable: "TahakkukKalemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SanalPosIslemleri_TahakkukOdemeleri_OdemeId",
                        column: x => x.OdemeId,
                        principalTable: "TahakkukOdemeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SanalPosIslemOlaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SanalPosIslemiId = table.Column<int>(type: "int", nullable: false),
                    OlayTipi = table.Column<int>(type: "int", nullable: false, comment: "SessionRequested=1, SessionResult=2, CallbackReceived=3, InquiryPerformed=4, Succeeded=5, Failed=6"),
                    SaglayiciYanitKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SaglayiciIslemDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GuvenliOzet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SaglayiciZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanalPosIslemOlaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanalPosIslemOlaylari_SanalPosIslemleri_SanalPosIslemiId",
                        column: x => x.SanalPosIslemiId,
                        principalTable: "SanalPosIslemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SanalPosIslemleri_BaslatanKullaniciId",
                table: "SanalPosIslemleri",
                column: "BaslatanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SanalPosIslemleri_MagazaHesapBilgisiId",
                table: "SanalPosIslemleri",
                column: "MagazaHesapBilgisiId");

            migrationBuilder.CreateIndex(
                name: "IX_SanalPosIslemleri_OdemeId",
                table: "SanalPosIslemleri",
                column: "OdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_SanalPosIslemleri_TahakkukKalemiId_Durum",
                table: "SanalPosIslemleri",
                columns: new[] { "TahakkukKalemiId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "UX_SanalPosIslemleri_UyeIsyeriOdemeNo_Silinmemis",
                table: "SanalPosIslemleri",
                column: "UyeIsyeriOdemeNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SanalPosIslemOlaylari_SanalPosIslemiId",
                table: "SanalPosIslemOlaylari",
                column: "SanalPosIslemiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SanalPosIslemOlaylari");

            migrationBuilder.DropTable(
                name: "SanalPosIslemleri");
        }
    }
}
