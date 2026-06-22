using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class Phase16A_TemelAltyapi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KiraciId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "Roller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Scope = table.Column<int>(type: "int", nullable: false, comment: "Internal=1, Kiraci=2"),
                    KiraciId = table.Column<int>(type: "int", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roller_Kiraciler_KiraciId",
                        column: x => x.KiraciId,
                        principalTable: "Kiraciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolPermissions_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    AtanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtayanUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoller_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_TcKimlikNo",
                table: "Kiraciler",
                column: "TcKimlikNo",
                unique: true,
                filter: "[TcKimlikNo] IS NOT NULL AND [TcKimlikNo] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_VergiNo",
                table: "Kiraciler",
                column: "VergiNo",
                unique: true,
                filter: "[VergiNo] IS NOT NULL AND [VergiNo] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_KiraciId",
                table: "AspNetUsers",
                column: "KiraciId");

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
                name: "IX_RolPermissions_RolId_Permission",
                table: "RolPermissions",
                columns: new[] { "RolId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoller_RolId",
                table: "UserRoller",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoller_UserId_RolId",
                table: "UserRoller",
                columns: new[] { "UserId", "RolId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Kiraciler_KiraciId",
                table: "AspNetUsers",
                column: "KiraciId",
                principalTable: "Kiraciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Veri geçişi: UserType = Internal (1) olarak işaretle
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [UserType] = 1");

            // Veri geçişi: PermissionCatalog Internal.* rename
            migrationBuilder.Sql(@"
UPDATE [UserPermissions]
SET [Permission] = 'Internal.' + [Permission]
WHERE [Permission] NOT LIKE 'Internal.%'
  AND [Permission] NOT LIKE 'Kiraci.%'");

            // Veri geçişi: Sistem rollerini Roller tablosuna ekle
            migrationBuilder.Sql(@"
INSERT INTO [Roller] ([Ad], [Aciklama], [Scope], [KiraciId], [IsSystemRole], [CreatedAt], [CreatedBy], [IsDeleted], [IsActive])
VALUES
  ('Admin',          'Sistem yöneticisi',   1, NULL, 1, GETUTCDATE(), 'migration', 0, 1),
  ('Yonetici',       'Yönetici kullanıcı',  1, NULL, 1, GETUTCDATE(), 'migration', 0, 1),
  ('Goruntuleyici',  'Salt okunur erişim',  1, NULL, 1, GETUTCDATE(), 'migration', 0, 1)");

            // Veri geçişi: AspNetUserRoles → UserRoller
            migrationBuilder.Sql(@"
INSERT INTO [UserRoller] ([UserId], [RolId], [AtanmaTarihi], [AtayanUserId])
SELECT ur.[UserId], r.[Id], GETUTCDATE(), NULL
FROM [AspNetUserRoles] ur
JOIN [AspNetRoles] ar ON ar.[Id] = ur.[RoleId]
JOIN [Roller] r ON r.[Ad] = ar.[Name]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Kiraciler_KiraciId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "RolPermissions");

            migrationBuilder.DropTable(
                name: "UserRoller");

            migrationBuilder.DropTable(
                name: "Roller");

            migrationBuilder.DropIndex(
                name: "IX_Kiraciler_TcKimlikNo",
                table: "Kiraciler");

            migrationBuilder.DropIndex(
                name: "IX_Kiraciler_VergiNo",
                table: "Kiraciler");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_KiraciId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "KiraciId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AspNetUsers");
        }
    }
}
