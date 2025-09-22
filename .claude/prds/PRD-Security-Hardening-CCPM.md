# PRD：后端安全加固（CCPM）

## 项目背景与目标
- 背景：当前服务存在密钥泄露、授权边界过宽、敏感日志、JWT 声明与解析不一致、密码校验缺陷、限流与配置漂移、生产 CSP 偏宽等安全风险。
- 目标：通过一轮“最小可行”安全加固，修复高风险缺陷，统一授权与审计基线，降低滥用与泄露风险，满足生产环境安全要求，且不改变对外 API 合同与业务功能。

## 范围与非范围
- 范围：
  - WebAPI（认证/授权、异常与日志、限流、健康检查、CSP、安全配置）
  - Users/Auth 相关业务服务与控制器
  - 配置与选项绑定（SecurityOptions/RateLimit/PasswordPolicy）
- 非范围：
  - 引入刷新令牌持久化与轮换机制（仅提出建议，不纳入本次变更）
  - 跨服务 SSO、OAuth2/OpenID Connect 改造
  - 前端与桌面客户端改造

## 业务价值与成功指标（北极星）
- 未授权访问率=0：高敏接口未登录/越权访问统一拒绝。
- 日志零敏感：日志中不出现密码/令牌/密钥等敏感字段。
- 配置即代码一致：限流/安全头以配置为准，修改立即生效。
- 生产严格模式：HTTPS/HSTS、禁用敏感日志、CSP 收紧上线。
- 门禁与测试：通过现有架构门禁与单测，新增安全用例 100% 通过。

## 功能需求（Epics / Stories）

### Epic A：密钥治理与轮换
- 必须：
  - 删除并清理历史中的泄露文件；仅经环境变量注入密钥；立即轮换泄露密钥。
- 变更点：
  - 删除 `src/Server/Services/LYBT.WebAPI/.encryption-key` 并进行历史清理（运维流程）；继续使用 `appsettings.Security.json` 的 `${ENCRYPTION_KEY}`。
- AC（验收标准）：
  - 仓库不再跟踪密钥文件；秘密扫描通过；应用仅从环境变量读取；完成密钥轮换记录。

### Epic B：授权边界加固（最小授权）
- 必须：
  - 高敏操作强制 `Admin` 角色；登录/基础健康检查显式匿名；启用全局回退授权策略。
- 变更点：
  - `AuthController`：
    - `POST /api/v1/Auth/changeSysAdminPassword` 增加 `[Authorize(Roles = "Admin")]`。
    - `POST /api/v1/Auth/login`、`GET /api/v1/Auth/validate` 标注 `[AllowAnonymous]`。
  - `UsersOperationController`：
    - 批量启用/禁用、重置/修改密码等端点加 `[Authorize(Roles = "Admin")]`。
    - 如保留自助“改自己的密码/资料”，需校验 `userId == 当前用户` 并使用更细粒度策略。
  - 在服务注册中启用 `DefaultPolicy/FallbackPolicy`（要求认证），需要匿名的端点显式 `[AllowAnonymous]`。
- AC：
  - 未登录访问高敏端点返回 401/403；普通用户访问管理端点返回 403；登录与基础健康检查匿名可用；架构门禁通过。

### Epic C：日志脱敏（防敏感信息入库）
- 必须：
  - 控制器异常处理传入脱敏后的上下文；底座统一对常见敏感字段名做红action（密码/令牌/密钥）。
- 变更点：
  - `AuthController`/`UsersOperationController` 中 `HandleException(...)` 第三参替换为脱敏对象，仅含必要非敏字段（如 `Username` 或 `userId`）。
  - 在 `BaseControllerCore.HandleExceptionCore(...)` 中，对 `Password/NewPassword/OldPassword/Token/Secret` 等字段名做统一脱敏（值统一为 `******`）。
- AC：
  - 触发异常时日志不出现密码/令牌/密钥字段；脱敏覆盖通用敏感字段名。

### Epic D：JWT Claims 与操作人解析一致
- 必须：
  - 令牌内增加 `ClaimTypes.NameIdentifier`、`ClaimTypes.Name`、`ClaimTypes.Role`（与 `sub/unique_name` 并存）；操作人解析兼容两种来源。
- 变更点：
  - `JwtAuthenticationService.GenerateToken(...)` 添加上述 `ClaimTypes.*`。
  - `BaseControllerCore.GetOperator()` 兼容读取 `JwtRegisteredClaimNames.Sub/UniqueName` 与 `ClaimTypes.NameIdentifier/Name`，并以 `ClaimTypes.Role` 为准。
- AC：
  - 新令牌下可正确解析 `OperatorId/Name/Role`；审计日志包含操作人信息（不含敏感）。

### Epic E：修复密码验证缺陷
- 必须：
  - 修复 `PasswordHelper.Verify(hash, password)` 参数顺序错误导致旧密码校验失败的问题。
- 变更点：
  - 将 `Verify(oldPassword, user.PasswordHash)` 改为 `Verify(user.PasswordHash, oldPassword)`。
