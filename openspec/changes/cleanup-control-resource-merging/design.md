# OpenSpec Design: cleanup-control-resource-merging

## 设计概述

本文档详细描述如何解决Master-Detail布局中的 `DependencyProperty.UnsetValue` 崩溃问题。

**解决方案包含两部分**：
1. **Converter迁移**：从 `StaticResource` 迁移到 `x:Static` 模式 (已完成)
2. **资源架构统一**：移除控件级资源合并，统一依赖 `Application.Resources` (2026-01-22定稿)

---

## 0. 资源架构统一方案 (2026-01-22定稿)

### 0.1 问题根因分析

之前的错误理解：认为ContentPresenter创建独立视觉树导致DynamicResource查找失败，因此需要在控件级别合并资源。

**正确理解**：
1. DynamicResource在运行时沿视觉树向上查找，最终到达Application.Resources
2. App.xaml已在Application.Resources级别配置了完整的资源
3. 控件级别重复合并相同资源导致资源键冲突和查找混乱

### 0.2 最终架构

```
Shell/App.xaml (资源入口 - Application.Resources)
├── Theme.Light.xaml        ← 主题 (合并 DesignTokens)
├── UnifiedComponents.xaml  ← 组件样式 + 按钮样式
└── Converters.xaml         ← 转换器 (保留向后兼容)

Infrastructure/Themes/ (资源定义，被App.xaml引用)
├── DesignTokens/
│   ├── Colors.Light.xaml   ← 所有颜色和 Brush 定义
│   ├── Typography.xaml     ← 字体定义
│   └── Spacing.xaml        ← 间距定义
├── Theme.Light.xaml        ← 主题入口 (合并 DesignTokens)
└── UnifiedComponents.xaml  ← 组件样式 (合并 Colors.Light.xaml + 按钮样式)

控件级别 (不合并任何外部资源字典)
├── 可以定义本地样式 (x:Key)
├── DynamicResource Brush → 从 Application.Resources 获取
├── StaticResource 按钮样式 → 从 Application.Resources 获取
└── Converter → 使用 x:Static converters:Cvt.XXX
```

### 0.3 已修复的控件

| 控件 | 变更 |
|------|------|
| `MasterDetailLayout.xaml` | 移除 Theme.Light.xaml 合并，保留本地样式定义 |
| `BaseDetailContainer.xaml` | 移除 UnifiedComponents.xaml 合并，保留本地样式定义 |

### 0.4 资源引用规范

| 资源类型 | 引用方式 | 来源 |
|----------|----------|------|
| **Style** | `DynamicResource` | Application.Resources |
| **Style.BasedOn** | `StaticResource` | Application.Resources |
| **Converter** | `x:Static converters:Cvt.XXX` | 静态实例 |
| **Brush/Color** | `DynamicResource` | Application.Resources |
| **本地样式** | 定义在控件 Resources 中 | 控件自身 |

---

---

## 1. 架构设计

### 1.1 当前架构（问题模式）

```
App.xaml
├── Theme.Light.xaml
├── UnifiedComponents.xaml
│   └── Converters (作为MergedDictionary)
└── Converters.xaml

控件级别：
├── MasterDetailLayout.xaml
│   └── <MergedDictionaries>  ← 重复合并
│       └── UnifiedComponents.xaml
├── HerbMasterDetailControl.xaml
│   └── 无资源合并（遵循规则A）
│       └── Converter={StaticResource XXX}  ← 运行时失败
```

**问题**：资源查找路径在ContentPresenter中断裂。

### 1.2 目标架构（x:Static模式）

```
App.xaml
├── Theme.Light.xaml
├── UnifiedComponents.xaml
│   └── Styles (无Converter依赖)
└── 不再需要Converters.xaml作为资源

Infrastructure/Converters/
├── ConverterInstances.cs  ← 静态实例提供者
│   └── public static class Cvt
│       ├── BoolToVis
│       ├── InverseBoolToVis
│       └── ...
└── (各个Converter实现类)

控件级别：
├── 所有控件
│   └── xmlns:converters="clr-namespace:..."
│   └── Converter={x:Static converters:Cvt.XXX}  ← 编译时解析
```

**优点**：
1. 无资源字典查找
2. 编译时类型检查
3. 无NameScope限制
4. 无重复资源加载

