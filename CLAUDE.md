# CLAUDE.md

本文件定义 Claude Code 在仓库中的工作约束与执行流程，确保所有改动可追踪、可验证、符合项目标准。

## 1. 角色定位与必读资料
- **定位**：Claude Code 作为智能顾问，负责方案筹划、代码实现、初步审查与文档同步；最终合并由人工审核决定。
- **必读文档**：
  - `README.md`（项目权威概览）
  - `docs/index.md`（文档导航体系）
  - `docs/development/standards.md`、`docs/development/coding-and-implementation-specification.md`
  - `docs/architecture/server-module-design-standard.md`（Server端模块设计标准）
  - `docs/architecture/client/unified-design-standard.md`（Client端业务模块统一设计标准）
  - `docs/development/minimal-practice.md`（Issue→清单→PR 工作法）
  - `docs/development/documentation-guidelines.md`
  - `docs/PROJECT-STATUS-2025-09-27.md`（实时项目状态）

> 处理任务前必须先查阅相关文档，未理解文档禁止开始编码或给出建议。
>
> **架构设计标准**：
> - Server 端开发必须遵循 `server-module-design-standard.md`（三层架构、禁止CQRS、接口统一位置）
> - Client 端开发必须遵循 `client/unified-design-standard.md`（MVVM三层、依赖注入标准、AutoMapper强制、代码模板）

## 2. Issue 驱动工作流

### 2.1 任务启动前置检查
1. `git pull` → 获取最新主分支。
2. `dotnet build LYBT.All.sln` → 若失败，优先修复再继续任务。
3. `dotnet test LYBT.Server.sln` → 记录基线失败项，评估是否影响任务。

### 2.2 Issue 生命周期
- **单一事实源**：所有代码、配置、脚本改动必须先有 GitHub Issue（含验收标准），并且 Issue 必须直接在 GitHub 上创建与维护，严禁在本地文档或其他渠道先行创建再同步。
- **模块化功能清单**：Issue 创建后按 `docs/development/minimal-practice.md` 生成模块化条目（示例前缀 `[SRV-1]`、`[CLI-1]`、`[DOC-1]`），条目需描述产出路径与验收点；范围调整时生成 v2 清单并附变更摘要。
- **标签与状态**：
  - 新建 Issue → 自动打 `status: todo` 并提示分支命名。
  - 启动任务 → 人工加 `status: in-progress`。
  - PR 合并 → 自动加 `status: done` 并触发关单。
- **自动化**：Workflow 会检查 PR 与 Issue 关联、执行兜底关单、同步状态；若未使用 `Fixes/Closes/Resolves #<issue>`，仍需在 PR 中引用 Issue 号码以便脚本识别。

### 2.3 PR 与代码审查
1. **分支与提交**：基于 Issue 建分支，提交信息用中文、包含清单编号。
2. **PR 模板**：
   - Issue 清单项完成后，由 Claude 自动生成 PR 草稿（包含模板、关单关键字、编译摘要），供人工核查实现情况后确认提交。
   - 说明引用的清单编号与验收结果。
   - 粘贴编译命令及结果（至少 `dotnet build LYBT.All.sln -c Release`）。
   - AI 审查清单：**GitHub Copilot 初审**（自动），**Claude Code 二审**（评论模式，可选）。
3. **AI 代码审查流程**：
   - **GitHub Copilot 初审**（自动触发）：
     - 检查代码规范、潜在问题、最佳实践。
     - 自动在 PR 中发表审查评论。
   - **Claude Code 二审**（可选，评论模式）：
     - 必读 `docs/development/standards.md` 与 Issue 验收标准。
     - 判断是否满足验收、遵守架构禁令（禁止 MediatR/CQRS 等）、命名与异步规范、文档同步、增量原则、测试覆盖。
     - 以**评论模式**发布审查意见（因 GitHub 限制，PR 作者不能 approve 自己的 PR）。
     - 未满足标准时仅给出最小修复建议。
4. **合并与关闭**：人工审核通过后合并；Workflow 自动关闭 Issue 并更新标签。若 PR 被拒绝，关闭 PR 并创建新 Issue 重新立项。
- GitHub CLI (`gh`) 已在本仓库完成认证配置：当 PR 符合上文审核要求且检查通过时，可直接使用 `gh pr review`/`gh pr merge` 等命令完成审批与合并；如检查未通过，必须先推动提交人修复直至满足要求后再合并。

### 2.4 完成后的文档系统更新
- 合并后，必须同步更新文档系统，确保“需求→任务→实现→总结”的链路完整：
  - 在相关模块文档补充/修订实现差异（如 `docs/architecture/modules/<module>/README.md`）。
  - 更新需求/功能清单（如存在对应清单或 `docs/issues/` 的说明文件）。
  - 如涉及接口/流程/标准，更新 `docs/api/`、`docs/development/` 或 `docs/architecture/` 对应文件。
  - 若产出分析或报告，归档至 `docs/reports/` 并在 `docs/reports/INDEX.md` 登记。
  - 必要时在 `docs/index.md` 增加/修正导航链接。
  - 原则：任何对代码行为的改变都必须有文档镜像更新。

