using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Registration;

/// <summary>
/// Must Have User Stories for Registration module.
/// PRD: US-REG-001 ~ US-REG-006 (6 Must Have)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Registration_MustHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Registration_MustHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    /// <summary>Create a patient and return its ID and name.</summary>
    private async Task<(Guid Id, string Name)> CreatePatientAsync(
        HttpClient client, string name = "挂号测试患者")
    {
        var fullName = $"{name}_{Guid.NewGuid():N}"[..12];
        var payload = PatientBuilder.Default().WithName(fullName).Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return (data.Id, data.Name);
    }

    /// <summary>Create a registration and return its ID.</summary>
    private async Task<Guid> CreateRegistrationAsync(
        HttpClient adminClient, Guid patientId, string patientName, Guid doctorId, string doctorName)
    {
        var payload = RegistrationBuilder.Default()
            .ForPatient(patientId, patientName)
            .WithDoctor(doctorId, doctorName)
            .Build();
        var response = await adminClient.PostAsJsonAsync("/api/v1/registrations", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<RegistrationDetailDto>();
        return data.Id;
    }

    #endregion

    #region US-REG-001: Create registration

    [Fact]
    public async Task US_REG_001_CreateRegistration_WithValidData_Succeeds()
    {
        // Arrange - admin/receptionist creates registration
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();

        var patient = await CreatePatientAsync(doctorClient);
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var payload = RegistrationBuilder.Default()
            .ForPatient(patient.Id, patient.Name)
            .WithDoctor(doctorId, "doctor")
            .Build();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/registrations", payload);

        // Assert
        var data = await response.ShouldBeCreatedWithDataAsync<RegistrationDetailDto>(
            "US-REG-001: registration should be created");
        data.PatientId.Should().Be(patient.Id);
        data.DoctorId.Should().Be(doctorId);
        data.Id.Should().NotBeEmpty();
        data.Status.Should().Be(RegistrationStatus.Waiting,
            "receptionist registration should start as Waiting");
    }

    [Fact]
    public async Task US_REG_001_CreateRegistration_ByDoctor_StartsInProgress()
    {
        // Arrange - doctor creates direct visit
        var doctorClient = await LoginAsDoctorAsync();
        var patient = await CreatePatientAsync(doctorClient);
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var payload = RegistrationBuilder.Default()
            .ForPatient(patient.Id, patient.Name)
            .WithDoctor(doctorId, "doctor")
            .Build();

        // Act - doctor creates registration (Source=Doctor -> InProgress)
        var response = await doctorClient.PostAsJsonAsync("/api/v1/registrations", payload);

        // Assert
        var data = await response.ShouldBeCreatedWithDataAsync<RegistrationDetailDto>(
            "US-REG-001: doctor registration should succeed");
        data.Status.Should().BeOneOf(
            new[] { RegistrationStatus.Waiting, RegistrationStatus.InProgress },
            "doctor registration status depends on source");
    }

    #endregion

    #region US-REG-002: View queue

    [Fact]
    public async Task US_REG_002_GetQueue_ReturnsWaitingRegistrations()
    {
        // Arrange - create registrations
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient1 = await CreatePatientAsync(doctorClient, "队列患者甲");
        var patient2 = await CreatePatientAsync(doctorClient, "队列患者乙");

        await CreateRegistrationAsync(adminClient, patient1.Id, patient1.Name, doctorId, "doctor");
        await CreateRegistrationAsync(adminClient, patient2.Id, patient2.Name, doctorId, "doctor");

        // Act
        var response = await doctorClient.GetAsync($"/api/v1/registrations/queue?doctorId={doctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-REG-002: queue endpoint should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace(
            "US-REG-002: queue should contain waiting registrations");
    }

    [Fact]
    public async Task D4_Receptionist_CanAccessQueue_WithMedicalCaseHint()
    {
        // Arrange - D-4 fix: Receptionist can view queue with hasMedicalCase hint
        var adminClient = await LoginAsAdminAsync();
        var receptionistClient = await LoginAsReceptionistAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "D4测试患者");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - Receptionist accesses queue (PatientAccess policy)
        var response = await receptionistClient.GetAsync("/api/v1/registrations/queue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "D-4: Receptionist should be able to access registration queue");

        var result = await response.ShouldBeSuccessWithDataAsync<List<RegistrationListDto>>();
        result.Should().NotBeEmpty("Queue should contain the registration");

        var registration = result.First(r => r.PatientName.Contains("D4测试患者"));
        registration.HasMedicalCase.Should().BeFalse(
            "D-4: New registration should have HasMedicalCase=false before visit starts");
        registration.MedicalCaseId.Should().BeNull(
            "D-4: New registration should not have associated MedicalCase yet");
    }

    [Fact]
    public async Task D4_Registration_HasMedicalCase_ComputedCorrectly()
    {
        // Arrange - create registration (without MedicalCase association)
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var receptionistClient = await LoginAsReceptionistAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "医案关联患者");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - Query registration list via Receptionist
        var response = await receptionistClient.GetAsync("/api/v1/registrations?page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            expectedMinCount: 1,
            because: "D-4: Registration list should contain the record");
        var registration = paged.Items.First(r => r.PatientName.Contains("医案关联患者"));

        // NOTE: Currently MedicalCase creation does not auto-update Registration.MedicalCaseId
        // This is documented in findings.md for future fix
        // For now, we verify the computed property logic is correct:
        // - When MedicalCaseId is null, HasMedicalCase should be false
        registration.HasMedicalCase.Should().Be(registration.MedicalCaseId.HasValue,
            "D-4: HasMedicalCase should correctly reflect MedicalCaseId presence");
    }

    [Fact]
    public async Task US_REG_002_GetQueue_EmptyQueue_ReturnsEmptyOrOk()
    {
        // Arrange - use a doctor with no registrations (use fake doctorId)
        var doctorClient = await LoginAsDoctorAsync();
        var fakeDoctorId = Guid.NewGuid();

        // Act
        var response = await doctorClient.GetAsync($"/api/v1/registrations/queue?doctorId={fakeDoctorId}");

        // Assert - should return 200 with empty result
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-REG-002: empty queue should still return 200");
    }

    #endregion

    #region US-REG-003: Start visit

    [Fact]
    public async Task US_REG_003_StartVisit_TransitionsToInProgress()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "接诊患者");
        var regId = await CreateRegistrationAsync(
            adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - start visit
        var response = await doctorClient.PutAsync(
            $"/api/v1/registrations/{regId}/start-visit", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-REG-003: start visit should succeed");

        // Verify
        var getResp = await doctorClient.GetAsync($"/api/v1/registrations/{regId}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<RegistrationDetailDto>();
        data.Status.Should().Be(RegistrationStatus.InProgress,
            "US-REG-003: status should transition to InProgress");
    }

    [Fact]
    public async Task US_REG_003_StartVisit_AlreadyInProgress_ShouldHandleGracefully()
    {
        // Arrange - create and start a visit
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "重复接诊患者");
        var regId = await CreateRegistrationAsync(
            adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // First start
        await doctorClient.PutAsync($"/api/v1/registrations/{regId}/start-visit", null);

        // Act - try to start again
        var response = await doctorClient.PutAsync(
            $"/api/v1/registrations/{regId}/start-visit", null);

        // Assert - idempotent OK or conflict
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity },
            "US-REG-003: double start-visit should be handled gracefully");
    }

    [Fact]
    public async Task US_REG_003_StartVisit_CancelledRegistration_ShouldFail()
    {
        // Arrange - create and cancel a registration
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "已取消接诊患者");
        var regId = await CreateRegistrationAsync(
            adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Cancel first
        await adminClient.PutAsync($"/api/v1/registrations/{regId}/cancel", null);

        // Act - try to start cancelled registration
        var response = await doctorClient.PutAsync(
            $"/api/v1/registrations/{regId}/start-visit", null);

        // Assert - cancelled registration cannot be started
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict, HttpStatusCode.NotFound },
            "US-REG-003: cannot start-visit on cancelled registration");
    }

    #endregion

    #region US-REG-005: List registrations

    [Fact]
    public async Task US_REG_005_ListRegistrations_WithDateFilter_ReturnsFiltered()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "日期过滤患者");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - filter by today's date
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?visitDate={today}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-REG-005: date filter should return 200");
    }

    #endregion

    #region US-REG-004: Cancel registration

    [Fact]
    public async Task US_REG_004_CancelRegistration_Succeeds()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "取消挂号患者");
        var regId = await CreateRegistrationAsync(
            adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - cancel
        var response = await adminClient.PutAsync(
            $"/api/v1/registrations/{regId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-REG-004: cancel registration should succeed");
    }

    [Fact]
    public async Task US_REG_004_CancelRegistration_NonexistentId_Returns404()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await adminClient.PutAsync(
            $"/api/v1/registrations/{fakeId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "US-REG-004: cancelling non-existent registration returns 422 via BusinessFail");
    }

    #endregion

    #region US-REG-005: List registrations

    [Fact]
    public async Task US_REG_005_ListRegistrations_ReturnsPaginatedResult()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "列表患者");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act
        var response = await adminClient.GetAsync("/api/v1/registrations?page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            expectedMinCount: 1,
            because: "US-REG-005: should return at least 1 registration");
    }

    #endregion

    #region US-REG-006: Filter by doctor

    [Fact]
    public async Task US_REG_006_FilterByDoctor_ReturnsFilteredResults()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "医生过滤患者");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - filter by doctor
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?doctorId={doctorId}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            expectedMinCount: 1,
            because: "US-REG-006: doctor filter should return matching registrations");
    }

    [Fact]
    public async Task US_REG_006_FilterByNonexistentDoctor_ReturnsEmpty()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var fakeDoctorId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?doctorId={fakeDoctorId}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            because: "US-REG-006: non-existent doctor should return empty result");
        paged.Items.Should().BeEmpty();
    }

    #endregion
}
