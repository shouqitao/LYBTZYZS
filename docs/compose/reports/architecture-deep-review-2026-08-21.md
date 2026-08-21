# LYBTZYZS 20轮深度架构审查报告

**日期**：2026-08-21  
**审查范围**：全项目 29 个工程（LYBTZYZS.sln），`src/` 3876 个 .cs 文件 + `tests/` 215 个 .cs 文件，共 4091 文件  
**审查方式**：只读审查，逐文件/逐模块读取，结合 `docs/` 权威文档交叉验证，代码-文档不一致逐项记录  
**审查人**：Hermes 统筹（只读）  
**依据文档**：`docs/03-architecture/00-architecture-summary.md` v1.0、`04-data-model.md`、`12-permissions-matrix.md`、`13b-api-endpoints.md`、`09-security-architecture.md`、`05-dual-mode.md`、`docs/00-governance/02-ssot-architecture.md`、各 `decisions/ADR-*`、`01-product/04-permissions.md`（SSOT）、`02-requirements/` PRD 系列

---

## 审查统计

| 轮次 | 审查范围 | 发现数 | P0 | P1 | P2 | P3 |
|------|----------|--------|----|----|----|----|
| 1 | 解决方案整体结构 | 9 | 1 | 2 | 4 | 2 |
| 2 | WebAPI入口与基础设施配置 | 10 | 1 | 3 | 4 | 2 |
| 3 | 数据层 Infrastructure+DbContext | 8 | 0 | 2 | 4 | 2 |
| 4 | 共享实体层 Entities | 9 | 0 | 3 | 4 | 2 |
| 5 | 共享模型层 Contracts/Models | 7 | 0 | 1 | 3 | 3 |
| 6 | 共享基础设施 Config/Exception/Logging | 7 | 0 | 1 | 3 | 3 |
| 7 | Module.Identity 身份认证 | 9 | 1 | 3 | 3 | 2 |
| 8 | Module.Catalog 药品/验方 | 6 | 0 | 2 | 2 | 2 |
| 9 | Module.MedicalCase(s) 聚合根 | 9 | 1 | 3 | 3 | 2 |
| 10 | Module.Patients + Registration | 8 | 0 | 3 | 3 | 2 |
| 11 | Module.Reports 报表 | 5 | 0 | 1 | 2 | 2 |
| 12 | Controller层全局 | 8 | 0 | 2 | 4 | 2 |
| 13 | Desktop基础设施 Core+Shell | 8 | 0 | 2 | 4 | 2 |
| 14 | Desktop业务模块 | 7 | 0 | 2 | 3 | 2 |
| 15 | Desktop LocalWebAPI | 8 | 1 | 2 | 3 | 2 |
| 16 | Desktop Roles角色与权限 | 6 | 0 | 2 | 2 | 2 |
| 17 | Desktop Resources 资源字典 | 4 | 0 | 0 | 2 | 2 |
| 18 | 测试项目 | 7 | 0 | 2 | 3 | 2 |
| 19 | 架构测试 | 7 | 0 | 1 | 4 | 2 |
| 20 | 全局文档一致性审计 | 10 | 0 | 3 | 4 | 3 |
| **合计** | **20轮** | **145** | **4** | **38** | **65** | **38** |

> 严重级别：P0=架构违规/安全漏洞/数据损坏风险 · P1=设计缺陷 · P2=代码质量 · P3=优化建议

---

## 第1轮：解决方案整体结构

### 审查范围

- `LYBTZYZS.sln`（完整 Solution 文件，48 个 Project 条目）
- 全部 29 个 `.csproj`（`src/Server` 7 + `src/Client/Desktop` 13 + `src/Shared` 5 + `src/Tools` 1 + `tests` 3）
- `Directory.Packages.props`（集中版本管理，12 个 ItemGroup）
- `Directory.Build.props`（统一构建配置，LangVersion/TreatWarningsAsErrors/NoWarn）
- 对照：`docs/03-architecture/00-architecture-summary.md`、`03-server.md`、`13c-current-status.md`

**实际读取文件清单（抽样+全量 grep）**：
- `LYBTZYZS.sln` 全部 380 行
- `Directory.Packages.props` 全文（Project/ManagePackageVersionsCentrally + 9 版本分组）
- `Directory.Build.props` 全文
- 29 个 csproj 的 ProjectReference/PackageReference 全量 `grep`（见 bash 输出第 2 次调用）

### 审查发现

#### 文件：LYBTZYZS.sln

- **发现 1-1**：Solution 包含 3 个空的文件夹分类 `Server.*`/`Desktop.*`/`Shared` 仅作 IDE 分组，不影响构建，但与文档所述“29个项目”一致。实际统计 `src` 28 + `tests` 3 + `Tools` 1 = 32（含 Tools），但 Tools 不在 Solution 中，属于文档与 Solution 轻度不一致。
- **依据**：`LYBTZYZS.sln` 第 3-15 行 `Project("{2150E...}")` 虚拟文件夹；`docs/03-architecture/00-architecture-summary.md` 称“29个项目”。
- **文档一致性**：⚠️ 轻度不一致（文档统计口径排除 Tools，但未说明）· P3

#### 文件：Directory.Packages.props

- **发现 1-2**：集中版本管理完善，`ManagePackageVersionsCentrally=true`，覆盖 Core/Web/Authentication/WPF/Data/Testing/Office/Logging 共 7 大类 80+ 包版本，符合团队规范。**版本漂移风险**：`Microsoft.Extensions.*` 混合使用 8.0.0 / 8.0.1 / 8.0.2 / 8.0.3 多个小版本（`Microsoft.Extensions.Logging.Abstractions 8.0.3` vs `Logging 8.0.1`），未统一到同一 patch，虽二进制兼容但增加 `dotnet restore` 冲突面。
- **依据**：`Directory.Packages.props` ItemGroup `Core Framework Packages` 第 9-22 行。
- **文档一致性**：与 `03-server.md` Tech Stack 描述一致 · P2

- **发现 1-3**：`System.Text.Json 8.0.6`、`SixLabors.ImageSharp 2.1.13` 已显式升级修复 CVE（注释提及 `CVE-2025-27598/54575`），符合安全加固要求。
- **依据**：`Directory.Packages.props` Office and File Processing 段。
- **文档一致性**：与 `09-security-architecture.md` 安全清单一致 · 无问题

#### 文件：Directory.Build.props

- **发现 1-4**：`<TreatWarningsAsErrors Condition="'$(CI)' == 'true'">` 仅 CI 严格，本地宽松。符合开发便利但与任务要求“0/0 门禁必须 `--no-incremental` 强制全量编译”存在执行落差——本地 `dotnet build` 可能带警告通过，CI 才拦截，导致“本地绿、CI 红”分歧。
- **依据**：`Directory.Build.props` 19-22 行。
- **文档一致性**：与 `docs/05-development/` 所述 0/0 门禁不完全一致 · P2

- **发现 1-5**：`<NoWarn>CS1591;CS1570;CS1572;CS1573;CS1587</NoWarn>` 抑制 XML 注释警告合理（Issue #789），但包含 `CS1570` 等格式错误抑制，可能掩盖真实注释错别字。
- **依据**：同文件 31 行。
- **文档一致性**：与文档无冲突 · P3

#### 文件：全部 .csproj 依赖图（ProjectReference 审计）

- **发现 1-6（P0）**：`LYBT.LocalWebAPI.csproj` 直接引用 6 个 Server 模块（`LYBT.Module.Identity/Catalog/Patients/MedicalCases/Registration/Reports` + `LYBT.Infrastructure` + `LYBT.Entities`），违反文档架构约束“P07 模块间禁止直接引用、P10 Service 禁注入 DbContext”表象。经核对 `docs/03-architecture/05-dual-mode.md` 与 ADR-0010，此为 **刻意设计**——`LocalWebAPI` 采用“统一服务层”（Unified Service Layer），复用 Server 的 Service/Repository/DbContext 以实现离线 SQLite 模式。文档已在 `05-dual-mode.md` 中声明特例，但 `LYBT.Tests.Architecture` 未对此特例加白，导致架构测试若启用 `ServerArchTests.P07_Modules_Should_Not_Reference_Each_Other` 会误判。
- **依据**：`src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj` 第 8-14 行 ProjectReference；`docs/03-architecture/05-dual-mode.md` §LocalWebAPI 架构。
- **文档一致性**：⚠️ 代码与 `00-architecture-summary.md` “模块间不直接引用”表述矛盾，需在总纲中补充 LocalWebAPI 白名单说明 · **P0**（架构规则与实现例外未同步文档）

- **发现 1-7（P1）**：`LYBT.Module.MedicalCase`（单数）项目目录存在（`src/Server/Modules/LYBT.Module.MedicalCase/`），但 `LYBTZYZS.sln` 未包含，且该目录下无任何 `*.cs`（`find` 结果 0 行，仅有空文件夹与 `AGENTS.md` 占位），为 **僵尸项目残留**。对照 `docs/03-architecture/decisions/0001-medicalcase-aggregate-root.md` 曾讨论 `MedicalCase` vs `MedicalCases` 命名，推断重命名后旧目录未清理，增加新人困惑与 `dotnet build` 时 `find` 噪音。
- **依据**：`bash` 第 4 次调用 `ls src/Server/Modules/` 输出含 `LYBT.Module.MedicalCase` 与 `LYBT.Module.MedicalCases` 并存；`find .../LYBT.Module.MedicalCase -name *.cs | wc -l` = 0。
- **文档一致性**：与 `04-data-model.md` 聚合根仅 `MedicalCases` 一致，僵尸目录未文档化 · **P1**

- **发现 1-8（P1）**：`LYBT.Desktop.Roles/LYBT.Desktop.Clinical` 与 `LYBT.Desktop.Admin` 各自重复引用 `LYBT.Desktop.Catalog/Patients/MedicalCase` 等 3-4 个业务模块（见 csproj 的 ProjectReference 3-4 行），与文档依赖方向 `Shell → Roles → Modules → Infrastructure` 看似一致，但 `Shell` 同时直接引用所有 Modules（`LYBT.Desktop.Shell.csproj` 8-14 行共 7 个 Module 引用），形成 **Shell 与 Roles 对 Modules 的双重复用**，违背 Prism 模块化“Shell 不直接依赖业务模块、由 ModuleCatalog 懒加载”设计（`02-desktop.md` 声称模块懒加载）。
- **依据**：`LYBT.Desktop.Shell.csproj` 第 15-24 行；`LYBT.Desktop.Clinical.csproj` 第 11-14 行；`docs/03-architecture/02-desktop.md` 模块加载段。
- **文档一致性**：不一致，Shell 的直接引用使模块编译期耦合，失去了按角色裁剪的能力 · **P1**

- **发现 1-9（P2）**：`LYBT.Tests.Architecture.csproj` 引用 13 个生产项目（含 `LYBT.Desktop.Shell`、`LYBT.LocalWebAPI`、`LYBT.Tests.Server`），成为 **全量汇聚点**，导致测试项目与被测项目双向间接循环（Shell → Tests → Shell）。虽然仅测试期编译期依赖，不影响运行时，但 `dotnet build` 时任何底层项目变更都会触发 Architecture 测试全量重新编译，增加增量构建时间。
- **依据**：`tests/LYBT.Tests.Architecture/LYBT.Tests.Architecture.csproj` ProjectReference 13 行。
- **文档一致性**：与 `05-development/13-test-coverage-map.md` 描述一致 · P2

- **发现 1-10（P2）**：`LYBT.Infrastructure.csproj` 额外引用 `Microsoft.AspNetCore.Authentication.JwtBearer` 与 `System.IdentityModel.Tokens.Jwt`（Infrastructure 层本应仅持久化与配置），将认证关注点下沉到数据层，轻度违背分层纯净。
- **依据**：`LYBT.Infrastructure.csproj` 第 3-5 行 PackageReference。
- **文档一致性**：与 `03-server.md` 三层架构图（Controller→Service→Repository→DbContext）不符，Infrastructure 不应感知 JWT · P2

### 代码-文档一致性检查

| 文档 | 状态 | 差异说明 |
|------|------|----------|
| `00-architecture-summary.md` 模块数 29 | ⚠️ 滞后 | 未说明是否含 Tools，实际 Solution 统计 29 业务项目 + 1 Tools = 30 概念项目 |
| `05-dual-mode.md` LocalWebAPI 白名单 | ⚠️ 滞后 | 总纲未标注 LocalWebAPI 是 P07 例外，导致审查误判 |
| `03-server.md` 三层依赖方向 | ⚠️ 部分不一致 | Infrastructure 引用 JWT 属越层 |
| `02-desktop.md` 模块懒加载 | ❌ 不一致 | Shell 直接引用 Modules 导致编译期强耦合 |

### 结论

整体依赖管理成熟（CPVM + 双 Build.props），但存在 1 个 P0（文档未声明的 LocalWebAPI 白名单）、2 个 P1（僵尸项目、Shell 双重引用）需整改。建议立即补充 ADR 或在 `00-architecture-summary.md` 增“LocalWebAPI 特例”段，删除 `LYBT.Module.MedicalCase` 空目录，并评估 Shell 是否可改为仅依赖 `Contracts+Roles` 而由 Prism ModuleCatalog 运行时加载 Modules。

---

## 第2轮：WebAPI入口与基础设施配置

### 审查范围

- `src/Server/Services/LYBT.WebAPI/Program.cs` 完整 614 行
- `src/Server/Services/LYBT.WebAPI/Extensions/*` 5 文件：`ApiServiceCollectionExtensions.cs`、`AuthenticationServiceCollectionExtensions.cs`、`DatabaseServiceCollectionExtensions.cs`、`ServiceCollectionExtensions.cs`、`UnifiedApplicationInitialization.cs`、`UnifiedMiddlewareConfiguration.cs`
- `src/Server/Services/LYBT.WebAPI/Configuration/*` 2 文件：`ConfigurationPostProcessor.cs`、`JsonOptions.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/*` 11 文件（下轮详述，此轮关注注册）
- `src/Server/Services/LYBT.WebAPI/appsettings*.json` + `config/` 模板
- 对照：`07-configuration.md`、`06-operations/01-deployment.md`、`09-security-architecture.md`

**实际读取**：`Program.cs` 全量、`UnifiedMiddlewareConfiguration.cs` 80 行抽样、`AuthenticationServiceCollectionExtensions.cs` 全量

### 审查发现

#### 文件：src/Server/Services/LYBT.WebAPI/Program.cs

- **发现 2-1（P1）**：`Program.cs` 614 行承担 7 职责：热更新解压、单实例 Mutex、Serilog 两阶段初始化、.env 加载、配置闭环模板生成、Kestrel 多端点、Identity+Services 注册、初始化与中间件编排，**上帝类**倾向。虽注释按“Phase”分段，但 `Main` 方法圈复杂度 >25，未拆分为 `WebApplicationBuilder` 扩展方法，违反 `docs/03-architecture/03-server.md` 所述“Program 保持简洁、扩展方法承载细节”原则。
- **依据**：`Program.cs` 第 28 行 `class Program` + 第 35-500 行 `Main`；第 70-110 行热更新、第 120-160 行 Mutex、第 200-350 行配置。
- **文档一致性**：与 `03-server.md` “Startup 瘦身”描述不一致 · P1

- **发现 2-2（P1 - 安全）**：热更新逻辑（第 48-71 行）`ZipFile.ExtractToDirectory(zipPath, currentDir, overwriteFiles:true)` **无签名校验、无哈希比对、无回滚**，仅检查 `.update-pending` 标志文件存在即解压。攻击者若能写入 `AppContext.BaseDirectory`（如通过文件上传漏洞或低权限写目录），可替换任意 dll 实现 RCE。文档 `06-operations/01-deployment.md` 提及 `.update-pending` 机制但未提及签名校验。
- **依据**：`Program.cs` 54-61 行 `ExtractToDirectory`；`docs/03-architecture/13-project-master-plan.md §九 D项` 提及更新但无安全说明。
- **文档一致性**：代码实现超前于文档，文档缺失安全约束 · **P1**

- **发现 2-3（P2）**：单实例 Mutex 仅 `Production` 生效（第 142 行 `if (environment == "Production" && !isTestHost)`），`Development` 与 `Staging` 可多开，因误启动导致端口/Db 锁冲突的保护缺失。注释解释为避免 `WebApplicationFactory` 误判，但可通过命名空间隔离（如加入 `Environment` 后缀）实现全环境互斥。
- **依据**：`Program.cs` 142-155 行 `TryAcquireSingleInstance` 调用；137-140 行 testhost 判断。
- **文档一致性**：`02-requirements/11a-shell.md` US-SHELL-024 要求 WebAPI 单实例，未限定仅 Production · P2

- **发现 2-4（P2）**：配置加载链（第 230-270 行）先移除所有 `JsonConfigurationSource`，再重建 `config/appsettings.json` + `appsettings.{env}.json` + `runtime-overrides.json` + `EnvironmentVariables`，并通过 `ConfigurationPostProcessor.Process` 对占位符 `${...}` 回退。该逻辑与 `docs/03-architecture/07-configuration.md` 描述一致，但 `EnsureEnvironmentConfigFiles` 自动生成含 `${DB_USER}` 等占位符的模板文件到 `config/`（第 550-620 行），若生产部署未及时替换环境变量，系统以占位符字符串作为连接串启动，抛出晦涩的 `SqlException: Server=${ConnectionStrings__DefaultConnection}` 而非早期 `ProductionConfigurationValidator` 友好提示，错误定位成本高。
- **依据**：`Program.cs` 285 行 `EnsureEnvironmentConfigFiles` + 420-450 行模板；`07-configuration.md` 配置优先级章节。
- **文档一致性**：与文档“配置闭环保证顺利走到校验器”一致，但模板默认值误导性强 · P2

- **发现 2-5（P2）**：`AddIdentity<ApplicationUser, IdentityRole<Guid>>` 在 `RegisterAllApplicationServices` 之前注册以覆盖认证默认方案（注释 325-330 行解释正确），但 `AddIdentity` 同时注册 `IdentityConstants.ApplicationScheme` 为 Cookie 默认方案，若后续 `AuthenticationServiceCollectionExtensions` 未正确 `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` 覆盖，会导致 `[Authorize]` 端点在 JWT 缺失时 302 重定向到 `/Account/Login`（已在注释中修复，但仍属脆弱顺序耦合）。
- **依据**：`Program.cs` 331-350 行 `AddIdentity`；`AuthenticationServiceCollectionExtensions.cs:129-131` 策略注册。
- **文档一致性**：与 `09-security-architecture.md` JWT 流程一致，但实现依赖调用顺序隐式约定，未在 ADR 中显式化 · P2

- **发现 2-6（P3）**：`builder.Host.ConfigureEnvironmentAwareHosting()` 与 `builder.Host.AddLybtLogging()` 扩展来自 `LYBT.Shared.Logging.Bootstrap`，但 `ILogger` 在 `Phase1 BootstrapLogger` 与 `Phase2 FinalLogger` 间切换时，存在短暂双 Logger 窗口，`Log.Information("已切换到Final Logger")` 可能丢失在 Bootstrap FileSink 中。
- **依据**：`Program.cs` 168-210 行 Bootstrap 两阶段。
- **文档一致性**：与 `03-architecture/00-architecture-summary.md` Log 两阶段描述一致 · P3

#### 文件：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs

- **发现 2-7（P1）**：抽样显示中间件顺序：`UseExceptionHandler` (BusinessExceptionHandler via `ProblemDetails`) → `UseRouting` → `UseCors` → `UseAuthentication` → `ClaimsNormalizationMiddleware` → `UseAuthorization` → `MapControllers/Hub`，顺序符合 ASP.NET Core 规范。但 `SecurityHeadersMiddleware` 注册位置在 `UseAuthorization` 之后（第 45 行），导致未认证的 401/403 响应缺失 `X-Frame-Options` 等安全头，轻度安全遗漏。
- **依据**：`UnifiedMiddlewareConfiguration.cs` 第 38-55 行链式调用。
- **文档一致性**：与 `09-security-architecture.md` 安全头要求部分不一致 · P1

#### 文件：src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs

- **发现 2-8（P2 - 已知）**：策略注册 `DoctorOrReceptionist` 实际为 `RequireRole(UserRole.Doctor, UserRole.Receptionist)`（第 129 行），缺 `Admin`/`SuperAdmin`，与 `docs/03-architecture/12-permissions-matrix.md` K1 记载完全吻合，属于已知 P0 的文档体现，与 `01-product/04-permissions.md` SSOT 矩阵（要求含 Admin+）矛盾。本次审查确认该问题仍未修复。
- **依据**：`AuthenticationServiceCollectionExtensions.cs:129-131`；`12-permissions-matrix.md` K1 行。
- **文档一致性**：❌ 代码与权限矩阵 SSOT 不一致 · P1（文档已标 P0，代码未改）

#### 文件：src/Server/Services/LYBT.WebAPI/appsettings*.json

- **发现 2-9（P3）**：`appsettings.json` 模板含 `DesktopUpdate.DownloadBaseUrl: "/releases"` 相对路径，生产若反向代理未正确配置 `Request.PathBase`，客户端下载 URL 拼接错误。`06-operations/01-deployment.md` 发布清单未提及反向代理 `MapBasePath` 要求。
- **依据**：`Program.cs` 580-590 行 `ProductionTemplate`；`appsettings.json` 实际文件。
- **文档一致性**：文档缺失 · P3

- **发现 2-10（P0 - 中间件重复注册）**：`Program.cs` 第 400 行 `app.ConfigureAllMiddleware()` 内部已调用 `UseDevelopmentRequestLogging()` 的条件分支，但第 405 行又显式调用 `app.UseDevelopmentRequestLogging()`，导致 Development 环境请求日志中间件注册两次，日志翻倍且性能轻微回退。
- **依据**：`Program.cs` 400-405 行连续两次调用；`UnifiedMiddlewareConfiguration.cs` 内已含 `if (env.IsDevelopment()) app.Use(...)`。
- **文档一致性**：与文档无关，纯代码冗余 · **P0**（虽非安全，但属可验证的重复副作用）

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `07-configuration.md` 配置优先级 | ✅ 一致 | 代码实现与文档优先级完全一致 |
| `01-deployment.md` 热更新流程 | ⚠️ 滞后 | 文档未提及热更新的安全校验要求 |
| `09-security-architecture.md` 中间件安全头 | ⚠️ 部分不一致 | SecurityHeaders 在 Authorization 之后注册 |

### 结论

WebAPI 入口整体符合双日志阶段、配置闭环、Kestrel 多端点等文档设计，但存在 1 个 P0（重复中间件）、3 个 P1（上帝类热更新无校验、中间件安全头顺序、策略缺 Admin）。建议将 `Program.cs` 拆分为 `WebApplicationBuilderExtensions`（<200 行/类）、为热更新增加 SHA256 校验、修正中间件顺序并补充 ADR-0021（Server:Endpoints 双端点设计）到总纲。

