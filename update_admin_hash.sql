-- 更新超级管理员密码哈希
-- 密码: Admin@123456
-- 生成的新哈希: AQAAAAIAAYagAAAAECYSBXrClbsPkKj/yxqkQRn32lat5dQXjonTeic2gL796NUE2yL8Pk/MAjR0Fjo1xQ==

USE LYBTDB;

-- 检查当前AdminSecrets表的状态
SELECT * FROM AdminSecrets WHERE UserName = 'sysadmin';

-- 如果记录不存在，插入新记录
IF NOT EXISTS (SELECT 1 FROM AdminSecrets WHERE UserName = 'sysadmin')
BEGIN
    INSERT INTO AdminSecrets (Id, UserName, PasswordHash)
    VALUES (NEWID(), 'sysadmin', 'AQAAAAIAAYagAAAAECYSBXrClbsPkKj/yxqkQRn32lat5dQXjonTeic2gL796NUE2yL8Pk/MAjR0Fjo1xQ==')
END
ELSE
BEGIN
    -- 如果记录存在，更新哈希
    UPDATE AdminSecrets 
    SET PasswordHash = 'AQAAAAIAAYagAAAAECYSBXrClbsPkKj/yxqkQRn32lat5dQXjonTeic2gL796NUE2yL8Pk/MAjR0Fjo1xQ=='
    WHERE UserName = 'sysadmin'
END

-- 验证更新结果
SELECT * FROM AdminSecrets WHERE UserName = 'sysadmin';