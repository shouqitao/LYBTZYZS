using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Roles;

public class WorkflowIntegrationTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public WorkflowIntegrationTests(ITestOutputHelper output)
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

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Workflow")]
    [Trait("Phase", "Integration")]
    public async Task ReceptionistToDoctor_CreateRegistrationAndStartVisit()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create a patient (as Receptionist/Admin)
        var patientInput = new PatientInputDto
        {
            Name = $"工作流患者_{Guid.NewGuid():N}".Substring(0, 15),
            PinYinCode = "GZLHZ",
            IdNumber = GenerateIdNumber(),
            Gender = Gender.Male,
            PhoneNumber = GeneratePhoneNumber(),
            Address = "工作流测试地址"
        };
        var patientResponse = await PatientApi.CreatePatientAsync(patientInput);
        patientResponse.Success.Should().BeTrue(patientResponse.Message);
        var patientId = patientResponse.Data!.Id;
        _output.WriteLine($"[Workflow] Created patient: {patientId}");

        // Step 2: Create a registration (as Receptionist)
        var registrationInput = new RegistrationInputDto
        {
            PatientId = patientId,
            Remark = "工作流测试挂号"
        };
        var registrationResponse = await RegistrationApi.CreateAsync(registrationInput);
        registrationResponse.Success.Should().BeTrue(registrationResponse.Message);
        var registrationId = registrationResponse.Data!.Id;
        _output.WriteLine($"[Workflow] Created registration: {registrationId}");

        // Step 3: Get registration queue
        var queueResponse = await RegistrationApi.GetQueueAsync();
        queueResponse.Success.Should().BeTrue(queueResponse.Message);
        queueResponse.Data.Should().NotBeNull();
        _output.WriteLine($"[Workflow] Queue has {queueResponse.Data!.Count} items");

        // Step 4: Start visit (as Doctor)
        var startVisitResponse = await RegistrationApi.StartVisitAsync(registrationId);
        startVisitResponse.Success.Should().BeTrue(startVisitResponse.Message);
        _output.WriteLine($"[Workflow] Started visit for registration: {registrationId}");

        // Step 5: Verify registration status changed
        var getResponse = await RegistrationApi.GetByIdAsync(registrationId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.Status.Should().Be(RegistrationStatus.InProgress);
        _output.WriteLine($"[Workflow] Registration status: {getResponse.Data.Status}");

        _output.WriteLine("[Workflow] Receptionist→Doctor workflow completed successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Workflow")]
    [Trait("Phase", "Integration")]
    public async Task ReceptionistToDoctor_CancelRegistration()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create a patient
        var patientInput = new PatientInputDto
        {
            Name = $"取消患者_{Guid.NewGuid():N}".Substring(0, 15),
            PinYinCode = "QXHZ",
            IdNumber = GenerateIdNumber(),
            Gender = Gender.Female,
            PhoneNumber = GeneratePhoneNumber(),
            Address = "取消测试地址"
        };
        var patientResponse = await PatientApi.CreatePatientAsync(patientInput);
        patientResponse.Success.Should().BeTrue(patientResponse.Message);
        var patientId = patientResponse.Data!.Id;

        // Step 2: Create a registration
        var registrationInput = new RegistrationInputDto
        {
            PatientId = patientId,
            Remark = "取消测试挂号"
        };
        var registrationResponse = await RegistrationApi.CreateAsync(registrationInput);
        registrationResponse.Success.Should().BeTrue(registrationResponse.Message);
        var registrationId = registrationResponse.Data!.Id;
        _output.WriteLine($"[Workflow] Created registration for cancellation: {registrationId}");

        // Step 3: Cancel registration
        var cancelResponse = await RegistrationApi.CancelAsync(registrationId);
        cancelResponse.Success.Should().BeTrue(cancelResponse.Message);
        _output.WriteLine($"[Workflow] Cancelled registration: {registrationId}");

        // Step 4: Verify status
        var getResponse = await RegistrationApi.GetByIdAsync(registrationId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.Status.Should().Be(RegistrationStatus.Cancelled);
        _output.WriteLine($"[Workflow] Registration status after cancel: {getResponse.Data.Status}");

        _output.WriteLine("[Workflow] Cancellation workflow completed successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Workflow")]
    [Trait("Phase", "Integration")]
    public async Task FullLifecycle_PatientRegistrationToMedicalCase()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create patient
        var patientInput = new PatientInputDto
        {
            Name = $"完整流程患者_{Guid.NewGuid():N}".Substring(0, 15),
            PinYinCode = "WZLCHZ",
            IdNumber = GenerateIdNumber(),
            Gender = Gender.Male,
            PhoneNumber = GeneratePhoneNumber(),
            Address = "完整流程测试地址"
        };
        var patientResponse = await PatientApi.CreatePatientAsync(patientInput);
        patientResponse.Success.Should().BeTrue(patientResponse.Message);
        var patientId = patientResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created patient: {patientId}");

        // Step 2: Create registration
        var registrationInput = new RegistrationInputDto
        {
            PatientId = patientId,
            Remark = "完整流程测试"
        };
        var registrationResponse = await RegistrationApi.CreateAsync(registrationInput);
        registrationResponse.Success.Should().BeTrue(registrationResponse.Message);
        var registrationId = registrationResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created registration: {registrationId}");

        // Step 3: Start visit
        var startVisitResponse = await RegistrationApi.StartVisitAsync(registrationId);
        startVisitResponse.Success.Should().BeTrue(startVisitResponse.Message);
        _output.WriteLine($"[Lifecycle] Started visit");

        // Step 4: Create medical case
        var medicalCaseInput = new LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = Guid.Parse("cddaf790-f68e-4dc9-833e-05188310ee07"), // sysadmin ID
            Consultation = new LYBT.Shared.Models.Contracts.Consultation.ConsultationInputDto
            {
                PresentIllness = "头痛发热",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "脉浮数",
                TcmDiagnosis = "外感风热证"
            }
        };
        var medicalCaseResponse = await MedicalCaseApi.CreateMedicalCaseAsync(medicalCaseInput);
        medicalCaseResponse.Success.Should().BeTrue(medicalCaseResponse.Message);
        var medicalCaseId = medicalCaseResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created medical case: {medicalCaseId}");

        // Step 5: Close case
        var closeResponse = await MedicalCaseApi.CloseCaseAsync(medicalCaseId);
        closeResponse.Success.Should().BeTrue(closeResponse.Message);
        _output.WriteLine($"[Lifecycle] Closed medical case");

        _output.WriteLine("[Lifecycle] Full patient registration → medical case workflow completed");
    }
}
