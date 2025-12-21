# Proposal: optimize-desktop-core

## Summary

深度优化Desktop层架构设计，解决Core层职责污染、ViewModel继承过深、数据流不清晰等问题。目标：达到业界优秀MVVM架构标准。

## Motivation

### 深度分析发现的架构问题

#### 1. Core层职责污染 (严重)

| 问题 | 位置 | 行数 | 说明 |
|------|------|------|------|
| HerbItemViewModelBase | Core/Models/ViewModels/Base/ | 304 | 业务代码不应在Core层 |
| 8个业务Item类 | Core/Models/Items/ | ~400 | 应归属各业务模块 |
| ExcelHelper重复 | Infrastructure + Utilities | 705×2 | 代码重复 |
| ClientErrorMessageMapper重复 | Infrastructure + Utilities | 366×2 | 代码重复 |

**问题**: Core层应只包含框架级代码，业务代码污染导致模块边界模糊。

#### 2. ViewModel继承过深 (4层)

```
BindableBase (Prism)
    └── ViewModelBase (362行)           ← Layer 1: 太多职责
            └── UnifiedViewModelBase (231行)    ← Layer 2: Navigation/Session/Validation
                    └── MasterDetailViewModelBase (484行)  ← Layer 3: List+Detail+Edit
                            └── 具体ViewModel               ← Layer 4: 业务逻辑
```

**问题**:
- ViewModelBase职责过多: Loading, Busy, Validation, Errors, HTTP, UI线程...
- 4层继承违反"组合优于继承"原则
- 与业界最佳实践(.NET Community Toolkit MVVM)差距明显

#### 3. 大型ViewModel违反规范

| ViewModel | 行数 | Components数 | 状态 |
|-----------|------|--------------|------|
| HerbMasterDetailViewModel | 683 | 0 | 严重超限 |
| UserMasterDetailViewModel | 695 | 0 | 严重超限 |
| PrescriptionPanelViewModel | 646 | 1 | 超限 |
| MedicalCaseWorkspaceViewModel | 622 | 8 | 临界 |

#### 4. 数据流不清晰

```
当前: API → Repository → ViewModel → Item → View (额外Item层)
目标: API → Repository → ViewModel → View (直接使用DTO)
```

**问题**: Item类是DTO的重复封装，增加复杂度且无明显价值。

### 业界最佳实践参考

| 框架 | ViewModel基类设计 | 特点 |
|------|-------------------|------|
| Prism官方 | ViewModelBase + INavigationAware | 精简，接口组合 |
| .NET Community Toolkit | ObservableObject + ObservableValidator | 模块化组合 |
| 行业共识 | 2-3层继承 + 组合模式 | 单一职责 |

## Refactoring Goals (高标准)

### 目标1: Core层纯净化
- Core只保留框架级代码
- 零业务逻辑污染
- 零代码重复

### 目标2: ViewModel扁平化
- 继承层级: 4层 → 2-3层
- 职责分离: 单一基类 → 多个Mixin/Component
- 代码行数: 所有ViewModel < 500行

### 目标3: 数据流清晰化
- 移除Item中间层
- 标准化: API → Repository → ViewModel → View
- 明确依赖方向

### 目标4: 模式统一化
- 100% Components模式覆盖
- 统一目录结构
- 统一命名规范

## Impact

### 受影响项目

| 项目 | 变更类型 | 说明 |
|------|----------|------|
| LYBT.Desktop.Models | 重构 | 移除业务代码，扁平化基类 |
| LYBT.Desktop.Infrastructure | 清理 | 合并Utilities功能 |
| LYBT.Desktop.Utilities | 删除 | 合并到Infrastructure |
| LYBT.Desktop.Herbs | 重构 | 添加Components，承接业务代码 |
| LYBT.Desktop.Users | 重构 | 添加Components |
| 其他业务模块 | 微调 | 统一模式 |

### 受影响规范

| 规范 | 变更类型 | 说明 |
|------|----------|------|
| viewmodel-conventions | 更新 | 添加继承层级限制、Mixin模式 |
| client-layer-architecture | 更新 | Core层职责定义、数据流规范 |

## Alternatives Considered

1. **仅修复超限ViewModel**: 不够彻底，Core污染问题未解决
2. **逐步渐进**: 风险低但效率低，选择中等力度优化
3. **完全重写基类体系**: 过于激进，选择重构而非重写

## Risks

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 功能回归 | 中 | 分Phase执行，每Phase验证 |
| 编译错误 | 中 | 严格遵循依赖方向 |
| DI配置 | 低 | 参考现有Components注册模式 |
| 时间超支 | 低 | 优先处理高价值改进 |

## Success Criteria (高标准)

### 量化指标

| 指标 | 当前 | 目标 |
|------|------|------|
| Core层业务代码 | ~1000行 | 0行 |
| ViewModel继承层级 | 4层 | 2-3层 |
| 超限ViewModel数量 | 4个 | 0个 |
| 代码重复 | ~2000行 | 0行 |
| Components覆盖率 | 60% | 100% |

### 质量指标

- [ ] 编译通过 (0错误0警告)
- [ ] 现有功能100%正常
- [ ] 数据流清晰可追踪
- [ ] 新开发者可快速理解架构

## Related

- **Specs**: viewmodel-conventions, client-layer-architecture
- **参考**: Prism Documentation, .NET Community Toolkit MVVM
- **模式**: MedicalCase模块 (8 Components) 作为最佳实践参考
