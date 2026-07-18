# Desktop 第三轮架构修复设计

## [S1] 问题

第三轮审计发现 4 HIGH + 11 MEDIUM 问题，覆盖异步、绑定、主题、异常处理四个维度。

## [S2] 工作流 A: 异步修复

### A1: ModuleLazyLoader sync-over-async (HIGH)
`EnsureModuleLoaded` 用 `.GetAwaiter().GetResult()` 阻塞 UI 线程。改为 `async Task`，`NavigationCoordinator.NavigateTo` 改为支持 async 路径。

### A2: EmbeddedLocalWebApiService Dispose (HIGH)
`StopAsync().GetAwaiter().GetResult()` 可能死锁。实现 `IAsyncDisposable`。

## [S3] 工作流 B: XAML 绑定修复

### B1: RadioButton IsChecked (HIGH)
`AccountSettingsControl.xaml` 的 `IsChecked` 绑定添加 `Mode=TwoWay`。

### B2: NavigableViewModelBase Task.Run (MEDIUM)
移除 Toast 方法中不必要的 `Task.Run` 包装。

## [S4] 工作流 C: 主题颜色统一

### C1: TCM 品牌色 (HIGH)
创建 `TcmBrands.xaml` 定义 `TcmGreen`/`TcmGold`/`TcmOrange` Brush。替换 6+ 文件中的硬编码颜色。

### C2: 侧边栏白色 (MEDIUM)
`MainWindow.xaml` 中 `Foreground="White"` 替换为 `MaterialDesign.Brush.Foreground`。

### C3: LoginView 颜色 (MEDIUM)
替换硬编码颜色为 DynamicResource。

## [S5] 工作流 D: 异常处理 + 安全

### D1: NavigationCoordinator 空 catch (MEDIUM)
3 处空 catch 添加 `Logger.LogDebug`。

### D2: 错误 UI 不一致 (MEDIUM)
统一使用 ToastService。

### D3: AccountSettings 异常消息 (MEDIUM)
用 `ClientErrorMessageMapper` 替换原始 `ex.Message`。

### D4: SwitchingApiClient 线程安全 (MEDIUM)
用 `Lazy<IApiClient>` 替代手写 lock。

### D5: LocalDB 连接字符串 (MEDIUM)
移到 `appsettings.json`。

### D6: LoginViewModel 代理属性 (MEDIUM)
已通过 `OnCredentialsPropertyChanged` 转发，无需修改。

### D7: MainWindowViewModel 代理属性 (MEDIUM)
手动通知已足够可靠，无需修改。

## [S6] 约束

- 纯修复，不改变功能行为
- 逐个 PR，每个工作流独立提交
- 每次修改后 `dotnet build` 验证

## [S7] 实施顺序

| 顺序 | 工作流 | 预估工作量 |
|------|--------|-----------|
| 1 | B: XAML 绑定（最简单） | 30 分钟 |
| 2 | D: 异常处理 + 安全 | 1-2 小时 |
| 3 | A: 异步修复 | 1-2 小时 |
| 4 | C: 主题颜色 | 1-2 小时 |
