using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccountLogAndLockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentLockType",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockViolationType",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByAdminId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserAccountLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LockType = table.Column<int>(type: "int", nullable: true),
                    ViolationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccountLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccountLogs_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccountLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurrentLockType", "LockExpiresAt", "LockReason", "LockViolationType", "LockedAt", "LockedByAdminId" },
                values: new object[] { null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CurrentLockType", "LockExpiresAt", "LockReason", "LockViolationType", "LockedAt", "LockedByAdminId" },
                values: new object[] { null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LockedByAdminId",
                table: "Users",
                column: "LockedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountLogs_AdminId",
                table: "UserAccountLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountLogs_CreatedAt",
                table: "UserAccountLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountLogs_UserId",
                table: "UserAccountLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_LockedByAdminId",
                table: "Users",
                column: "LockedByAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_LockedByAdminId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "UserAccountLogs");

            migrationBuilder.DropIndex(
                name: "IX_Users_LockedByAdminId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentLockType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockViolationType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedByAdminId",
                table: "Users");
        }
    }
}
