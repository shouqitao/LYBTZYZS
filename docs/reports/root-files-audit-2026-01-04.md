# 根目录文件审计报告

**日期**: 2026-01-04
**扫描结果**: ✅ 正常

## 统计信息

- 根目录文件总数: 12个
- 根目录目录总数: 5个
- 白名单文件数: 17个
- 白名单模式数: 1个
- 允许目录数: 15个
- 违规文件数: 0个
- 违规目录数: 0个

## 文件列表

### ✅ 符合白名单的文件

- AGENTS.md
- CHANGELOG.md
- CLAUDE.md
- Directory.Build.props
- Directory.Packages.props
- global.json
- LYBT.All.sln
- LYBT.Desktop.sln
- LYBT.Server.sln
- nuget.config
- README.md
- version.txt


### ❌ 违规文件

- 无

### ❌ 违规目录

- 无

## 建议操作

✅ 无需清理，根目录整洁

## 白名单配置

**配置文件**: .config/root-files-whitelist.json

### 允许的文件
- `README.md`
- `CLAUDE.md`
- `AGENTS.md`
- `CHANGELOG.md`
- `CONTRIBUTING.md`
- `LICENSE`
- `version.txt`
- `shrimp-rules.md`
- `.gitignore`
- `.gitattributes`
- `.editorconfig`
- `.runsettings`
- `.env.example`
- `global.json`
- `nuget.config`
- `Directory.Build.props`
- `Directory.Packages.props`

### 允许的模式
- `*.sln`

### 允许的目录
- `src/`
- `tests/`
- `docs/`
- `docs-archive/`
- `scripts/`
- `.github/`
- `.claude/`
- `.config/`
- `.githooks/`
- `.git/`
- `.vs/`
- `.ai/`
- `.serena/`
- `BIN/`
- `openspec/`

---

🤖 自动生成于 2026-01-04 00:27:17 UTC+8
🔧 检查脚本: `scripts/maintenance/check-root-files.ps1`
