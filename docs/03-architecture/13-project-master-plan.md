# LYBTZYZS 项目总账

> 版本: v2.0 | 更新: 2026-08-02 | 维护者: 技术总监 + 产品负责人
>
> **本文件是项目的唯一全局视图。** 任何 session 开始前必读此文件。
>
> **维护原则**: 文档必须反映代码真实状态。代码变更 → 文档同步更新。文档变更 → 代码必须跟上。

---

## 一、项目概况

| 项 | 值 |
|----|-----|
| 技术栈 | .NET 8 / WPF Prism / ASP.NET Core / EF Core / SQL Server |
| 架构 | 3-Layer (Server) + MVVM (Desktop) + Dual-Mode (Remote+Local) |
| 代码库 | D:\source\repos\LYBTZYZS |
| 分支 | master → Gitee (gitee.com/shouqitao/LYBTZYZS) |
| 数据库 | Remote: SQL Server (LYBTDB_Dev) / Local: LocalDB (LYBTDesktop) |
| Server 模块 | 8 个 (Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports) |
| Desktop 模块 | 7 个 + 3 个 Core 层 |
| 架构测试 | 85/86 pass（P07/P08/P10 约束不可违反） |
| Build | 0 错误 **9 个警告** |
| Desktop 测试 | 240 pass **104 fail**（测试主机进程崩溃） |

---

## 二、数据模型（代码实际定义）

### 2.1 核心实体（Shared/LYBT.Entities）

| 实体 | 表名 | 关键字段 | 关系 |
|------|------|---------|------|
| **ApplicationUser** | Users (Identity) | RealName, PinYinCode, Role(UserRole), IsSysAdmin, Status, MustChangeOnNextLogin | IdentityUser<Guid> |
| **Patient** | Patients | Name, PinYinCode, Gender, BirthDate, IdNumber(加密), PhoneNumber(加密), Status | — |
| **MedicalCase** | MedicalCases | PatientId, UserId(Doctor), CaseNumber, CaseStatus, NeedsPrescription, IsPrinted, PrintCount | 聚合根 |
| **Consultation** | Consultations | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis | 1:1 MedicalCase |
| **Prescription** | Prescriptions | MedicalCaseId, PrescriptionNumber, DosageCount, Discount, Usage, Advice, ReferencedFormulas | 1:0..1 MedicalCase |
| **PrescriptionItem** | PrescriptionItems | PrescriptionId, HerbId, HerbName, Quantity, UnitPrice, Unit, Role(君臣佐使) | 1:N Prescription |
| **Herb** | Herbs | Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price, CostPrice, Effect, Usage | — |
| **Formula** | Formulas | Name, Effect, Indication, Usage, Status, IsShared, ValidationStatus, Category, FormulaType | — |
| **FormulaHerbItem** | FormulaHerbItems | FormulaId, HerbId, HerbName, Quantity, Unit, Role | 1:N Formula |
| **Registration** | Registrations | PatientId, DoctorId, MedicalCaseId, Source(前台/医生), Status, QueueNumber, RegistrationFee | — |
| **AuthSession** | AuthSessions | UserId, RefreshToken, TokenFamily | — |
| **SecurityAuditLog** | SecurityAuditLogs | 事件类型、用户、IP、时间 | — |
| **MedicalCaseAuditLog** | MedicalCaseAuditLogs | 医案ID、操作、操作人 | — |
| **MedicalCasePrintLog** | MedicalCasePrintLogs | 医案ID、打印类型、打印机、操作人 | — |
| **SystemLog** | SystemLogs | 系统日志（已标记死代码，待删除） | — |

### 2.2 状态枚举

| 枚举 | 值 | 用途 |
|------|-----|------|
| **MedicalCaseStatus** | Active/Completed/Suspended/Cancelled | 医案生命周期 |
| **RegistrationStatus** | Waiting/InProgress/Completed/Cancelled | 挂号生命周期 |
| **RegistrationSource** | Receptionist/Doctor | 挂号来源 |
| **CommonStatus** | Enabled/Disabled | 通用启用/禁用 |
| **UserRole** | SuperAdmin(100)/Admin(10)/Doctor(1)/Receptionist(0) | 角色 |
| **FormulaValidationStatus** | Draft/Validated | 验方验证状态 |
| **FormulaType** | Classic/Experience | 经典方/经验方 |
| **Gender** | Male/Female/Unknown | 性别 |

