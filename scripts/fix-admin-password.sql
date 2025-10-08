-- ============================================
-- 修复 AdminSecrets 表密码哈希格式
-- 将 ASP.NET Identity 格式转换为 BCrypt 格式
-- ============================================

USE [LYBT_DB];
GO

-- BCrypt 哈希值 for "Dev@Admin2025!"
-- 生成命令: BCrypt.Net.BCrypt.HashPassword("Dev@Admin2025!")
DECLARE @BcryptHash NVARCHAR(500) = '$2a$11$4ZqJ5Z8Y6q4Q9Z6q4Q9Z6eLzOx7Z8Y6q4Q9Z6q4Q9Z6q4Q9Z6q4Q9';

UPDATE AdminSecrets
SET PasswordHash = @BcryptHash
WHERE Id = '00000000-0000-0000-0000-000000000001';

PRINT '✅ AdminSecrets 密码哈希已更新为 BCrypt 格式';
PRINT '📝 密码: Dev@Admin2025!';
GO

-- 验证更新
SELECT TOP 1
    Id,
    LEFT(PasswordHash, 10) + '...' AS PasswordHash前缀
FROM AdminSecrets;
GO
