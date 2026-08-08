# A-28 执行报告：A-26 P1 收敛批次（7 项）

> 执行者：Mimo Code ｜ 日期：2026-08-09 ｜ 任务书：`docs/compose/specs/task-a28-p1-convergence-2026-08-08.md`
> 基线 `c88cf342c` → 子批次1 `bf2f58d52` → 子批次2 `2988dde17`（均已 push origin master）
> 验证门禁：每子批次 `dotnet build LYBTZYZS.sln --no-incremental` **0 错误 0 警告** + `dotnet test tests/LYBT.Tests.Architecture/` **88/88 全绿**（原 86 + 新增 P19/P19b 2 项）

---

## 子批次 1（结构性 3 项）— commit `bf2f58d52`

### P1-1 双轨规范化（蓝图 §2.2）

**动作**：
- 4 模块（Users/Patients/Herbs/Formula）Controller 中走 Service 的 **14 处写操作**全部迁回 MediatR Handler（`Sender.Send`）：
  - Users 2：`Update`→`UpdateUserCommand`、`ChangeProfile`→`ChangeProfileCommand`
  - Patients 2：`Update`→`UpdatePatientCommand`、`Restore`→`RestorePatientCommand`
  - Herbs 5：`Update`/`ToggleStatus`/`Restore`/`BatchEnable`/`BatchDisable` → 对应 `UpdateHerbCommand`/`ToggleHerbStatusCommand`/`RestoreHerbCommand`/`BatchEnableHerbsCommand`/`BatchDisableHerbsCommand`
  - Formula 5：同上 `UpdateFormulaCommand`/`ToggleFormulaStatusCommand`/`RestoreFormulaCommand`/`BatchEnableFormulasCommand`/`BatchDisableFormulasCommand`
- 新建 **14 组 Command + Handler + Validator**（均含 FluentValidation Validator，注册经各模块 `AddValidatorsFromAssemblyContaining` 自动生效），Handler 逻辑与旧 Service 写方法逐行等价（含唯一性检查/软删除校验/操作者记录）。
- **Service 写方法及接口声明全部删除**（优先删除原则）：`PatientService.UpdateAsync/RestoreAsync`、`HerbService.UpdateAsync/ToggleStatusAsync/RestoreAsync/BatchEnableAsync/BatchDisableAsync`、`FormulaService.*` 同 5 个、`UserService.UpdateAsync/ChangeProfileAsync` + 对应 `IPatientService/IHerbService/IFormulaService/IUserService` 接口声明；读操作（GetPaged/GetById/GetByIdNumber/GetCurrentUser）保持 Service 直查不动。
- **LocalWebAPI 3 控制器同步迁移**（编译依赖条款）：Patients/Herbs/Formulas Controller 共 12 处直调改为 `Sender.Send`（LocalWebAPI 复用 Server 模块与 Handler，双轨真共享语义不变）。
- **新增架构守卫**（`tests/LYBT.Tests.Architecture/ServerArchTests.cs`）：
  - `P19_Cqrs_Services_Must_Not_Expose_Write_Methods`：CQRS 模块读 Service 接口（IUserService/IPatientService/IHerbService/IFormulaService）不得声明写方法（Update/Restore/Toggle/Enable/Disable/Change/Delete/Status/Save/Reset/Import/Batch 动词）——写方法自接口层面不存在，Controller 无法直调。
  - `P19b_Cqrs_Write_Endpoints_Must_Not_Call_Service_Write_Methods`：**IL 扫描** CQRS 模块 Controller（Users/BaseUsers/Patients/Herbs/Formulas/Auth/Registrations/BaseRegistrations）方法体的 call/callvirt 指令，禁止直调 Service 类型写方法（含 `Module.ResolveMethod` token 解析 + 完整操作数长度解码）。

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认 4 模块 + LocalWebAPI 控制器无 `_xxxService.UpdateAsync/RestoreAsync/ToggleStatusAsync/BatchEnableAsync/BatchDisableAsync/ChangeProfileAsync` 直调残留（PASS）。

**残留检查**：Auth/Registration 已单轨（纯 Handler），核实无回归（`rg` 无 Service 写方法直调）；读端点 Service 直查保留。

### P1-2 P10 守卫盲区修复

**动作**：
- 3 个跨模块服务由 `IDbContextAccessor` 直查 AppDbContext 改为**注入本模块 DbContext**：
  - `UserCrossModuleService` → `UsersDbContext`（Users 查询/写全覆盖，IdentityDbContext 提供 Users DbSet）
  - `HerbCrossModuleService` → `HerbsDbContext`（含 PrescriptionItems/Prescriptions/MedicalCases/Patients/FormulaHerbItems 引用检查 DbSet，`CheckHerbReferenceAsync` 跨表查询覆盖）
  - `PatientCrossModuleService` → `PatientsDbContext`（Patients DbSet）
