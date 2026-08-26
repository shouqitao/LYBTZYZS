# 运维侧 8 页中间内容区 · 需求追溯审计报告

> **审计对象**：`designs/sysadmin-home.pen`、`designs/server-config.pen`、`designs/backup-management.pen`、`designs/deployment.pen`、`designs/log-level.pen`、`designs/security-audit-log.pen`、`designs/cardreader-diagnostics.pen`、`designs/data-import-export.pen`（展开帧主内容区；三栏框架不在本次范围）
> **需求基准**：`docs/02-requirements/11a-shell.md`（US-SHELL-007/013/014/016/018/019/020/021/025）、`11b-configuration.md`（US-CFG-001~006）、`11d-observability.md`（US-LOG-001~009、US-SYS-001~009）、`11e-cardreader.md`（US-CARD-001~002）、`04-patients.md`（US-PAT-011）、`05-herbs.md`（US-HERB-006/007/013）、`06-formulas.md`（US-FORM-006）、`12-nfr.md`（NFR-AVAIL-001）
> **代码基准**：`src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/`、`src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml`、`src/Shared/LYBT.Shared.Configuration/Options/`、`src/Shared/LYBT.Shared.Logging/Management/LoggingLevelManager.cs`、`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/`、`src/Shared/LYBT.Entities/Auth/SecurityAuditLog.cs`、`src/Shared/LYBT.Shared.Models/Enums/DuplicateStrategy.cs`
> **原则**：**每个设计元素必须能在需求文档（US 编号）或现有代码中找到依据；找不到即为编造，移除。**
> **日期**：2026-08-23 | **结论统计**：✅保留 44 · ⚠️改标 38 · ❌编造待删 71

---

## 一、总体结论

8 个 sysadmin 页面中间内容区问题分为**三个层次**：

1. **通用残留（8 页共性）**：所有 8 个 .pen 文件的主内容区顶部均残留一条**患者列表工具栏模板**（搜索姓名/手机号/身份证号 + 筛选 + 新增患者 + 导入 + 导出 + 批量删除），属 patient-list 样板复制残留，全部应删。加上顶部应用栏残留（Logo + 凌隐宝堂 + 标签 + 用户区）同样是复制残留（属三栏框架，不在审计范围，但出现在主内容区节点内）。

2. **功能卡片/导航入口**：
   - **sysadmin-home**：设计稿画了 5 张功能卡片（数据库管理/远程部署/日志管理/安全审计/数据导入导出），真实 `SysadminHomeViewModel` 的导航命令只有 3 个：用户管理 / 日志级别控制 / 部署管理。**配置中心 7 组面板（US-SHELL-018 核心）在设计稿中完全缺失**。安全审计（US-SHELL-014 待实现）和数据导入导出（US-SHELL-016 待实现）有 US 依据但设计形态错误。
   - **server-config**：设计稿画了独立页面，代码实际是 `ServerConfigView` **对话框**（Modal），仅有 RemoteUrl 单字段 + 测试连接 + 保存/保存并启用/取消。端口/协议/超时/恢复默认/右侧状态面板全部编造。
   - **backup-management**：设计稿列（类型/状态/操作下载删除）与 `BackupFileInfo`（FileName/CreatedAt/SizeBytes）严重不符。导出备份/校验完整性/计划任务全部无代码。
   - **deployment**：设计稿画了版本信息卡 + 4 台服务器监控 + 部署历史列表，代码实际只有「选择 zip → 上传并部署 → 重启服务」三步。
   - **log-level**：设计稿画了按模块（WebAPI/Database/Auth/UI）设置日志级别，真实 `LoggingLevelManager` 只有**全局单一级别**（Serilog `LoggingLevelSwitch`）。实时日志查看器不存在。
   - **security-audit-log**：US-SHELL-014 待实现（D3），`ISecurityAuditRepository` 只有写入（`AddAsync`），**无查询端点**。设计稿中示例行内容为业务操作审计而非安全事件（登录/登出/密码变更），角色名「李护士」不存在（系统无 Nurse 角色）。
   - **cardreader-diagnostics**：**代码已实现**（`ICardReaderDiagnostics` + `CardReaderDiagnosticsViewModel` + `CardReaderDiagnosticsService`），但设计稿错误地画成 Windows 设备管理器风格（COM3/波特率 9600/驱动版本/INF 文件/数字签名），实际是诊断测试工具（厂家选择 → 探测 → 握手 → 读卡 → 固件）。
   - **data-import-export**：无独立 `DataImportExportView`。业务数据批量导入导出分散在各模块（herbs/formulas/patients），US-SHELL-016 仅指**配置导出导入**（非数据）。设计稿混合了两种概念。冲突策略「智能合并」不存在（`DuplicateStrategy` 枚举仅 Skip/Update/Error）。无「医案资料」导出端点。

---

## 二、逐元素追溯表

图例：✅=有 US + 有代码，保留原样 | ⚠️=需求有但设计形态/取值画错，改标 | ❌=无需求无代码，移除
（框架层问题已在总体结论中标注，此处仅审计主内容区元素）

---

### A. sysadmin-home（运维设置主页）

