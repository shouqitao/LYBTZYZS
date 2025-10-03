# Consultation 模块测试计划

**日期**: 2025-10-03
**相关Issue**: #864 - Phase 2.4
**目标覆盖率**: 21% → 80%
**预计工作量**: 1周

---

## 模块概览

### 源代码结构
- **Services**:
  - `ConsultationService.cs` (10个方法)
  - `ConsultationQueryService.cs` (查询服务)
- **Repositories**: `ConsultationRepository.cs`
- **Validators**: `ConsultationCreateDtoValidator.cs`
- **Mapping**: `ConsultationMappingProfile.cs`
- **Interfaces**: `IConsultationService.cs`, `IConsultationQueryService.cs`, `IConsultationRepository.cs`

### 现有测试
- `ConsultationServiceTests.cs` (基础测试)
- `ConsultationMappingProfileTests.cs` (AutoMapper测试)
- `ConsultationValidatorTests.cs` (验证器测试)

---

## 测试计划清单

### 1️⃣ ConsultationService 测试 (优先级: 🔴 高)

**测试文件**: `ConsultationServiceTests.cs`

#### 1.1 CRUD 操作 (12个测试)

##### CreateAsync
- [ ] `CreateAsync_WithValidData_ReturnsSuccessResult`
  - 验证: 正常创建就诊记录返回成功
  - Mock: Repository.AddAsync, Mapper.Map

- [ ] `CreateAsync_WithNullDto_ThrowsArgumentNullException`
  - 验证: null输入抛出异常

- [ ] `CreateAsync_WithInvalidPatientId_ReturnsFailureResult`
  - 验证: 无效患者ID返回失败

- [ ] `CreateAsync_SetsConsultationNumber_Automatically`
  - 验证: 自动生成就诊编号

- [ ] `CreateAsync_SetsInitialStatus_ToInProgress`
  - 验证: 初始状态设置为进行中

##### GetByIdAsync
- [ ] `GetByIdAsync_WithExistingId_ReturnsConsultationDto`
  - 验证: 存在的ID返回就诊DTO
  - Mock: Repository.GetByIdAsync

- [ ] `GetByIdAsync_WithNonExistentId_ReturnsNull`
  - 验证: 不存在的ID返回null

- [ ] `GetByIdAsync_IncludesRelatedData`
  - 验证: 包含患者、医生等关联数据

##### UpdateAsync
- [ ] `UpdateAsync_WithValidData_ReturnsSuccessResult`
  - 验证: 正常更新返回成功

- [ ] `UpdateAsync_WithNonExistentId_ReturnsNotFoundResult`
  - 验证: 不存在的ID返回NotFound

- [ ] `UpdateAsync_PreservesConsultationNumber`
  - 验证: 更新时保留就诊编号

##### DeleteAsync
- [ ] `DeleteAsync_WithExistingId_ReturnsSuccessResult`
  - 验证: 删除存在的记录返回成功

- [ ] `DeleteAsync_WithNonExistentId_ReturnsNotFoundResult`
  - 验证: 删除不存在的记录返回NotFound

#### 1.2 分页查询 (6个测试)

##### GetPagedAsync
- [ ] `GetPagedAsync_WithValidParameters_ReturnsPagedResult`
  - 验证: 正常分页返回结果

- [ ] `GetPagedAsync_WithPatientIdFilter_ReturnsFilteredResults`
  - 验证: 按患者ID过滤返回正确结果

- [ ] `GetPagedAsync_WithDoctorIdFilter_ReturnsFilteredResults`
  - 验证: 按医生ID过滤返回正确结果

- [ ] `GetPagedAsync_WithStatusFilter_ReturnsFilteredResults`
  - 验证: 按状态过滤返回正确结果

- [ ] `GetPagedAsync_WithDateRangeFilter_ReturnsFilteredResults`
  - 验证: 按日期范围过滤返回正确结果

- [ ] `GetPagedAsync_EmptyResult_ReturnsEmptyPage`
  - 验证: 无数据时返回空页

#### 1.3 业务操作 (8个测试)

##### StartAsync
- [ ] `StartAsync_WithValidData_StartsConsultation`
  - 验证: 开始就诊更新状态为进行中

- [ ] `StartAsync_WithNonExistentId_ReturnsNotFoundResult`
  - 验证: 不存在的ID返回NotFound

- [ ] `StartAsync_WithAlreadyStarted_ReturnsFailureResult`
  - 验证: 已开始的就诊返回失败

