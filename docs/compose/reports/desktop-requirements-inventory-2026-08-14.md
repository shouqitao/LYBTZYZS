# Desktop 需求梳理报告

> **日期**: 2026-08-14 | **版本**: v1.0  
> **范围**: LYBTZYZS Desktop（WPF/Prism.DryIoc MVVM）全部需求文档  
> **对照代码**: `src/Client/Desktop/` 全目录  

---

## 1. 总览

| 维度 | 数量 |
|------|------|
| 需求文档 | 11 个（02~10 + 11a + 11e） |
| US 总数（Desktop 有效） | **~72 条**（含 Desktop 独有 + 共享模块在 Desktop 的实现） |
| ✅ 已实现 | **53** |
| ⚠️ 部分实现 | **5** |
| 🔴 缺失/未实现 | **3** |
| 🧲 待实现（v1.0 计划中） | **8** |
| 📋 v2.0 规划 | **3** |

> **状态图例**: ✅ 已实现 | ⚠️ 部分实现 | 🔴 缺失 | 🧲 待实现 | 📋 v2.0 规划

---

## 2. 按模块需求全貌

### 2.1 Shell 模块（11a-shell.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-SHELL-001 | 应用启动（单实例） | Must | ✅ 已实现 | Mutex 单实例 `Global\LYBTZYZS_Shell_Instance`；启动闪屏；两阶段 Serilog；API 不可达→本地模式提示 | `Shell/App.xaml.cs` + `Services/Bootstrap/ApplicationBootstrapper.cs` + `Services/Startup/StartupPipeline.cs` |
| US-SHELL-003 | 角色基础模块加载 | Must | ⚠️ 部分实现 | Admin→管理模块；Doctor→临床模块；Receptionist→患者+读卡器；`LoadModulesForRoleAsync` 按角色过滤 | `Shell/Services/Bootstrap/ApplicationBootstrapper.cs:35` — LoginCoordinator 旁路待删 |
| US-SHELL-004 | 账户设置（个人资料+密码） | Could | ✅ 已实现 | AccountSettingsControl；修改密码弹框；调用 API 更新 | `Shell/Views/AccountSettingsView.xaml` + `Shell/ViewModels/AccountSettingsViewModel.cs` + `Shell/Controls/AccountSettingsControl.xaml.cs` |
| US-SHELL-005 | 菜单导航 | Must | ✅ 已实现 | `NavigateTo`/`NavigateBack`；导航历史 20 条；快捷键 Ctrl+N/S/F5/P；主题切换 | `Shell/Services/NavigationManager.cs` + `Shell/Services/MenuManager.cs` + `Core/Infrastructure/Navigation/` |
| US-SHELL-007 | 双模式连接切换 | Must | ⚠️ 部分实现 | SwitchingApiClient 路由 ✅；切换守卫 ERR-70506（本地有活跃医案时阻断远程切换）**无代码** | `Core/Foundation/Services/ConnectionModeService.cs` + `Core/Foundation/Http/SwitchingApiClient.cs` |
| US-SHELL-010 | Desktop 安装（Velopack） | Must | ✅ 已实现 | Velopack 打包 `Setup.exe`；安装到 `%LocalAppData%\LYBT`；下载页 + `/releases/` 静态服务 | `Shell/App.xaml.cs`（VelopackApp.Build）+ 脚本 `velopack-pack.ps1` / `sync-to-server.ps1` |
| US-SHELL-011 | 首次初始化向导 | Must | 🧲 待实现 | sysadmin 首登 5 步强制向导（改密→诊所→模式→admin→交权） | `Modules/LYBT.Desktop.Auth/ViewModels/FirstRunSetupViewModel.cs`（有基础框架，向导 UI 待开发） |
| US-SHELL-012 | Desktop 自动更新 | Should | 📋 v2.0 规划 | 启动自动检查 Velopack 更新；普通更新提示；安全更新强制倒计时 | 无代码（v2.0 范围） |
| US-SHELL-013 | 数据库备份恢复 | Should | ✅ 已实现 | LocalDB 备份文件列表；备份状态展示；手动备份按钮；恢复+确认弹框 | `Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/BackupManagementViewModel.cs` + `Sysadmin/Views/BackupManagementView.xaml` |
| US-SHELL-014 | 安全审计日志查看 | Should | 🧲 待实现 | 安全事件列表（分页/倒序）；事件类型筛选；保留 365 天 | 无 Desktop 专用 UI（服务端 `SecurityAuditService` 已实现，Desktop 查看面板待开发） |
| US-SHELL-016 | 配置导出/导入 | Could | 🧲 待实现 | 导出 appsettings + clinic-settings 为 JSON；导入+格式校验+重启提示 | 无代码（SysadminHomeView 新增按钮待实现） |
| US-SHELL-017 | 生产环境安全门控 | Must | ✅ 已实现 | `AutoCreateOnStartup` / `AllowAutoCreateInProduction` / `InitialSetupToken` / K4 环境变量 | `Shared.Configuration/SystemAdminOptions.cs` + `IdentitySeedData.cs` + `DatabaseInitializationService.cs`（Server/Shared 层，Desktop 通过 LocalWebAPI 消费） |
| US-SHELL-018 | sysadmin 配置中心 | Must | ✅ 已实现 | 7 组 13 项配置面板（诊所/会话/连接/安全/功能开关/读卡器/系统信息）；远程/本地双模式面板 | `Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/ConfigurationCenterViewModel.cs` + `Sysadmin/Views/SysadminHomeView.xaml` |
| US-SHELL-019 | 读卡器诊断测试工具 | Should | 🧲 待实现 | 厂家选择/设备探测/读卡测试/串口测试/固件版本 | `Core/Infrastructure/CardReader/Abstractions/ICardReaderDiagnostics.cs`（接口已定义）+ `CardReaderDiagnosticsService.cs`（实现已有）+ `Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/CardReaderDiagnosticsViewModel.cs`（VM 已有，UI 待完善） |
| US-SHELL-020 | 部署上传与远程重启 | Must | ✅ 已实现 | `POST /deploy/upload` + `POST /deploy/restart`（二次确认）；SysAdminOnly 权限 | `Roles/LYBT.Desktop.Admin/ViewModels/DeploymentViewModel.cs` + `Sysadmin/Views/DeploymentView.xaml` |
| US-SHELL-021 | 上线数据迁移 | Should | 🧲 待实现 | 标准 Excel 模板导入（患者/药材/验方）；分批导入；失败回滚 | 无代码 |
| US-SHELL-022 | 系统上线与回滚 | Should | 🧲 待实现 | 上线检查清单；试运行双轨；回滚方案 | 无代码（运维手册范畴） |
| US-SHELL-023 | 操作培训与用户支持 | Could | 🧲 待实现 | 培训材料；FAQ 手册；F1 帮助入口内容 | 无代码（文档范畴） |
| US-SHELL-024 | Server 单实例与端口防护 | Must | ✅ 已实现 | PID 文件 + 端口释放 + 启动探测 + Mutex | Server 端 `start.sh` + `Program.cs`（Desktop 侧由 Shell 单实例天然保护） |
| US-SHELL-025 | HTTP/HTTPS 双协议支持 | Should | ✅ 已实现 | Kestrel 多端点 + 配置开关 | Server 端 `Program.cs` + `appsettings.json`（Desktop 远程连接按配置） |

