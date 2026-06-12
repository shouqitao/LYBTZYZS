# 备份与恢复指南

本文档定义 LYBT 系统的备份策略、恢复流程和灾难恢复方案，覆盖服务端 SQL Server 数据库、客户端 LocalDB 数据和服务端配置文件。

> 部署架构见 [README.md](./README.md)；数据库运维见 [01-deployment.md](./01-deployment.md)。

---

## 备份范围

| 备份对象 | 位置 | 优先级 | 备份频率 |
|----------|------|--------|----------|
| SQL Server 生产数据库 | 服务端 SQL Server 实例 | **关键** | 每日全量 |
| LocalDB 本地数据 | `%APPDATA%\LYBT\data\lybt-local.mdf` | 高 | 每次同步后 |
| 服务端配置 | `appsettings.json` + `appsettings.Production.json` | 高 | 变更时 |
| Desktop 配置 | `%APPDATA%\LYBT\config\` | 中 | 变更时 |
| 服务端日志 | `logs/` + `SystemLogs` 表 | 低 | 30天轮转（自动） |
| Desktop 发布包 | `C:\Services\LYBT-releases\` | 中 | 版本发布时 |

---

## 服务端备份

### 1. SQL Server 全量备份

```sql
-- 手动全量备份
BACKUP DATABASE [LYBT_DB]
TO DISK = N'D:\Backup\LYBT_DB_full_20260612.bak'
WITH FORMAT, INIT,
     NAME = N'LYBT_DB-Full Backup',
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
      DECLARE @path NVARCHAR(500) = N''D:\Backup\LYBT_DB_full_'' + CONVERT(NVARCHAR(8), GETDATE(), 112) + N''.bak'';
      BACKUP DATABASE [LYBT_DB] TO DISK = @path WITH COMPRESSION, INIT;
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
RESTORE VERIFYONLY FROM DISK = N'D:\Backup\LYBT_DB_full_20260612.bak';
```

### 5. 配置文件备份

```powershell
# 每次配置变更后备份
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\Services\LYBT-API\appsettings.json" "D:\Backup\config\appsettings_$timestamp.json"
Copy-Item "C:\Services\LYBT-API\appsettings.Production.json" "D:\Backup\config\appsettings.Production_$timestamp.json"
```

---

## 客户端备份

### 1. LocalDB 数据文件

客户端本地数据存储在 SQL Server LocalDB 的 `.mdf` 文件中：

```
%APPDATA%\LYBT\data\lybt-local.mdf    # 数据库主文件
%APPDATA%\LYBT\data\lybt-local.ldf    # 日志文件
```

**自动备份**：同步模块在每次成功同步后自动创建备份（配置于 `appsettings.json`）：

```json
{
  "Sync": {
    "AutoBackup": {
      "Enabled": true,
      "MaxBackups": 5
    }
  }
}
```

**手动备份**：

```powershell
# 关闭 Desktop 客户端后复制
$timestamp = Get-Date -Format "yyyyMMdd"
Copy-Item "$env:APPDATA\LYBT\data\lybt-local.mdf" "$env:APPDATA\LYBT\data\backup\lybt-local_$timestamp.mdf"
```

### 2. Desktop 配置备份

```
%APPDATA%\LYBT\config\appsettings.json    # 客户端配置
%APPDATA%\LYBT\config\connection.json     # 连接设置
```

迁移到新机器时复制整个 `%APPDATA%\LYBT\` 目录即可。

---

## 恢复流程

### 场景 1：服务端数据库恢复

**适用**：数据库损坏、误操作、数据丢失。

```
1. 停止 WebAPI 服务
   → sc stop LYBT-WebAPI （或 IIS 停止应用池）

2. 确认备份文件可用
   → RESTORE VERIFYONLY FROM DISK = N'<备份路径>'

3. 恢复数据库（覆盖现有）
   → RESTORE DATABASE [LYBT_DB] FROM DISK = N'<备份路径>' WITH REPLACE

4. 验证数据完整性
   → DBCC CHECKDB ([LYBT_DB])

5. 启动 WebAPI 服务
   → sc start LYBT-WebAPI

6. 验证系统健康
   → GET /api/v1/health/details（应返回 Healthy）
```

### 场景 2：客户端 LocalDB 恢复

**适用**：本地数据丢失、LocalDB 损坏、机器更换。

```
1. 关闭 Desktop 客户端

2. 替换数据文件
   → 复制备份的 .mdf 文件到 %APPDATA%\LYBT\data\lybt-local.mdf

3. 如无备份但有远程同步历史
   → 启动客户端 → 切换到远程模式 → 执行同步 → 数据从服务端下载

4. 验证
   → 启动客户端 → 确认患者列表和医案数据完整
```

### 场景 3：服务端完全重建

**适用**：硬件故障、操作系统重装、迁移到新服务器。

```
1. 在新服务器安装前置条件
   → .NET 8 Runtime
   → SQL Server 2019+ (或 Express)
   → 确保 5000/5001 端口可用

2. 恢复数据库
   → 创建 LYBT_DB 数据库
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
   → sc start LYBT-WebAPI
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

---

## 灾难恢复 RTO/RPO

| 场景 | RTO | RPO | 恢复方式 |
|------|-----|-----|----------|
| 数据库损坏 | < 1 小时 | < 24 小时 | 最近全量备份恢复 |
| 服务器硬件故障 | < 4 小时 | < 24 小时 | 新服务器重建 + 备份恢复 |
| 客户端数据丢失 | < 30 分钟 | < 1 天 | 同步下载或备份文件替换 |
| 误删单条记录 | < 5 分钟 | 0 | API restore 端点 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
