# 任务 A-31-C8：决策点 2/4 收敛（门面单轨 + 映射统一）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §六 决策点 2/4 + S2/S3 审查报告
> 用户决策（2026-08-08）：按技术总监推荐执行——**决策点 2 = 删统一门面（只留域接口）；决策点 4 = 直用 DTO（删手写扩展）**
> **⚠️ T2 方案调整——先文档化（蓝图 §2.1/§3.5 已更新）再改代码**

## 任务

收敛两个"多方案并存"机制为单方案（与密码/日志/异常收敛同性质）。

## 范围

- ✅ Server `LYBT.Infrastructure/Services/CrossModule/`（决策点 2）
- ✅ Desktop `Shared.Models/Extensions/DtoConversionExtensions.cs` + MedicalCase 模块（决策点 4）
- ❌ 排除：C-3/C-4 合并（延期）、其他机制
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-7 完成后 `130e491bc`）

---

## C8-1：跨模块门面单轨化（决策点 2 = 方案 A：删统一门面）

**现状**：
- `ICrossModuleService`（统一门面，~20 方法）与 6 个域接口（IPatient/IHerb/IUser/IAuth/IMedicalCase/IRegistrationCrossModuleService）**并存**
- 同一方法（如 `GetPatientBasicInfoAsync`）在统一门面和域接口各有一份
- 消费方混用两套：有的注入 `ICrossModuleService`，有的注入域接口

**动作**：
1. **删 `ICrossModuleService` 统一门面**（接口 + `CrossModuleService` 实现 + `CrossModuleServiceExtensions` 注册调整）
2. **消费方迁移**（9 处注入 `ICrossModuleService` → 按方法归属改注入对应域接口）：
   - `LoginCommandHandler` → `IUserCrossModuleService`（VerifyPassword/GetUserByUsername）
   - `BatchImportFormulasCommandHandler` / `ValidateFormulaHerbCommandHandler` → `IHerbCrossModuleService`（GetHerbPrices/GetDisabledHerbIds）
   - `MedicalCaseCommandService` / `MedicalCaseServiceHelper` / `MedicalCaseStateService` / `PrescriptionItemService` → 按方法归属拆（IPatient/IHerb/IUser）
   - `QuickVisitCommandHandler` → `IRegistrationCrossModuleService` + `IPatientCrossModuleService`
3. **域接口补全**：统一门面中"域接口没有的方法"补进对应域接口（如 `GetAllActiveHerbsAsync` → IHerbCrossModuleService）
4. **实现合并**：`CrossModuleService` 中被各域接口已有实现覆盖的方法删除；域接口缺的实现补进对应域 Service

**验收**：`ICrossModuleService` 全仓 0 残留；消费方全部注入域接口；build 0 错误 0 警告；架构测试全绿。

## C8-2：映射统一（决策点 4 = 方案 A：直用 DTO，删手写扩展）

**现状**：
- `DtoConversionExtensions`（Shared.Models/Extensions，3 个手写方法：ToInputDto×2/ToPrescriptionInputDto）**唯一活跃调用点** `MedicalCaseCommandService.cs:46`（Desktop workspace 保存链路）
- 全仓其他映射已用 Mapperly（15 个 Mapper 文件）

**动作**：
1. **`MedicalCaseCommandService.cs:46` 改走 Mapperly**：新建/复用 `MedicalCaseDetailModelMapper`（已有：`MedicalCaseDetailModelMapper.cs:157` 医案管理保存路径已用 Mapperly——统一这条 workspace 保存路径）
2. **删 `DtoConversionExtensions`**（3 个手写方法 + 文件 + 相关 using）
3. **连带消费方清理**：`ConsultationMapper`/`PrescriptionMapper`/`FormulaDetailModelMapper`/`ConsultationItem`/`PrescriptionItemViewModel`/`MedicalCaseCommandsViewModel` 中引用 `DtoConversionExtensions` 的代码改走 Mapperly 或直用 DTO
4. **确认无残留**：`ToInputDto`/`ToPrescriptionInputDto`/`DtoConversionExtensions` 全仓 0 引用

**验收**：`DtoConversionExtensions` 全仓 0 残留；workspace 保存链路行为等价（测试覆盖）；build 0 错误 0 警告；架构测试全绿。

---

## 前置（文档先行，T2 流程——已由技术总监完成）

1. **蓝图 `14-structure-design-blueprint.md` §2.1 接口矩阵**：`ICrossModuleService` 移除，记录"跨模块取数统一走 `IXxxCrossModuleService` 域接口" ✅（已改）
2. **蓝图 §3.5**：Desktop 映射规则更新为"直用 DTO + Mapperly 唯一，禁止手写映射扩展" ✅（已改）
3. **总账 §九**：决策行追加（技术总监完成）

## 硬性约束

1. **Surgical Changes**：只改涉及文件
2. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
3. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿
4. **分 2 项独立 commit + push**：
   - `refactor(server): A-31-C8-1 跨模块门面单轨化（删 ICrossModuleService 统一门面，消费方改域接口）`
   - `refactor(desktop): A-31-C8-2 映射统一（workspace 保存走 Mapperly，删 DtoConversionExtensions 手写扩展）`
5. 产出报告：`docs/compose/reports/a31-c8-decision-points-convergence.md`（每项动作/验证/残留检查）

## 明确不做（防发散）

- ❌ 不执行项目合并（C-3/C-4 延期）
- ❌ 不碰 D72 extern、US-CARD-002（后续完善）
- ❌ 不删 6 个域接口（那是保留方案）
- ❌ 不重写 Mapperly 全部 Mapper（只删手写扩展，Mapperly 已用不动）
