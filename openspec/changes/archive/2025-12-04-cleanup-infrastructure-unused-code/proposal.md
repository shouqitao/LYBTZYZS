# OpenSpec Proposal: cleanup-infrastructure-unused-code

## Summary
清理Server端非业务模块（LYBT.Infrastructure、LYBT.Entities、LYBT.WebAPI）中的未使用代码，包括未调用的Repository接口方法、未使用的工具类、冗余的健康检查控制器等。

## Motivation
继上一次 `cleanup-unused-methods` 清理业务模块未用代码后，本次扫描发现Server端基础设施项目中同样存在未使用的代码：
- IRepository接口中定义了3个从未被调用的方法
- 存在多个定义但未被任何代码引用的工具类
- 健康检查端点存在冗余实现

清理这些代码可以：
1. 减少代码维护负担
2. 降低接口复杂度
3. 避免开发者误用已废弃的API
4. 提高代码库整洁度

## Detailed Design

### Phase 1: IRepository接口方法清理 (~30行)

| 方法 | 位置 | 调用次数 | 行动 |
|------|------|----------|------|
| `DeleteRangeAsync(IEnumerable<T>)` | IRepository.cs:101-102 | 0 | 删除 |
| `DeleteRangeAsync(IEnumerable<Guid>)` | IRepository.cs:108-109 | 0 | 删除 |
| `ExistsAsync(Guid)` | IRepository.cs:117-118 | 0 | 删除 |
| `GetSingleAsync(Expression<...>)` | IRepository.cs:64-65 | 0 (仅BaseRepository内部) | 保留* |

*注：`GetSingleAsync` 在 IReadRepository 中也有定义且被 BaseReadRepository 实现，考虑保留以保持接口完整性。

**BaseRepository对应实现删除**:
- `DeleteRangeAsync(Expression<...>)` - 行461-489 (~28行)
- `DeleteRangeAsync(IEnumerable<T>)` - 行492-527 (~35行)
- `DeleteRangeAsync(IEnumerable<Guid>)` - 行530-576 (~46行)
- `ExistsAsync(Guid)` - 行302-311 (~10行)

### Phase 2: 未使用工具类清理 (~330行)

