using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Integration.MedicalCases;

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
[Collection("ServerIntegration")]
public class MedicalCasePermissionAndFilterTests
{
    private readonly WebApiFixture _fixture;
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/medicalcases";
    private const string PatientUrl = "/api/v1/patients";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Doctor A: Fixture 预设的 DoctorClient
    private static readonly Guid DoctorAId = WebApiFixture.DoctorUserId;

    // Doctor B: 动态创建的第二个医生
    private static readonly Guid DoctorBId = Guid.Parse("00000000-0000-0000-0000-000000000042");

    public MedicalCasePermissionAndFilterTests(WebApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
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
        var input = new PatientInputDto
        {
            Name = "权限测试患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = $"139{Random.Shared.Next(10000000, 99999999)}",
            Address = "集成测试地址"
        };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(PatientUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建患者失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>Doctor A 创建医案 (通过 DoctorClient)</summary>
    private async Task<MedicalCaseDetailDto> CreateMedicalCaseByDoctorAAsync(Guid patientId)
    {
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = DoctorAId, // FluentValidation 要求 UserId 不为空
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "权限测试",
                TcmDiagnosis = "气滞血瘀"
            }
        };
        var resp = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"DoctorA 创建医案失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    /// <summary>创建 Doctor B 客户端</summary>
    private HttpClient CreateDoctorBClient()
    {
        return _fixture.CreateClientAs(UserRole.Doctor, DoctorBId, "doctorB");
    }

    /// <summary>确保 Doctor B 用户存在于数据库中</summary>
    private async Task EnsureDoctorBSeededAsync()
    {
        await _fixture.SeedAsync(async db =>
        {
            var existing = await db.Set<LYBT.Entities.Users.User>().FindAsync(DoctorBId);
            if (existing == null)
            {
                db.Set<LYBT.Entities.Users.User>().Add(new LYBT.Entities.Users.User
                {
                    Id = DoctorBId,
                    UserName = "doctorB",
                    RealName = "测试医生B",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestDoctor2025@"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        });
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
        var resp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{medicalCase.Id}/permissions");

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
        await EnsureDoctorBSeededAsync();
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}");

        // Act -- DoctorB 查询 DoctorA 创建的医案权限
        using var doctorBClient = CreateDoctorBClient();
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
        var resp = await _fixture.DoctorClient.GetAsync(
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
        await EnsureDoctorBSeededAsync();
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseByDoctorAAsync(patientId);
        _output.WriteLine($"DoctorA 创建医案: {medicalCase.Id}, PatientId: {patientId}");

        // Act -- DoctorB 查询同一患者的未完成医案
        using var doctorBClient = CreateDoctorBClient();
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

        // Act -- Admin 查询 Pending 类型 (Admin 可见全部)
        var resp = await _fixture.AdminClient.GetAsync(
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
        var resp = await _fixture.AdminClient.PostAsJsonAsync(BaseUrl, input);

        // Assert -- Admin 不是 Doctor 角色，应返回 403
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "只有 Doctor 角色可以创建医案，Admin 应被拒绝");
    }

    #endregion
}
