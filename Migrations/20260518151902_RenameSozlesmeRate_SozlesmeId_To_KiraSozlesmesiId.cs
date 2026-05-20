using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RenameSozlesmeRate_SozlesmeId_To_KiraSozlesmesiId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SozlesmeRateler_Sozlesmeler_SozlesmeId",
                table: "SozlesmeRateler");

            migrationBuilder.RenameColumn(
                name: "SozlesmeId",
                table: "SozlesmeRateler",
                newName: "KiraSozlesmesiId");

            migrationBuilder.RenameIndex(
                name: "IX_SozlesmeRateler_SozlesmeId_BorcTipiId",
                table: "SozlesmeRateler",
                newName: "IX_SozlesmeRateler_KiraSozlesmesiId_BorcTipiId");

            migrationBuilder.AddColumn<DateTime>(
                name: "SonHatirlatmaTarihi",
                table: "KiraTahakkuklar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SozlesmeRateler_Sozlesmeler_KiraSozlesmesiId",
                table: "SozlesmeRateler",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SozlesmeRateler_Sozlesmeler_KiraSozlesmesiId",
                table: "SozlesmeRateler");

            migrationBuilder.DropColumn(
                name: "SonHatirlatmaTarihi",
                table: "KiraTahakkuklar");

            migrationBuilder.RenameColumn(
                name: "KiraSozlesmesiId",
                table: "SozlesmeRateler",
                newName: "SozlesmeId");

            migrationBuilder.RenameIndex(
                name: "IX_SozlesmeRateler_KiraSozlesmesiId_BorcTipiId",
                table: "SozlesmeRateler",
                newName: "IX_SozlesmeRateler_SozlesmeId_BorcTipiId");

            migrationBuilder.AddForeignKey(
                name: "FK_SozlesmeRateler_Sozlesmeler_SozlesmeId",
                table: "SozlesmeRateler",
                column: "SozlesmeId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
