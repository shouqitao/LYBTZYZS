# Delta: client-layer-architecture

## REMOVED Requirements

### Requirement: PRESC-CLEANUP-001 删除重复的PrescriptionCalculator

Prescriptions模块 SHALL 删除ViewModels/Components/PrescriptionCalculator.cs，因为MedicalCase模块已有独立实现。

#### Scenario: 删除重复Calculator
- **WHEN** 执行Prescriptions模块清理
- **THEN** SHALL 删除ViewModels/Components/PrescriptionCalculator.cs文件
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: PRESC-CLEANUP-002 删除重复的PrescriptionValidator

Prescriptions模块 SHALL 删除ViewModels/Components/PrescriptionValidator.cs，因为MedicalCase模块已有独立实现。

#### Scenario: 删除重复Validator
- **WHEN** 执行Prescriptions模块清理
- **THEN** SHALL 删除ViewModels/Components/PrescriptionValidator.cs文件
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: PRESC-CLEANUP-003 删除重复的PrescriptionItemViewModel

Prescriptions模块 SHALL 删除ViewModels/PrescriptionItemViewModel.cs，因为MedicalCase模块已有独立实现。

#### Scenario: 删除重复ViewModel
- **WHEN** 执行Prescriptions模块清理
- **THEN** SHALL 删除ViewModels/PrescriptionItemViewModel.cs文件
- **AND** 编译 SHALL 通过，无错误

---

## MODIFIED Requirements

### Requirement: PRESC-CLEANUP-004 Prescriptions模块最小化服务架构

Prescriptions模块 SHALL 保持最小化服务提供者角色，仅包含打印服务和编辑器服务。

#### Scenario: 模块仅提供核心服务
- **GIVEN** Prescriptions模块完成冗余代码清理
- **WHEN** 检查模块注册的服务
- **THEN** SHALL 仅注册IPrescriptionPrintService
- **AND** SHALL 仅注册IPrescriptionEditorService
- **AND** 无ViewModels或UI组件注册

---
