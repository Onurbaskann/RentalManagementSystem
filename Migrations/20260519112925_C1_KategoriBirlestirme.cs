using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class C1_KategoriBirlestirme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Eski FK kısıtlarını kaldır (tablo drop'undan önce yapılmalı)
            migrationBuilder.DropForeignKey(
                name: "FK_BirimRateler_KiraciKategorileri_KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_KiraciKategorileri_KiraciKategoriId",
                table: "Kiraciler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_Sektorler_SektorId",
                table: "Kiraciler");

            migrationBuilder.DropForeignKey(
                name: "FK_TarifeKalemleri_KiraciKategorileri_KiraciKategoriId",
                table: "TarifeKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_TasinmazKiraciKategoriFiyatlari_KiraciKategorileri_KiraciKategoriId",
                table: "TasinmazKiraciKategoriFiyatlari");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasinmazlar_TasinmazTipleri_TasinmazTipiId",
                table: "Tasinmazlar");

            // 2. Yeni Kategoriler tablosunu oluştur
            migrationBuilder.CreateTable(
                name: "Kategoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipi = table.Column<int>(type: "int", nullable: false, comment: "Tasinmaz=1, Kiraci=2, Sektor=3"),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TekParcaDestekli = table.Column<bool>(type: "bit", nullable: false),
                    BirimBazliDestekli = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoriler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_Tipi_Kod",
                table: "Kategoriler",
                columns: new[] { "Tipi", "Kod" },
                unique: true);

            // 3. Eski tablolardan veri kopyala
            migrationBuilder.Sql(@"
                INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
                SELECT 1, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli FROM TasinmazTipleri;

                INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
                SELECT 2, Ad, Kod, Aktif, Sira, OlusturmaTarihi, 0, 0 FROM KiraciKategorileri;

                INSERT INTO Kategoriler (Tipi, Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
                SELECT 3, Ad, Kod, Aktif, Sira, OlusturmaTarihi, 0, 0 FROM Sektorler;
            ");

            // 4. FK kolon değerlerini yeni Kategoriler.Id'ye güncelle (Kod üzerinden eşleştirme)
            migrationBuilder.Sql(@"
                UPDATE t SET t.TasinmazTipiId = k.Id
                FROM Tasinmazlar t
                INNER JOIN TasinmazTipleri eski ON eski.Id = t.TasinmazTipiId
                INNER JOIN Kategoriler k ON k.Tipi = 1 AND k.Kod = eski.Kod;

                UPDATE kir SET kir.KiraciKategoriId = kat.Id
                FROM Kiraciler kir
                INNER JOIN KiraciKategorileri eski ON eski.Id = kir.KiraciKategoriId
                INNER JOIN Kategoriler kat ON kat.Tipi = 2 AND kat.Kod = eski.Kod;

                UPDATE kir SET kir.SektorId = kat.Id
                FROM Kiraciler kir
                INNER JOIN Sektorler eski ON eski.Id = kir.SektorId
                INNER JOIN Kategoriler kat ON kat.Tipi = 3 AND kat.Kod = eski.Kod;

                UPDATE br SET br.KiraciKategoriId = kat.Id
                FROM BirimRateler br
                INNER JOIN KiraciKategorileri eski ON eski.Id = br.KiraciKategoriId
                INNER JOIN Kategoriler kat ON kat.Tipi = 2 AND kat.Kod = eski.Kod;

                UPDATE tk SET tk.KiraciKategoriId = kat.Id
                FROM TarifeKalemleri tk
                INNER JOIN KiraciKategorileri eski ON eski.Id = tk.KiraciKategoriId
                INNER JOIN Kategoriler kat ON kat.Tipi = 2 AND kat.Kod = eski.Kod;

                UPDATE f SET f.KiraciKategoriId = kat.Id
                FROM TasinmazKiraciKategoriFiyatlari f
                INNER JOIN KiraciKategorileri eski ON eski.Id = f.KiraciKategoriId
                INNER JOIN Kategoriler kat ON kat.Tipi = 2 AND kat.Kod = eski.Kod;
            ");

            // 5. Eski tabloları kaldır (FK kısıtları ve veri eşleme tamamlandıktan sonra)
            migrationBuilder.DropTable(name: "KiraciKategorileri");
            migrationBuilder.DropTable(name: "Sektorler");
            migrationBuilder.DropTable(name: "TasinmazTipleri");

            // 6. Yeni FK kısıtlarını Kategoriler tablosuna ekle
            migrationBuilder.AddForeignKey(
                name: "FK_BirimRateler_Kategoriler_KiraciKategoriId",
                table: "BirimRateler",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_Kategoriler_KiraciKategoriId",
                table: "Kiraciler",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_Kategoriler_SektorId",
                table: "Kiraciler",
                column: "SektorId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_TarifeKalemleri_Kategoriler_KiraciKategoriId",
                table: "TarifeKalemleri",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TasinmazKiraciKategoriFiyatlari_Kategoriler_KiraciKategoriId",
                table: "TasinmazKiraciKategoriFiyatlari",
                column: "KiraciKategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasinmazlar_Kategoriler_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BirimRateler_Kategoriler_KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_Kategoriler_KiraciKategoriId",
                table: "Kiraciler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_Kategoriler_SektorId",
                table: "Kiraciler");

            migrationBuilder.DropForeignKey(
                name: "FK_TarifeKalemleri_Kategoriler_KiraciKategoriId",
                table: "TarifeKalemleri");

            migrationBuilder.DropForeignKey(
                name: "FK_TasinmazKiraciKategoriFiyatlari_Kategoriler_KiraciKategoriId",
                table: "TasinmazKiraciKategoriFiyatlari");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasinmazlar_Kategoriler_TasinmazTipiId",
                table: "Tasinmazlar");

            migrationBuilder.CreateTable(
                name: "KiraciKategorileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false)
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
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sektorler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TasinmazTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    BirimBazliDestekli = table.Column<bool>(type: "bit", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    TekParcaDestekli = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTipleri", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTipleri_Kod",
                table: "TasinmazTipleri",
                column: "Kod",
                unique: true);

            // Eski tablolara veri geri kopyala
            migrationBuilder.Sql(@"
                INSERT INTO TasinmazTipleri (Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli)
                SELECT Ad, Kod, Aktif, Sira, OlusturmaTarihi, TekParcaDestekli, BirimBazliDestekli FROM Kategoriler WHERE Tipi = 1;

                INSERT INTO KiraciKategorileri (Ad, Kod, Aktif, Sira, OlusturmaTarihi)
                SELECT Ad, Kod, Aktif, Sira, OlusturmaTarihi FROM Kategoriler WHERE Tipi = 2;

                INSERT INTO Sektorler (Ad, Kod, Aktif, Sira, OlusturmaTarihi)
                SELECT Ad, Kod, Aktif, Sira, OlusturmaTarihi FROM Kategoriler WHERE Tipi = 3;
            ");

            // FK kolon değerlerini eski tablo ID'lerine güncelle
            migrationBuilder.Sql(@"
                UPDATE t SET t.TasinmazTipiId = eski.Id
                FROM Tasinmazlar t
                INNER JOIN Kategoriler kat ON kat.Id = t.TasinmazTipiId AND kat.Tipi = 1
                INNER JOIN TasinmazTipleri eski ON eski.Kod = kat.Kod;

                UPDATE kir SET kir.KiraciKategoriId = eski.Id
                FROM Kiraciler kir
                INNER JOIN Kategoriler kat ON kat.Id = kir.KiraciKategoriId AND kat.Tipi = 2
                INNER JOIN KiraciKategorileri eski ON eski.Kod = kat.Kod;

                UPDATE kir SET kir.SektorId = eski.Id
                FROM Kiraciler kir
                INNER JOIN Kategoriler kat ON kat.Id = kir.SektorId AND kat.Tipi = 3
                INNER JOIN Sektorler eski ON eski.Kod = kat.Kod;

                UPDATE br SET br.KiraciKategoriId = eski.Id
                FROM BirimRateler br
                INNER JOIN Kategoriler kat ON kat.Id = br.KiraciKategoriId AND kat.Tipi = 2
                INNER JOIN KiraciKategorileri eski ON eski.Kod = kat.Kod;

                UPDATE tk SET tk.KiraciKategoriId = eski.Id
                FROM TarifeKalemleri tk
                INNER JOIN Kategoriler kat ON kat.Id = tk.KiraciKategoriId AND kat.Tipi = 2
                INNER JOIN KiraciKategorileri eski ON eski.Kod = kat.Kod;

                UPDATE f SET f.KiraciKategoriId = eski.Id
                FROM TasinmazKiraciKategoriFiyatlari f
                INNER JOIN Kategoriler kat ON kat.Id = f.KiraciKategoriId AND kat.Tipi = 2
                INNER JOIN KiraciKategorileri eski ON eski.Kod = kat.Kod;
            ");

            migrationBuilder.DropTable(name: "Kategoriler");

            migrationBuilder.AddForeignKey(
                name: "FK_BirimRateler_KiraciKategorileri_KiraciKategoriId",
                table: "BirimRateler",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_KiraciKategorileri_KiraciKategoriId",
                table: "Kiraciler",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_Sektorler_SektorId",
                table: "Kiraciler",
                column: "SektorId",
                principalTable: "Sektorler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TarifeKalemleri_KiraciKategorileri_KiraciKategoriId",
                table: "TarifeKalemleri",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TasinmazKiraciKategoriFiyatlari_KiraciKategorileri_KiraciKategoriId",
                table: "TasinmazKiraciKategoriFiyatlari",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasinmazlar_TasinmazTipleri_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId",
                principalTable: "TasinmazTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
