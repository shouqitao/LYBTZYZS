# 前端深度架构审查报告（HTTP → ViewModel 六层）

> 日期：2026-09-16 ｜ 范围：`src/Client/Desktop/**`、`src/Server/**`（HTTP/Service 边界）
> 审计方式：6 个只读侦察代理逐层扫描 + 主代理复核 + 真机/集成测试验证
> 关联：`docs/03-architecture/13-project-master-plan.md`（§九 决策）、`13c-current-status.md`（§三 状态）

---

## 一、审计报告（设计模式 · 最佳实践 · 现状 · 差距）

### L1 HTTP 层（Controller，双端 30 个）

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| 双端同名端点逐一对位 | 端点集合/签名/策略/返回结构完全一致 | 5 处缺口：Remote 缺 `GET /medicalcases/pending`；Local 缺 `PUT /configuration`；Local 独有 diagnostics `db-info/version/logs/recent` 与 `medicalcases/by-status`；创建状态码 200/201 不一 | 端点集合与语义分叉 | P1 |
| 命名单一策略 + 类级声明 | 默认拒绝（FallbackPolicy）+ 每写操作显式策略 | Remote 有 FallbackPolicy；**Local 无** → fail-open | 默认拒绝语义双端相反 | P1 |
| 全局异常处理 + `ApiResponse` 信封 | 双端同一实现、同一错误体 | Remote = `UseExceptionHandler` + `StatusCodePages` + `ProblemDetails` + `IExceptionHandler` 链；Local = `SharedHost` 内联 lambda，无 StatusCodePages/ProblemDetails/模型校验定制 | 两套错误契约 | P1 |
| 命名限流策略 + 端点标注 | 登录与写操作均限流、按 IP 分区、429 结构化 | Remote：`Login`(5/60s/IP)+`ApiCalls`(100/min/IP)+结构化 `OnRejected`；Local：仅 `LocalLogin`（全局窗口、无 `ApiCalls`、无 `OnRejected`） | 写操作无限流 + 继承端点 500 + 维度不同 | P1 |
| Controller → Service → Repository | Controller 只编排，禁注 Repository/DbContext/Store/Manager | 无 `AppDbContext` 注入（合规）；Local Diagnostics 注 `ISystemLogRepository`、Configuration 注 `IConfigurationStore`、Health 注 `UserManager` | 3 处跨层注入（本地端） | P2 |
| 201 创建 / 200 更新 / 404 不支持 | 与 `ProducesResponseType` 及双端一致 | MedicalCases Local 200 vs Remote 201；基类未 override 动作抛 `NotSupportedException` → 500；Remote `Cancel` 标注 204 实际 200 | 状态码语义漂移 | P2 |
| 复数资源 + 层级子资源 | 统一状态变更动词与查询动词 | 资源命名合规；`toggle-status` 用 POST、`status/close/suspend/cancel` 用 PUT；批量查询用 POST | 动词约定混用（双端一致，属既有约定） | P3 |
| `page/pageSize` + 上限校验 | 参数命名统一 + `ValidatePagination` | CRUD 统一 `page/pageSize`；MedicalCases 查询用 `PageIndex/PageSize`；Local 覆盖版丢失 `ValidatePagination` | 命名不统一 + 一处无上限 | P2 |
| `ProducesResponseType`/`Tags`/`ApiVersion` | 双端标注一致 | Remote 全量标注 + `[ApiVersion]` + `[Tags]`；Local 大面积缺失且固定 `api/v1`（无版本化） | 本地端文档缺失 | P3 |
| `SharedHost` 共享 7 步管道 | 双端同序同件 | 共享 `UseRouting/CORS/RateLimiter/AuthN/AuthZ/MapControllers`；Local 缺 ClaimsNormalization（`TryInvokeClaimsNormalization` 为空壳）、StatusCodePages、SecurityHeaders、CorrelationId | 本地端管道缺件 | P2 |

