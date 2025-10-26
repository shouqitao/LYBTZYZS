# Epic #1494医案流程UI重构 - 进度梳理报告

**生成日期**：2025-10-20
**Epic**：#1494 医案流程UI重构（4步流程）
**分析方法**：代码扫描 + 注释分析 + 实际测试
**当前阶段**：阶段1 - UI/UX交互框架验证

---

## 📊 执行摘要

### 核心目标
实现4步医案流程UI框架：
```
Step 1: 患者选择 → Step 2: 填写诊断 → Step 3: 填写处方 → Step 4: 完成医案
```

### 整体进度

| 阶段 | 状态 | 完成度 | 说明 |
|------|------|--------|------|
| **阶段1：UI交互框架** | 🚧 进行中 | 80% | 4步导航基本可用，需验证完整性 |
| **阶段2：数据验证和保存** | ⏳ 待开始 | 0% | 已创建技术债务跟踪 |
| **阶段3：UI优化** | ⏳ 待开始 | 0% | 后续实施 |

---

## 1️⃣ Epic #1494任务清单

### Task #1496：医案流程主视图 ✅

**状态**：✅ 已完成
**文件**：
- `ViewModels/MedicalCaseFlowViewModel.cs` - 流程协调器
- `Views/MedicalCaseFlowView.xaml` - 主界面布局

**已实现功能**：
- ✅ 4步进度条显示
- ✅ 患者信息条（Step 2-4可见）
- ✅ 前一步/后一步按钮（含禁用状态）
- ✅ 取消/保存草稿按钮
- ✅ ContentControl动态View切换
- ✅ FlowStep枚举状态管理

**待优化**：
- ⚠️ ViewModel实例重建导致数据丢失（已记录技术债务）

---

### Task #1497：Step 1患者选择 ✅

**状态**：✅ 已完成
**文件**：
- `ViewModels/PatientSelectionViewModel.cs`
- `Views/PatientSelectionView.xaml`

**已实现功能**：
- ✅ 患者列表显示（最近就诊）
- ✅ 患者搜索功能
- ✅ 患者选择事件触发
- ✅ 快速新建患者按钮（调用PatientSelectionDialog）
- ✅ WPF线程安全修复（ObservableCollection）

**已修复问题**：
- ✅ 患者列表加载线程异常（使用RunOnUIThread）
- ✅ LastVisitDate属性绑定错误（改为LastVisitTime）
- ✅ 患者参数传递到MedicalCaseFlowViewModel

**待实现**：
- ❌ 快速新建患者对话框（TODO注释 line 219）

---

### Task #1498：Step 2填写诊断 ✅

**状态**：✅ 基本完成
**文件**：
- `Consultation模块/ViewModels/ConsultationFormViewModel.cs`
- `Consultation模块/Views/ConsultationFormView.xaml`

**已实现功能**：
- ✅ ConsultationFormViewModel注册到DI容器
- ✅ ConsultationModule改为WhenAvailable立即加载
- ✅ View动态创建（避免循环依赖）
- ✅ AutoWireViewModel自动关联ViewModel
- ✅ 患者信息和MedicalCaseId通过反射传递

**已修复问题**：
- ✅ ConsultationModule OnDemand导致未加载
- ✅ ConsultationFormViewModel未注册
- ✅ DataTemplate循环依赖（改为动态创建View）

**待验证**：
- ⚠️ 诊断数据验证逻辑
- ⚠️ 诊断数据在导航间的持久化

---

### Task #1499：Step 3填写处方 ✅

**状态**：✅ 基本完成
**文件**：
- `ViewModels/PrescriptionEditorViewModel.cs`
- `Views/PrescriptionEditorView.xaml`

**已实现功能**：
- ✅ 8列DataGrid（4对药材+用量）
- ✅ 添加行按钮
- ✅ 剂数、用法、医嘱、备注输入
- ✅ 单剂价格、总价格自动计算
- ✅ 药材总数统计
- ✅ IValidatable验证接口
- ✅ ISaveable保存接口（演示模式）

**已修复问题**（今日）：
- ✅ 验证逻辑修改：检查HerbName而非HerbId（支持手工输入）
- ✅ Binding Mode错误：SingleDosagePrice和TotalPrice改为OneWay

**技术债务（已记录）**：
- ⚠️ HerbId验证跳过（阶段1临时方案）
- ⚠️ 药材选择器未集成
- ⚠️ 价格计算使用临时假设（每克1元）
- ⚠️ SaveAsync仅记录日志，未实际保存
- ⚠️ ViewModel重建导致数据丢失

**待实现**：
- ❌ 集成Herbs模块药材选择器
- ❌ 实现IPrescriptionRepository调用
- ❌ 更新MedicalCase.PrescriptionId

---

### Task #1500：Step 4完成医案 ⏳

**状态**：⏳ 待验证
**文件**：
- `ViewModels/CompletionViewModel.cs`
- `Views/CompletionView.xaml`

