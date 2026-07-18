# Desktop WPF 架构优化设计

## [S1] 问题

Desktop WPF 应用经过多年迭代，Shell 层的服务注册逻辑散落在 3 个 Extension 文件中（共 448 行），存在职责重叠和冗余注册。主要问题：

1. **双重 API 客户端注册**：`HttpServiceRegistrationExtensions` 注册了 8 个独立 Refit 接口（`IAuthApi`, `IPatientApi`, `IUserApi`, `IHerbApi`, `IFormulaApi`, `IMedicalCaseApi`, `IRegistrationApi`, `IDiagnosticsApi`），同时 `UnifiedApiClientExtensions` 注册了统一的 `IApiClient`（`SwitchingApiClient`）。两套并存造成混乱。
2. **Handler 链重复构建**：旧注册为全局 `HttpClient` 构建了完整 Handler 链（`HttpClientHandler` → `TokenRefreshHandler` → `AuthorizationMessageHandler` → `LoggingHttpHandler` → `BaseUrlDelegatingHandler`），而 `UnifiedApiClientExtensions` 在工厂方法中独立构建了另一条链。两条链的配置可能不一致。
3. **Extension 文件职责不清**：`ServiceCollectionExtensions`（213行）协调调用，但 `HttpServiceRegistrationExtensions`（93行）和 `UnifiedApiClientExtensions`（142行）各自维护独立的配置解析和注册逻辑。
4. **死代码累积**：多个模块包含大量 "已删除"/"已移除" 注释块。

## [S2] 解决方案

### 区域 1: Shell Extensions 整合 + API 层统一

**目标**：将散落在 3 个文件中的注册逻辑整合为清晰的分层结构，统一 API 客户端为 `IApiClient`。

**当前文件结构：**
```
Shell/Extensions/
├── ServiceCollectionExtensions.cs      # 主协调器 (213行)
├── HttpServiceRegistrationExtensions.cs # 旧 Refit 注册 (93行)
├── UnifiedApiClientExtensions.cs        # 新 SwitchingApiClient (142行)
├── ViewModelServicesExtensions.cs       # ViewModel 组合服务
└── ... (其他扩展)
```

**目标文件结构：**
```
Shell/Extensions/
├── ServiceCollectionExtensions.cs      # 主协调器（简化）
├── HttpServiceRegistrationExtensions.cs # 废弃，仅保留 [Obsolete] 壳
├── UnifiedApiClientExtensions.cs        # 保留，作为 IApiClient 的唯一注册点
├── ViewModelServicesExtensions.cs       # 不变
└── ... (其他扩展)
```

### 具体步骤

**Step 1: 标记旧 Refit 注册为 Obsolete**
- 在 `HttpServiceRegistrationExtensions.RegisterHttpServices()` 上添加 `[Obsolete("Use IApiClient (SwitchingApiClient) instead")]`
- 保留现有注册不动，仅添加标记

**Step 2: 迁移使用旧接口的 Repository**
- 扫描所有注入 `IAuthApi`, `IPatientApi`, `IUserApi`, `IHerbApi`, `IFormulaApi`, `IMedicalCaseApi`, `IRegistrationApi`, `IDiagnosticsApi` 的代码
- 迁移到 `IApiClient` 对应的子接口（如 `IApiClient.Users`, `IApiClient.Patients`）
- 确保每个 Repository 只依赖 `IApiClient`

**Step 3: 删除旧注册**
- 从 `ServiceCollectionExtensions.RegisterAllServices()` 中移除 `RegisterHttpServices()` 调用
- 删除 `HttpServiceRegistrationExtensions.cs`
- 从 `UnifiedApiClientExtensions` 中移除不再需要的旧 Handler 依赖（如 `BaseUrlDelegatingHandler` 已被 `SwitchingApiClient` 内部管理）

**Step 4: 清理死代码**
- 搜索并清理 "已删除"/"已移除" 注释块

### 区域 3: MasterDetailViewModelBase 瘦身（后续 PR）

**目标**：减少事件同步 boilerplate，提取命令区块。

- 引入 `PropertyChangedForwarder` 辅助类，用声明式方式订阅子服务属性变更
- 将 `#region 列表命令` 和 `#region 详情命令` 提取为 partial class 或使用 `partial` 方法

### 区域 4: MainWindowViewModel 简化（后续 PR）

**目标**：引入 `IShellServices` 聚合接口。

```csharp
public interface IShellServices
{
    MenuManager Menu { get; }
    NavigationManager Navigation { get; }
    StatusBarManager StatusBar { get; }
    ILoginStateManager LoginState { get; }
    ShellEventCoordinator Events { get; }
    ShellDialogHelper Dialogs { get; }
}
```

构造函数从 11 个参数减少到 3-4 个。

## [S3] 约束

- **纯重构**：不改变任何功能行为、API 契约、Repository 方法签名
- **逐个 PR**：每个区域独立提交，逐步推进
- **向后兼容**：在完全移除旧接口前，确保所有调用方已迁移

## [S4] 验证

每个区域完成后运行：
```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Desktop/
```

## [S5] 实施顺序

| 顺序 | 区域 | 风险 | 预估工作量 |
|------|------|------|-----------|
| 1 | Shell Extensions 整合 + API 统一 | 中 | 2-3 小时 |
| 2 | MasterDetailViewModelBase 瘦身 | 中 | 1-2 小时 |
| 3 | MainWindowViewModel 简化 | 中 | 1 小时 |
| 4 | 死代码清理 + Auth 一致性 | 低 | 30 分钟 |
