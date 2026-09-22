# 备份与恢复指南
> 版本: v1.3 | 日期: 2026-09-22

本文档定义 LYBT 系统的备份策略、恢复流程和灾难恢复方案，覆盖服务端 SQL Server 数据库、客户端 LocalDB 数据和服务端配置文件。

> **应用内备份/恢复（B-06，2026-09-22）**：应用层已内置备份引擎——双端（远程 WebAPI / 桌面内嵌 LocalWebAPI）提供**同路由** `/api/v1/backup` 系列端点，由共享 `IBackupService`（`SqlServerBackupService`）执行 T-SQL `BACKUP DATABASE` / `RESTORE DATABASE`：远程宿主操作服务端 SQL Server，本地宿主操作本机 LocalDB。下文第 1 章的手工 SQL、SQL Server Agent 作业、PowerShell 计划任务与异地复制仍有效，但降级为**可选补充**——日常备份/恢复走应用内链路（保留 7 天、登录触发 + 24 小时间隔、可选加密与选择性恢复）。API 见 [04-api-reference/15-backup.md](../04-api-reference/15-backup.md)。

> 部署架构见 [README.md](./README.md)；数据库运维见 [01-deployment.md](./01-deployment.md)。

---

## 备份范围

| 备份对象 | 位置 | 优先级 | 备份频率 |
|----------|------|--------|----------|
| SQL Server 生产数据库 | 服务端 SQL Server 实例（应用内备份产出 `Backup:Directory` 下的 `.bak`） | **关键** | 登录触发 + 24 小时间隔（或开启宿主每日调度） |
| LocalDB 本地数据 | `(localdb)\MSSQLLocalDB` 实例，库名取连接串 `InitialCatalog`；备份文件落 `%LOCALAPPDATA%\LYBT\Desktop\Backup` | 高 | 登录触发 + 24 小时间隔 |
| 服务端配置 | `appsettings.json` + `appsettings.Production.json` | 高 | 变更时 |
| Desktop 用户数据 | `%LOCALAPPDATA%\LYBT\Desktop\`（凭据/照片/系统设置/首次运行标记；**位于安装目录之外**，Velopack 更新/卸载不影响） | 中 | 变更时 |
| 服务端日志 | `logs/` + `SystemLogs` 表 | 低 | 30天轮转（自动） |
| Desktop 发布包 | `C:\Services\LYBT-releases\` | 中 | 版本发布时 |

> **本地数据不复制 `.mdf`**：LocalDB 数据由实例管理，直接复制数据文件不可靠（实例锁 + 日志文件一致性）。备份统一经 T-SQL `BACKUP DATABASE`（应用内链路，见下）。

---

## 服务端备份

### 0. 应用内备份/恢复（推荐，B-06）

应用层内置备份引擎，双端（远程 WebAPI / 桌面内嵌 LocalWebAPI）**同路由同契约**，无需登录服务器即可操作。

| 操作 | 端点 | 权限 |
|------|------|------|
| 备份列表 | `GET /api/v1/backup` | sysadmin |
| 备份状态（上次备份/文件数/总大小/目录/保留天数/进行中进度） | `GET /api/v1/backup/status` | sysadmin |
| 可选择性恢复的表清单 | `GET /api/v1/backup/tables` | sysadmin |
| 创建备份（全量/差异，可选压缩/加密） | `POST /api/v1/backup` | sysadmin |
| 恢复（整库或选择性） | `POST /api/v1/backup/{id}/restore` | sysadmin |
| 删除备份 | `DELETE /api/v1/backup/{id}` | sysadmin |
| 清理超期备份 | `POST /api/v1/backup/cleanup` | sysadmin |
| 自动备份（登录触发） | `POST /api/v1/backup/auto` | 已认证 |

**配置项**（`appsettings.json` 的 `Backup` 节）：

| 键 | 默认 | 说明 |
|----|------|------|
| `Backup:Directory` | 服务端 `{应用基目录}/backup`；桌面 `%LOCALAPPDATA%\LYBT\Desktop\Backup` | 备份目录（绝对路径） |
| `Backup:RetentionDays` | 7 | 保留天数（`cleanup` 与自动流程按此清理） |
| `Backup:CompressByDefault` | true | 自动备份/恢复前保护性备份是否使用 SQL Server 备份压缩（手动备份由请求 `compress` 控制，DTO 默认 true） |
| `Backup:EncryptByDefault` / `Backup:EncryptionPassword` | false / 空 | 恢复前保护性备份是否加密及默认口令（启用加密但无口令则报错；手动备份由请求 `encrypt`/`password` 控制） |
| `Backup:AutoBackup:Enabled` | false | 是否启用宿主定时调度（`BackupSchedulerService`） |
| `Backup:AutoBackup:IntervalHours` | 24 | 自动备份间隔；未满间隔的触发为空操作 |
| `Backup:AutoBackup:Kind` / `Encrypt` / `InitialDelayMinutes` | Full / false / 2 | 调度备份类型、是否加密、启动后首次检查延迟 |

**自动备份与登录触发**：桌面登录成功后 fire-and-forget 调用 `POST /api/v1/backup/auto`；服务端可另开 `Backup:AutoBackup:Enabled=true` 由宿主定时任务按 `IntervalHours` 执行。两条路径共用同一间隔判定（默认 24 小时）——**每次登录都触发调用，但未满间隔不会产生新文件**，因此 7 天保留期下最多 7 个自动备份文件（对齐 [NFR-AVAIL-001](../02-requirements/12-nfr.md)）。

**备份类型与文件**：全量 `BACKUP DATABASE … WITH INIT, FORMAT[, COMPRESSION]`；差异 `WITH DIFFERENTIAL`（依赖最近一次全量，无基准则报错）；恢复前自动保护性备份（`Kind=PreRestore`，默认开启，失败不阻断恢复但返回 Warning）。**压缩能力**：SQL Server Express / LocalDB 不支持 `WITH COMPRESSION`——应用层探测实例 Edition 后自动降级为未压缩（结果 `warning` 与消息提示，`isCompressed=false`），不会使备份失败；下文的运维脚本/Agent 作业若部署在 Express 实例上同样需去掉 `COMPRESSION`。每个备份旁挂 `{文件名}.manifest.json` 侧车清单（稳定 Id/类型/大小/压缩/加密/库名/差异基准），列表中的 Id 跨列举稳定（历史 `.bak` 无清单时由文件名确定性派生），恢复/删除按 Id 定位。

### 1. SQL Server 全量备份

```sql
-- 手动全量备份
BACKUP DATABASE [LYBTDB_Dev]
TO DISK = N'D:\Backup\LYBTDB_Dev_full_20260612.bak'
WITH FORMAT, INIT,
     NAME = N'LYBTDB_Dev-Full Backup',
     COMPRESSION,
     STATS = 10;
