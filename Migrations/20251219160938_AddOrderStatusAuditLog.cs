using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderStatusAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldOrderStatus = table.Column<int>(type: "int", nullable: false),
                    OldPaymentStatus = table.Column<int>(type: "int", nullable: false),
                    NewOrderStatus = table.Column<int>(type: "int", nullable: false),
                    NewPaymentStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusAuditLogs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditLogId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogAttachments_OrderStatusAuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "OrderStatusAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogAttachments_AuditLogId",
                table: "AuditLogAttachments",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusAuditLogs_AdminId",
                table: "OrderStatusAuditLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusAuditLogs_CreatedAt",
                table: "OrderStatusAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusAuditLogs_OrderId",
                table: "OrderStatusAuditLogs",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogAttachments");

            migrationBuilder.DropTable(
                name: "OrderStatusAuditLogs");
        }
    }
}
