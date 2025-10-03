# Patients 模块测试计划

**日期**: 2025-10-03
**相关Issue**: #864 - Phase 2.1
**目标覆盖率**: 23% → 80%
**预计工作量**: 1周

---

## 模块概览

### 源代码结构
- **Services**: `PatientService.cs` (6个方法)
- **Repositories**: `PatientRepository.cs` (9个方法)
- **Validators**: `PatientCreateDtoValidator.cs`, `PatientUpdateDtoValidator.cs`
- **Mapping**: `PatientMappingProfile.cs`
- **Interfaces**: `IPatientService.cs`, `IPatientRepository.cs`

### 现有测试
- `PatientServiceTests.cs` (基础测试)
- `PatientMappingProfileTests.cs` (AutoMapper测试)
- `PatientValidatorTests.cs` (验证器测试)

---

## 测试计划清单

### 1️⃣ PatientService 测试 (优先级: 🔴 高)

**测试文件**: `PatientServiceTests.cs`

#### 1.1 CreateAsync 方法

- [ ] `CreateAsync_WithValidData_ReturnsSuccessResult`
  - 验证: 正常创建患者返回成功结果
  - Mock: Repository.AddAsync, Mapper.Map

- [ ] `CreateAsync_WithNullDto_ThrowsArgumentNullException`
  - 验证: null输入抛出异常

- [ ] `CreateAsync_WithDuplicatePhoneNumber_ReturnsFailureResult`
  - 验证: 重复手机号返回失败
  - Mock: Repository.PhoneNumberExistsAsync 返回 true

- [ ] `CreateAsync_WhenRepositoryFails_ReturnsFailureResult`
  - 验证: Repository异常时返回失败
  - Mock: Repository.AddAsync 抛出异常

#### 1.2 GetByIdAsync 方法

- [ ] `GetByIdAsync_WithExistingId_ReturnsPatientDto`
  - 验证: 存在的ID返回患者DTO
  - Mock: Repository.GetByIdAsync 返回实体

- [ ] `GetByIdAsync_WithNonExistentId_ReturnsNull`
  - 验证: 不存在的ID返回null
  - Mock: Repository.GetByIdAsync 返回 null

- [ ] `GetByIdAsync_WithEmptyGuid_ThrowsArgumentException`
  - 验证: 空GUID抛出异常

#### 1.3 GetPagedAsync 方法

- [ ] `GetPagedAsync_WithValidParameters_ReturnsPagedResult`
  - 验证: 正常分页返回结果
  - Mock: Repository.GetPagedAsync

- [ ] `GetPagedAsync_WithPageSize0_ThrowsArgumentException`
  - 验证: 页大小为0抛出异常

- [ ] `GetPagedAsync_WithNegativePageNumber_ThrowsArgumentException`
  - 验证: 负页码抛出异常

- [ ] `GetPagedAsync_EmptyResult_ReturnsEmptyPage`
  - 验证: 无数据时返回空页

#### 1.4 UpdateAsync 方法

- [ ] `UpdateAsync_WithValidData_ReturnsSuccessResult`
  - 验证: 正常更新返回成功
  - Mock: Repository.GetByIdAsync, Repository.UpdateAsync

- [ ] `UpdateAsync_WithNonExistentId_ReturnsNotFoundResult`
  - 验证: 不存在的ID返回NotFound

- [ ] `UpdateAsync_WithNullDto_ThrowsArgumentNullException`
  - 验证: null输入抛出异常

- [ ] `UpdateAsync_WhenRepositoryFails_ReturnsFailureResult`
  - 验证: Repository异常时返回失败

#### 1.5 DeleteAsync 方法

- [ ] `DeleteAsync_WithExistingId_ReturnsSuccessResult`
  - 验证: 删除存在的患者返回成功

- [ ] `DeleteAsync_WithNonExistentId_ReturnsNotFoundResult`
  - 验证: 删除不存在的患者返回NotFound

- [ ] `DeleteAsync_WhenRepositoryFails_ReturnsFailureResult`
  - 验证: Repository异常时返回失败

