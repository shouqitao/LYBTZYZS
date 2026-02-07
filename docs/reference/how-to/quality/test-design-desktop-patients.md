# 测试设计方案 - LYBT.Desktop.Patients.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.Patients.Tests/` |
| **现有测试数** | 7 (6 Model + 1 占位) |
| **目标测试数** | 75 |
| **新增测试数** | +68 |
| **优先级** | P1 |

---

## 2. 被测组件清单

### 2.1 Services

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| PatientService | 7 | 14 |
| PatientSearchManager | 9 | 12 |
| PatientSearchCache | 3 | 5 |
| PendingQueueManager | 4 | 6 |
| PatientImportExecutor | 4 | 6 |

### 2.2 Repository & CommandHandler

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| PatientRepository | 12 | 18 |
| PatientCommandHandler | 7 | 10 |

### 2.3 Components

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| PatientValidator | 6 | 10 |

---

## 3. PatientService 测试设计 (14个)

### 3.1 CRUD 操作 (8个)

```
CreatePatientAsync_WithValidInput_ShouldReturnSuccess
CreatePatientAsync_WithInvalidInput_ShouldReturnFailure
UpdatePatientAsync_WithValidInput_ShouldReturnSuccess
UpdatePatientAsync_WithNonExistentId_ShouldReturnFailure
DeletePatientAsync_WithNoReferences_ShouldReturnSuccess
DeletePatientAsync_WithReferences_ShouldReturnFailure
GetByIdAsync_WithExistingId_ShouldReturnPatient
GetByIdAsync_WithNonExistentId_ShouldReturnFailure
```

### 3.2 查询操作 (4个)

```
GetPatientsPagedAsync_ShouldReturnPagedResult
GetPatientsPagedAsync_WithKeyword_ShouldFilter
SearchPatientsAsync_WithKeyword_ShouldReturnMatches
SearchPatientsAsync_WithEmptyKeyword_ShouldReturnAll
```

### 3.3 批量操作 (2个)

```
BatchDeletePatientsAsync_WithValidIds_ShouldDeleteAll
BatchDeletePatientsAsync_WithSomeReferences_ShouldReportPartial
```

---

## 4. PatientRepository 测试设计 (18个)

### 4.1 基础 CRUD (8个)

```
GetPagedAsync_ShouldReturnPagedResult
GetByIdAsync_WithExistingId_ShouldReturnPatient
GetByIdAsync_WithNonExistentId_ShouldReturnNull
CreateAsync_WithValidInput_ShouldCreate
UpdateAsync_WithExistingId_ShouldUpdate
DeleteAsync_WithExistingId_ShouldSoftDelete
SearchAsync_WithKeyword_ShouldReturnMatches
GetByIdNumberAsync_WithExistingIdNumber_ShouldReturn
```

### 4.2 特殊查询 (4个)

```
GetByIdNumberAsync_WithNonExistentIdNumber_ShouldReturnNull
SearchAsync_ByPhoneNumber_ShouldReturnMatches
SearchAsync_ByName_ShouldReturnMatches
SearchAsync_ExcludesDeletedPatients
```

### 4.3 批量操作 (4个)

```
BatchImportAsync_WithValidData_ShouldImportAll
BatchImportAsync_WithDuplicateIdNumber_ShouldReportError
BatchDeleteAsync_WithValidIds_ShouldDeleteAll
RestoreAsync_WithDeletedPatient_ShouldRestore
```

### 4.4 导出操作 (2个)

```
ExportTemplateAsync_ShouldReturnValidTemplate
ExportPatientsAsync_ShouldReturnExcelBytes
```

---

## 5. PatientCommandHandler 测试设计 (10个)

### 5.1 查询命令 (6个)

```
GetListAsync_ShouldReturnPagedResult
GetDetailAsync_WithExistingId_ShouldReturnDetail
GetDetailAsync_WithNonExistentId_ShouldReturnNull
SearchByNameAsync_ShouldReturnMatches
SearchByPhoneAsync_ShouldReturnMatches
HasMedicalCasesAsync_WithCases_ShouldReturnTrue
```

### 5.2 写入命令 (4个)

```
SaveAsync_WithNewPatient_ShouldCreate
SaveAsync_WithExistingPatient_ShouldUpdate
DeleteAsync_WithExistingId_ShouldReturnTrue
DeleteAsync_WithNonExistentId_ShouldReturnFalse
```

---

## 6. PatientSearchManager 测试设计 (12个)

### 6.1 加载操作 (4个)

```
LoadInitialPatientsAsync_ShouldLoadFirstPage
LoadCurrentPageAsync_ShouldLoadCurrentPage
LoadCurrentPageAsync_WithKeyword_ShouldFilter
LoadCurrentPageAsync_ShouldRaiseSearchCompleted
```

### 6.2 搜索操作 (4个)

```
ExecuteSearchAsync_WithKeyword_ShouldSearch
ExecuteSearchAsync_ShouldResetToFirstPage
ExecuteSearchAsync_WithEmptyKeyword_ShouldLoadAll
ExecuteSearchAsync_ShouldInvalidateCache
```

### 6.3 分页操作 (4个)

```
PreviousPageAsync_ShouldDecrementPage
NextPageAsync_ShouldIncrementPage
CanPreviousPage_OnFirstPage_ShouldReturnFalse
CanNextPage_OnLastPage_ShouldReturnFalse
```

