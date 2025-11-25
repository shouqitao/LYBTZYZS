-- =============================================
-- Issue #2242: 简化MedicalCaseStatus状态机 - 移除Cancelled状态
-- 描述: 将所有Cancelled状态的医案转换为软删除（IsDeleted=1）
-- 作者: Claude Code
-- 日期: 2025-11-25
-- =============================================

USE [LYBTDB]
GO

BEGIN TRANSACTION;

DECLARE @AffectedRows INT = 0;

-- Step 1: 备份当前Cancelled状态的医案（用于回滚）
IF OBJECT_ID('tempdb..#BackupCancelledCases') IS NOT NULL
    DROP TABLE #BackupCancelledCases;

SELECT
    Id,
    CaseStatus,
    IsDeleted,
    UpdatedAt
INTO #BackupCancelledCases
FROM MedicalCases
WHERE CaseStatus = 3; -- Cancelled = 3

SET @AffectedRows = @@ROWCOUNT;

PRINT '备份了 ' + CAST(@AffectedRows AS NVARCHAR(10)) + ' 条Cancelled状态的医案记录';

-- Step 2: 将Cancelled状态转换为软删除
-- 将 CaseStatus 改为 Completed (2)，IsDeleted 设置为 1
UPDATE MedicalCases
SET
    IsDeleted = 1,
    CaseStatus = 2,  -- Completed = 2
    UpdatedAt = GETDATE()
WHERE CaseStatus = 3;  -- Cancelled = 3

SET @AffectedRows = @@ROWCOUNT;

PRINT '成功转换 ' + CAST(@AffectedRows AS NVARCHAR(10)) + ' 条医案记录';

-- Step 3: 验证转换结果
DECLARE @RemainingCancelled INT;
SELECT @RemainingCancelled = COUNT(*)
FROM MedicalCases
WHERE CaseStatus = 3;

IF @RemainingCancelled > 0
BEGIN
    PRINT 'ERROR: 仍有 ' + CAST(@RemainingCancelled AS NVARCHAR(10)) + ' 条Cancelled状态的记录';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Step 4: 统计转换后的状态分布
SELECT
    '转换后状态分布' AS [统计信息],
    CaseStatus,
    IsDeleted,
    COUNT(*) AS [记录数]
FROM MedicalCases
GROUP BY CaseStatus, IsDeleted
ORDER BY CaseStatus, IsDeleted;

-- 提交事务
COMMIT TRANSACTION;

PRINT '数据迁移成功完成！';
PRINT '注意：备份表 #BackupCancelledCases 在会话结束时自动删除';

GO

-- =============================================
-- 回滚脚本（如需回滚，请执行以下SQL）
-- =============================================
/*
USE [LYBTDB]
GO

BEGIN TRANSACTION;

-- 从备份表恢复（需要在同一会话中执行，否则 #BackupCancelledCases 已不存在）
UPDATE m
SET
    m.CaseStatus = b.CaseStatus,
    m.IsDeleted = b.IsDeleted,
    m.UpdatedAt = b.UpdatedAt
FROM MedicalCases m
INNER JOIN #BackupCancelledCases b ON m.Id = b.Id;

COMMIT TRANSACTION;

PRINT '回滚完成！';
*/
