using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class UserRolVePermissionAuditDuzenleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrantedAt",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "GrantedBy",
                table: "UserPermissions");

            migrationBuilder.RenameColumn(
                name: "AtayanUserId",
                table: "UserRoller",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "AtanmaTarihi",
                table: "UserRoller",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserRoller",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserRoller",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRoller",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserRoller",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserRoller");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserRoller");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoller");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserRoller");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "UserRoller",
                newName: "AtayanUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "UserRoller",
                newName: "AtanmaTarihi");

            migrationBuilder.AddColumn<DateTime>(
                name: "GrantedAt",
                table: "UserPermissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GrantedBy",
                table: "UserPermissions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
