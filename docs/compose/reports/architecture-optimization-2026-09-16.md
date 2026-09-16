# 架构优化 6 项：设计与实施报告

> 日期：2026-09-16 ｜ 范围：前端架构审查（`frontend-architecture-audit-2026-09-16.md`）§四 的 6 个未执行项
> 设计文档：`docs/compose/specs/architecture-optimization-2026-09-16/design-01..06`
> 决策记录：ADR-0028（HTTP 池化/弹性）、ADR-0029（响应缓存）、ADR-0030（跨聚合写一致性）

---

## 一、逐项结果

### 1. CancellationToken 全链 ✅ 已实施

| 项 | 结果 |
|---|---|
| 设计 | `design-01-cancellation-token.md` |
| 契约 | 所有异步公共方法末位追加 `CancellationToken ct = default`（参数名统一 `ct`） |
| 改动面 | 契约层 **226** 个 `Task` 方法（`Contracts/ApiClient/` 126 + `Contracts/Api/` 100，含 20 个 DIM 转发）；适配器层 **202** 个方法；`EntityApiClientRepositoryBase` 5 处 + `ApiClientRepositoryBase` 2 处「声明了 ct 却丢弃」的修复；模块仓储 **69** 处调用点透传；`IReportRepository` 3 方法（唯一未声明 ct 的仓储接口） |
| 实施修正 | **DIM 实现不可有可选参数**（CS1066）→ 20 个显式接口实现用 `CancellationToken ct`（无默认值）；DIM 声明侧保留默认值 |
| 实测缺陷（实施中发现并修复） | **12 处 `ct` 静默落入 `object? body` 形参**（`Post/PutAndWrapAsync(url, ct)`、`SendAndWrapAsync(url, Put, ct)`）→ 序列化 `CancellationToken` 抛 `NotSupportedException`（`System.IntPtr` 不可序列化）。已全部改为具名实参 `ct: ct`，并写脚本全仓复扫：**剩余 0 处** |
| 验证 | 上述缺陷由 `RegistrationE2ETests.StartVisit_CreatesMedicalCase_AndSetsInProgress` 实测暴露（11 项失败），修复后同套件全绿 |

### 2. 领域错误层统一 ✅ 已实施

| 项 | 结果 |
|---|---|
| 设计 | `design-02-domain-error-layer.md` |
| 服务端 | `AppException.GetHttpStatusCode()` 增加 **Category 兜底映射**（`AppException.CategoryToHttpStatusCode`）——此前无 `TypedErrorCode` 时一律 500，导致 3 处 `new NotFoundException(msg)` 返回 500 而非 404。新增 `ForbiddenException`（403）补齐唯一缺失的 HTTP 语义 |
| 客户端 | 新增 `ApiClientException`（`ErrorCode` + `ServerMessage`）+ `ApiErrorEnvelope.TryExtract`（本地/远程共用解析）；本地 `EnsureSuccessOrThrowAsync` 与远程 `RefitSettings.ExceptionFactory` 抛同一类型 |
| **实施修正** | 初稿为 `: Exception`，落地改为 **`: HttpRequestException`** —— 既有 `catch (HttpRequestException)` 站点（`LogoutService`/`ApiHealthCheckService`/`ConnectionModeService`/`FormulaImportDialogViewModel`/E2E 断言助手）全部保持有效，实现「统一形状 + 零破坏」 |
| Mapper | 新增 `ApiClientException` 分支（优先服务端 message）；删除 `Refit.ApiException` 反射分支与 `ExtractMessageFromApiResponse`（死代码）；修复 `TaskCanceledException` 排在 `TimeoutException` 之前导致**超时被显示为「操作被取消」** 的缺陷 |
| 验证 | `HttpApiClientBaseTests` 新增信封解析断言；`ExceptionMapping`/`DesktopExceptionHandler` 套件通过 |

### 3. 跨聚合显式事务 ✅ 已实施（并修正一处错误声明）