- [ ] `StartAsync_UpdatesStartTime`
  - 验证: 更新开始时间

##### GetByMedicalCaseIdAsync
- [ ] `GetByMedicalCaseIdAsync_WithExistingCaseId_ReturnsConsultations`
  - 验证: 存在的病历ID返回就诊列表

- [ ] `GetByMedicalCaseIdAsync_WithNonExistentCaseId_ReturnsEmpty`
  - 验证: 不存在的病历ID返回空列表

- [ ] `GetByMedicalCaseIdAsync_OrdersByDateDescending`
  - 验证: 按日期降序排列

##### SearchAsync
- [ ] `SearchAsync_WithKeyword_ReturnsMatchingConsultations`
  - 验证: 关键字搜索返回匹配结果

- [ ] `SearchAsync_WithEmptyKeyword_ReturnsAllConsultations`
  - 验证: 空关键字返回所有就诊

**预计测试数**: 26个

---

### 2️⃣ ConsultationQueryService 测试 (优先级: 🟡 中)

**测试文件**: `ConsultationQueryServiceTests.cs`

#### 查询方法测试

- [ ] `GetTodayConsultationsAsync_ReturnsOnlyTodayRecords`
  - 验证: 返回今天的就诊记录

- [ ] `GetPatientConsultationHistoryAsync_ReturnsPatientHistory`
  - 验证: 返回患者就诊历史

- [ ] `GetDoctorConsultationsAsync_ReturnsDoctorConsultations`
  - 验证: 返回医生的就诊记录

- [ ] `GetConsultationStatisticsAsync_ReturnsCorrectStats`
  - 验证: 返回正确的统计数据

- [ ] `GetWaitingConsultationsAsync_ReturnsOnlyWaitingOnes`
  - 验证: 只返回等待中的就诊

- [ ] `GetInProgressConsultationsAsync_ReturnsOnlyInProgressOnes`
  - 验证: 只返回进行中的就诊

**预计测试数**: 6个

---

### 3️⃣ ConsultationRepository 测试 (优先级: 🔴 高)

**测试文件**: `ConsultationRepositoryTests.cs`

#### 3.1 基本查询

- [ ] `GetByConsultationNumberAsync_WithExistingNumber_ReturnsConsultation`
- [ ] `GetByConsultationNumberAsync_WithNonExistentNumber_ReturnsNull`
- [ ] `GetByPatientIdAsync_WithExistingPatientId_ReturnsConsultations`
- [ ] `GetByPatientIdAsync_OrdersByDateDescending`

#### 3.2 复杂查询

- [ ] `GetConsultationsWithDetailsAsync_IncludesAllRelations`
  - 验证: 包含患者、医生、病历等所有关联数据

- [ ] `GetByDateRangeAsync_WithValidRange_ReturnsMatchingRecords`
  - 验证: 日期范围查询返回匹配记录

- [ ] `GetByStatusAsync_ReturnsOnlyMatchingStatus`
  - 验证: 状态查询返回匹配状态的记录

- [ ] `GetTodayConsultationCountAsync_ReturnsCorrectCount`
  - 验证: 今日就诊数量统计正确

#### 3.3 搜索功能

- [ ] `SearchConsultationsAsync_ByPatientName_ReturnsMatches`
- [ ] `SearchConsultationsAsync_ByConsultationNumber_ReturnsMatches`
- [ ] `SearchConsultationsAsync_ByDoctorName_ReturnsMatches`
- [ ] `SearchConsultationsAsync_CaseInsensitive_ReturnsMatches`

#### 3.4 统计功能

- [ ] `GetConsultationCountByDoctorAsync_ReturnsCorrectCounts`
- [ ] `GetConsultationCountByDateAsync_ReturnsCorrectCounts`
- [ ] `GetAverageConsultationDurationAsync_ReturnsCorrectAverage`

**预计测试数**: 15个

---

### 4️⃣ Validator 测试 (优先级: 🟡 中)

**测试文件**: `ConsultationCreateDtoValidatorTests.cs`

#### ConsultationCreateDtoValidator

- [ ] `Validate_WithValidData_PassesValidation`
- [ ] `Validate_WithEmptyPatientId_FailsValidation`
- [ ] `Validate_WithEmptyDoctorId_FailsValidation`
- [ ] `Validate_WithFutureConsultationDate_FailsValidation`
- [ ] `Validate_WithInvalidStatus_FailsValidation`
- [ ] `Validate_WithChiefComplaintTooLong_FailsValidation`
- [ ] `Validate_WithNegativeConsultationFee_FailsValidation`

