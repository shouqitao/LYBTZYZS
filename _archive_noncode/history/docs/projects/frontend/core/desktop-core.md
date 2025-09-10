# Desktop.Core Project (桌面核心控件库)

## 📋 项目概述

### 项目定位
**Desktop.Core** 是凌隐宝堂中医诊所系统的**WPF核心控件库项目**，提供整个桌面应用的基础控件、样式系统、用户控件和UI基础设施。作为前端架构的核心层，为所有业务模块提供统一的UI组件和交互体验。

### 核心价值
- 🎨 **统一设计系统**: 提供一致的视觉风格和交互体验
- 🔧 **可复用控件库**: 高质量自定义控件减少重复开发
- ⚡ **性能优化组件**: 虚拟化列表、智能加载等高性能控件
- 🌟 **现代化UI**: 符合现代医疗软件界面设计标准
- 🎯 **业务导向**: 专门针对中医诊所业务场景优化的控件
- 🔗 **模块化架构**: 支持Prism模块化和依赖注入

### 技术定位 (v1.0)
```
LYBT.Desktop.Core (核心控件库) ← 本项目
    ↑ 依赖
WPF Framework + Prism.DryIoc 8.1.97
    ↓ 支持
8个业务模块 + 3个工作台 + Shell主程序
```

## 🏗️ 技术架构

### 核心技术栈
```csharp
// 基础技术栈
- .NET 8.0-windows
- WPF (Windows Presentation Foundation)
- Prism.DryIoc 8.1.97 (MVVM + 模块化)
- Microsoft.Xaml.Behaviors.Wpf (行为支持)
- Microsoft.Extensions.DependencyInjection (依赖注入)

// 项目引用
<ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
<ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
```

### 项目结构架构
```
src/Client/Desktop/Core/
├── Controls/                    # 自定义控件
│   ├── Auth/                   # 认证相关控件
│   ├── Authentication/         # 身份验证控件
│   ├── ErrorHandling/          # 错误处理控件
│   ├── FormulaTemplates/       # 验方模板控件
│   ├── Herbs/                  # 药材管理控件
│   ├── Patients/               # 患者管理控件
│   ├── Prescriptions/          # 处方相关控件
│   ├── Users/                  # 用户管理控件
│   ├── SmartLoadingIndicator.xaml    # 智能加载指示器
│   ├── VirtualizedDataGrid.xaml      # 虚拟化数据表格
│   └── VirtualizedListView.xaml      # 虚拟化列表视图
├── Views/                      # 视图和页面
│   ├── Base/                   # 基础视图类
│   └── Dialogs/                # 对话框视图
├── Themes/                     # 主题和样式
│   ├── Design/                 # 设计系统
│   └── Controls/               # 控件样式
├── Converters/                 # 值转换器
├── Behaviors/                  # 行为类
├── Services/                   # 核心服务
└── Assets/                     # 静态资源
```

### 依赖注入架构
```csharp
// 服务注册模式 (依赖Desktop.Infrastructure)
public static class DesktopCoreExtensions
{
    public static IServiceCollection AddDesktopCore(this IServiceCollection services)
    {
        // 核心服务注册
        services.AddSingleton<IResourceManager, ResourceManager>();
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddScoped<IDialogService, DialogService>();
        
        // 控件服务注册
        services.AddTransient<SmartLoadingIndicatorViewModel>();
        services.AddTransient<ErrorNotificationViewModel>();
        
        return services;
    }
}
```

## 🎯 核心控件规范

