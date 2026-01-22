# XAML资源引用规范指南

**创建日期**: 2026-01-21
**OpenSpec**: cleanup-control-resource-merging

---

## 核心规则

### 1. 资源引用方式

| 资源类型 | 引用方式 | 原因 |
|----------|----------|------|
| **Style** | `DynamicResource` | 支持运行时主题切换 |
| **Style.BasedOn** | `StaticResource` | **必须！** BasedOn不是DependencyProperty |
| **Converter** | `StaticResource` | **必须！** Binding.Converter不是DependencyProperty |
| **Brush/Color** | `DynamicResource` | 支持主题切换 |
| **控件内部资源** | `StaticResource` | 性能优化，仅控件内部使用 |

### 2. 关键技术限制

**Converter必须使用StaticResource**：

```xml
<!-- ✅ 正确 -->
<TextBlock Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"/>

<!-- ❌ 错误 - 运行时XamlParseException -->
<TextBlock Text="{Binding Status, Converter={DynamicResource EnumDescriptionConverter}}"/>
```

**原因**：`Binding.Converter`属性不是`DependencyProperty`，只有`DependencyProperty`才能使用`DynamicResource`。

**Style.BasedOn必须使用StaticResource**：

```xml
<!-- 正确 -->
<Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{StaticResource SecondaryButtonStyle}">

<!-- 错误 - 运行时XamlParseException -->
<Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{DynamicResource SecondaryButtonStyle}">
```

**原因**：`Style.BasedOn`属性不是`DependencyProperty`。

### 3. Style可以使用DynamicResource

```xml
<!-- ✅ 正确 - Style是DependencyProperty -->
<Button Style="{DynamicResource PrimaryButton}"/>
<TextBlock Style="{DynamicResource FormLabelStyle}"/>
```

---

## 资源字典结构

### App级资源加载顺序（Shell/App.xaml）

```
1. Theme.Light.xaml     → Design Tokens (颜色/排版/间距)
2. UnifiedComponents.xaml → 通用组件样式
3. Typography.xaml      → 字体系统
4. Controls.xaml        → 控件样式覆盖
5. DialogStyles.xaml    → 对话框样式
6. Converters.xaml      → 值转换器
```

### 控件级资源

**规则**：控件内部**禁止**合并`UnifiedComponents.xaml`，避免资源重复加载。

```xml
<!-- ❌ 禁止在控件级别 -->
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://...UnifiedComponents.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>

<!-- ✅ 正确 - 使用DynamicResource引用App级资源 -->
<UserControl>
    <Button Style="{DynamicResource PrimaryButton}"/>
</UserControl>
```

---

## 控件绑定模式

### ElementName绑定

**适用场景**：同一NameScope内的控件间绑定

```xml
<!-- ✅ 正确 - UserControl内部绑定自身的DependencyProperty -->
<UserControl x:Name="Root">
    <TextBlock Text="{Binding MyProperty, ElementName=Root}"/>
</UserControl>
```

**危险场景**：ContentPresenter创建独立NameScope

```xml
<!-- ❌ 危险 - ContentPresenter内的ElementName会失败 -->
<controls:MasterDetailLayout>
    <controls:MasterDetailLayout.DetailContent>
        <!-- 这里是独立NameScope，ElementName=Root找不到 -->
        <TextBlock Text="{Binding X, ElementName=Root}"/>
    </controls:MasterDetailLayout.DetailContent>
</controls:MasterDetailLayout>
```

**解决方案**：在ContentPresenter内部使用DataContext绑定

```xml
<!-- ✅ 正确 - 使用DataContext绑定 -->
<controls:MasterDetailLayout.DetailContent>
    <TextBlock Text="{Binding ViewModel.Property}"/>
</controls:MasterDetailLayout.DetailContent>
```

---

## 常见错误及修复

### 错误1：Converter使用DynamicResource

**症状**：
```
System.Windows.Markup.XamlParseException
不能在"Binding"类型的"Converter"属性上设置"DynamicResourceExtension"
```

**修复**：将`Converter={DynamicResource`改为`Converter={StaticResource`

### 错误2：DependencyProperty.UnsetValue

**症状**：
```
System.Windows.Data Error: Cannot find resource named 'XXX'
```

**原因**：控件级别未合并资源字典，且使用了StaticResource

**修复**：改用DynamicResource（仅限非Converter属性）

### 错误3：ElementName绑定失败

**症状**：
```
System.Windows.Data Error: Cannot find element named 'Root'
```

**原因**：绑定跨越了ContentPresenter的NameScope边界

**修复**：改用DataContext绑定或RelativeSource

---

## 检查清单

添加新控件时：

- [ ] 不在控件级别合并UnifiedComponents.xaml
- [ ] Style使用DynamicResource
- [ ] Converter使用StaticResource
- [ ] 如果在ContentPresenter内使用，避免ElementName绑定
- [ ] 添加OpenSpec注释说明资源加载策略

---

## 参考

- `Infrastructure/CLAUDE.md` - XAML资源加载顺序规则
- `openspec/changes/cleanup-control-resource-merging/` - 资源清理提案
