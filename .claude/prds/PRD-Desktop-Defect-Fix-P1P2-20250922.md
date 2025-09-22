**PRD — Desktop“主要设计缺陷与影响”修复方案（P1/P2）**

- 版本：v1.0-RF-Desktop-P1P2
- 部署环境：Windows 本地部署，不使用容器
- WebAPI 基址：`https://localhost:5001`（健康检查：`GET /api/v1/health`）

**背景与问题**
- 已完成 P0：登录状态打通（`IAuthenticationService` 读取真实会话）、统一 API 客户端注入 Bearer Token（登录设置/登出清除）、API 端口切换至 5001。
- 待修复（按严重度）
  - 会话与 Token 源重复且分裂（`ISessionManager` vs `UserSessionManager/ITokenManager`）
  - “基于角色按需加载”未真正生效（OnDemand 模块被全量加载）
  - DI 注册重复、生命周期不一致（模块/中央重复注册）
  - HTTP 层与序列化策略重复（Core/Infrastructure 两套并存）
  - 对话框服务重复（`WpfDialogService` vs `PrismDialogService`）
  - 健康检查与诊断提示不足（证书/端口/API 异常提示不直观）

**产品目标**
- 统一登录/会话/Token 管理，保障认证行为稳定、可预测。
- 落实“基于角色按需加载”，缩短冷启动并降低资源占用。
- 收敛服务注册与 HTTP 通道，降低维护复杂度与潜在冲突。
- 提升本地部署体验：健康检查可视化与诊断指引清晰。

**范围说明**
- In Scope：会话与 Token 源统一；角色按需加载；DI 注册收敛；HTTP/序列化统一；对话框服务统一；健康检查与诊断增强；文档与清理。
- Out of Scope：新业务功能；UI 视觉重构；容器化/远程部署。

**用户与场景**
- 管理员：系统/用户/资源管理，需要完整模块访问。
- 医生：看诊与患者管理，需要快速进入看诊工作台。
- 技术人员（内部）：便捷诊断（端口/证书/连通性）。

**假设与依赖**
- WebAPI 本机监听 5001 且支持 HTTPS；Windows 防火墙放行 5001。
- 开发/测试机器已执行 `dotnet dev-certs https --trust`。

**功能需求（FR）**
- FR-01 会话与 Token 源统一（Must）
  - 为 `ISessionManager` 增加只读 Token 访问器（如 `AuthToken`/`GetToken()`），成为唯一事实来源。
  - `AuthHeaderHandler` 改为依赖 `ISessionManager`，仅自此读取 Token。
  - 废弃 `UserSessionManager` 的 Token 职责（保留权限相关逻辑）。

- FR-02 角色按需加载（Should）
  - `ConfigureModuleCatalog` 维护“模块→角色白名单”映射。
  - 登录后在 `LoadRoleBasedModulesAsync` 依据当前角色过滤加载 `OnDemand` 模块。

- FR-03 DI 注册收敛（Must）
  - 以 `Shell/Extensions/ServiceCollectionExtensions.cs` 为唯一服务注册来源。
  - 各 `XxxModule` 内仅保留视图/导航注册，移除服务注册；生命周期统一（优先 Singleton）。
  - 通过 `ModuleRegistrationValidator` 校验无重复/无冲突。

- FR-04 HTTP 与序列化统一（Should）
  - 统一保留 `Infrastructure/UnifiedApiClientManager`（Refit + System.Text.Json）。
  - `Core/Http/HttpClientFactory` 标记弃用（无引用则移除）；`Core/Http/ApiService` 复用统一 HttpClient 实例。
  - 统一错误映射与重试策略（Polly）。

- FR-05 对话框服务统一（Should）
  - 标准化 `ICustomDialogService -> WpfDialogService` 为唯一入口。
  - 移除 `PrismDialogService` 的 DI 与示例；必要时由 `WpfDialogService` 适配 Prism 对话框能力。

- FR-06 健康检查与诊断增强（Should）
  - 启动健康检查 UI：显示“连接正常/证书未信任/端口占用/服务未启动”等指引。
  - 日志增强：请求关联 ID、Token 打码长度（不打印 Token 内容）。

