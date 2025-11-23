# MCP工具使用指南（User层配置）

> **用途**: 此文档内容应复制到 `C:\Users\player\.claude\CLAUDE.md` 中
> **目标**: 提高MCP工具使用效率，减少试错，规范化调用流程

---

## 🎯 核心原则

1. **MCP工具优先**: 优先使用MCP工具进行文件、数据库、GitHub、记忆等操作
2. **参数规范化**: 所有工具调用必须遵循参数规范，避免camelCase/snake_case混淆
3. **效率优先**: 组合使用工具，减少重复调用
4. **错误预防**: 常见参数错误已在此文档中标注，严格遵守

---

## 📚 MCP工具完整参数手册

### 1️⃣ 深度推理工具

#### `mcp__sequential-thinking__sequentialthinking`
**用途**: 结构化深度推理（复杂任务核心工具）

**适用场景**: 架构设计、问题诊断、方案评估、技术选型、需求分析

**⚠️ 参数格式（必须使用camelCase）**:
```json
{
  "thought": "当前思考步骤的内容（字符串）",
  "thoughtNumber": 1,          // 当前思考步骤编号（整数）
  "totalThoughts": 5,          // 预计总思考步骤数（整数）
  "nextThoughtNeeded": true,   // 是否需要下一步思考（布尔值）
  
  // 可选参数
  "isRevision": false,         // 是否是对之前思考的修订
  "revisesThought": null,      // 修订哪个步骤编号（整数）
  "branchFromThought": null,   // 分支起点步骤编号（整数）
  "branchId": null,            // 分支标识符（字符串）
  "needsMoreThoughts": false   // 是否需要增加总步骤数
}
```

**❌ 常见错误**:
- 使用 `thought_number` 而非 `thoughtNumber` → 导致 "Invalid thoughtNumber" 错误
- 使用 `next_thought_needed` 而非 `nextThoughtNeeded`
- 传入字符串而非整数到 `thoughtNumber`/`totalThoughts`

**✅ 正确示例**:
```json
{
  "thought": "分析当前架构问题：MedicalCase和Consultation的职责边界不清晰",
  "thoughtNumber": 1,
  "totalThoughts": 5,
  "nextThoughtNeeded": true
}
```

---

### 2️⃣ Graphiti记忆工具

#### `mcp__graphiti-memory__add_memory`
**用途**: 保存结构化记忆（代码决策、任务总结、经验教训）

**参数**:
```json
{
  "name": "{模块名}-{任务类型}-{简要描述}-{日期}",  // 必需
  "episode_body": "详细内容（JSON字符串或纯文本）",     // 必需
  "source": "json|text|message",                      // 可选，默认text
  "source_description": "来源描述",                    // 可选
  "group_id": "LYBTZYZS"                             // 可选，默认项目ID
}
```

**命名规范**:
- 格式: `{模块名}-{任务类型}-{简要描述}-{YYYY-MM-DD}`
- 示例: `MedicalCase-Bug修复-NeedsPrescription空值错误-2025-01-23`
- 示例: `Consultation-架构优化-术语规范统一-2025-01-23`

**JSON格式示例**:
```json
{
  "name": "MedicalCase-重构-Repository模式统一-2025-01-23",
  "episode_body": "{\"task\": \"统一Repository模式\", \"changes\": [\"删除冗余接口\", \"简化依赖注入\"], \"impact\": \"减少60%代码量\"}",
  "source": "json",
  "source_description": "Repository重构总结"
}
```

#### `mcp__graphiti-memory__search_memory_facts`
**用途**: 搜索记忆中的事实关系

**参数**:
```json
{
  "query": "搜索关键词",           // 必需
  "group_ids": ["LYBTZYZS"],      // 可选，过滤分组
  "max_facts": 10,                // 可选，默认10
  "center_node_uuid": "uuid"      // 可选，以某节点为中心
}
```

**搜索策略**:
- 模块相关: `"MedicalCase Repository 模式"`
- 技术决策: `"AutoMapper 简化 决策"`
- 问题诊断: `"NeedsPrescription null 错误"`

#### `mcp__graphiti-memory__search_nodes`
**用途**: 搜索记忆中的实体节点

