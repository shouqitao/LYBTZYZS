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
`n## 工具与效率提升

## 任务交付流程
- Thinker 发布的全部开发任务固定存放在 `docs/tasks/pending/`，文件命名建议为 `YYYY-MM-DD-任务名称.md`，包含背景、目标、验收点。
- Claude Code 在启动任务前，应确认对应任务文件并可在本地记录进展；若任务信息不完整，需先向 Thinker 反馈补充。
- 任务完成后，必须在 `docs/tasks/completed/` 中以同名文件追加 `-summary.md`（或在原任务文件中新增“完成情况”段），总结实现内容、测试结果、遗留风险与后续建议。
- 若任务涉及 README 或其他文档调整，请在总结中明确指出已更新的文件列表，方便 Thinker 审核。

## 测试与质量策略
- 当前桌面端缺少自动化测试，服务器端测试仍有失败用例。
- 优先补齐以下测试：
  - `SessionManager`：验证登录、诊疗状态切换及事件发布。
  - `UnifiedEventHandler`：验证状态消息、错误事件发布逻辑。
  - 关键导航服务与 ViewModel 命令逻辑。
- 推荐技术栈：xUnit、FluentAssertions、Moq、Bogus。
- 阶段目标：在修复失败用例后，将关键模块覆盖率提升至 **≥30%**，再逐步迈向 60%。

## 文档维护要求
1. README.md 由 Thinker 负责维护；Coder 专注代码实现。如发现 README 存在偏差，Thinker 必须优先更新或发布补充任务。
2. 每次调整架构、术语或关键流程时，必须同步更新 `README.md` 及相关 `docs/requirements/*` 文件。
3. `docs/reports/prism-8x-desktop-refactor-plan-2025-09-24.md` 应按 Phase A/B/C/D 的进展实时维护。
4. 本文件若新增约定或排除项，也需同步在 README 中体现。

## 常见陷阱
- 保留多套事件/枚举导致命名冲突与编译失败。
- 在 ViewModel 中直接访问容器或具体实现，破坏可测试性。
- 忽略 Shell 对资源字典的引用，造成转换器解析失败。
- 术语未同步更新，导致 README、UI、代码描述不一致。

## 默认环境信息
- **数据库**：SQL Server（推荐实例：`localhost/LYBTDB`）。
- **API 开发地址**：`http://localhost:5001`。
- **默认账号**：`sysadmin / LybtAdmin2025@SecurePass!`。
- **JWT 配置**：默认有效期 8 小时，记住我模式 30 天。

## Git 提交规范
```text
格式：<类型>(范围): <主题>
常用类型：
- feat：新增功能
- fix：缺陷修复
- refactor：重构（无功能变化）
- docs：文档更新
- test：测试相关变化
- chore：构建、脚本或依赖调整
```

## 语言与沟通要求
- Claude Code 及所有协同工具输出必须为中文。
- 对外表述统一使用“诊疗工作台”等最新术语，避免使用“看诊”旧称呼。
- 在提交代码前，请逐项核对“当前最高优先级任务”是否已完成；如未完成，请先处理阻断项。

请在开始任何编码工作前再次阅读本文件，并确保工作成果与上述要求保持一致。如发现内容与最新需求不符，请立即反馈并更新文档。





## MCP 工具快速参考

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
  - 方法：eadFile、writeFile、listDirectory、stat、searchText、createDirectory、delete
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

