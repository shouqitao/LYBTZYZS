# UnifiedManagementTable 组件

## 概述

UnifiedManagementTable 是统一的数据表格组件，基于WPF DataGrid封装，提供虚拟化滚动、空状态显示等功能。

**命名空间**: `LYBT.Desktop.Infrastructure.Controls`
**继承**: `UserControl`
**内部控件**: `DataGrid`
**Issue**: #1840, #1843

**典型场景**:
- 数据列表展示
- 多列数据表格
- 支持选中、排序、虚拟化滚动

## 快速开始

最简单的使用示例：

```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem, Mode=TwoWay}">

    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />
        <DataGridTextColumn Header="状态" Binding="{Binding Status}" Width="100" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

## API参考

### 依赖属性

| 属性名 | 类型 | 默认值 | 绑定模式 | 说明 |
|-------|------|--------|---------|------|
| `ItemsSource` | `IEnumerable` | `null` | `OneWay` | 数据源 |
| `SelectedItem` | `object` | `null` | `TwoWay` | 选中项 |
| `ShowEmptyState` | `bool` | `true` | `OneWay` | 是否显示空状态提示 |
| `EmptyStateText` | `string` | `"暂无数据"` | `OneWay` | 空状态提示文本 |

### 公共属性

| 属性名 | 类型 | 说明 |
|-------|------|------|
| `Columns` | `ObservableCollection<DataGridColumn>` | 获取DataGrid的列集合（只读） |

### 内部特性

- **虚拟化滚动**: 已启用 `VirtualizingPanel.IsVirtualizing="True"`
- **虚拟化模式**: `Recycling`（最优性能）
- **行虚拟化**: 已启用
- **空状态**: 无数据时自动显示提示文本

**预期性能**:
- 1,000行数据：流畅滚动，内存占用 <20MB
- 10,000行数据：流畅滚动，内存占用 <50MB

## 使用示例

### 示例1: 基础用法（文本列）

**XAML**:
```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Users}"
    SelectedItem="{Binding SelectedUser, Mode=TwoWay}"
    EmptyStateText="暂无用户数据">

    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="用户名" Binding="{Binding UserName}" Width="120" />
        <DataGridTextColumn Header="姓名" Binding="{Binding RealName}" Width="100" />
        <DataGridTextColumn Header="角色" Binding="{Binding Role}" Width="80" />
        <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd'}" Width="120" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

**ViewModel**:
```csharp
public ObservableCollection<UserDto> Users { get; set; }

private UserDto _selectedUser;
public UserDto SelectedUser
{
    get => _selectedUser;
    set => SetProperty(ref _selectedUser, value);
}
```

### 示例2: 使用UnifiedStatusBadge显示状态

**XAML**:
```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Patients}"
    SelectedItem="{Binding SelectedPatient, Mode=TwoWay}">

    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="100" />
        <DataGridTextColumn Header="性别" Binding="{Binding Gender, Converter={StaticResource EnumDescriptionConverter}}" Width="60" />

        <!-- 状态列：使用UnifiedStatusBadge -->
        <DataGridTemplateColumn Header="状态" Width="100">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <controls:UnifiedStatusBadge
                        Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
                        Type="Success" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>

        <DataGridTextColumn Header="电话" Binding="{Binding Phone}" Width="120" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

### 示例3: 添加操作列（查看/编辑/删除按钮）

**XAML**:
```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem, Mode=TwoWay}">

    <controls:UnifiedManagementTable.Columns>
        <!-- 数据列 -->
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />
        <DataGridTextColumn Header="描述" Binding="{Binding Description}" Width="*" />

        <!-- 操作列 -->
        <DataGridTemplateColumn Header="操作" Width="200">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                        <Button Content="查看"
                                Style="{StaticResource InfoButton}"
                                Padding="8,4"
                                FontSize="12"
                                Margin="2"
                                Command="{Binding DataContext.ViewCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                CommandParameter="{Binding}" />
                        <Button Content="编辑"
                                Style="{StaticResource SuccessButton}"
                                Padding="8,4"
                                FontSize="12"
                                Margin="2"
                                Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                CommandParameter="{Binding}" />
                        <Button Content="删除"
                                Style="{StaticResource DangerButton}"
                                Padding="8,4"
                                FontSize="12"
                                Margin="2"
                                Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                CommandParameter="{Binding}" />
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

**ViewModel**:
```csharp
public ICommand ViewCommand { get; }
public ICommand EditCommand { get; }
public ICommand DeleteCommand { get; }

public MyViewModel()
{
    ViewCommand = new DelegateCommand<MyDto>(OnView);
    EditCommand = new DelegateCommand<MyDto>(OnEdit);
    DeleteCommand = new DelegateCommand<MyDto>(OnDelete);
}

private void OnView(MyDto item)
{
    // 查看详情逻辑
}

private void OnEdit(MyDto item)
{
    // 编辑逻辑
}

private async void OnDelete(MyDto item)
{
    var result = await _dialogService.ShowConfirmAsync("确认删除", $"确定删除 {item.Name} 吗？");
    if (result)
    {
        await _service.DeleteAsync(item.Id);
        await RefreshAsync();
    }
}
```

### 示例4: 格式化数据显示

