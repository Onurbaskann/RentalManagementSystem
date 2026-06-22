using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGercekKiraciAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kiraciler_TcKimlikNo",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "AnneAdi",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "BabaAdi",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "DogumTarihi",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "DogumYeri",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "KiraciTuru",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "PasaportNo",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "Soyad",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "Unvan",
                table: "Kiraciler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnneAdi",
                table: "Kiraciler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BabaAdi",
                table: "Kiraciler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DogumTarihi",
                table: "Kiraciler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DogumYeri",
                table: "Kiraciler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KiraciTuru",
                table: "Kiraciler",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Gercek=1, Tuzel=2");

            migrationBuilder.AddColumn<string>(
                name: "PasaportNo",
                table: "Kiraciler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Soyad",
                table: "Kiraciler",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "Kiraciler",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unvan",
                table: "Kiraciler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_TcKimlikNo",
                table: "Kiraciler",
                column: "TcKimlikNo",
                unique: true,
                filter: "[TcKimlikNo] IS NOT NULL AND [TcKimlikNo] <> ''");
        }
    }
}
