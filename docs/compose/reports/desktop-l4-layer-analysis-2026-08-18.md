# Desktop L4 HTTP Client 层分析（2026-08-18）

## 架构总览

```
SwitchingApiClient (singleton IApiClient, URL 驱动路由)
  ├─ Remote 模式 → RefitApiClient
  │   └─ {Domain}ApiClient (Refit 适配器 × 10)
  │       └─ Refit 接口 → HttpClient → Handler 链
  │           Handler 链: HttpClientHandler → TokenRefreshHandler
  │           → AuthorizationMessageHandler → LoggingHttpHandler
  └─ Local 模式 → HttpClientApiClient
      └─ {Domain}HttpApiClient (HttpApiClientBase 派生 × 10)
          └─ IHttpClientFactory → HttpClient → LocalWebAPI Kestrel
```

## 核心文件清单

| 文件 | 行数 | 职责 |
|------|------|------|
| **HttpApiClientBase** | 210 | Local 模式基类：HttpClient + JSON + ApiResponse 信封 + 分页 URL 构建 |
| **RefitApiClient** | ~110 | Remote 模式：Refit 代理按领域惰性创建 |
| **HttpClientApiClient** | ~80 | Local 模式：HttpClient 按领域惰性创建 |
| **SwitchingApiClient** | 121 | URL 路由：IsLocal → HttpClientApiClient / 非本地 → RefitApiClient |
| **AuthorizationMessageHandler** | 74 | DelegatingHandler：自动注入 Bearer Token，跳过匿名端点 |
| **TokenRefreshHandler** | 535 | DelegatingHandler：Token 自动刷新 + 重试 + AutoLogin 降级 + 事件发布 |
| **{Domain}ApiClient** | ~50×10 | Remote 适配器：Refit 接口 → IApiClient 子接口 |
| **{Domain}HttpApiClient** | ~50×10 | Local 适配器：HttpApiClientBase → IApiClient 子接口 |

## 数据流

### Remote 模式
```
ViewModel → Repository → IApiClient.Xxx → SwitchingApiClient.Current
  → RefitApiClient.Xxx → {Domain}ApiClient
    → Refit 接口 → HttpClient → Handler 链 → 网络 → WebAPI
```

### Local 模式
```
ViewModel → Repository → IApiClient.Xxx → SwitchingApiClient.Current
  → HttpClientApiClient.Xxx → {Domain}HttpApiClient (继承 HttpApiClientBase)
    → IHttpClientFactory.CreateClient() → HttpClient → LocalWebAPI Kestrel
```

## Handler 链（Remote 模式独有）

```
HttpClientHandler (底层 TCP)
  └→ TokenRefreshHandler (过期检测 + 刷新 + 重试 + AutoLogin 降级)
      └→ AuthorizationMessageHandler (Bearer Token 注入 + 匿名端点跳过)
          └→ LoggingHttpHandler (请求/响应日志 + CorrelationId)
```

- TokenRefreshHandler 使用**独立 HttpClient** 调用 refresh 端点（避免循环依赖）
- SemaphoreSlim 保证并发刷新安全（单实例锁）
- Token 过期前 5 分钟触发刷新

## 当前状态评估

### ✅ 已实现（正常工作）
- SwitchingApiClient URL 路由 + client 创建/销毁 ✅
- RefitApiClient（Remote）惰性创建 + Refit 代理 ✅
- HttpClientApiClient（Local）惰性创建 + HttpApiClientBase ✅
- AuthorizationMessageHandler Token 注入 ✅
- TokenRefreshHandler 自动刷新 + 重试 + AutoLogin 降级 ✅
- Handler 链正确组装 ✅
- Singleton 注册模式（SwitchingApiClient + 子接口 transient）✅

### ⚠️ 设计观察（非阻断，TDD 可覆盖）
1. **Local 模式无 auth handler**：LocalWebAPI 使用 1 年固定 JWT，无 TokenRefreshHandler
2. **TokenRefreshHandler 的 _refreshHttpClient** 用 config 中的 BaseUrl（可能与运行时 URL 不一致）
3. **HttpClientApiClient** 使用 IHttpClientFactory，但 Remote 的 RefitApiClient 用独立 HttpClient 实例
4. **Refit 接口无版本化约束**：Refit 接口硬编码路径，Server 端点变更需同步修改

## TDD 测试范围（L4 层）

### 批次 1: HttpApiClientBase（基础 HTTP 操作）
- ✅ SendAsync 正确执行 GET/POST/PUT/DELETE
- ✅ EnsureSuccessOrThrowAsync 对非 2xx 抛 HttpRequestException
- ✅ DeserializeEnvelopeAsync 正确解包 ApiResponse<T>
- ✅ DeserializeEnvelopeAsync 兼容裸 T 格式
- ✅ BuildPagedUrl 正确构建分页 URL + 过滤参数
- ✅ ToJsonContent 生成正确 JSON 格式

### 批次 2: AuthorizationMessageHandler（Token 注入）
- ✅ 有 Token 时注入 Authorization header
- ✅ 无 Token 时不注入 header（仅 Warning 日志）
- ✅ 匿名端点（/health, /login, /refresh）跳过 Token 检查
- ✅ 非匿名端点正确注入 Token

### 批次 3: TokenRefreshHandler（自动刷新）
- ✅ Token 未过期时放行请求
- ✅ Token 即将过期（<5min）触发刷新
- ✅ 用户不活跃时跳过刷新（滑动过期）
- ✅ 并发请求不重复刷新（SemaphoreSlim）
- ✅ 刷新成功更新 Token 存储
- ✅ 刷新失败重试（3 次，指数退避）
- ✅ RefreshToken 不存在时返回失败
- ✅ AutoLogin 降级触发（RefreshToken 过期/撤销/无效）
- ✅ AutoLogin 降级失败时清除凭据
- ✅ Prism EventAggregator 事件发布

### 批次 4: SwitchingApiClient（路由）
- ✅ Local URL → HttpClientApiClient
- ✅ Remote URL → RefitApiClient
- ✅ URL 变更后创建新 client、销毁旧 client
- ✅ Thread-safe（双重检查锁定）
- ✅ Dispose 正确清理当前 client

### 批次 5: 集成验证
- ✅ Handler 链正确组装（TokenRefresh → Authorization → Logging）
- ✅ LocalWebAPI 连接 + 认证 + CRUD 端到端
- ✅ Remote WebAPI 连接 + 认证 + CRUD 端到端
- ✅ 模式切换后 client 正确重建
