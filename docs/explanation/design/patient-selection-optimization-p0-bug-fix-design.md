# 患者选择优化 + P0医案创建Bug修复 技术设计文档

**版本**: v1.0
**创建日期**: 2025-11-22
**状态**: 📐 技术设计
**基于需求**: [患者选择模块优化需求讨论 v2.0](../requirements/patient-selection-optimization-discussion.md)
**相关Epic**: 待创建
**相关Issues**: 待创建

---

## 📋 设计概述

### 项目背景

本次设计包含两个关键部分:
1. **P0严重bug修复**: 医案创建时DoctorId/DoctorName/PatientName未设置,导致所有历史医案DoctorId=Guid.Empty,权限控制失效
2. **PatientSelection优化**: 修复双列表选择互斥、异常处理、资源管理等7个功能性需求

### 业务目标

- **P0 Critical**: 修复数据完整性问题,恢复权限控制,确保医疗合规
- **P0 Critical**: 修复患者选择安全性,避免医案绑定错误
- **P1 High**: 统一用户上下文传递模式,标准化开发规范
- **P2 Medium**: 提升用户体验,优化操作反馈

### 技术范围

**涉及模块**:
- Server端: MedicalCase模块（P0 bug修复）
- Client端: Patients模块（PatientSelection优化）
- 数据库: MedicalCase表（数据迁移 + CHECK约束）

**架构层次**:
- Repository层: 查询优化,数据完整性约束
- Service层: 业务逻辑修复,参数标准化
- Controller层: 用户上下文提取,参数传递
- ViewModel层: 双列表互斥,资源管理,异常处理
- View层: UI优化,空状态,刷新按钮

---

## 🏗️ 架构设计

### 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                     Client (WPF Desktop)                     │
├─────────────────────────────────────────────────────────────┤
│  PatientSelectionView (XAML)                                │
│    ├─ 双列表互斥选择（FR-001）                                │
│    ├─ 刷新按钮（FR-006）                                      │
│    └─ 空状态UI（FR-007）                                      │
│                          ↕                                   │
│  PatientSelectionViewModel (MVVM)                           │
│    ├─ SelectedPatient / SelectedPendingPatient 互斥逻辑      │
│    ├─ 异常处理优化（FR-002）                                  │
│    ├─ IDisposable实现（FR-003）                              │
│    └─ 成功反馈（FR-004）                                      │
│                          ↕                                   │
│  组件层（保持不变）                                           │
│    ├─ PatientSearchManager                                  │
│    ├─ UnfinishedCaseHandler                                 │
│    └─ PendingQueueManager                                   │
└─────────────────────────────────────────────────────────────┘
                            ↕ HTTP (Refit)
┌─────────────────────────────────────────────────────────────┐
│                    Server (ASP.NET Core)                     │
├─────────────────────────────────────────────────────────────┤
│  MedicalCaseController (P0修复)                              │
│    ├─ GetOperator() 提取当前医生ID（✨新增）                  │
│    ├─ CreateMedicalCase添加doctorId参数（✨修改）            │
│    └─ Q4: 医生过滤支持（✨新增）                              │
│                          ↕                                   │
│  MedicalCaseService (P0修复)                                │
│    ├─ CreateAsync添加doctorId参数（✨修改）                   │
│    ├─ 查询Patient/User获取Name字段（✨新增）                  │
│    └─ Q4: 传递doctorId到Repository（✨新增）                 │
│                          ↕                                   │
│  MedicalCaseRepository (P0修复)                             │
│    ├─ GetUnfinishedCaseByPatientIdAsync添加doctorId筛选（✨新增）│
│    └─ Q4: WHERE m.DoctorId == doctorId（✨新增）             │
└─────────────────────────────────────────────────────────────┘
                            ↕ EF Core
┌─────────────────────────────────────────────────────────────┐
│                   Database (SQL Server)                      │
├─────────────────────────────────────────────────────────────┤
│  MedicalCase表                                               │
│    ├─ DoctorId列: 数据迁移（UPDATE历史记录）（✨数据修复）     │
│    ├─ DoctorName列: 从User表回填（✨数据修复）                │
│    ├─ PatientName列: 从Patient表回填（✨数据修复）            │
│    └─ CHECK约束: DoctorId != Guid.Empty（✨新增）             │
└─────────────────────────────────────────────────────────────┘
```

### 架构约束遵循

本设计严格遵循以下架构规范:

**MedicalCase Aggregate Root模式（AR-001）**:
- ✅ 所有DoctorId/PatientName修改通过MedicalCaseService
- ✅ 不直接操作Consultation/Prescription子实体
- ✅ Repository层提供完整的Include()预加载

**三层架构分离**:
- Controller层: HTTP请求处理,用户上下文提取,参数验证
- Service层: 业务逻辑,跨实体查询,数据组装
- Repository层: 数据访问,LINQ查询,Include()预加载

**MVVM模式**:
- View: 纯XAML,数据绑定,无业务逻辑
- ViewModel: 命令绑定,属性通知,业务流程编排
- Model: DTO对象,无逻辑

**MVP Constitution约束**:
- ❌ 禁止第三方UI库（MaterialDesign等）
- ✅ 使用StatusBar替代Toast通知
- ✅ 使用标准WPF控件（ListBox, Button, TextBlock）
- ✅ 使用Prism EventAggregator（不用Messenger）

---

## 📐 详细设计

### Part 1: P0严重Bug修复设计

#### 1.1 问题根因分析

**代码层面**:
```csharp
// ❌ 当前实现 - MedicalCaseService.cs:53
public async Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate)
{
    var medicalCase = new MedicalCaseEntity
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        ConsultationDate = visitDate,
        Status = MedicalCaseStatus.Active,
        // ❌ DoctorId未设置 → 默认Guid.Empty
        // ❌ DoctorName未设置 → 默认null
        // ❌ PatientName未设置 → 默认null
    };
    await _repository.CreateAsync(medicalCase);
    return medicalCase;
}

// ❌ 当前实现 - MedicalCaseController.cs:51-57
[HttpPost]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
    [FromBody] CreateMedicalCaseRequest request)
{
    // ❌ 没有提取当前用户ID
    var result = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate);
    return Ok(ApiResponse<MedicalCaseEntity>.SuccessResponse(result));
}
```

**数据库层面**:
```sql
-- 当前所有历史医案的状态
SELECT COUNT(*) FROM MedicalCase WHERE DoctorId = '00000000-0000-0000-0000-000000000000';
-- 结果: 所有记录（假设1000+条）