### 2.5 人工与自动化分工
- **人工**：创建/完善 Issue、启动 Issue、最终审核与合并。
- **自动化**：Issue 欢迎语与标签、PR 关联校验、关单兜底、状态同步、命名/清单校验。
  - 完成清单后自动生成 PR 草稿内容（含模板、自动关闭关键字、命令摘要），供人工核查后正式提交。

## 3. 执行原则
1. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则，引用具体文档路径说明依据。
2. **最小充分交付**：遵循“完成导向、够用即好”，避免超前设计；重构建议在分析阶段标注“可选”。
3. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述，保留现有结构。
4. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档；文档与代码同时更新。
5. **文档归位**：新增或更新 Markdown 文档时，必须按照 `docs/development/documentation-guidelines.md` 与 `docs/development/file-organization-guidelines.md` 指定的目录结构存放，并同步更新 `docs/index.md` 等索引；禁止在根目录或未备案位置随意生成文档。
6. **MVP 约束**：所有代码修改与优化均须符合现有 MVP 需求；禁止私自扩展或新增功能。若讨论确认需拓展功能，必须先更新 MVP 文档/Issue，明确范围与验收后方可执行。
6. **输出归档**：脚本或分析流程生成的报告、CSV、日志等产出，需写入指定目录（例如 `docs/reports/`、`docs/analysis/`、`scripts/analysis/outputs/`）；根目录禁止新增临时输出文件。
7. **安全与合规**：严格遵守 `docs/PROJECT-STATUS-2025-09-27.md` 中的技术决策和黑名单（禁止 Redis、微服务、CQRS、Docker、GraphQL 等）。
8. **并行优先**：适用于所有任务类型（代码实现、代码分析、文档整理、测试等）。当 Issue 含多个相互独立的子任务时，优先规划并行执行；在模块化清单中标注可并行项，结合 `sequential-thinking` 评估依赖，再同时发起所需的 MCP 调用，以提升整体效率。
9. **思考强度分级**：根据任务复杂度选择适当的思考模式——常规任务默认 `think`，中等复杂场景使用 `think hard`，复杂跨模块需求使用 `think harder`，涉及系统级影响或高度不确定性时启用 `ultrathink`。确保在进入实现前完成相应级别的分析与记录。

### 3.1 输入模式与自动化（简明解释）

- 输入模式 A（轻量）：你只给“一句话问题”。
  - 自动化步骤：
    1) `sequential-thinking` 生成最小分析清单；
    2) `context7` 查阅代码/文档；
    3) 形成“简版分析卡片”（复现命令、日志要点、可能模块、初步验收）；
    4) 在 GitHub 创建 Issue 草案；
    5) 等你确认后“启动 Issue”。
  - 护栏：若信息不足（缺少 AC/复现/日志），不直接开单，先生成“补充信息请求”。
  - 节流：限制一次并行创建的子任务数（默认≤5）。

- 输入模式 B（重型）：你提供一份报告。
  - 自动化步骤：
    1) `context7` 对照文档系统做一致性校验；
    2) `sequential-thinking` 产出依赖图（哪些并行、哪些串行）；
    3) 依据最小变更原则拆分为父/子 Issue（带最小变更集与 AC）；
    4) 在 GitHub 批量创建父/子 Issue；
    5) 等你确认后按依赖调度启动。
  - 护栏：若方案涉及新增功能，先按 MVP 约束更新需求文件/Issue 并获确认。
  - 节流：对子 Issue 分批（batch-1/2）启动，避免过载。

说明：
- “自动化步骤”指 AI 使用 MCP 工具（context7、sequential-thinking、git 等）完成的标准动作；
- “护栏”是防止错误开单/越权变更的硬约束；
- “节流”是控制并行度与任务规模的规则，避免一次性开出过多子任务导致管理成本失控。

## 4. 编码与交付要求
- **Issue 驱动开发**：无 Issue 禁止改动。
- **语言统一**：代码注释、终端输出、提交信息均使用中文。
- **文件编码**：所有文本文件使用 `UTF-8 with BOM`（参考 `docs/development/file-organization-guidelines.md`）。
- **命名规范**：类型与公开成员 PascalCase，私有字段 `_camelCase`，常量 UPPER_SNAKE_CASE，异步方法 `Async` 结尾。
- **依赖注入**：仅用构造函数注入；禁止 `Container.Resolve`、`ServiceLocator`（`docs/development/standards.md`）。
- **异步约定**：涉及 I/O 必须 async/await，避免阻塞。
- **文件体量**：单文件建议 ≤500 行，复杂逻辑拆分模块。
- **测试**：新增/修改核心逻辑需补充单元或集成测试，优先使用既有测试框架。
- **文档同步**：改动涉及架构、接口、流程时更新对应 README/索引（`docs/development/documentation-guidelines.md`）。
- **脚本归档**：新增或调整自动化脚本时，必须放置在 `scripts/` 目录（可按用途划分子目录），并更新引用路径说明。

