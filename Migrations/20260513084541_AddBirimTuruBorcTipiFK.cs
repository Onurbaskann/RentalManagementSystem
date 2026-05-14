using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBirimTuruBorcTipiFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BorcTipiId",
                table: "BirimTurleri",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirimTurleri_BorcTipiId",
                table: "BirimTurleri",
                column: "BorcTipiId");

            migrationBuilder.AddForeignKey(
                name: "FK_BirimTurleri_BorcTipleri_BorcTipiId",
                table: "BirimTurleri",
                column: "BorcTipiId",
                principalTable: "BorcTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BirimTurleri_BorcTipleri_BorcTipiId",
                table: "BirimTurleri");

            migrationBuilder.DropIndex(
                name: "IX_BirimTurleri_BorcTipiId",
                table: "BirimTurleri");

            migrationBuilder.DropColumn(
                name: "BorcTipiId",
                table: "BirimTurleri");
        }
    }
}
