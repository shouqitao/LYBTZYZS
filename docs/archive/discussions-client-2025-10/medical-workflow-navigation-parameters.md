# 看诊流程NavigationParameters规范

**版本**: v1.0
**创建时间**: 2025-10-21
**适用范围**: Issue #1557 - 看诊流程模块化迁移
**设计目标**: 定义Prism Region导航参数的标准化规范

---

## 📋 概述

看诊流程采用**Prism Region导航机制**，通过`NavigationParameters`在流程协调器和各步骤ViewModel之间传递上下文信息。本规范定义了参数命名、类型、必填性和使用方式。

---

## 🎯 设计原则

1. **参数键标准化** - 所有参数键使用统一的命名规范（PascalCase）
2. **类型安全** - 使用强类型参数，避免字符串解析错误
3. **最小传递** - 只传递必要的上下文信息，避免传递大对象
4. **向后兼容** - 参数键不可随意变更，确保兼容性

---

## 📦 标准参数定义

### 全局参数（所有步骤通用）

| 参数键 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| `MedicalCaseFlowId` | `Guid` | ✅ | 医案流程唯一标识（用于关联草稿数据） |
| `FlowContext` | `string` | ✅ | 流程上下文（"NewMedicalCase"/"EditDraft"/"ViewOnly"） |
| `UserId` | `Guid` | ✅ | 当前登录用户ID（医生ID） |
| `ClinicId` | `Guid` | ✅ | 当前诊所ID |

### Step 1: 患者选择 - 导航参数

**导航目标**: `PatientSelectionView`（Patients模块）

**参数定义**:
```csharp
var parameters = new NavigationParameters
{
    // ===== 全局参数 =====
    { "MedicalCaseFlowId", Guid.NewGuid() },  // 新建流程时生成，编辑草稿时传入已有ID
    { "FlowContext", "NewMedicalCase" },      // "NewMedicalCase" | "EditDraft"
    { "UserId", currentUserId },
    { "ClinicId", currentClinicId },

    // ===== Step 1特定参数 =====
    { "PreSelectedPatientId", Guid.Empty }    // 可选，预选患者ID（用于"继续编辑"场景）
};

_regionManager.RequestNavigate("WorkflowContentRegion", "PatientSelectionView", parameters);
```

**参数说明**:
- `PreSelectedPatientId` - 当恢复草稿时，传入之前选择的患者ID，用于自动高亮显示

**ViewModel接收示例**:
```csharp
public class PatientSelectionViewModel : INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 接收全局参数
        MedicalCaseFlowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
        FlowContext = navigationContext.Parameters.GetValue<string>("FlowContext");
        UserId = navigationContext.Parameters.GetValue<Guid>("UserId");
        ClinicId = navigationContext.Parameters.GetValue<Guid>("ClinicId");

        // 接收Step 1特定参数
        var preSelectedPatientId = navigationContext.Parameters.GetValue<Guid>("PreSelectedPatientId");
        if (preSelectedPatientId != Guid.Empty)
        {
            // 自动选中该患者
            LoadAndSelectPatient(preSelectedPatientId);
        }
    }
}
```

---

### Step 2: 诊断填写 - 导航参数

**导航目标**: `ConsultationDetailView`（Consultation模块）

**参数定义**:
```csharp
var parameters = new NavigationParameters
{
    // ===== 全局参数 =====
    { "MedicalCaseFlowId", this.MedicalCaseFlowId },
    { "FlowContext", "NewMedicalCase" },
    { "UserId", currentUserId },
    { "ClinicId", currentClinicId },

    // ===== Step 2特定参数 =====
    { "PatientId", selectedPatientId },           // 必填，从Step 1传入
    { "PatientName", selectedPatientName },       // 必填，用于显示
    { "ConsultationId", Guid.Empty }              // 可选，编辑草稿时传入已有ConsultationId
};

_regionManager.RequestNavigate("WorkflowContentRegion", "ConsultationDetailView", parameters);
```

