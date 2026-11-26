using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class MakeTenantCategoryRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kiracilar_Kategoriler_KiraciKategoriId",
                table: "Kiracilar");

            migrationBuilder.AlterColumn<int>(
                name: "KiraciKategoriId",
                table: "Kiracilar",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiracilar_Kategoriler_KiraciKategoriId",
                table: "Kiracilar",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kiracilar_Kategoriler_KiraciKategoriId",
                table: "Kiracilar");

            migrationBuilder.AlterColumn<int>(
                name: "KiraciKategoriId",
                table: "Kiracilar",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Kiracilar_Kategoriler_KiraciKategoriId",
                table: "Kiracilar",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
