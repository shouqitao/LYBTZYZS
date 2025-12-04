# repository-cleanup Specification

## Purpose
TBD - created by archiving change cleanup-unused-methods. Update Purpose after archive.
## Requirements
### Requirement: REPO-CLEANUP-001 MedicalCase Repository清理

MedicalCaseRepository SHALL 删除未被调用的GetByDoctorIdAsync方法。

#### Scenario: 删除GetByDoctorIdAsync
- **WHEN** 清理MedicalCase模块
- **THEN** SHALL 从IMedicalCaseRepository删除GetByDoctorIdAsync接口定义
- **AND** SHALL 从MedicalCaseRepository删除GetByDoctorIdAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: REPO-CLEANUP-002 Patient Repository清理

PatientRepository SHALL 删除未被调用的GetByDateRangeAsync方法。

#### Scenario: 删除GetByDateRangeAsync
- **WHEN** 清理Patient模块
- **THEN** SHALL 从IPatientRepository删除GetByDateRangeAsync接口定义
- **AND** SHALL 从PatientRepository删除GetByDateRangeAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: REPO-CLEANUP-003 Formula Repository清理

FormulaRepository SHALL 删除未被调用的分类查询和共享方剂方法。

#### Scenario: 删除GetByCategoryAsync
- **WHEN** 清理Formula模块
- **THEN** SHALL 从IFormulaRepository删除GetByCategoryAsync接口定义
- **AND** SHALL 从FormulaRepository删除GetByCategoryAsync实现

#### Scenario: 删除GetSharedFormulasAsync
- **WHEN** 清理Formula模块共享功能
- **THEN** SHALL 从IFormulaRepository删除GetSharedFormulasAsync接口定义
- **AND** SHALL 从FormulaRepository删除GetSharedFormulasAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: REPO-CLEANUP-004 Herb Repository清理

HerbRepository SHALL 删除未被调用的GetByCategoryAsync方法。

#### Scenario: 删除GetByCategoryAsync
- **WHEN** 清理Herbs模块
- **THEN** SHALL 从IHerbRepository删除GetByCategoryAsync接口定义
- **AND** SHALL 从HerbRepository删除GetByCategoryAsync实现
- **AND** 编译 SHALL 通过，无错误

---

