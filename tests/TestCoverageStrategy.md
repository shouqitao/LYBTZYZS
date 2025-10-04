# LYBT Server Solution 100%测试覆盖率策略

## 当前状态
- **代码行数**: 45,209行
- **当前覆盖率**: 0.5% (247/45209行)
- **方法覆盖率**: 2.2% (62/2772方法)
- **需要覆盖**: 44,962行代码，2,710个方法

## 测试覆盖优先级

### P0 - 核心业务逻辑 (必须100%覆盖)
1. **Infrastructure层** (0% → 100%)
   - AppDbContext及所有配置
   - Repository基类
   - 授权和认证逻辑
   - 缓存适配器

2. **业务模块Services** (当前<5% → 100%)
   - Auth模块: JwtAuthenticationService, AuthService
   - Users模块: UserService, UserQueryService, UserBusinessService
   - Patients模块: PatientService及相关服务
   - MedicalCase模块: 所有服务类
   - Consultation模块: 所有服务类
   - Prescriptions模块: 所有服务类
   - Herbs模块: 所有服务类
   - Formula模块: 所有服务类

3. **WebAPI控制器** (0% → 100%)
   - 所有Controller类
   - 全局异常处理器
   - 中间件逻辑

### P1 - 数据模型和DTO (80%覆盖)
- Entities层: 所有实体类
- Shared.Models: 所有DTO和异常类

### P2 - 工具类 (60%覆盖)
- Shared.Utilities: 帮助类和扩展方法

## 测试生成策略

### 阶段1: 基础设施测试 (Week 1)
```
tests/UnitTests/Core/
├── LYBT.Infrastructure.Tests/
│   ├── Data/
│   │   ├── AppDbContextTests.cs ✓
│   │   ├── DatabaseInitializationServiceTests.cs
│   │   └── AppDbContextFactoryTests.cs
│   ├── Repositories/
│   │   └── OptimizedBaseRepositoryTests.cs
│   ├── Authorization/
│   │   ├── AuthorizationPolicyExtensionsTests.cs
│   │   └── AuthorizeRolesTests.cs
│   ├── Caching/
│   │   └── MemoryCacheAdapterTests.cs
│   └── Configuration/
│       ├── OptionsTests.cs (所有Options类)
│       └── DefaultPasswordServiceTests.cs
```

### 阶段2: 业务模块测试 (Week 2-3)
```
tests/UnitTests/Modules/
├── Auth.UnitTests/
│   ├── Services/
│   │   ├── AuthServiceFullTests.cs
│   │   ├── JwtAuthenticationServiceTests.cs
│   │   ├── AuthQueryServiceTests.cs
│   │   └── AuthBusinessServiceTests.cs
│   └── Repositories/
│       └── AuthRepositoryTests.cs
├── Users.UnitTests/
│   ├── Services/ (类似结构)
│   └── Repositories/
├── [其他模块类似结构]
```

### 阶段3: WebAPI测试 (Week 4)
```
tests/UnitTests/WebAPI/
├── Controllers/
│   ├── AuthControllerTests.cs
│   ├── UsersControllerTests.cs
│   ├── PatientsControllerTests.cs
│   └── [所有控制器]
├── Middleware/
│   └── GlobalExceptionHandlerTests.cs
└── Extensions/
    ├── UnifiedServiceRegistrationTests.cs
    └── PerformanceOptimizationTests.cs
```

### 阶段4: 实体和DTO测试 (Week 5)
```
tests/UnitTests/
├── Entities.Tests/
│   ├── UserEntityTests.cs
│   ├── PatientEntityTests.cs
│   └── [所有实体]
└── Shared.Models.Tests/
    ├── DTOs/
    │   └── [所有DTO测试]
    └── Exceptions/
        └── [所有异常类测试]
```

## 测试模板

### Service测试模板
```csharp
public class [ServiceName]Tests
{
    private readonly Mock<I[Dependency]> _mockDep;
    private readonly [ServiceName] _service;

    public [ServiceName]Tests()
    {
        _mockDep = new Mock<I[Dependency]>();
        _service = new [ServiceName](_mockDep.Object);
    }

    [Fact]
    public async Task Method_Should_ReturnSuccess_When_ValidInput()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public async Task Method_Should_ReturnFailure_When_InvalidInput()
    {
        // Arrange
        // Act
        // Assert
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Method_Should_HandleEdgeCases(string input)
    {
        // Test edge cases
    }
}
```

### Controller测试模板
```csharp
public class [Controller]Tests
{
    private readonly Mock<I[Service]> _mockService;
    private readonly [Controller] _controller;

    public [Controller]Tests()
    {
        _mockService = new Mock<I[Service]>();
        _controller = new [Controller](_mockService.Object);
    }

    [Fact]
    public async Task Get_Should_ReturnOk_When_DataExists()
    {
        // Test HTTP 200 scenarios
    }

    [Fact]
    public async Task Get_Should_ReturnNotFound_When_DataNotExists()
    {
        // Test HTTP 404 scenarios
    }

    [Fact]
    public async Task Post_Should_ReturnBadRequest_When_ModelInvalid()
    {
        // Test HTTP 400 scenarios
    }
}
```

## 自动化测试生成

### PowerShell脚本生成测试
```powershell
# GenerateTests.ps1
$sourceDir = "src/Server"
$testDir = "tests/UnitTests"

Get-ChildItem -Path $sourceDir -Filter "*.cs" -Recurse | ForEach-Object {
    $className = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    if ($className -notlike "*Tests" -and $className -ne "Program") {
        # Generate test file based on template
        # ...
    }
}
```

## 测试执行计划

### 每日目标
- Day 1-2: Infrastructure层 (20个类)
- Day 3-5: Auth和Users模块 (30个类)
- Day 6-8: Patients和MedicalCase模块 (25个类)
- Day 9-11: Consultation和Prescriptions模块 (25个类)
- Day 12-14: Herbs和Formula模块 (20个类)
- Day 15-17: WebAPI控制器 (20个控制器)
- Day 18-20: 实体和DTO (50个类)
- Day 21: 集成测试和覆盖率验证

### 测试指标
- 单元测试总数目标: 5,000+
- 每个类平均测试: 10-15个
- 代码覆盖率目标: 100%
- 分支覆盖率目标: 95%+
- 方法覆盖率目标: 100%

## 测试工具和框架
- **测试框架**: xUnit 2.4.2
- **Mock框架**: Moq 4.20
- **断言库**: FluentAssertions 6.12
- **覆盖率工具**: dotnet-reportgenerator-globaltool
- **测试数据生成**: Bogus 35.0

## CI/CD集成
```yaml
# azure-pipelines.yml
- task: DotNetCoreCLI@2
  displayName: 'Run tests with coverage'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--collect:"XPlat Code Coverage" --settings coverlet.runsettings'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Agent.TempDirectory)/*/coverage.cobertura.xml'
    failIfCoverageEmpty: true
```

## 质量门禁
- 新代码覆盖率: >= 80%
- 总体覆盖率: >= 100%
- 测试通过率: 100%
- 无跳过的测试

## 注意事项
1. 优先测试public方法
2. 使用InMemoryDatabase进行数据库测试
3. Mock外部依赖
4. 测试异常路径和边界条件
5. 避免测试私有方法
6. 确保测试独立性和可重复性