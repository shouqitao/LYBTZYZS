-- 查询AdminSecrets表中的密码哈希
-- Issue #1908: 诊断sysadmin无法登录问题

USE LYBTDB;
GO

SELECT
    Id,
    PasswordHash,
    LEN(PasswordHash) AS HashLength,
    SUBSTRING(PasswordHash, 1, 20) AS HashPrefix
FROM AdminSecrets;

-- 期望的正确哈希（来自AdminSecretConfiguration.cs）
-- $2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C
