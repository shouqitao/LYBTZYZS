USE LYBTDB;
GO

PRINT '========== Adding missing CreatedAt and UpdatedAt columns ==========';

-- Herbs table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [Herbs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added CreatedAt column to Herbs table';
END
ELSE
    PRINT 'CreatedAt column already exists in Herbs table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Herbs]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [Herbs] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added UpdatedAt column to Herbs table';
END
ELSE
    PRINT 'UpdatedAt column already exists in Herbs table';

-- Formulas table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [Formulas] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added CreatedAt column to Formulas table';
END
ELSE
    PRINT 'CreatedAt column already exists in Formulas table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Formulas]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [Formulas] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added UpdatedAt column to Formulas table';
END
ELSE
    PRINT 'UpdatedAt column already exists in Formulas table';

PRINT '========== Migration completed ==========';
GO
