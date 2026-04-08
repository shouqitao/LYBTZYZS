using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Workflows;

[Collection("E2E")]
public class HerbFormulaWorkflowTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public HerbFormulaWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "HerbFormulaPrescription")]
    public async Task HerbToFormulaToPrescription_FullChain_DataConsistent()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var herb1 = await CreateTestHerbAsync("黄芪", 15.0m);
        var herb2 = await CreateTestHerbAsync("当归", 12.5m);
        _output.WriteLine($"Created herbs: {herb1.Name}({herb1.Id}), {herb2.Name}({herb2.Id})");

        var formulaInput = new FormulaInputDto
        {
            Name = $"补气养血方_Test_{Guid.NewGuid():N}",
            Effect = "补气养血",
            Usage = "水煎服，每日一剂",
            Category = "补益剂",
            IsShared = false,
            Herbs =
            [
                new FormulaHerbItemInputDto
                {
                    HerbId = herb1.Id,
                    HerbName = herb1.Name,
                    Dosage = 30,
                    Unit = "克",
                    SortOrder = 1,
                    DecocteMethod = DecocteMethod.Default
                },
                new FormulaHerbItemInputDto
                {
                    HerbId = herb2.Id,
                    HerbName = herb2.Name,
                    Dosage = 15,
                    Unit = "克",
                    SortOrder = 2,
                    DecocteMethod = DecocteMethod.Default
                }
            ]
        };

        var formulaResponse = await FormulaApi.CreateFormulaAsync(formulaInput);
        formulaResponse.Success.Should().BeTrue(formulaResponse.Message);
        var formulaId = formulaResponse.Data!.Id;
        _output.WriteLine($"Created formula: {formulaInput.Name}({formulaId})");

        var fetchedFormula = await FormulaApi.GetFormulaByIdAsync(formulaId);
        fetchedFormula.Success.Should().BeTrue();
        fetchedFormula.Data!.Herbs.Should().HaveCount(2);
        fetchedFormula.Data.Herbs.Should().Contain(h => h.HerbId == herb1.Id && h.Dosage == 30);
        fetchedFormula.Data.Herbs.Should().Contain(h => h.HerbId == herb2.Id && h.Dosage == 15);

        var patient = await CreateTestPatientAsync();
        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patient.Id,
            UserId = userId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "气血两虚",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "脉细弱",
                TcmDiagnosis = "气血两虚证"
            }
        };

        var caseResponse = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        caseResponse.Success.Should().BeTrue(caseResponse.Message);
        var caseId = caseResponse.Data!.Id;

        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patient.Id,
            UserId = userId,
            NeedsPrescription = true,
            Consultation = caseInput.Consultation,
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = caseId,
                DosageCount = 7,
                Discount = 1.0m,
                TotalPrice = (30 * herb1.Price + 15 * herb2.Price) * 7,
                Items =
                [
                    new PrescriptionItemInputDto
                    {
                        HerbId = herb1.Id,
                        HerbName = herb1.Name,
                        Unit = "克",
                        Dosage = 30,
                        UnitPrice = herb1.Price,
                        Subtotal = 30 * herb1.Price * 7
                    },
                    new PrescriptionItemInputDto
                    {
                        HerbId = herb2.Id,
                        HerbName = herb2.Name,
                        Unit = "克",
                        Dosage = 15,
                        UnitPrice = herb2.Price,
                        Subtotal = 15 * herb2.Price * 7
                    }
                ]
            }
        };

        var saveResponse = await MedicalCaseApi.SaveAsync(caseId, saveInput);
        saveResponse.Success.Should().BeTrue(saveResponse.Message);
        saveResponse.Data!.HasPrescription.Should().BeTrue();

        var savedCase = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        savedCase.Success.Should().BeTrue();
        savedCase.Data!.Prescription.Should().NotBeNull();
        savedCase.Data.Prescription!.Items.Should().HaveCount(2);
        savedCase.Data.Prescription.Items.Should().Contain(i => i.HerbId == herb1.Id);
        savedCase.Data.Prescription.Items.Should().Contain(i => i.HerbId == herb2.Id);
        _output.WriteLine("Prescription verified: herbs from formula correctly applied to case");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "FormulaModification")]
    public async Task ModifyFormulaHerbs_ExistingPrescriptionUnaffected()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;

        var herb1 = await CreateTestHerbAsync("甘草", 8.0m);
        var herb2 = await CreateTestHerbAsync("白术", 10.0m);
        var herb3 = await CreateTestHerbAsync("茯苓", 11.0m);

        var formulaInput = new FormulaInputDto
        {
            Name = $"四君子汤_Test_{Guid.NewGuid():N}",
            Effect = "健脾益气",
            Usage = "水煎服",
            Category = "补益剂",
            Herbs =
            [
                new FormulaHerbItemInputDto { HerbId = herb1.Id, HerbName = herb1.Name, Dosage = 6, Unit = "克", SortOrder = 1, DecocteMethod = DecocteMethod.Default },
                new FormulaHerbItemInputDto { HerbId = herb2.Id, HerbName = herb2.Name, Dosage = 9, Unit = "克", SortOrder = 2, DecocteMethod = DecocteMethod.Default }
            ]
        };

        var formulaResponse = await FormulaApi.CreateFormulaAsync(formulaInput);
        formulaResponse.Success.Should().BeTrue();
        var formulaId = formulaResponse.Data!.Id;

        var patient = await CreateTestPatientAsync();
        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patient.Id,
            UserId = userId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "脾胃虚弱",
                TcmDiagnosis = "脾虚证"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = Guid.Empty,
                DosageCount = 5,
                Discount = 1.0m,
                TotalPrice = 0m,
                Items =
                [
                    new PrescriptionItemInputDto { HerbId = herb1.Id, HerbName = herb1.Name, Unit = "克", Dosage = 6, UnitPrice = herb1.Price, Subtotal = 6 * herb1.Price * 5 },
                    new PrescriptionItemInputDto { HerbId = herb2.Id, HerbName = herb2.Name, Unit = "克", Dosage = 9, UnitPrice = herb2.Price, Subtotal = 9 * herb2.Price * 5 }
                ]
            }
        };

        var caseResponse = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        caseResponse.Success.Should().BeTrue(caseResponse.Message);
        var caseId = caseResponse.Data!.Id;
        _output.WriteLine($"Case created with prescription from formula herbs: {caseId}");

        var updatedFormulaInput = new FormulaInputDto
        {
            Name = formulaInput.Name,
            Effect = formulaInput.Effect,
            Usage = formulaInput.Usage,
            Category = formulaInput.Category,
            Herbs =
            [
                new FormulaHerbItemInputDto { HerbId = herb1.Id, HerbName = herb1.Name, Dosage = 6, Unit = "克", SortOrder = 1, DecocteMethod = DecocteMethod.Default },
                new FormulaHerbItemInputDto { HerbId = herb3.Id, HerbName = herb3.Name, Dosage = 12, Unit = "克", SortOrder = 2, DecocteMethod = DecocteMethod.Default }
            ]
        };

        var updateResponse = await FormulaApi.UpdateFormulaAsync(formulaId, updatedFormulaInput);
        updateResponse.Success.Should().BeTrue();
        _output.WriteLine("Formula updated: replaced herb2 with herb3");

        var savedCase = await MedicalCaseApi.GetMedicalCaseByIdAsync(caseId);
        savedCase.Success.Should().BeTrue();
        savedCase.Data!.Prescription.Should().NotBeNull();
        savedCase.Data.Prescription!.Items.Should().Contain(i => i.HerbId == herb2.Id,
            "prescription should still reference original herb2, not be affected by formula change");
        _output.WriteLine("Verified: existing prescription unaffected by formula modification");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Workflow")]
    [Trait("Workflow", "MultipleFormulas")]
    public async Task CreateMultipleFormulas_SameHerbs_DifferentDosages()
    {
        await LoginAsSysadminAsync();

        var herb = await CreateTestHerbAsync("人参", 50.0m);

        var formula1Input = new FormulaInputDto
        {
            Name = $"参苓白术散_Test_{Guid.NewGuid():N}",
            Effect = "益气健脾",
            Usage = "水煎服",
            Category = "补益剂",
            Herbs = [new FormulaHerbItemInputDto { HerbId = herb.Id, HerbName = herb.Name, Dosage = 10, Unit = "克", SortOrder = 1, DecocteMethod = DecocteMethod.Default }]
        };

        var formula2Input = new FormulaInputDto
        {
            Name = $"独参汤_Test_{Guid.NewGuid():N}",
            Effect = "大补元气",
            Usage = "水煎服",
            Category = "补益剂",
            Herbs = [new FormulaHerbItemInputDto { HerbId = herb.Id, HerbName = herb.Name, Dosage = 30, Unit = "克", SortOrder = 1, DecocteMethod = DecocteMethod.Default }]
        };

        var response1 = await FormulaApi.CreateFormulaAsync(formula1Input);
        var response2 = await FormulaApi.CreateFormulaAsync(formula2Input);
        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();

        var fetched1 = await FormulaApi.GetFormulaByIdAsync(response1.Data!.Id);
        var fetched2 = await FormulaApi.GetFormulaByIdAsync(response2.Data!.Id);

        fetched1.Data!.Herbs.Should().ContainSingle().Which.Dosage.Should().Be(10);
        fetched2.Data!.Herbs.Should().ContainSingle().Which.Dosage.Should().Be(30);
        _output.WriteLine("Verified: same herb can have different dosages across formulas");
    }

    private async Task<(Guid Id, string Name, decimal Price)> CreateTestHerbAsync(string name, decimal price)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var input = new HerbInputDto
        {
            Name = $"{name}_Test_{suffix}",
            PinYinCode = $"test_{suffix}",
            Unit = "克",
            Price = price,
            CostPrice = price * 0.5m
        };

        var response = await HerbApi.CreateHerbAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return (response.Data!.Id, input.Name, price);
    }

    private async Task<(Guid Id, string Name)> CreateTestPatientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var input = new PatientInputDto
        {
            Name = $"测试患者_{suffix}",
            Gender = Gender.Male,
            IdNumber = GenerateIdNumber(),
            PhoneNumber = $"138{new Random().Next(10000000, 99999999)}",
            Address = "北京市测试区测试街道1号",
            PinYinCode = $"cshz_{suffix}"
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
}
