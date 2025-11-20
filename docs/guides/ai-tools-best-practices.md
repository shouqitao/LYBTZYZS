# AI工具使用最佳实践指南

> **适用范围**: LYBTZYZS项目中使用Claude Code AI助手进行开发
> **核心工具**: GitHub MCP, Graphiti Memory, Serena, Bash, Sequential-Thinking
> **最后更新**: 2025-11-20
> **关联Issue**: [#2173](https://github.com/shouqitao/LYBTZYZS/issues/2173)

---

## 目录

1. [GitHub MCP工具使用规范](#github-mcp工具使用规范)
2. [Graphiti记忆管理最佳实践](#graphiti记忆管理最佳实践)
3. [Serena vs Graphiti选择指南](#serena-vs-graphiti选择指南)
4. [Bash命令使用原则](#bash命令使用原则)
5. [Windows环境特殊考虑](#windows环境特殊考虑)
6. [常见错误与修复](#常见错误与修复)
7. [工具选择决策树](#工具选择决策树)

---

## GitHub MCP工具使用规范

### 优先级原则

**优先使用GitHub MCP > Bash gh命令**

```bash
# ❌ 不推荐：使用bash gh命令
gh issue create --title "xxx" --body "xxx"
gh issue close 123

# ✅ 推荐：使用GitHub MCP工具
mcp__github__issue_write(method="create", ...)
mcp__github__issue_write(method="update", state="closed", ...)
```

### 工具选择矩阵

| 操作类型 | 推荐工具 | 备选工具 | 禁止工具 |
|---------|---------|---------|---------|
| 创建Issue | `mcp__github__issue_write` | - | `gh issue create` |
| 更新Issue | `mcp__github__issue_write` | - | `gh issue edit` |
| 关闭Issue | `mcp__github__issue_write` | - | `gh issue close` |
| 搜索Issue | `mcp__github__search_issues` | - | `gh issue list \| grep` |
| 创建PR | `mcp__github__create_pull_request` | - | `gh pr create` |
| 查看PR | `mcp__github__pull_request_read` | `gh pr view` | - |
| 搜索代码 | `mcp__github__search_code` | `Grep` | `gh api search/code` |

### 核心工具详解

#### 1. issue_write - Issue管理

**创建Issue**:
```typescript
mcp__github__issue_write({
  method: "create",
  owner: "shouqitao",
  repo: "LYBTZYZS",
  title: "Issue标题",
  body: "详细描述\n\n## 子任务\n- [ ] 任务1",
  labels: ["enhancement", "priority:high"],
  assignees: ["shouqitao"]
})
```

**更新Issue**:
```typescript
mcp__github__issue_write({
  method: "update",
  owner: "shouqitao",
  repo: "LYBTZYZS",
  issue_number: 2172,
  state: "closed",
  body: "## 完成总结\n..."
})
```

**关键参数**:
- `method`: "create" | "update"（必填）
- `state`: "open" | "closed"（更新时使用）
- `state_reason`: "completed" | "not_planned" | "duplicate"（关闭时建议提供）

#### 2. search_issues - Issue搜索

```typescript
mcp__github__search_issues({
  query: "repo:shouqitao/LYBTZYZS is:open label:bug",
  owner: "shouqitao",  // 可选，限制仓库
  repo: "LYBTZYZS",    // 可选，限制仓库
  sort: "updated",     // "created" | "updated" | "comments"
  order: "desc"        // "asc" | "desc"
})
```

**搜索语法**:
- `is:open` / `is:closed` - 状态过滤
- `label:bug` - 标签过滤
- `assignee:shouqitao` - 指派人过滤
- `milestone:"v1.0"` - 里程碑过滤
- `created:>2025-11-01` - 时间过滤

#### 3. pull_request_read - PR查看

```typescript
mcp__github__pull_request_read({
  method: "get",           // 获取PR详情
  owner: "shouqitao",
  repo: "LYBTZYZS",
  pullNumber: 123
})

mcp__github__pull_request_read({
  method: "get_diff",      // 获取PR diff
  owner: "shouqitao",
  repo: "LYBTZYZS",
  pullNumber: 123
})
```

**method选项**:
- `get` - 获取PR详情
- `get_diff` - 获取代码差异
- `get_files` - 获取变更文件列表
- `get_reviews` - 获取审查记录
- `get_comments` - 获取评论

### 常见场景示例

#### 场景1: 创建Epic和子Issues

```typescript
// 1. 创建Epic
const epic = mcp__github__issue_write({
  method: "create",
  title: "Epic: 质量改进",
  body: "## 目标\n...\n\n## 子任务\n- #2170\n- #2171",
  labels: ["epic", "quality"]
})

// 2. 创建子Issue 1
mcp__github__issue_write({
  method: "create",
  title: "XAML绑定模式检查",
  body: "关联Epic: #2169\n\n## 任务描述\n...",
  labels: ["subtask"]
})

// 3. 创建子Issue 2
mcp__github__issue_write({
  method: "create",
  title: "XAML标准模板提取",
  body: "关联Epic: #2169\n\n## 任务描述\n...",
  labels: ["subtask"]
})
```

#### 场景2: 批量关闭已完成Issues

```typescript
// 1. 搜索待关闭的Issues
const openIssues = mcp__github__search_issues({
  query: "repo:shouqitao/LYBTZYZS is:open label:completed"
})

// 2. 逐个关闭（串行执行）
for (const issue of openIssues) {
  mcp__github__issue_write({
    method: "update",
    issue_number: issue.number,
    state: "closed",
    state_reason: "completed",
    body: `## 完成总结\n${issue.body}\n\n✅ 已完成`
  })
}
```

#### 场景3: PR审查流程

```typescript
// 1. 获取PR详情
const pr = mcp__github__pull_request_read({
  method: "get",
  pullNumber: 123
})

// 2. 获取变更文件
const files = mcp__github__pull_request_read({
  method: "get_files",
  pullNumber: 123
})

// 3. 查看diff
const diff = mcp__github__pull_request_read({
  method: "get_diff",
  pullNumber: 123
})

// 4. 提交审查意见
mcp__github__pull_request_review_write({
  method: "create",
  pullNumber: 123,
  event: "COMMENT",
  body: "审查意见..."
})
```

### 错误处理

#### 常见错误1: Issue不存在

```typescript
// ❌ 错误
mcp__github__issue_write({
  method: "update",
  issue_number: 9999,  // 不存在的Issue
  state: "closed"
})
// 错误: Issue #9999 not found

// ✅ 修复：先搜索验证
const issues = mcp__github__search_issues({
  query: `repo:shouqitao/LYBTZYZS is:open number:9999`
})
if (issues.length > 0) {
  // 执行更新
}
```

#### 常见错误2: 权限不足

```typescript
// ❌ 错误
mcp__github__issue_write({
  method: "create",
  owner: "other-user",  // 无权限的仓库
  repo: "other-repo"
})
// 错误: Resource not accessible by integration

// ✅ 修复：使用get_me确认权限
const user = mcp__github__get_me()
console.log(`当前用户: ${user.login}`)
```

---

## Graphiti记忆管理最佳实践

### 核心原则

1. **episode_body必须是字符串**（最常见错误）
2. **及时记录，避免遗忘**（每完成子任务立即保存）
3. **结构化命名**（`{模块名}-{任务类型}-{简要描述}-{日期}`）
4. **搜索优先**（执行任务前先检索相关记忆）

### 关键发现：episode_body参数类型

**错误用法** ❌:
```typescript
mcp__graphiti-memory__add_memory({
  name: "任务完成记录",
  episode_body: {  // ❌ 错误：不能是JSON对象
    "任务类型": "Bug修复",
    "关联Issue": "#123"
  }
})
// 错误: Input should be a valid string
```

**正确用法** ✅:
```typescript
// 方式1: 使用Markdown格式字符串
mcp__graphiti-memory__add_memory({
  name: "任务完成记录",
  episode_body: `## Bug修复完成

**任务类型**: Bug修复
**关联Issue**: #123
**完成日期**: 2025-11-20

### 问题描述
...

### 修复方案
...`
})

// 方式2: 使用换行符分隔的纯文本
mcp__graphiti-memory__add_memory({
  name: "任务完成记录",
  episode_body: "任务类型: Bug修复\n关联Issue: #123\n完成日期: 2025-11-20\n\n问题描述:\n...\n\n修复方案:\n..."
})
```

### 记忆命名规范

**格式**: `{模块名}-{任务类型}-{简要描述}-{日期}`

**示例**:
```
✅ FormulaDetailView-XAML绑定检查-完成-2025-11-20
✅ git-index-lock-根因调查完成-Issue2172-2025-11-20
✅ Users模块-架构重构-Phase1完成-2025-11-15

❌ 任务完成  // 太泛化
❌ bug修复   // 缺少上下文
❌ 2025-11-20 // 缺少描述
```

### 记忆内容模板

#### 模板1: 子任务完成记忆

```markdown
## {任务名称}完成

**任务类型**: {Bug修复/功能开发/重构/文档}
**关联Issue**: #{Issue编号}
**关联Epic**: #{Epic编号}
**执行日期**: YYYY-MM-DD

### 任务描述
{简要描述任务目标}

### 执行过程
1. {步骤1}
2. {步骤2}
3. {步骤3}

### 关键发现
- {发现1}
- {发现2}

### 交付物
- {文件1}: {描述}
- {文件2}: {描述}

### 影响范围
- {影响的模块/文件}
```

#### 模板2: 根因分析记忆

```markdown
## {问题名称}根因分析完成

**问题类型**: {Bug/性能/架构}
**关联Issue**: #{Issue编号}

### 问题症状
{问题表现}

### 根本原因
{5 Why分析结果}

### 修复方案
**立即行动**: {已执行的修复}
**短期修复**: {1-3天计划}
**中期改进**: {1-2周计划}
**长期改进**: {1-3月计划}

### 关键洞察
- {洞察1}
- {洞察2}

### 预防措施
- {措施1}
- {措施2}
```

### 三阶段记忆管理

#### 阶段1: RETRIEVE（任务启动前）

```typescript
// 1. 搜索相关facts
const facts = mcp__graphiti-memory__search_memory_facts({
  query: "XAML 绑定 TwoWay OneWay 只读属性",
  max_facts: 30
})

// 2. 搜索相关nodes
const nodes = mcp__graphiti-memory__search_nodes({
  query: "FormulaDetailView XAML绑定",
  max_nodes: 15
})

// 3. 获取最近episodes
const episodes = mcp__graphiti-memory__get_episodes({
  max_episodes: 20
})
```

#### 阶段2: RECORD（执行过程中）

```typescript
// 每完成一个子任务立即记录
mcp__graphiti-memory__add_memory({
  name: "FormulaDetailView-XAML绑定检查-完成-2025-11-20",
  episode_body: `## FormulaDetailView XAML绑定检查完成

**子任务**: 检查FormulaDetailView.xaml绑定模式
**发现问题**: 3个只读属性绑定缺少Mode=OneWay
**修复位置**: Lines 182, 194, 215

### 修复详情
1. Formula.CreatedAt绑定 (Line 182)
2. Formula.UpdatedAt绑定 (Line 194)
3. HerbCount绑定 (Line 215)

### 经验教训
即使TextBlock默认OneWay，也应明确指定绑定模式以保持一致性`
})
```

#### 阶段3: ARCHIVE（任务结束后）

```typescript
// 保存完整任务记忆
mcp__graphiti-memory__add_memory({
  name: "XAML绑定模式系统性检查完成-Issue2170-2025-11-20",
  episode_body: `## XAML绑定模式系统性检查完成

**Epic**: #2169 质量改进Epic
**Issue**: #2170 XAML绑定模式检查
**执行日期**: 2025-11-20

### 执行摘要
检查了3个DetailView文件，发现9个绑定问题，修复率100%

### 详细结果
1. FormulaDetailView: 3个问题修复
2. MedicalCaseDetailView: 3个问题修复
3. ConsultationFormView: 3个问题修复（含1个高风险）

### 高风险发现
ConsultationFormView Line 181: RadioButton绑定只读属性PrescriptionDisabled
- RadioButton默认TwoWay绑定
- 只读计算属性无setter
- 用户点击会导致运行时绑定错误

### 交付物
- 报告: docs/archive/reports/XAML绑定模式系统性检查报告-2025-11-20.md
- 修复: 3个XAML文件，9处绑定模式修正

### 最佳实践
所有只读属性绑定必须明确指定Mode=OneWay，包括：
- Server生成属性（CreatedAt, UpdatedAt）
- 计算属性（HerbCount, Age）
- 表达式属性（=> !IsEditMode）

### 工具使用经验
- Read工具：快速定位XAML文件和ViewModel
- Edit工具：精确修改绑定声明
- Grep工具：搜索绑定模式关键字`
})
```

### 搜索技巧

#### 技巧1: 多关键词组合

```typescript
// ❌ 泛化查询
search_memory_facts({ query: "bug" })  // 结果太多

// ✅ 精确查询
search_memory_facts({
  query: "FormulaDetailView XAML 绑定 TwoWay 只读属性"
})
```

#### 技巧2: 按时间过滤

```typescript
// 获取最近的episodes
get_episodes({ max_episodes: 10 })

// 然后在结果中按created_at排序筛选
```

#### 技巧3: 分类检索

```typescript
// 技术问题
search_memory_facts({ query: "Bug NullReferenceException EF Core" })

// 架构决策
search_memory_facts({ query: "架构 重构 MVVM Repository" })

// 最佳实践
search_memory_facts({ query: "最佳实践 测试 命名规范" })
```

---

## Serena vs Graphiti选择指南

### 核心区别

| 特性 | Serena | Graphiti |
|-----|--------|----------|
| **用途** | 代码库操作 | 项目知识管理 |
| **范围** | 单个代码仓库 | 跨任务/跨会话 |
| **生命周期** | 当前会话 | 永久保存 |
| **检索方式** | 符号/文件路径 | 自然语言 |
| **写入操作** | 直接修改代码 | 记录知识/经验 |

### 使用场景

#### 使用Serena的场景

**1. 代码读取**:
```typescript
// 读取文件
mcp__serena__read_file({
  relative_path: "src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs"
})

// 获取符号概览
mcp__serena__get_symbols_overview({
  relative_path: "FormulaDetailViewModel.cs"
})

// 查找符号
mcp__serena__find_symbol({
  name_path: "FormulaDetailViewModel/HerbCount",
  include_body: true
})
```

**2. 代码编辑**:
```typescript
// 替换符号体
mcp__serena__replace_symbol_body({
  name_path: "FormulaDetailViewModel/SaveCommand",
  relative_path: "FormulaDetailViewModel.cs",
  body: "新的方法实现..."
})

// 正则替换
mcp__serena__replace_regex({
  relative_path: "FormulaDetailView.xaml",
  regex: 'Text="{Binding CreatedAt}"',
  repl: 'Text="{Binding CreatedAt, Mode=OneWay}"'
})
```

**3. 代码搜索**:
```typescript
// 搜索模式
mcp__serena__search_for_pattern({
  substring_pattern: "Mode=TwoWay",
  relative_path: "src/Client/Desktop/Modules",
  restrict_search_to_code_files: false
})

// 查找引用
mcp__serena__find_referencing_symbols({
  name_path: "IFormulaRepository/GetByIdAsync",
  relative_path: "IFormulaRepository.cs"
})
```

#### 使用Graphiti的场景

**1. 任务经验记录**:
```typescript
mcp__graphiti-memory__add_memory({
  name: "Formula模块-性能优化-完成-2025-11-20",
  episode_body: "优化了GetByIdAsync方法，减少N+1查询..."
})
```

**2. 架构决策记录**:
```typescript
mcp__graphiti-memory__add_memory({
  name: "架构决策-废弃UltraThink-2025-11-15",
  episode_body: "决定废弃QueryService/BusinessService双层架构..."
})
```

**3. 问题根因记录**:
```typescript
mcp__graphiti-memory__add_memory({
  name: "git-index-lock-根因分析-2025-11-20",
  episode_body: "根本原因：并发测试执行导致超时..."
})
```

**4. 最佳实践记录**:
```typescript
mcp__graphiti-memory__add_memory({
  name: "XAML绑定-最佳实践-2025-11-20",
  episode_body: "所有只读属性必须明确指定Mode=OneWay..."
})
```

### 组合使用示例

#### 场景: 修复Bug并记录经验

```typescript
// 1. 用Serena查找问题代码
const symbol = mcp__serena__find_symbol({
  name_path: "FormulaDetailViewModel/LoadDataAsync",
  include_body: true
})

// 2. 用Serena修复代码
mcp__serena__replace_symbol_body({
  name_path: "FormulaDetailViewModel/LoadDataAsync",
  body: "修复后的代码..."
})

// 3. 用Graphiti记录修复经验
mcp__graphiti-memory__add_memory({
  name: "FormulaDetailViewModel-LoadDataAsync-Bug修复-2025-11-20",
  episode_body: `## Bug修复: LoadDataAsync NullReferenceException

**根本原因**: 未检查HerbItems为null的情况

**修复方案**: 添加null检查
\`\`\`csharp
if (HerbItems == null) HerbItems = new ObservableCollection<HerbItemViewModel>();
\`\`\`

**预防措施**:
- 所有集合属性初始化时赋值空集合
- 添加单元测试验证null安全`
})
```

---

## Bash命令使用原则

### 优先级规则

**专用工具 > Bash命令**

```bash
# ❌ 不推荐：用Bash读取文件
cat src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml

# ✅ 推荐：使用Read工具
Read({ file_path: "D:/source/repos/LYBTZYZS/src/Client/.../FormulaDetailView.xaml" })
```

### 合适的Bash使用场景

#### 1. 系统操作

```bash
# ✅ 编译构建
dotnet build LYBT.Desktop.sln --no-restore

# ✅ 运行测试
dotnet test LYBT.Desktop.Users.Tests.csproj

# ✅ Git操作
git status
git add .
git commit -m "message"
```

#### 2. 环境检查

```bash
# ✅ 检查进程
ps aux | grep dotnet

# ✅ 检查文件存在性
ls -lh .git/index.lock

# ✅ 清理临时文件
rm -f .git/index.lock
```

#### 3. 批量操作

```bash
# ✅ 批量文件操作
for file in src/**/*.xaml; do
  echo "检查: $file"
done

# ✅ 串行测试执行
for proj in tests/**/*.csproj; do
  dotnet test "$proj" || true
done
```

### 禁止的Bash用法

#### 1. 文件读取

```bash
# ❌ 禁止
cat file.cs
head -n 100 file.cs
tail -n 50 file.cs

# ✅ 使用Read工具
Read({ file_path: "file.cs", limit: 100 })
```

#### 2. 文件编辑

```bash
# ❌ 禁止
sed -i 's/old/new/g' file.cs
awk '{print $1}' file.txt

# ✅ 使用Edit工具
Edit({
  file_path: "file.cs",
  old_string: "old",
  new_string: "new"
})
```

#### 3. 内容搜索

```bash
# ❌ 禁止
grep -r "pattern" src/
find . -name "*.cs" -exec grep "pattern" {} \;

# ✅ 使用Grep工具
Grep({
  pattern: "pattern",
  path: "src/",
  glob: "*.cs"
})
```

### 并发执行原则

#### 禁止后台并发

```bash
# ❌ 禁止：后台并发执行
dotnet test Project1.Tests &
dotnet test Project2.Tests &
dotnet test Project3.Tests &
wait

# ✅ 推荐：串行执行
dotnet test Project1.Tests
dotnet test Project2.Tests
dotnet test Project3.Tests
```

**原因**:
- 资源竞争导致超时
- git操作冲突产生index.lock
- 测试失败难以定位

#### 正确的批量执行

```bash
# ✅ 串行 + 错误处理
TEST_PROJECTS=(
  "LYBT.Desktop.Users.Tests"
  "LYBT.Desktop.Herbs.Tests"
  "LYBT.Desktop.Formula.Tests"
)

FAILED=()

for proj in "${TEST_PROJECTS[@]}"; do
  echo ">>> 测试: $proj"

  if ! dotnet test "tests/$proj/$proj.csproj" --no-build; then
    FAILED+=("$proj")
    echo "❌ 失败: $proj"
  else
    echo "✅ 通过: $proj"
  fi

  # 清理潜在锁文件
  [ -f .git/index.lock ] && rm -f .git/index.lock
done

echo "=== 测试完成 ==="
echo "失败数量: ${#FAILED[@]}"
```

---

## Windows环境特殊考虑

### 1. 路径处理

#### 反斜杠 vs 正斜杠

```bash
# ❌ Windows路径（反斜杠）
D:\source\repos\LYBTZYZS\src\Client\Desktop\...

# ✅ 跨平台路径（正斜杠）
D:/source/repos/LYBTZYZS/src/Client/Desktop/...
```

#### 路径引号

```bash
# ❌ 无引号（空格路径会失败）
cd D:/source/repos/My Project

# ✅ 双引号
cd "D:/source/repos/My Project"
```

### 2. 文件锁机制

Windows文件锁比Unix更严格：

```bash
# 问题：进程被kill后锁文件未释放
dotnet test ... &  # 后台执行
# 进程超时被kill
# .git/index.lock遗留

# 解决：串行执行 + 锁文件检查
dotnet test ...
if [ -f .git/index.lock ]; then
  rm -f .git/index.lock
fi
```

### 3. 换行符

Windows (CRLF) vs Unix (LF):

```bash
# Git配置：自动转换
git config --global core.autocrlf true

# 编辑器配置：LF优先
# VS Code: "files.eol": "\n"
```

### 4. PowerShell vs Bash

优先使用Bash（Git Bash）:

```bash
# ❌ PowerShell语法
Get-ChildItem -Recurse | Where-Object { $_.Extension -eq ".cs" }

# ✅ Bash语法
find . -name "*.cs"
```

### 5. 进程管理

```bash
# Windows进程查看
ps aux | grep dotnet  # Git Bash
tasklist | findstr dotnet  # CMD

# 进程终止
kill -9 <PID>  # Git Bash
taskkill /F /PID <PID>  # CMD
```

---

## 常见错误与修复

### 错误1: Graphiti episode_body类型错误

**错误信息**:
```
Input should be a valid string [type=string_type, input_value={...}, input_type=dict]
```

**原因**: episode_body参数传入了JSON对象而非字符串

**修复**:
```typescript
// ❌ 错误
mcp__graphiti-memory__add_memory({
  episode_body: { "key": "value" }  // 对象
})

// ✅ 修复
mcp__graphiti-memory__add_memory({
  episode_body: `key: value\n...`  // 字符串
})
```

### 错误2: git index.lock文件存在

**错误信息**:
```
fatal: Unable to create '.git/index.lock': File exists
```

**原因**: 并发git操作或进程异常终止

**修复**:
```bash
# 立即修复
rm -f .git/index.lock

# 预防措施
# 1. 避免并发测试
# 2. 测试后检查锁文件
if [ -f .git/index.lock ]; then
  echo "⚠️ 检测到index.lock，清理中..."
  rm -f .git/index.lock
fi
```

### 错误3: GitHub MCP权限不足

**错误信息**:
```
Resource not accessible by integration
```

**原因**: 访问无权限的仓库或操作

**修复**:
```typescript
// 1. 确认当前用户
const user = mcp__github__get_me()
console.log(`当前用户: ${user.login}`)

// 2. 仅操作授权仓库
mcp__github__issue_write({
  owner: user.login,  // 使用当前用户
  repo: "LYBTZYZS"
})
```

### 错误4: Serena符号未找到

**错误信息**:
```
Symbol 'Foo/Bar' not found
```

**原因**:
- name_path拼写错误
- 符号已被重构删除
- 搜索路径不正确

**修复**:
```typescript
// 1. 先获取符号概览
const overview = mcp__serena__get_symbols_overview({
  relative_path: "FormulaDetailViewModel.cs"
})

// 2. 使用substring_matching
const symbols = mcp__serena__find_symbol({
  name_path: "HerbCount",
  substring_matching: true,  // 模糊匹配
  relative_path: ""  // 全局搜索
})
```

### 错误5: 测试超时中止

**错误信息**:
```
中止测试运行: 测试运行超时时间超出 300000 毫秒
```

**原因**:
- 并发测试导致资源竞争
- 单个测试套件过大
- 测试代码存在死循环

**修复**:
```bash
# 1. 串行执行
dotnet test Project1.Tests
dotnet test Project2.Tests

# 2. 增加超时配置
dotnet test --timeout 600000  # 10分钟

# 3. 分片执行
dotnet test --filter "Category=Fast"
dotnet test --filter "Category=Slow"
```

---

## 工具选择决策树

```
任务类型？
├─ 代码操作
│  ├─ 读取代码 → Serena (read_file, find_symbol)
│  ├─ 编辑代码 → Serena (replace_symbol_body, replace_regex)
│  ├─ 搜索代码 → Serena (search_for_pattern, find_referencing_symbols)
│  └─ 搜索GitHub代码 → GitHub MCP (search_code)
│
├─ GitHub操作
│  ├─ Issue管理 → GitHub MCP (issue_write, search_issues)
│  ├─ PR管理 → GitHub MCP (pull_request_read, create_pull_request)
│  ├─ 代码搜索 → GitHub MCP (search_code)
│  └─ 仓库搜索 → GitHub MCP (search_repositories)
│
├─ 知识管理
│  ├─ 记录经验 → Graphiti (add_memory)
│  ├─ 检索历史 → Graphiti (search_memory_facts, search_nodes)
│  ├─ 架构决策 → Graphiti (add_memory with ADR template)
│  └─ 最佳实践 → Graphiti (add_memory with practices template)
│
├─ 系统操作
│  ├─ 编译构建 → Bash (dotnet build)
│  ├─ 运行测试 → Bash (dotnet test, 串行)
│  ├─ Git操作 → Bash (git add/commit/push)
│  └─ 环境检查 → Bash (ps, ls, rm)
│
└─ 文件操作
   ├─ 读取文件 → Read工具
   ├─ 编辑文件 → Edit工具
   ├─ 搜索文件 → Grep工具
   └─ 创建文件 → Write工具
```

---

## 附录：快速参考

### GitHub MCP核心命令

```typescript
// Issue
mcp__github__issue_write({ method: "create|update" })
mcp__github__search_issues({ query: "..." })
mcp__github__issue_read({ method: "get|get_comments" })

// PR
mcp__github__create_pull_request({ ... })
mcp__github__pull_request_read({ method: "get|get_diff|get_files" })
mcp__github__pull_request_review_write({ method: "create|submit_pending" })

// Search
mcp__github__search_code({ query: "..." })
mcp__github__search_repositories({ query: "..." })

// User
mcp__github__get_me()
```

### Graphiti核心命令

```typescript
// 写入
mcp__graphiti-memory__add_memory({
  name: "...",
  episode_body: "..." // 必须是字符串！
})

// 搜索
mcp__graphiti-memory__search_memory_facts({ query: "...", max_facts: 30 })
mcp__graphiti-memory__search_nodes({ query: "...", max_nodes: 15 })
mcp__graphiti-memory__get_episodes({ max_episodes: 20 })

// 管理
mcp__graphiti-memory__delete_episode({ uuid: "..." })
mcp__graphiti-memory__clear_graph({ group_ids: ["main"] })
mcp__graphiti-memory__get_status()
```

### Serena核心命令

```typescript
// 读取
mcp__serena__read_file({ relative_path: "..." })
mcp__serena__get_symbols_overview({ relative_path: "..." })
mcp__serena__find_symbol({ name_path: "...", include_body: true })

// 编辑
mcp__serena__replace_symbol_body({ name_path: "...", body: "..." })
mcp__serena__replace_regex({ regex: "...", repl: "..." })
mcp__serena__insert_after_symbol({ name_path: "...", body: "..." })

// 搜索
mcp__serena__search_for_pattern({ substring_pattern: "..." })
mcp__serena__find_referencing_symbols({ name_path: "..." })
```

---

**维护者**: Claude Code AI Assistant
**审核者**: TonyShou
**版本**: v1.0
**最后更新**: 2025-11-20
