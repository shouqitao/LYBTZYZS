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
/// Should Have User Stories for Registration module.
/// PRD: US-REG-007 (Registration history query)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Registration_ShouldHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Registration_ShouldHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreatePatientAsync(
        HttpClient client, string name = "历史查询患者")
    {
        var fullName = $"{name}_{Guid.NewGuid():N}"[..12];
        var payload = PatientBuilder.Default().WithName(fullName).Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return (data.Id, data.Name);
    }

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

    #region US-REG-007: Registration history query

    [Fact]
    public async Task US_REG_007_QueryHistory_WithDateRange_ReturnsFilteredResults()
    {
        // Arrange - create registrations today
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var patient = await CreatePatientAsync(doctorClient, "日期范围查询");
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

        // Act - query with date range
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?startDate={today}&endDate={tomorrow}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            expectedMinCount: 1,
            because: "US-REG-007: date range query should return today's registrations");
    }

    [Fact]
    public async Task US_REG_007_QueryHistory_WithPagination_RespectsPageSize()
    {
        // Arrange - create multiple registrations
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        for (var i = 0; i < 3; i++)
        {
            var patient = await CreatePatientAsync(doctorClient, $"分页患者{i}");
            await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");
        }

        // Act - request page size 2
        var response = await adminClient.GetAsync("/api/v1/registrations?page=1&pageSize=2");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            because: "US-REG-007: pagination should work for history query");
        paged.Items.Should().HaveCountLessThanOrEqualTo(2,
            "US-REG-007: page size should be respected");
        paged.TotalCount.Should().BeGreaterOrEqualTo(3,
            "US-REG-007: total count should reflect all records");
    }

    [Fact]
    public async Task US_REG_007_QueryHistory_WithKeyword_ReturnsMatchingResults()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var uniquePrefix = $"关键词_{Guid.NewGuid():N}"[..8];
        var patient = await CreatePatientAsync(doctorClient, uniquePrefix);
        await CreateRegistrationAsync(adminClient, patient.Id, patient.Name, doctorId, "doctor");

        // Act - search by keyword (patient name)
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?keyword={uniquePrefix}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            expectedMinCount: 1,
            because: "US-REG-007: keyword search should match patient name in registration history");
    }

    [Fact]
    public async Task US_REG_007_QueryHistory_FutureDateRange_ReturnsEmpty()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var futureStart = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var futureEnd = DateTime.UtcNow.AddDays(31).ToString("yyyy-MM-dd");

        // Act - query with future date range (no registrations expected)
        var response = await adminClient.GetAsync(
            $"/api/v1/registrations?startDate={futureStart}&endDate={futureEnd}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<RegistrationListDto>(
            because: "US-REG-007: future date range should return empty result");
        paged.Items.Should().BeEmpty(
            "US-REG-007: no registrations should exist in the future");
    }

    #endregion
}
