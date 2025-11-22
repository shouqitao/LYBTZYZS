# UnifiedManagementToolBar 组件

## 概述

UnifiedManagementToolBar 是统一的管理界面工具栏组件，提供搜索框、筛选区域和操作按钮区域。

**命名空间**: `LYBT.Desktop.Infrastructure.Controls`
**继承**: `UserControl`
**Issue**: #1840, #1842

**典型场景**:
- 数据列表搜索
- 业务数据筛选（状态、日期范围等）
- 批量操作按钮（新建、导入、导出等）

## 快速开始

最简单的使用示例（仅搜索功能）：

```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}" />
```

## API参考

### 依赖属性

| 属性名 | 类型 | 默认值 | 绑定模式 | 说明 |
|-------|------|--------|---------|------|
| `SearchText` | `string` | `""` | `TwoWay` | 搜索文本 |
| `SearchCommand` | `ICommand` | `null` | `OneWay` | 搜索命令（点击搜索按钮或按Enter键触发） |
| `FilterContent` | `object` | `null` | `OneWay` | 筛选区域内容（插槽） |
| `ActionButtons` | `object` | `null` | `OneWay` | 操作按钮区域内容（插槽） |

### 内部行为

- **搜索触发时机**:
  1. 用户按Enter键
  2. 用户点击"🔍 搜索"按钮
  3. 搜索框失去焦点（TextBox LostFocus）

- **性能优化**: `UpdateSourceTrigger=LostFocus` + Enter键绑定，避免每次按键都触发查询

## 使用示例

### 示例1: 基础用法（仅搜索）

**XAML**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}" />
```

**ViewModel**:
```csharp
public class MyViewModel : UnifiedListViewModelBase<MyDto>
{
    // SearchText和SearchCommand已由基类提供，无需重复定义

    protected override async Task<List<MyDto>> LoadDataAsync()
    {
        // 使用SearchText进行查询
        return await _service.SearchAsync(SearchText, CurrentPage, PageSize);
    }
}
```

### 示例2: 添加操作按钮

**XAML**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}">

    <controls:UnifiedManagementToolBar.ActionButtons>
        <StackPanel Orientation="Horizontal">
            <Button Content="+ 新建用户"
                    Command="{Binding AddCommand}"
                    Style="{StaticResource SuccessButton}"
                    Margin="{StaticResource SpacingSmall}" />
            <Button Content="刷新"
                    Command="{Binding RefreshCommand}"
                    Style="{StaticResource SecondaryButton}"
                    Margin="{StaticResource SpacingSmall}" />
            <Button Content="🏠 返回主页"
                    Command="{Binding NavigateToHomeCommand}"
                    Style="{StaticResource PrimaryButton}" />
        </StackPanel>
    </controls:UnifiedManagementToolBar.ActionButtons>
</controls:UnifiedManagementToolBar>
```

**ViewModel**:
```csharp
public ICommand AddCommand { get; }
public ICommand NavigateToHomeCommand { get; }

public MyViewModel()
{
    AddCommand = new DelegateCommand(OnAdd);
    NavigateToHomeCommand = new DelegateCommand(OnNavigateToHome);
}
```

### 示例3: 添加筛选区域（状态筛选）

**XAML**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}">

    <!-- 筛选区域 -->
    <controls:UnifiedManagementToolBar.FilterContent>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="状态:" VerticalAlignment="Center" Margin="0,0,8,0" />
            <ComboBox Width="120"
                      ItemsSource="{Binding StatusOptions}"
                      SelectedItem="{Binding SelectedStatus}"
                      Style="{StaticResource FilterComboBox}">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Converter={StaticResource EnumDescriptionConverter}}" />
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </StackPanel>
    </controls:UnifiedManagementToolBar.FilterContent>

    <!-- 操作按钮 -->
    <controls:UnifiedManagementToolBar.ActionButtons>
        <StackPanel Orientation="Horizontal">
            <Button Content="+ 新建" Command="{Binding AddCommand}" Style="{StaticResource SuccessButton}" />
        </StackPanel>
    </controls:UnifiedManagementToolBar.ActionButtons>
