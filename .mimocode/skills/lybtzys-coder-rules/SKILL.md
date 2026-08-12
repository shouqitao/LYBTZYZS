---
name: lybtzys-coder-rules
description: Use when 在 LYBTZYZS（凌隐宝堂中医诊所管理系统）仓库执行任何编码任务 — .NET 8 / WPF / ASP.NET Core / EF Core / SQL Server 代码修改、权限或 API 变更、重构、修 bug、加功能。本 skill 是 LYBTZYZS 项目编码守则（SSOT）：工作准则、架构约束、Common Pitfalls、命令速查、文档导航。
---

# LYBTZYZS 项目编码守则（Mimo Code 版）

凌隐宝堂中医诊所管理系统的开发约束。**SSOT（唯一真相来源）**：本文件与 Hermes 侧 `lybtzys-coder-rules` skill 同源，若与项目 docs/ 冲突，以 docs/ 为准（docs 是设计态，代码是当前态）。

## 项目速览

- **技术栈**：.NET 8 | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)
- **架构**：3-Layer (Controller→Service→Repository→DbContext)；Desktop MVVM (View←binding→ViewModel→Repository→API)；DDD (MedicalCase 唯一聚合根)
- **模块**：Server(`src/Server/Modules/LYBT.Module.*`)/Desktop(`src/Client/Desktop/...`)/Roles(Admin/Clinical/Receptionist/Sysadmin)
- **sysadmin=独立用户非角色**：`ApplicationUser.IsSysAdmin=true`；启动自动创建 sysadmin，admin 由 sysadmin 手动创建
- **Git**：Remote=Gitee(`gitee.com/shouqitao/LYBTZYZS.git`) 非 GitHub｜Branch=`master`｜Commit 英文 `feat/fix/docs/refactor/test(模块): 描述`

## 工作准则（强制）

- **文档地位（第一原则，2026-08-12 深化）**：文档是系统的**设计态权威**（系统应该是什么），代码是**当前态实现**（系统现在是什么）。文档是 SSOT：同一信息点只有一个权威定义。**文档优先于代码**——代码与文档冲突时，先改文档、再按文档改代码；交付任何变更必须同步受影响文档，禁止「代码已改、文档滞后」。文档是用户手册/运维手册的素材来源，是团队与 AI 协作的共同语言——文档的质量就是系统可维护性的上限
- **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental` 必须 0 错误 **且 0 警告**；存量警告一并修复，不允许带警告交付。增量构建会跳过已编译项目导致警告漏检，验证必须用 `--no-incremental`
- **声称完成必有证据**：交付前跑完整 build + 相关测试，报告真实输出
- **修改后自动提交并推送**：验证通过后 `git add` 具体文件 → `git commit`（英文，`refactor(scope): 描述` 格式）→ `git push origin master`。除非明确说先不提交
- **先文档后代码**：文档定义设计态，代码实现当前态。文档与代码冲突时：先更新文档 → 再按文档改代码，禁止跳过文档直接改代码。开始任务前先查 `docs/README.md#ai-查询指南` 定位权威文档
- **代码-文档一致性（2026-08-12 用户确立，红线）**：代码/配置/路由/安全头等任何变更，**交付时必须同步受影响文档**（部署 Runbook、发布踩坑清单、需求状态列、架构文档）。禁止「代码已改、文档滞后」。文档是用户手册/运维手册的素材来源，滞后即失职。交付前 grep 变更关键词确认文档已同步
- **发布标准流程（2026-08-12 用户确立）**：发布前先查 `docs/06-operations/01-deployment.md`（Runbook+踩坑清单）→ 文档与实际不符先修文档 → 无问题询问用户需求（一次一问逐步明确）→ 逐步执行 + 实时维护文档。文档是发布的地图，发布是文档的校验器
- **禁止兼容层/中间过渡状态**：发现错误的设计/实现，直接重写为正确版本，不做「新旧都支持」的兼容层
- **Surgical Changes（外科手术式修改）**：只改任务要求的代码，不「顺手」改相邻代码/注释/格式；每行改动可追溯到任务需求
- **设计定案后同步架构测试清单**：设计决策变化时，先同步 `tests/LYBT.Tests.Architecture/` 新增测试覆盖新规则，再改实现。测试清单不齐，代码不开写
- **总账维护**：任务完成后立即更新 `docs/03-architecture/13-project-master-plan.md` 状态表 ⬜→✅ + Commit SHA（若任务是总账中的编号任务）

