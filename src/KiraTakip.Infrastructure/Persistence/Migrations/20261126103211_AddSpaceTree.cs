using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlanOzellikTanimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VeriTipi = table.Column<int>(type: "int", nullable: false, comment: "Text=1, Number=2, Boolean=3, List=4"),
                    Secenekler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanOzellikTanimlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlanTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    VarsayilanKullanim = table.Column<int>(type: "int", nullable: false, comment: "Rentable=1, Reservable=2, NotRentable=3"),
                    VarsayilanOnTahsis = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanTurleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UstAlanId = table.Column<int>(type: "int", nullable: true),
                    AlanTuruId = table.Column<int>(type: "int", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Yol = table.Column<string>(type: "varchar(450)", unicode: false, maxLength: 450, nullable: false, collation: "Latin1_General_BIN2"),
                    Derinlik = table.Column<int>(type: "int", nullable: false),
                    Yuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Kullanim = table.Column<int>(type: "int", nullable: false, comment: "Rentable=1, Reservable=2, NotRentable=3"),
                    OnTahsiseAcik = table.Column<bool>(type: "bit", nullable: false),
                    Paylasimli = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alanlar", x => x.Id);
                    table.CheckConstraint("CK_Alanlar_Bayraklar", "(([OnTahsiseAcik] = 0 AND [Paylasimli] = 0) OR [Kullanim] = 1) AND NOT ([OnTahsiseAcik] = 1 AND [Paylasimli] = 1)");
                    table.CheckConstraint("CK_Alanlar_Derinlik", "[Derinlik] BETWEEN 0 AND 19");
                    table.CheckConstraint("CK_Alanlar_UstAlan", "[UstAlanId] IS NULL OR [UstAlanId] <> [Id]");
                    table.CheckConstraint("CK_Alanlar_Yuzolcumu", "[Yuzolcumu] > 0");
                    table.ForeignKey(
                        name: "FK_Alanlar_AlanTurleri_AlanTuruId",
                        column: x => x.AlanTuruId,
                        principalTable: "AlanTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alanlar_Alanlar_UstAlanId",
                        column: x => x.UstAlanId,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlanTuruVarsayilanOzellikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlanTuruId = table.Column<int>(type: "int", nullable: false),
                    OzellikTanimId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanTuruVarsayilanOzellikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlanTuruVarsayilanOzellikleri_AlanOzellikTanimlari_OzellikTanimId",
                        column: x => x.OzellikTanimId,
                        principalTable: "AlanOzellikTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlanTuruVarsayilanOzellikleri_AlanTurleri_AlanTuruId",
                        column: x => x.AlanTuruId,
                        principalTable: "AlanTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlanAdresleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlanId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ilce = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mahalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcikAdres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanAdresleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlanAdresleri_Alanlar_AlanId",
                        column: x => x.AlanId,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlanOzellikDegerleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlanId = table.Column<int>(type: "int", nullable: false),
                    OzellikTanimId = table.Column<int>(type: "int", nullable: false),
                    Deger = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanOzellikDegerleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlanOzellikDegerleri_AlanOzellikTanimlari_OzellikTanimId",
                        column: x => x.OzellikTanimId,
                        principalTable: "AlanOzellikTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlanOzellikDegerleri_Alanlar_AlanId",
                        column: x => x.AlanId,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlanTarifeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlanId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false, comment: "Fixed=1, M2=2"),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanTarifeleri", x => x.Id);
                    table.CheckConstraint("CK_AlanTarifeleri_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_AlanTarifeleri_Alanlar_AlanId",
                        column: x => x.AlanId,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlanTarifeleri_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlanTarifeleri_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlanTarihcesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlanId = table.Column<int>(type: "int", nullable: false),
                    Alan = table.Column<int>(type: "int", nullable: false, comment: "Created=1, Type=2, Name=3, Parent=4, Area=5, Usage=6, PreAllocatable=7, Shared=8, Active=9"),
                    EskiDeger = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YeniDeger = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlanTarihcesi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlanTarihcesi_Alanlar_AlanId",
                        column: x => x.AlanId,
                        principalTable: "Alanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AlanAdresleri_AlanId",
                table: "AlanAdresleri",
                column: "AlanId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Alanlar_AlanTuruId",
                table: "Alanlar",
                column: "AlanTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_Alanlar_Yol",
                table: "Alanlar",
                column: "Yol");

            migrationBuilder.CreateIndex(
                name: "UX_Alanlar_Kok_Kod",
                table: "Alanlar",
                column: "Kod",
                unique: true,
                filter: "[UstAlanId] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Alanlar_UstAlan_Kod",
                table: "Alanlar",
                columns: new[] { "UstAlanId", "Kod" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AlanOzellikDegerleri_OzellikTanimId",
                table: "AlanOzellikDegerleri",
                column: "OzellikTanimId");

            migrationBuilder.CreateIndex(
                name: "UX_AlanOzellikDegerleri_AlanOzellik",
                table: "AlanOzellikDegerleri",
                columns: new[] { "AlanId", "OzellikTanimId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AlanOzellikTanimlari_Kod",
                table: "AlanOzellikTanimlari",
                column: "Kod",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AlanTarifeleri_BorcTipiId",
                table: "AlanTarifeleri",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_AlanTarifeleri_KiraciKategoriId",
                table: "AlanTarifeleri",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "UX_AlanTarifeleri_AlanKategoriBorc",
                table: "AlanTarifeleri",
                columns: new[] { "AlanId", "KiraciKategoriId", "BorcTipiId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AlanTarihcesi_Alan_Tarih",
                table: "AlanTarihcesi",
                columns: new[] { "AlanId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "UX_AlanTurleri_Kod",
                table: "AlanTurleri",
                column: "Kod",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AlanTuruVarsayilanOzellikleri_OzellikTanimId",
                table: "AlanTuruVarsayilanOzellikleri",
                column: "OzellikTanimId");

            migrationBuilder.CreateIndex(
                name: "UX_AlanTuruVarsayilanOzellikleri_TurOzellik",
                table: "AlanTuruVarsayilanOzellikleri",
                columns: new[] { "AlanTuruId", "OzellikTanimId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlanAdresleri");

            migrationBuilder.DropTable(
                name: "AlanOzellikDegerleri");

            migrationBuilder.DropTable(
                name: "AlanTarifeleri");

            migrationBuilder.DropTable(
                name: "AlanTarihcesi");

            migrationBuilder.DropTable(
                name: "AlanTuruVarsayilanOzellikleri");

            migrationBuilder.DropTable(
                name: "Alanlar");

            migrationBuilder.DropTable(
                name: "AlanOzellikTanimlari");

            migrationBuilder.DropTable(
                name: "AlanTurleri");
        }
    }
}