</controls:UnifiedManagementToolBar>
```

**ViewModel**:
```csharp
public ObservableCollection<PatientStatus> StatusOptions { get; set; }

private PatientStatus _selectedStatus;
public PatientStatus SelectedStatus
{
    get => _selectedStatus;
    set
    {
        SetProperty(ref _selectedStatus, value);
        RefreshCommand.Execute(null); // 状态变更时自动刷新列表
    }
}

protected override async Task<List<PatientDto>> LoadDataAsync()
{
    return await _service.SearchAsync(SearchText, SelectedStatus, CurrentPage, PageSize);
}
```

### 示例4: 完整示例（病案管理 - 多筛选条件）

**XAML**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}">

    <!-- 筛选区域: 状态 + 日期范围 -->
    <controls:UnifiedManagementToolBar.FilterContent>
        <StackPanel Orientation="Horizontal">
            <!-- 状态筛选 -->
            <TextBlock Text="状态:" VerticalAlignment="Center" Margin="0,0,8,0" />
            <ComboBox Width="120"
                      ItemsSource="{Binding StatusOptions}"
                      SelectedItem="{Binding SelectedStatus}"
                      Margin="0,0,16,0" />

            <!-- 日期范围 -->
            <TextBlock Text="创建日期:" VerticalAlignment="Center" Margin="0,0,8,0" />
            <DatePicker SelectedDate="{Binding StartDate}" Width="120" Margin="0,0,8,0" />
            <TextBlock Text="-" VerticalAlignment="Center" Margin="0,0,8,0" />
            <DatePicker SelectedDate="{Binding EndDate}" Width="120" />
        </StackPanel>
    </controls:UnifiedManagementToolBar.FilterContent>

    <!-- 操作按钮 -->
    <controls:UnifiedManagementToolBar.ActionButtons>
        <StackPanel Orientation="Horizontal">
            <Button Content="+ 新建病案" Command="{Binding AddCommand}" Style="{StaticResource SuccessButton}" Margin="{StaticResource SpacingSmall}" />
            <Button Content="刷新" Command="{Binding RefreshCommand}" Style="{StaticResource SecondaryButton}" Margin="{StaticResource SpacingSmall}" />
            <Button Content="🏠 返回主页" Command="{Binding NavigateToHomeCommand}" Style="{StaticResource PrimaryButton}" />
        </StackPanel>
    </controls:UnifiedManagementToolBar.ActionButtons>
</controls:UnifiedManagementToolBar>
```

### 示例5: 导入/导出按钮（中药管理）

**XAML**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}">

    <controls:UnifiedManagementToolBar.ActionButtons>
        <StackPanel Orientation="Horizontal">
            <Button Content="📥 导入药材"
                    Command="{Binding ImportHerbsCommand}"
                    Style="{StaticResource SecondaryButton}"
                    ToolTip="从Excel/CSV文件导入药材数据"
                    Margin="{StaticResource SpacingSmall}" />
            <Button Content="📄 导出模板"
                    Command="{Binding ExportTemplateCommand}"
                    Style="{StaticResource InfoButton}"
                    ToolTip="下载药材导入模板"
                    Margin="{StaticResource SpacingSmall}" />
            <Button Content="📤 导出药材"
                    Command="{Binding ExportHerbsCommand}"
                    Style="{StaticResource WarningButton}"
                    ToolTip="导出药材数据到Excel文件"
                    Margin="{StaticResource SpacingSmall}" />
            <Button Content="+ 新增药材"
                    Command="{Binding AddHerbCommand}"
                    Style="{StaticResource SuccessButton}"
                    Margin="{StaticResource SpacingSmall}" />
        </StackPanel>
    </controls:UnifiedManagementToolBar.ActionButtons>