## 架构约束

- 3-Layer: Controller→Service→Repository→DbContext
- MVVM: View(XAML)←binding→ViewModel→Repository→API
- DDD: MedicalCase 唯一聚合根
- Dual-Mode: Remote(port 5000)+Local(LocalDB 5300)，URL 路由切换(`SwitchingApiClient`)；WebAPI 有公网部署需求
- 模块化: Server(`LYBT.Module.*`)/Desktop(`LYBT.Desktop.*`)/Roles(Admin/Clinical/Receptionist/Sysadmin)
- sysadmin=独立用户非角色: `ApplicationUser.IsSysAdmin=true`；启动仅自动创建 sysadmin，admin 由 sysadmin 手动创建
- **P07**: 模块间禁止直接引用
- **P08**: 跨模块必须用接口
- **P10**: Service 禁注入 AppDbContext（仅 Repository/Base 可）

## Code Style

- 中文业务文档/注释，英文标识符/commit
- PascalCase(公共)/_camelCase(私有)/I前缀(接口)
- 包版本统一 `Directory.Packages.props`
- 模块间禁止直接引用
- UI 用 MDIX 内置样式 + Spacing Token，不自定义 ControlTemplate

## Common Pitfalls（编码前必读）

### Identity/Auth
- `RoleManager`/`UserManager` 是 SCOPED，禁从 root provider resolve
- `IdentitySeedData` 仅 `LastLoginAt==null` 时重置密码
- BCrypt(PasswordHelper) vs PBKDF2(Identity) 不兼容，全走 `UserManager`
- `AddIdentity()` 必在 JWT `AddAuthentication()` 前

### LocalWebAPI
- 路由前缀必 `api/v1/[controller]`
- 响应必 `ApiResponse<T>`（bare `Ok()` 致反序列化静默失败）
- 用 `WebApplication.CreateBuilder`（非 CreateSlimBuilder）
- `.AddApplicationPart(typeof(HealthController).Assembly)`
- `InitializeDatabaseAsync` 用 `scope.ServiceProvider`（非 app.Services）

### 连接切换
- `localhost:5300`=LOCAL, `localhost:5000`=REMOTE
- `SaveAndEnableAsync` 切远程前必 `CheckRemoteAvailableAsync()`
- appsettings 路径用 `AppContext.BaseDirectory`

### Desktop WPF
- `FindAsync` 套全局过滤(`IsDeleted`)，恢复用 `IgnoreQueryFilters()`
- `PrescriptionItem.HerbId` 是 Guid 非 nullable
- Contracts/Api 禁 `using Refit;`(CS0104)，用 `[Refit.Get]`
- BoolToVis 用 `{x:Static converters:Cvt.BoolToVis}`
- XAML pack URI 用 `Source="/Assembly;component/Path.xaml"`

### API/Auth
- 权限 `Receptionist=0,Doctor=1,Admin=10,SuperAdmin=100`
- 策略 `DoctorOrReceptionist`+`AdminOrSuperAdmin`+`DoctorOrAdminOrReceptionist`
- sysadmin 不可删/禁/改
- `BaseApiController` 构造需 `ILogger`
- **委托扩展方法的实例方法禁写 `this.Method()`**：`BaseApiController` 的 8 个实例方法（Success×2/SuccessPaged/Error/BusinessFail/ValidationFail/HandleResult×2）与 `ControllerBaseExtensions` 扩展方法同名，`this.Success(...)` 解析到实例方法自身 → 无限递归 Stack Overflow。必须写 `ControllerBaseExtensions.Success(this, ...)` 显式调用

### Dual-mode 双控制器树（重要）
- Remote Server controllers（`src/Server/Services/LYBT.WebAPI/Controllers/`）和 Desktop LocalWebAPI controllers（`src/Client/Desktop/LocalWebAPI/Controllers/`）镜像彼此的路由和 `[Authorize]` 策略
- **任何权限/端点变更必须同时改两棵控制器树**，验收时 grep 两处确认，否则出现 dual-mode 权限分叉
- Local controllers 继承 `BaseCrudController` virtual 方法时，类级 `[Authorize]` 不限制继承来的 action，需要显式方法级覆盖

### EF Core 迁移
- 多 DbContext 时必须传 `--context <FullTypeName>`（`dotnet ef dbcontext list` 枚举）
- 迁移链冲突：迁移 A 建表 → 迁移 B 删表 → 迁移 C ALTER 同表会失败，检查 `DROP TABLE`+`ALTER TABLE` 同名
- 重复 `IEntityTypeConfiguration<T>`：`ApplyConfigurationsFromAssembly` 按类名字母序应用，后注册的 `ToTable()` 覆盖先前的——同一个实体出现两处配置会静默错表
- `IdentityUser<T>` 默认映射 `AspNetUsers` 表，若合并到业务表需加 `builder.ToTable("目标表")`
- 中断任务可能生成两个重复迁移文件（同名类）→ CS0579/CS0111，删除一个再继续

## Key Patterns

- **ApiResponse<T>** 信封 — 所有 controller 必包
- **CommunityToolkit.Mvvm** `[ObservableProperty]`/`[RelayCommand]`
- **Riok.Mapperly** 编译期映射（非 AutoMapper）
- **ViewModelLocator.AutoWireViewModel** — VM 在 Module.RegisterTypes 注册
- **Soft-delete + 全局查询过滤** 资源类实体（User/Herb/Formula/Patient）用 `IsDeleted`；禁用用 `CommonStatus`
- **SwitchingApiClient** 路由 localhost:5300→LocalWebAPI，否则→Refit 远程

## 数据管理规则

- **两字段模式**：`CommonStatus Status`（禁用）+ `IsDeleted`（归档），语义不同不可合并
- 资源类实体需两字段：User, Herb, Formula, Patient
- 流程类/从属类/审计类不需要 CommonStatus：MedicalCase, Registration, Consultation, Prescription, AuditLog
- 不执行物理删除，所有数据通过标记位管理生命周期
- 角色不可变更（避免权限追溯问题）

## 命令速查

```bash
dotnet build LYBTZYZS.sln --no-incremental   # 0 错误 0 警告门禁
dotnet test tests/LYBT.Tests.Server/          # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/         # Desktop (LocalDB)
dotnet test tests/LYBT.Tests.Architecture/    # Architecture guards (P07/P08/P10...)
# Migration:
dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
# Reset DB:
sqlcmd -S "localhost" -Q "DROP DATABASE IF EXISTS LYBTDB_Dev; CREATE DATABASE LYBTDB_Dev"            # then restart WebAPI
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS LYBTDesktop"                          # then restart Desktop
```

## 文档导航（信息点 → 权威文档）

| 信息点 | 权威文档（SSOT） |
|--------|-----------------|
| 术语定义 | `docs/01-product/03-glossary.md` |
| 权限矩阵（产品规则） | `docs/01-product/04-permissions.md` |
| 业务规则/US 需求 | `docs/02-requirements/`（按模块） |
| 数据模型（实体/状态） | `docs/03-architecture/04-data-model.md` |
| API 端点契约 | `docs/04-api-reference/`（按模块） |
| 任务清单/进度/决策 | `docs/03-architecture/13-project-master-plan.md` |
| 当前状态/已知问题 | `docs/03-architecture/13c-current-status.md` |
| 架构决策 (ADR) | `docs/03-architecture/decisions/` |
| 技术栈/架构总览 | `docs/03-architecture/00-architecture-summary.md` |
| 命名规范/文档规则 | `docs/00-governance/01-naming-convention.md` + `02-ssot-architecture.md` |

## WHERE TO LOOK

| 任务 | 位置 |
|------|------|
| 文档导航 | `docs/README.md#ai-查询指南` |
| WebAPI 入口 | `src/Server/Services/LYBT.WebAPI/Program.cs` |
| Desktop 入口 | `src/Client/Desktop/Shell/App.xaml.cs` |
| DbContext | `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` |
| 实体/DTO | `src/Shared/LYBT.Entities/` / `src/Shared/LYBT.Shared.Models/Contracts/` |
| Controller | `src/Server/Services/LYBT.WebAPI/Controllers/` |
| Desktop 模块 | `src/Client/Desktop/{Modules,Core,Roles}/` |
| Seed / Role 定义 | `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` / `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/` |

## Verification（交付前必做）

1. `dotnet build LYBTZYZS.sln --no-incremental` — 0 错误 **0 警告**
2. `dotnet test tests/LYBT.Tests.Architecture/` — 架构测试全绿
3. 涉及权限/端点：grep 双控制器树（Server + LocalWebAPI）确认策略一致
4. 涉及迁移：检查无重复迁移文件
5. 更新 `docs/03-architecture/13-project-master-plan.md` 状态（如适用）
6. `git commit`（英文）+ `git push origin master`

