-- Issue #1838: 为超级管理员创建Users表占位符记录
-- 超级管理员的实际认证存储在AdminSecrets表，此记录仅用于满足RefreshTokens表的FK约束

SET QUOTED_IDENTIFIER ON;
GO

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
    '00000000-0000-0000-0000-000000000001', -- 固定ID，对应AdminSecrets表
    'sysadmin',
    '系统超级管理员',
    2, -- Admin role
    1, -- Active status
    '*** PLACEHOLDER - 实际密码存储在AdminSecrets表 ***', -- 占位符，永远不会被使用
    0, -- 失败登录次数
    GETUTCDATE(),
    0, -- 未删除
    'RefreshToken FK约束占位符 - 实际认证数据存储在AdminSecrets表'
);
GO
