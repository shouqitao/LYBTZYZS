# 测试设计方案 - LYBT.Module.Sync.Tests

## 1. 设计原则 (基于 Microsoft .NET 最佳实践)

### 1.1 测试命名规范
```
{MethodName}_{Scenario}_{ExpectedBehavior}
```

**示例**:
- `ComputeHerbChecksum_WithSameData_ShouldReturnSameChecksum`
- `UploadAsync_WithNewEntity_ShouldCreate`

### 1.2 AAA 模式
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - 准备测试数据
    var entity = CreateTestEntity();

    // Act - 执行单一操作
    var result = _sut.Method(entity);

    // Assert - 验证结果
    result.Should().NotBeNull();
}
```

### 1.3 测试分层原则
| 层级 | 职责 | Mock 范围 |
|------|------|----------|
| **单元测试** | 业务逻辑、算法、边界条件 | 全部依赖 Mock |
| **集成测试** | 组件协作、数据流、HTTP | 真实组件 |

---

## 2. 实体结构分析

### 2.1 Herb 实体
```
业务字段 (纳入Checksum):
├── Name (string, required, 1-100字符)
├── PinYinCode (string?, 50字符)
├── Category (string?, 50字符)
├── Origin (string?, 100字符)
├── Spec (string?, 100字符)
├── Unit (string, required, 10字符)
├── Price (decimal, 18,2)
├── CostPrice (decimal?, 18,2)
├── Effect (string?, 500字符)
├── Usage (string?, 500字符)
├── Remark (string?, 500字符)
├── Status (CommonStatus: Disabled=0, Enabled=1)
└── IsDeleted (bool)

审计字段 (排除Checksum):
├── CreatedAt, UpdatedAt
├── CreatedBy, UpdatedBy
└── RowVersion
```

### 2.2 Patient 实体
```
业务字段:
├── Name, PinYinCode, Gender (Unknown=0, Male, Female)
├── MaritalStatus (int), BirthDate (DateTime?)
├── IdType (int), IdNumber (string?, 敏感)
├── PhoneNumber (string?, 敏感), Address (string?, 敏感)
├── AllergyHistory, MedicalHistory (敏感)
├── BloodType (int)
├── EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation
├── Status, DisableReason
├── LastVisitTime, VisitCount
└── IsDeleted
```

### 2.3 Formula 实体
```
业务字段:
├── Name, Effect, Indication, Usage, Remark, Property
├── Category, Status, IsShared
├── ValidationStatus (Draft, Validated)
├── FormulaType (Classic=1, Experience=2)
├── UserId
├── IsDeleted
└── Herbs (集合):
    ├── HerbId, HerbName
    ├── Dosage, Unit
    └── Remark
```

### 2.4 枚举值
```csharp
// FormulaType (只有两个值!)
Classic = 1     // 经典方
Experience = 2  // 经验方

// CommonStatus
Disabled = 0
Enabled = 1

