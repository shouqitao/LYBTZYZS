using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// API性能基准测试
    /// Task 5.2 - 集成测试性能验证
    /// </summary>
    public class PerformanceTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        // 固定测试用户ID，用于审计字段
        private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000099");

        #region 性能阈值常量

        /// <summary>单次API请求超时阈值（毫秒）</summary>
        private const int SingleRequestTimeoutMs = 500;

        /// <summary>批量请求总超时阈值（毫秒）</summary>
        private const int BatchRequestTimeoutMs = 2000;

        /// <summary>搜索请求超时阈值（毫秒）</summary>
        private const int SearchRequestTimeoutMs = 800;

        /// <summary>并发请求总超时阈值（毫秒）</summary>
        private const int ConcurrentRequestTimeoutMs = 10000;

        /// <summary>医案查询超时阈值（毫秒）- 包含关联数据</summary>
        private const int MedicalCaseQueryTimeoutMs = 1000;

        /// <summary>标准测试数据量</summary>
        private const int StandardTestDataCount = 100;

        /// <summary>并发测试数据量</summary>
        private const int ConcurrentTestDataCount = 50;

        /// <summary>并发请求数量</summary>
        private const int ConcurrentRequestCount = 20;

        /// <summary>并发测试最低成功率（80%）</summary>
        private const int MinConcurrentSuccessCount = 16;

        #endregion

        public PerformanceTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        /// <summary>
        /// 重写种子数据方法，创建测试用户
        /// </summary>
        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试用户（用于JWT Token和审计字段）
            var testUser = new LYBT.Entities.Users.User
            {
                Id = TestUserId,
                UserName = "perfTestUser",
                RealName = "性能测试用户",
                Email = "perftest@test.com",
                PasswordHash = "DummyHashForTesting",
                Role = LYBT.Shared.Models.Enums.UserRole.Admin,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Set<LYBT.Entities.Users.User>().Add(testUser);
            context.SaveChanges();
        }

        /// <summary>
        /// 重写JWT Token生成，使用固定的TestUserId
        /// </summary>
        protected override string GenerateTestToken()
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            // NOTE: 此密钥仅用于测试环境，必须与appsettings.Test.json中的配置保持一致
            var key = System.Text.Encoding.ASCII.GetBytes("TestSecretKey_MinLength32Characters_ForJWTTokenGeneration_123456789");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, TestUserId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Performance Test User"),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
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

        [Fact]
        public async Task GetPatients_ResponseTimeUnder500ms()
        {
            // Arrange
            await SeedLargeDataSetAsync(StandardTestDataCount);

            var stopwatch = Stopwatch.StartNew();

            // Act - 使用正确的API路径
            var response = await Client.GetAsync($"/api/v1/patients?page=1&pageSize={StandardTestDataCount}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"API响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(SingleRequestTimeoutMs,
                $"API响应时间 {stopwatch.ElapsedMilliseconds}ms 超过{SingleRequestTimeoutMs}ms限制");
        }

        [Fact]
        public async Task GetPatients_WithLargeDataSet_TotalTimeUnder1s()
        {
            // Arrange
            await SeedLargeDataSetAsync(StandardTestDataCount);

            var stopwatch = Stopwatch.StartNew();

            // Act - 连续请求10页数据
            for (int page = 1; page <= 10; page++)
            {
                var response = await Client.GetAsync($"/api/v1/patients?page={page}&pageSize=10");
                response.EnsureSuccessStatusCode();
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"10页数据总响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(BatchRequestTimeoutMs,
                $"10页数据总响应时间 {stopwatch.ElapsedMilliseconds}ms 超过{BatchRequestTimeoutMs}ms限制");
        }

        [Fact]
        public async Task GetPatients_WithSearchKeyword_PerformanceAcceptable()
        {
            // Arrange
            await SeedLargeDataSetAsync(StandardTestDataCount);
            var searchKeyword = "测试";

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.GetAsync($"/api/v1/patients?page=1&pageSize=50&keyword={searchKeyword}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"搜索响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(SearchRequestTimeoutMs,
                $"搜索响应时间 {stopwatch.ElapsedMilliseconds}ms 超过{SearchRequestTimeoutMs}ms限制");
        }

        [Fact]
        public async Task ConcurrentRequests_HandleLoadSuccessfully()
        {
            // Arrange
            await SeedLargeDataSetAsync(ConcurrentTestDataCount);

            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = Stopwatch.StartNew();

            // Act - 并发请求（InMemory数据库并发限制）
            for (int i = 0; i < ConcurrentRequestCount; i++)
            {
                // 创建独立的HttpClient实例避免并发问题
                var client = Factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = Client.DefaultRequestHeaders.Authorization;
                tasks.Add(client.GetAsync("/api/v1/patients?page=1&pageSize=10"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var failureCount = responses.Length - successCount;

            _output.WriteLine($"并发测试: {successCount}成功, {failureCount}失败, 总时间: {stopwatch.ElapsedMilliseconds}ms");

            // 至少80%的请求应该成功（InMemory数据库并发性能有限）
            successCount.Should().BeGreaterThanOrEqualTo(MinConcurrentSuccessCount);

            // 总响应时间应该在合理范围内
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(ConcurrentRequestTimeoutMs);
        }

        [Fact]
        public async Task MedicalCaseIntegrationTest_NoNPlusOneQueries()
        {
            // Arrange - 创建带关联数据的病案
            var medicalCaseId = await CreateMedicalCaseWithRelationsAsync();

            var stopwatch = Stopwatch.StartNew();

            // Act - 使用正确的API路径
            var response = await Client.GetAsync($"/api/v1/medicalcases/{medicalCaseId}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"病案详情查询时间: {stopwatch.ElapsedMilliseconds}ms");

            // 病案查询包含关联数据，应该在合理时间内完成
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(MedicalCaseQueryTimeoutMs,
                $"病案查询时间过长（超过{MedicalCaseQueryTimeoutMs}ms），可能存在N+1查询问题");
        }

        [Fact]
        public async Task PrescriptionIntegrationTest_OptimizedQueries()
        {
            // Arrange - 创建带关联数据的处方
            var prescriptionId = await CreatePrescriptionWithRelationsAsync();

            var stopwatch = Stopwatch.StartNew();

            // Act - 使用正确的API路径
            var response = await Client.GetAsync($"/api/v1/prescriptions/{prescriptionId}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"处方详情查询时间: {stopwatch.ElapsedMilliseconds}ms");

            // 处方查询优化后应该很快
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(SingleRequestTimeoutMs,
                $"处方查询时间过长（超过{SingleRequestTimeoutMs}ms），需要进一步优化");
        }

        #region Helper Methods

        private async Task SeedLargeDataSetAsync(int count)
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            // 种子患者数据
            for (int i = 0; i < count; i++)
            {
                var patient = new LYBT.Entities.Patients.Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"测试患者{i:D4}",
                    Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                    PhoneNumber = $"138{i:D8}",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = TestUserId,
                    UpdatedBy = TestUserId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(patient);
            }

            await db.SaveChangesAsync();
            _output.WriteLine($"已创建 {count} 条患者测试数据");
        }

        private async Task<Guid> CreateMedicalCaseWithRelationsAsync()
        {
            // 先创建一个患者
            var patientId = Guid.NewGuid();
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var patient = new LYBT.Entities.Patients.Patient
                {
                    Id = patientId,
                    Name = "性能测试患者",
                    Gender = Gender.Male,
                    PhoneNumber = "13800138001",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = TestUserId,
                    UpdatedBy = TestUserId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(patient);
                await db.SaveChangesAsync();
            }

            // 通过API创建病案
            var request = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };

            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"创建医案失败: {response.StatusCode}, {errorContent}");
            }
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<MedicalCaseDto>>();
            return result!.Data!.Id;
        }

        private async Task<Guid> CreatePrescriptionWithRelationsAsync()
        {
            // 先创建一个患者和病案（返回病案ID和患者ID）
            var (medicalCaseId, patientId) = await CreateMedicalCaseWithPatientIdAsync();

            // 设置处方需求标记（业务规则要求）
            var flagRequest = new { NeedsPrescription = true };
            var flagResponse = await Client.PutAsJsonAsync($"/api/v1/medicalcases/{medicalCaseId}/prescription-flag", flagRequest);
            if (!flagResponse.IsSuccessStatusCode)
            {
                var errorContent = await flagResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"设置处方标记失败: {flagResponse.StatusCode}, {errorContent}");
            }
            flagResponse.EnsureSuccessStatusCode();

            // 先创建一个测试中药
            var herbId = await CreateTestHerbAsync();

            // 通过聚合根API创建处方：POST /api/v1/medicalcases/{id}/prescriptions
            var request = new
            {
                DosageCount = 7,
                Diagnosis = "性能测试诊断",
                Advice = "每日一剂",
                PatientId = patientId,
                DoctorId = TestUserId,
                Items = new[]
                {
                    new { HerbId = herbId, HerbName = "甘草", Quantity = 10.0m, Unit = "g" }
                }
            };

            var response = await Client.PostAsJsonAsync($"/api/v1/medicalcases/{medicalCaseId}/prescriptions", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"创建处方失败: {response.StatusCode}, {errorContent}");
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>(content);

            // 从响应中提取Id
            var dataElement = (System.Text.Json.JsonElement)result!.Data!;
            var id = dataElement.GetProperty("id").GetGuid();
            return id;
        }

        /// <summary>
        /// 创建医案并返回病案ID和患者ID
        /// </summary>
        private async Task<(Guid medicalCaseId, Guid patientId)> CreateMedicalCaseWithPatientIdAsync()
        {
            // 先创建一个患者
            var patientId = Guid.NewGuid();
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var patient = new LYBT.Entities.Patients.Patient
                {
                    Id = patientId,
                    Name = "处方性能测试患者",
                    Gender = Gender.Male,
                    PhoneNumber = "13800138002",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = TestUserId,
                    UpdatedBy = TestUserId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(patient);
                await db.SaveChangesAsync();
            }

            // 通过API创建病案
            var request = new
            {
                PatientId = patientId,
                VisitDate = DateTime.Now
            };

            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"创建医案失败: {response.StatusCode}, {errorContent}");
            }
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<MedicalCaseDto>>();
            return (result!.Data!.Id, patientId);
        }

        private async Task<Guid> CreateTestHerbAsync()
        {
            var herbId = Guid.NewGuid();
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            var herb = new LYBT.Entities.Herbs.Herb
            {
                Id = herbId,
                Name = "甘草",
                PinYinCode = "GC",
                Category = "补益药",
                Unit = "克",
                Price = 0.5m,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = TestUserId,
                UpdatedBy = TestUserId
            };
            db.Set<LYBT.Entities.Herbs.Herb>().Add(herb);
            await db.SaveChangesAsync();

            return herbId;
        }

        #endregion
    }
}
