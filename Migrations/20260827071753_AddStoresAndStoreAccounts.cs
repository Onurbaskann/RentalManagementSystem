using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddStoresAndStoreAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Magazalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Magazalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MagazaHesapBilgileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MagazaId = table.Column<int>(type: "int", nullable: false),
                    SaglayiciKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParaBirimi = table.Column<string>(type: "char(3)", nullable: false),
                    MerchantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MerchantUser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SifreliMerchantPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GecerlilikBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GecerlilikBitisi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagazaHesapBilgileri", x => x.Id);
                    table.CheckConstraint("CK_MagazaHesapBilgileri_Gecerlilik", "[GecerlilikBitisi] IS NULL OR [GecerlilikBitisi] >= [GecerlilikBaslangici]");
                    table.ForeignKey(
                        name: "FK_MagazaHesapBilgileri_Magazalar_MagazaId",
                        column: x => x.MagazaId,
                        principalTable: "Magazalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MagazaHesapBilgileri_MagazaId_GecerlilikBaslangici",
                table: "MagazaHesapBilgileri",
                columns: new[] { "MagazaId", "GecerlilikBaslangici" });

            migrationBuilder.CreateIndex(
                name: "UX_MagazaHesapBilgileri_Magaza_Aktif",
                table: "MagazaHesapBilgileri",
                column: "MagazaId",
                unique: true,
                filter: "[Aktif] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Magazalar_Kod_Silinmemis",
                table: "Magazalar",
                column: "Kod",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagazaHesapBilgileri");

            migrationBuilder.DropTable(
                name: "Magazalar");
        }
    }
}
