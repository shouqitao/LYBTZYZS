using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// Issue #2233 Task 4.1.2: 权限控制集成测试
    /// 测试CanEdit权限和医生数据隔离功能
    /// </summary>
    public class MedicalCasePermissionControlTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testPatientId;

        // 医生A: 固定ID与JWT Token匹配
        private static readonly Guid DoctorAId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        // 医生B: 不同的医生ID
        private static readonly Guid DoctorBId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        // 医生B的HttpClient（使用不同的JWT Token）
        private HttpClient? _doctorBClient;

        public MedicalCasePermissionControlTests(ITestOutputHelper output) : base()
        {
            _output = output;

            // 基类构造函数中GenerateTestToken被调用时,DoctorAId还未初始化
            // 解决方案:在派生类构造函数中重新设置Authorization header
            SetAuthorizationHeader(Client);

            // 创建医生B的HttpClient
            _doctorBClient = CreateDoctorBClient();
        }

        /// <summary>
        /// 重写JWT Token生成方法，使用医生A的固定ID
        /// </summary>
        protected override string GenerateTestToken()
        {
            return GenerateTokenForDoctor(DoctorAId, "医生A");
        }

        /// <summary>
        /// 为指定医生生成JWT Token
        /// </summary>
        private string GenerateTokenForDoctor(Guid doctorId, string doctorName)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes("TestSecretKey_MinLength32Characters_ForJWTTokenGeneration_123456789");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, doctorId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, doctorName),
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
        /// 创建医生B的HttpClient
        /// </summary>
        private HttpClient CreateDoctorBClient()
        {
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTokenForDoctor(DoctorBId, "医生B"));
            return client;
        }

        /// <summary>
        /// 重写种子数据方法，创建两个医生和测试患者
        /// </summary>
        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建医生A
            var doctorA = new LYBT.Entities.Users.User
            {
                Id = DoctorAId,
                UserName = "doctorA",
                RealName = "医生A",
                Email = "doctorA@test.com",
                PasswordHash = "DummyHashForTesting",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 创建医生B
            var doctorB = new LYBT.Entities.Users.User
            {
                Id = DoctorBId,
                UserName = "doctorB",
                RealName = "医生B",
                Email = "doctorB@test.com",
                PasswordHash = "DummyHashForTesting",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Set<LYBT.Entities.Users.User>().Add(doctorA);
            context.Set<LYBT.Entities.Users.User>().Add(doctorB);

            // 创建测试患者
            var testPatient = new LYBT.Entities.Patients.Patient
            {
                Id = Guid.NewGuid(),
                Name = "权限测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                PhoneNumber = "13800138000",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
            context.SaveChanges();

            _testPatientId = testPatient.Id;
        }

        #region CanEdit Permission Tests

        /// <summary>
        /// Issue #2233: CanEdit权限测试 - 同一医生可以编辑自己的医案
        /// 验收标准: 测试CanEdit_ShouldReturnTrue_WhenSameDoctorId
        /// </summary>
        [Fact]
        public async Task CanEdit_ShouldReturnTrue_WhenSameDoctorId()
        {
            // Arrange - 医生A创建医案
            var medicalCase = await CreateMedicalCaseByDoctorAAsync();
            _output.WriteLine($"医生A创建的医案ID: {medicalCase.Id}, DoctorId: {medicalCase.DoctorId}");

            // Act - 医生A检查是否可编辑（使用医生A的Client）
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/can-edit");

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<LYBT.Module.MedicalCase.Interfaces.CanEditResponse>();

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.CanEdit.Should().BeTrue("同一医生应该可以编辑自己创建的医案");

            _output.WriteLine($"CanEdit返回: {apiResponse.Data.CanEdit}");
        }

        /// <summary>
        /// Issue #2233: CanEdit权限测试 - 不同医生不能编辑他人的医案
        /// 验收标准: 测试CanEdit_ShouldReturnFalse_WhenDifferentDoctorId
        /// </summary>
        [Fact]
        public async Task CanEdit_ShouldReturnFalse_WhenDifferentDoctorId()
        {
            // Arrange - 医生A创建医案
            var medicalCase = await CreateMedicalCaseByDoctorAAsync();
            _output.WriteLine($"医生A创建的医案ID: {medicalCase.Id}, DoctorId: {medicalCase.DoctorId}");

            // Act - 医生B检查是否可编辑（使用医生B的Client）
            var response = await _doctorBClient!.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}/can-edit");

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<LYBT.Module.MedicalCase.Interfaces.CanEditResponse>();

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.CanEdit.Should().BeFalse("不同医生不应该能编辑他人创建的医案");

            _output.WriteLine($"医生B尝试编辑医生A的医案, CanEdit返回: {apiResponse.Data.CanEdit}");
        }

        #endregion

        #region GetUnfinishedCase Doctor Filter Tests

        /// <summary>
        /// Issue #2233: GetUnfinishedCase医生过滤测试 - 能获取自己的未完成医案
        /// 验收标准: 测试GetUnfinishedCase_ShouldFilterByDoctorId
        /// </summary>
        [Fact]
        public async Task GetUnfinishedCase_ShouldFilterByDoctorId()
        {
            // Arrange - 医生A创建医案
            var medicalCase = await CreateMedicalCaseByDoctorAAsync();
            _output.WriteLine($"医生A创建的医案ID: {medicalCase.Id}, PatientId: {medicalCase.PatientId}");

            // Act - 医生A查询该患者的未完成医案
            var response = await Client.GetAsync($"/api/v1/medicalcases/patient/{medicalCase.PatientId}/unfinished");

            // Assert
            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>();

            apiResponse.Data.Should().NotBeNull("医生A应该能查询到自己创建的未完成医案");
            apiResponse.Data!.Id.Should().Be(medicalCase.Id);
            apiResponse.Data.DoctorId.Should().Be(DoctorAId);

            _output.WriteLine($"医生A成功获取未完成医案: {apiResponse.Data.Id}");
        }

        /// <summary>
        /// Issue #2233: GetUnfinishedCase数据隔离测试 - 不能获取其他医生的未完成医案
        /// 验收标准: 测试GetUnfinishedCase_ShouldNotReturnOtherDoctorsCases
        /// </summary>
        [Fact]
        public async Task GetUnfinishedCase_ShouldNotReturnOtherDoctorsCases()
        {
            // Arrange - 医生A创建医案
            var medicalCase = await CreateMedicalCaseByDoctorAAsync();
            _output.WriteLine($"医生A创建的医案ID: {medicalCase.Id}, PatientId: {medicalCase.PatientId}");

            // Act - 医生B查询同一患者的未完成医案
            var response = await _doctorBClient!.GetAsync($"/api/v1/medicalcases/patient/{medicalCase.PatientId}/unfinished");

            // Assert - 医生B不应该获取到医生A的医案
            _output.WriteLine($"医生B查询返回状态码: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应内容: {content}");

            // 根据API设计，如果没有找到当前医生的未完成医案，应该返回空数据或404
            // 这里验证医生B不能看到医生A的医案
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<MedicalCaseDto>>();

                if (apiResponse?.Data != null)
                {
                    // 如果返回了数据，验证不是医生A的医案
                    apiResponse.Data.DoctorId.Should().NotBe(DoctorAId, "医生B不应该看到医生A的医案");
                }
                else
                {
                    // 数据为空，这是正确的行为
                    _output.WriteLine("医生B正确地没有获取到医生A的未完成医案（返回空数据）");
                }
            }
            else
            {
                // 返回404也是正确的行为，表示没有找到该医生的未完成医案
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound,
                    "如果医生B没有该患者的未完成医案，应该返回404");
                _output.WriteLine("医生B正确地没有获取到医生A的未完成医案（返回404）");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 医生A创建医案
        /// </summary>
        private async Task<MedicalCaseDto> CreateMedicalCaseByDoctorAAsync()
        {
            // 为测试创建独立的患者
            var newPatientId = Guid.NewGuid();

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
                    CreatedBy = DoctorAId,
                    UpdatedBy = DoctorAId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
                await db.SaveChangesAsync();
            }

            var request = new
            {
                PatientId = newPatientId,
                VisitDate = DateTime.Now
            };

            // 使用医生A的Client创建医案
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"创建医案失败: {response.StatusCode}, {errorContent}");
            }

            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>();
            return apiResponse.Data!;
        }

        #endregion

        public override void Dispose()
        {
            _doctorBClient?.Dispose();
            base.Dispose();
        }
    }
}