```

### 2. 自动备份计划 (SQL Server Agent)

```sql
-- 创建每日凌晨 2:00 的全量备份作业
USE msdb;
GO

EXEC sp_add_job @job_name = N'LYBT_Daily_Full_Backup';
EXEC sp_add_jobstep @job_name = N'LYBT_Daily_Full_Backup',
    @command = N'
      DECLARE @path NVARCHAR(500) = N''D:\Backup\LYBTDB_Dev_full_'' + CONVERT(NVARCHAR(8), GETDATE(), 112) + N''.bak'';
      BACKUP DATABASE [LYBTDB_Dev] TO DISK = @path WITH COMPRESSION, INIT;
    ';
EXEC sp_add_schedule @job_name = N'LYBT_Daily_Full_Backup',
    @freq_type = 4, -- Daily
    @active_start_time = 020000;
EXEC sp_attach_schedule @job_name = N'LYBT_Daily_Full_Backup',
    @schedule_name = N'Daily_0200';
EXEC sp_add_jobserver @job_name = N'LYBT_Daily_Full_Backup';
GO
```

### 3. 备份保留策略

| 备份类型 | 保留期限 | 说明 |
|----------|----------|------|
| 每日全量 | 7 天 | 滚动覆盖 |
| 每周全量 | 4 周 | 每周日备份保留 |
| 手动备份 | 永久 | 仅在重大变更前手动创建 |

### 4. 备份验证

每周至少验证一次备份可恢复性：

```sql
-- 验证备份文件完整性
RESTORE VERIFYONLY FROM DISK = N'D:\Backup\LYBTDB_Dev_full_20260612.bak';
```

### 5. 配置文件备份

```powershell
# 每次配置变更后备份
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\Services\LYBT-API\appsettings.json" "D:\Backup\config\appsettings_$timestamp.json"
Copy-Item "C:\Services\LYBT-API\appsettings.Production.json" "D:\Backup\config\appsettings.Production_$timestamp.json"
```

### 6. PowerShell 自动备份脚本

创建 `C:\Scripts\lybt-backup.ps1`，由 Windows 任务计划程序每日调用：

```powershell
# lybt-backup.ps1 — 每日凌晨 2:00 自动执行
param(
    [string]$BackupRoot = "D:\Backup",
    [string]$Database = "LYBTDB_Dev",
    [string]$ServerInstance = ".",
    [int]$RetentionDays = 7
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dateOnly = Get-Date -Format "yyyyMMdd"
$backupDir = Join-Path $BackupRoot "database"
$configDir = Join-Path $BackupRoot "config"
$logFile = Join-Path $BackupRoot "logs\backup_$dateOnly.log"

# 确保目录存在
New-Item -ItemType Directory -Force -Path $backupDir, $configDir, (Split-Path $logFile) | Out-Null

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $entry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
    if ($Level -eq "ERROR") { Write-Error $Message } else { Write-Host $entry }
}

