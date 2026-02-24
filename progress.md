# Progress: Sprint 2 - Core Feature Fixes

## Session: 2026-02-23 22:20

### Sprint 2 Execution Start

- Branch: `feature/phase2-architecture-optimization`
- Base: Sprint 1 complete (1399 passed, 0 failed)
- Last commit: `40158be00` (Sprint1-Batch6)

### Batch 1: X8 实体基础 + PrintType + 索引 [COMPLETE]

**Modified files (7)**: MedicalCaseModel.cs, MedicalCaseConfiguration.cs, AppDbContext.cs, PrescriptionModel.cs(标记), MedicalCaseDetailDto.cs, MedicalCaseMapper.cs, LocalMedicalCaseMapper.cs
**New files (3)**: PrintType.cs, MedicalCasePrintLog.cs, MedicalCasePrintLogConfiguration.cs

**Verification**: Build 0 errors | Arch 58 passed | Unit 561 passed

### Batch 2: X8 打印逻辑 + 保护 [COMPLETE]

**Server 端变更 (8 files):**
- MedicalCaseCommandService -- 打印保护迁移 + RecordPrintCompletedAsync
- IMedicalCaseCommandService/Facade -- 新增接口
- MedicalCaseController -- PUT /{id}/print-completed
- MedicalCaseMapper -- 清理 Prescription IgnoreTarget
- PrescriptionModel -- 移除旧打印字段
- PrescriptionPrintLogConfiguration -- WithMany() 改无导航
- HerbService -- p.IsPrinted -> mc.IsPrinted

**Desktop 端变更 (5 files):**
- PrescriptionPrintHandler -- 注入 Repository + 打印回写
- IMedicalCaseApi/Repository -- RecordPrintCompletedAsync
- LocalMedicalCaseMapper -- 清理 IgnoreSource

**New files (1)**: PrintCompletedRequest.cs

**Verification**: Build 0 errors 0 warnings | Arch 58 passed | Unit 553 passed
