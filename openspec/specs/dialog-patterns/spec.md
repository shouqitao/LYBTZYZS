# Dialog Patterns Spec

## Purpose

定义 LYBT Desktop 应用中对话框和用户通知的使用模式，确保一致的用户体验和可维护的代码结构。
## Requirements
### Requirement: ViewModel必须通过服务接口显示对话框

ViewModel层 SHALL 使用 ICommonDialogService 或 IUserNotificationService 显示对话框，禁止直接使用 MessageBox。

#### Scenario: 确认删除操作
- **WHEN** 用户请求删除数据
- **THEN** ViewModel调用 `_dialogService.ShowConfirmAsync()` 获取确认

#### Scenario: 显示操作结果
- **WHEN** 操作完成需要通知用户
- **THEN** ViewModel调用 `ShowSuccessAsync()` 或 `ShowErrorAsync()`

### Requirement: 服务职责分离

不同类型的对话框 SHALL 使用对应的服务接口。

#### Scenario: 通用对话框
- **WHEN** 需要确认、输入或文件选择
- **THEN** 使用 ICommonDialogService

#### Scenario: 用户通知
- **WHEN** 需要显示操作反馈或处理异常
- **THEN** 使用 IUserNotificationService

#### Scenario: 自定义对话框
- **WHEN** 需要复杂表单或自定义UI
- **THEN** 使用 Prism IDialogService

### Requirement: 异步调用模式

对话框调用 SHALL 使用 async/await 模式，禁止阻塞调用。

#### Scenario: 异步确认
- **WHEN** 调用对话框服务
- **THEN** 使用 `await _dialogService.ShowConfirmAsync(message)`
- **THEN** 禁止使用 `.Result` 或 `.Wait()` 阻塞

### Requirement: DLG-001 Dialog Coordinator Interface

所有对话框交互 MUST 通过IDialogCoordinator接口。

**规范**:
- 注入 `IDialogCoordinator` 而非直接使用 `IDialogService`
- 提供类型安全的标准对话框方法
- 封装Prism对话框细节

#### Scenario: Confirmation dialog
- **GIVEN** 需要显示确认对话框
- **WHEN** 请求用户确认
- **THEN** 调用 `_dialogCoordinator.ShowConfirmationAsync(title, message)`
- **AND** 返回 `bool` 表示用户选择
- **AND** NOT 使用 `MessageBox.Show()`

#### Scenario: Information dialog
- **GIVEN** 需要显示信息对话框
- **WHEN** 向用户展示信息
- **THEN** 调用 `_dialogCoordinator.ShowInformationAsync(title, message)`
- **AND** NOT 使用 `MessageBox.Show()`

#### Scenario: Error dialog
- **GIVEN** 需要显示错误对话框
- **WHEN** 向用户展示错误
- **THEN** 调用 `_dialogCoordinator.ShowErrorAsync(title, message, exception)`
- **AND** 可选传入Exception以显示详情
- **AND** NOT 使用 `MessageBox.Show()`

#### Scenario: Custom dialog
- **GIVEN** 需要显示自定义对话框
- **WHEN** 打开业务对话框
- **THEN** 调用 `_dialogCoordinator.ShowDialogAsync<TResult>(dialogName, parameters)`
- **AND** 返回对话框结果
- **AND** 使用IDialogParameters传递参数

### Requirement: DLG-002 Dialog Directory Structure

对话框文件 MUST 放在统一目录结构中。

**规范**:
- 对话框View放在 `Module/Dialogs/` 目录
- 对话框ViewModel放在 `Module/ViewModels/Dialogs/` 目录
- 文件名以 `Dialog` 结尾

#### Scenario: Creating new dialog
- **GIVEN** 需要创建新对话框
- **WHEN** 创建对话框文件
- **THEN** View放在 `Dialogs/{Name}Dialog.xaml`
- **AND** ViewModel放在 `ViewModels/Dialogs/{Name}DialogViewModel.cs`
- **AND** NOT 放在 `Views/` 目录

#### Scenario: Dialog naming
- **GIVEN** 命名对话框
- **WHEN** 选择名称
- **THEN** 名称描述对话框用途
- **AND** 以 `Dialog` 后缀结尾
- **EXAMPLE** `ConfirmDeleteDialog`, `QuickCreatePatientDialog`

### Requirement: DLG-003 Dialog ViewModel Pattern

