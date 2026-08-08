# 任务 A-29：A-26 P2 收敛 + 顺带发现处置（3 组）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 用户决策：继续推进 P2（2026-08-08「继续」）｜技术引入治理规则生效
> 依据：A-26 收敛审查报告 `docs/compose/reports/structure-convergence-mimo-2026-08-08.md` §4（P2 11 项）+ §5（顺带 5 条）

## 任务

执行 A-26 剩余 P2 收敛项（11 项）与顺带发现（5 条），分 3 组执行，每组独立验证 + 独立 commit。

## 范围

- ✅ `src/Server` + `src/Shared` + `src/Client`（按项）
- ✅ `docs/03-architecture/14-structure-design-blueprint.md`（蓝图维护）
- ✅ `tests/LYBT.Tests.Architecture/`（守卫如有）
- ❌ 排除：tests/Server、tests/Desktop（除非编译依赖）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `3e7c1ddd6`（A-28 完成）

---

## 组 1：蓝图维护（T0，纯文档，6 项）

> 蓝图文件数已核实为**实际值**（A-28 后）：Infrastructure 92 / Patients 18 / Formula 14 / Foundation 70 / Contracts 78 / Logging 8——直接回写蓝图。

### P2-8：蓝图 §1/§3 文件数表格回写
- 蓝图 §1 Shared 表：`LYBT.Shared.Logging` 9→8
- 蓝图 §3.1 Core 表：`LYBT.Desktop.Contracts` 79→78、`LYBT.Desktop.Foundation` 72→70、`LYBT.Desktop.Infrastructure` 101→92、`LYBT.Desktop.Controls` 42→41
- 蓝图 §3.2 Modules 表：`LYBT.Desktop.Patients` 25→18、`LYBT.Desktop.Formula` 16→14、`LYBT.Desktop.MedicalCase` 50→49；`LYBT.Desktop.Admin`/`LYBT.Desktop.Clinical` **补文件数列**（Admin 17 / Clinical 21）
- 蓝图 §3.3 Roles 表：Admin/Clinical 补文件数列

### P2-9：蓝图 §2.1 补记 Controller 继承路径
- §2.1 `BaseApiController` 条目补记模块级路径：`BaseUsersController` / `BaseRegistrationsController` / `BaseMedicalCasesController`（A-26 报告 §2.1：5 条路径而非 3 条，均为合理三层继承）

### P2-10：蓝图 §2.4 结构矛盾澄清
- §2.4 七目录模板 vs §2.2 实际三态（CQRS/Service 化/只读聚合）矛盾——改为「三态模板」表述：
  - CQRS 模块：Application/Infrastructure/Interfaces/Services（无 Domain，实体在 Entities）
  - Service 化模块（MedicalCase）：Services/Repositories，无 Application
  - 只读模块（Reports）：Service+Repository，复用 AppDbContext
  - Mappers 位置注明：MedicalCase/Registration 在模块根 `Mappers/`，其他模块在 `Application/Mappers/`
- 注明 `Infrastructure/` vs `Repositories/` 目录名并存（MedicalCase 用 Repositories，其他用 Infrastructure——已定案 A-28，蓝图记录差异即可）

### P2-16：蓝图记录三层接口命名矩阵
- §3.1 Contracts 条目补记：`IXxxApi`（Refit internal）/ `IApiClientXxx`（唯一对外面）/ `IXxxService`（服务接口）三层命名矩阵 + 跨层镜像接口清单（Server 与 Desktop.Contracts 同名字不同程序集：IFormulaRepository/IHerbRepository/IUserRepository 等——镜像关系注明防误改）

### P2-17：蓝图/术语表记录 Status vs State 语义边界
- 蓝图 §0.4 或术语表补记：「域内状态用 Status 枚举（MedicalCaseStatus/RegistrationStatus）；客户端 UI/会话状态用 State 枚举（WorkspaceEditState/EditState/AuthState/SessionState/TokenLifecycleState）」
- 同步 `docs/01-product/03-glossary.md` 加一词条

### 顺带-4：蓝图 §4 守卫计数口径注明
- §4 Tests 表 Architecture 条目补注：「守卫计数口径 = [Fact]/[Theory] 方法数（86）；Theory 数据展开后多于该数」

---

## 组 2：代码收敛（T1，5 项）