| 项 | 结果 |
|---|---|
| 设计 | `design-03-cross-aggregate-transactions.md`、ADR-0030 |
| **核心发现** | `StartVisitCommandHandler` 注释声称「原子事务」，实测**不成立**：事务开在 `RegistrationDbContext` 上，而医案写入走 `MedicalCaseDbContext`（不同连接）→ 事务不覆盖跨模块写入 |
| 决策 | 同 DbContext → 显式事务；跨 DbContext → **幂等 + 补偿**（不引入 `TransactionScope`，不破坏 ADR-0017 模块隔离） |
| 幂等实现 | 幂等键落在**挂号侧**（`Registration.Status == InProgress && MedicalCaseId.HasValue` → 直接返回既有医案）；`MedicalCase` 实体无 `RegistrationId` 列，MedicalCases 侧无法按挂号查重（已穷举接口确认），并**明确禁止**按 `patientId` 判重（同患者可有多条医案，误用会复用历史/他人医案） |
| 补偿修正 | 回滚后新增 `CompensateInMemory`（`RevertToWaiting`）——`RollbackAsync` 不清理 ChangeTracker，不还原会在同一作用域后续 `SaveChanges` 时**复活已回滚的写** |
| 附带 | `RecordPrintAsync`（MedicalCases + PrintLogs 同库同上下文，原两次独立 `SaveChanges` 存在半写窗口）补显式事务 |
| 验证 | `RegistrationE2ETests.StartVisit_CreatesMedicalCase_AndSetsInProgress`（真实 LocalWebAPI + LocalDB）通过；Server 套件全绿 |
| 如实标注 | 失败窗口（第 4 步已提交、后续回滚 → 孤儿医案）与重试安全性（BR-001 单活跃医案约束拦截，不产生重复医案）已写入代码注释 |

### 4. 缓存层定案 ✅ 已实施

| 项 | 结果 |
|---|---|
| 设计 | `design-04-cache-strategy.md`、ADR-0029 |
| 定案 | 进程内 `IMemoryCache` + **读写一体的传输层 handler** |
| **实施修正** | 初稿把生产者放 `HttpApiClientBase`、失效放仓储模板；落地改为 `CachingHttpMessageHandler`（`DelegatingHandler`）——① 避免改 20 个适配器构造器；② 挂链上可覆盖**全部**出站请求而非仅经仓储的写 |
| 读 | GET 2xx → 键 `GET:{path}{query}#{userId}`，条目显式 `Size = 1`（满足 `SizeLimit`，否则 `Set` 抛 `InvalidOperationException`）；TTL：目录类 5 min / 事务类 30 s |
| 写 | 非 GET 2xx → 按域前缀失效（含跨域依赖表 registrations ↔ medicalcases），**覆盖所有写路径** |
| 去反射 | 新增 `DesktopCacheKeyRegistry`（登记键 + 逐出回调注销），替代依赖 `MemoryCache` 私有集合的反射前缀删除 |
| 补域 | `IDesktopCacheManager` +`InvalidateRegistrationCaches`/`InvalidateReportCaches`/`InvalidateAll`；`CacheDomain` +`Registrations`/`Reports` |
| 隔离 | 键含用户域后缀，避免换用户串读；`/export`、`/import-template`、`/health`、`/download` 不缓存 |
| 验证 | `LocalModeTransportTests` 走真实缓存链通过（重复调用第二次为命中）；`DesktopCacheManager` 行为不变（事件发布保留） |
| 已知窗口 | 事务类域最长 30 s 陈旧（写经传输层立即失效；当前无绕过传输层的写路径） |

### 5. AddHttpClient + 池化/弹性 ✅ 已实施（技术引入，已出 ADR）