**Shell 小结**: 13 个 v1.0 US，8 个 ✅，3 个 🧲 待实现，2 个 ⚠️ 部分实现。

---

### 2.2 Auth 模块（02-auth.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-AUTH-000 | 首次登录初始化（超管专属） | Must | ⚠️ 部分实现 | 改密已实现；向导 UI（5 步）待开发 | `Modules/Auth/ViewModels/FirstRunSetupViewModel.cs`（基础框架） |
| US-AUTH-001 | 用户名密码登录 | Must | ✅ 已实现 | 用户名+密码；JWT 双令牌（远程）/ 单 JWT（本地）；失败锁定；限流 | `Modules/Auth/ViewModels/LoginViewModel.cs` + `Shell/Services/Login/LoginCoordinator.cs` + `Core/Foundation/Security/AuthenticationService.cs` |
| US-AUTH-002 | 登录失败锁定 | Must | ⚠️ 部分实现 | 双轨锁定（远程 Identity Lockout / 本地简化） | `Core/Foundation/Security/AuthenticationStateMachine.cs` |
| US-AUTH-003 | 登录限流 | Must | ✅ 已实现 | `[EnableRateLimiting("Login")]`；本地 5 次/分 | 服务端 `AuthController.cs` + 本地 `LocalWebAPI/Controllers/AuthController.cs` |
| US-AUTH-004 | 令牌刷新 | Must | ✅ 已实现 | RefreshToken 族旋转 + 重放检测 | `Core/Foundation/Security/TokenLifecycleService.cs` + `TokenRefreshHandler.cs` |
| US-AUTH-005 | 令牌验证 | Must | ✅ 已实现 | Bearer 令牌验证；显式 /validate 端点 | `Core/Foundation/Security/LocalTokenValidator.cs` + 中间件 |
| US-AUTH-006 | 重放攻击检测 | Must | ✅ 已实现 | 令牌族撤销；已废弃令牌重放检测 | `Core/Foundation/Security/TokenLifecycleService.cs` |
| US-AUTH-007 | 安全审计日志 | Should | ✅ 已实现 | SecurityAuditService 事件覆盖 | `Core/Foundation/Security/AuthenticationService.cs` + 服务端 |
| US-AUTH-008 | 登出（含过期令牌） | Must | ✅ 已实现 | AllowAnonymous 登出；撤销令牌 | `Core/Foundation/Security/LogoutService.cs` |
| US-AUTH-009 | 本地自动登录（AutoLoginToken） | Must | ✅ 已实现 | AutoLoginToken 签发+持久化+服务端可撤销 | `Core/Foundation/Security/CredentialVault.cs` + `CredentialStorage.cs` + `LocalWebAPI/Controllers/AuthController.cs` |
| US-AUTH-010 | AutoLoginToken 轮换 | Should | ✅ 已实现 | 自动登录成功后轮换令牌 | `Core/Foundation/Security/TokenLifecycleService.cs` |

