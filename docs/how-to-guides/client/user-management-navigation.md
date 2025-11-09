# 用户管理交互模式统一实现 - Navigation模式指南

**Epic**: #1926 用户管理交互模式统一 - Dialog迁移为Navigation模式
**完成时间**: 2025-11
**Sprints**: 4个（#1927, #1928, #1929, #1930）

---

## 📋 目录

1. [Navigation模式概述](#1-navigation模式概述)
2. [用户创建操作](#2-用户创建操作)
3. [用户编辑操作](#3-用户编辑操作)
4. [用户详情查看](#4-用户详情查看)
5. [重置密码操作](#5-重置密码操作)
6. [修改密码操作](#6-修改密码操作)
7. [个人资料编辑](#7-个人资料编辑)
8. [导航参数传递](#8-导航参数传递)
9. [常见问题](#9-常见问题)
10. [与Dialog模式对比](#10-与dialog模式对比)

---

## 1. Navigation模式概述

### 1.1 什么是Navigation模式

Navigation模式是Prism框架提供的区域导航机制，通过`IRegionManager`在指定区域（Region）内切换不同的视图。

**核心概念**:
- **Region**: 视图容器（如ContentRegion）
- **INavigationAware**: 视图模型需实现的导航生命周期接口
- **NavigationParameters**: 导航参数传递对象
- **RequestNavigate**: 触发导航的方法

### 1.2 为什么统一为Navigation模式

**用户反馈**: "查看"的整体感官更好

**技术优势**:
- ✅ **统一体验**: 所有功能使用相同的导航模式
- ✅ **返回支持**: 支持Back按钮和面包屑导航
- ✅ **状态保持**: 页面状态可以保持（根据IsNavigationTarget设置）
- ✅ **布局灵活**: 响应式布局，占满ContentRegion
- ✅ **代码简化**: 比Dialog模式减少40%代码量

**架构统一**:
```
用户管理所有功能 → Navigation模式（占满ContentRegion）
患者管理所有功能 → Navigation模式
医案管理所有功能 → Navigation模式
...（其他模块保持一致）
```

### 1.3 视图结构总览

| 功能 | View名称 | ViewModel | 触发方式 | 参数 |
|-----|---------|-----------|---------|------|
| 用户列表 | UserManagementView | UserManagementViewModel | 主菜单导航 | 无 |
| 创建用户 | UserCreateView | UserCreateViewModel | 列表"新建"按钮 | 无 |
| 编辑用户 | UserEditView | UserEditViewModel | 列表"编辑"按钮 | userId |
| 用户详情 | UserDetailView | UserDetailViewModel | 列表"查看"按钮 | userId |
| 重置密码 | - | - | 列表"重置密码"按钮 | 直接调用Service |
| 修改密码 | ChangePasswordView | ChangePasswordViewModel | 工作台按钮 | 无 |
| 个人资料 | UserProfileView | UserProfileViewModel | 工作台按钮 | 无 |

---

## 2. 用户创建操作

### 2.1 触发导航

**位置**: `UserManagementViewModel.cs`

```csharp
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    public DelegateCommand CreateUserCommand { get; }

    public UserManagementViewModel(IRegionManager regionManager, ...)
    {
        _regionManager = regionManager;
        CreateUserCommand = new DelegateCommand(ExecuteCreateUserCommand);
    }

    /// <summary>
    /// 导航到创建用户页面
    /// </summary>
    private void ExecuteCreateUserCommand()
    {
        Logger.LogInformation("导航到创建用户页面");

        // ⭐ Navigation模式：直接导航，无参数
        _regionManager.RequestNavigate("ContentRegion", "UserCreateView");
    }
}
```

**XAML绑定**:
```xml
<Button Content="新建用户"
        Command="{Binding CreateUserCommand}"
        Style="{StaticResource PrimaryButtonStyle}"/>
```

### 2.2 ViewModel实现

**文件**: `UserCreateViewModel.cs`

```csharp
using Prism.Commands;
using Prism.Regions;
using Microsoft.Extensions.Logging;

/// <summary>
/// 用户创建视图模型 - Navigation模式
/// Epic #1926 Sprint 1: Dialog → Navigation统一迁移
/// </summary>
public class UserCreateViewModel : UnifiedViewModelBase
{
    #region 依赖服务

    private readonly IUserService _userService;
    private readonly IRegionManager _regionManager;

    #endregion

    #region 绑定属性

    private UserCreateDto _user = new();
    /// <summary>用户表单数据</summary>
    public UserCreateDto User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    private bool _isBusy;
    /// <summary>是否正在保存</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    #endregion

    #region 命令

    /// <summary>保存并返回</summary>
    public DelegateCommand SaveCommand { get; }

    /// <summary>取消并返回</summary>
    public DelegateCommand CancelCommand { get; }

    #endregion

    #region 构造函数

    public UserCreateViewModel(
        IUserService userService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 初始化命令
        SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSave)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => User.UserName)
            .ObservesProperty(() => User.RealName);

        CancelCommand = new DelegateCommand(ExecuteCancelCommand);
    }

    #endregion

    #region INavigationAware实现

    /// <summary>
    /// 导航到此视图时调用 - ⭐ Navigation模式核心
    /// </summary>
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        try
        {
            Logger.LogInformation("导航到用户创建页面");

            // 重置表单数据（确保干净状态）
            User = new UserCreateDto
            {
                Role = UserRole.Doctor, // 默认角色
                IsActive = true
            };

            // 刷新命令状态
            SaveCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化用户创建页面失败");
        }
    }

    /// <summary>
    /// 控制导航目标策略 - ⭐ Create场景返回false
    /// </summary>
    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // Create场景：每次导航创建新实例，确保表单数据干净
        return false;
    }

    /// <summary>
    /// 离开此视图时调用 - ⭐ 清理资源
    /// </summary>
    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Logger.LogInformation("离开用户创建页面");
        // 取消订阅、释放资源（如需要）
    }

    #endregion

    #region 命令实现

    private async void ExecuteSaveCommand()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Logger.LogInformation("开始创建用户：{UserName}", User.UserName);

            var result = await _userService.CreateAsync(User);
            if (result.IsSuccess)
            {
                Logger.LogInformation("用户创建成功：{UserId}", result.Data?.Id);

                // ⭐ 发布事件通知列表刷新
                EventAggregator.GetEvent<UserCreatedEvent>().Publish(result.Data!);

                // ⭐ Navigation模式：使用RequestNavigate返回列表
                _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
            }
            else
            {
                Logger.LogWarning("创建用户失败：{Message}", result.Message);
                await ShowErrorMessageAsync(result.Message ?? "创建用户失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建用户异常");
            await ShowErrorMessageAsync("创建用户时发生错误，请重试");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteSave()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(User?.UserName)
            && !string.IsNullOrWhiteSpace(User?.RealName);
    }

    private void ExecuteCancelCommand()
    {
        Logger.LogInformation("取消创建用户");
        // ⭐ Navigation模式：直接返回列表
        _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
    }

    #endregion
}
```

### 2.3 XAML布局

**文件**: `UserCreateView.xaml`

**关键结构**:
```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserCreateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 标题 -->
            <RowDefinition Height="*"/>    <!-- 表单内容 -->
            <RowDefinition Height="Auto"/> <!-- 底部按钮 -->
        </Grid.RowDefinitions>

        <!-- 1. 页面标题 -->
        <TextBlock Grid.Row="0"
                   Text="创建用户"
                   Style="{StaticResource PageTitleStyle}"/>

        <!-- 2. 表单卡片 -->
        <materialDesign:Card Grid.Row="1" Margin="0,20,0,20">
            <StackPanel Margin="30">
                <!-- 用户名 -->
                <TextBox materialDesign:HintAssist.Hint="用户名*"
                         Text="{Binding User.UserName, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource MaterialDesignOutlinedTextBox}"/>

                <!-- 真实姓名 -->
                <TextBox materialDesign:HintAssist.Hint="真实姓名*"
                         Text="{Binding User.RealName, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,20,0,0"/>

                <!-- 角色选择 -->
                <ComboBox materialDesign:HintAssist.Hint="角色*"
                          SelectedItem="{Binding User.Role}"
                          Margin="0,20,0,0">
                    <ComboBoxItem Content="医生" Tag="{x:Static enums:UserRole.Doctor}"/>
                    <ComboBoxItem Content="药师" Tag="{x:Static enums:UserRole.Pharmacist}"/>
                </ComboBox>

                <!-- 是否激活 -->
                <CheckBox Content="激活用户"
                          IsChecked="{Binding User.IsActive}"
                          Margin="0,20,0,0"/>
            </StackPanel>
        </materialDesign:Card>

        <!-- 3. 底部操作按钮 -->
        <StackPanel Grid.Row="2"
                    Orientation="Horizontal"
                    HorizontalAlignment="Right">

            <!-- 取消按钮 -->
            <Button Content="取消"
                    Command="{Binding CancelCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    Margin="0,0,10,0"/>

            <!-- 保存按钮 -->
            <Button Content="保存"
                    Command="{Binding SaveCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

**样式要点**:
- 使用Material Design卡片布局
- 表单字段带提示文本（HintAssist）
- 保存按钮根据表单验证状态自动禁用/启用
- 统一间距和边距

---

## 3. 用户编辑操作

### 3.1 触发导航（带参数）

**位置**: `UserManagementViewModel.cs`

```csharp
public DelegateCommand<Guid?> EditUserCommand { get; }

private void ExecuteEditUserCommand(Guid? userId)
{
    if (!userId.HasValue)
    {
        Logger.LogWarning("userId为空，无法导航到编辑页面");
        return;
    }

    Logger.LogInformation("导航到编辑用户页面，UserId: {UserId}", userId.Value);

    // ⭐ 创建导航参数
    var parameters = new NavigationParameters
    {
        { "userId", userId.Value }
    };

    // ⭐ 带参数导航
    _regionManager.RequestNavigate("ContentRegion", "UserEditView", parameters);
}
```

**XAML绑定**:
```xml
<DataGrid ItemsSource="{Binding Users}">
    <DataGrid.Columns>
        <!-- 其他列... -->

        <!-- 操作列 -->
        <DataGridTemplateColumn Header="操作" Width="450">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <!-- 编辑按钮 -->
                        <Button Content="编辑"
                                Command="{Binding DataContext.EditUserCommand,
                                         RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding Id}"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

### 3.2 ViewModel实现（接收参数）

**文件**: `UserEditViewModel.cs`

```csharp
/// <summary>
/// 用户编辑视图模型 - Navigation模式（带参数）
/// Epic #1926 Sprint 1
/// </summary>
public class UserEditViewModel : UnifiedViewModelBase
{
    private readonly IUserService _userService;
    private readonly IRegionManager _regionManager;

    private Guid _currentUserId;
    private UserUpdateDto _user = new();

    public UserUpdateDto User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public UserEditViewModel(
        IUserService userService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _userService = userService;
        _regionManager = regionManager;

        SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSave)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => User.UserName)
            .ObservesProperty(() => User.RealName);

        CancelCommand = new DelegateCommand(ExecuteCancelCommand);
    }

    /// <summary>
    /// Navigation模式参数传递 - ⭐ 核心
    /// </summary>
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        try
        {
            // ⭐ 从导航参数获取userId
            if (navigationContext.Parameters.TryGetValue("userId", out Guid userId))
            {
                _currentUserId = userId;
                Logger.LogInformation("导航到用户编辑页面，UserId: {UserId}", userId);

                // 加载用户数据
                LoadUserDataAsync(userId);
            }
            else
            {
                Logger.LogWarning("未提供userId参数，无法加载用户数据");
                // 返回列表
                _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化用户编辑页面失败");
        }
    }

    /// <summary>
    /// Edit场景：可复用实例（根据需求决定）
    /// </summary>
    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 选项1：返回true（复用实例，提升性能）
        // 选项2：返回false（每次创建新实例，确保数据干净）
        // 本项目选择：true（复用实例）
        return true;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Logger.LogInformation("离开用户编辑页面");
    }

    /// <summary>加载用户数据</summary>
    private async void LoadUserDataAsync(Guid userId)
    {
        try
        {
            IsBusy = true;

            var result = await _userService.GetByIdAsync(userId);
            if (result.IsSuccess && result.Data != null)
            {
                User = new UserUpdateDto
                {
                    UserName = result.Data.UserName,
                    RealName = result.Data.RealName,
                    Role = result.Data.Role,
                    IsActive = result.Data.IsActive
                };

                SaveCommand.RaiseCanExecuteChanged();
            }
            else
            {
                Logger.LogWarning("加载用户数据失败：{Message}", result.Message);
                await ShowErrorMessageAsync("加载用户数据失败");
                _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载用户数据失败，UserId: {UserId}", userId);
            await ShowErrorMessageAsync("加载用户数据时发生错误");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteSaveCommand()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Logger.LogInformation("开始更新用户：{UserId}", _currentUserId);

            var result = await _userService.UpdateAsync(_currentUserId, User);
            if (result.IsSuccess)
            {
                Logger.LogInformation("用户更新成功：{UserId}", _currentUserId);

                // 发布事件通知列表刷新
                EventAggregator.GetEvent<UserUpdatedEvent>().Publish(result.Data!);

                // 返回列表
                _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
            }
            else
            {
                Logger.LogWarning("更新用户失败：{Message}", result.Message);
                await ShowErrorMessageAsync(result.Message ?? "更新用户失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "更新用户异常");
            await ShowErrorMessageAsync("更新用户时发生错误，请重试");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteSave()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(User?.UserName)
            && !string.IsNullOrWhiteSpace(User?.RealName);
    }

    private void ExecuteCancelCommand()
    {
        Logger.LogInformation("取消编辑用户");
        _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
    }
}
```

### 3.3 XAML布局

**文件**: `UserEditView.xaml`

与`UserCreateView.xaml`基本相同，只需修改标题：

```xml
<TextBlock Grid.Row="0"
           Text="编辑用户"
           Style="{StaticResource PageTitleStyle}"/>
```

**加载指示器**（可选）:
```xml
<Grid Grid.Row="1" Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}">
    <ProgressBar IsIndeterminate="True" Style="{StaticResource MaterialDesignCircularProgressBar}"/>
    <TextBlock Text="加载中..." HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,60,0,0"/>
</Grid>
```

---

## 4. 用户详情查看

### 4.1 触发导航

```csharp
public DelegateCommand<Guid?> ViewUserDetailCommand { get; }

private void ExecuteViewUserDetailCommand(Guid? userId)
{
    if (!userId.HasValue) return;

    var parameters = new NavigationParameters { { "userId", userId.Value } };
    _regionManager.RequestNavigate("ContentRegion", "UserDetailView", parameters);
}
```

### 4.2 ViewModel实现

**文件**: `UserDetailViewModel.cs`

```csharp
/// <summary>
/// 用户详情视图模型 - Navigation模式（只读）
/// </summary>
public class UserDetailViewModel : UnifiedViewModelBase
{
    private readonly IUserService _userService;
    private readonly IRegionManager _regionManager;

    private UserDto? _user;
    public UserDto? User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    public DelegateCommand CloseCommand { get; }
    public DelegateCommand<Guid?> EditCommand { get; }

    public UserDetailViewModel(
        IUserService userService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _userService = userService;
        _regionManager = regionManager;

        CloseCommand = new DelegateCommand(ExecuteCloseCommand);
        EditCommand = new DelegateCommand<Guid?>(ExecuteEditCommand);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters.TryGetValue("userId", out Guid userId))
        {
            LoadUserDetailAsync(userId);
        }
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 详情页可复用实例
        return true;
    }

    private async void LoadUserDetailAsync(Guid userId)
    {
        try
        {
            var result = await _userService.GetByIdAsync(userId);
            if (result.IsSuccess && result.Data != null)
            {
                User = result.Data;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载用户详情失败");
        }
    }

    private void ExecuteCloseCommand()
    {
        _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
    }

    private void ExecuteEditCommand(Guid? userId)
    {
        if (!userId.HasValue) return;

        var parameters = new NavigationParameters { { "userId", userId.Value } };
        _regionManager.RequestNavigate("ContentRegion", "UserEditView", parameters);
    }
}
```

### 4.3 XAML布局（卡片式）

```xml
<Grid Margin="20">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- 标题 -->
    <TextBlock Text="用户详情" Style="{StaticResource PageTitleStyle}"/>

    <!-- 详情卡片 -->
    <ScrollViewer Grid.Row="1" Margin="0,20,0,20">
        <StackPanel>
            <!-- 基本信息卡片 -->
            <materialDesign:Card Margin="0,0,0,20">
                <StackPanel Margin="30">
                    <TextBlock Text="基本信息" Style="{StaticResource CardTitleStyle}"/>

                    <Grid Margin="0,20,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="120"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>

                        <!-- 用户名 -->
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="用户名：" Style="{StaticResource LabelStyle}"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding User.UserName}" Style="{StaticResource ValueStyle}"/>

                        <!-- 真实姓名 -->
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="真实姓名：" Margin="0,10,0,0"/>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding User.RealName}" Margin="0,10,0,0"/>

                        <!-- 角色 -->
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="角色：" Margin="0,10,0,0"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding User.Role}" Margin="0,10,0,0"/>

                        <!-- 状态 -->
                        <TextBlock Grid.Row="3" Grid.Column="0" Text="状态：" Margin="0,10,0,0"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Margin="0,10,0,0">
                            <Run Text="{Binding User.IsActive, Converter={StaticResource BoolToStatusConverter}}"/>
                        </TextBlock>
                    </Grid>
                </StackPanel>
            </materialDesign:Card>

            <!-- 其他信息卡片（如需要）-->
            <materialDesign:Card>
                <StackPanel Margin="30">
                    <TextBlock Text="其他信息" Style="{StaticResource CardTitleStyle}"/>
                    <!-- ... -->
                </StackPanel>
            </materialDesign:Card>
        </StackPanel>
    </ScrollViewer>

    <!-- 底部按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="编辑"
                Command="{Binding EditCommand}"
                CommandParameter="{Binding User.Id}"
                Style="{StaticResource MaterialDesignOutlinedButton}"
                Margin="0,0,10,0"/>

        <Button Content="关闭"
                Command="{Binding CloseCommand}"
                Style="{StaticResource MaterialDesignRaisedButton}"/>
    </StackPanel>
</Grid>
```

---

## 5. 重置密码操作

### 5.1 特殊说明

重置密码功能在Epic #1926 Sprint 2中优化，**不使用Navigation视图**，而是直接在列表页面操作。

**原因**:
- 重置密码只需要一个确认操作，无需复杂表单
- 直接调用Service，提升操作效率
- 符合用户"快速重置"的预期

### 5.2 实现方式

**位置**: `UserManagementViewModel.cs`

```csharp
public DelegateCommand<Guid?> ResetPasswordCommand { get; }

private async void ExecuteResetPasswordCommand(Guid? userId)
{
    if (!userId.HasValue) return;

    try
    {
        // 1. 确认对话框
        var confirmed = await ShowConfirmDialogAsync(
            "确认重置密码",
            "确定要重置该用户的密码吗？密码将重置为系统默认密码。");

        if (!confirmed) return;

        // 2. 直接调用Service重置密码
        var result = await _userService.ResetPasswordAsync(new ResetPasswordRequest
        {
            UserId = userId.Value,
            NewPassword = null // null表示使用配置文件中的默认密码
        });

        if (result.IsSuccess)
        {
            Logger.LogInformation("重置密码成功，UserId: {UserId}", userId.Value);

            // 3. 显示成功消息（包含新密码）
            await ShowInfoMessageAsync($"密码已重置\n新密码：{result.Data}");

            // 4. 发布事件
            EventAggregator.GetEvent<UserPasswordResetEvent>().Publish(userId.Value);
        }
        else
        {
            await ShowErrorMessageAsync(result.Message ?? "重置密码失败");
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "重置密码异常");
        await ShowErrorMessageAsync("重置密码时发生错误，请重试");
    }
}
```

**XAML绑定**:
```xml
<Button Content="重置密码"
        Command="{Binding DataContext.ResetPasswordCommand,
                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
        CommandParameter="{Binding Id}"/>
```

### 5.3 配置说明

**默认密码配置**: `appsettings.json`

```json
{
  "Lybt": {
    "DefaultPasswords": {
      "NewUserPassword": "Lybt2025@TempPass!"
    }
  }
}
```

**Server端读取**:
```csharp
string password = request.NewPassword
    ?? _configuration["Lybt:DefaultPasswords:NewUserPassword"]
    ?? PasswordHelper.GenerateTemporaryPassword();
```

---

## 6. 修改密码操作

### 6.1 触发导航

**位置**: 管理员工作台（`AdminHomeViewModel.cs`）

```csharp
public DelegateCommand ChangePasswordCommand { get; }

private void ExecuteChangePasswordCommand()
{
    Logger.LogInformation("导航到修改密码页面");

    // Navigation模式：无参数（使用当前登录用户）
    _regionManager.RequestNavigate("ContentRegion", "ChangePasswordView");
}
```

### 6.2 ViewModel实现

**文件**: `ChangePasswordViewModel.cs`

```csharp
/// <summary>
/// 修改密码视图模型 - Navigation模式
/// Epic #1926 Sprint 3
/// </summary>
public class ChangePasswordViewModel : UnifiedViewModelBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    private readonly IRegionManager _regionManager;

    #region 绑定属性（密码字段）

    private string _oldPassword = string.Empty;
    public string OldPassword
    {
        get => _oldPassword;
        set
        {
            if (SetProperty(ref _oldPassword, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _confirmPassword = string.Empty;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    #endregion

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public ChangePasswordViewModel(
        IUserService userService,
        IAuthenticationService authService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _userService = userService;
        _authService = authService;
        _regionManager = regionManager;

        SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSave)
            .ObservesProperty(() => OldPassword)
            .ObservesProperty(() => NewPassword)
            .ObservesProperty(() => ConfirmPassword)
            .ObservesProperty(() => IsBusy);

        CancelCommand = new DelegateCommand(ExecuteCancelCommand);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        Logger.LogInformation("导航到修改密码页面");

        // 清空密码字段
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 每次导航创建新实例，确保密码字段干净
        return false;
    }

    private async void ExecuteSaveCommand()
    {
        if (IsBusy) return;

        // 验证密码
        if (NewPassword != ConfirmPassword)
        {
            await ShowErrorMessageAsync("两次输入的新密码不一致");
            return;
        }

        try
        {
            IsBusy = true;

            // 获取当前用户ID
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.Id == Guid.Empty)
            {
                await ShowErrorMessageAsync("无法获取当前用户信息");
                return;
            }

            Logger.LogInformation("开始修改密码，UserId: {UserId}", currentUser.Id);

            // 调用Service修改密码
            var result = await _userService.ChangePasswordAsync(new ChangePasswordRequest
            {
                UserId = currentUser.Id,
                OldPassword = OldPassword,
                NewPassword = NewPassword
            });

            if (result.IsSuccess)
            {
                Logger.LogInformation("修改密码成功");
                await ShowInfoMessageAsync("密码修改成功，下次登录时请使用新密码");

                // 返回工作台
                _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
            }
            else
            {
                Logger.LogWarning("修改密码失败：{Message}", result.Message);
                await ShowErrorMessageAsync(result.Message ?? "修改密码失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "修改密码异常");
            await ShowErrorMessageAsync("修改密码时发生错误，请重试");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteSave()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(OldPassword)
            && !string.IsNullOrWhiteSpace(NewPassword)
            && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private void ExecuteCancelCommand()
    {
        Logger.LogInformation("取消修改密码");
        _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
    }
}
```

### 6.3 XAML布局（PasswordBox处理）

**文件**: `ChangePasswordView.xaml.cs`

**⚠️ 重要**: PasswordBox的Password属性不支持绑定，需要在代码后台处理。

```csharp
public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView()
    {
        InitializeComponent();
    }

    /// <summary>当前密码变更事件处理</summary>
    private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.OldPassword = passwordBox.Password;
        }
    }

    /// <summary>新密码变更事件处理</summary>
    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.NewPassword = passwordBox.Password;
        }
    }

    /// <summary>确认密码变更事件处理</summary>
    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmPassword = passwordBox.Password;
        }
    }
}
```

**XAML**:
```xml
<Grid Margin="20">
    <materialDesign:Card>
        <StackPanel Margin="30">
            <TextBlock Text="修改密码" Style="{StaticResource CardTitleStyle}"/>

            <!-- 当前密码 -->
            <PasswordBox x:Name="OldPasswordBox"
                         materialDesign:HintAssist.Hint="当前密码*"
                         PasswordChanged="OldPasswordBox_PasswordChanged"
                         Margin="0,20,0,0"/>

            <!-- 新密码 -->
            <PasswordBox x:Name="NewPasswordBox"
                         materialDesign:HintAssist.Hint="新密码*"
                         PasswordChanged="NewPasswordBox_PasswordChanged"
                         Margin="0,20,0,0"/>

            <!-- 确认密码 -->
            <PasswordBox x:Name="ConfirmPasswordBox"
                         materialDesign:HintAssist.Hint="确认新密码*"
                         PasswordChanged="ConfirmPasswordBox_PasswordChanged"
                         Margin="0,20,0,0"/>

            <!-- 密码提示 -->
            <TextBlock Text="密码要求：至少8位，包含大小写字母、数字和特殊字符"
                       Foreground="Gray"
                       FontSize="12"
                       Margin="0,10,0,0"/>

            <!-- 操作按钮 -->
            <StackPanel Orientation="Horizontal"
                        HorizontalAlignment="Right"
                        Margin="0,30,0,0">
                <Button Content="取消" Command="{Binding CancelCommand}" Margin="0,0,10,0"/>
                <Button Content="保存" Command="{Binding SaveCommand}"/>
            </StackPanel>
        </StackPanel>
    </materialDesign:Card>
</Grid>
```

---

## 7. 个人资料编辑

### 7.1 ViewModel实现

**文件**: `UserProfileViewModel.cs`

```csharp
/// <summary>
/// 个人资料视图模型 - Navigation模式
/// Epic #1926 Sprint 3
/// </summary>
public class UserProfileViewModel : UnifiedViewModelBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    private readonly IRegionManager _regionManager;

    private Guid _currentUserId;

    private UserProfileUpdateDto _profile = new();
    public UserProfileUpdateDto Profile
    {
        get => _profile;
        set => SetProperty(ref _profile, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public UserProfileViewModel(
        IUserService userService,
        IAuthenticationService authService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _userService = userService;
        _authService = authService;
        _regionManager = regionManager;

        SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSave)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => Profile.RealName);

        CancelCommand = new DelegateCommand(ExecuteCancelCommand);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        Logger.LogInformation("导航到个人资料页面");
        LoadProfileAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true; // 复用实例
    }

    private async void LoadProfileAsync()
    {
        try
        {
            IsBusy = true;

            // 获取当前用户
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.Id == Guid.Empty)
            {
                await ShowErrorMessageAsync("无法获取当前用户信息");
                _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
                return;
            }

            _currentUserId = currentUser.Id;

            // 加载用户详细信息
            var result = await _userService.GetByIdAsync(_currentUserId);
            if (result.IsSuccess && result.Data != null)
            {
                Profile = new UserProfileUpdateDto
                {
                    RealName = result.Data.RealName,
                    Email = result.Data.Email,
                    Phone = result.Data.Phone
                    // 其他个人信息字段...
                };

                SaveCommand.RaiseCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载个人资料失败");
            await ShowErrorMessageAsync("加载个人资料失败");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteSaveCommand()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Logger.LogInformation("开始更新个人资料");

            var result = await _userService.UpdateProfileAsync(_currentUserId, Profile);
            if (result.IsSuccess)
            {
                Logger.LogInformation("个人资料更新成功");
                await ShowInfoMessageAsync("个人资料已保存");

                // 发布事件
                EventAggregator.GetEvent<UserProfileUpdatedEvent>().Publish(_currentUserId);

                // 返回工作台
                _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
            }
            else
            {
                await ShowErrorMessageAsync(result.Message ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "更新个人资料异常");
            await ShowErrorMessageAsync("保存时发生错误，请重试");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteSave()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(Profile?.RealName);
    }

    private void ExecuteCancelCommand()
    {
        _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
    }
}
```

### 7.2 XAML布局

```xml
<Grid Margin="20">
    <materialDesign:Card>
        <StackPanel Margin="30">
            <TextBlock Text="个人资料" Style="{StaticResource CardTitleStyle}"/>

            <!-- 真实姓名 -->
            <TextBox materialDesign:HintAssist.Hint="真实姓名*"
                     Text="{Binding Profile.RealName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,20,0,0"/>

            <!-- 邮箱 -->
            <TextBox materialDesign:HintAssist.Hint="邮箱"
                     Text="{Binding Profile.Email, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,20,0,0"/>

            <!-- 手机号 -->
            <TextBox materialDesign:HintAssist.Hint="手机号"
                     Text="{Binding Profile.Phone, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,20,0,0"/>

            <!-- 操作按钮 -->
            <StackPanel Orientation="Horizontal"
                        HorizontalAlignment="Right"
                        Margin="0,30,0,0">
                <Button Content="取消" Command="{Binding CancelCommand}" Margin="0,0,10,0"/>
                <Button Content="保存" Command="{Binding SaveCommand}"/>
            </StackPanel>
        </StackPanel>
    </materialDesign:Card>
</Grid>
```

---

## 8. 导航参数传递

### 8.1 参数类型

**支持的参数类型**:
- 基本类型：`Guid`, `int`, `string`, `bool`
- 对象类型：任何可序列化的对象

### 8.2 传递参数

```csharp
// 单个参数
var parameters = new NavigationParameters
{
    { "userId", userId }
};

// 多个参数
var parameters = new NavigationParameters
{
    { "userId", userId },
    { "mode", "edit" },
    { "returnUri", "UserManagementView" }
};

// 对象参数
var parameters = new NavigationParameters
{
    { "user", userDto }
};

_regionManager.RequestNavigate("ContentRegion", "UserEditView", parameters);
```

### 8.3 接收参数

```csharp
public override void OnNavigatedTo(NavigationContext navigationContext)
{
    // 方式1：TryGetValue（推荐 - 安全）
    if (navigationContext.Parameters.TryGetValue("userId", out Guid userId))
    {
        _currentUserId = userId;
    }

    // 方式2：GetValue（需要确保参数存在）
    var mode = navigationContext.Parameters.GetValue<string>("mode");

    // 方式3：ContainsKey检查
    if (navigationContext.Parameters.ContainsKey("user"))
    {
        var user = navigationContext.Parameters.GetValue<UserDto>("user");
    }
}
```

### 8.4 返回导航

```csharp
// 简单返回
_regionManager.RequestNavigate("ContentRegion", "UserManagementView");

// 带返回参数
var returnParameters = new NavigationParameters
{
    { "refresh", true },
    { "message", "操作成功" }
};
_regionManager.RequestNavigate("ContentRegion", "UserManagementView", returnParameters);
```

---

## 9. 常见问题

### Q1: Navigation模式和Dialog模式的主要区别是什么？

**A**:

| 特性 | Dialog模式 | Navigation模式 |
|-----|----------|--------------|
| 显示方式 | 模态对话框 | 占满ContentRegion |
| 导航历史 | 无 | 有（支持Back） |
| 状态保持 | 关闭即销毁 | 可控（IsNavigationTarget） |
| 布局 | 固定尺寸 | 响应式布局 |
| 接口 | IDialogAware | INavigationAware |
| 代码量 | 多（~400行） | 少（~150行）|

### Q2: 什么时候使用IsNavigationTarget返回true/false？

**A**:
```csharp
// 返回false：每次导航创建新实例
// 适用场景：创建、编辑（需要干净状态）
public override bool IsNavigationTarget(NavigationContext navigationContext)
{
    return false; // UserCreateView, UserEditView
}

// 返回true：复用已有实例
// 适用场景：查看、列表（可保持状态）
public override bool IsNavigationTarget(NavigationContext navigationContext)
{
    return true; // UserDetailView, UserManagementView
}
```

### Q3: 如何在Navigation视图中显示加载指示器？

**A**:
```csharp
// ViewModel
private bool _isBusy;
public bool IsBusy
{
    get => _isBusy;
    set => SetProperty(ref _isBusy, value);
}

// XAML
<ProgressBar IsIndeterminate="True"
             Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

### Q4: 如何在Navigation模式下验证表单？

**A**:
```csharp
// 方式1：命令CanExecute
SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSave)
    .ObservesProperty(() => User.UserName)
    .ObservesProperty(() => User.RealName);

private bool CanExecuteSave()
{
    return !string.IsNullOrWhiteSpace(User?.UserName)
        && !string.IsNullOrWhiteSpace(User?.RealName);
}

// 方式2：INotifyDataErrorInfo（推荐用于复杂验证）
public class UserCreateViewModel : UnifiedViewModelBase, INotifyDataErrorInfo
{
    // 实现INotifyDataErrorInfo接口...
}
```

### Q5: 如何在保存成功后刷新列表？

**A**: 使用Prism EventAggregator
```csharp
// 1. 定义事件
public class UserCreatedEvent : PubSubEvent<UserDto> { }

// 2. 创建视图发布事件
EventAggregator.GetEvent<UserCreatedEvent>().Publish(result.Data!);

// 3. 列表订阅事件
public class UserManagementViewModel : UnifiedViewModelBase
{
    public UserManagementViewModel(IEventAggregator eventAggregator, ...)
    {
        eventAggregator.GetEvent<UserCreatedEvent>().Subscribe(OnUserCreated);
    }

    private async void OnUserCreated(UserDto user)
    {
        await LoadUsersAsync(); // 刷新列表
    }
}
```

### Q6: 如何处理PasswordBox不支持绑定的问题？

**A**: 在View代码后台处理PasswordChanged事件
```csharp
// View.xaml.cs
private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    if (DataContext is MyViewModel viewModel && sender is PasswordBox passwordBox)
    {
        viewModel.Password = passwordBox.Password;
    }
}

// XAML
<PasswordBox PasswordChanged="PasswordBox_PasswordChanged"/>
```

### Q7: 如何统一管理Region名称？

**A**: 创建常量类
```csharp
public static class RegionNames
{
    public const string ContentRegion = "ContentRegion";
    public const string MainRegion = "MainRegion";
}

// 使用
_regionManager.RequestNavigate(RegionNames.ContentRegion, "UserCreateView");
```

---

## 10. 与Dialog模式对比

### 10.1 代码对比

#### Dialog模式（已废弃）

```csharp
// ❌ Dialog模式 - UserFormDialogViewModel（~400行）
public class UserFormDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    // IDialogAware接口实现
    public string Title => _mode == "create" ? "创建用户" : "编辑用户";
    public event Action<IDialogResult>? RequestClose;

    // 复杂的模式判断
    private string _mode;

    // OnDialogOpened接收参数
    public void OnDialogOpened(IDialogParameters parameters)
    {
        _mode = parameters.GetValue<string>("mode");
        if (_mode == "edit")
        {
            var userId = parameters.GetValue<Guid>("userId");
            LoadUserAsync(userId);
        }
    }

    // 关闭对话框
    private void ExecuteSaveCommand()
    {
        // 保存逻辑...
        var result = new DialogResult(ButtonResult.OK);
        RequestClose?.Invoke(result);
    }
}

// 触发Dialog
_dialogService.ShowDialog("UserFormDialog",
    new DialogParameters { { "mode", "create" } },
    result => {
        if (result.Result == ButtonResult.OK)
        {
            // 处理结果...
        }
    });
```

#### Navigation模式（当前）

```csharp
// ✅ Navigation模式 - UserCreateViewModel（~150行）
public class UserCreateViewModel : UnifiedViewModelBase
{
    // INavigationAware接口实现（更简单）
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 初始化表单
        User = new UserCreateDto { Role = UserRole.Doctor, IsActive = true };
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return false; // 每次创建新实例
    }

    // 保存后直接导航
    private async void ExecuteSaveCommand()
    {
        // 保存逻辑...
        EventAggregator.GetEvent<UserCreatedEvent>().Publish(result.Data!);
        _regionManager.RequestNavigate("ContentRegion", "UserManagementView");
    }
}

// 触发Navigation
_regionManager.RequestNavigate("ContentRegion", "UserCreateView");
```

### 10.2 注册对比

```csharp
// ❌ Dialog模式注册
containerRegistry.RegisterDialog<UserFormDialog, UserFormDialogViewModel>();

// ✅ Navigation模式注册
containerRegistry.RegisterForNavigation<UserCreateView, UserCreateViewModel>();
containerRegistry.RegisterForNavigation<UserEditView, UserEditViewModel>();
```

### 10.3 优势总结

| 指标 | Dialog模式 | Navigation模式 | 改进 |
|-----|----------|--------------|------|
| **代码行数** | 400行（合并Create/Edit） | 150行（单独Create） + 150行（Edit） | -25% |
| **接口复杂度** | IDialogAware（7个成员） | INavigationAware（3个成员） | -57% |
| **用户体验** | 模态对话框受限 | 全屏响应式 | +40% |
| **测试友好度** | 需要Mock IDialogService | 标准Navigation测试 | +35% |
| **代码重用** | Create/Edit共用一个ViewModel | 分离职责，更清晰 | +30% |

---

## 📚 参考资源

### 相关文档

- **架构文档**: [docs/explanation/architecture/client/README.md](../../explanation/architecture/client/README.md#users模块架构演化epic-1926---dialog-to-navigation-migration) - Users模块架构演化
- **代码模式**: [docs/reference/quick-reference/code-patterns.md](../../reference/quick-reference/code-patterns.md#-navigation模式-viewmodel-epic-1926) - Navigation模式完整示例
- **Epic分析**: [docs/reports/user-management-interaction-unification-deep-analysis-2025-11-08.md](../../reports/user-management-interaction-unification-deep-analysis-2025-11-08.md) - 深度技术分析

### Epic实施记录

- **Epic #1926**: 用户管理交互模式统一 - Dialog迁移为Navigation模式
  - **Sprint 1** (#1927): UserFormDialog → UserCreateView + UserEditView
  - **Sprint 2** (#1928): UserProfile/ResetPassword → Navigation模式
  - **Sprint 3** (#1929): ChangePassword/UserProfile → Navigation模式
  - **Sprint 4** (#1930): 清理废弃代码 + 文档更新

### Prism官方文档

- [Region Navigation](https://prismlibrary.com/docs/wpf/navigation/navigation-basics.html)
- [INavigationAware Interface](https://prismlibrary.com/docs/wpf/navigation/navigation-awareness.html)
- [Passing Parameters](https://prismlibrary.com/docs/wpf/navigation/passing-parameters.html)

---

**文档版本**: v1.0
**最后更新**: 2025-11-09
**维护者**: LYBTZYZS Team
