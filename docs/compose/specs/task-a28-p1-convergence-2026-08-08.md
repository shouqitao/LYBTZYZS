# 任务 A-28：A-26 P1 收敛批次（7 项，分 2 个子批次）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 用户决策：方案 A（7 项 P1 一次批次，按依赖排序拆 2-3 个子批次）｜技术引入治理规则（先文档后代码，本次已先行文档化）
> **文档先行已就绪**：蓝图 §2.2「请求处理边界规则」（v1.4）+ 总账 §九 双轨定案行（commit `c88cf342c`）——代码改动前方案已定案。

## 任务

执行 A-26 收敛审查的 7 项 P1 修复（见 `docs/compose/reports/structure-convergence-mimo-2026-08-08.md` §4），分 **2 个子批次**顺序执行，每子批次独立验证 + 独立 commit。

## 范围

- ✅ `src/Server`（模块 + Infrastructure + WebAPI）+ `src/Shared` + `tests/LYBT.Tests.Architecture/`
- ❌ 排除：Desktop、LocalWebAPI、tests/Server（除非编译依赖）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `c88cf342c`

---

## 子批次 1（结构性，3 项）

### P1-1：统一 5 模块请求处理模式（双轨规范化）

**方案（蓝图 §2.2 已定案）**：CQRS 模块（Auth/Users/Patients/Herbs/Formula/Registration）——
- **写操作**（Create/Update/Delete/Status 变更/Import/Restore）→ 走 MediatR Handler（`ISender.Send`）
- **读操作**（Get/List/Search/Export）→ 走 Service 直查（`IXxxService`）
- **禁止**：Controller 混用同一操作两条路径

**现状（A-26 报告 §2.2 已核实）**：Users/Patients/Herbs/Formula 4 模块「读/改走 Service + 建/删/导入走 Send」——Update/Restore 等**写操作走了 Service**（如 PatientsController.cs:107 `_patientService.UpdateAsync`、:182 `_patientService.RestoreAsync`），丢失验证管道。

**动作**：
1. 逐模块（Users/Patients/Herbs/Formula）列出 Controller 中走 Service 的**写操作**（Update/Delete/Status 变更/Restore/Enable/Disable 等）
2. 为每个写操作补齐/迁移到对应 MediatR Command + Handler（若 Handler 已存在则改 Controller 调用 `_sender.Send`；若不存在则新建，含 Validator）
3. Service 中对应的写方法（如 `PatientService.UpdateAsync`）删除或降为 Handler 内部辅助——**优先删除**（Handler 已覆盖），保留则必须在报告说明理由
4. 读操作（GetPaged/GetById/Export）保持 Service 直查不动
5. Auth/Registration 已单轨（纯 Handler），只需**核实不回归**
6. **新增架构守卫**：`tests/LYBT.Tests.Architecture/` 补「写端点禁直调 Service 写方法」守卫（CQRS 模块 Controller 的 Update/Delete/Status 动作不得直接调用 `IXxxService` 对应方法，必须经 `ISender`）——守卫粒度以可行为准（反射扫描 Controller 方法体或按模式匹配），若反射扫描不可行则降级为「Controller 禁止注入既有写 Service」+ 报告说明

**验证**：build 0 错误 0 警告；架构测试（原 86 + 新守卫）全绿；`grep` 确认 4 模块 Controller 无 `_xxxService.UpdateAsync/RestoreAsync/DeleteAsync` 直调（读方法除外）。

### P1-2：P10 守卫盲区修复（跨模块服务绕过模块 DbContext）

**现状（A-26 报告 §1.4）**：`UserCrossModuleService.cs:18` 与 `HerbCrossModuleService.cs:17` 通过 `IDbContextAccessor` 注入并直查 `AppDbContext`（`_context.Users.FirstOrDefaultAsync` 等），绕过模块 DbContext 自治；P10 守卫只查构造函数参数类型，不查 `IDbContextAccessor`。

**动作**：
1. 调研跨模块服务实际查询场景（UserCrossModuleService / HerbCrossModuleService / PatientCrossModuleService / HealthCheckService 等所有 `IDbContextAccessor` 使用者）
2. 区分两类：① **只读跨模块查询**（如验证用户存在/取用户名）→ 可保留但改为注入**本模块 DbContext**（UsersDbContext/HerbsDbContext）或改走 `IRepository`；② **写操作** → 必须走本模块 Repository/Service
3. 实施改造：跨模块服务注入模块 DbContext（或 Repository），不再直查 AppDbContext
4. **补架构守卫**：P10 扩展——检查构造函数参数含 `IDbContextAccessor` 且类位于业务模块 Services 的，视为违规（基础设施类如 HealthCheckService/DatabaseInitializationService 豁免，守卫按程序集+命名空间区分）

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认业务模块 Services 无 `_context.` 直查 AppDbContext。

### P1-3：Server 仓储收敛（BaseRepository 泛型化推广）

**现状（A-26 报告 §2.4）**：`BaseRepository<TEntity,TDbContext>` 继承者仅 3（MedicalCase/MedicalCaseReference/HerbReference），9 个裸实现（Formula/Herb/User/AuthSession/SecurityAudit/Registration/Patient/Report/SystemLog）。

