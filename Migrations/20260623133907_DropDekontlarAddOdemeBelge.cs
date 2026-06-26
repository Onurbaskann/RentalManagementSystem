using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class DropDekontlarAddOdemeBelge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dekontlar");

            migrationBuilder.AlterColumn<int>(
                name: "HedefEntite",
                table: "BelgeTurleri",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Sablon=99");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerType",
                table: "Belgeler",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Sablon=99");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "HedefEntite",
                table: "BelgeTurleri",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sablon=99");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerType",
                table: "Belgeler",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sablon=99");

            migrationBuilder.CreateTable(
                name: "Dekontlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraOdemeId = table.Column<int>(type: "int", nullable: false),
                    YukleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiskDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    DosyaTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OrijinalDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_Dekontlar_KiraOdemeId",
                table: "Dekontlar",
                column: "KiraOdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dekontlar_YukleyenUserId",
                table: "Dekontlar",
                column: "YukleyenUserId");
        }
    }
}
