# 凌隐宝堂文档体系深度审查报告

> **审查日期**：2026-06-28
> **审查范围**：`docs/` 全量（6 大类 68 文档）+ 根级（README/AGENTS/DESIGN）+ `docs/compose/`（90+ 工作流产物）+ 多层 `AGENTS.md`（src/Client/Desktop/Shell）
> **审查方法**：6 路并行 subagent 按域深审 → `codegraph`/`read` 对照源码逐条核对 → 关键事实人工复核（端口、health 端点、US 计数）
> **基准原则**：**以代码为唯一真相源**。文档与代码冲突时，代码为准。

---

## 一、执行摘要

文档体系**整体健康度差（D 级）**，已出现系统性失同步。问题不是个别笔误，而是**核心数字（端口/US 数/端点数/版本号）多处与代码事实矛盾**、**整块内容描述了代码中不存在的实体/端点/策略**、**索引与导航大面积缺失**。

最严重的 6 类问题（阻断新人 onboarding、误导开发与排期）：

1. **端口铁律错误** —— 根 `AGENTS.md` 写 `5100=LOCAL`，代码实测 `5300`；同一 LocalWebAPI 在文档树出现 4 个端口号。
2. **API 幽灵端点** —— 整个 Sync 模块 7 端点无对应 Controller；Users/Patients/MedicalCases 共 13 个文档端点代码不存在；"废弃端点表"列的 5 个端点从未存在。端点总数虚报 ~109（实际 ~76）。
3. **权限标注错误（潜在功能 Bug）** —— 挂号文档标 `DoctorOrReceptionist`，代码实际 `DoctorOrAdmin`，导致 Receptionist（前台）无法挂号，与业务矛盾。
4. **架构文档幻影内容** —— `RefreshToken`/`MedicalCasePrintLog` 实体、`LYBTException`/`ForbiddenException` 异常、`DoctorOnly` 策略均**代码中不存在**，却在多份架构文档/ADR 中详细描述。
5. **US 总数三重矛盾** —— 声明 128 与 136 并存，逐文件实数 **138**（10 个 SHELL US 隐身未计数）。
6. **配置三文档与 `appsettings.json` 三方失配** —— JWT Issuer、过期分钟、连接串 key、Session 超时键名全错。

此外，`docs/AGENTS.md` 过期近 2 个月（8 ADR/136 US/文件数全错），`docs/compose/` 90+ 文件零治理索引，Desktop 测试库类型在 4+ 文档系统性误称 "SQLite InMemory"（实际 LocalDB）。

---

## 二、健康度总览

| 域 | 文件数 | 健康度 | 核心问题 |
|----|--------|--------|----------|
| 根级 + 导航 + 治理 | 9+ | **D** | `docs/AGENTS.md` 过期；compose 零治理；文档总数/US 数/ADR 数跨文档矛盾 |
| 01-product + 02-requirements | 16 | **C** | US 总数 128/136/138 三数并存；10 个 SHELL US 隐身；Sync 幽灵模块残留 4 处死链 |
| 03-architecture + ADR + localwebapi | 31 | **D** | 幻影实体/异常/策略；技术栈版本整行错；README 索引缺 7 项；ADR-0008 基于不存在的 RefreshToken |
| 04-api-reference | 14 | **D** | 整块幽灵端点（Sync）+ 13 个跨模块幽灵端点 + 权限标注错误 + 响应信封字段错 |
| 05-development | 26 | **C** | Desktop 测试库系统性误称 SQLite；`01-setup` 依赖/迁移机制过期；安全文档缺 Identity 铁律 |
| 06-operations | 13 | **D** | 端口四数并存；配置三文档与 appsettings 三方失配；sc/schtasks 双命令并存 |
| **总体** | **~110** | **D** | 系统性失同步，需一次集中对账与索引重建 |

---

## 三、🔴 关键问题（必须修复）

### 3.1 端口铁律错误（根 AGENTS.md + 多文档，全局影响）

代码真相（人工复核确认）：
- 嵌入式 LocalWebAPI：`http://localhost:5300`（`src/Client/Desktop/Shell/Services/EmbeddedLocalWebApiService.cs:17` 硬编码常量）
- 配置层一致：`src/Client/Desktop/Shell/appsettings.json:47` → `OfflineMode:LocalApiBaseUrl = "http://localhost:5300"`
- 独立运行模式（仅调试/测试）：`http://127.0.0.1:5290`（`src/Client/Desktop/LocalWebAPI/Program.cs:5`）
- 远程 WebAPI：`http://localhost:5000`（`appsettings.json:3` ApiClient:BaseUrl）

