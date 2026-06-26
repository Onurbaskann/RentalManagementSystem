using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class DropKullaniciYetkiKapsamiAuditKolonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtanmaTarihi",
                table: "KullaniciYetkiKapsamlari");

            migrationBuilder.DropColumn(
                name: "AtayanUserId",
                table: "KullaniciYetkiKapsamlari");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AtanmaTarihi",
                table: "KullaniciYetkiKapsamlari",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AtayanUserId",
                table: "KullaniciYetkiKapsamlari",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }
    }
}
