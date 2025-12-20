# logging-infrastructure Specification

## Purpose
TBD - created by archiving change refactor-logging-system. Update Purpose after archive.
## Requirements
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

### Requirement: LOG-004 日志输出格式

所有日志 SHALL 使用统一的输出格式。

**标准输出模板**:
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}
```

**字段说明**:
- Timestamp: ISO 8601格式,包含时区
- Level: 3字符大写(INF/WRN/ERR)
- CorrelationId: 请求追踪ID
- SourceContext: 日志来源(类名)
- Message: 结构化消息
- Exception: 异常堆栈(如有)

#### Scenario: Console输出格式
- **WHEN** 输出到Console(开发环境)
- **THEN** MAY 使用简化模板(省略日期)
- **AND** MAY 使用彩色输出(AnsiConsoleTheme)

#### Scenario: File输出格式
- **WHEN** 输出到文件(生产环境)
- **THEN** SHALL 使用完整模板
- **AND** SHALL 包含时区信息
- **AND** 编码 SHALL 为UTF-8

---

### Requirement: LOG-005 日志Enrichers配置

日志 SHALL 包含丰富的上下文信息。

**必需Enrichers**:
- FromLogContext: 支持LogContext.PushProperty
- CorrelationId: 请求追踪ID
- MachineName: 机器名称
- ThreadId: 线程ID

**可选Enrichers**:
- UserContext: 当前用户ID(Server端)
- Application: 应用名称标识

#### Scenario: Server端Enrichers
- **WHEN** 配置Server端Serilog
- **THEN** SHALL 添加所有必需Enrichers
- **AND** SHALL 添加UserContext Enricher
- **AND** Properties.Application SHALL 为"LYBT.WebAPI"

#### Scenario: Client端Enrichers
- **WHEN** 配置Client端Serilog
- **THEN** SHALL 添加所有必需Enrichers
- **AND** Properties.Application SHALL 为"LYBT.Desktop"

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

### Requirement: LOG-007 日志分级存储

高级别日志 SHALL 持久化到数据库,支持长期保存和高效查询。

**存储策略**:
- Debug: 仅文件(可选开启)
- Information: 仅文件
- Warning: 文件 + 数据库
- Error/Fatal: 文件 + 数据库

**保留策略**:
- 文件日志: 30天
- 数据库Warning: 90天
- 数据库Error/Fatal: 永久

#### Scenario: Warning级别日志写入数据库
- **WHEN** 记录Warning级别日志
- **THEN** 日志 SHALL 写入文件
- **AND** 日志 SHALL 同时写入SystemLogs数据库表
- **AND** 数据库记录 SHALL 包含CorrelationId、UserId、RequestPath

#### Scenario: Information级别日志不写入数据库
- **WHEN** 记录Information级别日志
- **THEN** 日志 SHALL 仅写入文件
- **AND** 日志 SHALL NOT 写入数据库

#### Scenario: 数据库日志保留策略
- **WHEN** 日志清理作业执行
- **THEN** 超过90天的Warning级别日志 SHALL 被删除
- **AND** Error/Fatal级别日志 SHALL NOT 被删除

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

### Requirement: LOG-009 SystemLogs数据库表设计

SystemLogs表 SHALL 存储高级别日志用于长期保存和查询。

**必需字段**:
- Id: 自增主键
- Timestamp: 日志时间(DATETIMEOFFSET)
- Level: 日志级别(NVARCHAR(16))
- Message: 日志消息
- Exception: 异常详情
- CorrelationId: 请求追踪ID
- UserId: 操作用户ID
- RequestPath: 请求路径
- MachineName: 服务器名称

**索引要求**:
- Timestamp索引(支持时间范围查询)
- Level索引(支持级别筛选)
- CorrelationId索引(支持追踪查询)

#### Scenario: 日志写入数据库完整性
- **WHEN** Warning/Error日志写入SystemLogs表
- **THEN** 所有必需字段 SHALL 正确填充
- **AND** CorrelationId SHALL 与文件日志保持一致
- **AND** Timestamp SHALL 包含时区信息

---

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

