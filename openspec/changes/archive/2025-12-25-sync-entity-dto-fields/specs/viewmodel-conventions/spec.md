# viewmodel-conventions (delta)

## ADDED Requirements

### Requirement: VM-CONV-016 DependencyProperty类型与DTO同步

系统 **SHALL** 确保WPF控件的DependencyProperty类型与对应DTO字段类型完全一致。

#### Scenario: 可空decimal属性
- **Given** DTO定义了 `decimal? CostPrice` 属性
- **When** 创建对应的DependencyProperty
- **Then** 必须使用 `typeof(decimal?)` 作为属性类型
- **And** 默认值必须为 `null` 而非 `0m`
- **And** PropertyMetadata必须使用 `BindsTwoWayByDefault`

```csharp
// 正确示例
public static readonly DependencyProperty CostPriceProperty =
    DependencyProperty.Register(
        nameof(CostPrice),
        typeof(decimal?),  // 可空类型
        typeof(HerbEditControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

public decimal? CostPrice
{
    get => (decimal?)GetValue(CostPriceProperty);
    set => SetValue(CostPriceProperty, value);
}
```

---

### Requirement: VM-CONV-017 ViewModel验证与Validator一致

系统 **SHALL** 确保ViewModel的验证逻辑与服务端FluentValidator保持一致。

#### Scenario: 可空字段验证模式
- **Given** 字段在Entity/DTO中定义为可空（非必填）
- **When** 在ViewModel中实现验证
- **Then** 必须使用 `if (value.HasValue && condition)` 模式
- **And** 不应检查 `!value.HasValue` 作为错误条件

```csharp
// 正确示例 - 可空字段只在有值时验证
if (detail.CostPrice.HasValue && detail.CostPrice <= 0)
{
    await ShowErrorMessageAsync("成本价必须大于0");
    return false;
}

// 错误示例 - 不应要求可空字段必须有值
if (!detail.CostPrice.HasValue || detail.CostPrice <= 0)
{
    await ShowErrorMessageAsync("进价必须大于0");
    return false;
}
```

---

### Requirement: VM-CONV-018 XAML绑定与可空类型

系统 **SHALL** 为可空类型的绑定添加适当的格式化和空值处理。

#### Scenario: 可空decimal绑定
- **Given** 属性类型为 `decimal?`
- **When** 在XAML中绑定到TextBox
- **Then** 必须添加 `TargetNullValue=''` 处理空值显示
- **And** 应添加 `StringFormat={}{0:0.##}` 格式化小数

```xml
<!-- 正确示例 -->
<TextBox Text="{Binding CostPrice, Mode=TwoWay, StringFormat={}{0:0.##}, TargetNullValue=''}"/>

<!-- 错误示例 - 缺少空值处理 -->
<TextBox Text="{Binding CostPrice, Mode=TwoWay}"/>
```

#### Scenario: 小数输入绑定
- **Given** 需要输入小数值
- **When** 配置TextBox绑定
- **Then** 应避免 `UpdateSourceTrigger=PropertyChanged`（会导致输入"10."时立即转换）
- **And** 应使用默认的 `LostFocus` 触发器

```xml
<!-- 正确示例 - 使用默认LostFocus -->
<TextBox Text="{Binding Price, Mode=TwoWay, StringFormat={}{0:0.##}}"/>

<!-- 问题示例 - PropertyChanged会导致小数输入问题 -->
<TextBox Text="{Binding Price, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```
