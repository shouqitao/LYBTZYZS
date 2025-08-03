-- 应用用户业务字段迁移
USE LYBTDB;
GO

-- 添加用户业务字段
ALTER TABLE Users ADD Department NVARCHAR(100) NULL;
ALTER TABLE Users ADD Position NVARCHAR(100) NULL;
ALTER TABLE Users ADD Remark NVARCHAR(500) NULL;
ALTER TABLE Users ADD UpdateTime DATETIME2 NULL;

-- 更新迁移历史表
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20250802120000_AddUserBusinessFields', '8.0.6');

PRINT '用户业务字段迁移已应用';

-- 验证字段
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME IN ('Department', 'Position', 'Remark', 'UpdateTime')
ORDER BY COLUMN_NAME;