---

## 三、API 端点（代码实际定义）

### 3.1 认证授权 (Auth) — `api/v1/auth`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| POST | /login | 用户名密码登录 | 匿名 |
| POST | /logout | 登出 | 匿名 |
| POST | /refresh | Token 刷新 | 匿名 |
| POST | /auto-login | 自动登录 | 匿名 |
| GET | /validate | Token 验证 | 需认证 |

### 3.2 用户管理 (Users) — `api/v1/users`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 用户列表（分页） | Admin+ |
| GET | /{id} | 用户详情 | Admin+ |
| POST | / | 创建用户 | Admin+ |
| PUT | /{id} | 更新用户 | Admin+ |
| DELETE | /{id} | 删除用户（软删除） | Admin+ |
| POST | /{id}/toggle-status | 启用/禁用 | Admin+ |
| POST | /{id}/restore | 恢复已删除 | Admin+ |
| POST | /batch-delete | 批量删除 | Admin+ |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |
| POST | /{id}/reset-password | 重置密码 | Admin+ |
| GET | /current | 当前用户信息 | 需认证 |
| PUT | /{id}/profile | 修改个人资料 | 需认证 |
| PUT | /{id}/change-password | 修改密码 | 需认证 |

### 3.3 患者管理 (Patients) — `api/v1/patients`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 患者列表（分页） | Doctor/Receptionist |
| GET | /{id} | 患者详情 | Doctor/Receptionist |
| POST | / | 创建患者 | Doctor/Receptionist |
| PUT | /{id} | 更新患者 | Doctor/Receptionist |
| DELETE | /{id} | 删除患者（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| GET | /{id}/check-reference | 检查引用关系 | Doctor/Receptionist |
| POST | /batch-check-reference | 批量检查引用 | Doctor/Receptionist |
| GET | /by-id-number/{idNumber} | 身份证号查询 | Doctor/Receptionist |

### 3.4 药材管理 (Herbs) — `api/v1/herbs`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 药材列表（分页） | Doctor/Receptionist |
| GET | /{id} | 药材详情 | Doctor/Receptionist |
| POST | / | 创建药材 | Doctor/Receptionist |
| PUT | /{id} | 更新药材 | Doctor/Receptionist |
| DELETE | /{id} | 删除药材（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| POST | /batch-import | 批量导入（JSON） | Doctor/Receptionist |
| GET | /{id}/check-reference | 检查引用关系 | Doctor/Receptionist |
| POST | /batch-check-reference | 批量检查引用 | Doctor/Receptionist |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |

### 3.5 验方管理 (Formulas) — `api/v1/formulas`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 验方列表（分页） | Doctor/Receptionist |
| GET | /{id} | 验方详情 | Doctor/Receptionist |
| POST | / | 创建验方 | Doctor/Receptionist |
| PUT | /{id} | 更新验方 | Doctor/Receptionist |
| DELETE | /{id} | 删除验方（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| POST | /batch-import | 批量导入（JSON） | Doctor/Receptionist |
| GET | /pending-validation | 待校验验方列表 | Doctor/Receptionist |
| POST | /{formulaId}/herbs/{herbItemId}/validate | 校验药材匹配 | Doctor/Receptionist |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |

### 3.6 医案管理 (MedicalCases) — `api/v1/medicalcases`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 医案列表（分页） | Doctor/Receptionist |
| GET | /{id} | 医案详情 | Doctor/Receptionist |
| POST | / | 创建医案 | Doctor |
| PUT | /{id} | 更新医案 | Doctor |
| DELETE | /{id} | 删除医案 | Doctor/Admin |
| POST | /batch-delete | 批量删除 | Doctor/Admin |
| PUT | /{id}/complete | 完成医案 | Doctor |
| PUT | /{id}/suspend | 挂起医案 | Doctor |
| PUT | /{id}/cancel | 取消医案 | Doctor |
| PUT | /{id}/status | 更新状态 | Doctor |
| PUT | /{id}/prescription-flag | 标记处方需求 | Doctor |
| PUT | /{id}/print-completed | 记录打印完成 | Doctor |
| GET | /{id}/consultations | 辨证记录列表 | Doctor/Receptionist |
| GET | /{id}/prescriptions | 处方列表 | Doctor/Receptionist |
| GET | /patient/{id}/consultations | 患者辨证历史 | Doctor/Receptionist |
| GET | /patient/{id}/prescriptions | 患者处方历史 | Doctor/Receptionist |
| POST | /batch-details | 批量查询详情（≤50） | Doctor/Receptionist |
| GET | /search | 跨医案搜索 | Doctor/Receptionist |
| GET | /query | 统一查询端点 | Doctor/Receptionist |
| GET | /{id}/permissions | 操作权限查询 | Doctor/Receptionist |
| GET | /{id}/audit-logs | 审计日志 | Doctor/Receptionist |

### 3.7 挂号管理 (Registrations) — `api/v1/registrations`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 挂号列表（分页+筛选） | Doctor/Receptionist |
| GET | /{id} | 挂号详情 | Doctor/Receptionist |
| POST | / | 创建挂号 | Doctor/Receptionist |
| PUT | /{id}/start-visit | 接诊 | Doctor/Receptionist |
| PUT | /{id}/cancel | 取消挂号 | Doctor/Receptionist |
| GET | /queue | 等待队列 | Doctor/Receptionist |
| POST | /quick-visit | 医生快速看诊 | Doctor |

### 3.8 统计报表 (Reports) — `api/v1/reports`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | /daily/income | 日收入统计 | Doctor/Admin |
| GET | /daily/consultations | 日问诊统计 | Doctor/Admin |
| GET | /daily/herbs | 日药材使用统计 | Doctor/Admin |

### 3.9 系统配置 (Configuration) — `api/v1/configuration`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 获取全部配置 | Admin+ |
| GET | /{key} | 获取单个配置 | Admin+ |
| POST | /validate | 生产环境验证 | Admin+ |
| ~~PUT~~ | ~~/~~ | ~~修改配置~~ | **❌ 缺失** |

### 3.10 诊断调试 (Diagnostics) — `api/v1/diagnostics`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | /logging/status | 日志级别状态 | Admin+ |
| POST | /logging/debug/enable | 启用调试模式 | Admin+ |
| POST | /logging/debug/disable | 禁用调试模式 | Admin+ |
| POST | /logging/level | 设置日志级别 | Admin+ |

### 3.11 部署管理 (Deploy) — `api/v1/deploy`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| POST | /upload | 上传更新包 | Admin+ |
| POST | /restart | 重启服务 | Admin+ |
| ~~GET~~ | ~~/version~~ | ~~版本检查~~ | **❌ 缺失** |

---

## 四、Desktop 视图（代码实际定义）

### 4.1 Shell

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| LoginView | Modules/LYBT.Desktop.Auth/Views/ | 登录界面 | — |
| FirstRunSetupView | Modules/LYBT.Desktop.Auth/Views/ | 首次运行向导 | 功能有限 |
| ServerConfigView | Modules/LYBT.Desktop.Auth/Views/ | 服务器地址配置 | — |
| AccountSettingsView | Shell/Views/ | 账户设置（个人资料/密码） | — |

### 4.2 管理员角色

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| AdminHomeView | Roles/LYBT.Desktop.Admin/Views/ | 管理员首页 | — |
| UserManagementView | Roles/LYBT.Desktop.Admin/Views/ | 用户管理 | — |
| SystemSettingsView | Roles/LYBT.Desktop.Admin/Views/ | 系统设置 | 仅读取，无编辑 |
| SysadminHomeView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 运维首页 | — |
| LogLevelControlView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 日志级别控制 | — |
| DeploymentView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 部署视图 | 仅上传+重启 |

