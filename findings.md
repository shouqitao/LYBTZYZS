# Findings: 剩余任务全量清零

## 任务来源
- Sprint 5 剩余 (2026-02-28)
- Code-vs-PRD 审计 (2026-02-28)
- 上一会话已修复 3 项: CODE-07, T5-P3-17, DEAD-12

## Phase A 验证结果 (2026-03-01 Session 4)

### 全部 4 项 CRITICAL 已在先前会话实现
- CODE-01: MedicalCaseStateService.cs:138-143 (TcmDiagnosis 验证)
- CODE-02: MedicalCaseCommandService.cs:208-214,387-393,556-562 (IsPrinted 重置)
- CODE-03: AuthService.cs:227-247 (AutoLoginToken 按 UserId 撤销)
- CODE-04: UsersController.cs:192,285 + SuperAdminOnly 策略 (API 层保护)

### 发现: CODE-04 Service 层防御深度不足 (非原始需求)
- ResetPasswordAsync 缺少 Service 层权限校验 (Controller 已保护)
- RestoreAsync 使用 CanManageUser 而非 SuperAdmin 专属检查
- 风险低: Controller 层已有 SuperAdminOnly 策略保护

## Phase B 调研发现 (2026-03-01)

### B1. CODE-05/06: MedicalCase FK -- 已完成
- MedicalCaseConfiguration.cs 已显式配置 PatientId + UserId FK
- HasOne<Patient>().WithMany().HasForeignKey().IsRequired().OnDelete(Restrict)
- HasOne<User>().WithMany().HasForeignKey().IsRequired().OnDelete(Restrict)
- 无需额外修改

### B2. CODE-11: Herb BatchDelete 引用检查 -- 需补充
- CheckReferenceAsync 仅检查 PrescriptionItems，缺 FormulaHerbItems
- FormulaHerbItem.HerbId 可空 (延迟绑定)，有 Restrict FK 约束
- BatchDeleteAsync 未调用 _cacheInvalidation (单项 Delete 有调用)
- 修复: CheckReferenceAsync 补充 FormulaHerbItems 计数 + BatchDelete 补缓存失效

### B3. T5-P3-06: Desktop 写后缓存失效 -- 需全面补充
- IDesktopCacheManager 仅被 SyncService.InvalidateAll() 调用
- 5 个模块 ViewModel 均未注入 IDesktopCacheManager:
  - HerbMasterDetailViewModel: Create/Update/Delete/ToggleStatus/Restore/Import
  - FormulaMasterDetailViewModel: Create/Update/Delete/ToggleStatus/Clone/Restore
  - PatientMasterDetailViewModel: Create/Update/Delete/Restore/Import/CardReader
  - UserMasterDetailViewModel: Create/Update/Delete/ToggleStatus/Restore/Import
  - MedicalCaseMasterDetailViewModel: Save/Delete
- 修复: 各 ViewModel 注入 IDesktopCacheManager，写操作成功后调用对应 Invalidate 方法

### B4. T5-P2-42: 同步前网络/Token 检查 -- 需补充
- CheckDifferencesAsync 有 SessionManager.IsAuthenticated 检查
- ExecuteSyncAsync 缺认证二次检查
- 两个阶段均无网络连通性检测 (IApiHealthCheckService 存在但未使用)
- 服务器请求失败时静默返回空结果，误导用户
- 修复: 提取 ValidatePreConditionsAsync，检查认证+网络，两阶段复用

## Phase C 验证结果 (2026-03-01 Session 5)

### 全部 3 项 MEDIUM 已在先前会话实现
- T5-P3-03: ProblemDetailsFactory.cs 已添加 Severity 字段关联 (注释 T5-P3-03 标记)
- T5-P3-01: ProductionConfigurationValidator.cs 已实现 Warning 级别配置验证 (注释 T5-P3-01 标记)
- T5-P3-19: AccountSettingsViewModel.cs 已添加 Email 属性和编辑功能 (注释 T5-P3-19 标记)

## E1 调研: DataSource 接口架构 (2026-03-01)

### 接口层次验证
- 基接口: `IDataSourceBase<TDetail, TInput>` 定义 5 个 CRUD 方法
- 5 个实体接口: IPatientDataSource, IHerbDataSource, IFormulaDataSource, IMedicalCaseDataSource, IUserDataSource
- 接口定义在 `LYBT.Desktop.Contracts/DataSources/`
- Remote 实现在 `LYBT.Desktop.Infrastructure/DataSources/Remote/` (依赖 Refit)
- Local 实现在 `LYBT.Desktop.LocalData/DataSources/` (依赖 LocalDbContext)
- DI 注册: `DataSourceRegistrationExtensions` 按 ConnectionMode 枚举切换，所有实现为 Transient

## E2 调研: Sync 端到端调用链 (2026-03-01)

### 双端 ISyncService 确认
- Desktop: `LYBT.Desktop.Contracts.Services.ISyncService` (面向 ViewModel)
- Server: `LYBT.Module.Sync.Interfaces.ISyncService` (面向 Controller)
- 同名不同命名空间，方法签名不同

### CrossModuleService ISP 四接口
- IPatientCrossModuleService: 患者查询 + 引用检查
- IHerbCrossModuleService: 药材查询 + 引用检查 + 价格查询
- IUserCrossModuleService: 用户查询 + 凭证 + 密码/登录状态更新
- ICrossModuleAuthService: Token 撤销
- 实现类: CrossModuleQueryService.cs (318 行)

### 同步依赖顺序
- SupportedTypes = ["Herb", "Patient", "Formula"] (MedicalCase 尚未加入)
- Herb -> Patient -> Formula 顺序由 FK 约束决定
- IgnoreQueryFilters 用于同步场景包含软删除记录
