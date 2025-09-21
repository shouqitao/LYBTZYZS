# 解决方案架构分析（All 层面）

## 一、总体概览
- 解决方案结构：
  - Server（Web API + 领域模块 + 基础设施）：`src/Server`
  - Client/Desktop（WPF 桌面端）：`src/Client/Desktop`
  - Shared（共享 DTO / 接口 / 工具）：`src/Shared`
  - 测试：`tests`
  - 文档/脚本：`docs`、`scripts`
- 架构风格：单体内的模块化分层（UltraThink 双层/模块化标准），接口以 REST + 版本化（Asp.Versioning）对外。
- 关键横切：认证/授权（JWT Bearer）、配置与日志（Serilog + appsettings.*）、异常统一（ProblemDetails + GlobalExceptionHandler）、持久化（EF Core/SQL Server）、限流（ASP.NET RateLimiter）。

## 二、分层与模块
- WebAPI 服务层：
  - 启动/组合根：`src/Server/Services/LYBT.WebAPI/Program.cs`
  - 统一服务注册/中间件：`src/Server/Services/LYBT.WebAPI/Extensions/*`
  - 控制器：`src/Server/Services/LYBT.WebAPI/Controllers`
- 领域模块层：
  - 模块目录：`src/Server/Modules/LYBT.Module.*`
  - 示例（认证）：`LYBT.Module.Auth` 提供仓储/服务（JWT 发放、登录流程、管理员处理等）
