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

## Mapperly 统一映射架构 (OpenSpec: adopt-mapperly-unified-mapping)

### 核心组件

```
Mapping/
├── IMappingService.cs           # 泛型接口 IMappingService<TDto, TInputDto, TItem>
└── MappingServiceBase.cs        # 抽象基类，提供集合映射实现
```

### 使用模式

**1. MasterDetailViewModel (推荐模式)**
```csharp
// DI 注入
private readonly IMappingService<XXXDetailDto, XXXInputDto, XXXItem> _mappingService;

// 加载时
var item = _mappingService.ToItem(dto);

// 保存时
var inputDto = _mappingService.ToInputDto(item);
```

**2. Controls 就地更新模式** (Herbs Controls)
- 使用 `LoadFromDto()` 实例方法填充现有对象
- 使用 `ToDto()` 实例方法导出数据
- 适合长生命周期的 Control ViewModel

**3. 手动属性复制** (复杂场景如 MedicalCaseWorkspaceViewModel)
- 使用 DataLoader 缓存原始 DTO
- 通过手动属性赋值初始化子 ViewModel

### 各模块 Mapper 位置

| 模块 | Mapper 类 | 位置 |
|------|-----------|------|
| Herbs | HerbMapper | `Mappers/HerbMapper.cs` |
| Formula | FormulaMapper, FormulaHerbItemMapper, FormulaDetailModelMapper | `Mappers/` |
| MedicalCase | MedicalCaseMapper | `Mappers/MedicalCaseMapper.cs` |
| Patients | PatientMapper | `Mappers/PatientMapper.cs` |
| Users | UserMapper | `Mappers/UserMapper.cs` |

### 已废弃的 FromDto/ToDto 方法

所有 Item 类中的静态 `FromDto()` 和实例 `ToDto()` 方法已标记 `[Obsolete]`：
- 请使用对应模块的 `XXXMappingService.ToItem()` / `ToDto()` / `ToInputDto()` 替代
- 这些方法将在后续版本移除
