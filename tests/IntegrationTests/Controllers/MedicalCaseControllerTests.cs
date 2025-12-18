using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.IntegrationTests.Api
{
    /// <summary>
    /// MedicalCaseController API集成测试
    /// </summary>
    public class MedicalCaseControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public MedicalCaseControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 移除现有的DbContext配置
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // 添加内存数据库
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // 构建服务提供器
                    var sp = services.BuildServiceProvider();

                    // 创建数据库并种子数据
                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<AppDbContext>();
                        db.Database.EnsureCreated();
                        SeedTestData(db);
                    }
                });
            });

            _client = _factory.CreateClient();
            
            // 设置默认请求头
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            // 模拟认证（实际项目中应该使用真实的JWT token）
            SetAuthorizationHeader();
        }

        private void SetAuthorizationHeader()
        {
            // 模拟JWT token
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "test-token");
        }

        private void SeedTestData(AppDbContext context)
        {
            // 添加测试患者
            var patient = new Patient
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "测试患者",
                Gender = Gender.Male,
                Age = 35,
                Phone = "13812345678"
            };

            // 添加测试医生
            var doctor = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserName = "testdoctor",
                RealName = "测试医生",
                Role = UserRole.Doctor
            };

            context.Patients.Add(patient);
            context.Users.Add(doctor);
            context.SaveChanges();
        }

        #region Create Aggregate Tests

        [Fact]
        public async Task CreateWithDetails_ShouldCreateCompleteAggregate()
        {
            // Arrange
            var createDto = new MedicalCaseWithDetailsCreateDto
            {
                MedicalCase = new MedicalCaseInputDto
                {
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    VisitDate = DateTime.Now, // Epic #1961: 必填字段
                    Remark = "集成测试医疗案例"
                },
                Consultation = new ConsultationInputDto
                {
                    ChiefComplaint = "头痛发热3天",
                    PresentIllness = "患者3天前开始出现头痛",
                    Diagnosis = "风寒感冒",
                    TreatmentPlan = "疏风散寒"
                },
                Prescription = new PrescriptionCreateDto
                {
                    Type = "中药饮片",
                    DosageCount = 7,
                    DailyDose = 1,
                    Usage = "水煎服",
                    PayableAmount = 168.50m
                }
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/medical-cases/with-details", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<MedicalCaseDetailDto>>(responseContent);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PatientId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        }

        [Fact]
        public async Task CreateWithDetails_WithoutPrescription_ShouldSucceed()
        {
            // Arrange
            var createDto = new MedicalCaseWithDetailsCreateDto
            {
                MedicalCase = new MedicalCaseInputDto
                {
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222")
                },
                Consultation = new ConsultationInputDto
                {
                    ChiefComplaint = "测试主诉",
                    Diagnosis = "测试诊断"
                },
                Prescription = null // 不包含处方
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/medical-cases/with-details", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        #endregion

        #region Get With Details Tests

        [Fact]
        public async Task GetByIdWithDetails_ShouldReturnCompleteAggregate()
        {
            // Arrange - 先创建一个医疗案例
            var createDto = new MedicalCaseWithDetailsCreateDto
            {
                MedicalCase = new MedicalCaseInputDto
                {
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222")
                },
                Consultation = new ConsultationInputDto
                {
                    ChiefComplaint = "查询测试",
                    Diagnosis = "测试诊断"
                }
            };

            var createJson = JsonConvert.SerializeObject(createDto);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/medical-cases/with-details", createContent);

            var createResult = JsonConvert.DeserializeObject<ApiResponse<MedicalCaseDetailDto>>(
                await createResponse.Content.ReadAsStringAsync());
            var createdId = createResult!.Data!.Id;

            // Act - 查询详情
            var response = await _client.GetAsync($"/api/medical-cases/{createdId}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<MedicalCaseDetailDto>>(responseContent);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(createdId);
            result.Data.ChiefComplaint.Should().Be("查询测试");
            result.Data.Diagnosis.Should().Be("测试诊断");
        }

        [Fact]
        public async Task GetByIdWithDetails_NotFound_ShouldReturn404()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/medical-cases/{nonExistentId}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Paged Query Tests

        [Fact]
        public async Task GetPaged_ShouldReturnPagedResults()
        {
            // Arrange - 创建多个医疗案例
            for (int i = 0; i < 5; i++)
            {
                var createDto = new MedicalCaseInputDto
                {
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    VisitDate = DateTime.Now, // Epic #1961: 必填字段
                    Remark = $"测试案例{i}"
                };

                var json = JsonConvert.SerializeObject(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/medical-cases", content);
            }

            // Act
            var response = await _client.GetAsync("/api/medical-cases?page=1&pageSize=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<PagedResult<MedicalCaseDetailDto>>>(responseContent);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(3);
            result.Data.TotalCount.Should().BeGreaterOrEqualTo(5);
            result.Data.PageSize.Should().Be(3);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task CreateWithoutAuthorization_ShouldReturn401()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null; // 移除认证头

            var createDto = new MedicalCaseInputDto
            {
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                VisitDate = DateTime.Now // Epic #1961: 必填字段
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/medical-cases", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateWithInvalidData_ShouldReturn400()
        {
            // Arrange - PatientId为空
            var createDto = new MedicalCaseInputDto
            {
                PatientId = Guid.Empty, // 无效的PatientId
                DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                VisitDate = DateTime.Now // Epic #1961: 必填字段
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/medical-cases", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ServerError_ShouldReturnFriendlyMessage()
        {
            // Arrange - 创建会导致服务器错误的请求
            var createDto = new MedicalCaseWithDetailsCreateDto
            {
                MedicalCase = new MedicalCaseInputDto
                {
                    PatientId = Guid.NewGuid(), // 不存在的患者ID
                    DoctorId = Guid.NewGuid()   // 不存在的医生ID
                },
                Consultation = new ConsultationInputDto()
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/medical-cases/with-details", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().NotContain("stack trace", "不应该暴露技术细节");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task GetWithDetails_ShouldReturnWithin1Second()
        {
            // Arrange
            var createDto = new MedicalCaseInputDto
            {
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                VisitDate = DateTime.Now // Epic #1961: 必填字段
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/medical-cases", content);

            var createResult = JsonConvert.DeserializeObject<ApiResponse<MedicalCaseDetailDto>>(
                await createResponse.Content.ReadAsStringAsync());
            var createdId = createResult!.Data!.Id;

            // Act
            var startTime = DateTime.UtcNow;
            var response = await _client.GetAsync($"/api/medical-cases/{createdId}/details");
            var duration = DateTime.UtcNow - startTime;

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            duration.Should().BeLessThan(TimeSpan.FromSeconds(1), "API响应应该在1秒内");
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    // API响应模型
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}