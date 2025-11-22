-- ============================================================================
-- Issue #2214: 添加CHECK约束防止MedicalCases.DoctorId = Guid.Empty
-- Epic #2210 Phase 1 - P0 Critical Bug修复
--
-- 目的:
--   在数据库层面添加CHECK约束，防止DoctorId字段被设置为Guid.Empty
--   ('00000000-0000-0000-0000-000000000000')
--
-- 约束名称: CK_MedicalCases_DoctorId_NotEmpty
--
-- 约束逻辑:
--   DoctorId != '00000000-0000-0000-0000-000000000000'
--
-- 前置条件:
--   执行此脚本前必须先执行Issue #2213的数据迁移脚本,
--   确保所有历史数据的DoctorId已被正确修复
--
-- 执行环境: SQL Server
-- 创建日期: 2025-11-22
-- 作者: Claude Code (Issue #2214)
-- ============================================================================

SET NOCOUNT ON;
GO

DECLARE @ConstraintName NVARCHAR(128) = 'CK_MedicalCases_DoctorId_NotEmpty';
DECLARE @TableName NVARCHAR(128) = 'MedicalCases';
DECLARE @StartTime DATETIME2 = GETDATE();

PRINT '========================================';
PRINT 'Issue #2214: 添加DoctorId CHECK约束';
PRINT '开始时间: ' + CONVERT(VARCHAR(23), @StartTime, 121);
PRINT '========================================';
PRINT '';

-- 检查约束是否已存在（幂等性检查）
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = @ConstraintName
      AND parent_object_id = OBJECT_ID(@TableName)
)
BEGIN
    PRINT '⚠️  约束 [' + @ConstraintName + '] 已存在，跳过创建。';
    PRINT '';
    PRINT '========================================';
    PRINT '脚本执行完成（无需操作）';
    PRINT '========================================';
END
ELSE
BEGIN
    BEGIN TRY
        -- 前置检查：验证是否还有DoctorId为Guid.Empty的记录
        DECLARE @InvalidRowCount INT;
        SELECT @InvalidRowCount = COUNT(*)
        FROM MedicalCases
        WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

        IF @InvalidRowCount > 0
        BEGIN
            PRINT '❌ 错误: 检测到 ' + CAST(@InvalidRowCount AS VARCHAR(10)) + ' 条DoctorId为Guid.Empty的记录!';
            PRINT '';
            PRINT '请先执行 Issue #2213 的数据迁移脚本:';
            PRINT '  20251122_Fix_MedicalCase_DoctorId_Issue2213.sql';
            PRINT '';
            PRINT '修复历史数据后再执行此脚本。';
            PRINT '';
            PRINT '⚠️  脚本执行中止。';

            -- 抛出错误
            RAISERROR('无法添加CHECK约束: 表中存在违反约束的数据', 16, 1);
            RETURN;
        END

        PRINT '✅ 前置检查通过: 未发现DoctorId为Guid.Empty的记录。';
        PRINT '';

        -- 添加CHECK约束
        PRINT '正在添加CHECK约束...';

        ALTER TABLE MedicalCases
        ADD CONSTRAINT CK_MedicalCases_DoctorId_NotEmpty
        CHECK (DoctorId != '00000000-0000-0000-0000-000000000000');

        PRINT '✅ CHECK约束添加成功!';
        PRINT '';

        -- 验证约束
        IF EXISTS (
            SELECT 1
            FROM sys.check_constraints
            WHERE name = @ConstraintName
              AND parent_object_id = OBJECT_ID(@TableName)
              AND is_disabled = 0
        )
        BEGIN
            PRINT '✅ 约束验证通过:';
            PRINT '  - 约束名称: ' + @ConstraintName;
            PRINT '  - 表名: ' + @TableName;
            PRINT '  - 约束状态: 已启用';
            PRINT '  - 约束定义: DoctorId != ''00000000-0000-0000-0000-000000000000''';
        END
        ELSE
        BEGIN
            PRINT '⚠️  警告: 约束创建成功但验证失败，请检查约束状态。';
        END

        DECLARE @EndTime DATETIME2 = GETDATE();
        DECLARE @Duration INT = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

        PRINT '';
        PRINT '========================================';
        PRINT 'CHECK约束添加完成';
        PRINT '结束时间: ' + CONVERT(VARCHAR(23), @EndTime, 121);
        PRINT '耗时: ' + CAST(@Duration AS VARCHAR(10)) + ' 毫秒';
        PRINT '========================================';

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        PRINT '';
        PRINT '❌ 错误: CHECK约束添加失败!';
        PRINT '错误消息: ' + @ErrorMessage;
        PRINT '错误严重性: ' + CAST(@ErrorSeverity AS VARCHAR(10));
        PRINT '错误状态: ' + CAST(@ErrorState AS VARCHAR(10));
        PRINT '';

        -- 重新抛出错误
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END

SET NOCOUNT OFF;
GO

-- ============================================================================
-- 约束测试（可选执行）
-- ============================================================================
PRINT '';
PRINT '========================================';
PRINT '约束测试';
PRINT '========================================';
PRINT '尝试插入违反约束的测试数据...';

BEGIN TRY
    -- 尝试插入一条DoctorId为Guid.Empty的测试记录（应该失败）
    INSERT INTO MedicalCases (
        Id,
        PatientId,
        PatientName,
        ConsultationDate,
        Status,
        DoctorId,
        DoctorName,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        NEWID(),
        NEWID(),
        'Test Patient',
        GETDATE(),
        0, -- MedicalCaseStatus.Active
        '00000000-0000-0000-0000-000000000000', -- 违反约束
        'Test Doctor',
        GETDATE(),
        GETDATE()
    );

    PRINT '❌ 测试失败: 约束未生效，允许插入Guid.Empty!';

END TRY
BEGIN CATCH
    -- 预期会抛出CHECK约束违反异常
    IF ERROR_NUMBER() = 547 -- CHECK约束冲突
    BEGIN
        PRINT '✅ 测试成功: 约束正常工作，拒绝了Guid.Empty!';
        PRINT '错误消息: ' + ERROR_MESSAGE();
    END
    ELSE
    BEGIN
        PRINT '⚠️  测试异常: 发生了意外错误';
        PRINT '错误消息: ' + ERROR_MESSAGE();
    END
END CATCH

PRINT '';
PRINT '========================================';
PRINT '约束测试完成';
PRINT '========================================';
GO
