using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAddressLineWithStructuredFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả địa chỉ cũ vì không thể migrate sang cấu trúc mới
            migrationBuilder.Sql("DELETE FROM Addresses");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "Addresses");

            migrationBuilder.AddColumn<int>(
                name: "DistrictCode",
                table: "Addresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DistrictName",
                table: "Addresses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProvinceCode",
                table: "Addresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceName",
                table: "Addresses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "Addresses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Addresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WardCode",
                table: "Addresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WardName",
                table: "Addresses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictCode",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "DistrictName",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "ProvinceName",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "WardName",
                table: "Addresses");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
