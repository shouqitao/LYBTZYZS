# Tasks: standardize-viewmodel-framework

## Phase 1: 基础设施完善 ✅ (2026-01-06)

### 1.1 EventSubscriptionManager ✅
- [x] 创建 `EventSubscriptionManager.cs` 在 `LYBT.Desktop.Infrastructure/Events/`
- [x] 实现 Subscribe/Publish/Dispose 方法
- [ ] 添加单元测试 (延后)

### 1.2 CoreViewModelBase 增强 ✅
- [x] 验证现有 `CoreViewModelBase` 实现
- [x] 添加 `IEventAggregator` 依赖
- [x] 添加 `Events` 属性 (EventSubscriptionManager)
- [x] 更新 Dispose 逻辑自动清理订阅
- [x] 添加 `ExecuteWithErrorHandlingAsync` 方法

### 1.3 NavigableViewModelBase 完善 ✅
- [x] 创建/更新 `NavigableViewModelBase.cs`
- [x] 实现 `INavigationAware` 接口
- [x] 实现 `IConfirmNavigationRequest` 接口
- [x] 实现 `IRegionMemberLifetime` 接口
- [x] 添加 `HasUnsavedChanges` 属性
- [x] 添加 `GetNavigationParameter<T>` 辅助方法
- [ ] 添加单元测试 (延后)

### 1.4 DialogViewModelBase 完善 ✅
- [x] 创建/更新 `DialogViewModelBase.cs`
- [x] 实现 `IDialogAware` 接口
- [x] 添加 `CloseDialog` 方法
- [x] 添加 `[RelayCommand] Cancel` 命令
- [ ] 添加单元测试 (延后)

### 1.5 ValidatingViewModelBase 完善 ✅
- [x] 创建/更新 `ValidatingViewModelBase.cs`
- [x] 实现 `INotifyDataErrorInfo` 接口
- [x] 集成 FluentValidation
- [x] 添加 `ValidateAsync<T>` 方法
- [ ] 添加单元测试 (延后)

### 1.6 PageViewModelBase 完善 ✅
- [x] 创建/更新 `PageViewModelBase.cs`
- [x] 继承 `NavigableViewModelBase`
- [x] 添加 `PageTitle`, `PageDescription` 属性
- [x] 添加 `[RelayCommand] RefreshAsync` 命令
- [x] 添加 `InitializeAsync` 虚方法
- [ ] 添加单元测试 (延后)

## Phase 2: Shell层迁移 ✅ (2026-01-06)

### 2.1 MainWindowViewModel ✅
- [x] 迁移到 `CoreViewModelBase`
- [x] 替换属性为 `[ObservableProperty]`
- [x] 替换命令为 `[RelayCommand]`
- [x] 更新事件订阅使用 `Events` 管理器
- [x] 验证编译
- [ ] 功能测试 (延后)

### 2.2 AccountSettingsViewModel ✅
- [x] 验证已使用 CommunityToolkit (确认)
- [x] 更新基类为 `CoreViewModelBase`
- [x] 验证编译
- [ ] 功能测试 (延后)

### 2.3 Shell Dialogs ✅
- [x] 迁移 `ConfirmationDialogViewModel` 到 `DialogViewModelBase`
- [x] 迁移 `ApiConnectionFailedDialogViewModel` 到 `DialogViewModelBase`
- [x] 迁移 `EntityAuditLogDialogViewModel` 到 `DialogViewModelBase`
- [x] 验证编译
- [ ] 功能测试 (延后)

## Phase 3: Roles层迁移 ✅ (2026-01-06)

### 3.1 Clinical Role ✅
- [x] 迁移 `ClinicalHomeViewModel` 到 `NavigableViewModelBase`
- [x] 迁移 `MedicalCaseWorkspaceViewModel` - 已使用CommunityToolkit模式
- [x] 迁移 `PatientSelectionViewModel` 到 `NavigableViewModelBase`
- [x] 更新事件订阅使用 `Events` 管理器
- [x] 验证编译

### 3.2 Admin Role ✅
- [x] 迁移 `AdminHomeViewModel` 到 `NavigableViewModelBase`
- [x] 验证编译

## Phase 4: Core Modules迁移 ✅ (2026-01-06)

### 4.1 LYBT.Desktop.Infrastructure ✅
- [x] 迁移 `MasterDetailViewModelBase` - 已使用CommunityToolkit模式
- [x] 迁移 `ListViewModelBase` - 已验证兼容
- [x] 验证编译

