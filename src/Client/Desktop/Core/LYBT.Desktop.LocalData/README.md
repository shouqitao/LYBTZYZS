# LYBT.Desktop.LocalData

本地数据访问层模块，基于 EF Core + LocalDB 实现离线数据存储，镜像服务端数据结构并提供本地认证与同步支持。

## 项目定位

桌面客户端的本地持久化层，在无法连接远程服务器时提供完整的数据读写能力。通过 EF Core 映射 10 个 DbSet 镜像服务端模型，支持软删除、审计字段自动填充、SHA256 校验和同步冲突检测。

## 目录结构

```
LYBT.Desktop.LocalData/
├── Data/
│   └── LocalDbContext.cs            # EF Core DbContext，10 个 DbSet
├── Initializers/
│   └── DatabaseInitializer.cs       # 线程安全的数据库初始化
├── Services/
│   ├── LocalAuthService.cs          # 本地认证（BCrypt + 锁定策略）
│   └── ChecksumHelper.cs            # SHA256 同步校验
├── Mappers/
│   ├── LocalPatientMapper.cs        # Riok.Mapperly 编译期映射
│   ├── LocalHerbMapper.cs
│   ├── LocalFormulaMapper.cs
│   ├── LocalMedicalCaseMapper.cs
│   ├── LocalUserMapper.cs
│   └── LocalRegistrationMapper.cs
└── LocalDataModule.cs               # Prism 模块注册
```

## 核心组件

### LocalDbContext — 本地数据库上下文

**设计依据**：EF Core DbContext，10 个 DbSet 镜像服务端 `AppDbContext` 表结构，保证离线/在线数据模型一致。

| 特性 | 说明 |
|------|------|
| DbSet 数量 | 10 个（Patient, Herb, Formula, MedicalCase, User, Registration 等） |
| 软删除 | `IsDeleted` 全局查询过滤器，`SaveChanges` 自动拦截 |
| 审计字段 | `CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy` 自动填充 |
| 数据库 | LocalDB（`LYBTDesktop`），非 SQLite |

### DatabaseInitializer — 数据库初始化器

**设计依据**：线程安全的幂等初始化，防止并发启动时重复执行迁移。

| 机制 | 说明 |
|------|------|
| SemaphoreSlim | 互斥锁，保证单线程执行初始化 |
| 双重检查锁定 | 外层 `_isInitialized` 快速路径，内层锁内二次确认 |
| 迁移策略 | `MigrateAsync()` 幂等迁移，支持重试 |

### LocalAuthService — 本地认证服务

**设计依据**：离线场景下的身份验证，BCrypt 哈希比对，带账户锁定策略防止暴力破解。

| 方法 | 签名 | 说明 |
|------|------|------|
| ValidateCredentials | `Task<LocalUser?> ValidateCredentialsAsync(string username, string password)` | 验证用户名密码 |
| 认证方式 | BCrypt | 与服务端 PasswordHelper 一致 |
| 锁定策略 | 5 次失败 → 锁定 15 分钟 | `FailedLoginAttempts` + `LockoutEnd` |

### ChecksumHelper — 同步校验工具

**设计依据**：SHA256 计算实体哈希值，用于同步时检测冲突。排除审计字段（`UpdatedAt` 等），避免时间戳变化导致误判冲突。

| 方法 | 签名 | 说明 |
|------|------|------|
| ComputeChecksum | `string ComputeChecksum<T>(T entity)` | 对实体序列化后计算 SHA256 |
| 排除字段 | `UpdatedAt`, `UpdatedBy`, `CreatedAt`, `CreatedBy` | 审计字段不参与哈希计算 |

### Mappers — Mapperly 编译期映射

**设计依据**：全部使用 Riok.Mapperly 编译期代码生成，零运行时反射开销。忽略审计字段，仅映射业务数据。

| Mapper | 源 → 目标 | 说明 |
|--------|-----------|------|
| LocalPatientMapper | Server ↔ Local | 患者数据映射 |
| LocalHerbMapper | Server ↔ Local | 中药数据映射 |
| LocalFormulaMapper | Server ↔ Local | 验方数据映射 |
| LocalMedicalCaseMapper | Server ↔ Local | 医案数据映射 |
| LocalUserMapper | Server ↔ Local | 用户数据映射 |
| LocalRegistrationMapper | Server ↔ Local | 挂号数据映射 |

共性：所有 Mapper 均忽略审计字段（`CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`），仅映射业务属性。

## 依赖关系

| 依赖 | 用途 |
|------|------|
| Microsoft.EntityFrameworkCore | ORM 框架 |
| Microsoft.EntityFrameworkCore.SqlServer | LocalDB 驱动 |
| BCrypt.Net-Next | 密码哈希验证 |
| Riok.Mapperly | 编译期对象映射 |
| Prism.Modularity | IModule 模块注册 |
| LYBT.Shared.Models | 实体/DTO 定义 |

## 设计决策

1. **镜像服务端结构**：10 个 DbSet 与 `AppDbContext` 保持一致，降低同步时的映射复杂度
2. **LocalDB 而非 SQLite**：与服务端同为 SQL Server 生态，EF Core 迁移脚本可复用
3. **全局查询过滤器**：软删除实体自动过滤，`IgnoreQueryFilters()` 用于恢复操作
4. **BCrypt 而非 PBKDF2**：桌面端本地认证使用 BCrypt（`PasswordHelper`），服务端 Identity 使用 PBKDF2，两者不互通
5. **编译期映射**：Mapperly 在编译时生成映射代码，避免 AutoMapper 的运行时反射和启动开销
6. **Checksum 排除审计字段**：同步冲突检测只关注业务数据变化，时间戳变化不触发冲突

## 已知陷阱

- **LocalDB 实例名**：连接字符串中的 `(localdb)\MSSQLLocalDB` 需确保 LocalDB 已安装并启动，否则初始化静默失败
- **BCrypt vs PBKDF2**：本地认证和远程认证使用不同哈希算法，密码不能直接跨端使用
- **软删除恢复**：恢复已删除实体必须用 `IgnoreQueryFilters()`，否则 `FindAsync` 找不到
- **审计字段自动填充**：`SaveChanges` 重写中设置审计字段，手动赋值会被覆盖
- **迁移并发**：`DatabaseInitializer` 的 SemaphoreSlim 仅限进程内，多进程同时启动 LocalDB 迁移可能冲突
