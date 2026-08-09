# 任务 A-31-C5：机制收敛（ErrorMessages / AddModuleDbContext / 仓储镜像模板 / VM 命令模板）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §3.3 + S2/S3 审查报告
> 用户方针：先收敛再完善｜已授权执行 C 批次
> **注意**：C-1（日志）/C-2（异常）已完成，本批次为剩余 T1 机制收敛

## 任务

执行 4 项机制收敛（每项独立验证 + 独立 commit + push）。

## 范围

- ✅ Server 模块（ErrorMessages / AddModuleDbContext / 仓储镜像模板）
- ✅ Desktop 模块（VM 命令模板）
- ❌ 排除：映射双机制统一（DtoConversionExtensions vs Mapperly——决策点 4 未拍板，**不动**）
- ❌ 排除：跨模块门面（决策点 2 未拍板，**不动**）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-2 完成后 `aba83a133`）

---

## C5-1：统一 ErrorMessages（S2 C6，E 级）

**背景**：模块 Handler 错误文案硬编码（如 `CreatePatientCommandHandler.cs:29` "患者不存在"），而 `ErrorMessages.cs:45-49` 已定义；Registration 已用 `ErrorMessages.Get`（`CancelRegistrationCommandHandler.cs:30`）。

**动作**：
1. 全仓扫描 Server 模块 Handler/Service 中硬编码错误文案（"XXX不存在"/"XXX已存在"/"状态不允许"等）
2. 与 `ErrorMessages.cs` 已有键对比：有键 → 改用 `ErrorMessages.Get(key)`；无键 → 补键
3. **只改文案来源，不改业务逻辑/不改文案内容**（文案内容保持一致，只是来源统一）

**验证**：grep 硬编码错误文案计数下降；build 0 错误 0 警告。

## C5-2：提取 AddModuleDbContext<TContext> 扩展（S2 C7，C 级）

**背景**：`PatientsModule.cs:29-36` 与 `RegistrationModule.cs:30-37` 逐行相同（DatabaseOptions+ConnectionStringResolver+UseSqlServer）；8 模块 OnModelCreating 各自注册配置。

**动作**：
1. 在 Infrastructure（或合适共享位置）提取 `AddModuleDbContext<TContext>(IServiceCollection, IConfiguration)` 扩展
2. 迁移 Patients/Registration 等模块的 DbContext 引导代码到共享扩展（消除逐行重复）
3. **OnModelCreating 各自注册配置不合并**（那是各模块自己的实体配置，保留）

**验证**：PatientsModule/RegistrationModule 引导代码不再逐行重复；build 0 错误 0 警告 + 架构测试全绿。

## C5-3：仓储镜像方法模板化（S2 §5.3，E 级）

**背景**：`GetByIdIncludingDeletedAsync`/`GetPagedAsync`/`ExistsByNameAsync` 在 Patient/Herb/Formula/User 4 仓储同构。

**动作**：
1. 分析 4 个仓储的镜像方法签名与实现差异
2. **若实现同构**（仅实体/DbContext 不同）→ 在 `BaseRepository<TEntity,TDbContext>` 补泛型镜像方法
3. **若存在合法特化**（如 User 的 `GetByIdIncludingDeletedAsync` 有额外逻辑）→ 保留特化，注明例外
4. 4 仓储改为继承或减少重复

**验证**：4 仓储镜像方法去重；build 0 错误 0 警告 + 架构测试全绿。

## C5-4：VM 命令模板收敛（S3 §2，C 级）

**背景**：
- `TestConnectionAsync`：`ServerConfigViewModel.cs:87-120` vs `FirstRunSetupViewModel.cs:75-112` **~95% 同构**（状态机逐行一致；CanTestConnection/OnTestStatusChanged/IsNotTesting 成对重复）
- **Editor VM 模板 4 份拷贝**（InitializeFromDto/InitializeForNewCase/GetXData/Validate/Reset/IsDirty/OnXPropertyChanged）：User/Patient/Herb/Formula Editor VM ~90% 同构（User 略异）；ConsultationEditor/PrescriptionEditor 同骨架

**动作**：
1. **先做 TestConnectionAsync**（低风险，2 处）：提取共享 `ConnectionTestHelper` 或 VM 基类方法
2. **Editor VM 模板**（高风险，4+ 份）：评估提取 `EditorViewModelBase<TContext,TDto,TInput>` 基类的可行性
   - 若可行 → 提取基类，4 个 Editor VM 继承
   - 若差异过大（User 略异、MedicalCase 骨架不同）→ **只提取高同构部分**（Patient/Herb/Formula 3 个 ~90%），其余注明例外
3. **事件泄漏修复顺带**：Patient/Herb/Formula 3 个 Editor VM 的 `Initialize` 重新 `+=` 问题（S3 E3）——在收敛时一并处理（`-=` 在 Reset 内 + 复用订阅）

**验证**：TestConnectionAsync 2 处合并；Editor VM 模板去重；事件泄漏修复；build 0 错误 0 警告 + 架构测试全绿 + Desktop 相关单测。

---

## 硬性约束

1. **Surgical Changes**：每项只改该任务涉及文件
2. **文档先行**：若某项改动影响蓝图（如仓储基类方法新增、VM 基类新增），先更新蓝图再改代码
3. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
4. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿
5. **每项独立 commit + push**：`refactor(server): A-31-C5-1 ...` / `refactor(server): A-31-C5-2 ...` / `refactor(server): A-31-C5-3 ...` / `refactor(desktop): A-31-C5-4 ...`
6. 产出报告：`docs/compose/reports/a31-c5-mechanism-convergence.md`（每项动作/验证/例外）

## 明确不做（防发散）

- ❌ 不碰映射双机制（DtoConversionExtensions vs Mapperly——决策点 4）
- ❌ 不碰跨模块门面（决策点 2）
- ❌ 不执行项目合并（C-3/C-4 延期）
- ❌ 不删死代码（C-6 批次）
- ❌ 不新增抽象层（禁止为收敛而造新基类——C5-4 若差异过大宁可保留现状注明例外）