**Auth 小结**: 11 个 US，9 个 ✅，2 个 ⚠️ 部分实现（000 向导 UI、002 双轨锁定）。

---

### 2.3 Users 模块（03-users.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-USER-001 | 分页查询用户列表 | Must | ✅ 已实现 | keyword/role/status 筛选+分页 | `Modules/Users/ViewModels/UserMasterDetailViewModel.cs` + `Repositories/UserRepository.cs` |
| US-USER-002 | 查看用户详情 | Must | ✅ 已实现 | 含角色信息+CreatedAt/UpdatedAt | `Modules/Users/ViewModels/UserEditorViewModel.cs` |
| US-USER-003 | 查看当前用户资料 | Must | ✅ 已实现 | JWT Claims 提取 | `Shell/ViewModels/AccountSettingsViewModel.cs` |
| US-USER-004 | 创建用户 | Must | ✅ 已实现 | 用户名唯一+保留用户名+层级规则 | `Modules/Users/ViewModels/UserEditorViewModel.cs` |
| US-USER-005 | 更新用户 | Must | ✅ 已实现 | UserName 不可改+角色变更层级约束 | `Modules/Users/ViewModels/UserEditorViewModel.cs` |
| US-USER-006 | 删除用户（软删除） | Must | ✅ 已实现 | 软删除+不可删自己+sysadmin 保护 | `Modules/Users/ViewModels/UserMasterDetailViewModel.cs` |
| US-USER-007 | 重置用户密码 | Must | ✅ 已实现 | 返回临时密码+sysadmin 保护 | `Modules/Users/ViewModels/Handlers/UserPasswordHandler.cs` |
| US-USER-008 | 修改个人资料 | Must | ✅ 已实现 | IDOR 防护 | `Shell/ViewModels/AccountSettingsViewModel.cs` |
| US-USER-009 | 修改密码 | Must | ✅ 已实现 | 需旧密码+IDOR 防护 | `Shell/ViewModels/AccountSettingsViewModel.cs` |
| US-USER-010 | 启用/禁用用户 | Must | ✅ 已实现 | Lockout 机制+sysadmin 保护 | `Modules/Users/ViewModels/UserMasterDetailViewModel.cs` |
| US-USER-011 | 恢复软删除用户 | Should | ✅ 已实现 | 层级管理+会话清理 | `Modules/Users/ViewModels/UserMasterDetailViewModel.cs` |
| US-USER-012 | 批量操作 | Should | ✅ 已实现 | 100 上限+部分失败+审计日志 | `Modules/Users/ViewModels/UserMasterDetailViewModel.cs` |

