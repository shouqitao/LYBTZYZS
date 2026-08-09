# A-31-C3b：Herbs+Formula → Catalog 模块合并报告

> **任务**：`docs/compose/specs/task-a31-c3b-herbs-formula-merge-2026-08-09.md`（T2 方案调整，先文档后代码）
> **执行**：Mimo Code | **日期**：2026-08-09 | **分支**：master
> **依据**：`docs/03-architecture/15-solution-integration-plan.md` §2.1 合并 1 + 蓝图 §2.2 模块表（已更新为 Catalog）

---

## 1. 执行摘要

将 `LYBT.Module.Herbs` + `LYBT.Module.Formula` 合并为 `LYBT.Module.Catalog`（药材方剂目录域），消除跨模块依赖与系统性重复（42 行 Service 孪生、5 对同构 CRUD/Toggle Handler、双份仓储/映射/验证器）。按 3 阶段独立验证 + 独立 commit + push 完成。

| 阶段 | 内容 | Commit | 验证 |
|------|------|--------|------|
| A | Catalog 骨架 + CatalogDbContext（HerbsDbContext 升级）+ sln | `bfc90a738` | build 0/0 |
| B | 接口迁移 + Command 泛型化 + Service/Mapper/Validator 合并 + AddCatalogModule | `76bcbb38f` | build 0/0 |
| C | 外部引用更新 + 双端 CatalogController + 删旧项目 + 测试更新 | `5b94893f5` | build 0/0 + 架构测试 86/86 |

---

## 2. 阶段 A：Catalog 骨架 + CatalogDbContext

- 新建 `src/Server/Modules/LYBT.Module.Catalog/`（csproj 引用 LYBT.Entities/Shared.Models/Infrastructure + InternalsVisibleTo LYBT.Tests.Server）
- `Infrastructure/CatalogDbContext.cs`：HerbsDbContext 升级——保留全部引用检查 DbSet（PrescriptionItems/Prescriptions/MedicalCases/Patients/FormulaHerbItems）+ 新增 `DbSet<Formula> Formulas`，配置复用 Infrastructure `FormulaConfiguration`/`FormulaHerbItemConfiguration`（表名/长度/索引/软删过滤器逐项保留）
- sln 加入（嵌套于 Server.BusinessModules 组，与 Herbs/Formula 同组）

**设计决策（偏离任务书字面，架构硬约束，C3a 同构先例）**：
- **实体不物理移动**：任务书 A2 要求 `Herb`/`Formula`/`FormulaHerbItem` → `Catalog/Entities/`。但实体位于 `Shared/LYBT.Entities/`（Herbs/ + Formulas/），且 **AppDbContext（Infrastructure/Core）是迁移链所有者**（迁移快照内嵌 `LYBT.Entities.Herbs.Herb` 等全限定名），Desktop 侧 LocalWebApiSeedData/LocalDbContext 亦直接消费实体；物理移入 Server 模块将产生 Core→Modules 反向依赖 + Client→Server 跨层引用，违反依赖方向并破坏迁移链。故实体保留 Shared，命名空间/表名/属性全部不变（与 C3a 对 ApplicationUser/AuthSession 的处理一致，蓝图 §2.4「实体统一下沉 LYBT.Entities」定案）。

## 3. 阶段 B：接口迁移 + Command 泛型化 + 合并

**接口迁移（→ Catalog/Interfaces/）**：
- `ICatalogRepository<TEntity>`（泛型契约，含 GetByIdIncludingDeletedAsync/GetPagedAsync/ExistsByNameAsync）+ `CatalogRepositoryBase<TEntity>`（BaseRepository<,CatalogDbContext> 基类）
- `IHerbRepository : ICatalogRepository<Herb>`（+GetByNameAsync）/ `IFormulaRepository : ICatalogRepository<Formula>`（+FindWithHerbsAsync）——实体特有分页关键字（拼音/功效）与排序保留
- `IHerbReferenceRepository`（引用检查，5 方法原样迁移）
- `ICatalogQueryService<TListDto, TDetailDto>`（只读查询，**不暴露实体类型**——P01b UI 层禁依赖 Entities）

