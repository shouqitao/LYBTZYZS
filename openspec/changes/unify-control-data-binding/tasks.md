# 任务分解: unify-control-data-binding

## Phase A: 基础设施 (2天)

### A.1 创建通用State类

**位置**: `Infrastructure/Models/State/`

- [ ] `PaginationState.cs` - 分页状态（复用6个控件的分页属性）
- [ ] `LoadingState.cs` - 加载状态（复用10+控件的IsBusy/Message）
- [ ] `SearchState.cs` - 搜索状态（Keyword, IsSearching）
- [ ] `SelectionState.cs` - 选择状态（SelectedItem, SelectedItems）

### A.2 创建通用Options类

**位置**: `Infrastructure/Models/Options/`

- [ ] `DisplayOptions.cs` - 显示选项（IsCompactMode, ShowHeader等）
- [ ] `PaginationOptions.cs` - 分页选项（ShowPageSize, PageSizeOptions）
- [ ] `ToolbarOptions.cs` - 工具栏选项（ShowSearch, ShowFilter）

### A.3 更新文档

- [ ] 更新 `Infrastructure/CLAUDE.md` - 添加对象化绑定规范
- [ ] 更新 `MedicalCase/CLAUDE.md` - 添加EditModel使用说明

---

## Phase B: 高优先级控件 (5天)

### B.1 MedicalCaseEditControl (26属性 → 6属性)

**新建模型**:
- [ ] `MedicalCase/Models/Edit/ConsultationEditModel.cs`
  - PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
  - IsValid, Reset()
- [ ] `MedicalCase/Models/Edit/PrescriptionEditModel.cs`
  - HerbItems, DoseCount, Usage, FormulaSource, TotalPrice
  - HerbCount, IsValid, Clear()
- [ ] `MedicalCase/Models/Commands/MedicalCaseCommands.cs`
  - ImportFormulaCommand, ImportHistoryCommand, ClearAllCommand

**重构控件**:
- [ ] 更新 `MedicalCaseEditControl.xaml.cs` - 替换为对象属性
- [ ] 更新 `MedicalCaseEditControl.xaml` - 更新绑定路径
- [ ] 更新 `MedicalCaseMasterDetailViewModel` - 使用新模型

### B.2 PatientViewControl (23属性 → 2属性)

**新建模型**:
- [ ] `Patients/Models/Display/PatientDetailDisplayModel.cs`
  - 基本信息: Name, PinYinCode, Gender, BirthDate, Age, IdNumber, IdType, MaritalStatus, BloodType
  - 联系信息: PhoneNumber, Address
  - 紧急联系人: EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation
  - 病史信息: AllergyHistory, MedicalHistory
  - 就诊信息: LastVisitTime, VisitCount
  - 系统信息: Status, DisableReason, CreatedAt, UpdatedAt
  - 计算属性: AgeDisplay, GenderDisplay, StatusDisplay

**重构控件**:
- [ ] 更新 `PatientViewControl.xaml.cs` - 单一Patient属性 + ShowStatus选项
- [ ] 更新 `PatientViewControl.xaml` - 更新绑定路径
- [ ] 创建 `PatientDetailDisplayModelMapper` - 从DTO映射

### B.3 BaseMasterDataListView (19属性 → 5属性)

**使用通用State**:
- 使用 `PaginationState` 替代分页属性(6个)
- 使用 `LoadingState` 替代IsBusy/BusyMessage(2个)
- 使用 `SearchState` 替代SearchText/IsSearching(2个)

**新建模型**:
- [ ] `Infrastructure/Models/Commands/ListViewCommands.cs`
  - SearchCommand, FirstPageCommand, PreviousPageCommand, NextPageCommand, LastPageCommand

**重构控件**:
- [ ] 更新 `BaseMasterDataListView.xaml.cs`
  - ItemsSource, SelectedItem, SelectedItems (保留)
  - Pagination: PaginationState
  - Loading: LoadingState
  - Commands: ListViewCommands
  - Slots: FilterContent, ActionButtons (保留)
- [ ] 更新 `BaseMasterDataListView.xaml`