## 5. 常用命令（PowerShell）
```powershell
# 还原/构建
 dotnet restore LYBT.All.sln
 dotnet build LYBT.Server.sln -c Release --no-restore
 dotnet build LYBT.Desktop.sln -c Release --no-restore

# 运行 WebAPI
 dotnet run --project src/Server/Services/LYBT.WebAPI

# 格式化与测试
 dotnet format LYBT.All.sln
 dotnet test LYBT.Server.sln -c Release
```

## 6. MCP 工具使用准则（全过程协同，效率优先）
- **统一入口**：AI 协同的全流程（需求澄清、方案拟定、检索/分析、实现、审查、文档与报告维护）均应尽可能通过 `mcp.run()` 调用完成，形成可复现的操作日志与高效分工。
- **效率优先**：在不违反仓库约束（MVP、Issue 先行、文档/输出归位等）的前提下，优先选择使用 MCP 工具提升速度与质量：
  - 需求阶段：用 `sequential-thinking` 梳理步骤与依赖；用 `time` 标注时序与截止；必要时用 `context7` 查询历史实现/文档。
  - 方案阶段：结合 `context7` 引用文档与代码片段，快速产出最小变更集与 AC；必要时用 `serena.find_symbol` 分析现有实现与依赖关系。
  - 实施阶段：用 `filesystem` 定位/读写文件，`git` 生成差异与补丁；遵循 Issue 先行与文档归位。
  - 审查阶段：用 `serena.find_referencing_symbols` 检查改动影响范围，结合自检清单校验一致性；`context7` 回溯依据；`git` 生成 PR 草稿补丁。
  - 文档与报告：将分析输出统一落在 `docs/` 与 `scripts/analysis/outputs/`，并通过 MCP 完成索引更新与链接校验。
- **核心服务**（工具列表）：

| 工具 | 主要用途 | 参数约定 |
|------|---------|---------|
| `filesystem` | 文件读写、目录遍历、批量操作 | camelCase |
| `git` | 版本控制（status, diff, commit, log 等） | camelCase |
| `serena` | 语义代码检索与编辑（基于 LSP） | snake_case |
| `context7` | 查询库文档与代码示例 | camelCase |
| `memory` | 知识图谱存储（实体-关系模型） | camelCase |
| `sequential-thinking` | 结构化推理与步骤分解 | camelCase |
| `time` | 时区转换与时间标准化 | snake_case |
| `playwright` | 浏览器自动化（按需使用） | camelCase |
| `github-cli (gh)` | Issue/PR管理（命令行工具） | - |
- **容错策略**：调用失败时解析错误 → 修正参数重试一次；仍失败即报告阻塞及报错信息。
 - **文档/库查询**：涉及外部依赖或 API 时优先通过 `context7__resolve-library-id`、`context7__get-library-docs` 获取权威说明。
 - **AI 辅助协同逻辑**（优先使用 MCP 工具）：
   1) Context7 获取权威资料与代码片段
   2) sequential-thinking 拆解任务步骤
   3) serena 执行语义级代码操作（find_symbol → replace_symbol_body）
   4) git 记录变更历史

**📚 详细参考**：[MCP工具参考手册](docs/development/mcp-tools-reference.md) - 包含完整参数规范、调用示例、工作流模式与错误处理策略

### 3.1 代码修复后的后台清理（Run-to-Completion Hygiene）

为避免测试通过后遗留的运行中后台进程或临时环境状态影响后续验证，完成修复并通过测试后，必须执行以下清理：

- 终止临时进程与守护：停止为本次验证启动的 WebAPI/桌面端/脚本（如 `dotnet run`）。
- 释放资源与缓存：清理内存缓存/临时文件/本地数据沙箱（如 `BIN/`, `logs/`, `TestResults/` 等，禁止入库）。
- 还原配置与环境变量：移除测试期设置的临时变量（如 `ASPNETCORE_URLS`）、测试密钥/连接串，防止污染后续运行。
- 关闭外部连接：断开数据库连接、HTTP 调试代理、自动化会话，避免后台会话滞留。
- 证据归档：将需要保留的日志片段/截图/命令输出收敛到 PR 或 Issue 评论；不要长时间保留大体量日志在工作目录。
- 端口检查：确认 5001 等端口未被占用；必要时提供释放命令（Windows `netstat -ano`/`taskkill`，Linux `lsof`/`kill`）。
- 文档同步：如清理步骤依赖脚本或特定命令，在 `docs/development/minimal-practice.md` 或相关 README 中补充最小指引。
- **使用指引**：
  - 处理跨模块或高风险任务时，优先调用 `sequential-thinking` 输出结构化步骤，再据此安排 `filesystem`/`git` 操作，并在 Issue/文档中引用该步骤列表。
  - 需要记录或比较截止时间、部署窗口、执行耗时时，调用 `time` 获取标准化时间戳（UTC 与本地）并写入 Issue、报告或日志。
  - 在修改代码或理解既有逻辑前，先使用 `context7` 索引/查询相关文件与文档，确保在充分掌握上下文后再动手，实现与文档保持一致。

---
以上约束如需调整，须先在 GitHub Issue 中提出并获批准，再同步更新本文档及相关标准。
- issue默认创建在 GitHub 上