try {
    # 1. 数据库全量备份
    $dbBackupFile = Join-Path $backupDir "LYBTDB_Dev_full_$timestamp.bak"
    $sql = "BACKUP DATABASE [$Database] TO DISK = N'$dbBackupFile' WITH FORMAT, INIT, COMPRESSION, STATS = 10"
    Invoke-Sqlcmd -Query $sql -ServerInstance $ServerInstance -QueryTimeout 300
    Write-Log "Database backup completed: $dbBackupFile"

    # 2. 备份配置文件
    $apiDir = "C:\Services\LYBT-API"
    if (Test-Path "$apiDir\appsettings.json") {
        Copy-Item "$apiDir\appsettings.json" "$configDir\appsettings_$timestamp.json"
        Copy-Item "$apiDir\appsettings.Production.json" "$configDir\appsettings.Production_$timestamp.json" -ErrorAction SilentlyContinue
        Write-Log "Config files backed up"
    }

    # 3. 清理过期备份
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem $backupDir -Filter "*.bak" | Where-Object { $_.CreationTime -lt $cutoffDate } | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Log "Removed expired backup: $($_.Name)"
    }

    # 4. 验证最新备份完整性
    $verifySql = "RESTORE VERIFYONLY FROM DISK = N'$dbBackupFile'"
    Invoke-Sqlcmd -Query $verifySql -ServerInstance $ServerInstance -QueryTimeout 60
    Write-Log "Backup verification passed"

    Write-Log "Backup job completed successfully"
} catch {
    Write-Log "Backup job failed: $($_.Exception.Message)" -Level "ERROR"
    exit 1
}
```

注册每日任务计划：

```powershell
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-ExecutionPolicy Bypass -File C:\Scripts\lybt-backup.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 2)
Register-ScheduledTask -TaskName "LYBT Daily Backup" -Action $action -Trigger $trigger `
    -Settings $settings -User "SYSTEM" -RunLevel Highest
```

### 7. 异地备份策略

