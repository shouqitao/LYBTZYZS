# CLAUDE.md

本文件用于指导 Claude Code（claude.ai/code）在本仓库内开展开发工作，请务必遵循以下约定。

## 项目简介
- **项目名称**：凌隐宝堂中医诊所管理系统（LYBTZYZS）
- **总体定位**：面向中医诊所的企业级 .NET 8 解决方案，前端采用 WPF + Prism.DryIoc，后端采用 ASP.NET Core Web API + EF Core，核心契约与工具位于 `src/Shared`。

## 当前状态速览（2025-09-24）
| 项目维度 | 当前结论 |
| --- | --- |
| 编译情况 | ❌ Desktop 端存在事件重复定义，暂无法通过编译 |
| 事件体系 | ⚠️ 多套事件/枚举并存，必须统一到 `UnifiedEvents.cs` |
| 测试现状 | ⚠️ 服务器侧 `dotnet test` 失败；桌面端尚未建立自动化测试基线 |
| 术语一致性 | ⚠️ README、UI 与文档需统一使用“诊疗工作台”等最新术语 |

## 当前最高优先级任务
1. **事件体系统一**：清理 `Core/Events` 目录下所有重复事件与枚举，仅保留权威定义，并统一使用 `StatusMessageType`。
2. **修复资源引用**：检查 `UnifiedDesignSystem.xaml` 中转换器命名空间，确保 `StringToVisibilityConverter` 所在程序集已被 Shell 正确加载。
3. **术语与结构调整**：将“看诊”相关命名改为“诊疗”，梳理 `MedicalWorkbenchMainView` 的职责，更新 UI 文案及 README。
4. **测试恢复计划**：在完成编译修复后，先解决服务器端失败用例，再为桌面端关键服务（如 `SessionManager`、`UnifiedEventHandler`）补齐首批单元测试。

> 未完成以上事项前，请勿开始新的功能开发。

## 核心工作流：GitHub 驱动，Claude/Serena 协同

为确保所有开发活动清晰、可控、可追溯，本项目严格遵循以 GitHub 为中心的管理模式。AI (Claude Code) 在此流程中扮演“智能顾问”而非“项目经理”的角色；在代码审查环节，Claude Code 参与自动化初审，Serena 可作为二审/深度校对辅助。

- **GitHub 作为“操作系统”**：
  - **信息记录**: 所有需求、任务、缺陷均须创建为 **GitHub Issues**。
  - **进度跟踪**: 使用 **GitHub Projects**（看板）对 Issues 的状态进行可视化跟踪。
  - **关系管理**: 通过 PR 与 Issue 的自动链接，建立代码变更与任务需求的明确关系。

- **Claude Code / Serena 作为“智能顾问”**：
  - **辅助规划**：针对复杂的 GitHub Issue，优先由 Claude Code 整理最小变更集与实施计划；必要时调用 Serena 的 `plan` 方法生成详细方案与子任务建议。
  - **代码审查**：提交 Pull Request 后，先由 Claude Code 进行自动化初审（规范、风险、与 Issue 的一致性）；如需进一步论证或跨文档一致性校验，再调用 Serena 的 `proofread` 方法进行二审。

**开发黄金路径**：
1.  一切工作始于一个明确的 **GitHub Issue**。
2.  （可选）针对复杂 Issue，调用 **Serena** 进行规划。
3.  创建与 Issue 关联的 **Git 分支**进行开发。
4.  通过 **Pull Request** 提交变更，并关联对应 Issue（建议在 PR 描述中使用 `Fixes #<issue>` 关键字实现自动关闭）。
5.  进行 AI 代码审查：先由 **Claude Code** 初审；必要时再调用 **Serena** 进行二审。
6.  经人工审核通过后合并 PR；合并后由关键字自动关闭 Issue。若未使用关键字，Actions 会根据 PR 描述中的引用自动关闭相关 Issue（确保任务与 GitHub 状态同步）。

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
- 采用 **UltraThink 双层架构**：Module（委托层）+ QueryService（查询）+ BusinessService（业务）。
- 通过角色驱动的工作台（系统工作台 / 诊疗工作台）实现按需加载与导航。
- ViewModel 必须通过接口注入服务，禁止直接解析容器或依赖具体模块实现。

### 后端（ASP.NET Core Web API）
- 延续 **控制器 → 服务 → 仓储** 的三层模式。
- 所有数据访问均使用 `LYBT.Infrastructure` 中的统一 `AppDbContext`。

