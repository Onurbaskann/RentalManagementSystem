using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserType = table.Column<int>(type: "int", nullable: true),
                    KiraciId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankaHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankaReferansNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankaKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GondericiIban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GondericiBilgisi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IslemTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EslesmeDurumu = table.Column<int>(type: "int", nullable: false, comment: "Unmatched=1, Matched=2, ManuallyMatched=3"),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaHareketleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BorcTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Davranis = table.Column<int>(type: "int", nullable: false, comment: "MonthlyFixed=1, FirstMonthOneTime=2, UserManual=3, ReservationSpecific=4"),
                    Sistem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorcTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnumDegerleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnumAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Deger = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumDegerleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kategoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipi = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoriler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciYetkiKapsamlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KapsamTipi = table.Column<int>(type: "int", nullable: false),
                    KapsamId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciYetkiKapsamlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciYetkileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SifreSifirlamaTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    KullanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TalepEdenIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SifreSifirlamaTalepleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TasinmazTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    TekBirimDestekli = table.Column<bool>(type: "bit", nullable: false),
                    CokluBirimDestekli = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTipleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BirimTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: true),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KullanimTuru = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Rentable=1, Reservable=2, NonRentable=3"),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
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
                name: "GenelTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_GenelTarifeler", x => x.Id);
                    table.CheckConstraint("CK_GenelTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_GenelTarifeler_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenelTarifeler_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kiracilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: true),
                    SektorId = table.Column<int>(type: "int", nullable: true),
                    KiraciNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TicaretSicilNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VergiNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VergiDairesi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MersisNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Adres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kiracilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kiracilar_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kiracilar_Kategoriler_SektorId",
                        column: x => x.SektorId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tasinmazlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazTipiId = table.Column<int>(type: "int", nullable: true),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BirimYapisi = table.Column<int>(type: "int", nullable: false, comment: "SingleUnit=1, MultipleUnits=2"),
                    AcikYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KapaliYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KatSayisi = table.Column<int>(type: "int", nullable: true),
                    Il = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ilce = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mahalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcikAdres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
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
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: true),
                    TumTasinmazlaraErisim = table.Column<bool>(type: "bit", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.CheckConstraint("CK_ApplicationUser_SuperAdmin_KiraciYok", "[IsSuperAdmin] = 0 OR [KiraciId] IS NULL");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OdemeLinkKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GecerlilikTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    IptalEdenUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IptalTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeLinkKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeLinkKayitlari_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Scope = table.Column<int>(type: "int", nullable: false, comment: "Internal=1, Tenant=2"),
                    KiraciId = table.Column<int>(type: "int", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roller_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    BirimTuruId = table.Column<int>(type: "int", nullable: false),
                    KatNo = table.Column<int>(type: "int", nullable: true),
                    BirimNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Yuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.Id);
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
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    ChargeTypeId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTarifeler", x => x.Id);
                    table.CheckConstraint("CK_TasinmazTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_TasinmazTarifeler_BorcTipleri_ChargeTypeId",
                        column: x => x.ChargeTypeId,
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
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Davetiyeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KullaniciTipi = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: true),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GecerlilikTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    DavetEdenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KabulTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TumTasinmazlaraErisim = table.Column<bool>(type: "bit", nullable: false),
                    TasinmazIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirimIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Davetiyeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Davetiyeler_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciRoller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciRoller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciRoller_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolYetkileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BirimTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: false),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Rezervasyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToplamSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretliSureDakika = table.Column<int>(type: "int", nullable: false),
                    BirimUcreti = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TarifeTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Planned=1, Completed=2, Cancelled=3, TransferredToCharge=4"),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervasyonlar", x => x.Id);
                    table.CheckConstraint("CK_Rezervasyonlari_KdvOrani", "[KdvOrani] IS NULL OR [KdvOrani] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Rezervasyonlari_TarihSirasi", "[BitisTarihi] > [BaslangicTarihi]");
                    table.CheckConstraint("CK_Rezervasyonlari_Tutarlar_Pozitif", "[TarifeTutari] >= 0 AND [ToplamTutar] >= 0 AND ([KdvTutari] IS NULL OR [KdvTutari] >= 0)");
                    table.ForeignKey(
                        name: "FK_Rezervasyonlar_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlar_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RezervasyonTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    BirimTuruId = table.Column<int>(type: "int", nullable: true),
                    Yil = table.Column<int>(type: "int", nullable: true),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretlendirmePeriyoduDakika = table.Column<int>(type: "int", nullable: false),
                    PeriyotUcreti = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RezervasyonTarifeler", x => x.Id);
                    table.CheckConstraint("CK_RezervasyonTarife_BirimOrYilTuru", "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");
                    table.CheckConstraint("CK_RezervasyonTarifeler_Degerler_Pozitif", "[PeriyotUcreti] >= 0 AND [UcretsizSureDakika] >= 0 AND [UcretlendirmePeriyoduDakika] > 0 AND [KdvOrani] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_RezervasyonTarifeler_BirimTurleri_BirimTuruId",
                        column: x => x.BirimTuruId,
                        principalTable: "BirimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RezervasyonTarifeler_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sozlesmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Active=1, Ended=2, Terminated=3"),
                    KdvUygulanacakMi = table.Column<bool>(type: "bit", nullable: false),
                    VadeKuraliTipi = table.Column<int>(type: "int", nullable: false),
                    VadeGunu = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FesihTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FesihNedeni = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sozlesmeler", x => x.Id);
                    table.CheckConstraint("CK_Sozlesmeler_TarihSirasi", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_Sozlesmeler_VadeGunu", "[VadeGunu] BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_Sozlesmeler_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sozlesmeler_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeIslemGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseId = table.Column<int>(type: "int", nullable: false),
                    IslemTipi = table.Column<int>(type: "int", nullable: false, comment: "Creation=1, Extension=2, Termination=3, TufeIncrease=4, KdvUpdate=5, ChargeRegeneration=6"),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EskiBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YeniBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EskiKiraBedeli = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YeniKiraBedeli = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TufeOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvUygulandiMi = table.Column<bool>(type: "bit", nullable: true),
                    KdvRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    KdvDahilTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeIslemGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozlesmeIslemGecmisleri_Sozlesmeler_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseId = table.Column<int>(type: "int", nullable: false),
                    ChargeTypeId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KdvRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeTarifeler", x => x.Id);
                    table.CheckConstraint("CK_SozlesmeTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_SozlesmeTarifeler_BorcTipleri_ChargeTypeId",
                        column: x => x.ChargeTypeId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SozlesmeTarifeler_Sozlesmeler_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tahakkuklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    SozlesmeId = table.Column<int>(type: "int", nullable: true),
                    RezervasyonId = table.Column<int>(type: "int", nullable: true),
                    DonemBaslangici = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonemBitisi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SonOdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeklenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Pending=1, PartiallyPaid=2, Paid=3, Overdue=4, Cancelled=5"),
                    KaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "Lease=1, Manual=2, Reservation=3"),
                    IptalNotu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SonHatirlatmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tahakkuklar", x => x.Id);
                    table.CheckConstraint("CK_Tahakkuklar_OdenenLimit", "[OdenenTutar] <= [ToplamTutar]");
                    table.CheckConstraint("CK_Tahakkuklar_TarihSirasi", "[DonemBitisi] > [DonemBaslangici]");
                    table.CheckConstraint("CK_Tahakkuklar_Tutarlar_Pozitif", "[BeklenenTutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0 AND [OdenenTutar] >= 0");
                    table.ForeignKey(
                        name: "FK_Tahakkuklar_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tahakkuklar_Kiracilar_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiracilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tahakkuklar_Rezervasyonlar_RezervasyonId",
                        column: x => x.RezervasyonId,
                        principalTable: "Rezervasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tahakkuklar_Sozlesmeler_SozlesmeId",
                        column: x => x.SozlesmeId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TahakkukKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TahakkukId = table.Column<int>(type: "int", nullable: false),
                    TahakkukTipiId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false, comment: "Fixed=1, M2=2"),
                    BirimDegeri = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Carpan = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "UndefinedRate=0, LeaseRateOverride=1, UnitRateOverride=2, RateSchedule=3, PropertyRateOverride=4, ManualInput=5, ReservationRule=6"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TahakkukKalemleri", x => x.Id);
                    table.CheckConstraint("CK_TahakkukKalemleri_KdvOrani", "[KdvOrani] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_TahakkukKalemleri_Tutarlar_Pozitif", "[Tutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0");
                    table.ForeignKey(
                        name: "FK_TahakkukKalemleri_BorcTipleri_TahakkukTipiId",
                        column: x => x.TahakkukTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TahakkukKalemleri_Tahakkuklar_TahakkukId",
                        column: x => x.TahakkukId,
                        principalTable: "Tahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TahakkukOdemeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TahakkukId = table.Column<int>(type: "int", nullable: false),
                    SozlesmeId = table.Column<int>(type: "int", nullable: true),
                    GirenKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OnaylayanKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeKanali = table.Column<int>(type: "int", nullable: false, comment: "BankTransfer=1, Eft=2, Cash=3, Other=4"),
                    OdemeKaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "Manual=1, BankMatch=2, VirtualPos=3"),
                    PosReferansNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "PendingApproval=1, Approved=2, Rejected=3"),
                    GirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedNedeni = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TahakkukOdemeleri", x => x.Id);
                    table.CheckConstraint("CK_TahakkukOdemeler_Tutar_Pozitif", "[Tutar] > 0");
                    table.ForeignKey(
                        name: "FK_TahakkukOdemeleri_AspNetUsers_GirenKullaniciId",
                        column: x => x.GirenKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TahakkukOdemeleri_AspNetUsers_OnaylayanKullaniciId",
                        column: x => x.OnaylayanKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TahakkukOdemeleri_Sozlesmeler_SozlesmeId",
                        column: x => x.SozlesmeId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TahakkukOdemeleri_Tahakkuklar_TahakkukId",
                        column: x => x.TahakkukId,
                        principalTable: "Tahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OdemeBankaEslesmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TahakkukOdemesiId = table.Column<int>(type: "int", nullable: false),
                    BankaHareketId = table.Column<int>(type: "int", nullable: false),
                    EslesmeTipi = table.Column<int>(type: "int", nullable: false, comment: "Automatic=1, Manual=2"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeBankaEslesmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_BankaHareketleri_BankaHareketId",
                        column: x => x.BankaHareketId,
                        principalTable: "BankaHareketleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_TahakkukOdemeleri_TahakkukOdemesiId",
                        column: x => x.TahakkukOdemesiId,
                        principalTable: "TahakkukOdemeleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    SahipTipi = table.Column<int>(type: "int", nullable: false, comment: "Tenant=1, Payment=2, Lease=3, Template=99"),
                    SahipId = table.Column<int>(type: "int", nullable: false),
                    DosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
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
                    HedefEntite = table.Column<int>(type: "int", nullable: false, comment: "Tenant=1, Payment=2, Lease=3, Template=99"),
                    Zorunlu = table.Column<bool>(type: "bit", nullable: false),
                    IzinVerilenUzantilar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxBoyutMb = table.Column<int>(type: "int", nullable: false),
                    SablonBelgeId = table.Column<int>(type: "int", nullable: true),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Sistem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.InsertData(
                table: "BelgeTurleri",
                columns: new[] { "Id", "IzinVerilenUzantilar", "Kod", "CreatedAt", "CreatedBy", "Aciklama", "Aktif", "IsDeleted", "Sistem", "MaxBoyutMb", "Ad", "Zorunlu", "Sira", "HedefEntite", "SablonBelgeId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, "pdf,jpg,jpeg,png", "ODEME_DEKONT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, true, false, true, 5, "Ödeme Dekontu", false, 1, 2, null, null, null });

            migrationBuilder.InsertData(
                table: "BorcTipleri",
                columns: new[] { "Id", "Davranis", "Kod", "CreatedAt", "CreatedBy", "Aktif", "IsDeleted", "Sistem", "Ad", "Sira", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, 1, "KIRA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, false, true, "Kira Bedeli", 1, null, null },
                    { 2, 2, "DEPOZITO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, false, true, "Depozito", 99, null, null },
                    { 3, 3, "DIGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, false, true, "Diğer", 100, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_KiraciId",
                table: "AspNetUsers",
                column: "KiraciId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BankaHareketleri_BankaReferansNo",
                table: "BankaHareketleri",
                column: "BankaReferansNo",
                unique: true,
                filter: "[BankaReferansNo] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_BelgeTuruId",
                table: "Belgeler",
                column: "BelgeTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_DegistirenBelgeId",
                table: "Belgeler",
                column: "DegistirenBelgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Belgeler_SahipTipi_SahipId_Gecersiz_IsDeleted",
                table: "Belgeler",
                columns: new[] { "SahipTipi", "SahipId", "Gecersiz", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_BelgeTurleri_Kod",
                table: "BelgeTurleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BelgeTurleri_SablonBelgeId",
                table: "BelgeTurleri",
                column: "SablonBelgeId");

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
                name: "IX_BorcTipleri_Kod",
                table: "BorcTipleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Davetiyeler_Email_Durum",
                table: "Davetiyeler",
                columns: new[] { "Email", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_Davetiyeler_KiraciId_Aktif",
                table: "Davetiyeler",
                column: "KiraciId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Davetiyeler_RolId",
                table: "Davetiyeler",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_EnumDegerleri_EnumAdi_Deger",
                table: "EnumDegerleri",
                columns: new[] { "EnumAdi", "Deger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenelTarifeler_BorcTipiId",
                table: "GenelTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_GenelTarifeler_KiraciKategoriId",
                table: "GenelTarifeler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "UX_GenelTarifeler_YilKategoriBorc",
                table: "GenelTarifeler",
                columns: new[] { "Yil", "KiraciKategoriId", "BorcTipiId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_Tipi_Kod",
                table: "Kategoriler",
                columns: new[] { "Tipi", "Kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiracilar_KiraciKategoriId",
                table: "Kiracilar",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiracilar_KiraciNo",
                table: "Kiracilar",
                column: "KiraciNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiracilar_SektorId",
                table: "Kiracilar",
                column: "SektorId");

            migrationBuilder.CreateIndex(
                name: "UX_Kiraciler_VergiNo",
                table: "Kiracilar",
                column: "VergiNo",
                unique: true,
                filter: "[VergiNo] IS NOT NULL AND [VergiNo] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciRoller_RolId",
                table: "KullaniciRoller",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciRoller_UserId_RolId",
                table: "KullaniciRoller",
                columns: new[] { "UserId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciYetkiKapsamlari_UserId_KapsamTipi_KapsamId",
                table: "KullaniciYetkiKapsamlari",
                columns: new[] { "UserId", "KapsamTipi", "KapsamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciYetkileri_UserId_Permission",
                table: "KullaniciYetkileri",
                columns: new[] { "UserId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OdemeBankaEslesmeleri_BankaHareketi_Birebir",
                table: "OdemeBankaEslesmeleri",
                column: "BankaHareketId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OdemeBankaEslesmeleri_TahakkukOdeme_Birebir",
                table: "OdemeBankaEslesmeleri",
                column: "TahakkukOdemesiId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeLinkKayitlari_KiraciId_Durum",
                table: "OdemeLinkKayitlari",
                columns: new[] { "KiraciId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_BirimId_BaslangicTarihi",
                table: "Rezervasyonlar",
                columns: new[] { "BirimId", "BaslangicTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_KiraciId_Aktif",
                table: "Rezervasyonlar",
                column: "KiraciId",
                filter: "[IsDeleted] = 0");

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

            migrationBuilder.CreateIndex(
                name: "IX_Roller_KiraciId",
                table: "Roller",
                column: "KiraciId");

            migrationBuilder.CreateIndex(
                name: "IX_Roller_Scope_KiraciId_Ad",
                table: "Roller",
                columns: new[] { "Scope", "KiraciId", "Ad" },
                unique: true,
                filter: "[KiraciId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_RolId_Permission",
                table: "RolYetkileri",
                columns: new[] { "RolId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlamaTalepleri_UserId_Durum",
                table: "SifreSifirlamaTalepleri",
                columns: new[] { "UserId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeIslemGecmisleri_LeaseId",
                table: "SozlesmeIslemGecmisleri",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_KiraciId_Aktif",
                table: "Sozlesmeler",
                column: "KiraciId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeTarifeler_ChargeTypeId",
                table: "SozlesmeTarifeler",
                column: "ChargeTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_SozlesmeTarifeler_SozlesmeBorc",
                table: "SozlesmeTarifeler",
                columns: new[] { "LeaseId", "ChargeTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukKalemleri_TahakkukId",
                table: "TahakkukKalemleri",
                column: "TahakkukId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukKalemleri_TahakkukTipiId",
                table: "TahakkukKalemleri",
                column: "TahakkukTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_BirimId_Aktif",
                table: "Tahakkuklar",
                column: "BirimId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_KiraciId_Aktif",
                table: "Tahakkuklar",
                column: "KiraciId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tahakkuklar_RezervasyonId_TekTahakkuk",
                table: "Tahakkuklar",
                column: "RezervasyonId",
                unique: true,
                filter: "[RezervasyonId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tahakkuklar_SozlesmeDonem_TekTahakkuk",
                table: "Tahakkuklar",
                columns: new[] { "SozlesmeId", "DonemBaslangici" },
                unique: true,
                filter: "[SozlesmeId] IS NOT NULL AND [KaynakTipi] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_GirenKullaniciId",
                table: "TahakkukOdemeleri",
                column: "GirenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_OnaylayanKullaniciId",
                table: "TahakkukOdemeleri",
                column: "OnaylayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_SozlesmeId",
                table: "TahakkukOdemeleri",
                column: "SozlesmeId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukOdemeleri_TahakkukId",
                table: "TahakkukOdemeleri",
                column: "TahakkukId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasinmazlar_TasinmazTipiId",
                table: "Tasinmazlar",
                column: "TasinmazTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTarifeler_ChargeTypeId",
                table: "TasinmazTarifeler",
                column: "ChargeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTarifeler_KiraciKategoriId",
                table: "TasinmazTarifeler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "UX_TasinmazTarifeler_TasinmazKategoriBorc",
                table: "TasinmazTarifeler",
                columns: new[] { "TasinmazId", "KiraciKategoriId", "ChargeTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTipleri_Kod",
                table: "TasinmazTipleri",
                column: "Kod",
                unique: true);

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
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BelgeIcerikleri");

            migrationBuilder.DropTable(
                name: "BirimTarifeler");

            migrationBuilder.DropTable(
                name: "Davetiyeler");

            migrationBuilder.DropTable(
                name: "EnumDegerleri");

            migrationBuilder.DropTable(
                name: "GenelTarifeler");

            migrationBuilder.DropTable(
                name: "KullaniciRoller");

            migrationBuilder.DropTable(
                name: "KullaniciYetkiKapsamlari");

            migrationBuilder.DropTable(
                name: "KullaniciYetkileri");

            migrationBuilder.DropTable(
                name: "OdemeBankaEslesmeleri");

            migrationBuilder.DropTable(
                name: "OdemeLinkKayitlari");

            migrationBuilder.DropTable(
                name: "RezervasyonTarifeler");

            migrationBuilder.DropTable(
                name: "RolYetkileri");

            migrationBuilder.DropTable(
                name: "SifreSifirlamaTalepleri");

            migrationBuilder.DropTable(
                name: "SozlesmeIslemGecmisleri");

            migrationBuilder.DropTable(
                name: "SozlesmeTarifeler");

            migrationBuilder.DropTable(
                name: "TahakkukKalemleri");

            migrationBuilder.DropTable(
                name: "TasinmazTarifeler");

            migrationBuilder.DropTable(
                name: "BankaHareketleri");

            migrationBuilder.DropTable(
                name: "TahakkukOdemeleri");

            migrationBuilder.DropTable(
                name: "Roller");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Tahakkuklar");

            migrationBuilder.DropTable(
                name: "Rezervasyonlar");

            migrationBuilder.DropTable(
                name: "Sozlesmeler");

            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropTable(
                name: "Kiracilar");

            migrationBuilder.DropTable(
                name: "BirimTurleri");

            migrationBuilder.DropTable(
                name: "Tasinmazlar");

            migrationBuilder.DropTable(
                name: "Kategoriler");

            migrationBuilder.DropTable(
                name: "BorcTipleri");

            migrationBuilder.DropTable(
                name: "TasinmazTipleri");

            migrationBuilder.DropTable(
                name: "Belgeler");

            migrationBuilder.DropTable(
                name: "BelgeTurleri");
        }
    }
}
