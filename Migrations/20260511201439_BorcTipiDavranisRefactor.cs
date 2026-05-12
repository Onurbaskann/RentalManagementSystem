using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class BorcTipiDavranisRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TOPLANTI: Manuel akıştan ayrı rezervasyon davranışına taşı ve Sistem flag'i koru
            migrationBuilder.Sql("UPDATE BorcTipleri SET Davranis = 4, Sistem = 1 WHERE Kod = 'TOPLANTI'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE BorcTipleri SET Davranis = 3, Sistem = 0 WHERE Kod = 'TOPLANTI'");
        }
    }
}
