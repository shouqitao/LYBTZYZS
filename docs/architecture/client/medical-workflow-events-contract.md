# 看诊流程事件聚合器契约设计

**版本**: v1.0
**创建时间**: 2025-10-21
**适用范围**: Issue #1557 - 看诊流程模块化迁移
**设计目标**: 定义看诊流程各步骤之间的事件通信契约

---

## 📋 概述

看诊流程采用**事件驱动架构（EDA）**，通过Prism的`EventAggregator`实现模块间松耦合通信。各步骤的View/ViewModel迁移到独立模块后，通过发布/订阅事件传递流程状态和数据。

---

## 🎯 设计原则

1. **单向数据流** - 事件只能由子步骤发送给流程协调器，不允许反向
2. **事件独立性** - 每个事件携带完整的上下文信息，不依赖全局状态
3. **类型安全** - 所有事件使用强类型Payload，避免弱类型字典
4. **可追溯性** - 事件包含时间戳和流程ID，便于调试和日志追踪

---

## 📦 事件定义

### 1. PatientSelectedEvent（患者选择完成事件）

**触发时机**: Step 1 - 用户选择患者并点击"下一步"

**发布者**: `PatientSelectionViewModel`（Patients模块）

**订阅者**: `MedicalCaseFlowViewModel`（MedicalCase模块）

**Payload结构**:
```csharp
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 患者选择完成事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload>
    {
    }

    /// <summary>
    /// 患者选择事件载荷
    /// </summary>
    public class PatientSelectedPayload
    {
        /// <summary>
        /// 选中的患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名（用于显示在患者信息条）
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 患者性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 患者年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 患者联系电话
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 医案流程ID（由流程协调器传入）
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

**使用示例**:
```csharp
// 发布事件（PatientSelectionViewModel）
var payload = new PatientSelectedPayload
{
    PatientId = selectedPatient.Id,
    PatientName = selectedPatient.Name,
    Gender = selectedPatient.Gender,
    Age = selectedPatient.Age,
    PhoneNumber = selectedPatient.PhoneNumber,
    MedicalCaseFlowId = this.MedicalCaseFlowId, // 从NavigationParameters接收
    Timestamp = DateTime.Now
};

_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Publish(payload);

// 订阅事件（MedicalCaseFlowViewModel）
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);

private void OnPatientSelected(PatientSelectedPayload payload)
{
    // 保存患者信息到流程上下文
    SelectedPatientId = payload.PatientId;
    SelectedPatientName = payload.PatientName;
    SelectedPatientInfo = $"{payload.Gender} / {payload.Age}岁 / {payload.PhoneNumber}";

    // 创建医案草稿（持久化）
    CreateMedicalCaseDraft(payload.PatientId);

    // 导航到Step 2
    NavigateToConsultationDetail();
}
```

---

### 2. ConsultationCompletedEvent（诊断填写完成事件）

**触发时机**: Step 2 - 用户填写诊断信息并点击"下一步"

**发布者**: `ConsultationDetailViewModel`（Consultation模块）

**订阅者**: `MedicalCaseFlowViewModel`（MedicalCase模块）

**Payload结构**:
```csharp
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 诊断填写完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedPayload>
    {
    }

    /// <summary>
    /// 诊断完成事件载荷
    /// </summary>
    public class ConsultationCompletedPayload
    {
        /// <summary>
        /// 诊断ID（后端创建后返回）
        /// </summary>
        public Guid ConsultationId { get; set; }

        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 主诉（简要，用于显示）
        /// </summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>
        /// 诊断结果（简要，用于显示）
        /// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 是否保存为草稿（true=草稿，false=正式保存）
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

**使用示例**:
```csharp
// 发布事件（ConsultationDetailViewModel）
var payload = new ConsultationCompletedPayload
{
    ConsultationId = consultationId,
    MedicalCaseFlowId = this.MedicalCaseFlowId,
    ChiefComplaint = this.ChiefComplaint,
    Diagnosis = this.Diagnosis,
    IsDraft = false,
    Timestamp = DateTime.Now
};

_eventAggregator.GetEvent<ConsultationCompletedEvent>()
    .Publish(payload);

// 订阅事件（MedicalCaseFlowViewModel）
_eventAggregator.GetEvent<ConsultationCompletedEvent>()
    .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);

private void OnConsultationCompleted(ConsultationCompletedPayload payload)
{
    // 保存诊断ID到流程上下文
    ConsultationId = payload.ConsultationId;

    // 更新医案草稿状态
    UpdateMedicalCaseDraft(payload.ConsultationId);

    // 导航到Step 3
    NavigateToPrescriptionEditor();
}
```

---

### 3. PrescriptionCompletedEvent（处方填写完成事件）

**触发时机**: Step 3 - 用户填写处方信息并点击"下一步"

**发布者**: `PrescriptionEditorViewModel`（Prescriptions模块）

**订阅者**: `MedicalCaseFlowViewModel`（MedicalCase模块）

**Payload结构**:
```csharp
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 处方填写完成事件
    /// </summary>
    public class PrescriptionCompletedEvent : PubSubEvent<PrescriptionCompletedPayload>
    {
    }

    /// <summary>
    /// 处方完成事件载荷
    /// </summary>
    public class PrescriptionCompletedPayload
    {
        /// <summary>
        /// 处方ID（后端创建后返回）
        /// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 处方药品总数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 处方总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 是否保存为草稿
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

**使用示例**:
```csharp
// 发布事件（PrescriptionEditorViewModel）
var payload = new PrescriptionCompletedPayload
{
    PrescriptionId = prescriptionId,
    MedicalCaseFlowId = this.MedicalCaseFlowId,
    TotalItems = prescriptionItems.Count,
    TotalAmount = prescriptionItems.Sum(x => x.Amount),
    IsDraft = false,
    Timestamp = DateTime.Now
};

_eventAggregator.GetEvent<PrescriptionCompletedEvent>()
    .Publish(payload);

// 订阅事件（MedicalCaseFlowViewModel）
_eventAggregator.GetEvent<PrescriptionCompletedEvent>()
    .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);

