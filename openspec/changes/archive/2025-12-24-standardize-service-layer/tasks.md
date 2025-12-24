# Tasks: 标准化Desktop Service层架构

**Change ID**: standardize-service-layer
**Total Tasks**: 40
**Phases**: 7

---

## Phase 1: Herbs模块 (4 tasks)

### Task 1.1: 重命名HerbCommandHandler接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Interfaces/IHerbCommandHandler.cs`
- **操作**:
  - 重命名接口 `IHerbCommandHandler` → `IHerbService`
  - 重命名文件 → `IHerbService.cs`
- **验证**: 编译通过

### Task 1.2: 重命名HerbCommandHandler实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbCommandHandler.cs`
- **操作**:
  - 重命名类 `HerbCommandHandler` → `HerbService`
  - 更新接口实现 `: IHerbService`
  - 重命名文件 → `HerbService.cs`
- **验证**: 编译通过

### Task 1.3: 更新Herbs模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs`
- **操作**:
  - 更新DI注册 `IHerbCommandHandler, HerbCommandHandler` → `IHerbService, HerbService`
- **验证**: 编译通过

### Task 1.4: 更新Herbs模块ViewModel引用
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
  - 其他引用IHerbCommandHandler的文件
- **操作**:
  - 更新注入类型 `IHerbCommandHandler` → `IHerbService`
  - 更新字段名 `_commandHandler` → `_herbService`
- **验证**: 编译通过，功能正常

---

## Phase 2: Formula模块 (4 tasks)

### Task 2.1: 重命名FormulaCommandHandler接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Interfaces/IFormulaCommandHandler.cs`
- **操作**:
  - 重命名接口 `IFormulaCommandHandler` → `IFormulaService`
  - 重命名文件 → `IFormulaService.cs`
- **验证**: 编译通过

### Task 2.2: 重命名FormulaCommandHandler实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaCommandHandler.cs`
- **操作**:
  - 重命名类 `FormulaCommandHandler` → `FormulaService`
  - 更新接口实现 `: IFormulaService`
  - 重命名文件 → `FormulaService.cs`
- **验证**: 编译通过

### Task 2.3: 更新Formula模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/FormulaModule.cs`
- **操作**:
  - 更新DI注册 `IFormulaCommandHandler, FormulaCommandHandler` → `IFormulaService, FormulaService`
- **验证**: 编译通过

### Task 2.4: 更新Formula模块ViewModel引用
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
  - 其他引用IFormulaCommandHandler的文件
- **操作**:
  - 更新注入类型 `IFormulaCommandHandler` → `IFormulaService`
  - 更新字段名 `_commandHandler` → `_formulaService`
- **验证**: 编译通过，功能正常

---

## Phase 3: Consultation模块 (4 tasks)

### Task 3.1: 重命名ConsultationCommandHandler接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Interfaces/IConsultationCommandHandler.cs`
- **操作**:
  - 重命名接口 `IConsultationCommandHandler` → `IConsultationService`
  - 重命名文件 → `IConsultationService.cs`
- **验证**: 编译通过

### Task 3.2: 重命名ConsultationCommandHandler实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationCommandHandler.cs`
- **操作**:
  - 重命名类 `ConsultationCommandHandler` → `ConsultationService`
  - 更新接口实现 `: IConsultationService`
  - 重命名文件 → `ConsultationService.cs`
- **验证**: 编译通过

### Task 3.3: 更新Consultation模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ConsultationModule.cs`
- **操作**:
  - 更新DI注册 `IConsultationCommandHandler, ConsultationCommandHandler` → `IConsultationService, ConsultationService`
- **验证**: 编译通过

### Task 3.4: 更新Consultation模块ViewModel引用
- **文件**: 相关ViewModel文件
- **操作**:
  - 更新注入类型和字段名
- **验证**: 编译通过，功能正常

---

## Phase 4: Patients模块合并重构 (8 tasks)

