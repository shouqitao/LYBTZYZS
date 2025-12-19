# 任务清单：诊断字段精简

> 状态: 已完成
> 归档日期: 2025-12-19
> 数据库迁移: 20251217052007_RemoveConsultationDiagnosisFields

## Phase 1: 后端数据层

### Task 1.1: 修改实体
- [x] 修改 `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
  - 移除: `ChiefComplaint`, `FourDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`, `Remark`
  - 保留: `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TCMDiagnosis`
- **验证**: 编译通过

### Task 1.2: 修改DTO
- [x] 修改 `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs`
  - `ConsultationDetailDto`: 移除5个字段
  - `ConsultationInputDto`: 移除5个字段
- **验证**: 编译通过

### Task 1.3: 修改验证器
- [x] 修改 `src/Shared/LYBT.Shared.Validators/Consultation/ConsultationInputDtoValidator.cs`
  - 移除ChiefComplaint必填验证
  - 移除其他4个字段的验证规则
- **验证**: 编译通过

### Task 1.4: 创建数据库迁移
- [x] 运行 `dotnet ef migrations add RemoveConsultationDiagnosisFields`
- [x] 迁移已应用到数据库
- **验证**: `dotnet ef database update` 成功

### Task 1.5: 更新服务层映射
- [x] 修改 `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- [x] 修改 `src/Server/Modules/LYBT.Module.Consultation/` 相关服务
- [x] 更新AutoMapper配置
- **验证**: API测试通过

---

## Phase 2: 客户端数据层

### Task 2.1: 修改客户端模型
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Models/ConsultationItem.cs`
  - 移除5个字段属性
  - 更新 `FromDto()` 和 `ToDto()` 方法
- **验证**: 编译通过

### Task 2.2: 修改ConsultationFormViewModel
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs`
  - 移除: `_chiefComplaint`, `_fourDiagnosis`, `_treatmentPrinciple`, `_medicalAdvice`, `_remark` 字段
  - 移除对应属性
  - 更新验证逻辑（TCMDiagnosis为唯一必填）
- **验证**: 编译通过

### Task 2.3: 修改ConsultationPanelViewModel
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/ConsultationPanelViewModel.cs`
  - 移除5个字段
  - 更新 `LoadFromDto()` 方法
  - 更新 `GetConsultationData()` 方法
  - 更新 `Validate()` 方法（TCMDiagnosis为唯一必填）
- **验证**: 编译通过

### Task 2.4: 修改DataManager
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationDataManager.cs`
  - 移除5个字段的 `UpdateField()` switch分支
  - 移除 `HasChanges()` 比较逻辑
  - 移除 `CloneConsultation()` 字段
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseDataManager.cs`
  - 移除 `HasConsultationChanges()` 相关字段
  - 移除 `CloneConsultationDto()` 相关字段
- **验证**: 编译通过

### Task 2.5: 修改CommandHandler
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationCommandHandler.cs`
  - 移除清空字段逻辑
- **验证**: 编译通过

---

## Phase 3: 客户端UI层

### Task 3.1: 修改ConsultationFormView
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml`
  - 移除5个输入框
  - 重新布局：现病史、舌诊/脉诊并排、中医诊断
- **验证**: UI正确显示

### Task 3.2: 修改ConsultationPanel
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml`
  - 移除5个显示区域
- **验证**: UI正确显示

### Task 3.3: 修改MedicalCaseWorkspaceView
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
  - 移除诊断预览区域中的5个字段
- **验证**: UI正确显示

---

## Phase 4: 打印功能

### Task 4.1: 修改打印DTO
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Models/PrescriptionPrintDto.cs`
  - 移除: `ChiefComplaint`, `FourDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`, `Remark`
- **验证**: 编译通过

### Task 4.2: 修改打印服务
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
  - 移除5个字段的填充逻辑
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`
  - 移除5个字段的显示逻辑
- [x] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionPrintTemplate.xaml.cs`
  - 移除5个字段的显示逻辑
- **验证**: 打印预览正确

---

## Phase 5: 测试与验证

### Task 5.1: 更新单元测试
- [x] 更新 `tests/UnitTests/Server/Modules/LYBT.Module.Consultation.Tests/Mapping/ConsultationMappingProfileTests.cs`
- [x] 更新 `tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Components/MedicalCaseDataManagerTests.cs`
- [x] 更新 `tests/UnitTests/Server/Common/TestData/TestDataFactory.cs`
- [x] 更新其他受影响的测试文件
- **验证**: `dotnet test` 全部通过

### Task 5.2: 集成测试
- [x] 启动应用，创建新诊断记录
- [x] 验证数据保存和读取
- [x] 验证打印功能
- **验证**: 功能正常

---

## 后续清理 (2025-12-19)

### consultation-field-alignment 补充变更
- [x] 删除 `Consultation.PrescriptionEnabled` 字段 (移至MedicalCase.NeedsPrescription)
- [x] 清理ConsultationItem 10个技术债务字段
- [x] 统一命名 TcmDiagnosis → TCMDiagnosis
- [x] 数据库迁移: 20251219011610_RemoveConsultationPrescriptionEnabled

---

## 字段变更汇总

| 操作 | 字段名 | 中文名 |
|------|--------|--------|
| 移除 | `ChiefComplaint` | 主诉 |
| 移除 | `FourDiagnosis` | 四诊 |
| 移除 | `TreatmentPrinciple` | 治疗原则 |
| 移除 | `MedicalAdvice` | 医嘱 |
| 移除 | `Remark` | 备注 |
| 移除 | `PrescriptionEnabled` | 处方开关(移至MedicalCase) |
| 保留 | `PresentIllness` | 现病史 |
| 保留 | `TongueDiagnosis` | 舌诊 |
| 保留 | `PulseDiagnosis` | 脉诊 |
| 保留 | `TCMDiagnosis` | 中医诊断 |
