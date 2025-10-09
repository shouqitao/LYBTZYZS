-- =============================================
-- MVP 初始化数据脚本
-- 版本：v1.0.0-mvp
-- 创建时间：2025-01-09
-- 说明：MVP发布所需的基础数据
-- =============================================

USE [LYBTDB];
GO

-- =============================================
-- 1. 创建默认管理员账号
-- =============================================
PRINT '正在创建默认管理员账号...';

-- 检查是否已存在管理员
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'sysadmin')
BEGIN
    -- 密码: LybtAdmin2025@SecurePass!
    -- 使用BCrypt加密后的密码
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        FullName, 
        Email, 
        PhoneNumber,
        Role, 
        IsActive, 
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginAttempts,
        LockoutEnd
    ) VALUES (
        NEWID(),
        'sysadmin',
        '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu', -- BCrypt hash of 'LybtAdmin2025@SecurePass!'
        '系统管理员',
        'admin@lybt.com',
        '13800138000',
        0, -- Role.Admin = 0
        1, -- IsActive = true
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL
    );
    PRINT '✓ 默认管理员账号创建成功 (用户名: sysadmin)';
END
ELSE
BEGIN
    PRINT '→ 管理员账号已存在，跳过创建';
END
GO

-- =============================================
-- 2. 创建示例医生账号
-- =============================================
PRINT '正在创建示例医生账号...';

-- 示例医生1
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'doctor001')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        FullName, 
        Email, 
        PhoneNumber,
        Role, 
        IsActive, 
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginAttempts,
        LockoutEnd
    ) VALUES (
        NEWID(),
        'doctor001',
        '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO', -- BCrypt hash of 'Doctor2025@Pass!'
        '张医生',
        'zhangys@lybt.com',
        '13900139001',
        1, -- Role.Doctor = 1
        1,
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL
    );
    PRINT '✓ 示例医生账号1创建成功 (用户名: doctor001)';
END

-- 示例医生2
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'doctor002')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        FullName, 
        Email, 
        PhoneNumber,
        Role, 
        IsActive, 
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginAttempts,
        LockoutEnd
    ) VALUES (
        NEWID(),
        'doctor002',
        '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO', -- BCrypt hash of 'Doctor2025@Pass!'
        '李医生',
        'liys@lybt.com',
        '13900139002',
        1, -- Role.Doctor = 1
        1,
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL
    );
    PRINT '✓ 示例医生账号2创建成功 (用户名: doctor002)';
END
GO

-- =============================================
-- 3. 创建诊所基础信息配置
-- =============================================
PRINT '正在创建诊所基础信息...';

-- 创建配置表（如果不存在）
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemConfigurations')
BEGIN
    CREATE TABLE SystemConfigurations (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
        ConfigValue NVARCHAR(MAX) NOT NULL,
        Description NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
    PRINT '✓ 系统配置表创建成功';
END

-- 插入诊所基础信息
IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'Clinic.Name')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('Clinic.Name', '洛阳白天鹅中医诊所', '诊所名称');
END

IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'Clinic.Address')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('Clinic.Address', '河南省洛阳市洛龙区', '诊所地址');
END

IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'Clinic.Phone')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('Clinic.Phone', '0379-60000000', '诊所电话');
END

IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'Clinic.License')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('Clinic.License', 'PDY00000000000000', '医疗机构执业许可证号');
END

IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'System.Version')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('System.Version', 'v1.0.0-mvp', '系统版本号');
END

IF NOT EXISTS (SELECT 1 FROM SystemConfigurations WHERE ConfigKey = 'Prescription.PrintFormat')
BEGIN
    INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, Description)
    VALUES ('Prescription.PrintFormat', 'A5_Landscape', '处方打印格式：A5横向');
END

PRINT '✓ 诊所基础信息配置完成';
GO

-- =============================================
-- 4. 创建审计日志表（如果不存在）
-- =============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NULL,
        UserName NVARCHAR(100) NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NULL,
        EntityId NVARCHAR(100) NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        Timestamp DATETIME2 DEFAULT GETDATE(),
        Result NVARCHAR(50) NULL,
        ErrorMessage NVARCHAR(MAX) NULL
    );

    -- 创建索引以提高查询性能
    CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON AuditLogs(Action);
    
    PRINT '✓ 审计日志表创建成功';
END
GO

-- =============================================
-- 5. 创建登录日志表（如果不存在）
-- =============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LoginLogs')
BEGIN
    CREATE TABLE LoginLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NULL,
        UserName NVARCHAR(100) NOT NULL,
        LoginTime DATETIME2 DEFAULT GETDATE(),
        LoginResult BIT NOT NULL, -- 1=成功, 0=失败
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        FailureReason NVARCHAR(200) NULL,
        SessionId NVARCHAR(100) NULL
    );

    -- 创建索引
    CREATE NONCLUSTERED INDEX IX_LoginLogs_UserId ON LoginLogs(UserId);
    CREATE NONCLUSTERED INDEX IX_LoginLogs_LoginTime ON LoginLogs(LoginTime DESC);
    CREATE NONCLUSTERED INDEX IX_LoginLogs_UserName ON LoginLogs(UserName);
    
    PRINT '✓ 登录日志表创建成功';
END
GO

-- =============================================
-- 6. 插入初始审计日志
-- =============================================
INSERT INTO AuditLogs (
    UserId, UserName, Action, EntityType, EntityId, 
    IpAddress, Timestamp, Result
) VALUES (
    NULL, 'System', 'Database.Initialize', 'System', 'MVP_v1.0.0',
    '127.0.0.1', GETDATE(), 'Success'
);

PRINT '✓ 初始审计日志记录已创建';
GO

-- =============================================
-- 完成提示
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MVP初始化数据脚本执行完成！';
PRINT '========================================';
PRINT '默认账号信息：';
PRINT '  管理员 - 用户名: sysadmin, 密码: LybtAdmin2025@SecurePass!';
PRINT '  医生1  - 用户名: doctor001, 密码: Doctor2025@Pass!';
PRINT '  医生2  - 用户名: doctor002, 密码: Doctor2025@Pass!';
PRINT '';
PRINT '注意：请在首次登录后立即修改默认密码！';
PRINT '========================================';
GO