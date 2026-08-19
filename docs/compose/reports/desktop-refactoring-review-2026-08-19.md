# Desktop 架构重构 Review（2026-08-19）

> 任务书：`.hermes-task-refactoring-review.md`（已删除）
> 范围：P0-1/P0-2/P1-1/P1-2/P1-3/P1-4 + L4（IDisposable/JSON 统一/诊断日志）
> 方法：逐项 git diff 审查 + 设计文档对照 + 构建/测试验证

---

## 1. P0-1 Repository 生命周期统一（Singleton → 最终 Transient）

### ✅ 通过（但被 P1-1 修正回退）

**commit**：`96802bd97`（6 Repository `Register`→`RegisterSingleton`）

- 前提声明「蓝图 §3.2 要求 Singleton」**不实**——蓝图 §3.2 实为模块文件数表（已核实 `14-structure-design-blueprint.md:280-290`），未对 Repository 生命周期作任何要求。
- P1-1（`b87073857`）复核后修正：注入 transient 子接口后，Singleton 仓库会在模式切换后持有陈旧引用（重蹈 desktop-di-fix/ConnectionModeService 修复的 ObjectDisposed 类 bug），故改回 Transient。**正确性优先的回退决策成立**。
- **master-plan 已记录修正**（`13-project-master-plan.md:337` P0-1 修正行），但 `13c-current-status.md` 尚未补 P0-1/P1-1 条目（见 §11 文档一致性）。

---

## 2. P0-2 Service 注入对齐蓝图 §3.5

### ✅ 通过（2 处注意点）

**commit**：`17f98a498`

- 6 列服务中 4 个已是模式 A（注入 IMedicalCaseRepository），仅 AuditLogService/ReportService 两处真模式 B 修正，分析准确。
- **AuditLogService 行为变化**：原 `result.Success && Data != null` → Succeeded，否则 Failed；现 Repository 解包后**恒 Succeeded**（Data==null → 空分页）。语义变化：服务端业务失败（Success=false 且带 Message）时，**原返回 Failed+Message，现返回空分页 Succeeded**——失败信息丢失。⚠️ 需关注：
  - 符合 ADR-0020 读操作契约（「读操作返回 null 或空集合」）；
  - 但 `MedicalCaseRepository.GetAuditLogsAsync` 对 Success=false 的响应**未抛异常也未透传 Message**，静默降级为空分页——与其余 Repository 读方法（GetById 直接取 Data 不检查 Success）模式一致，属既有约定；
  - 无 AuditLogService 单元测试覆盖此行为变化（仅 E2E `GetAuditLogs_ExistingCase_ReturnsAuditLogs`，需 localhost:5000）。
- **ReportRepository**：新建 L2 只读聚合，`ReportsModule.cs:26` 注册 **Singleton**。注入的是整体 `IApiClient`（SwitchingApiClient 本身是 singleton）——**安全**（非 P1-1 的 transient 子接口场景）；`_apiClient.Reports` 属性访问实时走 `SwitchingApiClient.Current`，模式切换后取当前模式客户端，无陈旧引用问题。
- `IReportRepository` 与 Server 侧 `IReportRepository` 同名镜像，XML 注释已说明「不同程序集，语义不同不可互换」——符合蓝图跨层镜像约定。

---

## 3. P1-1 Repository 注入子接口（最小权限原则）

### ✅ 通过

**commit**：`b87073857`

- 6 Repository 构造参数 `IApiClient` → 具体子接口，base 直接收子段，方法体 `_apiClient.Xxx.` → `_{sub}.`——diff 逐文件核对**无残留**（grep `IApiClient _apiClient` 仅剩 ReportRepository/Foundation 基础设施，属设计内）。
- DI：4 个新子接口注册（Patients/Herbs/Formulas/Registrations）均为 transient；Identity/MedicalCases 已有注册。**transient 子接口 + transient 仓库**的配对与 AuthHealthService 先例一致——每次解析仓库都重新取 `SwitchingApiClient.Current`，模式切换后拿新客户端，无陈旧引用。
- 测试同步改造为子接口 mock（MedicalCaseRepositoryTests/PatientRepositoryTests），构造路径正确。
- ⚠️ 注意：测试文件仅改了 MedicalCase/Patient 两个，其余 4 个仓库（Herb/Formula/User/Registration）的测试未在 P1-1 diff 中出现——需确认它们原本就不存在或已覆盖（grep `RepositoryTests` 见下）。

---

## 4. P1-2 Service 命名统一（去 Remote* 前缀）

