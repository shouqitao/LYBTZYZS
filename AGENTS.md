# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

---

## 🤝 协作角色（2026-08-02 确立）

| | 总设计师（AI agent） | 产品负责人（用户） |
|---|---|---|
| **职责** | 架构设计 + 任务派遣 + 监测 + 验收 | 决定业务「要什么」+ 确认方向 |
| **不做** | 亲自写代码（全部委派 Mimo Code） | 回答技术细节 |
| **工作流** | 汇报计划 → 提出问题 → 等待确认 → 调整方向 → 派遣执行 | 提供需求 → 确认/否决/调整 |
| **沟通** | 给「现状 + 专家推荐 + 理由」 | 在推荐上确认/否决/调整 |

问用户只问业务结果（是/否/选项/展开，如「自动更新要不要」）；不问技术题（如「用 Squirrel 还是 AutoUpdater.NET」）。

**身份纪律**: 总设计师必须向产品负责人汇报计划、提出问题、等待确认后再行动。不得跳过确认直接执行。

---

## ⚠️ 工作准则（必须遵守）

**设计前先读代码**（用 MCP），**最小改动**，**声称完成必有 `dotnet build` 证据**。

**修改后自动提交**：所有代码/文档修改完成后，验证通过（`dotnet build` 或相关测试）即自动 `git add` 具体文件并 `git commit`，无需用户另行指示。除非用户明确说「先不要提交」。

### 🔒 项目总账维护（强制规则，不可违反）

`docs/compose/plans/PROJECT-MASTER-PLAN.md` 是项目的**唯一全局视图**。所有 session 必须遵守：

| 时机 | 动作 |
|------|------|
| **Session 启动** | 读 PROJECT-MASTER-PLAN.md 接上进度 |
| **任务完成后** | 立即更新状态表：⬜→✅ + 填写 Commit SHA |
| **任务取消时** | 更新状态为 ❌ + 原因 |
| **新增任务时** | 在对应类别追加，更新总计数和依赖图 |
| **决策变更时** | 在 §九 决策记录追加一行 |

**禁止**：做完任务不更新清单。清单是项目的真相来源，不是装饰文档。

### 工具纪律：MCP 默认首选，积极主动，非"偶尔"

| 场景 | 用 | 禁止/兜底 |
|------|------|------|
| 理解流程/架构/调用链 | `codegraph_explore` | ❌ Read/Grep |
| 读文件/符号 + 调用方 | `codegraph_node`(symbol,includeCode) | ❌ 多次 Read |
| 重命名/重构 | `serena_rename_symbol` | ❌ 手动改 |
| 查引用/受影响面 | `codegraph_callers` / `serena_find_referencing_symbols` | ❌ Grep |
| 编译诊断 | `serena_get_diagnostics_for_file` | ❌ 等 build |
| 替换符号体 | `serena_replace_symbol_body` | — |
| 查框架文档 | `context7_resolve-library-id`→`query-docs` | ❌ 猜/凭记忆 |
| 多文件并行深读/审计 | `actor`(并行 explore) | ❌ 串行 Read |
| 兜底 | Read/Write/Edit/Glob/Grep | 仅当 MCP 不适用 |

- **前置**：首次用 Serena 前 `serena_activate_project(project="LYBTZYZS")`
- **自检**：每次要 Read/Grep 前，先问「MCP 能不能更好更快？」能就用

### 工程流程

| 场景 | 流程 |
|------|------|
| 新功能 | `compose:brainstorm` → `plan`(需用户确认) → 实现 → `verify` → `review` |
| Bug | `compose:debug` 找根因再修，禁止跳到修复 |
| 小修补(1-2行) | 改 → `dotnet build` → 提交 |
| 跨模块大改 | `brainstorm`→`plan`→`plan-eng-review`→`subagent`→`verify`→`review`→`merge` |

**Karpathy**：先思考再编码｜最小代码｜外科手术式修改｜可验证成功标准

---