### 1. 智能加载指示器 (SmartLoadingIndicator)
```xml
<!-- 智能加载控件 -->
<UserControl x:Class="LYBT.Desktop.Core.Controls.SmartLoadingIndicator"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <StackPanel Orientation="Vertical" 
                    HorizontalAlignment="Center" 
                    VerticalAlignment="Center"
                    Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
            
            <!-- 旋转指示器 -->
            <Border Width="40" Height="40" 
                    Background="{DynamicResource PrimaryBrush}" 
                    CornerRadius="20">
                <Border.RenderTransform>
                    <RotateTransform x:Name="LoadingRotation" CenterX="20" CenterY="20"/>
                </Border.RenderTransform>
                <Border.Triggers>
                    <EventTrigger RoutedEvent="Loaded">
                        <BeginStoryboard>
                            <Storyboard RepeatBehavior="Forever">
                                <DoubleAnimation Storyboard.TargetName="LoadingRotation"
                                               Storyboard.TargetProperty="Angle"
                                               From="0" To="360" Duration="0:0:2"/>
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Border.Triggers>
            </Border>
            
            <!-- 加载文本 -->
            <TextBlock Text="{Binding LoadingMessage, FallbackValue='正在加载...'}"
                       Margin="0,10,0,0"
                       HorizontalAlignment="Center"
                       Style="{DynamicResource CaptionTextStyle}"/>
                       
            <!-- 进度信息 -->
            <ProgressBar Value="{Binding Progress}"
                         Maximum="100"
                         Width="200"
                         Height="4"
                         Margin="0,10,0,0"
                         Visibility="{Binding ShowProgress, Converter={StaticResource BooleanToVisibilityConverter}}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

```csharp
// 智能加载指示器 ViewModel
public class SmartLoadingIndicatorViewModel : BindableBase
{
    private bool _isLoading;
    private string _loadingMessage = "正在加载...";
    private double _progress;
    private bool _showProgress;
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    public string LoadingMessage
    {
        get => _loadingMessage;
        set => SetProperty(ref _loadingMessage, value);
    }
    
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }
    
    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }
    
    // 显示加载状态
    public void ShowLoading(string message = "正在加载...", bool showProgress = false)
    {
        LoadingMessage = message;
        ShowProgress = showProgress;
        Progress = 0;
        IsLoading = true;
    }
    
    // 更新进度
    public void UpdateProgress(double progress, string message = null)
    {
        Progress = Math.Max(0, Math.Min(100, progress));
        if (!string.IsNullOrEmpty(message))
            LoadingMessage = message;
    }
    
    // 隐藏加载状态
    public void HideLoading()
    {
        IsLoading = false;
        Progress = 0;
    }
}
```

### 2. 虚拟化数据表格 (VirtualizedDataGrid)
```xml
<!-- 高性能虚拟化数据表格 -->
<UserControl x:Class="LYBT.Desktop.Core.Controls.VirtualizedDataGrid">
    <Grid>
        <DataGrid x:Name="MainDataGrid"
                  ItemsSource="{Binding ItemsSource}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  EnableRowVirtualization="True"
                  EnableColumnVirtualization="True"
                  VirtualizingPanel.IsVirtualizing="True"
                  VirtualizingPanel.VirtualizationMode="Recycling"
                  VirtualizingPanel.IsContainerVirtualizable="True"
                  ScrollViewer.CanContentScroll="True"
                  Style="{DynamicResource ModernDataGridStyle}">
            
            <!-- 列定义通过代码动态生成 -->
            
        </DataGrid>
        
        <!-- 分页控件 -->
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Right" 
                    VerticalAlignment="Bottom"
                    Margin="10"
                    Visibility="{Binding ShowPagination, Converter={StaticResource BooleanToVisibilityConverter}}">
            
            <Button Content="首页" 
                    Command="{Binding FirstPageCommand}"
                    IsEnabled="{Binding CanGoFirstPage}"/>
                    
            <Button Content="上一页" 
                    Command="{Binding PreviousPageCommand}"
                    IsEnabled="{Binding CanGoPreviousPage}"/>
                    
            <TextBlock Text="{Binding PageInfo}" 
                       VerticalAlignment="Center"
                       Margin="10,0"/>
                       
            <Button Content="下一页" 
                    Command="{Binding NextPageCommand}"
                    IsEnabled="{Binding CanGoNextPage}"/>
                    
            <Button Content="末页" 
                    Command="{Binding LastPageCommand}"
                    IsEnabled="{Binding CanGoLastPage}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

```csharp
// 虚拟化数据表格 ViewModel
public class VirtualizedDataGridViewModel : BindableBase
{
    private ObservableCollection<object> _itemsSource;
    private object _selectedItem;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount;
    private bool _showPagination = true;
    
    public ObservableCollection<object> ItemsSource
    {
        get => _itemsSource;
        set => SetProperty(ref _itemsSource, value);
    }
    
    public object SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }
    
    public string PageInfo => $"第 {CurrentPage} 页，共 {TotalPages} 页，总计 {TotalCount} 条记录";
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // 分页命令
    public DelegateCommand FirstPageCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand LastPageCommand { get; }
    
    public VirtualizedDataGridViewModel()
    {
        FirstPageCommand = new DelegateCommand(GoToFirstPage, CanGoFirstPage);
        PreviousPageCommand = new DelegateCommand(GoToPreviousPage, CanGoPreviousPage);
        NextPageCommand = new DelegateCommand(GoToNextPage, CanGoNextPage);
        LastPageCommand = new DelegateCommand(GoToLastPage, CanGoLastPage);
    }
    
    // 加载数据的抽象方法
    public virtual async Task LoadDataAsync()
    {
        // 由具体实现类重写
        await Task.CompletedTask;
    }
}
```

