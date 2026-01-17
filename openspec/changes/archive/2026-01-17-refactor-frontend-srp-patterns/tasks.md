# refactor-frontend-srp-patterns Tasks

## Overview

- **变更类型**: Refactor (架构重构)
- **风险等级**: Medium-High
- **预估工作量**: 3-5天
- **涉及问题**: HIGH 5 + MEDIUM 4 (调整后) + LOW 5 = 14个

## Phase 1: SRP核心修复 (HIGH H1-H3)

### 1.1 MasterDetailViewModelBase共享组件提取 (H3)

- [ ] 1.1.1 创建 `AuditLogHandler` 通用组件
  - **文件**: `Infrastructure/ViewModels/Handlers/AuditLogHandler.cs`
  - **方法**: `ShowAuditLogAsync(Guid entityId, string entityType)`
  - **验证**: 编译通过

- [ ] 1.1.2 创建 `ImportExportHandler<T>` 泛型组件
  - **文件**: `Infrastructure/ViewModels/Handlers/ImportExportHandler.cs`
  - **方法**: `ImportAsync()`, `ExportAsync()`, `DownloadTemplateAsync()`
  - **验证**: 编译通过

- [ ] 1.1.3 在MasterDetailViewModelBase中注入共享Handler
  - **文件**: `Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
  - **变更**: 添加可选的Handler注入点
  - **验证**: 现有子类行为不变

### 1.2 UserMasterDetailViewModel重构 (H2)

- [ ] 1.2.1 创建 `UserPasswordHandler`
  - **文件**: `Users/ViewModels/Handlers/UserPasswordHandler.cs`
  - **方法**: `ResetPasswordAsync(UserListDto user)`
  - **来源**: 从UserMasterDetailViewModel提取~28行代码

- [ ] 1.2.2 创建 `UserImportExportHandler`
  - **文件**: `Users/ViewModels/Handlers/UserImportExportHandler.cs`
  - **方法**: `ImportUsersAsync()`, `ExportUsersAsync()`, `DownloadTemplateAsync()`
  - **来源**: 从UserMasterDetailViewModel提取~104行代码

- [ ] 1.2.3 创建 `UserStatusHandler`
  - **文件**: `Users/ViewModels/Handlers/UserStatusHandler.cs`
  - **方法**: `ToggleStatusAsync(UserListDto user)`, `RestoreUserAsync(UserListDto user)`
  - **来源**: 从UserMasterDetailViewModel提取~57行代码

- [ ] 1.2.4 创建 `UserAuditHandler`
  - **文件**: `Users/ViewModels/Handlers/UserAuditHandler.cs`
  - **方法**: `ShowAuditLogAsync(Guid userId)`
  - **来源**: 复用通用AuditLogHandler

- [ ] 1.2.5 重构UserMasterDetailViewModel使用Handler
  - **文件**: `Users/ViewModels/UserMasterDetailViewModel.cs`
  - **变更**: 注入Handler，Command委托给Handler
  - **验证**: 用户管理功能正常

- [ ] 1.2.6 注册Handler到DI
  - **文件**: `Users/UsersModule.cs`
  - **变更**: 添加Handler注册

### 1.3 MedicalCaseService拆分 (H1)

- [ ] 1.3.1 创建 `IMedicalCaseCommandService` 接口
  - **文件**: `Contracts/Services/IMedicalCaseCommandService.cs`
  - **方法**:
    - `SetPrescriptionFlagAsync`
    - `CloseCaseAsync`
    - `DeleteMedicalCaseAsync`
    - `UpdateStatusAsync`
    - `SaveDraftViaApiAsync`
    - `CancelMedicalCaseViaApiAsync`

- [ ] 1.3.2 创建 `IMedicalCaseLifecycleService` 接口
  - **文件**: `Contracts/Services/IMedicalCaseLifecycleService.cs`
  - **方法**:
    - `CreateMedicalCaseAsync`
    - `SaveDraftAsync`
    - `CancelMedicalCaseAsync`
    - `CompleteMedicalCaseAsync`
    - `ResumeDraftAsync`

- [ ] 1.3.3 实现 `MedicalCaseCommandService`
  - **文件**: `MedicalCase/Services/MedicalCaseCommandService.cs`
  - **来源**: 从MedicalCaseService提取命令方法

- [ ] 1.3.4 实现 `MedicalCaseLifecycleService`
  - **文件**: `MedicalCase/Services/MedicalCaseLifecycleService.cs`
  - **来源**: 从MedicalCaseService提取生命周期方法

- [ ] 1.3.5 重构MedicalCaseService为门面模式
  - **文件**: `MedicalCase/Services/MedicalCaseService.cs`
  - **变更**: 注入子服务，委托调用
  - **保留**: IMedicalCaseService, IMedicalCaseQueryService接口

- [ ] 1.3.6 更新DI注册
  - **文件**: `MedicalCase/MedicalCaseModule.cs`
  - **变更**: 注册新服务接口和实现

### 1.4 Phase 1编译验证

- [ ] 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- [ ] 确保零编译错误
- [ ] 用户管理功能手动测试
- [ ] 医案创建/编辑功能手动测试

## Phase 2: 架构风险修复 (HIGH H4-H5)

### 2.1 ElementName绑定修复 (H4)

- [ ] 2.1.1 修复HerbItemControl.xaml Popup绑定
  - **文件**: `Herbs/Controls/HerbItem/HerbItemControl.xaml`
  - **位置**: 第112行附近
  - **Before**: `MinWidth="{Binding ActualWidth, ElementName=HerbNameTextBox}"`
  - **After**: `MinWidth="{Binding PlacementTarget.ActualWidth, RelativeSource={RelativeSource AncestorType=Popup}}"`

- [ ] 2.1.2 验证其他ElementName绑定安全性
  - **范围**: MasterDetailLayout.xaml (8处) - 已确认安全
  - **范围**: SearchBox.xaml - 使用BindingProxy，安全
  - **验证**: 运行时无绑定错误

### 2.2 缓存键用户隔离 (H5)

- [ ] 2.2.1 修改PatientSearchCache缓存键生成
  - **文件**: `Patients/Services/PatientSearchCache.cs`
  - **变更**:
    ```csharp
    private string GenerateKey(string keyword, int page)
    {
        var userId = _sessionManager.CurrentUserId ?? Guid.Empty;
        return $"{userId}:{keyword?.ToLowerInvariant() ?? string.Empty}:{page}";
    }
    ```

- [ ] 2.2.2 添加会话变更事件订阅
  - **文件**: `Patients/Services/PatientSearchCache.cs`
  - **变更**: 在构造函数订阅`SessionChanged`事件
  - **行为**: 用户切换时清空缓存

- [ ] 2.2.3 实现登出缓存清理
  - **文件**: `Patients/Services/PatientSearchCache.cs`
  - **变更**: 添加`ClearCache()`方法
  - **触发**: 登出或用户切换时调用

### 2.3 Phase 2编译验证

- [ ] 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- [ ] 运行应用检查无绑定错误 (Output窗口)
- [ ] 测试患者搜索缓存功能
- [ ] 测试用户切换场景

## Phase 3: 代码质量改进 (MEDIUM M1, M3, M5)

### 3.1 PatientService位置规范化 (M1)

- [ ] 3.1.1 移动PatientService到正确目录
  - **From**: `Patients/ViewModels/Components/PatientService.cs`
  - **To**: `Patients/Services/PatientService.cs`

- [ ] 3.1.2 更新命名空间
  - **From**: `LYBT.Desktop.Patients.ViewModels.Components`
  - **To**: `LYBT.Desktop.Patients.Services`

- [ ] 3.1.3 更新所有引用
  - 使用IDE重构功能或手动更新using语句

### 3.2 对话框ViewModel继承统一 (M3)

- [ ] 3.2.1 修改ApiConnectionFailedDialogViewModel
  - **文件**: `Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs`
  - **变更**: 改为继承`DialogViewModelBase`
  - **调整**: 更新构造函数和基类调用

- [ ] 3.2.2 修改ConfirmationDialogViewModel
  - **文件**: `Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs`
  - **变更**: 改为继承`DialogViewModelBase`

- [ ] 3.2.3 修改EntityAuditLogDialogViewModel
  - **文件**: `Shell/Dialogs/ViewModels/EntityAuditLogDialogViewModel.cs`
  - **变更**: 改为继承`DialogViewModelBase`

### 3.3 Master-Detail控件抽象 (M5)

- [ ] 3.3.1 创建MasterDetailControlBase泛型基类
  - **文件**: `Infrastructure/Controls/MasterDetailControlBase.cs`
  - **内容**:
    ```csharp
    public abstract class MasterDetailControlBase<TViewModel> : UserControl
        where TViewModel : class
    {
        protected TViewModel ViewModel => DataContext as TViewModel;
        protected virtual void OnLoaded() { }
        protected virtual void OnUnloaded() { }
    }
    ```

- [ ] 3.3.2 重构PatientMasterDetailControl
  - **文件**: `Patients/Controls/PatientMasterDetailControl.xaml.cs`
  - **变更**: 继承`MasterDetailControlBase<PatientMasterDetailViewModel>`

- [ ] 3.3.3 重构HerbMasterDetailControl
  - **文件**: `Herbs/Controls/HerbMasterDetailControl.xaml.cs`
  - **变更**: 继承`MasterDetailControlBase<HerbMasterDetailViewModel>`

- [ ] 3.3.4 重构UserMasterDetailControl
  - **文件**: `Users/Controls/UserMasterDetailControl.xaml.cs`
  - **变更**: 继承`MasterDetailControlBase<UserMasterDetailViewModel>`

- [ ] 3.3.5 重构FormulaMasterDetailControl
  - **文件**: `Formula/Controls/FormulaMasterDetailControl.xaml.cs`
  - **变更**: 继承`MasterDetailControlBase<FormulaMasterDetailViewModel>`

- [ ] 3.3.6 重构MedicalCaseMasterDetailControl
  - **文件**: `MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml.cs`
  - **变更**: 继承`MasterDetailControlBase<MedicalCaseMasterDetailViewModel>`

### 3.4 Phase 3编译验证

- [ ] 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- [ ] 确保零编译错误
- [ ] 测试各Master-Detail界面功能

## Phase 4: 规范统一 (LOW L1-L5) - 延迟执行

### 4.1 规范统一任务 (可选)

- [ ] L1: Repository签名统一 - 审查GetByIdAsync参数顺序
- [ ] L2: Mapper自动注册 - 评估必要性
- [ ] L3: 日志前缀规范 - 定义统一格式
- [ ] L4: 异步命名补全 - 扫描并添加Async后缀
- [ ] L5: XML注释补全 - 公开API注释覆盖

**注意**: Phase 4为低优先级，可在后续迭代中执行。

## Dependencies

```
Phase 1.1 (H3 共享组件) ─────────────────────┐
                                             │
