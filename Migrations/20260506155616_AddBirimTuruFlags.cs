using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBirimTuruFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Kolonları ekle (varsayılan: her ikisi false)
            migrationBuilder.AddColumn<bool>(
                name: "KiralanabilirMi",
                table: "BirimTurleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RezervasyonYapilabilirMi",
                table: "BirimTurleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2. Kiralanabilir türleri güncelle: OFIS, DIGER
            migrationBuilder.Sql(@"
                UPDATE BirimTurleri
                SET KiralanabilirMi = 1
                WHERE Kod IN ('OFIS', 'DIGER')
            ");

            // 3. Rezervasyon türünü güncelle: TOPLANTI
            migrationBuilder.Sql(@"
                UPDATE BirimTurleri
                SET RezervasyonYapilabilirMi = 1, KiralanabilirMi = 0
                WHERE Kod = 'TOPLANTI'
            ");

            // 4. Artık TasinmazTipi'nde olan türleri pasif yap
            migrationBuilder.Sql(@"
                UPDATE BirimTurleri
                SET Aktif = 0, KiralanabilirMi = 0
                WHERE Kod IN ('OTOMAT', 'BANKAMATIK', 'DEPO')
            ");

            // 5. Yeni rezervasyon alanı türlerini ekle
            migrationBuilder.Sql(@"
                INSERT INTO BirimTurleri (Ad, Kod, Aktif, KiralanabilirMi, RezervasyonYapilabilirMi, Sira, OlusturmaTarihi) VALUES
                (N'Etkinlik Alanı',    'ETKINLIK',  1, 0, 1, 11, GETUTCDATE()),
                (N'Konferans Salonu',  'KONFERANS',  1, 0, 1, 12, GETUTCDATE())
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KiralanabilirMi",
                table: "BirimTurleri");

            migrationBuilder.DropColumn(
                name: "RezervasyonYapilabilirMi",
                table: "BirimTurleri");
        }
    }
}
