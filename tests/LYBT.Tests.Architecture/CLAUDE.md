# LYBT.Tests.Architecture

架构约束测试项目。使用 NetArchTest.Rules 对服务端和桌面端程序集进行反射驱动的架构规则验证，确保分层边界、命名约定、禁用框架和设计模式约束不被破坏。

## 项目基本信息

- **目标框架**: net8.0-windows (需加载 WPF 程序集)
- **测试框架**: xunit + FluentAssertions + NetArchTest.Rules
- **引用**: 全量 Server + Desktop 程序集
- **总测试方法数**: 约 60 个 (含 Theory 展开)

## 目录结构

```
tests/LYBT.Tests.Architecture/
├── ArchTests.cs                  # 通用架构门禁 (26 个方法 + 9 Theory)
├── AggregateRootArchTests.cs     # 聚合根模式验证 (2)
├── CustomControlArchTests.cs     # WPF 控件 DataContext 规范 (5)
├── DesktopLayerArchTests.cs      # Desktop 层分层约束 (13)
└── ServerArchTests.cs            # Server 层专用约束 (16)
```

## 测试类详情

### ArchTests (26 + 9 Theory)
- 层依赖: UI 不依赖 Infrastructure/Entities，Desktop 不依赖 WebAPI
- API 版本: 路由必须 `api/v{version}/` 或 `api/v1/`
- 命名禁止: Pipeline/Workflow/Bus/Engine/Saga
- 框架禁止: MediatR/NServiceBus/MassTransit/Hangfire/Quartz
- Batch2 治理: 缓存单一来源、统一异常处理、控制器基类、回归防护
- P06 层级守护: 9 个程序集反向依赖检测 ([Theory][InlineData])

### AggregateRootArchTests (2)
- MedicalCase 聚合根验证 (Consultation/Prescription Controller 已删除)
- 软删除一致性 (所有实体必须有 IsDeleted 或继承 BaseEntity)

### CustomControlArchTests (5)
- ContentHosting 控件不得在构造函数设 DataContext
- 必需控件存在性验证
- MasterDetailLayout/DataGridToolbar Content 属性验证

### DesktopLayerArchTests (13)
- Desktop 不依赖 Server 层、不含 DTO 类
- ViewModel 基类继承检查、Services 命名规范
- 双模式完整性: 每个 IDataSource 必须有 Remote + Local 实现
- MasterDetail 继承基类检查

### ServerArchTests (16)
- 控制器命名空间、[Authorize] 属性、路由版本
- 禁止 MediatR/CQRS/Redis/Dapper
- 模块无循环依赖、Repository 继承 BaseRepository
- 跨模块引用必须通过接口

## 测试模式

- **白名单过滤**: 合理例外通过 `Where(t => !allowedPatterns.Contains(t.Name))` 排除
- **Theory 参数化**: P-06 层级依赖用 [InlineData] 覆盖 9 个程序集
- **存根测试**: `Batch2_ConfigurationDirectRead` 和 `Should_Use_Unified_Navigation_Service` 为占位

## 已知缺口

- `SetsDataContextInConstructor` IL 分析为简化实现，始终返回 false，无实际检测能力
- `Batch2_ConfigurationDirectRead` 测试体已注释，仅断言 true
- `Should_Use_Unified_Navigation_Service` 方法体为空

---
最后更新: 2026-03-01