### L2 API Client 层（Refit / SwitchingApiClient）

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| `IApiClient` 聚合 10 子域 + 窄接口 | 按域收敛 + 按消费者拆窄接口 | 聚合合理；`IAuthApiClient`/`IUserManagementApiClient` 仅作基接口，无消费者无注册 | ISP 拆分空转 | P3 |
| `IEntityApiSegment<TList,TDetail,TInput>` + DIM | 同形状用同泛型契约 | 仅 4/10 子接口实现；`category` 形参对 Identity 静默忽略 | 半覆盖 | P3 |
| 「当前模式不支持」单一契约 | 单一显式语义 | 三种并存：`ConfigurationHttpApiClient` 抛 `NotSupportedException`、`DeployHttpApiClient` 返回 `CreateFail`、`IdentityApiClient.GetCurrentUserAsync` 远程抛异常 | 调用方只能 `catch(Exception)` 兜底 | P2 |
| 契约层统一末尾 `ct` | 逐层透传 | 10 个子接口**全部无 `ct`**；仓储形参不传给 API | 取消是假契约 | P2 |
| 委托 `IConnectionSettingsService.IsLocal` | 解析 `Uri` 后按 Host+Port 判定 | `Contains("localhost:5300")`/`Contains("127.0.0.1:5300")` | `::1`、无端口、`127.0.0.1:53000` 误判 | P2 |
| volatile + lock 双检 | 双重检查锁定 | 双检正确；缓存键用 `CurrentUrl`、创建分支用 `IsLocal`，两次独立读取 | 无实际竞态，缺单一事实来源 | P3 |
| 切换时释放被替换资源 | 传输资源由持有者释放 | `RefitApiClient/HttpClientApiClient.Dispose` 只置空字段；`LocalApiHttpClientFactory.Dispose` 无调用点 | 每次切换泄漏一条 handler 链（有界） | P2 |
| `AddHttpClient` + 池化 + 弹性 | 具名客户端 + `PooledConnectionLifetime` + 重试/熔断 | **全仓 0 处 `AddHttpClient`**；Polly 仅在 `Directory.Packages.props` 声明无人引用；`IsRegistered<IHttpClientFactory>()` 恒 false → 工厂分支死代码 | 无重试/退避/熔断/DNS 刷新 | P2 |
| 单一配置源超时 | `ApiClientOptions.TimeoutSeconds` | 远程/本地用配置；刷新恒 30s；健康检查自建 CTS 5s | 刷新链路不受配置控制 | P3 |
| 适配器层统一转领域异常 | 携带服务端 message | 本地非 2xx → `HttpRequestException(原始响应体, StatusCode)`；`ClientErrorMessageMapper` 对 `HttpRequestException` 只按状态码映射、丢弃 Message | 同业务失败双模式文案不同 | P2 |
| 单一真相源路由 | 属性即契约，或集中路由常量 | 1 域 = Refit 接口 + 子接口 + 2 适配器 + 控制器路由（4 份形状）；10 个 Refit 适配器零逻辑直通 | 路由漂移只能在运行期暴露（既往已发生：药材导出双端 404） | P2 |
| 传输安全 | 生产禁用证书校验须显式 | `appsettings.Production.json` `IgnoreSslErrors=true`（BaseUrl 为 http）；`UnifiedApiClientExtensions` 无条件接受任意证书 | 切 https 即静默失去校验，无环境护栏 | P2 |
| DI 生命周期 | 模式绑定对象不得被单例长期持有 | 子接口均 transient；`ReportRepository` 以 Singleton 注入聚合 `IApiClient`（此例恰好安全） | 与 P1-1 约定不一致 | P3 |

