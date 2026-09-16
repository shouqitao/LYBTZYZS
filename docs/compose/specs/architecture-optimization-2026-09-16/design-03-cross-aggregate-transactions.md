# 设计 03：跨聚合显式事务

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：审计报告 L4 事务管理

## 1. 问题描述（实测，含一处**既存正确性缺陷**）

全仓仅 1 处 `BeginTransaction`：`RegistrationRepository.BeginTransactionAsync` + `StartVisitCommandHandler`。

`StartVisitCommandHandler` 的类注释声称：**「原子事务：Registration.Status=InProgress + 创建 MedicalCase(Active) 关联 RegistrationId」**。实测**该声明不成立**：

- `await using var transaction = await _repository.BeginTransactionAsync(ct);` — 事务开在 **`RegistrationDbContext`** 的连接上。
- 事务内调用 `_medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync(...)` → `MedicalCaseCrossModuleService` → `IMedicalCaseCommandService.SaveAsync` → **`MedicalCaseDbContext`**（模块自有 DbContext，ADR-0017）。
- 两个 DbContext 使用**不同连接**，`RegistrationDbContext` 上的事务**不可能**覆盖医案写入。医案写入若在挂号提交前失败，或挂号提交失败，两者不会一起回滚。

其余跨聚合写入同样无事务：`MedicalCaseStateService`（完成/取消 → 联动挂号，先提交本聚合再联动）、`MedicalCaseCommandService.Creation`（创建医案后另库联动挂号）。

## 2. 设计方案（**需 ADR 决策**）

跨 DbContext 写入无法用单个 EF 事务覆盖，三选一：

| 方案 | 说明 | 评价 |
|---|---|---|
| **A. 同连接显式事务** | 让跨模块服务接受调用方的 `DbContext`/`IDbContextTransaction`，两个模块共用一次连接 | 破坏 ADR-0017 模块隔离（模块不再持有自有上下文），牵连面大 |
| **B. `TransactionScope`** | 环境事务；跨连接时自动升级为分布式事务（MSDTC） | 诊所单机/小服务器上 MSDTC 运维成本高、易踩防火墙/权限坑；不建议 |
| **C. 补偿 + 幂等（Saga-lite）** | 不追求跨库原子；每个跨库步骤可重试且幂等，失败时**显式补偿**本聚合 | 与既有代码方向一致（`StartVisitCommandHandler` 已有 `RevertToWaiting()` 补偿雏形）；无新增基础设施 |

**推荐 C，并在「同 DbContext 内的多聚合写入」补显式事务（A 的受限形式）**：

1. **同 DbContext**（如同一模块内的 挂号 + 挂号明细）：用 `Database.BeginTransactionAsync` 包住，提交由调用方控制。
2. **跨 DbContext**（挂号 ↔ 医案）：保持两步写 + **幂等键 + 补偿**：
   - 幂等：跨模块创建医案以 `RegistrationId` 为幂等键（已存在则复用，不再新建）。
   - 补偿：第二步失败 → 第一步显式回滚（若尚未提交）或补偿写入（若已提交），并把结果如实返回调用方（**禁止**静默成功）。
   - 明确「已提交但后续步骤失败」的语义：返回失败 + 记录审计，由用户重试（幂等键保证重试安全）。
3. 删除 `StartVisitCommandHandler` 注释中不成立的分词，改为如实描述「两步写 + 补偿」。

## 3. 影响范围

`src/Server/Modules/LYBT.Module.Registration/**`（Handler + Repository + IRegistrationRepository）、`src/Server/Modules/LYBT.Module.MedicalCases/**`（StateService / CommandService / IMedicalCaseCrossModuleService 幂等化）。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| 补偿逻辑本身出错导致数据半写 | 中 | 补偿路径必须有覆盖测试（失败注入） |
| 幂等键改动影响既有挂号建案流程 | 中 | 以 `RegistrationId` 查重是纯增量判断；E2E `RegistrationE2ETests` / `ConsultationE2ETests` 覆盖 |
| 去除非原子的「原子」声明引发误读 | 低 | 文档 + ADR 如实描述 |

## 5. 实施步骤

1. 出 ADR（事务策略：同 DbContext 显式事务 + 跨 DbContext 补偿/幂等）。
2. 纠正 `StartVisitCommandHandler` 注释与真实语义（先文档后代码）。
3. 跨模块建案幂等化 + 失败补偿。
4. 同 DbContext 多聚合写入补显式事务。
5. build + Registration/MedicalCase 集成测试。