**参数说明**:
- `PatientId` - 从Step 1选择的患者ID，用于创建诊断记录
- `PatientName` - 患者姓名，用于界面显示
- `ConsultationId` - 如果是编辑草稿，传入已有的诊断ID；新建时为`Guid.Empty`

**ViewModel接收示例**:
```csharp
public class ConsultationDetailViewModel : INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 接收全局参数
        MedicalCaseFlowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
        FlowContext = navigationContext.Parameters.GetValue<string>("FlowContext");

        // 接收Step 2特定参数
        PatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
        PatientName = navigationContext.Parameters.GetValue<string>("PatientName");
        ConsultationId = navigationContext.Parameters.GetValue<Guid>("ConsultationId");

        // 如果是编辑草稿，加载已有数据
        if (ConsultationId != Guid.Empty)
        {
            LoadConsultationDraft(ConsultationId);
        }
    }
}
```

---

### Step 3: 处方编辑 - 导航参数

**导航目标**: `PrescriptionEditorView`（Prescriptions模块）

**参数定义**:
```csharp
var parameters = new NavigationParameters
{
    // ===== 全局参数 =====
    { "MedicalCaseFlowId", this.MedicalCaseFlowId },
    { "FlowContext", "NewMedicalCase" },
    { "UserId", currentUserId },
    { "ClinicId", currentClinicId },

    // ===== Step 3特定参数 =====
    { "PatientId", selectedPatientId },           // 必填
    { "ConsultationId", consultationId },         // 必填，从Step 2传入
    { "PrescriptionId", Guid.Empty },             // 可选，编辑草稿时传入已有PrescriptionId
    { "IsReadOnly", false }                       // 可选，是否只读模式（用于复用场景）
};

_regionManager.RequestNavigate("WorkflowContentRegion", "PrescriptionEditorView", parameters);
```

**参数说明**:
- `PatientId` - 患者ID（用于关联处方）
- `ConsultationId` - 诊断ID（用于关联处方与诊断）
- `PrescriptionId` - 如果是编辑草稿，传入已有的处方ID；新建时为`Guid.Empty`
- `IsReadOnly` - 是否只读模式（复用场景，如历史处方查看）

**ViewModel接收示例**:
```csharp
public class PrescriptionEditorViewModel : INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 接收全局参数
        MedicalCaseFlowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
        FlowContext = navigationContext.Parameters.GetValue<string>("FlowContext");

        // 接收Step 3特定参数
        PatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
        ConsultationId = navigationContext.Parameters.GetValue<Guid>("ConsultationId");
        PrescriptionId = navigationContext.Parameters.GetValue<Guid>("PrescriptionId");
        IsReadOnly = navigationContext.Parameters.GetValue<bool>("IsReadOnly");

        // 如果是只读模式，禁用所有编辑功能
        if (IsReadOnly)
        {
            DisableEditing();
        }

        // 如果是编辑草稿，加载已有数据
        if (PrescriptionId != Guid.Empty)
        {
            LoadPrescriptionDraft(PrescriptionId);
        }
    }
}
```

---

### Step 4: 完成医案 - 导航参数

**导航目标**: `CompletionView`（MedicalCase模块）

**参数定义**:
```csharp
var parameters = new NavigationParameters
{
    // ===== 全局参数 =====
    { "MedicalCaseFlowId", this.MedicalCaseFlowId },
    { "FlowContext", "NewMedicalCase" },
    { "UserId", currentUserId },
    { "ClinicId", currentClinicId },

    // ===== Step 4特定参数 =====
    { "PatientId", selectedPatientId },           // 必填
    { "PatientName", selectedPatientName },       // 必填
    { "ConsultationId", consultationId },         // 必填
    { "PrescriptionId", prescriptionId },         // 必填
    { "TotalAmount", totalAmount }                // 必填，处方总金额
};

_regionManager.RequestNavigate("WorkflowContentRegion", "CompletionView", parameters);
```

