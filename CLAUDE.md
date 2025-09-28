# CLAUDE.md

本文件约束 Claude Code（claude.ai/code）在本仓库的工作方式与标准。

## 项目简介
- **项目名称**：凌隐宝堂中医诊所管理系统（LYBTZYZS）
- **总体定位**：面向中医诊所的企业级 .NET 8 解决方案，前端采用 WPF + Prism.DryIoc，后端采用 ASP.NET Core Web API + EF Core，核心契约与工具位于 `src/Shared`。

## 当前状态（2025-09-24）
| 项目维度 | 当前结论 |
| --- | --- |
| 编译情况 | ❌ Desktop 端存在事件重复定义，暂无法通过编译 |
| 事件体系 | ⚠️ 多套事件/枚举并存，需统一至 `UnifiedEvents.cs` |
| 测试现状 | ⚠️ 服务器侧 `dotnet test` 失败；桌面端尚未建立自动化测试基线 |
| 术语一致性 | ⚠️ README、UI 与文档需统一使用“诊疗工作台”等最新术语 |

## 当前最高优先级任务
1. **事件体系统一**：清理 `Core/Events` 目录下所有重复事件与枚举，仅保留权威定义，并统一使用 `StatusMessageType`。
2. **修复资源引用**：检查 `UnifiedDesignSystem.xaml` 中转换器命名空间，确保 `StringToVisibilityConverter` 所在程序集已被 Shell 正确加载。
3. **术语与结构调整**：将“看诊”相关命名改为“诊疗”，梳理 `MedicalWorkbenchMainView` 的职责，更新 UI 文案及 README。
4. **测试恢复计划**：在完成编译修复后，先解决服务器端失败用例，再为桌面端关键服务（如 `SessionManager`、`UnifiedEventHandler`）补齐首批单元测试。

> 未完成以上事项前，不得开启新功能开发。

#### 任务启动前置检查 (Pre-flight Check)

在处理任何 GitHub Issue 前，先完成以下检查，并据此制定方案：

1.  **同步最新代码**：运行 `git pull` 确保本地分支为最新。
2.  **验证编译状态**：运行 `dotnet build LYBT.All.sln` 确认项目当前是否可编译。如果编译失败，你的首要任务是分析并修复编译错误，而不是继续原定任务。
3.  **检查测试基线**：运行 `dotnet test LYBT.Server.sln` 确认核心测试是否通过。若存在失败，需在方案中评估其对当前任务的影响。

## 核心工作流：GitHub 驱动，Claude/Serena 协同

本项目以 GitHub 为中心管理。Claude Code 充当“智能顾问”，负责方案与初审；如需二审可引入 Serena。

#### 任务分解与跟踪（模块化功能清单 + 实时追踪）

1.  使用“模块化功能清单”为唯一清单：Issue 创建后，AI 按模块生成清单并固定在 Issue 顶部“AI 追踪区块”。模块示例：Server(WebAPI)、Client(Desktop)、Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula、Shared、Infra/CI、Docs。
2.  条目规范（最小闭环）：动词开头，含“产出物路径/接口 + 验收点”。编号用模块前缀（如 [SRV-1]/[CLI-1]/[DOC-1]），依赖标注 Dep: [编号]。
3.  关联 PR：PR 标题或描述必须引用编号（如 “feat(server): set default port 5001 [SRV-1]”），AI/CI 才能自动勾选并回填证据（命令/截图/日志）。
4.  AI 执行：
    a) 阅读清单；b) 仅处理未勾选项；c) 范围变化生成 v2 清单并附变更摘要；d) 合并后自动勾选并回填证据链接。
5.  清单收敛：模块内条目控制在 3–8 条，过多则分期（Phase n）。

6.  编译守门（必备验收）：
    - Issue 关闭前，必须保证当前解决方案可编译。
    - 最低要求：`dotnet build LYBT.All.sln`；或分别构建 `LYBT.Server.sln`、`LYBT.Desktop.sln`（若 Desktop 在修复阶段，至少保证受影响解决方案无编译错误）。
    - PR 描述需附“编译命令与结果摘要”。AI/CI 在合并前校验，不通过不得勾选完成。

