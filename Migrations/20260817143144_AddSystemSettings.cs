using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SistemAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anahtar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Deger = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemAyarlari", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SistemAyarlari",
                columns: new[]
                {
                    "Id", "CreatedAt", "CreatedBy", "Aktif", "IsDeleted",
                    "Anahtar", "UpdatedAt", "UpdatedBy", "Deger"
                },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.MinimumDurationMinutes", null, null, "15" },
                    { 2, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.MaximumDurationMinutes", null, null, "1440" },
                    { 3, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.MinimumAdvanceMinutes", null, null, "0" },
                    { 4, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.MaximumAdvanceDays", null, null, "365" },
                    { 5, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.ModificationCutoffMinutes", null, null, "120" },
                    { 6, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.CompletionGraceMinutes", null, null, "15" },
                    { 7, new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc), "System", true, false, "Reservation.MaximumAttendeeCount", null, null, "100" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_SistemAyarlari_Anahtar_Aktif",
                table: "SistemAyarlari",
                column: "Anahtar",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SistemAyarlari");
        }
    }
}
