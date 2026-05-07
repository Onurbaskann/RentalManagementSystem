using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddManuelBorc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar");

            migrationBuilder.AddColumn<string>(
                name: "IptalNotu",
                table: "KiraTahakkuklar",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaynakTipi",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                defaultValue: 1); // 1 = Otomatik

            // Mevcut tüm tahakkukları Otomatik olarak işaretle
            migrationBuilder.Sql("UPDATE KiraTahakkuklar SET KaynakTipi = 1 WHERE KaynakTipi = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar");

            migrationBuilder.DropColumn(
                name: "IptalNotu",
                table: "KiraTahakkuklar");

            migrationBuilder.DropColumn(
                name: "KaynakTipi",
                table: "KiraTahakkuklar");

            migrationBuilder.CreateIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" },
                unique: true);
        }
    }
}
