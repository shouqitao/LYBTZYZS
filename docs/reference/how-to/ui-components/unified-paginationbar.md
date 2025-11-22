# UnifiedPaginationBar 组件

## 概述

UnifiedPaginationBar 是统一的分页导航组件,提供完整的分页控制功能,包括页码显示、页面跳转、每页数量选择等。

**命名空间**: `LYBT.Desktop.Infrastructure.Controls`
**继承**: `UserControl`
**Issue**: #1840, #1845

**典型场景**:
- 数据列表分页导航
- 大数据集分页显示
- 配合UnifiedManagementTable使用

## 快速开始

最简单的使用示例:

```xaml
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize, Mode=TwoWay}"
    TotalCount="{Binding TotalCount}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}" />
```

## API参考

### 依赖属性

| 属性名 | 类型 | 默认值 | 绑定模式 | 说明 |
|-------|------|--------|---------|------|
| `CurrentPage` | `int` | `1` | `TwoWay` | 当前页码(从1开始) |
| `TotalPages` | `int` | `1` | `OneWay` | 总页数 |
| `PageSize` | `int` | `20` | `TwoWay` | 每页显示数量 |
| `TotalCount` | `int` | `0` | `OneWay` | 总记录数 |
| `FirstPageCommand` | `ICommand` | `null` | `OneWay` | 首页命令 |
| `PreviousPageCommand` | `ICommand` | `null` | `OneWay` | 上一页命令 |
| `NextPageCommand` | `ICommand` | `null` | `OneWay` | 下一页命令 |
| `LastPageCommand` | `ICommand` | `null` | `OneWay` | 末页命令 |
| `PageSizeChangedCommand` | `ICommand` | `null` | `OneWay` | 页大小改变命令 |

### 自动计算逻辑

组件内部自动计算以下状态:
- **首页按钮禁用**: CurrentPage == 1
- **上一页按钮禁用**: CurrentPage == 1
- **下一页按钮禁用**: CurrentPage == TotalPages
- **末页按钮禁用**: CurrentPage == TotalPages

## 使用示例

### 示例1: 基础用法(配合UnifiedListViewModelBase)

**XAML**:
```xaml
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize, Mode=TwoWay}"
    TotalCount="{Binding TotalCount}"
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}" />
```

**ViewModel**:
```csharp
public class MyViewModel : UnifiedListViewModelBase<MyDto>
{
    // 所有分页相关属性和命令已由基类提供,无需手动实现:
    // - CurrentPage (int, TwoWay)
    // - TotalPages (int, 自动计算)
    // - PageSize (int, TwoWay)
    // - TotalCount (int)
    // - FirstPageCommand
    // - PreviousPageCommand
    // - NextPageCommand
    // - LastPageCommand

    protected override async Task<List<MyDto>> LoadDataAsync()
    {
        var result = await _service.GetPagedListAsync(
            SearchText,
            CurrentPage,
            PageSize);

        TotalCount = result.TotalCount; // 设置总记录数后TotalPages自动计算
        return result.Items;
    }
}
```

### 示例2: 完整的管理界面布局

