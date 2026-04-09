using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class PatientTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public PatientTests(ITestOutputHelper output)
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
        // 生成唯一的时间戳后缀（取GUID的前4位作为16进制数值）
        var hexSuffix = Guid.NewGuid().ToString("N")[..4];
        var uniqueNum = Convert.ToInt32(hexSuffix, 16) % 10000;
        
        // 身份证格式: 6位地址码 + 8位出生日期 + 3位顺序码 + 1位校验码 = 18位
        // 地址码: 110101 (北京市东城区)
        // 出生日期: 199001DD (1990年1月, DD用unique生成10-27日)
        // 顺序码: 001-999
        var day = 10 + (unique % 18);  // 10-27日
        var seq = 100 + (uniqueNum % 900);  // 100-999
        var body = $"110101199001{day:D2}{seq:D3}";
        
        // 计算校验码 (ISO 7064:1983.MOD 11-2)
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
        // 使用GUID确保唯一性: 1 + (3-9) + 9位数字
        var guidPart = Guid.NewGuid().ToString("N")[..6];
        // 取GUID前6位的前5位作为数字，确保11位: 1 + (3-9) + 9位数字
        var phoneSuffix = Convert.ToInt32(guidPart[..5], 16) % 1000000000;
        var secondDigit = 3 + (unique % 7);
        // 3-9
        return $"1{secondDigit
    }{phoneSuffix:D9}";
    }

    private static PatientInputDto CreateTestPatientInput(string suffix = "") => new()
    {
        Name = $"测试患者{suffix}_{Guid.NewGuid():N}".Substring(0, 15),
        PinYinCode = "CSHZ",
        IdNumber = GenerateIdNumber(),
        Gender = Gender.Male,
        BirthDate = new DateTime(1990, 1, 1),
        PhoneNumber = GeneratePhoneNumber(),
        Address = "E2E测试地址",
        AllergyHistory = "无",
        MedicalHistory = "无特殊病史"
    };

    #region CRUD

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task CreatePatient_ValidInput_ReturnsCreatedPatient()
    {
        await LoginAsSysadminAsync();
        var input = CreateTestPatientInput();

        var response = await PatientApi.CreatePatientAsync(input);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(input.Name);
        _output.WriteLine($"Created patient: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetPatientById_ExistingPatient_ReturnsDetail()
    {
        await LoginAsSysadminAsync();
        var createResponse = await PatientApi.CreatePatientAsync(CreateTestPatientInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var patientId = createResponse.Data!.Id;

        var response = await PatientApi.GetPatientByIdAsync(patientId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(patientId);
        _output.WriteLine($"Retrieved patient: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task UpdatePatient_ValidInput_ReturnsUpdatedPatient()
    {
        await LoginAsSysadminAsync();
        var createResponse = await PatientApi.CreatePatientAsync(CreateTestPatientInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var patientId = createResponse.Data!.Id;

        var updateInput = CreateTestPatientInput("upd");
        var response = await PatientApi.UpdatePatientAsync(patientId, updateInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(updateInput.Name);
        _output.WriteLine($"Updated patient: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetPatients_WithPagination_ReturnsPagedResult()
    {
        await LoginAsSysadminAsync();
        await PatientApi.CreatePatientAsync(CreateTestPatientInput());

        var response = await PatientApi.GetPatientsAsync(1, 10);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.TotalCount.Should().BeGreaterThan(0);
        _output.WriteLine($"Patients page: {response.Data.Items.Count}/{response.Data.TotalCount}");
    }

    #endregion

    #region Soft Delete & Restore

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task DeleteAndRestore_Patient_CompletesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var createResponse = await PatientApi.CreatePatientAsync(CreateTestPatientInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var patientId = createResponse.Data!.Id;

        var deleteResponse = await PatientApi.DeletePatientAsync(patientId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine($"Deleted patient {patientId}");

        var restoreResponse = await PatientApi.RestoreAsync(patientId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine($"Restored patient {patientId}");

        var getResponse = await PatientApi.GetPatientByIdAsync(patientId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
    }

    #endregion

    #region Batch Operations

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task BatchDelete_MultiplePatients_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var p1 = await PatientApi.CreatePatientAsync(CreateTestPatientInput("b1"));
        var p2 = await PatientApi.CreatePatientAsync(CreateTestPatientInput("b2"));
        p1.Success.Should().BeTrue(p1.Message);
        p2.Success.Should().BeTrue(p2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { p1.Data!.Id, p2.Data!.Id }
        };

        var response = await PatientApi.BatchDeleteAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch deleted: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Search

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task GetPatients_WithKeyword_FiltersResults()
    {
        await LoginAsSysadminAsync();
        var uniqueName = $"搜索患者_{Guid.NewGuid():N}".Substring(0, 15);
        var input = CreateTestPatientInput();
        input.Name = uniqueName;
        var createResponse = await PatientApi.CreatePatientAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);

        var response = await PatientApi.GetPatientsAsync(1, 10, uniqueName);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().Contain(p => p.Name == uniqueName);
        _output.WriteLine($"Search for '{uniqueName}': found {response.Data.Items.Count}");
    }

    #endregion
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task ExportTemplate_ReturnsFileResponse()
    {
        await LoginAsSysadminAsync();

        var response = await PatientApi.ExportTemplateAsync();

        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        _output.WriteLine($"Export template: status={response.StatusCode}");
    }

    #region Import

    [Fact(Skip = "Export/Import endpoints not yet implemented")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task BatchImport_ValidPatients_ReturnsImportResult()
    {
        await LoginAsSysadminAsync();

        var importData = new PatientBatchImportInputDto
        {
            Patients = new List<PatientInputDto>
            {
                new()
                {
                    Name = $"导入患者1_{Guid.NewGuid():N}".Substring(0, 15),
                    PinYinCode = "DRHZ",
                    IdNumber = GenerateIdNumber(),
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1990, 5, 15),
                    PhoneNumber = "13900000001",
                    Address = "导入测试地址1"
                },
                new()
                {
                    Name = $"导入患者2_{Guid.NewGuid():N}".Substring(0, 15),
                    PinYinCode = "DRHZ",
                    IdNumber = GenerateIdNumber(),
                    Gender = Gender.Female,
                    BirthDate = new DateTime(1985, 8, 20),
                    PhoneNumber = "13900000002",
                    Address = "导入测试地址2"
                }
            },
            Strategy = DuplicateStrategy.Skip
        };

        var response = await PatientApi.BatchImportAsync(importData);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch imported: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "PatientManagement")]
    [Trait("Role", "Receptionist")]
    public async Task PatientFullLifecycle_CreateUpdateDeleteRestore_AllSucceed()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create
        var input = CreateTestPatientInput("lc");
        var createResponse = await PatientApi.CreatePatientAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var patientId = createResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created: {patientId}");

        // Step 2: Read
        var getResponse = await PatientApi.GetPatientByIdAsync(patientId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.Name.Should().Be(input.Name);

        // Step 3: Update
        var updateInput = CreateTestPatientInput("lc_upd");
        var updateResponse = await PatientApi.UpdatePatientAsync(patientId, updateInput);
        updateResponse.Success.Should().BeTrue(updateResponse.Message);
        updateResponse.Data!.Name.Should().Be(updateInput.Name);
        _output.WriteLine($"[Lifecycle] Updated: {updateResponse.Data.Name}");

        // Step 4: Soft delete
        var deleteResponse = await PatientApi.DeletePatientAsync(patientId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine("[Lifecycle] Deleted");

        // Step 5: Restore
        var restoreResponse = await PatientApi.RestoreAsync(patientId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine("[Lifecycle] Restored");

        // Step 6: Verify accessible after restore
        var finalGet = await PatientApi.GetPatientByIdAsync(patientId);
        finalGet.Success.Should().BeTrue(finalGet.Message);
        _output.WriteLine($"[Lifecycle] Final verification OK: {finalGet.Data!.Id}");
    }

    #endregion
}
