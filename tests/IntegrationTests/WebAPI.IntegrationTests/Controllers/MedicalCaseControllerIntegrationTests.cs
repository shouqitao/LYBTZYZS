using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Module.MedicalCase.Dtos; // SetPrescriptionFlagRequest (模块专用)
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// MedicalCaseController集成测试 - Epic #1612
    /// 测试14个API端点的完整流程
    /// 业务规则验证：BR-001、BF-002、AR-003
    /// </summary>
    public class MedicalCaseControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testPatientId;

        public MedicalCaseControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        /// <summary>
        /// 重写种子数据方法，创建测试患者
        /// </summary>
        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试患者
            var testPatient = new LYBT.Entities.Patients.Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                PhoneNumber = "13800138000",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
            context.SaveChanges();

            _testPatientId = testPatient.Id;
            // ⚠️ 注意：此时_output还未初始化（seed方法在base构造函数中调用）
            // 测试患者ID已保存到_testPatientId字段供后续测试使用
        }

        #region Write Layer Tests - CreateMedicalCase

        [Fact]
        public async Task CreateMedicalCase_WithValidRequest_ShouldCreateSuccessfully()
        {
            // Arrange
            _output.WriteLine($"🔍 使用测试患者ID: {_testPatientId}");

            var request = new
            {
                PatientId = _testPatientId,
                VisitDate = DateTime.Now
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // ⚠️ 临时调试代码
            _output.WriteLine($"📡 HTTP状态码: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"📄 响应内容: {responseContent}");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.PatientId.Should().Be(_testPatientId);
            apiResponse.Data.CaseStatus.Should().Be(MedicalCaseStatus.Active);
            apiResponse.Data.Consultation.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateMedicalCase_WhenPatientHasActiveCase_ShouldReturn422()
        {
            // Arrange - 先创建一个病案
            var patientId = Guid.NewGuid();
            var firstRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };
            await Client.PostAsJsonAsync("/api/v1/medicalcases", firstRequest);

            var secondRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };

            // Act - 尝试为同一患者再创建病案
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", secondRequest);

            // Assert - BR-001: 单患者只能有一个Active病案
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);

            var apiResponse = await response.ShouldBeFailedApiResponseWithMessageAsync();
            apiResponse.Message.Should().Contain("未完成病案");
        }

        #endregion

        #region Write Layer Tests - UpdateConsultation

        [Fact]
        public async Task UpdateConsultation_WithValidRequest_ShouldUpdateSuccessfully()
        {
            // Arrange - 创建病案
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new ConsultationInputDto
            {
                ChiefComplaint = "头痛",
                TCMDiagnosis = "风寒感冒"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Consultation!.Step1CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateConsultation_WhenStatusNotActive_ShouldReturn400()
        {
            // Arrange - 创建并完成病案
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();

            var request = new ConsultationInputDto
            {
                ChiefComplaint = "测试"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                request);

            // Assert
            response.ShouldBeBadRequest();
        }

        #endregion

        #region Write Layer Tests - SetPrescriptionFlag

        [Fact]
        public async Task SetPrescriptionFlag_WithValidRequest_ShouldUpdateSuccessfully()
        {
            // Arrange - 创建病案并完成辨证
            var medicalCase = await CreateTestMedicalCaseWithConsultationAsync();

            var request = new { NeedsPrescription = true };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.NeedsPrescription.Should().BeTrue();
        }

        [Fact]
        public async Task SetPrescriptionFlag_WhenStep1NotCompleted_ShouldReturn422()
        {
            // Arrange - 创建病案但未辨证
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new { NeedsPrescription = true };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                request);

            // Assert - BF-002: 三步流程验证
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Write Layer Tests - CreatePrescription

        [Fact]
        public async Task CreatePrescription_WithValidRequest_ShouldCreateSuccessfully()
        {
            // Arrange - 创建病案、辨证、标记需要处方
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            var request = new PrescriptionCreateDto
            {
                PatientId = medicalCase.PatientId,
                DoctorId = medicalCase.DoctorId,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 10
                    }
                }
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.MedicalCaseId.Should().Be(medicalCase.Id);
        }

        [Fact]
        public async Task CreatePrescription_WhenPrescriptionAlreadyExists_ShouldReturn422()
        {
            // Arrange - 创建病案并已有处方
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            var request = new PrescriptionCreateDto
            {
                PatientId = medicalCase.PatientId,
                DoctorId = medicalCase.DoctorId,
                Items = new List<PrescriptionItemInputDto>()
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                request);

            // Assert - AR-003: 一诊一方约束（返回400 Bad Request）
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.BadRequest);
        }

        #endregion

        #region Write Layer Tests - UpdatePrescription

        [Fact]
        public async Task UpdatePrescription_WithValidRequest_ShouldUpdateSuccessfully()
        {
            // Arrange - 创建病案和处方
            var (medicalCase, prescription) = await CreateTestMedicalCaseWithPrescriptionAsync();

            var request = new PrescriptionEditDto
            {
                Id = prescription.Id,
                PatientId = medicalCase.PatientId,
                UserId = medicalCase.DoctorId,
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = Guid.NewGuid(), Quantity = 6m }
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions/{prescription.Id}",
                request);

            // Assert
            response.ShouldBeOk();
        }

        #endregion

        #region Write Layer Tests - DeletePrescription

        [Fact]
        public async Task DeletePrescription_WithValidRequest_ShouldDeleteSuccessfully()
        {
            // Arrange - 创建病案和处方
            var (medicalCase, prescription) = await CreateTestMedicalCaseWithPrescriptionAsync();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions/{prescription.Id}");

            // Assert
            response.ShouldBeNoContent();
        }

        #endregion

        #region Write Layer Tests - UpdateStatus

        [Fact]
        public async Task UpdateStatus_WithValidRequest_ShouldUpdateSuccessfully()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Issue #2242: Cancelled状态已废弃，改用Completed测试状态更新
            var request = new { CaseStatus = MedicalCaseStatus.Completed };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/status",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        #endregion

        #region Write Layer Tests - CompleteMedicalCase

        [Fact]
        public async Task CompleteMedicalCase_WithValidRequest_ShouldCompleteSuccessfully()
        {
            // Arrange - 创建完整流程的病案
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            // Act
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/complete",
                null);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CompleteMedicalCase_WhenPrescriptionNotCompleted_ShouldReturn422()
        {
            // Arrange - 创建病案但未完成处方
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Act
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/complete",
                null);

            // Assert - BF-002: 三步流程验证
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Read Layer Tests - GetById

        [Fact]
        public async Task GetById_WithExistingId_ShouldReturnMedicalCase()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Act
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            apiResponse.Data!.Id.Should().Be(medicalCase.Id);
        }

        [Fact]
        public async Task GetById_WithNonExistingId_ShouldReturn404()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/v1/medicalcases/{nonExistingId}");

            // Assert
            response.ShouldBeNotFound();
        }

        #endregion

        #region Read Layer Tests - GetList

        [Fact]
        public async Task GetList_WithValidParameters_ShouldReturnPagedResults()
        {
            // Arrange - 创建测试数据
            await CreateTestMedicalCaseAsync();
            await CreateTestMedicalCaseAsync();

            // Act
            var response = await Client.GetAsync("/api/v1/medicalcases?page=1&pageSize=10");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<MedicalCaseEntity>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeEmpty();
        }

        #endregion

        #region Helper Layer Tests - CanEdit

        [Fact]
        public async Task CanEdit_WhenStatusActive_ShouldReturnTrue()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Act
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/can-edit");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<LYBT.Module.MedicalCase.Interfaces.CanEditResponse>();
            apiResponse.Data!.CanEdit.Should().BeTrue();
        }

        [Fact]
        public async Task CanEdit_WhenStatusCompleted_ShouldReturnFalse()
        {
            // Arrange
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();

            // Act
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/can-edit");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<LYBT.Module.MedicalCase.Interfaces.CanEditResponse>();
            apiResponse.Data!.CanEdit.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建测试病案（最基础）
        /// ⚠️ Issue #1669 Phase 6: 每次调用创建独立患者，避免"患者已有未完成病案"错误
        /// </summary>
        private async Task<MedicalCaseEntity> CreateTestMedicalCaseAsync()
        {
            // 为本次测试创建独立的患者（避免多个测试共享患者导致冲突）
            var newPatientId = Guid.NewGuid();
            var testUserId = Guid.NewGuid(); // ⚠️ 模拟审计字段的用户ID

            // ⚠️ 在数据库中创建患者实体（必须设置审计字段）
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var testPatient = new LYBT.Entities.Patients.Patient
                {
                    Id = newPatientId,
                    Name = $"测试患者{newPatientId.ToString().Substring(0, 8)}",
                    Gender = LYBT.Shared.Models.Enums.Gender.Male,
                    PhoneNumber = "13800138000",
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = testUserId,  // ⚠️ Issue #1669: 必须设置CreatedBy
                    UpdatedBy = testUserId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
                var saveResult = await db.SaveChangesAsync();
                _output.WriteLine($"✅ 患者实体已创建: PatientId={newPatientId}, SavedEntities={saveResult}");
            }

            var request = new
            {
                PatientId = newPatientId,
                VisitDate = DateTime.Now
            };

            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // ⚠️ 临时调试代码：打印错误的详细信息
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"❌ 创建病案失败 - 状态码: {response.StatusCode}");
                _output.WriteLine($"❌ 错误响应: {errorContent}");
                _output.WriteLine($"❌ 使用的PatientId: {newPatientId}");
            }

            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案并完成辨证（Step 1完成）
        /// </summary>
        private async Task<MedicalCaseEntity> CreateTestMedicalCaseWithConsultationAsync()
        {
            var medicalCase = await CreateTestMedicalCaseAsync();

            var consultationRequest = new ConsultationInputDto
            {
                ChiefComplaint = "头痛",
                TCMDiagnosis = "风寒感冒"
            };

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                consultationRequest);

            // ⚠️ Issue #1669: 验证更新请求是否成功
            updateResponse.ShouldBeOk();
            await updateResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();

            // 重新获取更新后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案、辨证、标记需要处方（Ready for Prescription）
        /// </summary>
        private async Task<MedicalCaseEntity> CreateTestMedicalCaseReadyForPrescriptionAsync()
        {
            var medicalCase = await CreateTestMedicalCaseWithConsultationAsync();

            var flagRequest = new { NeedsPrescription = true };
            var flagResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                flagRequest);

            // ⚠️ Issue #1669: 验证标记请求是否成功
            flagResponse.ShouldBeOk();
            await flagResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();

            // 重新获取更新后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案并包含处方（完整流程）
        /// </summary>
        private async Task<(MedicalCaseEntity, PrescriptionEntity)> CreateTestMedicalCaseWithPrescriptionAsync()
        {
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            var prescriptionRequest = new PrescriptionCreateDto
            {
                PatientId = medicalCase.PatientId,
                DoctorId = medicalCase.DoctorId,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 10
                    }
                }
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                prescriptionRequest);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            var prescription = apiResponse.Data!;

            // 重新获取病案（包含处方）
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var updatedCase = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();

            return (updatedCase.Data!, prescription);
        }

        /// <summary>
        /// 创建并完成病案（完整流程 + 完成）
        /// </summary>
        private async Task<MedicalCaseEntity> CreateAndCompleteMedicalCaseAsync()
        {
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            var completeResponse = await Client.PutAsync($"/api/v1/medicalcases/{medicalCase.Id}/complete", null);

            // ⚠️ Issue #1669: 验证完成请求是否成功
            completeResponse.ShouldBeOk();
            await completeResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();

            // 重新获取完成后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseEntity>();
            return apiResponse.Data!;
        }

        #endregion
    }
}