### 4.3 临床角色

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| ClinicalHomeView | Roles/LYBT.Desktop.Clinical/Views/ | 临床首页 | TODO: 今日统计 |
| ClinicalWorkspaceView | Roles/LYBT.Desktop.Clinical/Views/ | 临床工作台 | — |
| PatientManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 患者管理 | — |
| PatientSelectionView | Roles/LYBT.Desktop.Clinical/Views/ | 患者选择（身份证读卡） | — |
| HerbManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 药材管理 | — |
| FormulaManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 验方管理 | — |
| MedicalCaseManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 医案管理 | — |
| MedicalCaseWorkspaceView | Roles/LYBT.Desktop.Clinical/Views/ | 医案工作台（核心） | 超大类型 558 行 |
| PendingQueueView | Roles/LYBT.Desktop.Clinical/Views/ | 待诊队列 | — |
| ReceptionistHomeView | Roles/LYBT.Desktop.Clinical/Receptionist/Views/ | 前台首页 | — |

### 4.4 医疗模块

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| MedicalCaseMasterDetailView | Modules/LYBT.Desktop.MedicalCase/Views/ | 医案主从详情 | — |
| AuditLogView | Modules/LYBT.Desktop.MedicalCase/Views/ | 审计日志 | — |
| ReportsHomeView | Modules/LYBT.Desktop.MedicalCase/Reports/Views/ | 报表首页 | TODO: 待完善 |

### 4.5 其他

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| RegistrationListView | Modules/LYBT.Desktop.Registration/Views/ | 挂号列表 | — |

---

## 五、已知问题（代码实际状态）

### 🔴 P0 — 必须修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P0-01 | Shell 登出状态机错误 | Shell/LoginCoordinator | 用户登出后状态不正确 |
| P0-02 | 并发登录竞态 | Shell/LoginCoordinator | 多人登录数据错乱 |
| P0-03 | 异常时事件未发布 | Shell/ShellEventCoordinator | 异常后 UI 卡死 |
| P0-04 | Sync-over-Async 死锁 | Desktop 多处 `.GetAwaiter().GetResult()` | WPF UI 线程冻结 |
| P0-05 | 明文密码泄露 | appsettings.json (SSH/SA/JWT SecretKey) | 安全风险 |
| P0-06 | 104 个 Desktop 测试失败 | tests/LYBT.Tests.Desktop | 测试主机崩溃，质量保障失效 |
| P0-07 | 配置无法 API 修改 | ConfigurationController 无 PUT | 运维只能手动改文件 |

### 🟡 P1 — 应修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P1-01 | 9 个 Build 警告 | 多处 (CA1001/CS8603/CS0168/CS4014) | 代码质量 |
| P1-02 | 8 个 TODO 残留 | MedicalCase/Shell/Reports | 技术债务 |
| P1-03 | 5 个超大类型 (>600行) | MedicalCaseCommandService/Repository/HttpClientApiClient/NavigableViewModelBase/PrescriptionPrintService | 可维护性 |
| P1-04 | 22 个 MediatR trivial Handler | MedicalCase Application/ | 过度设计 |
| P1-05 | 实体双模型 | Domain/ vs Shared/ (6 对双胞胎) | 维护成本翻倍 |
| P1-06 | Excel 导入/导出缺失 | Herbs/Formula/Patients | 无法批量操作 |
| P1-07 | 报表功能严重不足 | ReportsController (仅 3 个日统计) | 数据分析能力弱 |
| P1-08 | 6 个 NotSupportedException 桩 | Desktop Foundation Http/Clients | Desktop 功能不完整 |
| P1-09 |处方价格刷新未实现 | MedicalCasePrescriptionService TODO | 价格不自动更新 |

### 🔵 P2 — 可后续完善

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P2-01 | 自动更新 | Shell (Velopack) | 运维依赖 |
| P2-02 | 数据备份/恢复 | — | 运维依赖 |
| P2-03 | SignalR 实时通知 | — | 体验增强 |
| P2-04 | 离线同步 v2.0 | 旧分支已放弃 | 大功能 |
| P2-05 | Swagger/OpenAPI | — | 开发体验 |
| P2-06 | 排班管理 | Registration | 业务增强 |