**非功能需求（NFR）**
- NFR-01 性能：冷启动≤3s（本机），首次导航≤500ms。
- NFR-02 稳定：异常分类提示（401/403/5xx/超时），失败可重试，不崩溃。
- NFR-03 安全：不输出 Token/密码；DPAPI 存储凭据；Release 严格证书校验。
- NFR-04 可维护：DI 容器验证通过；无重复注册；日志清晰可追踪。

**成功指标（KPI）**
- 登录后 100% 请求携带 Bearer；登出后 0% 请求携带 Bearer。
- 医生角色较管理员少加载≥3个 OnDemand 模块；冷启动≤3s、首次导航≤500ms。
- 健康检查异常定位正确率≥95%。
- 构建 0 错误；无关键警告；注册验证全通过。

**验收标准**
- 会话与 Token
  - 登录后 `ISessionManager.AuthToken` 非空；`IAuthenticationService.IsLoggedIn == true`。
  - 所有 Refit 调用带 `Authorization: Bearer`；登出后清空。
- 角色加载
  - 管理员：系统工作台 + 全部核心模块；医生：看诊工作台 + 看诊相关模块。
  - 模块加载日志可见过滤名单。
- DI 与 HTTP
  - 容器验证无重复注册；仅一个 HttpClient 单例；统一 System.Text.Json 序列化与错误映射。
- 对话框与诊断
  - 工程内仅 `ICustomDialogService` 在用；F1/确认/错误/信息对话框可用。
  - 启动健康检查提示明确修复建议（信任证书/放行 5001/启动服务）。

**实施建议（不含代码）**
- P1（统一与落地）
  - 会话与 Token：为 `ISessionManager` 增添只读 Token；`AuthHeaderHandler` 依赖切换；移除 `ITokenManager` 绑定。
  - 角色按需加载：在 `App.ConfigureModuleCatalog` 维护映射；`LoadRoleBasedModulesAsync` 过滤加载。
  - DI 收敛：将模块内服务注册迁移至 Shell；统一生命周期；用 `ModuleRegistrationValidator` 验证。
  - HTTP/序列化：保留 `UnifiedApiClientManager`，`ApiService` 复用同一 HttpClient；标记弃用旧工厂。
- P2（统一体验与运维）
  - 对话框：统一 `ICustomDialogService -> WpfDialogService`；移除 `PrismDialogService` DI。
  - 诊断：健康检查 UI 与日志增强（关联 ID、Token 打码长度）。
  - 文档：更新 README/模块说明/部署指引（5001/证书/健康检查）；清理死代码与过时实现。

**交付物**
- 修复提交（P1→P2）及变更说明（按文件列举影响点）。
- 配置与部署指引（证书信任、端口 5001、健康检查）。
- 测试用例（单元/集成/端到端）与测试报告。
- 更新文档（README、模块说明、诊断指南）。
- 清理列表（移除/弃用项清单）。

**里程碑与排期**
- P1（3–5 天）：会话与 Token 统一、角色按需加载、DI 收敛第一批、HTTP/序列化统一。
- P2（3–4 天）：对话框统一、健康检查与诊断增强、文档与清理。

**风险与缓解**
- 会话来源切换风险：先“读新写旧”的过渡开关，验收后移除旧实现。
- 模块过滤误配：提供“加载全量”的回退开关与详细日志。
- 证书/端口环境差异：健康检查明确提示“信任证书/放行 5001/启动服务”。

**影响面（组件）**
- 会话与认证：`Core/Interfaces/Services/ISessionManager`、`Core/Services/SessionManager`、`Services/Handlers/AuthHeaderHandler`、`Services/UserSessionManager`
- 模块加载：`Shell/App.xaml.cs`
- DI 装配：`Shell/Extensions/ServiceCollectionExtensions.cs`
- HTTP/序列化：`Infrastructure/Api/*`、`Core/Http/*`（评估/清理）
- 对话框：`Core/Services/WpfDialogService`、`Services/PrismDialogService`（弃用）

