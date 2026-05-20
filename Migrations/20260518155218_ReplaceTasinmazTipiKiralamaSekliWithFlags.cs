using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTasinmazTipiKiralamaSekliWithFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BirimBazliDestekli",
                table: "TasinmazTipleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TekParcaDestekli",
                table: "TasinmazTipleri",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE tt
                SET tt.TekParcaDestekli = CASE WHEN ks.HasTekParca = 1 THEN 1 ELSE 0 END,
                    tt.BirimBazliDestekli = CASE WHEN ks.HasBirimBazli = 1 THEN 1 ELSE 0 END
                FROM TasinmazTipleri tt
                LEFT JOIN (
                    SELECT TasinmazTipiId,
                           MAX(CASE WHEN KiralamaSekli = 1 THEN 1 ELSE 0 END) AS HasTekParca,
                           MAX(CASE WHEN KiralamaSekli = 2 THEN 1 ELSE 0 END) AS HasBirimBazli
                    FROM TasinmazTipiKiralamaSekilleri
                    GROUP BY TasinmazTipiId
                ) ks ON ks.TasinmazTipiId = tt.Id;

                -- Tipler hiç kayıt yoksa (yeni eklendi vs) TekParça default true
                UPDATE TasinmazTipleri SET TekParcaDestekli = 1
                WHERE TekParcaDestekli = 0 AND BirimBazliDestekli = 0;
            ");

            migrationBuilder.DropTable(
                name: "TasinmazTipiKiralamaSekilleri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirimBazliDestekli",
                table: "TasinmazTipleri");

            migrationBuilder.DropColumn(
                name: "TekParcaDestekli",
                table: "TasinmazTipleri");

            migrationBuilder.CreateTable(
                name: "TasinmazTipiKiralamaSekilleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasinmazTipiId = table.Column<int>(type: "int", nullable: false),
                    KiralamaSekli = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasinmazTipiKiralamaSekilleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasinmazTipiKiralamaSekilleri_TasinmazTipleri_TasinmazTipiId",
                        column: x => x.TasinmazTipiId,
                        principalTable: "TasinmazTipleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TasinmazTipiKiralamaSekilleri_TasinmazTipiId_KiralamaSekli",
                table: "TasinmazTipiKiralamaSekilleri",
                columns: new[] { "TasinmazTipiId", "KiralamaSekli" },
                unique: true);
        }
    }
}