### 共享层
- DTO、接口、工具位于 `src/Shared`，禁止在前后端重复定义数据结构或服务接口。

### 架构约束与技术选型
- **明确禁止**: 项目当前阶段**明确决定不引入**完整的CQRS模式和MediatR库。此决策基于对项目规模的判断，旨在避免过度设计。
- **禁止推荐**: 在进行任何代码实现或重构建议时，**请勿推荐**使用上述技术。
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
- **依赖注入**：采用构造函数注入接口；禁止在 ViewModel 中使用 `Container.Resolve` 或 `ServiceLocator`。
- **异步规范**：涉及 I/O 的操作必须使用 async/await，避免同步阻塞。
- **文件体量**：建议单文件不超过 500 行，逻辑复杂时应拆分模块。
- **命名约定**：类型与公有成员 PascalCase，私有字段 `_camelCase`，异步方法以 `Async` 结尾。
## AI 开发核心：MCP 工具链与调用规范

本项目中的 AI 开发工作，其代码实现风格必须严格遵循**模型上下文协议（Model Context Protocol, MCP）**。所有与外部环境的交互（如文件读写、Git 操作、执行 SQL、甚至是发起架构分析）都必须通过调用 `mcp.run()` 函数来完成。

这种风格的核心是**“工具驱动”**和**“显式调用”**：
- **禁止自由发挥**：AI 不应假设自己拥有直接访问文件系统或执行命令的能力。
- **一切皆工具**：任何需要与项目环境交互的操作，都必须找到下方工具链中对应的 MCP 服务和方法来执行。
- **代码即日志**：`mcp.run()` 的调用序列本身就是一份清晰、可读、可复现的工作日志，记录了 AI 的每一步思考和操作。

下方是本项目配置的 MCP 工具链，请在开发时严格参照。

### 核心原则：文档驱动与约束遵守

除了遵循上述 MCP 调用规范外，所有 AI 操作还必须恪守以下核心原则，以确保开发过程的严谨性和一致性。

1.  **文档即代码，实时同步**
    *   **严格维护**：任何对代码逻辑、功能、架构的修改，都必须伴随着对相关文档的同步更新。代码与文档被视为同等重要的交付物。
    *   **变更即文档**：在提交代码变更（如生成 `git applyPatch` 的内容或撰写 PR 描述）时，必须一并提供需要修改的文档内容。例如，若重构了某个服务，则必须同时更新 `docs/architecture` 中相关的说明或图表。

2.  **一切方案，始于文档**
    *   **审查代码**：在审查或分析任何代码时，必须先通过 MCP 工具读取 `docs` 目录下的相关架构约束、设计原则和需求文档，并以这些既定约束作为评估代码质量的最高标准。
    *   **提出方案**：在设计新的技术方案或重构建议时，必须在方案的开头明确指出其所依据的文档条款。例如：“*依据 `docs/architecture/principles.md` 中定义的整洁架构原则，我建议...*”。

此举旨在将文档作为所有开发活动的基石，确保 AI 的所有行为都建立在项目既定的规则之上，从而根除需求与实现的偏离。

### 原则三：增量式优化，而非颠覆性重构

为避免 AI 在代码审查和重构建议中提出与现有代码库完全脱节的“颠覆性”方案，所有相关操作必须遵循“增量式优化”原则。

1.  **理解“审查”的内涵**：当接到“审查”或“优化”这类指令时，AI 的首要任务不是“推倒重来”，而是**在现有代码结构和逻辑的基础上进行“微调”**。应优先识别具体问题（如：潜在的空引用、不符合规范的命名、可读性差的循环等），并提出最小化的、针对性的修改建议。
.  **禁止默认重写**：除非收到“请用XX设计模式重构此代码”这样**极其明确**的指令，否则**严禁**对整个函数或类进行完全的、基于不同设计思想的重写。所有建议都应默认是对现有代码的**优化和增强**。

3.  **以“差异”形式交付**：所有代码修改建议，都应尽可能以 **Diff 格式** 或“修改前/修改后”的对比形式提供。这有助于开发者清晰地理解变更点，并决定是否采纳。

4.  **建议必须有据可依**：每一条优化或修改建议，都必须链接到它所依据的“项目宪法”（即 `docs` 目录下的相关文档）。例如：“*我建议进行此项修改，因为它能更好地遵循 `docs/architecture/principles.md` 中定义的单一职责原则。*”

### MCP 工具快速参考

