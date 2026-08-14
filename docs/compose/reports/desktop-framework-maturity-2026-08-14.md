# Desktop 框架使用成熟度报告

> 分析日期：2026-08-14  
> 分析范围：`src/Client/Desktop/`（553 个 .cs 文件，15 个项目）  
> 项目规模：~19,000 行代码（前 200 文件采样）

---

## 总览

| 成熟度等级 | 数量 | 占比 |
|-----------|------|------|
| 🟢 成熟框架（直接采用） | 7 | 50% |
| 🟡 部分自定义（框架 + 适配层） | 4 | 29% |
| 🔴 完全自定义 | 3 | 21% |

**整体评价**：项目框架选型成熟度较高，核心框架（Prism、MDIX、Refit、QuestPDF、Serilog、Velopack）均为业界主流成熟方案。自定义部分集中在业务基础设施层（ViewModel 基类体系、双模式切换、读卡器），设计合理、职责清晰。

---

## 1. MVVM 框架：Prism.DryIoc

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| DI 容器 | 🟢 成熟 | DryIoc，Prism 8.1.97 默认容器，深度使用 |
| 模块化 | 🟢 成熟 | 6 个业务模块（Auth/Patients/Catalog/MedicalCase/Registrations/Users）+ 2 个角色模块（Admin/Clinical）+ Printing 模块 |
| 导航 | 🟢 成熟 | IRegionManager + RegionMonitor + NavigationCoordinator + NavigationHistoryService，9 个文件涉及 RegionManager |
| 事件聚合器 | 🟡 部分自定义 | IEventAggregator 使用 15 个文件，但封装了 `EventSubscriptionManager` 管理订阅生命周期 |
| ViewModel 基类 | 🔴 完全自定义 | 7 个自定义基类（见下表） |

**Prism 功能使用深度**：

| Prism 功能 | 使用情况 | 文件数 |
|-----------|---------|--------|
| `IModule` 模块注册 | ✅ 全面使用 | 8+ 个 Module 类 |
| `RegisterForNavigation` | ✅ 全面使用 | 各模块 |
| `IRegionManager` | ✅ 全面使用 | 9 个文件 |
| `IEventAggregator` | ✅ 使用 | 15 个文件 |
| `IContainerRegistry` | ✅ 使用 | 各模块 RegisterTypes |
| `PrismApplication` | ✅ 入口 | App.xaml.cs |
| `IConfirmNavigationRequest` | ✅ 使用 | NavigableViewModelBase |
| `IRegionMemberLifetime` | ✅ 使用 | NavigableViewModelBase |
| `ViewModelLocator` | ✅ XAML 自动绑定 | 各 View |
| `DelegateCommand` | ❌ 不使用 | 被 CommunityToolkit.Mvvm 替代 |

**ViewModel 基类体系（完全自定义，7 个基类）**：

| 基类 | 职责 | 是否需要保留 |
|------|------|------------|
| `NavigableViewModelBase` | 核心基类：服务聚合/状态管理/Prism 导航/IDisposable | ✅ 必须保留（项目核心抽象） |
| `NavigableViewModelBase.Navigation` | partial：导航参数提取/区域导航方法 | ✅ 必须保留 |
| `NavigableViewModelBase.Editable` | partial：对话框/编辑状态/资源释放 | ✅ 必须保留 |
| `EditorViewModelBase` | 编辑器视图模型基类 | ✅ 必须保留 |
| `MasterDetailViewModelBase` | 主从详情视图模型基类 | ✅ 必须保留（8 个模块使用） |
| `DialogViewModelBase` | 对话框视图模型基类 | ✅ 必须保留 |
| `ValidatableModelBase` | FluentValidation 验证基类 | ⚠️ 可选（FluentValidation 仅 1 处使用） |
| `HerbItemViewModelBase` | 药材条目专用基类 | ⚠️ 业务专用，保留 |

**优化建议**：
- ViewModel 基类体系设计成熟，层次清晰，不建议替换
- `CommunityToolkit.Mvvm`（270 个文件使用 `[ObservableProperty]`/`[RelayCommand]`）已完全替代 Prism 的 `BindableBase`/`DelegateCommand`，这是正确选择
- `EventSubscriptionManager` 封装合理，解决了 Prism 事件订阅的内存泄漏风险

