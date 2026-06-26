using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class BankaHareketiKolonYeniden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bakiye",
                table: "BankaHareketleri");

            migrationBuilder.RenameColumn(
                name: "Tutar",
                table: "BankaHareketleri",
                newName: "IslemTutari");

            migrationBuilder.RenameColumn(
                name: "HareketTarihi",
                table: "BankaHareketleri",
                newName: "IslemTarihi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IslemTutari",
                table: "BankaHareketleri",
                newName: "Tutar");

            migrationBuilder.RenameColumn(
                name: "IslemTarihi",
                table: "BankaHareketleri",
                newName: "HareketTarihi");

            migrationBuilder.AddColumn<decimal>(
                name: "Bakiye",
                table: "BankaHareketleri",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