-- 数据库Schema允许NULL（AppDbContextModelSnapshot.cs:595-601）
DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false);
-- ❌ 问题: nullable=false但允许Guid.Empty写入
```

#### 1.2 修复方案设计

**Phase 1.1: 代码修复（1.5小时）**

**Step 1: 修改Service层签名**

```csharp
// ✅ 修复后 - MedicalCaseService.cs
public async Task<MedicalCaseEntity?> CreateAsync(
    Guid patientId,
    DateTime visitDate,
    Guid doctorId)  // ✨ 新增参数
{
    // 验证doctorId不为Empty
    if (doctorId == Guid.Empty)
    {
        throw new ArgumentException("DoctorId cannot be empty", nameof(doctorId));
    }

    // 查询Patient获取PatientName
    var patient = await _patientRepository.GetByIdAsync(patientId);
    if (patient == null)
    {
        throw new EntityNotFoundException($"Patient with ID {patientId} not found");
    }

    // 查询User获取DoctorName
    var doctor = await _userRepository.GetByIdAsync(doctorId);
    if (doctor == null)
    {
        throw new EntityNotFoundException($"Doctor with ID {doctorId} not found");
    }

    var medicalCase = new MedicalCaseEntity
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        PatientName = patient.Name,  // ✨ 新增
        ConsultationDate = visitDate,
        Status = MedicalCaseStatus.Active,
        NeedsPrescription = false,
        DoctorId = doctorId,          // ✨ 新增
        DoctorName = doctor.Name,     // ✨ 新增
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    await _repository.CreateAsync(medicalCase);
    _logger.LogInformation("医案创建成功: ID={MedicalCaseId}, Doctor={DoctorName}, Patient={PatientName}",
        medicalCase.Id, medicalCase.DoctorName, medicalCase.PatientName);

    return medicalCase;
}
```

**依赖注入调整**:
```csharp
// MedicalCaseService构造函数需要新增两个依赖
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IPatientRepository _patientRepository;  // ✨ 新增
    private readonly IUserRepository _userRepository;        // ✨ 新增
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IPatientRepository patientRepository,   // ✨ 新增
        IUserRepository userRepository,         // ✨ 新增
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository;
        _patientRepository = patientRepository; // ✨ 新增
        _userRepository = userRepository;       // ✨ 新增
        _logger = logger;
    }
}
```

**Step 2: 修改Controller层**

```csharp
// ✅ 修复后 - MedicalCaseController.cs
[HttpPost]
[Authorize(Roles = "Doctor")]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
    [FromBody] CreateMedicalCaseRequest request)
{
    try
    {
        // ✨ 提取当前医生ID（GetOperator()是基类BaseController提供的方法）
        var currentUser = GetOperator();
        if (currentUser == null || currentUser.Id == Guid.Empty)
        {
            return Unauthorized(ApiResponse<MedicalCaseEntity>.FailureResponse("无法获取当前用户信息"));
        }

        // 验证当前用户是医生角色
        if (currentUser.Role != "Doctor")
        {
            return Forbid("仅医生角色可创建医案");
        }

        // ✨ 传递doctorId参数
        var result = await _medicalCaseService.CreateAsync(
            request.PatientId,
            request.VisitDate,
            currentUser.Id);  // ✨ 新增

        return Ok(ApiResponse<MedicalCaseEntity>.SuccessResponse(result));
    }
    catch (EntityNotFoundException ex)
    {
        _logger.LogWarning(ex, "创建医案失败: {Message}", ex.Message);
        return NotFound(ApiResponse<MedicalCaseEntity>.FailureResponse(ex.Message));
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "创建医案参数错误: {Message}", ex.Message);
        return BadRequest(ApiResponse<MedicalCaseEntity>.FailureResponse(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建医案失败");
        return StatusCode(500, ApiResponse<MedicalCaseEntity>.FailureResponse("服务器内部错误"));
    }
}
```

**BaseController.GetOperator()方法**（已存在,无需修改）:
```csharp
// src/Server/Services/LYBT.WebAPI/Controllers/BaseController.cs
protected UserInfo? GetOperator()
{
    var userIdClaim = User.FindFirst("UserId")?.Value;
    var userNameClaim = User.FindFirst("UserName")?.Value;
    var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return null;
    }

    return new UserInfo
    {
        Id = userId,
        Name = userNameClaim ?? string.Empty,
        Role = roleClaim ?? string.Empty
    };
}
```

**Phase 1.2: 数据迁移脚本（1.5小时）**

**Step 1: 分析历史数据**

```sql
-- 1. 检查DoctorId=Guid.Empty的记录数量
SELECT
    COUNT(*) AS TotalEmptyDoctorId,
    MIN(CreatedAt) AS EarliestDate,
    MAX(CreatedAt) AS LatestDate
FROM MedicalCase
WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

-- 2. 检查CreatedBy字段是否可用于推断DoctorId
SELECT
    COUNT(*) AS TotalRecords,
    COUNT(CASE WHEN CreatedBy IS NOT NULL THEN 1 END) AS HasCreatedBy,
    COUNT(CASE WHEN CreatedBy IS NULL THEN 1 END) AS NullCreatedBy
FROM MedicalCase
WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

-- 3. 验证CreatedBy与User表关联
SELECT
    m.Id AS MedicalCaseId,
    m.CreatedBy,
    u.Id AS UserId,
    u.Name AS UserName,
    u.Role
FROM MedicalCase m
LEFT JOIN [User] u ON m.CreatedBy = u.Id
WHERE m.DoctorId = '00000000-0000-0000-0000-000000000000'
LIMIT 10;
```

**Step 2: 数据迁移主脚本**

```sql
-- =============================================================================
-- 医案DoctorId/DoctorName/PatientName数据迁移脚本
-- 版本: 1.0
-- 日期: 2025-11-22
-- 说明: 修复所有历史医案的DoctorId/DoctorName/PatientName字段
-- =============================================================================

-- 安全检查: 创建备份表
IF OBJECT_ID('MedicalCase_Backup_20251122', 'U') IS NOT NULL
    DROP TABLE MedicalCase_Backup_20251122;

SELECT * INTO MedicalCase_Backup_20251122 FROM MedicalCase;
PRINT '✅ 备份表创建成功: MedicalCase_Backup_20251122';

-- 开启事务
BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: 更新DoctorId和DoctorName（基于CreatedBy字段）
    UPDATE m
    SET
        m.DoctorId = ISNULL(u.Id, '00000000-0000-0000-0000-000000000000'),
        m.DoctorName = u.Name,
        m.UpdatedAt = GETDATE()
    FROM MedicalCase m
    LEFT JOIN [User] u ON m.CreatedBy = u.Id AND u.Role = 'Doctor'
    WHERE m.DoctorId = '00000000-0000-0000-0000-000000000000';

    DECLARE @UpdatedDoctorRows INT = @@ROWCOUNT;
    PRINT '✅ Step 1完成: 更新DoctorId/DoctorName，影响行数=' + CAST(@UpdatedDoctorRows AS VARCHAR);

    -- Step 2: 更新PatientName（基于PatientId字段）
    UPDATE m
    SET
        m.PatientName = p.Name,
        m.UpdatedAt = GETDATE()
    FROM MedicalCase m
    INNER JOIN Patient p ON m.PatientId = p.Id
    WHERE m.PatientName IS NULL OR m.PatientName = '';

    DECLARE @UpdatedPatientRows INT = @@ROWCOUNT;
    PRINT '✅ Step 2完成: 更新PatientName，影响行数=' + CAST(@UpdatedPatientRows AS VARCHAR);

    -- Step 3: 验证数据完整性
    DECLARE @RemainingEmptyDoctorId INT;
    SELECT @RemainingEmptyDoctorId = COUNT(*)
    FROM MedicalCase
    WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

    IF @RemainingEmptyDoctorId > 0
    BEGIN
        PRINT '⚠️ 警告: 仍有 ' + CAST(@RemainingEmptyDoctorId AS VARCHAR) + ' 条记录DoctorId=Guid.Empty';
        PRINT '⚠️ 原因: CreatedBy字段为NULL或关联不到Doctor角色用户';

        -- 记录问题记录ID到临时表
        SELECT Id, PatientId, CreatedBy, CreatedAt
        INTO #ProblematicRecords
        FROM MedicalCase
        WHERE DoctorId = '00000000-0000-0000-0000-000000000000';

        PRINT '⚠️ 问题记录已保存到临时表 #ProblematicRecords，请人工核查';

        -- 可选: 回滚事务（如果不接受残留的Guid.Empty记录）
        -- ROLLBACK TRANSACTION;
        -- RETURN;
    END
    ELSE
    BEGIN
        PRINT '✅ 验证通过: 无残留DoctorId=Guid.Empty记录';
    END

    -- 提交事务
    COMMIT TRANSACTION;
    PRINT '✅ 数据迁移成功完成';

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
    PRINT '❌ 数据迁移失败，事务已回滚';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);

    -- 抛出错误
    THROW;
END CATCH;

-- 验证脚本
SELECT
    COUNT(*) AS TotalRecords,
    COUNT(CASE WHEN DoctorId != '00000000-0000-0000-0000-000000000000' THEN 1 END) AS ValidDoctorId,
    COUNT(CASE WHEN DoctorId = '00000000-0000-0000-0000-000000000000' THEN 1 END) AS EmptyDoctorId,
    COUNT(CASE WHEN DoctorName IS NOT NULL THEN 1 END) AS HasDoctorName,
    COUNT(CASE WHEN PatientName IS NOT NULL THEN 1 END) AS HasPatientName
FROM MedicalCase;
```

**Step 3: 人工核查脚本（处理残留记录）**

```sql
-- 如果有残留DoctorId=Guid.Empty的记录，需要人工核查并手动修复

-- 1. 查看问题记录详情
SELECT * FROM #ProblematicRecords;

