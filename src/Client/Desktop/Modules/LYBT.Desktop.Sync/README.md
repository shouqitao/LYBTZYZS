# LYBT.Desktop.Sync

> 客户端数据同步 UI 模块，提供同步操作界面与冲突解决对话框

## 项目定位

- **层级**: Desktop Modules (功能模块层)
- **职责**: 提供基础数据 (Herb/Patient/Formula) 的双向同步界面，包含同步状态展示、实体类型选择、冲突检测与手动解决
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Sync/
├── SyncModule.cs          # Prism 模块注册 (依赖 AuthenticationModule)
├── ViewModels/            # MVVM ViewModel
│   ├── SyncViewModel.cs               # 同步主界面 ViewModel
│   └── SyncConflictDialogViewModel.cs # 冲突解决对话框 ViewModel
└── Views/                 # WPF 视图
    ├── SyncView.xaml                  # 同步主界面
    └── SyncConflictDialog.xaml        # 冲突解决对话框
```

## 核心组件

| 名称 | 说明 |
|------|------|
| SyncModule | Prism 模块入口，注册导航视图和冲突对话框，依赖 AuthenticationModule |
| SyncViewModel | 同步主界面逻辑，管理实体类型选择、同步状态、本地/服务器/冲突项分类展示 |
| SyncConflictDialogViewModel | 冲突解决对话框逻辑，支持选择本地版本或服务器版本 |
| SyncView | 同步操作主界面，展示待上传/待下载/冲突项列表 |
| SyncConflictDialog | 冲突解决对话框，Prism Dialog 注册 |

## 设计依据

同步模块是双模式架构的关键 UI 组件。本地模式下数据存储在 SQLite，需要与服务器 SQL Server 进行数据同步。本模块专注于 UI 展示和用户交互，实际同步逻辑委托给 LYBT.Desktop.LocalData 中的 SyncService。

冲突解决采用手动模式，由用户在对话框中选择保留本地版本或服务器版本，确保数据一致性由医生主动确认。

模块依赖 AuthenticationModule，确保同步操作在用户认证后才可用。

## 依赖关系

### 依赖
- Prism.Core / Prism.DryIoc / Prism.Wpf - 模块化与对话框框架
- LYBT.Desktop.Foundation - 基础框架
- LYBT.Desktop.Infrastructure - 基础设施支持
- LYBT.Desktop.Models - ViewModel 基类 (NavigableViewModelBase)
- LYBT.Desktop.Contracts - 服务接口契约 (ISyncService)
- LYBT.Shared.Models - 同步 DTO (SyncDiffDto, SyncDiffType 等)
- LYBT.Shared.Primitives - 共享基元类型

### 被依赖
- LYBT.Desktop.Shell - 主程序模块加载
- LYBT.Tests.Desktop.Integration - 集成测试

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |
