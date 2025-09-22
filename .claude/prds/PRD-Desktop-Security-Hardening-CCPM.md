# PRD：桌面客户端安全加固（CCPM）

## 项目背景与目标
- 背景：桌面端存在本地“安全配置”加密未鉴别（可被无声篡改）、凭据服务实现不一致（弱实现仍在）、调试日志打印部分 Token、错误详情复制可能包含敏感字段，以及 Debug 构建跳过证书校验等问题。
- 目标：以“最小可行变更”加固桌面端安全基线，优先修复高风险缺陷，统一凭据与配置的安全实现，避免敏感信息泄露，同时不改变外部业务功能与 API 行为。

## 范围与非范围
- 范围：
  - Core/Services/Configuration 安全配置存储与密钥管理
  - Services 凭据存储、用户会话与 HTTP 认证头处理
  - Infrastructure HTTP 客户端与证书校验（Debug 行为限定）
  - Shell 及 Workbenches 的错误复制与诊断输出
- 非范围：
  - 引入系统级密码库/硬件模块（如 TPM、CredUIPrompt）
  - 跨端统一密钥管理平台接入
  - 前端 UI/UX 大范围重构

## 业务价值与成功指标（北极星）
- 本地配置不可无声篡改：对被篡改的“安全配置”能检测并拒绝加载。
- 日志/剪贴板零敏感：不输出 Token 片段；错误复制前完成脱敏。
- 凭据统一安全：仅保留 DPAPI+随机熵的安全实现，删除弱实现使用面。
- Debug 限制明确：证书校验绕过仅在 Debug 且显式受控，避免误用到 Release。
- CI 门禁：新增单测全部通过，现有架构与功能测试不退化。

## 功能需求（Epics / Stories）

### Epic A：本地安全配置采用可鉴别加密（AEAD/HMAC）
- 必须：
  - 将 `EncryptData/DecryptData` 改为 AEAD（AES-GCM 优先）或 “AES-CBC + HMAC-SHA256（密钥分离）”。
  - 解密前先验 MAC/Tag，失败则拒绝并记录审计。
- 变更点（参考）：
  - 文件：`src/Client/Desktop/Core/Services/Configuration/SecureConfigurationService.cs`
    - `EncryptData`（约第 744 行）
    - `DecryptData`（约第 768 行）
    - `ComputeChecksum`（约第 789 行，删除或改为 HMAC 验证）
- AC：
  - 手动篡改/损坏密文或校验数据，加载时能检测并失败（抛出明确异常或返回安全错误）。

### Epic B：KDF 强化与随机盐
- 必须：
  - PBKDF2 迭代次数≥100,000；每条记录使用随机盐（与密文同行存储），不再使用固定 `_keyDerivationSalt`。
- 变更点：
  - `DeriveKey`（约第 725 行）：接受“随机盐”输入；为每条配置生成盐并持久化到 `SecureConfigEntry.Metadata`。
- AC：
  - 新写入的配置均含随机盐；读取时使用对应盐派生密钥；兼容旧格式（迁移或按需重写）。

### Epic C：主密钥管理与持久化保护
- 必须：
  - 废弃将“机器名/用户名”等非机密信息作为主密钥（`GetMachineKey`，约第 737 行）。
  - 将主密钥（或密钥加密密钥，KEK）使用 Windows DPAPI（CurrentUser）保护后持久化，启动时解封装。
  - 可选：支持用户口令作为附加 KDF 因子（提升跨用户安全性）。
- AC：
  - 切换 Windows 用户无法解密原配置；同一用户可正常读写；主密钥文件未明文暴露。

### Epic D：凭据服务统一与强化
- 必须：
  - 统一使用 `SecureCredentialService`（DPAPI + 随机熵 + 擦写删除）。
  - 将 `CredentialService`（固定 Entropy，`"LYBT-Credential-Entropy-2024"`）弃用/重定向到安全实现；确保删除路径执行擦写覆盖。
- 变更点：
  - 文件：
    - `src/Client/Desktop/Services/SecureCredentialService.cs`（保留）
    - `src/Client/Desktop/Services/CredentialService.cs:32`（固定 Entropy）
    - 依赖注入处确保仅注册安全实现（`Shell/Extensions/ServiceCollectionExtensions.cs`）。
- AC：
  - 代码中不再注入/引用弱实现；保存的凭据只能被当前用户读取；旧文件在迁移/删除后不可恢复。

### Epic E：认证头日志脱敏（移除 Token 片段）
- 必须：
  - 删除 Bearer 子串打印，仅输出是否存在 Token（存在/空）。
- 变更点：
  - 文件：`src/Client/Desktop/Services/Handlers/AuthHeaderHandler.cs:33`
- AC：
  - 调试输出不含任何 Token 片段；仅显示布尔状态与 URL。

### Epic F：错误复制到剪贴板前脱敏
- 必须：
  - 在 `BuildErrorSummary()` 中对 `TechnicalDetails/ContextData` 做敏感字段脱敏（Authorization、Token、Password、ConnectionString 等）。
  - 提供可配置开关：默认脱敏，开发人员可在 Debug 打开“原始详情”但附加显著提示。
- 变更点：
  - 文件：`src/Client/Desktop/Shell/Dialogs/ViewModels/ErrorDetailsDialogViewModel.cs:114, 147`
- AC：
  - 复制的文本不出现敏感字段值；Debug 下打开原始详情时有明显警示。