**XAML**:
```xaml
<Grid Background="{StaticResource BackgroundBrush}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- 工具栏 -->
        <RowDefinition Height="*" />     <!-- 表格 -->
        <RowDefinition Height="Auto" />  <!-- 分页栏 -->
    </Grid.RowDefinitions>

    <!-- 工具栏 -->
    <controls:UnifiedManagementToolBar
        Grid.Row="0"
        SearchText="{Binding SearchText, Mode=TwoWay}"
        SearchCommand="{Binding SearchCommand}">
        <controls:UnifiedManagementToolBar.ActionButtons>
            <StackPanel Orientation="Horizontal">
                <Button Content="+ 新建" Command="{Binding AddCommand}" Style="{StaticResource SuccessButton}" />
            </StackPanel>
        </controls:UnifiedManagementToolBar.ActionButtons>
    </controls:UnifiedManagementToolBar>

    <!-- 表格 -->
    <controls:UnifiedManagementTable
        Grid.Row="1"
        ItemsSource="{Binding Items}"
        SelectedItem="{Binding SelectedItem, Mode=TwoWay}">
        <controls:UnifiedManagementTable.Columns>
            <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />
            <DataGridTextColumn Header="描述" Binding="{Binding Description}" Width="*" />
        </controls:UnifiedManagementTable.Columns>
    </controls:UnifiedManagementTable>

    <!-- 分页栏 -->
    <controls:UnifiedPaginationBar
        Grid.Row="2"
        CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
        TotalPages="{Binding TotalPages}"
        PageSize="{Binding PageSize, Mode=TwoWay}"
        TotalCount="{Binding TotalCount}"
        FirstPageCommand="{Binding FirstPageCommand}"
        PreviousPageCommand="{Binding PreviousPageCommand}"
        NextPageCommand="{Binding NextPageCommand}"
        LastPageCommand="{Binding LastPageCommand}" />
</Grid>
```

### 示例3: 自定义每页数量选项

**场景**: 默认情况下,组件提供 10/20/50/100 四个每页数量选项,如需自定义需修改ViewModel。

**ViewModel**:
```csharp
public class MyViewModel : UnifiedListViewModelBase<MyDto>
{
    // 自定义每页数量选项
    public override List<int> PageSizeOptions => new List<int> { 5, 10, 20, 30 };

    protected override async Task<List<MyDto>> LoadDataAsync()
    {
        var result = await _service.GetPagedListAsync(
            SearchText,
            CurrentPage,
            PageSize);

        TotalCount = result.TotalCount;
        return result.Items;
    }
}
```

### 示例4: 手动实现分页逻辑(不使用基类)

**ViewModel**(不继承UnifiedListViewModelBase):
```csharp
public class CustomViewModel : BindableBase
{
    private readonly IMyService _myService;

    // 分页属性
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            SetProperty(ref _currentPage, value);
            RefreshCommand.Execute(null);
        }
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            SetProperty(ref _pageSize, value);
            CurrentPage = 1; // 页大小改变时重置到第1页
            RefreshCommand.Execute(null);
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set
        {
            SetProperty(ref _totalCount, value);
            RaisePropertyChanged(nameof(TotalPages));
        }
    }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    // 分页命令
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }
    public ICommand RefreshCommand { get; }

    public CustomViewModel(IMyService myService)
    {
        _myService = myService;

        FirstPageCommand = new DelegateCommand(OnFirstPage, () => CurrentPage > 1);
        PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
        NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        LastPageCommand = new DelegateCommand(OnLastPage, () => CurrentPage < TotalPages);
        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
    }

    private void OnFirstPage() => CurrentPage = 1;
    private void OnPreviousPage() => CurrentPage--;
    private void OnNextPage() => CurrentPage++;
    private void OnLastPage() => CurrentPage = TotalPages;

    private async Task LoadDataAsync()
    {
        var result = await _myService.GetPagedListAsync(CurrentPage, PageSize);
        Items = new ObservableCollection<MyDto>(result.Items);
        TotalCount = result.TotalCount;
    }
}
```

### 示例5: 分页信息国际化

**场景**: 自定义分页信息显示文本。

**方法1: 覆盖组件模板** (需修改UnifiedPaginationBar.xaml):
```xaml
<!-- 在组件内部修改 -->
<TextBlock>
    <Run Text="共" />
    <Run Text="{Binding TotalCount, RelativeSource={RelativeSource AncestorType=UserControl}}" />
    <Run Text="条记录, 共" />
    <Run Text="{Binding TotalPages, RelativeSource={RelativeSource AncestorType=UserControl}}" />
    <Run Text="页" />
</TextBlock>
```

**方法2: 在ViewModel中提供计算属性** (推荐):
```csharp
public string PageInfo => $"共 {TotalCount} 条记录, 第 {CurrentPage}/{TotalPages} 页";
```

