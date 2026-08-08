# 任务 A-23：越层修复 + 孤儿类清理 + 守卫补全

> [!NOTE]
> 此任务书已完成（2026-08-08）。实现详情见最终报告：
> [Final Report](../compose/reports/a23-layer-guard-cleanup.md)

> 依据：`structure-audit-perclass-crosscheck-2026-08-08.md` 交叉验证 §四
> 产品负责人已确认执行

## 任务清单（3 项）

### A-23a：3 个 VM 真越层修复（Shell/Roles 层）

**技术总监复核确认**：Mimo 报告的 6 处中有 3 处是 A-18 有意设计（SystemSettings/LogLevelControl/Deployment 注入统一 `IApiClient`，非越层），**真越层是 3 个**：

| VM | 位置 | 现注入 | 应改走 | 调用的 API 方法 |
|----|------|--------|--------|----------------|
| AccountSettingsViewModel | Shell/ViewModels/ | `IApiClientUsers` | `IUserService`（已有 ChangeProfile/ChangePassword）| ChangeProfileAsync / ChangePasswordAsync |
| PatientSelectionViewModel | Roles/Clinical/ViewModels/ | `IApiClientPatients` + `IApiClientMedicalCases` | `IPatientService` + `IMedicalCaseQueryService`（已有）| GetPendingCasesAsync / GetPatientsAsync / GetPatientByIdAsync |
| SysadminHomeViewModel | Roles/Admin/Sysadmin/ViewModels/ | `IApiClientAuth` | 查是否有 IAuthService；无则新建 `IAuthHealthService` 封装 HealthCheckAsync | HealthCheckAsync |

**动作**：
- 读每个 VM 全文，将 API 调用改为注入对应 Service 接口（对齐 MVVM 分层：VM→Service→Repository→IApiClient）
- Service 接口缺失的（如 Auth HealthCheck）在 Contracts/Services 新建接口 + 模块内实现，DI 注册
- 约束：行为等价，不改业务逻辑

### A-23b：架构守卫补 Desktop 越层规则

**现状**：P01c 只扫 Server 程序集——Desktop VM 注入 IApiClient 零守卫（A-21 漏抓的根因）

**动作**：
- 新增架构测试：**Desktop 所有 ViewModel 禁止注入 `IApiClient`/`IApiClient*` 子接口**（应注入 Service 接口）
- 参照既有 Desktop 架构测试写法（DesktopLayerArchTests.cs），豁免已知合法注入（若 A-18 有意设计的 3 个 VM 注入统一 IApiClient 被误伤，需确认豁免策略——**技术总监判断：统一 IApiClient 注入也应视为越层，长期目标是 VM 全部走 Service**；但 A-18 刚改过，本次先守卫「子接口注入」这一硬违规，统一 IApiClient 作为过渡允许）

### A-23c：孤儿类 D=29 清理

**现状**：Mimo 逐 class 验证 D=29 孤儿类（Patients 死组件链 12 类 ~680 行 + Infrastructure 9 + Foundation 2 + Formula 2 + Shared 1）

**动作**：
- 读 `structure-audit-perclass-mimo-2026-08-08.md` 的 D 级清单（§孤儿类与待决策项全清单）
- 逐类复证零引用（serena/codebase-memory）后删除
- 同步清理：Infrastructure Http 目录滞留（LoggingHttpHandler 下沉 Foundation、ApiResponseHelper 死）

## 验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：85/85 + 新守卫（预计 86/86）
3. 相关单测
4. 每项独立 commit + push

## 不做

- ❌ 不改 A-18 已确认的统一 IApiClient 注入（过渡允许）
- ❌ 不重写孤儿类（删除而非保留）
- ❌ 不改蓝图（A-23d 蓝图 v1.1 另行处理）
