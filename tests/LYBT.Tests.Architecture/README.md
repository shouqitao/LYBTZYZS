# LYBT.Tests.Architecture

> 架构约束测试 | 依赖规则、命名规范、聚合根模式、控件规范

## 覆盖范围

| 测试类 | 测试数 | 说明 |
|--------|--------|------|
| ArchTests | ~15 | 全局层间依赖规则 (UI 不依赖 Infrastructure/Entities) |
| ServerArchTests | ~15 | Server 端约束 (Controller 路由、Service 命名、分层纯净) |
| DesktopLayerArchTests | ~15 | Desktop 端约束 (不依赖 Server 层、不含 DTO 类) |
| AggregateRootArchTests | ~10 | MedicalCase 聚合根模式验证 (AR-001/AR-003) |
| CustomControlArchTests | ~5 | 自定义控件 DataContext 处理规范 |

## 测试策略

- 框架: xUnit + NetArchTest.Rules + FluentAssertions
- 模式: 基于反射的类型扫描和依赖分析
- 目标框架: net8.0-windows
- 覆盖 Server 端 + Desktop 端全部程序集

## 核心规则

- Server 层: Entities 不依赖 Infrastructure, Infrastructure 不依赖 WebAPI
- Desktop 层: 模块不依赖 Server 层 (Entities/Infrastructure)
- 聚合根: MedicalCase 是唯一聚合根, 无独立 ConsultationController
- 命名: Controller/Service/Repository 后缀强制, API 路由 v1 前缀

## 运行方式

```
dotnet test tests/LYBT.Tests.Architecture/
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始创建 README |
