# LYBT.Desktop.Shared

> 桌面客户端共享类型库：零依赖的纯 DTO/record/enum，被所有 Core、Module、Shell 项目引用。

## 项目定位

最底层的共享类型库，不依赖任何其他项目（包括 Contracts）。提供跨模块使用的数据模型、枚举、事件载荷和结果类型。所有类型均为纯数据结构，不含业务逻辑。被 `Contracts`、`Foundation`、`Infrastructure`、`Controls`、所有业务模块和 `Shell` 直接引用。

## 目录结构

```
LYBT.Desktop.Shared/
├── Enums/
│   └── UnfinishedCaseChoice.cs   # 未完成医案对话框用户选择
├── Events/
│   └── CacheEvents.cs            # 缓存失效事件（PubSubEvent）
├── Models/
│   ├── AuthState.cs              # 认证状态机（AuthState + AuthEvent + EventArgs）
│   ├── PerformanceMetric.cs      # 性能指标 + PerformanceLevel + 阈值常量
│   └── PerformanceReport.cs      # 性能报告（格式化/JSON 输出）
├── Results/
│   └── CommandResult.cs          # CommandHandler 统一返回类型
└── UI/
    └── BreadcrumbItem.cs         # 面包屑导航项
```

## 核心组件

| 类 | 设计依据 |
|---|---|
| **BreadcrumbItem** (record) — 面包屑导航项 | `Title` + `ViewName` + `IsCurrent`，用于 Shell 面包屑导航渲染 |

| 类 | 设计依据 |
|---|---|
| **CommandResult** — 无数据返回类型 | CommandHandler 统一返回，`Succeeded()`/`Failed(error)`，`implicit operator bool` |

| 类 | 设计依据 |
|---|---|
| **CommandResult\<T\>** — 泛型返回类型 | 带 `Data` 的命令结果，`Succeeded(data)`/`Failed(error)`/`NotFound(message?)`，隐式 bool 转换 |

| 类 | 设计依据 |
|---|---|
| **PerformanceMetric** (record) — 性能指标 | `OperationName`/`DurationMs`/`MemoryBeforeBytes`/`MemoryAfterBytes`/`Timestamp`，`Level` 属性自动计算 |

| 等级 | 阈值 | 描述 |
|------|------|------|
| `Excellent` | ≤ 500ms | 优秀 |
| `Good` | ≤ 1500ms | 良好 |
| `Acceptable` | ≤ 3000ms | 可接受 |
| `Poor` | > 3000ms | 需要优化 |

| 类 | 设计依据 |
|---|---|
| **PerformanceLevel** (enum) — 性能等级 | 四级评估：Excellent/Good/Acceptable/Poor |
| **PerformanceReport** (record) — 性能报告 | 聚合 `PerformanceMetric` 集合，提供 `GetFormattedReport()` 和 `GetJsonReport()` |

| 属性/方法 | 说明 |
|-----------|------|
| `TotalDurationMs` | 所有指标总耗时 |
| `TotalMemoryDeltaBytes` | 总内存增量 |
| `AverageDurationMs` | 平均操作耗时 |
| `SlowestOperation` | 最慢操作 |
| `LevelDistribution` | 按等级分组统计 |
| `GetFormattedReport()` | 格式化文本报告（含等级指示符 ✓/○/△/✗） |
| `GetJsonReport()` | JSON 格式报告（便于日志分析） |

| 类 | 设计依据 |
|---|---|
| **CacheEvents** — 缓存失效事件聚合 | 静态类，内含 `InvalidatedEvent : PubSubEvent<CacheInvalidatedPayload>` |

| 类 | 设计依据 |
|---|---|
| **CacheDomain** (enum) — 缓存域 | `Patients`/`MedicalCases`/`Herbs`/`Formulas`/`Users`/`All` |
| **CacheInvalidatedPayload** (record) | `Domain` + `Reason` + `Timestamp` |