#### 1.6 SearchAsync 方法

- [ ] `SearchAsync_WithKeyword_ReturnsMatchingPatients`
  - 验证: 关键字搜索返回匹配结果
  - Mock: Repository.SearchPatientsAsync

- [ ] `SearchAsync_WithEmptyKeyword_ReturnsAllPatients`
  - 验证: 空关键字返回所有患者

- [ ] `SearchAsync_NoMatches_ReturnsEmptyList`
  - 验证: 无匹配时返回空列表

**预计测试数**: 20个

---

### 2️⃣ PatientRepository 测试 (优先级: 🔴 高)

**测试文件**: `PatientRepositoryTests.cs`

#### 2.1 GetByNameAsync

- [ ] `GetByNameAsync_WithExactName_ReturnsPatient`
- [ ] `GetByNameAsync_WithNonExistentName_ReturnsNull`
- [ ] `GetByNameAsync_WithNullName_ThrowsArgumentNullException`

#### 2.2 GetPatientWithVisitsAsync

- [ ] `GetPatientWithVisitsAsync_WithVisits_IncludesRelatedData`
- [ ] `GetPatientWithVisitsAsync_WithoutVisits_ReturnsPatientOnly`
- [ ] `GetPatientWithVisitsAsync_NonExistentId_ReturnsNull`

#### 2.3 GetPatientSummariesAsync

- [ ] `GetPatientSummariesAsync_ReturnsProjectedData`
- [ ] `GetPatientSummariesAsync_WithPagination_ReturnsCorrectPage`
- [ ] `GetPatientSummariesAsync_EmptyDatabase_ReturnsEmptyList`

#### 2.4 SearchPatientsAsync

- [ ] `SearchPatientsAsync_ByName_ReturnsMatches`
- [ ] `SearchPatientsAsync_ByPhoneNumber_ReturnsMatches`
- [ ] `SearchPatientsAsync_ByIdNumber_ReturnsMatches`
- [ ] `SearchPatientsAsync_CaseInsensitive_ReturnsMatches`

#### 2.5 GetPatientsByIdsAsync

- [ ] `GetPatientsByIdsAsync_WithValidIds_ReturnsPatients`
- [ ] `GetPatientsByIdsAsync_WithEmptyList_ReturnsEmpty`
- [ ] `GetPatientsByIdsAsync_WithNonExistentIds_ReturnsEmpty`

#### 2.6 PhoneNumberExistsAsync

- [ ] `PhoneNumberExistsAsync_ExistingPhone_ReturnsTrue`
- [ ] `PhoneNumberExistsAsync_NonExistingPhone_ReturnsFalse`
- [ ] `PhoneNumberExistsAsync_ExcludingCurrentPatient_ReturnsCorrectResult`

#### 2.7 GetStatisticsAsync

- [ ] `GetStatisticsAsync_ReturnsCorrectCounts`
- [ ] `GetStatisticsAsync_EmptyDatabase_ReturnsZeroCounts`

#### 2.8 UpdateLastVisitDateAsync

- [ ] `UpdateLastVisitDateAsync_UpdatesDateAndCount`
- [ ] `UpdateLastVisitDateAsync_NonExistentId_DoesNothing`

**预计测试数**: 24个

---

### 3️⃣ Validator 测试 (优先级: 🟡 中)

#### 3.1 PatientCreateDtoValidator

**测试文件**: `PatientCreateDtoValidatorTests.cs`

- [ ] `Validate_WithValidData_PassesValidation`
- [ ] `Validate_WithEmptyName_FailsValidation`
- [ ] `Validate_WithNameTooLong_FailsValidation`
- [ ] `Validate_WithInvalidIdNumber_FailsValidation`
- [ ] `Validate_WithInvalidPhoneNumber_FailsValidation`
- [ ] `Validate_WithInvalidGender_FailsValidation`
- [ ] `Validate_WithFutureBirthDate_FailsValidation`

#### 3.2 PatientUpdateDtoValidator