---

## 六、待做工作清单

### A 类 — 架构清理

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| A-01 | 文档清理 | 归档 35 个 plan、删 17 个存根、修 27 个断链 | 无 | ⬜ | 0.5d |
| A-02 | 死代码删除 | ~737 行零引用代码 (Server 10 + Desktop 5 + Shared 2) | 无 | ⬜ | 0.5d |
| A-03 | MediatR 简化 | Herbs/Formula/Patients/Users 4 模块已完成(24 Handler)；MedicalCase 待做 | 无 | 🟡 | 0.5d |
| A-04 | 超大类型拆分 | 5 个 >600 行文件 | 无 | ⬜ | 2d |
| A-05 | 实体源统一 | 消除 Domain/Shared 双模型 | A-02 | ⬜ | 3d |
| A-06 | Repository 泛型化 | Desktop 15+ 对复制粘贴 | 无 | ⬜ | 1d |
| A-07 | CrossModule 死方法 | 6 个零调用方法 | 无 | ⬜ | 0.25d |
| A-08 | 命名规范统一 | 后缀/目录/注释语言 | 无 | ⬜ | 1d |
| A-09 | 架构测试补全 | 修复 1 skip + 新增 13 个缺口测试 | A-03/A-04 | ⬜ | 1d |
| A-10 | 接口下沉 Contracts | IPatientService/IUserService 移到 Contracts | 无 | ⬜ | 0.5d |
| A-11 | Registration 依赖清理 | 移除对 Patients/Users 直接引用 | A-10 | ⬜ | 0.25d |
| A-12 | AuthService 收敛 | RefreshToken 操作收敛到 Repository | 无 | ⬜ | 0.5d |

### B 类 — 产品功能

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| B-01 | P0 安全修复 | 明文密码/Shell Bug/死锁 (7 项) | 无 | ⬜ | 待定 |
| B-02 | 配置修改 API | ConfigurationController 添加 PUT | 无 | ⬜ | 1d |
| B-03 | Excel 导出/导入 | Herbs/Formula/Patients (NPOI) | 无 | ⬜ | 2-3d |
| B-04 | 报表增强 | 图表/多维度/时间范围 | 无 | ⬜ | 2d |
| B-05 | 配置中心 UI | SystemSettingsView 增强 | B-02 | ⬜ | 1d |
| B-06 | 数据备份/恢复 | SQL Server 备份+恢复 | 无 | ⬜ | 1.5d |
| B-07 | 初始化向导完善 | FirstRunSetupView 增强 | 无 | ⬜ | 1d |
| B-08 | Desktop 发布包 | 打包+依赖裁剪+安装器 | 无 | ⬜ | 2d |
| B-09 | 自动更新 | Velopack 集成 | B-08 | ⬜ | 2d |
| B-10 | SignalR 实时通知 | Hub+客户端+协议设计 | A-03 | ⬜ | 3d |
| B-11 | 药材/验方模板 | Excel 模板下载 | B-03 | ⬜ | 0.5d |
| B-12 | 患者导入导出 | Excel 模板+导出 | B-03 | ⬜ | 0.5d |
| B-13 | 验方校验 UI | Desktop 对齐 API | 无 | ⬜ | 0.5d |
| B-14 | 挂号排班 | 医生排班+号源管理 | 无 | ⬜ | 3d |
| B-15 | 离线同步 v2.0 | 重新设计架构 | 无 | ⬜ | 5d+ |
| B-16 | Swagger | API 文档生成 | 无 | ⬜ | 0.5d |

### C 类 — 运维部署

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| C-01 | Desktop 测试修复 | ~104 个失败测试 | 需运行中 WebAPI | ⬜ | 1d |
| C-02 | systemd 服务 | 开机自启 | 无 | ⬜ | 0.25d |
| C-03 | 部署脚本清理 | 评估 3 个脚本 | 无 | ⬜ | 0.25d |
| C-04 | NuGet 包清理 | 废弃包检查 | 无 | ⬜ | 0.25d |
| C-05 | 文档同步 | AGENTS.md/README.md 更新 | 所有代码改动后 | ⬜ | 0.5d |

