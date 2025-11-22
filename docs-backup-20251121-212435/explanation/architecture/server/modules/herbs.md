# Herbs模块架构文档

**版本**: v1.0 (Epic #1962)
**更新时间**: 2025-11-10
**状态**: ✅ 已完成

---

## 📦 模块概览

**Herbs模块**负责药材基础数据管理，包括CRUD、批量导入/导出、分类管理、引用检查。

### 核心功能

1. **基础CRUD**（Epic #1600）
   - 创建/更新/删除药材
   - 分页查询、按分类筛选
   - 拼音码自动生成

2. **批量导入**（Epic #1962 Phase 2）
   - Desktop层：Excel解析（EPPlus）→ `List<HerbInputDto>`
   - Server层：接收DTO → 业务处理 → 数据库写入
   - 拼音码自动生成：调用`PinYinHelper.GetPinYinCode()`
   - 3种重复策略：Skip（跳过）、Update（更新）、Error（报错）

3. **导出功能**（Epic #1962 Phase 3）
   - Server层：查询 → DTO → JSON返回
   - Desktop层：接收JSON → Excel生成（EPPlus）
   - 性能要求：10000条 < 2秒

4. **引用检查**（Epic #1962 Phase 4）
   - 跨模块依赖：Herbs → Prescriptions
   - 软删除支持：BR-007（CanDelete始终true）
   - 引用统计：显示最近5条引用记录

---

## 🏗️ 架构设计

### 三层架构

```
┌──────────────────────────────────────────────────┐
│  Presentation Layer (HerbsController)           │
│  - POST /api/v1/herbs/batch-import              │
│  - GET  /api/v1/herbs/export-all                │
│  - GET  /api/v1/herbs/{id}/check-reference      │
│  - POST /api/v1/herbs/batch-check-reference     │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Application Layer (HerbService)                 │
│  - BatchImportAsync(herbs, strategy)            │
│  - GetAllForExportAsync(category)               │
│  - CheckReferenceAsync(id)                      │
│  - BatchCheckReferenceAsync(ids)                │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Infrastructure Layer (HerbRepository)           │
│  - GetByNameAsync(name)                         │
│  - ExistsByNameAsync(name, excludeId)           │
│  - GetPagedAsync(page, size, category)          │
│  - GetByCategoryAsync(category)                 │
└──────────────────────────────────────────────────┘
```

---

## 📋 Repository层

### IHerbRepository接口

```csharp
namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材数据仓储接口
/// Epic #1962: 新增批量操作、分类查询、引用检查方法
/// </summary>
internal interface IHerbRepository : IBaseRepository<Herb>
{
    // ========== Epic #1600: 基础CRUD ==========

    /// <summary>
    /// 根据名称查询药材
    /// </summary>
    Task<Herb?> GetByNameAsync(string name);

    /// <summary>
    /// 检查药材名称是否存在（支持排除指定ID）
    /// </summary>
    /// <param name="name">药材名称</param>
    /// <param name="excludeId">排除的药材ID（用于更新时检查重复）</param>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    // ========== Epic #1962: 批量操作 ==========

    /// <summary>
    /// 分页查询药材（支持分类过滤）
    /// </summary>
    Task<PagedResult<Herb>> GetPagedAsync(int pageIndex, int pageSize, string? category = null);

    /// <summary>
    /// 按分类查询所有药材
    /// </summary>
    Task<List<Herb>> GetByCategoryAsync(string category);

    /// <summary>
    /// 获取所有药材（用于导出）
    /// </summary>
    /// <param name="category">可选的分类过滤</param>
    Task<List<Herb>> GetAllAsync(string? category = null);
}
```

### 关键实现要点

```csharp
public async Task<PagedResult<Herb>> GetPagedAsync(int pageIndex, int pageSize, string? category = null)
{
    var query = DbContext.Herbs
        .AsNoTracking() // ⭐ 只读查询，提升性能
        .Where(h => !h.IsDeleted);

    if (!string.IsNullOrEmpty(category))
        query = query.Where(h => h.Category == category);

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderBy(h => h.Name)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<Herb>(items, totalCount, pageIndex, pageSize);
}
```

---

## 📋 Service层

### IHerbService接口

```csharp
namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材业务服务接口
/// Epic #1962: 新增批量导入/导出、引用检查功能
/// </summary>
public interface IHerbService
{
    // ========== Epic #1600: 基础CRUD ==========

    Task<ServiceResult<HerbDto>> CreateAsync(HerbInputDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbInputDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page, int pageSize, string? keyword, string? category);

    // ========== Epic #1962: 批量导入/导出 ==========

    /// <summary>
    /// 批量导入药材（Desktop层已解析Excel）
    /// </summary>
    /// <param name="herbs">药材DTO列表</param>
    /// <param name="strategy">重复处理策略（Skip/Update/Error）</param>
    Task<ServiceResult<HerbBatchImportResultDto>> BatchImportAsync(
        List<HerbInputDto> herbs,
        DuplicateStrategy strategy);

    /// <summary>
    /// 获取所有药材用于导出（返回JSON，Desktop层生成Excel）
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetAllForExportAsync(string? category = null);

    // ========== Epic #1962: 引用检查 ==========

    /// <summary>
    /// 检查单个药材的引用关系
    /// </summary>
    Task<ServiceResult<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId);

    /// <summary>
    /// 批量检查药材引用关系
    /// </summary>
    Task<ServiceResult<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> herbIds);

    /// <summary>
    /// 批量删除药材（软删除）
    /// </summary>
    Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);
}
```

### 关键实现要点

**1. 批量导入逻辑**：

```csharp
public async Task<ServiceResult<HerbBatchImportResultDto>> BatchImportAsync(
    List<HerbInputDto> herbs,
    DuplicateStrategy strategy)
{
    // BR-006: 单次导入最多10000条
    if (herbs.Count > 10000)
        return ServiceResult<HerbBatchImportResultDto>.Fail("单次导入不能超过10000条");

    var result = new HerbBatchImportResultDto
    {
        TotalCount = herbs.Count
    };

    foreach (var dto in herbs)
    {
        try
        {
            // 1. ⭐ 生成拼音码（调用Shared层工具）
            dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);

            // 2. 检查重复
            var exists = await _repository.ExistsByNameAsync(dto.Name);
            if (exists)
            {
                if (strategy == DuplicateStrategy.Skip)
                {
                    result.SkippedCount++;
                    result.SkippedItems.Add(new FailedItem
                    {
                        RowNumber = herbs.IndexOf(dto) + 1,
                        Name = dto.Name,
                        Reason = "药材名称已存在（跳过）"
                    });
                    continue;
                }
                else if (strategy == DuplicateStrategy.Error)
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new FailedItem
                    {
                        RowNumber = herbs.IndexOf(dto) + 1,
                        Name = dto.Name,
                        Reason = "药材名称已存在（报错）"
                    });
                    continue;
                }
                // Update策略：继续处理
            }

            // 3. 验证DTO（FluentValidation）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                result.FailureCount++;
                result.FailedItems.Add(new FailedItem
                {
                    RowNumber = herbs.IndexOf(dto) + 1,
                    Name = dto.Name,
                    Reason = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                });
                continue;
            }

            // 4. 保存实体
            var entity = _mapper.Map<Herb>(dto);
            await _repository.AddAsync(entity);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailureCount++;
            result.FailedItems.Add(new FailedItem
            {
                RowNumber = herbs.IndexOf(dto) + 1,
                Name = dto.Name ?? "未知",
                Reason = ex.Message
            });
        }
    }

    // 5. ⭐ 一次性保存（事务保证）
    await _unitOfWork.SaveChangesAsync();

    result.Message = $"导入完成: 成功{result.SuccessCount}条, 失败{result.FailureCount}条, 跳过{result.SkippedCount}条";
    return ServiceResult<HerbBatchImportResultDto>.Success(result);
}
```

**2. 引用检查逻辑**（跨模块依赖）：

```csharp
public async Task<ServiceResult<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId)
{
    var herb = await _repository.GetByIdAsync(herbId);
    if (herb == null)
        return ServiceResult<HerbReferenceCheckDto>.Fail("药材不存在");

    // ⭐ 调用Prescription模块的Repository检查引用
    var referenceCount = await _prescriptionRepository.GetHerbReferenceCountAsync(herbId);
    var recentReferences = await _prescriptionRepository.GetRecentReferencesAsync(herbId, 5);

    var result = new HerbReferenceCheckDto
    {
        HerbId = herbId,
        HerbName = herb.Name,
        HasReferences = referenceCount > 0,
        ReferenceCount = referenceCount,
        CanDelete = true, // BR-007: 软删除支持，始终可删除
        DeleteRestriction = referenceCount > 0
            ? $"药材被{referenceCount}个处方引用，删除后将软删除"
            : null,
        RecentReferences = recentReferences
    };

    return ServiceResult<HerbReferenceCheckDto>.Success(result);
}
```

---

## 📋 Controller端点

### API端点列表

| 端点 | 方法 | 说明 | 业务规则 | Epic |
|-----|------|------|---------|------|
| `/api/v1/herbs` | GET | 分页查询药材列表 | 支持keyword和category过滤 | #1600 |
| `/api/v1/herbs/{id}` | GET | 查询药材详情 | - | #1600 |
| `/api/v1/herbs` | POST | 创建药材 | BR-001（名称1-50字符）<br>BR-002（名称唯一） | #1600 |
| `/api/v1/herbs/{id}` | PUT | 更新药材 | BR-001, BR-002 | #1600 |
| `/api/v1/herbs/{id}` | DELETE | 删除药材（软删除） | BR-007（软删除支持） | #1600 |
| `/api/v1/herbs/batch-import` | POST | 批量导入（DTO列表） | BR-006（≤10000条） | #1962 |
| `/api/v1/herbs/export-all` | GET | 导出数据（JSON） | 支持category过滤 | #1962 |
| `/api/v1/herbs/{id}/check-reference` | GET | 检查单个引用 | 跨模块依赖Prescriptions | #1962 |
| `/api/v1/herbs/batch-check-reference` | POST | 批量检查引用 | BR-006（≤100条） | #1962 |
| `/api/v1/herbs/batch-delete` | POST | 批量删除 | BR-006（≤100条）<br>BR-007（软删除） | #1169 |

### 关键端点实现

**批量导入端点**：

```csharp
/// <summary>
/// 批量导入药材数据（Epic #1962 Task 2.3）
/// Desktop层负责Excel解析，Server层接收DTO列表
/// </summary>
[HttpPost("batch-import")]
[ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<ApiResponse<HerbBatchImportResultDto>>> BatchImport(
    [FromBody] HerbBatchImportRequestDto request)
{
    try
    {
        // 验证请求
        if (request.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail<HerbBatchImportResultDto>("药材列表不能为空");

        // BR-006: 批量导入数量限制
        if (request.Herbs.Count > 10000)
            return ValidationFail<HerbBatchImportResultDto>("批量导入最多支持10000条记录");

        var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy);

        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("批量导入药材（Epic #1962）",
                new {
                    TotalCount = result.Data.TotalCount,
                    SuccessCount = result.Data.SuccessCount,
                    FailureCount = result.Data.FailureCount,
                    SkippedCount = result.Data.SkippedCount,
                    Strategy = request.Strategy.ToString()
                },
                null);
        }

        return HandleServiceResult(result, $"批量导入完成: 成功{result.Data?.SuccessCount ?? 0}条");
    }
    catch (Exception ex)
    {
        return HandleException<HerbBatchImportResultDto>(ex, "批量导入药材",
            new { HerbCount = request.Herbs?.Count, Strategy = request.Strategy });
    }
}
```

---

## 📊 性能基准

### 批量操作性能要求

| 操作 | 数据量 | 性能要求 | 实际性能 | 优化措施 |
|-----|-------|---------|---------|---------|
| **批量导入** | 1000条 | < 10秒 | ~8秒 | 使用事务、批量SaveChanges |
| **数据导出** | 10000条 | < 2秒 | ~1.5秒 | AsNoTracking()、无跟踪查询 |
| **引用检查** | 单次 | < 500ms | ~300ms | 索引优化、TOP 5限制 |
| **分页查询** | 每页20条 | < 100ms | ~50ms | Category索引、覆盖索引 |

### 索引优化（Epic #1962 Phase 1）

```sql
-- 唯一索引（药材名称，支持快速重复检查）
CREATE UNIQUE INDEX IX_Herbs_Name
ON Herbs(Name)
WHERE IsDeleted = 0;

-- 普通索引（拼音码，支持拼音搜索）
CREATE INDEX IX_Herbs_PinYinCode
ON Herbs(PinYinCode);

-- 覆盖索引（分类+状态，包含常用查询字段）
CREATE INDEX IX_Herbs_Category_Status_Includes
ON Herbs(Category, Status)
INCLUDE (Name, PinYinCode);
```

---

## 🔗 跨模块依赖

### Herbs → Prescriptions

**依赖原因**: 删除药材前需检查处方引用关系

```csharp
// src/Server/Modules/LYBT.Module.Prescriptions/Repositories/IPrescriptionRepository.cs

public interface IPrescriptionRepository
{
    // Epic #1962 新增：跨模块引用检查
    Task<int> GetHerbReferenceCountAsync(Guid herbId);
    Task<List<PrescriptionReferenceDto>> GetRecentReferencesAsync(Guid herbId, int limit = 5);
}
```

**业务规则**:
- **BR-007**: 即使被引用也可删除（软删除支持）
- **BR-006**: 批量删除≤100条（防止长事务）

---

## 📚 业务规则

| 规则ID | 描述 | 验证层 | 实现位置 |
|--------|------|--------|---------|
| **BR-001** | 药材名称1-50字符，必填 | FluentValidation | HerbInputDtoValidator |
| **BR-002** | 药材名称唯一性 | Service层 | HerbService.CreateAsync/UpdateAsync |
| **BR-003** | 拼音码自动生成 | Service层 | HerbService（调用PinYinHelper） |
| **BR-004** | 分类可选，最多50字符 | FluentValidation | HerbInputDtoValidator |
| **BR-006** | 批量操作数量限制 | Controller | BatchImport(≤10000), BatchDelete(≤100) |
| **BR-007** | 软删除支持 | Service层 | DeleteAsync（设置IsDeleted=true） |
| **BR-008** | Category索引优化 | Database | Migration（覆盖索引） |

---

## 📖 相关文档

- **需求文档**: [herbs-management-enhancement-requirements.md](../herbs-management-enhancement-requirements.md)
- **技术设计**: [herbs-management-enhancement-design.md](../herbs-management-enhancement-design.md)
- **跨模块依赖**: [../../shared/cross-module-dependencies.md](../../shared/cross-module-dependencies.md)
- **批量操作模式**: [../../../../how-to/patterns/batch-operations.md](../../../../how-to/patterns/batch-operations.md)
- **Patients模块参考**: [Epic #1934 - Patients批量导入](https://github.com/shouqitao/LYBTZYZS/issues/1934)

---

## 🏷️ 变更历史

| 版本 | 日期 | 描述 | Epic/Issue |
|------|------|------|------------|
| v1.0 | 2025-11-10 | 初始版本，文档化Epic #1962实现 | #1962, #1983 |

---

**最后更新**: 2025-11-10
**维护者**: @shouqitao
