using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddParametreTablolari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BirimTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimTurleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KiraciKategorileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiraciKategorileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sektorler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sektorler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BirimTurleri_Kod",
                table: "BirimTurleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KiraciKategorileri_Kod",
                table: "KiraciKategorileri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sektorler_Kod",
                table: "Sektorler",
                column: "Kod",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BirimTurleri");

            migrationBuilder.DropTable(
                name: "KiraciKategorileri");

            migrationBuilder.DropTable(
                name: "Sektorler");
        }
    }
}
