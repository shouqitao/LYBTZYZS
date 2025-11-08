using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Issue #1909: 三角色体系迁移（SuperAdmin + Admin + Doctor）
    /// 将AdminSecrets表中的超级管理员密码迁移到Users表，统一认证流程
    /// </summary>
    /// <inheritdoc />
    public partial class Issue1909_MigrateToThreeRoleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤1: 从AdminSecrets表迁移超级管理员数据到Users表
            // 创建SuperAdmin用户（Role=100），使用AdminSecrets表中的密码哈希
            migrationBuilder.Sql(@"
                INSERT INTO Users (
                    Id,
                    UserName,
                    RealName,
                    Email,
                    Role,
                    Status,
                    PasswordHash,
                    FailedLoginCount,
                    LockoutEnd,
                    CreatedAt,
                    UpdatedAt,
                    IsDeleted
                )
                SELECT
                    '00000000-0000-0000-0000-000000000001' AS Id,
                    'admin' AS UserName,
                    '超级管理员' AS RealName,
                    'admin@lybt.com' AS Email,
                    100 AS Role,  -- SuperAdmin = 100
                    1 AS Status,  -- Enabled = 1
                    PasswordHash,
                    0 AS FailedLoginCount,
                    NULL AS LockoutEnd,
                    GETDATE() AS CreatedAt,
                    GETDATE() AS UpdatedAt,
                    0 AS IsDeleted
                FROM AdminSecrets
                WHERE Id = '00000000-0000-0000-0000-000000000001'
                AND NOT EXISTS (
                    SELECT 1 FROM Users WHERE Id = '00000000-0000-0000-0000-000000000001'
                );
            ");

            // 步骤2: 删除AdminSecrets表（数据已迁移到Users表）
            migrationBuilder.DropTable(
                name: "AdminSecrets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤1: 重新创建AdminSecrets表
            migrationBuilder.CreateTable(
                name: "AdminSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSecrets", x => x.Id);
                });

            // 步骤2: 从Users表迁移SuperAdmin数据回AdminSecrets表
            migrationBuilder.Sql(@"
                INSERT INTO AdminSecrets (Id, PasswordHash)
                SELECT Id, PasswordHash
                FROM Users
                WHERE Id = '00000000-0000-0000-0000-000000000001'
                  AND Role = 100;  -- SuperAdmin
            ");

            // 步骤3: 从Users表删除SuperAdmin用户
            migrationBuilder.Sql(@"
                DELETE FROM Users
                WHERE Id = '00000000-0000-0000-0000-000000000001'
                  AND Role = 100;
            ");
        }
    }
}