```xaml
<TextBlock Text="{Binding PageInfo}" Margin="0,0,16,0" />
```

## 最佳实践

### 1. 始终使用TwoWay绑定CurrentPage和PageSize

```xaml
<!-- ✅ 正确 -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    PageSize="{Binding PageSize, Mode=TwoWay}" />

<!-- ❌ 错误: 用户切换页码/页大小无法更新ViewModel -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage}"
    PageSize="{Binding PageSize}" />
```

### 2. 推荐继承UnifiedListViewModelBase

**原因**:
- ✅ 自动提供所有分页属性和命令
- ✅ 自动计算TotalPages
- ✅ 自动处理命令CanExecute逻辑
- ✅ 减少80%样板代码

```csharp
// ✅ 推荐: 继承基类
public class MyViewModel : UnifiedListViewModelBase<MyDto>
{
    protected override async Task<List<MyDto>> LoadDataAsync()
    {
        var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
        TotalCount = result.TotalCount;
        return result.Items;
    }
}

// ❌ 不推荐: 手动实现所有分页逻辑(需200+行代码)
public class MyViewModel : BindableBase
{
    // ... 手动实现CurrentPage, TotalPages, PageSize, TotalCount
    // ... 手动实现FirstPageCommand, PreviousPageCommand, NextPageCommand, LastPageCommand
    // ... 手动实现CanExecute逻辑
}
```

### 3. 设置TotalCount后TotalPages自动更新

```csharp
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);

    // ✅ 正确: 设置TotalCount, TotalPages自动计算
    TotalCount = result.TotalCount;

    // ❌ 错误: 不要手动计算TotalPages
    // TotalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);

    return result.Items;
}
```

### 4. 页大小改变时重置到第1页

```csharp
public int PageSize
{
    get => _pageSize;
    set
    {
        SetProperty(ref _pageSize, value);
        CurrentPage = 1; // 🔑 关键: 页大小改变时重置到第1页
        RefreshCommand.Execute(null);
    }
}
```

**理由**: 避免越界(例如原来100条记录分10页,PageSize=10。改为PageSize=50后只有2页,如果停留在第10页会越界)。

### 5. 命令绑定顺序

```xaml
<!-- ✅ 推荐顺序: 按钮从左到右的顺序 -->
<controls:UnifiedPaginationBar
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}" />
```

## 性能优化

### 1. 避免全量加载数据

```csharp
// ❌ 错误: 一次性加载所有数据,内存占用大
public async Task LoadAllDataAsync()
{
    var allItems = await _service.GetAllAsync(); // 可能有10万条
    Items = new ObservableCollection<MyDto>(allItems);
}

// ✅ 正确: 分页加载,每次只加载当前页数据
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    TotalCount = result.TotalCount;
    return result.Items; // 只加载20条
}
```

**性能对比**:
- 全量加载10万条: ~500MB内存, ~5秒加载时间
- 分页加载20条: ~5MB内存, ~200ms加载时间

### 2. 配合DataGrid虚拟化

```xaml
<!-- UnifiedManagementTable内部已启用虚拟化 -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    ... />
```

**虚拟化配置**(已在UnifiedComponents.xaml中配置):
```xaml
<Setter Property="VirtualizingPanel.IsVirtualizing" Value="True"/>
<Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>
<Setter Property="EnableRowVirtualization" Value="True"/>
```

**预期性能**:
- 1,000条数据: 滚动流畅, 内存占用 <20MB
- 10,000条数据: 滚动流畅, 内存占用 <50MB

## 常见问题

### Q: 分页按钮不显示或禁用?

**A**: 检查以下3点:

1. **TotalPages是否正确计算?**
```csharp
// 调试方法
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    TotalCount = result.TotalCount;
    Console.WriteLine($"TotalCount={TotalCount}, TotalPages={TotalPages}"); // 检查值
    return result.Items;
}
```

2. **命令是否正确绑定?**
```xaml
<!-- 检查命令绑定是否正确 -->
<controls:UnifiedPaginationBar
    FirstPageCommand="{Binding FirstPageCommand}"
    ... />
```

