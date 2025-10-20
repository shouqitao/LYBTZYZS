# DEEP-003: 测试策略指南

## 概述

凌隐宝堂中医诊所管理系统作为医疗行业应用，对软件质量和可靠性有着极高的要求。本测试策略指南基于实际项目架构和业务特点,提供全面的测试方法论，包括单元测试、集成测试、API测试、UI测试和性能测试，确保系统在患者数据管理、处方计算、药材管理等关键业务场景中的稳定性和准确性。

## 测试架构体系

### 1. 测试金字塔

```
    E2E Tests (10%)
     用户端到端场景
   ─────────────────
  Integration Tests (20%)
    API和数据库集成
 ─────────────────────────
Unit Tests (70%)
   业务逻辑和算法测试
```

### 2. 测试分层策略

#### 2.1 单元测试层
- **目标**：验证单个类、方法的业务逻辑正确性
- **覆盖率**：代码覆盖率 ≥ 80%，核心业务逻辑 ≥ 95%
- **重点**：处方价格计算、中药材配伍规则、患者数据处理

#### 2.2 集成测试层
- **目标**：验证组件间协作和数据流转正确性
- **重点**：数据库操作、API接口、外部服务集成
- **环境**：使用内存数据库和模拟服务

#### 2.3 端到端测试层
- **目标**：验证完整业务流程和用户体验
- **重点**：患者就诊流程、处方开具、药材管理
- **频率**：每发布前执行

## 单元测试策略

### 1. 处方计算测试

