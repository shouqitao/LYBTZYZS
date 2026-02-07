# Research Findings: 测试体系全面重写计划

## 1. 测试项目现状统计

### 1.1 Server 模块单元测试 (8个项目, 228个测试)

| 项目 | 现有测试 | 预估目标 | 差距 | 优先级 |
|------|----------|----------|------|--------|
| LYBT.Module.Auth.Tests | 67 | 80 | +13 | P2 |
| LYBT.Module.Patients.Tests | 36 | 50 | +14 | P2 |
| LYBT.Module.Sync.Tests | 35 | 60 | **+25** | **P0** |
| LYBT.Module.MedicalCase.Tests | 32 | 50 | +18 | P3 |
| LYBT.Module.Herbs.Tests | 22 | 40 | +18 | P1 |
| LYBT.Module.Formula.Tests | 22 | 40 | +18 | P2 |
| LYBT.Module.Users.Tests | 14 | 30 | +16 | P2 |
| LYBT.WebAPI.Tests | ? | 30 | ? | P3 |

### 1.2 Desktop 模块单元测试 (11个项目, 553个测试)

| 项目 | 现有测试 | 预估目标 | 差距 | 优先级 |
|------|----------|----------|------|--------|
| LYBT.Desktop.Shell.Tests | 136 | 150 | +14 | P3 |
| LYBT.Desktop.Foundation.Tests | 120 | 130 | +10 | P2 |
| LYBT.Desktop.MedicalCase.Tests | 117 | 130 | +13 | P3 |
| LYBT.Desktop.Infrastructure.Tests | 67 | 80 | +13 | P2 |
| LYBT.Desktop.LocalData.Tests | 47 | 60 | +13 | P1 |
| LYBT.Desktop.Formula.Tests | 24 | 35 | +11 | P2 |
| LYBT.Desktop.Herbs.Tests | 18 | 30 | +12 | P2 |
| LYBT.Desktop.Auth.Tests | 10 | 25 | +15 | P2 |
| LYBT.Desktop.Patients.Tests | 7 | 25 | **+18** | **P1** |
| LYBT.Desktop.Models.Tests | 6 | 15 | +9 | P3 |
| LYBT.Desktop.Users.Tests | 1 | 20 | **+19** | **P1** |

### 1.3 Shared 单元测试 (5个项目, 284个测试)

| 项目 | 现有测试 | 预估目标 | 差距 | 优先级 |
|------|----------|----------|------|--------|
| LYBT.Shared.Utilities.Tests | 184 | 200 | +16 | P3 |
| LYBT.Shared.ExceptionHandling.Tests | 53 | 60 | +7 | P3 |
| LYBT.Shared.Configuration.Tests | 43 | 50 | +7 | P3 |
| LYBT.Shared.Models.Tests | 4 | 20 | +16 | P2 |
| LYBT.Shared.Validators.Tests | 0 | 30 | **+30** | **P1** |

### 1.4 架构测试 (58个测试)

| 项目 | 现有测试 | 预估目标 | 差距 | 优先级 |
|------|----------|----------|------|--------|
| LYBT.ArchTests | 58 | 70 | +12 | P3 |
| LYBT.Server.ArchTests | ? | 30 | ? | P3 |

---

## 2. 重写优先级分析

### P0 - Critical (立即处理)

| 项目 | 原因 |
|------|------|
| **LYBT.Module.Sync.Tests** | 缺少 UploadAsync 测试，核心同步功能无覆盖 |

### P1 - High (优先处理)

| 项目 | 原因 |
|------|------|
| LYBT.Module.Herbs.Tests | 基础数据模块，被多处依赖 |
| LYBT.Desktop.LocalData.Tests | 本地模式核心，与 Sync 相关 |
| LYBT.Desktop.Patients.Tests | 仅 7 个测试，严重不足 |
| LYBT.Desktop.Users.Tests | 仅 1 个测试，基本为空 |
| LYBT.Shared.Validators.Tests | 0 个测试，验证器无覆盖 |

### P2 - Medium (后续处理)

