# ADR: 跨聚合写入一致性策略（同上下文显式事务 + 跨上下文幂等/补偿）

> 状态: Accepted | 日期: 2026-09-16 | 相关: 设计 03（设计 03 跨聚合事务设计稿（已清理，git 历史可查））、ADR-0017（模块化单体各自 DbContext）

## 背景

`StartVisitCommandHandler` 的类注释声称「**原子事务**：Registration.Status=InProgress + 创建 MedicalCase(Active)」。实测该声明**不成立**：

- `_repository.BeginTransactionAsync` 开在 **`RegistrationDbContext`** 的连接上；
- 事务内 `_medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync(...)` → `IMedicalCaseCommandService.SaveAsync` → **`MedicalCaseDbContext`**（模块自有 DbContext，ADR-0017）；
- 两个 DbContext 使用**不同连接**，`RegistrationDbContext` 上的事务不可能覆盖医案写入。医案写失败或挂号提交失败时不会一起回滚。

其余跨聚合写入同样无事务：`MedicalCaseStateService`（完成/取消 → 联动挂号，先提交本聚合再联动）、`MedicalCaseCommandService.Creation`（创建医案后另库联动挂号）。全仓仅此 1 处 `BeginTransaction`。

根因是 ADR-0017 的模块隔离本身：跨模块写入天然跨连接，单个 EF 事务无法覆盖。

## 决策

1. **同 DbContext 内的多聚合写入 → 显式事务**：用 `Database.BeginTransactionAsync` 包裹，提交由调用方控制。
2. **跨 DbContext 写入 → 不追求原子，改为「幂等 + 补偿」**：
   - **幂等**：跨模块创建医案以 `RegistrationId` 为幂等键，已存在则复用而非重复创建。
   - **补偿**：后续步骤失败时显式回滚/补偿本聚合（`StartVisitCommandHandler` 已有 `RevertToWaiting()` 雏形），并把失败如实返回调用方，**禁止静默成功**。
   - 明确语义：返回失败 + 记录审计，由用户重试；幂等键保证重试安全。
3. **不采用 `TransactionScope`**：跨连接会升级为分布式事务（MSDTC），诊所单机/小服务器场景运维成本高、易踩防火墙与权限坑。
4. **不采用「跨模块共享 DbContext」**：破坏 ADR-0017 模块隔离，牵连面过大。
5. `StartVisitCommandHandler` 的注释改为如实描述「两步写 + 补偿 + 幂等」，删除「原子事务」表述。

## 后果

- **正向**：消除「注释声称原子、实为非原子」的误导；跨模块写入在失败时行为可预期、可重试；同上下文写入获得真正的原子性。
- **代价**：跨模块写入存在**短时不一致窗口**（第一步已提交、第二步未完成）；由幂等键 + 补偿 + 失败返回覆盖，而非数据库级原子性。
- **风险**：补偿逻辑自身出错会导致半写状态 —— 补偿路径必须有失败注入测试覆盖。