- AC：
  - 单测覆盖旧密码正确/错误两路径，行为符合预期。

### Epic F：密码复杂度统一校验
- 必须：
  - 重置/修改密码接入 `SecurityOptions.PasswordPolicy` 或 `PasswordHelper.ValidatePassword(...)`；生产最小 12 位，含大小写/数字/特殊字符。
- 变更点：
  - 在 Users 业务的重置/修改密码逻辑中增加复杂度校验，错误信息一致；开发/生产通过环境感知策略区分。
- AC：
  - 弱密码被拒并返回友好提示；生产策略受配置约束；新增单测通过。

### Epic G：限流配置与运行时一致
- 必须：
  - 限流从 `SecurityOptions.RateLimit` 配置加载，去除硬编码；登录策略的内网白名单仍有绝对上限与审计。
- 变更点：
  - `ConfigureRateLimiting` 改为读取 `SecurityOptions.RateLimit`，并相应调整策略实现。
- AC：
  - 修改配置后限流参数在运行时生效；登录端点限流与白名单策略有效。

### Epic H：健康检查信息最小化（生产）
- 必须：
  - 生产禁止匿名访问 `/health/details`；保留 `/health` 或 `/ping` 最小输出匿名可用。
- 变更点：
  - 在 `HealthController` 内根据环境对 `details` 端点加授权；基础健康端点保持匿名。
- AC：
  - 生产环境 `details` 需认证访问；匿名仅能访问最小健康信息。

### Epic I：CSP 生产收紧
- 必须：
  - 生产环境移除 `script/style` 的 `'unsafe-inline'`、`'unsafe-eval'`；Swagger 保持非生产可用。
- 变更点：
  - `appsettings.Security.json` 中 `Security.SecurityHeaders.ContentSecurityPolicy` 调整为严格策略（生产）。
- AC：
  - 响应头含严格 CSP；非生产 Swagger 正常；线上自测不阻断 API。

## 非功能 / 安全要求
- 生产必须启用 HTTPS/HSTS；禁用敏感数据日志；环境感知校验不可绕过。
- 不引入被基线禁止的框架；遵守 `/api/v1/*` 路由规范。
- 与现有 `.editorconfig`、StyleCop、一致的代码风格与命名规范。

## 关键链（CCPM）计划
- 任务依赖（主关键链）：
  1. C 日志脱敏（底座防护）
  2. D JWT 一致性（声明/解析）
  3. B 授权加固（全局策略+端点）
  4. H 健康检查授权最小化
  5. G 限流配置绑定
  6. I CSP 收紧
- 喂入链：
  - E→F（密码验证修复→复杂度校验）
  - A（密钥轮换）与主链并行推进，但发布窗口对齐最终上线。
- 缓冲：
  - 项目缓冲：关键链总工期的 30%
  - 喂入缓冲：各喂入链工期总和的 20%
- 里程碑：
  - M1：底座脱敏就绪（C）
  - M2：JWT 与授权上线（D、B、H）
  - M3：密码路径修复与策略生效（E、F）
  - M4：限流与 CSP 配置收敛（G、I）
  - M5：密钥轮换完成（A）

## 技术要点与实现约束
- 使用现有 `AuthorizationPolicyExtensions` 接通 `Default/FallbackPolicy`；对登录/基础健康检查加 `[AllowAnonymous]`。
- 在 `BaseControllerCore` 实现统一字段脱敏；控制器异常上下文不传递敏感 DTO。
- 令牌生成添加 `ClaimTypes.*`；解析兼容 `JwtRegisteredClaimNames` 与 `ClaimTypes`。
- 限流读取 `SecurityOptions.RateLimit`，登录白名单需保留上限与审计能力。
- 生产 CSP 严格，Swagger 仅非生产启用。

## 验收标准（抽样关键用例）
- 未登录访问 `POST /api/v1/users/operation/batch-disable` 返回 401/403。
- 普通用户访问 `POST /api/v1/auth/changeSysAdminPassword` 返回 403。
- 日志中不出现 `Password/NewPassword/OldPassword/Token/Secret` 值。
- 新 JWT 令牌可正确解析 `OperatorId/Name/Role`，不抛未授权异常。
- 修改密码：旧密码错误被拒、弱新密码被拒、强新密码成功。
- 修改 `Security.RateLimit` 参数后运行时生效。
- 生产访问 `/api/v1/health/details` 需授权；匿名仅可调用 `/api/v1/health` 或 `/api/v1/health/ping`。
- 响应头 CSP 严格，不含 `unsafe-inline/unsafe-eval`。

## 风险与缓解
- 启用全局授权策略可能影响遗漏 `[AllowAnonymous]` 的端点：
  - 预演路由清单+回归用例；灰度逐步放量，支持快速回滚。
- CSP 收紧导致第三方脚本报错：
  - 环境分级验证，仅生产收紧；CSP 可通过配置开关回滚。
- 密钥轮换时机与兼容：
  - 预留双活密钥期（如有必要），全节点同步完成后切换。

