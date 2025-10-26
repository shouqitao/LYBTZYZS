# 看诊流程模块迁移架构讨论

**创建时间**: 2025-10-21
**讨论范围**: 四步看诊流程的View/ViewModel模块归属设计
**决策目标**: 确定最佳的模块边界与职责划分

---

## 📋 背景说明

### 当前状态
- ✅ 所有四步看诊流程的View都集中在 MedicalCase 模块中
- 🔄 计划将View和ViewModel迁移到对应的功能模块

### 四步看诊流程（已确认）
✅ **已确认：四步看诊流程的具体步骤**
- ✅ 步骤1: **患者选择** (PatientSelectionView/ViewModel)
- ✅ 步骤2: **填写诊断** (MedicalCaseDetailView/ViewModel)
- ✅ 步骤3: **填写处方** (PrescriptionEditorView/ViewModel)
- ✅ 步骤4: **完成医案** (CompletionView/ViewModel)

**当前位置**：所有View/ViewModel都在 `LYBT.Desktop.MedicalCase` 模块

**可能的目标模块**：
- Step 1 → `LYBT.Desktop.Patients`
- Step 2 → `LYBT.Desktop.Consultation`
- Step 3 → `LYBT.Desktop.Prescriptions`
- Step 4 → 保留在 `LYBT.Desktop.MedicalCase`

**流程协调器**：`MedicalCaseFlowViewModel` 负责管理四步流程的状态机和步骤切换

---

## 🏗️ 架构分析（DDD + 模块化视角）

### 方案对比

#### 方案A: 按模块职责迁移（用户建议）⭐ 推荐
**设计思路**: 将View/ViewModel迁移到各自的业务模块

**优点**:
- ✅ **单一职责原则（SRP）** - 每个模块只管理自己的领域逻辑
- ✅ **高内聚低耦合** - 患者、诊断、处方相关的UI和逻辑内聚在各自模块
- ✅ **独立复用** - 患者选择、处方编辑器可以在其他场景复用（如历史处方查看、患者档案管理）
- ✅ **团队协作** - 不同团队成员可以并行开发各自的模块，减少冲突
- ✅ **DDD聚合根对齐** - UI组件与领域模型对齐（Patient、Consultation、Prescription是独立聚合根）
- ✅ **测试隔离** - 各模块的UI逻辑可以独立测试，不依赖流程上下文

**缺点**:
- ⚠️ **跨模块依赖** - MedicalCase模块需要依赖Patients、Consultation、Prescriptions模块（但符合依赖倒置原则）
- ⚠️ **流程数据传递** - 需要设计良好的事件聚合或参数传递机制（可通过Prism EventAggregator解决）
- ⚠️ **初期迁移成本** - 需要移动代码、调整命名空间、更新Region注册

**影响范围**:
- 📁 **Patients模块** - 新增 `Views/PatientSelectionView.xaml` 和 `ViewModels/PatientSelectionViewModel.cs`
- 📁 **Consultation模块** - 新增诊断相关View/ViewModel
- 📁 **Prescriptions模块** - 新增处方编辑器View/ViewModel
- 📁 **MedicalCase模块** - 保留 `MedicalCaseFlowViewModel`（流程协调器）和 `CompletionView`（流程专属）
- 🔗 **依赖关系** - MedicalCase → Patients/Consultation/Prescriptions（通过接口或事件）

#### 方案B: 保持集中管理（当前状态）
**设计思路**: 所有看诊流程View保持在MedicalCase模块

**优点**:
- ✅ **流程完整性** - 所有流程相关的UI在一个模块中，易于理解整体逻辑
- ✅ **简单直接** - 无需跨模块调用，代码路径清晰
- ✅ **快速迭代** - MVP阶段可以快速修改流程，无需多模块协调

**缺点**:
- ❌ **违反单一职责** - MedicalCase模块承担了患者、诊断、处方的UI职责
- ❌ **低复用性** - 患者选择器、处方编辑器无法在其他场景复用
- ❌ **模块膨胀** - MedicalCase模块过大，包含了多个领域的关注点
- ❌ **DDD边界模糊** - UI组件与领域模型不对齐，违反聚合根独立性
- ❌ **团队协作冲突** - 多人修改同一模块，容易产生Git冲突
- ❌ **测试耦合** - 测试患者选择器时需要加载整个MedicalCase上下文

---

## 💡 架构师建议（已分析）

### 1. DDD聚合根边界分析 ⭐ 核心论点

**关键问题**：看诊流程是一个"聚合根"还是多个"聚合根"的协作？