冲突点：
- `AGENTS.md`（根）「连接切换」→ `localhost:5100=LOCAL` ❌（应为 **5300**）
- `docs/06-operations/README.md:11` 部署架构图 → LocalWebAPI `:5000` ❌
- `docs/06-operations/10-variables-secrets.md:44` → `:5100` ❌
- `docs/06-operations/12-deployment-flow.md:9` → 仅提 5000 ❌
- LocalWebAPI 自身 AGENTS.md → 5100 ❌

→ **建议**：以 **5300**（嵌入模式，生产实际值）为唯一真相，全仓统一；根 `AGENTS.md` 的「连接切换」铁律段同步修正；`Program.cs:5` 的 5290 标注为"独立调试端口，嵌入模式不生效"。

### 3.2 API 幽灵端点（整模块 + 跨 3 模块 + 假废弃表）

| 位置 | 幽灵端点 | 实际情况 |
|------|----------|----------|
| `09-sync.md` + `README:160-167` | 整个 Sync 模块 7 端点（entity-types/metadata/compare/upload/download/delete + GET metadata） | **无 SyncController**，全仓 0 命中 |
| `README:118-121` + `02-users.md:673,820,885` | `POST /users/{id}/restore`、`/batch-enable`、`/batch-disable` | `UsersController` 仅 11 action，无此 3 端点（还配了完整示例） |
| `README:137-138` + `03-patients.md` | `import-template`、`export`、`restore`、`check-reference`、`batch-check-reference` | `PatientsController` 实际仅 7 端点 |
| `README:181,186,187,190,191` + `06-medical-cases.md` | `batch-details`、`permissions`、`audit-logs`、`print-completed`、`print-logs` | 两个 MedicalCase Controller 均无 |
| `README:277-287`「废弃端点」表 | 5 个 `[Obsolete]`（with-details/pending/by-patient/recent/unfinished） | **从未存在**，谈不上 Obsolete，整段失实 |

端点总数：README 变更记录 v2.1 声称 `~109 / 14 controllers`，实测公开端点 **~76**（Auth5+Users11+Patients7+Herbs8+Formulas10+MedicalCases15+Reg7+Reports3+Diag4+Config3+Health3+0 Sync），**虚报约 30**。

→ **建议**：删除 `09-sync.md` 或整体标注"未实现/v2.0 规划"；按 `UsersController`/`PatientsController`/`MedicalCasesController` 实际 action 重写三份分文档；删除虚构的"废弃端点表"；README 端点总数改 `~76`。

### 3.3 权限标注错误（潜在功能 Bug，业务阻断）

- **挂号模块**：`07-registrations.md:3` + `README:193` 标 `DoctorOrReceptionist`，但 `RegistrationsController.cs:23` 实际 `[Authorize(Policy=DoctorOrAdmin)]`（策略注册见 `AuthenticationServiceCollectionExtensions.cs:131`）。后果：**Receptionist（前台）无法挂号**，与 US-REG-001「前台模式」直接矛盾。这是文档与代码的冲突，需产品确认孰为准。
- **患者模块**：`03-patients.md:3` 标 `DoctorOrReceptionist`，`PatientsController.cs:23` 实际 `DoctorOrAdmin`（README:123 反而写对了）。

→ **建议**：先与产品确认挂号/患者的目标角色；若代码是 Bug 则修代码，若文档错则改文档。优先级最高（影响功能）。

### 3.4 架构文档幻影内容（4 个基础域）

| 文档 | 幻影内容 | 实际情况 |
|------|----------|----------|
| `04-data-model.md:328-340,346-362` | `RefreshToken`、`MedicalCasePrintLog` 实体字段表 | 两个类均不存在；`AppDbContext` 无 DbSet；迁移 `20260616115938_SimplifyDataModel` 已删 PrintLogs 表 |
| `ADR-0008:42-46` + `09-security-architecture.md:122-128,331-339` | 通篇基于 `RefreshToken.IsReplayAttack`、`TokenManagementService.RefreshTokenAsync`、`FamilyId` | RefreshToken 实体不存在，全章节失实 |
| `13-error-handling-flow.md:5-13` | `LYBTException`/`ForbiddenException(403)`/`ValidationException(422)` | 实际只有 `AppException` 体系，`ValidationException=400`，无 ForbiddenException；与 `06-error-handling.md` 直接冲突 |
| `12-permissions-matrix.md:64-71` + `09-security` | 引用 `DoctorOnly` 策略；漏写 `DoctorOrAdmin`/`AdminOnly` | `PolicyConstants` 实有 4 项，无 `DoctorOnly`；`DoctorOrAdmin` 是代码中最常用的策略却被文档漏掉 |

