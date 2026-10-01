using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1) Dört kolon da önce NULL'a izin verecek şekilde eklenir ──────────────
            migrationBuilder.AddColumn<string>(
                name: "OdemeNo",
                table: "TahakkukOdemeleri",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TahakkukNo",
                table: "Tahakkuklar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SozlesmeNo",
                table: "Sozlesmeler",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RezervasyonNo",
                table: "Rezervasyonlar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // ── 2) Mevcut kayıtlar Id sırasına göre <ÖNEK>-000001, ... ile doldurulur ──
            migrationBuilder.Sql(@"
;WITH ordered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Sozlesmeler
)
UPDATE s
SET s.SozlesmeNo = 'SZL-' + RIGHT('000000' + CAST(ordered.rn AS varchar(6)), 6)
FROM Sozlesmeler s
JOIN ordered ON ordered.Id = s.Id;
");

            migrationBuilder.Sql(@"
;WITH ordered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Tahakkuklar
)
UPDATE t
SET t.TahakkukNo = 'THK-' + RIGHT('000000' + CAST(ordered.rn AS varchar(6)), 6)
FROM Tahakkuklar t
JOIN ordered ON ordered.Id = t.Id;
");

            migrationBuilder.Sql(@"
;WITH ordered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM TahakkukOdemeleri
)
UPDATE o
SET o.OdemeNo = 'ODM-' + RIGHT('000000' + CAST(ordered.rn AS varchar(6)), 6)
FROM TahakkukOdemeleri o
JOIN ordered ON ordered.Id = o.Id;
");

            migrationBuilder.Sql(@"
;WITH ordered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Rezervasyonlar
)
UPDATE r
SET r.RezervasyonNo = 'RZV-' + RIGHT('000000' + CAST(ordered.rn AS varchar(6)), 6)
FROM Rezervasyonlar r
JOIN ordered ON ordered.Id = r.Id;
");

            // ── 3) Backfill tamamlandığına göre dört kolon da NOT NULL'a çevrilir ──────
            migrationBuilder.AlterColumn<string>(
                name: "OdemeNo",
                table: "TahakkukOdemeleri",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TahakkukNo",
                table: "Tahakkuklar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SozlesmeNo",
                table: "Sozlesmeler",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RezervasyonNo",
                table: "Rezervasyonlar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_TahakkukOdemeleri_OdemeNo_Silinmemis",
                table: "TahakkukOdemeleri",
                column: "OdemeNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tahakkuklar_TahakkukNo_Silinmemis",
                table: "Tahakkuklar",
                column: "TahakkukNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Sozlesmeler_SozlesmeNo_Silinmemis",
                table: "Sozlesmeler",
                column: "SozlesmeNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Rezervasyonlar_RezervasyonNo_Silinmemis",
                table: "Rezervasyonlar",
                column: "RezervasyonNo",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TahakkukOdemeleri_OdemeNo_Silinmemis",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropIndex(
                name: "UX_Tahakkuklar_TahakkukNo_Silinmemis",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "UX_Sozlesmeler_SozlesmeNo_Silinmemis",
                table: "Sozlesmeler");

            migrationBuilder.DropIndex(
                name: "UX_Rezervasyonlar_RezervasyonNo_Silinmemis",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "OdemeNo",
                table: "TahakkukOdemeleri");

            migrationBuilder.DropColumn(
                name: "TahakkukNo",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "SozlesmeNo",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "RezervasyonNo",
                table: "Rezervasyonlar");
        }
    }
}
