# 任务清单：诊断字段精简

## Phase 1: 后端数据层

### Task 1.1: 修改实体
- [ ] 修改 `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
  - 移除: `ChiefComplaint`, `FourDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`, `Remark`
  - 保留: `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TCMDiagnosis`
- **验证**: 编译通过

### Task 1.2: 修改DTO
- [ ] 修改 `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs`
  - `ConsultationDto`: 移除5个字段
  - `ConsultationInputDto`: 移除5个字段
- **验证**: 编译通过

### Task 1.3: 修改验证器
- [ ] 修改 `src/Shared/LYBT.Shared.Validators/Consultation/ConsultationInputDtoValidator.cs`
  - 移除ChiefComplaint必填验证
  - 移除其他4个字段的验证规则
- **验证**: 编译通过

### Task 1.4: 创建数据库迁移
- [ ] 运行 `dotnet ef migrations add RemoveDiagnosisFields`
- [ ] 可选：在迁移中添加数据备份逻辑
- **验证**: `dotnet ef database update` 成功

### Task 1.5: 更新服务层映射
- [ ] 修改 `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- [ ] 修改 `src/Server/Modules/LYBT.Module.Consultation/` 相关服务
- [ ] 更新AutoMapper配置（如有）
- **验证**: API测试通过

---

## Phase 2: 客户端数据层

### Task 2.1: 修改客户端模型
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Models/ConsultationItem.cs`
  - 移除5个字段属性
  - 更新 `FromDto()` 和 `ToDto()` 方法
- **验证**: 编译通过

### Task 2.2: 修改ConsultationFormViewModel
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs`
  - 移除: `_chiefComplaint`, `_fourDiagnosis`, `_treatmentPrinciple`, `_medicalAdvice`, `_remark` 字段
  - 移除对应属性
  - 更新验证逻辑（TCMDiagnosis为唯一必填）
- **验证**: 编译通过

### Task 2.3: 修改ConsultationPanelViewModel
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/ConsultationPanelViewModel.cs`
  - 移除5个字段
  - 更新 `LoadFromDto()` 方法
  - 更新 `GetConsultationData()` 方法
  - 更新 `Validate()` 方法（TCMDiagnosis为唯一必填）
- **验证**: 编译通过

### Task 2.4: 修改DataManager
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationDataManager.cs`
  - 移除5个字段的 `UpdateField()` switch分支
  - 移除 `HasChanges()` 比较逻辑
  - 移除 `CloneConsultation()` 字段
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseDataManager.cs`
  - 移除 `HasConsultationChanges()` 相关字段
  - 移除 `CloneConsultationDto()` 相关字段
- **验证**: 编译通过

### Task 2.5: 修改CommandHandler
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationCommandHandler.cs`
  - 移除清空字段逻辑
- **验证**: 编译通过

---

## Phase 3: 客户端UI层

### Task 3.1: 修改ConsultationFormView
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml`
  - 移除5个输入框
  - 重新布局：现病史、舌诊/脉诊并排、中医诊断
- **验证**: UI正确显示

### Task 3.2: 修改ConsultationPanel
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml`
  - 移除5个显示区域
- **验证**: UI正确显示

### Task 3.3: 修改MedicalCaseWorkspaceView
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
  - 移除诊断预览区域中的5个字段
- **验证**: UI正确显示

---

## Phase 4: 打印功能

### Task 4.1: 修改打印DTO
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Models/PrescriptionPrintDto.cs`
  - 移除: `ChiefComplaint`, `FourDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`, `Remark`
- **验证**: 编译通过

### Task 4.2: 修改打印服务
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
  - 移除5个字段的填充逻辑
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`
  - 移除5个字段的显示逻辑
- [ ] 修改 `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionPrintTemplate.xaml.cs`
  - 移除5个字段的显示逻辑
- **验证**: 打印预览正确

---

## Phase 5: 测试与验证

### Task 5.1: 更新单元测试
- [ ] 更新 `tests/UnitTests/Server/Modules/LYBT.Module.Consultation.Tests/Mapping/ConsultationMappingProfileTests.cs`
- [ ] 更新 `tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Components/MedicalCaseDataManagerTests.cs`
- [ ] 更新 `tests/UnitTests/Server/Common/TestData/TestDataFactory.cs`
- [ ] 更新 `tests/UnitTests/Server/Core/LYBT.Entities.Tests/Consultation/ConsultationModelTests.cs`
- [ ] 更新其他受影响的测试文件
- **验证**: `dotnet test` 全部通过

### Task 5.2: 集成测试
- [ ] 启动应用，创建新诊断记录
- [ ] 验证数据保存和读取
- [ ] 验证打印功能
- **验证**: 功能正常

---

## 依赖关系

```
Task 1.1 ──> Task 1.2 ──> Task 1.3 ──> Task 1.4 ──> Task 1.5
                │
                v
Task 2.1 ──> Task 2.2 ──> Task 2.4
     │           │
     │           v
     │       Task 2.3 ──> Task 2.5
     │
     v
Task 4.1 ──> Task 4.2
     │
     v
Task 3.1 ──> Task 3.2 ──> Task 3.3
                              │
                              v
                          Task 5.1 ──> Task 5.2
```

## 字段变更汇总

| 操作 | 字段名 | 中文名 |
|------|--------|--------|
| 移除 | `ChiefComplaint` | 主诉 |
| 移除 | `FourDiagnosis` | 四诊 |
| 移除 | `TreatmentPrinciple` | 治疗原则 |
| 移除 | `MedicalAdvice` | 医嘱 |
| 移除 | `Remark` | 备注 |
| 保留 | `PresentIllness` | 现病史 |
| 保留 | `TongueDiagnosis` | 舌诊 |
| 保留 | `PulseDiagnosis` | 脉诊 |
| 保留 | `TCMDiagnosis` | 中医诊断 |