```csharp
// 处方价格计算服务测试
[TestFixture]
public class PrescriptionCalculationServiceTests
{
    private PrescriptionCalculationService _service;
    private Mock<IHerbPriceService> _mockPriceService;
    private Mock<IDiscountService> _mockDiscountService;

    [SetUp]
    public void Setup()
    {
        _mockPriceService = new Mock<IHerbPriceService>();
        _mockDiscountService = new Mock<IDiscountService>();
        _service = new PrescriptionCalculationService(
            _mockPriceService.Object,
            _mockDiscountService.Object,
            Mock.Of<ILogger<PrescriptionCalculationService>>());
    }

    [Test]
    public async Task CalculatePrescriptionAsync_StandardCase_ReturnsCorrectTotal()
    {
        // Arrange
        var request = new PrescriptionCalculationRequest
        {
            PrescriptionId = 1,
            PatientId = 100,
            DoctorId = 10,
            Items = new List<PrescriptionItemRequest>
            {
                new() { HerbId = 1, HerbName = "人参", Quantity = 10, Unit = "g" },
                new() { HerbId = 2, HerbName = "白术", Quantity = 15, Unit = "g" },
                new() { HerbId = 3, HerbName = "茯苓", Quantity = 12, Unit = "g" }
            },
            PrescriptionDate = DateTime.Today,
            IsFirstTimePatient = false
        };

        _mockPriceService.Setup(x => x.GetCurrentPriceAsync(1))
            .ReturnsAsync(8.50m); // 人参 8.50元/g
        _mockPriceService.Setup(x => x.GetCurrentPriceAsync(2))
            .ReturnsAsync(3.20m); // 白术 3.20元/g
        _mockPriceService.Setup(x => x.GetCurrentPriceAsync(3))
            .ReturnsAsync(2.80m); // 茯苓 2.80元/g

        _mockDiscountService.Setup(x => x.CalculateDiscountAsync(It.IsAny<DiscountCalculationRequest>()))
            .ReturnsAsync(new DiscountCalculationResult
            {
                DiscountAmount = 0,
                DiscountReason = "无折扣"
            });

        // Act
        var result = await _service.CalculatePrescriptionAsync(request);

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(3));

        // 验证单项计算
        var ginsengItem = result.Items.First(x => x.HerbName == "人参");
        Assert.That(ginsengItem.Quantity, Is.EqualTo(10));
        Assert.That(ginsengItem.UnitPrice, Is.EqualTo(8.50m));
        Assert.That(ginsengItem.Subtotal, Is.EqualTo(85.00m));

        var atractylodesItem = result.Items.First(x => x.HerbName == "白术");
        Assert.That(atractylodesItem.Quantity, Is.EqualTo(15));
        Assert.That(atractylodesItem.UnitPrice, Is.EqualTo(3.20m));
        Assert.That(atractylodesItem.Subtotal, Is.EqualTo(48.00m));

        var poriaItem = result.Items.First(x => x.HerbName == "茯苓");
        Assert.That(poriaItem.Quantity, Is.EqualTo(12));
        Assert.That(poriaItem.UnitPrice, Is.EqualTo(2.80m));
        Assert.That(poriaItem.Subtotal, Is.EqualTo(33.60m));

        // 验证总计
        Assert.That(result.Subtotal, Is.EqualTo(166.60m));
        Assert.That(result.TotalAmount, Is.EqualTo(166.60m));
        Assert.That(result.DiscountAmount, Is.EqualTo(0));
    }

    [Test]
    public async Task CalculatePrescriptionAsync_WithDiscount_AppliesCorrectDiscount()
    {
        // Arrange
        var request = new PrescriptionCalculationRequest
        {
            PrescriptionId = 1,
            PatientId = 100,
            DoctorId = 10,
            IsFirstTimePatient = true,
            Items = new List<PrescriptionItemRequest>
            {
                new() { HerbId = 1, HerbName = "人参", Quantity = 10, Unit = "g" }
            },
            PrescriptionDate = DateTime.Today
        };

        _mockPriceService.Setup(x => x.GetCurrentPriceAsync(1))
            .ReturnsAsync(10.00m);

        _mockDiscountService.Setup(x => x.CalculateDiscountAsync(It.IsAny<DiscountCalculationRequest>()))
            .ReturnsAsync(new DiscountCalculationResult
            {
                DiscountAmount = 10.00m,
                DiscountReason = "首诊患者优惠10%"
            });

        // Act
        var result = await _service.CalculatePrescriptionAsync(request);

        // Assert
        Assert.That(result.Subtotal, Is.EqualTo(100.00m));
        Assert.That(result.DiscountAmount, Is.EqualTo(10.00m));
        Assert.That(result.TotalAmount, Is.EqualTo(90.00m));
        Assert.That(result.DiscountReason, Is.EqualTo("首诊患者优惠10%"));
    }

    [TestCase(0, 0, 0)] // 空处方
    [TestCase(1, 10.5, 10.5)] // 单项
    [TestCase(5, 100, 100)] // 多项
    public async Task CalculatePrescriptionAsync_VariousItemCounts_CalculatesCorrectly(
        int itemCount, decimal expectedSubtotal, decimal expectedTotal)
    {
        // Arrange
        var items = Enumerable.Range(1, itemCount)
            .Select(i => new PrescriptionItemRequest
            {
                HerbId = i,
                HerbName = $"药材{i}",
                Quantity = 10,
                Unit = "g"
            }).ToList();

        var request = new PrescriptionCalculationRequest
        {
            PrescriptionId = 1,
            Items = items
        };

        for (int i = 1; i <= itemCount; i++)
        {
            _mockPriceService.Setup(x => x.GetCurrentPriceAsync(i))
                .ReturnsAsync(expectedSubtotal / itemCount / 10); // 平均价格
        }

        _mockDiscountService.Setup(x => x.CalculateDiscountAsync(It.IsAny<DiscountCalculationRequest>()))
            .ReturnsAsync(new DiscountCalculationResult { DiscountAmount = 0 });

        // Act
        var result = await _service.CalculatePrescriptionAsync(request);

        // Assert
        Assert.That(result.Subtotal, Is.EqualTo(expectedSubtotal));
        Assert.That(result.TotalAmount, Is.EqualTo(expectedTotal));
    }
}
```

### 2. 患者数据处理测试

