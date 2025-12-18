# Design: consolidate-medicalcase-queries

## Context

### 背景
LYBTZYZS系统采用DDD架构，MedicalCase作为聚合根，包含Consultation(诊断)和Prescription(处方)两个子实体。当前存在跨医案查询实现违反聚合根设计原则的问题。

### 约束
1. 必须遵循DDD聚合根设计模式
2. 不删除Consultation/Prescription相关的4个项目结构
3. 保持向后兼容，渐进式清理

### 干系人
- 开发团队: 代码变更影响
- 测试团队: 需要回归测试

## Goals / Non-Goals

### Goals
- 将跨医案查询统一到MedicalCase聚合根
- 清理死代码(0调用的API端点)
- 符合DDD最佳实践

### Non-Goals
- 不重构MedicalCase内部实现
- 不修改数据库Schema
- 不删除项目结构

## Decisions

### Decision 1: 查询路径设计
**决策**: 从MedicalCase聚合根出发查询，返回完整聚合数据

**理由**:
- 符合DDD"从聚合根访问子实体"原则
- MedicalCaseDetailDto已包含嵌套的Consultation和Prescription
- 减少API调用次数，一次请求获取完整数据

**替代方案**:
1. ~~保持从Prescription查询~~ - 违反DDD原则
2. ~~创建独立的CrossMedicalCaseQueryService~~ - 增加复杂度

### Decision 2: API端点设计
**决策**: 在MedicalCaseController添加两个新端点

```csharp
// 跨医案搜索
[HttpGet("search")]
public async Task<IActionResult> SearchMedicalCases(
    [FromQuery] string? patientName = null,
    [FromQuery] string? diagnosisKeyword = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)

// 患者最近医案
[HttpGet("patient/{patientId}/recent")]
public async Task<IActionResult> GetPatientRecentMedicalCases(
    Guid patientId,
    [FromQuery] int count = 5)
```

**理由**:
- RESTful风格，与现有API一致
- 参数灵活，支持多种查询场景
- 返回PagedResult支持分页

### Decision 3: 返回DTO设计
**决策**: 统一返回MedicalCaseDetailDto

```csharp
public class MedicalCaseDetailDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? Diagnosis { get; set; }
    // ... 其他字段

    // 嵌套子实体
    public ConsultationDetailDto? Consultation { get; set; }
    public PrescriptionDetailDto? Prescription { get; set; }
}
```

**理由**:
- 现有DTO已支持完整聚合结构
- 减少DTO类型数量
- 客户端可按需使用嵌套数据

### Decision 4: EF Core查询优化
**决策**: 使用Include预加载避免N+1问题

```csharp
var query = _context.MedicalCases
    .Include(mc => mc.Consultation)
    .Include(mc => mc.Prescription)
        .ThenInclude(p => p.Items)
    .AsNoTracking();
```

**理由**:
- 单次查询获取完整数据
- AsNoTracking提升只读查询性能
- 避免懒加载带来的N+1问题

### Decision 5: 清理策略
**决策**: 删除代码但保留项目结构

| 组件 | 动作 | 理由 |
|------|------|------|
| ConsultationController | 删除文件 | 2个方法，0次调用 |
| PrescriptionsController | 删除文件 | 4个方法，仅1次调用（迁移后删除） |
| IConsultationApi | 删除文件 | 3个方法，0次调用 |
| IPrescriptionApi | 删除文件 | 4个方法，仅1次调用（迁移后删除） |
| PrescriptionService.SearchPrescriptionsAsync | 删除方法 | 迁移到MedicalCaseQueryService |
| PrescriptionService.GetPatientRecentPrescriptionsAsync | 删除方法 | 迁移到MedicalCaseQueryService |
| PrescriptionService.LoadMedicalCasesAsync | 删除方法 | 级联删除（服务于被删除的方法） |
| MedicalCaseBasicDto | 删除文件 | 级联删除（唯一调用者被删除） |
| ICrossModuleQueryService MedicalCase方法 | 删除方法 | 级联删除（MedicalCaseBasicDto被删除） |
| LYBT.Desktop.Consultation 项目 | **保留** | 未来扩展 |
| LYBT.Desktop.Prescriptions 项目 | **保留** | 未来扩展 |
| LYBT.Module.Consultation 项目 | **保留** | 未来扩展 |
| LYBT.Module.Prescriptions 项目 | **保留** | 未来扩展 |

**理由**:
- 死代码删除，减少维护负担
- 项目结构保留，便于未来扩展
- 渐进式清理，降低风险

### Decision 6: MedicalCase DTO结构分析
**决策**: 保持现有Input DTO结构，仅删除MedicalCaseBasicDto

**分析结论**:
经系统分析，MedicalCase相关DTOs设计合理，职责清晰：

| DTO | 用途 | 结论 |
|-----|------|------|
| MedicalCaseInputDto | 医案实体创建/更新统一入口 | **保留** - Epic #1961统一设计 |
| MedicalCaseCreateInputDto | 聚合根创建（包装Input + 嵌套子实体） | **保留** - 聚合创建需要 |
| MedicalCaseAggregateInputDto | 聚合根更新（带审计 + 嵌套子实体） | **保留** - Master-Detail保存需要 |
| MedicalCaseListDto | Desktop列表显示 | **保留** - 列表视图需要 |
| MedicalCaseDetailDto | 完整聚合详情 | **保留** - 详情视图需要 |
| MedicalCaseBasicDto | 跨模块基本信息查询 | **删除** - 唯一调用者被删除 |