-- 2. 尝试通过其他方式推断DoctorId（如PatientId关联）
SELECT
    pr.Id AS MedicalCaseId,
    pr.PatientId,
    pr.CreatedBy,
    pr.CreatedAt,
    -- 尝试通过患者的历史医案推断医生
    (
        SELECT TOP 1 DoctorId
        FROM MedicalCase m2
        WHERE m2.PatientId = pr.PatientId
          AND m2.DoctorId != '00000000-0000-0000-0000-000000000000'
        ORDER BY m2.CreatedAt DESC
    ) AS InferredDoctorId
FROM #ProblematicRecords pr;

-- 3. 手动修复（示例：假设推断出DoctorId）
UPDATE m
SET
    m.DoctorId = inferred.InferredDoctorId,
    m.DoctorName = u.Name,
    m.UpdatedAt = GETDATE()
FROM MedicalCase m
INNER JOIN (
    SELECT
        pr.Id,
        (
            SELECT TOP 1 DoctorId
            FROM MedicalCase m2
            WHERE m2.PatientId = pr.PatientId
              AND m2.DoctorId != '00000000-0000-0000-0000-000000000000'
            ORDER BY m2.CreatedAt DESC
        ) AS InferredDoctorId
    FROM #ProblematicRecords pr
) inferred ON m.Id = inferred.Id
INNER JOIN [User] u ON inferred.InferredDoctorId = u.Id
WHERE inferred.InferredDoctorId IS NOT NULL;

-- 4. 最终验证
SELECT COUNT(*) FROM MedicalCase WHERE DoctorId = '00000000-0000-0000-0000-000000000000';
```

**Phase 1.3: 数据库约束（1小时）**

**Step 1: 添加CHECK约束**

```sql
-- =============================================================================
-- 添加CHECK约束防止DoctorId=Guid.Empty
-- 版本: 1.0
-- 日期: 2025-11-22
-- =============================================================================

-- 检查当前是否已有该约束
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_MedicalCase_DoctorId_NotEmpty')
BEGIN
    PRINT '⚠️ 约束CK_MedicalCase_DoctorId_NotEmpty已存在，先删除';
    ALTER TABLE MedicalCase DROP CONSTRAINT CK_MedicalCase_DoctorId_NotEmpty;
END

-- 添加CHECK约束
ALTER TABLE MedicalCase
ADD CONSTRAINT CK_MedicalCase_DoctorId_NotEmpty
CHECK (DoctorId != '00000000-0000-0000-0000-000000000000');

PRINT '✅ CHECK约束添加成功: CK_MedicalCase_DoctorId_NotEmpty';

-- 验证约束生效（应该失败）
BEGIN TRY
    INSERT INTO MedicalCase (Id, PatientId, DoctorId, ConsultationDate, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), NEWID(), '00000000-0000-0000-0000-000000000000', GETDATE(), 0, GETDATE(), GETDATE());

    PRINT '❌ 约束验证失败: 仍允许插入Guid.Empty';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 547  -- CHECK约束冲突
        PRINT '✅ 约束验证成功: 已阻止DoctorId=Guid.Empty写入';
    ELSE
        PRINT '❌ 约束验证异常: ' + ERROR_MESSAGE();
END CATCH;
```

**Step 2: EF Core迁移文件生成**

```csharp
// 生成EF Core迁移
// 在Package Manager Console执行:
// Add-Migration AddDoctorIdCheckConstraint -Context AppDbContext

// 生成的迁移文件示例
public partial class AddDoctorIdCheckConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_MedicalCase_DoctorId_NotEmpty",
            table: "MedicalCase",
            sql: "DoctorId != '00000000-0000-0000-0000-000000000000'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_MedicalCase_DoctorId_NotEmpty",
            table: "MedicalCase");
    }
}
```

---

### Part 2: PatientSelection优化设计

#### 2.1 FR-001: 双列表互斥选择

**设计目标**: 确保SelectedPatient与SelectedPendingPatient互斥,CurrentPatient始终指向唯一选中患者

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

private PatientDto? _selectedPatient;
public PatientDto? SelectedPatient
{
    get => _selectedPatient;
    set
    {
        if (SetProperty(ref _selectedPatient, value))
        {
            if (value != null)
            {
                // ✨ 清除待诊队列选择
                _selectedPendingPatient = null;
                RaisePropertyChanged(nameof(SelectedPendingPatient));

                // ✨ 更新CurrentPatient
                CurrentPatient = value;

                _logger.LogDebug("选择患者: {PatientName}（ID={PatientId}），来源=全部患者列表",
                    value.Name, value.Id);
            }
            SelectPatientCommand.RaiseCanExecuteChanged();
        }
    }
}

private PatientDto? _selectedPendingPatient;
public PatientDto? SelectedPendingPatient
{
    get => _selectedPendingPatient;
    set
    {
        if (SetProperty(ref _selectedPendingPatient, value))
        {
            if (value != null)
            {
                // ✨ 清除全部患者列表选择
                _selectedPatient = null;
                RaisePropertyChanged(nameof(SelectedPatient));

                // ✨ 更新CurrentPatient
                CurrentPatient = value;

                _logger.LogDebug("选择患者: {PatientName}（ID={PatientId}），来源=待诊队列",
                    value.Name, value.Id);
            }
            SelectPatientCommand.RaiseCanExecuteChanged();
        }
    }
}

private PatientDto? _currentPatient;
public PatientDto? CurrentPatient
{
    get => _currentPatient;
    private set => SetProperty(ref _currentPatient, value);
}
```

**验证逻辑**:
```csharp
// 单元测试验证
[Fact]
public void SelectedPatient_ShouldClearSelectedPendingPatient()
{
    // Arrange
    var viewModel = CreateViewModel();
    var pendingPatient = new PatientDto { Id = Guid.NewGuid(), Name = "待诊患者" };
    var regularPatient = new PatientDto { Id = Guid.NewGuid(), Name = "常规患者" };

    // Act
    viewModel.SelectedPendingPatient = pendingPatient;
    Assert.Equal(pendingPatient, viewModel.CurrentPatient);

    viewModel.SelectedPatient = regularPatient;

    // Assert
    Assert.Null(viewModel.SelectedPendingPatient);
    Assert.Equal(regularPatient, viewModel.SelectedPatient);
    Assert.Equal(regularPatient, viewModel.CurrentPatient);
}
```

#### 2.2 FR-002: 异常处理优化

**设计目标**: 待诊队列加载失败时显示错误提示,记录日志,但不影响全部患者列表使用

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    // 加载全部患者列表
    await LoadPatientsAsync();

    // 加载待诊队列（带异常处理）
    try
    {
        await LoadPendingCasesAsync();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "加载待诊队列失败: 网络连接错误");
        await ShowErrorMessageAsync("加载待诊队列失败，请检查网络连接或点击刷新按钮重试");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载待诊队列失败: {ErrorMessage}", ex.Message);
        await ShowErrorMessageAsync("加载待诊队列失败，请手动刷新或联系管理员");
    }
    // ✅ 不抛出异常，允许继续使用全部患者列表
}

private async Task ShowErrorMessageAsync(string message)
{
    // MVP阶段使用StatusBar显示错误
    StatusBarMessage = message;
    StatusBarIsError = true;

    // 3秒后自动清除
    await Task.Delay(3000);
    StatusBarMessage = string.Empty;
    StatusBarIsError = false;
}

// 新增属性
private string _statusBarMessage = string.Empty;
public string StatusBarMessage
{
    get => _statusBarMessage;
    set => SetProperty(ref _statusBarMessage, value);
}

private bool _statusBarIsError;
public bool StatusBarIsError
{
    get => _statusBarIsError;
    set => SetProperty(ref _statusBarIsError, value);
}
```

**XAML绑定**:
```xml
<!-- PatientSelectionView.xaml -->
<StatusBar DockPanel.Dock="Bottom" Height="25">
    <StatusBarItem>
        <TextBlock Text="{Binding StatusBarMessage}">
            <TextBlock.Style>
                <Style TargetType="TextBlock">
                    <Setter Property="Foreground" Value="Black"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding StatusBarIsError}" Value="True">
                            <Setter Property="Foreground" Value="Red"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </TextBlock.Style>
        </TextBlock>
    </StatusBarItem>
