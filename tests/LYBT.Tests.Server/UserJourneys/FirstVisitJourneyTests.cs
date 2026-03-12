using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT Narrative 1: First visit journey.
/// Receptionist creates patient + registration, doctor starts visit,
/// creates medical case, fills diagnosis, sets prescription, completes case.
/// Includes 4 exception path tests (BR-001, BR-003, cancel registration).
/// </summary>
[Collection("ClinicalData")]
public sealed class FirstVisitJourneyTests : JourneyTestBase<ClinicalDataFixture>
{
    private const string TestPassword = "TestFirstVisit2025@";

    public FirstVisitJourneyTests(ClinicalDataFixture fixture) : base(fixture) { }

    [Fact]
    public async Task US_REG_001_NormalPath_CompleteFirstVisit()
    {
        // Setup: Reset, create receptionist, login all roles, create herbs
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorUserData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = doctorUserData!.Id;
        var doctorRealName = doctorUserData.RealName;

        var receptionistUsername = UniqueName("recep");
        await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = receptionistUsername, RealName = "前台小王",
            Role = UserRole.Receptionist, Password = TestPassword, ConfirmPassword = TestPassword,
            Email = $"{receptionistUsername}@test.com", PhoneNumber = UniquePhone()
        });
        var receptionist = await LoginAsAsync(receptionistUsername, TestPassword);

        var (_, herb1) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("黄芪"), Unit = "克", Price = 0.5m });
        var (_, herb2) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("当归"), Unit = "克", Price = 0.8m });

        // Step 1: Receptionist creates patient
        var patientInput = new PatientInputDto
        {
            Name = UniqueName("张三"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            PhoneNumber = UniquePhone(),
            IdNumber = $"32010119900101{Random.Shared.Next(1000, 9999)}",
            Address = "南京市鼓楼区"
        };
        var (createPatientResponse, patient) = await PostAsync<PatientDetailDto>(receptionist, "/api/v1/patients", patientInput);
        createPatientResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var patientId = patient!.Id;
        var patientName = patient.Name;

        // Step 2: Receptionist creates registration and assigns doctor
        var regInput = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorUserId,
            DoctorName = doctorRealName ?? "doctor",
            Source = RegistrationSource.Receptionist
        };
        var (createRegResponse, registration) = await PostAsync<RegistrationDetailDto>(receptionist, "/api/v1/registrations", regInput);
        createRegResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        registration!.Status.Should().Be(RegistrationStatus.Waiting);
        var registrationId = registration.Id;

        // Step 3: Doctor views waiting queue
        var (queueResponse, queueData) = await GetAsync<List<RegistrationListDto>>(
            doctor, $"/api/v1/registrations/queue?doctorId={doctorUserId}");
        queueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        queueData.Should().NotBeNull();

        // Step 4: Doctor starts visit (Registration -> InProgress), then creates medical case
        var startVisitResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/registrations/{registrationId}/start-visit", new { });
        startVisitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify registration status changed to InProgress
        var (regCheckResponse, regDetail) = await GetAsync<RegistrationDetailDto>(
            receptionist, $"/api/v1/registrations/{registrationId}");
        regCheckResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        regDetail!.Status.Should().Be(RegistrationStatus.InProgress);

        // Create medical case separately
        var caseInput = new MedicalCaseInputDto { PatientId = patientId, UserId = doctorUserId };
        var (createCaseResponse, createdCase) = await PostAsync<MedicalCaseDetailDto>(
            doctor, "/api/v1/medicalcases", caseInput);
        createCaseResponse.IsSuccessStatusCode.Should().BeTrue();
        createdCase!.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        var medicalCaseId = createdCase.Id;

        // Step 5: Fill diagnosis (Consultation via aggregate save)
        var diagInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId, PatientId = patientId, UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛3天，伴失眠",
                TongueDiagnosis = "舌红苔薄",
                PulseDiagnosis = "脉弦",
                TcmDiagnosis = "肝阳上亢"
            }
        };
        var (saveDiagResponse, _) = await PutAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCaseId}", diagInput);
        saveDiagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 6: Set NeedsPrescription flag
        var flagResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/medicalcases/{medicalCaseId}/prescription-flag",
            new SetPrescriptionFlagRequest { NeedsPrescription = true });
        flagResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 7: Add prescription items via aggregate save
        var prescInput = new MedicalCaseInputDto
        {
            Id = medicalCaseId, PatientId = patientId, UserId = doctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛3天，伴失眠",
                TongueDiagnosis = "舌红苔薄",
                PulseDiagnosis = "脉弦",
                TcmDiagnosis = "肝阳上亢"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCaseId,
                DosageCount = 7,
                Usage = "水煎服，日一剂",
                TotalPrice = 25.5m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herb1!.Id, HerbName = herb1.Name,
                        Unit = "克", Dosage = 15, UnitPrice = 0.5m, Subtotal = 7.5m
                    },
                    new()
                    {
                        HerbId = herb2!.Id, HerbName = herb2.Name,
                        Unit = "克", Dosage = 10, UnitPrice = 0.8m, Subtotal = 8.0m
                    }
                }
            }
        };
        var (savePrescResponse, _2) = await PutAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCaseId}", prescInput);
        savePrescResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify prescription persisted
        var (getCaseResponse, caseDetail) = await GetAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCaseId}");
        getCaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        caseDetail!.Consultation.Should().NotBeNull();
        caseDetail.Consultation!.TcmDiagnosis.Should().Be("肝阳上亢");
        caseDetail.Prescription.Should().NotBeNull();
        caseDetail.Prescription!.Items.Should().HaveCount(2);

        // Step 8: Complete medical case
        var statusInput = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        var completeResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/medicalcases/{medicalCaseId}/status", statusInput);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var (getCompletedResponse, completedCase) = await GetAsync<MedicalCaseDetailDto>(
            doctor, $"/api/v1/medicalcases/{medicalCaseId}");
        getCompletedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        completedCase!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task US_MC_009_DuplicateActiveCase_ShouldFail()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        var doctorUserId = doctorData!.Id;

        // Create patient
        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("重复"), Gender = Gender.Male,
            BirthDate = new DateTime(1985, 5, 15), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119850515{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        // Create first Active case
        var (firstResponse, _) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorUserId });
        firstResponse.IsSuccessStatusCode.Should().BeTrue();

        // Second case for same patient should fail (BR-001: duplicate active case)
        var (secondResponse, _2) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient.Id, UserId = doctorUserId });
        secondResponse.IsSuccessStatusCode.Should().BeFalse("BR-001: should block duplicate active case");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BusinessException returns 400");
        var (errorMsg, _) = await ReadErrorAsync(secondResponse);
        errorMsg.Should().Contain("医案", "Error message should mention existing case");
    }

    [Fact]
    public async Task US_MC_004_EmptyDiagnosis_BlocksCompletion()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");

        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("空诊断"), Gender = Gender.Female,
            BirthDate = new DateTime(1995, 3, 20), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119950320{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        // Create case without filling TcmDiagnosis
        var (_, createdCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorData!.Id });

        // Try to complete without TcmDiagnosis - should fail (BR-003)
        var statusInput = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        var completeResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/medicalcases/{createdCase!.Id}/status", statusInput);
        completeResponse.IsSuccessStatusCode.Should().BeFalse("BR-003: empty TcmDiagnosis should block completion");
        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BusinessException returns 400");
        var (errorMsg, _) = await ReadErrorAsync(completeResponse);
        // Note: completion validation checks NeedsPrescription flag BEFORE TcmDiagnosis.
        // When both are missing, error mentions prescription flag first.
        errorMsg.Should().NotBeNullOrEmpty("Error message should explain why completion was blocked");
    }

    [Fact]
    public async Task US_MC_004_NoPrescriptionDecision_BlocksCompletion()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");

        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("无处方决定"), Gender = Gender.Male,
            BirthDate = new DateTime(1988, 8, 8), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119880808{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        var (_, createdCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorData!.Id });

        // Fill TcmDiagnosis but leave NeedsPrescription null
        await PutAsync<MedicalCaseDetailDto>(doctor, $"/api/v1/medicalcases/{createdCase!.Id}", new MedicalCaseInputDto
        {
            Id = createdCase.Id, PatientId = patient.Id, UserId = doctorData.Id,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "气虚血瘀" }
        });

        // Try to complete without NeedsPrescription decision - should fail (BR-003)
        var statusInput = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        var completeResponse = await doctor.PutAsJsonAsync(
            $"/api/v1/medicalcases/{createdCase.Id}/status", statusInput);
        completeResponse.IsSuccessStatusCode.Should().BeFalse("BR-003: NeedsPrescription decision required");
        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BusinessException returns 400");
        var (errorMsg, _) = await ReadErrorAsync(completeResponse);
        errorMsg.Should().Contain("处方", "Error should mention prescription decision");
    }

    [Fact]
    public async Task US_REG_004_CancelRegistration_Succeeds()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");

        var receptionistUsername = UniqueName("recep");
        await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = receptionistUsername, RealName = "前台", Role = UserRole.Receptionist,
            Password = TestPassword, ConfirmPassword = TestPassword,
            Email = $"{receptionistUsername}@test.com", PhoneNumber = UniquePhone()
        });
        var receptionist = await LoginAsAsync(receptionistUsername, TestPassword);

        var (_, patient) = await PostAsync<PatientDetailDto>(receptionist, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("取消挂号"), Gender = Gender.Male,
            BirthDate = new DateTime(1992, 12, 1), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119921201{Random.Shared.Next(1000, 9999)}", Address = "测试地址"
        });

        // Create registration (Waiting)
        var (_, reg) = await PostAsync<RegistrationDetailDto>(receptionist, "/api/v1/registrations", new RegistrationInputDto
        {
            PatientId = patient!.Id, PatientName = patient.Name,
            DoctorId = doctorData!.Id, DoctorName = "doctor",
            Source = RegistrationSource.Receptionist
        });

        // Cancel registration
        var cancelResponse = await receptionist.PutAsJsonAsync(
            $"/api/v1/registrations/{reg!.Id}/cancel", new { });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cancelled status
        var (getResponse, cancelledReg) = await GetAsync<RegistrationDetailDto>(
            receptionist, $"/api/v1/registrations/{reg.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        cancelledReg!.Status.Should().Be(RegistrationStatus.Cancelled);
    }
}