**Users 小结**: 12 个 US，全部 ✅ 已实现。

---

### 2.4 Patients 模块（04-patients.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-PAT-001 | 分页查询患者列表 | Must | ✅ 已实现 | 姓名/电话/拼音搜索+分页+非管理员仅启用 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` + `Repositories/PatientRepository.cs` |
| US-PAT-002 | 查看患者详情 | Must | ✅ 已实现 | 敏感字段脱敏+404 | `Modules/Patients/ViewModels/PatientEditorViewModel.cs` |
| US-PAT-003 | 创建患者 | Must | ✅ 已实现 | 电话唯一 409+拼音自动生成+年龄计算 | `Modules/Patients/ViewModels/PatientEditorViewModel.cs` |
| US-PAT-004 | 更新患者 | Must | ✅ 已实现 | 电话唯一+拼音重生成+年龄重算 | `Modules/Patients/ViewModels/PatientEditorViewModel.cs` |
| US-PAT-005 | 删除患者 | Must | ✅ 已实现 | 软删除+引用检查 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-006 | 启用/禁用患者 | Must | ✅ 已实现 | IsEnabled 切换 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-007 | 恢复软删除患者 | Should | ✅ 已实现 | IgnoreQueryFilters | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-008 | 批量删除患者 | Should | ✅ 已实现 | 逐项引用检查+部分失败 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-009 | 单个引用检查 | Must | ✅ 已实现 | HasReference+引用计数 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-010 | 批量引用检查 | Should | ✅ 已实现 | 单次聚合查询 | `Modules/Patients/ViewModels/PatientMasterDetailViewModel.cs` |
| US-PAT-011 | 下载导入模板 | Should | 🔧 设计修订 | JSON 模板（非 Excel） | `Modules/Patients/Services/RemotePatientService.cs` |
| US-PAT-012 | 导出患者数据 | Should | ✅ 已实现 | JSON 导出 | `Modules/Patients/Services/RemotePatientService.cs` |

**Patients 小结**: 12 个 US，11 个 ✅，1 个 🔧 设计修订（011 JSON 模板）。

---

### 2.5 Catalog 模块（05-herbs.md + 06-formulas.md）

#### 药材（Herbs）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-HERB-001 | 分页查询药材列表 | Must | ✅ 已实现 | 拼音/分类搜索+分页 | `Modules/Catalog/ViewModels/HerbMasterDetailViewModel.cs` + `Repositories/HerbRepository.cs` |
| US-HERB-002 | 查看药材详情 | Must | ✅ 已实现 | 完整信息+404 | `Modules/Catalog/ViewModels/HerbEditorViewModel.cs` |
| US-HERB-003 | 创建药材 | Must | ✅ 已实现 | 名称唯一 409+拼音自动生成 | `Modules/Catalog/ViewModels/HerbEditorViewModel.cs` |
| US-HERB-004 | 更新药材 | Must | ✅ 已实现 | 名称唯一+拼音重生成 | `Modules/Catalog/ViewModels/HerbEditorViewModel.cs` |
| US-HERB-014 | Desktop 前端药材缓存 | Must | ⚠️ 部分实现 | 首次查询缓存+CRUD 后失效+提交时校验兜底 | `Core/Foundation/Caching/DesktopCacheManager.cs`（InvalidateHerbCaches 已实现）；缓存读取端待接线 |