7.  Issue 同步与生命周期（GitHub 单一事实源）：
    - 自动创建与同步：本地/文档侧新建的 Issue（含主/子）须由机器人同步至 GitHub，保持编号、标题、清单一致。
    - 主从关系：在 GitHub 上维护父子关联；清单中的跨 Issue 子任务用任务语法链接，保证联动。
    - 实时更新：清单变更、PR 合并、校验通过时，AI 同步勾选与证据评论；范围变更生成 v2 清单并回写。
    - 完成即关闭：当条目全勾且编译通过，自动关闭 Issue；子 Issue 完成时联动主 Issue 勾选。

- **GitHub 作为“操作系统”**：
  - **信息记录**: 所有需求、任务、缺陷均须创建为 **GitHub Issues**。
  - **进度跟踪**: 使用 **GitHub Projects**（看板）对 Issues 的状态进行可视化跟踪。
  - **关系管理**: 通过 PR 与 Issue 的自动链接，建立代码变更与任务需求的明确关系。

- **Claude Code / Serena 作为“智能顾问”**：
  - **辅助规划**：针对复杂的 GitHub Issue，优先由 Claude Code 整理最小变更集与实施计划；必要时调用 Serena 的 `plan` 方法生成详细方案与子任务建议。所有实施计划或方案建议的开头，必须包含一个“**遵循标准**”章节，明确列出本次任务将要遵循的关键技术标准（引自 `docs/development/standards.md`）。如果无相关标准，则注明“无特定标准适用”。
  - **代码审查**：提交 Pull Request 后，先由 Claude Code 进行自动化初审（规范、风险、与 Issue 的一致性）；如需进一步论证或跨文档一致性校验，再调用 Serena 的 `proofread` 方法进行二审。

#### 原则：Issue 的原子性与生命周期

这是指导所有开发活动的最核心原则：
- **一个 Issue = 一次独立的、原子的“工作尝试”**。
- Issue 的生命周期始于创建，终于其对应的 **Pull Request (PR) 到达终态（被合并或被关闭）**。
- 无论 PR 是被接受还是被拒绝，该 Issue 所代表的“工作尝试”均宣告结束。此举旨在确保历史记录的清晰，并为 AI 提供无歧义的、全新的任务指令。

**开发黄金路径（PR 被接受）**：
1.  一切工作始于一个明确的 **GitHub Issue**。
2.  （可选）针对复杂 Issue，调用 **Serena** 进行规划。
3.  创建与 Issue 关联的 **Git 分支**进行开发。
4.  通过 **Pull Request** 提交变更，并关联对应 Issue（建议在 PR 描述中使用 `Fixes #<issue>` 关键字实现自动关闭）。
5.  进行 AI 代码审查：先由 **Claude Code** 初审；必要时再调用 **Serena** 进行二审。
6.  经人工审核通过后 **合并 PR**，关联的 Issue 会被自动关闭。若未使用关键字，Actions 会根据 PR 描述中的引用自动关闭相关 Issue（确保任务与 GitHub 状态同步）。

**PR 审核不通过的处理路径（PR 被拒绝）**：
1.  **明确拒绝原因**：审核者在 PR 的评论中清晰说明拒绝理由（例如：方案设计不妥）。
2.  **关闭 Pull Request**：审核者直接关闭该 PR，不进行合并。
3.  **创建新 Issue**：审核者创建一个全新的 Issue，用于发起新的“工作尝试”。
    - 在新 Issue 中阐述新的实现方案或验收标准。
    - 在描述中链接到旧的 Issue 和被关闭的 PR，以保留上下文（例如：“此任务用新方案替代在 #42 中被拒绝的尝试”）。

#### AI 代码审查清单 (AI Code Review Checklist)