**动作**：
1. 分析 9 个裸 Repository 中**标准 CRUD 型**（含泛型接口 `IRepository<T>` 方法）→ 改为继承 `BaseRepository<TEntity,TDbContext>`，接口保留模块特化方法
2. **非 CRUD 型**（Reports/SystemLog 等聚合查询/只读）→ 保持裸实现，报告注明例外理由
3. 逐个迁移，行为等价（不改变查询逻辑，只改继承/基类方法调用）

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认裸 Repository 数下降且例外有据。

---

## 子批次 2（小修，4 项）

### P1-4：手写映射清除（UserCrossModuleService → Mapperly）

**现状（A-26 报告 §2.5）**：`UserCrossModuleService.cs:38-56`（`new UserBasicDto { ... }` 16 属性）+ `:68-87`（`new UserCredentialDto { ... }`）手写映射。

**动作**：Users 模块补 `UserCrossModuleMapper`（Mapperly partial），两处手写映射改为 Mapperly 调用；目标属性若 Mapperly 无法自动映射（源/目标名不同），用 `[MapProperty]` 显式映射。

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认 UserCrossModuleService 无 `new UserBasicDto {` 手写。

### P1-5：LoginRequestValidator 双份收敛

**现状（A-26 报告 §3.3）**：Shared `Validators/Auth/LoginRequestValidator.cs`（验证 `LoginRequest`）+ Auth 模块 `Application/Validators/LoginRequestValidator.cs`（验证 `LoginCommand`，规则几乎相同），AuthModule.cs:58-59 同时注册两个程序集。

**动作**：保留 **Shared 版为 SSOT**（契约层验证），删 Auth 模块版；若 `LoginCommand` 验证依赖命令特有属性（如验证码），将该规则迁入 Shared 版或命令 Handler 内验证。核实 AuthModule 注册后删除。

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认 `LoginRequestValidator` 仅 Shared 一份。

### P1-6：命名空间单复数全仓对齐

**现状（A-26 报告 §1.2）**：Server 3 模块（`LYBT.Module.Registration`/`LYBT.Module.MedicalCase`/`LYBT.Module.Formula`）**项目名单数** vs **命名空间复数**（`LYBT.Module.Registrations.*` 等）；蓝图 §3.2 仍写 `LYBT.Desktop.Registration`。

**动作**：
1. Server 3 模块 csproj 显式加 `AssemblyName`/`RootNamespace` 为复数（或按 Q-03 先例判断最小成本方案——Q-03 方案甲：纯命名空间重命名不碰程序集名；本次方向相反：项目名改复数，评估是否动 csproj 文件名/目录）
2. 蓝图 §3.2 表格同步 `LYBT.Desktop.Registration` → `LYBT.Desktop.Registrations`
3. 若项目名改动影响架构测试（按程序集名 `Assembly.Load`），同步测试

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认 3 模块 csproj 名/命名空间一致。

### P1-7：Jwt 配置 Section 统一

**现状（A-26 报告 §3.8）**：LocalWebAPI 用 `LocalJwt`（LocalJwtConfig.cs:22、LocalWebApiProgram.cs:104），Server/Shell 用 `Jwt`。

**动作**：统一为 `Jwt`——LocalWebAPI 配置读取改为 `Jwt` Section（若 Local 配置值不同，在 LocalWebAPI 启动时做映射）；或蓝图注明 `LocalJwt` 为 Local 专用例外（若统一成本高）。**由执行者评估后选择，倾向统一 `Jwt`**，在报告说明。

**验证**：build 0 错误 0 警告；架构测试全绿；`grep` 确认 `LocalJwt` 残留 0 或蓝图已注明例外。

---

## 硬性约束

1. **Surgical Changes**：每项只改该任务涉及代码，不「顺手」改其他
2. **文档先行已就绪**：蓝图 §2.2 规则 + 总账 §九 已定案，代码改动必须符合蓝图；若执行中发现蓝图规则需调整，**先停下**报告技术总监，不自行改方案
3. **0 错误 0 警告**：每子批次结束 `dotnet build LYBTZYZS.sln --no-incremental`
4. **架构测试**：每子批次结束 `dotnet test tests/LYBT.Tests.Architecture/` 全绿
5. 每子批次**独立 commit + push**：`refactor(server): A-28-1 双轨规范化/P10守卫/仓储收敛` / `refactor(server): A-28-2 映射/Validator/命名空间/Jwt`
6. 产出报告：`docs/compose/reports/a28-p1-convergence.md`（每项：动作/验证/残留检查/例外说明）

## 明确不做（防发散）

- ❌ 不碰 Desktop/LocalWebAPI（除非 P1-7 Jwt 涉及 LocalWebAPI 配置——该例外允许，但需说明）
- ❌ 不修 P2 项（蓝图文件数回写、BatchImport 泛型化等留待后续）
- ❌ 不重新质疑已定案方案（双轨规范化方向已拍板）
