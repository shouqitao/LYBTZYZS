# PRD：生产级安全与治理加固（CCPM — 全量方案）

## 一、项目背景与目标
- 背景：当前后端与桌面端已实现基础功能，但存在生产级别的安全与治理风险（授权边界、JWT 声明一致性、日志脱敏、限流配置化、CSP、密钥治理、客户端本地加密与凭据统一、Debug 行为约束、诊断输出等）。
- 目标：以最小可行变更完成生产就绪的安全与治理加固，统一配置与策略，降低越权与敏感信息泄露风险，建立可观测与回滚能力，不改变外部 API 合同与主要业务体验。

## 二、范围与非范围
- 范围：
  - Server（WebAPI/Modules/Infrastructure）安全与治理
  - Desktop（WPF 客户端）本地安全与HTTP交互治理
  - 生产运维准备（密钥、配置、迁移、观测、告警、回滚）
- 非范围：
  - 刷新令牌持久化与轮换体系（后续独立 Epic）
  - 跨端 SSO/OIDC 重构
  - 业务功能大改动

## 三、业务价值与成功指标（北极星）
- 未授权访问率=0：对高敏端点未登录/越权请求统一拒绝。
- 日志/剪贴板敏感采样=0：不含 Token/密码/密钥/连接串等。
- 配置一致：限流/安全头等策略以配置为准，变更即生效。
- 生产严格：HTTPS/HSTS/CSP/敏感日志关闭；密钥不落盘，仅来自密管/环境变量。
- 稳定观测：关键路径异常率下降、限流命中可见、审计链条完整。

## 四、功能需求（Epics / Stories）

### A. 服务端（Server）安全与治理
1) 统一授权与匿名
- 必须：启用 `DefaultPolicy/FallbackPolicy.RequireAuthenticatedUser()`；登录/Token 校验（GET）/基础健康检查标注 `[AllowAnonymous]`；高敏端点细化 `[Authorize(Roles = "Admin")]`。
- 变更点：`Infrastructure/Authorization/AuthorizationPolicyExtensions.cs`、`WebAPI` 服务注册、`AuthController`、`HealthController`、高敏控制器。
- AC：未登录访问敏感端点返回 401/403；匿名端点最小化暴露。

2) JWT Claims 一致与操作人解析
- 必须：令牌加入 `ClaimTypes.NameIdentifier/Name/Role`；`GetOperator()` 兼容 `JwtRegisteredClaimNames` 与 `ClaimTypes`。
- 变更点：`JwtAuthenticationService`、`BaseControllerCore`。
- AC：日志与审计可稳定解析 `OperatorId/Name/Role`。

3) 日志脱敏
- 必须：控制器异常处理不传递敏感 DTO；底座针对 `Password/NewPassword/OldPassword/Token/Secret` 做统一脱敏。
- 变更点：各控制器 `HandleException(...)` 调用、`BaseControllerCore`。
- AC：异常日志不含敏感字段值。

4) 限流配置化
- 必须：从 `SecurityOptions.RateLimit` 绑定生成 limiter（全局/登录策略含白名单与上限）。
- 变更点：`UnifiedServiceRegistration.ConfigureRateLimiting`。
- AC：修改配置后策略即时生效，行为与配置一致。

5) 生产 CSP 收紧
- 必须：生产移除 `'unsafe-inline'/'unsafe-eval'`；Swagger 仅非生产启用。
- 变更点：`appsettings.Security.json`、安全头中间件。
- AC：响应头含严格 CSP，不影响非生产调试。

6) 密钥治理
- 必须：删除并清理历史 `.encryption-key`；密钥旋转；仅环境变量/密管注入。
- AC：密钥扫描 0 警告；运行仅依赖注入密钥。

7) 密码校验修复与复杂度策略
- 必须：修正 `PasswordHelper.Verify(hash, password)` 调用顺序；重置/修改密码接入 `SecurityOptions.PasswordPolicy` 或 `PasswordHelper.ValidatePassword(...)`。
- AC：旧密码校验准确；弱密码拒绝，强密码通过。

8) ClaimsNormalization 落地
- 必须：标准化 Claims（去重/映射），减少角色串不一致。
- AC：角色判定一致，策略匹配稳定。

### B. 桌面端（Desktop）本地安全与交互治理
1) 本地安全配置采用可鉴别加密（AEAD/HMAC）
- 必须：`SecureConfigurationService` 改为 AES-GCM 或 “AES-CBC + HMAC-SHA256（密钥分离）”，解密前校验 Tag/MAC；移除纯 checksum。
- AC：篡改检测有效；正常数据可解。

2) KDF 强化与随机盐
- 必须：PBKDF2 ≥100k 迭代；每条记录随机盐并随数据存储；兼容旧数据迁移。
- AC：新数据含随机盐；读取使用对应盐。

3) 主密钥 DPAPI 化
- 必须：使用 Windows DPAPI（CurrentUser）保护主密钥/KEK 并持久化。
- AC：跨用户不可解密；同用户正常读写；主密钥不明文。

4) 凭据服务统一
- 必须：仅注册 `SecureCredentialService`；弃用 `CredentialService`（固定 Entropy），删除弱实现使用面；擦写删除。
- AC：DI 不再注入弱实现；DPAPI 生效。

