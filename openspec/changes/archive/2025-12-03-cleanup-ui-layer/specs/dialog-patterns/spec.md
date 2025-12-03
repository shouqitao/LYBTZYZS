# Dialog Patterns Specification

## ADDED Requirements

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

## Cross-Reference

- **viewmodel-conventions**: ViewModel设计规范，包含命令模式
- **ui-style-conventions**: 对话框样式定义