private void OnPrescriptionCompleted(PrescriptionCompletedPayload payload)
{
    // 保存处方ID到流程上下文
    PrescriptionId = payload.PrescriptionId;

    // 更新医案草稿状态
    UpdateMedicalCaseDraft(payload.PrescriptionId);

    // 导航到Step 4（完成页）
    NavigateToCompletion();
}
```

---

### 4. MedicalCaseFlowCancelledEvent（流程取消事件）

**触发时机**: 用户在任意步骤点击"取消"按钮

**发布者**: `MedicalCaseFlowViewModel`（MedicalCase模块）

**订阅者**: 各步骤ViewModel（用于清理本地状态）

**Payload结构**:
```csharp
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 医案流程取消事件
    /// </summary>
    public class MedicalCaseFlowCancelledEvent : PubSubEvent<MedicalCaseFlowCancelledPayload>
    {
    }

    /// <summary>
    /// 流程取消事件载荷
    /// </summary>
    public class MedicalCaseFlowCancelledPayload
    {
        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        public string CancelReason { get; set; } = string.Empty;

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

---

### 5. DraftSavedEvent（草稿保存事件）

**触发时机**: 用户在任意步骤点击"保存草稿"按钮

**发布者**: 各步骤ViewModel

**订阅者**: `MedicalCaseFlowViewModel`（用于更新流程状态）

**Payload结构**:
```csharp
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 草稿保存事件
    /// </summary>
    public class DraftSavedEvent : PubSubEvent<DraftSavedPayload>
    {
    }

    /// <summary>
    /// 草稿保存事件载荷
    /// </summary>
    public class DraftSavedPayload
    {
        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 当前步骤（1-4）
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 草稿数据快照（JSON序列化）
        /// </summary>
        public string DraftDataSnapshot { get; set; } = string.Empty;

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

---

## 🔄 事件流转图

```
┌──────────────────────────────────────────────────────────────┐
│                  MedicalCaseFlowViewModel                     │
│                    (流程协调器/订阅者)                         │
└────────┬───────────────┬──────────────┬─────────────┬────────┘
         │               │              │             │
         │ Subscribe     │ Subscribe    │ Subscribe   │ Publish
         │               │              │             │
         ▼               ▼              ▼             ▼
   PatientSelected  Consultation  Prescription  FlowCancelled
      Event         Completed     Completed       Event
                     Event         Event
         ▲               ▲              ▲             │
         │ Publish       │ Publish      │ Publish     │ Subscribe
         │               │              │             │
┌────────┴──────┐  ┌────┴─────┐  ┌─────┴────┐  ┌────┴──────┐
│ Patient       │  │Consultation│ │Prescription│ │各步骤VM   │
│ SelectionVM   │  │ DetailVM   │ │ EditorVM   │ │(清理状态) │
│(Patients模块) │  │(Consultation│ │(Prescriptions│└───────────┘
└───────────────┘  │  模块)     │ │  模块)     │
                   └────────────┘ └────────────┘
```

---

## ⚠️ 注意事项

### 1. 线程安全
所有事件订阅必须指定`ThreadOption.UIThread`，确保UI更新在主线程执行：

```csharp
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);
```

### 2. 事件订阅生命周期
ViewModel销毁时必须取消订阅，避免内存泄漏：

```csharp
public override void Destroy()
{
    _eventAggregator.GetEvent<PatientSelectedEvent>().Unsubscribe(OnPatientSelected);
    base.Destroy();
}
```

### 3. 事件顺序保证
Prism EventAggregator默认不保证事件顺序，如需顺序保证，使用`async/await`：

```csharp
private async void OnPatientSelected(PatientSelectedPayload payload)
{
    await CreateMedicalCaseDraftAsync(payload.PatientId);
    NavigateToConsultationDetail();
}
```

---

## 📚 参考文档

- **Prism EventAggregator官方文档**: https://prismlibrary.com/docs/event-aggregator.html
- **Client端架构指南**: `docs/architecture/client/README.md`
- **Issue #1557**: 看诊流程模块化迁移

---

**文档状态**: ✅ 设计完成
**下一步**: 设计NavigationParameters规范
