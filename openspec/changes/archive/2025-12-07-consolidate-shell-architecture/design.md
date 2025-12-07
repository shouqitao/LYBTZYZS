# Design: consolidate-shell-architecture

## Context

Shell层在近期经历了多个重构变更:
- `refactor-shell-startup-flow`: 引入状态机和管道模式
- `remove-titlebar-add-close-button`: 无边框窗口设计
- `remove-statusbar-relocate-status`: 移除底部状态栏

这些变更已归档，但规范Purpose仍为TBD，且代码组织有进一步优化空间。

### 当前架构

```
Shell Layer
├── App.xaml.cs                    # Prism应用入口
├── ViewModels/
│   └── MainWindowViewModel.cs     # 主窗口VM (548行，15个依赖)
├── Views/
│   └── MainWindow.xaml/cs         # 主窗口视图
└── Services/
    ├── Bootstrap/                 # 应用启动引导
    ├── Lifecycle/                 # 应用生命周期状态机
    ├── Login/                     # 登录流程编排
    ├── Session/                   # 会话生命周期管理
    ├── Startup/                   # 启动管道
    └── Diagnostics/               # 启动诊断
```

## Goals / Non-Goals

### Goals
- 完善`login-ui`和`shell-layout`规范的Purpose描述
- 提取`HealthCheckCoordinator`减少MainWindowViewModel职责
- 确认StartupPipeline为唯一启动入口

### Non-Goals
- 不实施POC抽屉布局(已有独立归档提案)
- 不改变现有用户界面行为
- 不引入新的外部依赖

## Decisions

### Decision 1: 提取HealthCheckCoordinator

**What**: 将MainWindowViewModel中的健康检查相关逻辑提取到独立的`HealthCheckCoordinator`服务

**Why**:
- MainWindowViewModel当前职责过重(548行，15个依赖)
- 健康检查逻辑与UI绑定逻辑是正交的关注点
- 便于独立测试健康检查逻辑

**Alternatives considered**:
- 保持现状: 不推荐，职责过重
- 提取到ApiHealthCheckService: 该服务已存在，专注于HTTP调用，不应包含定时调度逻辑

### Decision 2: 规范整合策略

**What**: 更新现有规范Purpose，而非创建新的统一规范

**Why**:
- 避免规范膨胀
- login-ui和shell-layout已能覆盖当前需求
- 符合MVP原则

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 重构可能引入bug | 有91个Shell单元测试覆盖 |
| 新服务增加DI复杂度 | 接口设计简洁，单一职责 |

## Migration Plan

1. 创建HealthCheckCoordinator(不影响现有代码)
2. 更新MainWindowViewModel使用新服务
3. 运行测试验证
4. 更新规范文档

无需回滚计划，变更是纯内部重组。

## Open Questions

无
