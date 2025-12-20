# logging-infrastructure Specification Delta

## MODIFIED Requirements

### Requirement: LOG-001 Serilog统一日志框架

Server端和Client端 **SHALL** 通过`LYBT.Shared.Logging`共享项目使用Serilog作为统一的结构化日志框架。

> **变更说明**: 将Serilog依赖和配置统一到LYBT.Shared.Logging项目

**项目结构**:
- `LYBT.Shared.Logging`项目集中管理所有Serilog依赖
- Server端通过`LYBT.Infrastructure`引用共享项目
- Client端通过`LYBT.Desktop.Infrastructure`引用共享项目
- WebAPI保留Serilog.AspNetCore和Serilog.Sinks.MSSqlServer直接引用

**共享配置**:
- 通用Enrichers配置(CorrelationId, MachineName, ThreadId)
- 统一输出格式模板
- 敏感数据脱敏策略

#### Scenario: 共享日志项目依赖
- **WHEN** 配置日志系统
- **THEN** Serilog核心依赖 **SHALL** 在LYBT.Shared.Logging中统一管理
- **AND** LYBT.Infrastructure **SHALL** 引用LYBT.Shared.Logging
- **AND** LYBT.Desktop.Infrastructure **SHALL** 引用LYBT.Shared.Logging
- **AND** Serilog包 **SHALL NOT** 在Infrastructure项目中重复引用

---

### Requirement: LOG-002 CorrelationId端到端追踪

所有请求 **SHALL** 通过统一的CorrelationIdEnricher实现端到端追踪。

> **变更说明**: CorrelationIdEnricher统一到共享项目,使用接口解耦HttpContext依赖

**接口抽象**:
- `ICorrelationIdProvider`接口定义获取/设置CorrelationId的方法
- Server端使用`HttpContextCorrelationIdProvider`实现
- Desktop端使用`AsyncLocalCorrelationIdProvider`实现

#### Scenario: CorrelationId提供者注入
- **WHEN** 配置日志Enricher
- **THEN** CorrelationIdEnricher **SHALL** 通过ICorrelationIdProvider获取CorrelationId
- **AND** Server端 **SHALL** 注册HttpContextCorrelationIdProvider
- **AND** Desktop端 **SHALL** 注册AsyncLocalCorrelationIdProvider
- **AND** CorrelationIdEnricher **SHALL** 在LYBT.Shared.Logging中定义

---

### Requirement: LOG-003 敏感数据脱敏

日志输出 **SHALL** 通过`LYBT.Shared.Logging.Masking`命名空间下的组件自动对敏感数据进行脱敏处理。

> **变更说明**: SensitiveDataMasker和SensitiveDataDestructuringPolicy迁移到共享项目

**组件位置**:
- `SensitiveDataMasker` → `LYBT.Shared.Logging.Masking.SensitiveDataMasker`
- `SensitiveDataDestructuringPolicy` → `LYBT.Shared.Logging.Masking.SensitiveDataDestructuringPolicy`

#### Scenario: 脱敏组件共享
- **WHEN** Server或Desktop需要日志脱敏
- **THEN** **SHALL** 使用LYBT.Shared.Logging.Masking中的组件
- **AND** SensitiveDataAttribute **SHALL** 从LYBT.Shared.Primitives引用
- **AND** 脱敏逻辑 **SHALL** 在两端保持一致

---

### Requirement: LOG-008 动态日志级别控制

生产环境 **SHALL** 通过`LYBT.Shared.Logging.Management.LoggingLevelManager`支持动态调整日志级别。

> **变更说明**: LoggingLevelManager迁移到共享项目

**组件位置**:
- `LoggingLevelManager` → `LYBT.Shared.Logging.Management.LoggingLevelManager`
- `DebugModeInfo` → `LYBT.Shared.Logging.Management.DebugModeInfo`

#### Scenario: 日志级别管理器共享
- **WHEN** 需要动态调整日志级别
- **THEN** **SHALL** 使用LYBT.Shared.Logging.Management.LoggingLevelManager
- **AND** Server和Desktop **MAY** 使用相同的管理器

---

## ADDED Requirements

### Requirement: LOG-010 共享日志项目架构

`LYBT.Shared.Logging`项目 **SHALL** 作为日志系统的统一基础设施层。

**项目职责**:
- 集中管理Serilog依赖
- 提供通用日志配置和扩展
- 定义日志相关的接口和抽象
- 提供敏感数据脱敏功能
- 提供日志级别动态管理

**项目依赖**:
- LYBT.Shared.Primitives (SensitiveDataAttribute)
- Serilog及相关Sink和Enricher包
- Microsoft.Extensions.Logging.Abstractions

#### Scenario: 项目结构验证
- **GIVEN** LYBT.Shared.Logging项目存在
- **WHEN** 检查项目结构
- **THEN** **SHALL** 包含Abstractions目录(接口定义)
- **AND** **SHALL** 包含Configuration目录(配置类)
- **AND** **SHALL** 包含Enrichers目录(Enricher实现)
- **AND** **SHALL** 包含Masking目录(脱敏组件)
- **AND** **SHALL** 包含Management目录(管理组件)
- **AND** **SHALL** 包含Extensions目录(扩展方法)

#### Scenario: 依赖方向验证
- **WHEN** 检查项目依赖
- **THEN** LYBT.Shared.Logging **SHALL** 仅依赖LYBT.Shared.Primitives
- **AND** LYBT.Infrastructure **SHALL** 依赖LYBT.Shared.Logging
- **AND** LYBT.Desktop.Infrastructure **SHALL** 依赖LYBT.Shared.Logging
- **AND** 循环依赖 **SHALL NOT** 存在

---

### Requirement: LOG-011 日志配置扩展方法

共享项目 **SHALL** 提供便捷的日志配置扩展方法。

**扩展方法**:
- `UseSharedLogging(ICorrelationIdProvider)`: 应用共享日志配置
- `WithSensitiveDataMasking()`: 启用敏感数据脱敏
- `WithCorrelationId(ICorrelationIdProvider)`: 添加CorrelationId Enricher

#### Scenario: 共享配置应用
- **WHEN** 配置LoggerConfiguration
- **THEN** **MAY** 调用UseSharedLogging扩展方法
- **AND** 该方法 **SHALL** 应用所有通用Enrichers
- **AND** 该方法 **SHALL** 应用敏感数据脱敏策略
- **AND** 该方法 **SHALL** 设置统一输出格式

#### Scenario: DI扩展方法
- **WHEN** 配置DI容器
- **THEN** **MAY** 调用AddSharedLogging扩展方法
- **AND** 该方法 **SHALL** 注册LoggingLevelManager
- **AND** 该方法 **SHALL** 注册ICorrelationIdProvider(需指定实现)
