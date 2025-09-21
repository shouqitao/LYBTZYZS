# 解决方案架构优化方案（All 层面）

## 一、设计目标与原则
- 安全基线优先：最小权限、最少暴露、可鉴别加密、防敏感输出。
- 配置一致性：策略集中配置化，运行时与配置一致，不硬编码。
- 可回滚可验证：分阶段发布、门禁与单测覆盖、清晰回滚路径。
- 稳健演进：优先修复高风险，再做治理与体验优化。

## 二、优化路线图（分阶段）
- Phase 1（安全基线）：统一授权/匿名、JWT Claims 一致、日志脱敏、密钥治理、密码校验修复、健康详情收口、生产 CSP 收紧。
- Phase 2（配置一致）：限流策略改为配置驱动；实现 ClaimsNormalization；统一安全选项落地；完善环境感知校验。
- Phase 3（桌面安全）：本地安全配置采用 AEAD/HMAC；KDF 强化+随机盐；DPAPI 保护主密钥；凭据服务统一；认证头与错误复制脱敏；Debug 证书护栏；诊断输出控制。
- Phase 4（可观测）：限流/拒绝/异常维度监控；审计 ID 贯通；关键路径指标。
- Phase 5（性能/稳态）：缓存/数据库参数复盘，关键接口限流与熔断优化。

## 三、服务端（Server）优化
1) 统一授权与匿名
- 变更：
  - 在服务注册启用 `DefaultPolicy/FallbackPolicy.RequireAuthenticatedUser()`（参考：`src/Server/Core/LYBT.Infrastructure/Authorization/AuthorizationPolicyExtensions.cs`）。
  - 对登录、Token 校验（GET）与基础健康检查增加 `[AllowAnonymous]`；其余敏感端点细化为 `[Authorize(Roles = "Admin")]`。
- 影响：未登录默认 401/403；减少漏网端点。

2) JWT Claims 一致与操作人解析
- 变更：
  - 令牌增加 `ClaimTypes.NameIdentifier/Name/Role`（参考：`JwtAuthenticationService`）。
  - `BaseControllerCore.GetOperator()` 兼容读取 `JwtRegisteredClaimNames.Sub/UniqueName` 与 `ClaimTypes.*`，并以 `ClaimTypes.Role` 为准。
- 影响：解决审计链条与操作人信息缺失问题。

3) 日志脱敏
- 变更：
  - 控制器调用 `HandleException(...)` 不再传入含密码/Token 的请求体；统一传“脱敏上下文”。
  - 在 `BaseControllerCore` 内对常见敏感字段（Password/NewPassword/OldPassword/Token/Secret）做统一脱敏。
- 影响：降低敏感信息写入日志的风险。

4) 限流配置化
- 变更：
  - `ConfigureRateLimiting` 从 `SecurityOptions.RateLimit` 绑定策略，不再硬编码；保留登录策略对白名单/上限的差异化。
- 影响：线上策略改动不需要代码变更，环境可控。

5) 健康详情接口收口
- 变更：
  - `/health/details` 生产仅授权访问；基础 `/health`/`/ping` 可匿名且最小化输出。
- 影响：降低信息泄露面。

6) 生产 CSP 收紧
- 变更：
  - 移除 `'unsafe-inline'/'unsafe-eval'`，按实际前端脚本需求配置严格 CSP；Swagger 仅非生产启用。

7) 密钥治理
- 变更：
  - 删除并清理历史 `.encryption-key`；旋转密钥；仅通过环境变量/密管注入。

8) 密码校验修复与复杂度
- 变更：
  - 修复 `PasswordHelper.Verify` 传参与顺序；在重置/修改密码时接入 `SecurityOptions.PasswordPolicy` 或 `PasswordHelper.ValidatePassword`。

9) ClaimsNormalization 落地
- 变更：
  - 在 `Infrastructure/Authorization` 或 `WebAPI` 扩展中实现标准化（去重/映射），减少角色串不一致。

## 四、桌面端（Client/Desktop）优化
1) 本地安全配置改造为 AEAD/HMAC
- 变更：
  - `SecureConfigurationService` 采用 AES-GCM 或 “AES-CBC + HMAC-SHA256（密钥分离）”；弃用明文 checksum。
  - KDF 迭代 ≥100k，随机盐 per-record；旧数据迁移为新格式。
- 影响：可检测篡改，提升密码学强度。

2) 主密钥 DPAPI 化
- 变更：
  - 使用 Windows DPAPI（CurrentUser）保护主密钥/KEK 并持久化；启动解封装。
  - 可选支持用户口令作为附加因子。
- 影响：跨用户隔离，提升安全边界。

3) 凭据服务统一
- 变更：
  - DI 仅注册 `SecureCredentialService`；`CredentialService` 标记弃用/迁移；删除固定 Entropy；增加擦写删除。

4) 认证头日志脱敏
- 变更：
  - 移除 Bearer 片段输出，仅显示 Token 存在/空的布尔提示。

5) 错误复制脱敏
- 变更：
  - `BuildErrorSummary()` 对 `Authorization/Token/Password/ConnectionString` 等键值脱敏；Debug 下可开关原始详情且有明显提示。

6) Debug 证书护栏
- 变更：
  - 保留 `#if DEBUG` 的 `ServerCertificateCustomValidationCallback = true`，但增加注释和启动提示；Release 固化严格校验。

7) 诊断输出控制
- 变更：
  - 仅 Debug 写入诊断文件；默认不包含敏感数据；提供“一键清理”。

## 五、落地计划与责任分配
- 安全基线（S0）：Server 安全（平台组）+ Desktop 安全（客户端组）同步推进，先提交 PRD 与任务清单。
- 配置一致（S1）：平台组负责限流/CSP/策略统一；客户端组负责 DI/配置清理。
- 迁移与回滚：提供“旧格式读取→新格式写入”的一次性迁移逻辑及配置开关，失败可降级只读旧格式。

## 六、测试与门禁
- 单测：覆盖授权/匿名、JWT 声明解析、日志脱敏、限流配置化、CSP、Desktop 本地加密与 KDF/盐、DPAPI 保护、认证头与错误复制脱敏、诊断输出控制。
- 架构门禁：保持 `tests/Architecture/ArchTests.cs` 全部通过；新增 Header/CSP/限流检查（可选）。
- 覆盖率：关键路径（认证/授权/错误/限流/本地存储）新增断言。

## 七、回滚策略
- 授权/匿名：配置开关回退 Default/FallbackPolicy；
- 限流：回退到硬编码默认（短期应急）；
- Desktop 本地加密：保留旧格式读取开关；
- CSP：可通过配置回退到宽松策略（不推荐长期）。

## 八、验收标准（摘要）
- 未登录访问敏感端点统一 401/403；
- 生产 `/health/details` 需授权；
- 日志与剪贴板不含密码/令牌/密钥/连接串；
- JWT 操作人解析稳定；
- 限流参数改配置即生效；
- Desktop 本地配置篡改被检测；
- Debug/Release 证书行为与诊断输出符合预期。

## 九、风险与缓解
- 历史数据迁移风险：提供回滚开关与备份导出；
- 授权策略启用影响面大：灰度/金丝雀发布，基于路由清单做预演；
- CSP 收紧引发脚本受限：分环境验证，逐步上线。

## 十、里程碑与交付物
- M1（S0+S1）：安全基线 + 配置一致 PR 合并与测试完成；
- M2（S2）：桌面本地安全栈改造、凭据统一；
- M3（S3）：可观测与性能优化；
- 交付物：补丁、配置样例、迁移工具、运维与回滚文档、测试报告。

