using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseAreaModeAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HesaplamaModu",
                table: "SozlesmeAlanlari",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "SozlesmeAlanGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SozlesmeAlanId = table.Column<int>(type: "int", nullable: false),
                    FizikselAlan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HesaplamaModu = table.Column<int>(type: "int", nullable: false),
                    GecerlilikBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DegisiklikNedeni = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeAlanGecmisleri", x => x.Id);
                    table.CheckConstraint("CK_SozlesmeAlanGecmisleri_FizikselAlan", "[FizikselAlan] > 0");
                    table.ForeignKey(
                        name: "FK_SozlesmeAlanGecmisleri_SozlesmeAlanlari_SozlesmeAlanId",
                        column: x => x.SozlesmeAlanId,
                        principalTable: "SozlesmeAlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeAlanGecmisleri_SozlesmeAlanId_GecerlilikBaslangici",
                table: "SozlesmeAlanGecmisleri",
                columns: new[] { "SozlesmeAlanId", "GecerlilikBaslangici" });

            migrationBuilder.Sql(@"
INSERT INTO SozlesmeAlanGecmisleri
    (SozlesmeAlanId, FizikselAlan, HesaplamaModu, GecerlilikBaslangici, DegisiklikNedeni, CreatedAt, CreatedBy, IsDeleted, Aktif)
SELECT a.Id, a.FizikselAlan, 2, CAST(s.BaslangicTarihi AS date), 4, GETUTCDATE(), 'migration', 0, 1
FROM SozlesmeAlanlari a
INNER JOIN Sozlesmeler s ON s.Id = a.SozlesmeId
WHERE s.Durum = 1 AND s.AnlikKiralamaBicimi = 2 AND s.IsDeleted = 0 AND a.IsDeleted = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SozlesmeAlanGecmisleri");

            migrationBuilder.DropColumn(
                name: "HesaplamaModu",
                table: "SozlesmeAlanlari");
        }
    }
}
