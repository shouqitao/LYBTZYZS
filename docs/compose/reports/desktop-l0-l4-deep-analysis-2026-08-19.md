# Desktop L0-L4 架构设计深度分析

**日期**：2026-08-19
**范围**：Desktop 项目全架构（L4 HTTP Client → L3 API Client → L2 Repository → L1 Service → L0 ViewModel/View）
**性质**：只分析不修改代码
**基线**：master HEAD（`9fe5583ca` 前），与 P0-1/P1-1/P1-2/P1-3/P1-4 在研改动一致

---

## 总览：跨层一致性短板

| 维度 | 现状 | 结论 |
|---|---|---|
| JSON 序列化 | Remote=Refit camelCase+enum-string；Local=PascalCase+case-insensitive，无 enum 转换器 | **不一致**，靠大小写不敏感桥接 |
| Repository 基类 | `EntityApiClientRepositoryBase`（4 仓）vs `ApiClientRepositoryBase`（2 仓）vs 无基类（ReportRepository） | **三套并存** |
| 错误处理 | CommandResult.Failed / 返回 null / throw InvalidOperationException / 吞异常记日志 多态并存 | **不统一** |
| 事件通知 | Prism PubSubEvent / 原生 EventHandler / CommandResult 三机制混用 | **不统一** |
| MVVM 框架 | CommunityToolkit `[ObservableProperty]` 主导 + Prism `BindableBase` 残留（PrescriptionItemViewModel） | 两框架共存（受 Mapperly 约束） |

---

## L4 — HTTP Client 层

### 4.1 HttpApiClientBase API 设计
- **当前状态**：单基类提供 `SendAsync`（统一方法分发 GET/POST/PUT/PATCH/DELETE）、`DeserializeEnvelopeAsync<T>`（信封解包）、`BuildPagedUrl`（分页/过滤）、`GetAndWrapAsync` 等便捷方法。设计合理，覆盖 Local 模式全部 HTTP 需求。
- **问题（中）**：`DeserializeEnvelopeAsync<T>` 的裸 T 回退仅对**非对象根**触发。JSON 对象形态会被 `ApiResponse<T>` 无感吞掉（未知属性跳过 → 空信封，`Success=false/Data=null`），不抛错、不警告。已在 Batch 1 单测实证（`DeserializeEnvelope_RawFormat_WrapsInEnvelope` 用字符串根才走回退）。**静默丢数据风险**：若 LocalWebAPI 某端点因契约偏移返回裸对象，客户端会得到空信封而非失败提示。
- **建议**：在信封反序列化后对 `Success==false` 且 `Data==null` 且响应体非空信封的场景加一条 `LogWarning`；或对 `DeserializeEnvelopeAsync` 增加「响应体明显示对象根但信封字段缺失 → 记警告」的判别。
- **收益**：把静默失败转为可诊断（日志层），约半天工作量。

### 4.2 RefitApiClient vs HttpClientApiClient 职责划分
- **当前状态**：两个并行的 `IApiClient` 实现——RefitApiClient（Remote，经 Refit 接口）+ HttpClientApiClient（Local，经 HttpApiClientBase 适配器）。每领域有**三份**表示：`I{Domain}Api`（Refit 接口）+ `{Domain}ApiClient`（Remote 适配器）+ `{Domain}HttpApiClient`（Local 适配器）。
- **问题（中）**：每领域三份类 = 大量重复/镜像（跨界镜像清单已登记 6 个，实际更多）。Remote 的 Refit 层与 Local 的手写 HttpClient 层存在**行为分叉风险**（序列化、header、错误封装各写一遍）。
- **建议**：中长期评估用「单一 DTO 契约 + 按模式切换的传输层」（Refit 适配器与 HttpClient 适配器都实现同一 `I{Domain}ApiClient` 而非镜像方法）。短期**不要**合并（Remote Refit 特性依赖、Local 原始 DTO 信封差异），只保证两侧对同一错误的语义一致。
- **收益**：消除行为分叉；长期显著减文件。短期仅为一致性评审。

### 4.3 SwitchingApiClient 路由 + 销毁
- **当前状态**：URL 路由（IsLocal→HttpClientApiClient / 其他→RefitApiClient），双重检查锁定 + volatile，模式安全（P1-1 配合把消费者改 transient）。
- **问题（高）**：**旧 client 从不 Dispose**。Batch 4 实证 `HttpClientApiClient`/`RefitApiClient` 均未实现 `IDisposable` → `_current as IDisposable` 恒 null → 模式切换后旧客户端（及其 HttpClient/底层 socket/Refit 实例）被**弃置不回收**。多模式频繁切换会累积资源（连接池/socket），长期驻留进程有泄漏风险。已登记为潜在泄漏点（Batch 4 master-plan）。
- **建议**：让 `HttpClientApiClient`/`RefitApiClient` 实现 `IDisposable`（释放持有的 HttpClient / IHttpClientFactory / Refit RestService），并在 `SwitchingApiClient.Current` 慢路径的 `oldClient?.Dispose()` 真正生效；`Dispose()` 暴露给测试（先前 HttpClient.Handler internal 不可测——需设计可观测出口）。
- **收益**：消除模式切换资源泄漏，属 P1 安全/资源治理范畴。

