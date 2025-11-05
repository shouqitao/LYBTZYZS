-- Issue #1838: 修复超级管理员UserId
-- 删除旧的sysadmin记录，插入正确ID的新记录

SET QUOTED_IDENTIFIER ON;
GO

-- 删除旧的sysadmin用户记录
DELETE FROM Users WHERE UserName = 'sysadmin';
GO

-- 插入正确ID的超级管理员占位符记录
INSERT INTO Users (
    Id,
    UserName,
    RealName,
    Role,
    Status,
    PasswordHash,
    FailedLoginCount,
    CreatedAt,
    IsDeleted,
    Remark
)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'sysadmin',
    N'系统超级管理员',
    2,
    1,
    '*** PLACEHOLDER ***',
    0,
    GETUTCDATE(),
    0,
    N'RefreshToken FK约束占位符 - 实际认证存储在AdminSecrets表'
);
GO