### L3 Repository 层（客户端）

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| 模板方法 + 泛型 API 段 | 基类承担类型化 CRUD | `ApiClientRepositoryBase<TList,TDetail>` **泛型参数在类体内 0 引用**；`ReportRepository` 被迫写 `<object,object>` 且完全绕过模板 | 命名上的假象 | P2 |
| 同层统一错误契约 | 调用方可区分「无数据」与「失败」 | 写路径抛 `InvalidOperationException`；读路径**返回 null/空分页且不看 `Success`**；导出吞异常返回 null；`UserRepository` 直接返回应用层 `CommandResult` | 同层契约三分裂 → 静默数据丢失 + 误导性错误信息 | P1 |
| `CancellationToken` 全链传播 | 逐层透传 | 接口声明齐全，**API 段契约 0 处 `ct`** → 基类形参被丢弃；`ExecuteBatchDeleteAsync` 连形参都没有 | 层内 100% 断裂 | P1 |
| 有界、可失效的缓存 | TTL + 主动失效 + 写后失效 | `DesktopCacheManager` 按 `GET:/api/v1/*` 前缀清理，但**全仓无缓存生产者**（0 处 `IMemoryCache.Set/GetOrCreate`）；失效调用点 100% 在 VM；`Registrations/Reports` 无失效入口 | 失效空转 + 写路径漏失效 + 无界 | P2 |
| 仓储只做数据访问 | 业务规则归 Service | `UserRepository.GetDoctorsAsync` 客户端过滤前 100 条 → 用户 >100 时**医生下拉静默缺人**；`PatientRepository.GetByIdNumberAsync` 客户端 N+1；4 处硬编码 `pageSize=100` | 业务规则渗入仓储并造成截断缺陷 | P2 |
| 继承链共享模板 | 横切模板上提 | 导出/下载骨架在 3 个仓储各写 2 份；状态/恢复/批量四件套 3 份同构；医案生命周期 6 个方法 6 份 | 策略变更需改 10+ 处 | P2 |
| 全部经 `IApiClient` | 禁止 `new HttpClient` | 7 个仓储全部经 `IApiClient*`；无 `Http{Domain}Repository` 遗留 | 合规（本层最干净的部分） | — |

### L4 Service 层

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| Controller → Service → Repository | 禁 `AppDbContext` 直注（P10） | `CancelRegistrationCommandHandler` 直注 `AppDbContext` 直查医案实体（只读、零写入） | P10 违规；门禁只扫类名以 `Service` 结尾者 → Handler 漏检 | P1 |
| 跨模块经接口（P07/P08） | 不跨模块直连 | 跨模块仅经 `IXxxCrossModuleService` + SignalR；无模块直引 | 合规（但该 Handler 除外） | — |
| 跨聚合写原子性 | 显式事务/UnitOfWork | 医案完成/取消先提交本聚合再联动挂号；创建医案后另库联动挂号；**全仓仅 1 处显式事务** | 非原子写入（TOCTOU 窗口） | P2 |
| Service 承载业务逻辑 | 避免贫血透传 | 客户端 Service 基本为 Repository 一行委托（`PatientService` 为代表）；业务规则部分落在 VM（`PrescriptionPrintHandler` 的价格/折扣） | 贫血 + 规则上浮 | P2 |
| 生命周期与状态匹配 | Singleton 不持可变会话态 | `MedicalCaseEditContext` 注册为 Singleton 且承载可变编辑会话（处方行集合/基线快照） | 生命周期错配 | P2 |
| 异常策略 | 不用异常表达业务失败 | 审计写失败全量吞异常（`SecurityAuditService`）；`CrudServiceBase` 以 `NotSupportedException` 作回退控制流 | 静默失败 | P2 |
| Service 层不依赖 UI | 无 WPF 类型 | `AppStartupOrchestrator` 直接 `MessageBox.Show`；`PrescriptionPreviewWindowBuilder` 直接 `new Window().ShowDialog()`；`LoginCoordinator` 直用 `Application.Current.Dispatcher` | 可测性破口 | P2 |

