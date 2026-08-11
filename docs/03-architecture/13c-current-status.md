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
| 2026-08-11 | P3 打磨第三批收尾（⚠️ 剩余 6 项） | **已执行**：AUTH-006 重放检测撤销该用户全部会话（RevokeAllUserSessionsAsync——原仅拒绝不撤销）；USER-010 JWT Bearer OnTokenValidated 禁用/删除用户拦截（令牌有效期内状态检查——原禁用后令牌到期前仍可用）；USER-012 批量用户 100 条上限；FORM-007 待验证列表分页（Query/Handler/双端 Controller——原全量返回）；MC-D09 验方导入禁用跳过 Toast 提示（原仅日志——「已停用、已跳过 N 味（名称）」）；PAT-010 患者批量引用计数一次查询（CountMedicalCasesBatchAsync GroupBy——原逐患者 N+1；GetRecent 5 条详情保留逐查） | 全量构建 0/0；架构 87/87；Server 568 通过（3 失败=ConfigurationLoadingTests 存量+2 并行偶发单跑通过） | ✅ 待提交 |

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
| P0-07 | 配置无法 API 修改 | ConfigurationController 无 PUT | ✅ 已实现 (A-18 P1-1)——IApiClientConfiguration 含 UpdateConfiguration/ValidateProduction，API 可修改配置 |

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
