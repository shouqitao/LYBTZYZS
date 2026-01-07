# Tasks: 删除Panel ViewModel - 简化医案数据模型

**Change ID**: consolidate-panel-viewmodels
**总任务数**: 15个
**预计工期**: 2天
**版本**: v1.2
**前置条件**: refactor-medicalcase-workspace Phase 5 (Panel控件删除)

---

## Phase 0: Item位置规范化 (0.25天) ✅ 完成

### 0.1 HerbItemDto迁移

- [x] **TASK-000**: 移动HerbItemDto到规范位置 ✅
  - 当前: `Herbs/Models/HerbItemDto.cs`
  - 目标: `Herbs/Models/Items/HerbItemDto.cs`
  - 创建目录: `Herbs/Models/Items/`
  - 更新命名空间: `LYBT.Desktop.Herbs.Models` → `LYBT.Desktop.Herbs.Models.Items`
  - 更新所有引用

### 0.2 TcmDiagnosis命名统一 (执行中新增)

- [x] **TASK-000.5**: 统一TcmDiagnosis属性名 ✅
  - 确保ConsultationItem属性名为TcmDiagnosis（非TCMDiagnosis）
  - 更新XAML绑定使用统一名称

---

## Phase 1: 删除ConsultationPanelViewModel (0.5天) ✅ 完成

### 1.1 复用现有ConsultationItem

- [x] **TASK-001**: WorkspaceViewModel持有ConsultationItem ✅
  - 添加: `public ConsultationItem Consultation { get; } = new();`
  - 位置: `MedicalCaseWorkspaceViewModel.cs`
  - **变更**: ConsultationItem迁移到MedicalCase模块（解决循环依赖）
  - 新位置: `MedicalCase/Models/Items/ConsultationItem.cs`

- [x] **TASK-002**: 更新XAML绑定路径 ✅
  - 文件: `MedicalCaseEditControl.xaml`
  - 绑定: `Consultation.PresentIllness`, `Consultation.TcmDiagnosis`等
  - ConsultationStatus派生属性基于Consultation.IsDiagnosisComplete

- [x] **TASK-003**: 删除ConsultationPanelViewModel ✅
  - 删除: `ViewModels/ConsultationPanelViewModel.cs`
  - 更新: `MedicalCaseModule.cs`移除DI注册
  - 添加: ConsultationDataProviderAdapter, ConsultationValidatorAdapter适配器类

- [x] **TASK-004**: Phase 1编译验证 ✅
  - LYBT.All.sln编译通过: 0错误
  - MedicalCase模块、Clinical角色模块、Shell均编译成功

---

## Phase 2: 创建PrescriptionItem (0.5天) ✅ 完成

### 2.1 新建Item类

- [x] **TASK-005**: 创建PrescriptionItem.cs ✅
  - 位置: `MedicalCase/Models/Items/PrescriptionItem.cs` (聚合根模块)
  - 属性: Id, MedicalCaseId, DosageCount, Usage, Advice, ReferencedFormulas, Remark, Items
  - 派生属性: ItemCount, SingleDosePrice, TotalPrice, HasItems, IsValid
  - 方法: FromDto(), ToDto(), ToInputDto(), Clear()
  - 实现: BindableBase (INotifyPropertyChanged)
  - **附加**: HerbItemDto添加处方DTO转换方法

---

## Phase 3: 删除PrescriptionPanelViewModel (1天) ✅ 完成

### 3.1 属性迁移

- [x] **TASK-006**: WorkspaceViewModel持有PrescriptionItem ✅
  - 添加: `public PrescriptionItem Prescription { get; } = new();`
  - 添加: `public ObservableCollection<HerbListDto> AllHerbs`
  - 位置: `Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`
  - **附加**: 创建PrescriptionDataProviderAdapter和PrescriptionValidatorAdapter适配器

- [x] **TASK-007**: 迁移命令到WorkspaceViewModel ✅
  - OpenFormulaImportDialogCommand
  - OpenHistoryCopyDialogCommand
  - ClearHerbItemsCommand
  - **附加**: 添加PrescriptionImportHandler依赖注入

- [x] **TASK-008**: 更新XAML绑定路径 ✅
  - 文件: `MedicalCaseWorkspaceView.xaml`
  - 操作: 全局替换为`Prescription.*`和直接命令绑定

