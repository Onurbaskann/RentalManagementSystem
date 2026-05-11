using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTekSeferlikWithDavranis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Davranis",
                table: "BorcTipleri",
                type: "int",
                nullable: false,
                defaultValue: 1); // 1 = AylikSabit

            // Data Migration
            migrationBuilder.Sql("UPDATE BorcTipleri SET Davranis = 2 WHERE TekSeferlikMi = 1"); // 2 = IlkAyTekSeferlik
            migrationBuilder.Sql("UPDATE BorcTipleri SET Davranis = 3 WHERE Kod IN ('MANUEL', 'TOPLANTI')"); // 3 = ManuelTetiklemeli

            migrationBuilder.DropColumn(
                name: "TekSeferlikMi",
                table: "BorcTipleri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TekSeferlikMi",
                table: "BorcTipleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE BorcTipleri SET TekSeferlikMi = 1 WHERE Davranis = 2");

            migrationBuilder.DropColumn(
                name: "Davranis",
                table: "BorcTipleri");
        }
    }
}
