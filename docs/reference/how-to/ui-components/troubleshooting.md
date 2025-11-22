# 统一组件库故障排查指南

## 概述

本文档提供统一UI组件库(Epic #1840)常见问题的排查步骤和解决方案。

**适用组件**:
- UnifiedManagementToolBar
- UnifiedManagementTable
- UnifiedStatusBadge
- UnifiedPaginationBar

**排查原则**:
1. **检查绑定路径** - 90%的问题来自绑定错误
2. **验证命名空间** - xmlns引用是否正确
3. **确认数据类型** - 属性类型匹配
4. **检查双向绑定** - Mode=TwoWay是否遗漏

---

## 1. 组件不显示

### 1.1 组件完全不显示

**症状**: XAML中添加了组件,运行后界面空白或不显示组件。

**排查步骤**:

**步骤1: 检查命名空间引用**
```xaml
<!-- ✅ 正确 -->
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">

<!-- ❌ 错误1: assembly名称错误 -->
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop">

<!-- ❌ 错误2: 命名空间拼写错误 -->
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Control;assembly=LYBT.Desktop.Infrastructure">
```

**步骤2: 检查Grid行高定义**
```xaml
<!-- ✅ 正确: 工具栏和分页栏设置为Auto,表格设置为* -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto" />  <!-- 工具栏 -->
    <RowDefinition Height="*" />     <!-- 表格(占据剩余空间) -->
    <RowDefinition Height="Auto" />  <!-- 分页栏 -->
</Grid.RowDefinitions>

<!-- ❌ 错误: 所有行都是Auto,表格可能被压缩 -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="Auto" />
    <RowDefinition Height="Auto" />
</Grid.RowDefinitions>
```

**步骤3: 检查Output窗口**
- Visual Studio → 输出 → 显示输出来源: "调试"
- 查找 `System.Windows.Data Error` 或 `BindingExpression` 错误

**步骤4: 临时简化组件**
```xaml
<!-- 临时删除所有属性绑定,测试组件是否能显示 -->
<controls:UnifiedManagementToolBar />
```

如果简化后能显示,问题在于绑定。继续排查绑定问题。

### 1.2 UnifiedStatusBadge不显示

**症状**: DataGrid中的UnifiedStatusBadge列空白。

**排查步骤**:

**步骤1: 检查Text属性绑定**
```xaml
<!-- ✅ 正确 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />

<!-- ❌ 错误: Text为空,徽章不显示 -->
<controls:UnifiedStatusBadge Type="Success" />
```

**步骤2: 检查EnumDescriptionConverter是否定义**
```xaml
<UserControl.Resources>
    <infrastructure:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
</UserControl.Resources>
```

**步骤3: 检查xmlns:infrastructure引用**
```xaml
<UserControl xmlns:infrastructure="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure">
```

**步骤4: 验证枚举Description特性**
```csharp
using System.ComponentModel;

public enum PatientStatus
{
    [Description("正常")] // ✅ 必须有Description特性
    Active,

    [Description("已删除")]
    Deleted
}
```

---

## 2. 绑定问题

### 2.1 搜索框输入无法更新ViewModel

**症状**: 在UnifiedManagementToolBar搜索框输入文字,ViewModel的SearchText属性没有更新。

**原因**: 缺少`Mode=TwoWay`绑定模式。

**解决方案**:
```xaml
<!-- ✅ 正确: 使用TwoWay绑定 -->
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}" />

<!-- ❌ 错误: 缺少TwoWay,默认为OneWay,用户输入无法传回ViewModel -->
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText}"
    SearchCommand="{Binding SearchCommand}" />
```

### 2.2 表格选中项无法传回ViewModel

**症状**: 点击UnifiedManagementTable行,ViewModel的SelectedItem属性没有更新。

**原因**: 缺少`Mode=TwoWay`绑定模式。

**解决方案**:
```xaml
<!-- ✅ 正确 -->
<controls:UnifiedManagementTable
    SelectedItem="{Binding SelectedItem, Mode=TwoWay}" />

<!-- ❌ 错误 -->
<controls:UnifiedManagementTable
    SelectedItem="{Binding SelectedItem}" />
```

### 2.3 分页页码改变无法更新ViewModel

**症状**: 点击UnifiedPaginationBar的上一页/下一页按钮,ViewModel的CurrentPage属性没有更新。

**原因**: 缺少`Mode=TwoWay`绑定模式。

**解决方案**:
```xaml
<!-- ✅ 正确 -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    PageSize="{Binding PageSize, Mode=TwoWay}" />

<!-- ❌ 错误 -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage}"
    PageSize="{Binding PageSize}" />
```

### 2.4 操作列按钮Command不触发

**症状**: DataGrid操作列中的按钮点击无响应。

**原因**: DataContext是行数据对象,而非UserControl的ViewModel。

**解决方案**: 使用RelativeSource向上查找UserControl的DataContext:
```xaml
<!-- ✅ 正确: 使用RelativeSource -->
<Button Content="编辑"
        Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}" />

<!-- ❌ 错误: DataContext是行数据,没有EditCommand -->
<Button Content="编辑"
        Command="{Binding EditCommand}"
        CommandParameter="{Binding}" />
```

**为何需要RelativeSource?**
```
UserControl (DataContext = ViewModel)
  └─ DataGrid
      └─ DataGridRow (DataContext = 行数据对象MyDto)
          └─ Button (DataContext继承自DataGridRow)
```

按钮的DataContext是`MyDto`,而`EditCommand`在ViewModel中,需要用RelativeSource向上查找。

---

## 3. 命令问题

### 3.1 SearchCommand不触发

**症状**: 在UnifiedManagementToolBar搜索框按Enter或点击搜索按钮,SearchCommand没有执行。

**排查步骤**:

**步骤1: 检查Command是否为null**
```csharp
public class MyViewModel : UnifiedListViewModelBase<MyDto>
{
    public MyViewModel()
    {
        Console.WriteLine($"SearchCommand is null? {SearchCommand == null}"); // 应输出False
    }
}
```

如果继承了`UnifiedListViewModelBase`,SearchCommand自动提供,不应为null。

**步骤2: 检查CanExecute**
```csharp
// UnifiedListViewModelBase中SearchCommand的实现
SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => true);
```

**步骤3: 检查SearchAsync是否被覆盖**
```csharp
protected override async Task SearchAsync()
{
    CurrentPage = 1; // 重置到第1页
    await RefreshAsync(); // 调用RefreshAsync加载数据
}
```

**步骤4: 检查绑定路径**
```xaml
<!-- ✅ 正确 -->
<controls:UnifiedManagementToolBar
    SearchCommand="{Binding SearchCommand}" />

<!-- ❌ 错误: 绑定路径拼写错误 -->
<controls:UnifiedManagementToolBar
    SearchCommand="{Binding SearchCmd}" />
```

### 3.2 分页Command不触发

**症状**: 点击分页按钮无反应。

**排查步骤**:

**步骤1: 确认继承UnifiedListViewModelBase**
```csharp
// ✅ 正确: 基类自动提供所有分页命令
public class MyViewModel : UnifiedListViewModelBase<MyDto> { }

// ❌ 错误: 需手动实现所有命令
public class MyViewModel : BindableBase { }
```

**步骤2: 检查命令绑定**
```xaml
<controls:UnifiedPaginationBar
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}" />
```

**步骤3: 检查按钮禁用状态**
- 首页/上一页: CurrentPage == 1时禁用
- 下一页/末页: CurrentPage == TotalPages时禁用

如果按钮是灰色(禁用状态),是正常行为。

### 3.3 自定义Command不触发

**症状**: 工具栏中的自定义操作按钮(如"新建"、"刷新")点击无反应。

**排查步骤**:

**步骤1: 检查Command定义**
```csharp
public ICommand AddCommand { get; }

public MyViewModel()
{
    AddCommand = new DelegateCommand(OnAdd); // ✅ 必须在构造函数中初始化
}

private void OnAdd()
{
    // 实现逻辑
}
```

**步骤2: 检查Command绑定**
```xaml
<!-- ✅ 正确 -->
<Button Content="+ 新建" Command="{Binding AddCommand}" />

<!-- ❌ 错误: 绑定路径错误 -->
<Button Content="+ 新建" Command="{Binding Add}" />
```

**步骤3: 检查CanExecute**
```csharp
// 如果Command始终可执行
AddCommand = new DelegateCommand(OnAdd, () => true);

// 如果Command有条件限制
AddCommand = new DelegateCommand(OnAdd, CanAdd);

private bool CanAdd()
{
    return SelectedItem != null; // 例如: 需选中项才能执行
}
```

---

## 4. 数据显示问题

### 4.1 表格数据不显示

**症状**: UnifiedManagementTable空白,无数据显示。

**排查步骤**:

**步骤1: 检查ItemsSource绑定**
```xaml
<!-- ✅ 正确 -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}" />

<!-- ❌ 错误: 绑定路径错误 -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding ItemList}" />
```

**步骤2: 检查Items集合是否有数据**
```csharp
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    Console.WriteLine($"Loaded {result.Items.Count} items"); // 检查数据条数
    TotalCount = result.TotalCount;
    return result.Items;
}
```

**步骤3: 检查列Binding路径**
```xaml
<!-- ✅ 正确: 属性名匹配MyDto类 -->
<DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />

<!-- ❌ 错误: MyDto类没有FullName属性 -->
<DataGridTextColumn Header="名称" Binding="{Binding FullName}" Width="150" />
```

**步骤4: 验证MyDto属性**
```csharp
public class MyDto
{
    public string Name { get; set; } // ✅ 必须是公共属性
    private string Description { get; set; } // ❌ 私有属性无法绑定
}
```

**步骤5: 检查是否调用了RefreshAsync**
```csharp
public MyViewModel()
{
    // ✅ 正确: 构造函数中调用RefreshAsync加载初始数据
    RefreshCommand.Execute(null);
}
```

### 4.2 空状态提示不显示

**症状**: 数据为空时,UnifiedManagementTable没有显示"暂无数据"提示。

**原因**: `ShowEmptyState`属性被设置为false。

**解决方案**:
```xaml
<!-- ✅ 正确: ShowEmptyState=true(默认值) -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    ShowEmptyState="True"
    EmptyStateText="暂无数据" />

<!-- ❌ 错误: 禁用了空状态提示 -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    ShowEmptyState="False" />
```

### 4.3 枚举值显示为数字而非文本

**症状**: UnifiedStatusBadge或DataGridTextColumn显示"0"、"1"而非"正常"、"已删除"。

**原因**: 缺少EnumDescriptionConverter转换器。

**解决方案**:

**步骤1: 定义转换器**
```xaml
<UserControl.Resources>
    <infrastructure:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
</UserControl.Resources>
```

**步骤2: 使用转换器**
```xaml
<!-- ✅ 正确: 使用转换器 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />

<!-- ❌ 错误: 直接绑定枚举,显示数字 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status}"
    Type="Success" />
```

**步骤3: 枚举添加Description特性**
```csharp
using System.ComponentModel;

public enum PatientStatus
{
    [Description("正常")] // ✅ 必须有Description
    Active,

    [Description("已删除")]
    Deleted
}
```

---

## 5. 样式问题

### 5.1 组件样式不生效

**症状**: 组件显示但样式与预期不符(颜色、字体、间距等)。

**排查步骤**:

**步骤1: 检查资源字典引用**
```xaml
<!-- App.xaml中必须引用 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/UnifiedDesignSystem.xaml" />
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/UnifiedComponents.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**步骤2: 检查StaticResource引用**
```xaml
<!-- ✅ 正确: 使用StaticResource -->
<Button Style="{StaticResource SuccessButton}" />

<!-- ❌ 错误: 资源名称拼写错误 -->
<Button Style="{StaticResource GreenButton}" />
```

**步骤3: 清理并重新生成**
```bash
dotnet clean LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

### 5.2 UnifiedStatusBadge颜色不正确

**症状**: 所有徽章都显示为灰色(Neutral)。

**原因**: Type属性未正确设置。

**解决方案**:
```xaml
<!-- ✅ 正确: 显式设置Type -->
<controls:UnifiedStatusBadge
    Text="正常"
    Type="Success" />

<!-- ❌ 错误: 缺少Type,默认为Neutral(灰色) -->
<controls:UnifiedStatusBadge
    Text="正常" />
```

**动态设置Type**:
```xaml
<!-- 使用DataTrigger根据状态动态设置颜色 -->
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}">
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
</controls:UnifiedStatusBadge>
```

---

## 6. 性能问题

### 6.1 表格滚动卡顿

**症状**: UnifiedManagementTable滚动时卡顿,尤其是数据量>1000条时。

**排查步骤**:

**步骤1: 确认虚拟化已启用**

UnifiedManagementTable内部已启用虚拟化,检查UnifiedComponents.xaml:
```xaml
<Setter Property="VirtualizingPanel.IsVirtualizing" Value="True"/>
<Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>
<Setter Property="EnableRowVirtualization" Value="True"/>
```

**步骤2: 检查CellTemplate复杂度**
```xaml
<!-- ❌ 问题: CellTemplate中使用复杂控件 -->
<DataGridTemplateColumn Header="图片">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Image Source="{Binding ImageUrl}" Width="100" Height="100" />
            <controls:ComplexUserControl Data="{Binding}" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<!-- ✅ 解决: 简化CellTemplate或使用图片缓存 -->
<DataGridTemplateColumn Header="图片">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Image Source="{Binding ImageUrl}" Width="50" Height="50" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**步骤3: 考虑分页**

如果数据量>10,000条,建议实现分页:
```csharp
protected override async Task<List<MyDto>> LoadDataAsync()
{
    // ✅ 推荐: 每次只加载20条(当前页数据)
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    TotalCount = result.TotalCount;
    return result.Items;
}
```

### 6.2 搜索时频繁查询数据库

**症状**: 输入搜索文字时,每按一个键就查询一次数据库。

**原因**: 已在Phase 3 Task 3.4修复,确保使用最新版本的UnifiedManagementToolBar。

**验证方案**: 检查UnifiedManagementToolBar.xaml:
```xaml
<!-- ✅ 正确: UpdateSourceTrigger=LostFocus + Enter键绑定 -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=LostFocus}">
    <TextBox.InputBindings>
        <KeyBinding Key="Enter" Command="{Binding SearchCommand}" />
    </TextBox.InputBindings>