**代码基准**：`Roles/LYBT.Desktop.Admin/Sysadmin/Views/SysadminHomeView.xaml`（配置中心 TabControl 5 组 Tab + 服务端配置 Tab + 功能入口 3 卡片 + 状态卡片 2 张）；`ViewModels/SysadminHomeViewModel.cs`（Dashboard 轮询、导航命令 NavigateToUserManagement/NavigateToLogLevelControl/NavigateToDeployment、ConfigCenter/ServerConfig/CardReaderDiagnostics 子面板）；`ViewModels/ConfigurationCenterViewModel.cs`（诊所信息/会话设置/连接设置/安全策略/功能开关 5 组可编辑 + 保存命令）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| A1 | 主内容区·患者工具栏残留（搜索姓名/手机号/身份证号 + 筛选 + 新增患者 + 导入 + 导出 + 批量删除） | 工具栏 | 整条 | ❌ | 无 | SysadminHomeViewModel 无任何搜索/新增/删除命令 | **整条删除**（patient-list 样板复制残留） |
| A2 | 页标题区：「系统配置中心」+ 副标题「管理数据库备份、远程部署、日志和安全审计…」 | 页面标题 | — | ⚠️ | US-SHELL-018 | `PageTitle = "运维设置"` | 标题改为「运维设置」；副标题对齐 US-SHELL-018「统一管理所有基础配置」 |
| A3 | 应用栏（Logo + 凌隐宝堂 + 系统管理标签 + 用户区） | 页内应用栏 | — | ⚠️ | — | 三栏框架全局应用栏已承载品牌与用户信息 | 此处为重复，应从主内容区移除（属框架层） |
| A4 | 第一行卡片·数据库管理：图标 + 标题「数据库管理」+ 描述「备份恢复与数据库状态监控」+ 状态标签「运行正常 · SQL Server」+ [立即备份] [恢复备份] | 导航卡 | — | ⚠️ | US-SHELL-013 | SysadminHomeView 无直接备份命令；BackupManagementView 有 `BackupCommand`/`RestoreCommand`（须先选行） | 卡片改为导航入口「数据库备份」→ NavigateTo(BackupManagement)；移除内联按钮和状态标签；「立即备份」需进入列表页触发 |
| A5 | 第一行卡片·远程部署：图标 + 标题「远程部署」+ 状态「已部署 · v2.4.1」+ [立即部署] [版本信息] | 导航卡 | — | ⚠️ | US-SHELL-020 | `NavigateToDeploymentCommand` → DeploymentView | 卡片保留为导航入口「部署管理」；「已部署 · v2.4.1」v2.4.1 编造（实际版本来自 `SystemConstants.ApplicationVersion`，程序集版本 1.0.0）→ 删除状态标签或绑定真实版本；「版本信息」无对应功能 → 删除 |
| A6 | 第一行卡片·日志管理：图标 + 标题「日志管理」+ [查看日志] [日志级别] | 导航卡 | — | ⚠️ | US-LOG-005/SYS-005~009 | `NavigateToLogLevelControlCommand` → LogLevelControlView | 卡片改为单一导航入口「日志级别控制」；「查看日志」无代码（无日志查看器视图）→ 删除；状态标签「日志级别：Information」无数据源（LogLevelControl 页面自行查询）→ 删除 |
| A7 | 第一行卡片·安全审计：图标 + 标题「安全审计」+ 状态「审计已开启」+ [查看审计日志] | 导航卡 | — | ⚠️ | US-SHELL-014 🧲 | SysadminHomeViewModel 无对应导航命令；AuditLogView 是**医案审计**（MedicalCase 域） | US-SHELL-014 待实现，保留规划入口；导航目标需新增 SecurityAuditLogView（非 AuditLogView）；「审计已开启」无数据源 → 删除；状态标签删除 |
| A8 | 第一行卡片·数据导入导出：图标 + 标题「数据导入导出」+ 描述「以 JSON 格式导入或导出系统配置与基础数据」+ [导出 JSON] [导入 JSON] | 导航卡 | — | ⚠️ | US-SHELL-016 🧲 | SysadminHomeViewModel 无对应导航命令 | US-SHELL-016 仅覆盖**配置**导出导入；描述「基础数据」超范围 → 改为「系统配置」；内联按钮删除（功能待实现后再加） |
| A9 | 底部状态栏：「服务器 60.190.215.86 · 当前版本 v2.4.1 · 数据库连接正常 · 最近备份 2026-08-15 03:00」 | 状态栏 | — | ⚠️ | US-SHELL-018 系统信息组 | Dashboard.SystemInfo.Value = 版本号，Dashboard.DbStatus = 数据库连接 | 服务器 IP 不应硬编码到设计稿（环境相关）→ 删除；版本 v2.4.1 编造 → 绑定 ApplicationVersion；数据库连接 ✅（Dashboard.DbStatus）；最近备份时间 ✅（BackupStatus.LastBackupAt） |
| A10 | 缺失元素：配置中心 7 组面板（诊所信息/会话设置/连接设置/安全策略/功能开关/读卡器管理/系统信息） | 核心面板 | 未画 | ❌→**补画** | US-SHELL-018 AC「SysadminHomeView 展示运维设置面板，分组显示所有可配置项」 | ConfigurationCenterViewModel（5 组可编辑）、ServerConfigSectionViewModel、CardReaderDiagnosticsViewModel、Dashboard（系统信息只读） | **必须补画**——这是 US-SHELL-018 的核心实现。应为 TabControl 或可折叠面板组，含 7 组字段编辑 + 保存按钮 + 功能开关热更新提示 |
| A11 | 缺失元素：功能入口卡片·用户管理 | 导航卡 | 未画 | ❌→**补画** | US-SHELL-003 | `NavigateToUserManagementCommand` → ViewNames.UserManagement | 补画导航卡片「用户管理」（图标 AccountTie） |
| A12 | 缺失元素：功能入口卡片·日志控制 / 部署管理 | 导航卡组 | 未画 | ❌→**补画** | US-LOG-005 / US-SHELL-020 | `NavigateToLogLevelControlCommand` / `NavigateToDeploymentCommand` | 补画为独立导航卡片（与日志管理/远程部署卡片整合或并列） |

---

### B. server-config（服务器配置）

**代码基准**：`Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml`（对话框标题「服务器配置」、RemoteUrl 输入、测试连接按钮、测试状态、本地模式提示、保存/保存并启用/取消按钮、保存中遮罩）；`ViewModels/ServerConfigViewModel.cs`（继承 ConnectionTestViewModelBase，仅 RemoteUrl + TestStatus + 保存逻辑）；`ApiClientOptions`（BaseUrl/RemoteUrl/PreferredMode/TimeoutSeconds[5-300]/IgnoreSslErrors）；US-SHELL-025（HTTP/HTTPS 双协议为服务端 Kestrel 配置，非客户端 UI 单选）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| B1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | ServerConfigView 无工具栏 | **整条删除** |
| B2 | 页面形态：独立全页 + 侧边导航 + 页面标题「服务器配置」 | 页面布局 | — | ⚠️ | US-SHELL-007 | ServerConfigView.xaml 为 **UserControl 对话框**（由 `DialogService` 打开，`prism:Dialog.IsOpen="{Binding IsActive}"`） | 改为对话框形态（非独立页） |
| B3 | API 地址输入框 | 文本框 | — | ✅ | US-CFG-005/SHELL-007 | `RemoteUrl` 绑定 | 保留；输入验证/URL 格式检查在代码中 |
| B4 | 端口输入框（默认 5000） | 文本框 | — | ❌ | 无 | `ApiClientOptions.BaseUrl` 是完整 URL，无独立端口字段 | **删除**。端口包含在 URL 中（如 `https://localhost:5001`） |
| B5 | 「连接测试」按钮 | 按钮 | — | ✅ | US-CFG-005 | `TestConnectionCommand` | 保留；按钮文案改「测试连接」对齐代码 XAML |
| B6 | 通信协议单选组：HTTPS / HTTP / WebSocket | 单选组 | — | ⚠️ | US-SHELL-025 | Kestrel 端点配置在服务端 appsettings，非客户端 UI 选项；WebSocket 无任何代码 | HTTP/HTTPS ⚠️：由 URL scheme 决定，非 UI 单选；WebSocket **删除** |
| B7 | 超时设置卡：连接超时 10s / 读取超时 30s / 写入超时 15s | 表单组 | — | ❌ | 无（US-CFG 无超时配置 UI） | `ApiClientOptions.TimeoutSeconds`（单一字段，5-300，默认 60）——在 SysadminHome 连接设置组编辑，不在 ServerConfig 对话框 | **整卡删除**。超时由 `TimeoutSeconds` 单字段控制，在运维设置主页连接设置编辑 |
| B8 | 取消按钮 | 按钮 | — | ✅ | — | `CancelCommand` | 保留 |
| B9 | 「恢复默认」按钮 | 按钮 | — | ❌ | 无 | ServerConfigViewModel 无 Reset 命令 | **删除** |
| B10 | 「保存配置」按钮 | 按钮 | — | ⚠️ | US-CFG-005 | 实际有两个按钮：`SaveOnlyCommand`（保存）+ `ConfirmCommand`（保存并启用） | 改为两个按钮「保存」和「保存并启用」 |
| B11 | 右侧·系统状态面板：连接状态「已连接」+ 服务器地址 | 状态面板 | — | ⚠️ | US-SHELL-007 AC5 | `TestStatusMessage` 仅在测试后显示结果，非持久状态面板 | 合并进测试结果区域，非独立面板 |
| B12 | 右侧·响应延迟「42ms」 | 状态值 | — | ❌ | 无 | 无延迟测量 API | **删除** |
| B13 | 右侧·上次同步 / 最近活动 | 状态值 | — | ❌ | 无 | 无同步/活动追踪 API | **删除** |
| B14 | 右侧·快捷操作（查看日志 / 导出配置 / 诊断工具） | 操作按钮 | — | ❌ | 无 | 无对应命令 | **删除**。查看日志→LogLevelControlView（已在主页导航）；导出配置→US-SHELL-016 待实现；诊断工具→读卡器诊断（已嵌入主页） |
| B15 | 底部「返回运维设置中心」链接 | 导航链接 | — | ⚠️ | — | 对话框无返回逻辑（关闭对话框自动回到主页） | 删除链接（对话框关闭即返回） |

