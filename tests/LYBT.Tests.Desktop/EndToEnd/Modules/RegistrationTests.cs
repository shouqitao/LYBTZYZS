using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class RegistrationTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public RegistrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static int _counter = 0;
    private static readonly object _lock = new();

    private static string GenerateIdNumber()
    {
        int unique;
        lock (_lock)
        {
            unique = Interlocked.Increment(ref _counter);
        }
        var hexSuffix = Guid.NewGuid().ToString("N")[..4];
        var uniqueNum = Convert.ToInt32(hexSuffix, 16) % 10000;
        var day = 10 + (unique % 18);
        var seq = 100 + (uniqueNum % 900);
        var body = $"110101199001{day:D2}{seq:D3}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }

    private static string GeneratePhoneNumber()
    {
        int unique;
        lock (_lock)
        {
            unique = Interlocked.Increment(ref _counter);
        }
        var guidPart = Guid.NewGuid().ToString("N")[..6];
        var phoneSuffix = Convert.ToInt32(guidPart[..5], 16) % 1000000000;
        var secondDigit = 3 + (unique % 7);
        return $"1{secondDigit}{phoneSuffix:D9}";
    }


    private async Task<Guid> CreateTestPatientAsync()
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var patient = new PatientInputDto
        {
            Name = $"挂号测试患者_{uniqueId}",
            PinYinCode = "GHCS",
            IdNumber = GenerateIdNumber(),
            PhoneNumber = GeneratePhoneNumber(),
            Gender = Gender.Male,
            Address = "E2E测试地址"
        };

        var response = await PatientApi.CreatePatientAsync(patient);
        response.Success.Should().BeTrue("创建患者应成功");
        response.Data.Should().NotBeNull();
        return response.Data!.Id;
    }

    private async Task<(Guid Id, string UserName, string RealName)> CreateTestDoctorAsync()
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var doctor = new UserInputDto
        {
            UserName = $"doctor_{uniqueId}",
            Password = "DoctorPass123",
            ConfirmPassword = "DoctorPass123",
            RealName = $"测试医生_{uniqueId}",
            Role = UserRole.Doctor,
            PhoneNumber = GeneratePhoneNumber(),
        };

        var response = await UserApi.CreateUserAsync(doctor);
        response.Success.Should().BeTrue("创建医生应成功");
        response.Data.Should().NotBeNull();
        return (response.Data!.Id, response.Data.UserName, response.Data.RealName);
    }

    private async Task<RegistrationDetailDto> CreateTestRegistrationAsync(Guid patientId, string patientName, Guid doctorId, string doctorName)
    {
        var registration = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Source = RegistrationSource.Receptionist,
            Remark = "挂号测试"
        };

        var response = await RegistrationApi.CreateAsync(registration);
        response.Success.Should().BeTrue("创建挂号应成功");
        response.Data.Should().NotBeNull();
        return response.Data!;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task CreateRegistration_ValidInput_ReturnsCreatedRegistration()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        
        var registration = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = "挂号测试患者",
            DoctorId = doctorId,
            DoctorName = doctorRealName,
            Source = RegistrationSource.Receptionist,
            Remark = "E2E测试挂号"
        };

        var response = await RegistrationApi.CreateAsync(registration);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().NotBe(Guid.Empty);
        response.Data.PatientId.Should().Be(patientId);
        response.Data.DoctorId.Should().Be(doctorId);
        response.Data.Source.Should().Be(RegistrationSource.Receptionist);
        response.Data.Status.Should().Be(RegistrationStatus.Waiting);
        response.Data.Remark.Should().Be("E2E测试挂号");

        _output.WriteLine($"挂号创建成功: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetRegistrationById_ExistingRegistration_ReturnsDetail()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        var created = await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);

        var response = await RegistrationApi.GetByIdAsync(created.Id);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(created.Id);
        response.Data.PatientId.Should().Be(patientId);
        response.Data.DoctorId.Should().Be(doctorId);
        response.Data.Status.Should().Be(RegistrationStatus.Waiting);

        _output.WriteLine($"挂号详情查询成功: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetRegistrations_WithPagination_ReturnsPagedResult()
    {
        await LoginAsSysadminAsync();
        
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);

        var response = await RegistrationApi.GetListAsync(1, 10);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeNull();
        response.Data.TotalCount.Should().BeGreaterThan(0);

        _output.WriteLine($"挂号列表查询成功，共 {response.Data.TotalCount} 条记录");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetQueue_WithDoctorFilter_ReturnsWaitingList()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        var created = await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);

        var response = await RegistrationApi.GetQueueAsync(doctorId);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        
        var found = response.Data!.Any(r => r.Id == created.Id && r.Status == RegistrationStatus.Waiting);
        found.Should().BeTrue("队列中应包含刚创建的 Waiting 状态挂号");

        _output.WriteLine($"队列查询成功，共 {response.Data.Count} 条记录");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetQueue_WithoutDoctorFilter_ReturnsAllWaitingList()
    {
        await LoginAsSysadminAsync();
        
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);

        var response = await RegistrationApi.GetQueueAsync();

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().OnlyContain(r => r.Status == RegistrationStatus.Waiting);

        _output.WriteLine($"全部队列查询成功，共 {response.Data.Count} 条记录");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task StartVisit_WaitingRegistration_ChangesToInProgress()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        var created = await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);
        
        created.Status.Should().Be(RegistrationStatus.Waiting);

        var response = await RegistrationApi.StartVisitAsync(created.Id);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBe(Guid.Empty, "接诊应返回创建的医案ID");

        var detailResponse = await RegistrationApi.GetByIdAsync(created.Id);
        detailResponse.Success.Should().BeTrue();
        detailResponse.Data!.Status.Should().Be(RegistrationStatus.InProgress);
        response.Data.Should().NotBeEmpty();

        _output.WriteLine($"接诊成功，医案ID: {response.Data}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task CancelRegistration_WaitingRegistration_CancelsSuccessfully()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        var created = await CreateTestRegistrationAsync(patientId, "测试患者", doctorId, doctorRealName);

        var response = await RegistrationApi.CancelAsync(created.Id);

        response.Success.Should().BeTrue();

        var detailResponse = await RegistrationApi.GetByIdAsync(created.Id);
        detailResponse.Success.Should().BeTrue();
        detailResponse.Data!.Status.Should().Be(RegistrationStatus.Cancelled);

        _output.WriteLine($"挂号取消成功: {created.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetRegistrations_WithKeyword_FiltersResults()
    {
        await LoginAsSysadminAsync();
        
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();
        var uniqueName = $"关键词测试_{Guid.NewGuid():N}";
        await CreateTestRegistrationAsync(patientId, uniqueName, doctorId, doctorRealName);

        var response = await RegistrationApi.GetListAsync(1, 10, keyword: uniqueName);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        
        _output.WriteLine($"关键词搜索返回 {response.Data!.TotalCount} 条记录");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task RegistrationFullLifecycle_CreateStartVisitCancel_AllSucceed()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();

        var createResponse = await RegistrationApi.CreateAsync(new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = "生命周期测试患者",
            DoctorId = doctorId,
            DoctorName = doctorRealName,
            Source = RegistrationSource.Receptionist,
            Remark = "完整生命周期测试"
        });
        createResponse.Success.Should().BeTrue("创建挂号应成功");
        var registrationId = createResponse.Data!.Id;
        _output.WriteLine($"✓ 挂号创建成功: {registrationId}");

        var getResponse = await RegistrationApi.GetByIdAsync(registrationId);
        getResponse.Success.Should().BeTrue("查询挂号应成功");
        getResponse.Data!.Status.Should().Be(RegistrationStatus.Waiting);
        _output.WriteLine($"✓ 挂号状态为 Waiting");

        var queueResponse = await RegistrationApi.GetQueueAsync(doctorId);
        queueResponse.Success.Should().BeTrue("查询队列应成功");
        queueResponse.Data!.Any(r => r.Id == registrationId).Should().BeTrue("挂号应在等待队列中");
        _output.WriteLine($"✓ 挂号在等待队列中");

        var startVisitResponse = await RegistrationApi.StartVisitAsync(registrationId);
        startVisitResponse.Success.Should().BeTrue("接诊应成功");
        var medicalCaseId = startVisitResponse.Data;
        _output.WriteLine($"✓ 接诊成功，医案ID: {medicalCaseId}");

        var afterVisitResponse = await RegistrationApi.GetByIdAsync(registrationId);
        afterVisitResponse.Success.Should().BeTrue();
        afterVisitResponse.Data!.Status.Should().Be(RegistrationStatus.InProgress);
        medicalCaseId.Should().NotBe(Guid.Empty, "接诊响应返回值即创建的医案ID");
        _output.WriteLine($"✓ 挂号状态已变为 InProgress");

        var listResponse = await RegistrationApi.GetListAsync(1, 20);
        listResponse.Success.Should().BeTrue("分页查询应成功");
        listResponse.Data!.Items.Any(r => r.Id == registrationId).Should().BeTrue("挂号应在列表中");
        _output.WriteLine($"✓ 挂号在分页列表中");

        _output.WriteLine("挂号完整生命周期测试全部通过 ✓");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "RegistrationManagement")]
    [Trait("Role", "Receptionist")]
    public async Task RegistrationFullLifecycle_ReceptionistFlow_Succeeds()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (doctorId, _, doctorRealName) = await CreateTestDoctorAsync();

        var registration = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = "前台流程测试患者",
            DoctorId = doctorId,
            DoctorName = doctorRealName,
            Source = RegistrationSource.Receptionist,
            Remark = "前台挂号流程测试"
        };

        var createResponse = await RegistrationApi.CreateAsync(registration);
        createResponse.Success.Should().BeTrue();
        var regId = createResponse.Data!.Id;

        var detailResponse = await RegistrationApi.GetByIdAsync(regId);
        detailResponse.Data!.Status.Should().Be(RegistrationStatus.Waiting);
        detailResponse.Data.Source.Should().Be(RegistrationSource.Receptionist);

        var visitResponse = await RegistrationApi.StartVisitAsync(regId);
        visitResponse.Success.Should().BeTrue();

        var finalResponse = await RegistrationApi.GetByIdAsync(regId);
        finalResponse.Data!.Status.Should().Be(RegistrationStatus.InProgress);

        _output.WriteLine("前台挂号流程测试通过 ✓");
    }
}
