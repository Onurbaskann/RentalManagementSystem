using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBelgeTablosu_DropKvkkOnayi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KvkkOnayi",
                table: "Kiraciler");

            migrationBuilder.CreateTable(
                name: "BelgeIcerikleri",
                columns: table => new
                {
                    BelgeId = table.Column<int>(type: "int", nullable: false),
                    Icerik = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BelgeIcerikleri", x => x.BelgeId);
                });

            migrationBuilder.CreateTable(
                name: "Belgeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BelgeTuruId = table.Column<int>(type: "int", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false, comment: "Kiraci=1, Sablon=99"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    DosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BoyutByte = table.Column<long>(type: "bigint", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Gecersiz = table.Column<bool>(type: "bit", nullable: false),
                    GecersizlikTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DegistirenBelgeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Belgeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Belgeler_Belgeler_DegistirenBelgeId",
                        column: x => x.DegistirenBelgeId,
                        principalTable: "Belgeler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BelgeTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HedefEntite = table.Column<int>(type: "int", nullable: false, comment: "Kiraci=1, Sablon=99"),
                    Zorunlu = table.Column<bool>(type: "bit", nullable: false),
                    IzinVerilenUzantilar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxBoyutMb = table.Column<int>(type: "int", nullable: false),
                    SablonBelgeId = table.Column<int>(type: "int", nullable: true),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BelgeTurleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BelgeTurleri_Belgeler_SablonBelgeId",
                        column: x => x.SablonBelgeId,
                        principalTable: "Belgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_BelgeTuruId",
                table: "Belgeler",
                column: "BelgeTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_DegistirenBelgeId",
                table: "Belgeler",
                column: "DegistirenBelgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_OwnerType_OwnerId_Gecersiz_IsDeleted",
                table: "Belgeler",
                columns: new[] { "OwnerType", "OwnerId", "Gecersiz", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_BelgeTurleri_Kod",
                table: "BelgeTurleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BelgeTurleri_SablonBelgeId",
                table: "BelgeTurleri",
                column: "SablonBelgeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BelgeIcerikleri_Belgeler_BelgeId",
                table: "BelgeIcerikleri",
                column: "BelgeId",
                principalTable: "Belgeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Belgeler_BelgeTurleri_BelgeTuruId",
                table: "Belgeler",
                column: "BelgeTuruId",
                principalTable: "BelgeTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BelgeTurleri_Belgeler_SablonBelgeId",
                table: "BelgeTurleri");

            migrationBuilder.DropTable(
                name: "BelgeIcerikleri");

            migrationBuilder.DropTable(
                name: "Belgeler");

            migrationBuilder.DropTable(
                name: "BelgeTurleri");

            migrationBuilder.AddColumn<bool>(
                name: "KvkkOnayi",
                table: "Kiraciler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
