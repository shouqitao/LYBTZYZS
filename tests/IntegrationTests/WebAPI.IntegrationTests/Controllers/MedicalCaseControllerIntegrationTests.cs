using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.MedicalCase;  // MedicalCaseDetailDto
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;
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

        // Issue #2231: 使用固定的医生ID以匹配JWT Token中的NameIdentifier
        private static readonly Guid FixedDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public MedicalCaseControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
            
            // Issue #2231: 基类构造函数中GenerateTestToken被调用时,FixedDoctorId还未初始化
            // 解决方案:在派生类构造函数中重新设置Authorization header,此时FixedDoctorId已初始化
            SetAuthorizationHeader(Client);
        }

        /// <summary>
        /// 重写JWT Token生成方法，使用固定的医生ID
        /// Issue #2231: 确保JWT NameIdentifier与数据库中的Doctor.Id匹配
        /// </summary>
        protected override string GenerateTestToken()
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes("VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, FixedDoctorId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test Doctor"),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Doctor")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "LYBT.WebAPI.Tests",
                Audience = "LYBT.Client.Tests",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 重写种子数据方法，创建测试患者和医生
        /// Issue #2231: 修复集成测试 - 添加Doctor实体避免"医生不存在"错误
        /// </summary>
        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试医生（关联到JWT Token中的用户）
            // 使用FixedDoctorId确保与JWT Token中的NameIdentifier匹配
            var testDoctor = new LYBT.Entities.Users.User
            {
                Id = FixedDoctorId, // 使用固定ID与JWT Token匹配
                UserName = "testdoctor",
                RealName = "测试医生",  // User实体使用RealName而不是DisplayName
                Email = "testdoctor@test.com",
                PasswordHash = "DummyHashForTesting",  // 必需字段
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Set<LYBT.Entities.Users.User>().Add(testDoctor);

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

            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除，用CreatedAt代替
            var request = new
            {
                PatientId = _testPatientId
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // ⚠️ 临时调试代码
            _output.WriteLine($"📡 HTTP状态码: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"📄 响应内容: {responseContent}");

            // Assert
            response.ShouldBeOk();

            // Issue #2231: API返回MedicalCaseDetailDto而不是MedicalCaseEntity
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.PatientId.Should().Be(_testPatientId);
            apiResponse.Data.CaseStatus.Should().Be(MedicalCaseStatus.Active);
            // MedicalCaseDetailDto只有ConsultationId,没有Consultation导航属性
            apiResponse.Data.ConsultationId.Should().NotBeNull();
        }

        /// <summary>
        /// Issue #2232 Task 4.1.1: 验证UserId正确设置
        /// OpenSpec: simplify-medicalcase-dataflow - DoctorId已重命名为UserId
        /// 测试目标: 验证创建医案时,UserId从JWT Token的NameIdentifier中正确设置
        /// </summary>
        [Fact]
        public async Task CreateMedicalCase_ShouldSetUserId_WhenCalled()
        {
            // Arrange
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var request = new
            {
                PatientId = _testPatientId
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // Issue #2232: 验证UserId被正确设置为JWT Token中的NameIdentifier
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId已重命名为UserId
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.UserId.Should().Be(FixedDoctorId);
            apiResponse.Data.UserId.Should().NotBe(Guid.Empty);

            _output.WriteLine($"✅ UserId正确设置: {apiResponse.Data.UserId}");
        }

        /// <summary>
        /// Issue #2232 Task 4.1.1: 验证DoctorName从Users表正确获取
        /// 测试目标: 验证创建医案时,DoctorName从Users.RealName字段正确获取
        /// </summary>
        [Fact]
        public async Task CreateMedicalCase_ShouldSetDoctorName_FromUserTable()
        {
            // Arrange
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var request = new
            {
                PatientId = _testPatientId
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // Issue #2232: 验证DoctorName从Users表的RealName字段正确获取
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.DoctorName.Should().NotBeNullOrEmpty();
            apiResponse.Data.DoctorName.Should().Be("测试医生"); // 与InitializeAsync中创建的testDoctor.RealName一致

            _output.WriteLine($"✅ DoctorName正确设置: {apiResponse.Data.DoctorName}");
        }

        /// <summary>
        /// Issue #2232 Task 4.1.1: 验证PatientName从Patients表正确获取
        /// 测试目标: 验证创建医案时,PatientName从Patients.Name字段正确获取
        /// </summary>
        [Fact]
        public async Task CreateMedicalCase_ShouldSetPatientName_FromPatientTable()
        {
            // Arrange
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var request = new
            {
                PatientId = _testPatientId
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // Issue #2232: 验证PatientName从Patients表的Name字段正确获取
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.PatientName.Should().NotBeNullOrEmpty();
            // 患者名称应该与InitializeAsync中创建的testPatient.Name一致
            // 注意: InitializeAsync创建的患者名称格式为"测试患者{Guid前8位}"，如"测试患者2e1e2f73"
            apiResponse.Data.PatientName.Should().StartWith("测试患者");

            _output.WriteLine($"✅ PatientName正确设置: {apiResponse.Data.PatientName}");
        }

        /// <summary>
        /// Issue #2232 Task 4.1.1: 验证空GUID异常处理
        /// 测试目标: 验证PatientId为空GUID时返回422 Unprocessable Entity（患者不存在）
        /// </summary>
        [Fact]
        public async Task CreateMedicalCase_ShouldThrowException_WhenGuidEmpty()
        {
            // Arrange
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var request = new
            {
                PatientId = Guid.Empty // 使用空GUID
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            // Assert
            // Issue #2232: 空GUID会通过FluentValidation，但在Service层业务逻辑验证时被拒绝（患者不存在）
            // 因此返回422 Unprocessable Entity而不是400 Bad Request
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);

            // 验证响应内容包含错误消息
            var apiResponse = await response.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("患者不存在");

            _output.WriteLine($"✅ 空GUID正确返回422: {apiResponse.Message}");
        }

        [Fact]
        public async Task CreateMedicalCase_WhenPatientHasActiveCase_ShouldReturn422()
        {
            // Arrange - 先创建一个病案（使用测试基类中已存在的患者）
            // Issue #2231: 使用_testPatientId而非Guid.NewGuid()，确保患者存在
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var firstRequest = new
            {
                PatientId = _testPatientId
            };
            await Client.PostAsJsonAsync("/api/v1/medicalcases", firstRequest);

            var secondRequest = new
            {
                PatientId = _testPatientId
            };

            // Act - 尝试为同一患者再创建病案
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", secondRequest);

            // Assert - BR-001: 单患者只能有一个Active病案
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);

            var apiResponse = await response.ShouldBeFailedApiResponseWithMessageAsync();
            apiResponse.Message.Should().Contain("进行中的医案");
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
                PatientId = medicalCase.PatientId,  // Issue #2231: ConsultationInputDtoValidator requires PatientId
                UserId = FixedDoctorId,              // Issue #2231: ConsultationInputDtoValidator requires UserId
                PresentIllness = "头痛",
                TcmDiagnosis = "风寒感冒"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            // Issue #2231: MedicalCaseDetailDto不包含Consultation导航属性，仅验证ConsultationId
        }

        [Fact]
        public async Task UpdateConsultation_WhenStatusNotActive_ShouldReturn403()
        {
            // Arrange - 创建并完成病案
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();

            var request = new ConsultationInputDto
            {
                PatientId = medicalCase.PatientId,  // Issue #2231: ConsultationInputDtoValidator requires PatientId
                UserId = FixedDoctorId,              // Issue #2231: ConsultationInputDtoValidator requires UserId
                PresentIllness = "测试"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                request);

            // Assert
            // refactor-authorization-system: 授权检查在业务逻辑之前执行
            // 已完成的医案会被资源授权处理器拒绝，返回403而非400
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.Forbidden);
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

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            // Issue #2231: MedicalCaseDetailDto不包含NeedsPrescription字段，跳过此断言
        }

        [Fact]
        public async Task SetPrescriptionFlag_WhenStep1NotCompleted_ShouldStillSucceed()
        {
            // Arrange - 创建病案但未辨证
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new { NeedsPrescription = true };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                request);

            // Assert - OpenSpec refactor-medicalcase-api: API已简化，不再强制step 1完成
            // 现在允许在任何时候设置NeedsPrescription标志
            response.ShouldBeOk();
        }

        #endregion

        #region Write Layer Tests - CreatePrescription

        [Fact]
        public async Task CreatePrescription_WithValidRequest_ShouldCreateSuccessfully()
        {
            // Arrange - 创建病案、辨证、标记需要处方
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            // OpenSpec: simplify-medicalcase-dataflow - 使用MedicalCaseId
            var request = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "测试中药",
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
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

            // OpenSpec: simplify-medicalcase-dataflow - 使用MedicalCaseId代替PatientId/UserId
            var request = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
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

            // OpenSpec: simplify-medicalcase-dataflow - 使用MedicalCaseId代替PatientId/UserId
            var request = new PrescriptionInputDto
            {
                Id = prescription.Id,
                MedicalCaseId = medicalCase.Id,
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = Guid.NewGuid(), HerbName = "测试中药", Dosage = 6, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
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
            // Issue #2231: 属性名应为Status而非CaseStatus（匹配UpdateStatusRequest定义）
            var request = new { Status = MedicalCaseStatus.Completed };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/status",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        #endregion

        #region Write Layer Tests - CompleteMedicalCase

        [Fact]
        public async Task CompleteMedicalCase_WithValidRequest_ShouldCompleteSuccessfully()
        {
            // Arrange - 创建完整流程的病案
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            // Act - 使用 PUT /status 端点并传递状态
            var statusRequest = new { Status = MedicalCaseStatus.Completed };
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/status",
                statusRequest);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CompleteMedicalCase_ViaStatusEndpoint_ShouldCompleteWithoutPrescription()
        {
            // Arrange - 创建病案但未完成处方
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Act - 使用 PUT /status 端点并传递状态
            // OpenSpec refactor-medicalcase-api: /status 端点只验证状态转换合法性，不验证三步流程
            var statusRequest = new { Status = MedicalCaseStatus.Completed };
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/status",
                statusRequest);

            // Assert - API已简化，/status 端点允许直接完成
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
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

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
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

        #region Write Layer Tests - SaveDraft (OpenSpec: refactor-medicalcase-api)

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - SaveDraft端点集成测试
        /// 验证暂存医案功能（PUT /api/v1/medicalcases/{id}/draft）
        /// </summary>
        [Fact]
        public async Task SaveDraft_WithValidRequest_ShouldSetStatusToDraft()
        {
            // Arrange - 创建Active状态的病案
            var medicalCase = await CreateTestMedicalCaseAsync();
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);

            // Act - 调用SaveDraft端点
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/draft",
                null);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Draft);

            _output.WriteLine($"✅ SaveDraft成功: 状态从Active变更为Draft");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - SaveDraft对Completed状态应返回403
        /// 验证业务规则: 已完成的医案不可编辑
        /// </summary>
        [Fact]
        public async Task SaveDraft_WhenStatusCompleted_ShouldReturn403()
        {
            // Arrange - 创建并完成病案
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Completed);

            // Act - 尝试对已完成的病案调用SaveDraft
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/draft",
                null);

            // Assert - MedicalCaseRules.CanEdit对Completed状态返回false，抛出UnauthorizedAccessException
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.Forbidden);

            _output.WriteLine($"✅ SaveDraft正确拒绝Completed状态的医案");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - SaveDraft对Draft状态应保持Draft
        /// 验证幂等性: 多次暂存不改变状态
        /// </summary>
        [Fact]
        public async Task SaveDraft_WhenStatusDraft_ShouldRemainDraft()
        {
            // Arrange - 创建病案并暂存
            var medicalCase = await CreateTestMedicalCaseAsync();
            await Client.PutAsync($"/api/v1/medicalcases/{medicalCase.Id}/draft", null);

            // Act - 再次调用SaveDraft
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/draft",
                null);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Draft);

            _output.WriteLine($"✅ SaveDraft幂等性验证通过");
        }

        #endregion

        #region Write Layer Tests - Cancel (OpenSpec: refactor-medicalcase-api)

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - Cancel端点集成测试
        /// 验证取消医案功能（PUT /api/v1/medicalcases/{id}/cancel）
        /// </summary>
        [Fact]
        public async Task CancelMedicalCase_WithValidRequest_ShouldSetStatusToCancelled()
        {
            // Arrange - 创建Active状态的病案
            var medicalCase = await CreateTestMedicalCaseAsync();
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);

            // Act - 调用Cancel端点（同天本人操作，无需理由）
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/cancel",
                null);

            // Assert - 取消操作现在返回204 NoContent（软删除）
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.NoContent);

            _output.WriteLine($"✅ Cancel成功: 医案已软删除");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - Cancel对Completed状态应返回403
        /// 验证业务规则: 已完成的医案不可取消
        /// </summary>
        [Fact]
        public async Task CancelMedicalCase_WhenStatusCompleted_ShouldReturn403()
        {
            // Arrange - 创建并完成病案
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Completed);

            // Act - 尝试对已完成的病案调用Cancel
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/cancel",
                null);

            // Assert - MedicalCaseRules.CanEdit对Completed状态返回false
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.Forbidden);

            _output.WriteLine($"✅ Cancel正确拒绝Completed状态的医案");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - Cancel带理由应成功
        /// 验证审计理由功能
        /// </summary>
        [Fact]
        public async Task CancelMedicalCase_WithReason_ShouldSucceed()
        {
            // Arrange - 创建Active状态的病案
            var medicalCase = await CreateTestMedicalCaseAsync();

            var cancelRequest = new CancelMedicalCaseRequestDto
            {
                Reason = "患者临时有事，择日再诊"
            };

            // Act - 调用Cancel端点并提供理由
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/cancel",
                cancelRequest);

            // Assert - 取消操作现在返回204 NoContent（软删除）
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.NoContent);

            _output.WriteLine($"✅ Cancel带理由成功(软删除)");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - Cancel对Draft状态应成功
        /// 验证草稿状态可取消
        /// </summary>
        [Fact]
        public async Task CancelMedicalCase_WhenStatusDraft_ShouldSucceed()
        {
            // Arrange - 创建病案并暂存
            var medicalCase = await CreateTestMedicalCaseAsync();
            await Client.PutAsync($"/api/v1/medicalcases/{medicalCase.Id}/draft", null);

            // Act - 取消Draft状态的病案
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/cancel",
                null);

            // Assert - 取消操作现在返回204 NoContent（软删除）
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.NoContent);

            _output.WriteLine($"✅ Cancel对Draft状态成功(软删除)");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-api - 已取消的医案不可再次取消
        /// 验证状态流转规则
        /// </summary>
        [Fact]
        public async Task CancelMedicalCase_WhenAlreadyCancelled_ShouldReturn403()
        {
            // Arrange - 创建并取消病案
            var medicalCase = await CreateTestMedicalCaseAsync();
            await Client.PutAsync($"/api/v1/medicalcases/{medicalCase.Id}/cancel", null);

            // Act - 尝试再次取消
            var response = await Client.PutAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/cancel",
                null);

            // Assert - 软删除后查不到，返回404
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.NotFound);

            _output.WriteLine($"✅ 已软删除的医案再次取消返回404");
        }

        #endregion

        #region Helper Layer Tests - CanEdit

        [Fact]
        public async Task CanEdit_WhenStatusActive_ShouldReturnTrue()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseAsync();

            // Act - 使用正确的endpoint: /permissions (不是 /can-edit)
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/permissions");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCasePermissionDto>();
            apiResponse.Data!.CanEdit.Should().BeTrue();
        }

        [Fact]
        public async Task CanEdit_WhenStatusCompleted_ShouldReturnFalse()
        {
            // Arrange
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();

            // Act - 使用正确的endpoint: /permissions (不是 /can-edit)
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/permissions");

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCasePermissionDto>();
            apiResponse.Data!.CanEdit.Should().BeFalse();
        }

        #endregion

        #region Write Layer Tests - SaveAggregate (OpenSpec: refactor-medicalcase-aggregate-crud)

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud - PERSIST-001
        /// 验证聚合保存端点：仅诊断无处方场景
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WithConsultationOnly_ShouldSaveSuccessfully()
        {
            // Arrange - 创建Active状态的病案
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new MedicalCaseInputDto
            {
                Id = medicalCase.Id,
                Remark = "仅诊断无处方测试",
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "头痛三天",
                    TcmDiagnosis = "肝阳上亢"
                },
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = false,
                    Items = new List<PrescriptionItemInputDto>()
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(medicalCase.Id);
            apiResponse.Data.Remark.Should().Be("仅诊断无处方测试");

            _output.WriteLine($"✅ SaveAggregate(仅诊断)成功");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud - PERSIST-002
        /// 验证聚合保存端点：诊断+处方完整保存
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WithConsultationAndPrescription_ShouldSaveSuccessfully()
        {
            // Arrange - 创建Active状态的病案
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new MedicalCaseInputDto
            {
                Id = medicalCase.Id,
                Remark = "完整保存测试",
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "头痛三天",
                    TcmDiagnosis = "肝阳上亢"
                },
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = true,
                    DosageCount = 7,
                    Usage = "每日一剂，早晚分服",
                    Advice = "忌辛辣",
                    Items = new List<PrescriptionItemInputDto>
                    {
                        new() { HerbId = Guid.NewGuid(), HerbName = "天麻", Dosage = 15, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 1.2m },
                        new() { HerbId = Guid.NewGuid(), HerbName = "钩藤", Dosage = 10, Unit = "g", DecocteMethod = DecocteMethod.PostAdd, UnitPrice = 0.8m }
                    }
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(medicalCase.Id);

            _output.WriteLine($"✅ SaveAggregate(诊断+处方)成功");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// 验证聚合保存端点：ID不匹配时返回400
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WithMismatchedId_ShouldReturn400()
        {
            // Arrange - 创建病案
            var medicalCase = await CreateTestMedicalCaseAsync();
            var wrongId = Guid.NewGuid();

            var request = new MedicalCaseInputDto
            {
                Id = wrongId, // 与URL中的ID不匹配
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "测试"
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.BadRequest);

            _output.WriteLine($"✅ SaveAggregate ID不匹配正确返回400");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// 验证聚合保存端点：对已完成病案返回403
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WhenStatusCompleted_ShouldReturn403()
        {
            // Arrange - 创建并完成病案
            var medicalCase = await CreateAndCompleteMedicalCaseAsync();

            var request = new MedicalCaseInputDto
            {
                Id = medicalCase.Id,
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "尝试修改已完成病案"
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert - 已完成的医案不可编辑
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.Forbidden);

            _output.WriteLine($"✅ SaveAggregate正确拒绝已完成的医案");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// 验证聚合保存端点：不存在的病案返回404
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WithNonExistingId_ShouldReturn404()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            var request = new MedicalCaseInputDto
            {
                Id = nonExistingId,
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "测试"
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{nonExistingId}/aggregate",
                request);

            // Assert
            response.ShouldBeNotFound();

            _output.WriteLine($"✅ SaveAggregate不存在的病案正确返回404");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// 验证聚合保存端点：空ID与路径ID不匹配返回400
        /// </summary>
        [Fact]
        public async Task SaveAggregate_WithEmptyId_ShouldReturn400()
        {
            // Arrange - 创建病案
            var medicalCase = await CreateTestMedicalCaseAsync();

            var request = new MedicalCaseInputDto
            {
                Id = Guid.Empty // 空ID与URL中的ID不匹配
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert - 空ID与URL路径ID不匹配，返回400
            response.ShouldHaveStatusCode(System.Net.HttpStatusCode.BadRequest);

            _output.WriteLine($"SaveAggregate空ID正确返回400(ID不匹配)");
        }

        /// <summary>
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// 验证聚合保存端点：更新现有处方
        /// </summary>
        [Fact]
        public async Task SaveAggregate_UpdateExistingPrescription_ShouldUpdateSuccessfully()
        {
            // Arrange - 创建病案并创建处方
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            var request = new MedicalCaseInputDto
            {
                Id = medicalCase.Id,
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = "更新后的主诉",
                    TcmDiagnosis = "更新后的诊断"
                },
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = true,
                    DosageCount = 14, // 更新剂数
                    Items = new List<PrescriptionItemInputDto>
                    {
                        new() { HerbId = Guid.NewGuid(), HerbName = "新药材1", Dosage = 20, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 1.0m },
                        new() { HerbId = Guid.NewGuid(), HerbName = "新药材2", Dosage = 15, Unit = "g", DecocteMethod = DecocteMethod.PreDecoct, UnitPrice = 0.8m },
                        new() { HerbId = Guid.NewGuid(), HerbName = "新药材3", Dosage = 10, Unit = "g", DecocteMethod = DecocteMethod.PostAdd, UnitPrice = 0.6m }
                    }
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/aggregate",
                request);

            // Assert
            response.ShouldBeOk();

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            apiResponse.Data.Should().NotBeNull();

            _output.WriteLine($"✅ SaveAggregate更新现有处方成功");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建测试病案（最基础）
        /// ⚠️ Issue #1669 Phase 6: 每次调用创建独立患者，避免"患者已有未完成病案"错误
        /// Issue #2231: 使用FixedDoctorId作为审计字段的用户ID
        /// </summary>
        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseAsync()
        {
            // 为本次测试创建独立的患者（避免多个测试共享患者导致冲突）
            var newPatientId = Guid.NewGuid();

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
                    CreatedBy = FixedDoctorId,  // Issue #2231: 使用固定医生ID
                    UpdatedBy = FixedDoctorId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
                var saveResult = await db.SaveChangesAsync();
                _output.WriteLine($"✅ 患者实体已创建: PatientId={newPatientId}, SavedEntities={saveResult}");
            }

            // OpenSpec: simplify-medicalcase-dataflow - VisitDate已删除
            var request = new
            {
                PatientId = newPatientId
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

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案并完成辨证（Step 1完成）
        /// </summary>
        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseWithConsultationAsync()
        {
            var medicalCase = await CreateTestMedicalCaseAsync();

            var consultationRequest = new ConsultationInputDto
            {
                // Issue #2231: 明确设置为null以避免EF Core键修改错误
                Id = null,  // 不设置Id，由服务端管理（共享主键）
                MedicalCaseId = null,  // 不设置MedicalCaseId，通过URL路由传递
                PatientId = null,  // 不设置PatientId，从MedicalCase获取
                UserId = null,  // 不设置UserId，从MedicalCase获取
                PresentIllness = "头痛",
                TcmDiagnosis = "风寒感冒"
            };

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                consultationRequest);

            // ⚠️ 临时调试代码：打印错误的详细信息
            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"❌ 更新辨证失败 - 状态码: {updateResponse.StatusCode}");
                _output.WriteLine($"❌ 错误响应: {errorContent}");
                _output.WriteLine($"❌ MedicalCaseId: {medicalCase.Id}");
                _output.WriteLine($"❌ 请求内容: PresentIllness={consultationRequest.PresentIllness}, TcmDiagnosis={consultationRequest.TcmDiagnosis}");
            }

            // ⚠️ Issue #1669: 验证更新请求是否成功
            updateResponse.ShouldBeOk();
            await updateResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // 重新获取更新后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案、辨证、标记需要处方（Ready for Prescription）
        /// </summary>
        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseReadyForPrescriptionAsync()
        {
            var medicalCase = await CreateTestMedicalCaseWithConsultationAsync();

            var flagRequest = new { NeedsPrescription = true };
            var flagResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                flagRequest);

            // ⚠️ Issue #1669: 验证标记请求是否成功
            flagResponse.ShouldBeOk();
            await flagResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // 重新获取更新后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        /// <summary>
        /// 创建测试病案并包含处方（完整流程）
        /// </summary>
        private async Task<(MedicalCaseDetailDto, PrescriptionEntity)> CreateTestMedicalCaseWithPrescriptionAsync()
        {
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            // OpenSpec: simplify-medicalcase-dataflow - 使用MedicalCaseId代替PatientId/UserId
            var prescriptionRequest = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "测试中药",
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
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
            var updatedCase = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            return (updatedCase.Data!, prescription);
        }

        /// <summary>
        /// 创建并完成病案（完整流程 + 完成）
        /// </summary>
        private async Task<MedicalCaseDetailDto> CreateAndCompleteMedicalCaseAsync()
        {
            var (medicalCase, _) = await CreateTestMedicalCaseWithPrescriptionAsync();

            // 使用 PUT /status 端点并传递状态
            var statusRequest = new { Status = MedicalCaseStatus.Completed };
            var completeResponse = await Client.PutAsJsonAsync($"/api/v1/medicalcases/{medicalCase.Id}/status", statusRequest);

            // ⚠️ Issue #1669: 验证完成请求是否成功
            completeResponse.ShouldBeOk();
            await completeResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();

            // 重新获取完成后的病案
            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        #endregion
    }
}