### 3. 错误通知控件 (ErrorNotificationControl)
```xml
<!-- 错误通知控件 -->
<UserControl x:Class="LYBT.Desktop.Core.Controls.ErrorHandling.ErrorNotificationControl">
    <Border Background="{DynamicResource ErrorBackgroundBrush}"
            BorderBrush="{DynamicResource ErrorBorderBrush}"
            BorderThickness="1"
            CornerRadius="4"
            Padding="12"
            Visibility="{Binding HasError, Converter={StaticResource BooleanToVisibilityConverter}}">
        
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <!-- 错误图标 -->
            <Path Grid.Column="0"
                  Data="{StaticResource ErrorIconGeometry}"
                  Fill="{DynamicResource ErrorForegroundBrush}"
                  Width="16" Height="16"
                  VerticalAlignment="Top"
                  Margin="0,0,8,0"/>
            
            <!-- 错误内容 -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="{Binding ErrorTitle}"
                           FontWeight="SemiBold"
                           Foreground="{DynamicResource ErrorForegroundBrush}"
                           Visibility="{Binding ErrorTitle, Converter={StaticResource StringToVisibilityConverter}}"/>
                           
                <TextBlock Text="{Binding ErrorMessage}"
                           TextWrapping="Wrap"
                           Foreground="{DynamicResource ErrorForegroundBrush}"
                           Margin="0,4,0,0"/>
                           
                <!-- 错误详情 (可展开) -->
                <Expander Header="错误详情"
                          Margin="0,8,0,0"
                          Visibility="{Binding ErrorDetails, Converter={StaticResource StringToVisibilityConverter}}">
                    <TextBlock Text="{Binding ErrorDetails}"
                               FontFamily="Consolas"
                               FontSize="11"
                               TextWrapping="Wrap"
                               Background="{DynamicResource CodeBackgroundBrush}"
                               Padding="8"
                               Margin="0,4,0,0"/>
                </Expander>
            </StackPanel>
            
            <!-- 关闭按钮 -->
            <Button Grid.Column="2"
                    Content="×"
                    Command="{Binding CloseCommand}"
                    Style="{DynamicResource CloseButtonStyle}"
                    VerticalAlignment="Top"/>
        </Grid>
    </Border>
</UserControl>
```

```csharp
// 错误通知 ViewModel
public class ErrorNotificationViewModel : BindableBase
{
    private bool _hasError;
    private string _errorTitle;
    private string _errorMessage;
    private string _errorDetails;
    
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }
    
    public string ErrorTitle
    {
        get => _errorTitle;
        set => SetProperty(ref _errorTitle, value);
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    
    public string ErrorDetails
    {
        get => _errorDetails;
        set => SetProperty(ref _errorDetails, value);
    }
    
    public DelegateCommand CloseCommand { get; }
    
    public ErrorNotificationViewModel()
    {
        CloseCommand = new DelegateCommand(ClearError);
    }
    
    // 显示错误
    public void ShowError(string message, string title = "错误", string details = null)
    {
        ErrorTitle = title;
        ErrorMessage = message;
        ErrorDetails = details;
        HasError = true;
    }
    
    // 显示异常
    public void ShowException(Exception exception, string title = "系统错误")
    {
        ErrorTitle = title;
        ErrorMessage = exception.Message;
        ErrorDetails = exception.ToString();
        HasError = true;
    }
    
    // 清除错误
    public void ClearError()
    {
        HasError = false;
        ErrorTitle = null;
        ErrorMessage = null;
        ErrorDetails = null;
    }
}
```

## 🎨 设计系统规范