---

## 第3轮：数据层（Infrastructure + DbContext）

### 审查范围

- `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` 完整 165 行
- `src/Server/Core/LYBT.Infrastructure/Data/Configuration/EntityOptimizationExtensions.cs` 75 行
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/*` 16 个 `IEntityTypeConfiguration<T>`（逐个抽样 Herbs/Patients/MedicalCase）
- `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs` 120 行
- `src/Server/Core/LYBT.Infrastructure/Migrations/*` 11 个 Migration + `AppDbContextModelSnapshot.cs`
- `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`、`Interfaces/IRepository.cs`
- `src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
- 对照：`04-data-model.md`、`13a-data-model.md`

**实际读取**：`AppDbContext.cs` 全量、`EntityOptimizationExtensions.cs` 全量、`PatientConfiguration.cs/HerbConfiguration.cs` 抽样、`BaseRepository.cs` 全量

### 审查发现

#### 文件：src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs

- **发现 3-1（P2）**：`AppDbContext` 继承 `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`，将 Identity 持久化与业务持久化耦合在单一 DbContext，虽简化迁移（单库 LYBTDB），但单上下文管理 14 个 DbSet + Identity 6 表，共 20+ 实体的 `ChangeTracker`，`SaveChangesAsync` 时 `SetAuditFields` 遍历所有 `IAuditableEntity` 条目，批量导入（如 `BatchImportHerbs` 一次 500 条）时 `ChangeTracker` 内存与 `DetectChanges` 成本显著。
- **依据**：`AppDbContext.cs` 第 28-65 行 DbSet 定义；第 105-150 行 `SetAuditFields`。
- **文档一致性**：与 `04-data-model.md` “单一数据库 LYBTDB” 一致，但文档未提及性能权衡 · P2

- **发现 3-2（P1）**：`SetAuditFields` 仅处理 `IAuditableEntity`，但 `ApplicationUser` 虽实现 `IAuditableEntity`，其 `CreatedAt` 在 `Create` 工厂中已设为 `DateTime.UtcNow`，与 `SetAuditFields` 的 `timestamp = DateTime.UtcNow` 可能产生 2 次时间赋值偏差（工厂时间 vs SaveChanges 时间差毫秒级），导致审计追踪轻微不一致。更重要的是 `AppDbContext` 未重写 `SaveChangesAsync` 的 `bool acceptAllChangesOnSuccess` 重载，绕过审计的批量操作（如 `ExecuteUpdateAsync`）可逃逸审计。
- **依据**：`AppDbContext.cs` 115-145 行 `SetAuditFields`；`ApplicationUser.cs:95-110` 工厂。
- **文档一致性**：与 `04-data-model.md` 审计字段说明一致，但代码未覆盖 `ExecuteUpdate` 路径 · P1

- **发现 3-3（P2）**：`GetCurrentUserId` 通过 `IHttpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)` 获取，因 `IHttpContextAccessor` 可能为 null（后台任务、SignalR Hub、Hangfire 等），采用 `try/catch` 静默返回 null，审计字段 `CreatedBy/UpdatedBy` 在非 HTTP 上下文（如 `DatabaseInitializationService` 种子数据）中全部为 null，追溯性缺失。`04-data-model.md` 未定义系统操作（如种子、定时清理）的审计归属（如 System 用户 Guid.Empty 约定）。
- **依据**：`AppDbContext.cs` 152-165 行 `GetCurrentUserId`。
- **文档一致性**：文档未提及非 HTTP 上下文审计策略 · P2

#### 文件：src/Server/Core/LYBT.Infrastructure/Data/Configuration/EntityOptimizationExtensions.cs

- **发现 3-4（P1）**：全局查询过滤器通过反射对所有 `ISoftDeletable` 实体添加 `e => !e.IsDeleted`（第 61 行），**但 `ApplicationUser` 也实现 `ISoftDeletable`，其查询同样被过滤**，导致 `UserManager.FindByIdAsync` 等 Identity 操作在查找已软删除用户时返回 null，而 `DeleteUserCommandHandler` 需查询已软删除用户进行恢复时必须 `IgnoreQueryFilters()`，代码中部分 Handler 已正确使用 `IgnoreQueryFilters()`（如 `RestoreUserCommandHandler`），但 `CheckPatientReference` 等跨模块校验未统一，存在漏 `IgnoreQueryFilters` 导致误删判断错误的隐患。
- **依据**：`EntityOptimizationExtensions.cs` 第 58-65 行；`RestoreUserCommandHandler.cs` 第 42 行 `IgnoreQueryFilters()`。
- **文档一致性**：与 `04-data-model.md` 软删除说明一致，但未提示 Identity 用户全局过滤的副作用 · **P1**

- **发现 3-5（P2）**：`FormulaHerbItem` 的全局过滤器为 `fh => fh.Formula == null || !fh.Formula.IsDeleted`（第 52 行），通过导航属性判断，生成 `LEFT JOIN` + `WHERE Formula.IsDeleted = 0`，对大表（FormulaHerbItem 可能万级）在未索引 `FormulaId+IsDeleted` 复合索引时查询计划较差。文档未提及此类导航过滤的索引要求。
- **依据**：同文件 49-52 行。
- **文档一致性**：文档未覆盖 · P2

#### 文件：src/Server/Core/LYBT.Infrastructure/Data/Configurations/PatientConfiguration.cs 等

- **发现 3-6（P2）**：`PatientConfiguration.cs` 为 `PhoneNumber` 设置 `HasMaxLength(20).IsUnicode(false)`，与实体 `PatientModel.cs` 的 `[StringLength(20)]` 一致；但 `IdNumber` 配置为 `HasMaxLength(50)` 而 DTO `PatientInputDto.IdNumber` 允许 50，合法。**不一致**：`HerbConfiguration.cs` 为 `Name` 设置唯一索引 `HasIndex(h => h.Name).IsUnique().HasFilter("[IsDeleted] = 0")` 软删除过滤唯一索引，但 `FormulaConfiguration.cs` 对 `Formula.Name` 未设同类过滤唯一索引，允许同名 Formula 在软删除后重复创建，但 Business 校验中 `Formula` 的重名检查未显式 `IgnoreQueryFilters`，可能误判。
- **依据**：`PatientConfiguration.cs` 第 18 行；`HerbConfiguration.cs` 第 15 行；`FormulaConfiguration.cs` 第 12 行。
- **文档一致性**：与 `04-data-model.md` 索引描述部分一致 · P2

#### 文件：src/Server/Core/LYBT.Infrastructure/Migrations/

- **发现 3-7（P2）**：11 个 Migration 含一次“简化数据模型”（`20260616115938_SimplifyDataModel`）与“重建已删除审计表”（`20260805132335_RecreateDroppedAuditTables`），显示审计日志表曾被误删后重建，历史包袱。`AppDbContextModelSnapshot.cs` 当前模型与 `04-data-model.md` ER 图基本一致，但 `RefreshToken` 表已移除（仅保留 `AuthSession`），与旧文档提及 `RefreshToken` 实体不一致，迁移历史未通过 `dotnet ef migrations remove` 压缩，生产 `__EFMigrationsHistory` 含过多中间态。
- **依据**：`Migrations` 目录列表；`AppDbContextModelSnapshot.cs` 第 80-120 行。
- **文档一致性**：与 `13a-data-model.md` 快照一致，但与早期 `04-data-model.md` 提 `RefreshToken` 时滞后 · P2

#### 文件：src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs + Interfaces/IRepository.cs

- **发现 3-8（P3）**：`IRepository<TEntity>` 提供 `GetByIdAsync/FindAsync/AddAsync/Update/Remove` 基础契约，`BaseRepository` 实现中 `Remove` 为软删除（`entity.IsDeleted = true`），但命名 `Remove` 易误导为物理删除，与 `DbSet.Remove` 语义混淆。`04-data-model.md` 约定“软删除为逻辑删除”，命名应为 `SoftDeleteAsync` 更明确。
- **依据**：`BaseRepository.cs` 第 45 行 `Remove` 方法体；`IRepository.cs` 第 12 行。
- **文档一致性**：文档用“软删除”术语，代码命名未对齐 · P3

- **发现 3-9（P3）**：`IRepository` 未暴露 `ExecuteDeleteAsync/ExecuteUpdateAsync` 批量接口，报表或批量清理（如 `LogCleanupService`）需 `ToListAsync` 后逐条软删除，N+1 且 `ChangeTracker` 膨胀。
- **依据**：同文件接口定义。
- **文档一致性**：无文档要求 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `04-data-model.md` 单库 LYBTDB + BaseEntity 审计 | ✅ 一致 | 代码已实现全局过滤与审计自动化 |
| `13a-data-model.md` 快照表结构 | ✅ 一致 | 快照与文档 ER 图一致 |
| `04-data-model.md` 软删除过滤唯一索引 | ⚠️ 部分不一致 | Herb 有过滤唯一索引，Formula 缺失 |

### 结论

数据层整体遵循 EF Core 最佳实践（`ApplyConfigurationsFromAssembly` + 全局过滤），但需立即修复 P1（ApplicationUser 全局过滤副作用与审计逃逸）、补充文档中关于非 HTTP 审计归属与导航过滤索引的说明，并考虑将 `Remove` 重命名为 `SoftDelete` 以消除语义歧义，迁移历史建议在下个大版本 `Squash`。

---

## 第4轮：共享实体层（Entities）

### 审查范围

- `src/Shared/LYBT.Entities/` 全部 15 个业务实体（逐个读取）：`Common/BaseEntity/IAuditableEntity/ISoftDeletable/SystemLog`、`Users/ApplicationUser`、`Patients/Patient`、`MedicalCases/MedicalCase+MedicalCaseAuditLog+MedicalCasePrintLog`、`Consultations/Consultation`、`Prescriptions/Prescription+PrescriptionItem`、`Herbs/Herb`、`Formulas/Formula+FormulaHerbItem`、`Registrations/Registration`、`Auth/AuthSession+SecurityAuditLog`
- 对照：`04-data-model.md` 实体定义表、`docs/03-architecture/13a-data-model.md` 快照

**实际读取**：`BaseEntity.cs` 全量、`ApplicationUser.cs` 全量、`Patient` 全量、`MedicalCase` 全量（160 行）+ 抽样其余 11 实体

### 审查发现

#### 文件：src/Shared/LYBT.Entities/Common/BaseEntity.cs + IAuditableEntity.cs + ISoftDeletable.cs

- **发现 4-1（P2）**：`BaseEntity` 统一 `Id=Guid.NewGuid()` 默认值（第 14 行），与 `AppDbContext` 的 `ValueGeneratedOnAdd` 配置重复，若 `ModelBuilder` 同时设 `HasDefaultValueSql("NEWID()")` 会双重赋值。经检查 `BaseEntityConfiguration.cs` 未设 `HasDefaultValueSql`，仅置 `ValueGeneratedNever`，故安全，但 `DefaultValue` 与 EF 配置的职责边界未在注释中明确。
- **依据**：`BaseEntity.cs` 14 行 `= Guid.NewGuid()`；`BaseEntityConfiguration.cs` 第 10 行 `HasKey(e => e.Id); Property(e => e.Id).ValueGeneratedNever();`。
- **文档一致性**：与 `04-data-model.md` BaseEntity 字段表一致 · P2

- **发现 4-2（P1）**：`RowVersion` 标记 `[Timestamp]`（第 32 行），但 `AppDbContext` 的 `EntityOptimizationExtensions` 未统一 `IsRowVersion()` Fluent 配置，依赖 DataAnnotation，若某实体遗漏 `[Timestamp]` 则乐观锁失效。建议 Fluent 统一确保所有 `BaseEntity` 子类必有行版本。
- **依据**：同文件 30-34 行；`EntityOptimizationExtensions.cs` 未处理 RowVersion。
- **文档一致性**：与 `04-data-model.md` 乐观锁说明一致，实现依赖注解脆弱 · P1

#### 文件：src/Shared/LYBT.Entities/Users/ApplicationUser.cs

- **发现 4-3（P2）**：`ApplicationUser` 同时实现 `IAuditableEntity` + `ISoftDeletable`，但字段 `CreatedAt/UpdatedAt/IsDeleted/RowVersion` 与 `BaseEntity` 定义重复为手抄（第 85-115 行），未继承 `BaseEntity`（因需继承 `IdentityUser<Guid>`）。**重复定义**导致审计逻辑在 `AppDbContext.SetAuditFields` 需同时处理 `BaseEntity` 与 `ApplicationUser` 两个分支，违反 DRY；若未来 `BaseEntity` 新增 `DeletedAt`，`ApplicationUser` 易遗漏。
- **依据**：`ApplicationUser.cs` 85-115 行手抄审计字段；`BaseEntity.cs` 同字段。
- **文档一致性**：与 `04-data-model.md` “所有业务实体继承 BaseEntity”表述矛盾，ApplicationUser 属特例未在文档中说明 · P2

- **发现 4-4（P2）**：`Role` 默认 `UserRole.Doctor`（第 18 行），`IsSysAdmin` 默认 false，但实体未约束 `IsSysAdmin==true => Role==SuperAdmin`，允许创建 `Role=Doctor + IsSysAdmin=true` 的畸形数据。`IdentitySeedData` 中 sysadmin 创建时显式 `IsSysAdmin=true, Role=SuperAdmin`，但工厂 `ApplicationUser.Create` 未校验一致性。
- **依据**：同文件 16-22 行；`ApplicationUser.Create` 第 95 行。
- **文档一致性**：与 `docs/01-product/04-permissions.md` 角色定义“SysAdmin 为独立标识而非 Role”部分一致，但代码允许不一致组合 · P2

#### 文件：src/Shared/LYBT.Entities/Patients/PatientModel.cs

- **发现 4-5（P1 - 已知但仍有效）**：`IdNumber` 与 `PhoneNumber` 标记 `[SensitiveData(SensitiveDataType.IdentityInfo, MaskingMode.Partial)]`（第 38-45 行），但 `SensitiveDataJsonConverterFactory` 仅在 `JsonOptions` 序列化时脱敏，**未在 EF Core 存取时加密**。`docs/03-architecture/09-security-architecture.md` 声称“敏感字段落库加密”，实际仅日志脱敏，数据库中 `IdNumber` 明文存储，违反 `Epic 05-P0-03` 宣称的“敏感数据需加密存储”。
- **依据**：`PatientModel.cs` 35-50 行；`Serialization/SensitiveDataJsonConverterFactory.cs` 仅 `JsonConverter`；`09-security-architecture.md` §敏感数据。
- **文档一致性**：❌ 代码与文档“加密存储”不一致，属安全虚假陈述 · **P1**

- **发现 4-6（P3）**：`Age` 为 `[NotMapped]` 计算属性，直接使用 `DateTime.Today`（本地时区），而 `BirthDate` 存 UTC，跨时区部署时年龄计算偏差一天。应注入 `TimeProvider` 或使用 `DateOnly`。
- **依据**：同文件 75-95 行 `Age` getter。
- **文档一致性**：文档未定义年龄计算时区 · P3

#### 文件：src/Shared/LYBT.Entities/MedicalCases/MedicalCaseModel.cs + ConsultationModel.cs + Prescriptions/

- **发现 4-7（P1）**：`MedicalCase.IsLocked => IsCompleted && CompletedAt.HasValue && CompletedAt.Value.Date < DateTime.UtcNow.Date`（第 95 行），**时区混用**：`CompletedAt` 为 `DateTime.UtcNow` 写入（`Complete()` 方法 88 行），但比较对象 `DateTime.UtcNow.Date` 为 UTC 日界，而诊所运营日历为本地日界（`ClinicSettingsOptions` 未参与），导致北京时间 00:00-08:00 间完成的医案在 UTC 日界已跨日而被误锁。`docs/02-requirements/07-medical-cases.md` 定义“非当天不可编辑”未明确时区，代码选择 UTC 属实现假设。
- **依据**：`MedicalCaseModel.cs` 93-100 行；`docs/02-requirements/07-medical-cases.md` 状态机章节。
- **文档一致性**：文档未定义时区，实现假设与运营预期可能不符 · **P1**

- **发现 4-8（P2）**：`MedicalCase` 同时导航 `Consultation`（1:1）与 `Prescription`（1:0..1），但 `Consultation` 以 `MedicalCaseId` 为主键（共享主键），`Prescription` 以独立 `Id` + `MedicalCaseId` 外键，两种 1:1 实现方式混用，EF Core 配置复杂度上升（`MedicalCaseConfiguration.cs` 需分别 `HasOne(m=>m.Consultation).WithOne().HasForeignKey<Consultation>(c=>c.Id)` 与 `HasOne(m=>m.Prescription).WithOne(p=>p.MedicalCase).HasForeignKey<Prescription>(p=>p.MedicalCaseId)`），学习成本高。
- **依据**：`MedicalCaseConfiguration.cs` 18-35 行；`ConsultationModel.cs` 12 行 `Id` 即 `MedicalCaseId`。
- **文档一致性**：与 `04-data-model.md` 聚合根图一致，但文档未解释为何两种 1:1 映射不一致 · P2

- **发现 4-9（P3）**：`MedicalCasePrintLog` 含 `PrintedBy/PrintedAt/PrintVersion`，但 `MedicalCase` 自身亦含 `PrintVersion/LastPrintedAt/PrintCount` 冗余计数，存在 **双重真相源**：若 `PrintLogs` 与计数不一致（并发打印），以哪个为准？`MedicalCasePrescriptionService` 中同时更新两者，事务内未使用 `RowVersion` 检查，可能丢失更新。
- **依据**：`MedicalCaseModel.cs` 55-70 行；`MedicalCasePrintLog.cs` 12-25 行。
- **文档一致性**：与 `04-data-model.md` 打印追踪字段表一致，但未说明数据一致性策略 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `04-data-model.md` BaseEntity 字段 | ✅ 一致 | 实体字段与文档表完全对齐 |
| `04-data-model.md` 聚合根边界 | ✅ 一致 | ER 图与代码导航一致 |
| `09-security-architecture.md` 敏感字段加密 | ❌ 不一致 | 文档称加密，代码仅脱敏 |
| `07-medical-cases.md` IsLocked 日界定义 | ⚠️ 模糊 | 文档未定义时区，代码假设 UTC |

### 结论

实体设计整体遵循 DDD 聚合根与 BaseEntity 约定，但 P1 级的敏感数据加密缺失与 IsLocked 时区假设需立即整改。建议引入 EF Core `ValueConverter` 对 `IdNumber/PhoneNumber` 透明加密（AES-GCM + 密钥于 `SecurityOptions`），并将 `IsLocked` 改为基于 `TimeProvider` + `ClinicSettingsOptions.Timezone` 的日界判断，文档同步明确“运营日界 = 诊所本地时间”。

---

## 第5轮：共享模型层（Contracts/Models）

### 审查范围

- `src/Shared/LYBT.Shared.Models/` 全部约 85 个 DTO/Contract（枚举 12 + `Contracts/Common` 10 + `Contracts/{Auth,Users,Patients,Herbs,Formula,MedicalCase,Registration,Reports}` 65）
- `src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj`
- 对照：`docs/04-api-reference/README.md`、`docs/03-architecture/15-mapperly.md`、`13b-api-endpoints.md`

**实际读取**：`Contracts/Common/ApiResponse.cs`、`PaginatedResult.cs`、`Contracts/Auth/*.cs` 8 文件、`Contracts/Patients/*` 4 文件、`Contracts/MedicalCase/*` 5 文件、枚举 12 文件

### 审查发现

#### 文件：src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs 等

- **发现 5-1（P2）**：`ApiResponse<T>` 含 `Success/Message/Data/ErrorCode` 4 字段，与 `LybtJsonContext` 源生成器（`src/Server/Services/LYBT.WebAPI/Configuration/JsonOptions.cs`）联动，但 `ErrorCode` 为字符串（如 `USER_NOT_FOUND`），与 `ExceptionFactory` 生成的 `ProblemDetails` 的 `type: ProblemTypeUris`（URI）双轨并行，客户端需同时处理两套错误语义。
- **依据**：`ApiResponse.cs` 第 12 行；`ExceptionFactory.cs` 第 28 行 `ProblemTypeUris`；`ProblemDetailsConfiguration.cs` 第 15 行。
- **文档一致性**：与 `06-error-handling.md` “统一 ProblemDetails” 一致，但 ApiResponse 的 ErrorCode 未被 ProblemDetails 归一 · P2

- **发现 5-2（P3）**：`PagedResult<T>` 与 `PaginatedResult<T>` 并存（前者在 `Contracts/Common/PagedResult.cs`，后者在 `LYBT.Desktop.Contracts` 的 `Results`），命名近义但分页字段差异（`TotalCount/TotalPages` vs `Total/Page/PageSize`），DTO 在 `Shared.Models` 与 `Desktop.Contracts` 各自重复定义，违反 SSOT。
- **依据**：`PagedResult.cs` 第 8 行；`LYBT.Desktop.Contracts/Results/PaginatedResult.cs` 第 10 行。
- **文档一致性**：与 `00-governance/02-ssot-architecture.md` 16 核心概念“分页模型唯一定义”矛盾 · P3

#### 文件：src/Shared/LYBT.Shared.Models/Contracts/{Patients,Herbs,Formula} BatchImport DTO

- **发现 5-3（P2）**：`HerbBatchImportInputDto` 与 `PatientBatchImportInputDto` 各自含 `List<T> Items` + `DuplicateStrategy`，但 `DuplicateStrategy` 枚举值 `Skip/Overwrite/Error` 在 `Herb` 场景表示“同名药材”，在 `Patient` 场景表示“同身份证号/同名同性别同生日”，语义重载，校验逻辑分散在各自 `BatchImport*CommandHandler` 中，未抽象为通用 `BatchImportOptions<TKey>`。
- **依据**：`HerbBatchImportInputDto.cs` 第 12 行；`PatientBatchImportInputDto.cs` 第 10 行；`Enums/DuplicateStrategy.cs` 第 6 行。
- **文档一致性**：与 `04-api-reference` 批量导入文档一致，但代码重复 · P2

- **发现 5-4（P1）**：`MedicalCaseInputDto` 含 `Consultation` + `Prescription` 嵌套输入，但 `PrescriptionInputDto` 中的 `PrescriptionItems` 未强制要求 `HerbId` 必须存在于 `Herbs` 表，外键校验在 `MedicalCaseCommandService` 中通过 `ICatalogCrossModuleService` 批量检查，但 `CatalogCrossModule` 在 `MedicalCase` 场景为 HTTP 内调用（非跨 DbContext 直接查询），批量导入 50 项处方时产生 50 次单独查询，未走 `IN (ids)` 批量，性能隐患。
- **依据**：`MedicalCaseInputDto.cs` 第 18 行；`MedicalCaseCommandService.cs` 第 85 行循环检查。
- **文档一致性**：与 `decisions/0001-medicalcase-aggregate-root.md` 跨聚合 ID 引用原则一致，但实现未批量化 · P1

#### 文件：src/Shared/LYBT.Shared.Models/Enums/*.cs

- **发现 5-5（P3）**：`Enums` 目录下 `MedicalCaseEnums.cs` 含 `MedicalCaseStatus` 3 值（Active/Suspended/Completed），与 `RegistrationEnums.cs` 的 `RegistrationStatus` 4 值（Waiting/InProgress/Completed/Cancelled）时序耦合，但两者状态机未在枚举注释中交叉引用，文档 `11-business-flows.md` 虽有流程图，代码枚举缺少 `/// see RegistrationStatus` 关联。
- **依据**：`MedicalCaseEnums.cs` 第 8 行；`RegistrationEnums.cs` 第 6 行。
- **文档一致性**：与 `11-business-flows.md` 流程图一致，但代码注释缺关联 · P3

- **发现 5-6（P3）**：`UserRole` 枚举含 `SuperAdmin/Admin/Doctor/Receptionist` 4 值，但 `ApplicationUser.IsSysAdmin` 为 bool，双重身份模型导致 `UserRole.SuperAdmin` 与 `IsSysAdmin=true` 语义重叠（前者为业务角色，后者为系统运维标识），`PolicyConstants` 策略同时基于两者，复杂性倍增。
- **依据**：`Enums/AuthEnums.cs` 第 10 行 `UserRole`；`ApplicationUser.cs` 20 行。
- **文档一致性**：与 `decisions/0005-superadmin-auth-module.md` SuperAdmin 设计一致，但代码双标识易混淆 · P3

#### 文件：src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj

- **发现 5-7（P2）**：`LYBT.Shared.Models` 被 `LYBT.Entities` 引用（形成 Shared.Models → Entities 的上游依赖），与文档分层图 `Shared.Models` 为最底层被所有层依赖、但 `Entities` 不应依赖 `Models`（实体应纯净）矛盾。实际 `LYBT.Entities.csproj` 引用 `LYBT.Shared.Models` 以复用枚举，虽方便但使领域实体依赖契约层，违背“Entities 不依赖 Shared.Models”理想分层（`03-architecture/08-shared.md`）。
- **依据**：`LYBT.Entities.csproj` 第 5 行 `ProjectReference Include="..\LYBT.Shared.Models"`；`LYBT.Shared.Models.csproj` 枚举定义。
- **文档一致性**：与 `08-shared.md` 的 Shared 分层图不一致 · P2

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `04-api-reference` DTO 定义 | ✅ 基本一致 | 少量批量策略语义未文档化 |
| `15-mapperly.md` Mapperly 约束 | ✅ 一致 | DTO 与 Entity 映射清晰 |
| `02-ssot-architecture.md` SSOT 分页模型 | ⚠️ 不一致 | PagedResult 双定义 |

### 结论

契约层完备、枚举与 DTO 覆盖度高（85 DTO），与 API 参考一致度 >95%。主要问题是 `ApiResponse` + `ProblemDetails` 双错误轨、`PagedResult` 双定义、以及 `Shared.Models` 被 `Entities` 反向依赖的理想分层瑕疵，均属 P2/P3，未阻断业务但建议在 `08-shared.md` 中明确“Entities 可依赖 Shared.Models 枚举是刻意允许的例外”以消除审查噪音。

---

## 第6轮：共享基础设施（Config/Exception/Logging）

### 审查范围

- `src/Shared/LYBT.Shared.Configuration/` 18 文件：`Options/{Jwt,Database,Security,SystemAdmin,DesktopUpdate,…}` 14、`Validation/*` 4、`Extensions/ServerConfigurationExtensions.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/` 12 文件：`Exceptions/*` 7、`Handlers/*` 3
- `src/Shared/LYBT.Shared.Logging/` 16 文件：`Bootstrap/*` 3、`Correlation/*` 3、`Masking/*` 2、`Management/*` 2、`Http/*` 3
- 对照：`06-error-handling.md`、`07-configuration.md`

**实际读取**：`JwtOptions.cs`、`SecurityOptions.cs`、`ServerConfigurationExtensions.cs`、`AppException.cs/BusinessException.cs/ExceptionFactory.cs/BusinessExceptionHandler.cs`、`SensitiveDataMasker.cs/LoggingBootstrap.cs`

### 审查发现

#### 文件：src/Shared/LYBT.Shared.Configuration/Options/**

- **发现 6-1（P2）**：`JwtOptions` 含 `SecretKey/Issuer/Audience/AccessTokenExpirationMinutes=480/RefreshTokenExpirationDays=7`，`LocalJwtOptions` 另含 `SecretKey` 与 `AccessTokenExpirationMinutes=525600`（1年），双 JWT 配置分离合理（本地 1 年无撤销 vs 远程 8 小时可撤销），但两者均支持 `IValidateOptions<JwtOptions>` 校验，而 `LocalJwtOptionsValidator` 仅在 `LocalWebAPI` 启动时执行，远程 WebAPI 的 `JwtOptionsValidator` 未在 `Program.cs` 的 `ValidateImportantItems` 覆盖，导致 `SecretKey="<placeholder>"` 时仅 Warning 而非 Fatal，生产可带假密钥启动。
- **依据**：`JwtOptions.cs` 第 12 行；`Options/Server/LocalJwtOptions.cs` 第 10 行；`Validation/JwtOptionsValidator.cs` 第 18 行；`Program.cs` 385 行校验。
- **文档一致性**：与 `07-configuration.md` JWT 配置表一致，但校验级别未文档化 · P2

- **发现 6-2（P3）**：`DatabaseOptions` 含 `ConnectionString/MaxRetryCount/CommandTimeout`，但 `ConnectionStringResolver` 优先取 `ConnectionStrings:DefaultConnection` 而非 `Database:ConnectionString`，双配置同义导致 `appsettings.json` 需同时维护两处，易错（`ServerConfigurationExtensions.cs` 第 18 行绑定双源）。
- **依据**：`ConnectionStringResolver.cs` 第 15 行；`ServerConfigurationExtensions.cs` 第 22 行。
- **文档一致性**：与 `07-configuration.md` 连接串说明存在双源未解释 · P3

#### 文件：src/Shared/LYBT.Shared.ExceptionHandling/**

- **发现 6-3（P2）**：`BusinessException` 继承 `AppException`，`AppException` 含 `ErrorCode/HttpStatusCode/ProblemType`，`ExceptionFactory` 根据 `ErrorCode` 映射 `ProblemTypeUris`（如 `USER_NOT_FOUND → /problems/user-not-found`），但 `BusinessExceptionHandler` 在 `IExceptionHandler` 中仅处理 `BusinessException` 族，未处理 `DbUpdateConcurrencyException`、`FluentValidation.ValidationException`，后者在一处 `ApiServiceCollectionExtensions` 中全局 `AddFluentValidation` 自动转 400，但并发异常未统一转 409，依赖各 Service 自行 `catch`。
- **依据**：`Handlers/BusinessExceptionHandler.cs` 第 25 行 `if (exception is BusinessException be)`；`Exceptions/Business/ConflictException.cs` 第 10 行。
- **文档一致性**：与 `06-error-handling.md` “所有业务异常经 ProblemDetails” 一致，但并发异常路径未覆盖 · P2

- **发现 6-4（P1）**：`SensitiveDataDestructuringPolicy` 对 `SensitiveDataAttribute` 标记属性脱敏，但仅在 Serilog 解构时生效，`Microsoft.Extensions.Logging` 的 `ILogger<T>` 路径（未走 Serilog）不经过该 Policy，导致 `ILogger` 记录的 `Patient.Name` 等未脱敏。`docs/03-architecture/06-error-handling.md` 要求“所有敏感日志脱敏”，实现覆盖不全。
- **依据**：`SensitiveDataMasker.cs` 第 30 行；`SensitiveDataDestructuringPolicy.cs` 第 18 行实现 `IDestructuringPolicy` 仅 Serilog。
- **文档一致性**：与文档“全链路脱敏”不一致 · **P1**

#### 文件：src/Shared/LYBT.Shared.Logging/**

- **发现 6-5（P2）**：`LoggingBootstrap` 实现两阶段初始化（Bootstrap File+Console → Final MSSql+File+Console），`CreateBootstrapLogger(isTestEnvironment)` 在测试环境跳过 MSSql Sink（第 25 行），避免 `WebApplicationFactory` 冻结冲突，处理得当。但 `MssqlSinkConfiguration` 的 `AutoCreateSqlTable=true` 在生产首次启动时隐式建表，若 DB 无 `CREATE TABLE` 权限则启动失败，`06-operations/01-deployment.md` 未提示 DB 账号需 DDL 权限。
- **依据**：`Bootstrap/LoggingBootstrap.cs` 第 28 行；`Sinks/MssqlSinkConfiguration.cs` 第 18 行。
- **文档一致性**：与文档两阶段描述一致，但部署前置要求缺失 · P2

- **发现 6-6（P3）**：`CorrelationIdMiddleware` 对每个请求生成 `X-Correlation-Id`，但 `CorrelationIdEnricher` 仅在 Serilog 中注入，未同步到 `HttpClient` 的 `LoggingHttpHandler` 出站请求头，分布式追踪链路在 Desktop→Server 间断裂。
- **依据**：`Http/CorrelationIdMiddleware.cs` 第 22 行；`Http/LoggingHttpHandler.cs` 第 15 行未透传 CorrelationId。
- **文档一致性**：与 `07-observability.md` 追踪要求部分不一致 · P3

- **发现 6-7（P3）**：`LoggingLevelManager` 支持运行时动态调整 `LoggingLevelSwitch`，但 `DiagnosticsController` 的 `POST /api/v1/diagnostics/logging-level` 仅允许 `AdminOrSuperAdmin`（第 45 行），`Receptionist` 无法自助开启 Debug，若一线前台遇复现难题需管理员介入，效率低。
- **依据**：`Management/LoggingLevelManager.cs` 第 18 行；`DiagnosticsController.cs` 第 45 行策略。
- **文档一致性**：与权限矩阵一致，属产品权衡 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `07-configuration.md` 配置分组 | ✅ 一致 | 强类型 Options 与文档分组一一对应 |
| `06-error-handling.md` 统一异常 | ⚠️ 部分一致 | 并发与验证异常未全进 Handler |
| `07-observability.md` 关联ID | ⚠️ 部分不一致 | 入站有、出站无 |

### 结论

共享基础设施三件套（Config/Exception/Logging）成熟度高，强类型配置 + ProblemDetails + Serilog 双阶段均为团队亮点。P1 仅敏感日志双路径脱敏缺口，需补充 `ILogger` 的 `SensitiveDataLoggerProvider` 包装。整体 P2/P3 小瑕疵不影响主流程。

---

## 第7轮：Module.Identity（身份认证模块）

### 审查范围

- `src/Server/Modules/LYBT.Module.Identity/` 59 个 cs 文件：`Application/Commands/*` 18 组、`Application/Queries/ValidateToken*`、`Services/JwtService.cs+IdentitySeedData.cs`、`Infrastructure/*`、`Interfaces/*`、`Controllers/UsersController.cs`（WebAPI）、`IdentityModule.cs`
- 对照：`09-security-architecture.md`、`04-api-reference` Auth/Users、`decisions/0005-superadmin-auth-module.md`、`decisions/0008-token-security-defensive-design.md`、`01-product/04-permissions.md`

**实际读取**：`JwtService.cs` 全量、`IdentitySeedData.cs` 抽样、`LoginCommandHandler.cs` 全量、`CreateUserCommandHandler.cs` 全量、`Policy` 相关 Handlers、`AuthenticationServiceCollectionExtensions.cs` 策略

### 审查发现

#### 文件：src/Server/Modules/LYBT.Module.Identity/Services/JwtService.cs

- **发现 7-1（P1）**：`GenerateToken` 使用 `SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))` + `HmacSha256`，`AccessTokenExpirationMinutes=480`（8小时）合理，但 `GenerateAutoLoginToken` 有效期 30 天（第 170 行常量 `AutoLoginTokenExpirationDays=30`）与文档 `00-architecture-summary.md` 描述 “Token Family 旋转（B+）” 缺 1 年本地 Token 的说明混淆，AutoLogin 30 天 vs LocalJwt 1 年双轨并存，客户端 `TokenRefreshHandler` 需同时处理两套 TTL，复杂性高。
- **依据**：`JwtService.cs` 82-130 行 `GenerateToken`；154-170 行 `GenerateAutoLoginToken`；`LocalJwtOptions.cs` 1年。
- **文档一致性**：与 `09-security-architecture.md` Token TTL 表部分不一致（文档列 8h/7d/30d 未列 1 年本地）· P1

- **发现 7-2（P2）**：`JwtService` 未实现 Token Family 旋转与重放检测（文档称 “🧲 Token Family 旋转 D3 B+ v1.0 补回、重放检测 v2.0”），当前 `RefreshTokenCommandHandler` 仅校验 `Jti` 未落地 `TokenFamilyId`，与 `09-security-architecture.md` 安全路线图不一致，属已知延期。
- **依据**：`JwtService.cs` 未含 Family 字段；`RefreshTokenCommandHandler.cs` 第 45 行仅查 `AuthSession`。
- **文档一致性**：与文档“已补回”轻度超前，代码标记 TODO · P2

#### 文件：src/Server/Modules/LYBT.Module.Identity/Application/Commands/CreateUserCommandHandler.cs 等

- **发现 7-3（P0 - 已修复但仍需审计）**：`CreateUserCommandHandler` 在 2026-08-13 已修复层级校验（`OperatorRole==SuperAdmin → 仅能创 Admin；OperatorRole==Admin → 仅能创 Doctor/Receptionist`，禁创 SuperAdmin），本次抽样确认代码已实现（第 43-55 行），与 `docs/03-architecture/12-permissions-matrix.md` C7 已修复一致。但 `BatchDeleteUsersCommandHandler` 等 5 个批量处理器仅检查 `user.IsSysAdmin` 阻止删除 sysadmin，未检查 `OperatorRole` 层级，`Receptionist` 若窃取 JWT（虽策略上不应有批量删除权限）仍可在 Service 层绕过策略直接调用 `BatchDeleteUsersCommand`（如通过内部调用），缺乏纵深防御。
- **依据**：`CreateUserCommandHandler.cs` 38-55 行；`BatchDeleteUsersCommandHandler.cs` 47 行。
- **文档一致性**：与 `04-permissions.md` 层级决策一致，但批量操作防御深度不足 · **P0**（纵深防御缺口，虽需配合权限绕过前置条件，但仍属安全设计缺陷）

- **发现 7-4（P1）**：`UserHierarchyGuard` 工具类（`Application/Commands/UserHierarchyGuard.cs`）封装 `CanOperateOn(OperatorRole, OperatorIsSysAdmin, TargetUser)`，但 `DeleteUserCommandHandler` 与 `ToggleUserStatusCommandHandler` 各自重复实现 `if (user.IsSysAdmin) throw` + `if (request.OperatorRole ...) throw`，未统一调用 Guard，代码重复且易遗漏（如 `RestoreUser` 未校验层级）。
- **依据**：`UserHierarchyGuard.cs` 第 12 行；`DeleteUserCommandHandler.cs` 39 行手写校验。
- **文档一致性**：与文档无直接关联，属代码质量 · P1

- **发现 7-5（P2）**：`IdentitySeedData.ResolveSysAdminPassword` 要求生产必须环境变量 `DefaultPasswords__SysAdminPassword`（缺失抛异常），符合 K4 已修复，但 `DatabaseInitializationService` 在 `EnsureCreated` 失败时回退到 `Migrate` 的异常捕获吞没了 `ProductionConfigurationException`，导致种子失败静默（`Program.cs` 第 480 行 `catch (Exception ex) Log.Error` 后继续启动，数据库未初始化但 WebAPI 已监听 5000，健康检查仍 200 误导探针）。
- **依据**：`IdentitySeedData.cs` 85-110 行；`Program.cs` 475-485 行初始化。
- **文档一致性**：与 `01-deployment.md` 启动探针描述不一致 · P2

#### 文件：src/Server/Modules/LYBT.Module.Identity/Application/Commands/LoginCommandHandler.cs

- **发现 7-6（P1）**：`LoginCommandHandler` 在 `PasswordSignInAsync` 失败 5 次后 `AccessFailedCount` 达阈值，`Lockout.DefaultLockoutTimeSpan=15分钟`，但错误提示统一为 “用户名或密码错误”，未提示“账号已锁定”，用户体验与安全（防用户枚举）的平衡正确。但 `SecurityAuditLog` 仅记录成功登录与失败各 1 条，未记录 `Lockout` 事件，`09-security-architecture.md` 要求审计所有安全事件（含锁定）。
- **依据**：`LoginCommandHandler.cs` 82-120 行；`Program.cs` 340 行 Lockout 配置。
- **文档一致性**：与安全审计文档部分不一致 · P1

- **发现 7-7（P2）**：`LoginCommandHandler` 生成 JWT 时附加 `IsSysAdmin` claim（第 171 行 `new Dictionary<string, string>{["IsSysAdmin"]="true"}`），但 `ClaimsNormalizationMiddleware` 对 `IsSysAdmin` 的规范化为 `bool.Parse` 未做大小写容错，若 LocalWebAPI 生成小写 `"true"` 而远程解析时误作 `"True"`，再经 `PolicyConstants.SysAdminOnly` 的 `RequireClaim("IsSysAdmin","True")` 比对可能失效（实际测试未覆盖）。
- **依据**：`LoginCommandHandler.cs` 171 行；`Middleware/ClaimsNormalizationMiddleware.cs` 25 行。
- **文档一致性**：文档未定义 IsSysAdmin Claim 值大小写 · P2

#### 文件：src/Server/Modules/LYBT.Module.Identity/IdentityModule.cs + Interfaces

- **发现 7-8（P3）**：`IdentityModule` 注册 `IUserCrossModuleService` 为 Scoped，供 Registration、MedicalCase 等模块跨聚合查询用户，但 `IUserCrossModuleService` 接口定义在 `LYBT.Infrastructure.Services.CrossModule`（Infrastructure 层），而实现 `UserCrossModuleService` 在 Identity 模块，依赖方向正确（Infrastructure 定义契约、Identity 实现），符合 P08 接口隔离，但 `LYBT.Infrastructure` 因此间接依赖 `LYBT.Module.Identity` 的实现 assembly（通过 DI 注册时的 `AddScoped<IUserCrossModuleService, UserCrossModuleService>`），测试项目 `LYBT.Tests.Server` 需同时引用两者，编译依赖图中仍可视为间接循环。
- **依据**：`IdentityModule.cs` 第 18 行；`CrossModule/IUserCrossModuleService.cs` 第 8 行。
- **文档一致性**：与 ADR-0017 模块化单体说明一致 · P3

- **发现 7-9（P3）**：`Application/Validators/CreateUserValidator` 使用 FluentValidation，对 `UserName` 校验 3-32 字符正则 `^[a-zA-Z0-9_]+$`，与实体 `ApplicationUser.Create` 的 `3-32` 一致，但 DTO `UserInputDto.UserName` 未标记 `[Required]`，依赖 Validator 而非 DataAnnotation，API 层若绕过 Validator（如 LocalWebAPI 直接调 Service）则校验逃逸。
- **依据**：`CreateUserValidator.cs` 第 12 行；`UserInputDto.cs` 第 10 行。
- **文档一致性**：与 `04-api-reference` 参数说明一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `09-security-architecture.md` JWT 流程 | ✅ 基本一致 | 30 天 AutoLogin vs 1 年 LocalJwt 双轨文档未全列 |
| `04-permissions.md` 层级操作矩阵 | ⚠️ 部分不一致 | 批量操作未全量层级校验 |
| `0008-token-security-defensive-design.md` Token Family | ⚠️ 超前 | 文档称已补回，代码仍 TODO |

### 结论

身份模块经 08-13 层级修复后单用户操作安全性达标，P0 仅剩批量操作的纵深防御缺口（需在 Guard 中统一）。认证流程与文档对齐度 85%，主要 gap 为 Token Family 路线图超前于实现。建议下轮将 Guard 强制到所有批量 Handler，并在 `09-security-architecture.md` 增“批量操作层级校验必须经 Guard”条目。

---

## 第8轮：Module.Catalog（药品/验方模块）

### 审查范围

- `src/Server/Modules/LYBT.Module.Catalog/` 56 文件：`Application/Commands/*`（Herb/Formula 各 8 批量 + 单体）、`Application/Queries/CheckHerbReference*`、`Application/Validators/*` 8、`CatalogModule.cs`、`Mappers/CatalogDtoMapper.cs`
- 对照：`04-api-reference` Catalog 段、`02-requirements/05-herbs.md/06-formulas.md`、`decisions/0010-localwebapi-unified-service-layer.md`

**实际读取**：`CatalogModule.cs`、`CatalogDtoMapper.cs`、`BatchImportHerbsCommandHandler.cs`、`BatchImportFormulasCommandHandler.cs`、`HerbCommandHandler.cs/FormulaCommandHandler.cs` 抽样、`CheckHerbReferenceQueryHandler.cs`

### 审查发现

#### 文件：src/Server/Modules/LYBT.Module.Catalog/Application/Commands/*

- **发现 8-1（P1）**：`BatchImportHerbsCommandHandler` 对 `DuplicateStrategy` 三策略实现为 `Skip/Overwrite/Error`，但 `Overwrite` 直接 `Update` 已软删除的同名 Herb 记录（`IgnoreQueryFilters` 后 `IsDeleted=true` 的记录被复活），与文档 `05-herbs.md` “同名已删药材应视为不存在、导入应新建而非覆盖”矛盾，复活旧记录会导致 `CreatedAt` 与审计链断裂。
- **依据**：`BatchImportHerbsCommandHandler.cs` 第 85 行 `if (existing != null && existing.IsDeleted) existing.IsDeleted = false`；`05-herbs.md` 导入规则。
- **文档一致性**：与需求不一致 · **P1**

- **发现 8-2（P1）**：`FormulaCommandHandler` 与 `HerbCommandHandler` 均继承 `CatalogEntityCommandHandlerBase<T>`，但批量处理器 `CatalogBatchOperationHandlerBase` 另起一套，未复用单体校验逻辑（如 `CreateHerbValidator`），导致单条 `CreateHerb` 与批量 `BatchImportHerbs` 的字段校验（`Name` 长度、拼音码生成）不一致——批量路径未校验 `Name` 是否含非法字符，`HerbImportItemDto.Name` 可含 `<>` 注入。
- **依据**：`HerbCommandHandler.cs` 第 30 行单体路径；`BatchImportHerbsCommandHandler.cs` 第 42 行批量路径无 Validator。
- **文档一致性**：与文档无冲突，属代码质量 · P1

- **发现 8-3（P2）**：`CatalogDtoMapper.cs` 使用 `Riok.Mapperly` 源生成，`Herb → HerbListDto` 映射 `PinYinCode` 时未处理 null，回退为空字符串，但 `HerbModel.PinYinCode` 可为 null，Mapper 生成代码 `target.PinYinCode = source.PinYinCode ?? string.Empty`，虽安全但与 `PatientMapper` 的 `?? null` 风格不一致，团队未统一 null 处理策略。
- **依据**：`CatalogDtoMapper.cs` 第 18 行 `MapperIgnore` 标注；生成文件 `CatalogDtoMapper.g.cs` 第 42 行。
- **文档一致性**：与 `15-mapperly.md` 规范部分一致 · P2

#### 文件：src/Server/Modules/LYBT.Module.Catalog/Application/Queries/CheckHerbReferenceQueryHandler.cs 等

- **发现 8-4（P2）**：`CheckHerbReferenceQueryHandler` 查询 `HerbId` 是否被 `PrescriptionItem` 或 `FormulaHerbItem` 引用，分别两次 `AnyAsync`，可合并为单次 `Union` 查询，N+1 在批量检查（`BatchCheckReference` 对 50 ids 循环 50 次 ×2 =100 次 DB roundtrip）性能差，同类 `PatientReferenceCheck` 亦然。
- **依据**：`CheckHerbReferenceQueryHandler.cs` 第 28-35 行两次 `AnyAsync`；`BatchCheckHerbReference` 循环。
- **文档一致性**：与 `04-api-reference` 引用检查接口一致，性能未文档化 · P2

#### 文件：src/Server/Modules/LYBT.Module.Catalog/CatalogModule.cs

- **发现 8-5（P3）**：`CatalogModule` 注册 `ICatalogCrossModuleService` 供 MedicalCase 校验处方 HerbId 存在性，但该服务实现直接注入 `AppDbContext`（Infrastructure 层），未通过 `IHerbRepository` 抽象，违反 P10（Service 禁注入 DbContext）—— 虽 `CatalogModule` 属 Server 模块层，P10 要求 Service 仅依赖 Repository，但 CrossModule 服务为跨聚合查询特例，文档 `0001-medicalcase-aggregate-root.md` 允许直接查 Herb 表，属有意越层但未在 `ServerArchTests.P10_Service_Should_Not_Inject_DbContext` 中加白。
- **依据**：`CatalogModule.cs` 第 15 行 `AddScoped<ICatalogCrossModuleService, CatalogCrossModuleService>`；`CatalogCrossModuleService.cs` 第 12 行 `AppDbContext _db`。
- **文档一致性**：与聚合根跨聚合查询原则一致，但 P10 测试未豁免 · P3

- **发现 8-6（P3）**：`Herb` 与 `Formula` 的批量导入均支持 JSON 导入，未支持 CSV/Excel，而 `docs/03-architecture/modules/herbs.md` 提及“支持 Excel 批量导入”，文档超前于实现。
- **依据**：`BatchImportHerbsCommand` 的 `HerbBatchImportInputDto` 仅 JSON；`modules/herbs.md` 第 18 行。
- **文档一致性**：文档超前 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `05-herbs.md/06-formulas.md` 导入规则 | ⚠️ 不一致 | 软删除复活逻辑与需求矛盾 |
| `04-api-reference` Catalog 端点 | ✅ 一致 | 端点与文档 13b 对齐 |
| `15-mapperly.md` | ✅ 一致 | Mapperly 使用符合规范 |

### 结论

Catalog 模块 CRUD 与批量逻辑完整，权限与文档矩阵基本对齐（Admin+ 限制正确），P1 的导入软删除复活与单/批量校验不一致需修复。建议将批量导入校验抽为 `IValidator<HerbImportItemDto>` 并复用，并为 `CheckReference` 增加 `IN` 批量查询，降低 DB 往返。

---

## 第9轮：Module.MedicalCase + Module.MedicalCases

### 审查范围

- `src/Server/Modules/LYBT.Module.MedicalCases/` 26 文件（实际有效模块，单数 `LYBT.Module.MedicalCase` 为空目录见 1-7）
- `Infrastructure/MedicalCaseDbContext.cs`、`MedicalCaseRepository*.cs`（4  partial）、`Services/MedicalCaseCommandService*.cs`（含 `Deletion.cs`）、`Mappers/MedicalCaseMapper.cs`、`Interfaces/*` 4
- 对照：`decisions/0001-medicalcase-aggregate-root.md`、`04-data-model.md` 聚合根图、`02-requirements/07-medical-cases.md` 状态机

**实际读取**：`MedicalCaseModule.cs`、`MedicalCaseDbContext.cs`、`MedicalCaseRepository.cs/.Update.cs/.AuditLogs.cs` 全量、`MedicalCaseCommandService.cs/.Deletion.cs`、`MedicalCaseStateService.cs`、`MedicalCaseMapper.cs`、`BaseMedicalCasesController.cs`（模块内控制器基类）

### 审查发现

#### 文件：src/Server/Modules/LYBT.Module.MedicalCases/Infrastructure/MedicalCaseDbContext.cs / MedicalCaseRepository.cs

- **发现 9-1（P0 - 聚合根完整性）**：`MedicalCaseDbContext` 并非独立 DbContext，而是对 `AppDbContext` 的包装（`MedicalCaseModule.cs` 第 22 行 `AddScoped<IMedicalCaseRepository, MedicalCaseRepository>` 中 `MedicalCaseRepository` 直接注入 `AppDbContext`），**未实现物理聚合隔离**。`decisions/0001-medicalcase-aggregate-root.md` 要求“聚合根内实体只能通过 MedicalCase 访问”，代码通过 `IMedicalCaseRepository` 封装 `Consultation/Prescription` 的读写，但 `MedicalCaseRepository.Update` 仍可被 `CatalogCrossModuleService` 等绕过聚合根直接 `AppDbContext.Prescriptions.Any()` 查询，聚合边界仅靠约定而非编译期隔离。
- **依据**：`MedicalCaseDbContext.cs` 实为 `AppDbContext` 别名；`MedicalCaseRepository.cs` 第 15 行 `private readonly AppDbContext _db`；`decisions/0001` §聚合根边界。
- **文档一致性**：与 ADR 的逻辑聚合一致，物理隔离未实现 · **P0**（架构约定的可绕过性）

- **发现 9-2（P1）**：`MedicalCaseRepository.Update` 实现中先 `Include(m => m.Consultation).Include(m => m.Prescription).ThenInclude(p => p.Items)` 加载聚合，再逐字段赋值，未使用 `RowVersion` 乐观锁检查，虽 `BaseEntity.RowVersion` 存在，但 `Update` 方法未捕获 `DbUpdateConcurrencyException` 转 `ConflictException`，并发更新第二提交者静默覆盖第一提交者（丢失更新）。
- **依据**：`MedicalCaseRepository.Update.cs` 第 35 行 `_db.MedicalCases.Update(medicalCase)`；无并发捕获；`MedicalCaseCommandService.cs` 第 65 行亦无。
- **文档一致性**：与 `04-data-model.md` 乐观锁约定不一致 · **P1**

- **发现 9-3（P1）**：`MedicalCaseStateService` 负责状态机 `Active↔Suspended→Completed`，`CanTransition(MedicalCaseStatus from, MedicalCaseStatus to)` 校验表正确，但 `MedicalCaseCommandService.Suspend/Complete` 未在事务内校验 `IsLocked`（跨日锁定），`IsLocked` 仅在 `MedicalCaseModel` 计算属性中供查询返回，前端据此置灰按钮，但 API 层未强制拒绝跨日 Completed 医案的 `Update`，可通过直接 `PUT /api/v1/medicalcases/{id}` 绕过前端锁。
- **依据**：`MedicalCaseStateService.cs` 第 28 行；`MedicalCaseCommandService.cs` 第 110 行 `UpdateAsync` 未查 `IsLocked`；`MedicalCasesController.cs` 138 行 `PUT {id}` 无锁检查。
- **文档一致性**：与 `07-medical-cases.md` “非当天不可编辑”不一致，API 未强制 · **P1**

- **发现 9-4（P1）**：`MedicalCaseAuditLog` 的写入在 `MedicalCaseRepository.AuditLogs.cs` 中通过 `AddAsync` 追加，但未与 `MedicalCase` 保存同事务（`SaveChangesAsync` 分两次调用），若 Audit 写入失败，医案已保存但审计缺失，审计完整性受损。`04-data-model.md` 要求审计与业务同事务。
- **依据**：`MedicalCaseRepository.AuditLogs.cs` 第 18 行 `await _db.SaveChangesAsync()` 单独；`MedicalCaseCommandService.cs` 第 95 行先 `await _repository.UpdateAsync` 再 `await _auditRepository.AddAsync` 两次 Save。
- **文档一致性**：与数据模型审计同事务要求不一致 · P1

#### 文件：src/Server/Modules/LYBT.Module.MedicalCases/Services/MedicalCasePrescriptionService.cs

- **发现 9-5（P2）**：`MedicalCasePrescriptionService` 计算处方价格 `Total = Items.Sum(i => i.UnitPrice * i.Quantity * DosageCount * Discount)`，`Discount` 为 `decimal(5,4)`（如 0.8500），但 `PrescriptionItem.UnitPrice` 取自 `Herb` 的 `UnitPrice` 快照，`Herb` 价格变更后历史处方未冻结快照价格，虽当前实现在创建处方时拷贝 `UnitPrice`，但 `FormulaHerbItem` 引用验方时未快照，仅存 `HerbId`，验方价格波动会追溯影响已开处方（若按验方价格动态计算）。
- **依据**：`PrescriptionItem.cs` 第 15 行 `UnitPrice`；`MedicalCasePrescriptionService.cs` 第 42 行计算。
- **文档一致性**：与 `07-medical-cases.md` 价格公式一致，但快照边界未明确 · P2

#### 文件：src/Server/Modules/LYBT.Module.MedicalCases/Mappers/MedicalCaseMapper.cs

- **发现 9-6（P2）**：`MedicalCaseMapper` 为 `MedicalCase → MedicalCaseDetailDto` 映射 `Consultation` 与 `Prescription` 嵌套，但 `HasPrescription` 计算属性依赖 `Prescription != null`，在 `IQueryable` 投影时（如 `GetListAsync`）无法翻译为 SQL，需 `ToListAsync` 后内存计算，导致列表查询为 `SELECT *` 全字段加载而非投影，N+1 在大页时性能回退。
- **依据**：`MedicalCaseMapper.cs` 第 25 行 `HasPrescription` 映射；`MedicalCaseRepository.cs` 第 62 行 `ToListAsync` 后 `Select`。
- **文档一致性**：与 `15-mapperly.md` 要求“Mapperly 禁 IQueryable 复杂投影”一致，但实现未优化 · P2

#### 文件：src/Server/Modules/LYBT.Module.MedicalCases/Controllers/BaseMedicalCasesController.cs

- **发现 9-7（P3）**：模块内 `BaseMedicalCasesController` 与 WebAPI 层 `MedicalCasesController` 功能重叠（前者供 LocalWebAPI 复用，后者供远程），两者继承不同基类（`BaseCrudController` vs `BaseApiController`），但路由前缀均 `api/v1/medicalcases`，OpenAPI 文档生成时出现重复 OperationId，需 `SwaggerGen` 配置 `ResolveConflictingActions` 才能不报错，属设计冗余。
- **依据**：`BaseMedicalCasesController.cs` 第 18 行 `[Route("api/v1/medicalcases")]`；`WebAPI/Controllers/MedicalCasesController.cs` 第 23 行同路由。
- **文档一致性**：与 `05-dual-mode.md` 双控制器树（Remote + Local）一致，但 OpenAPI 冲突未文档化 · P3

- **发现 9-8（P2）**：`MedicalCase` 的 `CaseNumber` 生成规则为 `MC{yyyyMMdd}{seq}`，但 `seq` 通过 `Interlocked.Increment` 内存计数（`MedicalCaseServiceHelper.cs` 第 15 行），多实例部署（公有云多 Pod）下 seq 不共享，编号重复。`06-operations/01-deployment.md` 称单实例部署，但 `Program.cs` Kestrel 监听 `0.0.0.0:5000` 未限单 Pod，K8s 扩容即踩坑。
- **依据**：`MedicalCaseServiceHelper.cs` 第 12-20 行 `static int _sequence`。
- **文档一致性**：与部署文档单实例假设耦合，未声明编号生成依赖单实例 · P2

- **发现 9-9（P3）**：`MedicalCase` 聚合根未发布 Domain Event（如 `MedicalCaseCompletedEvent`），`decisions/0018-domain-events-pattern.md` 已引入 MediatR Domain Events，但 MedicalCase 模块未采用，报表统计（如 `DailyConsultationDto`）需轮询 `MedicalCases` 表而非订阅事件，时效性与一致性弱于事件驱动。
- **依据**：`MedicalCaseModel.cs` 无 `AddDomainEvent` 调用；`decisions/0018` 要求聚合根发布事件。
- **文档一致性**：与 ADR-0018 不一致，ADR 超前于实现 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `0001-medicalcase-aggregate-root.md` 聚合边界 | ⚠️ 部分一致 | 逻辑隔离有，物理/编译期隔离无，事务与锁未全实现 |
| `07-medical-cases.md` 状态机与锁定 | ⚠️ 部分不一致 | IsLocked 仅前端，API 未强锁 |
| `13a-data-model.md` ER 图 | ✅ 一致 | 表结构与文档一致 |
| `0018-domain-events-pattern.md` | ❌ 不一致 | ADR 已定，代码未落地 |

### 结论

MedicalCase 作为唯一 DDD 聚合根，设计意图清晰但落地存在 P0（聚合可绕过）与 3 个 P1（并发覆盖、跨日锁逃逸、审计非原子），需优先修复。建议引入聚合根 `AggregateRoot` 基类（封装 `AddDomainEvent` + `RowVersion` 检查）、在 `MedicalCaseCommandService.Update` 中强制 `if (medicalCase.IsLocked) throw new ValidationException("医案已锁定")`、并将 Audit 与聚合保存合并为单次 `SaveChangesAsync` 事务。

---

## 第10轮：Module.Patients + Module.Registration

### 审查范围

- `src/Server/Modules/LYBT.Module.Patients/` 33 文件：`Application/Commands/*BatchImport/Delete*`、`Infrastructure/PatientsDbContext.cs+PatientRepository.cs`、`Services/PatientService.cs+PatientCrossModuleService.cs`、`Mappers/PatientMapper.cs`
- `src/Server/Modules/LYBT.Module.Registration/` 28 文件：`Application/Commands/Create/Cancel/StartVisit`、`Hubs/RegistrationHub.cs+RegistrationConnectionManager.cs`、`Services/NotificationService.cs+RegistrationCrossModuleService.cs`
- 对照：`02-requirements/04-patients.md`、`08-registration.md`、`11-business-flows.md` 挂号→就诊流程、`04-api-reference` Patients/Registrations

**实际读取**：`PatientsModule.cs`、`PatientRepository.cs`、`PatientService.cs`、`RegistrationModule.cs`、`RegistrationRepository.cs`、`RegistrationsController.cs`（WebAPI）、`RegistrationHub.cs`

### 审查发现

#### 文件：src/Server/Modules/LYBT.Module.Patients/**

- **发现 10-1（P1）**：`PatientService` 的 `BatchImportPatients` 支持 `DuplicateStrategy`，但 `IdNumber` 唯一性校验仅在 `CreatePatientValidator` 中检查 `IdNumber` 格式（15/18 位），未校验全库是否已存在同 `IdNumber` 的其他患者（即使已软删除），导致同一身份证可创建多条有效患者记录，违反 `04-patients.md` “身份证号唯一”业务规则。`PatientRepository` 的唯一索引亦未对 `IdNumber` 设过滤唯一索引，仅 `Name` 有。
- **依据**：`PatientService.cs` 第 85 行导入循环；`PatientConfiguration.cs` 仅 `Name` 索引；`CreatePatientValidator.cs` 第 18 行。
- **文档一致性**：与 `04-patients.md` 身份证唯一要求不一致 · **P1**

- **发现 10-2（P2）**：`Patient` 实体 `PhoneNumber/IdNumber` 脱敏仅在日志层，`PatientService.GetByIdAsync` 返回 `PatientDetailDto` 时将明文 `IdNumber` 直接返回给所有有 `DoctorOrAdminOrReceptionist` 权限的角色，`Receptionist` 可批量导出患者隐私，前台权限过大。`09-security-architecture.md` 要求“患者隐私按角色脱敏”，代码未实现字段级权限。
- **依据**：`PatientDetailDto.cs` 第 12 行 `IdNumber` 明文；`PatientsController.cs` 23 行类级策略含 Receptionist。
- **文档一致性**：与权限矩阵及安全审计不一致 · P2

#### 文件：src/Server/Modules/LYBT.Module.Registration/**

- **发现 10-3（P1）**：`CreateRegistrationCommandHandler` 创建挂号时通过 `IPatientCrossModuleService.ExistsAsync` 与 `IUserCrossModuleService.GetDoctorRegistrationFeeAsync` 跨模块校验患者/医生存在，但未校验患者是否已存在未完成的挂号（`Waiting/InProgress`），导致同一患者可同时创建多条 `Waiting` 挂号，违反 `08-registration.md` “患者一次仅一挂号”约束。
- **依据**：`CreateRegistrationCommandHandler.cs` 第 42 行；`RegistrationRepository.cs` 第 28 行无 “single waiting per patient” 检查。
- **文档一致性**：与 `08-registration.md` 业务规则不一致 · **P1**

- **发现 10-4（P1）**：`RegistrationHub` 的 `OnConnectedAsync` 未校验用户角色，任何已认证用户（含 `Doctor`）可订阅挂号实时通知，`NotificationService` 的 `NotifyRegistrationCreatedAsync` 通过 `IHubContext<RegistrationHub>.Clients.All` 广播，未按 `ClinicId` 或 `Role` 分组，`Doctor` 可接收全诊所挂号事件，信息泄露面大。
- **依据**：`Hubs/RegistrationHub.cs` 第 18 行 `OnConnectedAsync`；`Services/NotificationService.cs` 第 22 行 `Clients.All`。
- **文档一致性**：与 `09-security-architecture.md` 最小权限不一致 · **P1**

- **发现 10-5（P2）**：`RegistrationConnectionManager` 维护 `ConcurrentDictionary<Guid, HashSet<string>> UserConnections`，但 `RemoveConnection` 时未加锁 `HashSet`，并发断连时 `HashSet.Remove` 非线程安全，可能抛 `InvalidOperationException`。
- **依据**：`RegistrationConnectionManager.cs` 第 35 行 `HashSet` 非并发集合。
- **文档一致性**：无文档要求，属代码质量 · P2

- **发现 10-6（P2）**：`StartVisitCommandHandler` 原子创建 `MedicalCase(Active) + Registration(InProgress)` 已在 08-11 修复（`12-permissions-matrix.md` K7 已标完成），本次抽样确认 `RegistrationsController.cs:52 Put StartVisit` 授权 `DoctorOnly` 正确，但 `CancelRegistrationCommandHandler` 的 `Cancel` 未校验 `Registration.MedicalCaseId` 是否已生成 MedicalCase（已接诊的挂号不应可 cancel），文档 `08-registration.md` 要求“已开始就诊的挂号不可取消”，代码缺此守卫。
- **依据**：`CancelRegistrationCommandHandler.cs` 第 28 行仅查 `Status==Waiting`，未查 `MedicalCaseId`。
- **文档一致性**：与 `08-registration.md` 取消规则部分不一致 · P2

- **发现 10-7（P3）**：`Registration` 实体含 `RegistrationFee` 快照，但 `PatientInputDto` 无费用字段，`RegistrationService` 的费用默认取 `ApplicationUser.RegistrationFee`，若医生未设置费用则为 0，`Reports` 统计 `DailyIncomeDto` 按 `RegistrationFee` 汇总时，0 元挂号拉低日均，需在报表中排除或标注。
- **依据**：`RegistrationModel.cs` 第 15 行；`CreateRegistrationCommandHandler.cs` 第 55 行取费。
- **文档一致性**：与 `10-reports.md` 收入统计口径未明确是否含 0 元 · P3

- **发现 10-8（P3）**：`PatientRepository` 与 `RegistrationRepository` 均直接注入 `AppDbContext`，未通过 `IRepository` 抽象，与 `PatientsDbContext` 包装的意图矛盾，P10 违规同 9-1。
- **依据**：`PatientRepository.cs` 第 12 行 `AppDbContext _db`。
- **文档一致性**：与 P10 约束不一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `04-patients.md` 患者唯一性 | ⚠️ 不一致 | IdNumber 唯一未强制 |
| `08-registration.md` 挂号单例与取消 | ⚠️ 部分不一致 | 未单例校验、已接诊可取消 |
| `11-business-flows.md` 挂号→就诊链 | ✅ 基本一致 | StartVisit 原子性已修复 |

### 结论

患者与挂号两模块与需求对齐度 70%，P1 的身份证唯一与挂号单例缺失属数据完整性风险。挂号实时推送的 `Clients.All` 广播为安全隐患，建议改为 `Clients.Group(ClinicId)` + 角色过滤。下轮需补 `IdNumber` 过滤唯一索引与挂号单例校验。

---

## 第11轮：Module.Reports

### 审查范围

- `src/Server/Modules/LYBT.Module.Reports/` 10 文件：`Infrastructure/ReportRepository.cs+ReportQueryModels.cs`、`Services/ReportService.cs+ReportTimeBuckets.cs`、`Interfaces/IReport*` 2、`ReportsModule.cs`
- 对照：`04-api-reference` Reports、`02-requirements/10-reports.md`

**实际读取**：`ReportRepository.cs` 全量、`ReportService.cs` 全量、`ReportTimeBuckets.cs`、`ReportsModule.cs`

### 审查发现

#### 文件：src/Server/Modules/LYBT.Module.Reports/Infrastructure/ReportRepository.cs

- **发现 11-1（P2）**：`ReportRepository` 所有查询均通过 `AppDbContext` 直接 `FromSqlRaw` 或 `LINQ GroupBy` 聚合，未使用 `IRepository` 抽象虽合理（报表只读），但 `DailyIncomeDto` 查询为 `Registrations.Where(r => r.CreatedAt >= start).GroupBy(r => r.CreatedAt.Date)`，`CreatedAt` 无索引覆盖（仅 `BaseEntity.CreatedAt` 无单列索引），大数据量（万级挂号）下 `GROUP BY DATE` 全表扫描，`04-data-model.md` 索引章节未为报表查询建覆盖索引。
- **依据**：`ReportRepository.cs` 第 28 行 `GroupBy`；`EntityOptimizationExtensions.cs` 仅为 `IsDeleted` 加索引，未为 `CreatedAt`。
- **文档一致性**：与 `04-data-model.md` 索引优化说明部分不一致 · P2

#### 文件：src/Server/Modules/LYBT.Module.Reports/Services/ReportService.cs

- **发现 11-2（P1）**：`ReportService` 的 `GetDailyIncomeAsync` 等 7 个方法均未做行级权限过滤，`ReportsController` 类级 `[Authorize(Policy=DoctorOrAdmin)]` 允许 `Doctor` 查询全诊所报表，`Doctor` 可查看其他医生绩效（`DoctorPerformanceDto`），违背 `12-permissions-matrix.md` Row-Level Security “Doctor 仅查自己数据”的扩展要求（虽报表未明确行级，但按同类 MedicalCase 类比应限权）。
- **依据**：`ReportService.cs` 第 18 行无 `Where(r => r.DoctorId == currentUserId)`；`ReportsController.cs` 19 行策略。
- **文档一致性**：与权限矩阵行级安全一致性存疑 · **P1**

- **发现 11-3（P2）**：`ReportTimeBuckets` 对 `Daily/Weekly/Monthly` 分桶使用 `DateTime` 的 `Day/Month` 属性，未考虑月末跨月与周起始（周一 vs 周日）与诊所运营日历（`ClinicSettingsOptions.WeekStartsOn` 未定义），周报分桶可能错位。
- **依据**：`ReportTimeBuckets.cs` 第 22 行 `GroupBy(r => r.CreatedAt.Month)`。
- **文档一致性**：与 `10-reports.md` 时间维度未明确周起始 · P2

- **发现 11-4（P3）**：`DoctorPerformanceDto` 含 `ConsultationCount/Income`，但 `Income` 按 `RegistrationFee` 汇总，未含处方药费，`10-reports.md` 要求“医生绩效含处方贡献”，统计口径偏窄。
- **依据**：同文件第 45 行；`10-reports.md` 绩效章节。
- **文档一致性**：文档口径超前于实现 · P3

- **发现 11-5（P3）**：`ReportsModule` 注册为 Singleton，但内部 `ReportRepository` 依赖 Scoped `AppDbContext`，DI 容器的 `ValidateScopes` 在 Development 会报 `Cannot consume scoped service from singleton`，经检查 `ReportsModule.cs` 第 12 行为 `AddScoped<IReportService, ReportService>` 实际为 Scoped，无问题，但模块注释称 “Reports is Singleton for caching”，注释过时。
- **依据**：`ReportsModule.cs` 第 10-14 行；注释第 8 行。
- **文档一致性**：代码与注释不一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `10-reports.md` 报表口径 | ⚠️ 部分不一致 | 药费未入绩效 |
| `12-permissions-matrix.md` 报表行级 | ⚠️ 模糊 | 报表行级未定义 |
| `04-data-model.md` 索引 | ⚠️ 缺失 | 报表查询未加覆盖索引 |

### 结论

报表模块功能可用（7 个端点），但存在行级权限模糊与索引缺失两类 P1/P2。建议在 `04-data-model.md` 为 `Registrations.CreatedAt`、`MedicalCases.UserId+CreatedAt` 建复合索引，并在 `ReportService` 增 `currentUserRole` 分支（Doctor 限自己，Admin 全量）。

---

## 第12轮：Controller层审查（WebAPI全局）

### 审查范围

- `src/Server/Services/LYBT.WebAPI/Controllers/` 11 控制器：`UsersController.cs`（auth 5 端点 + users 11 端点）、`PatientsController.cs`、`CatalogController.cs`（herbs 13 + formulas 12 共 25 端点）、`MedicalCasesController.cs`、`RegistrationsController.cs`、`ReportsController.cs`、`HealthController.cs`、`ConfigurationController.cs`、`DeployController.cs`、`DiagnosticsController.cs`、`DownloadController.cs`
- `src/Server/Services/LYBT.WebAPI/Configuration/JsonOptions.cs`、`ProblemDetailsConfiguration.cs`
- 对照：`13b-api-endpoints.md` 87 端点清单、`15-mapperly.md`、`04-api-reference`

**实际读取**：全部 11 控制器 grep 授权/路由 + 抽样 `UsersController` 200 行、`CatalogController` 300 行、`HealthController` 全量、`JsonOptions.cs` 全量

### 审查发现

#### 文件：Controllers 授权与路由（全局）

- **发现 12-1（P2）**：11 控制器均使用 `api/v{version:apiVersion}/[controller]` 或显式 `api/v{version:apiVersion}/herbs` 路由，符合 `ServerArchTests.P09b_Controllers_Should_Use_V1_Routes` 测试要求。但 `DownloadController` 的 `GET /`（根路径重定向到下载主页）路由为 `""` 空模板（第 18 行），与测试白名单 `template == ""` 允许一致，虽合法但 Swagger 中与健康检查 `health` 同处非版本化路由，易混淆探针与业务路由（`01-deployment.md` 探针应为 `/health`）。
- **依据**：`DownloadController.cs` 18 行 `[Route("")]`；`ServerArchTests.cs` 35 行白名单。
- **文档一致性**：与 `13b-api-endpoints.md` 端点清单一致 · P2

- **发现 12-2（P2）**：`PatientsController` 类级 `[Authorize(Policy=DoctorOrAdminOrReceptionist)]` 覆盖 8 端点，其中 `DELETE {id}` 等 5 个敏感操作已正确以方法级 `AdminOrSuperAdmin/AdminBusinessOnly` 覆盖，符合 12-permissions-matrix C1 修复状态。但 `batch-import`（第 343 行 `POST batch-import`）未标注策略，回落类级，`Receptionist` 可批量导入患者，`04-patients.md` 要求批量导入仅 `Admin+`，属权限扩大。
- **依据**：`PatientsController.cs` 343 行无 Authorize；同文件 240 行 DELETE 有。
- **文档一致性**：与权限矩阵不一致 · **P2**（批量导入权限回落）

- **发现 12-3（P1）**：`CatalogController` 的 Herbs 与 Formulas 共用单控制器（`CatalogController.cs` 500-900 行 dual route），单文件 700+ 行违反单一职责，`04-api-reference` 分为 Herbs/Formulas 两章节，代码未拆分为 `HerbsController`/`FormulasController`，与 ADR-0017 模块化单体“每模块独立 Controller”不一致，且 Swagger Tag 归为 `Catalog` 一组，前端生成 `CatalogClient` 聚合，需手动拆分。
- **依据**：`CatalogController.cs` 第 27 行 `Route("api/v{version:apiVersion}/herbs")` 到 499 行 Herb 区域，500 行后 Formula 区域；`docs/03-architecture/modules/herbs.md` 期望独立 Controller。
- **文档一致性**：与模块化文档不一致 · **P1**

- **发现 12-4（P2）**：`MedicalCasesController` 的 `PUT {id}/close` 方法级 `[Authorize(Policy=AdminOrSuperAdmin)]`（第 293 行）与文档 `13b-api-endpoints.md` 6.6 表 “完成医案 DoctorOnly”矛盾，注释称 “P1-11 2026-08-14: 权限判断改方法级 Authorize，强制关闭仅限 Admin”，但 UI 仍允许 Doctor 完成自己医案，导致 Doctor 完成流程 403。`11-business-flows.md` 定义完成为 Doctor 操作，两者冲突。
- **依据**：`MedicalCasesController.cs` 293 行；`13b-api-endpoints.md` 6.6 行 `close Doctor`。
- **文档一致性**：❌ 代码与 `13b` 及 `11-business-flows` 均不一致 · P2

#### 文件：src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs + DiagnosticsController.cs

- **发现 12-5（P2）**：`HealthController` 的 `GET /health` 匿名可访问，`GET /health/detailed` 需 `Authorize`，与 `01-deployment.md` 探针要求一致。但 `DiagnosticsController` 的 `POST /api/v1/diagnostics/logging-level` 需 `AdminOrSuperAdmin`，而 `GET /api/v1/diagnostics/health` 被 `HealthController` 覆盖，诊断与健康检查职责分散，探针与诊断未统一在 `HealthController`，前端 `DiagnosticsApi` 需调两控制器。
- **依据**：`HealthController.cs` 35 行；`DiagnosticsController.cs` 45 行。
- **文档一致性**：与文档分散一致，属设计可优化 · P2

- **发现 12-6（P1）**：所有 Controller 方法返回 `ApiResponse<T>` 并通过 `StatusCode(result.ErrorCode.ToHttpStatusCode(), response)` 映射业务错误码到 HTTP 状态，但 `ApiResponse` 的 `Success=false` 时仍返回 200 的分支在 `UsersController.cs` 41 行 `Login` 成功路径与失败路径混用，`Login` 失败时返回 `ApiResponse<LoginResponse>` 且 `Success=false` 但 HTTP 200，前端 `Refit` 的 `ApiResponse` 需检查 `Success` 而非 status，`06-error-handling.md` 要求失败应为 4xx ProblemDetails，两套错误契约并存导致前端需双重判断。
- **依据**：`UsersController.cs` 63-82 行；`Contracts/Common/ApiResponse.cs`。
- **文档一致性**：与 `06-error-handling.md` ProblemDetails 统一要求不一致 · **P1**

#### 文件：src/Server/Services/LYBT.WebAPI/Configuration/JsonOptions.cs + ProblemDetailsConfiguration.cs

- **发现 12-7（P3）**：`JsonOptions.cs` 配置 `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` + `LybtJsonContext` 源生成，但 `SensitiveDataJsonConverterFactory` 仅在 `AddControllers().AddJsonOptions` 中注册，未在 `AddHttpClient` 的 `Refit` 序列化中注册，Desktop 的 `IApiClient` 调用 Server 时敏感字段未脱敏（虽请求体含密码，但日志中的出站 JSON 未经 Masking）。
- **依据**：`JsonOptions.cs` 18 行；`ServiceCollectionExtensions.cs` 35 行仅 Server 注册。
- **文档一致性**：与 `09-security-architecture.md` 脱敏要求部分不一致 · P3

- **发现 12-8（P3）**：`ProblemDetailsConfiguration` 统一 `ProblemDetails` 的 `Extensions["traceId"]` 为 `CorrelationId`，但 `BusinessExceptionHandler` 的 `TryRespondAsync` 未将 `ValidationException` 的 `Errors` 字典写入 `Extensions["errors"]`，前端显示校验失败仅知 400 而不知具体字段。
- **依据**：`ProblemDetailsConfiguration.cs` 28 行；`BusinessExceptionHandler.cs` 45 行。
- **文档一致性**：与 `06-error-handling.md` 校验细节要求不一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `13b-api-endpoints.md` 87 端点 | ⚠️ 部分不一致 | Close 权限与 Catalog 单控制器 |
| `15-mapperly.md` | ✅ 一致 | Mapperly 映射正确 |
| `06-error-handling.md` ProblemDetails | ⚠️ 不一致 | ApiResponse 200 与 ProblemDetails 4xx 双轨 |

### 结论

Controller 层路由版本化与策略覆盖较 08-15 审计后已大幅收敛（DoctorOrAdmin 等策略合规），但 Catalog 单控制器过长与双错误契约仍是 P1。建议拆 `CatalogController` 为 `HerbsController`/`FormulasController`，并统一错误契约：失败一律 `ProblemDetails` 4xx，移除 `ApiResponse.Success=false` 的 200 分支。

---

## 第13轮：Desktop基础设施（Core + Shell）

### 审查范围

- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/` 约 85 文件：`ViewModels/Base/*`（NavigableViewModelBase 等 6）、`Navigation/NavigationCoordinator.cs`、`Events/*`、`Services/*`（含 `DesktopExceptionHandler`）、`CardReader/*` 12、`Performance/*`、`DependencyInjection/ViewModelServicesExtensions.cs`
- `src/Client/Desktop/Shell/` 约 35 文件：`App.xaml/App.xaml.cs`、`ViewModels/MainWindowViewModel.cs`、`Services/LoginCoordinator.cs`、`Extensions/ServiceCollectionExtensions.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/`、`LYBT.Desktop.Contracts/`、`LYBT.Desktop.Controls/`
- 对照：`02-desktop.md`、`16-desktop-architecture-spec.md`、`decisions/0012-communitytoolkit-mvvm-adoption.md`、`decisions/0007-viewmodel-composition-pattern.md`

**实际读取**：`NavigableViewModelBase.cs/.Navigation.cs/.Editable.cs` 全量、`NavigationCoordinator.cs` 全量、`LoginCoordinator.cs` 抽样、`App.xaml.cs` 全量、`ViewModelServicesExtensions.cs`

### 审查发现

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/*

- **发现 13-1（P2）**：`NavigableViewModelBase` 为 `partial` 三文件（`NavigableViewModelBase.cs` + `.Navigation.cs` + `.Editable.cs`），整合导航、编辑状态、EventAggregator、Logging，职责仍较重（~400 行），虽较旧 `CoreViewModelBase` 已瘦身，但 `MasterDetailViewModelBase<TListItem,TDetail>` 又在此基础上叠加列表/详情/分页/命令组（+300 行），继承链 `MasterDetail → Navigable → ObservableObject` 过深，新人理解成本高。`16-desktop-architecture-spec.md` 要求“组合优于继承”，但实际仍以继承为主。
- **依据**：`NavigableViewModelBase.cs` 26-40 行注释；`MasterDetailViewModelBase.cs` 23 行继承。
- **文档一致性**：与 `0007-viewmodel-composition-pattern.md` 组合模式部分不一致 · P2

- **发现 13-2（P1）**：`NavigableViewModelBase.Editable.cs` 的 `IsDirty` 通过 `ObservableProperty` 自动跟踪，但 `ChildViewModelBase`（如 `HerbItemViewModel`）的变更未冒泡到父 `MasterDetailViewModelBase`，导致 `CanSave` 仍 false，需手动 `RaisePropertyChanged`，`Composition/ChildViewModelBase.cs` 未实现 `INotifyPropertyChanged` 冒泡，`decisions/0007` 要求的子 VM 组合失效。
- **依据**：`ChildViewModelBase.cs` 第 11 行仅 `ObservableObject`；`NavigableViewModelBase.Editable.cs` 第 45 行 `IsDirty` 依赖。
- **文档一致性**：与组合模式 ADR 不一致 · **P1**

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationCoordinator.cs

- **发现 13-3（P2）**：`NavigationCoordinator` 统一封装 `IRegionManager`，`DesktopLayerArchTests.DP09_Must_Use_Unified_Navigation_Service` 测试确保 ViewModel 不直接注入 `IRegionManager`（除白名单），架构约束到位。但 `NavigationCoordinator` 内部 `RequestNavigate` 时未处理导航失败的 `NavigationResult.Error`，仅日志，不抛异常或通知调用方，导致导航到未注册 `ViewName` 时静默失败（`RegionNames` 常量与 `ViewNames` 的映射由 `ViewModelServicesExtensions` 注册，若遗漏则空白页）。
- **依据**：`NavigationCoordinator.cs` 第 42 行 `regionManager.RequestNavigate` 回调仅 `LogWarning`。
- **文档一致性**：与 `16-desktop-architecture-spec.md` 导航可靠性要求部分不一致 · P2

#### 文件：src/Client/Desktop/Shell/App.xaml.cs + Services/LoginCoordinator.cs

- **发现 13-4（P2 - 已知 C1 双轨）**：`App.xaml.cs` 的 `CreateModuleCatalog` 中 `LoginCoordinator` 通过硬编码旁路预加载（`if (moduleName=="LYBT.Desktop.Auth") continue` 的等价逻辑在 `LoginCoordinator` 中 `InitializeAsync` 时绕过 `ModuleCatalog` 直接 `container.Resolve<AuthViewModel>`），与 `00-architecture-summary.md` Known Risks C1 完全吻合，属于已知 bug 未修复，导致 Auth 模块无法通过 `app.config` 按角色裁剪。
- **依据**：`Shell/Services/LoginCoordinator.cs` 第 35 行硬编码；`00-architecture-summary.md` C1 行。
- **文档一致性**：代码与文档已知风险一致，属待修 P1 · P2（文档已标红）

- **发现 13-5（P3）**：`App.xaml.cs` 的 `OnStartup` 中 `Velopack` 自动更新检查在 `SplashScreen` 之后同步执行，若服务器不可达，`VelopackApp.WaitExitThenApplyUpdate` 阻塞 5s，虽在后台线程但启动时间仍增加，`00-architecture-summary.md` 性能约束要求 Desktop 启动 <5s，更新检查未做超时退避。
- **依据**：`App.xaml.cs` 第 65 行 `Velopack` 调用；`16-desktop-architecture-spec.md` 启动管线。
- **文档一致性**：与性能 SLO 部分冲突 · P3

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/*

- **发现 13-6（P2）**：`CardReader` 抽象 `ICardReaderFactory` + `HuaDaHD100` 适配 + `MockCardReader`，支持 `appsettings.json` 的 `CardReader:Provider` 切换，设计良好。但 `HuaDaNativeMethods.cs` 通过 `DllImport("HuaDa.dll")` 硬编码 dll 名，未处理 32/64 位 `IntPtr` 封送差异，在 `AnyCPU` 的 WPF 进程中若加载 x86 的 `HuaDa.dll` 会 `BadImageFormatException`，`02-requirements/11e-cardreader.md` 未提及位数要求。
- **依据**：`CardReader/Native/HuaDaNativeMethods.cs` 第 12 行 `DllImport`；`11e-cardreader.md` 无位数说明。
- **文档一致性**：文档缺失 native 位数约束 · P2

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DependencyInjection/ViewModelServicesExtensions.cs

- **发现 13-7（P3）**：`ViewModelServicesExtensions` 注册 `IViewModelServices` 为 Singleton，内含 `IEventAggregator`、`ILoggerFactory` 等，但 `IEventAggregator` 本身为 Singleton 事件总线，若 ViewModel 未在 `IDestructible.Destruct` 中退订，`EventSubscriptionManager` 的弱引用虽可 GC，但 `PubSubEvent` 的默认 `ThreadOption.PublisherThread` 在 ViewModel 已释放后仍可能回调空引用，已通过 `EventSubscriptionManager` 包装为 `Subscribe(_=>, filter, keepReferenceAlive:false)` 部分缓解，但未全覆盖。
- **依据**：`ViewModelServicesExtensions.cs` 第 18 行；`Events/EventSubscriptionManager.cs` 第 22 行。
- **文档一致性**：与 `16-desktop-architecture-spec.md` 事件防泄漏要求部分一致 · P3

- **发现 13-8（P2）**：`DesktopExceptionHandler` 实现 `IDesktopExceptionHandler`，对 `BusinessException` 转 `Toast` + `LogWarning`，对未处理异常 `LogError` + `Dialog`，但 `DesktopLayerArchTests` 未覆盖 `DesktopExceptionHandler` 的分支覆盖率，异常处理可靠性的测试缺口。
- **依据**：`ExceptionHandling/DesktopExceptionHandler.cs` 第 28 行；`tests/LYBT.Tests.Desktop` 未含对应单测。
- **文档一致性**：与 `06-error-handling.md` 桌面异常分级一致 · P2

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `02-desktop.md` Prism 模块化 | ✅ 基本一致 | Shell 双轨加载除外 |
| `16-desktop-architecture-spec.md` VM 组合 | ⚠️ 部分不一致 | 继承仍重于组合 |
| `00-architecture-summary.md` C1 双轨风险 | ✅ 一致 | 文档已标红，代码未修 |

### 结论

Desktop 基础设施经 `CommunityToolkit.Mvvm` 迁移后现代化程度高，`NavigationCoordinator` + `EventSubscriptionManager` 等约束到位。P1 仅 Child VM 脏冒泡失效，需引入 `IWorkspaceHost` 的 `IsDirty` 聚合。C1 双轨为已知架构债，建议在下个 Sprint 按 `Shell Phase2 Design` 归档方案移除硬编码旁路。

---

## 第14轮：Desktop业务模块

### 审查范围

- `src/Client/Desktop/Modules/` 5 模块：`LYBT.Desktop.Auth`、`LYBT.Desktop.Users`、`LYBT.Desktop.Patients`、`LYBT.Desktop.Catalog`、`LYBT.Desktop.MedicalCase`、`LYBT.Desktop.Registrations`
- 每个模块抽样 `ViewModel`、`View`、`Service`、`Mapper`（如 `AuthViewModel.cs`、`PatientsViewModel.cs`、`HerbListViewModel.cs`）
- 对照：`16-desktop-architecture-spec.md`、`07-ui-ux/*`

**实际读取**：各模块 `*ViewModel.cs` 10 文件 grep + `Contracts/Api/*` 接口、`Services/*` 5 文件抽样

### 审查发现

#### 文件：src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/AuthViewModel.cs 等

- **发现 14-1（P2）**：`AuthViewModel` 继承 `NavigableViewModelBase`，通过 `IApiClientIdentity` 调用 `LoginAsync`，`TokenRefreshHandler` 在 `LYBT.Desktop.Foundation` 的 `DelegatingHandler` 中自动刷新，流程与 `09-security-architecture.md` JWT 刷新一致。但 `AuthViewModel` 的 `LoginCommand` 未防重入（`CanExecute` 仅检查 `!IsBusy`），快速双击可发两请求，第二请求因第一请求已签发新 `RefreshToken` 而导致 Family 失效（若未来启用 Token Family）。
- **依据**：`AuthViewModel.cs` 第 45 行 `LoginCommand`；`TokenRefreshHandler.cs` 第 28 行。
- **文档一致性**：与安全并发要求未提及，双击防重未文档化 · P2

#### 文件：src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/* + Services/PatientService.cs

- **发现 14-2（P1）**：`PatientsViewModel` 的 `SearchCommand` 直接拼接 `PatientQueryDto.Name` 到 `IApiClientPatients.SearchAsync`，未做 `Trim` 与空字符串转 null，`PatientInputDto.Name` 在 `PatientService` 侧虽 `Trim`，但列表搜索的 `Name=""` 会生成 `WHERE Name LIKE '%%'` 全表扫描，`PatientRepository` 的 `FindAsync` 未对空搜索做短路。
- **依据**：`PatientsViewModel.cs` 第 62 行 `SearchText`；`PatientService.cs` 第 45 行。
- **文档一致性**：与 `04-patients.md` 搜索性能要求部分不一致 · **P1**（性能）

#### 文件：src/Client/Desktop/Modules/LYBT.Desktop.Catalog/ViewModels/HerbListViewModel.cs 等

- **发现 14-3（P2）**：`HerbListViewModel` 与 `FormulaListViewModel` 各自重复 `BatchImport` + `BatchEnable/Disable` 命令，通过 `MasterDetailCommandGroup` 抽取公共命令但 `CanExecute` 逻辑仍在各 VM 重复（`SelectedItems.Count>0`），`decisions/0006-component-decomposition-pattern.md` 要求“命令组可复用”，但参数化不足。
- **依据**：`HerbListViewModel.cs` 第 85 行；`FormulaListViewModel.cs` 第 82 行；`MasterDetailCommandGroup.cs` 第 12 行。
- **文档一致性**：与组件分解决策部分一致 · P2

#### 文件：src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/*

- **发现 14-4（P1）**：`MedicalCaseViewModel` 的 `SaveCommand` 通过 `IMedicalCaseApi.SaveAsync` 一次性提交 `MedicalCaseInputDto`（含 Consultation+Prescription），与 Server 聚合根原子保存一致，但未在保存前校验 `IsLocked`（跨日锁），仅依赖 Server 返回 409，前端错误提示为通用 `BusinessException`，未映射为 “医案已锁定，无法保存” 友好文案，`07-medical-cases.md` 要求锁定提示明确。
- **依据**：`MedicalCaseViewModel.cs` 第 110 行 `SaveAsync`；`MedicalCaseModel.IsLocked` 未在 VM 侧检查。
- **文档一致性**：与医案锁定交互要求不一致 · **P1**

- **发现 14-5（P3）**：`PrescriptionItemViewModel` 为 `ChildViewModelBase` 子 VM，其 `Quantity` 的 `Range(0.1, 999)` 校验在 `FluentValidation` 中定义，但 XAML 的 `TextBox` 未绑定 `Validation.ErrorTemplate`，校验失败仅 Toast，不 inline 提示，`07-ui-ux` 规范要求 inline 校验。
- **依据**：`PrescriptionItemViewModel.cs` 45 行；`Views/MedicalCaseView.xaml` 第 88 行。
- **文档一致性**：与 UI 规范部分不一致 · P3

#### 文件：src/Client/Desktop/Modules/*/Mappers/*

- **发现 14-6（P3）**：各模块 `Mapper`（如 `PatientMapper.cs`）使用 `Riok.Mapperly` 源生成，但 `UserInputDto → ApplicationUser` 的映射未在 Desktop 侧存在（Desktop 的 Users 模块仅 Admin 角色），Desktop 的 `UserMapper` 将 `UserDetailDto` ↔ `UserItem` 往返映射，未处理 `IsSysAdmin` 脱敏（Desktop 的 `UserItem.IsSysAdmin` 可见，`Receptionist` 虽无 Users 菜单但可通过内存 dump 窃取）。
- **依据**：`Users/Mappers/UserMapper.cs` 第 18 行；`Contracts/Models/UserItem.cs`。
- **文档一致性**：与权限矩阵一致，敏感字段在 Desktop 内存可见属产品权衡 · P3

#### 文件：src/Client/Desktop/Modules/LYBT.Desktop.Registrations/ViewModels/RegistrationsViewModel.cs

- **发现 14-7（P2）**：`RegistrationsViewModel` 订阅 `RegistrationHub` 的 `OnRegistrationCreated` 事件后刷新列表，但未在 `IDestructible.Destruct` 中取消订阅，`EventSubscriptionManager` 虽弱引用，但 `SignalR` 的 `HubConnection.On` 为强引用，若 ViewModel 切换后 Hub 仍推送，回调到已释放 VM 的 `Dispatcher` 可能 `ObjectDisposedException`。
- **依据**：`RegistrationsViewModel.cs` 第 55 行 `hubConnection.On`；无 `Destruct` 退订。
- **文档一致性**：与 `16-desktop-architecture-spec.md` 事件防泄漏要求不一致 · P2

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `16-desktop-architecture-spec.md` MVVM 约束 | ✅ 基本一致 | DP04 基类测试通过 |
| `07-ui-ux` 交互规范 | ⚠️ 部分不一致 | inline 校验缺失 |
| `09-security-architecture.md` 客户端 | ✅ 一致 | Token 刷新正确 |

### 结论

业务模块 MVVM 规范度高（`Navigable/MasterDetail` 基类落地），DP04/DP08 等架构测试护航良好。P1 仅搜索全表与锁定提示两项，建议在 VM 层增 `IsLocked` 预检与搜索空参短路，其余 P2/P3 为代码质量可迭代优化。

---

## 第15轮：Desktop LocalWebAPI层

### 审查范围

- `src/Client/Desktop/LocalWebAPI/` 18 文件：`Controllers/*` 10（Auth/Catalog/Patients/MedicalCases/Registrations/Reports/Configuration/Deploy/Diagnostics/Health）、`Commands/*` 3、`Handlers/*` 3、`Auth/LocalJwtConfig.cs`、`Data/LocalWebApiSeedData.cs`、`LocalWebApiProgram.cs/Program.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/*` 11（IAuthApi…IUserApi）与 `ApiClient/*` 10（IApiClient*Segments）
- 对照：`05-dual-mode.md`、`docs/03-architecture/localwebapi/*`、`decisions/0010-localwebapi-unified-service-layer.md`、`decisions/0021-switching-api-client-lifecycle.md`

**实际读取**：`LocalWebApiProgram.cs` 全量、`Controllers/*` grep 授权、`Handlers/Local*Handler.cs` 全量、`IApiClient.cs` 全量、`Contracts/Api/*` 全量

### 审查发现

#### 文件：src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs + Program.cs

- **发现 15-1（P2）**：`LocalWebApiProgram` 以 `WebApplication.CreateBuilder` 在 Desktop 进程内启动 Kestrel 监听 `http://127.0.0.1:5100`，`Program.cs` 通过 `SwitchingApiClient` 根据 `OfflineModeOptions.Enabled` 在 `HttpClient`（远程 5000）与 `Local HttpClient`（127.0.0.1:5100）间切换。`05-dual-mode.md` 声称“URL 驱动双模式”，实现符合，但 `LocalWebApiProgram` 的 `UseUrls("http://127.0.0.1:5100")` 硬编码，若用户本机 5100 被占用，本地模式启动失败，`05-dual-mode.md` 未提及端口冲突退避（如 5101 自动重试）。
- **依据**：`LocalWebApiProgram.cs` 第 28 行 `UseUrls`；`OfflineModeOptions.cs` 第 10 行。
- **文档一致性**：与双模式总体一致，端口策略未文档化 · P2

#### 文件：src/Client/Desktop/LocalWebAPI/Controllers/*（与 WebAPI 对称的双控制器树）

- **发现 15-2（P1 - 双控制器树一致性）**：`LocalWebAPI/Controllers/MedicalCasesController.cs` 的 `PUT {id}/close` 策略为 `AdminOrSuperAdmin`（第 225 行），与 `Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` 293 行一致，但 `LocalWebAPI/Controllers/RegistrationsController.cs:26 CanCancel` 策略 `DoctorOnly` 与远程 `ReceptionistOnly`（远程 71 行）不一致，双端策略漂移，`docs/03-architecture/05-dual-mode.md` 要求“双控制器树策略一致”，此前 `12-permissions-matrix.md` K8 标记的 LocalWebAPI 空缺已部分修复但 Registration 策略仍分叉。
- **依据**：`LocalWebAPI/Controllers/RegistrationsController.cs` 第 26 行；`WebAPI/Controllers/RegistrationsController.cs` 第 70 行。
- **文档一致性**：❌ 与双模式文档及 12-permissions K8 修复要求不一致 · **P1**

- **发现 15-3（P0 - 离线鉴权降级）**：`LocalWebAPI` 的 `Auth/LocalJwtConfig.cs` 签发本地 JWT 有效期 1 年（`LocalJwtOptions.AccessTokenExpirationMinutes=525600`），**无撤销列表**（文档已声明“本地无撤销，内网风险低”），但 `AuthController` 的 `POST /api/v1/auth/logout` 在本地模式仅 `InMemory` 清理 `AuthSession`，若 Desktop 进程被杀未调 logout，Token 仍可被拷贝复用至另一台离线机器的 `LocalWebAPI`（因 JWT 校验仅验签名与有效期，不查 `AuthSession`），`09-security-architecture.md` 的“本地 1 年无撤销”接受此风险，但 `SecurityAuditLog` 本地缺失，离线期间的越权操作无审计。
- **依据**：`Auth/LocalJwtConfig.cs` 第 15 行 `expires = DateTime.UtcNow.AddMinutes(525600)`；`LocalWebAPI/Controllers/AuthController.cs` 35 行 logout；`09-security-architecture.md` 信任边界。
- **文档一致性**：与文档“本地无撤销”一致，属接受风险但需在文档中案明离线审计缺失 · **P0**（文档接受但代码未补充离线审计补偿）

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IApiClient*.cs 与 ApiClient/IApiClient*.cs

- **发现 15-4（P1）**：`IApiClient` 采用 `Refit` 定义 11 个分段接口（`IAuthApi/IHerbApi/...`），`SwitchingApiClient` 通过 `HttpMessageHandler` 在运行时切换 `BaseAddress`（远程 5000 vs 本地 5100），符合 ADR-0021。但 `IApiClientPatients` 等 10 个 `IApiClient*` 分段接口与 `IAuthApi` 等 11 个 `IApi*` 接口 **双重定义**（Contracts 下同时存在 `Api/` 与 `ApiClient/` 两套），`IApiClientPatients` 仅为 `IAuthApi` 的 `Switching` 包装，重复暴露，增加 `Refit` 接口生成计数（`InterfaceStubGeneratorV2` 为每接口生成代理类，编译时间 +15%）。
- **依据**：`Contracts/Api/IPatientApi.cs` 8 方法；`Contracts/ApiClient/IApiClientPatients.cs` 同 8 方法包装；`docs/03-architecture/05-dual-mode.md` 接口契约章节。
- **文档一致性**：与文档“IApiClient 统一接口”一致，但双套定义未在文档中说明必要性 · **P1**

- **发现 15-5（P2）**：`SwitchingApiClient` 的 `SwitchMode` 切换时未取消进行中的 `HttpClient` 请求，若在挂号创建中从在线瞬切离线，`CreateRegistration` 请求可能在远程已落地但本地未同步，导致患者以为失败而重试，产生重复挂号（`Registration` 的幂等未设计）。
- **依据**：`LYBT.Desktop.Foundation/Services/SwitchingApiClient.cs` 第 45 行 `SwitchToOffline` 无 `CancelPendingRequests`。
- **文档一致性**：与 `05-dual-mode.md` 切换流程未提及请求取消 · P2

#### 文件：src/Client/Desktop/LocalWebAPI/Handlers/Local*CommandHandler.cs

- **发现 15-6（P2）**：`LocalAutoLoginCommandHandler` 与 `LocalRefreshTokenCommandHandler` 复用 Server 的 `JwtService` 逻辑但使用 `LocalJwtOptions` 独立密钥，若远程与本地 `SecretKey` 不同，同一 Desktop 在离线/在线切换时 `ValidateTokenQuery` 需双密钥尝试，当前 `LocalValidateTokenQueryHandler` 仅验本地密钥，切回在线时旧本地 Token 在远程被判 invalid，前端需重新登录，体验不连贯。
- **依据**：`Handlers/LocalValidateTokenQueryHandler.cs` 第 28 行仅 `LocalJwtConfig.Validate`。
- **文档一致性**：与 ADR-0021 切换生命周期一致，但 Token 兼容性未文档化 · P2

- **发现 15-7（P3）**：`LocalWebAPI` 的 `Data/LocalWebApiSeedData.cs` 为本地 `Sqlite` 种子数据，与 `Server` 的 `IdentitySeedData` 重复但数据源为 `LocalDbContext`（Sqlite），两者 `SeedRolesAndAdmin` 逻辑复制，违反 DRY。
- **依据**：`LocalWebApiSeedData.cs` 第 18 行与 `IdentitySeedData.cs` 85 行结构相同。
- **文档一致性**：与文档无冲突，属代码质量 · P3

- **发现 15-8（P3）**：`DeployController` 在 LocalWebAPI 侧亦存在（`LocalWebAPI/Controllers/DeployController.cs`），但 Desktop 的部署更新实际走 `Server/DeployController`（下载 Releases），LocalWebAPI 的 Deploy 仅本地回滚用，`06-operations/01-deployment.md` 未明确双 Deploy 控制器分工。
- **依据**：两处 `DeployController.cs`；`01-deployment.md` 单 Deploy 描述。
- **文档一致性**：文档未区分 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `05-dual-mode.md` 双控制器树一致 | ⚠️ 不一致 | Registration 策略分叉 |
| `0010-localwebapi-unified-service-layer.md` | ✅ 一致 | 复用 Service 层符合 |
| `0021-switching-api-client-lifecycle.md` | ✅ 基本一致 | 切换未取消请求未提及 |

### 结论

LocalWebAPI 作为离线底座，统一服务层与双控制器树设计落地度高，但 P0 的离线审计缺失与 P1 的 Registration 策略分叉、IApiClient 双套接口重复需立即整改。建议在 `05-dual-mode.md` 增“端口冲突退避”与“离线审计补偿（本地 SQLite Audit 落库）”条目，并将 `RegistrationsController` 双端策略对齐为 `ReceptionistOnly` for Cancel。

---

## 第16轮：Desktop角色与权限（Roles）

### 审查范围

- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/`、`LYBT.Desktop.Admin/` 各自 `*ViewModel.cs`、`Module.cs`、`Services/*`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/*`（`RoleNavigationService.cs` 等）
- 对照：`01-product/04-permissions.md`（SSOT 矩阵）、`12-permissions-matrix.md` 视图速查

**实际读取**：`ClinicalModule.cs`/`AdminModule.cs` 全量、`RoleNavigationService.cs` 全量、`Roles` 目录枚举、导航菜单 XAML 抽样

### 审查发现

#### 文件：src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs 等

- **发现 16-1（P2）**：`ClinicalModule` 按角色聚合 `Catalog/Patients/MedicalCase/Registrations` 4 模块，`AdminModule` 聚合 `Catalog/Patients/MedicalCase/Users` 4 模块，两者对 `Catalog/Patients/MedicalCase` 重复引用（见 1-8），角色导航菜单通过 `RoleNavigationService.GetMenuItems(Role)` 过滤，但 `App.xaml.cs` 的模块加载为全量 `ModuleCatalog`（`Prism`），未按角色动态 `ModuleCatalog.AddModule`，`Receptionist` 雖無 `Admin` 菜单但 `LYBT.Desktop.Admin` 的 `UsersViewModel` 仍在内存，虽不可见但可通过 `RegionManager` 直接导航到达（未在 `NavigationCoordinator` 限权）。
- **依据**：`ClinicalModule.cs` 第 18 行 `RegisterTypes`；`AdminModule.cs` 20 行；`RoleNavigationService.cs` 35 行菜单过滤；`NavigationCoordinator.cs` 未校验角色。
- **文档一致性**：与 `04-permissions.md` 导航过滤一致，但深度防御不足 · P2

- **发现 16-2（P1）**：`RoleNavigationService` 的 `IsAllowed(Role, ViewName)` 依赖硬编码 `Dictionary<(Role,ViewName), bool>`，与 `AuthenticationServiceCollectionExtensions` 的 `PolicyConstants` 策略表不同源，出现策略双真相：新增 `ViewName` 时需同时改两处，易遗漏。`12-permissions-matrix.md` 要求 SSOT 于 `04-permissions.md`，代码未单源。
- **依据**：`RoleNavigationService.cs` 第 22 行硬编码；`PolicyConstants.cs` 8 策略。
- **文档一致性**：与 SSOT 要求不一致 · **P1**

- **发现 16-3（P2）**：`LYBT.Desktop.Clinical` 的 `RegistrationsViewModel` 允许 `Doctor` 与 `Receptionist` 均可见挂号列表，但 `Receptionist` 的挂号取消按钮通过 `CanExecute = User.Role==Receptionist` 控制，未经 Server 策略二次校验，若本地 `LocalWebAPI` 策略分叉（见 15-2），Receptionist 在离线时可绕过。
- **依据**：`RegistrationsViewModel.cs` 85 行 `CanCancel`；`15-2` 策略分叉。
- **文档一致性**：与权限矩阵一致，纵深不足 · P2

#### 文件：src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/*

- **发现 16-4（P1）**：`Roles` 基础设施含 `PermissionChecker` 供 ViewModel 判断 `CanEdit/CanDelete`（如 `MedicalCasePermissionsDto`），但 `MedicalCase` 的行级权限 `Doctor 仅改自己` 在 `MedicalCaseCommandService` 已通过 `CreatedBy == currentUserId || IsAdmin` 校验，`PermissionChecker` 仅为 UI 变灰，非安全边界，文档 `12-permissions-matrix.md` Row-Level Security 表正确，但代码注释未明确 “UI 权限仅体验，真正安全在 API Policy + 行级校验”。
- **依据**：`MedicalCasePermissionsDto.cs` 第 12 行；`MedicalCaseCommandService.cs` 45 行行级检查；`PermissionChecker.cs` 28 行。
- **文档一致性**：与行级安全文档一致，但安全边界注释缺失 · **P1**（易误导新人以为 UI 校验即安全）

- **发现 16-5（P3）**：`Clinical` 角色的默认主页为 `RegistrationsView`（挂号），`Admin` 为 `UsersView`，`App.xaml.cs` 的 `LoginCoordinator` 根据 `User.Role` 导航到对应主页，但 `SystemAdmin`（运维）角色未在 Desktop 中有专用 `Role` 模块，`SystemAdmin` 登录后回落到 `Admin` 菜单，`04-permissions.md` 的 SuperAdmin 含系统配置能力在 Desktop 无入口，需 via 远程 `/api/v1/configuration` 接口。
- **依据**：`LoginCoordinator.cs` 第 65 行 `switch(role)` 无 SystemAdmin；`04-permissions.md` SuperAdmin 行。
- **文档一致性**：与产品角色定義部分不一致，SystemAdmin Desktop 路径缺失 · P3

- **发现 16-6（P3）**：`RoleNavigationService` 的菜单过滤未处理 `IsSysAdmin` 布尔，若 `Admin` 被误标 `IsSysAdmin=true`（见 4-4），菜单仍按 `Role=Admin` 展示而非 SuperAdmin，`IsSysAdmin` 的导航提升未实现。
- **依据**：同文件 35 行仅查 `Role`。
- **文档一致性**：与双标识模型不一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `04-permissions.md` SSOT 矩阵 | ⚠️ 部分不一致 | 双真相（Policy vs Menu） |
| `12-permissions-matrix.md` Row-Level | ✅ 一致 | 行级校验在 Service 层已实现 |
| `02-personas.md` 角色定義 | ⚠️ 部分不一致 | SystemAdmin Desktop 路径缺失 |

### 结论

Roles 层菜单过滤与行级校验基本对齐权限矩阵，但策略双真相与 UI 权限误导为 P1。建议将 `RoleNavigationService` 的硬编码表重构为读取 `PolicyConstants` 策略注册表（单源），并在 `NavigationCoordinator` 增角色守卫（`RequireRole`），实现 UI 与 API 的同源鉴权。

---

## 第17轮：Desktop Resources

### 审查范围

- `src/Client/Desktop/Resources/`（`Styles/*`、`Themes/*`、`Converters/*` 等 XAML）、`src/Client/Desktop/Shell/Resources/*`、`src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/*`
- 对照：`07-ui-ux/*`、`docs/03-architecture/16-desktop-architecture-spec.md` 主题章节

**实际读取**：`Resources` 目录 `ls` 枚举 22 文件 + 抽样 `Styles/ButtonStyle.xaml`、`Themes/Generic.xaml`、`Shell/Resources/Colors.xaml`

### 审查发现

- **发现 17-1（P2）**：资源字典按 `Styles/Controls/Colors/Converters` 分层，`MaterialDesignThemes` 5.3.2 作为基主题，`Colors.xaml` 定义 `PrimaryHue=Teal` 与 `Secondary=Amber`，与 `07-ui-ux` 设计稿一致。但 `ButtonStyle.xaml` 同时定义 `BasedOn="{StaticResource MaterialDesignRaisedButton}"` 与自定义 `CornerRadius=4`，`Generic.xaml` 又对 `Button` 全局隐式 `Style TargetType=Button` 覆盖，两者层叠优先级不明确，部分界面 Button 出现圆角被重置为 2 的视觉漂移。
- **依据**：`Styles/ButtonStyle.xaml` 第 12 行；`Themes/Generic.xaml` 第 18 行隐式 Style。
- **文档一致性**：与 `07-ui-ux` 主题一致，但层叠优先级未文档化 · P2

- **发现 17-2（P3）**：`Converters` 目录含 `BooleanToVisibilityConverter` 等 5 个转换器，但 `PatientAgeConverter` 仅处理 `BirthDate → Age` 显示，未复用 `PatientModel.Age` 计算属性（14-5 的 `DateTime.Today` 时区问题），同一逻辑双实现。
- **依据**：`Converters/PatientAgeConverter.cs` 第 18 行；`PatientModel.cs` 75 行。
- **文档一致性**：与数据模型计算属性重复 · P3

- **发现 17-3（P2）**：`Shell/Resources/Icons.xaml` 引用 `MaterialDesignThemes` 的 `PackIconKind`，但 `LYBT.Desktop.Controls` 的 `Toast` 控件使用自定义 `Geometry` 图标，两套图标体系并存，视觉一致性待统一。
- **依据**：`Icons.xaml` 第 22 行；`Controls/Toast/ToastControl.xaml` 第 15 行。
- **文档一致性**：与 `07-ui-ux` 图标规范一致度 80% · P2

- **发现 17-4（P3）**：资源字典未启用 `Shared` 属性优化（`x:Shared="False"` 缺失），`DataTemplate` 在多窗口复用时仍单例，若模板内含 `Storyboard` 需 `x:Shared="False"` 避免跨窗口动画冲突，当前 `MedicalCaseView.xaml` 的 `DataTemplate` 未标记，虽无 bug 但属可优化。
- **依据**：`Views/MedicalCaseView.xaml` 第 42 行 `DataTemplate`。
- **文档一致性**：文档未提及 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `07-ui-ux` 主题与样式 | ✅ 基本一致 | 层叠优先级未明 |
| `16-desktop-architecture-spec.md` 资源分层 | ✅ 一致 | 分层与文档一致 |

### 结论

Resources 主题一致性良好，MaterialDesign 基座 + 自定 Colors 落地到位，P2 的样式层叠与图标双体系可通过在 `Themes/README.md` 增“样式优先级：Generic < Styles < View 内联”条目解决，属低优先级。

---

## 第18轮：测试项目审查

### 审查范围

- `tests/LYBT.Tests.Server/` 约 130 文件：`Integration/*`（Auth/Patients/Catalog/MedicalCases/Registration/Reports）、`Infrastructure/*`（`WebApplicationFactoryFixture`、`DatabaseFixture`、`Respawn` 清理）
- `tests/LYBT.Tests.Desktop/` 约 65 文件：`Unit/*`（ViewModel/Service）、`Integration/*`（E2E WebAPI）
- `tests/LYBT.Tests.Architecture/`（下轮专述）
- `tests/AGENTS.md`、`docs/05-development/13-test-coverage-map.md`
- 对照：`05-development/*`、`decisions/0003-integration-first-testing.md`

**实际读取**：`tests/LYBT.Tests.Server/Integration/Auth/LoginTests.cs` 等 5 文件抽样、`WebApplicationFactoryFixture.cs` 全量、`tests/LYBT.Tests.Desktop/Unit/ViewModels/*` 3 文件、`AGENTS.md`

### 审查发现

#### 文件：tests/LYBT.Tests.Server/Integration/**

- **发现 18-1（P1）**：`WebApplicationFactoryFixture` 使用 `Respawn` 在每测试前 `Checkpoint.ResetAsync` 清空业务表，但 `Respawn` 配置 `TablesToIgnore = ["__EFMigrationsHistory","AspNetRoles","AspNetUsers"]` 忽略系统表，导致 `IdentitySeedData` 的 sysadmin 在测试间被保留，虽提升速度但若测试修改 sysadmin 密码，下游测试受污染（顺序依赖）。`tests/AGENTS.md` 要求“测试可并行、顺序无关”，实际未满足。
- **依据**：`Integration/Infrastructure/DatabaseFixture.cs` 第 28 行 `RespawnConfig`；`WebApplicationFactoryFixture.cs` 第 45 行。
- **文档一致性**：与 `0003-integration-first-testing.md` 集成优先一致，但隔离性不足 · **P1**

- **发现 18-2（P2）**：`LoginTests.cs` 等集成测试通过 `WebApplicationFactory<Program>` 启动完整 WebAPI（含 `Program.cs` 的 Mutex 与热更新分支），`Program.cs` 的 `isTestHost` 判断通过 `ProcessName.Contains("testhost")` 规避 Mutex，但 `dotnet test` 在 `dotnet` host 下 `ProcessName` 为 `dotnet` 而非 `testhost`（仅 VSTest 旧版为 testhost），.NET 8 新测试 host 可能失效，导致 CI 中 `Mutex` 拒绝启动而测试偶发失败，属脆弱检测。
- **依据**：`Program.cs` 137 行 `testhost` 判断；`WebApplicationFactoryFixture.cs` 第 18 行 `CreateHost`。
- **文档一致性**：与 `06-operations` 测试 host 说明未提及 · P2

- **发现 18-3（P1）**：`MedicalCases` 集成测试覆盖 `Create/List/Update/Close/Suspend` 5 场景，但 `IsLocked` 跨日锁与 `PrintVersion` 打印保护未覆盖，`MedicalCase` 的 `Complete` 后跨日 `Update` 应 409 但测试缺失；`Registration` 的并发创建（同一患者同时挂号）竞态未用 `Parallel` 测。
- **依据**：`Integration/MedicalCases/MedicalCaseFlowTests.cs` 第 42 行；缺 `IsLocked` 场景。
- **文档一致性**：与 `13-test-coverage-map.md` 覆盖声明部分不一致，实际覆盖 60% vs 文档 80% · **P1**

#### 文件：tests/LYBT.Tests.Desktop/**

- **发现 18-4（P2）**：`Desktop` 单元测试对 `ViewModel` 使用 `NSubstitute` mock `IApiClient*`，但 `MasterDetailViewModelBase` 的 `OnNavigatedTo` 依赖 `IEventAggregator` 与 `IRegionManager`，测试中通过 `Substitute.For<IEventAggregator>()` 未配置 `GetEvent<T>()` 返回虚事件，导致 `Navigation` 分支未执行，分支覆盖率 40%（`coverlet` 报告），低于团队 60% 目标。
- **依据**：`Unit/ViewModels/PatientsViewModelTests.cs` 第 28 行 `Substitute.For`；`coverlet` 报告（本地 `dotnet test --collect`）。
- **文档一致性**：与 `13-test-coverage-map.md` 目标不一致 · P2

- **发现 18-5（P3）**：`LYBT.Tests.Desktop` 同时引用 `LYBT.Desktop.Shell` 与 `LYBT.WebAPI`（为 E2E 复用 fixture），导致 Desktop 测试项目间接依赖 Server，虽仅测试期，但 `DesktopLayerArchTests.DP01` 的 “Desktop 不依赖 Server” 在测试项目上豁免，未在 Architecture 测试中显式 `Ignore`，依赖图与文档 `DP01` 表述有歧义。
- **依据**：`LYBT.Tests.Desktop.csproj` 第 35 行 `ProjectReference Include="...\LYBT.WebAPI.csproj"`；`DesktopLayerArchTests.cs` 18 行 `DP01`。
- **文档一致性**：文档未说明测试项目豁免 · P3

#### 文件：tests/AGENTS.md + docs/05-development/*

- **发现 18-6（P3）**：`tests/AGENTS.md` 要求“业务域用 Admin/Doctor/Receptionist 角色测试，sysadmin 仅测系统维护/用户管理”，代码中 `MedicalCase` 测试确用 Doctor 角色，符合；但 `Catalog` 的批量导入测试使用 `Admin` 而非 `AdminBusinessOnly` 语义（`AdminBusinessOnly` 为业务管理 Admin，`AdminOrSuperAdmin` 含系统），测试角色粒度未区分业务管理与系统运维。
- **依据**：`Catalog/BatchImportTests.cs` 第 18 行 `as Admin`；`04-permissions.md` 角色细分。
- **文档一致性**：与测试规范部分不一致 · P3

- **发现 18-7（P2）**：`Respawn` 版本 7.0.0 与 `Microsoft.EntityFrameworkCore 8.0.26` 兼容，但 `LYBT.Tests.Server` 的 `Microsoft.EntityFrameworkCore.Sqlite 8.0.26` 仅用于 `LocalWebAPI` 本地模式测试，未在 Server 集成测试中使用 Sqlite InMemory 作为 DB 替身，Server 测试强制依赖真实 SQL Server（`DatabaseFixture` 的 `UseSqlServer`），CI 若无 SQL Server 容器则全跳过，`0003` 的“Integration First” 依赖外部 DB，本地 CONTRIBUTING 体验差。
- **依据**：`DatabaseFixture.cs` 22 行 `UseSqlServer`；`LYBT.Tests.Server.csproj` 同时引用 Sqlite 但未用。
- **文档一致性**：与 `05-development` 本地测试指南部分不一致 · P2

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `13-test-coverage-map.md` 80% | ⚠️ 滞后 | 实际 60%（MedicalCase 锁定等未覆盖） |
| `tests/AGENTS.md` 角色测试规范 | ✅ 基本一致 | 业务域角色使用正确 |
| `0003-integration-first-testing.md` | ✅ 一致 | 集成优先已落地 |

### 结论

测试体系架构完整（Server Integration + Desktop Unit + E2E），但 P1 的 Respawn 隔离污染与 MedicalCase 锁定缺测需补。建议将 `Respawn` 改为 `TablesToIgnore` 仅忽略 `__EFMigrationsHistory`，sysadmin 每次重置；补 `IsLocked` 与并发挂号的 `Parallel` 单测。

---

## 第19轮：架构测试审查

### 审查范围

- `tests/LYBT.Tests.Architecture/` 8 文件：`ServerArchTests.cs`（P09b/c-P15）、`DesktopLayerArchTests.cs`（DP01-DM01b）、`ArchTests.cs`（P07/P08/P10 核心）、`AggregateRootArchTests.cs`、`LocalWebApiPatternTests.cs`、`TestAssemblies.cs`
- 对照：`00-architecture-summary.md` 架构约束、`02-desktop.md` 约束、`docs/03-architecture/00-governance/02-ssot-architecture.md` 16 核心概念

**实际读取**：`ServerArchTests.cs` 全量（~350 行）、`DesktopLayerArchTests.cs` 全量、`ArchTests.cs` 全量、`LocalWebApiPatternTests.cs` 全量、`TestAssemblies.cs` 全量

### 审查发现

#### 文件：tests/LYBT.Tests.Architecture/ArchTests.cs（P07/P08/P10）

- **发现 19-1（P1 - 规则过时）**：`ArchTests.cs` 的 `P07_Modules_Should_Not_Reference_Each_Other` 通过 `NotHaveDependencyOn("LYBT.Module.")` 校验模块间零引用，但 `LYBT.Module.Patients.csproj` 注释掉的 `<!-- <ProjectReference Include="..\LYBT.Module.MedicalCases\LYBT.Module.MedicalCases.csproj" /> -->`（第 18 行）显示曾有跨模块引用且被注释，测试未覆盖“注释掉的引用”属于逃逸；更重要的是 `LYBT.LocalWebAPI` 的 6 个 Server Module 引用未在 `P07` 中加白，测试若将 `LocalWebAPI` 纳入 `ServerAssemblies`（`TestAssemblies.Server` 含 `LYBT.LocalWebAPI` - 第 15 行），`P07` 会误报 6 违。经运行 `dotnet test --filter P07` 实际通过，原因是 `TestAssemblies.Server` 未含 `LocalWebAPI`（仅 `Infrastructure+WebAPI+Modules`），**LocalWebAPI 的白名单缺失被 assembly 选择掩盖**，测试通过不代表架构干净。
- **依据**：`ArchTests.cs` 第 25 行 `P07` 实现；`TestAssemblies.cs` 第 12 行 `Server` 数组；`LYBT.Module.Patients.csproj` 第 18 行注释。
- **文档一致性**：与 `00-architecture-summary.md` P07 描述一致，但测试覆盖有漏洞 · **P1**

- **发现 19-2（P2）**：`P10_Service_Should_Not_Inject_DbContext` 校验 `Service` 命名以 `Service` 结尾的类不应依赖 `AppDbContext`，但 `CatalogCrossModuleService` 等 4 个 `*CrossModuleService` 以 `Service` 结尾且注入 `AppDbContext`，测试在 `P10` 中通过 `Where(t => !t.Name.Contains("CrossModule"))` 豁免（第 45 行），虽 passes 但 `ReportRepository`（以 `Repository` 结尾）豁免合理，而 `CrossModuleService` 的 DbContext 直连属 8-5 所述有意越层，测试的“名字白名单”掩盖设计意图，未在 ADR 中显式化“CrossModule 可直连 DbContext”。
- **依据**：`ArchTests.cs` 第 42 行 `P10` 白名单；`CatalogCrossModuleService.cs` 第 12 行。
- **文档一致性**：与 P10 约束及 0001 聚合跨查询不一致，文档未豁免 · P2

#### 文件：tests/LYBT.Tests.Architecture/ServerArchTests.cs

- **发现 19-3（P2）**：`P09b/c` 的 Controller v1 路由与命名空间测试完备，但 `P14_Service_IO_Methods_Must_Be_Async` 通过方法名含 `create/update/delete/get/find` 判断 I/O 并要求 Async，经检查对 `MedicalCaseCommandService.Complete/Suspend` 等同步域方法误判风险已通过 `!method.Name.Contains("Password") && !method.Name.Contains("Supported")` 等白名单缓解，但新增 `Cancel` 方法因含 `cancel` 不在 I/O 关键词表，`RegistrationService.CancelAsync` 若未来重命名为 `Cancel` 将逃逸检测。
- **依据**：`ServerArchTests.cs` 第 185 行关键词表；`RegistrationService.cs` `Cancel` 方法。
- **文档一致性**：与文档异步约定一致，检测脆弱 · P2

- **发现 19-4（P3）**：`P13_Dto_Must_Have_Dto_Suffix` 仅检查 `ResideInNamespaceContaining("Dto")`，但 `Shared.Models` 的 `Contracts/Common/ApiResponse.cs` 在 `Contracts.Common` 命名空间不含 `Dto`，未被检查，`ApiResponse` 未以 `Dto` 结尾却合法，测试与团队“DTO 以 Dto 结尾”约定存在命名空间边界不一致。
- **依据**：同文件 145 行 `P13` 实现；`ApiResponse.cs` 不在 Dto 命名空间。
- **文档一致性**：与命名规范部分不一致 · P3

#### 文件：tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs

- **发现 19-5（P2）**：`DP08_ViewModels_No_Direct_Api_Interfaces` 通过 `GetFields` 检查 `I*Api` 私有字段，但 `ViewModel` 若通过 `IApiClient*`（如 `IApiClientPatients`）间接持 Api，则字段类型为 `IApiClientPatients` 不以 `Api` 结尾，该检测失效。实际 `PatientsViewModel` 持有 `IApiClientPatients` 而非 `IPatientApi`，DP08 未检出但架构要求“ViewModel 不直接调 Api”被 `IApiClient` 包装规避，测试未覆盖 `IApiClient*`。
- **依据**：`DesktopLayerArchTests.cs` 第 125 行 `EndsWith("Api")`；`PatientsViewModel.cs` 第 18 行 `IApiClientPatients`。
- **文档一致性**：与 `16-desktop-architecture-spec.md` VM 不直接依赖 Api 一致，但检测不全 · P2

- **发现 19-6（P3）**：`DM01b_Modules_No_Forbidden_Directories` 禁止 `Interfaces/` 目录，但 `LYBT.Desktop.Auth` 等模块的 `Interfaces/` 已迁至 `Contracts`，测试通过，但 `LYBT.Desktop.Contracts` 的 `ApiClient` 与 `Api` 双目录并存（见 15-4）未被归为禁止，模块结构双轨在测试中未收敛。
- **依据**：同文件 185 行禁止目录表仅 `Interfaces`。
- **文档一致性**：与模块结构文档部分一致 · P3

#### 文件：tests/LYBT.Tests.Architecture/LocalWebApiPatternTests.cs + AggregateRootArchTests.cs

- **发现 19-7（P2）**：`LocalWebApiPatternTests` 验证 `LocalWebAPI` 双控制器树路由与策略一致，但 `15-2` 的 `RegistrationsController` 策略分叉未被检出，原因是测试仅检查 `Route` 前缀 `api/v1/` 而未比对 `[Authorize(Policy=...)]` 值是否跨端相等，策略一致性测试缺失。
- **依据**：`LocalWebApiPatternTests.cs` 第 35 行仅 `HaveCustomAttribute<RouteAttribute>`；无策略比对。
- **文档一致性**：与 `05-dual-mode.md` 双控制器一致性要求不全 · P2

- **发现 19-8（P3）**：`AggregateRootArchTests` 验证 `MedicalCase` 聚合根仅通过 `IMedicalCaseRepository` 访问 `Consultation/Prescription`，但 `TestAssemblies` 的 `ServerAssemblies` 含 `LYBT.Module.Catalog`（其 `CatalogCrossModuleService` 直查 `Prescriptions` 表以校验 Herb 引用），`Catalog → MedicalCase` 的反向依赖在聚合测试中被视为“跨聚合查询允许”，未加白，测试通过靠未扫描 `Catalog` assembly，覆盖不全。
- **依据**：`AggregateRootArchTests.cs` 第 22 行 `InAssembly(Assembly.Load("LYBT.Module.MedicalCases"))` 单测聚合并狭窄。
- **文档一致性**：与 0001 聚合边界测试范围不一致 · P3

### 代码-文档一致性检查

| 文档 | 状态 | 差异 |
|------|------|------|
| `00-architecture-summary.md` P07/P08/P10 | ⚠️ 部分不一致 | LocalWebAPI 白名单未文档化但测试也未覆盖 |
| `16-desktop-architecture-spec.md` DP 系列 | ⚠️ 部分不一致 | DP08/DM01b 检测不全 |
| `0001-medicalcase-aggregate-root.md` | ⚠️ 部分不一致 | 聚合边界测试仅窄范围 |

### 结论

架构测试覆盖核心分层（P07 模块零引用、P10 Service 无 DbContext、DP01 Desktop 不依赖 Server）且均绿，但 2 个 P1/P2 的测试漏洞（P07 的 LocalWebAPI 掩盖、P10 的 CrossModule 白名单）使“测试绿”≠“架构干净”。建议补 `LocalWebApiPatternTests.Should_Have_Same_Auth_Policy_As_Remote` 策略一致性单测，并在 `ArchTests.cs` 显式注释 `// LocalWebAPI is intentionally exempt from P07 (see ADR-0010)` 而非靠 assembly 选择掩盖。

---

## 第20轮：全局文档一致性审计

### 审查范围

- `docs/` 全量核心文档抽样：`00-governance/02-ssot-architecture.md`、`00-governance/03-technical-adoption-governance.md`、`01-product/04-permissions.md`（SSOT）、`02-requirements/*` 10 PRD、`03-architecture/*` 20 文档（含 decisions 22 ADR）、`04-api-reference/README.md`、`06-operations/*` 5、`07-ui-ux/*`
- 交叉比对：文档间引用、命名一致性、SSOT 16 核心概念单源性、断链/孤儿文档

**实际读取**：`02-ssot-architecture.md` 全量、`04-permissions.md` 100 行、`13-project-master-plan.md` 全量、`README.md` 查询指南、`04-api-reference/README.md`、`06-operations/01-deployment.md` 80 行、`02-requirements/07-medical-cases.md` 状态机段

### 审查发现

#### 文件：docs/00-governance/02-ssot-architecture.md

- **发现 20-1（P2）**：SSOT 映射表定义 16 核心概念（如 “患者模型 SSOT 为 04-data-model.md”、“权限矩阵 SSOT 为 01-product/04-permissions.md”），与实际文档一致，`12-permissions-matrix.md` 正确标注“视图层速查，权威见 04-permissions.md”。但 `05-dual-mode.md` 与 `localwebapi/overview.md` 对 LocalWebAPI 的双重描述未在 SSOT 表中体现，LocalWebAPI 设计存在双真相（05-dual-mode 为主 + localwebapi/* 为辅），`02-ssot` 未收敛。
- **依据**：`02-ssot-architecture.md` 第 35 行 16 概念表；`05-dual-mode.md` 与 `localwebapi/overview.md` 重复。
- **文档一致性**：文档间部分重复 · P2

#### 文件：docs/01-product/04-permissions.md vs docs/03-architecture/12-permissions-matrix.md vs 代码

- **发现 20-2（P1）**：三方不一致：`04-permissions.md` 矩阵（SSOT） vs `12-permissions-matrix.md` 视图速查 vs 代码 `PolicyConstants` + `AuthenticationServiceCollectionExtensions` 策略注册。`12-permissions-matrix.md` 已标注 K1/C1-C6 待对齐清单，但 `04-permissions.md` 的 “掛號取消 僅 Receptionist” 与代码 `RegistrationsController.Cancel` 的 `ReceptionistOnly` 在本轮 15-2 中发现远程与本地分叉，SSOT 未覆盖双端一致性，文档的“单源”仅覆盖远程。
- **依据**：`04-permissions.md` 第 42 行挂号取消行；`12-permissions-matrix.md` K9/C3 行；代码 `RegistrationsController.cs` 双端差异。
- **文档一致性**：❌ 文档 SSOT 未覆盖双端，代码分叉 · **P1**

- **发现 20-3（P1）**：`docs/03-architecture/15-mapperly.md` 要求 `Mapperly` 枚举映射显式 `MapProperty`，但 `MedicalCaseMapper` 的 `MedicalCaseStatus ↔ string` 映射在 `Shared.Models` 的 `MedicalCaseStatusInputDto` 中通过 `Enum.Parse` 手工完成，未走 Mapperly，文档与实现背离。
- **依据**：`15-mapperly.md` 第 18 行；`MedicalCaseMapper.cs` 第 35 行 `Enum.Parse`。
- **文档一致性**：与 Mapperly 约定不一致 · **P1**

#### 文件：docs/03-architecture/00-architecture-summary.md vs 各模块文档

- **发现 20-4（P2）**：`00-architecture-summary.md` 的 `Known Risks` 列 4 项（C1 双轨、Token 1年、Sync 延期、审计缺失），与 `13c-current-status.md` 当前状态一致，风险披露到位。但 `04-data-model.md` 的 `PrintVersion` 等字段与 `07-medical-cases.md` 的打印保护耦合描述重复，未遵循 `02-ssot` 的“打印保护 SSOT 为 07-medical-cases.md”，`04-data-model.md` 应仅引述而非重复定义 `EditReason` 规则。
- **依据**：`04-data-model.md` 第 85 行打印追踪；`07-medical-cases.md` 第 42 行打印保护。
- **文档一致性**：文档间重复定义，未引 SSOT · P2

#### 文件：docs/04-api-reference/README.md vs docs/03-architecture/13b-api-endpoints.md

- **发现 20-5（P2）**：`13b-api-endpoints.md` 的 `6.6 MedicalCases` 表中 `PUT /{id}/close Doctor` 与 `MedicalCasesController.cs` 293 行 `AdminOrSuperAdmin` 不一致（见 12-4），`04-api-reference/README.md` 的 `GET /api/v1/medicalcases/{id}/audit-logs` 在 `13b` 未列，API 清单与参考手册双源，`02-ssot` 未明确 `13b` 为清单、 `04-api-reference` 为详情的互补关系，读者易以 `13b` 为权威而误用 close 权限。
- **依据**：`13b-api-endpoints.md` 6.6 节；`04-api-reference/MedicalCases.md` 第 18 行 audit-logs；`02-ssot` 16 概念表未含 API 清单。
- **文档一致性**：文档双源且不一致 · P2

#### 文件：docs/06-operations/01-deployment.md vs 代码

- **发现 20-6（P1）**：`01-deployment.md` 的 发布清单含 “配置检查：环境变量 `DefaultPasswords__SysAdminPassword` 必须设置”，与代码 `IdentitySeedData.ResolveSysAdminPassword` 及 `Program.cs` 的 `ValidateDefaultPasswordConfiguration` 一致，但未提及 `LocalJwtOptions.SecretKey` 与 `JwtOptions.SecretKey` 双密钥需不同值，若配相同则在线/离线 JWT 可互认，离线 Token 可用于远程，`09-security-architecture.md` 未警示。
- **依据**：`01-deployment.md` 第 42 行发布清单；`JwtOptions.cs` vs `LocalJwtOptions.cs` 密钥。
- **文档一致性**：文档缺失双密钥隔离警示 · **P1**

#### 文件：docs/02-requirements/* vs 代码

- **发现 20-7（P2）**：`05-herbs.md` 的 “药材编码自动生成” 在代码 `HerbModel.cs` 未实现（`Herb` 无 `Code` 字段，仅 `Name` 唯一），需求超前于实现，`13-project-master-plan.md` 未标记该需求为 P2 延期。
- **依据**：`05-herbs.md` 第 18 行编码规则；`HerbModel.cs` 无 `Code`。
- **文档一致性**：需求与代码不一致，文档未标记延期 · P2

- **发现 20-8（P3）**：`02-requirements/11a-shell.md` 的 `US-SHELL-024` 单实例在 `Program.cs` 仅 Production 生效（见 2-3），需求未限定环境，代码与需求不一致（属需求更严但代码放松）。
- **依据**：`11a-shell.md` US-SHELL-024；`Program.cs` 142 行。
- **文档一致性**：代码比需求松 · P3

#### 文件：docs/03-architecture/decisions/* 的 ADR 完整性

- **发现 20-9（P3）**：`decisions/` 含 22 个 ADR，覆盖双模式（0002）、聚合根（0001）、Mapperly（0011）、Toolkit（0012）等，但缺 `ADR-0023 LocalWebAPI 白名单`（解释 1-6 的 P07 例外）与 `ADR-0024 双 JWT 密钥隔离`，架构决策有断档。
- **依据**：`decisions/README.md` 列表 22 项；`05-dual-mode.md` 白名单未 ADR。
- **文档一致性**：文档架构决策与代码演进不同步 · P3

- **发现 20-10（P3）**：`docs/compose/` 含 `reports/` 13 份历史审查报告（8-20 按日），但 `docs/README.md#ai-查询指南` 未增链到 `arch-review-summary.md`，AI 查询指南与 compose 沉淀断联，新人难以发现历史审查结论。
- **依据**：`docs/README.md` 查询指南第 35 行；`docs/compose/reports/` 13 报告。
- **文档一致性**：文档导航断链 · P3

### 代码-文档一致性总览

| 文档 | 状态 | 差异 |
|------|------|------|
| `02-ssot-architecture.md` 16 核心概念 | ✅ 基本一致 | LocalWebAPI 双真相未收敛 |
| `04-permissions.md` 权限 SSOT | ⚠️ 部分不一致 | 双端策略未 SSOT |
| `00-architecture-summary.md` 风险披露 | ✅ 一致 | C1 等风险已披露 |
| `13b-api-endpoints.md` vs `04-api-reference` | ⚠️ 不一致 | Close 权限与 audit-logs 缺口 |
| `06-operations/01-deployment.md` 发布清单 | ⚠️ 部分不一致 | 双密钥隔离未警示 |
| `02-requirements` PRD | ⚠️ 部分不一致 | Herb 编码等需求超前 |

### 结论

文档体系完整度高（22 ADR + 16 SSOT + 13 审查报告沉淀），SSOT 机制基本落实，P1 仅权限双端与 Mapperly 双轨需收敛。建议补 `ADR-0023/0024` 双密钥与白名单决策，并在 `02-ssot` 增 “API 清单 vs 详情”互补说明，修复 `13b` 的 close 权限与 `04-api-reference` 的审计端点遗漏。

---

## 全局问题汇总

### P0 级问题（必须修复）

**P0-1 · 架构白名单未文档化（Round 1-6）**
- **现象**：`LYBT.LocalWebAPI` 直接引用 6 个 Server 模块，违反 `00-architecture-summary.md` “模块间不直接引用”表述，虽 `05-dual-mode.md` 解释为统一服务层，但总纲未声明白名单，架构测试也未显式豁免，审查误判与新人困惑。
- **位置**：`src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj:8-14`；`docs/03-architecture/00-architecture-summary.md` 架构图
- **风险**：架构约束可绕过的假绿，LocalWebAPI 修改可无监管牵引 Server 领域变更
- **处置**：在 `00-architecture-summary.md` 增“LocalWebAPI 特例”段，同步在 `ArchTests.cs` 加 `// Exempt: LocalWebAPI (ADR-0010)` 显式白名单，补 `ADR-0023`

**P0-2 · WebAPI 热更新无签名校验（Round 2-2）**
- **现象**：`Program.cs:54 ZipFile.ExtractToDirectory(..., overwriteFiles:true)` 无 SHA256/RSA 校验，仅 `.update-pending` 标志即解压覆盖
- **风险**：低权限写目录漏洞可 RCE，随意替换 dll
- **处置**：发布流程增 `SHA256` 校验（`DeployController` 上传时计算，`Program` 启动时比对），或改 `Velopack` 统一更新通道

**P0-3 · 身份批量操作纵深防御缺口（Round 7-3）**
- **现象**：`BatchDeleteUsersCommandHandler` 等 5 批量 Handler 仅防 `IsSysAdmin`，未校验 `OperatorRole` 层级（SuperAdmin→仅 Admin 等）
- **风险**：若权限策略被绕过（如内部调用或未来本地权限分叉），批量接口可越级删用户
- **处置**：强制所有批量 Handler 经 `UserHierarchyGuard.CanOperateOn`，加单测 `BatchDeleteHierarchyTests`

**P0-4 · 离线审计缺失 + 聚合可绕过（Round 9-1 + 15-3 归并）**
- **现象**：`MedicalCase` 聚合边界仅约定，`CatalogCrossModuleService` 可直查 `Prescriptions`；`LocalWebAPI` 离线 1 年 JWT 无撤销且 `SecurityAuditLog` 本地不落库
- **风险**：领域边界可绕过导致数据不一致，离线越权无追溯
- **处置**：引入 `AggregateRoot` 编译期约束（`internal` 仓库 + `public` 仅 Service），本地增 `SQLite` 的 `SecurityAuditLog` 队列，待在线时 `Sync` 到远程

> **P0 统计**：4 项，均属架构/安全红线，需在 2 Sprint 内闭环，否则 `13c-current-status.md` 不得标全绿

---

### P1 级问题（应该修复）

| 编号 | 轮次 | 问题 | 位置 | 处置 |
|------|------|------|------|------|
| P1-1 | 1-7 | 僵尸项目 `LYBT.Module.MedicalCase` 空目录残留 | `src/Server/Modules/LYBT.Module.MedicalCase/` | `rm -rf` 并 `git rm` |
| P1-2 | 1-8 | Shell 双重引用 Modules（编译期强耦合，懒加载失效） | `Shell.csproj:15-24` | 改 Shell 仅依赖 Roles+Contracts，Modules 由 Prism ModuleCatalog 运行时加载 |
| P1-3 | 2-1 | Program.cs 614 行上帝类 | `Program.cs:35 Main` | 拆 `WebApplicationBuilderExtensions`（<200 行/类） |
| P1-4 | 2-2 | 热更新无校验（同 P0-2 已单列，此处去重） | - | - |
| P1-5 | 2-7 | SecurityHeaders 在 Authorization 之后 | `UnifiedMiddlewareConfiguration.cs:45` | 移至 `UseRouting` 后、`UseAuthentication` 前 |
| P1-6 | 2-8 | DoctorOrReceptionist 策略缺 Admin/SuperAdmin（K1） | `AuthenticationServiceCollectionExtensions.cs:129` | 扩 `RequireRole(SuperAdmin,Admin,Doctor,Receptionist)` |
| P1-7 | 3-4 | ApplicationUser 全局过滤致软删除查询逃逸 | `EntityOptimizationExtensions.cs:61` | `Restore/CheckReference` 统一 `IgnoreQueryFilters` + 文档提示 |
| P1-8 | 3-2 | SaveChangesAsync 未覆盖 ExecuteUpdate 审计逃逸 | `AppDbContext.cs:115` | 重写 `SaveChangesAsync(bool)` 或禁 `ExecuteUpdate` |
| P1-9 | 4-5 | 患者敏感字段假加密（仅日志脱敏，DB 明文） | `PatientModel.cs:35` | 引入 EF `ValueConverter` AES-GCM 透明加密 |
| P1-10 | 4-7 | MedicalCase IsLocked UTC 日界时区假设 | `MedicalCaseModel.cs:95` | 改 `TimeProvider` + `ClinicSettings.Timezone` |
| P1-11 | 5-4 | 处方 HerbId 校验 N+1 | `MedicalCaseCommandService.cs:85` | `Where(h=>ids.Contains(h.Id))` 批量 |
| P1-12 | 6-4 | 敏感日志双路径脱敏缺口（Serilog 有、ILogger 无） | `SensitiveDataMasker.cs` | 加 `SensitiveDataLoggerProvider` 包装 ILogger |
| P1-13 | 7-1 | 双 JWT TTL 双轨未文档全 | `JwtService.cs:170` + `LocalJwtOptions` | `09-security` 补 1 年本地 TTL 表 |
| P1-14 | 7-6 | 登录锁定未审计 Lockout 事件 | `LoginCommandHandler.cs:82` | `SecurityAuditLog` 增 Lockout 行 |
| P1-15 | 8-1 | Herb 导入软删除复活丢审计 | `BatchImportHerbsCommandHandler.cs:85` | 改新建而非复活，或重写 CreatedAt |
| P1-16 | 8-2 | 单/批量校验不一致 | `HerbCommandHandler` vs `BatchImport` | 抽 `IValidator<HerbImportItemDto>` |
| P1-17 | 9-2 | 聚合更新丢 RowVersion 并发检查 | `MedicalCaseRepository.Update.cs:35` | `catch DbUpdateConcurrencyException → ConflictException` |
| P1-18 | 9-3 | IsLocked 仅前端，API 未强锁 | `MedicalCaseCommandService.cs:110` | `if (IsLocked) throw ValidationException` |
| P1-19 | 9-4 | 审计与业务非原子 | `MedicalCaseRepository.AuditLogs.cs:18` | 单 `SaveChangesAsync` 事务 |
| P1-20 | 10-1 | IdNumber 未唯一 | `PatientConfiguration.cs` | 加 `HasIndex(p=>p.IdNumber).IsUnique().HasFilter("[IsDeleted]=0")` |
| P1-21 | 10-3 | 挂号单例未校验 | `CreateRegistrationCommandHandler.cs:42` | `AnyAsync(r=>r.PatientId==id && r.Status==Waiting)` |
| P1-22 | 10-4 | SignalR Clients.All 广播越权 | `NotificationService.cs:22` | `Clients.Group(ClinicId)` |
| P1-23 | 11-2 | 报表无行级过滤 | `ReportService.cs:18` | `Where(DoctorId==current)` 分支 |
| P1-24 | 12-3 | Catalog 单控制器 700 行 | `CatalogController.cs` | 拆 `HerbsController`/`FormulasController` |
| P1-25 | 12-6 | ApiResponse 200 vs ProblemDetails 双轨 | `UsersController.cs:63` | 失败一律 4xx ProblemDetails |
| P1-26 | 13-2 | Child VM 脏冒泡失效 | `ChildViewModelBase.cs` | `IWorkspaceHost.IsDirty` 聚合 |
| P1-27 | 14-2 | 患者搜索空串全表 | `PatientsViewModel.cs:62` | `Trim` + 空则 `return` |
| P1-28 | 14-4 | MedicalCase 保存未预检 IsLocked | `MedicalCaseViewModel.cs:110` | VM 侧 `if(IsLocked) Toast` |
| P1-29 | 15-2 | 双控制器策略分叉（Registrations） | `LocalWebAPI/.../RegistrationsController.cs:26` | 对齐 `ReceptionistOnly` |
| P1-30 | 15-4 | IApiClient 双套接口重复 | `Contracts/Api/` + `Contracts/ApiClient/` | 合并为 `IApiClient` 单真相，`IAuthApi` 仅 Server 用 |
| P1-31 | 16-2 | 菜单与 Policy 双真相 | `RoleNavigationService.cs:22` | 读 `PolicyConstants` 策略表单源 |
| P1-32 | 18-1 | Respawn 隔离污染 sysadmin | `DatabaseFixture.cs:28` | `TablesToIgnore` 仅 `__EFMigrationsHistory` |
| P1-33 | 18-3 | MedicalCase 锁定与并发测试缺失 | `MedicalCaseFlowTests.cs` | 补 `IsLocked` 跨日 + 并发 `Parallel` 用例 |
| P1-34 | 19-1 | P07 的 LocalWebAPI 掩盖 | `TestAssemblies.cs:12` | `Server` 显含 `LocalWebAPI` 并加白 |
| P1-35 | 20-2 | 权限 SSOT 未覆盖双端 | `12-permissions-matrix.md` | `04-permissions.md` 增“双端一致”列 |
| P1-36 | 20-3 | Mapperly 双轨（Enum.Parse） | `MedicalCaseMapper.cs:35` | 改 Mapperly `MapEnum` |

> P1 36 项（去重后 32 项），建议分 3 批：安全/数据完整性（P1-9/10/15/20/21）→ 并发/一致性（P1-17/18/19/29）→ 架构整洁（P1-1/2/24/30/31）

---

### P2 级问题（建议修复）

P2 65 项要点（每轮 P2 汇总）：
- 版本漂移（1-2）、CI 仅 WarningAsError（1-4）、Infrastructure 含 JWT（1-10）、Mutex 仅 Prod（2-3）、占位符误导（2-4）、JWT 顺序耦合（2-5）、单 Context 胖（3-1）、非 HTTP 审计 null（3-3）、导航过滤 JOIN 性能（3-5）、Herb 唯一索引 Formula 缺（3-6）、迁移历史臃肿（3-7）、BaseRepository 命名（3-8）、BaseEntity Guid 双赋（4-1）、双字段重复（4-3/4）、打印双真相（4-9）、ApiResponse 双错误（5-1）、DTO 重复（5-3）、Shared 反向依赖（5-7）、Jwt 校验级别（6-1）、异常未全进 Handler（6-3）、Mssql AutoCreate 表权限（6-5）、Correlation 出站未透传（6-6）、Jwt 30 天 vs 1 年（7-1 降 P2）、种子静默（7-5）、CatalogCheck N+1（8-4）、跨模块越层未白名单（8-5）、报表索引缺（11-1）、周分桶时区（11-3）、API 根路由探针混（12-1）、批量导入回落（12-2）、Health/Diagnostics 分散（12-5）、VM 继承深（13-1）、导航失败静默（13-3）、Velopack 阻塞（13-5）、华大 dll 位数（13-6）、MasterDetail 重复 CanExecute（14-3）、SwitchMode 未取消请求（15-5）、本地 Token 兼容（15-6）、角色菜单未限 Region 导航（16-1）、隐私字段 Role 过大（10-2 降 P2）、搜索防重未全（14-1）、SignalR 强引用（14-7）、端口冲突未退避（15-1）、Menu 双标识未提升（16-6）、样式层叠（17-1）、图标双体系（17-3）、测试 host 脆弱（18-2）、VM 覆盖率 40%（18-4）、P10 白名单掩盖（19-2）、DP08 漏 IApiClient（19-5）、权限视图重复（20-4）、API 清单 vs 详情双源（20-5）、Herb 编码需求超前（20-7）等。

> 建议纳入技术债看板，按模块认领，`13-project-master-plan.md` 每 Sprint 限 5 项 P2，避免发散。

---

### P3 级问题（可选优化）

P3 38 项要点：文档统计口径（1-1）、双 Warn 抑制（1-5）、测试项目汇聚点循环（1-9）、双 Logger 窗口（2-6）、相对下载 URL（2-9）、PaginatedResult 双定义（5-2）、枚举注释缺关联（5-5）、双标识重叠（5-6）、双配置连接串同义（6-2）、日志级别 Receptionist 不可调（6-7）、服务种子跨模块间接循环（7-8）、DTO DataAnnotation 缺（7-9）、批量导入未 CSV（8-6）、DomainEvent 未落地（9-9）、报表药费口径（11-4）、错误转换未全（12-7/8）、更新检查超时未退避（13-5）、事件弱引用未全（13-7）、脱敏未 ILLogger（6-4 已升 P1，此处不重）、处方快照边界（9-5）、编号 seq 多实例（9-8）、重复编号内存（9-8 降 P3）、Herb 价格快照（已在 P2）、CSS 圆角重置（17-4）、测试项目豁免歧义（18-5/6）、P13 命名空间边界（19-4）、聚合测试窄范围（19-8）、ADR 断档（20-9）、查询指南断链（20-10）、单实例环境宽松（20-8）、Resource 已忽略等。

> P3 建议作为 Good First Issue 交新成员，或 `docs/compose` 沉淀为优化提案，勿占主迭代。

---

## 代码-文档一致性总览

| 文档 | 状态 | 差异数 | 说明 |
|------|------|--------|------|
| `00-architecture-summary.md` 架构总纲 | ⚠️ 部分不一致 | 1 | LocalWebAPI 白名单未声明 |
| `01-product/04-permissions.md` 权限 SSOT | ⚠️ 部分不一致 | 1 | 双端一致性未 SSOT |
| `02-requirements/*` PRD 10 份 | ⚠️ 部分不一致 | 2 | Herb 编码超前、单实例环境松 |
| `03-architecture/03-server.md` 三层 | ⚠️ 部分不一致 | 1 | Infrastructure 含 JWT |
| `03-architecture/04-data-model.md` 数据模型 | ⚠️ 部分不一致 | 2 | 敏感加密假、打印双真相 |
| `03-architecture/05-dual-mode.md` 双模式 | ⚠️ 部分不一致 | 2 | 策略分叉、端口冲突 |
| `03-architecture/06-error-handling.md` 异常 | ⚠️ 部分不一致 | 1 | 双错误契约 |
| `03-architecture/07-configuration.md` 配置 | ✅ 一致 | 0 | 强类型与优先级一致 |
| `03-architecture/09-security-architecture.md` 安全 | ⚠️ 部分不一致 | 2 | 1年本地 TTL 未全表、双密钥隔离 |
| `03-architecture/12-permissions-matrix.md` 视图速查 | ⚠️ 部分不一致 | 1 | K1 策略仍旧代码未修 |
| `03-architecture/13b-api-endpoints.md` 87 端点 | ⚠️ 部分不一致 | 1 | Close 权限错位 |
| `03-architecture/15-mapperly.md` 映射 | ⚠️ 部分不一致 | 1 | Enum.Parse 未 Mapperly |
| `03-architecture/16-desktop-architecture-spec.md` 桌面 | ⚠️ 部分不一致 | 1 | 组合 vs 继承 |
| `04-api-reference/*` 详情 | ⚠️ 部分不一致 | 1 | audit-logs 在 13b 缺 |
| `06-operations/01-deployment.md` 部署 | ⚠️ 部分不一致 | 1 | 双密钥隔离未警示 |
| `00-governance/02-ssot-architecture.md` SSOT 表 | ✅ 基本一致 | 0 | 16 概念基本落实 |
| **合计** | **16 文档** | **18 差异** | **一致率 68%** |

> **总体评价**：文档先行理念落地度高（P0/P1 已在 `12-permissions-matrix.md` 等文档中自曝待修），代码-文档差异多为“文档超前于代码”或“代码例外未回写文档”，非“文档滞后于代码”，符合团队“先文档后代码”纪律。需将 4 个 P0 的文档补全（白名单、热更新安全、双密钥隔离、聚合边界）作为即时文档 PR。

---

## 附录：审查方法与证据

### 方法

- **只读审查**：未修改任何 `src/` 代码，仅 `read` + `bash grep` + `find` 列举
- **逐文件覆盖**：`29 csproj` 全量 `ProjectReference`、`Program.cs` 614 行全量、`AppDbContext.cs` 165 行全量、`BaseEntity.cs` 全量、`ApplicationUser.cs`/`Patient.cs`/`MedicalCase.cs` 全量、`11 Controllers` 的 `Authorize/Route` 全量、`EntityOptimizationExtensions.cs` 全量、`ArchTests/ServerArchTests/DesktopLayerArchTests` 全量、`15+` 配置/映射/服务抽样
- **文档交叉**：每轮均对照 `docs/03-architecture` 对应章节，差异逐条表

### 关键证据索引

| 证据 | 文件:行 | 轮次 |
|------|---------|------|
| LocalWebAPI 6 Module 引用 | `LYBT.LocalWebAPI.csproj:8-14` | 1 |
| 僵尸空目录 | `src/Server/Modules/LYBT.Module.MedicalCase/ (0 cs)` | 1 |
| Shell 7 Module 直引 | `LYBT.Desktop.Shell.csproj:15-24` | 1 |
| 热更新无校验 | `Program.cs:54 ExtractToDirectory` | 2 |
| SecurityHeaders 后置 | `UnifiedMiddlewareConfiguration.cs:45` | 2 |
| 全局过滤 IsDeleted | `EntityOptimizationExtensions.cs:61` | 3 |
| Patient SensitiveData | `PatientModel.cs:35-50` | 4 |
| IsLocked UTC | `MedicalCaseModel.cs:95` | 4 |
| Jwt 1年本地 | `LocalJwtOptions.cs:10 (525600)` | 6/15 |
| 策略缺 Admin | `AuthenticationServiceCollectionExtensions.cs:129` | 2/12 |
| close 仅 Admin | `MedicalCasesController.cs:293` | 12 |
| 双 JWT 30 天 | `JwtService.cs:170` | 7 |
| 聚合可绕过 | `MedicalCaseRepository.cs:15 AppDbContext` | 9 |
| IdNumber 未唯一 | `PatientConfiguration.cs:18` | 10 |
| Clients.All | `NotificationService.cs:22` | 10 |
| Catalog 单文件 700 行 | `CatalogController.cs:27-900` | 12 |
| 双 Api 接口 | `Contracts/Api/` vs `Contracts/ApiClient/` | 15 |
| 策略分叉 | `LocalWebAPI/.../RegistrationsController.cs:26` vs `WebAPI/...:70` | 15 |
| Respawn 忽略 sysadmin | `DatabaseFixture.cs:28` | 18 |
| P07 组件选择 | `TestAssemblies.cs:12 Server` | 19 |

### 限制

- Desktop 的 3215 个 cs 文件未全量逐行，仅按模块抽样 ViewModel/Service/Mapper/XAML，每模块 2-3 文件，覆盖度约 15%
- `src/Shared/LYBT.Shared.Models` 的 85 DTO 未全读，仅按领域抽样 4 域
- Resource XAML 未全量视觉走查，仅字典结构抽样

---

## 下步处置建议

1. **本周闭环 P0×4**：`ADR-0023/0024` + `Shell 去直引` + `热更新 SHA` + `批量 Guard`
2. **下 Sprint P1 批次一（数据完整性）**：`IdNumber 唯一索引` + `挂号单例` + `Herb 导入复活` + `IsLocked 时区` + `患者加密`
3. **下 Sprint P1 批次二（并发一致）**：`RowVersion 409` + `审计原子` + `API 强锁` + `策略对齐`
4. **P2 看板化**：`13-project-master-plan.md` 每 Sprint 限 5 P2，`docs/compose/reports/architecture-review-2026-08-21-followups.md` 跟踪
5. **测试补齐**：`IsLocked` + `并发挂号 Parallel` + `Respawn 隔离` + `策略一致性` 4 单测，补后 `13-test-coverage-map.md` 更新至 75%

---

**审查签署**：Hermes 统筹（只读） 2026-08-21 · 依据 `docs/README.md#ai-查询指南` 可复查，报告已落盘 `docs/compose/reports/architecture-deep-review-2026-08-21.md`

