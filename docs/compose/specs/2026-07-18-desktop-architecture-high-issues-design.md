# Desktop 架构 HIGH 问题全面修复设计（修正版）

## [S1] 问题

多角度审计发现 8 个 HIGH 级别问题。经深入验证，部分 "God Class" 实际已正确分解：

| 问题 | 原判断 | 验证结果 |
|------|--------|----------|
| MedicalCaseWorkspaceViewModel (784行) | God Class | **已分解** — 内含 3 个子 VM（ConsultationEditor、PrescriptionEditor、MedicalCaseCommands），784 行是 Composite Shell 编排逻辑 |
| MedicalCaseCommandsViewModel (555行) | God Class | **已分解** — 本身就是子 VM，9 个命令共享 context。架构评审明确决定保持单体 |
| LoginViewModel (705行) | God Class | **真正需要拆分** — 混合凭证 UI + 连接状态 + 登录流程 3 个关注点 |
| 命令模式分裂 | 62 DelegateCommand vs 70 [RelayCommand] | 确认存在，需统一 |
| 测试缺口 | 22 个 VM 无测试 | 确认存在 |
| 死代码 | ISettingsService/SettingsService 等 | 确认存在 |
| DI 缺陷 | PrescriptionPrintHandler | 需验证 |

## [S2] Phase 1: LoginViewModel 拆分

### 现状分析

LoginViewModel (705行) 已委托 6 个服务（ILoginCoordinator、IApplicationStateService、IUsernameStorageService、ICredentialVault、IConnectionModeService、IConnectionSettingsService），但 UI 状态层混合了 3 个关注点：

1. **凭证 UI** (~80行): Username、Password、RememberUsername、RememberPassword、HasSavedPassword
2. **连接状态 + 模式切换** (~180行): ApiStatus、ApiStatusMessage、IsRemoteMode、IsRemoteAvailable、切换命令
3. **登录编排 + 初始化** (~200行): ExecuteLoginAsync、BackgroundInitAsync、FirstRun 检查

### 拆分方案

```
LoginViewModel (协调器, ~250行)
├── LoginCredentialsViewModel (~100行) — 用户名/密码/记住密码 UI 状态
├── ConnectionStatusViewModel (~150行) — API 状态/连接模式/切换命令
└── [已有] ServerConfigViewModel — 服务器配置对话框
```

### XAML 绑定影响

当前 LoginView.xaml 绑定路径：
- `Username`, `Password`, `RememberUsername`, `RememberPassword` → 移到 `Credentials.Username` 等
- `ApiStatus`, `ApiStatusMessage`, `IsRemoteMode`, `IsRemoteAvailable` → 移到 `ConnectionStatus.*`
- `LoginCommand`, `SwitchToLocalCommand`, `SwitchToRemoteCommand` → 保留在 LoginViewModel

### 关键约束

- 子 VM 通过构造函数注入到 LoginViewModel
- LoginViewModel 保留 LoginCommand（核心登录流程）
- XAML 绑定路径变更需同步更新 LoginView.xaml
- 保持向后兼容：对外公开属性不变（通过 LoginViewModel 代理）

## [S3] Phase 2: 命令模式统一

将 DelegateCommand 迁移到 [RelayCommand]：

| 优先级 | 文件 | DelegateCommand 数 | 处理方式 |
|--------|------|-------------------|----------|
| P0 | `LoginViewModel.cs` | 6 | Phase 1 重构时同步迁移 |
| P1 | `MenuManager.cs` | 11 | 非 ViewModel，保持 DelegateCommand（架构测试已白名单） |
| P2 | `SearchBox.xaml.cs` | 1 | Code-behind，保持不变 |
| P2 | `BaseDetailContainer.xaml.cs` | 1 | 基础设施，保持不变 |

**注意**: MedicalCaseWorkspaceViewModel (4处) 和 MedicalCaseCommandsViewModel (9处) 的 DelegateCommand 保持不变 — 它们的 CanExecute 依赖跨 VM 边界的 State，DelegateCommand.ObservesProperty 无法跨 VM 工作。

## [S4] Phase 3: 测试 + 死代码 + DI

### 测试覆盖（P0 高风险）
- ReceptionistHomeViewModel (340行)
- CardReaderViewModel (381行)
- RegistrationListViewModel (328行)
- ClinicalWorkspaceViewModel (~200行)

### 死代码清理
- 删除 ISettingsService + SettingsService（从未注册 DI，零消费者）
- 删除 Foundation 版 LocalWebApiHttpClientFactory（Shell 版已覆盖）
- 删除 HttpServiceRegistrationExtensions 空壳

### DI 验证与修复
- 检查 PrescriptionPrintHandler 是否在 MedicalCaseModule 注册
- 如缺失则添加注册

## [S5] 不修复的问题（经验证无需处理）

| 问题 | 原因 |
|------|------|
| MedicalCaseWorkspaceViewModel 拆分 | 已正确分解为 Composite Shell + 3 子 VM |
| MedicalCaseCommandsViewModel 拆分 | 已是子 VM，架构评审明确保持单体 |
| 数据模型 God Class (PrescriptionItem 21字段) | DTO 不应拆分 |
| 跨层引用 (LocalData → Entities) | LocalDB 模式技术必需 |
| MenuManager DelegateCommand 迁移 | 非 ViewModel，架构测试已白名单 |

## [S6] 约束

- **纯重构**：不改变功能行为
- **逐个 PR**：每个 Phase 独立提交
- **向后兼容**：XAML 绑定路径通过代理属性保持兼容

## [S7] 验证

每个 Phase 完成后运行：
```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Desktop/
```

## [S8] 实施顺序

| Phase | 内容 | 预估工作量 | 依赖 |
|-------|------|-----------|------|
| 1 | LoginViewModel 拆分 | 2-3 小时 | 无 |
| 2 | 命令模式统一（LoginViewModel） | 0.5 小时 | Phase 1 |
| 3 | 测试 + 死代码 + DI | 2-3 小时 | 无 |
