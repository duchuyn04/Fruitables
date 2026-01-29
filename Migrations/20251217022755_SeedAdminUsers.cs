using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "Password", "Phone", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@fruitables.com", "Admin", true, null, "User", "$2a$11$lA/jMR6h6Qga83lrdc0xd.Fx1TLBOiefaI1vAvCcVTjhYFqTYisHO", null, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "superadmin@fruitables.com", "Super", true, null, "Admin", "$2a$11$lA/jMR6h6Qga83lrdc0xd.Fx1TLBOiefaI1vAvCcVTjhYFqTYisHO", null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
