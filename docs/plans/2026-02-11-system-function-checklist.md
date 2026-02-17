# 系统功能清单与实现状态评估

> **目的**: 系统分析整个项目的结构和设计，按开发顺序形成由基础到复杂的功能清单，标注实现状态，映射 PRD 需求编号。
>
> **创建时间**: 2026-02-11
> **最后更新**: 2026-02-12
> **数据来源**: 代码扫描 + PRD 文档交叉验证 (893 源文件 / 39 项目 / 120 FR / ~1948 测试)

---

## 一、项目总览

### 1.1 规模指标

| 指标 | 数值 |
|------|------|
| 源项目 | 39 (Server 10 + Desktop 18 + Shared 8 + Tools 4) |
| 测试项目 | ~20 (新体系 5 + 遗留 15) |
| 源文件 (.cs) | ~893 |
| 测试方法 | ~1,948 (新 1,278 + 遗留 670) |
| 编译状态 | **0 错误 / 0 警告** |
| PRD 文档 | 14 模块 / 120 个 FR |
| EF Migrations | 32+ |

### 1.2 架构分层

```
Server:   WebAPI (1) -> Modules (7 active + 2 abandoned) -> Core (Entities + Infrastructure)
Desktop:  Shell (1) -> Modules (7) -> Core (9) -> Roles (2)
Shared:   8 个共享库
```

### 1.3 技术栈

| 层 | 技术 |
|----|------|
| Server | ASP.NET Core 8 + EF Core 8 + SQL Server |
| Desktop | WPF + Prism.DryIoc + CommunityToolkit.Mvvm |
| 本地数据 | SQLite (双模式) |
| 认证 | JWT Bearer Token + DPAPI |
| 日志 | Serilog (结构化) |
| 测试 | xUnit + FluentAssertions + NSubstitute + NetArchTest |

### 1.4 PRD 覆盖总览

| 模块 | FR 范围 | FR 数 | 实现率 |
|------|---------|-------|--------|
| 认证与会话管理 | FR-AUTH-001~013 | 13 | 100% |
| 用户管理 | FR-USER-001~012 | 12 | 92% |
| 患者管理 | FR-PAT-001~012 | 12 | 83% |
| 药材管理 | FR-HERB-001~013 | 13 | 100% |
| 验方管理 | FR-FORM-001~013 | 13 | 100% |
| 医案管理 | FR-MC-001~017 | 17 | 94% |
| 数据同步 | FR-SYNC-001~008 | 8 | 100% |
| 打印 | FR-PRINT-001~004 | 4 | 100% |
| 身份证读卡器 | FR-CARD-001~002 | 2 | 100% |
| 系统健康与诊断 | FR-SYS-001~007 | 7 | 100% |
| 异常处理策略 | FR-ERR-001~005 | 5 | 100% |
| 日志与审计 | FR-LOG-001~004 | 4 | 100% |
| Desktop Shell | FR-SHELL-001~007 | 7 | 100% |
| 配置参数 | FR-CFG-001~003 | 3 | 100% |
| **合计** | | **120** | **97.5%** |

---

## 二、功能清单 (由基础到复杂)

> 说明:
> - **DONE** = 代码已实现且可工作
> - **GAP** = 存在明确缺口，需补充实现
> - **PARTIAL** = 部分实现，有残留 TODO
> - 编号格式: `L{层}.{序号}`，层号越大依赖越多/越复杂
> - FR 列: 对应 PRD 需求编号

---

### Layer 0: 基础设施层 (Foundation)