---

## 七、执行阶段

### Phase 0: 安全/基础设施 (P0，最高优先级)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-01 P0 安全修复 (7项) | 待定 |
| 2 | B-02 配置修改 API | 1d |
| 3 | A-12 AuthService 收敛 | 0.5d |
| 4 | C-04 NuGet 包清理 | 0.25d |
| 5 | C-03 部署脚本清理 | 0.25d |
| **小计** | | **~2d + 安全修复** |

### Phase 1: 基础清理 (低风险)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | A-01 文档清理 | 0.5d |
| 2 | A-02 死代码删除 | 0.5d |
| 3 | A-07 CrossModule 死方法 | 0.25d |
| 4 | A-10 接口下沉 | 0.5d |
| 5 | A-11 Registration 依赖清理 | 0.25d |
| 6 | A-08 命名规范统一 | 1d |
| **小计** | | **~3d** |

### Phase 2: 架构优化 (为功能扫清障碍)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | A-03 MediatR 简化 | 1d |
| 2 | A-04 超大类型拆分 | 2d |
| 3 | A-06 Repository 泛型化 | 1d |
| 4 | A-09 架构测试补全 | 1d |
| 5 | A-05 实体源统一 | 3d |
| **小计** | | **~8d** |

### Phase 3: 核心功能 (业务价值最高)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-03 Excel 导出/导入 | 2-3d |
| 2 | B-04 报表增强 | 2d |
| 3 | B-05 配置中心 UI | 1d |
| 4 | B-06 数据备份/恢复 | 1.5d |
| 5 | B-07 初始化向导 | 1d |
| **小计** | | **~8d** |

### Phase 4: 高级功能 (依赖 Phase 2/3)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-08 Desktop 发布包 | 2d |
| 2 | B-09 自动更新 | 2d |
| 3 | B-10 SignalR | 3d |
| 4 | B-11/B-12 Excel 模板 | 1d |
| 5 | B-13 验方校验 UI | 0.5d |
| **小计** | | **~8.5d** |

### Phase 5: 收尾
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | C-01 Desktop 测试修复 | 1d |
| 2 | C-02 systemd 服务 | 0.25d |
| 3 | C-05 文档同步 | 0.5d |
| 4 | B-16 Swagger | 0.5d |
| **小计** | | **~2.25d** |

### 总预估

| 阶段 | 预估 | 累计 |
|------|------|------|
| Phase 0 安全/基础设施 | 2d+ | 2d+ |
| Phase 1 基础清理 | 3d | 5d+ |
| Phase 2 架构优化 | 8d | 13d+ |
| Phase 3 核心功能 | 8d | 21d+ |
| Phase 4 高级功能 | 8.5d | 29.5d+ |
| Phase 5 收尾 | 2.25d | 31.75d+ |

> 不含 B-14 排班管理 (3d)、B-15 离线同步 (5d+)，视业务需求决定。

---

## 八、状态跟踪

> 每完成一项，更新: ⬜→✅ + Commit SHA

