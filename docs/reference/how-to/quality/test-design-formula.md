# 测试设计方案 - LYBT.Module.Formula.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Server/Modules/LYBT.Module.Formula/` |
| **测试路径** | `tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/` |
| **现有测试数** | 22 |
| **目标测试数** | 65 |
| **新增测试数** | +43 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 FormulaService (12个方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetPagedAsync | 3 | 5 | +2 |
| GetByIdAsync | 2 | 3 | +1 |
| CreateAsync | 4 | 6 | +2 |
| UpdateAsync | 3 | 5 | +2 |
| SearchAsync | 2 | 3 | +1 |
| DeleteAsync | 2 | 3 | +1 |
| ValidateFormulaHerbAsync | 0 | 5 | +5 |
| GetPendingValidationFormulasAsync | 0 | 3 | +3 |
| ToggleStatusAsync | 0 | 3 | +3 |
| RestoreAsync | 0 | 3 | +3 |
| BatchDeleteAsync | 0 | 4 | +4 |
| BatchUpdateStatusAsync | 0 | 3 | +3 |
| **小计** | **16** | **46** | **+30** |

### 2.2 FormulaImportExportService (4个方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| ImportFromDataAsync | 0 | 8 | +8 |
| ExportAsync | 0 | 3 | +3 |
| GenerateImportTemplate | 0 | 2 | +2 |
| TryMatchHerbAsync | 0 | 3 | +3 |
| **小计** | **0** | **16** | **+16** |

---

## 3. FormulaService 补充测试设计

### 3.1 GetPagedAsync 补充 (2个)

```
GetPagedAsync_WithRoleFilter_ShouldFilterByUser
GetPagedAsync_WithCategoryFilter_ShouldFilter
```

### 3.2 ValidateFormulaHerbAsync (5个)

```
ValidateFormulaHerbAsync_WithExistingHerb_ShouldBindHerbId
ValidateFormulaHerbAsync_WithNonExistentHerb_ShouldReturnFailure
ValidateFormulaHerbAsync_ShouldUpdateValidationStatus
ValidateFormulaHerbAsync_WithAlreadyValidated_ShouldReturnSuccess
ValidateFormulaHerbAsync_ShouldMatchByNameOrPinyin
```

**测试要点**:
- 验证 ICrossModuleQueryService 调用
- 验证 ValidationStatus 从 Draft 到 Validated 转换

### 3.3 GetPendingValidationFormulasAsync (3个)

```
GetPendingValidationFormulasAsync_ShouldReturnDraftFormulas
GetPendingValidationFormulasAsync_WithNoResults_ShouldReturnEmpty
GetPendingValidationFormulasAsync_ShouldExcludeValidated
```

### 3.4 ToggleStatusAsync (3个)

```
ToggleStatusAsync_EnabledToDisabled_ShouldToggle
ToggleStatusAsync_DisabledToEnabled_ShouldToggle
ToggleStatusAsync_WithNonExistentId_ShouldReturnFailure
```

### 3.5 RestoreAsync (3个)

```
RestoreAsync_WithDeletedFormula_ShouldRestore
RestoreAsync_WithNonDeletedFormula_ShouldReturnFailure
RestoreAsync_WithNonExistentId_ShouldReturnFailure
```

### 3.6 BatchDeleteAsync (4个)

```
BatchDeleteAsync_WithValidIds_ShouldDeleteAll
BatchDeleteAsync_WithSomeNonExistent_ShouldReportPartial
BatchDeleteAsync_WithEmptyList_ShouldReturnFailure
BatchDeleteAsync_ShouldIsolateItemErrors
```

### 3.7 BatchUpdateStatusAsync (3个)

```
BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll
BatchUpdateStatusAsync_WithMixedResults_ShouldReportPartial
BatchUpdateStatusAsync_WithEmptyList_ShouldReturnFailure
```

---

## 4. FormulaImportExportService 测试设计

### 4.1 ImportFromDataAsync (8个)

```
ImportFromDataAsync_WithValidData_ShouldImportAll
ImportFromDataAsync_WithDuplicateName_ShouldReportError
ImportFromDataAsync_WithInvalidHerbs_ShouldReportError
ImportFromDataAsync_WithPartialSuccess_ShouldReportDetails
ImportFromDataAsync_ShouldMatchHerbsToSystem
ImportFromDataAsync_WithUnmatchedHerbs_ShouldSetDraftStatus
ImportFromDataAsync_WithEmptyData_ShouldReturnError
ImportFromDataAsync_ShouldProvideFailureDetails
```

**测试要点**:
- 验证药材匹配逻辑 (TryMatchHerbAsync)
- 验证 ValidationStatus 设置
- 验证失败详情

### 4.2 ExportAsync (3个)

```
ExportAsync_WithFormulas_ShouldExportAll
ExportAsync_WithNoFormulas_ShouldReturnEmptyExcel
ExportAsync_ShouldIncludeHerbDetails
```

### 4.3 GenerateImportTemplate (2个)

```
GenerateImportTemplate_ShouldReturnValidExcel
GenerateImportTemplate_ShouldContainAllColumns
```

### 4.4 TryMatchHerbAsync (3个)

```
TryMatchHerbAsync_WithExactName_ShouldMatch
TryMatchHerbAsync_WithPinyin_ShouldMatch
TryMatchHerbAsync_WithNoMatch_ShouldReturnNull
```

---

## 5. 测试数据设计

### 5.1 TestFormulaBuilder

```csharp
public static class TestFormulaBuilder
{
    public static Formula Create(
        Guid? id = null,
        string? name = null,
        Guid? userId = null,
        FormulaType? formulaType = null,
        FormulaValidationStatus? validationStatus = null,
        List<FormulaHerbItem>? herbs = null,
        bool isDeleted = false)
    {
        var formulaId = id ?? Guid.NewGuid();
        return new Formula
        {
            Id = formulaId,
            Name = name ?? $"测试方剂_{Guid.NewGuid():N}".Substring(0, 15),
            Effect = "测试功效",
            Indication = "测试主治",
            Usage = "每日一剂",
            UserId = userId ?? Guid.NewGuid(),
            FormulaType = formulaType ?? FormulaType.Experience,
            ValidationStatus = validationStatus ?? FormulaValidationStatus.Validated,
            Status = CommonStatus.Enabled,
            IsDeleted = isDeleted,
            Herbs = herbs ?? CreateDefaultHerbs(formulaId),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<FormulaHerbItem> CreateDefaultHerbs(Guid formulaId)
    {
        return new List<FormulaHerbItem>
        {
            new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                FormulaId = formulaId,
                HerbId = Guid.NewGuid(),
                HerbName = "黄芪",
                Dosage = 15,
                Unit = "g"
            },
            new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                FormulaId = formulaId,
                HerbId = Guid.NewGuid(),
                HerbName = "党参",
                Dosage = 10,
                Unit = "g"
            }
        };
    }

    public static FormulaInputDto CreateInputDto(
        string? name = null,
        List<FormulaHerbItemInputDto>? herbs = null)
    {
        return new FormulaInputDto
        {
            Name = name ?? "测试方剂",
            Effect = "测试功效",
            Herbs = herbs ?? new List<FormulaHerbItemInputDto>
            {
                new FormulaHerbItemInputDto
                {
                    HerbName = "黄芪",
                    Dosage = 15,
                    Unit = "g"
                }
            }
        };
    }

    public static FormulaImportItemDto CreateImportItem(
        string? name = null,
        List<FormulaHerbImportItemDto>? herbs = null)
    {
        return new FormulaImportItemDto
        {
            Name = name ?? "导入方剂",
            Effect = "导入功效",
            Herbs = herbs ?? new List<FormulaHerbImportItemDto>
            {
                new FormulaHerbImportItemDto
                {
                    HerbName = "黄芪",
                    Dosage = "15",
                    Unit = "g"
                }
            }
        };
    }
}
```

---

## 6. Mock 策略

```csharp
public class FormulaServiceTests
{
    private readonly Mock<IFormulaRepository> _repositoryMock;
    private readonly Mock<IValidator<FormulaInputDto>> _validatorMock;
    private readonly Mock<ICrossModuleQueryService> _crossModuleMock;
    private readonly FormulaService _sut;

    public FormulaServiceTests()
    {
        _repositoryMock = new Mock<IFormulaRepository>();
        _validatorMock = new Mock<IValidator<FormulaInputDto>>();
        _crossModuleMock = new Mock<ICrossModuleQueryService>();

        // 默认: 验证通过
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<FormulaInputDto>(), default))
            .ReturnsAsync(new ValidationResult());

        // 默认: 药材匹配成功
        _crossModuleMock
            .Setup(x => x.GetHerbByNameOrPinyinAsync(It.IsAny<string>()))
            .ReturnsAsync(new Herb { Id = Guid.NewGuid(), Name = "匹配药材" });

        _sut = new FormulaService(
            _repositoryMock.Object,
            _validatorMock.Object,
            _crossModuleMock.Object,
            NullLogger<FormulaService>.Instance);
    }
}

public class FormulaImportExportServiceTests
{
    private readonly Mock<IFormulaRepository> _repositoryMock;
    private readonly Mock<ICrossModuleQueryService> _crossModuleMock;
    private readonly FormulaImportExportService _sut;

    public FormulaImportExportServiceTests()
    {
        _repositoryMock = new Mock<IFormulaRepository>();
        _crossModuleMock = new Mock<ICrossModuleQueryService>();

        _sut = new FormulaImportExportService(
            _repositoryMock.Object,
            _crossModuleMock.Object,
            NullLogger<FormulaImportExportService>.Instance);
    }
}
```

---

## 7. 验收标准

| 指标 | 目标 |
|------|------|
| FormulaService 测试数 | 46 |
| FormulaImportExportService 测试数 | 16 |
| 总测试数 | 62 |
| 药材验证覆盖 | 100% |
| 导入导出覆盖 | 100% |
| 批量操作覆盖 | 100% |

---

## 8. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | ValidateFormulaHerbAsync 测试 (5个) | 25min |
| 2 | 状态管理测试 (6个) | 20min |
| 3 | 批量操作测试 (7个) | 25min |
| 4 | ImportFromDataAsync 测试 (8个) | 35min |
| 5 | Export 测试 (5个) | 20min |
| 6 | TryMatchHerbAsync 测试 (3个) | 10min |
| 7 | 其他补充测试 (8个) | 25min |
| 8 | 编译验证和修复 | 15min |
| **总计** | | **~3h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
