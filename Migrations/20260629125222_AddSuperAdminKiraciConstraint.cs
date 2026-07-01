using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminKiraciConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationUser_SuperAdmin_NoTenant",
                table: "AspNetUsers",
                sql: "[IsSuperAdmin] = 0 OR [KiraciId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationUser_SuperAdmin_NoTenant",
                table: "AspNetUsers");
        }
    }
}
