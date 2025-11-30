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
    /// Issue #2234 Task 4.1.3: 医生过滤集成测试
    /// 测试场景：同一患者有多个医生的医案，验证医生筛选逻辑
    /// </summary>
    public class MedicalCaseDoctorFilterTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        // 医生A: 固定ID与JWT Token匹配
        private static readonly Guid DoctorAId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        // 医生B: 不同的医生ID
        private static readonly Guid DoctorBId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        // 共享患者ID（同一患者有两个医生的医案）
        private Guid _sharedPatientId;

        // 医生B的HttpClient
        private HttpClient? _doctorBClient;

        public MedicalCaseDoctorFilterTests(ITestOutputHelper output) : base()
        {
            _output = output;

            // 重新设置Authorization header（基类构造函数中DoctorAId还未初始化）
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
        /// 重写种子数据方法，创建两个医生和共享患者
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

            // 创建共享患者（用于测试同一患者有多个医生的医案）
            _sharedPatientId = Guid.NewGuid();
            var sharedPatient = new LYBT.Entities.Patients.Patient
            {
                Id = _sharedPatientId,
                Name = "共享测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                PhoneNumber = "13800138888",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Set<LYBT.Entities.Patients.Patient>().Add(sharedPatient);
            context.SaveChanges();
        }

        #region GetUnfinishedCase Doctor Filter Tests

        /// <summary>
        /// Issue #2234: 医生过滤测试 - 同一患者有两个医生的医案时，只返回当前医生的医案
        /// 验收标准: GetUnfinishedCase_ShouldReturnOnlyCurrentDoctorCases
        ///
        /// 场景：
        /// 1. 通过数据库直接Seed医生A和医生B的医案（绕过业务规则）
        /// 2. 医生A查询未完成医案 → 应该只返回医案A
        /// 3. 医生B查询未完成医案 → 应该只返回医案B
        ///
        /// 注意：业务规则阻止同一患者通过API创建多个活跃医案，
        /// 因此测试数据需要直接通过数据库Seed
        /// </summary>
        [Fact]
        public async Task GetUnfinishedCase_ShouldReturnOnlyCurrentDoctorCases()
        {
            // Arrange - 通过数据库直接Seed测试数据
            var (sharedPatientId, medicalCaseAId, medicalCaseBId) = await SeedTwoDoctorsCasesForSamePatientAsync();
            _output.WriteLine($"共享患者ID: {sharedPatientId}");
            _output.WriteLine($"医生A的医案ID: {medicalCaseAId}");
            _output.WriteLine($"医生B的医案ID: {medicalCaseBId}");

            // Act 1 - 医生A查询该患者的未完成医案
            var responseA = await Client.GetAsync($"/api/v1/medicalcases/patient/{sharedPatientId}/unfinished");

            // Assert 1 - 医生A只能看到自己的医案A
            responseA.ShouldBeOk();
            var apiResponseA = await responseA.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>();

            apiResponseA.Data.Should().NotBeNull("医生A应该能查询到未完成医案");
            apiResponseA.Data!.Id.Should().Be(medicalCaseAId, "应该返回医生A创建的医案");
            apiResponseA.Data.DoctorId.Should().Be(DoctorAId, "返回的医案应该属于医生A");
            apiResponseA.Data.Id.Should().NotBe(medicalCaseBId, "不应该返回医生B的医案");

            _output.WriteLine($"医生A查询结果: 医案ID={apiResponseA.Data.Id}, DoctorId={apiResponseA.Data.DoctorId}");

            // Act 2 - 医生B查询该患者的未完成医案
            var responseB = await _doctorBClient!.GetAsync($"/api/v1/medicalcases/patient/{sharedPatientId}/unfinished");

            // Assert 2 - 医生B只能看到自己的医案B
            responseB.ShouldBeOk();
            var apiResponseB = await responseB.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDto>();

            apiResponseB.Data.Should().NotBeNull("医生B应该能查询到未完成医案");
            apiResponseB.Data!.Id.Should().Be(medicalCaseBId, "应该返回医生B创建的医案");
            apiResponseB.Data.DoctorId.Should().Be(DoctorBId, "返回的医案应该属于医生B");
            apiResponseB.Data.Id.Should().NotBe(medicalCaseAId, "不应该返回医生A的医案");

            _output.WriteLine($"医生B查询结果: 医案ID={apiResponseB.Data.Id}, DoctorId={apiResponseB.Data.DoctorId}");
            _output.WriteLine("验证通过: 同一患者有两个医生的医案时，各自只能看到自己的医案");
        }

        /// <summary>
        /// Issue #2234: 医生过滤测试 - 当患者只有其他医生的医案时返回null
        /// 验收标准: GetUnfinishedCase_ShouldReturnNull_WhenOtherDoctorCase
        ///
        /// 场景：
        /// 1. 医生A为共享患者创建医案A
        /// 2. 医生B查询该患者的未完成医案 → 应该返回null/404
        /// </summary>
        [Fact]
        public async Task GetUnfinishedCase_ShouldReturnNull_WhenOtherDoctorCase()
        {
            // Arrange - 创建共享患者
            var sharedPatientId = await CreateSharedPatientAsync();
            _output.WriteLine($"共享患者ID: {sharedPatientId}");

            // Step 1: 只有医生A为共享患者创建医案
            var medicalCaseA = await CreateMedicalCaseAsync(Client, sharedPatientId);
            _output.WriteLine($"医生A创建的医案ID: {medicalCaseA.Id}, DoctorId: {medicalCaseA.DoctorId}");

            // Act - 医生B查询该患者的未完成医案
            var response = await _doctorBClient!.GetAsync($"/api/v1/medicalcases/patient/{sharedPatientId}/unfinished");

            // Assert - 医生B不应该看到医生A的医案
            _output.WriteLine($"医生B查询返回状态码: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应内容: {content}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<MedicalCaseDto>>();

                if (apiResponse?.Data != null)
                {
                    // 如果返回了数据，验证不是医生A的医案
                    apiResponse.Data.DoctorId.Should().NotBe(DoctorAId, "医生B不应该看到医生A的医案");
                    _output.WriteLine("警告: 医生B获取到了数据，但不是医生A的医案");
                }
                else
                {
                    // 数据为空，这是正确的行为
                    _output.WriteLine("验证通过: 医生B正确地没有获取到医生A的未完成医案（返回空数据）");
                }
            }
            else
            {
                // 返回404也是正确的行为
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound,
                    "如果医生B没有该患者的未完成医案，应该返回404");
                _output.WriteLine("验证通过: 医生B正确地没有获取到医生A的未完成医案（返回404）");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 通过数据库直接Seed同一患者的两个医生的医案
        /// 绕过业务规则（同一患者不能有多个活跃医案）进行测试
        /// </summary>
        private async Task<(Guid PatientId, Guid CaseAId, Guid CaseBId)> SeedTwoDoctorsCasesForSamePatientAsync()
        {
            var patientId = Guid.NewGuid();
            var caseAId = Guid.NewGuid();
            var caseBId = Guid.NewGuid();
            var patientName = $"共享患者{patientId.ToString().Substring(0, 8)}";

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

                // 创建共享患者
                var patient = new LYBT.Entities.Patients.Patient
                {
                    Id = patientId,
                    Name = patientName,
                    Gender = LYBT.Shared.Models.Enums.Gender.Male,
                    PhoneNumber = "13800138888",
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(patient);

                // 创建医生A的医案（使用正确的Entity属性）
                var medicalCaseA = new LYBT.Entities.MedicalCases.MedicalCase
                {
                    Id = caseAId,
                    PatientId = patientId,
                    PatientName = patientName,
                    DoctorId = DoctorAId,
                    DoctorName = "医生A",
                    ConsultationDate = DateTime.Now.AddDays(-1),
                    CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    UpdatedAt = DateTime.Now.AddDays(-1),
                    CreatedBy = DoctorAId,
                    UpdatedBy = DoctorAId,
                    // 设置Consultation导航属性
                    Consultation = new LYBT.Entities.Consultations.Consultation
                    {
                        Id = caseAId,
                        CreatedAt = DateTime.Now.AddDays(-1),
                        UpdatedAt = DateTime.Now.AddDays(-1),
                        CreatedBy = DoctorAId,
                        UpdatedBy = DoctorAId
                    }
                };
                db.Set<LYBT.Entities.MedicalCases.MedicalCase>().Add(medicalCaseA);

                // 创建医生B的医案（使用正确的Entity属性）
                var medicalCaseB = new LYBT.Entities.MedicalCases.MedicalCase
                {
                    Id = caseBId,
                    PatientId = patientId,
                    PatientName = patientName,
                    DoctorId = DoctorBId,
                    DoctorName = "医生B",
                    ConsultationDate = DateTime.Now,
                    CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = DoctorBId,
                    UpdatedBy = DoctorBId,
                    // 设置Consultation导航属性
                    Consultation = new LYBT.Entities.Consultations.Consultation
                    {
                        Id = caseBId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        CreatedBy = DoctorBId,
                        UpdatedBy = DoctorBId
                    }
                };
                db.Set<LYBT.Entities.MedicalCases.MedicalCase>().Add(medicalCaseB);

                await db.SaveChangesAsync();
            }

            return (patientId, caseAId, caseBId);
        }

        /// <summary>
        /// 创建共享患者（每个测试独立）
        /// </summary>
        private async Task<Guid> CreateSharedPatientAsync()
        {
            var patientId = Guid.NewGuid();

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var patient = new LYBT.Entities.Patients.Patient
                {
                    Id = patientId,
                    Name = $"共享患者{patientId.ToString().Substring(0, 8)}",
                    Gender = LYBT.Shared.Models.Enums.Gender.Male,
                    PhoneNumber = "13800138888",
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(patient);
                await db.SaveChangesAsync();
            }

            return patientId;
        }

        /// <summary>
        /// 使用指定的HttpClient创建医案
        /// </summary>
        private async Task<MedicalCaseDto> CreateMedicalCaseAsync(HttpClient client, Guid patientId)
        {
            var request = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };

            var response = await client.PostAsJsonAsync("/api/v1/medicalcases", request);

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
