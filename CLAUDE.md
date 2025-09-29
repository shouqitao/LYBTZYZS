# CLAUDE.md

本文件定义 Claude Code 在仓库中的工作约束与执行流程，确保所有改动可追踪、可验证、符合项目标准。

## 1. 角色定位与必读资料
- **定位**：Claude Code 作为智能顾问，负责方案筹划、代码实现、初步审查与文档同步；最终合并由人工审核决定。
- **必读文档**：
  - `README.md`（项目权威概览）
  - `docs/index.md`（文档导航体系）
  - `docs/development/standards.md`、`docs/development/coding-and-implementation-specification.md`
  - `docs/development/minimal-practice.md`（Issue→清单→PR 工作法）
  - `docs/development/documentation-guidelines.md`
  - `docs/PROJECT-STATUS-2025-09-27.md`（实时项目状态）

> 处理任务前必须先查阅相关文档，未理解文档禁止开始编码或给出建议。

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
   - 说明引用的清单编号与验收结果。
   - 粘贴编译命令及结果（至少 `dotnet build LYBT.All.sln -c Release`）。
   - 勾选 `Claude Code 初审`，如调用 Serena 完成二审，勾选 `Serena 二审`。
3. **AI 代码审查清单**：
   - 必读 `docs/development/standards.md` 与 Issue 验收标准。
   - 判断是否满足验收、遵守架构禁令（禁止 MediatR/CQRS 等）、命名与异步规范、文档同步、增量原则、测试覆盖。
   - 未满足标准时仅给出最小修复建议。
4. **合并与关闭**：人工审核通过后合并；Workflow 自动关闭 Issue 并更新标签。若 PR 被拒绝，关闭 PR 并创建新 Issue 重新立项。

### 2.4 人工与自动化分工
- **人工**：创建/完善 Issue、启动 Issue、最终审核与合并。
- **自动化**：Issue 欢迎语与标签、PR 关联校验、关单兜底、状态同步、命名/清单校验。

## 3. 执行原则
1. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则，引用具体文档路径说明依据。
2. **最小充分交付**：遵循“完成导向、够用即好”，避免超前设计；重构建议在分析阶段标注“可选”。
3. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述，保留现有结构。
4. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档；文档与代码同时更新。
5. **安全与合规**：严格遵守 `docs/PROJECT-STATUS-2025-09-27.md` 中的技术决策和黑名单（禁止 Redis、微服务、CQRS、Docker、GraphQL 等）。
6. **并行优先**：适用于所有任务类型（代码实现、代码分析、文档整理、测试等）。当 Issue 含多个相互独立的子任务时，优先规划并行执行；在模块化清单中标注可并行项，结合 `sequential-thinking` 评估依赖，再同时发起所需的 MCP 调用，以提升整体效率。

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

## 6. MCP 工具使用准则
- **统一入口**：所有文件/Git/SQL/分析操作须通过 `mcp.run()`，形成可复现日志。
- **核心服务**：
  - `filesystem`：目录遍历、读写文件；写入前确认路径。
  - `git`：`status`、`diff`、`log`、`applyPatch`、`commit`。
  - `context7`：索引与查询 `src/`、`docs/` 内容；缺乏上下文时先 `add` 再 `query`。
  - `serena`：`plan`（复杂任务拆解）、`execute`（深度分析）、`proofread`（PR 审查）。
  - `memory`：记录临时笔记或 TODO。
  - `playwright`：运行桌面/Web 自动化脚本（仅在任务要求时使用）。
  - `sequential-thinking`：在复杂任务或需要严密推理时生成逐步思考记录；调用后按返回的步骤逐一落实，可作为方案/复盘的附件引用。
  - `time`：获取标准化时间信息（UTC、本地时区、倒计时等）；用于安排截止日期、记录操作时间戳或在文档中标记时间。
- **容错策略**：调用失败时解析错误 → 修正参数重试一次；仍失败即报告阻塞及报错信息。
- **文档/库查询**：涉及外部依赖或 API 时优先通过 `context7__resolve-library-id`、`context7__get-library-docs` 获取权威说明。
- **使用指引**：
  - 处理跨模块或高风险任务时，优先调用 `sequential-thinking` 输出结构化步骤，再据此安排 `filesystem`/`git` 操作，并在 Issue/文档中引用该步骤列表。
  - 需要记录或比较截止时间、部署窗口、执行耗时时，调用 `time` 获取标准化时间戳（UTC 与本地）并写入 Issue、报告或日志。
  - 在修改代码或理解既有逻辑前，先使用 `context7` 索引/查询相关文件与文档，确保在充分掌握上下文后再动手，实现与文档保持一致。

---
以上约束如需调整，须先在 GitHub Issue 中提出并获批准，再同步更新本文档及相关标准。
