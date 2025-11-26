-- =============================================================================
-- 医案DoctorId/DoctorName/PatientName数据迁移脚本
-- 版本: 1.0
-- 日期: 2025-11-22
-- 说明: 修复所有历史医案的DoctorId/DoctorName/PatientName字段
-- 依赖: Task 1.1.3, Task 1.1.4 (Epic #2210)
-- =============================================================================

-- 安全检查: 创建备份表
IF OBJECT_ID('MedicalCase_Backup_20251122', 'U') IS NOT NULL
    DROP TABLE MedicalCase_Backup_20251122;

SELECT * INTO MedicalCase_Backup_20251122 FROM MedicalCase;
PRINT '备份表创建成功: MedicalCase_Backup_20251122';

-- 开启事务
BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: 更新DoctorId和DoctorName（基于CreatedBy字段）
    UPDATE m
    SET
        m.DoctorId = ISNULL(u.Id, '00000000-0000-0000-0000-000000000000'),
        m.DoctorName = u.RealName,
        m.UpdatedAt = GETDATE()
    FROM MedicalCase m
    LEFT JOIN [User] u ON m.CreatedBy = u.Id AND u.Role = 1  -- Role=1表示Doctor
    WHERE m.DoctorId = '00000000-0000-0000-0000-000000000000';

    DECLARE @UpdatedDoctorRows INT = @@ROWCOUNT;
    PRINT 'Step 1完成: 更新DoctorId/DoctorName，影响行数=' + CAST(@UpdatedDoctorRows AS VARCHAR);

    -- Step 2: 更新PatientName（基于PatientId字段）
    UPDATE m
    SET
        m.PatientName = p.Name,
        m.UpdatedAt = GETDATE()
    FROM MedicalCase m
    INNER JOIN Patient p ON m.PatientId = p.Id
    WHERE m.PatientName IS NULL OR m.PatientName = '';

    DECLARE @UpdatedPatientRows INT = @@ROWCOUNT;
    PRINT 'Step 2完成: 更新PatientName，影响行数=' + CAST(@UpdatedPatientRows AS VARCHAR);

    -- Step 3: 验证数据完整性
    DECLARE @RemainingEmptyDoctorId INT;
    SELECT @RemainingEmptyDoctorId = COUNT(*)
    FROM MedicalCase
    WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

    IF @RemainingEmptyDoctorId > 0
    BEGIN
        PRINT '警告: 仍有 ' + CAST(@RemainingEmptyDoctorId AS VARCHAR) + ' 条记录DoctorId=Guid.Empty';
        PRINT '原因: CreatedBy字段为NULL或关联不到Doctor角色用户';

        -- 记录问题记录到临时表供人工核查
        IF OBJECT_ID('tempdb..#ProblematicRecords') IS NOT NULL
            DROP TABLE #ProblematicRecords;

        SELECT Id, PatientId, CreatedBy, CreatedAt
        INTO #ProblematicRecords
        FROM MedicalCase
        WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

        PRINT '问题记录已保存到临时表 #ProblematicRecords，请人工核查';
    END
    ELSE
    BEGIN
        PRINT '验证通过: 无残留DoctorId=Guid.Empty记录';
    END

    -- 提交事务
    COMMIT TRANSACTION;
    PRINT '数据迁移成功完成';

    -- 输出统计信息
    SELECT
        '数据迁移统计' AS [Report],
        @UpdatedDoctorRows AS [DoctorId更新行数],
        @UpdatedPatientRows AS [PatientName更新行数],
        @RemainingEmptyDoctorId AS [残留Guid.Empty记录数];

END TRY
BEGIN CATCH
    -- 回滚事务
    ROLLBACK TRANSACTION;

    -- 输出错误信息
    PRINT '数据迁移失败，事务已回滚';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);

    -- 抛出错误
    THROW;
END CATCH;

-- 验证脚本（迁移后执行）
SELECT
    COUNT(*) AS TotalRecords,
    COUNT(CASE WHEN DoctorId != '00000000-0000-0000-0000-000000000000' THEN 1 END) AS ValidDoctorId,
    COUNT(CASE WHEN DoctorId = '00000000-0000-0000-0000-000000000000' THEN 1 END) AS EmptyDoctorId,
    COUNT(CASE WHEN DoctorName IS NOT NULL THEN 1 END) AS HasDoctorName,
    COUNT(CASE WHEN PatientName IS NOT NULL THEN 1 END) AS HasPatientName
FROM MedicalCase;
