using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteBaseEntityAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== Users 表 - 系统核心表，高优先级 ==========

            // 添加 CreatedAt（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedAt')
                BEGIN
                    ALTER TABLE [Users] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
                END
            ");

            // 添加 CreatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Users] ADD [CreatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 UpdatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Users] ADD [UpdatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 IsDeleted（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                END
            ");

            // 重命名 UpdateTime → UpdatedAt（如需要且UpdatedAt不存在）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdateTime')
                AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Users.UpdateTime', 'UpdatedAt', 'COLUMN';
                END
            ");

            // ========== Prescriptions 表 - 处方核心表，高优先级 ==========

            // 添加 CreatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Prescriptions] ADD [CreatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 UpdatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Prescriptions] ADD [UpdatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 IsDeleted（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Prescriptions] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                END
            ");

            // 重命名 CreateTime → CreatedAt（如需要且CreatedAt不存在）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'CreateTime')
                AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'CreatedAt')
                BEGIN
                    EXEC sp_rename 'Prescriptions.CreateTime', 'CreatedAt', 'COLUMN';
                END
            ");

            // 重命名 UpdateTime → UpdatedAt（如需要且UpdatedAt不存在）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'UpdateTime')
                AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Prescriptions.UpdateTime', 'UpdatedAt', 'COLUMN';
                END
            ");

            // ========== Formulas 表 - 方剂管理，中优先级 ==========

            // 添加 UpdatedBy（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Formulas] ADD [UpdatedBy] uniqueidentifier NULL;
                END
            ");

            // 添加 RowVersion（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'RowVersion')
                BEGIN
                    ALTER TABLE [Formulas] ADD [RowVersion] rowversion NOT NULL;
                END
            ");

            // 添加 IsDeleted（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Formulas] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                END
            ");

            // 重命名 CreatedById → CreatedBy（如需要）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'CreatedById')
                AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'CreatedBy')
                BEGIN
                    EXEC sp_rename 'Formulas.CreatedById', 'CreatedBy', 'COLUMN';
                END
            ");

            // ========== Patients 表 - 患者管理 ==========

            // 添加 IsDeleted（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Patients] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                END
            ");

            // ========== Herbs 表 - 药材管理 ==========

            // 添加 RowVersion（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'RowVersion')
                BEGIN
                    ALTER TABLE [Herbs] ADD [RowVersion] rowversion NOT NULL;
                END
            ");

            // 添加 IsDeleted（如不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Herbs] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========== Users 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Users.UpdatedAt', 'UpdateTime', 'COLUMN';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [IsDeleted];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [UpdatedBy];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [CreatedBy];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedAt')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [CreatedAt];
                END
            ");

            // ========== Prescriptions 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'UpdatedAt')
                BEGIN
                    EXEC sp_rename 'Prescriptions.UpdatedAt', 'UpdateTime', 'COLUMN';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'CreatedAt')
                BEGIN
                    EXEC sp_rename 'Prescriptions.CreatedAt', 'CreateTime', 'COLUMN';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Prescriptions] DROP COLUMN [IsDeleted];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Prescriptions] DROP COLUMN [UpdatedBy];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'CreatedBy')
                BEGIN
                    ALTER TABLE [Prescriptions] DROP COLUMN [CreatedBy];
                END
            ");

            // ========== Formulas 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'CreatedBy')
                AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'CreatedById')
                BEGIN
                    EXEC sp_rename 'Formulas.CreatedBy', 'CreatedById', 'COLUMN';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Formulas] DROP COLUMN [IsDeleted];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'RowVersion')
                BEGIN
                    ALTER TABLE [Formulas] DROP COLUMN [RowVersion];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'UpdatedBy')
                BEGIN
                    ALTER TABLE [Formulas] DROP COLUMN [UpdatedBy];
                END
            ");

            // ========== Patients 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Patients] DROP COLUMN [IsDeleted];
                END
            ");

            // ========== Herbs 表回滚 ==========
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'IsDeleted')
                BEGIN
                    ALTER TABLE [Herbs] DROP COLUMN [IsDeleted];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'RowVersion')
                BEGIN
                    ALTER TABLE [Herbs] DROP COLUMN [RowVersion];
                END
            ");
        }
    }
}