---

## 2. UI 框架：MDIX（MaterialDesignThemes）

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| 包引用 | 🟢 成熟 | MaterialDesignThemes 5.3.2 |
| 样式使用 | 🟢 成熟 | MDIX 内置样式 + Spacing Token |
| 自定义模板 | ✅ 无 | 按规范不自定义 ControlTemplate |

**项目约定**：UI 用 MDIX 内置样式 + Spacing Token，不自定义 ControlTemplate。

**优化建议**：无，MDIX 是 WPF 最成熟的 Material Design 实现，选型正确。

---

## 3. HTTP 客户端：Refit + 自定义双模式

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| Refit 接口定义 | 🟢 成熟 | 11 个 Refit 接口（IAuthApi/IPatientApi/IHerbApi 等），编译时代码生成 |
| 双模式切换 | 🟡 部分自定义 | `SwitchingApiClient` 根据 URL 自动路由 |
| HttpClient 管理 | 🟢 成熟 | `IHttpClientFactory` + `AuthorizationMessageHandler` + `TokenRefreshHandler` |
| 重试策略 | 🟢 成熟 | Polly（NuGet 引用），通过 `Microsoft.Extensions.Http.Polly` |

**SwitchingApiClient 架构**（121 行，设计精良）：
- `localhost:5300` → `HttpClientApiClient`（LocalWebAPI）
- 其他地址 → `RefitApiClient`（Remote WebAPI）
- 双重检查锁 + 无锁快路径（99.9% 命中）
- Repository 层完全无感知

**优化建议**：SwitchingApiClient 是项目核心创新，设计合理，无需替换。

---

## 4. 数据访问

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| ORM | ❌ 不使用 | Desktop 端无 ORM，无 EF Core 直接引用 |
| 数据访问模式 | 🟢 成熟 | HTTP Repository 模式：`I{Entity}Repository` → `SwitchingApiClient` → LocalWebAPI/Remote API |
| 本地模式 | 🟡 部分自定义 | 嵌入式 LocalWebAPI（ASP.NET Core in-process），通过 HTTP 访问 LocalDB |
| CRUD 基类 | 🟡 部分自定义 | `CrudServiceBase` + `ApiClientRepositoryBase` + `EntityApiClientRepositoryBase` |

**关键设计决策**：
- Desktop 端**不直接访问数据库**，所有数据操作走 HTTP（LocalWebAPI 或 Remote API）
- 旧的 `LocalDbContext` 已废弃移入测试项目
- Service 层提供业务逻辑，Repository 层提供 CRUD 抽象

**优化建议**：HTTP Repository 模式是双模式架构的正确选择，无需引入 ORM。

---

## 5. 配置管理

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| appsettings.json | 🟢 成熟 | Shell 级 appsettings.json |
| clinic-settings | 🟡 部分自定义 | `IClinicSettingsService` + `ClinicSettingsService` |
| 连接设置 | 🟡 部分自定义 | `IConnectionSettingsService` + `ConnectionSettingsService` |
| Feature Toggle | 🟡 部分自定义 | `IFeatureToggleService` + `FeatureToggleOptions` |

**优化建议**：配置管理层次清晰，`IOptions<T>` 模式标准，无需调整。

---

## 6. 认证/安全

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| JWT Token 管理 | 🟢 成熟 | 完整的 Token 生命周期：存储→刷新→验证→登出 |
| 凭证存储 | 🟢 成熟 | `CredentialVault` + `DpapiProtector`（Windows DPAPI 加密） |
| 认证状态机 | 🟡 部分自定义 | `AuthenticationStateMachine` 管理认证状态流转 |
| Token 存储策略 | 🟢 合规 | 内存存储（Session 级别），不持久化到磁盘 |
| 照片存储 | 🟡 部分自定义 | `DpapiPhotoStorageService`（DPAPI 加密存储） |

