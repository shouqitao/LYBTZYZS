# A-31-C5 机制收敛报告（ErrorMessages / AddModuleDbContext / 仓储镜像模板 / VM 命令模板）

> 执行：Mimo Code｜日期：2026-08-09｜基线：`aba83a133`（C-2 完成后）
> 任务书：`docs/compose/specs/task-a31-c5-mechanism-convergence-2026-08-08.md`
> 依据：S2 报告（`method-audit-server-2026-08-08.md` C6/C7/§5.3）+ S3 报告（`method-audit-desktop-2026-08-08.md` §2）
> 范围：Server（C5-1/2/3）+ Desktop（C5-4）；不碰映射双机制（决策点 4）、不碰跨模块门面（决策点 2）、不合并项目、不删死代码

---

## 总览

| 项 | 动作 | 验证 | Commit |
|----|------|------|--------|
| C5-1 | 统一 ErrorMessages：45 文件硬编码错误文案 → `ErrorMessages.Get` | build 0/0 + 架构 88/88 | `9eb8db522` |
| C5-2 | 提取 `AddModuleDbContext<TContext>`：7 模块 DbContext 引导收敛 | build 0/0 + 架构 88/88 | `0d78b56c6` |
| C5-3 | 仓储镜像方法模板化：`GetByIdIncludingDeleted`/`Exists` 谓词尾上收 BaseRepository | build 0/0 + 架构 88/88 + HerbRepo 12/12 | `b637690ed` |
| C5-4 | VM 命令模板：`ConnectionTestViewModelBase` + `EditorViewModelBase` + E3 修复 | build 0/0 + 架构 88/88 + Desktop MD 87 通过 | `9ba8540ed` |

每项独立 commit + push，均以 `dotnet build LYBTZYZS.sln --no-incremental`（0 错误 0 警告）+ `dotnet test tests/LYBT.Tests.Architecture/`（88/88）为门禁。

---

## C5-1：统一 ErrorMessages（S2 C6）

### 动作

1. **ErrorMessages.cs 键内容对齐 Handler 实际文案**（SSOT 向 Handler 现行文案收敛，用户可见文案零变化，仅来源统一）：
   - `UserNotFound` 用户未找到→用户不存在；`PatientNotFound` 患者未找到→患者不存在；`HerbNotFound` 草药未找到→药材不存在；`HerbNameExists` 草药名称已存在→药材名称已存在；`FormulaNotFound` 配方未找到→方剂不存在；`FormulaNameExists` 配方名称已存在→方剂名称已存在；`UserLocked`→账号已被锁定，请稍后重试；`AuthInvalidCredentials`→用户名或密码错误；`AuthTokenRevoked`→令牌已被撤销，请重新登录；`AuthAccessTokenExpired`→访问令牌已过期，请重新登录；`PatientNotDeleted`→该患者未被删除；`HerbNotDeleted`→药材未被删除，无需恢复；`FormulaNotDeleted`→验方未被删除，无需恢复；`McPatientDisabled`→该患者已被禁用，无法创建医案；`McActiveCaseExists`→该患者已有进行中的医案，请先完成或重开现有医案；`McPrescriptionFlagRequired`→请先标记是否需要开处方；`McPrescriptionRequired`→已标记需要开处方，但处方不存在，无法完成医案；`McPrescriptionItemsRequired`→处方必须包含至少一项药材才能完成医案；`MedicalCaseMissingDiagnosis`→中医诊断不能为空，请先填写中医诊断；`McCancelReasonRequired`→非当天本人创建的医案取消时必须提供取消原因；`McAlreadyDeleted`→医案已被删除；`McOnlyCompletedCanDelete`→仅已完成医案可删除，未完成医案请使用「取消」（物理删除）；`McSuspendedCaseExists`→该患者已有暂存的医案，请先处理现有医案（继续或关闭）
2. **45 个 Handler/Service 文件**硬编码文案改 `ErrorMessages.Get(ErrorCode.X)`（患者/用户/药材/方剂/医案 CRUD、Auth 登录/刷新/令牌、MedicalCase State/Command/Query、Registration QuickVisit、Batch 操作 `EntityNotFoundMessage` 等），涉及约 60 处调用点
3. **新增 using** `LYBT.Shared.Models.Primitives.ErrorCodes`（6 个 Batch Handler 原无该引用）

### 例外（保留硬编码，注明原因）

