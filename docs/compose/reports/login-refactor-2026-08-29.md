# Login 模块全面重构审计报告

> 日期：2026-08-29 | 任务书：`.hermes-task-login-refactor.md`
> 范围：4 View + 6 ViewModel + 7 Service + 2 Control（19 文件） | 6 审计维度（D1-D6）
> 结果：全量构建 0 错误 0 警告 | 架构测试 97/97 | 相关单测 129/129

## 一、逐文件审计表

### View 层（4 个）

| 文件 | 问题 | 修复 |
|------|------|------|
| `LoginView.xaml` | ① 状态栏「API 已连接」为静态文本 + 绿色椭圆硬编码，实际断连仍显示已连接（D3/D4）② 标题「欢迎登录」SemiBold 未达标题 Bold 规范（D2）③ 版权行 `#8D6E63`、分隔线 `#C9C1B9` 硬编码，有对应 token 未复用（D2）④ 表单区/状态栏多处间距 6/10/14/2px 未对齐 4px 网格（D2）⑤ `PenCheckBox` 样式内 `FontFamily` Setter 触发 WPF 编译器 MC3029（继承 DP 解析 bug） | ① Ellipse + 文本绑定 `ApiStatus`/`ApiStatusMessage`，DataTrigger 三态着色（Healthy 绿/Unhealthy 红/Checking 灰）② SemiBold→Bold ③ 换 `PenTextSecondaryBrush`/`PenBorderSoftBrush` ④ 间距对齐 4px 网格（6→8、10→12、14→16、2→4）⑤ Setter 改附加属性写法 `TextElement.FontFamily` |
| `LoginView.xaml.cs` | 无 | — |
| `ServerConfigView.xaml` | 标签/遮罩间距 6/10px 未对齐 4px 网格（D2） | 6→8、10→12 |
| `ServerConfigView.xaml.cs` | 无 | — |
| `FirstRunSetupView.xaml` | 欢迎说明 14px、标签 6px、回退提示 2px、本地说明/遮罩 10px 未对齐 4px 网格（D2） | 14→16、6→8、2→4、10→12 |
| `FirstRunSetupView.xaml.cs` | 无 | — |
| `AccountSettingsView.xaml(.cs)` | 无（薄包装，DataContext 由 `App.xaml.cs:122` 显式注册，绑定链正常） | — |

### ViewModel 层（6 个）

| 文件 | 问题 | 修复 |
|------|------|------|
| `LoginViewModel.cs` | 无新增问题（子 VM 组合、代理属性、防重入、事件退订/CTS 均完备） | — |
| `LoginCredentialsViewModel.cs` | 无（`rememberPassword ? Password : null` 等价简写为先前会话已改，审计确认无问题） | — |
| `ConnectionStatusViewModel.cs` | `SyncModeDisplay` async void 探测段有 try-catch（注释说明）；事件处理器经 `InvokeAsync` lambda 内部捕获——可接受，未改 | — |
| `ServerConfigViewModel.cs` | `_ = SetModeAsync(Remote)` fire-and-forget，异步阶段异常变未观察任务异常（D6） | 改 `await`（方法已 async，异常落入 catch → 友好提示） |
| `FirstRunSetupViewModel.cs` | ① `SaveRemoteAsync` 同左 fire-and-forget（D6）② `UseLocalMode` 同步命令内 `_ = SetModeAsync(Local)` 异步异常逃逸（D6） | ① 改 `await` ② 命令改 `async Task` + `await` |
| `AccountSettingsViewModel.cs` | ① `OnNavigatedTo` 为 `async void` 且参数解析无 try-catch（D6）② 保存/改密命令 CanExecute 不随输入与 IsBusy 刷新（D4——初始姓名空时按钮永久禁用）③ 加载用户资料失败仅记日志无用户提示（D4） | ① 去 async void + 参数解析 try-catch 回退默认 Tab，`_ = LoadUserProfileAsync()`（内部已捕获）② `[NotifyCanExecuteChangedFor]` + `OnIsBusyChangedCore` 重写通知 ③ 补 Toast 提示 |

