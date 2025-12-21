# converter-conventions Specification Delta

## Purpose

定义Desktop层WPF转换器(IValueConverter)的统一管理规范。

## ADDED Requirements

### Requirement: CONV-001 Converter Location Convention

所有共享转换器 MUST 定义在 `LYBT.Desktop.Infrastructure/Converters/` 目录。

**规范**:
- 共享转换器: 被多个模块使用的转换器
- 模块专用转换器: 仅在单一模块内使用的转换器

#### Scenario: Shared converter location
- **GIVEN** 需要创建被多个模块共享的转换器
- **WHEN** 添加新转换器
- **THEN** MUST 放在 `Infrastructure/Converters/` 目录
- **AND** 在 `Converters.xaml` 中注册

#### Scenario: Module-specific converter location
- **GIVEN** 转换器仅在单一模块内使用
- **WHEN** 添加新转换器
- **THEN** MAY 放在模块的 `Converters/` 目录
- **AND** 在模块级资源字典中注册
- **AND** 命名应包含模块前缀 (如 `Shell_BoolToSidebarWidthConverter`)

### Requirement: CONV-002 Converter Naming Convention

转换器命名 MUST 遵循统一规范。

**规范**:
- 格式: `{Source}To{Target}Converter` 或 `{Operation}Converter`
- 使用完整单词，NOT 缩写

#### Scenario: Boolean to visibility converter naming
- **GIVEN** 创建Bool到Visibility的转换器
- **WHEN** 命名转换器
- **THEN** MUST 使用 `BooleanToVisibilityConverter`
- **AND** NOT 使用 `BoolToVisConverter` 或其他缩写

#### Scenario: Inverse converter naming
- **GIVEN** 创建反转逻辑的转换器
- **WHEN** 命名转换器
- **THEN** MUST 使用 `Inverse{Type}Converter` 格式
- **EXAMPLE** `InverseBooleanConverter`, `InverseNullToVisibilityConverter`

#### Scenario: Enum to target converter naming
- **GIVEN** 创建枚举到其他类型的转换器
- **WHEN** 命名转换器
- **THEN** MUST 使用 `{EnumName}To{Target}Converter` 格式
- **EXAMPLE** `ApiHealthStatusToColorConverter`, `StatusToColorConverter`

### Requirement: CONV-003 Converter Resource Dictionary

共享转换器 MUST 通过资源字典全局注册。

**规范**:
- 资源字典文件: `Infrastructure/Converters/Converters.xaml`
- 在 `App.xaml` 中合并该资源字典
- View中使用 `{StaticResource ConverterKey}` 引用

#### Scenario: Converter registration in resource dictionary
- **GIVEN** 在Infrastructure中添加新转换器
- **WHEN** 注册转换器
- **THEN** MUST 在 `Converters.xaml` 中添加实例
- **AND** 使用转换器类名作为 `x:Key`
- **EXAMPLE** `<converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />`

#### Scenario: Converter reference in View
- **GIVEN** View需要使用转换器
- **WHEN** 引用转换器
- **THEN** MUST 使用 `{StaticResource ConverterKey}`
- **AND** NOT 在View.Resources中重复定义

### Requirement: CONV-004 No Duplicate Converter Definition

View中 MUST NOT 重复定义已全局注册的转换器。

**规范**:
- 全局注册的转换器不应在View本地重新定义
- 如需不同配置，创建新的转换器类

#### Scenario: Prohibited local converter definition
- **GIVEN** `BooleanToVisibilityConverter` 已在全局注册
- **WHEN** View需要使用该转换器
- **THEN** MUST 使用 `{StaticResource BooleanToVisibilityConverter}`
- **AND** MUST NOT 在 `<UserControl.Resources>` 中重新定义

#### Scenario: Custom configuration converter
- **GIVEN** 需要不同配置的转换器 (如不同的TrueValue/FalseValue)
- **WHEN** 实现自定义行为
- **THEN** MUST 创建新的转换器类
- **AND** 使用描述性命名区分 (如 `BoolToCollapsedConverter`)

### Requirement: CONV-005 Converter Color Standards

颜色相关转换器 MUST 使用Fluent Design标准色。

**规范**:
- 成功/健康状态: #22C55E (绿色)
- 警告/检查中状态: #FBBF24 (黄色)
- 错误/不健康状态: #EF4444 (红色)
- 信息/中性状态: #3B82F6 (蓝色)

#### Scenario: Health status color mapping
- **GIVEN** 转换器将状态映射到颜色
- **WHEN** 返回颜色值
- **THEN** Healthy状态 MUST 返回 #22C55E
- **AND** Checking状态 MUST 返回 #FBBF24
- **AND** Unhealthy状态 MUST 返回 #EF4444

## Cross-References

- `ui-style-conventions` - UI样式规范
- `desktop-code-patterns` - Desktop代码模式