### ✅ 代码通过 / ⚠️ 文档残留

**commit**：`84bd50c87`

- 5 个 Service git mv 重命名 + 类名/构造/ILogger<T>/4 个 Module DI/测试泛型全同步，无兼容层，grep `Remote\w+Service` 代码层**残留 0**。
- 横截面文档 `16-desktop-architecture-spec.md` 约定表已同步。
- **⚠️ 模块 README/AGENTS 未同步**（活文档残留旧名）：
  - `src/Client/Desktop/Modules/LYBT.Desktop.Users/README.md`（4 处：目录结构/注册表/章节名/风险段）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Registrations/README.md`（3 处）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/README.md`（2 处）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Registrations/AGENTS.md`（2 处）
  - `docs/compose/reports/` 历史报告（desktop-architecture-optimization-2026-08-18.md / desktop-requirements-inventory-2026-08-14.md）为**历史过程记录**，按「文档是字典不是过程」规则可保留。
- 历史决策行（13c D4、master-plan）按「历史不可改」保留原样，正确。

---

## 5. P1-3 PrescriptionItemViewModel BindableBase 迁移（回退）

### ✅ 通过（正确回退）

**commit**：`327d1f730` + `6fe2f855d`（master-plan 跟踪）

- 尝试 ObservableObject+[ObservableProperty] 后实证 Mapperly 源生成器不可见 CommunityToolkit 生成成员 → RMG004/RMG021/RMG066 → 空映射 → 保存处方运行时丢数据。
- 回退保留 BindableBase，类上 TODO 记录约束（`PrescriptionItemViewModel.cs:24-29`）。当前代码确认 `: BindableBase` 保留，无 ObservableProperty 残留。
- 属任务书逃逸条款「Mapperly 有依赖保持现状」的正确执行。

---

## 6. P1-4 HerbItem/HerbList VM 基类评估（维持现状）

### ✅ 通过

**commit**：`f1ffd1744`

- 两控件 VM 均为轻量 ObservableObject（无 IsBusy/Logger/EventAggregator 需求），评估结论成立。
- 无 CoreViewModelBase；唯一相关 HerbItemViewModelBase 在 Infrastructure，Controls 不可反向引用（Infrastructure→Controls 依赖方向）——层违反论证正确。
- 仅加 XML remarks 无功能变化，正确。

---

## 7. L4 — SwitchingApiClient IDisposable（ADR-0021）

### ✅ 通过

**commit**：`c56ec0893`

- HttpClientApiClient/RefitApiClient 实现 IDisposable，释放惰性子接口实例；不释放 DI 拥有的 IHttpClientFactory 与共享 handler-chain HttpClient——所有权边界正确。
- SwitchingApiClient 慢路径「先赋 `_current` 再 `oldClient?.Dispose()`」——顺序正确，杜绝自我销毁。
- Dispose 将惰性字段置 null，若旧实例被外部误复用会重建子接口而非抛 ObjectDisposed——**防御性更优**。
- ⚠️ 理论残留：已通过子接口 transient 注册消费方每次重新解析 `Current`，不存在持旧引用问题；Dispose 后 `_current` 若被再次访问（如切回同 URL）会重建新客户端而非复用已释放对象——安全。

---

## 8. L4 — JSON 序列化统一（ADR-0022）

### ✅ 通过（三层对齐核实）

**commit**：`c56ec0893`

- `HttpApiClientBase.JsonOptions` → camelCase + JsonStringEnumConverter，序列化测试断言已更新（`{"id":7,"name":"ZhangSan"}`）。
- **前提条件核实**：LocalWebAPI 原 `AddControllers` 无 JsonOptions——枚举为数字，客户端加转换器会双方向破坏；已同步在 `LocalWebApiProgram.cs:105-110` 加 JsonStringEnumConverter。**Remote WebAPI 已核实配置**（`ServiceCollectionExtensions.cs:160`）。三层（Remote/Refit/Local）camelCase + 字符串枚举统一成立。
- 测试：AuthControllerTests 验证枚举字符串契约（构建批次声明 125/125 通过；本机 E2E 类因 localhost:5000 未起无法复跑，但 HttpApiClientBase 序列化单测 34 项通过）。
- ⚠️ 潜在影响：**枚举字符串化改变 LocalWebAPI HTTP 契约**——任何直接调用 LocalWebAPI 的外部客户端（如有）需发字符串枚举；影响面限于进程内 LocalWebAPI（EmbeddedLocalWebApiService 场景），风险可控。

---

## 9. L4 — DeserializeEnvelopeAsync 诊断日志

### ✅ 通过

**commit**：`c56ec0893`

- 空信封（Success=false/Data=null/非空 body）记 Warning + 截断 body——静默数据丢失可观测化，正确。
- ILogger 经 HttpApiClientBase 构造 + 10 个 Local adapter + HttpClientApiClient + SwitchingApiClient 贯通，DI 注册一致。
- 测试覆盖：HttpApiClientBaseTests 空信封行为测试保留（断言不变，行为未改——只加日志）。
- ⚠️ 小点：`logger?.LogWarning` 的 `?` 使诊断在 logger 为 null 时静默（保持原行为）——设计合理（向后兼容静态调用）。

---

## 10. 测试与构建验证

### ✅ 构建门禁

- `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**（复跑通过，全 33 项目）。

