using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class FixTahakkuklarDateOrderCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tahakkuklar_TarihSirasi",
                table: "Tahakkuklar");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tahakkuklar_TarihSirasi",
                table: "Tahakkuklar",
                sql: "[DonemBitisi] >= [DonemBaslangici]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tahakkuklar_TarihSirasi",
                table: "Tahakkuklar");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tahakkuklar_TarihSirasi",
                table: "Tahakkuklar",
                sql: "[DonemBitisi] > [DonemBaslangici]");
        }
    }
}
