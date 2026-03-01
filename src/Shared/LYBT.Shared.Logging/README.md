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