| 类/文件 | 位置 | 调用次数 | 行动 |
|---------|------|----------|------|
| ~~`SensitiveDataAttribute`~~ | ~~LYBT.Entities/Attributes/SensitiveDataAttribute.cs~~ | PatientModel使用 | **保留** (安全合规预留，见Issue #2254) |
| `PaginatedList<T>` | LYBT.Infrastructure/Data/PaginatedList.cs | 0 | 删除整个文件 |
| `BaseSystemController` | LYBT.Infrastructure/Web/BaseSystemController.cs | 0 | 删除整个文件 |
| `BusinessRuleValidator` | LYBT.Infrastructure/Validation/BusinessRuleValidator.cs | 0 | 删除整个文件 (~388行) |

### Phase 3: 冗余健康检查控制器清理 (~50行)

| 组件 | 位置 | 问题 | 行动 |
|------|------|------|------|
| `RootHealthController` | LYBT.WebAPI/Controllers/RootHealthController.cs | 与MapHealthChecks路由冲突 | 删除整个文件 |

**冲突分析**:
- `RootHealthController` 提供 `/health` 和 `/health/ping` 端点
- `UnifiedMiddlewareConfiguration.cs:91-95` 已通过 `MapHealthChecks("/health")` 注册ASP.NET Core内置健康检查
- 两者在 `/health` 路由上冲突，内置HealthChecks功能更完善

**前端调用验证** (2025-12-04):
- **路由优先级**: `MapHealthChecks("/health")` 在 `MapControllers()` 之前注册，因此 `/health` 请求被内置HealthChecks拦截，`RootHealthController.HealthCheck()` 永远不会被调用
- **前端使用情况**:
  - `ApiHealthCheckService.cs` 调用 `{baseUrl}/health` 端点
  - 该请求由 `MapHealthChecks` 处理，非 `RootHealthController`
- **`/health/ping`**: 前端代码中零调用，完全未使用
- **结论**: `RootHealthController` 100%冗余，可安全删除

### Phase 4: 文档更新

更新以下文档以反映变更：
- `docs/explanation/architecture/` 相关架构文档
- 移除对已删除类/方法的引用

## Breaking Changes
- `IRepository<T>` 接口将移除 `DeleteRangeAsync` 和 `ExistsAsync(Guid)` 方法
- 任何直接引用这些方法的外部代码将无法编译（经验证项目内无调用）

## Alternatives Considered
1. **标记为[Obsolete]而非删除**: 考虑后拒绝，因为这些方法从未被使用过，没有兼容性需求
2. **保留用于未来**: 考虑后拒绝，YAGNI原则 - 需要时再添加

## Implementation Plan
- Phase 1: 清理IRepository接口和BaseRepository实现 (~120行)
- Phase 2: 删除未使用工具类文件 (~400行)
- Phase 3: 删除冗余健康检查控制器 (~50行)
- Phase 4: 文档同步

**实际删除代码量**: ~500行 (SensitiveDataAttribute保留)

## Validation Checklist
- [x] 编译通过 (dotnet build) - 2025-12-04
- [x] 单元测试通过 (dotnet test) - 2025-12-04
- [x] 健康检查端点仍可访问 (/health) - MapHealthChecks处理
- [x] API功能正常 - 编译测试验证

## Execution Summary (2025-12-04)

| 删除项 | 文件/方法 | 行数 |
|--------|-----------|------|
| RootHealthController.cs | 路由冲突冗余控制器 | ~48行 |
| IRepository方法 | DeleteRangeAsync x2, ExistsAsync | ~18行 |
| BaseRepository实现 | 对应3个方法实现 | ~80行 |
| PaginatedList.cs | 未使用工具类 | ~50行 |
| BaseSystemController.cs | 未使用基类 | ~30行 |
| BusinessRuleValidator.cs | 未使用验证器 | ~388行 |
| **总计** | | **~614行** |

**保留项**: SensitiveDataAttribute.cs - 安全合规预留，后续实现见 Issue #2254

## Appendix: 组件保留说明

以下组件经分析确认**必要保留** (前端调用验证: 2025-12-04):

### ClaimsNormalizationMiddleware
- **位置**: LYBT.WebAPI/Middleware/ClaimsNormalizationMiddleware.cs
- **作用**: 在认证后、授权前标准化JWT Claims格式，统一不同提供商的Claims命名差异
- **使用**: UnifiedMiddlewareConfiguration.cs:69 `app.UseClaimsNormalization()`
- **前端调用**: 所有需要认证的API请求间接使用（约50+个Refit接口方法）
- **必要性**: 高 - 确保授权逻辑稳定工作，无此中间件将导致Claims解析不一致

### MedicalCasePermissionMiddleware
- **位置**: LYBT.WebAPI/Middleware/MedicalCasePermissionMiddleware.cs
- **作用**: 为MedicalCase端点提供统一权限验证，支持Epic #1612
- **使用**: UnifiedMiddlewareConfiguration.cs:73 `app.UseMedicalCasePermission()`
- **前端调用**:
  - `IMedicalCaseApi.cs` 中所有PUT/PATCH/DELETE方法触发此中间件
  - 包括: `UpdateConsultationAsync`, `DeleteMedicalCaseAsync`, `SetPrescriptionFlagAsync`, `SaveDraftAsync`, `CancelMedicalCaseAsync`, `UpdateStatusAsync`, `CloseCaseAsync` 等
- **必要性**: 高 - 核心业务模块权限控制，支持"当天可改"业务规则

### SecurityHeadersMiddleware
- **位置**: LYBT.WebAPI/Middleware/SecurityHeadersMiddleware.cs
- **作用**: 添加安全响应头 (CSP, X-Frame-Options等)
- **使用**: UnifiedMiddlewareConfiguration.cs:41 `app.UseSecurityHeaders()`
- **前端调用**: 影响所有HTTP响应，前端无需显式调用
- **必要性**: 高 - 安全合规要求，防止XSS、Clickjacking等攻击

### SensitiveDataAttribute
- **位置**: LYBT.Entities/Attributes/SensitiveDataAttribute.cs
- **作用**: 标记敏感数据字段（身份证、手机号、地址、病史等），用于日志脱敏和数据保护
- **使用**: PatientModel.cs 中5个字段已标注
- **前端调用**: 无直接调用（声明性Attribute）
- **必要性**: 高 - 安全合规预留，处理逻辑待实现 (Issue #2254)
- **数据类型**: PersonalInfo, MedicalInfo, ContactInfo, IdentityInfo, FinancialInfo
- **脱敏模式**: Default, Partial, Full, Hash
