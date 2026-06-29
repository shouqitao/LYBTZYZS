# Herbs 模块设计

> 日期: 2026-06-29
> US 数量: 13 (PRD)
> 复杂度: 中低
> 状态: 草稿

## 模块概述

Herbs 管理中药药材信息，支持拼音搜索、引用检查、批量导入/导出。

**职责边界**:
- 药材的 CRUD、搜索、批量操作
- 拼音首字母自动生成与搜索
- 引用检查（被处方/验方引用时提示）
- 批量 Excel 导入/导出

**依赖关系**:
- 上游: 无（基础模块）
- 下游: MedicalCase（处方价格计算）、Formula（验方组成）
- 无模块间直接引用

**关键 US 清单**:
HERB-001 ~ HERB-013（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```csharp
public interface IHerbService
{
    Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);
    Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<HerbDetailDto>> CreateAsync(HerbInputDto dto, CancellationToken ct);
    Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword, CancellationToken ct);
    Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy, CancellationToken ct);
    Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category, CancellationToken ct);
    Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken ct);
}
```

### 引用检查接口

```csharp
public interface IHerbReferenceRepository
{
    Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct);
    Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct);
    Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct);
}
```

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| GET | `/api/v1/herbs` | GetList | DoctorOrAdmin |
| GET | `/api/v1/herbs/{id}` | GetById | DoctorOrAdmin |
| POST | `/api/v1/herbs` | Create | DoctorOrAdmin |
| PUT | `/api/v1/herbs/{id}` | Update | DoctorOrAdmin |
| DELETE | `/api/v1/herbs/{id}` | Delete | DoctorOrAdmin |
| POST | `/api/v1/herbs/{id}/toggle-status` | ToggleStatus | DoctorOrAdmin |
| POST | `/api/v1/herbs/batch-delete` | BatchDelete | DoctorOrAdmin |
| POST | `/api/v1/herbs/batch-import` | BatchImport | DoctorOrAdmin |

### DTO 结构

```
HerbInputDto
├── Name: string（必填）
├── PinYinCode: string?（自动生成）
├── Category: string?（分类）
├── Properties: string?（性味）
├── Origin: string?（产地）
├── Spec: string?（规格）
├── Unit: string（必填，默认"克"）
├── Price: decimal（必填）
├── CostPrice: decimal?
├── Effect: string?（功效）
├── Usage: string?（用法用量）
├── Remark: string?
└── Id: Guid?（null=创建，有值=更新）

HerbDetailDto: 所有字段 + UpdatedAt, CreatedBy
HerbListDto: Id, Name, PinYinCode, Category, Origin, Spec, Unit, Price, Status, CreatedAt
```

## 数据流

### 创建药材
```
Desktop → POST /api/v1/herbs
Controller → HerbService.CreateAsync
  → FluentValidation 校验
  → 检查名称唯一性（ExistsByNameAsync）
  → 自动生成 PinYinCode
  → 创建 Herb 实体
  → 缓存失效
  → 返回 HerbDetailDto
```

### 引用检查
```
Desktop → DELETE /api/v1/herbs/{id}
  → 先调用 IHerbReferenceRepository 检查引用
  → 有引用 → 返回警告（HerbReferenceCheckDto）
  → 无引用 → 执行软删除
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| FluentValidationException | 输入校验失败 | 返回 400 |
| BusinessException | 名称重复 | 返回 400 |
| NotFoundException | GetById 找不到 | 返回 404 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 拼音自动生成 | Name 变更时自动更新 PinYinCode | HERB-002 |
| 名称唯一 | 创建/更新时校验 | HERB-003 |
| 引用检查 | 删除前检查是否被处方/验方引用 | HERB-004 |
| 批量导入 | 支持 Skip/Update/Error 三种重复策略 | HERB-005 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| MedicalCase | 查询药材价格（处方计算） | MedicalCase → Herbs |
| Formula | 查询药材信息（验方组成） | Formula → Herbs |

**已知问题**:
- `IHerbReferenceRepository` 已注册但 Service 未注入/未使用
- 删除/批量删除均不检查引用 → 被处方引用的药材可静默软删
- `IHerbImportExportService` 在 PRD/AGENTS/README 三处引用，仓库中无此文件
