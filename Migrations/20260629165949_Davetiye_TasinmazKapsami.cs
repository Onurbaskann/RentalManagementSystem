using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class Davetiye_TasinmazKapsami : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TasinmazIds",
                table: "Davetiyeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TumTasinmazlaraErisim",
                table: "Davetiyeler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TasinmazIds",
                table: "Davetiyeler");

            migrationBuilder.DropColumn(
                name: "TumTasinmazlaraErisim",
                table: "Davetiyeler");
        }
    }
}
