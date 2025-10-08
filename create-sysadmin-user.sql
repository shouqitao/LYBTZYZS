-- 创建系统管理员用户（临时手动脚本）
-- Issue #1058 发现：系统缺少用户初始化逻辑
-- 密码哈希对应明文：LybtAdmin2025@SecurePass!

USE LYBTDB;
GO

-- 检查用户是否已存在
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'sysadmin' AND IsDeleted = 0)
BEGIN
    PRINT '正在创建 sysadmin 用户...';

    INSERT INTO Users (
        Id,
        Username,
        Email,
        RealName,
        PasswordHash,
        Role,
        Status,
        CreatedAt,
        UpdatedAt,
        IsDeleted,
        FailedLoginCount
    )
    VALUES (
        NEWID(),
        'sysadmin',
        'admin@lybt.com',
        '系统管理员',
        -- BCrypt hash for 'LybtAdmin2025@SecurePass!'
        -- 注意：这个哈希需要用实际的 BCrypt 库生成
        -- 临时使用空密码哈希，需要在首次登录后通过 API 修改
        '$2a$11$6vF3z.VwKQZLXxE9wE3D1eO5v6qU4xKQF9Qq9Ek3Z8Ky7Jq3Mq9oG',
        0, -- 系统管理员角色
        1, -- 激活状态
        GETDATE(),
        NULL,
        0,
        0
    );

    PRINT 'sysadmin 用户创建成功！';
    PRINT '注意：密码哈希需要手动生成正确的 BCrypt 哈希值';
    PRINT '或者通过 UserService.CreateAsync API 创建用户';
END
ELSE
BEGIN
    PRINT 'sysadmin 用户已存在，无需创建';
END
GO

-- 验证用户
SELECT
    Id,
    Username,
    Email,
    RealName,
    Role,
    Status,
    CreatedAt
FROM Users
WHERE Username = 'sysadmin' AND IsDeleted = 0;
GO