Phase 1.2 (H2 User重构) ◄────────────────────┤
                                             │
Phase 1.3 (H1 MedicalCase拆分) ◄─────────────┘

Phase 2.1 (H4 ElementName) ──────┐
                                 ├──> 独立执行
Phase 2.2 (H5 缓存隔离) ─────────┘

Phase 3.1 (M1 PatientService) ──┐
Phase 3.2 (M3 Dialog继承) ──────├──> 可并行
Phase 3.3 (M5 控件基类) ────────┘

Phase 4 (L1-L5) ──────────────────────────────> 延迟执行
```

**依赖说明**:
- H3必须先于H2，共享Handler可被复用
- H1最后执行，验证拆分模式
- Phase 2与Phase 1.3可并行
- Phase 3各任务相互独立

## Validation Checklist

- [ ] Desktop解决方案编译通过 (`dotnet build LYBT.Desktop.sln -c Release`)
- [ ] 无绑定错误 (检查Output窗口的System.Windows.Data Error)
- [ ] 核心功能可正常使用:
  - [ ] 登录/登出
  - [ ] 患者选择和搜索
  - [ ] 医案创建/编辑/完成
  - [ ] 处方管理
  - [ ] 用户管理 (重置密码/导入导出/状态切换)
  - [ ] 各Master-Detail界面导航
- [ ] 代码审查通过

## Skipped Items

| 原计划 | 跳过原因 |
|--------|----------|
| M2 对话框服务合并 | 分析未发现重复对话框服务类 |
| M4 构造函数参数聚合 | IViewModelServices已存在且足够 |
| M6 角色层View模板化 | AdminHome与ClinicalHome差异较大 |

## Notes

- **增量执行**: 每个子任务完成后立即编译验证
- **回滚点**: 每个Phase完成后创建Git提交 (不推送)
- **测试优先**: 修改核心类前确保有测试覆盖
- **Handler模式**: 遵循现有MedicalCaseWorkspaceCoordinator的Handler设计

---

**生成时间**: 2026-01-17
**状态**: 完整版 (已完成设计阶段细化)