| 类 | 设计依据 |
|---|---|
| **AuthState** (enum) — 认证状态 | 11 个状态的有限状态机，替代原有 `LoginState` + `LoginFlowState` |

| 状态 | 值 | 说明 |
|------|------|------|
| `Idle` | 0 | 空闲（未登录） |
| `Authenticating` | 1 | 正在验证凭证（手动登录） |
| `ValidatingToken` | 2 | 正在验证 Token（自动登录） |
| `LoadingProfile` | 3 | 正在加载用户资料 |
| `LoadingModules` | 4 | 正在加载模块 |
| `Navigating` | 5 | 正在导航到首页 |
| `Authenticated` | 10 | 已认证 |
| `Failed` | 20 | 认证失败 |
| `LoggingOut` | 30 | 正在登出 |
| `SessionExpired` | 40 | 会话已过期 |
| `RefreshingToken` | 50 | 正在刷新 Token |

| 类 | 设计依据 |
|---|---|
| **AuthEvent** (enum) — 认证事件 | 16 个触发器，驱动状态机转换 |

| 事件 | 说明 |
|------|------|
| `StartLogin` | 开始手动登录 |
| `StartAutoLogin` | 开始自动登录 |
| `CredentialsValidated` | 凭证验证成功 |
| `TokenValidated` | Token 验证成功 |
| `ProfileLoaded` | 用户资料加载完成 |
| `ModulesLoaded` | 模块加载完成 |
| `NavigationCompleted` | 导航完成 |
| `LoginFailure` | 登录失败 |
| `StartLogout` | 开始登出 |
| `LogoutSuccess` | 登出成功 |
| `LogoutFailure` | 登出失败 |
| `SessionExpire` | 会话过期 |
| `StartTokenRefresh` | 开始刷新 Token |
| `TokenRefreshSuccess` | Token 刷新成功 |
| `TokenRefreshFailure` | Token 刷新失败 |
| `Reset` | 重置状态机 |

| 类 | 设计依据 |
|---|---|
| **AuthStateChangedEventArgs** | `PreviousState`/`CurrentState`/`Trigger`/`StatusMessage`/`Timestamp` |
| **UnfinishedCaseChoice** (enum) | `Continue`/`CloseAndCreate`/`CloseOnly`/`Cancel`，用于未完成医案对话框 |

## 依赖关系

```
LYBT.Desktop.Shared
└── (无项目依赖 — 最底层共享库)
```

被以下项目引用：`Contracts`、`Foundation`、`Infrastructure`、`Controls`、所有 `LYBT.Desktop.*` 模块、`Shell`。

## 设计决策

1. **纯数据结构，零逻辑** — 所有类型均为 record/enum/class，不含业务逻辑，确保无循环依赖风险。
2. **合并认证状态** — 原有 `LoginState` + `LoginFlowState` 合并为单一 `AuthState` 枚举，配合 `AuthEvent` 驱动状态机转换，消除状态不一致。
3. **隐式 bool 转换** — `CommandResult`/`CommandResult<T>` 支持 `if (result)` 语法，简化 CommandHandler 返回值检查。
4. **性能等级自动计算** — `PerformanceMetric.Level` 基于阈值常量自动推导，无需手动设置。
5. **缓存域枚举** — `CacheDomain` 支持精确的缓存失效通知，`All` 用于 Sync 后全量清理。

## 已知陷阱

- **不可添加业务逻辑** — 此项目被所有模块引用，任何逻辑都会引入不必要的依赖。
- **AuthState 值不连续** — 状态值为 0-5/10/20/30/40/50，预留间隔便于插入中间状态，但不要假设连续性。
- **PerformanceThresholds 硬编码** — 阈值常量编译期固定，运行时不可调整。
- **CacheEvents 使用 Prism PubSubEvent** — 虽然 Shared 本身不依赖 Prism，但 `CacheEvents.cs` 引用了 `Prism.Events`，这是唯一例外（事件定义文件）。
