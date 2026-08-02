# Mimo Code 深度学习报告

> 2026-08-02 | 基于 v0.1.9 | 893 sessions / $178.65 总成本

---

## 一、Compose Agent 内部技能（14 个）

Compose agent 是 Mimo 的核心编排引擎，内置 14 个阶段技能：

| 技能 | 用途 | 何时触发 |
|------|------|----------|
| **brainstorm** | 探索需求→设计→获得批准 | 任何创造性工作前（**硬门禁**） |
| **plan** | 生成实现计划（任务分解） | brainstorm 通过后 |
| **execute** | 在隔离 workspace 中执行计划 | plan 通过后 |
| **subagent** | 当前 session 内派发子 agent 执行 | plan 的任务互相独立时 |
| **parallel** | 并行派发多个独立 agent | 3+ 个不相关的失败/任务 |
| **worktree** | 创建 git worktree 隔离工作区 | 开始 feature work 前 |
| **review** | 代码审查（spec 合规 + 质量） | execute/subagent 完成后 |
| **feedback** | 收集用户反馈 | review 发现问题时 |
| **verify** | 验证实现（build/test） | review 通过后 |
| **merge** | 合并分支 | verify 通过后 |
| **report** | 生成交付报告 | merge 完成后 |
| **tdd** | 测试驱动开发流程 | 任何实现前（**硬门禁**） |
| **debug** | 结构化调试 | 遇到 bug 时 |
| **ask** | 向用户提问（结构化选项） | 需要决策时 |

### 关键洞察

- **brainstorm 是硬门禁**：compose agent 在执行任何创造性工作前必须先 brainstorm（探索→设计→批准）。但 autonomous 模式下（无人可用时）自动跳过
- **tdd 也是硬门禁**：先写测试→看失败→写最小实现。除非是 throwaway prototype
- **subagent vs execute**：subagent 在当前 session 内派发（无上下文切换），execute 在隔离 session 中执行（并行）
- **worktree 自动隔离**：compose agent 在开始 feature work 时自动创建 git worktree，避免污染主分支

---

## 二、Builtin Skills（23 个）

### 开发相关（高价值）

| 技能 | 描述 | 对我们的价值 |
|------|------|-------------|
| **compose-next** | 端到端流程：Grill→Spec→Workspace→Implement→Verify→Review→Merge | ⭐ 比默认 compose 更紧凑的替代方案 |
| **deep-research** | 并行子 agent 深度调研，生成带引用的报告 | ⭐ 技术选型、架构调研 |
| **super-research** | 8 种研究模式：实验循环/主题调研/量化分析/对比评测/根因排查/消融实验/论文复现/论文写作 | ⭐ 性能优化、技术对比 |
| **skill-creator** | 交互式创建/审查/改进 agent skills | 积累项目知识 |
| **evolve** | 自修改系统：hooks/tools/skills/workflows/TUI plugins | ⭐⭐ 强大但需要理解 |
| **loop** | 定时循环执行 prompt（cron 式） | CI 监控、定期检查 |

### 文档生成

| 技能 | 描述 |
|------|------|
| **docx-official** | 生成 Word 文档 |
| **pptx-official** | 生成 PowerPoint |
| **xlsx-official** | 生成 Excel |
| **pdf-official** | 生成 PDF |
| **design-blueprint** | 设计蓝图 |

### 研究/分析

| 技能 | 描述 |
|------|------|
| **arxiv** | arXiv 论文搜索 |
| **data-analytics** | 数据分析 |
| **learn-everything** | 学习任何主题 |
| **research-paper-writing** | 学术论文写作 |

### 设计/创意

| 技能 | 描述 |
|------|------|
| **frontend-design** | 前端设计 |
| **product-design** | 产品设计 |
| **html-to-video-pipeline** | HTML 转视频 |

### 其他

| 技能 | 描述 |
|------|------|
| **claude-code** | Claude Code 集成 |
| **drive-mimo** | Google Drive 集成 |
| **mimocode-docs** | Mimo Code 文档 |
| **modern-python-toolchain** | 现代 Python 工具链 |
| **sales** | 销售相关 |

---

## 三、高级 CLI 功能（你可能不知道的）

### 1. `--role assistant` — 助手注入模式

```bash
mimo run --role assistant '这是之前的工作成果，请继续完成剩余部分'
```

**用途**：以助手身份注入消息，Mimo 会将其视为模型输出然后继续执行。适合：
- 注入之前的分析结果让 Mimo 继续
- 模拟对话上下文

### 2. `--command` — 命令模式

