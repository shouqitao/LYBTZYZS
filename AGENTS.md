# 仓库协作指南

本文件定义所有智能体在仓库中的协作规范、角色定位与通用开发要求。除非额外说明，所有协作交流与产出必须使用中文。

## 目录
1. 角色与定位
2. 项目结构与构建说明
3. 编码与提交约定
4. 测试与质量保障
5. 安全与配置要求

---

## 1. 角色与定位
| 角色 | 身份说明 | 职责重点 | 语言规范 |
| --- | --- | --- | --- |
| Thinker（ChatGPT） | 高级 .NET 架构师、代码评审顾问 | - 分析代码现状与风险<br>- 制定任务计划与重构策略<br>- 指导编码顺序与测试策略 | 必须使用中文分析、给出方案和反馈 |
| Coder（Claude Code） | 主要编码执行者 | - 根据 Thinker 的任务说明进行开发<br>- 及时同步实现细节，并遵守文档/提交规范 | 必须使用中文说明进展、回复日志 |
| 其他协作者 | 包括 Gemini、Serena 等工具链 | - 按角色说明执行特定任务（测试、脚本等）<br>- 如需新增角色，必须在本文件登记 | 按约定使用中文 |

> 若 Thinker 与 Coder 之间存在任务冲突或信息缺漏，Thinker 负责协调并更新文档。

---

## 2. 项目结构与构建说明
- 项目目录：`src/Server`、`src/Client/Desktop`、`src/Shared`、`tests`、`docs`、`scripts`，统一产物输出 `BIN/`。
- 源码分布：
  - `src/Server`：ASP.NET Core Web API。
  - `src/Client/Desktop`：WPF 客户端（Prism + UltraThink 架构）。
  - `src/Shared`：DTO、接口、公共工具。
- 构建与运行：
  - 还原：`dotnet restore LYBT.All.sln`
  - 构建：`dotnet build LYBT.All.sln -c Release --no-restore`
  - 运行 API：`dotnet run --project src/Server/Services/LYBT.WebAPI`
  - 格式化：`dotnet format LYBT.All.sln`
- 测试命令：
  - 单元测试：`dotnet test tests -c Release --no-build`
  - 架构测试：`dotnet test tests/Architecture/LYBT.ArchTests.csproj`
  - 覆盖率：`dotnet test tests -c Release --collect:"XPlat Code Coverage"`

---

## 3. 编码与提交约定
- 语言与编码：`UTF-8`、`CRLF`，移除行尾空白；所有注释与输出使用中文。
- 缩进规范：C# 4 空格；XML/JSON/YAML 2 空格。
- `using` 顺序：`System.*` 优先，统一放在命名空间外。
- 花括号：左花括号换行；`else/catch/finally` 前换行。
- 命名约定：类型与公开成员 PascalCase；接口以 `I` 开头；私有字段 `_camelCase`；异步方法以 `Async` 结尾。
- 提交规范：遵循 Conventional Commits（示例：`feat(patients): add CRUD endpoints`）。
- Pull Request 要求：说明变更、关联 Issue、附测试/截图（若涉及 UI），并保持提交原子化。

---

## 4. 测试与质量保障
- 测试框架：xUnit、FluentAssertions、Moq、Verify、NetArchTest；覆盖率工具使用 Coverlet。
- 测试组织：
  - 单元测试、集成测试、架构测试统一存放在 `tests/`，文件名以 `*Tests.cs` 结尾。
  - 覆盖公共 API、边界条件与回归路径。
- 要求：
  - 所有测试在提交前必须通过。
  - 架构测试、关键模块测试需在合并前重新执行。
  - CI 需采集覆盖率报告（无硬性阈值，但持续提升）。
- Thinker 有权根据决策需要，发起两种任务：【代码任务】与【审查任务】。
  - **审查任务**: 在规划新任务或需要了解代码现状时，Thinker 可随时发布此任务。其目的有两个：一是评估代码质量，二是帮助 Thinker 了解实现细节，为制定下一步计划提供依据。审查由 Coder 调用 Serena 完成。
  - **代码任务**: 主要遵循“发布-执行-审查-迭代”的闭环模式：
    1. **发布**: Thinker 根据总体目标发布明确的开发任务。
    2. **接收总结**: Coder 完成任务后，Thinker 接收其工作总结以及由 Serena 生成的【代码任务】审查报告。
    3. **带目的审查**: 基于上述总结和报告，Thinker 对代码进行一次有目的性的、高层次的最终审查。
    4. **综合迭代**: 结合【Coder 的工作总结】、【Serena 的审查报告】以及【用户的补充需求】，Thinker 综合分析并规划出下一个迭代任务。

---

## 5. 安全与配置要求
- 依赖版本需集中管理于 `Directory.Packages.props`，新增依赖需经评审。
- 禁止提交任何密钥、敏感凭据；本地开发使用 `appsettings.Development.json` 或环境变量。
- 优先使用 EF Core 隐式事务；显式事务需范围最小并附注释。
- 遵守 Record-Only 基线与 `/api/v1/*` 路由规范，不引入禁用框架。

---

## 任务发布与总结流程
- Thinker 在 `docs/tasks/pending/` 发布任务说明，文件命名建议为 `YYYY-MM-DD-任务名称.md`，并写明背景、目标、验收点。
- Coder 在执行任务时，以该文件为唯一权威来源；若信息不完整，应向 Thinker 反馈补充并更新任务文件。
- 任务完成后，Coder 必须在 `docs/tasks/completed/` 生成同名总结文件（推荐追加 `-summary.md` 后缀），记录实现细节、测试结果、遗留问题和后续建议。
- 如任务涉及其他智能体或文档改动，需在总结中列出受影响的文件或流程，便于 Thinker 与团队审查。

### 协作提醒
1. Thinker 负责把控总体架构与任务优先级，并指导 Coder 执行。
2. Coder 遇到阻塞或信息缺口时，需及时向 Thinker 汇报并等待确认。
3. 所有角色在更新约定后，必须同步刷新相关文档（如 README、CLAUDE.md）。
4. Thinker 在发布每项新任务前，必须结合最新代码现状完成必要的审计，确保任务要求不脱离实际实现。

如需新增角色或调整职责，请在获得一致同意后更新本文件并知会团队。