### Service 层（7 个）

| 文件 | 问题 | 修复 |
|------|------|------|
| `LoginCoordinator.cs` | ① `Dispose` 未退订 `_stateMachine.StateChanged`（D5 事件泄漏）② `StateChanged?.Invoke` 无逐订阅者保护，订阅者异常中断链路（D6，与 `RaiseLoginSucceeded` 模式不一致） | ① Dispose 补退订 ② 逐订阅者 try-catch 触发 |
| `LoginStateManager.cs` | ① `ApplyLoginSuccess` Title 角色映射不完整——Receptionist 误显示「医生」（D4）② `PerformLogoutAsync` 中 `_tokenLifecycleService.Reset()` 在 try 外，异常中断登出流程（D6） | ① 复用 `CurrentUserRoleDisplay` 完整映射（SuperAdmin→超级管理员/Admin→管理员/Doctor→医生/Receptionist→前台），移除 `SystemConstants` 引用 ② Reset 包 try-catch 继续流程 |
| `ConnectionModeService.cs` | ① 双重 `/// <summary>` 注释（代码质量）② `switch` 缩进错乱 ③ `FireAndForgetRemoteProbeAsync` catch 过滤 `OperationCanceledException`——3s 守卫超时取消逃逸为未观察任务异常（D6） | ① 修复注释 ② 修复缩进 ③ 拆显式 `OperationCanceledException` 分支（记录 debug，预期路径） |
| `ConnectionSettingsService.cs` | **D1 核心 bug**：`IsLocal => CurrentUrl.Contains("localhost") \|\| Contains("127.0.0.1")`——把本机部署的远程 WebAPI（`http://localhost:5000`）误判为本地 → `SwitchingApiClient` 选错客户端实现（HttpClientApiClient 而非 RefitApiClient）、`ConnectionModeService` 模式推导错（Local 而非 Remote） | `IsLocal => IsLocalUrl(_currentUrl)`（仅 `localhost:5300` / `127.0.0.1:5300` 视为本地），与 `SetUrlAsync`/`IsLocalUrl` 判定口径统一；接口注释同步 |
| `CredentialVault.cs` | 无（DPAPI + HMAC 完整性校验完备，异常路径全覆盖） | — |
| `UsernameStorageService.cs` | 无（`await Task.CompletedTask` 无害惯用法，保留） | — |
| `ILoginCoordinator.cs` | 无（接口契约稳定，`LoginSuccessEventArgs` 空参保护完备） | — |

### 控件层（2 个）

| 文件 | 问题 | 修复 |
|------|------|------|
| `AccountSettingsControl.xaml` | 6 处标签间距 6px 未对齐 4px 网格（D2） | 6→8 |
| `AccountSettingsControl.xaml.cs` | 无（PasswordBox 同步守卫 `_isSyncingPassword` 正确） | — |

## 二、修复汇总（按维度）