**参数**:
```json
{
  "query": "实体关键词",          // 必需
  "group_ids": ["LYBTZYZS"],     // 可选
  "max_nodes": 10,               // 可选，默认10
  "entity_types": ["Component"]  // 可选，过滤实体类型
}
```

#### `mcp__graphiti-memory__get_episodes`
**用途**: 获取最近的记忆片段

**参数**:
```json
{
  "group_ids": ["LYBTZYZS"],  // 可选
  "max_episodes": 10          // 可选，默认10
}
```

---

### 3️⃣ .NET代码分析工具

#### `mcp__netcontext-server__semantic_search`
**用途**: .NET代码库语义搜索（快速定位代码）

**参数**:
```json
{
  "query": "功能描述或类名/方法名",  // 必需
  "topK": 5                         // 可选，返回结果数，默认5
}
```

**搜索策略**:
- 功能搜索: `"获取患者未完成医案"`
- 类名搜索: `"MedicalCaseRepository"`
- 方法搜索: `"GetUnfinishedCaseAsync"`
- 组合搜索: `"MedicalCase Repository 未完成"`

#### `mcp__netcontext-server__list_projects`
**用途**: 列出解决方案中所有.csproj项目

#### `mcp__netcontext-server__list_source_files`
**用途**: 列出项目中所有源代码文件

**参数**:
```json
{
  "projectDir": "绝对路径到项目目录"  // 必需
}
```

---

### 4️⃣ Serena代码操作工具

#### `mcp__serena__find_symbol`
**用途**: 查找代码符号（类、方法、属性）

**参数**:
```json
{
  "name_path": "类名/方法名",        // 必需，支持路径匹配
  "relative_path": "相对文件路径",   // 可选，限定搜索范围
  "substring_matching": false,       // 可选，是否子串匹配
  "include_body": false,             // 可选，是否包含代码体
  "depth": 0,                        // 可选，子符号深度
  "include_kinds": [5, 6],           // 可选，符号类型过滤
  "exclude_kinds": []                // 可选，排除符号类型
}
```

**符号类型（LSP Symbol Kind）**:
- 5 = Class
- 6 = Method
- 7 = Property
- 8 = Field
- 12 = Function
- 13 = Variable

**匹配规则**:
- `"MedicalCaseRepository"` → 匹配所有名为MedicalCaseRepository的符号
- `"/MedicalCaseRepository"` → 仅匹配顶层MedicalCaseRepository
- `"MedicalCaseRepository/GetByIdAsync"` → 匹配该类的该方法

#### `mcp__serena__search_for_pattern`
**用途**: 正则表达式搜索代码模式

**参数**:
```json
{
  "substring_pattern": "正则表达式",                // 必需
  "relative_path": "限定搜索路径",                  // 可选
  "restrict_search_to_code_files": false,          // 可选，仅搜索代码文件
  "paths_include_glob": "*.cs",                    // 可选，包含文件通配符
  "paths_exclude_glob": "*test*",                  // 可选，排除文件通配符
  "context_lines_before": 0,                       // 可选，前文行数
  "context_lines_after": 0                         // 可选，后文行数
}
```

#### `mcp__serena__replace_regex`
**用途**: 正则表达式替换代码（精确替换工具）

**参数**:
```json
{
  "relative_path": "文件相对路径",                  // 必需
  "regex": "正则表达式（Python风格）",              // 必需
  "repl": "替换内容（支持\\1, \\2反向引用）",        // 必需
  "allow_multiple_occurrences": false              // 可选，允许多处匹配
}
```

**⚠️ 重要注意事项**:
- 正则表达式支持 `.`（点匹配所有字符包括换行）
- 转义字符: `\\` → `\\\\`, `\n` → `\\n`
- 插入带换行的字符串时使用双反斜杠: `\\\\n`

---

### 5️⃣ 文件系统工具

#### `mcp__filesystem__read_text_file`
**用途**: 读取文本文件

**参数**:
```json
{
  "path": "文件绝对路径",  // 必需
  "head": 100,            // 可选，仅读前N行
  "tail": 50              // 可选，仅读后N行
}
```

#### `mcp__filesystem__write_file`
**用途**: 创建或覆盖文件