**Command 泛型化（10 对 → 7 文件）**：
- `CatalogCommands.cs` 5 个泛型记录：CreateEntityCommand / UpdateEntityCommand / DeleteEntityCommand<TEntity> / RestoreEntityCommand<TEntity,TDetail> / ToggleEntityStatusCommand<TEntity,TDetail>
- `HerbCommandHandler` / `FormulaCommandHandler`：各实现 5 个 `IRequestHandler<>`（合并原 Create/Update/Delete/Restore/Toggle 同构 Handler，错误码/UpdateProfile 差异保留）
- Batch 6 对：BatchDeleteHerbs/Formulas（原样迁入）+ **BatchEnable/BatchDisable×2 统一收敛至 `BatchOperationHandlerBase`**（任务书要求；基类钩子覆盖 EntityNotFoundMessage/OperationName/GetEntityName，消息格式与行为等价验证）
- BatchImportHerbs/Formulas（批处理第二模板，A-29 P2-12 定案独立实现）+ ValidateFormulaHerb 原样迁入

**Service 合并（消除 42 行孪生）**：`CatalogQueryService<TEntity,TListDto,TDetailDto>`（internal）合并 HerbService/FormulaService，仓储/映射委托/错误码由 DI 工厂注入，注册为 `ICatalogQueryService<HerbListDto,HerbDetailDto>` + `ICatalogQueryService<FormulaListDto,FormulaDetailDto>`

**Mapper 合并**：`CatalogDtoMapper`（Mapperly，Herb 映射 ToHerbListDto/ToHerbDetailDto + Formula 映射 ToFormulaListDto/ToFormulaDetailDto/ToHerbItemDto 合一，工厂方法保留手写）

**Validator 合并**：14 验证器迁入 `Catalog/Application/Validators/`（命名空间 LYBT.Module.Catalog），泛型命令类型适配

**CatalogModule.cs**：`AddCatalogModule` 注册全部（DbContext/仓储/ICatalogService/CatalogQueryService×2/MediatR/Validators/ValidationBehavior）

**设计决策（跨模块接口，P07 守卫，C3a 同构先例）**：
- `ICatalogService`（改名自 `IHerbCrossModuleService`）**保留在 `Infrastructure/Services/CrossModule/`**（Phase C 落地改名），未移入 Catalog 模块。原因：MedicalCase 消费它，若接口在 Catalog 模块则 MedicalCase 必须 ProjectReference Catalog → 违反 P07；蓝图 §2.1 定案跨模块接口位于 Infrastructure。
- 验方 2 个 Handler（BatchImportFormulas/ValidateFormulaHerb）继续注入 `ICatalogService`（合并后为模块内自用，方法不变）。

## 4. 阶段 C：外部引用更新 + 清理

- **IHerbCrossModuleService → ICatalogService**：Infrastructure 接口文件改名（4 方法不变），`CatalogCrossModuleService` 实现；**MedicalCase `PrescriptionItemService` 仅改类型名**（using 不变，方法不变）；Catalog 模块内 2 个验方 Handler 同步改名；注：任务书表述"MedicalCase/Registration 消费"——**实际仅 MedicalCase 消费**（Registration 无引用，勘查确认）
- **WebAPI**：HerbsController + FormulasController → **CatalogController**（类路由 `api/v{version:apiVersion}/herbs` + 验方端点绝对路由 `api/v{version:apiVersion}/formulas/*`，IdentityController 先例；`/api/v1/herbs/*` + `/api/v1/formulas/*` 路由保持）；ServiceCollectionExtensions 切换 AddCatalogModule
- **LocalWebAPI**：同步合并（含 Clone 端点 `api/v1/formulas/{id}/clone`、验方 BatchImport 原始 List 体），LocalWebApiProgram 切换 AddCatalogModule
- **删除旧项目**：LYBT.Module.Herbs/ + LYBT.Module.Formula/ 整目录（-81 文件）+ sln 移除 + WebAPI/LocalWebAPI/测试 csproj 引用切换
- **测试更新**：HerbRepositoryTests（HerbsDbContext→CatalogDbContext）；架构测试 TestAssemblies/P05d/P06/P07/P09/P19/P19b/P21 名单更新（Herbs/Formulas→Catalog，控制器名单→CatalogController，P19 移除 IHerbService/IFormulaService）
- **P01b 适配**：CatalogController 加入 UI 禁依赖 Entities 例外清单（泛型命令的实体幻影类型参数 `DeleteEntityCommand<Herb>` 等，仅方法体构造；与 PatientsController 等同型例外）