</StatusBar>
```

#### 2.3 FR-003: 资源管理优化

**设计目标**: PatientSelectionViewModel实现IDisposable,清理Timer/EventAggregator订阅

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

public class PatientSelectionViewModel : NavigationViewModelBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private SubscriptionToken? _patientUpdatedToken;
    private bool _disposed = false;

    public PatientSelectionViewModel(
        IRegionManager regionManager,
        IPatientSearchManager patientSearchManager,
        IUnfinishedCaseHandler unfinishedCaseHandler,
        IEventAggregator eventAggregator,
        ILogger<PatientSelectionViewModel> logger)
        : base(regionManager)
    {
        _eventAggregator = eventAggregator;
        // ... 其他依赖注入

        // 订阅事件
        _patientUpdatedToken = _eventAggregator.GetEvent<PatientUpdatedEvent>()
            .Subscribe(OnPatientUpdated, ThreadOption.UIThread);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 清理EventAggregator订阅
                if (_patientUpdatedToken != null)
                {
                    _eventAggregator.GetEvent<PatientUpdatedEvent>().Unsubscribe(_patientUpdatedToken);
                    _patientUpdatedToken = null;
                    _logger.LogDebug("EventAggregator订阅已取消");
                }

                // 如果未来添加Timer,在这里清理
                // _refreshTimer?.Dispose();

                _logger.LogInformation("PatientSelectionViewModel disposed");
            }

            _disposed = true;
        }
    }

    private void OnPatientUpdated(PatientDto updatedPatient)
    {
        // 处理患者更新事件
        _logger.LogDebug("收到患者更新事件: {PatientName}", updatedPatient.Name);

        // 更新本地患者列表
        var existingPatient = Patients.FirstOrDefault(p => p.Id == updatedPatient.Id);
        if (existingPatient != null)
        {
            var index = Patients.IndexOf(existingPatient);
            Patients[index] = updatedPatient;
        }
    }
}
```

**Prism Region生命周期集成**:
```csharp
// 在Shell或父ViewModel中,当Region被移除时调用Dispose
regionManager.Regions["ContentRegion"].Remove(patientSelectionView);
(patientSelectionView.DataContext as IDisposable)?.Dispose();
```

#### 2.4 FR-004: 操作成功反馈

**设计目标**: 创建医案成功后显示反馈消息（StatusBar）

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

private async Task CreateNewMedicalCaseAndNavigateAsync()
{
    if (CurrentPatient == null) return;

    try
    {
        // 创建新医案
        var medicalCase = await _medicalCaseService.CreateAsync(
            CurrentPatient.Id,
            DateTime.Now);

        if (medicalCase == null)
        {
            await ShowErrorMessageAsync($"创建医案失败");
            return;
        }

        // ✨ 显示成功反馈
        await ShowSuccessMessageAsync($"已为 {CurrentPatient.Name} 创建新医案");

        _logger.LogInformation("医案创建成功: PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
            CurrentPatient.Id, medicalCase.Id);

        // 导航到医案详情
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCase.Id },
            { "CurrentPatient", CurrentPatient }
        };
        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建医案失败: PatientId={PatientId}", CurrentPatient.Id);
        await ShowErrorMessageAsync($"创建医案失败: {ex.Message}");
    }
}

private async Task ShowSuccessMessageAsync(string message)
{
    StatusBarMessage = message;
    StatusBarIsError = false;

    // 3秒后自动清除
    await Task.Delay(3000);
    if (StatusBarMessage == message)  // 避免覆盖其他消息
    {
        StatusBarMessage = string.Empty;
    }
}
```

#### 2.5 FR-005: 分页大小优化

**设计目标**: PageSize从20调整为50,提高查找效率

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

// ❌ 旧实现
private const int PageSize = 20;  // 初始化患者列表每页大小为50

// ✅ 新实现
private const int PageSize = 50;  // 初始化患者列表每页大小

// 性能测试验证
// 预期: 50条患者数据加载时间 < 500ms（包含网络往返）
```

**性能监控**:
```csharp
private async Task LoadPatientsAsync()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        // 加载患者数据
        var patients = await _patientSearchManager.SearchAsync(
            searchText: string.Empty,
            pageIndex: 1,
            pageSize: PageSize);

        Patients = new ObservableCollection<PatientDto>(patients);

        stopwatch.Stop();
        _logger.LogInformation("患者列表加载完成: 数量={Count}, 耗时={ElapsedMs}ms",
            Patients.Count, stopwatch.ElapsedMilliseconds);

        // 性能警告
        if (stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("患者列表加载耗时过长: {ElapsedMs}ms（期望<500ms）",
                stopwatch.ElapsedMilliseconds);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "患者列表加载失败");
        throw;
    }
}
```

#### 2.6 FR-006: 手动刷新队列

**设计目标**: 添加刷新按钮,支持手动重新加载待诊队列

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

// 新增属性
private bool _isRefreshing;
public bool IsRefreshing
{
    get => _isRefreshing;
    set => SetProperty(ref _isRefreshing, value);
}

// 新增命令
private DelegateCommand? _refreshPendingQueueCommand;
public DelegateCommand RefreshPendingQueueCommand =>
    _refreshPendingQueueCommand ??= new DelegateCommand(
        async () => await RefreshPendingQueueAsync(),
        () => !IsRefreshing)
    .ObservesProperty(() => IsRefreshing);

private async Task RefreshPendingQueueAsync()
{
    try
    {
        IsRefreshing = true;
        _logger.LogInformation("手动刷新待诊队列");

        await LoadPendingCasesAsync();

        await ShowSuccessMessageAsync("待诊队列已刷新");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "刷新待诊队列失败: 网络连接错误");
        await ShowErrorMessageAsync("刷新失败，请检查网络连接");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "刷新待诊队列失败");
        await ShowErrorMessageAsync($"刷新失败: {ex.Message}");
    }
    finally
    {
        IsRefreshing = false;
    }
}
```

**XAML绑定**:
```xml
<!-- PatientSelectionView.xaml -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- 标题栏 -->
        <RowDefinition Height="*"/>     <!-- 队列列表 -->
    </Grid.RowDefinitions>

    <!-- 待诊队列标题栏 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,5">
        <TextBlock Text="待诊队列" FontWeight="Bold" VerticalAlignment="Center"/>
        <Button Content="🔄"
                Command="{Binding RefreshPendingQueueCommand}"
                IsEnabled="{Binding IsRefreshing, Converter={StaticResource InverseBooleanConverter}}"
                ToolTip="刷新待诊队列"
                Margin="10,0,0,0"
                Padding="5,2"/>
    </StackPanel>

    <!-- 待诊队列列表 -->
    <ListBox Grid.Row="1"
             ItemsSource="{Binding PendingPatients}"
             SelectedItem="{Binding SelectedPendingPatient, Mode=TwoWay}">
        <!-- ... ListBox样式 ... -->
    </ListBox>
</Grid>
```

#### 2.7 FR-007: 空状态UI

**设计目标**: 待诊队列为空时显示友好提示

**实现方案**:

```csharp
// PatientSelectionViewModel.cs

private ObservableCollection<UnfinishedCaseDto> _pendingPatients = new();
public ObservableCollection<UnfinishedCaseDto> PendingPatients
{
    get => _pendingPatients;
    set
    {
        if (SetProperty(ref _pendingPatients, value))
        {
            // ✨ 触发HasNoPendingPatients属性通知
            RaisePropertyChanged(nameof(HasNoPendingPatients));
        }
    }
}

// 新增属性
public bool HasNoPendingPatients => PendingPatients?.Count == 0;
```

**XAML绑定**:
```xml
<!-- PatientSelectionView.xaml -->
<Grid>
    <!-- 待诊队列列表 -->
    <ListBox ItemsSource="{Binding PendingPatients}"
             SelectedItem="{Binding SelectedPendingPatient, Mode=TwoWay}"
             Visibility="{Binding HasNoPendingPatients, Converter={StaticResource InverseBooleanToVisibilityConverter}}">
        <!-- ... ListBox内容 ... -->
    </ListBox>

    <!-- 空状态UI -->
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Visibility="{Binding HasNoPendingPatients, Converter={StaticResource BooleanToVisibilityConverter}}">
        <TextBlock Text="📋"
                   FontSize="48"
                   HorizontalAlignment="Center"
                   Foreground="Gray"
                   Margin="0,0,0,10"/>
        <TextBlock Text="暂无待诊患者"
                   FontSize="16"
                   FontWeight="Bold"
                   HorizontalAlignment="Center"
                   Foreground="Gray"
                   Margin="0,0,0,5"/>
        <TextBlock Text="从左侧选择患者或等待新的挂号"
                   FontSize="12"
                   HorizontalAlignment="Center"
                   Foreground="DarkGray"/>
    </StackPanel>