### 4.4 Handler 链组装与扩展性
- **当前状态**：Remote 模式链**齐全**（HttpClient → LoggingHttpHandler → AuthorizationMessageHandler → TokenRefreshHandler → HttpClientHandler，`UnifiedApiClientExtensions.remoteHttpClientFactory`）。Local 模式（HttpClientApiClient）**无** auth/refresh/logging 处理器（LocalWebAPI 用 1 年固定 JWT 避免刷新；无 Token 校验需求）。
- **问题（低-中）**：① Local 模式无 LoggingHttpHandler → **Local 请求无集中日志/相关性 ID**（日志能力 Remote/Local 不对称）；② 链组装在工厂局部 lambda 中，**不可单测/不易扩展新处理器**（只能在工厂里硬编码）。
- **建议**：① 将 Remote 链组装抽成可注入的 `DelegatingHandler` 组装器（工厂调用），使 L4IntegrationTests 能对"生产链"断言而非复制链；② 若需 Local 日志，可为 Local 客户端包一层 LoggingHttpHandler（评估启动/性能开销）。
- **收益**：链可测、可扩展；Local 日志补齐。

### 4.5 JSON 序列化一致性
- **当前状态**：Remote RefitSettings = **camelCase + JsonStringEnumConverter + case-insensitive**；Local HttpApiClientBase JsonOptions = **PascalCase（PropertyNamingPolicy=null）+ case-insensitive，无 enum 转换器**。
- **问题（高）**：两侧命名策略不一致。Local 依赖 `PropertyNameCaseInsensitive` 把 ASP.NET 输出的 camelCase 反序列化进 PascalCase 属性——能工作但**脆弱**（属性名大小写拼写错误不再报错，静默 null）；且 Local **无 enum 字符串转换器** → 枚举以数字序列化，若 LocalWebAPI 某端点返回字符串枚举会解析失败（未统一验证）。
- **建议**：统一为 **camelCase**（Remote 已是；Local 侧把 HttpApiClientBase 的 `PropertyNamingPolicy` 从 null→CamelCase 并加 `JsonStringEnumConverter`），并加一条架构守卫/测试锁定两侧配置一致（避免单侧漂移）。
- **收益**：消除序列化静默风险，为 Local 枚举字符串化铺路；低风险（两侧共同依赖 DTO 契约）。⚠️ 需对照 LocalWebAPI 实际契约（改前确认服务端枚举格式）——**禁止直接判单**。

---

## L3 — API Client 层

### 3.1 IApiClient 子接口结构
- **当前状态**：`IApiClient` 聚合 10 个子接口（Identity/Patients/Herbs/Formulas/MedicalCases/Registrations/Reports/Deploy/Diagnostics/Configuration），契约单一（A-18）。划分合理（领域维度）。
- **问题（低）**：`SwitchingApiClient` 实现 `IApiClient` 但每个属性都是转发 `Current.X`——若未来加第 11 个领域，需同步改 SwitchingApiClient + RefitApiClient + HttpClientApiClient + IApiClient + 双适配器（共 5-6 处）。扩展成本随领域线性增。
- **建议**：评估用「子接口代理自动生成/反射式弱转发」降本；当前领域数稳定，收益低，**列为长期观察项**即可。

### 3.2 子接口方法签名统一
- **当前状态**：部分方法用 DIM（default interface method）桥接——`IApiClientPatients.GetPagedAsync` 默认实现转发到 `GetPatientsAsync(page,pageSize,keyword)`；`GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync` 同理。命名不统一：`GetPatientsAsync` vs `GetPagedAsync`、`GetMedicalCaseByIdAsync` vs `GetByIdAsync`。
- **问题（中）**：DIM 虽优雅但**对测试/工具链不友好**——NSubstitute 不执行 DIM 体（Batch 6 实证：mock 直接调用 DIM 返回 null Task 致 NRE，须在段接口显式配置）；阅读者需理解默认接口实现语义。
- **建议**：在 Batch 6-8 演进时**收敛命名**（统一 `GetPagedAsync`/`GetByIdAsync` 等的调用面），逐步去掉为桥接而生的 DIM；短期保留（改动面大、Risky），登记为后续命名统一批次。
- **收益**：契约面统一 + 测试友好。