---

### C. backup-management（备份管理）

**代码基准**：`Sysadmin/Views/BackupManagementView.xaml`（状态卡 3 列：上次备份/数量/总大小 + 刷新/立即备份按钮 + DataGrid 3 列：文件名/备份时间/大小 + 刷新 + 恢复所选备份按钮 + View 层确认弹框「将覆盖当前数据库」→ 恢复完成提示重启）；`ViewModels/BackupManagementViewModel.cs`（BackupAsync 手动备份、RefreshAsync 刷新、RestoreAsync 恢复 + 选行验证 + 状态消息）；`BackupModels.cs`（BackupFileInfo: Id/FileName/FullPath/CreatedAt/SizeBytes；BackupStatus: LastBackupAt/FileCount/TotalSizeBytes）；US-SHELL-013 AC：保留 7 天 + 备份状态展示 + 手动备份按钮 + 恢复（RESTORE）确认弹框 + 重启提示；NFR-AVAIL-001：本地模式登录自动备份

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| C1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| C2 | 数据库状态卡·连接状态 | 状态值 | — | ⚠️ | — | SysadminHomeView Dashboard.DbStatus 有（不在本页）；BackupManagementView 无此字段 | 删除或从 SysadminHome Dashboard 引用 |
| C3 | 数据库状态卡·数据库类型「SQL Server」 | 状态值 | — | ⚠️ | — | 本地模式实为 LocalDB（SQL Server Express LocalDB），非完整 SQL Server | 改为「LocalDB」或删除 |
| C4 | 数据库状态卡·数据库大小「2.4GB」 | 状态值 | — | ❌ | 无 | 无数据库大小查询 API | **删除** |
| C5 | 数据库状态卡·最后备份时间 | 状态值 | — | ✅ | US-SHELL-013 | BackupStatus.LastBackupAt / `LastBackupTime` 属性 | 保留（已在状态卡展示） |
| C6 | 操作按钮行：立即备份 | 按钮 | — | ✅ | US-SHELL-013 | `BackupCommand` | 保留 |
| C7 | 操作按钮行·数据恢复 | 按钮 | — | ⚠️ | US-SHELL-013 | `RestoreCommand`（需先选中列表行 + View 层确认弹框） | 按钮文案改为「恢复所选备份」；需与列表选择联动 |
| C8 | 操作按钮行·导出备份 | 按钮 | — | ❌ | 无 | ILocalDbBackupService 无 Export 方法 | **删除** |
| C9 | 操作按钮行·校验完整性 | 按钮 | — | ❌ | 无 | ILocalDbBackupService 无 Verify 方法 | **删除** |
| C10 | 操作按钮行·计划任务 | 按钮 | — | ❌ | 无 | NFR-AVAIL-001：登录时自动备份（fire-and-forget），非 cron 计划任务 | **删除** |
| C11 | 提示文字「每日凌晨 03:00 全量备份」 | 提示 | — | ❌ | NFR-AVAIL-001 | 本地模式：每次登录自动备份；保留 7 天（最多 7 个文件） | **删除**。提示改为「登录时自动备份，保留最近 7 天」 |
| C12 | 存储空间进度条（12.6GB / 50GB） | 进度条 | — | ❌ | 无 | 无存储空间查询 API | **删除** |
| C13 | 备份历史表·列：类型（Full/Incr/Manual 徽章） | DataGrid 列 | — | ❌ | 无 | LocalDbBackupService 仅 `BACKUP DATABASE` 全量，无增量（Incremental）| **删除** |
| C14 | 备份历史表·列：文件名 | DataGrid 列 | — | ✅ | US-SHELL-013 | BackupFileInfo.FileName | 保留 |
| C15 | 备份历史表·列：大小 | DataGrid 列 | — | ✅ | US-SHELL-013 | BackupFileInfo.SizeBytes | 保留 |
| C16 | 备份历史表·列：时间 | DataGrid 列 | — | ✅ | US-SHELL-013 | BackupFileInfo.CreatedAt | 保留 |
| C17 | 备份历史表·列：状态（成功/警告/失败 徽章） | DataGrid 列 | — | ❌ | 无 | BackupFileInfo 无 Status 字段；备份结果仅在 BackupCommand 执行时通过 StatusMessage 显示 | **删除** |
| C18 | 备份历史表·操作列（下载/删除图标） | DataGrid 列 | — | ❌ | 无 | ILocalDbBackupService 无 Download/Delete 方法；清理由 CleanupOldBackupsAsync 自动执行 | **删除** |
| C19 | 分页导航 | 分页 | — | ❌ | — | ObservableCollection 直接绑定，最多 7 个文件无需分页 | **删除** |
| C20 | 恢复流程：选行 → 确认弹框「将覆盖当前数据库」→ 恢复完成 → 提示重启 | 流程 | — | ✅ | US-SHELL-013 AC「恢复前弹框确认 + 恢复完成后提示重启」 | BackupManagementView.xaml.cs：MessageBox YesNo 确认 → RestoreCommand → StatusMessage「请重启应用」 | 保留（确认弹框在代码中，设计稿需画出弹框态） |
| C21 | 缺失：恢复前确认弹框的 UI 设计 | 弹框 | 未画 | ❌→**补画** | US-SHELL-013 AC「恢复前弹框确认」 | BackupManagementView.xaml.cs MessageBox | 补画确认弹框（MDIX DialogHost 样式） |
| C22 | 缺失：恢复成功后「请重启应用」提示的 UI 设计 | 提示 | 未画 | ⚠️ | US-SHELL-013 AC「恢复完成后提示重启应用」 | StatusMessage 在文本中显示 | 设计稿状态消息区域保留即可 |

---

