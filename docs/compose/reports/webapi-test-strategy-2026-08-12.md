# WebAPI 后端测试方案设计（专家评估 + 分层方案）

> 日期：2026-08-12 | 状态：设计稿（待用户确认） | 范围：**仅 WebAPI 后端**（Desktop 测试另行设计）
> 依据：上线实战 10 坑 + 测试实证盘点 + ASP.NET Core 官方 WebApplicationFactory 模式 + Respawn 测试隔离

---

## 一、现状评估（实证）

### 1.1 现有测试体系盘点

| 层 | 数量 | 覆盖 | 评价 |
|----|------|------|------|
| Server Unit | 57 文件 / 620+ 用例 | Service/Validator/Repository 逻辑 | ✅ 守护业务逻辑（0 mock 是优点） |
| Desktop Unit | 54 文件 | VM/StateMachine | ✅ 桌面逻辑 |
| Desktop Integration | 34 文件 | **LocalWebAPI** 真实启动（5300） | ⚠️ 守护本地端，非 Remote |
| Architecture | 7 文件 / 87 规则 | 依赖方向/架构约束 | ✅ 架构守门 |
| **Remote WebAPI 系统级测试** | **0** | **从未真实启动 Remote WebAPI** | ❌ **核心盲区** |

### 1.2 关键事实

- `Microsoft.AspNetCore.Mvc.Testing`（WebApplicationFactory）**已引用但从未使用**
- `Respawn`（数据库重置）**已引用但从未使用**
- InMemory 数据库 **11 处**（不校验 EF 列映射 → 掩盖 DbContext 配置错误）
- 真实 SQL Server **0 处**
- Program.cs 为 `public class Program`（注释明示「确保 WebApplicationFactory 完全兼容性」——**工具链早已备好，只差用**）

### 1.3 上线 10 坑 vs 测试覆盖

| # | 上线问题 | 类型 | 现有测试能否拦截 |
|---|---------|------|:---:|
| 1 | 路由 version 重复（启动崩溃） | 启动契约 | ❌（无全量路由注册测试） |
| 2 | IdentityDbContext 漏映射（登录崩溃） | 集成契约 | ❌（InMemory 不校验列映射） |
| 3 | SqlServerHealthCheck fallback | 集成契约 | ❌（无连接串解析测试） |
| 4 | JWT 必须 Base64 | 配置校验 | ❌（测试不跑 Production 校验器） |
| 5 | 缺 NEWUSER_PASSWORD | 配置校验 | ❌ |
| 6 | Encrypt=True 握手失败 | 环境差异 | ⚠️（需真实 SQL） |
| 7 | 环境变量双下划线 | 配置语义 | ❌（测试不模拟环境变量覆盖） |
| 8 | Swagger 生产关 | 设计决策 | ❌ |
| 9 | Swagger 401（FallbackPolicy） | 中间件契约 | ❌ |
| 10 | Swagger 空白（CSP） | 安全头契约 | ❌ |

**结论**：10 坑中 **9 个现有测试无法拦截**——因为全是「系统级」问题，而系统级测试 = 0。

---

## 二、为什么之前没测出问题（根因分析，2026-08-12 用户追问）

**根本原因一句话**：测试体系和被测试对象**层级错配**——所有测试都测「组件」，而发布问题全在「系统」层面。组件全绿 ≠ 系统能跑。

### 2.1 逐坑解释（为什么每个问题测不出来）

**① 路由 version 重复（启动崩溃）——为什么测不出？**
```
bug 在 MapControllers() 时抛出（构造完整路由表时）
现有测试：调用某 action → 断言返回 200 ✅
没测的：  启动 WebAPI → 注册全部路由 → 此时才发现 version 重复 ❌
```
测试从没真实启动过 WebAPI——只测了每个零件，但从没给机器通电。

**② IdentityDbContext 漏映射（登录崩溃）——为什么测不出？**
```
现有测试用 InMemory 数据库（11 处）
InMemory 特性：不校验 EF 列映射——写 User.LastLoginTime，InMemory 就存
真实 SQL Server 特性：列名不存在 → Invalid column name ❌
```
InMemory 太宽容——把「映射配置错了」这个最危险的错误掩盖了。测试全绿，因为测试环境根本不检查这个。

