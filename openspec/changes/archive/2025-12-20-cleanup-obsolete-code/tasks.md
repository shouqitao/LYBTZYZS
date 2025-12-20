# Tasks: 清理过时代码

## Phase 1: 更新引用到共享组件

- [x] 更新`BaseApiController.cs`使用`LYBT.Shared.Logging.Masking.SensitiveDataMasker`
- [x] 更新`SensitiveDataJsonConverterFactory.cs`使用`LYBT.Shared.Logging.Masking.SensitiveDataMasker`
- [x] 更新`Program.cs`使用`LYBT.Shared.Logging.Management.LoggingLevelManager`
- [x] 更新`DiagnosticsController.cs`使用`LYBT.Shared.Logging.Management.LoggingLevelManager`
- [x] 清理`DatabaseServiceCollectionExtensions.cs`无用的using
- [x] 验证Server端编译通过

## Phase 2: 删除Server端过时日志组件

- [x] 删除`LYBT.Infrastructure/Logging/CorrelationIdEnricher.cs`
- [x] 删除`LYBT.Infrastructure/Logging/LoggingLevelManager.cs`
- [x] 删除`LYBT.Infrastructure/Logging/SensitiveDataMasker.cs`
- [x] 删除`LYBT.Infrastructure/Logging/SensitiveDataDestructuringPolicy.cs`
- [x] 验证Server端编译通过

## Phase 3: 删除Desktop端过时日志组件

- [x] 删除`LYBT.Desktop.Infrastructure/Logging/CorrelationIdEnricher.cs`
- [x] 验证Desktop端编译通过

## Phase 4: 删除过时异常处理组件

- [x] 删除`LYBT.WebAPI/Middleware/GlobalExceptionHandler.cs`
- [x] 验证WebAPI编译通过

## Phase 5: 清理SerilogExtensions

- [x] 移除`SerilogExtensions.cs`中引用已删除组件的using语句
- [x] 验证编译通过

## Phase 6: 测试验证

- [x] 运行全解决方案构建，确认0错误0警告
- [x] 运行相关单元测试
- [x] 验证DiagnosticsController测试通过

## Phase 7: 更新测试引用

- [x] 更新`DiagnosticsControllerTests.cs`使用共享LoggingLevelManager
- [x] 运行所有测试确认通过
