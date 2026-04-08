using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Auth;
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

    private static int _counter = 0;

    private static string GenerateIdNumber()
    {
        var unique = Interlocked.Increment(ref _counter);
        var day = 10 + (unique % 18);
        var seq = 100 + (unique % 899);
        var body = $"110101199001{day:D2}{seq:D3}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }

    private static string GeneratePhoneNumber()
    {
        var unique = Interlocked.Increment(ref _counter);
        return $"1{3 + (unique % 7)}{unique:D9}";
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Workflow")]
    [Trait("Phase", "Integration")]
    public async Task ReceptionistToDoctor_CreateRegistrationAndStartVisit()
    {
        // Step 0: Get doctor info first (need a valid doctor for registration)
        await LoginAsDoctorAsync();
        var doctorId = CurrentUser!.User.Id;
        var doctorName = CurrentUser.User.RealName;
        _output.WriteLine($"[Workflow] Using doctor: {doctorName} ({doctorId})");

        // Step 1: Login as Receptionist and create a patient
        await LoginAsReceptionistAsync();

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
            PatientName = patientResponse.Data!.Name,
            DoctorId = doctorId,
            DoctorName = doctorName,
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

        // Step 4: Login as Doctor and start visit
        await LoginAsDoctorAsync();
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
        // Step 0: Get doctor info first
        await LoginAsDoctorAsync();
        var doctorId = CurrentUser!.User.Id;
        var doctorName = CurrentUser.User.RealName;

        // Step 1: Login as Receptionist and create a patient
        await LoginAsReceptionistAsync();
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
            PatientName = patientResponse.Data!.Name,
            DoctorId = doctorId,
            DoctorName = doctorName,
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
        // Step 0: Get doctor info first
        await LoginAsDoctorAsync();
        var doctorId = CurrentUser!.User.Id;
        var doctorName = CurrentUser.User.RealName;

        // Step 1: Login as Receptionist and create patient
        await LoginAsReceptionistAsync();
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
            PatientName = patientResponse.Data!.Name,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Remark = "完整流程测试"
        };
        var registrationResponse = await RegistrationApi.CreateAsync(registrationInput);
        registrationResponse.Success.Should().BeTrue(registrationResponse.Message);
        var registrationId = registrationResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created registration: {registrationId}");
        await LoginAsDoctorAsync();
        _output.WriteLine($"[Lifecycle] Logged in as doctor, Role={CurrentUser?.User.Role}");
        var startVisitResponse = await RegistrationApi.StartVisitAsync(registrationId);
        startVisitResponse.Success.Should().BeTrue(startVisitResponse.Message);
        _output.WriteLine($"[Lifecycle] Started visit");

        // Step 4: Create medical case
        var medicalCaseInput = new LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorId,
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
