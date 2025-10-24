-- Issue #1598: REQ-001 - 三步工作流优化-Step1
-- 为Consultations表添加工作流状态字段

USE LYBTDB;
GO

-- 检查字段是否已存在，避免重复添加
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Consultations') AND name = 'Step1CompletedAt')
BEGIN
    ALTER TABLE Consultations ADD Step1CompletedAt DATETIME2 NULL;
    PRINT 'Added Step1CompletedAt column';
END
ELSE
    PRINT 'Step1CompletedAt column already exists';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Consultations') AND name = 'Step2CompletedAt')
BEGIN
    ALTER TABLE Consultations ADD Step2CompletedAt DATETIME2 NULL;
    PRINT 'Added Step2CompletedAt column';
END
ELSE
    PRINT 'Step2CompletedAt column already exists';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Consultations') AND name = 'Step3CompletedAt')
BEGIN
    ALTER TABLE Consultations ADD Step3CompletedAt DATETIME2 NULL;
    PRINT 'Added Step3CompletedAt column';
END
ELSE
    PRINT 'Step3CompletedAt column already exists';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Consultations') AND name = 'PrescriptionEnabled')
BEGIN
    ALTER TABLE Consultations ADD PrescriptionEnabled BIT NOT NULL DEFAULT 1;
    PRINT 'Added PrescriptionEnabled column';
END
ELSE
    PRINT 'PrescriptionEnabled column already exists';

GO

-- 创建索引优化查询性能（可选）
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Consultations') AND name = 'IX_Consultations_CompletionStatus')
BEGIN
    CREATE INDEX IX_Consultations_CompletionStatus
    ON Consultations(Step1CompletedAt, Step2CompletedAt, Step3CompletedAt);
    PRINT 'Created IX_Consultations_CompletionStatus index';
END
ELSE
    PRINT 'IX_Consultations_CompletionStatus index already exists';

GO

PRINT 'Migration completed successfully!';