| 项 | 结果 |
|---|---|
| 设计 | `design-05-httpclient-resilience.md`、ADR-0028 |
| **关键约束** | 桌面主容器是 **Prism.DryIoc**，不能直接 `services.AddHttpClient(...)` |
| 方案 | `DesktopHttpTransportFactory` 用**私有 `ServiceCollection`** 构建传输层 `IHttpClientFactory`（3 个具名客户端 + `AddHttpClient` + Polly），产出的实例经 `RegisterInstance` 注入 DryIoc；Foundation 不依赖 DryIoc（自定义 handler 经 `IDesktopHttpHandlerProvider` 由 Shell 的 `DryIocHttpHandlerProvider` 用容器装配） |
| 具名客户端 | `RemoteApi`：Logging → Caching → Authorization → TokenRefresh → SocketsHttpHandler；`LocalApi`：Caching → Authorization → SocketsHttpHandler（**不含** TokenRefresh）；`RefreshToken`：裸链 |
| 池化/生命周期 | `SocketsHttpHandler` + `PooledConnectionLifetime=2min` + `PooledConnectionIdleTimeout=1min` + `SetHandlerLifetime(2min)` |
| 弹性 | `HandleTransientHttpError().OrResult(429)` + 3 次指数退避（200/400/800ms），**仅幂等方法**（GET/HEAD/OPTIONS/PUT/DELETE）；`POST` 返回 `NoOpAsync` —— 避免「服务端已成功但响应丢失」时重试重复创建 |
| 新增包 | `Microsoft.Extensions.DependencyInjection`、`Microsoft.Extensions.Http.Polly`（版本沿用中央声明） |
| 清理 | 删除死分支「`IsRegistered<IHttpClientFactory>()` 恒 false」与失真注释；删除被取代的 `LocalApiHttpClientFactory`（单一实现原则） |
| 验证 | `LocalModeTransportTests`（3）走真实新组合通过；`RefitApiClient` 远程路径编译并接线完成 |

### 6. L6 主题 Token / 样式提取 / i18n ⚠️ 部分实施（有据未做项）

