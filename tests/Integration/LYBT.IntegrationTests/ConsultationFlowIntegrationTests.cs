using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.IntegrationTests
{
    /// <summary>
    /// 看诊流程集成测试
    /// </summary>
    public class ConsultationFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConsultationFlowIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region 测试场景1: 新患者完整看诊流程

        [Fact]
        public async Task CompleteConsultationFlow_NewPatient_ShouldSucceed()
        {
            // Step 1: 登录获取令牌
            var token = await LoginAsTestDoctorAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Step 2: 创建新患者
            var patient = await CreateTestPatientAsync();
            patient.Should().NotBeNull();
            patient.Id.Should().NotBeEmpty();

            // Step 3: 为患者创建医疗案例
            var medicalCase = await CreateMedicalCaseAsync(patient.Id);
            medicalCase.Should().NotBeNull();
            medicalCase.Id.Should().NotBeEmpty();
            medicalCase.Status.Should().Be("Active");

            // Step 4: 开始看诊
            var consultation = await StartConsultationAsync(medicalCase.Id, patient.Id);
            consultation.Should().NotBeNull();
            consultation.Id.Should().NotBeEmpty();

            // Step 5: 更新看诊信息（四诊）
            await UpdateConsultationWithTCMDiagnosisAsync(consultation.Id);

            // Step 6: 开具处方
            var prescription = await CreatePrescriptionAsync(consultation.Id, patient.Id);
            prescription.Should().NotBeNull();
            prescription.Id.Should().NotBeEmpty();

            // Step 7: 完成看诊
            var completed = await CompleteConsultationAsync(consultation.Id);
            completed.Should().BeTrue();

            // Step 8: 验证最终状态
            var finalConsultation = await GetConsultationByIdAsync(consultation.Id);
            finalConsultation.Should().NotBeNull();
            // 验证状态已完成（假设 2 表示已完成）
            finalConsultation.Status.Should().Be(2);
        }

        #endregion

        #region 测试场景2: 老患者复诊流程

        [Fact]
        public async Task ConsultationFlow_ExistingPatient_ShouldAccessHistory()
        {
            // 准备：先创建一个有历史记录的患者
            var token = await LoginAsTestDoctorAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 创建患者并完成一次看诊
            var patient = await CreateTestPatientAsync();
            var firstCase = await CreateMedicalCaseAsync(patient.Id);
            var firstConsultation = await StartConsultationAsync(firstCase.Id, patient.Id);
            await CompleteConsultationAsync(firstConsultation.Id);

            // 测试：复诊流程
            // Step 1: 获取患者历史看诊记录
            var history = await GetPatientConsultationHistoryAsync(patient.Id);
            history.Should().NotBeNull();
            history.Should().HaveCountGreaterThan(0);

            // Step 2: 创建新的医疗案例
            var newCase = await CreateMedicalCaseAsync(patient.Id);
            newCase.Id.Should().NotBe(firstCase.Id);

            // Step 3: 开始新的看诊
            var newConsultation = await StartConsultationAsync(newCase.Id, patient.Id);
            newConsultation.Id.Should().NotBe(firstConsultation.Id);

            // 验证：新旧记录独立
            var oldRecord = await GetConsultationByIdAsync(firstConsultation.Id);
            var newRecord = await GetConsultationByIdAsync(newConsultation.Id);
            
            oldRecord.Id.Should().NotBe(newRecord.Id);
            oldRecord.MedicalCaseId.Should().NotBe(newRecord.MedicalCaseId);
        }

        #endregion

        #region 测试场景3: 看诊状态流转测试

        [Fact]
        public async Task ConsultationStatusFlow_ShouldTransitionCorrectly()
        {
            var token = await LoginAsTestDoctorAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var patient = await CreateTestPatientAsync();
            var medicalCase = await CreateMedicalCaseAsync(patient.Id);

            // 创建看诊（初始状态应该是待看诊或看诊中）
            var consultation = await StartConsultationAsync(medicalCase.Id, patient.Id);
            var initialStatus = consultation.Status;

            // 更新状态为看诊中（如果不是的话）
            if (initialStatus != 1) // 假设 1 是看诊中
            {
                await UpdateConsultationStatusAsync(consultation.Id, 1);
            }

            // 验证状态更新
            var inProgress = await GetConsultationByIdAsync(consultation.Id);
            inProgress.Status.Should().Be(1);

            // 完成看诊
            await CompleteConsultationAsync(consultation.Id);
            
            var completed = await GetConsultationByIdAsync(consultation.Id);
            completed.Status.Should().Be(2); // 假设 2 是已完成
        }

        #endregion

        #region 测试场景4: 异常流程测试

        [Fact]
        public async Task ConsultationFlow_InvalidOperations_ShouldFail()
        {
            var token = await LoginAsTestDoctorAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 测试1: 使用不存在的患者ID
            var invalidPatientId = Guid.NewGuid();
            var createCaseResponse = await CreateMedicalCaseRawAsync(invalidPatientId);
            createCaseResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 测试2: 重复开始看诊（如果业务不允许）
            var patient = await CreateTestPatientAsync();
            var medicalCase = await CreateMedicalCaseAsync(patient.Id);
            var consultation = await StartConsultationAsync(medicalCase.Id, patient.Id);
            
            // 尝试再次为同一案例开始看诊
            var duplicateResponse = await StartConsultationRawAsync(medicalCase.Id, patient.Id);
            // 根据业务逻辑，这可能返回 BadRequest 或者创建新的看诊记录
            
            // 测试3: 无效的状态转换
            // 尝试将已完成的看诊重新开始
            await CompleteConsultationAsync(consultation.Id);
            var invalidStatusUpdate = await UpdateConsultationStatusAsync(consultation.Id, 0); // 0 = 待看诊
            // 应该失败或被忽略
        }

        #endregion

        #region 辅助方法

        private async Task<string> LoginAsTestDoctorAsync()
        {
            var loginDto = new LoginRequestDto
            {
                Username = "sysadmin",
                Password = "Admin@123456",
                RememberMe = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/auth/login", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, _jsonOptions);
            
            return loginResponse?.Token ?? throw new Exception("登录失败");
        }

        private async Task<PatientDto> CreateTestPatientAsync()
        {
            var createDto = new PatientCreateDto
            {
                Name = $"测试患者_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                Gender = Gender.Male,
                Age = 35,
                PhoneNumber = "13800138000",
                IdCard = $"1101011988010{new Random().Next(10000, 99999)}",
                Address = "北京市测试地址"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/patients", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PatientDto>(responseContent, _jsonOptions)!;
        }

        private async Task<MedicalCaseDto> CreateMedicalCaseAsync(Guid patientId)
        {
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                ChiefComplaint = "测试主诉：疲劳乏力",
                CaseType = "初诊"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/medicalcase", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MedicalCaseDto>(responseContent, _jsonOptions)!;
        }

        private async Task<HttpResponseMessage> CreateMedicalCaseRawAsync(Guid patientId)
        {
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                ChiefComplaint = "测试主诉",
                CaseType = "初诊"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto),
                Encoding.UTF8,
                "application/json");

            return await _client.PostAsync("/api/v1/medicalcase", content);
        }

        private async Task<ConsultationDetailDto> StartConsultationAsync(Guid medicalCaseId, Guid patientId)
        {
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = patientId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(startDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/consultation/start", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ConsultationDetailDto>(responseContent, _jsonOptions)!;
        }

        private async Task<HttpResponseMessage> StartConsultationRawAsync(Guid medicalCaseId, Guid patientId)
        {
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = patientId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(startDto),
                Encoding.UTF8,
                "application/json");

            return await _client.PostAsync("/api/v1/consultation/start", content);
        }

        private async Task UpdateConsultationWithTCMDiagnosisAsync(Guid consultationId)
        {
            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色偏黄，精神尚可",
                AuscultationOlfaction = "语音低微，口气正常",
                Inquiry = "主诉：疲劳乏力3月余。现病史：近3月来感觉疲劳，活动后加重",
                Palpation = "脉象：脉细弱",
                TongueInspection = "舌质淡，苔薄白",
                PulseCondition = "脉细弱",
                TCMDiagnosis = "气虚证",
                Diagnosis = "慢性疲劳综合征"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PutAsync($"/api/v1/consultation/{consultationId}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<bool> UpdateConsultationStatusAsync(Guid consultationId, int status)
        {
            var updateDto = new UpdateStatusDto
            {
                Status = status,
                Reason = "测试状态更新"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync($"/api/v1/consultation/{consultationId}/update-status", content);
            return response.IsSuccessStatusCode;
        }

        private async Task<PrescriptionDto> CreatePrescriptionAsync(Guid consultationId, Guid patientId)
        {
            var createDto = new PrescriptionCreateDto
            {
                ConsultationId = consultationId,
                PatientId = patientId,
                Type = "中药处方",
                Usage = "每日一剂，水煎服，分两次温服",
                Days = 7,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto
                    {
                        HerbName = "黄芪",
                        Dosage = 30,
                        Unit = "g"
                    },
                    new PrescriptionItemDto
                    {
                        HerbName = "当归",
                        Dosage = 10,
                        Unit = "g"
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/prescriptions", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PrescriptionDto>(responseContent, _jsonOptions)!;
        }

        private async Task<bool> CompleteConsultationAsync(Guid consultationId)
        {
            var completeDto = new ConsultationCompleteDto
            {
                Summary = "患者气虚证明显，予补气方药治疗",
                FollowUpAdvice = "一周后复诊"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(completeDto),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync($"/api/v1/consultation/{consultationId}/complete", content);
            return response.IsSuccessStatusCode;
        }

        private async Task<ConsultationDetailDto> GetConsultationByIdAsync(Guid consultationId)
        {
            var response = await _client.GetAsync($"/api/v1/consultation/{consultationId}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ConsultationDetailDto>(responseContent, _jsonOptions)!;
        }

        private async Task<List<ConsultationDto>> GetPatientConsultationHistoryAsync(Guid patientId)
        {
            var response = await _client.GetAsync($"/api/v1/consultation/patient/{patientId}/history");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ConsultationDto>>(responseContent, _jsonOptions)!;
        }

        #endregion
    }
}