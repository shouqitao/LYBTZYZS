# WPF 登录重设计实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 重设计 WPF 登录流程，实现零配置启动 + 自动检测模式 + 透明降级 + 首次使用向导。

**Architecture:** 基于现有 ApplicationStateService + SwitchingApiClient + ConnectionSettingsService，增加模式自动检测、透明降级、配置界面。

**Tech Stack:** WPF/Prism, CommunityToolkit.Mvvm, ASP.NET Core Health Check

---

## 当前架构（不变部分）

- `SwitchingApiClient` — URL 路由（localhost→LocalWebAPI，其他→Refit）
- `LoginCoordinator` — 登录流程协调
- `ApplicationStateService` — API 健康检测
- `ConnectionSettingsService` — URL 管理

## 需要新增/修改

| 组件 | 操作 | 说明 |
|------|------|------|
| `IConnectionModeService` | 新建 | 模式管理（Remote/Local/Auto） |
| `ConnectionModeService` | 新建 | 自动检测 + 透明降级逻辑 |
| `ServerConfigView.xaml` | 新建 | 服务器配置界面（登录前可访问） |
| `ServerConfigViewModel.cs` | 新建 | 配置 VM（URL 输入 + 测试连接） |
| `LoginView.xaml` | 修改 | 添加 ⚙ 设置按钮 + 模式指示器 |
| `LoginViewModel.cs` | 修改 | 添加模式状态属性 |
| `MainWindowViewModel.cs` | 修改 | 状态栏显示当前模式 |
| `FirstRunSetupView.xaml` | 新建 | 首次使用向导 |
| `FirstRunSetupViewModel.cs` | 新建 | 向导 VM |

## Task 1: ConnectionModeService — 模式自动检测 + 透明降级

**Covers:** 自动检测 + 透明降级

- [ ] 创建 `IConnectionModeService` 接口
- [ ] 创建 `ConnectionModeService` 实现
- [ ] 注册 DI
- [ ] 单元测试

## Task 2: ServerConfigView — 服务器配置界面

**Covers:** 配置界面时机（登录前 + Admin）

- [ ] 创建 `ServerConfigView.xaml` + VM
- [ ] 支持 URL 输入 + 测试连接 + 多服务器
- [ ] 登录界面添加 ⚙ 设置按钮

## Task 3: LoginView 增强 — 模式指示器

**Covers:** 状态显示

- [ ] LoginView 添加模式指示器（远程/本地）
- [ ] LoginViewModel 添加 CurrentMode 属性
- [ ] MainWindowViewModel 状态栏显示模式

## Task 4: FirstRunSetup — 首次使用向导

**Covers:** 首次使用向导

- [ ] 创建 FirstRunSetupView + VM
- [ ] 检测首次启动（无配置文件）
- [ ] 引导用户配置远程 URL 或选择本地模式

## Task 5: 集成验证

- [ ] 全量编译
- [ ] 桌面端测试
- [ ] 提交