在进行代码审查时，必须依据以下清单逐项评估，并以列表形式输出审查结论。**审查的核心目标是判断 PR 是否满足了关联 Issue 中定义的“验收标准”。**

-1. **[ ] 读取技术标准**：在开始任何审查前，必须首先读取 `docs/development/standards.md` 的全部内容，并将其作为本次审查的最高准则。
0.  **[ ] 读取验收标准**：在开始审查前，必须首先通过 MCP 读取关联 Issue 的描述，并抽取出其中所有的“验收标准”。这是后续所有检查的基准。
1.  **[ ] 验收标准符合性**：逐一比对代码实现，判断是否**完全满足**了第 0 步中获取的所有验收标准。
    *   如果**全部满足**，则审查结论为“**通过 (Pass)**”，并明确指出“代码已满足所有验收标准，无需进一步优化”。**即使存在理论上可优化的空间，也应在此终止，避免过度设计。**
    *   如果**部分或完全不满足**，则审查结论为“**不通过 (Fail)**”，并仅针对**未满足**的标准提出具体的、最小化的修改建议。
2.  **[ ] 架构约束**：是否引入了如 `MediatR` 等“明确禁止”的技术？是否遵循了读写分离的模式？
3.  **[ ] 规范遵循**：命名、异步规范、文件体量等是否符合“开发规范要点”？
4.  **[ ] 依赖注入**：是否出现了 `Container.Resolve` 或 `ServiceLocator` 等反模式？
5.  **[ ] 文档同步**：相关的业务或技术文档（`docs/`目录下）是否已同步更新？（原则一）
6.  **[ ] 增量优化**：本次变更是否为“增量式优化”，而非无明确指令的“颠覆性重构”？（原则三）
7.  **[ ] 测试覆盖**：核心逻辑是否被单元测试或集成测试覆盖？

### 人工参与与自动化职责边界

- 人工参与（三步）：
  - 创建并完善 Issue（含验收标准）。
  - 启动 Issue（将 Issue 标记为 In-Progress）。
  - 审核 PR 并决定是否合并（审核通过即任务结束）。

- 自动化执行：
  - Issue 初始分流与提示（自动添加状态标签、分派模板校验、生成分支命名建议）。
  - PR 与 Issue 关联校验与提醒（未关联则评论提醒）。
  - PR 合并后自动关闭关联 Issue，并同步状态标签为 Done。
  - 可选：若配置了项目看板变量，自动维护 Projects 状态字段。

## 技术栈与架构
### 前端（WPF + Prism.DryIoc）
- 采用 **模块化双层架构**：Module（委托层）+ QueryService（查询）+ BusinessService（业务）。
- 通过角色驱动的工作台（系统工作台 / 诊疗工作台）实现按需加载与导航。
- ViewModel 必须通过接口注入服务，禁止直接解析容器或依赖具体模块实现。

### 后端（ASP.NET Core Web API）
- 延续 **控制器 → 服务 → 仓储** 的三层模式。
- 所有数据访问均使用 `LYBT.Infrastructure` 中的统一 `AppDbContext`。

### 共享层
- DTO、接口、工具位于 `src/Shared`，禁止在前后端重复定义数据结构或服务接口。

### 架构约束与技术选型

#### 适度设计原则（最高优先级）
- **核心理念**: 本项目是小型中医诊所系统，必须避免过度工程
- **判断标准**: 
  - 系统规模：并发用户<10人，数据量<10万条
  - 使用场景：诊所内部使用，无外部API
  - 团队规模：1-3人开发维护
  
#### 明确禁止的技术（过度工程黑名单）
- **禁止引入**: 
  - Redis/分布式缓存（内存缓存足够）
  - 消息队列/Kafka/RabbitMQ（同步处理足够）
  - 微服务架构（单体应用足够）
  - CQRS/事件溯源（三层架构足够）
  - Docker/K8s（传统部署足够）
  - GraphQL（RESTful足够）
  - API版本管理（内部系统不需要）
