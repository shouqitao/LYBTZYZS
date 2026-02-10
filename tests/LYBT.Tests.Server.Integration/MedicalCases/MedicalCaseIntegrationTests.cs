using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.MedicalCases;

/// <summary>
/// 医案聚合根全流程集成测试 -- 核心测试。
/// MedicalCase是最复杂的聚合根，包含:
///   - Consultation (共享主键 = MedicalCaseId)
///   - Prescription (独立ID) + PrescriptionHerbItems
///
/// 授权: DoctorOrAdmin，但Create只允许Doctor，不允许Admin。
/// 资源级授权: MedicalCaseAuthorizationHandler (Doctor只能编辑自己的)。
/// 状态流转: Draft -> Active -> Completed/Cancelled。
/// CQRS: CommandService / QueryService / StateService 分离。
/// </summary>
[Collection("ServerIntegration")]
public class MedicalCaseIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private const string BaseUrl = "/api/v1/medicalcases";
    private const string PatientUrl = "/api/v1/patients";
    private const string HerbUrl = "/api/v1/herbs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public MedicalCaseIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    #region Helpers

    /// <summary>创建测试患者并返回ID</summary>
    private async Task<Guid> CreatePatientAsync(string? name = null)
    {
        var input = new PatientInputDto
        {
            Name = name ?? "医案患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male
        };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(PatientUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>创建测试药材并返回(id, name, price)</summary>
    private async Task<(Guid id, string name)> CreateHerbAsync(
        string? name = null, decimal price = 10.0m)
    {
        var herbName = name ?? "测试药材_" + Guid.NewGuid().ToString("N")[..4];
        var input = new HerbInputDto { Name = herbName, Unit = "克", Price = price };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(HerbUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        return (body!.Data!.Id, herbName);
    }

    /// <summary>创建医案(Doctor创建)</summary>
    private async Task<(Guid id, MedicalCaseDetailDto dto)> CreateMedicalCaseAsync(
        Guid? patientId = null, ConsultationInputDto? consultation = null)
    {
        var pid = patientId ?? await CreatePatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = pid,
            UserId = WebApiFixture.DoctorUserId, // FluentValidation要求UserId不为空
            Consultation = consultation ?? new ConsultationInputDto
            {
                PresentIllness = "头痛三日",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "弦数",
                TcmDiagnosis = "肝阳上亢"
            }
        };

        var response = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, input);
        response.IsSuccessStatusCode.Should().BeTrue(
            $"创建医案应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return (body!.Data!.Id, body.Data);
    }

    #endregion

    #region Create MedicalCase

    [Fact]
    public async Task CreateMedicalCase_WithConsultation_ShouldPersist()
    {
        // Arrange
        var patientId = await CreatePatientAsync("创建测试患者");

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "咳嗽一周",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "浮紧",
                TcmDiagnosis = "风寒袭肺"
            }
        };

        // Act - Doctor创建
        var response = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"创建医案应成功, 实际: {response.StatusCode}");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().NotBe(Guid.Empty);
        body.Data.PatientId.Should().Be(patientId);
        body.Data.CaseStatus.Should().Be(MedicalCaseStatus.Active, "新医案应为Active状态");
        // 通过GetById验证Consultation和CaseNumber
        var getResp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{body.Data.Id}");
        var detail = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        // 注: CaseNumber可能由特定业务流程触发生成，此处不断言
        detail!.Data!.Consultation.Should().NotBeNull("应包含诊断信息");
        detail.Data.Consultation!.TcmDiagnosis.Should().Be("风寒袭肺");
    }

    [Fact]
    public async Task CreateMedicalCase_AdminShouldBeForbidden()
    {
        // Arrange - Admin不能创建医案（只有Doctor可以）
        var patientId = await CreatePatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = WebApiFixture.AdminUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync(BaseUrl, input);

        // Assert - Admin应被403
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Admin不能创建医案，只有Doctor可以");
    }

    #endregion

    #region Get MedicalCase

    [Fact]
    public async Task GetMedicalCase_ById_ShouldReturnDetailWithConsultation()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{caseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.Id.Should().Be(caseId);
        body.Data.Consultation.Should().NotBeNull("详情应包含诊断子实体");
    }

    [Fact]
    public async Task GetMedicalCase_NonExistentId_ShouldReturn404()
    {
        var response = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMedicalCaseList_ShouldReturnPagedResults()
    {
        // Arrange
        await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMedicalCaseList_InvalidPagination_ShouldReturn400()
    {
        var response = await _fixture.DoctorClient.GetAsync($"{BaseUrl}?page=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Aggregate Save (PUT /{id})

    [Fact]
    public async Task Save_UpdateConsultation_ShouldModifyDiagnosis()
    {
        // Arrange - 创建医案
        var (caseId, original) = await CreateMedicalCaseAsync();
        original.Consultation!.TcmDiagnosis.Should().Be("肝阳上亢");

        // Act - 更新诊断
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = WebApiFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛加剧伴恶心",
                TongueDiagnosis = "舌红苔黄腻",
                PulseDiagnosis = "弦滑",
                TcmDiagnosis = "肝阳化风"
            }
        };
        var response = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.Consultation!.TcmDiagnosis.Should().Be("肝阳化风");

        // Verify persistence
        var getResp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{caseId}");
        var fetched = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        fetched!.Data!.Consultation!.TcmDiagnosis.Should().Be("肝阳化风", "更新应已持久化");
    }

    [Fact]
    public async Task Save_WithPrescription_ShouldSucceed()
    {
        // Arrange - 创建医案 + 创建药材
        var (caseId, original) = await CreateMedicalCaseAsync();
        var (herbId1, herbName1) = await CreateHerbAsync("处方药材1", 15.0m);

        // Step 1: 先设置处方标志
        var flagReq = new { NeedsPrescription = true };
        var flagResp = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/prescription-flag", flagReq);
        flagResp.IsSuccessStatusCode.Should().BeTrue("设置处方标志应成功");

        // Step 2: 聚合保存: 诊断 + 处方
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = WebApiFixture.DoctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                TcmDiagnosis = "肝阳上亢"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = caseId,
                DosageCount = 7,
                Usage = "水煎服，每日一剂",
                Advice = "忌辛辣刺激",
                Discount = 1.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herbId1,
                        HerbName = herbName1,
                        Dosage = 10,
                        Unit = "克",
                        UnitPrice = 15.0m,
                        Subtotal = 150.0m
                    }
                }
            }
        };

        var response = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert - 聚合保存应成功
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Status Lifecycle

    [Fact]
    public async Task CloseMedicalCase_ShouldSetCompleted()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .PutAsync($"{BaseUrl}/{caseId}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed, "关闭后应为Completed");
    }

    [Fact]
    public async Task CancelMedicalCase_ShouldSetCancelled()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var cancelReq = new { Reason = "患者取消就诊" };
        var response = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/cancel", cancelReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Cancelled, "取消后应为Cancelled");
    }

    [Fact]
    public async Task SaveDraft_ShouldSetDraftStatus()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var draftInput = new ConsultationInputDto
        {
            TcmDiagnosis = "草稿诊断"
        };
        var response = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/draft", draftInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Draft, "暂存后应为Draft");
    }

    [Fact]
    public async Task SetPrescriptionFlag_ShouldUpdate()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var flagReq = new { NeedsPrescription = true };
        var response = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/prescription-flag", flagReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Delete MedicalCase

    [Fact]
    public async Task DeleteMedicalCase_ShouldSoftDelete()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .DeleteAsync($"{BaseUrl}/{caseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "删除应返回204");

        // Verify
        var getResp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{caseId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BatchDelete_MultipleCases_ShouldSoftDeleteAll()
    {
        // Arrange
        var ids = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var (id, _) = await CreateMedicalCaseAsync();
            ids.Add(id);
        }

        // Act
        var response = await _fixture.DoctorClient
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = ids });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var id in ids)
        {
            var get = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{id}");
            get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task AnonymousRequest_ShouldReturn401()
    {
        var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCanViewMedicalCase_ButNotCreate()
    {
        // Arrange - Doctor创建医案
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act - Admin可以查看
        var getResponse = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{caseId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin应能查看医案");
    }

    [Fact]
    public async Task GetPermissions_ShouldReturnPermissionInfo()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .GetAsync($"{BaseUrl}/{caseId}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCasePermissionDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    #endregion

    #region Queries

    [Fact]
    public async Task GetConsultationList_ShouldReturnForCase()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .GetAsync($"{BaseUrl}/{caseId}/consultations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrescriptionList_ShouldReturnForCase()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .GetAsync($"{BaseUrl}/{caseId}/prescriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchMedicalCases_ByPatientName_ShouldReturnResults()
    {
        // Arrange
        var uniqueName = "搜索患者_" + Guid.NewGuid().ToString("N")[..4];
        var pid = await CreatePatientAsync(uniqueName);
        await CreateMedicalCaseAsync(patientId: pid);

        // Act
        var response = await _fixture.DoctorClient
            .GetAsync($"{BaseUrl}/search?patientName={uniqueName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseDetailDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().Contain(c => c.PatientName.Contains(uniqueName));
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnPagedLogs()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();

        // Act
        var response = await _fixture.DoctorClient
            .GetAsync($"{BaseUrl}/{caseId}/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Full Flow: Create -> Diagnose -> Prescribe -> Close

    [Fact]
    public async Task FullFlow_CreateDiagnosePrescribeClose_ShouldSucceed()
    {
        // Step 1: 创建患者
        var patientId = await CreatePatientAsync("全流程患者");

        // Step 2: 创建药材
        var (herbId, herbName) = await CreateHerbAsync("全流程药材", 18.0m);

        // Step 3: 创建医案(含诊断)
        var createInput = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "失眠多梦一月",
                TongueDiagnosis = "舌红少苔",
                PulseDiagnosis = "细数",
                TcmDiagnosis = "阴虚火旺"
            }
        };
        var createResp = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, createInput);
        createResp.IsSuccessStatusCode.Should().BeTrue("Step3: 创建应成功");
        var created = await createResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        var caseId = created!.Data!.Id;
        created.Data.CaseStatus.Should().Be(MedicalCaseStatus.Active);

        // Step 4: 聚合保存(诊断+处方)
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "失眠多梦一月",
                TcmDiagnosis = "阴虚火旺"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = caseId,
                DosageCount = 14,
                Usage = "水煎服",
                Advice = "忌辛辣",
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herbId,
                        HerbName = herbName,
                        Dosage = 15,
                        Unit = "克",
                        UnitPrice = 18.0m,
                        Subtotal = 270.0m
                    }
                }
            }
        };
        var saveResp = await _fixture.DoctorClient
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);
        saveResp.IsSuccessStatusCode.Should().BeTrue("Step4: 聚合保存应成功");
        var saved = await saveResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        saved!.Data!.HasPrescription.Should().BeTrue("Step4: 应有处方");
        saved.Data.Prescription!.DosageCount.Should().Be(14);

        // Step 5: 关闭医案
        var closeResp = await _fixture.DoctorClient
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue("Step5: 关闭应成功");
        var closed = await closeResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        closed!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed,
            "Step5: 关闭后应为Completed");

        // Step 6: 验证完整状态
        var finalResp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{caseId}");
        var final = await finalResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        final!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        final.Data.HasConsultation.Should().BeTrue("应有诊断");
        final.Data.HasPrescription.Should().BeTrue("应有处方");
    }

    #endregion
}