所有业务模块的底座。无业务逻辑，纯技术组件。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L0.01 | - | EF Core DbContext + 实体定义 | AppDbContext (14 实体) | LocalDbContext (SQLite) | DONE | 154 实体测试 |
| L0.02 | - | 泛型 Repository 模式 | BaseRepository\<T\> + 10 具体实现 | 5 个 LocalDataSource | DONE | 70 LocalData |
| L0.03 | - | 数据库迁移管理 | 32+ Migrations | SQLite 自动建表 | DONE | - |
| L0.04 | FR-ERR-001 | 服务端全局异常处理 | BusinessExceptionHandler + SystemExceptionHandler 链式处理 | - | DONE | 架构测试 |
| L0.05 | FR-ERR-002 | ProblemDetails 标准化 (RFC 7807) | type/title/status/detail/errorCode/correlationId/traceId | ClientProblemDetails 解析 | DONE | 架构测试 |
| L0.06 | FR-ERR-004 | 异常类型体系 | AppException -> Business/NotFound/Conflict/Validation/Unauthorized/Api | ExceptionMessageMapper | DONE | - |
| L0.07 | FR-ERR-003 | 客户端异常处理 | - | DesktopExceptionHandler + SafeExecuteAsync + ServiceResult 模式 | DONE | - |
| L0.08 | FR-ERR-005 | 异常严重度分级 | - | Information/Warning/Error/Critical 四级 | DONE | - |
| L0.09 | FR-LOG-001 | 结构化日志 (Serilog) | CorrelationIdMiddleware + RequestLogging + Console/File/SqlServer | AsyncLocal CorrelationId | DONE | - |
| L0.10 | FR-LOG-003 | 敏感数据脱敏 | SensitiveDataAttribute (5类型4模式) + 文本级正则脱敏 | - | DONE | 4 测试 |
| L0.11 | FR-LOG-004 | 运行时日志级别管理 | LoggingLevelManager (LevelSwitch + Timer) | - | DONE | - |
| L0.12 | FR-CFG-001 | 服务端配置 (12 Options 类) | Jwt/Session/Security/PasswordPolicy/DB/Cache/Swagger 等 | - | DONE | ConfigHelper |
| L0.13 | FR-CFG-002 | 客户端配置 (5 Options 类) | - | ApiClient/ClientSession/FeatureToggles/ClinicSettings/Prescription | DONE | - |
| L0.14 | FR-CFG-003 | 环境配置管理 | appsettings.{Env}.json + 环境变量覆盖 | 客户端分环境配置 | DONE | - |
| L0.15 | - | FluentValidation 验证 | 全模块 Validator | - | DONE | Validator 测试 |
| L0.16 | - | API 版本控制 + Swagger | /api/v1/* + Asp.Versioning | - | DONE | 架构测试 |
| L0.17 | - | 安全头 + CORS | SecurityHeadersMiddleware (HSTS/CSP/X-Frame) | - | DONE | - |
| L0.18 | - | 限流 (Rate Limiting) | 登录 5次/60秒，全局 200次/60秒，内网 20次 | - | DONE | 集成测试 |
| L0.19 | - | 共享模型 (DTO/枚举) | LYBT.Shared.Models | LYBT.Shared.Models | DONE | - |
| L0.20 | - | 值对象/基元类型 | LYBT.Shared.Primitives | LYBT.Shared.Primitives | DONE | - |

**Layer 0 评估**: 100% 完成 (20 项全 DONE)。覆盖 FR-ERR-001~005 + FR-LOG-001/003/004 + FR-CFG-001~003。

---

### Layer 1: 认证与安全 (Auth)

系统安全的核心。所有业务操作依赖认证。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L1.01 | FR-AUTH-001 | JWT 登录 (用户名+密码) | AuthController.Login (7 端点) | LoginViewModel | DONE | 16 集成 + 10 VM |
| L1.02 | FR-AUTH-002 | 自动登录 (AutoLoginToken) | AuthController.AutoLogin | CredentialVault (DPAPI+HMAC) | DONE | 22 CredentialVault |
| L1.03 | FR-AUTH-003 | Token 刷新 (滑动过期) | AuthController.Refresh + JwtService | TokenRefreshHandler (自动) | DONE | 集成测试 |
| L1.04 | FR-AUTH-004 | 重放攻击检测 (FamilyId) | TokenRevocationService | - | DONE | 集成测试 |
| L1.05 | FR-AUTH-005 | 登出 (本地优先+服务端) | AuthController.Logout | LogoutService | DONE | 20 LogoutService |
| L1.06 | FR-AUTH-006 | 不活跃超时检测 | - | UserActivityTracker (15分钟) | DONE | 20 测试 |
| L1.07 | FR-AUTH-007 | 超时前警告对话框 | - | 超时警告弹窗 (2分钟前) | DONE | - |
| L1.08 | FR-AUTH-008 | Token 验证 | AuthController.Validate | LocalTokenValidator | DONE | 8 测试 |
| L1.09 | FR-AUTH-009 | 凭证本地存储 (DPAPI) | - | CredentialVault (加密+HMAC完整性) | DONE | 22 测试 |
| L1.10 | FR-AUTH-010 | 登录状态机 | - | AuthenticationStateMachine (Idle/Validating/Active/Expired) | DONE | 33 测试 |
| L1.11 | FR-AUTH-011 | Token 刷新失败分级处理 | - | TokenManager (指数退避 1s/2s/4s) | DONE | 14 测试 |
| L1.12 | FR-AUTH-012 | 登录界面 (无边框全屏) | - | LoginView + LoginWindow | DONE | - |
| L1.13 | FR-AUTH-013 | 认证事件体系 | - | TokenEvents (pub-sub) | DONE | 9 测试 |
| L1.14 | FR-LOG-002 | 安全审计日志 | SecurityAuditService -> SecurityAuditLogs 表 | - | DONE | 集成测试 |

**Layer 1 评估**: 100% 完成 (14 项全 DONE)。覆盖 FR-AUTH-001~013 + FR-LOG-002。Desktop Foundation 有 123 测试。

---

### Layer 2: 用户管理 (Users)

管理系统用户，四层角色体系的实现。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L2.01 | FR-USER-001 | 创建用户 (含保留名检查) | UsersController (13 端点) | UserMasterDetailVM | DONE | 21 集成 + 44 桌面 |
| L2.02 | FR-USER-002 | 用户列表 (分页+搜索+筛选) | GetList + keyword/role/status | UserMasterDetailVM | DONE | 集成测试 |
| L2.03 | FR-USER-003 | 用户详情 | GetById | UserDetailVM | DONE | 集成测试 |
| L2.04 | FR-USER-004 | 更新用户信息 | Update (拼音码自动重建) | UserDetailVM | DONE | 集成测试 |
| L2.05 | FR-USER-005~006 | 软删除+恢复 | Delete/Restore | RestoreCommand | DONE | 集成测试 |
| L2.06 | FR-USER-007 | 批量删除/启用/禁用 | BatchDelete/Enable/Disable | BatchOperationResultDto | DONE | 集成测试 |
| L2.07 | FR-USER-008 | 管理员重置密码 | ResetPassword | ResetPasswordCommand | DONE | 集成测试 |
| L2.08 | **FR-USER-009** | **用户修改密码** | ChangePassword API (完整) | **TODO: 占位实现，调用链未连接** | **GAP** | - |
| L2.09 | FR-USER-010 | 修改个人资料 | Profile API | UserProfileVM | DONE | - |
| L2.10 | FR-USER-011 | 启用/禁用 (Token 失效) | ToggleStatus | StatusToggle | DONE | 集成测试 |
| L2.11 | FR-USER-012 | 获取当前用户 | GetCurrent | - | DONE | 集成测试 |
| L2.12 | - | 角色权限策略 | AdminOnly / DoctorOrAdmin | 角色过滤 | DONE | 架构测试 |

**Layer 2 评估**: 92% (11/12 DONE)。缺口: Desktop 修改密码调用链 (L2.08)。

---

### Layer 3: 患者管理 (Patients)

患者档案的电子化管理，是医案的前置依赖。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L3.01 | FR-PAT-001 | 创建患者 | PatientsController | PatientMasterDetailVM | DONE | 23 集成 + 42 桌面 |
| L3.02 | FR-PAT-002 | 患者列表 (分页+拼音码搜索) | GetList + keyword + OutputCache | PaginationService + SearchService | DONE | 12+19 测试 |
| L3.03 | FR-PAT-003 | 患者详情 | GetById (含 Age 计算属性) | PatientDetailVM | DONE | 集成测试 |
| L3.04 | FR-PAT-004 | 更新患者信息 | Update (拼音码重建+手机号唯一) | PatientDetailVM | DONE | 集成测试 |
| L3.05 | FR-PAT-005~006 | 软删除+恢复 | Delete/Restore (IgnoreQueryFilters) | RestoreCommand | DONE | 集成测试 |
| L3.06 | FR-PAT-007 | 批量删除 | BatchDelete | BatchOperationResultDto | DONE | 集成测试 |
| L3.07 | FR-PAT-008 | Excel 批量导入 (1000行限制) | Import API (multipart) | NPOI 本地导入 | DONE | 集成测试 |
| L3.08 | FR-PAT-009 | 导入模板下载 | ImportTemplate (AllowAnonymous) | 内置模板 | DONE | 集成测试 |
| L3.09 | FR-PAT-010 | Excel 导出 | Export API | NPOI 本地导出 | DONE | 集成测试 |
| L3.10 | FR-PAT-011~012 | 引用检查 (单个+批量) | CheckReference / BatchCheck (最多100条) | 删除确认 | DONE | 集成测试 |
| L3.11 | - | 敏感数据保护 (掩码) | SensitiveDataAttribute (5字段) | - | DONE | 4 测试 |
| L3.12 | - | 年龄自动计算 (BirthDate) | Service 层计算 | - | DONE | 实体测试 |
| L3.13 | **-** | **导航到病历查看页面** | - | **TODO: 占位符 (PatientMasterDetailVM:408)** | **GAP** | - |
| L3.14 | **-** | **导航到问诊流程页面** | - | **TODO: 占位符 (PatientMasterDetailVM:418)** | **GAP** | - |

**Layer 3 评估**: 86% (12/14 DONE)。两个导航入口为占位符 (L3.13/L3.14)，不影响核心 CRUD 功能。

---

### Layer 4: 药材管理 (Herbs)

中药材库维护，是处方和验方的数据基础。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L4.01 | FR-HERB-001 | 创建药材 | HerbsController | HerbMasterDetailVM | DONE | 18 集成 + 18 桌面 |
| L4.02 | FR-HERB-002 | 药材列表 (分类+拼音码搜索) | GetList + category + keyword | HerbMasterDetailVM | DONE | 集成测试 |
| L4.03 | FR-HERB-003 | 药材详情 | GetById | HerbDetailVM | DONE | 集成测试 |
| L4.04 | FR-HERB-004 | 更新药材信息 | Update (拼音码重建) | HerbDetailVM | DONE | 集成测试 |
| L4.05 | FR-HERB-005~007 | 软删除+恢复 | Delete/Restore | RestoreCommand | DONE | 集成测试 |
| L4.06 | FR-HERB-006 | 启用/禁用 + 批量 | ToggleStatus / BatchEnable/Disable | StatusToggle | DONE | 集成测试 |
| L4.07 | FR-HERB-008 | 批量删除 | BatchDelete | BatchOperationResultDto | DONE | 集成测试 |
| L4.08 | FR-HERB-009 | Excel 导入 | Import API | NPOI 本地导入 | DONE | 集成测试 |
| L4.09 | FR-HERB-010 | JSON 批量导入 (10000条) | BatchImport (Skip/Update/Error 策略) | 本地 JSON 导入 | DONE | 集成测试 |
| L4.10 | FR-HERB-011 | 导出 (Excel + JSON) | Export / ExportAll | NPOI 本地导出 | DONE | 集成测试 |
| L4.11 | FR-HERB-012 | 导入模板下载 | ImportTemplate (AllowAnonymous) | 内置模板 | DONE | 集成测试 |
| L4.12 | FR-HERB-013 | 引用检查 (处方引用) | CheckReference / BatchCheck (最多100条) | 删除确认 | DONE | 集成测试 |
| L4.13 | - | 药材选择控件 (处方用) | - | HerbListControl + HerbItemControl | DONE | 18 HerbItemVM |
| L4.14 | - | 所有权检查 (Doctor限制) | OwnershipCheck | - | DONE | 集成测试 |

**Layer 4 评估**: 100% 完成 (14 项全 DONE)。覆盖 FR-HERB-001~013。

---

### Layer 5: 验方管理 (Formula)

经验方模板，处方的复用来源。依赖 Layer 4 (药材)。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L5.01 | FR-FORM-001 | 创建验方 (含药材组成) | FormulasController | FormulaMasterDetailVM | DONE | 16 集成 + 24 桌面 |
| L5.02 | FR-FORM-002 | 验方列表 (分类+关键词搜索) | GetList + category + keyword | FormulaMasterDetailVM | DONE | 集成测试 |
| L5.03 | FR-FORM-003 | 验方详情 (含药材验证状态) | GetById + Herbs + IsValidated | FormulaDetailVM | DONE | 集成测试 |
| L5.04 | FR-FORM-004 | 更新验方 (药材粗粒度替换) | Update (完整替换 Herbs 集合) | EditFormulaDialogVM | DONE | 9 回归测试 |
| L5.05 | FR-FORM-005 | 删除验方 + 批量删除 | Delete / BatchDelete | RestoreCommand | DONE | 集成测试 |
| L5.06 | FR-FORM-006 | 启用/禁用 + 批量 | ToggleStatus / BatchEnable/Disable | StatusToggle | DONE | 集成测试 |
| L5.07 | FR-FORM-007 | 恢复已删除验方 | Restore (IgnoreQueryFilters) | RestoreCommand | DONE | 集成测试 |
| L5.08 | FR-FORM-008 | 共享验方 (IsShared) | IsShared 字段 + Doctor 过滤 | 权限过滤 | DONE | 集成测试 |
| L5.09 | FR-FORM-009 | 延迟绑定 (药材验证) | Validate API (HerbId 关联) | FormulaValidationVM | DONE | 集成测试 |
| L5.10 | FR-FORM-010 | 获取待验证验方 | GetPendingValidation | - | DONE | 集成测试 |
| L5.11 | FR-FORM-011 | 批量导入 (JSON + 药材匹配) | BatchImport + ICrossModuleQueryService | - | DONE | 集成测试 |
| L5.12 | FR-FORM-012 | 导出 (Excel) | Export | NPOI 本地导出 | DONE | - |
| L5.13 | FR-FORM-013 | 导入模板下载 | ImportTemplate (AllowAnonymous) | 内置模板 | DONE | - |
| L5.14 | - | 所有权检查 (Doctor限制) | OwnershipCheck | - | DONE | 集成测试 |

**Layer 5 评估**: 100% 完成 (14 项全 DONE)。覆盖 FR-FORM-001~013。

---

### Layer 6: 医案管理 (MedicalCase -- 系统核心)

唯一聚合根 (DDD)，包含 Consultation + Prescription，是系统最复杂的模块。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L6.01 | FR-MC-001 | 创建医案 (聚合根) | CreateMedicalCase + 自动建 Consultation | - | DONE | 22 集成 + 74 桌面 |
| L6.02 | FR-MC-002 | 填写诊断 (Consultation) | 聚合保存 (PUT /{id}) | MedicalCaseEditControl | DONE | 集成测试 |
| L6.03 | FR-MC-003 | 处方需求标记 | SetPrescriptionFlag | NeedsPrescription 切换 | DONE | 集成测试 |
| L6.04 | FR-MC-004 | 开具处方 (药材列表+价格) | 聚合保存 (Prescription+Items) | PrescriptionItems 编辑 | DONE | 16 PrescriptionHerbItemPrice |
| L6.05 | FR-MC-005 | 聚合保存 (MC+Consultation+Rx) | Save API (粗粒度替换 Items) | - | DONE | 集成测试 |
| L6.06 | FR-MC-006 | 暂存草稿 (不验证完整性) | SaveDraft | 暂存按钮 | DONE | 集成测试 |
| L6.07 | FR-MC-007 | 完成医案 (锁定编辑) | CloseMedicalCase | 完成看诊按钮 | DONE | 集成测试 |
| L6.08 | FR-MC-008 | 取消医案 (软删除) | CancelMedicalCase | 取消确认 | DONE | 集成测试 |
| L6.09 | FR-MC-009 | 医案列表 (状态+患者+关键词) | GetList / Query | ListControl | DONE | 集成测试 |
| L6.10 | FR-MC-010 | 跨医案搜索 (全文) | Search API | SearchControl | DONE | 集成测试 |
| L6.11 | FR-MC-011 | 编辑模式状态机 (Clinical/Management) | - | WorkspaceState + EditModeStateMachine | DONE | 37 StateMachine |
| L6.12 | FR-MC-012 | 审计日志 (字段级变更) | AuditService (JSON 存储) | AuditLog 查看 | DONE | 集成测试 |
| L6.13 | FR-MC-013 | 权限控制 (资源级) | PermissionService + MedicalCasePermissionDto | 权限查询 | DONE | 集成测试 |
| L6.14 | FR-MC-014 | 锁定规则 (隔天锁定) | StateService (IsLocked = Completed && Date < Today) | - | DONE | 集成测试 |
| L6.15 | FR-MC-015 | 处方打印触发 (PrintVersion管理) | PrintVersion/PrintCount/IsPrinted 字段 | 打印按钮 -> PrintService | DONE | 集成测试 |
| L6.16 | FR-MC-016 | 验方导入到处方 | - | FormulaImportDialog (实时价格) | DONE | - |
| L6.17 | FR-MC-017 | 待诊队列 | GetPending | - | DONE | 集成测试 |
| L6.18 | - | 编辑理由 (锁定后修改) | EditReason 参数 | 修改原因对话框 | DONE | 集成测试 |
| L6.19 | - | 历史处方复制 | - | HistoryCopyDialog | DONE | 7 PrescriptionEditFlow |
| L6.20 | - | 未保存修改提示 | - | UnsavedChangesDialog (保存/放弃/取消) | DONE | - |
| L6.21 | - | 批量删除/批量详情 | BatchDelete / GetBatchDetails | - | DONE | 集成测试 |
| L6.22 | **-** | **查询分页 Repository 层扩展** | **TODO: 内存过滤，应迁移到 Repository 层** | - | **PARTIAL** | - |

**Layer 6 评估**: 95% (21/22 DONE)。覆盖 FR-MC-001~017。L6.22 已功能可用，仅优化项 (内存过滤 -> Repository 层)。

---

### Layer 7: 打印 (Printing)

处方打印是临床流程的最后一环，直接影响诊疗闭环。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L7.01 | FR-PRINT-001 | 处方打印 (A5/A4 模板) | - | PrescriptionPrintService (FixedDocument, 565行) | DONE | 0 |
| L7.02 | FR-PRINT-002 | 打印预览 (设置+预览双面板) | - | PrintPreview 窗口 (打印机/份数/纸张选择) | DONE | 0 |
| L7.03 | FR-PRINT-003 | 打印版本管理 | PrintVersion 字段 (实体) | PrintVersion 递增逻辑 | DONE | 0 |
| L7.04 | FR-PRINT-004 | 打印日志 | PrescriptionPrintLog 实体 | 打印操作日志记录 | DONE | 0 |

**Layer 7 评估**: 100% 完成 (4 项全 DONE)。覆盖 FR-PRINT-001~004。PrescriptionPrintService.cs 完整实现，含 XPS 导出和批量打印。零测试覆盖待补充。

---

### Layer 8: 身份证读卡器 (CardReader)

硬件集成功能，提升挂号效率。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L8.01 | FR-CARD-001 | 读卡器连接与读取 | - | ICardReader + HuaDaHD100CardReader + MockCardReader + CardReaderFactory | DONE | 0 |
| L8.02 | FR-CARD-002 | 读卡数据填充到患者表单 | - | PatientCardReaderIntegration (184行) + VM 集成 | DONE | 0 |

**Layer 8 评估**: 100% 完成 (2 项全 DONE)。覆盖 FR-CARD-001~002。PatientCardReaderIntegration 已完整实现 FindOrCreate 逻辑，与 PatientMasterDetailVM 已集成。零测试覆盖待补充。

---

### Layer 9: 数据同步 (Sync)

本地/远程双模式切换和数据同步。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L9.01 | FR-SYNC-001 | 获取可同步实体类型 | SyncController.GetEntityTypes | SyncViewModel | DONE | 25 集成 |
| L9.02 | FR-SYNC-002 | 获取同步元数据 (SHA256) | GetMetadata | - | DONE | 集成测试 |
| L9.03 | FR-SYNC-003 | 数据比对 (差异分类) | Compare API | DiffUI | DONE | 集成测试 |
| L9.04 | FR-SYNC-004 | 上传本地变更 | Upload API | - | DONE | 集成测试 |
| L9.05 | FR-SYNC-005 | 下载服务端变更 | Download API | - | DONE | 集成测试 |
| L9.06 | FR-SYNC-006 | 同步删除 (引用检查) | Delete API | - | DONE | 集成测试 |
| L9.07 | FR-SYNC-007 | 完整同步工作流 (UI) | - | SyncVM + SyncConflictDialog | DONE | - |
| L9.08 | FR-SYNC-008 | 模式切换 (远程/本地) | - | DataSource 切换 | DONE | 12 DataSource 集成 |

**Layer 9 评估**: 100% 完成 (8 项全 DONE)。覆盖 FR-SYNC-001~008。

---

### Layer 10: Desktop Shell (应用框架)

WPF 客户端的宿主框架，Prism 模块化启动。

| 编号 | FR | 功能 | 实现 | 状态 | 测试 |
|------|-----|------|------|------|------|
| L10.01 | FR-SHELL-001 | 应用启动流水线 (6步) | StartupPipeline (Core->Error->ApiHealth->DB->Config->Module) | DONE | 15+21 测试 |
| L10.02 | FR-SHELL-002 | 登录协调 (远程+本地) | LoginCoordinator (11 依赖) | DONE | 21 测试 |
| L10.03 | FR-SHELL-003 | 会话生命周期管理 | SessionLifecycleManager (Token+Activity) | DONE | 18 测试 |
| L10.04 | FR-SHELL-004 | 页面导航 (Prism Region) | NavigationCoordinator (历史+参数+Region) | DONE | - |
| L10.05 | FR-SHELL-005 | 菜单与快捷键系统 | MenuManager (Ctrl+N/S/P, F1/F5) | DONE | - |
| L10.06 | FR-SHELL-006 | 启动诊断与性能监控 | StartupDiagnostics (慢步骤>3秒) | DONE | 20 测试 |
| L10.07 | FR-SHELL-007 | 账户设置 | AccountSettingsControl | DONE | - |
| L10.08 | - | 应用生命周期 | ApplicationLifecycle (状态机) | DONE | 20 测试 |
| L10.09 | - | 健康检查协调 | HealthCheckCoordinator | DONE | - |

**Layer 10 评估**: 100% 完成 (9 项全 DONE)。覆盖 FR-SHELL-001~007。

---

### Layer 11: 系统运维 (Health + Diagnostics)

生产环境监控和诊断能力。

| 编号 | FR | 功能 | Server 实现 | Desktop 实现 | 状态 | 测试 |
|------|-----|------|------------|-------------|------|------|
| L11.01 | FR-SYS-001 | 基础健康检查 (匿名) | HealthController.GetBasic | StartupDiagnostics | DONE | 集成测试 |
| L11.02 | FR-SYS-002 | Ping 端点 | HealthController.Ping | - | DONE | 集成测试 |
| L11.03 | FR-SYS-003 | 详细健康检查 (DB+Migration) | HealthController.GetDetailed | - | DONE | 集成测试 |
| L11.04 | FR-SYS-004 | 获取日志级别状态 | DiagnosticsController | - | DONE | - |
| L11.05 | FR-SYS-005 | 启用临时调试模式 | EnableDebugMode (durationMinutes, 最大120) | - | DONE | - |
| L11.06 | FR-SYS-006 | 禁用调试模式 | DisableDebugMode | - | DONE | - |
| L11.07 | FR-SYS-007 | 手动设置日志级别 | SetLogLevel (仅 SuperAdmin) | - | DONE | - |

**Layer 11 评估**: 100% 完成 (7 项全 DONE)。覆盖 FR-SYS-001~007。

---

## 三、缺口汇总

### 3.1 功能缺口 (3 项)

| 优先级 | 编号 | 缺口 | 层级 | FR | 影响评估 |
|--------|------|------|------|-----|----------|
| **P1** | GAP-1 | Desktop 修改密码调用链 | L2.08 | FR-USER-009 | Server API 已完整，Desktop 仅占位实现，需连接调用 |
| **P2** | GAP-2 | 患者->病历/问诊导航 | L3.13~14 | - | UI 便捷入口，TODO 占位符，不影响核心 CRUD |
| **P2** | GAP-3 | MedicalCase 查询 Repository 优化 | L6.22 | - | 功能可用 (内存过滤)，性能优化项 |

### 3.2 工作量估算

| 缺口 | 预估复杂度 | 涉及文件 |
|------|-----------|----------|
| GAP-1 修改密码 | 低 (0.5天) | 连接 Desktop UserService -> ChangePassword API |
| GAP-2 导航入口 | 低 (0.5天) | PatientMasterDetailVM -> NavigateTo 逻辑 |
| GAP-3 查询优化 | 低 (0.5天) | Repository 扩展 + Service 适配 |

### 3.3 已关闭的缺口 (上次会话识别，本次验证已完成)

| 原编号 | 模块 | 状态变更 | 实现文件 |
|--------|------|----------|----------|
| ~~GAP-1~~ | Printing | **20% -> 100%** | PrescriptionPrintService.cs (565行) |
| ~~GAP-2~~ | CardReader 集成 | **70% -> 100%** | PatientCardReaderIntegration.cs (184行) |

---

## 四、统计汇总

### 4.1 功能清单总览

| Layer | 功能项数 | DONE | GAP | PARTIAL | 完成率 |
|-------|---------|------|-----|---------|--------|
| L0 基础设施 | 20 | 20 | 0 | 0 | 100% |
| L1 认证安全 | 14 | 14 | 0 | 0 | 100% |
| L2 用户管理 | 12 | 11 | 1 | 0 | 92% |
| L3 患者管理 | 14 | 12 | 2 | 0 | 86% |
| L4 药材管理 | 14 | 14 | 0 | 0 | 100% |
| L5 验方管理 | 14 | 14 | 0 | 0 | 100% |
| L6 医案管理 | 22 | 21 | 0 | 1 | 95% |
| L7 打印 | 4 | 4 | 0 | 0 | 100% |
| L8 读卡器 | 2 | 2 | 0 | 0 | 100% |
| L9 数据同步 | 8 | 8 | 0 | 0 | 100% |
| L10 Shell | 9 | 9 | 0 | 0 | 100% |
| L11 运维 | 7 | 7 | 0 | 0 | 100% |
| **合计** | **140** | **136** | **3** | **1** | **97.1%** |

### 4.2 PRD FR 覆盖率

- **总 FR**: 120
- **已实现**: 117 (97.5%)
- **未实现**: 1 (FR-USER-009 Desktop 部分)
- **部分实现**: 0
- **非 FR 功能项**: 20 (框架/基础设施级功能，无对应 FR 但已实现)

### 4.3 测试覆盖概况

| 类型 | 测试数 |
|------|--------|
| Server 单元测试 | 423 |
| Desktop 单元测试 | 649 |
| 架构测试 | 41 |
| Server 集成测试 | 141 |
| Desktop 集成测试 | 24 |
| **新体系合计** | **1,278** |
| 遗留测试 | 670 |
| **总计** | **~1,948** |

### 4.4 零测试覆盖模块

| 模块 | 建议 |
|------|------|
| LYBT.Desktop.Printing | 用 Mock 打印测试核心逻辑 |
| LYBT.Desktop.CardReader | 用 MockCardReader 测试集成 |
| LYBT.Desktop.Admin | 补充 AdminHomeVM 测试 |
| LYBT.Desktop.Clinical | 补充 ClinicalHomeVM 测试 |
| LYBT.Desktop.Sync | 补充 SyncVM 单元测试 |
| LYBT.Shared.Logging | 补充 LoggingLevelManager 测试 |

---

## 五、代码级 TODO 清单 (7处)

| 位置 | 内容 | 优先级 | 关联缺口 |
|------|------|--------|----------|
| Desktop/UserService.cs:325 | 修改密码逻辑 (占位实现) | P1 | GAP-1 |
| Desktop/PatientMasterDetailVM.cs:408 | 导航到病历查看页面 | P2 | GAP-2 |
| Desktop/PatientMasterDetailVM.cs:418 | 导航到问诊流程页面 | P2 | GAP-2 |
| Server/MedicalCaseQueryService.cs:59 | Repository 分页扩展 | P2 | GAP-3 |
| Desktop/ClinicalHomeViewModel.cs:264 | 今日统计数据 | P3 | - |
| Desktop/MedicalCaseWorkspaceVM.cs:768 | 审计功能独立项目 | P3 | - |
| Server/ApiErrorCodes.cs:84 | 错误码统一后删除 | P3 | - |

---

## 六、技术债务

### 6.1 废弃模块 (空壳)

| 模块 | 状态 | 建议 |
|------|------|------|
| LYBT.Module.Consultation | 空壳 (已合并到 MedicalCase) | 从 sln 移除 |
| LYBT.Module.Prescriptions | 空壳 (已合并到 MedicalCase) | 从 sln 移除 |

### 6.2 遗留测试项目

- 7 个遗留测试项目 (670 方法) 待迁移到新体系后删除
- Auth 6文件, Formula 1, Herbs 2, MC 3, Patients 3, Sync 2, Users 1

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，完整功能清单与缺口分析 |
| 2026-02-12 | v2.0 | 重大更新: (1) 新增 FR 编号映射列; (2) 新增 3 个 PRD 模块 (Error Handling/Logging/Configuration) 的 12 个 FR; (3) 验证 Printing 和 CardReader 已完全实现，关闭 2 个 GAP; (4) 编译警告从 15 降为 0; (5) 功能项从 ~110 增加到 140; (6) 缺口从 5 个减少到 3 个 |
