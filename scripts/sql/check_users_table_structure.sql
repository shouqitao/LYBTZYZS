-- 检查Users表结构
USE LYBTDB;
GO

-- 检查Users表是否存在
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    PRINT '=== Users表存在 ===';
    
    -- 显示Users表的列信息
    SELECT 
        COLUMN_NAME as '列名',
        DATA_TYPE as '数据类型',
        IS_NULLABLE as '允许空值',
        CHARACTER_MAXIMUM_LENGTH as '最大长度',
        COLUMN_DEFAULT as '默认值'
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    ORDER BY ORDINAL_POSITION;
    
    -- 检查具体的问题列是否存在
    PRINT '';
    PRINT '=== 检查问题列 ===';
    
    -- 检查Department列
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Department')
        PRINT '✓ Department列存在';
    ELSE
        PRINT '✗ Department列不存在';
    
    -- 检查Position列
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Position')
        PRINT '✓ Position列存在';
    ELSE
        PRINT '✗ Position列不存在';
    
    -- 检查Remark列
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Remark')
        PRINT '✓ Remark列存在';
    ELSE
        PRINT '✗ Remark列不存在';
    
    -- 检查UpdateTime列
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdateTime')
        PRINT '✓ UpdateTime列存在';
    ELSE
        PRINT '✗ UpdateTime列不存在';
        
    -- 检查数据行数
    DECLARE @rowCount INT;
    SELECT @rowCount = COUNT(*) FROM Users;
    PRINT '';
    PRINT '总记录数: ' + CAST(@rowCount AS VARCHAR(10));
    
END
ELSE
BEGIN
    PRINT '❌ Users表不存在';
END

GO