### L5 ViewModel 层（55 个）

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| View → VM → Service | VM 只经 Contracts 服务接口取数 | 1 个 VM 直连 Repository；2 个 VM 内联 HTTP/传输层载荷；14 个文件引用 WPF/SDK 类型 | 分层约束在少数 VM 破口 | P1 |
| CommunityToolkit 源生成命令 | 统一源生成 + CanExecute 谓词 | 源生成为主；1 处手写 4 个命令；公共 async 方法 + `[RelayCommand]` 双入口 | 风格不统一 + 重入 | P2 |
| `[ObservableProperty]` + `[NotifyPropertyChangedFor]` | 单一机制；派生属性显式通知 | 生成器/手写 `SetProperty`/Prism `BindableBase` 三套混用；4 处派生属性无通知；`MVVMTK0034` pragma 直改生成字段 | 陈旧 UI 风险 | P2 |
| 子 VM 经 DI/工厂注入并对称释放 | 父 VM 释放自有子 VM | 4 个父 VM 手写 `new`；`PendingQueue`/`Commands`/3 面板/4 个编辑器子 VM 无释放点 | 生命周期契约不完整 | P2 |
| 订阅对称 + CTS/Timer 释放 | Dispose/导航离场释放 | `ServiceEventBridge`/`EventSubscriptionManager` 已对称；热点 VM 存在重复 `+=` 未配对、匿名 lambda 不可退订、CTS 取消即释放竞态 | 重复回调与释放竞态 | P2 |
| async 入口有兜底 | 所有 async 入口 try/catch | 命令普遍有；导航与刷新链路 20+ 处 `_ =` 无兜底 | 失败静默 | P2 |
| 依赖 ≤5 | 超限按职责拆分 | 15 个 VM >5 依赖（Login 11、ConfigurationCenter 10、PatientMasterDetail 9） | 上帝对象信号 | P2 |
| `IDisposable`/`OnDisposing` 单一入口 | 唯一清理入口 | 三套并存 + 1 处空 override 死代码 | 漏释放风险 | P3 |

### L6 View 层（82 个 XAML）

| 设计模式 | 最佳实践 | 当前状态 | 差距 | 优先级 |
|---|---|---|---|---|
| 主题 Token 引用 | 禁止字面量色值 | 20+ 页面硬编码 `#FBF7F3`（`SurfaceLevel0Brush` 已提供等价 token）；`HerbItemControl` `Foreground="Black"` | 深色模式大面积失效 | P1 |
| `DynamicResource` 键必须已定义 | 定义与引用同源 | **3 个键全仓无定义**：`DarkOpacityBrush`（加载遮罩）、`HoverBackgroundBrush`/`SelectedBackgroundBrush`（列表 hover/选中） | 静默失效、无异常无日志 | P1 |
| 虚拟化面板由列表自身模板承载 | 禁外层 `ScrollViewer` 包裹 | `HerbListControl`：外层 `ScrollViewer` + `ItemsControl` + `VirtualizingStackPanel` | 虚拟化声明完全无效 | P1 |
| 特殊控件走单一 Behavior 通道 | 密码统一 `PasswordBoxHelper.BoundPassword` | `AccountSettingsControl`（3 个）与 `LoginView`：行为 + code-behind 双通道同步同一 VM 属性 | 每次击键写两次 + 同步竞态 | P1 |
| `Mode` 显式声明 | 只读显式 OneWay | 49 处 TwoWay 全部落在可写目标（无误用）；同文件 OneWay 与省略混用 | 声明不统一 | P3 |
| `UpdateSourceTrigger` 按需 | 仅实时反馈/校验用 `PropertyChanged` | 约 40 处 `PropertyChanged`，其中无实时需求字段同样逐键回写；`SearchBox` 使 5 个主从页逐键检索 | 输入迟滞 | P2 |
| 转换器单点注册 | 单一注册路径 | `Cvt` 静态门面（13 成员）与 `Converters.xaml`（10 键，仅 1 处使用）并存且缺 2 个类 | 双路径不一致 | P2 |
| 样式集中于主题字典 | 跨模块共享样式集中 | `ValidationErrorMessageVisibleStyle` 5 份重复；`HerbTag*` 2 份；打印模板 4 份内联样式 | 改一处要同步 2-5 个文件 | P2 |
| 响应式尺寸 | `*` 比例 + `MinWidth/MaxWidth` | 登录页固定 `Width="780"`（宿主 `MinWidth=1024`）；两个对话框固定 `1100×680` | 窄屏裁切 / 高 DPI 溢出 | P2 |
| i18n 经资源引用 | `.resx` 引用 | `StringResources.resx` 已存在但 XAML **0 处引用**，中文全部硬编码；`AutomationProperties` 全仓仅 1 处 | 基础设施与使用脱节 | P2 |
| `TargetType` / 禁 `EventSetter` | 样式完整、事件命令化 | 全部样式带 `TargetType`；`EventSetter` 0 处；code-behind 处理器 12 处 | 合规（仅 4 处死 `x:Name`、1 处可命令化） | P3 |

