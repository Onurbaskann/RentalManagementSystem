using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseExpirationActivityType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IslemTipi",
                table: "SozlesmeIslemGecmisleri",
                type: "int",
                nullable: false,
                comment: "Creation=1, Extension=2, Termination=3, TufeIncrease=4, KdvUpdate=5, ChargeRegeneration=6, Expiration=7",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Creation=1, Extension=2, Termination=3, TufeIncrease=4, KdvUpdate=5, ChargeRegeneration=6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IslemTipi",
                table: "SozlesmeIslemGecmisleri",
                type: "int",
                nullable: false,
                comment: "Creation=1, Extension=2, Termination=3, TufeIncrease=4, KdvUpdate=5, ChargeRegeneration=6",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Creation=1, Extension=2, Termination=3, TufeIncrease=4, KdvUpdate=5, ChargeRegeneration=6, Expiration=7");
        }
    }
}