**理由**:
- Input DTOs各有明确职责，不存在重复
- MedicalCaseBasicDto是为PrescriptionService设计的轻量级DTO，随其调用者删除而成为死代码
- 保持DTO结构稳定，避免不必要的破坏性变更

## Risks / Trade-offs

### Risk 1: Desktop客户端兼容性
**风险**: PrescriptionEditorService迁移可能影响功能
**缓解**: 充分测试处方编辑器的历史处方加载功能

### Risk 2: 返回数据量增大
**风险**: MedicalCaseDetailDto比PrescriptionSearchResultDto数据更多
**缓解**: 可考虑添加`includeDetails`参数控制嵌套数据返回

### Trade-off: 查询灵活性 vs 复杂度
**选择**: 优先简单实现，后续按需扩展
**理由**: 当前业务场景明确，避免过度设计

## Migration Plan

### Step 1: 新增能力 (Phase 1-2)
- 添加MedicalCaseController新端点
- 添加IMedicalCaseApi新方法
- 不删除任何现有代码

### Step 2: 客户端迁移 (Phase 2)
- PrescriptionEditorService改用IMedicalCaseApi
- 验证功能正常

### Step 3: 清理旧代码 (Phase 3-4)
- 删除无引用的Controller/API方法
- 清理Service层死代码

### Step 4: DTO级联清理 (Phase 6)
- 删除MedicalCaseBasicDto
- 清理ICrossModuleQueryService MedicalCase相关方法
- 删除PrescriptionService.LoadMedicalCasesAsync

### Rollback Plan
- 每个Phase独立提交
- 如有问题可回滚到上一Phase
- 保留项目结构便于恢复

### Decision 7: MedicalCase DTO统一为聚合模式
**决策**: 统一Input/Output DTO为聚合模式，修复API契约不一致

**业务规则确认**:
- 诊断（Consultation）：**必填**
- 处方（Prescription）：**可选**
- 系统不容许没有诊断的医案被保存

**问题分析**:
当前设计违反业务规则：先创建"空壳医案"再逐步填充，但空壳医案本身是非法状态。

**7.1 Input DTO统一**:

| 当前DTO | 问题 | 决策 |
|---------|------|------|
| MedicalCaseInputDto | 16字段仅用5个，11个死字段 | **删除** |
| MedicalCaseCreateInputDto | Server端点不存在 | **删除** |
| MedicalCaseAggregateInputDto | 设计正确 | **重命名为MedicalCaseInputDto并扩展** |
| CreateMedicalCaseRequest (Controller内部类) | 与统一DTO重复 | **删除** |

**统一后的MedicalCaseInputDto**:
```csharp
public class MedicalCaseInputDto
{
    // 标识（null=创建，有值=更新）
    public Guid? Id { get; set; }

    // 创建时必填
    public Guid? PatientId { get; set; }
    public DateTime? VisitDate { get; set; }

    // 通用字段
    public string? Remark { get; set; }
    public string? EditReason { get; set; }

    // 诊断（业务必填）
    public ConsultationInputDto Consultation { get; set; } = new();

    // 处方（可选）
    public PrescriptionAggregateInputDto? Prescription { get; set; }
}
```

**7.2 Output DTO设计（CQRS读模型原则）**:

根据CQRS"每屏一投影"原则（Greg Young），读侧查询不需要遵循DDD聚合根模式，应针对UI需求优化：

| 当前DTO | 用途 | 决策 |
|---------|------|------|
| MedicalCaseListDto | 列表视图 | **保留** - 扁平化，含Diagnosis字段支持hover |
| MedicalCaseDetailDto | 详情/编辑 | **保留** - 聚合模式，嵌套Consultation/Prescription |
| PendingMedicalCaseDto | 待诊队列 | **保留**（特殊业务场景） |

**设计依据**:
- Microsoft eShopOnContainers: 列表用OrderSummary，详情用OrderDetail
- 写侧(Command)遵循聚合根模式，读侧(Query)针对UI优化
- 列表DTO轻量，网络传输小，与其他模块设计一致

**7.3 API契约统一**:

| 操作 | 当前Client | 当前Server | 统一后 |
|-----|-----------|-----------|--------|
| 创建医案 | POST + MedicalCaseInputDto | POST + CreateMedicalCaseRequest | POST + MedicalCaseInputDto (统一后) |
| 创建带详情 | POST /with-details | **不存在** | **删除Client方法** |
| 更新医案 | PUT /{id}/aggregate | PUT /{id}/aggregate | **保持** |
| 软删除 | DELETE /{id}/soft | **不存在** | **删除Client方法** |
| 删除处方 | DELETE /{id}/prescription | **不存在** | **删除Client方法** |

**理由**:
- 写侧：符合DDD聚合根设计，一次性提交完整聚合，不存在非法中间状态
- 读侧：符合CQRS原则，List/Detail分离，针对UI优化
- 统一Client与Server契约，消除运行时错误风险

## Open Questions

1. ~~是否需要保留PrescriptionSearchResultDto?~~ - 否，统一使用MedicalCaseDetailDto
2. ~~是否需要为搜索结果创建轻量级DTO?~~ - 暂不需要，后续按性能需求评估
3. ~~三个Input DTO是否可以合并为一个?~~ - 分析结论：MedicalCaseAggregateInputDto是主要路径，其他两个存在设计缺陷待清理
