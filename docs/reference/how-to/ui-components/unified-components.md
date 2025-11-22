# 统一UI组件库

## 概述

统一UI组件库为LYBTZYZS项目提供了一套标准化的管理界面组件，基于WPF/.NET 8.0构建。这些组件遵循统一的设计系统，简化界面开发，提高代码复用率。

**Epic**: #1840 - Desktop端管理界面UI统一化
**适用场景**: 后台管理界面、数据列表展示、CRUD操作页面

## 组件清单

### 核心组件（4个）

| 组件 | 用途 | 文档 |
|-----|------|------|
| **UnifiedManagementToolBar** | 搜索+筛选+操作按钮工具栏 | [查看文档](./unified-toolbar.md) |
| **UnifiedManagementTable** | 数据表格 | [查看文档](./unified-table.md) |
| **UnifiedStatusBadge** | 状态徽章 | [查看文档](./unified-statusbadge.md) |
| **UnifiedPaginationBar** | 分页导航 | [查看文档](./unified-paginationbar.md) |

### 支持资源

- **UnifiedDesignSystem.xaml** - 颜色、字体、间距定义
- **UnifiedComponents.xaml** - DataGrid、Button等基础样式

## 设计原则

### 1. 4 epx增量规则

所有间距、圆角半径必须是4的倍数：

```xaml
<!-- ✅ 正确 -->
<Setter Property="Padding" Value="8,12" />
<Setter Property="CornerRadius" Value="8" />

<!-- ❌ 错误 -->
<Setter Property="Padding" Value="7,11" />
<Setter Property="CornerRadius" Value="7" />
```

### 2. Type Ramp字体系统

| 级别 | 字体大小 | 行高 | 使用场景 |
|-----|---------|------|---------|
| Caption | 12 epx | 16 epx | 辅助文字、标签 |
| Body | 14 epx | 20 epx | 正文、表格单元格 |
| Subtitle | 20 epx | 28 epx | 子标题、卡片标题 |
| Title | 28 epx | 36 epx | 页面标题 |

### 3. Slot插槽机制

组件使用`ContentPresenter`提供插槽，支持灵活定制：

- **FilterContent** - 筛选区域插槽（ToolBar）
- **ActionButtons** - 操作按钮区域插槽（ToolBar）
- **Columns** - 表格列定义插槽（Table）

## 快速开始

### 1. 添加命名空间引用

```xaml
<UserControl xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">
```

### 2. 构建标准管理界面（3行Grid）

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
                <Button Content="刷新" Command="{Binding RefreshCommand}" Style="{StaticResource SecondaryButton}" />
            </StackPanel>
        </controls:UnifiedManagementToolBar.ActionButtons>
    </controls:UnifiedManagementToolBar>

    <!-- 表格 -->
    <controls:UnifiedManagementTable
        Grid.Row="1"
        ItemsSource="{Binding Items}"
        SelectedItem="{Binding SelectedItem, Mode=TwoWay}"
        EmptyStateText="暂无数据">

        <controls:UnifiedManagementTable.Columns>
            <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="150" />
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

### 3. ViewModel继承基类

```csharp
using LYBT.Desktop.Models.ViewModels.Base;

public class MyManagementViewModel : UnifiedListViewModelBase<MyDto>
{
    private readonly IMyService _myService;

    public MyManagementViewModel(IMyService myService)
    {
        _myService = myService;
    }

    protected override async Task<List<MyDto>> LoadDataAsync()
    {
        var result = await _myService.GetPagedListAsync(
            SearchText,
            CurrentPage,
            PageSize);

        TotalCount = result.TotalCount;
        return result.Items;
    }
}
```

## 最佳实践

### ViewModel基类

**强制要求**: 所有列表管理ViewModel必须继承`UnifiedListViewModelBase<T>`

**基类提供的属性和命令**:
- `SearchText` - 搜索文本（双向绑定）
- `CurrentPage` - 当前页码（双向绑定）
- `PageSize` - 每页数量（双向绑定）
- `TotalCount` - 总记录数
- `TotalPages` - 总页数（自动计算）
- `Items` - 数据列表
- `SelectedItem` - 选中项
- `SearchCommand` - 搜索命令
- `RefreshCommand` - 刷新命令
- `FirstPageCommand` - 首页命令
- `PreviousPageCommand` - 上一页命令
- `NextPageCommand` - 下一页命令
- `LastPageCommand` - 末页命令

### 状态枚举映射

**推荐做法**: 使用`EnumDescriptionConverter`自动映射枚举显示文本

```csharp
public enum PatientStatus
{
    [Description("正常")]
    Active,

    [Description("已删除")]
    Deleted
}
```

```xaml
<controls:UnifiedStatusBadge
    Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
    Type="Success" />
```

### 性能优化

所有DataGrid已启用虚拟化：

```xml
<!-- UnifiedComponents.xaml中已配置 -->
<Setter Property="VirtualizingPanel.IsVirtualizing" Value="True"/>
<Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>
<Setter Property="EnableRowVirtualization" Value="True"/>
```

**预期性能**:
- 1,000行数据：流畅滚动，内存占用 <20MB
- 10,000行数据：流畅滚动，内存占用 <50MB（仅渲染可见行）

### 搜索触发优化

搜索框已优化为按Enter键或失去焦点时触发，避免每次按键都查询：

```xaml
<!-- UnifiedManagementToolBar内部已优化 -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=LostFocus}">
    <TextBox.InputBindings>
        <KeyBinding Key="Enter" Command="{Binding SearchCommand}" />
    </TextBox.InputBindings>
</TextBox>
```

**性能提升**: 数据库查询减少75%

## 已应用界面

以下5个界面已成功迁移至统一组件（截至2025-11-06）：

| 界面 | 代码精简率 | Commit |
|-----|-----------|--------|
| 用户管理 (UserManagementView) | 32.4% | 9696aa05 |
| 患者管理 (PatientManagementView) | 32.8% | fa3466d7 |
| 病案管理 (MedicalCaseManagementView) | 18.4% | dd919da5 |
| 中药管理 (HerbManagementView) | 26.7% | 0225e7cc |
| 方剂管理 (FormulaManagementView) | 22.1% | 207fa909 |

**平均代码精简率**: 26.5%

## 故障排查

遇到问题？查看[故障排查指南](./troubleshooting.md)

## 参考资源

- **设计文档**: `docs/explanation/design/ui-unification-design.md`
- **验收报告**:
  - `docs/reports/phase2-acceptance-report.md` - 界面迁移报告
  - `docs/reports/phase3-task3.4-performance-report.md` - 性能优化报告
- **WPF最佳实践**: [Microsoft Learn - WPF Typography](https://learn.microsoft.com/en-us/windows/apps/design/style/typography)

---

**最后更新**: 2025-11-06
**适用版本**: LYBTZYZS v1.0+
**维护团队**: Desktop Infrastructure Team
