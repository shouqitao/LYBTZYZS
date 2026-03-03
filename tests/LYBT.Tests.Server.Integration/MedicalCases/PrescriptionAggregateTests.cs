using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Integration.MedicalCases;

/// <summary>
/// 处方聚合保存集成测试 -- 基于实际聚合端点重设计。
/// 替代: Issue2250_PrescriptionSaveTests (旧文件使用已删除的独立处方端点)
///
/// 实际 API:
///   PUT  /api/v1/medicalcases/{id}                  聚合保存 (Consultation + Prescription)
///   PUT  /api/v1/medicalcases/{id}/prescription-flag 设置处方标志
///   GET  /api/v1/medicalcases/{id}                  查询验证持久化
///
/// 关键: 处方通过 MedicalCaseInputDto.Prescription 嵌套保存，不存在独立处方端点。
/// </summary>
[Collection("ServerIntegration")]
public class PrescriptionAggregateTests
{
    private readonly WebApiFixture _fixture;
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/medicalcases";
    private const string PatientUrl = "/api/v1/patients";
    private const string HerbUrl = "/api/v1/herbs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PrescriptionAggregateTests(WebApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Helpers

    private static int _idSeq;
    private static string UniqueIdNumber()
    {
        var seq = Interlocked.Increment(ref _idSeq);
        return $"44010019950501{seq:D3}X";
    }

    private async Task<Guid> CreatePatientAsync()
    {
        var input = new PatientInputDto
        {
            Name = "处方测试患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Female,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = $"137{Random.Shared.Next(10000000, 99999999)}",
            Address = "处方测试地址"
        };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(PatientUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建患者失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    private async Task<(Guid herbId, string herbName)> CreateHerbAsync(decimal price = 0.5m)
    {
        var herbName = "处方药材_" + Guid.NewGuid().ToString("N")[..4];
        var input = new { Name = herbName, Unit = "克", Price = price };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(HerbUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建药材失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<LYBT.Shared.Models.Contracts.Herbs.HerbDetailDto>>(JsonOptions);
        return (body!.Data!.Id, herbName);
    }

    /// <summary>DoctorClient 创建医案并返回详情</summary>
    private async Task<MedicalCaseDetailDto> CreateMedicalCaseAsync(Guid patientId)
    {
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId, // FluentValidation 要求 UserId 不为空
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "处方测试",
                TcmDiagnosis = "脾虚湿困"
            }
        };
        var resp = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建医案失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    /// <summary>设置处方标志为 true</summary>
    private async Task SetPrescriptionFlagAsync(Guid medicalCaseId)
    {
        var flagReq = new { NeedsPrescription = true };
        var resp = await _fixture.DoctorClient.PutAsJsonAsync(
            $"{BaseUrl}/{medicalCaseId}/prescription-flag", flagReq);
        resp.IsSuccessStatusCode.Should().BeTrue($"设置处方标志失败: {resp.StatusCode}");
    }

    /// <summary>获取医案详情</summary>
    private async Task<MedicalCaseDetailDto> GetMedicalCaseAsync(Guid id)
    {
        var resp = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/{id}");
        resp.IsSuccessStatusCode.Should().BeTrue($"获取医案失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    #endregion

    #region 聚合保存处方

    [Fact]
    public async Task AggregateSave_WithPrescription_ShouldPersist()
    {
        // Arrange -- 创建患者 → 创建医案 → 设置处方标志 → 准备药材
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseAsync(patientId);
        await SetPrescriptionFlagAsync(medicalCase.Id);
        var (herbId, herbName) = await CreateHerbAsync();
        _output.WriteLine($"医案: {medicalCase.Id}, 药材: {herbId}");

        // Act -- 聚合保存: Consultation + Prescription
        var saveInput = new MedicalCaseInputDto
        {
            Id = medicalCase.Id,
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId, // FluentValidation 要求 UserId 不为空
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛发热",
                TcmDiagnosis = "风热感冒"
            },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id, // 嵌套处方也需要设置 MedicalCaseId
                DosageCount = 7,
                Advice = "水煎服",
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = herbId,
                        HerbName = herbName,
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
                    }
                }
            }
        };

        var resp = await _fixture.DoctorClient.PutAsJsonAsync($"{BaseUrl}/{medicalCase.Id}", saveInput);
        var content = await resp.Content.ReadAsStringAsync();
        _output.WriteLine($"聚合保存响应: {resp.StatusCode}");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();

        // 验证持久化: 重新读取
        var persisted = await GetMedicalCaseAsync(medicalCase.Id);
        persisted.Consultation.Should().NotBeNull();
        persisted.Consultation!.TcmDiagnosis.Should().Be("风热感冒");
        persisted.Prescription.Should().NotBeNull();
        persisted.Prescription!.DosageCount.Should().Be(7);
        persisted.Prescription.Items.Should().HaveCount(1);

        _output.WriteLine("聚合保存并持久化验证通过");
    }

    [Fact]
    public async Task AggregateSave_ConsecutiveUpdates_ShouldNotCauseConcurrencyException()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseAsync(patientId);
        await SetPrescriptionFlagAsync(medicalCase.Id);
        var (herbId, herbName) = await CreateHerbAsync();

        // Act -- 第一次保存
        var save1 = new MedicalCaseInputDto
        {
            Id = medicalCase.Id,
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto { PresentIllness = "初诊", TcmDiagnosis = "风寒" },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = herbId, HerbName = herbName, Dosage = 10, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
                }
            }
        };

        var resp1 = await _fixture.DoctorClient.PutAsJsonAsync($"{BaseUrl}/{medicalCase.Id}", save1);
        resp1.StatusCode.Should().Be(HttpStatusCode.OK, "第一次保存应成功");
        _output.WriteLine("第一次保存成功");

        // Act -- 第二次保存 (连续更新)
        var save2 = new MedicalCaseInputDto
        {
            Id = medicalCase.Id,
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto { PresentIllness = "复诊", TcmDiagnosis = "风热" },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 14,
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = herbId, HerbName = herbName, Dosage = 15, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
                }
            }
        };

        var resp2 = await _fixture.DoctorClient.PutAsJsonAsync($"{BaseUrl}/{medicalCase.Id}", save2);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK, "连续更新不应触发并发异常");
        _output.WriteLine("连续更新成功，无并发异常");

        // Assert -- 验证最终状态
        var final = await GetMedicalCaseAsync(medicalCase.Id);
        final.Consultation!.TcmDiagnosis.Should().Be("风热");
        final.Prescription!.DosageCount.Should().Be(14);
    }

    [Fact]
    public async Task AggregateSave_WithoutPrescription_ShouldOnlySaveConsultation()
    {
        // Arrange -- 不设置处方标志
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseAsync(patientId);

        // Act -- 仅保存 Consultation，不含 Prescription
        var saveInput = new MedicalCaseInputDto
        {
            Id = medicalCase.Id,
            PatientId = patientId,
            UserId = WebApiFixture.DoctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "仅诊断",
                TcmDiagnosis = "气虚"
            }
        };

        var resp = await _fixture.DoctorClient.PutAsJsonAsync($"{BaseUrl}/{medicalCase.Id}", saveInput);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var persisted = await GetMedicalCaseAsync(medicalCase.Id);
        persisted.Consultation.Should().NotBeNull();
        persisted.Consultation!.TcmDiagnosis.Should().Be("气虚");
        // Prescription 可能为 null 或空 (未设置处方标志)

        _output.WriteLine("仅诊断保存验证通过");
    }

    #endregion
}
