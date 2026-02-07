# 测试设计方案 - LYBT.Desktop.LocalData.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Core/LYBT.Desktop.LocalData/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.LocalData.Tests/` |
| **现有测试数** | 51 |
| **目标测试数** | 120 |
| **新增测试数** | +69 |
| **优先级** | P1 |

---

## 2. 被测组件清单

### 2.1 DataSources (5个类)

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| LocalPatientDataSource | 21 | 21 | 0 |
| LocalHerbDataSource | 14 | 14 | 0 |
| LocalUserDataSource | 0 | 15 | +15 |
| LocalFormulaDataSource | 0 | 18 | +18 |
| LocalMedicalCaseDataSource | 0 | 20 | +20 |
| **小计** | **35** | **88** | **+53** |

### 2.2 Services (2个类)

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| LocalAuthService | 16 | 16 | 0 |
| SyncService | 0 | 16 | +16 |
| **小计** | **16** | **32** | **+16** |

---

## 3. LocalUserDataSource 测试设计 (15个)

### 3.1 基础 CRUD (8个)

```
GetByIdAsync_WithExistingId_ShouldReturnUser
GetByIdAsync_WithNonExistentId_ShouldReturnNull
GetByUsernameAsync_WithExistingUsername_ShouldReturnUser
GetByUsernameAsync_WithNonExistentUsername_ShouldReturnNull
GetPagedAsync_ShouldReturnPagedResult
CreateAsync_WithValidUser_ShouldCreate
UpdateAsync_WithExistingUser_ShouldUpdate
DeleteAsync_WithExistingId_ShouldSoftDelete
```

### 3.2 认证相关 (5个)

```
ChangePasswordAsync_WithValidOldPassword_ShouldChange
ChangePasswordAsync_WithInvalidOldPassword_ShouldReturnFalse
ToggleStatusAsync_ShouldToggleUserStatus
UpdateLastLoginTimeAsync_ShouldUpdateTime
ResetFailedLoginCountAsync_ShouldResetToZero
```

### 3.3 失败计数 (2个)

```
IncrementFailedLoginCountAsync_ShouldIncrement
IncrementFailedLoginCountAsync_AtMaxCount_ShouldLockAccount
```

---

## 4. LocalFormulaDataSource 测试设计 (18个)

### 4.1 基础查询 (6个)

```
GetByIdAsync_WithExistingId_ShouldReturnFormula
GetByIdAsync_WithNonExistentId_ShouldReturnNull
GetWithHerbsAsync_ShouldIncludeHerbItems
GetPagedAsync_WithDefaultParams_ShouldReturnPagedResult
GetPagedAsync_WithKeyword_ShouldFilter
GetPagedAsync_WithCategory_ShouldFilter
```

### 4.2 CRUD 操作 (6个)

```
CreateAsync_WithValidFormula_ShouldCreateWithHerbs
CreateAsync_ShouldSetDefaultStatus
UpdateAsync_WithExistingFormula_ShouldUpdate
UpdateAsync_ShouldUpdateHerbItems
DeleteAsync_WithExistingId_ShouldSoftDelete
DeleteAsync_WithNonExistentId_ShouldReturnFalse
```

### 4.3 特殊操作 (6个)

```
CloneAsync_WithExistingFormula_ShouldCreateCopy
CloneAsync_ShouldCopyHerbItems
CloneAsync_WithNonExistentId_ShouldReturnNull
ToggleStatusAsync_EnabledToDisabled_ShouldToggle
ToggleStatusAsync_DisabledToEnabled_ShouldToggle
RestoreAsync_WithDeletedFormula_ShouldRestore
```

---

## 5. LocalMedicalCaseDataSource 测试设计 (20个)

### 5.1 基础查询 (6个)

```
GetByIdAsync_WithExistingId_ShouldReturnMedicalCase
GetByIdAsync_WithNonExistentId_ShouldReturnNull
GetWithDetailsAsync_ShouldIncludeConsultationAndPrescription
GetPagedAsync_ShouldReturnPagedResult
GetByPatientIdAsync_ShouldReturnPatientCases
GetByPatientIdAsync_WithNonExistentPatient_ShouldReturnEmpty
```

### 5.2 复杂查询 (4个)

```
QueryAsync_WithPatientId_ShouldFilter
QueryAsync_WithDateRange_ShouldFilter
QueryAsync_WithCombinedFilters_ShouldFilterAll
QueryAsync_WithNoFilters_ShouldReturnAll
```

### 5.3 CRUD 操作 (5个)

```
CreateAsync_WithValidCase_ShouldCreateWithConsultation
CreateAsync_ShouldGenerateCaseNumber
UpdateAsync_WithExistingCase_ShouldUpdate
SaveAsync_WithNewCase_ShouldCreate
SaveAsync_WithExistingCase_ShouldUpdate
```

### 5.4 状态管理 (4个)

