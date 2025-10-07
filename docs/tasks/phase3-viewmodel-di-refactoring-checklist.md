# Phase 3: ViewModel 依赖注入重构任务清单

**创建时间**: 2025-10-07
**关联 Issue**: #1013
**分支**: feature/phase3-unify-viewmodel-di

---

## 📋 总体进度

- [x] Phase 3.1: 分析所有 ViewModel 依赖注入现状
- [x] Phase 3.2: 创建重构任务清单和分支
- [ ] Phase 3.3: 按模块重构 ViewModel 构造函数
- [ ] Phase 3.4: 编译验证和功能测试
- [ ] Phase 3.5: 创建 PR 并审核

---

## 🎯 Group A: 简单重排序 (22 个)

**预计时间**: 2 小时
**重构策略**: 仅调整参数顺序,确保业务服务在前,基类依赖紧随其后,可选依赖在最后

### Formula 模块 (3/3)

- [ ] `EditFormulaDialogViewModel.cs` (line 127-134)
  - 当前: IEventAggregator → IFormulaService
  - 目标: IFormulaService → IEventAggregator

- [ ] `ViewFormulaDialogViewModel.cs` (line 67-74)
  - 当前: IEventAggregator → IFormulaService
  - 目标: IFormulaService → IEventAggregator

- [ ] `FormulaManagementViewModel.cs`
  - 当前: IEventAggregator → IFormulaService
  - 目标: IFormulaService → IEventAggregator

### Herbs 模块 (2/2)

- [ ] `HerbManagementViewModel.cs` (line 35-40)
  - 当前: IEventAggregator → IHerbService
  - 目标: IHerbService → IEventAggregator

- [ ] `HerbDetailViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IHerbService → IEventAggregator

### MedicalCase 模块 (4/5)

- [ ] `MedicalCaseManagementViewModel.cs` (line 125-132)
  - 当前: IEventAggregator → IMedicalCaseService
  - 目标: IMedicalCaseService → IEventAggregator

- [ ] `CreateMedicalCaseViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IMedicalCaseService → IEventAggregator

- [ ] `MedicalCaseDetailViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IMedicalCaseService → IEventAggregator

- [ ] `MedicalCaseListViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IMedicalCaseService → IEventAggregator

- [ ] `RefactoredMedicalCaseListViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IMedicalCaseService → IEventAggregator

**注**: CreateMedicalCaseDialogViewModel 属于 Group C (骨架),暂不重构

### Consultation 模块 (1/2)

- [ ] `ConsultationManagementViewModel.cs` (line 84-90)
  - 当前: IConsultationService, IEventAggregator, ..., ISessionManager (非可选)
  - 目标: IConsultationService → IEventAggregator → ... → ISessionManager? (改为可选)

**注**: ConsultationMainViewModel 属于 Group B (多服务清理)

### Prescriptions 模块 (6/8)

- [ ] `PrescriptionsMainViewModel.cs` (line 119-126)
  - 当前: IEventAggregator → IPrescriptionService
  - 目标: IPrescriptionService → IEventAggregator

- [ ] `PrescriptionManagementViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IPrescriptionService → IEventAggregator

- [ ] `PrescriptionEditorDialogViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IPrescriptionService → IEventAggregator

- [ ] `HerbSelectionDialogViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IHerbService → IEventAggregator

- [ ] `FormulaTemplateDialogViewModel.cs` (line 141-148)
  - 当前: IEventAggregator → IFormulaService
  - 目标: IFormulaService → IEventAggregator

- [ ] `SelectFormulaDialogViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IFormulaService → IEventAggregator

**注**: PrescriptionViewModel, PrescriptionItemViewModel 属于 Group C (骨架)
**注**: PrescriptionComposerViewModel 属于 Group D (多服务特殊处理)

### Users 模块 (6/6)

- [ ] `ChangePasswordDialogViewModel.cs` (line 141-148)
  - 当前: IEventAggregator → AuthService
  - 目标: AuthService → IEventAggregator

- [ ] `ResetPasswordDialogViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IUserService → IEventAggregator

- [ ] `UserCreateViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IUserService → IEventAggregator

- [ ] `UserEditViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IUserService → IEventAggregator

- [ ] `UserManagementViewModel.cs`
  - 当前: 已读取 (line 35-40)
  - 目标: IUserService → IEventAggregator

- [ ] `UserProfileDialogViewModel.cs`
  - 当前: 需读取完整构造函数
  - 目标: IUserService, ISessionManager → IEventAggregator

**注**: UserDetailViewModel 属于 Group C (骨架)

---

## 🔧 Group B: 清理额外依赖 (2 个)

**预计时间**: 1 小时
**重构策略**: 调整参数顺序 + 清理多余依赖 + 处理可选参数

### Patients 模块 (1/1)