### ⚠️ 测试覆盖

- 本批次相关单测（Repositories/HttpApiClientBase/SwitchingApiClient/HttpClientApiClient/RefitApiClient/AuthController 序列化等）34 通过，1 失败为 E2E `GetAuditLogs_ExistingCase_ReturnsAuditLogs`（需 localhost:5000，环境依赖非代码问题）。
- 全量 Desktop 466：346 通过 / 115 失败 / 5 跳过——失败**全部为环境性**：localhost:5000 连接拒绝（E2E/集成）+ WPF 控件 STA 线程（WorkflowStepIndicatorTests）+ LocalWebAPI 404（未启动服务）。与历史基线（13c 多次记录的「存量环境失败」）一致。
- `CardReaderPureTests.MaskIdNumber` 2 例失败（期望 null 得 ""）——**存量测试与实现不一致**，与本批次无关（未触碰 CardReader 代码），建议单独登记 backlog。
- ⚠️ 覆盖缺口：AuditLogService（P0-2 行为变化）与 ReportService/ReportRepository（P0-2 新增）无单元测试；6 仓库中仅 MedicalCase/Patient 有 Repository 单测。

---

## 11. 文档一致性

### ⚠️ 需关注

| 项目 | 状态 | 说明 |
|------|------|------|
| master-plan 总账 | ✅ | P0-1 修正/P0-2/P1-1/P1-2/P1-3/P1-4 行齐全（含 commit SHA） |
| ADR-0020/0021/0022 | ✅ | 0021/0022 已实施标注 + commit；0020 为「提议」状态（错误契约实施未完成，属既定 backlog） |
| 蓝图 §3.5 | ✅ | 分层链 + ADR-0020 错误契约表已并入 |
| 16-desktop-architecture-spec 约定表 | ✅ | P1-2 已同步 |
| **模块 README/AGENTS** | ❌ | Users/Registrations/Patients 3 个 README + Registrations AGENTS 仍引用 `Remote*Service` 旧名（4 文件 11 处） |
| 13c-current-status | ⚠️ | 最新条目停留在 2026-08-14，本批次 9 项（P0-1~L4）未追加条目 |

---

## 12. 潜在风险

1. **AuditLogService 失败静默**（低）：Success=false 的审计日志请求返回空分页而非错误信息——UI 显示「无日志」而非失败原因。若审计日志有业务意义（如权限不足提示），建议后续在 Repository 层对读操作失败抛 InvalidOperationException 或透传 Message。
2. **ReportRepository Singleton 注入 IApiClient**（低）：分析确认安全（SwitchingApiClient singleton + 属性实时转发），但若未来 SwitchingApiClient 生命周期变化需同步复核。
3. **LocalWebAPI 枚举契约变更**（低）：进程内服务，影响面限于 Desktop 自身消费，无外部调用方。
4. **README 陈旧**（中）：新成员依据 README 定位 RemoteUserService 等文件会失败（文件已改名），影响开发效率；属文档-代码一致性红线范畴，应尽快同步。

---

## 13. 总体评估

### ✅ 可以发布

9 项改动中 **8 项代码实现正确**（P0-1 前提不实但已由 P1-1 正确回退，净效果正确），1 项（P1-4）为纯评估无改动。构建 0 错误 0 警告；相关单测通过；三层 JSON 契约已核实统一；IDisposable 生命周期边界正确；回退决策（P1-3）有实证依据。

**发布前建议（非阻塞）**：
1. 同步 4 个模块 README/AGENTS 的 `Remote*Service` 旧名引用（文档-代码一致性红线）；
2. 13c-current-status 追加本批次条目（总账已记录，横截面待补）；
3. 后续 backlog：AuditLogService/ReportService 单测、CardReaderPureTests.MaskIdNumber 存量失败。
