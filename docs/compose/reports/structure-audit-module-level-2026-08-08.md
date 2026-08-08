# 模块级结构审计报告（技术总监独立分析）

> 独立分析者 #1｜对应任务书：`structure-audit-module-level-task-spec-2026-08-08.md`
> 日期：2026-08-08｜基线：`6285d3202`（任务书）
> 本报告与 Mimo 独立分析（`structure-audit-module-level-mimo-2026-08-08.md`）交叉验证

---

## 0. 方法说明

- 范围：Desktop 7 模块 + Server 8 模块 + LocalWebAPI + Shell/Core + F 类遗留 + Tools/测试
- 工具：目录结构扫描 + 文件规模统计 + grep 引用验证（符号级由 Mimo 报告补充交叉）
- 与 A-16 全局审计衔接：全局层（依赖/契约/双轨）结论直接引用，本次聚焦**模块内部**

---

## 1. Desktop 模块内部（7 个）

### 总体结论：分层健康，组织有小分叉

| 模块 | 文件数 | 最大文件 | 分层 | 三分决策 |
|------|--------|---------|------|---------|
| Auth | 10 | LoginViewModel 325 行 | ViewModels/Views/Models，走 ILoginCoordinator | ✅ 保留 |
| Users | 16 | RemoteUserService 382 行 | 全目录齐全 | ✅ 保留 |
| Patients | 25 | PatientMasterDetailVM 342 行 | 有 **Interfaces/**（独有） | 修补（Interfaces 归位） |
| Herbs | 14 | RemoteHerbService 309 行 | 全目录齐全 | ✅ 保留 |
| Formula | 18 | FormulaService 340 行 | 全目录齐全 | ✅ 保留 |
| MedicalCase | **48** | MedicalCaseCommandsVM 513 行 | 目录最全（6 子目录） | 修补（超大 VM 拆分） |
| Registration | 9 | RegistrationListVM 354 行 | 目录精简 | ✅ 保留 |

### 发现 D1（P2）：Patients 模块 Interfaces/ 目录独有

- `Modules/LYBT.Desktop.Patients/Interfaces/` 含 `IMedicalCaseStartCoordinator/IPatientSearchCache/IPatientValidator`——**其他 6 个 Desktop 模块无此目录**
- 推理：模块内部组织不统一；这些接口应就近放 Models/ 或按惯例（如其它模块接口放 Services/ 旁）归位
- 建议：统一 Desktop 模块内部目录规范（文档化），Patients 对齐

### 发现 D2（P2）：Service 命名混乱（A-08 遗留未决）

| 模块 | Service 文件 |
|------|-------------|
| Patients | PatientCardReaderIntegration / PatientSearchCache / **PatientSearchManager** / PatientService |
| Herbs | **HerbSearchProvider** / RemoteHerbService |
| Formula | **FormulaSearchProvider** / FormulaService |
| MedicalCase | ChangeTracker / CommandService / EditContext / LifecycleService / QueryService / Service（6 个命名各异） |

- 推理：同类职责（搜索辅助）Patients=Manager、Herbs/Formula=Provider；MedicalCase 6 个 Service 命名无统一规律。A-08 已报告「~20 处 Manager/复数/Handler 不一致待决策」，**至今未决**
- 建议：统一 Service 后缀规则（如 `{X}Service` / `{X}QueryService` / `{X}CommandService`），改名影响面大需产品拍板（与 A-08 合并决策）

### 发现 D3（P2）：超大 VM（MedicalCaseCommandsViewModel 513 行）

- `MedicalCase/ViewModels/MedicalCaseCommandsViewModel.cs` 513 行——接近 A-04「>600 行拆分」阈值
- 建议：后续若继续增长再拆，当前记录观察项

### 发现 D4（✅ 正面）：VM 分层健康

- 抽查 `PatientMasterDetailViewModel`：注入 `IViewModelServices/IMasterDetailServices/IPatientService/IPatientStatusHandler/IDesktopCacheManager`——**走 Service 层，无直连 IApiClient/HttpClient**
- Auth `LoginViewModel`：注入 `ILoginCoordinator/IApplicationStateService` 等——认证逻辑走协调器，合理
- code-behind：`MedicalCaseEditControl.xaml.cs` 283 行主要为 **DependencyProperty 定义**（WPF 特性，非业务逻辑）——可接受

---

## 2. Server 模块内部（8 个）

### 总体结论：结构清晰，MedicalCase 与 Reports 是特化（已文档化）

| 模块 | 文件数 | 最大文件 | 结构 | 三分决策 |
|------|--------|---------|------|---------|
| Auth | 28 | JwtService 379 行 | CQRS（Application 15）| ✅ 保留 |
| Users | 31 | BaseUsersController 303 行 | CQRS（Application 20）| ✅ 保留 |
| Patients | 24 | BatchImportHandler 120 行 | CQRS（Application 16）| ✅ 保留 |
| Herbs | 24 | HerbService 162 行 | CQRS（Application 13）| ✅ 保留 |
| Formula | 23 | FormulaService 156 行 | CQRS（Application 15）| ✅ 保留 |
| MedicalCase | 21 | MedicalCaseQueryService **538 行** | **Services/ 8 个，无 Application/** | 修补（QueryService 拆分）|
| Registration | 28 | BaseRegistrationsController 145 行 | CQRS + Hubs | ✅ 保留 |
| Reports | 7 | ReportRepository 213 行 | Service+Repository 只读 | ✅ 保留（特化合理）|

### 发现 S1（P2）：MedicalCase 完全 Service 化 vs 其他 7 模块 CQRS

- MedicalCase 无 Application/ 目录，8 个 Service（CommandService/QueryService/StateService/PrescriptionService/Helper...），**与 Auth/Users/Patients/Herbs/Formula/Registration 的 Application/ CQRS 结构分叉**
- A-14 已文档化「MediatR+Service 混合注入是有意设计」——**但那是 Controller 层混合；模块内部结构分叉（MedicalCase 无 Handler/Command/Query 文件）是另一回事**，需确认是有意还是 A-03 简化后遗留
- 建议：文档化 MedicalCase 模块结构（作为「复杂领域 Service 化」的样板），或评估回归 CQRS 一致性（P2，需产品/技术决策）

### 发现 S2（P2）：MedicalCaseQueryService 538 行

- 接近 A-04 阈值，含 Query/GetPatientConsultations/GetPatientPrescriptions 等多职责
- 建议：若再增长拆分（记录观察项）

---

## 3. LocalWebAPI

| 检查 | 结果 |
|------|------|
| Controller 质量 | ✅ A-17 已补 CRUD，语义与 WebAPI 对齐（除已记录差异）|
| Program.cs 组织 | ✅ 复用 Server 模块（ADR-0010）|
| 重复业务逻辑 | ✅ 无（全部走 Server 模块）|
| 剩余差异 | 无 SignalR（有意）、本地配置已落盘（A-18 P1-6）|

**三分决策：保留**。LocalWebAPI 是「薄宿主 + Server 模块复用」，结构健康。

---

## 4. Shell/Core 层

### 发现 C1（P1）：Infrastructure 职责过载（Mimo F1-2 独立确认）

`Desktop.Infrastructure/` 14 类职责：Http/ViewModels/Views/Windows/CardReader/Navigation/Behaviors/Roles/Security/Helpers/Commands/Events/LocalData/Performance

- 推理：Core AGENTS 定义 Infrastructure =「WPF services — VM 基类/Dialog/Navigation/Behaviors」，但实际混入 Http 客户端适配（应属 Foundation）、CardReader 硬件（应独立）、LocalData（休眠，Mimo 确认生产 0 引用）
- 建议：**CardReader 独立成 `Desktop.CardReader`**、Http 适配已收窄（A-18 后统一 IApiClient）、LocalData 删除或并入（与 F-03 联动）

### 发现 C2（✅）：Shell 组合根健康

- Shell 引用全部模块（Prism 惯例），模块注册走 Module.RegisterTypes，无异常

---

## 5. F 类遗留评估（8 项）

| ID | 遗留 | 审计结论 | 三分决策 |
|----|------|---------|---------|
| F-01 | FeatureToggle：14 开关仅 1 被检查 | **FeatureToggleOptions 存在但几乎未接入**——是「功能开关」机制是否还需要的问题 | 待产品决策：若保留需统一接入方式（I*Options 注入）；否则删除机制（推荐评估删除，YAGNI）|
| F-02 | 桌面 Mapper 统一 | A-18 P1-4 后 **Server 已统一 Mapperly**；桌面侧 MedicalCaseMapper 等需核实 | 修补（对齐 Mapperly）|
| F-03 | LocalData Mapper Target | **LocalDbContext 休眠**（Mimo 确认生产 0 引用）——LocalData 路径已废弃 | 删除或标注废弃 |
| F-04 | SyncService CS8602 | SyncService 若随 LocalData 废弃则一并处理 | 与 F-03 联动 |
| F-05 | PatientMapper 死代码 | 未找到独立 PatientMapper.cs（可能已删）——需 Mimo 确认 | 核实后删 |
| F-06 | 打印模板扩展 | 低优先级，现状记录 | 后置 |
| F-07 | API 版本化 | 低优先级，现状记录 | 后置 |
| F-08 | 日志归档 | 低优先级，现状记录 | 后置 |

---

## 6. Tools + 测试

| 项 | 结论 |
|----|------|
| Tools 4 个 | ApiTester/LoginTester 是开发辅助（保留）；PasswordHashGenerator 有 Identity 兼容注释（保留，运维用）；UserInfoVerifier 核实存活 |
| 测试项目 | Architecture 84 条守卫健康；Server/Desktop 测试与代码同步（抽查未见大偏差）|

---

## 7. 统计汇总

| 维度 | 结果 |
|------|------|
| P0 | 0 |
| P1 | 1（C1 Infrastructure 职责过载）|
| P2 | 7（D1 Interfaces 独有 / D2 Service 命名 / D3 超大 VM / S1 MedicalCase 结构分叉 / S2 QueryService / F-01 FeatureToggle / F-03 LocalData）|
| ✅ 健康 | Desktop VM 分层 / LocalWebAPI / Shell / Reports 特化 / Auth 协调器模式 |
| 三分决策 | 保留 12 / 修补 6（D1/D2/D3/S1/S2/F-02）/ 重写 0 / 待产品决策 2（F-01/F-03）|

---

## 8. 给产品负责人的结论（业务语言）

1. **好消息**：Desktop/Server 模块内部**分层全部健康**（VM→Service→Repository 不越层），LocalWebAPI/Shell/Reports 结构都合理——**没有需要重写的模块**。
2. **主要问题**：① Infrastructure 一坨 14 类职责（建议 CardReader 独立、LocalData 废弃）；② Service 命名混乱（A-08 遗留未决，需你拍板是否统一改名）；③ MedicalCase 模块结构与其他模块分叉（需确认是有意）。
3. **F 类**：多数是「旧机制残留」（LocalData/SyncService 已休眠），建议删除而非修补；FeatureToggle 机制本身建议评估删除（14 开关仅 1 用）。
