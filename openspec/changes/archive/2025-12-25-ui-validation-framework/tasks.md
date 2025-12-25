# UI层数据验证框架 - 任务分解

## Phase 1: 基础设施建设

### Task 1.1: 创建ValidatableModelBase基类
**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Models/Models/Base/ValidatableModelBase.cs`

- [ ] 创建ValidatableModelBase类
- [ ] 实现INotifyDataErrorInfo接口
- [ ] 添加ValidationErrorsAccessor和HasErrorsAccessor
- [ ] 实现SetPropertyAndValidate方法
- [ ] 实现ValidateProperty和ValidateAll方法

**验收**: 可作为DetailModel的基类，支持DataAnnotations验证

### Task 1.2: 验证ValidationConstants可用性
**文件**: `src/Shared/LYBT.Shared.Primitives/Validation/ValidationConstants.cs`（已存在）

- [ ] 确认现有常量满足UI验证需求
- [ ] 如需补充，添加缺失的MinLength常量
- [ ] 确认Desktop项目可引用Primitives

**验收**: DetailModel可使用ValidationConstants定义验证规则

### Task 1.3: 添加XAML验证样式
**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ValidationStyles.xaml`

- [ ] 创建RequiredIndicatorStyle（红色星号样式）
- [ ] 创建ValidationErrorMessageStyle（错误消息样式）
- [ ] 创建ValidatingTextBoxStyle（带验证边框的TextBox）
- [ ] 创建ValidatingComboBoxStyle（带验证的ComboBox）
- [ ] 更新UnifiedComponents.xaml引用ValidationStyles.xaml

**验收**: 样式可在所有EditControl中使用

---

## Phase 2: Users模块试点

### Task 2.1: 改造UserDetailModel
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Models/UserDetailModel.cs`

- [ ] 修改基类为ValidatableModelBase
- [ ] 添加[Required]属性到UserName, RealName
- [ ] 添加[StringLength]属性到所有字符串属性
- [ ] 将SetProperty改为SetPropertyAndValidate

**验收**: UserDetailModel支持即时验证

### Task 2.2: 更新UserEditControl.xaml
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserEditControl.xaml`

- [ ] 添加ValidatesOnNotifyDataErrors=True到所有绑定
- [ ] 添加红色星号标识到必填字段
- [ ] 添加错误消息TextBlock到每个输入框下方
- [ ] 应用ValidatingTextBoxStyle

**验收**: 验证错误即时显示在输入框下方

### Task 2.3: 更新UserMasterDetailViewModel验证逻辑
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`

- [ ] 在SaveDetailAsync前调用CurrentDetail.ValidateAll()
- [ ] 验证失败时阻止保存并显示错误
- [ ] 更新CanSave判断逻辑

**验收**: 验证失败时保存按钮禁用

---

## Phase 3: Herbs模块改造

### Task 3.1: 改造HerbDetailModel
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Models/HerbDetailModel.cs`

- [ ] 修改基类为ValidatableModelBase
- [ ] 添加[Required]到Name, Unit
- [ ] 添加[StringLength]到所有字符串属性
- [ ] 添加[Range]到Price, CostPrice（可选，>=0）

**验收**: HerbDetailModel支持即时验证

### Task 3.2: 更新HerbEditControl.xaml
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbEditControl.xaml`

- [ ] 添加ValidatesOnNotifyDataErrors=True
- [ ] 添加必填标识和错误消息显示

**验收**: 药材编辑支持即时验证

---

## Phase 4: Patients模块改造

### Task 4.1: 改造PatientDetailModel
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/PatientDetailModel.cs`

- [ ] 修改基类为ValidatableModelBase
- [ ] 添加[Required]到Name
- [ ] 添加[StringLength]到字符串属性
- [ ] 添加[Range]到Age（0-150）

**验收**: PatientDetailModel支持即时验证

### Task 4.2: 更新PatientEditControl.xaml
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientEditControl.xaml`

- [ ] 添加验证绑定和错误显示

**验收**: 患者编辑支持即时验证

---

## Phase 5: Formula模块改造

### Task 5.1: 改造FormulaDetailModel
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/FormulaDetailModel.cs`

- [ ] 修改基类为ValidatableModelBase
- [ ] 添加验证属性

### Task 5.2: 更新FormulaEditControl.xaml
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaEditControl.xaml`

- [ ] 添加验证绑定和错误显示

---

## Phase 6: MedicalCase模块改造

### Task 6.1: 改造MedicalCaseDetailModel
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs`

- [ ] 修改基类为ValidatableModelBase
- [ ] 添加验证属性

### Task 6.2: 更新MedicalCaseEditControl.xaml
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`

- [ ] 添加验证绑定和错误显示

---

## Phase 7: 验证规则审计与统一

### Task 7.1: Entity层验证审计
- [ ] 检查所有Entity的DataAnnotations
- [ ] 确保与ValidationConstants一致

### Task 7.2: DTO层验证审计
- [ ] 检查所有InputDto的验证属性
- [ ] 与ValidationConstants对齐

### Task 7.3: FluentValidation审计
- [ ] 检查服务端验证规则
- [ ] 确保与前端规则一致

---

## 估算工作量

| Phase | 任务数 | 预估复杂度 |
|-------|--------|-----------|
| Phase 1 | 3 | 中 |
| Phase 2 | 3 | 中 |
| Phase 3 | 2 | 低 |
| Phase 4 | 2 | 低 |
| Phase 5 | 2 | 低 |
| Phase 6 | 2 | 低 |
| Phase 7 | 3 | 中 |

## 依赖关系

```
Phase 1 (基础设施)
    │
    ├──► Phase 2 (Users试点)
    │        │
    │        └──► Phase 3-6 (其他模块，可并行)
    │                │
    └────────────────┴──► Phase 7 (规则审计)
```

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 现有ViewModel验证被破坏 | 不修改ViewModelBase，仅新增ValidatableModelBase |
| 验证规则不一致 | Phase 7专门做规则审计 |
| 性能影响 | 验证仅在PropertyChanged时触发，无额外开销 |
