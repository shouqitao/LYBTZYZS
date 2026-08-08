# A-18 批次2 契约统一方案 — P1-1 双套统一 + P1-2 adapter 收敛

> 日期: 2026-08-08 | 作者: Mimo Code（勘察执行）| 状态: **待技术总监确认**
> 任务书: `docs/03-architecture/task-a18-batch2-contract-unification-2026-08-08.md`
> 本方案仅勘察结论 + 选项对比，**未改任何契约代码**（P1-5 已独立完成见 commit `812fcdd0c`）

---

## 0. 勘察结论速览（TL;DR）

| 项 | 结论 |
|----|------|
| ① Admin 3 VM 直用 Refit | 全部直用，共 **9 个调用点**（IConfigurationApi×3 / IDiagnosticsApi×4 / IDeployApi×2），**均未**走 IApiClient |
| ② RefitApiClient vs ApiClient 方法面 | 10 个域 **方法名 1:1**（7 域完全一致；Users/Herbs/Formulas 多 local-only 方法；1 处返回签名差异 CancelMedicalCaseAsync）；**Configuration 域无 ApiClient 对应段——唯一双套未统一缺口** |
| ③ 方案 A vs B | **推荐方案 A**（RefitApiClient 保留内部实现 + Api 接口 internal 化 + 补 Configuration 段），改动面小、风险低 |
| P1-2 adapter | 实际 **10 对**（任务书写 12 对，勘察确认 10 对/20 文件），方法名 **100% 1:1**，仅 6 个 local-only 桩（Refit 侧抛 NotSupportedException） |

> ⚠️ **与任务书的事实偏差（2 处）**：
> 1. 任务书称 RefitApiClient「内部 11 个包装方法」——实际 **10 个属性**（`RefitApiClient.cs` 只实现 IApiClient 的 10 段，Configuration 不在其中）
> 2. 任务书称 adapter「12 对 / 24 个文件」——实际 **10 对 / 20 个文件**（`Foundation/Http/Clients/`）

---

## 1. ① Admin 3 个 ViewModel 直用 Refit 接口调用点清单

三个 ViewModel 均直接构造注入 `LYBT.Desktop.Contracts.Api` 下的 Refit 接口（带 `[Get]/[Post]` 特性），**未**使用 `IApiClient` 统一接口（`Contracts/ApiClient/`）。

### 1.1 SystemSettingsViewModel（`Roles/LYBT.Desktop.Admin/ViewModels/SystemSettingsViewModel.cs`）
注入: `IConfigurationApi`（另注入 ISystemSettingsService/IClinicSettingsService 业务服务，非 Api）

| 调用点 | 行号 | 描述 |
|--------|------|------|
| `GetConfigurationAsync` | 321 | 加载服务器配置（App:Name/Version/Environment） |
| `UpdateConfigurationAsync` | 365 | 批量保存服务器配置 |
| `ValidateProductionAsync` | 393 | 验证生产环境配置 |

### 1.2 LogLevelControlViewModel（`Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/LogLevelControlViewModel.cs`）
注入: `IDiagnosticsApi`

| 调用点 | 行号 | 描述 |
|--------|------|------|
| `GetLoggingStatusAsync` | 43 | 查询当前日志级别状态（OnNavigatedTo 触发） |
| `SetLoggingLevelAsync` | 65 | 手动设置日志级别 |
| `EnableDebugModeAsync` | 84 | 开启调试模式（60 分钟自动过期） |
| `DisableDebugModeAsync` | 102 | 关闭调试模式 |

### 1.3 DeploymentViewModel（`Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/DeploymentViewModel.cs`）
注入: `IDeployApi`（另注入 INavigationCoordinator，非 Api）

| 调用点 | 行号 | 描述 |
|--------|------|------|
| `UploadAsync` | 61 | 上传 ZIP 更新包（MultipartFormDataContent） |
| `RestartAsync` | 92 | 发送服务重启指令 |

**结论**：3 个 VM 共 9 个调用点，改造 = 换注入接口（IConfigurationApi→IApiClient.Configuration / IDiagnosticsApi→IApiClient.Diagnostics / IDeployApi→IApiClient.Deploy）+ 调用处适配。IDiagnosticsApi/IDeployApi 已有对应 `IApiClientDiagnostics`/`IApiClientDeploy` 段；**IConfigurationApi 无对应段，需新增**（见 §2.3）。

