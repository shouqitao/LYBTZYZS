# UnifiedStatusBadge 组件

## 概述

UnifiedStatusBadge 是统一的状态标签组件，提供彩色徽章样式显示状态文本。

**命名空间**: `LYBT.Desktop.Infrastructure.Controls`
**继承**: `UserControl`
**Issue**: #1840, #1844

**典型场景**:
- 数据表格中显示状态
- 业务对象状态展示
- 枚举值可视化

## 快速开始

最简单的使用示例：

```xaml
<controls:UnifiedStatusBadge
    Text="正常"
    Type="Success" />
```

## API参考

### 依赖属性

| 属性名 | 类型 | 默认值 | 绑定模式 | 说明 |
|-------|------|--------|---------|------|
| `Text` | `string` | `""` | `OneWay` | 状态文本 |
| `Type` | `BadgeType` | `Neutral` | `OneWay` | 徽章类型（决定颜色） |
| `BadgeBackground` | `Brush` | （自动） | （只读） | 徽章背景色（自动根据Type设置） |
| `BadgeForeground` | `Brush` | （自动） | （只读） | 徽章前景色（自动根据Type设置） |

### BadgeType 枚举

| 枚举值 | 背景色 | 使用场景 |
|-------|--------|---------|
| `Success` | 绿色 (#34A853) | 正常、成功、已完成、已审核 |
| `Warning` | 橙色 (#FBBC04) | 警告、待处理、草稿、进行中 |
| `Danger` | 红色 (#EA4335) | 错误、失败、已删除、已拒绝 |
| `Info` | 蓝色 (#4285F4) | 信息、提示、角色标识 |
| `Neutral` | 灰色 (#9E9E9E) | 中性、默认、未知 |

### 颜色资源映射

组件会优先使用资源字典中的颜色，回退到硬编码颜色：

```
Success → SuccessBrush → #34A853
Warning → WarningBrush → #FBBC04
Danger  → DangerBrush  → #EA4335
Info    → InfoBrush    → #4285F4
Neutral → NeutralBrush → #9E9E9E
```

## 使用示例

### 示例1: 基础用法（固定文本）

**XAML**:
```xaml
<controls:UnifiedStatusBadge
    Text="正常"
    Type="Success" />
```

### 示例2: 绑定枚举值（使用转换器）

**XAML**:
```xaml
<UserControl.Resources>
    <infrastructure:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
</UserControl.Resources>

<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />
```

**C# 枚举定义**:
```csharp
using System.ComponentModel;

public enum PatientStatus
{
    [Description("正常")]
    Active,

    [Description("已删除")]
    Deleted
}
```

**ViewModel**:
```csharp
public PatientStatus Status { get; set; } = PatientStatus.Active;
// 显示为 "正常" (绿色Success徽章)
```

### 示例3: 在DataGrid中使用

**XAML**:
```xaml
<controls:UnifiedManagementTable ItemsSource="{Binding Items}">
    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />

        <!-- 状态列 -->
        <DataGridTemplateColumn Header="状态" Width="100">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <controls:UnifiedStatusBadge
                        Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
                        Type="Success" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

### 示例4: 根据枚举值动态选择Type

如果需要根据不同的状态值显示不同颜色，有2种方法：

**方法1: 使用DataTrigger（推荐）**

```xaml
<DataGridTemplateColumn Header="状态" Width="100">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <controls:UnifiedStatusBadge
                Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}">
                <controls:UnifiedStatusBadge.Style>
                    <Style TargetType="controls:UnifiedStatusBadge">
                        <Setter Property="Type" Value="Neutral" />
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding Status}" Value="Active">
                                <Setter Property="Type" Value="Success" />
                            </DataTrigger>
                            <DataTrigger Binding="{Binding Status}" Value="Deleted">
                                <Setter Property="Type" Value="Danger" />
                            </DataTrigger>
                            <DataTrigger Binding="{Binding Status}" Value="Draft">
                                <Setter Property="Type" Value="Warning" />
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </controls:UnifiedStatusBadge.Style>
            </controls:UnifiedStatusBadge>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**方法2: 使用自定义转换器**

```csharp
public class StatusToBadgeTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PatientStatus status)
        {
            return status switch
            {
                PatientStatus.Active => BadgeType.Success,
                PatientStatus.Deleted => BadgeType.Danger,
                _ => BadgeType.Neutral
            };
        }
        return BadgeType.Neutral;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

```xaml
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="{Binding Status, Converter={StaticResource StatusToBadgeTypeConverter}}" />
```

### 示例5: 所有5种类型展示

**XAML**:
```xaml
<StackPanel Orientation="Horizontal" Spacing="8">
    <controls:UnifiedStatusBadge Text="成功" Type="Success" />
    <controls:UnifiedStatusBadge Text="警告" Type="Warning" />
    <controls:UnifiedStatusBadge Text="危险" Type="Danger" />
    <controls:UnifiedStatusBadge Text="信息" Type="Info" />
    <controls:UnifiedStatusBadge Text="中性" Type="Neutral" />
</StackPanel>
```

**效果预览**:
- 🟢 成功 (绿色)
- 🟡 警告 (橙色)
- 🔴 危险 (红色)
- 🔵 信息 (蓝色)
- ⚪ 中性 (灰色)

## 最佳实践

### 1. 使用EnumDescriptionConverter自动获取文本

```csharp
// ✅ 推荐：使用Description特性
public enum PatientStatus
{
    [Description("正常")]
    Active,

    [Description("已删除")]
    Deleted
}
```

```xaml
<!-- ✅ 推荐：使用转换器 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />

<!-- ❌ 不推荐：硬编码文本 -->
<controls:UnifiedStatusBadge
    Text="正常"
    Type="Success" />
```

### 2. 状态-颜色映射规范

**推荐的状态类型映射**:

| 业务状态 | BadgeType | 示例 |
|---------|-----------|------|
| 正常、启用、已审核、已完成 | `Success` | Active, Approved, Completed |
| 草稿、待审核、处理中 | `Warning` | Draft, Pending, InProgress |
| 已删除、已拒绝、失败 | `Danger` | Deleted, Rejected, Failed |
| 角色、等级、标签 | `Info` | Role, Level, Tag |
| 未知、默认 | `Neutral` | Unknown, Default |

### 3. 在Table中居中对齐

```xaml
<DataGridTemplateColumn Header="状态" Width="100">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <!-- 使用容器居中 -->
            <Border HorizontalAlignment="Center">
                <controls:UnifiedStatusBadge
                    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
                    Type="Success" />
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### 4. 避免文本过长

```csharp
// ✅ 推荐：简洁的状态描述
[Description("正常")]
Active,

// ❌ 不推荐：过长的描述会导致徽章过宽
[Description("当前患者状态为正常并可以进行诊疗")]
Active,
```

## 样式定制

### 内部XAML结构

```xaml
<Border Background="{Binding BadgeBackground}"
        CornerRadius="8"
        Padding="8,4">
    <TextBlock Text="{Binding Text}"
               Foreground="{Binding BadgeForeground}"
               FontSize="12"
               FontWeight="Medium" />
</Border>
```

### 自定义颜色资源

在`UnifiedDesignSystem.xaml`中覆盖默认颜色：

```xaml
<SolidColorBrush x:Key="SuccessBrush" Color="#00C853" />  <!-- 更亮的绿色 -->
<SolidColorBrush x:Key="DangerBrush" Color="#D32F2F" />   <!-- 更深的红色 -->
```

## 常见问题

### Q: 徽章不显示？

**A**: 检查以下3点：
1. Text属性是否为空？
2. 是否正确添加了命名空间引用？
3. EnumDescriptionConverter是否正确定义在Resources中？

```xaml
<!-- 检查Text绑定 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />

<!-- 确保转换器已定义 -->
<UserControl.Resources>
    <infrastructure:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
</UserControl.Resources>
```

### Q: 颜色不正确？

**A**: 检查Type属性绑定：

```xaml
<!-- ✅ 正确 -->
<controls:UnifiedStatusBadge Type="Success" />

<!-- ❌ 错误：Type是枚举，不能绑定字符串 -->
<controls:UnifiedStatusBadge Type="{Binding StatusString}" />
```

### Q: 如何根据不同枚举值显示不同颜色？

**A**: 使用DataTrigger或自定义转换器（参见示例4）：

```xaml
<controls:UnifiedStatusBadge.Style>
    <Style TargetType="controls:UnifiedStatusBadge">
        <Style.Triggers>
            <DataTrigger Binding="{Binding Status}" Value="Active">
                <Setter Property="Type" Value="Success" />
            </DataTrigger>
            <DataTrigger Binding="{Binding Status}" Value="Deleted">
                <Setter Property="Type" Value="Danger" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</controls:UnifiedStatusBadge.Style>
```

### Q: 徽章宽度不一致？

**A**: 这是正常行为，徽章宽度根据文本内容自动调整。如需固定宽度：

```xaml
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success"
    MinWidth="60"
    HorizontalAlignment="Center" />
```

### Q: EnumDescriptionConverter找不到？

**A**: 确保命名空间引用正确：

```xaml
<UserControl
    xmlns:infrastructure="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure">

    <UserControl.Resources>
        <infrastructure:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
    </UserControl.Resources>
</UserControl>
```

## 实际应用示例

### 用户管理 - 角色徽章（Type=Info）

```xaml
<DataGridTemplateColumn Header="角色" Width="100">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <controls:UnifiedStatusBadge
                Text="{Binding Role, Converter={StaticResource EnumDescriptionConverter}}"
                Type="Info" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### 患者管理 - 状态徽章（Type=Success）

```xaml
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />
```

### 病案管理 - 动态颜色徽章

```xaml
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}">
    <controls:UnifiedStatusBadge.Style>
        <Style TargetType="controls:UnifiedStatusBadge">
            <Style.Triggers>
                <DataTrigger Binding="{Binding Status}" Value="Draft">
                    <Setter Property="Type" Value="Warning" />
                </DataTrigger>
                <DataTrigger Binding="{Binding Status}" Value="Completed">
                    <Setter Property="Type" Value="Success" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </controls:UnifiedStatusBadge.Style>
</controls:UnifiedStatusBadge>
```

## 相关资源

- [统一组件库总览](./unified-components.md)
- [UnifiedManagementTable组件](./unified-table.md) - 表格中使用StatusBadge
- [EnumDescriptionConverter文档](./../infrastructure/converters.md)

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