## 🔧 命令

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (LocalDB)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
# Migration:
dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
# Reset DB:
sqlcmd -S "localhost" -Q "DROP DATABASE IF EXISTS LYBTDB_Dev; CREATE DATABASE LYBTDB_Dev"  # then restart WebAPI
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS LYBTDesktop"               # then restart Desktop
```

**Git**: Remote=Gitee(`gitee.com/shouqitao/LYBTZYZS.git`) NOT GitHub｜Branch=`master`｜Commit 英文 `feat/fix/docs/refactor/test(模块): 描述`

**Database**: Latest migration=`AddRowVersionToAspNetUsers`｜Remote=`LYBTDB_Dev`(SQL Server)｜Local=`LYBTDesktop`(LocalDB, NOT SQLite)｜`DatabaseInitializationService` 用 `MigrateAsync()`（幂等迁移+重试），InMemory 用 `EnsureCreatedAsync`

---

## 🏗️ 架构

- **3-Layer**: Controller→Service→Repository→DbContext
- **MVVM**: View(XAML)←binding→ViewModel→Repository→API
- **DDD**: MedicalCase 唯一聚合根
- **Dual-Mode**: Remote(port 5000)+Local(LocalDB 5300)，URL 路由切换(`SwitchingApiClient`)；WebAPI 有公网部署需求（印证 D3 B+ 安全方案）
- **Modular**: Server(`LYBT.Module.*`)/Desktop(`LYBT.Desktop.*`)/Roles(Admin/Clinical/Receptionist/Sysadmin)
- **sysadmin=独立用户非角色**: `ApplicationUser.IsSysAdmin=true`；启动仅自动创建 `sysadmin/SysAdmin@2026!`（密码从 `appsettings.json:DefaultPasswords`），admin 由 sysadmin 手动创建

**术语**: Consultation=中医诊断｜MedicalCase=医案(非病历)｜Formula=验方｜HerbRole=君臣佐使｜Sysadmin=运维(独立用户)

**Code Style**: 中文业务文档/注释，英文标识符/commit｜PascalCase(公共)/_camelCase(私有)/I前缀(接口)｜包版本统一在 `Directory.Packages.props`｜无注释无 Emoji(除非要求)｜模块间禁止直接引用｜UI 用 MDIX 内置样式 + Spacing Token，不自定义 ControlTemplate

---

## ⚠️ Common Pitfalls（编码前必读）

**Identity/Auth**: `RoleManager`/`UserManager` 是 SCOPED，禁从 root provider resolve｜`IdentitySeedData` 仅 `LastLoginAt==null` 时重置密码｜BCrypt(PasswordHelper) vs PBKDF2(Identity) 不兼容，全走 `UserManager`｜`AddIdentity()` 必在 JWT `AddAuthentication()` 前

**LocalWebAPI**: 路由前缀必 `api/v1/[controller]`｜响应必 `ApiResponse<T>`(bare `Ok()` 致反序列化静默失败)｜用 `WebApplication.CreateBuilder`(非 CreateSlimBuilder)｜`.AddApplicationPart(typeof(HealthController).Assembly)`｜`InitializeDatabaseAsync` 用 `scope.ServiceProvider`(非 app.Services)

**连接切换**: `localhost:5300`=LOCAL, `localhost:5000`=REMOTE｜`SaveAndEnableAsync` 切远程前必 `CheckRemoteAvailableAsync()`｜appsettings 路径用 `AppContext.BaseDirectory`

**Desktop WPF**: `FindAsync` 套全局过滤(`IsDeleted`)，恢复用 `IgnoreQueryFilters()`｜`PrescriptionItem.HerbId` 是 Guid 非 nullable｜Contracts/Api 禁 `using Refit;`(CS0104)，用 `[Refit.Get]`｜BoolToVis 用 `{x:Static converters:Cvt.BoolToVis}`｜XAML pack URI 用 `Source="/Assembly;component/Path.xaml"`

**API/Auth**: 权限 `Receptionist=0,Doctor=1,Admin=10,SuperAdmin=100`｜策略 `DoctorOrReceptionist`+`AdminOrSuperAdmin`｜sysadmin 不可删/禁/改｜`BaseApiController` 构造需 `ILogger`

---

## 📂 WHERE TO LOOK

| Task | Location |
|------|----------|
| WebAPI entry | `src/Server/Services/LYBT.WebAPI/Program.cs` |
| Desktop entry | `src/Client/Desktop/Shell/App.xaml.cs` |
| DbContext | `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` |
| Entities / DTOs | `src/Server/Core/LYBT.Entities/` / `src/Shared/LYBT.Shared.Models/Contracts/` |
| Server Controllers | `src/Server/Services/LYBT.WebAPI/Controllers/` |
| Desktop Modules / Core / Roles | `src/Client/Desktop/{Modules,Core,Roles}/` |
| Seed / Role Definitions | `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` / `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/` |
| Specs / Plans | `docs/compose/{specs,plans}/` |

## Key Patterns

- **ApiResponse&lt;T&gt;** 信封 — 所有 controller 必包
- **CommunityToolkit.Mvvm** `[ObservableProperty]`/`[RelayCommand]` — 非 Prism BindableBase/DelegateCommand
- **Riok.Mapperly** 编译期映射 — 非 AutoMapper
- **ViewModelLocator.AutoWireViewModel** — VM 在 Module.RegisterTypes 注册
- **Soft-delete + 全局查询过滤** 多数实体
- **SwitchingApiClient** 路由 localhost:5300→内嵌 LocalWebAPI，否则→Refit 远程
