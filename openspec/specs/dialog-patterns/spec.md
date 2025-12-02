# Dialog Patterns Spec

## 概述

本规范定义了 LYBT Desktop 应用中对话框和用户通知的使用模式，确保一致的用户体验和可维护的代码结构。

## 状态

- **版本**: 1.0
- **创建日期**: 2025-12-03
- **OpenSpec**: cleanup-ui-layer Phase 4

## 核心接口

### 1. ICommonDialogService (Infrastructure层)

**用途**: 通用对话框操作，包括确认、输入、文件选择等

**位置**: `LYBT.Desktop.Infrastructure.Interfaces.ICommonDialogService`

```csharp
public interface ICommonDialogService
{
    // 消息对话框
    Task ShowInfoAsync(string message, string? title = null);
    Task ShowWarningAsync(string message, string? title = null);
    Task ShowErrorAsync(string message, string? title = null);

    // 确认对话框
    Task<bool> ShowConfirmAsync(string message, string? title = null);
    Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null);

    // 输入对话框
    Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null);

    // 文件对话框
    Task<string?> ShowOpenFileDialogAsync(string? filter = null, string? title = null);
    Task<string?> ShowSaveFileDialogAsync(string? filter = null, string? title = null, string? defaultFileName = null);
}
```

**适用场景**:
- 需要用户确认的操作（删除、保存、离开等）
- 需要用户输入的场景
- 文件选择操作

### 2. IUserNotificationService (Infrastructure层)

**用途**: 用户通知和异常处理

**位置**: `LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService`

```csharp
public interface IUserNotificationService
{
    // 异常处理
    Task HandleExceptionAsync(Exception exception, string? context = null);

    // 消息通知
    Task ShowErrorAsync(string message, string? title = null);
    Task ShowSuccessAsync(string message, string? title = null);
    Task ShowWarningAsync(string message, string? title = null);
    Task ShowInfoAsync(string message, string? title = null);

    // 确认对话框
    Task<bool> ShowConfirmAsync(string message, string? title = null);

    // 全局异常处理
    void RegisterGlobalExceptionHandlers();
}
```

**适用场景**:
- 操作结果反馈（成功、失败）
- 异常处理和显示
- 全局错误处理

### 3. IDialogService (Prism)

**用途**: 自定义对话框（复杂表单、详情查看等）

**位置**: `Prism.Services.Dialogs.IDialogService`

**适用场景**:
- 需要自定义UI的对话框
- 复杂数据输入表单
- 详情查看窗口
- 多步骤向导

## 使用规范

### DO (推荐)

1. **在 ViewModel 中使用服务接口**
```csharp
public class MyViewModel : UnifiedViewModelBase
{
    private readonly ICommonDialogService _dialogService;

    public async Task DeleteItemAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "确定要删除此项吗？此操作不可恢复。",
            "删除确认");

        if (confirmed)
        {
            // 执行删除
        }
    }
}
```

2. **使用 ViewModelBase 提供的便捷方法**
```csharp
// UnifiedViewModelBase 提供的方法
await ShowSuccessAsync("保存成功");
await ShowErrorAsync("保存失败: " + errorMessage);
var result = await ShowConfirmationAsync("确定要继续吗？");
```

3. **三选项对话框用于需要取消操作的场景**
```csharp
var result = await _dialogService.ShowTripleChoiceAsync(
    "您有未保存的更改，是否保存？",
    "离开确认");

switch (result)
{
    case TripleChoiceResult.Yes:
        await SaveAsync();
        NavigateAway();
        break;
    case TripleChoiceResult.No:
        NavigateAway();
        break;
    case TripleChoiceResult.Cancel:
        // 保持当前状态
        break;
}
```

4. **使用 Prism IDialogService 打开自定义对话框**
```csharp
_dialogService.ShowDialog("PatientDetailDialog",
    new DialogParameters { { "PatientId", patientId } },
    result => { /* 处理结果 */ });
```

### DON'T (禁止)

