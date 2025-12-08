# LYBTZYZS开发规范（Development Standards）

**项目**: 凌隐宝堂中医诊所管理系统（LYBTZYZS）
**版本**: v1.1
**最后更新**: 2025-12-08
**维护者**: Claude Code

---

## 目录

1. [用户上下文传递规范](#1-用户上下文传递规范)
2. [枚举使用规范](#2-枚举使用规范enum-usage-standards)
3. [API所有权验证规范](#3-api所有权验证规范ownership-validation-standards)
4. [测试规范（待补充）](#4-测试规范)

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

## 2. 枚举使用规范（Enum Usage Standards）

> **架构决策**: 枚举应在共享层定义，业务逻辑使用枚举比较，仅在WebAPI传输时转换为字符串
>
> **制定背景**: Issue #2241 发现多处违反枚举设计原则，使用字符串比较代替枚举比较

### 2.1 核心原则

**枚举优先 > 字符串比较**

在整个应用程序中：
- 枚举在共享层定义（LYBT.Shared.Models/Enums）
- 业务逻辑使用枚举类型进行比较和判断
- 仅在WebAPI序列化时转换为字符串（JSON传输）
- 禁止在业务逻辑中使用字符串进行枚举值比较

### 2.2 枚举定义规范

#### 2.2.1 定义位置

所有枚举必须定义在共享层：
```
LYBT.Shared.Models/
  └── Enums/
      ├── AuthEnums.cs          # 认证授权相关枚举
      ├── MedicalCaseEnums.cs   # 医案相关枚举
      ├── PatientEnums.cs       # 患者相关枚举
      └── ...
```

#### 2.2.2 JSON序列化配置

所有枚举必须添加`[JsonConverter]`特性以支持字符串序列化：

```csharp
using System.Text.Json.Serialization;

/// <summary>
/// 用户角色枚举
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>超级管理员（最高权限）</summary>
    [Description("超级管理员")]
    SuperAdmin = 100,

    /// <summary>管理员（系统管理、用户管理）</summary>
    [Description("管理员")]
    Admin = 10,

    /// <summary>医生（诊疗、记录、查询等业务操作）</summary>
    [Description("医生")]
    Doctor = 1
}
```

**必需配置**:
- `[JsonConverter(typeof(JsonStringEnumConverter))]` - 启用字符串序列化
- `[Description("...")]` - 提供中文描述（用于UI显示）
- 明确的整数值 - 便于数据库存储和版本兼容

### 2.3 业务逻辑中的枚举使用

#### 2.3.1 禁止的模式

**❌ 禁止1: 字符串相等比较**
```csharp
// ❌ 错误：使用字符串比较
if (operatorRole == "Admin")
{
    // ...
}

// ❌ 错误：使用字符串Contains
if (operatorRole?.Contains("Admin") == true)
{
    // ...
}

// ❌ 错误：使用字符串不等比较
if (operatorRole != "Doctor")
{
    // ...
}
```

**❌ 禁止2: 方法签名使用字符串类型**
```csharp
// ❌ 错误：返回值类型为string
protected (Guid OperatorId, string OperatorName, string OperatorRole) GetOperator()
{
    var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;
    return (opId, userName, roleStr); // ❌ 直接返回字符串
}
```

#### 2.3.2 推荐的模式

**✅ 推荐1: 枚举相等比较**
```csharp
// ✅ 正确：使用枚举比较
if (operatorRole == UserRole.Admin)
{
    // ...
}

// ✅ 正确：使用枚举逻辑或
if (operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin)
{
    // ...
}

// ✅ 正确：使用枚举不等比较
if (operatorRole != UserRole.Doctor)
{
    // ...
}
```

**✅ 推荐2: 方法签名使用枚举类型**
```csharp
// ✅ 正确：返回值类型为UserRole枚举
protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
{
    var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

    // ✅ 在数据入口点转换字符串→枚举
    var role = ParseUserRole(roleStr);
    return (opId, userName, role);
}
```

### 2.4 字符串→枚举转换规范

#### 2.4.1 转换时机

字符串→枚举转换应该在**数据入口点**进行：
- Controller层：从JWT Claims提取角色后立即转换
- Middleware层：从HttpContext.User提取角色后立即转换
- Service层：如需从外部数据源获取枚举值，获取后立即转换

#### 2.4.2 转换模式

**标准转换方法**:
```csharp
/// <summary>
/// 解析用户角色字符串为UserRole枚举
/// Issue #2241: 处理遗留命名和无效值
/// </summary>
private UserRole ParseUserRole(string? roleStr)
{
    if (string.IsNullOrWhiteSpace(roleStr))
    {
        _logger.LogWarning("角色值为空，默认使用Doctor");
        return UserRole.Doctor;
    }

    // 步骤1: 处理遗留命名（SysAdmin → SuperAdmin）
    if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
    {
        roleStr = "SuperAdmin";
    }

    // 步骤2: 尝试解析为枚举
    if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
    {
        // 步骤3: 检查是否为已废弃的角色
        if (role == UserRole.User ||
            role == UserRole.Pharmacist ||
            role == UserRole.Receptionist ||
            role == UserRole.Cashier ||
            role == UserRole.Therapist)
        {
            _logger.LogWarning("使用了已废弃的角色 {ObsoleteRole}，统一为Doctor", role);
            return UserRole.Doctor;
        }

        return role;
    }

    // 步骤4: 解析失败，记录警告并使用默认值
    _logger.LogWarning("无效的角色值: {RoleString}，默认使用Doctor", roleStr);
    return UserRole.Doctor;
}
```

**转换要点**:
1. **空值处理**: 空字符串/null应返回安全的默认值
2. **大小写不敏感**: 使用`ignoreCase: true`
3. **遗留兼容**: 处理历史命名（如SysAdmin→SuperAdmin）
4. **废弃值处理**: 将废弃的枚举值映射到新值
5. **日志记录**: 所有边界情况都应记录Warning日志
6. **安全回退**: 解析失败时返回安全的默认值

### 2.5 遗留兼容性处理

#### 2.5.1 废弃枚举值标记

对于已废弃但需保留兼容性的枚举值，使用`[Obsolete]`特性：

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    SuperAdmin = 100,
    Admin = 10,
    Doctor = 1,

    /// <summary>普通用户 - 已统一到Doctor角色</summary>
    [Description("普通用户")]
    [Obsolete("Use Doctor instead. User role unified to Doctor in role unification.", false)]
    User = 20,

    /// <summary>药师 - 已统一到Doctor角色</summary>
    [Description("药师")]
    [Obsolete("Use Doctor instead. Pharmacist role unified to Doctor in role unification.", false)]
    Pharmacist = 2
}
```

**处理策略**:
- 保留废弃值以避免反序列化错误
- 标记`[Obsolete]`防止新代码使用
- 在ParseXXX方法中将废弃值映射到新值
- 记录Warning日志用于监控

#### 2.5.2 遗留命名映射

对于历史原因使用的不同命名，在转换方法中统一映射：

```csharp
// 遗留命名映射
if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
{
    roleStr = "SuperAdmin";
}
```

### 2.6 示例场景

#### 2.6.1 Controller层GetOperator()实现

```csharp
/// <summary>
/// 获取当前操作者信息 - 兼容多种Claims标准
/// Issue #2241: 返回UserRole枚举而非字符串
/// </summary>
protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
{
    // 提取用户ID
    var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User?.FindFirst("sub")?.Value;

    // 提取用户名
    var userName = User?.Identity?.Name
                  ?? User?.FindFirst(ClaimTypes.Name)?.Value
                  ?? User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    // 提取角色字符串
    var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value
                 ?? User?.FindFirst("role")?.Value;

    if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName))
    {
        // ✅ 在数据入口点转换字符串→枚举
        var role = ParseUserRole(roleStr);
        return (opId, userName, role);
    }

    throw new UnauthorizedAccessException("未登录或用户信息无效");
}
```

#### 2.6.2 Middleware层角色检查

```csharp
/// <summary>
/// 提取用户权限信息
/// Issue #2241: 使用UserRole枚举
/// </summary>
private static MedicalCaseUserInfo? ExtractUserInfo(HttpContext context)
{
    if (context.User?.Identity?.IsAuthenticated != true)
        return null;

    var claims = context.User.Claims;

    // 获取用户ID
    var userIdClaim = claims.FirstOrDefault(c =>
        c.Type == ClaimTypes.NameIdentifier ||
        c.Type == JwtRegisteredClaimNames.Sub)?.Value;

    if (!Guid.TryParse(userIdClaim, out var userId))
        return null;

    // 获取用户名
    var userName = claims.FirstOrDefault(c =>
        c.Type == ClaimTypes.Name)?.Value ?? "Unknown";

    // Issue #2241: 获取角色并转换为UserRole枚举
    var roleStr = claims.FirstOrDefault(c =>
        c.Type == ClaimTypes.Role)?.Value;

    var role = ParseUserRole(roleStr);

    // Issue #2241: 检查是否为管理员，使用枚举比较
    var isAdmin = role == UserRole.SuperAdmin || role == UserRole.Admin;

    return new MedicalCaseUserInfo
    {
        UserId = userId,
        UserName = userName,
        Role = role,  // ← UserRole枚举类型
        IsAdmin = isAdmin
    };
}
```

#### 2.6.3 业务逻辑中的角色判断

```csharp
/// <summary>
/// 查询待诊队列 - 根据角色返回不同数据
/// Issue #2241: 使用UserRole枚举比较
/// </summary>
[HttpGet("pending")]
public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> GetPendingQueue()
{
    try
    {
        var (operatorId, operatorName, operatorRole) = GetOperator();

        // ✅ 使用UserRole枚举比较，而非字符串比较
        if (operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin)
        {
            // 管理员查询所有待诊医案
            _logger.LogInformation("管理员查询全部待诊队列，OperatorId: {OperatorId}, Role: {Role}",
                operatorId, operatorRole);
            result = await _medicalCaseService.GetAllPendingCasesAsync();
        }
        else if (operatorRole == UserRole.Doctor)
        {
            // 医生只查询自己的待诊医案
            _logger.LogInformation("医生查询自己的待诊队列，DoctorId: {DoctorId}",
                operatorId);
            result = await _medicalCaseService.GetPendingCasesAsync(operatorId);
        }
        else
        {
            return Forbid(); // 其他角色禁止访问
        }

        return Ok(ApiResponse<List<MedicalCaseDto>>.CreateSuccess(result));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(ApiResponse<List<MedicalCaseDto>>.CreateFail(ex.Message));
    }
}
```

### 2.7 Code Review检查清单

#### 2.7.1 枚举定义检查

```markdown
- [ ] 枚举定义在LYBT.Shared.Models/Enums命名空间中
- [ ] 枚举标记了[JsonConverter(typeof(JsonStringEnumConverter))]
- [ ] 每个枚举值都有[Description]特性
- [ ] 枚举值使用明确的整数值（非自动递增）
- [ ] 废弃的枚举值标记了[Obsolete]
```

#### 2.7.2 业务逻辑检查

```markdown
- [ ] 方法签名使用枚举类型而非string（如UserRole而非string）
- [ ] 业务逻辑使用枚举比较（role == UserRole.Admin）
- [ ] 没有使用字符串比较（role == "Admin"）
- [ ] 没有使用字符串Contains（role?.Contains("Admin")）
- [ ] 字符串→枚举转换在数据入口点进行
```

#### 2.7.3 转换方法检查

```markdown
- [ ] ParseXXX方法处理了null/空字符串情况
- [ ] ParseXXX方法使用Enum.TryParse(ignoreCase: true)
- [ ] ParseXXX方法处理了遗留命名（如SysAdmin→SuperAdmin）
- [ ] ParseXXX方法将废弃枚举值映射到新值
- [ ] ParseXXX方法对边界情况记录Warning日志
- [ ] ParseXXX方法有安全的默认返回值
```

### 2.8 常见问题FAQ

**Q1: 为什么不能在业务逻辑中使用字符串比较？**

A: 字符串比较的问题：
- **类型不安全**: 拼写错误（"Admin" vs "Admim"）在编译期无法发现
- **重构困难**: 重命名枚举值时字符串比较不会自动更新
- **性能较差**: 字符串比较比枚举比较慢
- **违反设计**: 枚举设计的目的就是提供类型安全的常量值

**Q2: JWT Claims中存储的是字符串，为什么要转换为枚举？**

A: JWT Claims存储字符串是传输层的实现细节，业务逻辑层应该使用枚举：
- **单一职责**: Controller/Middleware负责数据转换，Service负责业务逻辑
- **类型安全**: 业务逻辑使用强类型避免错误
- **关注点分离**: 传输格式（JSON字符串）与业务模型（枚举）分离

**Q3: ParseUserRole方法应该放在哪里？**

A: 根据使用场景放置：
- **BaseControllerCore**: Controller层GetOperator()使用
- **Middleware**: Middleware层ExtractUserInfo()使用
- **共享工具类**: 多处使用时可提取到Shared.Utilities

原则：**避免重复代码，但保持代码内聚性**

**Q4: 如何处理数据库中存储的旧枚举值？**

A: 使用迁移策略：
1. 标记废弃值为`[Obsolete]`保持反序列化兼容
2. ParseXXX方法将废弃值映射到新值
3. 数据迁移脚本更新数据库中的旧值
4. 监控日志确认无新代码使用废弃值
5. 一段时间后（如3个月）移除废弃值

**Q5: 枚举值应该使用什么整数值？**

A: 推荐策略：
- **权限等级递增**: SuperAdmin(100) > Admin(10) > Doctor(1)
- **避免连续值**: 便于未来插入新值（100, 10, 1而非3, 2, 1）
- **明确赋值**: 不依赖自动递增，避免添加新值时改变现有值

### 2.9 相关Issue

- Issue #2241: 枚举使用规范 - 全局修复字符串比较违规

---

---

## 3. API所有权验证规范（Ownership Validation Standards）

> **架构决策**: API端点执行修改/删除操作前必须验证资源所有权
>
> **制定背景**: OpenSpec optimize-module-list-ui 反思报告发现多Controller存在重复的所有权验证代码，提炼统一模式

### 3.1 核心原则

**统一验证 > 分散代码**

在Controller层执行写操作（Update/Delete/ToggleStatus/Restore）时：
- 必须验证资源存在性
- 必须验证当前用户是资源创建者或管理员
- 使用BaseApiController提供的统一方法而非重复编写

### 3.2 所有权验证方法

#### 3.2.1 GetEntityWithOwnershipCheckAsync

统一的所有权验证方法，用于Service层返回`Result<TDto>`的场景。所有DTO通过继承`TimestampDto`自动实现`ICreatorTrackable`接口。

**方法签名**:
```csharp
protected async Task<(TDto? data, IActionResult? error)> GetEntityWithOwnershipCheckAsync<TDto>(
    Guid id,
    Func<Guid, Task<Result<TDto>>> getByIdFunc,
    string resourceName = "资源") where TDto : class, ICreatorTrackable
```

**使用示例**:
```csharp
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto)
{
    try
    {
        // 使用统一的所有权检查方法
        var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
        if (ownershipError != null) return ownershipError;

        // 所有权验证通过，执行更新
        var result = await _herbService.UpdateAsync(id, dto);
        // ...
    }
    catch (Exception ex) { /* ... */ }
}
```

**适用场景**（所有Controller统一使用此方法）:
- HerbsController（药材管理）
- FormulasController（验方管理）
- PatientsController（患者管理）
- 其他所有需要所有权验证的Controller

**DTO继承链**:
```
PatientDto/HerbDto/FormulaDto → StatusDto → TimestampDto → BaseDto
                                              ↓
                                    实现 ICreatorTrackable (CreatedBy属性)