- **禁止推荐**: 在任何代码审查或建议中，严禁推荐上述技术

#### 正确的技术选择
- **优先使用**:
  - 框架内置功能
  - 简单直接的解决方案
  - 成熟稳定的技术
- **遵循现有模式**:
  - **读取操作**: 严格遵循已建立的 `Controller → QueryService → ReadRepository` 只读路径。
  - **写入操作**: 严格遵循标准的 `Controller → BusinessService → DbContext/WriteRepository` 模式，将业务逻辑封装在 `BusinessService` 中。

## 常用命令（PowerShell）
```powershell
# 还原 / 构建
dotnet restore LYBT.All.sln
dotnet build LYBT.Server.sln -c Release --no-restore
dotnet build LYBT.Desktop.sln -c Release --no-restore

# 运行 WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI

# 代码格式化
dotnet format LYBT.All.sln

# 测试（修复失败后执行）
dotnet test LYBT.Server.sln -c Release
```

## 开发规范要点
- **语言统一**：所有代码注释、终端输出、提交信息均使用中文。
- **文件编码**：为从根源上解决中文乱码问题，本项目所有文本类文件（如 `.cs`, `.xaml`, `.md`）**必须**使用 `UTF-8 with BOM` 编码格式。此规范已通过根目录下的 `.editorconfig` 文件进行强制约束。
- **内容纯洁性**：所有代码文件（如 `.cs`, `.xaml`）、脚本文件（如 `.ps1`, `.sh`）及提交信息的正文中，**严禁**使用任何 Emoji 表情符号，以确保跨平台兼容性与代码库的专业性。
- **依赖注入**：采用构造函数注入接口；禁止在 ViewModel 中使用 `Container.Resolve` 或 `ServiceLocator`。
- **异步规范**：涉及 I/O 的操作必须使用 async/await，避免同步阻塞。
- **文件体量**：建议单文件不超过 500 行，逻辑复杂时应拆分模块。
- **命名约定**：类型与公有成员 PascalCase，私有字段 `_camelCase`，异步方法以 `Async` 结尾。
## AI 开发核心：MCP 工具链与调用规范

AI 开发必须遵循**模型上下文协议（MCP）**。所有外部交互（文件/Git/SQL/分析）均通过 `mcp.run()` 执行。

核心理念：**工具驱动**与**显式调用**。
- 禁止自由发挥：不得假设可直接访问文件系统或执行命令。
- 一切皆工具：任何环境交互均使用对应 MCP 服务。
- 代码即日志：`mcp.run()` 调用序列即可复现的工作日志。

下方是本项目配置的 MCP 工具链，请在开发时严格参照。

### 核心原则：文档驱动与约束遵守

此外，AI 必须遵守以下核心原则：

1.  **文档即代码，实时同步**
    *   **严格维护**：任何对代码逻辑、功能、架构的修改，都必须伴随着对相关文档的同步更新。代码与文档被视为同等重要的交付物。
    *   **变更即文档**：在提交代码变更（如生成 `git applyPatch` 的内容或撰写 PR 描述）时，必须一并提供需要修改的文档内容。例如，若重构了某个服务，则必须同时更新 `docs/architecture` 中相关的说明或图表。

2.  **方案先于文档**
    *   **审查代码**：在审查或分析任何代码时，必须先通过 MCP 工具读取 `docs` 目录下的相关架构约束、设计原则和需求文档，并以这些既定约束作为评估代码质量的最高标准。
    *   **提出方案**：在设计新的技术方案或重构建议时，必须在方案的开头明确指出其所依据的文档条款。例如：“*依据 `docs/architecture/principles.md` 中定义的整洁架构原则，我建议...*”。

目的：以文档为基石，确保行为与既定规则一致，避免偏离。

### 原则 2.5：完成导向，“够用即好”

在“分析 Issue 并制定方案”阶段，遵循：