**预计测试数**: 7个

---

### 5️⃣ Mapping 测试 (优先级: 🟡 中)

**测试文件**: `ConsultationMappingProfileTests.cs` (已存在，需补充)

#### 补充测试

- [ ] `Map_ConsultationToDto_MapsAllProperties`
- [ ] `Map_ConsultationToDto_IncludesPatientInfo`
- [ ] `Map_ConsultationToDto_IncludesDoctorInfo`
- [ ] `Map_CreateDtoToConsultation_MapsAllProperties`
- [ ] `Map_UpdateDtoToConsultation_MapsAllProperties`
- [ ] `Map_ConsultationList_MapsAllItems`

**预计测试数**: 6个

---

## 测试数据准备

### 使用 Bogus 生成测试数据

```csharp
public class ConsultationTestData
{
    public static Faker<Consultation> ConsultationFaker = new Faker<Consultation>()
        .RuleFor(c => c.Id, f => Guid.NewGuid())
        .RuleFor(c => c.ConsultationNumber, f => $"C{DateTime.Now:yyyyMMdd}{f.Random.Number(1000, 9999)}")
        .RuleFor(c => c.PatientId, f => Guid.NewGuid())
        .RuleFor(c => c.DoctorId, f => Guid.NewGuid())
        .RuleFor(c => c.ConsultationDate, f => f.Date.Recent(7))
        .RuleFor(c => c.ChiefComplaint, f => f.Lorem.Sentence())
        .RuleFor(c => c.Status, f => ConsultationStatus.InProgress)
        .RuleFor(c => c.ConsultationFee, f => f.Finance.Amount(50, 500))
        .RuleFor(c => c.CreatedAt, f => f.Date.Past(1));

    public static ConsultationCreateDto CreateValidDto()
    {
        return new ConsultationCreateDto
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            ConsultationDate = DateTime.Now,
            ChiefComplaint = "头痛",
            ConsultationFee = 100
        };
    }
}
```

---

## Mock 对象配置

### ConsultationService 测试的 Mock 设置

```csharp
public class ConsultationServiceTests
{
    private readonly Mock<IConsultationRepository> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<ConsultationService>> _mockLogger;
    private readonly ConsultationService _sut;

    public ConsultationServiceTests()
    {
        _mockRepo = new Mock<IConsultationRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<ConsultationService>>();
        _sut = new ConsultationService(_mockRepo.Object, _mockMapper.Object, _mockLogger.Object);
    }
}
```

---

## 业务规则测试重点

### 必须覆盖的业务场景

1. **就诊编号生成**
   - 格式: C + 日期 + 序号
   - 唯一性保证
   - 自动递增

2. **状态转换**
   - 等待 → 进行中 → 已完成
   - 状态转换规则验证
   - 非法状态转换拒绝

3. **费用计算**
   - 挂号费
   - 诊疗费
   - 总费用计算

4. **时间管理**
   - 就诊开始时间
   - 就诊结束时间
   - 就诊时长计算

5. **关联数据完整性**
   - 患者存在性验证
   - 医生存在性验证
   - 病历关联

---

## 验收标准

- ✅ **行覆盖率**: ≥80%
- ✅ **分支覆盖率**: ≥70%
- ✅ **方法覆盖率**: 100% (所有public方法)
- ✅ **测试数量**: 60个测试
- ✅ **测试通过率**: 100%
- ✅ **遵循AAA模式**
- ✅ **使用FluentAssertions断言**
- ✅ **业务规则全覆盖**

---

## 实施步骤

1. **Step 1**: 创建测试文件骨架 (15分钟)
   - ConsultationServiceTests.cs
   - ConsultationQueryServiceTests.cs
   - ConsultationRepositoryTests.cs
   - ConsultationCreateDtoValidatorTests.cs

2. **Step 2**: 实现 Service CRUD 测试 (1.5小时)

3. **Step 3**: 实现 Service 分页查询测试 (1小时)

4. **Step 4**: 实现 Service 业务操作测试 (1小时)

5. **Step 5**: 实现 QueryService 测试 (1小时)

6. **Step 6**: 实现 Repository 测试 (2小时)

7. **Step 7**: 实现 Validator 测试 (45分钟)

8. **Step 8**: 补充 Mapping 测试 (30分钟)

9. **Step 9**: 运行并验证 (30分钟)

---

**下一步**: 开始实施 Step 1 - 创建测试文件骨架
