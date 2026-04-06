# LYBT.Shared.Logging

> Serilog 统一日志 | CorrelationId 追踪 | 敏感数据脱敏 | 动态级别管理

## 项目定位

- **层级**: Shared
- **职责**: 提供 Serilog 日志配置、请求关联 ID 追踪、敏感数据自动脱敏、运行时日志级别管理
- **状态**: Active

## 目录结构

```
LYBT.Shared.Logging/
├── Abstractions/
│   ├── ICorrelationIdProvider.cs         # 关联 ID 抽象
│   ├── ActivityCorrelationIdProvider.cs   # Server 实现 (Activity)
│   └── AsyncLocalCorrelationIdProvider.cs # Desktop 实现 (AsyncLocal)
├── Enrichers/
│   └── CorrelationIdEnricher.cs          # Serilog Enricher
├── Extensions/
│   ├── LoggerConfigurationExtensions.cs  # Serilog 配置扩展
│   └── ServiceCollectionExtensions.cs    # DI 注册
├── Management/
│   ├── LoggingLevelManager.cs            # 运行时日志级别切换
│   └── DebugModeInfo.cs                  # 调试模式信息
├── Masking/
│   ├── SensitiveDataDestructuringPolicy.cs # Serilog 脱敏策略
│   ├── SensitiveDataMasker.cs            # 脱敏处理器
│   └── SensitiveDataTypes.cs             # 敏感字段定义
└── TraceContext.cs                        # 追踪上下文
```

## 核心接口

| 名称 | 说明 |
|------|------|
| ICorrelationIdProvider | 请求关联 ID 提供者 (Server/Desktop 双实现) |
| CorrelationIdEnricher | Serilog Enricher，自动注入关联 ID |
| SensitiveDataMasker | 密码/身份证/手机号等敏感数据自动脱敏 |
| LoggingLevelManager | 运行时动态切换日志级别 (LoggingLevelSwitch) |

## 设计依据

- ICorrelationIdProvider 接口解耦 HttpContext 依赖，支持双端不同实现
- 脱敏策略通过 Serilog IDestructuringPolicy 自动拦截，业务代码无感
- 运行时级别管理支持调试模式快速切换，无需重启

## 依赖关系

### 依赖
- Serilog (NuGet, 核心日志库)
- Microsoft.Extensions.Logging.Abstractions

### 被依赖
- LYBT.Infrastructure (Server 日志配置)
- LYBT.Desktop.Infrastructure (Desktop 日志配置)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建 README |
| 2025-12 | 敏感数据脱敏策略添加 |
| 2025-11 | CorrelationId 双端实现 |

## 开发笔记

# LYBT.Shared.Logging 代码知识

基于 Serilog 的共享日志基础设施，提供 CorrelationId 追踪、敏感数据脱敏、运行时日志级别管理等功能，供 Server 和 Desktop 两端共用。

## 代码文件结构

```
Abstractions/
├── ICorrelationIdProvider.cs           # CorrelationId 提供者接口
├── AsyncLocalCorrelationIdProvider.cs  # Desktop 端 AsyncLocal 实现
└── ActivityCorrelationIdProvider.cs    # 基于 Activity API 的实现
Masking/
├── SensitiveDataTypes.cs               # 敏感数据特性 + SensitiveDataType/MaskingMode 枚举
├── SensitiveDataDestructuringPolicy.cs # Serilog 自动脱敏解构策略
└── SensitiveDataMasker.cs              # 统一脱敏入口 (属性级 + 文本级)
Management/
├── DebugModeInfo.cs                    # 调试模式状态 DTO
└── LoggingLevelManager.cs              # 运行时日志级别动态管理
Enrichers/
└── CorrelationIdEnricher.cs            # CorrelationId 日志富集器 + 扩展方法
Extensions/
├── LoggerConfigurationExtensions.cs    # Serilog LoggerConfiguration 扩展
└── ServiceCollectionExtensions.cs      # [SUSPECT] DI 注册扩展 (AddSharedLogging 未被调用)
TraceContext.cs                         # 分布式追踪上下文辅助类
```

### Abstractions/ICorrelationIdProvider.cs
**ICorrelationIdProvider** : interface | CorrelationId 提供者接口，解耦 HttpContext 依赖

