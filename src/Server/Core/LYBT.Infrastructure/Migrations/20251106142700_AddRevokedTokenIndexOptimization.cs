using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevokedTokenIndexOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Issue #1861: 添加UserType列以区分SuperAdmin和User的RefreshToken
            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "RefreshTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "user"); // 默认为user类型，确保现有数据兼容

            // Issue #1868: 创建RefreshTokens撤销优化索引（覆盖索引）
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked_Token",
                table: "RefreshTokens",
                columns: new[] { "IsRevoked", "Token" })
                .Annotation("SqlServer:Include", new[] { "UserId", "UserType", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Issue #1868: 回滚撤销优化索引
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_IsRevoked_Token",
                table: "RefreshTokens");

            // Issue #1861: 删除UserType列
            migrationBuilder.DropColumn(
                name: "UserType",
                table: "RefreshTokens");
        }
    }
}