---

## 2. ② RefitApiClient 内部包装 vs ApiClient 接口方法面 1:1 核查

### 2.1 RefitApiClient 实际结构（`Foundation/Http/RefitApiClient.cs`）

实现 `IApiClient` 聚合接口的 **10 个属性**（懒加载）：

```
Auth → AuthApiClient(RestService.For<IAuthApi>)
Users → UserApiClient(...IUserApi)
Patients → PatientApiClient(...IPatientApi)
Herbs → HerbApiClient(...IHerbApi)
Formulas → FormulaApiClient(...IFormulaApi)
MedicalCases → MedicalCaseApiClient(...IMedicalCaseApi)
Registrations → RegistrationApiClient(...IRegistrationApi)
Reports → ReportsApiClient(...IReportsApi)
Deploy → DeployApiClient(...IDeployApi)
Diagnostics → DiagnosticsApiClient(...IDiagnosticsApi)
```

> ⚠️ **无 Configuration 属性**——IConfigurationApi 不在 RefitApiClient 内（任务书称 11 个包装方法，实际 10 个）。

### 2.2 10 个域方法面 1:1 核查（Refit 接口 vs IApiClientXxx 子接口）

| 域 | Refit 接口 | 方法数 | ApiClient 接口 | 1:1? | 差异 |
|----|-----------|--------|----------------|------|------|
| Auth | IAuthApi | 6 | IApiClientAuth | ✅ 完全 | — |
| Users | IUserApi | 13 | IApiClientUsers | ✅ 远程 13/13 | ApiClient 多 `GetCurrentUserAsync`（local-only） |
| Patients | IPatientApi | 11 | IApiClientPatients | ✅ 完全 | — |
| Herbs | IHerbApi | 13 | IApiClientHerbs | ✅ 远程 13/13 | ApiClient 多 `GetCategoriesAsync`（local-only） |
| Formulas | IFormulaApi | 16 | IApiClientFormulas | ✅ 远程 16/16 | ApiClient 多 `GetCategoriesAsync`（local-only） |
| MedicalCases | IMedicalCaseApi | 17 | IApiClientMedicalCases | ✅ 17/17 | **1 处签名差异**：`CancelMedicalCaseAsync` Refit 返回 `Task<Refit.IApiResponse>` vs ApiClient 返回 `Task<ApiResponse>` |
| Registrations | IRegistrationApi | 6 | IApiClientRegistrations | ✅ 远程 6/6 | ApiClient 多 3 个 local-only（GetRegistrations/QuickVisit/DeleteRegistration） |
| Reports | IReportsApi | 3 | IApiClientReports | ✅ 完全 | — |
| Deploy | IDeployApi | 2 | IApiClientDeploy | ✅ 完全 | — |
| Diagnostics | IDiagnosticsApi | 4 | IApiClientDiagnostics | ✅ 完全 | — |
| **Configuration** | IConfigurationApi | 5 | **（无）** | ❌ **无对应段** | 见 §2.3 |

### 2.3 ⚠️ Configuration 域：双套未统一的唯一缺口

- `IConfigurationApi`（5 方法：GetConfiguration/GetValue/SetValue/UpdateConfiguration/ValidateProduction）**在 ApiClient 侧完全缺失**——无 `IApiClientConfiguration`，`IApiClient` 聚合接口无 Configuration 属性
- 引用方（全仓 3 处）：
  1. `Contracts/Api/IConfigurationApi.cs` 定义
  2. `Shell/Extensions/UnifiedApiClientExtensions.cs` L116-122 — **绕过 IApiClient**：单独 `Register<IConfigurationApi>`，按当前连接 URL 直建 Refit 客户端
  3. `Roles/.../Admin/ViewModels/SystemSettingsViewModel.cs` L25/L145 — 构造注入直用
- **含义**：Configuration 目前只有 Refit（远程）实现路径，本地模式下 SystemSettings 的服务器配置区域无本地对应实现（B-05 配置中心 UI 为只读展示设计，本地模式行为需确认——见 §5 待确认项）

### 2.4 其它已确认事实

