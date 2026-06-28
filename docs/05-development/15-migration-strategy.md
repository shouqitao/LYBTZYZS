# 数据库迁移策略

## 迁移工具

- **EF Core Migrations** — Code-First 迁移
- **命令**：`dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`

## 迁移原则

1. **幂等性** — `MigrateAsync()` 自动应用所有待执行迁移，重复调用安全
2. **非破坏性** — 新增列/表可回滚；删除列/表需谨慎
3. **向后兼容** — 迁移必须支持旧版本客户端过渡期

## 迁移命名规范

```
<日期>-<描述>.cs
例：2026-06-28-AddAuditLogFields.cs
```

## 当前迁移状态

| 迁移名 | 日期 | 说明 |
|--------|------|------|
| AddIsSysAdmin | 2026-06-28 | 添加 Sysadmin 支持 |
| SimplifyDataModel | 2026-06-16 | 数据模型简化（删除审计实体） |

## 双模式迁移

| 模式 | 迁移方式 | 说明 |
|------|---------|------|
| 远程 | `MigrateAsync()` | 服务启动时自动执行 |
| 本地 | `MigrateAsync()` | LocalDB 启动时自动执行 |

## 回滚策略

| 场景 | 策略 | 操作 |
|------|------|------|
| 新增列 | DROP COLUMN | 可直接回滚 |
| 新增表 | DROP TABLE | 可直接回滚 |
| 删除列 | 备份数据 | 需手动恢复 |
| 数据转换 | 备份表 | 需手动恢复 |

## 重置数据库

```bash
# 远程
sqlcmd -S "localhost" -Q "DROP DATABASE IF EXISTS LYBTDB_Dev; CREATE DATABASE LYBTDB_Dev"

# 本地
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS LYBTDesktop"

# 重启服务自动迁移
```

## 测试环境

- **Server 测试**：真实 SQL Server + Respawn（每次测试重置）
- **Desktop 测试**：SQL Server LocalDB（每次测试独立事务/连接）
- **Architecture 测试**：编译时检查，无数据库