| 方法 | 说明 |
|------|------|
| GetCorrelationId() | 获取当前 CorrelationId，不存在返回 null |
| SetCorrelationId(correlationId) | 设置当前 CorrelationId |

### Abstractions/AsyncLocalCorrelationIdProvider.cs
**AsyncLocalCorrelationIdProvider** : ICorrelationIdProvider | Desktop 端实现，使用 AsyncLocal 在异步上下文传递

| 方法 | 说明 |
|------|------|
| GetCorrelationId() | 获取 AsyncLocal 中的 CorrelationId |
| SetCorrelationId(correlationId) | 设置 AsyncLocal 值 |
| GetCorrelationIdOrDefault() | 获取 CorrelationId，不存在返回 "N/A" |
| GetOrNew() | 获取或自动生成新的 CorrelationId |
| Clear() | 清除当前 CorrelationId |
| GenerateAndSet() | 生成新 Guid 并设置为当前值 |

### Abstractions/ActivityCorrelationIdProvider.cs
**ActivityCorrelationIdProvider** : ICorrelationIdProvider | 基于 System.Diagnostics.Activity 的实现，使用 W3C TraceId

| 方法 | 说明 |
|------|------|
| GetCorrelationId() | 获取 Activity.Current 的 TraceId |
| SetCorrelationId(correlationId) | 空实现，Activity 不支持手动设置 TraceId |
| GetCorrelationIdOrNew() | 获取 TraceId，不存在则生成新 Guid |

### Masking/SensitiveDataTypes.cs
**SensitiveDataAttribute** : Attribute | 标记需要日志脱敏的属性，属性: DataType/RequireLogMasking/MaskingMode

**SensitiveDataType** | 敏感数据类型枚举: PersonalInfo/MedicalInfo/ContactInfo/IdentityInfo/FinancialInfo

**MaskingMode** | 脱敏模式枚举: Default(中间替代)/Partial(前后保留)/Full(完全隐藏)/Hash(哈希)

### Masking/SensitiveDataDestructuringPolicy.cs
**SensitiveDataDestructuringPolicy** : IDestructuringPolicy | Serilog 解构策略，自动对标记 [SensitiveData] 的属性脱敏

| 方法 | 说明 |
|------|------|
| TryDestructure(value, factory, out result) | 解构对象，对敏感字段调用 SensitiveDataMasker.Mask |

### Masking/SensitiveDataMasker.cs
**SensitiveDataMasker** : static partial class | 统一敏感数据脱敏入口，整合属性级和文本级脱敏

| 方法 | 说明 |
|------|------|
| Mask(value, mode, dataType) | 根据脱敏模式处理字符串值 |
| GetSensitiveDataAttribute(property) | 检查属性是否标记 [SensitiveData] |
| MaskObject(obj) | 对对象所有敏感字段进行脱敏，返回字典 |
| MaskUri(uri) | URI 敏感参数脱敏 (password/token/key 等) |
| SanitizeText(input) | 文本级脱敏 (密码/连接字符串/Bearer Token) |
| IsSensitiveFieldName(fieldName) | 检查字段名是否为敏感字段 |
| SerializeWithSanitization(obj) | 脱敏后 JSON 序列化 |
| SanitizeException(exception, maxLines) | 异常信息脱敏 (限制堆栈行数) |

### Management/DebugModeInfo.cs
**DebugModeInfo** | 调试模式状态 DTO，属性: IsActive/PreviousLevel/CurrentLevel/DefaultLevel/StartedAt/ExpiresAt/DurationMinutes

### Management/LoggingLevelManager.cs
**LoggingLevelManager** : IDisposable | 运行时日志级别动态管理，支持调试模式自动过期

| 方法 | 说明 |
|------|------|
| EnableDebugMode(level, durationMinutes) | 启用调试模式，默认 30 分钟自动过期 |
| DisableDebugMode() | 禁用调试模式，恢复默认日志级别 |
| GetStatus() | 获取当前调试模式状态 |
| SetLevel(level) | 直接设置日志级别 |

### Enrichers/CorrelationIdEnricher.cs
**CorrelationIdEnricher** : ILogEventEnricher | 日志富集器，通过 ICorrelationIdProvider 添加 CorrelationId 属性