### 4.2 LYBT.Desktop.Models ✅
- [x] 评估基类清理方案
- [x] 保留必要基类，标记废弃基类

## Phase 5: 业务模块迁移 ✅ (2026-01-06)

### 5.1 MedicalCase模块 ✅
- [x] 迁移 `MedicalCaseMasterDetailViewModel` - 已使用CommunityToolkit
- [x] 迁移 `MedicalCaseEditModeStateMachine` 到 ObservableObject
- [x] 迁移 Component ViewModels - 已使用CommunityToolkit
- [x] Item类迁移: `MedicalCaseItem`, `PrescriptionItem`, `ConsultationItem` 使用 `[ObservableProperty]`
- [x] 验证编译

### 5.2 Herbs模块 ✅
- [x] 迁移 `HerbMasterDetailViewModel` - 已使用CommunityToolkit
- [x] 迁移 `HerbListControlViewModel` - 已使用CommunityToolkit
- [x] 迁移 `HerbItemControlViewModel` - 已使用CommunityToolkit
- [x] Item类迁移: `HerbItemDto` 使用 `[ObservableProperty]`
- [x] 验证编译

### 5.3 Formula模块 ✅
- [x] 迁移 `FormulaMasterDetailViewModel` - 已使用CommunityToolkit
- [x] Item类保持现有模式
- [x] 验证编译

### 5.4 Users模块 ✅
- [x] 迁移 `UserMasterDetailViewModel` - 已使用CommunityToolkit
- [x] Item类保持现有模式
- [x] 验证编译

### 5.5 Patients模块 ✅
- [x] 验证 ViewModel 迁移状态 - 已使用CommunityToolkit
- [x] Item类保持现有模式
- [x] 验证编译

### 5.6 Consultation模块 ✅
- [x] 验证 ViewModel 迁移状态 - 已使用CommunityToolkit
- [x] Item类迁移: `ConsultationItem` 使用 `[ObservableProperty]`
- [x] 验证编译

## Phase 6: 清理与文档 ✅ (2026-01-06)

### 6.1 Mapperly源生成器兼容性修复 ✅
- [x] 修复 `MedicalCaseItemMapper` - 移除`[MapProperty]`，改用手动映射
- [x] 修复 `ConsultationMapper` - 使用字符串字面量
- [x] 修复 `PrescriptionMapper` - 使用字符串字面量
- [x] 添加OpenSpec兼容性注释

### 6.2 验证 ✅
- [x] Desktop Shell全量编译通过
- [x] 0错误 0警告

## 完成总结

### 迁移模式

**ViewModel类**: 从Prism的`BindableBase`/`DelegateCommand`迁移到CommunityToolkit.Mvvm的`ObservableObject`/`[ObservableProperty]`/`[RelayCommand]`

**Item类**: 同样使用`[ObservableProperty]`源生成器

**Mapperly兼容性**:
- `[ObservableProperty]`生成的属性无法使用`[MapProperty]`
- 解决方案: 使用`[MapperIgnoreSource]`/`[MapperIgnoreTarget]`，在包装方法中手动映射

### 基类继承关系

```
ObservableObject (CommunityToolkit.Mvvm)
    └── CoreViewModelBase (自定义)
            ├── NavigableViewModelBase (INavigationAware)
            │       └── PageViewModelBase
            ├── DialogViewModelBase (IDialogAware)
            └── ValidatingViewModelBase (INotifyDataErrorInfo)
```

### 关键变更

| 模式 | Before | After |
|------|--------|-------|
| 属性 | `SetProperty(ref _field, value)` | `[ObservableProperty] private T _field;` |
| 命令 | `new DelegateCommand(...)` | `[RelayCommand] async Task MethodAsync()` |
| 基类 | `BindableBase` | `ObservableObject` |
| 通知 | `RaisePropertyChanged(...)` | `OnPropertyChanged(...)` |
| 依赖属性 | 手动调用 | `[NotifyPropertyChangedFor(...)]` |

## 注意事项

1. **Prism接口保留**: `INavigationAware`, `IDialogAware`, `IRegionMemberLifetime` 保持使用
2. **源生成器顺序**: CommunityToolkit源生成器在Mapperly之后运行，需要特殊处理
3. **类必须partial**: 使用`[ObservableProperty]`或`[RelayCommand]`的类必须声明为`partial`
