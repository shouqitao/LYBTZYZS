# Formulas 模块设计

> 日期: 2026-06-29
> US 数量: 13 (PRD)
> 复杂度: 3/5
> 状态: 草稿

## 模块概述

Formulas 管理验方（经典方/经验方），支持延迟绑定（导入时药材名未匹配药典→手动绑定）。

**职责边界**:
- 验方的 CRUD、搜索、批量操作
- 延迟绑定工作流（Draft → Validated）
- Excel 导入/导出
- 与 MedicalCase 集成（导入验方到处方）

**依赖关系**:
- 上游: Herbs（药材信息查询）
- 下游: MedicalCase（导入验方到处方）
- 无模块间直接引用

**关键 US 清单**:
FORMULA-001 ~ FORMULA-013（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```csharp
public interface IFormulaService
{
    Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? currentUserId, bool isAdmin);
    Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id);
    Task<Result<FormulaDetailDto>> CreateAsync(FormulaInputDto dto, Guid? creatorId);
    Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto);
    Task<Result> DeleteAsync(Guid id);
    Task<Result<List<FormulaDetailDto>>> SearchAsync(string keyword);
    Task<Result> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);
    Task<Result<List<FormulaDetailDto>>> GetPendingValidationFormulasAsync();
    Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id);
    Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId);
}

public interface IFormulaImportExportService
{
    Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(List<FormulaImportItemDto> formulas, string? fileName);
    Task<MemoryStream> ExportAsync(string? category);
    MemoryStream GenerateImportTemplate();
}
```

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| GET | `/api/v1/Formulas` | GetList | DoctorOrAdmin |
| GET | `/api/v1/Formulas/{id}` | GetById | DoctorOrAdmin |
| POST | `/api/v1/Formulas` | Create | DoctorOrAdmin |
| PUT | `/api/v1/Formulas/{id}` | Update | DoctorOrAdmin |
| DELETE | `/api/v1/Formulas/{id}` | Delete | DoctorOrAdmin |
| POST | `/api/v1/Formulas/batch-import` | Import | DoctorOrAdmin |
| GET | `/api/v1/Formulas/pending-validation` | GetPendingValidation | DoctorOrAdmin |
| POST | `/api/v1/Formulas/{formulaId}/herbs/{herbItemId}/validate` | ValidateHerb | DoctorOrAdmin |
| POST | `/api/v1/Formulas/batch-delete` | BatchDelete | DoctorOrAdmin |
| POST | `/api/v1/Formulas/{id}/toggle-status` | ToggleStatus | DoctorOrAdmin |

### DTO 结构

```
FormulaInputDto
├── Name: string（必填，100字）
├── Effect: string?
├── Description: string?
├── Usage: string?
├── Property: string?
├── Category: string?
├── IsShared: bool
├── Indications: string?
├── Contraindications: string?
├── Remark: string?
├── Herbs: List<FormulaHerbItemInputDto>（必填，非空）
└── Id: Guid?（null=创建，有值=更新）

FormulaHerbItemInputDto
├── HerbId: Guid?（null=延迟绑定）
├── HerbName: string（必填）
├── Dosage: int（1-500）
├── Unit: string（必填）
├── ProcessingMethod: string?
├── Usage: string?
├── SortOrder: int
└── DecocteMethod: enum?

FormulaDetailDto: 所有字段 + Herbs 列表 + HerbCount/TotalPrice（计算值）
FormulaListDto: Id, Name, Effect, Indications, Category, IsShared, ValidationStatus, Status, HerbCount, TotalPrice, CreatedAt
```

## 状态机（延迟绑定）

```
┌─────────────┐
│    Draft     │◄──────────────────┐
│  (待验证)    │                   │
└──────┬──────┘                   │
       │ 所有药材绑定完成           │ 编辑后有未绑定药材
       ▼                          │
┌──────────────┐                   │
│  Validated   │───────────────────┘
│  (已验证)    │
└──────────────┘
```

**延迟绑定流程**:
1. 导入/创建时 → `ValidationStatus = Draft`
2. 药材名匹配药典 → `HerbId` 设置，`IsValidated = true`
3. 药材名未匹配 → `HerbId = null`，`IsValidated = false`
4. 手动绑定 → `POST /validate` 设置 `HerbId`
5. 全部绑定完成 → 自动升级为 `Validated`

## 数据流

### 导入验方
```
Desktop → POST /api/v1/Formulas/batch-import
Controller → FormulaImportExportService.ImportFromDataAsync
  → 遍历每个验方
    → TryMatchHerbAsync（名称/拼音匹配药典）
    → 匹配成功 → HerbId 设置，IsValidated=true
    → 匹配失败 → HerbId=null，IsValidated=false
  → 设置 ValidationStatus=Draft（如有未匹配）
  → 返回 FormulaBatchImportResultDto
```

### 手动绑定药材
```
Desktop → POST /api/v1/Formulas/{formulaId}/herbs/{herbItemId}/validate
Controller → FormulaService.ValidateFormulaHerbAsync
  → 查找 herbItemId 对应的 FormulaHerbItem
  → 通过 IHerbCrossModuleService 查找目标药材
  → 设置 HerbId, HerbName, IsValidated=true
  → 检查是否所有药材都已验证
  → 是 → 设置 ValidationStatus=Validated
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| FluentValidationException | 输入校验失败 | 返回 400 |
| BusinessException | 名称重复 | 返回 400 |
| NotFoundException | GetById 找不到 | 返回 404 |
| OwnershipException | 非所有者操作 | 返回 403 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 延迟绑定 | 导入时药材名未匹配→Draft→手动绑定→Validated | FORMULA-003 |
| 编辑重评估 | Validated 验方编辑后如有未绑定药材→回退 Draft | FORMULA-004 |
| 所有权 | 非共享验方仅所有者可编辑/删除 | FORMULA-005 |
| 药材非空 | 创建/更新时 Herbs 列表必须非空 | FORMULA-001 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| Herbs | 查询药材信息（匹配药典） | Formula → Herbs |
| MedicalCase | 导入验方到处方 | Formula → MedicalCase |

**MedicalCase 集成**:
- `FormulaImportDialog` 允许从验方库选择导入
- 通过 `IFormulaSearchProvider` 查询验方
- 导入后映射为 `PrescriptionItem` 条目

**已知问题**:
- `RestoreAsync` 是空操作（返回 null）
- 批量删除有所有权检查，单删无