```bash
mimo run --command 'test' -- '额外参数'
```

**用途**：使用预定义命令，message 作为参数传递。

### 3. `--fork` — 分叉 Session

```bash
mimo run --continue --fork '在新分支上继续开发'
```

**用途**：从当前 session 分叉出新 session，保留历史但独立执行。适合：
- 想尝试不同方案但保留原方案
- 从某个 checkpoint 尝试分支

### 4. `--share` — 分享 Session

```bash
mimo run --share '完成这个任务'
```

**用途**：生成可分享的 session 链接。

### 5. `--variant` — 推理强度

```bash
mimo run --variant high '复杂架构设计'
mimo run --variant max '需要深度推理的任务'
mimo run --variant minimal '简单机械任务'
```

**用途**：控制模型推理深度。高难度任务用 `high/max`，简单任务用 `minimal` 省 token。

### 6. `--thinking` — 显示思维链

```bash
mimo run --thinking '调试这个复杂 bug'
```

**用途**：显示模型的思考过程，帮助理解决策逻辑。

### 7. `--pure` — 无插件模式

```bash
mimo run --pure '纯代码任务'
```

**用途**：不加载任何外部插件/skills，减少干扰。

### 8. `mimo agent create` — 创建自定义 Agent

```bash
mimo agent create \
  --path .mimocode/agents/dotnet-reviewer.md \
  --description "专门审查 .NET 代码质量和架构合规性" \
  --mode primary \
  --tools "bash,read,grep,glob,edit" \
  -m opencode/deepseek-v4-pro
```

**用途**：创建专用 agent，可以：
- 限定工具集（只给需要的工具）
- 指定模型
- 定义行为模式

### 9. `mimo export/import` — Session 导出/导入

```bash
mimo export <sessionID> > session.json
mimo import session.json
```

**用途**：归档重要 session，或在机器间迁移。

### 10. `mimo session import-claude` — 导入 Claude Code Sessions

```bash
mimo session import-claude
```

**用途**：从 Claude Code 的 `~/.claude/projects` 导入 session 历史。

---

## 四、Evolve 系统（自修改能力）

这是 Mimo Code 最强大但最不为人知的功能。通过 `.mimocode/` 目录，你可以修改 Mimo 的每一个层面：

```
.mimocode/
├── tools/          # 自定义工具（包装命令/API）
├── hooks/          # 行为钩子（拦截/修改/阻止工具调用）
├── skills/         # 持久化知识（跨 session 记忆）
├── workflows/      # 工作流脚本（多 agent 流水线）
├── tui/            # TUI 插件（面板/命令/对话框）
├── agents/         # 自定义 agent 定义
└── mimocode.json   # 配置（MCP、权限等）
```

### Hook 示例

```
.mimocode/hooks/
├── block-dangerous-commands.js    # 阻止危险命令
├── auto-format-on-write.js        # 写入后自动格式化
└── enforce-commit-style.js        # 强制提交规范
```

### Skill 示例

```
.mimocode/skills/
├── dotnet-conventions/            # .NET 项目约定
│   └── SKILL.md
├── ef-core-patterns/              # EF Core 最佳实践
│   └── SKILL.md
└── wpf-debugging/                 # WPF 调试技巧
    └── SKILL.md
```

### Workflow 示例

```
.mimocode/workflows/
└── code-review.js                 # 自动化代码审查流水线
```

---

## 五、MCP 工具现状

| MCP Server | 状态 | 用途 |
|------------|------|------|
| **context7** | ✅ 连接 | 框架文档查询（.NET、EF Core 等） |
| **sequentialthinking** | ✅ 连接 | 结构化推理 |
| **codebase-memory** | ✅ 连接 | 代码图谱（28k 节点，SIMILAR_TO 查询） |
| **serena** | ❌ 超时 | 代码智能（重命名、引用查找、诊断） |
| **tavily** | ❌ 超时 | 网络搜索 |

### 注意

- **serena** 超时问题：已知在某些场景下会卡死（190s+），但不是全局禁用
- **tavily** 超时：可能是网络问题或 API key 过期
- **codegraph** 未配置到 Mimo（仅在 Hermes 中）

---

## 六、成本分析与优化

### 当前使用统计

- **893 sessions** / 91 天 / **$178.65**（日均 $1.96）
- **平均 10M tokens/session**，中位数 1.3M tokens/session
- **输入 321.2M** / 输出 14.5M / 缓存读 8562.3M

### 工具使用分布

