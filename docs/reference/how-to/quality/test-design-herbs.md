# 测试设计方案 - LYBT.Module.Herbs.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Server/Modules/LYBT.Module.Herbs/` |
| **测试路径** | `tests/UnitTests/Server/Modules/LYBT.Module.Herbs.Tests/` |
| **现有测试数** | 24 |
| **目标测试数** | 65 |
| **新增测试数** | +41 |
| **优先级** | P1 |

---

## 2. 被测组件清单

### 2.1 HerbService (17个方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetPagedAsync | 0 | 5 | +5 |
| GetByIdAsync | 0 | 3 | +3 |
| CreateAsync | 0 | 5 | +5 |
| UpdateAsync | 0 | 5 | +5 |
| DeleteAsync | 0 | 3 | +3 |
| SearchAsync | 0 | 3 | +3 |
| ImportFromExcelAsync | 0 | 5 | +5 |
| ExportAsync | 0 | 2 | +2 |
| GenerateImportTemplate | 0 | 1 | +1 |
| BatchImportAsync | 0 | 5 | +5 |
| GetAllForExportAsync | 0 | 2 | +2 |
| CheckReferenceAsync | 0 | 3 | +3 |
| BatchCheckReferenceAsync | 0 | 3 | +3 |
| ToggleStatusAsync | 0 | 3 | +3 |
| RestoreAsync | 0 | 3 | +3 |
| BatchUpdateStatusAsync | 0 | 3 | +3 |
| BatchDeleteAsync | 0 | 3 | +3 |
| **小计** | **0** | **58** | **+58** |

### 2.2 HerbRepository (已有部分测试)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetByNameAsync | 3 | 3 | 0 |
| GetByNameOrPinyinAsync | 10 | 10 | 0 |
| GetPagedAsync | 11 | 11 | 0 |
| ExistsByNameAsync | 0 | 3 | +3 |
| GetByIdIncludingDeletedAsync | 0 | 2 | +2 |
| **小计** | **24** | **29** | **+5** |

---

## 3. HerbService 测试设计

### 3.1 GetPagedAsync 测试 (5个)

```
GetPagedAsync_WithDefaultParameters_ShouldReturnPagedResult
GetPagedAsync_WithKeyword_ShouldFilterByNameOrPinyin
GetPagedAsync_WithCategory_ShouldFilterByCategory
GetPagedAsync_WithPagination_ShouldReturnCorrectPage
GetPagedAsync_WithNoResults_ShouldReturnEmptyList
```

**测试要点**:
- 验证默认分页参数 (page=1, pageSize=20)
- 验证关键字过滤 (Name, PinYinCode)
- 验证分类过滤
- 验证分页计算 (TotalCount, TotalPages)

### 3.2 GetByIdAsync 测试 (3个)

```
GetByIdAsync_WithExistingId_ShouldReturnHerbDetail
GetByIdAsync_WithNonExistentId_ShouldReturnFailure
GetByIdAsync_WithDeletedHerb_ShouldReturnFailure
```

**测试要点**:
- 验证返回 HerbDetailDto 包含所有字段
- 验证软删除药材不可获取

### 3.3 CreateAsync 测试 (5个)

```
CreateAsync_WithValidInput_ShouldCreateAndReturnHerb
CreateAsync_WithDuplicateName_ShouldReturnFailure
CreateAsync_WithInvalidInput_ShouldReturnValidationError
CreateAsync_ShouldGeneratePinYinCode
CreateAsync_ShouldSetDefaultStatus
```

**测试要点**:
- 验证创建成功返回 HerbDetailDto
- 验证重复名称检查 (ExistsByNameAsync)
- 验证 FluentValidation 集成
- 验证自动生成拼音码
- 验证默认状态 (Enabled)

### 3.4 UpdateAsync 测试 (5个)

```
UpdateAsync_WithValidInput_ShouldUpdateAndReturnHerb
UpdateAsync_WithNonExistentId_ShouldReturnFailure
UpdateAsync_WithDuplicateName_ShouldReturnFailure
UpdateAsync_WithDeletedHerb_ShouldReturnFailure
UpdateAsync_ShouldUpdatePinYinCode
```

