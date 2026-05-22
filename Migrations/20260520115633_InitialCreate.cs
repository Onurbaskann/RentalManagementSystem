using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "BorcTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Davranis = table.Column<int>(type: "int", nullable: false, comment: "AylikSabit=1, IlkAyTekSeferlik=2, KullaniciManuel=3, RezervasyonOzel=4"),
                    Sistem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                    Tipi = table.Column<int>(type: "int", nullable: false, comment: "Tasinmaz=1, Kiraci=2, Sektor=3"),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TekParcaDestekli = table.Column<bool>(type: "bit", nullable: false),
                    BirimBazliDestekli = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoriler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GrantedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
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
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
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
                name: "BankaHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportEdenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HareketTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KarsiHesap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KarsiUnvan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BankaKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EslesmeDurumu = table.Column<int>(type: "int", nullable: false, comment: "Eslestirilmedi=1, Eslesti=2, ManuelEslesti=3"),
                    ImportTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaHareketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankaHareketleri_AspNetUsers_ImportEdenUserId",
                        column: x => x.ImportEdenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BirimTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorcTipiId = table.Column<int>(type: "int", nullable: true),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    KiralanabilirMi = table.Column<bool>(type: "bit", nullable: false),
                    RezervasyonYapilabilirMi = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false, comment: "Sabit=1, M2=2"),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenelTarifeler", x => x.Id);
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
                name: "Kiraciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraciKategoriId = table.Column<int>(type: "int", nullable: true),
                    SektorId = table.Column<int>(type: "int", nullable: true),
                    KiraciNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KiraciTuru = table.Column<int>(type: "int", nullable: false, comment: "Gercek=1, Tuzel=2"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TcKimlikNo = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    PasaportNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unvan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnneAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BabaAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DogumTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DogumYeri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicaretSicilNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VergiNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VergiDairesi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MersisNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Adres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KvkkOnayi = table.Column<bool>(type: "bit", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kiraciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kiraciler_Kategoriler_KiraciKategoriId",
                        column: x => x.KiraciKategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kiraciler_Kategoriler_SektorId",
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
                    KiralamaSekli = table.Column<int>(type: "int", nullable: false, comment: "TekParca=1, BirimBazli=2"),
                    Il = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ilce = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mahalle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcikAdres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AcikYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KapaliYuzolcumu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KatSayisi = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasinmazlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasinmazlar_Kategoriler_TasinmazTipiId",
                        column: x => x.TasinmazTipiId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimTuruId = table.Column<int>(type: "int", nullable: true),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    BirimTipi = table.Column<int>(type: "int", nullable: false, comment: "Komple=1, Birim=2"),
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Birimler_BirimTurleri_BirimTuruId",
                        column: x => x.BirimTuruId,
                        principalTable: "BirimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTarifeler", x => x.Id);
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
                name: "UserTasinmazYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TasinmazId = table.Column<int>(type: "int", nullable: false),
                    AtayanUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_UserTasinmazYetkileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTasinmazYetkileri_Tasinmazlar_TasinmazId",
                        column: x => x.TasinmazId,
                        principalTable: "Tasinmazlar",
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirimTarifeler", x => x.Id);
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
                name: "RezervasyonTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    BirimTuruId = table.Column<int>(type: "int", nullable: true),
                    Yil = table.Column<int>(type: "int", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretlendirmePeriyoduDakika = table.Column<int>(type: "int", nullable: false),
                    PeriyotUcreti = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RezervasyonTarifeler", x => x.Id);
                    table.CheckConstraint("CK_RezervasyonTarife_BirimOrYilTuru", "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");
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
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notlar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Aktif=1, SonaErdi=2, Feshedildi=3"),
                    FesihTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FesihNedeni = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KdvUygulanacakMi = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sozlesmeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sozlesmeler_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sozlesmeler_Kiraciler_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiraciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiraTahakkuklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: true),
                    DonemBaslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonemBitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeklenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Bekleniyor=1, KismenOdendi=2, TamOdendi=3, Gecikti=4, IptalEdildi=5"),
                    KaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "Sozlesme=1, Manuel=2, Rezervasyon=3"),
                    IptalNotu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SonHatirlatmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiraTahakkuklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiraTahakkuklar_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SozlesmeIslemGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: false),
                    IslemTipi = table.Column<int>(type: "int", nullable: false, comment: "Olusturma=1, SureUzatma=2, Fesih=3, TufeArtis=4, KdvGuncelleme=5, TahakkukYenidenUretim=6"),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EskiBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YeniBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EskiKiraBedeli = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YeniKiraBedeli = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TufeOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvUygulandiMi = table.Column<bool>(type: "bit", nullable: true),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    KdvDahilTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeIslemGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozlesmeIslemGecmisleri_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
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
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: false),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeTarifeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozlesmeTarifeler_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SozlesmeTarifeler_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KiraOdemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraTahakkukId = table.Column<int>(type: "int", nullable: false),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: true),
                    GirenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OnaylayanUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeKanali = table.Column<int>(type: "int", nullable: false, comment: "Havale=1, EFT=2, Nakit=3, Diger=4"),
                    OdemeKaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "Manuel=1, BankaEslesme=2, SanalPos=3"),
                    PosReferansNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "OnayBekliyor=1, Onaylandi=2, Reddedildi=3"),
                    GirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedNedeni = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiraOdemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_AspNetUsers_GirenUserId",
                        column: x => x.GirenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_AspNetUsers_OnaylayanUserId",
                        column: x => x.OnaylayanUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_KiraTahakkuklar_KiraTahakkukId",
                        column: x => x.KiraTahakkukId,
                        principalTable: "KiraTahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KiraOdemeler_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rezervasyonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: false),
                    KiraciId = table.Column<int>(type: "int", nullable: false),
                    KiraSozlesmesiId = table.Column<int>(type: "int", nullable: true),
                    KiraTahakkukId = table.Column<int>(type: "int", nullable: true),
                    OlusturanUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToplamSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretsizSureDakika = table.Column<int>(type: "int", nullable: false),
                    UcretliSureDakika = table.Column<int>(type: "int", nullable: false),
                    BirimUcret = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UcretTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false, comment: "Planlandi=1, Tamamlandi=2, IptalEdildi=3, TahakkukaAktarildi=4"),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervasyonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlari_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlari_KiraTahakkuklar_KiraTahakkukId",
                        column: x => x.KiraTahakkukId,
                        principalTable: "KiraTahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlari_Kiraciler_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiraciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervasyonlari_Sozlesmeler_KiraSozlesmesiId",
                        column: x => x.KiraSozlesmesiId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TahakkukKalemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TahakkukId = table.Column<int>(type: "int", nullable: false),
                    BorcTipiId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HesaplamaYontemi = table.Column<int>(type: "int", nullable: false, comment: "Sabit=1, M2=2"),
                    BirimDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Carpan = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KdvOrani = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KaynakTipi = table.Column<int>(type: "int", nullable: false, comment: "TanimsizTarife=0, SozlesmeTarifesi=1, BirimTarifesi=2, GenelTarife=3, TasinmazTarifesi=4, ManuelGiris=5, RezervasyonKurali=6"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TahakkukKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TahakkukKalemleri_BorcTipleri_BorcTipiId",
                        column: x => x.BorcTipiId,
                        principalTable: "BorcTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TahakkukKalemleri_KiraTahakkuklar_TahakkukId",
                        column: x => x.TahakkukId,
                        principalTable: "KiraTahakkuklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dekontlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraOdemeId = table.Column<int>(type: "int", nullable: false),
                    YukleyenUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrijinalDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiskDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DosyaTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DosyaBoyutu = table.Column<long>(type: "bigint", nullable: false),
                    YuklemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dekontlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dekontlar_AspNetUsers_YukleyenUserId",
                        column: x => x.YukleyenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dekontlar_KiraOdemeler_KiraOdemeId",
                        column: x => x.KiraOdemeId,
                        principalTable: "KiraOdemeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdemeBankaEslesmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KiraOdemeId = table.Column<int>(type: "int", nullable: false),
                    BankaHareketiId = table.Column<int>(type: "int", nullable: false),
                    EslestirenUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EslesmeTipi = table.Column<int>(type: "int", nullable: false, comment: "Otomatik=1, Manuel=2"),
                    EslesmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeBankaEslesmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_AspNetUsers_EslestirenUserId",
                        column: x => x.EslestirenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_BankaHareketleri_BankaHareketiId",
                        column: x => x.BankaHareketiId,
                        principalTable: "BankaHareketleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OdemeBankaEslesmeleri_KiraOdemeler_KiraOdemeId",
                        column: x => x.KiraOdemeId,
                        principalTable: "KiraOdemeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportBatchId",
                table: "BankaHareketleri",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaHareketleri_ImportEdenUserId",
                table: "BankaHareketleri",
                column: "ImportEdenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_BirimTuruId",
                table: "Birimler",
                column: "BirimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_TasinmazId",
                table: "Birimler",
                column: "TasinmazId");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTarifeler_BirimId_KiraciKategoriId_BorcTipiId",
                table: "BirimTarifeler",
                columns: new[] { "BirimId", "KiraciKategoriId", "BorcTipiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirimTarifeler_BorcTipiId",
                table: "BirimTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_BirimTarifeler_KiraciKategoriId",
                table: "BirimTarifeler",
                column: "KiraciKategoriId");

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
                name: "IX_Dekontlar_KiraOdemeId",
                table: "Dekontlar",
                column: "KiraOdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dekontlar_YukleyenUserId",
                table: "Dekontlar",
                column: "YukleyenUserId");

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
                name: "IX_GenelTarifeler_Yil_KiraciKategoriId_BorcTipiId",
                table: "GenelTarifeler",
                columns: new[] { "Yil", "KiraciKategoriId", "BorcTipiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_Tipi_Kod",
                table: "Kategoriler",
                columns: new[] { "Tipi", "Kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_KiraciKategoriId",
                table: "Kiraciler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_KiraciNo",
                table: "Kiraciler",
                column: "KiraciNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_SektorId",
                table: "Kiraciler",
                column: "SektorId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_GirenUserId",
                table: "KiraOdemeler",
                column: "GirenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_KiraSozlesmesiId",
                table: "KiraOdemeler",
                column: "KiraSozlesmesiId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_KiraTahakkukId",
                table: "KiraOdemeler",
                column: "KiraTahakkukId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraOdemeler_OnaylayanUserId",
                table: "KiraOdemeler",
                column: "OnaylayanUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KiraTahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "KiraTahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri",
                column: "BankaHareketiId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_EslestirenUserId",
                table: "OdemeBankaEslesmeleri",
                column: "EslestirenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_KiraOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "KiraOdemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_BirimId_BaslangicTarihi",
                table: "Rezervasyonlari",
                columns: new[] { "BirimId", "BaslangicTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_KiraciId",
                table: "Rezervasyonlari",
                column: "KiraciId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_KiraSozlesmesiId",
                table: "Rezervasyonlari",
                column: "KiraSozlesmesiId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_KiraTahakkukId",
                table: "Rezervasyonlari",
                column: "KiraTahakkukId");

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonTarifeler_BirimId",
                table: "RezervasyonTarifeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_RezervasyonTarifeler_BirimTuruId_Yil",
                table: "RezervasyonTarifeler",
                columns: new[] { "BirimTuruId", "Yil" },
                unique: true,
                filter: "[BirimId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeIslemGecmisleri_KiraSozlesmesiId",
                table: "SozlesmeIslemGecmisleri",
                column: "KiraSozlesmesiId");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_KiraciId",
                table: "Sozlesmeler",
                column: "KiraciId");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeTarifeler_BorcTipiId",
                table: "SozlesmeTarifeler",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeTarifeler_KiraSozlesmesiId_BorcTipiId",
                table: "SozlesmeTarifeler",
                columns: new[] { "KiraSozlesmesiId", "BorcTipiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukKalemleri_BorcTipiId",
                table: "TahakkukKalemleri",
                column: "BorcTipiId");

            migrationBuilder.CreateIndex(
                name: "IX_TahakkukKalemleri_TahakkukId",
                table: "TahakkukKalemleri",
                column: "TahakkukId");

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
                name: "IX_TasinmazTarifeler_TasinmazId_KiraciKategoriId_BorcTipiId",
                table: "TasinmazTarifeler",
                columns: new[] { "TasinmazId", "KiraciKategoriId", "BorcTipiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_Permission",
                table: "UserPermissions",
                columns: new[] { "UserId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTasinmazYetkileri_TasinmazId",
                table: "UserTasinmazYetkileri",
                column: "TasinmazId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTasinmazYetkileri_UserId_TasinmazId",
                table: "UserTasinmazYetkileri",
                columns: new[] { "UserId", "TasinmazId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BirimTarifeler");

            migrationBuilder.DropTable(
                name: "Dekontlar");

            migrationBuilder.DropTable(
                name: "EnumDegerleri");

            migrationBuilder.DropTable(
                name: "GenelTarifeler");

            migrationBuilder.DropTable(
                name: "OdemeBankaEslesmeleri");

            migrationBuilder.DropTable(
                name: "Rezervasyonlari");

            migrationBuilder.DropTable(
                name: "RezervasyonTarifeler");

            migrationBuilder.DropTable(
                name: "SozlesmeIslemGecmisleri");

            migrationBuilder.DropTable(
                name: "SozlesmeTarifeler");

            migrationBuilder.DropTable(
                name: "TahakkukKalemleri");

            migrationBuilder.DropTable(
                name: "TasinmazTarifeler");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserTasinmazYetkileri");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BankaHareketleri");

            migrationBuilder.DropTable(
                name: "KiraOdemeler");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "KiraTahakkuklar");

            migrationBuilder.DropTable(
                name: "Sozlesmeler");

            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropTable(
                name: "Kiraciler");

            migrationBuilder.DropTable(
                name: "BirimTurleri");

            migrationBuilder.DropTable(
                name: "Tasinmazlar");

            migrationBuilder.DropTable(
                name: "BorcTipleri");

            migrationBuilder.DropTable(
                name: "Kategoriler");
        }
    }
}