**安全文件清单**（Foundation/Security/，19 个文件）：
- `AuthenticationService` / `AuthenticationStateMachine`
- `TokenStorageService` / `TokenLifecycleService` / `TokenRefreshHandler`
- `CredentialVault` / `CredentialStorage` / `DpapiProtector`
- `LogoutService` / `LocalTokenValidator`
- `UsernameStorageService` / `PhotoStorageService`

**优化建议**：认证/安全层设计成熟，符合医疗系统合规要求（Session 级 Token、DPAPI 加密）。不建议替换。

---

## 7. 日志：Serilog

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| 包引用 | 🟢 成熟 | Serilog 4.3.1 + 多 Sink（File/Console/MSSqlServer） |
| 集成深度 | 🟢 成熟 | 156 个文件使用 ILogger |
| 启动日志 | 🟢 成熟 | 版本/Commit/PID 启动日志 |
| 两阶段日志 | 🟢 成熟 | Bootstrap Logger → Final Logger |
| 日志关闭 | 🟢 成熟 | `CloseAndFlush()` 在 OnExit 中调用 |

**优化建议**：Serilog 集成完善，无需调整。

---

## 8. 打印：QuestPDF

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| 包引用 | 🟢 成熟 | QuestPDF 2025.12.4 |
| PDF 导出 | 🟢 成熟 | `PrescriptionPdfExporter`（214 行，A5 处方笺） |
| 打印预览 | 🟢 成熟 | WPF FixedDocument + XPS |
| 模板系统 | 🟢 成熟 | 4 个 XAML 模板（A5/A4 × 普通/续页） |
| 泛型接口 | 🟢 成熟 | `IPrintService<TModel>` 类型安全 |

**优化建议**：打印模块设计成熟，泛型接口支持扩展，无需调整。

---

## 9. 读卡器：硬件集成

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| 抽象层 | 🟢 成熟 | `ICardReader` / `ICardReaderFactory` / `ICardReaderDiagnostics` |
| 具体实现 | 🔴 完全自定义 | `HuaDaHD100CardReader`（P/Invoke 调用 HDstdapi.dll） |
| Mock 实现 | 🟢 有 | `MockCardReader` 用于测试 |
| 诊断服务 | 🟡 部分自定义 | `CardReaderDiagnosticsService` |
| 集成层 | 🟡 部分自定义 | `IPatientCardReaderIntegration` |

**文件清单**（15 个文件）：
- Abstractions: `ICardReader`, `ICardReaderFactory`, `ICardReaderDiagnostics`, `CardReaderDeviceInfo`
- Adapters: `HuaDaHD100CardReader`, `MockCardReader`
- Native: `HuaDaNativeMethods`（P/Invoke）
- Services: `CardReaderService`, `CardReaderFactory`, `CardReaderDiagnosticsService`
- Models: `CardReadResult`, `CardReaderDiagnosticReport`
- Integration: `IPatientCardReaderIntegration`

**优化建议**：
- 读卡器是硬件集成，必须自定义，没有现成库可替代
- 抽象层（ICardReader）设计良好，支持多设备扩展
- 可考虑未来支持更多读卡器型号（通过 ICardReaderFactory）

---

## 10. 双模式：远程/本地切换

| 维度 | 成熟度 | 说明 |
|------|--------|------|
| 切换机制 | 🟡 部分自定义 | URL 路由切换（`SwitchingApiClient`） |
| 本地 WebAPI | 🟡 部分自定义 | 嵌入式 ASP.NET Core（`LocalWebAPI/`，12 个 Controller） |
| 连接检测 | 🟡 部分自定义 | `IApiHealthCheckService` + `ApiHealthCheckService` |
| 模式切换 | 🟡 部分自定义 | `IConnectionModeService` + `ConnectionModeService` |

**架构设计**：
```
Desktop App → SwitchingApiClient
  ├── localhost:5300 → HttpClientApiClient → LocalWebAPI (in-process) → LocalDB
  └── other:5000 → RefitApiClient → Remote WebAPI → SQL Server
```

**优化建议**：双模式是项目核心架构，SwitchingApiClient 设计精良（双重检查锁 + 无锁快路径），无需替换。

