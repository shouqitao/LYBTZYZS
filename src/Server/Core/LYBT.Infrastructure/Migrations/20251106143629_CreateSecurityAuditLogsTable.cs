using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateSecurityAuditLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Issue #1869: 创建SecurityAuditLogs表
            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                });

            // Issue #1869: 按EventType和时间查询优化索引
            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "EventType", "CreatedAt" },
                descending: new[] { false, true }); // EventType升序，CreatedAt降序

            // Issue #1869: 按UserId和时间查询优化索引
            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true }); // UserId升序，CreatedAt降序
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Issue #1869: 删除索引
            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditLogs_UserId_CreatedAt",
                table: "SecurityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditLogs_EventType_CreatedAt",
                table: "SecurityAuditLogs");

            // Issue #1869: 删除SecurityAuditLogs表
            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");
        }
    }
}
