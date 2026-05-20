using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumDegerleriTablosu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "ToplantiSalonuRezervasyonlari",
                type: "int",
                nullable: false,
                comment: "Planlandi=1, Tamamlandi=2, IptalEdildi=3, TahakkukaAktarildi=4",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KiralamaSekli",
                table: "Tasinmazlar",
                type: "int",
                nullable: false,
                comment: "TekParca=1, BirimBazli=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HesaplamaYontemi",
                table: "TarifeKalemleri",
                type: "int",
                nullable: false,
                comment: "Sabit=1, M2=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                comment: "TanimsizTarife=0, SozlesmeTarifesi=1, BirimTarifesi=2, GenelTarife=3, TasinmazTarifesi=4, ManuelGiris=5, RezervasyonKurali=6",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HesaplamaYontemi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                comment: "Sabit=1, M2=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                comment: "Aktif=1, SonaErdi=2, Feshedildi=3",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IslemTipi",
                table: "SozlesmeIslemGecmisleri",
                type: "int",
                nullable: false,
                comment: "Olusturma=1, SureUzatma=2, Fesih=3, TufeArtis=4, KdvGuncelleme=5, TahakkukYenidenUretim=6",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "EslesmeTipi",
                table: "OdemeBankaEslesmeleri",
                type: "int",
                nullable: false,
                comment: "Otomatik=1, Manuel=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                comment: "Sozlesme=1, Manuel=2, Rezervasyon=3",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                comment: "Bekleniyor=1, KismenOdendi=2, TamOdendi=3, Gecikti=4, IptalEdildi=5",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "OdemeKanali",
                table: "KiraOdemeler",
                type: "int",
                nullable: false,
                comment: "Havale=1, EFT=2, Nakit=3, Diger=4",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "KiraOdemeler",
                type: "int",
                nullable: false,
                comment: "OnayBekliyor=1, Onaylandi=2, Reddedildi=3",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "KiraciTuru",
                table: "Kiraciler",
                type: "int",
                nullable: false,
                comment: "Gercek=1, Tuzel=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Davranis",
                table: "BorcTipleri",
                type: "int",
                nullable: false,
                comment: "AylikSabit=1, IlkAyTekSeferlik=2, KullaniciManuel=3, RezervasyonOzel=4",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BirimTipi",
                table: "Birimler",
                type: "int",
                nullable: false,
                comment: "Komple=1, Birim=2",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "EslesmeDurumu",
                table: "BankaHareketleri",
                type: "int",
                nullable: false,
                comment: "Eslestirilmedi=1, Eslesti=2, ManuelEslesti=3",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "EnumDegerleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnumAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Deger = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumDegerleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnumDegerleri_EnumAdi_Deger",
                table: "EnumDegerleri",
                columns: new[] { "EnumAdi", "Deger" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnumDegerleri");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "ToplantiSalonuRezervasyonlari",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Planlandi=1, Tamamlandi=2, IptalEdildi=3, TahakkukaAktarildi=4");

            migrationBuilder.AlterColumn<int>(
                name: "KiralamaSekli",
                table: "Tasinmazlar",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "TekParca=1, BirimBazli=2");

            migrationBuilder.AlterColumn<int>(
                name: "HesaplamaYontemi",
                table: "TarifeKalemleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Sabit=1, M2=2");

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "TanimsizTarife=0, SozlesmeTarifesi=1, BirimTarifesi=2, GenelTarife=3, TasinmazTarifesi=4, ManuelGiris=5, RezervasyonKurali=6");

            migrationBuilder.AlterColumn<int>(
                name: "HesaplamaYontemi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Sabit=1, M2=2");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Aktif=1, SonaErdi=2, Feshedildi=3");

            migrationBuilder.AlterColumn<int>(
                name: "IslemTipi",
                table: "SozlesmeIslemGecmisleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Olusturma=1, SureUzatma=2, Fesih=3, TufeArtis=4, KdvGuncelleme=5, TahakkukYenidenUretim=6");

            migrationBuilder.AlterColumn<int>(
                name: "EslesmeTipi",
                table: "OdemeBankaEslesmeleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Otomatik=1, Manuel=2");

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Sozlesme=1, Manuel=2, Rezervasyon=3");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Bekleniyor=1, KismenOdendi=2, TamOdendi=3, Gecikti=4, IptalEdildi=5");

            migrationBuilder.AlterColumn<int>(
                name: "OdemeKanali",
                table: "KiraOdemeler",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Havale=1, EFT=2, Nakit=3, Diger=4");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "KiraOdemeler",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "OnayBekliyor=1, Onaylandi=2, Reddedildi=3");

            migrationBuilder.AlterColumn<int>(
                name: "KiraciTuru",
                table: "Kiraciler",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Gercek=1, Tuzel=2");

            migrationBuilder.AlterColumn<int>(
                name: "Davranis",
                table: "BorcTipleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "AylikSabit=1, IlkAyTekSeferlik=2, KullaniciManuel=3, RezervasyonOzel=4");

            migrationBuilder.AlterColumn<int>(
                name: "BirimTipi",
                table: "Birimler",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Komple=1, Birim=2");

            migrationBuilder.AlterColumn<int>(
                name: "EslesmeDurumu",
                table: "BankaHareketleri",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Eslestirilmedi=1, Eslesti=2, ManuelEslesti=3");
        }
    }
}