→ **建议**：以 `AppDbContext`/`PolicyConstants`/`LYBT.Shared.ExceptionHandling` 为基准重写这 4 处；ADR-0008 标注"设计目标，RefreshToken 未实现"或按现状重写。

### 3.5 US 总数三重矛盾 + 10 个隐身 SHELL US

逐文件实数：AUTH13+USER12+PAT13+HERB13+FORM13+MC18+REG7+PRINT4+Platform**45** = **138**。
- 声明 128：`02-requirements/README.md:35,58`、`01-product/01-vision.md:128`、`01-prd.md:78,91`
- 声明 136：根 `README.md:106`、`docs/AGENTS.md`
- 差值根因：① `11-platform.md` 声明"Shell 5 US"，实际定义 SHELL-001/003/004/005/007/**010~019** 共 15 个（010 安装、011 初始化向导、012 自动更新、013 备份恢复、014 审计、015 备份状态、016 配置导入导出、017 生产门控、018 配置中心、019 读卡器诊断）；② `README` 总览(`:195-203`) 只列 5 个 SHELL，漏 10 个。
- 另：`10-sync.md` 文件缺失（目录跳号 09→11），但 `07-medical-cases.md:749`、`02-desktop.md:848,973`、`09-sync.md`(api):457、`01-vision.md:140` 仍把"Sync 8 US"列为 v1.0 模块 → 4 处死链 + 1 处幻影行。

→ **建议**：以 138 为准刷新所有"合计/总览"；将 SHELL-010~019 补入总览或明确标"v1.0 设计未实现"；二选一处理 Sync（补写 10-sync.md 或清除全部引用）。

### 3.6 配置三文档与 appsettings.json 三方失配

`appsettings.json` 真相（已读 `src/Client/Desktop/Shell/appsettings.json`）：
- `Jwt:Issuer = "LYBT.WebAPI"`、无 `ExpiryMinutes`（代码 `AddMinutes(480)`）
- 连接串 key = `ConnectionStrings:DefaultConnection`
- Session 节 = `Session:TimeoutMinutes`（Shell 用 `ClientSession:InactivityTimeoutMinutes=30`）
- `DefaultPasswords:SysAdminPassword/AdminPassword`（明文）

冲突：
- `11-variables-value-ranges.md:8-10` → `Jwt:Issuer=lybtyzys`/`ExpiryMinutes=30` ❌
- `10-variables-secrets.md:7,14,15` → key 名错（列 `ConnectionStrings:LYBTDB` 等）❌
- `02-configuration.md:62` → `ConnectionStrings:LYBTDB` 不存在（实为 `DefaultConnection`）❌
- `11:33` → `SessionOptions:InactivityTimeoutMinutes=30` 与 `Session:TimeoutMinutes=120` 不符
- 10/11 列 `Security:AccountLockout:*` 整节 → appsettings 中无此节
- `appsettings.json:8-12` DefaultPasswords 明文，但 `02-configuration.md:152` 声称"已 REDACTED" ❌

→ **建议**：以 `appsettings.json` + `Program.cs`/`AuthenticationServiceCollectionExtensions` 为基准重写 10/11/02 三份；明文默认密码是设计（开发占位），文档应如实描述而非声称 REDACTED。

### 3.7 Desktop 测试库系统性误称（含 AGENTS.md 体系）

`tests/LYBT.Tests.Desktop` 全部 `UseSqlServer("(localdb)\MSSQLLocalDB...")`，即 **SQL Server LocalDB**。
误称位置（4+ 文档 + AGENTS.md 体系内）：
- `05-development/05-testing.md:29,32,43,146,260,270`
- `05-development/15-migration-strategy.md:59`
- `05-development/standards/STD-05-AAA-Test.md:62`
- `05-development/archive/add-new-module.md:397`
- `src/Client/Desktop/AGENTS.md` + `src/Client/AGENTS.md`（Testing Requirements 段均写"SQLite InMemory"）❌

→ **建议**：全仓把 Desktop 测试的 "SQLite InMemory" 改为 "SQL Server LocalDB"；多层 AGENTS.md 同步。

### 3.8 docs/AGENTS.md 过期（全局导航失真）

`docs/AGENTS.md` 元数据 `Updated: 2026-05-04`，内容大面积过期：
- "8 ADRs / ADR-0001 through ADR-0008" → 实际 **12 条**（0001-0012）
- "136 User Stories" → 实际 **138**
- "03-architecture (25 files)" vs `docs/README.md` "15" vs 实际 31 个 .md
- 未反映 `docs/compose/` 体系（2026-06 才建立，90+ 文件无任何提及）
- "10 modules" → Sync 已删，实际 9

→ **建议**：触发全量重写，与根 AGENTS.md/代码对齐。

### 3.9 AccessToken 有效期三处矛盾 + 01-auth 自相矛盾

- `02-auth.md:7,49` = 2h；`12-nfr.md:156`(NFR-SEC-001) = 30 分钟（changelog:295 称已修正）；代码 `AuthController.cs:98` = `AddMinutes(60)` = 1h。NFR 称"可配置 5-1440"但代码硬编码。
- `01-auth.md:9` 声明"AutoLogin/Refresh 尚未实现"，但 `AuthController.cs:116,128` 的 `refresh`/`auto-login` 端点**已存在**，README:99-101 也已收录。

→ **建议**：以代码 60 分钟为准统一，明确"配置化"为 v2.0 待办；`01-auth.md` 删除"未实现"声明。

---

## 四、🟡 重要问题

### 治理与导航
- **`docs/compose/` 90+ 文件零治理** —— 无 README、无命名规范、无归档策略（`specs/plans/reports/archive` 嵌套）。→ 新建 `docs/compose/README.md`，规约 `YYYY-MM-DD-<slug>-{design|impl}.md` + 归档规则。
- **`docs/plans/` 与 `docs/compose/plans/` 职责重复** —— `docs/plans/README.md` 称"历史规划已清理"但 `archive/` 残留 5 文件，且其"活跃文档"链接指向 `archive/` 子目录文件（死链）。→ `docs/plans/` 整体弃用，重定向到 compose。
- **`docs/03-architecture/README.md:30-41` 文档索引缺 7 项** —— 未列 `00/09/10/11/12/13/implementation-tasks`。
- **`docs/README.md:9` 文件计数全错** —— "03-architecture 15 文件"（实际 31）、"总计 ~68"（未含 compose 90+）。
- **`docs/05-development/README.md:79-94` 导航** —— 缺 `13/14/15` 三个活跃文件，未链 `archive/` 与 standards 单文件。
- **`docs/06-operations/README.md:21-32` 导航** —— 缺 `10/11/12`（2026-06-28 新增）；变更记录止于 2026-02-10。
- **`CONTRIBUTING.md:38`** —— 分支策略写 `main`，与根 `AGENTS.md` `Branch=master` 冲突；`:154-156` 引用不存在的 `ONBOARDING.md`/`DEVELOPER-GUIDE.md`。

### 架构与技术栈
- **`03-architecture/README.md:11-23` 技术栈版本整行错** —— Prism 9.0（实 8.1.97）、BCrypt 4.0.3（实 4.1.0）、FluentValidation 12.0（实 12.1.1）、EF Core 8.0.20（实 8.0.26）。→ 按 `Directory.Packages.props` 全表核对。
- **`05-development/06-security-password-management.md` 缺 Identity 集成铁律** —— 未覆盖根 AGENTS.md 的：`AddIdentity` 必在 `AddAuthentication` 前、UserManager/RoleManager SCOPED 禁 root resolve、BCrypt(PasswordHelper) vs PBKDF2(Identity) 不兼容须全走 UserManager、IdentitySeedData 仅 `LastLoginAt==null` 重置。→ 新增「Identity 集成约束」小节。
- **`02-desktop.md:191`** —— Item 类继承描述"Prism BindableBase"，与 `ADR-0012`「禁止 BindableBase，新代码用 `[ObservableProperty]`」冲突。
- **`05-dual-mode.md:147-159`** —— 模式切换流程图过时，仍画"切换到 Local/启动 LocalWebAPI"双进程式，与 `ADR-0009`「URL 改即生效、无切换动作」矛盾。
- **`04-data-model.md:229-244`** —— User 实体描述为简单 POCO，实际 `ApplicationUser : IdentityUser<Guid>`。
- **`03-server.md:175-186` + `01-system-overview.md:117-126`** —— 模块清单缺 Reports（实际 Server 9 个 Module）。
- **`localwebapi/overview.md:34-42`** —— 控制器数错（说 8 实 11，漏 Reports/Diagnostics/Configuration）。
- **`implementation-tasks.md:19-32`** —— P0 任务 TASK-01/02 代码已修复，清单仍标"发布前必修"。
- **`localwebapi/authentication.md:86-88`** —— 种子用户密码写 `admin`，与 AGENTS.md `Admin@123456` 不符。
- **ADR 间一致性** —— `ADR-0001:49` 变更记录仍写 `SaveAsDraft()`（已重命名 `Suspend()`，MC-D20）；`ADR-0003:17-19` 测试数（Desktop 715）与 `01-system-overview.md:141-143`（760）不一致。

### API 参考
- **响应信封字段错** —— 实际 `ApiResponse` 字段为 `success/message/data/errors/timestamp/requestId`（无 `code`）。`02-users.md` 全文用 `"code":200`、`06-medical-cases.md:11` 写 `"code":0`。→ 统一为 `success`。
- **`README:265` Auth 错误码表过度虚构** —— 列 14 码（UserDisabled/PasswordExpired/TokenRevoked/Session*/ConcurrentSessionLimit…），`AuthController` 实际仅返回 `AuthInvalidCredentials` 与少量字符串。README 已自注"4 个为设计扩展"，但其余多数也无返回路径。
- **`README:193` vs `06-medical-cases.md:3` 策略不一致** —— 医案 README 写 `DoctorOrReceptionist`，分文档写 `DoctorOrAdmin`，代码 `DoctorOrAdmin`（README 错）。
- **`02-users.md:15` 策略名不符** —— 文档写 `AdminOnly`，代码用 `AdminOrSuperAdmin`（行为相同，名称不同）。

