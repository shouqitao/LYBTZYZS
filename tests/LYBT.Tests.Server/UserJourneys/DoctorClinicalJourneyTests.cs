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
/// Doctor clinical journey: login, query patients, create medical case,
/// save diagnosis, add prescription, complete case, admin views case.
/// </summary>
[Collection("Server")]
public sealed class DoctorClinicalJourneyTests : JourneyTestBase
{
    public DoctorClinicalJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DoctorClinical_Full_Journey()
    {
        // Step 1: Setup - reset, login, create patient and herb
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, userData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = userData!.Id;

        var patientInput = new PatientInputDto
        {
            Name = UniqueName("李四"),
            Gender = Gender.Female,
            BirthDate = new DateTime(1990, 6, 20),
            PhoneNumber = UniquePhone(),
            IdNumber = $"11010119900620{Random.Shared.Next(1000, 9999)}",
            Address = "北京市海淀区"
        };
        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", patientInput);
        var patientId = patient!.Id;

        var herbInput = new HerbInputDto { Name = UniqueName("黄芪"), Unit = "克", Price = 20.0m };
        var (_, herb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs", herbInput);
        var herbId = herb!.Id;

        // Step 2: Doctor queries patients
        var (patientsResponse, patientsData) = await GetAsync<PagedResult<PatientDetailDto>>(doctor, "/api/v1/patients");
        patientsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        patientsData!.Items.Should().NotBeEmpty();

        // Step 3: Create medical case
        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId
        };

        var (createCaseResponse, createdCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases", caseInput);
        createCaseResponse.IsSuccessStatusCode.Should().BeTrue($"创建医案应成功, 实际: {createCaseResponse.StatusCode}");
        createdCase!.Id.Should().NotBeEmpty();
        createdCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        var medicalCaseId = createdCase.Id;

        // Step 4: Save diagnosis
        var diagnosisInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId,
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛发热三日",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "浮数",
                TcmDiagnosis = "风热犯肺"
            }
        };

        var (saveDiagResponse, _) = await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{medicalCaseId}", diagnosisInput);
        saveDiagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify diagnosis saved
        var (getDiagResponse, diagData) = await GetAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{medicalCaseId}");
        getDiagResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        diagData!.Consultation.Should().NotBeNull();
        diagData.Consultation!.TcmDiagnosis.Should().Be("风热犯肺");

        // Step 6: Set needs prescription
        var flagResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/medicalcases/{medicalCaseId}/prescription-flag",
            new SetPrescriptionFlagRequest { NeedsPrescription = true });
        flagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 7: Save prescription
        var prescriptionInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId,
            PatientId = patientId,
            UserId = doctorUserId,
            NeedsPrescription = true,
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCaseId,
                DosageCount = 7,
                Usage = "水煎服，日一剂",
                Advice = "忌辛辣",
                TotalPrice = 140.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herbId,
                        HerbName = "黄芪",
                        Unit = "克",
                        Dosage = 30,
                        UnitPrice = 20.0m,
                        Subtotal = 140.0m
                    }
                }
            }
        };

        var (savePrescResponse, _2) = await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{medicalCaseId}", prescriptionInput);
        savePrescResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 8: Verify prescription saved
        var (getPrescResponse, prescData) = await GetAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{medicalCaseId}");
        getPrescResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        prescData!.Prescription.Should().NotBeNull();
        prescData.Prescription!.Items.Should().NotBeEmpty();
        prescData.Prescription.DosageCount.Should().Be(7);

        // Step 9: Complete medical case
        var statusInput = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        var completeResponse = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{medicalCaseId}/status", statusInput);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 10: Verify case completed
        var (getCompletedResponse, completedData) = await GetAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{medicalCaseId}");
        getCompletedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        completedData!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);

        // Step 11: Edit completed case (may require reason)
        var editInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId,
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "修改后的诊断" }
        };

        var editResponse = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{medicalCaseId}", editInput);
        ((int)editResponse.StatusCode).Should().BeOneOf(200, 400);

        // Step 12: Admin can view case
        var (adminViewResponse, adminViewData) = await GetAsync<MedicalCaseDetailDto>(admin, $"/api/v1/medicalcases/{medicalCaseId}");
        adminViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        adminViewData!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }
}
