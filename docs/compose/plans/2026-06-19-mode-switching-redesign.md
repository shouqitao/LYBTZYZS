# 远程/本地模式切换重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 实现记忆模式 + 远程探测 + 一键切换的完整远程/本地 API 切换逻辑。

**Architecture:** 在 ConnectionSettingsService 中持久化 PreferredMode + RemoteUrl。LocalUrl 固定常量 localhost:5100。ConnectionModeService 负责探测和切换。LoginViewModel 暴露切换命令。ServerConfigView 提供"保存"和"保存并启用"两种操作。

**Tech Stack:** C# / .NET 8 / WPF / Prism

---

### Task 1: 重构 ConnectionSettingsService — 持久化 PreferredMode + RemoteUrl

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IConnectionSettingsService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ConnectionSettingsService.cs`

- [ ] **Step 1: IConnectionSettingsService 新增属性**

```csharp
/// <summary>LocalWebAPI 固定地址。</summary>
string LocalUrl { get; }

/// <summary>远程服务器地址（持久化）。</summary>
string RemoteUrl { get; set; }

/// <summary>上次选择的模式（持久化）。</summary>
string PreferredMode { get; set; }

/// <summary>保存远程地址配置。</summary>
Task SaveRemoteUrlAsync(string url);

/// <summary>保存首选模式。</summary>
Task SavePreferredModeAsync(string mode);
```

- [ ] **Step 2: ConnectionSettingsService 实现**

```csharp
public const string LocalUrlConstant = "http://localhost:5100";
public string LocalUrl => LocalUrlConstant;
public string RemoteUrl { get; private set; } = string.Empty;
public string PreferredMode { get; private set; } = "Local";

// 构造函数中从 appsettings.json 读取 RemoteUrl + PreferredMode
// SaveRemoteUrlAsync / SavePreferredModeAsync 持久化到 appsettings.json
```

读取逻辑：从 IConfiguration 读取 `ApiClient:RemoteUrl` 和 `ApiClient:PreferredMode`。
保存逻辑：用 JSON 修改 appsettings.json（复用现有 PersistUrlAsync 模式）。

- [ ] **Step 3: CurrentUrl 逻辑改为基于 PreferredMode**

```csharp
public string CurrentUrl =>
    PreferredMode == "Remote" && !string.IsNullOrEmpty(RemoteUrl)
        ? RemoteUrl
        : LocalUrlConstant;

public bool IsLocal => PreferredMode != "Remote" || string.IsNullOrEmpty(RemoteUrl);
```

- [ ] **Step 4: 构建验证**

### Task 2: 重构 ConnectionModeService — 探测 + 切换

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/ConnectionModeService.cs`

- [ ] **Step 1: SetMode 重写**

```csharp
public void SetMode(ConnectionMode mode)
{
    if (mode == ConnectionMode.Local)
    {
        _connectionSettings.SavePreferredModeAsync("Local").FireAndForget();
        ApplyMode(ConnectionMode.Local);
    }
    else if (mode == ConnectionMode.Remote)
    {
        if (!string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
        {
            _connectionSettings.SavePreferredModeAsync("Remote").FireAndForget();
            ApplyMode(ConnectionMode.Remote);
        }
    }
}
```

- [ ] **Step 2: DetectBestModeAsync 重写**

```csharp
public async Task<ConnectionMode> DetectBestModeAsync()
{
    var preferred = _connectionSettings.PreferredMode;

    if (preferred == "Remote" && !string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
    {
        var reachable = await TestRemoteConnectionAsync(_connectionSettings.RemoteUrl);
        if (reachable)
        {
            ApplyMode(ConnectionMode.Remote);
            return ConnectionMode.Remote;
        }
        // 远程不可用 → 降级到本地，但不改 PreferredMode
        Logger.LogWarning("远程不可用，降级到本地模式");
    }

    ApplyMode(ConnectionMode.Local);
    return ConnectionMode.Local;
}
```

- [ ] **Step 3: IsRemoteAvailable 属性**

