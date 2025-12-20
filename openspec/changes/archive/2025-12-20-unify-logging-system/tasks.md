# Tasks: 统一日志系统项目

## Phase 1: 创建项目结构

- [x] 创建`src/Shared/LYBT.Shared.Logging/`目录
- [x] 创建`LYBT.Shared.Logging.csproj`项目文件
- [x] 添加项目到`LYBT.All.sln`解决方案
- [x] 创建目录结构: Abstractions, Configuration, Enrichers, Masking, Management, Extensions
- [x] 验证项目编译通过

## Phase 2: 迁移核心接口和抽象

- [x] 创建`Abstractions/ICorrelationIdProvider.cs`接口
- [x] 创建`Abstractions/AsyncLocalCorrelationIdProvider.cs` (Desktop实现)
- [x] 验证编译通过

## Phase 3: 迁移敏感数据脱敏组件

- [x] 创建`Masking/SensitiveDataTypes.cs` - 包含SensitiveDataAttribute和枚举
- [x] 创建`Masking/SensitiveDataMasker.cs` - 脱敏逻辑实现
- [x] 创建`Masking/SensitiveDataDestructuringPolicy.cs` - Serilog策略
- [x] 验证编译通过

## Phase 4: 迁移日志管理组件

- [x] 创建`Management/DebugModeInfo.cs`
- [x] 创建`Management/LoggingLevelManager.cs`
- [x] 验证编译通过

**Note**: LogCleanupService保留在Server层，因为依赖DbContext

## Phase 5: 创建统一Enrichers

- [x] 创建`Enrichers/CorrelationIdEnricher.cs`(使用ICorrelationIdProvider)
- [x] 验证编译通过

## Phase 6: 创建配置扩展

- [x] 创建`Extensions/LoggerConfigurationExtensions.cs`
- [x] 创建`Extensions/ServiceCollectionExtensions.cs`
- [x] 验证编译通过

## Phase 7: 更新Server层引用

- [x] 更新`LYBT.Infrastructure.csproj`引用`LYBT.Shared.Logging`
- [x] 创建`HttpContextCorrelationIdProvider`实现
- [x] 更新`SerilogExtensions.cs`使用共享组件
- [x] 标记旧组件为[Obsolete]以保持向后兼容
- [x] 验证编译通过

**Note**: 保留Serilog包引用和旧组件用于向后兼容

## Phase 8: 更新Desktop层引用

- [x] 更新`LYBT.Desktop.Infrastructure.csproj`引用`LYBT.Shared.Logging`
- [x] 创建`FoundationCorrelationIdProvider`桥接Foundation层
- [x] 更新`DesktopSerilogConfiguration.cs`使用共享组件
- [x] 标记旧`CorrelationIdEnricher`为[Obsolete]
- [x] 验证编译通过

## Phase 9: WebAPI配置

- [x] WebAPI保留直接Serilog.AspNetCore引用
- [x] 通过LYBT.Infrastructure间接使用共享日志组件

## Phase 10: 测试验证

- [x] 运行全解决方案构建验证
- [x] 确认0错误，仅有预期的[Obsolete]警告

## Phase 11: 清理和文档

- [x] 标记旧组件为[Obsolete]（替代删除以保持向后兼容）
- [x] 更新tasks.md完成状态

---

## 实现摘要

### 创建的新文件

**LYBT.Shared.Logging项目** (`src/Shared/LYBT.Shared.Logging/`):
- `LYBT.Shared.Logging.csproj`
- `Abstractions/ICorrelationIdProvider.cs`
- `Abstractions/AsyncLocalCorrelationIdProvider.cs`
- `Masking/SensitiveDataTypes.cs`
- `Masking/SensitiveDataMasker.cs`
- `Masking/SensitiveDataDestructuringPolicy.cs`
- `Management/DebugModeInfo.cs`
- `Management/LoggingLevelManager.cs`
- `Enrichers/CorrelationIdEnricher.cs`
- `Extensions/LoggerConfigurationExtensions.cs`
- `Extensions/ServiceCollectionExtensions.cs`

**适配器文件**:
- `LYBT.Infrastructure/Logging/HttpContextCorrelationIdProvider.cs`
- `LYBT.Desktop.Infrastructure/Logging/FoundationCorrelationIdProvider.cs`

### 标记为[Obsolete]的组件

**Server端**:
- `LYBT.Infrastructure.Logging.CorrelationIdEnricher`
- `LYBT.Infrastructure.Logging.LoggingLevelManager`
- `LYBT.Infrastructure.Logging.SensitiveDataMasker`
- `LYBT.Infrastructure.Logging.SensitiveDataDestructuringPolicy`

**Desktop端**:
- `LYBT.Desktop.Infrastructure.Logging.CorrelationIdEnricher`

### 构建结果

- **错误**: 0
- **警告**: 7 (全部为预期的[Obsolete]警告)
