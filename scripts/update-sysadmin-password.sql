-- 更新sysadmin密码为正确的BCrypt哈希
-- 密码: LybtAdmin2025@SecurePass!
-- BCrypt哈希: $2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C

SET QUOTED_IDENTIFIER ON;
GO

UPDATE Users
SET PasswordHash = '$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C',
    UpdatedAt = GETUTCDATE()
WHERE Username = 'sysadmin';

SELECT COUNT(*) AS UpdatedRows FROM Users WHERE Username = 'sysadmin';
GO