**参数**:
```json
{
  "path": "文件绝对路径",  // 必需
  "content": "文件内容"   // 必需
}
```

#### `mcp__filesystem__edit_file`
**用途**: 基于行的精确编辑

**参数**:
```json
{
  "path": "文件绝对路径",  // 必需
  "edits": [               // 必需
    {
      "oldText": "要替换的文本（必须精确匹配）",
      "newText": "新文本"
    }
  ],
  "dryRun": false  // 可选，true时仅预览diff
}
```

#### `mcp__filesystem__search_files`
**用途**: 递归搜索文件

**参数**:
```json
{
  "path": "搜索起始路径",           // 必需
  "pattern": "文件名模式",          // 必需
  "excludePatterns": ["*.tmp"]     // 可选，排除模式
}
```

---

### 6️⃣ GitHub工具

#### `mcp__github__issue_write`
**用途**: 创建或更新Issue

**参数**:
```json
{
  "method": "create|update",  // 必需
  "owner": "shouqitao",       // 必需
  "repo": "LYBTZYZS",         // 必需
  "issue_number": 123,        // update时必需
  "title": "Issue标题",        // create时必需
  "body": "Issue内容",         // 可选
  "state": "open|closed",     // update时可选
  "state_reason": "completed|not_planned|duplicate",  // 关闭时可选
  "labels": ["bug", "P0"],    // 可选
  "assignees": ["shouqitao"], // 可选
  "type": "Feature"           // 可选，组织issue类型
}
```

**关闭Issue标准（自动执行，无需询问用户）**:
满足以下4个条件立即关闭:
1. ✅ 验收完成
2. ✅ 功能验证通过
3. ✅ 文档已同步
4. ✅ 记忆已保存

#### `mcp__github__search_issues`
**用途**: 搜索Issues

**参数**:
```json
{
  "query": "搜索关键词",     // 必需
  "owner": "shouqitao",      // 可选，限定仓库
  "repo": "LYBTZYZS",        // 可选，限定仓库
  "sort": "created|updated", // 可选
  "order": "asc|desc",       // 可选
  "page": 1,                 // 可选
  "perPage": 30              // 可选，1-100
}
```

#### `mcp__github__create_pull_request`
**用途**: 创建PR

**参数**:
```json
{
  "owner": "shouqitao",         // 必需
  "repo": "LYBTZYZS",           // 必需
  "title": "PR标题",            // 必需
  "head": "feature-branch",     // 必需，源分支
  "base": "master",             // 必需，目标分支
  "body": "PR描述",             // 可选
  "draft": false,               // 可选
  "maintainer_can_modify": true // 可选
}
```

---

### 7️⃣ 数据库工具

#### `mcp__database-mcp__execute_query`
**用途**: 执行SQL查询（返回Markdown表格）

**参数**:
```json
{
  "db_id": "数据库连接ID",  // 必需
  "query": "SQL语句"       // 必需
}
```

#### `mcp__database-mcp__execute_query_json`
**用途**: 执行SQL查询（返回JSON）

**参数**:
```json
{
  "db_id": "数据库连接ID",  // 必需
  "query": "SQL语句"       // 必需
}
```

#### `mcp__database-mcp__describe_table`
**用途**: 获取表结构详情

**参数**:
```json
{
  "db_id": "数据库连接ID",  // 必需
  "table_name": "表名"     // 必需
}
```

---

### 8️⃣ Microsoft文档工具

#### `mcp__microsoft_docs_mcp__microsoft_docs_search`
**用途**: 搜索Microsoft官方文档

**参数**:
```json
{
  "query": "搜索关键词"  // 必需
}
```

**搜索策略**:
- .NET API: `"Entity Framework Core DbContext SaveChangesAsync"`
- WPF控件: `"WPF DataGrid ItemsSource binding"`
- 最佳实践: `"ASP.NET Core dependency injection best practices"`

#### `mcp__microsoft_docs_mcp__microsoft_code_sample_search`
**用途**: 搜索Microsoft代码示例

**参数**:
```json
{
  "query": "功能描述",  // 必需
  "language": "csharp" // 可选，过滤语言
}
```

**支持语言**: `csharp`, `javascript`, `typescript`, `python`, `powershell`, `java`

