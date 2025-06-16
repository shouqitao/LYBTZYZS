# LYBT.Module.DiagnosisTreatment

诊疗记录模块，负责保存诊断内容、治疗方案及药方信息。

## 主要服务及接口
- `IDiagnosisTreatmentService` / `DiagnosisTreatmentService`
- `IDiagnosisTreatmentRepository` / `DiagnosisTreatmentRepository`

## 重要模型和DTO
- `DiagnosisTreatmentModel`、`TreatmentItemModel`、`FormulaModel`
- `DiagnosisTreatmentDto`、`DiagnosisTreatmentCreateDto`、`DiagnosisTreatmentEditDto`、`DiagnosisTreatmentDetailDto`
- `TreatmentItemDto`、`FormulaDto`

## 用法
调用 `DiagnosisTreatmentModule.Register(services)` 注册后，通过 `IDiagnosisTreatmentService` 进行诊疗记录的增删改查。
