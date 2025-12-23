# Design: enhance-shell-connection-dialog

## Architecture Overview

### 组件关系

```
┌─────────────────────────────────────────────────────────────────┐
│                         Shell Layer                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐    ┌─────────────────────────────────┐   │
│  │ App.xaml.cs      │───▶│ StartupPipeline                 │   │
│  │                  │    │   └─ ApiHealthCheckStartupStep  │   │
│  └──────────────────┘    └─────────────┬───────────────────┘   │
│                                        │                        │
│                                        │ 失败时                  │
│                                        ▼                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ IApiConnectionRecoveryService                           │   │
│  │   └─ ShowConnectionFailedDialogAsync()                  │   │
│  │       └─ 返回 RecoveryAction (Retry/Offline/Exit)       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                          │                                      │
│                          ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ApiConnectionFailedDialog (View)                        │   │
│  │   └─ ApiConnectionFailedDialogViewModel                 │   │
│  │       ├─ ErrorSummary: string                           │   │
│  │       ├─ PossibleReasons: List<string>                  │   │
│  │       ├─ TechnicalDetails: string                       │   │
│  │       ├─ IsDetailsExpanded: bool                        │   │
│  │       ├─ IsOfflineModeEnabled: bool = false             │   │
│  │       ├─ RetryCommand: DelegateCommand                  │   │
│  │       ├─ OfflineModeCommand: DelegateCommand            │   │
│  │       ├─ ViewLogsCommand: DelegateCommand               │   │
│  │       └─ ExitCommand: DelegateCommand                   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. RecoveryAction 枚举

```csharp
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// API连接恢复操作类型
/// </summary>
public enum RecoveryAction
{
    /// <summary>重试连接</summary>
    Retry,

    /// <summary>进入离线模式 (v2.0预留)</summary>
    OfflineMode,

    /// <summary>退出应用</summary>
    Exit
}
```

### 2. IApiConnectionRecoveryService 接口

```csharp
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// API连接恢复服务接口
/// 负责处理API连接失败后的用户交互和恢复流程
/// </summary>
public interface IApiConnectionRecoveryService
{
    /// <summary>
    /// 显示连接失败对话框并获取用户选择的恢复操作
    /// </summary>
    /// <param name="errorMessage">错误摘要信息</param>
    /// <param name="exception">原始异常(可选)</param>
    /// <param name="apiEndpoint">API端点地址(可选)</param>
    /// <returns>用户选择的恢复操作</returns>
    Task<RecoveryAction> ShowConnectionFailedDialogAsync(
        string errorMessage,
        Exception? exception = null,
        string? apiEndpoint = null);
}
```

### 3. ApiConnectionFailedDialogViewModel

```csharp
namespace LYBT.Desktop.Shell.Dialogs.ViewModels;

/// <summary>
/// API连接失败对话框ViewModel
/// </summary>
public class ApiConnectionFailedDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    // === 属性 ===

    /// <summary>错误摘要</summary>
    public string ErrorSummary { get; set; }

    /// <summary>可能原因列表</summary>
    public List<string> PossibleReasons { get; set; }

    /// <summary>技术详情(可展开)</summary>
    public string TechnicalDetails { get; set; }

    /// <summary>详情是否展开</summary>
    public bool IsDetailsExpanded { get; set; }

    /// <summary>离线模式是否可用 (v2.0启用)</summary>
    public bool IsOfflineModeEnabled { get; } = false;

    /// <summary>离线模式提示文本</summary>
    public string OfflineModeTooltip { get; } = "离线模式将在v2.0版本中启用";

    // === 命令 ===

    public DelegateCommand RetryCommand { get; }
    public DelegateCommand OfflineModeCommand { get; }
    public DelegateCommand ViewLogsCommand { get; }
    public DelegateCommand ExitCommand { get; }

    // === IDialogAware ===

    public string Title { get; set; } = "无法连接到服务器";
    public event Action<IDialogResult> RequestClose;
}
```

### 4. 对话框XAML结构

```xml
<!-- ApiConnectionFailedDialog.xaml -->
<Window Style="{StaticResource DialogWindowStyle}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 图标+标题 -->
            <RowDefinition Height="Auto"/>  <!-- 错误信息 -->
            <RowDefinition Height="Auto"/>  <!-- 可能原因 -->
            <RowDefinition Height="Auto"/>  <!-- 展开详情 -->
            <RowDefinition Height="Auto"/>  <!-- 按钮区 -->
        </Grid.RowDefinitions>

        <!-- 标题区 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal">
            <Path Data="{StaticResource WarningIcon}" Fill="#F59E0B"/>
            <TextBlock Text="无法连接到服务器" Style="{StaticResource DialogTitleStyle}"/>
        </StackPanel>

        <!-- 错误信息 -->
        <TextBlock Grid.Row="1" Text="{Binding ErrorSummary}"/>

        <!-- 可能原因 -->
        <ItemsControl Grid.Row="2" ItemsSource="{Binding PossibleReasons}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="•" Margin="0,0,8,0"/>
                        <TextBlock Text="{Binding}"/>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- 展开详情 -->
        <Expander Grid.Row="3" Header="展开详情" IsExpanded="{Binding IsDetailsExpanded}">
            <Border Style="{StaticResource TechnicalDetailsBorder}">
                <TextBlock Text="{Binding TechnicalDetails}"
                          FontFamily="Consolas" FontSize="12"/>
            </Border>
        </Expander>

        <!-- 按钮区 -->
        <StackPanel Grid.Row="4" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="离线模式(v2.0)"
                    Command="{Binding OfflineModeCommand}"
                    IsEnabled="{Binding IsOfflineModeEnabled}"
                    ToolTip="{Binding OfflineModeTooltip}"/>
            <Button Content="查看日志" Command="{Binding ViewLogsCommand}"/>
            <Button Content="重试" Command="{Binding RetryCommand}" IsDefault="True"/>
            <Button Content="退出" Command="{Binding ExitCommand}" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

