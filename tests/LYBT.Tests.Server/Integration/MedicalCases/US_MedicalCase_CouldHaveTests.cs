using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.MedicalCases;

/// <summary>
/// Could Have User Stories for MedicalCases module.
/// PRD: US-MC-012 (Audit log for medical case operations)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_MedicalCase_CouldHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_MedicalCase_CouldHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var payload = PatientBuilder.Default()
            .WithName($"CH医案患者_{Guid.NewGuid():N}"[..12])
            .Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return data.Id;
    }

    private async Task<Guid> CreateCaseAsync(HttpClient doctorClient, Guid patientId)
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var payload = MedicalCaseBuilder.Default()
            .ForPatient(patientId)
            .WithDoctor(doctorId)
            .BuildCreate();
        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        return data.Id;
    }

    #endregion

}
