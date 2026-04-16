using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// MedicalCase lifecycle flow integration tests.
/// Tests the full chain: MedicalCaseRepository -> IMedicalCaseApi -> Server MedicalCasesController -> SQL Server.
/// </summary>
[Collection("Integration")]
public class MedicalCaseFlowTests : IntegrationTestBase
{
    private static int _counter;

    public MedicalCaseFlowTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<(MedicalCaseRepository CaseDs, PatientRepository PatientDs, HttpClient Client)> CreateDataSourcesAsync()
    {
        var (client, caseApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var patientApi = Fixture.CreateApi<IPatientApi>(client);
        return (
            new MedicalCaseRepository(caseApi, NullLogger<MedicalCaseRepository>.Instance),
            new PatientRepository(patientApi, NullLogger<PatientRepository>.Instance),
            client
        );
    }

    private async Task<Guid> CreateTestPatientAsync(PatientRepository patientDs)
    {
        var seq = Interlocked.Increment(ref _counter);
        var patient = await patientDs.CreateAsync(new PatientInputDto
        {
            Name = $"测试患者_{seq:D4}",
            Gender = Gender.Male,
            IdNumber = $"11010119900101{seq:D4}",
            PhoneNumber = $"139{seq:D8}",
            Address = "测试地址"
        });
        return patient.Id;
    }

    [Fact]
    public async Task CreateMedicalCase_ReturnsNewCase()
    {
        // Arrange
        var (caseDs, patientDs, _) = await CreateDataSourcesAsync();
        var patientId = await CreateTestPatientAsync(patientDs);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        };

        // Act
        var created = await caseDs.CreateAsync(input);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task SaveWithConsultation_UpdatesCase()
    {
        // Arrange
        var (caseDs, patientDs, _) = await CreateDataSourcesAsync();
        var patientId = await CreateTestPatientAsync(patientDs);

        var created = await caseDs.CreateAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });

        // Act - save with consultation data
        var saveInput = new MedicalCaseInputDto
        {
            Id = created.Id,
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛三天",
                TcmDiagnosis = "气虚血瘀"
            }
        };
        var saved = await caseDs.SaveAsync(created.Id, saveInput);

        // Assert
        saved.Should().NotBeNull();
        saved.Consultation.Should().NotBeNull();
        saved.Consultation!.TcmDiagnosis.Should().Be("气虚血瘀");
    }

    [Fact]
    public async Task CompleteCase_ClosesCase()
    {
        // Arrange
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        var createResponse = await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });
        var caseId = createResponse.Data!.Id;

        // Save consultation first (close may require consultation to exist)
        await caseApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                TcmDiagnosis = "测试诊断"
            }
        });

        // Act - close the case
        var closeResponse = await caseApi.CloseCaseAsync(caseId);

        // Assert
        closeResponse.Success.Should().BeTrue();

        // Verify status changed
        var detail = await caseApi.GetMedicalCaseByIdAsync(caseId);
        detail.Data.Should().NotBeNull();
        detail.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task SuspendAndResume_CaseLifecycle()
    {
        // Arrange
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        var createResponse = await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });
        var caseId = createResponse.Data!.Id;

        // Act - suspend
        var suspendResponse = await caseApi.SuspendAsync(caseId);
        suspendResponse.Success.Should().BeTrue();

        // Verify suspended
        var suspended = await caseApi.GetMedicalCaseByIdAsync(caseId);
        suspended.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);

        // Act - resume via UpdateStatus
        var resumeResponse = await caseApi.UpdateStatusAsync(caseId, new MedicalCaseStatusInputDto
        {
            Status = MedicalCaseStatus.Active
        });
        resumeResponse.Success.Should().BeTrue();

        // Verify resumed
        var resumed = await caseApi.GetMedicalCaseByIdAsync(caseId);
        resumed.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Active);
    }

    [Fact]
    public async Task DeleteCase_SoftDeletes()
    {
        // Arrange - use raw HttpClient because DELETE returns 204 (no body),
        // which Refit can't deserialize into the custom ApiResponse type.
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        var createResponse = await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });
        var caseId = createResponse.Data!.Id;

        // Act - delete via raw HttpClient to handle 204 properly
        var deleteResponse = await client.DeleteAsync($"/api/v1/medicalcases/{caseId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPendingCases_ReturnsDoctorCases()
    {
        // Arrange
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });

        // Act
        var response = await caseApi.GetPendingCasesAsync();

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPermissions_ReturnsPermissionInfo()
    {
        // Arrange
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        var createResponse = await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });

        // Act
        var permResponse = await caseApi.GetPermissionsAsync(createResponse.Data!.Id);

        // Assert
        permResponse.Success.Should().BeTrue();
        permResponse.Data.Should().NotBeNull();
        permResponse.Data!.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task FullWorkflow_CreateSaveComplete()
    {
        // Arrange
        var (_, patientDs, client) = await CreateDataSourcesAsync();
        var caseApi = Fixture.CreateApi<IMedicalCaseApi>(client);
        var patientId = await CreateTestPatientAsync(patientDs);

        // Step 1: Create
        var createResponse = await caseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId
        });
        createResponse.Data.Should().NotBeNull();
        var caseId = createResponse.Data!.Id;

        // Step 2: Save with consultation
        var saveResponse = await caseApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            UserId = IntegrationFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "腰膝酸软",
                TcmDiagnosis = "肾阳虚"
            }
        });
        saveResponse.Data!.Consultation.Should().NotBeNull();

        // Step 3: Complete via CloseCaseAsync
        var closeResponse = await caseApi.CloseCaseAsync(caseId);
        closeResponse.Success.Should().BeTrue();

        // Step 4: Verify final state
        var finalResponse = await caseApi.GetMedicalCaseByIdAsync(caseId);
        finalResponse.Data.Should().NotBeNull();
        finalResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        finalResponse.Data.Consultation.Should().NotBeNull();
    }
}