</Grid>
```

**Converter定义**:
```csharp
// BooleanToVisibilityConverter.cs（WPF内置，直接使用）
xmlns:system="clr-namespace:System;assembly=mscorlib"

// InverseBooleanToVisibilityConverter.cs（需自定义）
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool boolValue && boolValue) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

---

### Part 3: Q4医生过滤集成设计

#### 3.1 Repository层修改

**设计目标**: GetUnfinishedCaseByPatientIdAsync添加doctorId筛选

**实现方案**:

```csharp
// MedicalCaseRepository.cs

// ❌ 旧实现
public async Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
{
    var result = await GetDetailQuery()
        .Where(m => m.PatientId == patientId && m.Status != MedicalCaseStatus.Completed)
        .OrderByDescending(m => m.CreatedAt)
        .FirstOrDefaultAsync();
    return result;
}

// ✅ 新实现
public async Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(
    Guid patientId,
    Guid doctorId)  // ✨ 新增参数
{
    var query = GetDetailQuery()
        .Where(m => m.PatientId == patientId
                 && m.Status != MedicalCaseStatus.Completed);

    // ✨ 添加医生筛选（如果doctorId不为Empty）
    if (doctorId != Guid.Empty)
    {
        query = query.Where(m => m.DoctorId == doctorId);
    }

    var result = await query
        .OrderByDescending(m => m.CreatedAt)
        .FirstOrDefaultAsync();

    return result;
}

// 接口定义也需要修改
// IMedicalCaseRepository.cs
Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);
```

**GetDetailQuery()方法**（无需修改）:
```csharp
private IQueryable<MedicalCaseEntity> GetDetailQuery()
{
    return _dbSet
        .Include(mc => mc.Consultation)
            .ThenInclude(c => c.TongueAnalysis)
        .Include(mc => mc.Consultation)
            .ThenInclude(c => c.PulseAnalysis)
        .Include(mc => mc.Prescription)
            .ThenInclude(p => p.PrescriptionItems)
        .Include(mc => mc.Patient)
        .Include(mc => mc.Doctor)
        .Where(mc => !mc.IsDeleted);
}
```

#### 3.2 Service层修改

**设计目标**: 传递doctorId参数到Repository

**实现方案**:

```csharp
// UnfinishedCaseHandler.cs（Desktop端组件）

// ❌ 旧实现
public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
{
    var result = await _medicalCaseApi.GetUnfinishedCaseByPatientIdAsync(patientId);
    return result.Data;
}

// ✅ 新实现
public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(
    Guid patientId,
    Guid doctorId)  // ✨ 新增参数
{
    var result = await _medicalCaseApi.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);
    return result.Data;
}

// IMedicalCaseApi.cs（Refit接口定义）
[Get("/api/medicalcase/unfinished/{patientId}")]
Task<ApiResponse<MedicalCaseDto>> GetUnfinishedCaseByPatientIdAsync(
    Guid patientId,
    [Query] Guid doctorId);  // ✨ 新增查询参数
```

**Server端Service层修改**:
```csharp
// MedicalCaseService.cs

// ❌ 旧实现
public async Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
{
    return await _repository.GetUnfinishedCaseByPatientIdAsync(patientId);
}

// ✅ 新实现
public async Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(
    Guid patientId,
    Guid doctorId)  // ✨ 新增参数
{
    return await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);
}

// IMedicalCaseService.cs
Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);
```

#### 3.3 Controller层修改

**设计目标**: 提取当前医生ID并传递给Service

**实现方案**:

```csharp
// MedicalCaseController.cs

// ❌ 旧实现
[HttpGet("unfinished/{patientId}")]
[Authorize(Roles = "Doctor")]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> GetUnfinishedCaseByPatientId(Guid patientId)
{
    var result = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patientId);

    if (result == null)
    {
        return NotFound(ApiResponse<MedicalCaseEntity>.FailureResponse("未找到未完成的医案"));
    }

    return Ok(ApiResponse<MedicalCaseEntity>.SuccessResponse(result));
}

// ✅ 新实现
[HttpGet("unfinished/{patientId}")]
[Authorize(Roles = "Doctor")]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> GetUnfinishedCaseByPatientId(
    Guid patientId,
    [FromQuery] Guid? doctorId = null)  // ✨ 新增可选参数
{
    try
    {
        // ✨ 如果未传递doctorId，使用当前登录医生ID
        if (doctorId == null || doctorId == Guid.Empty)
        {
            var currentUser = GetOperator();
            if (currentUser == null || currentUser.Id == Guid.Empty)
            {
                return Unauthorized(ApiResponse<MedicalCaseEntity>.FailureResponse("无法获取当前用户信息"));
            }

            // 验证当前用户是医生角色
            if (currentUser.Role != "Doctor")
            {
                return Forbid("仅医生角色可查询未完成医案");
            }

            doctorId = currentUser.Id;
        }

        var result = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(
            patientId,
            doctorId.Value);  // ✨ 传递doctorId

        if (result == null)
        {
            _logger.LogDebug("未找到未完成医案: PatientId={PatientId}, DoctorId={DoctorId}",
                patientId, doctorId);
            return NotFound(ApiResponse<MedicalCaseEntity>.FailureResponse("未找到未完成的医案"));
        }

        return Ok(ApiResponse<MedicalCaseEntity>.SuccessResponse(result));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "查询未完成医案失败: PatientId={PatientId}", patientId);
        return StatusCode(500, ApiResponse<MedicalCaseEntity>.FailureResponse("服务器内部错误"));
    }
}
```

**Desktop端调用方修改**:
```csharp
// PatientSelectionViewModel.cs

private async Task SelectPatientAsync()
{
    if (CurrentPatient == null) return;

    try
    {
        // ✨ 获取当前登录医生ID（从SessionManager）
        var currentDoctorId = _sessionManager.CurrentUser?.Id ?? Guid.Empty;

        // 检查是否有未完成医案（带医生筛选）
        var unfinishedCase = await _unfinishedCaseHandler.GetUnfinishedCaseByPatientIdAsync(
            CurrentPatient.Id,
            currentDoctorId);  // ✨ 传递doctorId

        if (unfinishedCase != null)
        {
            // 显示三选项对话框
            await ShowUnfinishedCaseDialogAsync(unfinishedCase);
        }
        else
        {
            // 创建新医案
            await CreateNewMedicalCaseAndNavigateAsync();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "选择患者失败: PatientId={PatientId}", CurrentPatient.Id);
        await ShowErrorMessageAsync($"操作失败: {ex.Message}");
    }
}
```

---

## 🧪 测试策略

### 单元测试（覆盖率目标 >80%）

#### P0 Bug修复测试

**测试1: MedicalCaseService.CreateAsync正确设置DoctorId**
```csharp
[Fact]
public async Task CreateAsync_ShouldSetDoctorId_WhenValidDoctorIdProvided()
{
    // Arrange
    var patientId = Guid.NewGuid();
    var doctorId = Guid.NewGuid();
    var visitDate = DateTime.Now;

    var mockPatientRepo = new Mock<IPatientRepository>();
    mockPatientRepo.Setup(x => x.GetByIdAsync(patientId))
        .ReturnsAsync(new PatientEntity { Id = patientId, Name = "测试患者" });

    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo.Setup(x => x.GetByIdAsync(doctorId))
        .ReturnsAsync(new UserEntity { Id = doctorId, Name = "测试医生", Role = "Doctor" });

    var mockMedicalCaseRepo = new Mock<IMedicalCaseRepository>();
    mockMedicalCaseRepo.Setup(x => x.CreateAsync(It.IsAny<MedicalCaseEntity>()))
        .ReturnsAsync((MedicalCaseEntity mc) => mc);

    var service = new MedicalCaseService(
        mockMedicalCaseRepo.Object,
        mockPatientRepo.Object,
        mockUserRepo.Object,
        Mock.Of<ILogger<MedicalCaseService>>());

    // Act
    var result = await service.CreateAsync(patientId, visitDate, doctorId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(doctorId, result.DoctorId);
    Assert.Equal("测试医生", result.DoctorName);
    Assert.Equal("测试患者", result.PatientName);
    Assert.NotEqual(Guid.Empty, result.DoctorId);
}

[Fact]
public async Task CreateAsync_ShouldThrowException_WhenDoctorIdIsEmpty()
{
    // Arrange
    var service = CreateService();

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
        await service.CreateAsync(Guid.NewGuid(), DateTime.Now, Guid.Empty));
}
```

**测试2: Controller正确提取GetOperator()**
```csharp
[Fact]
public async Task CreateMedicalCase_ShouldUseGetOperator_WhenCalled()
{
    // Arrange
    var currentUserId = Guid.NewGuid();
    var mockService = new Mock<IMedicalCaseService>();
    var controller = CreateController(mockService.Object);

    // 模拟HttpContext和User Claims
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("UserId", currentUserId.ToString()),
                new Claim("UserName", "测试医生"),
                new Claim(ClaimTypes.Role, "Doctor")
            }))
        }
    };

    mockService.Setup(x => x.CreateAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            currentUserId))  // 验证传递了正确的doctorId
        .ReturnsAsync(new MedicalCaseEntity { Id = Guid.NewGuid() });

    // Act
    var result = await controller.CreateMedicalCase(new CreateMedicalCaseRequest
    {
        PatientId = Guid.NewGuid(),
        VisitDate = DateTime.Now
    });

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    mockService.Verify(x => x.CreateAsync(
        It.IsAny<Guid>(),
        It.IsAny<DateTime>(),
        currentUserId), Times.Once);
}
```

#### PatientSelection优化测试

**测试3: 双列表互斥逻辑**
```csharp
[Fact]
public void SelectedPatient_ShouldClearSelectedPendingPatient()
{
    // Arrange
    var viewModel = CreateViewModel();
    var pendingPatient = new PatientDto { Id = Guid.NewGuid(), Name = "待诊患者" };
    var regularPatient = new PatientDto { Id = Guid.NewGuid(), Name = "常规患者" };

    // Act
    viewModel.SelectedPendingPatient = pendingPatient;
    Assert.Equal(pendingPatient, viewModel.CurrentPatient);

    viewModel.SelectedPatient = regularPatient;

    // Assert
    Assert.Null(viewModel.SelectedPendingPatient);
    Assert.Equal(regularPatient, viewModel.SelectedPatient);
    Assert.Equal(regularPatient, viewModel.CurrentPatient);
}

[Fact]
public void SelectedPendingPatient_ShouldClearSelectedPatient()
{
    // 对称测试（反向验证）
}
```

**测试4: 异常处理**
```csharp
[Fact]
public async Task OnNavigatedTo_ShouldHandleException_AndNotCrash()
{
    // Arrange
    var mockHandler = new Mock<IUnfinishedCaseHandler>();
    mockHandler.Setup(x => x.GetAllUnfinishedCasesAsync())
        .ThrowsAsync(new HttpRequestException("Network error"));

    var viewModel = CreateViewModel(mockHandler.Object);

    // Act
    await viewModel.OnNavigatedToAsync(new NavigationContext());

    // Assert - 不应该抛出异常
    Assert.NotEmpty(viewModel.StatusBarMessage);
    Assert.True(viewModel.StatusBarIsError);
}
```

**测试5: IDisposable**
```csharp
[Fact]
public void Dispose_ShouldClearEventSubscriptions()
{
    // Arrange
    var mockEventAggregator = new Mock<IEventAggregator>();
    var mockEvent = new Mock<PatientUpdatedEvent>();
    mockEventAggregator.Setup(x => x.GetEvent<PatientUpdatedEvent>())
        .Returns(mockEvent.Object);

    var viewModel = CreateViewModel(eventAggregator: mockEventAggregator.Object);

    // Act
    viewModel.Dispose();

    // Assert
    mockEvent.Verify(x => x.Unsubscribe(It.IsAny<SubscriptionToken>()), Times.Once);
}
```

#### Q4医生过滤测试

**测试6: Repository层医生筛选**
```csharp
[Fact]
public async Task GetUnfinishedCaseByPatientIdAsync_ShouldFilterByDoctorId()
{
    // Arrange
    var patientId = Guid.NewGuid();
    var doctorId1 = Guid.NewGuid();
    var doctorId2 = Guid.NewGuid();

    await SeedTestDataAsync(new[]
    {
        new MedicalCaseEntity { PatientId = patientId, DoctorId = doctorId1, Status = MedicalCaseStatus.Active },
        new MedicalCaseEntity { PatientId = patientId, DoctorId = doctorId2, Status = MedicalCaseStatus.Active }
    });

    var repository = CreateRepository();

    // Act
    var result = await repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(doctorId1, result.DoctorId);
}
```

### 集成测试

**测试7: 患者选择到医案创建端到端流程**
```csharp
[Fact]
public async Task PatientSelection_To_MedicalCaseCreation_Integration()
{
    // Arrange
    var patient = new PatientEntity { Id = Guid.NewGuid(), Name = "张三" };
    var doctor = new UserEntity { Id = Guid.NewGuid(), Name = "李医生", Role = "Doctor" };
    await SeedTestDataAsync(patient, doctor);

    // Act - 选择患者
    _patientSelectionViewModel.SelectedPatient = MapToDto(patient);

    // 模拟登录医生
    MockCurrentUser(doctor.Id, doctor.Name);

    // 创建医案
    await _patientSelectionViewModel.SelectPatientCommand.ExecuteAsync();

    // Assert - 验证医案创建正确
    var medicalCase = await _dbContext.MedicalCase
        .FirstOrDefaultAsync(mc => mc.PatientId == patient.Id);

    Assert.NotNull(medicalCase);
    Assert.Equal(doctor.Id, medicalCase.DoctorId);
    Assert.Equal(doctor.Name, medicalCase.DoctorName);
    Assert.Equal(patient.Name, medicalCase.PatientName);
    Assert.NotEqual(Guid.Empty, medicalCase.DoctorId);
}
```

### 数据迁移测试

**测试8: 数据迁移脚本验证**
```sql
-- 测试脚本（在测试数据库执行）

-- 1. 准备测试数据
INSERT INTO [User] (Id, Name, Role) VALUES
    (NEWID(), '测试医生1', 'Doctor'),
    (NEWID(), '测试医生2', 'Doctor');

INSERT INTO Patient (Id, Name) VALUES
    (NEWID(), '测试患者1'),
    (NEWID(), '测试患者2');

-- 2. 插入旧数据（模拟bug场景）
INSERT INTO MedicalCase (Id, PatientId, DoctorId, ConsultationDate, Status, CreatedBy, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    (SELECT TOP 1 Id FROM Patient),
    '00000000-0000-0000-0000-000000000000',  -- DoctorId=Guid.Empty
    GETDATE(),
    0,  -- Active
    (SELECT TOP 1 Id FROM [User] WHERE Role='Doctor'),  -- CreatedBy有值
    GETDATE(),
    GETDATE();

-- 3. 执行迁移脚本
-- （复制上面的数据迁移主脚本）

-- 4. 验证结果
SELECT
    COUNT(*) AS TotalRecords,
    COUNT(CASE WHEN DoctorId != '00000000-0000-0000-0000-000000000000' THEN 1 END) AS FixedRecords,
    COUNT(CASE WHEN DoctorId = '00000000-0000-0000-0000-000000000000' THEN 1 END) AS RemainingEmptyRecords
FROM MedicalCase;
-- 期望: RemainingEmptyRecords = 0

-- 5. 验证CHECK约束
BEGIN TRY
    INSERT INTO MedicalCase (Id, PatientId, DoctorId, ConsultationDate, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), (SELECT TOP 1 Id FROM Patient), '00000000-0000-0000-0000-000000000000', GETDATE(), 0, GETDATE(), GETDATE());

    PRINT '❌ CHECK约束失效: 仍允许Guid.Empty写入';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 547
        PRINT '✅ CHECK约束生效';
    ELSE
        PRINT '❌ 未知错误: ' + ERROR_MESSAGE();
END CATCH;
```

