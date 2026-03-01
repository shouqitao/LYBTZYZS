# LYBT.Tests.Desktop.Integration

Desktop 端集成测试项目，验证桌面客户端本地数据层的端到端数据流。通过真实 LocalDataSource + SQLite InMemory 数据库运行，不依赖服务端。

## 项目基本信息

- **目标框架**: net8.0-windows (UseWPF=true)
- **测试框架**: xunit + FluentAssertions + NSubstitute
- **测试数据库**: SQLite InMemory (每个测试独立连接)
- **总测试方法数**: 约 24 个

## 目录结构

```
tests/LYBT.Tests.Desktop.Integration/
├── Fixtures/
│   └── DesktopFixture.cs              # IClassFixture: DI 容器 + SQLite InMemory
├── E2E/
│   └── BusinessFlowTests.cs           # 完整业务流程 (3)
├── LocalMode/
│   └── DataSourceIntegrationTests.cs  # DataSource CRUD 集成 (13)
└── MedicalCases/
    └── MedicalCaseDataSourceTests.cs  # 医案聚合操作专项 (8)
```

## DesktopFixture

- 每次 `CreateServiceProviderAsync()` 创建全新 SQLiteConnection(:memory:) + ServiceProvider
- 仅 Mock `ICurrentUserProvider` (最小 Mock 范围)，其余使用真实实现
- 静态 `TestUserId = Guid.Parse("...000099")` 供测试使用

## 测试覆盖映射

| 测试类 | 测试数 | 覆盖内容 |
|--------|--------|----------|
| BusinessFlowTests | 3 | 8步完整流程、本地认证、多患者数据隔离 |
| DataSourceIntegrationTests | 13 | DI 解析、Patient/Herb/Formula/User/MedicalCase CRUD、分页、搜索、批量删除 |
| MedicalCaseDataSourceTests | 8 | 聚合保存(Consultation+Prescription)、状态流转、按 PatientId/Status 查询 |

## 测试模式

- **SQLite InMemory 隔离**: 每个测试方法独立数据库，Fixture.Dispose 统一关闭
- **最小 Mock 原则**: 仅 Mock ICurrentUserProvider
- **聚合验证**: SaveAsync -> GetWithDetailsAsync 验证嵌套数据持久化

## 已知问题

- 缺少 WPF ViewModel 集成测试 (仅覆盖 DataSource 层)
- CancelAsync 实为软删除 (非状态变更)，与方法名语义不一致
- 无并发写入场景测试
- 仅 net8.0-windows，不可在 Linux CI 上运行

---
最后更新: 2026-03-01