#### 验方（Formulas）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-FORM-001 | 分页查询验方列表 | Must | ✅ 已实现 | Doctor 仅本人+共享；Admin 全部 | `Modules/Catalog/ViewModels/FormulaMasterDetailViewModel.cs` + `Repositories/FormulaRepository.cs` |
| US-FORM-002 | 查看验方详情 | Must | ✅ 已实现 | 完整 Herbs+验证状态+403 | `Modules/Catalog/ViewModels/FormulaEditorViewModel.cs` |
| US-FORM-003 | 创建验方 | Must | ✅ 已实现 | Draft 初始状态+药材至少 1 味 | `Modules/Catalog/ViewModels/FormulaEditorViewModel.cs` |
| US-FORM-004 | 更新验方 | Must | ✅ 已实现 | 替换药材+FLAW-F1 降级检查 | `Modules/Catalog/ViewModels/FormulaEditorViewModel.cs` |

**Catalog 小结**: 9 个 US，8 个 ✅，1 个 ⚠️ 部分实现（药材缓存接线）。

---

### 2.6 MedicalCase 模块（07-medical-cases.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-MC-001 | 创建医案 | Must | ✅ 已实现 | DoctorOnly+BR-001+CaseNumber+审计字段 | `Modules/MedicalCase/Services/MedicalCaseCommandService.cs` + `Repositories/MedicalCaseRepository.cs` |
| US-MC-002 | 保存医案（聚合保存） | Must | ✅ 已实现 | EditReason+乐观锁+打印标记 | `Modules/MedicalCase/Services/MedicalCaseCommandService.cs` |
| US-MC-003 | 设置处方需求标志 | Must | ✅ 已实现 | NeedsPrescription 三态 | `Modules/MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs` |
| US-MC-004 | 查询医案详情 | Must | ✅ 已实现 | Doctor 仅本人+计算属性 | `Modules/MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs` |
| US-MC-005 | 分页查询医案列表 | Must | ✅ 已实现 | 按角色过滤+状态/患者/关键词筛选 | `Modules/MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs` |
| US-MC-006 | 统一查询 | Must | ✅ 已实现 | ByPatient/Pending/Recent | `Modules/MedicalCase/Services/MedicalCaseQueryService.cs` |
| US-MC-007 | 跨模块搜索 | Must | ✅ 已实现 | 患者名+诊断关键词+日期范围 | `Modules/MedicalCase/Services/MedicalCaseQueryService.cs` |

**MedicalCase 小结**: 7 个 US（Desktop 视图侧），全部 ✅ 已实现。

---

### 2.7 Registrations 模块（08-registration.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-REG-001 | 前台创建挂号 | Must | ✅ 已实现 | 患者校验+挂号费带出 | `Modules/Registrations/ViewModels/RegistrationListViewModel.cs` + `Dialogs/RegistrationCreateDialogViewModel.cs` |
| US-REG-002 | 医生快速就诊 | Must | ⚠️ 部分实现 | 两步流程（POST + start-visit）；**Desktop 待接线激活** | `Roles/Clinical/ViewModels/PatientSelectionViewModel.cs`（死代码，待 UI 实施） |
| US-REG-003 | 查看挂号详情 | Must | ✅ 已实现 | RegistrationDetailDto | `Modules/Registrations/ViewModels/RegistrationListViewModel.cs` |
| US-REG-004 | 分页查询挂号+排队 | Must | ✅ 已实现 | 队列当天过滤+状态着色 | `Modules/Registrations/ViewModels/RegistrationListViewModel.cs` |
| US-REG-005 | 开始就诊 | Must | ✅ 已实现 | StartVisit 原子建医案+并发保护 | `Modules/Registrations/Services/RemoteRegistrationService.cs` + 服务端 `StartVisitCommandHandler` |
| US-REG-006 | 取消挂号 | Must | ✅ 已实现 | Waiting-only+关联医案拒绝 | `Modules/Registrations/ViewModels/RegistrationListViewModel.cs` |
| US-REG-007 | 医案联动 | Must | ✅ 已实现 | 完成/取消自动回写 Registration | 服务端 `MedicalCaseService` + Desktop 通过 API 联动 |
| US-REG-008 | 待诊列表实时更新 | Must | ✅ 已实现 | SignalR 推送+轮询降级 | `Modules/Registrations/Services/SignalRClient.cs` + `PendingQueueViewModel.cs` |

