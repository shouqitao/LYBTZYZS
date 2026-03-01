# LYBT.Tests.Desktop.Integration

> Desktop 端集成测试 | 本地模式 DataSource 完整数据流 + 业务流程 E2E

## 覆盖范围

| 测试类 | 测试数 | 说明 |
|--------|--------|------|
| DataSourceIntegrationTests | ~10 | DI 容器解析、5 个 DataSource CRUD 验证 |
| MedicalCaseDataSourceTests | ~8 | 医案聚合根保存、Consultation/Prescription 嵌套、状态管理 |
| BusinessFlowTests | ~6 | 完整业务流程 (创建用户->药材->验方->患者->医案->完成) |

## 测试策略

- 框架: xUnit + FluentAssertions + NSubstitute
- 数据库: SQLite InMemory (每测试独立实例)
- 共享 Fixture: DesktopFixture (IClassFixture)
- 最小 Mock: 仅 Mock ICurrentUserProvider
- 目标框架: net8.0-windows (WPF 依赖)

## 测试层次

- LocalMode: DI 容器 -> DataSource -> LocalDbContext -> SQLite 数据流
- MedicalCases: 聚合根 CRUD、嵌套实体持久化、查询过滤
- E2E: 跨模块完整业务链路 (用户/药材/患者/验方/医案联动)

## 运行方式

```
dotnet test tests/LYBT.Tests.Desktop.Integration/
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始创建 README |