| 项目 | 原因 |
|------|------|
| LYBT.Module.Auth.Tests | 已有 67 个测试，需完善 |
| LYBT.Module.Patients.Tests | 已有 36 个测试，需完善 |
| LYBT.Module.Formula.Tests | 已有 22 个测试，需完善 |
| LYBT.Module.Users.Tests | 已有 14 个测试，需完善 |
| LYBT.Desktop.Infrastructure.Tests | 已有 67 个测试，需完善 |
| LYBT.Desktop.Foundation.Tests | 已有 120 个测试，需完善 |
| LYBT.Shared.Models.Tests | 仅 4 个测试，DTO 验证不足 |

### P3 - Low (稳定后处理)

| 项目 | 原因 |
|------|------|
| LYBT.Module.MedicalCase.Tests | 已有 32 个测试 |
| LYBT.Desktop.Shell.Tests | 已有 136 个测试 |
| LYBT.Desktop.MedicalCase.Tests | 已有 117 个测试 |
| LYBT.Shared.Utilities.Tests | 已有 184 个测试 |
| 架构测试 | 已有 58 个测试 |

---

## 3. 工作量估算

### 3.1 按优先级估算

| 优先级 | 项目数 | 新增测试数 | 预估工时 |
|--------|--------|------------|----------|
| P0 | 1 | ~25 | 2-3h |
| P1 | 5 | ~100 | 8-10h |
| P2 | 8 | ~100 | 8-10h |
| P3 | 10+ | ~100 | 8-10h |
| **总计** | **24+** | **~325** | **26-33h** |

### 3.2 建议执行顺序

**Week 1 - Critical + High**
1. LYBT.Module.Sync.Tests (P0) - 2-3h
2. LYBT.Shared.Validators.Tests (P1) - 2h
3. LYBT.Module.Herbs.Tests (P1) - 2h
4. LYBT.Desktop.LocalData.Tests (P1) - 2h
5. LYBT.Desktop.Users.Tests (P1) - 1h
6. LYBT.Desktop.Patients.Tests (P1) - 1h

**Week 2 - Medium**
7. Server 模块 (P2) - Auth, Patients, Formula, Users
8. Desktop 模块 (P2) - Infrastructure, Foundation, Auth

**Week 3 - Low + 优化**
9. 剩余 P3 模块
10. 集成测试优化
11. 架构测试完善
12. 测试覆盖率报告

---

## 4. SyncServiceTests 详细重写计划

### 4.1 当前测试 (35个)

| 类别 | 数量 | 覆盖 |
|------|------|------|
| GetSupportedEntityTypes | 1 | 完整 |
| GetMetadataAsync | 3 | 基本 |
| CompareAsync | 5 | 基本 |
| DownloadAsync | 3 | 基本 |
| DeleteAsync | 7 | 较好 |
| **UploadAsync** | **0** | **无** |
| ChecksumHelper | 16 | 60% |

### 4.2 需新增测试 (~25个)

**UploadAsync 测试 (10个)** - Critical
```
UploadAsync_WithNewHerb_ShouldCreateEntity
UploadAsync_WithExistingHerb_OverwriteTrue_ShouldUpdate
UploadAsync_WithExistingHerb_OverwriteFalse_ShouldReturnConflict
UploadAsync_WithInvalidJson_ShouldReturnError
UploadAsync_WithBatchEntities_ShouldProcessAll
UploadAsync_WithNewPatient_ShouldCreateEntity
UploadAsync_WithNewFormula_ShouldCreateWithHerbs
UploadAsync_WithExistingFormula_ShouldUpdateHerbs
UploadAsync_WithInvalidEntityType_ShouldReturnFailure
UploadAsync_WithMixedResults_ShouldReportCorrectly
```

**CompareAsync 补充 (5个)**
```
CompareAsync_WithMixedDiffs_ShouldReturnAllTypes
CompareAsync_WithLargeBatch_ShouldHandleEfficiently
CompareAsync_WithNullLocalEntities_ShouldHandleGracefully
CompareAsync_WithDeletedEntities_ShouldIncludeInComparison
CompareAsync_TimeStampComparison_ShouldBeCorrect
```