| 层级 | 方式 | 频率 | 保留 |
|------|------|------|------|
| 本地 | `D:\Backup\` 目录 | 每日 | 7 天 |
| 异地 | 网络共享 / NAS | 每周 | 4 周 |
| 可选 | 云存储（Azure Blob / 阿里 OSS） | 每周 | 30 天 |

```powershell
# 异地备份脚本 — 将本周全量备份复制到网络共享
$source = "D:\Backup\database"
$dest = "\\NAS\LYBT-Backups\database"
$weekStart = (Get-Date).AddDays(-(Get-Date).DayOfWeek.value__)
Get-ChildItem $source -Filter "*.bak" | Where-Object { $_.CreationTime -ge $weekStart } | ForEach-Object {
    Copy-Item $_.FullName $dest -Force
}
```

---

## 客户端备份（本地模式 / LocalDB）

### 1. 应用内备份（唯一推荐路径）

桌面客户端的本地库由**嵌入式 LocalWebAPI** 通过同一 `/api/v1/backup` 契约备份/恢复（`IBackupService` 引擎对本机 LocalDB 执行 T-SQL `BACKUP DATABASE`）。UI 入口：sysadmin → 备份恢复（`BackupManagementView`）。

| 项 | 值 |
|----|-----|
| 备份目录 | `Backup:Directory`；未配置时默认 `%LOCALAPPDATA%\LYBT\Desktop\Backup`（`AppDataPaths.DesktopDataDirectory` 的 `Backup` 子目录，**位于应用安装目录之外**——Velopack 更新/卸载不会清理） |
| 自动备份 | 登录成功后 fire-and-forget `POST /api/v1/backup/auto`；**受 24 小时间隔判定**（`Backup:AutoBackup:IntervalHours`），未满间隔为空操作 |
| 保留期 | `Backup:RetentionDays`（默认 7 天）；清理会保护最新全量备份及其差异链 |
| 可选加密 | `Encrypt=true` + 口令（请求参数 → `Backup:EncryptionPassword`），文件为 `*.bak.enc` |
| 文件清单 | 每个备份旁挂 `{文件名}.manifest.json`（稳定 Id/类型/大小/压缩/加密/库名/差异基准） |

配置示例（`appsettings.json`）：

```json
{
  "Backup": {
    "Directory": "",              // 空 = 默认 %LOCALAPPDATA%\LYBT\Desktop\Backup
    "RetentionDays": 7,
    "CompressByDefault": true,
    "EncryptByDefault": false,
    "EncryptionPassword": "",
    "AutoBackup": {
      "Enabled": false,           // 桌面靠登录触发；宿主调度默认关
      "IntervalHours": 24,
      "Kind": "Full",
      "Encrypt": false,
      "InitialDelayMinutes": 2
    }
  }
}
```

**手动触发**：UI「立即备份」按钮（可全量/差异，可选加密），或调用 `POST /api/v1/backup`。

> **不要复制 `.mdf`**：LocalDB 由实例管理数据/日志文件，直接复制不可靠。历史文档中的 `%APPDATA%\LYBT\data\lybt-local.mdf` 路径与 `Sync:AutoBackup{Enabled,MaxBackups}` 配置**均不存在于代码**（同步模块延期至 v2.0），已废止；本地库名取连接串 `InitialCatalog`（开发内嵌宿主默认 `LYBTDesktop`，LocalWebAPI 独立宿主配置为 `LYBTDB_Local`）。

### 2. Desktop 用户数据备份

```text
%LOCALAPPDATA%\LYBT\Desktop\            # 用户数据根（凭据/照片/系统设置/首次运行标记）
%LOCALAPPDATA%\LYBT\Desktop\Backup\     # 本地数据库备份（.bak / .bak.enc + *.manifest.json）
```

迁移到新机器时复制整个 `%LOCALAPPDATA%\LYBT\Desktop\` 目录即可（含备份文件；数据库本体仍建议经 `POST /api/v1/backup/{id}/restore` 恢复而非拷贝文件）。

---

## 恢复流程

> ⚠️ **服务管理命令适用环境**：下方 `sc stop/start LYBT-API` 适用于 Windows Server 2016+。**Server 2012 R2 禁用 `sc.exe`**（SCM 1053 超时），须改用 `schtasks /end /run /tn LYBT-API`，详见 [01](./01-deployment.md) / [05](./04-development-environment-spec.md)。

### 场景 1：服务端数据库恢复

**适用**：数据库损坏、误操作、数据丢失。

**路径 A：应用内恢复（推荐，B-06）**

```
1. 以 sysadmin 登录
   → GET /api/v1/backup            列出备份，取目标 Id
   → GET /api/v1/backup/tables      （选择性恢复时才需要）

2. 执行恢复（整库，默认恢复前自动备份当前数据）
   → POST /api/v1/backup/{id}/restore
     { "mode": "Full", "createPreRestoreBackup": true }

