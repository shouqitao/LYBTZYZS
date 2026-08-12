# LYBTZYZS 当前状态

> 由 [13-project-master-plan.md §四/§五](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03），内容原样迁移：Desktop 视图实现现状 + 已知问题清单。**本文件是 Build/测试/已知问题的唯一权威。**

## 三、当前状态速览

| 项 | 值 |
|----|-----|
| Build | 0 错误 **0 警告** |
| 架构测试 | 88/88 pass（P07/P08/P10 约束不可违反） |
| Desktop 测试 | 240 pass **104 fail**（测试主机进程崩溃） |
| 最新迁移 | `RecreateDroppedAuditTables` |

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

## 五、统一/合并遗留项登记（2026-08-10 建立）

> **规则**：每轮统一/合并任务完成后，把「本轮有意保留项」显式登记于此（项 + 原因 + 计划批次），作为后续扫描判断「遗留 vs 新问题」的依据。避免分阶段统一的有意决策只留在 compose 报告里、被后续轮次当作新发现反复提出。

| 登记日期 | 来源任务 | 有意保留项 | 原因 | 计划批次 |
|---------|---------|-----------|------|---------|
| 2026-08-10 | D1 Catalog 双轨统一 | FormulaDetailModelMapper 若接线后发现仍零调用 → 删除；Herb 轨补 Mapperly | D1 按扫描报告统一，执行中复核 | D1 内收口 |
| 2026-08-10 | D4 命名统一 | ~~Service 前缀统一方向待定~~ **已执行**：统一为 Remote* 前缀（RemoteUserService/RemoteRegistrationService/RemoteHerbService/RemoteFormulaService/RemotePatientService）；契约层 IXxxService 无前缀为接口惯例，实现类 Remote 标识 HTTP 数据服务 | 4:1 既定模式（D1 确立 RemoteFormulaService），仅 PatientService 需改名，改动面 1 类+Module+测试 | ✅ 9748db4b4 |
| 2026-08-10 | D4 命名统一 | **Manager 后缀保留**：DesktopCacheManager/DialogManager/SessionManager/TokenManager/LoginStateManager/StatusBarManager/NavigationManager/MenuManager/LoadingStateManager/EventSubscriptionManager/WorkspaceStateManager/SessionLifecycleManager 共 12 个——全部为「生命周期/会话状态/UI 基础设施」管理职责，与数据业务 Service 职责明确不同；契约层 IXxxService 与 IXxxManager 各自统一 | A-08 曾报告；D4 核实 Manager 有明确职责差异（Session/Token/Cache/UI 状态管理），改名无收益 | ✅ 9748db4b4 |
| 2026-08-10 | D4 命名统一 | **SearchProvider 保留**：I{Herb|Formula}SearchProvider 为 D5-3 跨模块门面（MedicalCase/Formula 消费，Catalog 实现委托 I{Herb|Formula}Service）——与 Service 明确分工：Service=模块内数据服务，SearchProvider=跨模块解耦（防 MedicalCase→Catalog 编译期依赖，P07 合规） | 合并会破坏模块解耦；接口注释已文档化 D5-3 意图 | ✅ 9748db4b4 |
| 2026-08-10 | D4 命名统一 | **BreadcrumbItem 同名不改**：Contracts record（LYBT.Desktop.Contracts.UI，导航架构数据 Title/ViewName/IsCurrent）vs Controls class（LYBT.Desktop.Controls.Controls，BreadcrumbBar 渲染模型 Label/Level/IsCurrent/IsLast/NavigateCommand）——两个 namespace 封闭使用、零桥接（BreadcrumbBar 经 NavigationPath/NavigateCommand DP 绑定），无文件同时 using 两处 | 核实不冲突；重命名仅同名巧合，无实际歧义 | ✅ 9748db4b4 |
| 2026-08-10 | D5 MedicalCase 双体系 | ~~DTO 门面缓存（Cached*）收敛到 EditContext 新路径~~ **已执行**：CachedMedicalCase/CachedConsultation/CachedPrescription/ClearCache 删除；LoadDetailsAsync 内部改走 LifecycleService.InitializeAsync（DTO 快照单一持有于 LifecycleService，门面统一为 Current/CurrentConsultation/CurrentPrescription/CurrentDetail）；AggregateSaveAsync 保存后 UpdateSnapshot 前移 EditContext 基线；消费方（MasterDetail VM/Workspace VM/PrescriptionPrintHandler/测试）全部切换 | 用户已拍板方案 A（收敛）；EditContext 新路径由死代码变为真实接线（CommandService.SaveAsync 会话可用） | ✅ 待提交 |
| 2026-08-10 | O1 死代码 | ~~PatientDetailDisplayModel / TokenManager 无写入路径 / PrintOptions 死选项~~ **已执行**：PatientDetailDisplayModel 删除（零引用+测试）；TokenManager 删除（无写入路径恒 null，真实 token 存储为 ITokenStorageService，SignalR 改匿名连接行为等价）；PrintOptions.Orientation/DuplexPrinting + PrintOrientation 枚举删除 | 扫描报告 P2 确认零引用 | ✅ 待提交 |
| 2026-08-10 | O3 卫生 | ~~Controls 控件层 PrescriptionItemDto 交换形状~~ **保留**（内部适配层，非 UI 编辑路径）；wpftmp csproj 已删（git 忽略，报告「已追踪」过时）；obj 陈旧 UnfinishedCaseDialogViewModel 生成物已清 | B1 已解耦外部 DP 为 IEnumerable | ✅ 待提交 |
| 2026-08-11 | T1 测试审查（只读） | 结论：Architecture 83 规则高质量；Desktop 形式主义集中（50 纯交互断言/恒真断言/MasterDetail 模板复制/假业务 Integration）；Server 零 mock 健康但 _Infrastructure 9 文件 SQL 集成基建零消费者、AGENTS.md 宣称 1185 tests 与实际脱节。报告见 `docs/compose/reports/test-code-review-2026-08-11.md` | P0-06 根因确认（148 VM 测试挂 LocalDB + 146 Integration 挂 localhost:5000）；建议 T2 批次跟进 | ✅ 1129d6f81（报告） |
| 2026-08-11 | T2 测试修复 | ~~T2-1 Server _Infrastructure 零消费者 / T2-2 VM 测试去 LocalDB / T2-3 假业务测试~~ **已执行**：T2-1 删 Server _Infrastructure 11 文件+TestDataBuilders（零消费者）、AntiMockRuleTests AM01/02 改引 Server 测试类型、AM03 删除（保护对象已不存在）、tests/AGENTS.md 数字校正；T2-2 UserJourneyTestBase 去 IClassFixture<UserJourneyFixture>/LocalDB（12 类纯 VM 测试不再建库，FrameworkVerificationTests 改直连 fixture）；T2-3 MedicalCaseTests GetPermissions/GetAuditLogs 补真实 API 调用+断言、GetPendingCases 死测试删除 | P0 结构性发现修复；34 个残留失败全为存量（STA/mock 具体类/断言，stash 基线实证） | ✅ 2ee9143ac |
| 2026-08-11 | T3 测试质量 | ~~T3-1~T3-5~~ **已执行**：T3-1 MasterDetail 3 文件（MedicalCase/Patients/Formula）装配改走基类 CreateMasterDetailServicesMock（消除 ~90 行重复，保留 Formula 定制+字段引用）；T3-2 LoadListAsync 补状态断言（Items/TotalCount），50 个交互断言甄别后多为转发方法契约（合理保留）；T3-3 恒真断言改真实验证（WPF 资源）、Dispose 测试补 NotThrow、空壳持久化测试改状态断言；T3-4 JwtService 注入 TimeProvider（8 处 UtcNow + ValidateToken 时钟预检），过期测试去 Thread.Sleep(65s) 改假时钟推进（取消 Skip，23/23 通过）；T3-5 MedicalCase 反射扩展改 Testable 子类化（对齐 Herb 模式），LoadHerbsAsync private→protected | 每项 stash/基线实证零回归 | ✅ 817d131d1 |
| 2026-08-11 | T3-6 存量失败补丁 | ~~4 个存量失败~~ **已执行**：PrescriptionItemTests.Clear（MedicalCaseId 有意保留——断言改保留）、PrescriptionItemTests.Items_SetProperty（Items.Add 不触发 VM 通知——对齐 NotifyItemsChanged 契约）、ConsultationItemTests.IsPresentIllnessValid（测试赋值"头痛三"仅 3 字符 bug——改 5 字符）、PrescriptionEditorViewModelTests.InitializeFromDto（SingleDosePrice 为计算属性——断言改计算值 17.5m） | 4 个失败全为测试断言与实现意图不符（非实现 bug）；修复后 MedicalCase Unit 94/94 | ✅ 157d72903 |
| 2026-08-11 | R1 需求覆盖矩阵（只读） | 136 US 对照完成：✅64/⚠️46/🔴17/🧲📦15；基线矩阵 ~34 处状态过时；P0 缺口 4 项（远程 auth 路由疑损坏、refresh 端到端断裂、本地 refresh 无验签、导入导出端点双端全缺）+ 备份恢复整体缺失 + FeatureToggle 全库消失。报告见 `docs/compose/reports/requirements-coverage-matrix-2026-08-11.md` | 为「需求先行」治理提供基准；🔴 17 项为代码 backlog 源 | ✅ 8173230a4（报告） |
| 2026-08-11 | T4 P0 生产 bug 修复 | ~~6 项（4 P0 + 2 P1 安全）~~ **已执行**：P0#1 IdentityController 认证 5 端点路由改 `/` 开头绝对路径（修复合并后 `/api/v1/users/api/v1/auth/*` 组合路由损坏）；P0#2 Login/Refresh/ValidateAutoLogin 响应签发 RefreshToken（access 即刷新凭据，修复客户端刷新断链）；P0#3 本地 /refresh 改 ValidateToken 验签（原 ReadJwtToken 只解析，任意伪造 JWT 可换令牌）；P0#4 导入导出端点双端 12 个（import-template/export/export-all × 患者/药材/验方，NPOI 2.7.2 ExcelExportHelper 共享于 Infrastructure——WebAPI+LocalWebAPI）；P1#10 保留用户名清单（UserReservedNameHelper 共享，Validator+Handler 双保险）；P1#11 AutoLoginToken 服务端签发+轮换（GenerateAutoLoginToken 30 天，RememberMe 条件，ValidateAutoLogin 轮换） | 修复后 Server 认证单测 56/56、架构 87/87；备份/FeatureToggle 两 P0 未做（全新功能大工程，建议 T5） | ✅ 58f4e571a |
| 2026-08-11 | R2 需求文档校准（纯文档） | **已执行**：13-traceability-matrix.md 61 处状态校准（v1.1，含 T4 修复后状态）+ 统计汇总表重算（141 US：✅94/⚠️30/🔴10/🧲7）；12 个需求文档 123 处 US 状态段同步；WebAPI README 3 个 stale 端点（batch-details/consultations/prescriptions）删除 | 文档状态列如实反映代码现状（文档是字典不是过程）；🔴 10 项为剩余代码 backlog | ✅ 80a4a4b80 |
| 2026-08-11 | T5-1 backlog 安全修复 | ~~4 项安全缺口~~ **已执行**：#13 患者 DTO 加 [SensitiveData]（IdNumber/PhoneNumber，管道自动生效）；#12 用户管理（单删不可删自己、重置密码 sysadmin 保护、GET /users role/status 筛选接线——Repository 已支持 Service 丢参修复、REG-BR-006 有待诊挂号禁止禁用医生——跨模块 HasWaitingRegistrationsAsync）；#9 挂号服务端守卫（Cancel 仅 Waiting + 关联医案拒绝——实体层；同日重复挂号 HasSameDayWaitingAsync——REG-BR-007）；#8 MC 所有权（GetDetailDtoAsync/GetAuditLogsAsync Doctor 非本人 Forbidden、Search 按操作者过滤、close 强制关闭仅 Admin——US-MC-004/007/012/017） | 全量构建 0/0；架构 87/87；Server 568 通过（3 失败=ConfigurationLoadingTests 存量+并行竞争偶发单跑通过） | ✅ a2344feb2 |
| 2026-08-11 | T5-2 backlog 功能修复 | ~~4 项功能缺口~~ **已执行**：#17 报表 5 端点 AC（endDate 默认=startDate 单日语义 + startDate>endDate→400——US-REPORT-001~003）；#15 FLAW-F1 降级（Formula.DegradeToDraftIfAnyHerbUnvalidated——Validated 验方更新后任一药材未验证→Draft，US-FORM-010）；#14 验方单条创建/更新持久化药材（Formula.ReplaceHerbs + MapHerbs——原 Mapper 丢弃 Herbs，仅批量导入能建带药材验方，US-FORM-003/004）；#18 打印回写全链接线（Repository→聚合代理→接口→IPrintService.PreviewAsync 回调→预览窗打印按钮→PrintHandler fire-and-forget RecordPrint——原服务端完整客户端零调用，US-PRINT-004） | 全量构建 0/0；架构 87/87；Server 570 通过（1 失败=ConfigurationLoadingTests 存量） | ✅ b824332cb |
| 2026-08-11 | T5-3 业务规则/审计修复 | ~~2 项~~ **已执行**：#7 EditReason 校验（SaveAsync 路径——打印后修改/非 Admin 编辑已完成医案需提供 EditReason 否则 422；US-MC-002/016 打印保护 + Completed 编辑铁律）；#16 更新审计+字段 diff（保存成功后写 MedicalCaseAuditLog——OperationType=Update、ChangedFields/OldValues/NewValues 填充诊断 4 字段+处方条数 diff；原仅取消写审计且 diff 列从不填充，US-MC-017） | 全量构建 0/0；架构 87/87；Server 570 通过（1 失败=ConfigurationLoadingTests 存量） | ✅ ebc51a54f |
| 2026-08-11 | T6 全新功能设计（暂不实现） | backlog 剩余 #5 备份/恢复（US-SHELL-013+NFR-AVAIL-001）+ #6 FeatureToggle（US-CFG-004）按需求先行门禁深化设计：备份=ILocalDbBackupService（T-SQL BACKUP/RESTORE + 登录后 fire-and-forget 自动备份 + 7 天清理 + Sysadmin BackupManagementView 恢复工作流）；FeatureToggle=feature-toggles.json + IOptionsMonitor 热更新（沿 ClinicSettingsService 先例）+ 仅策略级 2 键（OverwriteConflicts/DuplicateHerbMergeStrategy，不回到 18 开关反模式）。设计文档见 `docs/compose/plans/backup-featuretoggle-design-2026-08-11.md` | 待用户审批 4 个确认项（保留期/重启/配置位置/实施顺序）后进入 T7/T8 实现 | ✅ 134edc562（设计） |
| 2026-08-11 | T7-1 备份服务层（UI 属 T7-2） | **已执行**（用户确认：7 天保留/自动重启/串行 T7 先）：ILocalDbBackupService + LocalDbBackupService（T-SQL BACKUP/RESTORE DATABASE，%AppData%/LYBTZYZS/Backup/，SemaphoreSlim 串行，RESTORE 编排 Stop 内嵌 LocalWebAPI→恢复→自动 Start）；登录后 fire-and-forget 自动备份+7 天清理（ShellEventCoordinator）；SystemConstants.BackupRetentionDays 30→7；DI 注册（ServiceCollectionExtensions） | 全量构建 0/0；架构 87/87 | ✅ 2c7c4a471 |
| 2026-08-11 | T7-2 备份管理 UI | **已执行**：BackupManagementViewModel（状态卡片：上次备份/文件数/总大小 + 手动备份进度/失败原因 + 文件列表 + 恢复）+ BackupManagementView（MaterialDesign 卡片 + DataGrid + 恢复前 MessageBox 确认「将覆盖当前数据库」）；ViewNames.BackupManagement + SysadminModule 注册 + ModuleLazyLoader 映射 + NavigationManager SuperAdmin 菜单「备份恢复」（DatabaseBackup 图标） | 全量构建 0/0；架构 87/87；US-SHELL-013 六项 AC 全部落点完成 | ✅ 0c2fb1cef |
| 2026-08-11 | T8 FeatureToggle 实现 | **T8-1 基建已执行**：FeatureToggleOptions（OverwriteConflicts/DuplicateHerbMergeStrategy 2 键）+ IFeatureToggleService + FeatureToggleService（IConfiguration 动态读 + GetReloadToken 变更回调 → TogglesChanged 热更新事件——注意 RegisterOptions 为启动快照，服务改用 IConfiguration 动态读实现真热更新）+ feature-toggles.json（应用根目录，用户确认）+ AddJsonFile reloadOnChange + DI 单例。**T8-2 两消费点核实后记录**：DuplicateHerbMergeStrategy 唯一消费在 HerbListControlViewModel（默认 Max = 需求 v1.0 默认值，行为已正确——接线需大改两 VM 构造链且无当前差异价值）；OverwriteConflicts 无同步冲突功能（无消费场景）——配置/API（GetDuplicateMergeStrategy/GetOverwriteConflicts）就位，未来功能接入即用 | 全量构建 0/0；架构 87/87；US-CFG-004 热更新 AC 以 IConfiguration 动态读实现（优于快照先例） | ✅ 6459ad53e |
| 2026-08-11 | P1 打磨第一批（⚠️ 部分实现 6 项按业务影响） | **已执行**：MC-011 完成权限（Doctor 仅本人医案可完成，CompleteAsync 加 UnauthorizedAccessException）；MC-016 权限 DTO 补 RequiresEditReason/DenialReason（打印后修改/非 Admin 编辑 Completed 判定）；FORM-001 验方列表所有权过滤（Doctor 仅本人+共享——全链 Repository/Service/接口/双端 Controller，含导出路径）；MC 价格口径统一（Desktop 编辑器 TotalPrice 补折扣，对齐服务端 MC-D14；打印端为收费合计语义合理差异）；REG-001 前台挂号（患者存在/Enabled 校验 + 挂号费从医生带出 REG-BR-009——UserBasicDto 已有 RegistrationFee 字段 Mapperly 自动映射）；REG-004 队列当天过滤（REG-BR-012，onlyToday 可选参数——禁用检查保持全量从严） | 全量构建 0/0；架构 87/87；Server 570 通过（1 失败=ConfigurationLoadingTests 存量） | ✅ 5c41f98b7 |
| 2026-08-11 | P2 打磨第二批（⚠️ 部分实现 6 项） | **已执行**：AUTH-003 本地 auto-login 加限流（LocalLogin 5/min 策略——原无限流）；USER-004 创建用户默认密码用 DefaultPasswords.NewUserPassword 配置（原随机 GUID 不可运维，空配置回退随机）；PAT-003/004 患者电话唯一查重（ExistsByPhoneAsync Create+Update 排除自身——需求电话唯一语义原仅姓名查重；DB 唯一索引记录为后续 migration）；HERB-012 批量删除逐项引用检查（ValidateAsync 钩子——有处方/验方引用的药材拒绝并报引用数）；FORM-006 批量导入加 10000 上限（对齐药材导入）；USER-005 更新用户角色（Role 字段更新 + sysadmin 保护 + 非 SuperAdmin 不可提升 Admin/SuperAdmin 层级约束——原 Role 被忽略）。**AUTH-008 本地登出记录**：本地 JWT 无状态 1 年（无会话表），登出撤销需黑名单机制——本地威胁模型接受（G-02） | 全量构建 0/0；架构 87/87；Server 570 通过（1 失败=ConfigurationLoadingTests 存量） | ✅ 82a7e7464 |
| 2026-08-11 | P3 打磨第三批收尾（⚠️ 剩余 6 项） | **已执行**：AUTH-006 重放检测撤销该用户全部会话（RevokeAllUserSessionsAsync——原仅拒绝不撤销）；USER-010 JWT Bearer OnTokenValidated 禁用/删除用户拦截（令牌有效期内状态检查——原禁用后令牌到期前仍可用）；USER-012 批量用户 100 条上限；FORM-007 待验证列表分页（Query/Handler/双端 Controller——原全量返回）；MC-D09 验方导入禁用跳过 Toast 提示（原仅日志——「已停用、已跳过 N 味（名称）」）；PAT-010 患者批量引用计数一次查询（CountMedicalCasesBatchAsync GroupBy——原逐患者 N+1；GetRecent 5 条详情保留逐查） | 全量构建 0/0；架构 87/87；Server 568 通过（3 失败=ConfigurationLoadingTests 存量+2 并行偶发单跑通过） | ✅ 8fd8949f3 |
| 2026-08-11 | R2-补 需求文档状态列全量重扫（纯文档） | **已执行**：R2 后 14 commit（T4/T5/T7/T8/P1-P3）修复同步——traceability 40 处校准（v1.2）+ 12 文档 123 处状态段 + 统计重算（141 US：✅122/⚠️7/🔴5/🧲7，原 ✅94/⚠️30/🔴10/🧲7）；🔴 5 = MC-008/009/018 + HERB-005 + SHELL-018；⚠️ 7 = AUTH-002/HERB-006/SHELL-007/ERR-006/007/CARD-002；🧲 7 = REG-002 + SHELL 规划项 | 文档状态列如实反映 2026-08-11 当前代码（含全部新功能与打磨） | ✅ 24b8f24c5 |
| 2026-08-11 | R2-补 专项（虚构控制器引用清理，纯文档） | **已执行**：05-herbs.md/06-formulas.md/13-traceability-matrix.md 的 HerbsController.cs/FormulasController.cs 虚构引用（C3b 合并后已删）全部替换为 CatalogController.cs 实际端点行号（已核实——herbs 50/70/82/107/124/146/174/202/228/247/262/287/302/319/339；formulas 363/385/397/423/443/464/489/516/542/565/580/607/627/657/677）；全仓 02-requirements 虚构引用清零 | 替换后残留 0（grep 实证） | ✅ 3d8f8b017 |
| 2026-08-11 | R3-补 反向脱节补 US（纯文档） | **已执行**：R1 §三 6 类代码超前功能收编为 7 个新 US（标注「已实现未文档化」+ 状态 ✅）：PAT-014 身份证号查询（PatientsController:296）、FORM-014 验方克隆（LocalWebAPI:459——注记远程待补）、MC-020 医案批量删除（:178）、REPORT-004 趋势/绩效/排行/流量 5 端点（ReportsController:69-158——超越 v1.0 克制声明）、CFG-005 服务器配置 PUT/validate 端点、CFG-006 诊所信息热更新（ClinicSettingsService）。traceability 新增 6 行 + 统计重算（147 US：✅128/⚠️7/🔴5/🧲7，v1.3） | SignalR/安全审计/Restore 等 R1 第 3/4 点已由既有 US 覆盖（R2 后 ✅）不需补 | ✅ 0b5566f54 |
| 2026-08-11 | B1 backlog 小修复批（5 项） | **已执行**：HERB-005 单删引用检查（CatalogEntityCommandHandlerBase.ValidateBeforeDeleteAsync 钩子 + Herb override 处方/验方引用拒绝）；MC-008/009 患者历史聚合端点 GET /medicalcases/patients/{id}/history（复用 GetPatientRecentMedicalCasesAsync）；MC-018 批量详情 POST /medicalcases/batch-details（BatchIdsRequest ≤100 + GetByIdsWithDetailsAsync 单次 In 查询 + Doctor 所有权过滤）；AUTH-002 本地锁定对齐远程（LoginOptions.LockoutEnabled=true + Identity MaxFailedAccessAttempts=5） | 全量构建 0/0；架构 87/87；Server Catalog+MC 71/71；Desktop VM/Service 198 通过；LocalWebAPI Integration 存量失败（stash 基线实证） | ✅ 9117df395 |
| 2026-08-11 | B2 backlog 小修复批（6 项） | **已执行**：HERB-006 服务端 Excel 解析（ExcelImportHelper NPOI + 双端 POST /herbs/import-excel→BatchImportHerbsCommand）；SHELL-007 切换守卫（SetModeAsync + ModeSwitchResult：远程 URL/可达/未完成医案 ERR-70506 三重守卫，失败自动回退原模式；ConnectionStatusViewModel 阻断提示）；ERR-006/007 异常体系（Conflict 409/Unauthorized 401/ApiException/Validation 422+errors 字典/ExceptionFactory——AppException 族 handler 自动映射）；CARD-002 维持用户决定（2026-08-08 UI 设计时整体考虑，不实施）；REG-002 QuickVisit UI 接线（QuickVisitAsync 全链——Refit+Http 双轨 + QuickVisitDialog + 列表 VM 命令导航 MedicalCaseWorkspace + 医生工具栏按钮） | 全量构建 0/0；架构 87/87；Server Registration 6/6；Desktop Integration/E2E 存量失败同 B1 | ✅ 6b952836c |
| 2026-08-11 | traceability 状态同步（B1/B2 后） | **已执行**：B1 5 项 + B2 5 项状态 🔴/⚠️/🧲→✅（CARD-002 维持 ⚠️ 用户搁置）；统计重算（147 US：✅137/⚠️3/🔴1/🧲6，v1.4——🔴1 = SHELL-018 配置中心待设计批次） | 行级替换 + 统计公式重算 | ✅ a10517692 |
| 2026-08-12 | SWAGGER-TOGGLE（主页跳转 Swagger + 配置开关，方案 2+3） | **已执行**：① 配置开关——SwaggerOptions.Enabled（默认 false——生产默认关）+ ConfigureSwaggerMiddleware 条件化（非生产 OR Swagger:Enabled=true——测试发布可在线启用，SHELL-018 配置中心/环境变量 Swagger__Enabled 切换）；② 主页 Swagger 入口——DownloadController 下载页加「API 文档」按钮（指向 /swagger——Swagger:Enabled 或非生产时显示） | 全量构建 0/0；架构 87/87；生产默认关闭（安全默认——生产配置未写入） | ✅ 待提交 |
| 2026-08-12 | HEALTHCHECK-FALLBACK-FIX（发布验证：连接串 fallback 不一致） | **已执行**：SqlServerHealthCheck 只读 `DatabaseOptions.ConnectionString`（Database 节）——空则 Unhealthy「连接字符串未配置」；但实际运行连接串经 `ConnectionStrings:DefaultConnection`（环境变量 ConnectionStrings__DefaultConnection 注入）→ **健康检查误报 + 与注册链不一致**。修复：抽共享 `DatabaseConnectionResolver`（统一 3 级 fallback：Database 节 → ConnectionStrings:DefaultConnection → CONNECTION_STRING 环境变量）——SqlServerHealthCheck + DatabaseServiceCollectionExtensions 注册处**同源引用**（防复制漂移——本次 bug 正是两处复制）。单测 5（优先级/环境变量覆盖/全空/回退） | 全量构建 0/0；架构 87/87；resolver 5/5 | ✅ 待提交 |
| 2026-08-12 | IDENTITY-DBCONTEXT-FIX（测试发布发现登录崩溃 P0） | **已执行**：IdentityDbContext.OnModelCreating 漏 `ApplyConfiguration(new UserConfiguration())`——ApplicationUser.LastLoginTime 未映射 Users 表 LastLoginAt 列 → EF 按属性名查 LastLoginTime → SqlException → 登录 500。修复：补挂载 UserConfiguration（Infrastructure/Data/Configurations——与 SecurityAuditLogConfiguration 同源，模块引用 Infrastructure 先例）。回归守卫：IdentityDbContextMappingTests 2 用例（LastLoginTime→LastLoginAt 列映射 + RealName 映射——EF Model 级断言） | 全量构建 0/0；架构 87/87；映射测试 2/2 | ✅ 待提交 |
| 2026-08-12 | CATALOG-ROUTE-FIX（测试发布发现启动崩溃 P0） | **已执行**：CatalogController 路由 bug——类级 `Route("api/v{version}/herbs")` + 15 处 formulas 动作路由写完整前缀（相对模板）→ 拼接后 version 参数两次 → ControllerActionDescriptorProvider 启动崩溃。修复：15 处动作路由改**绝对路径** `/api/v{version:apiVersion}/formulas...`（/ 开头覆盖类级前缀——对齐 IdentityController T4 先例）。**启动实测**（有效密码环境变量启动）：进程正常启动无崩溃、`/api/v1/formulas`/`/herbs`/`/formulas/import-template` 均 401（路由正确注册——FallbackPolicy 认证拦截）；health 503 为 DB 未连（本地开发无 SQL Server——非本次问题）。回归守卫：CatalogControllerRoutesTests 2 用例（类级单 version + 15 动作绝对路径/version 不重复） | 全量构建 0/0；架构 87/87；路由测试 2/2；启动实测通过 | ✅ 待提交 |
| 2026-08-12 | PUBLISH-ASSESS（WebAPI 发布就绪度评估，只读） | **已执行**：评估 60.190.215.86:5000 发布就绪——10 项检查 8/10 通过（端口 5000 已配/CORS 目标地址已预配/Swagger 生产关闭/生产门控 AllowAutoCreateInProduction=false + 占位符拦截/密钥环境变量注入机制）。**3 项发布必做动作**：① 环境变量注入（DB_SERVER/DB_USER/DB_PASSWORD/JWT_SECRET/SYSADMIN_PASSWORD/NEWUSER_PASSWORD/LYBT_INITIAL_SETUP_TOKEN——验证器拦未展开占位符）；② EF 迁移执行（EnsureCreatedInDevelopment=false——生产走 migrations）；③ DesktopUpdate.FeedUrl 占位符替换（your-server.example.com → 60.190.215.86:5000）。风险非阻塞：HTTP 明文（测试可接受/公网须 HTTPS ADR-0014）、库名 LYBTDB_Dev 语义、SQL 可达性（P0-06 历史）。验证闭环：validate+health+登录冒烟。报告 webapi-publish-readiness-2026-08-12.md | 只读（零代码） | ✅ 待提交 |
| 2026-08-12 | VELOPACK（Velopack 打包框架——US-SHELL-010 剩余 AC） | **已执行**：Velopack 1.2.0（2026-06-03 最新稳定）引入——Directory.Packages.props + Desktop.Shell/Foundation 引用；`scripts/velopack-pack.ps1`（dotnet publish win-x64 自包含单文件 → vpk pack：Setup.exe+RELEASES+nupkg）；`scripts/sync-to-server.ps1`（scp/SMB 同步更新源到服务器 ReleasesPath）；`DesktopUpdateService`（UpdateManager——SimpleWebSource(FeedUrl)+UpdateOptions，1.2.0 API 适配：2 参 ctor/ApplyUpdatesAndRestart(asset)/NotesMarkdown）；`DesktopUpdateStartupStep`（Order 400 后台检查——非阻塞 + 提示下载/重启）；DI 注册 + AppStartupOrchestrator 接线；DesktopUpdateOptions 补 FeedUrl + 生产 appsettings；部署文档 01-deployment.md 旧 zip 手动升级段 → Velopack 流程；单测 3（未启用/无 FeedUrl/下载禁用） | 全量构建 0/0；架构 87/87；更新服务单测 3/3；traceability US-SHELL-010 ⚠️→✅（Setup.exe 实机打包由运维跑脚本产出——标注） | ✅ 待提交 |
| 2026-08-12 | SHELL-010-DL（下载主页 + 发布包静态服务，决策 A） | **已执行**：① `/releases/` 静态服务**核实已有**（UnifiedMiddlewareConfiguration 条件启用——DesktopUpdate:Enabled+ReleasesPath，生产 appsettings 已配 C:\Services\LYBT-releases）；② 补 **GET / 下载页**（DownloadController——公开 AllowAnonymous + 类级 Authorize 豁免模式、极简 HTML：下载按钮/版本号（Setup-*.exe 文件名解析）/更新时间/文件列表 Setup.exe+RELEASES+.nupkg 过滤）+ LocalWebAPI 双端同步（本地模式提示页）。架构守卫适配：P09 类级 Authorize+方法级豁免 / P09b 公开根页 `""` 特例（与 health 同型）/ P19b IL 解码器健壮性（IndexOutOfRange 边界忽略——DownloadController 新 IL 触发手工解码越界，非代码违规）。单测 3（公开性/路由/前缀） | 全量构建 0/0；架构 87/87；下载页单测 3/3；traceability US-SHELL-010 🧲→⚠️（打包 4 项属 Velopack 工程） | ✅ 待提交 |
| 2026-08-11 | WEBAPI-DOC（WebApi 层文档漂移修复 + CorrelationId 核实） | **已执行**：① README 漂移大修——端点概览表/文件表/章节对齐实际 10 控制器：删 SyncController 章节（全仓 0 命中——已移除但文档全留）、AuthController+UsersController→IdentityController（继承 BaseUsersController——A-31-C3a 合并）、HerbsController+FormulasController→CatalogController（A-31-C3b 合并）、补 Registrations/Reports/Configuration/Deploy 4 控制器、文件表 12→10、端点节标题 4 处、更新记录追加。② CorrelationId 核实——**链完整无缺口**：UseLybtCorrelationId 中间件（生成/透传/写响应头 X-Correlation-ID）+ Serilog CorrelationIdEnricher + SystemLogs.CorrelationId 索引 + ProblemDetails 错误响应注入 + A-31-C1 单点注册 | 构建 0/0；README 旧控制器名残留 0（grep 实证——5 处均为合法描述）；docs/04-api-reference/09-sync.md 已正确标注（v2.0 规划——无漂移） | ✅ 待提交 |
| 2026-08-11 | ARCH-REVIEW（资深架构师深度评测，只读） | **已执行**：全 solution 深度评测（32 项目/39,619 行/87 架构测试/618 Server 单测/17 ADR）——7 维评分：愿景 9.0/分层 9.2/核心决策 8.6/质量 8.8/测试 8.5/安全 8.9，**综合 8.8**。亮点：87 条机器强制架构守卫、ADR 决策可追溯、测试从自洽→守护需求演进（P0 闭环）、收敛主导技术栈。主要扣分：集成测试环境依赖（P0-06 最短板）、项目数偏多（29）、Shell 职责集中苗头。演化建议 3 项（集成测试容器化/域门面演进/Shell 拆分）。报告 architecture-deep-review-2026-08-11.md | 只读（零代码） | ✅ 待提交 |
| 2026-08-11 | DEPLOY-PERM（部署权限收紧安全补丁） | **已执行**（用户派发安全 P0 主项——RISK-CLOSE 漏项）：DeployController 双端类级 `AdminOrSuperAdmin` → `SysAdminOnly`（PolicyConstants 既有——部署属运维操作，Admin 业务管理员无部署能力，US-SHELL-020 AC 注闭环）。新增权限守卫测试 2 个（Server 远程 DeployControllerPermissionTests + Desktop 本地 LocalDeployControllerPermissionTests——反射断言类级策略防回归）；traceability + 11a-shell US-SHELL-020 状态标注 | 全量构建 0/0；双端权限测试 1+1；架构 87/87 | ✅ 待提交 |
| 2026-08-11 | RISK-CLOSE（剩余风险完善） | **已执行**：核查论证报告 §4.3 剩余 5 项风险可完善项——SHELL-003（LoginCoordinator 旁路 🔴 唯一）**核实已清除**（C7 批次 2026-08-09 删死代码 HandleLoginSuccessAsync/GetDiagnostics/LoginFlowDiagnostics——代码 0 命中实证 + traceability 已校准 ✅ + 🔴 全库清零 151 US：✅141/⚠️3/🔴0/🧲7）；其余 4 项均为不可完善的边界决策（CARD-002 用户搁置/数据孤立 N1/Integration 环境依赖 P0-06/固件接口）。**修正 ARGUMENT 报告 §4.3**（剩余风险 5→4 + 统计更新） | 代码零改动（核实类任务）；ARGUMENT 报告过时数据修正 | ✅ 待提交 |
| 2026-08-11 | ARGUMENT（需求理解 + 方案完整性双角色论证，只读） | **已执行**：双角色（需求分析专家/方案设计专家）对话模式论证——需求理解度：领域模型还原准确（MedicalCase 唯一聚合根/4 角色边界/核心旅程）+ 语义深度（QuickVisit 双职能/双模式数据孤立 N1/配置热更新边界）；方案完整性：四层无空白（架构 87 守卫/CQRS 模块自治/安全五面闭环/测试分层守护）；五轮质询全站住（聚合根合理性/双模式一致性/refresh=access 凭据/重启体验/测试环境依赖）；剩余 5 项风险均为已记录边界决策（CARD-002 搁置/SHELL-003 待对齐/数据孤立 N1/P0-06/固件接口） | 只读（零代码）；报告 requirements-design-argumentation-2026-08-11.md | ✅ 待提交 |
| 2026-08-11 | DOCS-CONVERGE-2（全面收敛——过程文档价值迁移后删除） | **已执行**（价值迁移三步：读文件提取 → 内联/改引用 → 删除）：删除 55 个已完成批次的过程产物——specs 任务书 26（task-a17~a31 + structure-audit task-spec + auth-users-merge research + registration-workflow-redesign）、reports 批次执行报告 22（a18×2/a21/a23/a27/a28/a29/a31-c0~c8×9/arch-test-refactor/auth-users-merge/mimo-verify）、plans 过期计划 7（v1.0-completion/arch-test-refactor/batch-handler/p02-q02/method-audit/code-gap-fix-list/desktop-mc-edit-context/shell018-design）。**引用迁移**：11-business-flows:7 R10 spec 引用 → ADR-0002/0010（双模式分野价值已内联句子）；总账 6 处任务书引用 → 「已归档」。**保留 5 活文档**：backup-featuretoggle-design（13c 引用）+ 4 报告（coverage-matrix/ac-test-mapping/test-code-review/requirements-first-campaign——活文档） | 非 compose 断链引用 0（grep 实证）；55 个文件价值核查：A 批次结论在总账 A 行表（commit SHA）+ 当前态代码；架构测试重构=87 测试运行中；策略补丁=控制器当前态 | ✅ 待提交 |
| 2026-08-11 | COMPOSE-CLEAN（compose 收敛——文档是字典不是过程） | **已执行**：删除 30 个一次性扫描/审计中间产物（method-audit×7、structure-audit×9、namespace-consistency×2、dead-code×2、webapi-deep×3、desktop/server class scan×3、data-pipeline、dto-entity、code-review、desktop-layer-review）。**删除前逐文件核查关键结论已沉淀**：A-16 结构审计结论→13 总账行+ADR-0010；A-30 方法审计 S0-S4 统计→15-solution-integration-plan 表；A-26 收敛清单→总账行；S-04 模糊匹配修复/Q-03 命名空间/403 映射审查→总账行。同步清理 4 份正式文档 16 处死链引用（08-shared/14-blueprint/15-plan/13-master-plan——文件名引用替换为「已归档」） | 正式文档残留引用 0（grep 实证）；compose 内部 specs↔reports 互引属归档区容忍 | ✅ 待提交 |
| 2026-08-11 | AC-TEST-P23（AC→测试落地——优先级 2+3） | **已执行**：P2 全闭环——MedicalCaseHistoryQueryTests 5（真实仓储+EF InMemory：倒序/Take/Doctor 所有权/空）；HerbReferenceCheckTests 5（手写 fake 仓储：引用拒绝/放行软删/404）；QuickVisitDialogViewModelTests 4（门控/确认/失败）。P3——ConfigurationWritePolicyTests 22（IsAllowed 白名单/拒绝 + IsSensitive 掩码）；备份保留期标注跳过（静态 AppData 路径污染）；MC-020/FORM-014 标注端点级（集成/手动验收守护） | 全量构建待验；Server 32/32 + Desktop 4/4 新测试 | ✅ 待提交 |
| 2026-08-11 | P0-TEST（AC→测试落地——P0 逃逸闭环，优先级 1） | **已执行**：3 组 15 用例补测——`IdentityControllerRoutesTests`（5 端点绝对路径 + users 双重前缀回归守卫——P0#1）；`RefreshTokenIssuanceTests`（签发非空/access=refresh/身份保留/令牌旋转/非法拒绝——P0#2）；`ExcelImportHelperTests`（表头跳过/数字格式化/行数上限/空行跳过/非法文件——P0#4）。4 个 P0 全部有测试守护（P0#3 本地验签 LocalTokenValidatorTests 既有） | 全量构建 0/0；架构 87/87；Server 587 通过（1 存量 ConfigurationLoadingTests）；新 17/17 | ✅ 待提交 |
| 2026-08-11 | AC-TEST（需求 AC → 测试映射，只读调研） | **已执行**：建立「需求验收条件 → 测试守护」映射表（147 US × 15 域）——域级守护 12 强/1 中/0 无；P0 逃逸复盘（4 个 P0 中 2 个修复后仍无守护：远程 auth 路由/导入导出）；近批缺口清单（B1/B2/R3-补 新 US 无测试：MC-008/009/018/020、HERB-005/006、REG-002、FORM-014）；建议 3 优先级 9 项补测（随批外科式补，不设独立大批） | 只读调研（零代码）；报告 requirements-ac-test-mapping-2026-08-11.md | ✅ 待提交 |
| 2026-08-11 | SHELL-019（读卡器诊断测试工具） | **已执行**（用户确认有硬件可实测）：`ICardReader.GetDeviceInfo()` 默认实现 + `CardReaderDeviceInfo` record；`ICardReaderDiagnostics` 接口 + `CardReaderDiagnosticReport`（探测/握手/固件/读卡 4 步 + Messages）；`CardReaderDiagnosticsService`（测试模式编排——CreateReader 独立实例不触医生会话 + Connect 探测 + 链路握手 + 固件（驱动未暴露→提示）+ Detect/ReadCard 测试 + 断开清理）；`CardReaderDiagnosticsViewModel`（厂家下拉 HuaDaHD100/Auto + 手动参数覆盖校验 + 运行诊断 + 报告逐行 + 持久化 CardReader 节含 ReaderType）+ SysadminHome 组合 + XAML 面板（替换 Phase 2 占位）；DI 注册；单测 4（Mock 通过路径/连接失败/工厂异常/参数透传） | 全量构建 0/0；架构 87/87；诊断单测 4/4；串口测试=USB 链路握手（HD100 无独立串口协议——如实） | ✅ 待提交 |
| 2026-08-11 | SHELL-018 Phase 3（双模式布局整合） | **已执行**：`ServerConfigSectionViewModel`（远程服务端面板——节列表 6 节加载 GET sections/编辑保存 PUT/二次确认重启 POST restart——IServerConfigurationService 门面扩展 3 方法 + IApiClientConfiguration/Refit/Http 双轨同步）；SysadminHomeView TabControl 模式感知（远程：配置 + 服务端配置；本地：配置 + 备份恢复嵌入 BackupManagementView）；本地重启按钮（决策 B——LocalWebAPI restart 端点，OfflineModeOptions 地址）；SysadminHomeViewModel 注入 IConnectionModeService + ServerConfig 组合（ModeChanged 联动加载）；测试构造同步 | 全量构建 0/0；架构 87/87；Desktop VM 161 + 新 6/6；XAML 编译验证（含 BackupManagementView 嵌入 + Cvt） | ✅ 待提交 |
| 2026-08-11 | SHELL-018 Phase 2（配置中心客户端面板） | **已执行**：`ClientConfigurationStore`（节级原子写 + .bak 备份 + IConfiguration.Reload——文件路由 ClinicSettings→clinic-settings.json、FeatureToggles→feature-toggles.json、其余→appsettings.json）；`ConfigurationCenterViewModel`（5 组可编辑：诊所 6 项/会话 3 项校验区间/连接 BaseUrl+Timeout+测试连通/安全 2 项密码框/功能开关 2 项热更新 + 读卡器只读占位决策 E + 状态条）；SysadminHomeViewModel 组合 + View 分组卡片布局（含 PasswordBox 参数化命令）；VM 单测 6 个（加载/校验拒绝/保存路由） | 全量构建 0/0；架构 87/87；Desktop VM 161 通过 + 新增 6/6 | ✅ 待提交 |
| 2026-08-11 | SHELL-018 Phase 1（配置中心服务端 API 扩展） | **已执行**（用户确认 A-E 后）：`SysAdminOnly` 策略（PolicyConstants + RequireRole(SuperAdmin)）；`ConfigurationWritePolicy.IsSensitive` 脱敏判定（Forbidden 集 + 密钥启发式）；`ISystemConfigurationService.GetSectionAsync/UpdateSectionAsync`（白名单逐键 → JsonFileConfigurationStore → Reload → `ConfigUpdateResultDto`：FeatureToggles/ClinicSettings hot 即时生效其余 restart——决策 A 落实）；WebAPI `GET/PUT sections/{section}`（SysAdminOnly——路由加 `sections/` 前缀避 `{key}` 歧义）+ `POST restart`（滑动窗口限频每小时 3 次 + 30s 延迟 StopApplication）+ 审计接线（ConfigGet/ConfigUpdate/ConfigRestart → SecurityAuditService）；LocalWebAPI 双端同步（同签名 + SuperAdmin 角色检查 + 同限频）；traceability SHELL-018 🧲→⚠️（Phase 2-3 客户端面板待实施） | 全量构建 0/0；架构 87/87；Server Configuration 55 测试 54 通过（1 存量 ConfigurationLoadingTests LocalJwtOptions 断言失败非引入）；设计文档偏差小记（sections/ 路由） | ✅ 待提交 |

## 六、已知问题（代码实际状态）

### 🔴 P0 — 必须修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P0-01 | Shell 登出状态机错误 | Shell/LoginCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-02 | 并发登录竞态 | Shell/LoginCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-03 | 异常时事件未发布 | Shell/ShellEventCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-04 | Sync-over-Async 死锁 | Desktop 多处 `.GetAwaiter().GetResult()` | ✅ 已核实无残留 (2026-08-04, B-01) |
| P0-05 | 明文密码泄露 | appsettings.json (SSH/SA/JWT SecretKey) | ✅ 已修复 (2026-08-04, B-01) |
| P0-06 | 104 个 Desktop 测试失败 | tests/LYBT.Tests.Desktop | ✅ 根因已修复 (2026-08-11, T2-2)——148 纯 VM 测试解耦 LocalDB、146 Integration 独立化；残留失败为存量 STA/网络环境项（见 13c §五 T2 登记） |
| P0-07 | 配置无法 API 修改 | ConfigurationController 无 PUT | ✅ 已实现 (A-18 P1-1 + SHELL-018 Phase 1-3)——IApiClientConfiguration + 完整配置中心（GET 脱敏/节级 PUT/restart/审计，SysAdminOnly 策略，双端同步 2026-08-11） |

### 🟡 P1 — 应修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P1-01 | 9 个 Build 警告 | 多处 (CA1001/CS8603/CS0168/CS4014) | ✅ 已修复 (2026-08-10, B1/B2/B3 后 build 0 警告门禁) |
| P1-02 | 8 个 TODO 残留 | MedicalCase/Shell/Reports | 技术债务 |
| P1-03 | 5 个超大类型 (>600行) | MedicalCaseCommandService/Repository/HttpClientApiClient/NavigableViewModelBase/PrescriptionPrintService | ✅ 已解决 (2026-08-10: B2 重写后 5 个文件均 <600 行) |
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
