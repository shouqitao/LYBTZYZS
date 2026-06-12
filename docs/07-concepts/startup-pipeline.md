---
type: concept
title: 启动流水线机制
tags: [startup, reliability, diagnostics]
related: [desktop-shell, startup-diagnostics, splash-screen, dual-mode-architecture]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/02-requirements/desktop-shell.md"]
---

# 启动流水线机制

**启动流水线 (Startup Pipeline)** 是一种将应用程序启动过程分解为可观测、可取消、可降级的有序步骤序列的设计模式。在凌隐宝堂系统中，它由 `IStartupPipeline` 接口定义，旨在解决启动慢、无反馈以及 API 不可达时的用户体验问题。

## 核心原理

启动流水线通过注册一系列异步步骤（Steps），按顺序执行。每个步骤可以报告进度，并在失败时触发特定的降级策略。

### 关键特性

1.  **有序执行**：步骤通过 `RegisterStep` 注册，按注册顺序依次执行 `ExecuteAsync`。
2.  **进度报告**：支持 `IProgress<string>` 向 UI（如 [[splash-screen|Splash Screen]]）报告当前步骤名称和状态。
3.  **可取消性**：支持 `CancellationToken`，允许用户在启动过程中取消操作。
4.  **事件驱动**：每步完成触发 `StepCompleted` 事件，流水线状态变更触发 `StateChanged` 事件。
5.  **诊断集成**：自动与 [[startup-diagnostics|StartupDiagnostics]] 集成，记录每步耗时。

## 降级策略

当启动步骤失败时，流水线根据失败类型执行不同的降级逻辑：

| 失败场景 | 处理策略 |
| :--- | :--- |
| **API 不可达** | 提示“服务器连接失败”，提供“切换到本地模式”按钮，跳过远程初始化步骤。 |
| **SQLite 初始化失败** | 提示错误详情，提供“重试”或“退出”按钮。 |
| **配置文件缺失** | 提示“配置文件错误”，显示详细错误信息。 |

## 状态机

启动流水线遵循以下状态流转：
`NotStarted` -> `Running` -> `Completed` / `Failed`

## 性能监控

*   **慢步骤检测**：任何耗时超过 **3 秒** 的步骤将被标记为“慢步骤”，并记录在启动诊断报告中。
*   **最小显示时间**：Splash Screen 至少显示 1 秒，避免界面闪烁。

## 相关链接

*   [[desktop-shell]]
*   [[startup-diagnostics]]
*   [[splash-screen]]