1. **禁止在 ViewModel 中直接使用 MessageBox**
```csharp
// 错误做法
MessageBox.Show("保存成功", "提示");

// 正确做法
await _dialogService.ShowInfoAsync("保存成功", "提示");
```

2. **禁止在 View 代码后置中处理业务逻辑对话框**
```csharp
// 错误做法 - View.xaml.cs
private void Button_Click(object sender, RoutedEventArgs e)
{
    if (MessageBox.Show("确认?") == MessageBoxResult.Yes)
    {
        // 业务逻辑
    }
}

// 正确做法 - ViewModel 中处理
```

3. **禁止混用同步和异步对话框调用**
```csharp
// 错误做法
var result = _dialogService.ShowConfirmAsync(message).Result; // 阻塞

// 正确做法
var result = await _dialogService.ShowConfirmAsync(message);
```

## 服务职责分离

| 接口 | 层 | 主要职责 | 使用者 |
|------|------|----------|--------|
| ICommonDialogService | Infrastructure | 通用对话框、文件对话框 | ViewModel、服务 |
| IUserNotificationService | Infrastructure | 用户通知、异常处理 | ViewModel、全局 |
| IDialogService (Prism) | Presentation | 自定义对话框窗口 | ViewModel |

## 特殊场景

### 1. 应用初始化失败

在 DI 容器初始化之前的错误，允许直接使用 MessageBox：

```csharp
// App.xaml.cs - OnStartup 中
try
{
    InitializeContainer();
}
catch (Exception ex)
{
    // DI容器未初始化，无法使用服务
    MessageBox.Show(ex.Message, "初始化失败",
        MessageBoxButton.OK, MessageBoxImage.Error);
    Shutdown();
}
```

### 2. 后台服务中的通知

后台服务需要通过 Dispatcher 回到 UI 线程：

```csharp
await Application.Current.Dispatcher.InvokeAsync(async () =>
{
    await _notificationService.ShowWarningAsync("连接已断开");
});
```

## 实现细节

### 当前实现

- `CommonDialogService`: 基于 WPF MessageBox 的简单实现
- `UserNotificationService`: 基于 WPF MessageBox 的简单实现

### 未来扩展

可以将实现替换为自定义控件，而不影响 ViewModel 层代码：
- Toast 通知
- 自定义样式对话框
- 动画效果

## 通知服务分层

### IUserNotificationService vs INotificationService

这两个接口是**合理的分层设计**，不是重复：

| 接口 | 层 | 用途 | 使用文件数 |
|------|------|------|-----------|
| IUserNotificationService | Infrastructure | ViewModel 层通知 API | 35+ |
| INotificationService | Presentation | UI 层内部通知 + Loading | 6 |

**IUserNotificationService 特有功能**:
- `HandleExceptionAsync()` - 异常处理和显示
- `RegisterGlobalExceptionHandlers()` - 全局异常处理

**INotificationService 特有功能**:
- `ShowLoading()` / `HideLoading()` - 加载状态管理
- `NotificationShown` 事件 - UI 组件响应通知
- `LoadingStateChanged` 事件 - Loading 状态变化

### 使用指南

```csharp
// ViewModel 层 - 使用 IUserNotificationService
public class MyViewModel : UnifiedViewModelBase
{
    private readonly IUserNotificationService _notification;

    public async Task DoSomethingAsync()
    {
        try
        {
            await _service.OperateAsync();
            await _notification.ShowSuccessAsync("操作成功");
        }
        catch (Exception ex)
        {
            await _notification.HandleExceptionAsync(ex, "执行操作时");
        }
    }
}

// Presentation 层基础设施 - 使用 INotificationService
public class LoadingOverlay : UserControl
{
    public LoadingOverlay(INotificationService notification)
    {
        notification.LoadingStateChanged += (s, e) =>
        {
            Visibility = e.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = e.Message;
        };
    }
}
```

## 相关规范

- [viewmodel-conventions](../viewmodel-conventions/spec.md) - ViewModel 层约定
- [service-conventions](../service-conventions/spec.md) - 服务层约定

---
创建时间: 2025-12-03
OpenSpec: cleanup-ui-layer Phase 4
