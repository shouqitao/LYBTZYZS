-- 修复Herbs表字段长度统一化
-- TECH_DEBT_BACKLOG.md P0-6 修复项

-- 调整PinYinCode字段长度：从20改为50（与实体定义一致）
ALTER TABLE Herbs 
ALTER COLUMN PinYinCode NVARCHAR(50);
PRINT 'PinYinCode字段长度已调整为50';

-- 调整Origin字段长度：从50改为100（与实体定义一致）
ALTER TABLE Herbs 
ALTER COLUMN Origin NVARCHAR(100);
PRINT 'Origin字段长度已调整为100';

-- 调整Spec字段长度：从50改为100（与实体定义一致）
ALTER TABLE Herbs 
ALTER COLUMN Spec NVARCHAR(100);
PRINT 'Spec字段长度已调整为100';

-- 调整Effect字段长度：从256改为500（与实体定义一致）
ALTER TABLE Herbs 
ALTER COLUMN Effect NVARCHAR(500);
PRINT 'Effect字段长度已调整为500';

-- 调整Usage字段长度：从256改为500（与实体定义一致）
ALTER TABLE Herbs 
ALTER COLUMN Usage NVARCHAR(500);
PRINT 'Usage字段长度已调整为500';

-- 验证更改
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Herbs' 
  AND COLUMN_NAME IN ('PinYinCode', 'Origin', 'Spec', 'Effect', 'Usage')
ORDER BY COLUMN_NAME;

PRINT '✅ Herbs表字段长度统一化修复完成';