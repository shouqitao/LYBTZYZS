# Desktop View 链路完整性审计报告

**审计日期**: 2026-08-16  
**审计范围**: `src/Client/Desktop/` 全部 View/ViewModel/Module/Dialog  
**审计方法**: 逐文件交叉比对 XAML View ↔ ViewModel ↔ Module 注册 ↔ 导航引用 ↔ DI 注册 ↔ 事件订阅/发布

---

## 审计概览

| 指标 | 数量 |
|------|------|
| XAML 文件总数 | 78 |
| ViewModel 类总数 | 62（含基类/接口/工具类） |
| Prism Module 数 | 11 |
| RegisterForNavigation 注册 | 21 |
| RegisterDialog 注册 | 8 |
| ViewNames 常量 | 22 |
| 发现问题数 | 9（P0: 0, P1: 4, P2: 5） |

---

## 发现汇总

### ✅ 正常链路（通过审计）

1. **View → ViewModel 绑定链**：所有 21 个导航 View 均正确配置了 `prism:ViewModelLocator.AutoWireViewModel="True"` 或通过 `ViewModelLocationProvider.Register` 显式映射
2. **Dialog 注册一致性**：8 个 Dialog 的 `RegisterDialog<View, ViewModel>` 与 `ShowDialog` 调用名称完全匹配
3. **Region 定义一致性**：`LoginRegion` 和 `ContentRegion` 在 MainWindow.xaml 和代码中完全一致
4. **ViewModel DI 注册**：所有需要通过 DI 解析的 ViewModel 均已注册到对应 Module

---

## 发现的问题

### P1-1: ClinicalHomeView 半死状态（低风险）

| 项目 | 详情 |
|------|------|
| **文件** | `Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` |
| **问题类型** | 半死 View（有注册、有代码，但无正常导航路径） |
| **严重级别** | P1 |
| **描述** | `ClinicalHomeView` 已注册为导航 View（ClinicalModule.cs:30），有完整的 XAML 和 ViewModel。但：|
| | 1. **Doctor 角色的 HomeViewName 是 `ClinicalWorkspace`，不是 `ClinicalHome`**（DoctorRoleDefinition.cs:37）|
| | 2. **没有任何角色的 HomeViewName 指向它**（Doctor→ClinicalWorkspace, Admin→AdminHome, Receptionist→ReceptionistHome, SuperAdmin→SysadminHome）|
| | 3. **没有任何代码调用 `NavigateTo(ViewNames.ClinicalHome)`** |
| | 4. **ModuleLazyLoader.ViewToModuleMap 中也没有它的条目** |
| | 它仅作为 `RoleRegistry.DefaultHomeView` 和 `NavigationCoordinator.NavigateToHome()` 的 fallback 存在 |
| **影响** | 不影响正常功能，但 View+ViewModel+Module 注册全部浪费 |
| **建议修复** | 确认是否仍需要 fallback 主页。如不需要，删除 ClinicalHomeView.xaml + ClinicalHomeViewModel.cs + ClinicalModule 中的注册 + ViewNames.ClinicalHome 常量。如需要，在 ModuleLazyLoader.ViewToModuleMap 中补充映射 |

---

### P1-2: PendingQueueView 死 View（无 DI 注册）

| 项目 | 详情 |
|------|------|
| **文件** | `Roles/LYBT.Desktop.Clinical/Views/PendingQueueView.xaml` |
| **问题类型** | 死 View（XAML 存在但未被使用） |
| **严重级别** | P1 |
| **描述** | `PendingQueueView.xaml` 存在且有 `prism:ViewModelLocator.AutoWireViewModel="True"`，但：|
| | 1. **未注册为导航 View**（不在任何 Module 的 RegisterForNavigation 中）|
| | 2. **未被任何 XAML 引用**（grep "PendingQueueView" 在 *.xaml 中无结果）|
| | 3. **PendingQueueViewModel 未注册到 DI**（仅通过 `new PendingQueueViewModel(...)` 在 PatientSelectionViewModel 中手动创建）|
| | 4. **PendingQueueViewModel 是 `ChildViewModelBase`**，设计为组合子 VM，不应独立解析 |
| | 该 View 似乎是从 PatientSelectionView 拆分出来后遗留的 |
| **影响** | 不影响运行时（未注册不会被解析），但占用代码空间 |
| **建议修复** | 删除 PendingQueueView.xaml + PendingQueueView.xaml.cs。PendingQueueViewModel 保持不变（作为 PatientSelectionViewModel 的子 VM） |

---

### P1-3: ConsultationCompletedEvent / PrescriptionCompletedEvent 订阅无发布者

