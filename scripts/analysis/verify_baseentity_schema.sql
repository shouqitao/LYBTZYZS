-- 验证BaseEntity审计字段迁移结果
-- 检查Users、Prescriptions、Formulas、Patients、Herbs五张表的字段完整性

PRINT '========== Users 表字段检查 =========='
SELECT
    TABLE_NAME = 'Users',
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
  AND COLUMN_NAME IN ('Id', 'CreatedAt', 'UpdatedAt', 'CreatedBy', 'UpdatedBy', 'RowVersion', 'IsDeleted')
ORDER BY
    CASE COLUMN_NAME
        WHEN 'Id' THEN 1
        WHEN 'CreatedAt' THEN 2
        WHEN 'UpdatedAt' THEN 3
        WHEN 'CreatedBy' THEN 4
        WHEN 'UpdatedBy' THEN 5
        WHEN 'RowVersion' THEN 6
        WHEN 'IsDeleted' THEN 7
    END;

-- 检查是否存在旧字段UpdateTime
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdateTime')
    PRINT '⚠️ 警告：Users表仍存在旧字段 UpdateTime'
ELSE
    PRINT '✓ Users表已正确重命名 UpdateTime → UpdatedAt'

PRINT ''
PRINT '========== Prescriptions 表字段检查 =========='
SELECT
    TABLE_NAME = 'Prescriptions',
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Prescriptions'
  AND COLUMN_NAME IN ('Id', 'CreatedAt', 'UpdatedAt', 'CreatedBy', 'UpdatedBy', 'RowVersion', 'IsDeleted')
ORDER BY
    CASE COLUMN_NAME
        WHEN 'Id' THEN 1
        WHEN 'CreatedAt' THEN 2
        WHEN 'UpdatedAt' THEN 3
        WHEN 'CreatedBy' THEN 4
        WHEN 'UpdatedBy' THEN 5
        WHEN 'RowVersion' THEN 6
        WHEN 'IsDeleted' THEN 7
    END;

-- 检查是否存在旧字段
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME IN ('CreateTime', 'UpdateTime'))
    PRINT '⚠️ 警告：Prescriptions表仍存在旧字段 CreateTime/UpdateTime'
ELSE
    PRINT '✓ Prescriptions表已正确重命名 CreateTime → CreatedAt, UpdateTime → UpdatedAt'

PRINT ''
PRINT '========== Formulas 表字段检查 =========='
SELECT
    TABLE_NAME = 'Formulas',
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Formulas'
  AND COLUMN_NAME IN ('Id', 'CreatedAt', 'UpdatedAt', 'CreatedBy', 'UpdatedBy', 'RowVersion', 'IsDeleted')
ORDER BY
    CASE COLUMN_NAME
        WHEN 'Id' THEN 1
        WHEN 'CreatedAt' THEN 2
        WHEN 'UpdatedAt' THEN 3
        WHEN 'CreatedBy' THEN 4
        WHEN 'UpdatedBy' THEN 5
        WHEN 'RowVersion' THEN 6
        WHEN 'IsDeleted' THEN 7
    END;

-- 检查是否存在旧字段CreatedById
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'CreatedById')
    PRINT '⚠️ 警告：Formulas表仍存在旧字段 CreatedById'
ELSE
    PRINT '✓ Formulas表已正确重命名 CreatedById → CreatedBy'

PRINT ''
PRINT '========== Patients 表字段检查 =========='
SELECT
    TABLE_NAME = 'Patients',
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Patients'
  AND COLUMN_NAME IN ('Id', 'CreatedAt', 'UpdatedAt', 'CreatedBy', 'UpdatedBy', 'RowVersion', 'IsDeleted')
ORDER BY
    CASE COLUMN_NAME
        WHEN 'Id' THEN 1
        WHEN 'CreatedAt' THEN 2
        WHEN 'UpdatedAt' THEN 3
        WHEN 'CreatedBy' THEN 4
        WHEN 'UpdatedBy' THEN 5
        WHEN 'RowVersion' THEN 6
        WHEN 'IsDeleted' THEN 7
    END;

