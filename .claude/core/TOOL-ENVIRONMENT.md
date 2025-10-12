# 工具环境说明

> **📖 明确区分"项目运行环境"和"Claude Code 工具环境"，避免命令使用混淆。**

## ⚠️ 重要区分

本项目涉及两个不同的环境：

| 环境 | 说明 | 操作系统 | Shell | 用途 |
|------|------|---------|-------|------|
| **项目运行环境** | 开发者本地机器 | Windows 10/11 | PowerShell 7.x+ | 运行项目、编译代码、调试 |
| **Claude Code 工具环境** | Claude 的 Bash 工具 | Linux | `/usr/bin/bash` | 执行自动化命令 |

---

## 🖥️ 项目运行环境（Windows）

### 强制要求

- ✅ **操作系统**：Windows 10/11
- ✅ **Shell 环境**：PowerShell 7.x+
- ✅ **版本控制**：Git for Windows 2.40+
- ✅ **开发工具**：Visual Studio 2022 或 JetBrains Rider
- ✅ **.NET SDK**：.NET 8.0 SDK

### 常用命令（PowerShell）

```powershell
# 文件操作
Get-ChildItem       # 列出文件（别名: ls, dir）
Get-Content         # 读取文件内容（别名: cat, type）
Set-Content         # 写入文件内容
Copy-Item           # 复制文件（别名: cp, copy）
Move-Item           # 移动文件（别名: mv, move）
Remove-Item         # 删除文件（别名: rm, del）

# 文本搜索
Select-String       # 类似 grep 的文本搜索

# 进程管理
Get-Process         # 列出进程
Stop-Process        # 终止进程

# 环境变量
$env:PATH           # 访问环境变量
[Environment]::SetEnvironmentVariable() # 设置环境变量

# 项目构建
dotnet restore LYBT.All.sln
dotnet build LYBT.Server.sln -c Release --no-restore
dotnet build LYBT.Desktop.sln -c Release --no-restore
dotnet test LYBT.Server.sln -c Release
```

---

## 🤖 Claude Code 工具环境（Linux bash）

### 环境特性

- ✅ **Shell**：`/usr/bin/bash`（标准 Linux shell）
- ✅ **支持命令**：标准 Unix 命令（cat, grep, find, wc 等）
- ❌ **不支持**：PowerShell 特有命令（Get-*, Select-* 等）

### 正确使用 Bash 工具

**✅ 正确示例（使用标准 Unix 命令）**：

```bash
# 文件操作
cat CLAUDE.md                          # 读取文件
ls -la                                 # 列出文件
find . -name "*.cs"                    # 查找文件
grep "pattern" file.txt                # 搜索文本
wc -l file.txt                         # 统计行数

# Git 操作
git status
git diff
git log --oneline -10
```

**❌ 错误示例（PowerShell 命令在 bash 中不可用）**：

```powershell
# ❌ 这些命令会失败：
Get-Content CLAUDE.md                  # 错误！Bash 中无此命令
Select-String "pattern" file.txt       # 错误！应使用 grep
Get-ChildItem -Recurse -Filter "*.cs"  # 错误！应使用 find
```

### 常见错误与解决

| 错误命令（PowerShell） | 正确命令（Bash） | 说明 |
|---------------------|-----------------|------|
| `Get-Content file.txt` | `cat file.txt` | 读取文件 |
| `Select-String "pattern" file` | `grep "pattern" file` | 搜索文本 |
| `Get-ChildItem` | `ls` 或 `ls -la` | 列出文件 |
| `Get-ChildItem -Recurse -Filter "*.cs"` | `find . -name "*.cs"` | 查找文件 |
| `(Get-Content file.txt).Length` | `wc -l < file.txt` | 统计行数 |
| `Measure-Object` | `wc` | 统计信息 |

---

## ⭐ MCP 工具优先策略（推荐）

为了避免跨平台兼容性问题，**强烈推荐优先使用 MCP 工具**：

### 优先级排序

| 优先级 | 工具 | 用途 | 优势 |
|-------|------|------|------|
| ⭐⭐⭐ | `filesystem` MCP | 文件读写、目录遍历 | 跨平台，API 统一 |
| ⭐⭐⭐ | `git` MCP | 版本控制操作 | 跨平台，JSON 输出 |
| ⭐⭐⭐ | `serena` MCP | 语义代码操作 | 语义级，精确操作 |
| ⭐⭐ | `Bash` 工具 | 标准 Unix 命令 | 灵活，但需注意兼容性 |
| ⚠️ | PowerShell 命令 | 仅在项目环境使用 | 不可用于 Claude Code 工具 |

### 最佳实践

```
1. 文件操作 → 优先使用 filesystem MCP
   ✅ Read tool
   ✅ Write tool
   ✅ mcp__filesystem__* 系列工具

2. Git 操作 → 优先使用 git MCP
   ✅ mcp__git__git_status
   ✅ mcp__git__git_diff
   ✅ mcp__git__git_commit

3. 代码搜索 → 优先使用 serena MCP
   ✅ mcp__serena__find_symbol
   ✅ mcp__serena__search_for_pattern

4. 简单命令 → 可使用 Bash 工具（标准 Unix 命令）
   ✅ cat, grep, find, wc, ls
   ❌ Get-*, Select-*, Measure-Object
```

---

## 🔍 快速参考

### 场景：查看文件内容

```bash
# 项目环境（PowerShell）
Get-Content CLAUDE.md

# Claude Code 环境（选择一种）
cat CLAUDE.md                    # ✅ Bash 工具
Read tool(CLAUDE.md)             # ⭐ 推荐：Read 工具
```

### 场景：搜索代码

```bash
# 项目环境（PowerShell）
Select-String "pattern" -Path *.cs -Recurse

# Claude Code 环境（选择一种）
grep -r "pattern" --include="*.cs" .      # ✅ Bash 工具
Grep tool(pattern="pattern", type="cs")   # ⭐ 推荐：Grep 工具
mcp__serena__search_for_pattern(...)      # ⭐⭐⭐ 最佳：Serena 工具
```

### 场景：Git 状态

```bash
# 项目环境（PowerShell）
git status

# Claude Code 环境（选择一种）
git status                                # ✅ Bash 工具
mcp__git__git_status(path=".")           # ⭐ 推荐：Git MCP
```

---

## 相关文档

- `CLAUDE.md` - 主文档
- `docs/development/mcp-tools-reference.md` - MCP 工具完整参考
- `.claude/core/RULES.md` - 工具选择优先级