```csharp
private bool _isRemoteAvailable;
public bool IsRemoteAvailable => _isRemoteAvailable;

// 在 DetectBestModeAsync 中设置
// 也可以手动触发探测
public async Task<bool> CheckRemoteAvailableAsync()
{
    if (string.IsNullOrEmpty(_connectionSettings.RemoteUrl)) return false;
    _isRemoteAvailable = await TestRemoteConnectionAsync(_connectionSettings.RemoteUrl);
    return _isRemoteAvailable;
}
```

- [ ] **Step 4: 构建验证**

### Task 3: LoginViewModel — 切换命令 + 远程可用状态

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

- [ ] **Step 1: 替换 SwitchToLocal/SwitchToRemote 命令**

```csharp
[ObservableProperty]
private bool _isRemoteAvailable;

private void ExecuteSwitchToLocal()
{
    _connectionModeService?.SetMode(ConnectionMode.Local);
    IsRemoteMode = false;
    CurrentModeDisplay = "本地模式";
}

private void ExecuteSwitchToRemote()
{
    if (!IsRemoteAvailable) return;
    _connectionModeService?.SetMode(ConnectionMode.Remote);
    IsRemoteMode = true;
    CurrentModeDisplay = "远程模式";
}

private bool CanSwitchToRemote() => IsRemoteAvailable;
```

- [ ] **Step 2: BackgroundInit 中探测远程可用性**

在 `DetectConnectionModeAsync` 之后：

```csharp
if (_connectionModeService != null && _connectionSettingsService != null)
{
    IsRemoteAvailable = await _connectionModeService.CheckRemoteAvailableAsync();
}
```

- [ ] **Step 3: 构建验证**

### Task 4: LoginView.xaml — 切换按钮绑定

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`

- [ ] **Step 1: 切换按钮 + CanExecute 绑定**

```xml
<!-- 当前本地 → 显示"切换到远程"，远程不可用时禁用 -->
<Button Command="{Binding SwitchToRemoteCommand}" Content="🌐 切换到远程"
        FontSize="13" Foreground="#8D6E63" Background="Transparent" BorderThickness="0"
        Cursor="Hand" Padding="12,6">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Visibility" Value="Collapsed" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsRemoteMode}" Value="False">
                    <Setter Property="Visibility" Value="Visible" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>

<!-- 当前远程 → 显示"切换到本地" -->
<Button Command="{Binding SwitchToLocalCommand}" Content="📱 切换到本地"
        FontSize="13" Foreground="#8D6E63" Background="Transparent" BorderThickness="0"
        Cursor="Hand" Padding="12,6">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Visibility" Value="Collapsed" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsRemoteMode}" Value="True">
                    <Setter Property="Visibility" Value="Visible" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

按钮禁用状态由 `CanSwitchToRemote()` (returns IsRemoteAvailable) 控制。

- [ ] **Step 2: 构建验证**

### Task 5: ServerConfigView — 保存 + 保存并启用

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/ServerConfigViewModel.cs`

- [ ] **Step 1: 新增 SaveOnlyCommand**

```csharp
[RelayCommand]
private async Task SaveOnly()
{
    if (!_connectionSettingsService.IsValidUrl(RemoteUrl)) return;
    await _connectionSettingsService.SaveRemoteUrlAsync(RemoteUrl);
    CloseDialog(ButtonResult.OK);
}
```

- [ ] **Step 2: Confirm (保存并启用) 增加条件**

```csharp
protected override bool CanConfirm() =>
    !string.IsNullOrWhiteSpace(RemoteUrl)
    && TestStatus == ConnectionTestStatus.Success  // 测试通过才能启用
    && !IsLoading;
```

Confirm 方法中：保存 RemoteUrl + SetMode(Remote) + CloseDialog。

- [ ] **Step 3: ServerConfigView.xaml 增加两个按钮**

```xml
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
    <Button Content="保存" Command="{Binding SaveOnlyCommand}" Padding="16,8" Margin="0,0,8,0" />
    <Button Content="保存并启用" Command="{Binding ConfirmCommand}" Padding="16,8" />
</StackPanel>
```

"保存并启用" 按钮在 `TestStatus != Success` 时自动禁用（CanConfirm 返回 false）。

- [ ] **Step 4: 构建验证**

### Task 6: 最终构建 + 提交

- [ ] **Step 1: 全量构建**
Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 提交 + 推送**