**ChecksumHelper 补充 (10个)**
```
ComputeHerbChecksum_WithAllFieldsChanged_ShouldReturnDifferent
ComputeHerbChecksum_WithNullFields_ShouldNotThrow
ComputeHerbChecksum_WithSpecialCharacters_ShouldHandle
ComputePatientChecksum_WithAllFieldsChanged_ShouldReturnDifferent
ComputePatientChecksum_WithDateBoundaries_ShouldHandle
ComputeFormulaChecksum_WithLargeHerbsList_ShouldHandle
ComputeFormulaChecksum_WithHerbRemark_ShouldAffectChecksum
ComputeChecksum_WithNullEntity_ShouldThrow
ComputeChecksum_MultipleCallsSameData_ShouldReturnSame
ComputeChecksum_DecimalPrecision_ShouldBeConsistent
```

---

## 5. ChecksumHelperTests 逻辑问题修复

### 5.1 问题 1: First() 调用
```csharp
// 当前代码
formula2.Herbs!.First().Dosage = 20;

// 修复方案
var targetHerb = formula2.Herbs!.Single(h => h.HerbId == herbId1);
targetHerb.Dosage = 20;
```

### 5.2 问题 2: 硬编码 Id
```csharp
// 当前代码
Id = Guid.Parse("11111111-1111-1111-1111-111111111111")

// 修复方案 - 使用 Builder
public class HerbBuilder
{
    private Guid _id = Guid.NewGuid();
    public HerbBuilder WithId(Guid id) { _id = id; return this; }
    public Herb Build() => new Herb { Id = _id, ... };
}
```

### 5.3 问题 3: 缺少 Null Entity 测试
```csharp
// 新增测试
[Fact]
public void ComputeHerbChecksum_WithNullEntity_ShouldThrowArgumentNullException()
{
    // Act
    var act = () => ChecksumHelper.ComputeHerbChecksum(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>();
}
```

---

## 6. 测试基础设施改进

### 6.1 新增 TestDataBuilder
```
tests/TestConfiguration/TestDataBuilders/
├── HerbBuilder.cs
├── PatientBuilder.cs
├── FormulaBuilder.cs
├── UserBuilder.cs
└── SyncInputBuilder.cs
```

### 6.2 新增 MockFactory
```
tests/TestConfiguration/Mocks/
├── MockServiceFactory.cs
└── MockRepositoryFactory.cs
```

---

---

## 7. Phase 3.1 执行发现

### 7.1 现有测试分析

**ChecksumHelperTests** (35个测试):
- 质量较好，遵循 AAA 模式
- **问题**: 第 441 行使用 `FormulaType.Custom`，但实际枚举只有 `Classic=1, Experience=2`

**SyncServiceTests** (19个测试):
- GetSupportedEntityTypes: 1
- GetMetadataAsync: 3
- CompareAsync: 5
- DownloadAsync: 3
- DeleteAsync: 7
- **UploadAsync: 0** (Critical 缺失)

### 7.2 SyncService.UploadAsync 逻辑

```csharp
// 输入: SyncUploadInputDto { EntityType, Entities(JsonElement[]), OverwriteConflicts }
// 输出: SyncUploadResultDto { SuccessCount, ConflictCount, ErrorCount, Results }

// 业务逻辑:
1. 验证 EntityType
2. 遍历 Entities:
   - 反序列化 JSON
   - 如果已存在 && !overwriteConflicts → IsConflict
   - 如果已存在 && overwriteConflicts → 覆盖更新
   - 如果不存在 → 新增
3. SaveChangesAsync
```

### 7.3 需修复的测试问题

| 位置 | 问题 | 修复方案 |
|------|------|----------|
| ChecksumHelperTests:441 | `FormulaType.Custom` 不存在 | 改为 `FormulaType.Experience` |

---

---

## 8. Phase 4 Desktop P1 模块探索发现

### 8.1 LYBT.Desktop.Users.Tests (最严重不足)

**现状**: 仅 1 个占位符测试，实际功能测试为 0

**被测对象**:
- `UserRepository`: GetPaged/ById/Create/Update/Delete/ResetPassword/ChangeStatus
- `UserService`: 业务逻辑封装，返回 (success, error) 元组
- `UserListViewModel`: 列表展示和交互
- `UserMapper`: Entity ↔ DTO 映射
- `UserPasswordHandler/UserStatusHandler`: 处理器

