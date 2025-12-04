# Spec: project-architecture

## Purpose

定义LYBTZYZS项目的整体架构分层、项目命名规范和依赖方向约束。确保35个项目保持清晰的职责边界和一致的架构风格。

## Requirements

### Requirement: ARCH-001 三层架构定义

项目 SHALL 采用Server/Shared/Client三层架构。

**架构分层**:

| 层级 | 项目数 | 职责 | 技术栈 |
|------|--------|------|--------|
| Server | 13 | API服务、业务逻辑、数据访问 | ASP.NET Core 8, EF Core |
| Shared | 4 | DTO、验证器、工具类、组件 | .NET 8 类库 |
| Client | 16 | WPF桌面应用、MVVM | WPF, Prism 8 |

#### Scenario: 新增Server层项目
- **WHEN** 需要添加新的后端业务模块
- **THEN** SHALL 创建在 `src/Server/Modules/` 目录下
- **AND** SHALL 命名为 `LYBT.Module.{Domain}`

#### Scenario: 新增Shared层项目
- **WHEN** 需要添加Server和Client共享的代码
- **THEN** SHALL 创建在 `src/Shared/` 目录下
- **AND** SHALL 命名为 `LYBT.Shared.{Purpose}`

#### Scenario: 新增Client层项目
- **WHEN** 需要添加桌面端模块
- **THEN** SHALL 创建在 `src/Client/Desktop/` 目录下
- **AND** SHALL 命名为 `LYBT.Desktop.{Domain}`

---

### Requirement: ARCH-002 项目命名规范

项目 SHALL 遵循统一的命名规范。

**命名规则**:

| 层级 | 前缀 | 示例 |
|------|------|------|
| Server Core | `LYBT.` | LYBT.Entities, LYBT.Infrastructure |
| Server Module | `LYBT.Module.` | LYBT.Module.Patients, LYBT.Module.MedicalCase |
| Server Service | `LYBT.` | LYBT.WebAPI |
| Shared | `LYBT.Shared.` | LYBT.Shared.Models, LYBT.Shared.Validators |
| Client Core | `LYBT.Desktop.` | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation |
| Client Module | `LYBT.Desktop.` | LYBT.Desktop.Patients, LYBT.Desktop.MedicalCase |
| Client Role | `LYBT.Desktop.` | LYBT.Desktop.Clinical, LYBT.Desktop.Admin |
| Client Shell | `LYBT.Desktop.` | LYBT.Desktop.Shell |

#### Scenario: 验证项目命名
- **WHEN** 创建新项目
- **THEN** 项目名 SHALL 以 `LYBT.` 开头
- **AND** 命名空间 SHALL 与项目名一致

#### Scenario: 模块命名使用领域名
- **WHEN** 创建业务模块项目
- **THEN** `{Domain}` SHALL 使用业务领域名(如Patients, MedicalCase)
- **AND** SHALL NOT 使用技术术语

---

### Requirement: ARCH-003 依赖方向规范

项目依赖 SHALL 遵循单向依赖原则，禁止循环依赖。

**Server层依赖方向**:
```
WebAPI → Modules → Infrastructure → Entities
                 ↘ Shared.Models ↙
```

**Client层依赖方向**:
```
Shell → Roles → Modules → Presentation → Infrastructure → Foundation → Contracts
                                       ↘ Models ↙
```

#### Scenario: Server模块依赖Infrastructure
- **WHEN** Server Module需要数据访问
- **THEN** SHALL 依赖 LYBT.Infrastructure
- **AND** SHALL 依赖 LYBT.Entities
- **AND** MAY 依赖 LYBT.Shared.*

#### Scenario: 禁止循环依赖
- **WHEN** 模块A依赖模块B
- **THEN** 模块B SHALL NOT 依赖模块A
- **AND** 编译 SHALL 无循环引用警告

#### Scenario: Shared层无外部依赖
- **WHEN** Shared项目引用其他项目
- **THEN** SHALL NOT 引用Server层项目
- **AND** SHALL NOT 引用Client层项目
- **AND** MAY 引用其他Shared项目

#### Scenario: 跨模块通信
- **WHEN** Server Module需要其他模块数据
- **THEN** SHALL 使用ICrossModuleQueryService (参见module-communication规范)
- **AND** SHALL NOT 直接注入其他模块的Repository

---

### Requirement: ARCH-004 模块注册规范

模块 SHALL 通过标准方式注册到DI容器。

**Server模块注册**:
- 每个模块 SHALL 有 `{Domain}Module.cs` 入口类
- 模块 SHALL 实现 `RegisterTypes(IServiceCollection)` 方法
- WebAPI SHALL 在 `Program.cs` 调用模块注册

**Client模块注册**:
- 每个模块 SHALL 有 `{Domain}Module.cs` 继承 `IModule`
- 模块 SHALL 实现 `RegisterTypes(IContainerRegistry)` 方法
- Shell SHALL 在 `App.xaml.cs` 配置模块加载

#### Scenario: Server模块注册
- **WHEN** 创建Server业务模块
- **THEN** SHALL 创建 `{Domain}Module.cs`
- **AND** SHALL 注册模块的Service和Repository
- **AND** SHALL 在WebAPI的Program.cs中调用

#### Scenario: Client模块注册
- **WHEN** 创建Client业务模块
- **THEN** SHALL 创建 `{Domain}Module.cs` 继承 `IModule`
- **AND** SHALL 注册View和ViewModel
- **AND** SHALL 配置Region导航

#### Scenario: 验证模块注册完整性
- **WHEN** 应用启动
- **THEN** 所有必需Service SHALL 已注册
- **AND** DI解析 SHALL 无异常

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| module-communication | 模块间通信规范(ICrossModuleQueryService) |
| server-layer-architecture | Server层详细架构 |
| client-layer-architecture | Client层详细架构 |
| shared-layer-architecture | Shared层详细架构 |
| repository-patterns | Repository模式规范 |
| service-conventions | Service约定规范 |
| viewmodel-conventions | ViewModel约定规范 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义三层架构和命名规范 |