**测试要点**:
- 验证更新成功返回更新后的 DTO
- 验证名称唯一性检查 (排除当前ID)
- 验证软删除药材不可更新

### 3.5 DeleteAsync 测试 (3个)

```
DeleteAsync_WithNoReferences_ShouldSoftDelete
DeleteAsync_WithReferences_ShouldReturnFailure
DeleteAsync_WithNonExistentId_ShouldReturnFailure
```

**测试要点**:
- 验证软删除 (IsDeleted = true)
- 验证引用检查 (CheckReferenceAsync)
- 验证不存在的药材返回失败

### 3.6 SearchAsync 测试 (3个)

```
SearchAsync_WithKeyword_ShouldReturnMatchingHerbs
SearchAsync_WithEmptyKeyword_ShouldReturnAll
SearchAsync_ShouldExcludeDeleted
```

### 3.7 ImportFromExcelAsync 测试 (5个)

```
ImportFromExcelAsync_WithValidFile_ShouldImportHerbs
ImportFromExcelAsync_WithInvalidFormat_ShouldReturnError
ImportFromExcelAsync_WithDuplicateNames_ShouldReportDuplicates
ImportFromExcelAsync_WithValidationErrors_ShouldReportErrors
ImportFromExcelAsync_WithEmptyFile_ShouldReturnError
```

**测试要点**:
- 验证 Excel 解析
- 验证重复名称处理
- 验证数据验证

### 3.8 ExportAsync 测试 (2个)

```
ExportAsync_WithData_ShouldReturnExcelStream
ExportAsync_WithCategory_ShouldFilterExport
```

### 3.9 GenerateImportTemplate 测试 (1个)

```
GenerateImportTemplate_ShouldReturnValidTemplate
```

### 3.10 BatchImportAsync 测试 (5个)

```
BatchImportAsync_WithNewHerbs_ShouldCreateAll
BatchImportAsync_WithDuplicates_SkipStrategy_ShouldSkip
BatchImportAsync_WithDuplicates_UpdateStrategy_ShouldUpdate
BatchImportAsync_WithDuplicates_ErrorStrategy_ShouldReportError
BatchImportAsync_WithOverLimit_ShouldReturnError
```

**测试要点**:
- 验证批量创建 (<=10000条)
- 验证 DuplicateStrategy (Skip/Update/Error)
- 验证超限错误 (BR-006)

### 3.11 GetAllForExportAsync 测试 (2个)

```
GetAllForExportAsync_ShouldReturnAllHerbs
GetAllForExportAsync_WithCategory_ShouldFilter
```

### 3.12 CheckReferenceAsync 测试 (3个)

```
CheckReferenceAsync_WithNoReferences_ShouldReturnZeroCount
CheckReferenceAsync_WithReferences_ShouldReturnCount
CheckReferenceAsync_ShouldReturnRecentReferences
```

**测试要点**:
- 验证引用计数
- 验证最近5条引用记录

### 3.13 BatchCheckReferenceAsync 测试 (3个)

```
BatchCheckReferenceAsync_WithValidIds_ShouldReturnAllResults
BatchCheckReferenceAsync_WithOverLimit_ShouldReturnError
BatchCheckReferenceAsync_WithEmptyList_ShouldReturnEmpty
```

**测试要点**:
- 验证批量检查 (<=100条, BR-006)

### 3.14 ToggleStatusAsync 测试 (3个)

```
ToggleStatusAsync_EnabledToDisabled_ShouldToggle
ToggleStatusAsync_DisabledToEnabled_ShouldToggle
ToggleStatusAsync_WithNonExistentId_ShouldReturnFailure
```

### 3.15 RestoreAsync 测试 (3个)

```
RestoreAsync_WithDeletedHerb_ShouldRestore
RestoreAsync_WithNonDeletedHerb_ShouldReturnFailure
RestoreAsync_WithNonExistentId_ShouldReturnFailure
```

**测试要点**:
- 验证 GetByIdIncludingDeletedAsync 调用
- 验证 IsDeleted = false

### 3.16 BatchUpdateStatusAsync 测试 (3个)

```
BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll
BatchUpdateStatusAsync_WithPartialSuccess_ShouldReportResults
BatchUpdateStatusAsync_WithEmptyList_ShouldReturnFailure
```

### 3.17 BatchDeleteAsync 测试 (3个)

```
BatchDeleteAsync_WithNoReferences_ShouldDeleteAll
BatchDeleteAsync_WithSomeReferences_ShouldReportResults
BatchDeleteAsync_WithEmptyList_ShouldReturnFailure
```

---

## 4. HerbRepository 补充测试

### 4.1 ExistsByNameAsync 测试 (3个)

```
ExistsByNameAsync_WithExistingName_ShouldReturnTrue
ExistsByNameAsync_WithNonExistentName_ShouldReturnFalse
ExistsByNameAsync_WithExcludeId_ShouldExcludeSelf
```

**测试要点**:
- 验证名称存在检查
- 验证排除指定ID

### 4.2 GetByIdIncludingDeletedAsync 测试 (2个)

```
GetByIdIncludingDeletedAsync_WithDeletedHerb_ShouldReturn
GetByIdIncludingDeletedAsync_WithNonExistentId_ShouldReturnNull
```

---

## 5. 测试数据设计

### 5.1 TestHerbBuilder

```csharp
public static class TestHerbBuilder
{
    public static Herb Create(
        Guid? id = null,
        string? name = null,
        string? pinYinCode = null,
        string? category = null,
        decimal? price = null,
        CommonStatus? status = null,
        bool isDeleted = false)
    {
        return new Herb
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试药材_{Guid.NewGuid():N}".Substring(0, 20),
            PinYinCode = pinYinCode ?? "CSYC",
            Category = category ?? "补气药",
            Origin = "测试产地",
            Spec = "统货",
            Unit = "g",
            Price = price ?? 50m,
            CostPrice = 30m,
            Effect = "测试功效",
            Usage = "测试用法",
            Remark = "测试备注",
            Status = status ?? CommonStatus.Enabled,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    public static HerbInputDto CreateInputDto(
        string? name = null,
        decimal? price = null)
    {
        return new HerbInputDto
        {
            Name = name ?? $"测试药材_{Guid.NewGuid():N}".Substring(0, 20),
            Unit = "g",
            Price = price ?? 50m
        };
    }
}
```

---

## 6. Mock 策略

### 6.1 HerbServiceTests Mock 设置

```csharp
public class HerbServiceTests
{
    private readonly Mock<IHerbRepository> _repositoryMock;
    private readonly Mock<IValidator<HerbInputDto>> _validatorMock;
    private readonly Mock<AppDbContext> _dbContextMock;
    private readonly HerbService _sut;

    public HerbServiceTests()
    {
        _repositoryMock = new Mock<IHerbRepository>();
        _validatorMock = new Mock<IValidator<HerbInputDto>>();
        _dbContextMock = CreateMockDbContext();

        // 默认: 验证通过
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<HerbInputDto>(), default))
            .ReturnsAsync(new ValidationResult());

        // 默认: 名称不存在
        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);

        // 默认: 无引用
        _repositoryMock
            .Setup(x => x.CheckReferenceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new HerbReferenceCheckDto { HasReferences = false });

        _sut = new HerbService(
            _repositoryMock.Object,
            NullLogger<HerbService>.Instance,
            _validatorMock.Object,
            _dbContextMock.Object);
    }
}
```

---

## 7. 验收标准

| 指标 | 目标 |
|------|------|
| HerbService 测试数 | 58 |
| HerbRepository 测试数 | 29 |
| 总测试数 | 87 |
| 全部测试通过 | 100% |
| 编译警告 | 0 |
| Service 方法覆盖 | 100% |

---

## 8. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 创建 HerbServiceTests 基础框架 | 15min |
| 2 | 实现 CRUD 测试 (21个) | 45min |
| 3 | 实现 批量操作测试 (14个) | 30min |
| 4 | 实现 导入导出测试 (8个) | 30min |
| 5 | 实现 状态管理测试 (9个) | 20min |
| 6 | 实现 引用检查测试 (6个) | 15min |
| 7 | 补充 Repository 测试 (5个) | 15min |
| 8 | 编译验证和修复 | 10min |
| **总计** | | **~3h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