</TextBox>
```

**性能提升**: 数据库查询减少75%(详见phase3-task3.4-performance-report.md)

---

## 7. 编译问题

### 7.1 找不到命名空间或类型

**症状**: 编译错误 `The type or namespace name 'UnifiedManagementToolBar' could not be found`

**原因**: 缺少项目引用或assembly引用错误。

**解决方案**:

**步骤1: 检查项目引用**

在使用组件的项目(如LYBT.Desktop.Users)的`.csproj`文件中:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\Core\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
</ItemGroup>
```

**步骤2: 检查xmlns引用**
```xaml
<!-- ✅ 正确 -->
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">

<!-- ❌ 错误: assembly名称错误 -->
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=Infrastructure">
```

**步骤3: 清理并重新生成**
```bash
dotnet clean LYBT.All.sln
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

### 7.2 资源字典未找到

**症状**: 运行时异常 `Cannot find resource named 'SuccessButton'`

**原因**: 资源字典未正确引用。

**解决方案**:

**步骤1: 检查App.xaml**
```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/UnifiedDesignSystem.xaml" />
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/UnifiedComponents.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**步骤2: 检查资源文件生成操作**

在Visual Studio中右键UnifiedDesignSystem.xaml和UnifiedComponents.xaml:
- 属性 → 生成操作 → 必须是"Page"或"Resource"