| 工具 | 调用方式（在 Claude Code 中） | 典型场景 | 关键参数说明 |
| --- | --- | --- | --- |
| Serena Server | mcp.run({ server: "serena", method: "execute", params: {...} }) | 复杂分析、架构建议、代码审计 | prompt：输入需求或文件片段，	emperature：生成多样性（默认0.2） |
| Filesystem Server | mcp.run({ server: "filesystem", method: "readFile", params: { path } }) 等 | 批量读取/写入文件、遍历目录、搜索文本 | 方法：
eadFile、writeFile、listDirectory、searchText；path 为绝对或相对路径 |
| Context7 API | mcp.run({ server: "context7", method: "analyze", params: {...} }) | 法规咨询、专业知识背景、语言风格转换 | 常用方法：nalyze, summarize；需传入 	ext 和 context |
| Memory Server | mcp.run({ server: "memory", method: "save", params: {...} }) | 在当前会话中保存/检索临时笔记、TODO | save：追加内容；load：获取全部记录；clear：清空 |
| Git Server | mcp.run({ server: "git", method: "status" }) | 查看 Git 状态、变更、提交历史 | 方法：status, diff, log, stash, checkout 等 |
| Playwright Server | mcp.run({ server: "playwright", method: "execute", params: {...} }) | 桌面/前端测试脚本录制、运行 UI 自动化 | script：Playwright 代码；rowser：浏览器类型（chromium/firefox/webkit） |
| SQL Server | mcp.run({ server: "sql-server", method: "executeQuery", params: { query } }) | 针对 LYBTDB 做 SQL 调试、数据核对 | xecuteQuery：返回结果集；xecuteNonQuery：执行更新；database 默认 LYBTDB |

> 引擎会自动处理各 MCP 的启动/认证。调用示例：
`	s
await mcp.run({
  server: "filesystem",
  method: "readFile",
  params: { path: "src/Server/Services/LYBT.WebAPI/Program.cs" }
});
`

常用技巧：
1. **批量操作**：使用 Filesystem Server 的 listDirectory + 
eadFile 快速收集多个文件内容。
2. **知识补充**：在分析法规、医疗术语时，借助 Context7 获取权威描述，再回到代码落地。
3. **变量保存**：Memory Server 记录当前任务进度或后续待办，避免上下文丢失。
4. **SQL 验证**：通过 SQL Server MCP 快速验证 EF 查询结果，与测试数据对齐。
5. **Playwright 调试**：在需要录制桌面端交互脚本时，调用 Playwright MCP 执行自动化脚本。test
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

// Context7：医学术语解释
await mcp.run({
  server: "context7",
  method: "analyze",
  params: { text: "诊疗工作台" }
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

// SQL Server：检查测试数据库
await mcp.run({
  server: "sql-server",
  method: "executeQuery",
  params: { query: "SELECT TOP 10 * FROM Patients" }
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
  - 针对专业知识/法规问题，通过 nalyze 获取解释；summarize 总结长文本；xpand 用于生成背景说明。
  - 可传 language 参数控制输出语言；适合处理医疗术语或政策要求。
- **Memory**（https://github.com/modelcontextprotocol/servers/tree/main/src/memory）
  - 提供 save、load、delete、clear 方法，用于记录当前会话中的临时结论、下一步计划。
  - 建议整理任务列表或会议纪要，随时用 load 取出复盘。
- **Git**（内置 git-mcp-server）
  - 常用方法：status、diff、log、pplyPatch、commit、checkout。
  - 可在不离开 Claude 的情况下查看差异、生成 patch；提交前仍需在本地终端验证。
- **Playwright**（https://github.com/microsoft/playwright-mcp）
  - 方法：xecute；params.script 为 JS/TS 测试脚本；可设置 rowser、headless。
  - 用于录制/回放桌面或 Web 交互，支持截图、PDF、视频输出。
- **SQL Server**（基于 @executeautomation/database-server）
  - 方法：xecuteQuery（返回结果集）、xecuteNonQuery（执行 DML）、xecuteScalar。
  - 默认连接 localhost/LYBTDB，凭据 sa / LybtAdmin2025@SecurePass!；执行增删改前需确认是否使用测试数据库。

> 所有工具均可通过 wait mcp.run({ server, method, params }) 调用，可组合使用，例如：
> 1) Filesystem searchText 找到术语；2) Serena xecute 生成重构方案；3) Git pplyPatch 应用修改；4) SQL Server 验证结果。

- 充分利用Serena Server MCP的功能。
- 整个开发过程用GitHub跟踪。