PRINT ''
PRINT '========== Herbs 表字段检查 =========='
SELECT
    TABLE_NAME = 'Herbs',
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Herbs'
  AND COLUMN_NAME IN ('Id', 'CreatedAt', 'UpdatedAt', 'CreatedBy', 'UpdatedBy', 'RowVersion', 'IsDeleted')
ORDER BY
    CASE COLUMN_NAME
        WHEN 'Id' THEN 1
        WHEN 'CreatedAt' THEN 2
        WHEN 'UpdatedAt' THEN 3
        WHEN 'CreatedBy' THEN 4
        WHEN 'UpdatedBy' THEN 5
        WHEN 'RowVersion' THEN 6
        WHEN 'IsDeleted' THEN 7
    END;

PRINT ''
PRINT '========== 综合检查结果 =========='

-- 统计所有表的BaseEntity字段完整性
DECLARE @MissingFields TABLE (
    TableName NVARCHAR(50),
    MissingField NVARCHAR(50)
)

-- 检查Users表
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CreatedAt')
    INSERT INTO @MissingFields VALUES ('Users', 'CreatedAt')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdatedAt')
    INSERT INTO @MissingFields VALUES ('Users', 'UpdatedAt')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CreatedBy')
    INSERT INTO @MissingFields VALUES ('Users', 'CreatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdatedBy')
    INSERT INTO @MissingFields VALUES ('Users', 'UpdatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsDeleted')
    INSERT INTO @MissingFields VALUES ('Users', 'IsDeleted')

-- 检查Prescriptions表
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'CreatedAt')
    INSERT INTO @MissingFields VALUES ('Prescriptions', 'CreatedAt')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'UpdatedAt')
    INSERT INTO @MissingFields VALUES ('Prescriptions', 'UpdatedAt')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'CreatedBy')
    INSERT INTO @MissingFields VALUES ('Prescriptions', 'CreatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'UpdatedBy')
    INSERT INTO @MissingFields VALUES ('Prescriptions', 'UpdatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'IsDeleted')
    INSERT INTO @MissingFields VALUES ('Prescriptions', 'IsDeleted')

-- 检查Formulas表
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'CreatedBy')
    INSERT INTO @MissingFields VALUES ('Formulas', 'CreatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'UpdatedBy')
    INSERT INTO @MissingFields VALUES ('Formulas', 'UpdatedBy')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'RowVersion')
    INSERT INTO @MissingFields VALUES ('Formulas', 'RowVersion')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas' AND COLUMN_NAME = 'IsDeleted')
    INSERT INTO @MissingFields VALUES ('Formulas', 'IsDeleted')

-- 检查Patients表
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Patients' AND COLUMN_NAME = 'IsDeleted')
    INSERT INTO @MissingFields VALUES ('Patients', 'IsDeleted')

-- 检查Herbs表
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Herbs' AND COLUMN_NAME = 'RowVersion')
    INSERT INTO @MissingFields VALUES ('Herbs', 'RowVersion')
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Herbs' AND COLUMN_NAME = 'IsDeleted')
    INSERT INTO @MissingFields VALUES ('Herbs', 'IsDeleted')

IF EXISTS (SELECT * FROM @MissingFields)
BEGIN
    PRINT '❌ 发现缺失字段：'
    SELECT * FROM @MissingFields
END
ELSE
BEGIN
    PRINT '✅ 所有BaseEntity字段检查通过！'
    PRINT '   - Users: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted'
    PRINT '   - Prescriptions: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted'
    PRINT '   - Formulas: CreatedBy, UpdatedBy, RowVersion, IsDeleted'
    PRINT '   - Patients: IsDeleted'
    PRINT '   - Herbs: RowVersion, IsDeleted'
END
