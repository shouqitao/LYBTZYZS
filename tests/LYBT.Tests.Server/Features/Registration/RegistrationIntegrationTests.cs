using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Registration;

/// <summary>
/// 挂号管理模块集成测试。
/// 验证完整HTTP管线: Controller -> RegistrationService -> Repository -> DB。
/// 覆盖: CRUD、状态机流转 (Waiting -> InProgress -> Completed/Cancelled)、业务校验。
/// </summary>
public sealed class RegistrationIntegrationTests : IntegrationTestBase
{
    private static int _idSeq;

    public RegistrationIntegrationTests(ServerFixture fixture) : base(fixture) { }

    #region Helpers

    /// <summary>创建患者并返回ID和姓名</summary>
    private async Task<(Guid Id, string Name)> CreatePatientAsync(HttpClient client)
    {
        var name = "挂号测试患者_" + Guid.NewGuid().ToString("N")[..6];
        var seq = Interlocked.Increment(ref _idSeq);
        var request = new PatientInputDto
        {
            Name = name,
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            PhoneNumber = $"138{Random.Shared.Next(10000000, 99999999)}",
            IdNumber = $"11010119900101{seq:D3}X",
            Address = "北京市朝阳区"
        };

        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"创建患者失败: {response.StatusCode} - {errorBody}");
        }

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return (body!.Data!.Id, name);
    }

    /// <summary>获取医生用户信息</summary>
    private async Task<(Guid Id, string Name)> GetDoctorInfoAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=doctor");
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var doctor = body!.Data!.Items.First(u => u.Role == UserRole.Doctor);
        return (doctor.Id, doctor.RealName ?? doctor.UserName);
    }

    /// <summary>创建一条挂号记录并返回详情</summary>
    private async Task<RegistrationDetailDto> CreateRegistrationAsync(
        HttpClient client, Guid patientId, string patientName, Guid doctorId, string doctorName)
    {
        var dto = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Source = RegistrationSource.Receptionist,
            Remark = "集成测试挂号"
        };

        var response = await client.PostAsJsonAsync("/api/v1/registrations", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<RegistrationDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    #endregion

    #region Create Registration

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);

        var dto = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Source = RegistrationSource.Receptionist,
            Remark = "测试备注"
        };

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/registrations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<RegistrationDetailDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.PatientId.Should().Be(patientId);
        body.Data.DoctorId.Should().Be(doctorId);
        body.Data.Status.Should().Be(RegistrationStatus.Waiting);
        body.Data.Source.Should().Be(RegistrationSource.Receptionist);
        body.Data.Remark.Should().Be("测试备注");
        body.Data.MedicalCaseId.Should().BeNull("Waiting 状态尚未关联医案");
    }

    [Fact]
    public async Task Create_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var dto = new RegistrationInputDto
        {
            PatientId = Guid.NewGuid(),
            PatientName = "测试",
            DoctorId = Guid.NewGuid(),
            DoctorName = "医生",
            Source = RegistrationSource.Receptionist
        };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/registrations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get By Id

    [Fact]
    public async Task GetById_ExistingRegistration_ShouldReturnDetail()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        var created = await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.GetAsync($"/api/v1/registrations/{created.Id}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<RegistrationDetailDto>>(JsonOptions);
        body!.Data!.Id.Should().Be(created.Id);
        body.Data.PatientName.Should().Be(patientName);
    }

    [Fact]
    public async Task GetById_NonExistent_ShouldReturnErrorStatus()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"/api/v1/registrations/{Guid.NewGuid()}");

        // Assert - Service returns 422 (UnprocessableEntity) via HandleResult ErrorCode mapping
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    #endregion

    #region Waiting Queue

    [Fact]
    public async Task GetQueue_ShouldReturnWaitingRegistrations()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.GetAsync("/api/v1/registrations/queue");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<RegistrationListDto>>>(JsonOptions);
        body!.Data.Should().NotBeNullOrEmpty();
        body.Data!.Should().OnlyContain(r => r.Status == RegistrationStatus.Waiting);
    }

    [Fact]
    public async Task GetQueue_FilterByDoctor_ShouldReturnOnlyAssigned()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.GetAsync($"/api/v1/registrations/queue?doctorId={doctorId}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<RegistrationListDto>>>(JsonOptions);
        body!.Data.Should().NotBeNullOrEmpty();
        body.Data!.Should().OnlyContain(r => r.DoctorId == doctorId);
    }

    #endregion

    #region Paged List

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.GetAsync("/api/v1/registrations?page=1&pageSize=10");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<RegistrationListDto>>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.TotalCount.Should().BeGreaterThan(0);
        body.Data.Items.Should().NotBeEmpty();
    }

    #endregion

    #region Start Visit (State Transition: Waiting -> InProgress)

    [Fact]
    public async Task StartVisit_WaitingRegistration_ShouldTransitionToInProgress()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        var created = await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.PutAsync(
            $"/api/v1/registrations/{created.Id}/start-visit", null);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify state changed
        var getResponse = await admin.GetAsync($"/api/v1/registrations/{created.Id}");
        var detail = (await getResponse.Content
            .ReadFromJsonAsync<ApiResponse<RegistrationDetailDto>>(JsonOptions))!.Data!;
        detail.Status.Should().Be(RegistrationStatus.InProgress);
    }

    #endregion

    #region Cancel (State Transition: Waiting -> Cancelled)

    [Fact]
    public async Task Cancel_WaitingRegistration_ShouldTransitionToCancelled()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        var created = await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act
        var response = await admin.PutAsync(
            $"/api/v1/registrations/{created.Id}/cancel", null);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify state changed
        var getResponse = await admin.GetAsync($"/api/v1/registrations/{created.Id}");
        var detail = (await getResponse.Content
            .ReadFromJsonAsync<ApiResponse<RegistrationDetailDto>>(JsonOptions))!.Data!;
        detail.Status.Should().Be(RegistrationStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_InProgressRegistration_ShouldFail()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        var created = await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Transition to InProgress first
        await admin.PutAsync($"/api/v1/registrations/{created.Id}/start-visit", null);

        // Act - try to cancel InProgress registration
        var response = await admin.PutAsync(
            $"/api/v1/registrations/{created.Id}/cancel", null);

        // Assert - should fail (only Waiting can be cancelled)
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    #endregion

    #region Queue After State Transitions

    [Fact]
    public async Task GetQueue_AfterStartVisit_ShouldNotIncludeInProgress()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        var created = await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Start visit
        await admin.PutAsync($"/api/v1/registrations/{created.Id}/start-visit", null);

        // Act
        var response = await admin.GetAsync("/api/v1/registrations/queue");

        // Assert
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<RegistrationListDto>>>(JsonOptions);
        body!.Data.Should().NotContain(r => r.Id == created.Id,
            "InProgress registrations should not appear in waiting queue");
    }

    #endregion

    #region History Filter (US-REG-007)

    [Fact]
    public async Task GetList_FilterByPatientId_ShouldReturnOnlyThatPatient()
    {
        // Arrange - Create 2 patients, each with a registration
        var admin = await LoginAsAdminAsync();
        var (patientId1, patientName1) = await CreatePatientAsync(admin);
        var (patientId2, patientName2) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);

        await CreateRegistrationAsync(admin, patientId1, patientName1, doctorId, doctorName);
        await CreateRegistrationAsync(admin, patientId2, patientName2, doctorId, doctorName);

        // Act - Filter by patient 1
        var response = await admin.GetAsync($"/api/v1/registrations?patientId={patientId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<RegistrationListDto>>>(JsonOptions);
        body!.Data!.Items.Should().AllSatisfy(r =>
            r.PatientId.Should().Be(patientId1));
    }

    [Fact]
    public async Task GetList_FilterByDoctorId_ShouldReturnOnlyThatDoctor()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act - Filter by doctor
        var response = await admin.GetAsync($"/api/v1/registrations?doctorId={doctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<RegistrationListDto>>>(JsonOptions);
        body!.Data!.Items.Should().AllSatisfy(r =>
            r.DoctorId.Should().Be(doctorId));
    }

    [Fact]
    public async Task GetList_FilterByDateRange_ShouldReturnOnlyMatchingRecords()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        var today = DateTime.Today;
        var startDate = today.ToString("yyyy-MM-dd");
        var endDate = today.AddDays(1).ToString("yyyy-MM-dd");

        // Act - Filter by today's date range
        var response = await admin.GetAsync(
            $"/api/v1/registrations?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<RegistrationListDto>>>(JsonOptions);
        body!.Data!.Items.Should().NotBeEmpty("today's registration should be in range");
        body.Data.Items.Should().AllSatisfy(r =>
            r.CreatedAt.Should().BeOnOrAfter(today));
    }

    [Fact]
    public async Task GetList_FilterByPastDateRange_ShouldReturnEmpty()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (patientId, patientName) = await CreatePatientAsync(admin);
        var (doctorId, doctorName) = await GetDoctorInfoAsync(admin);
        await CreateRegistrationAsync(admin, patientId, patientName, doctorId, doctorName);

        // Act - Filter by a past date range (should not include today's registrations)
        var response = await admin.GetAsync(
            "/api/v1/registrations?startDate=2020-01-01&endDate=2020-01-02");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<RegistrationListDto>>>(JsonOptions);
        body!.Data!.Items.Should().BeEmpty("no registrations exist in 2020");
    }

    #endregion
}