**参数说明**:
- `PatientId` - 患者ID
- `PatientName` - 患者姓名
- `ConsultationId` - 诊断ID
- `PrescriptionId` - 处方ID
- `TotalAmount` - 处方总金额（用于显示摘要）

**ViewModel接收示例**:
```csharp
public class CompletionViewModel : INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 接收全局参数
        MedicalCaseFlowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");

        // 接收Step 4特定参数
        PatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
        PatientName = navigationContext.Parameters.GetValue<string>("PatientName");
        ConsultationId = navigationContext.Parameters.GetValue<Guid>("ConsultationId");
        PrescriptionId = navigationContext.Parameters.GetValue<Guid>("PrescriptionId");
        TotalAmount = navigationContext.Parameters.GetValue<decimal>("TotalAmount");

        // 加载完整的医案摘要
        LoadMedicalCaseSummary();
    }
}
```

---

## 🔄 参数传递流程图

```
┌──────────────────────────────────────────────────────────────┐
│                  MedicalCaseFlowViewModel                     │
│                    (流程协调器/导航发起者)                     │
└────────┬───────────────┬──────────────┬──────────────┬───────┘
         │               │              │              │
         │ Navigate      │ Navigate     │ Navigate     │ Navigate
         │ + Params      │ + Params     │ + Params     │ + Params
         │               │              │              │
         ▼               ▼              ▼              ▼
   ┌────────────┐  ┌────────────┐ ┌─────────────┐ ┌────────────┐
   │  Patient   │  │Consultation│ │Prescription │ │ Completion │
   │ Selection  │  │   Detail   │ │   Editor    │ │    View    │
   │    View    │  │    View    │ │    View     │ │            │
   └────────────┘  └────────────┘ └─────────────┘ └────────────┘
         │               │              │              │
         │ Receive       │ Receive      │ Receive      │ Receive
         │ Params        │ Params       │ Params       │ Params
         │               │              │              │
         ▼               ▼              ▼              ▼
   ┌────────────┐  ┌────────────┐ ┌─────────────┐ ┌────────────┐
   │  Patient   │  │Consultation│ │Prescription │ │ Completion │
   │ SelectionVM│  │  DetailVM  │ │  EditorVM   │ │  ViewModel │
   └────────────┘  └────────────┘ └─────────────┘ └────────────┘
```

---

## 🔧 辅助工具类（可选）

为了简化参数传递，可以创建辅助工具类：

