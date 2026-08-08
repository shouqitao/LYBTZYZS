# WebApi 架构深度分析报告

> 分析日期: 2026-08-07 | 分析者: 技术总监（Hermes Agent）
> 基于代码实际状态（非文档），覆盖全部 Server 端代码。

---

## 一、代码体量概览

| 层 | 项目/目录 | 总行数 | 最大文件 |
|----|----------|--------|---------|
| Entry | WebAPI/Program.cs | 335 | 335 |
| Controllers | WebAPI/Controllers + Module Controllers | 2,895 | MedicalCasesController (356) |
| Services | Module Services + Handlers | 4,097 | MedicalCaseQueryService (565) |
| Repositories | Infrastructure/Repositories + Module Repos | ~800 | BaseRepository (137) |
| Middleware | 3个自定义 + 异常处理链 | ~400 | — |
| Shared.Models | DTO/Contract/Primitives | **20,115** | — |
| MedicalCase 模块总计 | 含 Controllers/Services/Interfaces/Mappers | **4,658** | — |

**关键发现**：MedicalCase 模块占全部 Server 模块代码的 **33%**（4,658 / 14,081），是第二名 Users（1,761行）的 **2.6 倍**。

### 模块行数对比

| 模块 | 行数 | 占比 |
|------|------|------|
| MedicalCase | 4,658 | 33% |
| Users | 1,761 | 12% |
| Registration | 1,507 | 11% |
| Auth | 1,604 | 11% |
| Herbs | 1,486 | 11% |
| Formula | 1,227 | 9% |
| Patients | 1,175 | 8% |
| Reports | 663 | 5% |

---

## 二、架构模式分析

### ✅ 做得好的

| 模式 | 评价 |
|------|------|
| **3-Layer 分层** | Controller→Service→Repository→DbContext 边界清晰，架构测试 P07/P08/P10 强制执行 |
| **模块化注册** | 每个 Module 有独立 `{Domain}Module.cs`，DI 注册自包含 |
| **MediatR + Service 混合** | 查询走 Service 绕过管道（性能），命令走 MediatR（验证+审计），设计合理 |
| **BaseCrudController 模板方法** | 5 个 virtual 方法 + 批量操作模板，CRUD Controller 代码量大幅减少 |
| **异常处理器链** | BusinessExceptionHandler → SystemExceptionHandler 链式处理，职责分离 |
| **中间件管道** | 6 阶段有序注册，UnifiedMiddlewareConfiguration 统一管理 |
| **配置三层验证** | DataAnnotations + IValidateOptions + ValidateOnStart，启动快速失败 |

### ⚠️ 需要优化的

---

## 三、优化建议（按优先级排序）

### P0 — 架构风险（影响可维护性）

#### O-01: MedicalCase 模块过重（4,658行）

MedicalCase 虽已拆分为 Command/Query/State 三 Service，但总行数仍是第二名的 2.6 倍。子文件：
- MedicalCaseQueryService: 565行（接近 600 行拆分阈值）
- MedicalCaseCommandService: 487行 + 130行（.Deletion partial）
- MedicalCaseStateService: 362行
- MedicalCasePrescriptionService: 335行
- PrescriptionItemService: 220行
- MedicalCaseServiceHelper: 198行

**风险**：新开发者理解成本高，修改一个 Service 可能影响其他 5 个。

**建议**：
- 评估 MedicalCaseQueryService 是否可进一步拆分为「列表查询」和「详情/聚合查询」两个子服务
- MedicalCasePrescriptionService（335行）+ PrescriptionItemService（220行）= 555行，可考虑合并为一个 PrescriptionService（职责更内聚）

**预估**：中等复杂度，需确保拆分后接口不变（Desktop Refit 硬约束）

---

#### O-02: TryDeserializeDto 反序列化验证模式重复（14+ 处）

当前每个 Controller 的 Create/Update 方法都重复：
```csharp
if (!dto.TryDeserializeDto(out XInputDto? inputDto, out var validationError))
    return ValidationFail(validationError);
```

这个模式在 6 个 Controller × 2 方法 = 12+ 处重复。

**根因**：`[FromBody] object dto` + 手动反序列化是为了绕过 FluentValidation 不被 DataAnnotations 拦截的问题（B-21 修复引入）。

**建议**：将 TryDeserializeDto 提取为 Controller 基类的泛型辅助方法，或改用 `[FromBody] TInputDto` 直接绑定 + FluentValidation pipeline 处理验证（ValidationBehavior 已经在 MediatR 路径生效）。

**预估**：低风险，纯重构，减少 ~100 行重复代码

---

#### O-03: Shared.Models 过大（20,115行）

所有 DTO、Contract、枚举、Primitives 都在 Shared.Models，随着模块增多会持续膨胀。

**建议**：短期可接受（v1.0），v2.0 考虑按领域拆分（Shared.Models.Auth / Shared.Models.MedicalCase / ...）。

**预估**：低优先级，v2.0 规划

---

### P1 — 性能优化

#### O-04: 查询端点缺少 AsNoTracking

Server 端 Repository 层的查询方法未统一使用 `AsNoTracking()`。对于只读查询（列表、详情、报表），`AsNoTracking` 可减少 EF Core 变更追踪开销，提升 20-40% 查询性能。

