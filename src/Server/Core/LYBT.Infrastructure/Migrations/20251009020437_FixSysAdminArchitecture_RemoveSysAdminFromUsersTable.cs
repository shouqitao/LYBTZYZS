using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSysAdminArchitecture_RemoveSysAdminFromUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Issue #1074: 移除错误存储在Users表中的sysadmin
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            // Issue #1074: 更新AdminSecrets表使用正确的BCrypt哈希
            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：恢复旧的Identity格式哈希
            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==");

            // 回滚：重新插入sysadmin到Users表（不推荐，但为了回滚完整性）
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "FailedLoginCount", "IsDeleted", "LastLoginTime", "LockoutEnd", "PasswordHash", "PhoneNumber", "PinYinCode", "RealName", "Remark", "Role", "Status", "UpdatedAt", "UpdatedBy", "UserName" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@lybt.com", 0, false, null, null, "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C", null, null, "系统管理员", null, 10, 1, null, null, "sysadmin" });
        }
    }
}