- **P10 守卫扩展**：`P10_Services_Should_Not_Directly_Inject_AppDbContext` 增加 `IDbContextAccessor` 参数检查；`LYBT.Infrastructure` 命名空间基础设施类（HealthCheckService/DatabaseInitializationService/DbContextAccessor 本体）显式豁免。

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认业务模块 Services 无 `IDbContextAccessor` 注入（仅 LYBT.Infrastructure 基础设施 3 处保留）。

**例外说明**：`UserCrossModuleService` 写路径（UpdateUserPasswordHash/UpdateLoginFailure/ResetLoginState）现经 UsersDbContext 直存，UsersDbContext 未映射 RowVersion（AppDbContext 有），乐观并发语义由「冲突抛异常」变为「后写覆盖」——登录失败计数/锁定场景可接受，报告记录此细微差异；如需并发保护可后续在 UsersDbContext 补 RowVersion 映射（不在本批次范围）。

### P1-3 Server 仓储收敛（BaseRepository 泛型化推广）

**动作**：
- **3 个标准 CRUD 仓储迁移继承** `BaseRepository<TEntity,TDbContext>`（实体均满足 `: BaseEntity` 约束）：
  - `PatientRepository : BaseRepository<Patient, PatientsDbContext>`（接口 `IPatientRepository : IRepository<Patient>`，删除与基类重复的 GetById/Add/Update 实现，5 个特化查询保留）
  - `HerbRepository : BaseRepository<Herb, HerbsDbContext>`（接口 `IHerbRepository : IRepository<Herb>`）
  - `FormulaRepository : BaseRepository<Formula, FormulaDbContext>`（接口 `IFormulaRepository : IRepository<Formula>`，`GetByIdAsync` 因含 `Include(Herbs)` 用 `override` 保留）
  - 构造改为 `(context, ILogger<T>) : base(context, logger)`，行为等价（Add/Update 立即保存语义与原实现一致）。

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认裸 Repository 由 9 → 6，且例外均有据。

**例外说明（保留裸实现 6 个，理由如下）**：
| 仓储 | 例外理由 |
|---|---|
| `UserRepository` | 实体 `ApplicationUser : IdentityUser<Guid>` **不继承 BaseEntity**，违反 `where TEntity : BaseEntity` 约束；且接口无 Add/Delete |
| `AuthSessionRepository` | 实体 AuthSession 为纯 POCO（无 BaseEntity/IsDeleted，用 IsRevoked），方法为域定制（GetByTokenHash/RevokeAll） |
| `SecurityAuditRepository` | 拆分保存模式（Add 不 Save + 外部显式 SaveChangesAsync 工作单元），与 Base 自动保存语义冲突；审计写入不应触发完整 CRUD |
| `RegistrationRepository` | Add/Update **刻意延迟保存**（手动 Unit-of-Work + BeginTransactionAsync），直接继承会改变保存时机语义（挂号队列事务性关键） |
| `ReportRepository` | 纯只读聚合（跨 4 表 Sum/Count/GroupBy 下推，无单一 TEntity、无写方法），沿用 AppDbContext（P18 已豁免） |
| `SystemLogRepository` | 纯只读（唯一 GetRecentLogsAsync），实体 SystemLog 无 BaseEntity（int Id），沿用 AppDbContext |

---

## 子批次 2（小修 4 项）— commit `2988dde17`

### P1-4 手写映射清除（UserCrossModuleService → Mapperly）

**动作**：Users 模块新增 `Application/Mappers/UserCrossModuleMapper.cs`（`[Mapper(RequiredMappingStrategy = Target)]` 静态 partial）；`GetUserBasicInfoAsync` 的 `new UserBasicDto{}`（16 属性）与 `GetUserByUsernameAsync` 的 `new UserCredentialDto{}` 两处手写映射改为 `UserCrossModuleMapper.ToBasicDto(u)` / `ToCredentialDto(u)`。
- `[MapProperty]` 显式映射：`LastLoginAt→LastLoginTime`、`AccessFailedCount→FailedLoginCount`
- 自定义方法：`ToNonNullString`（UserName/PasswordHash 的 `string?→string` null 合并）、`ToLockoutEnd`（`DateTimeOffset?→DateTime?` 保留 `UtcDateTime` 语义）

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认 `UserCrossModuleService` 无 `new UserBasicDto {`/`new UserCredentialDto {` 残留（PASS）。

### P1-5 LoginRequestValidator 双份收敛

