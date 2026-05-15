using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class TasinmazTipiKiralamaSekli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TasinmazTipiKiralamaSekilleri");
        }
    }
}
