# OpenSpec Proposal: resolve-mapperly-source-generator-conflict

## 概述

**变更ID**: resolve-mapperly-source-generator-conflict
**状态**: Draft
**创建日期**: 2026-01-06
**影响范围**: Desktop Item类 + Mapper类
**关联提案**: adopt-mapperly-unified-mapping (已归档)

## Why

### 问题根因

`standardize-viewmodel-framework`提案将Item类从`Prism.BindableBase`迁移到了`CommunityToolkit.Mvvm.ObservableObject + [ObservableProperty]`，这**违反**了`adopt-mapperly-unified-mapping`的核心架构约束。

### 技术限制

.NET不支持源生成器链（[dotnet/roslyn#57239](https://github.com/dotnet/roslyn/issues/57239)）：
- CommunityToolkit.Mvvm的`[ObservableProperty]`在编译时生成属性
- Mapperly在编译时读取属性生成映射代码
- 两个源生成器并行执行，Mapperly无法看到`[ObservableProperty]`生成的属性
- **结果**：Mapper生成空映射方法（无属性赋值）

### 当前症状

所有Desktop Mapper编译产生RMG警告：
```
RMG012: The member X on the mapping source type Y is not mapped to any member on the mapping target type Z
RMG020: Source member X on Y was not found on target type Z
```

生成的映射代码为空：
```csharp
// 生成的空映射（无效）
public partial UserItem ToItem(UserDetailDto dto)
{
    var target = new UserItem();
    return target;  // 没有任何属性赋值！
}
```

## What Changes

### 核心变更

1. **Item类重构**：从`ObservableObject + [ObservableProperty]`恢复为`BindableBase + 显式属性`
2. **Mapper类恢复**：从手动实现恢复为Mapperly `[Mapper]` partial方法
3. **删除过时方法**：移除Item类中标记为`[Obsolete]`的`FromDto()/ToDto()/ToInputDto()`方法

### 不变更内容

- ViewModel类保持使用CommunityToolkit.Mvvm（[ObservableProperty]可用于ViewModel）
- MappingService层架构保持不变
- DTO类保持不变

## 架构决策

### 框架分工标准

| 组件类型 | 框架标准 | 原因 |
|----------|----------|------|
| **Item类** | Prism BindableBase + 显式属性 | Mapperly兼容要求 |
| **ViewModel** | CommunityToolkit.Mvvm | 可自由使用[ObservableProperty] |
| **DTO** | POCO | 无框架依赖 |

### 属性定义规范

**Item类（BindableBase）**：
```csharp
public class UserItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }
}
```

**ViewModel（ObservableObject）**：
```csharp
public partial class UserListViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<UserItem> _items = new();
}
```

## 受影响文件

### Desktop Item类（需重构）

| 模块 | 文件 | 属性数量 |
|------|------|---------|
| Users | `UserItem.cs` | 14 |
| Patients | `PatientItem.cs` | 14 |
| Formula | `FormulaItem.cs` | ~12 |
| Formula | `FormulaHerbItem.cs` | ~8 |
| Consultation | `ConsultationItem.cs` | 14 |
| MedicalCase | `ConsultationItem.cs` | 14 |
| MedicalCase | `PrescriptionItem.cs` | ~15 |
| MedicalCase | `PrescriptionHerbItem.cs` | ~8 |
| MedicalCase | `MedicalCaseItem.cs` | ~15 |
| Herbs | `HerbItemDto.cs` | ~12 |

### Desktop Mapper类（需恢复）

| 模块 | 文件 |
|------|------|
| Users | `UserMapper.cs` |
| Patients | `PatientMapper.cs` |
| Formula | `FormulaMapper.cs`, `FormulaHerbItemMapper.cs` |
| Consultation | `ConsultationMapper.cs` |
| MedicalCase | `ConsultationMapper.cs`, `PrescriptionMapper.cs`, `MedicalCaseItemMapper.cs` |
| Herbs | `HerbMapper.cs` |

## 预期收益

| 指标 | 变更前 | 变更后 |
|------|--------|--------|
| 编译警告数 | 50+ RMG警告 | 0 |
| 映射正确性 | 空映射（功能异常） | 完整映射（正常工作） |
| 代码可维护性 | Mapper需手写 | Mapperly自动生成 |

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 属性绑定破坏 | 低 | BindableBase与ObservableObject绑定行为一致 |
| UI更新异常 | 低 | 两者都实现INotifyPropertyChanged |
| 编译错误 | 中 | 逐模块迁移，分步验证 |

## 参考资料

- [adopt-mapperly-unified-mapping设计文档](../archive/2026-01-06-adopt-mapperly-unified-mapping/design.md)
- [dotnet/roslyn#57239 - Source Generator Chaining](https://github.com/dotnet/roslyn/issues/57239)
- [Mapperly FAQ - 源生成器限制](https://mapperly.riok.app/docs/getting-started/faq/)
