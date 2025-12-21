# Design: consolidate-wpf-converters

## Architecture Overview

```
LYBT.Desktop.Infrastructure/
├── Converters/
│   ├── Converters.xaml              # [NEW] 转换器资源字典
│   ├── BooleanToVisibilityConverter.cs
│   ├── InverseBooleanToVisibilityConverter.cs
│   ├── InverseBooleanConverter.cs
│   ├── BoolToBrushConverter.cs
│   ├── BoolToDoubleConverter.cs
│   ├── BoolToStringConverter.cs
│   ├── StringToVisibilityConverter.cs
│   ├── NullToVisibilityConverter.cs
│   ├── InverseNullToVisibilityConverter.cs
│   ├── EnumDescriptionConverter.cs
│   ├── FirstCharacterConverter.cs
│   ├── StatusToColorConverter.cs
│   ├── DecocteMethodToVisibilityConverter.cs
│   ├── ApiHealthStatusToColorConverter.cs
│   └── ApiHealthStatusToTextConverter.cs
└── Themes/
    └── UnifiedComponents.xaml       # 合并Converters.xaml
```

## Converter Registry Design

### Converters.xaml 资源字典

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters">
    
    <!-- Boolean Converters -->
    <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
    <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter" />
    <converters:InverseBooleanConverter x:Key="InverseBooleanConverter" />
    <converters:BoolToBrushConverter x:Key="BoolToBrushConverter" />
    <converters:BoolToDoubleConverter x:Key="BoolToDoubleConverter" />
    <converters:BoolToStringConverter x:Key="BoolToStringConverter" />
    
    <!-- Visibility Converters -->
    <converters:StringToVisibilityConverter x:Key="StringToVisibilityConverter" />
    <converters:NullToVisibilityConverter x:Key="NullToVisibilityConverter" />
    <converters:InverseNullToVisibilityConverter x:Key="InverseNullToVisibilityConverter" />
    
    <!-- Enum/Status Converters -->
    <converters:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
    <converters:StatusToColorConverter x:Key="StatusToColorConverter" />
    <converters:ApiHealthStatusToColorConverter x:Key="ApiHealthStatusToColorConverter" />
    <converters:ApiHealthStatusToTextConverter x:Key="ApiHealthStatusToTextConverter" />
    
    <!-- String Converters -->
    <converters:FirstCharacterConverter x:Key="FirstCharacterConverter" />
    
    <!-- Domain-specific Converters -->
    <converters:DecocteMethodToVisibilityConverter x:Key="DecocteMethodToVisibilityConverter" />
    
</ResourceDictionary>
```

## Duplicate Resolution

### 1. FirstCharConverter vs FirstCharacterConverter

**保留**: `FirstCharacterConverter` (Infrastructure)

**原因**:
- 更规范的命名(完整单词)
- 已在App.xaml全局注册
- 包含ToUpper处理更完善

**迁移**: 删除Shell/Converters/FirstCharConverter.cs

### 2. ApiHealthStatusToColorConverter (重复)

**保留**: Infrastructure版本

**变更**: 统一颜色值为Fluent Design标准色
```csharp
// 统一后的颜色值
Healthy   => #22C55E (绿色)
Checking  => #FBBF24 (黄色) 
Unhealthy => #EF4444 (红色)
```

**迁移**: 删除Shell/Converters/ApiHealthStatusToColorConverter.cs

### 3. ApiHealthStatusToTextConverter (完全重复)

**保留**: Infrastructure版本

**迁移**: 删除Shell/Converters/ApiHealthStatusToTextConverter.cs

### 4. InvertedBoolConverter vs InverseBooleanConverter

**保留**: `InverseBooleanConverter` (Infrastructure)

**原因**:
- 已在Infrastructure中定义
- 提供静态Instance单例模式
- 命名更符合WPF惯例

**迁移**: 删除MedicalCase/Converters/InvertedBoolConverter.cs

## View Cleanup Pattern

### Before (本地定义)
```xml
<UserControl.Resources>
    <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter"/>
</UserControl.Resources>
```

### After (使用全局资源)
```xml
<!-- 无需本地定义，直接使用全局资源 -->
<TextBlock Visibility="{Binding IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}" />
```

## App.xaml Integration

### Before
```xml
<converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
<converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter" />
<!-- 手动逐个定义 -->
```

### After
```xml
<ResourceDictionary.MergedDictionaries>
    <!-- 其他资源字典 -->
    <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Converters/Converters.xaml" />
</ResourceDictionary.MergedDictionaries>
<!-- 无需手动定义，自动继承所有转换器 -->
```

## Naming Convention

转换器命名规范:

| 类型 | 命名模式 | 示例 |
|------|----------|------|
| Bool转换 | `Bool{To/From}{Target}Converter` | `BoolToVisibilityConverter` |
| 反转转换 | `Inverse{Source}Converter` | `InverseBooleanConverter` |
| Null检查 | `{Null/NotNull}To{Target}Converter` | `NullToVisibilityConverter` |
| 枚举转换 | `{EnumType}To{Target}Converter` | `StatusToColorConverter` |
| 字符串处理 | `{Operation}Converter` | `FirstCharacterConverter` |

## Trade-offs

### 选择: 资源字典 vs 静态实例

**资源字典方式** (选择)
- 优点: XAML原生支持，IDE智能提示好
- 缺点: 每次引用创建新实例(可忽略)

**静态实例方式** (备选)
- 优点: 单例复用，内存效率高
- 缺点: 需要x:Static引用，IDE支持差

**决策**: 采用资源字典方式，符合WPF最佳实践

### 选择: 合并到UnifiedComponents vs 独立Converters.xaml

**独立Converters.xaml** (选择)
- 优点: 职责分离，易维护
- 缺点: 多一个资源文件

**合并到UnifiedComponents.xaml** (备选)
- 优点: 减少文件数量
- 缺点: UnifiedComponents.xaml已经较大

**决策**: 采用独立文件，保持单一职责

## Dependencies

- 无外部依赖变化
- 内部依赖: View → Infrastructure (已存在)