```

### 3.3 ICreatorTrackable接口

#### 3.3.1 接口定义

```csharp
/// <summary>
/// 标记DTO包含创建者信息，用于统一所有权验证
/// </summary>
public interface ICreatorTrackable
{
    /// <summary>创建者ID</summary>
    Guid? CreatedBy { get; }
}
```

#### 3.3.2 DTO实现

所有需要所有权验证的DTO应继承自`DtoBase`（已实现ICreatorTrackable）：

```csharp
public class HerbDto : DtoBase
{
    // 继承自DtoBase的CreatedBy属性
    public string Name { get; set; }
    // ...
}
```

### 3.4 验证逻辑

统一方法内部执行以下验证：

1. **Guid有效性验证**: 检查id不为Guid.Empty
2. **资源存在性验证**: 调用Service层GetById方法
3. **所有权验证**: 调用ValidateOwnership方法

```csharp
protected IActionResult? ValidateOwnership(Guid? createdBy, string resourceName = "资源")
{
    var (operatorId, _, operatorRole) = GetOperator();
    var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

    // 管理员可操作所有资源
    if (isAdmin) return null;

    // 非管理员只能操作自己创建的资源
    if (createdBy != operatorId)
    {
        _logger.LogWarning("非管理员尝试操作他人创建的{ResourceName}：CreatedBy={CreatedBy}, OperatorId={OperatorId}",
            resourceName, createdBy, operatorId);
        return Forbid($"只能操作自己创建的{resourceName}");
    }

    return null;
}
```

### 3.5 返回值说明

两个方法都返回元组`(TData? data, IActionResult? error)`：

| 场景 | data | error | 处理方式 |
|------|------|-------|----------|
| 验证通过 | 实体数据 | null | 继续执行业务逻辑 |
| Guid无效 | null | ValidationFail结果 | 直接return error |
| 资源不存在 | null | NotFound结果 | 直接return error |
| 无权限 | null | Forbid结果 | 直接return error |

### 3.6 需要所有权验证的操作

以下API操作**必须**使用统一所有权验证方法：

| 操作类型 | HTTP Method | 说明 |
|----------|-------------|------|
| Update | PUT | 更新资源信息 |
| Delete | DELETE | 删除资源（软删除） |
| ToggleStatus | POST | 切换启用/禁用状态 |
| Restore | POST | 恢复软删除的资源 |

### 3.7 不需要所有权验证的操作

以下操作**不需要**所有权验证：

| 操作类型 | HTTP Method | 说明 |
|----------|-------------|------|
| GetList | GET | 列表查询（通过Service层角色过滤） |
| GetById | GET | 详情查询（可查看不一定能修改） |
| Create | POST | 新增资源（无现有资源所有者） |
| Import | POST | 批量导入（创建操作） |
| Export | GET | 导出数据（读操作） |

### 3.8 Code Review检查清单

#### 3.8.1 方法选择检查

```markdown
- [ ] Update/Delete/ToggleStatus/Restore操作是否使用统一所有权验证方法
- [ ] 是否使用GetEntityWithOwnershipCheckAsync（所有DTO已通过继承实现ICreatorTrackable）
- [ ] 是否正确处理返回的error（if (ownershipError != null) return ownershipError）
```

#### 3.8.2 验证逻辑检查

```markdown
- [ ] 是否在业务逻辑之前执行所有权验证
- [ ] 资源名称参数是否使用中文（用于错误消息）
- [ ] 是否避免了重复的Guid验证代码
- [ ] 是否避免了重复的资源存在性检查代码
```

### 3.9 迁移指南

将旧代码迁移到统一模式：

**迁移1: 手动验证代码 → 统一方法**

旧模式（4行）:
```csharp
if (ValidateGuid(id, "药材ID") is { } guidError) return guidError;
var existing = await _herbService.GetByIdAsync(id);
if (!existing.IsSuccess || existing.Data == null) return NotFound("药材不存在");
if (ValidateOwnership(existing.Data.CreatedBy, "药材") is { } ownerError) return ownerError;
```

新模式（2行）:
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
if (ownershipError != null) return ownershipError;
```

**迁移2: Entity版本 → DTO版本（已废弃GetDataWithOwnershipCheckAsync）**

旧模式（使用选择器函数）:
```csharp
// 已废弃 - GetDataWithOwnershipCheckAsync方法已删除
var (_, ownershipError) = await GetDataWithOwnershipCheckAsync(
    id, _service.GetByIdEntityAsync, e => e.CreatedBy, "患者");
```

新模式（统一使用DTO）:
```csharp
// 所有DTO通过TimestampDto继承链自动实现ICreatorTrackable
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
    id, _service.GetByIdAsync, "患者");
```

**设计说明**: 所有DTO通过继承`TimestampDto`自动获得`CreatedBy`属性，AutoMapper自动映射Entity到DTO时会包含此属性，因此不再需要Entity版本的所有权验证方法。

### 3.10 相关Issue/OpenSpec

- OpenSpec: optimize-module-list-ui - 模块列表UI优化反思报告

---

## 4. 测试规范

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

**文档版本**: v1.1
**最后更新**: 2025-12-08
**维护者**: Claude Code
