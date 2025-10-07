# Phase 3.1: ViewModel 依赖注入分析报告

**生成时间**: 2025-10-07
**分析范围**: src/Client/Desktop/Modules/**/ViewModels/*.cs
**分析目标**: 检查所有 ViewModel 构造函数是否符合统一设计标准

---

## 📋 分析摘要

| 指标 | 数量 | 百分比 |
|------|------|--------|
| **总计 ViewModel** | 34 | 100% |
| **完全符合标准** | 1 | 2.9% |
| **需要重构** | 33 | 97.1% |

---

## ✅ 标准模式 (来自 unified-design-standard.md)

```csharp
public {Entity}ViewModel(
    I{Entity}Service {entity}Service,           // 1. 业务服务
    IEventAggregator eventAggregator,          // 2. 基类必需
    ILoggerFactory loggerFactory,              // 3. 基类必需
    IRegionManager regionManager,              // 4. 基类必需
    ISessionManager? sessionManager = null,     // 5. 可选依赖
    IUserNotificationService? userNotificationService = null  // 6. 可选依赖
) : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
```

**关键原则**:
1. 业务服务始终排在第一位
2. 基类必需依赖 (IEventAggregator, ILoggerFactory, IRegionManager) 紧随其后
3. 可选依赖放在最后,使用 `= null`

---

## 📊 问题分类统计

### 问题类型分布

| 问题类型 | 数量 | 示例 |
|---------|------|------|
| **Type A: 基类依赖在前** | 22 | EditFormulaDialogViewModel, HerbManagementViewModel |
| **Type B: 无业务服务 (骨架)** | 4 | LoginWindowViewModel, PrescriptionViewModel |
| **Type C: 不继承统一基类** | 1 | PatientImportWizardViewModel |
| **Type D: 业务服务在前但顺序混乱** | 6 | ConsultationMainViewModel, PatientDetailViewModel |
| **Type E: 完全符合标准** | 1 | LoginViewModel |

---

## 📁 按模块详细分析

### 🔐 Auth 模块 (2 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ✅ **LoginViewModel** | Auth/ViewModels/LoginViewModel.cs | 符合 | - | 35-42 |
| ❌ LoginWindowViewModel | Auth/ViewModels/LoginWindowViewModel.cs | 不符合 | Type B (骨架) | 61 |

**LoginViewModel 当前构造函数 (✅ 正确示例)**:
```csharp
public LoginViewModel(
    ILocalAuthService authService,              // ✅ 业务服务在前
    IEventAggregator eventAggregator,          // ✅ 基类依赖
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    IApiHealthCheckService? apiHealthCheckService = null,  // ✅ 可选依赖
    LYBT.Desktop.Services.Business.IUsernameStorageService? usernameStorage = null)
```

**LoginWindowViewModel 问题**:
```csharp
// ❌ 当前: 仅骨架,无业务服务
public LoginWindowViewModel(ILoggerFactory loggerFactory)

// ✅ 应修改为: 待集成业务服务时补充
```

---

### 💊 Consultation 模块 (2 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ ConsultationMainViewModel | Consultation/ViewModels/ConsultationMainViewModel.cs | 不符合 | Type D | 136-144 |
| ❌ ConsultationManagementViewModel | Consultation/ViewModels/ConsultationManagementViewModel.cs | 不符合 | Type A | 84-90 |

**ConsultationMainViewModel 问题**:
```csharp
// ❌ 当前: 多个业务服务在前,基类依赖在后
public ConsultationMainViewModel(
    IConsultationService consultationService,   // 业务服务
    IMedicalCaseService medicalCaseService,     // 业务服务
    IPatientService patientService,             // 业务服务
    IEventAggregator eventAggregator,           // 基类依赖 (顺序错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager sessionManager)

// ✅ 应修改为:
public ConsultationMainViewModel(
    IConsultationService consultationService,   // 主要业务服务
    IMedicalCaseService medicalCaseService,     // 次要业务服务
    IPatientService patientService,             // 次要业务服务
    IEventAggregator eventAggregator,           // 基类依赖
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,     // 改为可选
    IUserNotificationService? userNotificationService = null)
```

**ConsultationManagementViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public ConsultationManagementViewModel(
    IConsultationService consultationService,
    IEventAggregator eventAggregator,   // 顺序错误
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager sessionManager)

// ✅ 应修改为:
public ConsultationManagementViewModel(
    IConsultationService consultationService,   // 业务服务在前
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,     // 改为可选
    IUserNotificationService? userNotificationService = null)
```

---

### 🧪 Formula 模块 (4 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ EditFormulaDialogViewModel | Formula/ViewModels/EditFormulaDialogViewModel.cs | 不符合 | Type A | 127-134 |
| ❌ FormulaDetailViewModel | Formula/ViewModels/FormulaDetailViewModel.cs | 不符合 | Type A | (未读) |
| ❌ FormulaManagementViewModel | Formula/ViewModels/FormulaManagementViewModel.cs | 不符合 | Type A | (已读) |
| ❌ ViewFormulaDialogViewModel | Formula/ViewModels/ViewFormulaDialogViewModel.cs | 不符合 | Type A | 67-74 |

**EditFormulaDialogViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public EditFormulaDialogViewModel(
    IEventAggregator eventAggregator,    // 基类依赖在前 (错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    IFormulaService formulaService,      // 业务服务在后 (错误)
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public EditFormulaDialogViewModel(
    IFormulaService formulaService,      // 业务服务在前
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
```

---

### 🌿 Herbs 模块 (2 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ HerbDetailViewModel | Herbs/ViewModels/HerbDetailViewModel.cs | 不符合 | Type A | (未读) |
| ❌ HerbManagementViewModel | Herbs/ViewModels/HerbManagementViewModel.cs | 不符合 | Type A | 35-40 |

**HerbManagementViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public HerbManagementViewModel(
    IEventAggregator eventAggregator,    // 基类依赖在前 (错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    IHerbService herbService,            // 业务服务在后 (错误)
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public HerbManagementViewModel(
    IHerbService herbService,            // 业务服务在前
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
```

---

### 🏥 MedicalCase 模块 (6 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ CreateMedicalCaseDialogViewModel | MedicalCase/ViewModels/CreateMedicalCaseDialogViewModel.cs | 不符合 | Type B | 79-85 |
| ❌ CreateMedicalCaseViewModel | MedicalCase/ViewModels/CreateMedicalCaseViewModel.cs | 不符合 | Type A | (未读) |
| ❌ MedicalCaseDetailViewModel | MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs | 不符合 | Type A | (未读) |
| ❌ MedicalCaseListViewModel | MedicalCase/ViewModels/MedicalCaseListViewModel.cs | 不符合 | Type A | (未读) |
| ❌ MedicalCaseManagementViewModel | MedicalCase/ViewModels/MedicalCaseManagementViewModel.cs | 不符合 | Type A | 125-132 |
| ❌ RefactoredMedicalCaseListViewModel | MedicalCase/ViewModels/RefactoredMedicalCaseListViewModel.cs | 不符合 | Type A | (未读) |

**MedicalCaseManagementViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public MedicalCaseManagementViewModel(
    IEventAggregator eventAggregator,           // 基类依赖在前 (错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    IMedicalCaseService medicalCaseService,     // 业务服务在后 (错误)
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public MedicalCaseManagementViewModel(
    IMedicalCaseService medicalCaseService,     // 业务服务在前
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
```

---

### 🧑‍⚕️ Patients 模块 (2 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ PatientDetailViewModel | Patients/ViewModels/PatientDetailViewModel.cs | 不符合 | Type D | (已读) |
| ❌ PatientImportWizardViewModel | Patients/ViewModels/PatientImportWizardViewModel.cs | 不符合 | Type C | (不继承统一基类) |

**PatientDetailViewModel 问题**:
```csharp
// ❌ 当前: 业务服务在前但有多余参数混杂
public PatientDetailViewModel(
    IPatientService patientService,     // ✅ 业务服务在前
    IRegionManager navigationService,   // ❌ 基类依赖参数名错误
    IMapper mapper,                      // ❌ 不应出现在构造函数 (应由服务层注入)
    IPrescriptionPrintService printService,  // ❌ 应作为可选依赖
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,        // ❌ 重复注入 IRegionManager
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public PatientDetailViewModel(
    IPatientService patientService,     // 业务服务
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null,
    IPrescriptionPrintService? printService = null)  // 可选依赖
```

**PatientImportWizardViewModel 问题**:
```csharp
// ❌ 当前: 不继承 UnifiedViewModelBase
public class PatientImportWizardViewModel : BindableBase, IDisposable
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientImportWizardViewModel> _logger;

    // 构造函数未定义在前150行
}

// ✅ 应修改为: 继承 UnifiedViewModelBase 并统一构造函数
public class PatientImportWizardViewModel : UnifiedViewModelBase
{
    public PatientImportWizardViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
    }
}
```

---

### 💊 Prescriptions 模块 (8 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ FormulaTemplateDialogViewModel | Prescriptions/ViewModels/FormulaTemplateDialogViewModel.cs | 不符合 | Type A | 141-148 |
| ❌ HerbSelectionDialogViewModel | Prescriptions/ViewModels/HerbSelectionDialogViewModel.cs | 不符合 | Type A | (未读) |
| ❌ PrescriptionComposerViewModel | Prescriptions/ViewModels/PrescriptionComposerViewModel.cs | 不符合 | Type D | (未读,多服务) |
| ❌ PrescriptionEditorDialogViewModel | Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs | 不符合 | Type A | (未读) |
| ❌ PrescriptionItemViewModel | Prescriptions/ViewModels/PrescriptionItemViewModel.cs | 不符合 | Type B | 115-121 |
| ❌ PrescriptionManagementViewModel | Prescriptions/ViewModels/PrescriptionManagementViewModel.cs | 不符合 | Type A | (未读) |
| ❌ PrescriptionsMainViewModel | Prescriptions/ViewModels/PrescriptionsMainViewModel.cs | 不符合 | Type A | 119-126 |
| ❌ PrescriptionViewModel | Prescriptions/ViewModels/PrescriptionViewModel.cs | 不符合 | Type B (骨架) | 60-64 |
| ❌ SelectFormulaDialogViewModel | Prescriptions/ViewModels/SelectFormulaDialogViewModel.cs | 不符合 | Type A | (未读) |

**PrescriptionsMainViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public PrescriptionsMainViewModel(
    IEventAggregator eventAggregator,           // 基类依赖在前 (错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    IPrescriptionService prescriptionService,   // 业务服务在后 (错误)
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public PrescriptionsMainViewModel(
    IPrescriptionService prescriptionService,   // 业务服务在前
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
```

---

### 👤 Users 模块 (6 个 ViewModel)

| ViewModel | 文件路径 | 状态 | 问题类型 | 行号 |
|-----------|---------|------|---------|------|
| ❌ ChangePasswordDialogViewModel | Users/ViewModels/ChangePasswordDialogViewModel.cs | 不符合 | Type A | 141-148 |
| ❌ ResetPasswordDialogViewModel | Users/ViewModels/ResetPasswordDialogViewModel.cs | 不符合 | Type A | (未读) |
| ❌ UserCreateViewModel | Users/ViewModels/UserCreateViewModel.cs | 不符合 | Type A | (未读) |
| ❌ UserDetailViewModel | Users/ViewModels/UserDetailViewModel.cs | 不符合 | Type B (骨架) | 41-45 |
| ❌ UserEditViewModel | Users/ViewModels/UserEditViewModel.cs | 不符合 | Type A | (未读) |
| ❌ UserManagementViewModel | Users/ViewModels/UserManagementViewModel.cs | 不符合 | Type A | (已读) |
| ❌ UserProfileDialogViewModel | Users/ViewModels/UserProfileDialogViewModel.cs | 不符合 | Type A | (未读) |

**ChangePasswordDialogViewModel 问题**:
```csharp
// ❌ 当前: 基类依赖在业务服务之前
public ChangePasswordDialogViewModel(
    IEventAggregator eventAggregator,    // 基类依赖在前 (错误)
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    AuthService authService,             // 业务服务在后 (错误)
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)

// ✅ 应修改为:
public ChangePasswordDialogViewModel(
    AuthService authService,             // 业务服务在前 (或使用接口 IAuthService)
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
```

---

## 🎯 重构策略

### 分组策略 (按重构复杂度)

#### **Group A: 简单重排序 (22 个) - 预计 2 小时**

仅需调整参数顺序,无需改变逻辑:

- EditFormulaDialogViewModel
- ViewFormulaDialogViewModel
- FormulaManagementViewModel
- HerbManagementViewModel
- HerbDetailViewModel
- MedicalCaseManagementViewModel
- ConsultationManagementViewModel
- PrescriptionsMainViewModel
- PrescriptionManagementViewModel
- PrescriptionEditorDialogViewModel
- HerbSelectionDialogViewModel
- FormulaTemplateDialogViewModel
- SelectFormulaDialogViewModel
- ChangePasswordDialogViewModel
- ResetPasswordDialogViewModel
- UserCreateViewModel
- UserEditViewModel
- UserManagementViewModel
- UserProfileDialogViewModel
- CreateMedicalCaseViewModel
- MedicalCaseDetailViewModel
- MedicalCaseListViewModel
- RefactoredMedicalCaseListViewModel

**重构步骤**:
1. 读取完整构造函数
2. 调整参数顺序: 业务服务 → 基类依赖 → 可选依赖
3. 确保可选依赖使用 `= null`
4. 验证编译通过

---

#### **Group B: 需要清理额外依赖 (2 个) - 预计 1 小时**

需要移除多余参数或合并重复依赖:

- **PatientDetailViewModel**: 移除 IMapper, 合并重复的 IRegionManager, 将 IPrescriptionPrintService 改为可选
- **ConsultationMainViewModel**: 保持多个业务服务,但调整顺序和可选参数

---

#### **Group C: 骨架 ViewModel 待补充 (4 个) - 预计 0.5 小时**

暂时不需要重构,待 Phase 4C 集成业务服务时再处理:

- LoginWindowViewModel
- PrescriptionViewModel
- UserDetailViewModel
- CreateMedicalCaseDialogViewModel
- PrescriptionItemViewModel

**标记为**: 待补充业务服务 (Phase 4C)

---

#### **Group D: 特殊处理 (2 个) - 预计 1 小时**

需要特殊处理:

- **PatientImportWizardViewModel**: 需要改为继承 UnifiedViewModelBase,可能需要调整 IDisposable 实现
- **PrescriptionComposerViewModel**: 包含多个服务依赖和组件依赖,需要仔细分析后重构

---

### 总预计时间: **4.5 小时**

---

## 📝 下一步行动 (Phase 3.2)

1. **创建重构分支**: `git checkout -b feature/phase3-unify-viewmodel-di`
2. **按 Group A → B → D 顺序依次重构**
3. **每完成一个 Group 后编译验证**
4. **Group C 标记 TODO 注释,待 Phase 4C 补充**
5. **完成后运行完整编译和基础功能测试**
6. **提交 PR 并审核**

---

**报告生成工具**: Claude Code + Serena MCP
**分析依据**: docs/architecture/client/unified-design-standard.md
