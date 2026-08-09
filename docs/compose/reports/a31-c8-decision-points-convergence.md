# A-31-C8 决策点 2/4 收敛 — 交付报告

> 版本：v1.0 | 日期：2026-08-09
> 任务书：`docs/compose/specs/task-a31-c8-decision-points-convergence-2026-08-08.md`
> 基线：`7e7089cdd`（C-7 文档先行完成后）
> 分支：`master`｜远端：Gitee

---

## 摘要

收敛两个"多方案并存"机制为单方案（与密码/日志/异常收敛同性质）：

- **C8-1（决策点 2 = 删统一门面）**：删除 `ICrossModuleService` 统一门面（接口 + 实现 + 注册扩展），跨模块通信唯一走 6 个域接口 `IXxxCrossModuleService`。
- **C8-2（决策点 4 = 直用 DTO）**：Desktop workspace 保存链路改走 Mapperly，删除手写映射扩展 `DtoConversionExtensions`。

两项独立执行、独立验证、独立 commit + push。

---

## C8-1 跨模块门面单轨化（决策点 2）

### 动作

**1. 删除统一门面（3 文件）**

| 文件 | 处置 |
|------|------|
| `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ICrossModuleService.cs` | 删除 |
| `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/CrossModuleService.cs` | 删除（纯转发实现，无独有逻辑） |
| `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/CrossModuleServiceExtensions.cs` | 删除（含 `AddCrossModuleService` 注册） |

**2. 组合根注册调整（2 处）**

| 文件 | 变更 |
|------|------|
| `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs` | 删除 `services.AddCrossModuleService()` |
| `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | 删除 `builder.Services.AddCrossModuleService()` |

> 6 个域接口的 DI 注册本就存在于各模块 `*Module.cs`（`AddScoped<IXxxCrossModuleService, ...>`），无需新增。

**3. 消费方迁移（8 文件，按方法归属改注入域接口）**

| 消费方 | 原注入 | 现注入 | 使用方法 |
|--------|--------|--------|----------|
| `LoginCommandHandler.cs`（Auth） | ICrossModuleService | **IUserCrossModuleService** | GetUserByUsername/VerifyPassword/UpdateLoginFailure/ResetLoginState |
| `BatchImportFormulasCommandHandler.cs`（Formula） | ICrossModuleService | **IHerbCrossModuleService** | GetAllActiveHerbs |
| `ValidateFormulaHerbCommandHandler.cs`（Formula） | ICrossModuleService | **IHerbCrossModuleService** | GetHerbBasicInfo |
| `MedicalCaseCommandService.cs`（MedicalCase） | ICrossModuleService | **IPatient + IUserCrossModuleService** | 透传给 Helper（Patient/Doctor 校验） |
| `MedicalCaseServiceHelper.cs`（MedicalCase） | ICrossModuleService×2 | **IUser**（GetOperatorInfo）；**IPatient + IUser**（ValidateAndFetch） | GetUserBasicInfo/GetPatientBasicInfo |
| `MedicalCaseStateService.cs`（MedicalCase） | ICrossModuleService | **IUserCrossModuleService** | 透传 Helper.GetOperatorInfo |
| `PrescriptionItemService.cs`（MedicalCase） | ICrossModuleService | **IHerbCrossModuleService** | GetDisabledHerbIds/GetHerbPrices |
| `QuickVisitCommandHandler.cs`（Registration） | ICrossModuleService | **IPatient + IUserCrossModuleService** | GetPatientBasicInfo/GetUserBasicInfo |

**4. 域接口补全**

统一门面 10 个方法经逐方法比对，**全部已存在于 6 个域接口**（Patient 1 / Herb 4 / User 5），无需补全。验证依据：

| 门面方法 | 域接口 | 已存在 |
|----------|--------|--------|
| GetPatientBasicInfoAsync | IPatientCrossModuleService.cs:12 | ✅ |
| GetHerbBasicInfoAsync / GetHerbPricesAsync / GetDisabledHerbIdsAsync / GetAllActiveHerbsAsync | IHerbCrossModuleService.cs:12/15/18/21 | ✅ |
| GetUserBasicInfoAsync / GetUserByUsernameAsync / UpdateLoginFailureAsync / ResetLoginStateAsync / VerifyPasswordAsync | IUserCrossModuleService.cs:12/15/20/25/30 | ✅ |

**5. 实现合并**

`CrossModuleService.cs` 全部 10 个方法均为逐行转发到 `_patient/_herb/_user` 域实现（无独有逻辑），域实现（PatientCrossModuleService/HerbCrossModuleService/UserCrossModuleService）已完整覆盖——**无需合并，直接删除**。

**6. 文档残留同步（4 文件）**

| 文件 | 变更 |
|------|------|
| `src/Server/AGENTS.md` | `ICrossModuleService` → `IXxxCrossModuleService` 域接口（3 处） |
| `src/Server/Modules/AGENTS.md` | 同上（4 处） |
| `src/Server/Core/LYBT.Infrastructure/README.md` | 决策表更新为 A-31-C8 定案表述 |
| `src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj` | 注释 `ICrossModuleService` → `IHerbCrossModuleService` |

### 验证

| 项 | 结果 |
|----|------|
| `rg "ICrossModuleService" src/ tests/` | **0 残留** |
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告** |
| `dotnet test tests/LYBT.Tests.Architecture/` | **88/88 通过**（P07/P08/P10 全绿） |

### Commit

```
7e84e5149 refactor(server): A-31-C8-1 跨模块门面单轨化（删 ICrossModuleService 统一门面，消费方改域接口）
```
17 files changed, 40 insertions(+), 167 deletions(-)（含 3 文件删除）｜已 push origin master

---

## C8-2 映射统一（决策点 4）

### 现状确认

- `DtoConversionExtensions`（Shared.Models/Extensions，3 个手写方法）唯一活跃调用点 = `MedicalCaseCommandService.cs:46`（Desktop workspace 保存链路）。
- 全仓其余 `ToInputDto` 均为 Mapperly 调用（`s_mapper.ToInputDto(...)` / `_consultationMapper.ToInputDto(...)`），任务书列举的连带消费方（ConsultationMapper/PrescriptionMapper/FormulaDetailModelMapper/ConsultationItem/PrescriptionItemViewModel/MedicalCaseCommandsViewModel）经 grep 验证**零引用**手写扩展（全仓 `using LYBT.Shared.Models.Extensions` 仅 1 处，即被改的 MedicalCaseCommandService）。

### 动作

**1. `MedicalCaseCommandService.cs:46` 改走 Mapperly**

- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMapper.cs`：
  - 新增 `public partial MedicalCaseInputDto ToInputDto(MedicalCaseDetailDto dto)`（DTO→InputDto，与既有 Model→InputDto 方法并存）
  - 新增私有嵌套映射 `MapToPrescriptionInput(PrescriptionDetailDto)`（PrescriptionDetailDto→PrescriptionInputDto）
  - 忽略字段与手写扩展行为对齐：`RegistrationId/EditReason/NeedsPrescription`（顶层）、`MedicalCaseId/NeedsPrescription`（处方层）——手写扩展均未设置这些字段，**行为等价**
