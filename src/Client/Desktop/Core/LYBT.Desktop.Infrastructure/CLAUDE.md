# LYBT.Desktop.Infrastructure 模块说明

## XAML资源加载顺序规则

**重要**: WPF资源字典中的样式定义顺序敏感，`BasedOn`引用的样式必须在被引用之前定义。

### 已知问题及解决方案

1. **BaseDataGridCell 前向引用问题** (2026-01-04修复)
   - 问题: `MasterDetailDataGridCellStyle` 使用 `BasedOn="{StaticResource BaseDataGridCell}"`，但 `BaseDataGridCell` 定义在后面
   - 解决: 将 `BaseDataGridCell` 移到 `MasterDetailDataGridCellStyle` 之前

2. **跨文件资源引用问题** (2026-01-04修复)
   - 问题: `ValidationStyles.xaml` 中的 `ValidatingTextBoxStyle` 继承自 `EditableTextBoxStyle`，但 `ValidationStyles.xaml` 在 `UnifiedComponents.xaml` 开头被合并
   - 解决: 将 `ValidatingTextBoxStyle` 迁移到 `UnifiedComponents.xaml`，放在 `EditableTextBoxStyle` 之后

### 资源文件结构

```
Themes/
├── UnifiedComponents.xaml  # 主资源字典，合并其他资源
│   ├── 合并 ValidationStyles.xaml (开头)
│   ├── EditableTextBoxStyle (第412行)
│   ├── ValidatingTextBoxStyle (第478行，继承EditableTextBoxStyle)
│   ├── BaseDataGridCell (第633行)
│   └── MasterDetailDataGridCellStyle (第658行，继承BaseDataGridCell)
└── ValidationStyles.xaml   # 验证相关样式（基础样式，无继承依赖）
```

### 添加新样式的规则

1. 如果新样式使用 `BasedOn` 继承，确保基类样式已在前面定义
2. 跨文件继承时，检查资源字典合并顺序
3. 优先在 `UnifiedComponents.xaml` 中定义有继承关系的样式

---

## Mapperly 直接映射架构 (OpenSpec: standardize-api-architecture)

### 架构演进 (2026-01-07)

**已删除**: `IMappingService<TDto, TInputDto, TItem>` 接口和 `MappingServiceBase` 基类
- 原因: MappingService是Mapper的薄包装层，增加了不必要的间接性
- 方案: ViewModel直接实例化Mapper，无需DI注入

### 当前模式

**直接Mapper实例化 (唯一推荐模式)**
```csharp
public class XXXMasterDetailViewModel
{
    // 直接实例化，无需DI
    private readonly XXXMapper _mapper = new();

    // 加载时
    var item = _mapper.ToItem(dto);

    // 保存时
    var inputDto = _mapper.ToInputDto(item);
}
```

### 各模块 Mapper 位置

| 模块 | Mapper 类 | 位置 |
|------|-----------|------|
| Herbs | HerbMapper | `Mappers/HerbMapper.cs` |
| Formula | FormulaMapper, FormulaDetailModelMapper | `Mappers/` |
| MedicalCase | MedicalCaseDetailModelMapper | `Mappers/` |
| Patients | PatientMapper | `Mappers/PatientMapper.cs` |
| Users | UserMapper | `Mappers/UserMapper.cs` |

### 已废弃的 FromDto/ToDto 方法

所有 Item 类中的静态 `FromDto()` 和实例 `ToDto()` 方法已标记 `[Obsolete]`：
- 请使用对应模块的 `XXXMapper.ToItem()` / `ToDto()` / `ToInputDto()` 替代
- 这些方法将在后续版本移除

### Mapperly + CommunityToolkit.Mvvm 源生成器兼容性

**重要**: Mapperly源生成器与CommunityToolkit.Mvvm的`[ObservableProperty]`存在编译顺序冲突。

**问题**: Mapperly在编译时验证属性存在性，但`[ObservableProperty]`生成的属性尚未生成，导致RMG005/RMG006错误。

**解决方案**: 对于源生成属性，使用`[MapperIgnore*]`忽略，在包装方法中手动映射：

```csharp
// 错误模式（编译失败）
[MapProperty(nameof(Dto.CaseStatus), "CaseStatus")]
public partial Item ToItemCore(Dto dto);

// 正确模式
[MapperIgnoreTarget("CaseStatus")]  // 字符串字面量
[MapperIgnoreSource(nameof(Dto.CaseStatus))]
public partial Item ToItemCore(Dto dto);

public Item ToItem(Dto dto)
{
    var item = ToItemCore(dto);
    item.CaseStatus = dto.CaseStatus;  // 手动映射
    return item;
}
```

**详细说明**: 参见 `MedicalCase/CLAUDE.md` 的"Mapperly与CommunityToolkit.Mvvm源生成器兼容性"章节