---

## 2. 组件设计

### 2.1 ConverterInstances.cs

**位置**：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/ConverterInstances.cs`

**设计原则**：
1. 单一静态类 `Cvt` 提供所有转换器实例
2. 使用 `readonly` 确保实例不被替换
3. 命名简洁但具描述性
4. XML文档注释说明用途

### 2.2 XAML迁移模式

#### 命名空间声明

在每个XAML文件的根元素添加：

```xml
xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure"
```

#### Binding.Converter替换

**模式1：简单Converter**

```xml
<!-- Before -->
<TextBlock Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"/>

<!-- After -->
<TextBlock Visibility="{Binding IsLoading, Converter={x:Static converters:Cvt.BoolToVis}}"/>
```

**模式2：DataGrid列**

```xml
<!-- Before -->
<DataGridTextColumn Binding="{Binding Gender, Converter={StaticResource EnumDescriptionConverter}}"/>

<!-- After -->
<DataGridTextColumn Binding="{Binding Gender, Converter={x:Static converters:Cvt.EnumDesc}}"/>
```

### 2.3 带参数的Converter处理

某些Converter需要参数（如 `BoolToDoubleConverter` 的 `TrueValue/FalseValue`）。

**策略**：在控件本地Resources中定义带参数的实例：

```xml
<UserControl.Resources>
    <converters:BoolToDoubleConverter x:Key="BoolToSidebarWidthConverter"
                                       TrueValue="200"
                                       FalseValue="56"/>
</UserControl.Resources>
```

---

## 3. 迁移任务清单

### 3.1 Phase 1: Infrastructure Controls

| 文件 | Converter引用 |
|------|---------------|
| MasterDetailLayout.xaml | BooleanToVisibilityConverter, InverseBooleanToVisibilityConverter |
| SidebarControl.xaml | BooleanToVisibilityConverter, InverseBooleanToVisibilityConverter, EnumDescriptionConverter, FirstCharacterConverter, ApiHealthStatusToColorConverter, ApiHealthStatusToTextConverter |
| PatientSearchControl.xaml | BooleanToVisibilityConverter, EnumDescriptionConverter |
| 其他Controls | 待检查 |

### 3.2 Phase 2: Module Controls

- Herbs: HerbMasterDetailControl, HerbEditControl, HerbViewControl
- Formula: FormulaMasterDetailControl, FormulaEditControl, FormulaViewControl
- Patients: PatientMasterDetailControl, PatientEditControl, PatientViewControl
- MedicalCase: MedicalCaseMasterDetailControl, MedicalCaseEditControl, MedicalCaseViewControl
- Users: UserMasterDetailControl, UserEditControl, UserViewControl

### 3.3 Phase 3: Views

- LoginWindow.xaml
- AdminHomeView.xaml
- ClinicalHomeView.xaml
- SystemSettingsView.xaml
- 其他Views

---

## 4. 测试计划

每个模块迁移后执行：

1. 启动应用
2. 导航到对应模块
3. 点击列表中的任意项
4. 验证详情显示正常

---

## 附录：转换器映射表

| 原StaticResource Key | 新x:Static引用 |
|---------------------|----------------|
| `BooleanToVisibilityConverter` | `Cvt.BoolToVis` |
| `InverseBooleanToVisibilityConverter` | `Cvt.InverseBoolToVis` |
| `InverseBooleanConverter` | `Cvt.InverseBool` |
| `EnumDescriptionConverter` | `Cvt.EnumDesc` |
| `FirstCharacterConverter` | `Cvt.FirstChar` |
| `ApiHealthStatusToColorConverter` | `Cvt.ApiStatusToColor` |
| `ApiHealthStatusToTextConverter` | `Cvt.ApiStatusToText` |
| `NullToVisibilityConverter` | `Cvt.NullToVis` |
| `InverseNullToVisibilityConverter` | `Cvt.InverseNullToVis` |
| `StringToVisibilityConverter` | `Cvt.StringToVis` |
| `StatusToColorConverter` | `Cvt.StatusToColor` |
| `DecocteMethodToVisibilityConverter` | `Cvt.DecocteMethodToVis` |
| `PatientCardDisplayModeToVisibilityConverter` | `Cvt.PatientCardModeToVis` |
