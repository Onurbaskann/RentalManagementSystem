using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RenameKiraTahakkukToTahakkuk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys on tables that reference KiraTahakkuklar
            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_KiraTahakkuklar_KiraTahakkukId",
                table: "KiraOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlari_KiraTahakkuklar_KiraTahakkukId",
                table: "Rezervasyonlari");

            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukKalemleri_KiraTahakkuklar_TahakkukId",
                table: "TahakkukKalemleri");

            // 2. Drop foreign keys originating from KiraTahakkuklar
            migrationBuilder.DropForeignKey(
                name: "FK_KiraTahakkuklar_Kiraciler_KiraciId",
                table: "KiraTahakkuklar");

            migrationBuilder.DropForeignKey(
                name: "FK_KiraTahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                table: "KiraTahakkuklar");

            // 3. Rename Table KiraTahakkuklar to Tahakkuklar
            migrationBuilder.RenameTable(
                name: "KiraTahakkuklar",
                newName: "Tahakkuklar");

            // 4. Rename columns
            migrationBuilder.RenameColumn(
                name: "KiraTahakkukId",
                table: "KiraOdemeler",
                newName: "TahakkukId");

            migrationBuilder.RenameColumn(
                name: "KiraTahakkukId",
                table: "Rezervasyonlari",
                newName: "TahakkukId");

            // 5. Rename indexes on foreign keys
            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_KiraTahakkukId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_TahakkukId");

            migrationBuilder.RenameIndex(
                name: "IX_Rezervasyonlari_KiraTahakkukId",
                table: "Rezervasyonlari",
                newName: "IX_Rezervasyonlari_TahakkukId");

            migrationBuilder.RenameIndex(
                name: "IX_KiraTahakkuklar_KiraciId",
                table: "Tahakkuklar",
                newName: "IX_Tahakkuklar_KiraciId");

            migrationBuilder.RenameIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "Tahakkuklar",
                newName: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic");

            // 6. Add foreign keys originating from Tahakkuklar (with new name)
            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Kiraciler_KiraciId",
                table: "Tahakkuklar",
                column: "KiraciId",
                principalTable: "Kiraciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                table: "Tahakkuklar",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 7. Add foreign keys referencing Tahakkuklar
            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_Tahakkuklar_TahakkukId",
                table: "KiraOdemeler",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlari_Tahakkuklar_TahakkukId",
                table: "Rezervasyonlari",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukKalemleri_Tahakkuklar_TahakkukId",
                table: "TahakkukKalemleri",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys on tables referencing Tahakkuklar
            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_Tahakkuklar_TahakkukId",
                table: "KiraOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlari_Tahakkuklar_TahakkukId",
                table: "Rezervasyonlari");

            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukKalemleri_Tahakkuklar_TahakkukId",
                table: "TahakkukKalemleri");

            // 2. Drop foreign keys originating from Tahakkuklar
            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Kiraciler_KiraciId",
                table: "Tahakkuklar");

            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                table: "Tahakkuklar");

            // 3. Rename Table Tahakkuklar back to KiraTahakkuklar
            migrationBuilder.RenameTable(
                name: "Tahakkuklar",
                newName: "KiraTahakkuklar");

            // 4. Rename columns back
            migrationBuilder.RenameColumn(
                name: "TahakkukId",
                table: "KiraOdemeler",
                newName: "KiraTahakkukId");

            migrationBuilder.RenameColumn(
                name: "TahakkukId",
                table: "Rezervasyonlari",
                newName: "KiraTahakkukId");

            // 5. Rename indexes back
            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_TahakkukId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_KiraTahakkukId");

            migrationBuilder.RenameIndex(
                name: "IX_Rezervasyonlari_TahakkukId",
                table: "Rezervasyonlari",
                newName: "IX_Rezervasyonlari_KiraTahakkukId");

            migrationBuilder.RenameIndex(
                name: "IX_Tahakkuklar_KiraciId",
                table: "KiraTahakkuklar",
                newName: "IX_KiraTahakkuklar_KiraciId");

            migrationBuilder.RenameIndex(
                name: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar",
                newName: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic");

            // 6. Recreate foreign keys originating from KiraTahakkuklar
            migrationBuilder.AddForeignKey(
                name: "FK_KiraTahakkuklar_Kiraciler_KiraciId",
                table: "KiraTahakkuklar",
                column: "KiraciId",
                principalTable: "Kiraciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KiraTahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                table: "KiraTahakkuklar",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 7. Recreate foreign keys referencing KiraTahakkuklar
            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_KiraTahakkuklar_KiraTahakkukId",
                table: "KiraOdemeler",
                column: "KiraTahakkukId",
                principalTable: "KiraTahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlari_KiraTahakkuklar_KiraTahakkukId",
                table: "Rezervasyonlari",
                column: "KiraTahakkukId",
                principalTable: "KiraTahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukKalemleri_KiraTahakkuklar_TahakkukId",
                table: "TahakkukKalemleri",
                column: "TahakkukId",
                principalTable: "KiraTahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

