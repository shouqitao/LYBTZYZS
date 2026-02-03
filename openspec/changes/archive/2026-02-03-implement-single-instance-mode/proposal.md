# implement-single-instance-mode

## Why

### 发现的问题

WebAPI和WPF Shell应用存在**后台多进程驻留**问题，主要原因是缺乏单实例机制：

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| `Shell/App.xaml.cs` | 缺乏单实例检查 | 允许多实例运行 | Mutex单实例模式 |
| `Shell/App.xaml.cs:56-62` | 资源释放不完整 | 仅关闭日志 | 完整释放所有IDisposable |
| `WebAPI/Program.cs` | 开发模式多实例 | 无端口占用检测 | 检测并提示 |

### 影响分析

**后台驻留的表现：**
1. 用户多次点击启动应用，每次都创建新进程
2. 关闭窗口后进程仍在后台运行（Timer/HttpClient未释放）
3. 系统资源被持续占用

**根因分析：**

| 组件 | 潜在问题 | 风险等级 |
|------|----------|----------|
| WPF Shell - 无Mutex | 允许多实例运行 | **高** |
| TokenLifecycleService | System.Threading.Timer未显式Dispose | 中 |
| ApplicationTickService | DispatcherTimer未显式Stop | 中 |
| HttpClient单例 | 连接池未显式释放 | 低 |
| WebAPI开发模式 | 多实例端口冲突 | 低 |

## What Changes

### Phase 1: WPF Shell 单实例模式

实现基于Mutex的单实例检查，确保同一时间只有一个Shell进程运行。

**核心实现：**
1. 在`App.OnStartup`中创建命名Mutex
2. 如果Mutex已存在，激活已有窗口并退出
3. 使用Windows API (`FindWindow`/`SetForegroundWindow`) 激活已有实例

**关键代码位置：**
- `Shell/App.xaml.cs` - 添加单实例检查
- `Shell/NativeMethods.cs` (新建) - Windows API封装

### Phase 2: 完善资源释放机制

确保应用退出时所有资源正确释放，防止僵尸进程。

**清理项目：**
1. `IApplicationTickService` - 停止DispatcherTimer
2. `ITokenLifecycleService` - 停止监控Timer
3. `HttpClient` - 释放连接池
4. `IMemoryCache` - 清理缓存
5. Prism Container - 释放DI容器

### Phase 3: WebAPI端口占用检测（可选）

开发模式下检测端口占用，给出友好提示。

## Architecture

### 变更影响范围

```
src/Client/Desktop/Shell/
├── App.xaml.cs                    [修改] 添加单实例检查和资源释放
├── NativeMethods.cs               [新建] Windows API P/Invoke封装
└── Services/
    └── SingleInstanceService.cs   [新建] 单实例管理服务（可选）

src/Server/Services/LYBT.WebAPI/
└── Program.cs                     [修改] 添加端口占用检测（可选）
```

### 单实例实现方案

```
┌─────────────────────────────────────────────────────────────┐
│  App.OnStartup()                                            │
│  ─────────────                                              │
│  1. 尝试创建命名Mutex: "Global\\LYBTZYZS_Shell_Instance"    │
│     │                                                       │
│     ├─ 成功创建 (createdNew=true)                           │
│     │   → 正常启动应用                                       │
│     │                                                       │
│     └─ Mutex已存在 (createdNew=false)                       │
│         → 查找已有窗口 (FindWindow)                          │
│         → 激活已有窗口 (SetForegroundWindow)                 │
│         → 退出当前进程 (Shutdown)                            │
└─────────────────────────────────────────────────────────────┘
```

### 资源释放流程

```
┌─────────────────────────────────────────────────────────────┐
│  App.OnExit()                                               │
│  ────────────                                               │
│  1. 停止定时服务                                             │
│     ├─ IApplicationTickService.Dispose()                    │
│     └─ ITokenLifecycleService.Dispose()                     │
│                                                             │
│  2. 释放HTTP资源                                             │
│     └─ HttpClient.Dispose()                                 │
│                                                             │
│  3. 清理缓存                                                 │
│     └─ IMemoryCache.Dispose()                               │
│                                                             │
│  4. 释放DI容器                                               │
│     └─ Container.Dispose()                                  │
│                                                             │
│  5. 释放Mutex                                                │
│     └─ _mutex?.ReleaseMutex()                               │
│                                                             │
│  6. 关闭日志                                                 │
│     └─ Log.CloseAndFlush()                                  │
└─────────────────────────────────────────────────────────────┘
```

## Impact

- **文件变更**: 3-4个文件
- **风险等级**: Low - 独立功能，不影响业务逻辑
- **测试要求**: 
  - 手动测试：多次启动应用验证单实例
  - 手动测试：正常退出后验证进程结束

## Risks

| 风险 | 缓解措施 |
|------|----------|
| Mutex名称冲突 | 使用 `Global\\` 前缀 + 唯一应用标识 |
| 已有窗口激活失败 | 提供fallback：显示提示消息后退出 |
| 资源释放异常 | 使用try-catch包裹，确保后续清理继续 |
| 强制退出丢失数据 | 仅在正常退出流程中清理，不添加强制退出 |

## Non-Goals

以下内容**不在**本提案范围内：
- 将WebAPI和Shell合并为单进程（违反关注点分离原则）
- 实现IPC进程间通信（过度设计）
- 添加强制退出机制（可能导致数据丢失）

## References

- 用户需求: 解决WebAPI和Shell后台多进程驻留问题
- 技术参考: Windows Mutex单实例模式
- 相关分析: brainstorm会话中的问题诊断

## Success Criteria

1. 多次点击Shell.exe只启动一个进程
2. 第二次启动时自动激活已有窗口
3. 正常退出后任务管理器中无残留进程
4. 不影响现有功能和用户体验
