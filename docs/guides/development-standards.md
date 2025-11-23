# LYBTZYZS开发规范（Development Standards）

**项目**: 凌隐宝堂中医诊所管理系统（LYBTZYZS）
**版本**: v1.0
**最后更新**: 2025-11-22
**维护者**: Claude Code

---

## 目录

1. [用户上下文传递规范](#1-用户上下文传递规范)
2. [代码规范（待补充）](#2-代码规范)
3. [测试规范（待补充）](#3-测试规范)

---

## 1. 用户上下文传递规范

> **架构决策**: [ADR-001: 用户上下文传递模式](../architecture/decisions/ADR-001-user-context-propagation-pattern.md)
>
> **制定背景**: Epic #2210 P0 Bug修复发现MedicalCase等模块存在用户上下文传递缺失问题，导致审计字段丢失。

### 1.1 核心原则

**显式传递 > 隐式依赖**

在三层架构（Controller → Service → Repository）中：
- Controller层负责提取当前用户信息
- Service层方法签名显式包含userId参数
- 禁止Service层直接访问HttpContext

### 1.2 GetOperator()使用规范

#### 1.2.1 何时使用GetOperator()

在Controller层，当需要将当前用户信息传递给Service层时，必须使用`GetOperator()`方法。

**适用场景**:
- 创建操作（Create）- 需要设置CreatedBy/DoctorId等审计字段
- 更新操作（Update）- 需要权限检查（仅创建者或管理员可修改）
- 删除操作（Delete）- 需要权限检查
- 查询操作（Query）- 需要数据权限过滤（仅查看自己的数据）

**不适用场景**:
- 匿名访问的API（如登录、注册）
- 系统级操作（如健康检查）

#### 1.2.2 GetOperator()方法签名

```csharp
protected (Guid OperatorId, string OperatorName, string OperatorRole) GetOperator()
```

**返回值**:
- `OperatorId`: 当前用户的Guid（从JWT Claims中提取）
- `OperatorName`: 当前用户的姓名
- `OperatorRole`: 当前用户的角色（如"Admin"、"Doctor"、"User"）

**异常**:
- `UnauthorizedAccessException` - 用户未登录或JWT信息无效

#### 1.2.3 GetOperator()使用示例

**示例1: 仅提取OperatorId**（创建/审计场景）

```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
    [FromBody] CreateMedicalCaseRequest request)
{
    try
    {
        // ✅ 提取当前医生ID
        var (doctorId, _, _) = GetOperator();

        // ✅ 显式传递给Service层
        var result = await _medicalCaseService.CreateAsync(
            request.PatientId,
            request.VisitDate,
            doctorId);

        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "病案创建成功"));
    }
    catch (UnauthorizedAccessException ex)
    {
        // GetOperator()失败 - 返回401
        return Unauthorized(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
    catch (ArgumentException ex)
    {
        // Service层参数验证失败
        return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
}
```

**示例2: 提取OperatorId + Role**（权限检查场景）

```csharp
[HttpPut("{id}/consultation")]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> UpdateConsultation(
    Guid id,
    [FromBody] ConsultationInputDto request)
{
    try
    {
        // ✅ 提取当前用户ID和角色
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole?.Contains("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

        // ✅ 显式传递给Service层
        var result = await _medicalCaseService.UpdateConsultationAsync(
            id,
            request,
            operatorId,
            isAdmin);

        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "辨证信息更新成功"));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
}
```

**示例3: 提取全部信息**（日志记录场景）

```csharp
[HttpDelete("{id}")]
public async Task<ActionResult<ApiResponse>> DeletePatient(Guid id)
{
    try
    {
        // ✅ 提取全部用户信息用于日志记录
        var (operatorId, operatorName, operatorRole) = GetOperator();

        _logger.LogInformation("用户 {OperatorName}({OperatorId}, {OperatorRole}) 删除患者 {PatientId}",
            operatorName, operatorId, operatorRole, id);

        var result = await _patientService.DeleteAsync(id, operatorId);

        return Ok(ApiResponse.CreateSuccess("患者删除成功"));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(ApiResponse.CreateFail(ex.Message));
    }
}
```

### 1.3 Service层规范

#### 1.3.1 何时添加userId参数

Service层方法需要添加userId参数的场景：

1. **创建操作**（Create）
   - 需要设置CreatedBy、DoctorId、AuthorId等审计字段
   - 需要记录谁创建了这条记录

2. **更新操作**（Update）
   - 需要权限检查（仅创建者或管理员可修改）
   - 需要记录UpdatedBy字段

3. **删除操作**（Delete）
   - 需要权限检查（仅创建者或管理员可删除）
   - 需要软删除时记录DeletedBy字段

4. **查询操作**（Query）
   - 需要数据权限过滤（如仅查看自己创建的记录）

#### 1.3.2 参数命名约定

| 场景 | 参数名 | 说明 |
|------|--------|------|
| 医生创建医案 | `doctorId` | 特定领域语义（医疗场景） |
| 用户创建患者 | `createdBy` | 通用审计语义 |
| 用户创建方剂 | `authorId` | 特定领域语义（知识库场景） |
| 权限检查 | `operatorId` | 通用操作者语义 |
| 角色权限 | `isAdmin` / `operatorRole` | 权限标识 |

**命名原则**:
1. **优先使用领域语义**: doctorId > userId（医疗场景）
2. **审计场景使用createdBy**: 强调审计追踪
3. **权限场景使用operatorId**: 强调操作者身份

#### 1.3.3 参数顺序约定

```csharp
public async Task<T> MethodNameAsync(
    /* 1. 业务主键参数 */
    Guid entityId,

    /* 2. 业务数据参数 */
    BusinessDto dto,

    /* 3. 用户上下文参数（在业务参数之后、可选参数之前） */
    Guid userId,        // 或 doctorId/createdBy/authorId

    /* 4. 可选参数 */
    CancellationToken cancellationToken = default)
{
    // ...
}
```

**顺序原则**:
1. 业务主键参数在前
2. 业务数据参数居中
3. **用户上下文参数在业务参数之后、可选参数之前**
4. 可选参数最后（如CancellationToken）

#### 1.3.4 参数验证规范

**必须验证**:
1. userId不能为`Guid.Empty`
2. userId对应的User实体必须存在

**验证示例**:

```csharp
public async Task<MedicalCaseEntity?> CreateAsync(
    Guid patientId,
    DateTime visitDate,
    Guid doctorId)
{
    // ✅ 步骤1: 验证doctorId不为空
    if (doctorId == Guid.Empty)
    {
        _logger.LogWarning("DoctorId不能为空Guid");
        throw new ArgumentException("DoctorId不能为空", nameof(doctorId));
    }

    // ✅ 步骤2: 验证Doctor实体存在
    var doctor = await _userRepository.GetByIdAsync(doctorId);
    if (doctor == null)
    {
        _logger.LogWarning("医生不存在，DoctorId: {DoctorId}", doctorId);
        throw new InvalidOperationException($"医生不存在，DoctorId: {doctorId}");
    }

    // ✅ 步骤3: 设置审计字段
    var medicalCase = new MedicalCaseEntity
    {
        DoctorId = doctorId,
        DoctorName = doctor.RealName,
        // ...
    };

    return await _repository.AddAsync(medicalCase);
}
```

#### 1.3.5 异常处理规范

**Service层抛出的异常**:
- `ArgumentException` - 参数验证失败（如userId = Guid.Empty）
- `InvalidOperationException` - 业务规则验证失败（如User不存在）
- `UnauthorizedAccessException` - 权限检查失败（如非创建者尝试修改）

**Controller层捕获异常**:
```csharp
try
{
    var (userId, _, _) = GetOperator();
    var result = await _service.CreateAsync(dto, userId);
    return Ok(...);
}
catch (UnauthorizedAccessException ex)
{
    // GetOperator()失败或Service层权限检查失败
    return Unauthorized(ApiResponse.CreateFail(ex.Message));
}
catch (ArgumentException ex)
{
    // Service层参数验证失败
    return BadRequest(ApiResponse.CreateFail(ex.Message));
}
catch (InvalidOperationException ex)
{
    // Service层业务规则验证失败
    return BadRequest(ApiResponse.CreateFail(ex.Message));
}
```

### 1.4 单元测试规范

#### 1.4.1 Controller层测试

**测试目标**: 验证Controller层正确提取userId并传递给Service层

```csharp
[Fact]
public async Task CreateMedicalCase_ShouldExtractDoctorId_AndPassToService()
{
    // Arrange
    var mockService = new Mock<IMedicalCaseService>();
    var controller = new MedicalCaseController(mockService.Object, Mock.Of<ILogger>());

    // Mock User.Claims（模拟已登录医生）
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "doctor-guid-123"),
        new Claim(ClaimTypes.Name, "李医生"),
        new Claim(ClaimTypes.Role, "Doctor")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = principal }
    };

    var request = new CreateMedicalCaseRequest
    {
        PatientId = Guid.NewGuid(),
        VisitDate = DateTime.Now
    };

    // Act
    await controller.CreateMedicalCase(request);

    // Assert: 验证Service层收到正确的doctorId
    mockService.Verify(x => x.CreateAsync(
        request.PatientId,
        request.VisitDate,
        Guid.Parse("doctor-guid-123")),  // ✅ 验证传递正确
        Times.Once);
}
```

#### 1.4.2 Service层测试

**测试目标**: 验证userId参数验证、User查询、审计字段设置

**测试1: 正常流程**

```csharp
[Fact]
public async Task CreateAsync_WithValidDoctorId_ShouldSetDoctorFields()
{
    // Arrange
    var doctorId = Guid.NewGuid();
    var doctor = new User { Id = doctorId, RealName = "赵医生" };

    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo.Setup(x => x.GetByIdAsync(doctorId))
        .ReturnsAsync(doctor);

    var service = new MedicalCaseService(
        mockUserRepo.Object,
        Mock.Of<IPatientRepository>(),
        Mock.Of<IMedicalCaseRepository>(),
        Mock.Of<IConsultationRepository>(),
        Mock.Of<ILogger<MedicalCaseService>>());

    // Act
    var result = await service.CreateAsync(Guid.NewGuid(), DateTime.Now, doctorId);

    // Assert: 验证DoctorId和DoctorName正确设置
    result.DoctorId.Should().Be(doctorId);
    result.DoctorName.Should().Be("赵医生");
}
```

**测试2: 参数验证**

```csharp
[Fact]
public async Task CreateAsync_WithEmptyDoctorId_ShouldThrowArgumentException()
{
    // Arrange
    var service = new MedicalCaseService(...);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(
        () => service.CreateAsync(Guid.NewGuid(), DateTime.Now, Guid.Empty));

    exception.Message.Should().Contain("DoctorId不能为空");
    exception.ParamName.Should().Be("doctorId");
}
```

**测试3: 实体存在性验证**

```csharp
[Fact]
public async Task CreateAsync_WhenDoctorNotFound_ShouldThrowInvalidOperationException()
{
    // Arrange
    var doctorId = Guid.NewGuid();

    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo.Setup(x => x.GetByIdAsync(doctorId))
        .ReturnsAsync((User?)null);  // 模拟Doctor不存在

    var service = new MedicalCaseService(mockUserRepo.Object, ...);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.CreateAsync(Guid.NewGuid(), DateTime.Now, doctorId));

    exception.Message.Should().Contain("医生不存在");
    exception.Message.Should().Contain(doctorId.ToString());
}
```

### 1.5 Code Review检查清单

#### 1.5.1 Controller层检查

```markdown
- [ ] 是否调用了GetOperator()提取userId
- [ ] 是否正确传递userId给Service层
- [ ] GetOperator()异常是否正确处理（返回401 Unauthorized）
- [ ] 日志是否包含userId信息
- [ ] 是否使用Tuple解构语法（var (userId, _, _) = GetOperator()）
```

#### 1.5.2 Service层检查

```markdown
- [ ] 方法签名是否包含userId参数
- [ ] 参数命名是否符合约定（doctorId/createdBy/authorId/operatorId）
- [ ] 参数顺序是否符合约定（业务参数 → userId → 可选参数）
- [ ] 是否验证userId != Guid.Empty
- [ ] 是否查询User实体验证存在性
- [ ] 审计字段是否正确设置（DoctorId/DoctorName/CreatedBy等）
- [ ] 异常消息是否清晰（包含userId信息）
```

#### 1.5.3 单元测试检查

```markdown
- [ ] 是否测试userId = Guid.Empty场景（应抛ArgumentException）
- [ ] 是否测试User不存在场景（应抛InvalidOperationException）
- [ ] 是否测试正常流程（验证审计字段正确设置）
- [ ] 是否使用Mock隔离依赖
- [ ] 测试覆盖率是否≥80%
```

### 1.6 错误示例与正确示例

#### ❌ 错误示例1: Service层注入IHttpContextAccessor

```csharp
// ❌ 违反架构分层原则
public class PatientService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<T> CreateAsync(PatientDto dto)
    {
        // ❌ Service层直接访问HttpContext
        var userId = _httpContextAccessor.HttpContext.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ...
    }
}
```

**问题**:
- 违反单一职责原则（Service层依赖HTTP基础设施）
- 增加耦合（Service层依赖ASP.NET Core）
- 单元测试困难（需Mock HttpContext）

#### ❌ 错误示例2: 通过DTO隐式传递userId

```csharp
// ❌ DTO污染
public class CreatePatientDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public Guid CreatedBy { get; set; }  // ❌ 审计字段不应在业务DTO中
}

public async Task<T> CreateAsync(CreatePatientDto dto)
{
    var userId = dto.CreatedBy;  // ❌ 从DTO提取（隐式传递）
    // ...
}
```

**问题**:
- 业务数据与上下文数据混淆
- DTO污染（审计字段不应在DTO中）
- Controller层可能忘记设置dto.CreatedBy

#### ✅ 正确示例: 显式userId参数

```csharp
// ✅ Controller层
[HttpPost]
public async Task<ActionResult<ApiResponse<Patient>>> CreatePatient(
    [FromBody] CreatePatientDto dto)
{
    try
    {
        // ✅ 显式提取userId
        var (userId, _, _) = GetOperator();

        // ✅ 显式传递给Service层
        var result = await _patientService.CreateAsync(dto, userId);

        return Ok(ApiResponse<Patient>.CreateSuccess(result, "患者创建成功"));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(ApiResponse<Patient>.CreateFail(ex.Message));
    }
}

// ✅ Service层
public async Task<Patient> CreateAsync(CreatePatientDto dto, Guid createdBy)
{
    // ✅ 参数验证
    if (createdBy == Guid.Empty)
        throw new ArgumentException("CreatedBy不能为空", nameof(createdBy));

    // ✅ User查询验证
    var user = await _userRepository.GetByIdAsync(createdBy);
    if (user == null)
        throw new InvalidOperationException($"用户不存在，UserId: {createdBy}");

    // ✅ 设置审计字段
    var patient = new Patient
    {
        Name = dto.Name,
        Phone = dto.Phone,
        CreatedBy = createdBy,        // ← 从参数设置
        CreatedByName = user.RealName // ← 从查询结果设置
    };

    return await _repository.AddAsync(patient);
}
```

### 1.7 常见问题FAQ

**Q1: 为什么不在Service层注入IHttpContextAccessor？**

A: 违反架构分层原则。Service层应仅关注业务逻辑，不应依赖HTTP基础设施。显式参数传递可以：
- 提高代码可读性（依赖关系清晰）
- 提高可测试性（无需Mock HttpContext）
- 降低耦合度（Service层不依赖ASP.NET Core）

**Q2: 如果多个Service方法都需要userId，是否每个都要加参数？**

A: 是的。这是显式设计的一部分。虽然会增加参数数量，但好处是：
- 方法签名清晰表达依赖关系
- 单元测试更容易（Mock传参即可）
- 防止遗漏（编译期检查）

**Q3: GetOperator()失败时应该返回什么HTTP状态码？**

A: 返回`401 Unauthorized`。示例：
```csharp
catch (UnauthorizedAccessException ex)
{
    return Unauthorized(ApiResponse.CreateFail(ex.Message));
}
```

**Q4: userId参数应该放在方法签名的什么位置？**

A: 遵循参数顺序约定：
1. 业务主键参数（如entityId）
2. 业务数据参数（如dto）
3. **用户上下文参数**（如userId/doctorId）
4. 可选参数（如CancellationToken）

**Q5: 何时使用doctorId、何时使用createdBy、何时使用operatorId？**

A: 根据场景选择：
- `doctorId` - 医疗场景（强调医生身份）
- `createdBy` - 审计场景（强调创建者追踪）
- `authorId` - 知识库场景（强调作者身份）
- `operatorId` - 通用权限场景（强调操作者身份）

**Q6: Service层的userId参数是否需要验证User实体存在？**

A: **必须验证**。示例：
```csharp
var user = await _userRepository.GetByIdAsync(userId);
if (user == null)
    throw new InvalidOperationException($"用户不存在，UserId: {userId}");
```

---

## 2. 代码规范

（待补充）

---

## 3. 测试规范

（待补充）

---

## 参考资料

### 相关ADR
- [ADR-001: 用户上下文传递模式](../architecture/decisions/ADR-001-user-context-propagation-pattern.md)

### 相关Issue
- Epic #2210: PatientSelection优化 + P0 MedicalCase创建Bug修复
- Issue #2219: Task 2.1.1 - 全局审计Service Create方法签名
- Issue #2220: Task 2.1.2 - 制定用户上下文传递规范

### 相关审计报告
- `docs/audits/service-create-methods-audit-2025-11-22.md`

---

**文档版本**: v1.0
**最后更新**: 2025-11-22
**维护者**: Claude Code
