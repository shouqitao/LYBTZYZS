# Tasks for standardize-module-structure

## Phase 1: Consultation模块

- [x] TASK-1: 重命名 `Components/` → `Services/`
- [x] TASK-2: 更新 `ConsultationCommandHandler.cs` namespace
- [x] TASK-3: 更新 `ConsultationDataManager.cs` namespace
- [x] TASK-4: 更新 `ConsultationValidator.cs` namespace
- [x] TASK-5: 更新所有引用该namespace的文件

## Phase 2: Herbs模块

- [x] TASK-6: 重命名 `Components/` → `Services/`
- [x] TASK-7: 更新 `HerbDataManager.cs` namespace
- [x] TASK-8: 更新所有引用该namespace的文件

## Phase 3: MedicalCase模块

- [x] TASK-9: 重命名 `Components/` → `Services/`
- [x] TASK-10: 更新 `MedicalCaseCommandHandler.cs` namespace
- [x] TASK-11: 更新 `MedicalCaseDataManager.cs` namespace
- [x] TASK-12: 更新 `MedicalCaseEventCoordinator.cs` namespace
- [x] TASK-13: 更新 `MedicalCaseNavigationHandler.cs` namespace
- [x] TASK-14: 更新 `MedicalCaseStatusPresenter.cs` namespace
- [x] TASK-15: 更新 `MedicalCaseValidator.cs` namespace
- [x] TASK-16: 更新所有引用该namespace的文件

## Phase 4: Users模块

- [x] TASK-17: 重命名 `Components/` → `Services/`
- [x] TASK-18: 更新 `UserDataManager.cs` namespace
- [x] TASK-19: 更新 `UserValidator.cs` namespace
- [x] TASK-20: 更新所有引用该namespace的文件

## Phase 5: Patients模块

- [x] TASK-21: 合并 `Components/` 到 `Services/`
- [x] TASK-22: 更新 `ExcelParserService.cs` namespace（如在Components中）
- [x] TASK-23: 更新所有引用该namespace的文件

## Phase 6: 验证

- [x] TASK-24: 执行 `dotnet build LYBT.Desktop.sln` 验证编译
- [x] TASK-25: 运行单元测试验证功能 (预先存在的测试问题不影响此次重构)
- [x] TASK-26: 提交变更到Git (commit: 94fa5de8f)

## Summary

| Phase | 模块 | 操作 | 文件数 |
|-------|------|------|--------|
| 1 | Consultation | Components → Services | 3 |
| 2 | Herbs | Components → Services | 1 |
| 3 | MedicalCase | Components → Services | 6 |
| 4 | Users | Components → Services | 2 |
| 5 | Patients | Components → Services (合并) | 1 |
| 6 | 验证 | 编译 + 测试 | - |

## Validation Criteria

1. 所有模块使用统一的 `Services/` 文件夹命名
2. 所有namespace与文件路径一致
3. `dotnet build LYBT.Desktop.sln` 成功
4. 现有单元测试通过
