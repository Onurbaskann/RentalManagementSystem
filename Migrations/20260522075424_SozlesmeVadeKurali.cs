using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class SozlesmeVadeKurali : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VadeGunu",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VadeKuraliTipi",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VadeGunu",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "VadeKuraliTipi",
                table: "Sozlesmeler");
        }
    }
}
