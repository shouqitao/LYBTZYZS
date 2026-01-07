# Tasks: slim-medicalcase-workspace-viewmodel

**Change ID**: slim-medicalcase-workspace-viewmodel
**Total Tasks**: 10
**Completed**: 0/10
**Estimated Effort**: 6h

---

## Phase 1: Create Component Classes (2h)

### Task 1.1: Create WorkspaceStatusDisplay
- **ID**: SVM-001
- **Status**: pending
- **Priority**: P0
- **Effort**: 45min

**Description**: 创建状态显示组件

**File**: `ViewModels/Components/WorkspaceStatusDisplay.cs`

**Implementation**:
```csharp
public partial class WorkspaceStatusDisplay : ObservableObject
{
    [ObservableProperty] private string _consultationStatusText;
    [ObservableProperty] private Brush _consultationStatusColor;
    [ObservableProperty] private string _prescriptionStatusText;
    // ... 其他属性

    public void UpdateConsultationStatus(EditState state, bool hasValidDiagnosis);
    public void UpdatePrescriptionStatus(int itemCount, bool needsPrescription, bool isCompleted);
}
```

**Acceptance Criteria**:
- [ ] 组件类创建完成
- [ ] 使用[ObservableProperty]特性
- [ ] 状态计算逻辑正确

---

### Task 1.2: Create WorkspaceButtonState
- **ID**: SVM-002
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min

**Description**: 创建按钮状态组件

**File**: `ViewModels/Components/WorkspaceButtonState.cs`

**Implementation**:
```csharp
public partial class WorkspaceButtonState : ObservableObject
{
    [ObservableProperty] private bool _showCompleteButton;
    [ObservableProperty] private bool _showDraftButton;
    // ... 其他属性

    public void Update(EditState editState, WorkspaceMode mode, bool isFromManagement, bool hasUnsavedChanges);
    public void UpdateCanComplete(bool hasValidDiagnosis, bool needsPrescription, int herbCount);
}
```

**Acceptance Criteria**:
- [ ] 组件类创建完成
- [ ] 按钮逻辑集中管理
- [ ] 支持所有工作区模式

---

### Task 1.3: Create PendingQueueViewModel
- **ID**: SVM-003
- **Status**: pending
- **Priority**: P0
- **Effort**: 45min

**Description**: 创建待诊队列子ViewModel

**File**: `ViewModels/PendingQueueViewModel.cs`

**Implementation**:
```csharp
public partial class PendingQueueViewModel : ObservableObject
{
    public ObservableCollection<PendingMedicalCaseDto> Queue { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public event EventHandler<PendingMedicalCaseDto>? CaseSelected;
}
```

**Acceptance Criteria**:
- [ ] 独立管理待诊队列
- [ ] 提供事件通知父ViewModel
- [ ] 支持异步刷新

---

## Phase 2: Refactor Root ViewModel (2h)

### Task 2.1: Inject New Components
- **ID**: SVM-004
- **Status**: pending
- **Priority**: P0
- **Effort**: 20min
- **Depends On**: SVM-001, SVM-002, SVM-003

**Description**: 在MedicalCaseWorkspaceViewModel中注入新组件

**Changes**:
```csharp
// 添加构造函数参数
public MedicalCaseWorkspaceViewModel(
    WorkspaceStatusDisplay statusDisplay,
    WorkspaceButtonState buttonState,
    PendingQueueViewModel pendingQueue,
    // ... existing params
)

// 添加公共属性
public WorkspaceStatusDisplay StatusDisplay { get; }
public WorkspaceButtonState ButtonState { get; }
public PendingQueueViewModel PendingQueue { get; }
```

---

### Task 2.2: Migrate Status Logic
- **ID**: SVM-005
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min
- **Depends On**: SVM-004

**Description**: 迁移UpdateConsultationStatus/UpdatePrescriptionStatus到StatusDisplay

**Before**:
```csharp
private void UpdateConsultationStatus()
{
    // 120行逻辑在ViewModel中
}
```

**After**:
```csharp
private void UpdateConsultationStatus()
{
    StatusDisplay.UpdateConsultationStatus(_editModeStateMachine.CurrentState, hasValidDiagnosis);
}
```

---

### Task 2.3: Migrate Button Logic
- **ID**: SVM-006
- **Status**: pending
- **Priority**: P0
- **Effort**: 20min
- **Depends On**: SVM-004

**Description**: 迁移按钮可见性逻辑到ButtonState

---

### Task 2.4: Migrate PendingQueue Logic
- **ID**: SVM-007
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min
- **Depends On**: SVM-004

**Description**: 迁移待诊队列逻辑到PendingQueueViewModel

**Migrate**:
- LoadPendingQueueAsync
- ExecuteSelectPendingCaseAsync
- RefreshPendingQueueCommand
- PendingQueue collection

---

### Task 2.5: Add Delegate Properties
- **ID**: SVM-008
- **Status**: pending
- **Priority**: P1
- **Effort**: 20min
- **Depends On**: SVM-005, SVM-006, SVM-007

**Description**: 添加委托属性保持XAML向后兼容

```csharp
// 委托到组件，保持原有绑定工作
public string ConsultationStatusText => StatusDisplay.ConsultationStatusText;
public bool ShowCompleteButton => ButtonState.ShowCompleteButton;
```

---

## Phase 3: Update DI Registration (0.5h)

### Task 3.1: Register Components
- **ID**: SVM-009
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min
- **Depends On**: SVM-001, SVM-002, SVM-003

**Description**: 在MedicalCaseModule中注册新组件