**已实现功能**：
- ✅ CompletionViewModel注册
- ✅ CompletionView注册为导航视图
- ✅ InitializeAsync异步初始化（Fire-and-Forget）

**待验证**：
- ❓ Step 3到Step 4导航是否正常
- ❓ 完成界面是否正常显示
- ❓ 继续看诊/返回主页按钮功能

---

### Task #1501：流程状态机接口 ✅

**状态**：✅ 已完成
**文件**：
- `Interfaces/IValidatable.cs` - 可验证接口
- `Interfaces/ISaveable.cs` - 可保存接口

**已实现功能**：
- ✅ IValidatable.Validate()方法定义
- ✅ IValidatable.ValidationMessage属性
- ✅ ISaveable.SaveAsync()方法定义
- ✅ PrescriptionEditorViewModel实现两个接口

---

## 2️⃣ 已修复问题汇总

### 今日修复（2025-10-20）

#### 修复1：处方验证逻辑放宽 ✅
**问题**：用户手工输入药材名称后，点击"下一步"报错"至少添加一种药材"
**原因**：验证检查`HerbId != Guid.Empty`，手工输入时HerbId仍为空
**解决**：
```csharp
// PrescriptionEditorViewModel.cs line 327-345
// 修改前：if (row.Item1.HerbId != Guid.Empty)
// 修改后：if (!string.IsNullOrWhiteSpace(row.Item1.HerbName))
```
**文件**：`ViewModels/PrescriptionEditorViewModel.cs`
**状态**：✅ 编译通过（0警告 0错误）
**技术债务**：已创建跟踪文档 `docs/reports/medical-case-flow-validation-debt-2025-10-20.md`

---

### 历史修复（本周）

#### 修复2：WPF线程安全 ✅
**问题**：`System.NotSupportedException: 该类型的 CollectionView 不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改`
**文件**：`Patients/ViewModels/PatientSelectionDialogViewModel.cs` line 199-210
**解决**：使用`RunOnUIThread()`包裹ObservableCollection操作

#### 修复3：属性绑定错误 ✅
**问题**：`'LastVisitDate' property not found on 'object' 'PatientDto'`
**文件**：`Patients/Views/PatientSelectionDialog.xaml` line 155
**解决**：`LastVisitDate` → `LastVisitTime`

#### 修复4：模块加载失败 ✅
**问题**：导航到MedicalCaseFlowView时NullReferenceException
**原因**：MedicalCaseModule、ConsultationModule、PrescriptionsModule配置为OnDemand未加载
**文件**：`Shell/App.xaml.cs` line 271, 274, 277
**解决**：改为`InitializationMode.WhenAvailable`

#### 修复5：患者信息不显示 ✅
**问题**：选择患者后，MedicalCaseFlowView显示Step 1而非Step 2
**原因**：OnNavigatedTo未正确处理"Patient"参数
**文件**：`ViewModels/MedicalCaseFlowViewModel.cs` line 488-503
**解决**：添加Patient参数提取和ExecuteNextStepAsync()调用

#### 修复6：ConsultationFormViewModel解析失败 ✅
**问题**：`ContainerResolutionException: An unexpected error occurred while resolving 'ConsultationFormViewModel'`
**原因**：ViewModel未注册
**文件**：`Consultation/ConsultationModule.cs` line 28
**解决**：添加`containerRegistry.Register<ViewModels.ConsultationFormViewModel>();`

#### 修复7：View显示ViewModel类型名 ✅
**问题**：ContentControl显示"LYBT.Desktop.Consultation.ViewModels.ConsultationFormViewModel"而非UI
**原因**：无DataTemplate映射，且存在循环依赖
**文件**：`ViewModels/MedicalCaseFlowViewModel.cs` line 416-456
**解决**：
- 移除跨模块DataTemplate
- 动态创建View实例（Type.GetType + Resolve）
- 利用AutoWireViewModel自动关联
- Loaded事件通过反射设置属性
- CurrentStepViewModel类型改为`object?`

#### 修复8：Binding Mode错误 ✅
**问题**：`XamlParseException: 无法对只读属性进行 TwoWay 绑定`
**文件**：`Views/PrescriptionEditorView.xaml` line 217, 225
**解决**：SingleDosagePrice和TotalPrice绑定添加`Mode=OneWay`

---

## 3️⃣ 当前状态和下一步

### 当前状态（2025-10-20）
- ✅ Step 1（患者选择）→ Step 2（诊断录入）→ Step 3（处方编辑）导航正常
- ✅ 处方验证逻辑已放宽，允许手工输入药材名称
- ✅ 编译通过（0警告 0错误）
- ⏳ **待验证**：Step 3 → Step 4导航和UI显示

### 下一步任务（阶段1收尾）
1. ⏳ **用户验证**：测试完整4步流程
   - Step 3手工输入药材后点击"下一步"
   - 验证Step 4界面是否正常显示
   - 测试"前一步"/"后一步"导航是否正常
   - 测试"取消"/"保存草稿"按钮功能