**需新增测试**: 19 个
```
UserRepository (8-10个)
├── GetPagedAsync_ReturnsPagedResult
├── GetByIdAsync_ExistingUser_Returns
├── GetByIdAsync_NonExisting_ReturnsNull
├── CreateAsync_ValidUser_GeneratesId
├── UpdateAsync_ExistingUser_Updates
├── DeleteAsync_SetsIsDeleted
├── ResetPasswordAsync_SetsNewPassword
└── ChangeStatusAsync_TogglesStatus

UserService (6-8个)
├── CreateAsync_ValidInput_ReturnsSuccess
├── CreateAsync_DuplicateUsername_ReturnsError
├── UpdateAsync_ValidInput_ReturnsSuccess
├── DeleteAsync_RemovesUser
├── ResetPasswordAsync_GeneratesNewPassword
└── ExceptionHandling_LogsAndReturns

UserListViewModel (3-5个)
├── Initialize_LoadsUserList
├── Search_FiltersByKeyword
└── Pagination_WorksCorrectly
```

### 8.2 LYBT.Desktop.Patients.Tests (中等成熟)

**现状**: 7 个测试 (6 个展示模型 + 1 占位符)

**已覆盖**:
- `PatientDetailDisplayModel`: AgeDisplay, GenderDisplay, Summary, VisitInfo

**被测对象**:
- `PatientRepository`: CRUD + 分页 + 关系查询
- `PatientService`: 业务逻辑 + 验证 + 异常处理
- `PatientListViewModel`: 列表交互
- `PatientMapper`: Entity ↔ DTO

**需新增测试**: 18 个
```
PatientRepository (8-10个)
├── GetPagedAsync_ReturnsPagedResult
├── GetPagedAsync_Filtering_ByKeyword
├── GetByIdAsync_ExistingPatient_Returns
├── CreateAsync_ValidPatient_GeneratesId
├── UpdateAsync_ExistingPatient_Updates
├── DeleteAsync_SetsIsDeleted
├── GetWithRelationsAsync_IncludesMedicalCases
└── BatchOperations_HandleErrors

PatientService (6-8个)
├── CreateAsync_ValidInput_ReturnsSuccess
├── CreateAsync_InvalidInput_ReturnsError
├── UpdateAsync_ReturnsSuccess
├── DeleteAsync_CascadeWorks
└── ExceptionHandling_SafeMessage

PatientListViewModel (2-3个)
├── Initialize_LoadsPatientList
├── Search_FiltersByKeyword
└── SelectPatient_NavigatesToDetail
```

### 8.3 LYBT.Desktop.LocalData.Tests (最成熟)

**现状**: 47 个测试
- `LocalAuthServiceTests`: 16 个 (完善)
- `LocalHerbDataSourceTests`: 16 个 (完善)
- `LocalPatientDataSourceTests`: 15 个 (完善)

**优势**:
- 命名规范一致
- 严格 AAA 模式
- SQLite 内存数据库隔离
- FluentAssertions

**缺失**:
- `LocalFormulaDataSource`: 0 个测试
- `LocalMedicalCaseDataSource`: 0 个测试
- `LocalUserDataSource`: 0 个测试
- `SyncService/ChecksumHelper`: 0 个测试

**需新增测试**: 13 个
```
LocalFormulaDataSource (8-10个)
├── GetByIdAsync_ExistingFormula_Returns
├── GetWithHerbsAsync_IncludesRelatedHerbs
├── GetPagedAsync_FiltersByKeyword
├── GetPagedAsync_Pagination_Works
├── CreateAsync_WithHerbs_CreatesRelationships
├── UpdateAsync_ValidFormula_Updates
├── DeleteAsync_SoftDelete_Works
└── RestoreAsync_UndeleteWorks

SyncService/ChecksumHelper (3-5个)
├── ChecksumHelper_ComputeHerbChecksum
├── ChecksumHelper_VerifyConsistency
└── SyncService_IncrementalSync
```

### 8.4 执行优先级建议

| 顺序 | 模块 | 原因 |
|------|------|------|
| 1 | Users | 测试覆盖最低 (5%), 完全从零开始 |
| 2 | Patients | Repository/Service 层完全缺失 |
| 3 | LocalData | 已有良好基础，仅需补充 |

*Updated: 2026-02-05*
