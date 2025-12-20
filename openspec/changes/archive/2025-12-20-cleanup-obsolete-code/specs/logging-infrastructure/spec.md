# logging-infrastructure Delta

## MODIFIED Requirements

### Requirement: LOG-001 Serilog统一日志框架

Server端和Client端 **SHALL** 通过`LYBT.Shared.Logging`共享项目使用Serilog作为统一的结构化日志框架。

> **变更说明**: 删除过时组件，完成向共享项目的完全迁移

**项目结构**:
- `LYBT.Shared.Logging`项目集中管理所有Serilog依赖和组件
- Server端通过`LYBT.Infrastructure`引用共享项目
- Client端通过`LYBT.Desktop.Infrastructure`引用共享项目
- WebAPI保留Serilog.AspNetCore和Serilog.Sinks.MSSqlServer直接引用

**组件清理**:
- LYBT.Infrastructure.Logging中的过时组件 **SHALL** 被删除
- LYBT.Desktop.Infrastructure.Logging中的过时组件 **SHALL** 被删除
- 所有代码 **SHALL** 直接使用LYBT.Shared.Logging中的组件

#### Scenario: 共享日志项目完全迁移
- **WHEN** 使用日志组件
- **THEN** SensitiveDataMasker **SHALL** 从LYBT.Shared.Logging.Masking引用
- **AND** LoggingLevelManager **SHALL** 从LYBT.Shared.Logging.Management引用
- **AND** CorrelationIdEnricher **SHALL** 从LYBT.Shared.Logging.Enrichers引用
- **AND** 过时组件 **SHALL NOT** 存在于Infrastructure项目中

---

### Requirement: LOG-002 CorrelationId端到端追踪

所有请求 **SHALL** 通过统一的CorrelationIdEnricher实现端到端追踪。

> **变更说明**: 删除过时的CorrelationIdEnricher实现

**接口抽象**:
- `ICorrelationIdProvider`接口定义获取/设置CorrelationId的方法
- Server端使用`HttpContextCorrelationIdProvider`实现
- Desktop端使用`FoundationCorrelationIdProvider`实现

#### Scenario: CorrelationIdEnricher统一实现
- **WHEN** 配置日志Enricher
- **THEN** CorrelationIdEnricher **SHALL** 来自LYBT.Shared.Logging.Enrichers
- **AND** LYBT.Infrastructure.Logging.CorrelationIdEnricher **SHALL NOT** 存在
- **AND** LYBT.Desktop.Infrastructure.Logging.CorrelationIdEnricher **SHALL NOT** 存在

---

### Requirement: LOG-006 异常日志规范

异常处理 **SHALL** 记录完整的结构化日志。

> **变更说明**: 删除过时的GlobalExceptionHandler

**异常处理器**:
- `BusinessExceptionHandler` - 处理AppException及其子类
- `SystemExceptionHandler` - 兜底处理所有未被处理的系统异常

#### Scenario: 异常处理器统一
- **WHEN** 配置异常处理
- **THEN** **SHALL** 使用BusinessExceptionHandler和SystemExceptionHandler
- **AND** GlobalExceptionHandler **SHALL NOT** 存在
- **AND** 业务异常(AppException) **SHALL** 由BusinessExceptionHandler处理
- **AND** 系统异常 **SHALL** 由SystemExceptionHandler处理
