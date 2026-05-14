using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddRezervasyonGenelTarife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RezervasyonGenelTarifeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TarifeId = table.Column<int>(type: "int", nullable: false),
                    BirimTuruId = table.Column<int>(type: "int", nullable: false),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretlendirmePeriyoduDakika = table.Column<int>(type: "int", nullable: false),
                    PeriyotUcreti = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RezervasyonGenelTarifeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RezervasyonGenelTarifeleri_BirimTurleri_BirimTuruId",
                        column: x => x.BirimTuruId,
                        principalTable: "BirimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RezervasyonGenelTarifeleri_Tarifeler_TarifeId",
                        column: x => x.TarifeId,
                        principalTable: "Tarifeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonGenelTarifeleri_BirimTuruId",
                table: "RezervasyonGenelTarifeleri",
                column: "BirimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonGenelTarifeleri_TarifeId_BirimTuruId",
                table: "RezervasyonGenelTarifeleri",
                columns: new[] { "TarifeId", "BirimTuruId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RezervasyonGenelTarifeleri");
        }
    }
}
