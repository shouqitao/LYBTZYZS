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

### Bug 2（待修）：DbUpdateConcurrencyException（RowVersion 并发）
- 日志证据：`System.InvalidOperationException: 数据已被其他用户修改` → `DbUpdateConcurrencyException: expected 1 row, affected 0`
- 触发：PUT /api/v1/formulas/{id}（同 body / 改 dosage 都稳定复现）
- 代码链：`CatalogEntityCommandHandlerBase.Handle(Update)` → `ApplyUpdate`（UpdateProfile + ReplaceHerbs）→ `BaseRepository.UpdateAsync`（`_dbSet.Update(entity)`）
- 疑点：`_dbSet.Update(entity)` 对已跟踪实体标记全部 Modified（含 RowVersion 并发令牌）→ EF 用内存中的 RowVersion 作 WHERE → 与库中原值不匹配 → 0 rows
- **待 omp 深入**：确认 _dbSet.Update 对已跟踪实体的并发令牌处理，修复通用 UpdateEntityCommandHandler（应避免 Update 已跟踪实体，或刷新 RowVersion）

**沉淀**：真机测试连续发现 2 个 bug——①PostProcessor 测试类型≠生产类型（ConfigurationRoot vs ConfigurationManager）②通用更新命令的并发令牌处理（RowVersion 被 Update 标记）。两个都是「测试自洽但系统不跑」的层级错配实例——单元测试用 mock/fake 永远测不到 EF 真实并发语义。

## Domain 3+: 待继续（等 Bug 2 修复后重测 formula 更新）