- **D1 远程/本地切换**：修复 `IsLocal` 语义（`localhost:5000` 误判），切换流程验证通过——`SetModeAsync` 先持久化 URL 再 `ApplyMode` 后后台探测（fire-and-forget），`UrlChanged` 事件回流驱动 `CurrentServerUrl` 绑定与模式推导，重入守卫 `_isSwitching` 防循环，均已就位。
- **D2 字体与样式**：标题 Bold、token 复用（版权/分隔线）、4px 间距网格（表单区/状态栏/对话框/账户设置控件）。**有意保留**：左侧品牌区设计稿精确间距（22/18/6px）、品牌主标题 36px、对话框标题 16px（MDIX 对话框惯例）、状态色（#5B8FA8/#228B22/#E65100 语义色无对应 token）。
- **D3 绑定一致性**：状态栏静态文案 → 实时绑定 `ApiStatusMessage`/`ApiStatus`；DataContext 链审计通过（LoginView AutoWire + 子 VM 代理转发 + 事件转发；AccountSettingsControl 显式注册映射）；PasswordBox 绑定经 `PasswordBoxHelper`（BindsTwoWayByDefault + 内部防重入）正确。
- **D4 命令与交互**：Title 角色映射补全；AccountSettings 保存/改密 CanExecute 联动；登录按钮防重入（`!IsLoading` + AsyncRelayCommand 拒并发）确认；错误提示均为中文友好文案（`ClientErrorMessageMapper` 脱敏）。
- **D5 DI 注入**：LoginCoordinator Dispose 补事件退订；可选依赖 `?` 注入与回退路径审计通过；各 VM 构造参数完整（HEAD 74194465a 已注入 `IConnectionSettingsService`）。
- **D6 异常处理**：5 处 fire-and-forget 未观察任务异常修复（SetModeAsync×3、后台探测 OperationCanceled、Token 重置）；`async void` 去化（OnNavigatedTo）；StateChanged 订阅者隔离；错误消息不暴露 `HttpRequestException` 等技术细节。

## 三、连带测试同步（HEAD 存量回归）

审计与构建门禁暴露 5 个测试项目错误（**HEAD 基线验证复现**，先前会话提交 74194465a/b3ceb17d6 引入）：

| 文件 | 问题 | 修复 |
|------|------|------|
| `ConnectionStatusViewModelTests.cs` | CS7036：构造函数已注入第 4 参数 `IConnectionSettingsService`（74194465a），测试仍调 3 参数 | 补 NSubstitute mock + CreateSut 传参 |
| `Herb/Formula/User/PatientMasterDetailViewModelTests.cs` | CA1001×4：`EditorViewModelBase` 实现 IDisposable 后测试类持有 editor 字段未释放 | 测试类实现 IDisposable + `Dispose()` 释放 |

## 四、验证

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告** |
| `dotnet test tests/LYBT.Tests.Architecture/` | **97/97 通过** |
| 相关单测（Login/Auth/AccountSettings/MasterDetail×4） | **129/129 通过** |
| 双控制器树 | 本任务无权限/端点变更，无需双端检查 |

---

## 六、独立验收附录（验收必自己跑）

主体重构由协作会话提交 `858dcd7b9` 后，独立复验全量构建 + 测试，另发现并修复 2 个**陈旧单元测试**（编码重构前阻塞式切换语义，与 D1「先切 URL 再后台探测」设计冲突，非本次回归——HEAD 74194465a 已可复现失败）：

| 文件 | 陈旧断言（旧语义） | 同步为新设计语义 |
|------|------|------|
| `ConnectionModeServiceTests.SetMode_Remote_Unreachable_ReturnsBlocked` | 远程不可达 → `REMOTE_UNREACHABLE` 阻断、保持本地 | 切换立即成功（仅缺 URL 阻断）；后台探测更新 `IsRemoteAvailable=false` |
| `ConnectionModeServiceTests.OnUrlChanged_CallsSetModeAsync` | URL 变更推导远程 → 守卫阻断 → 保持本地 | URL 驱动切换立即生效（远程模式）；不可达由后台探测反映 |

连带同步：`IConnectionModeService.SetModeAsync` 接口文档修正（原「切换失败（守卫阻断/不可达）时保持切换前模式」与实现不符——现明确仅 `NO_REMOTE_URL` 阻断，不可达/未完成医案守卫在后台探测执行），代码-文档一致性红线。

**独立验证结果**：

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告** |
| `dotnet test tests/LYBT.Tests.Architecture/` | **97/97 通过** |
| 相关单测（ConnectionModeService+Login/Auth 35/35；Unit 命名空间 55/55） | **全部通过** |
| `WorkflowStepIndicatorTests` ×6 | 存量基线：`调用线程必须为 STA`（WPF 线程，重构未触及，13c 已登记） |
| `Integration.RemoteApi.*` | 存量基线：localhost:5000 远程 E2E，本地未启动（技能文档已登记） |