### 1. 颜色系统
```xml
<!-- 主题颜色定义 Resources/Colors.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 主色调 -->
    <Color x:Key="PrimaryColor">#2E7D4A</Color>          <!-- 中医绿 -->
    <Color x:Key="PrimaryLightColor">#4CAF50</Color>     <!-- 浅绿 -->
    <Color x:Key="PrimaryDarkColor">#1B5E20</Color>      <!-- 深绿 -->
    
    <!-- 辅助色 -->
    <Color x:Key="SecondaryColor">#FF9800</Color>        <!-- 橙色 -->
    <Color x:Key="AccentColor">#2196F3</Color>           <!-- 蓝色 -->
    <Color x:Key="WarningColor">#FF9800</Color>          <!-- 警告橙 -->
    <Color x:Key="ErrorColor">#F44336</Color>            <!-- 错误红 -->
    <Color x:Key="SuccessColor">#4CAF50</Color>          <!-- 成功绿 -->
    
    <!-- 中性色 -->
    <Color x:Key="BackgroundColor">#FAFAFA</Color>       <!-- 背景色 -->
    <Color x:Key="SurfaceColor">#FFFFFF</Color>          <!-- 表面色 -->
    <Color x:Key="TextPrimaryColor">#212121</Color>      <!-- 主要文本 -->
    <Color x:Key="TextSecondaryColor">#757575</Color>    <!-- 次要文本 -->
    <Color x:Key="DividerColor">#E0E0E0</Color>          <!-- 分割线 -->
    
    <!-- 画刷定义 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="PrimaryLightBrush" Color="{StaticResource PrimaryLightColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>
    <SolidColorBrush x:Key="ErrorBackgroundBrush" Color="#FFEBEE"/>
    <SolidColorBrush x:Key="ErrorBorderBrush" Color="{StaticResource ErrorColor}"/>
    <SolidColorBrush x:Key="ErrorForegroundBrush" Color="#C62828"/>
    
</ResourceDictionary>
```

### 2. 字体系统
```xml
<!-- 字体样式定义 Resources/Typography.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 标题样式 -->
    <Style x:Key="TitleTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei UI"/>
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="Margin" Value="0,0,0,16"/>
    </Style>
    
    <!-- 子标题样式 -->
    <Style x:Key="SubtitleTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei UI"/>
        <Setter Property="FontSize" Value="18"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="Margin" Value="0,0,0,12"/>
    </Style>
    
    <!-- 正文样式 -->
    <Style x:Key="BodyTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei UI"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="FontWeight" Value="Normal"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="LineHeight" Value="20"/>
    </Style>
    
    <!-- 说明文字样式 -->
    <Style x:Key="CaptionTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei UI"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="FontWeight" Value="Normal"/>
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
    </Style>
    
</ResourceDictionary>
```

### 3. 控件样式
```xml
<!-- 按钮样式 Resources/ButtonStyles.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 主要按钮样式 -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                        VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryLightBrush}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryDarkBrush}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Background" Value="#CCCCCC"/>
                            <Setter Property="Foreground" Value="#999999"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    
    <!-- 次要按钮样式 -->
    <Style x:Key="SecondaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                        VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
                            <Setter Property="Foreground" Value="White"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    
</ResourceDictionary>
```

## 🔧 开发标准

### MVVM模式实现
```csharp
// 基础 ViewModel
public abstract class BaseViewModel : BindableBase
{
    protected ILogger _logger;
    protected IDialogService _dialogService;
    
    private bool _isBusy;
    private string _busyMessage = "正在处理...";
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }
    
    protected BaseViewModel(ILogger logger, IDialogService dialogService)
    {
        _logger = logger;
        _dialogService = dialogService;
    }
    
    // 显示错误消息
    protected virtual void ShowError(string message, string title = "错误")
    {
        _dialogService.ShowError(message, title);
    }
    
    // 显示确认对话框
    protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        return await _dialogService.ShowConfirmationAsync(message, title);
    }
    
    // 异步命令执行包装
    protected async Task ExecuteAsync(Func<Task> action, string busyMessage = null)
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(busyMessage))
                BusyMessage = busyMessage;
                
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行异步操作时发生错误");
            ShowError($"操作失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 控件开发标准
```csharp
// 自定义控件基类
public abstract class BaseUserControl : UserControl
{
    protected ILogger _logger;
    
    // 依赖属性示例
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(BaseUserControl),
            new PropertyMetadata(string.Empty, OnTitleChanged));
    
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseUserControl control)
            control.OnTitleChanged((string)e.OldValue, (string)e.NewValue);
    }
    
    protected virtual void OnTitleChanged(string oldValue, string newValue)
    {
        // 子类可重写
    }
    
    protected BaseUserControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    protected virtual void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 控件加载完成
    }
    
    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // 控件卸载，清理资源
    }
}
```

### 值转换器规范
```csharp
// 布尔到可见性转换器
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool result = invert ? !boolValue : boolValue;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;
            bool invert = parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            return invert ? !result : result;
        }
        
        return false;
    }
}

