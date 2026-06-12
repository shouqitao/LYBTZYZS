# 部署与回滚指南

本文档定义 LYBT 系统的部署流程和回滚策略。服务端使用 Windows Service 部署，客户端使用 ClickOnce/文件夹分发。

> 备份恢复见 [07-backup-recovery.md](./07-backup-recovery.md)；监控告警见 [08-monitoring-alerting.md](./08-monitoring-alerting.md)；部署架构见 [01-deployment.md](./01-deployment.md)。

---

## 版本管理

### 版本号规则

遵循语义化版本 `MAJOR.MINOR.PATCH`（如 `1.2.3`），记录于 `VERSION` 文件。

### 发布包结构

```
C:\Services\LYBT-releases\
├── v1.2.3\                    # 当前版本
│   ├── LYBT.WebAPI\           # 服务端发布包
│   └── LYBT.Desktop\          # 客户端发布包
├── v1.2.2\                    # 前一版本（回滚保留）
└── rollback.json              # 回滚元数据
```

---

## 服务端部署流程

### 前置检查

```powershell
# 1. 确认当前版本
Get-Content "C:\Services\LYBT-API\VERSION"

# 2. 健康检查
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get

# 3. 数据库备份（必需）
$ts = Get-Date -Format "yyyyMMdd_HHmmss"
Backup-SqlDatabase -ServerInstance "." -Database "LYBT_DB" -BackupFile "D:\Backup\LYBT_DB_pre_deploy_$ts.bak"
```

### 部署步骤

```powershell
# 1. 停止服务
sc stop LYBT-WebAPI
Start-Sleep -Seconds 5

# 2. 备份当前版本
$currentVer = Get-Content "C:\Services\LYBT-API\VERSION"
Copy-Item "C:\Services\LYBT-API" "C:\Services\LYBT-releases\v$currentVer" -Recurse -Force

# 3. 部署新版本
# (a) 交叉编译（如在 Ubuntu 构建）
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj `
    -c Release -r win-x64 --self-contained false `
    -p:EnableWindowsTargeting=true

# (b) 复制发布产出
Copy-Item "bin\Release\win-x64\publish\*" "C:\Services\LYBT-API\" -Recurse -Force

# 4. 更新版本号
Set-Content "C:\Services\LYBT-API\VERSION" "v1.2.3"

# 5. 启动服务
sc start LYBT-WebAPI
Start-Sleep -Seconds 10

# 6. 验证
Invoke-RestMethod -Uri "http://localhost:5000/health/details" -Method Get
```

---

## 回滚流程

### 场景 1：服务启动失败

```powershell
# 1. 查看事件日志
Get-EventLog -LogName Application -Newest 20 | Where-Object { $_.Source -like "*LYBT*" }

# 2. 回滚到前一版本
sc stop LYBT-WebAPI
$prevVer = "v1.2.2"  # 前一版本号
Remove-Item "C:\Services\LYBT-API\*" -Recurse -Force -Exclude VERSION
Copy-Item "C:\Services\LYBT-releases\$prevVer\*" "C:\Services\LYBT-API\" -Recurse -Force
sc start LYBT-WebAPI

# 3. 验证
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
```

### 场景 2：数据库迁移需回退

```
⚠️ EF Core 不支持自动迁移回退。处理方式：
```

```powershell
# 1. 停止服务
sc stop LYBT-WebAPI

# 2. 恢复数据库到部署前备份
$sqlcmd = "RESTORE DATABASE [LYBT_DB] FROM DISK = N'<部署前备份路径>' WITH REPLACE"
Invoke-Sqlcmd -Query $sqlcmd -ServerInstance "."

# 3. 回滚应用版本（同场景 1）
# 4. 启动并验证
```

### 场景 3：功能异常但服务正常

```
1. 在 Diagnostics API 启用 Debug 日志
   → POST /api/v1/diagnostics/logging/debug/enable { "level": "Debug", "durationMinutes": 30 }

2. 复现问题，收集日志
   → GET /api/v1/diagnostics/recent-logs
   → 复制 logs/ 目录下相关日志文件

3. 评估影响
   → 非关键功能异常 → 记录问题，安排修复版本
   → 关键功能不可用 → 执行回滚（场景 1）

4. 修复后重新部署（回到"部署流程"）
```

---

## 客户端部署

### 发布流程

```powershell
# 1. 构建 Desktop 发布包
dotnet publish src/Client/Desktop/Shell/App.csproj -c Release -r win-x64

# 2. 复制到发布目录
Copy-Item "bin\Release\win-x64\publish\*" "C:\Services\LYBT-releases\v1.2.3\LYBT.Desktop\" -Recurse

# 3. 各客户端从共享目录安装
# \\SERVER\LYBT-releases\v1.2.3\LYBT.Desktop\setup.exe
```

### 客户端回滚

客户端版本独立于服务端。回滚方式：

1. 关闭 Desktop 客户端
2. 从 `C:\Services\LYBT-releases\v前版本号\LYBT.Desktop\` 重新复制
3. 客户端配置在 `%APPDATA%\LYBT\` — 回滚不影响配置和数据

---

## 回滚决策矩阵

| 场景 | 影响 | 回滚时间 | 操作 |
|------|------|----------|------|
| 服务无法启动 | 全部用户 | < 5 分钟 | 场景 1：版本回滚 |
| 数据库迁移失败 | 全部用户 | < 30 分钟 | 场景 2：数据库恢复 + 版本回滚 |
| 单一功能异常 | 部分用户 | 评估后决定 | 场景 3：Debug → 评估 → 修复或回滚 |
| 客户端不兼容 | 单用户 | < 10 分钟 | 客户端版本回滚（不影响服务端） |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
