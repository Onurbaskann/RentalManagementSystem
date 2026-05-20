using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class B1_KiraOdemeKaynakTipi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OdemeKaynakTipi",
                table: "KiraOdemeler",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "Manuel=1, BankaEslesme=2, SanalPos=3");

            migrationBuilder.AddColumn<string>(
                name: "PosReferansNo",
                table: "KiraOdemeler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OdemeKaynakTipi",
                table: "KiraOdemeler");

            migrationBuilder.DropColumn(
                name: "PosReferansNo",
                table: "KiraOdemeler");
        }
    }
}
