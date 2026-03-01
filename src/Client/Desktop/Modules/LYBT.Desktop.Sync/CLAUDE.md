# LYBT.Desktop.Sync 代码知识

## 模块概述

数据同步模块 -- Desktop 端基础数据 (Herb/Patient/Formula) 的双向同步功能。提供差异检查、冲突处理对话框、选择性上传/下载、批量操作。使用 CommunityToolkit.Mvvm 源生成器。

### 架构分层

```
SyncView (主界面)
  |
SyncViewModel (差异检查 + 同步执行 + 全选/取消)
  |
  +-- ISyncService (来自 LYBT.Desktop.Contracts，实现在 LocalData/Shell)
  +-- IDialogService (Prism 对话框服务)
        |
        SyncConflictDialog
          |
        SyncConflictDialogViewModel (冲突逐条处理)
```

### DI 注册 (SyncModule.cs)

```csharp
containerRegistry.Register<SyncViewModel>();
containerRegistry.Register<SyncConflictDialogViewModel>();
containerRegistry.RegisterForNavigation<SyncView, SyncViewModel>();
containerRegistry.RegisterDialog<SyncConflictDialog, SyncConflictDialogViewModel>();
```

### 模块依赖: `[ModuleDependency("AuthenticationModule")]`

## 架构决策

| 决策 | 原因 | 关联 OpenSpec |
|------|------|--------------|
| CommunityToolkit.Mvvm 源生成器 | [ObservableProperty] + [RelayCommand] 减少样板代码 | implement-data-sync |
| SyncItemViewModel 内联定义 | 与 SyncViewModel 同文件，作为轻量级列表项模型 | implement-data-sync |
| 冲突处理通过 Prism Dialog | 独立对话框逐条处理冲突，支持全部使用本地/全部使用服务器 | implement-data-sync |
| ISyncService 接口定义在 Contracts | 实现在 LocalData (SyncService)，Shell 注册，Sync 模块仅消费 | - |

## 代码文件结构

### 模块注册

| 文件 | 类名 | 基类/接口 | 职责 |
|------|------|-----------|------|
| SyncModule.cs | SyncModule | IModule | Prism 模块注册，注册 ViewModel + 导航视图 + 对话框 |

### ViewModel 层

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| ViewModels/SyncViewModel.cs | SyncViewModel | NavigableViewModelBase | 数据同步主界面 ViewModel |
| ViewModels/SyncViewModel.cs | SyncItemViewModel | ObservableObject | 同步项 ViewModel (内联定义) |
| ViewModels/SyncConflictDialogViewModel.cs | SyncConflictDialogViewModel | DialogViewModelBase | 冲突处理对话框 ViewModel |

#### SyncViewModel Observable Properties

| 属性 | 类型 | 说明 |
|------|------|------|
| EntityTypes | ObservableCollection<string> | 支持的实体类型列表 |
| SelectedEntityType | string? | 当前选中的实体类型 |
| LocalOnlyItems | ObservableCollection<SyncItemViewModel> | 仅本地有的项 (待上传) |
| ServerOnlyItems | ObservableCollection<SyncItemViewModel> | 仅服务器有的项 (待下载) |
| ConflictItems | ObservableCollection<SyncItemViewModel> | 冲突项 |
| LastSyncTime | DateTime? | 上次同步时间 |
| SyncProgress | int | 同步进度 (0-100) |
| IsSyncing | bool | 是否正在同步 |
| HasCheckedDifferences | bool | 是否已检查差异 |

#### SyncViewModel Computed Properties

| 属性 | 计算逻辑 |
|------|----------|
| HasDataToSync | 任一列表有选中项 |
| UploadCount | LocalOnlyItems 选中数量 |
| DownloadCount | ServerOnlyItems 选中数量 |
| ConflictCount | ConflictItems 总数 |
| TotalDifferenceCount | 三个列表总数之和 |

#### SyncViewModel Commands

| 命令 | CanExecute | 说明 |
|------|------------|------|
| CheckDifferencesCommand | !IsSyncing && SelectedEntityType != null | 检查差异 (调用 ISyncService.CheckDifferencesAsync) |
| ExecuteSyncCommand | !IsSyncing && HasCheckedDifferences && HasDataToSync | 执行同步 (未处理冲突时先弹出对话框) |
| SelectAllUploadCommand | - | 全选上传项 |
| SelectAllDownloadCommand | - | 全选下载项 |
| DeselectAllCommand | - | 取消全部选择 |
| RefreshCommand | - | 刷新 (重新检查差异) |

