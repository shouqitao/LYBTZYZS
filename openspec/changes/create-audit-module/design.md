# Design: create-audit-module

## 一、架构设计

### 1.1 模块定位

```
┌─────────────────────────────────────────────────────────────┐
│                      Server Layer                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ MedicalCase  │  │   Patient    │  │    User      │       │
│  │   Module     │  │   Module     │  │   Module     │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         │                 │                 │               │
│         └────────────────┬┴─────────────────┘               │
│                          │ IAuditService                    │
│                          ▼                                  │
│                 ┌─────────────────┐                         │
│                 │  Audit Module   │                         │
│                 │ LYBT.Module.Audit                         │
│                 └────────┬────────┘                         │
│                          │                                  │
│                          ▼                                  │
│                 ┌─────────────────┐                         │
│                 │   AuditLogs     │                         │
│                 │     Table       │                         │
│                 └─────────────────┘                         │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 依赖关系

```
LYBT.Module.Audit (独立，无业务模块依赖)
    ↑
    │ 依赖注入
    │
├── LYBT.Module.MedicalCase
├── LYBT.Module.Patient
├── LYBT.Module.Users
└── (其他需要审计的模块)
```

## 二、数据模型

### 2.1 AuditLog实体

```csharp
public class AuditLogModel : EntityBase
{
    /// <summary>
    /// 实体类型（MedicalCase, Patient, User等）
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 操作类型（Create, Update, Delete）
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperatedAt { get; set; }

    /// <summary>
    /// 操作人ID
    /// </summary>
    public Guid OperatedBy { get; set; }

    /// <summary>
    /// 操作人名称（冗余存储，避免关联查询）
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 修改原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 修改前快照（JSON）
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// 修改后快照（JSON）
    /// </summary>
    public string? AfterSnapshot { get; set; }
}
```

### 2.2 DTO定义

```csharp
// 审计日志详情
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? BeforeSnapshot { get; set; }
    public string? AfterSnapshot { get; set; }
}

// 审计日志列表项
public class AuditLogListDto
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

// 创建审计日志
public class AuditLogCreateDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public object? BeforeSnapshot { get; set; }
    public object? AfterSnapshot { get; set; }
}
```

## 三、服务接口

### 3.1 IAuditService

```csharp
public interface IAuditService
{
    /// <summary>
    /// 记录实体创建
    /// </summary>
    Task LogCreateAsync<T>(
        string entityType,
        Guid entityId,
        T snapshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录实体更新
    /// </summary>
    Task LogUpdateAsync<T>(
        string entityType,
        Guid entityId,
        T beforeSnapshot,
        T afterSnapshot,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录实体删除
    /// </summary>
    Task LogDeleteAsync<T>(
        string entityType,
        Guid entityId,
        T snapshot,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实体审计日志列表
    /// </summary>
    Task<List<AuditLogListDto>> GetEntityLogsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    Task<AuditLogDto?> GetLogByIdAsync(
        Guid logId,
        CancellationToken cancellationToken = default);
}
```

### 3.2 实现要点

```csharp
public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public async Task LogUpdateAsync<T>(
        string entityType,
        Guid entityId,
        T beforeSnapshot,
        T afterSnapshot,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLogModel
        {
            EntityType = entityType,
            EntityId = entityId,
            OperationType = "Update",
            OperatedAt = DateTime.UtcNow,
            OperatedBy = _currentUser.UserId,
            OperatorName = _currentUser.UserName,
            Reason = reason,
            BeforeSnapshot = JsonSerializer.Serialize(beforeSnapshot),
            AfterSnapshot = JsonSerializer.Serialize(afterSnapshot)
        };

        await _repository.AddAsync(log, cancellationToken);
    }
}
```

## 四、API端点

### 4.1 Controller设计

```csharp
[ApiController]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    /// <summary>
    /// 获取实体审计日志列表
    /// </summary>
    [HttpGet("{entityType}/{entityId}")]
    public async Task<ApiResponse<List<AuditLogListDto>>> GetEntityLogs(
        string entityType,
        Guid entityId)

    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    [HttpGet("logs/{logId}")]
    public async Task<ApiResponse<AuditLogDto>> GetLogById(Guid logId)
}
```

## 五、业务模块集成

### 5.1 MedicalCase模块集成示例

```csharp
public class MedicalCaseCommandService : IMedicalCaseCommandService
{
    private readonly IAuditService _auditService;