### B.4 MedicalCaseViewControl (17属性 → 3属性)

**新建模型**:
- [ ] `MedicalCase/Models/Display/MedicalCaseDisplayModel.cs`
  - 患者信息: PatientName, ConsultationDate, DoctorName
  - 诊断信息: PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
  - 处方信息: HerbCount, DoseCount, FormulaSource, PrescriptionItems
  - 系统信息: Status, CreatedAt, UpdatedAt, Remark
  - 计算属性: StatusDisplay, HasPrescriptionItems

**重构控件**:
- [ ] 更新 `MedicalCaseViewControl.xaml.cs`
  - MedicalCase: MedicalCaseDisplayModel
  - Patient: PatientDisplayModel (可选，用于头部卡片)
  - Options: DisplayOptions
- [ ] 更新 `MedicalCaseViewControl.xaml`

### B.5 HerbViewControl + HerbEditControl (31属性 → 6属性)

**新建模型**:
- [ ] `Herbs/Models/Display/HerbDisplayModel.cs`
  - 基本信息: Name, PinYinCode, CategoryName
  - 规格信息: Unit, Price, DefaultDosage
  - 煎法信息: CookingMethods
  - 属性信息: Nature, Flavor, Meridians, Functions, Indications
  - 系统信息: Status, CreatedAt, UpdatedAt
- [ ] `Herbs/Models/Edit/HerbEditModel.cs`
  - 同上属性，使用[ObservableProperty]

**重构控件**:
- [ ] 更新 `HerbViewControl.xaml.cs` - Herb: HerbDisplayModel
- [ ] 更新 `HerbEditControl.xaml.cs` - Herb: HerbEditModel
- [ ] 更新对应XAML文件

---

## Phase C: 中优先级控件 (4天)

### C.1 BaseDetailContainer (15属性)

- [ ] 分析属性分类
- [ ] 创建 DetailContainerOptions
- [ ] 重构控件

### C.2 PatientSearchControl (15属性)

- [ ] 使用 PaginationState
- [ ] 使用 SearchState
- [ ] 创建 PatientSearchCommands
- [ ] 重构控件

### C.3 PatientEditControl (13属性)

- [ ] 创建 PatientEditModel
- [ ] 重构控件

### C.4 FormulaEditControl (12属性)

- [ ] 创建 FormulaEditModel
- [ ] 重构控件

### C.5 PendingQueueControl (12属性)

- [ ] 创建 QueueState
- [ ] 创建 QueueOptions
- [ ] 重构控件

### C.6 UserEditControl + UserViewControl (23属性)

- [ ] 创建 UserDisplayModel
- [ ] 创建 UserEditModel
- [ ] 重构控件

---

## Phase D: 低优先级控件 (2天)

### 通用基础控件

- [ ] SidebarControl (9属性)
- [ ] UnifiedPaginationBar (9属性)
- [ ] DetailToolbar (7属性)
- [ ] UnifiedManagementTable (7属性)
- [ ] StatusBadge (6属性)
- [ ] MasterDetailLayout (6属性)
- [ ] HerbListControl (5属性)
- [ ] DataGridToolbar (5属性)
- [ ] EmptyState (5属性)
- [ ] UnifiedManagementToolBar (4属性)
- [ ] GlobalStatusBar (4属性)
- [ ] HerbItemControl (3属性)
- [ ] SearchBox (3属性)
- [ ] LoadingOverlay (2属性)
- [ ] InfoCard (2属性)
- [ ] FormulaViewControl (2属性)

---

## 验收检查清单

### 编译验证
- [ ] 全量编译通过 (0错误)
- [ ] 无新增警告

### 功能验证
- [ ] MedicalCase创建/编辑功能正常
- [ ] Patient创建/编辑/查看功能正常
- [ ] Herb管理功能正常
- [ ] Formula管理功能正常
- [ ] User管理功能正常
- [ ] 分页功能正常
- [ ] 搜索功能正常

### 代码质量
- [ ] 删除未使用的旧属性定义
- [ ] 代码注释完整
- [ ] 命名规范统一
