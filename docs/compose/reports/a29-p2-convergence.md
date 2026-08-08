# A-29 执行报告：A-26 P2 收敛 + 顺带发现处置（3 组）

> 执行者：Mimo Code ｜ 日期：2026-08-09 ｜ 基线：`3e7c1ddd6`（A-28 完成）
> 任务书：`docs/compose/specs/task-a29-p2-convergence-2026-08-08.md`
> 提交：`994925641`（组1）→ `27463791b`（组2）→ `fb5573586`（组3），均已 push origin master

---

## 组 1：蓝图维护（T0 纯文档，6 项）— commit `994925641`

| 项 | 动作 | 验证 |
|----|------|------|
| P2-8 | 蓝图 §1/§3 文件数回写：Logging 9→8、Contracts 79→78、Foundation 72→70、Infrastructure 101→92、Controls 42→41、Patients 25→18、Formula 16→14、MedicalCase 50→49；§3.3 Roles 表补文件数列（Admin 17 / Clinical 21，已核实实际值） | diff 核对 |
| P2-9 | §2.1 补记三条模块级 Controller 路径：BaseUsersController（Users:23）/BaseRegistrationsController（Registration:16）/BaseMedicalCasesController（MedicalCase:18），继承链 BaseApiController→BaseCrudController→模块级，5 条终态路径 | 源码核实继承链 |
| P2-10 | §2.4 七目录模板改为**三态模板**（CQRS/Service 化/只读聚合）+ Mappers 位置差异（MedicalCase/Registration 模块根 vs 其他 Application/Mappers/）+ Infrastructure vs Repositories 目录差异注记（A-28 定案并存） | diff 核对 |
| P2-16 | §3.1 补**接口命名三层矩阵**（IXxxApi internal / IApiClientXxx 对外 / IXxxService）+ 跨层镜像接口清单（7 对：FormulaRepository/HerbRepository/UserRepository/RegistrationRepository/MedicalCaseRepository/PatientRepository/FormulaService） | grep 双端核实 7 对镜像 |
| P2-17 | 蓝图 §0.4 原则 7 + 术语表 `Status / State` 词条：域状态用 Status 枚举（MedicalCaseStatus/RegistrationStatus/FormulaStatus/CommonStatus），客户端 UI/会话状态用 State 枚举（WorkspaceEditState/EditState/AuthState/SessionState/TokenLifecycleState） | diff 核对 |
| 顺带-4 | 蓝图 §4 Architecture 条目补守卫计数口径注记 + §0.2 图同步（85 守卫→81 方法/88 用例） | 见「例外说明 ①」 |

组 1 纯文档，未运行 build（任务书豁免）。

---

## 组 2：代码收敛（T1，5 项 + 顺带 2 条）— commit `27463791b`

| 项 | 动作 | 验证 |
|----|------|------|
| P2-13 | 3 处 `KeyNotFoundException`→`NotFoundException`：`MedicalCaseRepository.Update.cs:194`（补 using `LYBT.Shared.ExceptionHandling.Exceptions`）、`MedicalCaseServiceHelper.cs:71/82`（using 已有）| 行为不变：BusinessExceptionHandler 处理 AppException→404（GetHttpStatusCode）；SystemExceptionHandler 残留 KeyNotFoundException 仅映射定义（保留） |
| P2-14 | 请求后缀统一为 **`XxxRequest`**（grep 全部请求契约：10 个无后缀 vs 2 个有后缀，选少改动方向）：`ResetPasswordRequestDto`→`ResetPasswordRequest`（文件 git mv 重命名）、`CancelMedicalCaseRequestDto`→`CancelMedicalCaseRequest`；双控制器树（WebAPI+LocalWebAPI）+ Refit/IApiClient/仓储/测试共 23 处引用全量更新，0 残留 | build + 全仓 grep `RequestDto` 0 残留 |
| P2-15 | `UserBasicDto` 迁入 `Shared.Models/Contracts/Users/`（git mv + 命名空间 `DTOs.Users`→`Contracts.Users`）；8 处 `using` 更新（含 Auth 模块 2 处）；随带删 `DTOs/` 死目录（顺带-3，目录已空） | build + grep `DTOs` 0 残留 |
| P2-18 | ① Shell `appsettings.json` Jwt 节补 `AccessTokenExpirationMinutes: 480` / `RefreshTokenExpirationDays: 7`（与 Server 同型同值）。**已核实 Options 实际消费**：共享 `JwtOptions`（Shared.Configuration/Options/Common/）定义两字段（Range 校验）+ Shell 经 `PrismConfigurationExtensions.RegisterOptions<JwtOptions>` 绑定，非预留。② 蓝图 §2.2 Reports 条目补契约约定注记（`ReportQueryModels.cs` 为仓储私有返回形状，对外 DTO 在 Shared） | build 通过 |
| 顺带-3 | `Shared.Models/DTOs/` 死目录删除（仅剩 UserBasicDto 时随 P2-15 迁出） | Test-Path False |
| 顺带-5 | `BaseRegistrationsController.Update` 核实：**基类链（BaseApiController/BaseCrudController）均无 Update 方法**→为独立 action，无需 override/new，保留现状 | 源码核实；见「例外说明 ②」 |

