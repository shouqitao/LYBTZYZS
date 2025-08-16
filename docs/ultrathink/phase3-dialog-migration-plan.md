# Phase 3: IDialogService 全量迁移计划

## 📊 迁移统计

**总计发现**: 73个文件包含IDialogService引用  
**预估工作量**: 约150-200个引用点需要替换

## 🎯 迁移策略

### 核心策略
1. **替换IDialogService → ICustomDialogService**
2. **更新构造函数注入参数**
3. **替换方法调用语法**
4. **更新DI注册配置**

### 替换映射
```csharp
// 原有语法
await _dialogService.ShowInformationAsync(message, title);
await _dialogService.ShowConfirmationAsync(message, title);
var input = await _dialogService.ShowInputAsync(message, title, defaultValue);

// 新语法（保持相同）
await _dialogService.ShowInformationAsync(message, title);
await _dialogService.ShowConfirmationAsync(message, title);
var input = await _dialogService.ShowInputAsync(message, title, defaultValue);

// 业务对话框（新增）
var result = await _dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);
var result = await _dialogService.ShowDialogAsync("FormulaSelectionDialog", parameters);
```

## 📋 迁移优先级分类

### 🔴 优先级1: 核心基础设施 (立即处理)
1. **Core/Extensions/DialogServiceExtensions.cs** ⭐⭐⭐
   - 现有扩展方法，需要删除或标记过时
   - 这是其他模块的依赖基础

2. **Core/Extensions/CustomDialogServiceExtensions.cs** ✅
   - 已实现，提供向后兼容

### 🟠 优先级2: 核心业务模块 (高优先级)

#### Consultation模块 (11个文件)
- `ConsultationMainViewModel.cs` ⭐⭐⭐
- `ConsultationWorkflowCoordinator.cs` ⭐⭐
- `DifferentiationViewModel.cs` ⭐⭐
- `PrescriptionViewModel.cs` ⭐⭐⭐
- `PrescriptionViewModelRefactored.cs` (可能已废弃)
- `SelectFormulaDialogViewModel.cs` ⭐⭐
- `SelectHerbDialogViewModel.cs` ⭐⭐
- `SimpleTCMFourDiagnosisViewModel.cs` ⭐
- `TCMFourDiagnosisCoordinator.cs` ⭐
- `TCMFourDiagnosisViewModel.cs` ⭐
- `WorkflowNavigatorViewModel.cs` ⭐
- `Components/PrescriptionCommandHandler.cs` ⭐⭐

### 🟡 优先级3: 管理模块 (中优先级)

#### Formula模块 (1个文件)
- `FormulaManagementViewModelEnhanced.cs` ⭐⭐

#### Herbs模块 (2个文件)
- `HerbManagementViewModelEnhanced.cs` ⭐⭐
- `HerbManagementViewModelSimple.cs` ⭐⭐

#### MedicalCase模块 (3个文件)
- `CreateMedicalCaseViewModel.cs` ⭐⭐
- `MedicalCaseDetailViewModel.cs` ⭐⭐
- `MedicalCaseListViewModel.cs` ⭐⭐

#### Patients模块 (1个文件)
- `PatientManagementViewModelSimple.cs` ⭐⭐

#### Users模块 (1个文件)
- `UserManagementViewModelSimple.cs` ⭐

### 🟢 优先级4: 处方模块 (中低优先级)

#### Prescriptions模块 (2个文件)
- `PrescriptionEditorDialogViewModel.cs` (已临时禁用)
- `PrescriptionManagementViewModel.cs` (已临时禁用)

### 🔵 优先级5: SystemManagement模块 (低优先级)
**注意**: 这些文件在git状态中标记为删除，可能不需要迁移

#### Formulas (4个文件)
- `AddFormulaDialogViewModel.cs`
- `EditFormulaDialogViewModel.cs` 
- `FormulaManagementViewModel.cs`
- `ViewFormulaDialogViewModel.cs`
- `AddFormulaDialog.xaml.cs`

#### Herbs (5个文件)
- `AddHerbDialogViewModel.cs`
- `EditHerbDialogViewModel.cs`
- `HerbManagementViewModelRefactored.cs`
- `StockManagementDialogViewModel.cs`
- `ViewHerbDialogViewModel.cs`

#### Prescriptions (5个文件)
- `AddPrescriptionDialogViewModel.cs`
- `EditPrescriptionDialogViewModel.cs`
- `PrescriptionManagementViewModel.cs`
- `ViewPrescriptionDialogViewModel.cs`

#### System (1个文件)
- `SystemManagementViewModel.cs`

### 🟣 优先级6: Shared模块 (低优先级)

#### Shared ViewModels (5个文件)
- `Consultation/ConsultationManagementViewModel.cs`
- `Formula/FormulaManagementViewModel.cs`
- `Herbs/HerbManagementViewModel.cs`
- `Patients/PatientManagementViewModelSimple.cs`
- `Prescriptions/PrescriptionManagementViewModel.cs`
- `Users/UserManagementViewModelSimple.cs`

### ⚫ 优先级7: 服务层 (最低优先级)

#### Services (1个文件)
- `PrismDialogService.cs` - 适配器，可能需要重构或删除

## 🚀 执行计划

### Phase 3.1: 核心基础设施清理 ✋ **当前阶段**
1. 处理DialogServiceExtensions.cs（标记过时或删除）
2. 确保CustomDialogServiceExtensions.cs完整

### Phase 3.2: 核心业务模块迁移
1. **Consultation模块完整迁移**（11个文件）
2. **验证业务流程正常**

### Phase 3.3: 管理模块迁移
1. Formula、Herbs、MedicalCase、Patients、Users模块
2. **验证管理界面正常**

### Phase 3.4: 其他模块迁移
1. Prescriptions模块（如果需要）
2. Shared模块
3. 清理过时文件

### Phase 3.5: 最终验证
1. 完整系统测试
2. 性能验证
3. 清理临时代码

## 🔧 迁移工具和辅助

### 自动化替换脚本
```bash
# 批量替换接口名称
find . -name "*.cs" -exec sed -i 's/IDialogService/ICustomDialogService/g' {} \;

# 批量替换using声明
find . -name "*.cs" -exec sed -i 's/using Prism.Services.Dialogs;/using LYBT.Desktop.Core.Interfaces.Services;/g' {} \;
```

### 验证清单
- [ ] 编译无错误
- [ ] 所有对话框正常显示
- [ ] 参数传递正确
- [ ] 业务流程完整
- [ ] 性能无回归

## 📈 成功指标

1. **编译成功率**: 100%
2. **功能完整性**: 所有对话框功能保持不变
3. **性能指标**: 对话框响应时间 < 500ms
4. **代码质量**: 无警告，遵循编码规范
5. **用户体验**: 界面行为完全一致

## ⚠️ 风险点

1. **构造函数依赖链**可能需要大量修改
2. **业务逻辑**中的复杂对话框调用
3. **测试覆盖**确保迁移不破坏现有功能
4. **SystemManagement模块**文件状态不明确

## 📝 迁移日志

- **2025-01-XX**: Phase 3.1开始 - 核心基础设施清理
- **TBD**: Phase 3.2开始 - Consultation模块迁移
- **TBD**: Phase 3.3开始 - 管理模块迁移
- **TBD**: Phase 3.4开始 - 其他模块迁移
- **TBD**: Phase 3.5开始 - 最终验证

---

**总结**: 这是一个系统性的迁移项目，需要谨慎规划和分步执行。通过优先级分类，确保核心业务功能优先得到保障。