3. **ViewModel是否继承UnifiedListViewModelBase?**
```csharp
// ✅ 正确
public class MyViewModel : UnifiedListViewModelBase<MyDto> { }

// ❌ 错误: 未继承基类,需手动实现所有命令
public class MyViewModel : BindableBase { }
```

### Q: 点击下一页/上一页没有反应?

**A**: 检查CurrentPage是否使用TwoWay绑定:

```xaml
<!-- ✅ 正确 -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}" />

<!-- ❌ 错误: 缺少TwoWay,页码变更无法传回ViewModel -->
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage}" />
```

### Q: 改变每页数量后页码越界?

**A**: 在PageSize setter中重置到第1页:

```csharp
public int PageSize
{
    get => _pageSize;
    set
    {
        SetProperty(ref _pageSize, value);
        CurrentPage = 1; // 🔑 关键: 重置到第1页
        RefreshCommand.Execute(null);
    }
}
```

### Q: TotalPages计算不正确?

**A**: 确保正确设置TotalCount:

```csharp
// ✅ 正确
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    TotalCount = result.TotalCount; // 必须设置
    return result.Items;
}

// ❌ 错误: 忘记设置TotalCount
protected override async Task<List<MyDto>> LoadDataAsync()
{
    var result = await _service.GetPagedListAsync(CurrentPage, PageSize);
    return result.Items; // TotalCount保持0,TotalPages为1
}
```

### Q: 如何显示"显示第X-Y条,共Z条"?

**A**: 在ViewModel中提供计算属性:

```csharp
public string DisplayInfo
{
    get
    {
        if (TotalCount == 0) return "暂无数据";

        int startIndex = (CurrentPage - 1) * PageSize + 1;
        int endIndex = Math.Min(CurrentPage * PageSize, TotalCount);
        return $"显示第 {startIndex}-{endIndex} 条, 共 {TotalCount} 条";
    }
}
```

```xaml
<TextBlock Text="{Binding DisplayInfo}" />
```

### Q: 首页/末页按钮在哪里?

**A**: UnifiedPaginationBar组件已包含首页/末页按钮:

```xaml
<!-- 组件内部已包含4个按钮: |< < > >| -->
<controls:UnifiedPaginationBar
    FirstPageCommand="{Binding FirstPageCommand}"    <!-- |< 首页 -->
    PreviousPageCommand="{Binding PreviousPageCommand}" <!-- < 上一页 -->
    NextPageCommand="{Binding NextPageCommand}"      <!-- > 下一页 -->
    LastPageCommand="{Binding LastPageCommand}" />   <!-- >| 末页 -->
```

**如未显示**: 检查命令是否正确绑定(继承UnifiedListViewModelBase会自动提供)。

## 实际应用示例

### 用户管理界面(UserManagementView)

```xaml
<controls:UnifiedPaginationBar
    Grid.Row="2"
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize, Mode=TwoWay}"
    TotalCount="{Binding TotalCount}"
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}" />
```

### 患者管理界面(PatientManagementView)

```xaml
<controls:UnifiedPaginationBar
    Grid.Row="2"
    CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize, Mode=TwoWay}"
    TotalCount="{Binding TotalCount}"
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}" />
```

**ViewModel统一实现**:
```csharp
public class PatientManagementViewModel : UnifiedListViewModelBase<PatientDto>
{
    protected override async Task<List<PatientDto>> LoadDataAsync()
    {
        var result = await _patientService.GetPagedListAsync(
            SearchText,
            CurrentPage,
            PageSize);

        TotalCount = result.TotalCount; // TotalPages自动计算
        return result.Items;
    }
}
```

## 相关资源

- [统一组件库总览](./unified-components.md)
- [UnifiedManagementTable组件](./unified-table.md) - 配合使用
- [UnifiedListViewModelBase文档](./../../explanation/architecture/client/viewmodel-base.md) - ViewModel基类
- [故障排查指南](./troubleshooting.md)

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