## 发布与回滚
- 分里程碑分批发布：先底座与 JWT/授权，再密码与限流，最后 CSP 与密钥轮换。
- 回滚：
  - 通过配置快速关闭 FallbackPolicy、恢复旧 CSP、回退限流参数；代码回滚预案准备。

## 监控与度量
- 认证/授权：401/403 比率，越权访问拦截率。
- 安全与稳定性：异常率、限流命中率、健康检查访问来源与频次。
- 日志审计：敏感字段采样为 0，操作人审计记录完整。
- CI 门禁：架构测试、单元测试、安全扫描（密钥扫描/Headers 校验）通过。

## 单元测试与验证计划
- 总体要求：
  - 新增安全相关单测/集成用例，覆盖本 PRD 的关键行为；保持现有架构测试与单测全部通过。
  - 测试命名以 `*Tests.cs` 结尾，按模块放置于 `tests/` 目录（与现有结构一致）。
  - 日志相关用例使用可注入的内存日志记录器/测试 sink，避免实际落盘。

- Epic A（密钥治理）
  - CI 秘密扫描规则校验：仓库不存在 `.encryption-key` 被跟踪；提交 PR 时自动扫描失败率为 0。
  - 配置验证：`ENCRYPTION_KEY` 缺失时生产启动失败的单测（期望抛出 `InvalidOperationException`）。

- Epic B（授权边界加固）
  - 控制器授权：
    - 未登录访问 `POST /api/v1/auth/changeSysAdminPassword` → 401/403。
    - 普通用户访问同端点 → 403；Admin 访问 → 200。
    - `POST /api/v1/auth/login`、`GET /api/v1/auth/validate` 在匿名下 → 200。
  - 全局策略：启用 `FallbackPolicy` 后，随机业务控制器默认需要认证（无 `[AllowAnonymous]` 时返回 401）。

- Epic C（日志脱敏）
  - 当 `AuthController.LoginAsync` 抛异常时，采集的日志不包含 `Password/NewPassword/OldPassword/Token/Secret` 的明文值（仅出现 `******` 或字段被移除）。
  - `UsersOperationController.ChangePassword/ResetPassword` 异常上下文日志同样通过脱敏断言。

- Epic D（JWT 声明一致性）
  - `JwtAuthenticationService.GenerateToken` 生成的令牌包含：`sub/unique_name` 与 `ClaimTypes.NameIdentifier/Name/Role`。
  - `BaseControllerCore.GetOperator()` 能在两种声明来源下正确解析 `OperatorId/Name/Role`。

- Epic E（密码验证缺陷修复）
  - 旧密码错误 → `ChangePasswordAsync` 返回失败；旧密码正确 → 成功。
  - 对比修复前后（模拟）验证参数顺序正确性，确保不会误判。

- Epic F（密码复杂度统一校验）
  - 重置/修改密码在生产策略下：
    - 长度 < 12 或缺少大小写/数字/特殊字符 → 失败并返回提示。
    - 满足策略 → 成功。
  - 开发环境下按配置放宽策略的差异性验证。

- Epic G（限流配置绑定）
  - 修改 `SecurityOptions.RateLimit` 配置后，构建的 limiter 使用新值（通过读取内置选项或探测限流响应头/响应码断言）。
  - 登录端点内网白名单放宽但仍受绝对上限控制（压测式最小集成测试，断言429）。

- Epic H（健康检查最小化）
  - 生产环境模拟下：
    - 匿名访问 `/api/v1/health/details` → 401/403；认证访问 → 200。
  - 开发环境：匿名访问 `details` 可 200（或按策略定义）。

- Epic I（CSP 生产收紧）
  - 生产环境响应头包含严格的 `Content-Security-Policy`，且不含 `unsafe-inline/unsafe-eval`。
  - 非生产环境 Swagger 开启且不受生产 CSP 限制。

- 回归与门禁
  - 现有架构门禁 `tests/Architecture/ArchTests.cs` 需全部通过。
  - 涉及接口签名的变更不得影响既有公共 API 合同（路由/DTO/状态码语义），除授权行为强化外。

## 交付物与变更清单
- 控制器：
  - `AuthController`、`UsersOperationController` 增加/细化授权特性，异常上下文脱敏。
- 认证与授权：
  - `JwtAuthenticationService` 增加标准 `ClaimTypes`。
  - `BaseControllerCore` 操作人解析与字段脱敏。
  - 启用统一 `Default/FallbackPolicy`。
- 业务逻辑：
  - `UserBusinessService` 修复密码校验顺序、接入复杂度策略。
- 中间件与配置：
  - 限流从 `SecurityOptions.RateLimit` 绑定；生产 CSP 收紧。
- 运维文档：
  - 密钥历史清理与轮换步骤说明；变更影响与回滚指南。

> 备注：本 PRD 为一次“安全基线加固”的聚焦发布，遵循最小变更原则，优先修复高风险缺陷；刷新令牌持久化与轮换等中长期能力将作为后续独立 Epic 规划。
