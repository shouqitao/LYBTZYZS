# OpenSpec Proposal: adopt-mapperly-unified-mapping

## 概述

**变更ID**: adopt-mapperly-unified-mapping
**状态**: Draft
**创建日期**: 2026-01-05
**影响范围**: Server + Desktop (全栈)

## Why

- **AutoMapper商业化风险**: 2025年4月起AutoMapper转向商业授权模式
- **运行时性能**: AutoMapper基于反射，比编译时映射慢8.6倍
- **Desktop手写映射代码量大**: 约300+行分散在10+个文件，维护成本高
- **编译时错误检测缺失**: AutoMapper运行时错误难以发现

## What Changes

- **Server端**: AutoMapper → Mapperly编译时映射器
- **Desktop端**: 手写FromDto/ToDto → MappingService + Mapperly
- **架构**: 新增IMappingService接口层隔离映射逻辑
- **依赖**: 移除AutoMapper包，新增Riok.Mapperly包

## 背景

### 当前映射实现

| 层级 | 当前实现 | 问题 |
|------|----------|------|
| **Server** | AutoMapper 12.0.1 | 反射开销、运行时错误、2025年4月起商业化 |
| **Desktop** | 手写 FromDto/ToDto/ToInputDto | 大量重复代码、维护成本高、易出错 |

### 代码量统计

Desktop层现有手写映射方法示例：
- `ConsultationItem`: FromDto (15行) + ToDto (15行) + ToInputDto (10行) = 40行
- `PrescriptionItem`: FromDto (30行) + ToDto (22行) + ToInputDto (15行) = 67行
- 其他Item类: 约200+行

**总计**: 约300+行手写映射代码，分散在10+个文件中

## 研究结论

### 性能基准测试 (BenchmarkDotNet .NET 8.0.7)

| Mapper | 单次映射 | 10万对象 | 内存分配 | 相对速度 |
|--------|---------|----------|----------|----------|
| Manual | 4.094 ns | baseline | baseline | 1.0x |
| **Mapperly** | 4.077 ns | 28.6 ms | 3.9 MB | **1.0x** |
| AutoMapper | 35.244 ns | 36.4 ms | 4.8 MB | 8.61x slower |

**结论**: Mapperly与手写代码性能相同，比AutoMapper快8.6倍

### 技术对比

| 特性 | Mapperly | AutoMapper | 手写代码 |
|------|----------|------------|----------|
| 性能 | 最快(源生成器) | 较慢(反射) | 最快 |
| 编译时安全 | 是 | 否 | 是 |
| 维护成本 | 低 | 中 | 高 |
| License | MIT (免费) | 商业化 | N/A |
| 活跃度 | 高 | 高 | N/A |
| .NET 8/9支持 | 完整 | 完整 | N/A |
| AOT兼容 | 是 | 否 | 是 |

### WPF MVVM兼容性

**关键验证**: 本项目使用Prism的`BindableBase`，属性显式定义（非源生成器生成），Mapperly可以正常识别。

```csharp
// 当前实现 - Mapperly兼容
public class ConsultationItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
}
```

## 推荐方案

### 统一采用Mapperly + MappingService分层

**架构约束**：
- 项目同时使用 **Prism BindableBase** 和 **CommunityToolkit.Mvvm**
- Mapperly无法识别`[ObservableProperty]`生成的属性（源生成器链限制）
- **解决方案**: Item类保持BindableBase，通过MappingService隔离

**Desktop端**:
- 新增`IMappingService<TDto, TItem>`接口层
- 新增Mapperly映射器（编译时生成）
- Item类**保持**Prism BindableBase（Mapperly兼容）
- 删除Item类中的手写`FromDto()`/`ToDto()`/`ToInputDto()`方法
- ViewModel通过DI注入MappingService

**Server端（可选）**:
- 替换AutoMapper为Mapperly
- 删除AutoMapper依赖

### 架构图

```
┌────────────────────────────────────────────────────────────────┐
│  ViewModel层 (Prism OR CommunityToolkit)                       │
│  ─────────────────────────────────────                        │
│  • 可使用[ObservableProperty]简化ViewModel属性                  │
│  • 通过DI注入IMappingService                                   │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 依赖注入
┌────────────────────────────────────────────────────────────────┐
│  Mapping Service层 (新增)                                      │
│  ─────────────────────────                                    │
│  • IMappingService<TDto, TItem>                               │
│  • 封装Mapperly调用                                            │
│  • 处理ObservableCollection                                   │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 使用
┌────────────────────────────────────────────────────────────────┐
│  Mapperly Mapper层                                             │
│  ────────────────                                             │
│  • [Mapper] partial class                                      │
│  • 编译时生成映射代码                                           │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 映射
┌────────────────────────────────────────────────────────────────┐
│  Item层 (Prism BindableBase - Mapperly兼容)                    │
│  ────────────────────────────────────────                     │
│  • 显式属性定义 (非[ObservableProperty])                        │
│  • SetProperty + PropertyChanged                              │
└────────────────────────────────────────────────────────────────┘
```

### 框架标准化

| 组件 | 框架标准 | Mapperly兼容 | 说明 |
|------|----------|-------------|------|
| **Item类** | Prism BindableBase | 是 | 必须显式属性（Mapperly限制） |
| **ViewModel** | **CommunityToolkit.Mvvm** | N/A | **统一迁移**，使用源生成器 |
| MappingService | 无依赖 | 是 | 纯接口 |

### 为何Item类不能使用[ObservableProperty]

Mapperly与CommunityToolkit.Mvvm都是源生成器，.NET不支持源生成器链（[dotnet/roslyn#57239](https://github.com/dotnet/roslyn/issues/57239)）。因此：
- **ViewModel**: 可自由使用`[ObservableProperty]`和`[RelayCommand]`
- **Item类**: 必须使用显式属性定义（BindableBase），确保Mapperly可见

## 预期收益

| 指标 | 当前 | 目标 | 改善 |
|------|------|------|------|
| 映射代码行数 | ~300行 | ~50行 | -83% |
| 运行时性能 | AutoMapper反射 | 编译时生成 | 8.6x faster |
| 编译时错误检测 | 否 | 是 | 质量提升 |
| License风险 | 商业化风险 | MIT免费 | 风险消除 |

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 映射功能不完整 | 低 | Mapperly支持95%常见场景，复杂映射可用Before/After钩子 |
| 学习成本 | 低 | Mapperly API简单直观，文档完善 |
| 迁移工作量 | 中 | 分阶段迁移，先Desktop后Server |

## 参考资料

- [Mapperly官方文档](https://mapperly.riok.app/)
- [ABP Framework迁移到Mapperly](https://abp.io/community/articles/best-free-alternatives-to-automapper-in-.net-why-we-moved-to-mapperly-l9f5ii8s)
- [.NET Mappers Benchmark](https://github.com/mjebrahimi/DotNet-Mappers-Benchmark)
- [AutoMapper商业化公告](https://medium.com/@movsesaleksanyan7/comparing-riok-mapperly-and-automapper-a-performance-analysis-for-net-object-mapping-a4620eedf8f8)