    public async Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(
        Guid id,
        MedicalCaseInputDto input,
        CancellationToken cancellationToken)
    {
        // 1. 获取修改前数据
        var beforeData = await _queryService.GetByIdAsync(id);

        // 2. 执行保存逻辑
        // ...

        // 3. 获取修改后数据
        var afterData = await _queryService.GetByIdAsync(id);

        // 4. 记录审计（仅已完成状态的医案修改需要审计）
        if (beforeData.Status == MedicalCaseStatus.Completed)
        {
            await _auditService.LogUpdateAsync(
                entityType: "MedicalCase",
                entityId: id,
                beforeSnapshot: beforeData,
                afterSnapshot: afterData,
                reason: input.AuditReason ?? "数据修正",
                cancellationToken);
        }

        return afterData;
    }
}
```

### 5.2 审计触发时机

| 实体 | 创建时审计 | 更新时审计 | 删除时审计 |
|------|-----------|-----------|-----------|
| MedicalCase | 否 | 仅Completed状态 | 是 |
| Patient | 否 | 是 | 是 |
| User | 否 | 是 | 是 |

## 六、Desktop端集成

### 6.1 审计日志对话框

```
┌─────────────────────────────────────────────────────────────┐
│  审计日志 - 医案 #MC20251231001                    [X]      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 时间         │ 操作人  │ 类型  │ 原因              │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │ 12-31 10:30 │ 张医生  │ 修改  │ 剂量调整          │ ◀  │
│  │ 12-31 09:15 │ 张医生  │ 修改  │ 补充诊断          │    │
│  │ 12-30 14:00 │ 李医生  │ 创建  │ -                 │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 变更详情                                            │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │ 处方变更:                                           │    │
│  │   黄芪: 10g → 15g (高亮)                           │    │
│  │   + 白术: 10g (新增，绿色)                          │    │
│  │                                                     │    │
│  │ 诊断变更:                                           │    │
│  │   舌象: 舌红 → 舌红苔黄 (高亮)                      │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│                                          [关闭]             │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 Diff高亮实现

```csharp
public class AuditDiffService
{
    /// <summary>
    /// 对比两个快照，生成差异描述
    /// </summary>
    public List<DiffItem> ComputeDiff(string beforeJson, string afterJson)
    {
        var before = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeJson);
        var after = JsonSerializer.Deserialize<Dictionary<string, object>>(afterJson);

        var diffs = new List<DiffItem>();

        // 递归对比，生成差异项
        // ...

        return diffs;
    }
}

public class DiffItem
{
    public string Path { get; set; }      // 如 "Prescription.Items[0].Dosage"
    public string FieldName { get; set; } // 如 "黄芪剂量"
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DiffType Type { get; set; }    // Added, Modified, Removed
}
```

## 七、文件清单

### 7.1 新建文件

```
src/Server/Modules/LYBT.Module.Audit/
├── LYBT.Module.Audit.csproj
├── AuditModule.cs
├── Interfaces/
│   ├── IAuditService.cs
│   └── IAuditRepository.cs
├── Services/
│   └── AuditService.cs
├── Repositories/
│   └── AuditRepository.cs
└── Mapping/
    └── AuditMappingProfile.cs

src/Server/Core/LYBT.Entities/Audit/
└── AuditLogModel.cs

src/Server/Core/LYBT.Infrastructure/Configurations/
└── AuditLogConfiguration.cs

src/Shared/LYBT.Shared.Models/Contracts/Audit/
├── AuditLogDto.cs
├── AuditLogListDto.cs
└── AuditLogCreateDto.cs

src/Server/Services/LYBT.WebAPI/Controllers/
└── AuditController.cs

src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/
└── IAuditApi.cs

src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/
└── AuditDiffService.cs

src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/
├── AuditLogDialog.xaml
└── AuditLogDialog.xaml.cs

src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/
└── AuditLogDialogViewModel.cs
```

### 7.2 修改文件

```
src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs
  → 添加 DbSet<AuditLogModel>

src/Server/Services/LYBT.WebAPI/Program.cs
  → 注册 AuditModule

LYBT.All.sln
  → 添加 LYBT.Module.Audit 项目引用
```