### 运维
- **服务管理命令 `sc.exe` vs `schtasks` 并存** —— `09-deployment-rollback.md:48,86,90,106,118`、`07-backup-recovery.md:229,241,290`、`08-monitoring-alerting.md:114` 全用 `sc.exe`，但 `03:85-89` 与 `05:59` 明令 Server 2012 R2 禁用 `sc.exe`（1053 超时），须 `schtasks`。两套并存且未注适用环境，可致生产误操作。
- **`12-deployment-flow.md:16`** —— "发布自包含 exe"与 `03:138-140`/`05:171` 明令禁用自包含冲突。
- **数据库命名漂移** —— `LYBTDB`（`03:30,57`/`04:216`/`07:28`）vs `LYBTDB_Dev`（`02:61`/`10:43`/根 AGENTS.md）。
- **`01-deployment.md` 自相矛盾** —— `:135` `%APPDATA%\LYBT\data\lybt-local.mdf` vs `:207` v1.2 修正 `%LOCALAPPDATA%\LYBTZYZS`。
- **`06-api-tests.md` 归类错误** —— 630 行 Postman 用例属测试，与 `05-development/11-postman-vs-dotnet-testing.md` 职责重叠，应迁出 06-operations。
- **`12-deployment-flow.md:88` 健康表错** —— 列 `/ping`（实际 `/api/v1/health/ping`），`/health/details` 未标需认证；无变更记录段。