## Test Triage（测试失败甄别）

测试失败时，先分类再决定是否修复：

### 分类准则

| 测试类型 | 判断依据 | 失败原因 | 处理 |
|----------|----------|----------|------|
| **Remote E2E** | `WebApiE2ETestBase` 系（UserTests/AuthenticationIntegrationTests/Modules+Foundation 集成） | 本地未启动远程 WebAPI（localhost:5000） | 非回归，报告注明 |
| **LocalWebAPI 焦点** | `LocalWebApiControllerTestBase` 系（AuthControllerTests） | 基座缺 MediatR 注册→全 500 | 先查 HEAD 基线 |
| **架构测试** | `LYBT.Tests.Architecture` | 新规则未加测试覆盖 | 必须修复 |
| **单元测试** | `tests/LYBT.Tests.Desktop/Unit/` | 代码变更引入 | 必须修复 |

### HEAD 基线验证法

当怀疑测试失败是存量问题时：
```bash
git stash  # 暂存当前改动
dotnet test tests/LYBT.Tests.Desktop/ --filter "AuthControllerTests|LocalWebAPI|UserTests"
git stash pop  # 恢复改动
```
若 HEAD 基线同样失败 → 存量失败非本次回归 → 报告注明即可

### 关键存量失败（已知，无需修复）

- `AuthenticationIntegrationTests`: NSubstitute 代理 internal IAuthApi 必失败（DynamicProxyGenAssembly2 需 InternalsVisibleTo）
- `AuthControllerTests.Admin_Can_Login`: 按用户决策「启动只创建 sysadmin」，admin 未 seed = 存量失败
- `UserTests` 系列: localhost:5000 连接拒绝 = 远程 E2E，本地不启动必失败

### dotnet test 参数陷阱

- **错误**: `dotnet test tests/LYBT.Tests.Desktop/Foo.cs` → MSB4025
- **正确**: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Foo"`

## Desktop API Client 三实现架构

Desktop 侧 `IApiClient` 由三实现类落地，任何子接口合并/新增都需同步改这 3 处：

### 架构图

```
IApiClient (主接口)
├── Auth: IApiClientAuth / IApiClientIdentity
├── Users: IApiClientUsers / IApiClientIdentity
└── ...其他子接口

IApiClient 实现:
1. RefitApiClient (远程模式)
   - Auth => new AuthApiClient(RestService.For<IAuthApi>)
   - Users => new UserApiClient(RestService.For<IUserApi>)

2. HttpClientApiClient (本地模式)
   - Auth => new AuthHttpApiClient(_httpClientFactory)
   - Users => new UsersHttpApiClient

3. SwitchingApiClient (运行时代理)
   - 按 URL 切换: localhost:5300→HttpClientApiClient, 否则→RefitApiClient
   - Auth => Current.Auth
```

### DI 注册

- 仅注册 `IApiClient` 单例（`Shell/Extensions/UnifiedApiClientExtensions.AddUnifiedApiClient`）
- 子接口（IApiClientAuth/IApiClientUsers/IApiClientIdentity）**无独立注册**
- 通过 `IApiClient.Auth` / `IApiClient.Users` / `IApiClient.Identity` 属性访问

### 修改清单（合并/新增子接口时）

1. **Contracts/ApiClient/**: 新建/修改子接口（如 IApiClientIdentity）
2. **RefitApiClient**: 添加属性 + 惰性初始化
3. **HttpClientApiClient**: 添加属性 + 惰性初始化
4. **SwitchingApiClient**: 添加属性 + Current 切换逻辑
5. **所有注入点**: 改构造函数参数类型（如 IApiClientAuth → IApiClientIdentity）
6. **测试文件**: 同步更新 mock/注入

### 段接口继承（重要）

子接口可继承 `IEntityApiSegment<TListDto,TDetailDto,TInputDto>`（5 标准 CRUD）：
```csharp
public interface IApiClientIdentity : IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>
{
    // 额外方法...
}
```
`EntityApiClientRepositoryBase<TListDto,TDetailDto>` 构造函数参数类型是 `IEntityApiSegment<...>`，子接口必须保留该继承否则编译错。
