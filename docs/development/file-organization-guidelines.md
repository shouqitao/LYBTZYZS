# 文件组织和防污染指南

> **目的**：保持项目根目录整洁，防止临时文件污染
> **创建日期**：2025-09-28
> **维护人**：开发团队

## 📋 文件组织规范

### 1. 根目录文件规则

#### ✅ 允许存在的文件
- **解决方案文件**：`*.sln`
- **配置文件**：`.gitignore`, `.gitattributes`, `.editorconfig`
- **项目配置**：`global.json`, `nuget.config`, `Directory.*.props`
- **文档**：`README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `LICENSE`
- **CI/CD**：`.github/`, `azure-pipelines.yml`
- **工作配置**：`CLAUDE.md`（AI工作指南）

#### ❌ 禁止存在的文件
- **构建日志**：`*.log`, `build.log`, `*-warnings.log`
- **临时文件**：`*.tmp`, `*.temp`, `temp_*`
- **备份文件**：`*.bak`, `*.backup`, `*_backup.*`
- **IDE配置**：`*.user`, `*.suo`, `.vs/`, `.idea/`
- **测试输出**：`TestResults/`, `*.trx`

### 2. 目录结构规范

```
LYBTZYZS/
├── src/                    # 源代码
├── tests/                  # 测试代码
├── docs/                   # 文档
│   ├── architecture/       # 架构文档
│   ├── development/        # 开发文档
│   ├── reports/            # 各类报告
│   └── tasks/              # 任务管理
├── scripts/                # 脚本工具
├── tools/                  # 开发工具
└── .github/                # GitHub配置
```

### 3. 文件归类规则

| 文件类型 | 正确位置 | 示例 |
|---------|---------|------|
| 构建日志 | 忽略（.gitignore） | `build.log` → 不提交 |
| 任务文档 | `docs/tasks/` | `task-xxx.md` |
| 架构报告 | `docs/architecture/reports/` | `implementation-report.md` |
| 安全报告 | `docs/reports/` | `security-audit.html` |
| 技术债务 | `docs/development/` | `TECH_DEBT_BACKLOG.md` |
| 脚本工具 | `scripts/` | `cleanup.ps1` |
| 临时文件 | `temp/`（忽略） | 自动忽略 |

## 🛡️ 防污染策略

### 1. 预防措施

#### 自动化清理
```powershell
# 定期运行清理脚本
./scripts/cleanup.ps1

# 查看将要删除的文件
./scripts/cleanup.ps1 -WhatIf

# 详细模式
./scripts/cleanup.ps1 -Verbose
```

#### Git Hooks（推荐）
创建 `.git/hooks/pre-commit` 文件：
```bash
#!/bin/sh
# 检查是否有临时文件要提交
if git diff --cached --name-only | grep -E '\.(log|tmp|temp|bak|backup)$'; then
    echo "❌ 错误：尝试提交临时文件！"
    echo "请先运行 ./scripts/cleanup.ps1 清理项目"
    exit 1
fi
```

### 2. 常见问题原因

#### 构建日志污染
**原因**：MSBuild/dotnet build 输出重定向
**预防**：
```powershell
# ❌ 错误：输出到根目录
dotnet build > build.log

# ✅ 正确：输出到logs目录
dotnet build > logs/build-$(Get-Date -Format "yyyyMMdd-HHmmss").log
```

#### 备份文件污染
**原因**：编辑器自动备份
**预防**：
- 配置编辑器备份到特定目录
- 添加 `*.bak` 到 `.gitignore`

#### 异常文件名
**原因**：PowerShell/Bash 命令错误
**预防**：
```powershell
# ❌ 错误：特殊字符未转义
echo "test" > 更新时间：$(date)

# ✅ 正确：使用合法文件名
echo "test" > "update-$(Get-Date -Format 'yyyyMMdd').txt"
```

### 3. 监控和告警

#### 定期检查脚本
```powershell
# check-pollution.ps1
$pollutants = Get-ChildItem -Path . -Include "*.log","*.tmp","*.bak" -File

if ($pollutants.Count -gt 0) {
    Write-Warning "发现 $($pollutants.Count) 个污染文件！"
    $pollutants | Format-Table Name, Length, LastWriteTime
    Write-Host "运行 ./scripts/cleanup.ps1 进行清理"
}
```

#### CI/CD 集成
在 GitHub Actions 中添加检查：
```yaml
- name: Check for pollution files
  run: |
    if find . -name "*.log" -o -name "*.tmp" -o -name "*.bak" | grep .; then
      echo "::error::发现临时文件，请清理后再提交"
      exit 1
    fi
```

## 📊 污染文件分类

### 高风险（立即删除）
- `*.log` - 可能包含敏感信息
- `*.user` - 包含用户特定路径
- `.env.local` - 可能包含密钥

### 中风险（评估后删除）
- `*.bak` - 可能包含旧版本代码
- `*.tmp` - 临时处理文件
- `test_*` - 测试输出文件

### 低风险（整理归档）
- 报告文件 → 移动到 `docs/reports/`
- 文档文件 → 移动到相应 `docs/` 子目录
- 脚本文件 → 移动到 `scripts/`

## 🔧 工具使用

### 清理命令
```powershell
# 基础清理
./scripts/cleanup.ps1

# 深度清理（包括bin/obj）
dotnet clean
./scripts/cleanup.ps1

# Git清理未跟踪文件
git clean -fdx -e ".env" -e "appsettings.Development.json"
```

### 验证清洁度
```powershell
# 检查根目录文件数
(Get-ChildItem -Path . -File).Count

# 理想状态：< 20个文件
# 警告状态：20-30个文件
# 需要清理：> 30个文件
```

## 📝 最佳实践

1. **每日清理**：开发结束后运行 `cleanup.ps1`
2. **提交前检查**：`git status` 确认无临时文件
3. **定期审查**：每周检查根目录新增文件
4. **文档归档**：及时将文档移到正确位置
5. **脚本自动化**：使用脚本代替手动操作

## 🚨 紧急清理

如果根目录严重污染：
```powershell
# 1. 备份重要文件
git stash

# 2. 强制清理
git clean -fdx

# 3. 恢复必要文件
git checkout -- .
git stash pop

# 4. 重新构建
dotnet restore
dotnet build
```

---

*通过遵循这些指南，我们可以保持项目整洁，提高开发效率，减少潜在的安全风险。*