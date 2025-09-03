-- 添加用户业务字段到Users表
USE LYBTDB;
GO

PRINT '=== 添加用户业务字段 ===';

-- 添加Department列（如果不存在）
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Department')
BEGIN
    ALTER TABLE Users ADD Department NVARCHAR(100) NULL;
    PRINT '✓ 已添加Department列';
END
ELSE
    PRINT '• Department列已存在';

-- 添加Position列（如果不存在）
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Position')
BEGIN
    ALTER TABLE Users ADD Position NVARCHAR(100) NULL;
    PRINT '✓ 已添加Position列';
END
ELSE
    PRINT '• Position列已存在';

-- 添加Remark列（如果不存在）
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Remark')
BEGIN
    ALTER TABLE Users ADD Remark NVARCHAR(500) NULL;
    PRINT '✓ 已添加Remark列';
END
ELSE
    PRINT '• Remark列已存在';

-- 添加UpdateTime列（如果不存在）
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdateTime')
BEGIN
    ALTER TABLE Users ADD UpdateTime DATETIME2 NULL;
    PRINT '✓ 已添加UpdateTime列';
END
ELSE
    PRINT '• UpdateTime列已存在';

-- 验证添加结果
PRINT '';
PRINT '=== 验证结果 ===';
SELECT 
    COLUMN_NAME as '列名',
    DATA_TYPE as '数据类型',
    CHARACTER_MAXIMUM_LENGTH as '最大长度',
    IS_NULLABLE as '允许空值'
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME IN ('Department', 'Position', 'Remark', 'UpdateTime')
ORDER BY COLUMN_NAME;

PRINT '';
PRINT '✓ 用户业务字段添加完成';
GO