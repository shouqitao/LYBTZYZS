-- 更新 AdminSecrets 密码哈希为 BCrypt 格式
USE LYBTDB;
GO

UPDATE AdminSecrets
SET PasswordHash = '$2a$11$SodEimJaRdGDHZ4BEF31c.LSt664I4uAo.uGSN7kz.UXpiVacdqJ.'
WHERE Id = '00000000-0000-0000-0000-000000000001';

PRINT '✅ 密码哈希已更新';
GO

SELECT TOP 1 LEFT(PasswordHash, 20) AS HashPrefix FROM AdminSecrets;
GO