### Task 4.1: 分析PatientCommandHandler职责
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientCommandHandler.cs`
- **操作**:
  - 识别CRUD方法：CreateAsync, UpdateAsync, DeleteAsync, GetByIdAsync, GetPatientsPagedAsync
  - 识别UI命令：SaveCommand, EditCommand, DeleteCommand等（需移至ViewModel）
  - 记录与StateManager的交互方式
- **输出**: 合并计划

### Task 4.2: 分析PatientStateManager职责
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientStateManager.cs`
- **操作**:
  - 识别状态属性：CurrentPatient, HasChanges, IsLoading, IsNewPatient
  - 识别与Repository的直接交互
  - 确认与CommandHandler的重叠部分
- **输出**: 合并计划

### Task 4.3: 创建IPatientService接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientService.cs`
- **操作**:
  - 定义CRUD方法签名
  - 定义状态属性（如需要）
  - 使用标准(bool, Data?, Error?)返回元组
- **验证**: 编译通过

### Task 4.4: 创建PatientService实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PatientService.cs`
- **操作**:
  - 合并CommandHandler的CRUD逻辑
  - 合并StateManager的状态管理逻辑
  - 移除UI命令（SaveCommand等）
  - 统一返回格式
- **验证**: 编译通过

### Task 4.5: 删除PatientCommandHandler
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientCommandHandler.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientCommandHandler.cs`
- **操作**:
  - 删除类和接口文件
- **验证**: 编译通过

### Task 4.6: 删除PatientStateManager
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientStateManager.cs`
- **操作**:
  - 删除类文件
- **验证**: 编译通过

### Task 4.7: 更新Patients模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`
- **操作**:
  - 移除CommandHandler和StateManager注册
  - 添加PatientService注册
- **验证**: 编译通过

### Task 4.8: 更新ViewModel引用并移入UI命令
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
  - 其他相关ViewModel
- **操作**:
  - 注入IPatientService替代原有依赖
  - 将SaveCommand/EditCommand等UI命令移入ViewModel
  - 更新命令执行逻辑调用PatientService
- **验证**: 编译通过，功能正常

---

## Phase 5: Users模块 (4 tasks)

### Task 5.1: 重命名UserCommandHandler接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserCommandHandler.cs`
- **操作**:
  - 重命名接口 `IUserCommandHandler` → `IUserService`
  - 重命名文件 → `IUserService.cs`
- **验证**: 编译通过

### Task 5.2: 重命名UserCommandHandler实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Services/UserCommandHandler.cs`
- **操作**:
  - 重命名类 `UserCommandHandler` → `UserService`
  - 更新接口实现 `: IUserService`
  - 重命名文件 → `UserService.cs`
- **验证**: 编译通过

### Task 5.3: 更新Users模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/UsersModule.cs`
- **操作**:
  - 更新DI注册 `IUserCommandHandler, UserCommandHandler` → `IUserService, UserService`
- **验证**: 编译通过

### Task 5.4: 更新Users模块ViewModel引用
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`
  - 其他引用IUserCommandHandler的文件
- **操作**:
  - 更新注入类型 `IUserCommandHandler` → `IUserService`
  - 更新字段名 `_commandHandler` → `_userService`
- **验证**: 编译通过，功能正常

---

## Phase 6: MedicalCase模块 (8 tasks)

### Task 6.1: 重命名MedicalCaseAggregateService接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseAggregateService.cs`
- **操作**:
  - 重命名接口 `IMedicalCaseAggregateService` → `IMedicalCaseService`
  - 重命名文件 → `IMedicalCaseService.cs`
- **验证**: 编译通过

### Task 6.2: 重命名MedicalCaseAggregateService实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseAggregateService.cs`
- **操作**:
  - 重命名类 `MedicalCaseAggregateService` → `MedicalCaseService`
  - 更新接口实现 `: IMedicalCaseService`
  - 重命名文件 → `MedicalCaseService.cs`
- **验证**: 编译通过