### 5. 启动流程修改

```csharp
// App.xaml.cs - 修改后的InitializeApplicationAsync

private async Task InitializeApplicationAsync()
{
    try
    {
        _startupPipeline = Container.Resolve<IStartupPipeline>();
        RegisterStartupSteps();
        SubscribeToPipelineEvents();

        var progress = new Progress<string>(message => _splashScreen?.UpdateStatus(message));

        // 循环执行，支持重试
        while (true)
        {
            var result = await _startupPipeline.ExecuteAsync(progress);

            if (result.Success)
            {
                await ShowMainWindowAfterInitializationAsync();
                return;
            }

            // API健康检查失败时显示恢复对话框
            if (result.FailedStepName == "API健康检查")
            {
                var recoveryService = Container.Resolve<IApiConnectionRecoveryService>();
                var action = await recoveryService.ShowConnectionFailedDialogAsync(
                    result.ErrorMessage,
                    result.Exception,
                    GetApiEndpoint());

                switch (action)
                {
                    case RecoveryAction.Retry:
                        // 重置管道状态，继续循环
                        _startupPipeline.Reset();
                        continue;

                    case RecoveryAction.OfflineMode:
                        // v2.0: 启动离线模式
                        throw new NotImplementedException("离线模式将在v2.0实现");

                    case RecoveryAction.Exit:
                    default:
                        Application.Current.Shutdown(1);
                        return;
                }
            }

            // 其他步骤失败，使用原有处理
            throw new InvalidOperationException(
                $"启动步骤 '{result.FailedStepName}' 执行失败: {result.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        await HandleInitializationFailureAsync(ex);
    }
}
```

### 6. StartupPipeline扩展

需要添加`Reset()`方法支持重试：

```csharp
public interface IStartupPipeline
{
    // 现有方法...

    /// <summary>
    /// 重置管道状态，允许重新执行
    /// </summary>
    void Reset();
}
```

## v2.0本地模式预留设计

### 入口点

1. **对话框按钮**: `[离线模式(v2.0)]` 按钮已放置，当前IsEnabled=false
2. **配置开关**: `AppSettings.Features.OfflineModeEnabled = false`
3. **命令处理**: OfflineModeCommand已定义，执行时抛出NotImplementedException

### v2.0激活步骤

1. 修改`AppSettings.Features.OfflineModeEnabled = true`
2. 实现`IOfflineModeService`接口
3. 完善OfflineModeCommand逻辑
4. 添加离线模式状态管理

### 离线模式架构预览(v2.0)

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Mode                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐       ┌─────────────────┐             │
│  │  Online Mode    │       │  Offline Mode   │             │
│  │  (当前v1.0)     │       │  (v2.0预留)     │             │
│  ├─────────────────┤       ├─────────────────┤             │
│  │ • WebAPI连接    │       │ • SQLite本地库  │             │
│  │ • 实时数据同步  │       │ • 本地缓存      │             │
│  │ • 完整功能      │       │ • 基础功能      │             │
│  └─────────────────┘       └─────────────────┘             │
│           │                        ▲                        │
│           │                        │                        │
│           └────── 连接恢复 ────────┘                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## UI/UX Guidelines

### 错误信息层次

| 层级 | 内容 | 可见性 |
|-----|------|-------|
| L1 - 标题 | "无法连接到服务器" | 始终可见 |
| L2 - 摘要 | "无法连接到凌隐宝堂服务，请检查：" | 始终可见 |
| L3 - 原因 | 3-4个可能原因的bullet list | 始终可见 |
| L4 - 详情 | 服务地址、错误类型、异常信息 | 点击展开 |

### 按钮布局

```
[离线模式(v2.0)]  [查看日志]  [重试]  [退出]
     ↑              ↑          ↑       ↑
   禁用灰色      次要操作    主操作   取消
```

- **[重试]**: Primary按钮样式，IsDefault=true
- **[退出]**: IsCancel=true，支持ESC关闭
- **[离线模式]**: 禁用状态，ToolTip说明

## Testing Strategy

### Unit Tests

1. `ApiConnectionRecoveryServiceTests`
   - 对话框正确显示
   - 返回正确的RecoveryAction

2. `ApiConnectionFailedDialogViewModelTests`
   - 命令正确触发
   - 属性绑定正确
   - IsOfflineModeEnabled=false

### Integration Tests

1. 启动管道重试流程
2. 对话框与App.xaml.cs集成

### Manual Tests

1. 关闭WebAPI后启动应用，验证对话框显示
2. 点击[重试]后启动WebAPI，验证成功进入主界面
3. 点击[查看日志]验证打开日志文件夹
4. 点击[退出]验证应用正常退出
5. 验证[离线模式]按钮禁用状态

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `Contracts/Services/RecoveryAction.cs` | NEW | 恢复操作枚举 |
| `Contracts/Services/IApiConnectionRecoveryService.cs` | NEW | 服务接口 |
| `Shell/Services/ApiConnectionRecoveryService.cs` | NEW | 服务实现 |
| `Shell/Dialogs/Views/ApiConnectionFailedDialog.xaml` | NEW | 对话框视图 |
| `Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs` | NEW | 对话框VM |
| `Shell/App.xaml.cs` | MODIFIED | 启动流程修改 |
| `Shell/Services/Startup/IStartupPipeline.cs` | MODIFIED | 添加Reset方法 |
| `Shell/Services/Startup/StartupPipeline.cs` | MODIFIED | 实现Reset方法 |
| `Shell/Extensions/ServiceCollectionExtensions.cs` | MODIFIED | 注册新服务 |
