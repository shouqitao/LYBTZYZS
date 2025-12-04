# Design: cleanup-obsolete-code

## Overview

本文档记录废弃代码清理的详细分析和设计决策。

## 废弃代码分析

### 1. CacheHealthController (整个文件删除)

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/CacheHealthController.cs`

**分析结果**:
- 代码行数: 197行
- 标记: `[Obsolete("运维功能，无Client调用。保留待未来评估是否需要Admin UI")]`
- Client调用: 无
- 测试覆盖: 无
- 功能用途: 缓存管理运维功能

**删除理由**:
1. 从未被任何Client调用
2. 使用IMemoryCache的简化实现，无实际价值
3. 缓存管理应通过专业运维工具而非API暴露

**影响范围**:
- 无Service层依赖
- 无Repository层依赖
- 无Client层依赖

### 2. 批量删除端点 (3处)

**统一设计决策**: 根据 OpenSpec `refactor-webapi-layer`，批量删除模式已统一为Client端循环模式。

| Controller | 方法 | 替代方案 |
|------------|------|----------|
| HerbsController | BatchDeleteHerbs | Client循环调用 DELETE /{id} |
| FormulasController | BatchDeleteFormulas | Client循环调用 DELETE /{id} |
| UsersController | BatchDeleteUsers | Client循环调用 DELETE /{id} |

**删除代码结构** (每个约30行):
```csharp
[Obsolete("此端点未被Client使用，已在 OpenSpec refactor-webapi-layer 中标记废弃")]
[ApiExplorerSettings(IgnoreApi = true)]
[HttpPost("batch-delete")]
public async Task<IActionResult> BatchDelete[Entity]([FromBody] List<Guid> ids)
{
    // 循环删除逻辑
}
```

### 3. MedicalCaseController.CompleteMedicalCase

**分析**:
- 原方法: `PUT /{id}/complete`
- 替代方法: `PUT /{id}/status` 配合 `Completed` 状态

**删除理由**:
1. 功能重复，已有统一的状态更新端点
2. Client已迁移到使用 `PUT /{id}/status`

### 4. UsersController.ToggleStatus

**分析**:
- 原方法: `POST /{id}/toggle-status`
- 设计意图: 切换用户启用/禁用状态

**删除理由**:
1. 从未被Client实现或调用
2. 用户状态管理可通过 `PUT /{id}` 更新

## 未使用DTO分析

### 分析方法

使用全局代码搜索(Grep)检查每个DTO类名的引用数量。如果DTO类仅在定义文件中出现，则判定为未使用。

### 未使用DTO列表

#### 1. FormulaAnalysisDtos.cs (整个文件删除)

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaAnalysisDtos.cs`

**分析结果**:
- 代码行数: 98行
- 包含DTO: 6个
- 引用数量: 0 (仅定义文件)
- 功能用途: 验方分析相关功能（从未实现）

**删除的DTO**:
| DTO类名 | 行数 | 描述 |
|---------|------|------|
| FormulaFromTemplateDto | 12 | 从模板创建验方 |
| FormulaHistoryDto | 17 | 验方历史记录 |
| FormulaTypeDto | 7 | 验方类型枚举 |
| FormulaCopyResultDto | 9 | 验方复制结果 |
| FormulaUsageStatDto | 10 | 验方使用统计 |
| FormulaEffectivenessDto | 10 | 验方效果评估 |

**删除理由**:
1. 这些DTO是为未来功能预留的
2. 相关功能从未实现
3. 无任何代码引用

#### 2. MedicalCaseDtos.cs (部分删除)

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDtos.cs`

**需删除的DTO**:
| DTO类名 | 删除原因 |
|---------|----------|
| CompleteMedicalCaseDto | 已有UpdateMedicalCaseStatusDto替代 |
| SuspendMedicalCaseDto | 已有UpdateMedicalCaseStatusDto替代 |
| ArchiveMedicalCaseDto | 已有UpdateMedicalCaseStatusDto替代 |
| DoctorMedicalCaseStatisticsDto | 统计功能从未实现 |

**保留的DTO** (有引用):
- PendingMedicalCaseDto - 17个文件引用
- MedicalCasePermissionDto - 9个文件引用

#### 3. PatientOperationDtos.cs (部分删除)

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientOperationDtos.cs`

