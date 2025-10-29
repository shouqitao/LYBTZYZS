# Component模式

**创建日期**: 2025-10-25
**适用范围**: Desktop端
**复杂度**: ⭐（简单）

---

## 📋 模式概述

Component是Desktop端用于封装可复用逻辑的辅助类，主要用于跨模块共享的服务和功能。

**核心价值**：
- ✅ **跨模块复用**：避免代码重复
- ✅ **职责清晰**：单一职责，易于测试
- ✅ **降低耦合**：通过接口依赖

---

## 🎯 Component设计三原则（ADR-004）

### 原则1：跨模块共享优先

**何时创建Component**：
- ✅ 功能被2个及以上模块使用
- ✅ 需要统一的行为实现
- ✅ 有明确的业务价值

### 原则2：避免薄封装

**禁止场景**：
- ❌ 仅封装1-2行代码的Component
- ❌ 不包含业务逻辑的纯转发Component
- ❌ 与ViewModel职责重叠的Component

### 原则3：职责清晰优先

**设计检查**：
- ✅ Component有明确的单一职责
- ✅ Component职责不与ViewModel重叠
- ✅ Component边界清晰，易于测试

---

## 💻 代码示例

### ✅ 正确示例：跨模块通知服务

```csharp
// 接口定义
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}

// 实现类
public class NotificationService : INotificationService
{
    private readonly ISnackbarMessageQueue _messageQueue;

    public NotificationService(ISnackbarMessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    public void ShowSuccess(string message)
    {
        _messageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(3));
    }

    public void ShowError(string message)
    {
        _messageQueue.Enqueue($"错误: {message}", null, null, null, false, true, TimeSpan.FromSeconds(5));
    }

    public void ShowWarning(string message)
    {
        _messageQueue.Enqueue($"警告: {message}", null, null, null, false, true, TimeSpan.FromSeconds(4));
    }

    public void ShowInfo(string message)
    {
        _messageQueue.Enqueue($"提示: {message}", null, null, null, false, true, TimeSpan.FromSeconds(3));
    }
}

// 使用
public class PrescriptionManagementViewModel
{
    private readonly INotificationService _notificationService;

    public PrescriptionManagementViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private async Task OnDeleteAsync(int id)
    {
        try
        {
            await _repository.DeletePrescriptionAsync(id);
            _notificationService.ShowSuccess("删除成功");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"删除失败: {ex.Message}");
        }
    }
}
```

### ❌ 错误示例：薄封装Command Handler

```csharp
// ❌ 错误：仅转发，无业务价值
public class PrescriptionCommandHandler
{
    private readonly PrescriptionManagementViewModel _viewModel;

    public PrescriptionCommandHandler(PrescriptionManagementViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    // ❌ 仅封装1行代码
    public void Delete(int id)
    {
        _viewModel.DeleteCommand.Execute(id);
    }

    public void Print(int id)
    {
        _viewModel.PrintCommand.Execute(id);
    }
}

// ✅ 正确：直接在ViewModel实现
public class PrescriptionManagementViewModel
{
    public DelegateCommand<int> DeleteCommand { get; }
    public DelegateCommand<int> PrintCommand { get; }

    public PrescriptionManagementViewModel()
    {
        DeleteCommand = new DelegateCommand<int>(OnDelete);
        PrintCommand = new DelegateCommand<int>(OnPrint);
    }

    private void OnDelete(int id) { /* 实现 */ }
    private void OnPrint(int id) { /* 实现 */ }
}
```

---

## 🔗 相关资源

- **ADR-004**: [Component设计指南](../decisions/ADR-004-component-design-guidelines.md)
- **架构原则**: [principles.md](../principles.md) - P1-10（Component设计三原则）
- **MVVM模式**: [mvvm-pattern.md](./mvvm-pattern.md)

---

**最后更新**: 2025-10-25
