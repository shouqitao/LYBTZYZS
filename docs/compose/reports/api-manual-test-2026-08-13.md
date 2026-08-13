# API 真机测试报告（增量记录）

> 目标：60.190.215.86:5000（Production 环境名部署验证）
> 依据：docs/compose/reports/webapi-test-strategy-2026-08-12.md（L3/L4 真机形态）
> 方法：边沉淀边测试——每域完成即记录

## Domain 1: Identity（✅ 全通过）

| # | 测试项 | 预期 | 实际 | 结论 |
|---|--------|------|------|:---:|
| 1 | login（sysadmin 正确密码） | 200+JWT | 200 token=448 | ✅ |
| 2 | validate（token 有效） | 200 | 200「Token验证成功」 | ✅ |
| 3 | refresh（refreshToken） | 200 新 token | 200 data.token | ✅ |
| 4 | GET /users 分页 | 200+列表 | 200 totalCount=8 | ✅ |
| 5 | reset-password on sysadmin | 拒绝 | 422「系统管理员密码不可被重置」 | ✅ 安全保护 |
| 6 | create user + login 新用户 | 200+可登录 | 200 创建→200 登录 | ✅ |
| 7 | delete test user（清理） | 200 | 200 删除成功 | ✅ |

**沉淀**：
- 认证流完整（login→validate→refresh→logout 待测）
- sysadmin 密码保护真实生效（T5-1 #12）
- 测试用户创建→登录→删除闭环验证（不污染数据）

## Domain 2: Catalog（🔴 两个 bug：PUT formula 500——PostProcessor 误判 + RowVersion 并发）

### Bug 1（已修）：PostProcessor-ConfigurationManager 兼容
- 已修复（df49d7ba9 + 640da18b3）：PostProcessor 在 ConfigurationManager 下读不到新 Add 的 env provider → 误回退占位符
- 补 3 个真实 ConfigurationManager 测试（复现原场景）
- **但部署后 PUT formula 仍 500**——说明还有 Bug 2

### Bug 2（✅ 已修复 2026-08-13——两层根因）：DbUpdateConcurrencyException（RowVersion 并发）
- 日志证据：`System.InvalidOperationException: 数据已被其他用户修改` → `DbUpdateConcurrencyException: expected 1 row, affected 0`
- 触发：PUT /api/v1/formulas/{id}（同 body / 改 dosage 都稳定复现）
- **第 1 层根因**（6ecd7536d）：`BaseRepository.UpdateAsync` 的 `_dbSet.Update(entity)` 对已跟踪实体全标记 Modified（含 RowVersion）——已修复：已跟踪实体只 SaveChanges
- **第 2 层根因**（049d0e0e2，EF SQL 日志实证）：`ReplaceHerbs` 的新 `FormulaHerbItem` 被 EF **误标 Modified**（非 Added）→ `UPDATE FormulaHerbItems WHERE Id=新Guid` → 0 rows——已修复：① ReplaceHerbs 孤儿删除模式（只设 FormulaId 不设导航）；② `FormulaRepository.UpdateAsync` override 强制新 item `EntityState.Added`；③ BaseRepository 并发重试（真并发仍抛——乐观语义保持）
- 复现测试：`FormulaReplaceHerbsTests` 2 用例（真实 DbContext+SQLite——与真机异常一致）+ `HerbUpdateRowVersionTests` 2
- **真实 SQL Server 验证（2026-08-13 formula-realsql-fix——SQLite 通过不算数）**：`FormulaRealSqlUpdateTests` 2 用例（连接 192.168.190.243 LYBTDB_Test——TEST_DB_CONNECTION 激活）——`UpdateFormula_ReplaceHerbs_OnRealSqlServer_PersistsWithoutConcurrencyError`（764ms：EnsureCreated+建删数据）与 `UpdateFormula_DetachedReplaceHerbs_OnRealSqlServer_InsertsNewItems` **2/2 通过**——替换 herbs 无并发冲突（新 item INSERT 非 UPDATE WHERE 新 Id）+ Detached 场景正确 INSERT
- **引用校验缺口修复（同日）**：空 herbs → Validator `NotEmpty`（400）；herbId 不存在/已删除 → `ValidateBeforeSaveAsync` 引用校验（`FormulaValidationFailed` 60004 → **422**）——`FormulaReferenceValidationTests` 3 用例
- **深挖 3（formula-deep-fix——Dev 非空库权威）**：
  - **Dev 真实数据复现**：`FormulaDevReproTests` 连 **LYBTDB_Dev**（26 患者/15 挂号等真实数据——.env.development 连接串）——取真实含 herbs 验方 → 同 herbs 回传 ReplaceHerbs → UpdateAsync——**REPRO-NEGATIVE（更新成功无并发异常）**——SQL 证据：`DELETE FROM FormulaHerbItems` → `UPDATE Formulas SET UpdatedAt...OUTPUT INSERTED.RowVersion` → `INSERT INTO FormulaHerbItems`——当前代码在 Dev 数据上正确
  - **部署滞后铁证**：真机 dll 03:52 vs bca7bccf0 提交 11:49——真机 PUT 500/空 herbs 201/herbId 500FK **全是旧 dll 行为**（修复代码未部署）
  - **重写 FormulaRepository.UpdateAsync**（用户授权大范围）：显式 DbSet 模式——旧 items 按 FormulaId `RemoveRange`（未跟踪自动 Attach+Deleted）+ 新 items 显式 `Add`（强制 Added）——**根治 EF 对 item 状态的隐式误判**（不再依赖 Detached/Modified 状态判断）；父行走 base（Attached 只 SaveChanges——RowVersion 正确；Detached 保留 Update()）——乐观并发保持
  - **钩子未拦截根因**：本地 DI 注册测试证明 `CreateFormulaValidator` 已注册 + ValidationBehavior 拒绝空 herbs（`FormulaValidatorRegistrationTests` 2 通过）——真机 201 = 旧 dll 无此代码

**沉淀**：真机测试连续发现 2 个 bug——①PostProcessor 测试类型≠生产类型（ConfigurationRoot vs ConfigurationManager）②通用更新命令的并发令牌处理（RowVersion 被 Update 标记）。两个都是「测试自洽但系统不跑」的层级错配实例——单元测试用 mock/fake 永远测不到 EF 真实并发语义。

### Bug 2 修复后真机复查（2026-08-13 03:20，PID 730647 部署 049d0e0e2）：
- PUT herb → 200 ✅（第 1 层修复生效）
- **PUT formula → 仍 500 并发冲突** 🔴
- 修复测试（FormulaReplaceHerbsTests 用 SQLite）通过，但**真实 SQL Server 仍失败**——SQLite vs SQL Server 的 EF 行为差异
- **新增发现（测试矩阵 §11.2）**：
  - #2 空 herbs → 201（预期 400）❌ AC 未拦截
  - #3 herbId 不存在 → 500（预期 422）❌ 引用校验异常
  - #4 herbId 已删除 → 201（预期 422）❌ 引用校验缺失
- **待 omp**：真实 SQL Server 调试（非 SQLite）+ 修复 #2/#3/#4 校验

## Domain 3+: 待继续（等 Bug 2 真机修复 + 校验修复后重测）