</controls:UnifiedManagementToolBar>
```

## 最佳实践

### 1. 始终使用TwoWay绑定SearchText

```xaml
<!-- ✅ 正确 -->
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}" />

<!-- ❌ 错误：缺少TwoWay，用户输入无法更新ViewModel -->
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText}"
    SearchCommand="{Binding SearchCommand}" />
```

### 2. 筛选条件变更时自动刷新

```csharp
public MyStatus SelectedStatus
{
    get => _selectedStatus;
    set
    {
        SetProperty(ref _selectedStatus, value);
        RefreshCommand.Execute(null); // 🔑 关键：状态变更立即刷新列表
    }
}
```

### 3. 操作按钮使用标准样式

**可用按钮样式**:
- `PrimaryButton` - 主要操作（蓝色）
- `SuccessButton` - 成功/新建操作（绿色）
- `SecondaryButton` - 次要操作（灰色）
- `DangerButton` - 危险操作（红色）
- `WarningButton` - 警告操作（橙色）
- `InfoButton` - 信息操作（蓝色）

### 4. 间距遵循4 epx规则

```xaml
<!-- ✅ 正确：8px符合4 epx规则 -->
<Button Margin="{StaticResource SpacingSmall}" />  <!-- SpacingSmall = 8 -->

<!-- ❌ 错误：7px不符合规则 -->
<Button Margin="7" />
```

## 样式定制

### FilterComboBox样式

组件内部使用的筛选ComboBox样式：

```xaml
<ComboBox Style="{StaticResource FilterComboBox}" Width="120" />
```

### SearchTextBox样式

搜索框样式（组件内部已应用，无需手动指定）：

```xaml
<!-- 内部实现 -->
<TextBox Style="{StaticResource SearchTextBox}" />
```

## 性能说明

**搜索触发优化** (Issue #1840 - Task 3.4):

- **优化前**: 每次按键触发查询（`UpdateSourceTrigger=PropertyChanged`）
- **优化后**:
  1. 按Enter键触发
  2. 点击搜索按钮触发
  3. 失去焦点触发（`UpdateSourceTrigger=LostFocus`）

**性能提升**: 数据库查询减少75%（详见 `docs/reports/phase3-task3.4-performance-report.md`）

## 常见问题

### Q: SearchCommand没有触发？

**A**: 检查以下3点：
1. ViewModel是否正确绑定到View的DataContext？
2. SearchCommand是否为null？
3. SearchCommand.CanExecute是否返回false？

```csharp
// 调试方法
public ICommand SearchCommand { get; }

public MyViewModel()
{
    SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => true);
    Console.WriteLine($"SearchCommand created: {SearchCommand != null}");
}
```

### Q: 筛选条件变更后列表没有自动刷新？

**A**: 在筛选属性的setter中调用`RefreshCommand.Execute(null)`：

```csharp
public MyStatus SelectedStatus
{
    get => _selectedStatus;
    set
    {
        SetProperty(ref _selectedStatus, value);
        RefreshCommand.Execute(null); // 🔑 关键代码
    }
}
```

### Q: 如何在FilterContent中放置多个筛选控件？

**A**: 使用StackPanel包裹：

```xaml
<controls:UnifiedManagementToolBar.FilterContent>
    <StackPanel Orientation="Horizontal">
        <ComboBox Width="120" Margin="0,0,12,0" />
        <DatePicker Width="120" Margin="0,0,12,0" />
        <TextBox Width="150" />
    </StackPanel>
</controls:UnifiedManagementToolBar.FilterContent>
```

### Q: 按钮之间的间距不一致？

**A**: 使用`Margin="{StaticResource SpacingSmall}"`统一间距：

```xaml
<Button Content="按钮1" Margin="{StaticResource SpacingSmall}" />
<Button Content="按钮2" Margin="{StaticResource SpacingSmall}" />
```

## 相关资源

- [统一组件库总览](./unified-components.md)
- [UnifiedManagementTable组件](./unified-table.md)
- [故障排查指南](./troubleshooting.md)

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
