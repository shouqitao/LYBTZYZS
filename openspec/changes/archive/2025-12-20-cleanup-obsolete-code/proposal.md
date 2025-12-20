# Proposal: 清理过时代码

## Why

当前项目中存在多个标记为`[Obsolete]`的过时组件，这些组件在统一日志系统(`unify-logging-system`)和异常处理重构后已被新的共享组件替代。保留这些过时代码会：

1. **增加维护负担** - 需要维护两套实现
2. **产生编译警告** - 当前构建产生7个[Obsolete]警告
3. **造成混淆** - 开发者可能误用旧组件
4. **占用磁盘空间** - 无用代码增加项目体积

## What Changes

### 删除过时日志组件

**Server端 (LYBT.Infrastructure.Logging)**:
- `CorrelationIdEnricher.cs` - 已由`LYBT.Shared.Logging.Enrichers.CorrelationIdEnricher`替代
- `CorrelationIdEnricherExtensions` - 已由`SerilogExtensions.WithHttpContextCorrelationId`替代
- `LoggingLevelManager.cs` - 已由`LYBT.Shared.Logging.Management.LoggingLevelManager`替代
- `SensitiveDataMasker.cs` - 已由`LYBT.Shared.Logging.Masking.SensitiveDataMasker`替代
- `SensitiveDataDestructuringPolicy.cs` - 已由`LYBT.Shared.Logging.Masking.SensitiveDataDestructuringPolicy`替代

**Desktop端 (LYBT.Desktop.Infrastructure.Logging)**:
- `CorrelationIdEnricher.cs` - 已由`LYBT.Shared.Logging.Enrichers.CorrelationIdEnricher`替代

### 删除过时异常处理组件

**WebAPI (LYBT.WebAPI.Middleware)**:
- `GlobalExceptionHandler.cs` - 已由`BusinessExceptionHandler`和`SystemExceptionHandler`替代

### 更新引用

**需迁移到共享组件的文件**:
- `BaseApiController.cs` - 改用`LYBT.Shared.Logging.Masking.SensitiveDataMasker`
- `SensitiveDataJsonConverterFactory.cs` - 改用`LYBT.Shared.Logging.Masking.SensitiveDataMasker`
- `Program.cs` - 改用`LYBT.Shared.Logging.Management.LoggingLevelManager`
- `DiagnosticsController.cs` - 改用`LYBT.Shared.Logging.Management.LoggingLevelManager`
- `DatabaseServiceCollectionExtensions.cs` - 移除无用的using声明

## Scope

- **影响层**: Server (Infrastructure, WebAPI), Desktop (Infrastructure)
- **风险级别**: 低 - 仅删除已废弃且有替代方案的代码
- **回归风险**: 低 - 所有替代组件已在`unify-logging-system`中测试验证

## Success Criteria

1. 全解决方案构建 0错误 0警告
2. 所有单元测试通过
3. 无[Obsolete]警告
