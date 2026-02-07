# 测试设计方案 - LYBT.Module.Patients.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Server/Modules/LYBT.Module.Patients/` |
| **测试路径** | `tests/UnitTests/Server/Modules/LYBT.Module.Patients.Tests/` |
| **现有测试数** | 36 |
| **目标测试数** | 75 |
| **新增测试数** | +39 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 PatientService (22个方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetPagedAsync | 3 | 3 | 0 |
| GetByIdAsync | 2 | 2 | 0 |
| CreateAsync | 3 | 3 | 0 |
| UpdateAsync | 2 | 2 | 0 |
| SearchAsync | 2 | 2 | 0 |
| DeleteAsync | 2 | 2 | 0 |
| BatchImportAsync | 0 | 10 | +10 |
| ExportTemplateAsync | 0 | 2 | +2 |
| ExportPatientsAsync | 0 | 3 | +3 |
| RestoreAsync | 0 | 3 | +3 |
| BatchDeleteAsync | 0 | 4 | +4 |
| CheckReferenceAsync | 0 | 3 | +3 |
| BatchCheckReferenceAsync | 0 | 3 | +3 |
| GetPagedEntityAsync | 2 | 2 | 0 |
| **小计** | **19** | **44** | **+28** |

### 2.2 PatientRepository (6个方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetByNameAsync | 2 | 2 | 0 |
| ExistsAsync | 1 | 3 | +2 |
| GetByPhoneNumberAsync | 0 | 3 | +3 |
| GetByIdIncludingDeletedAsync | 0 | 2 | +2 |
| ApplyKeywordFilter | 2 | 2 | 0 |
| **小计** | **5** | **12** | **+7** |

---

## 3. PatientService 补充测试设计

### 3.1 BatchImportAsync 测试 (10个)

```
BatchImportAsync_WithValidData_ShouldImportAll
BatchImportAsync_WithDuplicatePhoneNumber_ShouldReportError
BatchImportAsync_WithDuplicateIdNumber_ShouldReportError
BatchImportAsync_WithInvalidGender_ShouldReportError
BatchImportAsync_WithInvalidBirthDate_ShouldReportError
BatchImportAsync_WithMissingRequiredFields_ShouldReportError
BatchImportAsync_WithPartialSuccess_ShouldReportDetails
BatchImportAsync_ShouldGeneratePinYinCode
BatchImportAsync_WithEmptyData_ShouldReturnError
BatchImportAsync_ShouldProvideFailureDetails
```

**测试要点**:
- 验证 Excel 行解析 (ParseExcelRow)
- 验证重复检查 (手机号 BR-004, 身份证)
- 验证部分成功模式
- 验证失败详情和修复建议

### 3.2 ExportTemplateAsync 测试 (2个)

```
ExportTemplateAsync_ShouldReturnValidExcel
ExportTemplateAsync_ShouldContainAllColumns
```

### 3.3 ExportPatientsAsync 测试 (3个)

```
ExportPatientsAsync_WithPatients_ShouldExportAll
ExportPatientsAsync_WithNoPatients_ShouldReturnEmptyExcel
ExportPatientsAsync_ShouldMaskSensitiveData
```

### 3.4 RestoreAsync 测试 (3个)

```
RestoreAsync_WithDeletedPatient_ShouldRestore
RestoreAsync_WithNonDeletedPatient_ShouldReturnFailure
RestoreAsync_WithNonExistentId_ShouldReturnFailure
```

### 3.5 BatchDeleteAsync 测试 (4个)

```
BatchDeleteAsync_WithNoReferences_ShouldDeleteAll
BatchDeleteAsync_WithSomeReferences_ShouldReportPartial
BatchDeleteAsync_WithEmptyList_ShouldReturnFailure
BatchDeleteAsync_ShouldIsolateItemErrors
```

### 3.6 CheckReferenceAsync 测试 (3个)

```
CheckReferenceAsync_WithNoReferences_ShouldReturnFalse
CheckReferenceAsync_WithMedicalCases_ShouldReturnTrue
CheckReferenceAsync_ShouldReturnReferenceCount
```

### 3.7 BatchCheckReferenceAsync 测试 (3个)

```
BatchCheckReferenceAsync_WithValidIds_ShouldReturnAll
BatchCheckReferenceAsync_WithOverLimit_ShouldReturnError
BatchCheckReferenceAsync_WithEmptyList_ShouldReturnEmpty
```

---

## 4. PatientRepository 补充测试设计

### 4.1 ExistsAsync 补充 (2个)

```
ExistsAsync_WithExcludeId_ShouldExcludeSelf
ExistsAsync_WithDeletedPatient_ShouldReturnFalse
```

### 4.2 GetByPhoneNumberAsync (3个)

```
GetByPhoneNumberAsync_WithExistingPhone_ShouldReturn
GetByPhoneNumberAsync_WithNonExistentPhone_ShouldReturnNull
GetByPhoneNumberAsync_ShouldExcludeDeleted
```

### 4.3 GetByIdIncludingDeletedAsync (2个)

```
GetByIdIncludingDeletedAsync_WithDeletedPatient_ShouldReturn
GetByIdIncludingDeletedAsync_WithNonExistentId_ShouldReturnNull
```

---

## 5. 测试数据设计

### 5.1 TestPatientBuilder (Server)

```csharp
public static class TestPatientBuilder
{
    public static Patient Create(
        Guid? id = null,
        string? name = null,
        string? phoneNumber = null,
        string? idNumber = null,
        Gender? gender = null,
        bool isDeleted = false)
    {
        return new Patient
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试患者_{Guid.NewGuid():N}".Substring(0, 10),
            PinYinCode = "CSHZ",
            Gender = gender ?? Gender.Male,
            PhoneNumber = phoneNumber,
            IdNumber = idNumber,
            Status = CommonStatus.Enabled,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    public static PatientInputDto CreateInputDto(
        string? name = null,
        Gender? gender = null,
        string? phoneNumber = null)
    {
        return new PatientInputDto
        {
            Name = name ?? "测试患者",
            Gender = gender ?? Gender.Male,
            PhoneNumber = phoneNumber
        };
    }

    public static PatientImportItemDto CreateImportItem(
        string? name = null,
        string? gender = null,
        string? phoneNumber = null)
    {
        return new PatientImportItemDto
        {
            Name = name ?? "导入患者",
            Gender = gender ?? "男",
            PhoneNumber = phoneNumber ?? "13800138000"
        };
    }
}
```

---

## 6. Mock 策略

```csharp
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly Mock<IValidator<PatientInputDto>> _validatorMock;
    private readonly Mock<IMedicalCaseRepository> _medicalCaseRepoMock;
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _validatorMock = new Mock<IValidator<PatientInputDto>>();
        _medicalCaseRepoMock = new Mock<IMedicalCaseRepository>();

        // 默认: 验证通过
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<PatientInputDto>(), default))
            .ReturnsAsync(new ValidationResult());

        // 默认: 手机号不存在
        _repositoryMock
            .Setup(x => x.GetByPhoneNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((Patient?)null);

        // 默认: 无医案引用
        _medicalCaseRepoMock
            .Setup(x => x.CountByPatientIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        _sut = new PatientService(
            _repositoryMock.Object,
            _validatorMock.Object,
            _medicalCaseRepoMock.Object,
            NullLogger<PatientService>.Instance);
    }
}
```

---

## 7. 验收标准

| 指标 | 目标 |
|------|------|
| PatientService 测试数 | 44 |
| PatientRepository 测试数 | 12 |
| PatientsController 测试数 | 12 (现有) |
| 总测试数 | 75 |
| 批量导入覆盖 | 100% |
| 引用检查覆盖 | 100% |

---

## 8. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | BatchImportAsync 测试 (10个) | 40min |
| 2 | Export 测试 (5个) | 20min |
| 3 | RestoreAsync 测试 (3个) | 10min |
| 4 | BatchDeleteAsync 测试 (4个) | 15min |
| 5 | 引用检查测试 (6个) | 20min |
| 6 | Repository 补充 (7个) | 25min |
| 7 | 编译验证和修复 | 15min |
| **总计** | | **~2.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