| 子项 | 结果 |
|---|---|
| 死资源清理 | ✅ 删 `Spacing.xaml`（7 键 0 引用）+ App 合并项；删 `Icons.xaml` 5 个未用几何；删 `Converters.xaml`（9/10 键无引用）并把唯一使用点改走 `Cvt` 静态门面 |
| 样式提取 | ✅ 新增 `Themes/ValidationStyles.xaml`（5 份重复的校验样式）与 `Themes/TcmTags.xaml`（`HerbTag*` 2 份 + `CompactSectionBorderStyle` 2 份），5+2+2 处本地定义删除 |
| 主题 Token 化 | ⚠️ **按实测修正**：设计文档给的 3 组映射**全部不等值**——`#FBF7F3`(251,247,243) ≠ `SurfaceLevel0Brush`(#FAF8F5)；`MaterialDesign.Brush.Outline` **在 MDIX 5.3.2 中不存在**；`Foreground="Black"` 已不存在于 `HerbItemControl`。据「不合理不替换」原则保留原字面量并报告 |
| Token 化（本轮实际执行） | ✅ **30 处页面底色字面量 `#FBF7F3` → `{DynamicResource SurfaceLevel0Brush}`**（29 个 `Background=` + 1 个 Setter）。理由：`ThemeService.ApplyCustomPalette` 会切换 `SurfaceLevel0Color = SurfaceLevel0ColorDark`，故该 token 主题可用而字面量不可用——这是「深色模式大面积失效」的根因。亮色下 ΔRGB = 1/1/2（视觉不可辨），已在下方「需确认」登记 |
| 可访问性 | ✅ `AutomationProperties` 由 1 处增至 **55 处**（5 个编辑控件的输入 + 工具栏/分页图标按钮） |
| i18n | ❌ **未做**（设计文档已列为高风险）：`{x:Static}` 缺 key 只返回空串不抛异常，必须先补 `.resx` 再替换，且本环境无法做视觉验收。已登记为独立批次 |

---

## 二、验证与证据

| 门禁 | 结果 |
|---|---|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告** |
| `tests/LYBT.Tests.Architecture/` | **99/99 pass** |
| `tests/LYBT.Tests.Server/` | **828/828 pass**（期间修复 1 处由上一批次 `[AllowAnonymous]` 变更引起的 Swagger 安全声明守卫：`/auth/validate` 按守卫要求显式登记为匿名端点） |
| `tests/LYBT.Tests.Desktop/` 受影响套件 | RegistrationFlow / MedicalCaseFlow / ShellFlow / LocalApi / Unit.Http / Unit.Repositories / HerbFormulaFlow = **71 项**；LocalModeTransport + HttpApiClientBase + ExceptionMapping + DesktopExceptionHandler = **24 项**；DoctorLocalTests = **7 项** —— 全绿 |
| `tests/LYBT.Tests.Desktop/` 全量 | 见 §三 |

### 实施中发现并修复的缺陷（本轮引入或暴露）

| 缺陷 | 根因 | 修复 |
|---|---|---|
| 12 处 `ct` 落入 `object? body` → `NotSupportedException: System.IntPtr` | `Post/PutAndWrapAsync(url, body=null, ct=default)` 的 `body` 是 `object?`，`ct` 误传为第 2 实参不报编译错 | 改具名实参 `ct: ct`；全仓脚本复扫剩余 0 |
| 20 个 DIM 转发不可有可选参数（CS1066） | C# 语法限制 | DIM 实现去掉默认值，声明侧保留 |
| `DoctorLocalTests` 断言 200 而 Local 已按契约返回 201 | 上一批次契约对齐的既有断言滞后 | 断言更新为 201（与 Remote `CreatedAtAction` 一致） |
| Swagger 安全声明守卫红 | 上一批次 `/auth/validate` 改匿名 | 按守卫要求显式登记匿名端点（守卫设计如此：「新增匿名端点须显式登记——安全评审触发点」） |

---

## 三、需确认 / 未执行（明确登记，非静默跳过）

| 项 | 说明 |
|---|---|
| **30 处底色 token 化的 1/1/2 色差** | 收敛到既有 `SurfaceLevel0Brush` 使深色模式生效，亮色下 `#FBF7F3 → #FAF8F5`（ΔRGB 1/1/2，视觉不可辨）。若设计上要求像素级保持，替代方案是新增 `#FBF7F3` 主题 token 并纳入 `ThemeService.ThemeColorKeys`——**需设计确认** |
| `#E9DFD7` 描边、`#5B8FA8` 文字 | 仓内**无等值** token，且 `MaterialDesign.Brush.Outline` 经实测不存在 → 保留字面量 |
| `White` 背景 10 处（含 4 个打印模板） | 其中打印模板**必须**恒白（纸张语义），不可 token 化；其余需逐点判定语义，未在本批次处理 |
| i18n（`StringResources` 接入） | 高风险（缺 key 静默空白）+ 无法视觉验收 → 独立批次，先补 key 再替换 |
| 打印模板样式合并、`Functions` 样式去重 | 风险高收益低，未做 |
| 模块仓储 `catch (Exception)` 吞 `OperationCanceledException` | ct 已透传到传输层（请求确实会中止），但这些方法会把取消异常降级为 null/failed 而非向上冒泡。属**行为契约变更**，需独立决策 |
| 服务端 `RemoveByPrefix` 反射实现 | 桌面已切到 `DesktopCacheKeyRegistry`；`Shared` 版本仍被 Server `CacheInvalidationService` 使用，未纳入本批次 |
| `AddHttpClient` 私有容器与主容器生命周期 | `ServiceProvider` 由 Shell 持有，未在退出路径显式释放（进程退出即回收）；如需严格释放需接入 Shell 关闭协调器 |
| 跨聚合失败窗口的失败注入测试 | 补偿路径已实现但**未加失败注入测试**（需构造医案写入失败的场景）；当前以 BR-001 约束 + E2E 覆盖间接验证 |