对话框ViewModel MUST 实现IDialogAware接口。

**规范**:
- 继承或实现 `IDialogAware`
- 使用 `DialogParameters` 接收参数
- 通过 `RequestClose` 关闭对话框
- 返回结果通过 `IDialogParameters` 传递

#### Scenario: Dialog with result
- **GIVEN** 对话框需要返回结果
- **WHEN** 用户确认
- **THEN** 设置 `IDialogParameters` 包含结果
- **AND** 调用 `RequestClose(new DialogResult(ButtonResult.OK, parameters))`

```csharp
// 示例实现
private void OnConfirm()
{
    var parameters = new DialogParameters
    {
        { "SelectedItem", SelectedItem }
    };
    RequestClose(new DialogResult(ButtonResult.OK, parameters));
}
```

#### Scenario: Dialog cancellation
- **GIVEN** 对话框被取消
- **WHEN** 用户点击取消或关闭
- **THEN** 调用 `RequestClose(new DialogResult(ButtonResult.Cancel))`
- **AND** 调用方收到取消结果

### Requirement: DLG-004 Dialog Registration

对话框 MUST 在模块中注册。

**规范**:
- 在 `IModule.RegisterTypes` 中注册
- 使用 `RegisterDialog<TView, TViewModel>` 方法
- 对话框名称与类名一致

#### Scenario: Registering dialog
- **GIVEN** 创建了新对话框
- **WHEN** 注册到DI容器
- **THEN** 在模块的 `RegisterTypes` 方法中添加
- **AND** 使用 `containerRegistry.RegisterDialog<Dialog, DialogViewModel>()`

```csharp
// 示例
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterDialog<ConfirmDeleteDialog, ConfirmDeleteDialogViewModel>();
}
```

### Requirement: DLG-005 No Direct MessageBox

ViewModel中 MUST NOT 直接使用MessageBox.Show。

**规范**:
- MessageBox.Show 在ViewModel中禁止使用
- 使用 `IDialogCoordinator` 或 `IUserNotification` 替代
- View code-behind中也应避免

#### Scenario: Replace MessageBox with DialogCoordinator
- **GIVEN** 现有代码使用 `MessageBox.Show`
- **WHEN** 重构代码
- **THEN** 替换为 `_dialogCoordinator.ShowConfirmationAsync` 或类似方法
- **AND** 确保异步调用模式

```csharp
// 禁止
var result = MessageBox.Show("确定删除?", "确认", MessageBoxButton.YesNo);

// 推荐
var confirmed = await _dialogCoordinator.ShowConfirmationAsync("确认", "确定删除?");
```

## 核心接口

### ICommonDialogService (Infrastructure层)

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

### IUserNotificationService (Infrastructure层)

**位置**: `LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService`

```csharp
public interface IUserNotificationService
{
    Task HandleExceptionAsync(Exception exception, string? context = null);
    Task ShowErrorAsync(string message, string? title = null);
    Task ShowSuccessAsync(string message, string? title = null);
    Task ShowWarningAsync(string message, string? title = null);
    Task ShowInfoAsync(string message, string? title = null);
    Task<bool> ShowConfirmAsync(string message, string? title = null);
    void RegisterGlobalExceptionHandlers();
}
```

## 使用示例

### 确认对话框

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

### 三选项对话框

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

## 服务职责矩阵

| 接口 | 层 | 主要职责 | 使用者 |
|------|------|----------|--------|
| ICommonDialogService | Infrastructure | 通用对话框、文件对话框 | ViewModel、服务 |
| IUserNotificationService | Infrastructure | 用户通知、异常处理 | ViewModel、全局 |
| IDialogService (Prism) | Presentation | 自定义对话框窗口 | ViewModel |

## 特殊场景

### 应用初始化失败

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

## 通知服务分层

IUserNotificationService 和 INotificationService 是合理的分层设计：

| 接口 | 层 | 用途 | 使用文件数 |
|------|------|------|-----------|
| IUserNotificationService | Infrastructure | ViewModel 层通知 API | 35+ |
| INotificationService | Presentation | UI 层内部通知 + Loading | 6 |

## 相关规范

- [viewmodel-conventions](../viewmodel-conventions/spec.md) - ViewModel 层约定
- [service-conventions](../service-conventions/spec.md) - 服务层约定

---
版本: 1.0
创建时间: 2025-12-03
OpenSpec: cleanup-ui-layer Phase 4