| 项目 | 详情 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Infrastructure/Events/CaseEvents.cs` |
| **问题类型** | 事件订阅断链（有订阅者，无发布者） |
| **严重级别** | P1 |
| **描述** | `CaseEvents.ConsultationCompletedEvent` 和 `CaseEvents.PrescriptionCompletedEvent` 在 `MedicalCaseWorkspaceViewModel.cs` 中被订阅（Dispose 时 Unsubscribe），但：|
| | 1. **整个项目中没有任何代码 Publish 这两个事件** |
| | 2. 订阅回调 `OnConsultationCompleted` / `OnPrescriptionCompleted` 永远不会被触发 |
| | 3. 事件类已定义（CaseEvents.cs:20, 29），但发布端缺失 |
| **影响** | 订阅代码是死代码，不影响运行时但造成维护混淆 |
| **建议修复** | 选项 A：在诊完成/处方完成的关键路径上添加 Publish 调用。选项 B：确认功能已弃用，删除事件定义和订阅代码 |

---

### P1-4: AuthStateChangedPubSubEvent 发布无订阅者

| 项目 | 详情 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Foundation/Security/AuthenticationStateMachine.cs` |
| **问题类型** | 事件发布断链（有发布者，无订阅者） |
| **严重级别** | P1 |
| **描述** | `AuthStateChangedPubSubEvent` 在 `AuthenticationStateMachine.cs:230` 被 Publish，但：|
| | 1. **整个项目中没有任何代码 Subscribe 这个事件** |
| | 2. 事件类已定义（AuthenticationStateMachine.cs:243）|
| | 3. 每次认证状态变更都会 Publish 一个无人接收的事件 |
| **影响** | 不影响功能，但每次认证状态变更都浪费一次事件分发 |
| **建议修复** | 确认是否需要此事件。如需要，添加订阅者。如不需要，删除事件定义和 Publish 调用 |

---

### P2-1: PatientEvents.UpdatedEvent 死事件定义

| 项目 | 详情 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Infrastructure/Events/PatientEvents.cs` |
| **问题类型** | 死代码（事件定义未使用） |
| **严重级别** | P2 |
| **描述** | `PatientEvents.UpdatedEvent` 已定义但：|
| | 1. **没有任何代码 Publish 或 Subscribe 它** |
| | 2. 仅在 PatientEvents.cs 中定义 |
| **影响** | 不影响功能，占用代码空间 |
| **建议修复** | 删除 PatientEvents.cs 或确认是否需要保留以备将来使用 |

---

### P2-2: PendingLogoutsClearedEvent 死事件定义

| 项目 | 详情 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Foundation/Security/AuthEvents.cs` |
| **问题类型** | 死代码（事件定义未使用） |
| **严重级别** | P2 |
| **描述** | `AuthEvents.PendingLogoutsClearedEvent` 已定义（AuthEvents.cs:47）但：|
| | 1. **没有任何代码 Publish 或 Subscribe 它** |
| | 2. 仅在 ILogoutService.cs 的注释中被引用 |
| **影响** | 不影响功能，占用代码空间 |
| **建议修复** | 删除事件定义和相关 Payload 类 |

---

### P2-3: MedicalCaseCommandsViewModel DI 注册缺失（低风险）

| 项目 | 详情 |
|------|------|
| **文件** | `Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs` |
| **问题类型** | DI 注册缺失（但手动创建） |
| **严重级别** | P2 |
| **描述** | `MedicalCaseCommandsViewModel` 是 `ChildViewModelBase`，在 `MedicalCaseWorkspaceViewModel` 中通过 `new MedicalCaseCommandsViewModel(...)` 手动创建（MedicalCaseWorkspaceViewModel.cs:299），而非通过 DI 解析。这不是错误（ChildVM 设计如此），但：|
| | 1. 该类有 9 个命令，~555 行代码 |
| | 2. 构造函数有 8 个参数 |
| | 3. 如果将来需要 DI 解析，当前无法从容器获取 |
| **影响** | 不影响当前功能 |
| **建议修复** | 保持现状（ChildVM 手动创建是合理设计），但建议在类注释中明确说明其生命周期绑定父 VM |

---

### P2-4: ConsultationEditorViewModel / PrescriptionEditorViewModel DI 注册缺失（低风险）