**③ SqlServerHealthCheck fallback——为什么测不出？**
这个**没有任何测试**——测试设计标准是「测业务逻辑」，连接串 fallback 是「基础设施逻辑」被认为不值得测。上线时两处解析逻辑不一致 → 健康检查挂。

**④ 配置类问题（JWT Base64 / 缺密码 / Encrypt / 双下划线）——为什么测不出？**
```
测试跑的环境：Development 配置（或 mock 注入）
上线跑的环境：Production 配置 + 环境变量占位符 ${} + 真实服务器
```
测试环境和部署环境是两个世界——测试从没跑过「Production 配置 + 占位符 + 环境变量注入」路径，校验器从没在测试中被触发过。

### 2.2 为什么会这样？（设计决策的偏差）

测试设计当时的标准是**「单元逻辑正确性」+「AC 需求映射」**——证明「每个组件按需求工作」。
这个标准漏掉了三个问题：
1. **系统能启动吗？**（启动契约——路由/中间件/校验装配）
2. **配置能生效吗？**（配置契约——Production 占位符/环境变量）
3. **真实数据库能连吗？**（映射契约——InMemory 掩盖 SQL 差异）

**如果测试设计时问过这三个问题，10 个坑里 9 个在发布前就能拦住。**

### 2.3 结论

**这不是测试数量的错**（620+ 测试很多），是**测试种类的错**——全是一类（单元），缺三类（系统/配置/集成）。就像体检只量了血压，没做心电图和 CT——血压正常不代表心脏没问题。

---

## 三、测试设计的依据是什么（设计输入分析，2026-08-12 用户追问）

### 3.1 现状：测试设计的依据是「混合的、且最近才补齐需求侧」

| 维度 | 依据 | 证据 |
|------|------|------|
| **组织方式** | 代码模块结构 | 测试按目录（Auth/Catalog/...） |
| **命名** | 模块_场景_结果 | 10-testing-standards.md |
| **断言** | ApiResponse 契约 | ShouldBeSuccessWithData 等 helper |
| **测什么** | （早期）代码功能 →（08-11 起）需求 AC | AC 映射文档 + T1 教训 |

- **既有测试（620+ 绝大部分）——依据「代码结构」**：代码有什么就测什么。验证「代码按我写的逻辑运行」，不是「系统按需求运行」。
- **AC 映射测试（08-11 补，147 US）——依据「需求验收条件」**：每个 US 的 AC 必须有测试守护（T1 教训：4 个 P0 逃过测试 → 建立「需求 AC → 测试」映射）。

### 3.2 关键盲区：AC 映射守护了「功能需求」，但没覆盖「非功能契约」

> AC 映射守护「功能需求」（业务验收条件：登录成功/失败/锁定...）
> **但没覆盖「系统契约」**（系统能启动 / 配置能生效 / 真实 DB 能连 / 中间件链正常）

**举例**：
- US-AUTH-001（登录）的 AC = 「成功/失败/禁用/锁定」→ 有测试 ✅
- 但「WebAPI 能启动」「IdentityDbContext 映射对」「JWT 配置通过校验」→ **没有任何 US 定义这些** → 没有任何测试 ❌

### 3.3 完整设计依据（三层）

```
测试设计依据 = 需求 AC（功能层面）✅ 已建立
            + 代码结构（组织层面）✅ 已有
            + 系统契约（启动/配置/集成层面）❌ 缺失 ← 发布 10 坑根源
```

**本方案 L2-L4 就是补第三类设计输入**——把「系统契约」也变成测试设计依据（不是拍脑袋补测试，而是补一类设计输入）。

---

## 四、目标（新测试标准）

**测试价值 = 守护「系统能交付」**，而不只是「组件能工作」。四层守护：

```
L1 单元层（现有，保留）→ 业务逻辑正确
L2 集成层（新建）     → 真实 SQL + 真实 DbContext 配置
L3 系统层（新建）     → WebApplicationFactory 真实启动 + 真实请求
L4 部署层（新建）     → Production 配置 + 占位符 + 环境变量模拟
```