```csharp
[TestFixture]
public class PatientServiceTests
{
    private PatientService _service;
    private Mock<LYBTClinicDbContext> _mockContext;
    private Mock<DbSet<Patient>> _mockPatients;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<LYBTClinicDbContext>();
        _mockPatients = CreateMockDbSet(GetTestPatients());

        _mockContext.Setup(x => x.Patients).Returns(_mockPatients.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PatientService(
            _mockContext.Object,
            Mock.Of<ILogger<PatientService>>(),
            Mock.Of<IMapper>());
    }

    [Test]
    public async Task CreatePatientAsync_ValidData_ReturnsPatientId()
    {
        // Arrange
        var request = new CreatePatientRequest
        {
            Name = "张三",
            Gender = "男",
            DateOfBirth = new DateTime(1980, 1, 1),
            PhoneNumber = "13800138000",
            IdentificationNumber = "110101198001011234",
            Address = "北京市朝阳区"
        };

        _mockPatients.Setup(x => x.Add(It.IsAny<Patient>()));

        // Act
        var result = await _service.CreatePatientAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.PatientId, Is.GreaterThan(0));

        _mockPatients.Verify(x => x.Add(It.Is<Patient>(p =>
            p.Name == request.Name &&
            p.Gender == request.Gender &&
            p.PhoneNumber == request.PhoneNumber)), Times.Once);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreatePatientAsync_DuplicateIdentificationNumber_ReturnsError()
    {
        // Arrange
        var request = new CreatePatientRequest
        {
            Name = "李四",
            Gender = "女",
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "13900139000",
            IdentificationNumber = "110101198001011234", // 重复的身份证号
            Address = "上海市浦东新区"
        };

        _mockPatients.Setup(x => x.AnyAsync(p => p.IdentificationNumber == request.IdentificationNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreatePatientAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("身份证号已存在"));

        _mockPatients.Verify(x => x.Add(It.IsAny<Patient>()), Times.Never);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SearchPatientsAsync_ByName_ReturnsMatchingPatients()
    {
        // Arrange
        var searchRequest = new SearchPatientsRequest
        {
            Keyword = "张",
            Page = 1,
            PageSize = 10
        };

        var expectedPatients = GetTestPatients()
            .Where(p => p.Name.Contains("张"))
            .ToList();

        _mockPatients.Setup(x => x.Where(It.IsAny<Expression<Func<Patient, bool>>>()))
            .Returns(_mockPatients.Object);
        _mockPatients.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPatients);

        // Act
        var result = await _service.SearchPatientsAsync(searchRequest);

        // Assert
        Assert.That(result.Patients.Count, Is.EqualTo(expectedPatients.Count));
        Assert.That(result.Patients.All(p => p.Name.Contains("张")), Is.True);
    }

    private List<Patient> GetTestPatients()
    {
        return new List<Patient>
        {
            new Patient
            {
                ID = 1,
                Name = "张三",
                Gender = "男",
                DateOfBirth = new DateTime(1980, 1, 1),
                PhoneNumber = "13800138000",
                IdentificationNumber = "110101198001011234",
                Address = "北京市朝阳区",
                Status = "Active",
                CreatedDate = DateTime.Now
            },
            new Patient
            {
                ID = 2,
                Name = "李四",
                Gender = "女",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "13900139000",
                IdentificationNumber = "110101199001011234",
                Address = "上海市浦东新区",
                Status = "Active",
                CreatedDate = DateTime.Now
            }
        };
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        var queryable = data.AsQueryable();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));

        return mockSet;
    }
}
```

## 集成测试策略

### 1. API控制器集成测试