1.  完成优先：以“尽快、正确地完成当前任务”为第一目标，避免为了潜在的未来扩展做超前设计。
2.  够用即好：默认选择简单、稳定、易于验证的方案与实现，满足当前验收标准即可；除非文档或需求明确要求可扩展性/可复用性。
3.  控制范围：仅在任务范围内修改必要文件与依赖，坚决避免无关重构与额外抽象。
4.  推迟优化：将可选优化点记录为后续改进建议（在 Issue 评论或后续子 Issue 中），不阻塞本次交付。
5.  推荐窗口：仅在“代码分析阶段”提出重构/替代技术，并标注“建议/可选”。

6.  方案参考与综合：AI 在分析 Issue 并制定实现方案时，必须优先参考 Issue 文档中已有的“解决方案/设计思路/约束说明”，结合仓库现行规范与代码事实进行综合分析；若参考结论与现有代码或规范冲突，需在方案中给出取舍理由与影响评估，以提高方案的可靠性与可追溯性。

### 原则三：增量式优化，而非颠覆性重构

为避免 AI 在代码审查和重构建议中提出与现有代码库完全脱节的“颠覆性”方案，所有相关操作必须遵循“增量式优化”原则。

1.  **理解“审查”的内涵**：当接到“审查”或“优化”这类指令时，AI 的首要任务不是“推倒重来”，而是**在现有代码结构和逻辑的基础上进行“微调”**。应优先识别具体问题（如：潜在的空引用、不符合规范的命名、可读性差的循环等），并提出最小化的、针对性的修改建议。
2.  **禁止默认重写**：除非收到“请用XX设计模式重构此代码”这样**极其明确**的指令，否则**严禁**对整个函数或类进行完全的、基于不同设计思想的重写。所有建议都应默认是对现有代码的**优化和增强**。
3.  **以“差异”形式交付**：所有代码修改建议，都应尽可能以 **Diff 格式** 或“修改前/修改后”的对比形式提供。这有助于开发者清晰地理解变更点，并决定是否采纳。
4.  **建议必须有据可依**：每一条优化或修改建议，都必须链接到它所依据的“项目宪法”（即 `docs` 目录下的相关文档）。例如：“*我建议进行此项修改，因为它能更好地遵循 `docs/architecture/principles.md` 中定义的单一职责原则。*”

### 原则四：智能规划与并行代理 (Intelligent Planning & Parallel Agents)

为了最大化开发效率，你（Claude Code）的角色不仅是执行者，更是任务的**总指挥 (Master Agent)**。在处理复杂任务时，你必须主动运用并行处理和任务委托的思维。

1.  **识别可并行的任务**：在分析一个 Issue 的任务列表时，你必须主动识别出哪些子任务是**相互独立、可以并行处理的**。例如：
    *   【代码与文档】：编写业务逻辑代码 vs. 撰写对应的 Markdown 文档。
    *   【分析与检索】：在 `Filesystem` 中搜索特定关键字 vs. 在 `Context7` 中进行语义查询。
    *   【测试与检查】：在一个子代理中运行 `dotnet test` vs. 在另一个子代理中运行代码格式化或规范检查。

2.  **启动并行子代理**：对于识别出的可并行任务，你应该通过并发调用 `mcp.run()` 来启动**多个子代理 (Sub-Agents)**，让它们同时开始工作，而不是按顺序等待。
    *   **执行语法**：你可以采用类似 `Promise.all([mcp.run(...), mcp.run(...)])` 的模式，同时发起多个 MCP 调用，并等待它们全部完成。

3.  **委托与综合**：你的核心工作流应升级为：
    a. **分解 (Decomposition)**：理解 Issue，并确认其任务列表的合理性。
    b. **委托 (Delegation)**：识别并行的子任务，启动相应的子代理去执行。
    c. **监督与综合 (Supervision & Synthesis)**：等待所有子代理执行完毕，收集它们各自的产出（如代码、测试结果、文档片段），然后由你（主代理）负责将这些碎片化的产出整合成一个完整的、高质量的 Pull Request。

