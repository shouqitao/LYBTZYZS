# 术语替换清单 - Issue #1017

> **创建日期**: 2025-10-07
> **关联 Issue**: [#1017 术语统一替换](https://github.com/shouqitao/LYBTZYZS/issues/1017)
> **执行人**: Claude Code

---

## 📋 替换规则

### ✅ 应该替换的场景
| 原文 | 替换为 | 场景说明 |
|------|--------|----------|
| `LYBTZYZS` | `凌隐宝堂中医诊所` | 描述性文本、标题、说明 |
| `LYBT` | `凌隐宝堂` | 描述性文本（非代码引用） |
| `凌隐宝堂中医诊所管理系统（LYBTZYZS）` | `凌隐宝堂中医诊所管理系统` | 简化括号说明 |

### ❌ 不应替换的场景
| 类型 | 示例 | 说明 |
|------|------|------|
| 文件路径 | `D:\source\repos\LYBTZYZS\...` | 物理路径 |
| GitHub URL | `github.com/shouqitao/LYBTZYZS` | 仓库名称 |
| 代码命名空间 | `LYBT.*`, `LYBT.Module.*` | 代码标识符 |
| 项目文件 | `LYBT.All.sln`, `LYBT.Desktop.sln` | 项目文件名 |
| 代码引用 | `dotnet build LYBT.All.sln` | 命令中的文件名 |

---

## 📊 LYBTZYZS 出现位置分类

### ✅ 需要替换（描述性文本）

#### 根目录文档

1. **README.md**
   - 行 1: `# 凌隐宝堂中医诊所管理系统（LYBTZYZS）` → `# 凌隐宝堂中医诊所管理系统`
   - 行 7: `[![Build Status]...(LYBTZYZS)` → 保留（URL中）

2. **CONTRIBUTING.md**
   - 行 3: `凌隐宝堂中医诊所系统 (LYBTZYZS)` → `凌隐宝堂中医诊所系统`

3. **GEMINI.md**
   - 行 4: `LYBTZYZS project` → `凌隐宝堂中医诊所项目`

#### docs/architecture/

4. **ADR-001-cqrs-mediatr-rejection.md**
   - 行 68: `凌隐宝堂中医诊所管理系统（LYBTZYZS）` → `凌隐宝堂中医诊所管理系统`

#### docs/ 目录

5. **DEVELOPER_GUIDE.md**
   - 行 1: `# 开发者指南 - LYBTZYZS 项目` → `# 开发者指南 - 凌隐宝堂中医诊所项目`
   - 行 24: `LYBTZYZS（凌隐宝堂中医诊所管理系统）` → `凌隐宝堂中医诊所管理系统`

6. **docs/index.md**
   - 行 1: `# LYBTZYZS 文档中心` → `# 凌隐宝堂中医诊所文档中心`

#### docs/development/

7. **mcp-tools-reference.md**
   - 行 7: `本文档提供LYBTZYZS项目中所有MCP` → `本文档提供凌隐宝堂中医诊所项目中所有MCP`

8. **server-testing-architecture-completion-report.md**
   - 行 3: `**项目**: 凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)` → `**项目**: 凌隐宝堂中医诊所诊疗系统`

9. **terminology-correction-guide.md**
   - 行 35: 表格中保留（这是定义文档）

#### docs/reports/

10. **architecture-analysis-2025-09-25.md**
    - 行 5: `分析范围：LYBTZYZS 全解决方案` → `分析范围：凌隐宝堂中医诊所全解决方案`
    - 行 9: `本报告通过对LYBTZYZS解决方案` → `本报告通过对凌隐宝堂中医诊所解决方案`

11. **archive/ 下的多个报告**
    - `2025-09-23-gemini-code-review-server.md` 行 1
    - `2025-09-24-server-layer-architecture-analysis.md` 行 1, 10, 210
    - `desktop-architecture-analysis-2025-09-25.md` 行 1, 10, 276
    - 等等...

12. **issue-1013-phase5-completion-report.md**
    - 行 3: GitHub Issue URL（保留，不替换）

13. **obsolete-unused-code-report.md**
    - 行 5: `**检查范围**: LYBTZYZS 完整解决方案` → `**检查范围**: 凌隐宝堂中医诊所完整解决方案`

14. **workflow-analysis-issue-933.md**
    - 行 438: GitHub Issue URL（保留）

#### docs/issues/

15. **ISSUE_808_DESKTOP_ARCHITECTURE_OPTIMIZATION.md**
    - 行 3: GitHub Issue URL（保留）

16. **users-module-refactoring-issue.md**
    - 行 149: GitHub Issue URL（保留）

#### scripts/

17. **ISSUE-820-README.md**
    - 需检查具体内容

#### src/Server/

18. **README.md**
    - 需检查具体内容

---

### ❌ 不应替换（技术引用）

#### 文件路径引用
- `D:\source\repos\LYBTZYZS\...` - 所有文件路径保持不变
- `docs/architecture/design/desktop-architecture-guide.md` 行 557: Python 脚本中的路径

#### GitHub URLs
- `https://github.com/shouqitao/LYBTZYZS` - 仓库名称保持不变
- 所有 Issue/PR 链接保持不变

#### 命令行引用
- `cd LYBTZYZS` - 目录名保持不变
- `git clone https://github.com/shouqitao/LYBTZYZS.git` - 保持不变

#### 代码引用
- `LYBTZYZS/` 在项目结构图中 - 保持不变（表示目录结构）
- 所有命令中的文件路径

---

## 📊 LYBT 出现位置分类（待完整搜索）

### 已知需要替换的场景

1. **标题和描述中的独立使用**
   - "LYBT 管理系统" → "凌隐宝堂管理系统"
   - "LYBT 项目" → "凌隐宝堂项目"

### 已知不应替换的场景

1. **代码命名空间**
   - `LYBT.Entities`
   - `LYBT.Infrastructure`
   - `LYBT.Desktop.*`
   - `LYBT.Module.*`
   - `LYBT.Shared.*`

2. **项目文件**
   - `LYBT.All.sln`
   - `LYBT.Server.sln`
   - `LYBT.Desktop.sln`

3. **命令中的引用**
   - `dotnet build LYBT.All.sln`
   - `dotnet test LYBT.Server.sln`

---

## 📝 执行计划

### Phase 1: 分析完成 ✅
- [x] 搜索 LYBTZYZS 出现位置
- [x] 分类：描述性 vs 技术性
- [x] 生成本清单文档

### Phase 2: 批量替换（待执行）
- [ ] 替换根目录文档（3个文件）
- [ ] 替换 docs/ 主要文档（约 5个文件）
- [ ] 替换 docs/reports/ 文档（约 15个文件）
- [ ] 替换 docs/development/ 文档（约 3个文件）

### Phase 3: 验证（待执行）
- [ ] 编译验证
- [ ] 文档链接检查
- [ ] 更新 terminology-correction-guide.md

---

## 📈 统计摘要

### 文件统计
- **需要修改的文件**: 约 30 个 markdown 文件
- **不需修改的文件**: 所有代码文件、配置文件、项目文件

### 替换统计（估算）
- **LYBTZYZS 描述性使用**: 约 40-50 处
- **LYBT 描述性使用**: 待统计（需进一步搜索）
- **技术性引用（不替换）**: 约 100+ 处

---

## ⚠️ 注意事项

1. **GitHub 仓库名称**: `github.com/shouqitao/LYBTZYZS` 保持不变（无法修改）
2. **文件系统路径**: 物理路径保持不变
3. **代码标识符**: 所有命名空间、类名、项目名保持英文
4. **命令行引用**: 所有命令中的文件名/路径保持不变
5. **版本控制**: 每批替换独立提交，便于回滚

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
