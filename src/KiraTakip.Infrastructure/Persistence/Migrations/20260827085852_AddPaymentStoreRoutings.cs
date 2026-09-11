using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStoreRoutings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OdemeMagazaYonlendirmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    TasinmazId = table.Column<int>(type: "int", nullable: true),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    MagazaId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeMagazaYonlendirmeleri", x => x.Id);
                    table.CheckConstraint("CK_OdemeMagazaYonlendirmeleri_Kapsam", "([TasinmazId] IS NULL AND [BirimId] IS NULL) OR ([TasinmazId] IS NOT NULL AND [BirimId] IS NULL) OR ([TasinmazId] IS NULL AND [BirimId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OdemeMagazaYonlendirmeleri_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeMagazaYonlendirmeleri_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeMagazaYonlendirmeleri_Magazalar_MagazaId",
                        column: x => x.MagazaId,
                        principalTable: "Magazalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeMagazaYonlendirmeleri_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_BirimId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_MagazaId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "MagazaId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_TasinmazId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "TasinmazId");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Birim_Aktif",
                table: "OdemeMagazaYonlendirmeleri",
                columns: new[] { "BorcTipiId", "BirimId" },
                unique: true,
                filter: "[TasinmazId] IS NULL AND [BirimId] IS NOT NULL AND [Aktif] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Genel_Aktif",
                table: "OdemeMagazaYonlendirmeleri",
                column: "BorcTipiId",
                unique: true,
                filter: "[TasinmazId] IS NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Tasinmaz_Aktif",
                table: "OdemeMagazaYonlendirmeleri",
                columns: new[] { "BorcTipiId", "TasinmazId" },
                unique: true,
                filter: "[TasinmazId] IS NOT NULL AND [BirimId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OdemeMagazaYonlendirmeleri");
        }
    }
}
