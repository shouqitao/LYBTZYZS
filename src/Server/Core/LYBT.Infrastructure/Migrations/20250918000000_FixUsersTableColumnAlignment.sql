-- 修复Users表列名与长度一致化
-- TECH_DEBT_BACKLOG.md P0-5 修复项

-- 检查当前列名是否为UserName，如果是则重命名为Username
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'UserName')
BEGIN
    -- 重命名列：UserName -> Username
    EXEC sp_rename 'Users.UserName', 'Username', 'COLUMN';
    PRINT '列名已从 UserName 重命名为 Username';
END
ELSE
BEGIN
    PRINT '列名已经是 Username，无需重命名';
END

-- 调整RealName字段长度：从100改为50（与实体定义一致）
ALTER TABLE Users 
ALTER COLUMN RealName NVARCHAR(50) NOT NULL;
PRINT 'RealName字段长度已调整为50';

-- 调整PasswordHash字段长度：从255改为256（与实体定义一致）
ALTER TABLE Users 
ALTER COLUMN PasswordHash NVARCHAR(256) NOT NULL;
PRINT 'PasswordHash字段长度已调整为256';

-- 验证更改
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME IN ('Username', 'RealName', 'PasswordHash')
ORDER BY COLUMN_NAME;

PRINT '✅ Users表列名与长度一致化修复完成';