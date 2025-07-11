## AGENTS.md — 基础设施模块（LYBT.Infrastructure）

### 1. Agent 概述

基础设施模块为全系统提供数据库访问、数据初始化、设计时上下文、仓储注入等底层支撑，是所有业务模块持久化的核心桥梁。

### 2. 核心能力

- 统一数据访问（基于 Entity Framework），包含 AppDbContext 的所有实体表映射与关系配置
- 提供设计时 DbContextFactory，支持 EF migrations 和 CLI 工具
- 系统初始化工具（如超级管理员账户种子 AdminSeeder）
- 仓储注册扩展与基础依赖注入（Repository 自动注入所有模块的仓储实现）

### 3. 输入输出规范

#### 输入

- 业务层通过依赖注入获得 DbContext 或仓储接口
- 数据库连接字符串、迁移参数等配置项

#### 输出

- 所有数据模型（Model）在此层自动映射为数据库表
- 通过仓储层返回查询结果、变更结果（新增、更新、删除等）

### 4. 协作与依赖模块

- **全部业务模块**：通过注入基础设施的 DbContext 或 Repository 进行数据操作
- **数据模型模块**：引用实体 Model 定义映射关系
- **通用模块**：字段映射依赖枚举/常量定义
- **日志/同步等模块**：日志与同步任务表同样由基础设施负责数据落库

### 5. 示例场景

#### EF Core 数据迁移

```csharp
// 控制台自动迁移数据库结构
var context = dbFactory.CreateDbContext(args);
context.Database.Migrate();
```

#### 按模块自动注入仓储

```csharp
services.AddModuleRepositories(); // 基础设施自动注册所有业务仓储
```

#### 数据初始化

```csharp
await AdminSeeder.SeedAsync(context); // 启动时自动生成初始超级管理员
```

### 6. 主要类型与工具

- `AppDbContext`：EF Core 上下文，映射全部实体
- `AppDbContextFactory`：设计时工厂，支持迁移与测试
- `AdminSeeder`：系统初始化超级管理员
- `Repository<T>`：通用仓储基类
- 仓储自动注入扩展方法 `AddModuleRepositories`

