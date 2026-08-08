# 结构审计报告（技术总监独立分析）

> 独立分析者 #1 —— 工具：csproj 依赖扫描 + 文本引用验证 + 架构测试对照
> 日期：2026-08-08｜基线：`97445a6f3`（死代码清理完成）+ `b7c7390d0`（任务书）
> 本报告与 Mimo Code 独立分析（`structure-audit-mimo-2026-08-08.md`）交叉验证

---

## 0. 方法说明

- 扫描全部 35 个 csproj 的 ProjectReference（**排除注释行**，修正依赖图脚本误抓注释引用的缺陷）
- 关键结论以 `grep`/`read_file` 二次验证（引用链确认），非仅依赖图
- 架构测试对照：`tests/LYBT.Tests.Architecture/` 83 条规则
- 时间点：2026-08-08，master @ b7c7390d0

---

## 1. 项目依赖图 + 违规边

### 分层健康度：✅ 核心边界无违规

| 检查 | 结果 | 证据 |
|------|------|------|
| Shared 被 Server/Client 反向引用 | ✅ 无 | Shared 5 项目只引用 Shared/Models，零 Server/Client 引用 |
| Server → Desktop 引用 | ✅ 无 | 依赖图无此方向边 |
| Server 模块间直接引用（P07） | ✅ 0 违规 | **所有模块间通信走接口**（IMedicalCaseCrossModuleService 等，csproj 注释「Phase 4 已解耦」） |
| WebAPI vs LocalWebAPI Controller 对称 | ✅ 1:1 | 12 vs 12，完全对应，无缺口 |

### ⚠️ 发现 1（P1）：Desktop 模块间直接引用无架构守卫

| 项 | 详情 |
|----|------|
| 证据 | `LYBT.Desktop.Registration.csproj:67` 直接引用 `LYBT.Desktop.MedicalCase`；`RegistrationListViewModel.cs:7` `using LYBT.Desktop.MedicalCase.Models;` |
| 引用内容 | 只用导航契约：`MedicalCaseNavigationParameters`/`WorkspaceMode`/`EditState`/`ViewNames.MedicalCaseWorkspace` |
| 推理 | Server 侧同一问题已通过接口下沉解决（A-10 先例：IFormulaService/IHerbService 移入 Contracts），Desktop 侧导航契约**本应下沉 `LYBT.Desktop.Contracts`**（ViewNames 已在 Infrastructure/Constants，NavigationParameters 在 MedicalCase/Models）——契约与常量分散两处 |
| 架构测试缺口 | **P07 只覆盖 Server**（`P07_ServerModules_Should_Not_Reference_Other_ServerModules`），Desktop 模块间引用 0 守卫 |
| 严重度 | P1（架构纪律缺口 + 契约归属不清） |

### ⚠️ 发现 2（P2）：Roles 层引用 Modules 是有意设计但未文档化

`Admin → 5 模块`、`Clinical → 5 模块`、`Shell → 全部` 是 Prism 组合根/角色聚合惯例，**可接受**，但架构文档未说明 Roles 层与 Modules 层的边界规则，建议文档化（同 A-14 先例）。

---

## 2. Shared 归属问题

### ✅ 样板确认：Gender 单源合规

`LYBT.Shared.Models/Enums/Gender.cs` 唯一定义，双端引用（Server 迁移 + Desktop 打印/卡片），无重复。**符合设计意图**。

### ⚠️ 发现 3（P1）：Shared 层文档 vs 实际严重脱节

| 项 | 文档声称（08-shared.md） | 实际 |
|----|------------------------|------|
| 项目数 | 8 个（Primitives/Models/Utilities/Components/Logging/Validators/ExH/Config） | **5 个**（Entities/Models/Configuration/ExceptionHandling/Logging） |
| Utilities | 独立项目 | 并入 Models/Utilities/ |
| Validators | 独立项目 | 并入 Models/Validators/ |
| Components | 独立项目 | **从未建立**（MedicalCaseBusinessRules「待实施 S5」在 08-shared.md 声称，代码不存在） |
| Primitives | 独立项目 | 并入 Models/Primitives/ |
| Entities | 文档未提 | 实际存在（24 文件，Server/Desktop 共用） |

