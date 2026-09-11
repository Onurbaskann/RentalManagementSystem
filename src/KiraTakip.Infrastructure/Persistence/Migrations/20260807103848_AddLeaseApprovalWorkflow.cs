using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                comment: "Active=1, Ended=2, Terminated=3, Draft=4, RevisionRequested=5",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Active=1, Ended=2, Terminated=3");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sozlesmeler",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "SozlesmeIncelemeGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SozlesmeId = table.Column<int>(type: "int", nullable: false),
                    IslemTipi = table.Column<int>(type: "int", nullable: false, comment: "DraftCreated=1, DraftUpdated=2, RevisionRequested=3, Resubmitted=4, Approved=5, Deleted=6"),
                    OncekiDurum = table.Column<int>(type: "int", nullable: true, comment: "Active=1, Ended=2, Terminated=3, Draft=4, RevisionRequested=5"),
                    YeniDurum = table.Column<int>(type: "int", nullable: true, comment: "Active=1, Ended=2, Terminated=3, Draft=4, RevisionRequested=5"),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IslemYapanKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozlesmeIncelemeGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozlesmeIncelemeGecmisleri_AspNetUsers_IslemYapanKullaniciId",
                        column: x => x.IslemYapanKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SozlesmeIncelemeGecmisleri_Sozlesmeler_SozlesmeId",
                        column: x => x.SozlesmeId,
                        principalTable: "Sozlesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Sozlesmeler_BirimId_AcikBasvuru",
                table: "Sozlesmeler",
                column: "BirimId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Durum] IN (4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId",
                table: "SozlesmeIncelemeGecmisleri",
                column: "IslemYapanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi",
                table: "SozlesmeIncelemeGecmisleri",
                columns: new[] { "SozlesmeId", "IslemTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SozlesmeIncelemeGecmisleri");

            migrationBuilder.DropIndex(
                name: "UX_Sozlesmeler_BirimId_AcikBasvuru",
                table: "Sozlesmeler");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sozlesmeler");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Sozlesmeler",
                type: "int",
                nullable: false,
                comment: "Active=1, Ended=2, Terminated=3",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Active=1, Ended=2, Terminated=3, Draft=4, RevisionRequested=5");
        }
    }
}
