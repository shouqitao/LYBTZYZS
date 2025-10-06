using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseEntityAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== Herbs 表 - 添加缺失的审计字段 ==========
            // 注：IsDeleted 和 RowVersion 可能已通过其他方式添加，先只处理明确缺失的字段

            // 添加 CreatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Herbs] ADD [CreatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 UpdatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Herbs] ADD [UpdatedBy] uniqueidentifier NULL;
                END
            ");

            // 重命名 UpdateTime → UpdatedAt（如需要）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'UpdateTime')
                BEGIN
                    EXEC sp_rename 'Herbs.UpdateTime', 'UpdatedAt', 'COLUMN';
                END
            ");

            // ========== Patients 表 - 已有 RowVersion (AddRowVersionConcurrencyControl 迁移)，只重命名 UpdateTime ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'UpdateTime')
                BEGIN
                    EXEC sp_rename 'Patients.UpdateTime', 'UpdatedAt', 'COLUMN';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========== Herbs 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Herbs.UpdatedAt', 'UpdateTime', 'COLUMN';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Herbs] DROP COLUMN [UpdatedBy];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Herbs] DROP COLUMN [CreatedBy];
                END
            ");

            // ========== Patients 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Patients.UpdatedAt', 'UpdateTime', 'COLUMN';
                END
            ");
        }
    }
}
