-- 修复 Herbs.UpdatedAt 列为可空（与代码定义一致）
-- 如果列当前是 NOT NULL，这个脚本会将其改为允许 NULL

USE LYBTDB;
GO

-- 检查当前列定义
SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Herbs'
  AND COLUMN_NAME = 'UpdatedAt';
GO

-- 修改 UpdatedAt 列为可空
ALTER TABLE Herbs
ALTER COLUMN UpdatedAt datetime2 NULL;
GO

-- 验证修改结果
SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Herbs'
  AND COLUMN_NAME = 'UpdatedAt';
GO

PRINT '修复完成：Herbs.UpdatedAt 现在允许 NULL 值';
