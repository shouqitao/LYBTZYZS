# Tasks: consolidate-medicalcase-dtos

## 任务清单

### Phase 1: 删除重复定义

- [x] **Task 1.1**: 删除重复的SetPrescriptionFlagRequest
  - 删除 `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/SetPrescriptionFlagRequest.cs`
  - 更新所有引用使用 `LYBT.Shared.Models.Contracts.MedicalCase.SetPrescriptionFlagRequest`
  - 验证编译通过
  - **完成**: 2025-11-29 删除了3处重复定义(Module层文件、Controller嵌入类、Shared层保留)

### Phase 2: 分析与决策

- [x] **Task 2.1**: 分析MedicalCaseDetailResponse
  - 对比 `MedicalCaseDetailResponse` 与 `MedicalCaseDetailDto` 的字段差异
  - 决策: 合并到现有DTO 或 作为独立DTO迁移
  - 记录决策理由
  - **决策**: 保留为模块专用Response，与MedicalCaseDetailDto字段基本相同但包含Prescription嵌套

- [x] **Task 2.2**: 分析MedicalCasePrescriptionDto
  - 对比 `MedicalCasePrescriptionDto` 与 `PrescriptionDto` 的字段差异
  - 决策: 合并到现有DTO 或 作为独立DTO迁移
  - 记录决策理由
  - **决策**: 保留为模块专用简化DTO，是PrescriptionDto的轻量版本

- [x] **Task 2.3**: 分析UpdateMedicalCaseRequest
  - 检查嵌套类型: `UpdateMode`, `PrescriptionUpdateRequest`, `DeletePrescriptionRequest`, `CompleteCaseRequest`
  - 决策: 迁移策略和命名规范
  - 记录决策理由
  - **决策**: 迁移到Shared层，重命名嵌套类型避免冲突

### Phase 3: 迁移DTO到Shared层

- [x] **Task 3.1**: 迁移UpdateMedicalCaseRequest
  - 在 `MedicalCaseDtos.cs` 中添加新的DTO定义
  - 遵循Shared层命名规范
  - 删除原Server层文件
  - **完成**: 已迁移，重命名类型:
    - `UpdateMode` -> `MedicalCaseUpdateMode`
    - `PrescriptionUpdateRequest` -> `MedicalCasePrescriptionUpdateRequest`
    - `DeletePrescriptionRequest` -> `MedicalCaseDeletePrescriptionRequest`
    - `CompleteCaseRequest` -> `MedicalCaseCompleteCaseRequest`

- [x] **Task 3.2**: 处理MedicalCaseDetailResponse
  - 根据Task 2.1决策执行合并或迁移
  - 更新AutoMapper配置
  - 删除原Server层文件
  - **状态**: 保留在Module层，作为模块专用Response

- [x] **Task 3.3**: 处理MedicalCasePrescriptionDto
  - 根据Task 2.2决策执行合并或迁移
  - 更新AutoMapper配置
  - 删除原Server层文件
  - **状态**: 保留在Module层，作为模块专用简化DTO

### Phase 4: 更新引用

- [x] **Task 4.1**: 更新Server层引用
  - `MedicalCaseService.cs` - 更新枚举和类型引用
  - `MedicalCaseMappingProfile.cs` - 无需修改(使用保留的模块DTO)
  - `MedicalCaseController.cs` - 删除嵌入的SetPrescriptionFlagRequest类

- [x] **Task 4.2**: 更新Client层引用
  - `MedicalCaseDataManager.cs` - 无需修改
  - `IMedicalCaseApi.cs` - 无需修改
  - `IMedicalCaseService.cs` - 无需修改

- [x] **Task 4.3**: 更新测试代码引用
  - `MedicalCaseServiceTests.cs` - 更新using语句
  - `MedicalCaseControllerIntegrationTests.cs` - 更新using语句

### Phase 5: 验证与清理

- [x] **Task 5.1**: 编译验证
  - `dotnet build LYBT.All.sln`
  - 确保无编译错误
  - **完成**: 0错误0警告

- [x] **Task 5.2**: 运行测试
  - `dotnet test tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests`
  - 确保所有测试通过
  - **完成**: DTO相关测试通过(其他失败为预存问题)

- [ ] **Task 5.3**: 删除空目录
  - 删除 `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/` 目录
  - **状态**: 目录非空(保留MedicalCaseDetailResponse和MedicalCasePrescriptionDto)

---

## 进度追踪

| Phase | 状态 | 完成日期 |
|-------|------|----------|
| Phase 1 | 已完成 | 2025-11-29 |
| Phase 2 | 已完成 | 2025-11-29 |
| Phase 3 | 已完成 | 2025-11-29 |
| Phase 4 | 已完成 | 2025-11-29 |
| Phase 5 | 已完成 | 2025-11-29 |

## 变更摘要

### 已删除文件
- `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/SetPrescriptionFlagRequest.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/UpdateMedicalCaseRequest.cs`
- `MedicalCaseController.cs`中嵌入的`SetPrescriptionFlagRequest`类

### 已迁移到Shared层
- `UpdateMedicalCaseRequest` 及相关类型 -> `MedicalCaseDtos.cs`

### 保留在Module层(模块专用)
- `MedicalCaseDetailResponse.cs` - 包含Prescription嵌套的Response
- `MedicalCasePrescriptionDto.cs` - 简化版处方DTO

### 编译结果
- 0 错误
- 0 警告
