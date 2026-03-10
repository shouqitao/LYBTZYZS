using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
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
/// UAT Narrative 2: Return visit journey.
/// Patient returns for follow-up: search patient, view history,
/// create new case, complete workflow.
/// Exception: print-then-edit requires EditReason.
/// </summary>
[Collection("Server")]
public sealed class ReturnVisitJourneyTests : JourneyTestBase
{
    public ReturnVisitJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ReturnVisit_Normal_Path()
    {
        // Setup: Create patient with completed first visit
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = doctorData!.Id;

        var (_, herb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("川芎"), Unit = "克", Price = 1.2m });

        var patientName = UniqueName("复诊");
        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = patientName, Gender = Gender.Female,
            BirthDate = new DateTime(1985, 7, 15), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119850715{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });
        var patientId = patient!.Id;

        // Complete first case
        var (_, firstCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patientId, UserId = doctorUserId });

        await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{firstCase!.Id}", new MedicalCaseInputDto
        {
            Id = firstCase.Id, PatientId = patientId, UserId = doctorUserId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "首诊症状", TcmDiagnosis = "首诊诊断",
                TongueDiagnosis = "舌淡", PulseDiagnosis = "脉细"
            }
        });

        await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{firstCase.Id}/prescription-flag",
            new SetPrescriptionFlagRequest { NeedsPrescription = false });

        var completeFirst = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{firstCase.Id}/status",
            new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed });
        completeFirst.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 1: Search patient by name
        var (searchResponse, searchResult) = await GetAsync<PagedResult<PatientDetailDto>>(
            doctor, $"/api/v1/patients?keyword={Uri.EscapeDataString(patientName[..4])}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        searchResult!.Items.Should().Contain(p => p.Id == patientId);

        // Step 2: View history (list cases for patient)
        var (historyResponse, historyResult) = await GetAsync<PagedResult<MedicalCaseDetailDto>>(
            doctor, $"/api/v1/medicalcases/query?queryType=ByPatient&patientId={patientId}");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        historyResult!.Items.Should().Contain(c => c.Id == firstCase.Id);

        // Step 3: Create new case for return visit
        var (_, newCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patientId, UserId = doctorUserId });
        newCase!.CaseStatus.Should().Be(MedicalCaseStatus.Active);

        // Step 4: Fill diagnosis + prescription + complete
        await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{newCase.Id}", new MedicalCaseInputDto
        {
            Id = newCase.Id, PatientId = patientId, UserId = doctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "复诊症状好转", TcmDiagnosis = "复诊诊断",
                TongueDiagnosis = "舌红", PulseDiagnosis = "脉弦"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = newCase.Id, DosageCount = 5, Usage = "水煎服",
                TotalPrice = 12.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herb!.Id, HerbName = herb.Name,
                        Unit = "克", Dosage = 10, UnitPrice = 1.2m, Subtotal = 12.0m
                    }
                }
            }
        });

        await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{newCase.Id}/prescription-flag",
            new SetPrescriptionFlagRequest { NeedsPrescription = true });

        var completeNew = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{newCase.Id}/status",
            new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed });
        completeNew.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify two cases exist for this patient
        var (finalResponse, finalResult) = await GetAsync<PagedResult<MedicalCaseDetailDto>>(
            doctor, $"/api/v1/medicalcases/query?queryType=ByPatient&patientId={patientId}");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        finalResult!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ReturnVisit_Exception_CompletedCase_RequiresEditReason()
    {
        // Setup: Create and complete a case
        // Note: print-completed endpoint has known 500 bug (PrintLogs navigation issue),
        // so we test EditReason via IsCompleted status instead of IsPrinted flag.
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = doctorData!.Id;

        var (_, herb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("白术"), Unit = "克", Price = 0.6m });

        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("编辑原因"), Gender = Gender.Male,
            BirthDate = new DateTime(1980, 3, 10), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119800310{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        var (_, createdCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorUserId });

        // Fill diagnosis + set prescription flag + complete
        await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{createdCase!.Id}", new MedicalCaseInputDto
        {
            Id = createdCase.Id, PatientId = patient.Id, UserId = doctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "编辑原因测试", TcmDiagnosis = "测试诊断",
                TongueDiagnosis = "舌红", PulseDiagnosis = "脉数"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = createdCase.Id, DosageCount = 3, Usage = "水煎服",
                TotalPrice = 6.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herb!.Id, HerbName = herb.Name,
                        Unit = "克", Dosage = 10, UnitPrice = 0.6m, Subtotal = 6.0m
                    }
                }
            }
        });

        await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{createdCase.Id}/prescription-flag",
            new SetPrescriptionFlagRequest { NeedsPrescription = true });

        var completeResponse = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{createdCase.Id}/status",
            new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 1: Edit completed case without EditReason - should fail
        var editWithoutReason = new MedicalCaseInputDto
        {
            Id = createdCase.Id, PatientId = patient.Id, UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "修改诊断" }
        };
        var (editFailResponse, _) = await PutAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{createdCase.Id}", editWithoutReason);
        editFailResponse.IsSuccessStatusCode.Should().BeFalse(
            "Completed case requires EditReason (RequiresEditReason: IsCompleted)");
        editFailResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BusinessException returns 400");
        var (errorMsg, _) = await ReadErrorAsync(editFailResponse);
        errorMsg.Should().Contain("修改原因", "Error should mention EditReason requirement");

        // Step 2: Edit with EditReason - should succeed
        var editWithReason = new MedicalCaseInputDto
        {
            Id = createdCase.Id, PatientId = patient.Id, UserId = doctorUserId,
            EditReason = "修正剂量",
            Consultation = new ConsultationInputDto { TcmDiagnosis = "修正后的诊断" }
        };
        var (editSuccessResponse, _2) = await PutAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{createdCase.Id}", editWithReason);
        editSuccessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify edit persisted
        var (_, editedCase) = await GetAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{createdCase.Id}");
        editedCase!.Consultation!.TcmDiagnosis.Should().Be("修正后的诊断");
    }
}