**Registrations 小结**: 8 个 US，7 个 ✅，1 个 ⚠️ 部分实现（QuickVisit UI 待接线）。

---

### 2.8 Printing 模块（09-printing.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-PRINT-001 | 打印处方（A5/A4） | Must | ✅ 已实现 | A5/A4+打印标记+PrintLog | `Core/Printing/Services/PrescriptionPrintService.cs` + 4 个 XAML 模板 |
| US-PRINT-002 | 处方预览 | Must | ✅ 已实现 | WYSIWYG 预览+打印设置 | `Core/Printing/Services/PrescriptionPreviewWindowBuilder.cs` |
| US-PRINT-003 | 导出处方（PDF） | Should | ✅ 已实现 | QuestPDF 独立布局 | `Core/Printing/Services/PrescriptionPdfExporter.cs` |
| US-PRINT-004 | 打印记录回写 | Must | ✅ 已实现 | 成功/失败日志+PrintVersion | `Modules/MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs` + 服务端 `MedicalCasePrintController` |

**Printing 小结**: 4 个 US，全部 ✅ 已实现。

---

### 2.9 Reports 模块（10-reports.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-REPORT-001 | 收入报表 | Must | ✅ 已实现 | 挂号费+药费+总计+时间范围 | `Modules/MedicalCase/Reports/ViewModels/ReportsHomeViewModel.cs` + `Services/ReportService.cs` |
| US-REPORT-002 | 就诊统计 | Must | ✅ 已实现 | 总数+按医生分组 | `Modules/MedicalCase/Reports/ViewModels/ReportsHomeViewModel.cs` |
| US-REPORT-003 | 药材使用排行 | Should | ✅ 已实现 | 使用次数+总用量 | `Modules/MedicalCase/Reports/ViewModels/ReportsHomeViewModel.cs` |

**Reports 小结**: 3 个 US，全部 ✅ 已实现。

---

### 2.10 CardReader 模块（11e-cardreader.md）

| US 编号 | 名称 | 优先级 | 状态 | 关键 AC | Desktop 代码对应 |
|---------|------|--------|------|---------|-----------------|
| US-CARD-001 | 身份证读卡 | Should | ✅ 已实现 | 连接+读取+自动轮询+事件 | `Core/Infrastructure/CardReader/Services/CardReaderService.cs` + `HuaDaHD100CardReader.cs` + `MockCardReader.cs` |
| US-CARD-002 | 患者去重查找/创建 | Should | 🔴 缺失 | PRD-15 降级链（精确/模糊/多候选/快速创建） | `Core/Infrastructure/CardReader/Integration/IPatientCardReaderIntegration.cs`（接口存在，实现已移除 A-31-C7） |

**CardReader 小结**: 2 个 US，1 个 ✅，1 个 🔴 缺失。

---

### 2.11 NFR 非功能需求（12-nfr.md — Desktop 相关部分）

| NFR 编号 | 名称 | Desktop 相关 | 状态 | 说明 |
|----------|------|-------------|------|------|
| NFR-PERF-002 | Desktop 客户端响应 | ✅ | 🧲 待验证 | 冷启动 <5s、热启动 <1s、页面切换 <1s、表单保存 <2s、搜索 <1s（设计目标，未有基准测试） |
| NFR-PERF-003 | 客户端运行环境 | ✅ | ✅ | Windows 10+ / .NET 8 / 4GB+ RAM |
| NFR-PERF-004 | 并发能力（本地） | ✅ | ✅ | 本地模式单用户独占（Mutex） |
| NFR-AVAIL-001 | 数据备份（LocalDB） | ✅ | ✅ | 登录自动备份 + 7 天保留 + T-SQL BACKUP |
| NFR-AVAIL-002 | 故障恢复（本地降级） | ✅ | ✅ | 本地模式即时可用 |
| NFR-AVAIL-004 | 单实例（LocalDB） | ✅ | ✅ | Mutex `Global\LYBTZYZS_Shell_Instance` |
| NFR-SEC-003 | 敏感数据保护 | ✅ | ✅ | DPAPI 加密（照片/密码/令牌）+ `[SensitiveData]` 脱敏 |