- [ ] `PatientDetailViewModel.cs`
  - **问题**:
    - IMapper 不应在 ViewModel 构造函数 (应由 Service 层注入)
    - 重复注入 IRegionManager (参数名: navigationService + regionManager)
    - IPrescriptionPrintService 应作为可选依赖
  - **当前**:
    ```csharp
    public PatientDetailViewModel(
        IPatientService patientService,
        IRegionManager navigationService,      // ❌ 重复
        IMapper mapper,                         // ❌ 不应出现
        IPrescriptionPrintService printService, // ❌ 应为可选
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,           // ❌ 重复
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
    ```
  - **目标**:
    ```csharp
    public PatientDetailViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null,
        IPrescriptionPrintService? printService = null)
    ```
  - **额外工作**: 检查 ViewModel 内部是否使用 IMapper,如有则需重构为调用 Service

### Consultation 模块 (1/1)

- [ ] `ConsultationMainViewModel.cs` (line 136-144)
  - **问题**:
    - 多个业务服务 (IConsultationService, IMedicalCaseService, IPatientService)
    - ISessionManager 不是可选参数
  - **当前**:
    ```csharp
    public ConsultationMainViewModel(
        IConsultationService consultationService,
        IMedicalCaseService medicalCaseService,
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager)         // ❌ 应为可选
    ```
  - **目标**:
    ```csharp
    public ConsultationMainViewModel(
        IConsultationService consultationService,
        IMedicalCaseService medicalCaseService,
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
    ```

---

## 🚧 Group C: 骨架 ViewModel 待补充 (5 个)

**预计时间**: 0.5 小时
**策略**: 添加 TODO 注释,标记待 Phase 4C 补充业务服务

- [ ] `LoginWindowViewModel.cs` (Auth 模块)
  - 仅有 ILoggerFactory,待补充 IAuthService

- [ ] `PrescriptionViewModel.cs` (Prescriptions 模块)
  - 仅有基类依赖,待补充 IPrescriptionService

- [ ] `PrescriptionItemViewModel.cs` (Prescriptions 模块)
  - 仅有基类依赖,待补充业务服务 (如有必要)

- [ ] `UserDetailViewModel.cs` (Users 模块)
  - 仅有基类依赖,待补充 IUserService

- [ ] `CreateMedicalCaseDialogViewModel.cs` (MedicalCase 模块)
  - 仅有基类依赖,待补充 IMedicalCaseService

**行动**:
```csharp
// TODO: Phase 4C - 待补充业务服务依赖
// 当前为骨架实现,仅包含基类依赖
// 集成业务服务后需调整构造函数顺序为: 业务服务 → 基类依赖 → 可选依赖
public XxxViewModel(
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager)
    : base(eventAggregator, loggerFactory, regionManager, null, null)
{
    // ...
}
```

---

## 🎨 Group D: 特殊处理 (2 个)

**预计时间**: 1 小时
**策略**: 需要额外架构调整

### Patients 模块 (1/1)

- [ ] `PatientImportWizardViewModel.cs`
  - **问题**: 不继承 UnifiedViewModelBase,继承 BindableBase + IDisposable
  - **当前**:
    ```csharp
    public class PatientImportWizardViewModel : BindableBase, IDisposable
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientImportWizardViewModel> _logger;
        // 构造函数未在前150行
    }
    ```
  - **目标**: 改为继承 UnifiedViewModelBase
  - **风险**: 可能需要处理 IDisposable 实现冲突,需检查 Dispose 方法内容
  - **额外工作**:
    1. 读取完整文件,确认 Dispose 逻辑
    2. 如有必要,在 UnifiedViewModelBase 中添加 IDisposable 支持
    3. 或保留 IDisposable 实现在子类

### Prescriptions 模块 (1/1)

- [ ] `PrescriptionComposerViewModel.cs`
  - **问题**: 包含多个服务依赖和组件依赖
  - **当前** (需读取完整构造函数):
    ```csharp
    private readonly IPrescriptionService _prescriptionService;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly PrescriptionDataManager _dataManager;
    private readonly PrescriptionCalculator _calculator;
    private readonly PrescriptionValidator _validator;
    private readonly PrescriptionCommandHandler _commandHandler;
    private readonly PrescriptionEventCoordinator _eventCoordinator;
    ```
  - **策略**:
    1. 读取完整构造函数
    2. 分析组件依赖是否应通过服务层注入
    3. 调整顺序: 业务服务 → 基类依赖 → 组件依赖 → 可选依赖

---

## 📝 重构检查清单 (每个 ViewModel 必检)

每完成一个 ViewModel 重构后,必须检查:

- [ ] ✅ 业务服务在第一位
- [ ] ✅ 基类必需依赖 (IEventAggregator, ILoggerFactory, IRegionManager) 紧随其后
- [ ] ✅ 可选依赖在最后,且使用 `= null` 语法
- [ ] ✅ 构造函数内部 null 检查顺序与参数顺序一致
- [ ] ✅ 文件编译通过 (无红线错误)
- [ ] ✅ 无警告产生

---

## 🔄 执行顺序

1. **Group A** → 2. **Group B** → 3. **Group C** → 4. **Group D**

每完成一个 Group 后:
1. 运行编译验证: `dotnet build LYBT.Desktop.sln`
2. 检查编译输出,确认无错误
3. 提交代码: `git add . && git commit -m "[SRV-X] Group X 重构完成"`

---

**最后更新**: Phase 3.2 创建
**下一步**: 开始执行 Group A 重构
