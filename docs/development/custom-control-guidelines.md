# WPF自定义控件开发规范

> 本文档定义了LYBTZYZS项目中自定义控件的开发标准，重点关注DataContext处理和内容承载模式。

## 1. DataContext处理规范

### 1.1 核心原则

**自定义控件不应覆盖或污染DataContext继承链**

当控件承载用户内容（通过ContentPresenter）时，用户内容需要能够访问父级ViewModel的属性。如果控件污染了DataContext，用户内容的绑定将无法正常工作。

### 1.2 错误模式

```xml
<!-- 错误：Grid设置DataContext会污染所有子元素 -->
<UserControl x:Name="Root">
    <Grid DataContext="{Binding ElementName=Root}">
        <ContentPresenter Content="{Binding UserContent}"/>
    </Grid>
</UserControl>
```

```csharp
// 错误：代码后台设置DataContext会污染所有子元素
public MyControl()
{
    InitializeComponent();
    DataContext = this;  // 禁止！
}
```

### 1.3 正确模式

```xml
<!-- 正确：不设置Grid的DataContext，内部绑定使用ElementName -->
<UserControl x:Name="Root">
    <!-- 注意：不设置Grid的DataContext，以便内容能继承父级ViewModel -->
    <Grid>
        <Button Command="{Binding SomeCommand, ElementName=Root}"/>
        <ContentPresenter Content="{Binding UserContent, ElementName=Root}"/>
    </Grid>
</UserControl>
```

```csharp
// 正确：不设置DataContext
public MyControl() => InitializeComponent();
```

## 2. 控件分类与处理方式

### 2.1 内容承载控件

**定义**: 通过ContentPresenter或类似机制承载用户自定义内容的控件

**示例**:
- MasterDetailLayout (MasterContent, DetailContent, EmptyContent)
- DataGridToolbar (AdditionalContent)
- VirtualizedListView (HeaderContent)

**规范**:
- 禁止设置任何元素的DataContext
- 所有内部绑定必须使用`ElementName=Root`
- ContentPresenter的Content属性使用`{Binding PropertyName, ElementName=Root}`

### 2.2 自包含控件

**定义**: 不承载用户内容，只显示自身UI的控件

**示例**:
- EmptyState (只显示图标、标题、按钮)
- LoadingOverlay (只显示加载动画)
- SearchBox (只显示搜索框)

**规范**:
- 可以设置内部元素的DataContext为控件本身
- 推荐使用`ElementName=Root`以保持一致性
- 如需设置DataContext，优先使用`DataContext="{Binding ElementName=Root}"`而非代码后台

### 2.3 独立ViewModel控件

**定义**: 拥有独立ViewModel、不依赖外部DataContext的控件

**示例**:
- VirtualizedDataGrid (有自己的VirtualizedDataGridViewModel)
- GlobalStatusBar (自包含状态管理)

**规范**:
- 可以在代码后台设置`DataContext = new SomeViewModel()`
- 必须明确文档说明此控件不支持外部绑定
- 所有交互通过DependencyProperty暴露

## 3. 绑定模式速查表

| 场景 | 推荐方式 | 示例 |
|------|---------|------|
| 控件内部绑定控件属性 | ElementName | `{Binding Prop, ElementName=Root}` |
| ControlTemplate内部 | TemplatedParent | `{TemplateBinding Prop}` |
| Style Trigger中 | RelativeSource Self | `{Binding Prop, RelativeSource={RelativeSource Self}}` |
| 查找祖先控件 | RelativeSource AncestorType | `{Binding Prop, RelativeSource={RelativeSource AncestorType=...}}` |

## 4. 检查清单

开发自定义控件时，请确认以下事项:

- [ ] 控件是否承载用户内容（ContentPresenter）?
  - 是 → 禁止设置任何DataContext
  - 否 → 可以使用ElementName=Root方式
- [ ] 所有内部绑定是否使用了ElementName=Root?
- [ ] 代码后台是否避免了`DataContext = this`?
- [ ] 是否添加了注释说明DataContext处理方式?
- [ ] 是否在控件头部注释中说明了使用方式?

## 5. 现有控件合规状态

| 控件 | 状态 | 说明 |
|------|------|------|
| MasterDetailLayout | 已修复 | 不设置DataContext，使用ElementName |
| DataGridToolbar | 已修复 | 不设置DataContext，使用ElementName |
| VirtualizedListView | 已修复 | 移除DataContext=this，使用RelativeSource |
| DetailToolbar | 合规 | 不承载用户内容 |
| EmptyState | 合规 | 不承载用户内容 |
| SearchBox | 合规 | 不承载用户内容 |
| LoadingOverlay | 合规 | 使用ElementName绑定 |
| GlobalStatusBar | 合规 | 独立ViewModel控件 |
| VirtualizedDataGrid | 合规 | 独立ViewModel控件 |

## 6. 相关资源

- [WPF Data Binding Overview](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview)
- [ContentPresenter Class](https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.contentpresenter)
- Issue #2259 - 自定义控件DataContext处理规范化

---

*文档版本: 1.0*
*创建日期: 2025-12-16*
*维护者: LYBTZYZS开发团队*