3. 重启服务/应用
   → 服务端：sc stop LYBT-API → sc start LYBT-API（或 IIS 回收应用池）
   → 桌面：关闭并重新启动客户端（恢复完成后必须重启，见 UI 提示）

4. 验证系统健康
   → GET /api/v1/health/details（应返回 Healthy）
```

> **选择性恢复（`"mode": "Selective"`）**：先还原到临时库 `<db>_LYBT_SELRESTORE`，再按 `tables[].tableName`（仅支持含 `Id` 列的表）与可选 `tables[].ids` 回写当前库。回写期间**临时禁用全部外键**并在结束时以 `WITH NOCHECK` 重新启用——**约束不做校验**，若返回警告请执行 `DBCC CHECKCONSTRAINTS` 复核。恢复完成后同样需重启应用。

**路径 B：手工 SQL（应用不可用/离线场景）**

```
1. 停止 WebAPI 服务
   → sc stop LYBT-API （或 IIS 停止应用池）

2. 确认备份文件可用
   → RESTORE VERIFYONLY FROM DISK = N'<备份路径>'
   （加密备份为 *.bak.enc，需先经应用内解密或改用应用内恢复）

3. 恢复数据库（覆盖现有）
   → RESTORE DATABASE [LYBTDB_Dev] FROM DISK = N'<备份路径>' WITH REPLACE
   （差异备份：先还原基准全量 WITH NORECOVERY，再差异 WITH RECOVERY）

4. 验证数据完整性
   → DBCC CHECKDB ([LYBTDB_Dev])

5. 启动 WebAPI 服务
   → sc start LYBT-API

6. 验证系统健康
   → GET /api/v1/health/details（应返回 Healthy）
```

### 场景 2：客户端 LocalDB 恢复

**适用**：本地数据丢失、LocalDB 损坏、机器更换。

```
1. 以 sysadmin 登录桌面客户端（本地模式）
   → 打开 备份恢复 页（BackupManagementView），或调用本地 /api/v1/backup

2. 选择备份并恢复
   → POST /api/v1/backup/{id}/restore
     { "mode": "Full", "createPreRestoreBackup": true }
   → 如需只回写个别表：先 GET /api/v1/backup/tables 取表清单，
     再以 { "mode": "Selective", "tables": [ { "tableName": "...", "ids": [...] } ] } 恢复
     （注意：外键以 WITH NOCHECK 重启、不校验，需人工复核）

3. 重启客户端
   → 恢复完成后关闭并重新启动应用（引擎需独占单用户连接与连接池重置）

4. 验证
   → 启动客户端 → 确认患者列表和医案数据完整
```

> 备份文件位于 `%LOCALAPPDATA%\LYBT\Desktop\Backup`（或 `Backup:Directory` 指定目录）。
> 不要用手工复制 `.mdf` 的方式恢复本地库。

### 场景 3：服务端完全重建

**适用**：硬件故障、操作系统重装、迁移到新服务器。

```
1. 在新服务器安装前置条件
   → .NET 8 Runtime
   → SQL Server 2019+ (或 Express)
   → 确保 5000/5001 端口可用

2. 恢复数据库
   → 创建 LYBTDB_Dev 数据库
   → RESTORE DATABASE FROM DISK（见场景 1）

3. 恢复配置文件
   → 复制备份的 appsettings.json 和 appsettings.Production.json
   → 检查连接字符串指向恢复后的数据库

4. 部署 WebAPI
   → dotnet publish 产出复制到 C:\Services\LYBT-API\
   → 运行 deploy.ps1 注册 Windows Service

5. 恢复 Desktop 发布包
   → 复制 C:\Services\LYBT-releases\ 目录

6. 启动并验证
   → sc start LYBT-API
   → GET /api/v1/health/details
   → Desktop 客户端连接测试
```

### 场景 4：误删数据恢复

**适用**：误删患者、药材、验方等（均为软删除）。

```
1. 通过 API 恢复（推荐）
   → POST /api/v1/patients/{id}/restore    # 患者恢复
   → POST /api/v1/herbs/{id}/restore       # 药材恢复
   → POST /api/v1/formulas/{id}/restore    # 验方恢复
   → POST /api/v1/users/{id}/restore       # 用户恢复

   注意：MedicalCase 目前无恢复端点（系统限制）