### MCP 工具快速参考

| 工具 | 调用方式（在 Claude Code 中） | 典型场景 | 关键参数说明 |
| --- | --- | --- | --- |
| Serena Server | mcp.run({ server: "serena", method: "execute", params: {...} }) | 复杂分析、架构建议、代码审计 | prompt：输入需求或文件片段，	emperature：生成多样性（默认0.2） |
| Filesystem Server | mcp.run({ server: "filesystem", method: "readFile", params: { path } }) 等 | 批量读取/写入文件、遍历目录、搜索文本 | 方法：
eadFile、writeFile、listDirectory、searchText；path 为绝对或相对路径 |
| Context7 API | mcp.run({ server: "context7", method: "query", params: {...} }) | **本地代码库问答**、专业知识查询、术语解释 | **核心方法**：`add` (索引路径), `query` (提问)<br/>**辅助方法**：`analyze`, `summarize` |
| Memory Server | mcp.run({ server: "memory", method: "save", params: {...} }) | 在当前会话中保存/检索临时笔记、TODO | save：追加内容；load：获取全部记录；clear：清空 |
| Git Server | mcp.run({ server: "git", method: "status" }) | 查看 Git 状态、变更、提交历史 | 方法：status, diff, log, stash, checkout 等 |
| Playwright Server | mcp.run({ server: "playwright", method: "execute", params: {...} }) | 桌面/前端测试脚本录制、运行 UI 自动化 | script：Playwright 代码；rowser：浏览器类型（chromium/firefox/webkit） |

> 引擎会自动处理各 MCP 的启动/认证。调用示例：
`	s
await mcp.run({
  server: "filesystem",
  method: "readFile",
  params: { path: "src/Server/Services/LYBT.WebAPI/Program.cs" }
});
`

常用技巧：
1. **代码库问答**：当需要理解某项功能的实现时，先用 `Context7` 的 `query` 方法提问，获取相关代码片段，再结合 `Serena` 的 `execute` 方法进行深入分析或重构。
2. **批量操作**：使用 `Filesystem` 的 `listDirectory` + `readFile` 快速收集多个文件内容。
3. **变量保存**：`Memory Server` 记录当前任务进度或后续待办，避免上下文丢失。
4. **SQL 验证**：通过 `SQL Server` MCP 快速验证 EF 查询结果，与测试数据对齐。
5. **Playwright 调试**：在需要录制桌面端交互脚本时，调用 `Playwright` MCP 执行自动化脚本。

### Context7 使用强制规则

Always use context7 when I need code generation, setup or configuration steps, or library/API documentation. This means you should automatically use the Context7 MCP tools to resolve library id and get library docs without me having to explicitly ask.

落实要求：
- 当涉及“代码生成、环境搭建/配置步骤、库或 API 文档查询”时，必须优先调用 Context7 的 MCP 工具（例如 resolve-library-id、get-library-docs）。
- 默认自动解析库 ID 并抓取文档，不等待人工提示；仅在歧义时发起澄清。
6. **工具容错与重试**：当 `mcp.run()` 调用失败时，应遵循以下步骤：
    a. **分析错误**：首先读取返回的 `error` 信息。
    b. **自我修正**：如果是参数错误（如路径错误），应根据错误信息修正 `params` 并重试一次。
    c. **报告阻塞**：如果重试后依然失败，或错误非自身能解决（如服务器不可用），应立即停止当前任务，并清晰地报告阻塞点和错误信息。

### 常用调用示例

