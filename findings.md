# Findings: Phase 2 - Desktop Architecture Optimization

## Architecture Issue

Desktop Contracts/Infrastructure 直接依赖 LYBT.Entities (Server Core 项目)。
DataSource 接口 `IDataSourceBase<TEntity>` 使用 EF 实体类型作为泛型参数。

## Current State Analysis

### DataSource 接口 Entity 依赖 (已修复)

| Interface | 旧类型 | 新类型 | 状态 |
|-----------|--------|--------|------|
| IDataSourceBase | `<TEntity>` | `<TDetail, TInput>` | done |
| IHerbDataSource | `Herb` | `HerbDetailDto, HerbInputDto` | done |
| IPatientDataSource | `Patient` | `PatientDetailDto, PatientInputDto` | done |
| IFormulaDataSource | `Formula` | `FormulaDetailDto, FormulaInputDto` | done |
| IMedicalCaseDataSource | `MedicalCase` | `MedicalCaseDetailDto, MedicalCaseInputDto` | done |
| IUserDataSource | `User` | `UserDetailDto, UserInputDto` | done |
| ILocalAuthService | `User?` | `UserDetailDto?` | done |

### Remote DataSource 架构变更

**旧路径**: API -> DetailDto -> mapper.ToEntity() -> Entity -> mapper.ToDetailDto() -> DetailDto
**新路径**: API -> DetailDto -> 直接返回

列表端点处理:
- API 返回 `XxxListDto`
- Remote DataSource 内部用 `XxxListToDetailMapper` (Mapperly) 转为 `XxxDetailDto`
- 未填充的 DetailDto 字段保持默认值 (null/0)
- Repository/ViewModel 只使用 ListDto 兼容的字段子集

### Local DataSource 架构变更

**边界转换模式**: EF Entity 在内部使用, 通过 `LocalXxxMapper` 在 DataSource 边界转换
- 入口: `_mapper.ToEntity(inputDto)` -> EF 操作
- 出口: EF 查询 Entity -> `_mapper.ToDetailDto(entity)` -> 返回 DTO

### API Response 类型映射

| API 端点 | 返回类型 | DataSource 返回 |
|---------|---------|----------------|
| GetXxxByIdAsync | `DetailDto` | 直接透传 |
| GetXxxsAsync (paged) | `PagedResult<ListDto>` | ListDto->DetailDto mapper |
| CreateXxxAsync | `DetailDto` | 直接透传 |
| UpdateXxxAsync | `DetailDto` | 直接透传 |
| RestoreAsync | `DetailDto` | 直接透传 |
| CloneAsync | `DetailDto` | 直接透传 |

### 项目依赖变更

| 项目 | LYBT.Entities 引用 | 状态 |
|-----|-------------------|------|
| LYBT.Desktop.Contracts | 已移除 | done |
| LYBT.Desktop.Infrastructure | 待移除 (Task 6) | pending |
| LYBT.Desktop.LocalData | 保留 (需要 EF Entity) | by design |

### 关键发现

1. **ListDto vs DetailDto**: Paged API 返回 ListDto (轻量), 但接口返回 DetailDto。通过 Mapperly mapper 桥接, DetailDto 中 ListDto 不包含的字段为默认值。
2. **FormulaHerbItem 无 UnitPrice**: Entity 没有此字段, TotalPrice 计算改为由 Service 层负责。
3. **MedicalCase.SaveAsync 签名变更**: 从 `SaveAsync(MedicalCase)` 改为 `SaveAsync(MedicalCaseInputDto)`, Local 实现中需从 InputDto 构建/更新聚合实体。

### 待处理: Repository 层编译错误 (27 个)

错误分布:
- HerbRepository: ~4 errors (Entity->DTO 类型不匹配)
- PatientRepository: ~4 errors
- FormulaRepository: ~4 errors
- MedicalCaseRepository: ~7 errors (最复杂, 包含聚合保存)
- UserRepository: ~8 errors