---

## 二、优化建议（按优先级）

| 优先级 | 建议 | 影响 | 工作量 |
|---|---|---|---|
| **P0** | 本地传输补 `AuthorizationMessageHandler`；工厂改为「共享 handler + 每次返回新 `HttpClient(disposeHandler:false)`」 | 本地模式受保护端点此前**恒 401**、第二次调用 `ObjectDisposedException`——本地模式事实上不可用 | S |
| **P1** | Local `FormulasController` 4 个端点改 `override`（路由被基类抢占/歧义 → 恒 500） | 本地模式验方删除/启停/批量删除/恢复全部 500 | S |
| **P1** | Local 注册 `ApiCalls` 限流策略 | 共享 `BaseUsersController` 的 batch-enable/disable 恒 500 | S |
| **P1** | Local 补 `FallbackPolicy` + `AuthController` 匿名面收敛 | 本地 fail-open：漏写 `[Authorize]` 的新端点默认匿名可达 | S |
| **P1** | Local 认证端点状态码收敛为 401（刷新/自动登录/validate） | 令牌失效在双模式行为不同（本地退化为业务失败，不触发重登） | S |
| **P1** | `TokenRefreshHandler` 目标地址改用当前连接 URL | 用户改 `RemoteUrl` 后，刷新/降级自动登录打到 appsettings 的 `BaseUrl`（错误主机，跨服务器提交 RefreshToken） | S |
| **P1** | 仓储读路径区分「失败」与「无数据」（`Success=false` 抛异常） | 「共 0 位患者」「患者不存在」掩盖真实权限/业务拒绝 | S |
| **P1** | 移除 `CancelRegistrationCommandHandler` 的 `AppDbContext` 直查（P10） | 模块边界破口；门禁漏检 | S |
| **P1** | 补 3 个未定义主题键；修 `HerbListControl` 虚拟化；去 `PasswordBox` 双通道 | 遮罩透明、选中态消失、大处方渲染开销线性上升、密码双写 | S |
| **P1** | VM 层：修 CTS 竞态、子 VM 对称释放、重复订阅、不可退订 lambda、派生属性通知 | 轮询静默死亡、订阅累积、UI 陈旧 | S |
| P2 | 客户端契约层（`IApiClient*` / Refit 接口）统一补 `CancellationToken` 并逐层透传 | 取消形同虚设；页面离场后请求继续占用连接 | L |
| P2 | 统一领域错误层（本地/远程/超时归一到同一异常类型 + 文案） | 双模式文案分叉；本地丢失服务端 message；超时显示为「操作已取消」 | M |
| P2 | 引入具名 `HttpClient` + 池化 + 弹性（`AddHttpClient`/`PooledConnectionLifetime`/重试） | 无退避重试与熔断；删掉误导性的 Polly 注释与文档 | M |
| P2 | 缓存层落地真实生产者或删除空转失效（含 `Registrations/Reports` 域） | 每次失效一次空转 + 反射全表枚举 | M |
| P2 | 合并双份适配器 / 路由常量集中 + 契约-控制器对拍测试 | 1 域 4 份形状，漂移只能运行期发现 | L |
| P2 | 20+ 页面的字面量色值改主题 Token | 深色模式大面积失效 | M |
| P2 | 重复样式与打印模板提取到共享字典 | 版式调整需改 2-5 个文件 | M |
| P2 | Local 控制器补齐与 Remote 同名同义的端点与限流标注 | 双端 API 树不可互换 | M |
| P2 | 跨聚合写引入显式事务 | 非原子写入 | L |
| P3 | 清理死资源/死 `x:Name`/未用转换器；删除 `RepositoryExecutionHelper` 或完成 T3.4 组合化 | 启动合并开销与误导性参考实现 | S |
| P3 | i18n 落地（`{x:Static StringResources.*}`）+ `AutomationProperties` | 本地化与可访问性 | L |