---

## 11. 其他框架/库

| 技术 | 成熟度 | 说明 |
|------|--------|------|
| CommunityToolkit.Mvvm 8.4.2 | 🟢 成熟 | 270 个文件使用，完全替代 Prism BindableBase |
| Riok.Mapperly 4.3.1 | 🟢 成熟 | 编译时对象映射，替代 AutoMapper |
| FluentValidation 12.1.1 | ⚠️ 低使用 | 仅 1 处引用（PatientsModule），大量验证通过业务逻辑处理 |
| SignalR Client 8.0.26 | 🟢 成熟 | 注册模块实时通知（`SignalRClient`） |
| Velopack 1.2.0 | 🟢 成熟 | 自动更新框架（`DesktopUpdateService`） |
| System.Reactive 6.1.0 | ⚠️ 低使用 | Rx 引用但使用有限 |
| MediatR 12.4.1 | ❌ 未使用 | Desktop 端无实际引用（仅生成代码中的接口匹配） |
| Polly 8.6.6 | 🟢 成熟 | HTTP 重试策略 |
| SixLabors.Fonts/ImageSharp | 🟢 成熟 | QuestPDF 依赖，显式引用修复安全漏洞 |

---

## 优化建议汇总

### 无需改动（成熟框架，选型正确）
1. **Prism.DryIoc** — MVVM 框架选型正确，深度使用
2. **CommunityToolkit.Mvvm** — 270 个文件使用，完全替代 Prism MVVM
3. **MDIX** — WPF Material Design 最佳选择
4. **Refit** — 编译时 HTTP 客户端，配合 SwitchingApiClient
5. **QuestPDF** — PDF 生成，泛型打印接口设计良好
6. **Serilog** — 结构化日志，156 个文件集成
7. **Velopack** — 自动更新，标准方案
8. **SignalR Client** — 实时通知，标准方案
9. **Riok.Mapperly** — 编译时映射，优于 AutoMapper

### 可考虑优化（低使用率/可替代）
1. **FluentValidation** — 仅 1 处引用，要么全项目推广验证，要么移除依赖减少包体积
2. **MediatR** — Desktop 端无实际使用，可从 Directory.Packages.props 移除 Desktop 侧引用
3. **System.Reactive** — 使用有限，评估是否真正需要

### 必须保留（自定义但设计合理）
1. **ViewModel 基类体系**（7 个基类）— 项目核心抽象，层次清晰
2. **SwitchingApiClient** — 双模式核心，设计精良
3. **CardReader 抽象层** — 硬件集成必须自定义
4. **EventSubscriptionManager** — 解决 Prism 事件内存泄漏
5. **NavigationCoordinator + RegionMonitor** — 导航增强

### 可考虑引入
1. **CommunityToolkit.Mvvm 的 `[NotifyPropertyChangedFor]`** — 已在使用，继续保持
2. **Polly 策略配置化** — 当前硬编码，可考虑从配置读取重试策略

---

## 附：项目结构概览

```
Desktop/
├── Shell/                    # PrismApplication 入口（1 项目）
├── Core/
│   ├── Contracts/            # 接口定义（38 个 Service 接口 + API 契约）
│   ├── Foundation/           # HTTP/Security/Config（63 个文件）
│   ├── Infrastructure/       # WPF 服务/ViewModel 基类（103 个文件）
│   ├── Controls/             # WPF 自定义控件（69 个文件）
│   └── Printing/             # QuestPDF 打印模块
├── Modules/                  # 6 个业务模块
│   ├── Auth/                 # 登录/连接测试
│   ├── Patients/             # 患者管理
│   ├── Catalog/              # 药材/验方目录
│   ├── MedicalCase/          # 医案管理
│   ├── Registrations/        # 挂号（含 SignalR）
│   └── Users/                # 用户管理
├── Roles/                    # 角色工作区
│   ├── Admin/                # 管理员 + 运维
│   └── Clinical/             # 临床 + 前台
└── LocalWebAPI/              # 嵌入式 ASP.NET Core（12 个 Controller）
```
