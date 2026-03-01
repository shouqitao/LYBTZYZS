# LYBT.Tests.Server.Integration

Server 端 HTTP 集成测试项目。使用 WebApplicationFactory 启动完整 ASP.NET Core 管线，连接本地 SQL Server 测试数据库，对所有 API 端点进行端到端验证。

## 项目基本信息

- **目标框架**: net8.0
- **测试框架**: xunit + FluentAssertions
- **测试数据库**: SQL Server localhost (LYBT_Test)，每次 Fixture 初始化 Drop+Migrate
- **总测试方法数**: 约 146 个

## 目录结构

```
tests/LYBT.Tests.Server.Integration/
├── appsettings.Test.json                  # 测试环境配置
├── Fixtures/
│   ├── ServerIntegrationCollection.cs     # xunit Collection 定义
│   └── WebApiFixture.cs                   # 核心 Fixture: WAF + SQL Server + JWT + 种子数据
├── Auth/AuthIntegrationTests.cs           # 认证 (15)
├── Herbs/HerbIntegrationTests.cs          # 药材 CRUD (17)
├── Formulas/FormulaIntegrationTests.cs    # 验方 CRUD (16)
├── Patients/PatientIntegrationTests.cs    # 患者 CRUD + 搜索 (24)
├── MedicalCases/MedicalCaseIntegrationTests.cs  # 医案聚合 + 状态流转 (22)
├── Users/UserIntegrationTests.cs          # 用户管理 + 权限 (28)
└── Sync/SyncIntegrationTests.cs           # 数据同步 (25)
```

## WebApiFixture

所有测试类通过 `[Collection("ServerIntegration")]` 共享同一 Fixture:
- HTTP 管线: `WebApplicationFactory<Program>`，Environment = "Test"
- 数据库: Drop+Migrate 确保唯一索引等迁移约束生效
- 认证客户端: `AdminClient` / `DoctorClient` / `AnonymousClient`
- JWT 生成: 内置 `GenerateJwtToken`，Claims 结构与 JwtService 一致
- 动态客户端: `CreateClientAs(UserRole, Guid, string)` 用于特定角色测试

## 测试覆盖映射

| 测试类 | 端点前缀 | 方法数 | 关键场景 |
|--------|----------|--------|----------|
| AuthIntegrationTests | /api/v1/auth | 15 | 登录/刷新/验证/登出/权限 |
| HerbIntegrationTests | /api/v1/herbs | 17 | CRUD/状态切换/恢复/批量/价格 |
| FormulaIntegrationTests | /api/v1/formulas | 16 | CRUD/药材子集/延迟绑定/批量 |
| PatientIntegrationTests | /api/v1/patients | 24 | CRUD/拼音码/分页/所有权/年龄 |
| MedicalCaseIntegrationTests | /api/v1/medicalcases | 22 | 聚合保存/状态流转/审计/EditReason |
| UserIntegrationTests | /api/v1/users | 28 | CRUD/密码/权限/最后管理员保护 |
| SyncIntegrationTests | /api/v1/sync | 25 | 元数据/比对/上传/下载/引用保护删除 |

## 测试模式

- **AAA 模式**: 注释标注 Arrange/Act/Assert
- **持久化验证**: Post -> Get 双重验证确认数据库真实持久化
- **数据隔离**: Guid.NewGuid() / 线程安全序列号生成唯一标识
- **种子数据**: `SeedAsync<T>` 直接写入 DbContext 绕过业务层

## 状态码语义

| 状态码 | 语义 |
|--------|------|
| 400 | 输入验证失败 |
| 401 | 未携带 Token 或无效 |
| 403 | 角色权限不足 |
| 404 | 资源不存在或已软删除 |
| 422 | 业务规则失败 (BusinessFail) |

## 已知问题

- 依赖本地 SQL Server 实例，CI 需额外配置
- NSubstitute 全局引入但未使用 (纯端到端测试)
- 所有测试共享数据库实例，并发运行可能数据累积
- 缺少: 医案并发冲突、JWT 过期、分页边界条件测试

---
最后更新: 2026-03-01
