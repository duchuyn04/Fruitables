using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RbacAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    ChangedByAdminId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RbacAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RbacAuditLogs_Users_ChangedByAdminId",
                        column: x => x.ChangedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_Users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module",
                table: "Permissions",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RbacAuditLogs_ChangedAt",
                table: "RbacAuditLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RbacAuditLogs_ChangedByAdminId",
                table: "RbacAuditLogs",
                column: "ChangedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_RbacAuditLogs_EntityType_EntityId",
                table: "RbacAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_AssignedByAdminId",
                table: "RolePermissions",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_AssignedByAdminId",
                table: "UserRoleMappings",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_RoleId",
                table: "UserRoleMappings",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_UserId",
                table: "UserRoleMappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_UserId_RoleId",
                table: "UserRoleMappings",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            // Seed default roles
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Name", "Description", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Customer", "Khách hàng thông thường", true, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "Admin", "Quản trị viên", true, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "SuperAdmin", "Quản trị viên cấp cao", true, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                });

            // Seed permissions for all modules
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Name", "Description", "Module", "CreatedAt" },
                values: new object[,]
                {
                    // Products module
                    { "products.view", "Xem danh sách và chi tiết sản phẩm", "products", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "products.create", "Tạo sản phẩm mới", "products", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "products.update", "Cập nhật sản phẩm", "products", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "products.delete", "Xóa sản phẩm", "products", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "products.manage_inventory", "Quản lý tồn kho", "products", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // Orders module
                    { "orders.view_all", "Xem tất cả đơn hàng", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "orders.view_own", "Chỉ xem đơn hàng của mình", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "orders.create", "Tạo đơn hàng mới", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "orders.update_status", "Cập nhật trạng thái đơn hàng", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "orders.cancel", "Hủy đơn hàng", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "orders.refund", "Xử lý hoàn tiền", "orders", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // Users module
                    { "users.view", "Xem danh sách và chi tiết người dùng", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "users.create", "Tạo người dùng mới", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "users.update", "Cập nhật thông tin người dùng", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "users.lock", "Khóa tài khoản người dùng", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "users.unlock", "Mở khóa tài khoản người dùng", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "users.delete", "Xóa người dùng", "users", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // Reviews module
                    { "reviews.view", "Xem tất cả đánh giá", "reviews", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "reviews.create", "Tạo đánh giá", "reviews", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "reviews.moderate", "Kiểm duyệt đánh giá", "reviews", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "reviews.delete", "Xóa đánh giá", "reviews", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // Settings module
                    { "settings.view", "Xem cài đặt hệ thống", "settings", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "settings.update", "Cập nhật cài đặt hệ thống", "settings", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // Dashboard module
                    { "dashboard.view", "Xem dashboard quản trị", "dashboard", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "dashboard.view_statistics", "Xem thống kê chi tiết", "dashboard", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    
                    // System module
                    { "system.manage", "Quản lý toàn bộ hệ thống", "system", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "system.view_logs", "Xem nhật ký hệ thống", "system", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "system.manage_rbac", "Quản lý vai trò và quyền hạn", "system", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                });

            // Assign permissions to Customer role (RoleId = 1)
            migrationBuilder.Sql(@"
                INSERT INTO RolePermissions (RoleId, PermissionId, AssignedAt)
                SELECT 1, Id, GETUTCDATE()
                FROM Permissions
                WHERE Name IN ('products.view', 'orders.view_own', 'orders.create', 'reviews.create')
            ");

            // Assign permissions to Admin role (RoleId = 2) - all except system.manage
            migrationBuilder.Sql(@"
                INSERT INTO RolePermissions (RoleId, PermissionId, AssignedAt)
                SELECT 2, Id, GETUTCDATE()
                FROM Permissions
                WHERE Name != 'system.manage'
            ");

            // Assign all permissions to SuperAdmin role (RoleId = 3)
            migrationBuilder.Sql(@"
                INSERT INTO RolePermissions (RoleId, PermissionId, AssignedAt)
                SELECT 3, Id, GETUTCDATE()
                FROM Permissions
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RbacAuditLogs");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoleMappings");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
