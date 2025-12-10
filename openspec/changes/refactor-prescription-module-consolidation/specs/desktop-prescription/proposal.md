# Proposal: 重构Client端处方模块代码整合

**Author:** Claude Code
**Created:** 2025-12-10
**Status:** draft
**Type:** refactor
**Risk:** medium

## Summary

整合处方模块从Server到Client的全栈职责剥离，消除历史技术债务。包括：
1. Client端：消除`LYBT.Desktop.Prescriptions`和`LYBT.Desktop.MedicalCase`模块之间~1200行重复代码
2. Server端：清理Prescription实体冗余字段（PatientId, UserId）
3. Shared层：简化DTO契约，移除冗余字段
4. 打印服务：提升至MedicalCase级别，支持完整医案打印

## Background

### 问题背景

在MedicalCase聚合根重构过程中(Epic #1540, #1600, #1606)，处方功能逐渐从独立模块迁移到MedicalCase模块内。这导致两个模块中存在大量功能重叠的代码：

1. **验方选择对话框**: `SelectFormulaDialogViewModel` (Prescriptions) vs `FormulaSelectionDialogViewModel` (MedicalCase) - 70%代码重复
2. **处方计算器**: `PrescriptionCalculator` 在两个模块中有不同实现
3. **处方项ViewModel**: 同名类但职责不同，造成混淆

### 架构约定（医案=诊断+处方）

根据DDD聚合根设计，建立以下职责边界约定：

**Panel组件 -> MedicalCase模块**（与聚合根生命周期绑定）
- ConsultationPanelViewModel
- PrescriptionPanelViewModel

**可复用Dialog/Service -> 各自领域模块**
- SelectFormulaDialog -> Prescriptions模块
- PrescriptionCalculator -> Prescriptions模块
- HerbSelectionDialog -> Herbs模块

**依赖方向**: MedicalCase -> Prescriptions -> Herbs/Formula

### 全栈技术债务分析

**Server端分析结果**:
- `IPrescriptionService` - 已重构为只读（写操作通过MedicalCaseService）
- `PrescriptionsController` - 已是只读控制器
- `Prescription`实体 - 存在冗余字段：PatientId, UserId（应通过MedicalCase导航获取）

**Shared层DTO分析结果**:
- `PrescriptionAggregateDto` - 设计正确（嵌套结构，无PatientId/UserId）
- `PrescriptionDto` - 存在冗余字段：PatientId, UserId
- `PrescriptionCreateDto`, `PrescriptionEditDto`, `PrescriptionInputDto` - 存在冗余字段

**纯处方职责（应保留）**:
- Items (药材列表) - 处方核心
- DosageCount, Discount, Usage, Advice, FormulaSource
- SingleDosePrice, TotalPrice, TotalWeight
- PrescriptionNumber (打印追踪)

### 当前代码量统计

| 模块 | 文件数 | 代码行数 | 备注 |
|------|--------|----------|------|
| LYBT.Desktop.Prescriptions | 16 | ~3,500 | 含骨架代码 |
| LYBT.Desktop.MedicalCase (处方相关) | 8 | ~1,800 | 活跃使用 |
| **重复代码估算** | - | ~1,200 | 待消除 |

## Goals

1. **消除代码重复**: 删除~1,200行重复代码
2. **统一组件**: 合并验方选择、价格计算等共享功能
3. **明确职责边界**: Prescriptions模块专注于打印和独立处方管理，MedicalCase模块处理医案内的处方编辑
4. **保持向后兼容**: 不影响现有功能和用户体验

## Non-Goals

- 不修改数据库Schema（字段标记为冗余但不删除列）
- 不涉及Formula模块的验方验证功能
- 不改变现有API契约（仅简化内部实现）

## Proposed Solution

### Phase 1: 统一验方选择对话框

**保留**: `LYBT.Desktop.Prescriptions.ViewModels.SelectFormulaDialogViewModel` (~587行)
- 功能更完整：支持分类筛选、效能筛选、详情预览
- 已与`PrescriptionDataManager`集成

**删除**: `LYBT.Desktop.MedicalCase.ViewModels.FormulaSelectionDialogViewModel` (~216行)
- 功能是SelectFormulaDialogViewModel的子集
- MedicalCase模块改为引用Prescriptions模块的对话框

### Phase 2: 统一处方计算器

**保留**: `LYBT.Desktop.Prescriptions.ViewModels.Components.PrescriptionCalculator`
- 继承自`HerbCalculatorBase<T>`
- 使用`IHerbItem`接口实现多态

**删除**: `LYBT.Desktop.MedicalCase.Services.PrescriptionCalculator`
- 独立实现，未使用基类
- 功能可合并到保留版本

**迁移**: 将事件机制(`PriceCalculatedEventArgs`)迁移到保留版本

### Phase 3: 重命名澄清

| 当前名称 | 新名称 | 模块 | 说明 |
|----------|--------|------|------|
| `PrescriptionItemViewModel` (MedicalCase) | `PrescriptionHerbEditorViewModel` | MedicalCase | 强调交互式编辑职责 |
| `PrescriptionItemViewModel` (Prescriptions) | 保持不变 | Prescriptions | DTO包装器 |
| `FormulaTemplateDialogViewModel` | `FormulaImportDialogViewModel` | Prescriptions | 更准确反映功能 |

### Phase 4: 清理骨架代码

删除以下注释掉/未使用的代码：
- `PrescriptionManagementViewModel` (注释状态)
- `PrescriptionEditorDialogViewModel` (部分实现)
- `PrescriptionsMainViewModel` (已标记删除)

### Phase 5: 打印服务提升至MedicalCase级别

**背景**: 当前打印服务仅针对处方，但医案打印需求包括完整案例（诊断+处方+医嘱）

**新增**: `IMedicalCasePrintService`接口
```csharp
public interface IMedicalCasePrintService
{
    Task<PrintResult> PrintFullCaseAsync(MedicalCaseDto medicalCase);
    Task<PrintResult> PrintConsultationAsync(ConsultationDto consultation);
    Task<PrintResult> PrintPrescriptionAsync(PrescriptionDto prescription);
    Task<PrintResult> PrintSummaryAsync(MedicalCaseDto medicalCase);
}
```

**保留**: `IPrescriptionPrintService`作为内部实现细节

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/IMedicalCasePrintService.cs` (新建)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCasePrintService.cs` (新建)

### Phase 6: 全栈处方职责分离

**Server端清理**:
1. 标记`Prescription.PatientId`为`[Obsolete]`（通过MedicalCase.PatientId获取）
2. 标记`Prescription.UserId`为`[Obsolete]`（通过MedicalCase.UserId获取）
3. 添加导航属性`MedicalCase.Prescription`确保数据一致性

**Shared层简化**:
1. 保留`PrescriptionAggregateDto`作为MedicalCase嵌套结构（已正确设计）
2. 简化`PrescriptionDto`：标记PatientId/UserId为`[Obsolete]`
3. 清理`PrescriptionCreateDto`、`PrescriptionEditDto`中的冗余字段

**Client端对齐**:
1. 确保PrescriptionPanelViewModel从MedicalCase获取Patient/User信息
2. 移除直接使用冗余字段的代码路径

## Alternatives Considered

### 方案A: 完全合并到MedicalCase模块

**优点**: 单一位置管理所有处方代码
**缺点**:
- MedicalCase模块会过于庞大
- 打印功能与MedicalCase关系不大
- 违反单一职责原则

**决策**: 不采用

### 方案B: 创建新的Shared.Prescription模块

**优点**: 清晰的共享边界
**缺点**:
- 增加模块数量
- 引入新的依赖关系
- 过度工程化

**决策**: 不采用，当前两模块依赖Prescriptions即可

## Impact Analysis

### 影响的文件

**Client端 (Phase 1-5)**:
```
MODIFIED:
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/PrescriptionsModule.cs

REMOVED:
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/FormulaSelectionDialogViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/FormulaSelectionDialog.xaml(.cs)
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/PrescriptionCalculator.cs

RENAMED:
- PrescriptionItemViewModel -> PrescriptionHerbEditorViewModel (MedicalCase)

NEW (Phase 5):
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/IMedicalCasePrintService.cs
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCasePrintService.cs
```

**Server端 (Phase 6)**:
```
MODIFIED:
- src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs (添加[Obsolete]标记)
```

**Shared层 (Phase 6)**:
```
MODIFIED:
- src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDtos.cs (添加[Obsolete]标记)
```

### 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 对话框注册冲突 | 低 | 中 | 测试模块加载顺序 |
| 计算结果不一致 | 中 | 高 | 单元测试覆盖所有计算场景 |
| 引用断裂 | 中 | 中 | 编译验证+全量搜索 |
| 打印格式变化 | 低 | 中 | 保持IPrescriptionPrintService兼容 |
| Obsolete警告噪音 | 中 | 低 | 分阶段清理，设置警告级别 |
| 全栈协调复杂 | 中 | 中 | Phase 6最后执行，确保前5个Phase稳定 |

## Success Metrics

- [ ] 代码行数减少 >=1,000行
- [ ] 编译通过且无警告
- [ ] 所有处方相关单元测试通过
- [ ] 手动测试验证：创建医案->开处方->导入验方->计算价格->打印

## Timeline

| Phase | 内容 | 预估复杂度 |
|-------|------|------------|
| Phase 1 | 统一验方选择对话框 | 中 |
| Phase 2 | 统一处方计算器 | 中 |
| Phase 3 | 重命名澄清 | 低 |
| Phase 4 | 清理骨架代码 | 低 |
| Phase 5 | 打印服务提升至MedicalCase | 中 |
| Phase 6 | 全栈处方职责分离 | 高 |

## Open Questions

1. `FormulaTemplateDialogViewModel`是否需要与`SelectFormulaDialogViewModel`合并？
   - 当前决策：暂不合并，两者入口不同（模板选择 vs 处方导入）

2. MedicalCase模块的`PrescriptionItemViewModel`的7级拼音过滤算法是否需要迁移？
   - 当前决策：保留在MedicalCase，这是交互式编辑的特有需求
