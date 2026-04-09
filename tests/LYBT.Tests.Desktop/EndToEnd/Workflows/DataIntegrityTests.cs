using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Workflows;

[Collection("E2E")]
public class DataIntegrityTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public DataIntegrityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "DataIntegrity")]
    public async Task DeleteHerbUsedInFormula_ShouldHandleGracefully()
    {
        await LoginAsSysadminAsync();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var herbInput = new HerbInputDto
        {
            Name = $"待删除草药_{suffix}",
            PinYinCode = $"dsc_{suffix}",
            Unit = "克",
            Price = 10.0m
        };

        var herbResponse = await HerbApi.CreateHerbAsync(herbInput);
        herbResponse.Success.Should().BeTrue();
        var herbId = herbResponse.Data!.Id;

        var formulaInput = new FormulaInputDto
        {
            Name = $"引用方_{suffix}",
            Effect = "测试引用",
            Usage = "测试",
            Category = "测试",
            Herbs =
            [
                new FormulaHerbItemInputDto
                {
                    HerbId = herbId,
                    HerbName = herbInput.Name,
                    Dosage = 10,
                    Unit = "克",
                    SortOrder = 1,
                    DecocteMethod = DecocteMethod.Default
                }
            ]
        };

        var formulaResponse = await FormulaApi.CreateFormulaAsync(formulaInput);
        formulaResponse.Success.Should().BeTrue();
        var formulaId = formulaResponse.Data!.Id;
        _output.WriteLine($"Herb {herbId} referenced by formula {formulaId}");

        var deleteResponse = await HerbApi.DeleteHerbAsync(herbId);
        _output.WriteLine($"Delete herb result: Success={deleteResponse.Success}, Message={deleteResponse.Message}");

        var formula = await FormulaApi.GetFormulaByIdAsync(formulaId);
        formula.Success.Should().BeTrue();
        _output.WriteLine($"Formula still accessible after herb deletion. Herbs count: {formula.Data!.Herbs?.Count ?? 0}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "DataIntegrity")]
    public async Task DeletePatientWithMedicalCases_ShouldHandleGracefully()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var patient = await CreateTestPatientAsync();
        _output.WriteLine($"Created patient: {patient.Id}");

        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patient.Id,
            UserId = userId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试数据完整性",
                TcmDiagnosis = "测试证型"
            }
        };

        var caseResponse = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        caseResponse.Success.Should().BeTrue(caseResponse.Message);
        var caseId = caseResponse.Data!.Id;
        _output.WriteLine($"Created case {caseId} for patient {patient.Id}");

        var deleteResponse = await PatientApi.DeletePatientAsync(patient.Id);
        _output.WriteLine($"Delete patient result: Success={deleteResponse.Success}, Message={deleteResponse.Message}");

        var fetchedCase = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        _output.WriteLine($"Case after patient deletion: Success={fetchedCase.Success}, PatientId={fetchedCase.Data?.PatientId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "DataIntegrity")]
    public async Task ClosedCase_RejectModification_DataPreserved()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var patient = await CreateTestPatientAsync();
        var herb = await CreateTestHerbAsync();

        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patient.Id,
            UserId = userId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试关闭保护",
                TongueDiagnosis = "舌红",
                PulseDiagnosis = "脉数",
                TcmDiagnosis = "热证"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = Guid.Empty,
                DosageCount = 3,
                Discount = 1.0m,
                TotalPrice = 0m,
                Items =
                [
                    new PrescriptionItemInputDto
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        Unit = "克",
                        Dosage = 10,
                        UnitPrice = 15.0m,
                        Subtotal = 150m
                    }
                ]
            }
        };

        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var caseId = createResponse.Data!.Id;

        var closeResponse = await MedicalCaseApi.CloseCaseAsync(caseId);
        closeResponse.Success.Should().BeTrue(closeResponse.Message);
        _output.WriteLine($"Case {caseId} closed successfully");

        var modifyInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patient.Id,
            UserId = userId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "修改后的主诉",
                TcmDiagnosis = "修改后的诊断"
            }
        };

        var modifyResponse = await MedicalCaseApi.SaveAsync(caseId, modifyInput);
        modifyResponse.Success.Should().BeFalse("closed case should reject modifications");
        _output.WriteLine($"Modify closed case: Success={modifyResponse.Success}, Message={modifyResponse.Message}");

        var preserved = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        preserved.Success.Should().BeTrue();
        preserved.Data!.Consultation!.PresentIllness.Should().Be("测试关闭保护",
            "original consultation data should be preserved after rejected modification");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "DataIntegrity")]
    public async Task MultipleRegistrations_SamePatient_IndependentCases()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var patient = await CreateTestPatientAsync();

        var reg1Input = new RegistrationInputDto
        {
            PatientId = patient.Id,
            PatientName = patient.Name,
            DoctorId = userId,
            DoctorName = "系统管理员"
        };

        var reg1 = await RegistrationApi.CreateAsync(reg1Input);
        reg1.Success.Should().BeTrue(reg1.Message);
        var visit1 = await RegistrationApi.StartVisitAsync(reg1.Data!.Id);
        visit1.Success.Should().BeTrue(visit1.Message);
        var case1Id = visit1.Data;
        _output.WriteLine($"Visit 1: registration {reg1.Data.Id} -> case {case1Id}");

        var reg2Input = new RegistrationInputDto
        {
            PatientId = patient.Id,
            PatientName = patient.Name,
            DoctorId = userId,
            DoctorName = "系统管理员"
        };

        var reg2 = await RegistrationApi.CreateAsync(reg2Input);
        reg2.Success.Should().BeTrue(reg2.Message);
        var visit2 = await RegistrationApi.StartVisitAsync(reg2.Data!.Id);
        visit2.Success.Should().BeTrue(visit2.Message);
        var case2Id = visit2.Data;
        _output.WriteLine($"Visit 2: registration {reg2.Data.Id} -> case {case2Id}");

        case1Id.Should().NotBe(case2Id, "each visit should create an independent case");

        var case1 = await MedicalCaseApi.GetMedicalCaseByIdAsync(case1Id);
        var case2 = await MedicalCaseApi.GetMedicalCaseByIdAsync(case2Id);
        case1.Success.Should().BeTrue();
        case2.Success.Should().BeTrue();
        case1.Data!.PatientId.Should().Be(patient.Id);
        case2.Data!.PatientId.Should().Be(patient.Id);
        case1.Data.CaseNumber.Should().NotBe(case2.Data!.CaseNumber, "each case should have unique case number");
        _output.WriteLine("Verified: same patient has independent cases with unique case numbers");
    }

    private async Task<(Guid Id, string Name)> CreateTestPatientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var input = new PatientInputDto
        {
            Name = $"完整性测试_{suffix}",
            Gender = Gender.Male,
            IdNumber = GenerateIdNumber(),
            PhoneNumber = $"139{Interlocked.Increment(ref _counter):D8}",
            Address = "北京市测试区测试街道1号",
            PinYinCode = $"wzxcs_{suffix}"
        };

        var response = await PatientApi.CreatePatientAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return (response.Data!.Id, input.Name);
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

    private async Task<(Guid Id, string Name)> CreateTestHerbAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var input = new HerbInputDto
        {
            Name = $"测试草药_{suffix}",
            PinYinCode = $"cscy_{suffix}",
            Unit = "克",
            Price = 15.0m
        };

        var response = await HerbApi.CreateHerbAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return (response.Data!.Id, input.Name);
    }
}