5) 认证头与错误复制脱敏
- 必须：移除 Token 片段日志（仅打印存在/空）；复制错误前对 `Authorization/Token/Password/ConnectionString` 等脱敏；Debug 下可开关原始详情并提示。
- AC：无 Token 片段；复制文本无敏感值。

6) Debug 证书校验护栏
- 必须：`#if DEBUG` 仅本机开发使用，启动提示；Release 强校验。
- AC：Debug/Release 行为符合预期。

7) 诊断输出控制
- 必须：Release 不写 Desktop 调试文件；Debug 不含敏感；提供清理机制。
- AC：诊断文件不会泄露敏感。

### C. 运维与生产准备
- 环境检查：使用 `scripts/deploy/verify-env.ps1` 校验 `ENCRYPTION_KEY/JWT_SECRET/ADMIN_DEFAULT_PASSWORD/USER_DEFAULT_PASSWORD` 等。
- 数据库：迁移前备份；灰度执行；回滚脚本可用。
- 日志与告警：Serilog 文件/SQL sink 保留周期；错误/限流/未授权 监控与阈值告警。
- 发布策略：金丝雀/灰度；开关控制（FallbackPolicy、CSP、限流）、可快速回退。
- 安全扫描：密钥扫描、依赖漏洞扫描、基础 SAST 规则（可选）。

## 五、非功能 / 安全要求
- 生产强制 HTTPS/HSTS；禁止敏感数据日志；严格 CSP；
- 策略集中配置化与环境感知校验；
- 不引入被基线禁止的框架；遵循 `/api/v1/*` 路由规范与 StyleCop；
- 桌面端文件权限（ACL）限制仅当前用户可读写。

## 六、CCPM 关键链计划
- 主关键链：
  1. Server 日志脱敏（A3）
  2. JWT 一致 + 操作人解析（A2）
  3. 授权统一与匿名（A1）
  4. 健康详情收口（A5 一并）
  5. 限流配置化（A4）
  6. 生产 CSP 收紧（A5）
  7. 桌面 AEAD/HMAC + KDF + DPAPI（B1/B2/B3）
  8. 桌面凭据统一 + 脱敏 + 证书护栏 + 诊断控制（B4/B5/B6/B7）
- 喂入链：
  - A6 密钥治理（并行推进，合入发布窗口）
  - A7/A8 密码与 ClaimsNormalization（并入关键链中段）
- 缓冲：项目缓冲=关键链工期 30%；喂入缓冲=各喂入链工期 20%。
- 里程碑：
  - M1：Server 基线安全上线（A1–A5）
  - M2：治理补充（A6–A8）
  - M3：Desktop 本地安全栈改造（B1–B3）
  - M4：Desktop 外围治理与护栏（B4–B7）
  - M5：运维与生产准备完成（C）

## 七、单元测试与验证计划
- Server：
  - 授权与匿名：未登录/普通用户/管理员访问敏感端点（401/403/200）；
  - JWT 声明/操作人解析；
  - 日志脱敏断言（拦截 sink 验证不含敏感字段值）；
  - 限流配置化（修改配置→429 行为验证）；
  - CSP 响应头校验（生产/非生产）；
  - 密码校验顺序与复杂度策略；
  - 架构门禁（tests/Architecture/ArchTests.cs）全量通过。
- Desktop：
  - AEAD/HMAC 篡改检测；KDF/随机盐验证；DPAPI 跨用户隔离；
  - DI 仅绑定安全凭据服务；
  - 认证头与错误复制脱敏；
  - Debug/Release 证书行为；
  - 诊断输出控制（Release 不生成；Debug 不含敏感）。

## 八、发布与回滚
- 分阶段灰度发布；先 Server，再 Desktop；
- 配置开关：FallbackPolicy/CSP/限流/旧格式读取回退；
- 失败回滚：恢复上版本二进制与配置，保留迁移前数据库备份。

## 九、风险与缓解
- 历史数据迁移失败：提供旧格式只读开关与备份导出；
- 授权策略启用影响面：基于路由清单预演 + 金丝雀；
- CSP 收紧导致脚本受限：分环境验证，逐步上线；
- 桌面本地加密改造：小批量用户试点，收集兼容性反馈。

## 十、监控与度量
- 认证/授权：401/403 比；
- 限流：429 命中与 Top URLs；
- 异常：服务端/桌面异常趋势；
- 日志：敏感采样=0；
- 行为变化前后对比：平均响应时间、错误率。

## 十一、交付物与变更清单
- 代码补丁（Server & Desktop）；
- 配置样例与变更说明（CSP/RateLimit/PasswordPolicy 等）；
- 脚本：密钥检查与部署脚本更新；
- 文档：运维与回滚指南、迁移兼容策略；
- 测试报告：单测/集成/门禁与生产验证清单。

> 备注：本 PRD 为生产级安全与治理加固的总纲，遵循最小变更原则，优先修复高风险并统一策略。更高级能力（刷新令牌持久化/统一密管/硬件信任）将作为后续独立 Epic 推进。

