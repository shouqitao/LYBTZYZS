# OpenSpec Proposal: Desktop层架构统一重构

**Change ID**: unify-desktop-architecture
**Status**: Cancelled
**Cancelled**: 2025-12-30
**Reason**: 已被 refactor-desktop-comprehensive 提案整合替代
**Created**: 2025-12-30
**Author**: Claude Code
**Epic**: Desktop Architecture Modernization

---

## 1. 概述

### 1.1 背景

近期针对Desktop层的优化提案分散在多个独立proposal中，包括：
- refactor-viewmodel-composition (ViewModel组合模式)
- unify-desktop-command-handler (CommandHandler统一)
- standardize-desktop-data-layer (数据层标准化)
- refactor-viewmodel-layer (ViewModel瘦身)
- simplify-medicalcase-dataflow (MedicalCase数据流简化)
- refactor-medicalcase-workspace (工作区控件提取)
- optimize-medicalcase-navigation (导航优化)
- optimize-medicalcase-ui (UI优化)

这些提案相互依赖但缺乏统一协调，本提案将其整合为一个完整的架构重构计划。

### 1.2 目标

1. **统一架构模式** - 所有模块遵循相同的ViewModel/CommandHandler/Repository分层
2. **减少代码量** - ViewModel行数从1500+降至400以下
3. **提高复用性** - 提取3+可复用控件
4. **标准化规范** - 统一DTO命名、返回类型、错误处理

### 1.3 范围

| 模块 | 影响程度 | 说明 |
|------|----------|------|
| MedicalCase | 高 | 主要重构目标，聚合根特殊处理 |
| Patients | 中 | 控件提取，ViewModel瘦身 |
| Users | 低 | 模式对齐 |
| Herbs | 低 | 模式对齐 |
| Formula | 低 | 模式对齐 |
| Infrastructure | 高 | 基础设施层扩展 |

---

## 2. 问题分析

### 2.1 当前问题

| # | 问题 | 影响 | 根因 |
|---|------|------|------|
| 1 | ViewModel臃肿 | 维护困难 | 职责混杂，缺乏分层 |
| 2 | 数据层模式不统一 | 代码重复 | Repository/CommandHandler混用 |
| 3 | DTO命名混乱 | 理解困难 | 缺乏命名规范 |
| 4 | 样板代码多 | 开发效率低 | 未使用源生成器 |
| 5 | 控件重复 | 维护成本高 | 缺乏可复用控件库 |

### 2.2 当前架构

```
ViewModel Layer (问题：1500+行，职责混杂)
    │
    ├── 直接依赖 Repository (问题：不统一)
    │   └── IXxxRepository
    │
    └── 或依赖 CommandHandler (问题：不统一)
        └── IXxxCommandHandler
            └── IXxxRepository
```

### 2.3 目标架构

```
ViewModel Layer (目标：<400行，职责清晰)
    │
    ├── 8个标准服务接口 (IMasterDetailServices)
    │   ├── ILoadingStateManager
    │   ├── IPaginationService
    │   ├── ISearchService
    │   ├── ISelectionService
    │   ├── IDetailEditorService
    │   ├── IDialogManager
    │   ├── IViewNavigationService
    │   └── IErrorHandler
    │
    └── CommandHandler Only (统一模式)
        └── IXxxCommandHandler
            └── IXxxRepository (内部实现)
```

---

## 3. 设计方案

### 3.1 分层架构

#### Layer 1: ViewModel Layer

**基类继承链简化**:
```
当前: BindableBase → ViewModelBase → UnifiedViewModelBase → UnifiedListViewModelBase → MasterDetailViewModelBase
目标: ObservableObject → MasterDetailViewModelBase (使用CommunityToolkit.Mvvm)
```

**标准ViewModel结构**:
```csharp
public partial class XxxMasterDetailViewModel : MasterDetailViewModelBase<XxxListDto, XxxDetailModel>
{
    // 1. 依赖注入 (CommandHandler Only)
    private readonly IXxxCommandHandler _commandHandler;
    
    // 2. [ObservableProperty] 属性 (源生成)
    [ObservableProperty] private XxxDetailModel? _selectedDetail;
    
    // 3. [RelayCommand] 命令 (源生成)
    [RelayCommand] private async Task SaveAsync() { ... }
    
    // 4. 抽象方法实现
    protected override async Task LoadListAsync() { ... }
    protected override async Task<bool> SaveDetailAsync(XxxDetailModel detail) { ... }
}
```

#### Layer 2: CommandHandler Layer

**统一接口规范**:
```csharp
public interface IXxxCommandHandler
{
    // 列表查询
    Task<(bool success, List<XxxListDto>? data, string? error)> GetListAsync(QueryParams? query = null);
    
    // 详情查询
    Task<(bool success, XxxDetailDto? data, string? error)> GetDetailAsync(Guid id);
    
    // 保存 (创建/更新)
    Task<(bool success, XxxDetailDto? data, string? error)> SaveAsync(XxxInputDto input);
    
    // 删除
    Task<(bool success, bool? data, string? error)> DeleteAsync(Guid id);
}
```

