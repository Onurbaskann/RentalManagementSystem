using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePropertyUnitWithSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Birimler_BirimId",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Tasinmazlar_TasinmazId",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_Birimler_BirimId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_RezervasyonTarifeler_BirimTurleri_BirimTuruId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropForeignKey(
                name: "FK_RezervasyonTarifeler_Birimler_BirimId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropForeignKey(
                name: "FK_SozlesmeGruplari_Birimler_BirimId",
                table: "SozlesmeGruplari");

            migrationBuilder.DropForeignKey(
                name: "FK_Sozlesmeler_Birimler_BirimId",
                table: "Sozlesmeler");

            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Birimler_BirimId",
                table: "Tahakkuklar");

            migrationBuilder.DropTable(
                name: "BirimKontenjanGecmisleri");

            migrationBuilder.DropTable(
                name: "BirimTarifeler");

            migrationBuilder.DropTable(
                name: "SozlesmeAlanGecmisleri");

            migrationBuilder.DropTable(
                name: "TasinmazTarifeler");

            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropTable(
                name: "BirimTurleri");

            migrationBuilder.DropTable(
                name: "Tasinmazlar");

            migrationBuilder.DropTable(
                name: "TasinmazTipleri");

            migrationBuilder.DropIndex(
                name: "IX_Tahakkuklar_BirimId_Aktif",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler");

            migrationBuilder.DropIndex(
                name: "UX_SozlesmeAlanlari_SozlesmeId_Silinmemis",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SozlesmeAlanlari_Alanlar",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropIndex(
                name: "IX_RezervasyonTarifeler_BirimId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropIndex(
                name: "UX_RezervasyonTarifeler_BirimTuruYil_GenelKural",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RezervasyonTarife_BirimOrYilTuru",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlari_BirimTarihDurum_Aktif",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_BirimId",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Birim_Aktif",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Genel_Aktif",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Tasinmaz_Aktif",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OdemeMagazaYonlendirmeleri_Kapsam",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "AnlikBirimTuruAdi",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikBirimYuzolcumu",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikKiralamaBicimi",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "AnlikKullanimTuru",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "HesaplamaModu",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "BirimTuruId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "Yil",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropColumn(
                name: "BirimIds",
                table: "Davetiyeler");

            migrationBuilder.RenameColumn(
                name: "BirimId",
                table: "SozlesmeGruplari",
                newName: "AlanId");

            migrationBuilder.RenameIndex(
                name: "IX_SozlesmeGruplari_BirimId",
                table: "SozlesmeGruplari",
                newName: "IX_SozlesmeGruplari_AlanId");

            migrationBuilder.RenameColumn(
                name: "FizikselAlan",
                table: "SozlesmeAlanlari",
                newName: "AnlikYuzolcumu");

            migrationBuilder.RenameColumn(
                name: "BirimId",
                table: "Rezervasyonlar",
                newName: "BorcTipiId");

            migrationBuilder.RenameColumn(
                name: "TasinmazId",
                table: "OdemeMagazaYonlendirmeleri",
                newName: "AlanId");

            migrationBuilder.RenameIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_TasinmazId",
                table: "OdemeMagazaYonlendirmeleri",
                newName: "IX_OdemeMagazaYonlendirmeleri_AlanId");

            migrationBuilder.RenameColumn(
                name: "TumTasinmazlaraErisim",
                table: "Davetiyeler",
                newName: "TumAlanlaraErisim");

            migrationBuilder.RenameColumn(
                name: "TasinmazIds",
                table: "Davetiyeler",
                newName: "AlanIds");

            migrationBuilder.RenameColumn(
                name: "TumTasinmazlaraErisim",
                table: "AspNetUsers",
                newName: "TumAlanlaraErisim");

            migrationBuilder.AddColumn<int>(
                name: "AlanId",
                table: "Tahakkuklar",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                comment: "UndefinedRate=0, LeaseRateOverride=1, SpaceRate=2, RateSchedule=3, ManualInput=5, ReservationRule=6",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "UndefinedRate=0, LeaseRateOverride=1, UnitRateOverride=2, RateSchedule=3, PropertyRateOverride=4, ManualInput=5, ReservationRule=6");

            migrationBuilder.AddColumn<int>(
                name: "AlanId",
                table: "SozlesmeAlanlari",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AnlikAd",
                table: "SozlesmeAlanlari",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnlikKod",
                table: "SozlesmeAlanlari",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AnlikKullanim",
                table: "SozlesmeAlanlari",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Rentable=1, Reservable=2, NotRentable=3");

            migrationBuilder.AddColumn<string>(
                name: "AnlikTurAdi",
                table: "SozlesmeAlanlari",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnlikYol",
                table: "SozlesmeAlanlari",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AlanId",
                table: "RezervasyonTarifeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BorcTipiId",
                table: "RezervasyonTarifeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AlanId",
                table: "Rezervasyonlar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_AlanId_Aktif",
                table: "Tahakkuklar",
                column: "AlanId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeAlanlari_AlanId",
                table: "SozlesmeAlanlari",
                column: "AlanId");

            migrationBuilder.CreateIndex(
                name: "UX_SozlesmeAlanlari_SozlesmeId_AlanId_Silinmemis",
                table: "SozlesmeAlanlari",
                columns: new[] { "SozlesmeId", "AlanId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SozlesmeAlanlari_KiralananAlan",
                table: "SozlesmeAlanlari",
                sql: "[KiralananAlan] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonTarifeler_BorcTipiId",
                table: "RezervasyonTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "UX_RezervasyonTarifeler_AlanId_Silinmemis",
                table: "RezervasyonTarifeler",
                column: "AlanId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_BorcTipiId",
                table: "Rezervasyonlar",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_AlanTarihDurum_Aktif",
                table: "Rezervasyonlar",
                columns: new[] { "AlanId", "BaslangicTarihi", "BitisTarihi", "Durum" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Alan_Aktif",
                table: "OdemeMagazaYonlendirmeleri",
                columns: new[] { "BorcTipiId", "AlanId" },
                unique: true,
                filter: "[AlanId] IS NOT NULL AND [Aktif] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Genel_Aktif",
                table: "OdemeMagazaYonlendirmeleri",
                column: "BorcTipiId",
                unique: true,
                filter: "[AlanId] IS NULL AND [Aktif] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciYetkiKapsamlari_KapsamId",
                table: "KullaniciYetkiKapsamlari",
                column: "KapsamId");

            migrationBuilder.AddForeignKey(
                name: "FK_KullaniciYetkiKapsamlari_Alanlar_KapsamId",
                table: "KullaniciYetkiKapsamlari",
                column: "KapsamId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Alanlar_AlanId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_Alanlar_AlanId",
                table: "Rezervasyonlar",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_BorcTipleri_BorcTipiId",
                table: "Rezervasyonlar",
                column: "BorcTipiId",
                principalTable: "BorcTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RezervasyonTarifeler_Alanlar_AlanId",
                table: "RezervasyonTarifeler",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RezervasyonTarifeler_BorcTipleri_BorcTipiId",
                table: "RezervasyonTarifeler",
                column: "BorcTipiId",
                principalTable: "BorcTipleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SozlesmeAlanlari_Alanlar_AlanId",
                table: "SozlesmeAlanlari",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SozlesmeGruplari_Alanlar_AlanId",
                table: "SozlesmeGruplari",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Alanlar_AlanId",
                table: "Tahakkuklar",
                column: "AlanId",
                principalTable: "Alanlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KullaniciYetkiKapsamlari_Alanlar_KapsamId",
                table: "KullaniciYetkiKapsamlari");

            migrationBuilder.DropForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Alanlar_AlanId",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_Alanlar_AlanId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_BorcTipleri_BorcTipiId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_RezervasyonTarifeler_Alanlar_AlanId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropForeignKey(
                name: "FK_RezervasyonTarifeler_BorcTipleri_BorcTipiId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropForeignKey(
                name: "FK_SozlesmeAlanlari_Alanlar_AlanId",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SozlesmeGruplari_Alanlar_AlanId",
                table: "SozlesmeGruplari");

            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Alanlar_AlanId",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_Tahakkuklar_AlanId_Aktif",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_SozlesmeAlanlari_AlanId",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropIndex(
                name: "UX_SozlesmeAlanlari_SozlesmeId_AlanId_Silinmemis",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SozlesmeAlanlari_KiralananAlan",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropIndex(
                name: "IX_RezervasyonTarifeler_BorcTipiId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropIndex(
                name: "UX_RezervasyonTarifeler_AlanId_Silinmemis",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_BorcTipiId",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlari_AlanTarihDurum_Aktif",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Alan_Aktif",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropIndex(
                name: "UX_OdemeMagazaYonlendirmeleri_Genel_Aktif",
                table: "OdemeMagazaYonlendirmeleri");

            migrationBuilder.DropIndex(
                name: "IX_KullaniciYetkiKapsamlari_KapsamId",
                table: "KullaniciYetkiKapsamlari");

            migrationBuilder.DropColumn(
                name: "AlanId",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "AlanId",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AnlikAd",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AnlikKod",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AnlikKullanim",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AnlikTurAdi",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AnlikYol",
                table: "SozlesmeAlanlari");

            migrationBuilder.DropColumn(
                name: "AlanId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "BorcTipiId",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "AlanId",
                table: "Rezervasyonlar");

            migrationBuilder.RenameColumn(
                name: "AlanId",
                table: "SozlesmeGruplari",
                newName: "BirimId");

            migrationBuilder.RenameIndex(
                name: "IX_SozlesmeGruplari_AlanId",
                table: "SozlesmeGruplari",
                newName: "IX_SozlesmeGruplari_BirimId");

            migrationBuilder.RenameColumn(
                name: "AnlikYuzolcumu",
                table: "SozlesmeAlanlari",
                newName: "FizikselAlan");

            migrationBuilder.RenameColumn(
                name: "BorcTipiId",
                table: "Rezervasyonlar",
                newName: "BirimId");

            migrationBuilder.RenameColumn(
                name: "AlanId",
                table: "OdemeMagazaYonlendirmeleri",
                newName: "TasinmazId");

            migrationBuilder.RenameIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_AlanId",
                table: "OdemeMagazaYonlendirmeleri",
                newName: "IX_OdemeMagazaYonlendirmeleri_TasinmazId");

            migrationBuilder.RenameColumn(
                name: "TumAlanlaraErisim",
                table: "Davetiyeler",
                newName: "TumTasinmazlaraErisim");

            migrationBuilder.RenameColumn(
                name: "AlanIds",
                table: "Davetiyeler",
                newName: "TasinmazIds");

            migrationBuilder.RenameColumn(
                name: "TumAlanlaraErisim",
                table: "AspNetUsers",
                newName: "TumTasinmazlaraErisim");

            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "Tahakkuklar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "KaynakTipi",
                table: "TahakkukKalemleri",
                type: "int",
                nullable: false,
                comment: "UndefinedRate=0, LeaseRateOverride=1, UnitRateOverride=2, RateSchedule=3, PropertyRateOverride=4, ManualInput=5, ReservationRule=6",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "UndefinedRate=0, LeaseRateOverride=1, SpaceRate=2, RateSchedule=3, ManualInput=5, ReservationRule=6");

            migrationBuilder.AddColumn<string>(
                name: "AnlikBirimTuruAdi",
                table: "Sozlesmeler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnlikBirimYuzolcumu",
                table: "Sozlesmeler",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AnlikKiralamaBicimi",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "UnitBased=1, CapacityBased=2");

            migrationBuilder.AddColumn<int>(
                name: "AnlikKullanimTuru",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Rentable=1, Reservable=2, NonRentable=3");

            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HesaplamaModu",
                table: "SozlesmeAlanlari",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "RezervasyonTarifeler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirimTuruId",
                table: "RezervasyonTarifeler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Yil",
                table: "RezervasyonTarifeler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "OdemeMagazaYonlendirmeleri",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BirimIds",
                table: "Davetiyeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BirimTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: true),
                    Kod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KiralamaBicimi = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "UnitBased=1, CapacityBased=2"),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KullanimTuru = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Rentable=1, Reservable=2, NonRentable=3")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimTurleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BirimTurleri_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeAlanGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SozlesmeAlanId = table.Column<int>(type: "int", nullable: false),
                    FizikselAlan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GecerlilikBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    HesaplamaModu = table.Column<int>(type: "int", nullable: false),
                    DegisiklikNedeni = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeAlanGecmisleri", x => x.Id);
                    table.CheckConstraint("CK_SozlesmeAlanGecmisleri_FizikselAlan", "[FizikselAlan] > 0");
                    table.ForeignKey(
                        name: "FK_SozlesmeAlanGecmisleri_SozlesmeAlanlari_SozlesmeAlanId",
                        column: x => x.SozlesmeAlanId,
                        principalTable: "SozlesmeAlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TasinmazTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    CokluBirimDestekli = table.Column<bool>(type: "bit", nullable: false),
                    TekBirimDestekli = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasinmazlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazTipiId = table.Column<int>(type: "int", nullable: true),
                    AcikAdres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Il = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KapaliYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ilce = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KatSayisi = table.Column<int>(type: "int", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mahalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcikYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BirimYapisi = table.Column<int>(type: "int", nullable: false, comment: "SingleUnit=1, MultipleUnits=2"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasinmazlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasinmazlar_TasinmazTipleri_TasinmazTipiId",
                        column: x => x.TasinmazTipiId,
                        principalTable: "TasinmazTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    BirimTuruId = table.Column<int>(type: "int", nullable: false),
                    Yuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Kontenjan = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KatNo = table.Column<int>(type: "int", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BirimNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.Id);
                    table.CheckConstraint("CK_Birimler_Kontenjan", "[Kontenjan] IS NULL OR [Kontenjan] > 0");
                    table.ForeignKey(
                        name: "FK_Birimler_BirimTurleri_BirimTuruId",
                        column: x => x.BirimTuruId,
                        principalTable: "BirimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Birimler_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TasinmazTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTarifeler", x => x.Id);
                    table.CheckConstraint("CK_TasinmazTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_TasinmazTarifeler_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasinmazTarifeler_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasinmazTarifeler_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BirimKontenjanGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    Kontenjan = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GecerlilikBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimKontenjanGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BirimKontenjanGecmisleri_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BirimTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimTarifeler", x => x.Id);
                    table.CheckConstraint("CK_BirimTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_BirimTarifeler_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BirimTarifeler_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BirimTarifeler_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_BirimId_Aktif",
                table: "Tahakkuklar",
                column: "BirimId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "UX_SozlesmeAlanlari_SozlesmeId_Silinmemis",
                table: "SozlesmeAlanlari",
                column: "SozlesmeId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SozlesmeAlanlari_Alanlar",
                table: "SozlesmeAlanlari",
                sql: "[KiralananAlan] > 0 AND [FizikselAlan] > 0 AND [FizikselAlan] <= [KiralananAlan]");

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonTarifeler_BirimId",
                table: "RezervasyonTarifeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "UX_RezervasyonTarifeler_BirimTuruYil_GenelKural",
                table: "RezervasyonTarifeler",
                columns: new[] { "BirimTuruId", "Yil" },
                unique: true,
                filter: "[BirimId] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RezervasyonTarife_BirimOrYilTuru",
                table: "RezervasyonTarifeler",
                sql: "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_BirimTarihDurum_Aktif",
                table: "Rezervasyonlar",
                columns: new[] { "BirimId", "BaslangicTarihi", "BitisTarihi", "Durum" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeMagazaYonlendirmeleri_BirimId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "BirimId");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_OdemeMagazaYonlendirmeleri_Kapsam",
                table: "OdemeMagazaYonlendirmeleri",
                sql: "([TasinmazId] IS NULL AND [BirimId] IS NULL) OR ([TasinmazId] IS NOT NULL AND [BirimId] IS NULL) OR ([TasinmazId] IS NULL AND [BirimId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BirimKontenjanGecmisleri_BirimId_GecerlilikBaslangici",
                table: "BirimKontenjanGecmisleri",
                columns: new[] { "BirimId", "GecerlilikBaslangici" });

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_BirimTuruId",
                table: "Birimler",
                column: "BirimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_TasinmazId",
                table: "Birimler",
                column: "TasinmazId");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTarifeler_BorcTipiId",
                table: "BirimTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTarifeler_KiraciKategoriId",
                table: "BirimTarifeler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "UX_BirimTarifeler_BirimKategoriBorc",
                table: "BirimTarifeler",
                columns: new[] { "BirimId", "KiraciKategoriId", "BorcTipiId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTurleri_BorcTipiId",
                table: "BirimTurleri",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTurleri_Kod",
                table: "BirimTurleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeAlanGecmisleri_SozlesmeAlanId_GecerlilikBaslangici",
                table: "SozlesmeAlanGecmisleri",
                columns: new[] { "SozlesmeAlanId", "GecerlilikBaslangici" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasinmazlar_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTarifeler_BorcTipiId",
                table: "TasinmazTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTarifeler_KiraciKategoriId",
                table: "TasinmazTarifeler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "UX_TasinmazTarifeler_TasinmazKategoriBorc",
                table: "TasinmazTarifeler",
                columns: new[] { "TasinmazId", "KiraciKategoriId", "BorcTipiId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTipleri_Kod",
                table: "TasinmazTipleri",
                column: "Kod",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Birimler_BirimId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OdemeMagazaYonlendirmeleri_Tasinmazlar_TasinmazId",
                table: "OdemeMagazaYonlendirmeleri",
                column: "TasinmazId",
                principalTable: "Tasinmazlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_Birimler_BirimId",
                table: "Rezervasyonlar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RezervasyonTarifeler_BirimTurleri_BirimTuruId",
                table: "RezervasyonTarifeler",
                column: "BirimTuruId",
                principalTable: "BirimTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RezervasyonTarifeler_Birimler_BirimId",
                table: "RezervasyonTarifeler",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SozlesmeGruplari_Birimler_BirimId",
                table: "SozlesmeGruplari",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sozlesmeler_Birimler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Birimler_BirimId",
                table: "Tahakkuklar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