---

### 9️⃣ Context7文档工具

#### `mcp__context7__resolve-library-id`
**用途**: 解析库名到Context7 ID

**参数**:
```json
{
  "libraryName": "库名"  // 必需，如 "Entity Framework Core"
}
```

#### `mcp__context7__get-library-docs`
**用途**: 获取库文档

**参数**:
```json
{
  "context7CompatibleLibraryID": "/org/project",  // 必需
  "topic": "主题关键词",                           // 可选
  "page": 1                                       // 可选，默认1
}
```

---

### 🔟 Web搜索工具

#### `mcp__tavily-mcp__tavily-search`
**用途**: 实时Web搜索

**参数**:
```json
{
  "query": "搜索关键词",                        // 必需
  "search_depth": "basic|advanced",            // 可选，默认basic
  "max_results": 10,                           // 可选，5-20
  "include_raw_content": false,                // 可选
  "include_images": false,                     // 可选
  "topic": "general|news"                      // 可选，默认general
}
```

**搜索策略**:
- 技术问题: `"WPF MVVM Prism IRegionManager best practices"`
- 错误解决: `"Entity Framework Core NullReferenceException SaveChangesAsync"`
- 开源示例: `"GitHub WPF DataGrid custom column example"`

---

## 🎯 工具组合使用模式

### 模式1：完整任务执行流程
```
1. sequential-thinking (深度分析任务)
2. graphiti-memory search_* (检索历史经验)
3. netcontext-server semantic_search (定位代码)
4. serena find_symbol (理解代码结构)
5. serena replace_regex (精确修改)
6. database-mcp execute_query (验证数据)
7. github issue_write (更新Issue)
8. graphiti-memory add_memory (保存经验)
```

### 模式2：快速Bug修复
```
1. graphiti-memory search_memory_facts (查找类似问题)
2. netcontext-server semantic_search (定位错误代码)
3. tavily-search (搜索解决方案)
4. serena replace_regex (应用修复)
5. github issue_write (关闭Issue)
```

### 模式3：架构设计
```
1. sequential-thinking (架构分析)
2. graphiti-memory search_nodes (检索组件关系)
3. microsoft_docs_search (查阅最佳实践)
4. context7 get-library-docs (深入库文档)
5. graphiti-memory add_memory (保存设计决策)
```

---

## ⚠️ 常见错误速查表

| 错误症状 | 原因 | 解决方案 |
|---------|------|----------|
| "Invalid thoughtNumber" | 使用了snake_case | 改为camelCase: `thoughtNumber` |
| "episode_body must be string" | 传入了对象 | 使用JSON.stringify或纯文本 |
| "File not found" | 相对路径错误 | 使用完整相对路径，从项目根开始 |
| "Regex no match" | 正则表达式不匹配 | 先用search_for_pattern验证模式 |
| "Multiple matches" | 正则匹配多处 | 增加上下文或设置allow_multiple_occurrences |
| "Invalid JSON" | episode_body格式错误 | 检查转义字符，使用\\而非\ |

---

## 📝 日期时间获取规范

**要求**: 所有写操作、保存操作前必须获取当前时间

**方法**:
```bash
# 使用Bash获取当前日期
Bash: "date"

# 使用PowerShell（Windows）
Bash: "powershell -Command Get-Date -Format 'yyyy-MM-dd'"
```

**记忆命名中的日期格式**: `YYYY-MM-DD`

---

## 🎓 最佳实践清单

- ✅ 任务开始前: 先查询Graphiti记忆，避免重复错误
- ✅ 复杂操作前: 使用sequential-thinking深度分析
- ✅ 代码修改前: 使用serena find_symbol理解结构
- ✅ 每个子任务完成后: 立即保存记忆到Graphiti
- ✅ Issue关闭前: 确认4条标准（验收+验证+文档+记忆）
- ✅ 工具调用失败后: 检查参数格式（camelCase vs snake_case）
- ✅ 搜索无结果时: 调整关键词或扩大搜索范围
- ✅ 不确定时: 先用dryRun预览，再执行实际操作

---

**文档版本**: v2.0
**最后更新**: 2025-01-23
**维护者**: Claude (Serena Agent)
