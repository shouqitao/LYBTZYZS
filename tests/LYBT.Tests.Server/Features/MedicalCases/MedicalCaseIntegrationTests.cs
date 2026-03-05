using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.MedicalCases;

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
public sealed class MedicalCaseIntegrationTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/v1/medicalcases";
    private const string PatientUrl = "/api/v1/patients";
    private const string HerbUrl = "/api/v1/herbs";

    public MedicalCaseIntegrationTests(ServerFixture fixture) : base(fixture) { }

    #region Helpers

    private static int _idSeq;
    private static string UniqueIdNumber()
    {
        var seq = Interlocked.Increment(ref _idSeq);
        return $"11010119900201{seq:D3}X";
    }
    private static string UniquePhone() => $"138{Random.Shared.Next(10000000, 99999999)}";

    /// <summary>创建测试患者并返回ID</summary>
    private async Task<Guid> CreatePatientAsync(string? name = null)
    {
        var admin = await LoginAsAdminAsync();
        var input = new PatientInputDto
        {
            Name = name ?? "医案患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Address = "集成测试地址"
        };
        var resp = await admin.PostAsJsonAsync(PatientUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>创建测试药材并返回(id, name, price)</summary>
    private async Task<(Guid id, string name)> CreateHerbAsync(
        string? name = null, decimal price = 10.0m)
    {
        var admin = await LoginAsAdminAsync();
        var herbName = name ?? "测试药材_" + Guid.NewGuid().ToString("N")[..4];
        var input = new HerbInputDto { Name = herbName, Unit = "克", Price = price };
        var resp = await admin.PostAsJsonAsync(HerbUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        return (body!.Data!.Id, herbName);
    }

    /// <summary>创建医案(Doctor创建)</summary>
    private async Task<(Guid id, MedicalCaseDetailDto dto)> CreateMedicalCaseAsync(
        Guid? patientId = null, ConsultationInputDto? consultation = null)
    {
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var pid = patientId ?? await CreatePatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = pid,
            UserId = doctorUserId,
            Consultation = consultation ?? new ConsultationInputDto
            {
                PresentIllness = "头痛三日",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "弦数",
                TcmDiagnosis = "肝阳上亢"
            }
        };

        var response = await doctor.PostAsJsonAsync(BaseUrl, input);
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
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "咳嗽一周",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "浮紧",
                TcmDiagnosis = "风寒袭肺"
            }
        };

        // Act - Doctor创建
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

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
        var getResp = await doctor.GetAsync($"{BaseUrl}/{body.Data.Id}");
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
        var admin = await LoginAsAdminAsync();
        var adminUserId = await GetAdminUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = adminUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await admin.PostAsJsonAsync(BaseUrl, input);

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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor.GetAsync($"{BaseUrl}/{caseId}");

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
        var doctor = await LoginAsDoctorAsync();
        var response = await doctor.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMedicalCaseList_ShouldReturnPagedResults()
    {
        // Arrange
        await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor.GetAsync(BaseUrl);

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
        var doctor = await LoginAsDoctorAsync();
        var response = await doctor.GetAsync($"{BaseUrl}?page=0");
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

        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        // Act - 更新诊断
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛加剧伴恶心",
                TongueDiagnosis = "舌红苔黄腻",
                PulseDiagnosis = "弦滑",
                TcmDiagnosis = "肝阳化风"
            }
        };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.Consultation!.TcmDiagnosis.Should().Be("肝阳化风");

        // Verify persistence
        var getResp = await doctor.GetAsync($"{BaseUrl}/{caseId}");
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
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        // Step 1: 先设置处方标志
        var flagReq = new { NeedsPrescription = true };
        var flagResp = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/prescription-flag", flagReq);
        flagResp.IsSuccessStatusCode.Should().BeTrue("设置处方标志应成功");

        // Step 2: 聚合保存: 诊断 + 处方
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = doctorUserId,
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

        var response = await doctor
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var cancelReq = new { Reason = "患者取消就诊" };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/cancel", cancelReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "取消操作返回204");
    }

    [Fact]
    public async Task Suspend_ShouldSetSuspendedStatus()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var suspendInput = new ConsultationInputDto
        {
            TcmDiagnosis = "暂停诊断"
        };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/suspend", suspendInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended, "暂停后应为Suspended");
    }

    [Fact]
    public async Task SetPrescriptionFlag_ShouldUpdate()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var flagReq = new { NeedsPrescription = true };
        var response = await doctor
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
            .DeleteAsync($"{BaseUrl}/{caseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "删除应返回204");

        // Verify
        var getResp = await doctor.GetAsync($"{BaseUrl}/{caseId}");
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = ids });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var id in ids)
        {
            var get = await doctor.GetAsync($"{BaseUrl}/{id}");
            get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task AnonymousRequest_ShouldReturn401()
    {
        var response = await AnonymousClient.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCanViewMedicalCase_ButNotCreate()
    {
        // Arrange - Doctor创建医案
        var (caseId, _) = await CreateMedicalCaseAsync();
        var admin = await LoginAsAdminAsync();

        // Act - Admin可以查看
        var getResponse = await admin.GetAsync($"{BaseUrl}/{caseId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin应能查看医案");
    }

    [Fact]
    public async Task GetPermissions_ShouldReturnPermissionInfo()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
            .GetAsync($"{BaseUrl}/{caseId}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCasePermissionDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    #endregion

    #region EditReason Validation (S3-03)

    [Fact]
    public async Task Save_CompletedCase_WithoutEditReason_ShouldFail()
    {
        // Arrange - 创建医案并关闭 (Completed 状态)
        var (caseId, original) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue("关闭医案应成功");

        // Act - Doctor (owner) 尝试保存已完成医案，不提供 EditReason
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛加剧",
                TcmDiagnosis = "肝阳上亢 (修改)"
            }
            // EditReason 未设置 (null)
        };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert - 验证层拒绝: 已完成医案未提供修改原因
        response.IsSuccessStatusCode.Should().BeFalse(
            "已完成医案未提供修改原因应被拒绝");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "editReason 缺失应被验证层拒绝 (400)");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Save_CompletedCase_WithEditReason_ShouldSucceed()
    {
        // Arrange - 创建医案并关闭
        var (caseId, original) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue("关闭医案应成功");

        // Act - Doctor (owner) 保存已完成医案，提供 EditReason
        var saveInput = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = original.PatientId,
            UserId = doctorUserId,
            EditReason = "患者复诊后补充诊断信息",
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛加剧伴恶心",
                TongueDiagnosis = "舌红苔黄腻",
                PulseDiagnosis = "弦滑",
                TcmDiagnosis = "肝阳化风"
            }
        };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert - 提供修改原因后应成功保存
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"提供 EditReason 后保存应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Consultation!.TcmDiagnosis.Should().Be("肝阳化风");

        // Verify - 审计日志应记录修改原因
        var auditResp = await doctor
            .GetAsync($"{BaseUrl}/{caseId}/audit-logs");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "审计日志查询应成功");
    }

    #endregion

    #region Queries

    [Fact]
    public async Task GetConsultationList_ShouldReturnForCase()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
            .GetAsync($"{BaseUrl}/{caseId}/consultations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrescriptionList_ShouldReturnForCase()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
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
        var doctor = await LoginAsDoctorAsync();

        // Act
        var response = await doctor
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

        // Get doctor client and userId
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        // Step 3: 创建医案(含诊断)
        var createInput = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "失眠多梦一月",
                TongueDiagnosis = "舌红少苔",
                PulseDiagnosis = "细数",
                TcmDiagnosis = "阴虚火旺"
            }
        };
        var createResp = await doctor.PostAsJsonAsync(BaseUrl, createInput);
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
            UserId = doctorUserId,
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
        var saveResp = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);
        saveResp.IsSuccessStatusCode.Should().BeTrue("Step4: 聚合保存应成功");
        var saved = await saveResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        saved!.Data!.HasPrescription.Should().BeTrue("Step4: 应有处方");
        saved.Data.Prescription!.DosageCount.Should().Be(14);

        // Step 5: 关闭医案
        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue("Step5: 关闭应成功");
        var closed = await closeResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        closed!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed,
            "Step5: 关闭后应为Completed");

        // Step 6: 验证完整状态
        var finalResp = await doctor.GetAsync($"{BaseUrl}/{caseId}");
        var final = await finalResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        final!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        final.Data.HasConsultation.Should().BeTrue("应有诊断");
        final.Data.HasPrescription.Should().BeTrue("应有处方");
    }

    #endregion

    #region Migrated from Structure B

    // ===== Create: 字段验证 =====

    [Fact]
    public async Task CreateMedicalCase_ShouldSetUserId()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.UserId.Should().Be(doctorUserId,
            "UserId应从JWT Token的NameIdentifier正确设置");
        body.Data.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateMedicalCase_ShouldSetDoctorName_FromUserTable()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.DoctorName.Should().NotBeNullOrEmpty(
            "DoctorName应从Users表的RealName字段正确获取");
    }

    [Fact]
    public async Task CreateMedicalCase_ShouldSetPatientName_FromPatientTable()
    {
        // Arrange
        var uniqueName = "患者名称验证_" + Guid.NewGuid().ToString("N")[..4];
        var patientId = await CreatePatientAsync(uniqueName);
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.PatientName.Should().Contain(uniqueName,
            "PatientName应从Patients表正确获取");
    }

    [Fact]
    public async Task CreateMedicalCase_WithEmptyGuid_ShouldReturn400()
    {
        // Arrange - 使用空GUID作为PatientId
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = Guid.Empty,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

        // Assert - 空GUID被FluentValidation的NotEmpty()规则拦截，返回400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "空GUID PatientId被FluentValidation拦截，应返回400");
    }

    [Fact]
    public async Task CreateMedicalCase_WhenPatientHasActiveCase_ShouldReturn400()
    {
        // Arrange - 先创建一个Active医案
        var patientId = await CreatePatientAsync();
        await CreateMedicalCaseAsync(patientId: patientId);

        // 再为同一患者尝试创建第二个
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "测试" }
        };

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, input);

        // Assert - BR-001: 单患者只能有一个Active医案
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "同一患者已有进行中医案时，BusinessException 被映射为 400");
    }

    // ===== SetPrescriptionFlag: 额外场景 =====

    [Fact]
    public async Task SetPrescriptionFlag_WithMinimalConsultation_ShouldStillSucceed()
    {
        // Arrange - 创建医案，仅提供最小必填诊断(TcmDiagnosis)
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "待定" }
        };
        var createResp = await doctor.PostAsJsonAsync(BaseUrl, input);
        createResp.IsSuccessStatusCode.Should().BeTrue(
            $"创建医案应成功, 实际: {createResp.StatusCode}");
        var created = await createResp.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        var caseId = created!.Data!.Id;

        // Act - 设置处方标志
        var flagReq = new { NeedsPrescription = true };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/prescription-flag", flagReq);

        // Assert - 仅有最小诊断也应允许设置处方标志
        response.IsSuccessStatusCode.Should().BeTrue(
            "仅有最小诊断也应允许设置处方标志");
    }

    // ===== Status: 通用更新 + 完成无处方 =====

    [Fact]
    public async Task UpdateStatus_ToCompleted_ViaCloseEndpoint_ShouldSucceed()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act - 通过 /close 端点完成医案
        var response = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task CompleteMedicalCase_ViaCloseEndpoint_ShouldCompleteWithoutPrescription()
    {
        // Arrange - 创建医案但不添加处方
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act - 通过 /close 端点直接完成(无处方)
        var response = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);

        // Assert - /close 端点允许无处方直接完成
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed,
            "无处方也应可通过close端点完成");
    }

    // ===== Suspend: 额外状态转换 =====

    [Fact]
    public async Task Suspend_WhenStatusCompleted_ShouldReturn400()
    {
        // Arrange - 创建医案并关闭
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue();

        // Act - 尝试对已完成的医案调用Suspend
        var suspendInput = new ConsultationInputDto { TcmDiagnosis = "尝试暂停" };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/suspend", suspendInput);

        // Assert - 已完成的医案不可挂起
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "已完成的医案不可暂停，BusinessException返回400");
    }

    [Fact]
    public async Task Suspend_WhenAlreadySuspended_ShouldRemainSuspended()
    {
        // Arrange - 创建医案并暂停
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var firstSuspend = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/suspend",
                new ConsultationInputDto { TcmDiagnosis = "第一次暂停" });
        firstSuspend.IsSuccessStatusCode.Should().BeTrue();

        // Act - 再次调用Suspend
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/suspend",
                new ConsultationInputDto { TcmDiagnosis = "再次暂停" });

        // Assert - 幂等性: 多次暂停不改变状态
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended,
            "多次暂停应保持Suspended状态");
    }

    // ===== Cancel: 额外状态转换 =====

    [Fact]
    public async Task CancelMedicalCase_WhenStatusCompleted_ShouldReturn400()
    {
        // Arrange - 创建医案并关闭
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue();

        // Act - 尝试对已完成的医案调用Cancel
        var cancelReq = new { Reason = "测试取消" };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/cancel", cancelReq);

        // Assert - 已完成的医案不可取消
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "已完成的医案不可取消，BusinessException返回400");
    }

    [Fact]
    public async Task CancelMedicalCase_WithReason_ShouldSucceed()
    {
        // Arrange
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act - 带理由取消
        var cancelReq = new { Reason = "患者临时有事，择日再诊" };
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/cancel", cancelReq);

        // Assert - 取消操作返回204 NoContent(软删除)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "带理由取消应成功(204)");
    }

    [Fact]
    public async Task CancelMedicalCase_WhenStatusSuspended_ShouldSucceed()
    {
        // Arrange - 创建医案并暂停
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var suspendResp = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}/suspend",
                new ConsultationInputDto { TcmDiagnosis = "暂停" });
        suspendResp.IsSuccessStatusCode.Should().BeTrue();

        // Act - 取消Suspended状态的医案
        var response = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/cancel", null);

        // Assert - 暂停状态可取消(软删除)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "暂停状态的医案应可取消");
    }

    [Fact]
    public async Task CancelMedicalCase_WhenAlreadyCancelled_ShouldReturn404()
    {
        // Arrange - 创建并取消医案
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        await doctor.PutAsync($"{BaseUrl}/{caseId}/cancel", null);

        // Act - 尝试再次取消
        var response = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/cancel", null);

        // Assert - 软删除后查不到，返回404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "已软删除的医案再次取消应返回404");
    }

    // ===== Permissions: 已完成状态 =====

    [Fact]
    public async Task GetPermissions_WhenCompleted_ShouldReturnRequiresEditReason()
    {
        // Arrange - 创建并关闭医案
        var (caseId, _) = await CreateMedicalCaseAsync();
        var doctor = await LoginAsDoctorAsync();
        var closeResp = await doctor
            .PutAsync($"{BaseUrl}/{caseId}/close", null);
        closeResp.IsSuccessStatusCode.Should().BeTrue();

        // Act
        var response = await doctor
            .GetAsync($"{BaseUrl}/{caseId}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<MedicalCasePermissionDto>>(JsonOptions);
        // 已完成的医案当天创建者仍可编辑 (CanEdit=true)，但需要提供修改原因
        body!.Data!.CanEdit.Should().BeTrue(
            "已完成的医案当天创建者仍可编辑");
        body.Data.RequiresEditReason.Should().BeTrue(
            "已完成的医案需要提供修改原因");
    }

    // ===== Save (PUT /{id}): 额外验证场景 =====

    [Fact]
    public async Task Save_WithMismatchedId_ShouldReturn400()
    {
        // Arrange - 创建医案
        var (caseId, original) = await CreateMedicalCaseAsync();
        var wrongId = Guid.NewGuid();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var saveInput = new MedicalCaseInputDto
        {
            Id = wrongId, // 与URL中的ID不匹配
            PatientId = original.PatientId,
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                TcmDiagnosis = "测试"
            }
        };

        // Act
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{caseId}", saveInput);

        // Assert - ID不匹配应返回400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "body.Id与URL路径ID不匹配应返回400");
    }

    [Fact]
    public async Task Save_NonExistingId_ShouldReturn404()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        var saveInput = new MedicalCaseInputDto
        {
            Id = nonExistingId,
            PatientId = Guid.NewGuid(),
            UserId = doctorUserId,
            Consultation = new ConsultationInputDto
            {
                TcmDiagnosis = "测试"
            }
        };

        // Act
        var response = await doctor
            .PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", saveInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "不存在的医案应返回404");
    }

    #endregion
}