**建议**：在 BaseRepository 的查询方法中默认添加 `AsNoTracking()`，或在各 Module Repository 的查询方法中显式标注。

**预估**：低风险，高收益（查询性能提升明显）

---

#### O-05: MedicalCase 查询缺少 SplitQuery

MedicalCase 的 `Include` 调用有 32 处，复杂聚合查询（医案+辨证+处方+药材）可能触发笛卡尔积。

**建议**：对超过 3 个 `Include` 的查询添加 `.AsSplitQuery()`，避免内存暴涨。

**预估**：低风险，需验证 SQL Server 行为

---

#### O-06: 缓存策略可加强

当前 OutputCache 仅应用于 4 个 GET 列表端点（Patients/Herbs/Formulas/MedicalCases），MemoryCache 使用 13 处但集中在 Reports 模块。

**建议**：
- 为高频查询端点（患者详情、药材详情、验方详情）添加 OutputCache
- 为报表聚合结果添加 MemoryCache（当日统计数据 5 分钟 TTL）

**预估**：低风险，中等收益

---

### P2 — 代码质量

#### O-07: Program.cs 过长（335行）

Program.cs 承载了：热更新检查、Serilog 两阶段初始化、.env 加载、配置注册、Identity 注册、服务注册、SignalR 注册、配置验证、Kestrel 限制、应用初始化、中间件配置。

**建议**：将热更新逻辑（42-64行）、密码验证逻辑（279-332行）提取为独立扩展方法或 Startup Helper 类。

**预估**：低风险，纯重构

---

#### O-08: 医案模块 Controller 继承层级过深

```
BaseApiController → BaseCrudController → BaseMedicalCasesController → MedicalCasesController
```

4 层继承。MedicalCasesController（356行）override 了 BaseMedicalCasesController 的大部分方法。

**建议**：评估 BaseMedicalCasesController（254行）是否可以简化为 MedicalCasesController 的一部分（减少一层继承），但需注意 Desktop Refit 路由约束。

**预估**：中等风险，需谨慎评估

---

#### O-09: 日志粒度可优化

Server 端有 239 处日志调用。部分 Service 层的 Warning 级别日志可能在生产环境造成噪音（如 SignalR 推送失败的 Warning 实际是正常降级）。

**建议**：审计日志级别，将非关键路径的 Warning 降为 Debug（如 SignalR 推送失败、缓存未命中）。

**预估**：低风险

---

### P3 — 安全加固

#### O-10: CORS 生产环境可收紧

当前 CORS 允许所有配置的 origin（通过 appsettings），生产环境应仅允许已知 Desktop 客户端的 origin（但 WPF 非浏览器，CORS 主要影响 Swagger）。

**建议**：生产环境禁用 Swagger（已有 `IsProduction()` 判断），CORS 保持当前配置即可（WPF 客户端不受 CORS 约束）。

**预估**：无需改动，仅确认

---

#### O-11: Kestrel 限制可细化

当前仅设置 `MaxRequestBodySize = 10MB`。建议同时配置：
- `MaxConcurrentConnections`（防止连接耗尽）
- `KeepAliveTimeout`（合理超时）
- `RequestHeadersTimeout`

**建议**：在 Program.cs 的 Kestrel 配置中补充。

**预估**：低风险

---

## 四、不建议优化的（维持现状）

| 项目 | 理由 |
|------|------|
| 删除 MediatR | 混合模式已验证合理，MediatR 在命令路径提供验证+审计 |
| 拆分 MedicalCasesController | A-15 评估已取消，状态流转仅 4 方法，ROI 不合理 |
| 统一 Controller 继承 | 三种路径各有设计意图（A-14 已文档化） |
| 引入 CQRS 框程 | 当前 MediatR 已满足需求，引入额外框架增加复杂度 |

---

## 五、总结

| 优先级 | 编号 | 建议 | 预估工时 | 收益 |
|--------|------|------|---------|------|
| P0 | O-01 | MedicalCase 模块拆分评估 | 1-2d | 可维护性 ↑↑ |
| P0 | O-02 | TryDeserializeDto 模式去重 | 0.5d | 代码质量 ↑ |
| P1 | O-04 | 查询 AsNoTracking | 0.5d | 查询性能 ↑↑ |
| P1 | O-05 | SplitQuery 笛卡尔积防护 | 0.5d | 内存安全 ↑ |
| P1 | O-06 | 缓存策略加强 | 1d | 响应速度 ↑ |
| P2 | O-07 | Program.cs 瘦身 | 0.5d | 可读性 ↑ |
| P2 | O-08 | Controller 继承层级评估 | 0.5d | 可维护性 ↑ |
| P2 | O-09 | 日志级别优化 | 0.5d | 生产噪音 ↓ |
| P3 | O-11 | Kestrel 限制细化 | 0.25d | 安全性 ↑ |

**整体评价**：架构质量 **良好**（8/10）。分层清晰、模块化到位、异常处理规范。主要优化空间在 MedicalCase 模块的体量控制和查询性能微调。不需要架构级重构。

---

*报告版本: v1.0 | 保存日期: 2026-08-07*
