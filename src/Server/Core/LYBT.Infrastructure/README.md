# LYBT.Infrastructure

> Server端基础设施层 | 数据访问/配置/安全/缓存

## 项目定位

- **层级**: Server Core层
- **职责**: 为所有业务模块提供数据库访问、配置管理、安全服务、缓存等底层能力

## 目录结构

```
LYBT.Infrastructure/
├── Data/                    # 数据访问核心
│   ├── AppDbContext.cs      # 统一数据库上下文
│   ├── Configurations/      # EF Core实体配置
│   └── Migrations/          # 数据库迁移
├── Repositories/            # 仓储实现
│   ├── IBaseRepository.cs
│   └── BaseRepository.cs
├── Configuration/           # 配置管理
│   └── Options/             # 配置选项类
├── Caching/                 # 缓存服务
├── Security/                # 安全服务
└── Web/                     # Web API基类
```

## 核心组件

| 组件 | 说明 |
|------|------|
| AppDbContext | 统一数据库上下文，管理所有实体DbSet |
| BaseRepository<T> | 泛型仓储基类，提供CRUD操作 |
| DatabaseOptions | 数据库连接配置 |
| JwtOptions | JWT认证配置 |
| CacheOptions | 缓存策略配置 |

## 配置选项

| 选项类 | 说明 |
|--------|------|
| DatabaseOptions | ConnectionString、CommandTimeout、MaxRetryCount |
| JwtOptions | SecretKey、Issuer、Audience、ExpiryInHours |
| CacheOptions | DefaultExpirationMinutes、MaxMemoryUsageMB |

## 扩展点

| 扩展点 | 用途 |
|--------|------|
| IBaseRepository<T> | 业务模块自定义Repository继承 |
| BaseApiController | 业务Controller继承基类 |
| IEntityTypeConfiguration | 实体配置扩展 |

## 依赖关系

### 依赖
- LYBT.Entities (数据实体定义)
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.Extensions.*

### 被依赖
- 所有Server业务模块
- LYBT.WebAPI

## EF Core迁移命令

```bash
# 添加迁移
dotnet ef migrations add MigrationName \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI

# 应用迁移
dotnet ef database update \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-09-02 | JWT Token存储安全增强 |
| 2025-08-11 | 性能索引优化 |
| 2025-08-10 | Auth模块重构 |