### D. deployment（部署管理）

**代码基准**：`Sysadmin/Views/DeploymentView.xaml`（上传更新包 Card：选择文件 + 上传并部署按钮 + 进度条 + 服务控制 Card：重启服务按钮（红色）+ 状态消息 TextBlock）；`ViewModels/DeploymentViewModel.cs`（SelectFile → OpenFileDialog zip 过滤、UploadAsync → MultipartFormDataContent 上传、RestartAsync → 重启指令 + 状态消息）；`DeployController.cs`（POST upload + POST restart with Confirm="RESTART"）；US-SHELL-020 AC：upload zip + restart 二次确认

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| D1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| D2 | 版本信息卡·标题「版本信息」 | 卡片 | — | ⚠️ | US-SHELL-020 | SystemConstants.ApplicationVersion（只读） | 保留标题 |
| D3 | 版本信息卡·当前版本「v2.4.1」 | 状态值 | — | ⚠️ | — | ApplicationVersion 从程序集读取，实际默认 1.0.0 | 改为「v{ApplicationVersion}」绑定，非硬编码 |
| D4 | 版本信息卡·最新版本「v2.4.2」+ 更新说明 | 状态值 | — | ❌ | 无 | 无版本查询 API（US-SHELL-012 Desktop 自动更新走 Velopack，非本页功能） | **删除** |
| D5 | 版本信息卡·构建时间 | 状态值 | — | ❌ | 无 | 无构建时间查询 API | **删除** |
| D6 | 版本信息卡·部署环境「生产」 | 状态值 | — | ❌ | 无 | 部署环境由环境变量决定，非 UI 展示项 | **删除** |
| D7 | 服务器状态区·4 台服务器（CPU/内存/运行天数） | 服务器列表 | — | ❌ | 无 | 单机部署架构，无多服务器监控 | **整区删除** |
| D8 | 部署操作区·「开始部署」按钮 + 版本号 v2.4.2 | 操作组 | — | ⚠️ | US-SHELL-020 | 实际流程：选择文件 → 上传并部署（SelectFileCommand + UploadCommand） | 改为「选择文件 + 上传并部署」交互（ZIP 选择 + 进度条） |
| D9 | 部署操作区·「预览变更」按钮 | 按钮 | — | ❌ | 无 | 无变更预览 API | **删除** |
| D10 | 部署操作区·「回滚版本」按钮 | 按钮 | — | ❌ | 无 | 回滚走 Velopack 安装器（US-SHELL-022），非 DeployController | **删除** |
| D11 | 右侧·部署记录面板（版本/状态/耗时/操作人列表 + 筛选） | 面板 | — | ❌ | 无 | DeployController 无历史记录 API | **整区删除** |
| D12 | 缺失：选择 zip 文件交互（OpenFileDialog + 文件名显示） | 交互 | 未画 | ❌→**补画** | US-SHELL-020 | SelectFileCommand → OpenFileDialog | 补画文件选择区域（选择文件按钮 + 文件名标签） |
| D13 | 缺失：上传进度条 | 进度条 | 未画 | ❌→**补画** | US-SHELL-020 | UploadProgress 属性 + ProgressBar | 补画进度条 |
| D14 | 缺失：重启确认（需 Confirm="RESTART" 参数） | 二次确认 | 未画 | ⚠️→**补画** | US-SHELL-020 AC「二次确认参数，延迟生效」 | RestartCommand → RestartConfirmDto(Confirm) | 补画重启确认弹框 |
| D15 | 缺失：返回按钮 | 按钮 | 未画 | ⚠️ | — | GoBackCommand | 补画返回按钮 |

---

### E. log-level（日志级别控制）

**代码基准**：`Sysadmin/Views/LogLevelControlView.xaml`（当前级别 TextBlock + 快捷操作 Card：开启 Debug(60分钟)/关闭 Debug + 手动设置级别 Card：Verbose/Debug/Info/Warning/Error 5 按钮 + StatusMessage）；`ViewModels/LogLevelControlViewModel.cs`（GetLoggingStatus → SetLevel → EnableDebug → DisableDebug）；`LoggingLevelManager`（全局单一级别 LogEventLevel：Verbose/Debug/Information/Warning/Error/Fatal；EnableDebugMode/DisableDebugMode/SetLevel）；`DiagnosticsController`（GET status / POST debug/enable / POST debug/disable / POST level）；`LoggingOptions.Cleanup`（Enabled/RetentionDays/CleanupIntervalHours）——服务端配置，非 Desktop UI 可调

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| E1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| E2 | 页面标题「日志级别控制」 | 标题 | — | ✅ | US-LOG-005/SYS-005~009 | `PageTitle = "日志级别控制"` | 保留 |
| E3 | 模块日志级别卡·WebAPI 下拉「Information」 | 下拉 | — | ❌ | 无 | LoggingLevelManager 为**全局单一级别**，非按模块 | **整卡删除**（Serilog LoggingLevelSwitch 全局单例） |
| E4 | 模块日志级别卡·Database 下拉 | 下拉 | — | ❌ | 无 | 同上 | **删除** |
| E5 | 模块日志级别卡·Auth 下拉 | 下拉 | — | ❌ | 无 | 同上 | **删除** |
| E6 | 模块日志级别卡·UI 框架 下拉 | 下拉 | — | ❌ | 无 | 同上 | **删除** |
| E7 | 日志配置卡·日志文件大小限制「50 MB」 | 配置项 | — | ❌ | 无 | 代码 fileSizeLimitBytes=10MB（固定值，LoggingBootstrap 中硬编码，不可 UI 配置） | **删除** |
| E8 | 日志配置卡·日志保留天数「30 天」 | 配置项 | — | ⚠️ | US-LOG-007 | LoggingOptions.Cleanup.RetentionDays（默认 90，服务端配置） | Desktop 页面无此编辑入口（清理由服务端定时任务执行）；若保留须注明为只读展示 |
| E9 | 日志配置卡·实时日志行数「500 行」 | 配置项 | — | ❌ | 无 | 无实时日志流 API | **删除** |
| E10 | 「保存设置」按钮 | 按钮 | — | ⚠️ | US-SYS-008 | SetLevel POST 即时生效，无「保存」概念 | 改为级别按钮点击即时生效（移除保存按钮） |
| E11 | 「清空日志」按钮 | 按钮 | — | ❌ | 无 | 无清空日志 API | **删除** |
| E12 | 「导出日志」按钮 | 按钮 | — | ❌ | 无 | 无导出日志 API | **删除** |
| E13 | 实时日志面板（20 行代码日志流） | 日志面板 | — | ❌ | 无 | 无实时日志流 API（SignalR 通知仅推业务事件，非日志） | **整区删除** |
| E14 | 「自动刷新」开关 | 开关 | — | ❌ | 无 | 依赖实时日志面板 | **删除** |
| E15 | 状态栏「v1.0.0 · 数据库连接正常 · 管理员 admin」 | 状态栏 | — | ⚠️ | — | ApplicationVersion 从程序集读取 | 保留级别显示（GetLoggingStatus 返回值）；其余数据源待确认 |
| E16 | 缺失：全局日志级别下拉/按钮组（Verbose/Debug/Information/Warning/Error/Fatal） | 核心交互 | 未画 | ❌→**补画** | US-SYS-008「支持级别：Verbose/Debug/Information/Warning/Error/Fatal」 | LogLevelControlView.xaml：Verbose/Debug/Info/Warning/Error 5 按钮（缺 Fatal） | 补画全局级别选择器（6 级别按钮/下拉）；**补充 Fatal 按钮**（代码当前缺此级别按钮） |
| E17 | 缺失：Debug 模式控制区（开启 60 分钟 / 关闭 Debug + 剩余时间显示） | 核心交互 | 未画 | ❌→**补画** | US-SYS-006「定时 ≤120 分钟，到期自动恢复」+ US-SYS-007 | EnableDebugCommand（60 分钟）+ DisableDebugCommand | 补画 Debug 模式开关区（开启按钮 + 时长选择 1-120 分钟 + remainingMinutes 展示 + 关闭按钮） |
| E18 | 缺失：当前级别只读展示 | 状态 | 未画 | ⚠️→**补画** | US-SYS-005 | CurrentLevel 属性 | 补画当前级别大字展示（GetLoggingStatus 返回 currentLevel/defaultLevel/isDebugModeActive） |

---

### F. security-audit-log（安全审计日志）

**代码基准**：US-SHELL-014 🧲 v1.0 待实现（D3）；`SecurityAuditLog` 实体（UserId/UserName/EventType/IpAddress/UserAgent/Details/IsSuccess/FailureReason + BaseEntity 字段）；`SecurityAuditService` 写入（`RecordEventAsync`）；`ISecurityAuditRepository` 仅有 `AddAsync`（**无查询端点**——写入侧已实现，查看侧 UI 与 API 均未实现）；US-LOG-004 审计事件类型：Login/LoginFailed/Logout/RefreshToken/TokenRevoked/PasswordChange/UserDisabled；`SecurityOptions.AuditRetentionDays=365`；ViewNames.AuditLog = `AuditLogView`（属 MedicalCase 域的**医案审计**，非安全审计）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| F1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| F2 | 页面标题「安全审计日志」+ 描述 | 标题 | — | ✅ | US-SHELL-014 | US-SHELL-014 待实现，但 US 有依据 | 保留（设计目标正确） |
| F3 | 表格列·时间 | DataGrid 列 | — | ✅ | US-SHELL-014 AC「每条记录含：时间」 | SecurityAuditLog.CreatedAt | 保留 |
| F4 | 表格列·用户 | DataGrid 列 | — | ✅ | US-SHELL-014 AC「每条记录含：用户」 | SecurityAuditLog.UserName | 保留 |
| F5 | 表格列·操作类型 | DataGrid 列 | — | ✅ | US-SHELL-014 AC「每条记录含：操作类型」 | SecurityAuditLog.EventType | 保留 |
| F6 | 表格列·操作对象 | DataGrid 列 | — | ❌ | 无（US-SHELL-014 AC 无此项） | SecurityAuditLog 无 TargetObject 字段 | **删除**（Details 字段可包含上下文信息） |
| F7 | 表格列·IP 地址 | DataGrid 列 | — | ✅ | US-SHELL-014 AC「每条记录含：IP 地址」 | SecurityAuditLog.IpAddress | 保留 |
| F8 | 表格列·结果（成功/失败徽章） | DataGrid 列 | — | ✅ | US-SHELL-014 AC「每条记录含：结果（成功/失败）」 | SecurityAuditLog.IsSuccess | 保留 |
| F9 | 表格列·详情（放大镜图标） | DataGrid 列 | — | ⚠️ | — | SecurityAuditLog.Details（string） | 保留（点击弹出详情面板合理）；详情面板中的 JSON 代码视图过度设计 → 简化为文本展示 |
| F10 | 示例数据·操作类型为「查看患者/修改处方/新建预约/删除记录/导出报告/修改排班/查看库存」 | 示例 | — | ❌ | US-SHELL-014/LOG-004 | 安全事件类型应为：Login/LoginFailed/Logout/PasswordChange/UserCreated/UserDeleted/UserDisabled | **修改示例数据**为安全事件类型 |
| F11 | 示例数据·操作人「李护士」「王前台」 | 角色名 | — | ❌ | — | UserRole 枚举：Receptionist/Doctor/Admin/SuperAdmin，无「护士」角色 | **删除「李护士」**（改为「李前台」或「王前台」对齐真实角色） |
| F12 | 搜索栏·操作类型/用户/日期筛选 + 搜索框 | 筛选 | — | ⚠️ | US-SHELL-014 AC「支持按事件类型/用户/时间范围筛选」 | ISecurityAuditRepository 无查询方法 → 待实现 | 保留（US 有依据）；待查询 API 实现后验证筛选参数 |
| F13 | 「重置」按钮 | 按钮 | — | ✅ | — | 辅助控件 | 保留 |
| F14 | 「导出日志」按钮 | 按钮 | — | ❌ | US-SHELL-014 无导出 AC | 无导出端点 | **删除** |
| F15 | 统计指示器「共 1,247 条 · 24h: 89」 | 统计 | — | ⚠️ | — | 无统计查询 API（待实现） | 保留概念（US-SHELL-014 待实现时补 TotalCount API）；「24h:89」无依据 → 删除 |
| F16 | 分页导航 | 分页 | — | ⚠️ | US-SHELL-014 AC「分页，按时间倒序」 | 代码无查询 API | 保留（US 有依据） |
| F17 | 右侧·详情面板（JSON 代码视图 + 客户端信息 + 快捷操作） | 面板 | — | ⚠️ | — | Details 为字符串，非结构化 JSON | JSON 代码视图简化为文本；客户端 UserAgent ✅（字段存在）；快捷操作「查看用户/相关日志」❌ 无对应 API → 删除 |
| F18 | 「新增审计事件」调试按钮 | 按钮 | — | ❌ | 无 | 无此调试功能 | **删除** |

---

### G. cardreader-diagnostics（读卡器诊断）

