# 架构治理规则

## 概述

本文档定义 LYBT 中医诊所管理系统的架构治理规则和约束，确保系统架构符合设计原则和业务需求。

## 核心原则

### 1. Record-Only System（记录型系统）
- **定义**：系统仅用于记录和查询中医诊所的业务数据
- **约束**：
  - 禁止引入复杂的业务工作流引擎
  - 禁止引入事件驱动架构（Event Sourcing/CQRS）
  - 禁止引入 AI/ML 集成
  - 禁止引入分布式事务协调

### 2. 模块化设计
- 业务模块按功能域划分（Users, Patients, Herbs, Formula, Prescriptions, Consultation, MedicalCase, Auth）
- 模块间通过接口通信，禁止直接依赖具体实现
- 每个模块包含独立的 Services、Repositories、Entities

### 3. 分层架构
- **表现层（Presentation）**：WPF Desktop + ASP.NET Core Web API
- **业务层（Business）**：模块化服务层
- **数据访问层（Data Access）**：Repository + EF Core
- **基础设施层（Infrastructure）**：公共组件、配置、日志

## 技术约束

### 禁止使用的技术框架

参考 `.ai/rules.json` 中定义的禁止列表：
- MediatR（CQRS模式实现）
- FluentValidation（复杂验证场景）
- Hangfire/Quartz（复杂调度需求）
- Redis/MemoryCache（分布式缓存）
- RabbitMQ/Kafka（消息队列）
- SignalR（实时通信）
- GraphQL（复杂查询）
- Docker/Kubernetes（容器化部署）

### 允许使用的技术

- **.NET 8.0**：核心框架
- **EF Core 8.0**：ORM
- **ASP.NET Core 8.0**：Web API
- **WPF + Prism**：桌面客户端
- **Serilog**：日志
- **xUnit/NUnit**：测试
- **JWT**：身份认证

## 命名约束

参考 `.ai/rules.json` 中的命名规则：
- **禁止**包含以下词汇的类名：
  - `Command`/`Query`（CQRS模式）
  - `Event`（事件驱动）
  - `Handler`（MediatR模式）
  - `Worker`（后台任务）
  - `Queue`（消息队列）

## API 设计约束

### 路由规范
- **统一前缀**：`/api/v1/`
- **RESTful 风格**：资源导向，使用标准 HTTP 方法
- **小写命名**：路由路径使用小写（例：`/api/v1/patients`，而非 `/api/v1/Patients`）

### 响应格式
- 统一使用 `System.Text.Json`
- 禁止使用 `Newtonsoft.Json`

## 数据访问约束

### Repository 模式
- 每个业务模块有独立的 Repository 接口和实现
- Repository 仅返回实体对象或 DTO，禁止返回 `IQueryable`
- 禁止在 Service 层直接使用 `DbContext`

### UnitOfWork 模式
- 使用 `IUnitOfWork` 管理事务
- 禁止在 Repository 外部直接调用 `SaveChangesAsync`

## 依赖注入约束

- **仅使用构造函数注入**
- **禁止**使用以下反模式：
  - `IServiceProvider.GetService<T>()`（Service Locator）
  - `Container.Resolve<T>()`（直接容器访问）
  - 属性注入
  - 方法注入

## 测试约束

### 单元测试
- 每个业务模块必须有对应的单元测试项目
- 测试覆盖率目标：≥70%（P3级）
- 使用 xUnit 或 NUnit
- 测试项目命名：`LYBT.Module.{ModuleName}.Tests.csproj`

### 集成测试
- WebAPI 集成测试：`LYBT.WebAPI.Tests.csproj`
- 使用 `WebApplicationFactory` 进行测试

### 架构测试
- 使用 NetArchTest.Rules 验证架构约束
- 架构测试项目：`LYBT.ArchTests.csproj`

## 构建与部署约束

### 构建输出
- **统一输出路径**：`BIN/` 目录
  - Server: `BIN/Server/{Configuration}/`
  - Desktop: `BIN/Desktop/{Configuration}/`
  - Tests: `BIN/Tests/{Configuration}/`

### 配置管理
- 使用 `appsettings.json` + 环境变量
- 敏感信息使用 User Secrets（开发）或 Key Vault（生产）

## 文档约束

### 品牌表述
- **项目名称**：LYBT（Lao Zhong Yi Ben Tou De）
- **命名空间**：统一使用 `LYBT.*` 前缀
- **文件路径**：保持 `LYBT.*` 结构

### 代码注释
- 所有公共接口必须有 XML 文档注释（`///`）
- 复杂逻辑必须有行内注释
- 使用中文编写注释和文档

## 合规性验证

### 自动化检查
- **代码格式**：`dotnet format --verify-no-changes`
- **架构测试**：`dotnet test tests/Architecture/LYBT.ArchTests.csproj`
- **单元测试**：所有测试必须通过

### 人工审查
- PR 审查必须确认符合架构约束
- 重大架构变更需要在 Issue 中讨论并获批准

## 变更管理

### 架构变更流程
1. 在 GitHub Issue 中提出架构变更建议
2. 团队讨论并评估影响
3. 更新本文档和 `.ai/rules.json`
4. 实施变更并验证

### 文档更新
- 架构变更必须同步更新：
  - `_governance/architecture.md`（本文档）
  - `.ai/rules.json`（规则配置）
  - `docs/architecture/`（架构文档）

---

**最后更新**：2025-10-02
**版本**：1.0.0
**维护者**：LYBT Team
