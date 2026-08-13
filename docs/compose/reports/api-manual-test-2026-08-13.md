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

## Domain 2: Catalog（⚠️ 发现 bug：PUT formula 500）

| # | 测试项 | 预期 | 实际 | 结论 |
|---|--------|------|------|:---:|
| 1 | herb CRUD（创建/查询/更新/禁用/删除） | 全 2xx | 201/200/200/200/200 | ✅ |
| 2 | herb 业务规则（禁用后 restore → 422） | 422 | 422「未被删除无需恢复」 | ✅ |
| 3 | 不存在 herb ID → 404 | 404 | 404 | ✅ |
| 4 | formula CRUD（创建含药材项/列表/详情） | 2xx | 201/200/200 | ✅ |
| 5 | **PUT formula → 500** | 200 | **500 稳定复现** | 🔴 **BUG** |

**Bug 详情（PUT /api/v1/formulas/{id} 稳定 500）**：
- 触发：创建方剂（含 herbs 数组）后 PUT 更新（同 body / 改 dosage 都 500）
- 日志证据：`server '${DB_SERVER}'`——连接串读到**占位符**，但 start.sh 环境变量值正确（进程 env 已实证）
- **根因推断**：ConfigurationPostProcessor 在 ConfigurationManager（生产真实类型）下执行时，`root.Providers` 快照不含刚 Add 的环境变量 provider → 误判连接串无效 → 回退到 appsettings.json 模板占位符
- **测试盲区**：PostProcessorTests 用自定义 ConfigurationRoot（非 ConfigurationManager）——测试类型 ≠ 生产类型（方案 §八 明确「生产用 ConfigurationManager」但测试没跟上）
- **待修复**：派发 omp（PostProcessor 适配 ConfigurationManager + 补真实类型测试）

**沉淀**：真机测试价值再次体现——组件测试全绿（PostProcessor 单测过）但系统集成暴露类型差异 bug。这正是「层级错配」教训的第三次验证（InMemory→SQLite→ConfigurationRoot→ConfigurationManager）。

## Domain 3+: 待继续（等 BUG 修复后重测 formula 更新）