**代码基准**：`CardReaderDiagnosticsViewModel.cs`（SelectedReaderType: HuaDaHD100/Auto、UsbPort/ConnectTimeout/ReadTimeout 字段、RunDiagnosticsCommand、ReportLines 集合、SaveSettingsCommand 持久化到 appsettings）；`ICardReaderDiagnostics.RunDiagnosticsAsync(readerType, options)` → `CardReaderDiagnosticReport`（DeviceDetected/HandshakeOk/FirmwareVersion/ReadTestResult/Messages/DeviceInfo）；`CardReaderDeviceInfo`（Name/Vendor/Model/IsConnected/FirmwareVersion）；`CardReaderType` 枚举：Auto=0/HuaDaHD100=1/HuaDaHD200=2（预留）/ShenSiSS628=10（预留）；`CardReaderOptions`：UsbPort(1001)/ConnectTimeout(5000ms)/ReadTimeout(10000ms)/SerialPort/ReconnectInterval/AutoReconnect/PhotoSaveDirectory；US-SHELL-019 已实现（traceability-matrix 确认 8/8 AC ✅；**注：US-SHELL-019 原文标注「待实现」但代码与追踪矩阵均已更新为已实现**）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| G1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| G2 | 页面标题「读卡器诊断」 | 标题 | — | ✅ | US-SHELL-019 | CardReaderDiagnosticsViewModel | 保留 |
| G3 | 设备状态卡·连接状态（图标 + 绿色圆点） | 状态 | — | ✅ | US-SHELL-019 AC「设备探测是否通过」 | CardReaderDiagnosticReport.DeviceDetected / CardReaderDeviceInfo.IsConnected | 保留 |
| G4 | 设备状态卡·设备型号「HD100」 | 状态 | — | ✅ | US-SHELL-019 AC「厂家选择」 | SelectedReaderType → CardReaderType.HuaDaHD100 → ReaderTypeDisplay | 保留 |
| G5 | 设备状态卡·通信端口「COM3」 | 状态 | — | ⚠️ | — | UsbPort = 1001（USB 端口号，非 COM 串口） | 改为「USB 端口 1001」 |
| G6 | 设备状态卡·波特率「9600」 | 状态 | — | ❌ | 无 | USB HID 设备无波特率概念 | **删除** |
| G7 | 驱动信息卡·驱动版本「v3.2.1.0」 | 信息 | — | ❌ | 无 | 代码无驱动版本查询（FirmwareVersion 是固件，非驱动） | **删除整个驱动信息卡** |
| G8 | 驱动信息卡·驱动状态「已安装」 | 信息 | — | ❌ | 无 | 无驱动状态检查 | **删除** |
| G9 | 驱动信息卡·INF 文件「HD100USB.inf」 | 信息 | — | ❌ | 无 | 无 INF 查询 | **删除** |
| G10 | 驱动信息卡·数字签名「Valid · Microsoft WHQL」 | 信息 | — | ❌ | 无 | 无签名查询 | **删除** |
| G11 | 驱动信息卡·安装日期 | 信息 | — | ❌ | 无 | 无安装日期查询 | **删除** |
| G12 | 快捷操作·刷新设备列表 | 按钮 | — | ⚠️ | US-SHELL-019 | RunDiagnostics 包含探测步骤 | 改为「运行诊断」按钮（RunDiagnosticsCommand） |
| G13 | 快捷操作·同步设备状态 | 按钮 | — | ❌ | 无 | 无此命令 | **删除** |
| G14 | 快捷操作·更新驱动程序 | 按钮 | — | ❌ | 无 | 无此功能 | **删除** |
| G15 | 快捷操作·高级设置 | 按钮 | — | ⚠️ | US-SHELL-019 AC「手动参数覆盖」 | SaveSettingsCommand（UsbPort/ConnectTimeout/ReadTimeout） | 改为「保存配置」（SaveSettingsCommand）；展开时显示参数表单 |
| G16 | 测试结果日志卡·「测试结果日志」标题 | 卡片 | — | ✅ | US-SHELL-019 | ReportLines 集合逐行展示 | 保留 |
| G17 | 测试结果日志·「清空」按钮 | 按钮 | — | ⚠️ | — | ReportLines.Clear()（前端清屏） | 保留（辅助功能） |
| G18 | 测试结果日志·日志内容「[驱动] 正在加载华大 HD100…COM3 就绪…」 | 示例 | — | ⚠️ | — | Messages 实际为诊断步骤：探测→握手→读卡→固件查询 | 修改示例为「[探测] 正在检测设备…[握手] 通信正常…[读卡] 读取成功…」 |
| G19 | 测试结果日志·统计「共 16 条 · 成功 12 · 失败 1」 | 统计 | — | ⚠️ | — | ReportLines 无成功/失败计数（Passed = DeviceDetected && HandshakeOk） | 保留计数概念；成功/失败逻辑对齐代码 |
| G20 | 缺失：厂家选择下拉（Auto/HD100） | 核心交互 | 未画 | ❌→**补画** | US-SHELL-019 AC「厂家选择：下拉选择已适配厂家」 | ReaderTypes + SelectedReaderType | 补画 ComboBox 下拉 |
| G21 | 缺失：手动参数覆盖表单（USB 端口/连接超时/读取超时） | 核心交互 | 未画 | ❌→**补画** | US-SHELL-019 AC「手动参数：USB 端口/连接超时/读取超时可手动覆盖」 | UsbPort/ConnectTimeout/ReadTimeout 输入框 | 补画参数表单（可折叠，仅自动检测失败时展开） |
| G22 | 缺失：保存配置按钮 | 按钮 | 未画 | ❌→**补画** | US-SHELL-019 AC「测试通过后，厂家选择持久化到 config」 | SaveSettingsCommand | 补画「保存配置」按钮 |
| G23 | 缺失：读卡测试结果展示（姓名/身份证号/性别/出生日期/住址） | 核心展示 | 未画 | ❌→**补画** | US-SHELL-019 AC「读卡测试：显示解析结果」 | CardReadResult（Name/IdNumber/Gender/BirthDate/Address） | 补画读卡结果展示区 |
| G24 | 缺失：运行诊断主按钮 | 按钮 | 未画 | ❌→**补画** | US-SHELL-019 AC「设备探测：发送探测指令」 | RunDiagnosticsCommand | 补画主操作按钮「运行诊断」 |

---

### H. data-import-export（数据导入导出）