**验证（门禁）**：`dotnet build LYBTZYZS.sln --no-incremental` **0 错误 0 警告**（首次检出 1 个 CS0105 重复 using——AuthUserMapper.cs 因替换产生，已删重复行复验通过）；`dotnet test tests/LYBT.Tests.Architecture/` **88/88 全绿**。

---

## 组 3：顺带处置（3 条）— commit `fb5573586`

| 项 | 动作 | 验证 |
|----|------|------|
| 顺带-1 | **codebase-memory 图谱索引过期**：仅报告说明，未重建（MCP 索引重建属运维，不在代码任务内）。索引含 A-21/A-23 已删文件（Users/Patients Mapper、Infrastructure ApiService 等），如需继续使用该 MCP 需重建索引 | 报告记录 |
| 顺带-2 | AGENTS.md 陈旧信息清理（只清陈旧、不重写结构），共 5 个文件：`src/Server/AGENTS.md`（删 Sync、SharedKernel×4 处改为 ICrossModuleService（Infrastructure/Services/CrossModule）表述）、`src/Server/Modules/AGENTS.md`（删 Sync 表格行、SharedKernel×3 处）、`src/Client/Desktop/LocalWebAPI/AGENTS.md`（controllers 10→12、Sync→Reports、Controllers 表补 Deploy/Reports 两行）、`src/Client/Desktop/AGENTS.md`（Core 目录列表删 LocalData/CardReader/Models/Utilities）、`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/AGENTS.md`（删 CardReader 行）| 见「例外说明 ③④」 |
| P2-12 | **BatchImport×3 保持独立**（评估结论：保持独立 + 蓝图 §2.1 注明「第二模板」）。评估依据：① 输入形状不同（List\<TRowDto\> 数据行 vs List\<Guid\> 按 ID）；② 返回形状不同（各模块专用 ImportResultDto 含行级失败明细/DataSnapshot vs BatchOperationResultDto）；③ 查重/策略分支（Skip/Update/Error）与 Update 语义模块特定；④ 泛型化需 5+ 抽象钩子 + 结果类型泛型，成本高于 3 处共性。蓝图已注明「第二模板」并禁止第三个手写导入 | 源码评估（3 handler 逐项对比） |

**验证（门禁）**：组 3 纯文档改动，仍按硬性约束复跑 `dotnet build LYBTZYZS.sln --no-incremental` **0 错误 0 警告** + `dotnet test tests/LYBT.Tests.Architecture/` **88/88 全绿**。

---

## 例外说明

1. **守卫计数口径（顺带-4）**：任务书/蓝图历史记 86，实测 `[Fact]/[Theory]` 方法数 **81**（80 Fact + 1 Theory），Theory 数据展开后 dotnet test 实际执行 **88** 用例。蓝图按实测注明口径（81 方法/88 用例），避免后续审计误解。
2. **顺带-5 结论**：A-26 报告 §5.5 假设「重写基类同名方法」不成立——基类链无 Update 方法，`BaseRegistrationsController.Update` 是独立 action，当前代码正确，无任何改动。
3. **LocalWebAPI AGENTS.md**：任务书「controllers 10→12」，改数字时同步把 Controllers 表补 Deploy/Reports 两行（DeployController 无业务注入、ReportsController 注入 IReportRepository），避免数字 12 与 10 行表格自相矛盾。子目录表注记改为「12 controllers，9 use Service layer, 3 use DbContext directly（Auth/Health/Diagnostics）」（保留原 7/3 口径的直连例外观测）。
4. **顺带清理**：① `src/Server/AGENTS.md` 删 Sync/SharedKernel 时同句修正「Domain 层」陈旧表述（蓝图 §2.4 已定案全模块无 Domain/，实体下沉 LYBT.Entities）；② `src/Client/Desktop/AGENTS.md` Core 目录列表同时删 Models/Utilities（从未建立的 Core 子项目，与 Core/AGENTS.md 注记一致）；③ `src/Server/Modules/AGENTS.md` 第 7 行 Purpose 的「Domain」表述保留未动（不在任务书清单且未与其他清理项同句）。
5. **P2-14 蓝图同步**：蓝图 §3.2 为模块结构表，不涉及请求契约命名，无需同步（仅组 2 中 P2-18 的蓝图 Reports 注记随组 2 提交）。
6. **任务书 86 vs 实测差异均已如实记录**；无 P1 已闭项触碰、无新技术引入、AGENTS.md 结构未重写。

---

## 交付物

- 蓝图 v1.5（`14-structure-design-blueprint.md`）：文件数回写 + 三态模板 + 接口矩阵 + Status/State + 守卫口径 + 批处理第二模板 + Reports 契约约定
- 术语表：Status/State 词条
- 代码：异常层次统一（P2-13）、请求契约后缀统一（P2-14）、UserBasicDto 归位 + DTOs 死目录清除（P2-15/顺带-3）、Shell Jwt 结构对齐（P2-18）
- 文档清理：5 个 AGENTS.md 陈旧引用（Sync/SharedKernel/LocalData/CardReader）
- 3 个独立 commit + push：`994925641` / `27463791b` / `fb5573586`
