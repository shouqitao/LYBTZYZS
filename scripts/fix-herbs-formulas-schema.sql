USE LYBTDB;
GO

PRINT '========== 修复 Herbs 和 Formulas 表 Schema ==========';
PRINT '';

-- ========================================
-- 1. Herbs 表：添加 Status 默认值约束
-- ========================================

PRINT '[1/5] 为 Herbs.Status 添加默认值约束...';

-- 检查是否已有默认值约束
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID('Herbs') AND c.name = 'Status'
)
BEGIN
    ALTER TABLE [Herbs] ADD CONSTRAINT [DF_Herbs_Status] DEFAULT (0) FOR [Status];
    PRINT '  ✅ 已添加 Herbs.Status 默认值约束 (CommonStatus.Enabled = 0)';
END
ELSE
    PRINT '  ℹ️  Herbs.Status 默认值约束已存在';

PRINT '';

-- ========================================
-- 2. Formulas 表：添加 Category 列
-- ========================================

PRINT '[2/5] 为 Formulas 添加 Category 列...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Formulas') AND name = 'Category')
BEGIN
    ALTER TABLE [Formulas] ADD [Category] nvarchar(50) NULL;
    PRINT '  ✅ 已添加 Formulas.Category 列';
END
ELSE
    PRINT '  ℹ️  Formulas.Category 列已存在';

PRINT '';

-- ========================================
-- 3. Formulas 表：添加 FormulaType 列
-- ========================================

PRINT '[3/5] 为 Formulas 添加 FormulaType 列...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Formulas') AND name = 'FormulaType')
BEGIN
    ALTER TABLE [Formulas] ADD [FormulaType] int NOT NULL DEFAULT (2); -- FormulaType.Experience = 2
    PRINT '  ✅ 已添加 Formulas.FormulaType 列 (默认值: Experience = 2)';
END
ELSE
    PRINT '  ℹ️  Formulas.FormulaType 列已存在';

PRINT '';

-- ========================================
-- 4. Formulas 表：添加 UserId 列
-- ========================================

PRINT '[4/5] 为 Formulas 添加 UserId 列...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Formulas') AND name = 'UserId')
BEGIN
    ALTER TABLE [Formulas] ADD [UserId] uniqueidentifier NULL;
    PRINT '  ✅ 已添加 Formulas.UserId 列';
END
ELSE
    PRINT '  ℹ️  Formulas.UserId 列已存在';

PRINT '';

-- ========================================
-- 5. Formulas 表：添加 CreatedBy 列
-- ========================================

PRINT '[5/5] 为 Formulas 添加 CreatedBy 列...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Formulas') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [Formulas] ADD [CreatedBy] uniqueidentifier NULL;
    PRINT '  ✅ 已添加 Formulas.CreatedBy 列';
END
ELSE
    PRINT '  ℹ️  Formulas.CreatedBy 列已存在';

PRINT '';
PRINT '========== Schema 修复完成 ==========';
PRINT '';

-- ========================================
-- 验证修复结果
-- ========================================

PRINT '========== 验证修复结果 ==========';
PRINT '';

PRINT '--- Herbs 表列数 ---';
SELECT COUNT(*) AS HerbsColumnCount FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Herbs';

PRINT '';
PRINT '--- Formulas 表列数 ---';
SELECT COUNT(*) AS FormulasColumnCount FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Formulas';

PRINT '';
PRINT '--- Formulas 新增列确认 ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Formulas'
  AND COLUMN_NAME IN ('Category', 'FormulaType', 'UserId', 'CreatedBy')
ORDER BY COLUMN_NAME;

GO
