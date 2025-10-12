# 文件组织规范

> **📖 本文档定义项目的文件组织强制规则，确保仓库结构清晰、易于维护。**

## 🚫 文件创建强制规则

**创建任何文件前，必须通过以下检查清单**：

### ✅ 文档类文件（.md/.txt/.pdf）

```
1. 是否为核心文档？（README/CLAUDE/CHANGELOG/CONTRIBUTING）
   → 是：可放根目录
   → 否：继续下一步

2. 确定文档类型：
   - 架构设计 → docs/architecture/
   - API文档 → docs/api/
   - 开发指南 → docs/development/
   - 分析报告 → docs/reports/
   - 任务说明 → docs/issues/
   - 其他文档 → docs/对应分类/

3. ❌ 禁止在根目录创建任何文档文件（核心文档除外）
```

### ✅ 脚本类文件（.ps1/.sh/.py/.js）

```
1. 确定脚本用途：
   - 构建脚本 → scripts/build/
   - 测试脚本 → scripts/testing/
   - 部署脚本 → scripts/deployment/
   - 维护脚本 → scripts/maintenance/
   - 分析脚本 → scripts/analysis/

2. ❌ 禁止在根目录创建任何脚本文件
```

### ✅ 配置类文件（.json/.xml/.yaml）

```
1. 是否为根级配置？（nuget.config/global.json等白名单）
   → 是：可放根目录
   → 否：放入 .config/ 或对应子目录

2. ❌ 禁止在根目录创建临时配置文件
```

### ✅ 输出类文件（.txt/.csv/.log）

```
1. 临时输出 → 使用内存或临时变量，禁止落盘
2. 需要保留 → docs/reports/ 或 scripts/analysis/outputs/
3. ❌ 禁止在根目录创建任何输出文件
```

### ✅ 截图/图片文件（.png/.jpg/.gif）

```
1. 文档配图 → docs/assets/images/
2. 调试截图 → docs/assets/screenshots/
3. ❌ 禁止在根目录保存任何图片文件
```

---

## 违规示例 ❌

```bash
# 错误：在根目录创建输出文件
output.txt          → 应该：内存变量或 docs/reports/output-{date}.txt
result.json         → 应该：docs/reports/result-{date}.json
Screenshot.png      → 应该：docs/assets/screenshots/debug-{date}.png
test.ps1            → 应该：scripts/testing/test.ps1
临时文档.md          → 应该：docs/reports/临时分析-{date}.md
```

---

## 正确示例 ✅

```bash
# 文档归档
docs/reports/performance-analysis-2025-01-11.md
docs/api/swagger-spec-v2.json

# 脚本归档
scripts/testing/test-all-apis.ps1
scripts/analysis/check-dependencies.py

# 配置归档
.config/root-files-whitelist.json
config/appsettings.Development.json
```

---

## 自动化防护

项目配置了自动化机制来防止违规文件进入仓库：

- ✅ **Pre-commit hook**：会自动检查根目录文件（`.githooks/pre-commit`）
- ✅ **白名单配置**：`.config/root-files-whitelist.json`
- ✅ **拒绝提交**：违规文件会被拒绝提交，并提示正确路径

---

## 相关文档

- `CLAUDE.md` - 主文档，核心约束
- `docs/development/file-organization-guidelines.md` - 详细的文件组织指南
- `.config/root-files-whitelist.json` - 根目录白名单配置
