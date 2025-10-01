-- 为 Users 表添加审计字段
-- Issue #835: 修复数据库 schema 与实体模型不一致问题

USE LYBTDB;
GO

-- 检查并添加 IsDeleted 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IsDeleted')
BEGIN
    ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
    PRINT '✓ 已添加 IsDeleted 列';
END
ELSE
    PRINT '⊙ IsDeleted 列已存在';

-- 检查并添加 CreatedAt 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [Users] ADD [CreatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE();
    PRINT '✓ 已添加 CreatedAt 列';
END
ELSE
    PRINT '⊙ CreatedAt 列已存在';

-- 检查并添加 CreatedBy 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [Users] ADD [CreatedBy] nvarchar(100) NULL;
    PRINT '✓ 已添加 CreatedBy 列';
END
ELSE
    PRINT '⊙ CreatedBy 列已存在';

-- 检查并添加 UpdatedAt 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [Users] ADD [UpdatedAt] datetime2(7) NULL;
    PRINT '✓ 已添加 UpdatedAt 列';
END
ELSE
    PRINT '⊙ UpdatedAt 列已存在';

-- 检查并添加 UpdatedBy 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'UpdatedBy')
BEGIN
    ALTER TABLE [Users] ADD [UpdatedBy] nvarchar(100) NULL;
    PRINT '✓ 已添加 UpdatedBy 列';
END
ELSE
    PRINT '⊙ UpdatedBy 列已存在';

-- 检查并添加 RowVersion 列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'RowVersion')
BEGIN
    ALTER TABLE [Users] ADD [RowVersion] rowversion NOT NULL;
    PRINT '✓ 已添加 RowVersion 列';
END
ELSE
    PRINT '⊙ RowVersion 列已存在';

PRINT '';
PRINT '===== Users 表审计字段添加完成 =====';
GO