2. ⏳ **已知问题验证**：
   - 数据在导航间是否丢失（已知会丢失，阶段2修复）
   - Step 4初始化是否正常（InitializeAsync）

3. ⏳ **问题记录**：
   - 记录所有发现的UI/UX问题
   - 创建GitHub Issues跟踪

---

## 4️⃣ 技术债务总览

### 高优先级（阶段2）
1. **ViewModel重建导致数据丢失** 📍最重要
   - 影响：用户点击前一步/后一步时数据丢失
   - 解决方案：在MedicalCaseFlowViewModel中缓存ViewModel实例
   - 跟踪文档：`docs/reports/medical-case-flow-validation-debt-2025-10-20.md`

2. **处方数据持久化**
   - SaveAsync()仅记录日志，未实际保存
   - 需注入IPrescriptionRepository
   - 需更新MedicalCase.PrescriptionId

3. **处方数据完整性验证**
   - 当前跳过HerbId验证
   - 需集成Herbs模块选择器
   - 需从Herbs表获取真实价格

### 中优先级（阶段2-3）
4. **诊断数据验证和持久化**
   - ConsultationFormViewModel验证逻辑待检查
   - 诊断数据在导航间是否丢失待验证

5. **快速新建患者**
   - PatientSelectionViewModel line 219 TODO

6. **UI优化**
   - 错误提示优化
   - 加载状态提示
   - 输入验证提示

---

## 5️⃣ 文件清单

### 核心文件
```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── ViewModels/
│   ├── MedicalCaseFlowViewModel.cs          ✅ 流程协调器（已修复7个问题）
│   ├── PatientSelectionViewModel.cs         ✅ Step 1（已修复2个问题）
│   ├── PrescriptionEditorViewModel.cs       ✅ Step 3（今日修复验证逻辑）
│   └── CompletionViewModel.cs               ⏳ Step 4（待验证）
├── Views/
│   ├── MedicalCaseFlowView.xaml             ✅ 主界面布局
│   ├── PatientSelectionView.xaml            ✅ Step 1视图
│   ├── PrescriptionEditorView.xaml          ✅ Step 3视图（已修复绑定模式）
│   └── CompletionView.xaml                  ⏳ Step 4视图（待验证）
├── Interfaces/
│   ├── IValidatable.cs                      ✅ 验证接口
│   └── ISaveable.cs                         ✅ 保存接口
└── Models/
    └── FlowStep.cs                          ✅ 流程步骤枚举

src/Client/Desktop/Modules/LYBT.Desktop.Consultation/
└── ViewModels/ConsultationFormViewModel.cs   ✅ Step 2（已修复注册问题）
    Views/ConsultationFormView.xaml           ✅ Step 2视图（动态创建）

src/Client/Desktop/Shell/
└── App.xaml.cs                              ✅ 模块加载配置（已修复3个模块）

src/Client/Desktop/Modules/LYBT.Desktop.Patients/
└── ViewModels/PatientSelectionDialogViewModel.cs  ✅ 患者选择对话框（已修复线程安全）
    Views/PatientSelectionDialog.xaml        ✅ 患者选择对话框（已修复属性绑定）
```

### 技术债务跟踪文档
```
docs/reports/
└── medical-case-flow-validation-debt-2025-10-20.md  ✅ 今日创建
```

---

## 6️⃣ 依赖关系

```
MedicalCaseFlowViewModel
├─► Step 1: PatientSelectionViewModel (MedicalCase模块)
│   └─► PatientSelectionDialog (Patients模块)
├─► Step 2: ConsultationFormView (Consultation模块，动态创建)
│   └─► ConsultationFormViewModel (AutoWireViewModel自动关联)
├─► Step 3: PrescriptionEditorViewModel (MedicalCase模块)
│   └─► PrescriptionRepository (Prescriptions模块，待集成)
└─► Step 4: CompletionViewModel (MedicalCase模块)
```

**模块依赖关系**：
- `MedicalCaseModule` → `PatientsModule`（患者选择）
- `MedicalCaseModule` → `ConsultationModule`（Step 2诊断）
- `MedicalCaseModule` → `PrescriptionsModule`（Step 3处方）
- `ConsultationModule` → `PatientsModule`（患者信息）
- `ConsultationModule` → `MedicalCaseModule`（医案关联）

**循环依赖**：
- ⚠️ `ConsultationModule` ↔ `MedicalCaseModule`（通过动态View创建解决）

---

## 📝 变更历史

| 日期 | 版本 | 变更描述 |
|------|------|---------|
| 2025-10-20 | v1.0 | 初始创建，梳理Epic #1494完整进度 |

---

## 🔗 相关文档

- **技术债务**：`docs/reports/medical-case-flow-validation-debt-2025-10-20.md`
- **MVP分析**：`docs/reports/mvp-analysis-report-2025-10-16.md`
- **架构指南**：`docs/architecture/client/README.md`（MVVM五层设计）
