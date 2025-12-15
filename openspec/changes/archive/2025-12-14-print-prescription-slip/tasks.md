# Tasks: print-prescription-slip

## Phase 1: 配置层实现

### Task 1.1: 添加诊所配置节
- **文件**: `src/Server/LYBT.WebAPI/appsettings.json`
- **操作**: 添加 ClinicSettings 配置节
- **验收**: 配置文件包含 Name、Address、Phone、Department 字段

### Task 1.2: 创建诊所配置模型
- **文件**: `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Models/ClinicSettings.cs`
- **操作**: 创建强类型配置类
- **验收**: 类包含所有配置属性

### Task 1.3: 实现配置读取服务
- **文件**: `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Services/ClinicSettingsService.cs`
- **操作**: 实现 IClinicSettingsService 接口，从本地配置文件读取诊所信息
- **验收**: 服务可正确读取配置，支持热更新

## Phase 2: 打印服务数据集成

### Task 2.1: 注入依赖服务
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- **操作**: 注入 IClinicSettingsService、ICrossModuleQueryService
- **验收**: 构造函数包含新依赖

### Task 2.2: 实现 PopulateClinicInfo
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- **操作**: 从 IClinicSettingsService 获取诊所信息填充
- **验收**: 打印预览显示配置的诊所名称

### Task 2.3: 实现 PopulatePatientInfo
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- **操作**: 通过 ICrossModuleQueryService 获取患者信息
- **验收**: 打印预览显示真实患者姓名、性别、年龄

### Task 2.4: 实现 PopulateDiagnosisInfo
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- **操作**: 映射 Consultation 的 TCMDiagnosis 到"诊断"，TreatmentPrinciple 到"诊见"
- **验收**: 打印预览正确显示中医诊断和治疗方案

## Phase 3: 接口扩展

### Task 3.1: 扩展打印方法签名
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Interfaces/IPrescriptionPrintService.cs`
- **操作**: 添加支持传入患者ID和医案ID的重载方法（保持原方法向后兼容）
- **验收**: 新增方法签名，原接口不变

### Task 3.2: 更新调用方传递上下文
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`
- **操作**: 调用打印时传递完整的患者ID和医案ID
- **验收**: 打印功能可获取完整上下文

## Phase 4: 布局调整

### Task 4.1: 调整 FlowDocument 布局
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`
- **操作**: 调整布局匹配处方模板格式
- **验收**: 打印布局与模板一致

### Task 4.2: 药材格式化
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`
- **操作**: 确保药材显示格式为"药名+剂量g"
- **验收**: 药材列表格式正确

## Phase 5: 测试与验证

### Task 5.1: 单元测试
- **文件**: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Prescriptions.Tests/Services/PrescriptionPrintServiceTests.cs`
- **操作**: 添加配置读取、数据映射的单元测试
- **验收**: 测试覆盖核心逻辑

### Task 5.2: 集成验证
- **操作**: 手动测试打印预览功能
- **验收**: 满足所有 AC 验收标准

## Dependencies

```
Phase 1 (配置层)
    ↓
Phase 2 (数据集成) ← Phase 3 (接口扩展)
    ↓
Phase 4 (布局调整)
    ↓
Phase 5 (测试验证)
```

## Acceptance Criteria Mapping

| AC | 相关 Task |
|----|-----------|
| AC-001 | Task 1.1 |
| AC-002 | Task 2.3 |
| AC-003 | Task 4.2 |
| AC-004 | Task 2.4 |
| AC-005 | Task 2.4 |
| AC-006 | Task 1.3, Task 2.2 |
| AC-007 | Task 4.1 |
