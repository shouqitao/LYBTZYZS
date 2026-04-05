using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class MedicalCaseTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public MedicalCaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GenerateIdNumber()
    {
        var random = new Random();
        var body = $"110101199001{random.Next(10, 28):D2}{random.Next(100, 999)}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }

    private static string GeneratePhoneNumber()
    {
        var random = new Random();
        return $"1{random.Next(3, 10)}{random.Next(100000000, 999999999):D9}";
    }

    private async Task<Guid> CreateTestPatientAsync()
    {
        var input = new PatientInputDto
        {
            Name = $"医案患者_{Guid.NewGuid():N}".Substring(0, 15),
            PinYinCode = "YAHZ",
            IdNumber = GenerateIdNumber(),
            Gender = Gender.Male,
            PhoneNumber = GeneratePhoneNumber(),
            Address = "E2E测试地址"
        };
        var response = await PatientApi.CreatePatientAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return response.Data!.Id;
    }

    private async Task<(Guid HerbId, string HerbName)> CreateTestHerbAsync()
    {
        var input = new HerbInputDto
        {
            Name = $"医案药材_{Guid.NewGuid():N}".Substring(0, 15),
            Unit = "克",
            Price = 12m
        };
        var response = await HerbApi.CreateHerbAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return (response.Data!.Id, response.Data.Name);
    }

    private MedicalCaseInputDto CreateTestCaseInput(Guid patientId, Guid userId) => new()
    {
        PatientId = patientId,
        UserId = userId,
        Consultation = new ConsultationInputDto
        {
            PresentIllness = "头痛发热三日",
            TongueDiagnosis = "舌红苔黄",
            PulseDiagnosis = "脉浮数",
            TcmDiagnosis = "外感风热证"
        }
    };

    #region CRUD

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task CreateMedicalCase_WithConsultation_ReturnsCreatedCase()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var input = CreateTestCaseInput(patientId, loginResponse.User.Id);

        var response = await MedicalCaseApi.CreateMedicalCaseAsync(input);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Created medical case: {response.Data!.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task GetMedicalCaseById_ExistingCase_ReturnsDetail()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(caseId);
        _output.WriteLine($"Retrieved case: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task GetMedicalCases_WithPagination_ReturnsPagedResult()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));

        var response = await MedicalCaseApi.GetMedicalCasesAsync(1, 10);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        _output.WriteLine($"Cases page: {response.Data.Items.Count}/{response.Data.TotalCount}");
    }

    #endregion

    #region Save (Aggregate)

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task SaveMedicalCase_UpdateConsultation_Succeeds()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var saveInput = CreateTestCaseInput(patientId, loginResponse.User.Id);
        saveInput.Id = caseId;
        saveInput.Consultation!.TcmDiagnosis = "外感风寒证（更新）";

        var response = await MedicalCaseApi.SaveAsync(caseId, saveInput);

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Saved case {caseId} with updated consultation");
    }

    #endregion

    #region Query & Search

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task QueryMedicalCases_ByPatient_ReturnsMatchingCases()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));

        var response = await MedicalCaseApi.QueryMedicalCasesAsync(
            queryType: MedicalCaseQueryType.ByPatient,
            patientId: patientId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        _output.WriteLine($"Query by patient: {response.Data.Items.Count} cases found");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task SearchMedicalCases_ByDiagnosisKeyword_ReturnsMatchingCases()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var input = CreateTestCaseInput(patientId, loginResponse.User.Id);
        input.Consultation!.TcmDiagnosis = "风热感冒测试诊断";
        await MedicalCaseApi.CreateMedicalCaseAsync(input);

        var response = await MedicalCaseApi.SearchMedicalCasesAsync(
            diagnosisKeyword: "风热感冒");

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Search results: {response.Data!.Items.Count} cases found");
    }

    #endregion

    #region Prescription

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task SetPrescriptionFlag_ToggleFlag_Succeeds()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.SetPrescriptionFlagAsync(
            caseId,
            new SetPrescriptionFlagRequest { NeedsPrescription = true });

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Set prescription flag for case {caseId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task SaveMedicalCase_WithPrescription_Succeeds()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (herbId, herbName) = await CreateTestHerbAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var saveInput = CreateTestCaseInput(patientId, loginResponse.User.Id);
        saveInput.NeedsPrescription = true;
        saveInput.Prescription = new PrescriptionInputDto
        {
            MedicalCaseId = caseId,
            DosageCount = 7,
            Discount = 1.0m,
            TotalPrice = 84m,
            Items = new List<PrescriptionItemInputDto>
            {
                new()
                {
                    HerbId = herbId,
                    HerbName = herbName,
                    Unit = "克",
                    Dosage = 10,
                    UnitPrice = 12m,
                    Subtotal = 84m
                }
            }
        };
        saveInput.Id = caseId;

        var response = await MedicalCaseApi.SaveAsync(caseId, saveInput);

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Saved case {caseId} with prescription");
    }

    #endregion

    #region Lifecycle Operations

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task CloseCase_ActiveCase_ClosesSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.CloseCaseAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Closed case {caseId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task SuspendCase_ActiveCase_SuspendsSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.SuspendAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Suspended case {caseId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task CancelMedicalCase_ActiveCase_CancelsSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var cancelResponse = await MedicalCaseApi.CancelMedicalCaseAsync(
            caseId,
            new CancelMedicalCaseRequestDto { Reason = "患者取消就诊" });

        cancelResponse.IsSuccessStatusCode.Should().BeTrue();
        _output.WriteLine($"Cancelled case {caseId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task UpdateStatus_ActiveToSuspended_UpdatesSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.UpdateStatusAsync(
            caseId,
            new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Suspended });

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Updated status for case {caseId}");
    }

    #endregion

    #region Pending Cases

    [Fact(Skip = "Obsolete endpoint migrated to /query, skip for now")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task GetPendingCases_ReturnsListSuccessfully()
    {
        await LoginAsSysadminAsync();

        var response = await MedicalCaseApi.GetPendingCasesAsync();

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Pending cases: {response.Data!.Count}");
    }

    #endregion

    #region Permissions & Audit

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task GetPermissions_ExistingCase_ReturnsPermissions()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.GetPermissionsAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Permissions for case {caseId}: {response.Data}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task GetAuditLogs_ExistingCase_ReturnsAuditLogs()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.GetAuditLogsAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Audit logs for case {caseId}: {response.Data!.Logs.Count} entries");
    }

    #endregion

    #region Print Operations

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task RecordPrintCompleted_ExistingCase_RecordsSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.RecordPrintCompletedAsync(
            caseId,
            new PrintCompletedRequest { PrintType = PrintType.Prescription });

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Recorded print completion for case {caseId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task AddPrintLog_ExistingCase_AddsSuccessfully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.AddPrintLogAsync(
            caseId,
            new PrintLogInputDto
            {
                PrintType = PrintType.Prescription,
                IsSuccess = true,
                PrinterName = "TestPrinter"
            });

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Added print log for case {caseId}");
    }

    #endregion

    #region Delete & Batch Delete

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task DeleteMedicalCase_ExistingCase_Succeeds()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var response = await MedicalCaseApi.DeleteMedicalCaseAsync(caseId);

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Deleted case {caseId}");
    }

    [Fact(Skip = "Cannot create multiple active cases for same patient — business rule")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task BatchDelete_MultipleCases_ReturnsOperationResult()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var c1 = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        var c2 = await MedicalCaseApi.CreateMedicalCaseAsync(
            CreateTestCaseInput(patientId, loginResponse.User.Id));
        c1.Success.Should().BeTrue(c1.Message);
        c2.Success.Should().BeTrue(c2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { c1.Data!.Id, c2.Data!.Id }
        };

        var response = await MedicalCaseApi.BatchDeleteAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch deleted: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "MedicalCaseManagement")]
    public async Task MedicalCaseFullLifecycle_CreateSavePrescriptionClose_AllSucceed()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var (herbId, herbName) = await CreateTestHerbAsync();

        // Step 1: Create case with consultation
        var input = CreateTestCaseInput(patientId, loginResponse.User.Id);
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created case: {caseId}");

        // Step 2: Read and verify
        var getResponse = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        getResponse.Success.Should().BeTrue(getResponse.Message);

        // Step 3: Save with updated consultation
        var saveInput = CreateTestCaseInput(patientId, loginResponse.User.Id);
        saveInput.Id = caseId;
        saveInput.Consultation!.TcmDiagnosis = "气虚血瘀证";
        var saveResponse = await MedicalCaseApi.SaveAsync(caseId, saveInput);
        saveResponse.Success.Should().BeTrue(saveResponse.Message);
        _output.WriteLine("[Lifecycle] Updated consultation");

        // Step 4: Add prescription
        saveInput.NeedsPrescription = true;
        saveInput.Prescription = new PrescriptionInputDto
        {
            MedicalCaseId = caseId,
            DosageCount = 7,
            Discount = 1.0m,
            TotalPrice = 84m,
            Items = new List<PrescriptionItemInputDto>
            {
                new()
                {
                    HerbId = herbId,
                    HerbName = herbName,
                    Unit = "克",
                    Dosage = 10,
                    UnitPrice = 12m,
                    Subtotal = 84m
                }
            }
        };
        var prescriptionResponse = await MedicalCaseApi.SaveAsync(caseId, saveInput);
        prescriptionResponse.Success.Should().BeTrue(prescriptionResponse.Message);
        _output.WriteLine("[Lifecycle] Added prescription");

        // Step 5: Close case
        var closeResponse = await MedicalCaseApi.CloseCaseAsync(caseId);
        closeResponse.Success.Should().BeTrue(closeResponse.Message);
        _output.WriteLine("[Lifecycle] Case closed");

        // Step 6: Final verification
        var finalGet = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        finalGet.Success.Should().BeTrue(finalGet.Message);
        _output.WriteLine($"[Lifecycle] Final verification OK: {finalGet.Data!.Id}");
    }

    #endregion
}