### Epic G：Debug 证书校验绕过的安全护栏
- 必须：
  - 保留 `#if DEBUG` 的证书校验绕过，但增加注释与环境判断，避免 Release 构建或误配启用。
  - 可选：Debug 启动时在日志打印“证书校验已绕过（仅 Debug）”。
- 变更点：
  - 文件：`src/Client/Desktop/Infrastructure/HttpClientFactory.cs:110–119`
- AC：
  - Release 构建下严格证书校验；Debug 下仅本机开发用途启用，并打印提示。

### Epic H：诊断文件输出控制
- 必须：
  - 将桌面诊断文件输出限制为 Debug；避免输出敏感信息；提供一键清理机制（菜单项或脚本）。
- 变更点：
  - 文件（示例）：
    - `src/Client/Desktop/Workbenches/SystemWorkbench/SystemWorkbenchModule.cs:37`
    - `src/Client/Desktop/Workbenches/SystemWorkbench/ViewModels/SystemWorkbenchMainViewModel.cs:125, 147, 182, 225`
- AC：
  - Release 不生成桌面诊断文件；Debug 输出不含敏感信息；清理功能可用。

## 非功能 / 安全要求
- 凭据与密钥文件设置严格 ACL（仅当前用户可读写）；
- 日志不记录敏感字段值（Token/密码/密钥/连接串等）；
- 保持 `.editorconfig`、命名与 StyleCop 规范一致；
- 不引入被基线禁止的第三方库；
- 与服务端安全基线策略一致（如字段脱敏名录）。

## 关键链（CCPM）计划
- 主关键链：
  1. A 本地配置 AEAD/HMAC（根因修复）
  2. B KDF 强化 + 随机盐
  3. C 主密钥 DPAPI 保护
  4. D 凭据服务统一
  5. E 认证头日志脱敏
  6. F 错误复制脱敏
  7. G Debug 证书护栏
  8. H 诊断输出控制
- 喂入链与缓冲：
  - 项目缓冲：关键链总工期的 30%
  - 喂入缓冲：凭据迁移/清理与诊断输出改造的 20%
- 里程碑：
  - M1：AEAD/HMAC 与 KDF 上线（A、B）
  - M2：主密钥 DPAPI 化（C）
  - M3：凭据统一与日志/错误脱敏（D、E、F）
  - M4：证书护栏与诊断输出收口（G、H）

## 单元测试与验证计划
- 总体：新增安全相关单测（与必要的集成测试），确保回归通过。
- Epic A（AEAD/HMAC）
  - 篡改密文/Tag → 解密失败（抛异常或返回失败），记录审计。
  - 正常密文 → 成功解密；兼容旧数据的迁移策略（如有）。
- Epic B（KDF 强化）
  - 新写入配置包含随机盐；读取使用对应盐；验证迭代次数≥100k。
- Epic C（主密钥 DPAPI）
  - 同机不同用户无法解密；同用户可正常读写；主密钥未明文落盘。
- Epic D（凭据统一）
  - DI 仅解析到 `SecureCredentialService`；`CredentialService` 不再被注入。
  - 记住我：可保存并成功读取；切换用户读取失败（DPAPI 生效）。
- Epic E（认证头）
  - 调试输出不包含任何 Token 片段；只显示存在/空。
- Epic F（错误复制）
  - 复制文本中常见敏感键（Authorization/Token/Password/ConnectionString）被脱敏。
  - Debug 原始详情开关：开启时出现明显提示。
- Epic G（证书护栏）
  - DEBUG 符号下回调为 true，Release 下严格校验；有日志提示（Debug）。
- Epic H（诊断输出）
  - Release 不生成诊断文件；Debug 诊断文件不包含敏感信息；清理功能可用。
- 回归与门禁
  - 现有功能与架构测试全部通过；无 API 兼容性变化（桌面与服务端约定不变）。

## 风险与缓解
- AEAD/HMAC 与 KDF 改造涉及历史数据兼容：
  - 方案：检测旧格式→一次性重写为新格式；失败则回滚并给出用户提示。
- 主密钥 DPAPI 化可能带来迁移失败：
  - 方案：先导出备份，加回滚路径；失败时保留旧路径可读取但标记为待迁移。
- 凭据统一后老文件剩余：
  - 方案：提供一次性迁移/删除工具与操作日志；确认擦写删除。

## 发布与回滚
- 分里程碑分批发布：先加密栈与 KDF，再主密钥与凭据统一，最后外围脱敏与护栏。
- 回滚策略：
  - 保留旧读取逻辑的受控开关；失败时降级到只读旧配置并提示迁移失败。

## 监控与度量
- 本地配置篡改检测率；
- 调试日志敏感采样为 0；
- 凭据读取失败率（跨用户场景）；
- 发行后崩溃/异常日志趋势（与 AEAD/HMAC 改造相关）。

## 交付物与变更清单
- 安全配置：`SecureConfigurationService` 采用 AEAD/HMAC；KDF 强化；DPAPI 保护主密钥；
- 凭据服务：统一 `SecureCredentialService`；弃用 `CredentialService`，追加擦写删除；
- 认证头：移除 Token 片段日志；
- 错误复制：新增敏感字段脱敏与 Debug 原始详情开关；
- HTTP 客户端：Debug 证书跳过护栏；
- 诊断输出：仅 Debug 且不含敏感；提供清理机制；
- 文档：迁移/回滚与用户指引。

> 备注：本 PRD 作为“桌面客户端安全基线加固”的聚焦发布，优先修复核心加密与泄露风险；更高级的密钥管理或硬件信任路径将作为后续独立 Epic 规划。