#### SyncItemViewModel 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| EntityId | Guid | 实体 ID |
| EntityType | string | 实体类型 |
| EntityName | string | [ObservableProperty] 实体名称 (显示用) |
| DiffType | SyncDiffType | 差异类型 (LocalOnly/ServerOnly/Modified) |
| LocalChecksum | string? | 本地 Checksum |
| ServerChecksum | string? | 服务器 Checksum |
| LocalChangedAt | DateTime? | 本地修改时间 |
| ServerChangedAt | DateTime? | 服务器修改时间 |
| ChangedFields | List<string>? | 变更字段列表 |
| IsSelected | bool | [ObservableProperty] 是否选中 |
| ResolutionDecision | bool? | [ObservableProperty] 冲突解决决策 (true=本地, false=服务器, null=未决定) |
| OperationDisplay | string | 计算属性: "上传"/"下载"/"冲突" |
| ChangedAtDisplay | string | 计算属性: 本地/服务器修改时间显示 |

#### SyncConflictDialogViewModel Observable Properties

| 属性 | 类型 | 说明 |
|------|------|------|
| Conflicts | ObservableCollection<SyncItemViewModel> | 冲突列表 |
| SelectedConflict | SyncItemViewModel? | 当前选中的冲突项 |
| CurrentIndex | int | 当前冲突索引 (1-based) |

#### SyncConflictDialogViewModel Computed Properties

| 属性 | 计算逻辑 |
|------|----------|
| ResolvedCount | Conflicts 中已有 ResolutionDecision 的数量 |
| TotalCount | Conflicts 总数 |
| AllResolved | 是否全部已处理 |

#### SyncConflictDialogViewModel Commands

| 命令 | CanExecute | 说明 |
|------|------------|------|
| UseLocalCommand | - | 当前冲突使用本地版本 |
| UseServerCommand | - | 当前冲突使用服务器版本 |
| SkipCommand | - | 跳过当前冲突 |
| UseAllLocalCommand | - | 全部使用本地版本 |
| UseAllServerCommand | - | 全部使用服务器版本 |
| PreviousCommand | CanGoPrevious | 上一个冲突 |
| NextCommand | CanGoNext | 下一个冲突 |
| CompleteCommand | - | 完成处理 (CloseDialog OK) |

### View 层

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| Views/SyncView.xaml.cs | SyncView | UserControl | 数据同步主界面 |
| Views/SyncConflictDialog.xaml.cs | SyncConflictDialog | UserControl | 冲突处理对话框 |

## 死代码与废弃标记

(无)

所有类型均在模块内或外部被正确引用:
- SyncModule: 被 Shell App.xaml.cs 加载
- SyncView: 被 SyncModule 注册为导航视图，ViewNames 中有引用
- SyncConflictDialog: 被 SyncModule 注册为对话框，SyncViewModel 中调用
- SyncViewModel/SyncConflictDialogViewModel: 被 SyncModule 注册
- SyncItemViewModel: 被 SyncViewModel 和 SyncConflictDialogViewModel 使用

## 已知陷阱

- `SyncViewModel.CheckDifferencesAsync` 会检查 `SessionManager.IsAuthenticated`，未登录时不能执行同步
- `SyncViewModel.ShowConflictResolutionDialogAsync` 使用 `Task.Run` + `RunOnUIThread` 混合模式调用 Prism Dialog，需注意线程切换
- `SyncConflictDialogViewModel.Cancel()` 重写时会清除所有冲突决策 (ResolutionDecision = null)，取消对话框不保留用户选择
- `SyncItemViewModel` 与 `SyncViewModel` 定义在同一个文件中 (ViewModels/SyncViewModel.cs)，查找时注意

## 依赖关系

### 项目依赖 (来源: .csproj ProjectReference)
- LYBT.Desktop.Foundation (基础设施)
- LYBT.Desktop.Infrastructure (MasterDetailViewModelBase)
- LYBT.Desktop.Models (DialogViewModelBase/NavigableViewModelBase)
- LYBT.Desktop.Contracts (ISyncService/IViewModelServices)
- LYBT.Shared.Models (SyncDiffDto/SyncDiffType/SyncResolution)
- LYBT.Shared.Primitives (基础类型)

### 被依赖
- LYBT.Desktop.Shell (模块加载)

---

最后更新: 2026-03-01