### 3.3 DTO 冗余
- **当前状态**：List/Detail/Input 三态 DTO 是标准；部分 `DetailDto` 携带父聚合字段（已有 refactor 批次清理，ADR entity-dto-model-refactor 记录）。仍有少量跨 DTO 别名字段（Notes/Remark 遗留处理过）。
- **问题（低）**：无系统性的 DTO→VM 冗余扫描；个别 VM 承担 DTO 门面（PrescriptionItemViewModel 自带计算属性），映射层（Mapperly）与手写并存。
- **建议**：保持现状，DTO 冗余随实体演进按 refactor 批次清理；不新增。

---

## L2 — Repository 层

### 2.1 泛型基类分裂
- **当前状态**：**三套并存**——`EntityApiClientRepositoryBase<TList,TDetail,TInput>`（Patient/Herb/Formula/User）；`ApiClientRepositoryBase<TList,TDetail>`（MedicalCase/Registration，形状特殊）；无基类（`ReportRepository`，P0-2 新增裸类）。
- **问题（中）**：同样「Repository」语义三种形态，CRUD 约定/日志模板/错误包装各自实现，新领域备注需要选哪种。`MedicalCaseRepository` 未用 Entity 基类（方法各异），属**有意为之**（形状特殊）；但边界模糊。
- **建议**：明确两点：① 标准 CRUD 实体一律走 `EntityApiClientRepositoryBase`；② 特殊形状实体（MedicalCase/Registration）与只读聚合（Report）走 `ApiClientRepositoryBase`（统一日志/ExecuteAsync 模板）——当前已大致如此，**补一条文档把分类规则写死**，避免第 7 个仓库又裸写。

### 2.2 方法与分页抽象
- **当前状态**：`GetPagedAsync` 签名不统一——Entity 基类含 `category` 参数，Patient 重载丢弃之；`BuildPagedUrl`（HttpApiClientBase）页参数默认一致。
- **问题（低-中）**：分页参数 `page/pageSize` vs `pageIndex/pageSize` 命名并存（`QueryMedicalCasesAsync` 用 pageIndex），跨仓不一致。
- **建议**：统一 `page` 命名（对齐 Entity 基类），分页查询接口收敛为单一 `(page, pageSize, keyword, ct)` 形状；非关键词类查询（QueryDto）保持 DTO 聚合。列入命名统一批次，低优先。

---

## L1 — Service 层

### 1.1 职责划分
- **当前状态**：清晰——`MedicalCaseService` 聚合代理委派 `Query/Command/Lifecycle` 三个专职服务（D5 收敛）；基础模块 Service 经 Repository 封装（P0-2 已把 AuditLog/Report 收口进 Repository 层；P1-2 去 Remote* 前缀）。命名已统一（`{Entity}Service`）。
- **问题（低）**：部分 Service 仍直接 `IApiClient`（Foundation 安全/生命周期基础设施 + Admin「服务门面」，已登记 P0-2 同类项，明确不属模块分层）。属**架构判定而非缺陷**。

### 1.2 错误处理策略
- **当前状态**：三态并存——① CommandResult.Failed（UI 友好，Service 层主流）；② 返回 null（Lifecycle 的 CloseCase/Suspend/UpdateStatus 用 `try/catch → 返回 null`）；③ throw InvalidOperationException（Repository 层 CRUD 失败；`ExecuteBatchDeleteAsync` 例外返回失败 DTO）。
- **问题（高）**：同一 Service 内 `CloseCaseAsync`（抛异常）与 `CancelMedicalCaseAsync`（catch→rethrow）、`RecordPrintAsync`（catch→null）并存**无统一裁决**；调用方无法预知「失败时是 null 还是抛异常」。这正是前几轮「分层/错误语义」反复踩坑的根源。
- **建议**：制定并落实「**Service 边界错误契约**」：① 读操作 → null/空集合（不抛）；② 写/状态变更 → CommandResult（返回失败原因）；③ 仅以下可抛——参数非法（ArgumentException）/ 基础设施彻底失效（且调用方确能处理）。以文档形式写入 `docs/03-architecture/` 分层规则附件，并作为后续 refactor 批次的裁定标准。**本轮只分析，不落码**。
- **收益**：消除调用方对错误语义的三态猜测；是 L1 层最高价值改进。

