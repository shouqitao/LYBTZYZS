-- ============================================================================
-- Issue #2213: 修复MedicalCase历史数据中DoctorId = Guid.Empty的问题
-- Epic #2210 Phase 1 - P0 Critical Bug修复
--
-- 问题描述:
--   由于历史版本的MedicalCaseService.CreateAsync方法未设置DoctorId/DoctorName字段,
--   导致所有历史医案记录的DoctorId = '00000000-0000-0000-0000-000000000000'
--
-- 修复策略:
--   1. 根据MedicalCases.CreatedBy字段查找Users表获取正确的DoctorId和RealName
--   2. 更新MedicalCases表的DoctorId和DoctorName字段
--   3. 如果CreatedBy为NULL或找不到对应User,则跳过该记录(需要人工处理)
--
-- 执行环境: SQL Server
-- 创建日期: 2025-11-22
-- 作者: Claude Code (Issue #2213)
-- ============================================================================

SET NOCOUNT ON;
GO

DECLARE @AffectedRows INT = 0;
DECLARE @SkippedRows INT = 0;
DECLARE @TotalRows INT = 0;
DECLARE @StartTime DATETIME2 = GETDATE();

PRINT '========================================';
PRINT 'Issue #2213: 修复MedicalCase DoctorId数据迁移';
PRINT '开始时间: ' + CONVERT(VARCHAR(23), @StartTime, 121);
PRINT '========================================';
PRINT '';

-- 统计需要修复的记录总数
SELECT @TotalRows = COUNT(*)
FROM MedicalCases
WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

PRINT '检测到需要修复的记录数: ' + CAST(@TotalRows AS VARCHAR(10));
PRINT '';

-- 开始事务
BEGIN TRANSACTION;

BEGIN TRY
    -- 更新有效的记录（CreatedBy不为NULL且能在Users表中找到对应记录）
    UPDATE mc
    SET
        mc.DoctorId = u.Id,
        mc.DoctorName = u.RealName,
        mc.UpdatedAt = GETDATE()
    FROM MedicalCases mc
    INNER JOIN Users u ON mc.CreatedBy = u.Id
    WHERE mc.DoctorId = '00000000-0000-0000-0000-000000000000'
      AND mc.CreatedBy IS NOT NULL
      AND u.IsDeleted = 0;

    SET @AffectedRows = @@ROWCOUNT;

    -- 统计无法自动修复的记录数（CreatedBy为NULL或找不到对应User）
    SELECT @SkippedRows = COUNT(*)
    FROM MedicalCases mc
    LEFT JOIN Users u ON mc.CreatedBy = u.Id AND u.IsDeleted = 0
    WHERE mc.DoctorId = '00000000-0000-0000-0000-000000000000'
      AND (mc.CreatedBy IS NULL OR u.Id IS NULL);

    PRINT '========================================';
    PRINT '数据迁移执行结果:';
    PRINT '----------------------------------------';
    PRINT '成功更新记录数: ' + CAST(@AffectedRows AS VARCHAR(10));
    PRINT '跳过记录数: ' + CAST(@SkippedRows AS VARCHAR(10));
    PRINT '总记录数: ' + CAST(@TotalRows AS VARCHAR(10));
    PRINT '';

    -- 如果有跳过的记录，列出详细信息供人工处理
    IF @SkippedRows > 0
    BEGIN
        PRINT '⚠️  警告: 以下记录无法自动修复(CreatedBy为NULL或User不存在):';
        PRINT '----------------------------------------';

        SELECT
            mc.Id AS MedicalCaseId,
            mc.PatientId,
            mc.PatientName,
            mc.ConsultationDate,
            mc.CreatedBy,
            mc.CreatedAt,
            CASE
                WHEN mc.CreatedBy IS NULL THEN 'CreatedBy为NULL'
                WHEN u.Id IS NULL THEN 'User不存在或已删除'
                ELSE '未知原因'
            END AS SkipReason
        FROM MedicalCases mc
        LEFT JOIN Users u ON mc.CreatedBy = u.Id AND u.IsDeleted = 0
        WHERE mc.DoctorId = '00000000-0000-0000-0000-000000000000'
          AND (mc.CreatedBy IS NULL OR u.Id IS NULL)
        ORDER BY mc.CreatedAt;

        PRINT '';
        PRINT '⚠️  请人工检查以上记录并手动设置DoctorId和DoctorName!';
    END

    -- 提交事务
    COMMIT TRANSACTION;

    DECLARE @EndTime DATETIME2 = GETDATE();
    DECLARE @Duration INT = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

    PRINT '';
    PRINT '========================================';
    PRINT '数据迁移完成';
    PRINT '结束时间: ' + CONVERT(VARCHAR(23), @EndTime, 121);
    PRINT '耗时: ' + CAST(@Duration AS VARCHAR(10)) + ' 毫秒';
    PRINT '========================================';

END TRY
BEGIN CATCH
    -- 回滚事务
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT '';
    PRINT '❌ 错误: 数据迁移失败!';
    PRINT '错误消息: ' + @ErrorMessage;
    PRINT '错误严重性: ' + CAST(@ErrorSeverity AS VARCHAR(10));
    PRINT '错误状态: ' + CAST(@ErrorState AS VARCHAR(10));
    PRINT '';
    PRINT '事务已回滚，未做任何更改。';

    -- 重新抛出错误
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH

SET NOCOUNT OFF;
GO

-- ============================================================================
-- 验证脚本（可选执行）
-- ============================================================================
PRINT '';
PRINT '========================================';
PRINT '数据验证';
PRINT '========================================';

-- 验证1: 检查是否还有DoctorId为Guid.Empty的记录
DECLARE @RemainingEmptyCount INT;
SELECT @RemainingEmptyCount = COUNT(*)
FROM MedicalCases
WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

PRINT '剩余DoctorId为Guid.Empty的记录数: ' + CAST(@RemainingEmptyCount AS VARCHAR(10));

-- 验证2: 检查DoctorName是否正确设置
DECLARE @EmptyDoctorNameCount INT;
SELECT @EmptyDoctorNameCount = COUNT(*)
FROM MedicalCases
WHERE DoctorId != '00000000-0000-0000-0000-000000000000'
  AND (DoctorName IS NULL OR DoctorName = '');

PRINT 'DoctorId已设置但DoctorName为空的记录数: ' + CAST(@EmptyDoctorNameCount AS VARCHAR(10));

-- 验证3: 显示修复后的样例数据（前5条）
PRINT '';
PRINT '修复后的样例数据（前5条）:';
PRINT '----------------------------------------';
SELECT TOP 5
    Id AS MedicalCaseId,
    PatientName,
    DoctorId,
    DoctorName,
    ConsultationDate,
    UpdatedAt
FROM MedicalCases
WHERE DoctorId != '00000000-0000-0000-0000-000000000000'
ORDER BY UpdatedAt DESC;

PRINT '';
PRINT '========================================';
PRINT '数据验证完成';
PRINT '========================================';
GO