// 字符串到可见性转换器
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(value?.ToString());
        bool invert = parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        
        bool result = invert ? !hasValue : hasValue;
        return result ? Visibility.Visible : Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

## 🔗 模块集成接口

### 与 Desktop.Infrastructure 集成
```csharp
// 基础设施服务依赖
public interface IDialogService
{
    void ShowError(string message, string title = "错误");
    void ShowInformation(string message, string title = "信息");
    void ShowWarning(string message, string title = "警告");
    Task<bool> ShowConfirmationAsync(string message, string title = "确认");
    Task<string> ShowInputDialogAsync(string prompt, string title = "输入", string defaultValue = "");
}

public interface IThemeManager
{
    void SetTheme(string themeName);
    string CurrentTheme { get; }
    event EventHandler<string> ThemeChanged;
}

public interface IResourceManager
{
    T GetResource<T>(string key) where T : class;
    void SetResource(string key, object value);
    bool HasResource(string key);
}
```

### 与业务模块集成
```csharp
// 业务控件基类
public abstract class BusinessUserControlBase : BaseUserControl
{
    protected IEventAggregator _eventAggregator;
    
    protected BusinessUserControlBase()
    {
        // 通过容器解析依赖
        if (Application.Current is App app && app.Container != null)
        {
            _eventAggregator = app.Container.Resolve<IEventAggregator>();
            _logger = app.Container.Resolve<ILogger<BusinessUserControlBase>>();
        }
    }
    
    // 发布事件
    protected void PublishEvent<T>(T eventData) where T : PubSubEvent, new()
    {
        _eventAggregator?.GetEvent<T>().Publish();
    }
    
    // 订阅事件
    protected void SubscribeEvent<T>(Action callback) where T : PubSubEvent, new()
    {
        _eventAggregator?.GetEvent<T>().Subscribe(callback);
    }
}
```

## ⚙️ 配置管理

### 控件配置选项
```csharp
public class DesktopCoreOptions
{
    public const string SectionName = "DesktopCore";
    
    /// <summary>
    /// 虚拟化列表默认页大小
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;
    
    /// <summary>
    /// 启用虚拟化
    /// </summary>
    public bool EnableVirtualization { get; set; } = true;
    
    /// <summary>
    /// 加载动画持续时间(毫秒)
    /// </summary>
    public int LoadingAnimationDuration { get; set; } = 2000;
    
    /// <summary>
    /// 错误消息自动消失时间(秒)
    /// </summary>
    public int ErrorMessageTimeout { get; set; } = 10;
    
    /// <summary>
    /// 默认主题
    /// </summary>
    public string DefaultTheme { get; set; } = "Light";
    
    /// <summary>
    /// 启用主题切换
    /// </summary>
    public bool EnableThemeSwitch { get; set; } = true;
}
```

### 应用配置
```json
{
  "DesktopCore": {
    "DefaultPageSize": 50,
    "EnableVirtualization": true,
    "LoadingAnimationDuration": 2000,
    "ErrorMessageTimeout": 10,
    "DefaultTheme": "Light",
    "EnableThemeSwitch": true
  },
  "Logging": {
    "LogLevel": {
      "LYBT.Desktop.Core": "Information"
    }
  }
}
```

## 🧪 测试规范

### 控件单元测试
```csharp
[TestFixture]
public class SmartLoadingIndicatorTests
{
    private SmartLoadingIndicatorViewModel _viewModel;
    
    [SetUp]
    public void SetUp()
    {
        _viewModel = new SmartLoadingIndicatorViewModel();
    }
    
    [Test]
    public void ShowLoading_SetsPropertiesCorrectly()
    {
        // Arrange
        const string message = "正在加载数据...";
        const bool showProgress = true;
        
        // Act
        _viewModel.ShowLoading(message, showProgress);
        
        // Assert
        Assert.That(_viewModel.IsLoading, Is.True);
        Assert.That(_viewModel.LoadingMessage, Is.EqualTo(message));
        Assert.That(_viewModel.ShowProgress, Is.EqualTo(showProgress));
        Assert.That(_viewModel.Progress, Is.EqualTo(0));
    }
    
    [Test]
    public void UpdateProgress_UpdatesProgressCorrectly()
    {
        // Arrange
        _viewModel.ShowLoading("Loading...", true);
        
        // Act
        _viewModel.UpdateProgress(75, "75% 完成");
        
        // Assert
        Assert.That(_viewModel.Progress, Is.EqualTo(75));
        Assert.That(_viewModel.LoadingMessage, Is.EqualTo("75% 完成"));
    }
    
    [Test]
    public void UpdateProgress_ClampsValueCorrectly()
    {
        // Arrange
        _viewModel.ShowLoading("Loading...", true);
        
        // Act & Assert
        _viewModel.UpdateProgress(-10);
        Assert.That(_viewModel.Progress, Is.EqualTo(0));
        
        _viewModel.UpdateProgress(150);
        Assert.That(_viewModel.Progress, Is.EqualTo(100));
    }
}
```