```ts
// Serena：获取重构建议
await mcp.run({
  server: "serena",
  method: "execute",
  params: { prompt: "审查 PatientBusinessService 的分层问题" }
});

// Filesystem：批量读取目录
const files = await mcp.run({
  server: "filesystem",
  method: "listDirectory",
  params: { path: "src/Server/Modules" }
});

// Context7: 索引本地代码（初次或代码变更后执行）
await mcp.run({
  server: "context7",
  method: "add",
  params: { path: "./src" }
});

// Context7: 查询代码库实现细节
const codeContext = await mcp.run({
  server: "context7",
  method: "query",
  params: { question: "用户认证流程是如何实现的？" }
});
// 接下来，可以将 codeContext 作为上下文交给 Serena 分析
await mcp.run({
  server: "serena",
  method: "execute",
  params: { 
    prompt: `基于以下代码上下文，解释用户认证的实现步骤：

${codeContext}`
  }
});

// Memory：记录 TODO
await mcp.run({
  server: "memory",
  method: "save",
  params: { content: "待统一所有 BusinessService 依赖" }
});

// Git：查看最新差异
await mcp.run({
  server: "git",
  method: "diff"
});

// Playwright：运行自动化脚本
await mcp.run({
  server: "playwright",
  method: "execute",
  params: { script: "const { chromium } = require('playwright'); /* ... */" }
});
```
### 工具深入用法提示
- **Serena**（https://github.com/oraios/serena）
  - 方法：xecute（通用推理）、plan（生成多步骤方案）、proofread（检查代码或文档）
  - 参数：prompt 输入要分析的问题；可设置 	emperature、maxTokens 控制输出；支持 ttachments 传入文件片段。
  - 建议：用于复杂架构审查、生成重构计划、对 PR 做技术点评。
- **Filesystem**（https://github.com/modelcontextprotocol/servers/tree/main/src/filesystem）
  - 方法：
eadFile、writeFile、listDirectory、stat、searchText、createDirectory、delete
  - 支持一次读取多文件，配合 searchText 可在代码库中定位 TODO/术语。
  - 写入操作需仔细核对路径，默认相对仓库根目录。
- **Context7**（https://github.com/upstash/context7）
  - **核心定位**：作为本项目的**本地代码知识库**，用于快速检索和理解现有代码实现。
  - **核心工作流**：
    1.  **`add` (索引)**：使用 `mcp.run({ server: "context7", method: "add", params: { path: "./src" } })` 将指定目录（如 `src` 或 `docs`）索引到向量数据库。此操作在首次使用或代码库有重大更新后执行。
    2.  **`query` (查询)**：使用 `mcp.run({ server: "context7", method: "query", params: { question: "..." } })` 提出自然语言问题。它会返回与问题最相关的代码片段作为上下文。
  - **使用场景**：在进行任何代码分析、重构或新功能开发前，应**首先使用 `query` 方法**来理解相关模块的现有实现。将返回的代码片段作为上下文，再调用 `Serena` 或其他工具进行下一步操作，确保所有 AI 工作都基于真实的项目代码。
  - **辅助功能**：`analyze`, `summarize` 等方法可用于处理外部文本或专业术语，作为对本地代码库知识的补充。
- **Memory**（https://github.com/modelcontextprotocol/servers/tree/main/src/memory）
  - 提供 save、load、delete、clear 方法，用于记录当前会话中的临时结论、下一步计划。
  - 建议整理任务列表或会议纪要，随时用 load 取出复盘。
- **Git**（内置 git-mcp-server）
  - 常用方法：status、diff、log、pplyPatch、commit、checkout。
  - 可在不离开 Claude 的情况下查看差异、生成 patch；提交前仍需在本地终端验证。
- **Playwright**（https://github.com/microsoft/playwright-mcp）
  - 方法：xecute；params.script 为 JS/TS 测试脚本；可设置 rowser、headless。
  - 用于录制/回放桌面或 Web 交互，支持截图、PDF、视频输出。
> 所有工具均可通过 wait mcp.run({ server, method, params }) 调用，可组合使用，例如：
> 1) Filesystem searchText 找到术语；2) Serena xecute 生成重构方案；3) Git pplyPatch 应用修改。

- 充分利用Serena Server MCP的功能。
- 整个开发过程用GitHub跟踪。
