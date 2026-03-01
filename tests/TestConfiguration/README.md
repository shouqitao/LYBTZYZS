# 共享测试配置 (LYBT.Tests.Configuration)

> 所有测试项目共用的基础设施 | 测试基类 | 数据构建器 | 断言辅助

本项目是类库 (非测试项目)，目标框架 net8.0 + net8.0-windows 双目标，
同时支持 Server 端和 Desktop 端测试。

## 目录结构

| 文件/目录 | 用途 |
|-----------|------|
| TestBase.cs | 测试基类 (DI 容器、Mock 创建、InMemory 数据库工厂) |
| IntegrationTestBase.cs | 集成测试基类 (WebApplicationFactory、认证、共享 InMemory 数据库) |
| ClientRepositoryTestBase.cs | 桌面端 Repository 测试基类 |
| SqlServerTestDbContextFactory.cs | SQL Server 测试数据库上下文工厂 |
| Database/SqliteTestDatabaseFactory.cs | SQLite 测试数据库工厂 |
| TestDataBuilders/BaseTestDataBuilder.cs | 测试数据构建器基类 (Builder 模式) |
| AssertionHelpers/TestAssertions.cs | 自定义断言辅助方法 |
| Wpf/WpfTestCollection.cs | WPF 测试集合定义 (STA 线程支持) |

## 技术信息

- **目标框架**: net8.0 + net8.0-windows (双目标，同时支持 Server 和 Desktop 测试)
- **类型**: 类库 (非测试项目)，被其他测试项目引用

## 主要依赖

- **项目引用 (net8.0)**: LYBT.WebAPI, LYBT.Infrastructure, LYBT.Entities, LYBT.Shared.Models, LYBT.Module.Auth/Users/Patients/Herbs/Formula/MedicalCase
- **项目引用 (net8.0-windows 额外)**: LYBT.Desktop.Models, LYBT.Desktop.Infrastructure
- **NuGet**: xUnit, FluentAssertions, NSubstitute, EF Core (InMemory/SQLite/SqlServer), ASP.NET Core Mvc.Testing, Xunit.StaFact

## 更新记录

- 2026-03-01: 创建 README 文档
