using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Issue1861_AddUserTypeToRefreshToken : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserType",
                table: "RefreshTokens");
        }
    }
}