## 5. 验证结果（真实输出）

| 验证项 | 结果 |
|--------|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告**（三阶段均验证） |
| `dotnet test tests/LYBT.Tests.Architecture/` | **86/86 全绿**（P01b 例外适配后） |
| `dotnet test tests/LYBT.Tests.Server/ --filter HerbRepository\|FormulaModel\|HerbInputDtoValidator\|FormulaInputDtoValidator` | **70/70 通过**（CatalogDbContext 仓储 + 实体 + 验证器） |

**HEAD 基线对比**（git stash 验证，证明非本次引入）：
- Desktop `HerbsControllerTests`/`FormulasControllerTests`（LocalWebAPI）12/12 失败：测试打 `/api/herbs`（无版本前缀）与控制器路由 `api/v1/herbs` 不匹配——**HEAD 基线（旧控制器）同样 12/12 失败**，与 C3a 报告记录的 `UsersControllerTests`（/api/users 路径不匹配）同型既有缺陷，非本次合并引入，未在本次范围修复

## 6. 关键约束达成

| 约束 | 达成 |
|------|------|
| 三者关系不变（药材/验方/处方） | ✅ Prescription/PrescriptionItem 未触碰 |
| PrescriptionItem.HerbId 引用药材库关系不变 | ✅ 实体/表/命名空间全部不变 |
| FormulaHerbItem 延迟绑定机制保留 | ✅ HerbId 可空 + OriginalHerbName + IsValidated 原样 |
| CatalogDbContext 含 Herbs/Formula/FormulaHerbItem | ✅ 同库同连接，无迁移障碍（表由 AppDbContext 迁移链管理） |
| 枚举 FormulaType/FormulaValidationStatus/DecocteMethod 不改名 | ✅ 未触碰 |
| 路由 /api/v1/herbs/* + /api/v1/formulas/* 保持 | ✅ CatalogController 双路由（绝对路由先例） |
| IHerbCrossModuleService→ICatalogService 对外接口保留 | ✅ MedicalCase 仅改类型名（实际消费方仅 MedicalCase） |
| 0 错误 0 警告 | ✅ |
| 架构测试全绿 | ✅ 86/86 |

## 7. 文档更新

- `docs/03-architecture/13-project-master-plan.md`：A-31 行追加 C-3b 完成记录（3 commit SHA + 验证 + 报告）
- `docs/03-architecture/15-solution-integration-plan.md`：合并 1 状态 → 已完成 ✅，批次表 C-3b → ✅ 已完成
- 蓝图 `14-structure-design-blueprint.md` §2.2 模块表已由 Hermes 更新（LYBT.Module.Catalog 行）

## 8. 遗留/后续

- **Desktop 侧合并（LYBT.Desktop.Herbs + LYBT.Desktop.Formula → LYBT.Desktop.Catalog）**：任务书明确排除为后续批次 C-3c，本批次未动
- Desktop LocalWebAPI 控制器测试的 `/api/herbs` 路径不匹配（HEAD 既有缺陷）不在本任务范围，后续可修正
- 任务书"MedicalCase/Registration 消费"中的 Registration 实际无引用（勘查确认），报告如实记录

---

*报告完成。全部验证基于真实 build/test 输出。*