```csharp
[TestFixture]
public class PatientsControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private LYBTClinicDbContext _context;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替换数据库上下文为内存数据库
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LYBTClinicDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LYBTClinicDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });

                    // 添加测试身份验证
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            });

        _client = _factory.CreateClient();

        // 初始化测试数据
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<LYBTClinicDbContext>();
        InitializeTestData();
    }

    [SetUp]
    public void Setup()
    {
        // 每个测试前清理并重新初始化数据
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        InitializeTestData();
    }

    [Test]
    public async Task CreatePatient_ValidRequest_ReturnsCreatedPatient()
    {
        // Arrange
        var request = new
        {
            Name = "测试患者",
            Gender = "男",
            DateOfBirth = "1980-01-01",
            PhoneNumber = "13800138000",
            IdentificationNumber = "110101198001011234",
            Address = "北京市朝阳区"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var patient = await response.Content.ReadFromJsonAsync<PatientDto>();

        Assert.That(patient, Is.Not.Null);
        Assert.That(patient.Name, Is.EqualTo(request.Name));
        Assert.That(patient.Gender, Is.EqualTo(request.Gender));
        Assert.That(patient.PhoneNumber, Is.EqualTo(request.PhoneNumber));

        // 验证数据库中的数据
        var dbPatient = await _context.Patients.FindAsync(patient.ID);
        Assert.That(dbPatient, Is.Not.Null);
        Assert.That(dbPatient.Name, Is.EqualTo(request.Name));
    }

    [Test]
    public async Task GetPatient_ExistingId_ReturnsPatientDetails()
    {
        // Arrange
        var existingPatient = _context.Patients.First();

        // Act
        var response = await _client.GetAsync($"/api/patients/{existingPatient.ID}");

        // Assert
        response.EnsureSuccessStatusCode();
        var patient = await response.Content.ReadFromJsonAsync<PatientDetailDto>();

        Assert.That(patient, Is.Not.Null);
        Assert.That(patient.ID, Is.EqualTo(existingPatient.ID));
        Assert.That(patient.Name, Is.EqualTo(existingPatient.Name));
    }

    [Test]
    public async Task UpdatePatient_ValidRequest_ReturnsUpdatedPatient()
    {
        // Arrange
        var existingPatient = _context.Patients.First();
        var updateRequest = new
        {
            Name = "更新后的姓名",
            PhoneNumber = "13900139000",
            Address = "更新后的地址"
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/patients/{existingPatient.ID}", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var updatedPatient = await response.Content.ReadFromJsonAsync<PatientDto>();

        Assert.That(updatedPatient.Name, Is.EqualTo(updateRequest.Name));
        Assert.That(updatedPatient.PhoneNumber, Is.EqualTo(updateRequest.PhoneNumber));
        Assert.That(updatedPatient.Address, Is.EqualTo(updateRequest.Address));
    }

    [Test]
    public async Task SearchPatients_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var patient1 = new Patient
        {
            Name = "张测试",
            Gender = "男",
            PhoneNumber = "13800138001",
            IdentificationNumber = "110101198001011235",
            Status = "Active"
        };
        var patient2 = new Patient
        {
            Name = "李测试",
            Gender = "女",
            PhoneNumber = "13800138002",
            IdentificationNumber = "110101198001011236",
            Status = "Active"
        };

        _context.Patients.AddRange(patient1, patient2);
        _context.SaveChanges();

        // Act
        var response = await _client.GetAsync("/api/patients/search?keyword=张&gender=男");

        // Assert
        response.EnsureSuccessStatusCode();
        var searchResult = await response.Content.ReadFromJsonAsync<PagedResult<PatientDto>>();

        Assert.That(searchResult.Items.Count, Is.EqualTo(1));
        Assert.That(searchResult.Items.First().Name, Is.EqualTo("张测试"));
        Assert.That(searchResult.Items.First().Gender, Is.EqualTo("男"));
    }

    private void InitializeTestData()
    {
        var patients = new List<Patient>
        {
            new Patient
            {
                Name = "初始患者1",
                Gender = "男",
                DateOfBirth = new DateTime(1980, 1, 1),
                PhoneNumber = "13800138001",
                IdentificationNumber = "110101198001011234",
                Address = "北京市朝阳区",
                Status = "Active",
                CreatedDate = DateTime.Now
            },
            new Patient
            {
                Name = "初始患者2",
                Gender = "女",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138002",
                IdentificationNumber = "110101199001011234",
                Address = "上海市浦东新区",
                Status = "Active",
                CreatedDate = DateTime.Now
            }
        };

        _context.Patients.AddRange(patients);
        _context.SaveChanges();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _context?.Dispose();
    }
}

// 测试身份验证处理器
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Doctor")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### 2. 数据库集成测试

```csharp
[TestFixture]
public class DatabaseIntegrationTests
{
    private LYBTClinicDbContext _context;
    private IDbContextTransaction _transaction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<LYBTClinicDbContext>()
            .UseSqlServer("Server=localhost;Database=LYBT_Clinic_Test;Trusted_Connection=true;")
            .Options;

