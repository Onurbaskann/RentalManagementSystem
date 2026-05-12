using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBorcTipiSistemFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sistem",
                table: "BorcTipleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Seed edilen sistem kodlarını işaretle
            migrationBuilder.Sql("UPDATE BorcTipleri SET Sistem = 1 WHERE Kod IN ('KIRA','ORTAK','PORTAL','DEPOZITO','MANUEL','TOPLANTI')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sistem",
                table: "BorcTipleri");
        }
    }
}
