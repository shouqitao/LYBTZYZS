# unify-pending-query-api

## Summary

统一待看诊医案(Pending)查询API设计，解决当前`GetPendingCasesAsync`和`QueryMedicalCasesAsync(Pending)`两套API的DTO结构不兼容问题，并修复`PatientSelectionViewModel`中的参数传递Bug。

## Motivation

### 问题1: DTO结构不兼容

当前存在两套Pending查询API返回不同的DTO结构：

| API | 返回类型 | 关键字段 |
|-----|---------|---------|
| `GetPendingCasesAsync(doctorId)` | `List<PendingMedicalCaseDto>` | `Type` (PendingCaseType枚举) |
| `QueryMedicalCasesAsync(Pending)` | `PagedResult<MedicalCaseListDto>` | `CaseStatus` (MedicalCaseStatus枚举) |

两个枚举的映射关系：
- `PendingCaseType.Suspended` ↔ `MedicalCaseStatus.Draft`
- `PendingCaseType.InProgress` ↔ `MedicalCaseStatus.Active`
- `PendingCaseType.Registered` ↔ 无对应（当日新建，状态为Active）

### 问题2: 包装器调用模式

Server端`MedicalCaseQueryService.QueryMedicalCasesAsync`当`queryType=Pending`时，内部仍调用`GetPendingCasesAsync`然后转换DTO：

```csharp
// MedicalCaseQueryService.cs
case MedicalCaseQueryType.Pending:
    var pendingCases = await _repository.GetPendingCasesAsync(doctorId.Value);
    // 转换 PendingMedicalCaseDto → MedicalCaseListDto
```

这导致：
1. 信息丢失（`Type`字段无法映射到`CaseStatus`）
2. 重复代码和维护负担

### 问题3: 参数传递Bug

`PatientSelectionViewModel.cs:170`存在Bug，将`patientId`传给了期望`doctorId`的参数：

```csharp
// 错误: SelectedPatient.Id 是患者ID，但API期望医生ID
var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(SelectedPatient.Id);
```

## Proposed Changes

### 方案A: 统一到GetPendingCasesAsync（推荐）

保留专用的`GetPendingCasesAsync` API，增强`PendingMedicalCaseDto`添加`PatientId`字段支持按患者筛选：

1. **Server端**：
   - 为`GetPendingCasesAsync`添加可选`patientId`参数
   - 废弃`QueryMedicalCasesAsync(Pending)`分支或使其直接代理到`GetPendingCasesAsync`

2. **Desktop端**：
   - 修复`PatientSelectionViewModel`参数传递Bug
   - 统一使用`GetPendingCasesAsync`获取待看诊列表

3. **DTO**：
   - `PendingMedicalCaseDto`添加`PatientId`字段（如尚未有）
   - 保留`Type`字段用于待看诊队列分类显示

### 方案B: 统一到QueryMedicalCasesAsync

废弃`GetPendingCasesAsync`，统一使用`QueryMedicalCasesAsync(Pending)`：

1. 需在`MedicalCaseListDto`中添加等价于`PendingCaseType`的信息
2. 前端需适配新DTO结构
3. 影响范围更大

### 推荐方案

**选择方案A**。理由：
- `PendingMedicalCaseDto`专为待看诊队列设计，包含UI所需的`Type`分类信息
- 改动范围小，仅需添加参数和修复Bug
- 保持关注点分离：列表查询和待看诊队列是不同场景

## Acceptance Criteria

- [ ] `GetPendingCasesAsync`支持可选`patientId`参数
- [ ] `PatientSelectionViewModel`参数传递Bug已修复
- [ ] 所有调用`GetPendingCasesAsync`的地方使用正确参数
- [ ] Server端`QueryMedicalCasesAsync(Pending)`行为保持兼容或标记废弃
- [ ] 编译零警告、零错误
- [ ] 待看诊队列功能正常

## Stakeholders

- 中医诊所临床医生（待看诊队列的主要使用者）
- 开发团队（API一致性维护）

## Risks

| 风险 | 缓解措施 |
|------|---------|
| API兼容性 | 添加参数使用可选默认值，保持向后兼容 |
| 待看诊队列功能中断 | 分阶段实施，先修Bug后重构 |

## Related

- OpenSpec: `standardize-api-naming` - 发现此设计问题的来源提案
- `PendingQueueManager.cs` - 正确使用`doctorId`的示例
- `PatientSelectionViewModel.cs:170` - Bug位置

---

**创建日期**: 2026-01-07
**状态**: 草稿
