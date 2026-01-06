# Technical Design: adopt-mapperly-unified-mapping

## 1. 架构约束分析

### 1.1 当前框架使用情况

| 基类 | 框架 | 特点 | 使用位置 |
|------|------|------|----------|
| `ViewModelBase` | Prism.Mvvm.BindableBase | 显式属性+SetProperty | LYBT.Desktop.Models |
| `MasterDetailViewModelBase` | CommunityToolkit.Mvvm.ObservableObject | [ObservableProperty]源生成器 | LYBT.Desktop.Infrastructure |
| `ConsultationItem` | Prism.Mvvm.BindableBase | 显式属性 | Item类 |

### 1.2 Mapperly兼容性约束

**问题**: Mapperly无法识别`[ObservableProperty]`生成的属性（[dotnet/roslyn#57239](https://github.com/dotnet/roslyn/issues/57239)）

| 场景 | Mapperly兼容 | 说明 |
|------|-------------|------|
| 显式属性 (Prism BindableBase) | 是 | 编译时可见 |
| `[ObservableProperty]` (CommunityToolkit) | **否** | 源生成器链不支持 |
| 普通POCO类 | 是 | 编译时可见 |
| DTO (Shared层) | 是 | 无INotifyPropertyChanged |

### 1.3 设计决策

**核心原则**: Item类保持使用Prism BindableBase，确保Mapperly兼容

```
┌────────────────────────────────────────────────────────────────┐
│  ViewModel层                                                   │
│  ─────────────                                                │
│  • Prism OR CommunityToolkit (自由选择)                        │
│  • 不直接调用Mapper，通过MappingService                        │
│  • [ObservableProperty]可用于ViewModel自有属性                 │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 依赖注入
┌────────────────────────────────────────────────────────────────┐
│  Mapping Service层 (新增)                                      │
│  ─────────────────────                                        │
│  • IMappingService<TDto, TItem> 接口                          │
│  • 封装Mapperly调用 + 特殊转换逻辑                             │
│  • 处理ObservableCollection等集合类型                          │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 使用
┌────────────────────────────────────────────────────────────────┐
│  Mapperly Mapper层                                             │
│  ────────────────                                             │
│  • [Mapper] partial class                                      │
│  • DTO ↔ Item 编译时映射                                      │
│  • 忽略UI状态字段                                              │
└────────────────────────────────────────────────────────────────┘
                           │
                           ▼ 映射
┌────────────────────────────────────────────────────────────────┐
│  Item层 (保持Prism BindableBase)                               │
│  ─────────────────────────────                                │
│  • 显式属性定义 (Mapperly兼容)                                 │
│  • SetProperty + PropertyChanged                              │
│  • 计算属性、验证逻辑保留                                      │
└────────────────────────────────────────────────────────────────┘
```

## 2. 详细设计

### 2.1 Mapperly Mapper定义

```csharp
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 诊断数据映射器 - 编译时生成
/// </summary>
[Mapper]
public partial class ConsultationMapper
{
    // DTO → Item (从API加载)
    [MapperIgnoreTarget(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsExpanded))]
    public partial ConsultationItem ToItem(ConsultationDetailDto dto);

    // Item → DTO (保存时)
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]      // 计算属性
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))] // 计算属性
    public partial ConsultationDetailDto ToDto(ConsultationItem item);

    // Item → InputDto (创建/更新)
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.CreatedAt))]       // 审计字段
    [MapperIgnoreSource(nameof(ConsultationItem.UpdatedAt))]       // 审计字段
    public partial ConsultationInputDto ToInputDto(ConsultationItem item);
}
```

### 2.2 Mapping Service接口

```csharp
namespace LYBT.Desktop.Infrastructure.Mapping;

/// <summary>
/// 通用映射服务接口
/// </summary>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TItem">Item类型</typeparam>
public interface IMappingService<TDto, TItem>
    where TDto : class
    where TItem : class
{
    /// <summary>
    /// DTO转Item
    /// </summary>
    TItem ToItem(TDto dto);

    /// <summary>
    /// Item转DTO
    /// </summary>
    TDto ToDto(TItem item);

    /// <summary>
    /// DTO列表转Item列表
    /// </summary>
    IEnumerable<TItem> ToItems(IEnumerable<TDto> dtos);

    /// <summary>
    /// DTO转Item并填充到ObservableCollection
    /// </summary>
    void ToItemsInto(IEnumerable<TDto> dtos, ObservableCollection<TItem> target);
}

/// <summary>
/// 支持InputDto的映射服务接口
/// </summary>
public interface IMappingService<TDto, TInputDto, TItem> : IMappingService<TDto, TItem>
    where TDto : class
    where TInputDto : class
    where TItem : class
{
    /// <summary>
    /// Item转InputDto (用于创建/更新)
    /// </summary>
    TInputDto ToInputDto(TItem item);
}
```

### 2.3 Mapping Service实现

```csharp
namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 诊断映射服务实现
/// </summary>
public class ConsultationMappingService
    : IMappingService<ConsultationDetailDto, ConsultationInputDto, ConsultationItem>
{
    private readonly ConsultationMapper _mapper = new();

    public ConsultationItem ToItem(ConsultationDetailDto dto)
        => _mapper.ToItem(dto);

    public ConsultationDetailDto ToDto(ConsultationItem item)
        => _mapper.ToDto(item);

    public ConsultationInputDto ToInputDto(ConsultationItem item)
        => _mapper.ToInputDto(item);

    public IEnumerable<ConsultationItem> ToItems(IEnumerable<ConsultationDetailDto> dtos)
        => dtos.Select(_mapper.ToItem);

    public void ToItemsInto(
        IEnumerable<ConsultationDetailDto> dtos,
        ObservableCollection<ConsultationItem> target)
    {
        target.Clear();
        foreach (var dto in dtos)
        {
            target.Add(_mapper.ToItem(dto));
        }
    }
}
```

### 2.4 DI注册

```csharp
// MedicalCaseModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册映射服务
    containerRegistry.RegisterSingleton<
        IMappingService<ConsultationDetailDto, ConsultationInputDto, ConsultationItem>,
        ConsultationMappingService>();

    containerRegistry.RegisterSingleton<
        IMappingService<PrescriptionDetailDto, PrescriptionInputDto, PrescriptionItem>,
        PrescriptionMappingService>();
}
```

### 2.5 ViewModel使用示例

```csharp
// ViewModel中使用 - 与框架无关
public class MedicalCaseMasterDetailViewModel : MasterDetailViewModelBase<...>
{
    private readonly IMappingService<ConsultationDetailDto, ConsultationInputDto, ConsultationItem>
        _consultationMapping;

    public MedicalCaseMasterDetailViewModel(
        IMappingService<ConsultationDetailDto, ConsultationInputDto, ConsultationItem> consultationMapping,
        ...)
    {
        _consultationMapping = consultationMapping;
    }

    private async Task LoadConsultationAsync(ConsultationDetailDto dto)
    {
        // 使用映射服务，而非直接调用FromDto
        ConsultationItem = _consultationMapping.ToItem(dto);
    }

    private async Task SaveAsync()
    {
        // 使用映射服务，而非直接调用ToInputDto
        var inputDto = _consultationMapping.ToInputDto(ConsultationItem);
        await _api.SaveAsync(inputDto);
    }
}
```

## 3. Item类改造

### 3.1 保留内容

- `BindableBase`继承（Mapperly兼容）
- 显式属性定义 + SetProperty
- 计算属性（IsDiagnosisComplete, DisplayText等）
- 业务逻辑方法（Clear, Validate等）

### 3.2 删除内容

- `FromDto()` 静态方法
- `ToDto()` 实例方法
- `ToInputDto()` 实例方法

### 3.3 改造前后对比

**改造前** (ConsultationItem.cs):
```csharp
public class ConsultationItem : BindableBase
{
    // 属性定义...

    // 删除: 映射方法
    public static ConsultationItem FromDto(ConsultationDetailDto dto) { ... }
    public ConsultationDetailDto ToDto() { ... }
    public ConsultationInputDto ToInputDto() { ... }
}
```

**改造后** (ConsultationItem.cs):
```csharp
public class ConsultationItem : BindableBase
{
    // 属性定义保持不变

    // 映射方法已移至ConsultationMapper

    // 业务逻辑方法保留
    public bool IsDiagnosisComplete => ...;
    public void Clear() { ... }
}
```

## 4. 框架标准化

### 4.1 统一标准

| 组件类型 | 框架标准 | Mapperly兼容 | 说明 |
|----------|----------|-------------|------|
| **Item类** | Prism BindableBase | 是 | 必须显式属性（Mapperly限制） |
| **ViewModel** | **CommunityToolkit.Mvvm** | N/A | 统一迁移，使用源生成器 |
| **DTO** | POCO | 是 | Shared层 |
| **MappingService** | 无框架依赖 | 是 | 纯C# |

### 4.2 ViewModel层标准化

**所有ViewModel统一使用CommunityToolkit.Mvvm**：

```csharp
// 新标准 ViewModel
public partial class MedicalCaseMasterDetailViewModel : ObservableObject, INavigationAware
{
    // 使用[ObservableProperty]简化属性
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Item类通过MappingService获取，不参与源生成器链
    [ObservableProperty]
    private ConsultationItem? _consultationItem;

    // 使用[RelayCommand]简化命令
    [RelayCommand]
    private async Task SaveAsync()
    {
        var inputDto = _mappingService.ToInputDto(ConsultationItem!);
        await _api.SaveAsync(inputDto);
    }
}
```

### 4.3 为何Item类不能使用[ObservableProperty]

**技术限制**：Mapperly与CommunityToolkit.Mvvm都是源生成器，.NET不支持源生成器链（[dotnet/roslyn#57239](https://github.com/dotnet/roslyn/issues/57239)）

```
编译顺序问题:
┌──────────────────────┐      ┌──────────────────────┐
│ CommunityToolkit.Mvvm │ ──► │ 生成 public 属性      │
│ [ObservableProperty]  │      │ (编译后才可见)        │
└──────────────────────┘      └──────────────────────┘
                                        │
                                        ▼ Mapperly无法看到
┌──────────────────────┐      ┌──────────────────────┐
│ Mapperly             │ ──► │ 找不到属性，映射失败    │
│ [Mapper]             │      │                      │
└──────────────────────┘      └──────────────────────┘
```

**解决方案**：Item类使用显式属性（BindableBase），ViewModel可自由使用[ObservableProperty]

## 5. 渐进迁移策略

### Phase 1: 基础设施
1. 添加Mapperly包引用
2. 创建`IMappingService`接口
3. 创建MappingService基类

### Phase 2: 模块迁移
按模块逐个迁移：
1. 创建Mapper类
2. 创建MappingService实现
3. 注册DI
4. 更新ViewModel使用MappingService
5. 删除Item类中的映射方法

### Phase 3: 验证
1. 全量编译
2. 单元测试
3. 功能测试

## 6. 未来演进路径

当.NET支持源生成器链（如果未来支持）：
1. Item类可迁移到CommunityToolkit.Mvvm的`[ObservableProperty]`
2. Mapper定义保持不变
3. MappingService层作为隔离，迁移成本低

## 7. 参考资料

- [Mapperly FAQ - Source Generator Chaining](https://mapperly.riok.app/docs/getting-started/faq/)
- [Prism BindableBase文档](https://docs.prismlibrary.com/docs/mvvm/bindablebase.html)
- [CommunityToolkit.Mvvm文档](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [dotnet/roslyn#57239 - Source Generator Chaining](https://github.com/dotnet/roslyn/issues/57239)
