# Sprint 6 - 发现记录

## 需求理解

从 v2.0 提前到 v1.0 的功能:
- SYNC-D02: DataSource 抽象层废除 (消除 5 对 Remote/Local DataSource 重复实现)
- SYNC-D03: 运行时远程/本地模式切换 (无需重启应用)
- D2: 诊所信息配置化 (替代硬编码 ClinicName/Department)
- D1: PDF 处方导出 (XPS 通用性差)
- C2: 照片 DPAPI 加密存储
- D3: 草稿水印

## 架构调研

### 当前 DataSource 层分析

文件清单 (待删除 ~24 个):
- 接口 (6): IDataSourceBase, IPatientDataSource, IHerbDataSource, IFormulaDataSource, IMedicalCaseDataSource, IUserDataSource (+ IRegistrationDataSource)
- Remote 实现 (6): RemotePatientDataSource, RemoteHerbDataSource, RemoteFormulaDataSource, RemoteMedicalCaseDataSource, RemoteUserDataSource, RemoteRegistrationDataSource
- Local 实现 (6): LocalPatientDataSource, LocalHerbDataSource, LocalFormulaDataSource, LocalMedicalCaseDataSource, LocalUserDataSource, LocalRegistrationDataSource
- Mapperly 映射器 (5): 编译时生成，删除源文件后自动消失

引用方 (需重构):
- 6 个 Repository (各注入 IXxxDataSource)
- DataSourceRegistrationExtensions.cs (DI 注册入口)
- ModeSwitchValidator (注入 IMedicalCaseDataSource)
- DesktopLayerArchTests.cs (架构测试)

### 关键约束

远程模式走 HTTP API (Refit) -> Server -> SQL Server
本地模式走 EF Core -> SQL Server LocalDB
两条路径本质不同，无法用单一 DbContext 统一。

### Gemini 审核发现 (2026-03-09)

1. **Singleton 陈旧依赖 (HIGH)**: Singleton 注入 Repository 后切换模式会持有旧实例。必须用 Func<T> 或 IConnectionModeProvider
2. **MenuManager 固定模式 (HIGH)**: 当前构造函数注入 ConnectionMode 枚举，切换模式后菜单可见性不变
3. **活跃医案检查 (HIGH)**: 切换前必须查询 IActiveConsultationService + 脏数据检查
4. **导航日志残留 (MEDIUM)**: Clear Region 后必须 ClearHistory()
5. **异步操作中断 (MEDIUM)**: CancellationToken 传播 + 切换时触发取消
6. **D2 热更新 (LOW)**: 使用 IOptionsMonitor 替代 IOptions

### 方案对比

| 方案 | 复杂度 | UX | 技术风险 |
|------|--------|-----|---------|
| A: 当前 DataSource 策略 | 中 | 低 (启动固定) | 低 |
| **B: Factory Dual-Repo (A+)** | **中** | **高 (动态)** | **中 (陈旧依赖)** |
| C: Proxy Repository (热交换) | 高 | 最佳 (无 UI 重置) | 高 |
| D: 应用重启 | 低 | 低 | 最低 |

Gemini 结论: **方案 B (A+) 最平衡**。

---

## Singleton 依赖审计 (Phase 1.3)

### CRITICAL: 直接注入 DataSource/Repository 的 Singleton

| Singleton | 注入的危险依赖 | 注册位置 | 修复方案 |
|-----------|---------------|----------|----------|
| **IUserRepository** | IUserDataSource + IUserApi? | UsersModule.cs:31 | 改 Transient 或注入 Func<> |
| **IPatientRepository** | IPatientDataSource + IPatientApi? | PatientsModule.cs:36 | 改 Transient 或注入 Func<> |
| **IHerbRepository** | IHerbDataSource + IHerbApi? | HerbsModule.cs:31 | 改 Transient 或注入 Func<> |
| **IFormulaRepository** | IFormulaDataSource + IFormulaApi? | FormulaModule.cs:29 | 改 Transient 或注入 Func<> |
| **IMedicalCaseRepository** | IMedicalCaseDataSource + IMedicalCaseApi? | MedicalCaseModule.cs:41 | 改 Transient 或注入 Func<> |
| **ISyncService** | LocalDbContext (Singleton) | DataSourceRegistrationExtensions.cs:94 | 注入 Func<LocalDbContext> 或改 Transient |
| **IModeSwitchValidator** | IMedicalCaseDataSource (Singleton factory) | DataSourceRegistrationExtensions.cs:114 | 懒解析 DataSource |

### MEDIUM: 注入固定值的 Singleton

| Singleton | 注入的危险依赖 | 注册位置 | 修复方案 |
|-----------|---------------|----------|----------|
| **MenuManager** | ConnectionMode 枚举 (固定值) | ServiceCollectionExtensions.cs:138 | 改注入 IConnectionModeProvider |

### SAFE: 无 DataSource/Repository 依赖的 Singleton (38个)

IMemoryCache, IDesktopCacheManager, IAuthenticationService, ITokenStorageService, ITokenManager, ICredentialVault, IAuthenticationStateMachine, ILogoutService, ITokenValidator, IUsernameStorageService, ISystemSettingsService, IApiHealthCheckService, IApiService, IStartupOptimizationService, ITokenLifecycleService, INotificationService, IDesktopExceptionHandler, INavigationCoordinator, ISessionManager, IActiveConsultationService, IApplicationTickService, UserActivityTracker, IUserNotificationService, IMainWindowServicesFacade, IPrescriptionSettingsService, IClinicSettingsService, ICommonDialogService, IRoleRegistry, IApplicationCommands, IModuleLoadingService, IApplicationInitializationService, IApplicationStateService, ISessionLifecycleManager, ILoginCoordinator, IStartupPipeline, IHealthCheckCoordinator, ICurrentUserProvider, ILocalAuthService, ILocalDbBackupService

### 修复策略

**Phase 1 (SYNC-D02)**: Repository 重构后，DataSource 层被移除，Repository 直接包含双模式逻辑。5 个 Repository 内部使用 IConnectionModeProvider 判断模式，通过工厂解析对应的 API 或 DbContext。

**Phase 2 (SYNC-D03)**: MenuManager 和 ModeSwitchValidator 重构为注入 IConnectionModeProvider，响应运行时模式切换。
