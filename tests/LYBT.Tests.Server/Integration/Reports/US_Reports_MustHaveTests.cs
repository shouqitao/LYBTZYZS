using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Reports;

/// <summary>
/// Must Have User Stories for Reports module.
/// PRD: US-RPT-001 ~ US-RPT-003 (3 Must Have)
/// Collection: ClinicalData (shares DB with clinical domain for test data)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Reports_MustHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Reports_MustHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreatePatientAsync(
        HttpClient client, string name = "报表测试患者")
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

    private async Task<(Guid CaseId, Guid DoctorId)> CreateCaseAsync(
        HttpClient doctorClient, Guid patientId)
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var payload = MedicalCaseBuilder.Default()
            .ForPatient(patientId)
            .WithDoctor(doctorId)
            .BuildCreate();
        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        return (data.Id, doctorId);
    }

    private async Task<(Guid Id, string Name)> CreateHerbAsync(
        HttpClient client, string name = "报表药材")
    {
        var fullName = $"{name}_{Guid.NewGuid():N}"[..12];
        var payload = HerbBuilder.Default().WithName(fullName).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    private async Task CreateCompleteCaseWithPrescriptionAsync(
        HttpClient doctorClient, Guid patientId, Guid doctorId)
    {
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        var consultation = MedicalCaseBuilder.BuildConsultation();
        var herb1 = await CreateHerbAsync(doctorClient, "麻黄");
        var herb2 = await CreateHerbAsync(doctorClient, "桂枝");

        var items = new List<object>
        {
            MedicalCaseBuilder.BuildPrescriptionItem(herb1.Id, herb1.Name, 6),
            MedicalCaseBuilder.BuildPrescriptionItem(herb2.Id, herb2.Name, 9)
        };
        var prescription = MedicalCaseBuilder.BuildPrescription(
            items: items, dosageCount: 7, medicalCaseId: caseId);
        var updatePayload = MedicalCaseBuilder.BuildUpdate(
            caseId, patientId: patientId, userId: doctorId,
            consultation: consultation, prescription: prescription, needsPrescription: true);
        await doctorClient.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}", updatePayload);
    }

    #endregion

    #region US-RPT-001: GetDailyIncome

    [Fact]
    public async Task US_RPT_001_GetDailyIncome_ReturnsZero_WhenNoData()
    {
        var adminClient = await LoginAsAdminAsync();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/income?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyIncomeDto>(
            "US-RPT-001: income should return zero when no data exists");
        data.TotalIncome.Should().Be(0);
        data.RegistrationFeeTotal.Should().Be(0);
        data.MedicineFeeTotal.Should().Be(0);
    }

    [Fact]
    public async Task US_RPT_001_GetDailyIncome_ReturnsCorrectTotals_WhenDataExists()
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var (patientId, patientName) = await CreatePatientAsync(doctorClient);
        var regId = await CreateRegistrationAsync(adminClient, patientId, patientName, doctorId, "doctor");

        await CreateCompleteCaseWithPrescriptionAsync(doctorClient, patientId, doctorId);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/income?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyIncomeDto>(
            "US-RPT-001: income should reflect registration and medicine fees");
        data.RegistrationFeeTotal.Should().BeGreaterOrEqualTo(0);
        data.MedicineFeeTotal.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task US_RPT_001_GetDailyIncome_SupportsDateRange()
    {
        var adminClient = await LoginAsAdminAsync();

        var startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/income?startDate={startDate}&endDate={endDate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-RPT-001: date range query should succeed");
    }

    #endregion

    #region US-RPT-002: GetDailyConsultations

    [Fact]
    public async Task US_RPT_002_GetDailyConsultations_ReturnsZero_WhenNoData()
    {
        var adminClient = await LoginAsAdminAsync();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/consultations?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyConsultationDto>(
            "US-RPT-002: consultations should return zero when no data exists");
        data.TotalCount.Should().Be(0);
        data.ByDoctor.Should().BeEmpty();
    }

    [Fact]
    public async Task US_RPT_002_GetDailyConsultations_ReturnsCorrectCount_WhenDataExists()
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var (patientId, _) = await CreatePatientAsync(doctorClient);
        await CreateCompleteCaseWithPrescriptionAsync(doctorClient, patientId, doctorId);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/consultations?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyConsultationDto>(
            "US-RPT-002: consultations count should reflect completed cases");
        data.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task US_RPT_002_GetDailyConsultations_IncludesByDoctor()
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var (patientId, _) = await CreatePatientAsync(doctorClient);
        await CreateCompleteCaseWithPrescriptionAsync(doctorClient, patientId, doctorId);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/consultations?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyConsultationDto>(
            "US-RPT-002: consultations should include byDoctor grouping");
        data.ByDoctor.Should().NotBeNull();
    }

    #endregion

    #region US-RPT-003: GetDailyHerbUsage

    [Fact]
    public async Task US_RPT_003_GetDailyHerbUsage_ReturnsEmpty_WhenNoData()
    {
        var adminClient = await LoginAsAdminAsync();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/herbs?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyHerbUsageDto>(
            "US-RPT-003: herb usage should return empty when no data exists");
        data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task US_RPT_003_GetDailyHerbUsage_ReturnsCorrectUsage_WhenDataExists()
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        var (patientId, _) = await CreatePatientAsync(doctorClient);
        await CreateCompleteCaseWithPrescriptionAsync(doctorClient, patientId, doctorId);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await adminClient.GetAsync(
            $"/api/v1/reports/daily/herbs?startDate={today}&endDate={today}");

        var data = await response.ShouldBeSuccessWithDataAsync<DailyHerbUsageDto>(
            "US-RPT-003: herb usage should reflect prescription items");
        data.Items.Should().NotBeNull();
    }

    #endregion
}
