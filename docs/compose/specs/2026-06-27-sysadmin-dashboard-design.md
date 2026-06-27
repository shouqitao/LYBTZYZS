# Sysadmin Dashboard v3 设计 Spec

> 日期: 2026-06-27 | 状态: 待审批

---

## [S1] 问题陈述

当前 Sysadmin Dashboard 存在三类问题：

### 1.1 运行时绑定错误

`DashboardStatus` 模型已删除 `ApiStatus` 和 `ConnectionMode` 属性，但 `SysadminHomeView.xaml` 仍绑定这两个属性 → 运行时静默失败，卡片显示空白。

### 1.2 导航入口分散

Dashboard 内容区有 3 个导航按钮（管理员账号管理、诊所信息配置、日志级别控制）。用户要求统一到侧边栏。

### 1.3 侧边栏导航不完整

`BuildNavigationItems` 只给 SuperAdmin 加了"主页"+"用户管理"。缺少：
- 管理员账号管理（AdminUserManagementView）
- 诊所信息配置（SystemSettingsView）
- 日志级别控制（LogLevelControlView）

### 1.4 冗余信息

API 状态和连接模式已在底部状态栏显示，首页不应重复。

---

## [S2] 设计约束

- 不修改工具栏和状态栏的现有设计
- 只修改主页内容区域 + 侧边栏导航项
- MDIX 优先：用 MDIX 内置样式和 FunctionCardStyle
- 颜色用 DynamicResource，确保 Dark 主题适配

---

## [S3] 设计方案

### 3.1 Dashboard 内容区重新设计

**移除：**
- API 状态卡片（状态栏已有）
- 连接模式卡片（状态栏已有）
- 3 个导航按钮（移到侧边栏）

**保留并升级：**
- 数据库状态卡片 — 使用 `FunctionCardStyle`
- 系统信息卡片 — 使用 `FunctionCardStyle`

**布局：**
```
┌─────────────────────────────────────────┐
│           运维控制台                      │
│      凌隐宝堂中医诊所 · 系统运维           │
├─────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐      │
│  │ FunctionCard │  │ FunctionCard │      │
│  │  数据库状态   │  │  系统信息    │      │
│  │  ● 连接正常  │  │  v1.0.0     │      │
│  │              │  │  诊所名称    │      │
│  └─────────────┘  └─────────────┘      │
└─────────────────────────────────────────┘
```

### 3.2 侧边栏导航项更新

在 `BuildNavigationItems` 中为 SuperAdmin 角色添加：

| 导航项 | ViewName | 图标 | 分组 |
|--------|----------|------|------|
| 主页 | SysadminHome | Home | 主页 |
| 管理员账号 | AdminUserManagementView | AccountTie | 管理 |
| 诊所信息 | SystemSettingsView | Domain | 管理 |
| 日志控制 | LogLevelControlView | Tune | 管理 |

### 3.3 ViewModel 清理

**移除：**
- `NavigateToAdminUsersCommand`
- `NavigateToClinicSettingsCommand`
- `NavigateToLogLevelCommand`
- `IConnectionModeService` 依赖（不再需要）
- `IConnectionSettingsService` 依赖（不再需要）
- `INavigationCoordinator` 依赖（不再需要）

**保留：**
- `IAuthApi` — 数据库健康检查
- `IClinicSettingsService` — 诊所名称显示
- `DashboardStatus` — DbStatus + SystemInfo

---

## [S4] 文件变更

| 操作 | 文件 | 变更 |
|------|------|------|
| Modify | `SysadminHomeView.xaml` | 重写：2 张 FunctionCard，移除导航按钮 |
| Modify | `SysadminHomeViewModel.cs` | 移除导航命令和多余依赖 |
| Modify | `MainWindowViewModel.cs` BuildNavigationItems | 添加 SuperAdmin 导航项 |
| Modify | `SysadminModule.cs` | 确认 SystemSettingsView 和 LogLevelControlView 已注册 |

---

## [S5] 验收标准

- [ ] XAML 无运行时绑定错误
- [ ] Dashboard 只显示数据库状态和系统信息
- [ ] 侧边栏有 4 个导航项（主页、管理员账号、诊所信息、日志控制）
- [ ] 导航功能正常（点击侧边栏可跳转）
- [ ] `dotnet build` 通过
- [ ] Dark/Light 主题切换正常

---

## [S6] 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-27 | v3.0 | 基于代码现状重新设计，修复绑定错误，导航统一到侧边栏 |