### 1.3 事件通知机制
- **当前状态**：Prism `PubSubEvent`（AuthEvents：Login/Logout/TokenRefresh/SessionExtended）+ 原生 `EventHandler`（HerbItemChanged）+ `CommandResult`（同步返回值）三机制混用，各有适用场景但无统一边界。
- **问题（中）**：`PubSubEvent` 全局广播 vs 实例级 `EventHandler` 的选用无规则；新功能易选错（全局事件泄漏/实例事件需手动管理生命周期——`NavigableViewModelBase.Events` 的 EventSubscriptionManager 已解决订阅清理，正确选项依赖）。
- **建议**：确立规则——进程级横切（认证/会话/模式）用 `PubSubEvent`；组件/控件内部联动用 `EventHandler`；同聚合内操作结果用 `CommandResult`。文档化并加架构评审检查项。低优先。

---

## L0 — ViewModel/View 层

### 0.1 ViewModel 基类体系
- **当前状态**：分层清晰——`NavigableViewModelBase`（富基类：IsBusy/Logger/Services/Events/RegionManager/UiThreadDispatcher）、`EditorViewModelBase<TContext>`、`MasterDetailViewModelBase`、`DialogViewModelBase`、`ChildViewModelBase`、`HerbItemViewModelBase`；轻量控件 VM 直接用 `ObservableObject`（P1-4 已评估合理）。
- **问题（中）**：基类较多且职责部分重叠（Navigable 含 Editor 能力经 partial 文件切分）；`PrescriptionItemViewModel` 仍是 **Prism `BindableBase`** 残留（P1-3 因 Mapperly 源生成可见性限制保留，已 TODO 登记）。
- **建议**：① 不合并基类（职责边界基本清晰）；② 为 `PrescriptionItemViewModel` 迁移保留专项（先解 Mapperly 对生成成员可见性问题——显式映射或生成器顺序配置，P1-3 已登记）；③ 评审是否有第三个 VM 继承 BindableBase（保持单一 MVVM 框架为主）。

### 0.2 绑定模式 / 命令
- **当前状态**：`partial VM + [ObservableProperty] + [RelayCommand]`（CommunityToolkit 8.4）为主导，绑定 P0X 前缀字段自动生成属性，`ObservableObject` 统一；命令用 `IAsyncRelayCommand`/`[RelayCommand(CanExecute=...)]`。一致性好。
- **问题（低）**：CommunityToolkit 8.4 生成器**不为 `CanExecute` 引用属性自动 Notify**（Batch switch-button-fix 实证，需手动 `NotifyCanExecuteChanged()`）——这是易踩点，应在研发规范中注明。

### 0.3 导航模式
- **当前状态**：Prism `RegionManager` + `ViewModelLocator`（AutoWireViewModel）+ `[RelayCommand]` 导航，标准且一致。
- **问题（低）**：无。

---

## 优先级排序（建议落地批次）

| 优先级 | 项 | 层 | 建议 |
|---|---|---|---|
| **高** | SwitchingApiClient 旧 client 从不 Dispose（资源泄漏） | L4 | 两实现实现 IDisposable + 慢路径真正释放 + 可观测出口 |
| **高** | Service 错误处理三态并存无契约 | L1 | 制定 Service 边界错误契约并裁定 |
| **高** | JSON 序列化 Remote/Local 不一致 | L4 | 统一 camelCase + enum 转换器（先对照 LocalWebAPI 契约） |
| 中 | DeserializeEnvelopeAsync 裸对象静默空信封 | L4 | 加诊断日志 |
| 中 | Repository 三套基类边界 | L2 | 文档固定分类规则 |
| 中 | 子接口 DIM/方法命名不统一 | L3 | 收敛命名批次 |
| 中 | 双 MVVM 框架残留（PrescriptionItemViewModel） | L0 | 专项迁移（先解 Mapperly 可见性） |
| 中 | Local 模式无 LoggingHandler | L4 | 链组装抽出 +（可选）Local 加日志 |
| 低 | IApiClient 子接口扩展经济 | L3 | 长期观察 |
| 低 | 事件通知三机制边界 | L1 | 文档化规则 |
| 低 | 分页 page/pageIndex 命名 | L2 | 命名收敛 |
| 低 | VM 基类职责重叠 | L0 | 评审不合并 |

---

## 结论

架构主干健康：分层（L4→L0）、契约单一（IApiClient）、VM 基类体系、子接口聚合、Round-trip 测试覆盖（Batch 1-7 = 85+）均达较高成熟度。**最大的系统性风险集中在跨层一致性**——JSON 配置、Repository 基类、Service 错误语义、事件机制四处「同层不同标」。**最高价值改进**是 L4 资源泄漏治理 + L1 错误契约 + L4 JSON 归一，建议优先作为后续独立批次（各自先文档/ADR 再落码）。

*（本报告为纯分析，未修改任何代码；涉及改动的建议项均待独立任务书/ADR 驱动。）*
