# Desktop层架构统一重构 - 任务分解

**Change ID**: unify-desktop-architecture
**Created**: 2025-12-30
**Total Phases**: 5
**Estimated Effort**: 40-50小时

---

## Phase 1: 基础设施层 (P0)

**目标**: 建立统一规范和基础设施
**预估工时**: 8小时
**依赖**: 无

### Task 1.1: 添加CommunityToolkit.Mvvm依赖

**优先级**: P0
**工时**: 0.5小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`
- [ ] `src/Client/Desktop/Modules/*/LYBT.Desktop.*.csproj` (所有模块)

**步骤**:
1. 添加NuGet包引用 `CommunityToolkit.Mvvm` Version="8.2.2"
2. 验证编译通过
3. 更新Directory.Packages.props (如使用中央包管理)

**验收**:
- [ ] 所有项目编译通过
- [ ] 无版本冲突警告

---

### Task 1.2: 创建IMasterDetailServices接口

**优先级**: P0
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IMasterDetailServices.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ILoadingStateManager.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IPaginationService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISearchService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISelectionService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IDetailEditorService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IDialogManager.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewNavigationService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IErrorHandler.cs` (新建)

**步骤**:
1. 创建8个服务接口文件
2. 创建IMasterDetailServices聚合接口
3. 添加XML文档注释
4. 验证编译通过

**验收**:
- [ ] 接口定义完整
- [ ] 编译通过

---

### Task 1.3: 实现MasterDetailServices

**优先级**: P0
**工时**: 3小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/MasterDetailServices.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/LoadingStateManager.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/PaginationService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SearchService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SelectionService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/DetailEditorService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/DialogManagerService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewNavigationService.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ErrorHandlerService.cs` (新建)

**步骤**:
1. 实现各服务接口
2. 实现MasterDetailServices聚合类
3. 注册到DI容器
4. 编写单元测试

**验收**:
- [ ] 所有服务实现完整
- [ ] DI注册正确
- [ ] 单元测试通过

---

### Task 1.4: 创建CommandHandler基础接口

**优先级**: P0
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/CommandResult.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/ICommandHandlerBase.cs` (新建)
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/QueryParams.cs` (新建)

**步骤**:
1. 创建CommandResult<T>记录类型
2. 创建ICommandHandlerBase<TListDto, TDetailDto, TInputDto>接口
3. 创建QueryParams查询参数类

**验收**:
- [ ] 接口定义完整
- [ ] 编译通过

---

### Task 1.5: DTO命名规范化

**优先级**: P0
**工时**: 1小时
**文件**:
- [ ] `src/Shared/LYBT.Shared.Models/Contracts/*/` (所有DTO文件)

**步骤**:
1. 审查现有DTO命名
2. 重命名不符合规范的DTO
3. 更新所有引用
4. 验证编译通过

**命名规范**:
| 当前名称 | 目标名称 |
|----------|----------|
| `XxxDto` (列表用) | `XxxListDto` |
| `XxxDetailDto` | 保持 |
| `XxxInputDto` | 保持 |
| `CreateXxxDto` | `XxxInputDto` (合并) |
| `UpdateXxxDto` | `XxxInputDto` (合并) |

**验收**:
- [ ] 所有DTO符合命名规范
- [ ] 编译通过
- [ ] 无运行时错误

---

## Phase 2: CommandHandler统一 (P1)

**目标**: 所有模块使用CommandHandler Only模式
**预估工时**: 10小时
**依赖**: Phase 1完成

### Task 2.1: Patient模块CommandHandler

**优先级**: P1
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/CommandHandlers/IPatientCommandHandler.cs` (新建或修改)
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/CommandHandlers/PatientCommandHandler.cs` (新建或修改)
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/*.cs` (移除Repository依赖)

**步骤**:
1. 创建/完善IPatientCommandHandler接口 (继承ICommandHandlerBase)
2. 实现PatientCommandHandler
3. 修改ViewModel，使用CommandHandler替代Repository
4. 更新DI注册

**验收**:
- [ ] ViewModel不再直接依赖Repository
- [ ] 所有CRUD操作通过CommandHandler
- [ ] 编译通过
- [ ] 功能测试通过