### Task 6.3: 分析MedicalCaseCommandHandler职责
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseCommandHandler.cs`
- **操作**:
  - 分析与MedicalCaseService的职责重叠
  - 确定合并策略
- **输出**: 合并计划文档

### Task 6.4: 合并MedicalCaseCommandHandler到MedicalCaseService
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseCommandHandler.cs`
- **操作**:
  - 将CommandHandler中非重复的方法合并到Service
  - 删除MedicalCaseCommandHandler.cs
  - 删除IMedicalCaseCommandHandler接口
- **验证**: 编译通过

### Task 6.5: 更新MedicalCase模块注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`
- **操作**:
  - 更新DI注册
  - 移除MedicalCaseCommandHandler注册
- **验证**: 编译通过

### Task 6.6: 更新MedicalCase ViewModel引用
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
  - 其他相关文件
- **操作**:
  - 更新注入类型 `IMedicalCaseAggregateService` → `IMedicalCaseService`
  - 更新字段名 `_aggregateService` → `_medicalCaseService`
- **验证**: 编译通过，功能正常

### Task 6.7: 更新跨模块引用
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/` 相关文件
  - 其他引用MedicalCaseAggregateService的模块
- **操作**:
  - 更新所有跨模块引用
- **验证**: 编译通过

### Task 6.8: 验证MedicalCase功能完整性
- **操作**:
  - 测试医案创建流程
  - 测试医案编辑流程
  - 测试处方操作
  - 测试诊断操作
- **验证**: 所有功能正常

---

## Phase 7: 验证与文档 (8 tasks)

### Task 7.1: 全量编译验证
- **操作**:
  - `dotnet build LYBT.All.sln -c Release`
  - 确保0 errors, 0 warnings
- **验证**: 编译成功

### Task 7.2: 全局搜索遗漏引用
- **操作**:
  - 搜索 `CommandHandler` 关键字
  - 搜索 `AggregateService` 关键字
  - 搜索 `StateManager` 关键字
  - 确认无遗漏
- **验证**: 无遗漏引用

### Task 7.3: 更新架构文档
- **文件**: `docs/reference/architecture/desktop-service-layer.md`
- **操作**:
  - 更新Service层架构说明
  - 更新接口设计规范
  - 添加最佳实践指南
- **验证**: 文档完整

### Task 7.4: 更新CLAUDE.md
- **文件**: `CLAUDE.md`
- **操作**:
  - 更新Desktop架构描述
  - 移除CommandHandler/AggregateService/StateManager术语
  - 使用标准Service术语
- **验证**: 文档一致

### Task 7.5: 更新enhance-dataflow-logging提案
- **文件**: `openspec/changes/enhance-dataflow-logging/`
- **操作**:
  - 更新日志前缀规范
  - [CMD]/[AGG]/[STATE] → [SVC]
  - 更新任务列表
- **验证**: 提案一致

### Task 7.6: 运行单元测试
- **操作**:
  - 运行所有Desktop相关测试
  - 确保无回归
- **验证**: 测试通过

### Task 7.7: 创建Memory记录
- **操作**:
  - 保存架构决策到Graphiti
  - 记录重构原因和结果
- **验证**: Memory已保存

### Task 7.8: 提交并归档
- **操作**:
  - 提交所有变更
  - 归档OpenSpec
  - 更新CHANGELOG
- **验证**: 归档完成

---

## 依赖关系

```
Phase 1 (Herbs) ─────┐
Phase 2 (Formula) ───┼──→ Phase 7 (验证与文档)
Phase 3 (Consultation)┼
Phase 4 (Patients) ──┼
Phase 5 (Users) ─────┘
        ↓
Phase 6 (MedicalCase) ──→ Phase 7 (验证与文档)
```

**说明**:
- Phase 1-5 可并行执行（各模块独立）
- Phase 6 依赖前5个Phase（MedicalCase可能引用其他模块）
- Phase 7 必须在所有Phase完成后执行

---

## 回滚策略

如遇到无法解决的问题：

1. **单模块回滚**: 使用git revert回滚特定模块的提交
2. **全量回滚**: 回滚到重构前的分支

**建议**: 每个Phase完成后创建一个检查点提交，便于定点回滚
