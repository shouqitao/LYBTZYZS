-- 创建默认系统管理员账户
-- 密码: Admin@123456
-- 使用 ASP.NET Core Identity v3 兼容的密码哈希

DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
DECLARE @PasswordHash NVARCHAR(500) = 'AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==';

-- 检查是否已存在 sysadmin
IF NOT EXISTS (SELECT 1 FROM AdminSecrets WHERE UserName = 'sysadmin')
BEGIN
    INSERT INTO AdminSecrets (Id, UserName, PasswordHash)
    VALUES (@AdminId, 'sysadmin', @PasswordHash);
    
    PRINT '✅ 成功创建 sysadmin 账户';
    PRINT '用户名: sysadmin';
    PRINT '默认密码: Admin@123456';
END
ELSE
BEGIN
    -- 如果存在，更新密码哈希
    UPDATE AdminSecrets 
    SET PasswordHash = @PasswordHash
    WHERE UserName = 'sysadmin';
    
    PRINT '✅ 更新 sysadmin 密码';
    PRINT '用户名: sysadmin';
    PRINT '默认密码: Admin@123456';
END

-- 显示结果
SELECT Id, UserName, LEFT(PasswordHash, 30) + '...' AS PasswordHashPrefix 
FROM AdminSecrets 
WHERE UserName = 'sysadmin';