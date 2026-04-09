using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Workflows;

[Trait("Category", "E2E")]
[Trait("Phase", "Workflow")]
public class PatientVisitWorkflowTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public PatientVisitWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Workflow", "FullVisit")]
    public async Task FullClinicalVisit_PatientToClosedCase_AllStatesVerified()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var patientInput = new PatientInputDto
        {
            Name = $"WF_Patient_{Guid.NewGuid():N}",
            Gender = Gender.Male,
            IdNumber = GenerateIdNumber(),
            PhoneNumber = GeneratePhoneNumber(),
            PinYinCode = "WFHZ",
            Address = "Workflow Test Address"
        };
        var patientResponse = await PatientApi.CreatePatientAsync(patientInput);
        var patient = E2EAssertionHelpers.AssertSuccess(patientResponse);
        _output.WriteLine($"Created patient: {patient.Id}");

        var regInput = new RegistrationInputDto
        {
            PatientId = patient.Id,
            PatientName = patientInput.Name,
            DoctorId = userId,
            DoctorName = loginResponse.User.RealName ?? "Sysadmin"
        };
        var regResponse = await RegistrationApi.CreateAsync(regInput);
        var registration = E2EAssertionHelpers.AssertSuccess(regResponse);
        _output.WriteLine($"Created registration: {registration.Id}");

        var startResponse = await RegistrationApi.StartVisitAsync(registration.Id);
        var medicalCaseId = E2EAssertionHelpers.AssertSuccess(startResponse);
        medicalCaseId.Should().NotBeEmpty("StartVisit should return a valid MedicalCase ID");
        _output.WriteLine($"Started visit, MedicalCase: {medicalCaseId}");

        var herbInput = new HerbInputDto
        {
            Name = $"WF_Herb_{Guid.NewGuid():N}",
            PinYinCode = "WFYC",
            Unit = "克",
            Price = 12.5m,
            CostPrice = 6.0m
        };
        var herbResponse = await HerbApi.CreateHerbAsync(herbInput);
        var herb = E2EAssertionHelpers.AssertSuccess(herbResponse);
        _output.WriteLine($"Created herb: {herb.Id}");

        var saveInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId,
            PatientId = patient.Id,
            UserId = userId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛三日，伴恶寒发热",
                TongueDiagnosis = "舌红苔薄白",
                PulseDiagnosis = "脉浮紧",
                TcmDiagnosis = "太阳伤寒证"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCaseId,
                DosageCount = 7,
                Discount = 1.0m,
                TotalPrice = 87.5m,
                Items =
                [
                    new PrescriptionItemInputDto
                    {
                        HerbId = herb.Id,
                        HerbName = herbInput.Name,
                        Unit = "克",
                        Dosage = 10,
                        UnitPrice = 12.5m,
                        Subtotal = 87.5m
                    }
                ]
            }
        };
        var saveResponse = await MedicalCaseApi.SaveAsync(medicalCaseId, saveInput);
        var savedCase = E2EAssertionHelpers.AssertSuccess(saveResponse);
        savedCase.HasPrescription.Should().BeTrue("case was saved with prescription");
        _output.WriteLine("Saved case with consultation and prescription");

        var getResponse = await MedicalCaseApi.GetMedicalCaseByIdAsync(medicalCaseId);
        var caseDetail = E2EAssertionHelpers.AssertSuccess(getResponse);
        caseDetail.Consultation.Should().NotBeNull("consultation should be persisted");
        caseDetail.Consultation!.TcmDiagnosis.Should().Be("太阳伤寒证");

        var closeResponse = await MedicalCaseApi.CloseCaseAsync(medicalCaseId);
        var closedCase = E2EAssertionHelpers.AssertSuccess(closeResponse);
        _output.WriteLine($"Case closed, status: {closedCase.CaseStatus}");

        var reSaveResponse = await MedicalCaseApi.SaveAsync(medicalCaseId, saveInput);
        reSaveResponse.Success.Should().BeFalse("saving a closed case should fail");
        _output.WriteLine($"Re-save on closed case failed as expected: {reSaveResponse.Message}");
    }

    [Fact]
    [Trait("Workflow", "Suspend")]
    public async Task SuspendAndResume_CaseStateTransitions()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var patientId = await CreateTestPatientAsync();
        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = userId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "暂挂测试",
                TcmDiagnosis = "待诊"
            }
        };
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        var createdCase = E2EAssertionHelpers.AssertSuccess(createResponse);
        var caseId = createdCase.Id;
        _output.WriteLine($"Created case: {caseId}");

        var suspendResponse = await MedicalCaseApi.SuspendAsync(caseId, null);
        var suspendedCase = E2EAssertionHelpers.AssertSuccess(suspendResponse);
        _output.WriteLine($"Suspended case, status: {suspendedCase.CaseStatus}");

        caseInput.Id = caseId;
        caseInput.Consultation!.PresentIllness = "暂挂后修改";
        var resumeResponse = await MedicalCaseApi.SaveAsync(caseId, caseInput);
        resumeResponse.Success.Should().BeTrue("saving a suspended case should resume it");
        _output.WriteLine("Resumed case by saving");
    }

    [Fact]
    [Trait("Workflow", "Cancel")]
    public async Task CancelVisit_RegistrationAndCaseCleanup()
    {
        var loginResponse = await LoginAsSysadminAsync();

        var patientId = await CreateTestPatientAsync();
        var regInput = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = "CancelTest",
            DoctorId = loginResponse.User.Id,
            DoctorName = loginResponse.User.RealName ?? "Sysadmin"
        };
        var regResponse = await RegistrationApi.CreateAsync(regInput);
        var registration = E2EAssertionHelpers.AssertSuccess(regResponse);

        var cancelResponse = await RegistrationApi.CancelAsync(registration.Id);
        cancelResponse.Success.Should().BeTrue("cancelling a pending registration should succeed");
        _output.WriteLine("Registration cancelled before visit started");

        try
        {
            var startResponse = await RegistrationApi.StartVisitAsync(registration.Id);
            startResponse.Success.Should().BeFalse("starting a cancelled registration should fail");
        }
        catch (Refit.ApiException ex)
        {
            _output.WriteLine($"StartVisit on cancelled registration threw {ex.StatusCode} as expected");
        }
    }

    private async Task<Guid> CreateTestPatientAsync()
    {
        var input = new PatientInputDto
        {
            Name = $"WF_P_{Guid.NewGuid():N}",
            Gender = Gender.Female,
            IdNumber = GenerateIdNumber(),
            PhoneNumber = GeneratePhoneNumber(),
            Address = "北京市测试区测试街道1号",
            PinYinCode = "WFP"
        };
        var response = await PatientApi.CreatePatientAsync(input);
        return E2EAssertionHelpers.AssertSuccess(response).Id;
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
}
