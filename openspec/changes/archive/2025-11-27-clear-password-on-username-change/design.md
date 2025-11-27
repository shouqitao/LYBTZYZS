# Design: clear-password-on-username-change

## 技术方案

### 1. 核心改动

在 `LoginViewModel.cs` 中：

```csharp
// 新增字段：记录加载的保存用户名
private string? _savedUsername;

// 修改 Username 属性 setter
public string Username
{
    get => _username;
    set
    {
        // 检测是否与保存的用户名不同
        var usernameChanged = _savedUsername != null &&
                              !string.IsNullOrEmpty(_savedUsername) &&
                              value != _savedUsername;

        if (SetProperty(ref _username, value))
        {
            // 如果用户名改变了（且不是初始加载），清空密码
            if (usernameChanged && !string.IsNullOrEmpty(_password))
            {
                Password = string.Empty;
                Logger.LogInformation("用户名已变更，密码字段已清空");
            }

            (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }
}
```

### 2. 加载凭据时记录用户名

在 `LoadSavedCredentialsAsync()` 中：

```csharp
// 成功加载凭据后，记录保存的用户名
_savedUsername = credentials.Value.Username;
```

### 3. 边界条件处理

| 场景 | 行为 |
|------|------|
| 初始加载（无保存凭据）| `_savedUsername = null`，不触发清空 |
| 初始加载（有保存凭据）| `_savedUsername = "用户A"`，填充用户名+密码 |
| 用户输入用户名（与保存不同）| 检测变化，清空密码 |
| 用户清空用户名 | 不清空密码（允许用户删除后重新输入）|
| 用户恢复原用户名 | 不恢复密码（安全考虑）|

### 4. 时序图

```
┌─────────────┐         ┌──────────────┐
│   启动应用   │         │ LoginViewModel│
└─────┬───────┘         └──────┬───────┘
      │                        │
      │  LoadSavedCredentials  │
      │───────────────────────>│
      │                        │ _savedUsername = "userA"
      │                        │ Username = "userA"
      │                        │ Password = "****"
      │                        │
      │  用户修改用户名为"userB" │
      │───────────────────────>│
      │                        │ Username setter 检测
      │                        │ value != _savedUsername
      │                        │ => Password = ""
      │                        │
```

## 测试要点

1. **基本功能**：用户名变更后密码清空
2. **初始加载**：不应触发清空
3. **无保存凭据**：正常输入不受影响
4. **边界条件**：空用户名、空格处理