**测试文件**: `PatientUpdateDtoValidatorTests.cs`

- [ ] `Validate_WithValidData_PassesValidation`
- [ ] `Validate_WithEmptyId_FailsValidation`
- [ ] `Validate_WithEmptyName_FailsValidation`
- [ ] `Validate_WithInvalidData_FailsValidation`

**预计测试数**: 11个

---

### 4️⃣ Mapping 测试 (优先级: 🟡 中)

**测试文件**: `PatientMappingProfileTests.cs` (已存在，需补充)

#### 补充测试

- [ ] `Map_PatientToDto_MapsAllProperties`
- [ ] `Map_PatientToDto_CalculatesAgeCorrectly`
- [ ] `Map_PatientToDto_WithNullBirthDate_ReturnsNullAge`
- [ ] `Map_CreateDtoToPatient_MapsAllProperties`
- [ ] `Map_UpdateDtoToPatient_MapsAllProperties`
- [ ] `Map_QuickCreateDtoToPatient_MapsEssentialProperties`
- [ ] `Map_PatientList_MapsAllItems`

**预计测试数**: 7个

---

## 测试数据准备

### 使用 Bogus 生成测试数据

```csharp
public class PatientTestData
{
    public static Faker<Patient> PatientFaker = new Faker<Patient>()
        .RuleFor(p => p.Id, f => Guid.NewGuid())
        .RuleFor(p => p.Name, f => f.Name.FullName())
        .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
        .RuleFor(p => p.BirthDate, f => f.Date.Past(80, DateTime.Now.AddYears(-18)))
        .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber("1##########"))
        .RuleFor(p => p.IdNumber, f => GenerateIdNumber())
        .RuleFor(p => p.Address, f => f.Address.FullAddress())
        .RuleFor(p => p.Status, f => CommonStatus.Enabled);

    public static PatientCreateDto CreateValidDto()
    {
        return new PatientCreateDto
        {
            Name = "张三",
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            PhoneNumber = "13800138000",
            IdNumber = "110101199001011234"
        };
    }
}
```

---

## Mock 对象配置

### PatientService 测试的 Mock 设置

```csharp
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PatientService>> _mockLogger;
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _mockRepo = new Mock<IPatientRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PatientService>>();
        _sut = new PatientService(_mockRepo.Object, _mockMapper.Object, _mockLogger.Object);
    }

    // 测试方法...
}
```

---

## 验收标准

- ✅ **行覆盖率**: ≥80%
- ✅ **分支覆盖率**: ≥70%
- ✅ **方法覆盖率**: 100% (所有public方法)
- ✅ **测试数量**: 62个测试
- ✅ **测试通过率**: 100%
- ✅ **遵循AAA模式**
- ✅ **使用FluentAssertions断言**
- ✅ **所有测试命名清晰**

---

## 实施步骤

1. **Step 1**: 创建测试文件骨架 (15分钟)
   - PatientServiceTests.cs
   - PatientRepositoryTests.cs
   - PatientCreateDtoValidatorTests.cs
   - PatientUpdateDtoValidatorTests.cs

2. **Step 2**: 实现 Service 层测试 (2小时)
   - 20个测试用例
   - Mock配置
   - 数据准备

3. **Step 3**: 实现 Repository 层测试 (2小时)
   - 24个测试用例
   - 使用InMemory数据库或Mock

4. **Step 4**: 实现 Validator 测试 (1小时)
   - 11个测试用例

5. **Step 5**: 补充 Mapping 测试 (30分钟)
   - 7个测试用例

6. **Step 6**: 运行并验证 (30分钟)
   - 执行所有测试
   - 生成覆盖率报告
   - 修复失败测试

---

## 依赖与前置条件

- ✅ xUnit 测试框架
- ✅ Moq Mock库
- ✅ FluentAssertions 断言库
- ✅ Bogus 测试数据生成
- ✅ AutoMapper
- ✅ Entity Framework Core (InMemory)

---

**下一步**: 开始实施 Step 1 - 创建测试文件骨架
