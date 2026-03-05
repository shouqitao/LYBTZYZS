using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Features.MedicalCases;

/// <summary>
/// 医案权限控制+查询过滤集成测试 -- 基于实际 API 重设计。
/// 替代: MedicalCasePermissionControlTests + MedicalCaseDoctorFilterTests
///
/// 测试场景:
///   1. CanEdit 权限: 同医生 true, 不同医生 false
///   2. 统一查询端点: /query?queryType=Unfinished/Pending 替代已删除的独立端点
///   3. Admin 可见全部, Doctor 仅见自己的
///
/// 关键端点:
///   GET /api/v1/medicalcases/{id}/permissions -> ApiResponse of MedicalCasePermissionDto
///   GET /api/v1/medicalcases/query?queryType=... -> ApiResponse of PagedResult of MedicalCaseListDto
///   POST /api/v1/medicalcases [Roles:Doctor] -> 仅 Doctor 可创建
/// </summary>
public sealed class MedicalCasePermissionAndFilterTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/medicalcases";
    private const string PatientUrl = "/api/v1/patients";

    // Doctor B credentials for dynamic creation
    private const string DoctorBUserName = "doctorB";
    private const string DoctorBPassword = "TestDoctorB2025@";

    public MedicalCasePermissionAndFilterTests(ServerFixture fixture, ITestOutputHelper output)
        : base(fixture)
    {
        _output = output;
    }

    #region Helpers

    private static int _idSeq;
    private static string UniqueIdNumber()
    {
        var seq = Interlocked.Increment(ref _idSeq);
        return $"31010019900301{seq:D3}X";
    }

    /// <summary>创建测试患者 (通过 API)</summary>
    private async Task<Guid> CreatePatientAsync()
    {
        var admin = await LoginAsAdminAsync();
        var input = new PatientInputDto
        {
            Name = "权限测试患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = $"139{Random.Shared.Next(10000000, 99999999)}",
            Address = "集成测试地址"
        };
        var resp = await admin.PostAsJsonAsync(PatientUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建患者失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>Doctor A 创建医案 (通过 DoctorClient)</summary>
    private async Task<MedicalCaseDetailDto> CreateMedicalCaseByDoctorAAsync(Guid patientId)
    {
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorAId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorAId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "权限测试",
                TcmDiagnosis = "气滞血瘀"
            }
        };
        var resp = await doctor.PostAsJsonAsync(BaseUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"DoctorA 创建医案失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    /// <summary>确保 Doctor B 用户存在于数据库中，并返回可用的 HttpClient</summary>
    private async Task<HttpClient> EnsureDoctorBAndLoginAsync()
    {
        // Seed Doctor B via Admin API
        var admin = await LoginAsAdminAsync();

        // Check if doctorB already exists
        var checkResp = await admin.GetAsync($"/api/v1/users?keyword={DoctorBUserName}");
        checkResp.EnsureSuccessStatusCode();
        var checkBody = await checkResp.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var existingDoctorB = checkBody!.Data!.Items.FirstOrDefault(u => u.UserName == DoctorBUserName);

        if (existingDoctorB == null)
        {
            // Create Doctor B via Admin API
            var createRequest = new UserInputDto
            {
                UserName = DoctorBUserName,
                RealName = "测试医生B",
                Role = UserRole.Doctor,
                Password = DoctorBPassword,
                ConfirmPassword = DoctorBPassword,
                PhoneNumber = $"136{Random.Shared.Next(10000000, 99999999)}"
            };
            var createResp = await admin.PostAsJsonAsync("/api/v1/users", createRequest);
            createResp.IsSuccessStatusCode.Should().BeTrue(
                $"创建DoctorB失败: {createResp.StatusCode}");
        }

        // Login as Doctor B
        return await Fixture.LoginAsAsync(DoctorBUserName, DoctorBPassword);
    }

    #endregion

    #region CanEdit Permission Tests

    [Fact]
    public async Task Permissions_SameDoctorShouldCanEdit()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}");

        // Act -- DoctorA 查询自己创建的医案权限
        var doctor = await LoginAsDoctorAsync();
        var resp = await doctor.GetAsync($"{BaseUrl}/{medicalCase.Id}/permissions");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCasePermissionDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.CanEdit.Should().BeTrue("同一医生应可编辑自己创建的医案");
    }

    [Fact]
    public async Task Permissions_DifferentDoctorShouldNotCanEdit()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}");

        // Act -- DoctorB 查询 DoctorA 创建的医案权限
        var doctorBClient = await EnsureDoctorBAndLoginAsync();
        var resp = await doctorBClient.GetAsync($"{BaseUrl}/{medicalCase.Id}/permissions");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCasePermissionDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.CanEdit.Should().BeFalse("不同医生不应能编辑他人创建的医案");
    }

    #endregion

    #region Unified Query Endpoint: /query

    [Fact]
    public async Task Query_Unfinished_ShouldReturnOnlyOwnCases()
    {
        // Arrange -- DoctorA 创建医案
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}, PatientId: {patientId}");

        // Act -- DoctorA 查询自己的未完成医案
        var doctor = await LoginAsDoctorAsync();
        var resp = await doctor.GetAsync(
            $"{BaseUrl}/query?queryType=Unfinished&patientId={patientId}");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty("DoctorA 应能看到自己的未完成医案");

        // 验证返回的都是自己的医案
        body.Data.Items.Should().Contain(x => x.Id == medicalCase.Id);
    }

    [Fact]
    public async Task Query_Unfinished_DifferentDoctorShouldNotSeeOthersCases()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}, PatientId: {patientId}");

        // Act -- DoctorB 查询同一患者的未完成医案
        var doctorBClient = await EnsureDoctorBAndLoginAsync();
        var resp = await doctorBClient.GetAsync(
            $"{BaseUrl}/query?queryType=Unfinished&patientId={patientId}");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();

        // DoctorB 不应看到 DoctorA 的医案
        body.Data!.Items.Should().NotContain(x => x.Id == medicalCase.Id,
            "DoctorB 不应能查看 DoctorA 的未完成医案");
    }

    [Fact]
    public async Task Query_Pending_AdminShouldSeeAllCases()
    {
        // Arrange -- DoctorA 创建医案 (状态默认 Active/Draft)
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}");

        // Act -- Admin 查询 All 类型 (Admin 可见全部)
        var admin = await LoginAsAdminAsync();
        var resp = await admin.GetAsync(
            $"{BaseUrl}/query?queryType=All&pageSize=100");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();

        // Admin 应能看到 DoctorA 创建的医案
        body.Data!.Items.Should().Contain(x => x.Id == medicalCase.Id,
            "Admin 应能查看所有医生的医案");
    }

    #endregion

    #region Create Authorization

    [Fact]
    public async Task Create_AdminShouldBeForbidden()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var input = new MedicalCaseInputDto { PatientId = patientId };

        // Act -- AdminClient 创建医案 (应被 [Roles:Doctor] 拒绝)
        var admin = await LoginAsAdminAsync();
        var resp = await admin.PostAsJsonAsync(BaseUrl, input);

        // Assert -- Admin 不是 Doctor 角色，应返回 403
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "只有 Doctor 角色可以创建医案，Admin 应被拒绝");
    }

    #endregion
}