**答案**：看诊流程是一个**流程协调器（Process Coordinator）**，而非聚合根。

**分析**：
- 📦 **患者（Patient）** - 独立聚合根，包含患者信息、病历、联系方式
- 📦 **诊断（Consultation）** - 独立聚合根，包含主诉、现病史、诊断结果
- 📦 **处方（Prescription）** - 独立聚合根，包含药品列表、用法用量、价格
- 🔄 **医案流程（MedicalCaseFlow）** - 流程协调器，编排上述聚合根的协作

**DDD原则**：
> "聚合根应该具有事务一致性边界，不同聚合根之间通过领域事件或命令进行通信。"

**结论**：
- ✅ **UI组件应该与聚合根对齐** - 患者选择器属于Patients模块，处方编辑器属于Prescriptions模块
- ✅ **流程协调器保持独立** - MedicalCaseFlowViewModel作为流程编排器，依赖各模块的UI组件

### 2. 模块依赖方向分析 ⭐ 依赖倒置

**依赖方向规则**（从Constitution第1.1条）：
```
UI → Service → Repository → DB
```

**迁移后的依赖关系**（符合依赖倒置原则）：
```
MedicalCase模块（流程编排）
    ↓ 依赖（通过Region或EventAggregator）
Patients模块 / Consultation模块 / Prescriptions模块（领域UI）
    ↓ 依赖
Shared.Contracts（DTO/接口）
```

**避免循环依赖**：
- ❌ **错误** - Patients模块依赖MedicalCase模块（会形成循环）
- ✅ **正确** - MedicalCase模块依赖Patients模块（单向依赖）
- ✅ **更好** - MedicalCase通过Prism Region或EventAggregator间接依赖（解耦）

**Prism模块化最佳实践**：
```csharp
// MedicalCaseFlowViewModel.cs（流程协调器）
public class MedicalCaseFlowViewModel : ViewModelBase
{
    private readonly IRegionManager _regionManager;

    private void NavigateToStep(int step)
    {
        // 通过Region导航，无需直接依赖具体模块
        switch (step)
        {
            case 1:
                _regionManager.RequestNavigate("WorkflowRegion", "PatientSelectionView");
                break;
            case 2:
                _regionManager.RequestNavigate("WorkflowRegion", "ConsultationDetailView");
                break;
            case 3:
                _regionManager.RequestNavigate("WorkflowRegion", "PrescriptionEditorView");
                break;
        }
    }
}
```

### 3. MVVM模式合规性分析

**Phase 2架构要求**（从Client README第1.2节）：
- ✅ ViewModel → 直接使用Repository（无中间Service层）
- ✅ 每个模块独立管理自己的Repository

**迁移后的结构**（符合Phase 2架构）：
```
LYBT.Desktop.Patients/
├── Views/
│   └── PatientSelectionView.xaml
├── ViewModels/
│   └── PatientSelectionViewModel.cs  ← 直接注入IPatientRepository
└── Repositories/
    └── PatientRepository.cs

LYBT.Desktop.Prescriptions/
├── Views/
│   └── PrescriptionEditorView.xaml
├── ViewModels/
│   └── PrescriptionEditorViewModel.cs  ← 直接注入IPrescriptionRepository
└── Repositories/
    └── PrescriptionRepository.cs
```

**复用性示例**（架构优势）：
```csharp
// 场景1：看诊流程中使用患者选择器
MedicalCaseFlowView → Region → PatientSelectionView

// 场景2：患者档案管理中使用患者选择器
PatientManagementView → 直接嵌入 → PatientSelectionView

// 场景3：历史处方查看中使用处方编辑器（只读模式）
PrescriptionHistoryView → Region → PrescriptionEditorView (IsReadOnly=true)
```

---

## ✅ 架构师最终建议

### 🎯 推荐方案：**方案A - 按模块职责迁移**

**理由总结**：
1. ⭐ **DDD对齐** - UI组件与聚合根边界一致，符合领域驱动设计
2. ⭐ **高复用性** - 患者选择器、处方编辑器可在多场景复用
3. ⭐ **单一职责** - 每个模块只关注自己的领域关注点
4. ⭐ **长期收益** - 降低模块耦合，提升可维护性和可测试性
5. ✅ **符合Constitution** - 遵循三层对齐架构原则

**关键设计模式**：
- **流程协调器模式** - MedicalCaseFlowViewModel作为流程状态机
- **Prism Region导航** - 通过Region实现松耦合的模块间导航
- **事件聚合器** - 通过EventAggregator传递流程数据（患者ID、医案ID等）
- **ViewModel参数传递** - 通过NavigationParameters传递上下文信息