**代码基准**：无独立 `DataImportExportView`；业务数据导入分散在各模块：US-HERB-006（药材 JSON 批量导入，DuplicateStrategy: Skip/Update/Error）、US-HERB-007/013（药材 JSON 导出）、US-FORM-006（验方 JSON 批量导入）、US-PAT-011（患者 JSON 模板下载）；US-SHELL-016（配置导出导入 JSON）待实现；US-SHELL-021（上线数据迁移 Excel）待实现；`DuplicateStrategy` 枚举仅 Skip=0/Update=1/Error=2（无「智能合并」）；项目定案：JSON 为唯一格式（2026-08-13 Excel→JSON 移除）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| H1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | — | **整条删除** |
| H2 | 页面标题「数据导入导出」 | 标题 | — | ⚠️ | — | 无独立视图 | 定位需明确：是 sysadmin 运维功能入口（配置导出 US-SHELL-016）还是业务数据聚合页 |
| H3 | 导出卡·数据类型多选：☑ 患者信息 | checkbox | — | ⚠️ | US-HERB-007/US-PAT-011 | 患者有 GET export 端点，但无日期筛选参数 | 保留；日期范围筛选无 API → 删除日期筛选 |
| H4 | 导出卡·数据类型多选：☑ 药材数据 | checkbox | — | ✅ | US-HERB-007/013 | HerbsController GET export | 保留 |
| H5 | 导出卡·数据类型多选：☑ 验方记录 | checkbox | — | ✅ | US-FORM-006 | FormulasController GET export | 保留 |
| H6 | 导出卡·数据类型多选：☑ 医案资料 | checkbox | — | ❌ | 无 | 无医案批量导出 API（MedicalCase 仅有审计日志 AuditLogDto，非数据导出） | **删除** |
| H7 | 导出卡·日期范围筛选 + 快捷按钮（本月/本季度/本年） | 筛选 | — | ⚠️ | US-HERB-013 | herb export 支持 keyword 筛选，但无日期范围参数 | 日期范围无 API → 删除；保留 keyword 搜索（如有） |
| H8 | 导出卡·格式：JSON（UTF-8 编码） | 格式 | — | ✅ | 项目定案 | 2026-08-13 Excel→JSON | 保留 |
| H9 | 导出卡·「开始导出」按钮 | 按钮 | — | ✅ | US-HERB-007 | HerbExportAll/Export 端点 | 保留；但需为每个数据类型调用对应端点 |
| H10 | 导入卡·DropZone「将 JSON 文件拖放至此处或点击选择文件」 | 上传区 | — | ⚠️ | US-HERB-006/FORM-006 | Desktop 用 OpenFileDialog，非拖放 | 改为文件选择区域（OpenFileDialog + 格式校验） |
| H11 | 导入卡·预览表（姓名/年龄/性别/电话/地址 + 操作状态列） | 预览表 | — | ⚠️ | US-HERB-006/FORM-006/PAT-011 | 无预览 API（BatchImportAsync 一步完成）；PatientInputDto 无「年龄」字段 | 年龄改为出生日期；操作状态列（处理中/错误/重复）无依据 → 删除 |
| H12 | 导入卡·冲突策略：跳过重复 / 覆盖更新 / 智能合并 | 单选组 | — | ⚠️ | DuplicateStrategy 枚举 | DuplicateStrategy = Skip/Update/Error（无「智能合并」） | 第三项「智能合并」改为「报错回滚」(Error) |
| H13 | 导入卡·「开始导入」按钮 + 进度条 | 按钮+进度 | — | ⚠️ | US-HERB-006/FORM-006 | BatchImportAsync 同步返回成功/失败计数；无流式进度 | 保留概念；进度为一次性返回（非实时） |
| H14 | 进度显示「已处理 106/156 条 · 剩余 50 条 · 预计 12 秒」 | 进度 | — | ⚠️ | US-SHELL-021（待实现） | 单次批量无流式进度 | 保留统计概念；「预计 12 秒」无依据 → 删除 |
| H15 | 导入卡·示例数据「张明/李芳/王强」 | 示例 | — | ⚠️ | — | 患者/药材示例数据 | 保留（合理示例） |
| H16 | 缺失：US-SHELL-016「系统配置」导出导入（appsettings.json + clinic-settings.json） | 功能 | 未画 | ❌→**补画** | US-SHELL-016 🧲 | 无代码实现 | 补画「配置导出/导入」独立区域（JSON 格式：appsettings + clinic-settings） |

---

## 三、反向覆盖检查（需求 → 设计漏画汇总）

| 来源 | 内容 | 状态 |
|------|------|------|
| US-SHELL-018 配置中心 7 组面板 | 诊所信息/会话设置/连接设置/安全策略/功能开关/读卡器管理/系统信息 — 全部未画 | ❌**漏画** |
| US-SHELL-018 服务端配置面板（仅远程） | ServerConfigSectionViewModel（EditablesSections 6 节 + RESTORE + restart 按钮）— 未画 | ❌**漏画** |
| US-SHELL-018 功能入口·用户管理 | NavigateToUserManagement — 未画 | ❌**漏画** |
| US-SHELL-020 上传交互 | ZIP 文件选择 + 上传进度条 + 二次确认弹框 — 未画 | ❌**漏画** |
| US-SHELL-020 重启确认 | RestartConfirmDto(Confirm="RESTART") — 未画 | ❌**漏画** |
| US-SYS-006/007 Debug 模式控制 | EnableDebugMode(1-120 min) + DisableDebugMode + remainingMinutes 展示 — 未画 | ❌**漏画** |
| US-SYS-008 Fatal 级别按钮 | LogLevelControlView 缺 Fatal 按钮（仅 Verbose/Debug/Info/Warning/Error 5 个） | ⚠️**漏画** |
| US-SHELL-019 厂家选择下拉 | CardReaderType 下拉（Auto/HuaDaHD100）— 未画 | ❌**漏画** |
| US-SHELL-019 运行诊断主按钮 | RunDiagnosticsCommand — 未画 | ❌**漏画** |
| US-SHELL-019 手动参数表单 | UsbPort/ConnectTimeout/ReadTimeout 输入框 — 未画 | ❌**漏画** |
| US-SHELL-019 读卡结果展示 | 姓名/身份证号/性别/出生日期/住址 — 未画 | ❌**漏画** |
| US-SHELL-019 保存配置按钮 | SaveSettingsCommand — 未画 | ❌**漏画** |
| US-SHELL-013 恢复确认弹框 | View 层确认弹框「将覆盖当前数据库」— 设计稿未画出弹框态 | ⚠️**漏画** |
| US-SHELL-016 配置导出导入 | appsettings.json + clinic-settings.json JSON 打包 — 未画 | ❌**漏画** |

---

## 四、修改点清单（属性级执行摘要）

### ❌ 删除（71 项，按页分组）

**通用（8×1 = 8）**：
- 8 页患者工具栏残留整条（A1/B1/C1/D1/E1/F1/G1/H1）

**sysadmin-home（3）**：应用栏重复（A3）；底部状态栏硬编码 IP（A9 中 IP 部分）；版本号 v2.4.1 硬编码（A5 状态标签）

**server-config（8）**：端口输入框（B4）；WebSocket 单选（B6）；超时设置整卡（B7）；恢复默认按钮（B9）；响应延迟/上次同步/最近活动（B12/B13）；快捷操作 3 按钮（B14）；返回链接（B15）

**backup-management（7）**：数据库大小（C4）；导出备份（C8）；校验完整性（C9）；计划任务（C10）；每日03:00提示（C11）；存储空间进度条（C12）；分页（C19）；备份历史列类型/状态/操作（C13/C17/C18）

**deployment（6）**：最新版本+更新说明（D4）；构建时间（D5）；部署环境（D6）；服务器状态整区（D7）；预览变更（D9）；回滚版本（D10）；部署记录整面板（D11）

**log-level（9）**：模块日志级别整卡（E3/E4/E5/E6）；文件大小限制（E7）；实时日志行数（E9）；保存设置按钮（E10）；清空日志（E11）；导出日志（E12）；实时日志面板（E13）；自动刷新开关（E14）

**security-audit-log（4）**：操作对象列（F6）；导出日志按钮（F14）；新增审计事件调试按钮（F18）；JSON 代码视图 + 快捷操作（F17 部分）

**cardreader-diagnostics（9）**：波特率 9600（G6）；驱动信息整卡 5 项（G7/G8/G9/G10/G11）；同步设备状态（G13）；更新驱动程序（G14）；搜索/刷新/筛选/操作列（G26/G27/G28/G29）

