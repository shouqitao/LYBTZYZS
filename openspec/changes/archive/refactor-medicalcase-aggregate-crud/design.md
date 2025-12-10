# Design: 医案聚合根CRUD重构

## Context

### 当前架构问题

```
当前保存流程（分散）:
┌─────────────────────────────────────────────────────────────┐
│  MedicalCaseWorkspaceCoordinator.SaveDraftAsync()           │
│    ├── ConsultationPanel.SaveSilentlyAsync()                │
│    │     └── PUT /api/consultation/{id}  ← 独立API          │
│    ├── PrescriptionPanel.SaveSilentlyAsync()                │
│    │     └── POST/PUT /api/prescription  ← 独立API          │
│    └── LifecycleHandler.SaveDraftAsync()                    │
│          └── PUT /api/medicalcase/{id}/status  ← 状态API    │
└─────────────────────────────────────────────────────────────┘
问题: 三个独立API调用，无事务保证，处方可能保存失败
```

### 目标架构

```
目标保存流程（聚合）:
┌─────────────────────────────────────────────────────────────┐
│  MedicalCaseWorkspaceCoordinator.SaveDraftAsync()           │
│    ├── 收集Consultation数据                                  │
│    ├── 收集Prescription数据                                  │
│    └── PUT /api/medicalcase/{id}/aggregate                  │
│          └── 事务内同时保存Consultation + Prescription       │
└─────────────────────────────────────────────────────────────┘
```

## Goals / Non-Goals

### Goals
- 医案聚合根单次API调用完成所有子实体保存
- 事务保证Consultation和Prescription原子性
- 简化前端保存逻辑，减少Handler数量
- CanComplete状态基于数据验证而非事件

### Non-Goals
- 不改变现有的MedicalCase创建流程
- 不修改只读查询API
- 不涉及打印、导出等非CRUD功能

## Decisions

### Decision 1: 统一聚合根DTO

创建`MedicalCaseAggregateInputDto`：

```csharp
public class MedicalCaseAggregateInputDto
{
    // 医案基础信息
    public Guid Id { get; set; }
    public string? Remark { get; set; }
    public string? EditReason { get; set; }  // 审计原因

    // 诊断信息（嵌套）
    public ConsultationInputDto? Consultation { get; set; }

    // 处方信息（嵌套）
    public PrescriptionAggregateDto? Prescription { get; set; }
}

public class PrescriptionAggregateDto
{
    public bool NeedsPrescription { get; set; }
    public int DosageCount { get; set; }
    public string? Usage { get; set; }
    public List<PrescriptionItemInputDto> Items { get; set; } = new();
}
```

**Rationale**: 符合DDD聚合根边界，一次请求完成完整保存

### Decision 2: 后端单一保存端点

新增`PUT /api/medicalcase/{id}/aggregate`端点，替代现有分散调用：

```csharp
[HttpPut("{id}/aggregate")]
public async Task<ActionResult<ApiResult<MedicalCaseDto>>> SaveAggregate(
    Guid id,
    [FromBody] MedicalCaseAggregateInputDto input)
{
    var userId = GetOperatorId();
    var result = await _commandService.SaveAggregateAsync(id, input, userId);
    return result.ToActionResult();
}
```

**Rationale**:
- 事务边界清晰
- 减少网络往返
- 符合聚合根一致性原则

### Decision 3: 前端保存逻辑简化

移除ISaveable接口的独立API调用，改为数据收集模式：

```csharp
// Before: 每个Panel独立保存
interface ISaveable {
    Task<bool> SaveSilentlyAsync();  // 调用各自API
}

// After: Panel仅提供数据
interface IDataProvider {
    ConsultationInputDto? GetConsultationData();
    PrescriptionAggregateDto? GetPrescriptionData();
}

// Coordinator统一保存
public async Task SaveAggregateAsync() {
    var input = new MedicalCaseAggregateInputDto {
        Id = _medicalCaseId,
        Consultation = _consultationPanel.GetConsultationData(),
        Prescription = _prescriptionPanel.GetPrescriptionData()
    };
    await _repository.SaveAggregateAsync(input);
}
```

**Rationale**: 职责分离，Panel只负责UI绑定，Coordinator负责API调用

### Decision 4: CanComplete状态改为数据验证

```csharp
// Before: 事件驱动
private void OnConsultationCompleted(...) { CanComplete = true; }

// After: 数据验证
private bool CanComplete =>
    IsConsultationValid &&
    (!NeedsPrescription || IsPrescriptionValid);

private bool IsConsultationValid =>
    !string.IsNullOrEmpty(ChiefComplaint) &&
    !string.IsNullOrEmpty(TCMDiagnosis);

private bool IsPrescriptionValid =>
    HerbItems.Any(h => h.HerbId != Guid.Empty && h.Dosage > 0);
```

**Rationale**: 状态基于数据实时计算，无需事件同步

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 大规模重构可能引入回归 | 分Phase实施，每Phase独立测试 |
| 现有独立API可能被其他功能使用 | 先调研所有调用点，保留兼容API |
| 聚合DTO过大影响性能 | 处方项通常<50个，影响可忽略 |

## Migration Plan

1. **Phase 1**: 添加新DTO，不修改现有代码
2. **Phase 2**: 添加新API端点，与现有端点并存
3. **Phase 3**: 前端切换到新API，灰度验证
4. **Phase 4**: 移除旧的独立保存逻辑
5. **Rollback**: 每Phase可独立回滚

## Open Questions

1. 是否需要保留现有的独立Consultation/Prescription API作为兼容层？
2. 完成看诊的验证规则是否需要可配置（如某些场景可不开处方）？
