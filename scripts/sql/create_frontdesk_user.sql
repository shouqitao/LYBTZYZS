-- 创建前台挂号人员测试用户
-- 用户名: frontdesk
-- 密码: Front@123456
-- 角色: RegistrationStaff (0)

USE LYBTDB;
GO

-- 检查用户是否已存在
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'frontdesk')
BEGIN
    INSERT INTO Users (
        Id,
        Username,
        PasswordHash,
        RealName,
        PinYinCode,
        Role,  -- UserRole.RegistrationStaff = 0
        IsActive,
        CreateTime,
        Email,
        PhoneNumber,
        Department,
        Position
    ) VALUES (
        NEWID(),
        'frontdesk',
        'AQAAAAIAAYagAAAAEPxjZQ6uXz1vIpH5kB9HgT9S2JO9bvHmzUAX8Yl+7Yx3hKQNMJ0RKP4ZvN6HzxVxVg==', -- Front@123456
        '张小丽',
        'ZXL',
        0,  -- RegistrationStaff
        1,  -- IsActive
        GETDATE(),
        'frontdesk@lybt.com',
        '13800138001',
        '挂号室',
        '挂号员'
    );
    
    PRINT '前台挂号人员用户创建成功！';
    PRINT '用户名: frontdesk';
    PRINT '密码: Front@123456';
    PRINT '角色: 挂号人员';
END
ELSE
BEGIN
    -- 如果用户已存在，更新其角色为挂号人员
    UPDATE Users 
    SET Role = 0,  -- RegistrationStaff
        RealName = '张小丽',
        Department = '挂号室',
        Position = '挂号员',
        IsActive = 1
    WHERE Username = 'frontdesk';
    
    PRINT '前台挂号人员用户已更新！';
END

-- 创建另一个前台测试用户
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'reception')
BEGIN
    INSERT INTO Users (
        Id,
        Username,
        PasswordHash,
        RealName,
        PinYinCode,
        Role,  -- UserRole.RegistrationStaff = 0
        IsActive,
        CreateTime,
        Email,
        PhoneNumber,
        Department,
        Position
    ) VALUES (
        NEWID(),
        'reception',
        'AQAAAAIAAYagAAAAEPxjZQ6uXz1vIpH5kB9HgT9S2JO9bvHmzUAX8Yl+7Yx3hKQNMJ0RKP4ZvN6HzxVxVg==', -- Front@123456
        '李小梅',
        'LXM',
        0,  -- RegistrationStaff
        1,  -- IsActive
        GETDATE(),
        'reception@lybt.com',
        '13800138002',
        '挂号室',
        '前台接待'
    );
    
    PRINT '前台接待人员用户创建成功！';
    PRINT '用户名: reception';
    PRINT '密码: Front@123456';
    PRINT '角色: 挂号人员';
END

-- 显示所有挂号人员
SELECT 
    Username as '用户名',
    RealName as '真实姓名',
    Role as '角色值',
    CASE Role 
        WHEN 0 THEN '挂号人员'
        WHEN 1 THEN '主治医生'
        WHEN 2 THEN '收费人员'
        WHEN 3 THEN '药剂师'
        WHEN 4 THEN '理疗师'
        WHEN 99 THEN '管理员'
        ELSE '未知'
    END as '角色名称',
    Department as '部门',
    Position as '职位',
    PhoneNumber as '电话',
    IsActive as '是否启用'
FROM Users
WHERE Role = 0
ORDER BY CreateTime DESC;