### 需求
- **`03-users.md` 无 US 故事块** —— README 总览列 12 个 US-USER 标题，但该文件通篇是 API 端点/权限矩阵，无任何 `### US-USER-NNN` 块，结构与其他模块不一致。
- **本地账户锁定自相矛盾** —— `02-auth.md` AUTH-002 称"本地完全一致"；`03-users.md:29` 称本地"无限失败不锁定"。
- **本地密码策略矛盾** —— `03-users.md:28`=本地"6 位+小写"；`12-nfr.md:166-171`(NFR-SEC-002)=统一"8 位+大小写+数字+特殊字符"。
- **默认密码硬编码** —— `03-users.md:101,126`="Lybt2025@TempPass!"，应以 `appsettings:DefaultPasswords` 为准（`admin/Admin@123456`、`sysadmin/SysAdmin@2026!`）。
- **"10 模块"表述错误** —— `01-prd.md:78-91` 表格跳过 #9，Platform 标 #10；`01-vision.md:128` 称 10 模块却含幽灵 Sync。→ 改"9 模块"并重排号。

---

## 五、🔵 次要问题

- **编号格式** —— `docs/README.md:53` "US-XXX" 不准确（实为 `US-{DOMAIN}-{NNN}`，如 US-MC-017）；"ADR-XXX" 实为 `ADR-NNNN`（四位）。
- **SDK 版本** —— `05-development/README.md:9` + `01-setup.md:17` 称 "8.0.406+"，`global.json` 实为 `8.0.400 + rollForward:latestMinor`。
- **Markdown 渲染破损** —— `05-testing.md:28-35` Desktop 项目结构代码块被 `>` 注记打断。
- **行号漂移** —— `02-auth.md:60` 引 `AuthController.cs:44`，实际 LoginAsync 在 :41；多处实现参考行号轻微漂移。
- **版本号未同步** —— `02-requirements/README.md:3` 标 `v2.1`，`docs/README.md:67` 标 `v2.0`（同日期）。
- **archive 内过期密码** —— `archive/test-scenarios-checklist.md:120` `LybtAdmin2025@SecurePass#`（已归档，低优）。
- **`11-postman-vs-dotnet-testing.md:251`** —— "DTO 映射 (AutoMapper 单元测试)"，与 Mapperly 规范冲突。
- **`archive/add-new-module.md:182-188`** —— Service 构造用 `IMapper`（AutoMapper 风格），应标"已过时"。

