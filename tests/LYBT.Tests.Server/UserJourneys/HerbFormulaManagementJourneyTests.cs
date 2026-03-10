using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT Narrative 3: Herb and Formula management journey.
/// Create herbs, edit price, toggle status, create formulas with
/// deferred binding, share formulas across doctors, use formula herbs in prescription.
/// </summary>
[Collection("HerbFormula")]
public sealed class HerbFormulaManagementJourneyTests : JourneyTestBase<HerbFormulaFixture>
{
    private const string SecondDoctorPassword = "TestDoctor2025@";

    public HerbFormulaManagementJourneyTests(HerbFormulaFixture fixture) : base(fixture) { }

    [Fact]
    public async Task HerbFormulaManagement_Full_Journey()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = doctorData!.Id;

        // Step 1: Create herb with PinYin auto-generation
        var herbName = UniqueName("川芎");
        var (createHerbResponse, herb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = herbName, Unit = "克", Price = 1.2m });
        createHerbResponse.IsSuccessStatusCode.Should().BeTrue();
        herb!.PinYinCode.Should().NotBeNullOrEmpty();
        var herbId = herb.Id;

        // Step 2: Edit herb price
        var (editHerbResponse, editedHerb) = await PutAsync<HerbDetailDto>(admin, $"/api/v1/herbs/{herbId}",
            new HerbInputDto { Id = herbId, Name = herbName, Unit = "克", Price = 1.5m });
        editHerbResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        editedHerb!.Price.Should().Be(1.5m);

        // Step 3: Disable herb
        var toggleResponse = await admin.PostAsJsonAsync($"/api/v1/herbs/{herbId}/toggle-status", new { });
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var (_, disabledHerb) = await GetAsync<HerbDetailDto>(admin, $"/api/v1/herbs/{herbId}");
        disabledHerb!.Status.Should().Be(CommonStatus.Disabled);

        // Re-enable for formula use
        await admin.PostAsJsonAsync($"/api/v1/herbs/{herbId}/toggle-status", new { });

        // Create a second herb for formula
        var (_, herb2) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("白芍"), Unit = "克", Price = 0.9m });

        // Step 4: Create formula with deferred binding (HerbId=null)
        var formulaInput = new FormulaInputDto
        {
            Name = UniqueName("验方A"),
            Effect = "活血行气",
            Usage = "水煎服",
            IsShared = false,
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = herbName, Dosage = 10, Unit = "克" },
                new() { HerbName = "未绑定药材", Dosage = 5, Unit = "克" } // Deferred: HerbId=null
            }
        };
        var (createFormulaResponse, formula) = await PostAsync<FormulaDetailDto>(
            doctor, "/api/v1/formulas", formulaInput);
        createFormulaResponse.IsSuccessStatusCode.Should().BeTrue();
        formula!.Herbs.Should().HaveCount(2);
        var formulaId = formula.Id;

        // Step 5: Share formula + verify cross-doctor visibility
        var updateInput = new FormulaInputDto
        {
            Name = formula!.Name,
            Effect = formula.Effect ?? string.Empty,
            Usage = formula.Usage ?? string.Empty,
            IsShared = true,
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = herbName, Dosage = 10, Unit = "克" }
            }
        };
        var (shareResponse, _) = await PutAsync<FormulaDetailDto>(
            doctor, $"/api/v1/formulas/{formulaId}", updateInput);
        shareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create second doctor and verify they can see the shared formula
        var secondDoctorUsername = UniqueName("dr2");
        await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = secondDoctorUsername, RealName = "张医生", Role = UserRole.Doctor,
            Password = SecondDoctorPassword, ConfirmPassword = SecondDoctorPassword,
            Email = $"{secondDoctorUsername}@test.com", PhoneNumber = UniquePhone()
        });
        var secondDoctor = await LoginAsAsync(secondDoctorUsername, SecondDoctorPassword);

        var (secondDoctorFormulasResponse, secondDoctorFormulas) = await GetAsync<PagedResult<FormulaDetailDto>>(
            secondDoctor, "/api/v1/formulas");
        secondDoctorFormulasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondDoctorFormulas!.Items.Should().Contain(f => f.Id == formulaId,
            "Shared formula should be visible to other doctors");

        // Step 6: Use formula herbs in a prescription
        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("方剂测试"), Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119900101{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        var (_, medicalCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorUserId });

        var prescInput = new MedicalCaseInputDto
        {
            Id = medicalCase!.Id, PatientId = patient.Id, UserId = doctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "方剂测试", TcmDiagnosis = "测试诊断",
                TongueDiagnosis = "舌红", PulseDiagnosis = "脉弦"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id, DosageCount = 7, Usage = "水煎服",
                TotalPrice = 24.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herbId, HerbName = herbName,
                        Unit = "克", Dosage = 10, UnitPrice = 1.5m, Subtotal = 15.0m
                    },
                    new()
                    {
                        HerbId = herb2!.Id, HerbName = herb2.Name,
                        Unit = "克", Dosage = 10, UnitPrice = 0.9m, Subtotal = 9.0m
                    }
                }
            }
        };
        var (saveResponse, _2) = await PutAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCase.Id}", prescInput);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify prescription saved with correct items
        var (_, savedCase) = await GetAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCase.Id}");
        savedCase!.Prescription.Should().NotBeNull();
        savedCase.Prescription!.Items.Should().HaveCount(2);
    }
}
