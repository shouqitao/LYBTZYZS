# 任务 A-31-C3b：Herbs+Formula → Catalog 模块合并（Server 端）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-09
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §2.1 合并 1 + S2/S3 审查报告
> 用户决策（2026-08-09）：从三者关系确认——药材是基础主数据，验方是药材的模板组合（同域），处方是实例+收费（MedicalCase 域不参与）；三种处方来源最终都是 PrescriptionItem 引用药材库，不影响合并
> **⚠️ T2 方案调整——先文档化再改代码**

## 任务

将 `LYBT.Module.Herbs` + `LYBT.Module.Formula` 合并为 `LYBT.Module.Catalog`（药材方剂域），消除跨模块依赖和系统性重复。

## 范围

- ✅ Server `LYBT.Module.Herbs/` + `LYBT.Module.Formula/` → 合并为 `LYBT.Module.Catalog/`
- ✅ Shared 实体：`Herbs/HerbModel` + `Formulas/FormulaModel` + `Formulas/FormulaHerbItem` → 合并到 Catalog 实体
- ✅ Shared 枚举：`FormulaType` / `FormulaValidationStatus` / `DecocteMethod` → 保留（不改名）
- ✅ DbContext：`HerbsDbContext` → 升级为 `CatalogDbContext`（含 Herb + Formula + FormulaHerbItem 三个 DbSet）
- ❌ 排除：**Prescription/PrescriptionItem 不动**（归 MedicalCase 域）
- ❌ 排除：Desktop 侧合并（后续批次 C-3c）
- ❌ 排除：其他模块

## 关键约束

| 约束 | 内容 |
|------|------|
| **三者关系不变** | Herb（药材）/ Formula（验方）/ Prescription（处方）的数据关系不变 |
| **Prescription 不动** | PrescriptionItem.HerbId 引用药材库的关系不变（合并后 Catalog 实体引用不变）|
| **FormulaHerbItem 延迟绑定** | HerbId 可空 + OriginalHerbName + IsValidated 机制保留 |
| **HerbsDbContext 升级** | 已含 FormulaHerbItem/Formula 配置（同库同连接），迁移链连续 |
| **跨模块引用更新** | MedicalCase 引用 Herbs → Catalog（命名空间改写）|
| **路由保持** | `/api/herbs/*` + `/api/formulas/*` 路由不变 |
| **0 错误 0 警告** | `dotnet build LYBTZYZS.sln --no-incremental` |
| **架构测试** | `dotnet test tests/LYBT.Tests.Architecture/` 全绿 |

## 分阶段执行

### 阶段 A：Catalog 骨架 + 实体合并

1. 新建 `LYBT.Module.Catalog/` 项目
2. 实体迁移：Herb + Formula + FormulaHerbItem → Catalog/Entities/（保持表名不变）
3. 枚举保留：FormulaType / FormulaValidationStatus / DecocteMethod 不改名
4. CatalogDbContext：HerbsDbContext 升级（保留现有配置，命名空间改写）
5. sln 更新
6. 验证：build 0/0

### 阶段 B：接口+Command/Handler 迁移

1. 接口迁移：IHerbRepository/IFormulaRepository → ICatalogRepository（泛型化）
2. IHerbCrossModuleService → ICatalogService（保留对外接口，MedicalCase/Registration 消费）
3. Command/Handler 迁移：Herbs 8 + Formula 7 = 15 → 同构泛型化（~8-9）
4. Service 迁移：HerbService + FormulaService → CatalogService（同构合并）
5. Mapper 合并：HerbMapper + FormulaMapper → CatalogMapper
6. Validator 合并：HerbsValidator + FormulaValidator → CatalogValidator
7. DI 注册：CatalogModule.cs（AddCatalogModule）
8. 验证：build 0/0

### 阶段 C：外部引用更新 + 清理

1. MedicalCase/Registration：IHerbCrossModuleService → ICatalogService（using 改名）
2. WebAPI Controllers：HerbsController + FormulasController → CatalogController（路由保持）
3. LocalWebAPI 同步
4. 删除旧项目：LYBT.Module.Herbs/ + LYBT.Module.Formula/
5. sln 移除旧项目
6. 测试更新
7. 验证：build 0/0 + 架构测试全绿

## 产出

- 3 个阶段独立 commit + push
- 报告：`docs/compose/reports/a31-c3b-herbs-formula-merge.md`