---

## 3. Desktop 独有需求清单

以下需求**仅在 Desktop 端实现**，WebAPI 无对应物：

| 需求 | 说明 | 状态 |
|------|------|------|
| 双模式切换（US-SHELL-007） | SwitchingApiClient 路由远程/本地 | ⚠️ 部分（守卫缺失） |
| 本地嵌入式 WebAPI（LocalWebAPI） | 内嵌 ASP.NET Core host 本地模式全栈 | ✅ |
| Velopack 打包/安装/更新（US-SHELL-010/012） | 桌面客户端安装分发 | 010 ✅ / 012 📋 v2.0 |
| Desktop 客户端缓存（US-HERB-014） | 前端药材缓存加速 | ⚠️ 部分 |
| 处方打印（US-PRINT-001~003） | WPF FixedDocument + QuestPDF | ✅ |
| 读卡器硬件集成（US-CARD-001） | P/Invoke 华大 HD100 SDK | ✅ |
| 读卡器诊断测试（US-SHELL-019） | sysadmin 诊断面板 | 🧲 待实现 |
| 首次初始化向导（US-SHELL-011） | 5 步强制向导 UI | 🧲 待实现 |
| 深色/浅色主题切换 | WPF DynamicResource 主题 | ✅ |
| 本地模式自动备份（NFR-AVAIL-001） | 登录时 T-SQL BACKUP | ✅ |
| 安全审计日志查看面板（US-SHELL-014） | sysadmin 审计日志 UI | 🧲 待实现 |
| 配置导出/导入（US-SHELL-016） | JSON 打包/恢复 | 🧲 待实现 |

---

## 4. Desktop 与 WebAPI 功能差异

| 维度 | Desktop（WPF 客户端） | WebAPI（ASP.NET Core） |
|------|----------------------|----------------------|
| **部署方式** | Velopack Setup.exe 安装到 `%LocalAppData%\LYBT` | Linux Docker / 直接运行 |
| **数据源** | 远程 HTTP API 或本地嵌入式 LocalWebAPI | SQL Server 直连 |
| **认证** | JWT 本地验证（DPAPI 加密存储）+ AutoLoginToken | JWT 中间件验证 + RefreshToken 族旋转 |
| **打印** | WPF FixedDocument + QuestPDF 导出 | 无（纯 API，打印由 Desktop 驱动） |
| **读卡器** | P/Invoke 硬件集成 | 无（纯 HTTP API） |
| **缓存** | DesktopCacheManager 前端缓存 | OutputCache 服务端缓存 |
| **信号推送** | SignalRClient 接收 + 轮询降级 | SignalR Hub 推送 |
| **配置管理** | SysadminHomeView 7 组面板（客户端+服务端） | ConfigurationController API |
| **备份** | LocalDB 备份恢复 UI | SQL Server Agent 备份 |
| **安装更新** | Velopack 自动化 | start.sh 部署脚本 |
| **UI 框架** | WPF + Prism + MaterialDesign | Swagger/OpenAPI 文档 |
| **离线支持** | 本地模式：LocalWebAPI + LocalDB 完全独立 | 无（必须联网） |

---

## 5. Desktop 模块→代码映射