---

### Task 2.2: User模块CommandHandler

**优先级**: P1
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/CommandHandlers/IUserCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/CommandHandlers/UserCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/*.cs`

**步骤**: 同Task 2.1

**验收**: 同Task 2.1

---

### Task 2.3: Herb模块CommandHandler

**优先级**: P1
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/CommandHandlers/IHerbCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/CommandHandlers/HerbCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/*.cs`

**步骤**: 同Task 2.1

**验收**: 同Task 2.1

---

### Task 2.4: Formula模块CommandHandler

**优先级**: P1
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/CommandHandlers/IFormulaCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/CommandHandlers/FormulaCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/*.cs`

**步骤**: 同Task 2.1

**验收**: 同Task 2.1

---

### Task 2.5: MedicalCase模块CommandHandler

**优先级**: P1
**工时**: 2.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/CommandHandlers/IMedicalCaseCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/CommandHandlers/MedicalCaseCommandHandler.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/MedicalCaseWorkspaceCoordinator.cs` (更新)

**特殊处理**:
- 聚合保存方法: `SaveAggregateAsync(MedicalCaseInputDto)`
- 生命周期方法: `CompleteAsync`, `CancelAsync`, `SuspendAsync`

**验收**:
- [ ] 聚合保存正常工作
- [ ] 生命周期操作正常

---

### Task 2.6: 清理Repository直接依赖

**优先级**: P1
**工时**: 1小时
**文件**:
- [ ] 所有ViewModel文件

**步骤**:
1. 全局搜索 `IXxxRepository` 在ViewModel中的使用
2. 移除直接依赖
3. 替换为CommandHandler调用
4. 验证编译

**验收**:
- [ ] ViewModel无Repository直接依赖
- [ ] 编译通过

---

## Phase 3: ViewModel瘦身 (P2)

**目标**: 所有ViewModel行数 < 400
**预估工时**: 12小时
**依赖**: Phase 2完成

### Task 3.1: 重构MasterDetailViewModelBase

**优先级**: P2
**工时**: 3小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

**步骤**:
1. 改为继承ObservableObject (CommunityToolkit.Mvvm)
2. 使用[ObservableProperty]替换手写属性
3. 使用[RelayCommand]替换手写命令
4. 注入IMasterDetailServices
5. 保持抽象方法签名兼容

**验收**:
- [ ] 代码行数减少50%+
- [ ] 所有子类编译通过
- [ ] 功能测试通过

---

### Task 3.2: 重构PatientMasterDetailViewModel

**优先级**: P2
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`

**目标行数**: < 300行

**步骤**:
1. 应用新MasterDetailViewModelBase
2. 提取PatientSelectionViewModel瘦身
3. 移除重复代码
4. 验证功能

**验收**:
- [ ] 行数 < 300
- [ ] 功能完整

---

### Task 3.3: 重构UserMasterDetailViewModel

**优先级**: P2
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`

**目标行数**: < 250行

**验收**:
- [ ] 行数 < 250
- [ ] 功能完整

---

### Task 3.4: 重构HerbMasterDetailViewModel

**优先级**: P2
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`

**目标行数**: < 250行

**验收**:
- [ ] 行数 < 250
- [ ] 功能完整

---

### Task 3.5: 重构FormulaMasterDetailViewModel

**优先级**: P2
**工时**: 1.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`

**目标行数**: < 250行

**验收**:
- [ ] 行数 < 250
- [ ] 功能完整

---

### Task 3.6: 重构MedicalCaseWorkspaceViewModel

**优先级**: P2
**工时**: 2.5小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/*.cs`

**目标行数**: < 400行

**步骤**:
1. 提取更多逻辑到Components
2. 应用CommunityToolkit.Mvvm
3. 简化属性和命令定义
4. 移除冗余注释

**验收**:
- [ ] 行数 < 400
- [ ] 聚合保存正常
- [ ] 导航正常

---

## Phase 4: 控件提取 (P3)

**目标**: 可复用控件标准化
**预估工时**: 6小时
**依赖**: Phase 3完成

### Task 4.1: 完善PatientInfoCardControl

**优先级**: P3
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientInfoCardControl.xaml`
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientInfoCardControl.xaml.cs`

**功能**:
- 紧凑模式 (Compact) / 完整模式 (Full)
- 可选操作按钮
- 数据绑定支持

**验收**:
- [ ] 两种显示模式正常
- [ ] 数据绑定工作
- [ ] 样式符合设计规范

---

### Task 4.2: 完善PatientSearchControl

**优先级**: P3
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml`
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml.cs`

**功能**:
- 搜索输入框
- 搜索结果列表
- 选择事件

**验收**:
- [ ] 搜索功能正常
- [ ] 选择事件触发正确

---

### Task 4.3: 完善PendingQueueControl

**优先级**: P3
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml`
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml.cs`

**功能**:
- 候诊列表显示
- 状态筛选 (Waiting/InProgress/Suspended)
- 选择事件

**验收**:
- [ ] 列表显示正常
- [ ] 筛选功能正常
- [ ] 选择事件触发正确

---

## Phase 5: MedicalCase优化 (P4)

**目标**: MedicalCase模块架构清晰化
**预估工时**: 8小时
**依赖**: Phase 4完成

### Task 5.1: 完善Coordinator模式

**优先级**: P4
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/MedicalCaseWorkspaceCoordinator.cs`

**功能**:
- SaveAsync (聚合保存)
- SaveDraftAsync (暂存)
- CompleteAsync (完成)
- CancelAsync (取消)
- SuspendAsync (挂起)

**验收**:
- [ ] 所有生命周期操作正常
- [ ] 审计检查正常

---

### Task 5.2: 优化聚合保存流程

**优先级**: P4
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/MedicalCaseWorkspaceCoordinator.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/ConsultationPanelViewModel.cs`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`

**步骤**:
1. 确保IDataProvider接口正确实现
2. 优化数据收集逻辑
3. 处理NeedsPrescription条件
4. 验证Unit字段同步

**验收**:
- [ ] 聚合保存成功
- [ ] 无"单位不能为空"错误
- [ ] 不开处方时跳过处方验证

---

### Task 5.3: 完善导航逻辑

**优先级**: P4
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseNavigationHandler.cs`
- [ ] `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/UnfinishedCaseDialog.xaml`

**功能**:
- 四选项弹窗 (继续编辑/暂存/完成/取消)
- 三选项弹窗 (保存/不保存/取消)
- 状态保存和恢复

**验收**:
- [ ] 弹窗显示正确
- [ ] 选项操作正确

---

### Task 5.4: UI布局优化

**优先级**: P4
**工时**: 2小时
**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml`

**目标布局**:
- 25% 患者信息卡片
- 75% 工作区 (35%诊断 + 65%处方)

**验收**:
- [ ] 布局比例正确
- [ ] 无滚动条 (诊断区)
- [ ] 响应式适配

---

## 验收测试清单

### 功能测试

- [ ] 患者CRUD完整流程
- [ ] 用户CRUD完整流程
- [ ] 药材CRUD完整流程
- [ ] 方剂CRUD完整流程
- [ ] 医案完整生命周期 (创建→诊断→处方→完成)
- [ ] 医案挂起恢复流程
- [ ] 医案取消流程

### 性能测试

- [ ] 列表加载速度 < 2秒 (100条数据)
- [ ] 详情加载速度 < 1秒
- [ ] 保存响应时间 < 2秒

### 代码质量

- [ ] 所有ViewModel行数 < 400
- [ ] 编译无警告
- [ ] 单元测试覆盖率 > 60%
- [ ] 集成测试通过

---

## 里程碑

| 里程碑 | 完成条件 | 目标日期 |
|--------|----------|----------|
| M1: 基础设施完成 | Phase 1全部任务完成 | TBD |
| M2: 数据层统一 | Phase 2全部任务完成 | TBD |
| M3: ViewModel瘦身 | Phase 3全部任务完成 | TBD |
| M4: 控件标准化 | Phase 4全部任务完成 | TBD |
| M5: 项目完成 | Phase 5全部任务完成 + 验收测试通过 | TBD |

---

**文档版本**: 1.0
**最后更新**: 2025-12-30
