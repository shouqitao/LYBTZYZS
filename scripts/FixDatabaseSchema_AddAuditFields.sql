-- 完整的数据库 Schema 修复脚本
-- Issue #835: 为所有实体表添加审计字段以匹配实体模型
-- 包括：IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion

USE LYBTDB;
GO

PRINT '===== 开始数据库 Schema 修复 =====';
PRINT '';

-- 定义要处理的表列表
DECLARE @tables TABLE (TableName NVARCHAR(100));
INSERT INTO @tables VALUES
    ('Users'),
    ('Patients'),
    ('Herbs'),
    ('Formulas'),
    ('FormulaItems'),
    ('Prescriptions'),
    ('PrescriptionItems'),
    ('Consultations'),
    ('MedicalCases'),
    ('Doctors');

DECLARE @tableName NVARCHAR(100);
DECLARE @sql NVARCHAR(MAX);

DECLARE table_cursor CURSOR FOR SELECT TableName FROM @tables;
OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @tableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT '处理表: ' + @tableName;

    -- 检查表是否存在
    IF OBJECT_ID(N'[dbo].[' + @tableName + ']', N'U') IS NULL
    BEGIN
        PRINT '  ⊙ 表不存在，跳过';
        GOTO NextTable;
    END

    -- IsDeleted
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'IsDeleted')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [IsDeleted] bit NOT NULL DEFAULT 0;';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 IsDeleted';
    END

    -- CreatedAt
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'CreatedAt')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [CreatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE();';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 CreatedAt';
    END

    -- CreatedBy
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'CreatedBy')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [CreatedBy] nvarchar(100) NULL;';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 CreatedBy';
    END

    -- UpdatedAt
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'UpdatedAt')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [UpdatedAt] datetime2(7) NULL;';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 UpdatedAt';
    END

    -- UpdatedBy
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'UpdatedBy')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [UpdatedBy] nvarchar(100) NULL;';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 UpdatedBy';
    END

    -- RowVersion
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[' + @tableName + ']') AND name = 'RowVersion')
    BEGIN
        SET @sql = 'ALTER TABLE [' + @tableName + '] ADD [RowVersion] rowversion NOT NULL;';
        EXEC sp_executesql @sql;
        PRINT '  ✓ 已添加 RowVersion';
    END

    PRINT '';

    NextTable:
    FETCH NEXT FROM table_cursor INTO @tableName;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;

PRINT '===== 数据库 Schema 修复完成 =====';
GO
