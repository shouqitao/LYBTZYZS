# LYBT.Tests.Server.Integration

> Server API 集成测试 | 完整 HTTP 管线验证 (Controller -> Service -> Repository -> DB)

## 覆盖范围

| 测试类 | 测试数 | 说明 |
|--------|--------|------|
| AuthIntegrationTests | ~25 | 登录/登出、JWT Token 签发与验证、角色鉴权 |
| UserIntegrationTests | ~20 | 用户 CRUD、角色权限隔离、分页查询 |
| PatientIntegrationTests | ~20 | 患者 CRUD、搜索、软删除 |
| HerbIntegrationTests | ~20 | 药材 CRUD、批量操作、分页 |
| FormulaIntegrationTests | ~20 | 验方 CRUD、药材组成关联 |
| MedicalCaseIntegrationTests | ~25 | 医案聚合根 CRUD、诊断/处方嵌套、状态流转 |
| SyncIntegrationTests | ~16 | 数据同步 6 端点 (metadata/compare/upload/download/delete) |

## 测试策略

- 框架: xUnit + FluentAssertions + NSubstitute
- 基础设施: WebApplicationFactory + SQL Server 测试库 (LYBT_Test)
- 共享 Fixture: WebApiFixture (IClassFixture + Collection)
- 预置角色客户端: AdminClient / DoctorClient / AnonymousClient
- 无 Mock: 全链路真实执行 (HTTP -> Controller -> Service -> EF Core -> DB)

## 前置条件

- 本地 SQL Server 实例可用
- appsettings.Test.json 中配置测试数据库连接字符串

## 运行方式

```
dotnet test tests/LYBT.Tests.Server.Integration/
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始创建 README |
