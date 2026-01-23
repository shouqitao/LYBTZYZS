# implement-single-instance-mode Tasks

## Overview

- **变更类型**: Enhancement
- **风险等级**: Low
- **预估工作量**: 1-2小时

## Phase 1: WPF Shell 单实例模式

### 1.1 创建 NativeMethods.cs [DONE]
- **文件**: `src/Client/Desktop/Shell/NativeMethods.cs` (新建)
- **变更**:
  - 创建 `NativeMethods` 静态类
  - 使用传统 `DllImport` 声明 P/Invoke (兼容性更好)
  - 封装 `FindWindow`, `SetForegroundWindow`, `ShowWindow`, `IsIconic`
  - 提供 `ActivateExistingWindow` 高层API
- **验证**: 文件编译无警告

### 1.2 修改 App.xaml.cs - 添加单实例检查 [DONE]
- **文件**: `src/Client/Desktop/Shell/App.xaml.cs`
- **变更**:
  - 添加字段: `private static Mutex? _instanceMutex;`
  - 添加常量: `MutexName`, `MainWindowTitle`
  - 修改 `OnStartup`: 在L40处添加单实例检查，位于 `SetConsoleEncoding()` 之前
  - 添加方法: `TryAcquireSingleInstance()`
- **验证**: 编译通过，多次启动只有一个进程

### 1.3 编译验证 [DONE]
- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误和警告

## Phase 2: 完善资源释放机制

### 2.1 修改 App.xaml.cs - 完善OnExit [DONE]
- **文件**: `src/Client/Desktop/Shell/App.xaml.cs`
- **变更**:
  - 添加 `using LYBT.Desktop.Contracts.Services;` (IApplicationTickService)
  - 添加 `using LYBT.Desktop.Foundation.Security;` (ITokenLifecycleService)
  - 添加 `using Microsoft.Extensions.Caching.Memory;` (IMemoryCache)
  - 重写 `OnExit` 方法，按顺序释放:
    1. IApplicationTickService (Stop + Dispose)
    2. ITokenLifecycleService (StopMonitoring + Dispose)
    3. IUserActivityTracker (Dispose)
    4. IMemoryCache (Dispose)
    5. Mutex (ReleaseMutex + Dispose)
    6. 最后关闭日志
  - 添加方法: `SafeDispose(Action, string)`
- **依赖**: Phase 1完成
- **验证**: 退出后任务管理器无残留Shell进程

### 2.2 编译验证 [DONE]
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

### 2.3 手动功能测试 [PENDING - 需用户验证]
- 正常退出应用
- 检查任务管理器确认Shell.exe进程已结束
- 查看日志确认资源释放顺序

## Phase 3: WebAPI端口检测 (Deferred)

> **状态**: 延迟执行 - 当前WebAPI已有UseWindowsService保护，优先级较低

- [ ] 3.1 评估是否需要端口检测
- [ ] 3.2 如需要，修改 `Program.cs` 添加检测逻辑

## Dependencies

```
Phase 1 ─────────────────────┐
  1.1 NativeMethods.cs       │
  1.2 单实例检查             │
  1.3 编译验证               │
                             ▼
Phase 2 ─────────────────────┐
  2.1 OnExit资源释放         │
  2.2 编译验证               │
  2.3 功能测试               │
                             ▼
Phase 3 (Deferred) ──────────┘
```

Phase 1 → Phase 2 顺序执行（单实例机制是资源释放的前提）

## Validation Checklist

- [x] Desktop解决方案编译通过 (零错误)
- [ ] 多次启动Shell.exe只有一个进程运行 (需手动测试)
- [ ] 第二次启动时自动激活已有窗口 (需手动测试)
- [ ] 正常退出后任务管理器无Shell.exe残留 (需手动测试)
- [ ] 日志显示资源释放顺序正确 (需手动测试)
- [ ] 不影响现有登录、数据加载等功能 (需手动测试)

## Notes

- `NativeMethods.cs` 使用传统 `DllImport` 而非 `LibraryImport`，避免需要启用 AllowUnsafeBlocks
- Mutex使用 `Global\\` 前缀确保跨用户会话单实例
- 资源释放使用独立try-catch，确保单个失败不影响整体清理

---

**生成时间**: 2026-01-23 09:18
**执行完成**: 2026-01-23 09:32
**状态**: 代码变更完成，待手动功能验证
