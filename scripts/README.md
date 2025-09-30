# 脚本目录说明

本目录包含所有项目维护和开发相关的脚本文件。

## 📋 脚本分类

### 构建与部署脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `build.bat` | Batch | 构建整个解决方案 |
| `build-check.bat` | Batch | 构建前检查和验证 |
| `build-webapi.bat` | Batch | 构建WebAPI服务 |
| `deploy.bat` | Batch | 部署应用程序 |
| `install-dependencies.ps1` | PowerShell | 安装项目依赖包 |

### 测试相关脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `run-tests.bat` | Batch | 运行所有测试 |
| `test-port-config.ps1` | PowerShell | 测试端口配置 |
| `auth-regression-test.ps1` | PowerShell | 认证模块回归测试 |
| `automapper-validation.ps1` | PowerShell | AutoMapper映射验证 |
| `clean-test-results.ps1` | PowerShell | 清理测试结果文件 |

### 数据库管理脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `backup-database.bat` | Batch | 备份数据库 |
| `restore-database.bat` | Batch | 恢复数据库 |
| `initialize-db.bat` | Batch | 初始化数据库 |
| `check-backup-status.bat` | Batch | 检查备份状态 |

### 清理与维护脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `cleanup.ps1` | PowerShell | 清理项目临时文件 |
| `clean-solution.bat` | Batch | 清理解决方案 |
| `clean-module-readmes.ps1` | PowerShell | 清理模块README文件 |
| `clean-test-results.ps1` | PowerShell | 清理测试结果 |
| `reset-env.bat` | Batch | 重置开发环境 |

### 开发辅助脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `create_tasks.sh` | Shell | 创建任务文档（Unix/Linux） |
| `rename_tasks.ps1` | PowerShell | 批量重命名任务文件 |
| `create-placeholders.ps1` | PowerShell | 创建占位文件 |
| `watch.bat` | Batch | 监视文件变化 |

### 文档与术语管理
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `terminology-fix.ps1` | PowerShell | 修正术语错误 |
| `fix_terminology.py` | Python | 批量修正术语（Python版） |
| `generate-docs.bat` | Batch | 生成文档 |

### 问题修复脚本
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `apply-fix.ps1` | PowerShell | 应用补丁修复 |
| `fix-module-imports.ps1` | PowerShell | 修复模块导入问题 |
| `fix-xaml-bindings.ps1` | PowerShell | 修复XAML绑定问题 |

### 桌面应用相关
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `run-desktop.bat` | Batch | 运行桌面应用 |
| `debug-desktop.bat` | Batch | 调试模式运行桌面应用 |
| `migrate-desktop-config.ps1` | PowerShell | 迁移桌面配置 |

### WebAPI运行管理
| 脚本名称 | 类型 | 用途说明 |
|---------|------|----------|
| `run-webapi.ps1` | PowerShell | 启动WebAPI服务 |
| `health-check.ps1` | PowerShell | WebAPI健康检查 |
| `stop-webapi.ps1` | PowerShell | 停止WebAPI服务 |
| `check-webapi-startup.ps1` | PowerShell | WebAPI启动前环境检查（Issue #827） |

## 🚀 使用说明

### PowerShell脚本执行
```powershell
# 设置执行策略（首次使用）
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 从项目根目录执行
.\scripts\[script-name].ps1

# 带参数执行
.\scripts\clean-test-results.ps1 -WhatIf
```

### Batch脚本执行
```cmd
# 在项目根目录执行
scripts\[script-name].bat

# 或进入scripts目录执行
cd scripts
[script-name].bat
```

### Shell脚本执行（Git Bash/WSL）
```bash
# 添加执行权限
chmod +x scripts/[script-name].sh

# 执行脚本
./scripts/[script-name].sh
```

### Python脚本执行
```bash
# 确保安装Python 3.x
python scripts/[script-name].py
```

## ⚠️ 注意事项

1. **执行权限**：PowerShell脚本可能需要调整执行策略
2. **路径问题**：所有脚本应从项目根目录执行
3. **依赖检查**：某些脚本依赖特定工具（如.NET SDK、Python等）
4. **备份建议**：执行清理或修改脚本前建议先备份重要文件
5. **编码问题**：PowerShell脚本使用UTF-8编码，避免中文乱码

## 📝 脚本开发规范

1. **命名规则**：
   - 使用小写字母和连字符：`script-name.ps1`
   - 功能明确的动词开头：`clean-`, `build-`, `test-`

2. **文档要求**：
   - 脚本开头包含用途说明注释
   - 参数说明和示例用法

3. **错误处理**：
   - 包含适当的错误处理逻辑
   - 提供清晰的错误信息

4. **安全性**：
   - 避免硬编码敏感信息
   - 使用参数而非固定路径

## 🔧 维护记录

- **2025-09-30**：添加 WebAPI 启动前环境检查脚本（Issue #827）
  - 新增 `check-webapi-startup.ps1`
  - 功能：检查残留进程、端口占用、SQL Server 状态
  - 用途：解决 Visual Studio 中 WebAPI 无法启动问题
- **2025-09-28**：将根目录脚本统一移至scripts目录
  - 移动 `create_tasks.sh`
  - 移动 `create-placeholders.ps1`
  - 移动 `install-dependencies.ps1`
  - 移动 `rename_tasks.ps1`
  - 移动 `test-port-config.ps1`
- **2025-09-28**：添加术语修正脚本
- **2025-09-28**：添加测试结果清理脚本