---

## 六、修复优先级建议

| 优先级 | 范围 | 理由 |
|--------|------|------|
| **P0（阻断）** | 3.3 权限标注错误（挂号/患者） | 潜在功能 Bug，影响业务正确性，需产品确认 |
| **P0（阻断）** | 3.1 端口铁律 | 全局影响，根 AGENTS.md 自身错误，误导所有连接调试 |
| **P1（严重）** | 3.2 API 幽灵端点 + 3.4 架构幻影内容 + 3.6 配置失配 | 误导开发与排期，阻断新人 onboarding |
| **P1（严重）** | 3.5 US 总数 + 3.7 Desktop 测试库误称 + 3.8 docs/AGENTS.md 过期 | 系统性失真，影响范围估算与协作 |
| **P2（重要）** | 第四章全部 | 索引/治理/版本/安全文档完善 |
| **P3（次要）** | 第五章全部 | 措辞/行号/排版 |

建议执行顺序：**P0 先决策（产品确认权限语义）→ P1 集中对账（以代码为基准批量重写）→ P2 治理重建（compose README + AGENTS.md 刷新）→ P3 清理**。

---

## 七、审查方法与基线修正声明

**方法**：6 路并行 subagent（explore，very thorough）按域分工，各域独立深读全部文件 + 用 `codegraph` 核对代码事实，输出分级问题清单；随后人工对 6 路发现做去重/交叉验证，并对关键冲突点（端口、health 端点、US 计数、幻影实体）逐一复核源码。

**两处基线修正（审查过程中被 subagent 纠正，已人工复核确认）**：
1. **LocalWebAPI 端口** —— 初判采信根 AGENTS.md "5100=LOCAL"；subagent-6 指出代码为 5300；复核 `EmbeddedLocalWebApiService.cs:17` + `appsettings.json:47` 确认 **5300 为真**，根 AGENTS.md 错误。
2. **`/health/database` 端点** —— 初判"不存在"（因 `HealthController` 仅 3 action）；subagent-6 指出存在于中间件层；复核 `UnifiedMiddlewareConfiguration.cs:154`（`MapHealthChecks("/health/database")`）确认 **存在**（仅 Server WebAPI，LocalWebAPI 无）。先前的"基线"仅查了控制器层，漏了 `MapHealthChecks` 终端映射。

**未覆盖项**（供后续审查参考）：
- `docs/training/clinician-training-guide.md` 未深审（培训类，低优先级）。
- `src/Tools/*/README.md` 4 个工具文档仅顺带核对一致性，未逐行审。
- 各文档「变更记录」表的完整性未逐一比对 git 历史。

---

## 八、后续行动

本报告为**审查交付物**，未修改任何文档。如需推进，建议：

1. **立即决策**（P0）：确认挂号/患者的目标角色语义（代码 Bug 还是文档错）。
2. **批量修复**（P1）：以代码为基准，批量重写 API 参考（Users/Patients/MedicalCases/Sync）+ 架构幻影段落 + 配置三文档 + 端口统一 + US 计数刷新 + Desktop 测试库措辞。
3. **治理重建**（P2）：新建 `docs/compose/README.md` 索引；弃用 `docs/plans/`；全量重写 `docs/AGENTS.md`；补齐各 README 导航。

> 是否需要我立即着手修复？建议从 P0（权限语义确认）+ P1 的「端口统一」与「Desktop 测试库措辞」这类高确定性、低风险的批量修正开始。