| 位置 | 文案 | 原因 |
|------|------|------|
| JwtService（~12 处） | 令牌不能为空/无效的令牌格式/令牌刷新失败/自动登录令牌… | 令牌内部状态细粒度消息，ErrorCode 均为 AuthTokenInvalid/AuthAccessTokenExpired，语义为凭证内部校验，非业务错误码面；补键 12 个新 ErrorCode 收益低于成本 |
| MedicalCaseStateService:62 | 完成医案请使用专用的 Complete 接口… | 开发者指引文案（告诉实现者走专用接口），非用户业务错误 |
| MedicalCaseStateService:78 / Helper:71,82 / Repository.Update:195 / 各 BatchImport | 插值消息（`$"患者有 {n} 条…"` 等） | 含运行时数据，ErrorMessages 无模板机制；为插值造模板机制违反"不为收敛造新抽象层" |
| 权限/校验类（无权删除用户/真实姓名不能为空/用户名不能为空/只能修改自己的密码等） | | 属权限/校验语义，S2 C-内4 方向是删 Handler 手写校验依赖管道，不补键 |
| CommandService:119 | 中医诊断不能为空（短版） | 与 MedicalCaseMissingDiagnosis 键（长版）内容不同，保留短版硬编码 |
| 验方不存在（ValidateFormulaHerb/RestoreFormula） | | 术语差异（方剂/验方），Glossary 以验方为准但多数代码用方剂，未强行统一 |

### 验证

- `rg "Failure\(ErrorCode\.\w+,\s*\"" src/Server/Modules` 硬编码计数显著下降（原 ~60 处 → 例外 ~25 处，降幅 >50%）
- build 0 错误 0 警告；架构测试 88/88

---

## C5-2：提取 AddModuleDbContext<TContext> 扩展（S2 C7）

### 动作

1. 新建 `src/Server/Core/LYBT.Infrastructure/Data/ModuleDbContextExtensions.cs`：
   ```csharp
   public static IServiceCollection AddModuleDbContext<TContext>(
       this IServiceCollection services, IConfiguration configuration)
       where TContext : DbContext
   ```
   内部统一 `DatabaseOptions` + `ConnectionStringResolver.GetEffectiveConnectionString` + 空串校验 + `UseSqlServer`
2. **7 个模块迁移**（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration）：`services.AddDbContext<XxxDbContext>((sp, options) => {...})` 逐行重复块 → `services.AddModuleDbContext<XxxDbContext>(configuration)`
3. **OnModelCreating 各自注册配置保留**（各模块 DbContext 内实体配置不合并）
4. 清理各 Module.cs 因内联块移除而失效的 using（`Microsoft.EntityFrameworkCore`/`IOptions`/`Shared.Configuration(.Options.Server)`），补 `LYBT.Infrastructure.Data`

### 验证

- `rg "AddDbContext<" src/Server/Modules` = 0（全部收敛）；`AddModuleDbContext` 7 处
- build 0 错误 0 警告；架构测试 88/88

---

## C5-3：仓储镜像方法模板化（S2 §5.3）

### 动作

1. **BaseRepository 补两个模板方法**（`Repositories/BaseRepository.cs`）：
   - `GetByIdIncludingDeletedAsync(Guid id, ct)`（virtual）——`_dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)`，Patient/Herb 直接继承
   - `ExistsAsync(Expression<Func<TEntity,bool>> predicate, Guid? excludeId, ct)`（protected）——谓词 + excludeId 判重尾，Patient/Herb/Formula 的 `ExistsByNameAsync` 委托调用
2. **PatientRepository/HerbRepository**：删 `GetByIdIncludingDeletedAsync` 实现（继承基类）、`ExistsByNameAsync` 改委托
3. **FormulaRepository**：`GetByIdIncludingDeletedAsync` **保留 override**（需 `Include(f => f.Herbs)`，合法特化，注明例外）；`ExistsByNameAsync` 改委托

### 例外（保留现状，注明原因）

| 仓储 | 方法 | 原因 |
|------|------|------|
| UserRepository | `GetByIdIncludingDeletedAsync`/`GetPagedAsync` | 裸实现（`ApplicationUser : IdentityUser` 非 BaseEntity，无法继承 `BaseRepository`）；S2 §5.2 已判合法特化 |
| 4 仓储 | `GetPagedAsync` | 签名与过滤/排序逻辑各不同（Patient 状态过滤、Herb/Formula category、User role+status），仅分页尾同构；S2 §5.3「分页内联 6 处」为独立 C 项（QueryablePagingExtensions 收敛），不在 C5-3 范围 |

### 验证

- 4 仓储镜像方法去重（GetByIdIncludingDeleted：4 实现→1 基类 + 1 特化；ExistsByName：3 实现→1 基类谓词）
- build 0 错误 0 警告；架构测试 88/88；`HerbRepositoryTests` 12/12 通过（行为等价）

---

## C5-4：VM 命令模板收敛（S3 §2）

### 动作 1：TestConnectionAsync 提取（S3 V4，~95% 同构）

