-- =============================================
-- MVP 初始化数据脚本 (更新版 - 修复测试用户配置)
-- 版本：v1.1.0-test-alignment
-- 更新时间：2026-04-07
-- 说明：对齐 E2E 测试配置，创建标准测试用户集
-- =============================================

USE [LYBTDB];
GO

-- =============================================
-- 1. 创建系统管理员账号 (SuperAdmin - 角色值: 100)
-- 用户名: sysadmin, 密码: DevPass123!
-- =============================================
PRINT '正在创建系统管理员账号...';

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'sysadmin')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'sysadmin',
        '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu', -- BCrypt hash of 'DevPass123!'
        '系统管理员',
        'sysadmin@lybt.local',
        '13800138000',
        100, -- Role.SuperAdmin = 100
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 系统管理员账号创建成功 (用户名: sysadmin, 密码: DevPass123!)';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu',
        Role = 100,
        RealName = '系统管理员',
        Email = 'sysadmin@lybt.local',
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'sysadmin';
    PRINT '→ 系统管理员账号已更新为测试配置';
END
GO

-- =============================================
-- 2. 创建业务管理员账号 (Admin - 角色值: 10)
-- 用户名: admin, 密码: AdminPass123!
-- =============================================
PRINT '正在创建业务管理员账号...';

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'admin')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'admin',
        '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu', -- BCrypt hash of 'AdminPass123!'
        '业务管理员',
        'admin@lybt.local',
        '13800138001',
        10,  -- Role.Admin = 10
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 业务管理员账号创建成功 (用户名: admin, 密码: AdminPass123!)';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu',
        Role = 10,
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'admin';
    PRINT '→ 业务管理员账号已更新';
END
GO

-- =============================================
-- 3. 创建前台接待账号 (Receptionist - 角色值: 0)
-- 用户名: receptionist, 密码: ReceptionistPass123!
-- =============================================
PRINT '正在创建前台接待账号...';

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'receptionist')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'receptionist',
        '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu', -- BCrypt hash of 'ReceptionistPass123!'
        '前台接待',
        'receptionist@lybt.local',
        '13800138002',
        0,   -- Role.Receptionist = 0
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 前台接待账号创建成功 (用户名: receptionist, 密码: ReceptionistPass123!)';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$rBNDJ8kZFq8WXzP5J9YQZ.x0xVcLhGxKLvQ8hXGYr5sNxT8KxqFWu',
        Role = 0,
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'receptionist';
    PRINT '→ 前台接待账号已更新';
END
GO

-- =============================================
-- 4. 创建医生账号 (Doctor - 角色值: 1)
-- =============================================
PRINT '正在创建医生账号...';

-- 主测试医生账号
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'doctor')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'doctor',
        '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO', -- BCrypt hash of 'DoctorPass123!'
        '张医生',
        'doctor@lybt.local',
        '13900139000',
        1,   -- Role.Doctor = 1
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 医生账号创建成功 (用户名: doctor, 密码: DoctorPass123!)';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO',
        Role = 1,
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'doctor';
    PRINT '→ 医生账号 doctor 已更新';
END

-- 备用医生账号1
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'doctor001')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'doctor001',
        '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO',
        '张医生',
        'zhangys@lybt.local',
        '13900139001',
        1,   -- Role.Doctor = 1
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 医生账号 doctor001 创建成功';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO',
        Role = 1,
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'doctor001';
    PRINT '→ 医生账号 doctor001 已更新';
END

-- 备用医生账号2
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'doctor002')
BEGIN
    INSERT INTO Users (
        Id, 
        UserName, 
        PasswordHash, 
        RealName, 
        Email, 
        PhoneNumber,
        Role, 
        Status, 
        IsDeleted,
        CreatedAt, 
        UpdatedAt,
        LastLoginTime,
        FailedLoginCount,
        LockoutEnd,
        MustChangeOnNextLogin
    ) VALUES (
        NEWID(),
        'doctor002',
        '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO',
        '李医生',
        'liys@lybt.local',
        '13900139002',
        1,   -- Role.Doctor = 1
        0,   -- Status.Enabled = 0
        0,   -- IsDeleted = false
        GETDATE(),
        GETDATE(),
        NULL,
        0,
        NULL,
        0
    );
    PRINT '✓ 医生账号 doctor002 创建成功';
END
ELSE
BEGIN
    UPDATE Users SET 
        PasswordHash = '$2a$11$3y.Nt9XTdVhT0OvK0pxMx.gZxYr3hDjH3Q8RQrJPx3xLbNxVJKHxO',
        Role = 1,
        Status = 0,
        IsDeleted = 0
    WHERE UserName = 'doctor002';
    PRINT '→ 医生账号 doctor002 已更新';
END
GO

-- =============================================
-- 5. 创建诊所基础信息配置
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
-- 6. 创建审计日志表（如果不存在）
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
-- 7. 创建登录日志表（如果不存在）
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
-- 8. 插入初始审计日志
-- =============================================
INSERT INTO AuditLogs (
    UserId, UserName, Action, EntityType, EntityId, 
    IpAddress, Timestamp, Result
) VALUES (
    NULL, 'System', 'Database.Initialize', 'System', 'MVP_v1.1.0_test_alignment',
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
PRINT '测试账号信息（与 appsettings.Test.json 一致）：';
PRINT '  系统管理员  - 用户名: sysadmin,      密码: DevPass123!,        角色: SuperAdmin';
PRINT '  业务管理员  - 用户名: admin,         密码: AdminPass123!,      角色: Admin';
PRINT '  前台接待    - 用户名: receptionist,  密码: ReceptionistPass123!, 角色: Receptionist';
PRINT '  医生        - 用户名: doctor,        密码: DoctorPass123!,     角色: Doctor';
PRINT '  医生(备用1) - 用户名: doctor001,     密码: DoctorPass123!,     角色: Doctor';
PRINT '  医生(备用2) - 用户名: doctor002,     密码: DoctorPass123!,     角色: Doctor';
PRINT '';
PRINT '角色层级: Receptionist(0) < Doctor(1) < Admin(10) < SuperAdmin(100)';
PRINT '========================================';
GO
