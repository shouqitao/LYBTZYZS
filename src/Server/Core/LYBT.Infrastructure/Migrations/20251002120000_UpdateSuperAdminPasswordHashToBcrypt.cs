using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuperAdminPasswordHashToBcrypt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 更新超级管理员密码哈希从 Identity PasswordHasher V3 格式到 BCrypt 格式
            // 原密码: LybtAdmin2025@SecurePass!
            // 使用 BCrypt.Net.BCrypt.HashPassword() 生成
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword("LybtAdmin2025@SecurePass!");

            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: bcryptHash);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚到原来的 Identity PasswordHasher V3 哈希
            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==");
        }
    }
}