---

## 7. PatientSearchCache 测试设计 (5个)

```
Get_WithCachedData_ShouldReturn
Get_WithNoCachedData_ShouldReturnNull
Set_ShouldCacheData
Invalidate_WithKeyword_ShouldInvalidateSpecific
Invalidate_WithNull_ShouldInvalidateAll
```

---

## 8. PendingQueueManager 测试设计 (6个)

```
LoadPendingCasesAsync_ShouldLoadQueue
LoadPendingCasesAsync_ShouldRaisePendingQueueLoaded
LoadPatientForPendingCaseAsync_ShouldLoadPatient
LoadPatientForPendingCaseAsync_ShouldRaisePatientLoaded
RemoveFromQueue_ShouldRemovePatient
ClearQueue_ShouldClearAll
```

---

## 9. PatientValidator 测试设计 (10个)

### 9.1 基本验证 (4个)

```
ValidateBasicInfo_WithValidData_ShouldPass
ValidateBasicInfo_WithEmptyName_ShouldFail
ValidateBasicInfo_WithInvalidGender_ShouldFail
IsValid_WithAllValidData_ShouldReturnTrue
```

### 9.2 身份证验证 (3个)

```
ValidateIdNumber_With18DigitId_ShouldPass
ValidateIdNumber_WithInvalidFormat_ShouldFail
ValidateIdNumber_WithEmpty_ShouldPass
```

### 9.3 年龄验证 (2个)

```
ValidateAge_WithValidAge_ShouldPass
ValidateAge_WithNegativeAge_ShouldFail
```

### 9.4 转换 (1个)

```
ConvertToInputDto_ShouldMapAllFields
```

---

## 10. PatientImportExecutor 测试设计 (6个)

```
StartImport_WithValidData_ShouldImport
StartImport_ShouldRaiseProgressChanged
StartImport_OnComplete_ShouldRaiseImportCompleted
CancelImport_ShouldStopImport
ProcessSingleImportRow_WithValidRow_ShouldSucceed
ProcessSingleImportRow_WithInvalidRow_ShouldFail
```

---

## 11. 测试数据设计

### 11.1 TestPatientBuilder

```csharp
public static class TestPatientBuilder
{
    public static Patient Create(
        Guid? id = null,
        string? name = null,
        string? phoneNumber = null,
        string? idNumber = null,
        Gender? gender = null)
    {
        return new Patient
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试患者_{Guid.NewGuid():N}".Substring(0, 10),
            PinYinCode = "CSHZ",
            Gender = gender ?? Gender.Male,
            PhoneNumber = phoneNumber ?? "13800138000",
            IdNumber = idNumber,
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PatientInputDto CreateInputDto(
        string? name = null,
        Gender? gender = null)
    {
        return new PatientInputDto
        {
            Name = name ?? "测试患者",
            Gender = gender ?? Gender.Male
        };
    }

    public static PatientDetailModel CreateDetailModel(
        Guid? id = null,
        string? name = null)
    {
        return new PatientDetailModel
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "测试患者",
            Gender = Gender.Male,
            IsNew = id == null
        };
    }
}
```

---

## 12. Mock 策略

### 12.1 PatientServiceTests Mock

```csharp
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();

        _sut = new PatientService(
            _repositoryMock.Object,
            NullLogger<PatientService>.Instance);
    }
}
```

### 12.2 PatientSearchManagerTests Mock

```csharp
public class PatientSearchManagerTests
{
    private readonly Mock<IPatientCommandHandler> _commandHandlerMock;
    private readonly Mock<IPatientSearchCache> _cacheMock;
    private readonly PatientSearchManager _sut;

    public PatientSearchManagerTests()
    {
        _commandHandlerMock = new Mock<IPatientCommandHandler>();
        _cacheMock = new Mock<IPatientSearchCache>();

        // 默认: 返回空结果
        _commandHandlerMock
            .Setup(x => x.GetListAsync())
            .ReturnsAsync(new PagedResult<PatientListDto>());

        _sut = new PatientSearchManager(
            _commandHandlerMock.Object,
            _cacheMock.Object,
            NullLogger<PatientSearchManager>.Instance);
    }
}
```

---

## 13. 验收标准

| 指标 | 目标 |
|------|------|
| PatientService 测试数 | 14 |
| PatientRepository 测试数 | 18 |
| PatientCommandHandler 测试数 | 10 |
| PatientSearchManager 测试数 | 12 |
| PatientSearchCache 测试数 | 5 |
| PendingQueueManager 测试数 | 6 |
| PatientValidator 测试数 | 10 |
| PatientImportExecutor 测试数 | 6 |
| 总测试数 | 81 |
| 全部测试通过 | 100% |

---

## 14. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | PatientService 测试 (14个) | 30min |
| 2 | PatientRepository 测试 (18个) | 40min |
| 3 | PatientCommandHandler 测试 (10个) | 25min |
| 4 | PatientSearchManager 测试 (12个) | 30min |
| 5 | PatientSearchCache 测试 (5个) | 10min |
| 6 | PendingQueueManager 测试 (6个) | 15min |
| 7 | PatientValidator 测试 (10个) | 20min |
| 8 | PatientImportExecutor 测试 (6个) | 15min |
| 9 | 编译验证和修复 | 15min |
| **总计** | | **~3.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
