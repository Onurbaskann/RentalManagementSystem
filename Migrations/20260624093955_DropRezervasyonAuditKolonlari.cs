using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class DropRezervasyonAuditKolonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OdemeBankaEslesmeleri_AspNetUsers_EslestirenUserId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropIndex(
                name: "IX_OdemeBankaEslesmeleri_EslestirenUserId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropColumn(
                name: "OlusturanUserId",
                table: "Rezervasyonlari");

            migrationBuilder.DropColumn(
                name: "OlusturmaTarihi",
                table: "Rezervasyonlari");

            migrationBuilder.DropColumn(
                name: "EslesenTutar",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropColumn(
                name: "EslesmeTarihi",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropColumn(
                name: "EslestirenUserId",
                table: "OdemeBankaEslesmeleri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OlusturanUserId",
                table: "Rezervasyonlari",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OlusturmaTarihi",
                table: "Rezervasyonlari",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "EslesenTutar",
                table: "OdemeBankaEslesmeleri",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EslesmeTarihi",
                table: "OdemeBankaEslesmeleri",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EslestirenUserId",
                table: "OdemeBankaEslesmeleri",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_EslestirenUserId",
                table: "OdemeBankaEslesmeleri",
                column: "EslestirenUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OdemeBankaEslesmeleri_AspNetUsers_EslestirenUserId",
                table: "OdemeBankaEslesmeleri",
                column: "EslestirenUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