        _context = new LYBTClinicDbContext(options);
        _transaction = _context.Database.BeginTransaction();
    }

    [TearDown]
    public void TearDown()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _context?.Dispose();
    }

    [Test]
    public async Task PrescriptionCalculation_WithComplexDiscounts_ReturnsCorrectTotal()
    {
        // Arrange
        var patient = new Patient
        {
            Name = "测试患者",
            Gender = "男",
            DateOfBirth = new DateTime(1980, 1, 1),
            PhoneNumber = "13800138000",
            IdentificationNumber = "110101198001011234",
            Status = "Active"
        };

        var doctor = new Doctor
        {
            Name = "测试医生",
            Specialty = "中医内科",
            Title = "主治医师",
            Status = "Active"
        };

        var medicalCase = new MedicalCase
        {
            Patient = patient,
            Doctor = doctor,
            VisitDate = DateTime.Today,
            ChiefComplaint = "头痛",
            Diagnosis = "风寒感冒",
            TreatmentPrinciple = "疏风散寒",
            Status = "Active"
        };

        var herbs = new List<Herb>
        {
            new() { Name = "麻黄", UnitPrice = 5.00m, IsActive = true },
            new() { Name = "桂枝", UnitPrice = 4.00m, IsActive = true },
            new() { Name = "杏仁", UnitPrice = 6.00m, IsActive = true }
        };

        _context.Patients.Add(patient);
        _context.Doctors.Add(doctor);
        _context.MedicalCases.Add(medicalCase);
        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act
        var prescription = new Prescription
        {
            MedicalCase = medicalCase,
            Doctor = doctor,
            PrescriptionDate = DateTime.Today,
            Status = "Active",
            PrescriptionItems = new List<PrescriptionItem>
            {
                new() { Herb = herbs[0], Quantity = 10, UnitPrice = 5.00m },
                new() { Herb = herbs[1], Quantity = 15, UnitPrice = 4.00m },
                new() { Herb = herbs[2], Quantity = 12, UnitPrice = 6.00m }
            }
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        // 计算总价
        var calculatedTotal = prescription.PrescriptionItems
            .Sum(item => item.Quantity * item.UnitPrice);

        // Assert
        Assert.That(calculatedTotal, Is.EqualTo(187.00m)); // 10*5 + 15*4 + 12*6 = 50 + 60 + 72 = 182

        // 验证数据库中的数据
        var savedPrescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .ThenInclude(pi => pi.Herb)
            .FirstOrDefaultAsync(p => p.ID == prescription.ID);

        Assert.That(savedPrescription, Is.Not.Null);
        Assert.That(savedPrescription.PrescriptionItems.Count, Is.EqualTo(3));
        Assert.That(savedPrescription.TotalAmount, Is.EqualTo(calculatedTotal));
    }

    [Test]
    public async Task ConcurrentPatientUpdate_WithOptimisticConcurrency_HandlesConflict()
    {
        // Arrange
        var patient = new Patient
        {
            Name = "并发测试患者",
            Gender = "男",
            DateOfBirth = new DateTime(1980, 1, 1),
            PhoneNumber = "13800138000",
            IdentificationNumber = "110101198001011234",
            Status = "Active"
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // 创建两个独立的上下文模拟并发
        using var context1 = new LYBTClinicDbContext(_context.GetDbContextOptions());
        using var context2 = new LYBTClinicDbContext(_context.GetDbContextOptions());

        var patient1 = await context1.Patients.FindAsync(patient.ID);
        var patient2 = await context2.Patients.FindAsync(patient.ID);

        // Act
        patient1.PhoneNumber = "13800138001";
        await context1.SaveChangesAsync();

        patient2.PhoneNumber = "13800138002"; // 这会引发并发冲突

        // Assert
        var ex = Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Entries.Count, Is.EqualTo(1));
        Assert.That(ex.Entries.First().Entity, Is.EqualTo(patient2));
    }
}
```

## UI测试策略

### 1. WPF客户端UI测试

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public class PatientManagementUITests
{
    private PatientManagementWindow _window;
    private PatientManagementViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        // 初始化测试环境
        var mockService = new Mock<IPatientService>();
        mockService.Setup(x => x.SearchPatientsAsync(It.IsAny<SearchPatientsRequest>()))
            .ReturnsAsync(new SearchPatientsResult
            {
                Patients = new List<PatientDto>
                {
                    new PatientDto { ID = 1, Name = "张三", Gender = "男", PhoneNumber = "13800138000" },
                    new PatientDto { ID = 2, Name = "李四", Gender = "女", PhoneNumber = "13900139000" }
                },
                TotalCount = 2
            });

        _viewModel = new PatientManagementViewModel(mockService.Object);
        _window = new PatientManagementWindow
        {
            DataContext = _viewModel
        };

        _window.Show();
    }

    [TearDown]
    public void TearDown()
    {
        _window?.Close();
        _window = null;
        _viewModel = null;
    }

    [Test]
    public void SearchButton_Click_PerformsSearchAndUpdatesResults()
    {
        // Arrange
        var searchTextBox = _window.FindName("SearchTextBox") as TextBox;
        var searchButton = _window.FindName("SearchButton") as Button;
        var patientsDataGrid = _window.FindName("PatientsDataGrid") as DataGrid;

        searchTextBox.Text = "张";

        // Act
        searchButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // 等待异步搜索完成
        WaitFor(() => _viewModel.Patients.Count > 0, TimeSpan.FromSeconds(5));

        // Assert
        Assert.That(patientsDataGrid.Items.Count, Is.EqualTo(2));
        Assert.That(_viewModel.Patients.Count, Is.EqualTo(2));
        Assert.That(_viewModel.Patients.Any(p => p.Name.Contains("张")), Is.True);
    }

    [Test]
    public void AddPatientButton_Click_OpensAddPatientDialog()
    {
        // Arrange
        var addButton = _window.FindName("AddPatientButton") as Button;

        // Act
        addButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // Assert
        // 验证添加患者对话框是否打开
        var addPatientDialog = Application.Current.Windows.OfType<AddPatientDialog>().FirstOrDefault();
        Assert.That(addPatientDialog, Is.Not.Null);
        Assert.That(addPatientDialog.IsVisible, Is.True);

        // 清理
        addPatientDialog?.Close();
    }

    [Test]
    public void PatientDataGrid_DoubleClick_OpensPatientDetail()
    {
        // Arrange
        var patientsDataGrid = _window.FindName("PatientsDataGrid") as DataGrid;

        // 先加载数据
        _viewModel.LoadPatientsCommand.Execute(null);
        WaitFor(() => _viewModel.Patients.Count > 0, TimeSpan.FromSeconds(5));

        // Act
        var firstItem = patientsDataGrid.Items[0];
        patientsDataGrid.SelectedItem = firstItem;
        patientsDataGrid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 1, MouseButton.Left)
        {
            RoutedEvent = DataGrid.MouseDoubleClickEvent,
            Source = patientsDataGrid
        });

        // Assert
        var detailWindow = Application.Current.Windows.OfType<PatientDetailWindow>().FirstOrDefault();
        Assert.That(detailWindow, Is.Not.Null);
        Assert.That(detailWindow.IsVisible, Is.True);

        // 清理
        detailWindow?.Close();
    }

    private void WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.Now;
        while (DateTime.Now - start < timeout && !condition())
        {
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            Thread.Sleep(50);
        }

        if (!condition())
        {
            throw new TimeoutException($"Condition not met within {timeout.TotalSeconds} seconds");
        }
    }
}
```