---

## 三、修复计划与执行结果

| 批次 | 内容 | 预估 | 状态 |
|---|---|---|---|
| B0 | 本地传输 P0（auth handler + HttpClient 所有权） | S | ✅ 已修复并验证 |
| B1 | L1 双端一致性 P1/P2（Local 验方路由、`ApiCalls` 策略、FallbackPolicy、认证 401、创建 201、分页校验、基类不支持动作 404、Remote `cancel` 标注、Remote 补 `pending`、refresh 限流、validate 匿名、注册路由收敛） | M | ✅ 已修复并验证 |
| B2 | L2 P1（刷新打向当前连接 URL） | S | ✅ 已修复并验证 |
| B3 | L3 P1（读路径失败语义） | S | ✅ 已修复并验证 |
| B4 | L4 P1（P10：移除 Handler 直注 `AppDbContext`） | S | ✅ 已修复并验证 |
| B5 | L5 P1（CTS 竞态、子 VM 释放、重复订阅、lambda 退订、派生属性通知） | S | ✅ 已修复（编译 + 既有测试回归通过） |
| B6 | L6 P1（未定义主题键、虚拟化、密码双通道） | S | ✅ 已修复（编译通过；见 §五 验证边界） |
| B7 | 未执行项（见 §四） | — | 已登记 |

---

## 四、本次未执行的建议（明确登记，非静默跳过）

| 项 | 原因 |
|---|---|
| 契约层补 `CancellationToken`（L2-04 / L3 取消传播） | 需改 10 个子接口 + 2 组适配器 + 仓储基类 + 全部调用方，属跨层契约变更；按「先文档后代码」应先出设计（是否需要 `SwitchingApiClient` 持 CTS 以取消在途请求，与已登记的 P2-15-5 决策冲突），不宜在审计批次内静默实施 |
| 引入 `AddHttpClient` + 池化/弹性（L2-02） | 属技术引入（T3），须走技术引入治理流程（分析 → 用户审批），且 `IsRegistered<IHttpClientFactory>()` 死分支与注释失真需一并定案 |
| 领域错误层统一（L2-06） | 影响面覆盖全部仓储与 VM 错误提示，需先定契约 |
| 缓存层（L3 缓存失效空转） | 需产品决策：是补齐 GET 响应缓存，还是移除空转失效与相关注释 |
| 20+ 页面主题 Token 化、样式提取、打印模板合并（L6-02/08/09/11） | 纯表现层批量改造，工作量 M-L，建议独立批次并以视觉回归验证 |
| i18n 落地（L6-14） | 工作量 L，且需先定键分区与 CI 门禁 |
| L4 跨聚合事务、`MedicalCaseEditContext` 单例生命周期、客户端 Service 贫血 | 需架构决策与回归面评估，超出「高优先级缺陷修复」范围 |
| Local 独有诊断端点与 Remote 缺口的最终归属（L1-15）、`toggle-status` 动词约定（L1-21） | 属 API 契约设计决策，需与双端 API 树定案一并处理 |
| 契约层 `RestoreAsync`/`GetCurrentUserAsync` 返回形状不一致（L2-10） | 契约变更，需同步调用方 |

---

## 五、验证与证据

### 门禁