---

## 五、WebAPI 后端测试方案设计

### L2 集成层（真实 SQL 映射守护）

**目标**：杀掉 InMemory 盲区——验证 EF 配置（列映射/关系/索引）与真实 SQL Server 一致

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| DbContext 映射一致性 | 每个 DbContext（App/Identity/Catalog/MedicalCase）对真实 SQL 建表 → 无 Invalid column | #2 |
| 全实体映射冒烟 | `EnsureCreatedAsync` 真实库 → 查询每个实体表 | #2 |

**技术**：真实 SQL Server（本机 LocalDB 或 192.168.190.243 测试库）；`CreateScope` + `EnsureCreated` 后断言表存在

### L3 系统层（真实启动守护）——**核心**

**目标**：WebApplicationFactory 真实启动 Remote WebAPI，验证「能启动 + 能响应」

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| **启动冒烟** | Factory 启动成功（无启动崩溃） | #1 |
| **全量路由注册** | GET /swagger/v1/swagger.json 200（隐含 MapControllers 成功） | #1 |
| **健康检查** | GET /health → Healthy；/health/database → 可达 | #3 |
| **登录链路** | POST /api/v1/auth/login → token 非空 | #2 |
| **中间件链** | X-Correlation-ID 响应头存在 | — |
| **FallbackPolicy** | 未认证访问 /api/v1/patients → 401 | #9 |
| **安全头** | 业务路径响应含 CSP（严格）；/swagger 路径宽松 | #10 |
| **下载页** | GET / → 200 含下载链接 | — |

**技术**：`WebApplicationFactory<Program>` + `WithWebHostBuilder`（注入测试配置：真实 SQL 连接串 + JWT 测试密钥 + Swagger:Enabled=true）；`Respawn` 每用例重置数据库

### L4 部署层（配置契约守护）

**目标**：Production 配置 + 占位符 + 环境变量模拟——让配置校验在测试中跑一遍

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| **占位符校验** | 模拟 Production 配置（含 ${} 占位符）→ 校验器拦截未展开 | #4 #5 #7 |
| **环境变量注入** | 设置 `Jwt__SecretKey`（Base64）等 → 校验通过 | #4 #7 |
| **JWT Base64 校验** | 非法 Base64 → 启动拦截 | #4 |
| **密码策略校验** | 缺 NEWUSER_PASSWORD → 启动拦截 | #5 |

**技术**：`WithWebHostBuilder(b => b.UseEnvironment("Production"))` + `SetBasePath` 指向真实 appsettings + 注入环境变量

---

## 六、优先级与工作量（按 ROI）

| 批次 | 内容 | 拦截坑 | 工作量 |
|------|------|--------|--------|
| **P0** | L3 启动冒烟 + 健康 + 登录（WebApplicationFactory 基建） | #1 #2 #3 #9 | 0.5-1 天 |
| **P1** | L2 DbContext 映射（真实 SQL 建表冒烟） | #2 | 0.5 天 |
| **P2** | L4 配置契约（Production 模拟 + 环境变量） | #4 #5 #7 | 0.5-1 天 |
| **P3** | L3 扩展（中间件/安全头/下载页/CSP） | #6 #10 | 0.5 天 |

**总计约 2-3 天**——让 10 坑中 9 个进入自动检查。

---

## 七、社区/官方最佳实践对照（2026-08-12 查询校准）

> 依据：EF Core 官方文档（choosing-a-testing-strategy）+ ASP.NET Core 官方集成测试文档 + Nick Chapsas/DevLeader/NimblePros 高赞实践

### 7.1 数据库选择（EF Core 官方立场）

**官方明确反对 InMemory**：
> "Avoid the in-memory provider for testing purposes - this is discouraged and only supported for legacy applications."

**推荐分级**（官方 choosing-a-testing-strategy）：

| 层 | 数据库 | 用途 | 依据 |
|----|--------|------|------|
| 逻辑测试（service 层） | InMemory（仅限简单场景）或 mock | 快 | 官方：simple, constrained query |
| **数据访问/仓储测试** | **SQLite in-memory** | 真实 SQL 引擎 + 约束校验 | 官方：better compatibility, FK constraints |
| **迁移/SQL Server 特性** | **真实 SQL Server** | 列映射/索引/性能 | 官方：no substitute |