### UI集成测试
```csharp
[TestFixture]
public class VirtualizedDataGridTests
{
    private TestApplication _app;
    private VirtualizedDataGrid _control;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _app = new TestApplication();
    }
    
    [SetUp]
    public void SetUp()
    {
        _control = new VirtualizedDataGrid();
        _control.Measure(new Size(800, 600));
        _control.Arrange(new Rect(0, 0, 800, 600));
    }
    
    [Test]
    public void DataGrid_WithLargeDataSet_EnablesVirtualization()
    {
        // Arrange
        var data = Enumerable.Range(1, 10000)
            .Select(i => new { Id = i, Name = $"Item {i}" })
            .ToList();
        
        // Act
        _control.DataContext = new VirtualizedDataGridViewModel
        {
            ItemsSource = new ObservableCollection<object>(data)
        };
        
        // Assert
        var dataGrid = _control.FindChild<DataGrid>("MainDataGrid");
        Assert.That(dataGrid.EnableRowVirtualization, Is.True);
        Assert.That(VirtualizingPanel.GetIsVirtualizing(dataGrid), Is.True);
    }
}
```

## 🚀 构建和部署

### 项目文件配置
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- 程序集信息 -->
    <AssemblyTitle>LYBT Desktop Core Controls</AssemblyTitle>
    <AssemblyDescription>凌隐宝堂桌面应用核心控件库</AssemblyDescription>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <FileVersion>1.0.0</FileVersion>
    
    <!-- 包信息 -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>LYBT.Desktop.Core</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Prism MVVM框架 -->
    <PackageReference Include="Prism.Core" Version="8.1.97" />
    <PackageReference Include="Prism.DryIoc" Version="8.1.97" />
    <PackageReference Include="Prism.Wpf" Version="8.1.97" />
    
    <!-- 基础框架 -->
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="FluentValidation" Version="12.0.0" />
    
    <!-- 配置管理 -->
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.7" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
    <PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="9.0.7" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.7" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    
    <!-- 功能库 -->
    <PackageReference Include="NPOI" Version="2.7.4" />  <!-- Excel处理 -->
    <PackageReference Include="Polly" Version="8.5.1" />  <!-- HTTP重试策略 -->
    <PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />  <!-- HTTP集成 -->
    <PackageReference Include="System.Reactive" Version="6.0.0" />  <!-- 响应式编程 -->
    <PackageReference Include="AutoMapper" Version="15.0.1" />  <!-- 对象映射 -->
    
    <!-- 数据访问 -->
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.0" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Resource Include="Assets\**\*" />
    <Page Include="Themes\**\*.xaml" />
  </ItemGroup>

</Project>
```

### 构建脚本
```bash
# 构建 Desktop.Core 项目
echo "构建 Desktop.Core 项目..."
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Core.csproj --configuration Release --verbosity minimal

# 检查构建结果
if [ $? -eq 0 ]; then
    echo "✅ Desktop.Core 构建成功"
else
    echo "❌ Desktop.Core 构建失败"
    exit 1
fi
```

## 📚 相关文档

### 架构文档
- [前端架构设计标准](../../architecture/frontend-architecture-standards.md)
- [MVVM模式实现指南](../../architecture/mvvm-implementation-guide.md)
- [Prism模块化开发指南](../../architecture/prism-modular-development.md)

### 设计文档  
- [UI设计系统规范](../../design/ui-design-system-standards.md)
- [控件设计指南](../../design/control-design-guidelines.md)
- [主题系统设计](../../design/theme-system-design.md)

### 开发指南
- [WPF控件开发规范](../../development/wpf-control-development-standards.md)
- [XAML编码规范](../../development/xaml-coding-standards.md)
- [前端测试指南](../../testing/frontend-testing-guide.md)

### 用户文档
- [控件使用手册](../../guides/control-usage-manual.md)
- [主题切换指南](../../guides/theme-switching-guide.md)
- [自定义样式指南](../../guides/custom-styling-guide.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 前端开发组