### ⚠️ 需要注意的风险点

1. **数据流设计** - 确保各步骤之间的数据传递机制清晰
   - 建议：使用 `NavigationParameters` + `EventAggregator` 结合
   - 避免：在ViewModel之间直接引用

2. **依赖方向控制** - 防止循环依赖
   - ✅ MedicalCase → Patients（通过Region导航）
   - ❌ Patients → MedicalCase（避免反向依赖）

3. **事务一致性** - 流程中断后的数据一致性保障
   - 建议：Step 1完成后立即创建医案草稿
   - 使用："保存草稿"功能保存各步骤的中间状态

### 📋 实施计划（分阶段）

#### Phase 1: 准备工作（1-2天）
- [ ] 创建GitHub Issue跟踪此重构任务
- [ ] 审查当前MedicalCaseFlowViewModel的依赖关系
- [ ] 设计事件聚合器的消息契约（PatientSelectedEvent、ConsultationCompletedEvent等）
- [ ] 设计NavigationParameters的参数规范

#### Phase 2: 迁移患者选择（Step 1）（2-3天）
- [ ] 在Patients模块创建 `PatientSelectionView.xaml`
- [ ] 迁移 `PatientSelectionViewModel.cs` 到Patients模块
- [ ] 在PatientsModule.cs中注册Region View
- [ ] 更新MedicalCaseFlowViewModel使用Region导航
- [ ] 测试患者选择步骤的独立功能

#### Phase 3: 迁移诊断填写（Step 2）（2-3天）
- [ ] 在Consultation模块创建 `ConsultationDetailView.xaml`（如果不存在）
- [ ] 迁移诊断相关ViewModel到Consultation模块
- [ ] 实现诊断数据的保存和恢复逻辑
- [ ] 测试诊断步骤的独立功能

#### Phase 4: 迁移处方编辑（Step 3）（2-3天）
- [ ] 在Prescriptions模块创建 `PrescriptionEditorView.xaml`
- [ ] 迁移 `PrescriptionEditorViewModel.cs` 到Prescriptions模块
- [ ] 实现处方数据的保存和恢复逻辑
- [ ] 测试处方编辑器的独立功能和只读模式

#### Phase 5: 完善流程协调器（1-2天）
- [ ] 优化MedicalCaseFlowViewModel的状态管理
- [ ] 实现流程中断和恢复机制
- [ ] 添加流程步骤验证逻辑
- [ ] 完善"保存草稿"和"取消流程"功能

#### Phase 6: 测试和文档（2-3天）
- [ ] 端到端测试完整看诊流程
- [ ] 测试各步骤的复用场景（患者档案、历史处方等）
- [ ] 更新架构文档（Client README、模块README）
- [ ] 创建迁移指南和最佳实践文档

### 🔧 技术实施细节

#### 1. Region导航示例
```csharp
// MedicalCaseFlowViewModel.cs
private void NavigateToPatientSelection()
{
    var parameters = new NavigationParameters
    {
        { "MedicalCaseId", MedicalCaseId },
        { "FlowContext", "NewMedicalCase" }
    };

    _regionManager.RequestNavigate("WorkflowContentRegion", "PatientSelectionView", parameters);
}
```

#### 2. 事件聚合器示例
```csharp
// 定义事件
public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload>
{
    public class PatientSelectedPayload
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public Guid MedicalCaseId { get; set; }
    }
}

// PatientSelectionViewModel发布事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Publish(new PatientSelectedPayload { PatientId = selectedPatient.Id, ... });

// MedicalCaseFlowViewModel订阅事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);
```

#### 3. 模块注册示例
```csharp
// PatientsModule.cs
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<PatientSelectionView, PatientSelectionViewModel>();
        containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
    }
}
```

---

## ✅ 决策记录

### 最终方案
⭐ **采用方案A：按模块职责迁移**

**决策依据**：
- 符合DDD聚合根边界原则
- 提升代码复用性和可维护性
- 遵循项目Constitution的三层对齐架构要求
- 长期收益大于短期迁移成本

### 后续行动
✅ **Q2已确认：采用方案A - 按模块职责迁移**

**确认时间**: 2025-10-21
**确认人**: 用户
**实施状态**: 🚀 已启动

---

**文档状态**: ✅ 方案已确认
**下一步**: 创建GitHub Issue并启动Phase 1准备工作