**修正原方案**：L2 集成层**优先 SQLite in-memory**（快 + 约束真实），真实 SQL Server 只留关键映射/迁移验证——不是全部走真实 DB。

### 7.2 WebApplicationFactory 标准模式（官方 + 社区一致）

```
CustomWebApplicationFactory : WebApplicationFactory<IApiMarker>
├── IApiMarker（空接口标记 Program，避免 public partial Program 可见性问题）
├── ConfigureWebHost:
│   ├── services.Remove(生产 DbContextOptions 描述符)
│   └── services.Add(测试 DbContext 注册——SQLite/真实 SQL)
└── IAsyncLifetime:
    ├── InitializeAsync（迁移/种子：lookup 数据在此）
    └── DisposeAsync
```

**数据种子规则**（社区共识）：
- lookup/参考数据（永不变化）→ factory 里种
- 测试专属数据 → `IAsyncLifetime.InitializeAsync` 种（贴近测试）

### 7.3 Respawn 数据库清理（Nick Chapsas 力荐）

- 观察 FK 关系 → 确定性 DELETE 顺序（比 TRUNCATE 快、免禁 FK）
- **每个测试前清理**（不是测试后）——测试失败时数据库状态可查
- 本方案已引用 Respawn 包（从未用）——正好用上

### 7.4 测试金字塔分层（社区共识）

```
单元（快，多）→ 逻辑正确
集成（中）   → 数据访问 + SQLite 约束
系统（慢，少）→ WebApplicationFactory 启动 + 真实流程
E2E（最少）  → 完整业务链路
```

**修正原方案**：L2-L4 分层与金字塔一致；真实 SQL 只在「迁移/映射验证」用（数量最少），不是每个测试都连真实库。

---

## 八、技术要点

1. **WebApplicationFactory<Program>**：Program 已 public（兼容性已保证），无需改动生产代码
2. **测试数据库**：本机 SQL Server（真实实例）——用 `Respawn` 每用例重置，不污染数据；连接串注入测试配置
3. **配置注入**：`WithWebHostBuilder` + `UseSetting` / 环境变量——不写死测试配置到代码
4. **隔离**：每个测试类独立数据库名（如 LYBT_Test_{ClassName}），并行安全
5. **不需要新包**：Mvc.Testing / Respawn / SqlServer 均已引用（SQLite in-memory 需新引 `Microsoft.EntityFrameworkCore.Sqlite`）

---

## 九、验收标准

- P0 完成后：**发布前跑 L3 冒烟 = 能拦截 #1 #2 #3 #9**（上线 10 坑中 4 个进自动检查）
- 全部完成后：10 坑中 **9 个进自动检查**（#6 环境差异部分覆盖、#8 设计决策除外）
- 测试运行时间：P0 套件 < 60s（可并入常规测试）

---

## 十、决策记录（2026-08-12 用户确认）

| # | 决策项 | 结论 | 说明 |
|---|--------|------|------|
| 1 | 测试数据库 | **192.168.190.243 测试库 `LYBTDB_Test`** | 连通性已实测通过（0.42s）；本机↔243 互通 |
| 2 | 测试库创建方式 | **B：全新空库 + EF 迁移自建** | schema 完全由迁移生成，与生产一致（正是要验证的） |
| 3 | P0 先行 or 全做 | **全做**（P0+P1+P2+P3） | 一次落地完整 L2-L4 |
| 4 | 发布门禁 | **纳入** Runbook 第 0 步 | P0 后发布前必跑冒烟 |
| 5 | 测试库命名 | **LYBTDB_Test** | 语义清晰，与 LYBTDB_Dev 并列 |

> 连通性实测：`sqlcmd -S 192.168.190.243 -U sa -C -Q "SELECT @@VERSION"` → SQL Server 2016 SP1 Enterprise，0.42s 响应，sa 认证成功。LYBTDB_Dev 现状：22 表 / 8340 行 / 8MB。