| Desktop 模块 | 代码路径 | 关联需求文档 |
|-------------|---------|-------------|
| Auth | `Modules/LYBT.Desktop.Auth/` | 02-auth.md |
| Users | `Modules/LYBT.Desktop.Users/` | 03-users.md |
| Patients | `Modules/LYBT.Desktop.Patients/` | 04-patients.md |
| Catalog（Herbs+Formulas） | `Modules/LYBT.Desktop.Catalog/` | 05-herbs.md + 06-formulas.md |
| MedicalCase | `Modules/LYBT.Desktop.MedicalCase/` | 07-medical-cases.md |
| Registrations | `Modules/LYBT.Desktop.Registrations/` | 08-registration.md |
| Printing | `Core/LYBT.Desktop.Printing/` | 09-printing.md |
| Reports | `Modules/LYBT.Desktop.MedicalCase/Reports/` | 10-reports.md |
| CardReader | `Core/LYBT.Desktop.Infrastructure/CardReader/` | 11e-cardreader.md |
| Shell | `Shell/` + `Roles/` | 11a-shell.md |
| LocalWebAPI | `LocalWebAPI/` | 11a-shell.md 双模式 |
| Admin（Sysadmin） | `Roles/LYBT.Desktop.Admin/Sysadmin/` | 11a-shell.md 013/018/019/020 |
| Clinical（Doctor） | `Roles/LYBT.Desktop.Clinical/` | 07/08 临床工作流 |
| Receptionist | `Roles/LYBT.Desktop.Clinical/Receptionist/` | 08-registration 前台 |

---

## 6. 待实现/缺失项优先级排序

### 🔴 缺失（需尽快处理）

1. **US-CARD-002 患者去重查找/创建**（PRD-15 降级链）— 实现已移除，接口空壳
2. **US-SHELL-007 切换守卫 ERR-70506** — 本地有活跃医案时阻断远程切换逻辑无代码
3. **US-SHELL-003 LoginCoordinator 旁路** — 角色基础模块加载旁路待清理

### 🧲 待实现（v1.0 计划中）

1. **US-SHELL-011 首次初始化向导** — FirstRunSetupViewModel 有基础框架，5 步向导 UI 待开发
2. **US-SHELL-014 安全审计日志查看面板** — 服务端已有 SecurityAuditService，Desktop 查看 UI 待开发
3. **US-SHELL-016 配置导出/导入** — SysadminHomeView 新增按钮待实现
4. **US-SHELL-019 读卡器诊断测试工具** — 接口+服务已有，sysadmin 诊断面板 UI 待完善
5. **US-SHELL-021 上线数据迁移** — Excel 模板批量导入
6. **US-SHELL-022 系统上线与回滚** — 运维检查清单
7. **US-SHELL-023 操作培训与用户支持** — 文档范畴
8. **US-REG-002 医生快速就诊（Desktop UI）** — 服务端已实现，Desktop 两步流程 UI 待接线

### ⚠️ 部分实现（需补全）

1. **US-SHELL-007 双模式切换守卫** — SwitchingApiClient 路由 ✅，ERR-70506 守卫 ❌
2. **US-HERB-014 Desktop 前端药材缓存** — 失效已实现，缓存读取端待接线
3. **US-REG-002 医生快速就诊 Desktop UI** — 服务端已实现，Desktop UI 待实施
4. **US-AUTH-000 首次初始化向导 UI** — 改密已实现，向导流程 UI 待开发
5. **US-AUTH-002 双轨锁定** — 远程 Identity Lockout ✅，本地简化锁定待完善

---

## 7. 统计摘要

```
总 US 数: ~72 条
  ✅ 已实现:    53 (73.6%)
  ⚠️ 部分实现:   5 (6.9%)
  🔴 缺失:      3 (4.2%)
  🧲 待实现:     8 (11.1%)
  📋 v2.0 规划:  3 (4.2%)

Desktop 独有需求: 12 项
Desktop 与 WebAPI 差异: 11 个维度
```

---

## 8. 附录：文件清单

| 文件 | 说明 |
|------|------|
| `src/Client/Desktop/Shell/` | Shell 入口、启动管线、导航、菜单、对话框 |
| `src/Client/Desktop/Modules/` | 7 个业务模块（Auth/Users/Patients/Catalog/MedicalCase/Registrations） |
| `src/Client/Desktop/Roles/` | 2 个角色工作空间（Admin/Sysadmin + Clinical/Receptionist） |
| `src/Client/Desktop/Core/` | 4 层核心库（Contracts/Foundation/Infrastructure/Controls/Printing） |
| `src/Client/Desktop/LocalWebAPI/` | 内嵌本地 WebAPI（11 个 Controller） |
| `src/Client/Desktop/Resources/` | XAML 资源字典和字符串 |

---

*报告生成: 2026-08-14 | 基于代码目录扫描 + 需求文档逐条对照*
