# User层CLAUDE.md配置（全局生效）

> **路径**: `C:\Users\player\.claude\CLAUDE.md`
> **用途**: 跨项目通用的MCP工具使用规范和个人偏好设置

---

## 🌍 全局基础设置

```markdown
# 全局指令

- 始终使用简体中文响应
- 禁止使用emoji表情
- 执行任务之前先获取正确的当前时间
- 优先使用MCP工具进行操作（文件、数据库、GitHub、记忆等）
- 进行写操作、保存操作前需要日期的都必须正确获取当前时间
```

---

## 🛠️ MCP工具标准化调用规范

### 1. 深度推理工具

#### `sequential-thinking`
**适用场景**: 架构设计、问题诊断、方案评估、技术选型

**⚠️ 参数必须使用camelCase**:
```json
{
  "thought": "当前思考内容",
  "thoughtNumber": 1,
  "totalThoughts": 5,
  "nextThoughtNeeded": true
}
```

**常见错误**: `thought_number` ❌ → `thoughtNumber` ✅

---

### 2. Graphiti记忆管理

#### `add_memory` - 保存记忆
```json
{
  "name": "{模块}-{任务类型}-{描述}-{YYYY-MM-DD}",
  "episode_body": "详细内容或JSON字符串",
  "source": "json|text|message",
  "group_id": "项目ID"
}
```

#### `search_memory_facts` - 搜索事实
```json
{
  "query": "关键词",
  "max_facts": 10
}
```

#### `search_nodes` - 搜索节点
```json
{
  "query": "实体关键词",
  "max_nodes": 10
}
```

#### `get_episodes` - 获取片段
```json
{
  "max_episodes": 10
}
```

---

### 3. .NET代码分析

#### `netcontext-server semantic_search`
```json
{
  "query": "功能描述或类名/方法名",
  "topK": 5
}
```

#### `netcontext-server list_projects`
无需参数，列出所有.csproj项目

---

### 4. Serena代码操作

#### `find_symbol` - 查找符号
```json
{
  "name_path": "类名/方法名",
  "relative_path": "文件路径",
  "include_body": false,
  "depth": 0,
  "include_kinds": [5, 6]  // 5=Class, 6=Method
}
```

#### `search_for_pattern` - 模式搜索
```json
{
  "substring_pattern": "正则表达式",
  "relative_path": "",
  "paths_include_glob": "*.cs",
  "context_lines_before": 2,
  "context_lines_after": 2
}
```

#### `replace_regex` - 正则替换
```json
{
  "relative_path": "文件路径",
  "regex": "匹配模式",
  "repl": "替换内容（支持\\1, \\2）",
  "allow_multiple_occurrences": false
}
```

**注意**: 
- `.` 匹配所有字符（包括换行）
- 插入换行字符串: `\\\\n`（双反斜杠）

---

### 5. 文件系统操作

#### `read_text_file`
```json
{
  "path": "绝对路径",
  "head": 100,  // 可选，读前N行
  "tail": 50    // 可选，读后N行
}
```

#### `write_file`
```json
{
  "path": "绝对路径",
  "content": "内容"
}
```

#### `edit_file` - 基于行的精确编辑
```json
{
  "path": "绝对路径",
  "edits": [
    {
      "oldText": "精确匹配的旧文本",
      "newText": "新文本"
    }
  ],
  "dryRun": false  // true时仅预览diff
}
```

#### `search_files`
```json
{
  "path": "搜索起始路径",
  "pattern": "文件名模式",
  "excludePatterns": ["*.tmp"]
}
```

---

### 6. GitHub操作

#### `issue_write` - 创建/更新Issue
```json
{
  "method": "create|update",
  "owner": "用户名",
  "repo": "仓库名",
  "title": "标题",         // create时必需
  "issue_number": 123,    // update时必需
  "body": "内容",
  "state": "open|closed",
  "state_reason": "completed|not_planned|duplicate",
  "labels": ["bug", "P0"]
}
```

#### `search_issues`
```json
{
  "query": "搜索词",
  "sort": "created|updated",
  "order": "asc|desc",
  "perPage": 30
}
```

#### `create_pull_request`
```json
{
  "owner": "用户名",
  "repo": "仓库名",
  "title": "PR标题",
  "head": "源分支",
  "base": "目标分支",
  "body": "描述",
  "draft": false
}
```

---

### 7. 数据库操作

#### `execute_query` - SQL查询（Markdown表格）
```json
{
  "db_id": "连接ID",
  "query": "SQL语句"
}
```

#### `execute_query_json` - SQL查询（JSON）
```json
{
  "db_id": "连接ID",
  "query": "SQL语句"
}
```

#### `describe_table` - 表结构
```json
{
  "db_id": "连接ID",
  "table_name": "表名"
}
```

---

### 8. Microsoft文档

#### `microsoft_docs_search`
```json
{
  "query": "搜索关键词"
}
```

#### `microsoft_code_sample_search`
```json
{
  "query": "功能描述",
  "language": "csharp|java|python|..."
}
```

---

### 9. Context7文档

#### `resolve-library-id`
```json
{
  "libraryName": "库名"
}
```

#### `get-library-docs`
```json
{
  "context7CompatibleLibraryID": "/org/project",
  "topic": "主题",
  "page": 1
}
```

---

### 10. Web搜索

#### `tavily-search`
```json
{
  "query": "搜索词",
  "search_depth": "basic|advanced",
  "max_results": 10,
  "topic": "general|news"
}
```

---

## 🎯 工具使用优先级

1. **任务规划**: `TodoWrite`（≥3步骤或≥30分钟任务必用）
2. **深度思考**: `sequential-thinking`（复杂决策必用）
3. **记忆检索**: `graphiti-memory search_*`（任务开始前必查）
4. **代码定位**: `netcontext-server semantic_search` → `serena find_symbol`
5. **代码修改**: `serena replace_regex` or `filesystem edit_file`
6. **文档查询**: `microsoft_docs_search` → `context7 get-library-docs`
7. **实时搜索**: `tavily-search`
8. **记忆保存**: `graphiti-memory add_memory`（每个子任务完成后）

---

## 🚨 常见错误速查

| 工具 | 错误 | 正确 |
|------|------|------|
| sequential-thinking | `thought_number` | `thoughtNumber` |
| sequential-thinking | `next_thought_needed` | `nextThoughtNeeded` |
| add_memory | 传入对象到episode_body | 传入JSON字符串 |
| replace_regex | `\n` 表示换行 | `\\\\n` 表示换行字符串 |
| edit_file | 模糊匹配oldText | 必须精确匹配 |

---

## 📅 日期时间获取

**写操作前必须执行**:
```bash
# Windows PowerShell
powershell -Command "Get-Date -Format 'yyyy-MM-dd'"

# Linux/Mac
date +%Y-%m-%d
```

---

## ✅ 操作检查清单

**任务开始前**:
- [ ] 使用 `sequential-thinking` 深度分析
- [ ] 使用 `graphiti-memory search_*` 检索历史经验
- [ ] 使用 `TodoWrite` 创建任务清单

**代码修改前**:
- [ ] 使用 `netcontext-server semantic_search` 定位代码
- [ ] 使用 `serena find_symbol` 理解结构
- [ ] 使用 `dryRun: true` 预览变更

**任务完成后**:
- [ ] 使用 `graphiti-memory add_memory` 保存经验
- [ ] 使用 `github issue_write` 更新/关闭Issue
- [ ] 验证4条关闭标准（验收+验证+文档+记忆）

---

**配置版本**: v2.0
**最后更新**: 2025-01-23
