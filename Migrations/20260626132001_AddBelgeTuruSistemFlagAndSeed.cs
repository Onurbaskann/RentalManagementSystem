using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBelgeTuruSistemFlagAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sistem",
                table: "BelgeTurleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "BelgeTurleri",
                columns: new[] { "Id", "Aciklama", "Ad", "CreatedAt", "CreatedBy", "HedefEntite", "IsActive", "IsDeleted", "IzinVerilenUzantilar", "Kod", "MaxBoyutMb", "SablonBelgeId", "Sira", "Sistem", "UpdatedAt", "UpdatedBy", "Zorunlu" },
                values: new object[] { 1, null, "Ödeme Dekontu", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 2, true, false, "pdf,jpg,jpeg,png", "ODEME_DEKONT", 5, null, 1, true, null, null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BelgeTurleri",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Sistem",
                table: "BelgeTurleri");
        }
    }
}