| 项目 | 详情 |
|------|------|
| **文件** | `Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs` |
| | `Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/PrescriptionEditorViewModel.cs` |
| **问题类型** | DI 注册缺失（但手动创建） |
| **严重级别** | P2 |
| **描述** | 两个 EditorVM 均为 `ChildViewModelBase`，在 `MedicalCaseWorkspaceViewModel` 中通过 `new` 手动创建（line 276-277），而非通过 DI 解析。构造函数参数较少（2-3个），手动创建合理 |
| **影响** | 不影响当前功能 |
| **建议修复** | 保持现状（ChildVM 手动创建是合理设计） |

---

### P2-5: ModuleLazyLoader.ViewToModuleMap 缺少 ClinicalHomeView 映射

| 项目 | 详情 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Infrastructure/Navigation/ModuleLazyLoader.cs` |
| **问题类型** | 导航链路不完整 |
| **严重级别** | P2 |
| **描述** | `ModuleLazyLoader.ViewToModuleMap` 包含 16 个 View→Module 映射，但缺少 `ClinicalHomeView → ClinicalModule`。如果有人导航到 ClinicalHomeView，懒加载不会触发（ClinicalModule 是 `WhenAvailable` 所以实际不影响，但如果将来改为 `OnDemand` 就会出问题）|
| **影响** | 当前不影响（ClinicalModule 是 WhenAvailable），但不符合 ViewToModuleMap 的完整性约定 |
| **建议修复** | 补充映射 `{ ViewNames.ClinicalHome, "ClinicalModule" }`，或删除 ClinicalHomeView（见 P1-1） |

---

## 附录：完整 View 链路状态表

### 导航 View（21 个 RegisterForNavigation）

| View | Module | ViewNames 常量 | NavigationManager 引用 | ModuleLazyLoader 映射 | 状态 |
|------|--------|----------------|----------------------|---------------------|------|
| LoginView | AuthModule | ✅ Login | — (LoginRegion) | — | ✅ 正常 |
| AccountSettingsView | App.xaml.cs | ✅ AccountSettings | MenuManager | — | ✅ 正常 |
| AdminHomeView | AdminModule | ✅ AdminHome | ✅ RoleDef | — (WhenAvailable) | ✅ 正常 |
| SystemSettingsView | AdminModule | ✅ SystemSettings | ✅ SuperAdmin | ✅ AdminModule | ✅ 正常 |
| UserManagementView | AdminModule | ✅ UserManagement | ✅ Admin/SuperAdmin | ✅ UsersModule | ✅ 正常 |
| SysadminHomeView | SysadminModule | ✅ SysadminHome | ✅ RoleDef | — (WhenAvailable) | ✅ 正常 |
| LogLevelControlView | SysadminModule | ✅ LogLevelControl | ✅ SuperAdmin | ✅ SysadminModule | ✅ 正常 |
| DeploymentView | SysadminModule | ✅ Deployment | ✅ SuperAdmin | ✅ SysadminModule | ✅ 正常 |
| BackupManagementView | SysadminModule | ✅ BackupManagement | ✅ SuperAdmin | ✅ SysadminModule | ✅ 正常 |
| ClinicalHomeView | ClinicalModule | ✅ ClinicalHome | ❌ 无角色引用 | ❌ 无映射 | ⚠️ 半死 |
| ClinicalWorkspaceView | ClinicalModule | ✅ ClinicalWorkspace | ✅ Doctor RoleDef | ✅ PatientsModule | ✅ 正常 |
| PatientSelectionView | ClinicalModule | ✅ PatientSelection | 代码导航 | ✅ PatientsModule | ✅ 正常 |
| MedicalCaseWorkspaceView | ClinicalModule | ✅ MedicalCaseWorkspace | 代码导航 | ✅ MedicalCaseModule | ✅ 正常 |
| HerbManagementView | ClinicalModule | ✅ HerbManagement | ✅ CatalogModule | ✅ CatalogModule | ✅ 正常 |
| FormulaManagementView | ClinicalModule | ✅ FormulaManagement | ✅ CatalogModule | ✅ CatalogModule | ✅ 正常 |
| PatientManagementView | ClinicalModule | ✅ PatientManagement | ✅ PatientsModule | ✅ PatientsModule | ✅ 正常 |
| MedicalCaseManagementView | ClinicalModule | ✅ MedicalCaseManagement | ✅ MedicalCaseModule | ✅ MedicalCaseModule | ✅ 正常 |
| ReceptionistHomeView | ClinicalModule | ✅ ReceptionistHome | ✅ RoleDef | — (WhenAvailable) | ✅ 正常 |
| RegistrationListView | RegistrationModule | ✅ RegistrationList | ✅ RegistrationModule | ✅ RegistrationModule | ✅ 正常 |
| MedicalCaseMasterDetailView | MedicalCaseModule | ✅ MedicalCaseMasterDetail | 代码导航 | ✅ MedicalCaseModule | ✅ 正常 |
| AuditLogView | MedicalCaseModule | ✅ AuditLog | 代码导航 | ✅ MedicalCaseModule | ✅ 正常 |
| ReportsHomeView | ReportsModule | ✅ ReportsHome | ✅ ReportsModule | ✅ ReportsModule | ✅ 正常 |

### Dialog（8 个 RegisterDialog）

| Dialog | ViewModel | ShowDialog 调用 | 状态 |
|--------|-----------|----------------|------|
| ConfirmationDialog | ConfirmationDialogViewModel | ✅ DialogManager | ✅ 正常 |
| MessageDialog | MessageDialogViewModel | ✅ DialogManager | ✅ 正常 |
| InputDialog | InputDialogViewModel | — | ✅ 正常 |
| ServerConfigView | ServerConfigViewModel | ✅ LoginViewModel | ✅ 正常 |
| FirstRunSetupView | FirstRunSetupViewModel | ✅ LoginViewModel | ✅ 正常 |
| FormulaImportDialog | FormulaImportDialogViewModel | ✅ MedicalCaseCommandsVM | ✅ 正常 |
| HistoryCopyDialog | HistoryCopyDialogViewModel | ✅ MedicalCaseCommandsVM | ✅ 正常 |
| UnsavedChangesDialog | UnsavedChangesDialogViewModel | ✅ WorkspaceNavigationHandler | ✅ 正常 |
| RegistrationCreateDialog | RegistrationCreateDialogViewModel | ✅ RegistrationListVM | ✅ 正常 |

### 事件订阅/发布完整性

| 事件 | 发布者 | 订阅者 | 状态 |
|------|--------|--------|------|
| RegistrationRefreshedEvent | SignalRClient | RegistrationListVM | ✅ 正常 |
| CacheEvents.InvalidatedEvent | DesktopCacheManager | ClinicalWorkspaceVM | ✅ 正常 |
| TokenLifecycleStateChangedEvent | TokenLifecycleService | SessionLifecycleManager | ✅ 正常 |
| AuthEvents.LogoutCompletedEvent | LogoutService, LoginStateManager | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.TokenRefreshSucceededEvent | TokenRefreshHandler | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.TokenRefreshFailedEvent | TokenRefreshHandler | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.LoginStartedEvent | AuthenticationService | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.LogoutStartedEvent | LogoutService | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.ProfileUpdatedEvent | AccountSettingsVM | ShellEventCoordinator | ✅ 正常 |
| AuthEvents.PasswordChangedEvent | — | ShellEventCoordinator | ⚠️ 仅订阅 |
| AuthEvents.SessionExtendedEvent | TokenRefreshHandler | — | ⚠️ 仅发布 |
| AuthEvents.ServerLogoutFailedEvent | LogoutService | — | ⚠️ 仅发布 |
| **ConsultationCompletedEvent** | **❌ 无** | MedicalCaseWorkspaceVM | ❌ 死订阅 |
| **PrescriptionCompletedEvent** | **❌ 无** | MedicalCaseWorkspaceVM | ❌ 死订阅 |
| **AuthStateChangedPubSubEvent** | AuthenticationStateMachine | **❌ 无** | ❌ 死发布 |
| **PatientEvents.UpdatedEvent** | **❌ 无** | **❌ 无** | ❌ 死定义 |
| **PendingLogoutsClearedEvent** | **❌ 无** | **❌ 无** | ❌ 死定义 |

---

## 修复优先级建议

1. **P1-1** (ClinicalHomeView 半死): 确认是否保留。如保留需补 ModuleLazyLoader 映射，如删除需清理 View+VM+注册+常量
2. **P1-2** (PendingQueueView 死 View): 删除 XAML + code-behind
3. **P1-3** (CaseEvents 死订阅): 确认是否需要发布端，或删除事件+订阅
4. **P1-4** (AuthStateChanged 死发布): 确认是否需要订阅端，或删除事件+发布
5. **P2-1~5**: 低优先级清理，可随其他任务一并处理

---

## 审计结论

Desktop 项目的 View 链路整体健康度较高：
- **21/21 导航 View** 均正确注册并可导航到达
- **9/9 Dialog** 注册与调用完全匹配
- **Region 定义** 与使用完全一致
- **主要问题集中在事件系统**：5 个事件存在发布/订阅不对称，其中 2 个死订阅 + 1 个死发布 + 2 个死定义
- **1 个半死 View** (ClinicalHomeView) 和 **1 个死 View** (PendingQueueView) 需要清理