2. 通过数据库直接恢复（紧急）
    → UPDATE Entities SET IsDeleted = 0, UpdatedAt = GETUTCDATE() WHERE Id = '<GUID>'
    → 需 SuperAdmin 权限，务必记录操作日志
```

### 恢复后验证清单

恢复操作完成后，按以下清单逐项验证：

```powershell
# 1. 数据库完整性
DBCC CHECKDB ([LYBTDB_Dev]) WITH NO_INFOMSGS, ALL_ERRORMSGS
# 期望：无错误输出

# 2. 关键表记录数
$tables = @("Patients", "Herbs", "Formulas", "MedicalCases", "Users")
foreach ($t in $tables) {
    $count = (Invoke-Sqlcmd -Query "SELECT COUNT(*) AS Cnt FROM [$t]" -Database LYBTDB_Dev).Cnt
    Write-Host "$t : $count rows"
}

# 3. 健康检查
$health = Invoke-RestMethod -Uri "http://localhost:5000/health/details" -Method Get
Write-Host "Status: $($health.status)"

# 4. 登录测试
$login = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
    -Method Post -ContentType "application/json" `
    -Body '{"username":"admin","password":"Admin@123456"}'
Write-Host "Login: $($login.success)"

# 5. API 功能抽查
$token = $login.token
$headers = @{ Authorization = "Bearer $token" }
$patients = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/patients?page=1&pageSize=5" -Headers $headers
Write-Host "Patients returned: $($patients.data.Count)"
```

---

## 灾难恢复 RTO/RPO

| 场景 | RTO | RPO | 恢复方式 |
|------|-----|-----|----------|
| 数据库损坏 | < 1 小时 | < 24 小时 | 最近全量备份恢复（应用内 `POST /api/v1/backup/{id}/restore`；亦见场景 1 路径 B 手工 SQL） |
| 服务器硬件故障 | < 4 小时 | < 24 小时 | 新服务器重建 + 备份恢复 |
| 客户端数据丢失 | < 30 分钟 | < 24 小时 | 应用内恢复本地备份（`%LOCALAPPDATA%\LYBT\Desktop\Backup`）；无备份时重建 |
| 误删单条记录 | < 5 分钟 | 0 | API restore 端点（软删）或选择性恢复 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
| 2026-06-25 | v1.1 | 确认远程备份保留 7 天（与 NFR 一致）；确认 RTO < 1 小时 |
| 2026-06-25 | v1.2 | 新增 PowerShell 自动备份脚本、异地备份策略、恢复后验证清单 |
| 2026-09-22 | v1.3 | **B-06 应用内备份/恢复落地同步**：① 新增「服务端备份 §0 应用内备份/恢复」——双端同路由 `/api/v1/backup` 8 端点、`Backup` 配置节全键表、登录触发 + 24 小时间隔的自动备份语义、备份类型/清单/加密；② 客户端备份章节重写——**删除不存在的 `Sync:AutoBackup{Enabled,MaxBackups}` 配置与 `%APPDATA%\LYBT\data\*.mdf` 路径**，改述应用内链路（`Backup:Directory` 默认 `%LOCALAPPDATA%\LYBT\Desktop\Backup`、`Backup:RetentionDays`、`Backup:AutoBackup:*`）并明确「不要复制 .mdf」；③ 恢复流程——场景 1 拆为「路径 A 应用内恢复（含选择性恢复外键 `WITH NOCHECK` 不校验 + `DBCC CHECKCONSTRAINTS` 提示）」与「路径 B 手工 SQL」，场景 2 由「替换 .mdf」改为应用内恢复 + **恢复后必须重启应用**；④ 备份范围表与 RTO/RPO 表按新引擎修正（客户端数据丢失 RPO <1 天 → <24 小时） 原客户端章节描述的是从未实现的同步模块备份与虚假数据文件路径（`Sync:AutoBackup{Enabled,MaxBackups}`）——B-06 交付后应用层具备真实、可自助的备份/恢复能力，运维文档必须指向它 |