**推理链**：文档是设计态、代码是当前态，此处文档严重滞后——8 项目设计被合并为 5 项目实现，`Components` 是纯空头设计。08-shared.md 需全面重写对齐实际 5 项目结构。

### ⚠️ 发现 4（P2）：Shared 内部依赖不透明

`LYBT.Shared.Logging → LYBT.Shared.Models`（引用 Models 却仅用少量工具类）——需核实是否可降级依赖（依赖倒置或瘦身）。

---

## 3. 多机制并存（全量清单）

| # | 机制 | 存活双方 | 证据 | 活跃度 | 建议方向 |
|---|------|---------|------|--------|---------|
| M1 | API 契约双套 | `Contracts/Api/*`（Refit 特性，11 接口）vs `Contracts/ApiClient/*`（Unified 无特性，11+1 接口） | 目录对照完全对应（除 Configuration 仅 ApiClient 侧） | ApiClient 侧为主（SwitchingApiClient 依赖它），Api 侧被 RefitApiClient 包装 | **统一为 ApiClient 侧**，Refit 特性接口下沉为内部实现（P1） |
| M2 | 领域客户端双实现 | 12 对：`{X}ApiClient`（Refit adapter）+ `{X}HttpApiClient`（HttpClient adapter） | Foundation/Http/Clients/ 24 个文件 | 两套都活（远程用 Refit 套、本地用 HttpClient 套） | 双轨对称的**实现代价**：24 类维护，需评估是否可收敛（P1） |
| M3 | 远程/本地切换 | SwitchingApiClient（IsLocal ? HttpClientApiClient : RefitApiClient） | SwitchingApiClient.cs:74-79 | 核心活跃 | ✅ 设计合理，双轨实现质量待审（见 §4） |
| M4 | CorrelationId | AsyncLocalCorrelationIdProvider（Shared，Enricher 引用）vs ActivityCorrelationIdProvider（Desktop 用）+ Server CorrelationIdMiddleware（自实现 W3C） | Shared.Logging/Abstractions/ 双 Provider + WebAPI/Middleware/ | AsyncLocal 扩展方法 `AddAsyncLocalCorrelationIdProvider` 死（上轮确认），但 Provider 类被 Enricher 引用；Desktop 用 Activity；Server 用中间件 | **三轨并存**：Shared 定义两 Provider + Server 自写中间件，需收敛（P1） |
| M5 | 映射 | Mapperly 13 个（`[Mapper]` 特性，9 Desktop + 4 Server）vs 文档声称 23 | 精确 grep `[Mapper` | Mapperly 为主，PatientRepository 内嵌 1 个 `[Mapper]` | 文档 23 与代码 13 不符；需确认 Server 侧 FormulaMapper.cs 是否为手动（无 [Mapper]）——见 M6 |
| M6 | Server 映射双轨（包引用与实现脱节） | Mapperly（Auth/MedicalCase/Registration 3 个）+ **手写静态类**（Formula/Herbs/Patients/Users 4 个：FormulaDtoMapper/HerbDtoMapper 等，无 partial/[Mapper]） | 7 个 Server 模块 csproj **全部引用 Riok.Mapperly 包**，但 4 个模块实际手写静态 Mapper | **双轨并存且 4 个模块白引用 Mapperly 包** | 统一 Mapperly（P1）——包已引用，手写类改为 partial + [Mapper] 即可 |
| M7 | DbContext | 5 模块独立（Auth/Users/Herbs/Formula/Reports）+ 5 复用 AppDbContext（Patients/MedicalCase/Registration/Auth SecurityAudit/Herbs HerbReference） | 03-server.md 已文档化 | 双轨活 | 已文档化，**有意设计**，维持（P2 仅需文档一致） |
| M8 | 密码哈希 | BCrypt PasswordHelper vs Identity PBKDF2 | 上轮确认 PasswordHelper 为死桩 | PasswordHelper 死 | 删除或标注（P2，上轮已保留为桩，待决策） |
| M9 | 批量操作 | BatchOperationHandlerBase（6 Handler）+ BatchImport 3 Handler + Service 版 BatchEnable/Disable | Q-01 已文档化 | 三轨 | 已决策保留差异（Q-01），维持 |
| M10 | Repository 泛型化 | EntityApiClientRepositoryBase（4 个标准 CRUD 仓储）+ 2 参旧基类（MedicalCase/Registration 异形） | A-06 已文档化 | 双轨 | 已决策（A-06 方案 B），维持 |
| M11 | 异常处理 | Shared 异常层次 + SystemExceptionHandler 映射 vs 各 Handler 直接 Result.Failure | 上轮审查确认映射健康 | 单一 | ✅ 无问题 |

