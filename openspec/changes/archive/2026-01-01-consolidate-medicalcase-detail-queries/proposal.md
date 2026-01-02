# Proposal: consolidate-medicalcase-detail-queries

## Summary

优化医案详情查询架构，解决N+1查询问题，支持批量获取医案详情（含处方），为清理Obsolete旧API扫清障碍。

## Motivation

当前`HistoryPrescriptionSelectionDialogViewModel`存在严重的N+1查询问题：

```csharp
// Line 117: 获取医案列表
var allMedicalCases = await _medicalCaseRepository.GetByPatientIdAsync(_patientId);

// Line 125-132: 对每个医案循环调用详情API
foreach (var medicalCase in medicalCases)
{
    var detail = await _medicalCaseRepository.GetByIdWithDetailsAsync(medicalCase.Id);
    // ...
}
```

**问题分析**：
1. `GetByPatientIdAsync`（已废弃）返回`List<MedicalCaseDetailDto>`，包含处方
2. `QueryMedicalCasesAsync`（新统一端点）返回`PagedResult<MedicalCaseListDto>`，**不含**处方
3. 迁移到新API后必须逐个查详情，造成N+1问题

**影响范围**：
- 患者有10个历史医案 → 发起11次API请求
- 网络延迟累加，用户体验差
- 阻塞`simplify-workspace-event-architecture`提案的Obsolete代码清理

## Goals

1. **新增批量详情查询端点**：一次请求获取多个医案详情（含处方）
2. **消除N+1查询**：`HistoryPrescriptionSelectionDialogViewModel`改为单次批量请求
3. **保持API设计一致性**：遵循现有`optimize-medicalcase-api`规范

## Non-Goals

1. 不修改现有`QueryMedicalCasesAsync`返回类型
2. 不改变MedicalCaseDetailDto与MedicalCaseListDto的设计边界
3. 不涉及其他模块的批量查询需求

## Design Overview

### 新增API端点

```csharp
/// <summary>
/// 批量获取医案详情（含处方）
/// OpenSpec: consolidate-medicalcase-detail-queries
/// </summary>
[Refit.Post("/api/v1/medicalcases/batch-details")]
Task<ApiResponse<List<MedicalCaseDetailDto>>> GetBatchDetailsAsync(
    [Refit.Body] BatchDetailQueryDto request);
```

### 请求DTO

```csharp
public class BatchDetailQueryDto
{
    /// <summary>
    /// 医案ID列表（最多50个）
    /// </summary>
    public List<Guid> Ids { get; set; } = new();
}
```

### 调用流程优化

```
优化前（N+1）:
QueryMedicalCasesAsync() → 1次
foreach: GetByIdWithDetailsAsync() → N次
总计: N+1次请求

优化后（2次）:
QueryMedicalCasesAsync() → 1次（获取ID列表）
GetBatchDetailsAsync([ids]) → 1次（批量获取详情）
总计: 2次请求
```

### Client端实现

```csharp
// HistoryPrescriptionSelectionDialogViewModel.cs
private async Task LoadPrescriptionsAsync()
{
    // Step 1: 获取患者已完成医案的ID列表
    var listResult = await _repository.QueryAsync(new MedicalCaseQueryDto
    {
        QueryType = MedicalCaseQueryType.ByPatient,
        PatientId = _patientId
    });
    
    var completedIds = listResult.Items
        .Where(mc => mc.CaseStatus == MedicalCaseStatus.Completed)
        .Select(mc => mc.Id)
        .ToList();
    
    if (completedIds.Count == 0) return;
    
    // Step 2: 批量获取详情（含处方）
    var details = await _repository.GetBatchDetailsAsync(completedIds);
    
    // Step 3: 提取处方
    foreach (var detail in details.Where(d => d.Prescription?.Items?.Count > 0))
    {
        Prescriptions.Add(detail.Prescription);
    }
}
```

## Risks and Mitigations

| 风险 | 缓解措施 |
|------|----------|
| 批量请求数据量大 | 限制最大50个ID，超过分批请求 |
| Server端性能 | 使用EF Core的`Contains`优化为单次数据库查询 |
| 返回顺序不一致 | Client端根据需要自行排序 |

## Success Criteria

1. 编译通过，无错误
2. `HistoryPrescriptionSelectionDialogViewModel`使用批量API
3. 现有单元测试通过
4. API响应时间优化（N+1 → 2）

## Related

- **前置依赖**: 无
- **解除阻塞**: `simplify-workspace-event-architecture` - Obsolete代码清理
- **参考规范**: `optimize-medicalcase-api` - API设计规范
