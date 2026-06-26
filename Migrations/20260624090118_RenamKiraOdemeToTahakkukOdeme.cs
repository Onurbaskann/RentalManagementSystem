using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RenamKiraOdemeToTahakkukOdeme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys referencing KiraOdemeler
            migrationBuilder.DropForeignKey(
                name: "FK_OdemeBankaEslesmeleri_KiraOdemeler_KiraOdemeId",
                table: "OdemeBankaEslesmeleri");

            // 2. Drop foreign keys originating from KiraOdemeler
            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_AspNetUsers_GirenUserId",
                table: "KiraOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_AspNetUsers_OnaylayanUserId",
                table: "KiraOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_Sozlesmeler_KiraSozlesmesiId",
                table: "KiraOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_KiraOdemeler_Tahakkuklar_TahakkukId",
                table: "KiraOdemeler");

            // 3. Rename table KiraOdemeler to TahakkukOdemeler
            migrationBuilder.RenameTable(
                name: "KiraOdemeler",
                newName: "TahakkukOdemeler");

            // 4. Rename column KiraOdemeId to TahakkukOdemeId in OdemeBankaEslesmeleri
            migrationBuilder.RenameColumn(
                name: "KiraOdemeId",
                table: "OdemeBankaEslesmeleri",
                newName: "TahakkukOdemeId");

            // 5. Rename index IX_OdemeBankaEslesmeleri_KiraOdemeId
            migrationBuilder.RenameIndex(
                name: "IX_OdemeBankaEslesmeleri_KiraOdemeId",
                table: "OdemeBankaEslesmeleri",
                newName: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId");

            // 6. Rename indexes on TahakkukOdemeler (which were on KiraOdemeler)
            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_GirenUserId",
                table: "TahakkukOdemeler",
                newName: "IX_TahakkukOdemeler_GirenUserId");

            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_KiraSozlesmesiId",
                table: "TahakkukOdemeler",
                newName: "IX_TahakkukOdemeler_KiraSozlesmesiId");

            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_OnaylayanUserId",
                table: "TahakkukOdemeler",
                newName: "IX_TahakkukOdemeler_OnaylayanUserId");

            migrationBuilder.RenameIndex(
                name: "IX_KiraOdemeler_TahakkukId",
                table: "TahakkukOdemeler",
                newName: "IX_TahakkukOdemeler_TahakkukId");

            // 7. Add foreign keys originating from TahakkukOdemeler
            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeler_AspNetUsers_GirenUserId",
                table: "TahakkukOdemeler",
                column: "GirenUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeler_AspNetUsers_OnaylayanUserId",
                table: "TahakkukOdemeler",
                column: "OnaylayanUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeler_Sozlesmeler_KiraSozlesmesiId",
                table: "TahakkukOdemeler",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TahakkukOdemeler_Tahakkuklar_TahakkukId",
                table: "TahakkukOdemeler",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 8. Add foreign key on OdemeBankaEslesmeleri referencing TahakkukOdemeler
            migrationBuilder.AddForeignKey(
                name: "FK_OdemeBankaEslesmeleri_TahakkukOdemeler_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "TahakkukOdemeId",
                principalTable: "TahakkukOdemeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys referencing TahakkukOdemeler
            migrationBuilder.DropForeignKey(
                name: "FK_OdemeBankaEslesmeleri_TahakkukOdemeler_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri");

            // 2. Drop foreign keys originating from TahakkukOdemeler
            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeler_AspNetUsers_GirenUserId",
                table: "TahakkukOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeler_AspNetUsers_OnaylayanUserId",
                table: "TahakkukOdemeler");

            // 3. Rename table TahakkukOdemeler back to KiraOdemeler
            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeler_Sozlesmeler_KiraSozlesmesiId",
                table: "TahakkukOdemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_TahakkukOdemeler_Tahakkuklar_TahakkukId",
                table: "TahakkukOdemeler");

            // Rename table TahakkukOdemeler back to KiraOdemeler
            migrationBuilder.RenameTable(
                name: "TahakkukOdemeler",
                newName: "KiraOdemeler");

            // 4. Rename column TahakkukOdemeId back to KiraOdemeId in OdemeBankaEslesmeleri
            migrationBuilder.RenameColumn(
                name: "TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri",
                newName: "KiraOdemeId");

            // 5. Rename index IX_OdemeBankaEslesmeleri_TahakkukOdemeId back
            migrationBuilder.RenameIndex(
                name: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri",
                newName: "IX_OdemeBankaEslesmeleri_KiraOdemeId");

            // 6. Rename indexes back to original names
            migrationBuilder.RenameIndex(
                name: "IX_TahakkukOdemeler_GirenUserId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_GirenUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TahakkukOdemeler_KiraSozlesmesiId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_KiraSozlesmesiId");

            migrationBuilder.RenameIndex(
                name: "IX_TahakkukOdemeler_OnaylayanUserId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_OnaylayanUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TahakkukOdemeler_TahakkukId",
                table: "KiraOdemeler",
                newName: "IX_KiraOdemeler_TahakkukId");

            // 7. Add foreign keys originating from KiraOdemeler
            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_AspNetUsers_GirenUserId",
                table: "KiraOdemeler",
                column: "GirenUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_AspNetUsers_OnaylayanUserId",
                table: "KiraOdemeler",
                column: "OnaylayanUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_Sozlesmeler_KiraSozlesmesiId",
                table: "KiraOdemeler",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KiraOdemeler_Tahakkuklar_TahakkukId",
                table: "KiraOdemeler",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 8. Add foreign key referencing KiraOdemeler
            migrationBuilder.AddForeignKey(
                name: "FK_OdemeBankaEslesmeleri_KiraOdemeler_KiraOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "KiraOdemeId",
                principalTable: "KiraOdemeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