| 项 | 结果 |
|---|---|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告** |
| `tests/LYBT.Tests.Architecture/` | **99/99 pass**（修复前 98/99 — `DP_M1` 存量失败，见下） |
| `tests/LYBT.Tests.Desktop/` 定向套件 | `FormulaCrudE2ETests` + `TokenRefreshE2ETests` + `LocalModeTransportTests` + `UserManagementE2ETests` = **25/25 pass**；`UserManagementE2ETests`（含新增批量启停用例）= **9/9 pass** |
| `tests/LYBT.Tests.Desktop/` 全量 | 870 项跑完后被 VSTest 会话超时（20 分钟）中止：**867 pass / 3 fail**；3 项失败已在 `HEAD`（`git stash` 基线）逐条复现，**与本批次改动无关**（见下） |

### 新增/更新的回归测试

| 测试 | 防御的缺陷 | 结果 |
|---|---|---|
| `Integration/LocalApi/LocalModeTransportTests`（新增 3 条） | 本地传输无 Bearer → 本地受保护端点 401；共享 `HttpClient` 被 `using` 释放 → 第二次调用 `ObjectDisposedException` | 3/3 pass（修复前：401 与 `ObjectDisposedException` 均实测复现） |
| `FormulaCrudE2ETests.FormulaWriteEndpoints_AreNotShadowedByBaseCrudRoutes`（新增） | Local 验方 delete / toggle-status / batch-delete / restore 四条路径 500 | pass |
| `UserManagementE2ETests.BatchDisableAndEnable_RoundTripsUserStatus`（新增） | Local 缺 `ApiCalls` 策略 → batch-enable/disable 500 | pass |
| `Unit/Shell/LocalOwnershipCheckLayeringTests`（删 2 条） | 原用反射断言方法名存在（实现钉死），改为由上述行为测试覆盖 | — |

### 既有失败（基线复现，与本次改动无关）

| 测试 | 现象 | 基线（`HEAD`） |
|---|---|---|
| `ConcurrencyAndIsolationE2ETests.Doctor_Queue_Isolation_OnlyOwnWaitingRegistrations` | 队列为空 | 同样失败 |
| `MedicalCaseE2ETests.Cancel_MedicalCase_RemovesFromList` | 422「非当天本人创建的医案取消时必须提供取消原因」 | 同样失败 |
| `RegistrationE2ETests.GetQueue_ReturnsWaitingRegistrations` | 队列为空 | 同样失败 |

### `DP_M1` 架构门禁（存量 → 已修复）

`DesktopLayerArchTests.DP_M1_ViewModels_Must_Not_Hold_Dto_As_Editable_Property` 在 `HEAD` 即失败（`SecurityAuditLogViewModel.SelectedLog`，`docs/03-architecture/13c-current-status.md` §三 已登记为存量）。
本次处理：`SelectedLog` 是 `DataGrid.SelectedItem` 的**只读选中项持有者**，其数据源 `Logs`（`AuditLogDto` 集合）本就在该守卫的只读展示白名单内；守卫既有先例（`SelectedPatient`/`SelectedHerb`）同属此类，故将其并入既有豁免清单并写明依据。**此为对守卫分类的调整，需架构负责人确认**；不建议通过改造只读审计日志页引入 Model 来「消除」该告警。

### 验证边界（未做的验证）

- WPF 视图未做视觉验收（本环境无法启动 GUI）；L6 改动（主题键、虚拟化、密码通道）以「全仓键定义/引用核对 + 编译通过」为证，**未做运行时目视验证**。
- L5 改动未新增单测（既有 VM 测试无相关断言）；以编译通过 + 既有 VM 测试回归通过为证。
- **Remote `GET /api/v1/medicalcases/pending`** 仅以编译通过 + 实现与 Local 对位为证，未做远程模式运行期验证（`RemoteApiTestBase` 需真实远程 SQL Server）。
- 本地传输超时由原硬编码 30s 改为跟随 `ApiClientOptions.TimeoutSeconds`（默认 60s）——属行为变更，未做超时场景的专项验证。
- `tests/LYBT.Tests.Server/` 未运行（需真实远程 SQL Server）。
