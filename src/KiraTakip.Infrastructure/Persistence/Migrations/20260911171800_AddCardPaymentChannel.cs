using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPaymentChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OdemeKanali",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: false,
                comment: "BankTransfer=1, Eft=2, Cash=3, Other=4, Card=5",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "BankTransfer=1, Eft=2, Cash=3, Other=4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OdemeKanali",
                table: "TahakkukOdemeleri",
                type: "int",
                nullable: false,
                comment: "BankTransfer=1, Eft=2, Cash=3, Other=4",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "BankTransfer=1, Eft=2, Cash=3, Other=4, Card=5");
        }
    }
}