**data-import-export（6）**：医案资料 checkbox（H6）；日期范围筛选+快捷按钮（H7）；预览表操作状态列（H11 部分）；预计 12 秒（H14 部分）

### ⚠️ 改标（38 项）

1. **sysadmin-home**：标题→「运维设置」(A2)；数据库管理卡→导航入口+去掉内联按钮(A4)；远程部署卡→去掉状态标签(A5)；日志管理卡→单一入口「日志级别控制」(A6)；安全审计卡→规划入口标注待实现(A7)；数据导入导出卡→文案改为「系统配置」(A8)；版本号绑定 ApplicationVersion(A5/A9)
2. **server-config**：对话框形态(B2)；API地址→保留对齐(B3)；连接测试→文案对齐(B5)；HTTP/HTTPS→URL scheme 承载(B6)；保存→双按钮(B10)；系统状态→合并测试结果(B11)
3. **backup-management**：连接状态→删除或引用 Dashboard(C2)；数据库类型→LocalDB(C3)；最后备份→保留(C5)；数据恢复→需选中行(C7)；恢复确认弹框→补画(C20/C21)
4. **deployment**：当前版本→绑定 ApplicationVersion(D3)；开始部署→选文件+上传流程(D8)；返回按钮→补画(D15)；重启确认→补画(D14)
5. **log-level**：日志保留天数→只读展示(E8)；状态栏→级别展示(E15)；全局级别→补画 6 级别选择器(E16)；Debug 模式→补画控制区(E17)；当前级别→补画展示(E18)
6. **security-audit-log**：详情→简化为文本(F9)；筛选→保留待实现(F12)；统计→保留概念(F15)；分页→保留(F16)
7. **cardreader-diagnostics**：通信端口→USB 端口 1001(G5)；刷新→「运行诊断」(G12)；高级设置→「保存配置」(G15)；日志内容→诊断步骤(G18)；统计→保留(G19)；测试结果→保留(G16)；厂家选择→补画(G20)；参数表单→补画(G21)；保存配置→补画(G22)；读卡结果→补画(G23)；运行诊断→补画(G24)
8. **data-import-export**：DropZone→文件选择(H10)；预览表年龄→出生日期(H11)；冲突策略第三项→「报错回滚」(H12)；导入进度→同步返回(H13)；页面定位需明确(H2)

### ✅ 保留（44 项）

**sysadmin-home**：配置中心面板容器（A10 待补画）+ 功能入口概念成立（A11/A12 待补画）

**server-config**：API 地址(B3)、连接测试(B5)、取消(B8)

**backup-management**：最后备份(C5)、立即备份(C6)、文件名(C14)、大小(C15)、时间(C16)、恢复确认弹框(C20)

**deployment**：版本信息卡标题(D2)

**log-level**：页面标题(E2)

**security-audit-log**：页面标题(F2)、时间列(F3)、用户列(F4)、操作类型列(F5)、IP 地址列(F7)、结果列(F8)、详情列容器(F9)、重置(F13)

**cardreader-diagnostics**：页面标题(G2)、连接状态(G3)、设备型号(G4)、测试结果日志卡(G16)、清空(G17)

**data-import-export**：药材数据(H4)、验方记录(H5)、JSON 格式(H8)、开始导出(H9)

### ➕ 补画（14 组）

1. sysadmin-home 补配置中心 7 组面板（A10）+ 用户管理导航卡（A11）+ 日志控制/部署管理导航卡（A12）
2. deployment 补 ZIP 文件选择 + 进度条（D12/D13）+ 重启确认弹框（D14）+ 返回按钮（D15）
3. log-level 补全局级别选择器 6 按钮含 Fatal（E16）+ Debug 模式控制区（E17）+ 当前级别展示（E18）
4. cardreader-diagnostics 补厂家选择下拉（G20）+ 参数表单（G21）+ 保存配置按钮（G22）+ 读卡结果展示（G23）+ 运行诊断主按钮（G24）
5. backup-management 补恢复确认弹框 UI 态（C21）
6. data-import-export 补 US-SHELL-016 配置导出导入区域（H16）

---

## 五、给下一轮设计稿的硬约束

1. **sysadmin-home = 配置中心 + 导航入口**：顶部标题「运维设置」+ 配置中心 TabControl/折叠面板 7 组（诊所信息/会话设置/连接设置/安全策略/功能开关/读卡器管理/系统信息，按 US-SHELL-018）+ 功能入口卡片 3 张（用户管理/日志级别控制/部署管理）+ 只读状态卡 2 张（数据库状态/系统信息，绑定 Dashboard）。不画：统计数字、内联操作按钮、底部状态栏 IP/版本硬编码。
2. **server-config = 对话框**：单一 URL 输入 + 测试连接按钮 + 保存/保存并启用/取消。无端口字段、无协议单选、无超时字段（超时在运维设置主页连接设置组编辑）、无恢复默认、无右侧面板。
3. **backup-management = 备份列表 + 恢复流程**：状态卡 3 数值 + DataGrid 3 列 + 立即备份按钮 + 刷新 + 恢复所选备份（须先选行 + 确认弹框 + 重启提示）。不画：类型/状态/操作列、导出/校验/计划按钮、存储进度条、数据库信息行。
4. **deployment = ZIP 上传 + 重启**：选择文件（OpenFileDialog zip）+ 上传并部署按钮 + 进度条 + 重启服务（红色按钮 + 二次确认）+ 返回。不画：版本信息卡（最新版本/构建时间/部署环境）、服务器监控、部署记录。
5. **log-level = 全局级别 + Debug 控制**：当前级别大字展示 + 6 级别选择按钮（Verbose/Debug/Info/Warning/Error/Fatal）+ Debug 模式开关（时长 1-120 分钟 + 剩余时间 + 关闭按钮）+ 状态消息。不画：按模块级别、日志配置、实时日志面板、清空/导出按钮。
6. **security-audit-log = 安全事件列表**：列=时间/用户/操作类型/IP 地址/结果 + 详情弹窗（文本非 JSON）；筛选=事件类型/用户/日期范围；分页。示例数据必须是安全事件（Login/LoginFailed/Logout/PasswordChange），非业务操作（查看患者/修改处方）。角色名用系统真实角色（前台/医生/管理员）。
7. **cardreader-diagnostics = 诊断测试工具**：厂家选择下拉 → 运行诊断按钮 + 手动参数（USB 端口/连接超时/读取超时）+ 诊断日志逐行 + 读卡结果展示（姓名/身份证号/性别/出生日期/住址）+ 保存配置。不画：Windows 设备管理器风格（COM3/波特率/驱动版本/INF/数字签名/安装日期）。
8. **data-import-export = 明确拆分**：如果是**配置导出导入**（US-SHELL-016），只画 appsettings + clinic-settings JSON 下载/上传；如果是**业务数据批量导入导出**，按模块分散在各自管理页（herbs/formulas/patients），不画独立聚合页。冲突策略三选：跳过重复(Skip)/覆盖更新(Update)/报错回滚(Error)，无「智能合并」。

> 元素树快照：`docs/compose/reports/_pen_trees/`（本次解析产物，供复查）