## 性能测试策略

### 1. 负载测试

```csharp
[TestFixture]
public class LoadTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Test]
    [TestCase(10, 1000)]    // 10个并发用户，每个发送1000个请求
    [TestCase(50, 500)]     // 50个并发用户，每个发送500个请求
    [TestCase(100, 200)]    // 100个并发用户，每个发送200个请求
    public async Task SearchPatients_LoadTest_ReturnsWithinExpectedTime(
        int concurrentUsers, int requestsPerUser)
    {
        // Arrange
        var stopwatch = new Stopwatch();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        stopwatch.Start();

        for (int i = 0; i < concurrentUsers; i++)
        {
            int userId = i;
            var userTasks = Enumerable.Range(0, requestsPerUser)
                .Select(async requestId =>
                {
                    var response = await _client.GetAsync(
                        $"/api/patients/search?keyword=test&page={requestId}&pageSize=20");
                    return response;
                });

            tasks.AddRange(userTasks);
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);

        var averageResponseTime = stopwatch.ElapsedMilliseconds / (concurrentUsers * requestsPerUser);
        var totalRequests = concurrentUsers * requestsPerUser;
        var requestsPerSecond = totalRequests / stopwatch.Elapsed.TotalSeconds;

        Assert.That(averageResponseTime, Is.LessThan(1000)); // 平均响应时间小于1秒
        Assert.That(requestsPerSecond, Is.GreaterThan(100));  // 每秒处理至少100个请求

        Console.WriteLine($"并发用户: {concurrentUsers}, 每用户请求数: {requestsPerUser}");
        Console.WriteLine($"总请求数: {totalRequests}, 平均响应时间: {averageResponseTime}ms");
        Console.WriteLine($"每秒处理请求数: {requestsPerSecond:F2}");
    }

    [Test]
    public async Task PrescriptionCalculation_ConcurrentCalculations_HandlesLoad()
    {
        // Arrange
        var concurrentCalculations = 50;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < concurrentCalculations; i++)
        {
            var prescriptionRequest = new
            {
                PatientId = 1,
                DoctorId = 1,
                Items = new[]
                {
                    new { HerbId = 1, Quantity = 10, Unit = "g" },
                    new { HerbId = 2, Quantity = 15, Unit = "g" },
                    new { HerbId = 3, Quantity = 12, Unit = "g" }
                }
            };

            var task = _client.PostAsJsonAsync("/api/prescriptions/calculate", prescriptionRequest);
            tasks.Add(task);
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);

        var calculationResults = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<PrescriptionCalculationResult>()));

        Assert.That(calculationResults.All(r => r.TotalAmount > 0), Is.True);
        Assert.That(calculationResults.All(r => r.Items.Count == 3), Is.True);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### 2. 内存泄漏测试

```csharp
[TestFixture]
public class MemoryLeakTests
{
    [Test]
    public async Task RepeatedPatientSearch_DoesNotCauseMemoryLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var factory = new WebApplicationFactory<Program>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            using var client = factory.CreateClient();
            await client.GetAsync($"/api/patients/search?keyword=test&page={i}&pageSize=20");