**File**: `MedicalCaseModule.cs`

```csharp
containerRegistry.Register<WorkspaceStatusDisplay>();
containerRegistry.Register<WorkspaceButtonState>();
containerRegistry.Register<PendingQueueViewModel>();
```

---

## Phase 4: Verification (1.5h)

### Task 4.1: Verify and Test
- **ID**: SVM-010
- **Status**: pending
- **Priority**: P0
- **Effort**: 1.5h
- **Depends On**: SVM-008, SVM-009

**Description**: 验证编译和功能

**Checklist**:
- [ ] `dotnet build LYBT.All.sln` 通过
- [ ] UI功能正常
- [ ] 状态显示正确
- [ ] 按钮可见性正确
- [ ] 待诊队列刷新正常
- [ ] 选择待诊患者正常

**Unit Tests**:
- [ ] WorkspaceStatusDisplayTests.cs
- [ ] WorkspaceButtonStateTests.cs
- [ ] PendingQueueViewModelTests.cs

---

## Phase 5: Handler清理 (4h) - 新增 2025-12-31

### Task 5.1: 删除PrescriptionItemHandler
- **ID**: SVM-011
- **Status**: pending
- **Priority**: P0
- **Effort**: 1h

**Description**: 完全删除PrescriptionItemHandler.cs，所有功能已被HerbListControl接管

**Steps**:
1. 查找所有对`_itemHandler`的引用
2. 删除或替换为直接使用控件方法
3. 删除PrescriptionItemHandler.cs文件
4. 移除DI注册

**Files to Modify**:
- `ViewModels/Components/PrescriptionItemHandler.cs` - 删除
- `ViewModels/PrescriptionPanelViewModel.cs` - 移除引用
- `MedicalCaseModule.cs` - 移除注册

---

### Task 5.2: 简化PrescriptionImportHandler
- **ID**: SVM-012
- **Status**: pending
- **Priority**: P0
- **Effort**: 1.5h
- **Depends On**: SVM-011

**Description**: 简化导入Handler为纯DTO转换器

**Before** (292行):
- `ProcessFormulaImport` - 包含重复检测、集合操作
- `ProcessHistoryCopy` - 包含重复检测、集合操作  
- `AddHerbItemsToCollection` - 添加到ObservableCollection

**After** (~80行):
- `ToHerbItemDtos(FormulaDetailDto)` - 纯DTO转换
- `ToHerbItemDtos(PrescriptionDetailDto)` - 纯DTO转换

```csharp
public class PrescriptionImportHandler
{
    public List<HerbItemDto> ToHerbItemDtos(FormulaDetailDto formula)
    {
        return formula.Herbs.Select(h => new HerbItemDto
        {
            HerbId = h.HerbId,
            HerbName = h.HerbName,
            Dosage = h.Dosage,
            CookingMethod = h.CookingMethod
        }).ToList();
    }
}
```

---

### Task 5.3: 实现ViewModel到控件的导入桥接
- **ID**: SVM-013
- **Status**: pending
- **Priority**: P0
- **Effort**: 1h
- **Depends On**: SVM-012

**Description**: 实现ViewModel请求View调用控件AddHerbs的机制

**Implementation**:
```csharp
// PrescriptionPanelViewModel.cs
public event EventHandler<ImportHerbsRequestEventArgs>? ImportHerbsRequested;

public void RequestImportHerbs(List<HerbItemDto> herbs)
{
    ImportHerbsRequested?.Invoke(this, new ImportHerbsRequestEventArgs(herbs));
}

// PrescriptionEditorPanel.xaml.cs
private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    // 订阅ViewModel的ImportHerbsRequested事件
}

private void OnViewModelImportHerbsRequested(object? sender, ImportHerbsRequestEventArgs e)
{
    HerbListCtrl.AddHerbs(e.Herbs);
}
```

---

### Task 5.4: 更新导入流程调用方
- **ID**: SVM-014
- **Status**: pending
- **Priority**: P0
- **Effort**: 0.5h
- **Depends On**: SVM-013

**Description**: 更新经验方和历史处方导入的调用代码

**Before**:
```csharp
var importResult = _importHandler.ProcessFormulaImport(formula, HerbItems);
_importHandler.AddHerbItemsToCollection(HerbItems, importResult.ItemsToAdd, ...);
```

**After**:
```csharp
var herbs = _importHandler.ToHerbItemDtos(formula);
RequestImportHerbs(herbs);
// 控件自动处理重复检测、剂量合并、价格同步
```

---

## Progress Summary

| Phase | Tasks | Status | Est. Effort |
|-------|-------|--------|-------------|
| 1. Create Components | SVM-001~003 | **completed** | 2h |
| 2. Refactor ViewModel | SVM-004~008 | pending | 2h |
| 3. Update DI | SVM-009 | pending | 0.5h |
| 4. Verification | SVM-010 | pending | 1.5h |
| 5. Handler清理 | SVM-011~014 | pending | 4h |

---

## Line Count Target

| Milestone | Target Lines |
|-----------|--------------|
| Current | 1183 |
| After Phase 1 | 1183 (no change yet) |
| After Phase 2 | ~900 |
| After Phase 5 | ~700 (删除Handler后) |
| Final | < 750 |

---

## Handler代码减少统计

| 文件 | 变更前 | 变更后 | 减少 |
|------|--------|--------|------|
| PrescriptionItemHandler.cs | 307行 | **删除** | -307行 |
| PrescriptionImportHandler.cs | 292行 | ~80行 | -212行 |
| **总计** | **599行** | **~80行** | **-519行 (87%)** |
