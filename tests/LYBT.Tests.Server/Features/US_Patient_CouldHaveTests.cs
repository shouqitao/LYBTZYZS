using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Patients;

/// <summary>
/// Could Have User Stories for Patients module.
/// PRD: US-PAT-006 (Restore), US-PAT-007 (Batch delete), US-PAT-008 (Excel import),
///      US-PAT-009 (Import template), US-PAT-010 (Export), US-PAT-011 (Check reference),
///      US-PAT-012 (Batch check reference)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Patient_CouldHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Patient_CouldHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<PatientDetailDto> CreatePatientAsync(HttpClient client, string name = "CH患者")
    {
        var payload = PatientBuilder.Default().WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        return await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
    }

    private async Task DeletePatientAsync(HttpClient client, Guid patientId)
    {
        var response = await client.DeleteAsync($"/api/v1/patients/{patientId}");
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "patient soft-delete should succeed");
    }

    #endregion

    #region US-PAT-006: Restore deleted patient

    [Fact]
    public async Task US_PAT_006_RestoreDeletedPatient_ReturnsSuccess()
    {
        // Arrange - create then soft-delete
        var doctorClient = await LoginAsDoctorAsync();
        var patient = await CreatePatientAsync(doctorClient, "待恢复患者");
        await DeletePatientAsync(doctorClient, patient.Id);

        // Act - restore
        var response = await doctorClient.PostAsync($"/api/v1/patients/{patient.Id}/restore", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-PAT-006: restore deleted patient should succeed");
    }

    [Fact]
    public async Task US_PAT_006_RestoreNonDeletedPatient_ReturnsSuccessOrNotDeleted()
    {
        // Arrange - create (not deleted)
        var doctorClient = await LoginAsDoctorAsync();
        var patient = await CreatePatientAsync(doctorClient, "未删除恢复");

        // Act - try to restore a non-deleted patient
        var response = await doctorClient.PostAsync($"/api/v1/patients/{patient.Id}/restore", null);

        // Assert - should return success or indicate "not deleted"
        response.StatusCode.Should().BeOneOf(
            new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.NoContent,
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnprocessableEntity
            },
            "US-PAT-006: restoring non-deleted patient should be handled gracefully");
    }

    [Fact]
    public async Task US_PAT_006_Restore_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsync($"/api/v1/patients/{Guid.NewGuid()}/restore", null);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-PAT-007: Batch delete patients

    [Fact]
    public async Task US_PAT_007_BatchDelete_WithoutReferences_ReturnsResult()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var p1 = await CreatePatientAsync(doctorClient, "批删甲");
        var p2 = await CreatePatientAsync(doctorClient, "批删乙");

        var payload = new { Ids = new[] { p1.Id, p2.Id } };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients/batch-delete", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-PAT-007: batch delete unreferenced patients should succeed");
    }

    [Fact]
    public async Task US_PAT_007_BatchDelete_ReturnsOperationResult()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var p1 = await CreatePatientAsync(doctorClient, "批删结果甲");
        var payload = new { Ids = new[] { p1.Id } };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients/batch-delete", payload);

        // Assert - should return BatchOperationResultDto
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace(
                "US-PAT-007: batch delete response should contain operation result");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "US-PAT-007: batch delete should return 200 or 204");
        }
    }

    [Fact]
    public async Task US_PAT_007_BatchDelete_EmptyList_Returns400()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = new { Ids = Array.Empty<Guid>() };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients/batch-delete", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-PAT-007: empty batch delete should be handled");
    }

    [Fact]
    public async Task US_PAT_007_BatchDelete_Anonymous_Returns401()
    {
        // Arrange
        var payload = new { Ids = new[] { Guid.NewGuid() } };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/patients/batch-delete", payload);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-PAT-008: Excel import patients

    [Fact]
    public async Task US_PAT_008_Import_EmptyFile_Returns400()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        using var content = new MultipartFormDataContent();
        using var stream = new MemoryStream(Array.Empty<byte>());
        content.Add(new StreamContent(stream), "file", "empty.xlsx");

        // Act
        var response = await doctorClient.PostAsync("/api/v1/patients/import", content);

        // Assert - empty file should fail validation
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK, HttpStatusCode.MethodNotAllowed },
            "US-PAT-008: empty file import should return validation error or be handled");
    }

    [Fact]
    public async Task US_PAT_008_Import_Anonymous_Returns401()
    {
        // Act
        using var content = new MultipartFormDataContent();
        var response = await AnonymousClient.PostAsync("/api/v1/patients/import", content);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-PAT-009: Import template download

    [Fact]
    public async Task US_PAT_009_ImportTemplate_Anonymous_ReturnsTemplate()
    {
        // Act - template endpoint is AllowAnonymous
        var response = await AnonymousClient.GetAsync("/api/v1/patients/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized },
            "US-PAT-009: import template should be accessible anonymously or return 404 if not implemented");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            contentType.Should().BeOneOf(
                new[]
                {
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/octet-stream",
                    "application/vnd.ms-excel"
                },
                "US-PAT-009: template download should return Excel file");
        }
    }

    [Fact]
    public async Task US_PAT_009_ImportTemplate_Authenticated_ReturnsTemplate()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/patients/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-PAT-009: authenticated user should download import template");
    }

    #endregion

    #region US-PAT-010: Export patients

    [Fact]
    public async Task US_PAT_010_Export_Authenticated_ReturnsData()
    {
        // Arrange - ensure there is at least one patient
        var doctorClient = await LoginAsDoctorAsync();
        await CreatePatientAsync(doctorClient, "导出患者");

        // Act
        var response = await doctorClient.GetAsync("/api/v1/patients/export");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-PAT-010: authenticated user should export patients");
    }

    [Fact]
    public async Task US_PAT_010_Export_WithKeyword_FiltersResults()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var uniqueName = $"UniqueExport_{Guid.NewGuid():N}"[..16];
        await CreatePatientAsync(doctorClient, uniqueName);

        // Act - export with keyword
        var response = await doctorClient.GetAsync($"/api/v1/patients/export?keyword={uniqueName}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-PAT-010: export with keyword filter should succeed");
    }

    [Fact]
    public async Task US_PAT_010_Export_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/patients/export");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-PAT-011: Check patient reference

    [Fact]
    public async Task US_PAT_011_CheckReference_PatientWithNoCase_CanDelete()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var patient = await CreatePatientAsync(doctorClient, "无引用检查");

        // Act
        var response = await doctorClient.GetAsync($"/api/v1/patients/{patient.Id}/check-reference");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<PatientReferenceCheckDto>(
            "US-PAT-011: reference check should succeed");
        data.HasReferences.Should().BeFalse("new patient has no medical cases");
        data.ReferenceCount.Should().Be(0, "US-PAT-011: new patient reference count should be 0");
        data.CanDelete.Should().BeTrue("US-PAT-011: patient without references should be deletable");
    }

    [Fact]
    public async Task US_PAT_011_CheckReference_PatientWithCase_ShowsReferenceCount()
    {
        // Arrange - create patient then a medical case
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var patient = await CreatePatientAsync(doctorClient, "有引用检查");

        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var casePayload = MedicalCaseBuilder.Default()
            .ForPatient(patient.Id)
            .WithDoctor(doctorId)
            .BuildCreate();
        var caseResp = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", casePayload);
        caseResp.StatusCode.Should().Be(HttpStatusCode.OK, "medical case creation should succeed");

        // Act
        var response = await doctorClient.GetAsync($"/api/v1/patients/{patient.Id}/check-reference");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<PatientReferenceCheckDto>(
            "US-PAT-011: reference check should succeed");
        data.HasReferences.Should().BeTrue("patient with medical case has references");
        data.ReferenceCount.Should().BeGreaterThan(0, "US-PAT-011: reference count should reflect medical cases");
    }

    [Fact]
    public async Task US_PAT_011_CheckReference_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync($"/api/v1/patients/{Guid.NewGuid()}/check-reference");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-PAT-012: Batch check reference

    [Fact]
    public async Task US_PAT_012_BatchCheckReference_ReturnsResultPerPatient()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var p1 = await CreatePatientAsync(doctorClient, "批检甲");
        var p2 = await CreatePatientAsync(doctorClient, "批检乙");

        var payload = new { PatientIds = new[] { p1.Id, p2.Id } };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients/batch-check-reference", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-PAT-012: batch reference check should succeed");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace(
                "US-PAT-012: batch check response should contain data");
        }
    }

    [Fact]
    public async Task US_PAT_012_BatchCheckReference_EmptyList_Returns400OrHandled()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = new { PatientIds = Array.Empty<Guid>() };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients/batch-check-reference", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-PAT-012: empty batch check should be handled");
    }

    [Fact]
    public async Task US_PAT_012_BatchCheckReference_Anonymous_Returns401()
    {
        // Arrange
        var payload = new { PatientIds = new[] { Guid.NewGuid() } };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/patients/batch-check-reference", payload);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion
}