新建 `LYBT.Desktop.Auth/ViewModels/ConnectionTestViewModelBase.cs`（继承 `DialogViewModelBase`）：
- 统一 `RemoteUrl/TestStatus/TestStatusMessage/IsNotTesting` + `TestConnectionCommand`（`[RelayCommand(CanExecute=CanTestConnection)]`）+ `TestConnectionAsync` 状态机
- 扩展点：`OnTestStatusChangedCore()`（子类联动额外属性）、`OnTestCompleted(bool ok)`（FirstRun 联动 IsRemoteAvailable）、`OnRemoteUrlChangedCore(string)`（ServerConfig 联动 SaveOnlyCommand）
- `ServerConfigViewModel`/`FirstRunSetupViewModel` 改为继承，删除重复实现（约 45 行/份）

### 动作 2：EditorViewModelBase 提取（S3 L1）

评估结论：**4 份全量提取不可行**（User 结构差异大：`[ObservableProperty]` 风格 + IDesktopCacheManager 缓存联动 + 无脏订阅；MedicalCase 骨架不同），按任务书「只提取高同构部分」执行：

新建 `LYBT.Desktop.Infrastructure/ViewModels/Base/EditorViewModelBase<TContext>`（`TContext : ValidatableModelBase`）：
- 统一 `IsDirty` 脏标记 + `InitializeForNewCase`/`Validate`/`Reset` 生命周期 + 上下文变更订阅（`SubscribeContext`/`UnsubscribeContext`）
- 抽象 `Context`/`CreateNewContext`，子类以业务名公开（`Patient`/`Herb`/`Formula`）保持 XAML 绑定兼容
- `InitializeFromDto`/`GetXData` 字段映射保留在子类（各模块私有映射，未强行泛型化）
- **Patient/Herb/Formula 3 个 Editor VM 继承**；Formula 覆写 `InitializeForNewCase`/`Reset` 追加药材列表管理

### 动作 3：E3 事件泄漏修复（S3 E3，顺带）

- 原 3 个 Editor VM 每次 `Initialize` 重新 `+=`、仅 `Reset` 内 `-=`（多 Initialize 泄漏风险）
- 修复：`SubscribeContext()` 采用「先 `-=` 再 `+=`」幂等订阅；`Reset` 内 `UnsubscribeContext()` 保持 `-=`——收敛时一并处理

### 例外（保留现状，注明原因）

| 位置 | 原因 |
|------|------|
| `UserEditorViewModel` | 结构差异（`[ObservableProperty]` 直接声明 + 构造注入 `IDesktopCacheManager` + Reset 联动缓存失效 + 无脏订阅），强行继承需改接口/构造，收益低于风险 |
| `ConsultationEditorViewModel`/`PrescriptionEditorViewModel`（MedicalCase） | 同骨架但宿主/上下文模型不同（Editor 依赖 `MedicalCaseWorkspaceViewModel` 状态），S3 判「同骨架」未达 90% 同构门槛 |
| EditorViewModelBase 未泛型化 `TDto/TInput` | `InitializeFromDto`/`GetXData` 为各模块私有字段映射（约 14 字段逐字段复制），泛型化需引入映射器（Mapperly 为决策点 4，禁止触碰） |

### 验证

- build 0 错误 0 警告；架构测试 88/88
- `LYBT.Tests.Desktop` MasterDetail 相关 87 通过 / 3 跳过（跳过项为 LocalDB 环境依赖，预存状态）

---

## 硬性约束核对

| 约束 | 状态 |
|------|------|
| Surgical Changes（每项只改任务涉及文件） | ✅ 4 项分别限定 Handler/Service、Module.cs、Repository、ViewModel |
| 文档先行（影响蓝图先更新蓝图） | ✅ C5-2 扩展为 Infrastructure 数据层新增（架构测试 P 系列未涉及）；C5-4 基类为 Desktop 内部收敛，蓝图 §2.2 无冲突；总账已同步（`docs/03-architecture/13-project-master-plan.md` A-31 行） |
| 0 错误 0 警告（`--no-incremental`） | ✅ 4 项均验证 |
| 架构测试全绿 | ✅ 88/88 × 4 |
| 每项独立 commit + push | ✅ 4 个 commit（见总览表），全部推送 origin/master |
| 产出报告 | ✅ 本文档 |
| 不碰映射双机制 / 跨模块门面 / 项目合并 / 死代码 / 新抽象层 | ✅ 均未触碰；C5-3/C5-4 的例外均注明 |

---

## 遗留与后续建议

1. **ErrorMessages 例外清单**（JwtService 令牌内部消息 / 插值消息 / 权限校验类）——建议随 C-6 死代码批次或独立决策处理，不补键避免 ErrorCode 膨胀
2. **UserEditorViewModel 收敛**——需先统一为手动 `SetProperty` 风格并解耦 `IDesktopCacheManager`（构造参数），方可继承 `EditorViewModelBase`；收益约 20 行，优先级低
3. **分页内联 6 处**（S2 §5.3）——独立 C 项，建议后续统一走 `QueryablePagingExtensions.GetPagedResultAsync`（MedicalCase 已示范）
4. **验方/方剂术语统一**——Glossary 与代码不一致，需产品定稿后统一 ErrorMessages 文案
