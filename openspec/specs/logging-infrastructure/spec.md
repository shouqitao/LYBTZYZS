# logging-infrastructure Specification

## Purpose
TBD - created by archiving change refactor-logging-system. Update Purpose after archive.
## Requirements
### Requirement: LOG-001 Serilog统一日志框架

Server端和Client端 SHALL 使用Serilog作为统一的结构化日志框架。

**Server端配置**:
- 使用Serilog.AspNetCore集成
- 采用两阶段初始化(Bootstrap Logger + Final Logger)
- 配置Console和File两个Sink

**Client端配置**:
- 使用Serilog.Extensions.Logging集成Microsoft.Extensions.Logging
- 配置File Sink输出到%LOCALAPPDATA%/LYBTZYZS/logs
- 按天Rolling,保留30天

#### Scenario: Server端两阶段初始化
- **WHEN** 应用启动
- **THEN** SHALL 首先创建Bootstrap Logger
- **AND** Bootstrap Logger SHALL 在try块外初始化
- **AND** 配置加载后 SHALL 切换到Final Logger
- **AND** 启动异常 SHALL 被Bootstrap Logger记录

#### Scenario: Client端日志初始化
- **WHEN** WPF应用启动
- **THEN** SHALL 在App.OnStartup中初始化Serilog
- **AND** SHALL 配置文件日志输出
- **AND** 日志路径 SHALL 为%LOCALAPPDATA%/LYBTZYZS/logs

#### Scenario: 日志文件Rolling策略
- **WHEN** 配置日志文件输出
- **THEN** SHALL 按天Rolling(RollingInterval.Day)
- **AND** SHALL 保留最近30天日志(retainedFileCountLimit: 30)
- **AND** 文件命名 SHALL 包含日期后缀

---

### Requirement: LOG-002 CorrelationId端到端追踪

所有请求 SHALL 通过CorrelationId实现端到端追踪。

**CorrelationId生成规则**:
- Client端发起请求时生成GUID格式CorrelationId
- Server端从X-Correlation-ID请求头读取
- 无请求头时Server端自动生成

**CorrelationId传递**:
- HTTP请求头: X-Correlation-ID
- 日志属性: CorrelationId
- 响应头: X-Correlation-ID(回传)

#### Scenario: Client端发起请求
- **WHEN** Client发起HTTP请求
- **THEN** DelegatingHandler SHALL 注入X-Correlation-ID头
- **AND** CorrelationId SHALL 通过LogContext传递
- **AND** 该请求的所有日志 SHALL 包含相同CorrelationId

#### Scenario: Server端接收请求
- **WHEN** Server收到HTTP请求
- **THEN** CorrelationIdMiddleware SHALL 读取X-Correlation-ID头
- **AND** 无请求头时 SHALL 生成新的CorrelationId
- **AND** CorrelationId SHALL 存入HttpContext.Items
- **AND** 响应头 SHALL 包含X-Correlation-ID

#### Scenario: 跨服务日志关联
- **WHEN** 排查问题需要关联日志
- **THEN** 可通过CorrelationId搜索Client端日志
- **AND** 可通过相同CorrelationId搜索Server端日志
- **AND** 两端日志 SHALL 可完整重建请求链路

---

### Requirement: LOG-003 敏感数据脱敏

日志输出 SHALL 自动对敏感数据进行脱敏处理。

**敏感数据类型**:
- ContactInfo: 手机号、电话号码
- IdentityInfo: 身份证号、护照号
- PersonalInfo: 地址、姓名(部分场景)
- MedicalInfo: 病史、诊断信息
- CredentialInfo: 密码、Token、连接字符串

**脱敏模式**:
- Partial: 部分显示(如138****5678)
- Full: 完全隐藏([已隐藏])
- Hash: 哈希标识([REDACTED:abc123])

#### Scenario: 结构化日志脱敏
- **WHEN** 记录包含[SensitiveData]属性的对象
- **THEN** SensitiveDataDestructuringPolicy SHALL 自动脱敏
- **AND** 脱敏模式 SHALL 按属性配置执行
- **AND** 非敏感属性 SHALL 保持原值

#### Scenario: 文本日志脱敏
- **WHEN** 日志消息包含敏感模式(如password=xxx)
- **THEN** LogSanitizer SHALL 进行正则脱敏
- **AND** 连接字符串 SHALL 脱敏Password部分
- **AND** Bearer Token SHALL 完全脱敏

#### Scenario: API响应脱敏
- **WHEN** API返回包含敏感属性的DTO
- **THEN** SensitiveDataJsonConverterFactory SHALL 自动脱敏
- **AND** 反序列化(接收数据)时 SHALL NOT 脱敏

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

异常处理 SHALL 记录完整的结构化日志。

**异常日志必含信息**:
- CorrelationId: 请求追踪ID
- ExceptionType: 异常类型名
- Message: 异常消息
- StackTrace: 完整堆栈(通过{Exception}模板)
- RequestPath: 请求路径(Server端)
- RequestMethod: 请求方法(Server端)

#### Scenario: Server端全局异常日志
- **WHEN** GlobalExceptionHandler捕获异常
- **THEN** SHALL 使用LogError记录
- **AND** SHALL 包含结构化异常详情
- **AND** 业务异常(AppException) SHALL 记录为Warning
- **AND** 系统异常 SHALL 记录为Error

#### Scenario: Client端异常日志
- **WHEN** StandardExceptionHandler处理异常
- **THEN** SHALL 使用LogError记录
- **AND** SHALL 包含CorrelationId
- **AND** SHALL 包含操作上下文信息

---

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

生产环境 SHALL 支持通过API动态调整日志级别。

**控制要求**:
- 使用Serilog LoggingLevelSwitch
- 仅Admin角色可调整
- 级别变更记录审计日志

#### Scenario: 动态开启Debug日志
- **WHEN** Admin用户调用POST /api/admin/logging/level
- **AND** 请求体Level为"Debug"
- **THEN** LoggingLevelSwitch SHALL 切换到Debug级别
- **AND** 后续所有Debug日志 SHALL 被记录
- **AND** 级别变更 SHALL 记录Warning日志

#### Scenario: 非Admin用户无法调整日志级别
- **WHEN** 非Admin用户调用POST /api/admin/logging/level
- **THEN** 响应状态码 SHALL 为403 Forbidden

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

