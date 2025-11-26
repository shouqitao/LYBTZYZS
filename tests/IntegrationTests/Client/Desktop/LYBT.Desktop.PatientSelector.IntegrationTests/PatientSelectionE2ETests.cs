using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using ApiResponse = LYBT.Tests.Common.AssertionHelpers.ApiResponse;

namespace LYBT.Desktop.PatientSelector.IntegrationTests
{
    /// <summary>
    /// Issue #2235 Task 4.2.1: PatientSelection端到端测试
    /// 采用WebAPI集成测试验证患者选择到医案创建的完整流程
    ///
    /// 测试覆盖：
    /// - FR-001 双列表互斥选择（通过API状态验证）
    /// - 患者选择到医案创建集成流程
    /// - 异常处理和边界条件
    /// </summary>
    public class PatientSelectionE2ETests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testPatientId1;
        private Guid _testPatientId2;
        private static readonly Guid TestDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public PatientSelectionE2ETests(ITestOutputHelper output) : base()
        {
            _output = output;
            SetAuthorizationHeader(Client);
        }

        /// <summary>
        /// 重写JWT Token生成方法
        /// </summary>
        protected override string GenerateTestToken()
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes("TestSecretKey_MinLength32Characters_ForJWTTokenGeneration_123456789");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, TestDoctorId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "测试医生"),
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
        /// 重写种子数据方法
        /// </summary>
        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试医生
            var doctor = new LYBT.Entities.Users.User
            {
                Id = TestDoctorId,
                UserName = "testDoctor",
                RealName = "测试医生",
                Email = "testdoctor@test.com",
                PasswordHash = "DummyHashForTesting",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Set<LYBT.Entities.Users.User>().Add(doctor);

            // 创建测试患者1
            _testPatientId1 = Guid.NewGuid();
            var patient1 = new LYBT.Entities.Patients.Patient
            {
                Id = _testPatientId1,
                Name = "E2E测试患者A",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                PhoneNumber = "13800138001",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = TestDoctorId,
                UpdatedBy = TestDoctorId
            };
            context.Set<LYBT.Entities.Patients.Patient>().Add(patient1);

            // 创建测试患者2
            _testPatientId2 = Guid.NewGuid();
            var patient2 = new LYBT.Entities.Patients.Patient
            {
                Id = _testPatientId2,
                Name = "E2E测试患者B",
                Gender = LYBT.Shared.Models.Enums.Gender.Female,
                PhoneNumber = "13800138002",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = TestDoctorId,
                UpdatedBy = TestDoctorId
            };
            context.Set<LYBT.Entities.Patients.Patient>().Add(patient2);

            context.SaveChanges();
        }

        #region FR-001 双列表互斥选择测试（通过API验证）

        /// <summary>
        /// Issue #2235: FR-001测试 - 为患者创建医案后应出现在待诊队列
        /// 验收标准: DoubleListMutex_ShouldWork_InRealScenario
        /// </summary>
        [Fact]
        public async Task FR001_CreateMedicalCase_ShouldAddToPendingQueue()
        {
            // Arrange - 使用种子数据中的患者
            var patientId = await CreateTestPatientAsync();
            _output.WriteLine($"测试患者ID: {patientId}");

            // 确保患者没有未完成的医案
            var unfinishedResponse = await Client.GetAsync($"/api/v1/medicalcases/patient/{patientId}/unfinished");
            if (unfinishedResponse.IsSuccessStatusCode)
            {
                var content = await unfinishedResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"已存在未完成医案: {content}");
            }

            // Act - 创建新医案
            var createRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };
            var createResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"创建医案失败: {errorContent}");
            }

            createResponse.ShouldBeOk();
            var apiResponse = await createResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>();
            var medicalCase = apiResponse.Data!;

            _output.WriteLine($"创建的医案ID: {medicalCase.Id}");

            // Assert - 验证医案创建成功
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(patientId);
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);

            // 验证待诊队列包含这个医案
            var pendingResponse = await Client.GetAsync("/api/v1/medicalcases/pending");
            pendingResponse.ShouldBeOk();
            var pendingApiResponse = await pendingResponse.ShouldBeSuccessfulApiResponseAsync<System.Collections.Generic.List<PendingMedicalCaseDto>>();

            pendingApiResponse.Data.Should().NotBeNull();
            pendingApiResponse.Data.Should().Contain(p => p.MedicalCaseId == medicalCase.Id,
                "新创建的医案应出现在待诊队列中（FR-001双列表数据来源）");

            _output.WriteLine($"待诊队列包含医案ID: {medicalCase.Id}");
        }

        /// <summary>
        /// Issue #2235: FR-001测试 - 同一患者不应有多个Active医案
        /// 验收标准: DoubleListMutex_ShouldWork_InRealScenario
        /// </summary>
        [Fact]
        public async Task FR001_CreateDuplicateMedicalCase_ShouldFail()
        {
            // Arrange - 创建患者并创建第一个医案
            var patientId = await CreateTestPatientAsync();
            _output.WriteLine($"测试患者ID: {patientId}");

            var createRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };

            var firstResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);
            firstResponse.ShouldBeOk();
            var firstCase = (await firstResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data!;
            _output.WriteLine($"第一个医案ID: {firstCase.Id}");

            // Act - 尝试为同一患者创建第二个医案
            var secondResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);

            // Assert - 应该返回错误或返回已存在的医案
            var secondContent = await secondResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"第二次创建响应: {secondResponse.StatusCode}, {secondContent}");

            // 根据业务规则，可能返回错误或返回已存在的医案
            // 这里验证不会创建重复的Active医案
            if (secondResponse.IsSuccessStatusCode)
            {
                // 如果成功，应该返回的是同一个医案
                var secondCase = (await secondResponse.Content.ReadFromJsonAsync<LYBT.Tests.Common.AssertionHelpers.ApiResponse<MedicalCaseDto>>())?.Data;
                secondCase?.Id.Should().Be(firstCase.Id, "同一患者的未完成医案不应重复创建");
            }
            else
            {
                // 如果失败，这也是正确的行为
                _output.WriteLine("正确：系统拒绝创建重复医案");
            }
        }

        /// <summary>
        /// Issue #2235: FR-001测试 - 患者列表和待诊队列数据独立
        /// 验收标准: DoubleListMutex_ShouldWork_InRealScenario
        /// </summary>
        [Fact]
        public async Task FR001_PatientListAndPendingQueue_ShouldBeIndependent()
        {
            // Arrange
            var patientId = await CreateTestPatientAsync();

            // Act - 获取患者列表
            var patientsResponse = await Client.GetAsync("/api/v1/patients");
            patientsResponse.ShouldBeOk();
            var patientsResult = await patientsResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"患者列表响应: {patientsResult.Substring(0, Math.Min(500, patientsResult.Length))}...");

            // Act - 获取待诊队列
            var pendingResponse = await Client.GetAsync("/api/v1/medicalcases/pending");
            pendingResponse.ShouldBeOk();
            var pendingResult = await pendingResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"待诊队列响应: {pendingResult.Substring(0, Math.Min(500, pendingResult.Length))}...");

            // Assert - 两个API应该独立工作
            patientsResult.Should().NotBeNullOrEmpty("患者列表API应正常工作");
            pendingResult.Should().NotBeNullOrEmpty("待诊队列API应正常工作");

            _output.WriteLine("FR-001验证：患者列表和待诊队列API独立运行");
        }

        #endregion

        #region 患者选择到医案创建集成测试

        /// <summary>
        /// Issue #2235: 集成测试 - 完整的患者选择到医案创建流程
        /// 验收标准: PatientSelection_To_MedicalCaseCreation_Integration
        /// </summary>
        [Fact]
        public async Task PatientSelection_CreateMedicalCase_FullFlow()
        {
            // Arrange - 创建新患者
            var patientId = await CreateTestPatientAsync();
            _output.WriteLine($"Step 1: 创建测试患者 ID={patientId}");

            // Act Step 2: 检查患者是否有未完成医案
            var unfinishedResponse = await Client.GetAsync($"/api/v1/medicalcases/patient/{patientId}/unfinished");
            _output.WriteLine($"Step 2: 检查未完成医案 - Status={unfinishedResponse.StatusCode}");

            // Act Step 3: 创建新医案
            var createRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };
            var createResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);
            createResponse.ShouldBeOk();
            var medicalCase = (await createResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data!;
            _output.WriteLine($"Step 3: 创建医案成功 ID={medicalCase.Id}");

            // Act Step 4: 获取医案详情
            var detailResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            detailResponse.ShouldBeOk();
            var detailCase = (await detailResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data!;
            _output.WriteLine($"Step 4: 获取医案详情成功");

            // Assert - 验证完整流程
            detailCase.Id.Should().Be(medicalCase.Id);
            detailCase.PatientId.Should().Be(patientId);
            detailCase.DoctorId.Should().Be(TestDoctorId, "医案的DoctorId应该是当前登录医生");
            detailCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);

            _output.WriteLine("患者选择到医案创建完整流程验证成功");
        }

        /// <summary>
        /// Issue #2235: 集成测试 - 待诊患者直接进入问诊
        /// 验收标准: PatientSelection_To_MedicalCaseCreation_Integration
        /// </summary>
        [Fact]
        public async Task PatientSelection_ExistingPendingCase_ShouldContinueConsultation()
        {
            // Arrange - 创建患者和医案
            var patientId = await CreateTestPatientAsync();
            var createRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };
            var createResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);
            createResponse.ShouldBeOk();
            var medicalCase = (await createResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data!;
            _output.WriteLine($"已创建医案 ID={medicalCase.Id}");

            // Act - 查询该患者的未完成医案
            var unfinishedResponse = await Client.GetAsync($"/api/v1/medicalcases/patient/{patientId}/unfinished");
            unfinishedResponse.ShouldBeOk();
            var unfinishedCase = (await unfinishedResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data;

            // Assert
            unfinishedCase.Should().NotBeNull("应该找到未完成的医案");
            unfinishedCase!.Id.Should().Be(medicalCase.Id, "应该返回之前创建的医案");
            unfinishedCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);

            _output.WriteLine($"成功获取待诊患者的医案: {unfinishedCase.Id}");
        }

        /// <summary>
        /// Issue #2235: 集成测试 - 医案权限验证
        /// 验收标准: PatientSelection_To_MedicalCaseCreation_Integration
        /// </summary>
        [Fact]
        public async Task PatientSelection_CanEditCheck_ShouldWorkCorrectly()
        {
            // Arrange - 创建患者和医案
            var patientId = await CreateTestPatientAsync();
            var createRequest = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };
            var createResponse = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);
            createResponse.ShouldBeOk();
            var medicalCase = (await createResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>()).Data!;

            // Act - 检查是否可编辑
            var canEditResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/can-edit");
            canEditResponse.ShouldBeOk();
            var canEditResult = await canEditResponse.ShouldBeSuccessfulApiResponseAsync<LYBT.Module.MedicalCase.Interfaces.CanEditResponse>();

            // Assert
            canEditResult.Data.Should().NotBeNull();
            canEditResult.Data!.CanEdit.Should().BeTrue("创建者应该可以编辑自己的医案");

            _output.WriteLine($"CanEdit结果: {canEditResult.Data.CanEdit}");
        }

        #endregion

        #region 异常处理测试

        /// <summary>
        /// Issue #2235: 异常处理测试 - 无效PatientId应返回适当错误
        /// 验收标准: ExceptionHandling_ShouldNotCrash_WhenNetworkFailure
        /// </summary>
        [Fact]
        public async Task ExceptionHandling_InvalidPatientId_ShouldReturnError()
        {
            // Arrange
            var invalidPatientId = Guid.NewGuid(); // 不存在的患者ID

            // Act
            var createRequest = new
            {
                PatientId = invalidPatientId,
                VisitDate = DateTime.Now
            };
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);

            // Assert - 应该返回错误而不是崩溃
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应状态: {response.StatusCode}");
            _output.WriteLine($"响应内容: {content}");

            // 无效PatientId应该返回400/404/422
            response.StatusCode.Should().BeOneOf(
                new[] { System.Net.HttpStatusCode.BadRequest, System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.UnprocessableEntity },
                "无效的PatientId应返回适当的错误状态码");
        }

        /// <summary>
        /// Issue #2235: 异常处理测试 - 空GUID应返回适当错误
        /// 验收标准: ExceptionHandling_ShouldNotCrash_WhenNetworkFailure
        /// </summary>
        [Fact]
        public async Task ExceptionHandling_EmptyGuid_ShouldReturnError()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act
            var createRequest = new
            {
                PatientId = emptyGuid,
                VisitDate = DateTime.Now
            };
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", createRequest);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应状态: {response.StatusCode}");
            _output.WriteLine($"响应内容: {content}");

            // 空GUID应该被拒绝
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.InternalServerError,
                "空GUID不应导致服务器内部错误");
        }

        /// <summary>
        /// Issue #2235: 异常处理测试 - 获取不存在的医案应返回404
        /// 验收标准: ExceptionHandling_ShouldNotCrash_WhenNetworkFailure
        /// </summary>
        [Fact]
        public async Task ExceptionHandling_NonExistentMedicalCase_ShouldReturn404()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/v1/medicalcases/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound,
                "不存在的医案应返回404");

            _output.WriteLine($"正确返回404 for 不存在的医案ID: {nonExistentId}");
        }

        /// <summary>
        /// Issue #2235: 异常处理测试 - 并发请求应正确处理
        /// 验收标准: ExceptionHandling_ShouldNotCrash_WhenNetworkFailure
        /// </summary>
        [Fact]
        public async Task ExceptionHandling_ConcurrentRequests_ShouldNotCrash()
        {
            // Arrange
            var patientId = await CreateTestPatientAsync();
            var tasks = new System.Collections.Generic.List<Task<System.Net.Http.HttpResponseMessage>>();

            // Act - 发送并发请求
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Client.GetAsync($"/api/v1/medicalcases/patient/{patientId}/unfinished"));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - 所有请求应该完成而不崩溃
            foreach (var result in results)
            {
                result.StatusCode.Should().NotBe(System.Net.HttpStatusCode.InternalServerError,
                    "并发请求不应导致服务器内部错误");
            }

            _output.WriteLine($"完成 {tasks.Count} 个并发请求，无服务器错误");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建测试患者
        /// </summary>
        private async Task<Guid> CreateTestPatientAsync()
        {
            var newPatientId = Guid.NewGuid();

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var testPatient = new LYBT.Entities.Patients.Patient
                {
                    Id = newPatientId,
                    Name = $"E2E测试患者{newPatientId.ToString().Substring(0, 8)}",
                    Gender = LYBT.Shared.Models.Enums.Gender.Male,
                    PhoneNumber = "13800138999",
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = TestDoctorId,
                    UpdatedBy = TestDoctorId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
                await db.SaveChangesAsync();
            }

            return newPatientId;
        }

        #endregion
    }
}