- 已删除残留核对：Refit 侧 `ChangeSysAdminPasswordAsync`/`CreateMedicalCaseWithDetailsAsync` 等已移除，两侧方法集一致
- `IEntityApiSegment<T>` 被 Users/Patients/Herbs/Formulas 4 个子接口继承（MedicalCases/Registrations 形状特殊不继承）；Segment 的 5 个标准方法经显式接口实现（DIM）转发到具名方法，不构成额外实现负担

---

## 3. ③ 方案 A vs 方案 B 对比

> 方向共识（任务书 + 交叉验证一致）：**以 ApiClient 为统一面，Api 套内化为其远程实现细节**。以下为具体落地选项。

### 方案 A：保留 RefitApiClient 内部实现，Api 接口 internal 化（推荐 ✅）

| 维度 | 内容 |
|------|------|
| **动作** | ① `Contracts/Api/*` 11 个 Refit 接口标记 `internal`（或移入 Foundation）——契约层对外只暴露 `IApiClient` 及子接口；② RefitApiClient 及各 adapter 保留（本就是内部实现，`internal sealed`）；③ **补 Configuration 段**：新增 `IApiClientConfiguration`（5 方法）+ `ConfigurationApiClient`（Refit 适配）+ `ConfigurationHttpApiClient`（本地适配）+ `IApiClient.Configuration` 属性 + RefitApiClient/HttpClientApiClient 各自实现；④ Shell 去掉单独 `Register<IConfigurationApi>`（改走统一注册）；⑤ Admin 3 VM 换注入 `IApiClient` |
| **改动面** | Contracts：11 个接口加 internal + 新增 1 个段接口（~12 处小改）；Foundation：新增 2 个 adapter 类；Shell：1 处注册删除；Admin：3 个 VM（9 调用点换注入 + 适配） |
| **风险** | **低**。不改任何行为语义；Refit 生成、handler 链、SwitchingApiClient 双轨全部不动；唯一新增量是 Configuration 双实现 |
| **优点** | 改动最小、可回退；Api 套保留为远程实现细节，未来若要删 Refit 可在 adapter 层替换 |
| **缺点** | 双套物理上仍存在（Api 接口未删），代码库仍有「两套接口」观感，只是可见性收窄 |

### 方案 B：删 Api 套接口，Refit 直接适配 ApiClient（不推荐）

| 维度 | 内容 |
|------|------|
| **动作** | ① 删除 `Contracts/Api/*` 11 个接口；② 在 `IApiClientXxx` 接口方法上直接加 `[Get]/[Post]` 等 Refit 特性（RestService.For<T> 要求 T 是带特性的接口）；③ RefitApiClient 改 `RestService.For<IApiClientXxx>`；④ adapter 层坍缩（Refit 生成类直接作为实现，删掉 10 个 XxxApiClient 包装） |
| **改动面** | Contracts：11 接口删除 + 10 个子接口全部加特性（~200 行特性注解）；Foundation：10 个 Refit adapter 类删除；HttpClientApiClient 侧不受影响；Shell/Admin 同上 |
| **风险** | **高**。① `IApiClient` 子接口被 55 个 Repository 引用，加 Refit 特性后接口暴露 Refit 依赖到整个桌面层（违背「统一无特性接口」初衷）；② `IEntityApiSegment<T>` 泛型方法与 Refit 特性不兼容（泛型方法无法映射 REST 路由），需拆 Segment 或改设计；③ `Refit.IApiResponse`/`HttpResponseMessage` 等返回类型的本地实现（HttpClientApiClient）不共享，双实现天然存在，删 Api 套并不能消掉双实现 |
| **优点** | 消除两套接口的物理存在，代码库「单套」观感 |
| **缺点** | 破坏 ApiClient 的无特性纯度；泛型 Segment 与 Refit 冲突；改动面覆盖 55 文件引用面；与「以文档为准」的现有设计（IApiClient 刻意无 Refit 依赖）相悖 |

### 决策建议

**推荐方案 A**。理由：
1. 任务书方向「Api 套内化为远程实现细节」正是方案 A 的语义（internal 化即内化）
2. Configuration 段补齐后，双套在**可见性**上统一（对外只有 IApiClient），行为完全等价
3. 方案 B 的泛型 Segment × Refit 冲突是硬伤，需要动 IEntityApiSegment 设计，超出本次「行为等价收敛」的边界
4. 若未来要彻底删除 Refit，方案 A 之后可以在 adapter 内部替换实现（影响面已收窄到 Foundation）

---