**XAML**:
```xaml
<controls:UnifiedManagementTable ItemsSource="{Binding Prescriptions}">
    <controls:UnifiedManagementTable.Columns>
        <!-- 日期格式化 -->
        <DataGridTextColumn Header="创建时间"
                            Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd HH:mm'}"
                            Width="150" />

        <!-- 货币格式化 -->
        <DataGridTextColumn Header="总价(元)"
                            Binding="{Binding TotalPrice, StringFormat='{}{0:F2}'}"
                            Width="100" />

        <!-- 枚举描述转换 -->
        <DataGridTextColumn Header="状态"
                            Binding="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
                            Width="100" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

### 示例5: 宽度自适应（使用*）

**XAML**:
```xaml
<controls:UnifiedManagementTable ItemsSource="{Binding Herbs}">
    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="药材名称" Binding="{Binding Name}" Width="150" />
        <DataGridTextColumn Header="功效" Binding="{Binding Effect}" Width="*" />  <!-- 占据剩余宽度 -->
        <DataGridTextColumn Header="单价(元)" Binding="{Binding Price, StringFormat='{}{0:F2}'}" Width="80" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

## 最佳实践

### 1. 始终使用TwoWay绑定SelectedItem

```xaml
<!-- ✅ 正确 -->
<controls:UnifiedManagementTable
    SelectedItem="{Binding SelectedItem, Mode=TwoWay}" />

<!-- ❌ 错误：用户选中项无法传回ViewModel -->
<controls:UnifiedManagementTable
    SelectedItem="{Binding SelectedItem}" />
```

### 2. 操作列命令绑定使用RelativeSource

```xaml
<!-- ✅ 正确：使用RelativeSource找到UserControl的DataContext -->
<Button Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}" />

<!-- ❌ 错误：DataContext是行数据，没有EditCommand -->
<Button Command="{Binding EditCommand}"
        CommandParameter="{Binding}" />
```

**原因**: DataGrid的CellTemplate内部DataContext是行数据对象，需要向上查找UserControl的DataContext。

### 3. 列宽度设置建议

- 固定宽度列：使用具体数值（如`Width="150"`）
- 主要内容列：使用`Width="*"`占据剩余空间
- 最小必要宽度：避免内容换行

```xaml
<DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="60" />
<DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />
<DataGridTextColumn Header="详细描述" Binding="{Binding Description}" Width="*" />
<DataGridTemplateColumn Header="操作" Width="200" />
```

### 4. 空状态文本本地化

```xaml
<controls:UnifiedManagementTable
    EmptyStateText="暂无患者数据" />  <!-- 针对性提示 -->
```

### 5. 性能优化（大数据量）

对于>1,000行数据：

```csharp
// ViewModel中实现分页
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    TotalCount = result.TotalCount;
    return result.Items; // 只加载当前页数据
}
```

## 样式定制

### DataGrid基础样式

组件内部已应用`BaseDataGridStyle`，包含以下特性：

```xml
<!-- UnifiedComponents.xaml -->
<Style x:Key="BaseDataGridStyle" TargetType="DataGrid">
    <!-- 虚拟化 -->
    <Setter Property="VirtualizingPanel.IsVirtualizing" Value="True"/>
    <Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>
    <Setter Property="EnableRowVirtualization" Value="True"/>

    <!-- 网格线 -->
    <Setter Property="GridLinesVisibility" Value="Horizontal"/>
    <Setter Property="HorizontalGridLinesBrush" Value="#E0E0E0"/>

    <!-- 行高 -->
    <Setter Property="RowHeight" Value="40"/>

    <!-- 其他样式... -->
</Style>
```

### 自定义列头样式

如需自定义列头：

```xaml
<DataGridTextColumn Header="自定义列头">
    <DataGridTextColumn.HeaderStyle>
        <Style TargetType="DataGridColumnHeader">
            <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
            <Setter Property="Foreground" Value="White" />
            <Setter Property="FontWeight" Value="Bold" />
        </Style>
    </DataGridTextColumn.HeaderStyle>
</DataGridTextColumn>
```

## 常见问题

### Q: 表格数据不显示？

**A**: 检查以下4点：
1. `ItemsSource`是否正确绑定到数据集合？
2. 数据集合是否为空？
3. 列的`Binding`路径是否正确？
4. 数据对象是否实现了属性？

```csharp
// 调试方法
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetListAsync();
    Console.WriteLine($"Loaded {result.Count} items"); // 检查数据条数
    return result;
}
```

### Q: 操作列按钮Command不触发？

**A**: 使用`RelativeSource`正确绑定到UserControl的DataContext：

```xaml
<!-- ✅ 正确 -->
<Button Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}" />
```

### Q: 空状态提示不显示？

**A**: 检查`ShowEmptyState`属性是否为true，或数据源是否确实为空：

```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    ShowEmptyState="True"
    EmptyStateText="暂无数据" />
```

### Q: 大数据量滚动卡顿？

**A**: 虚拟化已启用，如仍卡顿检查：
1. 是否在CellTemplate中使用了复杂控件？
2. 是否有大量数据转换器计算？
3. 考虑实现分页而非一次加载所有数据

```csharp
// 推荐：分页加载
public int PageSize { get; set; } = 20; // 每页20条

protected override async Task<List<MyDto>> LoadDataAsync()
{
    return await _service.GetPagedListAsync(CurrentPage, PageSize);
}
```

### Q: 选中项SelectedItem没有回传到ViewModel？

**A**: 确保使用`Mode=TwoWay`绑定：

```xaml
<controls:UnifiedManagementTable
    SelectedItem="{Binding SelectedItem, Mode=TwoWay}" />
```

## 相关资源

- [统一组件库总览](./unified-components.md)
- [UnifiedStatusBadge组件](./unified-statusbadge.md) - 状态列显示
- [故障排查指南](./troubleshooting.md)

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
