# Proposal: 统一日志系统项目 (unify-logging-system)

## Why

当前日志系统代码分散在两个位置:
- **Server层**: `src/Server/Core/LYBT.Infrastructure/Logging/` (6个文件)
- **Desktop层**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Logging/` (2个文件)

两层各自维护Serilog依赖和配置，存在以下问题:
1. **代码重复**: CorrelationIdEnricher在两层各有一份实现
2. **依赖分散**: Serilog包在多个csproj中重复引用
3. **维护困难**: 日志配置修改需要同步多处
4. **职责不清**: 敏感数据脱敏、日志清理等通用逻辑放在Server层

## 目标

参照`LYBT.Shared.ExceptionHandling`项目的成功模式，创建`LYBT.Shared.Logging`项目，统一管理:
1. 所有Serilog依赖和配置
2. 通用日志组件(Enrichers, Destructuring Policies)
3. 敏感数据脱敏逻辑
4. 日志清理服务
5. 动态日志级别控制
6. 前后端特定的日志配置扩展

## 方案

### Phase 1: 创建LYBT.Shared.Logging项目
- 创建`src/Shared/LYBT.Shared.Logging/`项目
- 定义项目结构和依赖

### Phase 2: 迁移通用日志组件
- 迁移`SensitiveDataMasker`和`SensitiveDataDestructuringPolicy`
- 迁移`LoggingLevelManager`和`DebugModeInfo`
- 创建统一的`CorrelationIdEnricher`(支持Server和Desktop)
- 创建`ICorrelationIdProvider`接口解耦HttpContext依赖

### Phase 3: 创建配置扩展
- 创建`LoggingConfiguration`基类
- 创建`ServerLoggingConfiguration`服务端配置
- 创建`DesktopLoggingConfiguration`客户端配置
- 提供DI扩展方法

### Phase 4: 迁移日志清理服务
- 迁移`LogCleanupService`和`LogCleanupOptions`
- 提取数据库无关的清理逻辑

### Phase 5: 更新项目引用
- 更新`LYBT.Infrastructure`引用`LYBT.Shared.Logging`
- 更新`LYBT.Desktop.Infrastructure`引用`LYBT.Shared.Logging`
- 删除原有Logging目录

## 预期成果

### 项目结构
```
src/Shared/LYBT.Shared.Logging/
├── Abstractions/
│   ├── ICorrelationIdProvider.cs       # CorrelationId提供者接口
│   └── ILoggingConfiguration.cs        # 日志配置接口
├── Configuration/
│   ├── LoggingConfigurationBase.cs     # 配置基类
│   ├── ServerLoggingConfiguration.cs   # Server端配置
│   └── DesktopLoggingConfiguration.cs  # Desktop端配置
├── Enrichers/
│   ├── CorrelationIdEnricher.cs        # 统一的CorrelationId Enricher
│   └── ApplicationEnricher.cs          # 应用标识Enricher
├── Masking/
│   ├── SensitiveDataMasker.cs          # 敏感数据脱敏器
│   └── SensitiveDataDestructuringPolicy.cs  # Serilog脱敏策略
├── Management/
│   ├── LoggingLevelManager.cs          # 动态日志级别管理
│   └── LogCleanupService.cs            # 日志清理服务
├── Extensions/
│   ├── LoggerConfigurationExtensions.cs # Serilog配置扩展
│   └── ServiceCollectionExtensions.cs   # DI扩展
└── LYBT.Shared.Logging.csproj
```

### 依赖变更

**LYBT.Shared.Logging将引用:**
- Serilog
- Serilog.Extensions.Logging
- Serilog.Sinks.File
- Serilog.Sinks.Console
- Serilog.Enrichers.Environment
- Serilog.Enrichers.Thread
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.DependencyInjection.Abstractions
- LYBT.Shared.Primitives (SensitiveDataAttribute)

**其他项目移除Serilog直接依赖:**
- LYBT.Infrastructure → 引用LYBT.Shared.Logging
- LYBT.Desktop.Infrastructure → 引用LYBT.Shared.Logging
- LYBT.WebAPI保留Serilog.AspNetCore和Serilog.Sinks.MSSqlServer

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 循环依赖 | 低 | 使用接口隔离,ICorrelationIdProvider解耦HttpContext |
| 编译错误 | 低 | 逐步迁移,每步验证编译 |
| 运行时问题 | 中 | 保留原有测试,添加新测试覆盖 |

## 成功标准

1. 所有日志功能正常工作
2. 编译0错误0警告
3. 现有日志相关测试全部通过
4. Server和Desktop日志配置统一管理
