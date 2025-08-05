# 系统管理模块开发模板

## 一、模块文件结构

```
{ModuleName}/
├── ViewModels/
│   ├── {Module}ManagementViewModel.cs          # 主列表视图模型
│   ├── Add{Module}DialogViewModel.cs           # 新增对话框视图模型
│   ├── Edit{Module}DialogViewModel.cs          # 编辑对话框视图模型
│   └── View{Module}DialogViewModel.cs          # 查看详情对话框视图模型（可选）
├── Views/
│   ├── {Module}ManagementView.xaml             # 主列表视图
│   ├── {Module}ManagementView.xaml.cs          # 主列表视图代码
│   ├── Add{Module}Dialog.xaml                  # 新增对话框
│   ├── Add{Module}Dialog.xaml.cs               # 新增对话框代码
│   ├── Edit{Module}Dialog.xaml                 # 编辑对话框
│   ├── Edit{Module}Dialog.xaml.cs              # 编辑对话框代码
│   └── View{Module}Dialog.xaml                 # 查看详情对话框（可选）
└── Converters/                                  # 特定转换器（可选）
    └── {Module}SpecificConverter.cs
```

## 二、主列表视图模板

### 2.1 视图模型 (`{Module}ManagementViewModel.cs`)

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Modules.SystemManagement.{ModuleName}.ViewModels
{
    /// <summary>
    /// {中文模块名}管理视图模型
    /// </summary>
    public class {Module}ManagementViewModel : BindableBase
    {
        private readonly I{Module}Service _{module}Service;
        
        #region 属性
        
        private string _searchKeyword = string.Empty;
        private {Module}Info? _selected{Module};
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public ObservableCollection<{Module}Info> {Module}s { get; }
        public ICollectionView {Module}sView { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的{中文模块名}</summary>
        public {Module}Info? Selected{Module}
        {
            get => _selected{Module};
            set => SetProperty(ref _selected{Module}, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePaginationStatus();
                }
            }
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdatePaginationStatus();
                }
            }
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>状态文本</summary>
        public string StatusText => $"第 {CurrentPage} 页，共 {TotalPages} 页，总计 {TotalCount} 条记录";

        /// <summary>是否可以跳转到第一页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>是否可以跳转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>是否可以跳转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>是否可以跳转到最后一页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;
        
        #endregion

        #region 命令
        
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand Add{Module}Command { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<{Module}Info> Edit{Module}Command { get; }
        public DelegateCommand<{Module}Info> View{Module}Command { get; }
        public DelegateCommand<{Module}Info> Delete{Module}Command { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }
        
        #endregion

        public {Module}ManagementViewModel(I{Module}Service {module}Service)
        {
            _{module}Service = {module}Service;

            {Module}s = new ObservableCollection<{Module}Info>();
            {Module}sView = CollectionViewSource.GetDefaultView({Module}s);

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await Load{Module}sAsync());
            Add{Module}Command = new DelegateCommand(ExecuteAdd{Module});
            RefreshCommand = new DelegateCommand(async () => await Load{Module}sAsync());
            Edit{Module}Command = new DelegateCommand<{Module}Info>(ExecuteEdit{Module});
            View{Module}Command = new DelegateCommand<{Module}Info>(ExecuteView{Module});
            Delete{Module}Command = new DelegateCommand<{Module}Info>(async ({module}) => await ExecuteDelete{Module}({module}));
            
            FirstPageCommand = new DelegateCommand(async () => { CurrentPage = 1; await Load{Module}sAsync(); }, () => CanGoFirstPage);
            PreviousPageCommand = new DelegateCommand(async () => { CurrentPage--; await Load{Module}sAsync(); }, () => CanGoPreviousPage);
            NextPageCommand = new DelegateCommand(async () => { CurrentPage++; await Load{Module}sAsync(); }, () => CanGoNextPage);
            LastPageCommand = new DelegateCommand(async () => { CurrentPage = TotalPages; await Load{Module}sAsync(); }, () => CanGoLastPage);

            // 加载初始数据
            _ = Load{Module}sAsync();
        }

        #region 私有方法
        
        /// <summary>
        /// 加载{中文模块名}列表
        /// </summary>
        private async Task Load{Module}sAsync()
        {
            try
            {
                IsLoading = true;
                {Module}s.Clear();

                var request = new PaginationRequest
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize,
                    SearchKeyword = SearchKeyword
                };

                var result = await _{module}Service.GetPagedAsync(request);
                if (result.IsSuccess && result.Data != null)
                {
                    TotalCount = result.Data.TotalCount;
                    foreach (var item in result.Data.Items)
                    {
                        {Module}s.Add(item);
                    }
                }
                else
                {
                    MessageBox.Show($"加载{中文模块名}列表失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载{中文模块名}列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 新增{中文模块名}
        /// </summary>
        private void ExecuteAdd{Module}()
        {
            // TODO: 实现新增对话框
            MessageBox.Show("新增{中文模块名}功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 编辑{中文模块名}
        /// </summary>
        private void ExecuteEdit{Module}({Module}Info {module})
        {
            if ({module} == null) return;
            // TODO: 实现编辑对话框
            MessageBox.Show("编辑{中文模块名}功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 查看{中文模块名}详情
        /// </summary>
        private void ExecuteView{Module}({Module}Info {module})
        {
            if ({module} == null) return;
            // TODO: 实现查看详情对话框
            MessageBox.Show($"{中文模块名}详情查看功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 删除{中文模块名}
        /// </summary>
        private async Task ExecuteDelete{Module}({Module}Info {module})
        {
            if ({module} == null) return;

            var confirmResult = MessageBox.Show($"确定要删除{中文模块名} {{{module}.Name}} 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var result = await _{module}Service.DeleteAsync({module}.Id);
                if (result.IsSuccess)
                {
                    await Load{Module}sAsync();
                    MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"删除失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新分页状态
        /// </summary>
        private void UpdatePaginationStatus()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(CanGoFirstPage));
            RaisePropertyChanged(nameof(CanGoPreviousPage));
            RaisePropertyChanged(nameof(CanGoNextPage));
            RaisePropertyChanged(nameof(CanGoLastPage));
            
            FirstPageCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }
        
        #endregion
    }
}
```

### 2.2 视图 (`{Module}ManagementView.xaml`)

```xml
<UserControl x:Class="LYBT.WPF.Client.Modules.SystemManagement.{ModuleName}.Views.{Module}ManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:prism="http://prismlibrary.com/"
             mc:Ignorable="d" 
             d:DesignHeight="600" d:DesignWidth="1000"
             prism:ViewModelLocator.AutoWireViewModel="True">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="#F8F9FA" Padding="20,15">
            <TextBlock Text="{中文模块名}管理" FontSize="18" FontWeight="Bold" Foreground="#333333"/>
        </Border>

        <!-- 工具栏 -->
        <Border Grid.Row="1" Background="White" BorderBrush="#DEE2E6" BorderThickness="0,0,0,1" Padding="20,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 搜索区域 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal">
                    <TextBox x:Name="SearchTextBox" 
                             Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                             Width="300" Height="32"
                             VerticalContentAlignment="Center"
                             Padding="10,0"
                             BorderBrush="#CED4DA"
                             BorderThickness="1">
                        <TextBox.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </TextBox.Resources>
                        <TextBox.InputBindings>
                            <KeyBinding Key="Enter" Command="{Binding SearchCommand}"/>
                        </TextBox.InputBindings>
                    </TextBox>
                    
                    <Button Content="搜索" 
                            Command="{Binding SearchCommand}"
                            Margin="10,0,0,0"
                            Width="80" Height="32"
                            Background="#007BFF"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                    </Button>
                </StackPanel>

                <!-- 操作按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="新增{中文模块名}" 
                            Command="{Binding Add{Module}Command}"
                            Margin="0,0,10,0"
                            Width="120" Height="32"
                            Background="#28A745"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                    </Button>
                    
                    <Button Content="刷新" 
                            Command="{Binding RefreshCommand}"
                            Width="80" Height="32"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 数据表格 -->
        <DataGrid Grid.Row="2" 
                  ItemsSource="{Binding {Module}sView}"
                  SelectedItem="{Binding Selected{Module}}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  GridLinesVisibility="Horizontal"
                  HeadersVisibility="Column"
                  BorderThickness="0"
                  Background="White"
                  RowHeight="45">
            
            <DataGrid.Resources>
                <Style TargetType="DataGridColumnHeader">
                    <Setter Property="Background" Value="#F8F9FA"/>
                    <Setter Property="Foreground" Value="#495057"/>
                    <Setter Property="FontWeight" Value="Bold"/>
                    <Setter Property="Height" Value="40"/>
                    <Setter Property="HorizontalContentAlignment" Value="Center"/>
                    <Setter Property="BorderBrush" Value="#DEE2E6"/>
                    <Setter Property="BorderThickness" Value="0,0,0,1"/>
                </Style>
                
                <Style TargetType="DataGridRow">
                    <Setter Property="Background" Value="White"/>
                    <Style.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="#F1F3F5"/>
                        </Trigger>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter Property="Background" Value="#E7F3FF"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
                
                <Style TargetType="DataGridCell">
                    <Setter Property="BorderThickness" Value="0"/>
                    <Setter Property="VerticalAlignment" Value="Center"/>
                    <Setter Property="HorizontalAlignment" Value="Center"/>
                </Style>
            </DataGrid.Resources>
            
            <DataGrid.Columns>
                <!-- TODO: 根据实际业务定义列 -->
                
                <!-- 操作列 -->
                <DataGridTemplateColumn Header="操作" Width="*" MinWidth="200">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                <Button Content="查看" 
                                        Command="{Binding DataContext.View{Module}Command, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Margin="2,0"
                                        Padding="8,4"
                                        Background="#17A2B8"
                                        Foreground="White"
                                        BorderThickness="0"
                                        Cursor="Hand"/>
                                
                                <Button Content="编辑" 
                                        Command="{Binding DataContext.Edit{Module}Command, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Margin="2,0"
                                        Padding="8,4"
                                        Background="#FFC107"
                                        Foreground="White"
                                        BorderThickness="0"
                                        Cursor="Hand"/>
                                
                                <Button Content="删除" 
                                        Command="{Binding DataContext.Delete{Module}Command, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Margin="2,0"
                                        Padding="8,4"
                                        Background="#DC3545"
                                        Foreground="White"
                                        BorderThickness="0"
                                        Cursor="Hand"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 分页栏 -->
        <Border Grid.Row="3" Background="#F8F9FA" BorderBrush="#DEE2E6" BorderThickness="0,1,0,0" Padding="20,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 分页信息 -->
                <TextBlock Grid.Column="0" 
                           Text="{Binding StatusText}" 
                           VerticalAlignment="Center"
                           Foreground="#6C757D"/>

                <!-- 分页按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="首页" 
                            Command="{Binding FirstPageCommand}"
                            Margin="0,0,5,0"
                            Padding="10,5"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand"/>
                    
                    <Button Content="上一页" 
                            Command="{Binding PreviousPageCommand}"
                            Margin="0,0,5,0"
                            Padding="10,5"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand"/>
                    
                    <Button Content="下一页" 
                            Command="{Binding NextPageCommand}"
                            Margin="0,0,5,0"
                            Padding="10,5"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand"/>
                    
                    <Button Content="末页" 
                            Command="{Binding LastPageCommand}"
                            Padding="10,5"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            Cursor="Hand"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Loading遮罩 -->
        <Grid Grid.Row="0" Grid.RowSpan="4" 
              Background="#80000000" 
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar IsIndeterminate="True" Width="200" Height="20"/>
                <TextBlock Text="正在加载..." Foreground="White" HorizontalAlignment="Center" Margin="0,10,0,0"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

## 三、对话框模板

### 3.1 新增对话框视图模型 (`Add{Module}DialogViewModel.cs`)

```csharp
using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace LYBT.WPF.Client.Modules.SystemManagement.{ModuleName}.ViewModels
{
    /// <summary>
    /// 新增{中文模块名}对话框视图模型
    /// </summary>
    public class Add{Module}DialogViewModel : BindableBase
    {
        private readonly I{Module}Service _{module}Service;
        
        // TODO: 定义属性
        
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        
        public Action<bool>? CloseDialogCallback { get; set; }

        public Add{Module}DialogViewModel(I{Module}Service {module}Service)
        {
            _{module}Service = {module}Service;
            
            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        private async void ExecuteSave()
        {
            // TODO: 验证逻辑
            
            try
            {
                // TODO: 构建实体并保存
                
                MessageBox.Show("{中文模块名}保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseDialogCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}
```

## 四、模块注册

在 `SystemManagementModule.cs` 中注册：

```csharp
// 注册{中文模块名}管理视图
containerRegistry.RegisterForNavigation<{Module}ManagementView>();
```

## 五、导航集成

在 `AdminMainView.xaml` 中添加导航按钮：

```xml
<!-- {中文模块名}管理 -->
<Button Command="{Binding NavigateTo{Module}ManagementCommand}" 
        Style="{x:Null}"
        Background="Transparent"
        BorderThickness="0"
        Padding="20,15"
        HorizontalContentAlignment="Left"
        Cursor="Hand">
    <Button.Template>
        <ControlTemplate TargetType="Button">
            <Border Background="{TemplateBinding Background}" 
                    Padding="{TemplateBinding Padding}">
                <ContentPresenter/>
            </Border>
            <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Background" Value="#E9ECEF"/>
                </Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate>
    </Button.Template>
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="{图标}" FontSize="16" VerticalAlignment="Center" Margin="0,0,10,0"/>
        <TextBlock Text="{中文模块名}管理" FontSize="14" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

在 `AdminMainViewModel.cs` 中添加命令：

```csharp
public DelegateCommand NavigateTo{Module}ManagementCommand { get; }

// 在构造函数中
NavigateTo{Module}ManagementCommand = new DelegateCommand(() => NavigateTo("{Module}ManagementView"));
```

## 六、服务接口规范

```csharp
public interface I{Module}Service
{
    /// <summary>
    /// 分页获取{中文模块名}列表
    /// </summary>
    Task<ServiceResult<PagedResult<{Module}Info>>> GetPagedAsync(PaginationRequest request);
    
    /// <summary>
    /// 根据ID获取{中文模块名}
    /// </summary>
    Task<ServiceResult<{Module}Info>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 新增{中文模块名}
    /// </summary>
    Task<ServiceResult<{Module}Info>> CreateAsync({Module}CreateDto createDto);
    
    /// <summary>
    /// 更新{中文模块名}
    /// </summary>
    Task<ServiceResult<{Module}Info>> UpdateAsync({Module}UpdateDto updateDto);
    
    /// <summary>
    /// 删除{中文模块名}
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
```

## 七、开发流程

1. **创建文件结构**
   - 在 SystemManagement 下创建模块文件夹
   - 创建 ViewModels 和 Views 子文件夹

2. **实现视图模型**
   - 先实现 {Module}ManagementViewModel
   - 实现对话框视图模型（可选）

3. **实现视图**
   - 实现 {Module}ManagementView.xaml
   - 实现对话框视图（可选）

4. **注册配置**
   - 在 SystemManagementModule 中注册视图
   - 在 AdminMainView 中添加导航按钮
   - 在 AdminMainViewModel 中添加导航命令

5. **测试验证**
   - 编译项目确保无错误
   - 运行应用测试功能

## 八、注意事项

1. **命名规范**
   - 使用 PascalCase 命名
   - 保持命名一致性
   - 使用有意义的名称

2. **错误处理**
   - 所有异步操作都要有 try-catch
   - 显示友好的错误消息
   - 记录详细的错误日志

3. **用户体验**
   - 提供加载状态指示
   - 操作前进行确认
   - 操作后给出反馈

4. **代码复用**
   - 使用基类减少重复代码
   - 提取公共方法和组件
   - 遵循 DRY 原则