using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class Phase17A_YetkiKapsami : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TumTasinmazlaraErisim",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KullaniciYetkiKapsamlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KapsamTipi = table.Column<int>(type: "int", nullable: false),
                    KapsamId = table.Column<int>(type: "int", nullable: false),
                    AtayanUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AtanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciYetkiKapsamlari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciYetkiKapsamlari_UserId_KapsamTipi_KapsamId",
                table: "KullaniciYetkiKapsamlari",
                columns: new[] { "UserId", "KapsamTipi", "KapsamId" },
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO KullaniciYetkiKapsamlari
                    (UserId, KapsamTipi, KapsamId, AtayanUserId, AtanmaTarihi,
                     CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, IsActive)
                SELECT
                    UserId, 1, TasinmazId, AtayanUserId, AtanmaTarihi,
                    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, IsActive
                FROM UserTasinmazYetkileri
                WHERE IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciYetkiKapsamlari");

            migrationBuilder.DropColumn(
                name: "TumTasinmazlaraErisim",
                table: "AspNetUsers");
        }
    }
}