#### Layer 3: DTO Layer

**命名规范**:
| 用途 | 命名规则 | 示例 |
|------|----------|------|
| 列表项 | `[Entity]ListDto` | PatientListDto, HerbListDto |
| 详情 | `[Entity]DetailDto` | PatientDetailDto, HerbDetailDto |
| 输入 | `[Entity]InputDto` | PatientInputDto, HerbInputDto |
| 聚合输入 | `[Entity]AggregateInputDto` | MedicalCaseInputDto (含Consultation/Prescription) |

### 3.2 MedicalCase特殊处理

MedicalCase作为聚合根，采用Coordinator模式：

```
MedicalCaseWorkspaceViewModel (<400行)
    │
    ├── ConsultationPanelViewModel (IDataProvider)
    │       └── GetConsultationData() → ConsultationInputDto
    │
    ├── PrescriptionPanelViewModel (IDataProvider, IValidatable)
    │       └── GetPrescriptionData() → PrescriptionInputDto
    │
    └── MedicalCaseWorkspaceCoordinator
            ├── SaveAsync() → 聚合保存
            ├── SaveDraftAsync() → 暂存
            ├── CompleteAsync() → 完成
            └── CancelAsync() → 取消
```

### 3.3 可复用控件

| 控件 | 位置 | 用途 |
|------|------|------|
| PatientInfoCardControl | Infrastructure/Controls | 患者信息卡片 |
| PatientSearchControl | Infrastructure/Controls | 患者搜索 |
| PendingQueueControl | Infrastructure/Controls | 候诊队列 |

---

## 4. 实施计划

### Phase 1: 基础设施 (P0)

**目标**: 建立统一规范和基础设施

**任务**:
1. 添加CommunityToolkit.Mvvm依赖
2. 创建统一CommandHandler接口模板
3. 标准化DTO命名 (全局重命名)
4. 创建IMasterDetailServices接口

**预期产出**:
- 新增NuGet包
- 新增接口定义文件
- DTO重命名完成

### Phase 2: CommandHandler统一 (P1)

**目标**: 所有模块使用CommandHandler Only模式

**任务**:
1. 为每个模块创建/完善CommandHandler
2. ViewModel移除Repository直接依赖
3. 统一返回类型为元组

**预期产出**:
- 6个标准CommandHandler实现
- ViewModel依赖更新

### Phase 3: ViewModel瘦身 (P2)

**目标**: 所有ViewModel行数 < 400

**任务**:
1. 提取Components (状态机、数据加载器等)
2. 应用CommunityToolkit.Mvvm源生成器
3. 移除冗余代码和注释

**预期产出**:
- Components目录结构
- ViewModel行数减少75%

### Phase 4: 控件提取 (P3)

**目标**: 可复用控件标准化

**任务**:
1. 完善PatientInfoCardControl
2. 完善PatientSearchControl
3. 完善PendingQueueControl
4. 更新使用方

**预期产出**:
- 3个可复用控件
- 使用文档

### Phase 5: MedicalCase优化 (P4)

**目标**: MedicalCase模块架构清晰化

**任务**:
1. 完善Coordinator模式
2. 优化聚合保存流程
3. 完善导航逻辑
4. UI布局优化

**预期产出**:
- MedicalCaseWorkspaceViewModel < 400行
- 导航流程清晰
- UI体验优化

---

## 5. 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 大规模重构引入Bug | 中 | 高 | 分Phase实施，每Phase完成后测试 |
| CommunityToolkit.Mvvm学习曲线 | 低 | 中 | 提供示例代码和文档 |
| 跨模块改动冲突 | 中 | 中 | 使用Feature分支，及时合并 |

---

## 6. 验收标准

| 指标 | 当前值 | 目标值 | 验收方法 |
|------|--------|--------|----------|
| 最大ViewModel行数 | 1544 | < 400 | 代码行数统计 |
| CommandHandler覆盖率 | 50% | 100% | 依赖检查 |
| DTO命名规范率 | 60% | 100% | 命名审查 |
| 可复用控件数 | 0 | 3+ | 控件清单 |
| 编译通过 | - | 是 | dotnet build |
| 单元测试通过 | - | 是 | dotnet test |

---

## 7. 相关文档

- [design.md](./design.md) - 详细设计文档
- [tasks.md](./tasks.md) - 任务分解清单
- [refactor-viewmodel-composition](../archive/2025-12-25-refactor-viewmodel-composition/) - 已归档提案
- [unify-desktop-command-handler](../archive/2025-12-24-unify-desktop-command-handler/) - 已归档提案

---

**等待审批后开始实施**
