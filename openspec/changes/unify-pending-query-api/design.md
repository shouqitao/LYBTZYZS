# unify-pending-query-api Design

## Architecture Overview

### 当前架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        Desktop Client                            │
├──────────────────────────┬──────────────────────────────────────┤
│  PendingQueueManager     │  PatientSelectionViewModel           │
│  (doctorId - 正确)       │  (patientId - BUG!)                  │
│          │               │           │                          │
│          ▼               │           ▼                          │
│  IMedicalCaseApi.GetPendingCasesAsync(Guid doctorId)            │
└──────────────────────────┴──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Server API                                │
├─────────────────────────────────────────────────────────────────┤
│  GET /api/v1/medicalcases/pending?doctorId={id}                 │
│          │                                                       │
│          ▼                                                       │
│  MedicalCaseRepository.GetPendingCasesAsync(doctorId)           │
│          │                                                       │
│          ▼                                                       │
│  返回 List<PendingMedicalCaseDto>                                │
│  - Id, PatientName, Type(PendingCaseType), CreatedAt            │
└─────────────────────────────────────────────────────────────────┘
```

### 目标架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        Desktop Client                            │
├──────────────────────────┬──────────────────────────────────────┤
│  PendingQueueManager     │  PatientSelectionViewModel           │
│  (doctorId)              │  (doctorId + patientId)              │
│          │               │           │                          │
│          ▼               │           ▼                          │
│  IMedicalCaseApi.GetPendingCasesAsync(doctorId, patientId?)     │
└──────────────────────────┴──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Server API                                │
├─────────────────────────────────────────────────────────────────┤
│  GET /api/v1/medicalcases/pending?doctorId={id}&patientId={id}  │
│          │                                                       │
│          ▼                                                       │
│  MedicalCaseRepository.GetPendingCasesAsync(doctorId, patientId?)│
│          │                                                       │
│          ▼                                                       │
│  返回 List<PendingMedicalCaseDto>                                │
│  - Id, PatientId, PatientName, Type, CreatedAt                  │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Design

### Phase 1: 修复PatientSelectionViewModel Bug

**问题分析**：

`PatientSelectionViewModel.StartConsultationAsync`方法中，调用`GetPendingCasesAsync`时传入了`SelectedPatient.Id`（患者ID），但API期望的是`doctorId`（医生ID）。

```csharp
// 当前代码 (错误)
var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(SelectedPatient.Id);

// 修复后 (正确)
var doctorId = _sessionManager.CurrentUserId.Value;
var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(doctorId);
```

**但这只修复了一半问题**：业务需求是查找该患者的暂存医案，而非所有待看诊。

### Phase 2: 扩展API支持按患者筛选

**Server端变更**：

1. **IMedicalCaseRepository接口**：
```csharp
Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(
    Guid doctorId,
    Guid? patientId = null);  // 新增可选参数
```

2. **MedicalCaseRepository实现**：
```csharp
public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(
    Guid doctorId,
    Guid? patientId = null)
{
    var query = _dbSet
        .Where(m => !m.IsDeleted
            && (m.CaseStatus == MedicalCaseStatus.Draft
                || m.CaseStatus == MedicalCaseStatus.Active)
            && m.UserId == doctorId);

    // 按患者筛选
    if (patientId.HasValue)
    {
        query = query.Where(m => m.PatientId == patientId.Value);
    }

    // ...existing projection
}
```

3. **MedicalCaseController**：
```csharp
[HttpGet("pending")]
public async Task<IActionResult> GetPendingCases(
    [FromQuery] Guid doctorId,
    [FromQuery] Guid? patientId = null)  // 新增可选参数
```

**Desktop端变更**：

1. **IMedicalCaseApi接口**：
```csharp
[Refit.Get("/api/v1/medicalcases/pending")]
Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(
    [Refit.Query] Guid doctorId,
    [Refit.Query] Guid? patientId = null);  // 新增可选参数
```

2. **PatientSelectionViewModel修复**：
```csharp
private async Task StartConsultationAsync()
{
    var doctorId = _sessionManager.CurrentUserId.Value;
    var patientId = SelectedPatient.Id;

    // 查找该患者的暂存医案
    var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(doctorId, patientId);
    var suspendedCase = pendingCases?.Data?.FirstOrDefault(c => c.Type == PendingCaseType.Suspended);
    // ...
}
```

### Phase 3: 清理QueryMedicalCasesAsync(Pending)分支

**选项A：标记废弃（推荐）**

在`MedicalCaseQueryService`中标记Pending分支为废弃，引导使用`GetPendingCasesAsync`：

```csharp
case MedicalCaseQueryType.Pending:
    // OpenSpec: unify-pending-query-api - 建议使用 GetPendingCasesAsync
    // 此分支保留向后兼容，但不推荐使用
    var pendingCases = await _repository.GetPendingCasesAsync(doctorId.Value);
    // ...
```

**选项B：移除Pending分支**

从`MedicalCaseQueryType`枚举中移除Pending值，强制迁移到`GetPendingCasesAsync`。影响范围较大，需评估。

## Data Model

### PendingMedicalCaseDto 结构

```csharp
public class PendingMedicalCaseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }      // 确保存在
    public string PatientName { get; set; }
    public PendingCaseType Type { get; set; } // Registered/InProgress/Suspended
    public DateTime CreatedAt { get; set; }
    public string? Diagnosis { get; set; }    // 可选：简要诊断
}
```

### PendingCaseType 枚举映射

| PendingCaseType | MedicalCaseStatus | 业务含义 |
|-----------------|-------------------|---------|
| `Registered` | `Active` (当日新建) | 已挂号等候 |
| `InProgress` | `Active` (非当日) | 正在看诊 |
| `Suspended` | `Draft` | 暂存草稿 |

## API Contract Changes

### GET /api/v1/medicalcases/pending

**变更前**：
```
GET /api/v1/medicalcases/pending?doctorId={guid}
```

**变更后**：
```
GET /api/v1/medicalcases/pending?doctorId={guid}&patientId={guid?}
```

**兼容性**：向后兼容，`patientId`为可选参数。

## Affected Components

### Server端

| 组件 | 变更类型 | 说明 |
|------|---------|------|
| `IMedicalCaseRepository.cs` | 修改 | 添加patientId参数 |
| `MedicalCaseRepository.cs` | 修改 | 实现按患者筛选 |
| `MedicalCaseController.cs` | 修改 | 添加patientId查询参数 |
| `MedicalCaseQueryService.cs` | 可选 | 标记Pending分支废弃 |

### Desktop端

| 组件 | 变更类型 | 说明 |
|------|---------|------|
| `IMedicalCaseApi.cs` | 修改 | 添加patientId参数 |
| `PatientSelectionViewModel.cs` | 修复 | 修复参数传递Bug |
| `PendingQueueManager.cs` | 无变更 | 已正确使用doctorId |

### Shared

| 组件 | 变更类型 | 说明 |
|------|---------|------|
| `PendingMedicalCaseDto.cs` | 验证 | 确认PatientId字段存在 |

## Testing Strategy

1. **单元测试**：
   - `MedicalCaseRepository.GetPendingCasesAsync`参数组合测试
   - 按doctorId筛选、按patientId筛选、组合筛选

2. **集成测试**：
   - API端点参数验证
   - 空结果和有结果场景

3. **手动测试**：
   - 待看诊队列显示正确
   - 患者选择后能找到其暂存医案

## Rollback Plan

1. 所有变更保持向后兼容
2. 可选参数默认值保证旧调用正常工作
3. 如需回滚，仅需还原`PatientSelectionViewModel`变更

---

**创建日期**: 2026-01-07
