# 任务 A-31-C6：死代码清理（C 批次收官）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §五（C-6）+ S1/S2/S3 审查报告 D 级清单
> 用户方针：先收敛再完善｜已授权执行 C 批次
> **注意**：C-2 已删 4 整类死类（37 方法）。本批次只删**符号级确认安全**的死代码（机械删除项），**复核项不删**（留待人工确认）。

## 任务

删除 S1/S2/S3 审查中**符号级确认死**且**删除无风险**的方法/常量/接口成员。复核项（接口契约/文档化 API/DI 扩展/死 Model 等）**保留**，注明待人工。

## 范围

- ✅ Server 模块 + Infrastructure（S2 §7 真死 14 项 + MedicalCase 死链 9 项 + 常量 2 项）
- ✅ Desktop 模块 + Core（S3 可安全删 14 项 + 2 死类 PatientItem/UserItem + OnSelfPropertyChanged 空体）
- ✅ Shared（S1 剩余死方法：SetCorrelationId/GetCorrelationIdOrNew/WriteToConsoleWithTemplate/WriteToFileWithTemplate/MaskObject/SanitizeException + NotFoundException 5 静态工厂 + Result.ValidationFailure + ErrorCodeExtensions.GetModuleName + PasswordHelper 仅测试引用的 4 方法）
- ❌ 排除：**复核项 64**（S3 §7.4：接口契约 39+14 / 文档化基类 API 7 / DI 扩展 3 / 死 Model 4 / extern SDK 1 组）——留待人工
- ❌ 排除：BaseCrudController 防呆桩（运行时不可达但契约签名，删除破坏编译）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-5 完成后 `9c19791e8`）

## 清单（以报告为准，执行者按文件:行号核实）

### S1 剩余（Shared）
1. `SetCorrelationId`（CorrelationId Provider 实现+接口，0 调用）
2. `GetCorrelationIdOrNew`（0 调用）
3. `WriteToConsoleWithTemplate`/`WriteToFileWithTemplate`（LoggerConfigurationExtensions，0 调用）
4. `MaskObject`（SensitiveDataMasker，仅测试引用）
5. `SanitizeException`（SensitiveDataMasker，仅测试引用——**C-1 后确认仍死则删**）
6. `NotFoundException` 5 个静态工厂
7. `Result.ValidationFailure`
8. `ErrorCodeExtensions.GetModuleName`
9. `PasswordHelper.GenerateTemporaryPassword/GenerateSalt/ValidatePassword/SecureEquals`（仅测试引用——**注意**：若测试是有效断言则保留测试，只删生产方法；若测试测的是死功能则删测试）

### S2 Server（29 符号中的真死部分）
按 S2 §7 表格逐项执行：真死 14 项 + MedicalCase 死链 9 项（D12-D14）+ 常量 2 项 + 接口 2 项（先核实接口成员删除是否破坏实现编译——破坏则只删实现，接口留待复核）

### S3 Desktop（可安全删 14 + 2 死类）
按 S3 §7.4「可安全删 14」逐项 + `PatientItem`/`UserItem` 死类 + Formula `OnSelfPropertyChanged` 空体

## 关键约束

1. **每个删除前必须再核实**（报告是审查时点，C-0/C-1/C-2/C-5 改动后可能变化——用 serena/grep 复查调用点）
2. **接口成员删除规则**：接口+实现成对且 0 消费 → 全删；接口成员有实现但删除破坏编译 → 只删实现，接口留待复核
3. **测试联动**：删死方法后若测试引用该方法的**功能本身**（非有效断言）→ 同步删测试；若测试测的是**有效行为** → 保留测试改走正确方法
4. **禁止顺手清理**：只删清单项，不删清单外任何代码（发现新死代码记录到报告，不处理）

## 硬性约束

1. **Surgical Changes**：只删清单项
2. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
3. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿
4. **分 3 组独立 commit + push**：`refactor(shared): A-31-C6-1 ...` / `refactor(server): A-31-C6-2 ...` / `refactor(desktop): A-31-C6-3 ...`
5. 产出报告：`docs/compose/reports/a31-c6-dead-code-cleanup.md`（每项删除/验证/保留复核项清单）

## 明确不做（防发散）

- ❌ 不删复核项 64（S3 §7.4 接口契约/文档化 API/DI 扩展/死 Model——留待人工，报告记录）
- ❌ 不删 BaseCrudController 防呆桩（契约签名）
- ❌ 不执行项目合并（C-3/C-4 延期）
- ❌ 不碰映射双机制（决策点 4）、跨模块门面（决策点 2）
- ❌ 不重构（只删，不提取/不合并/不改逻辑）
