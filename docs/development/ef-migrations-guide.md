# EF Core Migrations 使用指南

## 概述

本项目使用 Entity Framework Core Migrations 管理数据库架构变更。所有数据库结构修改都应通过 Migrations 进行，避免手动 SQL 脚本。

## 项目配置

- **DbContext 位置**：`src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
- **Migrations 目录**：`src/Server/Core/LYBT.Infrastructure/Migrations/`
- **启动项目**：`src/Server/Services/LYBT.WebAPI/`
- **数据库初始化**：`DatabaseInitializationService` 使用 `MigrateAsync()` 自动应用待执行的迁移

## 常用命令

### 1. 创建新迁移

修改实体模型后，创建迁移：

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI `
  --context AppDbContext
```

**命名规范**：
- 使用英文 PascalCase 命名
- 描述性名称，说明变更内容
- 示例：`AddUserEmailIndex`、`FixUserNamePropertyTypo`

### 2. 移除最后一次迁移

如果创建错误，可以移除未应用的迁移：

```powershell
dotnet ef migrations remove `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI `
  --context AppDbContext
```

⚠️ **注意**：只能移除尚未应用到数据库的迁移。

### 3. 查看待应用的迁移

```powershell
dotnet ef migrations list `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI `
  --context AppDbContext
```

### 4. 手动应用迁移（开发/测试环境）

```powershell
dotnet ef database update `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI `
  --context AppDbContext
```

### 5. 生成 SQL 脚本（生产环境）

为生产环境生成 SQL 脚本供 DBA 审查：

```powershell
dotnet ef migrations script `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI `
  --context AppDbContext `
  --output scripts/migrations/<MigrationName>.sql
```

## 工作流程

### 开发环境

1. **修改实体模型**（如 `User.cs`）
2. **创建迁移**：`dotnet ef migrations add <Name>`
3. **启动应用**：迁移自动应用（通过 `MigrateAsync()`）
4. **验证数据库**：检查表结构是否符合预期

### 生产环境

1. **生成 SQL 脚本**：`dotnet ef migrations script`
2. **提交脚本供审查**
3. **DBA 执行脚本**
4. **部署应用程序**

## 自动迁移逻辑

应用启动时，`DatabaseInitializationService` 会：

```csharp
// src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs
public async Task InitializeDatabaseAsync()
{
    _logger.LogInformation("开始初始化数据库并应用迁移");

    // 自动应用所有待执行的迁移
    await _context.Database.MigrateAsync();

    _logger.LogInformation("数据库初始化完成，所有迁移已应用");
}
```

这确保了：
- 开发环境：每次启动自动同步数据库架构
- 测试环境：CI/CD 自动应用迁移
- 生产环境：可选择自动或手动应用

## 注意事项

### ✅ 最佳实践

1. **每次变更创建一个迁移**：保持迁移历史清晰
2. **审查生成的代码**：检查 `Up()` 和 `Down()` 方法
3. **测试回滚**：确保 `Down()` 方法正确
4. **避免破坏性变更**：谨慎删除列或表
5. **使用事务**：EF 默认在事务中应用迁移

### ❌ 避免的做法

1. **不要手动修改迁移文件**（除非必要）
2. **不要删除已应用的迁移**
3. **不要混用 `EnsureCreated()` 和 `Migrate()`**
4. **不要在生产环境直接执行 `database update`**

## 故障排查

### 问题：迁移失败

```
An error occurred using the connection to database 'LYBTDB' on server 'localhost'.
```

**解决方法**：
1. 检查连接字符串（`appsettings.json`）
2. 确认数据库服务器正在运行
3. 验证权限（需要 DDL 权限）

### 问题：迁移冲突

```
There is already an object named 'Users' in the database.
```

**解决方法**：
1. 使用 `dotnet ef migrations script` 生成脚本
2. 手动检查并调整冲突部分
3. 或回滚数据库到干净状态

### 问题：模型与数据库不一致

```
The model backing the 'AppDbContext' context has changed since the database was created.
```

**解决方法**：
创建新迁移以同步模型变更：

```powershell
dotnet ef migrations add SyncModelChanges `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI
```

## 相关资源

- [EF Core Migrations 官方文档](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [项目架构文档](../architecture/README.md)
- [数据库设计文档](../architecture/database/README.md)

---

**最后更新**：2025-10-01
**维护者**：开发团队