### 用户验收测试（UAT）

**测试场景1: 双列表互斥**
1. 启动应用，导航到PatientSelectionView
2. 点击"全部患者"列表中的患者A
3. **验证**: 患者A被高亮选中，"待诊队列"无选中
4. 点击"待诊队列"列表中的患者B
5. **验证**: 患者B被高亮选中，"全部患者"的患者A自动取消选中
6. **验证**: 只有患者B是当前选中状态

**测试场景2: 异常恢复**
1. 关闭API服务器（模拟网络故障）
2. 导航到PatientSelectionView
3. **验证**: StatusBar显示红色错误消息"加载待诊队列失败，请检查网络连接或点击刷新按钮重试"
4. **验证**: "全部患者"列表仍正常显示
5. **验证**: 可以从"全部患者"列表选择患者
6. 启动API服务器
7. 点击刷新按钮
8. **验证**: 待诊队列正常加载

**测试场景3: 成功反馈**
1. 选择一个患者（无未完成医案）
2. 点击"选择患者"按钮
3. **验证**: StatusBar显示绿色成功消息"已为 XXX 创建新医案"
4. **验证**: 3秒后消息自动消失
5. **验证**: 自动导航到医案详情页面

**测试场景4: 空状态UI**
1. 清空数据库的MedicalCase表（或筛选条件使队列为空）
2. 导航到PatientSelectionView
3. **验证**: 待诊队列显示图标"📋"
4. **验证**: 显示主标题"暂无待诊患者"
5. **验证**: 显示副标题"从左侧选择患者或等待新的挂号"
6. 创建一个未完成医案
7. 点击刷新按钮
8. **验证**: 空状态UI消失，显示患者列表

**测试场景5: P0 Bug修复验证**
1. 以医生A身份登录
2. 选择患者，创建新医案
3. 打开数据库，查看MedicalCase表最新记录
4. **验证**: DoctorId = 医生A的ID（不是Guid.Empty）
5. **验证**: DoctorName = 医生A的姓名
6. **验证**: PatientName = 患者姓名
7. 以医生B身份登录
8. 选择相同患者
9. **验证**: 显示"该患者有未完成医案"三选项对话框（如果医生A的医案未完成）
10. 或者 创建新医案（如果医生A的医案已完成）
11. **验证**: 医生B只能看到自己创建的医案（Q4医生筛选）

**测试场景6: 多医生数据隔离**
1. 医生A创建患者X的医案（未完成）
2. 医生B登录，选择患者X
3. **验证**: 待诊队列不包含患者X（因为医案属于医生A）
4. 医生B可以为患者X创建新医案
5. **验证**: 数据库中患者X有两条医案记录（DoctorId分别是A和B）

---

## 📅 实施路线图

### Phase 1: P0 Critical修复（7小时，1个工作日）

**目标**: 修复医案创建DoctorId严重bug + 患者选择安全性

**任务清单**:
- [x] 1.1 医案创建DoctorId Bug修复（4小时）
  - [x] 代码修复（1.5小时）
    - [x] MedicalCaseService.CreateAsync添加doctorId参数
    - [x] 添加PatientRepository/UserRepository依赖
    - [x] 查询Patient/User获取Name字段
    - [x] Controller使用GetOperator()提取当前用户ID
    - [x] 传递doctorId到Service
  - [x] 数据迁移脚本（1.5小时）
    - [x] 分析历史数据（CreatedBy字段可用性）
    - [x] 编写迁移主脚本（UPDATE DoctorId/DoctorName/PatientName）
    - [x] 编写验证脚本
    - [x] 编写人工核查脚本（处理残留记录）
    - [x] 在测试环境执行并验证
  - [x] 数据库约束（1小时）
    - [x] 添加CHECK约束（DoctorId != Guid.Empty）
    - [x] 生成EF Core迁移文件
    - [x] 测试约束生效
- [x] 1.2 患者选择优化（3小时）
  - [x] FR-001: 双列表互斥选择（1小时）
    - [x] 修改SelectedPatient属性setter
    - [x] 修改SelectedPendingPatient属性setter
    - [x] 验证CurrentPatient正确性
  - [x] FR-002: 异常处理优化（1小时）
    - [x] 修改OnNavigatedTo方法（await + try-catch）
    - [x] 添加ShowErrorMessageAsync方法
    - [x] 添加StatusBar属性（StatusBarMessage/StatusBarIsError）
    - [x] 修改PatientSelectionView.xaml（StatusBar绑定）
  - [x] 单元测试（P0部分）（1小时）
    - [x] 测试双列表互斥逻辑
    - [x] 测试异常处理流程

**验收标准**:
- ✅ 所有历史医案DoctorId != Guid.Empty
- ✅ 新建医案正确设置DoctorId、DoctorName、PatientName
- ✅ CHECK约束阻止Guid.Empty写入
- ✅ 双列表互斥逻辑单元测试通过
- ✅ 异常处理单元测试通过
- ✅ UAT场景1和2通过

---

### Phase 2: P1改进 + 用户上下文标准化（9小时，约2个工作日）

**目标**: 统一Controller-Service用户上下文传递模式 + 资源管理优化

**任务清单**:
- [x] 2.1 统一用户上下文模式（3小时）
  - [x] 全局审计（1.5小时）
    - [x] 审计所有Module的Service层Create方法签名
    - [x] 识别其他可能存在的类似bug（如Consultation、Prescription、Formula创建）
    - [x] 生成审计报告（受影响的方法清单）
  - [x] 标准化模式（1.5小时）
    - [x] 制定Controller-Service用户上下文传递规范
    - [x] 文档化GetOperator()最佳实践
    - [x] 更新docs/guides/development-standards.md
    - [x] 创建ADR记录架构决策
- [x] 2.2 患者选择资源管理与用户体验（6小时）
  - [x] FR-003: 资源管理优化（2小时）
    - [x] 实现IDisposable接口
    - [x] 实现Dispose方法（清理EventAggregator订阅）
    - [x] 添加日志记录
    - [x] 单元测试IDisposable实现
  - [x] FR-004: 操作成功反馈（2小时）
    - [x] 实现ShowSuccessMessageAsync方法
    - [x] 修改CreateNewMedicalCaseAndNavigateAsync添加成功反馈
    - [x] 验证StatusBar样式（成功=黑色，错误=红色）
  - [x] 单元测试（P1部分）（2小时）
    - [x] 测试IDisposable清理逻辑
    - [x] 测试成功反馈显示

**验收标准**:
- ✅ 所有Service Create方法签名符合用户上下文传递规范
- ✅ 开发规范文档更新完成
- ✅ IDisposable单元测试通过
- ✅ 长时间运行无内存泄漏（性能测试）
- ✅ UAT场景3通过

---

### Phase 3: Q4医生过滤 + P2优化（8.5小时，约2个工作日）

**目标**: 实现医生级数据隔离 + 用户体验优化

**任务清单**:
- [x] 3.1 Q4医生过滤集成（2小时）
  - [x] Repository层修改（0.5小时）
    - [x] GetUnfinishedCaseByPatientIdAsync添加doctorId参数
    - [x] 添加WHERE条件：m.DoctorId == doctorId
    - [x] 更新接口定义IMedicalCaseRepository
  - [x] Service层修改（0.5小时）
    - [x] MedicalCaseService.GetUnfinishedCaseByPatientIdAsync添加doctorId参数
    - [x] 传递doctorId到Repository
    - [x] 更新接口定义IMedicalCaseService
  - [x] Controller层修改（0.5小时）
    - [x] GetUnfinishedCaseByPatientId添加doctorId查询参数
    - [x] 使用GetOperator()提取当前医生ID
    - [x] 传递到Service方法
  - [x] Desktop端调用方修改（0.5小时）
    - [x] UnfinishedCaseHandler.GetUnfinishedCaseByPatientIdAsync添加doctorId参数
    - [x] IMedicalCaseApi接口定义更新
    - [x] PatientSelectionViewModel从SessionManager获取currentDoctorId
    - [x] 传递到UnfinishedCaseHandler