- [x] **TASK-009**: 删除PrescriptionPanelViewModel ✅
  - 删除: `ViewModels/PrescriptionPanelViewModel.cs`
  - 删除: `ViewModels/MedicalCaseWorkspaceViewModel.cs` (模块版本已迁移到Clinical)
  - 删除: `Controls/PrescriptionEditorPanel.xaml(.cs)` (已被MedicalCaseEditControl替代)
  - 更新: `MedicalCaseModule.cs`移除DI注册

- [x] **TASK-010**: Phase 3编译验证 ✅
  - 编译通过: 0错误
  - 无悬挂引用

---

## Phase 4: 集成验证 (0.25天)

- [x] **TASK-011**: 全量编译验证 ✅
  - LYBT.All.sln编译通过
  - 0错误 (仅有无关警告)

- [ ] **TASK-012**: 功能测试清单
  - 诊断字段编辑正常
  - 处方药材添加/删除正常
  - 验方导入正常
  - 历史处方复制正常
  - 价格计算正常
  - 保存功能正常

---

## 验收检查清单

### Item位置规范化

- [x] HerbItemDto已移至`Herbs/Models/Items/`
- [x] 命名空间已更新
- [x] 所有引用已更新

### 代码删除

- [x] ConsultationPanelViewModel.cs已删除
- [x] PrescriptionPanelViewModel.cs已删除
- [x] PrescriptionEditorPanel.xaml(.cs)已删除
- [x] MedicalCaseWorkspaceViewModel.cs(模块版本)已删除
- [x] DI注册已清理

### 模型创建

- [x] PrescriptionItem.cs已创建
- [x] FromDto/ToDto/ToInputDto/Clear方法完整

### 绑定简化

- [x] XAML绑定路径不再包含`XXXPanelViewModel.`前缀
- [x] 诊断绑定: `Consultation.PresentIllness` 等
- [x] 处方绑定: `Prescription.Items` 等

### 服务层

- [x] 现有服务保持不变 (PrescriptionImportHandler, PrescriptionCalculator等)
- [x] 服务由WorkspaceViewModel直接调用

### 功能验证 (需运行时验证)

- [ ] 诊断编辑正常
- [ ] 处方编辑正常
- [ ] 导入功能正常
- [ ] 价格计算正常

---

## 任务依赖关系

```
Phase 0 (Item位置规范化)
    └── TASK-000 (HerbItemDto迁移)
            │
Phase 1 (ConsultationPanelViewModel删除) ←────┘
    ├── TASK-001 (持有ConsultationItem)
    │   └── TASK-002 (更新XAML)
    │       └── TASK-003 (删除文件)
    └── TASK-004 (编译验证)
            │
Phase 2 (PrescriptionEditItem创建) ←────┘
    └── TASK-005 (创建Item类)
            │
Phase 3 (PrescriptionPanelViewModel删除) ←────┘
    ├── TASK-006 (持有PrescriptionEditItem)
    ├── TASK-007 (迁移命令)
    │   └── TASK-008 (更新XAML)
    │       └── TASK-009 (删除文件)
    └── TASK-010 (编译验证)
            │
Phase 4 (集成验证) ←────┘
    ├── TASK-011 (全量编译)
    └── TASK-012 (功能测试)
```

---

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 绑定迁移遗漏 | 全局搜索确认无遗漏 |
| 功能回归 | 每阶段验证核心功能 |
| ConsultationItem属性名差异 | 检查TCMDiagnosis vs TcmDiagnosis |

---

## Desktop层Item审计结果 (2026-01-05)

| 模块 | Item类 | 状态 | 备注 |
|------|--------|------|------|
| Consultation | ConsultationItem | 已标准化 | 可复用 |
| Patients | PatientItem | 已标准化 | - |
| Formula | FormulaItem | 已标准化 | - |
| Users | UserItem | 已标准化 | - |
| Herbs | HerbItemDto | 已标准化 | - |
| **MedicalCase** | **待创建** | **PrescriptionEditItem** | **本提案** |

**结论**: 仅需为MedicalCase模块创建PrescriptionEditItem，其他模块已完成Item标准化。

---

**创建时间**: 2026-01-05
**版本**: v1.1
**负责人**: Claude Code