| 方法 | 说明 |
|------|------|
| Enrich(logEvent, propertyFactory) | 如果 LogContext 中无 CorrelationId 则从 Provider 获取并添加 |

**CorrelationIdEnricherExtensions** : static class | 扩展方法类

| 方法 | 说明 |
|------|------|
| WithCorrelationId(config, provider) | LoggerEnrichmentConfiguration 扩展，注册 CorrelationIdEnricher |

### Extensions/LoggerConfigurationExtensions.cs
**LoggerConfigurationExtensions** : static class | Serilog 配置扩展，定义输出模板和统一配置入口

| 方法 | 说明 |
|------|------|
| UseSharedLogging(config, provider) | 统一配置: LogContext + MachineName + ThreadId + CorrelationId + 脱敏 |
| WithSensitiveDataMasking(config) | 仅启用敏感数据脱敏解构策略 |
| WriteToConsoleWithTemplate(config, level, template) | 配置控制台输出 (默认模板含 CorrelationId) |
| WriteToFileWithTemplate(config, path, level, template, interval, limit) | 配置文件输出 (默认 Day 滚动，保留 31 天) |

### Extensions/ServiceCollectionExtensions.cs
**ServiceCollectionExtensions** : static class | DI 注册扩展

| 方法 | 说明 |
|------|------|
| AddSharedLogging(services, defaultLevel) | 注册 LoggingLevelManager 单例 |
| AddSharedLogging\<T\>(services, defaultLevel) | 注册 LoggingLevelManager + 指定的 ICorrelationIdProvider |
| AddAsyncLocalCorrelationIdProvider(services) | 注册 AsyncLocalCorrelationIdProvider (Desktop 端) |

### TraceContext.cs
**TraceContext** : static class | 分布式追踪上下文辅助类，基于 Activity API

| 方法/属性 | 说明 |
|-----------|------|
| CurrentTraceId | 获取当前 TraceId (可能为 null) |
| TraceIdOrNew | 获取 TraceId，不存在则生成 Guid |
| CurrentSpanId | 获取当前 SpanId (可能为 null) |
| StartActivity(operationName) | 启动新 Activity 用于追踪操作 |
| HasActiveTrace | 是否有活动追踪上下文 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| ServiceCollectionExtensions.AddSharedLogging | [SUSPECT] | Server 端 Program.cs 直接使用 LoggingLevelManager 构造 | 评估是否应迁移调用方使用此扩展方法 |
| ServiceCollectionExtensions.AddAsyncLocalCorrelationIdProvider | [SUSPECT] | Desktop 端直接注册 | 评估是否应迁移调用方使用此扩展方法 |
| CorrelationIdEnricherExtensions | [SUSPECT] | 仅被 LoggerConfigurationExtensions.UseSharedLogging 内部调用 | 内部实现类，保留 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| Masking/SensitiveDataTypes.cs | Logging 层定义了 SensitiveDataAttribute，但 Entities 层也有同名类型 | Entities 的 SensitiveDataAttribute 用于 JSON 序列化脱敏 (SensitiveDataJsonConverterFactory)，Logging 的用于 Serilog 日志脱敏，两者独立但概念重复 | 评估合并为统一特性或保持职责分离 |
| Extensions/ServiceCollectionExtensions.cs | AddSharedLogging/AddAsyncLocalCorrelationIdProvider 未被外部调用 | Server 端 Program.cs 直接 new LoggingLevelManager；Desktop 端可能直接注册 | 推广使用或移除，避免维护未使用代码 |
| BusinessRules 在 Logging 层 | 无，此为 Validators 项目分析项 | - | - |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| SensitiveDataAttribute 存在两个独立定义 | Logging (LYBT.Shared.Logging.Masking) 和 Entities (LYBT.Entities.Attributes) 各有一个，枚举值不同 | 使用时注意引用正确命名空间；Logging 版本用于日志，Entities 版本用于 API 序列化 |
| ActivityCorrelationIdProvider.SetCorrelationId 是空操作 | Activity API 不支持手动设置 TraceId | 需要新追踪上下文时应启动新 Activity，而非调用 Set |
| LoggingLevelManager 调试模式过期依赖 Timer | Timer 回调在线程池执行，可能与主线程竞争 | 已使用 lock 保护状态一致性 |