**需删除的DTO**:
| DTO类名 | 删除原因 |
|---------|----------|
| PatientVisitHistoryDto | 功能从未实现 |
| PatientProfileManagementDto | 功能从未实现 |

#### 4. HerbOperationDtos.cs (部分删除)

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbOperationDtos.cs`

**需删除的DTO**:
| DTO类名 | 删除原因 |
|---------|----------|
| CompatibilitySuggestionDto | 药材配伍建议功能从未实现 |
| HerbSpecialPriceDto | 特殊价格功能从未实现 |

### DTO删除影响矩阵

| 删除项 | Server层 | Client层 | 测试层 |
|--------|----------|----------|--------|
| FormulaAnalysisDtos.cs | 无影响 | 无影响 | 无影响 |
| CompleteMedicalCaseDto | 无影响 | 无影响 | 无影响 |
| SuspendMedicalCaseDto | 无影响 | 无影响 | 无影响 |
| ArchiveMedicalCaseDto | 无影响 | 无影响 | 无影响 |
| DoctorMedicalCaseStatisticsDto | 无影响 | 无影响 | 无影响 |
| PatientVisitHistoryDto | 无影响 | 无影响 | 无影响 |
| PatientProfileManagementDto | 无影响 | 无影响 | 无影响 |
| CompatibilitySuggestionDto | 无影响 | 无影响 | 无影响 |
| HerbSpecialPriceDto | 无影响 | 无影响 | 无影响 |

## TODO注释清理策略

### 分类标准

| 类别 | 处理方式 | 识别特征 |
|------|----------|----------|
| 已完成TODO | 删除 | Phase已完成、功能已实现 |
| 有Issue关联 | 保留 | 包含#IssueNumber |
| 真实待办 | 保留 | 对应未实现功能 |
| Bug Fix注释 | 保留 | 解释修复原因 |

### 需删除的TODO

```
InformationDialogViewModel.cs:31
// TODO: Phase 4C - 实现关闭对话框逻辑
```

**原因**: Phase 4C UI重构已完成，对话框逻辑应已实现。需验证后删除。

### 保留的TODO

1. **有Issue关联**:
   - `FormulaImportHandler.cs` - Issue #1807
   - `MedicalCaseManagementViewModel.cs` - 批量删除待实现

2. **有Epic关联**:
   - `PrescriptionPrintService.cs` - PRINT-4/5打印功能

3. **真实待办**:
   - `ClinicalHomeViewModel.cs:311` - 今日统计数据
   - `MedicalCaseEventCoordinator.cs` - 事件定义

## 依赖分析

### 删除影响矩阵

| 删除项 | Controller层 | Service层 | Repository层 | Client层 |
|--------|-------------|-----------|--------------|----------|
| CacheHealthController | 删除文件 | 无影响 | 无影响 | 无影响 |
| BatchDeleteHerbs | 删除方法 | 无影响 | 无影响 | 无影响 |
| BatchDeleteFormulas | 删除方法 | 无影响 | 无影响 | 无影响 |
| BatchDeleteUsers | 删除方法 | 无影响 | 无影响 | 无影响 |
| CompleteMedicalCase | 删除方法 | 无影响 | 无影响 | 无影响 |
| ToggleStatus | 删除方法 | 无影响 | 无影响 | 无影响 |

### 测试影响

| 测试类型 | 影响 |
|----------|------|
| CacheHealthController单元测试 | 删除（如存在） |
| Controller集成测试 | 无影响（废弃端点未测试） |
| Service单元测试 | 无影响 |
| Client测试 | 无影响 |

## 回滚策略

如需恢复废弃代码：
1. 使用git revert恢复提交
2. 废弃代码保存在OpenSpec archive中

## 清理统计

| 指标 | 数值 |
|------|------|
| 删除文件数 | 2 (CacheHealthController.cs + FormulaAnalysisDtos.cs) |
| 删除方法数 | 6 |
| 删除DTO类数 | 14 |
| 预计删除代码行 | ~570 (340行API + 230行DTO) |
| 影响测试数 | 0 |
| 风险等级 | 低 |