- 基础设施层：
  - 数据上下文/迁移：`src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
  - 授权策略/配置选项/环境校验：`src/Server/Core/LYBT.Infrastructure/*`
- 桌面端：
  - 基础设施/HTTP/Refit/服务注册：`src/Client/Desktop/Infrastructure`、`src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
  - 凭据与会话：`src/Client/Desktop/Services/*`、`src/Client/Desktop/Core/Interfaces/*`
  - 安全配置存储：`src/Client/Desktop/Core/Services/Configuration/SecureConfigurationService.cs`
- 共享：
  - DTO/契约：`src/Shared/LYBT.Shared.Models`
  - 工具：`src/Shared/LYBT.Shared.Utilities`（含密码学工具 PasswordHelper 等）

## 三、横切关注点与现状
- 认证/授权（Server）
  - JWT 配置与验证：`UnifiedServiceRegistration.cs` 从配置/环境变量加载 Secret 并配置 TokenValidationParameters
  - 授权策略：存在两套扩展（`Infrastructure/Authorization/AuthorizationPolicyExtensions.cs` 与 `WebAPI/Extensions/ServiceCollection/AuthorizationExtensions.cs`）
  - 控制器授权分布：大多数业务控制器带 `[Authorize]`，但存在个别敏感端点未标注
- 配置/日志
  - 非生产启用 Swagger；Serilog 在生产降噪；环境感知校验阻止生产下的危险配置（如敏感日志）
- 限流
  - 全局与登录策略在 `UnifiedServiceRegistration.cs` 硬编码，配置侧也存在 `SecurityOptions.RateLimit`（分散）
- 异常/错误处理
  - 全局异常中间件：`GlobalExceptionHandler.cs`；控制器封装 `BaseApiController` 统一返回
- 客户端（桌面）
  - 凭据：DPAPI 版本（SecureCredentialService）与弱化版本（CredentialService）并存
  - 安全配置：自实现 AES 加密与完整性校验，KDF 与主密钥策略偏弱
  - HTTP：Debug 下证书校验绕过；认证头处理打印 Token 片段（Debug）

## 四、主要问题清单（按域划分）

### A. 服务端（Server）
1) 高敏端点授权缺失/不一致
- 现象：
  - 系统管理员改密接口未加授权：`src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:117`
  - 健康详情接口未受保护：`src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`
- 风险：越权调用、信息泄露

2) 授权策略实现重复、接入不统一
- 现象：
  - 两套授权扩展定义：`src/Server/Core/LYBT.Infrastructure/Authorization/AuthorizationPolicyExtensions.cs` 与 `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollection/AuthorizationExtensions.cs`
  - 未明确在 WebAPI 启动统一启用 `DefaultPolicy/FallbackPolicy`
- 风险：策略漂移、端点覆盖不全

3) JWT Claims 与操作人解析不一致
- 现象：
  - 令牌写入 `sub/unique_name`；`BaseControllerCore.GetOperator()` 读取 `ClaimTypes.NameIdentifier/Name` 与 `FindFirst("Admin")`
  - 参考：`src/Server/Modules/LYBT.Module.Auth/Services/JwtAuthenticationService.cs`、`src/Server/Core/LYBT.Infrastructure/Web/BaseControllerCore.cs`
- 风险：操作人解析失败、审计断裂

4) 限流配置与实现分散
- 现象：
  - `UnifiedServiceRegistration.ConfigureRateLimiting(...)` 内硬编码；同时 `SecurityOptions.RateLimit` 也有配置项
- 风险：环境切换/策略变更时不一致

5) 日志可能包含敏感字段
- 现象：
  - 控制器异常处理传入 `request/dto`；`BaseControllerCore.HandleExceptionCore(...)` 会序列化上下文（如密码/Token）
  - 参考：`AuthController.cs:71`、`UsersOperationController.cs:119/153/187`
- 风险：敏感信息进入日志

6) 密钥文件曾被提交
- 现象：`src/Server/Services/LYBT.WebAPI/.encryption-key`
- 风险：泄露密钥；需旋转

7) 密码校验存在实现瑕疵
- 现象：
  - `UserBusinessService.cs` 的 `PasswordHelper.Verify` 参数顺序易错，已发现错误用法（应为 `Verify(hash, password)`）
- 风险：错误接受/拒绝密码

8) 生产 CSP 过宽
- 现象：`appsettings.Security.json` 的 CSP 包含 `'unsafe-inline'/'unsafe-eval'`（生产不宜）
- 风险：XSS 面扩大

### B. 桌面端（Client/Desktop）
1) 安全配置存储缺少“可鉴别性”
- 现象：
  - `SecureConfigurationService` 使用 AES（默认 CBC/PKCS7）+ 非密钥校验（SHA256 checksum），`ComputeChecksum` 非 HMAC
  - KDF 迭代数 10000（偏低），盐固定常量；主密钥来源机器信息（非机密）
  - 参考：`src/Client/Desktop/Core/Services/Configuration/SecureConfigurationService.cs:725/737/744/768/789`
- 风险：密文可被篡改且通过校验；抗暴力弱

2) 凭据服务实现不一致且存在弱实现
- 现象：
  - `SecureCredentialService`（DPAPI + 随机熵 + 覆写删除）与 `CredentialService`（固定 Entropy）并存
  - 参考：`src/Client/Desktop/Services/SecureCredentialService.cs`、`src/Client/Desktop/Services/CredentialService.cs:32`
- 风险：被误用时降低安全基线

3) Token 片段出现在调试输出
- 现象：`src/Client/Desktop/Services/Handlers/AuthHeaderHandler.cs:33`
- 风险：日志/调试工具扩散敏感片段

4) Debug 跳过证书校验（Release 安全）
- 现象：`src/Client/Desktop/Infrastructure/HttpClientFactory.cs:110–119`（`#if DEBUG`）
- 风险：误用到生产/Release 或被不当复制

5) 错误复制到剪贴板未脱敏
- 现象：`src/Client/Desktop/Shell/Dialogs/ViewModels/ErrorDetailsDialogViewModel.cs:114/147`
- 风险：敏感上下文被复制传播

6) 诊断文件写入桌面
- 现象：`SystemWorkbench*` 多处写入 Desktop 调试文件（若含敏感信息有风险）

### C. 通用治理问题
- 策略/配置分散（如限流、安全头、密码策略）与代码耦合，易产生漂移
- `ClaimsNormalization` 存在占位实现但未真正归一化声明
- 监控/可观测性对安全事件的追踪维度可加强（如拒绝/限流事件统计）

## 五、风险评估（摘要）
- 高：未授权访问（管理员改密、健康详情）、日志含敏感、密钥泄露、桌面安全配置可篡改
- 中：授权策略重复/不统一、JWT Claims 不一致、限流漂移、密码校验瑕疵、Token 片段打印
- 低：诊断文件、Debug 证书、错误复制未脱敏

## 六、结论
- 现有架构模块化清晰、分层合理，但在“统一授权与安全基线、配置一致性、客户端本地安全存储、日志与调试信息治理”方面存在明显改进空间。建议分阶段推进安全与治理优化，并与现有测试/架构门禁协同，确保改动可验证、可回滚。