## 4. 附：P1-2 adapter 收敛勘察（10 对，方法面 1:1 现状）

> 任务书称「12 对 adapter（24 文件）」——**勘察确认实际 10 对（20 文件）**：`Foundation/Http/Clients/` 下 `XxxApiClient`（Refit 包装）+ `XxxHttpApiClient`（HttpClient）成对，全部实现同一 `IApiClientXxx` 接口。

| 对名 | Refit 方法数 | HttpClient 方法数 | 1:1? | 差异说明 |
|------|------|------|------|----------|
| Auth | 6 | 6 | ✅ | 无差异 |
| Users | 14 | 14 | ✅ | Refit 侧 `GetCurrentUserAsync` 抛 NotSupportedException（local-only 桩） |
| Patients | 11 | 11 | ✅ | 无差异 |
| Herbs | 14 | 14 | ✅ | Refit 侧 `GetCategoriesAsync` 抛 NotSupportedException |
| Formulas | 17 | 17 | ✅ | Refit 侧 `GetCategoriesAsync` 抛 NotSupportedException |
| MedicalCases | 17 | 17 | ✅ | 无差异 |
| Registrations | 9 | 9 | ✅ | Refit 侧 3 个 local-only 桩（GetRegistrations/QuickVisit/DeleteRegistration） |
| Reports | 3 | 3 | ✅ | 无差异 |
| Deploy | 2 | 2 | ✅ | 无差异 |
| Diagnostics | 4 | 4 | ✅ | 无差异 |

**结论**：
1. **方法名 100% 1:1 对齐**，10 个 IApiClientXxx 接口方法全部被两侧完整实现，无缺失/多余——**A6 漂移风险不存在**
2. 唯一差异是 **6 个 local-only 方法在 Refit 侧为抛异常桩**（GetCategoriesAsync×2、GetCurrentUserAsync、GetRegistrations/QuickVisit/DeleteRegistration），HttpClient 侧均有真实实现——这是双轨设计意图（IsLocal 分支），符合任务书「不改 SwitchingApiClient 切换语义」约束
3. 签名细节差异（非方法名差异）：6 处 Refit 侧可选参数带默认值而 HttpClient 侧无默认值（ExportXxxAsync keyword/category、GetQueueAsync doctorId、GetDailyXxx startDate/endDate、GetAuditLogsAsync page/pageSize、SuspendAsync/CancelMedicalCaseAsync request）——不影响方法对齐

**收敛评估**（依赖任务 2 决策）：
- 若任务 2 选 **A**：双实现保留（本就 1:1，无漂移），可暂不收敛；仅需为新增的 Configuration 域补一对 adapter
- 若任务 2 选 **B**：Refit 侧 10 个包装 adapter 坍缩（删），HttpClient 侧 10 个保留——但本地/远程双实现依然存在（本地走 HttpClientApiClient，远程走 Refit 生成类），收敛程度有限
- **本次不执行任何 adapter 代码改动**

---

## 5. 待技术总监确认事项

1. **方案 A vs B**：是否同意方案 A（推荐）？
2. **Configuration 段补齐方式**：新增 `IApiClientConfiguration` + 双 adapter（Refit/HttpClient）是否符合预期？还是 Configuration 保持「仅远程 Refit 直用」（最小改动，只 internal 化）？
3. **本地模式 Configuration 行为**：SystemSettings 服务器配置区域在本地模式是否允许显示/编辑？（B-05 为只读展示，若本地模式需隐藏该区域，则 ApiClient 的 Configuration 段可只做远程实现 + 本地 NotSupported，与现有 local-only 桩模式一致）
4. **`CancelMedicalCaseAsync` 签名差异**（Refit.IApiResponse vs ApiResponse）：本次是否顺带统一？（行为等价原则下可留待后续，但 1:1 核查已发现）

---

## 6. 执行顺序（确认后）

1. **P1-1（方案 A）**：Contracts 11 接口 internal 化 → 新增 IApiClientConfiguration + 双 adapter → IApiClient.Configuration 属性 → Shell 去独立注册 → Admin 3 VM 换注入 → build + 架构测试 + Admin 相关单测
2. **P1-2**：视 5.2 决策——Configuration adapter 补齐；其余 10 对 1:1 已确认，仅文档记录，不改代码
3. 每项独立 commit + push，总账 A-18 状态更新