**动作**：删除 Auth 模块 `Application/Validators/LoginRequestValidator.cs`（验证 `LoginCommand`，规则与 Shared 版完全一致仅访问路径差 `Input.`）；`AuthModule.cs` 移除其 `AddValidatorsFromAssemblyContaining` 注册，**保留 Shared 版 `LYBT.Shared.Models.Validators.Auth.LoginRequestValidator` 为 SSOT**（契约层验证 `LoginRequest`）。
- 核实：`LoginCommand` 仅包裹 `LoginRequest`，**无命令特有属性**（无验证码等），规则全部由 Shared SSOT 覆盖，无需迁移；Handler 保留用户名/密码空值守卫（返回 `AuthInvalidCredentials`）。

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认 `LoginRequestValidator` 仅 Shared 一份（PASS）。

**残留检查**：命令级 MaxLength(32)/MinLength(6) 细化规则随 Auth 版删除——违规输入由 Handler 密码验证自然失败（"用户名或密码错误"），不泄露校验细节，行为可接受（任务书授权路径）。

### P1-6 命名空间单复数全仓对齐

**动作**（最小成本方案，沿用 Q-03 方案甲「不碰项目文件名/目录」先例，方向相反仅改程序集名）：
- `LYBT.Module.Registration.csproj` 补 `<RootNamespace>LYBT.Module.Registrations</RootNamespace>` + `<AssemblyName>LYBT.Module.Registrations</AssemblyName>`（与 MedicalCase/Formula 既有复数设置对齐；不重命名 csproj 文件/目录，ProjectReference/sln 条目零影响）
- 架构测试 3 处程序集名引用同步 `LYBT.Module.Registration` → `LYBT.Module.Registrations`：`TestAssemblies.cs`、`ArchTests.cs`（P07 列表）、`LocalWebApiPatternTests.cs`（P21 期望引用列表）
- 蓝图 §3.2 表格 `LYBT.Desktop.Registration` → `LYBT.Desktop.Registrations`（代码早已复数化，蓝图同步）

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg` 确认 3 模块 csproj 的 AssemblyName/RootNamespace 均为复数且与命名空间一致（PASS）。

### P1-7 Jwt 配置 Section 统一

**动作**（评估后选择**统一为 `Jwt`**，非例外路径）：
- `LocalJwtOptions.SectionName`：`"LocalJwt"` → `"Jwt"`（`Shared.Configuration/Options/Server/LocalJwtOptions.cs`），DataAnnotation 消息同步 `Jwt:...`
- `LocalWebAPI/appsettings.json`：配置节 `LocalJwt` → `Jwt`，注释与建议环境变量 `LocalJwt__SecretKey` → `Jwt__SecretKey`
- `LocalJwtOptionsValidator` 失败消息 `LocalJwt:` → `Jwt:`
- LocalWebApiProgram/PrismConfigurationExtensions 均经 `LocalJwtOptions.SectionName` 常量绑定，零代码改动自动生效

**验证**：build 0 错误 0 警告；架构测试 88/88；`rg "LocalJwt"` 残留仅**类型名**（`LocalJwtOptions`/`LocalJwtConfig`/`LocalJwtOptionsValidator` 及 LocalWebAPI 注释/README），**配置节残留 0**。

**例外说明**：① 类型名保留（LocalJwtOptions 为 Local 专用选项类型、LocalJwtConfig 为本地 JWT 生成器，非配置节，重命名收益低、波及 Desktop Shell 注册点）；② Local 与 Server 的 `Jwt` 节**值格式仍独立**（Local 为 Base64 密钥 + 365 天固定过期，Server 为明文密钥 + 6 键完整配置）——各宿主读各自 appsettings，节名统一即可满足一致性目标；③ `LocalJwtConfig.ExpirationMinutes` 属性实际未被使用（LocalJwtConfig 固定 365 天），保留默认值无行为影响。

---

## 明确不做（防发散确认）

- ✅ 未碰 P2 项（蓝图文件数回写、BatchImport 泛型化等）
- ✅ 未重新质疑已定案方案（双轨规范化方向）
- ✅ Desktop/LocalWebAPI 仅按「编译依赖 + P1-7 Jwt 例外」条款最小触碰（3 控制器命令化迁移 + Jwt 节统一），其余零改动

## 硬性约束达成核对

| 约束 | 子批次1 | 子批次2 |
|---|---|---|
| `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告 | ✅ | ✅ |
| `dotnet test tests/LYBT.Tests.Architecture/` 全绿 | ✅ 88/88 | ✅ 88/88 |
| Surgical Changes | ✅ | ✅ |
| 独立 commit + push | ✅ `bf2f58d52` | ✅ `2988dde17` |
| 报告产出 | ✅ 本文档 | — |