```csharp
namespace LYBT.Desktop.Core.Navigation
{
    /// <summary>
    /// 看诊流程导航参数键常量
    /// </summary>
    public static class MedicalWorkflowNavigationKeys
    {
        // 全局参数
        public const string MedicalCaseFlowId = "MedicalCaseFlowId";
        public const string FlowContext = "FlowContext";
        public const string UserId = "UserId";
        public const string ClinicId = "ClinicId";

        // Step 1特定参数
        public const string PreSelectedPatientId = "PreSelectedPatientId";

        // Step 2特定参数
        public const string PatientId = "PatientId";
        public const string PatientName = "PatientName";
        public const string ConsultationId = "ConsultationId";

        // Step 3特定参数
        public const string PrescriptionId = "PrescriptionId";
        public const string IsReadOnly = "IsReadOnly";

        // Step 4特定参数
        public const string TotalAmount = "TotalAmount";
    }

    /// <summary>
    /// 流程上下文枚举
    /// </summary>
    public enum FlowContextType
    {
        /// <summary>
        /// 新建医案
        /// </summary>
        NewMedicalCase,

        /// <summary>
        /// 编辑草稿
        /// </summary>
        EditDraft,

        /// <summary>
        /// 只读查看
        /// </summary>
        ViewOnly
    }

    /// <summary>
    /// 导航参数构建器
    /// </summary>
    public class MedicalWorkflowNavigationParametersBuilder
    {
        private readonly NavigationParameters _parameters = new();

        public MedicalWorkflowNavigationParametersBuilder WithGlobalContext(
            Guid medicalCaseFlowId,
            FlowContextType flowContext,
            Guid userId,
            Guid clinicId)
        {
            _parameters.Add(MedicalWorkflowNavigationKeys.MedicalCaseFlowId, medicalCaseFlowId);
            _parameters.Add(MedicalWorkflowNavigationKeys.FlowContext, flowContext.ToString());
            _parameters.Add(MedicalWorkflowNavigationKeys.UserId, userId);
            _parameters.Add(MedicalWorkflowNavigationKeys.ClinicId, clinicId);
            return this;
        }

        public MedicalWorkflowNavigationParametersBuilder WithPatientSelection(Guid? preSelectedPatientId = null)
        {
            if (preSelectedPatientId.HasValue)
                _parameters.Add(MedicalWorkflowNavigationKeys.PreSelectedPatientId, preSelectedPatientId.Value);
            return this;
        }

        public MedicalWorkflowNavigationParametersBuilder WithConsultationDetail(
            Guid patientId,
            string patientName,
            Guid? consultationId = null)
        {
            _parameters.Add(MedicalWorkflowNavigationKeys.PatientId, patientId);
            _parameters.Add(MedicalWorkflowNavigationKeys.PatientName, patientName);
            _parameters.Add(MedicalWorkflowNavigationKeys.ConsultationId, consultationId ?? Guid.Empty);
            return this;
        }

        public MedicalWorkflowNavigationParametersBuilder WithPrescriptionEditor(
            Guid patientId,
            Guid consultationId,
            Guid? prescriptionId = null,
            bool isReadOnly = false)
        {
            _parameters.Add(MedicalWorkflowNavigationKeys.PatientId, patientId);
            _parameters.Add(MedicalWorkflowNavigationKeys.ConsultationId, consultationId);
            _parameters.Add(MedicalWorkflowNavigationKeys.PrescriptionId, prescriptionId ?? Guid.Empty);
            _parameters.Add(MedicalWorkflowNavigationKeys.IsReadOnly, isReadOnly);
            return this;
        }

        public NavigationParameters Build() => _parameters;
    }
}
```

**使用示例**:
```csharp
// 构建Step 2导航参数
var parameters = new MedicalWorkflowNavigationParametersBuilder()
    .WithGlobalContext(
        medicalCaseFlowId: this.MedicalCaseFlowId,
        flowContext: FlowContextType.NewMedicalCase,
        userId: this.CurrentUserId,
        clinicId: this.CurrentClinicId)
    .WithConsultationDetail(
        patientId: selectedPatientId,
        patientName: selectedPatientName,
        consultationId: null)
    .Build();

_regionManager.RequestNavigate("WorkflowContentRegion", "ConsultationDetailView", parameters);
```

---

## ⚠️ 注意事项

### 1. 参数键命名规范
- 使用PascalCase命名
- 避免缩写，保持可读性
- 参数键不可随意变更（向后兼容）

### 2. 类型安全
使用泛型方法获取参数，避免类型转换错误：

```csharp
// ✅ 正确
var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");

// ❌ 错误
var patientId = (Guid)navigationContext.Parameters["PatientId"];  // 可能抛出InvalidCastException
```

### 3. 默认值处理
对于可选参数，提供合理的默认值：

```csharp
// 使用TryGetValue避免KeyNotFoundException
if (navigationContext.Parameters.TryGetValue("PreSelectedPatientId", out Guid preSelectedPatientId))
{
    // 参数存在，执行逻辑
}

// 或使用默认值
var isReadOnly = navigationContext.Parameters.GetValue<bool>("IsReadOnly") ?? false;
```

---

## 📚 参考文档

- **Prism Navigation官方文档**: https://prismlibrary.com/docs/navigation.html
- **事件聚合器契约**: `docs/explanation/architecture/client/medical-workflow-events-contract.md`
- **Issue #1557**: 看诊流程模块化迁移

---

**文档状态**: ✅ 设计完成
**下一步**: 开始Phase 1准备工作