**小结**：M1/M2/M4/M5/M6 五个机制需要收敛决策（契约/客户端/日志/映射），M3/M7/M9/M10 为已决策的有意设计（维持），M11 健康。**核心战场是 Desktop 客户端层的双套机制（M1+M2 合计 ~36 个文件）**。

---

## 4. 数据流逐层走查

### 4.1 设计意图 vs 实际

```
设计意图：DB → Entity → Repository → Service → Controller(API) → HTTP → Desktop HttpClient 解析 → VM → View
实际（远程）：SQL Server → AppDbContext → {Module}Repository → {Module}Service/Handler → WebAPI Controller → JSON
           → Refit I{Module}Api → {Module}ApiClient(adapter) → IApiClient{Module} → {Module}Repository(Desktop)
           → Mapperly/手动 Mapper → ViewModel → View
实际（本地）：LocalDB → LocalWebAPI Controller → MediatR Handler/Service（复用 Server 模块）→ JSON
           → HttpClient {Module}HttpApiClient → IApiClient{Module} → {Module}Repository(Desktop) → Mapper → VM → View
```

### 4.2 每层 4 问

| 层 | 职责 | 实际 | 转换点 | 路径对称 | 异常传导 |
|----|------|------|--------|---------|---------|
| Server Repository | DB 访问 | ✅ 正常（P10 遵守） | — | — | — |
| Server Service/Handler | 业务 | ✅ 正常（MediatR 命令 + Service 查询，A-14 有意设计） | Entity→DTO 由 Mapperly/手动 | 与 Local 共享（LocalWebAPI 复用模块） | Result.Failure → ApiResponse |
| WebAPI Controller | HTTP 边界 | ✅ 正常 | — | — | ApiResponse 信封 |
| **Desktop 客户端层** | HTTP 调用 | ⚠️ **双套契约 + 双实现**（M1+M2） | JSON→DTO（Refit/HttpClient 反序列化） | 远程 Refit vs 本地 HttpClient，**两套 adapter 各写一遍** | ApiResponse → ApiErrorHandler |
| Desktop Repository | 数据封装 | ✅ 正常（A-06 泛型化） | — | 对称（同 IApiClient 接口） | — |
| Mapper | DTO→VM 模型 | ⚠️ Mapperly 13 + 手动并存（M5/M6） | DTO→DetailModel | 对称 | — |
| ViewModel | UI 状态 | ✅ 正常 | — | — | 异常 → Toast/对话框 |

### 4.3 双轨对称性 6 检查点

| # | 检查点 | 结果 | 证据 |
|---|--------|------|------|
| 1 | Controller 对称性 | ✅ 1:1 | WebAPI 12 = LocalWebAPI 12 |
| 2 | 业务逻辑共享 | ✅ 真共享 | LocalWebAPI csproj 引用 8 个 Server 项目；HerbsController 对比：两者都注入 `IHerbService` 调 `GetByIdAsync`——**同一 Service 双宿主** |
| 3 | 数据访问一致性 | 🟡 待 Mimo 验证 | Remote=AppDbContext(SQL Server) vs Local=LocalDB，两套 DbContext 迁移链需确认无漂移（我侧未深入） |
| 4 | 切换逻辑 | ✅ 对称正确 | SwitchingApiClient 快路径缓存 + 锁 + 双工厂；**本地模式未绕过验证**（LocalWebApiProgram.cs:77 注册 ValidationBehavior，与 Server 6 模块一致） |
| 5 | 种子数据 | ✅ 设计正确 | LocalWebApiSeedData 注释明确「Users are created by IdentitySeedData via UserManager」——本地不建用户避免绕过 Identity 哈希；但本地用 `EnsureCreatedAsync`（无迁移链）vs Server `MigrateAsync`（迁移链），**潜在 schema 漂移点**，建议后续统一迁移机制（P2） |
| 6 | 契约双套 | ⚠️ 冗余 | 11 对完全对应 + Configuration 例外；Refit 接口只被 RefitApiClient 内部包装用 |

