using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class DropUnusedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "TasinmazTarifeler");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "TasinmazTarifeler");

            migrationBuilder.DropColumn(
                name: "OlusturmaTarihi",
                table: "TasinmazTarifeler");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "GenelTarifeler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "TasinmazTarifeler",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "TasinmazTarifeler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OlusturmaTarihi",
                table: "TasinmazTarifeler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "GenelTarifeler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