            // 每100次操作强制GC
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        // 最终清理
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.That(memoryIncrease, Is.LessThan(50 * 1024 * 1024)); // 内存增长小于50MB

        Console.WriteLine($"初始内存: {initialMemory / 1024 / 1024:F2}MB");
        Console.WriteLine($"最终内存: {finalMemory / 1024 / 1024:F2}MB");
        Console.WriteLine($"内存增长: {memoryIncrease / 1024 / 1024:F2}MB");

        factory.Dispose();
    }
}
```

## 测试配置和工具

### 1. 测试配置文件

```json
// appsettings.Test.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBT_Clinic_Test;Trusted_Connection=true;MultipleActiveResultSets=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "TestSettings": {
    "ParallelTestExecution": true,
    "MaxDegreeOfParallelism": 4,
    "TestTimeoutMinutes": 30,
    "CleanupTestData": true
  }
}
```

### 2. 测试基础类

```csharp
[TestFixture]
public abstract class TestBase
{
    protected WebApplicationFactory<Program> _factory;
    protected HttpClient _client;
    protected LYBTClinicDbContext _context;
    protected IServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public virtual async Task OneTimeSetup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.Test.json", optional: false);
                });

                builder.ConfigureServices(services =>
                {
                    // 替换数据库
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LYBTClinicDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LYBTClinicDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // 添加测试身份验证
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            });

        _client = _factory.CreateClient();
        _serviceProvider = _factory.Services;
        _context = _serviceProvider.GetRequiredService<LYBTClinicDbContext>();

        await InitializeTestData();
    }

    [SetUp]
    public virtual async Task Setup()
    {
        await CleanupTestData();
        await InitializeTestData();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        await CleanupTestData();
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _context?.Dispose();
    }

    protected abstract Task InitializeTestData();

    protected virtual async Task CleanupTestData()
    {
        // 清理测试数据
        _context.PrescriptionItems.RemoveRange(_context.PrescriptionItems);
        _context.Prescriptions.RemoveRange(_context.Prescriptions);
        _context.MedicalCases.RemoveRange(_context.MedicalCases);
        _context.Herbs.RemoveRange(_context.Herbs);
        _context.Patients.RemoveRange(_context.Patients);
        _context.Doctors.RemoveRange(_context.Doctors);

        await _context.SaveChangesAsync();
    }

    protected void AssertSuccessResponse(HttpResponseMessage response)
    {
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Request failed with status code {response.StatusCode}. Response: {response.Content.ReadAsStringAsync().Result}");
    }

    protected async Task<T> GetResponseContentAsync<T>(HttpResponseMessage response)
    {
        AssertSuccessResponse(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

## 测试自动化和CI/CD集成

### 1. GitHub Actions工作流

```yaml
# .github/workflows/test.yml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore LYBT.All.sln

    - name: Build
      run: dotnet build LYBT.All.sln --no-restore --configuration Release

    - name: Run Unit Tests
      run: dotnet test LYBT.All.sln --no-build --configuration Release --logger "trx;LogFileName=test_results.trx" --collect:"XPlat Code Coverage"

    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: |
          **/*.trx
          **/coverage.cobertura.xml

    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

    - name: Upload Coverage Report
      uses: actions/upload-artifact@v3
      with:
        name: coverage-report
        path: coverage-report/

  integration-tests:
    runs-on: windows-latest
    needs: unit-tests

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
        ports:
          - 1433:1433

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore LYBT.All.sln

    - name: Build
      run: dotnet build LYBT.All.sln --no-restore --configuration Release

    - name: Run Integration Tests
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=LYBT_Clin_Integration_Test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;"
      run: dotnet test tests/Integration/LYBT.Tests.Integration.csproj --no-build --configuration Release --logger "trx;LogFileName=integration_test_results.trx"

    - name: Upload Integration Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: integration-test-results
        path: **/integration_test_results.trx

  performance-tests:
    runs-on: windows-latest
    needs: integration-tests
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore LYBT.All.sln

    - name: Build
      run: dotnet build LYBT.All.sln --no-restore --configuration Release

    - name: Run Performance Tests
      run: dotnet test tests/Performance/LYBT.Tests.Performance.csproj --no-build --configuration Release --logger "trx;LogFileName=performance_test_results.trx"

    - name: Upload Performance Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: performance-test-results
        path: **/performance_test_results.trx
```

### 2. 测试报告生成

```xml
<!-- tests/.runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <TargetFrameworkVersion>net8.0</TargetFrameworkVersion>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TestCaseFilter>TestCategory!=Integration</TestCaseFilter>
    <CollectSourceInformation>false</CollectSourceInformation>
  </RunConfiguration>

  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft.CodeCoverage/2.0" assemblyQualifiedName="Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Exclude>
                <ModulePath>.*Tests.*</ModulePath>
                <ModulePath>.*TestHelpers.*</ModulePath>
              </Exclude>
            </ModulePaths>
            <Functions>
              <Exclude>
                <Function>.*\.cctor$</Function>
              </Exclude>
            </Functions>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## 测试检查清单

### 单元测试
- [ ] 处方价格计算算法准确性
- [ ] 中药材配伍规则验证
- [ ] 患者数据验证和业务规则
- [ ] 折扣计算规则
- [ ] 身份验证和授权逻辑
- [ ] 数据转换和映射逻辑

### 集成测试
- [ ] API接口端到端测试
- [ ] 数据库CRUD操作
- [ ] 事务完整性
- [ ] 并发操作处理
- [ ] 外部服务集成
- [ ] 缓存机制验证

### UI测试
- [ ] 主要用户工作流程
- [ ] 数据输入验证
- [ ] 用户界面响应性
- [ ] 错误处理和用户反馈
- [ ] 数据绑定和显示
- [ ] 键盘和鼠标交互

### 性能测试
- [ ] 负载测试和并发处理
- [ ] 响应时间基准测试
- [ ] 内存泄漏检测
- [ ] 数据库查询性能
- [ ] 批量操作性能
- [ ] 长时间运行稳定性

### 自动化
- [ ] CI/CD流水线集成
- [ ] 测试报告生成
- [ ] 代码覆盖率监控
- [ ] 性能回归测试
- [ ] 自动化测试环境部署
- [ ] 测试数据管理

通过这套完整的测试策略，凌隐宝堂中医诊所管理系统能够确保在各种使用场景下的稳定性、准确性和性能表现，为医疗机构的日常运营提供可靠的技术保障。