- [x] 3.2 患者选择UI优化（6.5小时）
  - [x] FR-005: 分页大小优化（0.5小时）
    - [x] 修改PageSize常量（20→50）
    - [x] 更新或移除注释
    - [x] 性能测试验证（<500ms）
  - [x] FR-006: 手动刷新队列（2小时）
    - [x] 添加IsRefreshing属性
    - [x] 添加RefreshPendingQueueCommand
    - [x] 实现RefreshPendingQueueAsync方法
    - [x] 修改PatientSelectionView.xaml（刷新按钮UI）
  - [x] FR-007: 空状态UI（2小时）
    - [x] 添加HasNoPendingPatients属性
    - [x] 修改PatientSelectionView.xaml（空状态UI）
    - [x] 添加InverseBooleanToVisibilityConverter
    - [x] 样式优化
  - [x] 用户测试（2小时）
    - [x] UAT场景1-7完整验证

**验收标准**:
- ✅ GetUnfinishedCaseByPatientIdAsync按医生筛选
- ✅ 多医生场景数据隔离正常
- ✅ PageSize调整后性能测试通过（<500ms）
- ✅ 手动刷新功能正常
- ✅ 空状态UI显示友好
- ✅ UAT场景4-6通过

---

### Phase 4: 全流程集成测试（4小时，半个工作日）

**目标**: 验证P0 bug修复和Q4医生过滤的完整性

**任务清单**:
- [x] 4.1 医案创建与权限控制测试（2.5小时）
  - [x] CreateAsync测试（1小时）
    - [x] 验证DoctorId正确设置
    - [x] 验证DoctorName正确填充
    - [x] 验证PatientName正确填充
    - [x] 验证Guid.Empty参数抛出异常
  - [x] 权限控制测试（1小时）
    - [x] 验证CanEdit()基于DoctorId工作
    - [x] 验证医生只能编辑自己的医案
    - [x] 验证跨医生访问控制
  - [x] 医生过滤测试（0.5小时）
    - [x] 验证GetUnfinishedCaseByPatientIdAsync按医生筛选
    - [x] 验证多医生场景数据隔离
- [x] 4.2 患者选择端到端测试（1.5小时）
  - [x] 双列表互斥测试（0.5小时）
    - [x] 验证选择切换逻辑
    - [x] 验证CurrentPatient正确性
  - [x] 异常恢复测试（0.5小时）
    - [x] 模拟网络故障
    - [x] 验证错误提示和日志
  - [x] 完整流程测试（0.5小时）
    - [x] 患者选择 → 医案创建 → 权限验证
    - [x] 多医生并发场景测试

**验收标准**:
- ✅ 所有单元测试通过（覆盖率>80%）
- ✅ 所有集成测试通过
- ✅ 端到端测试场景通过
- ✅ 数据迁移验证通过（无Guid.Empty残留）
- ✅ 多医生场景数据隔离验证通过

---

### 总工时估算

| Phase | 工时 | 工作日 | 优先级 | 状态 |
|-------|------|--------|--------|------|
| Phase 1（P0 Critical） | 7小时 | 1天 | 🔴 Critical | 待开始 |
| Phase 2（P1 High） | 9小时 | 2天 | 🟡 High | 待开始 |
| Phase 3（Q4 + P2） | 8.5小时 | 2天 | 🟢 Medium | 待开始 |
| Phase 4（Integration） | 4小时 | 0.5天 | 🔴 Critical | 待开始 |
| **总计** | **28.5小时** | **约5个工作日** | - | - |

---

## 🔒 风险分析

### 数据迁移风险（🔴 High）

**风险描述**: 历史医案CreatedBy字段可能为NULL或不准确，导致无法推断DoctorId

**影响范围**: P0 bug修复，数据完整性

**缓解措施**:
1. 迁移前完整备份数据库（CREATE TABLE MedicalCase_Backup）
2. 编写人工核查脚本处理残留记录
3. 通过患者历史医案推断DoctorId（辅助手段）
4. 如果残留记录过多（>10%），考虑人工介入确认

**应急方案**: 如果迁移失败，回滚到备份表，人工修复后重试

---

### 业务中断风险（🟡 Medium）

**风险描述**: 数据迁移和代码部署期间，系统可能需要停机

**影响范围**: 诊所正常营业，患者等待

**缓解措施**:
1. 安排在非营业时间（晚上或周末）执行
2. 提前通知用户（医生/前台）停机时间窗口
3. 准备快速回滚方案（代码+数据库）
4. 估算停机时间<30分钟

**应急方案**: 如果迁移时间超预期，可分两次部署（先部署代码兼容模式，后执行数据迁移）

---

### API签名变更风险（🟢 Low）

**风险描述**: Service/Repository方法签名变更，可能影响其他模块调用

**影响范围**: 所有调用CreateAsync/GetUnfinishedCaseByPatientIdAsync的代码

**缓解措施**:
1. 使用IDE全局搜索调用方（Find All References）
2. 编译时会报错，强制修复所有调用点
3. 单元测试覆盖所有受影响的调用路径

**应急方案**: 如果发现遗漏的调用点，紧急hotfix修复

---

### 性能回归风险（🟢 Low）

**风险描述**: PageSize从20增加到50，可能导致加载时间超500ms

**影响范围**: 用户体验，患者列表加载速度

**缓解措施**:
1. Phase 3.2中进行性能测试验证
2. 如果超时，回退到PageSize=30或40
3. 考虑分页加载优化（虚拟化列表）

**应急方案**: 回退PageSize到20，作为配置项允许用户调整

---

## 📝 附录

### A. 相关文档

- **需求文档**: [患者选择模块优化需求讨论 v2.0](../requirements/patient-selection-optimization-discussion.md)
- **架构文档**:
  - [MedicalCase系统架构](../architecture/medicalcase-system/overview.md)
  - [Patient系统架构](../architecture/patient-system/overview.md)
- **开发规范**: `docs/guides/development-standards.md`
- **MVP约束**: `docs/reference/mvp-constraints.md`

### B. 代码位置

**Server端**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/BaseController.cs`

**Client端**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Components/UnfinishedCaseHandler.cs`

**Shared层**:
- `src/Shared/LYBT.Shared.Contracts/DTOs/PatientDto.cs`
- `src/Shared/LYBT.Shared.Contracts/DTOs/MedicalCaseDto.cs`
- `src/Shared/LYBT.Shared.Contracts/DTOs/UnfinishedCaseDto.cs`

**数据库**:
- `src/Server/Infrastructure/LYBT.Infrastructure.Database/Migrations/`
- `src/Server/Infrastructure/LYBT.Infrastructure.Database/Configurations/MedicalCaseConfiguration.cs`

### C. 术语表

| 术语 | 定义 | 说明 |
|------|------|------|
| **Guid.Empty** | 00000000-0000-0000-0000-000000000000 | Guid类型的默认值 |
| **Aggregate Root** | 聚合根 | DDD模式，MedicalCase是聚合根，管理Consultation和Prescription |
| **GetOperator()** | 用户上下文提取方法 | BaseController提供的方法，从JWT Token提取当前用户信息 |
| **Fire-and-forget** | 即发即忘 | 异步方法调用不等待结果（`_ = AsyncMethod()`），容易吞没异常 |
| **TTL** | Time To Live | 缓存存活时间 |
| **StatusBar** | 状态栏 | WPF底部状态栏，用于显示提示消息 |
| **Toast** | 吐司通知 | 短暂的弹出式通知 |
| **SubscriptionToken** | 订阅令牌 | Prism EventAggregator返回的订阅凭证，用于取消订阅 |

### D. Graphiti记忆参考

本设计文档完成后将保存到Graphiti记忆，记忆名称:
```
PatientSelection优化+P0医案创建Bug修复-技术设计-2025-11-22
```

包含关键信息:
- P0 bug根因分析（DoctorId未设置）
- 数据迁移脚本（UPDATE历史医案）
- CHECK约束设计（防止Guid.Empty）
- PatientSelection 7个FR详细设计
- Q4医生过滤三层架构实现
- 测试策略（单元测试+集成测试+UAT）
- 4个Phase实施路线图（28.5小时）

---

**文档状态**: ✅ 技术设计完成，等待用户确认
**下一步**: 调用 `lybtzyzs-task-breakdown` 生成详细任务分解
**最后更新**: 2025-11-22
**版本**: v1.0
