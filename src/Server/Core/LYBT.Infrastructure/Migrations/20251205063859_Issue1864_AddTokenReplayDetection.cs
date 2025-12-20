using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Issue1864_AddTokenReplayDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加 RefreshTokens 表的新列
            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            // 使用条件创建索引（如果不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_CorrelationId' AND object_id = OBJECT_ID('SystemLogs'))
                CREATE INDEX [IX_SystemLogs_CorrelationId] ON [SystemLogs] ([CorrelationId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_Level' AND object_id = OBJECT_ID('SystemLogs'))
                CREATE INDEX [IX_SystemLogs_Level] ON [SystemLogs] ([Level]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_Timestamp' AND object_id = OBJECT_ID('SystemLogs'))
                CREATE INDEX [IX_SystemLogs_Timestamp] ON [SystemLogs] ([Timestamp]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_UserId' AND object_id = OBJECT_ID('SystemLogs'))
                CREATE INDEX [IX_SystemLogs_UserId] ON [SystemLogs] ([UserId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_FamilyId' AND object_id = OBJECT_ID('RefreshTokens'))
                CREATE INDEX [IX_RefreshTokens_FamilyId] ON [RefreshTokens] ([FamilyId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 使用条件删除索引（如果存在）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_CorrelationId' AND object_id = OBJECT_ID('SystemLogs'))
                DROP INDEX [IX_SystemLogs_CorrelationId] ON [SystemLogs];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_Level' AND object_id = OBJECT_ID('SystemLogs'))
                DROP INDEX [IX_SystemLogs_Level] ON [SystemLogs];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_Timestamp' AND object_id = OBJECT_ID('SystemLogs'))
                DROP INDEX [IX_SystemLogs_Timestamp] ON [SystemLogs];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemLogs_UserId' AND object_id = OBJECT_ID('SystemLogs'))
                DROP INDEX [IX_SystemLogs_UserId] ON [SystemLogs];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_FamilyId' AND object_id = OBJECT_ID('RefreshTokens'))
                DROP INDEX [IX_RefreshTokens_FamilyId] ON [RefreshTokens];
            ");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "RefreshTokens");
        }
    }
}