---

## 8. 调试技巧

### 8.1 启用WPF绑定调试

**方法1: Output窗口查看绑定错误**
- Visual Studio → 输出 → 显示输出来源: "调试"
- 查找 `System.Windows.Data Error` 或 `BindingExpression path error`

**方法2: 使用PresentationTraceSources**
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    xmlns:diagnostics="clr-namespace:System.Diagnostics;assembly=WindowsBase"
    diagnostics:PresentationTraceSources.TraceLevel="High" />
```

### 8.2 临时禁用绑定验证

测试组件是否能显示:
```xaml
<!-- 临时硬编码值,排除绑定问题 -->
<controls:UnifiedStatusBadge
    Text="测试文本"
    Type="Success" />
```

如果硬编码能显示,问题在于绑定路径或ViewModel属性。

### 8.3 使用Snoop工具

**Snoop**: WPF可视化调试工具,可实时查看UI树和绑定状态。

**下载**: https://github.com/snoopwpf/snoop

**使用**:
1. 启动应用程序
2. 启动Snoop → 选择正在运行的进程
3. 查看UI树 → 选中组件 → 检查DataContext和属性值

---

## 9. 快速检查清单

### 新建管理界面检查清单

使用统一组件创建新界面时,按此清单逐项检查:

**XAML检查**:
- [ ] 添加xmlns:controls引用
- [ ] Grid设置3行(Auto, *, Auto)
- [ ] UnifiedManagementToolBar.SearchText使用TwoWay绑定
- [ ] UnifiedManagementTable.SelectedItem使用TwoWay绑定
- [ ] UnifiedPaginationBar.CurrentPage和PageSize使用TwoWay绑定
- [ ] DataGridTextColumn的Binding路径与DTO属性匹配
- [ ] EnumDescriptionConverter已定义在Resources中
- [ ] 操作列Command使用RelativeSource绑定

**ViewModel检查**:
- [ ] 继承UnifiedListViewModelBase<T>
- [ ] 实现LoadDataAsync方法
- [ ] LoadDataAsync中设置TotalCount
- [ ] 构造函数中调用RefreshCommand.Execute(null)加载初始数据
- [ ] 自定义Command已在构造函数中初始化

**项目引用检查**:
- [ ] 项目引用LYBT.Desktop.Infrastructure
- [ ] App.xaml中引用UnifiedDesignSystem.xaml和UnifiedComponents.xaml

**编译验证**:
- [ ] 编译成功(0 errors)
- [ ] 运行后界面正常显示
- [ ] 搜索功能正常
- [ ] 分页功能正常
- [ ] 数据加载正常

---

## 10. 获取帮助

### 10.1 文档资源

- [统一组件库总览](./unified-components.md)
- [UnifiedManagementToolBar文档](./unified-toolbar.md)
- [UnifiedManagementTable文档](./unified-table.md)
- [UnifiedStatusBadge文档](./unified-statusbadge.md)
- [UnifiedPaginationBar文档](./unified-paginationbar.md)

### 10.2 验收报告

- [Phase 2验收报告](./../../reports/phase2-acceptance-report.md) - 5个界面迁移案例
- [Phase 3性能优化报告](./../../reports/phase3-task3.4-performance-report.md) - 性能优化详情

### 10.3 联系方式

**Issue反馈**: GitHub Issue #1840 (Desktop端管理界面UI统一化)

**常见问题汇总**: 本文档会持续更新,欢迎提交新问题。

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
**维护团队**: Desktop Infrastructure Team