```
CompleteAsync_WithDraftCase_ShouldMarkComplete
CompleteAsync_WithAlreadyCompleteCase_ShouldReturnFalse
CancelAsync_WithDraftCase_ShouldMarkCancelled
DeleteAsync_WithExistingId_ShouldSoftDelete
```

### 5.5 工具方法 (1个)

```
GenerateCaseNumber_ShouldReturnFormattedNumber
```

---

## 6. SyncService 测试设计 (16个)

### 6.1 支持类型 (1个)

```
GetSupportedEntityTypesAsync_ShouldReturnThreeTypes
```

### 6.2 元数据获取 (5个)

```
GetLocalMetadataAsync_ShouldReturnAllEntityMetadata
GetHerbMetadataAsync_ShouldReturnHerbMetadata
GetPatientMetadataAsync_ShouldReturnPatientMetadata
GetFormulaMetadataAsync_ShouldReturnFormulaMetadata
GetLocalMetadataAsync_WithEmptyDatabase_ShouldReturnZeroCounts
```

### 6.3 差异检查 (3个)

```
CheckDifferencesAsync_WithLocalOnlyEntities_ShouldReturnDiff
CheckDifferencesAsync_WithServerOnlyEntities_ShouldReturnDiff
CheckDifferencesAsync_WithNoChanges_ShouldReturnEmpty
```

### 6.4 同步执行 (4个)

```
ExecuteSyncAsync_ShouldUploadLocalChanges
ExecuteSyncAsync_ShouldDownloadServerChanges
ExecuteSyncAsync_ShouldHandleConflicts
ExecuteSyncAsync_WithApiError_ShouldReturnFailure
```

### 6.5 JSON 导出 (3个)

```
GetLocalEntitiesAsJsonAsync_ShouldReturnValidJson
GetHerbsAsJsonAsync_ShouldReturnHerbsJson
GetPatientsAsJsonAsync_ShouldReturnPatientsJson
```

---

## 7. 测试数据设计

### 7.1 TestUserBuilder

```csharp
public static class TestUserBuilder
{
    public static User Create(
        Guid? id = null,
        string? username = null,
        string? realName = null,
        UserRole? role = null,
        CommonStatus? status = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? $"user_{Guid.NewGuid():N}".Substring(0, 20),
            RealName = realName ?? "测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Role = role ?? UserRole.Doctor,
            Status = status ?? CommonStatus.Enabled,
            FailedLoginCount = 0,
            IsDeleted = false
        };
    }
}
```

### 7.2 TestFormulaBuilder

```csharp
public static class TestFormulaBuilder
{
    public static Formula Create(
        Guid? id = null,
        string? name = null,
        List<FormulaHerbItem>? herbs = null)
    {
        var formula = new Formula
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试方剂_{Guid.NewGuid():N}".Substring(0, 20),
            Effect = "测试功效",
            FormulaType = FormulaType.Experience,
            Status = CommonStatus.Enabled,
            IsDeleted = false
        };

        formula.Herbs = herbs ?? new List<FormulaHerbItem>
        {
            new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                FormulaId = formula.Id,
                HerbId = Guid.NewGuid(),
                HerbName = "黄芪",
                Dosage = 15,
                Unit = "g"
            }
        };

        return formula;
    }
}
```

### 7.3 TestMedicalCaseBuilder

```csharp
public static class TestMedicalCaseBuilder
{
    public static MedicalCase Create(
        Guid? id = null,
        Guid? patientId = null,
        Guid? userId = null,
        MedicalCaseStatus? status = null)
    {
        return new MedicalCase
        {
            Id = id ?? Guid.NewGuid(),
            CaseNumber = $"MC-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            PatientId = patientId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Status = status ?? MedicalCaseStatus.Draft,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

## 8. Mock 策略

### 8.1 SyncServiceTests Mock 设置

```csharp
public class SyncServiceTests
{
    private readonly Mock<ISyncApi> _syncApiMock;
    private readonly LocalDbContext _context;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _context = CreateInMemoryContext();
        _syncApiMock = new Mock<ISyncApi>();

        // 默认: API 返回成功
        _syncApiMock
            .Setup(x => x.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SyncMetadata>>.Success(new List<SyncMetadata>()));

        _sut = new SyncService(
            _syncApiMock.Object,
            _context,
            NullLogger<SyncService>.Instance);
    }
}
```

---

## 9. 验收标准

| 指标 | 目标 |
|------|------|
| LocalUserDataSource 测试数 | 15 |
| LocalFormulaDataSource 测试数 | 18 |
| LocalMedicalCaseDataSource 测试数 | 20 |
| SyncService 测试数 | 16 |
| 总测试数 | 120 |
| 全部测试通过 | 100% |

---

## 10. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | LocalUserDataSource 测试 (15个) | 30min |
| 2 | LocalFormulaDataSource 测试 (18个) | 40min |
| 3 | LocalMedicalCaseDataSource 测试 (20个) | 45min |
| 4 | SyncService 测试 (16个) | 45min |
| 5 | 编译验证和修复 | 20min |
| **总计** | | **~3h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
