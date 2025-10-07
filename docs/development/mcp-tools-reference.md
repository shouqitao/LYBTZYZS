# MCP工具参考手册

> **版本**: 1.0
> **最后更新**: 2025-10-07
> **关联**: [CLAUDE.md](../../CLAUDE.md) Section 6

本文档提供凌隐宝堂中医诊所项目中所有MCP（Model Context Protocol）工具的详细参数规范、调用示例与最佳实践。

---

## 目录

1. [工具详细规范](#1-工具详细规范)
2. [工具协调与工作流](#2-工具协调与工作流)
3. [常见问题与错误处理](#3-常见问题与错误处理)

---

## 1. 工具详细规范

### 1.1 filesystem

**用途**: 文件系统操作（读写文件、目录遍历、批量处理）

#### 核心工具

| 工具 | 关键参数 | 说明 |
|------|---------|------|
| `read_text_file` | path (string), head? (number), tail? (number) | 读取文本文件（支持前N/后N行） |
| `write_file` | path (string), content (string) | 创建/覆盖文件（谨慎使用） |
| `edit_file` | path (string), edits[] {oldText, newText}, dryRun? (boolean) | 基于行的精确编辑 |
| `list_directory` | path (string) | 列出目录内容（[FILE]/[DIR]前缀） |
| `search_files` | path (string), pattern (string), excludePatterns? (string[]) | 递归搜索文件名 |
| `read_multiple_files` | paths[] (string[]) | 批量读取文件（效率优先） |

#### 调用示例

```xml
<!-- 读取文件（前50行） -->
<invoke name="mcp__filesystem__read_text_file">
  <parameter name="path">D:\source\repos\LYBTZYZS\CLAUDE.md</parameter>
  <parameter name="head">50</parameter>
</invoke>

<!-- 精确编辑文件 -->
<invoke name="mcp__filesystem__edit_file">
  <parameter name="path">D:\source\repos\LYBTZYZS\src\Example.cs</parameter>
  <parameter name="edits">[{
    "oldText": "public class Example",
    "newText": "public sealed class Example"
  }]</parameter>
  <parameter name="dryRun">false</parameter>
</invoke>

<!-- 批量读取 -->
<invoke name="mcp__filesystem__read_multiple_files">
  <parameter name="paths">["file1.cs", "file2.cs", "file3.cs"]</parameter>
</invoke>
```

#### 验证规则

- `path` 必须是绝对路径（Windows使用反斜杠或正斜杠均可）
- `edit_file` 的 `oldText` 必须完全匹配，否则编辑失败
- `write_file` 会直接覆盖，写入前应先用 `read_text_file` 确认
- `dryRun=true` 时仅预览变更，不实际修改文件

---

### 1.2 git

**用途**: 版本控制操作（所有参数使用 camelCase）

#### 核心工具（最常用工作流）

| 工具 | 关键参数 | 说明 |
|------|---------|------|
| `git_status` | path? (string, 默认当前目录) | 查询工作区状态 |
| `git_diff` | commit1? (string), commit2? (string), file? (string), staged? (boolean), includeUntracked? (boolean) | 差异对比 |
| `git_add` | files (string/string[], 默认"."), path? (string) | 暂存文件 |
| `git_commit` | message (string), author? {name, email}, amend? (boolean), filesToStage? (string[]) | 提交变更 |
| `git_branch` | mode (list/create/delete/rename/show-current), branchName? (string), force? (boolean) | 分支管理 |
| `git_checkout` | branchOrPath (string), newBranch? (string), force? (boolean) | 切换分支 |
| `git_log` | branchOrFile? (string), maxCount? (number), author? (string), showSignature? (boolean) | 提交历史 |

#### 调用示例（典型工作流）

```xml
<!-- 1. 查看状态 -->
<invoke name="mcp__git__git_status">
  <parameter name="path">D:\source\repos\LYBTZYZS</parameter>
</invoke>

<!-- 2. 查看差异 -->
<invoke name="mcp__git__git_diff">
  <parameter name="staged">false</parameter>
  <parameter name="includeUntracked">true</parameter>
</invoke>

<!-- 3. 暂存+提交 -->
<invoke name="mcp__git__git_commit">
  <parameter name="message">feat: 完善MCP工具参数规范

- 添加8个工具的详细参数表
- 补充调用示例与验证规则
- 统一参数命名约定（camelCase）

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude &lt;noreply@anthropic.com&gt;</parameter>
  <parameter name="filesToStage">["CLAUDE.md"]</parameter>
</invoke>

<!-- 4. 查看最近3次提交 -->
<invoke name="mcp__git__git_log">
  <parameter name="maxCount">3</parameter>
  <parameter name="showSignature">false</parameter>
</invoke>
```

#### 验证规则

- `message` 遵循 Conventional Commits（如 `feat:`, `fix:`, `docs:`）
- `git_set_working_dir` 应在会话开始时调用，设置默认工作目录
- `filesToStage` 可自动暂存文件，避免单独调用 `git_add`
- `amend` 仅用于修改最近一次提交，且确认未 push
- 提交信息末尾附加 Claude Code 标识和 Co-Authored-By

---

### 1.3 context7

**用途**: 查询库文档与代码示例

#### 核心工具

1. `resolve-library-id`：libraryName (string) → 返回 Context7 兼容的库ID
2. `get-library-docs`：context7CompatibleLibraryID (string, 必需), topic (string, 可选), tokens (number, 可选, 默认5000)

#### 调用流程

```xml
<!-- 步骤1: 解析库名 -->
<invoke name="mcp__context7__resolve-library-id">
  <parameter name="libraryName">AutoMapper</parameter>
</invoke>
<!-- 返回: /automapper/automapper -->

<!-- 步骤2: 获取文档 -->
<invoke name="mcp__context7__get-library-docs">
  <parameter name="context7CompatibleLibraryID">/automapper/automapper</parameter>
  <parameter name="topic">profile configuration</parameter>
  <parameter name="tokens">3000</parameter>
</invoke>
```

#### 验证规则

- `context7CompatibleLibraryID` 必须是 `/org/project` 或 `/org/project/version` 格式
- `tokens` 建议 3000-5000（平衡详细度与上下文消耗）
- 必须先调用 `resolve-library-id` 获取准确ID，除非用户明确提供格式化ID

---

### 1.4 serena

**用途**: 语义代码检索与编辑（基于 LSP）

#### 核心工具

- **符号搜索**: `find_symbol`（全局/局部符号搜索，支持类型过滤）、`find_referencing_symbols`（查找引用关系）
- **代码编辑**: `replace_symbol_body`（替换符号定义）、`insert_after_symbol`/`insert_before_symbol`（精确位置插入）
- **项目管理**: `activate_project`（激活项目）、`execute_shell_command`（执行 shell 命令）
- **文件操作**: `read_file`（读取文件）、`create_text_file`（创建/覆盖文件）

#### 参数约定

所有参数使用 **snake_case**（遵循Python LSP约定）

#### 最佳实践

- 始于干净 git 状态
- 使用结构化代码库
- 利用类型注解
- 谨慎审查变更

---

### 1.5 memory

**用途**: 知识图谱存储（实体-关系模型）

#### 核心工具（实体操作）

| 工具 | 关键参数 | 说明 |
|------|---------|------|
| `create_entities` | entities[] {name, entityType, observations[]} | 创建实体节点 |
| `create_relations` | relations[] {from, to, relationType} | 创建实体间关系 |
| `add_observations` | observations[] {entityName, contents[]} | 为实体添加观察 |
| `search_nodes` | query (string) | 搜索实体/观察内容 |
| `read_graph` | - | 读取整个知识图谱 |

#### 调用示例

```xml
<!-- 创建实体 -->
<invoke name="mcp__memory__create_entities">
  <parameter name="entities">[{
    "name": "Issue-1013",
    "entityType": "Task",
    "observations": ["Phase 1-5 完成", "涉及29个ViewModel重构"]
  }]</parameter>
</invoke>

<!-- 创建关系 -->
<invoke name="mcp__memory__create_relations">
  <parameter name="relations">[{
    "from": "Issue-1013",
    "to": "LYBT.Desktop",
    "relationType": "修改"
  }]</parameter>
</invoke>

<!-- 搜索节点 -->
<invoke name="mcp__memory__search_nodes">
  <parameter name="query">ViewModel重构</parameter>
</invoke>
```

#### 验证规则

- `relationType` 使用主动语态（如"修改"、"依赖"、"实现"）
- `entityType` 建议使用统一分类（Task, Module, Issue, Feature, Bug 等）
- 实体名称应唯一且有意义，避免泛化命名

---

### 1.6 sequential-thinking

**用途**: 结构化推理与步骤分解

#### 参数规范（必须使用 camelCase）

| 参数 | 类型 | 必需 | 约束 | 说明 |
|------|------|------|------|------|
| `thought` | string | ✅ | - | 当前思考步骤内容 |
| `nextThoughtNeeded` | boolean | ✅ | - | 是否需要下一步思考 |
| `thoughtNumber` | integer | ✅ | ≥1 | 当前步骤编号 |
| `totalThoughts` | integer | ✅ | ≥1 | 预估总步骤数（可动态调整） |
| `isRevision` | boolean | ❌ | - | 是否修订先前思考 |
| `revisesThought` | integer | ❌ | ≥1 | 修订的步骤编号（需 isRevision=true） |
| `branchFromThought` | integer | ❌ | ≥1 | 分支起点编号 |
| `branchId` | string | ❌ | - | 分支标识符 |
| `needsMoreThoughts` | boolean | ❌ | - | 是否需要更多步骤（动态扩展） |

#### 调用示例

```xml
<invoke name="mcp__sequential-thinking__sequentialthinking">
  <parameter name="thought">分析需求并拆解为3个Phase...</parameter>
  <parameter name="nextThoughtNeeded">true</parameter>
  <parameter name="thoughtNumber">1</parameter>
  <parameter name="totalThoughts">5</parameter>
</invoke>
```

#### 验证规则

- `thoughtNumber` 必须 ≤ `totalThoughts`（除非设置 `needsMoreThoughts=true` 动态扩展）
- 设置 `isRevision=true` 时必须提供 `revisesThought`
- `branchId` 用于标识并行推理分支，同一分支保持一致

---

### 1.7 time

**用途**: 时区转换与时间标准化

#### 核心工具

1. `get_current_time`：timezone (string) → 返回指定时区当前时间（默认 Asia/Shanghai）
2. `convert_time`：source_timezone (string), time (string, HH:MM), target_timezone (string)

#### 调用示例

```xml
<!-- 获取当前时间 -->
<invoke name="mcp__time__get_current_time">
  <parameter name="timezone">Asia/Shanghai</parameter>
</invoke>

<!-- 时区转换 -->
<invoke name="mcp__time__convert_time">
  <parameter name="source_timezone">America/New_York</parameter>
  <parameter name="time">14:30</parameter>
  <parameter name="target_timezone">Asia/Shanghai</parameter>
</invoke>
```

#### 验证规则

- `timezone` 使用 IANA 格式（如 "America/New_York", "Europe/London", "Asia/Tokyo"）
- `time` 必须为 24小时制 HH:MM 格式（如 "14:30", "09:00"）
- 常用时区：Asia/Shanghai (中国), America/New_York (美东), Europe/London (英国), Asia/Tokyo (日本)

---

### 1.8 playwright

**用途**: 浏览器自动化（仅在任务明确要求时使用）

**说明**: 详细参数规范按需补充。常用工具包括 `browser_navigate`、`browser_snapshot`、`browser_click`、`browser_type` 等。

---

## 2. 工具协调与工作流

### 2.1 工具选择决策（何时用哪个工具）

| 场景 | 优先工具 | 次选工具 | 原因 |
|------|---------|---------|------|
| **查找类/方法定义** | `serena.find_symbol` | `grep` | 语义级搜索更精确，支持类型过滤 |
| **查找符号引用** | `serena.find_referencing_symbols` | `grep` + 人工过滤 | 自动过滤非代码引用（注释/字符串） |
| **读取完整文件** | `filesystem.read_text_file` | `serena.read_file` | filesystem支持head/tail，更灵活 |
| **替换整个方法** | `serena.replace_symbol_body` | `filesystem.edit_file` | 保留缩进，语义安全 |
| **替换跨符号代码** | `serena.replace_regex` | `filesystem.edit_file` | 支持通配符，避免指定精确内容 |
| **批量读取文件** | `filesystem.read_multiple_files` | 循环调用read | 一次调用减少往返开销 |
| **查询库文档** | `context7.get-library-docs` | WebSearch | 获取权威文档片段，含代码示例 |
| **执行Shell命令** | `serena.execute_shell_command` | Bash工具 | 集成在代码操作流程中 |
| **Git操作** | `git.*` 工具 | Bash git命令 | 结构化参数，返回JSON |
| **记录临时思路** | `memory.create_entities` | 本地文件 | 支持关系图谱，便于后续检索 |

---

### 2.2 典型协调流程（串行依赖链）

#### 流程1: 需求理解 → 方案设计 → 代码实现 → 验证提交

```
[Context7: 查询相关文档/库]
        ↓
[sequential-thinking: 拆解步骤与依赖]
        ↓
[serena.find_symbol: 定位目标代码]
        ↓
[serena.find_referencing_symbols: 检查影响范围]
        ↓
[serena.replace_symbol_body: 修改代码]
        ↓
[git.diff: 查看差异]
        ↓
[git.commit: 提交变更]
```

#### 流程2: Bug修复工作流

```
[serena.search_for_pattern: 定位错误代码]
        ↓
[serena.get_symbols_overview: 理解文件结构]
        ↓
[serena.find_symbol(..., include_body=true): 读取方法实现]
        ↓
[context7.get-library-docs: 查询正确用法]
        ↓
[serena.replace_symbol_body: 修复代码]
        ↓
[serena.execute_shell_command: 运行测试]
        ↓
[git.commit: 提交修复]
```

---

### 2.3 并行执行模式（独立任务）

#### 场景1: 多文件信息收集

```xml
<!-- 并行读取3个文件（单次调用） -->
<invoke name="mcp__filesystem__read_multiple_files">
  <parameter name="paths">["file1.cs", "file2.cs", "file3.cs"]</parameter>
</invoke>
```

#### 场景2: 多个符号定位

```xml
<!-- 并行查找多个类（单个消息内多次调用） -->
<invoke name="mcp__serena__find_symbol">
  <parameter name="name_path">UserService</parameter>
  <parameter name="relative_path">src/Server/Modules</parameter>
</invoke>
<invoke name="mcp__serena__find_symbol">
  <parameter name="name_path">HerbService</parameter>
  <parameter name="relative_path">src/Server/Modules</parameter>
</invoke>
<invoke name="mcp__serena__find_symbol">
  <parameter name="name_path">PatientService</parameter>
  <parameter name="relative_path">src/Server/Modules</parameter>
</invoke>
```

#### 场景3: Git状态+差异+日志同时查询

```xml
<!-- 并行获取Git信息（单个消息内多次调用） -->
<invoke name="mcp__git__git_status" />
<invoke name="mcp__git__git_diff">
  <parameter name="staged">false</parameter>
</invoke>
<invoke name="mcp__git__git_log">
  <parameter name="maxCount">5</parameter>
</invoke>
```

---

## 3. 常见问题与错误处理

### 3.1 错误处理与回退策略

| 错误类型 | 检测方式 | 回退策略 | 示例 |
|---------|---------|---------|------|
| **参数类型错误** | 工具返回参数验证失败 | 修正参数类型后重试1次 | `thoughtNumber` 传了字符串 → 改为数字 |
| **文件路径不存在** | `read_text_file` 返回FileNotFound | 用 `search_files` 查找正确路径 | 路径拼写错误 → 搜索文件名 |
| **符号未找到** | `find_symbol` 返回空结果 | 启用 `substring_matching=true` 重试 | 类名记忆不准确 → 模糊搜索 |
| **多符号匹配** | `replace_regex` 报告多次匹配 | 增加上下文扩展正则表达式 | 通用方法名 → 添加类名限定 |
| **Git冲突** | `git_merge` 返回冲突状态 | 用 `git.diff` 查看冲突 → 人工决策 | 合并分支冲突 → 展示差异给用户 |
| **编译失败** | `execute_shell_command` 返回非0退出码 | 解析错误信息 → 定位失败文件 → 修正 | dotnet build 失败 → 读取错误日志 |
| **上下文不足** | 工具返回内容截断 | 增加 `max_answer_chars` 或拆分查询 | 文件过大 → 分段读取 |

---

### 3.2 容错原则

1. **一次重试**：参数错误修正后仅重试1次，避免无限循环
2. **上报阻塞**：重试失败后立即报告用户，附带原始错误信息
3. **保留现场**：错误发生前的所有工具调用记录保留，便于复盘
4. **降级方案**：优先工具失败时，回退到次选工具（如 serena → filesystem → grep）

---

## 附录

### A. 参数命名约定总结

| 工具 | 参数约定 | 原因 |
|------|---------|------|
| filesystem, git, context7, memory, time | **camelCase** | 遵循JSON标准 |
| serena | **snake_case** | 遵循Python LSP约定 |
| sequential-thinking | **camelCase** | 遵循工具Schema定义 |

### B. 常用IANA时区列表

- `Asia/Shanghai` - 中国标准时间 (UTC+8)
- `America/New_York` - 美国东部时间 (UTC-5/-4)
- `Europe/London` - 英国时间 (UTC+0/+1)
- `Asia/Tokyo` - 日本标准时间 (UTC+9)
- `America/Los_Angeles` - 美国西部时间 (UTC-8/-7)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