### P2-13：KeyNotFoundException → NotFoundException（3 处）
- `MedicalCaseRepository.Update.cs:194` / `MedicalCaseServiceHelper.cs:71` / `MedicalCaseServiceHelper.cs:82` 改抛 `Shared.ExceptionHandling` 的 `NotFoundException`（业务异常层次统一；行为不变——SystemExceptionHandler 已映射 404）
- 核实 NotFoundException 构造函数签名，补 using

### P2-14：请求后缀统一（LoginRequest vs ResetPasswordRequestDto）
- 统一为 `XxxRequest`（推荐，对齐 LoginRequest/LogoutRequest，Refit 接口天然形态）或 `XxxRequestDto`——**由执行者 grep 全部请求契约后判断**，选少改动方向，报告说明
- 蓝图 §3.2 若涉及同步

### P2-15：UserBasicDto 迁入 Contracts/Users/
- `Shared.Models/DTOs/Users/UserBasicDto.cs` → `Shared.Models/Contracts/Users/`（与其余 User DTO 同目录）；删 `DTOs/Users/` 空目录
- 更新引用（grep UserBasicDto using）

### P2-18：Shell Jwt 结构补齐
- `src/Client/Desktop/Shell/appsettings.json` 的 `Jwt` 节补 `AccessTokenExpirationMinutes`/`RefreshTokenExpirationDays`（与 Server 同型）；核实 Shell JwtOptions 是否实际消费这两个字段（若 Options 不读则补字段但注明预留）
- Reports DTO 契约约定补记：`ReportQueryModels.cs`（Infrastructure 私有 record）为仓储返回形状，对外 DTO 在 Shared——蓝图 §2.2 Reports 条目注明

### 顺带-3：Shared.Models/DTOs 死目录清理
- 随 P2-15 一并：`DTOs/` 目录删空后移除

### 顺带-5：BaseRegistrationsController.Update 补 override
- `BaseRegistrationsController.cs:69` Update 未加 `override`（基类同名方法）——核实基类方法是否 virtual：若是则补 `override`，若否（隐藏）则加 `new` 或按设计保留，报告说明；与同文件其他 override 风格对齐

---

## 组 3：顺带发现处置（3 条）

### 顺带-1：codebase-memory 图谱索引过期
- 报告说明即可（MCP 索引重建属运维，不在代码任务内）；任务报告记录「索引含 A-21/A-23 已删文件，需重建」

### 顺带-2：AGENTS.md 陈旧信息清理
- `src/Server/AGENTS.md`：删 `LYBT.Module.Sync`（不存在）引用
- `src/Server/Modules/AGENTS.md`：删 `SharedKernel`（已坍缩）引用
- `src/Client/Desktop/LocalWebAPI/AGENTS.md`：controllers 10→12、删 Sync 依赖
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/AGENTS.md` 等：删 `LocalData/CardReader`（已废弃）引用
- **只改陈旧引用**，不重写 AGENTS.md 结构

### P2-12：BatchImport 二套骨架（评估，倾向记录）
- 评估 `BatchImport×3`（Formula/Herbs/Patients）与 `BatchOperationHandlerBase` 泛型化成本——因参数/返回形状不同（导入策略、成功计数），**倾向保持独立 + 蓝图 §2.1 注明「第二模板」**（避免第三个手写导入出现）
- 若评估认为可泛型化（成本低）则实施，否则蓝图注明——**执行者判断，报告说明理由**

---

## 硬性约束

1. **Surgical Changes**：每项只改该任务涉及代码/文档
2. **文档先行**：组 1 蓝图维护先行提交；组 2/3 代码改动前确认蓝图已同步（P2-14/15/18 涉及蓝图则一并）
3. **0 错误 0 警告**：组 2/3 结束 `dotnet build LYBTZYZS.sln --no-incremental`
4. **架构测试**：组 2/3 结束 `dotnet test tests/LYBT.Tests.Architecture/` 全绿（组 1 纯文档无需 build）
5. 每组**独立 commit + push**：`docs: A-29-1 蓝图维护（P2-8/9/10/16/17 + 守卫口径）` / `refactor(server): A-29-2 代码收敛（P2-13/14/15/18 + 顺带3/5）` / `docs: A-29-3 顺带处置（AGENTS.md 陈旧 + BatchImport 评估）`
6. 产出报告：`docs/compose/reports/a29-p2-convergence.md`（每项动作/验证/例外说明）

## 明确不做（防发散）

- ❌ 不碰 P1 已闭项（A-28 已完成）
- ❌ 不引入新技术（无 T3）
- ❌ 不重写 AGENTS.md 结构（只清陈旧）
- ❌ 不评估 Desktop 项目合并（方案 A 定：项目合并后续再评估）