| 任务 | 状态 | 完成日期 | Commit |
|------|------|---------|--------|
| A-01 文档清理 | ⬜ | — | — |
| A-02 死代码删除 | ⬜ | — | — |
| A-03 MediatR 简化 | 🟡 | 2026-08-02 | `29a4675af` `c5aca4e04` `741ca8735` `4b97bcfde` |
| A-04 超大类型拆分 | ⬜ | — | — |
| A-05 实体源统一 | ⬜ | — | — |
| A-06 Repository 泛型化 | ⬜ | — | — |
| A-07 CrossModule 死方法 | ⬜ | — | — |
| A-08 命名规范统一 | ⬜ | — | — |
| A-09 架构测试补全 | ⬜ | — | — |
| A-10 接口下沉 | ⬜ | — | — |
| A-11 Registration 依赖清理 | ⬜ | — | — |
| A-12 AuthService 收敛 | ⬜ | — | — |
| B-01 P0 安全修复 | ⬜ | — | — |
| B-02 配置修改 API | ⬜ | — | — |
| B-03 Excel 导出/导入 | ⬜ | — | — |
| B-04 报表增强 | ⬜ | — | — |
| B-05 配置中心 UI | ⬜ | — | — |
| B-06 数据备份/恢复 | ⬜ | — | — |
| B-07 初始化向导 | ⬜ | — | — |
| B-08 Desktop 发布包 | ⬜ | — | — |
| B-09 自动更新 | ⬜ | — | — |
| B-10 SignalR | ⬜ | — | — |
| B-11 药材/验方模板 | ⬜ | — | — |
| B-12 患者导入导出 | ⬜ | — | — |
| B-13 验方校验 UI | ⬜ | — | — |
| B-14 挂号排班 | ⬜ | — | — |
| B-15 离线同步 v2.0 | ⬜ | — | — |
| B-16 Swagger | ⬜ | — | — |
| C-01 Desktop 测试修复 | ⬜ | — | — |
| C-02 systemd 服务 | ⬜ | — | — |
| C-03 部署脚本清理 | ⬜ | — | — |
| C-04 NuGet 包清理 | ⬜ | — | — |
| C-05 文档同步 | ⬜ | — | — |
| D-01 接诊链修复（D8：StartVisit 原子建医案，2026-08-03 决策确认） | ⬜ | — | — |
| D-02 QuickVisit Desktop 接线（US-REG-002 激活） | ⬜ | — | — |

---

## 九、关键决策记录

| 日期 | 决策 | 理由 | 决策人 |
|------|------|------|--------|
| 2026-08-02 | MediatR 保留用于复杂业务，trivial CRUD 改直接注入 | 减少不必要的间接层 | 产品负责人 |
| 2026-08-02 | 架构测试约束 P07/P08/P10 不可违反 | 强制分层边界 | 技术总监 |
| 2026-08-02 | 实体源以 Shared/LYBT.Entities 为准 | 18 个项目已引用，变更成本最低 | 技术总监 |
| 2026-08-02 | 离线同步 v2.0 放弃旧分支，基于 master 重新实现 | 旧分支无法编译且删除了关键代码 | 技术总监 |
| 2026-08-03 | **接诊即建**：StartVisit/QuickVisit/本地选患者开始看诊时原子创建 MedicalCase(Active) + Registration(InProgress)，统一两条接诊路径 | 消除 BR-000 与 US-REG-005 矛盾；InProgress 天然挡住退号，无空医案残留 | 产品负责人 |
| 2026-08-03 | 权限决策四连（四角色需求审查）：① 患者删除/禁用仅 Admin+；② 前台不可查看药材/验方；③ 打印仅 Doctor（Admin 可查打印记录）；④ Admin 挂号只读查看 | 最小权限 + 角色画像清晰；代码待按操作级细分 | 产品负责人 |

---

## 十、维护规则（强制）

### 10.1 文档-代码一致性

| 时机 | 动作 |
|------|------|
| **Session 启动** | 读本文件接上进度 |
| **代码变更后** | 检查本文档是否需要同步更新 |
| **功能完成** | 更新状态表 ⬜→✅ + Commit SHA |
| **新增任务** | 在对应类别追加，更新计数和依赖图 |
| **决策变更** | 在 §九 追加一行 |

### 10.2 相关文档索引

| 文档 | 路径 | 用途 |
|------|------|------|
| **产品功能清单** | `docs/02-requirements/14-feature-inventory.md` | 128 个用户操作，按模块/角色分类 |
| 代码审计报告 | `docs/reports/2026-08-02-codebase-audit.md` | 项目现状全面审计 |
| 代码审查报告 | `docs/reports/code-review-duplicates.md` | 重复定义与不统一问题 |
| PRD | `docs/02-requirements/01-prd.md` | 产品需求文档 |
| AGENTS.md | `AGENTS.md` | 开发规范与约束 |
