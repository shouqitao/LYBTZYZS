using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserNameFromAdminSecretsAndUpdatePasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 更新超级管理员密码哈希从 Identity PasswordHasher V3 格式到 BCrypt 格式
            //    原密码: LybtAdmin2025@SecurePass!
            //    BCrypt 哈希(预计算): $2a$11$WuzT2cXdtI5/mftA8tvl8elHlSkBgU4rxFOZVgWMtgSoecmgA7Zyq
            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$WuzT2cXdtI5/mftA8tvl8elHlSkBgU4rxFOZVgWMtgSoecmgA7Zyq");

            // 2. 先删除 UserName 列上的索引
            migrationBuilder.DropIndex(
                name: "IX_AdminSecrets_Username",
                table: "AdminSecrets");

            // 3. 删除 AdminSecrets 表的 UserName 列以提高安全性
            //    防止 SQL 注入后暴露超级管理员账户名
            //    用户名将从配置文件读取，不再存储在数据库中
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "AdminSecrets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：重新添加 UserName 列
            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "AdminSecrets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // 恢复 UserName 和原密码哈希
            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "UserName", "PasswordHash" },
                values: new object[] { "sysadmin", "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==" });

            // 重新创建 UserName 列上的索引
            migrationBuilder.CreateIndex(
                name: "IX_AdminSecrets_Username",
                table: "AdminSecrets",
                column: "UserName",
                unique: false);
        }
    }
}
