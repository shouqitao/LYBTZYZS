# LYBT.Desktop.LocalData

> 本地 SQLite 数据层，离线模式核心基础设施

## 项目定位

- **层级**: Desktop Core (基础设施层)
- **职责**: 提供基于 SQLite + EF Core 的本地数据存储，支持离线模式下的完整数据操作，包括本地认证、数据同步、种子数据初始化
- **状态**: Active

## 目录结构

```
LYBT.Desktop.LocalData/
├── Context/               # LocalDbContext (SQLite DbContext)
├── DataSources/           # 本地数据源 (Herb/Formula/MedicalCase/Patient/User)
├── Helpers/               # 工具类 (ChecksumHelper)
├── Initialization/        # 数据库初始化与种子数据
├── Mappers/               # Mapperly 实体-DTO 映射器
└── Services/              # 本地认证与同步服务
```

## 核心组件

| 名称 | 说明 |
|------|------|
| LocalDbContext | SQLite DbContext，管理 10 个 DbSet，处理软删除过滤、decimal 转换、RowVersion 忽略 |
| LocalAuthService | 本地 BCrypt 密码认证，支持登录失败锁定 (5次/15分钟) |
| SyncService | 本地-服务器数据同步协调，基于 Checksum 的增量同步 |
| DatabaseInitializer | SQLite 数据库初始化，确保数据库创建与 Schema 同步 |
| SeedData | 种子数据填充，提供初始基础数据 |
| ChecksumHelper | 数据校验和计算，用于同步时检测数据变更 |
| Local*DataSource (x5) | 五个业务实体的本地数据源，实现 IDataSource 接口 |
| Local*Mapper (x5) | 基于 Mapperly 的编译时映射器，Entity 与 DTO 之间转换 |

## 设计依据

本项目是双模式架构 (SYNC-D02) 的本地模式实现。远程模式通过 API 访问 SQL Server，本地模式通过 SQLite 实现离线数据操作。两种模式共享 Service/Repository 层，仅 DbContext Provider 不同。

SQLite 适配处理了三个关键差异：
- 软删除全局查询过滤器 (ISoftDeletable)
- RowVersion 忽略 (SQLite 不支持并发令牌)
- decimal 到 double 的值转换 (SQLite 不原生支持 decimal)

## 依赖关系

### 依赖
- Microsoft.EntityFrameworkCore.Sqlite - SQLite 数据库引擎
- BCrypt.Net-Next - 密码哈希验证
- Riok.Mapperly - 编译时对象映射
- LYBT.Entities - 领域实体定义
- LYBT.Shared.Models - 共享 DTO 模型
- LYBT.Shared.Validators - 共享验证规则
- LYBT.Shared.Configuration - 同步选项等配置
- LYBT.Desktop.Contracts - 服务接口契约 (ISyncService, ILocalAuthService)

### 被依赖
- LYBT.Desktop.Shell - 主程序组合根
- LYBT.Tests.Desktop.Unit - 单元测试
- LYBT.Tests.Desktop.Integration - 集成测试

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |
