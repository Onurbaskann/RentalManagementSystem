using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class BankaHareketiRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankaHareketleri_AspNetUsers_ImportEdenUserId",
                table: "BankaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaHareketleri_ImportBatchId",
                table: "BankaHareketleri");

            migrationBuilder.DropIndex(
                name: "IX_BankaHareketleri_ImportEdenUserId",
                table: "BankaHareketleri");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "BankaHareketleri");

            migrationBuilder.DropColumn(
                name: "ImportEdenUserId",
                table: "BankaHareketleri");

            migrationBuilder.DropColumn(
                name: "ImportTarihi",
                table: "BankaHareketleri");

            migrationBuilder.RenameColumn(
                name: "KarsiUnvan",
                table: "BankaHareketleri",
                newName: "GonderenBilgisi");

            migrationBuilder.RenameColumn(
                name: "KarsiHesap",
                table: "BankaHareketleri",
                newName: "GonderenIban");

            migrationBuilder.AddColumn<string>(
                name: "BankaReferansNo",
                table: "BankaHareketleri",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_BankaReferansNo",
                table: "BankaHareketleri",
                column: "BankaReferansNo",
                unique: true,
                filter: "[BankaReferansNo] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankaHareketleri_BankaReferansNo",
                table: "BankaHareketleri");

            migrationBuilder.DropColumn(
                name: "BankaReferansNo",
                table: "BankaHareketleri");

            migrationBuilder.RenameColumn(
                name: "GonderenIban",
                table: "BankaHareketleri",
                newName: "KarsiHesap");

            migrationBuilder.RenameColumn(
                name: "GonderenBilgisi",
                table: "BankaHareketleri",
                newName: "KarsiUnvan");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportBatchId",
                table: "BankaHareketleri",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ImportEdenUserId",
                table: "BankaHareketleri",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportTarihi",
                table: "BankaHareketleri",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportBatchId",
                table: "BankaHareketleri",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportEdenUserId",
                table: "BankaHareketleri",
                column: "ImportEdenUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankaHareketleri_AspNetUsers_ImportEdenUserId",
                table: "BankaHareketleri",
                column: "ImportEdenUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
