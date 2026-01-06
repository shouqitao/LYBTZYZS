# OpenSpec Proposal: standardize-viewmodel-framework

## 概述

**变更ID**: standardize-viewmodel-framework
**状态**: Draft
**创建日期**: 2026-01-06
**影响范围**: Desktop Client (全Desktop层)
**前置依赖**: adopt-mapperly-unified-mapping (已归档)

## Why

- **框架混用**: 项目同时使用 Prism BindableBase 和 CommunityToolkit.Mvvm，增加认知负担
- **基类过多**: 15个ViewModel基类，继承层次复杂，职责不清
- **代码冗余**: 手写属性+命令代码量大，源生成器可减少50%+
- **维护成本**: 新开发者需同时学习两套模式，易出错

## What Changes

- **ViewModel层**: 统一迁移到 CommunityToolkit.Mvvm (ObservableObject + 源生成器)
- **Item层**: 保持 Prism BindableBase (Mapperly编译时映射兼容)
- **基类整合**: 15个基类精简为5个核心基类
- **导航机制**: 保持 Prism INavigationAware (稳定的导航方案)
- **命令模式**: DelegateCommand -> [RelayCommand] 源生成器

## 背景

### 当前框架使用情况

| 组件类型 | 当前框架 | 文件数 | 问题 |
|----------|----------|--------|------|
| ViewModel | 混用 Prism/CommunityToolkit | ~30 | 模式不一致 |
| Item类 | Prism BindableBase | ~20 | 必须保持(Mapperly) |
| 基类 | 15个基类 | 15 | 继承层次复杂 |

### 现有ViewModel基类清单

```
ViewModels/Base/
├── ViewModelBase.cs              (Prism BindableBase)
├── CoreViewModelBase.cs          (CommunityToolkit - 已迁移)
├── NavigableViewModelBase.cs     (导航支持)
├── PageViewModelBase.cs          (页面ViewModel)
├── DialogViewModelBase.cs        (对话框)
├── ValidatingViewModelBase.cs    (验证支持)
├── ValidatingDialogViewModelBase.cs
├── DetailViewModelBase.cs
├── ComposableViewModelBase.cs
├── LightViewModelBase.cs
├── UnifiedViewModelBase.cs
├── UnifiedListViewModelBase.cs
├── ValidatableModelBase.cs
└── ValidationAccessors.cs
```

## 推荐方案

### 统一框架标准

| 组件类型 | 目标框架 | 说明 |
|----------|----------|------|
| **ViewModel** | CommunityToolkit.Mvvm | [ObservableProperty] + [RelayCommand] |
| **Item类** | Prism BindableBase | Mapperly编译时映射兼容 |
| **导航** | Prism INavigationAware | 成熟稳定，无需变更 |
| **对话框** | Prism IDialogAware | 保持现有机制 |

### 精简后的基类体系

```
ViewModels/Base/ (精简后5个)
├── CoreViewModelBase.cs      ← 最小核心基类 (ObservableObject)
├── NavigableViewModelBase.cs ← 支持Prism导航 (INavigationAware)
├── DialogViewModelBase.cs    ← 对话框基类 (IDialogAware)
├── ValidatingViewModelBase.cs← 带验证的ViewModel
└── PageViewModelBase.cs      ← 主内容页面ViewModel
```

### 迁移策略

**渐进式迁移**: 按模块逐个迁移，确保每次变更可验证

1. **Phase 1**: 完善CoreViewModelBase，确立标准模式
2. **Phase 2**: 迁移Shell层ViewModel
3. **Phase 3**: 迁移Roles层ViewModel
4. **Phase 4**: 迁移Modules层ViewModel
5. **Phase 5**: 清理废弃基类，更新文档

## 技术约束

### Mapperly源生成器链限制

**问题**: Mapperly无法识别[ObservableProperty]生成的属性

**解决方案**:
- Item类保持显式属性定义 (BindableBase)
- ViewModel自由使用源生成器
- MappingService作为隔离层

```
ViewModel ([ObservableProperty]可用)
    ↓ 依赖注入
MappingService (隔离层)
    ↓ 使用
Mapperly Mapper
    ↓ 映射
Item类 (BindableBase - 显式属性)
```

### 导航机制兼容

保持Prism导航接口，通过组合实现：

```csharp
public abstract partial class NavigableViewModelBase
    : CoreViewModelBase, INavigationAware, IRegionMemberLifetime
{
    // CommunityToolkit源生成器 + Prism导航
}
```

## 预期收益

| 指标 | 当前 | 目标 | 改善 |
|------|------|------|------|
| ViewModel基类数量 | 15 | 5 | -67% |
| 属性定义代码量 | ~500行 | ~200行 | -60% |
| 命令定义代码量 | ~300行 | ~100行 | -67% |
| 框架统一度 | 混用 | 统一标准 | 质量提升 |

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 迁移范围大 | 中 | 按模块渐进迁移，每次可验证 |
| 导航兼容性 | 低 | 保持Prism导航接口不变 |
| Item类误迁移 | 低 | 明确文档标注，代码审查 |

## 参考资料

- [CommunityToolkit.Mvvm官方文档](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Prism导航文档](https://docs.prismlibrary.com/docs/wpf/navigation/)
- adopt-mapperly-unified-mapping (已归档) - Mapperly兼容性约束