// Gender
Unknown = 0
Male = 1  // 假设
Female = 2  // 假设
```

---

## 3. ChecksumHelperTests 设计

### 3.1 测试矩阵

| 测试类别 | 测试数量 | 描述 |
|----------|----------|------|
| **Herb 算法正确性** | 12 | 每个业务字段变更测试 |
| **Herb 审计字段排除** | 4 | 审计字段变更不影响 |
| **Patient 算法正确性** | 15 | 所有业务字段 |
| **Patient 审计字段排除** | 1 | 综合测试 |
| **Formula 算法正确性** | 10 | 含 Herbs 集合测试 |
| **边界条件** | 10 | Null/Empty/特殊字符/数值精度 |
| **类型路由** | 4 | 有效/无效类型 |
| **总计** | **~56** | |

### 3.2 详细测试清单

#### A. Herb Checksum 测试 (16个)

**算法正确性 (12个)**:
```
ComputeHerbChecksum_WithSameData_ShouldReturnSameChecksum
ComputeHerbChecksum_MultipleCallsSameData_ShouldReturnSame (确定性)
ComputeHerbChecksum_WithDifferentName_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentPinYinCode_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentCategory_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentOrigin_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentSpec_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentUnit_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentPrice_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentCostPrice_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentEffect_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentUsage_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentRemark_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentStatus_ShouldReturnDifferent
ComputeHerbChecksum_WithDifferentIsDeleted_ShouldReturnDifferent
```

**审计字段排除 (4个)**:
```
ComputeHerbChecksum_WithDifferentCreatedAt_ShouldReturnSame
ComputeHerbChecksum_WithDifferentUpdatedAt_ShouldReturnSame
ComputeHerbChecksum_WithDifferentCreatedBy_ShouldReturnSame
ComputeHerbChecksum_WithDifferentUpdatedBy_ShouldReturnSame
```

#### B. Patient Checksum 测试 (16个)

**算法正确性 (15个)**:
```
ComputePatientChecksum_WithSameData_ShouldReturnSameChecksum
ComputePatientChecksum_WithDifferentName_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentPinYinCode_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentGender_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentBirthDate_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentIdNumber_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentPhoneNumber_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentAddress_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentAllergyHistory_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentMedicalHistory_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentStatus_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentDisableReason_ShouldReturnDifferent
ComputePatientChecksum_WithDifferentIsDeleted_ShouldReturnDifferent
```

**审计字段排除 (1个)**:
```
ComputePatientChecksum_WithDifferentAuditFields_ShouldReturnSame
```

#### C. Formula Checksum 测试 (14个)

**算法正确性 (10个)**:
```
ComputeFormulaChecksum_WithSameData_ShouldReturnSameChecksum
ComputeFormulaChecksum_WithDifferentName_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentEffect_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentIndication_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentUsage_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentRemark_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentProperty_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentCategory_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentFormulaType_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentStatus_ShouldReturnDifferent
```

**Herbs 集合测试 (4个)**:
```
ComputeFormulaChecksum_WithDifferentHerbOrder_ShouldReturnSame (按HerbId排序)
ComputeFormulaChecksum_WithDifferentHerbDosage_ShouldReturnDifferent
ComputeFormulaChecksum_WithDifferentHerbRemark_ShouldReturnDifferent
ComputeFormulaChecksum_WithLargeHerbsList_ShouldHandle (100个药材)
```

#### D. 边界条件测试 (10个)

**Null/Empty**:
```
ComputeHerbChecksum_WithNullName_ShouldNotThrow
ComputeHerbChecksum_WithEmptyName_ShouldReturnDifferentFromNonEmpty
ComputeFormulaChecksum_WithNullHerbs_ShouldNotThrow
ComputeFormulaChecksum_WithEmptyHerbs_ShouldNotThrow
```

**特殊字符**:
```
ComputeHerbChecksum_WithSpecialCharacters_ShouldHandle (括号、换行)
ComputeHerbChecksum_WithUnicodeCharacters_ShouldHandle (繁体中文)
```

**数值精度**:
```
ComputeHerbChecksum_WithSameDecimalValue_ShouldReturnSame (50.00 = 50.000000)
ComputeHerbChecksum_WithSmallDecimalDifference_ShouldReturnDifferent
```

**日期边界**:
```
ComputePatientChecksum_WithDateTimeBoundaries_ShouldHandle (Min/Max)
```

#### E. 类型路由测试 (4个)

```
ComputeChecksum_WithHerbType_ShouldNotThrow
ComputeChecksum_WithPatientType_ShouldNotThrow
ComputeChecksum_WithFormulaType_ShouldNotThrow
ComputeChecksum_WithInvalidType_ShouldThrowArgumentException
```

---

## 4. SyncServiceTests 设计

### 4.1 测试矩阵

| API 方法 | 现有测试 | 目标测试 | 新增 |
|----------|----------|----------|------|
| GetSupportedEntityTypes | 1 | 2 | +1 |
| GetMetadataAsync | 3 | 5 | +2 |
| CompareAsync | 5 | 8 | +3 |
| **UploadAsync** | **0** | **10** | **+10** |
| DownloadAsync | 3 | 5 | +2 |
| DeleteAsync | 7 | 10 | +3 |
| **总计** | **19** | **40** | **+21** |

### 4.2 详细测试清单

#### A. GetSupportedEntityTypes (2个)
```
GetSupportedEntityTypes_ShouldReturnThreeTypes (Herb, Patient, Formula)
GetSupportedEntityTypes_ShouldBeIdempotent
```

#### B. GetMetadataAsync (5个)
```
GetMetadataAsync_WithHerbType_ShouldReturnMetadata
GetMetadataAsync_WithPatientType_ShouldReturnMetadata
GetMetadataAsync_WithFormulaType_ShouldReturnMetadata
GetMetadataAsync_WithInvalidType_ShouldReturnFailure
GetMetadataAsync_WithEmptyDatabase_ShouldReturnEmptyList
```

#### C. CompareAsync (8个)
```
CompareAsync_WithLocalOnlyEntity_ShouldReturnLocalOnlyDiff
CompareAsync_WithServerOnlyEntity_ShouldReturnServerOnlyDiff
CompareAsync_WithModifiedEntity_ShouldReturnModifiedDiff
CompareAsync_WithIdenticalChecksum_ShouldReturnNoDiff
CompareAsync_WithMixedDiffs_ShouldReturnAllTypes
CompareAsync_WithEmptyLocalEntities_ShouldReturnAllServerOnly
CompareAsync_WithInvalidType_ShouldReturnFailure
CompareAsync_WithDeletedServerEntity_ShouldIncludeInComparison
```

#### D. UploadAsync (10个) - **Critical 新增**
```
UploadAsync_WithNewHerb_ShouldCreate
UploadAsync_WithExistingHerb_OverwriteTrue_ShouldUpdate
UploadAsync_WithExistingHerb_OverwriteFalse_ShouldReturnConflict
UploadAsync_WithNewPatient_ShouldCreate
UploadAsync_WithNewFormula_ShouldCreateWithHerbs
UploadAsync_WithExistingFormula_OverwriteTrue_ShouldUpdateHerbs
UploadAsync_WithBatchEntities_ShouldProcessAll
UploadAsync_WithInvalidJson_ShouldReturnError
UploadAsync_WithInvalidEntityType_ShouldReturnFailure
UploadAsync_WithMixedResults_ShouldReportCorrectly
```

#### E. DownloadAsync (5个)
```
DownloadAsync_WithExistingIds_ShouldReturnEntities
DownloadAsync_WithNonExistentIds_ShouldReturnEmpty
DownloadAsync_WithMixedIds_ShouldReturnExistingOnly
DownloadAsync_WithFormulaId_ShouldIncludeHerbs
DownloadAsync_WithInvalidType_ShouldReturnFailure
```

#### F. DeleteAsync (10个)
```
DeleteAsync_HerbWithNoReferences_ShouldSoftDelete
DeleteAsync_HerbWithReferences_ShouldReject
DeleteAsync_PatientWithNoReferences_ShouldSoftDelete
DeleteAsync_PatientWithMedicalCases_ShouldReject
DeleteAsync_Formula_ShouldSoftDeleteDirectly
DeleteAsync_AlreadyDeletedEntity_ShouldReject
DeleteAsync_BatchWithMixedResults_ShouldReportCorrectly
DeleteAsync_WithEmptyIds_ShouldReturnFailure
DeleteAsync_WithInvalidType_ShouldReturnFailure
DeleteAsync_NonExistentEntity_ShouldIgnore
```

---

## 5. 测试数据工厂设计

### 5.1 TestDataBuilder 模式

```csharp
public static class TestHerbBuilder
{
    public static Herb Create(
        string? name = null,
        decimal? price = null,
        CommonStatus? status = null)
    {
        return new Herb
        {
            Id = Guid.NewGuid(), // 每次新 ID
            Name = name ?? "测试药材",
            PinYinCode = "CSYC",
            Category = "补气药",
            Origin = "测试产地",
            Spec = "统货",
            Unit = "g",
            Price = price ?? 50m,
            CostPrice = 30m,
            Effect = "测试功效",
            Usage = "测试用法",
            Remark = "测试备注",
            Status = status ?? CommonStatus.Enabled,
            IsDeleted = false
        };
    }
}
```

### 5.2 测试数据原则

1. **每个测试独立数据**: 使用 `Guid.NewGuid()` 避免干扰
2. **Builder 模式**: 只覆盖测试关注的字段
3. **明确的默认值**: 所有字段都有合理默认
4. **Theory + InlineData**: 参数化相似测试

---

## 6. Mock 策略

### 6.1 SyncServiceTests Mock 设置

```csharp
public class SyncServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHerbService> _herbServiceMock;
    private readonly Mock<IPatientService> _patientServiceMock;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _dbContext = CreateInMemoryContext();
        _herbServiceMock = new Mock<IHerbService>();
        _patientServiceMock = new Mock<IPatientService>();

        // 默认设置: 无引用
        _herbServiceMock
            .Setup(x => x.CheckReferenceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _patientServiceMock
            .Setup(x => x.CheckReferenceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _sut = new SyncService(_dbContext, _herbServiceMock.Object, _patientServiceMock.Object, ...);
    }
}
```

### 6.2 Mock 原则

1. **It.IsAny<T>()**: 默认行为覆盖所有情况
2. **特定设置覆盖默认**: 需要特殊行为时单独设置
3. **Verify 验证交互**: 确认服务被正确调用

---

## 7. 执行计划

### Phase 1: ChecksumHelperTests 重写 (1-2h)
1. 删除现有测试
2. 按上述清单重写 ~56 个测试
3. 编译验证

### Phase 2: SyncServiceTests 补充 (2-3h)
1. 保留现有有效测试
2. 新增 UploadAsync 10 个测试 (Critical)
3. 补充其他方法测试 +11 个
4. 编译验证

### Phase 3: 集成测试优化 (1h)
1. 审查 SyncControllerIntegrationTests
2. 移除与单元测试重复的逻辑测试
3. 专注端到端流程测试

---

## 8. 验收标准

| 指标 | 目标 |
|------|------|
| ChecksumHelperTests 测试数 | ~56 |
| SyncServiceTests 测试数 | ~40 |
| 全部测试通过 | 100% |
| 编译警告 | 0 |
| UploadAsync 覆盖 | 100% |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待用户确认后执行*