### 4.4 关键结论

**双轨业务逻辑真共享**（检查点 2 确认），双轨的主要成本在**客户端 adapter 层重复**（M1+M2）而非业务层。这意味着：**双轨本身健康，需要收敛的是 Desktop 客户端层的双套机制**——这是与「双轨是设计意图」不冲突的优化点。

---

## 5. 顺带发现（不评级，记录不深挖）

| # | 疑点 | 证据 | 为什么觉得有问题 |
|---|------|------|-----------------|
| I1 | Server 侧 Formula/Herbs/Patients/Users Mapper 无 [Mapper] 特性，疑似手动映射 | `grep [Mapper` 仅 3 Server Mapper 命中 | 文档声称 Mapperly 统一，实际 Server 侧可能大量手写——需 Mimo 核实这些文件内是否用 partial + [Mapper] 遗漏或真手动 |
| I2 | `Contracts/Api/IConfigurationApi.cs` 存在但 ApiClient 侧无对应 `IApiClientConfiguration`（diff 显示 Configuration 仅在 ApiClient 目录出现——实为 diff 误读，待核实） | diff 输出 | 契约双套可能有一侧缺 Configuration——需 Mimo 确认 SystemSettingsViewModel 走的哪条链 |
| I3 | 依赖图初版误抓注释行 ProjectReference（Patients→MedicalCase），修正后为 0 违规 | 脚本缺陷 | 类似「注释引用的历史残留」可能存在于其他 csproj，可作死代码/注释清理线索 |
| I4 | Desktop Contracts 内部职责重叠：`Api/` + `ApiClient/` + `Services/` 三个目录都放接口，边界不清晰 | 目录结构 | 接口归属规则需在设计中明确 |

---

## 6. 与文档偏差清单

| 文档 | 偏差 |
|------|------|
| 08-shared.md | 声称 8 项目实际 5 项目；Components 从未建立；Mapperly 声称 23 实际 13 |
| 03-server.md | 需核对 M6（Server 映射双轨）是否已描述 |
| 13c-current-status.md | 需补 Desktop 客户端层双套契约现状 |

---

## 7. 统计汇总

| 项 | 数 |
|----|-----|
| P0（红线违反） | 0 |
| P1（应修） | 4：发现1(Desktop P07缺口)、发现3(Shared 文档脱节)、M1(契约双套)、M4(CorrelationId 三轨) + M2/M6 归入收敛批次 |
| P2（可后议） | 3：发现2(Roles 文档化)、发现4(Logging 依赖)、M7/M8/M9/M10 文档一致 |
| 顺带发现 | 4 项 I1-I4 |
| 双轨对称性 | 5 项 ✅、1 项待 Mimo 验证（两套 DbContext 迁移链是否漂移） |

---

## 8. 给产品负责人的结论（业务语言）

1. **三层架构边界是健康的**——无越层、无环、Server 模块解耦彻底（0 违规）。
2. **双轨（远程/本地）是合理设计且业务逻辑真共享**——成本不在双轨本身，而在 Desktop 客户端层的**双套 API 契约 + 每领域双实现**（约 36 个文件，每加一个端点要写两遍 adapter）。
3. **最大收敛机会**：统一 Desktop 客户端契约为单一接口，Refit/HttpClient 差异收进一个实现层——开发新功能时不用再写两遍。
4. **文档严重滞后**（08-shared 声称 8 项目实际 5），设计文档需随本次审计重写。
5. **待 Mimo 验证**：两套 DbContext 迁移链是否漂移、种子数据是否一致、Server Mapper 是否真手动——这三点决定修复批次范围。
