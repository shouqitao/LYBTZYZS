# MedicalWorkstation 工作台职责拆分方案

## 1. 当前问题
- MedicalWorkstationMainView 混合了诊疗流程和管理功能
- 导航菜单过于复杂，职责不清晰
- 诊疗流程与CRUD管理操作耦合

## 2. 正确的诊疗流程
```
患者选择 → 诊断 → 处方（医生判断是否必要）
```

## 3. 拆分方案

### 3.1 诊疗流程区（Workflow区）
**位置**: 主工作区顶部或独立Tab
**功能**:
- 快速开始诊疗按钮
- 当前诊疗状态显示
- 诊疗步骤导航：
  1. 选择患者
  2. 录入诊断
  3. 开立处方（可选）

### 3.2 管理功能区（Management区）
**位置**: 左侧导航菜单
**功能**:
- 患者档案管理（CRUD）
- 历史诊疗记录查询
- 医疗案例管理
- 方剂模板管理
- 处方查询与统计

### 3.3 快速操作区
**位置**: 顶部工具栏
**功能**:
- 新增患者（快速入口）
- 开始诊疗（主要入口）
- 今日待诊列表

## 4. 实施步骤

### Step 1: 创建诊疗流程组件
```csharp
// MedicalWorkflowControl.xaml
- 流程步骤指示器
- 当前步骤内容区
- 步骤切换逻辑
```

### Step 2: 重构导航菜单
```csharp
// MedicalWorkstationMainView.xaml
- 移除诊疗流程相关按钮
- 保留管理功能入口
- 简化菜单结构
```

### Step 3: 添加工作区切换
```csharp
// 使用TabControl或Region切换
- Tab1: 诊疗流程
- Tab2: 数据管理
```

## 5. 文件结构调整
```
MedicalWorkstation/
├── Views/
│   ├── MedicalWorkstationMainView.xaml        # 主容器
│   ├── Workflow/
│   │   ├── MedicalWorkflowView.xaml        # 诊疗流程视图
│   │   ├── PatientSelectionView.xaml       # 患者选择
│   │   ├── DiagnosisEntryView.xaml         # 诊断录入
│   │   └── PrescriptionView.xaml           # 处方开立
│   └── Management/
│       ├── PatientManagementView.xaml      # 患者管理
│       ├── CaseHistoryView.xaml            # 病历查询
│       └── FormulaManagementView.xaml      # 方剂管理
├── ViewModels/
│   ├── MedicalWorkstationMainViewModel.cs    # 主视图模型
│   ├── Workflow/
│   │   └── MedicalWorkflowViewModel.cs     # 诊疗流程控制
│   └── Management/
│       └── [各管理模块ViewModel]
```

## 6. 导航流程优化

### 诊疗流程导航
```csharp
public class MedicalWorkflowViewModel
{
    private WorkflowStep _currentStep;

    public enum WorkflowStep
    {
        PatientSelection,  // 选择患者
        Diagnosis,         // 诊断
        Prescription       // 处方（可选）
    }

    public void NextStep() { }
    public void PreviousStep() { }
    public void CompleteWorkflow() { }
}
```

### 管理功能导航
- 使用标准MVVM导航
- 每个管理模块独立
- 通过RegionManager切换

## 7. 验收标准
- [ ] 诊疗流程清晰，步骤明确
- [ ] 管理功能独立，不影响诊疗
- [ ] 导航简洁，职责单一
- [ ] 编译通过，无错误
- [ ] 用户体验流畅