- `MedicalCaseCommandService.cs`：注入 DI Singleton `MedicalCaseDetailModelMapper`，`_context.CurrentDetail.ToInputDto()` → `_mapper.ToInputDto(_context.CurrentDetail)`；删除 `using LYBT.Shared.Models.Extensions`

**2. 删除 `DtoConversionExtensions`**

- 删除 `src/Shared/LYBT.Shared.Models/Extensions/DtoConversionExtensions.cs`（3 个手写方法 + 文件）

**3. 连带消费方清理**

经 grep 验证，任务书列举的 6 个连带消费方（ConsultationMapper/PrescriptionMapper/FormulaDetailModelMapper/ConsultationItem/PrescriptionItemViewModel/MedicalCaseCommandsViewModel）的 `ToInputDto` 全部是 Mapperly 调用，**本身零引用手写扩展，无需改动**。

**4. 文档残留同步（2 文件）**

| 文件 | 变更 |
|------|------|
| `src/Shared/LYBT.Shared.Models/README.md` | 删除 DtoConversionExtensions 章节与目录树条目，补映射规则说明 |
| `src/Shared/LYBT.Shared.Models/AGENTS.md` | Extensions 目录描述更新 |

### 验证

| 项 | 结果 |
|----|------|
| `rg "DtoConversionExtensions\|ToPrescriptionInputDto\|Shared.Models.Extensions" src/ tests/` | **0 残留** |
| `rg "\.ToInputDto\(\)" src/ tests/`（手写扩展调用形式） | **0 残留** |
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告**（Mapperly 源生成编译通过） |
| `dotnet test tests/LYBT.Tests.Architecture/` | **88/88 通过** |
| `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~MedicalCaseMasterDetailViewModelTests"` | **24/24 通过**（直接使用 MedicalCaseDetailModelMapper，验证映射正常） |

> 说明：Desktop 全量测试中 33 个 MedicalCase 相关失败均为 `localhost:5000` Socket 连接拒绝（需运行中的 WebAPI）——既有环境项（13c-current-status P0-06/C-01），与本次改动无关。行为等价由字段对齐 + Mapperly 单测覆盖验证。

### Commit

```
5e03819a1 refactor(desktop): A-31-C8-2 映射统一（workspace 保存走 Mapperly，删 DtoConversionExtensions 手写扩展）
```
5 files changed, 24 insertions(+), 101 deletions(-)（含 1 文件删除）｜已 push origin master

---

## 明确不做项确认

| 项 | 状态 |
|----|------|
| 项目合并（C-3/C-4） | ❌ 未执行 |
| D72 extern / US-CARD-002 | ❌ 未触碰 |
| 6 个域接口 | ❌ 全部保留（IPatient/IHerb/IUser/IAuth/IMedicalCase/IRegistration） |
| Mapperly 全部 Mapper 重写 | ❌ 未重写（仅新增 MedicalCaseDetailModelMapper 内 DTO→InputDto 方法） |

## 架构测试基线

| 项 | 值 |
|----|-----|
| Build | 0 错误 **0 警告**（`--no-incremental`） |
| 架构测试 | 88/88 pass |
| 蓝图 §2.1/§3.5 | 已更新（技术总监完成，未回滚） |

## Journey Log

- [lesson] 统一门面的 10 个方法早已全部收敛进域接口（审计报告中的"门面/域接口双份"实际为门面对域接口的转发）——删除门面是纯减法，无需任何域接口补全或实现合并。
- [lesson] Desktop 侧"连带消费方"（ConsultationMapper 等）经实测全部已是 Mapperly 调用，任务书列举的清理项实际无需改动，全仓手写扩展引用仅 1 处。
- [pivot] 新增 Mapperly 方法忽略的字段（RegistrationId/EditReason/NeedsPrescription/MedicalCaseId）严格对齐手写扩展"未设置"的行为，避免引入隐式行为变化。

## Source Materials

| 文件 | 角色 |
|------|------|
| `docs/compose/specs/task-a31-c8-decision-points-convergence-2026-08-08.md` | 任务书（本报告依据） |
| `docs/03-architecture/14-structure-design-blueprint.md` §2.1/§3.5 | 设计态定义（已更新） |
| `docs/03-architecture/13-project-master-plan.md` §九 | 决策记录（技术总监更新） |
