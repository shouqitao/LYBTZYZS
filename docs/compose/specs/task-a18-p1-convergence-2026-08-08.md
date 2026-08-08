# 任务 A-18：P1 机制收敛批次（架构完全可控核心）

> 依据：`structure-audit-crosscheck-2026-08-08.md` §二 P1 清单
> 产品负责人已拍板：**七项全做一个批次**（2-3d）
> 前置：A-17（P0 本地 CRUD 修复）完成后启动
>
> **进度（2026-08-08）**：第 1 子批次（低风险机械收敛）完成 ✅ — P1-7 `8db198b8a` / P1-3 `6769b02e0` / P1-4 `a484f6c15` / P1-6 `031682cad`（各独立验证 + commit + push）。剩余第 2 批次（P1-5 契约下沉+架构守卫 → P1-1/P1-2 契约统一+adapter 收敛）待执行。

## 目标

做完后：新增端点只需写一遍（契约单一）、映射单一机制、日志单一机制、Desktop 模块边界有守卫、文档=代码。**这是「架构完全可控」的落地批次。**

## 七项任务（按依赖排序执行）

### P1-1 契约双套统一（风险最高，先出方案再动手）

**现状**：
- `Contracts/Api/*`：11 个 Refit 特性接口（`IHerbApi` 等），被 RefitApiClient（远程模式）包装 + Admin 3 个 ViewModel 直用（SystemSettings/LogLevelControl/Deployment）
- `Contracts/ApiClient/*`：12 个统一无特性接口（`IApiClientHerbs` 等），被 SwitchingApiClient/HttpClientApiClient/全部桌面 Repository 使用（55 文件引用）

**方向**（交叉验证一致）：**以 ApiClient 为统一面**，Api 套内化为其远程实现细节。
- 具体方案需 Mimo 先行勘察：Admin 3 个 ViewModel 直用的 Refit 接口能否改为走 IApiClient？RefitApiClient 内部包装逻辑能否并入单一实现？
- **先输出方案（选项：A 保留 RefitApiClient 作为内部实现 / B 删 Api 套接口，Refit 生成直接适配 ApiClient）**，技术总监确认后执行

### P1-2 领域客户端双实现收敛

**现状**：`Foundation/Http/Clients/` 12 对 adapter（`HerbApiClient` Refit + `HerbsHttpApiClient` HttpClient），24 个文件维护双份
**方向**：与 P1-1 联动——契约统一后，adapter 层是否可收敛为单实现（远程/本地差异收进一个策略点）
**约束**：不改 SwitchingApiClient 的切换语义（IsLocal 分支是设计意图）

### P1-3 CorrelationId 收敛

**现状**：AsyncLocalCorrelationIdProvider（已死，AddAsyncLocalCorrelationIdProvider 0 调用点）+ ActivityCorrelationIdProvider（Desktop 用）+ Server CorrelationIdMiddleware（自实现 W3C）
**动作**：
- 删 `AsyncLocalCorrelationIdProvider.cs` + `AddAsyncLocalCorrelationIdProvider` 扩展
- 修 `CorrelationIdEnricher.cs:14` 注释（「Desktop端使用AsyncLocal」与代码矛盾）
- 核实 Server 中间件与 Activity Provider 的关系：Server 走中间件（W3C traceparent），Desktop 走 Activity Provider——**确认这是有意双端设计还是需要统一**（技术总监判断：Server/Desktop 各自端内单机制即可，跨端无需强统一）

### P1-4 Server 手写 Mapper 改 Mapperly

**现状**：7 个 Server 模块 csproj 全部引用 Riok.Mapperly 包，但 Formula/Herbs/Patients/Users 4 模块实际手写静态类（FormulaDtoMapper/HerbDtoMapper 等）
**动作**：手写类改 `partial class` + `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`，映射方法签名不变（行为等价）
**验证**：build + 对应模块单测

### P1-5 Desktop 模块边界补守卫

**现状**：P07 只守 Server；Desktop AGENTS.md 声明「Business modules MUST NOT reference each other」但无测试强制，且 Registration→MedicalCase 真违规
**动作**：
- 新增架构测试：Desktop 模块（Modules/）间禁止直接引用（参照 P07 写法）
- **先解决 Registration→MedicalCase 违规**：NavigationParameters/WorkspaceMode/EditState 下沉 `LYBT.Desktop.Contracts`（对齐 ViewNames 已在 Infrastructure 的先例），再补测试
- Admin/Clinical/Shell 对模块的引用是角色编排，测试需豁免或明确规则

### P1-6 本地配置持久化

**现状**：本地 ConfigurationController 用 `ConcurrentDictionary` 内存存储，重启即失；远程走 ISystemConfigurationService 持久化
**动作**：本地配置落盘（JsonFileConfigurationStore 先例）或落 LocalDB——**技术总监建议：复用远程的 JsonFileConfigurationStore 模式**，本地写入 `{BaseDirectory}/config/runtime-overrides.json`

### P1-7 08-shared.md 结构性重写

**现状**：声称 8 项目（Primitives/Utilities/Components/Validators 独立）实际 5 项目（坍缩进 Shared.Models）；Utilities 清单全错（ConfigurationHelper/PasswordHasher/JwtHelper 不存在，实际 4 文件）
**动作**：按实际 5 项目结构重写 08-shared.md（文档=代码，SSOT）
**同步**：03-server.md/05-dual-mode.md 的 13 项偏差（交叉验证 §5 清单）一并修正

## 执行顺序建议

1. **P1-7 文档重写先行**（文档定义设计态，后续代码改动以新文档为准）
2. P1-3/P1-4 机械收敛（低风险，独立验证）
3. P1-6 本地配置持久化（独立）
4. P1-5 契约下沉 + 架构测试（先解决违规再补守卫）
5. **P1-1/P1-2 契约统一 + adapter 收敛（最高风险，最后做，先出方案）**

## 验证（每项独立）

- `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
- `dotnet test tests/LYBT.Tests.Architecture/`：83/83（P1-5 后含新测试）
- 相关模块单测
- 每项完成单独 commit + push

## 不做（防发散）

- ❌ 不重构 SwitchingApiClient 切换语义（双轨是设计意图）
- ❌ 不改 DbContext 五套并存（已文档化有意设计）
- ❌ 不处理 M8 BCrypt/PasswordHelper（单独决策）
- ❌ 不处理 M9/M10（已决策维持）
