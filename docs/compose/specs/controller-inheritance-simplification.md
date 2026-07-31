---
feature: controller-inheritance-simplification
status: designed
updated: 2025-07-31
---

# Controller 继承体系简化

## [S1] Problem

Server 端 Controller 继承体系存在两个 P0 设计问题：

1. **MedicalCase 路由冲突**：`MedicalCasesController` 和 `MedicalCaseProcessingController` 共享 `/api/v{version:apiVersion}/medicalcases` 路由前缀，导致路由歧义。LocalWebAPI 已合并为一个 Controller，证明拆分是过度设计。

2. **BaseCrudController 抽象边界过高**：7 个抽象方法中 `ToggleStatus` 和 `Restore` 不是所有实体都需要的操作。Registration 模块 5/7 个方法被 override 为 "不支持"，违反 Liskov 替换原则。

## [S2] Solution

### S2.1 MedicalCase Controller 合并

将 `MedicalCaseProcessingController` 的 4 个端点合并回 `MedicalCasesController`，删除 `MedicalCaseProcessingController`。

合并后的 `MedicalCasesController` 端点清单：
- `GET /` — 分页查询（继承 + override 添加 OutputCache）
- `GET {id}` — 详情
- `POST /` — 创建
- `PUT {id}` — 保存（聚合根）
- `DELETE {id}` — 删除（软删除）
- `POST batch-delete` — 批量删除
- `PUT {id}/prescription-flag` — 处方标记
- `PUT {id}/print-completed` — 打印记录
- `PUT {id}/status` — 状态流转（从 ProcessingController 合入）
- `PUT {id}/close` — 关闭医案（从 ProcessingController 合入）
- `PUT {id}/suspend` — 挂起医案（从 ProcessingController 合入）
- `PUT {id}/cancel` — 取消医案（从 ProcessingController 合入）

MediatR Commands 不变，只是 Controller 层合并。

### S2.2 BaseCrudController 拆分

将 `BaseCrudController` 拆分为两层：

```
BaseApiController
└── BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>
    │  5 个方法: GetList, GetById, Create, Update, Delete, BatchDelete
    │  6 个抽象方法: CreateGetListQuery, CreateCreateCommand, CreateUpdateCommand,
    │               CreateDeleteCommand, CreateBatchDeleteCommand + GetById(abstract)
    │
    └── BaseSoftDeleteCrudController<TListDto, TDetailDto, TInputDto, TQuery>  (新增)
        │  新增 2 个方法: ToggleStatus, Restore
        │  新增 2 个抽象方法: CreateToggleStatusCommand, CreateRestoreCommand
        │
        ├── HerbsController
        ├── FormulasController
        ├── PatientsController
        └── MedicalCasesController
```

继承者变更：
- `HerbsController`：`BaseCrudController` → `BaseSoftDeleteCrudController`
- `FormulasController`：`BaseCrudController` → `BaseSoftDeleteCrudController`
- `PatientsController`：`BaseCrudController` → `BaseSoftDeleteCrudController`
- `BaseMedicalCasesController`：`BaseCrudController` → `BaseSoftDeleteCrudController`
- `BaseUsersController`：不变（直接继承 BaseCrudController，已有 BatchEnable/BatchDisable）
- `BaseRegistrationsController`：不变（继续继承 BaseCrudController，override 不需要的方法）

## [S3] Out of Scope

- Registration 模块的继承关系重构（留待后续评估）
- Server/LocalWebAPI 额外端点重复（batch-enable/disable 等）
- batch-enable/disable 与 ToggleStatus 语义统一
- BaseApiController 所有权检查职责下沉

## Tasks

- [ ] T1: 合并 MedicalCaseProcessingController 到 MedicalCasesController — acceptance: 删除 MedicalCaseProcessingController.cs，MedicalCasesController 包含 status/close/suspend/cancel 端点，`dotnet build` 通过（covers: S2.1）
- [ ] T2: 创建 BaseSoftDeleteCrudController — acceptance: 新增 BaseSoftDeleteCrudController.cs，包含 ToggleStatus + Restore 方法和对应抽象方法，BaseCrudController 移除这两个抽象（covers: S2.2）
- [ ] T3: 迁移 Herbs/Formula/Patients/MedicalCases 继承到 BaseSoftDeleteCrudController — acceptance: 4 个 Controller 改继承，`dotnet build` 通过（covers: S2.2）