| 工具 | 占比 | 说明 |
|------|------|------|
| read | 30.3% | 文件读取 |
| bash | 17.0% | Shell 命令 |
| edit | 16.1% | 文件编辑 |
| grep | 8.7% | 内容搜索 |
| task | 5.5% | 任务管理 |
| glob | 4.9% | 文件搜索 |
| write | 4.2% | 文件写入 |
| actor | 1.7% | 并行 agent |
| serena_* | ~5% | Serena MCP 工具 |

### 优化建议

1. **减少无效 read**：30% 的工具调用是 read，可能有大量重复读取
2. **用 grep 替代 read+循环**：先 grep 定位再精准 read
3. **用 `--variant minimal`** 简单任务省 token
4. **修复 serena/tavily**：恢复后可以减少 Mimo 自己的 grep/read 调用
5. **用 compose-next 替代默认 compose**：更紧凑的流程，减少 token 消耗

---

## 七、对 LYBTZYZS 项目最有价值的功能

### 1. 并行子 agent（compose:parallel）

当有 3+ 个独立任务时，Mimo 可以并行派发：

```
"同时修复这 3 个独立的 bug：LoginViewModel 的内存泄漏、
PrescriptionRepository 的 N+1 查询、DashboardView 的线程问题"
```

Mimo 会自动拆分为 3 个独立子 agent 并行执行。

### 2. 工作树隔离（compose:worktree）

大型重构时自动创建 git worktree，不影响主分支：

```
"重构整个 Users 模块，使用 git worktree 隔离"
```

### 3. TDD 流程（compose:tdd）

强制测试先行：

```
"为 MedicalCaseRepository 添加分页功能，使用 TDD"
```

Mimo 会：写测试 → 看失败 → 写最小实现 → 看通过 → 重构

### 4. 深度调研（deep-research）

技术选型时：

```
"调研 .NET 8 中最佳的 ORM 分页方案，对比 EF Core 原生、
Z.EntityFramework.Extensions、PagedList 的性能和 API"
```

### 5. 自定义 Agent（mimo agent create）

为 LYBTZYZS 创建专用 agent：

```bash
mimo agent create \
  --path .mimocode/agents/lybt-reviewer.md \
  --description "审查中医诊所管理系统的代码质量和架构合规性，关注 EF Core 查询、WPF 绑定、API 响应格式" \
  --mode primary \
  --tools "bash,read,grep,glob,edit" \
  -m opencode/deepseek-v4-pro
```

### 6. Evolve 自动化（.mimocode/hooks）

创建 hook 强制执行项目规范：

```
.mimocode/hooks/enforce-api-response.js
→ 每次写入 Controller 文件时自动检查是否使用 ApiResponse<T>
```

### 7. Skill 知识沉淀

将踩过的坑写成 skill：

```
.mimocode/skills/lybt-pitfalls/SKILL.md
→ 包含所有 AGENTS.md 中的 Common Pitfalls
→ Mimo 在执行时自动加载，避免重复犯错
```

---

## 八、使用建议（按优先级）

### 高优先级（立即可用）

1. **用 `--variant high` 处理复杂重构** — 当前所有任务都用默认推理强度
2. **修复 serena MCP** — 恢复后 Mimo 的代码理解能力大幅提升
3. **创建 LYBTZYZS 专用 skill** — 将 Common Pitfalls 沉淀为 Mimo 可用的 skill

### 中优先级（逐步引入）

4. **用 `mimo agent create` 创建专用 agent** — .NET Reviewer、EF Core Optimizer 等
5. **用 compose:parallel 处理独立任务** — 提速 2-3x
6. **用 evolve hooks 强制规范** — 减少 review 返工

### 低优先级（探索性）

7. **用 super-research 做性能基准测试** — 8 种研究模式
8. **用 loop 创建定期监控** — CI 状态、依赖更新检查
9. **用 `--share` 分享 session** — 团队协作

---

## 九、已知坑与注意事项

1. **`--role assistant` 不是万能的** — 它注入的是助手消息，不是用户消息，Mimo 的处理方式不同
2. **`--fork` 需要 `--continue` 或 `--session`** — 单独使用无效
3. **`--command` 需要预定义命令** — 不是所有命令都支持
4. **`--pure` 模式会禁用所有 skills** — 包括 compose 的内部技能
5. **自定义 agent 需要 `.mimocode/agents/` 目录** — 否则找不到
6. **evolve 的 hooks 是同步的** — 会增加每次工具调用的延迟
7. **MCP 超时问题** — serena/tavily 超时 30s，复杂查询可能不够
8. **cost 膨胀** — 平均 10M tokens/session，中位数 1.3M，说明有大量高消耗 session
